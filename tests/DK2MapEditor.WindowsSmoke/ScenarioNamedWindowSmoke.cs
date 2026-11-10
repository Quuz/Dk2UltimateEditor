using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private sealed record NamedWindowControl(int Parameter, string Kind, long Maximum, int SecondaryParameter, int[] Values, bool Unsigned = false);
    private sealed record NamedWindowPage(string Type, int PageID, NamedWindowControl[] Controls);
    private sealed record NamedWindowFixture(NamedWindowPage[] ConditionPages);
    private static void CheckScenarioNamedWindows(bool native, string language, string directory)
    {
        CheckScenarioAreaHints(native, language);
        CheckScenarioBandPrerequisites(native, language);
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioNamedPages.json")!;
        var fixture = JsonSerializer.Deserialize<NamedWindowFixture>(stream)!;
        using var offsets = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioParameterPages.json")!;
        var matrix = JsonSerializer.Deserialize<LiteralOptionPage[]>(offsets)!;
        Expect(fixture.ConditionPages.Length == 28, "Source condition window fixture incomplete.");
        var document = NativeMapDocument.CreateNewMap(new("NamedUI", "NamedUI", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
        var band = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEBAND"), new(0, 0)) : document.AddLegacyRecord("Band", new(0, 0));
        document.SetField(band, EntityReferences.Identity(band)!.Field, "0");
        var ids = new Dictionary<string, ushort>();
        foreach (var page in fixture.ConditionPages)
        {
            var record = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Trigger"), new(0, 0)) : document.AddLegacyRecord("Trigger", new(0, 0));
            document.SetEventType(record, LegacyEventSchemas.All.Single(item => item.Name == page.Type));
            record = document.Records().Single(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length);
            ids.Add(page.Type, ScriptGraph.ID(record));
            Expect(MemoryMarshal.TryGetArray(record.File.Data, out var data) && data.Offset == 0, "Cannot seed independent window imports.");
            for (var offset = native ? 8 : 16; offset < (native ? 16 : 48); offset++) data.Array![record.Offset + offset] = (byte)(0xA7 ^ offset);
            data.Array![record.Offset + (native ? 23 : 55)] = 255;
        }
        var workspace = new EditorWorkspace { Document = document };
        var clipboard = document.CopySelection(new(0, 0, 1, 1)); workspace.LoadSelectionIntoBuffer(clipboard);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); var list = Control<ListBox>(workspace, "RecordList");
        void LayoutWorkspace() { workspace.Measure(new Size(1000, 760)); workspace.Arrange(new Rect(0, 0, 1000, 760)); workspace.UpdateLayout(); }
        NativeRecord Select(NamedWindowPage page)
        {
            var record = ScriptGraph.Build(document).Events[ids[page.Type]].Record;
            categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            list.SelectedItem = list.Items.Cast<object>().Single(item => ScriptGraph.ID(Get<NativeRecord>(item, "Record")) == ids[page.Type]); LayoutWorkspace(); return record;
        }
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected) => Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)) && ReferenceEquals(workspace.BufferedSelection, clipboard), "Named window changed unrelated bytes/clipboard.");
        LiteralOptionOperand Operand(NamedWindowPage page, int parameter) => native ? matrix.Single(item => item.Type == page.Type).Native.Single(item => item.Name == "Param" + parameter) : new("param" + parameter, 16 + (parameter - 1) * 4, 4, "LONG");
        void Write(Dictionary<NativeFile, byte[]> expected, NativeRecord record, LiteralOptionOperand operand, long value)
        {
            var offset = record.Offset + operand.Offset;
            if (operand.Size == 1) expected[record.File][offset] = checked((byte)value);
            else if (operand.Size == 2) BinaryPrimitives.WriteUInt16LittleEndian(expected[record.File].AsSpan(offset), checked((ushort)value));
            else BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(offset), checked((uint)value));
        }
        void Choose(ComboBox combo, string value) => combo.SelectedItem = combo.Items.Cast<object>().First(item => Get<string>(item, "Value") == value);
        var applied = 0;
        foreach (var page in fixture.ConditionPages)
        {
            var record = Select(page);
            Expect(Control<Button>(workspace, "ScenarioParameterButton").Visibility == Visibility.Visible, "Complete condition page has no real command.");
            ScenarioParameterWindow? presented = null; workspace.ScenarioParameterPresenter = value => presented = value;
            Control<Button>(workspace, "ScenarioParameterButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var window = presented ?? throw new InvalidOperationException("The named page command did not present its window.");
            T Named<T>(string name) where T : FrameworkElement => (T)(window.FindName(name) ?? throw new InvalidOperationException(name));
            var before = Snapshot(); var history = document.GetHistory().Undo.Count;
            if (ScenarioBandHints.Supports(page.Type))
            {
                var population = Named<TextBlock>("ScenarioBandPopulation");
                Choose(Named<ComboBox>("ScenarioParam2"), "0");
                var expected = string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioPage.BandPopulation"), 0);
                Expect(population.Text == expected, "Selected band-zero population was not shown.");
                LocalizationService.SetLanguage(language == "ru" ? "en" : "ru");
                Expect(population.Text == string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioPage.BandPopulation"), 0), "Band population did not translate.");
                LocalizationService.SetLanguage(language); Exact(before); window.ResetDraft();
            }
            Named<Button>("ScenarioPageApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(before);
            Expect(document.GetHistory().Undo.Count == history, "Opening/Apply coerced imported named values.");
            foreach (var control in page.Controls.Where(item => item.Unsigned))
            {
                var operand = Operand(page, control.Parameter);
                if (operand.Size != 4) continue;
                var imported = BinaryPrimitives.ReadUInt32LittleEndian(before[record.File].AsSpan(record.Offset + operand.Offset)).ToString(CultureInfo.InvariantCulture);
                var input = Named<TextBox>("ScenarioParam" + control.Parameter);
                Expect(input.Text == imported, "Original unsigned condition displayed a signed import.");
                input.Text = "0"; input.Text = imported;
                LocalizationService.SetLanguage(language == "ru" ? "en" : "ru");
                Expect(input.Text == imported, "Translation changed an unsigned condition input."); LocalizationService.SetLanguage(language);
                Expect(window.ApplyDraft(), "Unsigned condition re-entry rejected unchanged source bits."); Exact(before);
                Expect(document.GetHistory().Undo.Count == history, "Unsigned condition re-entry created history.");
            }
            var batch = Snapshot();
            foreach (var control in page.Controls)
            {
                var operand = Operand(page, control.Parameter);
                if (control.Kind != "Number")
                {
                    var picker = Named<ComboBox>("ScenarioParam" + control.Parameter);
                    var values = control.Values.Length == 0 ? new[] { 0 } : control.Values;
                    Expect(values.All(value => picker.Items.Cast<object>().Any(item => Get<string>(item, "Value") == value.ToString(CultureInfo.InvariantCulture))), "Actual named picker omitted source values.");
                    if (control.Kind == "Element") Expect(picker.Items.Count == 58, "Interface pair/unknown catalogue was flattened or lost.");
                    foreach (var value in values)
                    {
                        if(page.Type=="TRIGGER_AREACONTENT"&&control.Parameter==2)
                            Choose(Named<ComboBox>("ScenarioParam3"),"8");
                        Choose(picker, value.ToString(CultureInfo.InvariantCulture)); var single = Snapshot();
                        Write(single, record, operand, control.Kind == "Element" ? value >> 16 : value);
                        if(page.Type=="TRIGGER_AREACONTENT")
                        {
                            if(control.Parameter==2)Write(single,record,Operand(page,3),8);
                            if(control.Parameter==3&&(value<=7||value==31||value==35))
                                Write(single,record,Operand(page,2),value==35?1:2);
                        }
                        if (control.SecondaryParameter != 0) Write(single, record, Operand(page, control.SecondaryParameter), value & 65535);
                        Expect(window.ApplyDraft(), "Named source choice failed actual Apply: " + page.Type); Exact(single);
                        Expect(document.GetHistory().Undo.Count == history + 1, "Named/pair window was not one edit.");
                        document.Undo(); Exact(before); document.Redo(); Exact(single); document.Undo(); Exact(before); window.ResetDraft(); applied++;
                    }
                }
            }
            // Prepare the whole-page batch after all single-choice round trips;
            // no pending input from an earlier control may contaminate a case.
            foreach (var control in page.Controls)
            {
                var operand = Operand(page, control.Parameter);
                if (control.Kind == "Number")
                {
                    Named<TextBox>("ScenarioParam" + control.Parameter).Text = "7"; Write(batch, record, operand, 7);
                }
                else
                {
                    var value = control.Values.Length == 0 ? 0 : control.Values.Last();
                    Choose(Named<ComboBox>("ScenarioParam" + control.Parameter), value.ToString(CultureInfo.InvariantCulture));
                    Write(batch, record, operand, control.Kind == "Element" ? value >> 16 : value);
                    if (control.SecondaryParameter != 0) Write(batch, record, Operand(page, control.SecondaryParameter), value & 65535);
                }
            }
            var repeatPicker = Named<ComboBox>("ScenarioRepeatMode"); repeatPicker.SelectedItem = repeatPicker.Items.Cast<object>().Single(item => Get<object>(item, "Mode").ToString() == "Count");
            Named<TextBox>("ScenarioRepeatCount").Text = "254"; batch[record.File][record.Offset + (native ? 23 : 55)] = 254;
            LocalizationService.SetLanguage(language == "ru" ? "en" : "ru"); Exact(before);
            LocalizationService.SetLanguage(language); Exact(before);
            Named<Button>("ScenarioPageApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(batch);
            Expect(document.GetHistory().Undo.Count == history + 1 && !window.HasPendingInputs, "Named full-page/translation batch was not atomic."); document.Undo(); Exact(before); window.ResetDraft();
            foreach (var control in page.Controls.Where(item => item.Kind == "Number"))
            {
                Named<TextBox>("ScenarioParam" + control.Parameter).Text = (control.Maximum + 1).ToString(CultureInfo.InvariantCulture);
                Expect(!window.ApplyDraft(), "Out-of-source/storage range number was accepted."); Exact(before); window.ResetDraft();
            }
            foreach (var control in page.Controls.Where(item => item.Kind == "Number" && item.Maximum == uint.MaxValue))
            {
                var input = Named<TextBox>("ScenarioParam" + control.Parameter);
                Expect(uint.TryParse(input.Text, NumberStyles.None, CultureInfo.InvariantCulture, out _), "Original unsigned imported count displayed as a signed number.");
                foreach (var value in new[] { 2147483648L, 4294967295L })
                {
                    input.Text = value.ToString(CultureInfo.InvariantCulture); var expected = Snapshot(); Write(expected, record, Operand(page, control.Parameter), value);
                    Expect(window.ApplyDraft(), "Original full DWORD count failed Apply."); Exact(expected);
                    Expect(input.Text == value.ToString(CultureInfo.InvariantCulture), "Applied unsigned count changed display.");
                    document.Undo(); Exact(before); document.Redo(); Exact(expected); document.Undo(); Exact(before); window.ResetDraft();
                }
            }
            if (page.Type == "TRIGGER_DESTROYED")
            {
                var picker = Named<ComboBox>("ScenarioParam2");
                var overlapping = picker.Items.Cast<object>().Where(item => Get<string>(item, "Value") == "1").ToArray();
                Expect(overlapping.Length == 2, "Overlapping door/trap IDs were silently deduplicated.");
                picker.SelectedItem = overlapping[1]; var selectedRow = Get<int>(picker.SelectedItem, "Row");
                LocalizationService.SetLanguage(language == "ru" ? "en" : "ru");
                Expect(Get<int>(picker.SelectedItem, "Row") == selectedRow, "Language change retargeted a pending trap choice to a door with the same ID.");
                LocalizationService.SetLanguage(language); Exact(before); window.ResetDraft();
            }
            if ((page.Type is "TRIGGER_CLICKELEMENT" or "TRIGGER_DESTROYED" or "TRIGGER_CREAENTERAREA" or "TRIGGER_STUNNEDINBAND" || page.Controls.Any(item => item.Unsigned))
                && Environment.GetEnvironmentVariable("DK2_SCENARIO_NAMED_RENDER_DIR") is { Length: > 0 } artifacts)
            {
                Directory.CreateDirectory(artifacts);
                var content = (FrameworkElement)window.Content; content.Measure(new Size(540, 420)); content.Arrange(new Rect(0, 0, 540, 420)); content.UpdateLayout();
                var bitmap = new RenderTargetBitmap(540, 420, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, "named-" + language + "-" + (native ? "native-" : "project-") + page.Type + ".png")); encoder.Save(output);
            }
            var firstControl = page.Controls[0];
            if (firstControl.Kind == "Number") Named<TextBox>("ScenarioParam" + firstControl.Parameter).Text = "1";
            else Choose(Named<ComboBox>("ScenarioParam" + firstControl.Parameter), (firstControl.Values.FirstOrDefault()).ToString(CultureInfo.InvariantCulture));
            window.Close(); Exact(before); Expect(!window.ApplyDraft(), "Closed named page remained editable.");
        }
        workspace.EndScenarioNamesLifetime();
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} 28 named condition windows, all source choices ({applied}), paired interface, unknown imports, atomic history, translation and Cancel");
    }
}
