using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only; execution opens WPF and is outside this CLI stage.
    private static void CheckCreatureJobInputs(NativeMapDocument document,string language)
    {
        var row=document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var clear=document.CreateCreatureJobsDraft(row);
        foreach(var group in CreatureJobs.Groups)clear=document.RemoveCreatureJobs(clear,group.ID,clear.Slots(group.ID).Select((s,i)=>(s,i)).Where(p=>p.s.Job!="0").Select(p=>p.i));
        document.ApplyCreatureJobs(clear,true);
        var initial=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"Private Jobs numeric edit changed live state.");
        void Choose(ComboBox combo,string id)=>combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==id);
        void Press(RepeatButton button)=>button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        var workspace=new EditorWorkspace{Document=document};
        try
        {
            Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=9;
            Choose(Control<ComboBox>(workspace,"DefinitionFamily"),"Creatures");Choose(Control<ComboBox>(workspace,"DefinitionSection"),"Jobs");
            TextBox Box(string field)=>Control<TextBox>(workspace,"DefinitionJobs"+field);
            var table=Control<DataGrid>(workspace,"DefinitionJobsTable");
            foreach(var group in new[]{"Happy","Unhappy","Angry"})
            {
                Choose(Control<ComboBox>(workspace,"DefinitionJobsGroup"),group);
                foreach(var field in new[]{"Chance","Mood","Mana"})
                {
                    Box(field).Text=field=="Chance"?"101":"-32768";Expect(Box(field).Text==(field=="Chance"?"100":"-32767"),"Jobs immediate clamp missing.");
                    Box(field).Text="";var button=Control<RepeatButton>(workspace,"DefinitionJobs"+field+"Increase");Press(button);
                    Expect(Box(field).Text=="1"&&ReferenceEquals(button,workspace.FindName(button.Name)),"Jobs empty step rebuilt control or failed.");
                }
                Expect(table.Items.Cast<object>().All(item=>Get<string>(item,"Chance")=="0"),"Typing/spinning submitted Add/Change implicitly.");
                Box("Chance").Text="100";Box("Mood").Text="-9";Box("Mana").Text="7";
                Control<Button>(workspace,"DefinitionJobsAdd").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact();
            }
            Box("Mood").Text="-17";LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Box("Mood").Text=="-17","Jobs language change rewrote draft string.");LocalizationService.SetLanguage(language);
            Choose(Control<ComboBox>(workspace,"DefinitionJobsGroup"),"Hate");
            Expect(!workspace.StepCreatureJob("Chance",1)&&!Box("Chance").IsEnabled,"Hate received numeric field authority.");Exact();
        }
        finally{workspace.Document=null;}Exact();
        var current=true;var parent=new CreatureGlobalsWindow(document,()=>current,()=>true);ShowManagerCallerWindow(parent);
        try
        {
            Expect(parent.Select("Jobs",0)&&parent.ActiveRowEditor is CreatureWorkspacePageWindow,"Parent Jobs route missing.");
            var page=(CreatureWorkspacePageWindow)parent.ActiveRowEditor!;
            foreach(var group in new[]{"Happy","Unhappy","Angry"})
            {
                Choose((ComboBox)page.FindControl("DefinitionJobsGroup")!,group);
                ((TextBox)page.FindControl("DefinitionJobsChance")!).Text="101";((TextBox)page.FindControl("DefinitionJobsMood")!).Text="-40000";((TextBox)page.FindControl("DefinitionJobsMana")!).Text="40000";
                Expect(((TextBox)page.FindControl("DefinitionJobsChance")!).Text=="100"&&((TextBox)page.FindControl("DefinitionJobsMood")!).Text=="-32767"&&((TextBox)page.FindControl("DefinitionJobsMana")!).Text=="32767","Hosted Jobs bypassed numeric callbacks.");
                ((Button)page.FindControl("DefinitionJobsAdd")!).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact();
            }
            Expect(parent.ApplyCreatures(),"Common Jobs Apply failed.");
            var applied=document.CreateCreatureJobsDraft(document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));
            Expect(applied.Totals.All(t=>t.Total==100)&&applied.Slots("Happy")[0].Mood==(document.IsLegacyProject?"-32767":"32769")
                &&document.EditorChangeCount==count+1&&document.GetHistory().Undo.Count==history+1,"Three groups split Undo or lost signed bits.");document.Undo();Exact();
        }
        finally{parent.Close();}
        var stale=new CreatureWorkspacePageWindow(document,"Jobs",0,()=>current);ShowManagerCallerWindow(stale);
        try
        {
            current=false;var input=(TextBox)stale.FindControl("DefinitionJobsChance")!;input.Text="101";Press((RepeatButton)stale.FindControl("DefinitionJobsChanceIncrease")!);
            Expect(input.Text=="101"&&!stale.ApplyPage(),"Stale Jobs page retained clamp/step/Apply authority.");Exact();
        }
        finally{stale.Close();current=true;}
        document.SetGlobalDefinitionValues([new("Creatures",0,"HJobs[0]",CreatureJobs.Allowed("Happy").First().ID.ToString()),new("Creatures",0,"HJobsChance[0]","255"),new("Creatures",0,"HJobsMood[0]","32768"),new("Creatures",0,"HJobsMana[0]","65527")]);
        var imported=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var retained=new CreatureWorkspacePageWindow(document,"Jobs",0,()=>current);ShowManagerCallerWindow(retained);
        try
        {
            Expect(((TextBox)retained.FindControl("DefinitionJobsChance")!).Text=="255"&&((TextBox)retained.FindControl("DefinitionJobsMood")!).Text=="-32768"&&((TextBox)retained.FindControl("DefinitionJobsMana")!).Text=="-9","Opening repaired Jobs import.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(((TextBox)retained.FindControl("DefinitionJobsMood")!).Text=="-32768","Translation repaired imported job field.");LocalizationService.SetLanguage(language);
            Expect(retained.ApplyPage()&&imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Jobs no-op Apply repaired imported tuple.");
        }
        finally{retained.Close();document.Undo();}Exact();
    }
}
