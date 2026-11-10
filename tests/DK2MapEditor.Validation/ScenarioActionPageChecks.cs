using System.Globalization;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    private sealed record ActionPage(string Type, int PageID, NamedControl[] Controls, string[] RequiredCollections);
    private sealed record ActionFixture(ActionPage[] Pages);
    private static ActionFixture ReadActionPages()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioActionPages.json")!;
        return JsonSerializer.Deserialize<ActionFixture>(stream)!;
    }
    private static int[] ActionDynamicValues(string type, int parameter) => type == "ACTION_RELEASEBAND" && parameter == 1 ? [0, 4]
        : type == "ACTION_TOGGLEEFFECT" ? [7, 29] : [13, 39];
    private static void SeedActionReferences(NativeMapDocument document, bool native)
    {
        foreach (var id in new[] { 0, 4 })
        {
            var band = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEBAND"), new(0, 0)) : document.AddLegacyRecord("Band", new(0, 0));
            document.SetField(band, EntityReferences.Identity(band)!.Field, id.ToString(CultureInfo.InvariantCulture));
        }
        foreach (var (kind, ids) in new[] { (MapRegionKind.Area, new[] { 13, 39 }), (MapRegionKind.Effect, new[] { 7, 29 }) })
            for (var index = 0; index < ids.Length; index++)
            {
                var region = document.AddMapRegion(kind, new(2 + index * 4, kind == MapRegionKind.Area ? 2 : 8, 1, 1));
                document.SetField(region, EntityReferences.Identity(region)!.Field, ids[index].ToString(CultureInfo.InvariantCulture));
            }
    }
    public static void RunActionPages()
    {
        var fixture = ReadActionPages();
        Expect(fixture.Pages.Length == 12, "Independent action fixture incomplete.");
        foreach (var native in new[] { false, true })
        {
            var document = NativeMapDocument.CreateNewMap(new("ActionPages", "ActionPages", 20, 20, NativeFormat: native));
            document.SetHistoryDepth(65535); SeedActionReferences(document, native);
            foreach (var source in fixture.Pages)
            {
                var page = pages.Single(item => item.Type == source.Type);
                Expect(page.PageID == source.PageID, "Original action page identity differs.");
                Expect(ScenarioControlPages.Controls(source.Type).Select(item => (item.Parameter, item.Kind.ToString(), item.Maximum, item.SecondaryParameter, item.Unsigned))
                    .SequenceEqual(source.Controls.Select(item => (item.Parameter, item.Kind, item.Maximum, item.SecondaryParameter, item.Unsigned))), "Source action controls differ: " + source.Type);
                var record = Add(document, native, page); var before = Snapshot(document); var history = document.GetHistory().Undo.Count;
                Expect(ScenarioControlPages.MissingRequirements(source.Type, document).Count == 0, "Existing action map identities were not recognized.");
                document.ApplyScenarioParameterDraft(document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [])); Exact(before);
                Expect(document.GetHistory().Undo.Count == history, "Action no-op repaired an imported operand.");
                var batch = new List<ScenarioParameterValue>(); var final = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                foreach (var control in source.Controls)
                {
                    var field = (native ? "Param" : "param") + control.Parameter;
                    var operand = Fields(native, page).Single(item => item.Name == field);
                    if (control.Unsigned && operand.Size == 4)
                    {
                        var imported = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(before[record.File].AsSpan(record.Offset + operand.Offset));
                        document.ApplyScenarioParameterDraft(document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, imported.ToString(CultureInfo.InvariantCulture))]));
                        Exact(before); Expect(document.GetHistory().Undo.Count == history, "Unsigned imported re-entry changed bytes/history.");
                    }
                    var maximum = Math.Min(control.Maximum, operand.Size == 1 ? 255L : operand.Size == 2 ? 65535L : int.MaxValue);
                    var values = control.Kind == "Number" ? new[] { 0L, maximum }
                        : (control.Values.Length == 0 ? ActionDynamicValues(source.Type, control.Parameter) : control.Values).Select(value => (long)value).ToArray();
                    if (control.Kind != "Number")
                    {
                        var choices = ScenarioControlPages.Choices(document.CreateScenarioParameterDraft(record), ScenarioControlPages.Controls(source.Type).Single(item => item.Parameter == control.Parameter), document);
                        Expect(choices.Select(item => long.Parse(item.Value, CultureInfo.InvariantCulture)).SequenceEqual(values), "Action source catalogue/identity order differs: " + source.Type);
                    }
                    void Prepare(Dictionary<NativeFile, byte[]> expected, List<ScenarioParameterValue> inputs, long value)
                    {
                        var first = control.Kind == "Element" ? value >> 16 : value;
                        inputs.Add(new(field, first.ToString(CultureInfo.InvariantCulture))); Write(expected[record.File], record.Offset + operand.Offset, operand, first);
                        if (control.SecondaryParameter != 0)
                        {
                            var second = Fields(native, page).Single(item => item.Name == (native ? "Param" : "param") + control.SecondaryParameter);
                            inputs.Add(new(second.Name, (value & 65535).ToString(CultureInfo.InvariantCulture))); Write(expected[record.File], record.Offset + second.Offset, second, value & 65535);
                        }
                    }
                    foreach (var value in values)
                    {
                        var expected = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone()); var inputs = new List<ScenarioParameterValue>(); Prepare(expected, inputs, value);
                        var prepared = document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), inputs); Exact(before);
                        document.ApplyScenarioParameterDraft(prepared); Exact(expected);
                        Expect(document.GetHistory().Undo.Count == history + 1, "Action or paired operands did not commit once.");
                        document.Undo(); Exact(before); document.Redo(); Exact(expected); document.Undo(); Exact(before);
                    }
                    Reject(() => document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, "-1")]));
                    Reject(() => document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, control.Kind == "Number" ? (maximum + 1).ToString(CultureInfo.InvariantCulture) : "999999999")]));
                    Prepare(final, batch, control.Kind == "Number" ? maximum : values.Last());
                }
                document.ApplyScenarioParameterDraft(document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), batch)); Exact(final);
                Expect(document.GetHistory().Undo.Count == history + 1, "Whole action page did not commit atomically."); document.Undo(); Exact(before);
                if (!native) Reject(() => document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new("param8", "1")]));
                var empty = NativeMapDocument.CreateNewMap(new("EmptyAction", "EmptyAction", 20, 20, NativeFormat: native));
                var emptyRecord = Add(empty, native, page); var untouched = Snapshot(empty); var revision = empty.EditRevision;
                var required = source.RequiredCollections.Select(name => name switch { "Areas" => EntityKind.Area, "Bands" => EntityKind.Band, "Effects" => EntityKind.Effect, _ => throw new InvalidOperationException() }).ToHashSet();
                Expect(ScenarioControlPages.MissingRequirements(source.Type, empty).ToHashSet().SetEquals(required), "Prerequisites differ from original ParamsCheck.");
                if (required.Count != 0) Reject(() => empty.UpdateScenarioControlPage(empty.CreateScenarioParameterDraft(emptyRecord), []));
                else empty.ApplyScenarioParameterDraft(empty.UpdateScenarioControlPage(empty.CreateScenarioParameterDraft(emptyRecord), []));
                Exact(untouched); Expect(empty.EditRevision == revision, "Missing-list guard or empty-area no-op changed data.");
            }
        }
    }
}
