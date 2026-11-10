using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioNames(bool native, string language, string directory)
    {
        var original = NativeMapDocument.CreateNewMap(new("AliasUI", "AliasUI", 24, 24, NativeFormat: native));
        var type = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_VARIABLE");
        var added = native ? original.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Trigger"), new(0, 0)) : original.AddLegacyRecord("Trigger", new(0, 0));
        original.SetEventType(added, type); var id = ScriptGraph.ID(added);
        var sourceRecord = ScriptGraph.Build(original).Events[id].Record; var indexField = sourceRecord.Fields.Single(field => field.Name == (native ? "Param2" : "param2")); original.SetField(sourceRecord, indexField, "7");
        var input = original.SaveCopy(Path.Combine(directory, $"alias-ui-input-{language}-{native}"));
        var disk = Directory.GetFiles(Path.GetDirectoryName(input)!, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        var shell = new MainWindow(new AppSettings(), persistSettings: false); shell.OpenMapFromPath(input);
        var workspace = (EditorWorkspace)shell.FindName("Editor"); var doc = workspace.Document!;
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 2;
        ScenarioNamesWindow? window = null; workspace.ScenarioNamesPresenter = shown => window = shown;
        var command = Control<Button>(workspace, "ScenarioNamesButton"); command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(window is not null, "Actual scenario-name command did not create a window.");
        T Named<T>(string name) => (T)window!.FindName(name);
        var variables = Named<DataGrid>("ScenarioVariables"); var timers = Named<DataGrid>("ScenarioTimers"); var name = Named<TextBox>("ScenarioNameInput");
        Expect(variables.Items.Count == 129 && timers.Items.Count == 17 && variables.IsReadOnly && timers.IsReadOnly, "Original alias lists not complete/read-only.");
        variables.SelectedItem = variables.Items.Cast<object>().Single(row => Get<int>(row, "Index") == 7);
        Expect(Get<int>(variables.SelectedItem, "References") == 1 && name.MaxLength == 63, "Source usage/capacity missing.");
        name.Text = "Pending alias"; LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(name.Text == "Pending alias" && Named<Button>("ScenarioNameApply").IsEnabled, "Language switching discarded alias draft."); LocalizationService.SetLanguage(language);
        Named<Button>("ScenarioNameDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(name.Text == "", "Discard changed data or retained input.");
        name.Text = "Имя"; Expect(!window!.ApplyName() && Named<TextBlock>("ScenarioNamesStatus").Text == LocalizationService.Get("ScenarioNames.Invalid"), "Unrepresentable alias silently replaced or error untranslated.");
        name.Text = "Wave count"; Named<Button>("ScenarioNameApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(doc.ReadScenarioNames().Slots.Single(row => row.Kind == ScenarioNameKind.Variable && row.Index == 7).StoredName == "Wave count"
            && doc.GetHistory().Undo.Count == history && doc.EditorChangeCount == 1 && doc.HasChanges, "Actual alias Apply missing/nonatomic.");
        if (native) Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Native alias changed game bytes.");
        var output = doc.SaveCopy(Path.Combine(directory, $"alias-ui-saved-{language}-{native}"));
        Expect(NativeMapDocument.Load(output).ReadScenarioNames().Slots.Single(row => row.Kind == ScenarioNameKind.Variable && row.Index == 7).StoredName == "Wave count", "Actual rename/save/reopen lost alias.");
        // Exact source special display does not erase its independently editable raw alias.
        timers.SelectedItem = timers.Items.Cast<object>().Single(row => Get<int>(row, "Index") == 16);
        name.Text = "Hidden timer name"; Expect(window.ApplyName(), "Special raw alias is not editable.");
        Expect(Get<string>(timers.SelectedItem, "Display") == LocalizationService.Get("ScenarioNames.TimeLimit") && name.Text == "Hidden timer name", "Special caption overwrote stored alias.");
        var content = (FrameworkElement)window.Content; content.Measure(new Size(760, 560)); content.Arrange(new Rect(0, 0, 760, 560)); content.UpdateLayout();
        Expect(variables.ActualWidth >= 270 && timers.ActualWidth >= 270 && Named<TextBlock>("ScenarioNamesStatus").Foreground == Application.Current.FindResource("Brush.Text"), "Name-manager tables/text unreadable.");
        if (Environment.GetEnvironmentVariable("DK2_SCENARIO_NAMES_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts); var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen())
            { drawing.DrawRectangle((Brush)Application.Current.FindResource("Brush.Background"), null, new Rect(0, 0, 760, 560)); drawing.DrawRectangle(new VisualBrush(content) { Stretch = Stretch.Fill }, null, new Rect(12, 12, content.ActualWidth, content.ActualHeight)); }
            var bitmap = new RenderTargetBitmap(760, 560, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = new FileStream(Path.Combine(artifacts, $"names-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(stream);
        }
        var managerBytes=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        var managerCount=doc.EditorChangeCount;var managerHistory=doc.GetHistory();var managerStats=doc.ReadActivityStatistics();
        var oldTile=doc.GetTile(20,20);doc.Paint([new(20,20)],new((byte)(oldTile.Terrain==6?4:6),2,1));
        doc.Undo();
        Expect(managerBytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&doc.EditorChangeCount==managerCount
            &&doc.ReadScenarioNames().Slots.Single(row=>row.Kind==ScenarioNameKind.Variable&&row.Index==7).StoredName=="Wave count"
            &&doc.ReadScenarioNames().Slots.Single(row=>row.Kind==ScenarioNameKind.Timer&&row.Index==16).StoredName=="Hidden timer name",
            "Ordinary Undo did not restore every byte while retaining manager aliases.");
        Expect(!window.ApplyName() && Named<TextBlock>("ScenarioNamesStatus").Text == LocalizationService.Get("ScenarioNames.Stale"), "Undo did not invalidate old draft.");
        Named<Button>("ScenarioNameDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        name.Text = "Replacement"; Expect(window.ApplyName(), "Refreshed rename unavailable.");
        Expect(doc.GetHistory().Undo.Count==managerHistory.Undo.Count&&doc.GetHistory().Redo.Count==1&&doc.EditorChangeCount==managerCount+1,
            "Manager replacement allocated Undo or destroyed previous redo.");
        var replacementBytes=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        if(native)Expect(managerBytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Native manager changed game bytes.");
        else foreach(var pair in managerBytes)
        {
            var expected=pair.Value.ToArray();if(pair.Key.Signature==NativeMapDocument.LegacyEvents)
            {var offset=expected.Length-64;expected.AsSpan(offset,64).Clear();System.Text.Encoding.ASCII.GetBytes("Replacement").CopyTo(expected,offset);}
            Expect(pair.Key.Data.Span.SequenceEqual(expected),"Manager changed unrelated project bytes.");
        }
        doc.Redo();Expect(doc.ReadScenarioNames().Slots.Single(row=>row.Kind==ScenarioNameKind.Timer&&row.Index==16).StoredName=="Replacement",
            "Previous redo discarded manager replacement.");doc.Undo();
        Expect(replacementBytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&doc.GetHistory().Undo.Count==history&&doc.EditorChangeCount==managerCount+1,
            "Alias-preserving previous Redo/Undo did not restore every unrelated byte/count.");
        if(managerStats is not null)Expect(doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual((managerStats with{UsageOfUndo=unchecked(managerStats.UsageOfUndo+2)}).Encode()),
            "Manager or replay changed activity fields beyond two source Undo uses.");
        window.Close();
        // The ordinary event property picker now projects aliases and retains unknown IDs.
        doc.SetScenarioName(ScenarioNameKind.Variable, 7, "Wave count");
        var record = ScriptGraph.Build(doc).Events[id].Record;
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); var records = Control<ListBox>(workspace, "RecordList");
        categories.SelectedItem = categories.Items.Cast<object>().Single(row => Get<string>(row, "ID") == record.Category);
        records.SelectedItem = records.Items.Cast<object>().Single(row => Get<NativeRecord>(row, "Record").Offset == record.Offset);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var fields = Control<DataGrid>(workspace, "PropertyGrid"); fields.SelectedItem = fields.Items.Cast<object>().Single(row => Get<NativeField>(row, "Field").Name == indexField.Name);
        var picker = Control<ComboBox>(workspace, "NamedValuePicker");
        Expect(picker.Items.Cast<object>().Any(row => Get<ValueChoice>(row, "Choice").Value == "7" && Get<ValueChoice>(row, "Choice").English == "Wave count"), "Actual event property picker lacks aliases.");
        doc.SetField(record, indexField, "200");
        Expect(Get<ValueChoice>(picker.SelectedItem, "Choice").Value == "200"
            && picker.Items.Cast<object>().Any(row => Get<ValueChoice>(row, "Choice").Value == "200"), "Alias picker coerced an imported unknown index."); doc.Undo();
        window = workspace.CreateScenarioNamesWindow()!; name = Named<TextBox>("ScenarioNameInput"); name.Text = "Must not apply";
        // Exercise a real pending DataGrid edit while the modeless manager is open.
        workspace.Measure(new Size(1300, 900)); workspace.Arrange(new Rect(0, 0, 1300, 900)); workspace.UpdateLayout();
        fields.SelectedItem = fields.Items.Cast<object>().Single(row => Get<NativeField>(row, "Field").Name == indexField.Name);
        fields.ScrollIntoView(fields.SelectedItem, fields.Columns[1]); fields.UpdateLayout();
        fields.CurrentCell = new DataGridCellInfo(fields.SelectedItem, fields.Columns[1]); Expect(fields.BeginEdit(), "Pending naming input did not enter property editing."); fields.UpdateLayout();
        var pending = fields.Columns[1].GetCellContent(fields.SelectedItem) as TextBox;
        Expect(pending is not null, "Pending naming field has no actual editor."); pending!.Text = "8";
        var pendingHistory = doc.GetHistory().Undo.Count;
        Expect(!window.ApplyName() && ScriptGraph.Build(doc).Events[id].Record.Read(indexField) == "8"
            && doc.GetHistory().Undo.Count == pendingHistory + 1 && name.Text == "Must not apply"
            && !Named<Button>("ScenarioNameApply").IsEnabled, "Pending commit reused naming authority or discarded input."); doc.Undo();
        Named<Button>("ScenarioNameDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); name.Text = "Raw mutation must not apply";
        record = ScriptGraph.Build(doc).Events[id].Record;
        Expect(MemoryMarshal.TryGetArray(record.File.Data, out var backing), "Synthetic raw-mutation backing unavailable.");
        var rawOffset = backing.Offset + record.Offset + indexField.Offset; var rawValue = backing.Array![rawOffset]; backing.Array[rawOffset] ^= 1;
        Expect(!window.ApplyName() && !Named<Button>("ScenarioNameApply").IsEnabled
            && Named<TextBlock>("ScenarioNamesStatus").Text == LocalizationService.Get("ScenarioNames.Stale"), "Raw byte changes reused a draft or left stale Apply enabled.");
        backing.Array[rawOffset] = rawValue;
        Named<Button>("ScenarioNameDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var noOpHistory = doc.GetHistory().Undo.Count;var equalCount=doc.EditorChangeCount;var equalRevision=doc.EditRevision;
        Expect(window.ApplyName() && doc.GetHistory().Undo.Count == noOpHistory&&doc.EditorChangeCount==equalCount+1&&doc.EditRevision==equalRevision+1,
            "Accepted equal GUI name did not count once without Undo."); window.Close();
        doc.Undo(); workspace.Document = null;
        Expect(!command.IsEnabled && !window.ApplyName(), "Closed-document naming command/draft retained authority.");
        workspace.Document = doc; window = workspace.CreateScenarioNamesWindow()!;
        workspace.Document = null; workspace.Document = doc; Expect(!window.ApplyName(), "Replaced-and-restored workspace reused rename authority."); window.Close();
        var discardCalls=0;shell.DiscardChangesPrompt=()=>{discardCalls++;return true;};
        workspace.Document = null; shell.Close(); Expect(workspace.CreateScenarioNamesWindow() is null&&discardCalls==1,
            "Closed shell retained name manager or silently skipped explicit discard of nonhistorical aliases.");
        Expect(disk.All(pair => File.ReadAllBytes(pair.Key).AsSpan().SequenceEqual(pair.Value)), "Name-manager edited original disk input.");
        Console.WriteLine($"PASS WPF scenario names, original lists/counts, alias-aware property choices, native metadata, exact history and draft lifetime {language} {(native ? "native" : "project")}");
    }
}
