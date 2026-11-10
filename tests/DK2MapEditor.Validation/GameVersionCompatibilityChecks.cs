using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class GameVersionCompatibilityChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("Version", "Version", 24, 24, Version: 13, NativeFormat: native));
    private static void Target(NativeMapDocument doc, int version)
    { var draft = doc.CreateMapGeneralDraft(); doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, MapGeneralSettingsChecks.Inputs(draft, "Version", version.ToString(CultureInfo.InvariantCulture)))); }
    private static void Set(NativeMapDocument doc, NativeRecord record, string field, string value) => doc.SetField(record, record.Fields.Single(item => item.Name == field), value);
    private static NativeRecord Event(NativeMapDocument doc, string name, byte expectedType)
    {
        var type = LegacyEventSchemas.All.Single(item => item.Name == name); Expect(type.Type == expectedType, "Independent original event ID mismatch.");
        var record = doc.IsLegacyProject ? doc.AddLegacyRecord(type.RecordSignature == 0xD5 ? "Trigger" : "Action", new(0, 0)) : doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0));
        doc.SetEventType(record, type); return doc.Records().Single(item => item.File == record.File && item.Offset == record.Offset);
    }
    private static NativeRecord Thing(NativeMapDocument doc, string kind, int id, byte owner = 3)
    {
        var structure = kind switch { "Creature" => owner == 1 ? "GAMEHERO" : owner == 2 ? "GAMENEUTRAL" : "GAMEKEEPER", "Trap" => "GAMETRAP", "Door" => "GAMEDOOR", _ => "GAMEOBJECT" };
        var record = doc.IsLegacyProject ? doc.AddLegacyRecord(kind, new(8, 9), owner) : doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == structure), new(8, 9), owner);
        Set(doc, record, doc.IsLegacyProject ? "id" : kind == "Creature" ? "bGlobalCreatureID" : kind == "Trap" ? "bTrapID" : kind == "Door" ? "bDoorID" : "bObjectID", id.ToString(CultureInfo.InvariantCulture)); return record;
    }
    private static string[] Codes(NativeMapDocument doc, bool scripts = true) => GameVersionCompatibility.Validate(doc, scripts).Select(row => row.Code).ToArray();
    private static void Check(NativeMapDocument doc, params string[] expected)
    {
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var revision = doc.EditRevision; var dirty = doc.HasChanges; var history = doc.GetHistory();
        var diagnostics = GameVersionCompatibility.Validate(doc); Expect(diagnostics.Select(row => row.Code).SequenceEqual(expected), "Version notices differ: " + string.Join(',', diagnostics.Select(row => row.Code)) + " expected " + string.Join(',', expected));
        Expect(diagnostics.All(row => row.Severity == DiagnosticSeverity.Information), "Source compatibility notice became a warning/error.");
        Expect(NativeMapValidator.Validate(doc).Where(row => row.Code.StartsWith("DK2V", StringComparison.Ordinal)).SequenceEqual(diagnostics), "Ordinary validator omitted version notices.");
        Expect(revision == doc.EditRevision && dirty == doc.HasChanges && history.Undo.Count == doc.GetHistory().Undo.Count && history.Redo.Count == doc.GetHistory().Redo.Count && before.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Version diagnostics mutated document/history.");
    }
    public static string ReadOnlyProjection(NativeMapDocument doc) => JsonSerializer.Serialize(GameVersionCompatibility.Validate(doc).Select(row => new { row.Severity, row.Code, row.Message, row.Region }));
    public static void Run()
    {
        foreach (var native in new[] { false, true }) { Conditions(native); Actions(native); Things(native); Aggregates(native); Unknown(native); }
        CompileAndReload();
    }
    private static void Conditions(bool native)
    {
        foreach (var (name, type) in new[] { ("TRIGGER_CREAENTERAREA", (byte)0x27), ("TRIGGER_HAPPYCREATURES", (byte)0x14), ("TRIGGER_LOSTCREATURES", (byte)0x16), ("TRIGGER_CREATURESLEVEL", (byte)0x34), ("TRIGGER_GROUPINPOSSESS", (byte)0x49), ("TRIGGER_CREATURES", (byte)0x13), ("TRIGGER_TRAPS", (byte)0x1C) })
        {
            var doc = New(native); var record = Event(doc, name, type); var field = record.Fields.SingleOrDefault(item => item.Name == (native ? "Param2" : "param2"));
            if (native && type is 0x16 or 0x34)
            {
                Expect(field is null, "Native lost/level condition fabricated param2.");
                Expect(MemoryMarshal.TryGetArray(record.File.Data, out var memory), "Native reserved fixture backing."); memory.Array![memory.Offset + record.Offset + 9] = 0x2D;
                Check(doc); continue;
            }
            Expect(field is not null && field.Offset == (native ? 9 : 20) && field.Size == (native ? 1 : 4), "Independent species operand location mismatch.");
            foreach (var species in new[] { 0, 0x10, 0x1F, 0x20, 0x21, 0x2B, 0x2C, 0x2D, 0x2E, 0xFF })
            {
                Set(doc, record, field!.Name, species.ToString(CultureInfo.InvariantCulture));
                foreach (var target in new[] { 13, 16, 17 })
                {
                    Target(doc, target);
                    var need61 = type == 0x1C ? species == 0x10 : type != 0x13 && species is >= 0x20 and <= 0x2C;
                    var need70 = type != 0x1C && species == 0x2D;
                    Check(doc, target < 16 && need61 ? ["DK2V002"] : target < 17 && need70 ? ["DK2V005"] : []);
                    Expect(Codes(doc, false).Length == 0, "Explicit no-scenario validation included script notices.");
                }
            }
        }
    }
    private static void Actions(bool native)
    {
        var doc = New(native); var record = Event(doc, "ACTION_ADDCREATURE", 0x01); var field = record.Fields.Single(item => item.Name == (native ? "Param1" : "param1"));
        Expect(field.Offset == (native ? 8 : 16) && field.Size == (native ? 1 : 4), "Independent ADDCREATURE layout mismatch.");
        foreach (var id in Enumerable.Range(0x1F, 16))
        {
            Set(doc, record, field.Name, id.ToString(CultureInfo.InvariantCulture)); foreach (var target in new[] { 13, 16, 17 })
            { Target(doc, target); Check(doc, target < 16 && id is >= 0x20 and <= 0x2C ? ["DK2V003"] : target < 17 && id == 0x2D ? ["DK2V006"] : []); }
        }
        var other = Event(doc, "ACTION_ALLIES", 0x12); Set(doc, other, native ? "Param1" : "param1", "45"); Set(doc, record, field.Name, "31"); Target(doc, 13); Check(doc);
    }
    private static void Things(bool native)
    {
        foreach (var owner in new byte[] { 1, 2, 3 })
        {
            var doc = New(native); var creature = Thing(doc, "Creature", 0x20, owner);
            foreach (var id in Enumerable.Range(0x1F, 16))
            {
                Set(doc, creature, native ? "bGlobalCreatureID" : "id", id.ToString(CultureInfo.InvariantCulture)); foreach (var target in new[] { 13, 16, 17 })
                { Target(doc, target); Check(doc, target < 16 && id is >= 0x20 and <= 0x2C ? ["DK2V001"] : target < 17 && id == 0x2D ? ["DK2V004"] : []); }
            }
        }
        var traps = New(native); var trap = Thing(traps, "Trap", 0x10); Check(traps, "DK2V004"); Target(traps, 16); Check(traps, "DK2V004"); Target(traps, 17); Check(traps); Set(traps, trap, native ? "bTrapID" : "id", "15"); Target(traps, 13); Check(traps);
        var objects = New(native); foreach (var id in new[] { 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x91, 0x8A }) Thing(objects, "Object", id);
        Check(objects, Enumerable.Repeat("DK2V007", 7).ToArray()); var notices = GameVersionCompatibility.Validate(objects); Expect(notices.All(row => row.Region == new MapDiagnosticRegion(8, 9, 1, 1)), "Per-object notices lost coordinates/duplicates."); Target(objects, 17); Check(objects);
        var band = objects.IsLegacyProject ? objects.AddLegacyRecord("Band", new(8, 9)) : objects.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEBAND"), new(8, 9));
        objects.AddBandMember(band, 0x2D); Target(objects, 13); Check(objects, Enumerable.Repeat("DK2V007", 7).ToArray());
        var doors = New(native); Thing(doors, "Door", 0x2D); Check(doors);
    }
    private static void Aggregates(bool native)
    {
        var doc = New(native); Thing(doc, "Creature", 0x20); Thing(doc, "Creature", 0x2D); Thing(doc, "Trap", 0x10); Thing(doc, "Creature", 0x20);
        var condition61 = Event(doc, "TRIGGER_HAPPYCREATURES", 0x14); Set(doc, condition61, native ? "Param2" : "param2", "32");
        var condition70 = Event(doc, "TRIGGER_CREATURES", 0x13); Set(doc, condition70, native ? "Param2" : "param2", "45");
        var action61 = Event(doc, "ACTION_ADDCREATURE", 0x01); Set(doc, action61, native ? "Param1" : "param1", "32");
        var action70 = Event(doc, "ACTION_ADDCREATURE", 0x01); Set(doc, action70, native ? "Param1" : "param1", "45"); Thing(doc, "Object", 0x90);
        Check(doc, "DK2V001", "DK2V002", "DK2V003", "DK2V004", "DK2V005", "DK2V006", "DK2V007");
        Expect(Codes(doc, false).SequenceEqual(new[] { "DK2V001", "DK2V004", "DK2V007" }), "No-scenario mode removed placed notices or included script notices.");
        Target(doc, 16); Check(doc, "DK2V004", "DK2V005", "DK2V006", "DK2V007"); Target(doc, 17); Check(doc);
    }
    private static void Unknown(bool native)
    {
        var doc = New(native); var record = Thing(doc, "Creature", 0x2D); var file = record.File;
        Expect(MemoryMarshal.TryGetArray(file.Data, out var bytes), "Unknown-layout fixture backing."); BinaryPrimitives.WriteUInt32LittleEndian(bytes.Array!.AsSpan(bytes.Offset + 4), 99);
        Expect(GameVersionCompatibility.Validate(doc).Count == 0, "Unknown physical layout inferred requirements.");
        var events = New(native); var action = Event(events, "ACTION_ADDCREATURE", 0x01); Set(events, action, native ? "Param1" : "param1", "45");
        Expect(MemoryMarshal.TryGetArray(action.File.Data, out bytes), "Unknown-event fixture backing."); BinaryPrimitives.WriteUInt32LittleEndian(bytes.Array!.AsSpan(bytes.Offset + 4), 99);
        Expect(GameVersionCompatibility.Validate(events).Count == 0, "Unknown event layout inferred requirements.");
    }
    private static void CompileAndReload()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2VersionCompatibility", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var project = New(false); Thing(project, "Creature", 0x2D); Thing(project, "Object", 0x90);
        var before = ReadOnlyProjection(project); Expect(NativeMapValidator.Validate(project).All(row => row.Severity != DiagnosticSeverity.Error), "Otherwise-valid old-target compiler fixture invalid.");
        var native = project.CompileLegacyProject("VersionCompiled", NativeMapDocument.Create(24, 24, "Basis")); Expect(native.TargetGameVersion == 13 && BinaryPrimitives.ReadUInt32LittleEndian(native.MainFile.Data.Span[4..]) == 4, "Non-failing notices blocked old-target compilation or changed container version."); Check(native, "DK2V004", "DK2V007");
        Expect(before == ReadOnlyProjection(project), "Compile mutated original compatibility state.");
        var saved = native.SaveCopy(Path.Combine(directory, "compiled")); var reopened = NativeMapDocument.Load(saved); Expect(reopened.TargetGameVersion == 17, "Original native reload target differs."); Check(reopened);
    }
}
