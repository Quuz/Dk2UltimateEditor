using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class GateGenerationRequestChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Refuse(Action run){try{run();}catch(InvalidOperationException){return;}throw new Exception("Unsafe generator consent was accepted.");}
    private static NativeMapDocument New(bool native)
    {var doc=NativeMapDocument.CreateNewMap(new("GateRequests","GateRequests",20,20,NativeFormat:native));doc.Paint([new(5,5)],new(29,1,1));return doc;}
    private static HeroGateGenerationPlan Plan(NativeMapDocument doc)=>doc.PlanHeroGateGeneration(HeroGateGenerationOptions.Defaults(doc));
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument doc)=>doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> before)=>Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Reply changed document bytes.");
    private static GateGenerationCapacityException Capacity(Action action)
    {try{action();}catch(GateGenerationCapacityException error){return error;}throw new Exception("Expected the source capacity notice.");}
    private static void Fill(NativeMapDocument doc,bool areas,bool gates,int count)
    {doc.Edit("Populate regions",()=>{for(var i=0;i<count;i++){if(areas)doc.AddMapRegion(MapRegionKind.Area,new(1,1,1,1));if(gates)doc.AddMapRegion(MapRegionKind.Gate,new(1,1,1,1));}});}
    public static void Run()
    {
        foreach(var native in new[]{false,true})
        {Replies(native);Guards(native);Capacities(native);Nested(native);}
    }
    private static void Replies(bool native)
    {
        var doc=New(native);var plan=Plan(doc);var before=Bytes(doc);var revision=doc.EditRevision;var history=doc.GetHistory();
        var prefs=new EditorRequestPreferences(2);using var first=new EditorGateGenerationPolicy(prefs);using var second=new EditorGateGenerationPolicy(prefs);
        using(var request=first.Begin(plan,()=>true)!)
        {
            Check(request.RequiresPrompt&&request.Gates==1&&request.Areas==1&&request.Conditions==0,"Source summary differs.");
            Refuse(()=>second.Begin(plan,()=>true));Refuse(second.ResetSuppression);
            Check(!request.Decline(true)&&request.IsCurrent&&prefs.Flags==2,"No remembered a nondefault reply.");
            Check(request.Decline()&&!request.IsAccepted,"No approved a generator plan.");
        }
        using(var request=first.Begin(plan,()=>true)!){request.Dispose();Check(!request.IsAccepted&&prefs.Flags==2,"Close remembered consent.");}
        using(var request=first.Begin(plan,()=>true)!){request.Confirm(true);Check(request.IsAccepted&&request.IsClosed,"Yes failed.");}
        Exact(before);Check(doc.EditRevision==revision&&doc.GetHistory().Undo.SequenceEqual(history.Undo)&&doc.GetHistory().Redo.SequenceEqual(history.Redo),"Approval changed history or lifetime.");
        using(var request=second.Begin(plan,()=>true)!){Check(!request.RequiresPrompt,"Workspace/child preference was not shared.");request.Confirm();}
        Check(prefs.Flags==(2|EditorGateGenerationPolicy.ConfirmationBit),"Auto reply cleared suppression or another bit.");
        doc.ApplyEditorHeroGateGeneration(plan);doc.Undo();Exact(before);Check((prefs.Flags&EditorGateGenerationPolicy.ConfirmationBit)!=0,"Undo changed runtime preference.");
        first.ResetSuppression();Check(prefs.Flags==2,"Reset changed an unrelated request bit.");
        using var disposed=new EditorGateGenerationPolicy();disposed.Dispose();Refuse(()=>disposed.Begin(Plan(doc),()=>true));
        Console.WriteLine($"GATE REQUEST REPLY DETAIL native={native} counts Yes/No/Close default-only remember shared lease reset read-only history Undo");
    }
    private static void Guards(bool native)
    {
        var doc=New(native);using var policy=new EditorGateGenerationPolicy();var plan=Plan(doc);
        Refuse(()=>policy.Begin(plan,()=>false));
        try{policy.Begin(plan,()=>throw new ApplicationException("Owner callback failure"));throw new Exception("Broken owner accepted.");}
        catch(ApplicationException){ }
        policy.ResetSuppression();
        using(var request=policy.Begin(plan,()=>true)!){doc.Paint([new(2,2)],new(4,2,1));doc.Undo();Check(!request.IsCurrent,"Edit/Undo revived old approval.");Refuse(()=>request.Confirm(true));}
        plan=Plan(doc);
        using(var request=policy.Begin(plan,()=>true)!)
        {
            var file=doc.Files[0];Check(MemoryMarshal.TryGetArray(file.Data,out var data),"Raw guard fixture unavailable.");
            var index=data.Offset+data.Count-1;var old=data.Array![index];data.Array[index]^=1;
            try{Check(!request.IsCurrent,"Raw mutation reused consent.");Refuse(()=>request.Confirm(true));}finally{data.Array[index]=old;}
        }
        plan=Plan(doc);using(var request=policy.Begin(plan,()=>true)!)
        {using var pen=doc.BeginLivePenStroke(new(4,2,1));Check(!request.IsCurrent,"Live stroke allowed approval.");Refuse(()=>request.Confirm());}
        plan=Plan(doc);using(var request=policy.Begin(plan,()=>true)!)doc.Edit("Open transaction",()=>{Check(!request.IsCurrent,"Open edit allowed consent.");Refuse(()=>request.Confirm());});
        Check(policy.Flags==0,"Stale refusal persisted a bit.");
        Console.WriteLine($"GATE REQUEST GUARD DETAIL native={native} owner edit-Undo raw pen nested and no remembered stale reply");
    }
    private static void Capacities(bool native)
    {
        foreach(var mode in new[]{0,1,2})
        {
            var doc=New(native);Fill(doc,mode!=1,mode!=0,254);Check(Plan(doc).HasWork,"254+1 source boundary refused.");
            Fill(doc,mode!=1,mode!=0,1);var before=Bytes(doc);var revision=doc.EditRevision;
            var error=Capacity(()=>Plan(doc));Check(error.AreasExhausted==(mode!=1)&&error.GatesExhausted==(mode!=0),"Wrong area/gate/both capacity report.");
            var prefs=new EditorRequestPreferences(2);using var policy=new EditorGateGenerationPolicy(prefs);
            using(var request=policy.BeginCapacity(error,()=>true))
            {Refuse(()=>request.Confirm(true));Check(!request.IsClosed&&prefs.Flags==2,"Capacity report became permission.");request.Dispose();}
            using(var request=policy.BeginCapacity(error,()=>true)){request.Acknowledge(true);Check(request.IsAcknowledged&&!request.IsAccepted,"OK authorized an impossible plan.");}
            using(var request=policy.BeginCapacity(error,()=>true)){Check(!request.RequiresPrompt,"Capacity suppression failed.");request.Acknowledge();}
            Check(prefs.Flags==(2|EditorGateGenerationPolicy.CapacityBit),"Capacity changed the confirmation bit.");
            Capacity(()=>Plan(doc));Exact(before);Check(doc.EditRevision==revision,"Notice changed map revision.");
            var other=New(native);using(var request=policy.Begin(Plan(other),()=>true)!){Check(request.RequiresPrompt,"Capacity suppressed ordinary Yes/No.");request.Decline();}
            doc.Paint([new(2,2)],new(4,2,1));Refuse(()=>policy.BeginCapacity(error,()=>true));policy.ResetSuppression();Check(prefs.Flags==2,"Capacity reset altered another bit.");
        }
        Console.WriteLine($"GATE REQUEST CAPACITY DETAIL native={native} 254+1/255+1 areas gates both OK Close suppressed never permit generation");
    }
    private static void Nested(bool native)
    {
        var doc=New(native);var gate=doc.AddMapRegion(MapRegionKind.Gate,new(10,10,1,1));var before=Bytes(doc);
        var prefs=new EditorRequestPreferences();using var policy=new EditorGateGenerationPolicy(prefs);
        using(var parent=doc.CreateRegionPropertiesSession(gate))
        using(var child=doc.BeginRegionGateGeneration(parent))
        {
            var plan=doc.PreviewRegionGateGeneration(child,child.Defaults);
            using(var request=policy.Begin(doc,plan,()=>true)!){request.Confirm(true);}Exact(before);
            doc.AcceptRegionGateGeneration(plan,true);Exact(before);
        }
        Exact(before);Check((prefs.Flags&EditorGateGenerationPolicy.ConfirmationBit)!=0,"Parent Cancel erased runtime reply.");
        using(var parent=doc.CreateRegionPropertiesSession(gate))
        using(var child=doc.BeginRegionGateGeneration(parent))
        {
            var plan=doc.PreviewRegionGateGeneration(child,child.Defaults);
            using var request=policy.Begin(doc,plan,()=>true)!;Check(!request.RequiresPrompt,"Parent Cancel lost the shared remembered reply.");
            doc.UpdateRegionProperties(parent,parent.Values with{Bounds=new(11,11,1,1)});
            Check(!request.IsCurrent,"Changed parent draft reused approval.");Refuse(()=>request.Confirm(true));
        }
        Fill(doc,true,false,255);gate=doc.MapRegions().First(r=>r.Kind==MapRegionKind.Gate).Record;
        using(var parent=doc.CreateRegionPropertiesSession(gate))
        using(var child=doc.BeginRegionGateGeneration(parent))
        {
            var error=Capacity(()=>doc.PreviewRegionGateGeneration(child,child.Defaults));
            using var request=policy.BeginCapacity(error,()=>true);child.Dispose();Check(!request.IsCurrent,"Capacity report remained bound to a temporary replay document.");
            Refuse(()=>request.Acknowledge(true));
        }
        Console.WriteLine($"GATE REQUEST NESTED DETAIL native={native} private child parent Cancel shared reply version capacity replay lifetime");
    }
}
