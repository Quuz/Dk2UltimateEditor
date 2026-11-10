using System.Globalization;

namespace DK2MapEditor.Core.Editing;

internal sealed record RegionGateGenerationOperation(HeroGateGenerationOptions Options,byte[] ParentPayload);

/// <summary>A child lifetime, with no externally mutable working document.</summary>
public sealed class RegionGateGenerationSession : IDisposable
{
    internal RegionGateGenerationSession(RegionPropertiesSession parent,HeroGateGenerationOptions defaults)
    {Parent=parent;Version=parent.Version;Defaults=defaults;}
    internal RegionPropertiesSession Parent {get;}
    internal int Version {get;}
    internal bool Closed {get;private set;}
    public HeroGateGenerationOptions Defaults {get;}
    public bool HasEditorLabels=>Parent.HasEditorLabels;
    public void Dispose(){Closed=true;if(ReferenceEquals(Parent.ActiveGeneration,this))Parent.ActiveGeneration=null;}
}

public sealed record RegionGateGenerationItem(int Type,TileRect Footprint,bool CannotDestroy,bool AddGate,bool AddArea,bool AddEvents,
    uint? GateID,uint? AreaID,string GateName,string GateInformation,string AreaName,string AreaInformation,int ImageResource=89);

/// <summary>Immutable preview values: no NativeRecord, NativeFile or draft buffers
/// escape to callers. Consent refers to this specific child and parent version.</summary>
public sealed class RegionGateGenerationPlan
{
    internal RegionGateGenerationPlan(RegionGateGenerationSession child,HeroGateGenerationOptions options,HeroGateGenerationPlan generated)
    {
        Child=child;Options=options;Gates=generated.Gates;Areas=generated.Areas;Conditions=generated.Conditions;Actions=generated.Actions;
        Items=Array.AsReadOnly(generated.Items.Select(item=>new RegionGateGenerationItem(item.Site.Type,item.Site.Footprint,item.Site.CannotDestroy,
            item.AddGate,item.AddArea,item.AddEvents,item.GateID,item.AreaID,item.GateName,item.GateInfo,item.AreaName,item.AreaInfo,
            GateGenerationImageResources.ForSite(item.Site))).ToArray());
    }
    internal RegionGateGenerationSession Child {get;}
    internal HeroGateGenerationOptions Options {get;}
    public IReadOnlyList<RegionGateGenerationItem> Items {get;}
    public int Gates {get;}
    public int Areas {get;}
    public int Conditions {get;}
    public int Actions {get;}
    public bool HasWork=>Gates+Areas+Conditions>0;
}

public sealed partial class NativeMapDocument
{
    public RegionGateGenerationSession BeginRegionGateGeneration(RegionPropertiesSession parent)
    {
        VerifyRegionPropertiesSession(parent);
        if(parent.Kind!=MapRegionKind.Gate||parent.ActivePick is not null||parent.ActiveGeneration is not null)
            throw new InvalidOperationException("Select a gate draft without another pending child.");
        var defaults=HeroGateGenerationOptions.Defaults(this);
        defaults=defaults with{Gate=defaults.Gate with{Color=parent.Values.Colour,TextColor=parent.Values.TextColour}};
        var child=new RegionGateGenerationSession(parent,defaults);parent.ActiveGeneration=child;return child;
    }
    public bool IsRegionGateGenerationCurrent(RegionGateGenerationSession child)=>child is not null&&!child.Closed
        &&ReferenceEquals(child.Parent.ActiveGeneration,child)&&child.Version==child.Parent.Version&&IsRegionPropertiesSessionCurrent(child.Parent);
    private void VerifyRegionGateGeneration(RegionGateGenerationSession child)
    {if(!IsRegionGateGenerationCurrent(child))throw new InvalidOperationException("The generation child is closed, foreign or stale.");}
    public RegionGateGenerationPlan PreviewRegionGateGeneration(RegionGateGenerationSession child,HeroGateGenerationOptions options)
    {
        VerifyRegionGateGeneration(child);var parent=child.Parent;var replay=ReplayRegionGeneration(parent,RegionPropertyPayload(parent,parent.Values));
        HeroGateGenerationPlan plan;
        try { plan=replay.Document.PlanHeroGateGeneration(options); }
        catch(GateGenerationCapacityException error)
        {
            // The capacity report belongs to the real parent/child lifetime,
            // never to the temporary replay document retained by an exception.
            throw new GateGenerationCapacityException(error.ExistingAreas,error.ExistingGates,error.NeededAreas,error.NeededGates,
                ()=>IsRegionGateGenerationCurrent(child));
        }
        // Validate every prospective allocation/link before requesting consent,
        // not merely counts. A failed child never appends an operation.
        replay.Document.ApplyEditorHeroGateGeneration(plan);
        return new(child,options,plan);
    }
    public void AcceptRegionGateGeneration(RegionGateGenerationPlan plan,bool confirmed=false)
    {
        ArgumentNullException.ThrowIfNull(plan);VerifyRegionGateGeneration(plan.Child);
        if(plan.HasWork&&!confirmed)throw new InvalidOperationException("Confirm the current generation preview first.");
        var parent=plan.Child.Parent;
        // Replay the approved operation afresh from the live baseline plus the
        // parent's previous approved commands. No arbitrary child bytes apply.
        var fresh=PreviewRegionGateGeneration(plan.Child,plan.Options);
        if(!fresh.Items.SequenceEqual(plan.Items)||fresh.Conditions!=plan.Conditions||fresh.Actions!=plan.Actions)
            throw new InvalidOperationException("Refresh the generation preview.");
        if(plan.HasWork)parent.Generations.Add(new(plan.Options,RegionPropertyPayload(parent,parent.Values)));
        plan.Child.Dispose();parent.Update(parent.Values);
    }
    public void CancelRegionGateGeneration(RegionGateGenerationSession child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if(!ReferenceEquals(child.Parent.Owner,this))throw new ArgumentException("Generation child belongs to another document.");
        child.Dispose();
    }

    private (NativeMapDocument Document,Dictionary<NativeFile,NativeFile> Files) ReplayRegionGeneration(RegionPropertiesSession parent,byte[] finalPayload)
    {
        VerifyRegionPropertiesSession(parent);var copy=ClonePropertiesDocument();var working=copy.Document;
        if(copy.Files.Count!=TransactionFiles().Count()||!copy.Files.Values.ToHashSet().SetEquals(working.TransactionFiles()))
            throw new InvalidOperationException("Generation draft file topology is not verified.");
        var regionFile=copy.Files[parent.Record.File];
        NativeRecord Region()=>working.Records().Single(record=>record.File==regionFile&&record.Offset==parent.Record.Offset
            &&record.Length==parent.Record.Length&&record.Category==parent.Record.Category);
        var boundActions=new HashSet<ushort>();
        void Parent(byte[] payload)
        {
            working.MutateNativeGate(Region(),()=>payload.CopyTo(regionFile.Bytes,parent.Record.Offset));
            if(working.IsLegacyProject||boundActions.Count==0)return;
            // Source DKM actions target a gate ID, compiled to its CURRENT point.
            // Retain explicit child bindings as well, including a gate that
            // temporarily shares a position with an earlier physical entry.
            var bounds=working.GetMapRegion(Region())!.Bounds;var graph=ScriptGraph.Build(working);
            foreach(var id in boundActions)
            {
                var action=graph.Events[id].Record;
                foreach(var (name,value) in new[]{("NativeArgument5",bounds.X+1),("NativeArgument7",bounds.Y+1)})
                {var field=action.Fields.Single(field=>field.Name==name);field.Encode(value.ToString(CultureInfo.InvariantCulture)).CopyTo(action.File.Bytes,action.Offset+field.Offset);}
                working.SetNativeGateTarget(action,Region());
            }
        }
        foreach(var operation in parent.Generations)
        {
            Parent(operation.ParentPayload);var record=Region();var bounds=working.GetMapRegion(record)!.Bounds;
            var plan=working.PlanHeroGateGeneration(operation.Options);
            var bind=!working.IsLegacyProject&&plan.Items.Any(item=>item.AddEvents&&item.Site.Gate is {} gate&&SameRecord(gate,record));
            var before=bind?ScriptGraph.Build(working).Events.Keys.ToHashSet():[];
            working.ApplyEditorHeroGateGeneration(plan);
            if(bind)foreach(var item in ScriptGraph.Build(working).Events.Values.Where(item=>!before.Contains(item.ID)&&item.Type?.Name=="ACTION_DESTROYGATE"))
            {
                var action=item.Record;
                if(GateValue(action,"NativeArgument5")== (bounds.X+1).ToString(CultureInfo.InvariantCulture)
                    &&GateValue(action,"NativeArgument7")== (bounds.Y+1).ToString(CultureInfo.InvariantCulture))boundActions.Add(item.ID);
            }
        }
        Parent(finalPayload);return copy;
    }
}
