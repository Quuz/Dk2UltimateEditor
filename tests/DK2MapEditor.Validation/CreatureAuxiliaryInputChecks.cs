using DK2MapEditor.Core.Editing;

internal static class CreatureAuxiliaryInputChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or OverflowException or FormatException){return;}throw new Exception("Invalid auxiliary input accepted.");}
    public static void Run()
    {
        (string Page,string Field,int Maximum,bool Fractional)[] fields=[("Possession","SpecialAb1Data",32767,false),("Possession","SpecialAb2Data",32767,false),
            ("Possession","WaddleScale",1,true),("Possession","OscillateScale",1,true),("Possession","WalkScale",12,false),
            ("CrSpells","RechargeTime",1024,true),("CrSpells","Range",1024,true)];
        foreach(var (page,field,maximum,fractional) in fields)
        {
            var suffix=fractional?".0":"";
            Expect(CreatureAuxiliaryInputs.Handles(page,field),"Missing one of7 source controls.");
            Expect(CreatureAuxiliaryInputs.Normalize(page,field,(maximum+1)+".75")==maximum+suffix,"Upper immediate clamp differs.");
            Expect(CreatureAuxiliaryInputs.Normalize(page,field,"4294967295")=="0"+suffix,"Unsigned UINT/LONG clamp differs.");
            foreach(var text in new[]{"","-","invalid",maximum+".75","4294967296"})Expect(CreatureAuxiliaryInputs.Normalize(page,field,text)==text,"Partial/in-range/overflow text was rewritten.");
            Expect(CreatureAuxiliaryInputs.Step(page,field,"",1,false)=="1","Unsigned empty spin differs.");
            Expect(CreatureAuxiliaryInputs.Step(page,field,"0",-1,false)=="0"+suffix,"Unsigned zero spin differs.");
            Expect(CreatureAuxiliaryInputs.Step(page,field,maximum+".75",1,false)==maximum+suffix,"Source upper spin differs.");
            Expect(CreatureAuxiliaryInputs.Step(page,field,"4294967295",1,false)=="0","UINT increment did not wrap through source getter/setter.");
        }
        Expect(CreatureAuxiliaryInputs.Step("CrSpells","Range","15.75",1,false)=="16"&&CreatureAuxiliaryInputs.Step("CrSpells","Range","15.75",1,true)=="15","Native WORD spin guard changed.");
        Reject(()=>CreatureAuxiliaryInputs.Normalize("CrSpells","Shot","1025"));Reject(()=>CreatureAuxiliaryInputs.Step("Possession","WalkScale","1",0,false));
        foreach(var native in new[]{false,true})
        {
            var directory=Path.Combine(Path.GetTempPath(),"AuxiliaryInput-"+Guid.NewGuid().ToString("N"));
            try
            {
                var doc=native?GlobalDefinitionNumericChecks.Fixture(directory):NativeMapDocument.CreateNewMap(new("AuxiliaryInput","AuxiliaryInput",20,20));
                var initial=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Auxiliary private/cancel/Undo changed live state.");
                using(var parent=doc.CreateCreatureGlobalsSession())
                {
                    var working=parent.WorkingDocument;var row=working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var possession=working.CreateCreatureOptionsDraft(row,"Possession");
                    var updated=working.UpdateCreatureOptions(possession,CreatureOptions.Fields("Possession").Select(field=>new CreatureOptionInput(field,
                        CreatureAuxiliaryInputs.Handles("Possession",field)?CreatureAuxiliaryInputs.Normalize("Possession",field,field=="WalkScale"?"13":field is "WaddleScale" or "OscillateScale"?"2.5":"40000"):possession.Display(field))));
                    working.ApplyCreatureOptions(updated);
                    Expect(updated.Raw("SpecialAb1Data")=="32767"&&updated.Raw("SpecialAb2Data")=="32767"&&updated.Raw("WaddleScale")=="4096"&&updated.Raw("OscillateScale")=="4096"&&updated.Raw("WalkScale")=="12","Possession source values encoded incorrectly.");Exact();
                    var spell=working.CreateCreatureSpellDraft(working.GlobalDefinitionRows().Single(r=>r.Family=="CrSpells"&&r.Index==0));
                    var inputs=CreatureSpellControls.MainFields.Select(field=>new CreatureSpellInput(field,CreatureAuxiliaryInputs.Handles("CrSpells",field)?CreatureAuxiliaryInputs.Normalize("CrSpells",field,native?"1.75":"1025.75"):spell.Display(field)));
                    var prepared=working.UpdateCreatureSpellDraft(spell,inputs);working.ApplyCreatureSpellDraft(prepared);
                    Expect(prepared.Raw("Range")== (native?"7168":"4194304")&&prepared.Raw("RechargeTime")== (native?"7168":"4194304"),"CrSpells fixed-point/source limit differs.");Exact();
                    if(native)Reject(()=>working.UpdateCreatureSpellDraft(working.CreateCreatureSpellDraft(working.GlobalDefinitionRows().Single(r=>r.Family=="CrSpells"&&r.Index==0)),CreatureSpellControls.MainFields.Select(field=>new CreatureSpellInput(field,field=="Range"?CreatureAuxiliaryInputs.Normalize("CrSpells",field,"1025"):prepared.Display(field)))));
                    doc.ApplyCreatureGlobals(doc.PrepareCreatureGlobals(parent));Expect(doc.EditorChangeCount==count+1&&doc.GetHistory().Undo.Count==history+1,"Two private pages split common Undo.");doc.Undo();Exact();doc.Redo();doc.Undo();Exact();
                }
                using(var canceled=doc.CreateCreatureGlobalsSession())
                {
                    var working=canceled.WorkingDocument;var possession=working.CreateCreatureOptionsDraft(working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0),"Possession");
                    working.ApplyCreatureOptions(working.UpdateCreatureOptions(possession,CreatureOptions.Fields("Possession").Select(field=>new CreatureOptionInput(field,field=="WalkScale"?CreatureAuxiliaryInputs.Normalize("Possession",field,"13"):possession.Display(field)))));
                }
                Exact();
                doc.SetGlobalDefinitionValues([new("Creatures",0,"SpecialAb1Data","65535"),new("Creatures",0,"WaddleScale","65535"),new("CrSpells",0,"Range",native?"65535":"-1")]);
                var imported=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var retained=doc.CreateCreatureOptionsDraft(doc.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0),"Possession");
                doc.ApplyCreatureOptions(doc.UpdateCreatureOptions(retained,CreatureOptions.Fields("Possession").Select(field=>new CreatureOptionInput(field,retained.Display(field)))));
                var retainedSpell=doc.CreateCreatureSpellDraft(doc.GlobalDefinitionRows().Single(r=>r.Family=="CrSpells"&&r.Index==0));
                doc.ApplyCreatureSpellDraft(doc.UpdateCreatureSpellDraft(retainedSpell,CreatureSpellControls.MainFields.Select(field=>new CreatureSpellInput(field,retainedSpell.Display(field)))));
                Expect(imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"No-op repaired imported auxiliary values.");doc.Undo();Exact();
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        Console.WriteLine("AUXILIARY INPUT DETAIL Possession5 and CrSpells2 literal controls; unsigned whole-part clamp/spin/wrap/partial text, native WORD guard, both families private common oneUndo/Redo/Cancel and imported no-op; no GUI/game");
    }
}
