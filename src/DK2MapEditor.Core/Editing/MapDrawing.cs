namespace DK2MapEditor.Core.Editing;

public static class MapDrawing
{
    public static IEnumerable<TilePoint> Line(TilePoint start, TilePoint end)
    {
        var x = start.X;
        var y = start.Y;
        var dx = Math.Abs(end.X - x);
        var dy = -Math.Abs(end.Y - y);
        var sx = x < end.X ? 1 : -1;
        var sy = y < end.Y ? 1 : -1;
        var error = dx + dy;
        while (true)
        {
            yield return new(x, y);
            if (x == end.X && y == end.Y) yield break;
            var twice = 2 * error;
            if (twice >= dy) { error += dy; x += sx; }
            if (twice <= dx) { error += dx; y += sy; }
        }
    }

    public static IEnumerable<TilePoint> Rectangle(TileRect rect, bool filled)
    {
        for (var y = rect.Y; y < (long)rect.Y + rect.Height; y++)
            for (var x = rect.X; x < (long)rect.X + rect.Width; x++)
                if (filled || x == rect.X || y == rect.Y || x == rect.X + rect.Width - 1 || y == rect.Y + rect.Height - 1)
                    yield return new(x, y);
    }

    public static IEnumerable<TilePoint> Ellipse(TileRect rect, bool filled)
    {
        var rx = rect.Width / 2.0;
        var ry = rect.Height / 2.0;
        if (rx <= 0 || ry <= 0) yield break;
        bool Inside(int x, int y) => Math.Pow((x - rect.X + 0.5 - rx) / rx, 2) + Math.Pow((y - rect.Y + 0.5 - ry) / ry, 2) <= 1;
        foreach (var point in Rectangle(rect, true))
            if (Inside(point.X, point.Y) && (filled || !Inside(point.X - 1, point.Y) || !Inside(point.X + 1, point.Y) || !Inside(point.X, point.Y - 1) || !Inside(point.X, point.Y + 1)))
                yield return point;
    }
}
