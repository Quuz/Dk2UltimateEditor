namespace DK2MapEditor.Core.Editing;

public sealed record MapZoomOptions(double Width = 64, double Height = 64, int UserWidth = 64, int UserHeight = 64);
public static class MapZoom
{
    // Windows_ZoomButton.c, cases 0..10. The modern tiny zooms are retained
    // as extensions; user dimensions are bounded for safe managed rendering.
    public const int Minimum = 2, Maximum = 512;
    public static IReadOnlyList<int> Presets { get; } = Array.AsReadOnly(new[] { 10, 16, 20, 32, 40, 48, 60, 80, 96, 100, 128 });
    public static bool Valid(double value) => double.IsFinite(value) && value is >= Minimum and <= Maximum;
    public static MapZoomOptions Safe(MapZoomOptions? options)
    {
        var value = options ?? new();
        return new(Valid(value.Width) ? value.Width : 64, Valid(value.Height) ? value.Height : 64,
            Valid(value.UserWidth) ? value.UserWidth : 64, Valid(value.UserHeight) ? value.UserHeight : 64);
    }
}
