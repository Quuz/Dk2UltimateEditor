namespace DK2MapEditor.Core.Editing;

public enum EditorMapNameReply { Cancel, Copy, Keep }

/// <summary>Source map-properties name read, independent of applying the parent
/// properties. Native Unicode storage remains a modern extension.</summary>
public sealed class EditorMapNamePlan
{
    internal EditorMapNamePlan(NativeMapDocument document,MapGeneralDraft draft,string entered,MapPropertiesSession? session)
    {Document=document;Draft=draft;EnteredName=entered;Session=session;}
    internal NativeMapDocument Document {get;}
    internal MapGeneralDraft Draft {get;}
    internal MapPropertiesSession? Session {get;}
    public string EnteredName {get;}
    public string OriginalName=>Draft.Values["Name"];
    public bool NeedsPrompt=>EnteredName.Length>64;
    public bool IsCurrent=>Document.IsEditorMapNameCurrent(this);
    public string CopyName()
    {
        if(!IsCurrent)throw new InvalidOperationException("Refresh the map name after document changes.");
        var length=Math.Min(63,EnteredName.Length);
        // The original ANSI UI cannot contain surrogate pairs. Keep the native
        // Unicode extension well formed at its fixed UTF-16 field boundary.
        if(length>0&&length<EnteredName.Length&&char.IsHighSurrogate(EnteredName[length-1])&&char.IsLowSurrogate(EnteredName[length]))length--;
        var value=EnteredName[..length];
        var field=Draft.Record!.Fields.Single(item=>item.Name==MapGeneralSettings.StorageField(Document.IsLegacyProject,"Name"));
        _=field.Encode(value);return value;
    }
}

public sealed class EditorMapNameRequest : IDisposable
{
    private readonly Func<bool> current;
    internal EditorMapNameRequest(EditorMapNamePolicy policy,EditorMapNamePlan plan,Func<bool> current)
    {Policy=policy;Plan=plan;this.current=current;}
    internal EditorMapNamePolicy Policy {get;}
    internal bool TargetCurrent=>current()&&Plan.IsCurrent;
    public EditorMapNamePlan Plan {get;}
    public bool RequiresPrompt=>!Policy.Suppressed;
    public bool IsCurrent=>Policy.IsCurrent(this);
    public bool IsClosed {get;internal set;}
    public EditorMapNameReply Reply {get;private set;}
    public string? SelectedName {get;private set;}
    public bool CanKeep(bool remember)=>IsCurrent&&!remember;
    public bool Copy(bool remember=false)
    {
        if(!IsCurrent)throw new InvalidOperationException("The name-overflow request is no longer current.");
        var name=Plan.CopyName();
        Policy.Remember(this,remember);SelectedName=name;Reply=EditorMapNameReply.Copy;Policy.Release(this);return true;
    }
    public bool Keep(bool remember=false)
    {
        if(!CanKeep(remember))return false;
        SelectedName=Plan.OriginalName;Reply=EditorMapNameReply.Keep;Policy.Release(this);return true;
    }
    public void Dispose()=>Policy.Release(this);
}

/// <summary>REQUEST_BUFOVERFLOW0x4; only default Copy can be remembered.
/// Decisions are process preferences, never map bytes or Undo entries.</summary>
public sealed class EditorMapNamePolicy : IDisposable
{
    public const uint WarningBit=4;
    private readonly EditorRequestPreferences preferences;
    private EditorMapNameRequest? active;
    private bool disposed;
    public EditorMapNamePolicy(uint initialFlags=0):this(new EditorRequestPreferences(initialFlags)) { }
    public EditorMapNamePolicy(EditorRequestPreferences preferences)=>this.preferences=preferences??throw new ArgumentNullException(nameof(preferences));
    public uint Flags=>preferences.Flags;
    public bool Suppressed=>(Flags&WarningBit)!=0;
    public EditorMapNameRequest? Begin(EditorMapNamePlan plan,Func<bool> callerCurrent)
    {
        ObjectDisposedException.ThrowIf(disposed,this);ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(callerCurrent);
        if(!plan.IsCurrent)throw new InvalidOperationException("Refresh the map name before requesting a reply.");
        if(!plan.NeedsPrompt)return null;
        if(active is not null)throw new InvalidOperationException("Finish the current name-overflow request.");
        var request=new EditorMapNameRequest(this,plan,callerCurrent);preferences.Acquire(request);active=request;
        if(!IsCurrent(request)){Release(request);throw new InvalidOperationException("The map-name caller changed during preparation.");}
        return request;
    }
    private bool Owns(EditorMapNameRequest request)=>!disposed&&!request.IsClosed&&ReferenceEquals(request.Policy,this)&&ReferenceEquals(active,request);
    internal bool IsCurrent(EditorMapNameRequest request)
    {
        if(!Owns(request))return false;
        try{return request.TargetCurrent&&Owns(request);}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or FormatException or OverflowException){return false;}
    }
    internal void Remember(EditorMapNameRequest request,bool remember)
    {if(request.RequiresPrompt)preferences.Remember(request,WarningBit,remember);}
    internal void Release(EditorMapNameRequest request)
    {if(!ReferenceEquals(request.Policy,this))return;request.IsClosed=true;preferences.Release(request);if(ReferenceEquals(active,request))active=null;}
    public void ResetSuppression(){ObjectDisposedException.ThrowIf(disposed,this);active?.Dispose();preferences.Reset(WarningBit);}
    public void Dispose(){if(disposed)return;active?.Dispose();disposed=true;}
}

public sealed partial class NativeMapDocument
{
    public EditorMapNamePlan PrepareEditorMapName(MapGeneralDraft draft,string enteredName,MapPropertiesSession? session=null)
    {
        ArgumentNullException.ThrowIfNull(enteredName);
        if(IsSaveLocked)throw new InvalidOperationException("Finish the current edit before preparing the map name.");
        _=VerifyMapGeneralDraft(draft);
        if(!draft.Values.ContainsKey("Name"))throw new InvalidOperationException("A verified map-name field is required.");
        var plan=new EditorMapNamePlan(this,draft,enteredName,session);
        if(!IsEditorMapNameCurrent(plan))throw new InvalidOperationException("A current matching map-properties session is required.");
        return plan;
    }
    internal bool IsEditorMapNameCurrent(EditorMapNamePlan plan)
    {
        if(!ReferenceEquals(plan.Document,this)||IsSaveLocked)return false;
        if(plan.Session is { } session&&(!ReferenceEquals(session.WorkingDocument,this)||session.Owner.IsSaveLocked||!session.Owner.IsMapPropertiesSessionCurrent(session)))return false;
        try{_=VerifyMapGeneralDraft(plan.Draft);return true;}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or OverflowException){return false;}
    }
}
