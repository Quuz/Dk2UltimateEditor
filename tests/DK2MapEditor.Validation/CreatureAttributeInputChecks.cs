using DK2MapEditor.Core.Editing;

internal static class CreatureAttributeInputChecks
{
    private static void Expect(bool condition,string message){if(!condition)throw new Exception(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or FormatException or OverflowException){return;}throw new Exception("Invalid attribute input accepted.");}
    public static void Run()
    {
        string[] fractional=["Height","EyeHeight","DistanceCanSee","DistanceCanHear","Speed","RunSpeed","HungerRate","StunDuration","GuardDuration","IdleDuration","SlapDuration"];
        string[] signed=["HealthFromChicken","SlapDamage","TortureHealthCng","OwnLandHealthInc","PrayerMana","PossessionManaCost","AngerNoLair","AngerNoFood","AngerNoPay","AngerNoWork","AngerSlap","AngerInHand","TortureMoodCng"];
        string[] unsigned=["ExpPtsForNextLevel","ExpPts","ExpPtsTraining","ResearchPts","ManufacturePts","Fear","Threat","DecomposeValue","Health","Pay","InitialGoldHeld","MaxGoldHeld","TimeAwake","SleepDuration"];
        var all=fractional.Concat(signed).Concat(unsigned).ToArray();
        Expect(all.Length==38&&all.Order().SequenceEqual(CreaturePageControls.Fields("Attributes").Order()),"Literal source38 attributes do not match the page.");
        foreach(var field in all)
        {
            var isFloat=fractional.Contains(field);var isSigned=signed.Contains(field);
            Expect(CreatureAttributeInputs.Normalize(field,"40000.75")== (isFloat?"32767.0":"32767"),"Attribute upper EN_CHANGE differs: "+field);
            Expect(CreatureAttributeInputs.Normalize(field,"-40000")== (isSigned?"-40000":isFloat?"0.0":"0"),"Attribute lower EN_CHANGE differs: "+field);
            foreach(var value in new[]{"","-",".5","123","32767.75","2147483648","invalid"})
                Expect(CreatureAttributeInputs.Normalize(field,value)==value,"In-range/partial/overflow text was rewritten: "+field);
            Expect(CreatureAttributeInputs.Step(field,"",1,false)=="1","Empty signed whole-part spin differs.");
            Expect(CreatureAttributeInputs.Step(field,"invalid",-1,false)== (isSigned?"-1":isFloat?"0.0":"0"),"Failed getter spin differs.");
            Expect(CreatureAttributeInputs.Step(field,"32767.75",1,false)==(isFloat?"32767.0":"32767"),"Whole-part upper spin differs.");
            Expect(CreatureAttributeInputs.Step(field,"-2147483648",-1,false)==(isFloat?"32767.0":"32767"),"LONG underflow did not follow source wrap/clamp.");
            foreach(var native in new[]{false,true})Expect(CreaturePageControls.Step("Attributes",field,"1.75",1,native)=="2","Page spin did not route source whole part.");
        }
        Expect(CreatureAttributeInputs.Step("AngerNoFood","-32768",-1,false)=="-32769"&&CreatureAttributeInputs.Step("AngerNoFood","-32768",-1,true)=="-32768","Source project lower range/native guard changed.");
        Expect(CreatureAttributeInputs.Step("Speed","15.75",1,false)=="16"&&CreatureAttributeInputs.Step("Speed","15.75",1,true)=="15","Source project/native WORD width distinction changed.");
        Reject(()=>CreatureAttributeInputs.Normalize("Attract2Data","40000"));Reject(()=>CreatureAttributeInputs.Step("Health","1",0,false));
        foreach(var native in new[]{false,true})
        {
            var directory=Path.Combine(Path.GetTempPath(),"AttributeInput-"+Guid.NewGuid().ToString("N"));
            try
            {
                var doc=native?GlobalDefinitionNumericChecks.Fixture(directory):NativeMapDocument.CreateNewMap(new("AttributeInput","AttributeInput",20,20));
                var initial=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Attribute draft/cancel/Undo changed live bytes/history.");
                using(var session=doc.CreateCreatureGlobalsSession())
                {
                    var working=session.WorkingDocument;var row=working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var draft=working.CreateCreaturePageDraft(row,"Attributes");
                    var inputs=all.Select(field=>new CreaturePageInput(field,CreatureAttributeInputs.Normalize(field,field switch
                    {"Health"=>"40000","HungerRate"=>"40000.75","Speed"=>"1.75","AngerNoFood"=>"-9",_=>draft.Display(field)})));
                    working.ApplyCreaturePageDraft(working.UpdateCreaturePageDraft(draft,inputs));Exact();
                    var changed=working.CreateCreaturePageDraft(row,"Attributes");
                    Expect(changed.Raw("Health")=="32767"&&changed.Raw("HungerRate")=="134213632"&&changed.Raw("Speed")=="7168"&&changed.Display("AngerNoFood")=="-9","Attribute clamp/private storage differs.");
                    doc.ApplyCreatureGlobals(doc.PrepareCreatureGlobals(session));Expect(doc.GetHistory().Undo.Count==history+1&&doc.EditorChangeCount==count+1,"Attribute Apply split common history.");doc.Undo();Exact();doc.Redo();doc.Undo();Exact();
                }
                using(var session=doc.CreateCreatureGlobalsSession())
                {
                    var working=session.WorkingDocument;var row=working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var draft=working.CreateCreaturePageDraft(row,"Attributes");
                    var inputs=all.Select(field=>new CreaturePageInput(field,field=="Speed"?CreatureAttributeInputs.Normalize(field,"40000"):draft.Display(field))).ToArray();
                    if(native){Reject(()=>working.UpdateCreaturePageDraft(draft,inputs));Exact();}
                    else{working.ApplyCreaturePageDraft(working.UpdateCreaturePageDraft(draft,inputs));Exact();}
                }
                Exact();
                doc.SetGlobalDefinitionValues([new("Creatures",0,"Health",native?"65535":"100000"),new("Creatures",0,"TortureHealthCng","65527")]);
                var imported=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var retained=doc.CreateCreaturePageDraft(doc.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0),"Attributes");
                doc.ApplyCreaturePageDraft(doc.UpdateCreaturePageDraft(retained,all.Select(field=>new CreaturePageInput(field,retained.Display(field)))));
                Expect(imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Read/no-op repaired imported health or signed alias.");doc.Undo();Exact();
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        Console.WriteLine("ATTRIBUTE INPUT DETAIL38 literal fields (11 fixed/13 signed/14 unsigned), immediate source bounds, whole-part/empty/overflow/wrap spin, retained native WORD guard, private native/DKM Apply/oneUndo/Redo/Cancel and imported no-op; no GUI/game");
    }
}
