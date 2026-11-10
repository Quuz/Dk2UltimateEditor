namespace DK2MapEditor.Core.Editing;

/// <summary>Things_ManagerGetImage returns index0 of the manager's shared list
/// for unknown IDs, rather than the separate chooser's per-kind category image.</summary>
public static class ObjectManagerImageResources
{
    public static int For(PlacedThingKind kind,uint id,int version=17)
    {
        var (group,fallback)=kind switch
        {
            PlacedThingKind.Creature=>(new[]{"BadCreatures","BadElites","Heros"},1115),
            PlacedThingKind.Door=>(new[]{"Doors"},1301),PlacedThingKind.Trap=>(new[]{"Traps"},1301),
            PlacedThingKind.Object=>(new[]{"Objects"},1508),PlacedThingKind.SpecialItem=>(new[]{version>=17?"MagicsV17":"Magics"},1508),
            _=>throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return group.SelectMany(LegacyCompilerRules.Elements).FirstOrDefault(entry=>entry.ID==id)?.Icon??fallback;
    }
}
