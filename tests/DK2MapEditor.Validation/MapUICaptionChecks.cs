using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class MapUICaptionChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var source=JsonDocument.Parse(typeof(MapUICaptionChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.MapUICaptionsSource.json")!);
        foreach(var input in source.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Map caption source changed.");
        var expected=source.RootElement.GetProperty("SemanticCaptions").EnumerateArray().ToDictionary(r=>r.GetProperty("Field").GetString()!,r=>new MapMetadataCaption(r.GetProperty("Key").GetString()!,r.GetProperty("Ordinal").ValueKind==JsonValueKind.Null?null:r.GetProperty("Ordinal").GetInt32()));
        Expect(expected.Count==43,"Incomplete map caption scope.");
        var resources=new[]{"en","ru"}.ToDictionary(language=>language,language=>XDocument.Load(Path.Combine(root,$"src/DK2MapEditor.App/Localization/Strings.{language}.xaml")).Root!.Elements().Where(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml")) is not null).ToDictionary(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml"))!.Value,e=>e.Value));
        foreach(var(field,caption)in expected)
        {
            Expect(MapMetadataCaptions.ForSemantic(field)==caption,"Map field lost original dialog context: "+field);
            foreach(var language in resources.Keys)
            {var text=caption.Render(k=>resources[language][k]);Expect(text.Length>0&&!text.Contains("{0}"),"Missing/unformatted map caption.");if(caption.Ordinal is {} ordinal)Expect(text.Contains(ordinal.ToString()),"Appearance override ordinal lost.");}
        }
        foreach(var field in new[]{"Options","Options2","TextIntroCreatureID8","TextIntroTextID-1","TextIntroTextID99","Description","Unknown"})Expect(MapMetadataCaptions.ForSemantic(field) is null,"Unrelated field/whole flags byte got guessed caption.");
        var projects=0;
        for(uint version=1;version<=7;version++)
        {
            var structure=version switch{1=>"MAPPROPERTIESV1",2 or 3=>"MAPPROPERTIESV2",4=>"MAPPROPERTIESV4",5=>"MAPPROPERTIESV5",6=>"MAPPROPERTIESV6",_=>"MAPPROPERTIES"};var fields=LegacyProjectSchemas.Fields(structure);var length=LegacyProjectSchemas.Size(structure);var bytes=new byte[length+12];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes,NativeMapDocument.LegacyInfo);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),version);
            var file=(NativeFile)Activator.CreateInstance(typeof(NativeFile),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{"private-map-caption","private-map-caption",bytes},null)!;
            var record=new NativeRecord(file,12,length,"Project."+structure,fields);var before=bytes.ToArray();
            foreach(var field in fields)
            {
                Expect(MapMetadataCaptions.ForRecord(record,field)==expected.GetValueOrDefault(field.Name),"Historical/current field mapped to absent or different UI semantic caption.");
                if(expected.ContainsKey(field.Name))projects++;
            }
            Expect(fields.Any(f=>f.Name=="Version")== (version>=6),"Target game version unexpectedly stored in old layout.");
            Expect(bytes.AsSpan().SequenceEqual(before),"Caption read upgraded historical metadata.");
            var sample=fields.First(f=>expected.ContainsKey(f.Name));Expect(MapMetadataCaptions.ForRecord(record with{Length=length-1},sample) is null,"Truncated metadata got context caption.");
        }
        var natives=0;
        foreach(var native in new[]{true,false})
        {
            var d=NativeMapDocument.CreateNewMap(new("MapCaption","MapCaption",20,20,NativeFormat:native));var record=d.Records().Single(r=>native?r.Category=="Map":r.File.Signature==NativeMapDocument.LegacyInfo);var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo);
            foreach(var field in record.Fields)
            {
                var semantic=native?LegacyCompilerRules.Metadata.FirstOrDefault(m=>m.Target==field.Name)?.Source:field.Name;var wanted=semantic is null?null:expected.GetValueOrDefault(semantic);
                Expect(MapMetadataCaptions.ForRecord(record,field)==wanted,"Actual map field caption not connected: "+field.Name);if(native&&wanted is not null)natives++;
            }
            Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo),"Map caption browsing changed bytes/history.");
            var sample=record.Fields.First();Expect(MapMetadataCaptions.ForRecord(record with{Offset=-1},sample) is null&&MapMetadataCaptions.ForRecord(record with{Category="Project.ACTION"},sample) is null,"Malformed/unrelated record got map caption.");
        }
        Expect(natives>30,"Native metadata caption routes not exercised.");
        Console.WriteLine($"MAP UI CAPTIONS DETAIL8 source pages/43 semantic fields/8 appearance override pairs/{projects} stored fields across7project versions/{natives} native fields/EN+RU/read-only bytes/history; target version not invented in native metadata; no WPF execution.");
    }
}
