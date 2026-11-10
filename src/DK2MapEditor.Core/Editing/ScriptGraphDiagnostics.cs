using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

/// <summary>Read-only scenario checks. Semantic warnings never normalize old data.</summary>
internal static class ScriptGraphDiagnostics
{
    internal static IReadOnlyList<MapDiagnostic> Validate(NativeMapDocument document)
    {
        var graph = ScriptGraph.Build(document);
        var diagnostics = new List<MapDiagnostic>();
        void Add(string code, string message, NativeRecord record, DiagnosticSeverity severity = DiagnosticSeverity.Warning) =>
            diagnostics.Add(new(severity, code, message + $" at 0x{record.Offset:X}.", record.File.SourcePath));
        foreach (var id in graph.DuplicateIDs)
            Add("DK2N003", $"Script IDs must be nonzero and unique: {id}", graph.Events[id].Record, DiagnosticSeverity.Error);
        var links = graph.Roots.Concat(graph.Events.Values.SelectMany(item => item.Edges)).ToArray();
        foreach (var edge in links)
            if (edge.Target != 0 && !graph.Events.ContainsKey(edge.Target))
                Add("DK2N004", $"Dangling script reference {edge.Field.Name}={edge.Target}", edge.Owner, DiagnosticSeverity.Error);
        foreach (var item in graph.Events.Values)
        {
            if (item.Type is null) Add("DK2N015", $"Event #{item.ID} has an unverified condition/action type; its arguments and scope cannot be checked", item.Record);
            if (document.IsLegacyProject && (item.Record.Category == "Project.ACTION" ? item.ID < 32768 : item.ID >= 32768))
                Add("DK2N013", $"Event #{item.ID} has the wrong signed-short ID category: historical conditions use 1–32767 and actions use 32768–65535", item.Record);
        }
        // Duplicate IDs make ownership and traversal ambiguous. Do not report speculative
        // scope/backlink/cycle conclusions based on whichever duplicate was indexed first.
        if (graph.DuplicateIDs.Count != 0) return diagnostics;

        // Properties_UpdateTriggers retains shifted targets; the original
        // compiler checks upper bounds only when traversing a bound action.
        // Unused actions remain editable and are warnings rather than blockers.
        var boundEvents = new HashSet<ushort>();
        var targetStack = new Stack<ushort>(graph.Roots.Where(root => root.Target != 0).Select(root => root.Target));
        while (targetStack.TryPop(out var targetID))
            if (boundEvents.Add(targetID) && graph.Events.TryGetValue(targetID, out var targetEvent))
                foreach (var edge in targetEvent.Edges) if (edge.Target != 0) targetStack.Push(edge.Target);
        foreach (var item in graph.Events.Values.Where(item => item.Type is { RecordSignature: 0xD6, Type: 1 or 0x16 or 0x31 }))
        {
            string Coordinate(int number) => item.Record.Read(item.Record.Fields.Single(field => field.Name == (document.IsLegacyProject ? "param" : "Param") + number));
            var x = long.Parse(Coordinate(5), System.Globalization.CultureInfo.InvariantCulture);
            var y = long.Parse(Coordinate(7), System.Globalization.CultureInfo.InvariantCulture);
            if (x >= document.Width || y >= document.Height)
                Add("DK2N029", $"Event #{item.ID} ({item.Type!.Name}) has a target beyond the map's upper bounds: ({x}, {y}) / ({document.Width}, {document.Height})", item.Record,
                    document.IsLegacyProject && boundEvents.Contains(item.ID) ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning);
        }

        var bindings = document.ScriptBindings();
        foreach (var binding in bindings.Where(binding => ScriptGraph.IsKnownScope(binding.Root.Scope) && binding.Owner is null
            && (document.IsLegacyProject || binding.Root.Target != 0)))
            Add("DK2N017", $"Scenario scope {binding.Root.Scope} has a missing, inactive or ambiguous owner", binding.Root.Owner);
        foreach (var group in bindings.Where(binding => binding.Owner is not null).GroupBy(binding =>
            (binding.Owner!.Scope, binding.Owner.Record.File, binding.Owner.Record.Offset, binding.Owner.MemberIndex, binding.Owner.PlayerID)).Where(group => group.Count() > 1))
            Add("DK2N018", $"The same physical owner has {group.Count()} bindings in scope {group.Key.Scope}; the historical compiler uses only the first", group.First().Root.Owner);

        // One iterative traversal per distinct scope, not one traversal per owner.
        // The original has nine scopes, so shared branches remain bounded and deduplicated.
        foreach (var roots in graph.Roots.GroupBy(edge => edge.Scope))
        {
            if (!ScriptGraph.IsKnownScope(roots.Key))
            {
                foreach (var root in roots) Add("DK2N016", $"Scenario owner has unverified scope {root.Scope}; compatibility was not inferred", root.Owner);
                continue;
            }
            var visited = new HashSet<ushort>(); var pending = new Stack<ushort>();
            foreach (var root in roots) if (root.Target != 0) pending.Push(root.Target);
            while (pending.TryPop(out var id))
            {
                if (!visited.Add(id) || !graph.Events.TryGetValue(id, out var item)) continue;
                if (item.Type is not null && (LegacyEventSchemas.AllowedScopes(item.Type) & roots.Key) == 0)
                    Add("DK2N012", $"Event #{id} ({item.Type.Name}) is incompatible with scope {roots.Key}", item.Record);
                foreach (var edge in item.Edges) if (edge.Target != 0) pending.Push(edge.Target);
            }
        }

        // Full graph, including unlinked components; report DFS back edges, not every
        // owner that happens to reach a cycle. Shared acyclic graphs are not cycles.
        var colors = new Dictionary<ushort, byte>(); var stack = new Stack<(ushort ID, int Next)>();
        foreach (var start in graph.Events.Keys.Order())
        {
            if (colors.GetValueOrDefault(start) != 0) continue;
            colors[start] = 1; stack.Push((start, 0));
            while (stack.TryPop(out var frame))
            {
                var item = graph.Events[frame.ID];
                if (frame.Next == item.Edges.Count) { colors[frame.ID] = 2; continue; }
                stack.Push((frame.ID, frame.Next + 1));
                var edge = item.Edges[frame.Next];
                if (edge.Target == 0 || !graph.Events.ContainsKey(edge.Target)) continue;
                var color = colors.GetValueOrDefault(edge.Target);
                if (color == 1) Add("DK2N011", $"Scenario cycle: #{frame.ID}.{edge.Field.Name} → #{edge.Target}", edge.Owner);
                else if (color == 0) { colors[edge.Target] = 1; stack.Push((edge.Target, 0)); }
            }
        }

        if (document.IsLegacyProject)
        {
            var incoming = links.Where(edge => edge.Target != 0).GroupBy(edge => edge.Target)
                .ToDictionary(group => group.Key, group => group.Select(edge => ScriptGraph.IsEvent(edge.Owner) ? ScriptGraph.ID(edge.Owner) : (ushort)0).Distinct().ToArray());
            foreach (var item in graph.Events.Values)
            {
                var parents = incoming.GetValueOrDefault(item.ID) ?? [];
                // A shared node has no single representable legacy backlink.
                // Only compare the unambiguous cases; preserve shared graphs unchanged.
                if (parents.Length > 1) continue;
                var expected = parents.Length == 1 ? parents[0] : (ushort)0;
                var actual = BinaryPrimitives.ReadUInt16LittleEndian(item.Record.File.Data.Span[(item.Record.Offset + LegacyProjectSchemas.Size("NODE"))..]);
                if (actual != expected) Add("DK2N014", $"Event #{item.ID} has an inconsistent previous-event backlink ({actual}; expected {expected})", item.Record);
            }
        }
        return diagnostics;
    }
}
