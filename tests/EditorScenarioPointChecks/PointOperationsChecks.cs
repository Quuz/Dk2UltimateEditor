using System.Reflection;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void DeleteAll(bool native)
    {
        foreach(var scope in Scopes)
        {
            var d=Fixture(native);var owner=Owner(d,scope);var root=Branch(d,owner);
            var next=ScriptGraph.Build(d).Events[root.Target].Edges.Single(edge=>edge.Field.Name is "or" or "NextOr");
            d.InsertScriptEvent(next.Owner,next.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
            root=d.FindScriptScope(owner)!;
            var removed=ScriptGraph.Build(d).Reachable(root.Target);Check(removed.Count==3,"Complete point branch/OR fixture");
            var other=Owner(d,scope==1?2u:1u);_=Branch(d,other);
            var graph=ScriptGraph.Build(d);var retained=Events(d).Where(pair=>!removed.Contains(pair.Key)).ToDictionary();
            var bytes=Snapshot(d);var history=d.GetHistory();var revision=d.EditRevision;
            d.DeleteEditorScenarioPoint(root);Accepted(d,history,revision,1);ExactEvents(d,retained);
            Check(native?d.FindScriptScope(owner)?.Target==0:d.FindScriptScope(owner) is null,"Deleted point still bound");
            if(!native && UniqueScopes.Contains(scope))Check(owner.Record.Read(owner.Record.Fields.Single(field=>field.Name==Identity(owner)))=="0","Deleted point did not release source owner identity");
            Check(d.FindScriptScope(other)!.Target!=0,"Unrelated point lost");
            d.Undo();Exact(bytes);d.Redo();ExactEvents(d,retained);
            var saved=d.SaveCopy(Path.Combine(directory,$"deleted-{Format(native)}-{scope}"));var reopened=NativeMapDocument.Load(saved);ExactEvents(reopened,retained);
        }
        Pass(Format(native)+" Delete all nine points removes AND and OR/next events, releases IDs, preserves unrelated graph, exact Undo/Redo and Save/reopen");
    }

    private static void DeleteEmpty(bool native)
    {
        var d=Fixture(native);var owner=Owner(d,2);var root=Create(d,owner);
        d.Paint([new(8,8)],new(5,2,1));var painted=Snapshot(d);d.Undo();var bytes=Snapshot(d);var history=d.GetHistory();var revision=d.EditRevision;
        d.DeleteEditorScenarioPoint(root);Accepted(d,history,revision,1);
        if(native){Exact(bytes);Check(!d.HasStoredScriptScope(d.FindScriptScope(owner)!) && d.GetHistory().Redo.Count==0,"Empty native point not deleted");d.Undo();Exact(bytes);Check(d.HasStoredScriptScope(d.FindScriptScope(owner)!),"Empty native Undo lost point");}
        else{Check(d.FindScriptScope(owner) is null,"Empty project point retained");Check(owner.Record.Read(owner.Record.Fields.Single(field=>field.Name==Identity(owner)))=="0","Empty point ID retained");d.Undo();Exact(bytes);}
        Pass(Format(native)+" empty stored Delete accepts exactly once, releases identity and restores the point with Undo");
    }

    private static void DeleteRefusals(bool native)
    {
        var d=Fixture(native);var owner=Owner(d,2);var root=Branch(d,owner);var other=Create(d,Owner(d,2,true));
        d.SetScriptLink(other.Owner,other.Field,root.Target);var bytes=Snapshot(d);var revision=d.EditRevision;var history=d.GetHistory();
        Reject(()=>d.DeleteEditorScenarioPoint(root));Exact(bytes);var refused=d.GetHistory();Check(d.EditRevision==revision && refused.ChangeCount==history.ChangeCount && refused.EditorChangeCount==history.EditorChangeCount && refused.Undo.SequenceEqual(history.Undo) && refused.Redo.SequenceEqual(history.Redo) && refused.SnapshotBytes==history.SnapshotBytes,"Shared Delete changed counters/history");
        d.SetScriptLink(other.Owner,other.Field,0);
        var continuation=ScriptGraph.Build(d).Events[root.Target].Edges.Single(edge=>edge.Field.Name is "or" or "NextOr");
        RawFixtureField(continuation.Owner,continuation.Field.Name,root.Target.ToString());bytes=Snapshot(d);Reject(()=>d.DeleteEditorScenarioPoint(root));Exact(bytes);
        RawFixtureField(continuation.Owner,continuation.Field.Name,"0");RawFixtureField(root.Owner,root.Field.Name,"65535");bytes=Snapshot(d);Reject(()=>d.DeleteEditorScenarioPoint(root));Exact(bytes);
        RawFixtureField(root.Owner,root.Field.Name,root.Target.ToString());bytes=Snapshot(d);
        using(var pen=d.BeginLivePenStroke(new(5,2,1))){Reject(()=>d.DeleteEditorScenarioPoint(root));}Exact(bytes);
        d.Edit("nested delete",()=>Reject(()=>d.DeleteEditorScenarioPoint(root)));Exact(bytes);
        var count=typeof(NativeMapDocument).GetField("changeCount",BindingFlags.Instance|BindingFlags.NonPublic)!;var prior=(long)count.GetValue(d)!;count.SetValue(d,long.MaxValue);Reject(()=>d.DeleteEditorScenarioPoint(root));Exact(bytes);count.SetValue(d,prior);
        using var s=d.CreateEditorScenarioPointSession(d.FindScriptScope(Owner(d,2,true)));s.SelectOwner(Owner(d,2,true));var plan=d.PrepareEditorScenarioPoint(s);d.DeleteEditorScenarioPoint(root);Reject(()=>d.ApplyEditorScenarioPoint(plan));
        Pass(Format(native)+" shared/cyclic/dangling/live-pen/nested/overflow Delete refuses atomically and accepted Delete invalidates an old point plan");
    }

    private static void MovePoints()
    {
        var d=Fixture(false);var first=Branch(d,Owner(d,32));_=Branch(d,Owner(d,1));_=Branch(d,Owner(d,2));var original=Snapshot(d);var events=Events(d);
        foreach(var expected in new[]{new uint[]{1,32,2},new uint[]{1,2,32}})
        {var history=d.GetHistory();var revision=d.EditRevision;first=d.MoveEditorScenarioPoint(first,true);Accepted(d,history,revision,1);Check(ScriptGraph.Build(d).Roots.Select(root=>root.Scope).SequenceEqual(expected),"Point Down order");ExactEvents(d,events);}
        var bytes=Snapshot(d);Reject(()=>d.MoveEditorScenarioPoint(first,true));Exact(bytes);
        first=d.MoveEditorScenarioPoint(first,false);first=d.MoveEditorScenarioPoint(first,false);Exact(original);Reject(()=>d.MoveEditorScenarioPoint(first,false));Exact(original);
        d.Undo();Check(ScriptGraph.Build(d).Roots.Select(root=>root.Scope).SequenceEqual(new uint[]{1,32,2}),"Point Move Undo order");d.Redo();Exact(original);
        var native=Fixture(true);var inherent=native.FindScriptScope(Owner(native,32))!;bytes=Snapshot(native);
        Reject(()=>native.MoveEditorScenarioPoint(inherent,true));Reject(()=>native.CreateEditorScenarioPointSession(insertBefore:inherent));Exact(bytes);
        Pass("project point Up/Down preserves all event bytes and original physical order with exact Undo/Redo; native inherent order refuses explicit unsupported insertion/move");
    }
}
