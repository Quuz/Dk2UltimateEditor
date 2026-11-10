using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

internal static class ProjectGateRemovalChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static NativeMapDocument New() => NativeMapDocument.CreateNewMap(new("GateRemoval", "GateRemoval", 32, 30));
    private static string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    private static void Set(NativeMapDocument doc, NativeRecord record, string name, string value) => doc.SetField(record, record.Fields.Single(field => field.Name == name), value);
    private static NativeRecord Gate(NativeMapDocument doc, int x, int y) => doc.AddMapRegion(MapRegionKind.Gate, new(x, y, 1, 1));
    private static NativeRecord Action(NativeMapDocument doc, string id)
    {
        var action = doc.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(type => type.Name == "ACTION_DESTROYGATE"));
        Set(doc, action, "param5", id); Set(doc, action, "param2", "117"); Set(doc, action, "param4", "233"); Set(doc, action, "param6", "-12345");
        return action;
    }
    private static Dictionary<NativeFile, byte[]> Bytes(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> before) => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Gate removal/Undo lost complete project bytes.");
    private static void OnlyRemoved(Dictionary<NativeFile, byte[]> before, params NativeRecord[] removed)
    {
        foreach (var (file, bytes) in before)
        {
            var rows = removed.Where(record => record.File == file).OrderByDescending(record => record.Offset).ToArray();
            var expected = bytes.ToArray();
            foreach (var row in rows) expected = expected.Take(row.Offset).Concat(expected.Skip(row.Offset + row.Length)).ToArray();
            if (rows.Length != 0) BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(8), (uint)expected.Length - 12);
            Expect(file.Data.Span.SequenceEqual(expected), "Gate deletion rewrote a retained record/action or an unrelated section.");
        }
    }
    private static void Refused(Action operation)
    {
        try { operation(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException) { return; }
        throw new InvalidOperationException("An unrelated reference or foreign-document guard was bypassed.");
    }
    public static void Run()
    {
        RemoveAndRestore(); BatchAndGuards(); SaveReopenAndReuse();
        Console.WriteLine("PROJECT GATE REMOVAL DETAIL literal source gate deletion with retained param5/all action bytes, complete section/header oracle, one count/Undo/notification, batch/rollback/history0, other entity/script guards, warning/source1,1 compiler fallback, real missing-target DKM Save/reopen and first-free reuse through seven-file compiler export");
    }
    private static void RemoveAndRestore()
    {
        foreach (var index in new[] { 0, 1, 2 })
        {
            var doc = New(); var gates = new[] { Gate(doc, 3, 5), Gate(doc, 9, 11), Gate(doc, 15, 17) };
            var action = Action(doc, (index + 1).ToString()); _ = Action(doc, (index + 1).ToString());
            var before = Bytes(doc); var history = doc.GetHistory().Undo.Count; var count = doc.EditorChangeCount; var notifications = 0;
            doc.Changed += (_, _) => notifications++;
            doc.DeleteRecord(gates[index]); OnlyRemoved(before, gates[index]);
            Expect(doc.EditorChangeCount == unchecked(count + 1) && doc.GetHistory().Undo.Count == history + 1 && notifications == 1, "Gate deletion added an extra count/Undo/notification.");
            Expect(Read(action, "param5") == (index + 1).ToString(), "Deletion cleared its missing gate ID.");
            var after = Bytes(doc); var revision = doc.EditRevision;
            Expect(EntityReferenceDiagnostics.Validate(doc).Any(item => item.Code == "DK2N026" && item.Message.Contains("Missing Gate target " + (index + 1))), "The now-missing gate ID was not diagnosed.");
            Exact(after); Expect(doc.EditRevision == revision, "Missing-target diagnosis mutated history.");
            doc.Undo(); Exact(before); doc.Redo(); Exact(after);
            var fresh = Gate(doc, 20, 21); Expect(Read(fresh, "id") == (index + 1).ToString(), "Deleted gate ID was not the first free ID.");
            Expect(!EntityReferenceDiagnostics.Validate(doc).Any(item => item.Code == "DK2N026" && item.Message.Contains("Missing Gate target")), "ID reuse did not resolve retained action references.");
        }
    }
    private static void BatchAndGuards()
    {
        var doc = New(); var a = Gate(doc, 3, 5); var b = Gate(doc, 9, 11); _ = Action(doc, "1"); _ = Action(doc, "2");
        var before = Bytes(doc); var count = doc.EditorChangeCount; var history = doc.GetHistory().Undo.Count;
        doc.Edit("Remove selected gates", () => { doc.DeleteRecord(b); doc.DeleteRecord(a); }); OnlyRemoved(before, a, b);
        Expect(doc.EditorChangeCount == unchecked(count + 1) && doc.GetHistory().Undo.Count == history + 1, "Batch removal was not one outer operation."); doc.Undo(); Exact(before);
        try { doc.Edit("Failed gate removal", () => { doc.DeleteRecord(b); doc.DeleteRecord(a); throw new InvalidOperationException("fixture rollback"); }); }
        catch (InvalidOperationException error) when (error.Message == "fixture rollback") { }
        Exact(before); Expect(doc.EditorChangeCount == count, "Failed deletion advanced the counter.");
        doc.SetHistoryDepth(0); doc.DeleteRecord(a); OnlyRemoved(before, a); Expect(!doc.CanUndo && Read(doc.Records().First(record => record.Category == "Project.ACTION"), "param5") == "1", "History0 discarded the retained gate ID or kept Undo.");

        var guarded = New();
        foreach (var kind in new[] { EntityKind.Area, EntityKind.Band, EntityKind.Effect })
        {
            var owner = kind == EntityKind.Band ? guarded.AddLegacyRecord("Band", new(2, 2)) : guarded.AddMapRegion(kind == EntityKind.Area ? MapRegionKind.Area : MapRegionKind.Effect, new(2, 2, 1, 1));
            var rule = EntityReferences.EventRules.First(rule => rule.Kind == kind);
            var incoming = guarded.AddScenarioEvent(rule.Event.RecordSignature, new(0, 0), rule.Event);
            Set(guarded, incoming, rule.Parameter, Read(owner, "id")); var untouched = Bytes(guarded);
            Refused(() => guarded.DeleteRecord(owner)); Exact(untouched);
        }
        var action = Action(guarded, "1"); var condition = guarded.AddScenarioEvent(0xD5, new(0, 0), LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        Set(guarded, condition, "and", Read(action, "id")); var scripts = Bytes(guarded); Refused(() => guarded.DeleteRecord(action)); Exact(scripts);
        var foreign = Gate(New(), 4, 5); Refused(() => guarded.DeleteRecord(foreign)); Exact(scripts);
    }
    private static void SaveReopenAndReuse()
    {
        var doc = New(); var first = Gate(doc, 3, 5); _ = Gate(doc, 9, 11); _ = Action(doc, "1"); doc.DeleteRecord(first);
        var directory = Path.Combine(Path.GetTempPath(), "DK2ProjectGateRemoval", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "missing.dkm"); using (var save = new EditorProjectSaveSession(doc)) save.SaveAs(path);
        var reopened = NativeMapDocument.Load(path); var action = reopened.Records().Single(record => record.Category == "Project.ACTION");
        Expect(Read(action, "param5") == "1" && reopened.MapRegions().Where(region => region.Kind == MapRegionKind.Gate).Select(region => Read(region.Record, "id")).SequenceEqual(new[] { "2" }), "DKM Save/reopen cleared the absent ID or renumbered live gates.");
        var before = Bytes(reopened); var basis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
        var fallback = reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78", basis);
        foreach (var row in fallback.Records().Where(record => EntityReferences.EventType(record)?.Name == "ACTION_DESTROYGATE"))
            Expect(BinaryPrimitives.ReadUInt16LittleEndian(row.File.Data.Span.Slice(row.Offset + 12)) == 1 && BinaryPrimitives.ReadUInt16LittleEndian(row.File.Data.Span.Slice(row.Offset + 14)) == 1, "Compiler did not use the literal source fallback1,1 for an absent ID.");
        Exact(before);
        var copy = reopened.DuplicateRecord(action); Expect(Read(copy, "param5") == "1", "Cloned action forgot the missing gate ID.");
        _ = Gate(reopened, 15, 17);
        var compiled = reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78", basis);
        foreach (var row in compiled.Records().Where(record => EntityReferences.EventType(record)?.Name == "ACTION_DESTROYGATE"))
            Expect(BinaryPrimitives.ReadUInt16LittleEndian(row.File.Data.Span.Slice(row.Offset + 12)) == 16 && BinaryPrimitives.ReadUInt16LittleEndian(row.File.Data.Span.Slice(row.Offset + 14)) == 18, "Compiler did not resolve the reused param5 ID to literal 16,18.");
        var output = compiled.SaveCopy(Path.Combine(directory, "native")); Expect(Directory.GetFiles(Path.GetDirectoryName(output)!).Length == 7, "Gate removal changed the official seven-file format.");
    }
}
