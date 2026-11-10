namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Source PathsSetGroups manager ordering. This is a read-only
    /// projection; graph APIs still expose every head and remain unchanged.</summary>
    public IReadOnlyList<PatrolRoute> EditorPatrolRoutes()
    {
        var areas = PatrolAreas();
        // The source uses IDs as identities and zero as an array terminator.
        // It has no defined grouping for invalid/duplicate identities. Keep
        // the existing diagnostic view for those imports, without renumbering.
        if (areas.Any(point => point.ID is 0 or > 255 || point.NextID > 255)
            || areas.Select(point => point.ID).Distinct().Count() != areas.Count)
            return PatrolRoutes();
        var index = areas.ToDictionary(point => point.ID);
        var paths = new List<List<uint>>();
        foreach (var seed in areas)
        {
            if (seed.NextID == 0 || paths.Any(path => path.Contains(seed.ID))) continue;
            var path = new List<uint> { seed.ID };
            paths.Add(path);
            while (index.TryGetValue(path[^1], out var point))
            {
                if (point.NextID == 0) break;
                var repeated = path.Contains(point.NextID);
                path.Add(point.NextID);
                if (repeated) break;
            }
            // Source scans file order and restarts after every insertion.
            for (var candidate = 0; candidate < areas.Count;)
            {
                var point = areas[candidate];
                if (point.NextID != 0 && path.Contains(point.NextID) && !path.Contains(point.ID))
                {
                    path.Insert(path.IndexOf(point.NextID), point.ID);
                    candidate = 0;
                }
                else candidate++;
            }
            if (path[^1] != path[0])
            {
                var repeated = -1;
                var closing = -1;
                for (var i = 0; i < path.Count && repeated < 0; i++)
                    for (var j = 0; j < path.Count; j++)
                        if (i != j && path[i] == path[j]) { repeated = i; closing = j; break; }
                if (repeated > 0 && index.TryGetValue(path[repeated - 1], out var predecessor))
                {
                    var start = repeated;
                    while (start != closing && path[start] != predecessor.NextID) start++;
                    // Exact source partial-loop rotation, including the
                    // repeated closing occurrence. No serialized edge changes.
                    var rotated = path.Skip(start).Take(path.Count - start - 1)
                        .Concat(path.Skip(repeated).Take(start - repeated)).ToArray();
                    for (var i = 0; i < closing - repeated; i++) path[repeated + i] = rotated[i];
                    path[closing] = path[repeated];
                }
            }
            // Inserted predecessors can belong to another branch. Remove
            // those false tree occurrences, then restart, as the source does.
            for (var i = 0; i + 1 < path.Count;)
            {
                if (index.TryGetValue(path[i], out var point) && point.NextID != path[i + 1])
                {
                    path.RemoveAt(i + 1);
                    i = 0;
                }
                else i++;
            }
        }
        var result = new List<PatrolRoute>();
        foreach (var path in paths)
        {
            var points = new List<PatrolPoint>();
            var used = new HashSet<uint>();
            uint end = 0;
            foreach (var id in path)
            {
                if (!used.Add(id) || !index.TryGetValue(id, out var point)) { end = id; break; }
                points.Add(point);
            }
            result.Add(new(points.AsReadOnly(), end));
        }
        return result.AsReadOnly();
    }
}
