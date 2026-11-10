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
    private sealed record WindowControl(int Parameter, string Kind, long Maximum, int[] Values, bool Unsigned = false);
    private sealed record WindowPage(string Type, int PageID, WindowControl[] Controls);
    private sealed record WindowFixture(WindowPage[] Pages, string[] RepeatOnlyTypes);
    private static void CheckScenarioControlWindows(bool native, string language, string directory)
    {
        using var fixtureStream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioScalarPages.json")!;
        var fixture = JsonSerializer.Deserialize<WindowFixture>(fixtureStream)!;
        using var saveStream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioCreationDefaults.json")!;
        using var saveFixture = JsonDocument.Parse(saveStream);
        var clearOnSave = saveFixture.RootElement.GetProperty("CreationAndSaveFlow").GetProperty("Trigger").GetProperty("ZeroOnSaveTypes").EnumerateArray().Select(item => item.GetString()!).ToHashSet();
        Expect(clearOnSave.Count == 20, "Independent source zero-on-Save condition scope differs.");
        using var offsetStream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioParameterPages.json")!;
        var matrix = JsonSerializer.Deserialize<LiteralOptionPage[]>(offsetStream)!;
        var document = NativeMapDocument.CreateNewMap(new("ScalarUI", "ScalarUI", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
        var allPages = fixture.Pages.Concat(fixture.RepeatOnlyTypes.Select(type => new WindowPage(type, -1, []))).ToArray();
        var ids = new Dictionary<string, ushort>();
        foreach (var page in allPages)
        {
            var type = LegacyEventSchemas.All.Single(item => item.Name == page.Type); var kind = type.RecordSignature == 213 ? "Trigger" : "Action";
            var record = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == kind), new(0, 0)) : document.AddLegacyRecord(kind, new(0, 0));
            document.SetEventType(record, type); record = document.Records().Single(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length);
            ids[page.Type] = ScriptGraph.ID(record);
            Expect(MemoryMarshal.TryGetArray(record.File.Data, out var bytes) && bytes.Offset == 0, "Independent WPF fixture cannot seed opaque bytes.");
            for (var offset = native ? 8 : 16; offset < (native ? 16 : 48); offset++) bytes.Array![record.Offset + offset] = (byte)(0xA7 ^ offset);
            if (kind == "Trigger") bytes.Array![record.Offset + (native ? 23 : 55)] = 255;
        }
        var workspace = new EditorWorkspace { Document = document };
        var clipboard = document.CopySelection(new(0, 0, 1, 1)); workspace.LoadSelectionIntoBuffer(clipboard);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); var list = Control<ListBox>(workspace, "RecordList");
        void WorkspaceLayout() { workspace.Measure(new Size(1000, 760)); workspace.Arrange(new Rect(0, 0, 1000, 760)); workspace.UpdateLayout(); }
        NativeRecord Select(WindowPage page)
        {
            var record = ScriptGraph.Build(document).Events[ids[page.Type]].Record;
            categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            list.SelectedItem = list.Items.Cast<object>().Single(item => ScriptGraph.ID(Get<NativeRecord>(item, "Record")) == ids[page.Type]); WorkspaceLayout(); return record;
        }
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected) => Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)) && ReferenceEquals(workspace.BufferedSelection, clipboard), "Window changed unrelated bytes or clipboard.");
        void SourceSave(Dictionary<NativeFile, byte[]> expected, NativeRecord record, WindowPage page)
        { if (clearOnSave.Contains(page.Type)) Array.Clear(expected[record.File], record.Offset + (native ? 8 : 16), native ? 8 : 32); }
        LiteralOptionOperand Operand(WindowPage page, int parameter) => native ? matrix.Single(item => item.Type == page.Type).Native.Single(item => item.Name == "Param" + parameter) : new("param" + parameter, 16 + 4 * (parameter - 1), 4, "LONG");
        void Write(Dictionary<NativeFile, byte[]> expected, NativeRecord record, LiteralOptionOperand operand, long value)
        {
            var offset = record.Offset + operand.Offset;
            if (operand.Size == 1) expected[record.File][offset] = checked((byte)value);
            else if (operand.Size == 2) BinaryPrimitives.WriteUInt16LittleEndian(expected[record.File].AsSpan(offset), checked((ushort)value));
            else BinaryPrimitives.WriteInt32LittleEndian(expected[record.File].AsSpan(offset), checked((int)value));
        }
        foreach (var page in allPages)
        {
            var record = Select(page); var initial = Snapshot(); var revision = document.EditRevision; var history = document.GetHistory().Undo.Count; var dirty = document.HasChanges;
            var command = Control<Button>(workspace, "ScenarioParameterButton"); Expect(command.Visibility == Visibility.Visible && command.IsEnabled, "Source page command unavailable.");
            ScenarioParameterWindow? presented = null; workspace.ScenarioParameterPresenter = window => presented = window;
            command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); var window = presented!; Expect(window is not null, "Actual command did not present parameter window.");
            T Named<T>(string name) where T : FrameworkElement => (T)window!.FindName(name);
            void Layout() { var content = (FrameworkElement)window!.Content; content.Measure(new Size(540, 420)); content.Arrange(new Rect(0, 0, 540, 420)); content.UpdateLayout(); }
            void Choose(ComboBox picker, string value) => picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<string>(item, "Value") == value);
            void Mode(string mode) { var picker = Named<ComboBox>("ScenarioRepeatMode"); picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<object>(item, "Mode").ToString() == mode); }
            Layout(); Expect(!window!.HasPendingInputs, "Opening imported values produced pending edits."); Exact(initial);
            var savedInitial = initial.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone()); SourceSave(savedInitial, record, page);
            Expect(window.ApplyDraft(), "Explicit source Save rejected imported values."); Exact(savedInitial);
            if (clearOnSave.Contains(page.Type))
            {
                Expect(document.GetHistory().Undo.Count == history + 1, "Source zero-on-Save did not create one atomic edit.");
                document.Undo(); Exact(initial); window.ResetDraft();
            }
            else Expect(document.EditRevision == revision + 1 && document.GetHistory().Undo.Count == history && document.HasChanges, "Accepted equal source Save did not advance revision without adding Undo.");
            foreach (var control in page.Controls.Where(item => item.Unsigned))
            {
                var operand = Operand(page, control.Parameter);
                if (operand.Size != 4) continue;
                var imported = BinaryPrimitives.ReadUInt32LittleEndian(initial[record.File].AsSpan(record.Offset + operand.Offset)).ToString(CultureInfo.InvariantCulture);
                var input = Named<TextBox>("ScenarioParam" + control.Parameter);
                Expect(input.Text == imported, "Original unsigned scalar displayed a signed import.");
                input.Text = "0"; input.Text = imported;
                LocalizationService.SetLanguage(language == "ru" ? "en" : "ru");
                Expect(input.Text == imported, "Translation changed unsigned scalar input."); LocalizationService.SetLanguage(language);
                var equalRevision = document.EditRevision; var equalHistory = document.GetHistory();
                Expect(window.ApplyDraft(), "Unsigned scalar re-entry rejected unchanged source bits."); Exact(initial);
                var afterEqual = document.GetHistory();
                Expect(document.EditRevision == equalRevision + 1 && afterEqual.ChangeCount == equalHistory.ChangeCount + 1 && afterEqual.EditorChangeCount == unchecked(equalHistory.EditorChangeCount + 1) && afterEqual.Undo.SequenceEqual(equalHistory.Undo) && afterEqual.Redo.SequenceEqual(equalHistory.Redo), "Unsigned scalar accepted source Save changed history or failed to count once.");
            }
            var expected = Snapshot();
            SourceSave(expected, record, page);
            foreach (var control in page.Controls)
            {
                var operand = Operand(page, control.Parameter); var maximum = operand.Size == 1 ? Math.Min(255, control.Maximum) : control.Maximum;
                if (control.Kind == "Choice")
                {
                    var picker = Named<ComboBox>("ScenarioParam" + control.Parameter);
                    Expect(picker.Items.Count == control.Values.Length + 1, "Unknown/source choices not retained.");
                    foreach (var value in control.Values)
                    {
                        Choose(picker, value.ToString()); var single = Snapshot(); Write(single, record, operand, value);
                        Expect(window.ApplyDraft(), "Original choice failed Apply."); Exact(single); document.Undo(); Exact(initial); window.ResetDraft();
                    }
                    Choose(picker, maximum.ToString(CultureInfo.InvariantCulture));
                }
                else if (control.Kind == "Duration")
                {
                    Named<TextBox>("ScenarioHours").Text = "23"; Named<TextBox>("ScenarioMinutes").Text = "59"; Named<TextBox>("ScenarioSeconds").Text = "59";
                    Expect(Named<TextBox>("ScenarioParam" + control.Parameter).IsReadOnly && Named<TextBox>("ScenarioParam" + control.Parameter).Text == "86399", "Actual source time units/total differ.");
                }
                else if (control.Kind == "Percentage") { Named<Slider>("ScenarioMusicSlider").Value = 99; Named<Slider>("ScenarioMusicSlider").Value = 100; }
                else Named<TextBox>("ScenarioParam" + control.Parameter).Text = maximum.ToString(CultureInfo.InvariantCulture);
                Write(expected, record, operand, maximum);
            }
            if (page.Type.StartsWith("TRIGGER_", StringComparison.Ordinal))
            { Mode("Count"); Named<TextBox>("ScenarioRepeatCount").Text = "254"; expected[record.File][record.Offset + (native ? 23 : 55)] = 254; }
            var pending = page.Controls.Where(item => item.Kind != "Choice").Select(item => Named<TextBox>("ScenarioParam" + item.Parameter).Text).ToArray();
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Layout();
            Expect(page.Controls.Where(item => item.Kind != "Choice").Select(item => Named<TextBox>("ScenarioParam" + item.Parameter).Text).SequenceEqual(pending) && window.HasPendingInputs, "Language switch discarded pending inputs: " + page.Type);
            Expect((string)Named<Button>("ScenarioPageApply").Content == LocalizationService.Get("ScenarioPage.Apply") && window.Title == LocalizationService.Get("ScenarioPage.Title"), "Actual parameter title/button did not translate.");
            LocalizationService.SetLanguage(language); Exact(initial);
            Named<Button>("ScenarioPageApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(expected);
            Expect(document.GetHistory().Undo.Count == history + 1 && !window.HasPendingInputs, "Whole page did not apply atomically."); document.Undo(); Exact(initial); document.Redo(); Exact(expected); document.Undo(); Exact(initial); window.ResetDraft();
            foreach (var control in page.Controls.Where(item => item.Kind is "Number" or "Percentage"))
            { Named<TextBox>("ScenarioParam" + control.Parameter).Text = (control.Maximum + 1).ToString(); Expect(!window.ApplyDraft(), "Out-of-source-range number was applied."); Exact(initial); window.ResetDraft(); }
            if (page.Controls.Any(item => item.Kind == "Duration"))
            {
                Named<TextBox>("ScenarioHours").Text = "24"; Named<TextBox>("ScenarioMinutes").Text = "60"; Named<TextBox>("ScenarioSeconds").Text = "60";
                Expect(Named<TextBox>("ScenarioHours").Text == "23" && Named<TextBox>("ScenarioMinutes").Text == "59" && Named<TextBox>("ScenarioSeconds").Text == "59", "Actual source time callback did not clamp 23/59/59.");
                Expect(window.ApplyDraft(), "Clamped time failed Apply."); document.Undo(); Exact(initial); window.ResetDraft();
                Named<TextBox>("ScenarioMinutes").Text = "x"; Expect(!window.ApplyDraft(), "Invalid time input was applied."); Exact(initial); window.ResetDraft();
            }
            if (page.Controls.Any(item => item.Kind is "Duration" or "Percentage" || item.Unsigned) && Environment.GetEnvironmentVariable("DK2_SCENARIO_CONTROL_RENDER_DIR") is { Length: > 0 } artifacts)
            {
                Directory.CreateDirectory(artifacts);
                void Render(string state)
                {
                    var content = (FrameworkElement)window.Content; Layout(); var bitmap = new RenderTargetBitmap(540, 420, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = File.Create(Path.Combine(artifacts, $"{language}-{native}-{page.Type}-{state}-540.png")); encoder.Save(output);
                }
                Render("imported");
                if (page.Controls.Any(item => item.Kind == "Duration"))
                { Named<TextBox>("ScenarioHours").Text = "1"; Named<TextBox>("ScenarioMinutes").Text = "2"; Named<TextBox>("ScenarioSeconds").Text = "3"; }
                else if (page.Controls.Any(item => item.Kind == "Percentage")) { Named<Slider>("ScenarioMusicSlider").Value = 99; Named<Slider>("ScenarioMusicSlider").Value = 75; }
                else foreach (var control in page.Controls.Where(item => item.Unsigned)) Named<TextBox>("ScenarioParam" + control.Parameter).Text = "1";
                if (page.Type.StartsWith("TRIGGER_", StringComparison.Ordinal)) { Choose(Named<ComboBox>("ScenarioParam1"), "3"); Mode("Count"); Named<TextBox>("ScenarioRepeatCount").Text = "254"; }
                Render("edited"); Exact(initial); window.ResetDraft();
            }
            if (page.Type.StartsWith("TRIGGER_", StringComparison.Ordinal))
            {
                foreach (var (mode, text, value) in new[] { ("Count", "0", 0), ("Count", "254", 254), ("Once", "invalid", 1), ("Infinite", "invalid", 255) })
                {
                    Mode(mode); Named<TextBox>("ScenarioRepeatCount").Text = text;
                    Expect(Named<TextBox>("ScenarioRepeatCount").IsEnabled == (mode == "Count"), "Repeat count active state differs.");
                    var after = Snapshot(); after[record.File][record.Offset + (native ? 23 : 55)] = (byte)value;
                    SourceSave(after, record, page);
                    Expect(window.ApplyDraft(), "Source repeat mode failed."); Exact(after);
                    if (value != 255 || clearOnSave.Contains(page.Type)) document.Undo(); Exact(initial); window.ResetDraft();
                }
                Mode("Count"); Named<TextBox>("ScenarioRepeatCount").Text = "300"; Expect(Named<TextBox>("ScenarioRepeatCount").Text == "254", "Source repeat callback did not clamp 254.");
            }
            window.Close(); Expect(!window.ApplyDraft(), "Closed window retained write authority."); Exact(initial);
        }
        // Real pending property editor, reentrance, edited inputs during commit, and owner lifetime.
        var sample = fixture.Pages.Single(item => item.Type == "TRIGGER_GOLDAMOUNT"); var selected = Select(sample); var baseline = Snapshot();
        var pendingWindow = workspace.CreateScenarioParameterWindow()!; ((TextBox)pendingWindow.FindName("ScenarioParam5")).Text = "42";
        var grid = Control<DataGrid>(workspace, "PropertyGrid"); var row = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == (native ? "Param5" : "param5"));
        grid.SelectedItem = row; grid.ScrollIntoView(row); WorkspaceLayout(); grid.CurrentCell = new(row, grid.Columns[1]); Expect(grid.BeginEdit(), "Pending generic editor did not start."); WorkspaceLayout();
        var editor = grid.Columns[1].GetCellContent(row) as TextBox; Expect(editor is not null, "Actual pending field TextBox missing."); editor!.Text = "7";
        Expect(!pendingWindow.ApplyDraft(), "Pending generic commit accepted stale page edits."); Write(baseline, selected, Operand(sample, 5), 7); Exact(baseline); document.Undo(); pendingWindow.Close();
        var beforeGuards = Snapshot(); ScenarioParameterWindow? reentrant = null; var nested = true;
        reentrant = new(document, selected, () => true, () => { nested = reentrant!.ApplyDraft(); return true; }); Expect(reentrant.ApplyDraft() && !nested, "Reentrant page Apply was accepted."); reentrant.Close(); Exact(beforeGuards);
        ScenarioParameterWindow? changing = null; changing = new(document, selected, () => true, () => { ((TextBox)changing!.FindName("ScenarioParam5")).Text = "99"; return true; });
        Expect(!changing.ApplyDraft(), "Inputs changed during commit were applied."); changing.Close(); Exact(beforeGuards);
        var identity = new ScenarioParameterWindow(document, selected, () => true, () => true);
        document.SetEventType(selected, LegacyEventSchemas.All.Single(item => item.Name == "TRIGGER_GOLDMINED")); var changedType = Snapshot();
        identity.ResetDraft(); Expect(!identity.ApplyDraft(), "Reset retargeted old window controls to another event type."); Exact(changedType); document.Undo(); identity.Close(); Exact(beforeGuards);
        var raw = new ScenarioParameterWindow(document, selected, () => true, () => true); ((TextBox)raw.FindName("ScenarioParam5")).Text = "42";
        Expect(MemoryMarshal.TryGetArray(selected.File.Data, out var mutable), "Raw fingerprint test buffer missing."); var rawOffset = selected.Offset + Operand(sample, 5).Offset;
        var original = mutable.Array![rawOffset]; mutable.Array[rawOffset] ^= 1; Expect(!raw.ApplyDraft(), "Unnotified raw mutation accepted a stale page."); mutable.Array[rawOffset] = original; raw.Close(); Exact(beforeGuards);
        var closing = workspace.CreateScenarioParameterWindow()!; workspace.EndScenarioNamesLifetime(); Expect(!closing.ApplyDraft(), "Closed workspace retained page authority."); closing.Close(); Exact(beforeGuards);
        workspace.Document = null; Expect(Control<Button>(workspace, "ScenarioParameterButton").Visibility == Visibility.Collapsed, "Closed document retained page command.");
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} 19 scalar/26 repeat parameter windows, literal bytes, atomic batches, source ranges, Cancel and pending/lifetime guards");
    }
}
