using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;
internal static partial class Program
{
    // Prepared callers only. No WPF execution is part of this stage.
    private static void CheckScenarioTreeKeyboard(bool native,string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("TreeKeys","Tree keys",18,16,NativeFormat:native));
        var root=doc.EnsureScriptScope(doc.ScriptOwners().Single(o=>o.Scope==32));
        var condition=ScriptGraph.ID(doc.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED")));
        var child=ScriptGraph.Build(doc).Events[condition].Edges.Single(e=>e.Field.Name is "and" or "ChildAnd");
        doc.InsertScriptEvent(child.Owner,child.Field,LegacyEventSchemas.All.Single(t=>t.Name=="ACTION_ALLIES"));
        var workspace=new EditorWorkspace{Document=doc};var host=new Window{Content=workspace};ShowManagerCallerWindow(host);
        var tabs=Control<TabControl>(workspace,"WorkspaceTabs");tabs.SelectedIndex=2;workspace.UpdateLayout();var tree=Control<TreeView>(workspace,"ScriptTree");
        void Pick(ushort id)=>Expect(Select(tree,item=>Get<ushort>(item,"EventID")==id&&(id!=0||Get<ScriptEdge?>(item,"Incoming")?.Scope==32)),"Could not select source keyboard fixture.");
        ScenarioCreationWindow? wizard=null;ScenarioPointWindow? point=null;var requests=0;
        workspace.ScenarioCreationPresenter=w=>wizard=w;workspace.ScenarioPointPresenter=w=>point=w;
        try
        {
            var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=doc.EditRevision;var history=doc.GetHistory();
            void Exact()=>Expect(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditRevision==revision&&doc.GetHistory().Undo.Count==history.Undo.Count,"Opening/cancelling a tree-key request edited the owner.");
            Pick(condition);Expect(workspace.RequestScriptTreeKey(Key.Insert,ModifierKeys.None)&&wizard is not null,"TVN Insert did not open condition creation at the selected incoming edge.");
            var types=(ComboBox)wizard!.FindName("ScenarioWizardTypes");Expect(Get<LegacyEventSchemas.EventType>(types.SelectedItem,"Type").RecordSignature==0xD5,"Tree Insert created an action instead of a condition.");wizard.Close();wizard=null;Exact();
            workspace.ScenarioDeletionPresenter=w=>{requests++;w.Close();};
            Expect(workspace.RequestScriptTreeKey(Key.Delete,ModifierKeys.Control|ModifierKeys.Shift)&&requests==1,"Tree Delete did not reach its existing confirmation policy.");Exact();
            Pick(0);Expect(workspace.RequestScriptTreeKey(Key.Insert,ModifierKeys.Shift)==!native,"Point-list Insert ignored project ordering scope.");if(!native){Expect(point is not null,"LVN Insert did not open point insertion.");point!.Close();point=null;}Exact();
            Pick(0);Expect(workspace.RequestScriptTreeKey(Key.Delete,ModifierKeys.None)&&requests==2,"Point Delete did not reach point removal confirmation.");Exact();
            Pick(condition);Expect(!workspace.RequestScriptTreeKey(Key.Insert,ModifierKeys.Alt),"Alt key unexpectedly captured.");
            using(var pen=doc.BeginLivePenStroke(new(4,2,0)))Expect(!workspace.RequestScriptTreeKey(Key.Delete,ModifierKeys.None),"Tree deletion interrupted active pen.");
            tabs.SelectedIndex=0;Expect(!workspace.RequestScriptTreeKey(Key.Delete,ModifierKeys.None),"Map-context key removed a hidden tree selection.");tabs.SelectedIndex=2;Pick(condition);
            workspace.ScenarioDeletionPresenter=w=>{requests++;Expect(w.Accept(),"Actual Delete confirmation refused a current tree branch.");};
            Expect(workspace.RequestScriptTreeKey(Key.Delete,ModifierKeys.None)&&ScriptGraph.Build(doc).Events.Count==0,"Tree Delete did not use complete branch removal.");
            doc.Undo();Expect(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Tree Delete Undo lost the original scenario bytes.");
            workspace.Document=null;Expect(!workspace.RequestScriptTreeKey(Key.Insert,ModifierKeys.None),"Closed document opened a stale creation request.");
        }
        finally{wizard?.Close();point?.Close();workspace.Document=null;host.Close();}
    }
}
