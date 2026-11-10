using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class EditorTransformCallerChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error) when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Refused editor transform was accepted.");}
    public static void Run()
    {
        using var fixture=JsonDocument.Parse(typeof(EditorTransformCallerChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.EditorTransformCallersSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var hash in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,hash.Name)))).Equals(hash.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Transform caller source changed.");
        Expect(fixture.RootElement.GetProperty("AcceptanceIncrements").GetInt32()==1&&fixture.RootElement.GetProperty("EachMovedThingIncrements").GetInt32()==1
            &&fixture.RootElement.GetProperty("ProtectedEdgeThingsCounted").GetBoolean(),"Original composed transform counter recipe differs.");
        foreach(var native in new[]{false,true})
        foreach(var mode in Enumerable.Range(0,4))
        foreach(var equal in new[]{false,true})
        foreach(var edge in new[]{false,true})
        {
            var doc=NativeMapDocument.CreateNewMap(new("TransformCaller","TransformCaller",12,12,NativeFormat:native));doc.SetHistoryDepth(65535);
            var rect=new TileRect(edge?0:2,2,4,4);
            var original=new MapTile[16];
            for(var y=0;y<4;y++)for(var x=0;x<4;x++)
            {var tile=new MapTile(equal?(byte)1:(byte)(3+(y*4+x)%6),2,1);original[y*4+x]=tile;doc.Paint([new(rect.X+x,rect.Y+y)],tile);}
            ushort eventID=0;var creaturePoint=new TilePoint(edge?0:1,2);
            if(!equal)
            {
                var creature=native?doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEKEEPER"),new(rect.X+creaturePoint.X,rect.Y+creaturePoint.Y))
                    :doc.AddLegacyRecord("Creature",new(rect.X+creaturePoint.X,rect.Y+creaturePoint.Y));
                var owner=doc.ScriptOwners().Single(value=>value.Scope==2);var binding=doc.EnsureScriptScope(owner);
                var trigger=doc.InsertScriptEvent(binding.Owner,binding.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));eventID=ScriptGraph.ID(trigger);
            }
            var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var counter=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;var usage=doc.ReadActivityStatistics()!.Encode();
            var identities=doc.ScriptBindings().Select(binding=>(binding.Root.Scope,binding.Root.Target,binding.Owner?.Record.Category)).ToArray();
            var notifications=0;EventHandler observer=(_,_)=>notifications++;doc.Changed+=observer;
            TilePoint Move(int x,int y)=>mode switch{0=>new(3-y,x),1=>new(y,3-x),2=>new(3-x,y),_=>new(x,3-y)};
            doc.TransformEditorSelection(rect,mode<2,horizontal:mode==2,clockwise:mode==0);
            var expectedCount=unchecked(counter+(equal?1u:2u));
            Expect(doc.EditorChangeCount==expectedCount&&doc.GetHistory().Undo.Count==history+1&&notifications==1,"Transform acceptance plus source removal count/equal Undo/notification differs.");
            Expect(doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(usage),"Transform invented paint/click activity.");
            for(var y=0;y<4;y++)for(var x=0;x<4;x++)
            {
                var target=Move(x,y);var world=new TilePoint(rect.X+target.X,rect.Y+target.Y);
                if(doc.IsEditorProtectedCell(world.X,world.Y))continue;
                var expected=doc.IsEditorProtectedCell(rect.X+x,rect.Y+y)?new MapTile(1,2,1):original[y*4+x];
                Expect(doc.GetTile(world.X,world.Y)==expected,"Independent source tile transform/protection permutation differs.");
            }
            if(equal)Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Equal source acceptance rewrote uniform bytes.");
            else
            {
                var moved=Move(creaturePoint.X,creaturePoint.Y);var creature=doc.Records().Single(record=>record.Category==(native?"GAMEKEEPER":"Project.MAPTHING"));
                string Read(string project,string game)=>creature.Read(creature.Fields.Single(field=>field.Name==(native?game:project)));
                Expect(Read("x","dwKeeperLocationX")== (rect.X+moved.X).ToString()&&Read("y","dwKeeperLocationY")== (rect.Y+moved.Y).ToString(),"Physical thing did not move through a protected edge.");
                Expect(ScriptGraph.Build(doc).Events.ContainsKey(eventID)&&doc.ScriptBindings().Select(binding=>(binding.Root.Scope,binding.Root.Target,binding.Owner?.Record.Category)).SequenceEqual(identities),"Transform changed script identity/owner links.");
            }
            var after=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());doc.Changed-=observer;
            doc.Undo();Expect(doc.EditorChangeCount==counter&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Transform Undo lost original bytes/count.");
            doc.Redo();Expect(doc.EditorChangeCount==expectedCount&&after.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Transform Redo lost result/count.");
        }
        CheckComposedRemovalCounts();
        foreach(var native in new[]{false,true})
        {
            var doc=NativeMapDocument.CreateNewMap(new("TransformGuards","TransformGuards",12,12,NativeFormat:native));
            var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
            for(var mode=0;mode<4;mode++)doc.TransformSelection(new(2,2,4,4),mode<2,horizontal:mode==2,clockwise:mode==0,respectEditorRules:true);
            Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Generic equal transform acquired editor acceptance.");
            Reject(()=>doc.TransformEditorSelection(new(2,2,4,3),true));Reject(()=>doc.TransformEditorSelection(new(2,2,1,3),false,true));
            Reject(()=>doc.TransformEditorSelection(new(2,2,3,1),false,false));Reject(()=>doc.TransformEditorSelection(new(-1,2,4,4),true));
            Reject(()=>doc.TransformEditorSelection(new(10,10,4,4),true));
            doc.Edit("Nested transform",()=>Reject(()=>doc.TransformEditorSelection(new(2,2,4,4),true)));
            Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Refused transform changed bytes/count/history.");
            doc.TransformEditorSelection(new(2,2,1,1),true);Expect(doc.EditorChangeCount==unchecked(count+1),"Source single-cell square rotation was wrongly refused.");doc.Undo();
            typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(doc,uint.MaxValue);
            doc.TransformEditorSelection(new(2,2,4,4),false,true);Expect(doc.EditorChangeCount==0,"Transform32bit source count did not wrap.");doc.Undo();Expect(doc.EditorChangeCount==uint.MaxValue,"Wrapped transform Undo count differs.");
            doc.SetHistoryDepth(0);doc.TransformEditorSelection(new(2,2,4,4),true);
            Expect(doc.EditorChangeCount==0&&!doc.CanUndo&&doc.GetHistory().SnapshotBytes==0,"Zero-depth transform lost accepted count or retained bytes.");
            Console.WriteLine($"TRANSFORM CALLER DETAIL native={native} four modes x equal/asymmetric x edge/interior; literal tile/thing permutations, source acceptance1/oneUndo, identities/activity, raw no-op, singlecell/size/nested/bounds/wrap/history0 guards");
        }
    }

    private static void CheckComposedRemovalCounts()
    {
        foreach(var native in new[]{false,true})
        foreach(var mode in Enumerable.Range(0,4))
        foreach(var movedCount in new[]{0,1,3,5,13})
        foreach(var edge in new[]{false,true})
        {
            var doc=NativeMapDocument.CreateNewMap(new("TransformRemoval","TransformRemoval",12,12,NativeFormat:native));
            doc.SetHistoryDepth(65535);
            var rectangle=new TileRect(edge?0:2,2,4,4);
            var position=new TilePoint(rectangle.X,rectangle.Y+1);
            NativeRecord Add(int index,TilePoint point)
            {
                var kind=new[]{"Creature","Door","Trap","Object","MagicalObject"}[index%5];
                var structure=kind switch{"Creature"=>new[]{"GAMEKEEPER","GAMENEUTRAL","GAMEHERO"}[(index/5)%3],"Door"=>"GAMEDOOR","Trap"=>"GAMETRAP",_=>"GAMEOBJECT"};
                var record=native?doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure==structure),point):doc.AddLegacyRecord(kind,point);
                var name=native?kind switch{"Creature"=>"bGlobalCreatureID","Door"=>"bDoorID","Trap"=>"bTrapID",_=>"bObjectID"}:"id";
                doc.SetField(record,record.Fields.Single(field=>field.Name==name),(kind=="MagicalObject"?117:index+1).ToString());
                return record;
            }
            for(var i=0;i<movedCount;i++)Add(i,position);
            // Neither a placed object outside the rectangle nor region records inside
            // the rectangle pass through the source's five physical removal lists.
            Add(0,new(8,8));
            var area=native?doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEAREA"),position):doc.AddLegacyRecord("Area",position);
            var areaBytes=area.File.Data.Slice(area.Offset,area.Length).ToArray();
            var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;var activity=doc.ReadActivityStatistics()!.Encode();
            var expected=unchecked(count+1u+(uint)movedCount);
            var notices=0;EventHandler notice=(_,_)=>notices++;doc.Changed+=notice;
            doc.TransformEditorSelection(rectangle,mode<2,horizontal:mode==2,clockwise:mode==0);
            doc.Changed-=notice;
            Expect(doc.EditorChangeCount==expected&&doc.GetHistory().Undo.Count==history+1&&notices==1,"Composed source removal helper count differs, including protected edge and stacked families.");
            Expect(doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),"Transform removal increments invented activity usage.");
            Expect(area.File.Data.Span.Slice(area.Offset,area.Length).SequenceEqual(areaBytes),"Transform counted or moved a nonphysical area.");
            var moved=mode switch{0=>new TilePoint(2,0),1=>new TilePoint(1,3),2=>new TilePoint(3,1),_=>new TilePoint(0,2)};
            var objects=doc.CopySelection(new(0,0,12,12)).ObjectCount;
            Expect(objects==movedCount+1,"Transform lost or duplicated a placed object.");
            var atDestination=doc.CopySelection(new(rectangle.X+moved.X,rectangle.Y+moved.Y,1,1)).ObjectCount;
            Expect(atDestination==movedCount,"Independent transformed destination differs for stacked source families.");
            Expect(doc.CopySelection(new(8,8,1,1)).ObjectCount==1,"Transform moved/count included the unrelated outside object.");
            var after=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            doc.Undo();Expect(doc.EditorChangeCount==count&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Composed transform Undo lost full data/count.");
            doc.Redo();Expect(doc.EditorChangeCount==expected&&after.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Composed transform Redo lost full data/count.");
        }
        Console.WriteLine("TRANSFORM REMOVAL DETAIL 80 native/project x four modes x placed0/1/3/5/13 x protected/interior; five source families, three native creature signatures, outside objects/nonphysical regions excluded, literal 1+N counter, one Undo, full bytes and activity");
    }
}
