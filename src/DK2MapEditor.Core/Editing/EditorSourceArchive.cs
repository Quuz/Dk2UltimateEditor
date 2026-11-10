using System.Collections.Immutable;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace DK2MapEditor.Core.Editing;

/// <summary>Original working bundle outside the DKM section stream. No paths from the archive are extracted.</summary>
public sealed class EditorSourceArchive
{
    private const long MaximumBytes = 512L * 1024 * 1024;
    private readonly byte[] bytes;
    public string ContentHash { get; }
    public ImmutableArray<ProjectConversionSourceFile> SourceFiles { get; }
    public ImmutableArray<ProjectConversionIssue> Issues { get; }
    public ImmutableDictionary<ushort, ushort> NativeEventIDs { get; }
    public ImmutableArray<byte> Statistics { get; }
    public ImmutableArray<byte> RetainedVariables { get; }
    private EditorSourceArchive(byte[] bytes, Manifest manifest, ImmutableArray<ProjectConversionSourceFile> files)
    {
        this.bytes = bytes; ContentHash = Hash(bytes); SourceFiles = files;
        Issues = manifest.Issues.ToImmutableArray(); NativeEventIDs = manifest.NativeEventIDs.ToImmutableDictionary();
        Statistics = manifest.Statistics.ToImmutableArray();
        RetainedVariables = (manifest.RetainedVariables ?? []).ToImmutableArray();
    }
    private sealed record FileEntry(string SourcePath, string ExportPath, string Entry, int Length, string SHA256);
    private sealed record Manifest(int Version, FileEntry[] Files, ProjectConversionIssue[] Issues,
        Dictionary<ushort, ushort> NativeEventIDs, byte[] Statistics, byte[]? RetainedVariables = null);
    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    public static EditorSourceArchive Capture(PreparedProjectConversion conversion) => CaptureCore(conversion, temporary: false);
    internal static EditorSourceArchive CaptureTemporary(PreparedProjectConversion conversion) => CaptureCore(conversion, temporary: true);
    private static EditorSourceArchive CaptureCore(PreparedProjectConversion conversion, bool temporary)
    {
        ArgumentNullException.ThrowIfNull(conversion);
        if (!conversion.IsCurrentForSave(temporary)) throw new InvalidOperationException("Prepare conversion again after source changes.");
        if (conversion.SourceFiles.Sum(file => (long)file.Bytes.Length) > MaximumBytes)
            throw new InvalidDataException("Source bundle exceeds the archive size limit.");
        var entries = conversion.SourceFiles.Select((file, index) => new FileEntry(file.SourcePath, file.ExportPath,
            $"files/{index:D5}.bin", file.Bytes.Length, Hash(file.Bytes.AsSpan()))).ToArray();
        var manifest = new Manifest(2, entries, conversion.Issues.ToArray(), conversion.NativeEventIDs.ToDictionary(), conversion.Statistics ?? [], conversion.Project.nativeRetainedVariables);
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Write(string name, ReadOnlySpan<byte> payload)
            {
                var entry = zip.CreateEntry(name, CompressionLevel.NoCompression);
                entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
                using var stream = entry.Open(); stream.Write(payload);
            }
            Write("manifest.json", JsonSerializer.SerializeToUtf8Bytes(manifest));
            for (var i = 0; i < entries.Length; i++) Write(entries[i].Entry, conversion.SourceFiles[i].Bytes.AsSpan());
        }
        return Parse(buffer.ToArray());
    }
    public static EditorSourceArchive Read(string path)
    {
        var info = new FileInfo(path);
        if (info.Length > MaximumBytes + 16 * 1024 * 1024) throw new InvalidDataException("Oversized source archive.");
        return Parse(File.ReadAllBytes(path));
    }
    private static EditorSourceArchive Parse(byte[] bytes)
    {
        using var input = new MemoryStream(bytes, writable: false);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read);
        var metadata = zip.GetEntry("manifest.json") ?? throw new InvalidDataException("Missing source manifest.");
        if (metadata.Length > 16 * 1024 * 1024) throw new InvalidDataException("Oversized source manifest.");
        using var stream = metadata.Open();
        var manifest = JsonSerializer.Deserialize<Manifest>(stream) ?? throw new InvalidDataException("Empty source manifest.");
        if (manifest.Version is not (1 or 2) || manifest.Version == 2 && manifest.RetainedVariables is null || manifest.Version == 1 && manifest.RetainedVariables is { Length: > 0 } || manifest.Files is null || manifest.Issues is null || manifest.NativeEventIDs is null || manifest.Statistics is null
            || manifest.Files.Length > 10000 || zip.Entries.Count != manifest.Files.Length + 1)
            throw new InvalidDataException("Unsupported source archive structure.");
        long total = 0; var result = ImmutableArray.CreateBuilder<ProjectConversionSourceFile>();
        for (var i = 0; i < manifest.Files.Length; i++)
        {
            var file = manifest.Files[i];
            if (file is null || file.Entry != $"files/{i:D5}.bin" || file.Length < 0 || file.SourcePath is null || file.ExportPath is null)
                throw new InvalidDataException("Invalid source file entry.");
            var entry = zip.GetEntry(file.Entry) ?? throw new InvalidDataException("Missing source file.");
            total = checked(total + entry.Length);
            if (entry.Length != file.Length || total > MaximumBytes) throw new InvalidDataException("Source file length mismatch.");
            using var source = entry.Open(); using var output = new MemoryStream(file.Length); source.CopyTo(output);
            var payload = output.ToArray();
            if (payload.Length != file.Length || Hash(payload) != file.SHA256) throw new InvalidDataException("Source file hash mismatch.");
            result.Add(new(file.SourcePath, file.ExportPath, ImmutableArray.CreateRange(payload)));
        }
        if (zip.Entries.Select(entry => entry.FullName).Distinct(StringComparer.Ordinal).Count() != zip.Entries.Count)
            throw new InvalidDataException("Duplicate source archive entry.");
        var files = result.ToImmutable();
        NativeMapDocument.ValidateRetainedNativeVariables(manifest.RetainedVariables ?? [], files);
        return new(bytes, manifest, files);
    }
    internal string Publish(string projectPath, Func<string, Stream>? createOutput)
    {
        var destination = projectPath + ".native-source-" + ContentHash + ".zip";
        if (File.Exists(destination))
        {
            if (Read(destination).ContentHash != ContentHash) throw new InvalidDataException("Existing source archive is corrupt.");
            return destination;
        }
        var temporary = Path.Combine(Path.GetDirectoryName(projectPath)!, ".dk2-source-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = createOutput is null ? new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None) : createOutput(temporary))
            {
                stream.Write(bytes); stream.Flush();
                if (stream is FileStream file) file.Flush(flushToDisk: true);
            }
            if (Read(temporary).ContentHash != ContentHash) throw new InvalidDataException("Incomplete source archive write.");
            try { File.Move(temporary, destination); }
            catch (IOException) when (File.Exists(destination))
            { if (Read(destination).ContentHash != ContentHash) throw; }
            // Once published, immutable archives are retained even if a later
            // project write fails. Never remove a bundle another save may use.
            return destination;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal static IReadOnlyList<EditorSourceArchive> Discover(string projectPath)
    {
        var directory = Path.GetDirectoryName(projectPath)!; var prefix = Path.GetFileName(projectPath) + ".native-source-";
        if (!Directory.Exists(directory)) return [];
        var result = new List<EditorSourceArchive>();
        foreach (var path in Directory.EnumerateFiles(directory).Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(path);
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
            var expected = name[prefix.Length..^4];
            if (expected.Length != 64 || expected.Any(character => !Uri.IsHexDigit(character))) continue;
            var archive = Read(path);
            if (!archive.ContentHash.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Source companion hash mismatch.");
            result.Add(archive);
        }
        return result.AsReadOnly();
    }
}
