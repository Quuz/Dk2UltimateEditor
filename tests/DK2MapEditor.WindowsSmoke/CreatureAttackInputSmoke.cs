using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only; no WPF execution is part of this stage's CLI checks.
    private static void CheckCreatureAttackInputs(NativeMapDocument document,string language)
    {
        var initial=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"Private attack UI changed live state.");
        void Choose(ComboBox combo,string id)=>combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==id);
        void Press(RepeatButton button)=>button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        var workspace=new EditorWorkspace{Document=document};
        try
        {
            Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=9;
            Choose(Control<ComboBox>(workspace,"DefinitionFamily"),"Creatures");Choose(Control<ComboBox>(workspace,"DefinitionSection"),"Attacks");
            var slot=Control<ComboBox>(workspace,"DefinitionAttackSlot");var type=Control<ComboBox>(workspace,"DefinitionAttackType");
            TextBox Box(string field)=>Control<TextBox>(workspace,"DefinitionAttack"+field);
            RepeatButton Spin(string field,bool up)=>Control<RepeatButton>(workspace,"DefinitionAttack"+field+(up?"Increase":"Decrease"));
            foreach(var id in CreatureAttacks.Slots)
            {
                Choose(slot,id);Choose(type,"1");
                foreach(var field in CreatureAttacks.Parameters(id).Where(CreatureAttackInputs.Handles))
                {
                    var maximum=field=="RechargeTime"?255:field=="Dammage"||field.EndsWith('X')||field.EndsWith('Y')||field.EndsWith('Z')?32767:1000;
                    Box(field).Text=(maximum+1)+".75";Expect(Box(field).Text==maximum+(field=="Dammage"?"":".0"),"Standalone field does not normalize immediately.");
                    Box(field).Text="";Press(Spin(field,true));Expect(Box(field).Text=="1","Actual RepeatButton spin from empty failed.");
                }
            }
            Choose(slot,"Shot1");Box("Shot1X").Text="-1.75";Expect(workspace.StepCreatureAttack("Shot1X",1)&&Box("Shot1X").Text=="0","Standalone signed whole-part step differs.");
            Box("Shot1Delay").Text="7.5";Choose(type,"0");Expect(!Box("Shot1Delay").IsEnabled&&!Spin("Shot1Delay",true).IsEnabled&&!workspace.StepCreatureAttack("Shot1Delay",1)&&Box("Shot1Delay").Text=="7.5","None lost pending input or kept step enabled.");
            Choose(type,"1");Box("Shot1X").Text="1.25";LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(Box("Shot1X").Text=="1.25"&&Box("Shot1Delay").Text=="7.5","Language change rewrote cached attack fields.");LocalizationService.SetLanguage(language);Exact();
        }
        finally{workspace.Document=null;}Exact();
        var current=true;var parent=new CreatureGlobalsWindow(document,()=>current,()=>true);ShowManagerCallerWindow(parent);
        try
        {
            Expect(parent.Select("Attacks",0)&&parent.ActiveRowEditor is CreatureWorkspacePageWindow,"Shared parent Attacks page missing.");
            var page=(CreatureWorkspacePageWindow)parent.ActiveRowEditor!;
            TextBox Box(string field)=>(TextBox)page.FindControl("DefinitionAttack"+field)!;
            var type=(ComboBox)page.FindControl("DefinitionAttackType")!;Choose(type,"1");
            Box("Shot1X").Text="40000.5";Box("Shot1Delay").Text="1001.5";
            Expect(Box("Shot1X").Text=="32767.0"&&Box("Shot1Delay").Text=="1000.0","Detached panel bypasses active text callbacks.");
            Press((RepeatButton)page.FindControl("DefinitionAttackShot1DelayIncrease")!);Expect(Box("Shot1Delay").Text=="1000.0","Hosted upper spin differs.");
            Choose(type,"0");Exact();Expect(parent.ApplyCreatures(),"Common Apply rejected clamped cached None parameters.");
            var state=document.CreateCreatureAttacksDraft(document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));
            Expect(state.Raw("Shot1ID")=="0"&&state.Raw("Shot1X")=="134213632"&&state.Raw("Shot1Delay")=="4096000"
                &&document.GetHistory().Undo.Count==history+1&&document.EditorChangeCount==count+1,"Common Apply lost None parameters or split Undo.");document.Undo();Exact();
        }
        finally{parent.Close();}
        var standalonePage=new CreatureWorkspacePageWindow(document,"Attacks",0,()=>current);ShowManagerCallerWindow(standalonePage);
        try
        {
            Choose((ComboBox)standalonePage.FindControl("DefinitionAttackType")!,"1");current=false;
            var box=(TextBox)standalonePage.FindControl("DefinitionAttackShot1X")!;box.Text="40000";
            Press((RepeatButton)standalonePage.FindControl("DefinitionAttackShot1XIncrease")!);
            Expect(box.Text=="40000"&&!standalonePage.ApplyPage(),"Stale hosted owner retained input/spin/Apply authority.");Exact();
        }
        finally{standalonePage.Close();current=true;}
        document.SetGlobalDefinitionValues([new("Creatures",0,"Shot1ID",document.IsLegacyProject?"4294967295":"255"),new("Creatures",0,"Shot1Delay","-1")]);
        var imported=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());standalonePage=new(document,"Attacks",0,()=>current);ShowManagerCallerWindow(standalonePage);
        try
        {
            var field=(TextBox)standalonePage.FindControl("DefinitionAttackShot1Delay")!;
            Expect(field.Text=="1048575.999755859375","Opening repaired imported unsigned attack value.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(((TextBox)standalonePage.FindControl("DefinitionAttackShot1Delay")!).Text=="1048575.999755859375","Translation repaired imported attack.");LocalizationService.SetLanguage(language);
            Expect(standalonePage.ApplyPage()&&imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"No-op normalized imported attack.");
        }
        finally{standalonePage.Close();document.Undo();}Exact();
    }
}
