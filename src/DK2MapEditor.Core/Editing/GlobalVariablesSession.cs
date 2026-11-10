namespace DK2MapEditor.Core.Editing;

/// <summary>One private context for the original five global-variable pages.
/// Row editors and resets write only to WorkingDocument until the common Apply.</summary>
public sealed class GlobalVariablesSession : IDisposable
{
    internal GlobalVariablesSession(NativeMapDocument owner, NativeMapDocument working,
        Dictionary<NativeFile, NativeFile> files, long revision, byte[] fingerprint)
    { Owner = owner; WorkingDocument = working; Files = files; Revision = revision; Fingerprint = fingerprint; }
    internal NativeMapDocument Owner { get; }
    internal Dictionary<NativeFile, NativeFile> Files { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal bool Closed { get; private set; }
    internal bool Applying { get; set; }
    public NativeMapDocument WorkingDocument { get; }
    public static IReadOnlyList<string> Families { get; } = Array.AsReadOnly(new[] { "Terrains", "Rooms", "Doors", "Traps", "Spells" });
    public void Dispose() => Closed = true;
}

/// <summary>Immutable delta bound to both the live owner and the private context.</summary>
public sealed class GlobalVariablesPlan
{
    internal GlobalVariablesPlan(GlobalVariablesSession session, long revision, byte[] fingerprint,
        IReadOnlyList<GlobalDefinitionChange> changes)
    { Session = session; Revision = revision; Fingerprint = fingerprint; Changes = changes; }
    internal GlobalVariablesSession Session { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    public IReadOnlyList<GlobalDefinitionChange> Changes { get; }
    public bool Changed => Changes.Count != 0;
    public int ChangedRows => Changes.Select(row => (row.Family, row.Index)).Distinct().Count();
}

public sealed partial class NativeMapDocument
{
    public GlobalVariablesSession CreateGlobalVariablesSession()
    {
        if (transactionOpen || !GlobalDefinitionRows().Any(row => GlobalVariablesSession.Families.Contains(row.Family) && row.Editable))
            throw new InvalidOperationException("A verified global-variable owner outside an edit is required.");
        var fingerprint = UnusedScriptFingerprint(); var copy = ClonePropertiesDocument();
        return new(this, copy.Document, copy.Files, EditRevision, fingerprint);
    }

    public bool IsGlobalVariablesSessionCurrent(GlobalVariablesSession session) => session is not null && !session.Closed
        && !session.Applying && ReferenceEquals(session.Owner, this) && !transactionOpen && session.Revision == EditRevision
        && UnusedScriptFingerprint().AsSpan().SequenceEqual(session.Fingerprint);

    private void VerifyGlobalVariablesSession(GlobalVariablesSession session)
    {
        if (!IsGlobalVariablesSessionCurrent(session))
            throw new InvalidOperationException("Refresh global variables after owner changes or Cancel.");
    }

    public void ResetGlobalVariables(GlobalVariablesSession session, string family, int? index = null)
    {
        VerifyGlobalVariablesSession(session);
        if (!GlobalVariablesSession.Families.Contains(family)) throw new ArgumentException("Choose one of the five source global-variable pages.");
        session.WorkingDocument.ResetGlobalDefinitions(family, index is { } selected ? new[] { selected } : null);
    }

    public GlobalVariablesPlan PrepareGlobalVariables(GlobalVariablesSession session)
    {
        VerifyGlobalVariablesSession(session);
        var working = session.WorkingDocument;
        if (working.transactionOpen || session.Files.Count != TransactionFiles().Count()
            || !session.Files.Values.ToHashSet().SetEquals(working.TransactionFiles()))
            throw new InvalidOperationException("The global-variable context changed its file topology or has an unfinished edit.");

        // Read semantic deltas only from the five verified source families.
        // Read-only and opaque fields never become transaction permissions.
        var liveRows = GlobalDefinitionRows().Where(row => GlobalVariablesSession.Families.Contains(row.Family)).ToArray();
        var draftRows = working.GlobalDefinitionRows().Where(row => GlobalVariablesSession.Families.Contains(row.Family)).ToArray();
        if (!liveRows.Select(row => (row.Family, row.Index, row.ID, row.Editable))
            .SequenceEqual(draftRows.Select(row => (row.Family, row.Index, row.ID, row.Editable))))
            throw new InvalidOperationException("The private context changed a source catalogue or verified layout.");
        var changes = new List<GlobalDefinitionChange>();
        foreach (var (before, after) in liveRows.Zip(draftRows))
        {
            var original = GlobalDefinitionValues(before); var updated = working.GlobalDefinitionValues(after);
            if (!original.Select(value => (value.Name, value.Editable)).SequenceEqual(updated.Select(value => (value.Name, value.Editable))))
                throw new InvalidOperationException("The private context changed its source fields.");
            foreach (var (oldValue, newValue) in original.Zip(updated))
                if (newValue.Value != oldValue.Value)
                {
                    if (!oldValue.Editable) throw new InvalidOperationException("An unavailable global row cannot be changed.");
                    changes.Add(new(before.Family, before.Index, newValue.Name, newValue.Value));
                }
        }

        // Reconstruct the only permitted byte result. Comparing the entire
        // context excludes creature arrays, padding, Room.CeilingHeight,
        // headers, map settings, objects, scripts, metadata and extensions.
        var replay = ClonePropertiesDocument();
        if (IsLegacyProject)
        {
            // A private row edit followed by reverting its value may leave a
            // defaults-only family materialized. Accept that private state,
            // but commit only semantic changes, so live no-op stays byte exact.
            var present = draftRows.Where(row => row.Record is not null).Select(row => row.Family).Distinct().ToArray();
            replay.Document.EnsureProjectDefinitionFamilies(present);
        }
        replay.Document.SetGlobalDefinitionValues(changes);
        foreach (var (live, draft) in session.Files)
        {
            var expected = replay.Files[live];
            if (draft.SourcePath != expected.SourcePath || draft.ExportPath != expected.ExportPath
                || !draft.Data.Span.SequenceEqual(expected.Data.Span))
                throw new InvalidOperationException("The private context changed bytes outside the five global-variable families.");
        }
        return new(session, working.EditRevision, working.UnusedScriptFingerprint(), Array.AsReadOnly(changes.ToArray()));
    }

    public bool IsGlobalVariablesPlanCurrent(GlobalVariablesPlan plan) => plan is not null && IsGlobalVariablesSessionCurrent(plan.Session)
        && !plan.Session.WorkingDocument.transactionOpen && plan.Revision == plan.Session.WorkingDocument.EditRevision
        && plan.Session.WorkingDocument.UnusedScriptFingerprint().AsSpan().SequenceEqual(plan.Fingerprint);

    public void ApplyGlobalVariables(GlobalVariablesPlan plan)
    {
        if (!IsGlobalVariablesPlanCurrent(plan)) throw new InvalidOperationException("Refresh global variables after the live or private context changes.");
        plan.Session.Applying = true;
        try { SetGlobalDefinitionValues(plan.Changes); plan.Session.Dispose(); }
        finally { plan.Session.Applying = false; }
    }
}
