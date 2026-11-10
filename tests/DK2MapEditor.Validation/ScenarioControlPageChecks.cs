using System.Globalization;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    private sealed record ScalarControl(int Parameter, string Kind, long Maximum, int[] Values, bool Unsigned = false);
    private sealed record ScalarPage(string Type, int PageID, ScalarControl[] Controls);
    private sealed record ScalarFixture(ScalarPage[] Pages, string[] RepeatOnlyTypes, int RepeatResource);
    public static void RunControlPages()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioScalarPages.json")!;
        var fixture = JsonSerializer.Deserialize<ScalarFixture>(stream)!;
        Expect(fixture.Pages.Length == 19 && fixture.RepeatOnlyTypes.Length == 26 && fixture.RepeatResource == 7199, "Literal scalar/repeat fixture incomplete.");
        var included = fixture.Pages.Select(item => item.Type).Concat(fixture.RepeatOnlyTypes).ToHashSet();
        Expect(included.All(ScenarioControlPages.Supports), "An accepted scalar/repeat page disappeared.");
        Expect(ScenarioControlPages.Duration("23", "59", "59") == 86399 && ScenarioControlPages.Duration("0", "0", "0") == 0, "Original time arithmetic differs.");
        foreach (var values in new[] { ("24", "0", "0"), ("0", "60", "0"), ("0", "0", "60"), ("-1", "0", "0"), ("x", "0", "0") })
            Reject(() => ScenarioControlPages.Duration(values.Item1, values.Item2, values.Item3));
        Expect(ScenarioControlPages.Repeat(ScenarioRepeatMode.Once, "ignored") == 1 && ScenarioControlPages.Repeat(ScenarioRepeatMode.Infinite, "ignored") == 255, "Source repeat modes differ.");
        for (var count = 0; count <= 254; count++) Expect(ScenarioControlPages.Repeat(ScenarioRepeatMode.Count, count.ToString()) == count, "Source count changed.");
        foreach (var value in new[] { "255", "256", "-1", "", "x" }) Reject(() => ScenarioControlPages.Repeat(ScenarioRepeatMode.Count, value));
        foreach (var native in new[] { false, true })
        {
            var document = NativeMapDocument.CreateNewMap(new("ScalarPages", "ScalarPages", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
            foreach (var type in included)
            {
                var page = pages.Single(item => item.Type == type); var scalar = fixture.Pages.SingleOrDefault(item => item.Type == type);
                var controls = ScenarioControlPages.Controls(type);
                Expect(controls.Select(item => (item.Parameter, item.Kind.ToString(), item.Maximum, item.Unsigned)).SequenceEqual((scalar?.Controls ?? []).Select(item => (item.Parameter, item.Kind, item.Maximum, item.Unsigned))), "Controls/ranges differ from source fixture: " + type);
                var record = Add(document, native, page); var before = Snapshot(document); var history = document.GetHistory().Undo.Count;
                var draft = document.CreateScenarioParameterDraft(record);
                document.ApplyScenarioParameterDraft(document.UpdateScenarioControlPage(draft, [])); Exact(before);
                Expect(document.GetHistory().Undo.Count == history, "Browsing imported source-out-of-range values created an edit.");
                var batch = new List<ScenarioParameterValue>(); var after = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                foreach (var control in scalar?.Controls ?? [])
                {
                    var field = (native ? "Param" : "param") + control.Parameter; var operand = Fields(native, page).Single(item => item.Name == field);
                    if (control.Unsigned && operand.Size == 4)
                    {
                        var imported = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(before[record.File].AsSpan(record.Offset + operand.Offset));
                        document.ApplyScenarioParameterDraft(document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, imported.ToString(CultureInfo.InvariantCulture))]));
                        Exact(before); Expect(document.GetHistory().Undo.Count == history, "Unsigned scalar re-entry changed bytes/history.");
                    }
                    var maximum = operand.Size == 1 ? Math.Min(255, control.Maximum) : operand.Size == 2 ? Math.Min(65535, control.Maximum) : control.Maximum;
                    var values = control.Kind == "Choice" ? control.Values.Select(item => (long)item) : new[] { 0L, maximum };
                    foreach (var value in values)
                    {
                        var expected = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone()); Write(expected[record.File], record.Offset + operand.Offset, operand, value);
                        var prepared = document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, value.ToString(CultureInfo.InvariantCulture))]); Exact(before);
                        document.ApplyScenarioParameterDraft(prepared); Exact(expected); document.Undo(); Exact(before); document.Redo(); Exact(expected); document.Undo(); Exact(before);
                    }
                    Reject(() => document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, (control.Maximum + 1).ToString())]));
                    Reject(() => document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, "-1")]));
                    batch.Add(new(field, maximum.ToString(CultureInfo.InvariantCulture))); Write(after[record.File], record.Offset + operand.Offset, operand, maximum);
                }
                if (page.Signature == 213)
                {
                    var field = native ? "Repeat" : "repeat"; batch.Add(new(field, "254")); after[record.File][record.Offset + (native ? 23 : 55)] = 254;
                }
                var final = document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), batch); document.ApplyScenarioParameterDraft(final); Exact(after);
                Expect(document.GetHistory().Undo.Count == history + 1, "Multi-control page was not one edit."); document.Undo(); Exact(before);
                if (!native && page.PageID != -1)
                    Reject(() => document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new("param8", "0")]));
            }
        }
    }
}
