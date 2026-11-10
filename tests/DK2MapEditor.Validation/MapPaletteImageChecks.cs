using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class MapPaletteImageChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(MapPaletteImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.MapPaletteImagesSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Source palette changed.");
        var groups=fixture.RootElement.GetProperty("Groups").EnumerateObject().Select(group=>(group.Name,Rows:group.Value.EnumerateArray().Select(r=>(ID:r.GetProperty("PackedID").GetUInt32(),Resource:r.GetProperty("Resource").GetInt32())).ToArray())).ToArray();
        Expect(groups.Sum(g=>g.Rows.Length)==53,"Visible source toolbar row count changed.");
        for(var terrain=0;terrain<256;terrain++)for(var owner=1;owner<=7;owner++)for(var variant=1;variant<=2;variant++)
        {
            int? expected=null;
            foreach(var group in groups)
            {
                foreach(var row in group.Rows)
                {
                    var matches=(row.ID>>24)==terrain&&((row.ID>>8)&255)==variant&&(group.Name!="Walls"||((row.ID>>16)&255)==owner);
                    if(matches){expected=row.Resource;break;}
                }
                if(expected is not null)break;
            }
            Expect(MapPaletteImageResources.ForTerrain((byte)terrain,(byte)owner,(byte)variant)==expected,"Palette used canvas fallback/frame or lost owner/variant identity.");
        }
        foreach(var owner in new byte[]{0,8,255})Expect(MapPaletteImageResources.ForTerrain(9,owner,1) is null,"Unknown owner invented a palette image.");
        foreach(var variant in new byte[]{0,3,255})Expect(MapPaletteImageResources.ForTerrain(9,3,variant) is null,"Unknown variant invented a palette image.");
        foreach(var tool in fixture.RootElement.GetProperty("Tools").EnumerateArray().Where(r=>r.GetProperty("Group").GetString()=="DrawingTools"))Expect(MapPaletteImageResources.ForDrawingTool(tool.GetProperty("CurrentTool").GetString()!)==tool.GetProperty("Resource").GetInt32(),"Drawing tool uses current enum index as source icon.");
        foreach(var tool in new[]{"Select","Sample","PlaceObject","Unknown",""})Expect(MapPaletteImageResources.ForDrawingTool(tool) is null,"Modern extra tool given an invented source icon.");
        foreach(var pair in new[]{("GAMEKEEPER",920),("GAMENEUTRAL",920),("GAMEHERO",920),("Creature",920),("GAMEDOOR",921),("Door",921),("GAMETRAP",922),("Trap",922),("GAMEOBJECT",923),("Object",923),("MagicalObject",924)})Expect(MapPaletteImageResources.ForTemplate(pair.Item1)==pair.Item2,"Template family differs from source MiscTools.");
        foreach(var type in new string?[]{null,"Trigger","Action","Band","GAMEBAND","GAMEAREA","Unknown"})Expect(MapPaletteImageResources.ForTemplate(type) is null,"Unrelated template given an invented source category icon.");
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;using var stream=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));using var pe=new PEReader(stream);
        var metadata=pe.GetMetadataReader();var section=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources)
        {var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;var reader=section.GetReader(checked((int)resource.Offset),section.Length-checked((int)resource.Offset));var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));}
        foreach(var image in fixture.RootElement.GetProperty("Images").EnumerateArray())
        {
            var original=File.ReadAllBytes(Path.Combine(root,image.GetProperty("SourceBitmap").GetString()!));var id=image.GetProperty("Resource").GetInt32();
            Expect(Convert.ToHexString(SHA256.HashData(original)).Equals(image.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Protected source palette BMP changed.");
            var prefixes=image.GetProperty("PaletteResource").GetBoolean()?new[]{"PaletteImage"}:new[]{"ScenarioImage","ObjectImage","TerrainImage","DefinitionImage","OverviewIcon"};
            var bytes=prefixes.Select(prefix=>embedded.GetValueOrDefault($"Legacy.{prefix}.{id}")).FirstOrDefault(v=>v is not null);
            Expect(bytes is not null&&original.AsSpan().SequenceEqual(bytes),"Compiled source palette bitmap missing/different:"+id);
        }
        foreach(var native in new[]{true,false})
        {
            var document=NativeMapDocument.CreateNewMap(new("Palette","Palette",20,20,NativeFormat:native));var bytes=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(document.EditRevision,document.EditorChangeCount,document.HasChanges,document.CanUndo,document.CanRedo);
            foreach(var owner in Enumerable.Range(1,7))foreach(var terrain in Enumerable.Range(1,41))_ = MapPaletteImageResources.ForTerrain((byte)terrain,(byte)owner,1);
            foreach(var data in bytes)Expect(data.Key.Data.Span.SequenceEqual(data.Value),"Palette metadata changed map bytes.");
            Expect(state==(document.EditRevision,document.EditorChangeCount,document.HasChanges,document.CanUndo,document.CanRedo),"Palette metadata changed history/count.");
        }
        Console.WriteLine("MAP PALETTE DETAIL3584 terrain-owner-variant combinations/53source rows/12tool resources/compiled source BMP bytes/read-only native+DKM state; no App/WPF execution.");
    }
}
