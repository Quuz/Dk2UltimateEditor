namespace DK2MapEditor.Core.Editing;

public sealed record ScriptEdge(NativeRecord Owner, NativeField Field, ushort Target, uint Scope = 0);
public sealed record ScriptEvent(ushort ID, NativeRecord Record, LegacyEventSchemas.EventType? Type, IReadOnlyList<ScriptEdge> Edges);

/// <summary>A non-recursive index of events and actual root/link fields. Shared
/// graphs, cycles, missing targets and duplicate IDs remain visible, not repaired.</summary>
public sealed class ScriptGraph
{
    public static bool IsKnownScope(uint scope) => scope is 1 or 2 or 4 or 8 or 16 or 32 or 64 or 128 or 256;
    public IReadOnlyDictionary<ushort, ScriptEvent> Events { get; }
    public IReadOnlyList<ScriptEdge> Roots { get; }
    public IReadOnlyList<ushort> DuplicateIDs { get; }
    private ScriptGraph(IReadOnlyDictionary<ushort, ScriptEvent> events, IReadOnlyList<ScriptEdge> roots, IReadOnlyList<ushort> duplicates)
    { Events = events; Roots = roots; DuplicateIDs = duplicates; }

    public static bool IsEvent(NativeRecord record) => record.File.Signature == 0xD2 && record.Length == 24
        && record.File.UInt32(record.Offset) is 0xD5 or 0xD6 || record.Category is "Project.TRIGGER" or "Project.ACTION";
    public static ushort ID(NativeRecord record) => ushort.Parse(record.Read(record.Fields.Single(field => field.Name is "ID" or "id")));
    public static IReadOnlyList<ScriptEdge> Edges(NativeRecord record)
    {
        var project = record.Category is "Project.TRIGGER" or "Project.ACTION";
        var condition = project ? record.Category == "Project.TRIGGER" : record.File.Signature == 0xD2 && record.File.UInt32(record.Offset) == 0xD5;
        return record.Fields.Where(field => project ? condition ? field.Name is "or" or "and" : field.Name == "next"
            : condition ? field.Name is "NextOr" or "ChildAnd" : field.Name == "NextOr")
            .Select(field => new ScriptEdge(record, field, ushort.Parse(record.Read(field)))).ToArray();
    }
    public static ScriptGraph Build(NativeMapDocument document)
    {
        var records = document.Records();
        var groups = records.Where(IsEvent).GroupBy(ID).ToArray();
        var events = groups.ToDictionary(group => group.Key, group =>
        {
            var record = group.First(); return new ScriptEvent(group.Key, record, EntityReferences.EventType(record), Edges(record));
        });
        var roots = new List<ScriptEdge>();
        foreach (var record in records.Where(record => !IsEvent(record)))
        {
            var projectLink = record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal);
            var scope = projectLink ? uint.Parse(record.Read(record.Fields.Single(field => field.Name == "node.type"))) : record.Category switch
            {
                "Map" => 32u, "GAMEPLAYER" => 1u, "GAMEAREA" => 4u, "GAMEBAND" => 8u,
                "GAMEHERO" or "GAMENEUTRAL" or "GAMEKEEPER" => 2u, "GAMEDOOR" => 64u,
                "GAMEOBJECT" when record.Fields.FirstOrDefault(field => field.Name == "bObjectID") is { } objectID => ValueCatalogues.ForTemplate("MagicalObject", ValueCatalogues.GameVersion(document)).Any(choice => choice.Value == record.Read(objectID)) ? 256u : 128u,
                _ when record.Category.StartsWith("BandHero", StringComparison.Ordinal) => 16u, _ => 0u
            };
            foreach (var field in record.Fields.Where(field => projectLink ? field.Name == "trigger" : field.Name.EndsWith("Trigger", StringComparison.Ordinal)))
                roots.Add(new(record, field, ushort.Parse(record.Read(field)), scope));
        }
        roots.AddRange(document.NativeOrphanAreaRoots());
        roots.AddRange(document.NativeOrphanOwnerRoots());
        return new(events, roots, groups.Where(group => group.Count() != 1 || group.Key == 0).Select(group => group.Key).ToArray());
    }
    public IReadOnlySet<ushort> Reachable(ushort root)
    {
        var visited = new HashSet<ushort>(); var pending = new Stack<ushort>(); if (root != 0) pending.Push(root);
        while (pending.TryPop(out var id))
            if (visited.Add(id) && Events.TryGetValue(id, out var item))
                foreach (var edge in item.Edges) if (edge.Target != 0) pending.Push(edge.Target);
        return visited;
    }
    public bool HasCycle(ushort root)
    {
        var colors = new Dictionary<ushort, byte>(); var pending = new Stack<(ushort ID, bool Exit)>();
        if (root != 0) pending.Push((root, false));
        while (pending.TryPop(out var step))
        {
            if (step.Exit) { colors[step.ID] = 2; continue; }
            var color = colors.GetValueOrDefault(step.ID);
            if (color == 1) return true;
            if (color == 2 || !Events.TryGetValue(step.ID, out var item)) continue;
            colors[step.ID] = 1; pending.Push((step.ID, true));
            foreach (var edge in item.Edges) if (edge.Target != 0) pending.Push((edge.Target, false));
        }
        return false;
    }
}

public sealed partial class NativeMapDocument
{
    private static ScriptEdge CurrentScriptEdge(ScriptGraph graph, NativeRecord owner, NativeField field) => graph.Roots.Concat(graph.Events.Values.SelectMany(item => item.Edges))
        .SingleOrDefault(edge => SameField(owner, field, edge.Owner, edge.Field)) ?? throw new ArgumentException("This field is not a current scenario link.");

    private static void RequireUnambiguousGraph(ScriptGraph graph)
    {
        if (graph.DuplicateIDs.Count != 0) throw new InvalidDataException("Repair zero/duplicate scenario IDs before editing graph links.");
    }

    private static IReadOnlySet<uint> LinkScopes(ScriptGraph graph, ScriptEdge edge)
    {
        if (!ScriptGraph.IsEvent(edge.Owner)) return new HashSet<uint> { edge.Scope };
        var id = ScriptGraph.ID(edge.Owner);
        return graph.Roots.Where(root => graph.Reachable(root.Target).Contains(id)).Select(root => root.Scope).ToHashSet();
    }

    public void SetScriptLink(NativeRecord owner, NativeField field, ushort target) => Edit("Change scenario link", () =>
    {
        var graph = ScriptGraph.Build(this); RequireUnambiguousGraph(graph);
        var edge = CurrentScriptEdge(graph, owner, field);
        if (target != 0)
        {
            if (!graph.Events.ContainsKey(target)) throw new ArgumentException("The selected scenario event does not exist.");
            var reachable = graph.Reachable(target);
            if (graph.HasCycle(target)) throw new InvalidOperationException("Repair the cyclic branch before connecting it.");
            if (ScriptGraph.IsEvent(owner) && reachable.Contains(ScriptGraph.ID(owner))) throw new InvalidOperationException("This link would introduce a scenario cycle.");
            foreach (var scope in LinkScopes(graph, edge))
                if (!ScriptGraph.IsKnownScope(scope) || reachable.Any(id => !graph.Events.TryGetValue(id, out var item) || item.Type is null || (LegacyEventSchemas.AllowedScopes(item.Type) & scope) == 0))
                    throw new InvalidOperationException("The target branch is missing, unverified or incompatible with its scenario scope.");
        }
        SetField(edge.Owner, edge.Field, target.ToString());
        RefreshProjectBacklinks(edge.Target, target);
    });

    public NativeRecord InsertScriptEvent(NativeRecord owner, NativeField field, LegacyEventSchemas.EventType type)
    {
        ushort addedID = 0;
        Edit("Insert scenario event", () =>
        {
            var graph = ScriptGraph.Build(this); RequireUnambiguousGraph(graph);
            var edge = CurrentScriptEdge(graph, owner, field);
            if (graph.Reachable(edge.Target).Any(id => !graph.Events.ContainsKey(id))) throw new InvalidDataException("Repair the dangling branch before inserting an event.");
            if (graph.HasCycle(edge.Target)) throw new InvalidOperationException("Repair the cyclic branch before inserting an event.");
            var scopes = LinkScopes(graph, edge);
            if (scopes.Any(scope => !ScriptGraph.IsKnownScope(scope) || (LegacyEventSchemas.AllowedScopes(type) & scope) == 0)) throw new InvalidOperationException("This event is not supported by its scenario scope.");
            var ownerID = ScriptGraph.IsEvent(owner) ? ScriptGraph.ID(owner) : (ushort)0;
            var added = AddScenarioEvent(type.RecordSignature, new(0, 0), type);
            addedID = ScriptGraph.ID(added);
            var refreshed = ScriptGraph.Build(this); added = refreshed.Events[addedID].Record;
            var next = refreshed.Events[addedID].Edges.Single(link => link.Field.Name is "NextOr" or "or" or "next");
            SetField(added, next.Field, edge.Target.ToString());
            var currentOwner = ownerID == 0 ? owner : refreshed.Events[ownerID].Record;
            SetField(currentOwner, currentOwner.Fields.Single(candidate => candidate.Name == field.Name), addedID.ToString());
            RefreshProjectBacklinks(edge.Target, addedID);
        });
        return ScriptGraph.Build(this).Events[addedID].Record;
    }

    private void RefreshProjectBacklinks(params ushort[] ids)
    {
        if (!IsLegacyProject) return;
        var graph = ScriptGraph.Build(this);
        foreach (var id in ids.Where(id => id != 0).Distinct())
        {
            if (!graph.Events.TryGetValue(id, out var item)) continue;
            var incoming = graph.Roots.Concat(graph.Events.Values.SelectMany(item => item.Edges)).Where(edge => edge.Target == id)
                .Select(edge => ScriptGraph.IsEvent(edge.Owner) ? ScriptGraph.ID(edge.Owner) : (ushort)0).Distinct().ToArray();
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(item.Record.File.Bytes.AsSpan(item.Record.Offset + LegacyProjectSchemas.Size("NODE")), incoming.Length == 1 ? incoming[0] : (ushort)0);
        }
    }
}
