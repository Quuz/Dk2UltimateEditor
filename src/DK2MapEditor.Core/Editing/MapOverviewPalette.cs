using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

/// <summary>Read-only original Thumbnail_Generate colours after the original
/// game-to-editor tile lookup. The native reserved byte is not a discriminator.</summary>
public static class MapOverviewPalette
{
    private static readonly uint[,,] colours = BuildColours();

    /// <returns>Original Windows COLORREF (0x00BBGGRR).</returns>
    public static uint ColorRef(MapTile tile)
        => colours[tile.Terrain, tile.Owner is >= 1 and <= 7 ? tile.Owner : 2, tile.Variant is 1 or 2 ? tile.Variant - 1 : 0];

    /// <summary>One top-down BGRA pixel per map cell. Does not mutate map data,
    /// normalize unknown tiles, include objects or create history.</summary>
    public static byte[] RenderBgra(NativeMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var width = document.Width; var height = document.Height;
        var pixels = new byte[checked(width * height * 4)];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
        {
            var colour = ColorRef(document.GetTile(x,y)); var offset = (y * width + x) * 4;
            pixels[offset] = (byte)(colour >> 16); pixels[offset + 1] = (byte)(colour >> 8);
            pixels[offset + 2] = (byte)colour; pixels[offset + 3] = 255;
        }
        return pixels;
    }

    private static uint[,,] BuildColours()
    {
        string Read(string name)
        {
            using var stream = typeof(MapOverviewPalette).Assembly.GetManifestResourceStream("Legacy." + name)
                ?? throw new InvalidDataException("Missing original overview colour source: " + name);
            using var reader = new StreamReader(stream);
            return Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        }
        var definitions = Regex.Matches(Read("Thumbnails.h"), @"#define\s+(THUMB_\w+)\s+(0x[\dA-Fa-f]+|THUMB_\w+)")
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value, StringComparer.Ordinal);
        uint Resolve(string name)
        {
            var value = definitions[name];
            return value.StartsWith("0x",StringComparison.Ordinal) ? uint.Parse(value[2..],NumberStyles.HexNumber,CultureInfo.InvariantCulture) : Resolve(value);
        }
        var tables = Read("Datas_Thumbnails.c");
        uint[] Table(string name) => Regex.Matches(Regex.Match(tables, name + @"\[\](?:\[7\])?\s*=\s*\{([\s\S]*?)\};").Groups[1].Value, @"THUMB_\w+")
            .Select(match => Resolve(match.Value)).ToArray();
        var world = LegacyCompilerRules.Elements("World"); var rooms = LegacyCompilerRules.Elements("Rooms");
        var gates = LegacyCompilerRules.Elements("Gates"); var walls = LegacyCompilerRules.Elements("Walls");
        var worldColours = Table("WorldCols"); var roomColours = Table("RoomsCols"); var wallColours = Table("WallsCols");
        if (worldColours.Length != world.Count || roomColours.Length != rooms.Count * 7 || wallColours.Length != walls.Count)
            throw new InvalidDataException("Original overview palette/catalogue lengths disagree.");
        int Find(IReadOnlyList<LegacyCompilerRules.Element> entries,uint key,uint mask)
        {
            for (var i = 0; i < entries.Count; i++) if ((entries[i].ID & mask) == key) return i;
            return -1;
        }
        var result = new uint[256,8,2];
        for (var terrain = 0; terrain < 256; terrain++) for (var owner = 1; owner <= 7; owner++) for (var variant = 1; variant <= 2; variant++)
        {
            var key = (uint)(terrain << 24 | variant << 8); var worldIndex = Find(world,key,0xFF00FF00);
            var roomIndex = Find(rooms,key,0xFF00FF00); var wallIndex = Find(walls,key | (uint)(owner << 16),0xFFFFFF00);
            result[terrain,owner,variant - 1] = worldIndex >= 0 ? worldColours[worldIndex]
                : roomIndex >= 0 ? roomColours[roomIndex * 7 + owner - 1]
                : Find(gates,key,0xFF00FF00) >= 0 ? Resolve("THUMB_HEROPORTAL")
                : wallIndex >= 0 ? wallColours[wallIndex]
                : Resolve("THUMB_ROCK"); // Original unknown-terrain display fallback only.
        }
        return result;
    }
}
