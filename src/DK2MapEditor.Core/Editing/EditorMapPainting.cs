namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Map_CreateLimitWall locks the outermost row/column in both original
    /// loaders. This does not rewrite imported bytes or infer locks from padding.</summary>
    public bool IsEditorProtectedCell(int x, int y)
    {
        CheckPoint(x, y); return x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
    }

    public void PaintEditor(IEnumerable<TilePoint> points, MapTile selected, string description = "Paint")
        => Paint(points.Where(point => point.X > 0 && point.Y > 0 && point.X < Width - 1 && point.Y < Height - 1), EditorTerrainRules.NormalizePaint(selected), description);

    public void FloodFillEditor(TilePoint start, MapTile selected, TileRect? limit = null)
    {
        // The original reader projects game bytes to catalogue cells before
        // Map_FillArea compares them. Native reserved bytes are not that fourth
        // editor discriminator; fixed world/gate owners are projected as well.
        // Cache only terrain/owner/variant, never arbitrary per-cell reserved
        // values, so normal large fills do not repeat catalogue allocation.
        var keys = new Dictionary<MapTile, MapTile>();
        MapTile ComponentKey(MapTile raw)
        {
            var input = raw with { Reserved = 1 };
            if (!keys.TryGetValue(input, out var value)) keys[input] = value = ConversionTile(input);
            return value;
        }
        FloodFillCore(start, EditorTerrainRules.NormalizePaint(selected), limit, protectBoundary: true, ComponentKey);
    }

    private MapTile EditorCutFill(MapTile selected)
    {
        if (!EditorTerrainRules.IsSpecialCutFill(selected)) return EditorTerrainRules.NormalizePaint(selected);
        // Native bundles have no original-editor FillID preference; their new-map
        // default is neutral earth. Project metadata retains the exact preference.
        var preset = Records().FirstOrDefault(record => record.Category.StartsWith("Project.MAPPROPERTIES", StringComparison.Ordinal)) is { } info
            && info.Fields.FirstOrDefault(field => field.Name == "FillID") is { } field
            ? unchecked((uint)long.Parse(info.Read(field))) : 0x02020101u;
        return EditorTerrainRules.DecodeFillPreset(preset);
    }
}
