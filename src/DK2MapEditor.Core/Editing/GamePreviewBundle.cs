namespace DK2MapEditor.Core.Editing;

public sealed record GamePreviewOutput(string LevelName, string DescriptorPath, IReadOnlyList<string> CreatedFiles);

/// <summary>An official seven-file map with independent names for its game files.
/// Publishing uses CreateNew; an installed map or shared definition is never replaced.</summary>
public sealed class GamePreviewBundle
{
    private readonly IReadOnlyList<(string Name, byte[] Data)> files;
    private readonly IReadOnlyList<(string Path, byte[] Hash)> shared;
    public string LevelName { get; }
    public int FileCount => files.Count;

    internal GamePreviewBundle(string levelName, IReadOnlyList<(string Name, byte[] Data)> files,
        IReadOnlyList<(string Path, byte[] Hash)>? shared = null)
    { LevelName = levelName; this.files = files; this.shared = shared ?? []; }

    public GamePreviewOutput Install(string gameRoot)
    {
        var maps = Path.Combine(Path.GetFullPath(gameRoot), "Data", "Editor", "Maps");
        if (!Directory.Exists(maps)) throw new DirectoryNotFoundException("Choose a Dungeon Keeper 2 installation containing Data/Editor/Maps.");
        foreach (var dependency in shared)
        {
            var path = Path.Combine(Path.GetFullPath(gameRoot), dependency.Path);
            if (!File.Exists(path) || !System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)).AsSpan().SequenceEqual(dependency.Hash))
                throw new InvalidDataException("The installation's shared game data differs from the map basis: " + dependency.Path);
        }
        var outputs = files.Select(file => Path.Combine(maps, file.Name)).ToArray();
        if (outputs.Any(path => File.Exists(path) || Directory.Exists(path)))
            throw new IOException("A preview output already exists. Prepare a new preview instead of replacing it.");
        var created = new List<string>();
        try
        {
            for (var index = 0; index < files.Count; index++)
            {
                using var stream = new FileStream(outputs[index], FileMode.CreateNew, FileAccess.Write, FileShare.None);
                created.Add(outputs[index]);
                stream.Write(files[index].Data);
                stream.Flush(flushToDisk: true);
            }
        }
        catch (Exception failure)
        {
            var failures = new List<Exception> { failure };
            foreach (var path in created.AsEnumerable().Reverse())
                try { File.Delete(path); }
                catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException) { failures.Add(cleanup); }
            if (failures.Count > 1) throw new AggregateException("Preview installation failed; some private files could not be removed.", failures);
            throw;
        }
        return new(LevelName, outputs[^1], created.AsReadOnly());
    }
}

public sealed partial class NativeMapDocument
{
    /// <summary>Captures current native bytes without editing the document, its
    /// history, or its source files. Descriptor links point only to this preview.</summary>
    public GamePreviewBundle PrepareGamePreview(string? levelName = null)
    {
        if (IsLegacyProject || MainFile.Signature != 0xDC)
            throw new InvalidOperationException("Compile the editor project before preparing a game preview.");
        if (transactionOpen || activePenStroke is not null)
            throw new InvalidOperationException("Finish the active edit before preparing a game preview.");
        if (MissingReferences.Count != 0)
            throw new InvalidDataException("A game preview requires all native map references to be loaded: " + string.Join(", ", MissingReferences));
        var exported = ExportFiles().Where(file => Path.GetExtension(file.path).Equals(".kwd", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(file.path).Equals(".kld", StringComparison.OrdinalIgnoreCase)).ToArray();
        var level = levelName ?? "DK2Preview_" + Guid.NewGuid().ToString("N")[..16];
        if (string.IsNullOrWhiteSpace(level) || level.Length > 32 || level.Any(character => character > 127 || Path.GetInvalidFileNameChars().Contains(character)) || level.Contains('\\') || level.Contains('/') || level is "." or "..")
            throw new ArgumentException("Use a native level name with 1–32 ASCII characters.", nameof(levelName));
        var mainPath = SafeRelativePath(MainFile.ExportPath);
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var payloads = new List<(string Name, byte[] Data)>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var shared = new List<(string Path, byte[] Hash)>();
        var suffixes = new Dictionary<uint, string> { [0x64] = "Map", [0xB4] = "Players", [0xBE] = "Things", [0xD2] = "Triggers", [0xE6] = "Variables" };
        byte[]? descriptor = null;
        for (var index = 0; index < exported.Length; index++)
        {
            var (path, data) = exported[index];
            var safe = SafeRelativePath(path);
            var isMain = safe.Equals(mainPath, StringComparison.OrdinalIgnoreCase);
            if (!safe.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                shared.Add((safe, System.Security.Cryptography.SHA256.HashData(data)));
                paths.Add(safe, safe); continue;
            }
            var signature = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data);
            var name = isMain ? level + ".kwd"
                : safe.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && suffixes.TryGetValue(signature, out var suffix)
                    ? level + suffix + ".kld"
                    : Path.GetFileName(safe).EndsWith("Globals.kld", StringComparison.OrdinalIgnoreCase)
                        ? level + "Globals.kld" : level + "_" + index.ToString("D3") + Path.GetExtension(safe);
            if (!names.Add(name) || !paths.TryAdd(safe, Path.Combine("Data", "Editor", "Maps", name)))
                throw new InvalidDataException("Ambiguous native preview files.");
            if (isMain) descriptor = (byte[])data.Clone();
            else payloads.Add((name, (byte[])data.Clone()));
        }
        if (descriptor is null) throw new InvalidDataException("The native descriptor is missing.");
        // The shared reader validates bounds/signatures before this fixed-width rewrite.
        _ = Formats.NativeDescriptorReferences.Resolve(descriptor, MainFile.SourcePath);
        var start = checked(56 + (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(52)) + 8);
        var count = (int)(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(20)) & 0xFFFF);
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * 72 + 8;
            var reference = NativeReference.Read(descriptor.AsSpan(offset, 64));
            if (string.IsNullOrWhiteSpace(reference)) continue;
            var safe = SafeRelativePath(reference);
            if (!Path.HasExtension(safe)) safe += ".kwd";
            if (!paths.TryGetValue(safe, out var replacement))
                throw new InvalidDataException("The preview has an unresolved descriptor link: " + reference);
            NativeReference.Encode(replacement.Replace(Path.DirectorySeparatorChar, '\\')).CopyTo(descriptor, offset);
        }
        // Publish the descriptor last: the game never sees a successful preview
        // descriptor before its referenced files have been written and flushed.
        payloads.Add((level + ".kwd", descriptor));
        if (payloads.Count != 7 || !names.Contains(level + "Globals.kld"))
            throw new InvalidDataException("Compile the map to the official seven-file layout before game publication.");
        return new(level, payloads.AsReadOnly(), shared.AsReadOnly());
    }
}
