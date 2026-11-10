using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public static class OriginalTerrainImages
{
    private sealed record CacheItem(BitmapSource Bitmap, LinkedListNode<(MapTerrainImage, int, int)> Node, long Bytes);
    private static readonly Dictionary<int, BitmapSource> originals = [];
    private static readonly Dictionary<(MapTerrainImage, int, int), CacheItem> scaled = [];
    private static readonly LinkedList<(MapTerrainImage, int, int)> order = [];
    private static long cachedBytes;
    public static BitmapSource Raw(int resource)
    {
        lock (originals)
        {
            if (originals.TryGetValue(resource, out var bitmap)) return bitmap;
            using var stream = typeof(OriginalTerrainImages).Assembly.GetManifestResourceStream("Legacy.TerrainImage." + resource)
                ?? typeof(OriginalTerrainImages).Assembly.GetManifestResourceStream("Legacy.OverviewIcon." + resource)
                ?? throw new InvalidOperationException("Missing original terrain image: " + resource);
            bitmap = new FormatConvertedBitmap(BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad), PixelFormats.Bgra32, null, 0);
            bitmap.Freeze(); originals.Add(resource, bitmap); return bitmap;
        }
    }
    public static BitmapSource For(MapTile tile, int width, int height)
    {
        if (!MapZoom.Valid(width) || !MapZoom.Valid(height)) throw new ArgumentOutOfRangeException(nameof(width));
        var source = MapTerrainImages.For(tile); var key = (source, width, height);
        lock (scaled)
        {
            if (scaled.TryGetValue(key, out var old)) { order.Remove(old.Node); order.AddLast(old.Node); return old.Bitmap; }
            var pixels = Scale(Raw(source.Resource), width, height);
            if (source.OwnerFrame is { } frame)
            {
                var colour = Scale(Raw(frame), width, height);
                for (var i = 0; i < pixels.Length; i++) pixels[i] |= colour[i];
            }
            var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4); bitmap.Freeze();
            var node = order.AddLast(key); scaled.Add(key, new(bitmap, node, pixels.LongLength)); cachedBytes += pixels.LongLength;
            while (scaled.Count > 256 || cachedBytes > 64L * 1024 * 1024)
            {
                var first = order.First!; cachedBytes -= scaled[first.Value].Bytes; scaled.Remove(first.Value); order.RemoveFirst();
            }
            return bitmap;
        }
    }
    private static byte[] Scale(BitmapSource source, int width, int height)
    {
        // Source LoadImage resizes an opaque BMP. COLORONCOLOR GDI scaling is
        // independently compared to LoadImage(LR_LOADFROMFILE) by Windows tests.
        var srcInfo = Info(source.PixelWidth, source.PixelHeight); var dstInfo = Info(width, height);
        IntPtr srcDC = IntPtr.Zero, dstDC = IntPtr.Zero, srcBitmap = IntPtr.Zero, dstBitmap = IntPtr.Zero, oldSrc = IntPtr.Zero, oldDst = IntPtr.Zero;
        try
        {
            srcDC = CreateCompatibleDC(IntPtr.Zero); dstDC = CreateCompatibleDC(IntPtr.Zero);
            if (srcDC == IntPtr.Zero || dstDC == IntPtr.Zero) throw new Win32Exception();
            srcBitmap = CreateDIBSection(srcDC, ref srcInfo, 0, out var srcBits, IntPtr.Zero, 0);
            dstBitmap = CreateDIBSection(dstDC, ref dstInfo, 0, out var dstBits, IntPtr.Zero, 0);
            if (srcBitmap == IntPtr.Zero || dstBitmap == IntPtr.Zero) throw new Win32Exception();
            var original = new byte[source.PixelWidth * source.PixelHeight * 4]; source.CopyPixels(original, source.PixelWidth * 4, 0); Marshal.Copy(original, 0, srcBits, original.Length);
            oldSrc = SelectObject(srcDC, srcBitmap); oldDst = SelectObject(dstDC, dstBitmap);
            if (oldSrc == IntPtr.Zero || oldDst == IntPtr.Zero || oldSrc == new IntPtr(-1) || oldDst == new IntPtr(-1)) throw new Win32Exception();
            if (SetStretchBltMode(dstDC, 3) == 0 || !StretchBlt(dstDC, 0, 0, width, height, srcDC, 0, 0, source.PixelWidth, source.PixelHeight, 0x00CC0020)) throw new Win32Exception();
            var pixels = new byte[width * height * 4]; Marshal.Copy(dstBits, pixels, 0, pixels.Length);
            for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255; return pixels;
        }
        finally
        {
            if (oldSrc != IntPtr.Zero && oldSrc != new IntPtr(-1)) SelectObject(srcDC, oldSrc);
            if (oldDst != IntPtr.Zero && oldDst != new IntPtr(-1)) SelectObject(dstDC, oldDst);
            if (srcBitmap != IntPtr.Zero) DeleteObject(srcBitmap); if (dstBitmap != IntPtr.Zero) DeleteObject(dstBitmap);
            if (srcDC != IntPtr.Zero) DeleteDC(srcDC); if (dstDC != IntPtr.Zero) DeleteDC(dstDC);
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo { public uint Size; public int Width, Height; public ushort Planes, Bits; public uint Compression, ImageSize; public int XPels, YPels; public uint Used, Important, Colour; }
    private static BitmapInfo Info(int width, int height) => new() { Size = 40, Width = width, Height = -height, Planes = 1, Bits = 32 };
    [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BitmapInfo info, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern int SetStretchBltMode(IntPtr hdc, int mode);
    [DllImport("gdi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool StretchBlt(IntPtr dest, int x, int y, int width, int height, IntPtr src, int sx, int sy, int swidth, int sheight, uint operation);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteDC(IntPtr hdc);
}
