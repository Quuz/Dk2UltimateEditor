using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    internal static void RunEditorParameterSave()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioCreationDefaults.json")!;
        using var fixture = JsonDocument.Parse(stream);
        var oracle = fixture.RootElement; var flow = oracle.GetProperty("CreationAndSaveFlow");
        var clear = flow.GetProperty("Trigger").GetProperty("ZeroOnSaveTypes").EnumerateArray().Select(item => item.GetString()!).ToHashSet();
        Expect(clear.Count == 20 && flow.GetProperty("Action").GetProperty("ZeroOnSaveTypes").GetArrayLength() == 6, "Independent original Save scope differs.");
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Original source missing.");
        foreach (var input in oracle.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original Save/default input changed.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2ScenarioSave-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true })
            {
                var document = NativeMapDocument.CreateNewMap(new("Save", "Save", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
                var eligible = pages.Where(page => page.Signature == 213 || page.PageID != -1).ToArray();
                Expect(eligible.Length == 95 && clear.All(name => eligible.Any(page => page.Type == name)), "Save scope does not fit independently captured eligible pages.");
                var normalized = 0; var retained = 0;
                foreach (var page in eligible)
                {
                    var record = Add(document, native, page);
                    var baseline = Snapshot(document); var history = document.GetHistory().Undo.Count; var revision = document.EditRevision;
                    var draft = document.CreateScenarioParameterDraft(record); Exact(baseline);
                    document.ApplyScenarioParameterDraft(draft); Exact(baseline);
                    Expect(document.EditRevision == revision && document.GetHistory().Undo.Count == history, "Trusted sparse/no-op operation performed human Save normalization.");
                    var expected = baseline.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                    var clears = clear.Contains(page.Type);
                    if (clears) Array.Clear(expected[record.File], record.Offset + (native ? 8 : 16), native ? 8 : 32);
                    document.ApplyScenarioEditorParameterDraft(draft); Exact(expected);
                    Expect(document.GetHistory().Undo.Count == history + (clears ? 1 : 0), "Explicit source Save used wrong atomic/no-op history.");
                    if (clears)
                    {
                        normalized++; document.Undo(); Exact(baseline); document.Redo(); Exact(expected);
                        var accepted = document.CreateScenarioParameterDraft(record); var acceptedRevision = document.EditRevision; var acceptedHistory = document.GetHistory();
                        document.ApplyScenarioEditorParameterDraft(accepted); Exact(expected);
                        var afterAccepted = document.GetHistory();
                        Expect(document.EditRevision == acceptedRevision + 1 && afterAccepted.ChangeCount == acceptedHistory.ChangeCount + 1 && afterAccepted.EditorChangeCount == unchecked(acceptedHistory.EditorChangeCount + 1) && afterAccepted.Undo.SequenceEqual(acceptedHistory.Undo) && afterAccepted.Redo.SequenceEqual(acceptedHistory.Redo), "Repeated already-normalized source Save did not count once while retaining history.");
                        document.Undo(); Exact(baseline);
                        var repeat = native ? "Repeat" : "repeat";
                        var prepared = document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(repeat, "254")]);
                        expected[record.File][record.Offset + (native ? 23 : 55)] = 254;
                        document.ApplyScenarioEditorParameterDraft(prepared); Exact(expected);
                        Expect(document.GetHistory().Undo.Count == history + 1, "Repeat and normalization did not compose into one Undo.");
                        document.Undo(); Exact(baseline); document.Redo(); Exact(expected); document.Undo(); Exact(baseline);
                        var stale = document.CreateScenarioParameterDraft(record);
                        document.SetField(record, record.Fields.Single(field => field.Name == repeat), "254"); document.Undo(); Exact(baseline);
                        Reject(() => document.ApplyScenarioEditorParameterDraft(stale)); Exact(baseline);
                        var other = NativeMapDocument.CreateNewMap(new("Other", "Other", 8, 8, NativeFormat: native));
                        Reject(() => other.ApplyScenarioEditorParameterDraft(document.CreateScenarioParameterDraft(record))); Exact(baseline);
                    }
                    else retained++;
                    if (clears && page.Type == "TRIGGER_APPEAR")
                    {
                        // Opening/saving a copied map without explicit editor Apply must remain lossless.
                        var saved = document.SaveCopy(Path.Combine(directory, native ? "native-before" : "project-before")); var loaded = NativeMapDocument.Load(saved);
                        var imported = loaded.Records().Single(item => EntityReferences.EventType(item)?.Name == page.Type);
                        Expect(imported.File.Data.Span.Slice(imported.Offset, imported.Length).SequenceEqual(record.File.Data.Span.Slice(record.Offset, record.Length)), "Save/reopen normalized imported parameters silently.");
                        var loadedBefore = Snapshot(loaded); _ = loaded.CreateScenarioParameterDraft(imported); Exact(loadedBefore);
                        loaded.ApplyScenarioEditorParameterDraft(loaded.CreateScenarioParameterDraft(imported));
                        var zeros = imported.File.Data.Span.Slice(imported.Offset + (native ? 8 : 16), native ? 8 : 32);
                        Expect(zeros.IndexOfAnyExcept((byte)0) < 0 && imported.File.Data.Span[imported.Offset + (native ? 23 : 55)] == 255, "Explicit Save did not clear literal parameters while retaining repeat.");
                    }
                    document.DeleteRecord(record);
                }
                Expect(normalized == 20 && retained == 75, "Not all source-clear and retained pages were exercised.");
                Console.WriteLine($"SOURCE SAVE DETAIL native={native} clear=20 retained=75 raw-preserving=95");
            }
        }
        finally { Directory.Delete(directory, true); }
    }
}
