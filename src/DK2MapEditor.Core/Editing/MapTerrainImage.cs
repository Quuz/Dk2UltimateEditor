namespace DK2MapEditor.Core.Editing;

public sealed record MapTerrainImage(int Resource, int? OwnerFrame, bool Fallback = false);
public static class MapTerrainImages
{
    private static readonly MapTerrainImage[,,] images = Build();
    public static MapTerrainImage For(MapTile tile) => images[tile.Terrain, tile.Owner is >= 1 and <= 7 ? tile.Owner : 2,
        tile.Variant is 1 or 2 ? tile.Variant - 1 : 0];
    private static MapTerrainImage[,,] Build()
    {
        var groups = new[] { "World", "Rooms", "Gates", "Walls" }.Select(LegacyCompilerRules.Elements).ToArray();
        var result = new MapTerrainImage[256, 8, 2];
        for (var terrain = 0; terrain < 256; terrain++) for (var owner = 1; owner <= 7; owner++) for (var variant = 1; variant <= 2; variant++)
        {
            var key = (uint)(terrain << 24 | variant << 8); MapTerrainImage? image = null;
            for (var group = 0; group < 4 && image is null; group++)
            {
                var mask = group == 3 ? 0xFFFFFF00u : 0xFF00FF00u;
                var id = group == 3 ? key | (uint)(owner << 16) : key;
                var item = groups[group].FirstOrDefault(item => (item.ID & mask) == id);
                if (item is null) continue;
                // Map_InitialiseZoom: owner 1 uses ordinary Rooms, owners 2..7
                // OR a scaled player frame (Misc_CreatePlayerImageList).
                image = new(item.Icon, group == 1 && owner >= 2 ? owner + 930 : null);
            }
            result[terrain, owner, variant - 1] = image ?? new(306, null, true);
        }
        return result;
    }
}
