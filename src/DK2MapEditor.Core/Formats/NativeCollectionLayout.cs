using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Formats;

/// <summary>Shared native collection framing. A readable frame is not evidence
/// that a record has a verified semantic schema.</summary>
internal static class NativeCollectionLayout
{
    internal const string OpaqueCategory = "Opaque.NativeCollection";
    internal static bool IsCollection(uint signature) => signature is 0xB4 or 0xBE or 0xD2 or 0xE6;
    internal static int HeaderLength(uint signature) => signature == 0xD2 ? 60 : 56;
    private static uint Word(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));

    internal static bool VerifiedHeader(byte[] bytes, string path)
    {
        if (bytes.Length < 4) throw new InvalidDataException($"Truncated collection: {path}");
        var signature = Word(bytes, 0); var start = HeaderLength(signature);
        if (bytes.Length < start) throw new InvalidDataException($"Truncated collection: {path}");
        return IsCollection(signature) && Word(bytes, 4) == 4 && Word(bytes, 12) == signature + 1
            && Word(bytes, 16) == (signature == 0xD2 ? 32u : 28u) && Word(bytes, start - 8) == signature + 2;
    }

    internal static bool VerifiedRecord(uint collection, uint signature, int length) => collection switch
    {
        0xB4 => length == 205,
        0xE6 => length == 16,
        0xD2 => signature is 0xD5 or 0xD6 && length == 24,
        0xBE => length == (signature switch
        {
            0xC2 => 32, 0xC3 => 24, 0xC4 => 28, 0xC5 => 64, 0xC6 => 32,
            0xC7 => 40, 0xC8 => 36, 0xC9 => 563, 0xCB => 44, 0xCC => 28, _ => -1
        }),
        _ => false
    };

    internal static IEnumerable<(int Offset, uint Signature, int Length)> Entries(byte[] bytes, string path)
    {
        if (!VerifiedHeader(bytes, path)) throw new InvalidDataException($"The native collection header has no verified editing layout: {path}");
        var collection = Word(bytes, 0); var offset = HeaderLength(collection);
        var count = (long)Word(bytes, 20) + (collection == 0xD2 ? Word(bytes, 24) : 0);
        if (count > 1_000_000) throw new InvalidDataException("Invalid native record count.");
        for (var index = 0; index < count; index++)
        {
            if (offset > bytes.Length - 8) throw new InvalidDataException($"Truncated record {index}: {path}");
            var signature = Word(bytes, offset);
            var length = collection switch { 0xB4 => 205, 0xE6 => 16, _ => checked((int)Word(bytes, offset + 4) + 8) };
            if (length < 8 || length > bytes.Length - offset) throw new InvalidDataException($"Invalid record size at {offset}: {path}");
            yield return (offset, signature, length);
            offset += length;
        }
    }

    internal static MapDiagnostic Unverified(string path, int offset, int length) => new(DiagnosticSeverity.Warning,
        "DK2N047", $"Native collection data at 0x{offset:X} ({length} bytes) has no verified header or record layout; raw copying preserves it, semantic editing/conversion is refused.", path);

    internal static bool VerifiedScriptPartition(byte[] bytes, string path)
    {
        if (Word(bytes, 0) != 0xD2) return true;
        var conditions = Word(bytes, 20); var index = 0L; var verified = true;
        // Read all counted frames so a wrong category does not conceal later
        // truncation. Opaque suffixes remain outside this declared partition.
        foreach (var entry in Entries(bytes, path))
            verified &= entry.Signature == (index++ < conditions ? 0xD5u : 0xD6u);
        return verified;
    }

    internal static IReadOnlyList<MapDiagnostic> Inspect(byte[] bytes, string path)
    {
        if (!VerifiedHeader(bytes, path) || !VerifiedScriptPartition(bytes, path)) return [Unverified(path, 0, bytes.Length)];
        var collection = Word(bytes, 0); var result = new List<MapDiagnostic>();
        foreach (var entry in Entries(bytes, path))
            if (!VerifiedRecord(collection, entry.Signature, entry.Length)) result.Add(Unverified(path, entry.Offset, entry.Length));
        return result;
    }
}
