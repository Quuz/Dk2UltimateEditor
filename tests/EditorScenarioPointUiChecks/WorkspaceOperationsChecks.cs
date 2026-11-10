using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static T Property<T>(object item,string name)=>(T)item.GetType().GetProperty(name)!.GetValue(item)!;
    private static void Root(EditorWorkspace workspace,ScriptEdge root)
    {
        var tree=(TreeView)workspace.FindName("ScriptTree");tree.UpdateLayout();Flush();
        var node=tree.Items.Cast<object>().Single(item=>Property<ScriptEdge?>(item,"Incoming") is { } incoming && incoming.Scope==root.Scope && incoming.Owner.File==root.Owner.File && incoming.Owner.Offset==root.Owner.Offset);
        var container=tree.ItemContainerGenerator.ContainerFromItem(node) as TreeViewItem??throw new InvalidOperationException("Actual root container not generated");container.IsSelected=true;Flush();
    }
    private static void Command(EditorWorkspace workspace,string name)
    {var button=(Button)workspace.FindName(name);Check(button.IsEnabled,"Workspace command disabled: "+name);button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));Flush();}
    private static void WorkspaceOperations(string language,bool native)
    {
        LocalizationService.SetLanguage(language);var name=Name(language,native,"workspace-operations",2);var d=Fixture(native);var first=Owner(d,2);var destination=Owner(d,2,true);var root=Branch(d,first);
        var shell=new MainWindow(new AppSettings(),persistSettings:false);var workspace=(EditorWorkspace)shell.FindName("Editor");workspace.Document=d;workspace.Visibility=Visibility.Visible;Show(shell);((TabControl)workspace.FindName("WorkspaceTabs")).SelectedIndex=2;Flush();ScenarioPointWindow? window=null;
        workspace.ScenarioPointPresenter=shown=>{window=shown;Show(shown);};
        try
        {
            Root(workspace,root);((ComboBox)workspace.FindName("ScriptScopePicker")).SelectedValue=2;var owners=(ComboBox)workspace.FindName("ScriptOwnerPicker");owners.SelectedIndex=owners.Items.Count-1;var before=Snapshot(d);
            Command(workspace,"ScriptReassignButton");Check(window is {IsLoaded:true,IsModifying:true},"Actual Reassign did not open Modify wizard");Click(window!,"ScenarioPointNext");Pick(window!,destination);Render(window!,name);Exact(before);Click(window!,"ScenarioPointFinish");
            var moved=d.FindScriptScope(destination)!;Check(moved.Target==root.Target,"Actual Reassign lost event chain");var retained=Snapshot(d);Root(workspace,moved);
            workspace.ScenarioDeletionPresenter=dialog=>dialog.Close();Command(workspace,"ScriptRemoveScopeButton");Exact(retained);
            var events=ScriptGraph.Build(d).Events.Count;workspace.ScenarioDeletionPresenter=dialog=>Check(dialog.Accept(),"Scenario deletion Yes refused");Command(workspace,"ScriptRemoveScopeButton");Check(ScriptGraph.Build(d).Events.Count==events-1,"Actual Delete left point events");d.Undo();Exact(retained);
            Root(workspace,d.FindScriptScope(destination)!);
            if(native)
            {Check(!((Button)workspace.FindName("ScriptInsertPointButton")).IsEnabled && !((Button)workspace.FindName("ScriptPointUpButton")).IsEnabled && !((Button)workspace.FindName("ScriptPointDownButton")).IsEnabled,"Native inherent order enabled stored-list commands");}
            else
            {
                ((ComboBox)workspace.FindName("ScriptScopePicker")).SelectedValue=1;((ComboBox)workspace.FindName("ScriptOwnerPicker")).SelectedIndex=0;Command(workspace,"ScriptInsertPointButton");Check(window is {IsLoaded:true,IsModifying:false},"Actual Insert did not open private wizard");Click(window!,"ScenarioPointNext");Pick(window!,Owner(d,1));Click(window!,"ScenarioPointFinish");
                Check(ScriptGraph.Build(d).Roots.Select(edge=>edge.Scope).SequenceEqual(new uint[]{1,2}),"Actual InsertBefore stored order");Command(workspace,"ScriptPointDownButton");Check(ScriptGraph.Build(d).Roots.Select(edge=>edge.Scope).SequenceEqual(new uint[]{2,1}),"Actual point Down order");Command(workspace,"ScriptPointUpButton");Check(ScriptGraph.Build(d).Roots.Select(edge=>edge.Scope).SequenceEqual(new uint[]{1,2}),"Actual point Up order");
            }
            window=null;Command(workspace,"ScriptReassignButton");Check(window is {IsLoaded:true},"Context close fixture missing wizard");workspace.Document=null;Check(!window!.IsVisible,"Closed map left point wizard open");Pass(name);
        }
        finally{window?.Close();shell.Close();Flush();}
    }
}
