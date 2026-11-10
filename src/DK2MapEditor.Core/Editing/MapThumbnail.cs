using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed record MapThumbnailOptions(uint WorldFlags = uint.MaxValue, uint PlayersFlags = uint.MaxValue,
    uint HiddenColour = 0, bool UseColourKey = false, bool KeepProportions = true, int PreviewSize = 144)
{
    public int ExportSize => PreviewSize == 144 ? 128 : 96;
    public void Validate()
    {
        if (PreviewSize is not (96 or 144)) throw new ArgumentOutOfRangeException(nameof(PreviewSize));
        if (HiddenColour > 0xFFFFFF) throw new ArgumentOutOfRangeException(nameof(HiddenColour));
    }
}
public sealed record ThumbnailVisibility(string Key, bool PlayerScoped, uint Mask);
public sealed record ThumbnailRaster(int Width,int Height,ReadOnlyMemory<byte> Bgra);

/// <summary>Original thumbnail filtering/background/BMP workflow. Rendering is
/// read-only; the only disk operation publishes a new file without overwriting.</summary>
public static class MapThumbnail
{
    public static IReadOnlyList<ThumbnailVisibility> WorldChoices { get; } = new[]
    { "Limit","Water","Lava","Gold","Gems","Solid","Rock","Unclaimed","HeroRest" }
        .Select((key,index) => new ThumbnailVisibility(key,false,1u << index)).ToList().AsReadOnly();
    public static IReadOnlyList<string> PlayerCategories { get; } = Array.AsReadOnly(new[] { "Walls","Rooms","Portals","Hearts" });
    public static IReadOnlyList<ThumbnailVisibility> PlayerChoices(int owner)
    {
        if (owner is < 1 or > 7) throw new ArgumentOutOfRangeException(nameof(owner));
        return PlayerCategories.Select((key,index) => new ThumbnailVisibility(key,true,1u << (4 + (owner - 1) * 4 + index))).ToList().AsReadOnly();
    }
    private sealed record VisibilityRule(bool PlayerScoped,uint Mask);
    private static readonly VisibilityRule[,,] rules = BuildRules();
    private static readonly byte[] background = ReadBackground();
    public static uint Colour(MapTile tile,MapThumbnailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options); options.Validate();
        var rule = rules[tile.Terrain,tile.Owner is >= 1 and <= 7 ? tile.Owner : 2,tile.Variant is 1 or 2 ? tile.Variant - 1 : 0];
        return ((rule.PlayerScoped ? options.PlayersFlags : options.WorldFlags) & rule.Mask) != 0 ? MapOverviewPalette.ColorRef(tile) : options.HiddenColour;
    }
    public static ThumbnailRaster Filter(NativeMapDocument document,MapThumbnailOptions options)
    {
        ArgumentNullException.ThrowIfNull(document); ArgumentNullException.ThrowIfNull(options); options.Validate();
        var pixels = new byte[checked(document.Width * document.Height * 4)];
        for (var y = 0; y < document.Height; y++) for (var x = 0; x < document.Width; x++)
            Put(pixels,(y * document.Width + x) * 4,Colour(document.GetTile(x,y),options));
        return new(document.Width,document.Height,pixels);
    }
    /// <summary>Source colour-key compositing produces an opaque image over
    /// resource 996, not alpha transparency. Preview and file sizes differ.</summary>
    public static ThumbnailRaster Compose(ThumbnailRaster source,MapThumbnailOptions options,bool export = false)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(options); options.Validate();
        if (source.Width < 1 || source.Height < 1 || (long)source.Width * source.Height * 4 != source.Bgra.Length) throw new ArgumentException("Invalid thumbnail raster.",nameof(source));
        var size = export ? options.ExportSize : options.PreviewSize; var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            background.AsSpan((y * 144 / size * 144 + x * 144 / size) * 4,4).CopyTo(pixels.AsSpan((y * size + x) * 4));
        var width = size; var height = size;
        if (options.KeepProportions)
        {
            // Retain at least one visible pixel for very narrow valid maps.
            if (source.Width < source.Height) width = Math.Max(1,(int)((long)size * source.Width / source.Height));
            else height = Math.Max(1,(int)((long)size * source.Height / source.Width));
        }
        var left = (size - width) / 2; var top = (size - height) / 2; var input = source.Bgra.Span;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
        {
            var offset = ((int)((long)y * source.Height / height) * source.Width + (int)((long)x * source.Width / width)) * 4;
            var colour = (uint)(input[offset + 2] | input[offset + 1] << 8 | input[offset] << 16);
            if (options.UseColourKey && colour == options.HiddenColour) continue;
            input.Slice(offset,4).CopyTo(pixels.AsSpan(((top + y) * size + left + x) * 4));
            pixels[((top + y) * size + left + x) * 4 + 3] = 255;
        }
        return new(size,size,pixels);
    }
    public static ThumbnailRaster Render(NativeMapDocument document,MapThumbnailOptions options,bool export = false) => Compose(Filter(document,options),options,export);
    public static byte[] Bmp(NativeMapDocument document,MapThumbnailOptions options) => EncodeBmp(Render(document,options,export: true));
    public static byte[] EncodeBmp(ThumbnailRaster image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Width < 1 || image.Height < 1 || (long)image.Width * image.Height * 4 != image.Bgra.Length) throw new ArgumentException("Invalid thumbnail raster.",nameof(image));
        var stride = checked((image.Width * 3 + 3) & ~3); var bytes = new byte[checked(54 + stride * image.Height)];
        void U32(int offset,uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset),value);
        bytes[0] = (byte)'B'; bytes[1] = (byte)'M'; U32(2,(uint)bytes.Length); U32(10,54); U32(14,40);
        U32(18,(uint)image.Width); U32(22,(uint)image.Height); bytes[26] = 1; bytes[28] = 24; U32(34,(uint)(stride * image.Height));
        var pixels = image.Bgra.Span;
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
            pixels.Slice((y * image.Width + x) * 4,3).CopyTo(bytes.AsSpan(54 + (image.Height - y - 1) * stride + x * 3));
        return bytes;
    }
    public static void SaveNewBmp(string path,NativeMapDocument document,MapThumbnailOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path); var bytes = Bmp(document,options); var target = Path.GetFullPath(path);
        if (File.Exists(target) || Directory.Exists(target)) throw new IOException("Thumbnail destination already exists.");
        var parent = Path.GetDirectoryName(target)!;
        if (!Directory.Exists(parent)) throw new DirectoryNotFoundException(parent);
        var staging = Path.Combine(parent,".dk2-thumbnail-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var file = new FileStream(staging,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { file.Write(bytes); file.Flush(flushToDisk: true); }
            File.Move(staging,target,overwrite: false);
        }
        finally { if (File.Exists(staging)) File.Delete(staging); }
    }
    private static void Put(byte[] pixels,int offset,uint colour)
    { pixels[offset] = (byte)(colour >> 16); pixels[offset + 1] = (byte)(colour >> 8); pixels[offset + 2] = (byte)colour; pixels[offset + 3] = 255; }
    private static byte[] ReadBackground()
    {
        using var stream = typeof(MapThumbnail).Assembly.GetManifestResourceStream("Legacy.ThumbnailBackground") ?? throw new InvalidDataException("Missing original thumbnail background.");
        using var buffer = new MemoryStream(); stream.CopyTo(buffer); var bytes = buffer.ToArray();
        int I32(int offset) => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));
        if (bytes.Length < 54 || bytes[0] != 'B' || bytes[1] != 'M' || I32(14) != 40 || I32(18) != 144 || I32(22) != 144
            || BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(28)) != 24 || I32(30) != 0 || I32(10) < 54 || (long)I32(10) + 144 * 144 * 3 > bytes.Length)
            throw new InvalidDataException("Unsupported original thumbnail background layout.");
        var pixels = new byte[144 * 144 * 4];
        for (var y = 0; y < 144; y++) for (var x = 0; x < 144; x++)
        {
            var offset = I32(10) + ((143 - y) * 144 + x) * 3; var colour = (uint)(bytes[offset + 2] | bytes[offset + 1] << 8 | bytes[offset] << 16);
            Put(pixels,(y * 144 + x) * 4,colour);
        }
        return pixels;
    }
    private static VisibilityRule[,,] BuildRules()
    {
        var groups = new[] { "World","Rooms","Gates","Walls" }.Select(LegacyCompilerRules.Elements).ToArray();
        var world = new Dictionary<byte,uint> { [30] = 1,[254] = 1,[4] = 2,[5] = 4,[6] = 8,[7] = 16,[1] = 32,[2] = 64,[3] = 128,[35] = 256 };
        var result = new VisibilityRule[256,8,2];
        for (var terrain = 0; terrain < 256; terrain++) for (var owner = 1; owner <= 7; owner++) for (var variant = 1; variant <= 2; variant++)
        {
            var key = (uint)(terrain << 24 | variant << 8); VisibilityRule? rule = null;
            for (var group = 0; group < 4 && rule is null; group++)
            {
                var id = group == 3 ? key | (uint)(owner << 16) : key; var mask = group == 3 ? 0xFFFFFF00u : 0xFF00FF00u;
                var entry = groups[group].FirstOrDefault(element => (element.ID & mask) == id);
                if (entry is null) continue;
                if (group == 0) rule = terrain == 31 ? new(true,PlayerChoices(2)[1].Mask) : new(false,world[(byte)terrain]);
                else if (group == 2) rule = new(true,PlayerChoices(1)[2].Mask);
                else rule = new(true,PlayerChoices(owner)[group == 3 ? 0 : terrain == 14 ? 3 : terrain is 12 or 40 ? 2 : 1].Mask);
            }
            result[terrain,owner,variant - 1] = rule ?? new(false,64); // Original unknown-terrain-to-Rock display conversion.
        }
        return result;
    }
}
