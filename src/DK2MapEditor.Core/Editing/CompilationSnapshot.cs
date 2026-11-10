namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Independent current bytes and working metadata for background
    /// validation/compilation. No disk round-trip, history, event handlers or
    /// live-map save counters are shared with the worker.</summary>
    public NativeMapDocument CreateCompilationSnapshot()
    {
        if(transactionOpen||activePenStroke is not null)
            throw new InvalidOperationException("Finish the active edit before starting a compilation task.");
        return ClonePropertiesDocument().Document;
    }
}
