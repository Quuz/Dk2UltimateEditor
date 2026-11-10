namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>The original manager counts each approved physical removal,
    /// while retaining one parent Undo operation for the complete batch.</summary>
    public int ApplyEditorObjectRemoval(ObjectRemovalPlan plan, bool includeScripted = false)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var removed = 0;
        EditEditorDrawing("Batch remove objects", () =>
        {
            removed = ApplyObjectRemovalCore(plan, includeScripted, editorCaller: true);
            return new(removed != 0, checked((uint)removed));
        });
        return removed;
    }

    /// <summary>Things_ManagerModify pass 10 stores every approved Modify/Remove
    /// child and additionally counts each replaced physical object. An accepted
    /// equal modification still retains Undo and increments MapChanges.</summary>
    public int ApplyEditorObjectBatchModification(ObjectBatchModificationPlan plan, bool allowScriptDetachment = false)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var applied = 0;
        EditEditorDrawing("Batch modify objects", () =>
        {
            // The existing core validates ownership, generation, collision consent,
            // script detachments and layouts before publishing any prepared bytes.
            applied = ApplyObjectBatchModificationCore(plan, allowScriptDetachment, true,
                "Batch modify objects", editorManagerCaller: true);
            var modifying = plan.Candidates.Where(candidate => candidate.Decision == ObjectModificationDecision.Modify).ToArray();
            var removed = plan.Candidates.Where(candidate => candidate.Decision == ObjectModificationDecision.Remove).Select(candidate => candidate.Source)
                .Concat(modifying.Where(candidate => candidate.Replacement is not null).Select(candidate => candidate.Replacement!))
                .DistinctBy(entry => (entry.Record.File, entry.Record.Offset)).Count();
            return new(applied != 0, applied == 0 ? 0 : checked((uint)(modifying.Length + removed)));
        });
        return applied;
    }
}
