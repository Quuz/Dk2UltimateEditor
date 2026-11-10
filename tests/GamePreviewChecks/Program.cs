using System.Security.Cryptography;
using DK2MapEditor.Core.Editing;

internal static class Program
{
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    static void Refused<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    static int Main(string[] args)
    {
        if (args.Length != 2 || Directory.Exists(args[0])) throw new ArgumentException("Require fresh private output and native basis.");
        args[0] = Path.GetFullPath(args[0]); Directory.CreateDirectory(args[0]);
        try
        {
            var basis = NativeMapDocument.Load(args[1]);
            var installed = basis.Files.Select(file => file.SourcePath).Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(File.Exists).ToDictionary(path => path, Hash);
            Check(basis.MissingReferences.Count == 0, "The real basis must resolve every native reference.");
            var project = NativeMapDocument.CreateNewMap(new("GamePreview", "Game preview", 24, 24, PlaceHearts: true));
            var before = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var undo = project.GetHistory().Undo.Count; var revision = project.EditRevision;
            var native = project.CompileLegacyProject("CapturedSource", basis);
            var packed = basis.Files.Single(file => file.Signature == 0x6E);
            var sections = new Dictionary<uint, byte[]>();
            for (var offset = 0; offset < packed.Length;)
            {
                var data = packed.Data.Span;
                Check(offset <= data.Length - 56, "Truncated independent packed fixture header.");
                var signature = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                var length = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 8)..]));
                Check(length >= 56 && length <= data.Length - offset && !sections.ContainsKey(signature), "Malformed independent packed section.");
                sections.Add(signature, data.Slice(offset, length).ToArray()); offset += length;
            }
            foreach (var signature in new uint[] { 0x6E, 0x78, 0xAA, 0x8C, 0x82, 0xF0, 0xA0, 0x96, 0x104 })
                Check(native.Files.Single(file => file.Signature == signature).Data.Span.SequenceEqual(sections[signature]),
                    "Compilation omitted or changed packed definition section " + signature.ToString("X"));
            Check(native.MissingReferences.Count == 0, "Complete packed basis still produces unresolved default references.");
            Console.WriteLine("PASS retail packed basis copies all nine required definition sections exactly, including non-first headers");
            var nativeBefore = native.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var game = Path.Combine(args[0], "fake-game"); var maps = Path.Combine(game, "Data", "Editor", "Maps");
            Directory.CreateDirectory(maps); var sentinel = Path.Combine(maps, "InstalledMap.kwd");
            File.WriteAllText(sentinel, "Never replace installed maps."); var sentinelHash = Hash(sentinel);
            var shared = basis.Files.Single(file => Path.GetFileName(file.ExportPath) == "GlobalVariables.kwd");
            var sharedPath = Path.Combine(game, shared.ExportPath); Directory.CreateDirectory(Path.GetDirectoryName(sharedPath)!);
            File.WriteAllBytes(sharedPath, shared.Data.ToArray()); var sharedHash = Hash(sharedPath);
            var preview = native.PrepareGamePreview(); var output = preview.Install(game);
            var loaded = NativeMapDocument.Load(output.DescriptorPath);
            Check(loaded.MissingReferences.Count == 0 && loaded.Width == 24 && loaded.Height == 24, "The relocated preview does not load as a complete native bundle.");
            Check(output.CreatedFiles.Count == 7 && Hash(sharedPath) == sharedHash && loaded.Files.All(file =>
                file.SourcePath == sharedPath || output.CreatedFiles.Contains(file.SourcePath, StringComparer.OrdinalIgnoreCase)), "Official preview must own seven map files and reuse the unchanged shared catalogue.");
            Check(output.CreatedFiles.Count == preview.FileCount && output.CreatedFiles.All(path => Path.GetDirectoryName(path) == maps
                && Path.GetFileName(path).StartsWith(output.LevelName, StringComparison.Ordinal)), "Preview owns inconsistent output names.");
            Check(output.CreatedFiles[^1] == output.DescriptorPath && output.LevelName.Length <= 32, "Descriptor was not published last or level name exceeds source limit.");
            var publishedDefinitions = File.ReadAllBytes(Path.Combine(maps, output.LevelName + "Globals.kld"));
            var publishedSections = new List<byte[]>();
            for (var offset = 0; offset < publishedDefinitions.Length;)
            {
                var length = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(publishedDefinitions.AsSpan(offset + 8)));
                Check(length >= 56 && length <= publishedDefinitions.Length - offset, "Invalid packed preview definition bounds.");
                publishedSections.Add(publishedDefinitions.AsSpan(offset, length).ToArray()); offset += length;
            }
            foreach (var file in nativeBefore.Where(pair => pair.Key != native.MainFile))
                Check(loaded.Files.Any(candidate => candidate.Data.Span.SequenceEqual(file.Value))
                    || publishedSections.Any(section => section.AsSpan().SequenceEqual(file.Value)), "Relocation or packing changed a non-descriptor game payload.");
            Check(nativeBefore.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value))
                && project.EditRevision == revision && project.GetHistory().Undo.Count == undo, "Private preparation changed source bytes or history.");
            Check(Hash(sentinel) == sentinelHash && installed.All(pair => Hash(pair.Key) == pair.Value), "Preview changed installed source files.");
            Console.WriteLine("PASS official seven-file compile/preview, packed definition bytes, unchanged shared catalogue, descriptor-last publication and exact source/history");

            var snapshot = Directory.GetFiles(maps).ToDictionary(path => path, Hash);
            Refused<IOException>(() => preview.Install(game));
            Check(snapshot.Count == Directory.GetFiles(maps).Length && snapshot.All(pair => Hash(pair.Key) == pair.Value), "Collision changed an existing output.");
            Refused<DirectoryNotFoundException>(() => preview.Install(Path.Combine(args[0], "missing-game")));
            Refused<InvalidOperationException>(() => project.PrepareGamePreview());
            Refused<InvalidDataException>(() => NativeMapDocument.Create(8, 8, "Incomplete").PrepareGamePreview());
            var second = native.PrepareGamePreview().Install(game);
            Check(second.LevelName != output.LevelName && !second.CreatedFiles.Intersect(output.CreatedFiles, StringComparer.OrdinalIgnoreCase).Any()
                && snapshot.All(pair => Hash(pair.Key) == pair.Value), "Second preview reused or overwrote the first preview.");
            Console.WriteLine("PASS preview collision refusal, incomplete/project/game-directory guards and independent repeated runs");

            var captured = native.PrepareGamePreview();
            var metadata = native.Records().Single(record => record.Category == "Map");
            var name = metadata.Fields.Single(field => field.Name == "wstrMainName"); var originalName = metadata.Read(name);
            native.SetField(metadata, name, "Changed after preparation");
            var frozen = captured.Install(game); var reopened = NativeMapDocument.Load(frozen.DescriptorPath);
            var reopenedMetadata = reopened.Records().Single(record => record.Category == "Map");
            Check(reopenedMetadata.Read(reopenedMetadata.Fields.Single(field => field.Name == "wstrMainName")) == originalName,
                "Prepared preview retained mutable live byte arrays.");
            Check(installed.All(pair => Hash(pair.Key) == pair.Value), "Capture lifetime test changed installed input.");
            Console.WriteLine("PASS immutable preview capture remains exact after later source edits");
            Console.WriteLine("GAME PREVIEW CHECKS PASSED (4)"); return 0;
        }
        catch (Exception error) { Console.WriteLine(error); return 1; }
    }
}
