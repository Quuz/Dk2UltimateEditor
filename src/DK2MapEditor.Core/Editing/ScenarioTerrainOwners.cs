using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record ScenarioTerrainOwnerState(int TerrainParameter,bool KnownTerrain,string? ForcedOwner)
{
    public int OwnerParameter=>2;
    public bool OwnerEnabled=>KnownTerrain&&ForcedOwner is null;
}

/// <summary>LVN_ITEMCHANGED on the source terrain lists. Initial presentation
/// can disable the owner without rewriting an imported owner; only an actual
/// verified terrain selection applies the forced owner to the private draft.</summary>
public static class ScenarioTerrainOwners
{
    public static int? TerrainParameter(string type)=>type switch
    {
        "TRIGGER_AREACONTENT"=>3,"ACTION_CHANGESQUARE"=>1,_=>null
    };
    public static ScenarioTerrainOwnerState? State(string type,string terrain)
    {
        if(TerrainParameter(type) is not { } parameter)return null;
        var known=ScenarioNamedCatalogues.Choices(type,parameter).Any(choice=>choice.Value==terrain);
        if(!known)return new(parameter,false,null);
        var id=uint.Parse(terrain,CultureInfo.InvariantCulture);
        return new(parameter,true,id<=7||id==0x1F?"2":id==0x23?"1":null);
    }
}

public sealed partial class NativeMapDocument
{
    public ScenarioParameterDraft UpdateScenarioTerrainSelection(ScenarioParameterDraft draft,string terrain)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var state=ScenarioTerrainOwners.State(draft.Type,terrain)
            ??throw new ArgumentException("This source page has no terrain-owner callback.");
        if(!state.KnownTerrain)throw new ArgumentOutOfRangeException(nameof(terrain),"Select a verified source terrain.");
        string Field(int number)=>draft.Fields.Single(field=>field.Name=="param"+number||field.Name=="Param"+number).Name;
        var inputs=new List<ScenarioParameterValue>{new(Field(state.TerrainParameter),terrain)};
        if(state.ForcedOwner is { } owner)inputs.Add(new(Field(state.OwnerParameter),owner));
        return UpdateScenarioControlPage(draft,inputs);
    }
}
