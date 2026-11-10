using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public static partial class ScenarioNamedCatalogues
{
    /// <summary>Bitmap resource identity for a selectable catalogue row. The row is
    /// necessary because door and trap values overlap. Imported unknown rows use -1.
    /// This never reads or changes a document, and never derives identity from a caption.</summary>
    public static int? ImageResource(string type, int parameter, int row, string value)
    {
        if (row < 0 || !contexts.TryGetValue((type, parameter), out var context)
            || !uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id)) return null;
        // These source controls use Misc_CreatePlayerComboBoxList, not an image list.
        if (context.Kind == "Players" && type is "TRIGGER_AREACONTENT" or "ACTION_ADDCREATURE" or "ACTION_ADDELEMENT" or "ACTION_CHANGESQUARE") return null;
        if (context.Any && row == 0 && id == 0) return context.Kind == "Rooms" && context.Mode == -1 ? 221 : 210;
        int? Variable(string group, uint variable) => LegacyCompilerRules.Elements(group).FirstOrDefault(item => item.Variable == variable)?.Icon;
        int? Creature()
        {
            var groups = context.Extra ? new[] { "BadCreatures", "BadElites", "Heros" } : ["BadCreatures", "Heros"];
            foreach (var group in groups)
                if (LegacyCompilerRules.Elements(group).Any(item => item.ID == id)) return Variable(group, id);
            return null;
        }
        switch (context.Kind)
        {
            case "Creatures": return Creature();
            case "Rooms": return Variable("Terrain", id);
            case "Doors": case "Traps": case "Spells": return Variable(context.Kind, id);
            case "Destroyed":
                var doors = LegacyCompilerRules.Elements("Doors");
                var index = row - (context.Any ? 1 : 0);
                var group = index < doors.Count ? doors : LegacyCompilerRules.Elements("Traps");
                if (index >= doors.Count) index -= doors.Count;
                return index >= 0 && index < group.Count && group[index].Variable == id ? group[index].Icon : null;
            case "Players": return id is >= 1 and <= 7 ? 930 + (int)id : null;
            case "Terrain":
                var terrain = LegacyCompilerRules.Elements("Terrain").FirstOrDefault(item => item.ID >> 24 == id);
                return terrain is null ? null : Variable("Terrain", terrain.Variable);
            case "Elements":
                var category = id >> 16; var element = id & 65535;
                return category switch
                {
                    1 => Variable("Terrain", element), 3 => Variable("Doors", element),
                    4 => Variable("Traps", element), 5 => Variable("Spells", element),
                    0 => element switch
                    {
                        242 => 211, 243 => 212, 241 => 213, 244 => 214, 245 => 215,
                        247 => 216, 248 => 217, 251 => 218, 250 => 219, 249 => 220, _ => null
                    },
                    _ => null
                };
            case "Bands": return 1212; // Source HerosList parent row: TrSpecial[3].
            default: return null; // Ordinary level/area/effect/gate combos have no source image.
        }
    }
}
