using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class ObjectManagerChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException) { return; }
        throw new Exception("Unsafe manager operation accepted.");
    }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> snapshot) => Expect(snapshot.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Manager changed unrelated/undo bytes.");
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("Manager", "Manager", 12, 10, NativeFormat: native));
    private static void Add(NativeMapDocument doc, PlacedThingKind kind, TilePoint position, byte owner = 3, int data = 1, uint? id = null)
    {
        var template = ObjectBatchTemplate.Default(kind) with { Owner = owner, DataAll = data };
        if (id is { } type) template = template with { TypeID = type };
        doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(template, new(Position: position, Flags: (ObjectBatchFlags)127)), [0]);
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2MapEditor.ObjectManager", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true }) { Transactions(native, directory); Scopes(native, directory); Sorting(native); }
            Historical(directory); Ambiguous(directory); Tail(directory); Large();
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void Transactions(bool native, string directory)
    {
        var doc = New(native);
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) Add(doc, kind, new(1 + (int)kind, 2));
        foreach (byte owner in new byte[] { 1, 2 }) Add(doc, PlacedThingKind.Creature, new(owner, 3), owner);
        var entries = doc.ObjectManagerEntries(); var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
        Expect(entries.Count == 7 && entries.All(entry => entry.CanRemove && !entry.Scripted), "All five verified categories/native creature signatures not exposed.");
        if (!native) Expect(entries.All(entry => entry.Identity != 0), "Allocated project UID fixture invalid.");
        foreach (var method in Enum.GetValues<ObjectManagerSort>()) foreach (var language in new[] { "en", "ru" })
            Expect(ObjectManagerOrdering.Sort(entries, method, language).Count == 7, "Display sort lost entries.");
        var plan = doc.PlanObjectRemoval(entries.Select(entry => entry.Record).Concat([entries[0].Record]));
        Expect(plan.Entries.Count == 7 && plan.ScriptedCount == 0, "Preview duplicate handling/script-free UIDs wrong."); Exact(before);
        Reject(() => New(native).ApplyObjectRemoval(plan)); Exact(before);
        Reject(() => doc.PlanObjectRemoval([doc.Records().First(record => record.File.Signature == (native ? 0xB5 : NativeMapDocument.LegacyInfo))]));
        var changes = 0; doc.Changed += (_, _) => changes++;
        Expect(doc.ApplyObjectRemoval(doc.PlanObjectRemoval([])) == 0 && changes == 0 && doc.GetHistory().Undo.Count == history, "Empty removal consumed history/emitted change.");
        Expect(doc.ApplyObjectRemoval(plan) == 7 && doc.ObjectManagerEntries().Count == 0 && changes == 1 && doc.GetHistory().Undo.Count == history + 1, "Grouped removal not atomic/complete.");
        foreach (var pair in before.Where(pair => pair.Key.Signature != (native ? 0xBE : NativeMapDocument.LegacyThings))) Exact(new() { [pair.Key] = pair.Value });
        var after = Snapshot(doc); doc.Undo(); Exact(before); doc.Redo(); Exact(after);
        var saved = doc.SaveCopy(Path.Combine(directory, "removed-" + native)); Expect(NativeMapDocument.Load(saved).ObjectManagerEntries().Count == 0, "Removed objects returned on reopen.");
        doc.Undo(); Exact(before); var old = doc.PlanObjectRemoval(doc.ObjectManagerEntries().Select(entry => entry.Record));
        doc.Paint([new(7, 7)], new(8, 3, 1)); var edited = Snapshot(doc); Reject(() => doc.ApplyObjectRemoval(old)); Exact(edited);
        doc.Undo(); Exact(before);
        // Enumerating a caller's selection may edit; stale offsets must never be accepted.
        IEnumerable<NativeRecord> Mutating()
        { doc.Paint([new(7, 7)], new(4, 2, 1)); yield return doc.ObjectManagerEntries()[0].Record; }
        Reject(() => doc.PlanObjectRemoval(Mutating())); doc.Undo(); Exact(before);
        var redo = doc.GetHistory().Redo.Count; doc.ApplyObjectRemoval(doc.PlanObjectRemoval([])); Expect(doc.GetHistory().Redo.Count == redo, "No-op removal consumed redo.");
        // Coordinates may be invalid or on a protected border; removal still works without changing tiles.
        var edge = doc.ObjectManagerEntries()[0].Record; var x = edge.Fields.First(field => native ? field.Name.EndsWith("LocationX", StringComparison.Ordinal) : field.Name == "x");
        doc.SetField(edge, x, native ? "4294967295" : "-1");
        var malformed = doc.ObjectManagerEntries().First(entry => entry.Record.File == edge.File && entry.Record.Offset == edge.Offset);
        Expect(malformed.CanRemove && malformed.Thing.Position.X == -1, "Out-of-map physical removal incorrectly locked.");
        var malformedBefore = Snapshot(doc); Expect(doc.ApplyObjectRemoval(doc.PlanObjectRemoval([malformed.Record])) == 1, "Cannot remove out-of-map object."); doc.Undo(); Exact(malformedBefore);
    }
    private static void Scopes(bool native, string directory)
    {
        var doc = New(native);
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) { Add(doc, kind, new(1 + (int)kind, 2)); Add(doc, kind, new(1 + (int)kind, 3)); }
        ushort shared = 0;
        foreach (var kind in new[] { PlacedThingKind.Creature, PlacedThingKind.Door, PlacedThingKind.Object, PlacedThingKind.SpecialItem })
        {
            var entry = doc.ObjectManagerEntries().First(entry => entry.Thing.Kind == kind);
            var owner = doc.ScriptOwners().Single(owner => owner.Record.File == entry.Record.File && owner.Record.Offset == entry.Record.Offset);
            if (shared == 0) { var root = doc.EnsureScriptScope(owner); doc.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER")); shared = doc.FindScriptScope(owner)!.Target; }
            else doc.BindScriptScope(owner, shared);
        }
        var entries = doc.ObjectManagerEntries(); Expect(entries.Count(entry => entry.Scripted) == 4, "Scripted detection wrong across entity scopes.");
        var before = Snapshot(doc); var graph = ScriptGraph.Build(doc); var events = graph.Events.Values.Select(item => item.Record.File.Data.Span.Slice(item.Record.Offset, item.Record.Length).ToArray()).ToArray();
        var plan = doc.PlanObjectRemoval(entries.Select(entry => entry.Record)); Expect(plan.ScriptedCount == 4, "Preview scenario count wrong.");
        Expect(doc.ApplyObjectRemoval(plan) == 6 && doc.ObjectManagerEntries().Count == 4, "Default policy skipped allocated non-scripted UIDs or deleted scripted entries.");
        doc.Undo(); Exact(before);
        // Delete one owner of a shared branch, then the other approved owners.
        var first = doc.ObjectManagerEntries().First(entry => entry.Scripted);
        Expect(doc.ApplyObjectRemoval(doc.PlanObjectRemoval([first.Record]), true) == 1, "Approved scripted owner not removed.");
        Expect(ScriptGraph.Build(doc).Events.Count == events.Length && doc.ObjectManagerEntries().Count(entry => entry.Scripted) == 3, "Shared graph/other owner changed.");
        doc.Undo(); Exact(before);
        Expect(doc.ApplyObjectRemoval(plan, true) == 10 && doc.ObjectManagerEntries().Count == 0, "Explicit include did not delete complete group.");
        var remainingGraph = ScriptGraph.Build(doc);
        Expect(remainingGraph.Events.Count == events.Length && !remainingGraph.Roots.Any(root => root.Scope is 2 or 64 or 128 or 256 && root.Target != 0), "Obsolete scope/event graph removal wrong.");
        Expect(remainingGraph.Events.Values.Select((item, index) => item.Record.File.Data.Span.Slice(item.Record.Offset, item.Record.Length).SequenceEqual(events[index])).All(value => value), "Event bytes changed by owner removal.");
        var after = Snapshot(doc); doc.Undo(); Exact(before); doc.Redo(); Exact(after);
        var saved = doc.SaveCopy(Path.Combine(directory, "scopes-" + native)); var reopened = NativeMapDocument.Load(saved);
        Expect(reopened.ObjectManagerEntries().Count == 0 && ScriptGraph.Build(reopened).Events.Count == events.Length, "Scope/event preservation failed saved round trip.");
        if (!native)
        {
            doc.Undo(); var owner = doc.ScriptOwners().First(owner => owner.Scope == 2); var root = doc.FindScriptScope(owner)!;
            doc.SetScriptLink(root.Owner, root.Field, 0);
            Expect(doc.ObjectManagerEntries().Single(entry => entry.Record.File == owner.Record.File && entry.Record.Offset == owner.Record.Offset).Scripted, "Empty project binding not distinguished from unbound identity.");
            doc.Paint(MapDrawing.Rectangle(new(1, 1, 10, 8), true), new(8, 3, 1));
            doc.ApplyObjectRemoval(doc.PlanObjectRemoval(doc.ObjectManagerEntries().Select(entry => entry.Record)), true);
            var compiled = doc.CompileLegacyProject("RemovedCompiled", NativeMapDocument.Create(12, 10, "Basis"));
            Expect(compiled.ObjectManagerEntries().Count == 0 && ScriptGraph.Build(compiled).Events.Count == events.Length, "Removal lost events/entities in native compilation.");
        }
    }
    private static void Sorting(bool native)
    {
        var doc = New(native);
        Add(doc, PlacedThingKind.Creature, new(7, 2), 7, 8, 1); Add(doc, PlacedThingKind.Creature, new(3, 3), 1, 2, 2);
        Add(doc, PlacedThingKind.Creature, new(2, 2), 3, 3, 1); Add(doc, PlacedThingKind.Creature, new(2, 2), 3, 1, 1);
        Add(doc, PlacedThingKind.Trap, new(1, 1)); Add(doc, PlacedThingKind.Door, new(8, 7));
        Add(doc, PlacedThingKind.Object, new(1, 1), 2, -7); Add(doc, PlacedThingKind.SpecialItem, new(8, 7));
        var entries = doc.ObjectManagerEntries(); var before = Snapshot(doc);
        var creatures = entries.Where(entry => entry.Thing.Kind == PlacedThingKind.Creature).ToArray();
        Expect(ObjectManagerOrdering.Sort(creatures, ObjectManagerSort.Position).Select(entry => entry.DataAll).SequenceEqual(new[] { 3, 1, 8, 2 }), "Row-major/same-position stable sort wrong.");
        Expect(ObjectManagerOrdering.Sort(creatures, ObjectManagerSort.Owner).Select(entry => entry.Thing.Owner).SequenceEqual(new byte[] { 1, 3, 3, 7 }), "Owner sort wrong.");
        Expect(ObjectManagerOrdering.Sort(creatures, ObjectManagerSort.ID).Select(entry => entry.Thing.TypeID).SequenceEqual(new uint[] { 1, 1, 1, 2 }), "ID sort wrong.");
        var same = creatures.Where(entry => entry.Thing.TypeID == 1).ToArray();
        Expect(ObjectManagerOrdering.Sort(same, ObjectManagerSort.Type).Select(entry => entry.DataAll).SequenceEqual(new[] { 1, 3, 8 }), "Catalogue/data tie order wrong.");
        var negative = same[0] with { DataAll = -7 }; Expect(ObjectManagerOrdering.Compare(negative, same[0], ObjectManagerSort.Type) < 0, "Signed data comparison wrong.");
        foreach (var method in Enum.GetValues<ObjectManagerSort>()) foreach (var language in new[] { "en", "ru" })
        {
            foreach (var a in entries) foreach (var b in entries)
                Expect(Math.Sign(ObjectManagerOrdering.Compare(a, b, method, language)) == -Math.Sign(ObjectManagerOrdering.Compare(b, a, method, language)), "Manager comparator violates antisymmetry.");
            foreach (var a in entries) foreach (var b in entries) foreach (var c in entries)
                if (ObjectManagerOrdering.Compare(a, b, method, language) <= 0 && ObjectManagerOrdering.Compare(b, c, method, language) <= 0)
                    Expect(ObjectManagerOrdering.Compare(a, c, method, language) <= 0, "Manager comparator violates transitivity.");
            var sorted = ObjectManagerOrdering.Sort(entries, method, language);
            Expect(sorted.ToList().FindIndex(entry => entry.Thing.Kind == PlacedThingKind.Door) < sorted.ToList().FindIndex(entry => entry.Thing.Kind == PlacedThingKind.Trap)
                && sorted.ToList().FindIndex(entry => entry.Thing.Kind == PlacedThingKind.SpecialItem) < sorted.ToList().FindIndex(entry => entry.Thing.Kind == PlacedThingKind.Object), "Source family priority missing.");
        }
        Reject(() => ObjectManagerOrdering.Sort(entries, (ObjectManagerSort)5)); Exact(before);
    }
    private static void Historical(string directory)
    {
        var doc = New(false); Add(doc, PlacedThingKind.Creature, new(2, 2)); var record = doc.ObjectManagerEntries().Single().Record;
        var container = LegacySectionFile.Read(doc.SaveCopy(Path.Combine(directory, "historical-basis")));
        for (var version = 1; version <= 6; version++)
        {
            var structure = version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", 5 => "MAPTHINGV5", _ => "MAPTHING" };
            var payload = new byte[LegacyProjectSchemas.Size(structure)];
            foreach (var field in LegacyProjectSchemas.Fields(structure)) if (record.Fields.FirstOrDefault(source => source.Name == field.Name) is { } source) field.Encode(record.Read(source)).CopyTo(payload, field.Offset);
            var path = Path.Combine(directory, $"historical-{version}.dkm"); File.WriteAllBytes(path, LegacySectionFile.Create(container.Sections.Select(section => section.Signature == NativeMapDocument.LegacyThings ? new(section.Signature, version, payload) : section)).ToArray());
            var old = NativeMapDocument.Load(path); var before = Snapshot(old); var entry = old.ObjectManagerEntries().Single();
            Expect(entry.CanRemove && old.ApplyObjectRemoval(old.PlanObjectRemoval([entry.Record])) == 1, "Historical physical removal incorrectly needs upgrade.");
            Expect(BinaryPrimitives.ReadUInt32LittleEndian(entry.Record.File.Data.Span[4..]) == version && entry.Record.File.Length == 12, "Removal upgraded historical Things layout.");
            old.Undo(); Exact(before);
        }
        var future = Path.Combine(directory, "future-events.dkm");
        File.WriteAllBytes(future, LegacySectionFile.Create(container.Sections.Append(new(NativeMapDocument.LegacyEvents, 99, new byte[12]))).ToArray());
        var unsupported = NativeMapDocument.Load(future); var unchanged = Snapshot(unsupported);
        Expect(!unsupported.ObjectManagerEntries().Single().CanRemove, "Opaque scenario layout treated as understood.");
        Reject(() => unsupported.ApplyObjectRemoval(unsupported.PlanObjectRemoval(unsupported.ObjectManagerEntries().Select(entry => entry.Record)), true)); Exact(unchanged);
    }
    private static void Ambiguous(string directory)
    {
        var doc = New(false); Add(doc, PlacedThingKind.Door, new(2, 2)); Add(doc, PlacedThingKind.Door, new(3, 3));
        var entries = doc.ObjectManagerEntries(); var owner = doc.ScriptOwners().First(owner => owner.Scope == 64);
        var root = doc.EnsureScriptScope(owner); doc.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        var container = LegacySectionFile.Read(doc.SaveCopy(Path.Combine(directory, "ambiguous-basis")));
        var section = container.Sections.Single(section => section.Signature == NativeMapDocument.LegacyThings); var payload = section.Payload.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(64 + 56), entries[0].Identity);
        var path = Path.Combine(directory, "ambiguous.dkm"); File.WriteAllBytes(path, LegacySectionFile.Create(container.Sections.Select(item => item == section ? new(item.Signature, item.Version, payload) : item)).ToArray());
        var imported = NativeMapDocument.Load(path); var before = Snapshot(imported); var first = imported.ObjectManagerEntries()[0];
        Reject(() => imported.ApplyObjectRemoval(imported.PlanObjectRemoval([first.Record]), true)); Exact(before);
        Expect(imported.ApplyObjectRemoval(imported.PlanObjectRemoval(imported.ObjectManagerEntries().Select(entry => entry.Record)), true) == 2 && ScriptGraph.Build(imported).Events.Count == 1, "Explicit all ambiguous owners did not preserve events.");
        imported.Undo(); Exact(before);
    }
    private static void Tail(string directory)
    {
        var doc = New(true); foreach (var kind in Enum.GetValues<PlacedThingKind>()) Add(doc, kind, new(1 + (int)kind, 2));
        var path = doc.SaveCopy(Path.Combine(directory, "tail-basis")); var things = doc.Files.Single(file => file.Signature == 0xBE && file.ExportPath.Contains("Maps"));
        var filePath = Path.Combine(directory, "tail-basis", things.ExportPath.Replace('/', Path.DirectorySeparatorChar));
        var bytes = File.ReadAllBytes(filePath);
        foreach (var storedSize in new[] { 0u, 1u, 55u, (uint)bytes.Length })
        {
            var fixture = bytes.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(fixture.AsSpan(8), storedSize);
            File.WriteAllBytes(filePath, [.. fixture, 0x91, 0x82, 0x73]);
            var reopened = NativeMapDocument.Load(path); var before = Snapshot(reopened); var file = reopened.Files.Single(file => file.Signature == 0xBE && file.ExportPath == things.ExportPath);
            var removedBytes = reopened.ObjectManagerEntries().Sum(entry => entry.Record.Length);
            reopened.ApplyObjectRemoval(reopened.PlanObjectRemoval(reopened.ObjectManagerEntries().Select(entry => entry.Record)));
            Expect(file.Data.Span[^3..].SequenceEqual(new byte[] { 0x91, 0x82, 0x73 }) && BinaryPrimitives.ReadUInt32LittleEndian(file.Data.Span[20..]) == 0
                && BinaryPrimitives.ReadUInt32LittleEndian(file.Data.Span[52..]) == 0, "Native header/opaque tail removal wrong.");
            Expect(BinaryPrimitives.ReadUInt32LittleEndian(file.Data.Span[8..]) == (storedSize >= removedBytes ? storedSize - removedBytes : file.Length), "Understated size underflow not corrected safely.");
            reopened.Undo(); Exact(before);
            var first = reopened.ObjectManagerEntries()[0].Record; reopened.DeleteRecord(first);
            Expect(reopened.ObjectManagerEntries().Count == 4 && file.Data.Span[^3..].SequenceEqual(new byte[] { 0x91, 0x82, 0x73 }), "Generic deletion still fails on understated file size.");
            reopened.Undo(); Exact(before);
            // A smaller replacement also has negative byte delta (28-byte door -> 24-byte trap).
            reopened.ApplyObjectBatchInsertion(reopened.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Trap),
                new(Position: new(2, 2), Flags: (ObjectBatchFlags)127, Replacement: ObjectReplacementPolicy.All)), [0]);
            reopened.Undo(); Exact(before);
            var saved = reopened.SaveCopy(Path.Combine(directory, "understated-" + storedSize)); Expect(NativeMapDocument.Load(saved).ObjectManagerEntries().Count == 5, "Understated-size round trip lost entries.");
        }
    }
    private static void Large()
    {
        foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("Large", "Large", 40, 40, NativeFormat: native));
            doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Creature),
                new(ObjectBatchMode.Rectangle, WholeMap: false, Selection: new(1, 1, 32, 32), Flags: (ObjectBatchFlags)127)));
            var before = Snapshot(doc); var entries = doc.ObjectManagerEntries(); Expect(entries.Count == 1024, "Large manager fixture wrong.");
            Expect(doc.ApplyObjectRemoval(doc.PlanObjectRemoval(entries.Select(entry => entry.Record))) == 1024, "Large removal truncated selected entries."); doc.Undo(); Exact(before);
        }
    }
    public static void CheckLoaded(NativeMapDocument doc)
    {
        var before = Snapshot(doc); var history = doc.GetHistory(); var entries = doc.ObjectManagerEntries();
        foreach (var method in Enum.GetValues<ObjectManagerSort>()) foreach (var language in new[] { "en", "ru" })
        {
            var sorted = ObjectManagerOrdering.Sort(entries, method, language, ValueCatalogues.GameVersion(doc));
            Expect(sorted.Count == entries.Count, "Loaded manager sort lost entries.");
            for (var index = 1; index < sorted.Count; index++) Expect(ObjectManagerOrdering.Compare(sorted[index - 1], sorted[index], method, language, ValueCatalogues.GameVersion(doc)) <= 0, "Loaded manager ordering inconsistent.");
        }
        var approved = entries.Where(entry => entry.CanRemove).Take(7).Select(entry => entry.Record).ToArray();
        var plan = doc.PlanObjectRemoval(approved); Exact(before);
        if (approved.Length != 0)
        {
            var events = doc.Files.Where(file => file.Signature == 0xD2).ToDictionary(file => file, file => file.Data.ToArray());
            Expect(doc.ApplyObjectRemoval(plan, true) == approved.Length && doc.ObjectManagerEntries().Count == entries.Count - approved.Length, "Loaded native removal count wrong.");
            Exact(events); doc.Undo(); Exact(before);
        }
        Expect(doc.GetHistory().Undo.Count == history.Undo.Count, "Loaded removal undo changed existing history.");
    }
}
