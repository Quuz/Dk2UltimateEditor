using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckCameraPaths(NativeMapDocument document, string language, string directory)
    {
        var action = document.IsLegacyProject ? document.AddLegacyRecord("Action", new(2,2))
            : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "Action"), new(2,2));
        document.SetEventType(action, LegacyEventSchemas.All.Single(type => type.Name == "ACTION_CAMERAPATH"));
        action = document.Records().Single(item => item.File == action.File && item.Offset == action.Offset);
        void Set(int parameter, string value) => document.SetField(action, action.Fields.Single(field => field.Name == CameraPaths.FieldName(action, parameter)), value);
        Set(1, document.IsLegacyProject ? "9999" : "255"); Set(2, "7"); Set(3, "9");
        var workspace = new EditorWorkspace { Document = document }; var host = new Window { Content = workspace };
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); var records = Control<ListBox>(workspace, "RecordList");
        categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == action.Category);
        records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == action.Offset);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var command = Control<Button>(workspace, "CameraPathButton");
        Expect(command.IsEnabled && command.Visibility == Visibility.Visible && command.Content as string == LocalizationService.Get("CameraPath.Open"), "Camera command not enabled/translated for selected action.");
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var changes = 0;
        document.Changed += (_, _) => changes++;
        void Exact() { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Chooser selection/cancel/translation changed map bytes."); }
        var window = workspace.CreateCameraPathWindow()!;
        T Field<T>(string name) => (T)window.FindName(name);
        void Layout()
        {
            var content = (FrameworkElement)window.Content; content.Measure(new Size(720,550)); content.Arrange(new Rect(0,0,720,550)); content.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.Background); content.UpdateLayout();
        }
        void Choose(int id)
        {
            var list = Field<ListBox>("PathList"); list.SelectedItem = list.Items.Cast<object>().Single(item => Get<CameraPathChoice>(item, "Choice").ID == id); Layout();
        }
        Layout(); Expect(window.Selection is null && !Field<Button>("ApplyButton").IsEnabled && Field<Image>("Diagram").Source is null
            && Field<TextBlock>("DescriptionText").Text == LocalizationService.Get("CameraPath.Unknown"), "Unknown imported path normalized or given an invented diagram.");
        Expect(!window.ApplySelection(), "Unknown path applied without explicit selection.");
        foreach (var choice in CameraPaths.Choices)
        {
            Choose(choice.ID); var image = (BitmapSource)Field<Image>("Diagram").Source;
            Expect(image.IsFrozen && image.PixelWidth == 256 && image.PixelHeight == 192 && window.Selection == choice
                && Field<TextBlock>("DescriptionText").Text == choice.Description(language)
                && Field<TextBlock>("ModeText").Text == LocalizationService.Get(choice.ShowCell ? "CameraPath.Cell" : "CameraPath.Area"), "Actual camera diagram/description/default differs.");
            var sourceRoot = AppContext.BaseDirectory; while (!Directory.Exists(Path.Combine(sourceRoot, "Source"))) sourceRoot = Path.GetDirectoryName(sourceRoot)!;
            using var source = File.OpenRead(Path.Combine(sourceRoot, "Source/Bitmaps/Chemins de caméra/Chemin n°" + choice.ID.ToString("D3") + ".bmp"));
            var original = BitmapFrame.Create(source, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            byte[] Pixels(BitmapSource bitmap)
            {
                var bgra = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0); var bytes = new byte[256 * 192 * 4]; bgra.CopyPixels(bytes, 256 * 4, 0); return bytes;
            }
            Expect(Pixels(image).AsSpan().SequenceEqual(Pixels(original)), "Embedded diagram differs from preserved original pixels.");
        }
        Choose(210); Field<TextBox>("SearchBox").Text = "no matches anywhere";
        Expect(Field<ListBox>("PathList").Items.Count == 0 && window.Selection?.ID == 210, "Search erased chosen path/draft.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Layout();
        Expect(Field<TextBox>("SearchBox").Text == "no matches anywhere" && window.Title == LocalizationService.Get("CameraPath.Title")
            && Field<TextBlock>("DescriptionText").Text == CameraPaths.Choices.Single(choice => choice.ID == 210).Description(LocalizationService.CurrentLanguage), "Live chooser translation lost search/selection or description.");
        LocalizationService.SetLanguage(language); Field<TextBox>("SearchBox").Text = ""; Layout(); Exact(); Expect(changes == 0, "Read-only chooser created history/events.");
        if (Environment.GetEnvironmentVariable("DK2_CAMERA_PATH_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts); var bitmap = new RenderTargetBitmap(720,550,96,96,PixelFormats.Pbgra32); bitmap.Render((Visual)window.Content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = new FileStream(Path.Combine(artifacts, $"camera-{language}-{(document.IsLegacyProject ? "project" : "native")}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        window.CancelSelection(); Exact(); Expect(!window.Applied && window.Selection is null && !window.ApplySelection() && Field<Image>("Diagram").Source is null, "Canceled/closed chooser retained apply authority or image.");
        window = workspace.CreateCameraPathWindow()!; Choose(50); Expect(window.ApplySelection() && changes == 1, "Actual chooser did not atomically apply.");
        window.Close(); document.Undo(); Exact(); document.Redo();
        Expect(document.CreateCameraPathDraft(action).PathID == 50 && action.Read(action.Fields.Single(field => field.Name == CameraPaths.FieldName(action,3))) == "0", "Cell mode inversion/redo wrong.");
        document.Undo(); Exact();
        // A pending generic area edit is committed first, then makes the old
        // chooser stale; it must never overwrite those new property values.
        window = workspace.CreateCameraPathWindow()!; Choose(150);
        var grid = Control<DataGrid>(workspace, "PropertyGrid"); workspace.Measure(new Size(1300,900)); workspace.Arrange(new Rect(0,0,1300,900)); workspace.UpdateLayout();
        grid.SelectedItem = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == CameraPaths.FieldName(action,2));
        grid.CurrentCell = new DataGridCellInfo(grid.SelectedItem, grid.Columns[1]); grid.BeginEdit(); grid.UpdateLayout();
        var editing = grid.Columns[1].GetCellContent(grid.SelectedItem) as TextBox;
        Expect(editing is not null, "Camera pending property edit did not enter actual DataGrid."); editing!.Text = "8";
        Expect(!window.ApplySelection() && document.CreateCameraPathDraft(action).PathID == (document.IsLegacyProject ? 9999 : 255)
            && action.Read(action.Fields.Single(field => field.Name == CameraPaths.FieldName(action,2))) == "8", "Camera chooser overwrote pending property edit.");
        Expect(Field<TextBlock>("StatusText").Text == LocalizationService.Get("CameraPath.Failed"), "Stale camera error not localized inline.");
        window.Close(); document.Undo(); Exact();
        window = workspace.CreateCameraPathWindow()!; Choose(150); workspace.Document = null;
        Expect(!window.ApplySelection() && changes > 0, "Chooser applied to a closed/replaced source."); window.Close();
        Expect(workspace.CreateCameraPathWindow() is null && !command.IsEnabled, "Closed workspace retained camera command.");
        workspace.Document = document; categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item,"ID") == action.Category);
        records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item,"Record").Offset == action.Offset);
        window = workspace.CreateCameraPathWindow()!;
        typeof(EditorWorkspace).GetField("cameraPathWindow", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(workspace, window);
        workspace.Document = null; Expect(window.Selection is null && !window.ApplySelection(), "Document close did not close active chooser.");
        host.Close(); Exact();
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} camera paths: all 52 original pixels/defaults, search, live translation, Cancel, atomic apply/undo, pending/stale and closed-source guards");
    }
}
