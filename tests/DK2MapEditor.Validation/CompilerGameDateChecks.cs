using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class CompilerGameDateChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static readonly Lazy<NativeMapDocument> gameBasis = new(() => NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd"));
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static NativeMapDocument Project(int version = 17)
    {
        var document = NativeMapDocument.CreateNewMap(new("GameDates", "GameDates", 32, 30, PlaceHearts: false));
        var info = document.Records().Single(record => record.Category == "Project.MAPPROPERTIES");
        document.SetField(info, info.Fields.Single(field => field.Name == "Version"), version.ToString()); return document;
    }
    private static byte[] DateBytes(NativeFile file, int offset = 28) => file.Data.Slice(offset, 20).ToArray();
    private static DateTime Time(ReadOnlySpan<byte> bytes, int offset) => new(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]), bytes[offset + 3], bytes[offset + 2], bytes[offset + 6], bytes[offset + 7], bytes[offset + 8], DateTimeKind.Unspecified);
    private static void Fresh(NativeFile file, DateTime before, DateTime after, int offset = 28)
    {
        var bytes = DateBytes(file, offset);
        DateTime created;
        try { created = Time(bytes, 0); }
        catch (ArgumentOutOfRangeException) { throw new InvalidOperationException("Generated GAMEDATE contains an invalid/zero calendar value: " + file.ExportPath); }
        Require(created == Time(bytes, 10) && created >= before.AddSeconds(-1) && created <= after.AddSeconds(1), "Generated creation/modification fields do not contain local generation time.");
        Require(new[] { 4, 5, 9, 14, 15, 19 }.All(index => bytes[index] == 0), "Source-zeroed GAMEDATE padding was copied from a basis file.");
    }
    private static byte[] Sentinel()
    {
        var bytes = Enumerable.Repeat((byte)0xA7, 20).ToArray();
        void Set(int offset, ushort year, byte day, byte month, byte hour, byte minute, byte second)
        { BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), year); bytes[offset + 2] = day; bytes[offset + 3] = month; bytes[offset + 6] = hour; bytes[offset + 7] = minute; bytes[offset + 8] = second; }
        Set(0, 1994, 4, 3, 5, 6, 7); Set(10, 1995, 8, 9, 10, 11, 12); return bytes;
    }
    private static void Raw(NativeFile file, int offset, ReadOnlySpan<byte> bytes)
    {
        Require(MemoryMarshal.TryGetArray(file.Data, out var buffer), "Fixture bytes unavailable.");
        bytes.CopyTo(buffer.Array!.AsSpan(buffer.Offset + offset));
    }
    private static NativeMapDocument Basis()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2CompilerDateBasis-" + Guid.NewGuid().ToString("N"));
        var document = GlobalDefinitionNumericChecks.Fixture(directory);
        foreach (var family in LegacyGlobalSchemas.All) Raw(document.Files.Single(file => file.Signature == family.Signature), 28, Sentinel());
        return document;
    }
    private static (NativeMapDocument Output, DateTime Before, DateTime After) Compile(NativeMapDocument project, NativeMapDocument basis)
    {
        var source = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var originals = basis.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var history = project.GetHistory(); var revision = project.EditRevision; var dirty = project.HasChanges;
        var changes = 0; var histories = 0;
        EventHandler changed = (_, _) => changes++; EventHandler hc = (_, _) => histories++;
        project.Changed += changed; project.HistoryChanged += hc;
        var before = DateTime.Now; NativeMapDocument output;
        try { output = project.CompileLegacyProject(GameName, basis); }
        finally { project.Changed -= changed; project.HistoryChanged -= hc; }
        var after = DateTime.Now; var last = project.GetHistory();
        Require(source.Count == project.Files.Count && source.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && originals.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value))
            && revision == project.EditRevision && dirty == project.HasChanges && changes == 0 && histories == 0
            && history.EditorChangeCount == last.EditorChangeCount && history.ChangeCount == last.ChangeCount && history.SnapshotBytes == last.SnapshotBytes
            && history.Undo.SequenceEqual(last.Undo) && history.Redo.SequenceEqual(last.Redo), "Date generation changed the source project/basis or source history/counters.");
        return (output, before, after);
    }
    private static GlobalDefinitionRow Row(NativeMapDocument project, string family) => project.GlobalDefinitionRows().First(row => row.Family == family && row.Editable);
    private static string Field(string family) => family switch { "Terrains" => "GoldValue", "Rooms" or "Doors" => "Cost", "Traps" => "GoldCost", "Spells" => "ManaCost", "Creatures" => "Health", _ => "Range" };
    public static void Run(Action<string, Action> run)
    {
        run("compiler dates: five generated headers use literal GAMEDATE offsets in targets13/16/17; Map.kld is untouched", () =>
        {
            foreach (var version in new[] { 13, 16, 17 })
            {
                var project = Project(version); var mapBytes = project.TileFile.Data.ToArray(); var result = Compile(project, gameBasis.Value);
                Fresh(result.Output.MainFile, result.Before, result.After);
                foreach (var signature in new uint[] { 0xB4, 0xBE, 0xD2, 0xE6 })
                {
                    var file = result.Output.Files.Single(file => file.Signature == signature && file.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
                    Fresh(file, result.Before, result.After, signature == 0xD2 ? 32 : 28);
                }
                Require(result.Output.TileFile.Data.Span.SequenceEqual(mapBytes), "GAMEDATE overwrote the date-free map header or tile data.");
            }
        });
        run("compiler dates: seven global families copy dates for defaults and regenerate only source-changed values", () =>
        {
            var basis = Basis();
            foreach (var family in LegacyGlobalSchemas.All)
            {
                var project = Project(); var row = Row(project, family.Group); var field = Field(family.Group);
                var original = project.GlobalDefinitionValues(row).Single(value => value.Name == field).Value;
                var changed = checked(long.Parse(original) + 1).ToString();
                NativeFile Family(NativeMapDocument document) => document.Files.Single(file => file.Signature == family.Signature);
                var copy = Compile(project, basis); Require(DateBytes(Family(copy.Output)).AsSpan().SequenceEqual(Sentinel()), "Default-backed family date was regenerated.");
                project.SetGlobalDefinitionValues([new(family.Group, row.Index, field, changed)]);
                var generated = Compile(project, basis); Fresh(Family(generated.Output), generated.Before, generated.After);
                project.SetGlobalDefinitionValues([new(family.Group, row.Index, field, original)]);
                var materializedDefault = Compile(project, basis);
                Require(DateBytes(Family(materializedDefault.Output)).AsSpan().SequenceEqual(Sentinel()), "An optional array containing original defaults was mistaken for a source change.");
            }
        });
        run("compiler dates: source change is decided before WORD truncation, not by equal generated payload", () =>
        {
            var project = Project(); var basis = Basis(); var row = Row(project, "Rooms");
            project.SetGlobalDefinitionValues([new("Rooms", row.Index, "Cost", "65536")]);
            var result = Compile(project, basis); var native = result.Output.GlobalDefinitionRows().Single(item => item.Family == "Rooms" && item.Index == row.Index).Record!;
            Require(native.Read(native.Fields.Single(field => field.Name == "Cost")) == "0", "Fixture no longer exercises source WORD truncation.");
            Fresh(native.File, result.Before, result.After);
        });
        run("compiler dates: unverified future global header is never stamped as the known GAMEDATE layout", () =>
        {
            var project = Project(); var basis = Basis(); var row = Row(project, "Rooms");
            project.SetGlobalDefinitionValues([new("Rooms", row.Index, "Cost", "1")]);
            var file = basis.Files.Single(file => file.Signature == 0x78); var version = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(version, 99); Raw(file, 4, version);
            var source = project.Files.ToDictionary(item => item, item => item.Data.ToArray()); var before = basis.Files.ToDictionary(item => item, item => item.Data.ToArray());
            var refused = false;
            try { _ = project.CompileLegacyProject(GameName, basis); } catch (InvalidDataException) { refused = true; }
            Require(refused && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && source.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Unknown header was interpreted or failure changed source bytes.");
        });
        run("compiler dates: raw native SaveCopy keeps old dates and generated seven-file Save/reopen keeps new dates", () =>
        {
            var directory = Path.Combine(Path.GetTempPath(), "DK2CompilerDates-" + Guid.NewGuid().ToString("N"));
            var native = NativeMapDocument.Create(32, 30, GameName, gameBasis.Value); Raw(native.MainFile, 28, Sentinel());
            var plainPath = native.SaveCopy(Path.Combine(directory, "plain"));
            Require(DateBytes(NativeMapDocument.Load(plainPath).MainFile).AsSpan().SequenceEqual(Sentinel()), "Raw native SaveCopy was changed into a recompilation/date repair.");
            var project = Project(); var result = Compile(project, gameBasis.Value);
            var path = result.Output.SaveCopy(Path.Combine(directory, "compiled"));
            Require(Path.GetFileNameWithoutExtension(path) == GameName && Directory.GetFiles(Path.GetDirectoryName(path)!).Length == 7, "Date generation changed the required preview name or seven-file bundle.");
            var reopened = NativeMapDocument.Load(path); Fresh(reopened.MainFile, result.Before, result.After);
            Require(DateBytes(reopened.MainFile).AsSpan().SequenceEqual(DateBytes(result.Output.MainFile)), "Publication changed the generated header date.");
        });
    }
}
