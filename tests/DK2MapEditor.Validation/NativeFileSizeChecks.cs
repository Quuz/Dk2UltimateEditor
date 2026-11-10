using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class NativeFileSizeChecks
{
    private static void Expect(bool condition,string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(),"DK2NativeSizes-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var basis = NativeMapDocument.Create(6,5,"FileSizes");
            foreach (var structure in new[] { "GAMEOBJECT","Trigger","Action","GAMEGLOBALVARIABLE" })
                basis.AddRecord(RecordTemplate.All.Single(template => template.Structure == structure),new(2,2),3);
            var inspector = new Dk2FileInspector();
            foreach (var file in basis.Files)
            foreach (var declared in new uint[] { 0,12,(uint)file.Length - 1,(uint)file.Length })
            foreach (var tail in new byte[][] { [], [0xA1,0,0xFE,0x39,0x23] })
            {
                var root = Path.Combine(directory,$"{file.Signature:X}-{declared}-{tail.Length}"); var entry = basis.SaveCopy(root);
                var path = Path.Combine(root,file.ExportPath); var bytes = file.Data.ToArray().Concat(tail).ToArray();
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),declared); File.WriteAllBytes(path,bytes);
                var inspection = inspector.Inspect(entry);
                Expect(inspection.IsValid,$"Complete native 0x{file.Signature:X} rejected because stored file size {declared} differs from actual {bytes.Length}.");
                Expect(inspection.Diagnostics.Any(item => item.Code == "DK2W141") == (declared != bytes.Length),"Native stored-size warning missing or incorrectly emitted.");
                var loaded = NativeMapDocument.Load(entry); var snapshot = loaded.Files.ToDictionary(item => item.ExportPath,item => item.Data.ToArray());
                Expect(!loaded.HasChanges && !loaded.CanUndo && loaded.Records().Count == basis.Records().Count,"Stored file size changed record parsing or load history.");
                var reopened = NativeMapDocument.Load(loaded.SaveCopy(Path.Combine(root,"unchanged")));
                foreach (var item in reopened.Files) Expect(item.Data.Span.SequenceEqual(snapshot[item.ExportPath]),"Native size metadata or opaque bytes normalized during export.");
                loaded.Paint([new(2,2)],new MapTile(11,3,1)); loaded.Undo();
                foreach (var item in loaded.Files) Expect(item.Data.Span.SequenceEqual(snapshot[item.ExportPath]),"Undo did not retain native size metadata/opaque bytes.");
                Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes),"Native size validation/edit/export mutated input.");
            }
            foreach (var file in basis.Files)
            {
                var root = Path.Combine(directory,$"truncated-{file.Signature:X}"); var entry = basis.SaveCopy(root); var path = Path.Combine(root,file.ExportPath);
                var bytes = file.Data.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),(uint)bytes.Length + 1); File.WriteAllBytes(path,bytes);
                var inspection = inspector.Inspect(entry); Expect(!inspection.IsValid && inspection.Diagnostics.Any(item => item.Code == "DK2E141"),"Oversized native declared file size lost its error.");
                RejectLoad(entry);
                // A smaller stored size cannot make incomplete tile/record payloads safe.
                if (file == basis.MainFile) continue;
                var truncated = file.Data.Span[..^1].ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(truncated.AsSpan(8),12); File.WriteAllBytes(path,truncated); RejectLoad(entry);
            }
            foreach (var signature in new uint[] { LegacySectionFile.MapFileSignature,LegacySectionFile.AreaFileSignature })
            {
                var bytes = new byte[13]; BinaryPrimitives.WriteUInt32LittleEndian(bytes,signature); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),1);
                var path = Path.Combine(directory,$"editor-{signature:X}.dkm"); File.WriteAllBytes(path,bytes);
                Expect(!inspector.Inspect(path).IsValid && inspector.Inspect(path).Diagnostics.Any(item => item.Code == "DK2E141"),"Editor payload-size contract incorrectly relaxed with native file-size metadata.");
            }
            var invalidInfo = basis.TileFile.Data.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(invalidInfo.AsSpan(16),9);
            var invalidInfoPath = Path.Combine(directory,"invalid-info.kld"); File.WriteAllBytes(invalidInfoPath,invalidInfo);
            Expect(!inspector.Inspect(invalidInfoPath).IsValid,"Inspector accepted unsupported tile information version rejected by original/modern loader."); RejectLoad(invalidInfoPath);
        }
        finally { Directory.Delete(directory,true); }
    }
    private static void RejectLoad(string path)
    {
        try { NativeMapDocument.Load(path); } catch (InvalidDataException) { return; }
        throw new Exception("Actually truncated/oversized native file accepted by editing loader.");
    }
}
