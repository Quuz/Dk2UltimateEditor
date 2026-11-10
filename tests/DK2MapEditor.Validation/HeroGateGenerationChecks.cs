using System.Globalization;
using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class HeroGateGenerationChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    private static void Set(NativeMapDocument document, NativeRecord record, string name, string value) => document.SetField(record, record.Fields.Single(field => field.Name == name), value);
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes)
    { Expect(document.Files.Count == bytes.Count, "Generation changed the file set."); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Generation/undo changed unrelated bytes: " + file.ExportPath); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException) { return; }
        throw new Exception("Unsafe generation was accepted.");
    }
    private static NativeMapDocument Fixture(bool legacy)
    {
        var document = NativeMapDocument.CreateNewMap(new("GateTest", "GateTest", 32, 32, Players: 3, NativeFormat: !legacy));
        document.Edit("Four source gate types", () =>
        {
            document.Paint([new(3, 3)], new(29, 1, 1));
            document.Paint(MapDrawing.Rectangle(new(8, 8, 2, 2), true), new(33, 1, 1));
            document.Paint(MapDrawing.Rectangle(new(15, 15, 3, 1), true), new(37, 1, 1));
            document.Paint(MapDrawing.Rectangle(new(23, 23, 1, 3), true), new(37, 1, 2));
        });
        return document;
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2-GateChecks-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            Expect(NativeMapDocument.FormatHeroGateText("%g/%G/%p/%P %a/%A/%z/%Z %% %q end%", 17, 23, 64) == "17/17/17/17 23/23/23/23 %  end", "Original template aliases/unknown/trailing percent behavior differs.");
            Expect(NativeMapDocument.FormatHeroGateText("text %a tail", 1, null, 128) == "", "Missing template identity must clear the whole text.");
            Expect(NativeMapDocument.FormatHeroGateText(new string('x', 100), 1, 2, 64).Length == 60 && NativeMapDocument.FormatHeroGateText(new string('x', 59) + "%grest", 255, 2, 64).Length == 62, "Source formatter's pre-token truncation changed.");
            foreach (var legacy in new[] { false, true })
            {
                var document = Fixture(legacy); var defaults = HeroGateGenerationOptions.Defaults(document);
                var before = Snapshot(document); var changes = 0; document.Changed += (_, _) => changes++;
                var sites = document.HeroGateSites();
                Expect(sites.Select(site => site.Type).SequenceEqual(new[] { 2, 3, 6, 7 }) && sites.Select(site => site.CannotDestroy).SequenceEqual(new[] { true, false, true, true }), "Gate scan/order/protection differs from source.");
                Expect(defaults.Keepers == 3, "Generator lost the document's declared keepers."); Exact(document, before);
                var combinations = 0;
                for (var bits = 0; bits < 64; bits++)
                {
                    var flags = (HeroGateGenerationFlags)((uint)(bits & 7) | ((uint)(bits & 24) << 6) | ((uint)(bits & 32) << 15));
                    if ((bits & 32) != 0 && (bits & 10) != 10) continue;
                    combinations++;
                    for (var keepers = 1; keepers <= 5; keepers++)
                    {
                        var plan = document.PlanHeroGateGeneration(defaults with { Flags = flags, Keepers = keepers }); Exact(document, before);
                        var gates = (bits & 4) != 0 ? 4 : ((bits & 1) != 0 ? 2 : 0) + ((bits & 2) != 0 ? 1 : 0);
                        var areas = (bits & 16) != 0 ? 4 : (bits & 8) != 0 ? 1 : 0;
                        var conditions = (bits & 32) != 0 ? keepers : 0;
                        Expect(plan.Gates == gates && plan.Areas == areas && plan.Conditions == conditions && plan.Actions == conditions, "Generator option matrix counts differ from independent source rules.");
                        var notifications = changes; var count = document.EditorChangeCount; var history = document.GetHistory();
                        document.ApplyEditorHeroGateGeneration(plan);
                        var increments = (uint)(gates + areas + conditions * 2 + (conditions > 0 ? 1 : 0));
                        Expect(document.EditorChangeCount == unchecked(count + increments), "Source generator increments differ for option matrix.");
                        Expect(document.GetHistory().Undo.Count == history.Undo.Count + (plan.HasWork ? 1 : 0), "Generator created multiple/phantom Undo entries.");
                        Expect(changes == notifications + (plan.HasWork ? 1 : 0), "Generation did not use one transaction or no-op changed history.");
                        Expect(document.TileFile.Data.Span.SequenceEqual(before[document.TileFile]), "Generator changed map tiles.");
                        var expectedGatePoints = new[] { new TileRect(3, 3, 1, 1), new(8, 8, 1, 1), new(16, 15, 1, 1), new(23, 24, 1, 1) };
                        var expectedAreaBounds = new[] { new TileRect(3, 3, 1, 1), new(7, 7, 4, 4), new(16, 15, 1, 1), new(23, 24, 1, 1) };
                        var selectedGates = expectedGatePoints.Where((_, i) => (bits & 4) != 0 || i == 1 && (bits & 2) != 0 || i >= 2 && (bits & 1) != 0);
                        var selectedAreas = expectedAreaBounds.Where((_, i) => (bits & 16) != 0 || i == 1 && (bits & 8) != 0);
                        Expect(document.MapRegions().Where(region => region.Kind == MapRegionKind.Gate).Select(region => region.Bounds).SequenceEqual(selectedGates), "Generated gate centers differ from original types.");
                        Expect(document.MapRegions().Where(region => region.Kind == MapRegionKind.Area).Select(region => region.Bounds).SequenceEqual(selectedAreas), "Generated area rectangles differ from original types.");
                        CheckEvents(document, keepers, conditions > 0);
                        Expect(!document.PlanHeroGateGeneration(defaults with { Flags = flags, Keepers = keepers }).HasWork, "Second generation duplicated structures/events.");
                        if (plan.HasWork)
                        {
                            var after = Snapshot(document); document.Undo(); Exact(document, before);
                            Expect(document.EditorChangeCount == count, "Undo lost the pre-generation source counter.");
                            // Preview is read-only even when redo exists.
                            _ = document.PlanHeroGateGeneration(defaults); Expect(document.CanRedo, "Preview consumed redo.");
                            document.Redo(); Exact(document, after);
                            Expect(document.EditorChangeCount == unchecked(count + increments), "Redo lost generated source increments.");
                            document.Undo(); Exact(document, before);
                        }
                    }
                }
                Expect(combinations == 40, "Not all valid source flag combinations were tested.");
                var options = defaults with { Gate = new("G%g-A%a", "Gate %% %p", 0x123456, 0x654321), Area = new("A%z-G%P", "Area %A", 0x112233, 0x332211) };
                var full = document.PlanHeroGateGeneration(options); document.ApplyEditorHeroGateGeneration(full); CheckEvents(document, 3, true);
                if (legacy)
                    foreach (var region in document.MapRegions())
                    {
                        var id = Read(region.Record, "id"); var template = region.Kind == MapRegionKind.Gate ? options.Gate : options.Area;
                        Expect(Read(region.Record, "rect.color") == template.Color.ToString(CultureInfo.InvariantCulture) && Read(region.Record, "rect.textcolor") == template.TextColor.ToString(CultureInfo.InvariantCulture), "Template colors were swapped/lost.");
                        Expect(Read(region.Record, "name") == (region.Kind == MapRegionKind.Gate ? $"G{id}-A{id}" : $"A{id}-G{id}"), "Template IDs/namespace differ.");
                    }
                var source = Snapshot(document); var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory, "generated-" + legacy));
                var reopened = NativeMapDocument.Load(saved);
                // KMAP section is a serialized projection of TileFile, not an
                // authoritative edited buffer; export rebuilds it deliberately.
                if(legacy)SaveProjectionChecks.RoundTrip(document,reopened,source.ToDictionary(pair=>pair.Key.ExportPath,pair=>pair.Value));
                else foreach(var file in reopened.Files)Expect(file.Data.Span.SequenceEqual(document.Files.Single(original=>original.ExportPath==file.ExportPath).Data.Span),"Native gate generation save/reopen was not lossless.");
                if (legacy)
                {
                    var savedAgain = SaveProjectionChecks.SaveCopy(reopened, Path.Combine(directory, "generated-repeat"));
                    Expect(SaveProjectionChecks.BeforeEquals(reopened,File.ReadAllBytes(saved)), "Reopened generated project did not serialize identically.");
                }
                CheckEvents(reopened, 3, true); Exact(document, source);
                if (legacy) CheckEvents(document.CompileLegacyProject("GateCompiled", NativeMapDocument.Create(32, 32, "Basis")), 3, true);
                // Existing records get neither new names/colors nor replacement events.
                var noWork = document.PlanHeroGateGeneration(options with { Gate = new("Changed", "Changed", 0, 0) });
                Expect(!noWork.HasWork, "Existing generated records were scheduled for replacement."); document.ApplyEditorHeroGateGeneration(noWork); Exact(document, source);
                var stale = document.PlanHeroGateGeneration(defaults); document.Paint([new(2, 2)], new(4, 2, 1)); var stable = Snapshot(document);
                Reject(() => document.ApplyEditorHeroGateGeneration(stale)); Exact(document, stable);
                Reject(() => Fixture(legacy).ApplyEditorHeroGateGeneration(stale));
                Reject(() => document.PlanHeroGateGeneration(defaults with { Keepers = 0 }));
                Reject(() => document.PlanHeroGateGeneration(defaults with { Flags = HeroGateGenerationFlags.Events }));
                Reject(() => document.PlanHeroGateGeneration(defaults with { Flags = (HeroGateGenerationFlags)0x80000000 }));
                Reject(() => document.PlanHeroGateGeneration(defaults with { Gate = defaults.Gate with { Name = "Ворота" } })); Exact(document, stable);
                CheckScopeCounters(legacy);
                CheckExistingAndRollback(legacy);
                CheckDetection(legacy);
            }
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void CheckScopeCounters(bool legacy)
    {
        var document = Fixture(legacy); var options = HeroGateGenerationOptions.Defaults(document);
        var primitives = options with { Flags = HeroGateGenerationFlags.All & ~HeroGateGenerationFlags.Events };
        var before = Snapshot(document); var initial = document.EditorChangeCount;
        // Raw API keeps its existing atomic-operation semantics.
        document.ApplyHeroGateGeneration(document.PlanHeroGateGeneration(primitives));
        Expect(document.EditorChangeCount == unchecked(initial + 1), "Generic generator semantics changed.");
        document.Undo(); Exact(document, before);
        document.ApplyEditorHeroGateGeneration(document.PlanHeroGateGeneration(primitives));
        Expect(document.EditorChangeCount == unchecked(initial + 8), "Four gates/four areas must count separately.");
        var area = document.MapRegions().Single(region => region.Kind == MapRegionKind.Area && region.Bounds == new TileRect(7, 7, 4, 4)).Record;
        var owner = document.ScriptOwners().Single(candidate => candidate.Scope == 4 && candidate.Record.File == area.File && candidate.Record.Offset == area.Offset);
        var count = document.EditorChangeCount; before = Snapshot(document);
        document.ApplyEditorHeroGateGeneration(document.PlanHeroGateGeneration(options));
        Expect(document.EditorChangeCount == unchecked(count + 7), "Absent logical scope plus six events must count seven.");
        document.Undo(); Exact(document, before); Expect(document.EditorChangeCount == count, "Scope generation Undo lost counter.");
        var root = document.EnsureScriptScope(owner); // Both formats now retain explicit empty logical points.
        count = document.EditorChangeCount; before = Snapshot(document);
        document.ApplyEditorHeroGateGeneration(document.PlanHeroGateGeneration(options));
        Expect(document.HasStoredScriptScope(root) && document.EditorChangeCount == unchecked(count + 6u), "An explicit empty logical point was counted as newly allocated.");
        document.Undo(); Exact(document, before);
        root = document.FindScriptScope(owner)!;
        document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        count = document.EditorChangeCount; before = Snapshot(document);
        document.ApplyEditorHeroGateGeneration(document.PlanHeroGateGeneration(options));
        Expect(document.EditorChangeCount == unchecked(count + 6), "Existing logical scope was counted as newly allocated.");
        var revision = document.EditRevision; var history = document.GetHistory(); var generated = Snapshot(document);
        document.ApplyEditorHeroGateGeneration(document.PlanHeroGateGeneration(options));
        Expect(document.EditorChangeCount == unchecked(count + 6) && document.EditRevision == revision
            && document.GetHistory().Undo.Count == history.Undo.Count, "No-work generation changed counter/revision/history.");
        Exact(document, generated); document.Undo(); Exact(document, before);
    }
    private static void CheckEvents(NativeMapDocument document, int keepers, bool expected)
    {
        var graph = ScriptGraph.Build(document);
        Expect(graph.DuplicateIDs.Count == 0 && graph.Events.Count == (expected ? keepers * 2 : 0), "Wrong generated script count/identities.");
        if (!expected) return;
        var root = graph.Roots.Single(edge => edge.Scope == 4 && edge.Target != 0); var id = root.Target; var legacy = document.IsLegacyProject;
        for (var keeper = 3; keeper < keepers + 3; keeper++)
        {
            var condition = graph.Events[id];
            Expect(condition.Type?.Name == "TRIGGER_CLAIMAREA" && Read(condition.Record, legacy ? "param1" : "Param1") == "5" && Read(condition.Record, legacy ? "param2" : "Param2") == keeper.ToString() && Read(condition.Record, legacy ? "param5" : "Param5") == "12" && Read(condition.Record, legacy ? "repeat" : "Repeat") == "1", "Generated claim-area arguments are incorrect.");
            var action = graph.Events[condition.Edges.Single(edge => edge.Field.Name is "and" or "ChildAnd").Target];
            Expect(action.Type?.Name == "ACTION_DESTROYGATE" && Read(action.Record, legacy ? "one" : "Repeat") == "1" && action.Edges.Single().Target == 0, "Generated destruction action differs from source.");
            var gate = document.MapRegions().Single(region => region.Kind == MapRegionKind.Gate && region.Bounds.X == 8 && region.Bounds.Y == 8);
            if (legacy)
            {
                ushort Previous(NativeRecord record) => BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[(record.Offset + LegacyProjectSchemas.Size("NODE"))..]);
                Expect(Read(action.Record, "param5") == Read(gate.Record, "id") && Previous(action.Record) == id, "Project destruction ID/backlink incorrect.");
                Expect(Previous(condition.Record) == (keeper == 3 ? 0 : id - 1), "Project condition backlink incorrect.");
            }
            else
            {
                Expect(Read(action.Record, "NativeArgument5") == "9" && Read(action.Record, "NativeArgument7") == "9", "Native destruction must use 1-based gate coordinates, not IDs.");
                Expect(action.Record.File.Data.Span.Slice(action.Record.Offset + 8, 4).SequenceEqual(new byte[4]), "Native action reserved bytes changed.");
            }
            id = condition.Edges.Single(edge => edge.Field.Name is "or" or "NextOr").Target;
        }
        Expect(id == 0, "Generated chain was not terminated.");
    }
    private static void CheckExistingAndRollback(bool legacy)
    {
        var document = Fixture(legacy); var options = HeroGateGenerationOptions.Defaults(document);
        var primitives = document.PlanHeroGateGeneration(options with { Flags = HeroGateGenerationFlags.All & ~HeroGateGenerationFlags.Events });
        document.ApplyEditorHeroGateGeneration(primitives); var before = Snapshot(document);
        var eventsOnly = document.PlanHeroGateGeneration(options);
        Expect(eventsOnly.Gates == 0 && eventsOnly.Areas == 0 && eventsOnly.Conditions == 3, "Event-only repair is unavailable.");
        document.ApplyEditorHeroGateGeneration(eventsOnly); CheckEvents(document, 3, true); document.Undo(); Exact(document, before);
        var area = document.MapRegions().Single(region => region.Kind == MapRegionKind.Area && region.Bounds == new TileRect(7, 7, 4, 4)).Record;
        var owner = document.ScriptOwners().Single(candidate => candidate.Scope == 4 && candidate.Record.File == area.File && candidate.Record.Offset == area.Offset);
        var root = document.EnsureScriptScope(owner);
        var existing = document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        var existingID = ScriptGraph.ID(existing); var existingBytes = existing.File.Data.Span.Slice(existing.Offset, existing.Length).ToArray();
        before = Snapshot(document); document.ApplyEditorHeroGateGeneration(document.PlanHeroGateGeneration(options));
        var graph = ScriptGraph.Build(document); var head = graph.Events[existingID];
        Expect(graph.Events.Count == 7 && head.Edges.Single(edge => edge.Field.Name is "or" or "NextOr").Target != 0 && document.FindScriptScope(owner)!.Target == existingID, "Generation replaced the existing scenario instead of appending.");
        var modified = head.Record.File.Data.Span.Slice(head.Record.Offset, head.Record.Length).ToArray();
        var nextField = head.Edges.Single(edge => edge.Field.Name is "or" or "NextOr").Field;
        existingBytes.AsSpan(nextField.Offset, nextField.Size).CopyTo(modified.AsSpan(nextField.Offset));
        Expect(modified.AsSpan().SequenceEqual(existingBytes), "Generation changed existing condition payload.");
        document.Undo(); Exact(document, before);
        // Malformed child: fail after planning, rolling back primitives/scripts atomically.
        existing = ScriptGraph.Build(document).Events[existingID].Record;
        Set(document, existing, legacy ? "and" : "ChildAnd", "12345"); before = Snapshot(document); var undo = document.CanUndo; var redo = document.CanRedo;
        var failedCount = document.EditorChangeCount; var failedHistory = document.GetHistory(); var failedRevision = document.EditRevision;
        var plan = document.PlanHeroGateGeneration(options); Reject(() => document.ApplyEditorHeroGateGeneration(plan)); Exact(document, before);
        Expect(document.CanUndo == undo && document.CanRedo == redo && document.EditorChangeCount == failedCount
            && document.EditRevision == failedRevision && document.GetHistory().Undo.Count == failedHistory.Undo.Count
            && document.GetHistory().Redo.Count == failedHistory.Redo.Count, "Failed generation changed undo/redo/counter/revision.");
        Set(document, existing, legacy ? "and" : "ChildAnd", "0");
        Set(document, existing, legacy ? "or" : "NextOr", existingID.ToString()); before = Snapshot(document);
        Reject(() => document.ApplyEditorHeroGateGeneration(document.PlanHeroGateGeneration(options))); Exact(document, before);
        var exhausted = Fixture(legacy);
        exhausted.Edit("Exhaust area IDs", () => { for (var index = 0; index < 255; index++) exhausted.AddMapRegion(MapRegionKind.Area, new(1, 1, 1, 1)); });
        before = Snapshot(exhausted); Reject(() => exhausted.PlanHeroGateGeneration(options)); Exact(exhausted, before);
        var partial = Fixture(legacy); area = partial.AddMapRegion(MapRegionKind.Area, new(7, 7, 4, 4));
        owner = partial.ScriptOwners().Single(candidate => candidate.Scope == 4 && candidate.Record.File == area.File && candidate.Record.Offset == area.Offset);
        root = partial.EnsureScriptScope(owner);
        existing = partial.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        Set(partial, existing, legacy ? "and" : "ChildAnd", "12345");
        before = Snapshot(partial); var notifications = 0; partial.Changed += (_, _) => notifications++;
        plan = partial.PlanHeroGateGeneration(options);
        Expect(plan.Gates == 4 && plan.Areas == 3 && plan.Conditions == 3, "Partial rollback fixture did not schedule primitives.");
        Reject(() => partial.ApplyEditorHeroGateGeneration(plan)); Exact(partial, before);
        Expect(notifications == 0, "Failed generation published partial additions.");
        var orphan = Fixture(legacy); area = orphan.AddMapRegion(MapRegionKind.Area, new(1, 1, 1, 1));
        Set(orphan, area, legacy ? "id" : "bAreaID", "2"); Set(orphan, area, legacy ? "nextid" : "bAreaNextID", "1");
        before = Snapshot(orphan); Reject(() => orphan.PlanHeroGateGeneration(options)); Exact(orphan, before);
        // Imported unrenderable records still reserve their identity.
        var malformed = Fixture(legacy); area = malformed.AddMapRegion(MapRegionKind.Area, new(1, 1, 1, 1));
        Set(malformed, area, legacy ? "rect.x1" : "dwAreaLocationStartX", legacy ? int.MinValue.ToString() : uint.MaxValue.ToString());
        before = Snapshot(malformed); plan = malformed.PlanHeroGateGeneration(options);
        Expect(plan.Items[0].AreaID == 2, "Unrenderable area's ID was incorrectly reused."); Exact(malformed, before);
        malformed.ApplyEditorHeroGateGeneration(plan); malformed.Undo(); Exact(malformed, before);
    }
    private static void CheckDetection(bool legacy)
    {
        var document = Fixture(legacy); var options = HeroGateGenerationOptions.Defaults(document);
        var unchanged = Snapshot(document);
        document.Paint([new(2, 2)], new(2, 2, 1, 2)); document.Paint([new(4, 4)], new(37, 1, 99, 2));
        Expect(document.HeroGateSites().Count == 4, "Native reserved bytes/unknown gate variants became catalogue discriminators.");
        document.Undo(); document.Undo(); Exact(document, unchanged);
        document.Paint([new(7, 7)], new(33, 1, 1));
        Expect(!document.HeroGateSites().Any(site => site.Footprint.X == 8 && site.Footprint.Y == 8), "Original greedy footprint suppression became connected-component detection.");
        document.Undo(); Exact(document, unchanged);
        foreach (var terrain in new byte[] { 1, 7, 30, 35, 254, 28, 29, 33, 34, 37 })
        {
            document.Paint([new(10, 7)], new(terrain, 1, 1));
            Expect(document.HeroGateSites().Single(site => site.Footprint.X == 8 && site.Footprint.Y == 8).CannotDestroy, "Blocked gate perimeter failed for terrain " + terrain);
            document.Undo();
        }
        var expanded = document.AddMapRegion(MapRegionKind.Area, new(7, 7, 4, 4));
        document.Paint([new(7, 7)], new(1, 1, 1));
        var blocked = document.HeroGateSites().Single(site => site.Type == 3);
        Expect(blocked.Area?.Offset == expanded.Offset, "Original expanded-area fallback was cleared for a blocked gate.");
        var exact = document.AddMapRegion(MapRegionKind.Area, new(8, 8, 2, 2));
        Expect(document.HeroGateSites().Single(site => site.Type == 3).Area?.Offset == exact.Offset, "Blocked gate did not prefer exact footprint area.");
        var edge = NativeMapDocument.CreateNewMap(new("EdgeGate", "EdgeGate", 12, 12, NativeFormat: !legacy));
        edge.Paint([new(11, 5), new(5, 11)], new(29, 1, 1)); Expect(edge.HeroGateSites().Count == 0, "Source last-row/column scan exclusions changed.");
        edge.Paint([new(10, 6)], new(37, 1, 1)); var before = Snapshot(edge);
        Reject(() => edge.PlanHeroGateGeneration(options)); Exact(edge, before);
        var ambiguous = Fixture(legacy); var gate = ambiguous.AddMapRegion(MapRegionKind.Gate, new(8, 8, 1, 1));
        if (!legacy)
        {
            Set(ambiguous, gate, "bGateID", "73");
            var heart = ambiguous.AddMapRegion(MapRegionKind.Gate, new(2, 2, 1, 1)); Set(ambiguous, heart, "bGateType", "14");
            Expect(ambiguous.HeroGateSites().Single(site => site.Type == 3).GateID == 1, "Native gate editor ordinal was confused with bGateID/heart.");
        }
        else
        {
            var duplicate = ambiguous.AddMapRegion(MapRegionKind.Gate, new(2, 2, 1, 1));
            var field = duplicate.Fields.Single(candidate => candidate.Name == "id");
            // Corrupt fixture without identity-remapping API: bytes are deliberately malformed.
            Expect(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(duplicate.File.Data, out var fixtureBytes), "Fixture is not array-backed.");
            field.Encode("1").CopyTo(fixtureBytes.Array!, fixtureBytes.Offset + duplicate.Offset + field.Offset);
            before = Snapshot(ambiguous); Reject(() => ambiguous.PlanHeroGateGeneration(options)); Exact(ambiguous, before);
        }
    }
}
