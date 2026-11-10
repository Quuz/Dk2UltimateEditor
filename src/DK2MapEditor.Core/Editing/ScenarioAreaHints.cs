namespace DK2MapEditor.Core.Editing;

public sealed record ScenarioAreaOwnerContext(ScriptOwner Owner, ScriptOwner OriginalOwner);
public sealed record ScenarioAreaMetrics(int Width, int Height, int TotalCells, int? PerimeterCells);

public static class ScenarioAreaHints
{
    public static bool Supports(string type) => type is
        "TRIGGER_CLAIMAREA" or "TRIGGER_AREACONTENT" or "TRIGGER_SELECTAREA";
}

public sealed partial class NativeMapDocument
{
    private IReadOnlyList<ScriptOwner> ScenarioAreaOwners(NativeRecord? record, ScriptEdge? insertion)
    {
        var graph = ScriptGraph.Build(this);
        if (graph.DuplicateIDs.Count != 0) return [];
        var id = record is null ? (ushort)0 : ScriptGraph.ID(CurrentScenarioRecord(record));
        if (insertion is not null)
        {
            insertion = CurrentScriptEdge(graph, insertion.Owner, insertion.Field);
            if (ScriptGraph.IsEvent(insertion.Owner)) id = ScriptGraph.ID(insertion.Owner);
        }
        return ScriptBindings().Where(binding => binding.Owner is { Scope: 4 }
            && (insertion is { Scope: not 0 }
                ? SameField(binding.Root.Owner, binding.Root.Field, insertion.Owner, insertion.Field)
                : id != 0 && graph.Reachable(binding.Root.Target).Contains(id)))
            .Select(binding => binding.Owner!).DistinctBy(owner => (owner.Record.File, owner.Record.Offset, owner.Record.Category)).ToArray();
    }

    public IReadOnlyList<ScenarioAreaOwnerContext> ScenarioAreaContexts(ScenarioParameterDraft draft)
    {
        VerifyScenarioParameterDraft(draft);
        if (!ScenarioAreaHints.Supports(draft.Type)) return [];
        return ScenarioAreaOwners(draft.Record, null).Select(owner => new ScenarioAreaOwnerContext(owner, owner)).ToArray();
    }

    /// <summary>Insertion context is resolved in the live graph, then translated
    /// through the session's file correspondence into its private document.
    /// The new private event is deliberately still unlinked before Finish.</summary>
    public IReadOnlyList<ScenarioAreaOwnerContext> ScenarioCreationAreaContexts(ScenarioCreationSession session)
    {
        VerifyScenarioCreation(session);
        if (!ScenarioAreaHints.Supports(session.Type.Name)) return [];
        var original = session.IsEditingExisting ? ScriptGraph.Build(this).Events[session.EventID].Record : null;
        return ScenarioAreaOwners(original, session.Insertion).Select(owner =>
        {
            var file = session.Files[owner.Record.File];
            var record = session.WorkingDocument.Records().Single(current => current.File == file
                && current.Offset == owner.Record.Offset && current.Length == owner.Record.Length && current.Category == owner.Record.Category);
            return new ScenarioAreaOwnerContext(new ScriptOwner(4, record), owner);
        }).ToArray();
    }

    /// <summary>Read the explicitly selected physical AREA, without choosing
    /// the first root of a shared graph or treating an event operand as owner.</summary>
    public ScenarioAreaMetrics? ReadScenarioAreaMetrics(ScenarioParameterDraft draft, ScriptOwner requested)
    {
        VerifyScenarioParameterDraft(draft);
        if (!ScenarioAreaHints.Supports(draft.Type) || requested.Scope != 4)
            throw new ArgumentException("Select an AREA context for this scenario page.");
        var owner = CurrentScriptOwner(requested);
        var identity = EntityReferences.Identity(owner.Record);
        if (identity is null || Records().Select(EntityReferences.Identity).OfType<EntityIdentity>()
            .Count(current => current.Kind == EntityKind.Area && current.Record.Read(current.Field) == owner.Record.Read(identity.Field)) != 1)
            return null;
        var region = GetMapRegion(owner.Record);
        if (region is not { Kind: MapRegionKind.Area }) return null;
        var width = region.Bounds.Width; var height = region.Bounds.Height;
        // Original LONG arithmetic and its unusual single-column perimeter.
        var total = unchecked(width * height);
        int? perimeter = draft.Type == "TRIGGER_CLAIMAREA"
            ? width > 1 ? unchecked((width - 2) * 2 + height * 2) : 1 : null;
        return new(width, height, total, perimeter);
    }
}
