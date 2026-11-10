using System.Globalization;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class FullMapOverviewChecks
{
    private static void Expect(bool condition,string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root,"Source"))) root = Path.GetDirectoryName(root) ?? throw new Exception("Source not found.");
        string Source(string path) => Regex.Replace(File.ReadAllText(Path.Combine(root,"Source",path)),@"//[^\r\n]*|/\*[\s\S]*?\*/","");
        var original = Source("Sources/Windows/Windows_Fullmap.c"); var init = Source("Sources/Editor.c");
        Expect(original.Contains("SetTimer(hWnd,1,100,NULL)") && original.Contains("FullNeutralDelay = -1;")
            && original.Contains("FullRoomOffset++;") && init.Contains("FullRoomOffset    = 2;"),"Source fullscreen timing/init changed.");
        Expect(original.Contains("WallsCols[(Trap->owner-1)*2+FullTrapsOffset]")
            && original.Contains("WallsCols[(Door->owner-1)*2+((FullDoorsOffset)?0:1)]")
            && original.Contains("WallsCols[(Creature->owner-1)*2]") && original.Contains("PaintRect.right = PaintRect.left+3;"),"Source entity shape/colour evidence changed.");
        var definitions = Regex.Matches(Source("Includes/Thumbnails.h"),@"#define\s+(THUMB_\w+)\s+(\S+)")
            .ToDictionary(match => match.Groups[1].Value,match => match.Groups[2].Value);
        uint Colour(string name) => definitions[name] is var value && value.StartsWith("0x",StringComparison.Ordinal)
            ? uint.Parse(value[2..],NumberStyles.HexNumber,CultureInfo.InvariantCulture) : Colour(value);
        var wallColours = Regex.Matches(Regex.Match(Source("Sources/Datas/Datas_Thumbnails.c"),@"WallsCols\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value,@"THUMB_\w+").Select(match => Colour(match.Value)).ToArray();
        Expect(wallColours.Length == 14,"Source wall-colour count changed.");
        var traps = 0; var doors = 0; var neutral = 0; var rooms = 2; var delay = 0;
        for (var tick = 0; tick <= 112; tick++)
        {
            var phase = FullMapAnimation.At(tick); Expect(phase == new FullMapAnimation(traps,doors,neutral,rooms),"Fullscreen animation differs from original state machine.");
            Expect(phase.RoomColour == wallColours[rooms * 2],"Neutral-room colour differs from source.");
            foreach (var kind in Enum.GetValues<FullMapMarkerKind>()) for (byte owner = 0; owner <= 8; owner++)
            {
                var effective = owner is >= 1 and <= 7 ? owner : (byte)2;
                var index = effective == 2 ? neutral * 2 : (effective - 1) * 2 + (kind == FullMapMarkerKind.Trap ? traps : kind == FullMapMarkerKind.Door ? (doors == 0 ? 1 : 0) : 0);
                Expect(phase.Colour(new(kind,new(0,0),owner)) == wallColours[index],"Fullscreen marker phase/owner mismatch.");
            }
            traps = 1 - traps;
            if (delay != 0) { doors = 1 - doors; neutral = (neutral + 1) % 7; rooms = (rooms + 1) % 7; delay = -1; }
            delay++;
        }
        try { FullMapAnimation.At(-1); throw new Exception("Negative animation time accepted."); } catch (ArgumentOutOfRangeException) { }
        var sourceTiles = Source("Sources/Datas/Datas_Map.c");
        (uint ID,int Icon)[] Entries(string group) => Regex.Matches(Regex.Match(sourceTiles,@"ELEMENT\s+" + group + @"\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value,@"\{\s*(0x[\dA-Fa-f]+),\s*(?:0x[\dA-Fa-f]+|\d+),\s*(\d+),")
            .Select(match => (uint.Parse(match.Groups[1].Value[2..],NumberStyles.HexNumber,CultureInfo.InvariantCulture),int.Parse(match.Groups[2].Value,CultureInfo.InvariantCulture))).Where(item => item.Item1 != 0).ToArray();
        var groups = new[] { "World","Rooms","Gates","Walls" }.Select(Entries).ToArray(); var icons = new HashSet<int>();
        for (var terrain = 0; terrain < 256; terrain++) for (var owner = 0; owner < 256; owner++) foreach (var variant in new byte[] { 0,1,2,3,255 })
        {
            var tile = new MapTile((byte)terrain,(byte)owner,variant,255); var player = owner is >= 1 and <= 7 ? owner : 2;
            var key = (uint)(terrain << 24 | (variant is 1 or 2 ? variant : 1) << 8); var icon = 0; var animated = false;
            for (var group = 0; group < 4; group++)
            {
                var match = groups[group].FirstOrDefault(entry => group == 3 ? (entry.ID & 0xFFFFFF00) == (key | (uint)(player << 16)) : (entry.ID & 0xFF00FF00) == key);
                if (match.ID == 0) continue;
                var mana = match.ID == 0x1F020101; var effective = group == 1 ? player : (int)(match.ID >> 16 & 255);
                animated = effective == 2 && (group == 1 || mana || group == 3 && terrain == 8);
                icon = group is 1 or 2 || mana ? match.Icon : 0; break;
            }
            Expect(FullMapOverview.HoverIcon(tile) == icon && FullMapOverview.IsAnimatedNeutral(tile) == animated,$"Fullscreen tile classification differs from independent source oracle: {tile}; expected icon={icon}, neutral={animated}; actual icon={FullMapOverview.HoverIcon(tile)}, neutral={FullMapOverview.IsAnimatedNeutral(tile)}.");
            if (icon != 0) icons.Add(icon);
        }
        Expect(icons.Count == 30,"Not all original room/gate/mana icons covered.");
        var directory = Path.Combine(Path.GetTempPath(),"DK2FullMap-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false,true })
            {
                var document = NativeMapDocument.CreateNewMap(new("FullMap","FullMap",64,32,NativeFormat: native));
                var expected = new List<FullMapMarker>();
                foreach (var kind in Enum.GetValues<FullMapMarkerKind>()) for (byte owner = 1; owner <= 7; owner++)
                {
                    var point = new TilePoint(2 + owner,2 + (int)kind * 3);
                    if (native) document.AddRecord(RecordTemplate.All.Single(template => template.Structure == (kind switch { FullMapMarkerKind.Trap => "GAMETRAP",FullMapMarkerKind.Door => "GAMEDOOR",_ => "GAMEKEEPER" })),point,owner);
                    else document.AddLegacyRecord(kind.ToString(),point,owner);
                    expected.Add(new(kind,point,owner));
                }
                var invalid = native ? document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEHERO"),new(2,20)) : document.AddLegacyRecord("Creature",new(2,20),1);
                document.SetField(invalid,invalid.Fields.First(field => field.Name.EndsWith("LocationY",StringComparison.Ordinal) || field.Name == "y"),"1000");
                if (native)
                {
                    document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEHERO"),new(3,20),7); expected.Add(new(FullMapMarkerKind.Creature,new(3,20),1));
                    document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMENEUTRAL"),new(4,20),7); expected.Add(new(FullMapMarkerKind.Creature,new(4,20),2));
                    document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEOBJECT"),new(5,20));
                }
                else document.AddLegacyRecord("Object",new(5,20));
                document.Paint([new(12,12)],new(11,2,1)); document.Paint([new(13,12)],new(8,2,1)); document.Paint([new(14,12)],new(31,2,1));
                document.Paint([new(15,12)],new(9,2,1)); document.Paint([new(16,12)],new(11,3,1));
                document.Paint([new(17,12)],new(255,255,255)); document.Undo();
                var before = document.Files.ToDictionary(file => file,file => file.Data.ToArray()); var undo = document.CanUndo; var redo = document.CanRedo; var dirty = document.HasChanges; var notifications = 0;
                document.Changed += (_,_) => notifications++;
                var capture = FullMapOverview.Capture(document);
                Expect(capture.Markers.SequenceEqual(expected),"Fullscreen entities include bands/objects, omit real things, reorder owners or mishandle implied owners.");
                Expect(capture.NeutralCells.SequenceEqual(new[] { new TilePoint(12,12),new(13,12),new(14,12) }),"Neutral walls/owned rooms were animated or neutral rooms/claimed/mana omitted.");
                foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]),"Fullscreen capture changed map bytes.");
                Expect(undo == document.CanUndo && redo == document.CanRedo && dirty == document.HasChanges && notifications == 0,"Fullscreen capture changed history.");
                var reopened = NativeMapDocument.Load(document.SaveCopy(Path.Combine(directory,native ? "native" : "project")));
                Expect(FullMapOverview.Capture(reopened).Markers.SequenceEqual(capture.Markers) && FullMapOverview.Capture(reopened).NeutralCells.SequenceEqual(capture.NeutralCells),"Fullscreen display data changed after save/reopen.");
            }
        }
        finally { Directory.Delete(directory,true); }
    }
}
