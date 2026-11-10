namespace DK2MapEditor.Core.Editing;

/// <summary>The original mouse caller may allocate Undo even without a paint call.
/// Accepted paint/fill calls increment MapChanges independently of written bytes.</summary>
public readonly record struct EditorDrawingCompletion(bool RetainUndo, uint ChangeIncrements);

public sealed partial class NativeMapDocument
{
    private uint editorChangeCount;
    /// <summary>Reversible 32-bit editor change state. Source painting counts calls;
    /// other edits keep the existing atomic-operation convention until audited.
    /// This is distinct from modern committed-operation count and activity uses.</summary>
    public uint EditorChangeCount => editorChangeCount;

    /// <summary>Actual editor drawing caller, not a raw byte-edit primitive.
    /// Generic no-op Edit/session semantics remain unchanged.</summary>
    public void EditEditorDrawing(string description, Func<EditorDrawingCompletion> drawing)
    {
        ArgumentNullException.ThrowIfNull(drawing);
        if (transactionOpen || activePenStroke is not null)
            throw new InvalidOperationException("Finish the current edit before an editor drawing.");
        var result = default(EditorDrawingCompletion);
        EditCore(description, () =>
        {
            result = drawing();
            if (!result.RetainUndo && result.ChangeIncrements != 0)
                throw new InvalidOperationException("A refused drawing cannot increment the editor change state.");
        }, () => result);
    }
}
