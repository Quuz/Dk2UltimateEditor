using System.Buffers.Binary;
using System.Text;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class InspectorReferenceChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static uint Read(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
    private static void Write(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2InspectorReferences-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            CheckStaleCompanions(Path.Combine(directory, "stale"));
            CheckCustomReferences(Path.Combine(directory, "custom"));
            CheckTableBounds(Path.Combine(directory, "tables"));
            CheckSearchDepth(Path.Combine(directory, "depth"));
            CheckFileBounds(Path.Combine(directory, "bounds"));
            CheckUntypedReferences(Path.Combine(directory, "untyped"));
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void CheckStaleCompanions(string directory)
    {
        var basis = NativeMapDocument.Create(7, 6, "Resolved"); var entry = basis.SaveCopy(directory);
        var copied = Path.Combine(directory, "copied"); Directory.CreateDirectory(copied);
        var copy = Path.Combine(copied, Path.GetFileName(entry)); File.Copy(entry, copy);
        foreach (var file in basis.Files.Where(file => file != basis.MainFile))
            File.WriteAllBytes(Path.Combine(copied, Path.GetFileName(file.ExportPath)), new byte[12]);
        var inspection = new Dk2FileInspector().Inspect(copy);
        Expect(inspection.IsValid && inspection.Width == 7 && inspection.Height == 6, "Inspector examined stale local companions instead of explicit parent-root references.");
        var loaded = NativeMapDocument.Load(copy);
        Expect(loaded.Files.Count == basis.Files.Count && !loaded.HasChanges && !loaded.CanUndo, "Shared resolver changed loader precedence/history.");
        var inputs = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        // Corrupt every actual referenced file independently; never follow the stale local one.
        foreach (var file in basis.Files.Where(file => file != basis.MainFile))
        {
            var path = Path.Combine(directory, file.ExportPath);
            var bytes = File.ReadAllBytes(path); var bad = bytes.ToArray(); Write(bad, 4, 99); File.WriteAllBytes(path, bad);
            var result = new Dk2FileInspector().Inspect(copy);
            Expect(result.Diagnostics.Any(item => item.Code == "DK2E140" && item.FilePath == path), "Actual explicit component version was not diagnosed at its resolved path.");
            Expect(result.Width == 7 && result.Height == 6, "One bad component discarded valid map dimensions.");
            File.WriteAllBytes(path, bytes);
        }
        foreach (var (path, bytes) in inputs) Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "Inspection changed source bytes.");
    }

    private static void CheckCustomReferences(string directory)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var codec = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        var basis = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis"));
        var descriptor = basis.MainFile.Data.ToArray(); var start = checked(56 + (int)Read(descriptor, 52) + 8);
        var count = (int)(Read(descriptor, 20) & 0xFFFF); var paths = new Dictionary<uint, string>();
        var expectedMissing = new List<string>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * 72; var type = Read(descriptor, offset);
            var original = codec.GetString(descriptor, offset + 8, 64).Split('\0')[0].Replace('\\', Path.DirectorySeparatorChar);
            if (!Path.HasExtension(original)) original += ".kwd";
            var relative = "références/¡éßœ€_" + index; // No extension: original .kwd inference, even for per-map data.
            var path = Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar) + ".kwd");
            var source = basis.Files.SingleOrDefault(file => file.ExportPath.Equals(original, StringComparison.OrdinalIgnoreCase));
            if (source is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, source.Data.ToArray()); paths[type] = path;
            }
            else expectedMissing.Add(relative);
            descriptor.AsSpan(offset + 8, 64).Clear(); codec.GetBytes(relative).CopyTo(descriptor, offset + 8); descriptor[offset + 71] = 0xA1;
        }
        var entry = Path.Combine(directory, "Custom.kwd"); File.WriteAllBytes(entry, descriptor);
        var inputs = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        var inspector = new Dk2FileInspector(); var loaded = NativeMapDocument.Load(entry); var valid = inspector.Inspect(entry);
        Expect(valid.IsValid && valid.Width == loaded.Width && valid.Height == loaded.Height, "Custom CP1252 references did not provide actual map dimensions.");
        Expect(valid.Diagnostics.Count(item => item.Code == "DK2W130") == expectedMissing.Count, "Custom companion names incorrectly reported missing or missing definitions hidden.");
        Expect(loaded.MissingReferences.SequenceEqual(expectedMissing), "Shared resolver changed missing reference labels/order.");
        // Shared definition failures must not erase usable map/metadata results.
        var definition = paths[0x6E]; var originalDefinition = File.ReadAllBytes(definition);
        foreach (var mode in new[] { "signature", "version", "size", "short" })
        {
            var bad = originalDefinition.ToArray(); var code = "DK2E132";
            switch (mode)
            {
                case "signature": Write(bad, 0, 0xAA); break;
                case "version": Write(bad, 4, 3); code = "DK2E140"; break;
                case "size": Write(bad, 8, (uint)bad.Length + 1); code = "DK2E141"; break;
                case "short": bad = bad[..8]; code = "DK2E131"; break;
            }
            File.WriteAllBytes(definition, bad); var result = inspector.Inspect(entry);
            Expect(!result.IsValid && result.Diagnostics.Any(item => item.Code == code && item.FilePath == definition)
                && result.Kind == Dk2FileKind.GameMapDescriptor && result.MapName == valid.MapName && result.Width == loaded.Width,
                "Referenced definition failure lost its exact file diagnostic or valid descriptor/map data.");
        }
        File.WriteAllBytes(definition, originalDefinition);
        // Local leaf fallback is required after searching the six parent roots.
        var tile = paths[0x64]; var leaf = Path.Combine(directory, Path.GetFileName(tile)); File.Move(tile, leaf);
        Expect(inspector.Inspect(entry).Width == loaded.Width && NativeMapDocument.Load(entry).Width == loaded.Width, "Local-leaf reference resolution diverged.");
        File.Move(leaf, tile);
        // A repeated resolved physical source is read once, preserving first-reference order.
        var duplicate = descriptor.ToArray(); duplicate.AsSpan(start, 72).CopyTo(duplicate.AsSpan(start + 72, 72));
        var duplicateEntry = Path.Combine(directory, "Duplicate.kwd"); File.WriteAllBytes(duplicateEntry, duplicate);
        var duplicateResult = inspector.Inspect(duplicateEntry);
        Expect(duplicateResult.Diagnostics.Count(item => item.FilePath == definition) == 0 && duplicateResult.Width == loaded.Width, "Repeated references produced duplicate/false diagnostics.");
        // Known required component missing, but other linked files still inspected.
        File.Move(tile, tile + ".hold"); var missingTile = inspector.Inspect(entry);
        Expect(missingTile.Width is null && missingTile.Height is null && missingTile.Diagnostics.Any(item => item.Code == "DK2W130" && item.FilePath!.EndsWith(Path.GetFileName(tile))), "Missing custom tile fell back to unrelated data or lost warning.");
        File.Move(tile + ".hold", tile);
        foreach (var (path, bytes) in inputs) Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "Reference inspection/load modified an input.");
    }

    private static void CheckTableBounds(string directory)
    {
        var basis = NativeMapDocument.Create(7, 6, "Tables"); var entry = basis.SaveCopy(directory);
        var descriptor = File.ReadAllBytes(entry); var end = checked(56 + (int)Read(descriptor, 52));
        var inspector = new Dk2FileInspector();
        foreach (var mode in new[] { "parameters", "overflow", "signature", "count", "truncated", "traversal", "rooted", "empty-part" })
        {
            var bytes = descriptor.ToArray();
            switch (mode)
            {
                case "parameters": Write(bytes, 48, 0); break;
                case "overflow": Write(bytes, 52, uint.MaxValue); break;
                case "signature": Write(bytes, end, 0); break;
                case "count": Write(bytes, 20, (Read(bytes, 20) & 0xFFFF0000) | 129); break;
                case "truncated": bytes = bytes[..(end + 8 + 71)]; Write(bytes, 8, (uint)bytes.Length); break;
                default:
                    var reference = mode switch { "traversal" => "Data/../escape", "rooted" => "C:\\escape", _ => "Data//escape" };
                    bytes.AsSpan(end + 16, 64).Clear(); Encoding.ASCII.GetBytes(reference).CopyTo(bytes, end + 16); break;
            }
            File.WriteAllBytes(entry, bytes); var result = inspector.Inspect(entry);
            Expect(!result.IsValid && result.Kind == Dk2FileKind.GameMapDescriptor && result.Diagnostics.Any(item => item.Code == "DK2E113")
                && result.Width is null && result.MapName is not null, "Malformed reference table accepted or descriptor metadata lost: " + mode);
            try { NativeMapDocument.Load(entry); throw new Exception("Loader accepted malformed table: " + mode); } catch (InvalidDataException) { }
            Expect(File.ReadAllBytes(entry).AsSpan().SequenceEqual(bytes), "Malformed-table inspection/load changed input.");
        }
        foreach (var count in new[] { 0, 128 })
        {
            var bytes = new byte[end + 8 + count * 72]; descriptor.AsSpan(0, end + 8).CopyTo(bytes);
            Write(bytes, 8, (uint)bytes.Length); Write(bytes, 20, (Read(descriptor, 20) & 0xFFFF0000) | (uint)count);
            File.WriteAllBytes(entry, bytes); var result = inspector.Inspect(entry); var loaded = NativeMapDocument.Load(entry);
            Expect(result.IsValid && result.Width == loaded.Width && loaded.Files.Count == basis.Files.Count, "Valid empty/128-blank table did not use exact local suffix fallback.");
        }
        File.WriteAllBytes(entry, descriptor);
    }

    private static void CheckSearchDepth(string directory)
    {
        var basis = NativeMapDocument.Create(7, 6, "Depth"); var entry = basis.SaveCopy(directory);
        var inspector = new Dk2FileInspector(); var nested = directory;
        for (var depth = 1; depth <= 6; depth++)
        {
            nested = Path.Combine(nested, "level" + depth); Directory.CreateDirectory(nested);
            var copy = Path.Combine(nested, Path.GetFileName(entry)); File.Copy(entry, copy);
            var result = inspector.Inspect(copy);
            if (depth <= 5)
                Expect(result.Width == 7 && NativeMapDocument.Load(copy).Width == 7, "Allowed parent search depth diverged: " + depth);
            else
            {
                Expect(result.Width is null && result.Diagnostics.Any(item => item.Code == "DK2W130"), "Inspector escaped the original six-root search bound.");
                try { NativeMapDocument.Load(copy); throw new Exception("Loader searched beyond six roots."); } catch (InvalidDataException) { }
            }
        }
        // Nearest matching root wins over a further parent, without changing references.
        var nearest = Path.GetDirectoryName(nested)!;
        var closestTile = Path.Combine(nearest, basis.TileFile.ExportPath); Directory.CreateDirectory(Path.GetDirectoryName(closestTile)!);
        var bytes = basis.TileFile.Data.ToArray(); Write(bytes, 20, 6); File.WriteAllBytes(closestTile, bytes);
        var nearestEntry = Path.Combine(nearest, Path.GetFileName(entry));
        Expect(inspector.Inspect(nearestEntry).Width == 6 && NativeMapDocument.Load(nearestEntry).Width == 6, "Nearest explicit root did not win over parent data.");
        Expect(File.ReadAllBytes(closestTile).AsSpan().SequenceEqual(bytes), "Parent search inspection changed input.");
    }

    private static void CheckFileBounds(string directory)
    {
        var basis = NativeMapDocument.Create(7, 6, "Bounds"); var entry = basis.SaveCopy(directory);
        var inspector = new Dk2FileInspector();
        foreach (var file in new[] { basis.MainFile, basis.Files.Single(file => file.Signature == 0xE6) })
        {
            var path = Path.Combine(directory, file.ExportPath); var bytes = File.ReadAllBytes(path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write)) stream.SetLength(64L * 1024 * 1024 + 1);
            var result = inspector.Inspect(entry);
            Expect(!result.IsValid && result.Kind == Dk2FileKind.GameMapDescriptor && result.Diagnostics.Any(item => item.Code == "DK2E134" && item.FilePath == path), "Oversized linked/descriptor file was not bounded.");
            if (file != basis.MainFile) Expect(result.Width == 7 && result.Height == 6, "Oversized variables erased valid tile dimensions.");
            try { NativeMapDocument.Load(entry); throw new Exception("Loader accepted oversized file."); } catch (InvalidDataException) { }
            File.WriteAllBytes(path, bytes);
        }
        Expect(inspector.Inspect(entry).IsValid, "Bounds test did not restore its private fixture.");
    }

    private static void CheckUntypedReferences(string directory)
    {
        var basis = GlobalDefinitionNumericChecks.Fixture(Path.Combine(directory, "basis"));
        var entry = basis.SaveCopy(Path.Combine(directory, "game")); var bytes = File.ReadAllBytes(entry);
        var start = 56 + checked((int)Read(bytes, 52)) + 8;
        var count = (int)(Read(bytes, 20) & 0xFFFF);
        var definitions = basis.Files.Where(file => LegacyGlobalSchemas.Find(file.Signature) is not null).ToArray();
        var relative = "Data/Editor/Maps/PackedGlobals.kld";
        var packedPath = Path.Combine(directory, "game", relative.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllBytes(packedPath, definitions.SelectMany(file => file.Data.ToArray()).ToArray());
        Write(bytes, start, 0); bytes.AsSpan(start + 8, 64).Clear(); Encoding.ASCII.GetBytes(relative).CopyTo(bytes, start + 8);
        // Remaining explicit definition references are absent: the untyped
        // packed file is their source, while all five map collections stay linked.
        for (var index = 1; index < count; index++)
            if (LegacyGlobalSchemas.Find(Read(bytes, start + index * 72)) is not null)
                bytes.AsSpan(start + index * 72 + 8, 64).Clear();
        File.WriteAllBytes(entry, bytes);
        var inspector = new Dk2FileInspector(); var loaded = NativeMapDocument.Load(entry); var result = inspector.Inspect(entry);
        Expect(result.IsValid && result.Width == loaded.Width && loaded.Files.Any(file => file.SourcePath == packedPath)
            && loaded.Records().Count(record => record.Category.StartsWith("Definition.")) > 0,
            "Untyped packed Globals.kld reference was treated as expected signature zero.");
        // Untyped tile links also use actual signature for dimensions, as the loader does.
        for (var index = 0; index < count; index++) if (Read(bytes, start + index * 72) == 0x64) Write(bytes, start + index * 72, 0);
        File.WriteAllBytes(entry, bytes);
        Expect(inspector.Inspect(entry).Width == loaded.Width && NativeMapDocument.Load(entry).Width == loaded.Width, "Untyped actual tile dimensions diverged from loader.");
        Expect(File.ReadAllBytes(entry).AsSpan().SequenceEqual(bytes), "Untyped inspection changed descriptor bytes.");
    }
}
