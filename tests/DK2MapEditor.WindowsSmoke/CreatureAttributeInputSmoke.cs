using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only. Actual execution opens WPF and is not part of CLI checks.
    private static void CheckCreatureAttributeInputs(NativeMapDocument document,string language)
    {
        var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        var row=document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var current=true;
        CreaturePageWindow Open()=>new(document,row,"Attributes",()=>current,()=>true);
        TextBox Input(CreaturePageWindow window,string field)=>(TextBox)window.FindName("CreatureProperty"+field);
        void Exact()=>Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"Private attribute input changed live state.");
        var window=Open();ShowManagerCallerWindow(window);
        try
        {
            foreach(var field in CreaturePageControls.Fields("Attributes"))
            {
                var format=GlobalDefinitionNumbers.Format("Creatures",field);
                Input(window,field).Text="40000";Expect(Input(window,field).Text==(format.Fractional?"32767.0":"32767"),"Attribute EN_CHANGE upper bound missing.");
                Input(window,field).Text="-40000";Expect(Input(window,field).Text==(format.SignedAttribute?"-40000":format.Fractional?"0.0":"0"),"Attribute EN_CHANGE lower rule differs.");
            }
            Input(window,"Speed").Text="";Expect(Input(window,"Speed").Text==""&&window.StepNumber("Speed",1)&&Input(window,"Speed").Text=="1","Empty attribute spin differs.");
            Input(window,"Health").Text="-1";Expect(Input(window,"Health").Text=="0","Unsigned attribute was not clamped immediately.");
            Input(window,"Speed").Text="1.75";Input(window,"AngerNoFood").Text="-9";
            LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(Input(window,"Speed").Text=="1.75"&&Input(window,"AngerNoFood").Text=="-9","Translation rewrote attribute strings.");LocalizationService.SetLanguage(language);
            Exact();window.Close();Exact();
        }
        finally{window.Close();}
        var parent=new CreatureGlobalsWindow(document,()=>current,()=>true);ShowManagerCallerWindow(parent);
        try
        {
            Expect(parent.Select("Attributes",0)&&parent.ActiveRowEditor is CreaturePageWindow,"Parent Attributes route missing.");
            var page=(CreaturePageWindow)parent.ActiveRowEditor!;Input(page,"Health").Text="40000";Input(page,"HungerRate").Text="40000.75";
            Expect(Input(page,"Health").Text=="32767"&&Input(page,"HungerRate").Text=="32767.0","Hosted page bypassed immediate normalization.");Exact();
            Expect(parent.ApplyCreatures(),"Clamped attributes rejected by common Apply.");
            var values=document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));
            Expect(values.Single(v=>v.Name=="Health").Value=="32767"&&values.Single(v=>v.Name=="HungerRate").Value=="134213632"
                &&document.EditorChangeCount==count+1&&document.GetHistory().Undo.Count==history+1,"Attribute common Apply lost values or split Undo.");document.Undo();Exact();
        }
        finally{parent.Close();}
        window=Open();ShowManagerCallerWindow(window);
        try{current=false;Input(window,"Health").Text="40000";Expect(Input(window,"Health").Text=="40000"&&!window.StepNumber("Health",1)&&!window.ApplyPage(),"Stale attribute callback retained authority.");Exact();}
        finally{window.Close();current=true;}
        var importedValue=document.IsLegacyProject?"100000":"65535";
        document.SetGlobalDefinitionValues([new("Creatures",0,"Health",importedValue),new("Creatures",0,"TortureHealthCng","65527")]);
        var imported=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());window=Open();ShowManagerCallerWindow(window);
        try
        {
            Expect(Input(window,"Health").Text==importedValue&&Input(window,"TortureHealthCng").Text=="-9","Opening repaired import or lost signed display.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Input(window,"Health").Text==importedValue,"Translation repaired imported health.");LocalizationService.SetLanguage(language);
            Expect(window.ApplyPage()&&imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"No-op Apply repaired imported attribute.");
        }
        finally{window.Close();document.Undo();}Exact();
    }
}
