namespace DK2MapEditor.Core.Editing;

/// <summary>Source count limit before assigning any IDs. Acknowledging this
/// report never authorizes generation or bypasses the ordinary validators.</summary>
public sealed class GateGenerationCapacityException : InvalidOperationException
{
    private readonly Func<bool> current;
    internal GateGenerationCapacityException(int areas,int gates,int neededAreas,int neededGates,Func<bool> current)
        :base("Original gate generation supports at most 255 areas and 255 non-heart gates.")
    {ExistingAreas=areas;ExistingGates=gates;NeededAreas=neededAreas;NeededGates=neededGates;this.current=current;}
    public int ExistingAreas {get;}
    public int ExistingGates {get;}
    public int NeededAreas {get;}
    public int NeededGates {get;}
    public bool AreasExhausted=>(long)ExistingAreas+NeededAreas>255;
    public bool GatesExhausted=>(long)ExistingGates+NeededGates>255;
    internal bool IsCurrent=>current();
}

public sealed class EditorGateGenerationRequest : IDisposable
{
    private readonly Func<bool> current;
    internal EditorGateGenerationRequest(EditorGateGenerationPolicy policy,int gates,int areas,int conditions,int actions,
        GateGenerationCapacityException? capacity,Func<bool> current)
    {Policy=policy;Gates=gates;Areas=areas;Conditions=conditions;Actions=actions;Capacity=capacity;this.current=current;}
    internal EditorGateGenerationPolicy Policy {get;}
    internal bool TargetCurrent=>current();
    public int Gates {get;}
    public int Areas {get;}
    public int Conditions {get;}
    public int Actions {get;}
    public GateGenerationCapacityException? Capacity {get;}
    public bool RequiresPrompt=>(Policy.Flags&(Capacity is null?EditorGateGenerationPolicy.ConfirmationBit:EditorGateGenerationPolicy.CapacityBit))==0;
    public bool IsCurrent=>Policy.IsCurrent(this);
    public bool IsClosed {get;internal set;}
    public bool IsAccepted {get;private set;}
    public bool IsAcknowledged {get;private set;}
    public bool Decline(bool remember=false)
    {if(!IsCurrent||remember)return false;Dispose();return true;}
    public void Confirm(bool remember=false)
    {
        if(Capacity is not null)throw new InvalidOperationException("A capacity notice cannot approve generation.");
        if(!IsCurrent)throw new InvalidOperationException("Refresh the generation preview before confirming.");
        Policy.Remember(this,remember);IsAccepted=true;Policy.Release(this);
    }
    public void Acknowledge(bool remember=false)
    {
        if(Capacity is null)throw new InvalidOperationException("A generation question requires an explicit reply.");
        if(!IsCurrent)throw new InvalidOperationException("The generation capacity notice is stale.");
        Policy.Remember(this,remember);IsAcknowledged=true;Policy.Release(this);
    }
    public void Dispose()=>Policy.Release(this);
}

/// <summary>Two independent source session bits shared by workspace and child
/// generators. Replies only grant consent; document mutation stays in callers.</summary>
public sealed class EditorGateGenerationPolicy : IDisposable
{
    public const uint CapacityBit=0x00004000,ConfirmationBit=0x00008000;
    private readonly EditorRequestPreferences preferences;
    private EditorGateGenerationRequest? active;
    private bool disposed;
    public EditorGateGenerationPolicy(uint initialFlags=0):this(new EditorRequestPreferences(initialFlags)) { }
    public EditorGateGenerationPolicy(EditorRequestPreferences preferences)=>this.preferences=preferences??throw new ArgumentNullException(nameof(preferences));
    public uint Flags=>preferences.Flags;
    public EditorGateGenerationRequest? Begin(HeroGateGenerationPlan plan,Func<bool> ownerCurrent)
    {
        ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(ownerCurrent);
        ObjectDisposedException.ThrowIf(disposed,this);
        return plan.HasWork?Begin(plan.Gates,plan.Areas,plan.Conditions,plan.Actions,null,
            ()=>ownerCurrent()&&plan.Owner.IsGateGenerationApprovalCurrent(plan)):null;
    }
    public EditorGateGenerationRequest? Begin(NativeMapDocument document,RegionGateGenerationPlan plan,Func<bool> ownerCurrent)
    {
        ArgumentNullException.ThrowIfNull(document);ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(ownerCurrent);
        ObjectDisposedException.ThrowIf(disposed,this);
        return plan.HasWork?Begin(plan.Gates,plan.Areas,plan.Conditions,plan.Actions,null,
            ()=>ownerCurrent()&&document.IsRegionGateGenerationCurrent(plan.Child)):null;
    }
    public EditorGateGenerationRequest BeginCapacity(GateGenerationCapacityException error,Func<bool> ownerCurrent)
    {
        ArgumentNullException.ThrowIfNull(error);ArgumentNullException.ThrowIfNull(ownerCurrent);
        return Begin(error.NeededGates,error.NeededAreas,0,0,error,()=>ownerCurrent()&&error.IsCurrent);
    }
    private EditorGateGenerationRequest Begin(int gates,int areas,int conditions,int actions,GateGenerationCapacityException? capacity,Func<bool> current)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(active is not null)throw new InvalidOperationException("Finish the current gate generation request.");
        var request=new EditorGateGenerationRequest(this,gates,areas,conditions,actions,capacity,current);
        preferences.Acquire(request);active=request;
        try
        {
            if(!IsCurrent(request))throw new InvalidOperationException("The generation caller changed while preparing the request.");
            return request;
        }
        catch{Release(request);throw;}
    }
    private bool Owns(EditorGateGenerationRequest request)=>!disposed&&!request.IsClosed&&ReferenceEquals(request.Policy,this)&&ReferenceEquals(active,request);
    internal bool IsCurrent(EditorGateGenerationRequest request)
    {
        if(!Owns(request))return false;
        try{return request.TargetCurrent&&Owns(request);}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or FormatException or OverflowException){return false;}
    }
    internal void Remember(EditorGateGenerationRequest request,bool remember)
    {if(request.RequiresPrompt)preferences.Remember(request,request.Capacity is null?ConfirmationBit:CapacityBit,remember);}
    internal void Release(EditorGateGenerationRequest request)
    {if(ReferenceEquals(active,request)){active=null;preferences.Release(request);}request.IsClosed=true;}
    public void ResetSuppression()
    {ObjectDisposedException.ThrowIf(disposed,this);active?.Dispose();preferences.Reset(CapacityBit|ConfirmationBit);}
    public void Dispose(){if(disposed)return;active?.Dispose();disposed=true;}
}
