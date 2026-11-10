using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private IReadOnlyList<ScriptOwner> scriptOwners = [];
    private bool updatingScriptOwners;
    private ScenarioPointWindow? scenarioPointWindow;
    private uint scenarioPointScope=1;
    public Action<ScenarioPointWindow>? ScenarioPointPresenter {get;set;}
    private void CloseScenarioPointWindow(){scenarioPointWindow?.Close();scenarioPointWindow=null;scenarioPointScope=1;}
    private sealed record ScriptOwnerView(ScriptOwner Owner, string Label)
    { public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalEditorListImages.For(EditorListImageResources.ScenarioScope(Owner.Scope),16); }
    private static bool SameScriptOwner(ScriptOwner first, ScriptOwner second) => first.Scope == second.Scope && first.MemberIndex == second.MemberIndex
        && first.PlayerID == second.PlayerID && first.Record.File == second.Record.File && first.Record.Offset == second.Record.Offset && first.Record.Length == second.Record.Length;
    private string ScriptOwnerLabel(ScriptOwner owner)
    {
        var label = LocalizationService.Get("ScriptScope." + owner.Scope) + " · ";
        if (owner.Scope == 1)
        {
            var id = owner.PlayerID != 0 ? owner.PlayerID.ToString() : owner.Record.Read(owner.Record.Fields.Single(field => field.Name == "bGlobalPlayer"));
            label += ValueCatalogues.Owners.FirstOrDefault(choice => choice.Value == id)?.Name(LocalizationService.CurrentLanguage) + $" (#{id}) · ";
        }
        if (owner.MemberIndex >= 0)
        {
            var band = owner.BandRecord ?? owner.Record;
            var member = document!.BandMembers(band)[owner.MemberIndex];
            var name = ValueCatalogues.ForTemplate("Creature").FirstOrDefault(choice => choice.Value == member.CreatureID.ToString())?.Name(LocalizationService.CurrentLanguage) ?? $"ID {member.CreatureID}";
            var bandID = band.Read(band.Fields.Single(field => field.Name == (document.IsLegacyProject ? "id" : "bBandID")));
            label += $"{LocalizationService.Get("ScriptScope.8")} #{bandID} · {owner.MemberIndex + 1} · {name} · {LocalizationService.Get("Field.Level")} {member.Level} · ";
        }
        if(owner.Scope is 1 or 16 or 32)return label.TrimEnd(' ','·');
        if(owner.Scope==8)
        {
            var id=owner.Record.Fields.Single(field=>field.Name==(document!.IsLegacyProject?"id":"bBandID"));
            return label+"#"+owner.Record.Read(id);
        }
        if(owner.Scope==4 && document!.GetMapRegion(owner.Record) is { } region)
        {
            var id=owner.Record.Fields.Single(field=>field.Name==(document.IsLegacyProject?"id":"bAreaID"));
            return label+$"#{owner.Record.Read(id)} · ({region.Bounds.X}, {region.Bounds.Y}) · {region.Bounds.Width} × {region.Bounds.Height}";
        }
        var primary=ValueCatalogues.PrimaryField(owner.Record);
        var entityName=primary is null?null:ValueCatalogues.ForField(owner.Record,primary,catalogueVersion).FirstOrDefault(choice=>choice.Value==owner.Record.Read(primary))?.Name(LocalizationService.CurrentLanguage);
        var x=owner.Record.Fields.FirstOrDefault(field=>field.Name=="x" || field.Name.EndsWith("LocationX",StringComparison.Ordinal));
        var y=owner.Record.Fields.FirstOrDefault(field=>field.Name=="y" || field.Name.EndsWith("LocationY",StringComparison.Ordinal));
        return label+(entityName??"#"+(primary is null?"?":owner.Record.Read(primary)))+(x is not null && y is not null?$" · ({owner.Record.Read(x)}, {owner.Record.Read(y)})":"");
    }
    private void RefreshScriptOwners()
    {
        if (document is null) return;
        scriptOwners = document.ScriptOwners();
        var scope = ScriptScopePicker.SelectedValue as int? ?? 32;
        updatingScriptOwners = true;
        ScriptScopePicker.ItemsSource = scriptOwners.Select(owner => owner.Scope).Distinct().OrderBy(value => value == 32 ? 0 : value)
            .Select(value => new Choice((int)value, LocalizationService.Get("ScriptScope." + value))).ToArray();
        ScriptScopePicker.SelectedValue = scope;
        if (ScriptScopePicker.SelectedIndex < 0) ScriptScopePicker.SelectedIndex = 0;
        updatingScriptOwners = false;
        FilterScriptOwners();
    }
    private void ScriptScopeFilter_Changed(object sender, SelectionChangedEventArgs e) { if (!updatingScriptOwners) FilterScriptOwners(); }
    private void ScriptOwnerSearch_Changed(object sender, TextChangedEventArgs e) { if (!updatingScriptOwners) FilterScriptOwners(); }
    private void FilterScriptOwners()
    {
        if (ScriptOwnerPicker is null || ScriptOwnerSearch is null) return;
        var old = (ScriptOwnerPicker.SelectedItem as ScriptOwnerView)?.Owner;
        var query = ScriptOwnerSearch.Text.Trim(); var scope = ScriptScopePicker.SelectedValue as int?;
        var choices = scriptOwners.Where(owner => owner.Scope == scope).Select(owner => new ScriptOwnerView(owner, ScriptOwnerLabel(owner)))
            .Where(choice => query.Length == 0 || choice.Label.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        ScriptOwnerPicker.ItemsSource = choices;
        ScriptOwnerPicker.SelectedItem = old is null ? null : choices.FirstOrDefault(choice => SameScriptOwner(choice.Owner, old));
        if (ScriptOwnerPicker.SelectedIndex < 0) ScriptOwnerPicker.SelectedIndex = 0;
    }
    private void SelectTreeScope(ScriptEdge root, string? preferredIdentity = null)
    {
        ScriptTree.UpdateLayout();
        var node = ScriptTree.Items.Cast<ScriptNode>().FirstOrDefault(node => node.Incoming is { } incoming
            && incoming.Scope == root.Scope && incoming.Owner.File == root.Owner.File && incoming.Field.Name == root.Field.Name
            && (preferredIdentity is null ? incoming.Owner.Offset == root.Owner.Offset : node.ProjectScopeIdentity == preferredIdentity));
        if (node is not null && ScriptTree.ItemContainerGenerator.ContainerFromItem(node) is TreeViewItem container)
        { container.IsExpanded = true; container.IsSelected = true; container.BringIntoView(); }
    }
    private void ScriptOpenScope_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields() || ScriptOwnerPicker.SelectedItem is not ScriptOwnerView choice) return;
        var root=document.FindScriptScope(choice.Owner);
        if(root is not null && document.HasStoredScriptScope(root)){SelectTreeScope(root);return;}
        OpenScenarioPoint(preferred:choice.Owner);
    }
    private void ScriptReassign_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields() || ScriptOwnerPicker.SelectedItem is not ScriptOwnerView choice
            || (ScriptTree.SelectedItem as ScriptNode)?.Incoming is not { Scope: not 0 } source) return;
        OpenScenarioPoint(source,preferred:choice.Owner);
    }
    private void ScriptRemoveScope_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields() || (ScriptTree.SelectedItem as ScriptNode)?.Incoming is not { Scope: not 0 } source) return;
        var node=(ScriptNode)ScriptTree.SelectedItem;
        RecordOperation(()=>{using var request=scenarioDeletionPolicy.BeginPoint(document,source);PresentScenarioDeletion(request,node);return null;});
    }

    private void ScriptNewPoint_Click(object sender,RoutedEventArgs args)=>RequestNewScenarioPoint();
    public bool RequestNewScenarioPoint()
    {
        var opened=false;
        RecordOperation(()=>
        {
            if(!IsLoaded||scenarioNamesOwnerClosed||WorkspaceTabs.SelectedIndex!=2||document is not {} source||source.HasActivePenStroke)return null;
            if(scenarioPointWindow is not null||scenarioCreationWindow is not null)
            {ScriptStatus.SetResourceReference(TextBlock.TextProperty,"ScenarioPoint.FinishCurrent");return null;}
            // Source103 sets Modify=FALSE, Insert=FALSE. It opens the type
            // page even when the selected owner already has a point.
            opened=OpenScenarioPoint(initialScope:scenarioPointScope);return null;
        });
        return opened;
    }
    private bool OpenScenarioPoint(ScriptEdge? existing=null,ScriptEdge? before=null,ScriptOwner? preferred=null,uint? initialScope=null)
    {
        if(scenarioPointWindow is not null){scenarioPointWindow.Activate();return false;}
        if(scenarioNamesOwnerClosed||document is not {} source||source.HasActivePenStroke)return false;
        var context=scenarioNamesContext;var revision=source.EditRevision;
        if(!CommitFields()||!ReferenceEquals(source,document)||scenarioNamesOwnerClosed||context!=scenarioNamesContext
            ||revision!=source.EditRevision||source.HasActivePenStroke)return false;
        var window=new ScenarioPointWindow(source,()=>!scenarioNamesOwnerClosed&&context==scenarioNamesContext&&ReferenceEquals(source,document)&&IsLoaded,CommitFields,ScriptOwnerLabel,
            root=>{if(!scenarioNamesOwnerClosed&&context==scenarioNamesContext&&ReferenceEquals(source,document)){RecordOperation(()=>null);SelectTreeScope(root);}},
            existing,before,preferred?.Scope??initialScope??1,preferred);
        scenarioPointWindow=window;
        window.Closed+=(_,_)=>
        {
            if(!ReferenceEquals(scenarioPointWindow,window))return;
            if(!scenarioNamesOwnerClosed&&context==scenarioNamesContext&&ReferenceEquals(source,document))scenarioPointScope=window.SelectedScope;
            scenarioPointWindow=null;
        };
        if(ScenarioPointPresenter is { } presenter)presenter(window);else{window.Owner=Window.GetWindow(this);window.Show();}
        return true;
    }
    private void ScriptInsertPoint_Click(object sender,RoutedEventArgs e)
    {
        if(document is null || !document.IsLegacyProject || !CommitFields() || (ScriptTree.SelectedItem as ScriptNode)?.Incoming is not {Scope:not 0} before)return;
        OpenScenarioPoint(before:before,preferred:(ScriptOwnerPicker.SelectedItem as ScriptOwnerView)?.Owner);
    }
    private void ScriptPointUp_Click(object sender,RoutedEventArgs e)=>MoveScenarioPoint(false);
    private void ScriptPointDown_Click(object sender,RoutedEventArgs e)=>MoveScenarioPoint(true);
    private void MoveScenarioPoint(bool down)
    {
        if(document is null || !document.IsLegacyProject || !CommitFields() || (ScriptTree.SelectedItem as ScriptNode)?.Incoming is not {Scope:not 0} source)return;
        ScriptEdge? moved=null;RecordOperation(()=>{moved=document.MoveEditorScenarioPoint(source,down);return null;});
        if(moved is not null)SelectTreeScope(moved);
    }
}
