namespace DK2MapEditor.Core.Editing;

/// <summary>Visible source toolbar palette rows; separate from canvas owner-frame composition.</summary>
public static class MapPaletteImageResources
{
    public static int? ForTerrain(byte terrain, byte owner, byte variant)
    {
        if (owner is < 1 or > 7 || variant is < 1 or > 2) return null;
        var key = (uint)(terrain << 24 | variant << 8);
        foreach (var group in new[] { "World", "Gates", "Walls", "Rooms" })
        {
            var rows = LegacyCompilerRules.Elements(group).Take(group == "World" ? 10 : group == "Rooms" ? 23 : int.MaxValue);
            var mask = group == "Walls" ? 0xFFFFFF00u : 0xFF00FF00u;
            var id = group == "Walls" ? key | (uint)(owner << 16) : key;
            if (rows.FirstOrDefault(row => (row.ID & mask) == id) is { Icon: > 0 } row) return row.Icon;
        }
        return null;
    }
    public static int? ForDrawingTool(string tool) => tool switch
    {
        "Rectangle" => 900, "FilledRectangle" => 901, "Ellipse" => 902,
        "FilledEllipse" => 903, "Line" => 904, "Brush" => 905, "Fill" => 906, _ => null
    };
    public static int? ForTemplate(string? template) => template switch
    {
        "Creature" or "NeutralCreature" or "Hero" or "KeeperCreature" or "GAMEKEEPER" or "GAMENEUTRAL" or "GAMEHERO" => 920,
        "Door" or "GAMEDOOR" => 921, "Trap" or "GAMETRAP" => 922,
        "Object" or "GAMEOBJECT" => 923, "MagicalObject" => 924, _ => null
    };
}
