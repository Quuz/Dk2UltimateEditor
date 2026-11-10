using System.Globalization;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class MapOverviewChecks
{
    private static void Expect(bool condition,string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root,"Source"))) root = Path.GetDirectoryName(root) ?? throw new Exception("Original source not found.");
        string Read(string path) => Regex.Replace(File.ReadAllText(Path.Combine(root,"Source",path)),@"//[^\r\n]*|/\*[\s\S]*?\*/","");
        var colourSource = Read("Includes/Thumbnails.h"); var tableSource = Read("Sources/Datas/Datas_Thumbnails.c"); var tileSource = Read("Sources/Datas/Datas_Map.c");
        var colours = new Dictionary<string,uint>(StringComparer.Ordinal);
        var definitions = Regex.Matches(colourSource,@"#define\s+(THUMB_\w+)\s+(\S+)");
        foreach (Match definition in definitions)
        {
            var value = definition.Groups[2].Value;
            if (value.StartsWith("0x",StringComparison.Ordinal)) colours.Add(definition.Groups[1].Value,uint.Parse(value[2..],NumberStyles.HexNumber,CultureInfo.InvariantCulture));
        }
        while (colours.Count != definitions.Count)
        {
            var before = colours.Count;
            foreach (Match definition in definitions)
                if (!colours.ContainsKey(definition.Groups[1].Value) && colours.TryGetValue(definition.Groups[2].Value,out var value)) colours.Add(definition.Groups[1].Value,value);
            Expect(colours.Count > before,"Unresolved original colour alias.");
        }
        uint[] TileIDs(string name) => Regex.Matches(Regex.Match(tileSource,@"ELEMENT\s+" + name + @"\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value,@"\{\s*(0x[\dA-Fa-f]+),")
            .Select(match => uint.Parse(match.Groups[1].Value[2..],NumberStyles.HexNumber,CultureInfo.InvariantCulture)).TakeWhile(value => value != 0).ToArray();
        uint[] ColourIDs(string name) => Regex.Matches(Regex.Match(tableSource,name + @"\[\](?:\[7\])?\s*=\s*\{([\s\S]*?)\};").Groups[1].Value,@"THUMB_\w+").Select(match => colours[match.Value]).ToArray();
        var world = TileIDs("World"); var rooms = TileIDs("Rooms"); var gates = TileIDs("Gates"); var walls = TileIDs("Walls");
        var worldColours = ColourIDs("WorldCols"); var roomColours = ColourIDs("RoomsCols"); var wallColours = ColourIDs("WallsCols");
        Expect(world.Length == 11 && rooms.Length == 23 && gates.Length == 6 && walls.Length == 14
            && worldColours.Length == 11 && roomColours.Length == 161 && wallColours.Length == 14,"Source overview coverage changed.");
        uint Oracle(MapTile tile)
        {
            var player = tile.Owner is 0 or > 7 ? 2 : tile.Owner; var option = tile.Variant is 1 or 2 ? tile.Variant : 1;
            var key = (uint)(tile.Terrain << 24 | option << 8);
            for (var i = 0; i < world.Length; i++) if ((world[i] & 0xFF00FF00) == key) return worldColours[i];
            for (var i = 0; i < rooms.Length; i++) if ((rooms[i] & 0xFF00FF00) == key) return roomColours[i * 7 + player - 1];
            if (gates.Any(gate => (gate & 0xFF00FF00) == key)) return colours["THUMB_HEROPORTAL"];
            key |= (uint)(player << 16);
            for (var i = 0; i < walls.Length; i++) if ((walls[i] & 0xFFFFFF00) == key) return wallColours[i];
            return colours["THUMB_ROCK"];
        }
        var conversion = Read("Sources/Compiler/Compiler_Conversions.c"); var thumbnail = Read("Sources/Thumbnail/Thumbnail_Functions.c");
        Expect(conversion.Contains("GamePlayer = 2;") && conversion.Contains("GameOption = 0x01;")
            && conversion.Contains("Map_TranslateID(0x02,0x01,World)") && thumbnail.Contains("RoomsCols[*LinePtr][(*(LinePtr+1))-1]"),"Original overview conversion fallback unverified.");
        for (var terrain = 0; terrain <= 255; terrain++) for (var owner = 0; owner <= 255; owner++) foreach (var variant in new byte[] { 0,1,2,3,255 })
        {
            var tile = new MapTile((byte)terrain,(byte)owner,variant,(byte)(terrain ^ owner));
            Expect(MapOverviewPalette.ColorRef(tile) == Oracle(tile),$"Original overview colour differs: {tile}.");
            Expect(MapOverviewPalette.ColorRef(tile with { Reserved = 255 }) == Oracle(tile),"Native reserved byte used as an editor discriminator.");
        }
        var directory = Path.Combine(Path.GetTempPath(),"DK2Overview-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false,true })
            {
                var document = NativeMapDocument.CreateNewMap(new("Overview","Overview",128,32,NativeFormat: native));
                var entries = world.Concat(rooms).Concat(gates).Concat(walls).ToArray(); var position = 0;
                foreach (var id in entries) for (byte owner = 1; owner <= 7; owner++)
                {
                    var point = new TilePoint(position % document.Width,2 + position / document.Width);
                    document.Paint([point],new MapTile((byte)(id >> 24),owner,(byte)(id >> 8),(byte)position)); position++;
                }
                document.Paint([new(12,12)],new(255,255,255,255)); document.Undo();
                var before = document.Files.ToDictionary(file => file,file => file.Data.ToArray());
                var canUndo = document.CanUndo; var canRedo = document.CanRedo; var hasChanges = document.HasChanges; var notifications = 0;
                document.Changed += (_,_) => notifications++;
                var pixels = MapOverviewPalette.RenderBgra(document);
                Expect(pixels.Length == document.Width * document.Height * 4,"Overview pixel dimensions incorrect.");
                for (var y = 0; y < document.Height; y++) for (var x = 0; x < document.Width; x++)
                {
                    var value = Oracle(document.GetTile(x,y)); var offset = (y * document.Width + x) * 4;
                    Expect(pixels.AsSpan(offset,4).SequenceEqual(new byte[] { (byte)(value >> 16),(byte)(value >> 8),(byte)value,255 }),"Overview pixel channel/orientation mismatch.");
                }
                foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]),"Overview normalized source bytes.");
                Expect(canUndo == document.CanUndo && canRedo == document.CanRedo && hasChanges == document.HasChanges && notifications == 0,"Read-only overview changed history/events.");
                var reopened = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,native ? "native" : "project")));
                Expect(MapOverviewPalette.RenderBgra(reopened).AsSpan().SequenceEqual(pixels),"Saved/reopened overview changed colours or orientation.");
                document.Resize(132,35,1,1,new(4,2,1)); Expect(MapOverviewPalette.RenderBgra(document).Length == 132 * 35 * 4,"Resize used stale overview dimensions.");
                document.Undo(); Expect(MapOverviewPalette.RenderBgra(document).AsSpan().SequenceEqual(pixels),"Undo overview mismatch.");
            }
        }
        finally { Directory.Delete(directory,true); }
    }
}
