using DK2MapEditor.Core.Editing;

internal static class CreatureResistanceSliderChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or FormatException or OverflowException){return;}throw new Exception("Invalid resistance input accepted.");}
    public static void Run()
    {
        Expect(CreatureResistanceInputs.Fields.SequenceEqual(new[]{"Res1Rate","Res2Rate","Res3Rate","Res4Rate"}),"Four source sliders incomplete.");
        foreach(var (raw,thumb) in new[]{("",0),("0",0),("1",1),("99",99),("100",100),("101",100),("255",100),("4294967295",100),("-1",0)})
            Expect(CreatureResistanceInputs.Position(raw)==thumb,"Source trackbar position differs: "+raw);
        foreach(var rate in Enumerable.Range(0,101))
        foreach(var delta in new[]{-20,-1,1,20})
            Expect(CreatureResistanceInputs.Scroll(rate.ToString(),delta)==Math.Clamp(rate+delta,0,100).ToString(),"Line/page boundary differs.");
        Expect(CreatureResistanceInputs.Scroll("255",-1)=="99"&&CreatureResistanceInputs.Scroll("-1",1)=="1","Imported thumb must scroll from clamped position.");
        Reject(()=>CreatureResistanceInputs.TypeField("Res5Rate"));Reject(()=>CreatureResistanceInputs.Position("1.5"));Reject(()=>CreatureResistanceInputs.Scroll("1",0));
        foreach(var native in new[]{false,true})
        {
            var directory=Path.Combine(Path.GetTempPath(),"ResistanceSlider-"+Guid.NewGuid().ToString("N"));
            try
            {
                var doc=native?GlobalDefinitionNumericChecks.Fixture(directory):NativeMapDocument.CreateNewMap(new("Resistance","Resistance",20,20));
                var initialValues=CreatureResistanceInputs.Fields.SelectMany((field,index)=>new[]{new GlobalDefinitionChange("Creatures",0,CreatureResistanceInputs.TypeField(field),index%2==0?"1":"0"),new GlobalDefinitionChange("Creatures",0,field,"255")});
                doc.SetGlobalDefinitionValues(initialValues);
                var original=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                void Exact()=>Expect(original.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Projection/private slider/Cancel/Undo changed live state.");
                GlobalDefinitionRow Row(NativeMapDocument d)=>d.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);
                foreach(var field in CreatureResistanceInputs.Fields)
                {
                    using(var parent=doc.CreateCreatureGlobalsSession())
                    {
                        var working=parent.WorkingDocument;var draft=working.CreateCreatureOptionsDraft(Row(working),"Resistances");
                        var retained=working.UpdateCreatureOptions(draft,CreatureOptions.Fields("Resistances").Select(f=>new CreatureOptionInput(f,draft.Display(f))));
                        working.ApplyCreatureOptions(retained);Expect(!retained.Changed,"Opening/no-op converted raw255 to thumb100.");Exact();
                        // A type change does not change any rate; only the explicit
                        // scroll of this slot replaces its raw255 with thumb99.
                        var targetType=CreatureResistanceInputs.TypeField(field);
                        var prepared=working.UpdateCreatureOptions(draft,CreatureOptions.Fields("Resistances").Select(f=>new CreatureOptionInput(f,
                            f==field?CreatureResistanceInputs.Scroll(draft.Display(f),-1):f==targetType?"2":draft.Display(f))));
                        working.ApplyCreatureOptions(prepared);Exact();
                        var actual=working.CreateCreatureOptionsDraft(Row(working),"Resistances");
                        foreach(var rate in CreatureResistanceInputs.Fields)Expect(actual.Raw(rate)==(rate==field?"99":"255"),"One slider changed another imported rate/None value.");
                        if(native)
                        {
                            var record=Row(working).Record!;
                            for(var index=0;index<4;index++)Expect(record.File.Data.Span[record.Offset+0xE0D+index*2]==(CreatureResistanceInputs.Fields[index]==field?99:255),"Independent packed resistance byte differs.");
                        }
                        doc.ApplyCreatureGlobals(doc.PrepareCreatureGlobals(parent));Expect(doc.EditorChangeCount==count+1&&doc.GetHistory().Undo.Count==history+1,"Slider private parent split Undo.");
                        doc.Undo();Exact();doc.Redo();doc.Undo();Exact();
                    }
                }
                using(var canceled=doc.CreateCreatureGlobalsSession())
                {
                    var working=canceled.WorkingDocument;var draft=working.CreateCreatureOptionsDraft(Row(working),"Resistances");
                    working.ApplyCreatureOptions(working.UpdateCreatureOptions(draft,CreatureOptions.Fields("Resistances").Select(f=>new CreatureOptionInput(f,f=="Res1Rate"?"42":draft.Display(f)))));
                }
                Exact();
                var typeOnly=doc.CreateCreatureOptionsDraft(Row(doc),"Resistances");
                doc.ApplyCreatureOptions(doc.UpdateCreatureOptions(typeOnly,CreatureOptions.Fields("Resistances").Select(f=>new CreatureOptionInput(f,f=="Res1"?"0":typeOnly.Display(f)))));
                Expect(doc.CreateCreatureOptionsDraft(Row(doc),"Resistances").Raw("Res1Rate")=="255","None type change erased imported rate.");doc.Undo();Exact();
            }
            finally
            {
                if(Directory.Exists(directory))
                {
                    var full=Path.GetFullPath(directory);Expect(full.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(full).StartsWith("ResistanceSlider-",StringComparison.Ordinal),"Unexpected cleanup target.");Directory.Delete(full,true);
                }
            }
        }
        Console.WriteLine("RESISTANCE SLIDER DETAIL four source rate/type pairs;0..100 thumb,1/20 line/page steps, imported raw255 versus thumb100 retained; native/DKM each slot exact private/common oneUndo/Redo/Cancel and independent packed bytes; None keeps rate; no GUI/game");
    }
}
