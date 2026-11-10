using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.Core.Formats;

public sealed record LegacySection(uint Signature, int Version, ReadOnlyMemory<byte> Payload);

/// <summary>Bounded DKM/DKZ container reader. Unknown sections are retained byte-for-byte.</summary>
public sealed class LegacySectionFile
{
    public const uint MapFileSignature = 0x4650414D;
    public const uint AreaFileSignature = 0x41455241;
    public const uint TileSectionSignature = 0x50414D4B;
    private const int MaximumFileBytes = 64 * 1024 * 1024;
    private readonly List<LegacySection> sections;
    private LegacySectionFile(uint signature, int version, List<LegacySection> sections)
    { Signature = signature; Version = version; this.sections = sections; }
    public uint Signature { get; }
    public int Version { get; }
    public IReadOnlyList<LegacySection> Sections => sections.AsReadOnly();

    public static LegacySectionFile Create(IEnumerable<LegacySection> sections)
        => Parse(new LegacySectionFile(MapFileSignature, 1, sections.ToList()).ToArray());

    public static LegacySectionFile Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumFileBytes || stream.Length < 12) throw new InvalidDataException("Invalid legacy project size.");
        var bytes = new byte[(int)stream.Length]; stream.ReadExactly(bytes);
        return Parse(bytes);
    }

    public static LegacySectionFile Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12 || bytes.Length > MaximumFileBytes) throw new InvalidDataException("Invalid legacy project size.");
        var signature = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        var version = BinaryPrimitives.ReadInt32LittleEndian(bytes[4..]);
        if (signature is not (MapFileSignature or AreaFileSignature) || version != 1)
            throw new InvalidDataException("Unsupported legacy project signature or version.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]) != bytes.Length - 12)
            throw new InvalidDataException("Legacy outer payload size does not match the file length.");
        var sections = new List<LegacySection>();
        var offset = 12;
        while (offset < bytes.Length)
        {
            if (offset > bytes.Length - 12) throw new InvalidDataException("Truncated legacy section header.");
            var type = BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
            var sectionVersion = BinaryPrimitives.ReadInt32LittleEndian(bytes[(offset + 4)..]);
            var size = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 8)..]);
            if (size > bytes.Length - offset - 12 || sectionVersion < 1)
                throw new InvalidDataException("Invalid legacy section size or version.");
            sections.Add(new(type, sectionVersion, bytes.Slice(offset + 12, (int)size).ToArray()));
            offset += 12 + (int)size;
            if (sections.Count > 1024) throw new InvalidDataException("Too many legacy sections.");
        }
        var document = new LegacySectionFile(signature, version, sections);
        if (sections.Count(section => section.Signature == TileSectionSignature) > 1)
            throw new InvalidDataException("A legacy project contains multiple KMAP sections.");
        if (sections.Any(section => section.Signature == TileSectionSignature)) _ = document.ReadTiles();
        return document;
    }

    public TileClipboard ReadTiles()
    {
        var map = sections.SingleOrDefault(section => section.Signature == TileSectionSignature)
            ?? throw new InvalidDataException("The legacy project has no KMAP section.");
        var data = map.Payload.Span;
        if (map.Version != 1 || data.Length < 8) throw new InvalidDataException("Unsupported legacy tile section.");
        var width = BinaryPrimitives.ReadUInt32LittleEndian(data);
        var height = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        if (width is 0 or > 1024 || height is 0 or > 1024 || 8L + width * (long)height * 4 != data.Length)
            throw new InvalidDataException("Invalid legacy map dimensions or tile payload length.");
        var tiles = new MapTile[width * height];
        for (var index = 0; index < tiles.Length; index++)
            tiles[index] = MapTile.FromPacked(BinaryPrimitives.ReadUInt32LittleEndian(data[(8 + index * 4)..]));
        return new((int)width, (int)height, tiles);
    }

    public void ReplaceTiles(TileClipboard clipboard)
    {
        if (clipboard.Width is < 1 or > 1024 || clipboard.Height is < 1 or > 1024 || (long)clipboard.Width * clipboard.Height != clipboard.Tiles.Count)
            throw new ArgumentException("Invalid tile clipboard.", nameof(clipboard));
        var bytes = new byte[8 + clipboard.Tiles.Count * 4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, clipboard.Width);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), clipboard.Height);
        for (var index = 0; index < clipboard.Tiles.Count; index++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8 + index * 4), clipboard.Tiles[index].Packed);
        var position = sections.FindIndex(section => section.Signature == TileSectionSignature);
        if (position < 0) throw new InvalidDataException("The legacy project has no KMAP section.");
        sections[position] = new(TileSectionSignature, 1, bytes);
    }

    public byte[] ToArray()
    {
        var length = 12L + sections.Sum(section => 12L + section.Payload.Length);
        if (length > MaximumFileBytes) throw new InvalidDataException("Legacy project exceeds its safe size limit.");
        var bytes = new byte[(int)length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, Signature);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), Version);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), bytes.Length - 12);
        var offset = 12;
        foreach (var section in sections)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), section.Signature);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 4), section.Version);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 8), section.Payload.Length);
            section.Payload.Span.CopyTo(bytes.AsSpan(offset + 12));
            offset += 12 + section.Payload.Length;
        }
        return bytes;
    }
}
