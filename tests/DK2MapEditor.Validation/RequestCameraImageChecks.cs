using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class RequestCameraImageChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(RequestCameraImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.RequestCameraImagesSource.json")!);var source=fixture.RootElement;
        foreach(var input in source.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Request/camera source changed.");
        var policies=0;
        foreach(var kind in new[]{"OK","OKCancel","YesNo","YesNoCancel"})
        {
            var buttons=OriginalMessagePolicy.Buttons(kind);Expect(buttons.Distinct().Count()==buttons.Count,"Duplicate message result.");
            foreach(var requested in new[]{"None","OK","Cancel","Yes","No"})Expect(buttons.Contains(OriginalMessagePolicy.Default(kind,requested)),"Default would invoke an absent command.");
            var closed=OriginalMessagePolicy.Close(kind);Expect(buttons.Contains(closed)&&closed!="Yes","Close/Escape could accept destructive action.");
            if(kind!="OK")Expect(closed is "No" or "Cancel","Close/Escape would accept changes.");
            foreach(var button in buttons)Expect(OriginalMessagePolicy.Label(button).StartsWith("Message.",StringComparison.Ordinal),"Result lost bilingual caption.");policies++;
        }
        foreach(var (kind,id) in new[]{("Information",80),("Asterisk",80),("Warning",81),("Exclamation",81),("Error",82),("Hand",82),("Stop",82),("Question",83)})Expect(OriginalMessagePolicy.Icon(kind)==id,"Request severity/source resource mapping changed.");Expect(OriginalMessagePolicy.Icon("None") is null,"Text-only message gained an icon.");
        var choices=CameraPaths.Choices.ToDictionary(choice=>choice.ID);Expect(choices.Count==52,"Camera catalogue count changed.");
        foreach(var camera in source.GetProperty("CameraDiagrams").EnumerateArray())
        {
            var id=camera.GetProperty("ID").GetInt32();Expect(choices.TryGetValue(id,out var choice)&&choice.ShowCell==camera.GetProperty("ShowCell").GetBoolean()&&!string.IsNullOrWhiteSpace(choice.English)&&!string.IsNullOrWhiteSpace(choice.Russian),"Camera source choice/mode/ENRU missing.");
            var bytes=File.ReadAllBytes(Path.Combine(root,camera.GetProperty("Source").GetString()!));Expect(BitConverter.ToInt32(bytes,18)==256&&Math.Abs(BitConverter.ToInt32(bytes,22))==192,"Original camera diagram size changed.");
        }
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;using var stream=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));using var pe=new PEReader(stream);var metadata=pe.GetMetadataReader();var section=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources){var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;var reader=section.GetReader(checked((int)resource.Offset),section.Length-checked((int)resource.Offset));var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));}
        foreach(var image in source.GetProperty("Images").EnumerateArray().Concat(source.GetProperty("CameraDiagrams").EnumerateArray()))
        {
            var bytes=File.ReadAllBytes(Path.Combine(root,image.GetProperty("Source").GetString()!));Expect(Convert.ToHexString(SHA256.HashData(bytes)).Equals(image.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Original request/camera asset changed.");Expect(embedded.TryGetValue(image.GetProperty("LogicalName").GetString()!,out var actual)&&bytes.AsSpan().SequenceEqual(actual),"Embedded request/camera resource differs.");
        }
        Console.WriteLine($"REQUEST CAMERA DETAIL {policies}button-set policies/close cannot accept destructive Yes/default always present/8severity aliases/10original ICO+52camera BMP PE identities/52source IDs+ShowCell+ENRU;35caller bodies verified separately; no WPF execution. FILE_THUMBNAIL chooser preview remains code work.");
    }
}
