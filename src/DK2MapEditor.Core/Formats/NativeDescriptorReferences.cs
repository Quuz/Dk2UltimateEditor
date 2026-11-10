using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Formats;

/// <summary>Shared, read-only KWD link-table and original local-fallback resolution.</summary>
internal static class NativeDescriptorReferences
{
    internal const int MaximumFileBytes = 64 * 1024 * 1024;
    internal const int MetadataOffset = 56;
    internal const int NameBytes = 128;
    internal const int DescriptionBytes = 2048;
    internal const int AuthorBytes = 128;
    internal const int MinimumMetadataEnd = MetadataOffset + NameBytes + DescriptionBytes + AuthorBytes;
    private static readonly NativeField ReferenceField = new("Reference", 0, 64, NativeValueKind.WesternAnsi);
    internal sealed record Link(uint Type, string Reference, string ExportPath, string? SourcePath);

    internal static MapDiagnostic? HeaderProblem(ReadOnlySpan<byte> descriptor, string path)
    {
        MapDiagnostic Error(string code, string message) => new(DiagnosticSeverity.Error, code, message, path);
        if (descriptor.Length < MetadataOffset)
            return Error("DK2E112", "The native KWD metadata header is truncated.");
        if (Read(descriptor, 0) != 0xDC || Read(descriptor, 4) != 4 || Read(descriptor, 12) != 0xDD || Read(descriptor, 16) != 28)
            return Error("DK2E110", "The KWD information header or format version is unsupported.");
        if (Read(descriptor, 48) != 0xDF)
            return Error("DK2E111", "The KWD parameters header is invalid.");
        var parametersEnd = MetadataOffset + (long)Read(descriptor, 52);
        if (parametersEnd < MinimumMetadataEnd || parametersEnd > descriptor.Length - 8)
            return Error("DK2E112", "The KWD name/description/author prefix is outside its bounded parameters block.");
        return null;
    }

    internal static IReadOnlyList<Link> Resolve(ReadOnlySpan<byte> descriptor, string descriptorPath)
    {
        if (HeaderProblem(descriptor, descriptorPath) is { } problem)
            throw new InvalidDataException(problem.Message);
        var parametersEnd = 56L + Read(descriptor, 52);
        var count = (int)(Read(descriptor, 20) & 0xFFFF);
        var start = parametersEnd + 8;
        if (parametersEnd > descriptor.Length - 8 || Read(descriptor, (int)parametersEnd) != 0xDE
            || count > 128 || start + count * 72L > descriptor.Length)
            throw new InvalidDataException("Invalid KWD reference table.");

        var fullPath = Path.GetFullPath(descriptorPath);
        var directory = Path.GetDirectoryName(fullPath)!;
        var links = new List<Link>();
        var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { fullPath };
        var exports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < count; index++)
        {
            var offset = checked((int)start + index * 72);
            var reference = ReferenceField.Read(descriptor.Slice(offset + 8, 64));
            if (string.IsNullOrWhiteSpace(reference)) continue;
            var relative = SafeRelativePath(reference);
            if (!Path.HasExtension(relative)) relative += ".kwd";
            var resolved = ResolveReference(directory, relative);
            // Missing links retain their original labels/order, even when repeated.
            if (resolved is not null && !sources.Add(resolved)) continue;
            links.Add(new(Read(descriptor, offset), reference, relative, resolved));
            if (resolved is not null) exports.Add(relative);
        }
        foreach (var (suffix, type) in new (string, uint)[]
                 { ("Map", 0x64), ("Players", 0xB4), ("Things", 0xBE), ("Triggers", 0xD2), ("Variables", 0xE6) })
        {
            // A renamed KWD may explicitly use another map's companion names.
            // Resolve each collection from its explicit source before considering
            // a stem-based fallback; loading both creates duplicate scenario IDs.
            // Shared GlobalVariables is not the map Variables collection.
            if (links.Any(link => link.Type == type && link.SourcePath is not null
                && (type != 0xE6 || !Path.GetFileName(link.ExportPath).Equals("GlobalVariables.kwd", StringComparison.OrdinalIgnoreCase)))) continue;
            var fallback = Path.Combine(directory, Path.GetFileNameWithoutExtension(fullPath) + suffix + ".kld");
            var export = Path.Combine("Data", "Editor", "Maps", Path.GetFileName(fallback));
            if (!File.Exists(fallback) || sources.Contains(fallback) || exports.Contains(export)) continue;
            sources.Add(fallback); exports.Add(export);
            links.Add(new(type, Path.GetFileName(fallback), export, fallback));
        }
        return links.AsReadOnly();
    }

    internal static string SafeRelativePath(string reference)
    {
        var parts = reference.Replace('\\', '/').Split('/');
        if (Path.IsPathRooted(reference) || parts.Any(part => part is "" or "." or ".."
            || part.Contains(':') || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new InvalidDataException($"Unsafe native file reference: {reference}");
        return Path.Combine(parts);
    }

    private static string? ResolveReference(string directory, string relative)
    {
        var current = new DirectoryInfo(directory);
        for (var depth = 0; current is not null && depth < 6; depth++, current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, relative);
            if (File.Exists(candidate)) return candidate;
        }
        var leaf = Path.Combine(directory, Path.GetFileName(relative));
        return File.Exists(leaf) ? leaf : null;
    }

    private static uint Read(ReadOnlySpan<byte> bytes, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
}
