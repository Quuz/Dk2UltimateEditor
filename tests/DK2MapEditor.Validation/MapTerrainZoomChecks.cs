using System.Globalization;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class MapTerrainZoomChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var zoomSource = File.ReadAllText(Path.Combine(repo, "Source/Sources/Windows/Windows_ZoomButton.c"));
        var presets = Regex.Matches(zoomSource, @"case\s+(\d+):\s*ZX\s*=\s*(\d+);\s*ZY\s*=\s*(\d+);")
            .Where(match => int.Parse(match.Groups[1].Value) <= 10).Select(match => int.Parse(match.Groups[2].Value)).ToArray();
        Expect(MapZoom.Presets.SequenceEqual(presets) && presets.Length == 11 && MapZoom.Safe(null) == new MapZoomOptions(64, 64, 64, 64), "Original zoom presets/defaults differ.");
        var safe = MapZoom.Safe(new(double.NaN, double.PositiveInfinity, -1, int.MaxValue));
        Expect(safe == new MapZoomOptions() && MapZoom.Safe(new(16, 128, 17, 63)) == new MapZoomOptions(16, 128, 17, 63), "Malformed/independent zoom settings not handled.");
        var text = Regex.Replace(File.ReadAllText(Path.Combine(repo, "Source/Sources/Datas/Datas_Map.c")), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        var names = new[] { "World", "Rooms", "Gates", "Walls" };
        var groups = names.Select(name => Regex.Matches(Regex.Match(text, @"ELEMENT\s+" + name + @"\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value,
            @"\{0x([\da-fA-F]{8}),0x[\da-fA-F]+,(\d+),").Select(match => (ID: uint.Parse(match.Groups[1].Value, NumberStyles.HexNumber), Icon: int.Parse(match.Groups[2].Value))).Where(item => item.ID != 0).ToArray()).ToArray();
        Expect(groups.Select(group => group.Length).SequenceEqual(new[] { 11, 23, 6, 14 }), "Commented Crypt row treated as active source terrain.");
        for (var terrain = 0; terrain <= 255; terrain++) for (var owner = 0; owner <= 8; owner++) for (var variant = 0; variant <= 3; variant++)
        {
            var effectiveOwner = owner is >= 1 and <= 7 ? owner : 2; var effectiveVariant = variant is 1 or 2 ? variant : 1;
            var expected = new MapTerrainImage(306, null, true); var found = false;
            for (var group = 0; group < 4 && !found; group++) foreach (var entry in groups[group])
            {
                if ((entry.ID >> 24) != terrain || ((entry.ID >> 8) & 255) != effectiveVariant || group == 3 && ((entry.ID >> 16) & 255) != effectiveOwner) continue;
                expected = new(entry.Icon, group == 1 && effectiveOwner >= 2 ? effectiveOwner + 930 : null); found = true; break;
            }
            Expect(MapTerrainImages.For(new((byte)terrain, (byte)owner, (byte)variant)) == expected, "Terrain bitmap/owner-frame/variant/fallback differs from independent C catalogue join.");
        }
        foreach (var width in presets.Concat([2, 17, 64, 512])) foreach (var height in presets.Concat([3, 63, 64, 512]))
        {
            var mark = new MapObjectMark(PlacedThingKind.Object, new(2, 3), 1, 3, 1, false, true, true, false);
            var px = (int)(width * 60f / 100f); var py = (int)(height * 60f / 100f);
            Expect(MapObjectDisplay.Glyph(mark, width, height, new())!.Bounds == new MapObjectBox(2 * width + px, 3 * height, width - px, height - py), "Non-square object geometry scales wrong axis.");
            var sx = (int)(width * 70f / 100f); var sy = (int)(height * 70f / 100f);
            Expect(MapObjectDisplay.Glyph(mark with { Kind = PlacedThingKind.SpecialItem }, width, height, new())!.Bounds
                == new MapObjectBox(2 * width, 3 * height + sy, width - sx, height - sy), "Non-square special geometry scales wrong axis.");
            var dx = (int)((width * 60f / 100f) / 2); var dy = (int)((height * 60f / 100f) / 2);
            var door = MapObjectDisplay.Glyph(mark with { Kind = PlacedThingKind.Door }, width, height, new())!;
            Expect(door.Bounds == new MapObjectBox(2 * width, 3 * height + dy, width, height - 2 * dy)
                && door.Cross == new MapObjectBox(2 * width + dx, 3 * height + dy, width - 2 * dx, height - 2 * dy), "Non-square door/lock geometry differs.");
        }
    }
}
