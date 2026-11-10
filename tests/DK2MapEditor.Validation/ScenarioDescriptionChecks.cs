using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class ScenarioDescriptionChecks
{
    private sealed record Operand(int Number, int Offset, int Size, string Type);
    private sealed record Spec(string Type, byte ID, uint Signature, int[] Required, Operand[] Native);
    private static readonly Spec[] specs = ReadSpecs();
    private static Spec[] ReadSpecs()
    {
        using var stream = typeof(ScenarioDescriptionChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioDescriptionOperands.json")!;
        return JsonSerializer.Deserialize<Spec[]>(stream)!;
    }
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static byte[] Mutable(NativeRecord record)
    { Expect(MemoryMarshal.TryGetArray(record.File.Data, out var segment) && segment.Offset == 0, "No independent fixture buffer."); return segment.Array!; }
    private static NativeRecord Add(NativeMapDocument doc, bool native, Spec spec)
    {
        var type = LegacyEventSchemas.All.Single(type => type.Name == spec.Type);
        Expect(type.Type == spec.ID && type.RecordSignature == spec.Signature, "Active type differs from literal original catalogue.");
        var name = spec.Signature == 213 ? "Trigger" : "Action";
        var record = native ? doc.AddRecord(RecordTemplate.All.Single(template => template.Structure == name), new(0, 0)) : doc.AddLegacyRecord(name, new(0, 0));
        doc.SetEventType(record, type);
        record = doc.Records().Single(candidate => candidate.File == record.File && candidate.Offset == record.Offset && candidate.Length == record.Length);
        var values = Enumerable.Range(1, 8).ToDictionary(number => number, number => (long)number);
        values[1] = 3; values[2] = 2; values[3] = 1; values[4] = 4; values[5] = 5; values[7] = 6;
        if (spec.Type is "TRIGGER_VARIABLE" or "TRIGGER_TIMER") { values[2] = 7; values[5] = spec.Type == "TRIGGER_TIMER" ? 3661 : 42; }
        if (spec.Type == "ACTION_SETVARIABLE") { values[1] = 7; values[2] = 10; values[5] = 42; }
        if (spec.Type == "ACTION_SETTIMER") values[1] = 7;
        if (spec.Type is "TRIGGER_CREATURES" or "TRIGGER_HAPPYCREATURES" or "TRIGGER_ANGRYCREATURES") values[2] = 1;
        if (spec.Type is "TRIGGER_DOORS" or "TRIGGER_TRAPS" or "TRIGGER_DESTROYED" or "TRIGGER_SPELLS") values[2] = 4;
        if (spec.Type == "ACTION_ADDCREATURE") { values[1] = 1; values[2] = 3; values[3] = 4; }
        if (spec.Type == "ACTION_ADDELEMENT") { values[1] = 3; values[2] = 2; values[3] = 1; values[4] = 1; }
        if (spec.Type == "ACTION_SHOWELEMENT") { values[1] = 2; values[2] = 1; values[3] = 1; values[5] = 3661; }
        if (spec.Type == "ACTION_RELEASEBAND") { values[1] = 4; values[2] = 1; values[5] = 2; }
        if (spec.Type == "ACTION_CHANGEPARAMS") { values[1] = 3; values[2] = 11; }
        if (spec.Type == "ACTION_ALLIES") { values[1] = 3; values[2] = 4; values[3] = 0; }
        if (spec.Type is "ACTION_CHANGESQUARE" or "ACTION_CHANGEOWNER") { values[1] = 4; values[2] = 3; values[5] = 6; values[7] = 7; }
        if (spec.Type == "ACTION_GENERATE") { values[1] = 1; values[2] = 4; }
        if (spec.Type == "ACTION_DISPLAYTIMER") values[5] = 3661;
        if (native)
        {
            foreach (var operand in spec.Native) Write(record, operand.Offset, operand.Size, values[operand.Number]);
            if (spec.Type == "ACTION_DESTROYGATE") { Write(record, 12, 2, 6); Write(record, 14, 2, 7); }
        }
        else foreach (var number in values.Keys) Write(record, 16 + (number - 1) * 4, 4, values[number]);
        Write(record, native ? 23 : 55, 1, 1);
        return record;
    }
    private static void Write(NativeRecord record, int offset, int size, long value)
    {
        var bytes = Mutable(record).AsSpan(record.Offset + offset, size);
        if (size == 1) bytes[0] = unchecked((byte)value);
        else if (size == 2) BinaryPrimitives.WriteUInt16LittleEndian(bytes, unchecked((ushort)value));
        else BinaryPrimitives.WriteUInt32LittleEndian(bytes, unchecked((uint)value));
    }
    private static void Param(NativeRecord record, bool native, Spec spec, int number, long value)
    {
        var operand = native ? spec.Native.Single(item => item.Number == number) : new Operand(number, 16 + (number - 1) * 4, 4, "LONG");
        Write(record, operand.Offset, operand.Size, value);
    }
    private static string Text(NativeMapDocument doc, NativeRecord record, string language = "en")
    {
        var result = ScenarioDescriptions.Build(doc, language).Describe(record);
        Expect(result.Verified, "Unverified known event: " + EntityReferences.EventType(record)?.Name + " / " + result.Text);
        return result.Text;
    }
    private static void Contains(string text, string expected) => Expect(text.Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' in '" + text + "'.");
    private static void Exact(string actual, string expected) => Expect(actual == expected, "Expected '" + expected + "', got '" + actual + "'.");
    public static void Run()
    {
        Expect(specs.Length == 103 && specs.Count(spec => spec.Signature == 213) == 63 && specs.Count(spec => spec.Signature == 214) == 40, "Incomplete independent source matrix.");
        var baseline = new Dictionary<string, (string En, string Ru)>();
        foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("Descriptions", "Descriptions", 20, 20, NativeFormat: native));
            doc.SetScenarioName(ScenarioNameKind.Variable, 7, "Keep"); doc.SetScenarioName(ScenarioNameKind.Variable, 8, "Other");
            doc.SetScenarioName(ScenarioNameKind.Timer, 7, "Clock");
            var identities = specs.Select(spec => (Spec: spec, ID: ScriptGraph.ID(Add(doc, native, spec)))).ToArray();
            var records = ScriptGraph.Build(doc).Events;
            var bytes = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var revision = doc.EditRevision; var dirty = doc.HasChanges; var undo = doc.GetHistory().Undo.Count;
            var en = ScenarioDescriptions.Build(doc, "en"); var ru = ScenarioDescriptions.Build(doc, "ru");
            foreach (var (spec, id) in identities)
            {
                var record = records[id].Record; var english = en.Describe(record); var russian = ru.Describe(record);
                Expect(english.Verified && russian.Verified && english.Text.Length > 8 && russian.Text.Length > 8, "Missing semantic case " + spec.Type);
                Expect(!english.Text.Contains("TRIGGER_") && !english.Text.Contains("ACTION_") && !english.Text.Contains("sz") && !russian.Text.Contains("sz"), "Source symbol leaked: " + spec.Type);
                Expect(english.Text != russian.Text && russian.Text.Any(character => character is >= 'А' and <= 'я'), "Missing Russian sentence " + spec.Type);
                if (!native) baseline.Add(spec.Type, (english.Text, russian.Text));
                else if (spec.Type != "ACTION_DESTROYGATE")
                { Exact(english.Text, baseline[spec.Type].En); Exact(russian.Text, baseline[spec.Type].Ru); }
            }
            Expect(bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)) && doc.EditRevision == revision && doc.HasChanges == dirty && doc.GetHistory().Undo.Count == undo, "Read-only description projection changed bytes/dirty/history.");
            var seen = records[identities.Single(item => item.Spec.Type == "TRIGGER_SEE").ID].Record;
            Exact(en.Describe(seen).Text, "If the creature is attacked"); Exact(ru.Describe(seen).Text, "Если существо атаковано");
            var take = records[identities.Single(item => item.Spec.Type == "TRIGGER_TAKE").ID].Record;
            Exact(en.Describe(take).Text, "If the creature has picked something up");
            foreach (var type in new[] { "TRIGGER_VARIABLE", "TRIGGER_TIMER", "ACTION_SETVARIABLE", "ACTION_SETTIMER" })
                Aliases(doc, records[identities.Single(item => item.Spec.Type == type).ID].Record, native, specs.Single(spec => spec.Type == type));
            Branches(doc, records, identities.ToDictionary(item => item.Spec.Type, item => item.ID), native);
            var gate = records[identities.Single(item => item.Spec.Type == "ACTION_DESTROYGATE").ID].Record;
            if (!native) Exact(Text(doc, gate), "Destroy hero gate 5");
            else
            {
                Exact(Text(doc, gate), "Destroy hero gate at (5,6) (unresolved gate: 0 matches)");
                var added = doc.AddMapRegion(MapRegionKind.Gate, new(5, 6, 1, 1));
                doc.SetField(added, added.Fields.Single(field => field.Name == "bGateType"), "1");
                Exact(Text(doc, gate), "Destroy hero gate at (5,6)");
                added = doc.AddMapRegion(MapRegionKind.Gate, new(5, 6, 1, 1)); doc.SetField(added, added.Fields.Single(field => field.Name == "bGateType"), "1");
                Contains(Text(doc, gate), "unresolved gate: 2 matches");
            }
            var unknown = seen; Write(unknown, native ? 22 : 54, 1, 255); Write(unknown, native ? 23 : 55, 1, 255);
            var unknownResult = ScenarioDescriptions.Build(doc, "en").Describe(unknown);
            Expect(!unknownResult.Verified, "Unknown event was described as verified."); Contains(unknownResult.Text, "255"); Contains(unknownResult.Text, "repeat indefinitely");
            Expect(!ScenarioDescriptions.Build(doc, "en").Describe(new(unknown.File, int.MaxValue, 24, "Trigger", [])).Verified, "Foreign/unbounded record was accepted.");
            Reopen(doc, native);
        }
    }
    private static void Aliases(NativeMapDocument doc, NativeRecord record, bool native, Spec spec)
    {
        doc.SetScenarioName(ScenarioNameKind.Variable, 7, "Keep");
        if (spec.Type == "ACTION_SETTIMER")
        {
            Exact(Text(doc, record), "Initialize timer “Clock”");
            doc.SetScenarioName(ScenarioNameKind.Timer, 16, "Hidden"); Param(record, native, spec, 1, 16);
            Exact(Text(doc, record), "Initialize timer “Hidden”"); return;
        }
        if (spec.Type == "ACTION_SETVARIABLE")
        {
            var operations = new[] { (10, "Set variable “Keep” to 42"), (18, "Add 42 to variable “Keep”"), (34, "Subtract 42 from variable “Keep”"),
                (12, "Set variable “Keep” to variable “Other”"), (20, "Add variable “Other” to variable “Keep”"), (36, "Subtract variable “Other” from variable “Keep”") };
            foreach (var (mode, expected) in operations)
            {
                Param(record, native, spec, 2, mode); Param(record, native, spec, 5, mode is 12 or 20 or 36 ? 8 : 42); Exact(Text(doc, record), expected);
                if (mode is 12 or 20 or 36) { Param(record, native, spec, 5, 128); Contains(Text(doc, record), "score"); }
            }
            Param(record, native, spec, 1, 128); Contains(Text(doc, record), "score");
            Param(record, native, spec, 2, 99); Contains(Text(doc, record), "Unknown variable operation 99");
            Param(record, native, spec, 2, 10); Param(record, native, spec, 5, -1); Contains(Text(doc, record), "-1"); return;
        }
        var compareEn = new[] { "less than", "at most", "equal to", "greater than", "at least", "not equal to" };
        for (var comparison = 1; comparison <= 6; comparison++)
        { Param(record, native, spec, 1, comparison); Contains(Text(doc, record), compareEn[comparison - 1]); }
        Param(record, native, spec, 1, 99); Contains(Text(doc, record), "comparison (99)"); Param(record, native, spec, 1, 3);
        for (var repeat = 0; repeat <= 255; repeat++)
        {
            Write(record, native ? 23 : 55, 1, repeat); var description = Text(doc, record);
            if (repeat == 255) Contains(description, "repeat indefinitely"); else if (repeat > 1) Contains(description, "repeat " + repeat + " times");
            else Expect(!description.Contains("repeat"), "Unexpected zero/one repeat suffix.");
        }
        Write(record, native ? 23 : 55, 1, 1);
        if (spec.Type == "TRIGGER_TIMER")
        {
            Exact(Text(doc, record), "If timer “Clock” is equal to 1 hour 1 minute 1 second");
            var durations = new[] { (0L, "0 seconds", "0 секундам"), (1L, "1 second", "1 секунде"), (2L, "2 seconds", "2 секундам"),
                (11L, "11 seconds", "11 секундам"), (21L, "21 seconds", "21 секунде"), (-1L, "-1 second", "-1 секунде"),
                (2147483647L, "596523 hours 14 minutes 7 seconds", "596523 часам 14 минутам 7 секундам"), (-2147483648L, "-596523 hours -14 minutes -8 seconds", "-596523 часам -14 минутам -8 секундам") };
            foreach (var (value, english, russian) in durations)
            { Param(record, native, spec, 5, value); Contains(Text(doc, record), english); Contains(Text(doc, record, "ru"), russian); }
            Param(record, native, spec, 2, 16); Contains(Text(doc, record), "time limit"); return;
        }
        Exact(Text(doc, record), "If variable “Keep” is equal to 42"); Exact(Text(doc, record, "ru"), "Если переменная «Keep» равна 42");
        Param(record, native, spec, 3, 2); Param(record, native, spec, 4, 8); Exact(Text(doc, record), "If variable “Keep” is equal to variable “Other”");
        Param(record, native, spec, 4, 128); Exact(Text(doc, record, "ru"), "Если переменная «Keep» равна счёту");
        Param(record, native, spec, 3, 1); Param(record, native, spec, 5, int.MinValue); Contains(Text(doc, record), "-2147483648");
        var old = ScenarioDescriptions.Build(doc, "en"); doc.SetScenarioName(ScenarioNameKind.Variable, 7, "{0}%s%n\"tail");
        Contains(old.Describe(record).Text, "Keep"); Contains(Text(doc, record), "{0}%s%n\"tail");
        Param(record, native, spec, 2, 0); doc.SetScenarioName(ScenarioNameKind.Variable, 0, "q");
        Contains(Text(doc, record), "variable 0"); Expect(!Text(doc, record).Contains("“q”"), "Original slot-zero numeric-length quirk changed.");
        doc.SetScenarioName(ScenarioNameKind.Variable, 0, "00"); Contains(Text(doc, record), "“00”");
        Param(record, native, spec, 2, 128); Contains(Text(doc, record), "score");
        Param(record, native, spec, 2, 255); Contains(Text(doc, record), "variable (255)");
    }
    private static void Branches(NativeMapDocument doc, IReadOnlyDictionary<ushort, ScriptEvent> records, Dictionary<string, ushort> ids, bool native)
    {
        foreach (var (type, expected) in new[] { ("TRIGGER_CREATURES", "creatures of type “Imp”"), ("TRIGGER_HAPPYCREATURES", "happy creatures"),
            ("TRIGGER_ROOMSTYPES", "rooms of type"), ("TRIGGER_DOORS", "door blueprints (built or waiting)"), ("TRIGGER_TRAPS", "trap blueprints (built or waiting)"),
            ("ACTION_SAY", "do not display text"), ("ACTION_ALLIES", "Establish alliance"), ("ACTION_DISPLAYTIMER", "1 hour 1 minute 1 second") }) Contains(Text(doc, records[ids[type]].Record), expected);
        foreach (var (type, parameter, phrase) in new[] { ("ACTION_DISPLAYTIMER", 5, "Disable the time limit"), ("ACTION_SHOWNEXTROOM", 1, "Disable the next-room display"),
            ("ACTION_SLAPSREMAINING", 5, "Disable the remaining-slaps display"), ("ACTION_SAY", 5, "Say speech"), ("ACTION_SHOWELEMENT", 3, "Disable highlighting") })
        { var record = records[ids[type]].Record; Param(record, native, specs.Single(spec => spec.Type == type), parameter, 0); Contains(Text(doc, record), phrase); }
        var change = records[ids["ACTION_CHANGEPARAMS"]].Record; var changeSpec = specs.Single(spec => spec.Type == "ACTION_CHANGEPARAMS");
        foreach (var (mode, phrase) in new[] { (11, "destroy rooms"), (12, "destroy walls"), (13, "steal gold"), (14, "steal spells"), (17, "steal crates"),
            (18, "kill creatures"), (19, "attack"), (22, "stay in place"), (23, "go to area 5"), (27, "attempt an escape"), (99, "creature order (99)") })
        { Param(change, native, changeSpec, 2, mode); Contains(Text(doc, change), phrase); }
        var target = records[ids["ACTION_PRIMARYTARGET"]].Record; var targetSpec = specs.Single(spec => spec.Type == "ACTION_PRIMARYTARGET");
        foreach (var (mode, phrase) in new[] { (0, "Disable"), (1, "(kill)"), (2, "(imprison)"), (3, "(convert)"), (99, "(kill)") })
        { Param(target, native, targetSpec, 1, mode); Contains(Text(doc, target), phrase); }
        var destroyed = records[ids["TRIGGER_DESTROYED"]].Record; var destroyedSpec = specs.Single(spec => spec.Type == "TRIGGER_DESTROYED");
        Param(destroyed, native, destroyedSpec, 2, 0); Contains(Text(doc, destroyed), "doors or traps"); Param(destroyed, native, destroyedSpec, 2, 4); Contains(Text(doc, destroyed), "doors/traps with ID 4");
        var click = records[ids["TRIGGER_CLICKELEMENT"]].Record; var clickSpec = specs.Single(spec => spec.Type == "TRIGGER_CLICKELEMENT");
        foreach (var (kind, value, phrase) in new[] { (0, 241, "hero invasion"), (1, 3, "Portal"), (1, 5, "Dungeon heart"), (2, 1, "Imp"),
            (3, 6, "door"), (4, 4, "trap"), (5, 4, "spell"), (99, 255, "element kind (99)") })
        { Param(click, native, clickSpec, 1, kind); Param(click, native, clickSpec, 2, value); Contains(Text(doc, click), phrase); }
        foreach (var (kind, group) in new[] { (2, "TrCreatures"), (3, "Doors"), (4, "Traps"), (5, "Spells") })
            foreach (var item in LegacyCompilerRules.Elements(group))
            {
                Param(click, native, clickSpec, 1, kind); Param(click, native, clickSpec, 2, item.ID);
                var choice = ValueCatalogues.Named(item, item.ID.ToString(CultureInfo.InvariantCulture));
                Contains(Text(doc, click), choice.English); Contains(Text(doc, click, "ru"), choice.Russian);
            }
        foreach (var item in LegacyCompilerRules.Elements("VarRooms"))
        {
            Param(click, native, clickSpec, 1, 1); Param(click, native, clickSpec, 2, item.Variable);
            var choice = ValueCatalogues.Named(item, item.Variable.ToString(CultureInfo.InvariantCulture));
            Contains(Text(doc, click), choice.English); Contains(Text(doc, click, "ru"), choice.Russian);
        }
        foreach (var item in LegacyCompilerRules.Elements("Interface"))
        { Param(click, native, clickSpec, 1, 0); Param(click, native, clickSpec, 2, item.ID); Expect(!Text(doc, click).Contains("sz"), "Interface source symbol leaked."); }
        var terrain = records[ids["TRIGGER_AREACONTENT"]].Record; var terrainSpec = specs.Single(spec => spec.Type == "TRIGGER_AREACONTENT");
        foreach (var item in LegacyCompilerRules.Elements("Terrain"))
        { Param(terrain, native, terrainSpec, 3, item.ID >> 24); Expect(!Text(doc, terrain).Contains("Unknown terrain") && !Text(doc, terrain, "ru").Contains("Неизвестное значение: тип земли"), "Source terrain vocabulary missing."); }
        foreach (var type in new[] { "ACTION_RUN", "ACTION_ATTACK", "ACTION_PORTALS", "ACTION_CHANGEVIEW", "ACTION_MOODS", "ACTION_SYSTEMMESSAGES", "ACTION_TIMERSPEECH",
            "ACTION_ADDELEMENT", "ACTION_ALLIES", "ACTION_SHOWAREA", "ACTION_ENABLEFOG", "ACTION_CAMERAPATH" })
        {
            var record = records[ids[type]].Record; var spec = specs.Single(spec => spec.Type == type);
            var parameter = type switch { "ACTION_ADDELEMENT" => 4, "ACTION_ALLIES" or "ACTION_CAMERAPATH" => 3, "ACTION_SHOWAREA" or "ACTION_ENABLEFOG" => 2, _ => 1 };
            Param(record, native, spec, parameter, 0); var no = Text(doc, record); var noRu = Text(doc, record, "ru");
            Param(record, native, spec, parameter, 1); var yes = Text(doc, record); var yesRu = Text(doc, record, "ru");
            Expect(no != yes && noRu != yesRu, "Source boolean branches collapsed: " + type);
            Param(record, native, spec, parameter, 255); Exact(Text(doc, record), yes); Exact(Text(doc, record, "ru"), yesRu);
        }
        foreach (var type in new[] { "TRIGGER_CREATURESLEVEL", "TRIGGER_LOSTCREATURES" })
        {
            if (native) continue; // Source deliberately omits this operand from the native compiler.
            var record = records[ids[type]].Record; var before = Text(doc, record); var spec = specs.Single(spec => spec.Type == type);
            Param(record, false, spec, 2, int.MinValue); Exact(Text(doc, record), before);
        }
    }
    private static void Reopen(NativeMapDocument doc, bool native)
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2ScenarioDescriptions", Guid.NewGuid().ToString("N"));
        var input = doc.SaveCopy(directory); var opened = NativeMapDocument.Load(input);
        var bytes = opened.Files.ToDictionary(file => file, file => file.Data.ToArray()); var dirty = opened.HasChanges;
        var en = ScenarioDescriptions.Build(opened, "en"); var ru = ScenarioDescriptions.Build(opened, "ru");
        Expect(bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)) && opened.HasChanges == dirty && opened.GetHistory().Undo.Count == 0, "Reopened description navigation modified native/project data.");
        if (native)
        {
            var companion = input + NativeMapDocument.ScenarioNamesExtension; var data = File.ReadAllBytes(companion); BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 99); File.WriteAllBytes(companion, data);
            opened = NativeMapDocument.Load(input); var names = File.ReadAllBytes(companion); var record = opened.Records().Single(record => EntityReferences.EventType(record)?.Name == "ACTION_SETTIMER");
            Contains(Text(opened, record), "name metadata unavailable"); Expect(File.ReadAllBytes(companion).AsSpan().SequenceEqual(names), "Future name metadata changed while rendering.");
        }
    }
}
