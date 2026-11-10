using System.Globalization;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class MapObjectDisplayChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var config = File.ReadAllText(Path.Combine(repo, "Source/Includes/Config.h"));
        var names = new[] { "CREATURES", "DOORS", "TRAPS", "OBJECTS", "MAGICALOBJECTS", "DETAILED", "OBJECTUSESPLYCOL", "MAGICUSESPLYCOL" };
        var flags = Enum.GetValues<MapObjectViewFlags>().Where(flag => flag != 0).ToArray();
        Expect(flags.Length == names.Length && flags.Select((flag, index) => (uint)flag == uint.Parse(Regex.Match(config, @"#define\s+MVF_" + names[index] + @"\s+0x([\da-fA-F]+)").Groups[1].Value, NumberStyles.HexNumber)).All(value => value), "View flag words differ from Config.h.");
        var colours = File.ReadAllText(Path.Combine(repo, "Source/Includes/Thumbnails.h"));
        for (byte owner = 1; owner <= 7; owner++)
        {
            var name = owner == 1 ? "HERO" : owner == 2 ? "NEUTRAL" : "KEEPER" + (owner - 2);
            var expected = uint.Parse(Regex.Match(colours, @"#define\s+THUMB_" + name + @"HEART\s+0x([\da-fA-F]+)").Groups[1].Value, NumberStyles.HexNumber);
            Expect(MapObjectDisplay.OwnerColour(owner) == expected, "Owner colour differs from source.");
        }
        for (var terrain = 0; terrain <= 255; terrain++)
            Expect(MapObjectDisplay.IsSolid(new((byte)terrain, 3, 1)) == new[] { 1, 2, 6, 7, 9, 30, 254 }.Contains(terrain), "Source solidity classification changed.");
        Geometry(); foreach (var native in new[] { false, true }) Capture(native);
    }
    private static void Geometry()
    {
        var all = new MapObjectDisplayOptions((MapObjectViewFlags)uint.MaxValue);
        for (var zoom = 2; zoom <= 48; zoom++) for (var neighbors = 0; neighbors < 16; neighbors++)
        foreach (var id in new uint[] { 1, 6, 7, 999 }) foreach (var status in new[] { 0, 1, 2, 3 })
        foreach (var solid in new[] { false, true }) foreach (var match in new[] { false, true })
        {
            // Independent direct branch transcription of Things_DrawDoors.
            var horizontal = (neighbors & 3) == 3; var missing = !horizontal && (neighbors & 12) != 12;
            var badTile = solid || !match; var barrier = id is 6 or 7;
            var pad40 = (int)((zoom * 40f / 100f) / 2); var pad60 = (int)((zoom * 60f / 100f) / 2); var pad80 = (int)((zoom * 80f / 100f) / 2);
            var ox = 3 * zoom; var oy = 4 * zoom;
            MapObjectBox Inset(int pad) => new(ox + pad, oy + pad, zoom - 2 * pad, zoom - 2 * pad);
            var expected = badTile ? Inset(pad80) : barrier ? Inset(pad40) : missing ? Inset(pad80)
                : horizontal ? new MapObjectBox(ox, oy + pad60, zoom, zoom - 2 * pad60) : new MapObjectBox(ox + pad60, oy, zoom - 2 * pad60, zoom);
            var mark = new MapObjectMark(PlacedThingKind.Door, new(3, 4), id, 4, status, solid, match, horizontal, missing);
            var glyph = MapObjectDisplay.Glyph(mark, zoom, all)!;
            Expect(glyph.Bounds == expected && glyph.Blueprint == (status == 2) && glyph.Invalid == (badTile || !barrier && missing)
                && glyph.Cross == (!badTile && !barrier && !missing && status == 1 ? Inset(pad60) : null), "Door geometry/status/source jump differs.");
            Expect(MapObjectDisplay.Glyph(mark, zoom, new(MapObjectViewFlags.None)) is null, "Hidden door produced a glyph.");
        }
        for (var zoom = 2; zoom <= 48; zoom++) foreach (var kind in new[] { PlacedThingKind.Trap, PlacedThingKind.Object, PlacedThingKind.SpecialItem })
        foreach (var solid in new[] { false, true }) foreach (var match in new[] { false, true }) foreach (var data in new[] { 0, 1, -1 })
        {
            var mark = new MapObjectMark(kind, new(3, 4), 1, 5, data, solid, match, false, true);
            var glyph = MapObjectDisplay.Glyph(mark, zoom, new())!;
            if (kind == PlacedThingKind.Trap)
            {
                var bad = solid || !match; var pad = (int)((zoom * (bad ? 80f : 40f) / 100f) / 2);
                Expect(glyph.Bounds == new MapObjectBox(3 * zoom + pad, 4 * zoom + pad, zoom - 2 * pad, zoom - 2 * pad)
                    && glyph.Shape == (!bad && data == 0 ? MapObjectShape.HalfEllipse : MapObjectShape.Ellipse) && glyph.Invalid == bad, "Trap blueprint/invalid projection differs.");
            }
            else
            {
                var pad = (int)(zoom * (kind == PlacedThingKind.Object ? 60f : 70f) / 100f);
                var expected = kind == PlacedThingKind.Object ? new MapObjectBox(3 * zoom + pad, 4 * zoom, zoom - pad, zoom - pad)
                    : new MapObjectBox(3 * zoom, 4 * zoom + pad, zoom - pad, zoom - pad);
                Expect(glyph.Bounds == expected && glyph.Colour == (solid ? 0u : kind == PlacedThingKind.Object ? 0x5555FFu : 0xFFFF00u)
                    && glyph.Invalid == solid && (glyph.Cross is not null) == (kind == PlacedThingKind.SpecialItem && !solid), "Object/special corner/colour/cross projection differs.");
                Expect(MapObjectDisplay.Glyph(mark, zoom, all)!.Colour == (solid ? 0u : MapObjectDisplay.OwnerColour(5)), "Optional player colour differs.");
            }
        }
    }
    private static void Capture(bool native)
    {
        var doc = NativeMapDocument.CreateNewMap(new("Display", "Display", 14, 12, NativeFormat: native));
        doc.Paint(MapDrawing.Rectangle(new(1, 1, 12, 10), true), new(8, 3, 1));
        void Add(PlacedThingKind kind, TilePoint position, byte owner = 3, int data = 1, uint id = 1)
            => doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(kind) with { Owner = owner, TypeID = id, DataAll = data }, new(Position: position, Flags: (ObjectBatchFlags)127)), [0]);
        Add(PlacedThingKind.Door, new(2, 3)); Add(PlacedThingKind.Trap, new(4, 3), data: 0, id: 4);
        Add(PlacedThingKind.Object, new(6, 3)); Add(PlacedThingKind.SpecialItem, new(8, 3), id: 117);
        for (byte owner = 1; owner <= 7; owner++) Add(PlacedThingKind.Creature, new(10, 3), owner);
        Add(PlacedThingKind.Creature, new(10, 3), 3); Add(PlacedThingKind.Creature, new(10, 3), 3);
        doc.Paint([new(1, 3), new(3, 3), new(10, 3)], new(2, 2, 1));
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory();
        var snapshot = MapObjectDisplay.Capture(doc);
        Expect(snapshot.Marks.Select(mark => mark.Kind).SequenceEqual(new[] { PlacedThingKind.Door, PlacedThingKind.Trap, PlacedThingKind.SpecialItem, PlacedThingKind.Object }), "Main-window physical draw order differs.");
        Expect(snapshot.Marks[0].HorizontalDoor && !snapshot.Marks[0].MissingDoorWalls && snapshot.Marks[1].Data == 0, "Neighbors or raw shots omitted.");
        Expect(snapshot.Creatures.Count == 1 && snapshot.Creatures[0].Solid && snapshot.Creatures[0].Owners.SequenceEqual(new[] { 1, 1, 3, 1, 1, 1, 1 }), "Per-owner creature aggregation/source solid warning differs.");
        Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history.Undo.Count, "Read-only display changed bytes/history.");
        var record = doc.ObjectManagerEntries().First(entry => entry.Thing.Kind == PlacedThingKind.Door).Record;
        var x = record.Fields.First(field => native ? field.Name.EndsWith("LocationX", StringComparison.Ordinal) : field.Name == "x");
        doc.SetField(record, x, native ? "4294967295" : "-1");
        Expect(MapObjectDisplay.Capture(doc).Marks.Count == 3 && snapshot.Marks.Count == 4, "Out-of-map glyph or immutable snapshot changed.");
        doc.Undo(); Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Display/undo changed source bytes.");
    }
}
