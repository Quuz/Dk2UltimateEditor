using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DK2MapEditor.App.Editing;

/// <summary>Source scenario image-list bitmaps, loaded once at its 32-pixel row size.</summary>
public static class OriginalScenarioImages
{
    private static readonly Dictionary<int, BitmapSource> images = [];
    public static BitmapSource? For(int? identity)
    {
        if (identity is not { } id) return null;
        var resource = OriginalObjectImages.CanonicalResource(id);
        lock (images)
        {
            if (images.TryGetValue(resource, out var cached)) return cached;
            var assembly = typeof(OriginalScenarioImages).Assembly;
            var name = resource.ToString(CultureInfo.InvariantCulture);
            using var stream = assembly.GetManifestResourceStream("Legacy.ScenarioImage." + name)
                ?? assembly.GetManifestResourceStream("Legacy.ObjectImage." + name)
                ?? assembly.GetManifestResourceStream("Legacy.TerrainImage." + name)
                ?? assembly.GetManifestResourceStream("Legacy.DefinitionImage." + name)
                ?? assembly.GetManifestResourceStream("Legacy.OverviewIcon." + name)
                ?? throw new InvalidOperationException("Missing original scenario image: " + id);
            var original = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapSource result = original.PixelWidth == 32 && original.PixelHeight == 32 ? original
                : new TransformedBitmap(original, new ScaleTransform(32d / original.PixelWidth, 32d / original.PixelHeight));
            result.Freeze(); images.Add(resource, result); return result;
        }
    }
}
