using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public enum DrawingTool { Brush, Line, Rectangle, FilledRectangle, Ellipse, FilledEllipse, Fill, Select, Sample, PlaceObject }
public sealed class ObjectPaintingRequestedEventArgs(TilePoint position) : EventArgs { public TilePoint Position { get; } = position; }
public sealed class MapRegionPickedEventArgs(TileRect bounds) : EventArgs { public TileRect Bounds { get; } = bounds; }
public sealed class RightMapSelectionEventArgs(TileRect bounds, bool continuePath, bool newPath) : EventArgs
{ public TileRect Bounds { get; } = bounds; public bool ContinuePath { get; } = continuePath; public bool NewPath { get; } = newPath; }

public sealed partial class MapCanvas : FrameworkElement
{
    private NativeMapDocument? document;
    private TilePoint start;
    private TilePoint previous;
    private readonly HashSet<TilePoint> stroke = [];
    private bool dragging;
    private bool rightSelecting;
    private NativeMapDocument.LivePenStroke? livePen;
    public event EventHandler<RightMapSelectionEventArgs>? RightMapSelectionCompleted;
    private bool regionPoint;
    private bool regionRightClickCancels=true;
    private DrawingTool tool;
    private MapTile paintTile = new(2, 2, 1);
    private double cellWidth = 64, cellHeight = 64;

    public NativeMapDocument? Document
    {
        get => document;
        set
        {
            CancelRegionPick(); CancelDrawing(); HighlightedRegion = null;
            if (document is not null) document.Changed -= DocumentChanged;
            document = value;
            if (document is not null) document.Changed += DocumentChanged;
            Selection = null; CursorTile = new(0, 0); LastPlacementResult = null; SelectionChanged?.Invoke(this, EventArgs.Empty); CursorChanged?.Invoke(this, EventArgs.Empty); PlacementChanged?.Invoke(this, EventArgs.Empty);
            RefreshObjectDisplay(); RefreshOverlayDisplay(); UpdateSize();
        }
    }
    /// <summary>Compatibility square-size API. Two-dimensional code uses CellWidth/CellHeight.</summary>
    public double CellSize
    {
        get => cellWidth;
        set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetCellDimensions(Math.Clamp(value, MapZoom.Minimum, MapZoom.Maximum), Math.Clamp(value, MapZoom.Minimum, MapZoom.Maximum)); }
    }
    public double CellWidth => cellWidth;
    public double CellHeight => cellHeight;
    public event EventHandler? ZoomChanged;
    public void SetCellDimensions(double width, double height)
    {
        if (!MapZoom.Valid(width) || !MapZoom.Valid(height)) throw new ArgumentOutOfRangeException(nameof(width));
        if (width == cellWidth && height == cellHeight) return;
        CancelRegionPick(); CancelDrawing(); cellWidth = width; cellHeight = height;
        ResetPlacement(); UpdateSize(); ZoomChanged?.Invoke(this, EventArgs.Empty);
    }
    public DrawingTool Tool { get => tool; set { if (tool != value) { CancelRegionPick(); CancelDrawing(); } tool = value; ResetPlacement(); } }
    public MapTile PaintTile { get => paintTile; set { if (paintTile != value) CancelDrawing(); paintTile = value; ResetPlacement(); } }
    public EditorTerrainFootprint PaintFootprint => EditorTerrainRules.Footprint(PaintTile);
    public EditorPlacementResult? LastPlacementResult { get; private set; }
    public TileRect? PlacementPreview => document is null || IsPickingRegion || Tool is DrawingTool.Select or DrawingTool.Sample or DrawingTool.PlaceObject || !PaintFootprint.RequiresRectangle ? null
        : dragging ? PaintFootprint.GestureRectangle(start, CursorTile, document.Width, document.Height)
        : PaintFootprint.CursorRectangle(CursorTile, document.Width, document.Height);
    public TileRect? Selection { get; private set; }
    public TilePoint CursorTile { get; private set; }
    public bool ShowEntities { get; set; } = true;
    public bool ShowPatrolPaths
    {
        get => (overlayOptions.Flags & MapOverlayFlags.Paths) != 0;
        set => OverlayOptions = overlayOptions with { Flags = value ? overlayOptions.Flags | MapOverlayFlags.Paths : overlayOptions.Flags & ~MapOverlayFlags.Paths };
    }
    public bool RandomPainting { get; set; }
    public bool GeometryOnly { get; set; }
    public event EventHandler? SelectionChanged;
    public event EventHandler? CursorChanged;
    public event EventHandler? TileSampled;
    public event EventHandler? PlacementChanged;
    public event EventHandler<ObjectPaintingRequestedEventArgs>? ObjectPaintingRequested;
    /// <summary>Source SHOW_CURSOR navigation, independent of painting and copy selection.</summary>
    public bool NavigateCursorTo(TilePoint point)
    {
        var source=document;if(source is null)return false;
        CancelDrawing();if(!ReferenceEquals(source,document))return false;
        if(point.X<0||point.Y<0||point.X>=source.Width||point.Y>=source.Height)throw new ArgumentOutOfRangeException(nameof(point));
        CursorTile=point;CursorChanged?.Invoke(this,EventArgs.Empty);InvalidateVisual();return ReferenceEquals(source,document);
    }
    /// <summary>The same one-click operation as the object tool. No terrain stroke, region pick or selection changes.</summary>
    public void PaintObjectAt(TilePoint position)
    {
        if (GeometryOnly) return;
        if (document is null || IsPickingRegion || Tool != DrawingTool.PlaceObject) throw new InvalidOperationException("Select the object tool on an open map.");
        if (position.X < 0 || position.Y < 0 || position.X >= document.Width || position.Y >= document.Height) throw new ArgumentOutOfRangeException(nameof(position));
        CursorTile = position; CursorChanged?.Invoke(this, EventArgs.Empty);
        ObjectPaintingRequested?.Invoke(this, new(position)); InvalidateVisual();
    }
    public bool IsPickingRegion { get; private set; }
    public TileRect? HighlightedRegion { get; private set; }
    public event EventHandler<MapRegionPickedEventArgs>? RegionPicked;
    public event EventHandler? RegionPickCanceled;

    public void HighlightRegion(TileRect? bounds) { HighlightedRegion = bounds; InvalidateVisual(); }
    public void BeginRegionPick(bool point)=>BeginRegionPick(point,true);
    internal void BeginRegionPick(bool point,bool rightClickCancels)
    {
        if (document is null) throw new InvalidOperationException("Open a map before choosing geometry.");
        CancelRegionPick(); CancelDrawing(); regionPoint = point;regionRightClickCancels=rightClickCancels; IsPickingRegion = true; ResetPlacement();
    }
    public bool CancelRegionPick()
    {
        if (!IsPickingRegion) return false;
        IsPickingRegion = false; CancelDrawing(); ResetPlacement(); RegionPickCanceled?.Invoke(this, EventArgs.Empty); return true;
    }
    /// <summary>The same release operation used by point/rectangle mouse picking.
    /// It never paints terrain or changes the ordinary copy-selection rectangle.</summary>
    public void CompleteRegionPick(TilePoint origin, TilePoint end)
    {
        if (!IsPickingRegion || document is null) throw new InvalidOperationException("No map geometry choice is pending.");
        foreach (var point in new[] { origin, end })
            if (point.X < 0 || point.Y < 0 || point.X >= document.Width || point.Y >= document.Height) throw new ArgumentOutOfRangeException(nameof(origin));
        var bounds = regionPoint ? new TileRect(origin.X, origin.Y, 1, 1) : TileRect.Between(origin, end);
        IsPickingRegion = false; CancelDrawing(); ResetPlacement(); RegionPicked?.Invoke(this, new(bounds)); InvalidateVisual();
    }

    public MapCanvas()
    {
        Focusable = true; Cursor = Cursors.Cross; ClipToBounds = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        Loaded += OnLoaded; Unloaded += OnUnloaded;
    }
    public void SelectAll()
    {
        if (document is null) return;
        SetSelection(new(0, 0, document.Width, document.Height));
    }
    public void SetSelection(TileRect? rectangle)
    {
        if (rectangle is { } rect && (document is null || rect.X < 0 || rect.Y < 0 || rect.Width < 1 || rect.Height < 1
            || (long)rect.X + rect.Width > document.Width || (long)rect.Y + rect.Height > document.Height)) throw new ArgumentOutOfRangeException(nameof(rectangle));
        Selection = rectangle; ResetPlacement(); InvalidateVisual(); SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
    public void ClearSelection() => SetSelection(null);

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        if (document is null) return;
        context.DrawRectangle(Brushes.Black, null, new(0, 0, ActualWidth, ActualHeight));
        var viewer = FindScrollViewer();
        var minX = viewer is null ? 0 : Math.Clamp((int)(viewer.HorizontalOffset / cellWidth), 0, document.Width);
        var minY = viewer is null ? 0 : Math.Clamp((int)(viewer.VerticalOffset / cellHeight), 0, document.Height);
        var maxX = viewer is null ? document.Width : Math.Min(document.Width, (int)Math.Ceiling((viewer.HorizontalOffset + viewer.ViewportWidth) / cellWidth) + 1);
        var maxY = viewer is null ? document.Height : Math.Min(document.Height, (int)Math.Ceiling((viewer.VerticalOffset + viewer.ViewportHeight) / cellHeight) + 1);
        DrawTerrain(context, new(minX, minY, maxX - minX, maxY - minY));
        if (ShowEntities)
        {
            DrawMapObjects(context, new(minX, minY, maxX - minX, maxY - minY));
        }
        DrawMapOverlays(context, new(minX, minY, maxX - minX, maxY - minY));
        if (HighlightedRegion is { } highlighted) DrawSelection(context, highlighted, Colors.Orange);
        if (Selection is { } selection) DrawSelection(context, selection, Colors.Cyan);
        if (rightSelecting) DrawSelection(context, TileRect.Between(start, CursorTile), Colors.White);
        if (PlacementPreview is { } footprint) DrawSelection(context, footprint, Colors.Gold);
        if (dragging && IsPickingRegion) DrawSelection(context, regionPoint ? new(start.X, start.Y, 1, 1) : TileRect.Between(start, CursorTile), Colors.White);
        if (dragging && !IsPickingRegion && Tool is not (DrawingTool.Brush or DrawingTool.Fill or DrawingTool.Sample))
            DrawSelection(context, Tool == DrawingTool.Select ? TileRect.Between(start, CursorTile) : PlacementPreview ?? TileRect.Between(start, CursorTile), Colors.White);
        if (dragging && !IsPickingRegion && Tool == DrawingTool.Brush)
            foreach (var point in stroke) context.DrawRectangle(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), null, new(point.X * cellWidth, point.Y * cellHeight, cellWidth, cellHeight));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!BeginLeftMapPointer(e.GetPosition(this))) return;
        e.Handled = true;
    }
    internal bool BeginLeftMapPointer(Point pointer)
    {
        if (!PointerInsideMap(pointer)) return false;
        if (rightSelecting || dragging) { CancelDrawing(); return true; }
        if (!GeometryOnly && !IsPickingRegion && Tool is not (DrawingTool.Select or DrawingTool.Sample or DrawingTool.PlaceObject)
            && document!.CheckEditorPlacement(PointToTile(pointer), PaintTile, Selection) != EditorPlacementResult.Painted) return false;
        // Source objects have a fixed1x1 maximum and reject a protected
        // initial footprint before LMB counting. Free terrain has maximum0:
        // its click is accepted, and its operation skips protected writes.
        if (!GeometryOnly && !IsPickingRegion && Tool == DrawingTool.PlaceObject
            && document!.IsEditorProtectedCell(PointToTile(pointer).X, PointToTile(pointer).Y)) return false;
        if (!GeometryOnly && !IsPickingRegion) document!.RecordActivity(ActivityCounterEvent.LeftMapClick);
        BeginLeftMapDrawing(pointer);
        return true;
    }
    private void BeginLeftMapDrawing(Point pointer)
    {
        if (document is null) return;
        Focus(); start = previous = PointToTile(pointer); CursorTile = start;
        CursorChanged?.Invoke(this, EventArgs.Empty);
        if (IsPickingRegion) { dragging = true; stroke.Clear(); CaptureMouse(); InvalidateVisual(); return; }
        if (GeometryOnly && Tool != DrawingTool.Select) return;
        if (Tool == DrawingTool.PlaceObject) { PaintObjectAt(start); return; }
        if (Tool == DrawingTool.Sample)
        { SampleAt(start); return; }
        if (Tool != DrawingTool.Select && PaintFootprint.RequiresRectangle)
        {
            var result = document.CheckEditorPlacement(start, PaintTile, Selection);
            if (result != EditorPlacementResult.Painted) { SetPlacementResult(result); return; }
            if (PaintFootprint.FixedSize || Tool is DrawingTool.Brush or DrawingTool.Fill)
            { RequestDrawingSound(EditorSoundAction.Paint); DrawGesture(start, start); return; }
            var cursor = PaintFootprint.CursorRectangle(start, document.Width, document.Height)!.Value;
            start = previous = new(cursor.X, cursor.Y);
        }
        if (Tool == DrawingTool.Fill) { FillAt(start); return; }
        if (Tool == DrawingTool.Brush)
        {
            RequestDrawingSound(EditorSoundAction.Paint);
            var source = document;
            var pen = livePen = source.BeginLivePenStroke(PaintTile, Selection);
            dragging = true;
            try { pen.Start(start); }
            catch { FinishLivePen(); dragging = false; throw; }
            if (!ReferenceEquals(source, document) || !ReferenceEquals(livePen, pen) || pen.IsClosed) return;
            CaptureMouse(); InvalidateVisual(); return;
        }
        if(Tool!=DrawingTool.Select)RequestDrawingSound(EditorSoundAction.DefineRectangle);
        dragging = true; stroke.Clear(); stroke.Add(start); CaptureMouse(); InvalidateVisual();
    }
    private bool PointerInsideMap(Point pointer) => document is not null && double.IsFinite(pointer.X) && double.IsFinite(pointer.Y)
        && pointer.X >= 0 && pointer.Y >= 0 && pointer.X < document.Width * cellWidth && pointer.Y < document.Height * cellHeight;
    internal bool CanRemoveUnderCursor=>document is not null && !GeometryOnly && !dragging && !rightSelecting && !IsPickingRegion;
    internal TilePoint? RemovalTile(Point pointer)=>CanRemoveUnderCursor && PointerInsideMap(pointer)?PointToTile(pointer):null;
    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    { base.OnMouseRightButtonDown(e); if (BeginRightMapPointer(e.GetPosition(this))) e.Handled = true; }
    internal bool BeginRightMapPointer(Point pointer)
    {
        if(IsPickingRegion&&!regionRightClickCancels)return true;
        if (rightSelecting || dragging || IsPickingRegion) { CancelRegionPick(); CancelDrawing(); return true; }
        if (!PointerInsideMap(pointer)) return false;
        Focus(); start = previous = PointToTile(pointer); CursorTile = start;
        rightSelecting = true; CaptureMouse();
        if (!GeometryOnly) document!.RecordActivity(ActivityCounterEvent.RightMapClick);
        CursorChanged?.Invoke(this, EventArgs.Empty); InvalidateVisual(); return true;
    }
    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        if (CompleteRightMapPointer(e.GetPosition(this), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift), Keyboard.Modifiers.HasFlag(ModifierKeys.Control))) e.Handled = true;
    }
    internal bool CompleteRightMapPointer(Point pointer, bool continuePath, bool newPath)
    {
        if (!rightSelecting || document is null) return false;
        CursorTile = PointToTile(pointer); rightSelecting = false; ReleaseMouseCapture();
        var bounds = TileRect.Between(start, CursorTile); SetSelection(bounds);
        if (!GeometryOnly) RightMapSelectionCompleted?.Invoke(this, new(bounds, continuePath, newPath));
        InvalidateVisual(); return true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        MoveMapPointer(e.GetPosition(this));
    }
    internal void MoveMapPointer(Point pointer)
    {
        if (document is null) return;
        CursorTile = PointToTile(pointer); CursorChanged?.Invoke(this, EventArgs.Empty);
        if (rightSelecting) { InvalidateVisual(); return; }
        if (!dragging) { InvalidateVisual(); return; }
        if (livePen is { } pen) { pen.Move(CursorTile); previous = CursorTile; InvalidateVisual(); return; }
        if (!IsPickingRegion && Tool == DrawingTool.Brush)
            foreach (var point in MapDrawing.Line(previous, CursorTile)) stroke.Add(point);
        previous = CursorTile; InvalidateVisual();
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (CompleteLeftMapPointer(e.GetPosition(this), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))) e.Handled = true;
    }
    internal bool CompleteLeftMapPointer(Point pointer, bool shift)
    {
        if (!dragging || document is null) return false;
        CursorTile = PointToTile(pointer); dragging = false;
        if (livePen is not null) { FinishLivePen(); ReleaseMouseCapture(); InvalidateVisual(); return true; }
        if (IsPickingRegion) { CompleteRegionPick(start, CursorTile); return true; }
        var rect = TileRect.Between(start, CursorTile);
        if (Tool == DrawingTool.Select)
        { SetSelection(rect); }
        else
        {
            if (Tool == DrawingTool.Brush)
                foreach (var point in MapDrawing.Line(previous, CursorTile)) stroke.Add(point);
            var soundRectangle=PaintFootprint.RequiresRectangle
                ? PaintFootprint.GestureRectangle(start,CursorTile,document.Width,document.Height):rect;
            RequestDrawingSound(EditorSoundAction.Paint,EditorSoundPaintMode.Shape,soundRectangle);
            DrawGesture(start, CursorTile, stroke, shift: shift);
        }
        stroke.Clear(); ReleaseMouseCapture(); InvalidateVisual(); return true;
    }
    protected override void OnLostMouseCapture(MouseEventArgs e)
    { base.OnLostMouseCapture(e); if (dragging && IsPickingRegion) CancelRegionPick(); FinishLivePen(); dragging = rightSelecting = false; stroke.Clear(); InvalidateVisual(); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key != Key.Escape) return;
        if (CancelRegionPick()) { e.Handled = true; return; }
        if (!dragging && !rightSelecting) return;
        CancelDrawing(); e.Handled = true;
    }
    private void CancelDrawing()
    {
        FinishLivePen();
        dragging = rightSelecting = false; stroke.Clear();
        if (IsMouseCaptured) ReleaseMouseCapture();
        InvalidateVisual();
    }
    private void FinishLivePen()
    {
        var pen = livePen; livePen = null;
        try{pen?.Dispose();}
        catch
        {
            dragging=rightSelecting=false;stroke.Clear();
            // The failed frame is already closed/rolled back by Core. Release
            // input capture before the error notice; cleanup must not hide it.
            try{if(IsMouseCaptured)ReleaseMouseCapture();InvalidateVisual();}catch{ }
            throw;
        }
    }
    /// <summary>Per-tile policy operation for prepared points; interactive
    /// cursor footprints are applied by DrawGesture/FillAt, not this raw stroke.</summary>
    public void PaintStroke(IEnumerable<TilePoint> points)
    {
        if (document is null || GeometryOnly) return;
        if (Selection is { } selected) points = points.Where(point => selected.Contains(point.X, point.Y));
        var before = BrushBefore(points);
        document.PaintEditor(before.Keys, PaintTile, Tool.ToString());
        CountChangedBrush(before);
    }
    public void DrawGesture(TilePoint origin, TilePoint end, IEnumerable<TilePoint>? brushPoints = null, bool shift = false, Func<int,int>? randomSample = null)
    {
        if (document is null || GeometryOnly || Tool is DrawingTool.Select or DrawingTool.Sample or DrawingTool.PlaceObject) return;
        var shape = Tool switch
        {
            DrawingTool.Line => EditorPaintShape.Line, DrawingTool.Rectangle => EditorPaintShape.Rectangle,
            DrawingTool.FilledRectangle => EditorPaintShape.FilledRectangle, DrawingTool.Ellipse => EditorPaintShape.Ellipse,
            DrawingTool.FilledEllipse => EditorPaintShape.FilledEllipse, _ => EditorPaintShape.Brush
        };
        var simpleBrush = shape == EditorPaintShape.Brush && !PaintFootprint.RequiresRectangle;
        var before = simpleBrush ? BrushBefore(brushPoints ?? MapDrawing.Line(origin, end)) : null;
        var result = EditorPlacementResult.Painted;
        void Draw() => result = document.PaintEditorGesture(origin, end, PaintTile, shape, Selection, before?.Keys ?? brushPoints,
            randomized: RandomPainting || shift, randomSample: randomSample);
        if (simpleBrush) Draw(); // Prepared raw brush points retain their existing no-op policy.
        else document.EditEditorDrawing((RandomPainting || shift) && RandomPaint.Supports(shape) && !PaintFootprint.RequiresRectangle
            ? "Random " + shape : shape.ToString(), () =>
        {
            Draw(); return new(result == EditorPlacementResult.Painted, result == EditorPlacementResult.Painted ? 1u : 0u);
        });
        if (result == EditorPlacementResult.Painted)
        {
            if (before is not null) CountChangedBrush(before);
            else document.RecordActivity((RandomPainting || shift) && RandomPaint.Supports(shape) && !PaintFootprint.RequiresRectangle
                ? ActivityCounterEvent.RandomPaint : ActivityCounterEvent.Paint);
        }
        SetPlacementResult(result);
    }
    public void FillAt(TilePoint point)
    {
        if (document is null || GeometryOnly) return;
        if (PaintFootprint.RequiresRectangle)
        {
            var result = EditorPlacementResult.Painted;
            document.EditEditorDrawing("FilledRectangle", () =>
            {
                result = document.PaintEditorGesture(point, point, PaintTile, EditorPaintShape.Brush, Selection);
                return new(result == EditorPlacementResult.Painted, result == EditorPlacementResult.Painted ? 1u : 0u);
            });
            if (result == EditorPlacementResult.Painted) document.RecordActivity(ActivityCounterEvent.Paint);
            SetPlacementResult(result); return;
        }
        var eligible = !document.IsEditorProtectedCell(point.X, point.Y) && (Selection is null || Selection.Value.Contains(point.X, point.Y));
        document.EditEditorDrawing("Fill", () =>
        {
            document.FloodFillEditor(point, PaintTile, Selection);
            // Mouse_LeftButtonDown allocated full Undo before protected-fill refusal.
            return new(true, eligible ? 1u : 0u);
        });
        // Map_FillArea has no equal-target return: a successful equal fill
        // still records one use. Protected/selection-excluded starts do not.
        if (eligible) document.RecordActivity(ActivityCounterEvent.Fill);
        ResetPlacement();
    }
    private Dictionary<TilePoint, MapTile> BrushBefore(IEnumerable<TilePoint> points) => points
        .Where(point => point.X > 0 && point.Y > 0 && point.X < document!.Width - 1 && point.Y < document.Height - 1
            && (Selection is null || Selection.Value.Contains(point.X, point.Y)))
        .Distinct().ToDictionary(point => point, point => document!.GetTile(point.X, point.Y));
    private void CountChangedBrush(Dictionary<TilePoint, MapTile> before)
    {
        // A prepared stroke is one operation, never one use per changed cell.
        // The original live pen also has separate initial/movement event sites;
        // their continuous-gesture lifecycle still needs the source audit.
        if (before.Any(pair => document!.GetTile(pair.Key.X, pair.Key.Y) != pair.Value))
            document!.RecordActivity(ActivityCounterEvent.Paint);
    }
    public void SampleAt(TilePoint point)
    {
        if (document is null) return;
        PaintTile = document.GetTile(point.X, point.Y); TileSampled?.Invoke(this, EventArgs.Empty);
    }
    private void SetPlacementResult(EditorPlacementResult result)
    { LastPlacementResult = result; InvalidateVisual(); PlacementChanged?.Invoke(this, EventArgs.Empty); }
    internal void RestorePlacementResult(EditorPlacementResult? result)
    { LastPlacementResult = result; PlacementChanged?.Invoke(this, EventArgs.Empty); }
    private void ResetPlacement()
    { LastPlacementResult = null; InvalidateVisual(); PlacementChanged?.Invoke(this, EventArgs.Empty); }
    private TilePoint Position(MouseEventArgs e)
    {
        var position = e.GetPosition(this);
        return PointToTile(position);
    }
    /// <summary>Shared mouse hit conversion, including captured drags outside the map.</summary>
    public TilePoint PointToTile(Point position)
    {
        if (document is null || !double.IsFinite(position.X) || !double.IsFinite(position.Y)) throw new ArgumentException("Open map and finite pointer required.");
        return new((int)Math.Clamp(Math.Floor(position.X / cellWidth), 0, document.Width - 1), (int)Math.Clamp(Math.Floor(position.Y / cellHeight), 0, document.Height - 1));
    }
    public Rect CellBounds(TileRect rect) => new(rect.X * cellWidth, rect.Y * cellHeight, rect.Width * cellWidth, rect.Height * cellHeight);
    private void DrawSelection(DrawingContext context, TileRect rect, Color color) =>
        context.DrawRectangle(null, new(new SolidColorBrush(color), 2), CellBounds(rect));
    private void UpdateSize() { Width = (document?.Width ?? 0) * cellWidth; Height = (document?.Height ?? 0) * cellHeight; InvalidateVisual(); }
    private void DocumentChanged(object? sender, EventArgs e)
    {
        RefreshObjectDisplay();
        RefreshOverlayDisplay();
        CancelRegionPick();
        if (document is not null)
        {
            if (Selection is { } rect && ((long)rect.X + rect.Width > document.Width || (long)rect.Y + rect.Height > document.Height)) ClearSelection();
            var cursor = new TilePoint(Math.Clamp(CursorTile.X, 0, document.Width - 1), Math.Clamp(CursorTile.Y, 0, document.Height - 1));
            if (cursor != CursorTile) { CursorTile = cursor; CursorChanged?.Invoke(this, EventArgs.Empty); }
        }
        UpdateSize();
    }
    private ScrollViewer? subscribedViewer;
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var viewer = FindScrollViewer();
        if (viewer == subscribedViewer) return;
        if (subscribedViewer is not null) subscribedViewer.ScrollChanged -= ScrollChanged;
        subscribedViewer = viewer;
        if (viewer is not null) viewer.ScrollChanged += ScrollChanged;
        InvalidateVisual();
    }
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CancelDrawing();
        if (subscribedViewer is not null) subscribedViewer.ScrollChanged -= ScrollChanged;
        subscribedViewer = null;
    }
    private void ScrollChanged(object sender, ScrollChangedEventArgs e) => InvalidateVisual();
    private ScrollViewer? FindScrollViewer()
    {
        DependencyObject? current = this;
        while ((current = VisualTreeHelper.GetParent(current)) is not null)
            if (current is ScrollViewer viewer) return viewer;
        return null;
    }
}
