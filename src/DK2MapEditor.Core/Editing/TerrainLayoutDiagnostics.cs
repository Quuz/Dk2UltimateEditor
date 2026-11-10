using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

/// <summary>Read-only compiler terrain checks. Catalogue identity includes the
/// variant but not the owner or the native reserved byte. Unlike the original
/// compiler, all regions and missing keeper positions are reported in one pass.</summary>
public static class TerrainLayoutDiagnostics
{
    private static readonly LegacyCompilerRules.Element[] entries = LegacyCompilerRules.Elements("Rooms")
        .Where(item => item.ID is 0x0E000101 or 0x0C000101 or 0x28000101)
        .Concat(LegacyCompilerRules.Elements("Gates").Where(item => (byte)(item.ID >> 24) != 0xFE)).ToArray();
    private static readonly IReadOnlyDictionary<int, int> keys = entries.Select((item, index) =>
        new KeyValuePair<int, int>((int)(item.ID >> 24) << 8 | (byte)(item.ID >> 8), index + 1)).ToDictionary();

    public static IReadOnlyList<MapDiagnostic> Validate(NativeMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var width = document.Width; var height = document.Height; var count = checked(width * height);
        var identities = new byte[count]; var owners = new byte[count];
        var widths = new int[count]; var heights = new int[count]; var minimumWidths = new int[count];
        var failures = new byte[count]; var handled = new bool[count];
        var diagnostics = new List<MapDiagnostic>(); var hearts = new HashSet<byte>();
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
        {
            var tile = document.GetTile(x, y); var index = y * width + x;
            identities[index] = (byte)keys.GetValueOrDefault(tile.Terrain << 8 | tile.Variant);
            owners[index] = tile.Owner;
        }
        // Bottom-up row summaries avoid rescanning large malformed regions.
        // Failure order matches TaskTestMapRect: row size, then equal row widths,
        // then height. Error expansion uses the minimum row width, not a bounding
        // box or a flood component (neither matches the original algorithm).
        bool BadWidth(int value, LegacyCompilerRules.Element entry) =>
            value < entry.MinWidth || entry.MaxWidth != 0 && value > entry.MaxWidth;
        for (var y = height - 1; y >= 0; y--) for (var x = width - 1; x >= 0; x--)
        {
            var index = y * width + x; var identity = identities[index];
            if (identity == 0) continue;
            var entry = entries[identity - 1];
            var down = y + 1 < height && identities[index + width] == identity;
            widths[index] = 1 + (x + 1 < width && identities[index + 1] == identity ? widths[index + 1] : 0);
            heights[index] = 1 + (down ? heights[index + width] : 0);
            minimumWidths[index] = down ? Math.Min(widths[index], minimumWidths[index + width]) : widths[index];
            failures[index] = BadWidth(widths[index], entry) ? (byte)1
                : !down ? (byte)0
                : BadWidth(widths[index + width], entry) ? (byte)1
                : widths[index] != widths[index + width] ? (byte)2 : failures[index + width];
        }
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
        {
            var index = y * width + x; var identity = identities[index];
            if (identity == 0 || handled[index]) continue;
            var entry = entries[identity - 1]; var heart = entry.ID == 0x0E000101;
            var owner = owners[index]; var region = new MapDiagnosticRegion(x, y, minimumWidths[index], heights[index]);
            void Add(DiagnosticSeverity severity, string code, string message) => diagnostics.Add(new(severity, code,
                message + $" Terrain={(byte)(entry.ID >> 24)}, variant={(byte)(entry.ID >> 8)}, owner={owner}; ({x}, {y}), {region.Width} × {region.Height}.",
                document.TileFile.SourcePath, region));
            if (heart && !hearts.Add(owner) && owner is not (1 or 2))
                Add(DiagnosticSeverity.Error, "DK2N022", "A keeper has more than one dungeon heart.");
            var failure = failures[index];
            if (failure == 0 && (heights[index] < entry.MinHeight || entry.MaxHeight != 0 && heights[index] > entry.MaxHeight)) failure = 1;
            if (failure == 2) Add(DiagnosticSeverity.Error, "DK2N019", "Heart, portal or gate rows do not form a filled rectangle.");
            else if (failure == 1) Add(heart ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                heart ? "DK2N020" : "DK2N021", $"Terrain dimensions must be {entry.MinWidth}–{(entry.MaxWidth == 0 ? "unbounded" : entry.MaxWidth)} × {entry.MinHeight}–{(entry.MaxHeight == 0 ? "unbounded" : entry.MaxHeight)}.");
            for (var row = y; row < y + region.Height; row++)
                Array.Fill(handled, true, row * width + x, region.Width);
        }
        if (hearts.Count == 0) diagnostics.Add(new(DiagnosticSeverity.Error, "DK2N023", "The map has no dungeon heart.", document.TileFile.SourcePath));
        else
        {
            if (!hearts.Contains(3)) diagnostics.Add(new(DiagnosticSeverity.Error, "DK2N024", "Keeper 1 (owner 3) has no dungeon heart.", document.TileFile.SourcePath));
            for (byte owner = 4; owner <= 6; owner++)
                if (!hearts.Contains(owner) && hearts.Any(value => value > owner && value <= 7))
                    diagnostics.Add(new(DiagnosticSeverity.Error, "DK2N025", $"Keeper {owner - 2} (owner {owner}) is missing before a later keeper.", document.TileFile.SourcePath));
        }
        return diagnostics;
    }
}
