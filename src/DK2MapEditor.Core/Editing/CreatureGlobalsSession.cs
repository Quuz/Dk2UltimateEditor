namespace DK2MapEditor.Core.Editing;

/// <summary>One private context for the original nine creature/creature-spell pages.
/// Row editors and resets write only to WorkingDocument until the common Apply.</summary>
public sealed record CreatureGlobalsJobWarning(int Index, string Group, long Total);
public sealed record CreatureGlobalsSection(string Name, string Family)
{
    public IReadOnlyList<string> Fields => Array.AsReadOnly(GlobalDefinitionDefaults.Rules(Family).Where(rule => rule.Section == Name).Select(rule => rule.Name).ToArray());
}

public sealed class CreatureGlobalsSession : IDisposable
{
    internal CreatureGlobalsSession(NativeMapDocument owner, NativeMapDocument working,
        Dictionary<NativeFile, NativeFile> files, long revision, byte[] fingerprint)
    { Owner = owner; WorkingDocument = working; Files = files; Revision = revision; Fingerprint = fingerprint; }
    internal NativeMapDocument Owner { get; }
    internal Dictionary<NativeFile, NativeFile> Files { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal bool Closed { get; private set; }
    internal bool Applying { get; set; }
    public NativeMapDocument WorkingDocument { get; }
    public static IReadOnlyList<string> Families { get; } = Array.AsReadOnly(new[] { "Creatures", "CrSpells" });
    public static IReadOnlyList<CreatureGlobalsSection> Sections { get; } = Array.AsReadOnly(new[] {
        new CreatureGlobalsSection("Attributes", "Creatures"), new("Parameters", "Creatures"), new("Flags", "Creatures"),
        new("Attacks", "Creatures"), new("Resistances", "Creatures"), new("Possession", "Creatures"),
        new("Jobs", "Creatures"), new("Light", "Creatures"), new("CrSpells", "CrSpells") });
    public void Dispose() => Closed = true;
}

/// <summary>Immutable delta bound to both the live owner and the private context.</summary>
public sealed class CreatureGlobalsPlan
{
    internal CreatureGlobalsPlan(CreatureGlobalsSession session, long revision, byte[] fingerprint,
        IReadOnlyList<GlobalDefinitionChange> changes, IReadOnlyList<CreatureGlobalsJobWarning> jobWarnings)
    { Session = session; Revision = revision; Fingerprint = fingerprint; Changes = changes; JobWarnings = jobWarnings; }
    internal CreatureGlobalsSession Session { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    public IReadOnlyList<GlobalDefinitionChange> Changes { get; }
    public IReadOnlyList<CreatureGlobalsJobWarning> JobWarnings { get; }
    public bool Changed => Changes.Count != 0;
    public int ChangedRows => Changes.Select(row => (row.Family, row.Index)).Distinct().Count();
}

public sealed partial class NativeMapDocument
{
    public CreatureGlobalsSession CreateCreatureGlobalsSession()
    {
        if (transactionOpen || !GlobalDefinitionRows().Any(row => CreatureGlobalsSession.Families.Contains(row.Family) && row.Editable))
            throw new InvalidOperationException("A verified global-variable owner outside an edit is required.");
        var fingerprint = UnusedScriptFingerprint(); var copy = ClonePropertiesDocument();
        return new(this, copy.Document, copy.Files, EditRevision, fingerprint);
    }

    public bool IsCreatureGlobalsSessionCurrent(CreatureGlobalsSession session) => session is not null && !session.Closed
        && !session.Applying && ReferenceEquals(session.Owner, this) && !transactionOpen && session.Revision == EditRevision
        && UnusedScriptFingerprint().AsSpan().SequenceEqual(session.Fingerprint);

    private void VerifyCreatureGlobalsSession(CreatureGlobalsSession session)
    {
        if (!IsCreatureGlobalsSessionCurrent(session))
            throw new InvalidOperationException("Refresh global variables after owner changes or Cancel.");
    }

    public void ResetCreatureGlobals(CreatureGlobalsSession session, string section, int? index = null)
    {
        VerifyCreatureGlobalsSession(session);
        if (!CreatureGlobalsSession.Sections.Any(page => page.Name == section)) throw new ArgumentException("Choose one of the nine source creature pages.");
        session.WorkingDocument.ResetGlobalDefinitions(section, index is { } selected ? new[] { selected } : null);
    }

    public CreatureGlobalsPlan PrepareCreatureGlobals(CreatureGlobalsSession session)
    {
        VerifyCreatureGlobalsSession(session);
        var working = session.WorkingDocument;
        if (working.transactionOpen || session.Files.Count != TransactionFiles().Count()
            || !session.Files.Values.ToHashSet().SetEquals(working.TransactionFiles()))
            throw new InvalidOperationException("The global-variable context changed its file topology or has an unfinished edit.");

        // Read semantic deltas only from the two verified source families.
        // Read-only and opaque fields never become transaction permissions.
        var liveRows = GlobalDefinitionRows().Where(row => CreatureGlobalsSession.Families.Contains(row.Family)).ToArray();
        var draftRows = working.GlobalDefinitionRows().Where(row => CreatureGlobalsSession.Families.Contains(row.Family)).ToArray();
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
        // context excludes the other five families, unused creature slots, padding,
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
                throw new InvalidOperationException("The private context changed bytes outside the creature and creature-spell families.");
        }
        var jobFields = CreatureGlobalsSession.Sections.Single(section => section.Name == "Jobs").Fields;
        var warnings = changes.Where(change => change.Family == "Creatures" && jobFields.Contains(change.Field))
            .Select(change => change.Index).Distinct().SelectMany(index => working.CreateCreatureJobsDraft(
                draftRows.Single(row => row.Family == "Creatures" && row.Index == index)).Totals.Where(total => total.NeedsAcknowledgement)
                .Select(total => new CreatureGlobalsJobWarning(index, total.Group, total.Total))).ToArray();
        return new(session, working.EditRevision, working.UnusedScriptFingerprint(), Array.AsReadOnly(changes.ToArray()), Array.AsReadOnly(warnings));
    }

    public bool IsCreatureGlobalsPlanCurrent(CreatureGlobalsPlan plan) => plan is not null && IsCreatureGlobalsSessionCurrent(plan.Session)
        && !plan.Session.WorkingDocument.transactionOpen && plan.Revision == plan.Session.WorkingDocument.EditRevision
        && plan.Session.WorkingDocument.UnusedScriptFingerprint().AsSpan().SequenceEqual(plan.Fingerprint);

    public void ApplyCreatureGlobals(CreatureGlobalsPlan plan, bool acknowledgeNonstandardJobTotals = false)
    {
        if (!IsCreatureGlobalsPlanCurrent(plan)) throw new InvalidOperationException("Refresh global variables after the live or private context changes.");
        if (!acknowledgeNonstandardJobTotals && plan.JobWarnings.Count != 0)
            throw new InvalidOperationException("A changed creature job probability total differs from 0 or 100%; acknowledge this current plan before Apply.");
        plan.Session.Applying = true;
        try { SetGlobalDefinitionValues(plan.Changes); plan.Session.Dispose(); }
        finally { plan.Session.Applying = false; }
    }
}
