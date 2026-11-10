using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class DefinitionCaptionRouteChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(DefinitionCaptionRouteChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.DefinitionCaptionRoutesSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Definition caption source changed.");
        var resources=new[]{"en","ru"}.ToDictionary(language=>language,language=>XDocument.Load(Path.Combine(root,$"src/DK2MapEditor.App/Localization/Strings.{language}.xaml")).Root!.Elements().Where(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml")) is not null).ToDictionary(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml"))!.Value,e=>e.Value));
        void Caption(DefinitionCaption? caption,string key,string? argument=null,string? prefix=null)
        {
            Expect(caption==new DefinitionCaption(key,argument,prefix),"Caption lost source family/slot/parameter identity: "+key);
            foreach(var language in resources.Keys)
            {
                var text=caption!.Render(k=>resources[language][k]);Expect(text.Length>0&&!text.Contains("{0}"),"Caption missing text or unformatted slot.");
            }
        }
        foreach(var row in fixture.RootElement.GetProperty("StaticCaptions").EnumerateArray())Caption(DefinitionCaptionRules.For(row.GetProperty("Family").GetString()!,row.GetProperty("Field").GetString()!),row.GetProperty("Key").GetString()!);
        foreach(var row in fixture.RootElement.GetProperty("CreatureSlotCaptions").EnumerateArray())Caption(DefinitionCaptionRules.For("Creatures",row.GetProperty("Field").GetString()!),row.GetProperty("Key").GetString()!,row.GetProperty("Argument").ValueKind==JsonValueKind.Null?null:row.GetProperty("Argument").GetString());
        Caption(DefinitionCaptionRules.For("Traps","TriggerData",_=>"1"),"TrapProperties.TriggerAreaOfEffect");Caption(DefinitionCaptionRules.For("Traps","TriggerData",_=>"2"),"TrapProperties.TriggerRequiredPressure");
        foreach(var value in new[]{"3","0","255","invalid"})Expect(DefinitionCaptionRules.For("Traps","TriggerData",_=>value) is null,"Unknown trigger data assigned a guessed meaning.");
        var projectileCases=0;
        foreach(var family in new[]{"Traps","CrSpells","Spells"})foreach(var upgrade in family=="Spells"?new[]{false,true}:new[]{false})foreach(var row in fixture.RootElement.GetProperty("ProjectileOptions").EnumerateArray())foreach(var second in new[]{false,true})
        {
            var typeField=upgrade?"UpgShotType":family=="CrSpells"?"Shot":"ShotType";var field=(upgrade?"Upg":"")+"ShotData"+(second?"2":"1");var id=row.GetProperty("ID").GetUInt32().ToString();var form=row.GetProperty("Form").GetInt32();
            var actual=DefinitionCaptionRules.For(family,field,name=>name==typeField?id:null);
            if(form==255||second&&form==1){Expect(actual is null,"Inactive source parameter received an invented active caption.");continue;}
            var suffix=second?form switch{2=>"Count",3=>"Level",4=>"Gold",_=>"Label2."+row.GetProperty("Label2").GetInt32()}:form switch{2 or 3=>"Creature",4=>"Object",_=>"Label1."+row.GetProperty("Label1").GetInt32()};
            Caption(actual,"Editor.Definitions.Projectile."+suffix,prefix:"Editor.Definitions.Projectile."+(upgrade?"Upgrade":"Shot"));projectileCases++;
        }
        foreach(var family in new[]{"Terrains","Rooms","Doors","Creatures","Unknown"})Expect(DefinitionCaptionRules.For(family,"ShotData1",_=>"3") is null,"Unrelated family received a projectile parameter label.");
        foreach(var field in new[]{"Param1","Opaque[3]","Unknown","Res5","Special3"})Expect(DefinitionCaptionRules.For("Creatures",field) is null,"Unknown field lost generic/array fallback.");
        var structures=new[]{("Terrains","GLOBALTERRAIN"),("Rooms","GLOBALROOM"),("Doors","GLOBALDOOR"),("Traps","GLOBALTRAP"),("Spells","GLOBALSPELL"),("Creatures","GLOBALCREATURE"),("CrSpells","GLOBALCRSPELLS")};var rawCases=0;var storedParameterCases=0;
        foreach(var native in new[]{true,false})foreach(var(family,structure)in structures)
        {
            var schema=LegacyGlobalSchemas.All.Single(s=>s.Group==family);var length=native?schema.RecordSize:LegacyProjectSchemas.Size(structure);var fields=native?schema.Fields:LegacyProjectSchemas.Fields(structure);var bytes=new byte[64+length];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes,native?schema.Signature:NativeMapDocument.LegacyGlobals);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),1);
            // Isolated managed fixture, no loading or modifying installed maps.
            var file=(NativeFile)Activator.CreateInstance(typeof(NativeFile),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{"caption-fixture","caption-fixture",bytes},null)!;
            var record=new NativeRecord(file,64,length,(native?"Definition."+family:"Project."+structure),fields);
            Expect(DefinitionCaptionRules.Family(record)==family,"Verified native/project definition family not recognized.");
            if(family is "Traps" or "Spells" or "CrSpells")foreach(var upgrade in family=="Spells"?new[]{false,true}:new[]{false})foreach(var row in fixture.RootElement.GetProperty("ProjectileOptions").EnumerateArray())
            {
                var type=upgrade?"UpgShotType":family=="CrSpells"?"Shot":"ShotType";var typeField=fields.Single(f=>f.Name==type);var id=row.GetProperty("ID").GetUInt32().ToString();typeField.Encode(id).CopyTo(bytes.AsSpan(record.Offset+typeField.Offset));
                foreach(var second in new[]{false,true})
                {
                    var field=fields.Single(f=>f.Name==(upgrade?"Upg":"")+"ShotData"+(second?"2":"1"));var before=bytes.ToArray();
                    Expect(DefinitionCaptionRules.ForRecord(record,field)==DefinitionCaptionRules.For(family,field.Name,name=>name==type?id:null),"Raw inspector read wrong projectile type/slot from stored record.");
                    Expect(bytes.AsSpan().SequenceEqual(before),"Stored projectile caption changed raw bytes.");storedParameterCases++;
                }
            }
            foreach(var row in fixture.RootElement.GetProperty("StaticCaptions").EnumerateArray().Where(r=>r.GetProperty("Family").GetString()==family))
            {
                var field=fields.SingleOrDefault(f=>f.Name==row.GetProperty("Field").GetString());if(field is null)continue;
                Caption(DefinitionCaptionRules.ForRecord(record,field),row.GetProperty("Key").GetString()!);rawCases++;
            }
            foreach(var row in fixture.RootElement.GetProperty("CreatureSlotCaptions").EnumerateArray().Where(_=>family=="Creatures"))
            {
                var field=fields.SingleOrDefault(f=>f.Name==row.GetProperty("Field").GetString());if(field is null)continue;
                Caption(DefinitionCaptionRules.ForRecord(record,field),row.GetProperty("Key").GetString()!,row.GetProperty("Argument").ValueKind==JsonValueKind.Null?null:row.GetProperty("Argument").GetString());rawCases++;
            }
            var snapshot=bytes.ToArray();foreach(var field in fields)_ = DefinitionCaptionRules.ForRecord(record,field);Expect(bytes.AsSpan().SequenceEqual(snapshot),"Raw caption reads changed record bytes.");
            Expect(DefinitionCaptionRules.Family(record with{Category="Project.ACTION"}) is null&&DefinitionCaptionRules.Family(record with{Offset=-1}) is null,"Unrelated or malformed record recognized as a definition.");
        }
        Expect(rawCases>=100,"Too few actual native/project schema fields covered.");
        foreach(var native in new[]{true,false})
        {
            var d=NativeMapDocument.CreateNewMap(new("Caption","Caption",20,20,NativeFormat:native));var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo);
            foreach(var row in d.GlobalDefinitionRows().DistinctBy(r=>r.Family))
            {
                var values=d.GlobalDefinitionValues(row).ToArray();string? Read(string name){var item=values.FirstOrDefault(v=>v.Name==name);return item?.Value.Length==0?item.DefaultValue:item?.Value;}
                foreach(var field in values)if(DefinitionCaptionRules.For(row.Family,field.Name,Read) is {} caption)foreach(var language in resources.Keys)_ = caption.Render(k=>resources[language][k]);
            }
            Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo),"Definition caption browsing changed map/history.");
        }
        Console.WriteLine($"DEFINITION CAPTION ROUTES DETAIL44 established context keys/13 creature slot captions/66 source projectile forms/{projectileCases} active parameter routes/{rawCases} actual native+project raw fields/{storedParameterCases} stored type+slot bindings/14 layouts/EN+RU/default-value route/read-only map bytes/history; no WPF execution.");
    }
}
