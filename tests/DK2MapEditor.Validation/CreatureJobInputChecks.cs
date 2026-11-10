using DK2MapEditor.Core.Editing;

internal static class CreatureJobInputChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new Exception("Invalid job input accepted.");}
    public static void Run()
    {
        foreach(var field in new[]{"Chance","Mood","Mana"})
        {
            var signed=field!="Chance";var maximum=signed?32767:100;
            Expect(CreatureJobInputs.Normalize(field,(maximum+1)+".75")==maximum.ToString(),"Source upper job input clamp differs.");
            Expect(CreatureJobInputs.Normalize(field,signed?"-32768":"4294967295")== (signed?"-32767":"0"),"Source signed/unsigned lower job clamp differs.");
            foreach(var text in new[]{"","-","invalid",maximum+".75",signed?"2147483648":"4294967296"})Expect(CreatureJobInputs.Normalize(field,text)==text,"Partial/in-range/overflow job text was rewritten.");
            Expect(CreatureJobInputs.Step(field,"",1)=="1"&&CreatureJobInputs.Step(field,"invalid",-1)==(signed?"-1":"0"),"Empty or failed getter job spin differs.");
            Expect(CreatureJobInputs.Step(field,"2.75",1)=="3"&&CreatureJobInputs.Step(field,maximum+".75",1)==maximum.ToString(),"Whole-part job step differs.");
        }
        Expect(CreatureJobInputs.Step("Mood","-1.75",1)=="0"&&CreatureJobInputs.Step("Mana","-2147483648",-1)=="32767","Signed job spin/wrap differs.");
        Expect(CreatureJobInputs.Step("Chance","2147483647",1)=="0"&&CreatureJobInputs.Step("Chance","4294967295",1)=="0","Unsigned job getter/setter roundtrip differs.");
        Reject(()=>CreatureJobInputs.Normalize("Job","1"));Reject(()=>CreatureJobInputs.Step("Chance","1",0));
        foreach(var native in new[]{false,true})
        {
            var directory=Path.Combine(Path.GetTempPath(),"JobInput-"+Guid.NewGuid().ToString("N"));
            try
            {
                var doc=native?GlobalDefinitionNumericChecks.Fixture(directory):NativeMapDocument.CreateNewMap(new("JobInput","JobInput",20,20));
                var initial=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Job input/private/Cancel/Undo changed live state.");
                using(var session=doc.CreateCreatureGlobalsSession())
                {
                    var working=session.WorkingDocument;var row=working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var draft=working.CreateCreatureJobsDraft(row);
                    foreach(var group in CreatureJobs.Groups)draft=working.RemoveCreatureJobs(draft,group.ID,draft.Slots(group.ID).Select((slot,index)=>(slot,index)).Where(p=>p.slot.Job!="0").Select(p=>p.index));
                    foreach(var group in CreatureJobs.Groups.Where(g=>g.HasParameters))
                    {
                        var id=CreatureJobs.Allowed(group.ID).First().ID;
                        var before=draft.Slots(group.ID).ToArray();
                        var chance=CreatureJobInputs.Normalize("Chance","101");var mood=CreatureJobInputs.Normalize("Mood","-40000");var mana=CreatureJobInputs.Normalize("Mana","40000");
                        Expect(draft.Slots(group.ID).SequenceEqual(before),"Text-only normalization submitted a job tuple.");Exact();
                        draft=working.AddCreatureJob(draft,group.ID,id,chance,mood,mana);
                        var added=draft.Slots(group.ID)[0];
                        Expect(added.Chance=="100"&&added.Mood==(native?"32769":"-32767")&&added.Mana=="32767","Clamped Add tuple bits differ.");
                        draft=working.ChangeCreatureJob(draft,group.ID,0,id.ToString(),CreatureJobInputs.Step("Chance","99",1),CreatureJobInputs.Step("Mood","-32767",1),CreatureJobInputs.Step("Mana","32767",1));
                        var changed=draft.Slots(group.ID)[0];Expect(changed.Chance=="100"&&changed.Mood==(native?"32770":"-32766")&&changed.Mana=="32767","Clamped Change tuple bits differ.");
                    }
                    Expect(draft.Totals.All(t=>t.Total==100&&!t.NeedsAcknowledgement),"Clamped Add/Change totals not source100.");
                    working.ApplyCreatureJobs(draft);Exact();doc.ApplyCreatureGlobals(doc.PrepareCreatureGlobals(session));
                    Expect(doc.EditorChangeCount==count+1&&doc.GetHistory().Undo.Count==history+1,"Job controls split common Undo.");doc.Undo();Exact();doc.Redo();doc.Undo();Exact();
                }
                using(var canceled=doc.CreateCreatureGlobalsSession())
                {
                    var working=canceled.WorkingDocument;var draft=working.CreateCreatureJobsDraft(working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));
                    draft=working.RemoveCreatureJobs(draft,"Happy",draft.Slots("Happy").Select((s,i)=>(s,i)).Where(p=>p.s.Job!="0").Select(p=>p.i));
                    draft=working.AddCreatureJob(draft,"Happy",CreatureJobs.Allowed("Happy").First().ID,CreatureJobInputs.Normalize("Chance","101"),"0","0");
                    working.ApplyCreatureJobs(draft,true);
                }
                Exact();
                doc.SetGlobalDefinitionValues([new("Creatures",0,"HJobs[0]",CreatureJobs.Allowed("Happy").First().ID.ToString()),new("Creatures",0,"HJobsChance[0]","255"),new("Creatures",0,"HJobsMood[0]","32768"),new("Creatures",0,"HJobsMana[0]","65527")]);
                var imported=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var retained=doc.CreateCreatureJobsDraft(doc.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));var slot=retained.Slots("Happy")[0];
                doc.ApplyCreatureJobs(doc.ChangeCreatureJob(retained,"Happy",0,slot.Job,slot.Chance,CreatureJobs.DisplaySigned(slot.Mood),CreatureJobs.DisplaySigned(slot.Mana)),true);
                Expect(imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"No-op Change repaired imported chance/signed alias.");doc.Undo();Exact();
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        Console.WriteLine("JOB INPUT DETAIL9 source controls/3 selected-group fields:chance unsigned0..100,mood/mana signed±32767; partial/whole/wrap spin, no implicit submission, all3 groups native/DKM Add/Change exact tuples/private common oneUndo/Redo/Cancel/imported no-op; no GUI/game");
    }
}
