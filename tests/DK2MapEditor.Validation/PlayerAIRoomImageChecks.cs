using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

internal static class PlayerAIRoomImageChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        var path = root?.FullName ?? throw new Exception("Canonical project root missing.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(1252);
        var source = File.ReadAllText(Path.Combine(path, "Source/Sources/Datas/Datas_Map.c"), encoding);
        var body = Regex.Match(source, @"ELEMENT\s+CPURooms\[\]\s*=\s*\{(.*?)\};", RegexOptions.Singleline).Groups[1].Value;
        var rows = Regex.Matches(body, @"\{(0x[0-9A-Fa-f]+),0x00,\s*(\d+),[^{}]*?\b(sz\w+),\s*NULL\}")
            .Select(match => (ID: long.Parse(match.Groups[1].Value[2..], NumberStyles.HexNumber), Resource: int.Parse(match.Groups[2].Value))).ToArray();
        long[] ids = [1,2,3,4,6,7,8,9,10,11,12,13,14,15,16];
        int[] resources = [403,401,411,412,402,413,480,415,414,416,407,409,408,404,419];
        Expect(rows.Length == 15 && rows.Select(row => row.ID).SequenceEqual(ids) && rows.Select(row => row.Resource).SequenceEqual(resources), "CPURooms source identities changed.");
        Expect(PlayerAIOptions.Rooms.Select(room => long.Parse(room.Value)).SequenceEqual(ids), "Room image support changed source room ordering.");
        foreach (var row in rows) Expect(PlayerAIOptions.RoomImageResource(row.ID) == row.Resource, "Room image uses a terrain ID or array index.");
        foreach (var id in new long[] { long.MinValue,-1,0,5,17,255,uint.MaxValue,long.MaxValue })
            Expect(PlayerAIOptions.RoomImageResource(id) is null, "Unknown room ID received a fallback image.");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        using var stream = File.OpenRead(Path.Combine(path, "src/DK2MapEditor.App/bin", configuration, "net10.0-windows/DK2MapEditor.App.dll"));
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        var section = pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);
        var embedded = new Dictionary<string,byte[]>();
        foreach (var handle in metadata.ManifestResources)
        {
            var resource = metadata.GetManifestResource(handle); if (!resource.Implementation.IsNil) continue;
            var reader = section.GetReader(checked((int)resource.Offset), section.Length - checked((int)resource.Offset));
            embedded.Add(metadata.GetString(resource.Name), reader.ReadBytes(checked((int)reader.ReadUInt32())));
        }
        var rc = File.ReadAllText(Path.Combine(path, "Source/Resources/Editor.rc"), encoding);
        foreach (var row in rows)
        {
            var bitmap = Regex.Match(rc, @"^" + row.Resource + @"\s+BITMAP\s+""([^""]+)""", RegexOptions.Multiline).Groups[1].Value.Replace("\\\\", "\\");
            Expect(bitmap.Length > 0, "Source room bitmap RC binding missing.");
            var original = File.ReadAllBytes(Path.Combine(path, "Source", bitmap));
            var matches = new[] { "Legacy.ObjectImage.", "Legacy.OverviewIcon." }
                .Where(prefix => embedded.ContainsKey(prefix + row.Resource)).Select(prefix => embedded[prefix + row.Resource]).ToArray();
            Expect(matches.Length == 1 && matches[0].AsSpan().SequenceEqual(original), "Compiled room image differs from source bitmap or loader binding.");
        }
        foreach (var native in new[] { true, false })
        {
            var document = NativeMapDocument.CreateNewMap(new("AIRoomImages", "AIRoomImages", 20, 20, NativeFormat: native));
            var bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var state = (document.EditRevision, document.HasChanges, document.CanUndo, document.CanRedo);
            var original = document.GetPlayerAIConfiguration(3).RoomOrder.ToArray();
            foreach (var order in new[] { original, original.Reverse().ToArray(), new long[] { 5,255,0,-1 } })
            {
                var saved = order.ToArray();
                foreach (var id in order) _ = PlayerAIOptions.RoomImageResource(id);
                Expect(order.SequenceEqual(saved), "Read-only image lookup sorted or repaired the priority draft.");
            }
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Room image lookup changed map bytes.");
            Expect(state == (document.EditRevision, document.HasChanges, document.CanUndo, document.CanRedo), "Room image lookup changed history/state.");
        }
        Console.WriteLine("AI ROOM IMAGE DETAIL 15 CPURooms identities/source BMP bytes in managed PE; unknown IDs and read-only order/state verified without App/WPF execution.");
    }
}
