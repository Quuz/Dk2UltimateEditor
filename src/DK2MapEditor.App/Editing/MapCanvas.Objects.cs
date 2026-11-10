using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class MapCanvas
{
    private MapObjectDisplayOptions objectDisplayOptions = new();
    public MapObjectDisplay? ObjectDisplay { get; private set; }
    public MapObjectDisplayOptions ObjectDisplayOptions
    {
        get => objectDisplayOptions;
        set { objectDisplayOptions = value ?? new(); InvalidateVisual(); }
    }
    private void RefreshObjectDisplay() => ObjectDisplay = document is null ? null : MapObjectDisplay.Capture(document);
    private static Brush ObjectBrush(uint colour)
    {
        var brush = new SolidColorBrush(Color.FromRgb((byte)colour, (byte)(colour >> 8), (byte)(colour >> 16)));
        brush.Freeze(); return brush;
    }
    private static Rect ObjectRect(MapObjectBox box) => new(box.X, box.Y, box.Width, box.Height);
    private void DrawMapObjects(DrawingContext context, TileRect visible)
    {
        if (ObjectDisplay is null) return;
        foreach (var mark in ObjectDisplay.Marks)
        {
            if (!visible.Contains(mark.Position.X, mark.Position.Y) || MapObjectDisplay.Glyph(mark, cellWidth, cellHeight, objectDisplayOptions) is not { } glyph) continue;
            var rect = ObjectRect(glyph.Bounds); var brush = ObjectBrush(glyph.Colour);
            if (glyph.Shape == MapObjectShape.Rectangle)
            {
                if (glyph.Blueprint)
                {
                    // The source draws two one-pixel inset outlines; small zooms
                    // remain bounded rather than constructing negative WPF Rects.
                    context.DrawRectangle(null, new Pen(brush, 1), rect);
                    if (rect.Width > 2 && rect.Height > 2) { rect.Inflate(-1, -1); context.DrawRectangle(null, new Pen(brush, 1), rect); }
                }
                else context.DrawRectangle(brush, null, rect);
            }
            else if (glyph.Shape == MapObjectShape.Ellipse)
                context.DrawEllipse(brush, new Pen(Brushes.Black, 1), new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), rect.Width / 2, rect.Height / 2);
            else
            {
                // Chord: start ray is upper-right, end lower-left, GDI's
                // counter-clockwise arc is the upper-left half of the ellipse.
                var geometry = new StreamGeometry(); var dx = rect.Width / (2 * Math.Sqrt(2)); var dy = rect.Height / (2 * Math.Sqrt(2));
                var center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                using (var path = geometry.Open())
                {
                    path.BeginFigure(new(center.X + dx, center.Y - dy), true, true);
                    path.ArcTo(new(center.X - dx, center.Y + dy), new(rect.Width / 2, rect.Height / 2), 0, false, SweepDirection.Counterclockwise, true, false);
                }
                geometry.Freeze(); context.DrawGeometry(brush, new Pen(Brushes.Black, 1), geometry);
            }
            if (glyph.Cross is { } cross)
            {
                var box = ObjectRect(cross); var special = mark.Kind == PlacedThingKind.SpecialItem;
                var pen = new Pen(special ? ObjectBrush(0x555555) : Brushes.Black, 1);
                if (special)
                {
                    context.DrawLine(pen, new(box.X + box.Width / 2, box.Y), new(box.X + box.Width / 2, box.Bottom));
                    context.DrawLine(pen, new(box.X, box.Y + box.Height / 2), new(box.Right, box.Y + box.Height / 2));
                }
                else
                {
                    context.DrawLine(pen, box.TopLeft, box.BottomRight); context.DrawLine(pen, box.TopRight, box.BottomLeft);
                }
            }
        }
        if ((objectDisplayOptions.Flags & MapObjectViewFlags.Creatures) == 0) return;
        var typeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        foreach (var count in ObjectDisplay.Creatures)
        {
            if (!visible.Contains(count.Position.X, count.Position.Y)) continue;
            var x = count.Position.X * cellWidth; var y = count.Position.Y * cellHeight;
            void Number(string value, uint background, bool warning = false)
            {
                var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface,
                    SystemFonts.MessageFontSize, warning ? Brushes.White : Brushes.Black, pixelsPerDip);
                context.DrawRectangle(ObjectBrush(background), null, new(x, y, text.WidthIncludingTrailingWhitespace, text.Height));
                context.DrawText(text, new(x, y)); x += text.WidthIncludingTrailingWhitespace;
            }
            if (count.Solid) { Number("!!!", 0xFF, true); continue; }
            if ((objectDisplayOptions.Flags & MapObjectViewFlags.Detailed) == 0) Number(count.Owners.Sum().ToString(CultureInfo.InvariantCulture), 0xFFFFFF);
            else for (var owner = 0; owner < 7; owner++)
                if (count.Owners[owner] != 0) Number(count.Owners[owner].ToString(CultureInfo.InvariantCulture), MapObjectDisplay.OwnerColour((byte)(owner + 1)));
        }
    }
}
