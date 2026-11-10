using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    public static void RunAreaHints()
    {
        using var fixture = JsonDocument.Parse(typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioAreaHintsSource.json")!);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        foreach (var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "AREA information source changed.");
        var types = fixture.RootElement.GetProperty("Types").EnumerateArray().Select(value => value.GetString()!).ToArray();
        Expect(types.Length == 3 && types.All(ScenarioAreaHints.Supports) && !ScenarioAreaHints.Supports("TRIGGER_CLAIMENTIREAREA"), "AREA hint page scope differs.");
        foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("AreaHints", "AreaHints", 20, 20, NativeFormat: native));
            NativeRecord Area() => native ? doc.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEAREA"), new(2, 3)) : doc.AddLegacyRecord("Area", new(2, 3));
            var first = Area(); var second = Area();
            var owner = doc.ScriptOwners().Single(value => value.Scope == 4 && value.Record.File == first.File && value.Record.Offset == first.Offset);
            var other = doc.ScriptOwners().Single(value => value.Scope == 4 && value.Record.File == second.File && value.Record.Offset == second.Offset);
            foreach (var type in types)
            {
                var record = Add(doc, native, pages.Single(page => page.Type == type));
                var id = ScriptGraph.ID(record);
                var unlinked = doc.CreateScenarioParameterDraft(record);
                Expect(doc.ScenarioAreaContexts(unlinked).Count == 0, "Unlinked event inferred an arbitrary AREA.");
                var firstRoot = doc.BindScriptScope(owner, id); doc.BindScriptScope(other, id);
                // Adding project scope records moves the following event
                // collection. Resolve the event again by its retained ID.
                record = ScriptGraph.Build(doc).Events[id].Record;
                foreach (var (width, height, total, perimeter) in new[] { (1, 1, 1, 1), (1, 4, 4, 1), (2, 1, 2, 2), (3, 5, 15, 12), (4, 3, 12, 10) })
                {
                    doc.SetMapRegionBounds(first, new(2, 3, width, height));
                    doc.SetMapRegionBounds(second, new(7, 6, 2, 3));
                    var draft = doc.CreateScenarioParameterDraft(record); var before = Snapshot(doc);
                    var history = doc.GetHistory().Undo.Count; var revision = doc.EditRevision; var counter = doc.EditorChangeCount;
                    var contexts = doc.ScenarioAreaContexts(draft);
                    Expect(contexts.Count == 2 && contexts.All(context => context.Owner.Record.File == context.OriginalOwner.Record.File), "Shared event lost its two physical AREA contexts.");
                    var metrics = doc.ReadScenarioAreaMetrics(draft, owner)!;
                    Expect(metrics.Width == width && metrics.Height == height && metrics.TotalCells == total
                        && metrics.PerimeterCells == (type == "TRIGGER_CLAIMAREA" ? perimeter : null), "AREA total or source single-column perimeter differs.");
                    Expect(doc.ReadScenarioAreaMetrics(draft, other)!.TotalCells == 6, "Chosen second AREA used the first shared owner.");
                    Exact(before); Expect(doc.GetHistory().Undo.Count == history && doc.EditRevision == revision && doc.EditorChangeCount == counter, "Reading AREA contexts/metrics changed history/counters.");
                }
                void Raw(string project, string game, string value)
                {
                    var field = first.Fields.Single(item => item.Name == (native ? game : project));
                    field.Encode(value).CopyTo(Mutable(first.File).AsSpan(first.Offset + field.Offset));
                }
                Raw("rect.x1", "dwAreaLocationStartX", "9"); Raw("rect.x2", "dwAreaLocationEndX", "2");
                Raw("rect.y1", "dwAreaLocationStartY", "8"); Raw("rect.y2", "dwAreaLocationEndY", "3");
                var reversed = doc.CreateScenarioParameterDraft(record); var reverseBytes = Snapshot(doc);
                Expect(doc.ReadScenarioAreaMetrics(reversed, owner) is { Width: 8, Height: 6, TotalCells: 48 }, "Reversed inclusive endpoints differ from Map_CorrectRect."); Exact(reverseBytes);
                using (var session = doc.CreateScenarioCreationSession(doc.CreateScenarioCreationContext(), 0xD5, LegacyEventSchemas.All.Single(item => item.Name == type), firstRoot))
                {
                    var privateBefore = Snapshot(session.WorkingDocument); var liveBefore = Snapshot(doc);
                    var contexts = doc.ScenarioCreationAreaContexts(session);
                    Expect(contexts.Count == 1 && contexts[0].OriginalOwner.Record.File == first.File
                        && contexts[0].Owner.Record.File != first.File, "Insertion owner was not translated into the private document.");
                    Expect(session.WorkingDocument.ScenarioAreaContexts(session.WorkingDocument.CreateScenarioParameterDraft(session.Record)).Count == 0, "Unsubmitted event unexpectedly gained a live binding.");
                    Expect(session.WorkingDocument.ReadScenarioAreaMetrics(session.WorkingDocument.CreateScenarioParameterDraft(session.Record), contexts[0].Owner)!.TotalCells == 48, "Private insertion used no AREA or a foreign live AREA.");
                    Exact(liveBefore); Expect(privateBefore.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Private context read modified the wizard.");
                }
                using (var session = doc.CreateScenarioEditingSession(doc.CreateScenarioCreationContext(), record))
                {
                    var contexts = doc.ScenarioCreationAreaContexts(session);
                    Expect(contexts.Count == 2 && contexts.All(context => context.Owner.Record.File != first.File), "Editing wizard lost a shared AREA owner.");
                    var privateDraft = session.WorkingDocument.CreateScenarioParameterDraft(session.Record);
                    Expect(session.WorkingDocument.ReadScenarioAreaMetrics(privateDraft, contexts.Single(context => context.OriginalOwner.Record.Offset == second.Offset).Owner)!.TotalCells == 6, "Private editing chose the wrong shared AREA.");
                    Reject(() => doc.ReadScenarioAreaMetrics(privateDraft, owner));
                }
                var stale = doc.CreateScenarioParameterDraft(record);
                doc.SetScenarioName(ScenarioNameKind.Variable, 0, type);
                Reject(() => doc.ScenarioAreaContexts(stale)); Reject(() => doc.ReadScenarioAreaMetrics(stale, owner));
                var current = doc.CreateScenarioParameterDraft(record); var stable = Snapshot(doc);
                doc.Edit("Read-only nested AREA hint", () => Reject(() => doc.ReadScenarioAreaMetrics(current, owner))); Exact(stable);
            }
            var sample = doc.Records().First(record => EntityReferences.EventType(record)?.Name == types[0]);
            var foreign = NativeMapDocument.CreateNewMap(new("ForeignArea", "ForeignArea", 20, 20, NativeFormat: native));
            Reject(() => foreign.ReadScenarioAreaMetrics(doc.CreateScenarioParameterDraft(sample), owner));
            Console.WriteLine($"AREA HINT DETAIL native={native} three source pages x five shapes; inclusive reversed endpoints, two shared owners, private insertion/editing correspondence and read-only lifetime guards");
        }
    }
}
