using System.Buffers.Binary;
using System.Text;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class DescriptorHeaderGuardChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static uint Word(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
    private static void Word(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Editable loader accepted an unsupported descriptor header/prefix."); }
    private static byte[] ResizeParameters(byte[] original, int end)
    {
        var oldEnd = checked(56 + (int)Word(original, 52)); var bytes = new byte[end + original.Length - oldEnd];
        original.AsSpan(0, Math.Min(end, oldEnd)).CopyTo(bytes); original.AsSpan(oldEnd).CopyTo(bytes.AsSpan(end));
        if (end > oldEnd) for (var index = oldEnd; index < end; index++) bytes[index] = (byte)(0x80 + index % 113);
        Word(bytes, 52, (uint)end - 56); Word(bytes, 8, (uint)bytes.Length); return bytes;
    }
    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2DescriptorGuards-" + Guid.NewGuid().ToString("N")));
        public string PathName { get; private set; } = "";
        private readonly List<(string Path, byte[] Bytes)> inputs = [];
        public Fixture() => Directory.CreateDirectory(Root);
        public string Fresh() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
        public string Write(Func<byte[], byte[]> patch, Action<NativeMapDocument>? prepare = null)
        {
            var document = NativeMapDocument.Create(8, 8, GameName); prepare?.Invoke(document);
            PathName = document.SaveCopy(Fresh(), temporary: true);
            var bytes = patch(File.ReadAllBytes(PathName)); File.WriteAllBytes(PathName, bytes); inputs.Add((PathName, bytes)); return PathName;
        }
        public NativeMapDocument Variables(uint id, int value, byte[] tail)
        {
            var descriptor = Write(bytes => bytes, document => document.SetMapVariables([new(id, value)]));
            var path = Path.Combine(Path.GetDirectoryName(descriptor)!, GameName + "Variables.kld"); var bytes = File.ReadAllBytes(path).Concat(tail).ToArray();
            Word(bytes, 8, 0); Word(bytes, 52, 7); File.WriteAllBytes(path, bytes); inputs.Add((path, bytes)); return NativeMapDocument.Load(descriptor);
        }
        public void Bad(Func<byte[], byte[]> patch, string code)
        {
            var path = Write(patch); var preview = new Dk2FileInspector().Inspect(path);
            Require(!preview.IsValid && preview.Diagnostics.Any(item => item.Code == code && item.Severity == DiagnosticSeverity.Error), "Inspector did not classify the descriptor header failure: " + code);
            Reject(() => NativeMapDocument.Load(path));
        }
        public void Good(Func<byte[], byte[]> patch)
        {
            var path = Write(patch); var original = File.ReadAllBytes(path); var document = NativeMapDocument.Load(path); var preview = new Dk2FileInspector().Inspect(path);
            Require(preview.IsValid && preview.Width == 8 && preview.Height == 8, "Structurally supported descriptor lost preview dimensions.");
            Require(!document.HasChanges && !document.CanUndo, "Descriptor parsing changed working bytes/history.");
            var map = document.Records().Single(record => record.Category == "Map"); var field = map.Fields.Single(field => field.Name == "wstrMainName");
            var before = document.MainFile.Data.ToArray(); document.SetField(map, field, "Guarded Keep"); var after = document.MainFile.Data.ToArray();
            Require(after.AsSpan(0, field.Offset).SequenceEqual(before.AsSpan(0, field.Offset)) && after.AsSpan(field.Offset + field.Size).SequenceEqual(before.AsSpan(field.Offset + field.Size)), "Editing a supported field changed headers, references or suffixes.");
            document.Undo(); Require(document.MainFile.Data.Span.SequenceEqual(before), "Undo failed to restore exact descriptor bytes."); document.Redo();
            var copy = document.SaveCopy(Fresh(), temporary: true); var reopened = NativeMapDocument.Load(copy);
            Require(reopened.MainFile.Data.Span.SequenceEqual(after) && reopened.Files.Count == document.Files.Count && new Dk2FileInspector().Inspect(copy).MapName == "Guarded Keep", "Raw Save/reopen altered a supported descriptor layout.");
            Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(original), "Editing wrote back into the original descriptor.");
        }
        public void Dispose()
        {
            foreach (var (path, bytes) in inputs) Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "Original descriptor input changed.");
            var resolved = Path.GetFullPath(Root); Require(resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Unexpected fixture cleanup target."); Directory.Delete(resolved, recursive: true);
        }
    }
    public static void MapVariableDeletion()
    {
        using var fixture = new Fixture(); var variable = MapVariables.Definitions.First(value => value.Minimum != value.Maximum); var tail = Enumerable.Range(0, 96).Select(index => (byte)(0x80 + index % 113)).ToArray();
        var document = fixture.Variables(variable.ID, variable.Minimum, tail); var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        using var session = document.CreateMapPropertiesSession(); session.WorkingDocument.ClearMapVariableOverrides([variable.ID]); var plan = document.PrepareMapProperties(session); document.ApplyMapProperties(plan);
        var file = document.Files.Single(value => value.Signature == 0xE6); var after = file.Data.ToArray();
        Require(Word(after, 20) == 0 && Word(after, 52) == 0 && file.Data.Span.EndsWith(tail), "Parent variable deletion lost safe payload fallback or opaque suffix.");
        document.Undo(); foreach (var (original, bytes) in before) Require(original.Data.Span.SequenceEqual(bytes), "Properties deletion Undo changed original buffers.");
        document.Redo(); Require(file.Data.Span.SequenceEqual(after), "Properties deletion Redo changed resulting variable bytes.");
        var reopened = NativeMapDocument.Load(document.SaveCopy(fixture.Fresh(), temporary: true)); Require(reopened.Files.Single(value => value.Signature == 0xE6).Data.Span.SequenceEqual(after), "Parent variable deletion did not survive raw Save/reopen.");
    }
    public static void Run(Action<string, Action> run)
    {
        run("descriptor guards: bad DD information signature and unsupported info size are rejected by inspector and editor", () =>
        {
            foreach (var (offset, value) in new (int, uint)[] { (12, 0), (12, 0x7000), (16, 0), (16, 27), (16, 29), (16, uint.MaxValue) })
            { using var fixture = new Fixture(); fixture.Bad(bytes => { Word(bytes, offset, value); return bytes; }, "DK2E110"); }
        });
        run("descriptor guards: a physical reference table cannot masquerade as truncated metadata parameters", () =>
        {
            foreach (var end in new[] { 56, 64, 2232, 2359 })
            { using var fixture = new Fixture(); fixture.Bad(bytes => ResizeParameters(bytes, end), "DK2E112"); }
        });
        run("descriptor guards: DF marker, missing reference prefix and overflow remain bounded failures", () =>
        {
            using var marker = new Fixture(); marker.Bad(bytes => { Word(bytes, 48, 0); return bytes; }, "DK2E111");
            using var overflow = new Fixture(); overflow.Bad(bytes => { Word(bytes, 52, uint.MaxValue); return bytes; }, "DK2E112");
            using var references = new Fixture(); references.Bad(bytes => { Word(bytes, 56 + (int)Word(bytes, 52), 0); return bytes; }, "DK2E113");
        });
        run("descriptor guards: minimal2304 metadata payload, complete source layout and extended parameters preserve fields and every raw byte", () =>
        {
            foreach (var mode in new[] { "minimum", "source", "extended" })
            { using var fixture = new Fixture(); fixture.Good(bytes => mode == "minimum" ? ResizeParameters(bytes, 2360) : mode == "extended" ? ResizeParameters(bytes, 56 + (int)Word(bytes, 52) + 96) : bytes); }
        });
        run("descriptor guards: understated native generic size remains source-compatible metadata rather than a parsing bound", () =>
        {
            foreach (var declared in new uint[] { 0, 12, 56 })
            { using var fixture = new Fixture(); fixture.Good(bytes => { Word(bytes, 8, declared); return bytes; }); }
        });
        run("descriptor guards: metadata-only preview remains available while full editing needs an actual tile companion", () =>
        {
            using var fixture = new Fixture(); var bytes = new byte[2368]; Word(bytes, 0, 0xDC); Word(bytes, 4, 4); Word(bytes, 8, (uint)bytes.Length); Word(bytes, 12, 0xDD); Word(bytes, 16, 28); Word(bytes, 48, 0xDF); Word(bytes, 52, 2304); Word(bytes, 2360, 0xDE);
            Encoding.Unicode.GetBytes("Metadata Keep").CopyTo(bytes, 56); Encoding.Unicode.GetBytes("Original author").CopyTo(bytes, 2232);
            var path = fixture.Write(_ => bytes); var standalone = Path.Combine(fixture.Root, "MetadataOnly.kwd"); File.Copy(path, standalone);
            var preview = new Dk2FileInspector().Inspect(standalone);
            Require(preview.IsValid && preview.MapName == "Metadata Keep" && preview.Author == "Original author" && preview.Width is null && preview.Diagnostics.Any(item => item.Code == "DK2W130"), "Read-only metadata preview was confused with an editable map.");
            Reject(() => NativeMapDocument.Load(standalone));
        });
        run("properties variable deletion: parent uses the same safe metadata fallback as native child editing with exact suffix and Undo", MapVariableDeletion);
    }
}
