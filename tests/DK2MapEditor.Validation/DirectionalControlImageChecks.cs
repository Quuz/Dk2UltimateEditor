using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class DirectionalControlImageChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(DirectionalControlImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.DirectionalControlImagesSource.json")!);
        var source=fixture.RootElement;
        foreach(var input in source.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Directional image source changed.");
        Expect(ControlImageResources.ForVerticalMove(false)==50&&ControlImageResources.ForVerticalMove(true)==51,"Vertical direction changed.");
        foreach(var (move,id) in new[]{(ScriptBranchMove.Up,50),(ScriptBranchMove.Down,51),(ScriptBranchMove.Outdent,52),(ScriptBranchMove.Indent,53)})Expect(ControlImageResources.ForBranchMove(move)==id,"Branch image direction changed.");
        Expect(ControlImageResources.ForBranchMove((ScriptBranchMove)255) is null,"Unknown branch direction given an image.");
        var cases=0;
        foreach(var matrix in source.GetProperty("ResizeMatrices").EnumerateArray())for(var anchor=0;anchor<9;anchor++)
        {
            var expected=new List<ResizeAnchorImage>();var mask=source.GetProperty("AnchorMasks")[anchor];var resources=matrix.GetProperty("Resources");
            for(var index=0;index<9;index++)if(mask[index].GetInt32()!=0)expected.Add(new(resources[index].GetInt32(),anchor%3*2+index%3,anchor/3*2+index/3));
            var actual=ResizeAnchorImageLayout.Create(anchor,matrix.GetProperty("ShrinkWidth").GetBoolean(),matrix.GetProperty("ShrinkHeight").GetBoolean());
            Expect(actual.SequenceEqual(expected),$"Source resize display differs at anchor{anchor}, matrix{cases/9}.");
            Expect(actual.Count==(anchor==4?8:anchor%2==0?3:5)&&actual.All(image=>image.Column is >=0 and <=5&&image.Row is >=0 and <=5),"Resize image mask/geometry differs.");cases++;
        }
        foreach(var anchor in new[]{-1,9}){var rejected=false;try{ResizeAnchorImageLayout.Create(anchor,false,false);}catch(ArgumentOutOfRangeException){rejected=true;}Expect(rejected,"Invalid anchor accepted.");}
        // Inspect the PE resource section without loading or executing WPF.
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;using var stream=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));using var pe=new PEReader(stream);var metadata=pe.GetMetadataReader();var section=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources){var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;var reader=section.GetReader(checked((int)resource.Offset),section.Length-checked((int)resource.Offset));var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));}
        var images=0;
        foreach(var image in source.GetProperty("Images").EnumerateArray())
        {
            var bytes=File.ReadAllBytes(Path.Combine(root,image.GetProperty("SourceICO").GetString()!));Expect(Convert.ToHexString(SHA256.HashData(bytes)).Equals(image.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Original directional ICO changed.");
            Expect(embedded.TryGetValue(image.GetProperty("LogicalName").GetString()!,out var actual)&&bytes.AsSpan().SequenceEqual(actual),"Compiled directional ICO differs.");images++;
        }
        Console.WriteLine($"DIRECTIONAL CONTROL IMAGES DETAIL {cases}source resize cases/9masks/4growth-shrink matrices/half-cell geometry/8direction and4picker routes/{images}originalICO identities and PE resources; no WPF execution; labels/handlers remain in UI source.");
    }
}
