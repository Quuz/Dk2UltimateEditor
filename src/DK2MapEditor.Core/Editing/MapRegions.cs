using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public enum MapRegionKind { Area, Gate, Effect }
public sealed record MapRegion(NativeRecord Record, MapRegionKind Kind, TileRect Bounds, uint Color);

public sealed partial class NativeMapDocument
{
    private static MapRegionKind? MapRegionKindOf(NativeRecord record) => record.Category switch
    {
        "GAMEAREA" => MapRegionKind.Area, "GAMEGATE" => MapRegionKind.Gate, "GAMEEFFECT" => MapRegionKind.Effect,
        "Project.MAPRECTEX" => record.Read(record.Fields.Single(field => field.Name == "node.type")) switch
        { "0" => MapRegionKind.Area, "1" => MapRegionKind.Gate, "2" => MapRegionKind.Effect, _ => null },
        _ => null
    };

    /// <summary>Read-only geometry. Endpoints are inclusive; native effect sizes
    /// are endpoint deltas, so zero width/height means one tile, not an empty area.</summary>
    public MapRegion? GetMapRegion(NativeRecord record)
    {
        if (!files.Contains(record.File) || record.Offset < 0 || record.Length < 0 || record.Offset > record.File.Length - record.Length) return null;
        string? Read(string name) => record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? record.Read(field) : null;
        bool Number(string name, out int value) => int.TryParse(Read(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        if (MapRegionKindOf(record) is not { } kind) return null;
        string startX, startY, endX, endY;
        if (record.Category == "Project.MAPRECTEX")
        {
            (startX, startY, endX, endY) = ("rect.x1", "rect.y1", "rect.x2", "rect.y2");
        }
        else
        {
            (startX, startY, endX, endY) = kind switch {
                MapRegionKind.Area => ("dwAreaLocationStartX", "dwAreaLocationStartY", "dwAreaLocationEndX", "dwAreaLocationEndY"),
                MapRegionKind.Gate => ("dwGateLocationX", "dwGateLocationY", "", ""),
                _ => ("dwEffectStartX", "dwEffectStartY", "wEffectWidth", "wEffectHeight") };
        }
        if (!Number(startX, out var x) || !Number(startY, out var y)) return null;
        long lastX = x, lastY = y;
        if (kind != MapRegionKind.Gate)
        {
            if (!Number(endX, out var ex) || !Number(endY, out var ey)) return null;
            lastX = record.Category == "GAMEEFFECT" ? (long)x + ex : ex;
            lastY = record.Category == "GAMEEFFECT" ? (long)y + ey : ey;
        }
        var left = Math.Min(x, lastX); var top = Math.Min(y, lastY);
        var width = Math.Abs(lastX - x) + 1; var height = Math.Abs(lastY - y) + 1;
        if (left < int.MinValue || left > int.MaxValue || top < int.MinValue || top > int.MaxValue || width > int.MaxValue || height > int.MaxValue) return null;
        var color = kind switch { MapRegionKind.Area => 0x00FFFF00u, MapRegionKind.Gate => 0x00AAAAAAu, _ => 0x007F00FFu };
        if (uint.TryParse(Read("rect.color"), out var custom)) color = custom;
        return new(record, kind, new((int)left, (int)top, (int)width, (int)height), color);
    }

    public IReadOnlyList<MapRegion> MapRegions() => Records().Select(GetMapRegion).OfType<MapRegion>().ToArray();

    public void SetMapRegionBounds(NativeRecord record, TileRect bounds, ReadOnlyMemory<byte>? expectedPayload = null)
    {
        record = Records().SingleOrDefault(current => current.File == record.File && current.Offset == record.Offset && current.Length == record.Length && current.Category == record.Category)
            ?? throw new ArgumentException("Select a current verified map region.", nameof(record));
        if (MapRegionKindOf(record) is not { } kind) throw new ArgumentException("Select a verified map region kind.", nameof(record));
        if (expectedPayload is { } expected && !record.File.Data.Span.Slice(record.Offset, record.Length).SequenceEqual(expected.Span))
            throw new InvalidOperationException("The region changed while its geometry was being chosen.");
        // Explicitly chosen bounds may repair malformed imported coordinates.
        // Reading/rendering never makes this repair automatically.
        ValidateMapRegionBounds(kind, bounds);
        Edit("Change map region bounds", () => MutateNativeGate(record, () =>
        {
            void Set(string name, int value) => SetField(record, record.Fields.Single(field => field.Name == name), value.ToString(CultureInfo.InvariantCulture));
            var endX = bounds.X + bounds.Width - 1; var endY = bounds.Y + bounds.Height - 1;
            if (record.Category == "Project.MAPRECTEX")
            {
                Set("rect.x1", bounds.X); Set("rect.y1", bounds.Y); Set("rect.x2", endX); Set("rect.y2", endY);
            }
            else if (kind == MapRegionKind.Area)
            {
                Set("dwAreaLocationStartX", bounds.X); Set("dwAreaLocationStartY", bounds.Y); Set("dwAreaLocationEndX", endX); Set("dwAreaLocationEndY", endY);
            }
            else if (kind == MapRegionKind.Effect)
            {
                Set("dwEffectStartX", bounds.X); Set("dwEffectStartY", bounds.Y); Set("wEffectWidth", bounds.Width - 1); Set("wEffectHeight", bounds.Height - 1);
            }
            else { Set("dwGateLocationX", bounds.X); Set("dwGateLocationY", bounds.Y); }
        }));
    }

    public NativeRecord AddMapRegion(MapRegionKind kind, TileRect bounds)
    {
        ValidateMapRegionBounds(kind, bounds);
        NativeRecord? added = null;
        Edit("Add map region", () =>
        {
            added = IsLegacyProject ? AddLegacyRecord(kind.ToString(), new(bounds.X, bounds.Y), 1)
                : AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAME" + kind.ToString().ToUpperInvariant()), new(bounds.X, bounds.Y), 1);
            if (IsLegacyProject)
            {
                var color = kind switch { MapRegionKind.Area => 0xFFFF00, MapRegionKind.Gate => 0xAAAAAA, _ => 0x7F00FF };
                foreach (var name in new[] { "rect.color", "rect.textcolor" }) SetField(added, added.Fields.Single(field => field.Name == name), color.ToString(CultureInfo.InvariantCulture));
            }
            else if (kind == MapRegionKind.Gate)
            {
                var (type, orientation) = HeroGateTerrainInfo(bounds.X, bounds.Y);
                foreach (var (name, value) in new[] { ("bGateID", 1), ("bGateType", type), ("bGateOrientation", orientation) })
                    SetField(added, added.Fields.Single(field => field.Name == name), value.ToString(CultureInfo.InvariantCulture));
            }
            SetMapRegionBounds(added, bounds);
        });
        return added!;
    }

    // Compiler_InfoGateType / Things_IsSolid. Only the two verified source
    // gate-37 variants select type 37; an arbitrary/unknown variant falls back.
    private (int Type, int Orientation) HeroGateTerrainInfo(int x, int y)
    {
        var tile = GetTile(x, y);
        if (tile.Terrain != 37 || tile.Variant is not (1 or 2)) return (33, 0);
        bool Solid(int px, int py) => px < 0 || px >= Width || py < 0 || py >= Height || GetTile(px, py).Terrain is 1 or 2 or 6 or 7 or 9 or 30 or 254;
        return (37, tile.Variant == 1 ? Solid(x, y + 1) ? 0 : 4 : Solid(x + 1, y) ? 6 : 2);
    }

    private void ValidateMapRegionBounds(MapRegionKind kind, TileRect bounds)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (bounds.X < 0 || bounds.Y < 0 || bounds.Width < 1 || bounds.Height < 1 || bounds.X >= Width || bounds.Y >= Height
            || bounds.Width > Width - bounds.X || bounds.Height > Height - bounds.Y) throw new ArgumentOutOfRangeException(nameof(bounds));
        if (kind == MapRegionKind.Gate && (bounds.Width != 1 || bounds.Height != 1)) throw new ArgumentException("Gate regions are points, not rectangles.", nameof(bounds));
    }
}
