using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record RegionPropertiesValues(TileRect Bounds,string Name,string Information,uint Colour,uint TextColour,
    uint AreaFlags,int Effect1,int Effect2,int Effect3,int Effect4,int Frequency);

/// <summary>Independent semantic values. Child options update this context;
/// Dispose/Cancel never changes the document.</summary>
public sealed class RegionPropertiesSession : IDisposable
{
    internal RegionPropertiesSession(NativeMapDocument owner,NativeRecord record,MapRegionKind kind,long revision,byte[] fingerprint,byte[] payload,RegionPropertiesValues values)
    {Owner=owner;Record=record;Kind=kind;Revision=revision;Fingerprint=fingerprint;Payload=payload;Baseline=Values=values;}
    internal NativeMapDocument Owner {get;}
    internal NativeRecord Record {get;}
    internal long Revision {get;}
    internal byte[] Fingerprint {get;}
    internal byte[] Payload {get;}
    internal int Version {get;private set;}
    internal bool Closed {get;private set;}
    internal RegionPropertiesPick? ActivePick {get;set;}
    internal RegionGateGenerationSession? ActiveGeneration {get;set;}
    internal List<RegionGateGenerationOperation> Generations {get;}=[];
    public MapRegionKind Kind {get;}
    public bool HasEditorLabels=>Record.Category=="Project.MAPRECTEX";
    public RegionPropertiesValues Baseline {get;}
    public RegionPropertiesValues Values {get;private set;}
    internal void Update(RegionPropertiesValues values){Values=values;Version=checked(Version+1);}
    public void Dispose(){Closed=true;if(ActivePick is {} pick){pick.Finished=true;ActivePick=null;}ActiveGeneration?.Dispose();Generations.Clear();}
}

public sealed class RegionPropertiesPick
{
    internal RegionPropertiesPick(RegionPropertiesSession session){Session=session;Version=session.Version;}
    internal RegionPropertiesSession Session {get;}
    internal int Version {get;}
    internal bool Finished {get;set;}
    public bool IsPoint=>Session.Kind==MapRegionKind.Gate;
}

public sealed class RegionPropertiesPlan
{
    internal RegionPropertiesPlan(RegionPropertiesSession session,int version,byte[] payload,
        IReadOnlyDictionary<NativeFile,byte[]>? result=null,uint increments=1,IReadOnlyList<HistoryOperation>? children=null)
    {Session=session;Version=version;Payload=payload;Result=result;Increments=increments;Children=children??[];}
    internal RegionPropertiesSession Session {get;}
    internal int Version {get;}
    internal byte[] Payload {get;}
    internal IReadOnlyDictionary<NativeFile,byte[]>? Result {get;}
    internal uint Increments {get;}
    internal IReadOnlyList<HistoryOperation> Children {get;}
    public bool Changed=>Result is {} result?result.Any(row=>!row.Key.Data.Span.SequenceEqual(row.Value)):!Payload.AsSpan().SequenceEqual(Session.Payload);
}

public sealed partial class NativeMapDocument
{
    private NativeRecord VerifiedRegionPropertyRecord(NativeRecord record)
    {
        var current=Records().SingleOrDefault(item=>item.File==record.File&&item.Offset==record.Offset&&item.Length==record.Length&&item.Category==record.Category)
            ??throw new InvalidOperationException("Select a current region.");
        var length=current.Category switch {"Project.MAPRECTEX"=>292,"GAMEAREA"=>64,"GAMEGATE"=>28,"GAMEEFFECT"=>44,_=>0};
        if(length==0||current.Length!=length||GetMapRegion(current) is null
            ||current.Category=="GAMEGATE"&&current.Read(current.Fields.Single(field=>field.Name=="bGateType"))=="14")
            throw new InvalidOperationException("A verified area, hero gate or effect generator is required.");
        if(current.Category=="Project.MAPRECTEX"&&current.File.UInt32(4)!=1)throw new InvalidOperationException("Unverified project region version.");
        return current;
    }
    public RegionPropertiesSession CreateRegionPropertiesSession(NativeRecord record)
    {
        if(transactionOpen||activePenStroke is not null)throw new InvalidOperationException("Finish the current edit first.");
        record=VerifiedRegionPropertyRecord(record);var region=GetMapRegion(record)!;
        string Read(string name,string fallback="0")=>record.Fields.FirstOrDefault(field=>field.Name==name) is {} field?record.Read(field):fallback;
        int N(string name)=>int.Parse(Read(name),CultureInfo.InvariantCulture);
        var bounds=region.Bounds;
        var x1=Math.Clamp(bounds.X,0,Width-1);var y1=Math.Clamp(bounds.Y,0,Height-1);
        var x2=(int)Math.Clamp((long)bounds.X+bounds.Width-1,0,Width-1);var y2=(int)Math.Clamp((long)bounds.Y+bounds.Height-1,0,Height-1);
        bounds=region.Kind==MapRegionKind.Gate?new(x1,y1,1,1):new(Math.Min(x1,x2),Math.Min(y1,y2),Math.Abs(x2-x1)+1,Math.Abs(y2-y1)+1);
        var native=record.Category!="Project.MAPRECTEX";
        var values=new RegionPropertiesValues(bounds,Read("name",""),Read("info",""),region.Color,
            uint.Parse(Read("rect.textcolor",region.Color.ToString(CultureInfo.InvariantCulture)),CultureInfo.InvariantCulture),
            region.Kind==MapRegionKind.Area?uint.Parse(Read(native?"bAreaFlags":"flags"),CultureInfo.InvariantCulture):0,
            region.Kind==MapRegionKind.Effect?N(native?"wEffect1":"effect1"):0,
            region.Kind==MapRegionKind.Effect?N(native?"wEffect2":"effect2"):0,
            region.Kind==MapRegionKind.Effect?N(native?"wEffect3":"effect3"):0,
            region.Kind==MapRegionKind.Effect?N(native?"wEffect4":"effect4"):0,
            region.Kind==MapRegionKind.Effect?N(native?"bEffectFrequency":"freq"):0);
        return new(this,record,region.Kind,EditRevision,UnusedScriptFingerprint(),record.File.Data.Slice(record.Offset,record.Length).ToArray(),values);
    }
    public bool IsRegionPropertiesSessionCurrent(RegionPropertiesSession session)=>session is not null&&!session.Closed
        &&ReferenceEquals(session.Owner,this)&&!transactionOpen&&activePenStroke is null&&session.Revision==EditRevision
        &&UnusedScriptFingerprint().AsSpan().SequenceEqual(session.Fingerprint);
    private void VerifyRegionPropertiesSession(RegionPropertiesSession session)
    {if(!IsRegionPropertiesSessionCurrent(session))throw new InvalidOperationException("The region draft is closed, foreign or stale.");VerifiedRegionPropertyRecord(session.Record);}
    public void UpdateRegionProperties(RegionPropertiesSession session,RegionPropertiesValues values)
    {
        VerifyRegionPropertiesSession(session);_=RegionPropertyPayload(session,values);session.Update(values);
    }
    private byte[] RegionPropertyPayload(RegionPropertiesSession session,RegionPropertiesValues values)
    {
        var record=session.Record;var old=session.Baseline;ValidateMapRegionBounds(session.Kind,values.Bounds);
        if(values.Name is null||values.Information is null)throw new ArgumentNullException(nameof(values));
        if(!session.HasEditorLabels&&(values.Name!=old.Name||values.Information!=old.Information||values.Colour!=old.Colour||values.TextColour!=old.TextColour))
            throw new ArgumentException("Native game records do not store editor names or colours; use a DKM project.");
        if(values.Colour!=old.Colour&&values.Colour>0xFFFFFF||values.TextColour!=old.TextColour&&values.TextColour>0xFFFFFF)throw new ArgumentOutOfRangeException(nameof(values));
        if(session.Kind!=MapRegionKind.Area&&values.AreaFlags!=old.AreaFlags)throw new ArgumentException("Flags belong to areas.");
        if((values.AreaFlags&~0x71u)!=(old.AreaFlags&~0x71u))throw new ArgumentException("Retain unknown imported area flags.");
        var effects=new[]{values.Effect1,values.Effect2,values.Effect3,values.Effect4,values.Frequency};
        var prior=new[]{old.Effect1,old.Effect2,old.Effect3,old.Effect4,old.Frequency};
        for(var i=0;i<5;i++)if(effects[i]!=prior[i])
        {
            if(session.Kind!=MapRegionKind.Effect)throw new ArgumentException("Effect options belong to generators.");
            if(i==4){if(effects[i] is <1 or >255)throw new ArgumentOutOfRangeException(nameof(values));}
            else if(!EffectCatalogue.Choices.Any(choice=>choice.Value==effects[i].ToString(CultureInfo.InvariantCulture)))throw new ArgumentException("Choose a known effect; imported IDs may be retained.");
        }
        var result=session.Payload.ToArray();
        void Set(string name,object value)
        {
            var field=record.Fields.Single(field=>field.Name==name);var text=Convert.ToString(value,CultureInfo.InvariantCulture)!;
            if(field.Read(result)!=text)field.Encode(text).CopyTo(result,field.Offset);
        }
        var b=values.Bounds;var native=!session.HasEditorLabels;
        if(!native)
        {Set("rect.x1",b.X);Set("rect.y1",b.Y);Set("rect.x2",b.X+b.Width-1);Set("rect.y2",b.Y+b.Height-1);
            Set("name",values.Name);Set("info",values.Information);Set("rect.color",values.Colour);Set("rect.textcolor",values.TextColour);}
        else if(session.Kind==MapRegionKind.Area)
        {Set("dwAreaLocationStartX",b.X);Set("dwAreaLocationStartY",b.Y);Set("dwAreaLocationEndX",b.X+b.Width-1);Set("dwAreaLocationEndY",b.Y+b.Height-1);}
        else if(session.Kind==MapRegionKind.Gate){Set("dwGateLocationX",b.X);Set("dwGateLocationY",b.Y);}
        else{Set("dwEffectStartX",b.X);Set("dwEffectStartY",b.Y);Set("wEffectWidth",b.Width-1);Set("wEffectHeight",b.Height-1);}
        if(session.Kind==MapRegionKind.Area)Set(native?"bAreaFlags":"flags",values.AreaFlags);
        if(session.Kind==MapRegionKind.Effect)for(var i=0;i<5;i++)Set(EffectGenerators.StorageField(record,EffectGenerators.Fields[i]),effects[i]);
        return result;
    }
    public RegionPropertiesPlan PrepareRegionProperties(RegionPropertiesSession session)
    {
        VerifyRegionPropertiesSession(session);if(session.ActivePick is not null||session.ActiveGeneration is not null)throw new InvalidOperationException("Finish the region geometry choice or generation child first.");
        var payload=RegionPropertyPayload(session,session.Values);
        if(session.Generations.Count==0)return new(session,session.Version,payload);
        var replay=ReplayRegionGeneration(session,payload);
        return new(session,session.Version,payload,replay.Files.ToDictionary(row=>row.Key,row=>row.Value.Data.ToArray()),
            checked(1+replay.Document.EditorChangeCount),replay.Document.GetHistory().Undo);
    }
    public void ApplyEditorRegionProperties(RegionPropertiesPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);var session=plan.Session;VerifyRegionPropertiesSession(session);
        if(plan.Version!=session.Version||session.ActivePick is not null||session.ActiveGeneration is not null)throw new InvalidOperationException("Region options changed after preflight or a child choice is pending.");
        EditCore("Change region properties",()=>
        {
            if(plan.Result is {} result)foreach(var row in result)row.Key.Bytes=row.Value.ToArray();
            else MutateNativeGate(session.Record,()=>plan.Payload.CopyTo(session.Record.File.Bytes,session.Record.Offset));
            if(historyDepth!=0)operationChildren.Peek().AddRange(plan.Children);
        },()=>new(true,plan.Increments));
        session.Dispose();
    }
    public RegionPropertiesPick BeginRegionPropertiesPick(RegionPropertiesSession session)
    {
        VerifyRegionPropertiesSession(session);
        if(session.ActivePick is not null||session.ActiveGeneration is not null)throw new InvalidOperationException("One child choice is already pending.");
        var pick=new RegionPropertiesPick(session);session.ActivePick=pick;return pick;
    }
    public bool IsRegionPropertiesPickCurrent(RegionPropertiesPick pick)=>pick is not null&&!pick.Finished
        &&ReferenceEquals(pick.Session.ActivePick,pick)&&pick.Version==pick.Session.Version&&IsRegionPropertiesSessionCurrent(pick.Session);
    public void CompleteRegionPropertiesPick(RegionPropertiesPick pick,TileRect bounds)
    {
        if(!IsRegionPropertiesPickCurrent(pick))throw new InvalidOperationException("The region geometry choice is closed, foreign or stale.");
        UpdateRegionProperties(pick.Session,pick.Session.Values with{Bounds=bounds});
        pick.Finished=true;pick.Session.ActivePick=null;
    }
    public void CancelRegionPropertiesPick(RegionPropertiesPick pick)
    {
        ArgumentNullException.ThrowIfNull(pick);
        if(!ReferenceEquals(pick.Session.Owner,this))throw new ArgumentException("Geometry choice belongs to another document.");
        pick.Finished=true;if(ReferenceEquals(pick.Session.ActivePick,pick))pick.Session.ActivePick=null;
    }
}
