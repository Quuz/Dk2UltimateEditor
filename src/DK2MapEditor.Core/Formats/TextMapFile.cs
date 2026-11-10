using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.Core.Formats;

/// <summary>Atomic, bounded import of the original GUI_GENERATED_MAP text grammar.</summary>
public static partial class TextMapFile
{
    private const int MaximumTextBytes = 8 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<LegacyCompilerRules.Element>> Catalogues =
        new[] { "World", "Rooms", "Walls", "Gates", "BadCreatures", "BadElites", "Heros", "Doors", "Traps", "Objects", "Magics", "Spells", "VarRooms", "MapVars" }
            .ToDictionary(name => name, LegacyCompilerRules.Elements);

    public static LegacySectionFile Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is < 1 or > MaximumTextBytes) throw new InvalidDataException("Invalid text map size (limit: 8 MiB).");
        var bytes = new byte[(int)stream.Length]; stream.ReadExactly(bytes);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            var text = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })
                ? new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3)
                : Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetString(bytes);
            return Parse(text);
        }
        catch (DecoderFallbackException exception) { throw new InvalidDataException("Invalid text MAP encoding.", exception); }
    }

    public static LegacySectionFile Parse(string text)
    {
        if (text.Length > MaximumTextBytes || text.Contains('\0')) throw new InvalidDataException("Invalid text map size or embedded NUL.");
        var parser = new Parser(text);
        try { return parser.Read(); }
        catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
        { throw parser.Error(exception.Message); }
    }

    private sealed class Buffer(string structure)
    {
        public byte[] Bytes { get; } = new byte[LegacyProjectSchemas.Size(structure)];
        private readonly IReadOnlyDictionary<string, NativeField> fields = LegacyProjectSchemas.Fields(structure).ToDictionary(field => field.Name);
        public void Set(string name, object value) => fields[name].Encode(Convert.ToString(value, CultureInfo.InvariantCulture)!).CopyTo(Bytes, fields[name].Offset);
        public int Get(string name) => int.Parse(fields[name].Read(Bytes), CultureInfo.InvariantCulture);
        public void SetPrevious(int id)
        {
            // TRIGGER/ACTION keep a historical signed-short backlink immediately
            // after NODE. Its bit pattern is an event ID; it is not a UI field.
            var offset = LegacyProjectSchemas.Size("NODE");
            if (structure is not ("TRIGGER" or "ACTION") || fields["param1"].Offset != offset + 4)
                throw new InvalidDataException("Unverified historical event backlink layout.");
            BinaryPrimitives.WriteUInt16LittleEndian(Bytes.AsSpan(offset), checked((ushort)id));
        }
    }

    private sealed class Parser
    {
        private readonly string[] lines;
        private int cursor;
        private int width, height;
        private uint uniqueId;
        private int conditionId, actionId;
        private long recordBytes;
        private readonly Buffer info = new("MAPPROPERTIES");
        private readonly Buffer[] players = Enumerable.Range(0, 7).Select(_ => new Buffer("MAPOPTIONS")).ToArray();
        private readonly List<Buffer> things = [], rectangles = [], bands = [], links = [], conditions = [], actions = [];
        private readonly Dictionary<(int Scope, int Target), Buffer> previous = [];
        private static readonly HashSet<string> Headers = new(StringComparer.OrdinalIgnoreCase)
        { "THINGS", "CREATURES", "DOORS", "TRAPS", "HEARTS_AND_GOLD", "HERO_BANDS", "AREAS", "HERO_GATES", "TRIGGERS", "SCENARIO_DESCRIPTION", "EOF" };

        public Parser(string text)
        {
            lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
            if (lines.Length > 200000 || lines.Any(line => line.Length > 4096)) throw new InvalidDataException("Too many or excessively long text-map lines.");
        }
        public InvalidDataException Error(string message) => new($"Text MAP line {Math.Max(1, cursor)}: {message}");
        private string Line()
        {
            while (cursor < lines.Length && string.IsNullOrWhiteSpace(lines[cursor])) cursor++;
            if (cursor == lines.Length) throw Error("Unexpected end of file.");
            return lines[cursor++].Trim();
        }
        private string[] Words() => Line().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        private void Length(string[] words, int count) { if (words.Length != count) throw Error($"Expected {count} tokens, found {words.Length}."); }
        private void Expect(string expected) { if (!Line().Equals(expected, StringComparison.OrdinalIgnoreCase)) throw Error("Expected " + expected + "."); }
        private int Number(string text, int minimum = 0, int maximum = int.MaxValue)
        {
            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) || value < minimum || value > maximum)
                throw Error($"Invalid number '{text}' (expected {minimum}..{maximum}).");
            return value;
        }
        private int Count() { var words = Words(); Length(words, 1); return Number(words[0], 0, 100000); }
        private int Player(string token) => token.ToUpperInvariant() switch
        { "PH" => 1, "PN" => 2, "P1" => 3, "P2" => 4, "P3" => 5, "P4" => 6, _ => throw Error("Invalid player token: " + token) };
        private int Level(string token) => token.StartsWith('L') || token.StartsWith('l') ? Number(token[1..], 1, 10) : throw Error("Expected L1..L10.");
        private int X(string token) => Number(token, 0, width - 1);
        private int Y(string token) => Number(token, 0, height - 1);
        private static IReadOnlyList<LegacyCompilerRules.Element> Creatures => [.. Catalogues["BadCreatures"], .. Catalogues["BadElites"], .. Catalogues["Heros"]];
        private LegacyCompilerRules.Element Find(string name, params string[] groups) => groups.SelectMany(group => Catalogues[group])
            .FirstOrDefault(item => string.Equals(item.AsciiName, name, StringComparison.OrdinalIgnoreCase)) ?? throw Error("Unknown element: " + name);
        private void Add(List<Buffer> collection, Buffer buffer)
        {
            recordBytes += buffer.Bytes.Length;
            if (recordBytes > 60 * 1024 * 1024) throw Error("Imported record data exceeds the safe limit.");
            collection.Add(buffer);
        }

        public LegacySectionFile Read()
        {
            Expect("GUI_GENERATED_MAP"); var name = Line();
            var playerLine = Words(); Length(playerLine, 2);
            if (!playerLine[0].Equals("PLAYERS", StringComparison.OrdinalIgnoreCase)) throw Error("Expected PLAYERS.");
            var playerCount = Number(playerLine[1], 1, 4);
            Expect("MAP"); var dimensions = Words(); Length(dimensions, 3);
            if (!dimensions[1].Equals("X", StringComparison.OrdinalIgnoreCase)) throw Error("Expected width X height.");
            width = Number(dimensions[0], 3, 1024); height = Number(dimensions[2], 3, 1024);
            var tiles = new byte[8 + width * height * 4]; Write(tiles, 0, width); Write(tiles, 4, height);
            for (var y = 0; y < height; y++)
            {
                var row = Line();
                if (row.Length != width * 2) throw Error("Tile row length does not match map width.");
                for (var x = 0; x < width; x++) BinaryPrimitives.WriteUInt32LittleEndian(tiles.AsSpan(8 + (y * width + x) * 4), Tile(row.Substring(x * 2, 2)).Packed);
            }
            Defaults(name, playerCount);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                var header = Line().ToUpperInvariant();
                if (!Headers.Contains(header)) throw Error("Unknown section: " + header);
                if (!seen.Add(header)) throw Error("Duplicate section: " + header);
                if (header == "EOF") break;
                if (header == "HEARTS_AND_GOLD") { Hearts(); continue; }
                if (header == "SCENARIO_DESCRIPTION") { Scenario(); continue; }
                var count = Count();
                for (var i = 0; i < count; i++)
                    switch (header)
                    {
                        case "THINGS": case "CREATURES": case "DOORS": case "TRAPS": Thing(header); break;
                        case "HERO_BANDS": Band(); break;
                        case "AREAS": case "HERO_GATES": Rectangle(header); break;
                        case "TRIGGERS": Trigger(); break;
                    }
            }
            if (lines.Skip(cursor).Any(line => !string.IsNullOrWhiteSpace(line))) throw Error("Unexpected data after EOF.");
            VerifyReferences();
            var eventBytes = new byte[12 + links.Sum(item => item.Bytes.Length) + conditions.Sum(item => item.Bytes.Length) + actions.Sum(item => item.Bytes.Length) + 146 * 64];
            Write(eventBytes, 0, links.Count); Write(eventBytes, 4, conditions.Count); Write(eventBytes, 8, actions.Count);
            var offset = 12;
            foreach (var record in links.Concat(conditions).Concat(actions)) { record.Bytes.CopyTo(eventBytes, offset); offset += record.Bytes.Length; }
            return LegacySectionFile.Create(new[]
            {
                new LegacySection(LegacySectionFile.TileSectionSignature, 1, tiles),
                new LegacySection(NativeMapDocument.LegacyInfo, 7, info.Bytes),
                new LegacySection(NativeMapDocument.LegacyPlayers, 3, Join(players)),
                new LegacySection(NativeMapDocument.LegacyThings, 6, Join(things)),
                new LegacySection(NativeMapDocument.LegacyAreas, 1, Join(rectangles)),
                new LegacySection(NativeMapDocument.LegacyBands, 5, Join(bands)),
                new LegacySection(NativeMapDocument.LegacyEvents, 3, eventBytes)
            });
        }

        public MapTile Tile(string token)
        {
            LegacyCompilerRules.Element element;
            if (token[0] == 'O')
            {
                var code = token[1] switch { 'r' => 'p', 's' => 'q', var value => value };
                var group = code == 'm' ? "Rooms" : code >= 'i' && code != 'o' ? "Gates" : "World";
                element = Catalogues[group].FirstOrDefault(item => item.AsciiName == code.ToString()) ?? throw Error("Unknown tile: " + token);
                var owner = code == 'm' ? (byte)1 : (byte)(element.ID >> 16);
                return new((byte)(element.ID >> 24), owner, (byte)(element.ID >> 8));
            }
            var player = token[0] switch { 'H' => 1, 'N' => 2, >= '1' and <= '4' => token[0] - '1' + 3, _ => throw Error("Unknown tile owner: " + token) };
            var groupName = token[1] is 'A' or 'B' ? "Walls" : "Rooms";
            element = Catalogues[groupName].FirstOrDefault(item => item.AsciiName == (groupName == "Walls" ? token : token[1..])) ?? throw Error("Unknown tile: " + token);
            return new((byte)(element.ID >> 24), (byte)player, (byte)(element.ID >> 8));
        }

        private void Defaults(string name, int playerCount)
        {
            info.Set("Width", width); info.Set("Height", height); info.Set("Name", name); info.Set("Players", playerCount);
            info.Set("FillID", unchecked((int)0x02020101)); info.Set("Version", 13); info.Set("Options", 4); info.Set("Options2", 12);
            info.Set("Soundtrack", 1); info.Set("GameTicks", 4);
            for (var i = 0; i < 7; i++) if (i != 1) { info.Set($"Allies[1][{i}]", 1); info.Set($"Allies[{i}][1]", 1); }
            foreach (var item in Catalogues["MapVars"].Where(item => item.ID < 256)) info.Set($"Variables[{item.ID}]", unchecked((int)item.Variable));
            for (var i = 0; i < 7; i++)
            {
                var player = players[i];
                foreach (var rule in LegacyCompilerRules.DefaultPlayerAI) player.Set(rule.Target, rule.Source);
                player.Set("PlayerHealth", 100); player.Set("PlayerGold", 3000); player.Set("PlayerComputer", i < 2 ? 1 : 0);
                player.Set("PlayerName", PlayerSettings.DefaultName((uint)i + 1));
                foreach (var coordinate in new[] { "PlayerCPUAreaX1", "PlayerCPUAreaY1", "PlayerCPUAreaX2", "PlayerCPUAreaY2", "PlayerCameraX", "PlayerCameraY" }) player.Set(coordinate, -1);
                for (var index = 0; index < 45; index++) player.Set($"CreaturesCount[{index}]", index is >= 1 and <= 13 && index is not (6 or 11) ? 32 : -1);
                foreach (var (field, count, defaults) in new[] { ("Rooms", 15, 15), ("Traps", 14, 11), ("Doors", 8, 6), ("Spells", 12, 12) })
                    for (var index = 0; index < count; index++) player.Set($"{field}[{index}]", index >= defaults ? 255 : field == "Spells" && index is not (0 or 1 or 11) ? 2 : 3);
            }
        }

        private void Thing(string category)
        {
            var words = Words(); Length(words, category == "THINGS" ? 3 : category == "TRAPS" ? 4 : 5);
            var groups = category switch { "THINGS" => new[] { "Magics", "Objects" }, "CREATURES" => new[] { "BadCreatures", "Heros" }, "DOORS" => new[] { "Doors" }, _ => new[] { "Traps" } };
            var element = Find(words[0], groups); var thing = new Buffer("MAPTHING");
            var type = category == "CREATURES" ? 0 : category == "DOORS" ? 1 : category == "TRAPS" ? 2 : Catalogues["Magics"].Contains(element) ? 4 : 3;
            thing.Set("node.type", type); thing.Set("id", element.ID); thing.Set("x", X(words[1])); thing.Set("y", Y(words[2])); thing.Set("uniqueid", ++uniqueId);
            thing.Set("owner", category == "THINGS" ? 2 : Player(words[category == "CREATURES" ? 4 : 3]));
            if (category == "CREATURES") { thing.Set("dataAll", Level(words[3])); thing.Set("health", 100); thing.Set("creaturesoptions", 9); thing.Set("objective", 19); }
            if (category == "TRAPS") thing.Set("dataAll", 4);
            if (category == "DOORS") thing.Set("dataAll", words[4].ToUpperInvariant() switch { "L" => 1, "U" => 0, _ => throw Error("Expected door lock state L or U.") });
            Add(things, thing);
        }

        private void Band()
        {
            var words = Words(); Length(words, 4);
            if (!words[0].Equals("HEROES", StringComparison.OrdinalIgnoreCase)) throw Error("Expected HEROES count player id.");
            var count = Number(words[1], 1, 16); var enemy = Player(words[2]); var id = Number(words[3], 0, 255);
            if (bands.Any(band => band.Get("id") == id)) throw Error("Duplicate hero-band ID.");
            var band = new Buffer("MTHBAND"); band.Set("id", id); band.Set("uniqueid", ++uniqueId); band.Set("name", "Band " + id);
            for (var i = 0; i < count; i++)
            {
                words = Words(); Length(words, 2); var element = Find(words[0], "Heros", "BadCreatures"); var prefix = $"heros[{i}].";
                band.Set(prefix + "isvalid", 1); band.Set(prefix + "id", element.ID); band.Set(prefix + "level", Level(words[1])); band.Set(prefix + "enemy", enemy);
                band.Set(prefix + "health", 100); band.Set(prefix + "objective", 19); band.Set(prefix + "options2", i == 0 ? 43 : 45); band.Set(prefix + "uniqueid", ++uniqueId);
            }
            Add(bands, band);
        }

        private void Rectangle(string section)
        {
            var words = Words(); Length(words, section == "AREAS" ? 5 : 3);
            var id = Number(words[0], 1, 255); var type = section == "AREAS" ? 0 : 1;
            if (rectangles.Any(rectangle => rectangle.Get("node.type") == type && rectangle.Get("id") == id)) throw Error("Duplicate area/gate ID.");
            var x = X(words[1]); var y = Y(words[2]); var x2 = type == 0 ? X(words[3]) : x; var y2 = type == 0 ? Y(words[4]) : y;
            if (x2 < x || y2 < y) throw Error("Reversed rectangle coordinates.");
            AddRectangle(type, id, x, y, x2, y2);
        }
        private void AddRectangle(int type, int id, int x, int y, int x2, int y2)
        {
            var rectangle = new Buffer("MAPRECTEX"); rectangle.Set("node.type", type); rectangle.Set("id", id);
            rectangle.Set("rect.x1", x); rectangle.Set("rect.y1", y); rectangle.Set("rect.x2", x2); rectangle.Set("rect.y2", y2);
            rectangle.Set("rect.color", type == 0 ? 0xFFFF00 : 0xAAAAAA); rectangle.Set("rect.textcolor", type == 0 ? 0xFFFF00 : 0xAAAAAA);
            rectangle.Set("name", (type == 0 ? "Area " : "Gate ") + id); Add(rectangles, rectangle);
        }
        private bool AtHeader()
        {
            while (cursor < lines.Length && string.IsNullOrWhiteSpace(lines[cursor])) cursor++;
            return cursor == lines.Length || Headers.Contains(lines[cursor].Trim());
        }
        private void Hearts()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (!AtHeader())
            {
                var words = Words(); var key = words[0].ToUpperInvariant();
                if (!seen.Add(key)) throw Error("Duplicate heart/gold entry.");
                if (key.Length == 5 && key.StartsWith("GOLD", StringComparison.Ordinal) && key[4] is >= '1' and <= '4')
                { Length(words, 2); players[key[4] - '1' + 2].Set("PlayerGold", Number(words[1])); }
                else if (key.Length == 17 && key.StartsWith("KPR", StringComparison.Ordinal) && key.EndsWith("_HEART_CENTER", StringComparison.Ordinal) && key[3] is >= '1' and <= '4')
                {
                    Length(words, 3);
                    // Historical informational hints, not camera overrides; the compiler locates actual heart tiles.
                    if (words[1] != "-1" || words[2] != "-1") { _ = X(words[1]); _ = Y(words[2]); }
                }
                else throw Error("Unknown heart/gold entry: " + key);
            }
        }
        private void Scenario()
        {
            while (!AtHeader())
            {
                var words = Words(); var command = words[0].ToUpperInvariant(); Length(words, command == "CREATURE_COUNT" ? 4 : 3);
                var player = players[Player(words[1]) - 1];
                if (command == "CREATURE_COUNT")
                {
                    var creature = Find(words[2], "BadCreatures", "Heros"); var index = Creatures.ToList().FindIndex(item => item.ID == creature.ID);
                    player.Set($"CreaturesCount[{index}]", Number(words[3], -1)); continue;
                }
                var value = command switch { "NO" => 1, "MUST_RESEARCH" => 2, "START_WITH" => 3, _ => throw Error("Unknown scenario option: " + command) };
                var found = false;
                foreach (var (group, field) in new[] { ("VarRooms", "Rooms"), ("Traps", "Traps"), ("Doors", "Doors"), ("Spells", "Spells") })
                {
                    var index = Catalogues[group].ToList().FindIndex(item => string.Equals(item.AsciiName, words[2], StringComparison.OrdinalIgnoreCase));
                    if (index < 0) continue;
                    player.Set($"{field}[{index}]", value); found = true; break;
                }
                if (!found) throw Error("Unknown availability element: " + words[2]);
            }
        }

        private int NextEventID(bool action = false)
        {
            if (action) return ++actionId <= 32768 ? 65536 - actionId : throw Error("Too many actions for signed-short IDs.");
            return ++conditionId <= 32767 ? conditionId : throw Error("Too many conditions for signed-short IDs.");
        }
        private static byte EventType(string name) => LegacyEventSchemas.All.Single(type => type.Name == name).Type;
        private void Trigger()
        {
            var words = Words(); var command = words[0].ToUpperInvariant();
            var scope = command == "TRIGGER_CONTROL_AREA" ? 4 : 1; var target = 3;
            var condition = new Buffer("TRIGGER"); condition.Set("node.type", 0xD5); condition.Set("id", NextEventID()); condition.Set("repeat", 1); condition.Set("param1", 5);
            switch (command)
            {
                case "TRIGGER_CONTROL_AREA": Length(words, 3); target = Number(words[2], 1, 255); condition.Set("param2", Player(words[1])); condition.Set("ctrl", EventType("TRIGGER_CLAIMENTIREAREA")); break;
                case "TRIGGER_TIME": case "TRIGGER_SLAP_COUNT_EQUALS": Length(words, 2); condition.Set("param5", Number(words[1])); condition.Set("ctrl", EventType(command == "TRIGGER_TIME" ? "TRIGGER_TIMEELAPSED" : "TRIGGER_SLAPCOUNT")); break;
                default: throw Error("Unknown condition: " + command);
            }
            if (previous.TryGetValue((scope, target), out var last)) { last.Set("or", condition.Get("id")); condition.SetPrevious(last.Get("id")); }
            else
            {
                var link = new Buffer("TRIGGERLINK"); link.Set("node.type", scope); link.Set(scope == 4 ? "area" : "player", target); link.Set("trigger", condition.Get("id")); Add(links, link);
            }
            previous[(scope, target)] = condition; Add(conditions, condition);
            var count = Count(); Buffer? lastAction = null;
            for (var i = 0; i < count; i++)
            {
                var action = Action();
                if (lastAction is null) { condition.Set("and", action.Get("id")); action.SetPrevious(condition.Get("id")); }
                else { lastAction.Set("next", action.Get("id")); action.SetPrevious(lastAction.Get("id")); }
                Add(actions, action); lastAction = action;
            }
        }
        private Buffer Action()
        {
            var words = Words(); var command = words[0].ToUpperInvariant(); var action = new Buffer("ACTION");
            action.Set("node.type", 0xD6); action.Set("id", NextEventID(true)); action.Set("one", 1);
            if (command == "ACTION_DESTROY_HERO_GATE") { Length(words, 2); action.Set("ctrl", EventType("ACTION_DESTROYGATE")); action.Set("param5", Number(words[1], 1, 255)); }
            else if (command == "ACTION_HERO_RELEASE")
            {
                Length(words, 4); action.Set("ctrl", EventType("ACTION_RELEASEBAND")); action.Set("param1", Number(words[1], 0, 255));
                var id = Number(words[3], 1, 255);
                if (words[2].Equals("G", StringComparison.OrdinalIgnoreCase))
                {
                    var gate = rectangles.SingleOrDefault(rectangle => rectangle.Get("node.type") == 1 && rectangle.Get("id") == id) ?? throw Error("Release refers to a missing gate.");
                    var x = gate.Get("rect.x1"); var y = gate.Get("rect.y1");
                    var area = rectangles.FirstOrDefault(rectangle => rectangle.Get("node.type") == 0 && rectangle.Get("rect.x1") <= x && rectangle.Get("rect.x2") >= x && rectangle.Get("rect.y1") <= y && rectangle.Get("rect.y2") >= y);
                    if (area is not null) id = area.Get("id");
                    else
                    {
                        id = Enumerable.Range(1, 255).FirstOrDefault(candidate => !rectangles.Any(rectangle => rectangle.Get("node.type") == 0 && rectangle.Get("id") == candidate));
                        if (id == 0) throw Error("No free area ID for gate release.");
                        AddRectangle(0, id, x, y, x, y);
                    }
                }
                else if (!words[2].Equals("A", StringComparison.OrdinalIgnoreCase)) throw Error("Expected release target A or G.");
                action.Set("param5", id);
            }
            else
            {
                Length(words, 3);
                var (group, type) = command switch { "ACTION_ADD_ROOM" => ("VarRooms", 1), "ACTION_ADD_DOOR" => ("Doors", 3), "ACTION_ADD_TRAP" => ("Traps", 4), "ACTION_ADD_SPELL" => ("Spells", 5), _ => throw Error("Unknown action: " + command) };
                var element = Find(words[2], group); action.Set("ctrl", EventType("ACTION_ADDELEMENT")); action.Set("param1", Player(words[1]));
                action.Set("param2", type); action.Set("param3", type == 1 ? element.Variable : element.ID); action.Set("param4", 1);
            }
            return action;
        }
        private void VerifyReferences()
        {
            bool HasRectangle(int type, int id) => rectangles.Any(rectangle => rectangle.Get("node.type") == type && rectangle.Get("id") == id);
            if (links.Any(link => link.Get("node.type") == 4 && !HasRectangle(0, link.Get("area")))) throw Error("Condition refers to a missing area.");
            foreach (var action in actions)
            {
                if (action.Get("ctrl") == EventType("ACTION_DESTROYGATE") && !HasRectangle(1, action.Get("param5"))) throw Error("Destroy action refers to a missing gate.");
                if (action.Get("ctrl") == EventType("ACTION_RELEASEBAND") && (!HasRectangle(0, action.Get("param5")) || !bands.Any(band => band.Get("id") == action.Get("param1")))) throw Error("Release action refers to a missing area or band.");
            }
        }
        private static byte[] Join(IEnumerable<Buffer> records) => records.SelectMany(record => record.Bytes).ToArray();
        private static void Write(byte[] bytes, int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), value);
    }
}
