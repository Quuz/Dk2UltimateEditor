using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

internal static class CompilerThingOrderChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static readonly Lazy<NativeMapDocument> basis = new(() => NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd"));
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Set(NativeMapDocument document, NativeRecord record, string field, string value)
        => document.SetField(record, record.Fields.Single(item => item.Name == field), value);
    private static NativeMapDocument Project(int version, bool reverse, bool things, bool regions, bool heart)
    {
        var project = NativeMapDocument.CreateNewMap(new("ThingOrder", "ThingOrder", 40, 36, PlaceHearts: false));
        var info = project.Records().Single(item => item.Category == "Project.MAPPROPERTIES"); Set(project, info, "Version", version.ToString());
        if (things)
        {
            (string Kind, int X, int Y, byte Owner)[] input =
            [
                ("Creature",4,4,3), ("Door",8,3,3), ("Object",7,4,3), ("Trap",10,6,3),
                ("MagicalObject",13,3,3), ("Creature",5,5,1), ("Object",9,5,3),
                ("MagicalObject",14,4,3), ("Trap",11,7,3), ("Door",8,6,3), ("Creature",6,6,2)
            ];
            foreach (var item in reverse ? input.Reverse() : input)
            {
                var record = project.AddLegacyRecord(item.Kind, new(item.X, item.Y), item.Owner);
                if (item.Kind == "Creature" && item.Owner == 1) Set(project, record, "ennemy", "3");
            }
        }
        if (regions)
        {
            (string Kind, int X, int Y)[] input =
            [("Effect",5,12),("Gate",9,12),("Area",14,12),("Effect",6,13),("Gate",10,13),("Area",15,13),("Band",0,0)];
            foreach (var item in reverse ? input.Reverse() : input) project.AddLegacyRecord(item.Kind, new(item.X, item.Y));
        }
        if (heart) project.Paint(from y in Enumerable.Range(20,5) from x in Enumerable.Range(20,5) select new TilePoint(x,y), new(14,3,1));
        return project;
    }
    private static string[] Expected(bool reverse, bool things, bool regions, bool heart)
    {
        var result = new List<string>();
        void Group(params string[] values) => result.AddRange(reverse ? values.Reverse() : values);
        if (things)
        {
            Group("C2:7,4","C2:9,5"); Group("C2:13,3","C2:14,4");
            Group("C3:10,6","C3:11,7"); Group("C4:8,3","C4:8,6");
            Group("C8:4,4","C7:5,5","C6:6,6");
        }
        if (regions) { Group("C5:14,12","C5:15,13"); Group("CC:9,12","CC:10,13"); }
        if (heart) result.Add("CC:22,22");
        if (regions) { Group("CB:5,12","CB:6,13"); result.Add("band"); }
        return result.ToArray();
    }
    private static NativeFile Things(NativeMapDocument document) => document.Files.Single(file => file.Signature == 0xBE);
    private static string[] Tags(NativeMapDocument document)
    {
        var bytes = Things(document).Data.Span; var result = new List<string>(); var offset = 56;
        Require(bytes.Length >= 56 && BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]) == bytes.Length, "Invalid things header size.");
        while (offset < bytes.Length)
        {
            Require(offset <= bytes.Length - 8, "Truncated record header.");
            var signature = BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
            var size = (long)BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset+4)..]) + 8;
            Require(size >= 16 && size <= bytes.Length - offset, "Invalid physical record size.");
            result.Add(signature == 0xC9 ? "band" : signature.ToString("X") + ":" + BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset+8)..]) + "," + BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset+12)..]));
            offset += (int)size;
        }
        Require(result.Count == BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]), "Physical count differs from things header.");
        return result.ToArray();
    }
    private static NativeMapDocument Compile(NativeMapDocument source)
    {
        var snapshots = source.Files.Concat(basis.Value.Files).ToDictionary(file => file, file => file.Data.ToArray());
        var revision = source.EditRevision; var dirty = source.HasChanges; var before = source.GetHistory();
        var notices = 0; EventHandler changed = (_, _) => notices++; source.Changed += changed; source.HistoryChanged += changed;
        NativeMapDocument result;
        try { result = source.CompileLegacyProject(GameName, basis.Value); }
        finally { source.Changed -= changed; source.HistoryChanged -= changed; }
        var after = source.GetHistory();
        Require(snapshots.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && revision == source.EditRevision && dirty == source.HasChanges
            && notices == 0 && before.ChangeCount == after.ChangeCount && before.EditorChangeCount == after.EditorChangeCount
            && before.SnapshotBytes == after.SnapshotBytes && before.Undo.SequenceEqual(after.Undo) && before.Redo.SequenceEqual(after.Redo), "Compilation changed input bytes/history.");
        return result;
    }
    public static void Run(Action<string, Action> run)
    {
        run("compiler things order: physical category order and stable within-category order in targets13/16/17", () =>
        {
            foreach (var version in new[]{13,16,17}) foreach (var reverse in new[]{false,true})
                Require(Tags(Compile(Project(version,reverse,true,false,false))).SequenceEqual(Expected(reverse,true,false,false)), "Things categories follow DKM storage instead of source writer calls.");
        });
        run("compiler things order: interleaved project regions become areas, gates, effects, bands", () =>
        {
            foreach (var version in new[]{13,16,17}) foreach (var reverse in new[]{false,true})
                Require(Tags(Compile(Project(version,reverse,false,true,false))).SequenceEqual(Expected(reverse,false,true,false)), "Region categories follow DKM storage instead of source writer calls.");
        });
        run("compiler things order: keeper hearts follow hero gates and precede effect generators", () =>
        {
            foreach (var version in new[]{13,16,17}) foreach (var reverse in new[]{false,true})
                Require(Tags(Compile(Project(version,reverse,true,true,true))).SequenceEqual(Expected(reverse,true,true,true)), "Hearts are not inside the source gates writer stage.");
        });
        run("compiler things order: DKM save/reopen and official seven-file save/reopen retain the full generated record stream", () =>
        {
            var directory = Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2ThingOrder-"+Guid.NewGuid().ToString("N")));
            Require(directory.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected temporary fixture root.");
            Directory.CreateDirectory(directory);
            try
            {
                var project=Project(17,true,true,true,true); var path=Path.Combine(directory,"working.dkm");
                using(var save=new EditorProjectSaveSession(project)) save.SaveAs(path);
                var loaded=NativeMapDocument.Load(path); var compiled=Compile(loaded);
                Require(Tags(compiled).SequenceEqual(Expected(true,true,true,true)),"DKM reopen changed source generation order.");
                var saved=compiled.SaveCopy(Path.Combine(directory,"game"));
                Require(Directory.GetFiles(Path.GetDirectoryName(saved)!).Length==7,"Game bundle is not seven files.");
                var reopened=NativeMapDocument.Load(saved);
                Require(Things(reopened).Data.Span.SequenceEqual(Things(compiled).Data.Span)
                    && Tags(reopened).SequenceEqual(Expected(true,true,true,true)),"Save/reopen changed the generated things stream.");
            }
            finally { Directory.Delete(directory,recursive:true); }
        });
    }
}
