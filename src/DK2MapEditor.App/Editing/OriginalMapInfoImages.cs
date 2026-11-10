using System.Windows.Media.Imaging;

namespace DK2MapEditor.App.Editing;

internal static class OriginalMapInfoImages
{
    private static readonly Dictionary<int,BitmapSource> images=[];
    internal static BitmapSource For(int resource)
    {
        if(resource is <30 or >32)throw new ArgumentOutOfRangeException(nameof(resource));
        if(images.TryGetValue(resource,out var image))return image;
        using var stream=typeof(OriginalMapInfoImages).Assembly.GetManifestResourceStream("Legacy.MapInfoIcon."+resource)
            ??throw new InvalidOperationException("Missing original map information icon: "+resource);
        image=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames.First(frame=>frame.PixelWidth==32&&frame.PixelHeight==32);
        image.Freeze();images.Add(resource,image);return image;
    }
}
