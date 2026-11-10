using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class UnusedScriptChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("Unused", "Unused", 24, 20, NativeFormat: native));
    private static ushort Add(NativeMapDocument doc, string name)
    {
        var type = LegacyEventSchemas.All.Single(item => item.Name == name);
        var record = doc.IsLegacyProject ? doc.AddLegacyRecord(type.RecordSignature == 0xD5 ? "Trigger" : "Action", new(0, 0))
            : doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0));
        doc.SetEventType(record, type); return ScriptGraph.ID(record);
    }
    private static void Set(NativeMapDocument doc, NativeRecord record, string name, string value) => doc.SetField(record, record.Fields.Single(field => field.Name == name), value);
    private static void Set(NativeMapDocument doc, ushort id, string name, string value) => Set(doc, ScriptGraph.Build(doc).Events[id].Record, name, value);
    private static void Raw(NativeFile file, int offset, ushort value)
    { Expect(MemoryMarshal.TryGetArray(file.Data, out var buffer), "No fixture backing array."); BinaryPrimitives.WriteUInt16LittleEndian(buffer.Array!.AsSpan(buffer.Offset + offset), value); }
    private static ScriptEdge Root(NativeMapDocument doc) => doc.EnsureScriptScope(doc.ScriptOwners().Single(item => item.Scope == 32));
    private static void Link(NativeMapDocument doc, ushort id, ushort target, bool child = false)
    {
        var record = ScriptGraph.Build(doc).Events[id].Record;
        var field = ScriptGraph.Edges(record).Single(edge => child ? edge.Field.Name is "and" or "ChildAnd" : edge.Field.Name is "or" or "next" or "NextOr");
        doc.SetField(record, field.Field, target.ToString());
    }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<NativeFile, byte[]> bytes, string message) => Expect(doc.Files.All(file => file.Data.Span.SequenceEqual(bytes[file])), message);
    private static byte[] EventBytes(ScriptEvent item) => item.Record.File.Data.Slice(item.Record.Offset, item.Record.Length).ToArray();
    private static void Reject(Action action, string message) { var rejected = false; try { action(); } catch (InvalidOperationException) { rejected = true; } Expect(rejected, message); }
    public static void Run()
    {
        foreach (var native in new[] { false, true }) { SharedAndAtomic(native); Guards(native); Layouts(native); Semantics(native); SaveCompile(native); }
        ProjectScopes(); NativePayloadSizes(); DeepGraph();
    }
    public static string ReadOnlyProjection(NativeMapDocument doc)
    {
        var revision = doc.EditRevision; var history = doc.GetHistory();
        var hashes = doc.Files.ToDictionary(file => file, file => System.Security.Cryptography.SHA256.HashData(file.Data.Span));
        var plan = doc.PlanUnusedScriptRemoval();
        Expect(revision == doc.EditRevision && history.Undo.Count == doc.GetHistory().Undo.Count && history.Redo.Count == doc.GetHistory().Redo.Count
            && doc.Files.All(file => System.Security.Cryptography.SHA256.HashData(file.Data.Span).AsSpan().SequenceEqual(hashes[file])), "Loaded-map preview modified bytes/history.");
        if (plan.CanApply)
        {
            // Independent FIFO traversal starts from every stored root, rather
            // than the implementation's depth-first color/exit stack.
            var graph = ScriptGraph.Build(doc); var seen = new HashSet<ushort>(); var queue = new Queue<ushort>(graph.Roots.Select(root => root.Target));
            while (queue.TryDequeue(out var id))
                if (id != 0 && seen.Add(id)) foreach (var edge in graph.Events[id].Edges) queue.Enqueue(edge.Target);
            Expect(plan.ReachableCount == seen.Count && plan.Events.Select(item => item.ID).ToHashSet().SetEquals(graph.Events.Keys.Except(seen)), "Loaded-map candidate set differs from all-root traversal.");
        }
        return plan.CanApply + ":" + plan.ReachableCount + ":" + string.Join("|", plan.Events.Select(item => item.ID + ":" + item.Condition + ":" + item.TypeName))
            + ":" + string.Join("|", plan.Issues.Select(item => item.Problem + ":" + item.EventID + ":" + item.Detail));
    }
    private static void SharedAndAtomic(bool native)
    {
        var doc = New(native); var root = Root(doc);
        var a = Add(doc, "TRIGGER_TIMEELAPSED"); var b = Add(doc, "TRIGGER_TIMER"); var shared = Add(doc, "ACTION_ALLIES");
        var unusedCondition = Add(doc, "TRIGGER_TIMEELAPSED"); var unusedAction = Add(doc, "ACTION_ALLIES"); var dangling = Add(doc, "TRIGGER_TIMER");
        Set(doc, root.Owner, root.Field.Name, a.ToString()); Link(doc, a, b);
        Link(doc, a, shared, true); Link(doc, b, shared, true);
        Link(doc, unusedCondition, unusedAction); Link(doc, unusedAction, unusedCondition); Link(doc, dangling, 32000);
        if (native) Set(doc, shared, "ChildAnd", unusedAction.ToString());
        var before = Snapshot(doc); var graph = ScriptGraph.Build(doc); var retained = graph.Reachable(a).ToDictionary(id => id, id => EventBytes(graph.Events[id]));
        var eventFile = graph.Events[a].Record.File;
        var history = doc.GetHistory().Undo.Count; var revision = doc.EditRevision; var changes = 0; doc.Changed += (_, _) => changes++;
        var plan = doc.PlanUnusedScriptRemoval();
        Expect(plan.CanApply && plan.Conditions == 2 && plan.Actions == 1 && plan.ReachableCount == 3, "Shared classification wrong: " + native + " C=" + plan.Conditions + " A=" + plan.Actions + " reachable=" + plan.ReachableCount + " issues=" + string.Join(";", plan.Issues));
        Expect(plan.Events.Select(item => item.ID).ToHashSet().SetEquals(new[] { unusedCondition, unusedAction, dangling }), "Wrong unused identities.");
        Exact(doc, before, "Preview changed bytes."); Expect(changes == 0 && revision == doc.EditRevision, "Preview changed lifetime.");
        Expect(plan.Events is not UnusedScriptEvent[] && plan.Issues is not List<UnusedScriptIssue>, "Mutable preview exposed.");
        doc.ApplyUnusedScriptRemoval(plan); var after = Snapshot(doc); var current = ScriptGraph.Build(doc);
        Expect(current.Events.Count == 3 && retained.All(pair => EventBytes(current.Events[pair.Key]).AsSpan().SequenceEqual(pair.Value)), "Retained payload/link/backlink changed.");
        Expect(doc.Files.Where(file => file != eventFile).All(file => file.Data.Span.SequenceEqual(before[file])), "Cleanup modified unrelated files.");
        if (!native)
        {
            Expect(eventFile.Data.Span[^ (146 * 64)..].SequenceEqual(before[eventFile].AsSpan()[^ (146 * 64)..]), "Project alias tail was reordered/damaged.");
            var retainedIDs = current.Events.Keys.ToHashSet();
            Expect(current.Events.Values.OrderBy(item => item.Record.Offset).Select(item => item.ID)
                .SequenceEqual(graph.Events.Values.OrderBy(item => item.Record.Offset).Where(item => retainedIDs.Contains(item.ID)).Select(item => item.ID)), "Retained project record order changed.");
        }
        Expect(current.Roots.Select(edge => (edge.Scope, edge.Target)).SequenceEqual(graph.Roots.Select(edge => (edge.Scope, edge.Target))), "Stored roots changed.");
        Expect(doc.GetHistory().Undo.Count == history + 1 && changes == 1 && doc.EditRevision == revision + 1, "Cleanup not one committed edit.");
        doc.Undo(); Exact(doc, before, "Cleanup Undo not exact."); Expect(doc.EditRevision == revision + 2, "Undo lifetime reverted."); Reject(() => doc.ApplyUnusedScriptRemoval(plan), "Edit+Undo revived consent.");
        doc.Redo(); Exact(doc, after, "Cleanup Redo not exact.");
        var empty = doc.PlanUnusedScriptRemoval(); var noOpRevision = doc.EditRevision; var noOpHistory = doc.GetHistory().Undo.Count;
        Expect(empty.CanApply && !empty.HasWork, "Not all unused removed."); doc.ApplyUnusedScriptRemoval(empty);
        Expect(noOpRevision == doc.EditRevision && doc.GetHistory().Undo.Count == noOpHistory && changes == 3, "No-op consumed history.");
    }
    private static void Guards(bool native)
    {
        var doc = New(native); var root = Root(doc); var a = Add(doc, "TRIGGER_TIMEELAPSED"); var unused = Add(doc, "ACTION_ALLIES");
        Set(doc, root.Owner, root.Field.Name, a.ToString()); var before = Snapshot(doc); var foreign = doc.PlanUnusedScriptRemoval();
        Reject(() => New(native).ApplyUnusedScriptRemoval(foreign), "Foreign plan applied.");
        Link(doc, a, a); var blocked = doc.PlanUnusedScriptRemoval();
        Expect(!blocked.CanApply && blocked.Issues.Any(item => item.Problem == UnusedScriptProblem.Cycle), "Retained cycle accepted."); Reject(() => doc.ApplyUnusedScriptRemoval(blocked), "Blocked cleanup applied."); doc.Undo(); Exact(doc, before, "Cycle guard changed bytes.");
        Link(doc, a, 32000); Expect(doc.PlanUnusedScriptRemoval().Issues.Any(item => item.Problem == UnusedScriptProblem.MissingLink), "Missing edge accepted."); doc.Undo();
        Set(doc, root.Owner, root.Field.Name, "32000"); Expect(!doc.PlanUnusedScriptRemoval().CanApply, "Missing root accepted."); doc.Undo();
        var plan = doc.PlanUnusedScriptRemoval(); doc.Paint([new(5, 5)], new(8, 3, 1)); doc.Undo(); Reject(() => doc.ApplyUnusedScriptRemoval(plan), "Tile edit+Undo reused plan.");
        plan = doc.PlanUnusedScriptRemoval(); var unusedRecord = ScriptGraph.Build(doc).Events[unused].Record; var field = unusedRecord.Fields.Single(field => field.Name is "ID" or "id"); var oldID = unused; var revision = doc.EditRevision;
        Raw(unusedRecord.File, unusedRecord.Offset + field.Offset, a); Expect(doc.PlanUnusedScriptRemoval().Issues.Any(item => item.Problem == UnusedScriptProblem.Identity), "Duplicate ID accepted.");
        Reject(() => doc.ApplyUnusedScriptRemoval(plan), "Fingerprint missed external mutation."); Expect(revision == doc.EditRevision, "Raw fixture changed revision.");
        Raw(unusedRecord.File, unusedRecord.Offset + field.Offset, 0); Expect(!doc.PlanUnusedScriptRemoval().CanApply, "Zero ID accepted."); Raw(unusedRecord.File, unusedRecord.Offset + field.Offset, oldID); Exact(doc, before, "Raw guard restoration failed.");
        if (!native)
        {
            Raw(unusedRecord.File, unusedRecord.Offset + field.Offset, 31000); Expect(!doc.PlanUnusedScriptRemoval().CanApply, "Wrong signed action accepted."); Raw(unusedRecord.File, unusedRecord.Offset + field.Offset, oldID);
            Set(doc, root.Owner, "node.type", "512"); Expect(doc.PlanUnusedScriptRemoval().Issues.Any(item => item.Problem == UnusedScriptProblem.Scope), "Unknown scope accepted."); doc.Undo();
        }
        Set(doc, a, native ? "Type" : "ctrl", "255"); Expect(doc.PlanUnusedScriptRemoval().Issues.Any(item => item.Problem == UnusedScriptProblem.EventType), "Unknown reachable type accepted."); doc.Undo();
        Set(doc, unused, native ? "Type" : "ctrl", "255"); var unknown = doc.PlanUnusedScriptRemoval();
        Expect(unknown.CanApply && unknown.Events.Single().TypeName is null, "Verified unused unknown type cannot be removed."); doc.ApplyUnusedScriptRemoval(unknown); doc.Undo(); doc.Undo(); Exact(doc, before, "Unknown fixture restoration failed.");
        doc.Edit("No-op nested test", () => Reject(() => doc.PlanUnusedScriptRemoval(), "Nested preview accepted."));
    }
    private static void Semantics(bool native)
    {
        foreach (var name in new[] { "ACTION_ADDCREATURE", "ACTION_CHANGESQUARE", "ACTION_CHANGEOWNER", "ACTION_SHOWAREA", "ACTION_ENABLEFOG", "ACTION_MOVECAMERA", "ACTION_ROTATECAMERA", "ACTION_CAMERAPATH", "ACTION_SHOWCRYSTAL", "ACTION_RELEASEBAND" })
        {
            var doc = New(native); var root = Root(doc); var condition = Add(doc, "TRIGGER_TIMEELAPSED"); var action = Add(doc, name); Add(doc, "ACTION_ALLIES");
            Set(doc, root.Owner, root.Field.Name, condition.ToString()); Link(doc, condition, action, true); var prefix = native ? "Param" : "param";
            if (name is "ACTION_ADDCREATURE" or "ACTION_CHANGESQUARE" or "ACTION_CHANGEOWNER") Set(doc, action, prefix + "5", "24");
            Expect(!doc.PlanUnusedScriptRemoval().CanApply, "Invalid retained action accepted: " + name);
            var area = doc.AddMapRegion(MapRegionKind.Area, new(2, 2, 2, 2)); var areaID = area.Read(EntityReferences.Identity(area)!.Field);
            if (name is "ACTION_ADDCREATURE" or "ACTION_CHANGESQUARE" or "ACTION_CHANGEOWNER") { Set(doc, action, prefix + "5", "23"); Set(doc, action, prefix + "7", "19"); }
            else if (name == "ACTION_CAMERAPATH") Set(doc, action, prefix + "2", areaID);
            else if (name == "ACTION_SHOWCRYSTAL")
            { var crystal = native ? doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEOBJECT"), new(4, 4)) : doc.AddLegacyRecord("Object", new(4, 4)); Set(doc, crystal, native ? "bObjectID" : "id", "28"); }
            else if (name == "ACTION_RELEASEBAND")
            {
                var band = native ? doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEBAND"), new(0, 0)) : doc.AddLegacyRecord("Band", new(0, 0));
                Set(doc, band, EntityReferences.Identity(band)!.Field.Name, "0"); Set(doc, action, prefix + "1", "0"); Set(doc, action, prefix + "5", areaID); Set(doc, action, prefix + "2", "0"); Expect(doc.PlanUnusedScriptRemoval().CanApply, "Band zero rejected.");
                Set(doc, action, prefix + "2", "1"); Expect(doc.PlanUnusedScriptRemoval().Issues.Any(item => item.Problem == UnusedScriptProblem.PetBand), "Pet-band flag not checked.");
                var info = doc.Records().Single(item => native ? item.Category == "Map" : item.Category == "Project.MAPPROPERTIES"); Set(doc, info, native ? "bMainOptions2" : "Options2", "32");
            }
            else Set(doc, action, prefix + "1", areaID);
            Expect(doc.PlanUnusedScriptRemoval().CanApply, "Valid retained action rejected: " + name);
        }
    }
    private static void Layouts(bool native)
    {
        var doc = New(native); var condition = Add(doc, "TRIGGER_TIMEELAPSED"); var root = Root(doc); Set(doc, root.Owner, root.Field.Name, condition.ToString()); Add(doc, "ACTION_ALLIES");
        var file = doc.Files.Single(item => item.Signature == (native ? 0xD2 : NativeMapDocument.LegacyEvents)); var before = Snapshot(doc);
        // Raw fixture changes preserve array shape and never touch disk inputs.
        if (native)
        {
            var record = ScriptGraph.Build(doc).Events[condition].Record; Raw(file, record.Offset, 0xD7); Expect(!doc.PlanUnusedScriptRemoval().CanApply, "Opaque native event accepted."); Raw(file, record.Offset, 0xD5);
            Raw(file, 20, 2); Raw(file, 24, 0); Expect(!doc.PlanUnusedScriptRemoval().CanApply, "Native category-count mismatch accepted."); Raw(file, 20, 1); Raw(file, 24, 1);
            var originalSize = BinaryPrimitives.ReadUInt32LittleEndian(doc.MainFile.Data.Span[52..]);
            Expect(MemoryMarshal.TryGetArray(doc.MainFile.Data, out var main), "No main fixture buffer."); BinaryPrimitives.WriteUInt32LittleEndian(main.Array!.AsSpan(main.Offset + 52), 1);
            Expect(!doc.PlanUnusedScriptRemoval().CanApply, "Truncated root fields accepted."); BinaryPrimitives.WriteUInt32LittleEndian(main.Array.AsSpan(main.Offset + 52), originalSize);
        }
        Raw(file, 4, native ? (ushort)5 : (ushort)1); Expect(!doc.PlanUnusedScriptRemoval().CanApply, "Unverified scenario version accepted."); Raw(file, 4, native ? (ushort)4 : (ushort)3);
        Exact(doc, before, "Layout guard changed bytes.");
        var plan = doc.PlanUnusedScriptRemoval(); var revision = doc.EditRevision; doc.Edit("Explicit no-op", () => { }); Expect(revision == doc.EditRevision, "No-op changed monotonic revision.");
        doc.ApplyUnusedScriptRemoval(plan); doc.Undo(); Exact(doc, before, "Fresh plan after no-op cannot be applied exactly.");
    }
    private static void SaveCompile(bool native)
    {
        var doc = New(native); var first = Add(doc, "TRIGGER_TIMEELAPSED"); Add(doc, "ACTION_ALLIES"); var root = Root(doc); Set(doc, root.Owner, root.Field.Name, first.ToString()); var before = Snapshot(doc); var basis = NativeMapDocument.Create(24, 20, "Base");
        if (!native) Expect(ScriptGraph.Build(doc.CompileLegacyProject("KeepUnused", basis)).Events.Count == 2, "Keep filtered unused compile entries.");
        doc.ApplyUnusedScriptRemoval(doc.PlanUnusedScriptRemoval()); if (!native) Expect(ScriptGraph.Build(doc.CompileLegacyProject("RemoveUnused", basis)).Events.Count == 1, "Compiled removal not live edit.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2UnusedSave", Guid.NewGuid().ToString("N")); var reopened = NativeMapDocument.Load(doc.SaveCopy(directory));
        Expect(ScriptGraph.Build(reopened).Events.Count == 1 && !reopened.PlanUnusedScriptRemoval().HasWork, "Save/reopen changed cleaned graph."); doc.Undo(); Exact(doc, before, "Save/compile Undo not exact.");
    }
    private static void ProjectScopes()
    {
        var doc = New(false); var condition = Add(doc, "TRIGGER_TIMEELAPSED"); var id = condition; Add(doc, "ACTION_ALLIES");
        foreach (var scope in new uint[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 2 })
        { var link = doc.AddLegacyRecord("TriggerLink", new(0, 0)); Set(doc, link, "node.type", scope.ToString()); Set(doc, link, "trigger", id.ToString()); }
        var before = Snapshot(doc); var plan = doc.PlanUnusedScriptRemoval();
        Expect(plan.CanApply && plan.ReachableCount == 1 && plan.Actions == 1 && doc.ScriptBindings().Any(item => item.Owner is null), "Unresolved/all-nine/duplicate bindings dropped roots.");
        doc.ApplyUnusedScriptRemoval(plan); Expect(ScriptGraph.Build(doc).Roots.Count == 10 && ScriptGraph.Build(doc).Events.ContainsKey(id), "Cleanup repaired owners."); doc.Undo(); Exact(doc, before, "Owner cleanup Undo not exact.");
    }
    private static void NativePayloadSizes()
    {
        foreach (var stored in new uint[] { 0, 1, 48 })
        {
            var doc = New(true); var keep = Add(doc, "TRIGGER_TIMEELAPSED"); Add(doc, "ACTION_ALLIES"); var root = Root(doc); Set(doc, root.Owner, root.Field.Name, keep.ToString());
            var directory = Path.Combine(Path.GetTempPath(), "DK2UnusedSizes", Guid.NewGuid().ToString("N")); var path = doc.SaveCopy(directory); var events = doc.Files.Single(file => file.Signature == 0xD2); var eventsPath = Path.Combine(directory, events.ExportPath);
            var bytes = File.ReadAllBytes(eventsPath).Concat(new byte[] { 0x71, 0xC7, 0x1E }).ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), stored); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(56), stored); File.WriteAllBytes(eventsPath, bytes);
            var loaded = NativeMapDocument.Load(path); var before = Snapshot(loaded); loaded.ApplyUnusedScriptRemoval(loaded.PlanUnusedScriptRemoval()); var file = loaded.Files.Single(item => item.Signature == 0xD2);
            Expect(file.Data.Span[^3..].SequenceEqual(new byte[] { 0x71, 0xC7, 0x1E }) && file.Length == bytes.Length - 24, "Opaque native tail discarded.");
            Expect(BinaryPrimitives.ReadUInt32LittleEndian(file.Data.Span[8..]) == (stored >= 24 ? stored - 24 : file.Length) && BinaryPrimitives.ReadUInt32LittleEndian(file.Data.Span[56..]) == (stored >= 24 ? stored - 24 : 24), "Native size delta/underflow differs."); loaded.Undo(); Exact(loaded, before, "Size/tail Undo not exact.");
        }
    }
    private static void DeepGraph()
    {
        var doc = New(true); var first = Add(doc, "TRIGGER_TIMEELAPSED"); var root = Root(doc); Set(doc, root.Owner, root.Field.Name, "1");
        var directory = Path.Combine(Path.GetTempPath(), "DK2UnusedDeep", Guid.NewGuid().ToString("N")); var path = doc.SaveCopy(directory); var file = doc.Files.Single(item => item.Signature == 0xD2); var data = new byte[60 + 12000 * 24]; file.Data.Span[..60].CopyTo(data);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), (uint)data.Length); BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20), 12000); BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24), 0); BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(56), 12000 * 24); var record = EventBytes(ScriptGraph.Build(doc).Events[first]);
        for (var index = 0; index < 12000; index++)
        { var span = data.AsSpan(60 + index * 24, 24); record.CopyTo(span); BinaryPrimitives.WriteUInt16LittleEndian(span[16..], (ushort)(index + 1)); BinaryPrimitives.WriteUInt16LittleEndian(span[18..], index < 9999 ? (ushort)(index + 2) : (ushort)0); }
        File.WriteAllBytes(Path.Combine(directory, file.ExportPath), data); var loaded = NativeMapDocument.Load(path); var before = Snapshot(loaded); var plan = loaded.PlanUnusedScriptRemoval();
        Expect(plan.CanApply && plan.ReachableCount == 10000 && plan.Conditions == 2000, "Deep iterative reachability incorrect."); loaded.ApplyUnusedScriptRemoval(plan); Expect(ScriptGraph.Build(loaded).Events.Count == 10000, "Deep deletion incomplete."); loaded.Undo(); Exact(loaded, before, "Deep Undo not exact.");
    }
}
