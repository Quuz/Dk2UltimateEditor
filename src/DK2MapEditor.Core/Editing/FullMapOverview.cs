using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public enum FullMapMarkerKind { Trap, Door, Creature }
public sealed record FullMapMarker(FullMapMarkerKind Kind, TilePoint Position, byte Owner);
public readonly record struct FullMapAnimation(int Traps, int Doors, int Neutral, int Rooms)
{
    // Windows_Fullmap.c: 100 ms ticks, traps every tick, other offsets every
    // second tick. Editor.c initializes the room offset to 2, not zero.
    public static FullMapAnimation At(long tick)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        var slow = tick / 2;
        return new((int)(tick % 2), (int)(slow % 2), (int)(slow % 7), (int)((slow % 7 + 2) % 7));
    }
    public uint Colour(FullMapMarker marker)
    {
        var owner = marker.Owner is >= 1 and <= 7 ? marker.Owner : (byte)2;
        if (owner == 2) return Wall(Neutral, false);
        return Wall(owner - 1, marker.Kind switch { FullMapMarkerKind.Trap => Traps != 0, FullMapMarkerKind.Door => Doors == 0, _ => false });
    }
    public uint RoomColour => Wall(Rooms, false);
    private static uint Wall(int index, bool claimed) => MapOverviewPalette.ColorRef(new(claimed ? (byte)8 : (byte)9, (byte)(index + 1), 1));
}

/// <summary>Read-only source-derived fullscreen display data. Unknown values
/// use the original terrain fallback; invalid entities are skipped, never repaired.</summary>
public sealed class FullMapOverview
{
    private sealed record TileRule(int Icon, bool Neutral, bool Hover);
    private static readonly TileRule[,,] rules = BuildRules();
    public IReadOnlyList<TilePoint> NeutralCells { get; }
    public IReadOnlyList<FullMapMarker> Markers { get; }
    private FullMapOverview(List<TilePoint> cells, List<FullMapMarker> markers)
    { NeutralCells = cells.AsReadOnly(); Markers = markers.AsReadOnly(); }
    private static TileRule Rule(MapTile tile) => rules[tile.Terrain, tile.Owner is >= 1 and <= 7 ? tile.Owner : 2, tile.Variant is 1 or 2 ? tile.Variant - 1 : 0];
    public static int HoverIcon(MapTile tile) => Rule(tile) is { Hover: true } rule ? rule.Icon : 0;
    public static bool IsAnimatedNeutral(MapTile tile) => Rule(tile).Neutral;

    public static FullMapOverview Capture(NativeMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var cells = new List<TilePoint>(); var markers = new List<FullMapMarker>();
        for (var y = 0; y < document.Height; y++) for (var x = 0; x < document.Width; x++)
            if (IsAnimatedNeutral(document.GetTile(x,y))) cells.Add(new(x,y));
        foreach (var record in document.Records())
        {
            FullMapMarkerKind? kind = null; int owner;
            if (record.File.Signature == NativeMapDocument.LegacyThings && record.Category != "Project.Opaque")
            {
                kind = Number(record,"node.type") switch { 0 => FullMapMarkerKind.Creature, 1 => FullMapMarkerKind.Door, 2 => FullMapMarkerKind.Trap, _ => null };
                owner = Number(record,"owner");
            }
            else if (record.File.Signature == 0xBE && record.File.UInt32(4) == 4)
            {
                kind = record.Category switch { "GAMETRAP" => FullMapMarkerKind.Trap, "GAMEDOOR" => FullMapMarkerKind.Door,
                    "GAMEHERO" or "GAMENEUTRAL" or "GAMEKEEPER" => FullMapMarkerKind.Creature, _ => null };
                owner = record.Category switch { "GAMEHERO" => 1, "GAMENEUTRAL" => 2, _ => Number(record,"bGlobalPlayer") };
            }
            else continue;
            if (kind is null) continue;
            var xField = record.Fields.FirstOrDefault(field => field.Name.EndsWith("LocationX",StringComparison.Ordinal) || field.Name == "x");
            var yField = record.Fields.FirstOrDefault(field => field.Name.EndsWith("LocationY",StringComparison.Ordinal) || field.Name == "y");
            if (xField is null || yField is null || !int.TryParse(record.Read(xField),CultureInfo.InvariantCulture,out var x)
                || !int.TryParse(record.Read(yField),CultureInfo.InvariantCulture,out var y)
                || x < 0 || y < 0 || x >= document.Width || y >= document.Height) continue;
            markers.Add(new(kind.Value,new(x,y),owner is >= 1 and <= 7 ? (byte)owner : (byte)2));
        }
        // Original draw order: terrain, traps, doors, creatures.
        return new(cells,markers.OrderBy(marker => marker.Kind).ToList());
    }
    private static int Number(NativeRecord record,string name)
        => record.Fields.FirstOrDefault(field => field.Name == name) is { } field && int.TryParse(record.Read(field),CultureInfo.InvariantCulture,out var value) ? value : -1;

    private static TileRule[,,] BuildRules()
    {
        var groups = new[] { "World","Rooms","Gates","Walls" }.Select(LegacyCompilerRules.Elements).ToArray();
        var result = new TileRule[256,8,2];
        for (var terrain = 0; terrain <= 255; terrain++) for (var owner = 1; owner <= 7; owner++) for (var variant = 1; variant <= 2; variant++)
        {
            var key = (uint)(terrain << 24 | variant << 8); TileRule? rule = null;
            for (var group = 0; group < 4 && rule is null; group++)
            {
                var mask = group == 3 ? 0xFFFFFF00u : 0xFF00FF00u;
                var id = group == 3 ? key | (uint)(owner << 16) : key;
                var element = groups[group].FirstOrDefault(item => (item.ID & mask) == id);
                if (element is null) continue;
                var effectiveOwner = group == 1 ? owner : (int)((element.ID >> 16) & 255);
                var mana = element.ID == 0x1F020101;
                rule = new(element.Icon,effectiveOwner == 2 && (mana || group == 1 || group == 3 && terrain == 8),mana || group is 1 or 2);
            }
            result[terrain,owner,variant - 1] = rule ?? new(0,false,false);
        }
        return result;
    }
}
