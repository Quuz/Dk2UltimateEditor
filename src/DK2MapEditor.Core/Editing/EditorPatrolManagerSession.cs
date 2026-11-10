using System.Security.Cryptography;

namespace DK2MapEditor.Core.Editing;

public sealed record EditorPatrolOccurrence(uint ID, PatrolPoint? Point);

public sealed class EditorPatrolManagerRoute
{
    internal EditorPatrolManagerRoute(EditorPatrolManagerSession owner, long version, long revision,
        string fingerprint, long key, IReadOnlyList<EditorPatrolOccurrence> points, bool changed, bool ambiguous, bool canAppend)
    { Owner=owner; Version=version; Revision=revision; Fingerprint=fingerprint; Key=key; Points=points; IsChanged=changed; Ambiguous=ambiguous; CanAppend=canAppend; }
    internal EditorPatrolManagerSession Owner { get; }
    internal long Version { get; }
    internal long Revision { get; }
    internal string Fingerprint { get; }
    public long Key { get; }
    public IReadOnlyList<EditorPatrolOccurrence> Points { get; }
    public bool IsChanged { get; }
    public bool Ambiguous { get; }
    public bool CanAppend { get; }
    public bool Closed => Points.Count>1&&Points.Take(Points.Count-1).Any(point=>point.ID==Points[^1].ID);
}

/// <summary>Source manager's private occurrence lists. Commands commit live
/// map fields, not a document draft; disposing never rolls accepted edits back.</summary>
public sealed partial class EditorPatrolManagerSession : IDisposable
{
    private sealed record Route(long Key,uint[] IDs);
    private readonly NativeMapDocument document;
    private List<Route> routes=[];
    private (NativeFile File,int Offset,int Length,uint ID)[] structure=[];
    private long version,nextKey;
    private bool disposed,editing,needsReload;
    public EditorPatrolManagerSession(NativeMapDocument document)
    {
        this.document=document??throw new ArgumentNullException(nameof(document));
        Reload(); document.Changed+=DocumentChanged;
    }
    public NativeMapDocument Document => document;
    public bool IsEditing => editing;
    public bool IsClosed => disposed;
    public bool RequiresReload { get { EnsureOpen(); CheckStructure(); return needsReload; } }
    public bool LastRemovalChangedRoutes { get; private set; }
    private void EnsureOpen(){if(disposed)throw new ObjectDisposedException(nameof(EditorPatrolManagerSession));}
    private (NativeFile,int,int,uint)[] Structure() => document.PatrolAreas().Select(point=>(point.Record.File,point.Record.Offset,point.Record.Length,point.ID)).ToArray();
    private void CheckStructure(){if(!structure.SequenceEqual(Structure()))needsReload=true;}
    private void DocumentChanged(object? sender,EventArgs args){if(!editing)CheckStructure();}
    private Dictionary<uint,PatrolPoint[]> Index()=>document.PatrolAreas().GroupBy(point=>point.ID).ToDictionary(group=>group.Key,group=>group.ToArray());
    private static PatrolPoint? Find(Dictionary<uint,PatrolPoint[]> index,uint id)=>index.TryGetValue(id,out var found)&&found.Length==1?found[0]:null;
    private static bool Changed(uint[] ids,Dictionary<uint,PatrolPoint[]> index)
    {
        // Things_Paths.c PathsCheck: skip missing areas; a final point may
        // target any occurrence already in this local list without a warning.
        for(var i=0;i<ids.Length;i++)
        {
            var point=Find(index,ids[i]); if(point is null)continue;
            var next=i+1<ids.Length?ids[i+1]:0;
            if(point.NextID==next)continue;
            return next!=0||!ids.Contains(point.NextID);
        }
        return false;
    }
    private string Fingerprint()
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach(var point in document.PatrolAreas())
        {
            hash.AppendData(BitConverter.GetBytes(point.Record.Offset));
            hash.AppendData(BitConverter.GetBytes(point.Record.Length));
            hash.AppendData(point.Record.File.Data.Span.Slice(point.Record.Offset,point.Record.Length));
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    public void Reload()
    {
        EnsureOpen(); if(editing)throw new InvalidOperationException("Finish the patrol command before reloading groups.");
        var next=document.EditorPatrolRoutes().Select(route=>new Route(++nextKey,route.Points.Select(point=>point.ID)
            .Concat(route.EndID==0?[]:new[]{route.EndID}).ToArray())).ToList();
        routes=next;structure=Structure();needsReload=false;LastRemovalChangedRoutes=false;version++;
    }
    public IReadOnlyList<EditorPatrolManagerRoute> GetRoutes()
    {
        EnsureOpen();CheckStructure();var index=Index();var fingerprint=Fingerprint();
        var hasUnlinked=index.Values.Any(points=>points.Length==1&&points[0].ID is >0 and <=255&&points[0].NextID==0);
        return routes.Select(route=>View(route,index,fingerprint,hasUnlinked)).ToList().AsReadOnly();
    }
    private EditorPatrolManagerRoute View(Route route,Dictionary<uint,PatrolPoint[]> index,string fingerprint,bool hasUnlinked)
    {
        var occurrences=route.IDs.Select(id=>new EditorPatrolOccurrence(id,Find(index,id))).ToList().AsReadOnly();
        var ambiguous=route.IDs.Any(id=>id is 0 or >255||index.TryGetValue(id,out var points)&&points.Length!=1);
        var closed=route.IDs.Length>1&&route.IDs.Take(route.IDs.Length-1).Contains(route.IDs[^1]);
        return new(this,version,document.EditRevision,fingerprint,route.Key,occurrences,Changed(route.IDs,index),ambiguous,
            !needsReload&&!ambiguous&&!closed&&hasUnlinked&&route.IDs.Length<513);
    }
    private Route Guard(EditorPatrolManagerRoute requested)
    {
        ArgumentNullException.ThrowIfNull(requested);
        EnsureOpen();if(editing)throw new InvalidOperationException("Finish the current patrol command.");
        CheckStructure();
        if(needsReload)throw new InvalidOperationException("Area identities or storage changed. Reload the patrol manager.");
        if(!ReferenceEquals(requested.Owner,this)||requested.Version!=version||requested.Revision!=document.EditRevision||requested.Fingerprint!=Fingerprint())
            throw new InvalidOperationException("The patrol view changed. Select a current occurrence before editing.");
        if(requested.Ambiguous)throw new InvalidOperationException("Patrol editing requires unique area identities in 1–255.");
        return routes.SingleOrDefault(route=>route.Key==requested.Key)??throw new InvalidOperationException("The local route was removed.");
    }
    private PatrolPoint Point(NativeRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var areas=document.PatrolAreas();
        var point=areas.SingleOrDefault(point=>ReferenceEquals(point.Record.File,record.File)&&point.Record.Offset==record.Offset&&point.Record.Length==record.Length)
            ??throw new ArgumentException("Select a current area record.",nameof(record));
        if(point.ID is 0 or >255||areas.Count(item=>item.ID==point.ID)!=1)throw new InvalidOperationException("Select an unambiguous area identity in 1–255.");
        return point;
    }
    private PatrolPoint Required(uint id)=>Find(Index(),id)??throw new InvalidOperationException("The selected occurrence has no unique current area.");
    private static void Wait(int value){if(value is <0 or >250)throw new ArgumentOutOfRangeException(nameof(value),"Original patrol pauses are 0–250.");}
    private void Ready()
    {
        EnsureOpen();CheckStructure();if(editing||needsReload)throw new InvalidOperationException("Finish the command or reload the patrol manager.");
    }
    private void Commit(string description,List<(NativeRecord Record,uint? Next,int? Wait)> writes,Action accepted)
    {
        var revision=document.EditRevision;var synchronized=false;
        editing=true;
        try{document.ApplyPatrolManagerFields(description,writes);if(!disposed){synchronized=true;accepted();version++;}}
        catch
        {
            // NativeMapDocument notifications run after the atomic commit.
            // An observer exception must not leave private lists pretending
            // that a committed command was rolled back.
            if(document.EditRevision!=revision&&!disposed&&!synchronized){accepted();version++;}
            throw;
        }
        finally{editing=false;}
    }
    private EditorPatrolManagerRoute Completed(Route route)
    {
        if(!disposed)return GetRoutes().Single(item=>item.Key==route.Key);
        // Closing in a post-commit observer keeps accepted map edits, but
        // returns no live capability and never resurrects the closed lists.
        return View(route,Index(),Fingerprint(),false);
    }
    public EditorPatrolManagerRoute CreateEmptyRoute()
    {
        Ready();
        if(!document.PatrolAreas().Any(point=>point.NextID==0))throw new InvalidOperationException("A new route needs an available unlinked area.");
        var route=new Route(++nextKey,[]);routes.Add(route);version++;LastRemovalChangedRoutes=false;
        return Completed(route);
    }
    /// <summary>Existing quick-map cursor affordance: UI-only first occurrence,
    /// zero edits. It is distinct from accepted manager Start/AddArea.</summary>
    public EditorPatrolManagerRoute CreateCursorRoute(NativeRecord record)
    {
        Ready();var point=Point(record);if(point.NextID!=0)throw new InvalidOperationException("Choose an unlinked cursor area.");
        var route=new Route(++nextKey,[point.ID]);routes.Add(route);version++;LastRemovalChangedRoutes=false;
        return Completed(route);
    }
    public EditorPatrolManagerRoute Start(NativeRecord record,int wait)
    {
        Ready();Wait(wait);var point=Point(record);if(point.NextID!=0)throw new InvalidOperationException("Start from an unlinked area.");
        var route=new Route(nextKey+1,[point.ID]);
        Commit("Start patrol with first area",[(record,null,wait)],()=>{nextKey++;routes.Add(route);LastRemovalChangedRoutes=false;});
        return Completed(route);
    }
    public EditorPatrolManagerRoute Append(EditorPatrolManagerRoute requested,NativeRecord record,int wait)
    {
        var route=Guard(requested);Wait(wait);var target=Point(record);
        if(!requested.CanAppend)throw new InvalidOperationException("The source manager cannot append to this local route.");
        var writes=new List<(NativeRecord,uint?,int?)>{(record,null,wait)};
        if(route.IDs.Length!=0&&Find(Index(),route.IDs[^1]) is{ } predecessor)writes.Add((predecessor.Record,target.ID,null));
        var replacement=route with{IDs=route.IDs.Concat(new[]{target.ID}).ToArray()};
        Commit("Append patrol point",writes,()=>{routes[routes.IndexOf(route)]=replacement;LastRemovalChangedRoutes=false;});
        return Completed(replacement);
    }
    public EditorPatrolManagerRoute Insert(EditorPatrolManagerRoute requested,int occurrence,NativeRecord record,int wait)
    {
        var route=Guard(requested);Wait(wait);var target=Point(record);
        if(occurrence<0||occurrence>=route.IDs.Length)throw new ArgumentOutOfRangeException(nameof(occurrence));
        if(route.IDs.Length>=513||target.NextID!=0)throw new InvalidOperationException("Insert requires capacity and an unlinked area.");
        Required(route.IDs[occurrence]);
        var writes=new List<(NativeRecord,uint?,int?)>{(record,route.IDs[occurrence],wait)};
        if(occurrence>0)writes.Add((Required(route.IDs[occurrence-1]).Record,target.ID,null));
        var ids=route.IDs.ToList();ids.Insert(occurrence,target.ID);var replacement=route with{IDs=ids.ToArray()};
        Commit("Insert patrol point",writes,()=>{routes[routes.IndexOf(route)]=replacement;LastRemovalChangedRoutes=false;});
        return Completed(replacement);
    }
    public EditorPatrolManagerRoute Remove(EditorPatrolManagerRoute requested,int occurrence)
    {
        var route=Guard(requested);
        if(occurrence<0||occurrence>=route.IDs.Length)throw new ArgumentOutOfRangeException(nameof(occurrence));
        var ids=route.IDs.ToList();var removed=ids[occurrence];ids.RemoveAt(occurrence);
        var writes=new List<(NativeRecord,uint?,int?)>();
        if(route.IDs.Length>=2)
        {
            if(occurrence>0)writes.Add((Required(route.IDs[occurrence-1]).Record,occurrence+1<route.IDs.Length?route.IDs[occurrence+1]:0,null));
            var point=Find(Index(),removed);
            if(point is not null&&(!ids.Contains(removed)||ids.Count==0||ids[^1]==removed))writes.Add((point.Record,0,null));
        }
        var replacement=route with{IDs=ids.ToArray()};
        Commit("Remove patrol point link",writes,()=>routes[routes.IndexOf(route)]=replacement);
        if(!disposed)LastRemovalChangedRoutes=GetRoutes().Any(item=>item.IsChanged);
        return Completed(replacement);
    }
    public void Clear(EditorPatrolManagerRoute requested)
    {
        var route=Guard(requested);var index=Index();
        var writes=route.IDs.Take(Math.Max(0,route.IDs.Length-1)).Select(id=>Find(index,id)).Where(point=>point is not null)
            .Select(point=>(point!.Record,(uint?)0,(int?)null)).ToList();
        Commit("Remove patrol route links",writes,()=>routes.Remove(route));
        if(!disposed)LastRemovalChangedRoutes=GetRoutes().Any(item=>item.IsChanged);
    }
    public EditorPatrolManagerRoute SetPause(EditorPatrolManagerRoute requested,int occurrence,int wait)
    {
        var route=Guard(requested);Wait(wait);
        if(occurrence<0||occurrence>=route.IDs.Length)throw new ArgumentOutOfRangeException(nameof(occurrence));
        var point=Required(route.IDs[occurrence]);
        Commit("Change patrol pause",[(point.Record,null,wait)],()=>LastRemovalChangedRoutes=false);
        return Completed(route);
    }
    public void Dispose(){if(disposed)return;disposed=true;document.Changed-=DocumentChanged;routes.Clear();version++;}
}

public sealed partial class NativeMapDocument
{
    internal void ApplyPatrolManagerFields(string description,IReadOnlyList<(NativeRecord Record,uint? Next,int? Wait)> writes)
        =>EditEditorPatrol(description,()=>
        {
            foreach(var write in writes)
            {
                var point=CurrentPatrolPoint(write.Record);
                if(write.Wait is{ } wait)SetPatrolWait(point,wait);
                if(write.Next is{ } next)SetPatrolNext(point,next);
            }
        });
}
