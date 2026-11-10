using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class HistoricalSectionInspectionChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Word(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static void Reject(Action action)
    { try { action(); } catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or ArgumentException) { return; } throw new Exception("Unsupported/corrupt input was accepted."); }
    private static LegacySection Tiles()
    {
        var bytes = new byte[8 + 49 * 4]; Word(bytes, 0, 7); Word(bytes, 4, 7);
        for (var index = 0; index < 49; index++) Word(bytes, 8 + index * 4, new MapTile(30, 2, 1).Packed);
        return new(LegacySectionFile.TileSectionSignature, 1, bytes);
    }
    private static IEnumerable<(uint Signature, int Version, string Structure, int Rows)> FixedLayouts()
    {
        foreach (var version in Enumerable.Range(1, 7)) yield return (NativeMapDocument.LegacyInfo, version, version switch { 1 => "MAPPROPERTIESV1", 2 or 3 => "MAPPROPERTIESV2", 4 => "MAPPROPERTIESV4", 5 => "MAPPROPERTIESV5", 6 => "MAPPROPERTIESV6", _ => "MAPPROPERTIES" }, 1);
        foreach (var version in Enumerable.Range(1, 3)) yield return (NativeMapDocument.LegacyPlayers, version, version switch { 1 => "MAPOPTIONSV1", 2 => "MAPOPTIONSV2", _ => "MAPOPTIONS" }, 7);
        foreach (var version in Enumerable.Range(1, 6)) yield return (NativeMapDocument.LegacyThings, version, version switch { 1 => "MAPTHINGV1", 2 or 3 => "MAPTHINGV2", 4 => "MAPTHINGV4", 5 => "MAPTHINGV5", _ => "MAPTHING" }, 2);
        yield return (NativeMapDocument.LegacyAreas, 1, "MAPRECTEX", 2);
        foreach (var version in Enumerable.Range(1, 5)) yield return (NativeMapDocument.LegacyBands, version, version == 5 ? "MTHBAND" : "MTHBANDV" + version, 2);
    }
    private static byte[] Events(int version, uint links = 0, uint conditions = 0, uint actions = 0)
    {
        var linkSize = LegacyProjectSchemas.Size(version == 2 ? "TRIGGERLINKV2" : "TRIGGERLINK");
        var bytes = new byte[12 + (int)links * linkSize + (int)(conditions + actions) * LegacyProjectSchemas.Size("TRIGGER") + 146 * 64];
        Word(bytes, 0, links); Word(bytes, 4, conditions); Word(bytes, 8, actions); return bytes;
    }
    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2HistoricalInspection-" + Guid.NewGuid().ToString("N")));
        public string NativeDescriptor { get; private set; } = "";
        private readonly List<(string Path, byte[] Bytes)> inputs = [];
        public Fixture() => Directory.CreateDirectory(Root);
        public string Fresh() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
        public string Project(bool area, params LegacySection[] sections)
        {
            var bytes = LegacySectionFile.Create(new[] { Tiles() }.Concat(sections)).ToArray(); if (area) Word(bytes, 0, LegacySectionFile.AreaFileSignature);
            var path = Fresh() + (area ? ".dkz" : ".dkm"); File.WriteAllBytes(path, bytes); inputs.Add((path, bytes)); return path;
        }
        public string Raw(byte[] bytes) { var path = Fresh() + ".dkm"; File.WriteAllBytes(path, bytes); inputs.Add((path, bytes)); return path; }
        public (NativeMapDocument Document, Dk2FileInspection Preview) Same(string path)
        {
            var document = NativeMapDocument.Load(path); var preview = new Dk2FileInspector().Inspect(path);
            Require(preview.IsValid && preview.Width == document.Width && preview.Height == document.Height, "Inspector did not structurally parse the same historical tile/section stream.");
            var expected = NativeMapValidator.Validate(document).Where(item => item.Code == "DK2N008").ToArray(); var actual = preview.Diagnostics.Where(item => item.Code == "DK2N008").ToArray();
            Require(expected.SequenceEqual(actual), "Inspector/load disagree about preserved historical opaque sections."); return (document, preview);
        }
        public void Refused(string path)
        { Reject(() => NativeMapDocument.Load(path)); Require(new Dk2FileInspector().Inspect(path).Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error), "Inspector accepted a historical stream refused by editable loading."); }
        public (NativeMapDocument Document, string Descriptor, string Companion) NativeUnknown(uint version = 4, uint referenceType = 0)
        {
            var document = NativeMapDocument.Create(8, 8, GameName); var path = document.SaveCopy(Fresh(), temporary: true); var descriptor = File.ReadAllBytes(path);
            NativeDescriptor = path;
            var reference = 56 + (int)BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(52)) + 8; Word(descriptor, reference, referenceType);
            new NativeField("Reference", 0, 64, NativeValueKind.WesternAnsi).Encode("Data\\Editor\\Maps\\" + GameName + "Unknown.kld").CopyTo(descriptor, reference + 8); File.WriteAllBytes(path, descriptor); inputs.Add((path, descriptor));
            var bytes = Enumerable.Range(0, 112).Select(index => (byte)(0x80 + index % 113)).ToArray(); Word(bytes, 0, 0x7000); Word(bytes, 4, version); Word(bytes, 8, (uint)bytes.Length);
            var companion = Path.Combine(Path.GetDirectoryName(path)!, GameName + "Unknown.kld"); File.WriteAllBytes(companion, bytes); inputs.Add((companion, bytes));
            return (NativeMapDocument.Load(path), path, companion);
        }
        public void Dispose()
        {
            foreach (var (path, bytes) in inputs) Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "An original historical/native file changed.");
            var resolved = Path.GetFullPath(Root); Require(resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Unexpected fixture cleanup target."); Directory.Delete(resolved, recursive: true);
        }
    }
    public static void NativeBoundaries(Action<string, Action> run)
    {
        run("unknown native companion: fabricated fields and direct span writers cannot change unverified file bytes", () =>
        {
            using var fixture = new Fixture(); var (document, _, _) = fixture.NativeUnknown(); var file = document.Files.Single(file => file.Signature == 0x7000); var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var field = new NativeField("Invented", 12, 4, NativeValueKind.UInt32); var record = new NativeRecord(file, 0, file.Length, "Guessed", [field]);
            Reject(() => document.SetField(record, field, "1"));
            Reject(() => document.Edit("Guessed opaque-file edit", () => { Require(MemoryMarshal.TryGetArray(file.Data, out var buffer), "Fixture buffer missing."); buffer.Array![buffer.Offset + 16] ^= 1; }));
            foreach (var (original, bytes) in before) Require(original.Data.Span.SequenceEqual(bytes), "Refused opaque-file edit changed original bytes.");
            Require(!document.CanUndo && !document.CanRedo, "Refused opaque-file edit left history.");
        });
        run("unknown native companion: future outer versions and typed-reference signature mismatch are refused by both routes", () =>
        {
            foreach (var (version, reference) in new (uint, uint)[] { (5, 0), (4, 0x6E) })
            {
                using var fixture = new Fixture(); Reject(() => fixture.NativeUnknown(version, reference));
                Require(new Dk2FileInspector().Inspect(fixture.NativeDescriptor).Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error), "Unknown companion outer boundary differs between inspector and loading.");
            }
        });
    }
    public static void Run(Action<string, Action> run)
    {
        run("historical inspection: all22 fixed layouts and DKM/AREA roots share editable fields, dimensions and bounded parser", () =>
        {
            foreach (var area in new[] { false, true }) foreach (var (signature, version, structure, rows) in FixedLayouts())
            {
                using var fixture = new Fixture(); var path = fixture.Project(area, new LegacySection(signature, version, new byte[rows * LegacyProjectSchemas.Size(structure)])); var (document, _) = fixture.Same(path);
                var records = document.Records().Where(record => record.File.Signature == signature).ToArray(); Require(records.Length == rows && records.All(record => record.Category == "Project." + structure && record.Fields.Count != 0), "A supported historical layout became opaque.");
            }
        });
        run("historical inspection: truncated fixed-layout sections are errors in both routes", () =>
        {
            foreach (var area in new[] { false, true }) foreach (var (signature, version, structure, rows) in FixedLayouts())
            { using var fixture = new Fixture(); fixture.Refused(fixture.Project(area, new LegacySection(signature, version, new byte[rows * LegacyProjectSchemas.Size(structure) - 1]))); }
        });
        run("historical inspection: KTRG2/3 counts and full146-slot alias tables use the loading contract", () =>
        {
            foreach (var area in new[] { false, true }) foreach (var version in new[] { 2, 3 })
            {
                using var fixture = new Fixture(); fixture.Same(fixture.Project(area, new LegacySection(NativeMapDocument.LegacyEvents, version, Events(version, 1, 2, 1))));
                var malformed = Events(version); Word(malformed, 0, uint.MaxValue); fixture.Refused(fixture.Project(area, new LegacySection(NativeMapDocument.LegacyEvents, version, malformed)));
                fixture.Refused(fixture.Project(area, new LegacySection(NativeMapDocument.LegacyEvents, version, Events(version)[..^1])));
            }
        });
        run("historical inspection: KGBL flags, seven arrays and unknown flag policy agree", () =>
        {
            foreach (var area in new[] { false, true }) foreach (var (mask, structure, count) in new (uint, string, int)[] { (1, "GLOBALTERRAIN", 38), (2, "GLOBALROOM", 24), (4, "GLOBALDOOR", 8), (8, "GLOBALTRAP", 15), (16, "GLOBALSPELL", 13), (0xFF00, "GLOBALCREATURE", 46), (0x10000, "GLOBALCRSPELLS", 41) })
            {
                using var fixture = new Fixture(); var bytes = new byte[4 + count * LegacyProjectSchemas.Size(structure)]; Word(bytes, 0, mask);
                fixture.Same(fixture.Project(area, new LegacySection(NativeMapDocument.LegacyGlobals, 1, bytes))); fixture.Refused(fixture.Project(area, new LegacySection(NativeMapDocument.LegacyGlobals, 1, bytes[..^1])));
            }
            using var unknown = new Fixture(); var flags = new byte[4]; Word(flags, 0, 0x80000000); var pair = unknown.Same(unknown.Project(false, new LegacySection(NativeMapDocument.LegacyGlobals, 1, flags)));
            Require(pair.Document.Records().Any(record => record.File.Signature == NativeMapDocument.LegacyGlobals && record.Category == "Project.Opaque"), "Unknown KGBL flags were guessed.");
        });
        run("historical inspection: old nested and future/unknown sections stay opaque with exact raw Save", () =>
        {
            foreach (var area in new[] { false, true }) foreach (var (signature, version) in new (uint, int)[] { (NativeMapDocument.LegacyEvents, 1), (NativeMapDocument.LegacyThings, 99), (0x70000001, 7) })
            {
                using var fixture = new Fixture(); var path = fixture.Project(area, new LegacySection(signature, version, new byte[31])); var (document, _) = fixture.Same(path); var bytes = File.ReadAllBytes(path);
                Require(document.Records().Any(record => record.Category == "Project.Opaque" && record.File.Signature == signature), "Unknown historical section received guessed fields.");
                var copy = document.SaveCopy(fixture.Fresh(), temporary: true); var saved = LegacySectionFile.Read(copy); var original = LegacySectionFile.Parse(bytes);
                var unchanged = saved.Sections.Where(section => section.Signature != ActivityStatisticsPayload.Signature).ToArray();
                Require(unchanged.Length == original.Sections.Count && saved.Sections.Count(section => section.Signature == ActivityStatisticsPayload.Signature) <= 1, "Historical Save added optional placeholder or duplicate statistics sections.");
                for (var index = 0; index < unchanged.Length; index++) Require(unchanged[index].Signature == original.Sections[index].Signature && unchanged[index].Version == original.Sections[index].Version && unchanged[index].Payload.Span.SequenceEqual(original.Sections[index].Payload.Span), "Raw historical Save changed an original section or its order.");
            }
        });
        run("historical inspection: inner framing, missing/duplicate KMAP and enhanced clipboard route cannot be approved as ordinary projects", () =>
        {
            using var fixture = new Fixture(); var original = LegacySectionFile.Create([Tiles()]).ToArray(); var broken = original.Concat(new byte[11]).ToArray(); Word(broken, 8, (uint)broken.Length - 12); fixture.Refused(fixture.Raw(broken));
            var missing = new byte[12]; Word(missing, 0, LegacySectionFile.MapFileSignature); Word(missing, 4, 1); fixture.Refused(fixture.Raw(missing));
            var doubled = original.Concat(original.AsSpan(12).ToArray()).ToArray(); Word(doubled, 8, (uint)doubled.Length - 12); fixture.Refused(fixture.Raw(doubled));
            fixture.Refused(fixture.Project(true, new LegacySection(SelectionFile.ClipboardSection, 6, new byte[16])));
        });
        run("historical inspection: malformed DKM private level sections share loader refusal without source/archive discovery", () =>
        {
            using var fixture = new Fixture(); foreach (var signature in new uint[] { 0x4D4C3244, 0x4F4C3244 })
            { fixture.Refused(fixture.Project(false, new LegacySection(signature, 1, new byte[3]))); fixture.Refused(fixture.Project(true, new LegacySection(signature, 1, new byte[52]))); }
        });
        run("unknown native companion: visible opaque record, shared diagnostic and explicit DKM loss with exact source archive", () =>
        {
            using var fixture = new Fixture(); var (document, descriptor, _) = fixture.NativeUnknown(); var record = document.Records().Single(record => record.File.Signature == 0x7000);
            Require(record.Category == "Opaque.NativeFile" && record.Offset == 0 && record.Length == record.File.Length && record.Fields.Count == 0, "Unknown native companion was silently omitted from Records.");
            var expected = NativeMapValidator.Validate(document).Where(item => item.Code == "DK2N049").ToArray(); var preview = new Dk2FileInspector().Inspect(descriptor).Diagnostics.Where(item => item.Code == "DK2N049").ToArray();
            Require(expected.Length == 1 && expected.SequenceEqual(preview), "Unknown companion diagnostics differ or are absent.");
            using var save = new EditorSaveSession(document); var path = fixture.Fresh() + ".dkm"; var plan = save.PrepareSaveAs(path);
            Require(!plan.HasBlockingIssues && plan.ConversionIssues.Any(issue => issue.Code == "OpaqueNativeFile" && issue.Severity == ProjectConversionSeverity.Loss), "Unknown file was dropped from DKM without a reported loss.");
            Require(!save.Save(plan, false) && !File.Exists(path) && save.Save(plan, true), "Unknown-file loss consent did not control publishing.");
            var archive = EditorSourceArchive.Read(save.LastSave!.SourceArchives.Single()); Require(archive.SourceFiles.Single(file => file.ExportPath == record.File.ExportPath).Bytes.AsSpan().SequenceEqual(record.File.Data.Span), "Source archive dropped unknown native companion bytes.");
            var copy = NativeMapDocument.Load(document.SaveCopy(fixture.Fresh(), temporary: true)); Require(copy.Files.Single(file => file.Signature == 0x7000).Data.Span.SequenceEqual(record.File.Data.Span), "Raw copy changed the unknown native file.");
        });
        NativeBoundaries(run);
    }
}
