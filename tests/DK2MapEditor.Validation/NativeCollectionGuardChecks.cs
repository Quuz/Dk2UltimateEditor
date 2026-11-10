using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class NativeCollectionGuardChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Word(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> snapshot)
    { foreach (var (file, bytes) in snapshot) Require(file.Data.Span.SequenceEqual(bytes), "Refused edit changed native bytes."); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or ArgumentException) { return; }
        throw new Exception("Unverified semantic edit/export was accepted.");
    }
    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2CollectionGuards-" + Guid.NewGuid().ToString("N")));
        private readonly List<(string Path, byte[] Bytes)> inputs = [];
        public string Descriptor { get; private set; } = "";
        public Fixture() => Directory.CreateDirectory(Root);
        public string Fresh() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
        public NativeMapDocument Load(string structure, Func<byte[], byte[]> patch)
        {
            var document = NativeMapDocument.Create(8, 8, GameName); var template = RecordTemplate.All.Single(value => value.Structure == structure);
            if (template.FileSignature != 0xB4) document.AddRecord(template, new(2, 2));
            Descriptor = document.SaveCopy(Fresh(), temporary: true);
            var path = Path.Combine(Path.GetDirectoryName(Descriptor)!, Path.GetFileName(document.Files.Single(file => file.Signature == template.FileSignature).ExportPath));
            var bytes = patch(File.ReadAllBytes(path)); Word(bytes, 8, (uint)bytes.Length); File.WriteAllBytes(path, bytes); inputs.Add((path, bytes));
            return NativeMapDocument.Load(Descriptor);
        }
        public NativeMapDocument Variant(string structure, int length) => Load(structure, original =>
        {
            var template = RecordTemplate.All.Single(value => value.Structure == structure); var start = template.FileSignature == 0xD2 ? 60 : 56;
            var bytes = new byte[start + length + 96]; original.AsSpan(0, Math.Min(original.Length, start + length)).CopyTo(bytes);
            Word(bytes, start - 4, (uint)length); Word(bytes, start + 4, (uint)length - 8);
            for (var index = start + length; index < bytes.Length; index++) bytes[index] = (byte)(0x80 + index % 113);
            return bytes;
        });
        public void VerifyRaw(NativeMapDocument document, uint signature)
        {
            var file = document.Files.Single(value => value.Signature == signature); var bytes = file.Data.ToArray();
            var copy = NativeMapDocument.Load(document.SaveCopy(Fresh(), temporary: true));
            Require(copy.Files.Single(value => value.Signature == signature).Data.Span.SequenceEqual(bytes), "Opaque native collection was not preserved by raw Save/reopen.");
        }
        public void VerifyDiagnostics(NativeMapDocument document, int expected = 1)
        {
            var actual = NativeMapValidator.Validate(document).Where(item => item.Code == "DK2N047").ToArray();
            var preview = new Dk2FileInspector().Inspect(Descriptor).Diagnostics.Where(item => item.Code == "DK2N047").ToArray();
            Require(actual.Length == expected && preview.Length == expected, "Inspector/editor disagree about opaque collection layouts.");
            Require(actual.Select(item => (item.Severity, item.Message, item.FilePath)).SequenceEqual(preview.Select(item => (item.Severity, item.Message, item.FilePath))), "Inspector/editor layout diagnostics differ.");
            Require(actual.All(item => item.Severity == DiagnosticSeverity.Warning), "Raw preservation was confused with a blocking semantic conversion.");
        }
        public void Dispose()
        {
            foreach (var (path, bytes) in inputs) Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "An original input was changed.");
            var resolved = Path.GetFullPath(Root); Require(resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Unexpected fixture cleanup root.");
            Directory.Delete(resolved, recursive: true);
        }
    }
    private static void VerifyRefusal(NativeMapDocument document, NativeRecord record)
    {
        var before = Snapshot(document); var template = RecordTemplate.All.First(value => value.FileSignature == record.File.Signature);
        Reject(() => document.AddRecord(template, new(3, 3))); Exact(before);
        Reject(() => document.DuplicateRecord(record)); Exact(before);
        Reject(() => document.DeleteRecord(record)); Exact(before);
        Require(!document.CanUndo && !document.CanRedo, "Refused edits left a history step.");
        var conversion = document.PrepareProjectConversion();
        Require(conversion.HasBlockingIssues && conversion.Issues.Any(issue => issue.Code == "UnverifiedCollection" && issue.Severity == ProjectConversionSeverity.Error), "Unsupported layout was silently projected into DKM.");
        Require(conversion.SourceFiles.Any(file => file.ExportPath == record.File.ExportPath && file.Bytes.AsSpan().SequenceEqual(record.File.Data.Span)), "Conversion source snapshot lost the opaque collection.");
        Exact(before);
    }
    public static void Transactions(Action<string, Action> run)
    {
        run("native collection guards: direct band-member writes cannot bypass an opaque sibling record", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Load("GAMEBAND", original =>
            { var bytes = original.Concat(new byte[24]).ToArray(); Word(bytes, original.Length, 0xCA); Word(bytes, original.Length + 4, 16); Word(bytes, 20, 2); Word(bytes, 52, 587); return bytes; });
            var band = document.Records().Single(record => record.Category == "GAMEBAND"); var before = Snapshot(document);
            Reject(() => document.AddBandMember(band, 1)); Exact(before);
            Require(!document.CanUndo && !document.CanRedo && document.BandMembers(band).All(member => !member.Active), "Refused band edit left bytes or history.");
        });
        run("native collection guards: resize refuses unknown geometry instead of silently omitting it", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Variant("GAMEOBJECT", 31); var before = Snapshot(document);
            Reject(() => document.Resize(9, 9, 0, 0, new(30, 2, 1))); Exact(before);
            Require(document.Width == 8 && document.Height == 8 && !document.CanUndo && !document.CanRedo, "Refused resize changed dimensions or history.");
        });
    }
    public static void Run(Action<string, Action> run)
    {
        run("native collection guards: every short/extended known thing and event is opaque, retained and refused for semantic edits", () =>
        {
            foreach (var template in RecordTemplate.All.Where(value => value.FileSignature is 0xBE or 0xD2))
                foreach (var length in new[] { template.Size - 1, template.Size + 4 })
                {
                    using var fixture = new Fixture(); var document = fixture.Variant(template.Structure, length);
                    var record = document.Records().Single(value => value.File.Signature == template.FileSignature);
                    Require(record.Fields.Count == 0 && record.Category == "Opaque.NativeCollection", "Unverified record received a partial or complete typed schema.");
                    fixture.VerifyDiagnostics(document); VerifyRefusal(document, record); fixture.VerifyRaw(document, template.FileSignature);
                }
        });
        run("native collection guards: all four container info/define layouts are opaque before interpreting unverified counts", () =>
        {
            foreach (var structure in new[] { "GAMEOBJECT", "GAMEPLAYER", "GAMEVARCREATURECOUNT", "Trigger" })
                foreach (var mutation in new[] { 12, 16, -8 })
                {
                    using var fixture = new Fixture(); var signature = RecordTemplate.All.Single(value => value.Structure == structure).FileSignature;
                    var document = fixture.Load(structure, bytes => { Word(bytes, mutation < 0 ? (signature == 0xD2 ? 60 : 56) + mutation : mutation, 0x7000); if (mutation == 12) Word(bytes, 20, uint.MaxValue); return bytes; });
                    var record = document.Records().Single(value => value.File.Signature == signature);
                    Require(record.Offset == 0 && record.Length == record.File.Length && record.Fields.Count == 0 && record.Category == "Opaque.NativeCollection", "Unverified container was interpreted as editable records.");
                    fixture.VerifyDiagnostics(document); VerifyRefusal(document, record); fixture.VerifyRaw(document, signature);
                }
        });
        run("native collection guards: unknown framed things do not permit a guessed edit, selection or text projection", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Load("GAMEOBJECT", original =>
            { var bytes = original.Concat(new byte[24]).ToArray(); Word(bytes, original.Length, 0xCA); Word(bytes, original.Length + 4, 16); Word(bytes, 20, 2); Word(bytes, 52, 56); return bytes; });
            var opaque = document.Records().Single(record => record.File.Signature == 0xBE && record.Fields.Count == 0);
            fixture.VerifyDiagnostics(document); VerifyRefusal(document, opaque);
            var known = document.Records().Single(record => record.Category == "GAMEOBJECT"); var before = Snapshot(document);
            Reject(() => document.SetField(known, known.Fields.Single(field => field.Name == "dwObjectLocationX"), "3")); Exact(before);
            Reject(() => document.CopySelection(new(1, 1, 4, 4))); Exact(before);
            Reject(() => TextMapFile.Project(document)); Exact(before);
            document.Paint([new(4, 4)], new(8, 3, 1)); document.Undo(); Exact(before);
            fixture.VerifyRaw(document, 0xBE);
        });
        run("native collection guards: fabricated typed fields cannot bypass an opaque record", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Variant("GAMEOBJECT", 31); var file = document.Files.Single(value => value.Signature == 0xBE);
            var fields = LegacySchemas.Fields("GAMEOBJECT").Where(field => field.Offset + field.Size <= 31).ToArray(); var forged = new NativeRecord(file, 56, 31, "GAMEOBJECT", fields); var before = Snapshot(document);
            Reject(() => document.SetField(forged, fields.Single(field => field.Name == "dwObjectLocationX"), "3")); Exact(before);
        });
        run("native collection guards: verified headers still refuse truncation and outer future versions consistently", () =>
        {
            foreach (var structure in new[] { "GAMEOBJECT", "GAMEPLAYER", "GAMEVARCREATURECOUNT", "Trigger" })
                foreach (var variant in new[] { 0, 1, 2 })
                {
                    using var fixture = new Fixture(); Reject(() => fixture.Load(structure, original => variant switch { 0 => original[..55], 1 => original[..^1], _ => Future(original) }));
                    var diagnostics = new Dk2FileInspector().Inspect(fixture.Descriptor).Diagnostics;
                    Require(diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error), "Inspector accepted a companion rejected by the editor.");
                }
            static byte[] Future(byte[] bytes) { Word(bytes, 4, 5); return bytes; }
        });
        run("native collection guards: every canonical template remains typed and editable", () =>
        {
            foreach (var template in RecordTemplate.All)
            {
                using var fixture = new Fixture(); var document = fixture.Load(template.Structure, bytes => bytes); var record = document.Records().First(value => value.File.Signature == template.FileSignature && !value.Category.StartsWith("BandHero", StringComparison.Ordinal));
                Require(record.Fields.Count != 0 && record.Category != "Opaque.NativeCollection", "Canonical template became opaque.");
                fixture.VerifyDiagnostics(document, 0); var field = record.Fields.First(value => value.Name is not ("ID" or "bAreaID" or "bBandID" or "bEffectID"));
                document.SetField(record, field, record.Read(field)); fixture.VerifyRaw(document, template.FileSignature);
            }
        });
        run("native collection guards: original text policy may explicitly omit opaque scripts while supported-scenario export refuses them", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Variant("Trigger", 28);
            Require(NativeMapValidator.Validate(document, includeScripts: false).All(item => item.Code != "DK2N047"), "No-scenario validation inspected an intentionally omitted script layout.");
            Reject(() => TextMapFile.Project(document));
            Require(TextMapFile.Project(document, TextMapScriptMode.OriginalNoScenarios).Text.Length != 0, "Explicit original no-scenario policy was unnecessarily blocked.");
        });
        Transactions(run);
    }
}
