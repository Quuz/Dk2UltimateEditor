using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class OverviewNavigationEventArgs(double x,double y) : EventArgs
{
    public double X { get; } = x;
    public double Y { get; } = y;
}

/// <summary>Cached one-pixel-per-cell overview. Only viewport navigation is
/// interactive; painting, selections, copy buffers and document history are untouched.</summary>
public class MapOverview : FrameworkElement
{
    private NativeMapDocument? document;
    private bool keepProportions = true;
    private bool navigating;
    private Rect viewport;
    public BitmapSource? Bitmap { get; private set; }
    public event EventHandler<OverviewNavigationEventArgs>? NavigationRequested;
    public event EventHandler? FullscreenRequested;
    public NativeMapDocument? Document
    {
        get => document;
        set
        {
            CancelNavigation();
            if (document is not null) document.Changed -= DocumentChanged;
            document = value;
            if (document is not null) document.Changed += DocumentChanged;
            viewport = Rect.Empty; Rebuild(); OnDocumentRefreshed();
        }
    }
    public bool KeepProportions { get => keepProportions; set { keepProportions = value; InvalidateVisual(); } }
    /// <summary>Visible rectangle in map-cell coordinates, including partial cells.</summary>
    public Rect Viewport { get => viewport; set { viewport = value; InvalidateVisual(); } }
    public Rect MapBounds
    {
        get
        {
            if (document is null || ActualWidth <= 0 || ActualHeight <= 0) return Rect.Empty;
            if (!keepProportions) return new(0,0,ActualWidth,ActualHeight);
            var scale = Math.Min(ActualWidth / document.Width,ActualHeight / document.Height);
            var width = Math.Max(1,Math.Floor(document.Width * scale)); var height = Math.Max(1,Math.Floor(document.Height * scale));
            return new(Math.Floor((ActualWidth - width) / 2),Math.Floor((ActualHeight - height) / 2),width,height);
        }
    }
    public MapOverview()
    {
        Cursor = Cursors.Hand; ClipToBounds = true;
        RenderOptions.SetBitmapScalingMode(this,BitmapScalingMode.NearestNeighbor);
        Unloaded += (_,_) => CancelNavigation();
    }
    private void DocumentChanged(object? sender,EventArgs e) { CancelNavigation(); Rebuild(); OnDocumentRefreshed(); }
    protected virtual void OnDocumentRefreshed() { }
    private void Rebuild()
    {
        Bitmap = document is null ? null : BitmapSource.Create(document.Width,document.Height,96,96,PixelFormats.Bgra32,null,
            MapOverviewPalette.RenderBgra(document),document.Width * 4);
        Bitmap?.Freeze(); InvalidateVisual();
    }
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context); context.DrawRectangle(Brushes.Black,null,new Rect(RenderSize));
        if (Bitmap is null || document is null || MapBounds is not { IsEmpty: false } bounds) return;
        context.DrawImage(Bitmap,bounds);
        if (viewport.IsEmpty) return;
        var visible = Rect.Intersect(viewport,new Rect(0,0,document.Width,document.Height));
        if (visible.IsEmpty) return;
        var frame = new Rect(bounds.X + visible.X / document.Width * bounds.Width,bounds.Y + visible.Y / document.Height * bounds.Height,
            visible.Width / document.Width * bounds.Width,visible.Height / document.Height * bounds.Height);
        // A black outline keeps the white viewport visible on both bright and dark rooms.
        context.DrawRectangle(null,new Pen(Brushes.Black,3),frame);
        context.DrawRectangle(null,new Pen(Brushes.White,1),frame);
    }
    /// <summary>Shared click/drag operation. Initial clicks in letterboxing are
    /// ignored; captured drags outside the map clamp to its edges.</summary>
    public bool Navigate(Point point,bool initial = true)
    {
        if (document is null || viewport.IsEmpty || MapBounds is not { IsEmpty: false } bounds
            || !double.IsFinite(point.X) || !double.IsFinite(point.Y) || initial && !bounds.Contains(point)) return false;
        var width = Math.Clamp(viewport.Width,0,document.Width); var height = Math.Clamp(viewport.Height,0,document.Height);
        if (width <= 0 || height <= 0 || width == document.Width && height == document.Height) return false;
        var x = Math.Clamp((point.X - bounds.X) / bounds.Width * document.Width - width / 2,0,document.Width - width);
        var y = Math.Clamp((point.Y - bounds.Y) / bounds.Height * document.Height - height / 2,0,document.Height - height);
        NavigationRequested?.Invoke(this,new(x,y)); return true;
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!Navigate(e.GetPosition(this))) return;
        navigating = CaptureMouse(); e.Handled = true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); if (navigating) { Navigate(e.GetPosition(this),initial: false); e.Handled = true; }
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e); if (navigating) { CancelNavigation(); e.Handled = true; }
    }
    public void RequestFullscreen() { if (document is not null) FullscreenRequested?.Invoke(this,EventArgs.Empty); }
    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e) { if (document is not null) { RequestFullscreen(); e.Handled = true; } }
    protected override void OnLostMouseCapture(MouseEventArgs e) { navigating = false; base.OnLostMouseCapture(e); }
    private void CancelNavigation() { navigating = false; if (IsMouseCaptured) ReleaseMouseCapture(); }
}
