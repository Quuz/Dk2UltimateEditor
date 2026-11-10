using System.IO;
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
    private static void CheckOverview(NativeMapDocument document,string language)
    {
        var workspace = new EditorWorkspace { Document = document };
        // Attach to a real (never shown) Window so application resource changes
        // reach DynamicResource labels as they do in the running editor.
        var host = new Window { Content = workspace };
        var canvas = Control<MapCanvas>(workspace,"Canvas"); var overview = Control<MapOverview>(workspace,"Overview");
        canvas.CellSize = 12; // Explicit small-viewport fixture; source startup zoom is now 64.
        var scroll = Control<ScrollViewer>(workspace,"MapScroll"); var toggle = Control<CheckBox>(workspace,"OverviewToggle");
        var proportions = Control<CheckBox>(workspace,"OverviewProportions"); var size = Control<ComboBox>(workspace,"OverviewSize");
        void Layout()
        {
            workspace.Measure(new Size(1200,850)); workspace.Arrange(new Rect(0,0,1200,850)); workspace.UpdateLayout();
            workspace.Dispatcher.Invoke(() => { },DispatcherPriority.Background); workspace.UpdateLayout();
        }
        byte[] Pixels()
        {
            var pixels = new byte[overview.Bitmap!.PixelWidth * overview.Bitmap.PixelHeight * 4]; overview.Bitmap.CopyPixels(pixels,overview.Bitmap.PixelWidth * 4,0); return pixels;
        }
        void Press(CheckBox check) => check.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        Dictionary<NativeFile,byte[]> Snapshot() => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> snapshot)
        {
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]),"Minimap navigation/settings changed map bytes.");
        }
        Layout();
        Expect(overview.Document == document && overview.IsEnabled && overview.Bitmap is { IsFrozen: true } && overview.Width == 144 && overview.Height == 144,"Default minimap/document binding absent.");
        Expect(Pixels().AsSpan().SequenceEqual(MapOverviewPalette.RenderBgra(document)),"Actual WPF minimap pixels differ from source palette.");
        Expect(overview.MapBounds.Width == 144 && overview.MapBounds.Height == 72 && overview.MapBounds.Y == 36,"Rectangular minimap was stretched instead of letterboxed.");
        Expect(overview.Viewport.Width > 0 && overview.Viewport.Width < document.Width && overview.Viewport.Height > 0 && overview.Viewport.Height < document.Height,"Minimap visible-frame is not bound to actual canvas viewport.");
        workspace.SetSelection(new(2,2,4,4)); workspace.Copy(); var clipboard = workspace.BufferedSelection; var selection = canvas.Selection;
        document.Paint([new(3,3)],new(11,3,1)); document.Undo(); Layout();
        var before = Snapshot(); var bitmap = overview.Bitmap; var canUndo = document.CanUndo; var canRedo = document.CanRedo; var notifications = 0;
        document.Changed += (_,_) => notifications++;
        var requests = new List<OverviewNavigationEventArgs>(); overview.NavigationRequested += (_,args) => requests.Add(args);
        Expect(!overview.Navigate(new Point(72,10)) && requests.Count == 0,"Letterbox click incorrectly moved map.");
        Expect(!overview.Navigate(new Point(double.NaN,72)),"Nonfinite minimap coordinate accepted.");
        var bound = overview.MapBounds; var visible = overview.Viewport;
        Expect(overview.Navigate(new Point(bound.Right - 1,bound.Bottom - 1)),"Actual overview click operation refused."); Layout();
        var request = requests[^1];
        Expect(Math.Abs(scroll.HorizontalOffset / canvas.CellSize - request.X) < 0.01 && Math.Abs(scroll.VerticalOffset / canvas.CellSize - request.Y) < 0.01,"Minimap click did not scroll the actual map.");
        Expect(Math.Abs(overview.Viewport.X - request.X) < 0.01 && Math.Abs(overview.Viewport.Y - request.Y) < 0.01,"Viewport frame did not track minimap scroll.");
        Expect(overview.Navigate(new Point(-100,-100),initial: false),"Captured drag outside overview was refused instead of clamped."); Layout();
        Expect(scroll.HorizontalOffset == 0 && scroll.VerticalOffset == 0,"Outside drag did not clamp to top-left.");
        Expect(overview.Navigate(new Point(1000,1000),initial: false),"Captured drag to far edge refused."); Layout();
        Expect(Math.Abs(scroll.HorizontalOffset - scroll.ScrollableWidth) < 0.01 && Math.Abs(scroll.VerticalOffset - scroll.ScrollableHeight) < 0.01,"Outside drag did not clamp to bottom-right.");
        scroll.ScrollToHorizontalOffset(180); scroll.ScrollToVerticalOffset(96); Layout();
        Expect(Math.Abs(overview.Viewport.X - 180 / canvas.CellSize) < 0.01 && Math.Abs(overview.Viewport.Y - 96 / canvas.CellSize) < 0.01,"Ordinary scrolling left stale minimap frame.");
        var viewportWidth = overview.Viewport.Width; canvas.CellSize = 24; Layout();
        Expect(overview.Viewport.Width < viewportWidth && ReferenceEquals(bitmap,overview.Bitmap),"Zoom failed to update frame or rebuilt unchanged terrain bitmap.");
        foreach (var index in new[] { 0,2,3,1 })
        {
            size.SelectedIndex = index; Layout(); var expected = new[] { 96,144,216,288 }[index];
            Expect(overview.ActualWidth == expected && overview.ActualHeight == expected && overview.MapBounds.Height == expected / 2,"Actual minimap size control failed.");
        }
        proportions.IsChecked = false; Press(proportions); Layout();
        Expect(!overview.KeepProportions && overview.MapBounds == new Rect(0,0,144,144),"Actual stretch control failed.");
        toggle.IsChecked = false; Press(toggle); Layout(); Expect(Control<StackPanel>(workspace,"OverviewPanel").Visibility == Visibility.Collapsed,"Minimap hide control failed.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(toggle.Content as string == LocalizationService.Get("Editor.Overview.Title") && !overview.KeepProportions && toggle.IsChecked == false,$"Translation discarded minimap settings or left stale label: content={toggle.Content}; expected={LocalizationService.Get("Editor.Overview.Title")}; proportions={overview.KeepProportions}; toggle={toggle.IsChecked}.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        toggle.IsChecked = true; Press(toggle); proportions.IsChecked = true; Press(proportions); Layout();
        Exact(before); Expect(canUndo == document.CanUndo && canRedo == document.CanRedo && notifications == 0 && canvas.Selection == selection && ReferenceEquals(clipboard,workspace.BufferedSelection),"Minimap navigation/settings mutated history/selection/copy buffer.");
        document.Redo(); Layout(); Expect(!ReferenceEquals(bitmap,overview.Bitmap) && Pixels().AsSpan().SequenceEqual(MapOverviewPalette.RenderBgra(document)),"Terrain redo did not rebuild minimap.");
        document.Undo(); Layout(); Exact(before); var originalPixels = Pixels();
        document.Resize(132,70,1,1,new(4,2,1)); Layout(); Expect(overview.Bitmap!.PixelWidth == 132 && overview.Bitmap.PixelHeight == 70,"Resize retained stale minimap dimensions.");
        document.Undo(); Layout(); Expect(Pixels().AsSpan().SequenceEqual(originalPixels),"Resize undo did not restore minimap colours.");
        var savedViewport = overview.Viewport; overview.Viewport = new Rect(0,0,document.Width,document.Height);
        Expect(!overview.Navigate(new Point(72,72)),"Fully visible map produced navigation."); overview.Viewport = savedViewport;
        if (Environment.GetEnvironmentVariable("DK2_OVERVIEW_UI_ARTIFACTS") is { Length: > 0 } artifactDirectory)
        {
            Directory.CreateDirectory(artifactDirectory); var rendered = new RenderTargetBitmap(1200,850,96,96,PixelFormats.Pbgra32); rendered.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(rendered));
            using var output = File.Create(Path.Combine(artifactDirectory,$"overview-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
        }
        var replacement = NativeMapDocument.Create(3,5,"OverviewTall"); workspace.Document = replacement; Layout();
        Expect(overview.MapBounds.Width == 86 && overview.MapBounds.Height == 144 && overview.MapBounds.X == 29,"Portrait minimap bounds incorrect.");
        var replacementBitmap = overview.Bitmap; document.Paint([new(3,3)],new(4,2,1)); Layout();
        Expect(ReferenceEquals(replacementBitmap,overview.Bitmap),"Old document retained minimap subscription.");
        workspace.Document = null; Layout();
        Expect(overview.Bitmap is null && overview.Document is null && !overview.IsEnabled && overview.Viewport.IsEmpty && !overview.Navigate(new Point(72,72)),"Closed document retained minimap content/authority.");
        host.Content = null; host.Close();
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} minimap palette, viewport, click/drag scroll, sizes/proportions, live language and read-only history");
    }
}
