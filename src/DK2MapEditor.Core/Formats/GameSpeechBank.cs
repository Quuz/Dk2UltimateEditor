using System.Buffers.Binary;
using System.Text;

namespace DK2MapEditor.Core.Formats;

/// <summary>Immutable, bounded SDT snapshot. No game files or temporary playback files are written.</summary>
public sealed class GameSpeechBank
{
    public const int MaximumFileBytes = 64 * 1024 * 1024;
    public const int MaximumEntries = 65536;
    private readonly byte[] bytes;
    public IReadOnlyList<GameSpeechEntry> Entries { get; }
    private GameSpeechBank(byte[] bytes, GameSpeechEntry[] entries) { this.bytes = bytes; Entries = Array.AsReadOnly(entries); }
    public static GameSpeechBank Load(string path) => Parse(GameResourceFiles.ReadBounded(path, MaximumFileBytes));
    public static GameSpeechBank Parse(ReadOnlySpan<byte> input)
    {
        if (input.Length < 4 || input.Length > MaximumFileBytes) throw new InvalidDataException("A bounded SDT sound bank is required.");
        var count = BinaryPrimitives.ReadUInt32LittleEndian(input);
        if (count > MaximumEntries || 4L + count * 4L > input.Length) throw new InvalidDataException("Truncated or oversized SDT index.");
        var entries = new GameSpeechEntry[(int)count];
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); var encoding = Encoding.GetEncoding(1252);
        for (var i = 0; i < entries.Length; i++)
        {
            var offset = (long)BinaryPrimitives.ReadUInt32LittleEndian(input[(4 + i * 4)..]);
            if (offset < 4L + count * 4L || offset > input.Length - 40L) throw new InvalidDataException("SDT entry points outside its data section.");
            var start = (int)offset; var size = BinaryPrimitives.ReadUInt32LittleEndian(input[(start + 4)..]);
            if (offset + 40L + size > input.Length) throw new InvalidDataException("Truncated SDT audio payload.");
            var name = input.Slice(start + 8, 32); var end = name.IndexOf((byte)0); if (end >= 0) name = name[..end];
            entries[i] = new(i + 1, start, checked((int)size), BinaryPrimitives.ReadUInt32LittleEndian(input[start..]), encoding.GetString(name));
        }
        return new(input.ToArray(), entries);
    }
    public GameSpeechEntry Entry(int identifier) => identifier >= 1 && identifier <= Entries.Count ? Entries[identifier - 1]
        : throw new ArgumentOutOfRangeException(nameof(identifier), "Sound identifiers start at one; zero means no sound.");
    public byte[] AudioPayload(int identifier)
    {
        var entry = Entry(identifier);
        if (!entry.PlayableHeader) throw new InvalidDataException("The original sound reader requires a 40-byte sound header.");
        return bytes.AsSpan(entry.Offset + 40, entry.Length).ToArray();
    }
    public byte[] Snapshot() => bytes.ToArray();
}

public sealed record GameSpeechEntry(int Identifier, int Offset, int Length, uint HeaderSize, string Name)
{
    public bool PlayableHeader => HeaderSize == 40;
}
