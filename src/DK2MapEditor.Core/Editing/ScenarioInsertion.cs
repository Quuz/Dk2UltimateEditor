namespace DK2MapEditor.Core.Editing;

public enum ScenarioInsertCommand { NewRoot,AppendCondition,InsertCondition,AppendAction,InsertAction }

/// <summary>Read-only source parent/sibling placement. The shared creation
/// wizard performs the eventual insertion; this resolver never edits a graph.</summary>
public static class ScenarioInsertion
{
    private static bool Same(ScriptEdge first,ScriptEdge second)=>ReferenceEquals(first.Owner.File,second.Owner.File)
        &&first.Owner.Offset==second.Owner.Offset&&first.Owner.Length==second.Owner.Length&&first.Owner.Category==second.Owner.Category
        &&first.Field==second.Field&&first.Target==second.Target;
    private static ScriptEdge Next(ScriptEvent item)=>item.Edges.Single(edge=>edge.Field.Name is "NextOr" or "or" or "next");
    private static ScriptEdge Child(ScriptEvent item)=>item.Edges.SingleOrDefault(edge=>edge.Field.Name is "ChildAnd" or "and")
        ??throw new InvalidOperationException("This event has no condition child list.");
    private static ScriptEdge Checked(ScriptEdge edge)
    {
        if(edge.Owner.Read(edge.Field)!=edge.Target.ToString(System.Globalization.CultureInfo.InvariantCulture))
            throw new InvalidOperationException("Refresh the changed scenario connection.");
        return edge;
    }
    private static ScriptEdge Tail(ScriptGraph graph,ScriptEdge head)
    {
        var seen=new HashSet<ushort>();var edge=Checked(head);
        while(edge.Target!=0)
        {
            if(!seen.Add(edge.Target))throw new InvalidOperationException("The sibling list is cyclic.");
            if(!graph.Events.TryGetValue(edge.Target,out var item))throw new InvalidDataException("The sibling list has a missing event.");
            edge=Checked(Next(item));
        }
        return edge;
    }
    public static ScriptEdge Resolve(ScriptGraph graph,ScriptTreeEntry scope,ScriptTreeEntry? selected,ScenarioInsertCommand command)
    {
        ArgumentNullException.ThrowIfNull(graph);ArgumentNullException.ThrowIfNull(scope);
        if(!Enum.IsDefined(command))throw new ArgumentOutOfRangeException(nameof(command));
        if(graph.DuplicateIDs.Count!=0)throw new InvalidDataException("Scenario IDs must be nonzero and unique.");
        if(!scope.IsScope||scope.Incoming is not {} requested||!ScriptGraph.IsKnownScope(requested.Scope))
            throw new InvalidOperationException("Choose a current scenario point.");
        var root=graph.Roots.SingleOrDefault(edge=>Same(edge,requested)&&edge.Scope==requested.Scope)
            ??throw new InvalidOperationException("Refresh the scenario point.");
        Checked(root);
        if(command==ScenarioInsertCommand.NewRoot)return Tail(graph,root);
        if(selected is null||selected.EventID==0)throw new InvalidOperationException("Select a condition or action.");

        // Preserve the actual visual occurrence in a shared graph. A numeric
        // event ID or globally unique incoming-edge guess loses its parent.
        var pending=new Stack<(ScriptTreeEntry Parent,int[] Path)>();pending.Push((scope,[]));int[]? path=null;
        var visited=new HashSet<ScriptTreeEntry>();
        while(pending.TryPop(out var step))
        {
            if(!visited.Add(step.Parent))throw new InvalidOperationException("Invalid scenario projection.");
            for(var i=0;i<step.Parent.Children.Count;i++)
            {
                var child=step.Parent.Children[i];var next=step.Path.Append(i).ToArray();
                if(ReferenceEquals(child,selected)){path=next;break;}
                if(child.Children.Count!=0)pending.Push((child,next));
            }
            if(path is not null)break;
        }
        if(path is null)throw new InvalidOperationException("The selected occurrence does not belong to this point.");
        var parent=scope;var head=root;ScriptEdge? incoming=null;ScriptEvent? item=null;
        for(var level=0;level<path.Length;level++)
        {
            var index=path[level];
            head=parent.IsScope?root:Child(graph.Events[parent.EventID]);var edge=Checked(head);
            // Every preceding sibling must still occupy its original place.
            // Merely finding the selected ID somewhere in the root is unsafe.
            for(var i=0;i<=index;i++)
            {
                var entry=parent.Children[i];
                if(entry.EventID==0||entry.Incoming is not {} old||!Same(edge,old)||edge.Target!=entry.EventID
                    ||!graph.Events.TryGetValue(entry.EventID,out item)||ScriptGraph.ID(item.Record)!=entry.EventID)
                    throw new InvalidOperationException("Refresh the changed scenario tree occurrence.");
                Checked(edge);incoming=edge;
                if(i<index)edge=Checked(Next(item));
            }
            if(level<path.Length-1)parent=parent.Children[index];
        }
        if(item is null||incoming is null)throw new InvalidOperationException("No selected event.");
        var condition=item.Edges.Any(edge=>edge.Field.Name is "ChildAnd" or "and");
        if(command is ScenarioInsertCommand.InsertCondition or ScenarioInsertCommand.InsertAction)
        {
            // Source disables action insertion before a top-level condition.
            // Its linker also cannot put the first action directly on a root.
            if(command==ScenarioInsertCommand.InsertAction&&parent.IsScope&&(condition||Same(incoming,root)))
                throw new InvalidOperationException("An action must follow a root condition or be inside a condition.");
            return incoming;
        }
        return Tail(graph,condition?Child(item):head);
    }
}
