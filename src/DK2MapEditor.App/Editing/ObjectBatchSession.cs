using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Session-local drafts, independent for each source manager family.</summary>
public sealed class ObjectBatchSession
{
    internal Dictionary<PlacedThingKind, string[]> Templates { get; }
    internal int Mode, Policy = 4, Frequency = 2;
    internal bool WholeMap = true;
    internal ObjectBatchFlags Flags = ObjectBatchFlags.AskOptions | ObjectBatchFlags.Preview | ObjectBatchFlags.IgnoreAsk;
    internal PlacedThingKind? LastKind;
    public ObjectBatchSession() : this(new Dictionary<PlacedThingKind, string[]>()) { }
    internal ObjectBatchSession(Dictionary<PlacedThingKind, string[]> templates) => Templates = templates;
}
