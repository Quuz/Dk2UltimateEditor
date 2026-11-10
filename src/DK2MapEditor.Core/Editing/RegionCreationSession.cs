using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record RegionCreationDefaults(RegionPropertiesValues Values,bool AutomaticName);

/// <summary>A prospective region exists only in this independent document.
/// Its existing properties, geometry and generation children share one draft.</summary>
public sealed class RegionCreationSession : IDisposable
{
    internal RegionCreationSession(NativeMapDocument owner,long revision,byte[] fingerprint,
        NativeMapDocument working,Dictionary<NativeFile,NativeFile> files,RegionPropertiesSession properties)
    {Owner=owner;Revision=revision;Fingerprint=fingerprint;WorkingDocument=working;Files=files;Properties=properties;}
    internal NativeMapDocument Owner {get;}
    internal long Revision {get;}
    internal byte[] Fingerprint {get;}
    internal Dictionary<NativeFile,NativeFile> Files {get;}
    internal bool Closed {get;private set;}
    public NativeMapDocument WorkingDocument {get;}
    public RegionPropertiesSession Properties {get;}
    public RegionCreationDefaults ContinuationDefaults
    {
        get
        {
            var values=Properties.Values;var automatic=false;
            if(Properties.HasEditorLabels)
            {
                var record=Properties.Record;var id=uint.Parse(record.Read(record.Fields.Single(f=>f.Name=="id")),CultureInfo.InvariantCulture);
                var prefix=Properties.Kind switch{MapRegionKind.Area=>'Z',MapRegionKind.Gate=>'P',_=>'E'};
                // RectEx_AddRect uses first character + atoi, including a
                // decimal prefix followed by arbitrary text.
                if(values.Name.Length>1&&values.Name[0]==prefix)
                {
                    var text=values.Name.AsSpan(1).TrimStart();var negative=false;
                    if(!text.IsEmpty&&text[0] is '+' or '-'){negative=text[0]=='-';text=text[1..];}
                    uint number=0;var digits=0;
                    while(digits<text.Length&&char.IsAsciiDigit(text[digits])&&number<=255)
                    {number=number*10+(uint)(text[digits]-'0');digits++;}
                    automatic=!negative&&digits>0&&number==id;
                }
            }
            return new(values,automatic);
        }
    }
    public void Dispose(){Closed=true;Properties.Dispose();}
}

public sealed class RegionCreationPlan
{
    internal RegionCreationPlan(RegionCreationSession session,int version,IReadOnlyDictionary<NativeFile,byte[]> result,
        uint increments,IReadOnlyList<HistoryOperation> children)
    {Session=session;Version=version;Result=result;Increments=increments;Children=children;}
    internal RegionCreationSession Session {get;}
    internal int Version {get;}
    internal IReadOnlyDictionary<NativeFile,byte[]> Result {get;}
    internal uint Increments {get;}
    internal IReadOnlyList<HistoryOperation> Children {get;}
}

public sealed partial class NativeMapDocument
{
    public RegionCreationSession CreateRegionCreationSession(MapRegionKind kind,TileRect bounds,RegionCreationDefaults? defaults=null)
    {
        if(transactionOpen||activePenStroke is not null)throw new InvalidOperationException("Finish the current edit first.");
        ValidateMapRegionBounds(kind,bounds);
        var fingerprint=UnusedScriptFingerprint();var copy=ClonePropertiesDocument();
        var record=copy.Document.AddMapRegion(kind,bounds);
        var properties=copy.Document.CreateRegionPropertiesSession(record);
        var values=defaults is {} retained?retained.Values with{Bounds=bounds}:properties.Values with{AreaFlags=0};
        if(properties.HasEditorLabels&&(defaults is null||defaults.AutomaticName))
        {
            var prefix=kind switch{MapRegionKind.Area=>"Z",MapRegionKind.Gate=>"P",_=>"E"};
            values=values with{Name=prefix+record.Read(record.Fields.Single(f=>f.Name=="id"))};
        }
        copy.Document.UpdateRegionProperties(properties,values);
        return new(this,EditRevision,fingerprint,copy.Document,copy.Files,properties);
    }
    public bool IsRegionCreationSessionCurrent(RegionCreationSession session)=>session is not null&&!session.Closed
        &&ReferenceEquals(session.Owner,this)&&!transactionOpen&&activePenStroke is null&&session.Revision==EditRevision
        &&UnusedScriptFingerprint().AsSpan().SequenceEqual(session.Fingerprint)
        &&session.Files.Count==TransactionFiles().Count()
        &&session.Files.Values.ToHashSet().SetEquals(session.WorkingDocument.TransactionFiles())
        &&session.WorkingDocument.IsRegionPropertiesSessionCurrent(session.Properties);
    private void VerifyRegionCreationSession(RegionCreationSession session)
    {if(!IsRegionCreationSessionCurrent(session))throw new InvalidOperationException("The region creation draft is closed, foreign or stale.");}
    public RegionCreationPlan PrepareRegionCreation(RegionCreationSession session)
    {
        VerifyRegionCreationSession(session);var properties=session.Properties;var working=session.WorkingDocument;
        if(properties.ActivePick is not null||properties.ActiveGeneration is not null)
            throw new InvalidOperationException("Finish the geometry or generation child first.");
        // Always replay the parent through MutateNativeGate, even without a
        // generation child: moving a prospective native gate updates bindings.
        var payload=working.RegionPropertyPayload(properties,properties.Values);
        if(properties.Record.Category=="GAMEGATE")
        {
            var bounds=properties.Values.Bounds;var (type,orientation)=working.HeroGateTerrainInfo(bounds.X,bounds.Y);
            foreach(var (name,value) in new[]{("bGateType",type),("bGateOrientation",orientation)})
            {var field=properties.Record.Fields.Single(f=>f.Name==name);field.Encode(value.ToString(CultureInfo.InvariantCulture)).CopyTo(payload,field.Offset);}
        }
        var replay=working.ReplayRegionGeneration(properties,payload);
        var result=session.Files.ToDictionary(row=>row.Key,row=>replay.Files[row.Value].Data.ToArray());
        return new(session,properties.Version,result,checked(1+replay.Document.EditorChangeCount),replay.Document.GetHistory().Undo);
    }
    public NativeRecord ApplyEditorRegionCreation(RegionCreationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);var session=plan.Session;VerifyRegionCreationSession(session);
        if(plan.Version!=session.Properties.Version||session.Properties.ActivePick is not null||session.Properties.ActiveGeneration is not null)
            throw new InvalidOperationException("Region creation options changed after preflight or a child is pending.");
        var draft=session.Properties.Record;var file=session.Files.Single(row=>row.Value==draft.File).Key;
        var revision=EditRevision;
        try
        {
            EditCore("Add map region",()=>
            {
                foreach(var row in plan.Result)row.Key.Bytes=row.Value.ToArray();
                if(historyDepth!=0)operationChildren.Peek().AddRange(plan.Children);
            },()=>new(true,plan.Increments));
        }
        finally
        {
            // A post-commit observer exception must never leave a reusable Add.
            // Allocation failure before publication leaves the draft retryable.
            if(EditRevision!=revision)session.Dispose();
        }
        return Records().Single(record=>record.File==file&&record.Offset==draft.Offset&&record.Length==draft.Length&&record.Category==draft.Category);
    }
}
