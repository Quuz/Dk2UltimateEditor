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
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckUnusedScripts(bool native, string language, string directory)
    {
        var source = NativeMapDocument.CreateNewMap(new("UnusedUI", "UnusedUI", 24, 24, PlaceHearts: true, NativeFormat: native));
        ushort Add(string name)
        {
            var type = LegacyEventSchemas.All.Single(item => item.Name == name);
            var record = native ? source.AddRecord(RecordTemplate.All.Single(item => item.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0))
                : source.AddLegacyRecord(type.RecordSignature == 0xD5 ? "Trigger" : "Action", new(0, 0));
            source.SetEventType(record, type); return ScriptGraph.ID(record);
        }
        var retained = Add("TRIGGER_TIMEELAPSED"); var unused = Add("TRIGGER_TIMER"); Add("ACTION_ALLIES");
        var root = source.EnsureScriptScope(source.ScriptOwners().Single(item => item.Scope == 32)); source.SetField(root.Owner, root.Field, retained.ToString());
        var input = source.SaveCopy(Path.Combine(directory, $"unused-input-{language}-{native}"));
        var inputBytes = Directory.GetFiles(Path.GetDirectoryName(input)!, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        var shell = new MainWindow(new AppSettings(), persistSettings: false); shell.OpenMapFromPath(input);
        var workspace = (EditorWorkspace)shell.FindName("Editor"); var doc = workspace.Document!;
        var baseline = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        var calls = 0; var notices = new List<ScenarioNotice>(); shell.ScenarioNoticePresenter = notices.Add;
        void Exact() => Expect(doc.Files.All(file => file.Data.Span.SequenceEqual(baseline[file])) && doc.GetHistory().Undo.Count == history, "Scenario consent changed retained bytes/history.");
        void SetLink(ushort id, ushort target)
        {
            var record = ScriptGraph.Build(doc).Events[id].Record; var edge = ScriptGraph.Edges(record).Single(item => item.Field.Name is "or" or "next" or "NextOr");
            doc.SetField(record, edge.Field, target.ToString());
        }
        var validate = (Button)shell.FindName("ValidateButton"); var compile = (MenuItem)shell.FindName("CompileMenuItem");
        shell.UnusedScriptPrompt = request => { calls++; Expect(request.Purpose == ScenarioCheckPurpose.Test && request.Plan.Conditions == 1 && request.Plan.Actions == 1 && request.Plan.ReachableCount == 1, "Actual Test preview/counts are wrong."); return UnusedScriptDecision.Keep; };
        shell.ValidateDocument(doc); Expect(calls == 0, "Automatic read-only validation requested cleanup."); Exact();
        validate.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); FinishCompilerUiTask(shell); Expect(calls == 1 && notices.Count == 0 && shell.CurrentCompilerTaskWindow is {Succeeded:true} && shell.LastScenarioCheck is { Eligible: true, Removed: false }, "Actual Test/Keep not integrated."); Exact();
        shell.UnusedScriptPrompt = _ => { calls++; return UnusedScriptDecision.Cancel; };
        validate.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); FinishCompilerUiTask(shell); Expect(calls == 2 && notices.Count == 0 && shell.CurrentCompilerTaskWindow is {Succeeded:false} && shell.LastScenarioCheck is null, "Canceled Test continued to summary."); Exact();
        var request = new UnusedScriptRequest(ScenarioCheckPurpose.Test, doc.PlanUnusedScriptRemoval());
        var window = shell.CreateUnusedScriptConfirmation(request);
        var grid = (DataGrid)window.FindName("UnusedScriptsCandidates");
        Expect(grid.IsReadOnly && grid.Items.Count == 2 && ((Button)window.FindName("UnusedScriptsKeep")).IsDefault
            && ((Button)window.FindName("UnusedScriptsCancel")).IsCancel && !((Button)window.FindName("UnusedScriptsRemove")).IsDefault, "Unsafe dialog default or writable candidates.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); LocalizationService.SetLanguage(language);
        Expect(grid.Items.Count == 2 && Get<string>(grid.Items[0], "Type") == LocalizationService.Get("Event.TRIGGER_TIMER"), "Live candidate translation failed.");
        var content = (FrameworkElement)window.Content; content.Measure(new Size(640, double.PositiveInfinity));
        var height = (int)Math.Ceiling(content.DesiredSize.Height); content.Arrange(new Rect(0, 0, 640, height)); content.UpdateLayout();
        Expect(grid.Columns[2].ActualWidth >= 300 && grid.Columns.Sum(column => column.ActualWidth) <= grid.ActualWidth,
            "Event-type column collapsed or exceeds preview width.");
        foreach (var name in new[] { "UnusedScriptsKeep", "UnusedScriptsRemove", "UnusedScriptsCancel" })
        {
            var button = (Button)window.FindName(name); var position = button.TransformToAncestor(content).Transform(new Point());
            Expect(position.X + button.ActualWidth <= content.ActualWidth && position.Y + button.ActualHeight <= content.ActualHeight, "Unused-event decision button is clipped.");
        }
        Expect(((TextBlock)window.FindName("UnusedScriptsSummary")).Foreground == Application.Current.FindResource("Brush.Text"), "Unused-event summary is unreadable.");
        if (Environment.GetEnvironmentVariable("DK2_UNUSED_SCRIPT_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts); var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen())
            { drawing.DrawRectangle((Brush)Application.Current.FindResource("Brush.Background"), null, new Rect(0, 0, 640, height)); drawing.DrawRectangle(new VisualBrush(content) { Stretch = Stretch.Fill }, null, new Rect(16, 16, content.ActualWidth, content.ActualHeight)); }
            var bitmap = new RenderTargetBitmap(640, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new FileStream(Path.Combine(artifacts, $"unused-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        ((Button)window.FindName("UnusedScriptsCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(window.Decision == UnusedScriptDecision.Cancel && !window.Respond(UnusedScriptDecision.Remove), "Closed candidate window retained authority.");
        window = shell.CreateUnusedScriptConfirmation(request);
        ((Button)window.FindName("UnusedScriptsKeep")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(window.Decision == UnusedScriptDecision.Keep && !window.Respond(UnusedScriptDecision.Remove), "Actual Keep button retained deletion authority.");
        window = shell.CreateUnusedScriptConfirmation(request); window.Close();
        Expect(window.Decision == UnusedScriptDecision.Cancel && !window.Respond(UnusedScriptDecision.Remove), "Window close did not cancel cleanup.");
        shell.UnusedScriptPrompt = model =>
        {
            var dialog = shell.CreateUnusedScriptConfirmation(model); ((Button)dialog.FindName("UnusedScriptsRemove")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); return dialog.Decision;
        };
        validate.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); FinishCompilerUiTask(shell);
        Expect(shell.LastScenarioCheck is { Removed: true, Eligible: true } && ScriptGraph.Build(doc).Events.Count == 1 && doc.GetHistory().Undo.Count == history + 1, "Actual Remove not one live undoable edit.");
        doc.Undo(); Exact(); doc.Redo(); Expect(ScriptGraph.Build(doc).Events.Count == 1, "Cleanup Redo failed.");
        shell.UnusedScriptPrompt = _ => throw new InvalidOperationException("No-op cleanup asked a question.");
        Expect(shell.RequestScenarioValidation() is { Eligible: true, Removed: false } && doc.GetHistory().Undo.Count == history + 1, "Empty cleanup consumed history."); doc.Undo(); Exact();
        shell.UnusedScriptPrompt = model =>
        {
            var active = shell.CreateUnusedScriptConfirmation(model);
            typeof(MainWindow).GetField("unusedScriptWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(shell, active);
            doc.Paint([new(20, 20)], new(4, 2, 1)); doc.Undo();
            Expect(!active.Respond(UnusedScriptDecision.Remove), "Document change did not close actual consent window."); return UnusedScriptDecision.Remove;
        };
        Expect(shell.RequestScenarioValidation() is null, "Edit+Undo reused old cleanup consent."); Exact();
        shell.UnusedScriptPrompt = _ => { LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); LocalizationService.SetLanguage(language); return UnusedScriptDecision.Keep; };
        Expect(shell.RequestScenarioValidation() is { Removed: false, Eligible: true }, "Language refresh invalidated unchanged consent."); Exact();
        shell.UnusedScriptPrompt = _ => { Expect(shell.RequestScenarioValidation() is null && shell.RequestCompileProject() is null, "Reentrant scenario command was admitted."); return UnusedScriptDecision.Keep; };
        Expect(shell.RequestScenarioValidation() is { Removed: false }, "Current Keep failed after rejected reentrance."); Exact();
        shell.UnusedScriptPrompt = _ => { workspace.Document = null; workspace.Document = doc; return UnusedScriptDecision.Remove; };
        Expect(shell.RequestScenarioValidation() is null, "Replaced-and-restored workspace reused consent."); Exact();
        shell.UnusedScriptPrompt = _ => { workspace.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent)); return UnusedScriptDecision.Remove; };
        Expect(shell.RequestScenarioValidation() is null, "Unloaded workspace reused consent."); Exact();
        var rawRecord = ScriptGraph.Build(doc).Events[unused].Record;
        Expect(MemoryMarshal.TryGetArray(rawRecord.File.Data, out var raw), "No synthetic backing buffer.");
        var rawPosition = raw.Offset + rawRecord.Offset + rawRecord.Length - 1; var rawByte = raw.Array![rawPosition];
        shell.UnusedScriptPrompt = _ => { raw.Array[rawPosition] ^= 1; return UnusedScriptDecision.Keep; };
        Expect(shell.RequestScenarioValidation() is null, "Changed raw payload reused Keep consent."); raw.Array[rawPosition] = rawByte; Exact();
        shell.UnusedScriptPrompt = _ => throw new InvalidOperationException("Blocked retained graph/terrain asked for cleanup.");
        SetLink(retained, retained); var blocked = shell.RequestScenarioValidation();
        Expect(blocked is { Eligible: false } && blocked.Diagnostics.Any(item => item.Code == "UnusedScripts.Cycle"), "Retained cycle not blocked/reported."); doc.Undo(); Exact();
        doc.Paint(MapDrawing.Rectangle(new(12, 12, 5, 5), true), new(14, 3, 1, 217)); blocked = shell.RequestScenarioValidation();
        Expect(blocked is { Eligible: false } && blocked.Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error), "Non-script gameplay errors did not precede consent."); doc.Undo(); Exact();
        SetLink(unused, 32000); var dangling = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        shell.UnusedScriptPrompt = _ => UnusedScriptDecision.Keep;
        Expect(shell.RequestScenarioValidation() is { Eligible: false, Removed: false }, "Keep suppressed full dangling diagnostics.");
        Expect(doc.Files.All(file => file.Data.Span.SequenceEqual(dangling[file])), "Keep repaired unused dangling graph.");
        shell.UnusedScriptPrompt = _ => UnusedScriptDecision.Remove;
        Expect(shell.RequestScenarioValidation() is { Eligible: true, Removed: true }, "Full dangling gate prevented explicit unused removal.");
        doc.Undo(); doc.Undo(); Exact();
        // A pending actual property-grid edit is committed BEFORE the preview.
        var recordToEdit = ScriptGraph.Build(doc).Events[unused].Record;
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); var records = Control<ListBox>(workspace, "RecordList");
        categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == recordToEdit.Category);
        records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == recordToEdit.Offset);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var properties = Control<DataGrid>(workspace, "PropertyGrid"); workspace.Measure(new Size(1300, 900)); workspace.Arrange(new Rect(0, 0, 1300, 900)); workspace.UpdateLayout();
        var param = recordToEdit.Fields.First(field => field.Name is "Param1" or "param1");
        void BeginPending()
        {
            Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
            workspace.Measure(new Size(1300, 900)); workspace.Arrange(new Rect(0, 0, 1300, 900)); workspace.UpdateLayout();
            properties.SelectedItem = properties.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == param.Name);
            properties.ScrollIntoView(properties.SelectedItem, properties.Columns[1]); properties.UpdateLayout();
            properties.CurrentCell = new DataGridCellInfo(properties.SelectedItem, properties.Columns[1]); Expect(properties.BeginEdit(), "Pending event input did not enter editing."); properties.UpdateLayout();
            var inputBox = properties.Columns[1].GetCellContent(properties.SelectedItem) as TextBox; Expect(inputBox is not null, "Pending event input is not a TextBox."); inputBox!.Text = "1";
        }
        BeginPending();
        shell.UnusedScriptPrompt = _ => { Expect(ScriptGraph.Build(doc).Events[unused].Record.Read(param) == "1", "Preview preceded pending field commit."); return UnusedScriptDecision.Cancel; };
        Expect(shell.RequestScenarioValidation() is null && doc.GetHistory().Undo.Count == history + 1, "Cancel lost the separately committed pending field."); doc.Undo(); Exact();
        shell.UnusedScriptPrompt = _ => { BeginPending(); return UnusedScriptDecision.Remove; };
        Expect(shell.RequestScenarioValidation() is null && ScriptGraph.Build(doc).Events.Count == 3 && doc.GetHistory().Undo.Count == history + 1,
            "Pending input created during consent reused old removal authority."); doc.Undo(); Exact();
        if (!native)
        {
            var basis = NativeMapDocument.CreateNewMap(new("UnusedBase", "UnusedBase", 24, 24, PlaceHearts: true, NativeFormat: true));
            var basisPath = basis.SaveCopy(Path.Combine(directory, $"unused-basis-{language}"));
            var basisBytes = Directory.GetFiles(Path.GetDirectoryName(basisPath)!, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
            shell.CompileLocationPrompt = () => new(basisPath, directory); shell.CompileWarningsPrompt = _ => true;
            shell.UnusedScriptPrompt = model => { Expect(model.Purpose == ScenarioCheckPurpose.Compile, "Compile did not request source cleanup."); return UnusedScriptDecision.Keep; };
            compile.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); FinishCompilerUiTask(shell);
            Expect(shell.LastCompiledPath is not null && ScriptGraph.Build(NativeMapDocument.Load(shell.LastCompiledPath)).Events.Count == 3, "Actual Compile/Keep filtered unused entries."); Exact();
            shell.UnusedScriptPrompt = _ => UnusedScriptDecision.Remove; compile.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); FinishCompilerUiTask(shell);
            Expect(shell.LastCompiledPath is not null && ScriptGraph.Build(NativeMapDocument.Load(shell.LastCompiledPath)).Events.Count == 1 && doc.GetHistory().Undo.Count == history + 1, "Actual Compile/Remove not a live edit."); doc.Undo(); Exact();
            var directories = Directory.GetDirectories(directory).ToHashSet();
            var canceledCleanupCalls = 0; var locationCalls = 0;
            shell.UnusedScriptPrompt = _ => { canceledCleanupCalls++; return UnusedScriptDecision.Cancel; };
            shell.CompileLocationPrompt = () => { locationCalls++; return new(basisPath, directory); };
            compile.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); FinishCompilerUiTask(shell);
            Expect(locationCalls == 1 && canceledCleanupCalls == 1 && shell.LastCompiledPath is null && directories.SetEquals(Directory.GetDirectories(directory)), "Started task cleanup cancellation created output or was skipped."); Exact();
            var beforeLocationCancel = doc.ReadActivityStatistics()!.CompileTime;
            shell.UnusedScriptPrompt = _ => throw new InvalidOperationException("Canceled destination started a cleanup task.");
            shell.CompileLocationPrompt = () => null;
            compile.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); FinishCompilerUiTask(shell);
            Expect(doc.ReadActivityStatistics()!.CompileTime == beforeLocationCancel && shell.LastCompiledPath is null
                && directories.SetEquals(Directory.GetDirectories(directory)), "Canceled destination changed task date/output."); Exact();
            shell.UnusedScriptPrompt = _ => UnusedScriptDecision.Keep;
            shell.CompileLocationPrompt = () => { doc.Paint([new(20, 20)], new(4, 2, 1)); doc.Undo(); return new(basisPath, directory); };
            compile.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); FinishCompilerUiTask(shell); Expect(shell.LastCompiledPath is null && directories.SetEquals(Directory.GetDirectories(directory)), "Stale destination compiled changed input."); Exact();
            doc.Paint([new(20, 20)], new(1, 2, 3)); shell.CompileLocationPrompt = () => new(basisPath, directory);
            var warningCalls = 0; shell.CompileWarningsPrompt = _ => { warningCalls++; doc.Paint([new(20, 19)], new(4, 2, 1)); doc.Undo(); return true; };
            compile.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); FinishCompilerUiTask(shell);
            Expect(warningCalls == 1 && shell.LastCompiledPath is null && directories.SetEquals(Directory.GetDirectories(directory)), "Stale warning approval created output."); doc.Undo(); Exact();
            Expect(basisBytes.All(pair => File.ReadAllBytes(pair.Key).AsSpan().SequenceEqual(pair.Value)), "Compilation edited the native basis.");
        }
        shell.UnusedScriptPrompt = _ => { workspace.Document = null; shell.Close(); return UnusedScriptDecision.Remove; };
        Expect(shell.RequestScenarioValidation() is null && shell.RequestScenarioValidation() is null && shell.RequestCompileProject() is null, "Closed shell retained cleanup authority."); Exact();
        Expect(inputBytes.All(pair => File.ReadAllBytes(pair.Key).AsSpan().SequenceEqual(pair.Value)), "UI workflow edited original input files.");
        Console.WriteLine($"PASS WPF explicit Test/Compile unused-event consent, Keep/Remove/Cancel, exact undo, pending/stale/closed guards {language} {(native ? "native" : "project")}");
    }
}
