namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Original accepted parent Save for the four inline alias pages.
    /// A changed parent remains one modern Undo; an equal accepted parent adds
    /// one source count without replacing either history stack. Private wizard
    /// pages and trusted composition continue using the generic draft API.</summary>
    public void ApplyScenarioEditorAliasParameterDraft(ScenarioParameterDraft parameters, ScenarioAliasEventDraft aliases)
    {
        if (transactionOpen || activePenStroke is not null)
            throw new InvalidOperationException("Finish the current edit before accepting scenario parameters.");
        var revision = EditRevision;
        ApplyScenarioAliasParameterDraft(parameters, aliases);
        if (EditRevision == revision) CountAcceptedUnchangedScenarioSave();
    }
}
