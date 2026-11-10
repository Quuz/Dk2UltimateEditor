using System.Buffers.Binary;
using System.IO;
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
    private sealed record SemanticOptionValue(int Value, string English, string Russian);
    private sealed record SemanticOptionPage(string Type, int Parameter, SemanticOptionValue[] Values);
    private sealed record ComparisonFixture(string Type, int PageID, int[] Values, int[]? Controls, int Parameter = 1);
    private static void CheckScenarioParameterChoices(bool native, string language, string directory)
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioComparisonPages.json")!;
        var comparisons = JsonSerializer.Deserialize<ComparisonFixture[]>(stream)!;
        using var enums = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioEnumPages.json")!;
        var enumPages = JsonSerializer.Deserialize<ComparisonFixture[]>(enums)!;
        Expect(comparisons.Length == 30 && enumPages.Length == 20, "Original UI option fixture incomplete.");
        using var semanticStream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioSemanticOptions.json")!;
        var semanticPages = JsonSerializer.Deserialize<SemanticOptionPage[]>(semanticStream)!;
        Expect(semanticPages.Length == 2, "Independent UI semantic fixture incomplete.");
        var pages = comparisons.Concat(enumPages).ToArray();
        using var literalStream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioParameterPages.json")!;
        var matrix = JsonSerializer.Deserialize<LiteralOptionPage[]>(literalStream)!;
        var document = NativeMapDocument.CreateNewMap(new("ParameterUI", "ParameterUI", 20, 20, NativeFormat: native));
        document.SetHistoryDepth(65535);
        var ids = new Dictionary<string, ushort>();
        foreach (var page in pages)
        {
            var type = LegacyEventSchemas.All.Single(item => item.Name == page.Type);
            if (!ids.ContainsKey(page.Type))
            {
                var structure = type.RecordSignature == 213 ? "Trigger" : "Action";
                var added = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == structure), new(0, 0)) : document.AddLegacyRecord(structure, new(0, 0));
                document.SetEventType(added, type); ids.Add(page.Type, ScriptGraph.ID(added));
            }
            var record = ScriptGraph.Build(document).Events[ids[page.Type]].Record;
            document.SetField(record, record.Fields.Single(item => item.Name == (native ? "Param" : "param") + page.Parameter), "254");
        }
        var workspace = new EditorWorkspace { Document = document };
        var clipboard = document.CopySelection(new(0, 0, 1, 1)); workspace.LoadSelectionIntoBuffer(clipboard);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var categories = Control<ComboBox>(workspace, "CategoryPicker");
        var records = Control<ListBox>(workspace, "RecordList");
        var grid = Control<DataGrid>(workspace, "PropertyGrid");
        var picker = Control<ComboBox>(workspace, "NamedValuePicker");
        var panel = Control<StackPanel>(workspace, "NamedValuePanel");
        var apply = panel.Children.OfType<WrapPanel>().Single().Children.OfType<Button>().Single(button => button.Name.Length == 0);
        void Layout()
        {
            workspace.Measure(new Size(1000, 760)); workspace.Arrange(new Rect(0, 0, 1000, 760)); workspace.UpdateLayout();
        }
        void SelectField(ComparisonFixture page)
        {
            var record = ScriptGraph.Build(document).Events[ids[page.Type]].Record;
            categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            records.SelectedItem = records.Items.Cast<object>().Single(item => ScriptGraph.ID(Get<NativeRecord>(item, "Record")) == ids[page.Type]);
            grid.SelectedItem = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == (native ? "Param" : "param") + page.Parameter);
            Layout();
        }
        void CheckPicker(ComparisonFixture page, string currentLanguage)
        {
            var choices = picker.Items.Cast<object>().Select(item => Get<ValueChoice>(item, "Choice")).ToArray();
            Expect(panel.Visibility == Visibility.Visible && choices.Length == page.Values.Length + 1 && choices[0].Value == "254"
                && choices.Skip(1).Select(item => int.Parse(item.Value)).Order().SequenceEqual(page.Values.Order()), "Actual picker lost unknown/source option choices.");
            Expect(Get<ValueChoice>(picker.SelectedItem, "Choice").Value == "254", "Unknown imported operator selected a repair implicitly.");
            Expect(picker.Items.Cast<object>().All(item => Get<string>(item, "Label").Contains(Get<ValueChoice>(item, "Choice").Name(currentLanguage), StringComparison.Ordinal)), "Actual choice labels did not translate.");
            var semantic = semanticPages.SingleOrDefault(item => item.Type == page.Type && item.Parameter == page.Parameter);
            if (semantic is not null)
                foreach (var expected in semantic.Values)
                {
                    var item = picker.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == expected.Value.ToString());
                    var choice = Get<ValueChoice>(item, "Choice");
                    Expect(choice.English == expected.English && choice.Russian == expected.Russian
                        && Get<string>(item, "Label").Contains(currentLanguage == "ru" ? expected.Russian : expected.English, StringComparison.Ordinal),
                        "Actual semantic flag label/value differs from independent source: " + page.Type);
                }
        }
        foreach (var page in pages)
        {
            SelectField(page);
            var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var revision = document.EditRevision; var history = document.GetHistory().Undo.Count; var dirty = document.HasChanges;
            CheckPicker(page, language);
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); SelectField(page);
            CheckPicker(page, LocalizationService.CurrentLanguage);
            LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); SelectField(page);
            Expect(document.EditRevision == revision && document.GetHistory().Undo.Count == history && document.HasChanges == dirty
                && before.All(item => item.Key.Data.Span.SequenceEqual(item.Value)) && ReferenceEquals(workspace.BufferedSelection, clipboard), "Browsing/translating comparisons changed data/history/copy buffer.");
            foreach (var value in page.Values)
            {
                SelectField(page);
                picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == value.ToString());
                Expect(apply.IsEnabled, "Comparison Apply is unavailable.");
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var record = ScriptGraph.Build(document).Events[ids[page.Type]].Record;
                var expected = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                if (native)
                {
                    var operand = matrix.Single(item => item.Type == page.Type).Native.Single(item => item.Name == "Param" + page.Parameter);
                    if (operand.Size == 1) expected[record.File][record.Offset + operand.Offset] = checked((byte)value);
                    else if (operand.Size == 2) BinaryPrimitives.WriteUInt16LittleEndian(expected[record.File].AsSpan(record.Offset + operand.Offset), checked((ushort)value));
                    else BinaryPrimitives.WriteInt32LittleEndian(expected[record.File].AsSpan(record.Offset + operand.Offset), value);
                }
                else BinaryPrimitives.WriteInt32LittleEndian(expected[record.File].AsSpan(record.Offset + 16 + (page.Parameter - 1) * 4), value);
                Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)) && document.GetHistory().Undo.Count == history + 1
                    && ReferenceEquals(workspace.BufferedSelection, clipboard), "Actual comparison Apply changed unrelated data or failed one-step history.");
                if (page == pages[0] && value == 2)
                {
                    var imageDirectory = Environment.GetEnvironmentVariable("DK2_SCENARIO_PARAMETER_RENDER_DIR") ?? Path.Combine(directory, "scenario-parameters-render"); Directory.CreateDirectory(imageDirectory);
                    workspace.Measure(new Size(900, 600)); workspace.Arrange(new Rect(0, 0, 900, 600)); workspace.UpdateLayout();
                    var bitmap = new RenderTargetBitmap(900, 600, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var image = File.Create(Path.Combine(imageDirectory, $"{language}-{(native ? "native" : "project")}-900.png")); encoder.Save(image);
                }
                document.Undo(); Expect(before.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Actual comparison Undo differs from original imported bytes.");
                document.Redo(); Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Actual comparison Redo differs from accepted bytes.");
                document.Undo();
            }
        }
        workspace.Document = null;
        Expect(panel.Visibility == Visibility.Collapsed && picker.Items.Count == 0, "Closed document retained comparison authority.");
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} all 30 comparison and 20 source radio pickers, actual Apply/Undo/Redo values, unknown retention, live translation and copy buffer");
    }
    private sealed record LiteralOptionOperand(string Name, int Offset, int Size, string Kind);
    private sealed record LiteralOptionPage(string Type, LiteralOptionOperand[] Native);
}
