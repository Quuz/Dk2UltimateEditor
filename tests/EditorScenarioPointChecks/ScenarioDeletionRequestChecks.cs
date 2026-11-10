using System.Reflection;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void ScenarioDeletionRequests(bool native)
    {
        for(var shape=0;shape<4;shape++)
        {
            var d=Fixture(native);var owner=Owner(d,2);var root=shape==0?Create(d,owner):Branch(d,owner);
            ushort action=0,following=0;
            if(shape!=0)
            {
                var graph=ScriptGraph.Build(d);action=graph.Events[root.Target].Edges.Single(edge=>edge.Field.Name is "and" or "ChildAnd").Target;
                var next=graph.Events[root.Target].Edges.Single(edge=>edge.Field.Name is "or" or "NextOr");
                following=ScriptGraph.ID(d.InsertScriptEvent(next.Owner,next.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED")));
                next=ScriptGraph.Build(d).Events[action].Edges.Single();
                d.InsertScriptEvent(next.Owner,next.Field,ScriptGraph.Build(d).Events[action].Type!);
            }
            var prompt=shape<2?EditorConfirmationPrompt.ScenarioPointRemoval:shape==2?EditorConfirmationPrompt.ScenarioConditionRemoval:EditorConfirmationPrompt.ScenarioActionRemoval;
            var bit=shape<2?EditorScenarioDeletionPolicy.PointBit:shape==2?EditorScenarioDeletionPolicy.ConditionBit:EditorScenarioDeletionPolicy.ActionBit;
            var expected=shape==0?0:shape==1?4:shape==2?3:1;
            var shared=new EditorRequestPreferences();using var policy=new EditorScenarioDeletionPolicy(shared);
            EditorScenarioDeletionRequest Begin(EditorScenarioDeletionPolicy target,NativeMapDocument map)=>shape<2
                ?target.BeginPoint(map,map.FindScriptScope(Owner(map,2))!):target.BeginEvent(map,shape==2?root.Target:action);
            var before=OwnerState(d);var history=d.GetHistory();var revision=d.EditRevision;
            using(var cancel=Begin(policy,d))
            {
                Check(cancel.RequiresPrompt && cancel.IsCurrent && cancel.Prompt==prompt && cancel.EventCount==expected,"Wrong scenario command/extent preview");
                Check(!cancel.Decline(true) && !cancel.IsClosed,"Checked No must remain disabled");
            }
            ExactOwnerState(d,before);Check(policy.Flags==0 && d.EditRevision==revision,"Checked close remembered/changed deletion");
            using(var no=Begin(policy,d))Check(no.Decline() && no.IsClosed && !no.IsAccepted,"No did not close without acceptance");
            ExactOwnerState(d,before);Check(d.GetHistory().Undo.SequenceEqual(history.Undo) && d.GetHistory().Redo.SequenceEqual(history.Redo),"No changed history");
            using(var yes=Begin(policy,d))Check(yes.Accept(true)==1 && yes.IsAccepted && yes.IsClosed,"Accepted scenario command did not complete exactly once");
            Accepted(d,history,revision,1);Check(policy.Flags==bit && ScriptGraph.Build(d).Events.Count==4-(shape==0?4:expected),"Deletion extent or independent preference wrong");
            if(shape>=2)Check(ScriptGraph.Build(d).Events.ContainsKey(following),"Following condition was deleted");
            var after=OwnerState(d);d.Undo();ExactOwnerState(d,before);Check(policy.Flags==bit,"Undo rolled back request preference");
            d.Redo();ExactOwnerState(d,after);d.Undo();ExactOwnerState(d,before);
            using(var sibling=new EditorScenarioDeletionPolicy(shared))using(var suppressed=Begin(sibling,d))
                Check(!suppressed.RequiresPrompt,"Shared session did not retain independent Yes bit across policies");
            // A different kind still asks; a new process/session starts with no bits.
            using(var fresh=new EditorScenarioDeletionPolicy())using(var asks=Begin(fresh,d))Check(asks.RequiresPrompt,"New session inherited old suppression");
            using(var independent=shape<2?policy.BeginEvent(d,following==0?AddTemporaryCondition(d):following):policy.BeginPoint(d,d.FindScriptScope(Owner(d,2))!))
                Check(independent.RequiresPrompt,"Another command reused this command's suppression bit");
            // Reset only its three bits, preserving manager/region/used preference state.
            using(var reset=new EditorScenarioDeletionPolicy(new EditorRequestPreferences(bit|0x112)))
            {reset.ResetSuppression();Check(reset.Flags==0x112,"Scenario reset cleared other caller bits");}
            var saved=d.SaveCopy(Path.Combine(directory,$"request-{Format(native)}-{shape}"));var reopened=NativeMapDocument.Load(saved);
            using(var other=new EditorScenarioDeletionPolicy(shared))using(var persisted=Begin(other,reopened))Check(!persisted.RequiresPrompt,"Session bit did not cross Save/reopen documents");
        }
        Console.WriteLine($"SCENARIO DELETION REQUEST DETAIL 4 {Format(native)} empty/nonempty point/condition/action; exact deletion extents, continuation, Close/No/checked Yes, separate shared bits, full metadata UndoRedo, reset and Save/reopen");
        Pass(Format(native)+" scenario deletion request extents and session preferences");
    }

    private static ushort AddTemporaryCondition(NativeMapDocument d)
    {
        var root=Create(d,Owner(d,32));return ScriptGraph.ID(d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED")));
    }

    private static void ScenarioDeletionRequestGuards(bool native)
    {
        var d=Fixture(native);var root=Branch(d,Owner(d,2));var shared=new EditorRequestPreferences();using var policy=new EditorScenarioDeletionPolicy(shared);
        using(var current=policy.BeginPoint(d,root))
        {
            Reject(()=>policy.BeginEvent(d,root.Target));
            using var things=new EditorThingRequestPolicy(shared);
            Reject(()=>things.Begin(d,[d.ObjectManagerEntries().First().Record],EditorThingCommand.Remove));
            Check(current.IsCurrent,"Other caller acquired/invalidated a live scenario lease");
        }
        using(var things=new EditorThingRequestPolicy(shared))using(var active=things.Begin(d,[d.ObjectManagerEntries().First().Record],EditorThingCommand.Remove))
        {Reject(()=>policy.BeginPoint(d,root));Reject(policy.ResetSuppression);Check(active.IsCurrent,"Scenario reset stole another caller's lease");}
        using(var current=policy.BeginPoint(d,root))
        {
            Check(MemoryMarshal.TryGetArray(d.MainFile.Data,out var raw),"Raw request fixture");raw.Array![raw.Offset+raw.Count-1]^=1;
            Check(!current.IsCurrent,"Raw edit did not invalidate request");Reject(()=>current.Accept(true));raw.Array[raw.Offset+raw.Count-1]^=1;
        }
        var before=OwnerState(d);
        using(var stale=policy.BeginPoint(d,root)){d.Paint([new(9,9)],new(5,2,1));d.Undo();Check(!stale.IsCurrent,"Undo revived a stale request");Reject(()=>stale.Accept(true));}
        ExactOwnerState(d,before);Check(policy.Flags==0,"Stale acceptance remembered a reply");
        using(var pen=d.BeginLivePenStroke(new(5,2,1)))Reject(()=>policy.BeginPoint(d,root));
        d.Edit("nested scenario request",()=>Reject(()=>policy.BeginPoint(d,root)));
        Reject(()=>policy.BeginEvent(d,0));Reject(()=>policy.BeginPoint(Fixture(native),root));
        var other=Create(d,Owner(d,2,true));d.SetScriptLink(other.Owner,other.Field,root.Target);
        Reject(()=>policy.BeginPoint(d,root));Reject(()=>policy.BeginEvent(d,root.Target));d.SetScriptLink(other.Owner,other.Field,0);
        var count=typeof(NativeMapDocument).GetField("changeCount",BindingFlags.Instance|BindingFlags.NonPublic)!;var old=(long)count.GetValue(d)!;
        count.SetValue(d,long.MaxValue);Reject(()=>policy.BeginPoint(d,root));count.SetValue(d,old);
        before=OwnerState(d);var history=d.GetHistory();
        EventHandler observer=(_,_)=>throw new InvalidOperationException("committed observer");d.Changed+=observer;
        using(var committed=policy.BeginPoint(d,root))
        {Reject(()=>committed.Accept(true));Check(committed.IsAccepted && committed.IsClosed && policy.Flags==EditorScenarioDeletionPolicy.PointBit,"Postcommit observer was reported as uncommitted or retained the lease");}
        d.Changed-=observer;Check(d.GetHistory().Undo.Count==history.Undo.Count+1,"Observer failure lost committed history");d.Undo();ExactOwnerState(d,before);
        using(var active=policy.BeginPoint(d,root)){policy.ResetSuppression();Check(active.IsClosed && policy.Flags==0,"Reset did not cancel its inactive request and clear its bits");}
        Pass(Format(native)+" scenario request stale/raw/foreign/shared/pen/nested/overflow, cross-policy lease and committed-observer guards");
    }
}
