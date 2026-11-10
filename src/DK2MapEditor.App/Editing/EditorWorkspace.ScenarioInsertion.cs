using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private IEnumerable<(Button Button,ScenarioInsertCommand Command)> ScenarioInsertionButtons=>
        new[]{(ScriptNewRootButton,ScenarioInsertCommand.NewRoot),(ScriptAppendConditionButton,ScenarioInsertCommand.AppendCondition),
            (ScriptInsertConditionButton,ScenarioInsertCommand.InsertCondition),(ScriptAppendActionButton,ScenarioInsertCommand.AppendAction),
            (ScriptInsertActionButton,ScenarioInsertCommand.InsertAction)};
    private void RefreshScenarioInsertionButtons(ScriptNode? node)
    {
        if(ScriptNewRootButton is null)return;
        foreach(var (button,command) in ScenarioInsertionButtons)
        {
            button.IsEnabled=false;
            if(scenarioNamesOwnerClosed||document is null||document.HasActivePenStroke||scriptGraph is null||node?.ScopeEntry is not {} root)continue;
            try{_=ScenarioInsertion.Resolve(scriptGraph,root,node.ProjectionEntry,command);button.IsEnabled=true;}
            catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or FormatException or OverflowException){ }
        }
    }
    private void ScriptSourceInsert_Click(object sender,RoutedEventArgs args)
    {
        if(sender is Button{Tag:string name}&&Enum.TryParse<ScenarioInsertCommand>(name,out var command))RequestScenarioInsert(command);
    }
    public bool RequestScenarioInsert(ScenarioInsertCommand command)
    {
        var opened=false;RecordOperation(()=>{opened=OpenSourceScenarioInsertion(command);return null;});return opened;
    }
    private bool OpenSourceScenarioInsertion(ScenarioInsertCommand command)
    {
        if(!Enum.IsDefined(command)||scenarioNamesOwnerClosed||WorkspaceTabs.SelectedIndex!=2||document is not {} source||source.HasActivePenStroke
            ||ScriptTree.SelectedItem is not ScriptNode node||node.ScopeEntry is not {} root)return false;
        if(scenarioCreationWindow is not null)
        {ScriptStatus.SetResourceReference(TextBlock.TextProperty,"ScriptInsert.FinishCurrent");return false;}
        var revision=source.EditRevision;var context=scenarioNamesContext;var resources=GameResources;
        if(!CommitFields()||!ReferenceEquals(source,document)||scenarioNamesOwnerClosed||context!=scenarioNamesContext||revision!=source.EditRevision
            ||!ReferenceEquals(resources,GameResources)||!ReferenceEquals(node,ScriptTree.SelectedItem)||source.HasActivePenStroke)return false;
        var graph=ScriptGraph.Build(source);var edge=ScenarioInsertion.Resolve(graph,root,node.ProjectionEntry,command);
        var signature=command is ScenarioInsertCommand.AppendAction or ScenarioInsertCommand.InsertAction?0xD6u:0xD5u;
        var scopes=ScriptGraph.IsEvent(edge.Owner)?graph.Roots.Where(r=>graph.Reachable(r.Target).Contains(ScriptGraph.ID(edge.Owner))).Select(r=>r.Scope).Distinct().ToArray():new[]{edge.Scope};
        bool Allowed(LegacyEventSchemas.EventType type)=>type.RecordSignature==signature&&scopes.All(scope=>ScriptGraph.IsKnownScope(scope)&&(LegacyEventSchemas.AllowedScopes(type)&scope)!=0);
        scenarioCreationContext??=source.CreateScenarioCreationContext();var preferred=scenarioCreationContext.InitialType(signature);
        var type=Allowed(preferred)?preferred:LegacyEventSchemas.All.FirstOrDefault(Allowed);if(type is null)return false;
        var window=CreateScenarioWizard(signature,type,edge,null,fieldsCommitted:true);if(window is null)return false;
        PresentScenarioWizard(window);return true;
    }
}
