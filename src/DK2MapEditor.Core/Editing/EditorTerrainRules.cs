namespace DK2MapEditor.Core.Editing;

/// <summary>Original editor terrain policy, separate from lossless raw tile access.
/// The original fourth in-memory byte was a catalogue discriminator, not the
/// native file's reserved byte. Protection is rebuilt at the map's outer edge.</summary>
public static class EditorTerrainRules
{
    private static readonly IReadOnlyList<LegacyCompilerRules.Element> terrainCatalogue =
        LegacyCompilerRules.Elements("World").Concat(LegacyCompilerRules.Elements("Rooms")).Concat(LegacyCompilerRules.Elements("Gates"))
            .Where(item => (byte)(item.ID >> 24) != 0xFE).ToArray();
    private static readonly IReadOnlyList<LegacyCompilerRules.Element> fixedOwnerTiles =
        LegacyCompilerRules.Elements("World").Concat(LegacyCompilerRules.Elements("Gates")).Where(item => (byte)(item.ID >> 24) != 0xFE).ToArray();
    private static readonly IReadOnlyList<LegacyCompilerRules.Element> fillPresets = LegacyCompilerRules.Elements("NewMap");

    public static bool IsSpecialCutFill(MapTile selected) => selected.Terrain is 0x0E or 0x0C or 0x28 or 0x1C or 0x21 or 0x22 or 0x25;

    public static EditorTerrainFootprint Footprint(MapTile selected)
    {
        selected = NormalizePaint(selected);
        var entry = terrainCatalogue.FirstOrDefault(item => (byte)(item.ID >> 24) == selected.Terrain && (byte)(item.ID >> 8) == selected.Variant);
        return entry is null ? new(1, 1, 0, 0) : new(entry.MinWidth, entry.MinHeight, entry.MaxWidth, entry.MaxHeight);
    }

    /// <summary>World terrain and hero gates have a fixed catalogue owner and
    /// variant. Rooms and owned paths/walls retain their selected player.</summary>
    public static MapTile NormalizePaint(MapTile selected)
    {
        var matches = fixedOwnerTiles.Where(item => (byte)(item.ID >> 24) == selected.Terrain).ToArray();
        var entry = matches.FirstOrDefault(item => (byte)(item.ID >> 8) == selected.Variant) ?? matches.FirstOrDefault();
        return entry is null ? selected : selected with { Owner = (byte)(entry.ID >> 16), Variant = (byte)(entry.ID >> 8) };
    }

    public static MapTile DecodeFillPreset(uint packedPreset)
    {
        if (!fillPresets.Any(item => item.ID == packedPreset)) throw new InvalidDataException("The project has an unverified map fill preset; choose a normal terrain before cutting.");
        return new((byte)(packedPreset >> 24), (byte)(packedPreset >> 16), (byte)(packedPreset >> 8));
    }
}

/// <summary>Original catalogue limits: a zero maximum means unbounded. These
/// constrain cursor/gesture geometry, not raw data edits or flood components.</summary>
public readonly record struct EditorTerrainFootprint(int MinWidth, int MinHeight, int MaxWidth, int MaxHeight)
{
    public bool RequiresRectangle => MinWidth > 1 || MinHeight > 1 || MaxWidth != 0 || MaxHeight != 0;
    public bool FixedSize => MinWidth == MaxWidth && MinHeight == MaxHeight;

    public TileRect? CursorRectangle(TilePoint cursor, int mapWidth, int mapHeight)
    {
        if (mapWidth < MinWidth || mapHeight < MinHeight) return null;
        return new(Math.Clamp(cursor.X, 0, mapWidth - MinWidth), Math.Clamp(cursor.Y, 0, mapHeight - MinHeight), MinWidth, MinHeight);
    }

    public TileRect? GestureRectangle(TilePoint start, TilePoint end, int mapWidth, int mapHeight, bool immediate = false)
    {
        if (CursorRectangle(start, mapWidth, mapHeight) is not { } cursor) return null;
        if (immediate || FixedSize) return cursor;
        int ClampAxis(int origin, int target, int minimum, int maximum, int mapSize)
        {
            target = Math.Clamp(target, 0, mapSize - 1);
            var size = Math.Abs(target - origin) + 1;
            // Mouse_UpdateRect expands an undersized reversed drag in the positive
            // direction, but retains direction when reducing an oversized drag.
            if (size < minimum) return origin + minimum - 1;
            if (maximum != 0 && size > maximum) return origin + (target < origin ? 1 - maximum : maximum - 1);
            return target;
        }
        var target = new TilePoint(ClampAxis(cursor.X, end.X, MinWidth, MaxWidth, mapWidth), ClampAxis(cursor.Y, end.Y, MinHeight, MaxHeight, mapHeight));
        return TileRect.Between(new(cursor.X, cursor.Y), target);
    }
}
