using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class HeroBandDisplayChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static string Sha(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(HeroBandDisplayChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.HeroBandDisplaySource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Sha(File.ReadAllBytes(Path.Combine(root,input.Name))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Hero display source changed.");
        var creatures=fixture.RootElement.GetProperty("Creatures").EnumerateArray().ToArray();var elite=creatures.First(c=>c.GetProperty("Group").GetString()=="BadElites").GetProperty("ID").GetUInt32();
        Expect(creatures.Length==45&&ValueCatalogues.BandCreatures.Count==32,"Display repair broadened source creation catalogue.");
        Expect(!ValueCatalogues.BandCreatures.Any(c=>c.Value==elite.ToString()),"Elite became a new-member suggestion.");
        foreach(var language in new[]{"en","ru"})
        {
            var strings=XDocument.Load(Path.Combine(root,$"src/DK2MapEditor.App/Localization/Strings.{language}.xaml")).Root!.Elements().Where(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml")) is not null).ToDictionary(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml"))!.Value,e=>e.Value);
            string Text(string key)=>strings[key];
            foreach(var creature in creatures)
            {
                var id=creature.GetProperty("ID").GetUInt32();var choice=HeroBandDisplayRows.Creature(id);var row=HeroBandDisplayRows.Create(3,new(id,7,0,73,22,15,63,23,41),language,Text);
                Expect(choice is not null&&row.Label.Contains(choice.Name(language))&&row.ImageResource==creature.GetProperty("Resource").GetInt32(),"Known band creature lost its bilingual name/source image.");
                Expect(row.Label.Contains("+TAHL/SJMC")&&row.Label.Contains("41")&&row.Index==3,"Band flags/objective area or physical member index lost.");
            }
            var unknown=HeroBandDisplayRows.Create(0,new(255,Objective:254),language,Text);Expect(unknown.Label.Contains("255")&&unknown.Label.Contains("254")&&unknown.ImageResource==1115,"Unknown imported member/objective discarded or invented known creature.");
        }
        var assignments=fixture.RootElement.GetProperty("Flags").EnumerateArray().ToArray();
        foreach(var row in fixture.RootElement.GetProperty("ManagerRows").EnumerateArray())
            Expect(ObjectManagerImageResources.For(Enum.Parse<PlacedThingKind>(row.GetProperty("Kind").GetString()!),row.GetProperty("ID").GetUInt32(),row.GetProperty("Version").GetInt32())==row.GetProperty("Resource").GetInt32(),"Manager image route uses wrong family/game version/index.");
        foreach(var pair in fixture.RootElement.GetProperty("ManagerUnknownFallbacks").EnumerateObject())foreach(var version in new[]{16,17})
            Expect(ObjectManagerImageResources.For(Enum.Parse<PlacedThingKind>(pair.Name),255,version)==pair.Value.GetInt32(),"Manager unknown ID did not use source shared-list index0.");
        for(uint behavior=0;behavior<64;behavior++)for(uint options=0;options<16;options++)
        {
            var expected="-----/----".ToCharArray();
            foreach(var assignment in assignments)if(((assignment.GetProperty("Field").GetString()=="options"?options:behavior)&assignment.GetProperty("Mask").GetUInt32())!=0)expected[assignment.GetProperty("Index").GetInt32()]=assignment.GetProperty("Symbol").GetString()![0];
            Expect(HeroBandDisplayRows.Flags(options,behavior)==new string(expected),"Compact flags differ from ordered source assignments.");
            Expect(HeroBandDisplayRows.Flags(options|0x8000,behavior|0x8000)==new string(expected),"Unknown retained flags interpreted as named source flags.");
        }
        foreach(var native in new[]{true,false})
        {
            var d=NativeMapDocument.CreateNewMap(new("Display","Display",20,20,NativeFormat:native));
            var band=native?d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEBAND"),new(0,0)):d.AddLegacyRecord("Band",new(0,0));
            foreach(var id in new[]{elite,17u,255u})d.AddBandMember(band,id,7,3);
            void Set(int index,string project,string field,string value)
            {
                var owner=native?d.Records().Single(r=>r.File==band.File&&r.Offset==band.Offset+51+index*32&&r.Category.StartsWith("BandHero",StringComparison.Ordinal)):band;
                var name=native?field:$"heros[{index}].{project}";d.SetField(owner,owner.Fields.Single(f=>f.Name==name),value);
            }
            Set(0,"options","bHeroOptions","15");Set(0,"options2","bGlobalBehavior","63");Set(0,"objective","bHeroObjective","23");Set(0,"area","bHeroTargetArea","41");Set(0,"health","dwGlobalHealth","73");Set(0,"goldheld","bGlobalGoldHeld","22");
            var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.HasChanges,d.CanUndo,d.CanRedo);
            var snapshot=d.ReadHeroBandDisplay(band);Expect(snapshot.Count==3&&snapshot[0].Values==new EditorHeroMemberValues(elite,7,3,73,22,15,63,23,41)&&snapshot[2].Values.CreatureID==255,"Band selector lost literal member fields or unknown imported creature.");
            foreach(var data in bytes)Expect(data.Key.Data.Span.SequenceEqual(data.Value),"Display read changed map bytes.");Expect(state==(d.EditRevision,d.EditorChangeCount,d.HasChanges,d.CanUndo,d.CanRedo),"Display read changed history/count.");
            Set(1,"isvalid","bGlobalBehavior","0");Expect(d.ReadHeroBandDisplay(band).Count==1&&snapshot.Count==3,"Inactive gap included retained tail or mutated old snapshot.");
            try{_ = d.ReadHeroBandDisplay(band with{Length=band.Length-1});throw new Exception("Malformed record accepted.");}catch(ArgumentException){}
            var other=NativeMapDocument.CreateNewMap(new("Other","Other",20,20,NativeFormat:native));try{_ = other.ReadHeroBandDisplay(band);throw new Exception("Foreign record accepted.");}catch(ArgumentException){}
        }
        var privateDirectory=Path.Combine(Path.GetTempPath(),"DK2MapEditor.HeroBandDisplay",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(privateDirectory);
        for(var version=1;version<=5;version++)
        {
            var seed=NativeMapDocument.CreateNewMap(new("History","History",16,16,NativeFormat:false));var file=seed.SaveCopy(Path.Combine(privateDirectory,"seed"+version));var container=LegacySectionFile.Read(file);var entry=Path.Combine(privateDirectory,"historical"+version+".dkm");
            File.WriteAllBytes(entry,LegacySectionFile.Create(container.Sections.Select(s=>s.Signature==NativeMapDocument.LegacyBands?new(s.Signature,version,ReadOnlyMemory<byte>.Empty):s)).ToArray());var d=NativeMapDocument.Load(entry);
            for(var ordinal=0;ordinal<2;ordinal++)
            {
                var band=d.AddLegacyRecord("Band",new(0,0));d.AddBandMember(band,ordinal==0?17u:elite,4+ordinal,3);
                var id=band.Fields.Single(f=>f.Name=="id");
                // Simulate an imported duplicate, which normal editing correctly refuses.
                Expect(MemoryMarshal.TryGetArray(band.File.Data,out var raw),"Private historical fixture unavailable.");
                id.Encode("7").CopyTo(raw.Array!.AsSpan(raw.Offset+band.Offset+id.Offset));
                var fields=band.Fields.Where(f=>f.Name is "heros[0].options2" or "heros[0].objective").ToArray();foreach(var field in fields)d.SetField(band,field,field.Name.EndsWith("options2",StringComparison.Ordinal)?"63":"23");
            }
            var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo);var bands=d.Records().Where(NativeMapDocument.IsBand).ToArray();
            var first=d.ReadHeroBandDisplay(bands[0]);var second=d.ReadHeroBandDisplay(bands[1]);
            Expect(first.Count==1&&second.Count==1&&first[0].Values.CreatureID==17&&second[0].Values.CreatureID==elite&&first[0].Values.Level==4&&second[0].Values.Level==5,"Historical display matched duplicate band IDs instead of physical ordinal.");
            Expect(second[0].Values.Options2==(version<=3?43u:63u)&&second[0].Values.Objective==(version<=2?19u:23u),"Historical source reader defaults differ.");
            Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo),"Historical display upgraded live map bytes/history.");
        }
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;using var stream=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));using var pe=new PEReader(stream);var metadata=pe.GetMetadataReader();var section=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources){var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;var reader=section.GetReader(checked((int)resource.Offset),section.Length-checked((int)resource.Offset));var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));}
        foreach(var image in fixture.RootElement.GetProperty("Images").EnumerateArray())
        {
            var bytes=File.ReadAllBytes(Path.Combine(root,image.GetProperty("SourceBitmap").GetString()!));Expect(Sha(bytes).Equals(image.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Original band image changed.");var id=image.GetProperty("Canonical").GetInt32();var actual=embedded.GetValueOrDefault("Legacy.ObjectImage."+id)??embedded.GetValueOrDefault("Legacy.OverviewIcon."+id);Expect(actual is not null&&bytes.AsSpan().SequenceEqual(actual),"Original band bitmap missing in compiled loader route.");
        }
        Console.WriteLine("HERO BAND DISPLAY DETAIL45 known creatures/32 unchanged creation choices/1024 ordered flag combinations/EN+RU/native+DKM prefix snapshots/5 historical versions with duplicate IDs/34 original BMP identities in PE (32band+2manager)/209 manager routes+five shared-list fallbacks; no WPF execution.");
    }
}
