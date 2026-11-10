namespace DK2MapEditor.Core.Editing;

public enum EditorPatrolWarningKind { Selection, AfterRemoval }

public sealed class EditorPatrolWarningRequest : IDisposable
{
    internal EditorPatrolWarningRequest(EditorPatrolWarningPolicy policy,EditorPatrolManagerSession manager,
        EditorPatrolManagerRoute route,EditorPatrolWarningKind kind,long revision)
    {Policy=policy;Manager=manager;Route=route;Kind=kind;Revision=revision;}
    internal EditorPatrolWarningPolicy Policy {get;}
    internal EditorPatrolManagerSession Manager {get;}
    internal EditorPatrolManagerRoute Route {get;}
    internal long Revision {get;}
    public EditorPatrolWarningKind Kind {get;}
    public long RouteKey=>Route.Key;
    public bool IsClosed {get;internal set;}
    public bool IsCurrent=>Policy.IsCurrent(this);
    /// <summary>Source default gadget1 acknowledges the warning and remembers
    /// checkbox1200. Window close0 releases it without updating request flags.</summary>
    public bool Respond(bool acknowledge,bool suppress)=>Policy.Respond(this,acknowledge,suppress);
    public void Dispose()=>Policy.Release(this);
}

/// <summary>Source Config.RequestsFlags subset for PathsCheck. Runtime state
/// belongs to the editor session, never to map files, counters or Undo.</summary>
public sealed class EditorPatrolWarningPolicy : IDisposable
{
    public const uint ChangedRouteBit=0x00002000;
    private uint flags;
    private long revision;
    private bool disposed;
    private EditorPatrolWarningRequest? active;
    public EditorPatrolWarningPolicy(uint initialFlags=0){flags=initialFlags;}
    public uint Flags=>flags;
    public bool Suppressed=>(flags&ChangedRouteBit)!=0;
    private void Ready()
    {
        if(disposed)throw new ObjectDisposedException(nameof(EditorPatrolWarningPolicy));
        if(active is not null)throw new InvalidOperationException("Finish the current patrol warning request.");
    }
    public EditorPatrolWarningRequest? BeginSelection(EditorPatrolManagerSession manager,EditorPatrolManagerRoute? route)
    {
        ArgumentNullException.ThrowIfNull(manager);Ready();
        if(route is null)return null;
        manager.GuardWarning(route);
        return Begin(manager,route,EditorPatrolWarningKind.Selection);
    }
    public EditorPatrolWarningRequest? BeginAfterRemoval(EditorPatrolManagerSession manager)
    {
        ArgumentNullException.ThrowIfNull(manager);Ready();
        var route=manager.GetRoutes().FirstOrDefault(route=>route.IsChanged);
        if(route is null)return null;
        manager.GuardWarning(route);
        // Source All checks the first changed root, including the selected
        // root; "other" in the message is not an exclusion predicate.
        return Begin(manager,route,EditorPatrolWarningKind.AfterRemoval);
    }
    private EditorPatrolWarningRequest? Begin(EditorPatrolManagerSession manager,EditorPatrolManagerRoute route,EditorPatrolWarningKind kind)
    {
        if(!route.IsChanged||Suppressed)return null;
        active=new(this,manager,route,kind,revision);return active;
    }
    internal bool IsCurrent(EditorPatrolWarningRequest request)
        =>!disposed&&!request.IsClosed&&ReferenceEquals(active,request)&&ReferenceEquals(request.Policy,this)
            &&request.Revision==revision&&request.Manager.IsWarningCurrent(request.Route);
    internal bool Respond(EditorPatrolWarningRequest request,bool acknowledge,bool suppress)
    {
        if(!IsCurrent(request))return false;
        if(acknowledge){flags=(flags&~ChangedRouteBit)|(suppress?ChangedRouteBit:0);revision++;}
        Release(request);return true;
    }
    internal void Release(EditorPatrolWarningRequest request)
    {if(!ReferenceEquals(request.Policy,this))return;request.IsClosed=true;if(ReferenceEquals(active,request))active=null;}
    public void ResetSuppression()
    {
        if(disposed)throw new ObjectDisposedException(nameof(EditorPatrolWarningPolicy));
        active?.Dispose();flags&=~ChangedRouteBit;revision++;
    }
    public void Dispose(){if(disposed)return;active?.Dispose();disposed=true;revision++;}

    /// <summary>Follow-mouse top-left in device pixels, clamped to the selected
    /// monitor work area. Native metrics remain a separate physical DPI gate.</summary>
    public static (int X,int Y) PlaceAtCursor(int cursorX,int cursorY,int width,int height,
        int workLeft,int workTop,int workWidth,int workHeight)
    {
        if(width<=0||height<=0||workWidth<=0||workHeight<=0)throw new ArgumentOutOfRangeException(nameof(width));
        var maxX=(long)workLeft+Math.Max(0,workWidth-width);var maxY=(long)workTop+Math.Max(0,workHeight-height);
        return(checked((int)Math.Clamp((long)cursorX,workLeft,maxX)),checked((int)Math.Clamp((long)cursorY,workTop,maxY)));
    }
}

public sealed partial class EditorPatrolManagerSession
{
    internal void GuardWarning(EditorPatrolManagerRoute route)=>Guard(route);
    internal bool IsWarningCurrent(EditorPatrolManagerRoute route)
    {
        try{GuardWarning(route);return route.IsChanged;}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or FormatException or OverflowException){return false;}
    }
}
