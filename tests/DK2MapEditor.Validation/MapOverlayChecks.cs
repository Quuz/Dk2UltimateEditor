using System.Globalization;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class MapOverlayChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    public static void Run()
    {
        var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var source = File.ReadAllText(Path.Combine(repo, "Source/Includes/Config.h"));
        var names = new[] { "AREAS", "GATES", "PATHS", "EFFECTS", "CAMERAS", "EXPANDGATES" };
        var flags = Enum.GetValues<MapOverlayFlags>().Where(flag => flag != 0).ToArray();
        Expect(flags.Select((flag, index) => (uint)flag == uint.Parse(Regex.Match(source, @"#define\s+MVF_" + names[index] + @"\s+0x([\da-fA-F]+)").Groups[1].Value, NumberStyles.HexNumber)).All(value => value), "Overlay source flag words differ.");
        var all = Enum.GetValues<MapObjectViewFlags>().Where(flag => flag != 0).Select(flag => (uint)flag).Concat(flags.Select(flag => (uint)flag)).Append(1u).ToArray();
        Expect(all.Distinct().Count() == 15 && (uint)MapOverlayDisplay.DefaultFlags == 0x7C0 && new MapOverlayOptions().PathColour == 0xFFFFFF, "All fifteen source flags/defaults not covered.");
        // Independent direct source integer camera geometry, all overlap subsets;
        // global keeper index is deliberately not rank among occupied positions.
        foreach (var width in MapZoom.Presets.Concat([2, 17, 64, 512])) foreach (var height in MapZoom.Presets.Concat([3, 63, 64, 512]))
        for (uint id = 3; id <= 7; id++) for (var others = 0; others <= 4; others++)
        {
            var x = 4 * width + (width - 16) / 2; var y = 5 * height + (height - 16) / 2;
            var delta = others == 0 ? 0 : (others - (int)(id - 3)) * 2;
            var glyph = MapOverlayDisplay.CameraGlyph(new(id, new(4, 5), others), width, height);
            Expect(glyph.Resource == 17 + id && glyph.Icon == new MapObjectBox(x - delta, y - delta, 16, 16)
                && glyph.Ellipse == new MapObjectBox(x - width * 2 + 8 - delta, y - height * 2 + 8 - delta, width * 4 + delta * 2, height * 4 + delta * 2)
                && glyph.Colour == MapObjectDisplay.OwnerColour((byte)id), "Source camera/overlap geometry differs.");
            var start = new TileRect(2, 3, 3, 5); var end = new TileRect(9, 7, 2, 4);
            Expect(MapOverlayDisplay.PathGlyph(new(start, end, false), width, height) == new MapPathGlyph(2 * width + 3 * width / 2, 3 * height + 5 * height / 2, 9 * width + 2 * width / 2, 7 * height + 4 * height / 2, false), "Integer source path centres differ.");
            Expect(MapOverlayDisplay.PathGlyph(new(start, start, true), width, height) == new MapPathGlyph(2 * width + 3 * width / 9, 3 * height + 5 * height / 9, 5 * width - 3 * width / 9, 8 * height - 5 * height / 9, true), "Integer self-loop ellipse differs.");
        }
        foreach (var native in new[] { false, true }) Capture(native);
        GateEdges();
    }
    private static void Set(NativeMapDocument doc, NativeRecord record, string field, string value) => doc.SetField(record, record.Fields.Single(item => item.Name == field), value);
    private static void Capture(bool native)
    {
        var doc = NativeMapDocument.CreateNewMap(new("Overlays", "Overlays", 16, 14, Players: 5, NativeFormat: native));
        for (uint id = 3; id <= 7; id++) doc.SetPlayerCamera(id, new(5, 6));
        var area = doc.AddMapRegion(MapRegionKind.Area, new(2, 2, 4, 3));
        doc.LinkPatrolAreas(area, area);
        var effect = doc.AddMapRegion(MapRegionKind.Effect, new(8, 2, 2, 3));
        var gate = doc.AddMapRegion(MapRegionKind.Gate, new(12, 2, 1, 1));
        if (!native) { Set(doc, area, "name", "Named area"); Set(doc, area, "rect.textcolor", "305419896"); Set(doc, area, "rect.hidden", "1"); Set(doc, gate, "rect.x2", "15"); }
        var original = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        var view = MapOverlayDisplay.Capture(doc);
        Expect(view.Cameras.Count == 5 && view.Cameras.All(camera => camera.Overlaps == 4), "Keeper camera overlap projection missing.");
        Expect(view.Regions.Select(region => region.Kind).SequenceEqual(new[] { MapRegionKind.Effect, MapRegionKind.Gate, MapRegionKind.Area }) && view.Regions[1].Bounds.Width == 1 && view.Paths.Single().Loop, "Source overlay order/gate point/self-link differs.");
        if (!native) Expect(view.Regions[2].Name == "Named area" && view.Regions[2].Hidden && view.Regions[2].TextColour == 0x345678, "Project name/hidden/masked colour not retained.");
        Expect(original.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history, "Overlay capture mutated editing data.");
        for (var mask = 0; mask < 32; mask++)
        {
            for (uint id = 3; id <= 7; id++) doc.SetPlayerCamera(id, (mask & 1 << (int)(id - 3)) != 0 ? new(5, 6) : new(10, (int)id));
            var selected = MapOverlayDisplay.Capture(doc).Cameras.Where(camera => camera.Position == new TilePoint(5, 6)).ToArray();
            Expect(selected.All(camera => camera.Overlaps == System.Numerics.BitOperations.PopCount((uint)mask) - 1), "Absent earlier keeper changes overlap rank.");
        }
        doc.SetPlayerCamera(3, null);
        Expect(MapOverlayDisplay.Capture(doc).Cameras.Any(camera => camera.PlayerID == 3) == native, "Project sentinel confused with effective native camera.");
        if (!native)
        {
            var row = doc.PlayerGeometries().Single(row => row.ID == 4); Set(doc, row.Record, "PlayerCameraY", "-1");
            Expect(!MapOverlayDisplay.Capture(doc).Cameras.Any(camera => camera.PlayerID == 4), "Y sentinel not suppressed.");
            Set(doc, row.Record, "PlayerCameraY", "9999"); Expect(!MapOverlayDisplay.Capture(doc).Cameras.Any(camera => camera.PlayerID == 4), "Out-of-map camera not guarded.");
        }
        else
        {
            Set(doc, gate, "bGateType", "14"); Expect(MapOverlayDisplay.Capture(doc).Regions.All(region => region.Kind != MapRegionKind.Gate), "Native heart incorrectly rendered as gate region.");
        }
    }
    private static void GateEdges()
    {
        var doc = NativeMapDocument.CreateNewMap(new("GateEdges", "GateEdges", 12, 10));
        var entry = LegacyCompilerRules.Elements("Gates").First(); var tile = new MapTile((byte)(entry.ID >> 24), 3, (byte)(entry.ID >> 8));
        var random = new Random(218137);
        for (var iteration = 0; iteration < 100; iteration++)
        {
            var data = new MapTile[12, 10];
            doc.Edit("Gate edge fixture", () =>
            {
                for (var y = 0; y < 10; y++) for (var x = 0; x < 12; x++)
                { data[x, y] = random.Next(3) == 0 ? new(8, 2, 1) : tile with { Owner = (byte)random.Next(1, 8), Variant = (byte)(random.Next(4) == 0 ? 2 : tile.Variant) }; doc.Paint([new(x, y)], data[x, y]); }
            });
            for (var y = 0; y < 10; y++) for (var x = 0; x < 12; x++)
            {
                var value = data[x, y]; var left = x; var right = x; var top = y; var bottom = y;
                bool Same(int xx, int yy) => data[xx, yy].Terrain == value.Terrain && data[xx, yy].Variant == value.Variant;
                if (LegacyCompilerRules.Elements("Gates").Any(item => (byte)(item.ID >> 24) == value.Terrain && (byte)(item.ID >> 8) == value.Variant))
                {
                    for (var xx = x - 1; xx >= 0 && Same(xx, y); xx--) left = xx;
                    for (var xx = x + 1; xx < 12 && Same(xx, y); xx++) right = xx;
                    for (var yy = y - 1; yy >= 0 && Same(left, yy); yy--) top = yy;
                    for (var yy = y + 1; yy < 10 && Same(right, yy); yy++) bottom = yy;
                }
                Expect(MapOverlayDisplay.ExpandGate(doc, new(x, y)) == new TileRect(left, top, right - left + 1, bottom - top + 1), "Safe ordered gate-edge expansion differs.");
            }
        }
        Expect(MapOverlayDisplay.ExpandGate(doc, new(-1, 500)) == new TileRect(-1, 500, 1, 1), "Invalid gate origin read outside map.");
    }
}
