using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class ScenarioNamesWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly Func<bool> ownerCurrent;
    private readonly Func<bool> commitFields;
    private readonly DataGrid variables = Grid(), timers = Grid();
    private readonly TextBox nameInput = new() { Margin = new(4), MaxLength = 63, MinWidth = 300 };
    private readonly TextBlock status = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly Button apply = new(), discard = new();
    private ScenarioNameDraft? draft;
    private bool refreshing, applying, stale, closed, referencesVerified;
    private string? statusKey;
    private sealed record Row(ScenarioNameKind Kind, int Index, string Display, string StoredName, int References)
    { public System.Windows.Media.Imaging.BitmapSource? Icon=>OriginalControlIcons.For(ControlImageResources.ForScenarioName(Kind)); }
    public ScenarioNamesWindow(NativeMapDocument document, Func<bool> ownerCurrent, Func<bool> commitFields)
    {
        this.document = document; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields;
        NameScope.SetNameScope(this, new NameScope()); SetResourceReference(TitleProperty, "ScenarioNames.Title");
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 760; Height = 560; MinWidth = 680; MinHeight = 500; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel;
        panel.SetResourceReference(Panel.BackgroundProperty, "Brush.Background"); panel.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Brush.Text");
        var info = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4) }; info.SetResourceReference(TextBlock.TextProperty, "ScenarioNames.Explanation"); DockPanel.SetDock(info, Dock.Top); panel.Children.Add(info);
        var bottom = new StackPanel(); DockPanel.SetDock(bottom, Dock.Bottom); panel.Children.Add(bottom);
        var label = new TextBlock { Margin = new(4) }; label.SetResourceReference(TextBlock.TextProperty, "ScenarioNames.RawName"); bottom.Children.Add(label);
        bottom.Children.Add(nameInput); RegisterName("ScenarioNameInput", nameInput);
        bottom.Children.Add(status); RegisterName("ScenarioNamesStatus", status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        apply.SetResourceReference(ContentControl.ContentProperty, "ScenarioNames.Apply"); apply.Click += (_, _) => ApplyName(); RegisterName("ScenarioNameApply", apply);
        discard.SetResourceReference(ContentControl.ContentProperty, "ScenarioNames.Discard"); discard.Click += (_, _) => ResetDraft(); RegisterName("ScenarioNameDiscard", discard);
        var close = new Button { IsCancel = true }; close.SetResourceReference(ContentControl.ContentProperty, "ScenarioNames.Close"); close.Click += (_, _) => Close();
        buttons.Children.Add(apply); buttons.Children.Add(discard); buttons.Children.Add(close); bottom.Children.Add(buttons);
        var tables = new System.Windows.Controls.Grid(); tables.ColumnDefinitions.Add(new()); tables.ColumnDefinitions.Add(new());
        AddList(variables, "ScenarioNames.Variables", 0); AddList(timers, "ScenarioNames.Timers", 1); panel.Children.Add(tables);
        RegisterName("ScenarioVariables", variables); RegisterName("ScenarioTimers", timers);
        variables.SelectionChanged += Selected; timers.SelectionChanged += Selected;
        LocalizationService.LanguageChanged += LanguageChanged; document.Changed += DocumentChanged;
        Closed += (_, _) => { closed = true; draft = null; document.Changed -= DocumentChanged; LocalizationService.LanguageChanged -= LanguageChanged; };
        Refresh(false); variables.SelectedIndex = variables.Items.Count == 0 ? -1 : 0;
        void AddList(DataGrid grid, string key, int column)
        {
            var group = new DockPanel { Margin = new(4) }; var title = new TextBlock { Margin = new(4) }; title.SetResourceReference(TextBlock.TextProperty, key);
            DockPanel.SetDock(title, Dock.Top); group.Children.Add(title); group.Children.Add(grid); System.Windows.Controls.Grid.SetColumn(group, column); tables.Children.Add(group);
        }
    }
    private static DataGrid Grid()
    {
        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, CanUserDeleteRows = false, SelectionMode = DataGridSelectionMode.Single };
        grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Index"), Width = 48 });
        grid.Columns.Add(new DataGridTemplateColumn { CellTemplate=OriginalControlIcons.Template("Display","StoredName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 160 });
        grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("References"), Width = 64 }); return grid;
    }
    private Row? SelectedRow => variables.SelectedItem as Row ?? timers.SelectedItem as Row;
    private void Selected(object sender, SelectionChangedEventArgs args)
    {
        if (refreshing || closed) return;
        refreshing = true;
        try { if (ReferenceEquals(sender, variables) && variables.SelectedItem is not null) timers.SelectedItem = null;
            else if (ReferenceEquals(sender, timers) && timers.SelectedItem is not null) variables.SelectedItem = null; }
        finally { refreshing = false; }
        ResetDraft();
    }
    private void Refresh(bool preserveDraft)
    {
        var selected = SelectedRow; refreshing = true;
        try
        {
            var table = document.ReadScenarioNames();
            referencesVerified = table.ReferencesVerified;
            var rows = table.Slots.Select(row => new Row(row.Kind, row.Index,
                row.Kind == ScenarioNameKind.Variable && row.Index == 128 ? LocalizationService.Get("ScenarioNames.Score")
                : row.Kind == ScenarioNameKind.Timer && row.Index == 16 ? LocalizationService.Get("ScenarioNames.TimeLimit")
                : row.StoredName.Length == 0 ? LocalizationService.Get("ScenarioNames.NoName") : row.StoredName, row.StoredName, row.References)).ToArray();
            variables.ItemsSource = rows.Where(row => row.Kind == ScenarioNameKind.Variable).ToArray(); timers.ItemsSource = rows.Where(row => row.Kind == ScenarioNameKind.Timer).ToArray();
            if (selected is not null)
            {
                var grid = selected.Kind == ScenarioNameKind.Variable ? variables : timers;
                grid.SelectedItem = grid.Items.Cast<Row>().FirstOrDefault(row => row.Index == selected.Index);
            }
            foreach (var grid in new[] { variables, timers })
            { grid.Columns[0].Header = LocalizationService.Get("ScenarioNames.Index"); grid.Columns[1].Header = LocalizationService.Get("ScenarioNames.Name"); grid.Columns[2].Header = LocalizationService.Get("ScenarioNames.References"); }
            if (!table.Supported) statusKey = "ScenarioNames.Unsupported";
            else if (!stale && statusKey != "ScenarioNames.Invalid") statusKey = !table.ReferencesVerified ? "ScenarioNames.UnverifiedCounts" : null;
        }
        finally { refreshing = false; }
        if (!preserveDraft) ResetDraft(); else UpdateStatus();
    }
    private void ResetDraft()
    {
        if (closed) return;
        stale = false;
        draft = SelectedRow is { } row && ownerCurrent() ? document.CreateScenarioNameDraft(row.Kind, row.Index) : null;
        nameInput.Text = draft?.Name ?? ""; statusKey = !document.CanEditScenarioNames ? "ScenarioNames.Unsupported" : !referencesVerified ? "ScenarioNames.UnverifiedCounts" : null; UpdateStatus();
    }
    private void UpdateStatus()
    {
        status.Text = statusKey is null ? "" : LocalizationService.Get(statusKey);
        apply.IsEnabled = !closed && !stale && draft is not null && ownerCurrent() && document.CanEditScenarioNames; discard.IsEnabled = !closed && draft is not null && ownerCurrent();
        nameInput.IsEnabled = !closed && draft is not null && ownerCurrent();
    }
    private void DocumentChanged(object? sender, EventArgs args)
    {
        if (closed || applying) return;
        stale = true; statusKey = "ScenarioNames.Stale"; Refresh(true);
    }
    private void LanguageChanged(object? sender, EventArgs args) { if (!closed) Refresh(true); }
    public bool ApplyName()
    {
        if (closed || !ownerCurrent() || draft is null || !commitFields() || closed || !ownerCurrent() || stale) return false;
        try
        {
            applying = true; document.ApplyEditorScenarioNameDraft(draft, nameInput.Text); applying = false;
            Refresh(false); return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { if (exception is InvalidOperationException) stale = true;
            statusKey = stale ? "ScenarioNames.Stale" : "ScenarioNames.Invalid"; UpdateStatus(); return false; }
        finally { applying = false; }
    }
}
