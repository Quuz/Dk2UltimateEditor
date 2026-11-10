using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class CompilerDefaultGlobalChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string Field(string family) => family switch
    {
        "Terrains" => "GoldValue", "Rooms" or "Doors" => "Cost", "Traps" => "GoldCost",
        "Spells" => "ManaCost", "Creatures" => "Health", _ => "Range"
    };
    private static NativeFile File(NativeMapDocument document, string family)
        => document.Files.Single(file => file.Signature == LegacyGlobalSchemas.All.Single(item => item.Group == family).Signature);
    private static GlobalDefinitionRow Row(NativeMapDocument project, string family)
        => project.GlobalDefinitionRows().First(row => row.Family == family && row.Editable);
    private static byte[] Section(NativeMapDocument document, string family)
    {
        var signature = LegacyGlobalSchemas.All.Single(item => item.Group == family).Signature;
        var matches = new List<byte[]>();
        foreach (var file in document.Files)
        {
            var bytes = file.Data.Span;
            for (var offset = 0; offset <= bytes.Length - 12;)
            {
                var length = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 8)..]);
                if (length < 12 || length > bytes.Length - offset) break;
                if (BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]) == signature)
                    matches.Add(bytes.Slice(offset, (int)length).ToArray());
                offset += (int)length;
            }
        }
        Require(matches.Count == 1, "Expected one physical definition section: " + family);
        return matches[0];
    }
    private static void MaterializeDefaults(NativeMapDocument project, string family)
    {
        var row = Row(project, family); var name = Field(family);
        var original = project.GlobalDefinitionValues(row).Single(field => field.Name == name).Value;
        project.SetGlobalDefinitionValues([new(family, row.Index, name, original == "7" ? "8" : "7")]);
        project.SetGlobalDefinitionValues([new(family, row.Index, name, original)]);
        Require(Row(project, family).Record is not null, "Fixture did not leave an explicit project array.");
    }
    private static NativeMapDocument Project(int version = 17)
    {
        var project = NativeMapDocument.CreateNewMap(new("DefaultGlobals", "DefaultGlobals", 32, 30, PlaceHearts: false));
        var info = project.Records().Single(record => record.Category == "Project.MAPPROPERTIES");
        project.SetField(info, info.Fields.Single(field => field.Name == "Version"), version.ToString()); return project;
    }
    private static NativeMapDocument CustomBasis(string directory)
    {
        var basis = GlobalDefinitionNumericChecks.Fixture(directory);
        foreach (var family in LegacyGlobalSchemas.All)
        {
            var record = Row(basis, family.Group).Record!; var field = record.Fields.Single(field => field.Name == Field(family.Group));
            var value = record.Read(field) == "19" ? "23" : "19";
            Require(MemoryMarshal.TryGetArray(record.File.Data, out var bytes), "Fixture bytes unavailable.");
            field.Encode(value).CopyTo(bytes.Array!, bytes.Offset + record.Offset + field.Offset);
        }
        return basis;
    }
    private static NativeMapDocument Compile(NativeMapDocument project, NativeMapDocument basis)
    {
        var source = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var originals = basis.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var history = project.GetHistory(); var revision = project.EditRevision; var dirty = project.HasChanges;
        var notices = 0; var histories = 0;
        EventHandler changed = (_, _) => notices++; EventHandler hc = (_, _) => histories++;
        project.Changed += changed; project.HistoryChanged += hc;
        NativeMapDocument output;
        try { output = project.CompileLegacyProject(GameName, basis); }
        finally { project.Changed -= changed; project.HistoryChanged -= hc; }
        var after = project.GetHistory();
        Require(source.Count == project.Files.Count && source.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value))
            && originals.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && revision == project.EditRevision
            && dirty == project.HasChanges && notices == 0 && histories == 0 && history.EditorChangeCount == after.EditorChangeCount
            && history.ChangeCount == after.ChangeCount && history.SnapshotBytes == after.SnapshotBytes
            && history.Undo.SequenceEqual(after.Undo) && history.Redo.SequenceEqual(after.Redo), "Compilation changed source/basis bytes or source history.");
        return output;
    }
    public static void Run(Action<string, Action> run)
    {
        var directory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2DefaultGlobals-" + Guid.NewGuid().ToString("N")));
        Require(directory.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Unexpected temporary fixture root.");
        Directory.CreateDirectory(directory);
        try
        {
            var basis = CustomBasis(Path.Combine(directory, "basis"));
            foreach (var family in LegacyGlobalSchemas.All)
                run("compiler default globals: " + family.Group + " keeps every basis byte after edit-and-return in targets13/16/17", () =>
                {
                    foreach (var version in new[] { 13, 16, 17 })
                    {
                        var project = Project(version); var original = File(basis, family.Group).Data.ToArray();
                        Require(File(Compile(project, basis), family.Group).Data.Span.SequenceEqual(original), "Absent array did not copy the basis family.");
                        MaterializeDefaults(project, family.Group);
                        Require(File(Compile(project, basis), family.Group).Data.Span.SequenceEqual(original), "Default-valued array overwrote the basis: " + family.Group);
                    }
                });
            run("compiler default globals: a real override regenerates one family and preserves the other six materialized defaults", () =>
            {
                foreach (var changed in LegacyGlobalSchemas.All)
                {
                    var project = Project();
                    foreach (var family in LegacyGlobalSchemas.All) MaterializeDefaults(project, family.Group);
                    var row = Row(project, changed.Group); var name = Field(changed.Group);
                    project.SetGlobalDefinitionValues([new(changed.Group, row.Index, name, "13")]);
                    var output = Compile(project, basis);
                    var native = output.GlobalDefinitionRows().Single(item => item.Family == changed.Group && item.Index == row.Index).Record!;
                    Require(native.Read(native.Fields.Single(field => field.Name == name)) == "13"
                        && !File(output, changed.Group).Data.Span.SequenceEqual(File(basis, changed.Group).Data.Span), "A real override was skipped.");
                    foreach (var family in LegacyGlobalSchemas.All.Where(family => family.Group != changed.Group))
                        Require(File(output, family.Group).Data.Span.SequenceEqual(File(basis, family.Group).Data.Span), "An unrelated default family was rewritten: " + family.Group);
                }
            });
            run("compiler default globals: real seven-file save/reopen preserves complete packed definitions after returning all arrays to defaults", () =>
            {
                var gameBasis = NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
                var project = Project(); var plain = Compile(project, gameBasis);
                var plainPath = plain.SaveCopy(Path.Combine(directory, "plain"));
                foreach (var family in LegacyGlobalSchemas.All) MaterializeDefaults(project, family.Group);
                var explicitDefaults = Compile(project, gameBasis);
                var saved = explicitDefaults.SaveCopy(Path.Combine(directory, "explicit-defaults"));
                var mapDirectory = Path.GetDirectoryName(saved)!;
                Require(Directory.GetFiles(mapDirectory).Length == 7, "Game bundle is not seven files.");
                var globalsName = GameName + "Globals.kld";
                Require(System.IO.File.ReadAllBytes(Path.Combine(mapDirectory, globalsName)).AsSpan().SequenceEqual(
                    System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(plainPath)!, globalsName))), "Packed Globals changed solely because default arrays existed.");
                var reopened = NativeMapDocument.Load(saved);
                foreach (var family in LegacyGlobalSchemas.All)
                    Require(Section(reopened, family.Group).AsSpan().SequenceEqual(Section(plain, family.Group)), "Save/reopen changed a default family: " + family.Group);
            });
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
