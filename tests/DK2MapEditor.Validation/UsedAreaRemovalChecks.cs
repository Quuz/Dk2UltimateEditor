using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class UsedAreaRemovalChecks
{
    private const uint OtherBits = 0x82000004;
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Refused(Action action)
    { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException) { return; } throw new InvalidOperationException("A stale or guarded operation was accepted."); }
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("UsedAreas", "UsedAreas", 24, 24, NativeFormat: native));
    private static NativeRecord Add(NativeMapDocument doc, MapRegionKind kind = MapRegionKind.Area, int x = 3) => doc.AddMapRegion(kind, new(x, 4, 1, 1));
    private static string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    private static void Set(NativeMapDocument doc, NativeRecord record, string name, string value) => doc.SetField(record, record.Fields.Single(field => field.Name == name), value);
    private static ScriptOwner Owner(NativeMapDocument doc, NativeRecord area) => doc.ScriptOwners().Single(owner => owner.Scope == 4 && owner.Record.File == area.File && owner.Record.Offset == area.Offset);
    private static ScriptEdge Point(NativeMapDocument doc, NativeRecord area, bool nonempty)
    {
        var owner = Owner(doc, area); var root = doc.EnsureScriptScope(owner);
        if (nonempty)
        {
            var type = LegacyEventSchemas.All.First(type => type.RecordSignature == 0xD5 && (LegacyEventSchemas.AllowedScopes(type) & 4) != 0);
            var record = doc.AddScenarioEvent(type.RecordSignature, new(0, 0), type); doc.BindScriptScope(owner, ScriptGraph.ID(record));
        }
        return doc.FindScriptScope(owner)!;
    }
    private static Dictionary<string, byte[]> State(NativeMapDocument doc) => doc.IsLegacyProject
        ? doc.Files.ToDictionary(file => file.ExportPath, file => file.Data.ToArray())
        : doc.PrepareProjectConversion().SourceFiles.ToDictionary(file => file.ExportPath, file => file.Bytes.ToArray());
    private static void Exact(NativeMapDocument doc, Dictionary<string, byte[]> expected)
    {
        var current = State(doc); Expect(current.Count == expected.Count && expected.All(pair => current.TryGetValue(pair.Key, out var bytes) && bytes.AsSpan().SequenceEqual(pair.Value)), "Removal changed unrelated bytes, lost metadata, or failed complete Undo.");
    }
    private static ScriptEdge Orphan(NativeMapDocument doc) => doc.ScriptBindings().Single(binding => binding.Root.Scope == 4 && binding.Owner is null).Root;
    public static void Run()
    {
        foreach (var native in new[] { false, true }) foreach (var nonempty in new[] { false, true }) Decisions(native, nonempty);
        foreach (var native in new[] { false, true }) { References(native); OrphanLifecycle(native); AllSkipped(native); }
        NativePrivateAndConversion(); SourceSpecialCases(); PhysicalConversionOrder(); DuplicateNativePoint();
        Console.WriteLine("USED AREA REMOVAL DETAIL native/project empty/nonempty Yes/No/Cancel, literal0x100/default-only remember/reset, retained point/event operands, all-skipped count/Undo, source wrong-scope/zero-ID predicates, orphan graph identity/unused/owner handoff/reuse, private copies, DKM/archive/compiler/seven-file and exact state Undo/Redo");
    }
    private static void Decisions(bool native, bool nonempty)
    {
        var doc = New(native); var used = Add(doc); var unused = Add(doc, x: 8); var root = Point(doc, used, nonempty);
        var before = State(doc); var count = doc.EditorChangeCount; var history = doc.GetHistory().Undo.Count;
        using var policy = new EditorRegionRemovalPolicy(OtherBits);
        using (var cancel = policy.Begin(doc, [unused, used]))
        {
            Expect(cancel.Stage == EditorRegionRemovalStage.Confirmation && cancel.UsedCount == 1, "Stored empty/nonempty point did not mark one used area.");
            Expect(cancel.Accept(true) == 0 && !cancel.IsClosed && !cancel.IsAccepted && cancel.Stage == EditorRegionRemovalStage.UsedAreas && cancel.RequiresPrompt, "First Yes did not advance without committing.");
            Expect(!cancel.Decline(true), "Remembered No was accepted."); Exact(doc, before);
        }
        Expect(policy.Flags == (OtherBits | 16) && doc.EditorChangeCount == count && doc.GetHistory().Undo.Count == history, "Second Cancel changed map/history or forgot the first remembered reply.");
        using (var no = policy.Begin(doc, [unused, used]))
        {
            Expect(!no.RequiresPrompt, "General remembered Yes was lost."); no.Accept();
            Expect(no.Stage == EditorRegionRemovalStage.UsedAreas && no.Decline() && no.RemovedCount == 1 && no.IsAccepted, "Second No did not keep used/delete unused.");
        }
        Expect(doc.EditorRegionRemovalRecords().Count == 1 && doc.HasStoredScriptScope(doc.FindScriptScope(Owner(doc, used))!), "No removed a used area/empty point.");
        Expect(doc.EditorChangeCount == unchecked(count + 1) && doc.GetHistory().Undo.Count == history + 1, "Second No did not make exactly one operation.");
        doc.Undo(); Exact(doc, before);
        using (var yes = policy.Begin(doc, [used, unused]))
        {
            yes.Accept(); Expect(yes.Stage == EditorRegionRemovalStage.UsedAreas && yes.RequiresPrompt, "Second question suppressed without acceptance.");
            Expect(yes.Accept(true) == 2 && yes.IsClosed && yes.IsAccepted, "Second Yes did not delete all selected regions.");
        }
        Expect(policy.Flags == (OtherBits | 0x110) && Orphan(doc).Target == root.Target && doc.EditorRegionRemovalRecords().Count == 0, "Yes lost the retained area point or altered unrelated bits.");
        var after = State(doc); doc.Undo(); Exact(doc, before); doc.Redo(); Exact(doc, after);
        var other = New(!native); var area = Add(other); Point(other, area, nonempty: false);
        using (var auto = policy.Begin(other, [area])) { Expect(!auto.RequiresPrompt, "First remembered reply did not cross documents."); auto.Accept(); Expect(!auto.RequiresPrompt && auto.Stage == EditorRegionRemovalStage.UsedAreas, "Second bit did not cross documents."); auto.Accept(); }
        policy.ResetSuppression(); Expect(policy.Flags == OtherBits, "Reset changed unrelated request bits or left a deletion bit set.");
        var staleDoc = New(native); var staleArea = Add(staleDoc); Point(staleDoc, staleArea, nonempty: false);
        using var stale = policy.Begin(staleDoc, [staleArea]); stale.Accept(); staleDoc.SetMapRegionBounds(staleArea, new(5, 5, 1, 1));
        Refused(() => stale.Accept(true)); Expect(policy.Flags == OtherBits, "A stale second question saved a preference.");
    }
    private static void AllSkipped(bool native)
    {
        var doc = New(native); var area = Add(doc); Point(doc, area, false); var before = State(doc); var count = doc.EditorChangeCount; var history = doc.GetHistory().Undo.Count;
        using var policy = new EditorRegionRemovalPolicy(); using var request = policy.Begin(doc, [area]); request.Accept();
        Expect(request.KeepUsed() == 0 && request.IsAccepted, "All-skipped No was not an accepted source command."); Exact(doc, before);
        Expect(doc.EditorChangeCount == unchecked(count + 1) && doc.GetHistory().Undo.Count == history + 1, "All-skipped source outer Undo/count was omitted.");
        doc.Undo(); Exact(doc, before); Expect(doc.EditorChangeCount == count, "Empty Undo did not restore the counter.");
        doc.Redo(); Exact(doc, before);
        doc.SetHistoryDepth(0); using var depth0 = policy.Begin(doc, [area]); depth0.Accept(); depth0.KeepUsed(); Expect(!doc.CanUndo, "All-skipped history0 retained a snapshot.");
    }
    private static void References(bool native)
    {
        foreach (var kind in new[] { MapRegionKind.Area, MapRegionKind.Effect, MapRegionKind.Gate })
        {
            var doc = New(native); var region = Add(doc, kind);
            var rule = EntityReferences.EventRules.First(rule => rule.Kind == (kind == MapRegionKind.Area ? EntityKind.Area : kind == MapRegionKind.Effect ? EntityKind.Effect : EntityKind.Gate));
            var action = doc.AddScenarioEvent(rule.Event.RecordSignature, new(0, 0), rule.Event);
            if (native && kind == MapRegionKind.Gate) doc.ApplyScenarioEditorParameterDraft(doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(action), region.File.ExportPath + ":" + region.Offset));
            else Set(doc, action, native ? "Param" + rule.Parameter[5..] : rule.Parameter, "1");
            var payload = action.File.Data.Slice(action.Offset, action.Length).ToArray();
            using var policy = new EditorRegionRemovalPolicy(); using var request = policy.Begin(doc, [region]);
            Expect(request.UsedCount == 0 && request.Accept() == 1 && request.IsClosed, "Action-only area/effect/gate reference opened a used-area question.");
            var current = ScriptGraph.Build(doc).Events[ScriptGraph.ID(action)].Record;
            if (!(native && kind == MapRegionKind.Gate)) Expect(current.File.Data.Span.Slice(current.Offset, current.Length).SequenceEqual(payload), "Source deletion rewrote an event operand.");
        }
    }
    private static void OrphanLifecycle(bool native)
    {
        var doc = New(native); var area = Add(doc); var point = Point(doc, area, true); var id = point.Target;
        using var policy = new EditorRegionRemovalPolicy(); using (var request = policy.Begin(doc, [area])) { request.Accept(); request.Accept(); }
        Expect(Orphan(doc).Target == id && doc.ScriptOwners().All(owner => owner.Scope != 4), "Deleted area remained a physical owner or lost its root.");
        var plan = doc.PlanUnusedScriptRemoval(); Expect(!plan.Events.Any(item => item.ID == id), "Unused cleanup offered the orphan-rooted event.");
        var record = ScriptGraph.Build(doc).Events[id].Record; var newID = (ushort)(id + 7);
        Set(doc, record, native ? "ID" : "id", newID.ToString()); Expect(Orphan(doc).Target == newID, "Event identity remapping ignored the orphan root.");
        Refused(() => doc.DeleteRecord(ScriptGraph.Build(doc).Events[newID].Record));
        if (native)
        {
            var beforeWizard = State(doc); var context = doc.CreateScenarioCreationContext();
            using var session = doc.CreateScenarioCreationSession(context, 0xD5, EntityReferences.EventType(record), Orphan(doc));
            var added = doc.ApplyScenarioEditorCreation(doc.PrepareScenarioCreation(session));
            Expect(Orphan(doc).Target == ScriptGraph.ID(added), "Creation wizard did not insert at the orphan root.");
            doc.Undo(); Exact(doc, beforeWizard);
        }
        var beforeReuse = State(doc); var replacement = Add(doc, x: 13);
        var attached = doc.FindScriptScope(Owner(doc, replacement))!; Expect(attached.Target == newID && doc.ScriptBindings().Count(binding => binding.Root.Scope == 4 && binding.Owner is null) == 0, "First-free reused ID did not recover the stored point.");
        doc.Undo(); Exact(doc, beforeReuse); Expect(Orphan(doc).Target == newID, "Undo of ID reuse lost its orphan point.");
        var beforeDeletePoint = State(doc); doc.DeleteEditorScenarioPoint(Orphan(doc));
        Expect(doc.ScriptBindings().All(binding => binding.Root.Scope != 4) && !ScriptGraph.Build(doc).Events.ContainsKey(newID), "Explicit orphan point removal did not remove its events/root.");
        doc.Undo(); Exact(doc, beforeDeletePoint);
        var destination = Add(doc, x: 15); // ID1 reacquires; move to another physical ID.
        var other = Add(doc, x: 18); var root = doc.FindScriptScope(Owner(doc, destination))!;
        doc.ReassignScriptScope(root, Owner(doc, other));
        var former = doc.FindScriptScope(Owner(doc, destination));
        Expect(doc.FindScriptScope(Owner(doc, other))!.Target == newID && (former is null || !doc.HasStoredScriptScope(former)), "Area point handoff left a second stored point.");
    }
    private static void NativePrivateAndConversion()
    {
        var doc = New(true); var area = Add(doc); Point(doc, area, true); using var policy = new EditorRegionRemovalPolicy();
        using (var request = policy.Begin(doc, [area])) { request.Accept(); request.Accept(); }
        var before = State(doc);
        using (var cancel = doc.CreateMapPropertiesSession())
        {
            Expect(Orphan(cancel.WorkingDocument).Target == Orphan(doc).Target, "Private properties clone lost orphan metadata.");
            cancel.WorkingDocument.RemoveScriptScope(Orphan(cancel.WorkingDocument)); Exact(doc, before);
            Refused(() => doc.PrepareEditorMapProperties(cancel, new(24, 24, 8)));
        }
        using (var apply = doc.CreateMapPropertiesSession())
        {
            doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(apply, new(26, 26, 8)));
        }
        Expect(Orphan(doc).Target != 0 && doc.Width == 26, "Parent resize Apply lost orphan metadata."); doc.Undo(); Exact(doc, before);
        var directory = Path.Combine(Path.GetTempPath(), "DK2UsedArea-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "Orphan.dkm");
        using (var save = new EditorSaveSession(doc))
        {
            Expect(save.Save(save.PrepareSaveAs(path), acceptLosses: true) && !save.Document.HasChanges, "Successful native conversion did not adopt a clean DKM.");
            var archive = EditorSourceArchive.Read(save.LastSave!.SourceArchives.Single());
            Expect(archive.SourceFiles.Count(file => file.ExportPath.EndsWith("#AreaScopes", StringComparison.Ordinal)) == 1, "DKM source archive omitted working area scopes.");
        }
        var reopened = NativeMapDocument.Load(path); Expect(Orphan(reopened).Target != 0, "Native-to-DKM save/reopen lost orphan root.");
        var basis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
        var compiled = reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78", basis); Expect(Orphan(compiled).Target == Orphan(reopened).Target, "Compiler dropped the editor orphan point.");
        var output = compiled.SaveCopy(Path.Combine(directory, "native")); Expect(Directory.GetFiles(Path.GetDirectoryName(output)!).Length == 7, "Orphan metadata added a game file.");
        // Empty points are editor data; preserve them in DKM even though native bytes have no marker.
        var empty = New(true); var emptyArea = Add(empty); var root = Point(empty, emptyArea, false);
        Expect(empty.HasStoredScriptScope(root), "Explicit empty native point was not stored.");
        var project = empty.PrepareProjectConversion().Project; Expect(project.ScriptBindings().Any(binding => binding.Root.Scope == 4 && binding.Root.Target == 0), "Conversion dropped the explicit empty native point.");
        empty.RemoveScriptScope(root); Expect(!empty.HasStoredScriptScope(root), "Explicit removal retained an empty point marker."); empty.Undo(); Expect(empty.HasStoredScriptScope(root), "Undo did not restore empty point presence.");
    }
    private static void SourceSpecialCases()
    {
        var project = New(false); var area = Add(project); var link = project.AddLegacyRecord("TriggerLink", new(0, 0));
        Set(project, link, "node.type", "1"); Set(project, link, "area", "1"); // source ignores node.type while scanning area operand
        using var policy = new EditorRegionRemovalPolicy(); using var request = policy.Begin(project, [area]); Expect(request.UsedCount == 1, "Source wrong-scope stored area operand was ignored.");
        var native = New(true); var zero = Add(native); Point(native, zero, true);
        Expect(MemoryMarshal.TryGetArray(zero.File.Data, out var bytes), "Zero-ID fixture has no buffer."); bytes.Array![bytes.Offset + zero.Offset + 30] = 0;
        using var zeroPolicy = new EditorRegionRemovalPolicy(); using var zeroRequest = zeroPolicy.Begin(native, [zero]); Expect(zeroRequest.UsedCount == 0, "Source area ID0 acquired a used-point question.");
    }
    private static void PhysicalConversionOrder()
    {
        var doc = New(true);
        var creature = doc.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEKEEPER"), new(3, 5));
        var area = Add(doc, x: 8);
        var band = doc.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(14, 5));
        var type = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
        foreach (var row in new[] { creature, area, band })
        {
            var owner = doc.ScriptOwners().Single(owner => owner.Record.File == row.File && owner.Record.Offset == row.Offset && owner.Record.Length == row.Length);
            var item = doc.AddScenarioEvent(0xD5, new(0, 0), type); doc.BindScriptScope(owner, ScriptGraph.ID(item));
        }
        var before = State(doc); var converted = doc.PrepareProjectConversion().Project;
        Expect(ScriptGraph.Build(converted).Roots.Select(root => root.Scope).SequenceEqual(new uint[] { 2, 4, 8 }), "Native area metadata reordered live scope points relative to other physical things.");
        Exact(doc, before);
    }
    private static void DuplicateNativePoint()
    {
        foreach (var nonempty in new[] { true, false }) foreach (var withOrphan in new[] { true, false })
        {
            var doc = New(true); NativeRecord? reserved = withOrphan ? Add(doc) : null;
            var reservedRoot = reserved is null ? (ushort)0 : Point(doc, reserved, true).Target;
            var source = Add(doc, x: 8); var sourceRoot = Point(doc, source, nonempty).Target;
            if (reserved is not null)
            {
                using var policy = new EditorRegionRemovalPolicy(); using var request = policy.Begin(doc, [reserved]); request.Accept(); request.Accept();
                source = doc.EditorRegionRemovalRecords().Single();
            }
            var before = State(doc); var count = doc.EditorChangeCount; var history = doc.GetHistory().Undo.Count;
            var copy = doc.DuplicateRecord(source); var copiedRoot = doc.FindScriptScope(Owner(doc, copy))!;
            Expect(doc.HasStoredScriptScope(copiedRoot), "Duplicating an explicit empty native area point lost its presence marker.");
            if (nonempty) Expect(copiedRoot.Target != 0 && copiedRoot.Target != sourceRoot && copiedRoot.Target != reservedRoot, "A duplicated area acquired an unrelated orphan's point instead of its cloned branch.");
            else Expect(copiedRoot.Target == 0, "Duplicating an empty point acquired an unrelated orphan branch.");
            if (withOrphan) Expect(Orphan(doc).Target == reservedRoot, "Area duplication consumed the older orphan scope.");
            Expect(doc.FindScriptScope(Owner(doc, source))!.Target == sourceRoot, "Area duplication changed the source point.");
            Expect(doc.EditorChangeCount == unchecked(count + 1) && doc.GetHistory().Undo.Count == history + 1, "Duplicating an area point added a second operation.");
            var after = State(doc); doc.Undo(); Exact(doc, before); doc.Redo(); Exact(doc, after);
        }
        Console.WriteLine("NATIVE AREA POINT DUPLICATION DETAIL four empty/nonempty x orphan/free-ID cases; cloned branch and explicit empty marker preserved, older orphan retained, unique physical scope addresses and one complete Undo/Redo");
    }
}
