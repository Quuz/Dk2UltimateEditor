using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only. Execution opens actual WPF controls and is left to the user.
    private static void CheckScenarioNewPoint(bool native,string language)
    {
        CheckScenarioPointBandDetails(native,language);
        var d=NativeMapDocument.CreateNewMap(new("NewPointUI","New Point UI",20,20,NativeFormat:native));
        if(native&&!d.ScriptOwners().Any(o=>o.Scope==1&&o.Record.Read(o.Record.Fields.Single(f=>f.Name=="bGlobalPlayer"))=="4"))
            d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEPLAYER"),new(0,0),4);
        var level=d.ScriptOwners().Single(o=>o.Scope==32);var root=d.EnsureScriptScope(level);
        var type=LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED");var id=ScriptGraph.ID(d.InsertScriptEvent(root.Owner,root.Field,type));
        var workspace=new EditorWorkspace{Document=d};var host=new Window{Content=workspace};ShowManagerCallerWindow(host);
        var tabs=Control<TabControl>(workspace,"WorkspaceTabs");tabs.SelectedIndex=2;workspace.UpdateLayout();
        var scopes=Control<ComboBox>(workspace,"ScriptScopePicker");scopes.SelectedValue=32;
        ScenarioPointWindow? window=null;var shown=0;workspace.ScenarioPointPresenter=w=>{window=w;shown++;};
        var initial=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=d.EditorChangeCount;var history=d.GetHistory().Undo.Count;
        void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&d.EditorChangeCount==count&&d.GetHistory().Undo.Count==history,"New Point caller/Cancel changed owner.");
        void Scope(uint value)=>((RadioButton)window!.FindName("ScenarioPointScope"+value)).IsChecked=true;
        void PickFirst()
        {
            var list=(ListBox)window!.FindName("ScenarioPointOwners");
            foreach(var row in list.Items.Cast<ListBoxItem>())
            {
                var choice=(ScenarioPointOwnerChoice)row.Tag;
                var icon=row.Content.GetType().GetProperty("Icon")!.GetValue(row.Content);
                Expect(row.ContentTemplate is not null && ReferenceEquals(icon,OriginalScenarioImages.For(ScenarioPointOwnerImageResources.For(choice.Owner))),"Point owner list lost source image or choice identity.");
                Expect(row.IsEnabled==choice.CanSelect,"Owner image presentation changed selection authority.");
            }
            list.SelectedItem=list.Items.Cast<ListBoxItem>().First(row=>row.IsEnabled);
        }
        try
        {
            Click(Control<StackPanel>(workspace,"ScriptScopePanel"),"Editor.ScriptOpenScope");
            Expect(shown==0,"Open existing unexpectedly opened New wizard.");Exact();
            Control<Button>(workspace,"ScriptNewPointButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(shown==1&&window is {IsModifying:false,SelectedScope:1,IsOwnerPage:false},"Explicit New reused selected level point instead of its private type page.");Exact();
            Expect(!workspace.RequestNewScenarioPoint()&&shown==1&&Control<TextBlock>(workspace,"ScriptStatus").Text==LocalizationService.Get("ScenarioPoint.FinishCurrent"),"Second New replaced an open point draft.");
            Scope(32);Expect(!((Button)window!.FindName("ScenarioPointFinish")).IsEnabled&&!window.Finish(),"New allowed a second level point.");
            Scope(4);window.Close();window=null;Exact();
            Expect(workspace.RequestNewScenarioPoint()&&window is {SelectedScope:4,IsModifying:false},"Cancel did not retain source point type for the next New.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();
            Expect(window!.SelectedScope==4&&Control<Button>(workspace,"ScriptNewPointButton").Content.ToString()==LocalizationService.Get("ScenarioPoint.New"),"Translation lost New label or private type.");
            LocalizationService.SetLanguage(language);workspace.RefreshLanguage();window.Close();window=null;Exact();
            foreach(var copy in new[]{0,1})
            {
                Expect(workspace.RequestNewScenarioPoint()&&window is {IsModifying:false},"New failed with existing global points.");
                Scope(1);Expect(window!.Next(),"Player type did not show owners.");PickFirst();
                var selected=window.SelectedOwner;Expect(selected is not null&&window.Finish()&&window.SavedPoint is {Scope:1,Target:0},"Finish did not append a separate empty player point.");
                var saved=window.SavedPoint!;window=null;
                Expect(d.FindScriptScope(level)!.Target==id,"New player point overwrote existing level script.");
                d.InsertScriptEvent(saved.Owner,saved.Field,type);
                // The next wizard must exclude this player while offering a
                // different owner. Existing level remains selectable in tree.
                count=d.EditorChangeCount;history=d.GetHistory().Undo.Count;initial=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
                Expect(workspace.RequestNewScenarioPoint()&&window!.Next(),"Repeated New failed.");
                var row=((ListBox)window!.FindName("ScenarioPointOwners")).Items.Cast<ListBoxItem>().Single(item=>item.Tag is ScenarioPointOwnerChoice c
                    &&c.Owner.PlayerID==selected!.PlayerID&&c.Owner.Record.File==selected.Record.File&&c.Owner.Record.Offset==selected.Record.Offset);
                Expect(!row.IsEnabled,"Repeated New offered an occupied player.");window.Close();window=null;Exact();
            }
            using(var pen=d.BeginLivePenStroke(new(4,1,1)))Expect(!workspace.RequestNewScenarioPoint(),"New interrupted a live pen.");Exact();
            tabs.SelectedIndex=0;Expect(!workspace.RequestNewScenarioPoint(),"Hidden scenario command opened New.");tabs.SelectedIndex=2;
            Expect(workspace.RequestNewScenarioPoint(),"Final New did not open.");workspace.EndScenarioNamesLifetime();
            Expect(!window!.Finish()&&!workspace.RequestNewScenarioPoint(),"Closed context retained New/Finish authority.");Exact();
        }
        finally{window?.Close();workspace.Document=null;host.Close();}
    }
}
