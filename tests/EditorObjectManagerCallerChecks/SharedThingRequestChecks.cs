using System.Reflection;
using DK2MapEditor.Core.Editing;

internal static class SharedThingRequestChecks
{
    private static void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);}
    private static void Reject(Action action){try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException){return;}throw new InvalidOperationException("Unsafe source request accepted.");}
    private static Dictionary<string,byte[]> State(NativeMapDocument d)=>d.IsLegacyProject?d.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray())
        :d.PrepareProjectConversion().SourceFiles.ToDictionary(file=>file.ExportPath,file=>file.Bytes.ToArray());
    private static void Exact(NativeMapDocument d,Dictionary<string,byte[]> before){var after=State(d);Check(after.Count==before.Count && before.All(pair=>after.TryGetValue(pair.Key,out var bytes)&&bytes.AsSpan().SequenceEqual(pair.Value)),"Request/Undo changed game or working metadata bytes");}
    private static NativeRecord Add(NativeMapDocument d,string kind,int x)=>d.IsLegacyProject?d.AddEditorLegacyRecord(kind,new(x,4))
        :d.AddRecord(RecordTemplate.All.Single(template=>template.Structure==(kind switch{"Creature"=>"GAMEKEEPER","Door"=>"GAMEDOOR",_=>"GAMETRAP"})),new(x,4));
    private static ScriptOwner Owner(NativeMapDocument d,NativeRecord row)=>d.ScriptOwners().Single(owner=>owner.Record.File==row.File && owner.Record.Offset==row.Offset);
    private static NativeMapDocument Fixture(bool native,bool empty)
    {
        var d=NativeMapDocument.CreateNewMap(new("SharedRequest","SharedRequest",16,16,NativeFormat:native));
        var used=Add(d,"Creature",3);var assigned=Add(d,"Creature",5);_=Add(d,"Creature",7);_=Add(d,"Door",9);
        var root=d.EnsureScriptScope(Owner(d,used));if(!empty)d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
        var reserved=d.EnsureScriptScope(Owner(d,assigned));d.RemoveScriptScope(reserved);return d;
    }
    private static NativeRecord[] Creatures(NativeMapDocument d)=>d.ObjectManagerEntries().Where(entry=>entry.Thing.Kind==PlacedThingKind.Creature).Select(entry=>entry.Record).ToArray();
    public static void Run()
    {
        var cases=0;
        foreach(var native in new[]{false,true})foreach(var empty in new[]{false,true})foreach(var command in new[]{EditorThingCommand.Remove,EditorThingCommand.Modify})foreach(var reply in new[]{0,1,2})
        {Matrix(native,empty,command,reply);cases++;}
        foreach(var native in new[]{false,true}){EmptyNo(native);SharedFlags(native);Guards(native);FailedAndCommitted(native);}
        Predicate();
        Console.WriteLine($"SHARED THING REQUEST DETAIL {cases} native/project x empty/nonempty x remove/modify x Cancel/Yes/No; literal bits2/100 shared with regions, no-type-filter creature predicate, assigned-ID No filter, original multi before filtering, complete metadata UndoRedo, empty parent Undo/count0, runtime remember/reset/lease, private read-only previews, guards, rollback and committed observers");
    }
    private static void Matrix(bool native,bool empty,EditorThingCommand command,int reply)
    {
        var d=Fixture(native,empty);using var policy=new EditorThingRequestPolicy();var before=State(d);var h=d.GetHistory();var revision=d.EditRevision;
        var records=Creatures(d);using var request=policy.Begin(d,records,command);Check(request.UsedCount==1,"Stored empty point or unused assigned identity predicate wrong");Exact(d,before);
        if(command==EditorThingCommand.Remove){Check(request.Prompt==EditorConfirmationPrompt.ObjectRemoval && request.RequiresPrompt,"Missing first manager request");request.Accept();Check(request.Prompt==EditorConfirmationPrompt.UsedThingRemoval && !request.IsClosed,"Used request not advanced");Exact(d,before);}
        Check(request.RequiresPrompt,"Missing source used request");Check(!request.Decline(true) && !request.IsClosed && policy.Flags==0,"Checked No acquired consent");
        if(reply==0){request.Dispose();Check(!request.IsAccepted,"Cancel accepted command");Exact(d,before);Check(d.EditRevision==revision && d.GetHistory().Undo.SequenceEqual(h.Undo),"Cancel allocated Undo");return;}
        if(reply==1)request.Accept();else Check(request.Decline(),"No refused");Check(request.IsClosed && request.IsAccepted && policy.Flags==0,"Reply or remember state wrong");
        var count=reply==1?3:1;
        if(command==EditorThingCommand.Remove)
        {
            Check(request.RemovedCount==count && d.EditorChangeCount==h.EditorChangeCount+count && d.GetHistory().Undo.Count==h.Undo.Count+1,"Source delete count/parent Undo wrong");
            Check(d.ScriptBindings().Any(binding=>binding.Root.Scope==2 && (reply==1?binding.Owner is null:binding.Owner is not null) && d.HasStoredScriptScope(binding.Root)),"Deletion lost point or No removed assigned owner");
        }
        else
        {
            Exact(d,before);Check(d.EditRevision==revision && request.ResultSelection.Count==count,"Modify question changed map or wrong assigned-ID filter");
            var template=ObjectBatchTemplate.Default(PlacedThingKind.Creature) with{Health=66};
            var plan=d.PlanEditorObjectBatchModification(request,template,new(ObjectModificationMask.Health,Flags:(ObjectBatchFlags)127));
            Check(plan.Multiple && plan.Candidates.Count==count && plan.Options.IncludeScripted,"Original ModifyMulti/accepted script choice lost after filtering");
            Check(d.ApplyEditorObjectBatchModification(plan)==count && d.EditorChangeCount==h.EditorChangeCount+count,"Approved source modification count wrong");
        }
        var after=State(d);d.Undo();Exact(d,before);d.Redo();Exact(d,after);
    }
    private static void EmptyNo(bool native)
    {
        foreach(var zeroHistory in new[]{false,true})
        {
            var d=Fixture(native,true);if(zeroHistory)d.SetHistoryDepth(0);var before=State(d);var h=d.GetHistory();var revision=d.EditRevision;var changes=0;d.Changed+=(_,_)=>changes++;
            using var policy=new EditorThingRequestPolicy();using var request=policy.Begin(d,Creatures(d).Take(2).ToArray(),EditorThingCommand.Remove);request.Accept();request.Decline();Exact(d,before);
            Check(request.IsAccepted && request.RemovedCount==0 && d.EditRevision==revision+1 && d.EditorChangeCount==h.EditorChangeCount && changes==1
                && d.GetHistory().Undo.Count==h.Undo.Count+(zeroHistory?0:1),"All-skipped delete must retain parent Undo with source increment0");
            if(!zeroHistory){d.Undo();Exact(d,before);d.Redo();Exact(d,before);}
            using var modify=policy.Begin(d,Creatures(d).Take(2).ToArray(),EditorThingCommand.Modify);modify.Decline();Check(modify.IsAccepted && modify.ResultSelection.Count==0,"All-skipped Modify selection wrong");Reject(()=>d.PlanEditorObjectBatchModification(modify,ObjectBatchTemplate.Default(PlacedThingKind.Creature),new(ObjectModificationMask.Health)));
        }
    }
    private static void SharedFlags(bool native)
    {
        var d=Fixture(native,true);var preferences=new EditorRequestPreferences(0x40000000);using var things=new EditorThingRequestPolicy(preferences);using var regions=new EditorRegionRemovalPolicy(preferences);
        using(var request=things.Begin(d,Creatures(d).Take(1).ToArray(),EditorThingCommand.Remove)){request.Accept(true);request.Accept(true);}d.Undo();
        Check(preferences.Flags==0x40000102 && things.UsedSuppressed && regions.UsedAreasSuppressed,"Shared0x100 or runtime Undo lifetime wrong");
        var area=d.AddMapRegion(MapRegionKind.Area,new(11,11,1,1));d.EnsureScriptScope(d.ScriptOwners().Single(owner=>owner.Scope==4 && owner.Record.Offset==area.Offset && owner.Record.File==area.File));
        using(var request=regions.Begin(d,[area])){Check(request.RequiresPrompt,"Object bit2 suppressed region bit16");request.Accept();Check(!request.RequiresPrompt,"Remembered creature Yes did not suppress area question");request.Accept();}d.Undo();
        regions.ResetSuppression();Check(preferences.Flags==0x40000002 && !things.UsedSuppressed,"Region reset failed to clear shared bit or damaged unrelated bit");
        using(var request=regions.Begin(d,[area])){Reject(()=>things.Begin(d,Creatures(d),EditorThingCommand.Remove));Reject(()=>things.ResetSuppression());Check(request.IsCurrent,"Foreign policy reset canceled active region");}
        using(var request=things.Begin(d,Creatures(d),EditorThingCommand.Modify)){Reject(()=>regions.Begin(d,[area]));Reject(()=>regions.ResetSuppression());Check(request.IsCurrent,"Foreign region reset canceled active object request");}
        // An area remembers the same bit in the opposite direction too.
        using(var request=regions.Begin(d,[area])){request.Accept();request.Accept(true);}d.Undo();
        using(var request=things.Begin(d,Creatures(d),EditorThingCommand.Modify)){Check(!request.RequiresPrompt,"Area Yes did not suppress creature Modify question");request.Accept();Check(request.ResultSelection.Count==3,"Suppressed request did not use default Yes");}
        things.ResetSuppression();Check(preferences.Flags==0x40000000 && !regions.UsedAreasSuppressed,"Object reset damaged unrelated preferences");
    }
    private static void Guards(bool native)
    {
        var d=Fixture(native,false);using var policy=new EditorThingRequestPolicy();var records=Creatures(d);var before=State(d);
        Reject(()=>policy.Begin(d,[],EditorThingCommand.Remove));Reject(()=>policy.Begin(d,[records[0],records[0]],EditorThingCommand.Remove));Reject(()=>policy.Begin(d,[records[0] with{Fields=[]}],EditorThingCommand.Remove));
        var other=Fixture(native,false);Reject(()=>policy.Begin(d,Creatures(other),EditorThingCommand.Remove));
        using(var request=policy.Begin(d,records,EditorThingCommand.Modify)){Reject(()=>policy.Begin(d,records,EditorThingCommand.Remove));d.Paint([new(10,10)],new(8,3,1));d.Undo();Reject(()=>request.Accept(true));Check(!request.IsCurrent && policy.Flags==0,"Stale reply acquired consent");}Exact(d,before);
        using(var pen=d.BeginLivePenStroke(new(8,3,1)))Reject(()=>policy.Begin(d,records,EditorThingCommand.Remove));
        d.Edit("nested request",()=>Reject(()=>policy.Begin(d,records,EditorThingCommand.Modify)));Exact(d,before);
        using(var request=policy.Begin(d,records,EditorThingCommand.Remove)){request.Decline();Check(!request.IsAccepted,"First No accepted removal");}Exact(d,before);
        Reject(()=>policy.Begin(d,new MutatingSelection(records,()=>d.Paint([new(10,10)],new(8,3,1))),EditorThingCommand.Remove));d.Undo();Exact(d,before);
        var area=d.AddMapRegion(MapRegionKind.Area,new(11,11,1,1));using(var regions=new EditorRegionRemovalPolicy())
        {Reject(()=>regions.Begin(d,new MutatingSelection([area],()=>d.Paint([new(10,10)],new(8,3,1)))));d.Undo();using var valid=regions.Begin(d,[area]);Check(valid.IsCurrent,"Failed enumeration leaked region request lease");}
        d.Undo();Exact(d,before);
        using var resolved=policy.Begin(d,records,EditorThingCommand.Modify);resolved.Accept();var template=ObjectBatchTemplate.Default(PlacedThingKind.Creature);other.Paint([new(10,10)],new(8,3,1));Reject(()=>other.PlanEditorObjectBatchModification(resolved,template,new(ObjectModificationMask.Health)));d.Paint([new(10,10)],new(8,3,1));Reject(()=>d.PlanEditorObjectBatchModification(resolved,template,new(ObjectModificationMask.Health)));d.Undo();Exact(d,before);
    }
    private sealed class MutatingSelection(NativeRecord[] records,Action change):IReadOnlyList<NativeRecord>
    {
        private bool changed;
        public int Count=>records.Length;
        public NativeRecord this[int index]=>records[index];
        public IEnumerator<NativeRecord> GetEnumerator(){if(!changed){changed=true;change();}return ((IEnumerable<NativeRecord>)records).GetEnumerator();}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private static void FailedAndCommitted(bool native)
    {
        var d=Fixture(native,false);using var policy=new EditorThingRequestPolicy(0x100);var before=State(d);var records=Creatures(d);
        var counter=typeof(NativeMapDocument).GetField("changeCount",BindingFlags.Instance|BindingFlags.NonPublic)!;var prior=(long)counter.GetValue(d)!;
        using(var request=policy.Begin(d,records,EditorThingCommand.Remove)){request.Accept(true);counter.SetValue(d,long.MaxValue);Reject(()=>request.Accept());counter.SetValue(d,prior);Check(!request.IsAccepted && request.IsClosed && policy.Flags==0x102,"Rollback lost remembered Yes or misclassified commit");}Exact(d,before);
        using var committed=policy.Begin(d,records,EditorThingCommand.Remove);committed.Accept();EventHandler failure=(_,_)=>throw new InvalidOperationException("committed observer");d.Changed+=failure;
        try{Reject(()=>committed.Accept());}finally{d.Changed-=failure;}
        Check(committed.IsClosed && committed.IsAccepted && committed.RemovedCount==3 && d.ObjectManagerEntries().Count==1,"Postcommit observer was reported as refused source command");d.Undo();Exact(d,before);
    }
    private static void Predicate()
    {
        var d=Fixture(false,true);var point=d.Records().Single(row=>row.Category.StartsWith("Project.TRIGGERLINK",StringComparison.Ordinal));
        d.SetField(point,point.Fields.Single(field=>field.Name=="node.type"),"64");using var policy=new EditorThingRequestPolicy();using var request=policy.Begin(d,Creatures(d),EditorThingCommand.Modify);
        Check(request.UsedCount==1 && request.RequiresPrompt,"Source creature operand predicate was incorrectly filtered by point node.type");
    }
}
