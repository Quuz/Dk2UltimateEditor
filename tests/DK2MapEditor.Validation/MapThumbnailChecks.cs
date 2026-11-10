using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class MapThumbnailChecks
{
    private static void Expect(bool condition,string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root,"Source"))) root = Path.GetDirectoryName(root) ?? throw new Exception("Source not found.");
        string Read(string path) => Regex.Replace(File.ReadAllText(Path.Combine(root,"Source",path)),@"//[^\r\n]*|/\*[\s\S]*?\*/","");
        var functions = Read("Sources/Thumbnail/Thumbnail_Functions.c"); var config = Read("Sources/Properties/Editor/Properties_Editor_Config.c");
        Expect(functions.Contains("if (ThumbSize == 144) ThumbSize = 128;") && functions.Contains("InfoHeader.biBitCount = 24;")
            && functions.Contains("GetPixel(hWorkDC,X,ThumbSize-Y-1)") && functions.Contains("Config.MinimapKeepProps")
            && functions.Contains("if (Color == ColorKey) continue;"),"Original size/format/key behavior not verified.");
        Expect(config.Contains("ZeroMemory(&Config,sizeof(Config))") && config.Contains("Config.MinimapKeepProps = TRUE;")
            && config.Contains("Config.Thumbnail.WorldFlags = 0xFFFFFFFF;") && config.Contains("Config.Thumbnail.PlayersFlags = 0xFFFFFFFF;"),"Original option defaults changed.");
        Expect(functions.Contains("Options->WorldFlags&TOF_SHOWHEROESHEARTS") && Read("Sources/Windows/Windows_Thumbnail.c").Contains("TOF_SHOWHEROESHEARTS"),"Historical hero-heart wrong-word defect not reproduced.");
        var flags = Regex.Matches(Read("Includes/Config.h"),@"#define\s+(TOF_\w+)\s+(0x[\dA-Fa-f]+)")
            .ToDictionary(match => match.Groups[1].Value,match => uint.Parse(match.Groups[2].Value[2..],NumberStyles.HexNumber,CultureInfo.InvariantCulture));
        var worldNames = new[] { "LIMIT","WATER","LAVA","GOLD","GEMS","SOLID","ROCK","UNCLAIMED","HEROESREST" };
        Expect(MapThumbnail.WorldChoices.Count == 9 && MapThumbnail.PlayerCategories.Count == 4,"Thumbnail control count differs.");
        for (var i = 0; i < 9; i++) Expect(MapThumbnail.WorldChoices[i].Mask == flags["TOF_SHOW" + worldNames[i]] && !MapThumbnail.WorldChoices[i].PlayerScoped,"World checkbox mask differs from source.");
        var ownerNames = new[] { "HEROES","NEUTRAL","KEEPER1","KEEPER2","KEEPER3","KEEPER4","KEEPER5" };
        var categoryNames = new[] { "WALLS","ROOMS","PORTALS","HEARTS" };
        for (var owner = 1; owner <= 7; owner++) for (var column = 0; column < 4; column++)
        {
            var key = "TOF_SHOW" + ownerNames[owner - 1] + categoryNames[column]; if (owner > 2 && column == 3) key = key[..^1];
            Expect(MapThumbnail.PlayerChoices(owner)[column].Mask == flags[key],"Owner checkbox mask differs from original.");
        }
        var tileSource = Read("Sources/Datas/Datas_Map.c");
        uint[] Entries(string group) => Regex.Matches(Regex.Match(tileSource,@"ELEMENT\s+" + group + @"\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value,@"\{\s*(0x[\dA-Fa-f]+),")
            .Select(match => uint.Parse(match.Groups[1].Value[2..],NumberStyles.HexNumber,CultureInfo.InvariantCulture)).Where(value => value != 0).ToArray();
        var catalogues = new[] { "World","Rooms","Gates","Walls" }.Select(Entries).ToArray();
        var worldTerrain = new byte[] { 30,4,5,6,7,1,2,3,35 };
        var cases = new List<MapThumbnailOptions> { new(),new(0,0,0x123456),new(HiddenColour: 0x123456) };
        foreach (var flag in MapThumbnail.WorldChoices) { cases.Add(new(~flag.Mask,uint.MaxValue,0x123456)); cases.Add(new(flag.Mask,0,0x123456)); }
        for (var owner = 1; owner <= 7; owner++) foreach (var flag in MapThumbnail.PlayerChoices(owner)) { cases.Add(new(uint.MaxValue,~flag.Mask,0x123456)); cases.Add(new(0,flag.Mask,0x123456)); }
        uint Oracle(MapTile tile,MapThumbnailOptions options)
        {
            var owner = tile.Owner is >= 1 and <= 7 ? tile.Owner : 2; var variant = tile.Variant is 1 or 2 ? tile.Variant : 1;
            var key = (uint)(tile.Terrain << 24 | variant << 8); var group = -1;
            for (var i = 0; i < 4 && group == -1; i++) if (catalogues[i].Any(id => i == 3 ? (id & 0xFFFFFF00) == (key | (uint)(owner << 16)) : (id & 0xFF00FF00) == key)) group = i;
            uint mask; bool player;
            if (group == 0 && tile.Terrain == 31) { player = true; mask = flags["TOF_SHOWNEUTRALROOMS"]; }
            else if (group == 0) { player = false; mask = flags["TOF_SHOW" + worldNames[Array.IndexOf(worldTerrain,tile.Terrain == 254 ? (byte)30 : tile.Terrain)]]; }
            else if (group == 2) { player = true; mask = flags["TOF_SHOWHEROESPORTALS"]; }
            else if (group is 1 or 3)
            {
                player = true; var category = group == 3 ? 0 : tile.Terrain == 14 ? 3 : tile.Terrain is 12 or 40 ? 2 : 1;
                var name = "TOF_SHOW" + ownerNames[owner - 1] + categoryNames[category]; if (owner > 2 && category == 3) name = name[..^1]; mask = flags[name];
            }
            else { player = false; mask = flags["TOF_SHOWROCK"]; }
            return ((player ? options.PlayersFlags : options.WorldFlags) & mask) != 0 ? MapOverviewPalette.ColorRef(tile) : options.HiddenColour;
        }
        for (var terrain = 0; terrain < 256; terrain++) foreach (var owner in new byte[] { 0,1,2,3,4,5,6,7,255 }) foreach (var variant in new byte[] { 0,1,2,3,255 })
            foreach (var options in cases)
            {
                var tile = new MapTile((byte)terrain,owner,variant,255);
                Expect(MapThumbnail.Colour(tile,options) == Oracle(tile,options),$"Thumbnail filter mismatch: {tile}; {options}.");
            }
        // Intended named heart control, not the preserved wrong WorldFlags word.
        var heroHeart = new MapTile(14,1,1); var hidden = 0x123456u;
        Expect(MapThumbnail.Colour(heroHeart,new(~flags["TOF_SHOWUNCLAIMED"],uint.MaxValue,hidden)) == MapOverviewPalette.ColorRef(heroHeart),"Hero heart incorrectly coupled to unclaimed flag.");
        Expect(MapThumbnail.Colour(heroHeart,new(uint.MaxValue,~flags["TOF_SHOWHEROESHEARTS"],hidden)) == hidden,"Hero heart checkbox remains ineffective.");
        var originalBackground = File.ReadAllBytes(Path.Combine(root,"Source/Bitmaps/Interface/Carte - Fond.bmp"));
        uint Background(int x,int y,int size)
        {
            var bx = x * 144 / size; var by = y * 144 / size; var offset = 54 + ((143 - by) * 144 + bx) * 3;
            return (uint)(originalBackground[offset + 2] | originalBackground[offset + 1] << 8 | originalBackground[offset] << 16);
        }
        byte[] Pixel(uint colour) => [(byte)(colour >> 16),(byte)(colour >> 8),(byte)colour,255];
        foreach (var dims in new[] { (W:3,H:2),(W:2,H:3),(W:3,H:3),(W:1,H:1024),(W:1024,H:1) })
        {
            var pixels = new byte[dims.W * dims.H * 4];
            for (var y = 0; y < dims.H; y++) for (var x = 0; x < dims.W; x++) Pixel((x + y) % 3 == 0 ? hidden : 0x112233u + (uint)(x + y)).CopyTo(pixels,(y * dims.W + x) * 4);
            foreach (var previewSize in new[] { 96,144 }) foreach (var keep in new[] { false,true }) foreach (var colourKey in new[] { false,true }) foreach (var export in new[] { false,true })
            {
                var options = new MapThumbnailOptions(HiddenColour: hidden,PreviewSize: previewSize,KeepProportions: keep,UseColourKey: colourKey);
                var composed = MapThumbnail.Compose(new(dims.W,dims.H,pixels),options,export); var size = export ? (previewSize == 144 ? 128 : 96) : previewSize;
                Expect(composed.Width == size && composed.Height == size,"Thumbnail preview/export size differs.");
                var w = keep && dims.W < dims.H ? Math.Max(1,size * dims.W / dims.H) : size; var h = keep && dims.W > dims.H ? Math.Max(1,size * dims.H / dims.W) : size;
                var left = (size - w) / 2; var top = (size - h) / 2;
                for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
                {
                    var expected = Pixel(Background(x,y,size));
                    if (x >= left && x < left + w && y >= top && y < top + h)
                    {
                        var p = ((y - top) * dims.H / h * dims.W + (x - left) * dims.W / w) * 4;
                        var colour = (uint)(pixels[p + 2] | pixels[p + 1] << 8 | pixels[p] << 16);
                        if (!colourKey || colour != hidden) expected = pixels.AsSpan(p,4).ToArray();
                    }
                    Expect(composed.Bgra.Span.Slice((y * size + x) * 4,4).SequenceEqual(expected),"Thumbnail aspect/key/background nearest-neighbour projection differs.");
                }
                var bmp = MapThumbnail.EncodeBmp(composed);
                Expect(bmp[0] == 'B' && bmp[1] == 'M' && BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(2)) == bmp.Length && BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10)) == 54
                    && BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(14)) == 40 && BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(18)) == size && BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22)) == size
                    && BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(26)) == 1 && BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(28)) == 24 && bmp.AsSpan(38,16).IndexOfAnyExcept((byte)0) < 0,"Original BMP header differs.");
                for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
                    Expect(bmp.AsSpan(54 + ((size - 1 - y) * size + x) * 3,3).SequenceEqual(composed.Bgra.Span.Slice((y * size + x) * 4,3)),"BMP bottom-up BGR channels differ.");
            }
        }
        foreach (var bad in new[] { new MapThumbnailOptions(PreviewSize: 128),new MapThumbnailOptions(HiddenColour: 0x1000000) })
            try { MapThumbnail.Compose(new(1,1,new byte[4]),bad); throw new Exception("Invalid thumbnail option accepted."); } catch (ArgumentOutOfRangeException) { }
        try { MapThumbnail.Compose(new(2,1,new byte[4]),new()); throw new Exception("Truncated raster accepted."); } catch (ArgumentException) { }
        var directory = Path.Combine(Path.GetTempPath(),"DK2Thumbnail-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false,true })
            {
                var doc = NativeMapDocument.CreateNewMap(new("Thumbnail","Thumbnail",32,16,NativeFormat: native));
                var point = 0;
                foreach (var id in catalogues.SelectMany(group => group)) for (byte owner = 1; owner <= 7; owner++)
                { doc.Paint([new(point % 32,point / 32)],new((byte)(id >> 24),owner,(byte)(id >> 8))); point++; }
                doc.Paint([new(20,12)],new(255,255,255)); doc.Undo();
                var before = doc.Files.ToDictionary(file => file,file => file.Data.ToArray()); var undo = doc.CanUndo; var redo = doc.CanRedo; var dirty = doc.HasChanges; var notifications = 0;
                doc.Changed += (_,_) => notifications++;
                var options = new MapThumbnailOptions(WorldFlags: 0x101,PlayersFlags: 0x8ABCDE00,HiddenColour: hidden,UseColourKey: true);
                var raster = MapThumbnail.Filter(doc,options);
                for (var y = 0; y < doc.Height; y++) for (var x = 0; x < doc.Width; x++) Expect(raster.Bgra.Span.Slice((y * doc.Width + x) * 4,4).SequenceEqual(Pixel(Oracle(doc.GetTile(x,y),options))),"Actual document filtering differs.");
                var path = Path.Combine(directory,native ? "native.bmp" : "project.bmp"); MapThumbnail.SaveNewBmp(path,doc,options); var bytes = File.ReadAllBytes(path);
                Expect(bytes.AsSpan().SequenceEqual(MapThumbnail.Bmp(doc,options)),"Published BMP differs.");
                try { MapThumbnail.SaveNewBmp(path,doc,options); throw new Exception("Existing thumbnail overwritten."); } catch (IOException) { }
                Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes) && !Directory.EnumerateFiles(directory,".dk2-thumbnail-*").Any(),"Failed export changed existing file or retained staging.");
                try { MapThumbnail.SaveNewBmp(Path.Combine(directory,"missing","image.bmp"),doc,options); throw new Exception("Missing parent accepted."); } catch (DirectoryNotFoundException) { }
                var reopened = NativeMapDocument.Load(doc.SaveCopy(Path.Combine(directory,native ? "native" : "project")));
                Expect(MapThumbnail.Bmp(reopened,options).AsSpan().SequenceEqual(bytes),"Thumbnail changed after complete document save/reopen.");
                foreach (var file in doc.Files) Expect(file.Data.Span.SequenceEqual(before[file]),"Thumbnail normalized source bytes.");
                Expect(undo == doc.CanUndo && redo == doc.CanRedo && dirty == doc.HasChanges && notifications == 0,"Thumbnail changed history/events.");
            }
        }
        finally { Directory.Delete(directory,true); }
    }
}
