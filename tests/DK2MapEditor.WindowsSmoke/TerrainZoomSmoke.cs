using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckTerrainPixels()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Source"));
        var rc = File.ReadAllText(Path.Combine(root, "Resources/Editor.rc"), Encoding.GetEncoding(1252));
        var paths = Regex.Matches(rc, "(?m)^\\s*(\\d+)\\s+BITMAP\\s+\"([^\"]+)\"").ToDictionary(match => int.Parse(match.Groups[1].Value), match => Path.Combine(root, match.Groups[2].Value.Replace("\\\\", "\\")));
        var baseline = TerrainGetGuiResources(Process.GetCurrentProcess().Handle, 0);
        var reference = new Dictionary<(int, int, int), byte[]>();
        byte[] Native(int resource, int width, int height)
        {
            if (reference.TryGetValue((resource, width, height), out var data)) return data;
            var bitmap = TerrainLoadImage(IntPtr.Zero, paths[resource], 0, width, height, 0x10); Expect(bitmap != IntPtr.Zero, "Source LoadImage failed: " + resource);
            var dc = TerrainCreateCompatibleDC(IntPtr.Zero);
            try
            {
                var info = new TerrainBitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, Bits = 32 };
                data = new byte[width * height * 4]; Expect(TerrainGetDIBits(dc, bitmap, 0, (uint)height, data, ref info, 0) == height, "Native bitmap pixels unavailable.");
                for (var i = 3; i < data.Length; i += 4) data[i] = 255; reference.Add((resource, width, height), data); return data;
            }
            finally { TerrainDeleteObject(bitmap); if (dc != IntPtr.Zero) TerrainDeleteDC(dc); }
        }
        var tiles = new List<MapTile>();
        foreach (var group in new[] { "World", "Rooms", "Gates", "Walls" }) foreach (var entry in LegacyCompilerRules.Elements(group))
            foreach (var owner in group == "Rooms" ? Enumerable.Range(1, 7) : [(int)(entry.ID >> 16 & 255)])
                tiles.Add(new((byte)(entry.ID >> 24), (byte)owner, (byte)(entry.ID >> 8)));
        var pairs = MapZoom.Presets.Select(value => (value, value)).Concat([(17, 63), (48, 96), (512, 2)]).ToArray();
        var checks = 0;
        foreach (var (width, height) in pairs) foreach (var tile in tiles)
        {
            var source = MapTerrainImages.For(tile); var expected = Native(source.Resource, width, height).ToArray();
            if (source.OwnerFrame is { } frame) { var overlay = Native(frame, width, height); for (var i = 0; i < expected.Length; i++) expected[i] |= overlay[i]; }
            var actual = OriginalTerrainImages.For(tile, width, height); var pixels = new byte[expected.Length]; actual.CopyPixels(pixels, width * 4, 0);
            Expect(actual.IsFrozen && pixels.AsSpan().SequenceEqual(expected), $"Source LoadImage/OR pixels differ: {tile}, resource {source.Resource}, {width}x{height}, first byte {Enumerable.Range(0, pixels.Length).FirstOrDefault(i => pixels[i] != expected[i], -1)}."); checks++;
        }
        Expect(TerrainGetGuiResources(Process.GetCurrentProcess().Handle, 0) <= baseline + 6, "Terrain scaling leaked GDI objects.");
        Expect(ReferenceEquals(OriginalTerrainImages.For(new(11, 3, 1), 64, 64), OriginalTerrainImages.For(new(11, 3, 1), 64, 64)), "Frozen terrain cache not reused.");
        Console.WriteLine($"PASS WPF original terrain LoadImage pixels, room OR composition, all zooms and GDI lifetime comparisons={checks}");
    }
    private static void CheckTerrainZoom(bool native, string language)
    {
        var doc = NativeMapDocument.CreateNewMap(new("TerrainZoom", "TerrainZoom", 40, 30, NativeFormat: native));
        var entries = new[] { "World", "Rooms", "Gates", "Walls" }.SelectMany(LegacyCompilerRules.Elements).ToArray();
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i]; doc.Paint([new(1 + i % 18, 1 + i / 18)], new((byte)(entry.ID >> 24), (byte)(((entry.ID >> 16) & 255) is 0 ? 3 : (entry.ID >> 16) & 255), (byte)(entry.ID >> 8)));
        }
        doc.Paint([new(8, 7)], new(8, 3, 1));
        doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Object) with { Owner = 3 }, new(Position: new(8, 7), Flags: (ObjectBatchFlags)127)), [0]);
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace };
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var zoom = Control<ComboBox>(workspace, "ZoomPicker"); var overview = Control<MapOverview>(workspace, "Overview"); var scroll = Control<ScrollViewer>(workspace, "MapScroll");
        void Layout() { workspace.Measure(new Size(1400, 1000)); workspace.Arrange(new Rect(0, 0, 1400, 1000)); workspace.UpdateLayout(); workspace.Dispatcher.Invoke(() => { }, DispatcherPriority.Background); workspace.UpdateLayout(); }
        Layout(); workspace.SetSelection(new(2, 2, 4, 3)); workspace.Copy(); var clipboard = workspace.BufferedSelection; var selection = canvas.Selection;
        Expect(canvas.ObjectDisplay is { Marks.Count: 1 } && !canvas.ObjectDisplay.Marks[0].Solid, "Fixture lacks its valid placed object.");
        var original = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        void Exact() => Expect(original.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history && canvas.Selection == selection && ReferenceEquals(clipboard, workspace.BufferedSelection), "View edits changed map/history/selection/copy buffers.");
        Expect(canvas.CellWidth == 64 && canvas.CellHeight == 64 && zoom.Items.Count == 12, "Source initial/custom/preset zoom missing.");
        for (var index = 0; index < 11; index++)
        {
            zoom.SelectedValue = index; Layout(); var size = MapZoom.Presets[index];
            Expect(canvas.CellWidth == size && canvas.CellHeight == size && canvas.Width == doc.Width * size && canvas.Height == doc.Height * size, "Actual preset did not resize map."); Exact();
        }
        var dialog = workspace.CreateZoomWindow();
        ((TextBox)dialog.FindName("ZoomUserWidth")).Text = "0"; ((TextBox)dialog.FindName("ZoomUserHeight")).Text = "63";
        var prior = workspace.ZoomOptions; Expect(!dialog.ApplyDimensions() && workspace.ZoomOptions == prior, "Invalid draft applied partial zoom.");
        ((TextBox)dialog.FindName("ZoomUserWidth")).Text = "17"; Expect(dialog.ApplyDimensions(), "Valid custom dimensions unavailable."); Layout();
        Expect(canvas.CellWidth == 17 && canvas.CellHeight == 63 && workspace.ZoomOptions.UserWidth == 17 && workspace.ZoomOptions.UserHeight == 63, "Custom dimensions lost independent axes.");
        for (var y = 0; y < doc.Height; y++) for (var x = 0; x < doc.Width; x++)
            Expect(canvas.PointToTile(new(x * 17 + 8, y * 63 + 31)) == new TilePoint(x, y), "Non-square mouse hit selected wrong cell.");
        Expect(canvas.CellBounds(new(2, 3, 4, 5)) == new Rect(34, 189, 68, 315) && canvas.PointToTile(new(-10, 100000)) == new TilePoint(0, doc.Height - 1), "Bounds/captured outside drag use wrong axes.");
        scroll.ScrollToHorizontalOffset(80); scroll.ScrollToVerticalOffset(250); Layout();
        Expect(Math.Abs(overview.Viewport.Y - scroll.VerticalOffset / 63) < 0.01 && Math.Abs(overview.Viewport.X - scroll.HorizontalOffset / 17) < 0.01, "Overview uses square zoom on non-square map.");
        var savedTop = overview.Viewport; workspace.ApplyZoomPreset(10); Layout();
        Expect(Math.Abs(overview.Viewport.Y - savedTop.Y) < 0.01, "Zoom discarded logical scroll position."); workspace.ApplyZoomPreset(11); Layout();
        var toggle = Control<CheckBox>(workspace, "TerrainViewToggle"); toggle.IsChecked = false; toggle.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        Expect(!canvas.ShowTerrain && canvas.ShowEntities, "Hiding terrain also hid independent entities.");
        // RenderTargetBitmap includes the scroll-content visual offset. The
        // coordinate assertions above exercise scrolling; pixel sampling here
        // uses the origin so canvas-local coordinates are also bitmap-local.
        scroll.ScrollToHorizontalOffset(0); scroll.ScrollToVerticalOffset(0); Layout();
        canvas.InvalidateVisual(); Layout(); var render = new RenderTargetBitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight, 96, 96, PixelFormats.Pbgra32); render.Render(canvas);
        var pixel = new byte[4]; render.CopyPixels(new Int32Rect(8 * 17 + 13, 7 * 63 + 10, 1, 1), pixel, 4, 0);
        Expect(pixel[2] == 255 && pixel[1] == 85 && pixel[0] == 85, $"Terrain hiding removed placed object pixels: {string.Join(',', pixel)}, viewport {overview.Viewport}, cell {canvas.CellWidth}x{canvas.CellHeight}.");
        canvas.BeginRegionPick(false); workspace.ApplyZoomPreset(0); Expect(!canvas.IsPickingRegion, "Changing scale retained a stale geometry gesture.");
        workspace.ApplyZoomPreset(11); Layout(); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(workspace.ZoomOptions == new MapZoomOptions(17, 63, 17, 63) && !canvas.ShowTerrain && toggle.Content as string == LocalizationService.Get("MapZoom.ShowTerrain"), "Translation discarded zoom/terrain state.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Exact();
        var settings = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(new AppSettings { MapZoom = workspace.ZoomOptions, ShowTerrain = false }))!;
        var shell = new MainWindow(settings, persistSettings: false); var editor = (EditorWorkspace)shell.FindName("Editor");
        Expect(editor.ZoomOptions == workspace.ZoomOptions && !editor.ShowTerrain, "Actual shell failed persisted view restoration.");
        editor.ApplyUserZoom(32, 80); editor.ConfigureTerrainVisibility(true); shell.Close();
        Expect(settings.MapZoom == new MapZoomOptions(32, 80, 32, 80) && settings.ShowTerrain, "Actual shell failed view capture.");
        var stale = workspace.CreateZoomWindow(); workspace.Document = null; var closedState = workspace.ZoomOptions; Expect(!stale.ApplyDimensions() && workspace.ZoomOptions == closedState, "Closed-map zoom draft applied."); stale.Close();
        workspace.Document = doc; Expect(canvas.Selection is null, "Document replacement retained old selection.");
        workspace.SetSelection(selection!.Value); workspace.ConfigureTerrainVisibility(true); workspace.ApplyUserZoom(48, 32); Layout(); Exact();
        if (Environment.GetEnvironmentVariable("DK2_TERRAIN_ZOOM_ARTIFACTS") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); var bitmap = new RenderTargetBitmap(1400, 1000, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new FileStream(Path.Combine(directory, $"terrain-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        workspace.Document = null; host.Close();
        Console.WriteLine($"PASS WPF original terrain, all zoom presets, independent mouse/viewport axes and persisted view {language} {(native ? "native" : "project")}");
    }
    [StructLayout(LayoutKind.Sequential)] private struct TerrainBitmapInfo { public uint Size; public int Width, Height; public ushort Planes, Bits; public uint Compression, ImageSize; public int XPels, YPels; public uint Used, Important, Colour; }
    [DllImport("user32.dll", EntryPoint = "LoadImageW", CharSet = CharSet.Unicode)] private static extern IntPtr TerrainLoadImage(IntPtr instance, string path, uint type, int width, int height, uint flags);
    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC")] private static extern IntPtr TerrainCreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll", EntryPoint = "GetDIBits")] private static extern int TerrainGetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] data, ref TerrainBitmapInfo info, uint usage);
    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")] private static extern bool TerrainDeleteObject(IntPtr bitmap);
    [DllImport("gdi32.dll", EntryPoint = "DeleteDC")] private static extern bool TerrainDeleteDC(IntPtr dc);
    [DllImport("user32.dll", EntryPoint = "GetGuiResources")] private static extern uint TerrainGetGuiResources(IntPtr process, uint kind);
}
