using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioExpansionWindow(bool native,string language)
    {
        var d=NativeMapDocument.CreateNewMap(new("ExpansionUI","Expansion UI",20,20,NativeFormat:native));
        var root=d.EnsureScriptScope(d.ScriptOwners().Single(o=>o.Scope==32));var type=LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED");
        var first=d.InsertScriptEvent(root.Owner,root.Field,type);var a=ScriptGraph.ID(first);
        var child=ScriptGraph.Edges(first).Single(e=>e.Field.Name is "ChildAnd" or "and");var b=ScriptGraph.ID(d.InsertScriptEvent(child.Owner,child.Field,type));
        var bRecord=ScriptGraph.Build(d).Events[b].Record;child=ScriptGraph.Edges(bRecord).Single(e=>e.Field.Name is "ChildAnd" or "and");
        d.InsertScriptEvent(child.Owner,child.Field,LegacyEventSchemas.All.Single(t=>t.Name=="ACTION_SETVARIABLE"));
        d.SetScenarioExpanded(a,true);var count=d.EditorChangeCount;var history=d.GetHistory();var revision=d.EditRevision;
        var workspace=new EditorWorkspace{Document=d};var host=new Window{Content=workspace};ShowManagerCallerWindow(host);
        var tabs=Control<TabControl>(workspace,"WorkspaceTabs");tabs.SelectedIndex=2;workspace.UpdateLayout();var tree=Control<TreeView>(workspace,"ScriptTree");
        TreeViewItem? Find(ItemsControl parent,ushort id)
        {
            parent.UpdateLayout();
            foreach(var item in parent.Items)
            {
                if(parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem row)continue;
                if(Get<ushort>(item,"EventID")==id)return row;
                if(row.IsExpanded&&Find(row,id) is {} found)return found;
            }
            return null;
        }
        void State()=>Expect(d.EditorChangeCount==count&&d.GetHistory().Undo.Count==history.Undo.Count&&d.EditRevision==revision,"Expansion created a command/history revision.");
        try
        {
            var row=Find(tree,a)??throw new Exception("Expanded condition was not generated.");
            Expect(row.IsExpanded&&Find(tree,b) is {IsExpanded:false},"Initial WPF expansion ignored stored condition flags.");State();
            row.IsSelected=true;row.IsExpanded=false;tree.UpdateLayout();
            Expect(!d.ReadScenarioExpansion()[a],"Actual collapsed TreeViewItem did not write the bound flag.");State();
            LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();
            row=Find(tree,a)??throw new Exception("Condition lost after translation.");Expect(!row.IsExpanded,"Refresh reopened a deliberately collapsed condition.");State();
            LocalizationService.SetLanguage(language);workspace.RefreshLanguage();row=Find(tree,a)!;row.IsExpanded=true;tree.UpdateLayout();
            var inner=Find(tree,b)??throw new Exception("Expanded parent did not generate the nested condition.");inner.IsExpanded=true;
            Expect(d.ReadScenarioExpansion()[a]&&d.ReadScenarioExpansion()[b],"Nested TreeViewItem binding did not store expansion.");State();
            // A held model from before refresh must not retain write authority.
            var oldModel=row.DataContext;workspace.RefreshLanguage();var property=oldModel.GetType().GetProperty("IsExpanded")!;property.SetValue(oldModel,false);
            Expect(d.ReadScenarioExpansion()[a],"Stale model changed the current document.");
            tabs.SelectedIndex=0;tabs.SelectedIndex=2;row=Find(tree,a)!;Expect(row.IsExpanded&&Find(tree,b) is {IsExpanded:true},"Tab refresh lost persisted nested expansion.");State();
            workspace.Document=null;property.SetValue(oldModel,false);Expect(d.ReadScenarioExpansion()[a],"Closed workspace retained flag-write authority.");
        }
        finally{workspace.Document=null;host.Close();}
    }
}
