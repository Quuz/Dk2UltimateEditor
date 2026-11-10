using DK2MapEditor.Core.Editing;

internal static class CreatureAttackInputChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action)
    {try{action();}catch(ArgumentException){return;}throw new Exception("Invalid attack callback accepted.");}
    public static void Run()
    {
        var source=new Dictionary<string,(int Maximum,bool Signed,bool Fractional)>
        { ["RechargeTime"]=(255,false,true),["Range"]=(1000,false,true),["Dammage"]=(32767,false,false),["Delay"]=(1000,false,true),
          ["Shot1Delay"]=(1000,false,true),["Shot2Delay"]=(1000,false,true),["Shot3Delay"]=(1000,false,true),
          ["Shot1X"]=(32767,true,true),["Shot1Y"]=(32767,true,true),["Shot1Z"]=(32767,true,true),
          ["Shot2X"]=(32767,true,true),["Shot2Y"]=(32767,true,true),["Shot2Z"]=(32767,true,true),
          ["Shot3X"]=(32767,true,true),["Shot3Y"]=(32767,true,true),["Shot3Z"]=(32767,true,true) };
        Expect(CreatureAttackInputs.Fields.Order().SequenceEqual(source.Keys.Order()),"Missing source16 numeric fields.");
        foreach(var (field,rule) in source)
        {
            var suffix=rule.Fractional?".0":"";
            Expect(CreatureAttackInputs.Normalize(field,(rule.Maximum+1)+".75")==rule.Maximum+suffix,"Wrong source upper clamp: "+field);
            Expect(CreatureAttackInputs.Normalize(field,rule.Signed?"-32768.75":"4294967295")== (rule.Signed?"-32767":"0")+suffix,"Wrong source lower/unsigned LONG clamp: "+field);
            foreach(var text in new[]{"","invalid","-",".5",rule.Maximum+".75"})Expect(CreatureAttackInputs.Normalize(field,text)==text,"Partial/in-range text normalized: "+field);
            Expect(CreatureAttackInputs.Step(field,"",1)=="1","Empty spin failed.");
            Expect(CreatureAttackInputs.Step(field,"invalid",-1)== (rule.Signed?"-1":"0"+suffix),"Failed getter/unsigned underflow spin differs.");
            Expect(CreatureAttackInputs.Step(field,"2.75",1)=="3","Spin retained a source-discarded fraction.");
            Expect(CreatureAttackInputs.Step(field,rule.Maximum+".75",1)==rule.Maximum+suffix,"Upper spin clamp differs.");
            Expect(CreatureAttackInputs.Normalize(field,rule.Signed?"2147483648":"4294967296")== (rule.Signed?"2147483648":"4294967296"),"Failed getter repaired overflowing text.");
        }
        Expect(CreatureAttackInputs.Step("Shot1X","-1.75",1)=="0"&&CreatureAttackInputs.Step("Shot1X","-2147483648",-1)=="32767.0","Signed whole-part/wrap path differs.");
        Expect(CreatureAttackInputs.Step("Dammage","2147483647",1)=="0"&&CreatureAttackInputs.Step("RechargeTime","4294967295",1)=="0","Unsigned SetDlgItemInt roundtrip differs.");
        Reject(()=>CreatureAttackInputs.Normalize("Shot1Level","11"));Reject(()=>CreatureAttackInputs.Step("Shot1X","1",0));
        foreach(var native in new[]{false,true})
        {
            var directory=Path.Combine(Path.GetTempPath(),"AttackInput-"+Guid.NewGuid().ToString("N"));
            try
            {
                var doc=native?GlobalDefinitionNumericChecks.Fixture(directory):NativeMapDocument.CreateNewMap(new("AttackInput","AttackInput",20,20));
                var initial=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Private/canceled/Undo attack callback changed live state.");
                using(var parent=doc.CreateCreatureGlobalsSession())
                {
                    var working=parent.WorkingDocument;var row=working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var draft=working.CreateCreatureAttacksDraft(row);
                    foreach(var slot in CreatureAttacks.Slots)
                        draft=working.UpdateCreatureAttack(draft,slot,"1",CreatureAttacks.Parameters(slot).Select(field=>new CreatureAttackInput(field,
                            source.TryGetValue(field,out var rule)?CreatureAttackInputs.Normalize(field,rule.Signed?"-32768.5":(rule.Maximum+1)+".5"):draft.Display(field))));
                    // None changes enabled state but keeps the already entered
                    // parameters. Source Save still writes those fields.
                    draft=working.UpdateCreatureAttack(draft,"Shot1","0",CreatureAttacks.Parameters("Shot1").Select(field=>new CreatureAttackInput(field,draft.Display(field))));
                    working.ApplyCreatureAttacks(draft);Exact();
                    foreach(var (field,rule) in source)
                    {
                        var expected=rule.Signed?-134213632:field=="RechargeTime"?1044480:field=="Dammage"?32767:4096000;
                        Expect(draft.Raw(field)==expected.ToString(System.Globalization.CultureInfo.InvariantCulture),"Wrong literal encoded source clamp: "+field);
                    }
                    Expect(draft.Raw("Shot1ID")=="0"&&draft.Raw("Shot1Delay")=="4096000","None discarded pending parameters.");
                    doc.ApplyCreatureGlobals(doc.PrepareCreatureGlobals(parent));
                    Expect(doc.EditorChangeCount==count+1&&doc.GetHistory().Undo.Count==history+1,"Attacks common Apply split Undo/counter.");doc.Undo();Exact();doc.Redo();doc.Undo();Exact();
                }
                using(var canceled=doc.CreateCreatureGlobalsSession())
                {
                    var working=canceled.WorkingDocument;var draft=working.CreateCreatureAttacksDraft(working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));
                    working.ApplyCreatureAttacks(working.UpdateCreatureAttack(draft,"Melee","1",CreatureAttacks.Parameters("Melee").Select(field=>new CreatureAttackInput(field,field=="Dammage"?CreatureAttackInputs.Normalize(field,"40000"):draft.Display(field)))));
                }
                Exact();
                doc.SetGlobalDefinitionValues([new("Creatures",0,"Shot1ID",native?"255":"4294967295"),new("Creatures",0,"Shot1Delay","-1"),new("Creatures",0,"Shot1X","134217728")]);
                var imported=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var retained=doc.CreateCreatureAttacksDraft(doc.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));
                foreach(var slot in CreatureAttacks.Slots)retained=doc.UpdateCreatureAttack(retained,slot,retained.Raw(CreatureAttacks.TypeField(slot)),CreatureAttacks.Parameters(slot).Select(field=>new CreatureAttackInput(field,retained.Display(field))));
                doc.ApplyCreatureAttacks(retained);Expect(imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Read/no-op normalized unknown type or imported out-of-range parameters.");doc.Undo();Exact();
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        Console.WriteLine("ATTACK INPUT DETAIL16 literal fields:9 signed fixed,6 unsigned fixed,1 unsigned integer; EN_CHANGE whole-part bounds, UDN signed/unsigned wrap/partial input, cached None parameters, native/DKM private common oneUndo/Redo/Cancel, imported no-op; no GUI/game");
    }
}
