using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public enum EditorPatrolSelectionOperation { Append, Insert }

public sealed class EditorPatrolAreaCandidate
{
    internal EditorPatrolAreaCandidate(PatrolPoint point,bool selectable)
    {
        Record=point.Record;ID=point.ID;NextID=point.NextID;WaitDelay=point.WaitDelay;
        Bounds=point.Region?.Bounds;Selectable=selectable;
        string Text(string name)=>point.Record.Fields.FirstOrDefault(field=>field.Name==name) is{} field?point.Record.Read(field):"";
        Name=Text("name");Information=Text("info");
    }
    internal NativeRecord Record {get;}
    public uint ID {get;}
    public uint NextID {get;}
    public uint WaitDelay {get;}
    public TileRect? Bounds {get;}
    public bool Selectable {get;}
    public string Name {get;}
    public string Information {get;}
}

/// <summary>Source Append/Insert candidate and pause dialog. Selection, pause
/// edits and Cancel never modify the live map or manager occurrence lists.</summary>
public sealed class EditorPatrolAreaSelectionSession : IDisposable
{
    private readonly EditorPatrolManagerSession manager;
    private readonly EditorPatrolManagerRoute route;
    private readonly int occurrence;
    private bool closed,applying;
    internal EditorPatrolAreaSelectionSession(EditorPatrolManagerSession manager,EditorPatrolManagerRoute route,
        EditorPatrolSelectionOperation operation,int occurrence)
    {
        this.manager=manager;this.route=route;Operation=operation;this.occurrence=occurrence;
        manager.GuardSelection(route,operation,occurrence);
        var areas=manager.Document.PatrolAreas();var counts=areas.GroupBy(point=>point.ID).ToDictionary(group=>group.Key,group=>group.Count());
        Candidates=areas.Where(point=>operation==EditorPatrolSelectionOperation.Append||point.NextID==0)
            .Select(point=>new EditorPatrolAreaCandidate(point,point.ID is >0 and <=255&&counts[point.ID]==1)).ToList().AsReadOnly();
    }
    public EditorPatrolSelectionOperation Operation {get;}
    public IReadOnlyList<EditorPatrolAreaCandidate> Candidates {get;}
    public EditorPatrolAreaCandidate? Selected {get;private set;}
    public int Pause {get;private set;}
    public bool IsClosed=>closed;
    public bool IsCurrent
    {
        get
        {
            if(closed||applying)return false;
            try{manager.GuardSelection(route,Operation,occurrence);return true;}
            catch(Exception error)when(error is ArgumentException or InvalidOperationException){return false;}
        }
    }
    public bool CanAccept=>IsCurrent&&Selected is{Selectable:true};
    private void Guard()
    {
        if(closed)throw new ObjectDisposedException(nameof(EditorPatrolAreaSelectionSession));
        if(applying)throw new InvalidOperationException("Finish the patrol selection acceptance.");
        manager.GuardSelection(route,Operation,occurrence);
    }
    public void Select(EditorPatrolAreaCandidate? candidate)
    {
        Guard();
        if(candidate is not null&&!Candidates.Any(item=>ReferenceEquals(item,candidate)))
            throw new ArgumentException("Choose a candidate owned by this selector.",nameof(candidate));
        Selected=candidate;
        // Native SetDlgItemInt triggers the source range-clamping EN_CHANGE.
        // Deselecting disables acceptance but leaves the last pause text.
        if(candidate is not null)Pause=(int)Math.Min(candidate.WaitDelay,250u);
    }
    public void SetPause(int value){Guard();Pause=Math.Clamp(value,0,250);}
    public void SetPauseText(string value)
    {
        Guard();Pause=int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var number)?Math.Clamp(number,0,250):0;
    }
    public EditorPatrolManagerRoute Accept()
    {
        Guard();
        var candidate=Selected is{Selectable:true} selected?selected:throw new InvalidOperationException("Select an unambiguous area before accepting.");
        var revision=manager.Document.EditRevision;applying=true;
        try
        {
            var result=Operation==EditorPatrolSelectionOperation.Append
                ?manager.Append(route,candidate.Record,Pause):manager.Insert(route,occurrence,candidate.Record,Pause);
            closed=true;return result;
        }
        catch
        {
            // Notification exceptions occur after commit. Never permit a
            // second acceptance of the already committed selection.
            if(manager.Document.EditRevision!=revision||manager.IsClosed)closed=true;
            throw;
        }
        finally{applying=false;}
    }
    public void Dispose(){closed=true;}
}

public sealed record EditorPatrolCommandAvailability(bool New,bool Append,bool Insert,bool RemovePoint,bool Clear,bool Pause);

public sealed partial class EditorPatrolManagerSession
{
    public EditorPatrolAreaSelectionSession SelectAppend(EditorPatrolManagerRoute route)=>new(this,route,EditorPatrolSelectionOperation.Append,-1);
    public EditorPatrolAreaSelectionSession SelectInsert(EditorPatrolManagerRoute route,int occurrence)=>new(this,route,EditorPatrolSelectionOperation.Insert,occurrence);
    internal void GuardSelection(EditorPatrolManagerRoute requested,EditorPatrolSelectionOperation operation,int occurrence)
    {
        var route=Guard(requested);
        if(operation==EditorPatrolSelectionOperation.Append)
        {
            if(!requested.CanAppend)throw new InvalidOperationException("The source manager cannot append to this route.");
        }
        else if(operation==EditorPatrolSelectionOperation.Insert)
        {
            if(route.IDs.Length>=513||occurrence<0||occurrence>=route.IDs.Length)throw new ArgumentOutOfRangeException(nameof(occurrence));
            Required(route.IDs[occurrence]);if(occurrence>0)Required(route.IDs[occurrence-1]);
            if(!HasUnlinked())throw new InvalidOperationException("Insert needs an available unlinked area.");
        }
        else throw new ArgumentOutOfRangeException(nameof(operation));
    }
    private bool HasUnlinked()=>Index().Values.Any(points=>points.Length==1&&points[0].ID is >0 and <=255&&points[0].NextID==0);
    public EditorPatrolCommandAvailability GetAvailability(EditorPatrolManagerRoute? selected,int occurrence)
    {
        bool canNew;
        try{Ready();canNew=HasUnlinked();}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException){return new(false,false,false,false,false,false);}
        if(selected is null)return new(canNew,false,false,false,false,false);
        try
        {
            var route=Guard(selected);var isPoint=occurrence>=0&&occurrence<route.IDs.Length;
            var index=Index();var actual=isPoint&&Find(index,route.IDs[occurrence]) is not null;
            var predecessor=occurrence<=0||isPoint&&Find(index,route.IDs[occurrence-1]) is not null;
            return new(canNew,selected.CanAppend,canNew&&actual&&predecessor&&route.IDs.Length<513,
                isPoint&&(route.IDs.Length==1||predecessor),true,actual);
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException){return new(canNew,false,false,false,false,false);}
    }
}
