using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class RewardDestinationImageChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(RewardDestinationImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.RewardDestinationImagesSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Reward/list source changed.");
        var rewards=MapLevelOptions.Rewards;var source=fixture.RootElement.GetProperty("Rewards").EnumerateArray().ToArray();
        Expect(rewards.Count==49&&rewards[0].Value=="0"&&rewards[0].Icon is null,"None reward lost its empty image slot or changed identity.");
        for(var i=0;i<source.Length;i++)Expect(rewards[i+1].Value==source[i].GetProperty("ID").GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture)&&rewards[i+1].Icon==source[i].GetProperty("Resource").GetInt32(),"Reward source order/ID/icon association differs.");
        // Literal physical sizes derived from source line height + two pixels,
        // including a fractional font line that must first fit whole device pixels.
        foreach(var row in new[]{(12d,1d,14d),(16d,1d,18d),(20d,1.25d,27d),(20d,1.5d,32d),(20d,2d,42d),(13.2d,1.25d,19d),(13.2d,1.5d,22d)})
            Expect(Math.Abs(RewardImageSizing.Size(row.Item1,row.Item2)*row.Item2-row.Item3)<0.000001,"Reward size not font-relative or extra pixels scaled twice.");
        foreach(var invalid in new[]{0d,-1d,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
        {
            try{_ = RewardImageSizing.Size(invalid,1);throw new Exception("Invalid font height accepted.");}catch(ArgumentOutOfRangeException){}
            try{_ = RewardImageSizing.Size(16,invalid);throw new Exception("Invalid monitor scale accepted.");}catch(ArgumentOutOfRangeException){}
        }
        foreach(var native in new[]{true,false})
        {
            var document=NativeMapDocument.CreateNewMap(new("Destination","Destination",20,20,NativeFormat:native));
            var events=new[]{"Trigger","Action"}.Select(kind=>native?document.AddRecord(RecordTemplate.All.Single(t=>t.Structure==kind),new(0,0)):document.AddLegacyRecord(kind,new(0,0))).ToArray();
            var bytes=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(document.EditRevision,document.EditorChangeCount,document.HasChanges,document.CanUndo,document.CanRedo);
            for(var i=0;i<events.Length;i++)
            {
                var field=events[i].Fields.First();var edge=new ScriptEdge(events[i],field,0);
                Expect(EditorListImageResources.ScenarioDestination(edge)==200+i,"Event destination icon uses scope/index instead of native category.");
                Expect(EditorListImageResources.ScenarioDestination(edge with{Owner=events[i] with{Length=events[i].Length-1}}) is null,"Malformed event destination got an icon.");
                foreach(var scope in fixture.RootElement.GetProperty("Scopes").EnumerateArray())Expect(EditorListImageResources.ScenarioDestination(edge with{Scope=scope.GetProperty("Scope").GetUInt32()})==scope.GetProperty("Resource").GetInt32(),"Root destination icon differs from source TrImages.");
                foreach(var scope in new uint[]{3,5,512,uint.MaxValue})Expect(EditorListImageResources.ScenarioDestination(edge with{Scope=scope}) is null,"Combined/unknown destination scope given invented icon.");
            }
            foreach(var data in bytes)Expect(data.Key.Data.Span.SequenceEqual(data.Value),"Image metadata changed map bytes.");
            Expect(state==(document.EditRevision,document.EditorChangeCount,document.HasChanges,document.CanUndo,document.CanRedo),"Image metadata changed history/count.");
        }
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;using var stream=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));using var pe=new PEReader(stream);
        var metadata=pe.GetMetadataReader();var section=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources)
        {var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;var reader=section.GetReader(checked((int)resource.Offset),section.Length-checked((int)resource.Offset));var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));}
        var imageCount=0;
        foreach(var image in fixture.RootElement.GetProperty("Images").EnumerateArray())
        {
            var original=File.ReadAllBytes(Path.Combine(root,image.GetProperty("SourceBitmap").GetString()!));var id=image.GetProperty("Resource").GetInt32();
            Expect(Convert.ToHexString(SHA256.HashData(original)).Equals(image.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Source reward/list BMP changed.");
            var prefixes=id is >=200 and <=209?new[]{"ListImage"}:new[]{"ObjectImage","OverviewIcon","TerrainImage","DefinitionImage","ScenarioImage"};
            var bytes=prefixes.Select(prefix=>embedded.GetValueOrDefault($"Legacy.{prefix}.{id}")).FirstOrDefault(v=>v is not null);
            Expect(bytes is not null&&original.AsSpan().SequenceEqual(bytes),"Loader cannot resolve original reward/list bitmap:"+id);imageCount++;
        }
        Console.WriteLine($"REWARD DESTINATION DETAIL48 ordered source rewards/None0/font+DPI physical sizing/9 scope+2 event identities/{imageCount} original BMPs in compiled PE/native+DKM read-only bytes/history. No WPF execution.");
    }
}
