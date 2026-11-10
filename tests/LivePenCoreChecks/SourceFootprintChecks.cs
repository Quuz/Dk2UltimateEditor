using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class SourceFootprintChecks
{
    public static void Run()
    {
        using var capture=JsonDocument.Parse(typeof(SourceFootprintChecks).Assembly.GetManifestResourceStream("LivePenCoreChecks.LivePenSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var hash in capture.RootElement.GetProperty("InputHashes").EnumerateObject())
            if(!Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,hash.Name)))).Equals(hash.Value.GetString(),StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Original pen/cursor source changed: "+hash.Name);
        var rows=capture.RootElement.GetProperty("Rows").EnumerateArray().ToArray();
        if(rows.Length!=39)throw new InvalidOperationException("Original footprint catalogue incomplete.");
        foreach(var row in rows)
        {
            int Number(string name)=>row.GetProperty(name).GetInt32();
            var selected=new MapTile((byte)Number("Terrain"),7,(byte)Number("Variant"));
            var footprint=EditorTerrainRules.Footprint(selected);
            var expected=new EditorTerrainFootprint(Number("MinWidth"),Number("MinHeight"),Number("MaxWidth"),Number("MaxHeight"));
            if(footprint!=expected)throw new InvalidOperationException("Original cursor size differs: "+Number("Terrain")+"/"+Number("Variant"));
            foreach(var native in new[]{false,true})
            {
                var doc=NativeMapDocument.CreateNewMap(new("FootprintPen","FootprintPen",20,20,NativeFormat:native));
                var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
                var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                var bounded=Number("MinWidth")!=1||Number("MinHeight")!=1||Number("MaxWidth")!=0||Number("MaxHeight")!=0;
                if(bounded)
                {
                    try{doc.BeginLivePenStroke(selected);throw new Exception("Geometric terrain acquired free pen authority.");}
                    catch(ArgumentException){ }
                }
                else
                {
                    using var pen=doc.BeginLivePenStroke(selected); // reserve without starting
                    if(!doc.HasActivePenStroke||doc.EditorChangeCount!=count)throw new InvalidOperationException("Free pen reservation changed source count.");
                }
                if(doc.HasActivePenStroke||doc.GetHistory().Undo.Count!=history||doc.EditorChangeCount!=count
                    ||before.Any(pair=>!pair.Key.Data.Span.SequenceEqual(pair.Value)))
                    throw new InvalidOperationException("Footprint-only pen routing mutated the document.");
            }
        }
        Console.WriteLine("SOURCE FOOTPRINT DETAIL 39 rows x two formats: catalogue sizes, free/geometric pen route, rejected/reserved ownership and exact unchanged bytes/history/counters");
    }
}
