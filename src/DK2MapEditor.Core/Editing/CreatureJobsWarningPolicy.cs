namespace DK2MapEditor.Core.Editing;

/// <summary>Source REQUEST_GLOBALJOB: first invalid Happy/Unhappy/Angry total,
/// signed LONG arithmetic, Continue/Cancel and process-session auto reply.</summary>
public sealed class CreatureJobsWarningPolicy : IDisposable
{
    public const uint WarningBit=0x00010000;
    private uint flags;
    private long revision;
    private bool disposed;
    private CreatureJobsWarningRequest? active;
    public CreatureJobsWarningPolicy(uint initialFlags=0){flags=initialFlags;}
    public uint Flags=>flags;
    public bool Suppressed=>(flags&WarningBit)!=0;
    public static CreatureGlobalsJobWarning? FirstWarning(IEnumerable<CreatureGlobalsJobWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);
        foreach(var warning in warnings)
        {
            if(warning.Index<0||warning.Group is not ("Happy" or "Unhappy" or "Angry"))
                throw new ArgumentException("Choose a source creature probability group.");
            var total=unchecked((int)warning.Total);
            if(total is not (0 or 100))return warning with{Total=total};
        }
        return null;
    }
    public CreatureJobsWarningRequest? Begin(IEnumerable<CreatureGlobalsJobWarning> warnings,Func<bool> ownerCurrent)
    {
        ObjectDisposedException.ThrowIf(disposed,this);ArgumentNullException.ThrowIfNull(ownerCurrent);
        if(active is not null)throw new InvalidOperationException("Finish the current creature jobs request.");
        if(!ownerCurrent())throw new InvalidOperationException("The creature jobs owner is no longer current.");
        var warning=FirstWarning(warnings);if(warning is null)return null;
        active=new(this,warning,ownerCurrent,revision,!Suppressed);return active;
    }
    internal bool IsCurrent(CreatureJobsWarningRequest request)=>!disposed&&!request.IsClosed
        &&ReferenceEquals(active,request)&&request.PolicyRevision==revision&&request.OwnerCurrent();
    internal void Accept(CreatureJobsWarningRequest request,bool remember)
    {
        if(!IsCurrent(request))throw new InvalidOperationException("The creature jobs request is no longer current.");
        if(request.RequiresPrompt)flags=(flags&~WarningBit)|(remember?WarningBit:0);
        Release(request);
    }
    internal void Release(CreatureJobsWarningRequest request)
    {request.IsClosed=true;if(ReferenceEquals(active,request)){active=null;revision++;}}
    public void ResetSuppression()
    {ObjectDisposedException.ThrowIf(disposed,this);if(active is{} request)Release(request);flags&=~WarningBit;revision++;}
    public void Dispose(){if(disposed)return;if(active is{} request)Release(request);disposed=true;revision++;}
}

/// <summary>Read-only consent. It never applies a creature draft or map edit.</summary>
public sealed class CreatureJobsWarningRequest : IDisposable
{
    internal CreatureJobsWarningRequest(CreatureJobsWarningPolicy policy,CreatureGlobalsJobWarning warning,
        Func<bool> ownerCurrent,long revision,bool prompt)
    {Policy=policy;Warning=warning;OwnerCurrent=ownerCurrent;PolicyRevision=revision;RequiresPrompt=prompt;}
    internal CreatureJobsWarningPolicy Policy{get;}
    internal Func<bool> OwnerCurrent{get;}
    internal long PolicyRevision{get;}
    public CreatureGlobalsJobWarning Warning{get;}
    public bool RequiresPrompt{get;}
    public bool IsClosed{get;internal set;}
    public bool IsAccepted{get;private set;}
    public bool IsCurrent=>Policy.IsCurrent(this);
    public bool CanCancel(bool remember)=>IsCurrent&&!remember;
    public bool Cancel(bool remember=false)
    {if(!CanCancel(remember))return false;Dispose();return true;}
    public bool Continue(bool remember=false)
    {Policy.Accept(this,remember);IsAccepted=true;return true;}
    public void Dispose()=>Policy.Release(this);
}
