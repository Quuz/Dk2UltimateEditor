using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class PlacedThingCaptionChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(PlacedThingCaptionChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.PlacedThingCaptionsSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Placed thing source changed.");
        var resources=new[]{"en","ru"}.ToDictionary(language=>language,language=>XDocument.Load(Path.Combine(root,$"src/DK2MapEditor.App/Localization/Strings.{language}.xaml")).Root!.Elements().Where(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml")) is not null).ToDictionary(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml"))!.Value,e=>e.Value));
        var keys=new HashSet<string>();
        void Caption(string? actual,string? expected){Expect(actual==expected,$"Wrong source caption: {actual} / {expected}");if(actual is not null)keys.Add(actual);}
        string Data(PlacedThingKind kind,uint? id)=>kind switch{PlacedThingKind.Creature=>"Field.Level",PlacedThingKind.Door=>"PlacedThing.DoorState",PlacedThingKind.Trap=>"Field.Shots",PlacedThingKind.Object when id is 1 or 2 or 3=>"Field.GoldValue",PlacedThingKind.Object when id==4=>"Field.Spell",_=>"Field.DataAll"};
        foreach(var kind in Enum.GetValues<PlacedThingKind>())foreach(var id in new uint?[]{null,0,1,2,3,4,5,116,127,255})Caption(PlacedThingCaptionRules.DataKey(kind,id),Data(kind,id));
        NativeFile CreateFixtureFile(uint signature,uint version,byte[] bytes)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes,signature);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),version);
            return (NativeFile)Activator.CreateInstance(typeof(NativeFile),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{"private-caption-fixture","private-caption-fixture",bytes},null)!;
        }
        var projectCases=0;var nativeCases=0;
        foreach(var versionRow in fixture.RootElement.GetProperty("ProjectVersions").EnumerateArray())foreach(var kind in Enum.GetValues<PlacedThingKind>())foreach(var id in new uint[]{1,2,3,4,5,255})
        {
            var version=versionRow.GetProperty("Version").GetUInt32();var structure=versionRow.GetProperty("Structure").GetString()!;var fields=LegacyProjectSchemas.Fields(structure);var length=LegacyProjectSchemas.Size(structure);var bytes=new byte[12+length];var file=CreateFixtureFile(NativeMapDocument.LegacyThings,version,bytes);var record=new NativeRecord(file,12,length,"Project."+structure,fields);
            void Set(string name,string value){var field=fields.Single(f=>f.Name==name);field.Encode(value).CopyTo(bytes.AsSpan(record.Offset+field.Offset));}
            Set("node.type",((int)kind).ToString());Set("id",id.ToString());Set("dataAll","-12345");var data=fields.Single(f=>f.Name=="dataAll");var before=bytes.ToArray();
            var expected=kind==PlacedThingKind.Door&&version<=4?"Field.Locked":kind==PlacedThingKind.Trap&&version<=4?"Field.DataAll":Data(kind,id);
            Caption(PlacedThingCaptionRules.ForRecord(record,data),expected);
            foreach(var field in fields)
            {
                var key=PlacedThingCaptionRules.ForRecord(record,field);
                if(field.Name is "herosoptions" or "options")Caption(key,"Batch.HeroOptions");
                if(field.Name=="creaturesoptions")Caption(key,"Field.Behavior");
                if(field.Name=="behavior")Caption(key,null); // Historical behavior has different meaning.
            }
            Expect(bytes.AsSpan().SequenceEqual(before),"Project caption normalized retained union value.");
            Caption(PlacedThingCaptionRules.ForRecord(record with{Length=length-1},data),null);Caption(PlacedThingCaptionRules.ForRecord(record with{Category="Project.ACTION"},data),null);
            Set("node.type","255");Caption(PlacedThingCaptionRules.ForRecord(record,data),null);projectCases++;
        }
        foreach(var row in fixture.RootElement.GetProperty("Native").EnumerateArray())
        {
            var structure=row.GetProperty("Structure").GetString()!;var fields=LegacySchemas.Fields(structure);var length=LegacySchemas.Size(structure);var bytes=new byte[56+length];var file=CreateFixtureFile(0xBE,1,bytes);var record=new NativeRecord(file,56,length,structure,fields);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(record.Offset),row.GetProperty("Header").GetUInt32());BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(record.Offset+4),row.GetProperty("PayloadBytes").GetUInt32());
            Expect(length-8==row.GetProperty("PayloadBytes").GetInt32(),"Source payload size not record-length minus header.");var before=bytes.ToArray();
            foreach(var field in fields)
            {
                var expected=field.Name=="bGlobalPlayer"?row.GetProperty("PlayerKey").GetString():field.Name=="bDoorLocked"?"PlacedThing.DoorState":field.Name=="bHeroOptions"?"Batch.HeroOptions":null;
                Caption(PlacedThingCaptionRules.ForRecord(record,field),expected);nativeCases++;
            }
            Expect(bytes.AsSpan().SequenceEqual(before),"Native caption changed record bytes.");
            Expect(!fields.Any(f=>f.Name==row.GetProperty("SizeField").GetString()),"Structural size member became editable.");
            var sample=fields.First(); // GAMENEUTRAL has implicit ownership and no player byte.
            Caption(PlacedThingCaptionRules.ForRecord(record with{Offset=-1},sample),null);Caption(PlacedThingCaptionRules.ForRecord(record with{Length=length-1},sample),null);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(record.Offset+4),255);Caption(PlacedThingCaptionRules.ForRecord(record,sample),null);
        }
        {
            var fields=LegacySchemas.Fields("GAMEBANDHERO");var bytes=new byte[96];var file=CreateFixtureFile(0xBE,1,bytes);var record=new NativeRecord(file,64,32,"BandHero 1",fields);var field=fields.Single(f=>f.Name=="bGlobalPlayer");
            Caption(PlacedThingCaptionRules.ForRecord(record,field),"Field.Enemy");Caption(PlacedThingCaptionRules.ForRecord(record with{Category="BandHero invalid",Fields=fields.Take(2).ToArray()},field),null);
        }
        foreach(var native in new[]{true,false})
        {
            var d=NativeMapDocument.CreateNewMap(new("Captions","Captions",20,20,NativeFormat:native));
            var kinds=native?new[]{"GAMEKEEPER","GAMEHERO","GAMENEUTRAL","GAMEDOOR","GAMETRAP","GAMEOBJECT"}:new[]{"Creature","Door","Trap","Object","MagicalObject"};
            foreach(var kind in kinds)if(native)d.AddRecord(RecordTemplate.All.Single(t=>t.Structure==kind),new(2,2));else d.AddLegacyRecord(kind,new(2,2));
            var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo);var hits=0;
            foreach(var record in d.Records())foreach(var field in record.Fields)if(PlacedThingCaptionRules.ForRecord(record,field) is {} key){keys.Add(key);hits++;}
            Expect(hits>=5,"Real document records do not use contextual caption route.");
            Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo),"Caption browsing changed document/history.");
        }
        foreach(var key in keys)foreach(var language in resources.Keys)Expect(resources[language].TryGetValue(key,out var value)&&!string.IsNullOrWhiteSpace(value),"Missing EN/RU contextual caption: "+key);
        Console.WriteLine($"PLACED THING CAPTIONS DETAIL50 template type/ID cases/{projectCases} project cases across6 versions/{nativeCases} fields across6 native layouts/band enemy/player-owner distinction/raw retained values/EN+RU/native+DKM read-only map bytes/history; no WPF execution.");
    }
}
