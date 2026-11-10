namespace DK2MapEditor.Core.Editing;

/// <summary>Original GLOBALCTX2's separately retained Happy/Unhappy/Angry job
/// page buffers. Their warning projection outlives page/creature navigation;
/// it is not a scan of every changed creature in the shared map draft.</summary>
public sealed class CreatureJobsPageContext : IDisposable
{
    private IReadOnlyList<CreatureGlobalsJobWarning> warnings=Array.Empty<CreatureGlobalsJobWarning>();
    public bool Initialized{get;private set;}
    public bool IsClosed{get;private set;}
    public int? Index{get;private set;}
    public long Revision{get;private set;}
    public IReadOnlyList<CreatureGlobalsJobWarning> Warnings=>warnings;
    public void Capture(int index,IEnumerable<CreatureGlobalsJobWarning> currentWarnings)
    {
        ObjectDisposedException.ThrowIf(IsClosed,this);ArgumentNullException.ThrowIfNull(currentWarnings);
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        var snapshot=currentWarnings.ToArray();
        if(snapshot.Any(warning=>warning.Index!=index||warning.Group is not("Happy" or "Unhappy" or "Angry"))
            ||snapshot.Select(warning=>warning.Group).Distinct().Count()!=snapshot.Length)
            throw new ArgumentException("Capture one source job page's probability groups.");
        // The source checks these named groups in fixed order even when the
        // caller's immutable warning list uses a different enumeration order.
        warnings=Array.AsReadOnly(snapshot.OrderBy(warning=>warning.Group switch{"Happy"=>0,"Unhappy"=>1,_=>2}).ToArray());
        Index=index;Initialized=true;Revision++;
    }
    public void Dispose(){if(IsClosed)return;IsClosed=true;Initialized=false;Index=null;warnings=Array.Empty<CreatureGlobalsJobWarning>();Revision++;}
}
