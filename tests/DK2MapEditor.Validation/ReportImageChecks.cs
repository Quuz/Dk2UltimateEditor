using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Diagnostics;

internal static class ReportImageChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(ReportImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ReportImagesSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Report image source changed.");
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;using var stream=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));using var pe=new PEReader(stream);var metadata=pe.GetMetadataReader();var section=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources){var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;var reader=section.GetReader(checked((int)resource.Offset),section.Length-checked((int)resource.Offset));var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));}
        foreach(var image in fixture.RootElement.GetProperty("Images").EnumerateArray())
        {
            var severity=Enum.Parse<DiagnosticSeverity>(image.GetProperty("Severity").GetString()!);Expect(ReportImageResources.For(severity)==image.GetProperty("Resource").GetInt32(),"Report source severity/index mapping differs.");
            var bytes=File.ReadAllBytes(Path.Combine(root,image.GetProperty("OriginalICO").GetString()!));Expect(Convert.ToHexString(SHA256.HashData(bytes)).Equals(image.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Original ICO changed.");
            Expect(embedded.TryGetValue(image.GetProperty("LogicalName").GetString()!,out var actual)&&bytes.AsSpan().SequenceEqual(actual),"Original report ICO missing from App resource loader route.");
            var frames=System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(4));Expect(Enumerable.Range(0,frames).Any(i=>bytes[6+i*16]==16&&bytes[7+i*16]==16),"Original report ICO has no16px frame.");
        }
        foreach(var unknown in new[]{-1,255,int.MaxValue})Expect(ReportImageResources.For((DiagnosticSeverity)unknown) is null,"Unknown diagnostic severity assigned invented icon.");
        Console.WriteLine("REPORT IMAGE DETAIL3source ICO identities/16px frames/exact compiled PE resources/known and unknown severity routes; no WPF/App execution, no diagnostic classification changes.");
    }
}
