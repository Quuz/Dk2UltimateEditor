using System.Buffers.Binary;
using System.Text;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Formats;

public sealed class Dk2FileInspector
{
    private const uint GameDescriptorHeader = 0x000000DC;
    private const uint GameMapHeader = 0x00000064;
    private const uint GameMapInfoHeader = 0x00000065;
    private const uint GameMapDefinitionHeader = 0x00000066;
    private const uint GamePlayersHeader = 0x000000B4;
    private const uint GameThingsHeader = 0x000000BE;
    private const uint GameTriggersHeader = 0x000000D2;
    private const uint GameVariablesHeader = 0x000000E6;
    private const uint EditorMapHeader = 0x4650414D; // MAPF, little-endian.
    private const uint EditorAreaHeader = 0x41455241; // AREA, little-endian.

    private const int GenericHeaderSize = 12;
    private const int GameMapHeaderSize = 36;

    private static readonly ComponentDefinition[] RequiredComponents =
    [
        new("Map.kld", GameMapHeader),
        new("Players.kld", GamePlayersHeader),
        new("Things.kld", GameThingsHeader),
        new("Triggers.kld", GameTriggersHeader),
        new("Variables.kld", GameVariablesHeader)
    ];

    public Dk2FileInspection Inspect(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = Path.GetFullPath(filePath);
        var diagnostics = new List<MapDiagnostic>();

        if (!File.Exists(fullPath))
        {
            diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E001", "The file does not exist.", fullPath));
            return Unknown(fullPath, diagnostics);
        }

        try
        {
            if (Path.GetExtension(fullPath).Equals(".map", StringComparison.OrdinalIgnoreCase))
            {
                var project = TextMapFile.Read(fullPath); var tiles = project.ReadTiles();
                var info = project.Sections.Single(section => section.Signature == Editing.NativeMapDocument.LegacyInfo);
                var name = Editing.LegacyProjectSchemas.Fields("MAPPROPERTIES").Single(field => field.Name == "Name").Read(info.Payload.Span);
                return new(fullPath, Dk2FileKind.TextMap, new FileInfo(fullPath).Length, 0, 1, name, null, tiles.Width, tiles.Height, diagnostics);
            }
            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length < GenericHeaderSize)
            {
                diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E002", "The file is too small to contain a DK2 header.", fullPath));
                return Unknown(fullPath, diagnostics, stream.Length);
            }

            Span<byte> prefix = stackalloc byte[GameMapHeaderSize];
            var prefixLength = stream.Read(prefix);
            var header = ReadUInt32(prefix, 0);
            var version = ReadInt32(prefix, 4);

            return header switch
            {
                GameDescriptorHeader => InspectDescriptor(fullPath, stream, prefix[..prefixLength], version, diagnostics),
                GameMapHeader => InspectMapTiles(fullPath, stream.Length, prefix[..prefixLength], version, diagnostics),
                EditorMapHeader => InspectLegacyEditorFile(fullPath, stream.Length, header, version, Dk2FileKind.EditorMap, prefix, diagnostics),
                EditorAreaHeader => InspectLegacyEditorFile(fullPath, stream.Length, header, version, Dk2FileKind.EditorArea, prefix, diagnostics),
                _ => Unsupported(fullPath, stream.Length, header, version, diagnostics)
            };
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or OverflowException)
        {
            diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E003", exception.Message, fullPath));
            return Unknown(fullPath, diagnostics);
        }
        catch (UnauthorizedAccessException exception)
        {
            diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E004", exception.Message, fullPath));
            return Unknown(fullPath, diagnostics);
        }
    }

    private static Dk2FileInspection InspectDescriptor(
        string filePath,
        FileStream stream,
        ReadOnlySpan<byte> prefix,
        int version,
        List<MapDiagnostic> diagnostics)
    {
        ValidateVersion(version, 4, filePath, diagnostics);
        ValidateNativeDeclaredSize(stream.Length, ReadUInt32(prefix, 8), filePath, diagnostics);

        string? name = null;
        string? author = null;
        int? width = null;
        int? height = null;
        if (stream.Length > NativeDescriptorReferences.MaximumFileBytes)
            diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E134", "Native file size is outside safe bounds.", filePath));
        else
        {
            var descriptor = new byte[(int)stream.Length];
            stream.Position = 0;
            stream.ReadExactly(descriptor);
            // Available text remains useful when reporting a broken header or
            // reference table. Displaying a bounded physical prefix does not
            // approve that file for editing or follow its invalid references.
            if (descriptor.Length >= NativeDescriptorReferences.MinimumMetadataEnd)
            {
                name = ReadUtf16String(descriptor, NativeDescriptorReferences.MetadataOffset, NativeDescriptorReferences.NameBytes);
                author = ReadUtf16String(descriptor, NativeDescriptorReferences.MetadataOffset + NativeDescriptorReferences.NameBytes + NativeDescriptorReferences.DescriptionBytes, NativeDescriptorReferences.AuthorBytes);
            }
            var problem = NativeDescriptorReferences.HeaderProblem(descriptor, filePath);
            if (problem is not null) diagnostics.Add(problem);
            try
            {
                var links = NativeDescriptorReferences.Resolve(descriptor, filePath);
                ValidateCompanionFiles(filePath, links, diagnostics, ref width, ref height);
            }
            catch (InvalidDataException exception)
            {
                diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E113", exception.Message, filePath));
            }
        }

        return new(
            filePath,
            Dk2FileKind.GameMapDescriptor,
            stream.Length,
            GameDescriptorHeader,
            version,
            NullIfWhiteSpace(name),
            NullIfWhiteSpace(author),
            width,
            height,
            diagnostics);
    }

    private static Dk2FileInspection InspectMapTiles(
        string filePath,
        long fileSize,
        ReadOnlySpan<byte> prefix,
        int version,
        List<MapDiagnostic> diagnostics)
    {
        ValidateVersion(version, 4, filePath, diagnostics);
        ValidateNativeDeclaredSize(fileSize, ReadUInt32(prefix, 8), filePath, diagnostics);

        int? width = null;
        int? height = null;
        if (prefix.Length < GameMapHeaderSize)
        {
            diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E120", "The map tile header is truncated.", filePath));
        }
        else
        {
            if (ReadUInt32(prefix, 12) != GameMapInfoHeader || ReadUInt32(prefix, 16) != 8 || ReadUInt32(prefix, 28) != GameMapDefinitionHeader)
            {
                diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E121", "Invalid map tile section header.", filePath));
            }

            var nativeWidth = ReadUInt32(prefix, 20);
            var nativeHeight = ReadUInt32(prefix, 24);
            width = nativeWidth <= int.MaxValue ? (int)nativeWidth : null;
            height = nativeHeight <= int.MaxValue ? (int)nativeHeight : null;
            var definitionSize = ReadUInt32(prefix, 32);
            var expectedDefinitionSize = nativeWidth <= 1024 && nativeHeight <= 1024
                ? (long)nativeWidth * nativeHeight * 4 : 0;

            if (width is null or <= 0 or > 1024 || height is null or <= 0 or > 1024)
            {
                diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E122", $"Suspicious map dimensions: {width} x {height}.", filePath));
            }
            else if (fileSize < GameMapHeaderSize + expectedDefinitionSize)
            {
                diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E123", "The tile data is shorter than the declared map dimensions.", filePath));
            }
            else if (definitionSize != expectedDefinitionSize)
            {
                diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2W124", $"Stored tile size {definitionSize} differs from dimension-derived size {expectedDefinitionSize}; the original loader uses actual available tile bytes.", filePath));
            }
        }

        return new(
            filePath,
            Dk2FileKind.GameMapTiles,
            fileSize,
            GameMapHeader,
            version,
            Path.GetFileNameWithoutExtension(filePath),
            null,
            width,
            height,
            diagnostics);
    }

    private static Dk2FileInspection InspectLegacyEditorFile(
        string filePath,
        long fileSize,
        uint header,
        int version,
        Dk2FileKind kind,
        ReadOnlySpan<byte> prefix,
        List<MapDiagnostic> diagnostics)
    {
        ValidateVersion(version, 1, filePath, diagnostics);
        // Editor HEADER.Size is the payload length, unlike native KWD/KLD sizes.
        ValidateDeclaredSize(fileSize - GenericHeaderSize, ReadUInt32(prefix, 8), filePath, diagnostics);

        var project = Editing.NativeMapDocument.InspectLegacyProject(filePath, LegacySectionFile.Read(filePath));
        diagnostics.AddRange(project.Diagnostics);

        return new(
            filePath,
            kind,
            fileSize,
            header,
            version,
            Path.GetFileNameWithoutExtension(filePath),
            null,
            project.Width,
            project.Height,
            diagnostics);
    }

    private static void ValidateCompanionFiles(
        string descriptorPath,
        IReadOnlyList<NativeDescriptorReferences.Link> links,
        List<MapDiagnostic> diagnostics,
        ref int? width,
        ref int? height)
    {
        var directory = Path.GetDirectoryName(descriptorPath) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(descriptorPath);

        Span<byte> header = stackalloc byte[GameMapHeaderSize];
        // Validate exactly the same explicit files and fallback precedence as
        // editing, including shared definitions. One bad file must not prevent
        // dimensions/diagnostics being collected from the remaining files.
        foreach (var link in links)
        {
            var componentPath = link.SourcePath ?? Path.Combine(directory, link.ExportPath);
            if (link.SourcePath is null)
            {
                diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2W130", $"Missing referenced file: {link.Reference}.", componentPath));
                continue;
            }
            try
            {
                using var stream = new FileStream(componentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                header.Clear();
                var bytesRead = ReadAtMost(stream, header);
                if (bytesRead < GenericHeaderSize)
                {
                    diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E131", "Companion file header is truncated.", componentPath));
                    continue;
                }
                if (stream.Length > NativeDescriptorReferences.MaximumFileBytes)
                {
                    diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E134", "Native file size is outside safe bounds.", componentPath));
                    continue;
                }

                var signature = ReadUInt32(header, 0);
                // Type 0 is the untyped/packed Globals.kld reference used by
                // original generated maps, not an expected zero file signature.
                if (link.Type != 0 && signature != link.Type)
                {
                    diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E132", $"Unexpected companion signature; KWD reference expects 0x{link.Type:X}.", componentPath));
                    continue;
                }

                if (signature == GameMapHeader)
                {
                    var mapResult = InspectMapTiles(componentPath, stream.Length, header[..bytesRead], ReadInt32(header, 4), []);
                    if (width is null && height is null) { width = mapResult.Width; height = mapResult.Height; }
                    diagnostics.AddRange(mapResult.Diagnostics);
                }
                else
                {
                    ValidateVersion(ReadInt32(header, 4), 4, componentPath, diagnostics);
                    ValidateNativeDeclaredSize(stream.Length, ReadUInt32(header, 8), componentPath, diagnostics);
                    if (NativeOpaqueFile.IsUnknown(signature) && ReadInt32(header, 4) == 4)
                        diagnostics.Add(NativeOpaqueFile.Diagnostic(componentPath));
                    if ((NativeCollectionLayout.IsCollection(signature) || Editing.NativeMapDocument.NativeDefinitionSignature(signature)) && ReadInt32(header, 4) == 4)
                    {
                        stream.Position = 0;
                        var collection = new byte[checked((int)stream.Length)]; stream.ReadExactly(collection);
                        if (NativeCollectionLayout.IsCollection(signature)) diagnostics.AddRange(NativeCollectionLayout.Inspect(collection, componentPath));
                        else diagnostics.AddRange(Editing.NativeMapDocument.InspectNativeDefinitions(new Editing.NativeFile(componentPath, componentPath, collection)));
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or OverflowException)
            {
                diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E133", exception.Message, componentPath));
            }
        }
        foreach (var component in RequiredComponents.Where(component => !links.Any(link => link.Type == component.Header)))
        {
            var componentPath = Path.Combine(directory, stem + component.Suffix);
            diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2W130", $"Missing companion file: {Path.GetFileName(componentPath)}.", componentPath));
        }
    }

    private static void ValidateVersion(int actual, int expected, string path, List<MapDiagnostic> diagnostics)
    {
        if (actual != expected)
        {
            diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E140", $"Unsupported format version {actual}; expected {expected}.", path));
        }
    }

    private static void ValidateDeclaredSize(long actual, uint declared, string path, List<MapDiagnostic> diagnostics)
    {
        if (declared != actual)
        {
            diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E141", $"Declared size {declared:N0} does not match actual size {actual:N0}.", path));
        }
    }

    private static void ValidateNativeDeclaredSize(long actual, uint declared, string path, List<MapDiagnostic> diagnostics)
    {
        // Native Game_Extract* reads actual file bytes rather than trusting the
        // generic size as an exact end marker. Match the editing loader: retain
        // undersized metadata/opaque extensions but never hide missing bytes.
        // Editor DKM/DKZ HEADER.Size is a different, strict payload contract.
        if (declared > actual) ValidateDeclaredSize(actual, declared, path, diagnostics);
        else if (declared < actual)
            diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2W141", $"Stored native size {declared:N0} is smaller than actual size {actual:N0}; actual payload bounds apply and extra bytes are preserved.", path));
    }

    private static Dk2FileInspection Unsupported(
        string path,
        long size,
        uint header,
        int version,
        List<MapDiagnostic> diagnostics)
    {
        diagnostics.Add(new(DiagnosticSeverity.Error, "DK2E005", $"Unknown DK2 file signature 0x{header:X8}.", path));
        return new(path, Dk2FileKind.Unknown, size, header, version, null, null, null, null, diagnostics);
    }

    private static Dk2FileInspection Unknown(string path, List<MapDiagnostic> diagnostics, long size = 0) =>
        new(path, Dk2FileKind.Unknown, size, 0, 0, null, null, null, null, diagnostics);

    private static uint ReadUInt32(ReadOnlySpan<byte> source, int offset) =>
        source.Length >= offset + sizeof(uint)
            ? BinaryPrimitives.ReadUInt32LittleEndian(source[offset..])
            : 0;

    private static int ReadInt32(ReadOnlySpan<byte> source, int offset) =>
        source.Length >= offset + sizeof(int)
            ? BinaryPrimitives.ReadInt32LittleEndian(source[offset..])
            : 0;

    private static int ReadAtMost(Stream stream, Span<byte> target)
    {
        var total = 0;
        while (total < target.Length)
        {
            var count = stream.Read(target[total..]);
            if (count == 0)
            {
                break;
            }

            total += count;
        }

        return total;
    }

    private static string ReadUtf16String(ReadOnlySpan<byte> source, int offset, int byteCount)
    {
        if (offset < 0 || byteCount < 0 || source.Length < offset + byteCount)
        {
            return string.Empty;
        }

        var value = Encoding.Unicode.GetString(source.Slice(offset, byteCount));
        var terminator = value.IndexOf('\0');
        return (terminator >= 0 ? value[..terminator] : value).Trim();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed record ComponentDefinition(string Suffix, uint Header);
}
