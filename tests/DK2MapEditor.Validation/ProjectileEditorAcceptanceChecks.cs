using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class ProjectileEditorAcceptanceChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> snapshot)=>Expect(snapshot.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Projectile source UI draft/Undo modified unexpected buffers.");
    private static (string Type,string First,string Second) Fields(string family,string slot)
        =>slot=="Upgrade"?("UpgShotType","UpgShotData1","UpgShotData2"):(family=="CrSpells"?"Shot":"ShotType","ShotData1","ShotData2");
    public static void Run(NativeMapDocument native,NativeMapDocument project)
    {
        using var source=JsonDocument.Parse(typeof(ProjectileEditorAcceptanceChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ProjectileEditorSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var hash in source.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,hash.Name)))).Equals(hash.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Original projectile source changed.");
        Expect(source.RootElement.GetProperty("UnknownTargetSelectsFirst").GetBoolean()&&source.RootElement.GetProperty("InvalidLevelSelectsOne").GetBoolean()
            &&source.RootElement.GetProperty("NonGoldAcceptanceClearsSecond").GetBoolean(),"Source option acceptance fixture differs.");
        var previews=0;
        foreach(var option in GlobalProjectiles.Options)
        foreach(var first in new[]{"1","4","999","-1","65535"})
        foreach(var second in new[]{"0","1","10","11"})
        {
            var actual=GlobalProjectiles.EditorInputs(option,first,second);
            var expectedFirst=option.Form is 2 or 3 or 4 && first is not ("1" or "4")?"1":first;
            var expectedSecond=option.Form==3&&second is "0" or "11"?"1":second;
            Expect(actual.Data1==expectedFirst&&actual.Data2==expectedSecond,"Independent source list/level initialization differs.");
            previews++;
        }
        Expect(previews==1320,"All source projectile preview combinations were not evaluated.");
        var accepted=0;
        foreach(var document in new[]{native,project})
        foreach(var slot in new[]{("Traps","Shot"),("Spells","Shot"),("Spells","Upgrade"),("CrSpells","Shot")})
        foreach(var type in new[]{2u,60u,5u})
        foreach(var unknown in new[]{false,true})
        foreach(var variant in new[]{false,true})
        {
            var family=slot.Item1;var field=Fields(family,slot.Item2);
            var row=document.GlobalDefinitionRows().Single(row=>row.Family==family&&row.Index==0);
            var initial=Snapshot(document);
            var first=unknown?document.IsLegacyProject?"-1":"65535":"4";
            var second=type==60?variant?"11":"10":variant?"234":"23";
            document.SetGlobalDefinitionValues([new(family,0,field.Type,type.ToString()),new(family,0,field.First,first),new(family,0,field.Second,second)]);
            var stored=Snapshot(document);var counter=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;var usage=document.ReadActivityStatistics()!.Encode();
            var inputs=GlobalProjectiles.EditorInputs(GlobalProjectiles.Find(type.ToString())!,first,second);
            Expect(inputs.Data1==(unknown?"1":"4")&&inputs.Data2==(type==60&&variant?"1":second),"Original imported target/level fallback differs.");
            // Opening and cancelling a child leave every map byte untouched.
            Exact(stored);Expect(document.EditorChangeCount==counter&&document.GetHistory().Undo.Count==history,"Opening source options wrote map history/count.");
            var expectedSecond=type==5&&!unknown?"0":inputs.Data2;
            var changed=inputs.Data1!=first||expectedSecond!=second;
            document.SetEditorGlobalProjectile(family,0,slot.Item2,type,inputs.Data1,inputs.Data2);
            var state=document.GlobalProjectile(row,slot.Item2);
            Expect(state.Type==type.ToString()&&state.Data1==inputs.Data1&&state.Data2==expectedSecond,"Explicit source option acceptance did not store selected fallback/non-gold zero.");
            Expect(document.EditorChangeCount==unchecked(counter+(changed?1u:0u))&&document.GetHistory().Undo.Count==history+(changed?1:0)
                &&document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(usage),"Source options added child count/history/activity or missed atomic parent write.");
            if(changed){var after=Snapshot(document);document.Undo();Exact(stored);document.Redo();Exact(after);document.Undo();}
            // The nested source child returns a private draft; the common parent
            // remains responsible for publishing all composed fields together.
            switch(family)
            {
                case "Traps":
                    var trap=document.CreateGlobalTrapDraft(row);var trapChild=document.UpdateGlobalTrapShotDraft(trap,inputs.Data1,inputs.Data2);
                    Exact(stored);Expect(trapChild.Raw(field.First)==inputs.Data1&&trapChild.Raw(field.Second)==expectedSecond,"Trap child selected values differ.");
                    document.ApplyGlobalTrapDraft(trapChild);break;
                case "Spells":
                    var spell=document.CreateGlobalSpellDraft(row);var spellChild=document.UpdateGlobalSpellShotDraft(spell,slot.Item2,inputs.Data1,inputs.Data2);
                    Exact(stored);Expect(spellChild.Raw(field.First)==inputs.Data1&&spellChild.Raw(field.Second)==expectedSecond,"Spell child selected values differ.");
                    document.ApplyGlobalSpellDraft(spellChild);break;
                default:
                    var creature=document.CreateCreatureSpellDraft(row);var creatureChild=document.UpdateCreatureSpellShotDraft(creature,slot.Item2,inputs.Data1,inputs.Data2);
                    Exact(stored);Expect(creatureChild.Raw(field.First)==inputs.Data1&&creatureChild.Raw(field.Second)==expectedSecond,"Creature spell child selected values differ.");
                    document.ApplyCreatureSpellDraft(creatureChild);break;
            }
            Expect(document.GlobalProjectile(row,slot.Item2).Data2==expectedSecond,"Common parent did not publish accepted source options.");
            if(changed){document.Undo();Exact(stored);}
            document.Undo();Exact(initial);accepted++;
        }
        Expect(accepted==96,"Four source slots x two formats acceptance combinations differ.");
        Console.WriteLine("PROJECTILE EDITOR DETAIL 1320 source-form initialization combinations and 96 native/project x four-slot child/inline acceptance cases; first target/level1 fallback, non-gold zero, opening/cancel/private-parent isolation, exact counts/bytes/UndoRedo, inactive/raw routes retained");
    }
}
