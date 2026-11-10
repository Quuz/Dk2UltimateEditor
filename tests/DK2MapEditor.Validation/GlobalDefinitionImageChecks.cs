using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class GlobalDefinitionImageChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new InvalidOperationException("Canonical project root missing.");
        using var fixture=JsonDocument.Parse(typeof(GlobalDefinitionImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalDefinitionImagesSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Definition image source input changed.");
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var app=Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll");
        // Read managed PE resource bytes directly. Never load/execute the App,
        // WPF, a Window, BitmapSource, rendering, sound or native dialogs.
        using var file=File.OpenRead(app);using var pe=new PEReader(file);
        var metadata=pe.GetMetadataReader();var resources=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);
        var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources)
        {
            var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;
            var reader=resources.GetReader(checked((int)resource.Offset),resources.Length-checked((int)resource.Offset));
            var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));
        }
        var rows=fixture.RootElement.GetProperty("Entries").EnumerateArray().ToArray();
        Expect(rows.Length==184&&rows.Select(row=>row.GetProperty("Family").GetString()).Distinct().Count()==7,"Incomplete source global image scope.");
        var objects=fixture.RootElement.GetProperty("ObjectTargetEntries").EnumerateArray().ToArray();
        Expect(objects.Length==116,"Incomplete source projectile object image scope.");
        foreach(var row in rows.Concat(objects))
        {
            var family=row.GetProperty("Family").GetString()!;var index=row.GetProperty("Index").GetInt32();var id=row.GetProperty("Resource").GetInt32();
            var entry=(family=="Objects"?LegacyCompilerRules.Elements(family):GlobalDefinitionDefaults.Catalogue(family))[index];
            Expect(entry.ID==row.GetProperty("ID").GetUInt32()&&entry.Icon==id&&entry.Name==row.GetProperty("Symbol").GetString(),"Wrong source global row/image identity.");
            var expected=File.ReadAllBytes(Path.Combine(root,row.GetProperty("SourceBitmap").GetString()!));
            Expect(Convert.ToHexString(SHA256.HashData(expected)).Equals(row.GetProperty("BitmapSHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Protected original bitmap changed.");
            var embeddedID=row.GetProperty("EmbeddedResource").GetInt32();
            var bytes=new[]{"DefinitionImage","ObjectImage","TerrainImage","OverviewIcon"}.Select(prefix=>embedded.GetValueOrDefault($"Legacy.{prefix}.{embeddedID}")).FirstOrDefault(value=>value is not null);
            Expect(bytes is not null&&bytes.AsSpan().SequenceEqual(expected),"Compiled App bitmap missing or different from original resource:"+id);
            Expect(bytes![0]=='B'&&bytes[1]=='M'&&BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(18))==row.GetProperty("Width").GetInt32()
                &&Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(22)))==row.GetProperty("Height").GetInt32(),"Compiled bitmap dimensions differ.");
        }
        Console.WriteLine("DEFINITION IMAGE DETAIL source rows=184 families=7 object targets=116 original resource IDs/BMP hashes and compiled PE resource bytes exact; no App/WPF/bitmap execution.");
    }
}
