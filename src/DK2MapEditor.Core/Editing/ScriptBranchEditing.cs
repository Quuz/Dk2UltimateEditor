namespace DK2MapEditor.Core.Editing;

public enum ScriptBranchMove { Up, Down, Indent, Outdent }

public sealed partial class NativeMapDocument
{
    private static ScriptEdge Continuation(ScriptEvent item) => item.Edges.Single(edge => edge.Field.Name is "NextOr" or "or" or "next");
    private static IEnumerable<ScriptEdge> AllScriptEdges(ScriptGraph graph) => graph.Roots.Concat(graph.Events.Values.SelectMany(item => item.Edges));
    private static ScriptEdge? UniqueScriptIncoming(ScriptGraph graph, ushort id)
    {
        var incoming = AllScriptEdges(graph).Where(edge => edge.Target == id).ToArray();
        if (incoming.Length > 1) throw new InvalidOperationException("This event has several incoming connections. Detach the extra connections before moving or deleting it.");
        return incoming.SingleOrDefault();
    }
    private static HashSet<ushort> ScriptBranchIDs(ScriptGraph graph, ushort id)
    {
        RequireUnambiguousGraph(graph);
        if (!graph.Events.TryGetValue(id, out var item)) throw new ArgumentException("The selected scenario event does not exist.");
        if (graph.HasCycle(id)) throw new InvalidOperationException("Repair the cyclic branch before moving or deleting it.");
        if (graph.Reachable(id).Any(target => !graph.Events.ContainsKey(target))) throw new InvalidDataException("Repair the dangling branch before moving or deleting it.");
        // A tree branch includes the selected event and its AND descendants,
        // not the selected event's OR/next siblings (Triggers_SelectMove/Kill).
        var result = new HashSet<ushort> { id };
        foreach (var child in item.Edges.Where(edge => edge.Field.Name is "ChildAnd" or "and")) result.UnionWith(graph.Reachable(child.Target));
        return result;
    }

    /// <summary>Insert the event and its children before the destination's old
    /// target; splice its old continuation into the source connection. No IDs
    /// or event payloads are regenerated. A shared root requires explicit detach.</summary>
    public void MoveScriptBranch(ushort id, NativeRecord destinationOwner, NativeField destinationField) => Edit("Move scenario branch", () =>
    {
        var graph = ScriptGraph.Build(this); var branch = ScriptBranchIDs(graph, id);
        var source = UniqueScriptIncoming(graph, id);
        var destination = CurrentScriptEdge(graph, destinationOwner, destinationField);
        if (source is not null && SameField(source.Owner, source.Field, destination.Owner, destination.Field)) return;
        if (ScriptGraph.IsEvent(destination.Owner) && branch.Contains(ScriptGraph.ID(destination.Owner)))
            throw new InvalidOperationException("A branch cannot be moved into itself or one of its children.");
        var next = Continuation(graph.Events[id]);
        if (next.Target != 0 && branch.Contains(next.Target)) throw new InvalidOperationException("The child branch also shares its continuation. Detach that connection before moving it.");
        if (source is not null) SetField(source.Owner, source.Field, next.Target.ToString());
        SetField(next.Owner, next.Field, "0");
        // Validate the final connection against every reachable destination scope,
        // after detaching, so a valid reorder does not look like a transient cycle.
        SetScriptLink(next.Owner, next.Field, destination.Target);
        SetScriptLink(destination.Owner, destination.Field, id);
        RefreshProjectBacklinks(id, next.Target, destination.Target);
    });

    /// <summary>Original arrow behavior: up/down among siblings, indent into
    /// the next condition's first child, outdent immediately before the parent.</summary>
    public void MoveScriptBranch(ushort id, ScriptBranchMove direction)
    {
        var graph = ScriptGraph.Build(this); _ = ScriptBranchIDs(graph, id);
        var incoming = UniqueScriptIncoming(graph, id);
        var next = Continuation(graph.Events[id]); ScriptEdge? destination = null;
        switch (direction)
        {
            case ScriptBranchMove.Up:
                if (incoming is not null && ScriptGraph.IsEvent(incoming.Owner) && incoming.Field.Name is "NextOr" or "or" or "next")
                    destination = UniqueScriptIncoming(graph, ScriptGraph.ID(incoming.Owner));
                break;
            case ScriptBranchMove.Down:
                if (next.Target != 0) destination = Continuation(graph.Events[next.Target]);
                break;
            case ScriptBranchMove.Indent:
                for (var sibling = next.Target; sibling != 0; sibling = Continuation(graph.Events[sibling]).Target)
                    if (graph.Events[sibling].Edges.FirstOrDefault(edge => edge.Field.Name is "ChildAnd" or "and") is { } child)
                    { destination = child; break; }
                break;
            case ScriptBranchMove.Outdent:
                while (incoming is not null && ScriptGraph.IsEvent(incoming.Owner) && incoming.Field.Name is "NextOr" or "or" or "next")
                    incoming = UniqueScriptIncoming(graph, ScriptGraph.ID(incoming.Owner));
                if (incoming is not null && ScriptGraph.IsEvent(incoming.Owner) && incoming.Field.Name is "ChildAnd" or "and")
                    destination = UniqueScriptIncoming(graph, ScriptGraph.ID(incoming.Owner));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(direction));
        }
        if (destination is null) throw new InvalidOperationException("There is no destination in this direction.");
        MoveScriptBranch(id, destination.Owner, destination.Field);
    }

    /// <summary>Delete the selected event and its AND descendants while retaining
    /// its OR/next continuation. Never remove a descendant used outside the branch.</summary>
    public void DeleteScriptBranch(ushort id) => Edit("Delete scenario branch", () =>
    {
        var (graph,branch,source,next)=ValidateScriptBranchDeletion(id);
        if (source is not null) SetField(source.Owner, source.Field, next.Target.ToString());
        // Clear internal links first so the regular reference-safe record deletion
        // remains in force. Descending offsets preserve DKM ACTION/condition strides.
        foreach (var target in branch)
            foreach (var edge in graph.Events[target].Edges) SetField(edge.Owner, edge.Field, "0");
        foreach (var record in branch.Select(target => graph.Events[target].Record).OrderByDescending(record => record.Offset)) DeleteRecord(record);
        RefreshProjectBacklinks(next.Target);
    });

    private (ScriptGraph Graph,HashSet<ushort> Branch,ScriptEdge? Source,ScriptEdge Next) ValidateScriptBranchDeletion(ushort id)
    {
        var graph = ScriptGraph.Build(this); var branch = ScriptBranchIDs(graph, id);
        var source = UniqueScriptIncoming(graph, id); var next = Continuation(graph.Events[id]);
        foreach (var edge in AllScriptEdges(graph).Where(edge => branch.Contains(edge.Target)))
            if (!(ScriptGraph.IsEvent(edge.Owner) && branch.Contains(ScriptGraph.ID(edge.Owner)))
                && !(source is not null && SameField(source.Owner, source.Field, edge.Owner, edge.Field)))
                throw new InvalidOperationException("A child event is referenced outside this branch. Detach the shared connections before deleting the branch.");
        if (next.Target != 0 && branch.Contains(next.Target)) throw new InvalidOperationException("The child branch also shares its continuation. Detach that connection before deleting it.");
        return (graph,branch,source,next);
    }
}
