using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DK2MapEditor.Core.Editing;
namespace DK2MapEditor.App.Editing;
public partial class EditorWorkspace
{
    private void ScriptTree_KeyDown(object sender,KeyEventArgs args)
    {
        if(args.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase or PasswordBox)return;
        if(RequestScriptTreeKey(args.Key,Keyboard.Modifiers))args.Handled=true;
    }
    public bool RequestScriptTreeKey(Key key,ModifierKeys modifiers)
    {
        var handled=true;RecordOperation(()=>{handled=ExecuteScriptTreeKey(key,modifiers);return null;});return handled;
    }
    private bool ExecuteScriptTreeKey(Key key,ModifierKeys modifiers)
    {
        if(key is not (Key.Insert or Key.Delete)||(modifiers&~(ModifierKeys.Control|ModifierKeys.Shift))!=0
            ||scenarioNamesOwnerClosed||WorkspaceTabs.SelectedIndex!=2||document is not {} source||source.HasActivePenStroke
            ||ScriptTree.SelectedItem is not ScriptNode node||!CommitFields()||!ReferenceEquals(document,source)||!ReferenceEquals(ScriptTree.SelectedItem,node))return false;
        var args=new RoutedEventArgs();
        if(key==Key.Delete)
        {
            if(node.EventID!=0)ScriptDeleteBranch_Click(ScriptTree,args);
            else if(node.Incoming is {Scope:not 0})ScriptRemoveScope_Click(ScriptTree,args);
            else return false;
            return true;
        }
        if(node.EventID==0)
        {
            if(!source.IsLegacyProject||node.Incoming is not {Scope:not 0})return false;
            ScriptInsertPoint_Click(ScriptTree,args);return true;
        }
        if(node.ScopeEntry is not null)return OpenSourceScenarioInsertion(ScenarioInsertCommand.InsertCondition);
        // Keep the existing modern orphan-editor route. Source parent commands
        // require a point, while the generic editor can edit an unlinked chain.
        if(node.Incoming is not {} edge)return false;
        var graph=ScriptGraph.Build(source);
        var scopes=ScriptGraph.IsEvent(edge.Owner)?graph.Roots.Where(root=>graph.Reachable(root.Target).Contains(ScriptGraph.ID(edge.Owner))).Select(root=>root.Scope).Distinct().ToArray():new[]{edge.Scope};
        bool Allowed(LegacyEventSchemas.EventType candidate)=>candidate.RecordSignature==0xD5&&scopes.All(scope=>ScriptGraph.IsKnownScope(scope)&&(LegacyEventSchemas.AllowedScopes(candidate)&scope)!=0);
        scenarioCreationContext??=source.CreateScenarioCreationContext();var preferred=scenarioCreationContext.InitialType(0xD5);
        var type=Allowed(preferred)?preferred:LegacyEventSchemas.All.FirstOrDefault(Allowed);
        if(type is null)return false;OpenScenarioCreation(0xD5,type,edge);return true;
    }
}
