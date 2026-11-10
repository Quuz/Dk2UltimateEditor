namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Explicit original editor Save: accepted equal values still count
    /// once. Trusted sparse edits retain normal no-op semantics. Existing modern
    /// Undo for actual parameter changes is retained.</summary>
    public void ApplyScenarioEditorParameterDraft(ScenarioParameterDraft draft)
    {
        VerifyScenarioParameterDraft(draft);
        if (transactionOpen || activePenStroke is not null)
            throw new InvalidOperationException("Finish the current edit before accepting scenario parameters.");
        var record = draft.Record;
        var clear = ScenarioTypeDefaults.ClearsParametersOnSave(draft.Type);
        var start = record.Offset + (IsLegacyProject ? 16 : 8);
        var length = IsLegacyProject ? 32 : 8;
        var values = draft.Fields.Select(field => (Field: field, Bytes: field.Encode(draft.Values[field.Name]))).ToArray();
        var changed = draft.NativeGateKey != draft.NativeGateBaselineKey || (clear && record.File.Data.Span.Slice(start,length).IndexOfAnyExcept((byte)0) >= 0)
            || values.Any(value => !value.Bytes.AsSpan().SequenceEqual(record.File.Data.Span.Slice(record.Offset+value.Field.Offset,value.Field.Size)));
        if (!changed)
        {
            CountAcceptedUnchangedScenarioSave();
            return;
        }
        Edit("Edit scenario parameters", () =>
        {
            if (clear) Array.Clear(record.File.Bytes,start,length);
            foreach (var value in values) value.Bytes.CopyTo(record.File.Bytes,record.Offset+value.Field.Offset);
            ApplyNativeGateParameterBinding(draft);
        });
    }

    // Shared by the ordinary and inline-alias original parent Save callers.
    // Carry the accepted non-Undo count through both existing history stacks.
    // Complete overflow checks before changing any history or counter state.
    private void CountAcceptedUnchangedScenarioSave()
    {
        var nextCount = checked(changeCount + 1);
        var nextRevision = checked(EditRevision + 1);
        Change Rebase(Change entry) => entry with
        {
            BeforeCount = checked(entry.BeforeCount + 1), AfterCount = checked(entry.AfterCount + 1),
            BeforeEditorChangeCount = unchecked(entry.BeforeEditorChangeCount + 1),
            AfterEditorChangeCount = unchecked(entry.AfterEditorChangeCount + 1)
        };
        var priorUndo = undo.Select(Rebase).ToArray();
        var priorRedo = redo.Select(Rebase).ToArray();
        undo.Clear(); undo.AddRange(priorUndo); redo.Clear(); redo.AddRange(priorRedo);
        changeCount = nextCount; editorChangeCount = unchecked(editorChangeCount + 1); EditRevision = nextRevision;
        HistoryChanged?.Invoke(this, EventArgs.Empty); Changed?.Invoke(this, EventArgs.Empty);
    }
}
