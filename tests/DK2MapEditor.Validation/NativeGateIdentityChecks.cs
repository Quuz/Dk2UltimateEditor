using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

internal static class NativeGateIdentityChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static NativeMapDocument New() => NativeMapDocument.CreateNewMap(new("GateIdentity", "GateIdentity", 32, 30, NativeFormat: true));
    private static NativeRecord Gate(NativeMapDocument doc, int x, int y) => doc.AddMapRegion(MapRegionKind.Gate, new(x, y, 1, 1));
    private static string Key(NativeRecord row) => row.File.ExportPath + ":" + row.Offset;
    private static NativeRecord Action(NativeMapDocument doc, NativeRecord? gate)
    {
        var action = doc.AddScenarioEvent(0xD6, new(0, 0), LegacyEventSchemas.All.Single(type => type.Name == "ACTION_DESTROYGATE"));
        if (gate is not null) doc.ApplyScenarioEditorParameterDraft(doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(action), Key(gate)));
        return action;
    }
    private static void Point(NativeRecord action, int x, int y, byte[]? before = null)
    {
        var current = action.File.Data.Slice(action.Offset, action.Length).ToArray();
        if (before is not null)
        {
            var expected = before.ToArray(); BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(12), (ushort)x); BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(14), (ushort)y);
            Expect(current.AsSpan().SequenceEqual(expected), "Gate ID reuse changed unrelated action bytes.");
        }
        Expect(BinaryPrimitives.ReadUInt16LittleEndian(current.AsSpan(12)) == x && BinaryPrimitives.ReadUInt16LittleEndian(current.AsSpan(14)) == y, "Gate ID reuse projected the wrong coordinates.");
    }
    private static Dictionary<string, byte[]> Snapshot(NativeMapDocument doc) => doc.PrepareProjectConversion().SourceFiles.ToDictionary(file => file.ExportPath, file => file.Bytes.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<string, byte[]> before)
    {
        var after = Snapshot(doc); Expect(after.Count == before.Count && before.All(pair => after[pair.Key].AsSpan().SequenceEqual(pair.Value)), "Gate identities or game buffers did not restore exactly.");
    }
    private static uint[] IDs(NativeMapDocument doc) => doc.ScenarioGateChoices().Select(choice => choice.WorkingID).ToArray();
    public static void Run()
    {
        ReuseAndHistory(); MissingAndClone(); PrivateAndCompiler(); GeneratorAndVersionOne(); CapacityAndImport();
        Console.WriteLine("NATIVE GATE IDENTITY DETAIL literal first-free IDs, deletion/reuse target restoration, clone of missing ID, no-coordinate acquisition of ID0, complete metadata UndoRedo/rollback/history0, private parent apply/cancel, sparse DKM compiler/conversion, generator identity, capacity255 and plain seven-file reopen");
    }
    private static void ReuseAndHistory()
    {
        var doc = New(); var a = Gate(doc, 3, 5); var b = Gate(doc, 9, 11); var action = Action(doc, a);
        Expect(IDs(doc).SequenceEqual(new uint[] { 1, 2 }), "Initial source gate IDs are not literal 1,2.");
        doc.DeleteRecord(a); Point(action, 1, 1); Expect(IDs(doc).SequenceEqual(new uint[] { 2 }), "Deletion renumbered the remaining source ID.");
        var before = Snapshot(doc); var actionBefore = action.File.Data.Slice(action.Offset, action.Length).ToArray(); var count = doc.EditorChangeCount; var history = doc.GetHistory().Undo.Count; var notifications = 0;
        doc.Changed += (_, _) => notifications++;
        var replacement = Gate(doc, 20, 21); Point(action, 21, 22, actionBefore);
        Expect(IDs(doc).SequenceEqual(new uint[] { 2, 1 }) && doc.SelectedScenarioGate(doc.CreateScenarioParameterDraft(action)) == Key(replacement), "A freed ID did not acquire its original action.");
        Expect(doc.EditorChangeCount == unchecked(count + 1) && doc.GetHistory().Undo.Count == history + 1 && notifications == 1, "Derived ID reuse added a second command/count/notification.");
        var after = Snapshot(doc); doc.Undo(); Exact(doc, before); doc.Redo(); Exact(doc, after);
        doc.Undo(); var rolled = Snapshot(doc); count = doc.EditorChangeCount;
        try { doc.Edit("failed gate reuse", () => { _ = Gate(doc, 17, 18); throw new InvalidOperationException("fixture rollback"); }); }
        catch (InvalidOperationException error) when (error.Message == "fixture rollback") { }
        Exact(doc, rolled); Expect(doc.EditorChangeCount == count, "Failed insertion advanced its count.");
        doc.SetHistoryDepth(0); replacement = Gate(doc, 14, 16); Point(action, 15, 17); Expect(!doc.CanUndo && IDs(doc).SequenceEqual(new uint[] { 2, 1 }), "Zero history lost source reuse or retained an Undo.");
        var conversion = doc.PrepareProjectConversion(); var gates = conversion.Project.MapRegions().Where(region => region.Kind == MapRegionKind.Gate).Select(region => region.Record.Read(region.Record.Fields.Single(field => field.Name == "id"))).ToArray();
        Expect(gates.SequenceEqual(new[] { "2", "1" }), "DKM conversion renumbered sparse/reused gate IDs.");
        var projectedAction = conversion.Project.Records().Single(row => row.Category == "Project.ACTION"); Expect(projectedAction.Read(projectedAction.Fields.Single(field => field.Name == "param5")) == "1", "DKM lost the reused action ID.");
    }
    private static void MissingAndClone()
    {
        var doc = New(); var gate = Gate(doc, 3, 5); var original = Action(doc, gate); doc.DeleteRecord(gate); var copy = doc.DuplicateRecord(original);
        Point(original, 1, 1); Point(copy, 1, 1);
        var before = Snapshot(doc); var replacement = Gate(doc, 8, 10); Point(original, 9, 11); Point(copy, 9, 11); doc.Undo(); Exact(doc, before); doc.Redo();
        Expect(doc.SelectedScenarioGate(doc.CreateScenarioParameterDraft(copy)) == Key(replacement), "Cloning a missing target discarded param5's ID.");
        var unknown = Action(doc, null); // Payload 0,0 imports as no ID, not the newly freed ID.
        var unknownBefore = unknown.File.Data.Slice(unknown.Offset, unknown.Length).ToArray();
        _ = Gate(doc, 0, 0); Expect(unknown.File.Data.Span.Slice(unknown.Offset, unknown.Length).SequenceEqual(unknownBefore) && doc.SelectedScenarioGate(doc.CreateScenarioParameterDraft(unknown)) is null, "Unknown ID0 acquired a gate from coordinates during insertion.");
        // Deletion/reuse within one outer transaction must restore even the same point.
        var live = doc.MapRegions().Single(region => region.Kind == MapRegionKind.Gate && region.Bounds.X == 8).Record;
        doc.Edit("replace same source ID", () => { doc.DeleteRecord(live); _ = Gate(doc, 8, 10); }); Point(original, 9, 11); Point(copy, 9, 11);
    }
    private static void PrivateAndCompiler()
    {
        var doc = New(); var a = Gate(doc, 3, 5); _ = Gate(doc, 9, 11); var action = Action(doc, a); doc.DeleteRecord(a); var before = Snapshot(doc);
        using (var cancel = doc.CreateMapPropertiesSession()) { Expect(IDs(cancel.WorkingDocument).SequenceEqual(new uint[] { 2 }), "Private copy renumbered a sparse ID."); Exact(doc, before); }
        using (var apply = doc.CreateMapPropertiesSession()) doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(apply, new(36, 34, 8)));
        Point(action, 1, 1); var resized = Snapshot(doc); _ = Gate(doc, 18, 19);
        Point(action, 19, 20); Expect(IDs(doc).SequenceEqual(new uint[] { 2, 1 }), "Parent replay lost allocator metadata."); doc.Undo(); Exact(doc, resized); doc.Undo(); Exact(doc, before); doc.Redo(); doc.Redo();
        var conversion = doc.PrepareProjectConversion(); var project = conversion.Project;
        var first = project.MapRegions().First(region => region.Kind == MapRegionKind.Gate).Record; project.SetField(first, first.Fields.Single(field => field.Name == "id"), "7");
        var target = project.MapRegions().Last(region => region.Kind == MapRegionKind.Gate).Record; project.SetField(target, target.Fields.Single(field => field.Name == "id"), "9");
        var basis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
        var compiled = project.CompileLegacyProject("GateIdentityOfficial", basis); Expect(IDs(compiled).SequenceEqual(new uint[] { 7, 9 }), "Compiler working output discarded the project's sparse IDs.");
        var compiledAction = compiled.Records().Single(row => EntityReferences.EventType(row)?.Name == "ACTION_DESTROYGATE"); var last = compiled.MapRegions().Last(region => region.Kind == MapRegionKind.Gate && region.Record.Read(region.Record.Fields.Single(field => field.Name == "bGateType")) != "14").Record;
        compiled.DeleteRecord(last); var newGate = Gate(compiled, 15, 16); Expect(IDs(compiled).SequenceEqual(new uint[] { 7, 1 }), "Compiler working output did not allocate the first hole."); Point(compiledAction, 1, 1);
        for (var id = 2; id <= 9; id++) if (id != 7) _ = Gate(compiled, id + 1, id + 2);
        Point(compiledAction, 11, 12); // The eighth insertion recovers missing ID9 at (10,11).
    }
    private static void GeneratorAndVersionOne()
    {
        var doc = New(); var a = Gate(doc, 4, 5); _ = Gate(doc, 3, 3); var action = Action(doc, a); doc.DeleteRecord(a);
        doc.Paint([new(3, 3), new(10, 10)], new(29, 1, 1));
        Expect(doc.HeroGateSites().First().GateID == 2, "Generator detection substituted physical index for sparse ID2.");
        var plan = doc.PlanHeroGateGeneration(HeroGateGenerationOptions.Defaults(doc) with { Flags = HeroGateGenerationFlags.GateRelease });
        Expect(plan.Gates == 1 && plan.Items.Single(item => item.AddGate).GateID == 1, "Generator plan did not reserve literal freed ID1.");
        var before = Snapshot(doc); doc.ApplyEditorHeroGateGeneration(plan); Point(action, 11, 11); Expect(IDs(doc).SequenceEqual(new uint[] { 2, 1 }), "Generator apply changed the planned source IDs."); doc.Undo(); Exact(doc, before);
        // A literal historical v1 row selects the second physical gate. Its
        // migration must retain that owner while assigning source IDs 1,2.
        var old = New(); _ = Gate(old, 3, 5); var second = Gate(old, 9, 11); var secondAction = Action(old, second);
        var v1 = new byte[36]; void W(int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(v1.AsSpan(offset), value);
        W(0, 0x42473244); W(4, 1); W(8, 36); W(12, 1); W(16, 0); W(20, secondAction.Offset); W(24, 0); W(28, 1);
        BinaryPrimitives.WriteUInt16LittleEndian(v1.AsSpan(32), 10); BinaryPrimitives.WriteUInt16LittleEndian(v1.AsSpan(34), 12);
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var file = (NativeFile)Activator.CreateInstance(typeof(NativeFile), flags, null, new object[] { "old#GateBindings", "old#GateBindings", v1 }, null)!;
        typeof(NativeMapDocument).GetField("nativeGateBindings", flags)!.SetValue(old, file);
        typeof(NativeMapDocument).GetField("observedNativeGateBindings", flags)!.SetValue(old, null);
        var gameBytes = old.Files.ToDictionary(item => item, item => item.Data.ToArray());
        Expect(IDs(old).SequenceEqual(new uint[] { 1, 2 }) && old.SelectedScenarioGate(old.CreateScenarioParameterDraft(secondAction)) == Key(second), "Historical v1 upgrade lost the second physical gate.");
        Expect(gameBytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Metadata migration rewrote game buffers.");
        old.DeleteRecord(second); var recovered = Gate(old, 14, 16); Point(secondAction, 15, 17); Expect(IDs(old).SequenceEqual(new uint[] { 1, 2 }), "Upgraded v1 metadata did not retain a later deleted ID.");
    }
    private static void CapacityAndImport()
    {
        var doc = New(); doc.SetHistoryDepth(0);
        for (var id = 1; id <= 255; id++) _ = Gate(doc, 2 + id % 25, 2 + id % 23);
        var before = Snapshot(doc); var count = doc.EditorChangeCount;
        var refused = false; try { _ = Gate(doc, 28, 27); } catch (InvalidOperationException) { refused = true; }
        Expect(refused && doc.EditorChangeCount == count, "A 256th source gate was accepted or changed the counter."); Exact(doc, before);
        var removed = doc.MapRegions().First(region => region.Kind == MapRegionKind.Gate).Record; doc.DeleteRecord(removed); _ = Gate(doc, 28, 27);
        Expect(IDs(doc).First() == 2 && IDs(doc).Last() == 1, "Full allocator did not reuse the lowest freed ID.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2GateIdentity", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var basis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
        var project = doc.PrepareProjectConversion().Project; var compiled = project.CompileLegacyProject("GateIdentityImport", basis); var output = compiled.SaveCopy(Path.Combine(directory, "native"));
        var maps = Path.GetDirectoryName(output)!; Expect(Directory.GetFiles(maps).Length == 7, "Identity metadata escaped into the seven-file export.");
        var reopened = NativeMapDocument.Load(output); Expect(IDs(reopened).SequenceEqual(Enumerable.Range(1, 255).Select(id => (uint)id)), "Plain game import did not rebuild sequential source MapGates IDs.");
    }
}
