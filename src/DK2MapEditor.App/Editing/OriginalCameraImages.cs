using System.Windows.Media.Imaging;

namespace DK2MapEditor.App.Editing;

public static class OriginalCameraImages
{
    private static readonly Dictionary<int, BitmapSource> images = [];
    public static BitmapSource For(int resource)
    {
        if (resource is < 20 or > 24) throw new ArgumentOutOfRangeException(nameof(resource));
        lock (images)
        {
            if (images.TryGetValue(resource, out var image)) return image;
            using var stream = typeof(OriginalCameraImages).Assembly.GetManifestResourceStream("Legacy.CameraIcon." + resource)
                ?? throw new InvalidOperationException("Missing original keeper camera icon.");
            image = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames.Single(frame => frame.PixelWidth == 16 && frame.PixelHeight == 16);
            image.Freeze(); images.Add(resource, image); return image;
        }
    }
}
