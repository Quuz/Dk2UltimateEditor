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
    private static void CheckFullMap(NativeMapDocument document,string language)
    {
        foreach (var kind in Enum.GetValues<FullMapMarkerKind>()) for (byte owner = 1; owner <= 7; owner++)
        {
            var point = new TilePoint(2 + owner,2 + (int)kind * 3);
            if (document.IsLegacyProject) document.AddLegacyRecord(kind.ToString(),point,owner);
            else document.AddRecord(RecordTemplate.All.Single(template => template.Structure == (kind switch { FullMapMarkerKind.Trap => "GAMETRAP",FullMapMarkerKind.Door => "GAMEDOOR",_ => "GAMEKEEPER" })),point,owner);
        }
        document.Paint([new(15,15)],new(11,2,1)); document.Paint([new(16,15)],new(8,2,1)); document.Paint([new(17,15)],new(31,2,1));
        var entries = LegacyCompilerRules.Elements("Rooms").Concat(LegacyCompilerRules.Elements("Gates")).Concat(LegacyCompilerRules.Elements("World").Where(item => item.Icon == 309)).ToArray();
        for (var i = 0; i < entries.Length; i++) document.Paint([new(20 + i,20)],new((byte)(entries[i].ID >> 24),3,(byte)(entries[i].ID >> 8)));
        document.Paint([new(10,10)],new(4,2,1)); document.Undo();
        var workspace = new EditorWorkspace { Document = document }; var host = new Window { Content = workspace };
        var scroll = Control<ScrollViewer>(workspace,"MapScroll"); var canvas = Control<MapCanvas>(workspace,"Canvas");
        void Layout()
        { workspace.Measure(new Size(1200,850)); workspace.Arrange(new Rect(0,0,1200,850)); workspace.UpdateLayout(); workspace.Dispatcher.Invoke(() => { },DispatcherPriority.Background); workspace.UpdateLayout(); }
        Layout(); workspace.SetSelection(new(2,2,4,4)); workspace.Copy(); var selection = canvas.Selection; var clipboard = workspace.BufferedSelection;
        var snapshots = document.Files.ToDictionary(file => file,file => file.Data.ToArray()); var undo = document.CanUndo; var redo = document.CanRedo; var dirty = document.HasChanges; var notifications = 0;
        document.Changed += (_,_) => notifications++;
        var button = Control<Button>(workspace,"FullscreenOverviewButton");
        Expect(button.IsEnabled && button.Content as string == LocalizationService.Get("Editor.Overview.Fullscreen"),"Fullscreen actual command missing/disabled/untranslated.");
        var window = workspace.CreateFullscreenOverview(); var view = window.View;
        view.Measure(new Size(768,480)); view.Arrange(new Rect(0,0,768,480)); view.UpdateLayout();
        Expect(window.WindowStyle == WindowStyle.None && window.ResizeMode == ResizeMode.NoResize && !window.ShowInTaskbar && !window.AnimationRunning,"Fullscreen window chrome/timer initialization differs.");
        Expect(view.MapBounds == new Rect(0,48,768,384) && view.Snapshot!.Markers.Count == 21,"Fullscreen map proportions or placed overlays incorrect.");
        var bitmap = view.Bitmap; view.AdvanceAnimation(); view.AdvanceAnimation();
        Expect(view.Animation == new FullMapAnimation(0,1,1,3) && ReferenceEquals(view.Bitmap,bitmap),"Animation rebuilt terrain bitmap or has wrong source phases.");
        var icons = new HashSet<int>();
        for (var i = 0; i < entries.Length; i++)
        {
            view.HoverAt(new Point((20 + i + 0.5) * 6,48 + 20.5 * 6));
            Expect(view.HoverIconID == entries[i].Icon && view.HoverIconBounds.Width == 32 && view.HoverIconBounds.Height == 32,"Fullscreen hover selected wrong original bitmap.");
            var image = OriginalOverviewIcons.Get(view.HoverIconID); Expect(image.IsFrozen && image.PixelWidth == 32 && image.PixelHeight == 32,"Original embedded hover icon size/cache differs.");
            icons.Add(view.HoverIconID);
        }
        Expect(icons.Count == 30,"WPF did not load every original hover icon.");
        var repo = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(repo,"Source"))) repo = Path.GetDirectoryName(repo) ?? throw new Exception("Source not found.");
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var rc = File.ReadAllText(Path.Combine(repo,"Source/Resources/Editor.rc"),System.Text.Encoding.GetEncoding(1252));
        foreach (var id in icons)
        {
            var match = System.Text.RegularExpressions.Regex.Match(rc,"(?m)^" + id + " BITMAP \"([^\"]+)\"");
            Expect(match.Success,"Original icon RC identity not found.");
            var path = match.Groups[1].Value.Replace("\\\\",Path.DirectorySeparatorChar.ToString());
            using var resource = typeof(OriginalOverviewIcons).Assembly.GetManifestResourceStream("Legacy.OverviewIcon." + id)!;
            using var raw = new MemoryStream(); resource.CopyTo(raw);
            Expect(raw.ToArray().AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(repo,"Source",path))),"Embedded bitmap differs from original numeric RC resource.");
        }
        view.HoverAt(new Point(10,10)); Expect(view.HoverIconID == 0 && view.HoverIconBounds.IsEmpty,"Letterbox retained room icon.");
        view.HoverAt(new Point(double.NaN,10)); Expect(view.HoverIconID == 0 && !view.ChoosePoint(new(double.PositiveInfinity,1)),"Nonfinite pointer accepted.");
        // Actual render: original raster, neutral fills, traps/doors/creatures and hover image.
        view.HoverAt(new Point(20.5 * 6,48 + 20.5 * 6));
        view.UpdateLayout();
        var rendered = new RenderTargetBitmap(768,480,96,96,PixelFormats.Pbgra32); rendered.Render(view);
        var pixels = new byte[768 * 480 * 4]; rendered.CopyPixels(pixels,768 * 4,0);
        Expect(pixels.AsSpan(0,4).SequenceEqual(new byte[] { 0,0,0,255 }),"Fullscreen letterboxing is not black.");
        var neutral = view.Animation.RoomColour; var offset = ((48 + 15 * 6 + 3) * 768 + 15 * 6 + 3) * 4;
        Expect(pixels.AsSpan(offset,4).SequenceEqual(new byte[] { (byte)(neutral >> 16),(byte)(neutral >> 8),(byte)neutral,255 }),$"Actual fullscreen neutral fill does not match source colour: expected={neutral:X6}, pixel={Convert.ToHexString(pixels.AsSpan(offset,4))}.");
        var geometry = new FullMapView { Document = document }; geometry.Measure(new Size(1536,768)); geometry.Arrange(new Rect(0,0,1536,768)); geometry.UpdateLayout();
        var shapes = new RenderTargetBitmap(1536,768,96,96,PixelFormats.Pbgra32); shapes.Render(geometry);
        var shapePixels = new byte[1536 * 768 * 4]; shapes.CopyPixels(shapePixels,1536 * 4,0);
        bool PixelIs(int x,int y,uint colour) => shapePixels.AsSpan((y * 1536 + x) * 4,4).SequenceEqual(new byte[] { (byte)(colour >> 16),(byte)(colour >> 8),(byte)colour,255 });
        var ground = MapOverviewPalette.ColorRef(document.GetTile(5,2));
        Expect(PixelIs(5 * 12 + 6,2 * 12 + 6,ground) && !PixelIs(5 * 12 + 2,2 * 12 + 6,ground),"Trap outline was omitted or rendered filled.");
        var doorColour = geometry.Animation.Colour(new(FullMapMarkerKind.Door,new(5,5),3));
        Expect(PixelIs(5 * 12 + 6,5 * 12 + 6,doorColour) && PixelIs(5 * 12 + 1,5 * 12 + 1,ground),"Door filled inset rectangle differs.");
        var creatureColour = geometry.Animation.Colour(new(FullMapMarkerKind.Creature,new(5,8),3));
        Expect(PixelIs(5 * 12 + 5,8 * 12 + 5,creatureColour) && PixelIs(5 * 12 + 3,8 * 12 + 3,ground),"Creature fixed 3-pixel marker differs.");
        geometry.Document = null;
        if (Environment.GetEnvironmentVariable("DK2_FULLMAP_UI_ARTIFACTS") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(rendered));
            using var output = new FileStream(Path.Combine(directory,$"fullmap-{language}-{(document.IsLegacyProject ? "project" : "native")}.png"),FileMode.CreateNew); encoder.Save(output);
        }
        var chosen = new List<OverviewNavigationEventArgs>(); view.PointChosen += (_,point) => chosen.Add(point);
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(button.Content as string == LocalizationService.Get("Editor.Overview.Fullscreen") && window.Title == LocalizationService.Get("Editor.Overview.Fullscreen")
            && view.ToolTip as string == LocalizationService.Get("Editor.Overview.FullscreenHint"),"Live language left stale fullscreen command/window/hint.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout();
        // Start the real Loaded handler without showing a native window; Close
        // exercises the same cleanup used by click/right-click/Esc.
        window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent)); Expect(window.AnimationRunning,"Fullscreen Loaded did not start animation.");
        Expect(view.ChoosePoint(new(600,350)),"Fullscreen click operation refused."); Layout();
        Expect(chosen.Count == 1 && chosen[0].X == 100 && chosen[0].Y == 50,"Fullscreen point conversion incorrect.");
        var wantedX = Math.Clamp((100 + 0.5) * canvas.CellSize - scroll.ViewportWidth / 2,0,scroll.ScrollableWidth);
        var wantedY = Math.Clamp((50 + 0.5) * canvas.CellSize - scroll.ViewportHeight / 2,0,scroll.ScrollableHeight);
        Expect(Math.Abs(scroll.HorizontalOffset - wantedX) < 0.01 && Math.Abs(scroll.VerticalOffset - wantedY) < 0.01,"Fullscreen click did not centre the actual map viewport.");
        Expect(!window.AnimationRunning && view.Document is null && view.Bitmap is null && view.Snapshot is null,"Fullscreen close retained timer/document/cache.");
        view.AdvanceAnimation(); Expect(!view.ChoosePoint(new(1,1)),"Closed fullscreen retained navigation authority.");
        var dismissed = workspace.CreateFullscreenOverview(); var oldX = scroll.HorizontalOffset; var oldY = scroll.VerticalOffset;
        dismissed.View.Dismiss(); Layout(); Expect(scroll.HorizontalOffset == oldX && scroll.VerticalOffset == oldY && dismissed.View.Document is null,"Dismissal navigated or retained source.");
        var margins = workspace.CreateFullscreenOverview(); margins.View.Measure(new Size(768,480)); margins.View.Arrange(new Rect(0,0,768,480));
        var edge = new List<OverviewNavigationEventArgs>(); margins.View.PointChosen += (_,point) => edge.Add(point);
        margins.View.ChoosePoint(new(-100,-100)); Layout(); Expect(edge.Single().X == 0 && edge.Single().Y == 0,"Fullscreen letterbox clicks did not clamp like original.");
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshots[file]),"Fullscreen interaction edited map bytes.");
        Expect(undo == document.CanUndo && redo == document.CanRedo && dirty == document.HasChanges && notifications == 0 && canvas.Selection == selection && ReferenceEquals(clipboard,workspace.BufferedSelection),"Fullscreen changed history/selection/copy buffer.");
        var stale = workspace.CreateFullscreenOverview();
        document.Redo(); Layout(); Expect(stale.View.Document is null && !stale.AnimationRunning,"Map edit retained stale fullscreen navigation."); document.Undo();
        var replacement = workspace.CreateFullscreenOverview();
        typeof(EditorWorkspace).GetField("fullMapWindow",BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(workspace,replacement);
        workspace.Document = null; Layout(); Expect(replacement.View.Document is null && !button.IsEnabled,"Document close retained fullscreen window/command.");
        try { workspace.CreateFullscreenOverview(); throw new Exception("Fullscreen opened without document."); } catch (InvalidOperationException) { }
        host.Content = null; host.Close();
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} fullscreen original overlays/icons, navigation/dismissal, live language and timer/history cleanup");
    }
}
