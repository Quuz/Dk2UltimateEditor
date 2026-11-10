using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

internal static class GateTransformSemanticsChecks
{
    private const string Name="DK2Preview_6a88240eaf814f78";
    private const int Size=14;
    private static readonly TileRect Selection=new(3,3,3,3);
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static NativeMapDocument New(bool project)=>project
        ?NativeMapDocument.CreateNewMap(new(Width:Size,Height:Size,DisplayName:Name,FileName:Name))
        :NativeMapDocument.Create(Size,Size,Name);
    private static uint Number(NativeRecord r,string field)=>uint.Parse(r.Read(r.Fields.Single(f=>f.Name==field)));
    private static void Set(NativeMapDocument d,NativeRecord r,string field,uint value)=>d.SetField(r,r.Fields.Single(f=>f.Name==field),value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static byte[] Bytes(NativeRecord record)=>record.File.Data.Slice(record.Offset,record.Length).ToArray();
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes){foreach(var(f,data)in bytes)Require(f.Data.Span.SequenceEqual(data),"Transform changed unexpected buffer bytes.");}
    private static TilePoint Mapped(TilePoint p,int operation)
    {
        var x=p.X-3;var y=p.Y-3;
        var q=operation switch{0=>new TilePoint(2-y,x),1=>new TilePoint(y,2-x),2=>new TilePoint(2-x,y),_=>new TilePoint(x,2-y)};
        return new(q.X+3,q.Y+3);
    }
    private static MapTile[] Tiles(NativeMapDocument d)=>Enumerable.Range(0,Size*Size).Select(i=>d.GetTile(i%Size,i/Size)).ToArray();
    private static void Transform(NativeMapDocument d,int operation)=>d.TransformEditorSelection(Selection,rotate:operation<2,horizontal:operation==2,clockwise:operation==0);
    private static MapTile[] Expected(MapTile[] original,int operation)
    {
        var result=(MapTile[])original.Clone();
        for(var y=3;y<=5;y++)for(var x=3;x<=5;x++){var q=Mapped(new(x,y),operation);result[q.Y*Size+q.X]=original[y*Size+x];}
        return result;
    }
    private static void AssertTiles(NativeMapDocument d,MapTile[] expected)
    {Require(Tiles(d).SequenceEqual(expected),"Gate terrain/owner/variant/reserved did not follow the literal source permutation.");}
    private static NativeRecord[] Gates(NativeMapDocument d)=>d.Records().Where(r=>r.Category=="GAMEGATE").ToArray();
    private static NativeRecord Gate(NativeMapDocument d)=>Gates(d).Single(r=>Number(r,"bGateType")!=14);
    private static NativeRecord Region(NativeMapDocument d)=>d.Records().Single(r=>r.Category=="Project.MAPRECTEX"&&Number(r,"node.type")==1);
    private static NativeMapDocument Compile(NativeMapDocument d)=>d.CompileLegacyProject(Name,New(false));
    private static void OrientedTerrain(NativeMapDocument d,byte variant)
    {
        d.Paint([new(4,4)],new(37,1,variant));
        if(variant==1)
        {
            d.Paint([new(3,4),new(5,4)],new(37,1,1));
            d.Paint([new(4,5)],new(1,2,1));d.Paint([new(4,3)],new(8,3,1));
        }
        else
        {
            d.Paint([new(4,3),new(4,5)],new(37,1,2));
            d.Paint([new(5,4)],new(1,2,1));d.Paint([new(3,4)],new(8,3,1));
        }
    }
    public static void Run(Action<string,Action> run)
    {
        run("gate transform: all six gate catalogue variants move literally while native gate records/orientation and non-XY thing bytes stay exact",NativeLiteral);
        run("gate transform: unchanged project regions compile fresh source orientations after all four terrain transforms without variant swapping",ProjectOrientation);
        run("gate transform: moved gate terrain leaves the explicit project anchor fixed and source default type33 orientation0",ProjectAnchor);
        run("gate transform: native-source orientation/ID retained before editing and after Undo, regenerated after changed local geometry and Redo",NativeSourceRetention);
    }
    private static void NativeLiteral()
    {
        foreach(var operation in Enumerable.Range(0,4))
        {
            var d=New(false);var kinds=new(byte Terrain,byte Variant)[]{(28,1),(29,1),(33,1),(34,1),(37,1),(37,2)};
            for(var i=0;i<kinds.Length;i++)
            {
                var at=new TilePoint(3+i%3,3+i/3);var kind=kinds[i];d.Paint([at],new(kind.Terrain,1,kind.Variant,(byte)(191+i)));
                var gate=d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEGATE"),at,1);
                Set(d,gate,"bGateType",kind.Terrain);Set(d,gate,"bGateID",(uint)(91+i));Set(d,gate,"bGateOrientation",(uint)(i%4*2));
            }
            var hero=d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEHERO"),new(3,5),1);
            var original=Tiles(d);var before=Snapshot(d);var expected=before.ToDictionary(pair=>pair.Key,pair=>(byte[])pair.Value.Clone());
            var next=Expected(original,operation);
            for(var y=3;y<=5;y++)for(var x=3;x<=5;x++)BinaryPrimitives.WriteUInt32LittleEndian(expected[d.TileFile].AsSpan(36+(y*Size+x)*4),next[y*Size+x].Packed);
            var moved=Mapped(new(3,5),operation);
            foreach(var axis in new[]{("LocationX",moved.X),("LocationY",moved.Y)})
            {var field=hero.Fields.Single(f=>f.Name.EndsWith(axis.Item1,StringComparison.Ordinal));field.Encode(axis.Item2.ToString()).CopyTo(expected[hero.File],hero.Offset+field.Offset);}
            Transform(d,operation);AssertTiles(d,next);Exact(expected);
            d.Undo();Exact(before);d.Redo();Exact(expected);
        }
    }
    private static void ProjectOrientation()
    {
        foreach(var operation in Enumerable.Range(0,4))
        {
            var d=New(true);var variant=(byte)(operation==2?2:1);OrientedTerrain(d,variant);d.AddLegacyRecord("Gate",new(4,4));
            var region=Bytes(Region(d));var before=Snapshot(d);var original=Tiles(d);Transform(d,operation);
            AssertTiles(d,Expected(original,operation));Require(Bytes(Region(d)).AsSpan().SequenceEqual(region),"Terrain transform moved/reshaped a project gate region.");
            var gate=Gate(Compile(d));Require(Number(gate,"dwGateLocationX")==4&&Number(gate,"dwGateLocationY")==4&&Number(gate,"bGateType")==37
                &&Number(gate,"bGateOrientation")== (variant==1?4u:2u)&&Number(gate,"bGateID")==1,"Compiler did not use unchanged variant plus newly transformed south/east solid neighbour.");
            var after=Snapshot(d);d.Undo();Exact(before);d.Redo();Exact(after);
        }
    }
    private static void ProjectAnchor()
    {
        foreach(var operation in Enumerable.Range(0,4))
        {
            var d=New(true);d.Paint([new(3,3)],new(37,1,1,219));d.AddLegacyRecord("Gate",new(3,3));var region=Bytes(Region(d));
            var original=Tiles(d);Transform(d,operation);AssertTiles(d,Expected(original,operation));var gate=Gate(Compile(d));
            Require(Bytes(Region(d)).AsSpan().SequenceEqual(region)&&Number(gate,"dwGateLocationX")==3&&Number(gate,"dwGateLocationY")==3
                &&Number(gate,"bGateType")==33&&Number(gate,"bGateOrientation")==0,"Moved gate terrain invented/moved a region or retained a type37 source gate at an empty anchor.");
        }
    }
    private static void NativeSourceRetention()
    {
        var root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2GateTransforms-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(root);
        try
        {
            foreach(var operation in Enumerable.Range(0,4))
            {
                var source=New(false);var variant=(byte)(operation==2?2:1);OrientedTerrain(source,variant);
                var gate=source.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEGATE"),new(4,4),1);
                Set(source,gate,"bGateType",37);Set(source,gate,"bGateID",91);Set(source,gate,"bGateOrientation",(uint)(variant==1?0:6));
                var original=Bytes(Gate(source));var path=Path.Combine(root,operation+".dkm");
                using(var save=new EditorSaveSession(source)){var plan=save.PrepareSaveAs(path);Require(!plan.HasBlockingIssues,"Synthetic gate native-to-project save blocked.");save.Save(plan,true);}
                var d=NativeMapDocument.Load(path);Require(Bytes(Gate(Compile(d))).AsSpan().SequenceEqual(original),"Unedited native gate source bytes were not retained.");
                var before=Snapshot(d);Transform(d,operation);var after=Snapshot(d);var generated=Gate(Compile(d));
                Require(Number(generated,"bGateID")==1&&Number(generated,"bGateOrientation")== (variant==1?4u:2u),"Transform restored obsolete source-only gate ID/orientation despite changed local terrain.");
                d.Undo();Exact(before);Require(Bytes(Gate(Compile(d))).AsSpan().SequenceEqual(original),"Undo did not restore eligibility for exact original native gate bytes.");
                d.Redo();Exact(after);generated=Gate(Compile(d));Require(Number(generated,"bGateID")==1&&Number(generated,"bGateOrientation")== (variant==1?4u:2u),"Redo restored stale native gate bytes.");
            }
        }
        finally{Require(root.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture cleanup path.");Directory.Delete(root,recursive:true);}
    }
}
