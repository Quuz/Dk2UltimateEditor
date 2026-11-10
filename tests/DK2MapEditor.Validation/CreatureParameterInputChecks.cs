using DK2MapEditor.Core.Editing;

internal static class CreatureParameterInputChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        foreach(var (input,expected) in new[]{("16001","16000"),("16001.75","16000"),("999999","16000"),("16000.75","16000.75"),
            ("0","0"),("",""),("invalid","invalid"),("-1","-1"),("4294967295","0"),("2147483648","0"),("4294967296","4294967296"),
            ("\t20000tail","16000"),("123tail","123tail")})
            Expect(CreatureParameterInputs.NormalizeAmount(input)==expected,"Source amount129 EN_CHANGE differs for "+input);
        foreach(var (input,delta,expected) in new[]{("16000",1,"16000"),("16000.75",-1,"15999"),("123.75",1,"124"),("0",-1,"0"),
            ("",1,"1"),("invalid",-1,"0"),("-1",1,"1"),("4294967295",1,"0"),("2147483647",1,"0")})
        {
            Expect(CreatureParameterInputs.StepAmount(input,delta)==expected,"Source amount129 unsigned spin differs.");
            foreach(var native in new[]{false,true})Expect(CreaturePageControls.Step("Parameters","Attract2Data",input,delta,native)==expected,"Parameters page bypasses source amount spin.");
        }
        try{CreatureParameterInputs.StepAmount("1",0);throw new Exception("Zero spin accepted.");}catch(ArgumentOutOfRangeException){}
        foreach(var native in new[]{false,true})
        {
            var directory=Path.Combine(Path.GetTempPath(),"ParameterInput-"+Guid.NewGuid().ToString("N"));
            try
            {
                var doc=native?GlobalDefinitionNumericChecks.Fixture(directory):NativeMapDocument.CreateNewMap(new("ParameterInput","ParameterInput",20,20));
                doc.SetGlobalDefinitionValues([new("Creatures",0,"Attract2Type","2"),new("Creatures",0,"Attract2ID","0"),new("Creatures",0,"Attract2Data","37")]);
                var initial=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Amount callback changed live/canceled/Undo state.");
                foreach(var type in new[]{"1","2","3","4"})
                {
                    using(var session=doc.CreateCreatureGlobalsSession())
                    {
                        var working=session.WorkingDocument;var row=working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);
                        var draft=working.CreateCreaturePageDraft(row,"Parameters");
                        var inputs=CreaturePageControls.Fields("Parameters").Select(field=>new CreaturePageInput(field,field switch
                        {"Attract2Type"=>type,"Attract2ID"=>"0","Attract2Data"=>CreatureParameterInputs.NormalizeAmount("16001"),_=>draft.Display(field)}));
                        working.ApplyCreaturePageDraft(working.UpdateCreaturePageDraft(draft,inputs));Exact();
                        Expect(working.CreateCreaturePageDraft(row,"Parameters").Raw("Attract2Data")=="16000","Clamped amount not stored as source integer.");
                        doc.ApplyCreatureGlobals(doc.PrepareCreatureGlobals(session));
                        Expect(doc.EditorChangeCount==count+1&&doc.GetHistory().Undo.Count==history+1,"Parameters common Apply split Undo/counter.");
                        doc.Undo();Exact();doc.Redo();doc.Undo();Exact();
                    }
                }
                // No-op loading must retain unsigned imported values beyond
                // the edit limit. Only explicit active input is clamped.
                doc.SetGlobalDefinitionValues([new("Creatures",0,"Attract2Data",native?"65535":"100000")]);
                var imported=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var importedRow=doc.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);
                var retained=doc.CreateCreaturePageDraft(importedRow,"Parameters");
                doc.ApplyCreaturePageDraft(doc.UpdateCreaturePageDraft(retained,CreaturePageControls.Fields("Parameters").Select(field=>new CreaturePageInput(field,retained.Display(field)))));
                Expect(imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"No-op repaired imported quantity.");doc.Undo();Exact();
                using(var canceled=doc.CreateCreatureGlobalsSession())
                {
                    var working=canceled.WorkingDocument;var draft=working.CreateCreaturePageDraft(working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0),"Parameters");
                    var inputs=CreaturePageControls.Fields("Parameters").Select(field=>new CreaturePageInput(field,field=="Attract2Data"?CreatureParameterInputs.NormalizeAmount("20000"):draft.Display(field)));
                    working.ApplyCreaturePageDraft(working.UpdateCreaturePageDraft(draft,inputs));
                }
                Exact();
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        Console.WriteLine("PARAMETERS INPUT DETAIL source control129 EN_CHANGE0..16000 and unsigned UDN; empty/invalid/overflow/whole-part spin, all4 active types native/DKM private page/common oneUndo/Redo/Cancel, imported no-op bytes; no GUI/game");
    }
}
