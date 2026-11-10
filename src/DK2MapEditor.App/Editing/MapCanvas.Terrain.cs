using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class MapCanvas
{
    private bool showTerrain = true;
    public bool ShowTerrain { get => showTerrain; set { showTerrain = value; InvalidateVisual(); } }
    private void DrawTerrain(DrawingContext context, TileRect visible)
    {
        if (!showTerrain || document is null) return;
        var width = (int)Math.Ceiling(cellWidth); var height = (int)Math.Ceiling(cellHeight);
        var bitmaps = new Dictionary<MapTile, BitmapSource>();
        for (var y = visible.Y; y < visible.Y + visible.Height; y++) for (var x = visible.X; x < visible.X + visible.Width; x++)
        {
            var tile = document.GetTile(x, y);
            if (!bitmaps.TryGetValue(tile, out var image)) bitmaps.Add(tile, image = OriginalTerrainImages.For(tile, width, height));
            context.DrawImage(image, new Rect(x * cellWidth, y * cellHeight, cellWidth, cellHeight));
        }
    }
}
