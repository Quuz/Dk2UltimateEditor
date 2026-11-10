using System.Diagnostics;
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
    private static void CheckCameraPixels()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Source/Icons/Players"));
        // Establish the retained five-image WIC cache before auditing repeated
        // temporary DrawIconEx/DC/DIB handles, and initialize WPF's renderer.
        for (var resource = 20; resource <= 24; resource++) OriginalCameraImages.For(resource);
        var warmup = new DrawingVisual(); using (var drawing = warmup.RenderOpen()) drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, 16, 16));
        new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32).Render(warmup);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var warmIcon = TerrainLoadImage(IntPtr.Zero, Path.Combine(root, "Keeper1Camera.ico"), 1, 16, 16, 0x10);
        var warmDc = TerrainCreateCompatibleDC(IntPtr.Zero);
        Expect(warmIcon != IntPtr.Zero && TerrainDeleteDC(warmDc) && OverlayDestroyIcon(warmIcon), "Native camera warmup failed.");
        var checks = 0;
        for (var index = 0; index < 5; index++) foreach (var background in new uint[] { 0, 0xFFFFFF, 0x123456 })
        {
            var baseline = TerrainGetGuiResources(Process.GetCurrentProcess().Handle, 0);
            byte[] reference;
            var icon = TerrainLoadImage(IntPtr.Zero, Path.Combine(root, $"Keeper{index + 1}Camera.ico"), 1, 16, 16, 0x10);
            Expect(icon != IntPtr.Zero, "Original camera icon unavailable.");
            var dc = TerrainCreateCompatibleDC(IntPtr.Zero); IntPtr bitmap = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                var info = new TerrainBitmapInfo { Size = 40, Width = 16, Height = -16, Planes = 1, Bits = 32 };
                bitmap = OverlayCreateDIBSection(dc, ref info, 0, out var bits, IntPtr.Zero, 0); Expect(bitmap != IntPtr.Zero, "Camera reference DIB unavailable.");
                reference = new byte[16 * 16 * 4];
                for (var i = 0; i < reference.Length; i += 4) { reference[i] = (byte)(background >> 16); reference[i + 1] = (byte)(background >> 8); reference[i + 2] = (byte)background; reference[i + 3] = 255; }
                Marshal.Copy(reference, 0, bits, reference.Length); old = OverlaySelectObject(dc, bitmap);
                Expect(OverlayDrawIconEx(dc, 0, 0, icon, 16, 16, 0, IntPtr.Zero, 3), "Source DrawIconEx failed."); Marshal.Copy(bits, reference, 0, reference.Length);
            }
            finally
            {
                if (old != IntPtr.Zero) OverlaySelectObject(dc, old);
                if (bitmap != IntPtr.Zero) Expect(TerrainDeleteObject(bitmap), "Reference DIB handle not released.");
                Expect(TerrainDeleteDC(dc) && OverlayDestroyIcon(icon), "Reference DC/icon handles not released.");
            }
            var finalHandles = TerrainGetGuiResources(Process.GetCurrentProcess().Handle, 0);
            Expect(finalHandles == baseline, $"Source camera reference leaked GDI handles: {baseline} -> {finalHandles}.");
            // WPF render targets have dispatcher/render-thread surface caches;
            // audit explicit native handle lifetime before allocating them.
            var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen())
            { drawing.DrawRectangle(new SolidColorBrush(Color.FromRgb((byte)background, (byte)(background >> 8), (byte)(background >> 16))), null, new Rect(0, 0, 16, 16)); drawing.DrawImage(OriginalCameraImages.For(20 + index), new Rect(0, 0, 16, 16)); }
            var rendered = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32); rendered.Render(visual); var pixels = new byte[reference.Length]; rendered.CopyPixels(pixels, 64, 0);
            Expect(Enumerable.Range(0, pixels.Length).Where(i => i % 4 != 3).All(i => pixels[i] == reference[i]), "Original camera RGB/AND transparency differs from DrawIconEx: " + index);
            Expect(OriginalCameraImages.For(20 + index).IsFrozen && ReferenceEquals(OriginalCameraImages.For(20 + index), OriginalCameraImages.For(20 + index)), "Camera images not frozen/cached."); checks++;
        }
        Console.WriteLine("PASS WPF original camera ICO pixels/transparency, three backgrounds and GDI lifetime comparisons=" + checks);
    }
    private static void CheckMapOverlays(bool native, string language)
    {
        var doc = NativeMapDocument.CreateNewMap(new("OverlayUI", "OverlayUI", 40, 24, Players: 5, NativeFormat: native));
        var area = doc.AddMapRegion(MapRegionKind.Area, new(3, 3, 7, 4)); var target = doc.AddMapRegion(MapRegionKind.Area, new(15, 9, 4, 3));
        doc.LinkPatrolAreas(area, target); doc.LinkPatrolAreas(target, target);
        doc.AddMapRegion(MapRegionKind.Effect, new(15, 3, 4, 3));
        var gateEntry = LegacyCompilerRules.Elements("Gates").First(); var tile = new MapTile((byte)(gateEntry.ID >> 24), 3, (byte)(gateEntry.ID >> 8));
        doc.Paint(MapDrawing.Rectangle(new(26, 3, 4, 3), true), tile); doc.AddMapRegion(MapRegionKind.Gate, new(27, 4, 1, 1));
        for (uint id = 3; id <= 7; id++) doc.SetPlayerCamera(id, new(5 + (int)(id - 3) * 5, 16));
        if (!native) doc.SetField(area, area.Fields.Single(field => field.Name == "name"), "Guard room / patrol");
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace };
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var menu = Control<MenuItem>(workspace, "OverlayViewMenu"); var patrol = Control<CheckBox>(workspace, "PatrolToggle");
        var colour = Control<TextBox>(workspace, "PathColourInput"); workspace.ConfigureTerrainVisibility(false); workspace.ApplyUserZoom(32, 24);
        void Layout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); canvas.HighlightRegion(null); canvas.InvalidateVisual(); canvas.UpdateLayout(); }
        Layout(); workspace.SetSelection(new(1, 1, 2, 2)); workspace.Copy(); var selection = canvas.Selection; var clipboard = workspace.BufferedSelection;
        var original = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        void Exact() => Expect(original.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history && canvas.Selection == selection && ReferenceEquals(clipboard, workspace.BufferedSelection), "Overlay view changed map/history/selection/copy.");
        Expect(menu.Items.Count == 6 && canvas.OverlayDisplay is { Cameras.Count: 5, Regions.Count: 4, Paths.Count: 2 } && workspace.OverlayOptions == new MapOverlayOptions(), "Actual source overlay defaults/projection absent.");
        foreach (MenuItem item in menu.Items)
        {
            var flag = (MapOverlayFlags)item.Tag; var before = workspace.OverlayOptions.Flags; item.IsChecked = (before & flag) == 0; item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Expect(workspace.OverlayOptions.Flags == (before ^ flag), "Overlay click changed wrong flag."); Exact();
            item.IsChecked = (before & flag) != 0; item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
        patrol.IsChecked = false; patrol.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        Expect(!canvas.ShowPatrolPaths && !menu.Items.Cast<MenuItem>().Single(item => (MapOverlayFlags)item.Tag == MapOverlayFlags.Paths).IsChecked, "Legacy patrol control out of sync.");
        patrol.IsChecked = true; patrol.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        colour.Text = "#12A0F3"; Expect(workspace.ApplyPathColour() && workspace.OverlayOptions.PathColour == 0xF3A012 && colour.Text == "12A0F3", "RGB UI/source COLORREF conversion differs.");
        colour.Text = "GG"; var prior = workspace.OverlayOptions; Expect(!workspace.ApplyPathColour() && workspace.OverlayOptions == prior, "Invalid colour applied partial preference.");
        workspace.ConfigureOverlays(new(MapOverlayDisplay.DefaultFlags | MapOverlayFlags.ExpandedGates)); Layout();
        Expect(canvas.OverlayDisplay!.Regions.Single(region => region.Kind == MapRegionKind.Gate).ExpandedBounds == new TileRect(26, 3, 4, 3), "Actual gate footprint not expanded.");
        RenderTargetBitmap Render() { Layout(); var bitmap = new RenderTargetBitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(canvas); return bitmap; }
        var shown = Render(); var bytes = new byte[shown.PixelWidth * shown.PixelHeight * 4]; shown.CopyPixels(bytes, shown.PixelWidth * 4, 0);
        Expect(bytes.Any(value => value != 0 && value != 255), "Overlay render is empty black background.");
        workspace.ConfigureOverlays(new(MapOverlayFlags.None)); var hidden = Render(); var hiddenBytes = new byte[bytes.Length]; hidden.CopyPixels(hiddenBytes, hidden.PixelWidth * 4, 0);
        Expect(!bytes.AsSpan().SequenceEqual(hiddenBytes), "Visibility controls did not reach actual rendering.");
        foreach (var flag in new[] { MapOverlayFlags.Areas, MapOverlayFlags.Gates, MapOverlayFlags.Effects, MapOverlayFlags.Paths, MapOverlayFlags.Cameras })
        {
            workspace.ConfigureOverlays(new(flag)); var isolated = Render(); var isolatedBytes = new byte[bytes.Length]; isolated.CopyPixels(isolatedBytes, isolated.PixelWidth * 4, 0);
            Expect(!isolatedBytes.AsSpan().SequenceEqual(hiddenBytes), "Individual source layer did not render: " + flag); Exact();
            if (flag == MapOverlayFlags.Cameras)
            {
                // Independently calculate the first keeper icon position; the
                // previously GDI-verified 16px image must reach actual canvas.
                var iconPixels = new byte[16 * 16 * 4]; isolated.CopyPixels(new Int32Rect(5 * 32 + 8, 16 * 24 + 4, 16, 16), iconPixels, 64, 0);
                var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen())
                { drawing.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 16, 16)); drawing.DrawImage(OriginalCameraImages.For(20), new Rect(0, 0, 16, 16)); }
                var reference = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32); reference.Render(visual); var expected = new byte[iconPixels.Length]; reference.CopyPixels(expected, 64, 0);
                Expect(iconPixels.AsSpan().SequenceEqual(expected), "Camera icon size/position/pixels differ on nonsquare actual canvas.");
            }
        }
        workspace.ConfigureOverlays(new(MapOverlayFlags.Paths)); canvas.ShowEntities = false;
        Expect(canvas.ShowPatrolPaths, "Entity master incorrectly hid independent path layer."); canvas.ShowEntities = true;
        // A native/project camera write and exact Undo must refresh snapshots.
        var beforeSnapshot = canvas.OverlayDisplay; doc.SetPlayerCamera(3, new(7, 14));
        Expect(!ReferenceEquals(beforeSnapshot, canvas.OverlayDisplay) && canvas.OverlayDisplay!.Cameras.Single(camera => camera.PlayerID == 3).Position == new TilePoint(7, 14), "Camera edit did not refresh snapshot."); doc.Undo(); Exact();
        var retained = new MapOverlayOptions(MapOverlayFlags.Cameras | MapOverlayFlags.Paths | (MapOverlayFlags)0x80000000, 0x563412); workspace.ConfigureOverlays(retained); colour.Text = "unfinished";
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(workspace.OverlayOptions == retained && colour.Text == "unfinished" && menu.Items.Cast<MenuItem>().All(item => item.Header is string label && !label.StartsWith("MapOverlays.", StringComparison.Ordinal)), "Translation discarded preferences/draft or leaked keys.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Exact();
        var settings = new AppSettings { MapOverlays = retained }; var shell = new MainWindow(settings, persistSettings: false); var editor = (EditorWorkspace)shell.FindName("Editor");
        Expect(editor.OverlayOptions == retained, "Actual shell failed overlay restore."); editor.ConfigureOverlays(new(MapOverlayFlags.Areas, 0x010203)); shell.Close(); Expect(settings.MapOverlays == editor.OverlayOptions, "Actual shell failed overlay capture.");
        workspace.ConfigureOverlays(new(MapOverlayDisplay.DefaultFlags | MapOverlayFlags.ExpandedGates)); workspace.ConfigureTerrainVisibility(true); Layout();
        if (Environment.GetEnvironmentVariable("DK2_OVERLAY_ARTIFACTS") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); var bitmap = new RenderTargetBitmap(1500, 1000, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new FileStream(Path.Combine(directory, $"overlays-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        workspace.Document = null; Expect(canvas.OverlayDisplay is null, "Closed map retained overlay snapshot."); host.Close();
        Console.WriteLine($"PASS WPF original cameras, region labels, gate expansion, paths, six view flags and persisted colour {language} {(native ? "native" : "project")}");
    }
    [DllImport("gdi32.dll", EntryPoint = "CreateDIBSection")] private static extern IntPtr OverlayCreateDIBSection(IntPtr dc, ref TerrainBitmapInfo info, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll", EntryPoint = "SelectObject")] private static extern IntPtr OverlaySelectObject(IntPtr dc, IntPtr obj);
    [DllImport("user32.dll", EntryPoint = "DrawIconEx")] private static extern bool OverlayDrawIconEx(IntPtr dc, int x, int y, IntPtr icon, int width, int height, uint step, IntPtr brush, uint flags);
    [DllImport("user32.dll", EntryPoint = "DestroyIcon")] private static extern bool OverlayDestroyIcon(IntPtr icon);
}
