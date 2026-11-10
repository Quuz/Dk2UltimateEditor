namespace DK2MapEditor.Core.Editing;

/// <summary>Original map-property catalogue rows and packed NewMap fill identities.</summary>
public static class MapRuleImageResources
{
    public static int? ForRule(MapRuleKind kind, int sourceRowIndex)
    {
        if (sourceRowIndex < 0) return null;
        string[] groups = kind switch
        {
            MapRuleKind.Rooms => ["VarRooms"], MapRuleKind.Traps => ["Traps"],
            MapRuleKind.Doors => ["Doors"], MapRuleKind.Spells => ["Spells"],
            MapRuleKind.CreaturePool or MapRuleKind.CreatureModifier => ["BadCreatures", "BadElites", "Heros"],
            _ => []
        };
        var resource = groups.SelectMany(LegacyCompilerRules.Elements).Skip(sourceRowIndex).FirstOrDefault()?.Icon;
        return resource is > 0 ? resource : null;
    }
    public static int? ForFill(uint packedID)
    {
        var resource = LegacyCompilerRules.Elements("NewMap").FirstOrDefault(item => item.ID == packedID)?.Icon;
        return resource is > 0 ? resource : null;
    }
}
