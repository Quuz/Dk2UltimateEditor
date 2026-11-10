using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only. Do not invoke while actual GUI execution is disallowed.
    private static void CheckCreatureParameterInputs(NativeMapDocument document,string language)
    {
        document.SetGlobalDefinitionValues([new("Creatures",0,"Attract2Type","2"),new("Creatures",0,"Attract2ID","0"),new("Creatures",0,"Attract2Data","37")]);
        var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        var row=document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var current=true;
        CreaturePageWindow Open()=>new(document,row,"Parameters",()=>current,()=>true);
        TextBox Amount(CreaturePageWindow w)=>(TextBox)w.FindName("CreaturePropertyAttract2Data");
        void Type(CreaturePageWindow w,string type)
        {
            var combo=(ComboBox)w.FindName("CreaturePropertyAttract2Type");
            combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Equals(item.GetType().GetProperty("ID")!.GetValue(item),type));
        }
        void Exact()=>Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"A private Parameters input changed live state.");
        var window=Open();ShowManagerCallerWindow(window);
        try
        {
            foreach(var type in new[]{"1","2","3","4"})
            {
                Type(window,type);Amount(window).Text="16001";Expect(Amount(window).Text=="16000","Active amount did not clamp immediately.");
                Amount(window).Text="4294967295";Expect(Amount(window).Text=="0","Unsigned LONG boundary not clamped to zero.");
                Amount(window).Text="";Expect(Amount(window).Text==""&&window.StepNumber("Attract2Data",1)&&Amount(window).Text=="1","Empty amount was repaired early or could not spin from zero.");
                Amount(window).Text="0";Expect(window.StepNumber("Attract2Data",-1)&&Amount(window).Text=="0","Zero unsigned amount underflow differs.");
            }
            Amount(window).Text="123";LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(Amount(window).Text=="123","Language change rewrote pending amount.");LocalizationService.SetLanguage(language);
            Type(window,"2");Expect(Amount(window).Text=="37","Type callback no longer reloads captured private amount.");
            Type(window,"0");Expect(!Amount(window).IsEnabled&&Amount(window).Text=="","None amount is active or not blank.");
            Amount(window).Text="16001";Expect(Amount(window).Text=="16001"&&!window.StepNumber("Attract2Data",1),"Disabled amount retained clamp/spin authority.");
            Exact();window.Close();Exact();
        }
        finally{window.Close();}
        var parent=new CreatureGlobalsWindow(document,()=>current,()=>true);ShowManagerCallerWindow(parent);
        try
        {
            Expect(parent.Select("Parameters",0)&&parent.ActiveRowEditor is CreaturePageWindow,"Parameters parent route missing.");
            var page=(CreaturePageWindow)parent.ActiveRowEditor!;Amount(page).Text="20000";Expect(Amount(page).Text=="16000","Hosted page bypassed input callback.");Exact();
            Expect(parent.ApplyCreatures(),"Common Parameters Apply failed.");
            var state=document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));
            Expect(state.Single(v=>v.Name=="Attract2Data").Value=="16000"&&document.EditorChangeCount==count+1&&document.GetHistory().Undo.Count==history+1,"Common Apply lost amount or split Undo.");
            document.Undo();Exact();
        }
        finally{parent.Close();}
        window=Open();ShowManagerCallerWindow(window);
        try{current=false;Amount(window).Text="20000";Expect(Amount(window).Text=="20000"&&!window.StepNumber("Attract2Data",1)&&!window.ApplyPage(),"Stale owner changed amount input.");Exact();}
        finally{window.Close();current=true;}
        var importedValue=document.IsLegacyProject?"100000":"65535";document.SetGlobalDefinitionValues([new("Creatures",0,"Attract2Data",importedValue)]);
        var imported=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());window=Open();ShowManagerCallerWindow(window);
        try
        {
            Expect(Amount(window).Text==importedValue,"Opening repaired imported amount.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Amount(window).Text==importedValue,"Translation repaired imported amount.");LocalizationService.SetLanguage(language);
            Expect(window.ApplyPage()&&imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"No-op Apply repaired imported amount.");
        }
        finally{window.Close();document.Undo();}Exact();
    }
}
