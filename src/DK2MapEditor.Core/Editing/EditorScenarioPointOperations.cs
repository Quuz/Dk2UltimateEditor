namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>The original Delete Point command removes its complete event
    /// graph, including OR/next siblings, and releases its owner's identity.
    /// Shared, dangling or cyclic graphs require explicit repair first.</summary>
    public void DeleteEditorScenarioPoint(ScriptEdge requested)
    {
        RequireEditorPointOperation();
        var (graph,root,remove)=ValidateEditorScenarioPointDeletion(requested);
        var owner=OwnerForScriptScope(root);
        // Empty native roots are inherent fields. Accepting the source command
        // still counts once when there are no event or binding bytes to remove.
        if(!HasStoredScriptScope(root)){CountAcceptedUnchangedScenarioSave();return;}
        Edit("Delete scenario point",()=>
        {
            SetField(root.Owner,root.Field,"0");
            if(!IsLegacyProject){RemoveNativeAreaScope(root);RemoveNativeOwnerScope(root,releaseIdentity:true);}
            foreach(var id in remove)
                foreach(var edge in graph.Events[id].Edges)SetField(edge.Owner,edge.Field,"0");
            foreach(var record in remove.Select(id=>graph.Events[id].Record).OrderByDescending(record=>record.Offset))DeleteRecord(record);
            if(IsLegacyProject)
            {
                if(owner is not null && PointUniqueScope(owner.Scope))WritePointIdentity(owner,"0");
                DeleteLegacyRecord(root.Owner);
            }
        });
    }

    private (ScriptGraph Graph,ScriptEdge Root,IReadOnlySet<ushort> Remove) ValidateEditorScenarioPointDeletion(ScriptEdge requested)
    {
        var graph=ScriptGraph.Build(this);RequireUnambiguousGraph(graph);
        var root=CurrentScopeRoot(graph,requested);
        if(graph.HasCycle(root.Target))throw new InvalidOperationException("Repair the cyclic point before deleting it.");
        var remove=graph.Reachable(root.Target);
        if(remove.Any(id=>!graph.Events.ContainsKey(id)))throw new InvalidDataException("Repair the dangling point before deleting it.");
        foreach(var incoming in AllScriptEdges(graph).Where(edge=>remove.Contains(edge.Target)))
            if(!SameField(root.Owner,root.Field,incoming.Owner,incoming.Field)
                && !(ScriptGraph.IsEvent(incoming.Owner) && remove.Contains(ScriptGraph.ID(incoming.Owner))))
                throw new InvalidOperationException("An event in this point is used outside it. Detach the shared connection first.");
        return (graph,root,remove);
    }

    /// <summary>Move a stored project point one physical list position. Native
    /// roots are inherent entity fields and do not have this stored list order.</summary>
    public ScriptEdge MoveEditorScenarioPoint(ScriptEdge requested,bool down)
    {
        RequireEditorPointOperation();
        if(!IsLegacyProject)throw new InvalidOperationException("Native entity roots have no stored Action Point list order.");
        var graph=ScriptGraph.Build(this);RequireUnambiguousGraph(graph);
        var root=CurrentScopeRoot(graph,requested);
        var points=Records().Where(record=>record.Category.StartsWith("Project.TRIGGERLINK",StringComparison.Ordinal)).ToArray();
        var index=Array.FindIndex(points,point=>SameRecord(point,root.Owner));
        var next=index+(down?1:-1);
        if(index<0 || next<0 || next>=points.Length)throw new InvalidOperationException("There is no point in this direction.");
        var neighbor=points[next];var first=down?root.Owner:neighbor;var second=down?neighbor:root.Owner;
        if(first.File!=second.File || first.Offset+first.Length!=second.Offset)throw new InvalidDataException("The stored point list is not contiguous.");
        var movedOffset=down?first.Offset+neighbor.Length:first.Offset;
        Edit("Move scenario point",()=>
        {
            var firstBytes=first.File.Bytes.AsSpan(first.Offset,first.Length).ToArray();
            var secondBytes=second.File.Bytes.AsSpan(second.Offset,second.Length).ToArray();
            secondBytes.CopyTo(first.File.Bytes,first.Offset);
            firstBytes.CopyTo(first.File.Bytes,first.Offset+second.Length);
        });
        return ScriptGraph.Build(this).Roots.Single(edge=>edge.Owner.File==root.Owner.File && edge.Owner.Offset==movedOffset);
    }

    private void RequireEditorPointOperation()
    {
        if(transactionOpen || activePenStroke is not null)throw new InvalidOperationException("Finish the active edit before changing an Action Point.");
        _=checked(changeCount+1);_=checked(EditRevision+1);
    }
}
