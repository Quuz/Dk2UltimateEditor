using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

internal static class NativeCollectionBoundaryChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Word(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static NativeRecord[] Rows(NativeMapDocument document, uint signature) => document.Records()
        .Where(row => row.File.Signature == signature && !row.Category.StartsWith("BandHero", StringComparison.Ordinal)).ToArray();
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> snapshot)
    { foreach (var (file, bytes) in snapshot) Require(file.Data.Span.SequenceEqual(bytes), "History changed preserved bytes: " + file.ExportPath); }

    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2CollectionBoundaries-" + Guid.NewGuid().ToString("N")));
        private readonly List<(string Path, byte[] Bytes)> inputs = [];
        public byte[] Tail { get; } = Enumerable.Range(0, 128).Select(index => (byte)(0x80 + index % 113)).ToArray();
        public Fixture() => Directory.CreateDirectory(Root);
        public string Fresh() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
        public NativeMapDocument Load(uint signature, uint storedSize, string structure = "GAMEOBJECT")
        {
            var document = NativeMapDocument.Create(8, 8, GameName);
            var template = RecordTemplate.All.Single(value => value.Structure == structure);
            document.AddRecord(template, new(2, 2)); document.AddRecord(template, new(3, 3));
            var descriptor = document.SaveCopy(Fresh(), temporary: true);
            var path = Path.Combine(Path.GetDirectoryName(descriptor)!, Path.GetFileName(document.Files.Single(file => file.Signature == signature).ExportPath));
            var bytes = File.ReadAllBytes(path).Concat(Tail).ToArray();
            Word(bytes, signature == 0xD2 ? 56 : 52, storedSize); File.WriteAllBytes(path, bytes); inputs.Add((path, bytes));
            return NativeMapDocument.Load(descriptor);
        }
        public void Verify(NativeMapDocument document, uint signature, Dictionary<NativeFile, byte[]> before, int expectedCount, uint expectedSize)
        {
            var rows = Rows(document, signature); var file = document.Files.Single(value => value.Signature == signature);
            Require(rows.Length == expectedCount, "Wrong parsed record count after collection mutation.");
            Require(BinaryPrimitives.ReadUInt32LittleEndian(file.Data.Span[(signature == 0xD2 ? 56 : 52)..]) == expectedSize, "Payload metadata was not adjusted safely.");
            Require(rows[^1].Offset + rows[^1].Length == file.Length - Tail.Length, "New records were placed inside a record or opaque suffix.");
            Require(file.Data.Span[^Tail.Length..].SequenceEqual(Tail), "Opaque collection suffix changed.");
            var oldCount = BinaryPrimitives.ReadUInt32LittleEndian(before[file].AsSpan(20));
            if (signature == 0xD2) oldCount += BinaryPrimitives.ReadUInt32LittleEndian(before[file].AsSpan(24));
            var header = signature == 0xD2 ? 60 : 56;
            if (expectedCount > oldCount) Require(file.Data.Span.Slice(header, before[file].Length - Tail.Length - header)
                .SequenceEqual(before[file].AsSpan(header, before[file].Length - Tail.Length - header)), "Insertion reordered or overwrote existing collection rows.");
            var after = Snapshot(document); document.Undo(); Exact(before); document.Redo(); Exact(after);
            var reopened = NativeMapDocument.Load(document.SaveCopy(Fresh(), temporary: true));
            Require(Rows(reopened, signature).Length == expectedCount, "Saved collection cannot be parsed with its new count.");
            Require(reopened.Files.Single(value => value.Signature == signature).Data.Span.SequenceEqual(file.Data.Span), "Raw Save/reopen changed collection bytes.");
        }
        public void Dispose()
        {
            foreach (var (path, bytes) in inputs) Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "Original collection input changed.");
            var resolved = Path.GetFullPath(Root);
            Require(resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Unexpected fixture cleanup target.");
            Directory.Delete(resolved, recursive: true);
        }
    }

    public static void Run(Action<string, Action> run)
    {
        run("native collection boundaries: Add uses parsed end for things, players, variables and both event categories", () =>
        {
            foreach (var (signature, structure) in new (uint, string)[] { (0xBE, "GAMEOBJECT"), (0xB4, "GAMEPLAYER"), (0xE6, "GAMEVARCREATURECOUNT"), (0xD2, "Trigger"), (0xD2, "Action") })
                foreach (var stored in new uint[] { 0, 96, uint.MaxValue })
                {
                    using var fixture = new Fixture(); var document = fixture.Load(signature, stored, structure); var before = Snapshot(document); var rows = Rows(document, signature);
                    var size = RecordTemplate.All.Single(value => value.Structure == structure).Size;
                    document.AddRecord(RecordTemplate.All.Single(value => value.Structure == structure), new(4, 4));
                    var payload = rows.Sum(row => row.Length) + size;
                    fixture.Verify(document, signature, before, rows.Length + 1, stored == uint.MaxValue ? (uint)payload : stored + (uint)size);
                }
        });
        run("native collection boundaries: Duplicate preserves original rows and opaque suffix", () =>
        {
            foreach (var (signature, structure) in new (uint, string)[] { (0xBE, "GAMEOBJECT"), (0xD2, "Trigger") })
            {
                using var fixture = new Fixture(); var document = fixture.Load(signature, 16, structure); var rows = Rows(document, signature); var before = Snapshot(document);
                var originals = rows.Select(row => row.File.Data.Slice(row.Offset, row.Length).ToArray()).ToArray(); document.DuplicateRecord(rows[0]);
                var after = Rows(document, signature); for (var i = 0; i < rows.Length; i++) Require(after[i].File.Data.Span.Slice(after[i].Offset, after[i].Length).SequenceEqual(originals[i]), "Duplicate overwrote an original row.");
                fixture.Verify(document, signature, before, rows.Length + 1, 16 + (uint)rows[0].Length);
            }
        });
        run("native collection boundaries: Delete recovers understated metadata without unsigned underflow", () =>
        {
            foreach (var (signature, structure) in new (uint, string)[] { (0xBE, "GAMEOBJECT"), (0xB4, "GAMEPLAYER"), (0xE6, "GAMEVARCREATURECOUNT"), (0xD2, "Trigger"), (0xD2, "Action") })
            {
                using var fixture = new Fixture(); var document = fixture.Load(signature, 16, structure); var rows = Rows(document, signature); var before = Snapshot(document);
                var removed = rows[^1]; document.DeleteRecord(removed); var payload = rows.Sum(row => row.Length) - removed.Length;
                fixture.Verify(document, signature, before, rows.Length - 1, removed.Length > 16 ? (uint)payload : 16 - (uint)removed.Length);
            }
        });
        run("native collection boundaries: batch insertion uses parsed end", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Load(0xBE, 0); var before = Snapshot(document); var count = Rows(document, 0xBE).Length;
            var plan = document.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 3 }, new(Position: new(4, 4), Flags: (ObjectBatchFlags)127));
            Require(document.ApplyObjectBatchInsertion(plan, [0]) == 1, "Batch insertion did not add a creature.");
            fixture.Verify(document, 0xBE, before, count + 1, 36);
        });
        run("native collection boundaries: batch removal recovers understated metadata", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Load(0xBE, 16); var before = Snapshot(document); var rows = Rows(document, 0xBE);
            Require(document.ApplyObjectRemoval(document.PlanObjectRemoval([rows[0]])) == 1, "Batch removal did not remove one object.");
            fixture.Verify(document, 0xBE, before, rows.Length - 1, (uint)(rows.Sum(row => row.Length) - rows[0].Length));
        });
        run("native collection boundaries: batch modification uses real insertion and owner relocation offsets", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Load(0xBE, 16); var before = Snapshot(document); var rows = Rows(document, 0xBE);
            var plan = document.PlanObjectBatchModification(rows, new(PlacedThingKind.Object, 2, 3), new(ObjectModificationMask.Type, Flags: (ObjectBatchFlags)127));
            plan = document.ReviewObjectBatchModification(plan, [ObjectModificationDecision.Modify, ObjectModificationDecision.Modify]);
            Require(document.ApplyObjectBatchModification(plan) == 2, "Batch modification did not change both objects.");
            Require(document.ObjectManagerEntries().All(entry => entry.Thing.TypeID == 2), "Replacement object types were lost.");
            fixture.Verify(document, 0xBE, before, rows.Length, 16);
        });
        run("native collection boundaries: editor hero-band deletion recovers understated metadata", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Load(0xBE, 16, "GAMEBAND"); var before = Snapshot(document); var rows = Rows(document, 0xBE);
            document.DeleteEditorHeroBand(rows[0]); fixture.Verify(document, 0xBE, before, rows.Length - 1, (uint)(rows.Sum(row => row.Length) - rows[0].Length));
        });
    }
}
