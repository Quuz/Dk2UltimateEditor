using System.Globalization;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class RegionGateGenerationChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new InvalidOperationException("Invalid region generation accepted.");}
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(NativeMapDocument document,Dictionary<NativeFile,byte[]> bytes)=>Expect(document.Files.Count==bytes.Count&&bytes.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Region generation changed unexpected buffers.");
    private static (NativeMapDocument Document,NativeRecord Gate) Fixture(bool project)
    {
        var document=NativeMapDocument.CreateNewMap(new("NestedGates","NestedGates",32,32,Players:3,NativeFormat:!project));
        document.Edit("Gate sites",()=>
        {
            document.Paint([new(3,3)],new(29,1,1));
            document.Paint(MapDrawing.Rectangle(new(8,8,2,2),true),new(33,1,1));
            document.Paint(MapDrawing.Rectangle(new(15,15,3,1),true),new(37,1,1));
            document.Paint(MapDrawing.Rectangle(new(23,23,1,3),true),new(37,1,2));
        });
        return (document,document.AddMapRegion(MapRegionKind.Gate,new(8,8,1,1)));
    }
    private static string Read(NativeRecord record,string field)=>record.Read(record.Fields.Single(value=>value.Name==field));
    private static void Set(NativeMapDocument document,NativeRecord record,string field,string value)=>document.SetField(record,record.Fields.Single(item=>item.Name==field),value);
    public static void Run()
    {
        foreach(var project in new[]{false,true})
        {
            Matrix(project);Lifecycle(project);Composition(project);Malformed(project);
        }
    }
    private static void Matrix(bool project)
    {
        var (document,gate)=Fixture(project);var original=Bytes(document);var changes=0;document.Changed+=(_,_)=>changes++;
        for(var bits=0;bits<64;bits++)
        {
            var flags=(HeroGateGenerationFlags)((uint)(bits&7)|((uint)(bits&24)<<6)|((uint)(bits&32)<<15));
            if((bits&32)!=0&&(bits&10)!=10)continue;
            for(var keepers=1;keepers<=5;keepers++)
            {
                using var parent=document.CreateRegionPropertiesSession(gate);var count=document.EditorChangeCount;var revision=document.EditRevision;var history=document.GetHistory();
                var stats=document.ReadActivityStatistics()!.Encode();var notifications=changes;
                using var child=document.BeginRegionGateGeneration(parent);
                var options=child.Defaults with{Flags=flags,Keepers=keepers};
                var plan=document.PreviewRegionGateGeneration(child,options);
                var gates=(bits&4)!=0?3:(bits&1)!=0?2:0; // Selected destroyable gate already exists.
                var areas=(bits&16)!=0?4:(bits&8)!=0?1:0;
                var conditions=(bits&32)!=0?keepers:0;
                Expect(plan.Gates==gates&&plan.Areas==areas&&plan.Conditions==conditions&&plan.Actions==conditions,"Private generator counts differ from independent source allocation rules.");
                Exact(document,original);Expect(changes==notifications&&document.EditRevision==revision&&document.EditorChangeCount==count,"Child preview mutated live counters/events.");
                if(plan.HasWork)Reject(()=>document.AcceptRegionGateGeneration(plan));
                document.AcceptRegionGateGeneration(plan,true);
                Exact(document,original);Expect(changes==notifications&&document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(stats),"Accepted child changed live bytes/activity.");
                var prepared=document.PrepareRegionProperties(parent);document.ApplyEditorRegionProperties(prepared);
                var increments=(uint)(1+gates+areas+conditions*2+(conditions>0?1:0));
                Expect(document.EditorChangeCount==unchecked(count+increments)&&changes==notifications+1
                    &&document.GetHistory().Undo.Count==history.Undo.Count+1,"Parent did not commit one Undo and the composed source counter.");
                Expect(ScriptGraph.Build(document).Events.Count==conditions*2,"Child events were lost or duplicated.");
                Expect(original.Where(row=>row.Key!=gate.File&&row.Key.Signature is not (0xD2 or NativeMapDocument.LegacyEvents))
                    .All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Generation changed unrelated files/tiles/definitions/metadata.");
                Expect(gate.File.Data.Span.Slice(gate.Offset,gate.Length).SequenceEqual(original[gate.File].AsSpan(gate.Offset,gate.Length)),"Equal parent changed imported gate bytes.");
                var after=Bytes(document);document.Undo();Exact(document,original);Expect(document.EditorChangeCount==count,"Parent Undo lost source counter.");
                document.Redo();Exact(document,after);Expect(document.EditorChangeCount==unchecked(count+increments),"Parent Redo lost source counter.");
                document.Undo();Exact(document,original);
            }
        }
    }
    private static void Lifecycle(bool project)
    {
        var (document,gate)=Fixture(project);using var parent=document.CreateRegionPropertiesSession(gate);
        var values=parent.Values;
        if(project)values=values with{Colour=0x123456,TextColour=0x654321,Name="Parent only"};
        document.UpdateRegionProperties(parent,values);var oldPlan=document.PrepareRegionProperties(parent);
        var bytes=Bytes(document);var revision=document.EditRevision;var count=document.EditorChangeCount;var history=document.GetHistory();
        var child=document.BeginRegionGateGeneration(parent);
        Expect(child.Defaults.Flags==HeroGateGenerationFlags.All&&child.Defaults.Keepers==3
            &&child.Defaults.Gate.Color==values.Colour&&child.Defaults.Gate.TextColor==values.TextColour
            &&child.Defaults.Gate.Name=="Gate %g"&&child.Defaults.Area.Color==0xFFFF00,"Child defaults lost source parent colours/default templates.");
        Reject(()=>document.BeginRegionGateGeneration(parent));Reject(()=>document.BeginRegionPropertiesPick(parent));
        Reject(()=>document.PrepareRegionProperties(parent));Reject(()=>document.ApplyEditorRegionProperties(oldPlan));
        var plan=document.PreviewRegionGateGeneration(child,child.Defaults);
        document.CancelRegionGateGeneration(child);document.CancelRegionGateGeneration(child);
        Reject(()=>document.AcceptRegionGateGeneration(plan,true));Exact(document,bytes);
        Expect(document.EditRevision==revision&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history.Undo.Count,"Cancelled child changed live history.");
        child=document.BeginRegionGateGeneration(parent);plan=document.PreviewRegionGateGeneration(child,child.Defaults);
        document.UpdateRegionProperties(parent,parent.Values with{Bounds=new(9,9,1,1)});
        Reject(()=>document.AcceptRegionGateGeneration(plan,true));document.CancelRegionGateGeneration(child);
        child=document.BeginRegionGateGeneration(parent);plan=document.PreviewRegionGateGeneration(child,child.Defaults);
        Reject(()=>Fixture(project).Document.AcceptRegionGateGeneration(plan,true));Reject(()=>Fixture(project).Document.CancelRegionGateGeneration(child));
        document.CancelRegionGateGeneration(child);
        using(var area=document.CreateRegionPropertiesSession(document.AddMapRegion(MapRegionKind.Area,new(1,1,1,1))))Reject(()=>document.BeginRegionGateGeneration(area));
        using(var stale=document.CreateRegionPropertiesSession(gate))
        {
            child=document.BeginRegionGateGeneration(stale);plan=document.PreviewRegionGateGeneration(child,child.Defaults);
            document.Paint([new(2,2)],new(4,2,1));document.Undo();
            Reject(()=>document.AcceptRegionGateGeneration(plan,true));document.CancelRegionGateGeneration(child);
        }
        using(var raw=document.CreateRegionPropertiesSession(gate))
        {
            child=document.BeginRegionGateGeneration(raw);plan=document.PreviewRegionGateGeneration(child,child.Defaults);
            Expect(MemoryMarshal.TryGetArray(gate.File.Data,out var segment),"Fixture memory unavailable.");
            var offset=segment.Offset+gate.Offset+(project?80:16);var previous=segment.Array![offset];segment.Array[offset]^=0x40;
            Reject(()=>document.AcceptRegionGateGeneration(plan,true));segment.Array[offset]=previous;document.CancelRegionGateGeneration(child);
        }
        var closing=document.CreateRegionPropertiesSession(gate);child=document.BeginRegionGateGeneration(closing);plan=document.PreviewRegionGateGeneration(child,child.Defaults);closing.Dispose();
        Reject(()=>document.AcceptRegionGateGeneration(plan,true));document.CancelRegionGateGeneration(child);
    }
    private static void Composition(bool project)
    {
        var (document,gate)=Fixture(project);var before=Bytes(document);var count=document.EditorChangeCount;var history=document.GetHistory();
        using(var cancel=document.CreateRegionPropertiesSession(gate))
        {
            using var child=document.BeginRegionGateGeneration(cancel);document.AcceptRegionGateGeneration(document.PreviewRegionGateGeneration(child,child.Defaults),true);
            Expect(document.PrepareRegionProperties(cancel).Changed,"Accepted child is absent from prepared result.");Exact(document,before);
        }
        Exact(document,before);Expect(document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history.Undo.Count,"Outer Cancel committed child data.");
        using var parent=document.CreateRegionPropertiesSession(gate);
        var first=document.BeginRegionGateGeneration(parent);
        document.AcceptRegionGateGeneration(document.PreviewRegionGateGeneration(first,first.Defaults with{Flags=HeroGateGenerationFlags.All&~HeroGateGenerationFlags.Events}),true);
        var beforeSecond=document.PrepareRegionProperties(parent);
        var second=document.BeginRegionGateGeneration(parent);var plan=document.PreviewRegionGateGeneration(second,second.Defaults);
        Expect(plan.Gates==0&&plan.Areas==0&&plan.Conditions==3,"Second child did not see the accepted primitives.");
        document.AcceptRegionGateGeneration(plan,true);Reject(()=>document.ApplyEditorRegionProperties(beforeSecond));
        var noWork=document.BeginRegionGateGeneration(parent);plan=document.PreviewRegionGateGeneration(noWork,noWork.Defaults);
        Expect(!plan.HasWork,"Repeated child duplicated generated structures/events.");document.AcceptRegionGateGeneration(plan);
        document.UpdateRegionProperties(parent,parent.Values with{Bounds=new(10,10,1,1)});
        document.ApplyEditorRegionProperties(document.PrepareRegionProperties(parent));
        Expect(document.EditorChangeCount==unchecked(count+15)&&document.GetHistory().Undo.Count==history.Undo.Count+1,"Repeated children/point change altered the composed allocation count.");
        var graph=ScriptGraph.Build(document);var actions=graph.Events.Values.Where(item=>item.Type?.Name=="ACTION_DESTROYGATE").ToArray();
        Expect(actions.Length==3&&document.GetMapRegion(gate)!.Bounds==new TileRect(10,10,1,1),"Final parent geometry or generated actions missing.");
        foreach(var action in actions)
            Expect(project?Read(action.Record,"param5")==Read(gate,"id"):Read(action.Record,"NativeArgument5")=="11"&&Read(action.Record,"NativeArgument7")=="11","Staged action lost its physical gate after parent moved.");
        var directory=Path.Combine(Path.GetTempPath(),"DK2-RegionGeneration-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var path=SaveProjectionChecks.SaveCopy(document,Path.Combine(directory,"saved"));var reopened=NativeMapDocument.Load(path);
            Expect(ScriptGraph.Build(reopened).Events.Count==6&&reopened.MapRegions().Count(region=>region.Kind==MapRegionKind.Gate)==4,"Staged generation did not survive real Save/reopen.");
            if(project)Expect(ScriptGraph.Build(document.CompileLegacyProject("NestedCompiled",NativeMapDocument.Create(32,32,"Basis"))).Events.Count==6,"Staged project cannot compile.");
        }
        finally{Directory.Delete(directory,true);}
        document.Undo();Exact(document,before);Expect(document.EditorChangeCount==count,"Composed Undo failed.");
    }
    private static void Malformed(bool project)
    {
        var (document,gate)=Fixture(project);var area=document.AddMapRegion(MapRegionKind.Area,new(7,7,4,4));
        var owner=document.ScriptOwners().Single(owner=>owner.Scope==4&&owner.Record.File==area.File&&owner.Record.Offset==area.Offset);
        var root=document.EnsureScriptScope(owner);var entry=document.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
        Set(document,entry,project?"and":"ChildAnd","12345");
        using var parent=document.CreateRegionPropertiesSession(gate);using var child=document.BeginRegionGateGeneration(parent);
        var bytes=Bytes(document);var count=document.EditorChangeCount;var revision=document.EditRevision;var history=document.GetHistory();
        Reject(()=>document.PreviewRegionGateGeneration(child,child.Defaults));Exact(document,bytes);
        Expect(document.EditorChangeCount==count&&document.EditRevision==revision&&document.GetHistory().Undo.Count==history.Undo.Count,"Failed child published allocations/counter/history.");
        document.CancelRegionGateGeneration(child);document.ApplyEditorRegionProperties(document.PrepareRegionProperties(parent));
        Expect(document.EditorChangeCount==unchecked(count+1)&&ScriptGraph.Build(document).Events.Count==1,"Failed child leaked an approved generation command.");document.Undo();Exact(document,bytes);
    }
}
