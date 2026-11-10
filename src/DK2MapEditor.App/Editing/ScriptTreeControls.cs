using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private ScriptGraph? scriptGraph;
    private ScenarioDescriptions? scenarioDescriptions;
    private bool updatingScriptTree;
    private bool restoringScriptSelection;
    private long scriptTreeGeneration;
    private sealed class ScriptNode(string label, ScriptEdge? incoming = null, ushort eventID = 0, ushort incomingEventID = 0, string? projectScopeIdentity = null,
        ScriptEdge? scopeRoot = null, string? scopeIdentity = null,ScriptTreeEntry? projectionEntry=null,ScriptTreeEntry? scopeEntry=null,
        bool expanded=false,Func<bool,bool>? storeExpansion=null,int? imageResource=null):INotifyPropertyChanged
    {
        public string Label { get; } = label;
        public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalEditorListImages.For(imageResource,16);
        public ScriptEdge? Incoming { get; } = incoming;
        public ushort EventID { get; } = eventID;
        public ushort IncomingEventID { get; } = incomingEventID;
        public string? ProjectScopeIdentity { get; } = projectScopeIdentity;
        public ScriptEdge? ScopeRoot { get; } = scopeRoot;
        public string? ScopeIdentity { get; } = scopeIdentity;
        public ScriptTreeEntry? ProjectionEntry {get;}=projectionEntry;
        public ScriptTreeEntry? ScopeEntry {get;}=scopeEntry;
        public List<ScriptNode> Children { get; } = [];
        private bool isExpanded=expanded;
        public bool IsExpanded
        {
            get=>isExpanded;
            set
            {
                if(isExpanded==value)return;
                if(storeExpansion?.Invoke(value)!=false)isExpanded=value;
                PropertyChanged?.Invoke(this,new(nameof(IsExpanded)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed record ScriptEdgeView(ScriptEdge Edge, string Label)
    { public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalEditorListImages.For(EditorListImageResources.ScenarioDestination(Edge),16); }
    private sealed record ScriptTargetView(ushort ID, string Label, int? ImageResource=null)
    { public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalEditorListImages.For(ImageResource,16); }
    private static string ScriptLinkLabel(ScriptEdge edge) => LocalizationService.Get(edge.Field.Name switch
    { "ChildAnd" or "and" => "Editor.ScriptAnd", "NextOr" when EntityReferences.EventType(edge.Owner)?.RecordSignature == 0xD6 => "Editor.ScriptNext",
        "NextOr" or "or" => "Editor.ScriptOr", "next" => "Editor.ScriptNext", _ => "Editor.ScriptRoot" });
    private string ScriptEventLabel(ScriptEvent item) => $"#{item.ID} · " + (scenarioDescriptions?.Describe(item.Record).Text
        ?? (item.Type is null ? LocalizationService.Get("Editor.ScriptUnknown") : LocalizationService.Get("Event." + item.Type.Name, item.Type.Name)));
    private void WorkspaceTab_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource == WorkspaceTabs && WorkspaceTabs.SelectedIndex == 2) RefreshScriptTree();
        if (e.OriginalSource == WorkspaceTabs && WorkspaceTabs.SelectedIndex == 7) RefreshPlayerResources();
        if (e.OriginalSource == WorkspaceTabs && WorkspaceTabs.SelectedIndex == 8) RefreshStatistics();
        if (e.OriginalSource == WorkspaceTabs && WorkspaceTabs.SelectedIndex == 9) RefreshDefinitions();
    }
    private void RefreshScriptTree()
    {
        if (ScriptTree is null) return;
        var generation=++scriptTreeGeneration;
        if (document is null)
        { scriptGraph = null; scenarioDescriptions = null; updatingScriptTree = true; ClearTreeSelection(); ScriptTree.ItemsSource = null; updatingScriptTree = false;
            ScriptStatus.Text = ""; ScriptPanel.IsEnabled = false;
            ScriptLinkPicker.ItemsSource = ScriptTypePicker.ItemsSource = ScriptTargetPicker.ItemsSource = null;
            ScriptMovePicker.ItemsSource = null; ScriptBranchPanel.IsEnabled = false;
            ScriptScopePicker.ItemsSource = ScriptOwnerPicker.ItemsSource = null; scriptOwners = []; return; }
        if (WorkspaceTabs.SelectedIndex != 2) return;
        var selectedID = (ScriptTree.SelectedItem as ScriptNode)?.EventID ?? 0;
        var selectedScope = (ScriptTree.SelectedItem as ScriptNode)?.Incoming;
        var selectedParent = (ScriptTree.SelectedItem as ScriptNode)?.IncomingEventID ?? 0;
        var selectedIdentity = (ScriptTree.SelectedItem as ScriptNode)?.ProjectScopeIdentity;
        var selectedRoot = (ScriptTree.SelectedItem as ScriptNode)?.ScopeRoot;
        var selectedRootIdentity = (ScriptTree.SelectedItem as ScriptNode)?.ScopeIdentity;
        scriptGraph = ScriptGraph.Build(document);
        var expansion=document.ReadScenarioExpansion();var source=document;var revision=source.EditRevision;
        scenarioDescriptions = ScenarioDescriptions.Build(document, LocalizationService.CurrentLanguage);
        var bindings = document.ScriptBindings().Where(binding => document.IsLegacyProject || binding.Root.Scope != 16 || binding.Owner is not null || binding.Root.Target != 0).ToArray();
        var owners = bindings.ToDictionary(binding => (binding.Root.Owner.File, binding.Root.Owner.Offset, binding.Root.Field.Name));
        var projection = ScriptTreeProjection.Build(scriptGraph, bindings.Select(binding => binding.Root));
        ScriptNode Display(ScriptTreeEntry entry, ScriptEdge? scopeRoot = null, string? scopeIdentity = null,ScriptTreeEntry? scopeEntry=null)
        {
            var incoming = entry.Incoming;
            if (entry.IsScope) { scopeRoot = incoming; scopeIdentity = entry.ProjectScopeIdentity;scopeEntry=entry; }
            var prefix = incoming is null ? "" : ScriptLinkLabel(incoming) + ": ";
            string label;
            if (entry.IsScope && incoming is not null)
            {
                var binding = owners[(incoming.Owner.File, incoming.Owner.Offset, incoming.Field.Name)];
                label = binding.Owner is null ? LocalizationService.Get("ScriptScope." + incoming.Scope, incoming.Owner.Category) + " · " + RecordLabel(incoming.Owner) : ScriptOwnerLabel(binding.Owner);
            }
            else label = entry.Marker switch
            {
                ScriptTreeMarker.Empty => prefix + LocalizationService.Get("Editor.ScriptEmpty"),
                ScriptTreeMarker.Missing => prefix + $"#{incoming?.Target} · " + LocalizationService.Get("Editor.ScriptMissing"),
                _ => prefix + ScriptEventLabel(scriptGraph.Events[entry.EventID]) + (entry.Marker switch
                { ScriptTreeMarker.Cycle => " · " + LocalizationService.Get("Editor.ScriptCycle"), ScriptTreeMarker.Shared => " · " + LocalizationService.Get("Editor.ScriptShared"), _ => "" })
            };
            bool StoreExpansion(bool value)
            {
                if(updatingScriptTree||restoringScriptSelection)return true;
                if(generation!=scriptTreeGeneration||!ReferenceEquals(document,source)||source.EditRevision!=revision||scenarioNamesOwnerClosed)return false;
                try{source.SetScenarioExpanded(entry.EventID,value);return true;}
                catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or FormatException or OverflowException)
                {ScriptStatus.SetResourceReference(TextBlock.TextProperty,"Editor.OperationFailed");ScriptStatus.ToolTip=error.Message;return false;}
            }
            var node = new ScriptNode(label,incoming,entry.EventID,entry.IncomingEventID,entry.ProjectScopeIdentity,scopeRoot,scopeIdentity,entry,scopeEntry,
                entry.IsScope||expansion.GetValueOrDefault(entry.EventID),expansion.ContainsKey(entry.EventID)?StoreExpansion:null,
                entry.IsScope ? EditorListImageResources.ScenarioScope(incoming?.Scope ?? 0)
                    : scriptGraph.Events.TryGetValue(entry.EventID,out var imageEvent) ? EditorListImageResources.ScenarioEvent(imageEvent.Record) : null);
            node.Children.AddRange(entry.Children.Select(child => Display(child, scopeRoot, scopeIdentity,scopeEntry)));
            return node;
        }
        var nodes = projection.Roots.Select(entry => Display(entry)).ToList();
        var orphan = new ScriptNode(LocalizationService.Get("Editor.ScriptUnlinked"),expanded:true);
        orphan.Children.AddRange(projection.Orphans.Select(entry => Display(entry)));
        if (orphan.Children.Count != 0) nodes.Add(orphan);
        updatingScriptTree = true; ClearTreeSelection(); ScriptTree.ItemsSource = nodes; updatingScriptTree = false;
        ScriptStatus.Text = $"{LocalizationService.Get("Editor.Scripts")}: {scriptGraph.Events.Count} · {LocalizationService.Get("Editor.ScriptRoots")}: {scriptGraph.Roots.Count}"
            + (scriptGraph.DuplicateIDs.Count == 0 ? "" : "\n" + LocalizationService.Get("Editor.ScriptDuplicates"))
            + (projection.Truncated ? "\n" + LocalizationService.Get("Editor.ScriptLimit") : "");
        ScriptPanel.IsEnabled = scriptGraph.DuplicateIDs.Count == 0;
        RefreshScriptOwners();
        var restoring=restoringScriptSelection;restoringScriptSelection=true;
        try
        {
            if (selectedID != 0) SelectTreeEvent(selectedID, selectedScope, selectedParent, selectedIdentity, selectedRoot, selectedRootIdentity,reveal:false);
            else if (selectedScope is { Scope: not 0 }) SelectTreeScope(selectedScope, selectedIdentity);
        }
        finally{restoringScriptSelection=restoring;}
        RefreshScriptControls();
    }
    private void ClearTreeSelection()
    {
        // WPF can retain SelectedItem from a removed hierarchical data source.
        // Deselect generated containers before replacing it, then explicitly
        // restore only a current, verified connection below.
        void Clear(ItemsControl parent)
        {
            foreach (var item in parent.Items)
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem container)
                { if (container.IsSelected) container.IsSelected = false; Clear(container); }
        }
        Clear(ScriptTree);
    }
    private void SelectTreeEvent(ushort id, ScriptEdge? preferredConnection = null, ushort preferredParent = 0, string? preferredIdentity = null,
        ScriptEdge? preferredRoot = null, string? preferredRootIdentity = null,bool reveal=true)
    {
        bool RootMatches(ScriptNode node) => preferredRoot is null || node.ScopeRoot is { } root
            && root.Owner.File == preferredRoot.Owner.File && root.Field.Name == preferredRoot.Field.Name && root.Scope == preferredRoot.Scope
            && (preferredRootIdentity is null ? root.Owner.Offset == preferredRoot.Owner.Offset : node.ScopeIdentity == preferredRootIdentity);
        bool Preferred(ScriptNode node) => node.EventID == id && RootMatches(node) && preferredConnection is not null && node.Incoming is { } edge
            && edge.Field.Name == preferredConnection.Field.Name && (preferredParent != 0
                ? node.IncomingEventID == preferredParent
                : edge.Owner.File == preferredConnection.Owner.File && edge.Scope == preferredConnection.Scope && (preferredIdentity is null
                    ? edge.Owner.Offset == preferredConnection.Owner.Offset : node.ProjectScopeIdentity == preferredIdentity));
        bool Contains(ScriptNode node, Func<ScriptNode, bool> predicate) => predicate(node) || node.Children.Any(child => Contains(child, predicate));
        bool Select(ItemsControl parent, Func<ScriptNode, bool> predicate)
        {
            parent.UpdateLayout();
            foreach (var item in parent.Items.Cast<ScriptNode>())
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem container) continue;
                if (predicate(item)) { container.IsSelected = true; container.BringIntoView(); return true; }
                if (Contains(item, predicate))
                {if(reveal)container.IsExpanded=true;if(container.IsExpanded&&Select(container,predicate))return true;}
            }
            return false;
        }
        if (preferredConnection is null) { Select(ScriptTree, node => node.EventID == id && RootMatches(node)); return; }
        if (Select(ScriptTree, Preferred)) return;
        int Matches(ScriptNode node) => (node.EventID == id ? 1 : 0) + node.Children.Sum(Matches);
        // If an identity was changed or a connection removed, do not silently
        // choose another displayed shared parent. An unambiguous event can still
        // be selected after a move; otherwise the user chooses a new connection.
        if (ScriptTree.Items.Cast<ScriptNode>().Sum(Matches) == 1) Select(ScriptTree, node => node.EventID == id);
    }
    private void ScriptTree_Changed(object sender, RoutedPropertyChangedEventArgs<object> e) { if (!updatingScriptTree) { if(!restoringScriptSelection)CloseScenarioDeletionWindow(); RefreshScriptControls(); } }
    private void RefreshScriptControls()
    {
        if (ScriptLinkPicker is null) return;
        var node = ScriptTree.SelectedItem as ScriptNode;
        RefreshScenarioInsertionButtons(node);
        RefreshScriptBranchControls(node);
        ScriptEditEventButton.IsEnabled = node is { EventID: not 0 }
            && scriptGraph?.Events.TryGetValue(node.EventID, out var selectedEvent) == true && CanEditScenarioEvent(selectedEvent.Record);
        ScriptReassignButton.IsEnabled = ScriptRemoveScopeButton.IsEnabled = node?.Incoming is { Scope: not 0 };
        var points=document?.IsLegacyProject==true?scriptGraph?.Roots.ToArray()??[]:[];
        var pointIndex=node?.Incoming is {Scope:not 0} point?Array.FindIndex(points,root=>root.Owner.File==point.Owner.File && root.Owner.Offset==point.Owner.Offset):-1;
        ScriptInsertPointButton.IsEnabled=pointIndex>=0;
        ScriptPointUpButton.IsEnabled=pointIndex>0;
        ScriptPointDownButton.IsEnabled=pointIndex>=0 && pointIndex<points.Length-1;
        var links = node is { EventID: not 0 } && scriptGraph?.Events.TryGetValue(node.EventID, out var item) == true ? item.Edges
            : node?.Incoming is { } incoming ? new[] { incoming } : [];
        var views = links.Select(edge => new ScriptEdgeView(edge, ScriptLinkLabel(edge))).ToArray();
        ScriptLinkPicker.ItemsSource = views;
        ScriptLinkPicker.SelectedItem = views.FirstOrDefault(view => view.Edge.Field.Name is "ChildAnd" or "and") ?? views.FirstOrDefault();
        RefreshScriptLinkChoices();
    }
    private void ScriptLink_Changed(object sender, SelectionChangedEventArgs e) => RefreshScriptLinkChoices();
    private void RefreshScriptLinkChoices()
    {
        if (ScriptTypePicker is null || ScriptTargetPicker is null || scriptGraph is null) return;
        var edge = (ScriptLinkPicker.SelectedItem as ScriptEdgeView)?.Edge;
        if (edge is null) { ScriptTypePicker.ItemsSource = null; ScriptTargetPicker.ItemsSource = null; return; }
        var scopes = ScriptGraph.IsEvent(edge.Owner) ? scriptGraph.Roots.Where(root => scriptGraph.Reachable(root.Target).Contains(ScriptGraph.ID(edge.Owner))).Select(root => root.Scope).Distinct().ToArray() : new[] { edge.Scope };
        var types = LegacyEventSchemas.All.Where(type => scopes.All(scope => ScriptGraph.IsKnownScope(scope) && (LegacyEventSchemas.AllowedScopes(type) & scope) != 0))
            .OrderBy(type => type.RecordSignature).ThenBy(type => type.Type).Select(type => new EventTypeView(type,
                LocalizationService.Get(type.RecordSignature == 0xD5 ? "ProjectKind.Trigger" : "ProjectKind.Action") + " · " + LocalizationService.Get("Event." + type.Name, type.Name))).ToArray();
        var old = (ScriptTypePicker.SelectedItem as EventTypeView)?.Type.Name;
        ScriptTypePicker.ItemsSource = types; ScriptTypePicker.SelectedItem = types.FirstOrDefault(view => view.Type.Name == old) ?? types.FirstOrDefault();
        ScriptTargetPicker.ItemsSource = new[] { new ScriptTargetView(0, LocalizationService.Get("Editor.ScriptEmpty")) }
            .Concat(scriptGraph.Events.Values.OrderBy(item => item.ID).Select(item => new ScriptTargetView(item.ID, ScriptEventLabel(item),EditorListImageResources.ScenarioEvent(item.Record)))).ToArray();
        ScriptTargetPicker.SelectedItem = ((ScriptTargetView[])ScriptTargetPicker.ItemsSource).FirstOrDefault(view => view.ID == edge.Target) ?? ((ScriptTargetView[])ScriptTargetPicker.ItemsSource)[0];
    }
    private void ScriptProperties_Click(object sender, RoutedEventArgs e)
    {
        var node = ScriptTree.SelectedItem as ScriptNode;
        var record = node is { EventID: not 0 } && scriptGraph?.Events.TryGetValue(node.EventID, out var item) == true ? item.Record : node?.Incoming?.Owner;
        if (record is null || !CommitFields()) return;
        RecordSearch.Text = "";
        CategoryPicker.SelectedItem = ((CategoryView[])CategoryPicker.ItemsSource).FirstOrDefault(view => view.ID == record.Category);
        RecordList.SelectedItem = ((RecordView[])RecordList.ItemsSource).FirstOrDefault(view => view.Record.File == record.File && view.Record.Offset == record.Offset);
        WorkspaceTabs.SelectedIndex = 1;
    }
    private void ScriptInsert_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || ScriptLinkPicker.SelectedItem is not ScriptEdgeView edge || ScriptTypePicker.SelectedItem is not EventTypeView type || !CommitFields()) return;
        OpenScenarioCreation(type.Type.RecordSignature, type.Type, edge.Edge);
    }
    private void ScriptAttach_Click(object sender, RoutedEventArgs e)
    {
        if (ScriptTargetPicker.SelectedItem is ScriptTargetView target) ChangeTreeLink(target.ID);
    }
    private void ScriptDetach_Click(object sender, RoutedEventArgs e) => ChangeTreeLink(0);
    private void ChangeTreeLink(ushort target)
    {
        if (document is null || ScriptLinkPicker.SelectedItem is not ScriptEdgeView edge || !CommitFields()) return;
        RecordOperation(() => { document.SetScriptLink(edge.Edge.Owner, edge.Edge.Field, target); return null; });
    }
}
