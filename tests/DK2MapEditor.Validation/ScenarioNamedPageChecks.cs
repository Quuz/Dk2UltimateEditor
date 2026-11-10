using System.Globalization;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    private sealed record SemanticValue(int Value, string English, string Russian);
    private sealed record SemanticPage(string Type, int Parameter, SemanticValue[] Values);
    private sealed record NamedContext(string Type, int Parameter, int SecondaryParameter, bool Dynamic, int[] Values);
    private sealed record NamedControl(int Parameter, string Kind, long Maximum, int SecondaryParameter, int[] Values, bool Unsigned = false);
    private sealed record NamedPage(string Type, int PageID, NamedControl[] Controls);
    private sealed record NamedFixture(NamedContext[] Contexts, NamedPage[] ConditionPages);
    private static NamedFixture ReadNamed()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioNamedPages.json")!;
        return JsonSerializer.Deserialize<NamedFixture>(stream)!;
    }
    public static void RunNamedPages()
    {
        RunBandHints();
        RunAreaHints();
        RunPrerequisites();
        RunTerrainOwners();
        using var semanticStream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioSemanticOptions.json")!;
        var semanticPages = JsonSerializer.Deserialize<SemanticPage[]>(semanticStream)!;
        Expect(semanticPages.Length == 2, "Independent source semantic fixture incomplete.");
        foreach (var native in new[] { false, true })
        {
            var semanticDocument = NativeMapDocument.CreateNewMap(new("SemanticOptions", "SemanticOptions", 20, 20, NativeFormat: native));
            foreach (var source in semanticPages)
            {
                var record = Add(semanticDocument, native, pages.Single(page => page.Type == source.Type));
                var field = record.Fields.Single(item => item.Name == (native ? "Param" : "param") + source.Parameter);
                var before = Snapshot(semanticDocument); var revision = semanticDocument.EditRevision;
                var choices = ScenarioParameterChoices.ForField(record, field);
                Expect(choices.Select(item => (int.Parse(item.Value, CultureInfo.InvariantCulture), item.English, item.Russian))
                    .SequenceEqual(source.Values.Select(item => (item.Value, item.English, item.Russian))), "Source flag caption/value meaning differs: " + source.Type);
                Expect(ValueCatalogues.ForField(record, field).SequenceEqual(choices), "Ordinary semantic catalogue differs.");
                Exact(before); Expect(semanticDocument.EditRevision == revision, "Reading semantic choices changed the document.");
            }
        }
        var fixture = ReadNamed();
        Expect(fixture.Contexts.Length == 55 && fixture.ConditionPages.Length == 28, "Source named-page fixture incomplete.");
        foreach (var context in fixture.Contexts.Where(item => !item.Dynamic))
        {
            var choices = ScenarioNamedCatalogues.Choices(context.Type, context.Parameter);
            Expect(choices.Select(item => int.Parse(item.Value, CultureInfo.InvariantCulture)).SequenceEqual(context.Values), "Source context values/order differ: " + context.Type + "/" + context.Parameter);
            Expect(choices.All(item => item.English.Length != 0 && item.Russian.Length != 0), "A named context lacks a language.");
        }
        Expect(ScenarioNamedCatalogues.InterfaceElements(1).Count == 57 && ScenarioNamedCatalogues.InterfaceElements(0).Count == 54 && ScenarioNamedCatalogues.InterfaceElements(-1).Count == 49, "Source interface contexts were merged.");
        var scalar = JsonSerializer.Deserialize<ScalarFixture>(typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioScalarPages.json")!)!;
        var supported = fixture.ConditionPages.Select(item => item.Type).Concat(scalar.Pages.Select(item => item.Type)).Concat(scalar.RepeatOnlyTypes).Concat(ReadActionPages().Pages.Select(item => item.Type)).Concat(new[] { "TRIGGER_VARIABLE", "TRIGGER_TIMER", "ACTION_SETVARIABLE", "ACTION_SETTIMER", "ACTION_CAMERAPATH", "ACTION_ADDCREATURE" }).ToHashSet();
        using var spatial = JsonDocument.Parse(typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioSpatialSource.json")!);
        supported.UnionWith(spatial.RootElement.GetProperty("Pages").EnumerateObject().Select(page => page.Name));
        using var speech = JsonDocument.Parse(typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioSpeechSource.json")!);
        supported.Add(speech.RootElement.GetProperty("Type").GetString()!);
        Expect(pages.All(page => ScenarioControlPages.Supports(page.Type) == supported.Contains(page.Type)), "An incomplete page was exposed or a complete page hidden.");
        foreach (var native in new[] { false, true })
        {
            void WriteValue(byte[] bytes, int offset, Operand operand, long value)
            {
                if (operand.Size == 4 && value > int.MaxValue) System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), checked((uint)value));
                else Write(bytes, offset, operand, value);
            }
            var document = NativeMapDocument.CreateNewMap(new("NamedPages", "NamedPages", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
            var band = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEBAND"), new(0, 0)) : document.AddLegacyRecord("Band", new(0, 0));
            var identity = EntityReferences.Identity(band)!; document.SetField(band, identity.Field, "0");
            var player = document.PlayerSettingsRows().First(item => item.ID == 3);
            var playerName = player.Record.Fields.Single(item => item.Name == (native ? "szPlayerName" : "PlayerName"));
            document.SetField(player.Record, playerName, "Scenario keeper");
            foreach (var source in fixture.ConditionPages)
            {
                var page = pages.Single(item => item.Type == source.Type); var controls = ScenarioControlPages.Controls(source.Type);
                Expect(controls.Select(item => (item.Parameter, item.Kind.ToString(), item.Maximum, item.SecondaryParameter, item.Unsigned))
                    .SequenceEqual(source.Controls.Select(item => (item.Parameter, item.Kind, item.Maximum, item.SecondaryParameter, item.Unsigned))), "Condition controls differ from independent source fixture: " + source.Type);
                var record = Add(document, native, page); var before = Snapshot(document); var history = document.GetHistory().Undo.Count;
                var draft = document.CreateScenarioParameterDraft(record);
                document.ApplyScenarioParameterDraft(document.UpdateScenarioControlPage(draft, [])); Exact(before);
                foreach (var control in source.Controls)
                {
                    var field = (native ? "Param" : "param") + control.Parameter;
                    var operand = Fields(native, page).Single(item => item.Name == field);
                    if (control.Unsigned && operand.Size == 4)
                    {
                        var imported = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(before[record.File].AsSpan(record.Offset + operand.Offset));
                        document.ApplyScenarioParameterDraft(document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, imported.ToString(CultureInfo.InvariantCulture))]));
                        Exact(before); Expect(document.GetHistory().Undo.Count == history, "Unsigned condition re-entry changed bytes/history.");
                    }
                    var values = control.Kind == "Number"
                        ? new[] { 0L, Math.Min(control.Maximum, operand.Size == 1 ? 255L : operand.Size == 2 ? 65535L : uint.MaxValue) }
                        : control.Values.Length != 0 ? control.Values.Select(item => (long)item).ToArray() : new[] { 0L };
                    foreach (var value in values)
                    {
                        var expected = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                        var inputs = new List<ScenarioParameterValue>();
                        inputs.Add(new(field, (control.Kind == "Element" ? value >> 16 : value).ToString(CultureInfo.InvariantCulture)));
                        WriteValue(expected[record.File], record.Offset + operand.Offset, operand, control.Kind == "Element" ? value >> 16 : value);
                        if (control.SecondaryParameter != 0)
                        {
                            var second = Fields(native, page).Single(item => item.Name == (native ? "Param" : "param") + control.SecondaryParameter);
                            inputs.Add(new(second.Name, (value & 65535).ToString(CultureInfo.InvariantCulture)));
                            Write(expected[record.File], record.Offset + second.Offset, second, value & 65535);
                        }
                        var prepared = document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), inputs); Exact(before);
                        document.ApplyScenarioParameterDraft(prepared); Exact(expected);
                        Expect(document.GetHistory().Undo.Count == history + 1, "Named/page pair did not commit once.");
                        document.Undo(); Exact(before); document.Redo(); Exact(expected); document.Undo(); Exact(before);
                    }
                    Reject(() => document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, "-1")]));
                    Reject(() => document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new(field, "999999999999")]));
                }
                var batch = new List<ScenarioParameterValue>(); var final = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                foreach (var control in source.Controls)
                {
                    var field = (native ? "Param" : "param") + control.Parameter;
                    var operand = Fields(native, page).Single(item => item.Name == field);
                    var value = control.Kind == "Number" ? 7 : control.Values.Length == 0 ? 0 : control.Values.Last();
                    batch.Add(new(field, (control.Kind == "Element" ? value >> 16 : value).ToString(CultureInfo.InvariantCulture)));
                    Write(final[record.File], record.Offset + operand.Offset, operand, control.Kind == "Element" ? value >> 16 : value);
                    if (control.SecondaryParameter != 0)
                    {
                        var second = Fields(native, page).Single(item => item.Name == (native ? "Param" : "param") + control.SecondaryParameter);
                        batch.Add(new(second.Name, (value & 65535).ToString(CultureInfo.InvariantCulture))); Write(final[record.File], record.Offset + second.Offset, second, value & 65535);
                    }
                }
                batch.Add(new(native ? "Repeat" : "repeat", "254")); final[record.File][record.Offset + (native ? 23 : 55)] = 254;
                document.ApplyScenarioParameterDraft(document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), batch)); Exact(final);
                Expect(document.GetHistory().Undo.Count == history + 1, "Complete named page batch was not atomic."); document.Undo(); Exact(before);
                if (!native) Reject(() => document.UpdateScenarioControlPage(document.CreateScenarioParameterDraft(record), [new("param8", "1")]));
                Expect(document.GetHistory().Undo.Count == history, "Failed named draft changed history.");
                foreach (var context in fixture.Contexts.Where(item => item.Type == source.Type && item.SecondaryParameter == 0))
                {
                    var operandField = record.Fields.Single(item => item.Name == (native ? "Param" : "param") + context.Parameter);
                    Expect(ScenarioNamedCatalogues.HasFieldContext(record, operandField), "Ordinary named input context missing.");
                    if (context.Dynamic) Expect(ScenarioNamedCatalogues.ForField(record, operandField, document).Select(item => item.Value).SequenceEqual(new[] { "0" }), "Band ID zero became an unselectable heading/None.");
                }
            }
            Expect(ScenarioNamedCatalogues.Choices("TRIGGER_LOSTCREATURES", 4, document).Single(item => item.Value == "3").English == "Scenario keeper", "Scenario player names are stale/fixed labels.");
            var recordForGuard = document.Records().First(item => EntityReferences.EventType(item)?.Name == "TRIGGER_CREATUREEXP"); var guardField = recordForGuard.Fields.Single(item => item.Name == (native ? "Param" : "param") + 5);
            Expect(ScenarioNamedCatalogues.ForField(recordForGuard, guardField, document).Count == 10, "Level guard fixture has no real choices.");
            Expect(!ScenarioNamedCatalogues.HasFieldContext(recordForGuard, guardField with { BitMask = 1 }), "A bit field was inferred as a catalogue.");
            if (native)
            {
                var bytes = Mutable(recordForGuard.File); var original = bytes[4]; bytes[4] = 99;
                Expect(ScenarioNamedCatalogues.ForField(recordForGuard, guardField, document).Count == 0, "Future native layout received familiar choices."); bytes[4] = original;
            }
        }
    }
}
