using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class ObjectBatchInsertionChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException) { return; }
        throw new Exception("Unsafe insertion accepted.");
    }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> snapshot) => Expect(snapshot.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Insertion/undo changed unrelated bytes.");
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("Batch", "Batch", 12, 10, NativeFormat: native));
    private static NativeRecord[] Things(NativeMapDocument document) => document.Records().Where(record => record.File.Signature == (document.IsLegacyProject ? NativeMapDocument.LegacyThings : 0xBE)
        && (document.IsLegacyProject || record.Category is "GAMEHERO" or "GAMENEUTRAL" or "GAMEKEEPER" or "GAMEDOOR" or "GAMETRAP" or "GAMEOBJECT")).ToArray();
    private static ObjectBatchPlacementOptions Options(TilePoint point) => new(Position: point, Flags: (ObjectBatchFlags)127, Replacement: ObjectReplacementPolicy.All);

    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2MapEditor.BatchInsertion", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            CheckDefaults();
            foreach (var native in new[] { false, true }) { CheckEncoding(native, directory); CheckTransactions(native); CheckReplacement(native, directory); }
            CheckHistorical(directory); CheckCompiler(); CheckNativeTail(directory); CheckLarge(); CheckLimits();
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void CheckDefaults()
    {
        var expected = new[] { new ObjectBatchTemplate(PlacedThingKind.Creature, 1, Health: 100, DataAll: 1, CreatureOptions: 9, Objective: 19),
            new(PlacedThingKind.Door, 6), new(PlacedThingKind.Trap, 4, DataAll: 4), new(PlacedThingKind.Object, 1, 2), new(PlacedThingKind.SpecialItem, 117, 2) };
        Expect(Enum.GetValues<PlacedThingKind>().Select(ObjectBatchTemplate.Default).SequenceEqual(expected), "Original Editor.c manager defaults differ.");
        Reject(() => ObjectBatchTemplate.Default((PlacedThingKind)5));
    }
    // Independent literal byte layouts from Things.h/Game.h, not the serializer's schema or compiler mapping.
    private static byte[] Expected(ObjectBatchTemplate template, TilePoint point, byte owner, bool native, uint identity = 1)
    {
        var size = !native ? 64 : template.Kind switch { PlacedThingKind.Creature => owner == 1 ? 40 : owner == 2 ? 32 : 36, PlacedThingKind.Door => 28, PlacedThingKind.Trap => 24, _ => 32 };
        var bytes = new byte[size];
        void D(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
        if (!native)
        {
            D(8, (uint)template.Kind); D(12, (uint)point.X); D(16, (uint)point.Y); D(20, template.TypeID); D(24, owner);
            D(28, template.Health); D(32, template.GoldHeld); D(36, unchecked((uint)template.DataAll)); D(40, template.HeroOptions);
            D(44, template.CreatureOptions); D(48, template.Objective); D(52, template.Area); D(56, identity); D(60, template.Enemy);
            return bytes;
        }
        D(0, template.Kind switch { PlacedThingKind.Creature => owner == 1 ? 0xC7u : owner == 2 ? 0xC6u : 0xC8u, PlacedThingKind.Door => 0xC4u, PlacedThingKind.Trap => 0xC3u, _ => 0xC2u });
        D(4, (uint)size - 8); D(8, (uint)point.X); D(12, (uint)point.Y);
        if (template.Kind == PlacedThingKind.Creature)
        {
            bytes[20] = unchecked((byte)template.GoldHeld); bytes[22] = unchecked((byte)template.DataAll); bytes[23] = unchecked((byte)template.CreatureOptions);
            D(owner == 1 ? 28 : 24, template.Health);
            if (owner == 1)
            {
                bytes[24] = unchecked((byte)template.Area); bytes[34] = unchecked((byte)template.Enemy); bytes[35] = unchecked((byte)template.Objective);
                bytes[36] = unchecked((byte)template.TypeID); bytes[38] = 255; bytes[39] = unchecked((byte)template.HeroOptions);
            }
            else { bytes[owner == 2 ? 30 : 34] = unchecked((byte)template.TypeID); if (owner != 2) bytes[35] = owner; }
        }
        else if (template.Kind == PlacedThingKind.Door) { bytes[22] = unchecked((byte)template.TypeID); bytes[23] = owner; bytes[24] = unchecked((byte)template.DataAll); }
        else if (template.Kind == PlacedThingKind.Trap) { bytes[20] = unchecked((byte)template.DataAll); bytes[21] = unchecked((byte)template.TypeID); bytes[22] = owner; }
        else
        {
            bytes[30] = unchecked((byte)template.TypeID); bytes[31] = owner;
            if (template.Kind == PlacedThingKind.Object) { if (template.TypeID is 1 or 2 or 3) D(24, unchecked((uint)template.DataAll)); if (template.TypeID == 4) D(20, unchecked((uint)template.DataAll)); }
        }
        return bytes;
    }
    private static void CheckEncoding(bool native, string directory)
    {
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) foreach (var owner in Enumerable.Range(1, 7).Select(value => (byte)value))
            foreach (var id in kind == PlacedThingKind.Object ? new uint[] { 1, 2, 3, 4, 28, 130, 999 } : kind == PlacedThingKind.SpecialItem ? [117u, 127u] : [1u, 999u])
        {
            var document = New(native); var template = new ObjectBatchTemplate(kind, id, owner, uint.MaxValue, 0x12345678, -123, 0x87654321, 0xABCD1234, 0x123, 0x134, 0x145);
            var before = Snapshot(document); var undo = document.GetHistory().Undo.Count; var events = 0; document.Changed += (_, _) => events++;
            var plan = document.PlanObjectBatchInsertion(template, Options(new(2, 2))); Exact(before);
            Expect(document.ApplyObjectBatchInsertion(plan, [0]) == 1, "Full template was not inserted.");
            var record = Things(document).Single();
            Expect(record.File.Data.Span.Slice(record.Offset, record.Length).SequenceEqual(Expected(template, new(2, 2), owner, native)), $"Independent full bytes differ: {native}/{kind}/{owner}/{id}.");
            Expect(events == 1 && document.GetHistory().Undo.Count == undo + 1, "Insertion not one atomic event/history step.");
            var after = Snapshot(document); document.Undo(); Exact(before); document.Redo(); Exact(after);
            var path = document.SaveCopy(Path.Combine(directory, $"encoding-{native}-{kind}-{owner}-{id}")); var reopened = Things(NativeMapDocument.Load(path)).Single();
            Expect(reopened.File.Data.Span.Slice(reopened.Offset, reopened.Length).SequenceEqual(record.File.Data.Span.Slice(record.Offset, record.Length)), "Saved template differs.");
        }
        var automatic = New(native);
        automatic.Paint([new(2, 2)], new(8, 1, 1)); automatic.Paint([new(3, 2)], new(3, 2, 1)); automatic.Paint([new(4, 2)], new(8, 7, 1));
        var full = ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Enemy = 5 };
        var auto = automatic.PlanObjectBatchInsertion(full, new(ObjectBatchMode.Rectangle, WholeMap: false, Selection: new(2, 2, 3, 1), Flags: (ObjectBatchFlags)127));
        automatic.ApplyObjectBatchInsertion(auto);
        var added = Things(automatic); Expect(added.Length == 3, "Auto-owner batch count wrong.");
        for (var index = 0; index < 3; index++) Expect(added[index].File.Data.Span.Slice(added[index].Offset, added[index].Length)
            .SequenceEqual(Expected(full, new(index + 2, 2), new byte[] { 1, 2, 7 }[index], native, (uint)index + 1)), "Per-candidate automatic creature representation wrong.");
    }
    private static void CheckTransactions(bool native)
    {
        var document = New(native); document.Paint([new(2, 2)], new(8, 3, 1)); var before = Snapshot(document); var history = document.GetHistory();
        var calls = 0; var plan = document.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Trap),
            new(ObjectBatchMode.RandomRectangle, WholeMap: false, Selection: new(0, 0, 5, 4), RandomFrequency: 2, Flags: (ObjectBatchFlags)127), randomSample: _ => calls++ % 2);
        var chosen = plan.Placement.Candidates.Where(candidate => !candidate.Locked).Select(candidate => candidate.Index).Take(3).ToArray();
        var count = document.ApplyObjectBatchInsertion(plan, chosen); Expect(count == chosen.Length && calls == 20, "Apply resampled or lost explicit decisions.");
        var after = Snapshot(document); Reject(() => document.ApplyObjectBatchInsertion(plan, chosen)); Exact(after);
        document.Undo(); Exact(before); Expect(document.GetHistory().Undo.Count == history.Undo.Count, "Undo count wrong.");
        var redo = document.GetHistory().Redo.ToArray(); var empty = document.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Object), Options(new(2, 2)));
        Expect(document.ApplyObjectBatchInsertion(empty, []) == 0 && redo.SequenceEqual(document.GetHistory().Redo), "Empty application consumed redo."); Exact(before);
        Reject(() => document.ApplyObjectBatchInsertion(empty, [0, 0])); Reject(() => New(native).ApplyObjectBatchInsertion(empty, [0]));
        Reject(() => document.ApplyObjectBatchInsertion(empty with { Template = empty.Template with { TypeID = 99 } }, [0])); Exact(before);
        var locked = document.PlanObjectBatchInsertion(empty.Template, Options(new(0, 0))); Reject(() => document.ApplyObjectBatchInsertion(locked, [0])); Exact(before);
        document.Redo(); Exact(after);
        var nested = document.PlanObjectBatchInsertion(empty.Template, Options(new(3, 3)));
        Reject(() => document.Edit("Batch rollback fixture", () => { document.ApplyObjectBatchInsertion(nested, [0]); throw new InvalidOperationException("Injected enclosing transaction failure."); })); Exact(after);
        var current = document.PlanObjectBatchInsertion(empty.Template, Options(new(3, 3)));
        IEnumerable<int> MutatingSelection() { document.Paint([new(4, 4)], new(8, 3, 1)); yield return 0; }
        Reject(() => document.ApplyObjectBatchInsertion(current, MutatingSelection())); Expect(Things(document).Length == count, "Mutating selection inserted stale bytes.");
    }
    private static void CheckReplacement(bool native, string directory)
    {
        var document = New(native);
        var old = document.IsLegacyProject ? document.AddLegacyRecord("Door", new(2, 2), 3)
            : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEDOOR"), new(2, 2), 3);
        var root = document.EnsureScriptScope(document.ScriptOwners().Single(owner => owner.Scope == 64));
        document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        if (native) document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMETRAP"), new(2, 2), 3);
        else document.AddLegacyRecord("Trap", new(2, 2), 3);
        var before = Snapshot(document); var graphEvents = ScriptGraph.Build(document).Events.Count;
        var plan = document.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Trap) with { Owner = 3, DataAll = 9 }, Options(new(2, 2)));
        Expect(plan.Placement.Candidates[0].Replacement?.Kind == PlacedThingKind.Door, "Source door-before-trap replacement wrong.");
        document.ApplyObjectBatchInsertion(plan); Expect(Things(document).Length == 2 && !Things(document).Any(record => record.Category == "GAMEDOOR"
            || document.IsLegacyProject && record.Read(record.Fields.Single(field => field.Name == "node.type")) == "1"), "Replacement removed too many/incorrect collocated records.");
        Expect(ScriptGraph.Build(document).Events.Count == graphEvents && !ScriptGraph.Build(document).Roots.Any(edge => edge.Scope == 64 && edge.Target != 0), "Replacement deleted event graph or retained obsolete root.");
        var after = Snapshot(document); document.Undo(); Exact(before); document.Redo(); Exact(after);
        var saved = document.SaveCopy(Path.Combine(directory, "replacement-" + native)); Expect(ScriptGraph.Build(NativeMapDocument.Load(saved)).Events.Count == graphEvents, "Replacement graph lost on save.");
        if (!native)
        {
            document.Undo(); var copy = document.AddLegacyRecord("Door", new(3, 3), 3); var identity = old.Read(old.Fields.Single(field => field.Name == "uniqueid"));
            // Build an imported duplicate identity using a saved fixture: normal identity editing correctly rejects it.
            var path = document.SaveCopy(Path.Combine(directory, "ambiguous-basis")); var container = LegacySectionFile.Read(path);
            var section = container.Sections.Single(section => section.Signature == NativeMapDocument.LegacyThings); var bytes = section.Payload.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(copy.Offset - 12 + 56), uint.Parse(identity));
            var fixture = Path.Combine(directory, "ambiguous.dkm"); File.WriteAllBytes(fixture, LegacySectionFile.Create(container.Sections.Select(item => item == section ? new(item.Signature, item.Version, bytes) : item)).ToArray());
            var imported = NativeMapDocument.Load(fixture); var unchanged = Snapshot(imported);
            var ambiguous = imported.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Trap), Options(new(2, 2)));
            Reject(() => imported.ApplyObjectBatchInsertion(ambiguous, [0])); Exact(unchanged);
        }
    }
    private static void CheckHistorical(string directory)
    {
        var project = New(false); var record = project.AddLegacyRecord("Creature", new(3, 3), 1);
        var saved = project.SaveCopy(Path.Combine(directory, "historical")); var container = LegacySectionFile.Read(saved);
        for (var version = 1; version <= 6; version++)
        {
            var structure = version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", 5 => "MAPTHINGV5", _ => "MAPTHING" };
            var payload = new byte[LegacyProjectSchemas.Size(structure)];
            foreach (var field in LegacyProjectSchemas.Fields(structure)) if (record.Fields.FirstOrDefault(item => item.Name == field.Name) is { } source) field.Encode(record.Read(source)).CopyTo(payload, field.Offset);
            var path = Path.Combine(directory, "old-" + version + ".dkm"); File.WriteAllBytes(path, LegacySectionFile.Create(container.Sections.Select(section => section.Signature == NativeMapDocument.LegacyThings ? new(section.Signature, version, payload) : section)).ToArray());
            var old = NativeMapDocument.Load(path); var before = Snapshot(old); var options = Options(new(2, 2)); var template = ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 1, HeroOptions = 15, Area = 7, Enemy = 5 };
            var plan = old.PlanObjectBatchInsertion(template, options);
            if (version != 6) { Reject(() => old.ApplyObjectBatchInsertion(plan, [0])); Exact(before); }
            var empty = old.PlanObjectBatchInsertion(template, options, true); Expect(old.ApplyObjectBatchInsertion(empty, []) == 0, "Empty batch upgraded old section."); Exact(before);
            old.ApplyObjectBatchInsertion(empty, [0]); var thing = Things(old).Last();
            Expect(thing.Category == "Project.MAPTHING" && thing.File.Data.Span.Slice(thing.Offset, thing.Length).SequenceEqual(Expected(template, new(2, 2), 1, false, version == 1 ? 1u : 2u)), "Historical template payload lost.");
            foreach (var pair in before.Where(pair => pair.Key.Signature != NativeMapDocument.LegacyThings)) Expect(pair.Key.Data.Span.SequenceEqual(pair.Value), "Things-only upgrade changed unrelated project sections.");
            var after = Snapshot(old); old.Undo(); Exact(before); old.Redo(); Exact(after);
        }
    }
    private static void CheckCompiler()
    {
        var project = New(false); var native = New(true);
        var index = 0;
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) foreach (byte owner in new byte[] { 1, 2, 3, 7 })
        {
            var point = new TilePoint(1 + index % 10, 1 + index / 10); index++;
            var template = ObjectBatchTemplate.Default(kind) with { Owner = owner, Health = 87, GoldHeld = 55, DataAll = kind == PlacedThingKind.Creature ? 5 : 1, HeroOptions = 15, CreatureOptions = 137, Enemy = 5 };
            project.ApplyObjectBatchInsertion(project.PlanObjectBatchInsertion(template, Options(point)), [0]); native.ApplyObjectBatchInsertion(native.PlanObjectBatchInsertion(template, Options(point)), [0]);
        }
        // Make unrelated terrain validation valid; creatures/doors were explicitly placed over warnings.
        project.Paint(MapDrawing.Rectangle(new(1, 1, 10, 8), true), new(8, 3, 1));
        var compiled = project.CompileLegacyProject("BatchCompiled", NativeMapDocument.Create(12, 10, "Basis"));
        // Original Compiler_GenerateThingsFile writes five category lists;
        // direct editing retains insertion order. Compare complete payloads in
        // that source order, preserving order within each list.
        int SourceOrder(NativeRecord record) => record.Category switch
        {
            "GAMEOBJECT" => byte.Parse(record.Read(record.Fields.Single(field => field.Name == "bObjectID"))) is >= 0x75 and <= 0x81 ? 1 : 0,
            "GAMETRAP" => 2, "GAMEDOOR" => 3, _ => 4
        };
        Expect(Things(compiled).Select(record => Convert.ToHexString(record.File.Data.Span.Slice(record.Offset, record.Length)))
            .SequenceEqual(Things(native).OrderBy(SourceOrder).Select(record => Convert.ToHexString(record.File.Data.Span.Slice(record.Offset, record.Length)))), "Direct native template differs from source compiler output.");
    }
    private static void CheckNativeTail(string directory)
    {
        var document = New(true); var path = document.SaveCopy(Path.Combine(directory, "tail-basis")); var file = document.Files.Single(file => file.Signature == 0xBE && file.ExportPath.Contains("Maps"));
        var target = Path.Combine(directory, "tail-basis", file.ExportPath); var original = File.ReadAllBytes(target); var bytes = original.Concat(new byte[] { 201, 202, 203, 204 }).ToArray(); File.WriteAllBytes(target, bytes);
        var loaded = NativeMapDocument.Load(path); var before = Snapshot(loaded); var plan = loaded.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Object), Options(new(2, 2)));
        loaded.ApplyObjectBatchInsertion(plan, [0]); var current = loaded.Files.Single(item => item.ExportPath == file.ExportPath);
        Expect(current.Data.Span[^4..].SequenceEqual(bytes.AsSpan(bytes.Length - 4)), "Opaque native tail overwritten."); loaded.Undo(); Exact(before);
    }
    private static void CheckLarge()
    {
        foreach (var native in new[] { false, true })
        {
            var document = NativeMapDocument.CreateNewMap(new("LargeBatch", "LargeBatch", 128, 128, NativeFormat: native));
            var before = Snapshot(document); var plan = document.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Object),
                new(ObjectBatchMode.Rectangle, Flags: (ObjectBatchFlags)127));
            var selected = plan.Placement.Candidates.Where(candidate => !candidate.Locked).Select(candidate => candidate.Index).ToArray();
            Expect(document.ApplyObjectBatchInsertion(plan, selected) == 126 * 126 && Things(document).Length == selected.Length, "Large batch lost objects."); document.Undo(); Exact(before);
        }
    }
    private static void CheckLimits()
    {
        foreach (var native in new[] { false, true })
        {
            var document = NativeMapDocument.CreateNewMap(new("LimitBatch", "LimitBatch", 1024, 1024, NativeFormat: native));
            var before = Snapshot(document); var history = document.GetHistory(); var events = 0; document.Changed += (_, _) => events++;
            var plan = document.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Object), new(ObjectBatchMode.Rectangle, Flags: (ObjectBatchFlags)127));
            Reject(() => document.ApplyObjectBatchInsertion(plan, plan.Placement.Candidates.Where(candidate => !candidate.Locked).Select(candidate => candidate.Index)));
            Exact(before); Expect(events == 0 && history.Undo.SequenceEqual(document.GetHistory().Undo), "Count/size preflight failure partially applied batch.");
        }
    }
}
