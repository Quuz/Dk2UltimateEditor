using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Original fullscreen overlays over the cached terrain raster.
/// Pointer operations only request navigation/dismissal; they never edit.</summary>
public sealed class FullMapView : MapOverview
{
    private FullMapOverview? snapshot;
    private Point? hover;
    private long tick;
    public event EventHandler<OverviewNavigationEventArgs>? PointChosen;
    public event EventHandler? DismissRequested;
    public FullMapOverview? Snapshot => snapshot;
    public FullMapAnimation Animation => FullMapAnimation.At(tick);
    public int HoverIconID { get; private set; }
    public Rect HoverIconBounds { get; private set; } = Rect.Empty;
    public FullMapView() { Focusable = true; }
    protected override void OnDocumentRefreshed()
    {
        snapshot = Document is null ? null : FullMapOverview.Capture(Document);
        hover = null; HoverIconID = 0; HoverIconBounds = Rect.Empty; InvalidateVisual();
    }
    public void AdvanceAnimation() { if (Document is null) return; tick = tick == long.MaxValue ? 0 : tick + 1; InvalidateVisual(); }
    public void HoverAt(Point? point)
    {
        hover = point; UpdateHover(); InvalidateVisual();
    }
    private void UpdateHover()
    {
        HoverIconID = 0; HoverIconBounds = Rect.Empty;
        if (hover is not { } point || Document is null || !Finite(point) || MapBounds is not { IsEmpty: false } bounds || !bounds.Contains(point)) return;
        var cell = ToCell(point,bounds); HoverIconID = FullMapOverview.HoverIcon(Document.GetTile(cell.X,cell.Y));
        if (HoverIconID != 0) HoverIconBounds = new(Math.Max(0,Math.Min(point.X + 16,ActualWidth - 32)),Math.Max(0,Math.Min(point.Y + 16,ActualHeight - 32)),32,32);
    }
    public bool ChoosePoint(Point point)
    {
        if (Document is null || !Finite(point) || MapBounds is not { IsEmpty: false } bounds) return false;
        // Original fullscreen clicks in letterboxing clamp to the nearest cell.
        var cell = ToCell(point,bounds); PointChosen?.Invoke(this,new(cell.X,cell.Y)); return true;
    }
    public void Dismiss() => DismissRequested?.Invoke(this,EventArgs.Empty);
    private static bool Finite(Point point) => double.IsFinite(point.X) && double.IsFinite(point.Y);
    private TilePoint ToCell(Point point,Rect bounds) => new(
        Math.Clamp((int)Math.Clamp(Math.Floor((point.X - bounds.X) / bounds.Width * Document!.Width),0,Document.Width - 1),0,Document.Width - 1),
        Math.Clamp((int)Math.Clamp(Math.Floor((point.Y - bounds.Y) / bounds.Height * Document.Height),0,Document.Height - 1),0,Document.Height - 1));
    private Rect CellBounds(TilePoint cell,Rect map)
    {
        var left = Math.Floor(map.X + cell.X * map.Width / Document!.Width); var top = Math.Floor(map.Y + cell.Y * map.Height / Document.Height);
        return new(left,top,Math.Max(1,Math.Floor(map.X + (cell.X + 1) * map.Width / Document.Width) - left),
            Math.Max(1,Math.Floor(map.Y + (cell.Y + 1) * map.Height / Document.Height) - top));
    }
    private static Brush Brush(uint colour)
    { var brush = new SolidColorBrush(Color.FromRgb((byte)colour,(byte)(colour >> 8),(byte)(colour >> 16))); brush.Freeze(); return brush; }
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        if (snapshot is null || Document is null || MapBounds is not { IsEmpty: false } bounds) return;
        var phase = Animation; var neutralBrush = Brush(phase.RoomColour);
        context.PushClip(new RectangleGeometry(bounds));
        foreach (var cell in snapshot.NeutralCells) context.DrawRectangle(neutralBrush,null,CellBounds(cell,bounds));
        foreach (var marker in snapshot.Markers)
        {
            var cell = CellBounds(marker.Position,bounds); var brush = Brush(phase.Colour(marker));
            if (marker.Kind == FullMapMarkerKind.Creature)
            {
                // Source draws a centred fixed 3 x 3 marker, not a tile-sized disc.
                context.DrawRectangle(brush,null,new Rect(cell.X + Math.Truncate((cell.Width - 3) / 2),cell.Y + Math.Truncate((cell.Height - 3) / 2),3,3)); continue;
            }
            var insetX = cell.Width > 4 ? 2 : 0; var insetY = cell.Height > 4 ? 2 : 0;
            var box = new Rect(cell.X + insetX,cell.Y + insetY,cell.Width - insetX * 2,cell.Height - insetY * 2);
            if (marker.Kind == FullMapMarkerKind.Door) context.DrawRectangle(brush,null,box);
            else context.DrawEllipse(null,new Pen(brush,2),new Point(box.X + box.Width / 2,box.Y + box.Height / 2),box.Width / 2,box.Height / 2);
        }
        context.Pop(); UpdateHover();
        if (HoverIconID != 0) context.DrawImage(OriginalOverviewIcons.Get(HoverIconID),HoverIconBounds);
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { if (ChoosePoint(e.GetPosition(this))) e.Handled = true; }
    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e) { Dismiss(); e.Handled = true; }
    protected override void OnMouseMove(MouseEventArgs e) => HoverAt(e.GetPosition(this));
    protected override void OnMouseLeave(MouseEventArgs e) { HoverAt(null); base.OnMouseLeave(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.Key == Key.Escape) { Dismiss(); e.Handled = true; } else base.OnKeyDown(e); }
}

public static class OriginalOverviewIcons
{
    private static readonly Dictionary<int,BitmapSource> icons = [];
    public static BitmapSource Get(int id)
    {
        if (icons.TryGetValue(id,out var image)) return image;
        using var stream = typeof(OriginalOverviewIcons).Assembly.GetManifestResourceStream("Legacy.OverviewIcon." + id)
            ?? throw new InvalidOperationException("Missing original room overview icon: " + id);
        var original = BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
        // The source LoadImage explicitly requests 32 x 32 even though most
        // repository bitmaps are 64 x 64. Keep that display/cache contract.
        image = original.PixelWidth == 32 && original.PixelHeight == 32 ? original
            : new TransformedBitmap(original,new ScaleTransform(32.0 / original.PixelWidth,32.0 / original.PixelHeight));
        image.Freeze(); icons.Add(id,image); return image;
    }
}
