using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioInsertionCommands(bool native,string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("InsertUI","Insert UI",20,20,NativeFormat:native));
        var root=doc.EnsureScriptScope(doc.ScriptOwners().Single(o=>o.Scope==32));
        var a=ScriptGraph.ID(doc.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED")));
        ScriptGraph Graph()=>ScriptGraph.Build(doc);
        ScriptEdge Next(ushort id)=>Graph().Events[id].Edges.Single(e=>e.Field.Name is "NextOr" or "or" or "next");
        ScriptEdge Child(ushort id)=>Graph().Events[id].Edges.Single(e=>e.Field.Name is "ChildAnd" or "and");
        ushort Add(ScriptEdge e,string type)=>ScriptGraph.ID(doc.InsertScriptEvent(e.Owner,e.Field,LegacyEventSchemas.All.Single(t=>t.Name==type)));
        var b=Add(Next(a),"TRIGGER_TIMEELAPSED");var c=Add(Child(a),"TRIGGER_TIMEELAPSED");var d=Add(Next(c),"ACTION_SETVARIABLE");
        var workspace=new EditorWorkspace{Document=doc};var host=new Window{Content=workspace};ShowManagerCallerWindow(host);
        var tabs=Control<TabControl>(workspace,"WorkspaceTabs");tabs.SelectedIndex=2;workspace.UpdateLayout();var tree=Control<TreeView>(workspace,"ScriptTree");
        ScenarioCreationWindow? wizard=null;var shown=0;workspace.ScenarioCreationPresenter=w=>{wizard=w;shown++;};
        void Pick(ushort id)=>Expect(Select(tree,node=>Get<ushort>(node,"EventID")==id&&(id!=0||Get<ScriptEdge?>(node,"Incoming")?.Scope==32)),"Cannot select insertion fixture.");
        var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
        void Exact()=>Expect(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Pending/cancelled insertion changed live map.");
        var cases=new[]{(ScenarioInsertCommand.NewRoot,a,b,false),(ScenarioInsertCommand.AppendCondition,a,d,false),
            (ScenarioInsertCommand.AppendAction,a,d,false),(ScenarioInsertCommand.AppendCondition,d,d,false),
            (ScenarioInsertCommand.AppendAction,d,d,false),(ScenarioInsertCommand.InsertAction,c,a,true),
            (ScenarioInsertCommand.InsertCondition,d,c,false)};
        try
        {
            foreach(var (command,selected,owner,child) in cases)
            {
                Pick(selected);var button=Control<Button>(workspace,"Script"+command+"Button");Expect(button.IsEnabled,"Source insertion button is disabled in its proper context.");
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(wizard is not null,"Explicit source command did not open the shared wizard.");Exact();
                var signature=command is ScenarioInsertCommand.AppendAction or ScenarioInsertCommand.InsertAction?0xD6u:0xD5u;
                var types=(ComboBox)wizard!.FindName("ScenarioWizardTypes");Expect(Get<LegacyEventSchemas.EventType>(types.SelectedItem,"Type").RecordSignature==signature,"Source command opened the wrong category.");
                var beforeShown=shown;Expect(!workspace.RequestScenarioInsert(ScenarioInsertCommand.NewRoot)&&shown==beforeShown,"Another command replaced the active wizard.");
                LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();Exact();LocalizationService.SetLanguage(language);workspace.RefreshLanguage();
                wizard.Close();wizard=null;Exact();Pick(selected);Expect(workspace.RequestScenarioInsert(command)&&wizard is not null,"Cancelled caller did not reopen.");
                // Choose parameter-safe source defaults rather than relying on
                // choices retained by an unrelated previous harness case.
                types=(ComboBox)wizard!.FindName("ScenarioWizardTypes");types.SelectedItem=types.Items.Cast<object>().Single(v=>Get<LegacyEventSchemas.EventType>(v,"Type").Name==(signature==0xD5?"TRIGGER_TIMEELAPSED":"ACTION_SETVARIABLE"));
                while(wizard.Next()){}
                Expect(wizard.Finish()&&wizard.CreatedEventID!=0,"Explicit insertion wizard did not finish.");
                var added=wizard.CreatedEventID;wizard=null;var edge=child?Child(owner):Next(owner);
                Expect(edge.Target==added&&doc.GetHistory().Undo.Count==history+1&&doc.EditorChangeCount==unchecked(count+1),"Explicit command inserted at the wrong end/incoming edge or split history.");
                var after=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());doc.Undo();Exact();doc.Redo();Expect(after.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Insertion redo differs.");doc.Undo();Exact();
            }
            Pick(b);Expect(!Control<Button>(workspace,"ScriptInsertActionButton").IsEnabled,"Action insert enabled before a top-level condition.");
            Pick(0);Expect(Control<Button>(workspace,"ScriptNewRootButton").IsEnabled&&!Control<Button>(workspace,"ScriptAppendActionButton").IsEnabled,"Point-root command availability differs.");
            Pick(c);var presented=shown;using(var pen=doc.BeginLivePenStroke(new(4,1,1)))Expect(!workspace.RequestScenarioInsert(ScenarioInsertCommand.AppendAction),"Source command interrupted the pen.");Exact();
            tabs.SelectedIndex=0;Expect(!workspace.RequestScenarioInsert(ScenarioInsertCommand.NewRoot)&&shown==presented,"Hidden tree command opened a wizard.");tabs.SelectedIndex=2;Pick(c);
            Expect(workspace.RequestScriptTreeKey(Key.Insert,ModifierKeys.None)&&wizard is not null,"Tree Insert did not share InsertCondition routing.");wizard!.Close();wizard=null;Exact();
            workspace.Document=null;Expect(!workspace.RequestScenarioInsert(ScenarioInsertCommand.NewRoot),"Closed owner retained insertion authority.");
        }
        finally{wizard?.Close();workspace.Document=null;host.Close();}
    }
}
