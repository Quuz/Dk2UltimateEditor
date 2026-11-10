using System.Globalization;

namespace DK2MapEditor.Core.Editing;

[Flags]
public enum MapOverlayFlags : uint
{
    None = 0, Areas = 64, Gates = 128, Paths = 256, Effects = 512, Cameras = 1024, ExpandedGates = 131072
}
public sealed record MapOverlayOptions(MapOverlayFlags Flags = MapOverlayDisplay.DefaultFlags, uint PathColour = 0xFFFFFF);
public sealed record MapCameraMark(uint PlayerID, TilePoint Position, int Overlaps);
public sealed record MapCameraGlyph(int Resource, MapObjectBox Icon, MapObjectBox Ellipse, uint Colour);
public sealed record MapRegionMark(MapRegionKind Kind, TileRect Bounds, TileRect ExpandedBounds,
    string? Name, uint DisplayID, uint Colour, uint TextColour, bool Hidden);
public sealed record MapPathMark(TileRect Start, TileRect End, bool Loop);
public sealed record MapPathGlyph(double X1, double Y1, double X2, double Y2, bool Loop);

/// <summary>Immutable read-only source overlay projection. Stored cameras are
/// deliberately distinct from compiler-resolved default/heart positions.</summary>
public sealed class MapOverlayDisplay
{
    public const MapOverlayFlags DefaultFlags = MapOverlayFlags.Areas | MapOverlayFlags.Gates | MapOverlayFlags.Paths | MapOverlayFlags.Effects | MapOverlayFlags.Cameras;
    public IReadOnlyList<MapCameraMark> Cameras { get; }
    public IReadOnlyList<MapRegionMark> Regions { get; }
    public IReadOnlyList<MapPathMark> Paths { get; }
    private MapOverlayDisplay(List<MapCameraMark> cameras, List<MapRegionMark> regions, List<MapPathMark> paths)
    { Cameras = cameras.AsReadOnly(); Regions = regions.AsReadOnly(); Paths = paths.AsReadOnly(); }
    public static MapOverlayDisplay Capture(NativeMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var players = document.PlayerGeometries().Where(row => row.CameraEditable && row.ID is >= 3 and <= 7
            && row.Camera.X >= 0 && row.Camera.Y >= 0 && row.Camera.X < document.Width && row.Camera.Y < document.Height).OrderBy(row => row.ID).ToArray();
        var cameras = players.Select(row => new MapCameraMark(row.ID, row.Camera, players.Count(other => other.ID != row.ID && other.Camera == row.Camera))).ToList();
        var regions = new List<MapRegionMark>(); var gateOrdinal = 0;
        foreach (var region in document.MapRegions())
        {
            var record = region.Record;
            string? Read(string name) => record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? record.Read(field) : null;
            uint Number(string name, uint fallback = 0) => uint.TryParse(Read(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
            // Files_Game consumes heart gates into player health, not MapGates.
            if (record.Category == "GAMEGATE" && Number("bGateType") == 14) continue;
            var bounds = region.Bounds; var expanded = bounds;
            if (region.Kind == MapRegionKind.Gate)
            {
                gateOrdinal++;
                var x = document.IsLegacyProject ? int.Parse(Read("rect.x1")!, CultureInfo.InvariantCulture) : bounds.X;
                var y = document.IsLegacyProject ? int.Parse(Read("rect.y1")!, CultureInfo.InvariantCulture) : bounds.Y;
                bounds = new(x, y, 1, 1); expanded = ExpandGate(document, new(x, y));
            }
            var id = document.IsLegacyProject ? Number("id") : region.Kind switch
            { MapRegionKind.Area => Number("bAreaID"), MapRegionKind.Gate => (uint)gateOrdinal, _ => Number("bEffectID") };
            regions.Add(new(region.Kind, bounds, expanded, Read("name"), id, region.Color & 0xFFFFFF,
                Number("rect.textcolor", region.Color) & 0xFFFFFF, Number("rect.hidden") != 0));
        }
        var points = document.PatrolAreas();
        var unique = points.GroupBy(point => point.ID).Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single());
        var paths = new List<MapPathMark>();
        foreach (var point in points)
            if (point.NextID != 0 && unique.ContainsKey(point.ID) && point.Region is { } start
                && unique.TryGetValue(point.NextID, out var next) && next.Region is { } end)
                paths.Add(new(start.Bounds, end.Bounds, point.NextID == point.ID));
        // MainWindowProc paints Effects, Gates, Areas, then Paths and Cameras.
        return new(cameras, regions.OrderBy(region => region.Kind == MapRegionKind.Effect ? 0 : region.Kind == MapRegionKind.Gate ? 1 : 2).ToList(), paths);
    }
    public static MapOverlayFlags Flag(MapRegionKind kind) => kind switch
    { MapRegionKind.Area => MapOverlayFlags.Areas, MapRegionKind.Gate => MapOverlayFlags.Gates, _ => MapOverlayFlags.Effects };
    public static TileRect ExpandGate(NativeMapDocument document, TilePoint origin)
    {
        if (origin.X < 0 || origin.Y < 0 || origin.X >= document.Width || origin.Y >= document.Height) return new(origin.X, origin.Y, 1, 1);
        var tile = document.GetTile(origin.X, origin.Y);
        // Source compares the editor Gates catalogue slot, including orientation,
        // but not owner. Never turn the unsafe source edge reads into row wraps.
        if (!LegacyCompilerRules.Elements("Gates").Any(entry => (byte)(entry.ID >> 24) == tile.Terrain && (byte)(entry.ID >> 8) == tile.Variant)) return new(origin.X, origin.Y, 1, 1);
        bool Matches(int x, int y) { var other = document.GetTile(x, y); return other.Terrain == tile.Terrain && other.Variant == tile.Variant; }
        var x1 = origin.X; var x2 = x1; var y1 = origin.Y; var y2 = y1;
        while (x1 > 0 && Matches(x1 - 1, y1)) x1--;
        while (x2 + 1 < document.Width && Matches(x2 + 1, y1)) x2++;
        while (y1 > 0 && Matches(x1, y1 - 1)) y1--;
        while (y2 + 1 < document.Height && Matches(x2, y2 + 1)) y2++;
        return new(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
    }
    public static MapCameraGlyph CameraGlyph(MapCameraMark camera, double width, double height)
    {
        Validate(width, height);
        if (camera.PlayerID is < 3 or > 7 || camera.Overlaps is < 0 or > 4) throw new ArgumentOutOfRangeException(nameof(camera));
        var index = (int)camera.PlayerID - 3;
        var x = camera.Position.X * width + Math.Truncate((width - 16) / 2);
        var y = camera.Position.Y * height + Math.Truncate((height - 16) / 2);
        var offset = camera.Overlaps == 0 ? 0 : (camera.Overlaps - index) * 2;
        return new(20 + index, new(x - offset, y - offset, 16, 16),
            new(x - width * 2 + 8 - offset, y - height * 2 + 8 - offset, width * 4 + offset * 2, height * 4 + offset * 2), MapObjectDisplay.OwnerColour((byte)camera.PlayerID));
    }
    public static MapPathGlyph PathGlyph(MapPathMark path, double width, double height)
    {
        Validate(width, height);
        var a = path.Start; var b = path.End;
        if (path.Loop)
        {
            var dx = Math.Truncate(a.Width * width / 9); var dy = Math.Truncate(a.Height * height / 9);
            return new(a.X * width + dx, a.Y * height + dy, ((double)a.X + a.Width) * width - dx, ((double)a.Y + a.Height) * height - dy, true);
        }
        return new(a.X * width + Math.Truncate(a.Width * width / 2), a.Y * height + Math.Truncate(a.Height * height / 2),
            b.X * width + Math.Truncate(b.Width * width / 2), b.Y * height + Math.Truncate(b.Height * height / 2), false);
    }
    private static void Validate(double width, double height)
    { if (!MapZoom.Valid(width) || !MapZoom.Valid(height)) throw new ArgumentOutOfRangeException(nameof(width)); }
}
