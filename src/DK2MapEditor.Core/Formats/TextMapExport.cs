using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.Core.Formats;

public sealed record TextMapLoss(string Code, int Count);
public enum TextMapScriptMode { SupportedScenarios, OriginalNoScenarios }
public sealed record TextMapProjection(string Text, IReadOnlyList<TextMapLoss> Limitations)
{
    public TextMapScriptMode ScriptMode { get; init; } = TextMapScriptMode.SupportedScenarios;
}
public sealed record TextMapExportResult(string TextPath, string EditableCopyPath);

public static partial class TextMapFile
{
    /// <summary>The historical text format is a projection, not a replacement for a full project.</summary>
    public static TextMapProjection Project(NativeMapDocument document, TextMapScriptMode scriptMode = TextMapScriptMode.SupportedScenarios)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!Enum.IsDefined(scriptMode)) throw new ArgumentOutOfRangeException(nameof(scriptMode));
        var errors = NativeMapValidator.Validate(document, includeScripts: scriptMode == TextMapScriptMode.SupportedScenarios)
            .Where(item => item.Severity == DiagnosticSeverity.Error || item.Code == "DK2N047").ToArray();
        if (errors.Length != 0) throw new InvalidDataException("Fix map validation errors before text export: " + string.Join("; ", errors.Take(5).Select(item => item.Message)));
        var result = new Writer(document,scriptMode).Build();
        // Never publish syntactically invalid or internally dangling classic MAP output.
        _ = Parse(result.Text);
        return result;
    }

    /// <summary>Atomically publishes MAP together with an independent complete editable snapshot.</summary>
    public static TextMapExportResult SaveCopy(string directory, NativeMapDocument document, TextMapScriptMode scriptMode = TextMapScriptMode.SupportedScenarios)
    {
        var projection = Project(document,scriptMode); var destination = Path.GetFullPath(directory);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("The text export destination must not exist.");
        var parent = Path.GetDirectoryName(destination) ?? throw new ArgumentException("An export parent directory is required.");
        if (!Directory.Exists(parent)) throw new DirectoryNotFoundException(parent);
        var staging = Path.Combine(parent, ".dk2-text-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var snapshot = document.SaveCopy(Path.Combine(staging, "EditableCopy"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var bytes = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(projection.Text);
            using (var stream = new FileStream(Path.Combine(staging, "Map.map"), FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes); stream.Flush(true); }
            var report = "MAP is a limited historical interchange format. Do not treat Map.map as the complete source.\r\n" +
                "MAP — ограниченный исторический формат обмена. Полный источник находится в EditableCopy.\r\n" +
                (scriptMode == TextMapScriptMode.OriginalNoScenarios
                    ? "Mode: original editor — NO scenarios in Map.map; all stored scenario data and links remain in EditableCopy.\r\n" +
                      "Режим: оригинальный редактор — БЕЗ сценариев в Map.map; все сохранённые сценарии и связи остаются в EditableCopy.\r\n"
                    : "Mode: supported scenarios projected to Map.map; complete original data remain in EditableCopy.\r\n" +
                      "Режим: перенос поддерживаемых сценариев в Map.map; полные исходные данные остаются в EditableCopy.\r\n") +
                (!document.IsLegacyProject
                    ? "Native EditableCopy retains game-file bytes; editor-only working IDs and bindings are not stored in that format. Retain a DKM project with its source companion for editor state.\r\n" +
                      "Игровая EditableCopy сохраняет байты игровых файлов; рабочие ID и привязки редактора в этот формат не входят. Для рабочего состояния сохраняйте DKM с исходным архивом.\r\n"
                    : "") +
                "Open / Открыть: EditableCopy/" + Path.GetRelativePath(Path.Combine(staging, "EditableCopy"), snapshot).Replace('\\', '/') + "\r\n" +
                string.Join("\r\n", projection.Limitations.Select(loss => loss.Code + ": " + loss.Count)) + "\r\n";
            using (var stream = new FileStream(Path.Combine(staging, "Export-notes.txt"), FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(Encoding.UTF8.GetBytes(report)); stream.Flush(true); }
            var relative = Path.GetRelativePath(staging, snapshot);
            Directory.Move(staging, destination);
            return new(Path.Combine(destination, "Map.map"), Path.Combine(destination, relative));
        }
        catch { Directory.Delete(staging, true); throw; }
    }

    private sealed class Writer
    {
        private readonly NativeMapDocument document;
        private readonly TextMapScriptMode scriptMode;
        private readonly IReadOnlyList<NativeRecord> records;
        private readonly Dictionary<string, int> losses = new(StringComparer.Ordinal);
        private readonly List<Buffer> things = [], rectangles = [], bands = [];
        private readonly Dictionary<(NativeFile File,int Offset),int> nativeGateIDs = [];
        private readonly Buffer[] players;
        private readonly List<(int Scope, int Target, int Root)> roots = [];
        private readonly Dictionary<int, Script> scripts = [];
        private readonly Dictionary<MapTile, (string Token, bool Exact)> tileTokens = [];
        private readonly HashSet<int> emitted = [];
        private readonly StringBuilder text = new();
        private sealed record Script(int ID, int Next, int Child, string Type, Dictionary<string, string> Args, bool Condition, int Repeat, NativeRecord Source);
        private static string V(NativeRecord record, string name, string fallback = "0") => record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? record.Read(field) : fallback;
        private static int I(NativeRecord record, string name, int fallback = 0) => int.TryParse(V(record, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        private void Loss(string code, int count = 1) { if (count != 0) losses[code] = losses.GetValueOrDefault(code) + count; }
        private static void CopyFields(Buffer target, string structure, NativeRecord source)
        {
            foreach (var field in LegacyProjectSchemas.Fields(structure))
                if (source.Fields.FirstOrDefault(candidate => candidate.Name == field.Name) is { } original) field.Encode(source.Read(original)).CopyTo(target.Bytes, field.Offset);
        }

        public Writer(NativeMapDocument document,TextMapScriptMode scriptMode)
        {
            this.document = document; this.scriptMode = scriptMode; records = document.Records();
            var defaults = Parse("GUI_GENERATED_MAP\nDefaults\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF");
            var data = defaults.Sections.Single(section => section.Signature == NativeMapDocument.LegacyPlayers).Payload;
            players = Enumerable.Range(0, 7).Select(index =>
            {
                var player = new Buffer("MAPOPTIONS"); data.Span.Slice(index * player.Bytes.Length, player.Bytes.Length).CopyTo(player.Bytes); return player;
            }).ToArray();
        }

        public TextMapProjection Build()
        {
            if (document.Width < 3 || document.Height < 3) throw new InvalidDataException("Historical text maps require dimensions of at least 3×3.");
            // Settings/definitions/unknown bytes are intentionally kept in EditableCopy,
            // even when the following classic projection happens to be semantically exact.
            Loss("AdditionalSettings");
            if (!document.IsLegacyProject) Loss("NativeEditorState");
            var info = records.FirstOrDefault(record => record.File == document.MainFile || record.File.Signature == NativeMapDocument.LegacyInfo);
            if (document.IsLegacyProject) info = records.FirstOrDefault(record => record.File.Signature == NativeMapDocument.LegacyInfo);
            var name = info is null ? "ExportedMap" : V(info, document.IsLegacyProject ? "Name" : "wstrMainName", "ExportedMap");
            try
            {
                if (string.IsNullOrWhiteSpace(name) || name.Contains('\n') || name.Contains('\r')) throw new ArgumentException("Invalid name.");
                _ = new NativeField("Name", 0, 64, NativeValueKind.WesternAnsi).Encode(name);
            }
            catch (ArgumentException) { name = "ExportedMap"; Loss("Metadata"); }
            var playerCount = info is null ? 1 : I(info, document.IsLegacyProject ? "Players" : "bMainPlayers", 1);
            if (playerCount is < 1 or > 4) { playerCount = Math.Clamp(playerCount, 1, 4); Loss("Players"); }
            text.Append("GUI_GENERATED_MAP\r\n").Append(name).Append("\r\nPLAYERS ").Append(playerCount).Append("\r\n\r\nMAP\r\n")
                .Append(document.Width).Append(" X ").Append(document.Height).Append("\r\n");
            for (var y = 0; y < document.Height; y++)
            {
                for (var x = 0; x < document.Width; x++) text.Append(TileToken(document.GetTile(x, y)));
                text.Append("\r\n");
            }
            PrepareRecords();
            if (scriptMode == TextMapScriptMode.SupportedScenarios) PrepareScripts();
            WriteThings(); WriteHearts(); WriteBands(); WriteRectangles(); WriteScripts(); WriteVariables();
            text.Append("EOF\r\n");
            if (text.Length > MaximumTextBytes) throw new InvalidDataException("Text export exceeds 8 MiB.");
            return new(text.ToString(), losses.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new TextMapLoss(pair.Key, pair.Value)).ToArray()) { ScriptMode = scriptMode };
        }

        private string TileToken(MapTile tile)
        {
            if (tileTokens.TryGetValue(tile, out var cached)) { if (!cached.Exact) Loss("Terrain"); return cached.Token; }
            string? token = null;
            var all = new[] { "World", "Rooms", "Gates", "Walls" }.SelectMany(group => Catalogues[group]);
            foreach (var item in all.Where(item => item.AsciiName is not null && (byte)(item.ID >> 24) == tile.Terrain).OrderByDescending(item => (byte)(item.ID >> 8) == tile.Variant))
            {
                var symbol = item.AsciiName!;
                if (symbol.Length == 2) { if ((byte)(item.ID >> 16) == tile.Owner) token = symbol; }
                else if ((item.ID >> 16 & 255) == 0) token = tile.Owner is >= 1 and <= 6 ? Owner(tile.Owner)[1..] + symbol : null;
                else token = "O" + symbol;
                if (token is not null) break;
            }
            token ??= "Oc";
            var interpreted = new Parser("").Tile(token);
            tileTokens[tile] = (token, interpreted == tile);
            if (interpreted != tile) Loss("Terrain");
            return token;
        }
        private static string Owner(int player) => player switch { 1 => "PH", 2 => "PN", >= 3 and <= 6 => "P" + (player - 2), _ => throw new InvalidDataException("Player cannot be represented in classic MAP.") };
        private string? Name(int id, params string[] groups)
        {
            var elements = groups.SelectMany(group => Catalogues[group]).ToArray();
            var match = elements.FirstOrDefault(item => item.ID == (uint)id && item.AsciiName is not null);
            if (match is null) return null;
            // Elite creatures and several objects/traps share a token with a different ID.
            return elements.First(item => item.AsciiName == match.AsciiName).ID == match.ID ? match.AsciiName : null;
        }

        private void PrepareRecords()
        {
            uint unique = 0;
            foreach (var record in records)
            {
                if (record.File.Signature == NativeMapDocument.LegacyPlayers && record.Fields.Count != 0)
                {
                    var index = (record.Offset - 12) / record.Length;
                    if (index < 7) CopyFields(players[index], "MAPOPTIONS", record);
                }
                else if (record.Category == "GAMEPLAYER")
                {
                    var index = I(record, "bGlobalPlayer") - 1;
                    if (index is < 0 or >= 7) { Loss("Players"); continue; }
                    foreach (var rule in LegacyCompilerRules.Players)
                        if (record.Fields.FirstOrDefault(field => field.Name == rule.Target) is { } field) players[index].Set(rule.Source, record.Read(field));
                    roots.Add((1, index + 1, I(record, "bPlayerTrigger")));
                }
                else if (record.File.Signature == NativeMapDocument.LegacyThings && record.Fields.Count != 0 || record.Category is "GAMEOBJECT" or "GAMETRAP" or "GAMEDOOR" or "GAMENEUTRAL" or "GAMEHERO" or "GAMEKEEPER")
                {
                    var thing = new Buffer("MAPTHING");
                    SelectionFile.ToLegacyThing(new(record.File.Signature, record.Category, record.File.Data.Span.Slice(record.Offset, record.Length).ToArray(), record.Fields, record.Offset), new(0, 0, document.Width, document.Height), ++unique).CopyTo(thing.Bytes, 0);
                    things.Add(thing);
                }
                else if (record.File.Signature == NativeMapDocument.LegacyAreas && record.Fields.Count != 0)
                {
                    var rectangle = new Buffer("MAPRECTEX"); CopyFields(rectangle, "MAPRECTEX", record);
                    if (rectangle.Get("node.type") is 0 or 1) rectangles.Add(rectangle); else Loss("Areas");
                }
                else if (record.Category == "GAMEAREA")
                {
                    var rectangle = new Buffer("MAPRECTEX"); rectangle.Set("id", V(record, "bAreaID"));
                    foreach (var (target, source) in new[] { ("rect.x1", "dwAreaLocationStartX"), ("rect.y1", "dwAreaLocationStartY"), ("rect.x2", "dwAreaLocationEndX"), ("rect.y2", "dwAreaLocationEndY") }) rectangle.Set(target, V(record, source));
                    rectangles.Add(rectangle); roots.Add((4, rectangle.Get("id"), I(record, "wAreaTrigger")));
                }
                else if (record.Category == "GAMEGATE" && I(record, "bGateType") != 14)
                {
                    var used = rectangles.Where(item => item.Get("node.type") == 1).Select(item => item.Get("id")).ToHashSet();
                    var id = Enumerable.Range(1, 255).FirstOrDefault(value => !used.Contains(value));
                    if (id == 0) { Loss("Gates"); continue; }
                    var gate = new Buffer("MAPRECTEX"); gate.Set("node.type", 1); gate.Set("id", id);
                    gate.Set("rect.x1", V(record, "dwGateLocationX")); gate.Set("rect.x2", V(record, "dwGateLocationX"));
                    gate.Set("rect.y1", V(record, "dwGateLocationY")); gate.Set("rect.y2", V(record, "dwGateLocationY")); rectangles.Add(gate);
                    nativeGateIDs.Add((record.File,record.Offset),id);
                }
                else if (record.File.Signature == NativeMapDocument.LegacyBands && record.Fields.Count != 0)
                { var band = new Buffer("MTHBAND"); CopyFields(band, "MTHBAND", record); bands.Add(band); }
                else if (record.Category == "GAMEBAND")
                {
                    var band = new Buffer("MTHBAND"); band.Set("id", V(record, "bBandID"));
                    var ended = false;
                    for (var index = 0; index < 16; index++)
                    {
                        var hero = records.SingleOrDefault(item => item.File == record.File && item.Offset == record.Offset + 51 + index * 32);
                        if (hero is null || I(hero, "bGlobalCreatureID") == 0 || I(hero, "bGlobalBehavior") == 0)
                        {
                            ended = true;
                            if (hero is not null && (I(hero, "bGlobalCreatureID") != 0 || I(hero, "bGlobalBehavior") != 0 || I(hero, "wHeroTrigger") != 0)) Loss("Bands");
                            continue;
                        }
                        if (ended) { Loss("Bands"); continue; }
                        var prefix = $"heros[{index}]."; band.Set(prefix + "isvalid", 1);
                        foreach (var (target, source) in new[] { ("id", "bGlobalCreatureID"), ("level", "bGlobalLevel"), ("enemy", "bGlobalPlayer") }) band.Set(prefix + target, V(hero, source));
                    }
                    bands.Add(band);
                }
                else if (record.Category == "GAMEEFFECT") Loss("Areas");
                else if (record.Category == "Project.Opaque") Loss("AdditionalSettings");
            }
            foreach (var group in rectangles.GroupBy(item => (item.Get("node.type"), item.Get("id"))).Where(group => group.Key.Item2 is < 1 or > 255 || group.Count() > 1).ToArray())
            { Loss(group.Key.Item1 == 0 ? "Areas" : "Gates", group.Count()); foreach (var item in group) rectangles.Remove(item); }
            foreach (var group in bands.GroupBy(item => item.Get("id")).Where(group => group.Key is < 0 or > 255 || group.Count() > 1).ToArray())
            { Loss("Bands", group.Count()); foreach (var item in group) bands.Remove(item); }
            PrepareNativeVariables();
        }

        private void PrepareNativeVariables()
        {
            if (document.IsLegacyProject) return;
            var creatures = new[] { "BadCreatures", "BadElites", "Heros" }.SelectMany(group => Catalogues[group]).ToArray();
            foreach (var record in records.Where(record => record.File.Signature == 0xE6 && record.File.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                var player = I(record, record.Category == "GAMEVARCREATURECOUNT" ? "dwGlobalPlayer" : "wGlobalPlayer") - 1;
                if (player is < 0 or >= 7) continue;
                if (record.Category == "GAMEVARCREATURECOUNT")
                {
                    var index = Array.FindIndex(creatures, item => item.Variable == (uint)I(record, "dwGlobalCreatureID"));
                    var count = unchecked((int)uint.Parse(V(record, "dwCreatureCount"), CultureInfo.InvariantCulture));
                    if (index < 0 || count < -1) { Loss("Variables"); continue; }
                    players[player].Set($"CreaturesCount[{index}]", count);
                }
                if (record.Category == "GAMEVARTHINGSTATUS")
                {
                    var (group, field) = I(record, "wThingType") switch { 1 => ("VarRooms", "Rooms"), 3 => ("Traps", "Traps"), 4 => ("Doors", "Doors"), 5 => ("Spells", "Spells"), _ => ("", "") };
                    if (group.Length == 0) { Loss("Variables"); continue; }
                    var index = Catalogues[group].ToList().FindIndex(item => item.Variable == (uint)I(record, "dwThingID")); var state = I(record, "dwThingState", -1);
                    if (index < 0 || state is < 0 or > 255) { Loss("Variables"); continue; }
                    players[player].Set($"{field}[{index}]", state);
                }
            }
        }
        private void Section(string name, IReadOnlyList<string> rows)
        {
            if (rows.Count > 100000) throw new InvalidDataException("Too many records for a historical text section.");
            text.Append("\r\n").Append(name).Append("\r\n").Append(rows.Count).Append("\r\n");
            foreach (var row in rows) { text.Append(row).Append("\r\n"); if (text.Length > MaximumTextBytes) throw new InvalidDataException("Text export exceeds 8 MiB."); }
        }
        private void WriteThings()
        {
            foreach (var (type, header) in new[] { (3, "THINGS"), (0, "CREATURES"), (1, "DOORS"), (2, "TRAPS") })
            {
                var rows = new List<string>();
                foreach (var item in things.Where(item => item.Get("node.type") == type || type == 3 && item.Get("node.type") == 4))
                {
                    var name = Name(item.Get("id"), type == 0 ? ["BadCreatures", "BadElites", "Heros"] : type == 1 ? ["Doors"] : type == 2 ? ["Traps"] : ["Magics", "Objects"]);
                    var player = item.Get("owner"); var level = item.Get("dataAll");
                    if (name is null || type != 3 && player is < 1 or > 6 || type == 0 && level is < 1 or > 10 || type == 1 && level is not (0 or 1)) { Loss("Things"); continue; }
                    rows.Add(name + " " + item.Get("x") + " " + item.Get("y") + (type == 0 ? " L" + level + " " + Owner(player) : type == 1 ? " " + Owner(player) + (level == 1 ? " L" : " U") : type == 2 ? " " + Owner(player) : ""));
                }
                Section(header, rows);
            }
        }
        private void WriteHearts()
        {
            text.Append("\r\nHEARTS_AND_GOLD\r\n");
            for (var index = 0; index < 4; index++) text.Append("KPR").Append(index + 1).Append("_HEART_CENTER -1 -1\r\n");
            for (var index = 0; index < 4; index++)
            {
                var value = players[index + 2].Get("PlayerGold"); if (value < 0) { value = 0; Loss("Players"); }
                text.Append("GOLD").Append(index + 1).Append(' ').Append(value).Append("\r\n");
            }
        }
        private void WriteBands()
        {
            var rows = new List<string>();
            foreach (var band in bands.ToArray())
            {
                var heroes = Enumerable.Range(0, 16).Where(index => band.Get($"heros[{index}].isvalid") != 0).ToArray();
                var enemy = heroes.Length == 0 ? 0 : band.Get($"heros[{heroes[0]}].enemy");
                if (heroes.Length == 0 || enemy is < 1 or > 6 || heroes.Any(index => Name(band.Get($"heros[{index}].id"), "Heros", "BadCreatures", "BadElites") is null || band.Get($"heros[{index}].level") is < 1 or > 10))
                { Loss("Bands"); bands.Remove(band); continue; }
                if (heroes.Any(index => band.Get($"heros[{index}].enemy") != enemy)) Loss("Bands");
                rows.Add("HEROES " + heroes.Length + " " + Owner(enemy) + " " + band.Get("id") + "\r\n" + string.Join("\r\n", heroes.Select(index => Name(band.Get($"heros[{index}].id"), "Heros", "BadCreatures", "BadElites") + " L" + band.Get($"heros[{index}].level"))));
            }
            Section("HERO_BANDS", rows);
        }
        private void WriteRectangles()
        {
            Section("AREAS", rectangles.Where(item => item.Get("node.type") == 0).Select(item => item.Get("id") + " " + item.Get("rect.x1") + " " + item.Get("rect.y1") + " " + item.Get("rect.x2") + " " + item.Get("rect.y2")).ToArray());
            Section("HERO_GATES", rectangles.Where(item => item.Get("node.type") == 1).Select(item => item.Get("id") + " " + item.Get("rect.x1") + " " + item.Get("rect.y1")).ToArray());
        }

        private void PrepareScripts()
        {
            foreach (var record in records.Where(record => record.Category is "Project.TRIGGER" or "Project.ACTION" || record.File.Signature == 0xD2))
            {
                if (!record.Fields.Any(field => field.Name is "id" or "ID")) { Loss("Scripts"); continue; }
                var condition = document.IsLegacyProject ? record.Category == "Project.TRIGGER" : BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[record.Offset..]) == 0xD5;
                var type = LegacyEventSchemas.Find(condition ? 0xD5u : 0xD6u, checked((byte)I(record, document.IsLegacyProject ? "ctrl" : "Type")));
                var arguments = record.Fields.Where(field => document.IsLegacyProject ? field.Name.StartsWith("param", StringComparison.Ordinal) : field.Name.StartsWith("Param", StringComparison.Ordinal)).ToDictionary(field => "param" + field.Name[5..], record.Read);
                var id = I(record, document.IsLegacyProject ? "id" : "ID");
                scripts.Add(id, new(id, I(record, document.IsLegacyProject ? condition ? "or" : "next" : "NextOr"), I(record, document.IsLegacyProject ? condition ? "and" : "unused" : "ChildAnd"), type?.Name ?? "", arguments, condition, I(record, document.IsLegacyProject ? condition ? "repeat" : "one" : "Repeat"), record));
            }
            if (document.IsLegacyProject)
                roots.AddRange(records.Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).Select(record => (I(record, "node.type"), I(record, I(record, "node.type") == 4 ? "area" : "player"), I(record, "trigger"))));
        }
        private string? Condition(Script script, int scope, int target)
        {
            int Arg(string name) => int.TryParse(script.Args.GetValueOrDefault(name, "0"), out var value) ? value : -1;
            if (!script.Condition || Arg("param1") != 5) return null;
            if (scope == 4 && script.Type == "TRIGGER_CLAIMENTIREAREA" && Arg("param2") is >= 1 and <= 6 && rectangles.Any(item => item.Get("node.type") == 0 && item.Get("id") == target)) return "TRIGGER_CONTROL_AREA " + Owner(Arg("param2")) + " " + target;
            if (scope == 1 && target == 3 && Arg("param5") >= 0)
                return script.Type switch { "TRIGGER_TIMEELAPSED" => "TRIGGER_TIME " + Arg("param5"), "TRIGGER_SLAPCOUNT" => "TRIGGER_SLAP_COUNT_EQUALS " + Arg("param5"), _ => null };
            return null;
        }
        private string? ActionLine(Script script)
        {
            int Arg(string name) => int.TryParse(script.Args.GetValueOrDefault(name, "0"), out var value) ? value : -1;
            if (script.Condition || script.Child != 0) return null;
            if (script.Type == "ACTION_RELEASEBAND" && bands.Any(item => item.Get("id") == Arg("param1")) && rectangles.Any(item => item.Get("node.type") == 0 && item.Get("id") == Arg("param5"))) return "ACTION_HERO_RELEASE " + Arg("param1") + " A " + Arg("param5");
            if (script.Type == "ACTION_DESTROYGATE")
            {
                var id = Arg("param5");
                if (!document.IsLegacyProject)
                {
                    // Preserve the working target, including deliberate absence
                    // and an explicitly selected duplicate at the same point.
                    id = document.NativeGateTarget(script.Source) is { } gate
                        && nativeGateIDs.TryGetValue((gate.File,gate.Offset),out var projectedID) ? projectedID : 0;
                }
                return rectangles.Any(item => item.Get("node.type") == 1 && item.Get("id") == id) ? "ACTION_DESTROY_HERO_GATE " + id : null;
            }
            if (script.Type != "ACTION_ADDELEMENT" || Arg("param1") is < 1 or > 6 || Arg("param4") != 1) return null;
            var (group, command) = Arg("param2") switch { 1 => ("VarRooms", "ACTION_ADD_ROOM"), 3 => ("Doors", "ACTION_ADD_DOOR"), 4 => ("Traps", "ACTION_ADD_TRAP"), 5 => ("Spells", "ACTION_ADD_SPELL"), _ => ("", "") };
            if (group.Length == 0) return null;
            var element = Catalogues[group].FirstOrDefault(item => (group == "VarRooms" ? item.Variable : item.ID) == (uint)Arg("param3"));
            var name = element is null ? null : Name((int)element.ID, group);
            return name is null ? null : command + " " + Owner(Arg("param1")) + " " + name;
        }
        private void WriteScripts()
        {
            if (scriptMode == TextMapScriptMode.OriginalNoScenarios)
            {
                // Files_Text.c: Text_GenerateFile writes a literal TRIGGERS 0.
                // Do not index/traverse/deduplicate omitted scenario records or
                // change their stored root links in the complete snapshot.
                Loss("OriginalNoScenarios");
                Loss("Scripts",records.Count(ScriptGraph.IsEvent));
                Loss("ScriptLinks",records.Where(record => !ScriptGraph.IsEvent(record)).Sum(record => record.Fields.Count(field =>
                    (record.Category.StartsWith("Project.TRIGGERLINK",StringComparison.Ordinal) ? field.Name == "trigger" : field.Name.EndsWith("Trigger",StringComparison.Ordinal))
                    && long.TryParse(record.Read(field),out var value) && value != 0)));
                Section("TRIGGERS",Array.Empty<string>()); return;
            }
            var rows = new List<string>();
            var operations = 0; var characters = 0;
            foreach (var (scope, target, root) in roots.Where(root => root.Root != 0).Distinct())
            {
                if (!(scope == 1 && target == 3) && !(scope == 4 && rectangles.Any(item => item.Get("node.type") == 0 && item.Get("id") == target))) continue;
                var visited = new HashSet<int>(); var id = root;
                while (id != 0 && visited.Add(id) && scripts.TryGetValue(id, out var script))
                {
                    if (++operations > 1000000) throw new InvalidDataException("Text script projection exceeds its traversal limit.");
                    var condition = Condition(script, scope, target); var actionRows = new List<string>(); var actionIDs = new HashSet<int>(); var actionID = script.Child; var valid = condition is not null;
                    while (valid && actionID != 0)
                    {
                        if (++operations > 1000000) throw new InvalidDataException("Text script projection exceeds its traversal limit.");
                        if (!actionIDs.Add(actionID) || !scripts.TryGetValue(actionID, out var action) || ActionLine(action) is not { } line) { valid = false; break; }
                        actionRows.Add(line); actionID = action.Next;
                    }
                    if (valid)
                    {
                        var row = condition + "\r\n" + actionRows.Count + (actionRows.Count == 0 ? "" : "\r\n" + string.Join("\r\n", actionRows));
                        characters += row.Length;
                        if (characters > MaximumTextBytes || rows.Count >= 100000) throw new InvalidDataException("Text script projection exceeds its size limit.");
                        rows.Add(row); emitted.Add(script.ID); emitted.UnionWith(actionIDs);
                        if (script.Repeat != 1 || actionIDs.Any(action => scripts[action].Repeat != 1)) Loss("Scripts");
                    }
                    id = script.Next;
                }
                if (id != 0 && visited.Contains(id)) Loss("Scripts");
            }
            Loss("Scripts", scripts.Keys.Count(id => !emitted.Contains(id)));
            Section("TRIGGERS", rows);
        }
        private void WriteVariables()
        {
            text.Append("\r\nSCENARIO_DESCRIPTION\r\n");
            var creatures = new[] { "BadCreatures", "BadElites", "Heros" }.SelectMany(group => Catalogues[group]).ToArray();
            for (var player = 1; player <= 6; player++)
            {
                for (var index = 0; index < creatures.Length; index++)
                {
                    var value = players[player - 1].Get($"CreaturesCount[{index}]"); var name = Name((int)creatures[index].ID, "BadCreatures", "BadElites", "Heros");
                    if (name is null) { if (value != -1) Loss("Variables"); continue; }
                    if (value < -1) { Loss("Variables"); continue; }
                    text.Append("CREATURE_COUNT ").Append(Owner(player)).Append(' ').Append(name).Append(' ').Append(value).Append("\r\n");
                }
                foreach (var (group, field) in new[] { ("VarRooms", "Rooms"), ("Doors", "Doors"), ("Traps", "Traps"), ("Spells", "Spells") })
                    for (var index = 0; index < Catalogues[group].Count; index++)
                    {
                        var value = players[player - 1].Get($"{field}[{index}]"); var name = Name((int)Catalogues[group][index].ID, group);
                        if (name is null || value is < 1 or > 3) { if (value != 255) Loss("Variables"); continue; }
                        var command = value == 1 ? "NO" : value == 2 ? "MUST_RESEARCH" : "START_WITH";
                        text.Append(command).Append(' ').Append(Owner(player)).Append(' ').Append(name).Append("\r\n");
                    }
            }
        }
    }
}
