using System.Reflection;
using DK2MapEditor.Core.Editing;

internal static class HistoryFailureChecks
{
    private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static FieldInfo Field(string name)=>typeof(NativeMapDocument).GetField(name,Private)??throw new InvalidOperationException("Missing fixture field: "+name);
    private static T Get<T>(NativeMapDocument document,string name)=>(T)Field(name).GetValue(document)!;
    private static void Set<T>(NativeMapDocument document,string name,T value)=>Field(name).SetValue(document,value);
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Refuses<T>(Action action)where T:Exception
    {try{action();}catch(T error){if(typeof(T)==typeof(OutOfMemoryException))Check(error is EditorHistoryMemoryException {IsSafeToNotify:true},"History failure was untyped or escaped before cleanup.");return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("HistoryFailure","HistoryFailure",16,14,NativeFormat:native));
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> before)=>Check(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Failure changed map bytes.");
    private static void SameHistory(NativeMapDocument document,EditHistorySnapshot before)
    {
        var after=document.GetHistory();Check(after.Undo.SequenceEqual(before.Undo)&&after.Redo.SequenceEqual(before.Redo)&&after.ChangeCount==before.ChangeCount
            &&after.EditorChangeCount==before.EditorChangeCount&&after.SnapshotBytes==before.SnapshotBytes&&after.Depth==before.Depth,"Failure changed prior history/counters/bytes.");
    }
    private static void RedoBranch(NativeMapDocument document)
    {document.Paint([new(2,2)],new(5,2,1));document.Paint([new(3,3)],new(4,2,1));document.Undo();}
    private sealed record State(Dictionary<NativeFile,byte[]> Files,EditHistorySnapshot History,long Revision,bool Dirty,uint? UndoUsage);
    private static State Capture(NativeMapDocument document)
    {
        var files=(IEnumerable<NativeFile>)typeof(NativeMapDocument).GetMethod("TransactionFiles",Private)!.Invoke(document,null)!;
        return new(files.ToDictionary(file=>file,file=>file.Data.ToArray()),document.GetHistory(),document.EditRevision,document.HasChanges,document.ReadActivityStatistics()?.UsageOfUndo);
    }
    private static void Unchanged(NativeMapDocument document,State before)
    {
        Exact(before.Files);SameHistory(document,before.History);
        Check(document.EditRevision==before.Revision&&document.HasChanges==before.Dirty&&document.ReadActivityStatistics()?.UsageOfUndo==before.UndoUsage,"Failed preparation/replay changed lifetime, dirty state or Undo usage.");
        Check(!Get<bool>(document,"transactionOpen")&&!document.HasActivePenStroke,"Failed allocation left an active edit.");
    }
    private static void Mutate(NativeMapDocument document)
    {
        document.Edit("Two-file change",()=>
        {
            document.Paint([new(5,5)],new(4,2,1));
            var draft=document.CreateMapGeneralDraft();document.ApplyMapGeneralSettings(document.UpdateMapGeneralSettings(draft,draft.Values.Select(row=>new MapGeneralInput(row.Key,row.Key=="Name"?"After allocation":""+row.Value))));
        });
    }
    private static int RootBufferFailures(bool native)
    {
        var sample=New(native);RedoBranch(sample);var total=0;var copy=Get<Func<byte[],byte[]>>(sample,"historyBufferCopy");
        Set<Func<byte[],byte[]>>(sample,"historyBufferCopy",bytes=>{total++;return copy(bytes);});Mutate(sample);
        for(var fail=1;fail<=total;fail++)
        {
            var document=New(native);RedoBranch(document);var before=Capture(document);var plan=document.PrepareEditorMapName(document.CreateMapGeneralDraft(),new string('N',65));var attempts=0;
            var original=Get<Func<byte[],byte[]>>(document,"historyBufferCopy");
            Set<Func<byte[],byte[]>>(document,"historyBufferCopy",bytes=>++attempts==fail?throw new OutOfMemoryException("Simulated snapshot allocation"):original(bytes));
            try{Refuses<OutOfMemoryException>(()=>Mutate(document));}finally{Set(document,"historyBufferCopy",original);}
            Check(attempts==fail,"Requested snapshot failure did not occur.");Unchanged(document,before);Check(plan.IsCurrent,"Refused allocation invalidated a current draft.");
            document.Redo();document.Undo();Exact(before.Files);Mutate(document);document.Undo();Exact(before.Files);
        }
        return total;
    }
    private static int MetadataFailures(bool native)
    {
        var sample=New(native);var total=0;var factory=Get<Func<string,List<HistoryOperation>,HistoryOperation>>(sample,"historyOperationFactory");
        Set<Func<string,List<HistoryOperation>,HistoryOperation>>(sample,"historyOperationFactory",(name,children)=>{total++;return factory(name,children);});Mutate(sample);
        for(var fail=1;fail<=total;fail++)
        {
            var document=New(native);RedoBranch(document);var before=Capture(document);var attempts=0;var original=Get<Func<string,List<HistoryOperation>,HistoryOperation>>(document,"historyOperationFactory");
            Set<Func<string,List<HistoryOperation>,HistoryOperation>>(document,"historyOperationFactory",(name,children)=>++attempts==fail?throw new OutOfMemoryException("Simulated history metadata allocation"):original(name,children));
            try{Refuses<OutOfMemoryException>(()=>Mutate(document));}finally{Set(document,"historyOperationFactory",original);}
            Check(attempts==fail,"Requested metadata failure did not occur.");Unchanged(document,before);document.Redo();document.Undo();Exact(before.Files);
        }
        return total;
    }
    private static int ReplayFailures(bool native,bool backward)
    {
        var sample=New(native);Mutate(sample);if(!backward)sample.Undo();var total=0;var copy=Get<Func<byte[],byte[]>>(sample,"historyBufferCopy");
        Set<Func<byte[],byte[]>>(sample,"historyBufferCopy",bytes=>{total++;return copy(bytes);});if(backward)sample.Undo();else sample.Redo();
        Check(total>=2,"Replay fixture did not change multiple files.");
        for(var fail=1;fail<=total;fail++)
        {
            var document=New(native);Mutate(document);if(!backward)document.Undo();var before=Capture(document);var attempts=0;var original=Get<Func<byte[],byte[]>>(document,"historyBufferCopy");
            Set<Func<byte[],byte[]>>(document,"historyBufferCopy",bytes=>++attempts==fail?throw new OutOfMemoryException("Simulated replay allocation"):original(bytes));
            try{Refuses<OutOfMemoryException>(()=>{if(backward)document.Undo();else document.Redo();});}finally{Set(document,"historyBufferCopy",original);}
            Unchanged(document,before);if(backward){document.Undo();document.Redo();}else{document.Redo();document.Undo();}Exact(before.Files);
        }
        return total;
    }
    private static void PenAllocations(bool native)
    {
        foreach(var metadata in new[]{false,true})
        {
            var document=New(native);RedoBranch(document);var before=Capture(document);var plan=document.PrepareEditorMapName(document.CreateMapGeneralDraft(),new string('N',65));
            var pen=document.BeginLivePenStroke(new(4,2,1));pen.Start(new(5,5));
            var buffer=Get<Func<byte[],byte[]>>(document,"historyBufferCopy");var factory=Get<Func<string,List<HistoryOperation>,HistoryOperation>>(document,"historyOperationFactory");
            if(metadata)Set<Func<string,List<HistoryOperation>,HistoryOperation>>(document,"historyOperationFactory",(_,_)=>throw new OutOfMemoryException("Simulated pen metadata allocation"));
            else Set<Func<byte[],byte[]>>(document,"historyBufferCopy",_=>throw new OutOfMemoryException("Simulated pen snapshot allocation"));
            try{Refuses<OutOfMemoryException>(pen.Dispose);}finally{Set(document,"historyBufferCopy",buffer);Set(document,"historyOperationFactory",factory);}
            Exact(before.Files);SameHistory(document,before.History);Check(pen.IsClosed&&!document.HasActivePenStroke&&!plan.IsCurrent&&document.EditRevision>before.Revision,"Failed live frame lost closure or revived an old draft.");
            Check(document.ReadActivityStatistics()!.UsageOfPaint==1,"Rollback erased independently observed paint activity.");pen.Dispose();document.Redo();document.Undo();Exact(before.Files);
        }
        var reserved=New(native);var state=Capture(reserved);var original=Get<Func<byte[],byte[]>>(reserved,"historyBufferCopy");
        Set<Func<byte[],byte[]>>(reserved,"historyBufferCopy",_=>throw new OutOfMemoryException("Simulated pen reservation"));
        try{Refuses<OutOfMemoryException>(()=>reserved.BeginLivePenStroke(new(4,2,1)));}finally{Set(reserved,"historyBufferCopy",original);}Unchanged(reserved,state);
    }
    private static void RevisionAndObservers(bool native,string directory)
    {
        foreach(var backward in new[]{false,true})
        {
            var document=New(native);Mutate(document);if(!backward)document.Undo();Set(document,"<EditRevision>k__BackingField",long.MaxValue);var state=Capture(document);
            Refuses<OverflowException>(()=>{if(backward)document.Undo();else document.Redo();});Unchanged(document,state);
        }
        var root=New(native);RedoBranch(root);Set(root,"<EditRevision>k__BackingField",long.MaxValue);var rootState=Capture(root);Refuses<OverflowException>(()=>Mutate(root));Unchanged(root,rootState);
        var penDocument=New(native);var initial=Bytes(penDocument);var pen=penDocument.BeginLivePenStroke(new(4,2,1));pen.Start(new(5,5));
        EventHandler broken=(_,_)=>throw new IOException("Committed observer");penDocument.Changed+=broken;Refuses<IOException>(pen.Dispose);penDocument.Changed-=broken;
        Check(pen.IsClosed&&penDocument.CanUndo&&!penDocument.HasActivePenStroke,"Postcommit observer undid a successful stroke.");penDocument.Undo();Exact(initial);
        var replay=New(native);Mutate(replay);replay.Changed+=broken;Refuses<IOException>(replay.Undo);replay.Changed-=broken;Check(replay.CanRedo&&!replay.CanUndo,"Postcommit replay observer was treated as refusal.");replay.Redo();
        var saved=replay.SaveCopy(Path.Combine(directory,"allocation-recovery-"+native));var reopened=NativeMapDocument.Load(saved);Check(reopened.CreateMapGeneralDraft().Values["Name"]=="After allocation","Recovered history did not save/reopen.");
        void PlainMemoryFailure(Action action)
        {try{action();}catch(OutOfMemoryException error){Check(error.GetType()==typeof(OutOfMemoryException),"Ordinary action/observer OOM was classified as Undo preparation.");return;}throw new Exception("Expected plain observer/action OOM.");}
        var ordinary=New(native);var ordinaryBytes=Bytes(ordinary);
        PlainMemoryFailure(()=>ordinary.Edit("Action failure",()=>throw new OutOfMemoryException("Action allocation")));Exact(ordinaryBytes);
        EventHandler memoryObserver=(_,_)=>throw new OutOfMemoryException("Committed observer");
        ordinary.Changed+=memoryObserver;PlainMemoryFailure(()=>ordinary.Paint([new(4,4)],new(4,2,1)));ordinary.Changed-=memoryObserver;
        Check(ordinary.CanUndo,"Postcommit OOM erased a committed operation.");ordinary.Undo();Exact(ordinaryBytes);
        var stroke=ordinary.BeginLivePenStroke(new(4,2,1));stroke.Start(new(5,5));ordinary.Changed+=memoryObserver;
        PlainMemoryFailure(stroke.Dispose);ordinary.Changed-=memoryObserver;Check(stroke.IsClosed&&!ordinary.HasActivePenStroke&&ordinary.CanUndo,"Postcommit pen OOM was rolled back.");
        ordinary.Undo();Exact(ordinaryBytes);
        var committed=New(native);var committedBytes=Bytes(committed);var copy=Get<Func<byte[],byte[]>>(committed,"historyBufferCopy");
        EventHandler nestedFailure=(_,_)=>
        {
            Set<Func<byte[],byte[]>>(committed,"historyBufferCopy",_=>throw new OutOfMemoryException("Secondary observer operation"));
            try{committed.Edit("Observer secondary operation",()=>throw new Exception("Refused observer action ran."));}
            finally{Set(committed,"historyBufferCopy",copy);}
        };
        committed.Changed+=nestedFailure;Refuses<OutOfMemoryException>(()=>committed.Paint([new(6,6)],new(4,2,1)));committed.Changed-=nestedFailure;
        Check(committed.CanUndo&&committed.GetTile(6,6)==new MapTile(4,2,1),"A secondary history failure rolled back the prior committed change.");
        committed.Undo();Exact(committedBytes);
    }
    private static void ManagerBatchAllocations(bool native)
    {
        foreach(var kind in new[]{"insert","modify","remove"})
        foreach(var fail in new[]{1,2})
        {
            var document=New(native);var template=ObjectBatchTemplate.Default(PlacedThingKind.Creature) with {Owner=3,Health=72};
            for(var index=0;index<3;index++)document.ApplyObjectBatchInsertion(document.PlanObjectBatchInsertion(template,new(Position:new(2+index,3),Flags:(ObjectBatchFlags)127)),[0]);
            RedoBranch(document);Func<int> apply;
            if(kind=="remove")
            {
                var plan=document.PlanObjectRemoval(document.ObjectManagerEntries().Select(entry=>entry.Record));
                apply=()=>document.ApplyEditorObjectRemoval(plan);
            }
            else if(kind=="modify")
            {
                var plan=document.PlanObjectBatchModification(document.ObjectManagerEntries().Select(entry=>entry.Record),template with {Health=93},new(ObjectModificationMask.Health,Flags:(ObjectBatchFlags)127));
                plan=document.ReviewObjectBatchModification(plan,plan.Candidates.Select(_=>ObjectModificationDecision.Modify));
                apply=()=>document.ApplyEditorObjectBatchModification(plan);
            }
            else
            {
                var plan=document.PlanObjectBatchInsertion(template,new(Position:new(6,6),Mode:ObjectBatchMode.Rectangle,WholeMap:false,Selection:new(6,6,2,2),Flags:(ObjectBatchFlags)127));
                apply=()=>document.ApplyEditorObjectBatchInsertion(plan,[0,1,2]);
            }
            var before=Capture(document);var attempts=0;var observedStagedBytes=false;
            var original=Get<Func<string,List<HistoryOperation>,HistoryOperation>>(document,"historyOperationFactory");
            Set<Func<string,List<HistoryOperation>,HistoryOperation>>(document,"historyOperationFactory",(name,children)=>
            {
                if(++attempts==fail)
                {
                    observedStagedBytes=before.Files.Any(pair=>!pair.Key.Data.Span.SequenceEqual(pair.Value));
                    throw new OutOfMemoryException("Simulated manager batch history allocation");
                }
                return original(name,children);
            });
            var events=0;EventHandler changed=(_,_)=>events++;document.Changed+=changed;
            try{Refuses<OutOfMemoryException>(()=>apply());}finally{Set(document,"historyOperationFactory",original);document.Changed-=changed;}
            Check(attempts==fail&&observedStagedBytes,"Manager fault did not occur after staged batch changes.");
            Check(events==0,"Failed manager batch published a change event.");Unchanged(document,before);
            document.Redo();document.Undo();Exact(before.Files);
            Check(apply()==3,"Failed manager batch could not be retried with its original plan.");document.Undo();Exact(before.Files);
        }
    }
    public static void Run(string directory)
    {
        var failures=new List<string>();
        void Case(string name,Action run){try{run();Console.WriteLine("PASS history failure "+name);}catch(Exception error){failures.Add(name+": "+error.Message);Console.WriteLine("FAIL history failure "+name+": "+error.Message);}}
        foreach(var native in new[]{false,true})
        {
            Case("prepare "+native,()=>Preparation(native));
            Case("pen counter "+native,()=>PenCounter(native));
        }
        if(failures.Count!=0)throw new InvalidOperationException(string.Join(Environment.NewLine,failures));
        Console.WriteLine("HISTORY FAILURE REGRESSIONS 4 preparation lock and failed pen counter preserve complete prior history; no real memory exhaustion");
        var allocations=0;
        foreach(var native in new[]{false,true})
        {
            allocations+=RootBufferFailures(native)+MetadataFailures(native)+ReplayFailures(native,true)+ReplayFailures(native,false);
            PenAllocations(native);RevisionAndObservers(native,directory);ManagerBatchAllocations(native);
        }
        Console.WriteLine("MANAGER HISTORY ALLOCATION DETAIL 12 staged insertion/modification/removal failures native/project; full bytes/history/counters/revision rollback, prior redo retained, no events, original plan retry and Undo; approved atomic policy, no partial/untracked continuation");
        Console.WriteLine($"HISTORY ALLOCATION FAILURE DETAIL {allocations} every snapshot/metadata/multi-file UndoRedo allocation boundary native/project; unchanged bytes/both stacks/counters/revision/dirty/Undo usage, exact recovery/retry, pen reservation/rollback/stale guards, revision overflow and committed observers; no real OOM, seven-file export/save/reopen and original full Undo retained");
    }
    private static void Preparation(bool native)
    {
        var document=New(native);RedoBranch(document);var before=Bytes(document);var history=document.GetHistory();var revision=document.EditRevision;
        var files=Get<List<NativeFile>>(document,"files");var action=false;
        // A duplicate private entry makes preparation fail deterministically.
        // Restore fixture topology before checking recovery; no bytes are corrupt.
        files.Add(files[0]);try{Refuses<ArgumentException>(()=>document.Edit("Preparation failure",()=>action=true));}finally{files.RemoveAt(files.Count-1);}
        Check(!action,"Preparation invoked the edit action.");Exact(before);SameHistory(document,history);Check(document.EditRevision==revision,"Preparation changed revision.");
        Check(!Get<bool>(document,"transactionOpen"),"Failed preparation left the transaction locked.");
        document.Redo();document.Undo();document.Paint([new(4,4)],new(5,2,1));document.Undo();Exact(before);
    }
    private static void PenCounter(bool native)
    {
        var document=New(native);RedoBranch(document);var priorCount=document.GetHistory().ChangeCount;Set(document,"changeCount",long.MaxValue);var before=Bytes(document);var history=document.GetHistory();var editor=document.EditorChangeCount;
        var pen=document.BeginLivePenStroke(new(4,2,1));pen.Start(new(5,5));Refuses<OverflowException>(pen.Dispose);
        Exact(before);SameHistory(document,history);Check(!document.HasActivePenStroke&&pen.IsClosed&&document.EditorChangeCount==editor,"Failed pen remained active or kept its counter.");
        pen.Dispose();Set(document,"changeCount",priorCount);document.Redo();document.Undo();
    }
}
