using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class SelectionBandPlacementChecks
{
    private const string Name="DK2Preview_6a88240eaf814f78";
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static NativeMapDocument New(bool project)=>project
        ?NativeMapDocument.CreateNewMap(new(Width:14,Height:12,DisplayName:Name,FileName:Name))
        :NativeMapDocument.Create(14,12,Name);
    private static uint Number(NativeRecord record,string name)=>uint.Parse(record.Read(record.Fields.Single(f=>f.Name==name)));
    private static void Set(NativeMapDocument document,NativeRecord record,string name,uint value)=>document.SetField(record,record.Fields.Single(f=>f.Name==name),value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static NativeRecord Add(NativeMapDocument document,string project,string native,TilePoint at,byte owner=1)=>document.IsLegacyProject
        ?document.AddLegacyRecord(project,at,owner)
        :document.AddRecord(RecordTemplate.All.Single(t=>t.Structure==native),at,owner);
    private static NativeRecord Band(NativeMapDocument document)=>document.Records().Single(r=>r.Category is "GAMEBAND" || r.Category.StartsWith("Project.MTHBAND",StringComparison.Ordinal));
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document)=>document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> before){foreach(var(f,bytes)in before)Require(f.Data.Span.SequenceEqual(bytes),"History changed complete buffers.");}
    private static byte[] Bytes(NativeRecord row)=>row.File.Data.Slice(row.Offset,row.Length).ToArray();
    private static byte[][] GlobalRows(NativeMapDocument document)=>document.Records().Where(r=>r.Category is "GAMEBAND" or "GAMEAREA" or "GAMEGATE" or "GAMEEFFECT"
        || r.Category.StartsWith("BandHero",StringComparison.Ordinal) || r.Category.StartsWith("Project.MTHBAND",StringComparison.Ordinal) || r.Category=="Project.MAPRECTEX").Select(Bytes).ToArray();
    private static void SameGlobals(NativeMapDocument document,byte[][] expected)
    {var actual=GlobalRows(document);Require(actual.Length==expected.Length && actual.Zip(expected).All(pair=>pair.First.AsSpan().SequenceEqual(pair.Second)),"Spatial operation changed a global band/member/area/gate/effect record.");}
    private static void Script(NativeMapDocument document,uint scope)
    {
        var owner=document.ScriptOwners().First(o=>o.Scope==scope);var root=document.EnsureScriptScope(owner);
        document.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED"));
    }
    private static NativeMapDocument WithGlobals(bool project)
    {
        var d=New(project);var band=Add(d,"Band","GAMEBAND",new(3,3));Set(d,band,project?"id":"bBandID",23);
        d.AddBandMember(Band(d),17,4,3);d.AddBandMember(Band(d),19,8,5);
        foreach(var(kind,native)in new[]{("Area","GAMEAREA"),("Gate","GAMEGATE"),("Effect","GAMEEFFECT")})Add(d,kind,native,new(3,3));
        Script(d,8);Script(d,16);return d;
    }
    private static void History(NativeMapDocument document,Dictionary<NativeFile,byte[]> before)
    {var after=Snapshot(document);document.Undo();Exact(before);document.Redo();Exact(after);}
    public static void Run(Action<string,Action> run)
    {
        run("selection band placement: native global band/member scopes stay outside spatial copy, DKZ, cut and paste",()=>BandCase(false));
        run("selection band placement: project global band/member scopes stay outside spatial copy, DKZ, cut and paste",()=>BandCase(true));
        RunPhysical(run);
        run("selection band placement: partial heart/portal/gate terrain pastes literally without cursor-footprint coercion",()=>
        {
            foreach(var project in new[]{false,true})foreach(var terrain in new byte[]{14,12,40,28,33,34,37})
            {
                var d=WithGlobals(project);var globals=GlobalRows(d);var cell=new MapTile(terrain,1,1,97);
                d.Paint([new(3,3)],cell);var clipboard=SelectionFile.Parse(SelectionFile.Serialize(d.CopySelection(new(3,3,1,1),protectBoundary:true)));
                Require(clipboard.ObjectCount==0&&clipboard.ScriptCount==0,"Terrain copied a global region/band scope.");
                var before=Snapshot(d);var count=d.EditorChangeCount;var neighbour=d.GetTile(9,7);
                d.PasteEditorSelection(new(8,7),clipboard);
                Require(d.GetTile(8,7)==cell&&d.GetTile(9,7)==neighbour&&d.EditorChangeCount==unchecked(count+1),"Paste coerced the literal one-cell special terrain into a footprint or changed accepted count.");
                SameGlobals(d,globals);History(d,before);
            }
        });
    }
    public static void RunPhysical(Action<string,Action> run,bool nativeOnly=false)
    {
        run("selection band placement: native paste clones all five physical categories and exact payloads even on source-ineligible terrain",()=>PhysicalPaste(false));
        if(!nativeOnly)run("selection band placement: project paste clones all five physical categories and exact payloads even on source-ineligible terrain",()=>PhysicalPaste(true));
    }
    private static void BandCase(bool project)
    {
        var d=WithGlobals(project);Add(d,"Creature","GAMEHERO",new(3,3));
        var globals=GlobalRows(d);var scripts=d.Files.Where(f=>f.Signature is 0xD2 or NativeMapDocument.LegacyEvents).ToDictionary(f=>f,f=>f.Data.ToArray());
        var clipboard=d.CopySelection(new(2,2,4,3),protectBoundary:true);
        Require(clipboard.ObjectCount==1&&clipboard.ScriptCount==0,"Spatial copy included a global band/member scope or omitted the placed hero.");
        clipboard=SelectionFile.Parse(SelectionFile.Serialize(clipboard));Require(clipboard.ObjectCount==1&&clipboard.ScriptCount==0,"DKZ introduced a global owner.");
        var before=Snapshot(d);var count=d.EditorChangeCount;d.CutEditorSelection(new(2,2,4,3),new(2,2,1));
        Require(d.CopySelection(new(2,2,4,3)).ObjectCount==0&&d.EditorChangeCount==unchecked(count+2),"Cut removed a global entry or did not count exactly one placed hero.");
        SameGlobals(d,globals);Exact(scripts);History(d,before);
        before=Snapshot(d);count=d.EditorChangeCount;d.PasteEditorSelection(new(7,6),clipboard);
        Require(d.CopySelection(new(7,6,4,3)).ObjectCount==1&&d.EditorChangeCount==unchecked(count+1),"Paste counted band members as physical removals or copied them as placed heroes.");
        SameGlobals(d,globals);Exact(scripts);History(d,before);
    }
    private static void PhysicalPaste(bool project)
    {
        var d=WithGlobals(project);var globals=GlobalRows(d);
        foreach(var(kind,native)in new[]{("Creature","GAMEHERO"),("Door","GAMEDOOR"),("Trap","GAMETRAP"),("Object","GAMEOBJECT"),("MagicalObject","GAMEOBJECT")})
        {
            var record=Add(d,kind,native,new(3,3));
            if(kind=="MagicalObject"&&!project)Set(d,record,"bObjectID",0x75);
        }
        d.Paint([new(3,3)],new(4,2,1,199));
        var expected=d.Records().Where(Physical).ToDictionary(Kind,r=>(Record:r,Data:Bytes(r)));
        Require(expected.Count==5,"Fixture did not contain each of the five original physical categories.");
        var clipboard=SelectionFile.Parse(SelectionFile.Serialize(d.CopySelection(new(3,3,1,1),protectBoundary:true)));
        Require(clipboard.ObjectCount==5&&clipboard.ScriptCount==0,"Source copy failed to retain all stacked physical categories independently of terrain.");
        var before=Snapshot(d);d.PasteEditorSelection(new(8,7),clipboard);
        Require(d.GetTile(8,7)==new MapTile(4,2,1,199)&&d.CopySelection(new(8,7,1,1)).ObjectCount==5,"Paste incorrectly applied single-object CanPaint eligibility or dropped a physical category.");
        var actual=d.Records().Where(Physical).Where(r=>Number(r,r.Fields.Single(f=>f.Name=="x"||f.Name.EndsWith("LocationX",StringComparison.Ordinal)).Name)==8).ToArray();
        Require(actual.Length==5,"Placed physical payload count differs.");
        foreach(var record in actual)
        {
            var original=expected[Kind(record)];var data=Bytes(record);
            foreach(var field in record.Fields.Where(f=>f.Name is "x" or "y" or "uniqueid" || f.Name.EndsWith("LocationX",StringComparison.Ordinal)||f.Name.EndsWith("LocationY",StringComparison.Ordinal)))
                field.Encode(field.Read(original.Data)).CopyTo(data,field.Offset);
            Require(data.AsSpan().SequenceEqual(original.Data),"Paste changed physical category/owner/options/reserved payload outside relocated XY and fresh uniqueid.");
        }
        SameGlobals(d,globals);History(d,before);
    }
    private static bool Physical(NativeRecord record)=>record.Category is "Project.MAPTHING" or "GAMEHERO" or "GAMEDOOR" or "GAMETRAP" or "GAMEOBJECT";
    private static uint Kind(NativeRecord record)=>record.Category switch
    {
        "Project.MAPTHING"=>Number(record,"node.type"),"GAMEHERO"=>0,"GAMEDOOR"=>1,"GAMETRAP"=>2,
        "GAMEOBJECT"=>Number(record,"bObjectID") is >=0x75 and <=0x81?4u:3u,_=>throw new InvalidOperationException("Unexpected fixture category.")
    };
}
