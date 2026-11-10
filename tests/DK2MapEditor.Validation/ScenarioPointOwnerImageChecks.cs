using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class ScenarioPointOwnerImageChecks
{
    private static void Expect(bool value,string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(ScenarioPointOwnerImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioPointOwnerImagesSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Scenario point image source changed.");
        var rows=fixture.RootElement.GetProperty("Entries").EnumerateArray().ToArray();Expect(rows.Length==109,"Incomplete point owner image source inventory.");
        foreach(var row in rows)Expect(ScenarioPointOwnerImageResources.ForIdentity(row.GetProperty("Scope").GetUInt32(),row.GetProperty("Identity").GetUInt32())==row.GetProperty("Resource").GetInt32(),"Owner image differs from original GetImageId/category lookup.");
        foreach(var scope in new uint[]{1,2,16,64,128,256})Expect(ScenarioPointOwnerImageResources.ForIdentity(scope,uint.MaxValue)==400,"Source unknown/default fallback differs.");
        foreach(var scope in new uint[]{0,3,32,0x1000000,0x2000000,uint.MaxValue})Expect(ScenarioPointOwnerImageResources.ForIdentity(scope,1) is null,"No-page or unsupported scope invented an image.");
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        using var file=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));using var pe=new PEReader(file);
        var metadata=pe.GetMetadataReader();var section=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources)
        {
            var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;
            var reader=section.GetReader(checked((int)resource.Offset),section.Length-checked((int)resource.Offset));
            var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));
        }
        foreach(var image in fixture.RootElement.GetProperty("Images").EnumerateArray())
        {
            var original=File.ReadAllBytes(Path.Combine(root,image.GetProperty("SourceBitmap").GetString()!));
            Expect(Convert.ToHexString(SHA256.HashData(original)).Equals(image.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Original point owner BMP changed.");
            var id=image.GetProperty("EmbeddedResource").GetInt32();var compiled=new[]{"ScenarioImage","ObjectImage","TerrainImage","DefinitionImage","OverviewIcon"}.Select(prefix=>embedded.GetValueOrDefault($"Legacy.{prefix}.{id}")).FirstOrDefault(bytes=>bytes is not null);
            Expect(compiled is not null&&original.AsSpan().SequenceEqual(compiled),"Compiled loader BMP missing/different for owner:"+id);
        }
        foreach(var native in new[]{true,false})
        {
            var document=NativeMapDocument.CreateNewMap(new("PointImages","PointImages",20,20,NativeFormat:native));
            if(native)foreach(var template in RecordTemplate.All.Where(t=>t.Structure is "GAMEKEEPER" or "GAMEHERO" or "GAMEDOOR" or "GAMEOBJECT" or "GAMEAREA" or "GAMEBAND"))document.AddRecord(template,new(2,2));
            else foreach(var kind in document.LegacyAddChoices().ToArray())document.AddLegacyRecord(kind,new(2,2));
            var band=document.Records().First(r=>native?r.Category=="GAMEBAND":r.Category=="Project.MTHBAND");
            Expect(MemoryMarshal.TryGetArray(band.File.Data,out var raw),"Independent band image fixture inaccessible.");
            var memberIDs=rows.Where(r=>r.GetProperty("Scope").GetUInt32()==16).Take(2).Select(r=>r.GetProperty("Identity").GetUInt32()).ToArray();
            for(var i=0;i<2;i++)
            {
                var offset=band.Offset+(native?51+i*32:12+i*44);
                if(native){raw.Array![offset+15]=1;raw.Array[offset+28]=checked((byte)memberIDs[i]);}
                else{BinaryPrimitives.WriteInt32LittleEndian(raw.Array!.AsSpan(offset),1);BinaryPrimitives.WriteUInt32LittleEndian(raw.Array.AsSpan(offset+12),memberIDs[i]);}
            }
            var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(document.EditRevision,document.HasChanges,document.CanUndo,document.CanRedo);
            var owners=document.ScriptOwners();Expect(owners.Any(o=>o.Scope==1)&&owners.Any(o=>o.Scope==4)&&owners.Any(o=>o.Scope==8),"Owner fixtures lack source categories.");
            var members=owners.Where(o=>o.Scope==16).ToArray();Expect(members.Length==2,"Native/project physical member owner fixtures missing.");
            foreach(var member in members)Expect(ScenarioPointOwnerImageResources.For(member)==ScenarioPointOwnerImageResources.ForIdentity(16,memberIDs[member.MemberIndex]),"Member image read band ID instead of physical member creature ID.");
            foreach(var owner in owners)
            {
                var resource=ScenarioPointOwnerImageResources.For(owner);
                if(owner.Scope==32)Expect(resource is null,"Level owner has no physical owner page.");
                else Expect(resource is not null,"Physical owner lacks image:"+owner.Scope);
                if(owner.Scope==1)
                {
                    var id=owner.PlayerID!=0?owner.PlayerID:uint.Parse(owner.Record.Read(owner.Record.Fields.Single(f=>f.Name=="bGlobalPlayer")));
                    Expect(resource==ScenarioPointOwnerImageResources.ForIdentity(1,id),"Player record read confused physical ID with row.");
                }
                Expect(ScenarioPointOwnerImageResources.For(owner with{Record=owner.Record with{Offset=-1}}) is null,"Invalid physical owner range accepted.");
            }
            foreach(var data in before)Expect(data.Key.Data.Span.SequenceEqual(data.Value),"Image lookup changed map bytes.");
            Expect(state==(document.EditRevision,document.HasChanges,document.CanUndo,document.CanRedo),"Image lookup changed history/revision.");
        }
        Console.WriteLine("POINT OWNER IMAGE DETAIL109 source identity cases/player+creature+door+area+band+generic object/magic fallbacks/compiled BMP bytes/native+DKM read-only state; no App/WPF execution.");
    }
}
