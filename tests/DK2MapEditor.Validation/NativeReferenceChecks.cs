using DK2MapEditor.Core.Editing;

internal static class NativeReferenceChecks
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(),"DK2ExplicitReferences-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            CheckRenamedDescriptor(Path.Combine(directory,"renamed"));
            CheckWesternReferences(Path.Combine(directory,"western"));
            var basis = NativeMapDocument.Create(20,20,"ExplicitReference"); var gameRoot = Path.Combine(directory,"game-root"); var basisEntry = basis.SaveCopy(gameRoot);
            var importedDirectory = Path.Combine(gameRoot,"Imported"); Directory.CreateDirectory(importedDirectory); var copiedEntry = Path.Combine(importedDirectory,Path.GetFileName(basisEntry)); File.Copy(basisEntry,copiedEntry);
            foreach (var file in basis.Files.Where(file => file != basis.MainFile))
            {
                var stale = file.Data.ToArray(); stale[^1] ^= 1;
                File.WriteAllBytes(Path.Combine(importedDirectory,Path.GetFileName(file.ExportPath)),stale);
            }
            var loaded = NativeMapDocument.Load(copiedEntry);
            if (loaded.Files.Select(file => file.ExportPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != loaded.Files.Count) throw new Exception("Local fallback duplicated explicit KWD references.");
            foreach (var file in basis.Files.Where(file => file != basis.MainFile))
                if (!loaded.Files.Single(item => item.ExportPath == file.ExportPath).Data.Span.SequenceEqual(file.Data.Span)) throw new Exception("Stale local fallback shadowed an explicit KWD reference.");
            var exported = NativeMapDocument.Load(loaded.SaveCopy(Path.Combine(directory,"exported")));
            foreach (var file in loaded.Files)
                if (!exported.Files.Single(item => item.ExportPath == file.ExportPath).Data.Span.SequenceEqual(file.Data.Span)) throw new Exception("Explicit-reference/fallback export changed bytes.");
            if (loaded.HasChanges || !File.ReadAllBytes(copiedEntry).AsSpan().SequenceEqual(File.ReadAllBytes(basisEntry))) throw new Exception("Read-only reference resolution mutated input/history.");
        }
        finally { Directory.Delete(directory,true); }
    }
    private static void CheckRenamedDescriptor(string directory)
    {
        var basis = NativeMapDocument.Create(20,20,"OriginalName");
        var entry = basis.SaveCopy(directory);
        var renamed = Path.Combine(Path.GetDirectoryName(entry)!,"Renamed.kwd");
        File.Copy(entry,renamed);
        var companions = basis.Files.Where(file => file != basis.MainFile).ToArray();
        foreach (var file in companions)
        {
            var suffix = Path.GetFileName(file.ExportPath)["OriginalName".Length..];
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(renamed)!,"Renamed" + suffix),file.Data.ToArray());
        }
        var inputs = Directory.GetFiles(directory,"*",SearchOption.AllDirectories).ToDictionary(path => path,File.ReadAllBytes);
        var loaded = NativeMapDocument.Load(renamed);
        if (loaded.Files.Count != basis.Files.Count)
            throw new Exception("Renamed descriptor added same-type fallback collections alongside resolved explicit companions.");
        foreach (var file in companions)
            if (!loaded.Files.Single(item => item.ExportPath == file.ExportPath).Data.Span.SequenceEqual(file.Data.Span))
                throw new Exception("Renamed descriptor did not retain the exact explicit companion.");
        // Existing but invalid local files must not invalidate a resolved explicit source.
        foreach (var file in companions)
        {
            var suffix = Path.GetFileName(file.ExportPath)["OriginalName".Length..];
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(renamed)!,"Renamed" + suffix),new byte[12]);
        }
        if (!new DK2MapEditor.Core.Formats.Dk2FileInspector().Inspect(renamed).IsValid
            || NativeMapDocument.Load(renamed).Files.Count != basis.Files.Count)
            throw new Exception("Stale renamed companions overrode explicit descriptor types.");
        foreach (var (path,bytes) in inputs) File.WriteAllBytes(path,bytes);
        var reopened = NativeMapDocument.Load(loaded.SaveCopy(Path.Combine(directory,"exported")));
        foreach (var file in loaded.Files)
            if (!reopened.Files.Single(item => item.ExportPath == file.ExportPath).Data.Span.SequenceEqual(file.Data.Span))
                throw new Exception("Renamed descriptor export changed explicit input bytes.");
        // An unresolved explicit collection still permits the original stem fallback.
        var explicitTile = Path.Combine(directory,basis.TileFile.ExportPath);
        File.Move(explicitTile,explicitTile + ".hold");
        try
        {
            var fallback = NativeMapDocument.Load(renamed);
            if (fallback.TileFile.SourcePath != Path.Combine(Path.GetDirectoryName(renamed)!,"RenamedMap.kld")
                || !fallback.MissingReferences.Any(reference => reference.EndsWith("OriginalNameMap.kld")))
                throw new Exception("Unresolved explicit map did not retain its missing label and valid fallback.");
        }
        finally { File.Move(explicitTile + ".hold",explicitTile); }
        foreach (var (path,bytes) in inputs)
            if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) throw new Exception("Renamed descriptor load/export changed inputs.");
        CheckSharedVariablesFallback(directory,basis);
    }
    private static void CheckSharedVariablesFallback(string directory,NativeMapDocument basis)
    {
        var descriptor = basis.MainFile.Data.ToArray();
        var start = 56 + (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(52)) + 8;
        var count = (int)(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(20)) & 0xFFFF);
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * 72;
            if (System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(offset)) != 0xE6) continue;
            descriptor.AsSpan(offset + 8,64).Clear();
            System.Text.Encoding.ASCII.GetBytes("Data\\Editor\\GlobalVariables.kwd").CopyTo(descriptor,offset + 8);
        }
        var variables = basis.Files.Single(file => file.Signature == 0xE6);
        File.WriteAllBytes(Path.Combine(directory,"Data","Editor","GlobalVariables.kwd"),variables.Data.ToArray());
        var entry = Path.Combine(directory,"Data","Editor","Maps","SharedReference.kwd");
        File.WriteAllBytes(entry,descriptor);
        var fallbackPath = Path.Combine(Path.GetDirectoryName(entry)!,"SharedReferenceVariables.kld");
        File.WriteAllBytes(fallbackPath,variables.Data.ToArray());
        var loaded = NativeMapDocument.Load(entry);
        if (loaded.Files.Count(file => file.Signature == 0xE6) != 2 || !loaded.Files.Any(file => file.SourcePath == fallbackPath))
            throw new Exception("Shared GlobalVariables incorrectly suppressed the map Variables fallback.");
    }
    private static void CheckWesternReferences(string directory)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var encoding = System.Text.Encoding.GetEncoding(1252,System.Text.EncoderFallback.ExceptionFallback,System.Text.DecoderFallback.ExceptionFallback);
        var basis = GlobalDefinitionNumericChecks.Fixture(directory); var descriptor = basis.MainFile.Data.ToArray();
        var expected = new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase); var missing = new List<string>();
        var start = 56 + checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(52))) + 8;
        var count = (int)(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(20)) & 0xFFFF);
        for (var index = 0; index < count; index++)
        {
            var offset = start + index * 72 + 8; var original = System.Text.Encoding.ASCII.GetString(descriptor,offset,64).Split('\0')[0];
            var originalPath = original.Replace('\\',Path.DirectorySeparatorChar); if (!Path.HasExtension(originalPath)) originalPath += ".kwd";
            var reference = "Data\\Editor\\références\\¡éßœ€_" + Path.GetFileName(original.Replace('\\',Path.DirectorySeparatorChar));
            var relative = reference.Replace('\\',Path.DirectorySeparatorChar); if (!Path.HasExtension(relative)) relative += ".kwd";
            if (basis.Files.SingleOrDefault(file => file.ExportPath.Equals(originalPath,StringComparison.OrdinalIgnoreCase)) is { } file)
            {
                var target = Path.Combine(directory,relative); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.WriteAllBytes(target,file.Data.ToArray()); expected.Add(relative,file.Data.ToArray());
            }
            else missing.Add(reference);
            descriptor.AsSpan(offset,64).Clear(); encoding.GetBytes(reference).CopyTo(descriptor,offset);
            // Opaque data after NUL must survive untouched references/export.
            descriptor[offset + 63] = 0xA1;
        }
        var entry = Path.Combine(directory,"¡éßœ€_Numbers.kwd"); File.WriteAllBytes(entry,descriptor); var loaded = NativeMapDocument.Load(entry);
        if (!loaded.MissingReferences.SequenceEqual(missing)) throw new Exception("CP1252 missing-reference labels/order lost.");
        foreach (var (relative,bytes) in expected)
            if (!loaded.Files.Single(file => file.ExportPath.Equals(relative,StringComparison.OrdinalIgnoreCase)).Data.Span.SequenceEqual(bytes)) throw new Exception("CP1252 explicit reference did not resolve exact input bytes.");
        var snapshot = loaded.Files.ToDictionary(file => file.ExportPath,file => file.Data.ToArray()); var unchanged = NativeMapDocument.Load(loaded.SaveCopy(Path.Combine(directory,"unchanged")));
        foreach (var file in unchanged.Files) if (!file.Data.Span.SequenceEqual(snapshot[file.ExportPath])) throw new Exception("CP1252 unchanged export altered reference or companion bytes.");
        loaded.SetGlobalDefinitionValues([new("Terrains",6,"GoldValue","456")]); var edited = NativeMapDocument.Load(loaded.SaveCopy(Path.Combine(directory,"edited")));
        var value = edited.GlobalDefinitionValues(edited.GlobalDefinitionRows().Single(row => row.Family == "Terrains" && row.Index == 6)).Single(field => field.Name == "GoldValue").Value;
        if (value != "456" || !edited.MissingReferences.SequenceEqual(missing) || edited.Files.Any(file => file.ExportPath.Contains('?'))) throw new Exception("CP1252 scoped-definition rewrite replaced filename characters or lost edits.");
        loaded.Undo(); foreach (var file in loaded.Files) if (!file.Data.Span.SequenceEqual(snapshot[file.ExportPath])) throw new Exception("CP1252 definition undo altered source bytes.");
        foreach (var invalid in new[] { "Data\\Editor\\références\\..\\escape.kld", "Data\\Editor\\références\\?bad.kld", "C:\\références\\bad.kld" })
        {
            var bad = descriptor.ToArray(); bad.AsSpan(start + 8,64).Clear(); encoding.GetBytes(invalid).CopyTo(bad,start + 8); var badEntry = Path.Combine(directory,"unsafe.kwd"); File.WriteAllBytes(badEntry,bad);
            try { NativeMapDocument.Load(badEntry); throw new Exception("Western encoding relaxed unsafe reference rejection."); } catch (InvalidDataException) { }
        }
        if (!File.ReadAllBytes(entry).AsSpan().SequenceEqual(descriptor)) throw new Exception("Western reference edits/export mutated the input descriptor.");
    }
}
