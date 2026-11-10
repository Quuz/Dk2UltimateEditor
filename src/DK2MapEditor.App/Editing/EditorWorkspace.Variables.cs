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
    private sealed record VariableView(MapVariableCell Cell, string ID, string Group, string Name, string Value, string Default, string Range, string State);
    private bool updatingVariables;
    private string? variableStatusKey;
    private void InvalidateVariableEditing()
    {
        VariableConfirmed.IsChecked = false; variableStatusKey = null; VariableStatus.Text = ""; VariableStatus.ToolTip = null;
    }
    private void ResetVariableEditing()
    {
        InvalidateVariableEditing();
        updatingVariables = true;
        try { VariableNumber.Text = "0"; VariableSearch.Text = ""; VariableGroup.SelectedIndex = 0; VariableTable.SelectedItems.Clear(); }
        finally { updatingVariables = false; }
        RefreshVariables();
    }
    private void RefreshVariables()
    {
        if (VariableTable is null || VariableStatus is null) return;
        var selected = VariableTable.SelectedItems.Cast<VariableView>().Select(view => view.Cell.Definition.ID).ToHashSet();
        var group = (VariableGroup.SelectedItem as NamedChoiceView)?.Choice.Value ?? "*";
        updatingVariables = true;
        try
        {
            var groups = new[] { new ValueChoice("*", "All groups", "Все группы") }.Concat(MapVariables.Definitions.Select(item => item.Group).Distinct()).ToArray();
            VariableGroup.ItemsSource = groups.Select(choice => new NamedChoiceView(choice, ChoiceLabel(choice))).ToArray();
            VariableGroup.SelectedItem = VariableGroup.Items.Cast<NamedChoiceView>().FirstOrDefault(choice => choice.Choice.Value == group) ?? VariableGroup.Items[0];
            VariablePanel.IsEnabled = document is not null; VariableTable.IsEnabled = document is not null;
            VariableClear.IsEnabled = document is { IsLegacyProject: false };
            VariableTable.Columns.Clear();
            var columns = new[] { ("ID", "Editor.VariableID", 70), ("Group", "Editor.VariableGroup", 170), ("Name", "Editor.VariableName", 430), ("Value", "Editor.VariableValue", 120), ("Default", "Editor.VariableDefault", 130), ("Range", "Editor.VariableRange", 150), ("State", "Editor.VariableState", 220) };
            foreach (var (property, key, width) in columns) VariableTable.Columns.Add(new DataGridTextColumn
            { DisplayIndex = VariableTable.Columns.Count, Header = LocalizationService.Get(key), Binding = new Binding(property), Width = new DataGridLength(width) });
            string Text(string key) => LocalizationService.Get("Editor.Variable" + key);
            var rows = (document?.MapVariableCells() ?? []).Select(cell =>
            {
                var item = cell.Definition;
                var state = cell.Bits is null ? Text("Missing") : cell.Bits == item.DefaultBits ? Text("DefaultState") : Text("Changed");
                if (!cell.Editable) state += " · " + Text("ReadOnly");
                if (cell.RecordCount > 1) state += " · " + string.Format(Text("Duplicates"), cell.RecordCount);
                return new VariableView(cell, $"{item.ID:X2}", item.Group.Name(LocalizationService.CurrentLanguage), item.Name.Name(LocalizationService.CurrentLanguage),
                    cell.Bits is { } bits ? item.Display(bits) : "—", item.Display(item.DefaultBits), item.FixedPoint ? "20.12" : $"{item.Minimum} … {item.Maximum}", state);
            }).Where(view => (group == "*" || view.Cell.Definition.Group.Value == group) &&
                (string.IsNullOrWhiteSpace(VariableSearch.Text) || new[] { view.ID, view.Group, view.Name, view.Cell.Definition.Name.English, view.Cell.Definition.Name.Russian }
                    .Any(text => text.Contains(VariableSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase)))).ToArray();
            VariableTable.ItemsSource = rows;
            foreach (var row in rows.Where(row => selected.Contains(row.Cell.Definition.ID))) VariableTable.SelectedItems.Add(row);
            if (variableStatusKey is { } status) VariableStatus.SetResourceReference(TextBlock.TextProperty, status);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException)
        { VariableTable.ItemsSource = null; VariableMessage("Editor.VariableFailed", exception.Message); }
        finally { updatingVariables = false; }
    }
    private void VariableFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (updatingVariables || VariableTable is null || VariableStatus is null) return;
        InvalidateVariableEditing(); RefreshVariables();
    }
    private void VariableSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingVariables || VariableConfirmed is null) return;
        VariableConfirmed.IsChecked = false;
        if (VariableTable.SelectedItems.Count == 1 && VariableTable.SelectedItem is VariableView { Cell.Editable: true } view)
            VariableNumber.Text = unchecked((int)(view.Cell.Bits ?? view.Cell.Definition.DefaultBits)).ToString(CultureInfo.InvariantCulture);
    }
    private void VariableMessage(string key, string? detail = null)
    { variableStatusKey = key; VariableStatus.ToolTip = detail; VariableStatus.SetResourceReference(TextBlock.TextProperty, key); }
    private void VariableEdit_Click(object sender, RoutedEventArgs e)
    {
        if (document is null) return;
        var operation = (sender as Button)?.Tag as string ?? "Apply";
        var ids = operation == "All" ? document.MapVariableCells().Where(cell => cell.Resettable).Select(cell => cell.Definition.ID).ToArray()
            : VariableTable.SelectedItems.Cast<VariableView>().Select(view => view.Cell.Definition.ID).ToArray();
        if (operation != "Apply" && VariableConfirmed.IsChecked != true || !CommitFields()) return;
        // A committed property edit invalidates confirmation; do not reset
        // values on authority that belonged to an earlier document state.
        if (operation != "Apply" && VariableConfirmed.IsChecked != true) return;
        try
        {
            if (ids.Length == 0) throw new ArgumentException("Select variables first.");
            if (operation == "Apply")
            {
                var value = int.Parse(VariableNumber.Text, CultureInfo.InvariantCulture);
                document.SetMapVariables(ids.Select(id => new MapVariableChange(id, value)));
            }
            else if (operation == "Clear") document.ClearMapVariableOverrides(ids);
            else document.ResetMapVariables(ids);
            VariableConfirmed.IsChecked = false; RefreshVariables(); VariableMessage("Editor.VariableApplied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException or FormatException)
        { VariableMessage("Editor.VariableFailed", exception.Message); }
    }
}
