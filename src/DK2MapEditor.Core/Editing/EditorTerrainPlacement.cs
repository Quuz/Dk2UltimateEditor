namespace DK2MapEditor.Core.Editing;

public enum EditorPaintShape { Brush, Line, Rectangle, FilledRectangle, Ellipse, FilledEllipse }
public enum EditorPlacementResult { Painted, MapTooSmall, ProtectedFootprint, SelectionClipsFootprint }

public sealed partial class NativeMapDocument
{
    /// <summary>Mouse_LeftButtonDown tests the initial minimum cursor against
    /// locked cells for bounded terrain. Larger drags still skip locked writes,
    /// matching the original release routine rather than rejecting whole drags.</summary>
    public EditorPlacementResult CheckEditorPlacement(TilePoint start, MapTile selected, TileRect? limit = null)
    {
        CheckPoint(start.X, start.Y);
        var footprint = EditorTerrainRules.Footprint(selected);
        if (footprint.CursorRectangle(start, Width, Height) is not { } rectangle) return EditorPlacementResult.MapTooSmall;
        if (footprint.RequiresRectangle && (rectangle.X == 0 || rectangle.Y == 0 || rectangle.X + rectangle.Width == Width || rectangle.Y + rectangle.Height == Height))
            return EditorPlacementResult.ProtectedFootprint;
        if (footprint.RequiresRectangle && limit is { } bounds && !Contains(bounds, rectangle)) return EditorPlacementResult.SelectionClipsFootprint;
        return EditorPlacementResult.Painted;
    }

    /// <summary>Source-catalogue cursor sizes and forced filled rectangles for
    /// hearts/portals/gates. Raw PaintEditor remains a per-tile policy operation.</summary>
    public EditorPlacementResult PaintEditorGesture(TilePoint start, TilePoint end, MapTile selected, EditorPaintShape shape,
        TileRect? limit = null, IEnumerable<TilePoint>? brushPoints = null, bool randomized = false,
        int randomFrequency = RandomPaint.DefaultFrequency, Func<int,int>? randomSample = null)
    {
        if (!Enum.IsDefined(shape)) throw new ArgumentOutOfRangeException(nameof(shape));
        var result = CheckEditorPlacement(start, selected, limit);
        if (result != EditorPlacementResult.Painted) return result;
        var footprint = EditorTerrainRules.Footprint(selected);
        end = new(Math.Clamp(end.X, 0, Width - 1), Math.Clamp(end.Y, 0, Height - 1));
        var rectangle = footprint.GestureRectangle(start, end, Width, Height, immediate: shape == EditorPaintShape.Brush && footprint.RequiresRectangle)!.Value;
        if (footprint.RequiresRectangle && limit is { } bounds && !Contains(bounds, rectangle)) return EditorPlacementResult.SelectionClipsFootprint;
        IEnumerable<TilePoint> points = footprint.RequiresRectangle ? MapDrawing.Rectangle(rectangle, true) : shape switch
        {
            EditorPaintShape.Brush => brushPoints ?? MapDrawing.Line(start, end),
            EditorPaintShape.Line => MapDrawing.Line(start, end),
            EditorPaintShape.Rectangle => MapDrawing.Rectangle(rectangle, false),
            EditorPaintShape.FilledRectangle => MapDrawing.Rectangle(rectangle, true),
            EditorPaintShape.Ellipse => MapDrawing.Ellipse(rectangle, false),
            EditorPaintShape.FilledEllipse => MapDrawing.Ellipse(rectangle, true),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        var useRandom = randomized && RandomPaint.Supports(shape) && !footprint.RequiresRectangle;
        if(useRandom && randomFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(randomFrequency));
        if(!footprint.RequiresRectangle && RandomPaint.Supports(shape) && OperatingSystem.IsWindows())
            points = OriginalPaintRaster.Points(shape,start,end,rectangle);
        if(useRandom)
        {
            var mask = points.ToHashSet(); var generator = new Random();
            points = RandomPaint.Points(rectangle,mask,randomFrequency,randomSample ?? generator.Next);
        }
        if (limit is { } selection) points = points.Where(point => selection.Contains(point.X, point.Y));
        PaintEditor(points, selected, useRandom ? "Random " + shape : shape.ToString());
        return EditorPlacementResult.Painted;
    }

    private static bool Contains(TileRect outer, TileRect inner) => outer.Contains(inner.X, inner.Y)
        && outer.Contains(inner.X + inner.Width - 1, inner.Y + inner.Height - 1);
}
