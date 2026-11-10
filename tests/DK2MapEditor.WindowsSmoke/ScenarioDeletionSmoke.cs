using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Compiled for later manual acceptance; never execute WPF for a CLI check.
    private static void CheckScenarioDeletionRequests(bool native,string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("DeleteUI","DeleteUI",16,16,NativeFormat:native));
        var owner=doc.ScriptOwners().Single(item=>item.Scope==32);var root=doc.EnsureScriptScope(owner);
        var workspace=new EditorWorkspace{Document=doc};var host=new Window{Content=workspace};ShowManagerCallerWindow(host);
        Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=2;workspace.UpdateLayout();var tree=Control<TreeView>(workspace,"ScriptTree");
        void Pick(ushort id)=>Expect(Select(tree,item=>Get<ushort>(item,"EventID")==id && (id!=0 || Get<ScriptEdge?>(item,"Incoming")?.Scope==32)),"Scenario request fixture could not select target");
        void Command(string name)=>typeof(EditorWorkspace).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(workspace,[workspace,new RoutedEventArgs()]);
        var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        Pick(0);workspace.ScenarioDeletionPresenter=window=>
        {
            Expect(window.WindowStartupLocation==WindowStartupLocation.CenterOwner && window.Title==LocalizationService.Get("ScenarioDelete.Title"),"Scenario request is not centered/localized");
            Expect(((TextBlock)window.FindName("RegionRemovalMessage")).Text==LocalizationService.Get("ScenarioDelete.EmptyPoint"),"Empty point request text wrong");
            Expect(((TextBlock)window.FindName("RegionRemovalCount")).Visibility==Visibility.Collapsed,"Scenario request exposed an unrelated batch count");
            ((CheckBox)window.FindName("RegionRemovalRemember")).IsChecked=true;Expect(!((Button)window.FindName("RegionRemovalNo")).IsEnabled,"Checked scenario reply left No enabled");window.Close();
        };
        Command("ScriptRemoveScope_Click");Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Checked close deleted empty point");
        root=doc.FindScriptScope(owner)!;var condition=ScriptGraph.ID(doc.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED")));
        var child=ScriptGraph.Build(doc).Events[condition].Edges.Single(edge=>edge.Field.Name is "and" or "ChildAnd");
        var action=ScriptGraph.ID(doc.InsertScriptEvent(child.Owner,child.Field,LegacyEventSchemas.All.Single(type=>type.Name=="ACTION_ALLIES")));
        Pick(condition);var prompts=0;
        workspace.ScenarioDeletionPresenter=window=>
        {
            prompts++;Expect(window.Request.Prompt==EditorConfirmationPrompt.ScenarioConditionRemoval,"Condition used wrong request bit");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(((TextBlock)window.FindName("RegionRemovalMessage")).Text==LocalizationService.Get("ScenarioDelete.Condition"),"Open scenario request translation failed");
            ((CheckBox)window.FindName("RegionRemovalRemember")).IsChecked=true;Expect(window.Accept(),"Current translated condition request refused Yes");
        };
        Command("ScriptDeleteBranch_Click");Expect(ScriptGraph.Build(doc).Events.Count==0 && prompts==1,"Condition confirmation did not delete children");doc.Undo();Pick(condition);
        workspace.ScenarioDeletionPresenter=_=>throw new Exception("Remembered condition Yes asked again");Command("ScriptDeleteBranch_Click");doc.Undo();
        Pick(action);workspace.ScenarioDeletionPresenter=window=>{prompts++;Expect(window.Request.Prompt==EditorConfirmationPrompt.ScenarioActionRemoval && window.Accept(),"Action reused condition suppression or failed Yes");};
        Command("ScriptDeleteBranch_Click");Expect(ScriptGraph.Build(doc).Events.Count==1,"Action deletion removed condition");doc.Undo();
        Pick(0);workspace.ScenarioDeletionPresenter=window=>{prompts++;Expect(window.Request.Prompt==EditorConfirmationPrompt.ScenarioPointRemoval,"Point reused event suppression");window.Close();};
        Command("ScriptRemoveScope_Click");Expect(ScriptGraph.Build(doc).Events.Count==2 && prompts==3,"Point close deleted events or failed to ask");
        Pick(action);workspace.ScenarioDeletionPresenter=window=>{Pick(condition);Expect(!window.Accept(),"Changed tree selection authorized an old request");window.Close();};Command("ScriptDeleteBranch_Click");
        Expect(ScriptGraph.Build(doc).Events.Count==2,"Changed scenario selection deleted events");
        Command("ScenarioDeletionReset_Click");Pick(condition);
        workspace.ScenarioDeletionPresenter=window=>{prompts++;workspace.Document=null;Expect(!window.Accept(),"Closed document authorized an old scenario request");window.Close();};Command("ScriptDeleteBranch_Click");
        Expect(ScriptGraph.Build(doc).Events.Count==2 && prompts==4,"Reset/closed-document guard failed");
        LocalizationService.SetLanguage(language);host.Close();
        Console.WriteLine($"PASS WPF scenario deletion questions/centered lifetime/independent bits/reset {native} {language}; compiled harness requires physical acceptance");
    }
}
