using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioCameraParent(bool native, string language, string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("CameraParentUI", "CameraParentUI", 20, 20, NativeFormat: native));
        foreach (var id in new[] { 13, 39 })
        {
            var area = doc.AddMapRegion(MapRegionKind.Area, new(id == 13 ? 2 : 6, 2, 1, 1));
            doc.SetField(area, EntityReferences.Identity(area)!.Field, id.ToString());
        }
        var added = native ? doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0)) : doc.AddLegacyRecord("Action", new(0, 0));
        doc.SetEventType(added, LegacyEventSchemas.All.Single(item => item.Name == "ACTION_CAMERAPATH"));
        var record = doc.Records().Single(item => item.File == added.File && item.Offset == added.Offset);
        string Name(int number) => (native ? "Param" : "param") + number;
        void Set(int number, string value) => doc.SetField(record, record.Fields.Single(item => item.Name == Name(number)), value);
        Set(1, native ? "255" : "-1"); Set(2, "13"); Set(3, "9");
        var workspace = new EditorWorkspace { Document = doc };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
        var records = Control<ListBox>(workspace, "RecordList"); records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == record.Offset);
        void WorkspaceLayout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); }
        WorkspaceLayout(); ScenarioParameterWindow? parent = null;
        workspace.ScenarioParameterPresenter = window => parent = window;
        var command = Control<Button>(workspace, "ScenarioParameterButton"); Expect(command.IsEnabled, "Common camera parameter command disabled.");
        command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(parent is not null, "Common command did not create camera parent.");
        T Field<T>(string name) => (T)parent!.FindName(name);
        void Layout(Window window, Size size) { var content = (FrameworkElement)window.Content; content.Measure(size); content.Arrange(new Rect(new Point(), size)); content.UpdateLayout(); }
        void Choose(ComboBox combo, string value) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "Value") == value);
        Dictionary<NativeFile, byte[]> Snapshot() => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected) => Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Camera child/parent changed unexpected or premature bytes.");
        void Render(Window window, string state, Size size)
        {
            if (Environment.GetEnvironmentVariable("DK2_CAMERA_PARENT_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return;
            Directory.CreateDirectory(artifacts); Layout(window, size);
            var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render((Visual)window.Content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = new FileStream(Path.Combine(artifacts, $"camera-parent-{state}-{language}-{native}-{size.Width}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        Layout(parent!, new(700, 580)); var initial = Snapshot(); var history = doc.GetHistory().Undo.Count;
        Expect(Field<TextBox>("ScenarioParam1").Text == (native ? "255" : "4294967295"), "Imported camera DWORD shown as signed or repaired.");
        Expect(parent!.ApplyDraft() && doc.GetHistory().Undo.Count == history, "Camera parent no-op made history."); Exact(initial);
        foreach (var width in new[] { 700, 540 }) Render(parent, "imported", new(width, width == 700 ? 580 : 420));
        CameraPathWindow? child = null; parent.CameraLibraryPresenter = window => child = window;
        Field<Button>("ScenarioCameraLibrary").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(child is not null, "Camera library button did not open child.");
        Expect(ReferenceEquals(child, parent.CreateCameraLibraryWindow()) && child!.Selection is null, "Parent reused wrong child or normalized unknown profile.");
        Expect(((TextBlock)child!.FindName("HintText")).Text == LocalizationService.Get("CameraPath.ParentHint")
            && !LocalizationService.Get("CameraPath.ParentHint").Contains(language == "en" ? "cell/area" : "клетки/области"), "Nested library did not explain private parent Apply/ceiling semantics.");
        if (!native) Expect(((TextBlock)child!.FindName("PathTitle")).Text.Contains("4294967295"), "Child title lost unsigned imported ID.");
        Choose(Field<ComboBox>("ScenarioParam2"), "39");
        foreach (var choice in CameraPaths.Choices)
        {
            var list = (ListBox)child!.FindName("PathList"); list.SelectedItem = list.Items.Cast<object>().Single(item => Get<CameraPathChoice>(item, "Choice").ID == choice.ID);
            Layout(child, new(720, 550));
            var ceilingText = language == "en" ? (choice.ShowCell ? "Source default: show the ceiling (flag 0)." : "Source default: hide the ceiling (flag 1).")
                : (choice.ShowCell ? "Настройка оригинала: показывать потолок (флаг 0)." : "Настройка оригинала: скрывать потолок (флаг 1).");
            Expect(((TextBlock)child.FindName("ModeText")).Text == ceilingText && LocalizationService.Get("EventParam.ACTION_CAMERAPATH.Param3") == (language == "en" ? "Ceiling" : "Потолок"), "Visible camera ceiling captions differ from source meaning.");
            Expect(child.ApplySelection() && Field<TextBox>("ScenarioParam1").Text == choice.ID.ToString()
                && Get<string>(Field<ComboBox>("ScenarioParam3").SelectedItem, "Value") == (choice.ShowCell ? "0" : "1")
                && Get<string>(Field<ComboBox>("ScenarioParam2").SelectedItem, "Value") == "39", "Library selection lost area or private ceiling/profile inputs.");
            Exact(initial); Expect(doc.GetHistory().Undo.Count == history, "Nested camera Apply wrote document.");
        }
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Layout(parent, new(700, 580));
        Expect(Get<string>(Field<ComboBox>("ScenarioParam2").SelectedItem, "Value") == "39" && parent.HasPendingInputs, "Camera translation lost private area/draft.");
        LocalizationService.SetLanguage(language); Render(child!, "library", new(720, 550)); child!.CancelSelection(); Exact(initial);
        Expect(parent.HasPendingInputs, "Closing selected library lost accepted private parent inputs.");
        foreach (var width in new[] { 700, 540 }) Render(parent, "selected", new(width, width == 700 ? 580 : 420));
        var finalChoice = CameraPaths.Choices.Last(); var expected = Snapshot();
        foreach (var (number, value) in new[] { (1, finalChoice.ID), (2, 39), (3, finalChoice.ShowCell ? 0 : 1) })
            if (native) expected[record.File][record.Offset + 7 + number] = checked((byte)value);
            else BinaryPrimitives.WriteInt32LittleEndian(expected[record.File].AsSpan(record.Offset + 12 + number * 4), value);
        Expect(parent.ApplyDraft(), "Parent camera Apply rejected valid nested selection."); Exact(expected);
        Expect(doc.GetHistory().Undo.Count == history + 1, "Parent area/profile/ceiling did not make one Undo.");
        workspace.Undo(); Exact(initial); workspace.Redo(); Exact(expected); workspace.Undo(); parent.ResetDraft();
        child = parent.CreateCameraLibraryWindow()!; var pathList = (ListBox)child.FindName("PathList");
        pathList.SelectedItem = pathList.Items.Cast<object>().Single(item => Get<CameraPathChoice>(item, "Choice").ID == 50);
        child.CancelSelection(); Expect(Field<TextBox>("ScenarioParam1").Text == (native ? "255" : "4294967295"), "Child Cancel accepted unconfirmed selection."); Exact(initial);
        child = parent.CreateCameraLibraryWindow()!; parent.ResetDraft(); Expect(!child.ApplySelection() && child.Selection is null, "Reload left old camera child active.");
        Field<TextBox>("ScenarioParam1").Text = native ? "256" : "10000"; Expect(!parent.ApplyDraft(), "Camera number exceeded stored/source bound."); Exact(initial); parent.ResetDraft();
        var grid = Control<DataGrid>(workspace, "PropertyGrid") ?? throw new InvalidOperationException("Camera generic grid missing.");
        var row = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field")?.Name == Name(2));
        grid.SelectedItem = row; grid.ScrollIntoView(row); WorkspaceLayout(); grid.CurrentCell = new(row, grid.Columns[1]); Expect(grid.BeginEdit(), "Camera generic grid did not enter edit."); WorkspaceLayout();
        var input = grid.Columns[1].GetCellContent(row) as TextBox; Expect(input is not null, "Camera generic input missing."); input!.Text = "39";
        Field<TextBox>("ScenarioParam1").Text = "50"; Expect(!parent.ApplyDraft() && record.Read(record.Fields.Single(item => item.Name == Name(2))) == "39", "Camera parent overwrote committed generic fields.");
        workspace.Undo(); Exact(initial); parent.ResetDraft(); child = parent.CreateCameraLibraryWindow()!;
        Set(2, "39"); Expect(!child.ApplySelection() && parent.CreateCameraLibraryWindow() is null, "External edit left camera child/parent active."); workspace.Undo(); parent.ResetDraft(); Exact(initial);
        child = parent.CreateCameraLibraryWindow()!; parent.Close(); Expect(!child.ApplySelection() && !parent.ApplyDraft(), "Parent Cancel left child application authority."); Exact(initial);
        ScenarioParameterWindow? reentrant = null; var nested = true;
        reentrant = new(doc, record, () => true, () => { nested = reentrant!.ApplyDraft(); return true; }); Expect(reentrant.ApplyDraft() && !nested, "Camera parent reentrance guard failed."); reentrant.Close();
        ScenarioParameterWindow? changed = null;
        changed = new(doc, record, () => true, () => { ((TextBox)changed!.FindName("ScenarioParam1")).Text = "50"; return true; }); Expect(!changed.ApplyDraft(), "Camera inputs changed during commit were accepted."); changed.Close(); Exact(initial);
        var closing = workspace.CreateScenarioParameterWindow()!; child = closing.CreateCameraLibraryWindow()!; workspace.Document = null;
        Expect(!closing.ApplyDraft() && !child.ApplySelection(), "Replaced document retained parent/child authority."); closing.Close(); Exact(initial);
        var empty = NativeMapDocument.CreateNewMap(new("EmptyCameraUI", "EmptyCameraUI", 20, 20, NativeFormat: native));
        var emptyAction = native ? empty.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0)) : empty.AddLegacyRecord("Action", new(0, 0));
        empty.SetEventType(emptyAction, LegacyEventSchemas.All.Single(item => item.Name == "ACTION_CAMERAPATH"));
        emptyAction = empty.Records().Single(CameraPaths.IsCameraAction); var missing = new ScenarioParameterWindow(empty, emptyAction, () => true, () => true);
        Expect(!((Button)missing.FindName("ScenarioPageApply")).IsEnabled && !missing.ApplyDraft(), "Missing-area camera Apply was enabled."); missing.Close();
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} camera parent: 52 private profile/ceiling applications, unsigned imports, exact bytes, one Undo, area retention, translation, Cancel, generic commit and lifetime guards");
    }
}
