using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only. Actual windows/input/DPI remain manual acceptance.
    private static void CheckCreatureAuxiliaryInputs(NativeMapDocument document,string language)
    {
        var initial=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"Auxiliary private input changed live state.");
        void Choose(ComboBox combo,string id)=>combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==id);
        void Press(RepeatButton button)=>button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        var workspace=new EditorWorkspace{Document=document};
        try
        {
            Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=9;
            Choose(Control<ComboBox>(workspace,"DefinitionFamily"),"Creatures");Choose(Control<ComboBox>(workspace,"DefinitionSection"),"Possession");
            TextBox Box(string field)=>Control<TextBox>(workspace,"DefinitionOption"+field);
            foreach(var field in new[]{"SpecialAb1Data","SpecialAb2Data","WaddleScale","OscillateScale","WalkScale"})
            {
                var max=field=="WalkScale"?12:field is "WaddleScale" or "OscillateScale"?1:32767;
                Box(field).Text=(max+1)+".75";Expect(Box(field).Text==max+(field is "WaddleScale" or "OscillateScale"?".0":""),"Possession immediate source clamp missing.");
                Box(field).Text="";Press(Control<RepeatButton>(workspace,"DefinitionOption"+field+"Increase"));Expect(Box(field).Text=="1","Possession repeat button failed from empty.");
            }
            Box("WaddleScale").Text="1.25";LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Box("WaddleScale").Text=="1.25","Possession language change rewrote draft.");LocalizationService.SetLanguage(language);Exact();
        }
        finally{workspace.Document=null;}Exact();
        var current=true;var parent=new CreatureGlobalsWindow(document,()=>current,()=>true);ShowManagerCallerWindow(parent);
        try
        {
            Expect(parent.Select("Possession",0)&&parent.ActiveRowEditor is CreatureWorkspacePageWindow,"Parent Possession route missing.");
            var possession=(CreatureWorkspacePageWindow)parent.ActiveRowEditor!;
            ((TextBox)possession.FindControl("DefinitionOptionWalkScale")!).Text="13";
            ((TextBox)possession.FindControl("DefinitionOptionWaddleScale")!).Text="2.5";
            Expect(((TextBox)possession.FindControl("DefinitionOptionWalkScale")!).Text=="12"&&((TextBox)possession.FindControl("DefinitionOptionWaddleScale")!).Text=="1.0","Hosted Possession bypasses numeric callbacks.");Exact();
            Expect(parent.Select("CrSpells",0)&&parent.ActiveRowEditor is CreatureSpellWindow,"Parent CrSpells route missing.");
            var spell=(CreatureSpellWindow)parent.ActiveRowEditor!;
            foreach(var field in new[]{"Range","RechargeTime"})
            {
                var input=(TextBox)spell.FindName("CreatureSpellProperty"+field);input.Text="1025.75";Expect(input.Text=="1024.0","CrSpells source clamp missing.");
                input.Text="";var spin=(RepeatButton)spell.FindName("CreatureSpellProperty"+field+"Increase");Press(spin);
                Expect(ReferenceEquals(spin,spell.FindName("CreatureSpellProperty"+field+"Increase"))&&ReferenceEquals(input,spell.FindName("CreatureSpellProperty"+field))&&input.Text=="1","CrSpells spin rebuilt controls or failed from empty.");
                input.Text="1.75";
            }
            Exact();Expect(parent.ApplyCreatures(),"Common auxiliary Apply failed.");
            var values=document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));
            Expect(values.Single(v=>v.Name=="WalkScale").Value=="12"&&values.Single(v=>v.Name=="WaddleScale").Value=="4096"&&document.GetHistory().Undo.Count==history+1&&document.EditorChangeCount==count+1,"Two auxiliary pages split common Undo or lost values.");document.Undo();Exact();
        }
        finally{parent.Close();}
        var spellRow=document.GlobalDefinitionRows().Single(r=>r.Family=="CrSpells"&&r.Index==0);
        var single=new CreatureSpellWindow(document,spellRow,()=>current,()=>true);ShowManagerCallerWindow(single);
        try
        {
            var input=(TextBox)single.FindName("CreatureSpellPropertyRange");input.Text="1.25";LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(((TextBox)single.FindName("CreatureSpellPropertyRange")).Text=="1.25","CrSpells language change rewrote draft.");LocalizationService.SetLanguage(language);
            current=false;input=(TextBox)single.FindName("CreatureSpellPropertyRange");input.Text="1025";Expect(input.Text=="1025"&&!single.StepNumber("Range",1)&&!single.ApplySpell(),"Stale CrSpells input retained authority.");Exact();
        }
        finally{single.Close();current=true;}
        var stale=new CreatureWorkspacePageWindow(document,"Possession",0,()=>current);ShowManagerCallerWindow(stale);
        try
        {
            current=false;var input=(TextBox)stale.FindControl("DefinitionOptionWalkScale")!;input.Text="13";Press((RepeatButton)stale.FindControl("DefinitionOptionWalkScaleIncrease")!);
            Expect(input.Text=="13"&&!stale.ApplyPage(),"Stale Possession retained numeric authority.");Exact();
        }
        finally{stale.Close();current=true;}
        document.SetGlobalDefinitionValues([new("Creatures",0,"WaddleScale","65535"),new("CrSpells",0,"Range",document.IsLegacyProject?"-1":"65535")]);
        var imported=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());single=new(document,spellRow,()=>current,()=>true);ShowManagerCallerWindow(single);
        try{Expect(single.ApplySpell()&&imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"CrSpells no-op repaired import.");}
        finally{single.Close();}
        var retained=new CreatureWorkspacePageWindow(document,"Possession",0,()=>current);ShowManagerCallerWindow(retained);
        try{Expect(((TextBox)retained.FindControl("DefinitionOptionWaddleScale")!).Text=="15.999755859375"&&retained.ApplyPage()&&imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Possession read/no-op repaired import.");}
        finally{retained.Close();document.Undo();}Exact();
    }
}
