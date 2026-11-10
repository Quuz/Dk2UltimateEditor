namespace DK2MapEditor.Core.Editing;

// The physical-object subset of Config.h MVF_*; other view layers are separate.
[Flags]
public enum MapObjectViewFlags : uint
{
    None = 0, Creatures = 2, Doors = 4, Traps = 8, Objects = 16, SpecialItems = 32,
    Detailed = 0x10000, ObjectOwnerColours = 0x40000, SpecialOwnerColours = 0x80000
}
public sealed record MapObjectDisplayOptions(MapObjectViewFlags Flags = MapObjectDisplay.DefaultFlags);
public sealed record MapObjectMark(PlacedThingKind Kind, TilePoint Position, uint TypeID, byte Owner,
    int Data, bool Solid, bool OwnerMatches, bool HorizontalDoor, bool MissingDoorWalls);
public enum MapObjectShape { Rectangle, Ellipse, HalfEllipse }
public readonly record struct MapObjectBox(double X, double Y, double Width, double Height);
public sealed record MapObjectGlyph(MapObjectShape Shape, MapObjectBox Bounds, uint Colour,
    bool Invalid = false, bool Blueprint = false, MapObjectBox? Cross = null);
public sealed record MapCreatureCount(TilePoint Position, IReadOnlyList<int> Owners, bool Solid);

/// <summary>Immutable display-only projection of Things_Display.c. No map repairs,
/// script plans, selected records or catalogue pictures are used for canvas glyphs.</summary>
public sealed class MapObjectDisplay
{
    public const MapObjectViewFlags DefaultFlags = MapObjectViewFlags.Creatures | MapObjectViewFlags.Doors
        | MapObjectViewFlags.Traps | MapObjectViewFlags.Objects | MapObjectViewFlags.SpecialItems | MapObjectViewFlags.Detailed;
    public IReadOnlyList<MapObjectMark> Marks { get; }
    public IReadOnlyList<MapCreatureCount> Creatures { get; }
    private MapObjectDisplay(List<MapObjectMark> marks, List<MapCreatureCount> creatures)
    { Marks = marks.AsReadOnly(); Creatures = creatures.AsReadOnly(); }
    public static bool IsSolid(MapTile tile) => tile.Terrain is 1 or 2 or 6 or 7 or 9 or 30 or 254;
    // Datas_Thumbnails.c Players[] and Thumbnails.h heart colours, COLORREF.
    public static uint OwnerColour(byte owner) => owner switch
    { 1 => 0xCECFCE, 2 => 0xFFFF00, 3 => 0x1818E7, 4 => 0xE74D31, 5 => 0x5ABE5A, 6 => 0x31AAE7, 7 => 0xFF00FF, _ => 0xFFFF00 };
    public static MapObjectViewFlags Flag(PlacedThingKind kind) => kind switch
    { PlacedThingKind.Creature => MapObjectViewFlags.Creatures, PlacedThingKind.Door => MapObjectViewFlags.Doors,
        PlacedThingKind.Trap => MapObjectViewFlags.Traps, PlacedThingKind.Object => MapObjectViewFlags.Objects,
        PlacedThingKind.SpecialItem => MapObjectViewFlags.SpecialItems, _ => throw new ArgumentOutOfRangeException(nameof(kind)) };
    public static MapObjectDisplay Capture(NativeMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var marks = new List<MapObjectMark>(); var counts = new Dictionary<TilePoint, int[]>();
        bool Solid(int x, int y) => x < 0 || y < 0 || x >= document.Width || y >= document.Height || IsSolid(document.GetTile(x, y));
        foreach (var entry in document.ObjectManagerEntries())
        {
            var thing = entry.Thing; var p = thing.Position;
            if (p.X < 0 || p.Y < 0 || p.X >= document.Width || p.Y >= document.Height) continue;
            var tile = document.GetTile(p.X, p.Y); var solid = IsSolid(tile);
            if (thing.Kind == PlacedThingKind.Creature)
            {
                // Source switch counts exactly owners 1..7; owner 0 is not invented.
                if (thing.Owner is < 1 or > 7) continue;
                if (!counts.TryGetValue(p, out var owners)) counts.Add(p, owners = new int[7]);
                owners[thing.Owner - 1]++; continue;
            }
            var horizontal = Solid(p.X - 1, p.Y) && Solid(p.X + 1, p.Y);
            var missing = !horizontal && (!Solid(p.X, p.Y - 1) || !Solid(p.X, p.Y + 1));
            var data = thing.Kind == PlacedThingKind.Trap && document.IsLegacyProject && entry.Record.File.UInt32(4) <= 4 ? 4 : entry.DataAll;
            marks.Add(new(thing.Kind, p, thing.TypeID, thing.Owner, data, solid, tile.Owner == thing.Owner, horizontal, missing));
        }
        // MainWindowProc draws doors, traps, specials, objects, then creature counts.
        int Order(PlacedThingKind kind) => kind switch { PlacedThingKind.Door => 0, PlacedThingKind.Trap => 1, PlacedThingKind.SpecialItem => 2, _ => 3 };
        return new(marks.OrderBy(mark => Order(mark.Kind)).ToList(), counts.OrderBy(pair => pair.Key.Y).ThenBy(pair => pair.Key.X)
            .Select(pair => new MapCreatureCount(pair.Key, Array.AsReadOnly(pair.Value), Solid(pair.Key.X, pair.Key.Y))).ToList());
    }
    public static MapObjectGlyph? Glyph(MapObjectMark mark, double cellSize, MapObjectDisplayOptions options)
        => Glyph(mark, cellSize, cellSize, options);
    public static MapObjectGlyph? Glyph(MapObjectMark mark, double cellWidth, double cellHeight, MapObjectDisplayOptions options)
    {
        ArgumentNullException.ThrowIfNull(mark); ArgumentNullException.ThrowIfNull(options);
        if (!MapZoom.Valid(cellWidth) || !MapZoom.Valid(cellHeight)) throw new ArgumentOutOfRangeException(nameof(cellWidth));
        if (mark.Kind == PlacedThingKind.Creature || (options.Flags & Flag(mark.Kind)) == 0) return null;
        var x = mark.Position.X * cellWidth; var y = mark.Position.Y * cellHeight;
        // Source float percentage arithmetic followed by integer truncation.
        int PadX(int percent) => (int)(((float)cellWidth * percent / 100f) / 2);
        int PadY(int percent) => (int)(((float)cellHeight * percent / 100f) / 2);
        MapObjectBox Inset(int percent) => new(x + PadX(percent), y + PadY(percent), cellWidth - PadX(percent) * 2, cellHeight - PadY(percent) * 2);
        var invalid = mark.Solid || mark.Kind is PlacedThingKind.Door or PlacedThingKind.Trap && !mark.OwnerMatches;
        var colour = OwnerColour(mark.Owner);
        if (mark.Kind == PlacedThingKind.Door)
        {
            var barrier = mark.TypeID is 6 or 7;
            var bad = invalid || !barrier && mark.MissingDoorWalls;
            var bounds = invalid ? Inset(80) : barrier ? Inset(40) : bad ? Inset(80)
                : mark.HorizontalDoor ? new MapObjectBox(x, y + PadY(60), cellWidth, cellHeight - PadY(60) * 2)
                : new MapObjectBox(x + PadX(60), y, cellWidth - PadX(60) * 2, cellHeight);
            // Source jumps to Draw_Door for barriers/invalid positions, bypassing lock X.
            return new(MapObjectShape.Rectangle, bounds, colour, bad, mark.Data == 2,
                !bad && !barrier && mark.Data == 1 ? Inset(60) : null);
        }
        if (mark.Kind == PlacedThingKind.Trap)
            return new(!invalid && mark.Data == 0 ? MapObjectShape.HalfEllipse : MapObjectShape.Ellipse,
                Inset(invalid ? 80 : 40), colour, invalid);
        if (mark.Kind == PlacedThingKind.Object)
        {
            var px = (int)((float)cellWidth * 60 / 100f); var py = (int)((float)cellHeight * 60 / 100f);
            return new(MapObjectShape.Rectangle, new(x + px, y, cellWidth - px, cellHeight - py),
                mark.Solid ? 0u : (options.Flags & MapObjectViewFlags.ObjectOwnerColours) != 0 ? colour : 0x5555FFu, mark.Solid);
        }
        var specialPadX = (int)((float)cellWidth * 70 / 100f); var specialPadY = (int)((float)cellHeight * 70 / 100f);
        var specialBox = new MapObjectBox(x, y + specialPadY, cellWidth - specialPadX, cellHeight - specialPadY);
        return new(MapObjectShape.Rectangle, specialBox,
            mark.Solid ? 0u : (options.Flags & MapObjectViewFlags.SpecialOwnerColours) != 0 ? colour : 0xFFFF00u,
            mark.Solid, Cross: mark.Solid ? null : specialBox);
    }
}
