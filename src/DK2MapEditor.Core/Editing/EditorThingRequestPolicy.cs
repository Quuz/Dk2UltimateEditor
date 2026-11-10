namespace DK2MapEditor.Core.Editing;

public enum EditorThingCommand { Remove, Modify, RemoveAtCursor }

/// <summary>Original manager confirmation and Triggers_IsUsed(CREATURE|KILL/MODIFY2).
/// A No reply filters every assigned unique ID, as the source caller does.</summary>
public sealed class EditorThingRequest : IEditorConfirmationRequest
{
    private bool applying;
    internal EditorThingRequest(EditorThingRequestPolicy policy, NativeMapDocument document, IReadOnlyList<NativeRecord> records,
        EditorThingCommand command, byte[] fingerprint, long revision,UnderCursorRemovalPlan? cursorPlan=null)
    { Policy=policy;Document=document;Records=records;Command=command;Fingerprint=fingerprint;Revision=revision;CursorPlan=cursorPlan;Prompt=command==EditorThingCommand.Remove?EditorConfirmationPrompt.ObjectRemoval:command==EditorThingCommand.RemoveAtCursor?EditorConfirmationPrompt.UsedCursorRemoval:EditorConfirmationPrompt.UsedThingModification; }
    internal UnderCursorRemovalPlan? CursorPlan {get;}
    internal EditorThingRequestPolicy Policy { get; }
    internal byte[] Fingerprint { get; }
    internal long Revision { get; }
    public NativeMapDocument Document { get; }
    public IReadOnlyList<NativeRecord> Records { get; }
    public EditorThingCommand Command { get; }
    public EditorConfirmationPrompt Prompt { get; private set; }
    public int Count => Records.Count;
    public int UsedCount => CursorPlan is {Family:not UnderCursorFamily.Creatures}?0:Document.EditorThingRequestUsage(Records).Count(entry=>entry.Used);
    public bool RequiresPrompt => Prompt==EditorConfirmationPrompt.ObjectRemoval ? !Policy.RemovalSuppressed : UsedCount!=0 && !Policy.UsedSuppressed;
    public bool IsCurrent => !applying && Policy.IsCurrent(this);
    public bool IsApplying => applying;
    public bool IsClosed { get; internal set; }
    public bool IsAccepted { get; private set; }
    public int RemovedCount { get; private set; }
    public IReadOnlyList<NativeRecord> ResultSelection { get; private set; } = [];
    public bool CanDecline(bool remember) => IsCurrent && !remember;
    public int Accept(bool remember=false)
    { if(!IsCurrent)throw new InvalidOperationException("The object request is no longer current.");return Reply(remember,false); }
    public bool Decline(bool remember=false)
    {
        if(!CanDecline(remember))return false;
        if(Prompt==EditorConfirmationPrompt.ObjectRemoval){Dispose();return true;}
        Reply(false,true);return true;
    }
    private int Reply(bool remember,bool keepAssigned)
    {
        applying=true;var advanced=false;var revision=Document.EditRevision;var selected=Records;
        try
        {
            Policy.Remember(this,remember);
            if(Prompt==EditorConfirmationPrompt.ObjectRemoval && UsedCount!=0)
            {Prompt=EditorConfirmationPrompt.UsedThingRemoval;advanced=true;return 0;}
            if(keepAssigned)selected=Array.AsReadOnly(Document.EditorThingRequestUsage(Records).Where(entry=>entry.Identity==0).Select(entry=>entry.Record).ToArray());
            if(Command==EditorThingCommand.Remove)RemovedCount=Document.ApplyRequestedObjectRemoval(this,selected);
            else if(Command==EditorThingCommand.RemoveAtCursor)RemovedCount=Document.ApplyUnderCursorRemoval(this,selected);
            ResultSelection=selected;IsAccepted=true;return RemovedCount;
        }
        catch {IsAccepted=Document.EditRevision!=revision;if(IsAccepted){ResultSelection=selected;RemovedCount=selected.Count;}throw;}
        finally {applying=false;if(!advanced)Policy.Release(this);}
    }
    public void Dispose(){if(!applying)Policy.Release(this);}
}

public sealed class EditorThingRequestPolicy : IDisposable
{
    public const uint RemovalBit=0x00000002;
    public const uint UsedBit=0x00000100;
    private readonly EditorRequestPreferences preferences;
    private EditorThingRequest? active;
    private bool disposed;
    public EditorThingRequestPolicy(uint initialFlags=0):this(new EditorRequestPreferences(initialFlags)) { }
    public EditorThingRequestPolicy(EditorRequestPreferences preferences){this.preferences=preferences??throw new ArgumentNullException(nameof(preferences));}
    public uint Flags=>preferences.Flags;
    public bool RemovalSuppressed=>(Flags&RemovalBit)!=0;
    public bool UsedSuppressed=>(Flags&UsedBit)!=0;
    public EditorThingRequest Begin(NativeMapDocument document,IReadOnlyList<NativeRecord> records,EditorThingCommand command)
    {
        ObjectDisposedException.ThrowIf(disposed,this);ArgumentNullException.ThrowIfNull(document);ArgumentNullException.ThrowIfNull(records);
        if(active is not null || !Enum.IsDefined(command) || command==EditorThingCommand.RemoveAtCursor)throw new InvalidOperationException("Finish the current object request first; cursor removal requires its own preview.");
        if(document.IsSaveLocked)throw new InvalidOperationException("Finish the current edit before requesting object changes.");
        var fingerprint=document.RegionRemovalFingerprint();var revision=document.EditRevision;
        var snapshot=document.SnapshotThingRequestSelection(records);
        if(document.EditRevision!=revision || !fingerprint.AsSpan().SequenceEqual(document.RegionRemovalFingerprint()))throw new InvalidOperationException("The object selection changed while preparing confirmation.");
        var request=new EditorThingRequest(this,document,snapshot,command,fingerprint,revision);
        preferences.Acquire(request);active=request;return request;
    }
    public EditorThingRequest? BeginUnderCursor(UnderCursorRemovalPlan plan)
    {
        ObjectDisposedException.ThrowIf(disposed,this);ArgumentNullException.ThrowIfNull(plan);
        if(active is not null || !plan.IsCurrent)throw new InvalidOperationException("Refresh the current cursor-removal preview.");
        if(plan.Records.Count==0)return null;
        var request=new EditorThingRequest(this,plan.Owner,plan.Records,EditorThingCommand.RemoveAtCursor,plan.Fingerprint,plan.Revision,plan);
        preferences.Acquire(request);active=request;return request;
    }
    internal bool IsCurrent(EditorThingRequest request)
    {
        if(disposed || request.IsClosed || !ReferenceEquals(request.Policy,this) || !ReferenceEquals(active,request))return false;
        try{return request.Document.IsThingRequestCurrent(request);}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException){return false;}
    }
    internal void Remember(EditorThingRequest request,bool remember)
    {if(request.RequiresPrompt)preferences.Remember(request,request.Prompt==EditorConfirmationPrompt.ObjectRemoval?RemovalBit:UsedBit,remember);}
    internal void Release(EditorThingRequest request)
    {if(!ReferenceEquals(request.Policy,this))return;request.IsClosed=true;preferences.Release(request);if(ReferenceEquals(active,request))active=null;}
    public void ResetSuppression()
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(active is {IsApplying:true})throw new InvalidOperationException("Finish the object command before resetting confirmation.");
        active?.Dispose();preferences.Reset(RemovalBit|UsedBit);
    }
    public void Dispose(){if(disposed)return;if(active is { } request)Release(request);disposed=true;}
}

public sealed partial class NativeMapDocument
{
    internal IReadOnlyList<NativeRecord> SnapshotThingRequestSelection(IReadOnlyList<NativeRecord> records)
    {
        if(IsSaveLocked)throw new InvalidOperationException("Finish the current edit before requesting object changes.");
        if(records.Count==0 || records.Any(record=>record is null) || records.DistinctBy(record=>(record.File,record.Offset)).Count()!=records.Count)
            throw new ArgumentException("Select current physical objects once each.");
        var entries=ObjectManagerEntries().ToDictionary(entry=>(entry.Record.File,entry.Record.Offset,entry.Record.Length,entry.Record.Category));
        return Array.AsReadOnly(records.Select(record=>record is not null && entries.TryGetValue((record.File,record.Offset,record.Length,record.Category),out var entry)
            && entry.CanRemove && record.Fields.SequenceEqual(entry.Record.Fields)?entry.Record with{Fields=Array.AsReadOnly(entry.Record.Fields.ToArray())}
            :throw new ArgumentException("Select verified physical object records.")).ToArray());
    }
    internal bool IsThingRequestCurrent(EditorThingRequest request)=>request.Document==this && request.Revision==EditRevision && !IsSaveLocked
        && RegionRemovalFingerprint().AsSpan().SequenceEqual(request.Fingerprint) && SnapshotThingRequestSelection(request.Records).Count==request.Count;
    internal IReadOnlyList<(NativeRecord Record,uint Identity,bool Used)> EditorThingRequestUsage(IReadOnlyList<NativeRecord> records)
    {
        var entries=ObjectManagerEntries().ToDictionary(entry=>(entry.Record.File,entry.Record.Offset));
        var used=IsLegacyProject?Records().Where(point=>point.Category.StartsWith("Project.TRIGGERLINK",StringComparison.Ordinal))
            .Where(point=>point.Fields.Any(field=>field.Name=="creature")).Select(point=>uint.Parse(point.Read(point.Fields.Single(field=>field.Name=="creature")))).ToHashSet()
            :DecodeNativeOwnerScopes(NativeOwnerScopes).Where(row=>row.Present && row.Scope is 2 or 16).Select(row=>row.ID).ToHashSet();
        return records.Select(record=>{var entry=entries[(record.File,record.Offset)];return(record,entry.Identity,entry.Identity!=0 && used.Contains(entry.Identity));}).ToArray();
    }
    internal int ApplyRequestedObjectRemoval(EditorThingRequest request,IReadOnlyList<NativeRecord> selected)
    {
        if(!IsThingRequestCurrent(request))throw new InvalidOperationException("Refresh the selected objects before deleting them.");
        RequireKnownEntityReferences(Records());var removed=0;
        EditCore("Batch remove objects",()=>{if(selected.Count!=0)removed=ApplyObjectRemovalCore(PlanObjectRemoval(selected),true,editorCaller:true);},()=>new EditorDrawingCompletion(true,checked((uint)removed)));
        return removed;
    }
}
