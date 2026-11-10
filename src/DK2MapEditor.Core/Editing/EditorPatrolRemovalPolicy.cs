namespace DK2MapEditor.Core.Editing;

public enum EditorPatrolRemovalKind { Point, Route }

/// <summary>One owned source Yes/No request. Yes accepts exactly one existing
/// manager command; No/close/cancel never mutate the map or request flags.</summary>
public sealed class EditorPatrolRemovalRequest : IDisposable
{
    private bool applying;
    internal EditorPatrolRemovalRequest(EditorPatrolRemovalPolicy policy,EditorPatrolManagerSession manager,
        EditorPatrolManagerRoute route,EditorPatrolRemovalKind kind,int occurrence,long revision,bool prompt)
    {Policy=policy;Manager=manager;Route=route;Kind=kind;Occurrence=occurrence;Revision=revision;RequiresPrompt=prompt;}
    internal EditorPatrolRemovalPolicy Policy {get;}
    internal EditorPatrolManagerSession Manager {get;}
    internal EditorPatrolManagerRoute Route {get;}
    internal long Revision {get;}
    public EditorPatrolRemovalKind Kind {get;}
    public int Occurrence {get;}
    public long RouteKey=>Route.Key;
    public bool RequiresPrompt {get;}
    public bool IsClosed {get;internal set;}
    public bool IsAccepted {get;private set;}
    public bool IsApplying=>applying;
    public bool IsCurrent=>!applying&&Policy.IsCurrent(this);
    public bool CanDecline(bool remember)=>IsCurrent&&!remember;
    public bool Decline(bool remember=false)
    {
        if(!CanDecline(remember))return false;
        Dispose();return true;
    }
    public EditorPatrolManagerRoute? Accept(bool remember=false)
    {
        if(!IsCurrent)throw new InvalidOperationException("The removal request is no longer current.");
        Manager.GuardRemoval(Route,Kind,Occurrence);
        var revision=Manager.Document.EditRevision;
        applying=true;
        try
        {
            // Source Reset remembers checkbox1200 before returning Yes to
            // PathsKill. An already suppressed default reply leaves flags alone.
            Policy.Remember(this,remember);
            EditorPatrolManagerRoute? result=null;
            if(Kind==EditorPatrolRemovalKind.Point)result=Manager.Remove(Route,Occurrence);
            else Manager.Clear(Route);
            IsAccepted=true;return result;
        }
        catch
        {
            // Observers run after commit. A failure/owner close there cannot
            // restore a consumed request or undo an accepted removal.
            IsAccepted=Manager.Document.EditRevision!=revision;
            throw;
        }
        finally{applying=false;Policy.Release(this);}
    }
    public void Dispose(){if(!applying)Policy.Release(this);}
}

/// <summary>REQUEST_RECTEXKILLPATH subset of source runtime RequestsFlags.
/// Kept separate from the changed-route warning and copy-buffer preferences.</summary>
public sealed class EditorPatrolRemovalPolicy : IDisposable
{
    public const uint RemovalBit=0x00001000;
    private uint flags;
    private long revision;
    private bool disposed;
    private EditorPatrolRemovalRequest? active;
    public EditorPatrolRemovalPolicy(uint initialFlags=0){flags=initialFlags;}
    public uint Flags=>flags;
    public bool Suppressed=>(flags&RemovalBit)!=0;
    private void Ready()
    {
        if(disposed)throw new ObjectDisposedException(nameof(EditorPatrolRemovalPolicy));
        if(active is not null)throw new InvalidOperationException("Finish the current removal request.");
    }
    public EditorPatrolRemovalRequest BeginPoint(EditorPatrolManagerSession manager,EditorPatrolManagerRoute route,int occurrence)
        =>Begin(manager,route,EditorPatrolRemovalKind.Point,occurrence);
    public EditorPatrolRemovalRequest BeginRoute(EditorPatrolManagerSession manager,EditorPatrolManagerRoute route)
        =>Begin(manager,route,EditorPatrolRemovalKind.Route,-1);
    private EditorPatrolRemovalRequest Begin(EditorPatrolManagerSession manager,EditorPatrolManagerRoute route,EditorPatrolRemovalKind kind,int occurrence)
    {
        ArgumentNullException.ThrowIfNull(manager);Ready();manager.GuardRemoval(route,kind,occurrence);
        active=new(this,manager,route,kind,occurrence,revision,!Suppressed);return active;
    }
    internal bool IsCurrent(EditorPatrolRemovalRequest request)
    {
        if(disposed||request.IsClosed||!ReferenceEquals(active,request)||!ReferenceEquals(request.Policy,this)||request.Revision!=revision)return false;
        try{request.Manager.GuardRemoval(request.Route,request.Kind,request.Occurrence);return true;}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or FormatException or OverflowException){return false;}
    }
    internal void Remember(EditorPatrolRemovalRequest request,bool remember)
    {
        if(request.RequiresPrompt){flags=(flags&~RemovalBit)|(remember?RemovalBit:0);revision++;}
    }
    internal void Release(EditorPatrolRemovalRequest request)
    {if(!ReferenceEquals(request.Policy,this))return;request.IsClosed=true;if(ReferenceEquals(active,request))active=null;}
    public void ResetSuppression()
    {
        if(disposed)throw new ObjectDisposedException(nameof(EditorPatrolRemovalPolicy));
        if(active is{IsApplying:true})throw new InvalidOperationException("Finish the removal command before resetting confirmation.");
        active?.Dispose();flags&=~RemovalBit;revision++;
    }
    public void Dispose(){if(disposed)return;if(active is{ } request)Release(request);disposed=true;revision++;}
}

public sealed partial class EditorPatrolManagerSession
{
    internal void GuardRemoval(EditorPatrolManagerRoute requested,EditorPatrolRemovalKind kind,int occurrence)
    {
        var route=Guard(requested);
        if(kind==EditorPatrolRemovalKind.Route)
        {if(occurrence!=-1)throw new ArgumentOutOfRangeException(nameof(occurrence));return;}
        if(kind!=EditorPatrolRemovalKind.Point||occurrence<0||occurrence>=route.IDs.Length)throw new ArgumentOutOfRangeException(nameof(occurrence));
        if(route.IDs.Length>=2&&occurrence>0)Required(route.IDs[occurrence-1]);
    }
}
