using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>The original three list families, five display sorts and confirmed grouped removal.
/// Sorting/filtering never reorders serialized records or changes the map selection.</summary>
public sealed partial class ObjectManagerWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly Func<bool> canApply;
    private readonly Func<TileRect?> selection;
    private readonly ObjectManagerSession session;
    private readonly Func<bool> ownerCurrent;
    private readonly ComboBox family = new(), sort = new();
    private readonly TextBox search = new();
    private readonly CheckBox selectionOnly = new();
    private readonly DataGrid table = new() { AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false,
        IsReadOnly = true, SelectionMode = DataGridSelectionMode.Extended, SelectionUnit = DataGridSelectionUnit.FullRow,
        EnableRowVirtualization = true, EnableColumnVirtualization = true, CanUserSortColumns = false, CanUserReorderColumns = false };
    private readonly TextBlock summary = new() { TextWrapping = TextWrapping.Wrap }, status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button apply = new();
    private IReadOnlyList<ObjectManagerEntry> entries = [];
    private List<ManagerRow> rows = [];
    private bool updating = true, closed;
    private ObjectRemovalPlan? plan;
    private string? statusKey;
    private int appliedCount;
    private ObjectBatchWindow? modificationWindow;
    private ObjectBatchWindow? additionWindow;
    private ObjectManagerOptionsWindow? optionsWindow;
    private Button? addOptions,modifyOptions;
    private readonly EditorObjectManagerHistory managerHistory;
    private readonly Button managerUndo=new();
    private readonly Button managerModify;
    private readonly Button managerAdd;
    private long selectionRevision;
    public ObjectRemovalPlan? RemovalPreview => plan;

    public ObjectManagerWindow(NativeMapDocument document, Func<TileRect?> selection, Func<bool> canApply, ObjectManagerSession? session = null,
        Func<bool>? ownerCurrent=null,Func<ObjectManagerWindow,TilePoint,Action?>? viewRequester=null,Action<ObjectManagerWindow>? pointRequester=null)
    {
        this.document = document; this.selection = selection; this.canApply = canApply; this.session = session ?? new();
        this.ownerCurrent=ownerCurrent??(()=>true);this.viewRequester=viewRequester;this.pointRequester=pointRequester;
        managerHistory=new(document);managerHistory.Changed+=ManagerHistoryChanged;
        requestPolicy = new(this.session.RequestPreferences);
        NameScope.SetNameScope(this, new NameScope());
        Width = 1050; Height = 680; MinWidth = 740; MinHeight = 450; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(TitleProperty, "Manager.Title"); SetResourceReference(ForegroundProperty, "Brush.Text"); SetResourceReference(BackgroundProperty, "Brush.Background");
        var outer = new DockPanel { Margin = new(12) };
        outer.SetResourceReference(Panel.BackgroundProperty, "Brush.Background");
        outer.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Brush.Text");
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top); outer.Children.Add(top);
        var filters = new WrapPanel(); top.Children.Add(filters);
        Label(filters, "Batch.Family"); Named(family, "ManagerFamily"); family.Width = 200; family.DisplayMemberPath = "Label"; family.SelectedValuePath = "ID"; filters.Children.Add(family);
        Label(filters, "Manager.Sort"); Named(sort, "ManagerSort"); sort.Width = 180; sort.DisplayMemberPath = "Label"; sort.SelectedValuePath = "ID"; filters.Children.Add(sort);
        Named(search, "ManagerSearch"); search.Width = 190; search.SetResourceReference(ToolTipProperty, "Editor.Search"); filters.Children.Add(search);
        Named(selectionOnly, "ManagerSelectionOnly"); selectionOnly.SetResourceReference(ContentControl.ContentProperty, "Manager.SelectionOnly"); filters.Children.Add(selectionOnly);
        var commands = new WrapPanel(); top.Children.Add(commands);
        InitializeNavigation(top,commands);
        InitializeFlowPreferences(top,commands);
        addOptions=Button("ManagerAddOptions","ManagerOptions.Add",()=>ShowOptions(false));commands.Children.Add(addOptions);
        modifyOptions=Button("ManagerModifyOptions","ManagerOptions.Modify",()=>ShowOptions(true));commands.Children.Add(modifyOptions);
        commands.Children.Add(Button("ManagerSelectAll", "Manager.SelectAll", () => table.SelectAll()));
        commands.Children.Add(Button("ManagerSelectNone", "Manager.SelectNone", () => table.UnselectAll()));
        commands.Children.Add(Button("ManagerRefresh", "Manager.Refresh", () => { Invalidate(); RefreshEntries(false); }));
        commands.Children.Add(Button("ManagerResetRequests", "ThingRequest.Reset", () => requestPolicy.ResetSuppression()));
        managerAdd=Button("ManagerAdd", "Manager.Add",()=>RequestAddition());commands.Children.Add(managerAdd);
        Named(managerUndo,"ManagerUndo");managerUndo.SetResourceReference(ContentControl.ContentProperty,"Menu.Undo");
        managerUndo.Click+=(_,_)=>UndoManagerCommand();commands.Children.Add(managerUndo);
        managerModify=Button("ManagerModify", "Modify.Title",()=>RequestModification());commands.Children.Add(managerModify);
        Named(summary, "ManagerSummary"); top.Children.Add(summary);
        var sessionHint = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(0, 3, 0, 5) };
        sessionHint.SetResourceReference(TextBlock.TextProperty, "Manager.SessionHint"); top.Children.Add(sessionHint);
        var bottom = new StackPanel(); DockPanel.SetDock(bottom, Dock.Bottom); outer.Children.Add(bottom);
        var hint = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(0, 5, 0, 5) }; hint.SetResourceReference(TextBlock.TextProperty, "Manager.ScopeHint"); bottom.Children.Add(hint);
        bottom.Children.Add(Button("ManagerPreview", "Manager.Preview", () => BuildRemovalPreview()));
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right }; bottom.Children.Add(actions);
        Named(apply, "ManagerApply"); apply.SetResourceReference(ContentControl.ContentProperty, "Manager.Remove"); apply.Click += (_, _) => ApplyRemoval(); actions.Children.Add(apply);
        actions.Children.Add(Button("ManagerCancel", "Editor.Cancel", Cancel));
        Named(status, "ManagerStatus"); bottom.Children.Add(status);
        Named(table, "ManagerObjects"); outer.Children.Add(table);
        var picture = new FrameworkElementFactory(typeof(Image)); picture.SetBinding(Image.SourceProperty, new Binding(nameof(ManagerRow.Icon)));
        picture.SetValue(FrameworkElement.WidthProperty, 32d); picture.SetValue(FrameworkElement.HeightProperty, 32d);
        table.Columns.Add(new DataGridTemplateColumn { CellTemplate = new DataTemplate { VisualTree = picture }, Width = 40, MinWidth = 40, IsReadOnly = true });
        foreach (var (key, property, width) in new[] { ("Batch.Family", nameof(ManagerRow.Kind), 105d), ("Manager.Name", nameof(ManagerRow.Name), 235d),
            ("Manager.Position", nameof(ManagerRow.Position), 90d), ("Editor.Owner", nameof(ManagerRow.Owner), 110d), ("Manager.TypeID", nameof(ManagerRow.ID), 75d),
            ("Manager.Data", nameof(ManagerRow.Data), 130d), ("Manager.Scenario", nameof(ManagerRow.Scripted), 125d) })
        {
            var style = new Style(typeof(TextBlock)); style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap)); style.Setters.Add(new Setter(ToolTipProperty, new Binding(property)));
            table.Columns.Add(new DataGridTextColumn { Header = new TextBlock { Text = LocalizationService.Get(key), TextWrapping = TextWrapping.Wrap, MaxWidth = width - 12 },
                Binding = new Binding(property), ElementStyle = style, Width = new(width), MinWidth = width });
        }
        Content = outer;
        family.SelectionChanged += FilterChanged; sort.SelectionChanged += FilterChanged; search.TextChanged += FilterChanged;
        selectionOnly.Checked += FilterChanged; selectionOnly.Unchecked += FilterChanged;
        table.SelectionChanged += (_, _) => { if (!updating) { Invalidate(); RefreshSummary(); } };
        table.MouseDoubleClick+=(_,args)=>
        {
            if(args.OriginalSource is not DependencyObject origin || ItemsControl.ContainerFromElement(table,origin) is not DataGridRow)return;
            args.Handled=true;
            RequestModification();
        };
        this.session.SelectionOnly=ObjectManagerListRules.SelectionAvailable(selection(),document.Width,document.Height);
        selectionOnly.IsEnabled=this.session.SelectionOnly;
        RefreshLanguage(); search.Text = this.session.Search; selectionOnly.IsChecked = this.session.SelectionOnly;
        updating = false; RefreshEntries(false); UpdateApply();
        document.Changed += DocumentChanged;document.HistoryChanged+=ManagerHistoryChanged; LocalizationService.LanguageChanged += LanguageChanged; Closed += (_, _) => Release();
    }
    private T Named<T>(T control, string name) where T : FrameworkElement { control.Name = name; RegisterName(name, control); return control; }
    private static void Label(Panel panel, string key) { var label = new TextBlock { Margin = new(4), VerticalAlignment = VerticalAlignment.Center }; label.SetResourceReference(TextBlock.TextProperty, key); panel.Children.Add(label); }
    private Button Button(string name, string key, Action action)
    { var button = Named(new Button(), name); button.SetResourceReference(ContentControl.ContentProperty, key); button.Click += (_, _) => action(); return button; }
    private sealed record Choice(int ID, string Label);
    private void FilterChanged(object sender, RoutedEventArgs e)
    {
        if (updating || closed) return;
        session.Family = family.SelectedValue as int? ?? 0; session.Sort = (ObjectManagerSort)(sort.SelectedValue as int? ?? 0);
        session.Search = search.Text; session.SelectionOnly = selectionOnly.IsChecked == true; Invalidate(); RefreshRows(true);
    }
    private void Invalidate() { selectionRevision=checked(selectionRevision+1);plan = null; statusKey = null; status.ToolTip = null; RefreshStatus(); UpdateApply(); }
    private void DocumentChanged(object? sender, EventArgs e) { if (closed) return; optionsWindow?.Cancel();optionsWindow=null;EndNavigation();Invalidate(); RefreshEntries(false); }
    private void LanguageChanged(object? sender, EventArgs e) => RefreshLanguage();
    public void RefreshLanguage()
    {
        if (closed) return;
        var previous = updating; updating = true;
        var familyID = family.SelectedValue as int? ?? session.Family; var sortID = sort.SelectedValue as int? ?? (int)session.Sort;
        family.ItemsSource = new[] { new Choice(0, LocalizationService.Get("ProjectKind.Creature")), new(1, LocalizationService.Get("Manager.DoorsTraps")),
            new(2, LocalizationService.Get("Manager.ObjectsSpecials")), new(3, LocalizationService.Get("Manager.AllFamilies")) }; family.SelectedValue = familyID;
        sort.ItemsSource = Enum.GetValues<ObjectManagerSort>().Select(value => new Choice((int)value, LocalizationService.Get("Manager.Sort." + value))).ToArray(); sort.SelectedValue = sortID;
        var keys = new[] { "Batch.Family", "Manager.Name", "Manager.Position", "Editor.Owner", "Manager.TypeID", "Manager.Data", "Manager.Scenario" };
        for (var index = 0; index < keys.Length; index++) ((TextBlock)table.Columns[index + 1].Header).Text = LocalizationService.Get(keys[index]);
        RefreshRows(true); RefreshStatus(); updating = previous; UpdateApply();
    }
    private void RefreshEntries(bool retainSelection) { entries = document.ObjectManagerEntries(); RefreshRows(retainSelection); }
    private void RefreshRows(bool retainSelection)
    {
        var previous = updating; updating = true;
        try
        {
            var selected = retainSelection ? table.SelectedItems.Cast<ManagerRow>().Select(row => (row.Entry.Record.File, row.Entry.Record.Offset)).ToHashSet() : [];
            var chosenFamily = family.SelectedValue as int? ?? 0; var bounds = selectionOnly.IsChecked == true ? selection() : null;
            var query = entries.Where(entry => ObjectManagerListRules.MatchesFamily(entry.Thing.Kind,chosenFamily)
                &&ObjectManagerListRules.Includes(entry.Thing.Position,document.Width,document.Height,selectionOnly.IsChecked==true,bounds));
            var text = search.Text.Trim(); var version = ValueCatalogues.GameVersion(document);
            query = query.Where(entry => text.Length == 0 || entry.Name("en", version).Contains(text, StringComparison.OrdinalIgnoreCase)
                || entry.Name("ru", version).Contains(text, StringComparison.OrdinalIgnoreCase) || entry.Thing.TypeID.ToString().Contains(text, StringComparison.OrdinalIgnoreCase));
            rows = ObjectManagerOrdering.Sort(query, (ObjectManagerSort)(sort.SelectedValue as int? ?? 0), LocalizationService.CurrentLanguage, version)
                .Select(entry => new ManagerRow(entry, version)).ToList();
            table.ItemsSource = rows;
            foreach (var row in rows.Where(row => selected.Contains((row.Entry.Record.File, row.Entry.Record.Offset)))) table.SelectedItems.Add(row);
            RefreshSummary();
        }
        finally { updating = previous; }
    }
    private void RefreshSummary()
    {
        var selected = table.SelectedItems.Cast<ManagerRow>().ToArray();
        summary.Text = string.Format(LocalizationService.Get("Manager.Summary"), rows.Count, selected.Length, selected.Count(row => row.Entry.Scripted),
            plan?.Entries.Count(entry => entry.CanRemove) ?? 0);
    }
    private void RefreshStatus() => status.Text = statusKey is null ? "" : statusKey is "Manager.Applied" or "Manager.CommandApplied"
        ? string.Format(LocalizationService.Get(statusKey), appliedCount) : LocalizationService.Get(statusKey);
    private void UpdateApply()
    {
        var enabled=!closed&&!requestPending&&!navigating&&!managerFlowBusy&&optionsWindow is null;
        apply.IsEnabled = enabled && plan is not null && plan.Entries.Count!=0 && plan.Entries.All(entry => entry.CanRemove);
        managerUndo.IsEnabled=enabled && managerHistory.CanUndo;
        managerModify.IsEnabled=enabled && table.SelectedItems.Count!=0;
        managerAdd.IsEnabled=enabled;
        if(addOptions is {} a)a.IsEnabled=enabled;
        if(modifyOptions is {} m)m.IsEnabled=enabled&&TryOptionsKind(true,out _);
        UpdateNavigationCommands();
        UpdateFlowPreferences();
    }
    private void ManagerHistoryChanged(object? sender,EventArgs args)=>UpdateApply();
    public bool UndoManagerCommand()
    {
        if(closed || requestPending || navigating || managerFlowBusy || optionsWindow is not null || !managerHistory.CanUndo || !canApply() || closed || !managerHistory.CanUndo)return false;
        modificationWindow?.Cancel();modificationWindow=null;additionWindow?.Cancel();additionWindow=null;
        try{return managerHistory.Undo();}
        finally{Invalidate();RefreshEntries(false);}
    }
    public ObjectBatchWindow? CreateAdditionWindow()=>CreateAdditionWindow(false);
    private ObjectBatchWindow? CreateAdditionWindow(bool command)
    {
        if(closed || requestPending || navigating || managerFlowBusy&&!command || optionsWindow is not null || !canApply() || closed)return null;
        modificationWindow?.Cancel();modificationWindow=null;additionWindow?.Cancel();
        var kind=(family.SelectedValue as int? ?? 0) switch {1=>session.KindFor(PlacedThingKind.Door),2=>session.KindFor(PlacedThingKind.Object),_=>PlacedThingKind.Creature};
        var origin=session.ReadDestination(document.Width,document.Height);
        additionWindow=new(document,origin,selection,()=>!closed && canApply(),session.Addition,kind,managerSession:session){ManagerHistory=managerHistory};
        additionWindow.Closed+=(_,_)=>RefreshDestinationCoordinates();
        return additionWindow;
    }
    public bool BuildRemovalPreview()
    {
        if (closed || navigating || managerFlowBusy || optionsWindow is not null) return false; Invalidate();
        try
        {
            if (table.SelectedItems.Count == 0) { statusKey = "Manager.NoSelection"; RefreshStatus(); return false; }
            plan = document.PlanObjectRemoval(CurrentManagerSelection());
            statusKey = plan.Entries.Any(entry => !entry.CanRemove) ? "Manager.Unverified" : "Manager.Ready";
            RefreshSummary(); RefreshStatus(); UpdateApply(); return true;
        }
        catch (Exception exception) when (IsThingRequestFailure(exception))
        { statusKey = "Manager.Failed"; status.ToolTip = exception.Message; RefreshStatus(); return false; }
    }
    public ObjectBatchWindow? CreateModificationWindow()=>CreateModificationWindow(false);
    private ObjectBatchWindow? CreateModificationWindow(bool command)
    {
        if (closed || requestPending || navigating || managerFlowBusy&&!command || optionsWindow is not null || !canApply() || closed) return null;
        try
        {
            var selected = CurrentManagerSelection();
            if (selected.Length == 0) { statusKey = "Manager.NoSelection"; RefreshStatus(); return null; }
            var entries = document.PlanObjectRemoval(selected).Entries;
            if (entries.Select(entry => ObjectModificationRules.Family(entry.Thing.Kind)).Distinct().Count() != 1)
            { statusKey = "Modify.MixedFamilies"; RefreshStatus(); return null; }
            additionWindow?.Cancel();additionWindow=null;modificationWindow?.Cancel();modificationWindow=null;
            using var request=requestPolicy.Begin(document,selected,EditorThingCommand.Modify);
            if(!PresentThingRequest(request,selected))return null;
            var originalSelection=selected;var context=selectionRevision;var revision=document.EditRevision;
            var initialKind=request.Count>1?session.KindFor(entries[0].Thing.Kind):entries[0].Thing.Kind;
            if(command&&ObjectManagerFlowRules.AskOptions(true,request.Count,session.Modification.Template.Flags)
                &&!PresentCommandOptions(initialKind,true,()=>context==selectionRevision&&ManagerSelectionCurrent(originalSelection)))return null;
            if(closed||revision!=document.EditRevision||context!=selectionRevision||!canApply()||revision!=document.EditRevision)return null;
            selected=request.ResultSelection.ToArray();
            if(selected.Length==0){statusKey="ThingRequest.NoEligible";RefreshStatus();return null;}
            entries=document.PlanObjectRemoval(selected).Entries;
            var first = entries[0].Thing;
            modificationWindow = new(document, new(entries.Min(entry => entry.Thing.Position.X), entries.Min(entry => entry.Thing.Position.Y)), selection,
                () => !closed && canApply(), initialKind: request.Count > 1 ? session.KindFor(first.Kind) : first.Kind, modificationSelection: selected,
                managerSession: session, modificationSession: session.Modification, sourceModificationRequest: request){ManagerHistory=managerHistory};
            modificationWindow.Closed+=(_,_)=>RefreshDestinationCoordinates();
            return modificationWindow;
        }
        catch (Exception exception) when (IsThingRequestFailure(exception))
        { statusKey = "Manager.Failed"; status.ToolTip = exception.Message; RefreshStatus(); return null; }
    }
    public bool ApplyRemoval()
    {
        if (closed || navigating || managerFlowBusy || optionsWindow is not null || !apply.IsEnabled || plan is null) return false;
        var preview = plan;
        if (!canApply() || closed || !ReferenceEquals(preview, plan)) return false;
        try
        {
            additionWindow?.Cancel();additionWindow=null;modificationWindow?.Cancel();modificationWindow=null;
            using var request=requestPolicy.Begin(document,preview.Entries.Select(entry=>entry.Record).ToArray(),EditorThingCommand.Remove);
            if(!managerHistory.Execute(()=>PresentThingRequest(request,request.Records),()=>request.IsAccepted))return false;
            appliedCount = request.RemovedCount;
            Invalidate(); RefreshEntries(false); statusKey = "Manager.Applied"; RefreshStatus(); return true;
        }
        catch (Exception exception) when (IsThingRequestFailure(exception))
        { statusKey = "Manager.Failed"; status.ToolTip = exception.Message; RefreshStatus(); UpdateApply(); return false; }
    }
    public void Cancel() { Release(); Close(); }
    private void Release()
    { if (closed) return; closed = true; optionsWindow?.Cancel();optionsWindow=null;EndNavigation(false);confirmationWindow?.Close(); confirmationWindow=null; requestPolicy.Dispose(); additionWindow?.Cancel();additionWindow=null;modificationWindow?.Cancel(); modificationWindow = null; managerHistory.Changed-=ManagerHistoryChanged;managerHistory.Dispose();document.Changed -= DocumentChanged;document.HistoryChanged-=ManagerHistoryChanged; LocalizationService.LanguageChanged -= LanguageChanged; Invalidate(); }
    public sealed class ManagerRow(ObjectManagerEntry entry, int gameVersion)
    {
        public ObjectManagerEntry Entry { get; } = entry;
        public System.Windows.Media.Imaging.BitmapSource Icon => OriginalObjectImages.Get(ObjectManagerImageResources.For(Entry.Thing.Kind, Entry.Thing.TypeID, gameVersion));
        public string Kind => LocalizationService.Get("ProjectKind." + (Entry.Thing.Kind == PlacedThingKind.SpecialItem ? "MagicalObject" : Entry.Thing.Kind.ToString()));
        public string Name => Entry.Name(LocalizationService.CurrentLanguage, gameVersion);
        public string Position => $"{Entry.Thing.Position.X}, {Entry.Thing.Position.Y}";
        public string Owner => ValueCatalogues.Owners.FirstOrDefault(choice => choice.Value == Entry.Thing.Owner.ToString())?.Name(LocalizationService.CurrentLanguage) ?? Entry.Thing.Owner.ToString();
        public uint ID => Entry.Thing.TypeID;
        public int Data => Entry.DataAll;
        public string Scripted => LocalizationService.Get(Entry.Scripted ? "Manager.Yes" : "Manager.No");
    }
}
