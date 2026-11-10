using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private void RefreshScriptBranchControls(ScriptNode? node)
    {
        ScriptBranchPanel.IsEnabled = node is { EventID: not 0 };
        var old = (ScriptMovePicker.SelectedItem as ScriptEdgeView)?.Edge;
        if (scriptGraph is null || document is null) { ScriptMovePicker.ItemsSource = null; return; }
        var destinations = document.ScriptBindings().Select(binding => new ScriptEdgeView(binding.Root,
            (binding.Owner is null ? RecordLabel(binding.Root.Owner) : ScriptOwnerLabel(binding.Owner)) + " · " + ScriptLinkLabel(binding.Root)))
            .Concat(scriptGraph.Events.Values.OrderBy(item => item.ID).SelectMany(item => item.Edges.Select(edge =>
                new ScriptEdgeView(edge, ScriptEventLabel(item) + " · " + ScriptLinkLabel(edge))))).ToArray();
        ScriptMovePicker.ItemsSource = destinations;
        ScriptMovePicker.SelectedItem = destinations.FirstOrDefault(view => old is not null && view.Edge.Owner.File == old.Owner.File
            && view.Edge.Owner.Offset == old.Owner.Offset && view.Edge.Field.Name == old.Field.Name) ?? destinations.FirstOrDefault();
    }
    private void ScriptMove_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields() || ScriptTree.SelectedItem is not ScriptNode { EventID: not 0 } node
            || ScriptMovePicker.SelectedItem is not ScriptEdgeView destination) return;
        RecordOperation(() => { document.MoveScriptBranch(node.EventID, destination.Edge.Owner, destination.Edge.Field); return null; });
        SelectTreeEvent(node.EventID);
    }
    private void ScriptMoveDirection_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields() || ScriptTree.SelectedItem is not ScriptNode { EventID: not 0 } node
            || sender is not Button { Tag: string direction }) return;
        RecordOperation(() => { document.MoveScriptBranch(node.EventID, Enum.Parse<ScriptBranchMove>(direction)); return null; });
        SelectTreeEvent(node.EventID);
    }
    private void ScriptDeleteBranch_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields() || ScriptTree.SelectedItem is not ScriptNode { EventID: not 0 } node) return;
        RecordOperation(() => { using var request=scenarioDeletionPolicy.BeginEvent(document,node.EventID);PresentScenarioDeletion(request,node);return null; });
    }
}
