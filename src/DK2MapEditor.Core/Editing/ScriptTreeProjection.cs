namespace DK2MapEditor.Core.Editing;

public enum ScriptTreeMarker { None, Empty, Missing, Cycle, Shared }

/// <summary>A read-only visual projection; OR/next events are siblings and AND
/// events are children. Incoming connections retain their exact graph context.</summary>
public sealed class ScriptTreeEntry
{
    internal readonly List<ScriptTreeEntry> ChildItems = [];
    public IReadOnlyList<ScriptTreeEntry> Children { get; }
    public ushort EventID { get; }
    public ScriptEdge? Incoming { get; }
    public ushort IncomingEventID { get; }
    public string? ProjectScopeIdentity { get; }
    public ScriptTreeMarker Marker { get; }
    public bool IsScope { get; }
    internal ScriptTreeEntry(ushort id, ScriptEdge? incoming, ushort parent, ScriptTreeMarker marker = ScriptTreeMarker.None, bool isScope = false)
    {
        EventID = id; Incoming = incoming; IncomingEventID = parent; Marker = marker; IsScope = isScope; Children = ChildItems.AsReadOnly();
        if (incoming?.Owner.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal) == true)
            ProjectScopeIdentity = incoming.Scope + ":" + string.Join(",", incoming.Owner.Fields.Where(field => field.Name is "player" or "creature" or "band" or "door" or "object" or "area")
                .Select(field => field.Name + "=" + incoming.Owner.Read(field)));
    }
}

public sealed class ScriptTreeProjection
{
    public IReadOnlyList<ScriptTreeEntry> Roots { get; }
    public IReadOnlyList<ScriptTreeEntry> Orphans { get; }
    public bool Truncated { get; }
    public int EntryCount { get; }
    private ScriptTreeProjection(List<ScriptTreeEntry> roots, List<ScriptTreeEntry> orphans, bool truncated, int count)
    { Roots = roots.AsReadOnly(); Orphans = orphans.AsReadOnly(); Truncated = truncated; EntryCount = count; }
    private sealed record Step(List<ScriptTreeEntry> Siblings, ScriptEdge? Edge, ushort ID, ushort Parent, int Depth, bool Exit = false);

    public static ScriptTreeProjection Build(ScriptGraph graph, IEnumerable<ScriptEdge>? visibleRoots = null, int maximumEntries = 5000, int maximumChildDepth = 128)
    {
        if (maximumEntries < 1) throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        if (maximumChildDepth < 0) throw new ArgumentOutOfRangeException(nameof(maximumChildDepth));
        var rootEdges = (visibleRoots ?? graph.Roots).ToArray();
        var roots = new List<ScriptTreeEntry>(); var orphans = new List<ScriptTreeEntry>();
        var emitted = new HashSet<ushort>(); var ancestors = new HashSet<ushort>(); var count = 0; var truncated = false;
        void Append(List<ScriptTreeEntry> siblings, ScriptEdge? edge, ushort id)
        {
            // Explicit enter/exit steps keep graph ancestors across the sibling
            // chain without recursion, while visual depth grows only for AND.
            var pending = new Stack<Step>(); pending.Push(new(siblings, edge, id, 0, 0));
            while (pending.TryPop(out var step))
            {
                if (step.Exit) { ancestors.Remove(step.ID); continue; }
                if (count >= maximumEntries) { truncated = true; break; }
                count++;
                if (step.ID == 0 && step.Edge is not null)
                { step.Siblings.Add(new(0, step.Edge, step.Parent, ScriptTreeMarker.Empty)); continue; }
                if (!graph.Events.TryGetValue(step.ID, out var item))
                { step.Siblings.Add(new(0, step.Edge, step.Parent, ScriptTreeMarker.Missing)); continue; }
                var marker = ancestors.Contains(item.ID) ? ScriptTreeMarker.Cycle : !emitted.Add(item.ID) ? ScriptTreeMarker.Shared : ScriptTreeMarker.None;
                var node = new ScriptTreeEntry(item.ID, step.Edge, step.Parent, marker); step.Siblings.Add(node);
                if (marker != ScriptTreeMarker.None) continue;
                ancestors.Add(item.ID); pending.Push(step with { Exit = true });
                var next = item.Edges.Single(link => link.Field.Name is "NextOr" or "or" or "next");
                if (next.Target != 0) pending.Push(new(step.Siblings, next, next.Target, item.ID, step.Depth));
                var child = item.Edges.FirstOrDefault(link => link.Field.Name is "ChildAnd" or "and");
                if (child is not null)
                {
                    if (child.Target != 0 && step.Depth >= maximumChildDepth) truncated = true;
                    else pending.Push(new(node.ChildItems, child, child.Target, item.ID, step.Depth + 1));
                }
            }
            ancestors.Clear();
        }
        foreach (var edge in rootEdges)
        {
            if (count >= maximumEntries) { truncated = true; break; }
            var root = new ScriptTreeEntry(0, edge, 0, isScope: true); roots.Add(root); count++;
            Append(root.ChildItems, edge, edge.Target);
        }
        // Root reachability is independent of rendering limits: a hidden deep
        // child must not be misrepresented as an unlinked component.
        var rooted = new HashSet<ushort>(); var reachable = new Stack<ushort>(rootEdges.Where(edge => edge.Target != 0).Select(edge => edge.Target));
        while (reachable.TryPop(out var id))
            if (rooted.Add(id) && graph.Events.TryGetValue(id, out var item))
                foreach (var edge in item.Edges) if (edge.Target != 0) reachable.Push(edge.Target);
        var unlinked = graph.Events.Keys.Where(id => !rooted.Contains(id)).ToHashSet();
        var incoming = graph.Events.Values.Where(item => unlinked.Contains(item.ID)).SelectMany(item => item.Edges).Where(edge => edge.Target != 0).Select(edge => edge.Target).ToHashSet();
        // Component heads precede continuations regardless of signed project ID
        // ordering. Cyclic components with no head are emitted by the fallback.
        foreach (var id in unlinked.Where(id => !incoming.Contains(id)).Order().Concat(unlinked.Order()))
        {
            if (emitted.Contains(id)) continue;
            if (count >= maximumEntries) { truncated = true; break; }
            Append(orphans, null, id);
        }
        return new(roots, orphans, truncated, count);
    }
}
