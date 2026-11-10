using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class DirectListImageChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(DirectListImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.DirectListImagesSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Direct image source changed.");
        Expect(ControlImageResources.ForScenarioName(ScenarioNameKind.Variable)==54&&ControlImageResources.ForScenarioName(ScenarioNameKind.Timer)==55&&ControlImageResources.ForScenarioName((ScenarioNameKind)255) is null,"Name kind confused with numeric alias/index.");
        var cases=0;
        foreach(var type in new[]{3,6,7,255})for(var mask=0;mask<16;mask++)
        {
            var protect=(mask&1)!=0;var gate=(mask&2)!=0;var area=(mask&4)!=0;var events=(mask&8)!=0;
            var expected=89;if(type==3){if(!protect&&(!gate||!area||!events))expected=87;}else if(type is 6 or 7&&!gate)expected=88;
            Expect(GateGenerationImageResources.ForSite(type,protect,gate,area,events)==expected,"Gate list status differs from source site facts.");cases++;
        }
        foreach(var native in new[]{true,false})
        {
            var d=NativeMapDocument.CreateNewMap(new("IconSites","IconSites",24,24,NativeFormat:native));d.Paint(MapDrawing.Rectangle(new(8,8,2,2),true),new(33,1,1));
            var gate=d.AddMapRegion(MapRegionKind.Gate,new(8,8,1,1));var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo);
            using(var parent=d.CreateRegionPropertiesSession(gate))using(var child=d.BeginRegionGateGeneration(parent))
            {
                var empty=d.PreviewRegionGateGeneration(child,child.Defaults with{Flags=HeroGateGenerationFlags.None});var full=d.PreviewRegionGateGeneration(child,child.Defaults);
                Expect(empty.Items.Count>0&&empty.Items.Select(r=>r.ImageResource).SequenceEqual(full.Items.Select(r=>r.ImageResource)),"Status image inferred from Add flags instead of existing site facts.");
                Expect(empty.Items.Zip(full.Items).All(pair=>pair.First.Type==pair.Second.Type&&pair.First.Footprint==pair.Second.Footprint),"Image metadata changed physical site identity.");
                d.AcceptRegionGateGeneration(empty,true); // Recomputed immutable image metadata must compare equal.
            }
            Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo),"Display snapshot changed map/history.");
        }
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;using var stream=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));using var pe=new PEReader(stream);var metadata=pe.GetMetadataReader();var section=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources){var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;var reader=section.GetReader(checked((int)resource.Offset),section.Length-checked((int)resource.Offset));var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));}
        foreach(var image in fixture.RootElement.GetProperty("Images").EnumerateArray())
        {
            var bytes=File.ReadAllBytes(Path.Combine(root,image.GetProperty("SourceICO").GetString()!));Expect(Convert.ToHexString(SHA256.HashData(bytes)).Equals(image.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Original ICO changed.");
            Expect(embedded.TryGetValue(image.GetProperty("LogicalName").GetString()!,out var actual)&&bytes.AsSpan().SequenceEqual(actual),"Compiled direct icon resource differs.");
        }
        Console.WriteLine($"DIRECT LIST IMAGES DETAIL5 source ICOs/64site-status combinations/variable+timer identity/native+DKM option-independent immutable preview/read-only bytes/history; no WPF execution. Direct resource inventory:{fixture.RootElement.GetProperty("DirectResourceCallCount").GetInt32()}calls.");
    }
}
