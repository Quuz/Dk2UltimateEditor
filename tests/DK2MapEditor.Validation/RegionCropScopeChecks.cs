using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

internal static class RegionCropScopeChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Refused(Action action)
    { try { action(); } catch (Exception error) when (error is InvalidOperationException or ArgumentException or InvalidDataException) { return; } throw new InvalidOperationException("A declined, stale or guarded crop was accepted."); }
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("RegionCrop", "RegionCrop", 24, 24, NativeFormat: native));
    private static string Read(NativeRecord row, string name) => row.Read(row.Fields.Single(field => field.Name == name));
    private static void Set(NativeMapDocument doc, NativeRecord row, string name, string value) => doc.SetField(row, row.Fields.Single(field => field.Name == name), value);
    private static Dictionary<string, byte[]> State(NativeMapDocument doc) => doc.IsLegacyProject ? doc.Files.ToDictionary(file => file.ExportPath, file => file.Data.ToArray())
        : doc.PrepareProjectConversion().SourceFiles.ToDictionary(file => file.ExportPath, file => file.Bytes.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<string, byte[]> before)
    { var after = State(doc); Expect(before.Count == after.Count && before.All(pair => after.TryGetValue(pair.Key, out var bytes) && bytes.AsSpan().SequenceEqual(pair.Value)), "Crop preview, rejection or Undo changed game/working buffers."); }
    private static NativeRecord Area(NativeMapDocument doc, string id) => doc.MapRegions().Single(region => region.Kind == MapRegionKind.Area && Read(region.Record, doc.IsLegacyProject ? "id" : "bAreaID") == id).Record;
    private static ScriptEdge Point(NativeMapDocument doc, NativeRecord row, bool nonempty)
    {
        var owner = doc.ScriptOwners().Single(owner => owner.Scope == 4 && owner.Record.File == row.File && owner.Record.Offset == row.Offset);
        var root = doc.EnsureScriptScope(owner);
        if (nonempty) doc.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        return doc.FindScriptScope(owner)!;
    }
    public static void Run()
    {
        var cases = 0;
        foreach (var native in new[] { false, true }) foreach (var kind in Enum.GetValues<MapRegionKind>())
            foreach (var empty in kind == MapRegionKind.Area ? new[] { false, true } : new[] { false })
                foreach (var anchor in new[] { 0, 4, 8 }) foreach (var position in Enumerable.Range(0, 3))
                { Matrix(native, kind, empty, anchor, position); cases++; }
        SaveAndReuse(); GenericContracts(); Batch();
        Console.WriteLine($"REGION CROP SCOPE DETAIL {cases} native/project x region kind/empty point x anchors x outside/fixed-boundary cases; retained points/all event bytes, no predecessor rewiring, immutable preview/declined crop, source parent counters, complete UndoRedo, DKM archive/save/reuse, seven-file native export, batch and unchanged generic resize policies");
    }
    private static void Matrix(bool native, MapRegionKind kind, bool empty, int anchor, int position)
    {
        var doc = New(native); var shift = -4 * (anchor % 3) / 2;
        var destinationX = position == 0 ? 0 : position == 1 ? 19 : anchor == 0 ? 20 : -1;
        var target = doc.AddMapRegion(kind, new(destinationX - shift, 8 - shift, position == 2 && kind != MapRegionKind.Gate ? 2 : 1, 1));
        var first = doc.AddMapRegion(MapRegionKind.Area, new(7, 8, 1, 1)); var second = doc.AddMapRegion(MapRegionKind.Area, new(11, 12, 1, 1));
        var idName = native ? "bAreaID" : "id"; var nextName = native ? "bAreaNextID" : "nextid";
        var firstID = Read(first, idName); var secondID = Read(second, idName);
        ushort root = 0;
        if (kind == MapRegionKind.Area)
        {
            Set(doc, target, nextName, secondID); Set(doc, first, nextName, "1"); Set(doc, second, nextName, "1");
            root = Point(doc, target, !empty).Target;
        }
        var entity = kind == MapRegionKind.Area ? EntityKind.Area : kind == MapRegionKind.Effect ? EntityKind.Effect : EntityKind.Gate;
        var rule = EntityReferences.EventRules.First(rule => rule.Kind == entity);
        var action = doc.AddScenarioEvent(rule.Event.RecordSignature, new(0, 0), rule.Event);
        if (native && kind == MapRegionKind.Gate) doc.ApplyScenarioEditorParameterDraft(doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(action), target.File.ExportPath + ":" + target.Offset));
        else Set(doc, action, native ? "Param" + rule.Parameter[5..] : rule.Parameter, "1");
        var events = doc.Files.Where(file => doc.IsLegacyProject ? doc.Records().Any(row => row.File == file && row.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)) || doc.Records().Any(row => row.File == file && ScriptGraph.IsEvent(row)) : ScriptGraph.Build(doc).Events.Values.Any(item => item.Record.File == file))
            .ToDictionary(file => file.ExportPath, file => file.Data.ToArray());
        var before = State(doc); var count = doc.EditorChangeCount; var revision = doc.EditRevision; var history = doc.GetHistory().Undo.Count;
        using (var cancel = doc.CreateMapPropertiesSession())
        {
            var plan = doc.PrepareEditorMapProperties(cancel, new(20, 20, anchor, RemoveOutside: true));
            Expect(plan.NeedsCropConsent && plan.Crops.Sum(row => row.Count) == 1, "Region crop preview counted a retained point or missed the boundary."); Exact(doc, before);
        }
        using var session = doc.CreateMapPropertiesSession(); var accepted = doc.PrepareEditorMapProperties(session, new(20, 20, anchor, RemoveOutside: true));
        Refused(() => doc.ApplyEditorMapProperties(accepted)); Exact(doc, before);
        Expect(doc.EditorChangeCount == count && doc.EditRevision == revision && doc.GetHistory().Undo.Count == history, "Declining crop changed counters/history.");
        var increments = shift == 0 ? 3u : 6u; // options + resize + three moved regions (when shifted) + one removed region
        Expect(doc.EditorMapPropertiesChangeIncrements(accepted) == increments, "Source crop added a point-removal or question counter.");
        var notifications = 0; EventHandler handler = (_, _) => notifications++; doc.Changed += handler;
        try { doc.ApplyEditorMapProperties(accepted, croppingAccepted: true); } finally { doc.Changed -= handler; }
        Expect(doc.EditorChangeCount == unchecked(count + increments) && doc.GetHistory().Undo.Count == history + 1 && notifications == 1, "Crop did not remain one parent operation.");
        Expect(doc.EditorRegionRemovalRecords().Count == 2, "Crop left its region or deleted a surviving predecessor.");
        if (kind == MapRegionKind.Area)
        {
            Expect(Read(Area(doc, firstID), nextName) == "1" && Read(Area(doc, secondID), nextName) == "1", "Properties crop incorrectly reused direct-delete predecessor rewiring.");
            var orphan = doc.ScriptBindings().Single(binding => binding.Root.Scope == 4 && binding.Owner is null).Root;
            Expect(orphan.Target == root && doc.HasStoredScriptScope(orphan), "Crop lost its empty/nonempty scenario point.");
        }
        foreach (var (path, bytes) in events)
        {
            var current = doc.Files.Single(file => file.ExportPath == path).Data.ToArray();
            if (native && kind == MapRegionKind.Gate)
            {
                var expected = bytes.ToArray(); BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(action.Offset + 12), 1); BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(action.Offset + 14), 1);
                Expect(current.AsSpan().SequenceEqual(expected), "Gate crop changed bytes beyond its established missing-target projection.");
            }
            else Expect(current.AsSpan().SequenceEqual(bytes), "Crop rewrote scenario operands, point records or events.");
        }
        var after = State(doc); doc.Undo(); Exact(doc, before); Expect(doc.EditorChangeCount == count, "Crop Undo lost the parent counter."); doc.Redo(); Exact(doc, after);
    }
    private static void SaveAndReuse()
    {
        foreach (var native in new[] { false, true }) foreach (var empty in new[] { false, true })
        {
            var doc = New(native); var target = doc.AddMapRegion(MapRegionKind.Area, new(22, 7, 1, 1)); Point(doc, target, !empty);
            using (var parent = doc.CreateMapPropertiesSession()) doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(parent, new(20, 20, RemoveOutside: true)), croppingAccepted: true);
            var directory = Path.Combine(Path.GetTempPath(), "DK2RegionCrop-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); var path = Path.Combine(directory, "Crop.dkm");
            using (var save = new EditorSaveSession(doc)) Expect(save.Save(save.PrepareSaveAs(path), acceptLosses: true), "Crop DKM save was refused.");
            var reopened = NativeMapDocument.Load(path); var orphan = reopened.ScriptBindings().Single(binding => binding.Root.Scope == 4 && binding.Owner is null).Root;
            Expect((orphan.Target == 0) == empty, "Crop DKM/archive Save/reopen lost point content.");
            var replacement = reopened.AddMapRegion(MapRegionKind.Area, new(5, 6, 1, 1));
            var owner = reopened.ScriptOwners().Single(owner => owner.Scope == 4 && owner.Record.Offset == replacement.Offset && owner.Record.File == replacement.File);
            Expect(reopened.FindScriptScope(owner)!.Target == orphan.Target, "First-free area ID reuse did not recover the cropped point.");
            var basis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
            var compiled = reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78", basis); var output = compiled.SaveCopy(Path.Combine(directory, "native"));
            Expect(Directory.GetFiles(Path.GetDirectoryName(output)!).Length == 7, "Crop metadata changed the seven-file game layout.");
        }
    }
    private static void GenericContracts()
    {
        foreach (var native in new[] { false, true })
        {
            var doc = New(native); var area = doc.AddMapRegion(MapRegionKind.Area, new(22, 7, 1, 1)); Point(doc, area, true);
            var predecessor = doc.AddMapRegion(MapRegionKind.Area, new(5, 6, 1, 1)); Set(doc, predecessor, native ? "bAreaNextID" : "nextid", "1");
            var before = State(doc);
            if (native) { Refused(() => doc.Resize(20, 20, 0, 0, new(1, 2, 1), removeOutside: true)); Exact(doc, before); }
            else
            {
                doc.Resize(20, 20, 0, 0, new(1, 2, 1), removeOutside: true);
                Expect(doc.ScriptBindings().All(binding => binding.Root.Scope != 4), "Generic historical resize no longer retains its separate owned-link deletion contract.");
                doc.Undo(); Exact(doc, before);
            }
        }
    }
    private static void Batch()
    {
        foreach (var native in new[] { false, true })
        {
            var doc = New(native);
            for (var index = 0; index < 3; index++) { var area = doc.AddMapRegion(MapRegionKind.Area, new(20 + index, 7, 1, 1)); Point(doc, area, index != 1); }
            var before = State(doc); var count = doc.EditorChangeCount; var history = doc.GetHistory().Undo.Count;
            using var session = doc.CreateMapPropertiesSession(); var plan = doc.PrepareEditorMapProperties(session, new(20, 20, RemoveOutside: true));
            Expect(plan.Crops.Sum(row => row.Count) == 3 && doc.EditorMapPropertiesChangeIncrements(plan) == 5, "Batch crop counted scenario points as extra placed removals.");
            doc.ApplyEditorMapProperties(plan, croppingAccepted: true);
            Expect(doc.ScriptBindings().Count(binding => binding.Root.Scope == 4 && binding.Owner is null) == 3 && doc.EditorChangeCount == unchecked(count + 5) && doc.GetHistory().Undo.Count == history + 1, "Batch crop lost a scope or its atomic parent count.");
            doc.Undo(); Exact(doc, before);
        }
    }
}
