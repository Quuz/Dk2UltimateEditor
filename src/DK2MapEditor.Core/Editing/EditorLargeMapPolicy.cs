namespace DK2MapEditor.Core.Editing;

/// <summary>Source REQUEST_BIGMAP approval only. New-map creation has no live
/// document yet; accepting this request never creates or resizes a map.</summary>
public sealed class EditorLargeMapRequest : IDisposable
{
    private readonly Func<bool> current;
    internal EditorLargeMapRequest(EditorLargeMapPolicy policy,int width,int height,Func<bool> current)
    { Policy=policy;Width=width;Height=height;this.current=current; }
    internal EditorLargeMapPolicy Policy { get; }
    internal bool TargetCurrent => current();
    public int Width { get; }
    public int Height { get; }
    public bool RequiresPrompt => !Policy.Suppressed;
    public bool IsCurrent => Policy.IsCurrent(this);
    public bool IsClosed { get; internal set; }
    public bool IsAccepted { get; private set; }
    public bool CanDecline(bool remember) => IsCurrent && !remember;
    public bool Decline(bool remember=false)
    {if(!CanDecline(remember))return false;Dispose();return true;}
    public bool Accept(bool remember=false)
    {
        if(!IsCurrent)throw new InvalidOperationException("The large-map approval is no longer current.");
        Policy.Remember(this,remember);IsAccepted=true;Policy.Release(this);return true;
    }
    public void Dispose()=>Policy.Release(this);
}

/// <summary>One source session bit shared by New Map and map-properties resize,
/// outside document state and Undo. Other confirmation callers share its lease.</summary>
public sealed class EditorLargeMapPolicy : IDisposable
{
    public const uint WarningBit=0x00000020;
    private readonly EditorRequestPreferences preferences;
    private EditorLargeMapRequest? active;
    private bool disposed;
    public EditorLargeMapPolicy(uint initialFlags=0):this(new EditorRequestPreferences(initialFlags)) { }
    public EditorLargeMapPolicy(EditorRequestPreferences preferences)=>this.preferences=preferences??throw new ArgumentNullException(nameof(preferences));
    public uint Flags=>preferences.Flags;
    public bool Suppressed=>(Flags&WarningBit)!=0;

    public EditorLargeMapRequest? BeginNew(NewMapOptions options,Func<bool> ownerCurrent)
    {
        ArgumentNullException.ThrowIfNull(options);ArgumentNullException.ThrowIfNull(ownerCurrent);
        ObjectDisposedException.ThrowIf(disposed,this);
        if(options.Validate()!=NewMapProblem.None)throw new ArgumentException("Choose valid new-map settings before requesting approval.",nameof(options));
        if(!options.NeedsLargeMapWarning)return null;
        return Begin(options.Width,options.Height,ownerCurrent);
    }
    public EditorLargeMapRequest? BeginResize(MapPropertiesPlan plan,Func<bool> ownerCurrent)
    {
        ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(ownerCurrent);
        ObjectDisposedException.ThrowIf(disposed,this);
        var document=plan.Session.Owner;
        if(document.IsSaveLocked || plan.Session.WorkingDocument.IsSaveLocked || !document.IsMapPropertiesPlanCurrent(plan))throw new InvalidOperationException("Refresh the map-properties preview before requesting approval.");
        if(!plan.NeedsLargeMapConsent)return null;
        return Begin(plan.Resize.Width,plan.Resize.Height,()=>ownerCurrent() && !document.IsSaveLocked && !plan.Session.WorkingDocument.IsSaveLocked && document.IsMapPropertiesPlanCurrent(plan));
    }
    private EditorLargeMapRequest Begin(int width,int height,Func<bool> current)
    {
        if(active is not null)throw new InvalidOperationException("Finish the current large-map request.");
        var request=new EditorLargeMapRequest(this,width,height,current);
        preferences.Acquire(request);active=request;
        if(!IsCurrent(request)){Release(request);throw new InvalidOperationException("The caller changed while preparing large-map approval.");}
        return request;
    }
    private bool Owns(EditorLargeMapRequest request)=>!disposed && !request.IsClosed && ReferenceEquals(request.Policy,this) && ReferenceEquals(active,request);
    internal bool IsCurrent(EditorLargeMapRequest request)
    {
        if(!Owns(request))return false;
        try{return request.TargetCurrent && Owns(request);}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or FormatException or OverflowException){return false;}
    }
    internal void Remember(EditorLargeMapRequest request,bool remember)
    {if(request.RequiresPrompt)preferences.Remember(request,WarningBit,remember);}
    internal void Release(EditorLargeMapRequest request)
    {
        if(!ReferenceEquals(request.Policy,this))return;
        request.IsClosed=true;preferences.Release(request);if(ReferenceEquals(active,request))active=null;
    }
    public void ResetSuppression()
    {ObjectDisposedException.ThrowIf(disposed,this);active?.Dispose();preferences.Reset(WarningBit);}
    public void Dispose(){if(disposed)return;active?.Dispose();disposed=true;}
}
