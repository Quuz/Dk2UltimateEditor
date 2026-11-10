namespace DK2MapEditor.Core.Editing;

public enum EditorRegionRemovalStage { Confirmation, UsedAreas }

/// <summary>The source general confirmation followed, when needed, by the
/// used-area Yes/No/Cancel decision. Neither question mutates the document.</summary>
public sealed class EditorRegionRemovalRequest : IEditorConfirmationRequest
{
    private bool applying;
    internal EditorRegionRemovalRequest(EditorRegionRemovalPolicy policy, NativeMapDocument document,
        IReadOnlyList<NativeRecord> records, MapRegionKind kind, byte[] fingerprint, long revision)
    { Policy = policy; Document = document; Records = records; Kind = kind; Fingerprint = fingerprint; Revision = revision; }
    internal EditorRegionRemovalPolicy Policy { get; }
    internal byte[] Fingerprint { get; }
    internal long Revision { get; }
    public NativeMapDocument Document { get; }
    public IReadOnlyList<NativeRecord> Records { get; }
    public MapRegionKind Kind { get; }
    public int Count => Records.Count;
    public EditorRegionRemovalStage Stage { get; private set; }
    public EditorConfirmationPrompt Prompt => Stage == EditorRegionRemovalStage.UsedAreas ? EditorConfirmationPrompt.UsedAreas
        : Kind switch { MapRegionKind.Area => EditorConfirmationPrompt.RegionArea, MapRegionKind.Gate => EditorConfirmationPrompt.RegionGate, _ => EditorConfirmationPrompt.RegionEffect };
    public bool RequiresPrompt => Stage == EditorRegionRemovalStage.Confirmation ? !Policy.Suppressed : !Policy.UsedAreasSuppressed;
    public int UsedCount => Records.Count(Document.IsEditorRegionUsed);
    public bool IsCurrent => !applying && Policy.IsCurrent(this);
    public bool IsApplying => applying;
    public bool IsClosed { get; internal set; }
    public bool IsAccepted { get; private set; }
    public int RemovedCount { get; private set; }
    public bool CanDecline(bool remember) => IsCurrent && !remember;
    public bool Decline(bool remember = false)
    {
        if (!CanDecline(remember)) return false;
        if (Stage == EditorRegionRemovalStage.UsedAreas) { KeepUsed(); return true; }
        Dispose(); return true;
    }
    public int Accept(bool remember = false)
    {
        if (!IsCurrent) throw new InvalidOperationException("The region-removal request is no longer current.");
        return ApplyReply(remember, keepUsed: false);
    }
    public int KeepUsed()
    {
        if (!IsCurrent || Stage != EditorRegionRemovalStage.UsedAreas) throw new InvalidOperationException("Answer the current used-area request first.");
        return ApplyReply(false, keepUsed: true);
    }
    private int ApplyReply(bool remember, bool keepUsed)
    {
        var revision = Document.EditRevision; applying = true; var advanced = false;
        var selected = Records.ToArray();
        try
        {
            if (keepUsed) selected = Records.Where(record => !Document.IsEditorRegionUsed(record)).ToArray();
            // Source MessageBoxExtReset stores the default Yes checkbox before
            // running the deletion. A failed command does not undo that choice.
            Policy.Remember(this, remember);
            if (Stage == EditorRegionRemovalStage.Confirmation && UsedCount != 0)
            { Stage = EditorRegionRemovalStage.UsedAreas; advanced = true; return 0; }
            Document.ApplyConfirmedRegionRemoval(this, selected);
            IsAccepted = true; RemovedCount = selected.Length; return RemovedCount;
        }
        catch
        {
            IsAccepted = Document.EditRevision != revision;
            if (IsAccepted) RemovedCount = selected.Length;
            throw;
        }
        finally { applying = false; if (!advanced) Policy.Release(this); }
    }
    public void Dispose() { if (!applying) Policy.Release(this); }
}

/// <summary>Runtime REQUEST_RECTEXKILL, shared by areas, gates and effects.
/// It is independent of document history and preserves unrelated request bits.</summary>
public sealed class EditorRegionRemovalPolicy : IDisposable
{
    public const uint RemovalBit = 0x00000010;
    public const uint UsedAreasBit = 0x00000100;
    private readonly EditorRequestPreferences preferences;
    private bool disposed;
    private EditorRegionRemovalRequest? active;
    public EditorRegionRemovalPolicy(uint initialFlags = 0) : this(new EditorRequestPreferences(initialFlags)) { }
    public EditorRegionRemovalPolicy(EditorRequestPreferences preferences) { this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences)); }
    public uint Flags => preferences.Flags;
    public bool Suppressed => (Flags & RemovalBit) != 0;
    public bool UsedAreasSuppressed => (Flags & UsedAreasBit) != 0;
    public EditorRegionRemovalRequest Begin(NativeMapDocument document, IReadOnlyList<NativeRecord> records)
    {
        ObjectDisposedException.ThrowIf(disposed, this); ArgumentNullException.ThrowIfNull(document); ArgumentNullException.ThrowIfNull(records);
        if (active is not null) throw new InvalidOperationException("Finish the current region-removal request.");
        if (document.IsSaveLocked) throw new InvalidOperationException("Finish the current edit before requesting region removal.");
        var fingerprint=document.RegionRemovalFingerprint();var revision=document.EditRevision;
        var current = document.EditorRegionRemovalRecords().ToDictionary(record => (record.File, record.Offset, record.Length, record.Category));
        var snapshot = Array.AsReadOnly(records.Select(record => record is not null && current.TryGetValue((record.File, record.Offset, record.Length, record.Category), out var canonical)
            ? canonical with { Fields = Array.AsReadOnly(canonical.Fields.ToArray()) }
            : throw new ArgumentException("Select current verified map-region records.", nameof(records))).ToArray());
        var kind = document.ValidateRegionRemovalSelection(snapshot);
        if(document.EditRevision!=revision || !fingerprint.AsSpan().SequenceEqual(document.RegionRemovalFingerprint()))throw new InvalidOperationException("The region selection changed while preparing confirmation.");
        var request = new EditorRegionRemovalRequest(this, document, snapshot, kind, fingerprint, revision);
        preferences.Acquire(request); active = request;
        return active;
    }
    internal bool IsCurrent(EditorRegionRemovalRequest request)
    {
        if (disposed || request.IsClosed || !ReferenceEquals(active, request) || !ReferenceEquals(request.Policy, this)) return false;
        try { return request.Document.IsRegionRemovalCurrent(request); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or FormatException or OverflowException) { return false; }
    }
    internal void Remember(EditorRegionRemovalRequest request, bool remember)
    {
        var bit = request.Stage == EditorRegionRemovalStage.Confirmation ? RemovalBit : UsedAreasBit;
        if (request.RequiresPrompt) preferences.Remember(request, bit, remember);
    }
    internal void Release(EditorRegionRemovalRequest request)
    {
        if (!ReferenceEquals(request.Policy, this)) return;
        request.IsClosed = true; preferences.Release(request); if (ReferenceEquals(active, request)) active = null;
    }
    public void ResetSuppression()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (active is { IsApplying: true }) throw new InvalidOperationException("Finish the removal command before resetting confirmation.");
        active?.Dispose(); preferences.Reset(RemovalBit | UsedAreasBit);
    }
    public void Dispose() { if (disposed) return; if (active is { } request) Release(request); disposed = true; }
}

public sealed partial class NativeMapDocument
{
    public bool IsEditorRegionRemovalRecord(NativeRecord record)
    {
        return EditorRegionRemovalRecords().Any(current => SameRecord(current, record) && current.Category == record.Category);
    }
    public IReadOnlyList<NativeRecord> EditorRegionRemovalRecords() => Array.AsReadOnly(Records().Where(IsVerifiedRegionRemovalShape).ToArray());
    private bool IsVerifiedRegionRemovalShape(NativeRecord record)
    {
        if (IsLegacyProject) return record.Category == "Project.MAPRECTEX" && record.File.UInt32(4) == 1 && MapRegionKindOf(record) is not null;
        return record.File.UInt32(4) == 4 && (record.Category switch
        {
            "GAMEAREA" => record.Length == 64 && record.File.UInt32(record.Offset + 4) == 56,
            "GAMEGATE" => NativeSourceGate(record),
            "GAMEEFFECT" => record.Length == 44 && record.File.UInt32(record.Offset + 4) == 36,
            _ => false
        });
    }
    internal MapRegionKind ValidateRegionRemovalSelection(IReadOnlyList<NativeRecord> records)
    {
        if (IsSaveLocked) throw new InvalidOperationException("Finish the current edit before requesting region removal.");
        var current = EditorRegionRemovalRecords().Select(record => (record.File, record.Offset, record.Length, record.Category)).ToHashSet();
        if (records.Count == 0 || records.DistinctBy(record => (record.File, record.Offset)).Count() != records.Count || records.Any(record => !current.Contains((record.File, record.Offset, record.Length, record.Category))))
            throw new ArgumentException("Select current verified map-region records once each.", nameof(records));
        var kinds = records.Select(MapRegionKindOf).Distinct().ToArray();
        if (kinds.Length != 1) throw new ArgumentException("Select regions of one kind.", nameof(records));
        return kinds[0]!.Value;
    }
    internal byte[] RegionRemovalFingerprint() => SaveWorkingFingerprint();
    internal bool IsRegionRemovalCurrent(EditorRegionRemovalRequest request)
        => request.Document == this && request.Revision == EditRevision && !IsSaveLocked
            && RegionRemovalFingerprint().AsSpan().SequenceEqual(request.Fingerprint)
            && ValidateRegionRemovalSelection(request.Records) == request.Kind;
    internal bool IsEditorRegionUsed(NativeRecord record)
    {
        if (MapRegionKindOf(record) != MapRegionKind.Area) return false;
        var id = record.Read(record.Fields.Single(field => field.Name == (IsLegacyProject ? "id" : "bAreaID")));
        if (id == "0") return false;
        // Triggers_GetFirstTrigger tests the stored area operand regardless of
        // node.type; an empty point counts. Action-only references do not set Used.
        return IsLegacyProject ? Records().Any(point => point.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)
            && point.Fields.FirstOrDefault(field => field.Name == "area") is { } area && point.Read(area) == id)
            : DecodeNativeAreaScopes(NativeAreaScopes).Any(point => point.ID.ToString() == id);
    }
    internal void ApplyConfirmedRegionRemoval(EditorRegionRemovalRequest request, IReadOnlyList<NativeRecord> selected)
    {
        if (!IsRegionRemovalCurrent(request)) throw new InvalidOperationException("Refresh the region selection before deleting it.");
        RequireKnownEntityReferences(Records());
        EditCore("Delete selected map regions", () =>
        {
            // RectEx_RemoveRects processes display order. Physical offsets move
            // after each removal, but the captured selection remains immutable.
            var removed = new List<NativeRecord>();
            foreach (var original in selected)
            {
                var offset = original.Offset - removed.Where(prior => prior.File == original.File && prior.Offset < original.Offset).Sum(prior => prior.Length);
                var record = EditorRegionRemovalRecords().Single(current => current.File == original.File && current.Offset == offset && current.Length == original.Length && current.Category == original.Category);
                DeleteEditorRegion(record);
                removed.Add(original);
            }
        }, () => new EditorDrawingCompletion(true, 1));
    }

    private void DeleteEditorRegion(NativeRecord record)
    {
        // Source deletion retains every scenario point and entity operand. The
        // explicit command owns this policy; generic DeleteRecord stays guarded.
        if (MapRegionKindOf(record) != MapRegionKind.Area) { DeleteRecordCore(record, editorAreaRemoval: true); return; }
        var idField = IsLegacyProject ? "id" : "bAreaID";
        var nextField = IsLegacyProject ? "nextid" : "bAreaNextID";
        var id = record.Read(record.Fields.Single(field => field.Name == idField));
        var next = record.Read(record.Fields.Single(field => field.Name == nextField));
        DeleteRecordCore(record, editorAreaRemoval: true);
        // The source rewires only the first surviving predecessor, including
        // malformed multiple-predecessor/zero-ID imports. Do not normalize the
        // other links or rewrite scenario/hero references here.
        var predecessor = EditorRegionRemovalRecords().FirstOrDefault(candidate => MapRegionKindOf(candidate) == MapRegionKind.Area
            && candidate.Read(candidate.Fields.Single(field => field.Name == nextField)) == id);
        if (predecessor is not null) SetField(predecessor, predecessor.Fields.Single(field => field.Name == nextField), next);
    }
}
