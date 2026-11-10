using DK2MapEditor.Core.Editing;

internal static class ProjectileChildInputChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException){return;}throw new Exception("Invalid projectile numeric input accepted.");}
    public static void Run()
    {
        var unsigned=GlobalProjectiles.Find("3")!;var signed=GlobalProjectiles.Find("4")!;var count=GlobalProjectiles.Find("2")!;var gold=GlobalProjectiles.Find("5")!;var level=GlobalProjectiles.Find("60")!;
        Expect(unsigned.Form==0&&!unsigned.Signed1&&!unsigned.Signed2&&signed.Form==1&&signed.Signed1&&count.Form==2&&gold.Form==4&&level.Form==3,"Literal source numeric examples changed.");
        foreach(var (option,second,minimum,maximum) in new[]{(unsigned,false,0,32767),(unsigned,true,0,32767),(signed,false,-32768,32767),(count,true,0,100),(gold,true,0,80000)})
        {
            Expect(GlobalProjectileInputs.IsNumber(option,second),"Source numeric field absent.");
            Expect(GlobalProjectileInputs.Normalize(option,second,(maximum+1)+".75")==maximum.ToString(),"Immediate source upper integer clamp differs.");
            Expect(GlobalProjectileInputs.Normalize(option,second,minimum<0?"-32769.5":"4294967295")==minimum.ToString(),"Signed approved lower clamp or unsigned LONG clamp differs.");
            foreach(var text in new[]{"","invalid","-",maximum+".75",minimum<0?"2147483648":"4294967296"})
                Expect(GlobalProjectileInputs.Normalize(option,second,text)==text,"Partial/in-range/overflow text changed before explicit step.");
            Expect(GlobalProjectileInputs.Step(option,second,"",1)=="1"&&GlobalProjectileInputs.Step(option,second,"2.75",1)=="3","Empty or whole-part source step differs.");
            Expect(GlobalProjectileInputs.Step(option,second,"0",-1)==(minimum<0?"-1":"0"),"Signed/unsigned zero step differs.");
            Expect(GlobalProjectileInputs.Step(option,second,maximum.ToString(),1)==maximum.ToString(),"Source upper step clamp lost.");
        }
        Expect(GlobalProjectileInputs.NormalizeTrigger("32768.75")=="32767"&&GlobalProjectileInputs.NormalizeTrigger("4294967295")=="0"
            &&GlobalProjectileInputs.StepTrigger("",1)=="1"&&GlobalProjectileInputs.StepTrigger("2.75",1)=="3"&&GlobalProjectileInputs.StepTrigger("0",-1)=="0","Trigger101/spin102 differs.");
        Expect(GlobalProjectileInputs.Step(signed,false,"-2147483648",-1)=="32767"&&GlobalProjectileInputs.Step(signed,false,"-32768",-1)=="-32768","Signed wrap or approved lower step differs.");
        Reject(()=>GlobalProjectileInputs.Step(level,true,"1",1));Reject(()=>GlobalProjectileInputs.Normalize(count,false,"1"));Reject(()=>GlobalProjectileInputs.Step(signed,true,"1",1));
        Reject(()=>GlobalProjectileInputs.Step(gold,true,"1",0));Reject(()=>GlobalProjectileInputs.StepTrigger("1",0));
        // Exercise the independent second signed flag even if the current GShots
        // table does not use it; source ShotProc has a separate signed2 branch.
        var signedSecond=unsigned with{Signed2=true};
        Expect(GlobalProjectileInputs.Normalize(signedSecond,true,"-32769")=="-32768"&&GlobalProjectileInputs.Step(signedSecond,true,"-1.5",1)=="0","Approved independent second signed control differs.");
        foreach(var native in new[]{false,true})
        {
            var directory=Path.Combine(Path.GetTempPath(),"ProjectileChildInput-"+Guid.NewGuid().ToString("N"));
            try
            {
                var doc=native?GlobalDefinitionNumericChecks.Fixture(directory):NativeMapDocument.CreateNewMap(new("ProjectileInput","ProjectileInput",20,20));
                doc.SetGlobalDefinitionValues([new("Traps",0,"TriggerType","1"),new("Traps",0,"TriggerData","0")]);
                foreach(var (type,creatureContext) in from t in new[]{3u,4u,2u,5u} from c in new[]{false,true} select (t,c))
                {
                    var original=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var changes=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                    void Exact()=>Expect(original.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==changes&&doc.GetHistory().Undo.Count==history,"Private child/Cancel/Undo changed live state.");
                    void Edit(NativeMapDocument working)
                    {
                        foreach(var (family,slot) in new[]{("Traps","Shot"),("Spells","Shot"),("Spells","Upgrade"),("CrSpells","Shot")}.Where(s=>(s.Item1=="CrSpells")==creatureContext))
                        {
                            var typeField=slot=="Upgrade"?"UpgShotType":family=="CrSpells"?"Shot":"ShotType";
                            working.SetGlobalDefinitionValues([new(family,0,typeField,type.ToString())]);
                            var row=working.GlobalDefinitionRows().Single(r=>r.Family==family&&r.Index==0);var option=GlobalProjectiles.Find(type.ToString())!;
                            var first=type==4?GlobalProjectileInputs.Normalize(option,false,"-32769"):type==3?GlobalProjectileInputs.Normalize(option,false,"32768.75"):"1";
                            var second=type==3?GlobalProjectileInputs.Step(option,true,"",1):type==2?GlobalProjectileInputs.Normalize(option,true,"101"):type==5?native?"65535":GlobalProjectileInputs.Normalize(option,true,"80001"):"0";
                            switch(family)
                            {
                                case "Traps":
                                    var trap=working.CreateGlobalTrapDraft(row);trap=working.UpdateGlobalTrapTriggerDraft(trap,GlobalProjectileInputs.NormalizeTrigger("32768"));
                                    working.ApplyGlobalTrapDraft(working.UpdateGlobalTrapShotDraft(trap,first,second));break;
                                case "Spells":working.ApplyGlobalSpellDraft(working.UpdateGlobalSpellShotDraft(working.CreateGlobalSpellDraft(row),slot,first,second));break;
                                default:working.ApplyCreatureSpellDraft(working.UpdateCreatureSpellShotDraft(working.CreateCreatureSpellDraft(row),slot,first,second));break;
                            }
                            var state=working.GlobalProjectile(row,slot);
                            Expect(state.Data1==(type==4?native?"32768":"-32768":type==3?"32767":"1"),"Independent child signed bits/first numeric value differ.");
                            if(type!=4)Expect(state.Data2==second,"Independent second numeric child value differs.");
                            if(native&&type==5)
                            {
                                var stored=working.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
                                Reject(()=>working.SetEditorGlobalProjectile(family,0,slot,5,first,GlobalProjectileInputs.Normalize(option,true,"80001")));
                                Expect(stored.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Source80000 UI clamp bypassed native WORD width preflight.");
                            }
                        }
                    }
                    void CheckParent(NativeMapDocument working,Action apply)
                    {
                        Edit(working);Exact();apply();
                        Expect(doc.EditorChangeCount==changes+1&&doc.GetHistory().Undo.Count==history+1,"Numeric children split common Undo.");
                        doc.Undo();Exact();doc.Redo();doc.Undo();Exact();
                    }
                    if(creatureContext)
                    {
                        using(var session=doc.CreateCreatureGlobalsSession())CheckParent(session.WorkingDocument,()=>doc.ApplyCreatureGlobals(doc.PrepareCreatureGlobals(session)));
                        using(var cancel=doc.CreateCreatureGlobalsSession())Edit(cancel.WorkingDocument);
                    }
                    else
                    {
                        using(var session=doc.CreateGlobalVariablesSession())CheckParent(session.WorkingDocument,()=>doc.ApplyGlobalVariables(doc.PrepareGlobalVariables(session)));
                        using(var cancel=doc.CreateGlobalVariablesSession())Edit(cancel.WorkingDocument);
                    }
                    Exact();
                }
            }
            finally
            {
                if(Directory.Exists(directory))
                {
                    var full=Path.GetFullPath(directory);Expect(full.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(full).StartsWith("ProjectileChildInput-",StringComparison.Ordinal),"Unexpected cleanup target.");Directory.Delete(full,true);
                }
            }
        }
        Console.WriteLine("PROJECTILE CHILD INPUT DETAIL TypeProc101/spin102; ShotProc101/103/203/205 whole-part signed/unsigned clamp/step; approved own-field spinner and signed minimum; four slots native/DKM exact values, native WORD preflight and private/common oneUndo/Redo/Cancel; no GUI/game");
    }
}
