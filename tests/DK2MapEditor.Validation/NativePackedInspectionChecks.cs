using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class NativePackedInspectionChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Word(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static byte[] Section(uint signature, int? count = null, int? stride = null)
    {
        var family = LegacyGlobalSchemas.Find(signature); var rows = count ?? (family is null ? 1 : LegacyCompilerRules.DefinitionOrder(family.Group).Count); var size = stride ?? family?.RecordSize ?? 16;
        var bytes = new byte[56 + rows * size]; Word(bytes, 0, signature); Word(bytes, 4, 4); Word(bytes, 8, (uint)bytes.Length); Word(bytes, 12, signature + 1); Word(bytes, 16, 28); Word(bytes, 20, (uint)rows); Word(bytes, 24, 1); Word(bytes, 48, signature + 2); Word(bytes, 52, (uint)(rows * size));
        if (signature == 0xAA) for (var index = 0; index < rows; index++) bytes[56 + index * size + 0xF4B] = (byte)(index + 1);
        return bytes;
    }
    private static byte[] Join(params byte[][] parts) => parts.SelectMany(part => part).ToArray();
    private static byte[] Tail(int length) => Enumerable.Range(0, length).Select(index => (byte)(0x80 + index % 113)).ToArray();
    private static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Invalid definition layout was accepted."); }
    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2PackedInspection-" + Guid.NewGuid().ToString("N")));
        public string Descriptor { get; private set; } = "";
        private readonly List<(string Path, byte[] Bytes)> inputs = [];
        public Fixture() => Directory.CreateDirectory(Root);
        public string Fresh() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
        public NativeMapDocument Definitions(byte[] bytes)
        {
            Descriptor = NativeMapDocument.Create(8, 8, GameName).SaveCopy(Fresh(), temporary: true); var descriptor = File.ReadAllBytes(Descriptor);
            var reference = 56 + checked((int)BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(52))) + 8;
            Word(descriptor, reference, 0); new NativeField("Reference", 0, 64, NativeValueKind.WesternAnsi).Encode("Data\\Editor\\Maps\\" + GameName + "Globals.kld").CopyTo(descriptor, reference + 8); File.WriteAllBytes(Descriptor, descriptor);
            var path = Path.Combine(Path.GetDirectoryName(Descriptor)!, GameName + "Globals.kld"); File.WriteAllBytes(path, bytes); inputs.Add((path, bytes)); return NativeMapDocument.Load(Descriptor);
        }
        public NativeMapDocument Collection(uint signature, byte[] tail, bool unknownHeader = false)
        {
            var document = NativeMapDocument.Create(8, 8, GameName);
            if (signature == 0xBE) document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEOBJECT"), new(2, 2));
            if (signature == 0xE6) document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEGLOBALVARIABLE"), new(0, 0));
            if (signature == 0xD2) { document.AddScenarioEvent(0xD5, new(0, 0)); document.AddScenarioEvent(0xD6, new(0, 0)); }
            Descriptor = document.SaveCopy(Fresh(), temporary: true); var path = Path.Combine(Path.GetDirectoryName(Descriptor)!, Path.GetFileName(document.Files.Single(file => file.Signature == signature).ExportPath));
            var bytes = Join(File.ReadAllBytes(path), tail); Word(bytes, 8, (uint)bytes.Length); if (unknownHeader) Word(bytes, 12, 0x7000); File.WriteAllBytes(path, bytes); inputs.Add((path, bytes)); return NativeMapDocument.Load(Descriptor);
        }
        public MapDiagnostic[] Inspector() => new Dk2FileInspector().Inspect(Descriptor).Diagnostics.ToArray();
        public void Raw(NativeMapDocument document)
        {
            var copy = NativeMapDocument.Load(document.SaveCopy(Fresh(), temporary: true));
            foreach (var file in document.Files) Require(copy.Files.Single(row => row.ExportPath == file.ExportPath).Data.Span.SequenceEqual(file.Data.Span), "Raw copy changed original native bytes.");
        }
        public void SameDiagnostics(NativeMapDocument document, bool opaque, bool schema)
        {
            var expected = NativeMapValidator.Validate(document).Where(item => item.Code is "DK2N008" or "DK2N048").ToArray(); var actual = Inspector().Where(item => item.Code is "DK2N008" or "DK2N048").ToArray();
            Require(expected.SequenceEqual(actual), "Inspector and editable definition policies disagree.");
            Require(actual.Any(item => item.Code == "DK2N048") == opaque && actual.Any(item => item.Code == "DK2N008") == schema, "Missing or false definition layout warning.");
            Require(actual.All(item => item.Severity == DiagnosticSeverity.Warning), "Preservable definition bytes were treated as a read error.");
        }
        public void Dispose()
        {
            foreach (var (path, bytes) in inputs) Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "An original definition/collection file was edited.");
            var resolved = Path.GetFullPath(Root); Require(resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Unexpected fixture cleanup target."); Directory.Delete(resolved, recursive: true);
        }
    }
    public static void UnknownStride()
    {
        using var fixture = new Fixture(); var bytes = Section(0x6E, 100_000, 1); var document = fixture.Definitions(bytes);
        var records = document.Records().Where(record => record.Category == "Definition.Terrains").ToArray();
        Require(records.Length == 1 && records[0].Offset == 0 && records[0].Length == bytes.Length && records[0].Fields.Count == 0,
            "Unverified one-byte stride was expanded into speculative per-row objects.");
        fixture.SameDiagnostics(document, false, true); fixture.Raw(document);
    }
    public static void Run(Action<string, Action> run)
    {
        run("packed inspector: named definition count, stride, payload and adjacent-frame truncation refusals match editable load", () =>
        {
            foreach (var kind in new[] { "payload", "count", "zero-stride", "truncated" })
            {
                using var fixture = new Fixture(); var second = kind == "zero-stride" ? Section(0x78, 0) : Section(0x78);
                if (kind == "payload") Word(second, 52, 1); if (kind == "count") Word(second, 20, 0); if (kind == "zero-stride") Word(second, 20, 1); if (kind == "truncated") second = second[..^1];
                Reject(() => fixture.Definitions(Join(Section(0x6E), second)));
                Require(fixture.Inspector().Any(item => item.Severity == DiagnosticSeverity.Error), "Inspector accepted packed data rejected by editable load.");
            }
        });
        run("packed inspector: future headers and unsupported known row sizes expose the same opaque-schema diagnostics", () =>
        {
            foreach (var kind in new[] { "future", "info", "stride" })
            {
                using var fixture = new Fixture(); var second = Section(0x8C, stride: kind == "stride" ? 617 : null);
                if (kind == "future") Word(second, 4, 5); if (kind == "info") { Word(second, 12, 0); Word(second, 20, uint.MaxValue); }
                var document = fixture.Definitions(Join(Section(0x6E), second)); fixture.SameDiagnostics(document, kind == "future", true); fixture.Raw(document);
            }
        });
        run("packed inspector: editable rows following a preserved read-only prefix are not silently skipped", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Definitions(Join(Section(0xF0), Section(0x6E)));
            var rows = document.Records().Where(record => record.Category == "Definition.Terrains").ToArray();
            Require(rows.Length == LegacyCompilerRules.DefinitionOrder("Terrains").Count && rows.All(record => record.Fields.Count != 0), "Read-only first signature hid a known editable inner family.");
            fixture.SameDiagnostics(document, false, false); fixture.Raw(document);
        });
        run("packed inspector: unknown frames and non-resynchronising suffixes report retained opaque data consistently", () =>
        {
            foreach (var bytes in new[] { Join(Section(0x6E), Section(0x7000), Section(0x78)), Join(Section(0x6E), Tail(96), Section(0x78)) })
            {
                using var fixture = new Fixture(); var document = fixture.Definitions(bytes); fixture.SameDiagnostics(document, true, false); fixture.Raw(document);
                Require(document.PrepareProjectConversion().Issues.Any(issue => issue.Code == "OpaqueDefinitionData" && issue.Severity == ProjectConversionSeverity.Loss), "Opaque definition data lost its existing conversion warning.");
            }
        });
        run("packed inspector: all four creature count/stride variants share verified prefix fields and no false opaque-schema warning", () =>
        {
            foreach (var count in new[] { 44, 46 }) foreach (var stride in new[] { 5449, 5537 })
            {
                using var fixture = new Fixture(); var document = fixture.Definitions(Section(0xAA, count, stride)); var rows = document.Records().Where(record => record.Category == "Definition.Creatures").ToArray();
                Require(rows.Length == count && rows.All(record => record.Fields.Count == LegacyGlobalSchemas.Find(0xAA)!.Fields.Count), "Known creature prefix variant lost editable fields."); fixture.SameDiagnostics(document, false, false);
            }
        });
        run("collection suffix: all four families require loss consent, retain complete source archive and demonstrate omitted regenerated suffix", () =>
        {
            foreach (var signature in new uint[] { 0xB4, 0xBE, 0xD2, 0xE6 })
            {
                using var fixture = new Fixture(); var tail = Tail(96); var document = fixture.Collection(signature, tail); var file = document.Files.Single(file => file.Signature == signature); var original = file.Data.ToArray();
                using var save = new EditorSaveSession(document); var path = fixture.Fresh() + ".dkm"; var plan = save.PrepareSaveAs(path);
                Require(!plan.HasBlockingIssues && plan.ConversionIssues.Count(issue => issue.Code == "OpaqueCollectionData" && issue.Severity == ProjectConversionSeverity.Loss) == 1, "Opaque collection suffix is omitted without an explicit conversion loss.");
                Require(plan.NeedsLossConsent && !save.Save(plan, false) && !File.Exists(path), "Declined suffix-loss consent published a project.");
                Require(save.Save(plan, true), "Approved source-preserving conversion failed.");
                var archive = EditorSourceArchive.Read(save.LastSave!.SourceArchives.Single()); Require(archive.SourceFiles.Single(row => row.ExportPath == file.ExportPath).Bytes.AsSpan().SequenceEqual(original), "Source companion omitted opaque collection bytes.");
                var reopened = NativeMapDocument.Load(path); var compiled = reopened.CompileLegacyProject(GameName, NativeMapDocument.Create(8, 8, GameName));
                Require(!compiled.Files.Single(row => row.Signature == signature && row.ExportPath.Contains("Maps")).Data.Span.EndsWith(tail), "Fixture did not demonstrate actual suffix omission during recompilation.");
                Require(file.Data.Span.SequenceEqual(original), "Conversion changed its source collection.");
            }
        });
        run("collection suffix: empty suffixes produce no new loss and unverified whole containers remain blocking", () =>
        {
            foreach (var signature in new uint[] { 0xB4, 0xBE, 0xD2, 0xE6 })
            {
                using var fixture = new Fixture(); var document = fixture.Collection(signature, []); Require(document.PrepareProjectConversion().Issues.All(issue => issue.Code != "OpaqueCollectionData"), "A fully represented collection received a false suffix loss.");
                var unknown = fixture.Collection(signature, Tail(96), unknownHeader: true); var conversion = unknown.PrepareProjectConversion();
                Require(conversion.HasBlockingIssues && conversion.Issues.Any(issue => issue.Code == "UnverifiedCollection"), "Opaque header was downgraded from blocking to optional loss.");
            }
        });
        run("packed inspector: unknown strides retain one opaque frame instead of allocating speculative row objects", UnknownStride);
    }
}
