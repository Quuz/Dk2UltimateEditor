namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Original Things_ManagerAdd counts each accepted insertion and
    /// each physical replacement removal, retaining one parent Undo. The sampled
    /// preview and explicit choices are resolved once without random resampling.</summary>
    public int ApplyEditorObjectBatchInsertion(ObjectBatchInsertionPlan plan, IEnumerable<int>? selectedCandidateIndices = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var inserted = 0;
        EditEditorDrawing("Batch add objects", () =>
        {
            var selected = ResolveObjectBatchPlacement(plan.Placement, selectedCandidateIndices);
            var removed = selected.Where(candidate => candidate.Replacement is not null)
                .Select(candidate => candidate.Replacement!.Record)
                .DistinctBy(record => (record.File, record.Offset)).Count();
            var increments = checked((uint)(selected.Count + removed));
            inserted = ApplyObjectBatchInsertionCore(plan,selected.Select(candidate=>candidate.Index),null,"Batch add objects",editorCreation:true);
            return new(inserted != 0, inserted == 0 ? 0 : increments);
        });
        return inserted;
    }
}
