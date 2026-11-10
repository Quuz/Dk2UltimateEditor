namespace DK2MapEditor.Core.Editing;

internal sealed record ScenarioDeletionSnapshot(NativeMapDocument Document,byte[] Fingerprint,long Revision,
    ScriptEdge? Point,ushort EventID,EditorConfirmationPrompt Prompt,int EventCount);

/// <summary>A frozen source Delete Point/Condition/Action command. Preferences
/// are session state; accepting or cancelling never folds them into map Undo.</summary>
public sealed class EditorScenarioDeletionRequest : IEditorConfirmationRequest
{
    internal EditorScenarioDeletionRequest(EditorScenarioDeletionPolicy policy,ScenarioDeletionSnapshot snapshot)
    { Policy=policy;Snapshot=snapshot; }
    internal EditorScenarioDeletionPolicy Policy { get; }
    internal ScenarioDeletionSnapshot Snapshot { get; }
    internal bool IsApplying { get; private set; }
    public NativeMapDocument Document => Snapshot.Document;
    public ScriptEdge? Point => Snapshot.Point;
    public ushort EventID => Snapshot.EventID;
    public int EventCount => Snapshot.EventCount;
    public EditorConfirmationPrompt Prompt => Snapshot.Prompt;
    public int Count => 1;
    public bool RequiresPrompt => (Policy.Flags & EditorScenarioDeletionPolicy.Bit(Prompt))==0;
    public bool IsCurrent => Policy.IsCurrent(this);
    public bool IsClosed { get; internal set; }
    public bool IsAccepted { get; private set; }
    public bool CanDecline(bool remember) => IsCurrent && !remember;
    public bool Decline(bool remember=false)
    { if(!CanDecline(remember))return false;Dispose();return true; }
    public int Accept(bool remember=false)
    {
        if(!IsCurrent)throw new InvalidOperationException("The scenario deletion request is no longer current.");
        IsApplying=true;var revision=Document.EditRevision;
        try
        {
            Policy.Remember(this,remember);
            Document.ApplyRequestedScenarioDeletion(Snapshot);
            IsAccepted=true;return 1;
        }
        catch { IsAccepted=Document.EditRevision!=revision;throw; }
        finally { IsApplying=false;Policy.Release(this); }
    }
    public void Dispose(){if(!IsApplying)Policy.Release(this);}
}

public sealed class EditorScenarioDeletionPolicy : IDisposable
{
    public const uint ConditionBit=0x00000200,ActionBit=0x00000400,PointBit=0x00000800;
    private readonly EditorRequestPreferences preferences;
    private EditorScenarioDeletionRequest? active;
    private bool disposed;
    public EditorScenarioDeletionPolicy(uint initialFlags=0):this(new EditorRequestPreferences(initialFlags)) { }
    public EditorScenarioDeletionPolicy(EditorRequestPreferences preferences) => this.preferences=preferences??throw new ArgumentNullException(nameof(preferences));
    public uint Flags => preferences.Flags;
    internal static uint Bit(EditorConfirmationPrompt prompt) => prompt switch
    {
        EditorConfirmationPrompt.ScenarioConditionRemoval=>ConditionBit,
        EditorConfirmationPrompt.ScenarioActionRemoval=>ActionBit,
        EditorConfirmationPrompt.ScenarioPointRemoval=>PointBit,
        _=>throw new ArgumentOutOfRangeException(nameof(prompt))
    };
    public EditorScenarioDeletionRequest BeginPoint(NativeMapDocument document,ScriptEdge root)
    { ArgumentNullException.ThrowIfNull(root);return Begin(document,root,0); }
    public EditorScenarioDeletionRequest BeginEvent(NativeMapDocument document,ushort id) => Begin(document,null,id);
    private EditorScenarioDeletionRequest Begin(NativeMapDocument document,ScriptEdge? root,ushort id)
    {
        ObjectDisposedException.ThrowIf(disposed,this);ArgumentNullException.ThrowIfNull(document);
        if(active is not null)throw new InvalidOperationException("Finish the current scenario deletion request.");
        var request=new EditorScenarioDeletionRequest(this,document.PrepareScenarioDeletion(root,id));
        preferences.Acquire(request);active=request;return request;
    }
    internal bool IsCurrent(EditorScenarioDeletionRequest request)
    {
        if(disposed || request.IsClosed || !ReferenceEquals(request.Policy,this) || !ReferenceEquals(active,request))return false;
        try{return request.Document.IsScenarioDeletionCurrent(request.Snapshot);}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or FormatException or OverflowException){return false;}
    }
    internal void Remember(EditorScenarioDeletionRequest request,bool remember)
    { if(request.RequiresPrompt)preferences.Remember(request,Bit(request.Prompt),remember); }
    internal void Release(EditorScenarioDeletionRequest request)
    {
        if(!ReferenceEquals(request.Policy,this))return;
        request.IsClosed=true;preferences.Release(request);if(ReferenceEquals(active,request))active=null;
    }
    public void ResetSuppression()
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(active is {IsApplying:true})throw new InvalidOperationException("Finish scenario deletion before resetting confirmation.");
        active?.Dispose();preferences.Reset(ConditionBit|ActionBit|PointBit);
    }
    public void Dispose()
    {
        if(disposed)return;
        if(active is {IsApplying:true})throw new InvalidOperationException("Finish scenario deletion before disposing its policy.");
        active?.Dispose();disposed=true;
    }
}

public sealed partial class NativeMapDocument
{
    internal ScenarioDeletionSnapshot PrepareScenarioDeletion(ScriptEdge? requested,ushort id)
    {
        if(IsSaveLocked)throw new InvalidOperationException("Finish the current edit before requesting scenario deletion.");
        RequireEditorPointOperation();
        var fingerprint=RegionRemovalFingerprint();var revision=EditRevision;
        ScriptEdge? point=null;int count;EditorConfirmationPrompt prompt;
        if(requested is not null)
        {
            var validated=ValidateEditorScenarioPointDeletion(requested);
            point=validated.Root with {Owner=validated.Root.Owner with {Fields=Array.AsReadOnly(validated.Root.Owner.Fields.ToArray())}};
            count=validated.Remove.Count;prompt=EditorConfirmationPrompt.ScenarioPointRemoval;
        }
        else
        {
            var validated=ValidateScriptBranchDeletion(id);count=validated.Branch.Count;
            prompt=validated.Graph.Events[id].Edges.Any(edge=>edge.Field.Name is "ChildAnd" or "and")
                ?EditorConfirmationPrompt.ScenarioConditionRemoval:EditorConfirmationPrompt.ScenarioActionRemoval;
        }
        if(EditRevision!=revision || !fingerprint.AsSpan().SequenceEqual(RegionRemovalFingerprint()))throw new InvalidOperationException("The scenario changed while preparing deletion.");
        return new(this,fingerprint,revision,point,id,prompt,count);
    }
    internal bool IsScenarioDeletionCurrent(ScenarioDeletionSnapshot snapshot) => ReferenceEquals(this,snapshot.Document)
        && !IsSaveLocked && EditRevision==snapshot.Revision && snapshot.Fingerprint.AsSpan().SequenceEqual(RegionRemovalFingerprint());
    internal void ApplyRequestedScenarioDeletion(ScenarioDeletionSnapshot snapshot)
    {
        if(!IsScenarioDeletionCurrent(snapshot))throw new InvalidOperationException("Refresh the selected scenario before deleting it.");
        if(snapshot.Point is { } root)DeleteEditorScenarioPoint(root);else DeleteScriptBranch(snapshot.EventID);
    }
}
