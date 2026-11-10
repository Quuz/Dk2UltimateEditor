using System.Buffers.Binary;
using System.Text;

namespace DK2MapEditor.Core.Formats;

/// <summary>Read-only original STR preview. Glyph IDs are not ANSI/UTF-8 text.</summary>
public sealed class GameTextResource
{
    public const int MaximumFileBytes = 16 * 1024 * 1024;
    public const int MaximumEntries = 65536;
    // Source/Includes/Editor.h enables DEBUG_TEXTS even in its normal build.
    public const bool OriginalDebugPreview = true;
    private const int MaximumPreviewBytes = 8 * 1024 * 1024;
    private readonly byte[] bytes;
    public uint HeaderValue { get; }
    public IReadOnlyList<GameTextEntry> Entries { get; }
    private GameTextResource(byte[] bytes, uint headerValue, GameTextEntry[] entries)
    { this.bytes = bytes; HeaderValue = headerValue; Entries = Array.AsReadOnly(entries); }

    public static GameTextResource Load(string path) => Parse(GameResourceFiles.ReadBounded(path, MaximumFileBytes));
    public static GameTextResource Parse(ReadOnlySpan<byte> input)
    {
        if (input.Length < 12 || input.Length > MaximumFileBytes || !input[..4].SequenceEqual("BFST"u8))
            throw new InvalidDataException("A bounded BFST text resource is required.");
        var count = BinaryPrimitives.ReadUInt32LittleEndian(input[8..]);
        if (count > MaximumEntries || 12L + 4L * count > input.Length) throw new InvalidDataException("Truncated or oversized STR index.");
        var bytes = input.ToArray(); var entries = new GameTextEntry[(int)count]; long total = 0;
        for (var i = 0; i < entries.Length; i++)
        {
            var offset = 12L + BinaryPrimitives.ReadUInt32LittleEndian(input[(12 + i * 4)..]);
            if (offset < 12L + count * 4L || offset > bytes.Length - 4L) throw new InvalidDataException("STR entry points outside its data section.");
            var start = (int)offset; var length = BinaryPrimitives.ReadUInt16LittleEndian(input[(start + 1)..]);
            if (offset + 4L + length > bytes.Length || (total += length) > MaximumPreviewBytes)
                throw new InvalidDataException("Truncated or oversized STR preview payload.");
            var payload = input.Slice(start + 4, length); var unknown = 0;
            foreach (var glyph in payload) if (!KnownGlyph(glyph)) unknown++;
            entries[i] = new(i + 1, start, length, input[start], input[start + 3], DecodeOriginal(payload), unknown);
        }
        return new(bytes, BinaryPrimitives.ReadUInt32LittleEndian(input[4..]), entries);
    }
    public GameTextEntry Entry(int identifier) => identifier >= 1 && identifier <= Entries.Count ? Entries[identifier - 1]
        : throw new ArgumentOutOfRangeException(nameof(identifier), "Text identifiers start at one.");
    public byte[] RawGlyphs(int identifier) { var entry = Entry(identifier); return bytes.AsSpan(entry.Offset + 4, entry.Length).ToArray(); }
    public byte[] Snapshot() => bytes.ToArray();
    public string DiagnosticPreview(int identifier) => DecodeOriginal(RawGlyphs(identifier), true);
    public string ReleasePreview(int identifier) => DecodeOriginal(RawGlyphs(identifier), false);

    // Exact original Properties_MapTextsGameToASCII mapping. Unknown glyphs
    // are '*' with DEBUG_TEXTS off and '$XX*' in the configured source build.
    // Diagnostic mode exposes their byte IDs instead of guessing
    // another locale's alphabet or silently pretending to decode arbitrary Unicode.
    public static string DecodeOriginal(ReadOnlySpan<byte> glyphs, bool exposeUnknown = OriginalDebugPreview)
    {
        if (glyphs.Length > MaximumPreviewBytes) throw new ArgumentOutOfRangeException(nameof(glyphs));
        var result = new StringBuilder(glyphs.Length);
        foreach (var glyph in glyphs)
        {
            if (glyph is >= 0x9B and <= 0xB4) result.Append((char)(0xB4 - glyph + 'a'));
            else if (glyph is >= 0xBB and <= 0xD4) result.Append((char)(0xD4 - glyph + 'A'));
            else if (glyph is >= 2 and <= 10) result.Append((char)(glyph - 2 + '1'));
            else if (Special(glyph) is { } text) result.Append(text);
            else if (exposeUnknown) result.Append('$').Append(glyph.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)).Append('*');
            else result.Append('*');
        }
        return result.ToString();
    }
    private static bool KnownGlyph(byte value) => value is >= 0x9B and <= 0xB4 or >= 0xBB and <= 0xD4 or >= 2 and <= 10 || Special(value) is not null;
    private static string? Special(byte value) => value switch {
        0x01 => "*", 0x0B => "0", 0x0C => " ", 0x0F => ".", 0x10 => ",", 0x13 => "-", 0x15 => "...",
        0x16 => "œ", 0x1B => "û", 0x1D => "ù", 0x22 => "ô", 0x27 => "ï", 0x28 => "î", 0x2C => "ê", 0x2D => "é", 0x2E => "è", 0x2F => "ç",
        0x34 => "â", 0x36 => "à", 0x68 => "®", 0xE0 => "'", 0xE4 => "\"", 0xE5 => "!", 0xD6 => "?", 0xDA => ";", 0xDB => ":", 0xDC => "/", 0xDD => "+", 0xDE => ")", 0xDF => "(", _ => null };
}

public sealed record GameTextEntry(int Identifier, int Offset, int Length, byte Type, byte Reserved, string Preview, int UnknownGlyphCount);

internal static class GameResourceFiles
{
    public static byte[] ReadBounded(string path, int maximum)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > maximum) throw new InvalidDataException("Game resource exceeds the reader's bounded size.");
        var data = new byte[checked((int)stream.Length)]; stream.ReadExactly(data); return data;
    }
}
