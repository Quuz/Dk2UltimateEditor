using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record RuleCellView(MapRuleCell Cell, string Display);
    private sealed record RuleRowView(string Name, IReadOnlyDictionary<int, RuleCellView> Cells, int? ImageResource = null)
    {
        public string Label => Name;
        public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalScenarioImages.For(ImageResource);
    }
    private bool updatingRules;
    private string? ruleStatusKey;
    private MapRuleKind RuleKind => RuleKindPicker.SelectedValue is int value ? (MapRuleKind)value : MapRuleKind.Rooms;
    private void ResetRuleEditing()
    {
        ruleStatusKey = null; RuleStatus.Text = ""; RuleStatus.ToolTip = null; RuleConfirmed.IsChecked = false;
        RuleNumber.Text = "0"; RuleUnspecified.IsChecked = false;
        RefreshRuleMatrix();
    }
    private IReadOnlyList<MapRuleKey> SelectedRuleKeys() => RuleTable.SelectedCells.Where(info => info.Item is RuleRowView && info.Column.DisplayIndex > 0)
        .Select(info => ((RuleRowView)info.Item).Cells.GetValueOrDefault(info.Column.DisplayIndex)?.Cell)
        .OfType<MapRuleCell>().Where(cell => cell.Editable).Select(cell => cell.Key).Distinct().ToArray();
    private void RefreshRuleMatrix()
    {
        if (RuleTable is null || RuleStatus is null) return;
        var selected = SelectedRuleKeys().ToHashSet(); var kind = RuleKind; var selectedValue = (RuleValuePicker.SelectedItem as NamedChoiceView)?.Choice.Value;
        updatingRules = true;
        try
        {
            RuleKindPicker.ItemsSource = Enum.GetValues<MapRuleKind>().Select(value => new Choice((int)value, LocalizationService.Get("Editor.RuleKind." + value))).ToArray();
            RuleKindPicker.SelectedValue = (int)kind;
            RulePanel.IsEnabled = document is not null;
            var numeric = kind is MapRuleKind.CreaturePool or MapRuleKind.CreatureModifier;
            RuleValuePicker.Visibility = numeric ? Visibility.Collapsed : Visibility.Visible;
            RuleNumber.Visibility = RuleUnspecified.Visibility = numeric ? Visibility.Visible : Visibility.Collapsed;
            RuleNumber.IsEnabled = RuleUnspecified.IsChecked != true;
            var values = kind == MapRuleKind.Alliance ? new[] { new ValueChoice("0", "No alliance", "Нет союза"), new("1", "Allied", "Союзники") } : MapRules.Availability;
            var choices = values.Select(choice => new NamedChoiceView(choice, ChoiceLabel(choice))).ToArray();
            RuleValuePicker.ItemsSource = choices; RuleValuePicker.SelectedItem = choices.FirstOrDefault(choice => choice.Choice.Value == selectedValue) ?? choices.First();
            RuleTable.Columns.Clear();
            RuleTable.Columns.Add(new DataGridTemplateColumn { DisplayIndex = 0, Header = LocalizationService.Get("Editor.RuleElement"), CellTemplate = OriginalObjectImages.NamedTemplate(), Width = new DataGridLength(240) });
            var columnCount = kind == MapRuleKind.CreatureModifier ? 4 : 7;
            for (var index = 1; index <= columnCount; index++) RuleTable.Columns.Add(new DataGridTextColumn
            {
                DisplayIndex = index,
                Header = kind == MapRuleKind.CreatureModifier ? LocalizationService.Get("Editor.RuleParameter." + index) : ValueCatalogues.Owners[index - 1].Name(LocalizationService.CurrentLanguage),
                Binding = new Binding($"Cells[{index}].Display"), Width = new DataGridLength(135)
            });
            var cells = (document?.MapRuleCells() ?? []).Where(cell => cell.Key.Kind == kind).ToDictionary(cell => cell.Key);
            RuleCellView View(MapRuleCell cell)
            {
                var display = cell.Value is null ? LocalizationService.Get("Editor.RuleUnspecified")
                    : MapRules.IsAvailability(kind) ? MapRules.Availability.FirstOrDefault(choice => choice.Value == cell.Value.Value.ToString(CultureInfo.InvariantCulture))?.Name(LocalizationService.CurrentLanguage) ?? cell.Value.Value.ToString(CultureInfo.InvariantCulture)
                    : kind == MapRuleKind.Alliance ? LocalizationService.Get(cell.Value == 0 ? "Editor.RuleNo" : "Editor.RuleYes") : cell.Value.Value.ToString(CultureInfo.InvariantCulture);
                if (!cell.Editable) display += " · " + LocalizationService.Get("Editor.RuleReadOnly");
                if (cell.RecordCount > 1) display += " · " + cell.RecordCount;
                return new(cell, display);
            }
            var rows = new List<RuleRowView>();
            if (document is not null)
                if (kind == MapRuleKind.Alliance)
                    for (uint player = 1; player <= 7; player++) rows.Add(new(ValueCatalogues.Owners[(int)player - 1].Name(LocalizationService.CurrentLanguage), Enumerable.Range(1, 7).ToDictionary(column => column, column => View(cells[new(kind, Math.Min(player, (uint)column), Math.Max(player, (uint)column))]))));
                else foreach (var element in MapRules.Elements(kind)) rows.Add(new(element.Name.Name(LocalizationService.CurrentLanguage), Enumerable.Range(1, columnCount).ToDictionary(column => column,
                    column => View(cells[new(kind, kind == MapRuleKind.CreatureModifier ? 0 : (uint)column, element.ID, kind == MapRuleKind.CreatureModifier ? (uint)column : 0)])), MapRuleImageResources.ForRule(kind,element.Index)));
            RuleTable.ItemsSource = rows;
            foreach (var row in rows) foreach (var column in RuleTable.Columns.Skip(1))
                if (row.Cells.TryGetValue(column.DisplayIndex, out var cell) && selected.Contains(cell.Cell.Key)) RuleTable.SelectedCells.Add(new(row, column));
            if (ruleStatusKey is { } key) RuleStatus.SetResourceReference(TextBlock.TextProperty, key);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException)
        { RuleTable.ItemsSource = null; RuleMessage("Editor.RuleFailed", exception.Message); }
        finally { updatingRules = false; }
    }
    private void RuleKind_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingRules || RuleTable is null || RuleStatus is null) return;
        RuleTable.SelectedCells.Clear(); RuleConfirmed.IsChecked = false; ruleStatusKey = null; RuleStatus.Text = ""; RuleStatus.ToolTip = null; RefreshRuleMatrix();
    }
    private void RuleUnspecified_Click(object sender, RoutedEventArgs e) => RuleNumber.IsEnabled = RuleUnspecified.IsChecked != true;
    private void RuleMessage(string key, string? detail = null) { ruleStatusKey = key; RuleStatus.ToolTip = detail; RuleStatus.SetResourceReference(TextBlock.TextProperty, key); }
    private void RuleOperation(Action action)
    {
        if (document is null || !CommitFields()) return;
        try { action(); RuleConfirmed.IsChecked = false; RefreshRuleMatrix(); RuleMessage("Editor.RuleApplied"); }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException or FormatException)
        { RuleMessage("Editor.RuleFailed", exception.Message); }
    }
    private void RuleApply_Click(object sender, RoutedEventArgs e)
    {
        if (document is null) return;
        var keys = SelectedRuleKeys();
        RuleOperation(() =>
        {
            if (keys.Count == 0) throw new ArgumentException("Select editable table cells first.");
            var kind = RuleKind; long? value;
            if (kind is MapRuleKind.CreaturePool or MapRuleKind.CreatureModifier) value = RuleUnspecified.IsChecked == true ? null : long.Parse(RuleNumber.Text, CultureInfo.InvariantCulture);
            else { var selected = ((NamedChoiceView)RuleValuePicker.SelectedItem).Choice.Value; value = selected == "255" ? null : long.Parse(selected, CultureInfo.InvariantCulture); }
            document.SetMapRules(keys.Select(key => new MapRuleChange(key, value)));
        });
    }
    private void RuleClear_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || RuleConfirmed.IsChecked != true) return;
        var keys = SelectedRuleKeys(); RuleOperation(() => { if (keys.Count == 0) throw new ArgumentException("Select editable cells first."); document.SetMapRules(keys.Select(key => new MapRuleChange(key, null))); });
    }
    private void RuleReset_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || RuleConfirmed.IsChecked != true) return;
        var keys = (sender as Button)?.Tag as string == "Table" ? MapRules.Keys().Where(key => key.Kind == RuleKind).ToArray() : SelectedRuleKeys();
        RuleOperation(() => { if (keys.Count == 0) throw new ArgumentException("Select editable cells first."); document.ResetMapRules(keys); });
    }
}
