using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class SpatialCustomCaptionChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;
        var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var source=JsonDocument.Parse(typeof(SpatialCustomCaptionChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.SpatialCustomCaptionsSource.json")!);
        foreach(var input in source.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Spatial caption source changed.");
        var keys=new HashSet<string>{"Editor.EffectGenerator.Often","Editor.EffectGenerator.Rarely"};var cases=0;
        foreach(var native in new[]{true,false})
        {
            var d=NativeMapDocument.CreateNewMap(new("SpatialCaption","SpatialCaption",20,20,NativeFormat:native));
            NativeRecord Add(string nativeKind,string projectKind)=>native?d.AddRecord(RecordTemplate.All.Single(t=>t.Structure==nativeKind),new(2,2)):d.AddLegacyRecord(projectKind,new(2,2));
            var area=Add("GAMEAREA","Area");var effect=Add("GAMEEFFECT","Effect");var gate=Add("GAMEGATE","Gate");
            var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo);
            foreach(var(name,key) in new[]{(native?"wAreaWaitDelay":"waitdelay","RegionProperties.PauseSeconds"),(native?"bAreaNextID":"nextid","Field.NextID")})
            {
                var field=area.Fields.Single(f=>f.Name==name);Expect(SpatialCaptionRules.ForRecord(area,field)==key,"Area raw caption loses seconds or linked area identity.");keys.Add(key);cases++;
            }
            foreach(var fieldName in EffectGenerators.Fields)
            {
                var field=effect.Fields.Single(f=>f.Name==EffectGenerators.StorageField(effect,fieldName));var key="Editor.EffectGenerator."+fieldName;
                Expect(SpatialCaptionRules.ForRecord(effect,field)==key,"Effect raw field lost ordinal or frequency context.");keys.Add(key);cases++;
                Expect(SpatialCaptionRules.ForRecord(effect with{Length=effect.Length-1},field) is null,"Truncated effect record got context caption.");
            }
            foreach(var record in new[]{area,effect,gate})foreach(var field in record.Fields)_ = SpatialCaptionRules.ForRecord(record,field);
            foreach(var field in gate.Fields)Expect(SpatialCaptionRules.ForRecord(gate,field) is null,"Gate inherited area/effect-only labels.");
            Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo),"Spatial captions changed map/history.");
        }
        foreach(var language in new[]{"en","ru"})
        {
            var strings=XDocument.Load(Path.Combine(root,$"src/DK2MapEditor.App/Localization/Strings.{language}.xaml")).Root!.Elements().Where(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml")) is not null).ToDictionary(e=>e.Attribute(XName.Get("Key","http://schemas.microsoft.com/winfx/2006/xaml"))!.Value,e=>e.Value);
            foreach(var key in keys)Expect(strings.TryGetValue(key,out var text)&&!string.IsNullOrWhiteSpace(text),"Missing spatial EN/RU caption: "+key);
            Expect(strings["Editor.EffectGenerator.Often"].Contains("(1)")&&strings["Editor.EffectGenerator.Rarely"].Contains("(255)"),"Frequency end captions lost direction.");
        }
        Console.WriteLine($"SPATIAL CUSTOM CAPTIONS DETAIL6 source dialogs/4area flags/source scale1 often–255 very rarely/patrol seconds/{cases} native+DKM raw bindings/gates excluded/read-only bytes/history/EN+RU; no WPF execution.");
    }
}
