using System.Reflection;
using DK2MapEditor.Core.Editing;

internal static class ObjectManagerHistoryChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Refuses<T>(Action action) where T:Exception
    {try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    private static NativeMapDocument New(bool native)
    {
        var d=NativeMapDocument.CreateNewMap(new("ManagerHistory","ManagerHistory",16,14,NativeFormat:native));
        d.Paint([new(3,3)],new(4,2,1));return d;
    }
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> before)=>Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Manager Undo changed unrelated bytes/order.");
    private static int Add(NativeMapDocument d,int x)
    {
        var plan=d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Creature) with {Owner=3,Health=72},new(Position:new(x,5),Flags:(ObjectBatchFlags)127));
        return d.ApplyEditorObjectBatchInsertion(plan,[0]);
    }
    private static int Modify(NativeMapDocument d)
    {
        var plan=d.PlanObjectBatchModification(d.ObjectManagerEntries().Select(e=>e.Record),ObjectBatchTemplate.Default(PlacedThingKind.Creature) with {Owner=3,Health=93},new(ObjectModificationMask.Health,Flags:(ObjectBatchFlags)127));
        plan=d.ReviewObjectBatchModification(plan,plan.Candidates.Select(_=>ObjectModificationDecision.Modify));
        return d.ApplyEditorObjectBatchModification(plan);
    }
    private static int Remove(NativeMapDocument d)=>d.ApplyEditorObjectRemoval(d.PlanObjectRemoval(d.ObjectManagerEntries().Select(e=>e.Record)));
    public static void Run()
    {
        foreach(var native in new[]{false,true})
        {
            var d=New(native);var before=Bytes(d);var original=d.GetHistory();
            using(var manager=new EditorObjectManagerHistory(d))
            {
                Check(!manager.CanUndo&&!manager.Undo(),"Fresh manager undid a prior document edit.");
                Check(manager.Execute(()=>Add(d,4))==1&&manager.CanUndo,"Manager insertion was not owned.");var inserted=Bytes(d);
                Check(manager.Execute(()=>Modify(d))==1&&manager.CanUndo,"Manager modification was not owned.");var modified=Bytes(d);
                Check(manager.Execute(()=>Modify(d))==1&&manager.CanUndo,"Accepted equal modification was not owned.");Exact(modified);
                Check(manager.Undo(),"Equal modification could not be undone.");Exact(modified);
                Check(manager.Execute(()=>Remove(d))==1&&manager.CanUndo,"Manager removal was not owned.");
                Check(manager.Undo(),"Removal could not be undone.");Exact(modified);
                Check(manager.Undo(),"Modification could not be undone.");Exact(inserted);
                Check(manager.Undo()&&!manager.CanUndo&&!manager.Undo(),"Manager crossed its opening boundary.");Exact(before);
                Check(d.GetHistory().Undo.SequenceEqual(original.Undo)&&d.EditorChangeCount==original.EditorChangeCount,"Manager Undo lost prior stack/counter.");
                d.Redo();Check(!manager.CanUndo,"External Redo recreated a consumed local Undo count.");
            }
            var interleaved=New(native);using(var manager=new EditorObjectManagerHistory(interleaved))
            {
                manager.Execute(()=>Add(interleaved,4));var own=Bytes(interleaved);
                interleaved.Paint([new(6,6)],new(5,2,1));var foreign=Bytes(interleaved);
                Check(!manager.CanUndo&&!manager.Undo(),"Manager undid an unrelated command.");Exact(foreign);
                interleaved.Undo();Exact(own);Check(manager.CanUndo&&manager.Undo(),"Undoing the foreign edit did not restore the exact owned head.");
                var declined=Bytes(interleaved);manager.Execute(()=>{interleaved.Paint([new(7,7)],new(4,2,1));return false;},()=>false);
                Check(!manager.CanUndo,"Declined dialog claimed its host's pending field commit.");interleaved.Undo();Exact(declined);
            }
            foreach(var depth in new[]{0,1})
            {
                var limited=New(native);limited.SetHistoryDepth(depth);using var manager=new EditorObjectManagerHistory(limited);
                manager.Execute(()=>Add(limited,4));manager.Execute(()=>Add(limited,5));
                Check(manager.CanUndo==(depth!=0),"Disabled history produced a fake manager Undo.");
                if(depth!=0)Check(manager.Undo()&&!manager.CanUndo,"Trimmed manager history crossed its retained boundary.");
            }
            var failed=New(native);using(var manager=new EditorObjectManagerHistory(failed))
            {
                var beforeFailure=Bytes(failed);Refuses<IOException>(()=>manager.Execute<int>(()=>throw new IOException("Before commit")));
                Check(!manager.CanUndo,"Refused manager operation gained Undo.");Exact(beforeFailure);
                manager.Execute(()=>Add(failed,4));var added=Bytes(failed);var history=failed.GetHistory();
                var field=typeof(NativeMapDocument).GetField("historyBufferCopy",BindingFlags.Instance|BindingFlags.NonPublic)!;var copy=field.GetValue(failed);
                field.SetValue(failed,(Func<byte[],byte[]>)(_=>throw new OutOfMemoryException("Controlled manager replay allocation")));
                try{Refuses<EditorHistoryMemoryException>(()=>manager.Undo());}finally{field.SetValue(failed,copy);}
                Check(manager.CanUndo&&failed.GetHistory().Undo.SequenceEqual(history.Undo),"Failed replay consumed manager ownership.");Exact(added);Check(manager.Undo(),"Manager replay retry failed.");Exact(beforeFailure);
                EventHandler observer=(_,_)=>throw new IOException("After commit");failed.Changed+=observer;
                try{Refuses<IOException>(()=>manager.Execute(()=>Add(failed,4)));}finally{failed.Changed-=observer;}
                Check(manager.CanUndo,"Committed observer exception lost manager ownership.");
                failed.Changed+=observer;try{Refuses<IOException>(()=>manager.Undo());}finally{failed.Changed-=observer;}
                Check(!manager.CanUndo,"Committed Undo observer exception retained the wrong local count.");Exact(beforeFailure);
                manager.Dispose();Check(!manager.CanUndo&&!manager.Undo(),"Closed manager retained authority.");Refuses<InvalidOperationException>(()=>manager.Execute(()=>Add(failed,4)));
            }
            var disposed=New(native);var closeDuringUndo=new EditorObjectManagerHistory(disposed);closeDuringUndo.Execute(()=>Add(disposed,4));
            disposed.Changed+=(_,_)=>closeDuringUndo.Dispose();Check(closeDuringUndo.Undo()&&!closeDuringUndo.CanUndo,"Close during committed replay corrupted ownership.");
            var reentrant=New(native);using(var manager=new EditorObjectManagerHistory(reentrant))
            {
                manager.Execute(()=>{Check(!manager.Undo(),"Reentrant Undo was accepted.");Refuses<InvalidOperationException>(()=>manager.Execute(()=>Add(reentrant,5)));return Add(reentrant,4);});
                using var pen=reentrant.BeginLivePenStroke(new(4,2,1));Check(!manager.CanUndo&&!manager.Undo(),"Live pen allowed manager Undo.");
            }
            Console.WriteLine($"MANAGER HISTORY DETAIL native={native}: add/modify/remove ownership, opening boundary, unrelated host edits, declined request, depth0/1, controlled replay OOM/retry, committed observers, close and reentrancy");
        }
    }
}
