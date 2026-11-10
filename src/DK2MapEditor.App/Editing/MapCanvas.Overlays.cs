using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class MapCanvas
{
    private MapOverlayOptions overlayOptions = new();
    public MapOverlayDisplay? OverlayDisplay { get; private set; }
    public MapOverlayOptions OverlayOptions
    {
        get => overlayOptions;
        set { overlayOptions = (value ?? new()) with { PathColour = (value?.PathColour ?? 0xFFFFFF) & 0xFFFFFF }; InvalidateVisual(); }
    }
    private void RefreshOverlayDisplay() => OverlayDisplay = document is null ? null : MapOverlayDisplay.Capture(document);
    private void DrawMapOverlays(DrawingContext context, TileRect visible)
    {
        if (OverlayDisplay is null) return;
        var viewer = FindScrollViewer();
        var viewport = viewer is null ? new Rect(0, 0, ActualWidth, ActualHeight)
            : new Rect(viewer.HorizontalOffset, viewer.VerticalOffset, viewer.ViewportWidth, viewer.ViewportHeight);
        var typeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        if (ShowEntities) foreach (var region in OverlayDisplay.Regions)
        {
            if (region.Hidden || (overlayOptions.Flags & MapOverlayDisplay.Flag(region.Kind)) == 0) continue;
            var bounds = region.Kind == MapRegionKind.Gate && (overlayOptions.Flags & MapOverlayFlags.ExpandedGates) != 0 ? region.ExpandedBounds : region.Bounds;
            var box = new Rect(bounds.X * cellWidth, bounds.Y * cellHeight, Math.Max(0, bounds.Width * cellWidth - 1), Math.Max(0, bounds.Height * cellHeight - 1));
            if (!box.IntersectsWith(viewport)) continue;
            var name = region.Name ?? LocalizationService.Get("Editor.RegionKind." + region.Kind) + " " + region.DisplayID.ToString(CultureInfo.InvariantCulture);
            var textBox = Rect.Intersect(box, viewport); textBox.Inflate(-3, -3);
            if (!string.IsNullOrEmpty(name) && textBox.Width > 0 && textBox.Height > 0)
            {
                var text = new FormattedText(name, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface,
                    SystemFonts.MessageFontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip) { MaxTextWidth = textBox.Width, MaxTextHeight = textBox.Height };
                context.PushClip(new RectangleGeometry(Rect.Intersect(box, viewport)));
                context.DrawText(text, textBox.TopLeft); text.SetForegroundBrush(ObjectBrush(region.TextColour));
                context.DrawText(text, new(textBox.X - 1, textBox.Y - 1)); context.Pop();
            }
            context.DrawRectangle(null, new Pen(ObjectBrush(region.Colour), 1), box);
        }
        if (ShowPatrolPaths)
        {
            var pen = new Pen(ObjectBrush(overlayOptions.PathColour), 1);
            foreach (var path in OverlayDisplay.Paths)
            {
                var glyph = MapOverlayDisplay.PathGlyph(path, cellWidth, cellHeight);
                if (glyph.Loop) context.DrawEllipse(null, pen, new((glyph.X1 + glyph.X2) / 2, (glyph.Y1 + glyph.Y2) / 2), (glyph.X2 - glyph.X1) / 2, (glyph.Y2 - glyph.Y1) / 2);
                else context.DrawLine(pen, new(glyph.X1, glyph.Y1), new(glyph.X2, glyph.Y2));
            }
        }
        if (ShowEntities && (overlayOptions.Flags & MapOverlayFlags.Cameras) != 0) foreach (var camera in OverlayDisplay.Cameras)
        {
            if (!visible.Contains(camera.Position.X, camera.Position.Y)) continue;
            var glyph = MapOverlayDisplay.CameraGlyph(camera, cellWidth, cellHeight); var ellipse = glyph.Ellipse;
            // Tiny extension zooms can invert source overlap ellipses. Keep the
            // icon, but do not construct an invalid WPF negative-radius shape.
            if (ellipse.Width > 0 && ellipse.Height > 0) context.DrawEllipse(null, new Pen(ObjectBrush(glyph.Colour), 2),
                new(ellipse.X + ellipse.Width / 2, ellipse.Y + ellipse.Height / 2), ellipse.Width / 2, ellipse.Height / 2);
            context.DrawImage(OriginalCameraImages.For(glyph.Resource), ObjectRect(glyph.Icon));
        }
    }
}
