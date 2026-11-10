using System.Globalization;
using DK2MapEditor.Core.Editing;

internal static class CreatureLightInputChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException){return;}throw new Exception("Invalid light callback accepted.");}
    public static void Run()
    {
        foreach(var field in new[]{"LightX","LightY","LightZ"})
        {
            foreach(var (input,expected) in new[]{("32768.75","32767.0"),("-32768.75","-32767.0"),("999999.25","32767.0"),("-999999.25","-32767.0"),
                ("32767.999755859375","32767.999755859375"),("-32767.999755859375","-32767.999755859375"),("",""),("-","-"),(".5",".5"),("1.25","1.25"),
                ("2147483648.5","2147483648.5"),("-2147483649.5","-2147483649.5"),("2147483647","32767.0"),("-2147483648","-32767.0"),("\t-40000tail","-32767.0")})
                Check(CreatureLightInputs.Normalize(field,input)==expected,$"Signed whole-part clamp differs for {field}: {input}.");
            foreach(var (input,delta,expected) in new[]{("-1.75",1,"0"),("-1.75",-1,"-2"),("32767.75",1,"32767.0"),("-32767.75",-1,"-32767.0"),("",1,"1"),("garbage",-1,"-1")})
            {
                Check(CreatureLightInputs.Step(field,input,delta)==expected,"Signed source spin callback differs.");
                Check(CreaturePageControls.Step("Light",field,input,delta,false)==expected&&CreaturePageControls.Step("Light",field,input,delta,true)==expected,"Page step bypasses the source light callback.");
            }
        }
        foreach(var (input,expected) in new[]{("4.125","3.0"),("3.999755859375","3.999755859375"),("0.25","0.25"),("4294967295","0.0"),("2147483648","0.0"),
            ("4294967296","4294967296"),("-1.25","-1.25"),("",""),("3,75","3,75"),("4,75","3.0")})
            Check(CreatureLightInputs.Normalize("LightRadius",input)==expected,"Unsigned radius whole-part clamp differs: "+input);
        foreach(var (input,delta,expected) in new[]{("2.75",1,"3"),("3.75",1,"3.0"),("0",-1,"0.0"),("",-1,"0.0"),("",1,"1"),("2147483647",1,"0.0"),("4294967295",1,"0")})
            Check(CreaturePageControls.Step("Light","LightRadius",input,delta,false)==expected,"Radius spin/unsigned roundtrip differs.");
        Reject(()=>CreatureLightInputs.Normalize("LightR","256"));Reject(()=>CreatureLightInputs.Step("LightX","1",0));
        foreach(var native in new[]{false,true})
        {
            var doc=NativeMapDocument.CreateNewMap(new("LightCallbacks","LightCallbacks",20,20));
            // Native storage exercised through the existing exact definition
            // fixture; no installed map is saved or repaired.
            var directory=Path.Combine(Path.GetTempPath(),"LightCallback-"+Guid.NewGuid().ToString("N"));
            try
            {
                if(native)doc=GlobalDefinitionNumericChecks.Fixture(directory);
                var original=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var revision=doc.EditRevision;
                using var parent=doc.CreateCreatureGlobalsSession();var working=parent.WorkingDocument;
                var row=working.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var page=working.CreateCreaturePageDraft(row,"Light");
                var updated=working.UpdateCreaturePageDraft(page,CreaturePageControls.Fields("Light").Select(field=>new CreaturePageInput(field,field switch
                {
                    "LightX"=>CreatureLightInputs.Normalize(field,"40000.5"),"LightY"=>CreatureLightInputs.Normalize(field,"-40000.5"),
                    "LightZ"=>CreatureLightInputs.Normalize(field,"32767.5"),"LightRadius"=>CreatureLightInputs.Normalize(field,"4.5"),_=>page.Display(field)
                })));
                Check(updated.Raw("LightX")=="134213632"&&updated.Raw("LightY")=="-134213632"&&updated.Raw("LightZ")=="134215680"&&updated.Raw("LightRadius")=="12288","Clamped fixed-point values were encoded incorrectly.");
                working.ApplyCreaturePageDraft(updated);
                Check(original.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.EditRevision==revision,"Private numeric callback/page Apply changed the live parent.");
                var plan=doc.PrepareCreatureGlobals(parent);var history=doc.GetHistory().Undo.Count;doc.ApplyCreatureGlobals(plan);
                Check(doc.EditorChangeCount==count+1&&doc.GetHistory().Undo.Count==history+1,"Light callbacks split common Apply.");
                doc.Undo();Check(original.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Full Undo lost original definition bytes.");doc.Redo();doc.Undo();
                // Unchanged imported coordinates are not normalized by loading
                // or semantic read/re-entry. Only an explicit callback clamps.
                var importedRow=doc.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);
                doc.SetGlobalDefinitionValues([new("Creatures",0,"LightX","134217728")]);
                var imported=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var draft=doc.CreateCreaturePageDraft(importedRow,"Light");
                Check(draft.Display("LightX")=="32768.0","Imported display repaired the coordinate.");
                doc.ApplyCreaturePageDraft(doc.UpdateCreaturePageDraft(draft,CreaturePageControls.Fields("Light").Select(field=>new CreaturePageInput(field,draft.Display(field)))));
                Check(imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"A read-only/no-op draft repaired imported out-of-range data.");
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        Console.WriteLine("CREATURE LIGHT CALLBACK DETAIL four source controls104/106/108/114, signed/unsigned integer-prefix clamps and fractional retention, UDN whole-part one-unit steps, unsigned radius underflow, overflow/partial text; private native/DKM draft, exact fixed-point bits, one common Apply/Undo, imported no-op retention; no UI/game");
    }
}
