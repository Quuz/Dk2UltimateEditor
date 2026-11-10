namespace DK2MapEditor.Core.Editing;

public static class RandomPaint
{
    public const int DefaultFrequency = 2;
    public static bool Supports(EditorPaintShape shape) => shape is EditorPaintShape.Line or EditorPaintShape.Rectangle
        or EditorPaintShape.FilledRectangle or EditorPaintShape.Ellipse or EditorPaintShape.FilledEllipse;

    /// <summary>Consume one sample for every bounding cell BEFORE mask, selection
    /// or protected-cell tests, as Map_PaintElement does. Enumeration runs inside
    /// the document transaction so failures roll back all earlier writes.</summary>
    internal static IEnumerable<TilePoint> Points(TileRect rectangle,IReadOnlySet<TilePoint> mask,int frequency,Func<int,int> sample)
    {
        for(var y=rectangle.Y;y<rectangle.Y+rectangle.Height;y++) for(var x=rectangle.X;x<rectangle.X+rectangle.Width;x++)
        {
            var value=sample(frequency); if(value<0 || value>=frequency) throw new ArgumentException("Random paint sample is outside its requested range.",nameof(sample));
            var point=new TilePoint(x,y); if(value==0 && mask.Contains(point)) yield return point;
        }
    }
}
