using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class NativeTileLayoutChecks
{
    private static void Expect(bool condition,string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Unsafe native tile payload accepted."); }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(),"DK2NativeTiles-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var basis = NativeMapDocument.Create(6,5,"TileLayout"); var original = basis.TileFile.Data.ToArray(); var inspector = new Dk2FileInspector();
            foreach (var declared in new uint[] { 0,1,120,uint.MaxValue }) foreach (var tail in new byte[][] { [], [0xA1,0x00,0xFE,0x57,0x23] })
            {
                var bytes = original.Concat(tail).ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),(uint)bytes.Length); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32),declared);
                var path = Path.Combine(directory,$"tiles-{declared}-{tail.Length}.kld"); File.WriteAllBytes(path,bytes);
                Expect(inspector.Inspect(path).IsValid,"Complete source-compatible tile payload rejected by inspector because of stored size metadata.");
                var document = NativeMapDocument.Load(path); Expect(document.Width == 6 && document.Height == 5 && !document.HasChanges && !document.CanUndo,"Native tile metadata changed during loading.");
                Expect(document.GetTile(5,4) == basis.GetTile(5,4),"Last actual tile lost to size metadata/trailing bytes.");
                var unchanged = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,$"unchanged-{declared}-{tail.Length}"))); Expect(unchanged.TileFile.Data.Span.SequenceEqual(bytes),"Unchanged export normalized tile header/tail.");
                document.Paint([new(2,2)],new MapTile(11,3,1)); var painted = bytes.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(painted.AsSpan(36 + (2 * 6 + 2) * 4),new MapTile(11,3,1).Packed);
                Expect(document.TileFile.Data.Span.SequenceEqual(painted),"Painting touched header/tail/unrelated tile bytes."); document.Undo(); Expect(document.TileFile.Data.Span.SequenceEqual(bytes),"Painting undo lost stored size/tail.");
                document.Resize(7,6,1,1,new(2,2,1)); var resized = document.TileFile.Data.ToArray();
                Expect(resized.Length == 36 + 7 * 6 * 4 + tail.Length && resized.AsSpan(36 + 7 * 6 * 4).SequenceEqual(tail) && BinaryPrimitives.ReadUInt32LittleEndian(resized.AsSpan(32)) == 7 * 6 * 4,"Resize discarded/miscounted opaque tile tail.");
                Expect(document.GetTile(3,3) == basis.GetTile(2,2),"Resize shift used opaque tail as cells.");
                var reopened = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,$"resized-{declared}-{tail.Length}"))); Expect(reopened.TileFile.Data.Span.SequenceEqual(resized),"Resized opaque-tail map did not reopen exactly.");
                document.Undo(); Expect(document.TileFile.Data.Span.SequenceEqual(bytes),"Resize undo failed to restore source size metadata/tail."); document.Redo(); Expect(document.TileFile.Data.Span.SequenceEqual(resized),"Resize redo changed source opaque tail.");
                Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes),"Native tile editing/export mutated input.");
            }
            foreach (var length in new[] { 36,original.Length - 1 })
            {
                var truncated = original.AsSpan(0,length).ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(truncated.AsSpan(8),(uint)length); BinaryPrimitives.WriteUInt32LittleEndian(truncated.AsSpan(32),0);
                var path = Path.Combine(directory,$"truncated-{length}.kld"); File.WriteAllBytes(path,truncated);
                Expect(!inspector.Inspect(path).IsValid,"Actual truncated tiles accepted by inspector."); Reject(() => NativeMapDocument.Load(path));
            }
            foreach (var dimension in new uint[] { 0,1025,uint.MaxValue })
            {
                var bad = original.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(20),dimension); var path = Path.Combine(directory,$"dimensions-{dimension}.kld"); File.WriteAllBytes(path,bad);
                Expect(!inspector.Inspect(path).IsValid,"Unsafe dimensions accepted by inspector."); Reject(() => NativeMapDocument.Load(path));
            }
        }
        finally { Directory.Delete(directory,true); }
    }
}
