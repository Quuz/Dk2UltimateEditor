using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record DecorationForm(GlobalDecorationsDraft Baseline, IReadOnlyDictionary<string, string> Inputs);
    private readonly Dictionary<string, DecorationForm> decorationForms = [];
    private readonly Dictionary<string, ComboBox> decorationControls = [];
    private string? decorationFamily;
    private static string DecorationText(string key) => DefinitionText("Decorations." + key);
    private void ClearDecorationControls()
    {
        foreach (var control in decorationControls.Values) UnregisterName(control.Name);
        decorationControls.Clear(); DefinitionDecorationFields.Children.Clear();
    }
    private void RefreshDecorations()
    {
        if (DefinitionDecorationPanel is null) return;
        var row = (DefinitionPicker.SelectedItem as DefinitionView)?.Row;
        var section = (DefinitionSection.SelectedItem as DefinitionChoice)?.ID;
        var visible = row is not null && GlobalDefinitionDecorations.Families.Contains(row.Family) && (row.Family != "Creatures" || section == "Parameters");
        DefinitionDecorationPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ClearDecorationControls(); decorationFamily = visible ? row!.Family : null;
        if (!visible)
        {
            if (document is null) decorationForms.Clear();
            DefinitionDecorationApply.IsEnabled = DefinitionDecorationRevert.IsEnabled = false; DefinitionDecorationHint.Text = ""; return;
        }
        var current = document!.CreateGlobalDecorationsDraft(row!);
        if (!decorationForms.TryGetValue(row!.Family, out var form) || !form.Baseline.SameBaseline(current))
            decorationForms[row.Family] = form = new(current, GlobalDefinitionDecorations.Fields(row.Family).ToDictionary(field => field, current.Raw));
        foreach (var field in GlobalDefinitionDecorations.Fields(row.Family))
        {
            var enabled = GlobalDefinitionDecorations.Enabled(row.Family, row.Index, field);
            var cell = new StackPanel { Width = 330, Margin = new(0, 0, 12, 8) };
            cell.Children.Add(new TextBlock { Text = DefinitionFieldLabel(row.Family,field) + (enabled ? "" : " · " + DecorationText("Inactive")), TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 4) });
            var options = GlobalDefinitionDecorations.Choices(row.Family, field).Select(choice => new DefinitionChoice(choice.Value, $"#{uint.Parse(choice.Value, CultureInfo.InvariantCulture):X3} · " + choice.Name(Localization.LocalizationService.CurrentLanguage))).ToList();
            if (form.Inputs[field].Length > 0 && options.All(choice => choice.ID != form.Inputs[field])) options.Add(new(form.Inputs[field], form.Inputs[field] + " · " + DecorationText("Unknown")));
            var combo = new ComboBox { Name = "DefinitionDecoration" + field.Replace("[", "").Replace("]", ""), DisplayMemberPath = "Label", ItemsSource = options, SelectedItem = options.FirstOrDefault(choice => choice.ID == form.Inputs[field]), IsEnabled = form.Baseline.Editable && enabled };
            combo.ToolTip = (combo.SelectedItem as DefinitionChoice)?.Label;
            combo.SelectionChanged += DefinitionDecorationChoice_Changed; RegisterName(combo.Name, combo); decorationControls.Add(field, combo); cell.Children.Add(combo); DefinitionDecorationFields.Children.Add(cell);
        }
        RefreshDecorationHint();
    }
    private void CaptureDecorations()
    {
        if (decorationFamily is null || !decorationForms.TryGetValue(decorationFamily, out var form)) return;
        decorationForms[decorationFamily] = form with { Inputs = decorationControls.ToDictionary(item => item.Key, item => item.Value.SelectedItem is DefinitionChoice choice ? choice.ID : form.Inputs[item.Key]) };
        foreach (var control in decorationControls.Values) control.ToolTip = (control.SelectedItem as DefinitionChoice)?.Label;
    }
    private void RefreshDecorationHint()
    {
        if (decorationFamily is null || !decorationForms.TryGetValue(decorationFamily, out var form)) return;
        var changed = form.Inputs.Any(input => input.Value != form.Baseline.Raw(input.Key));
        DefinitionDecorationApply.IsEnabled = form.Baseline.Editable; DefinitionDecorationRevert.IsEnabled = form.Baseline.Editable && changed;
        DefinitionDecorationHint.Text = DecorationText(!form.Baseline.Editable ? "ReadOnly" : "Hint") + (changed ? " " + DecorationText("Draft") : "");
    }
    private void DefinitionDecorationChoice_Changed(object sender, SelectionChangedEventArgs e)
    { if (!updatingDefinitions) { InvalidateDefinitionEditing(); CaptureDecorations(); RefreshDecorationHint(); } }
    private void DefinitionDecorationEdit_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || decorationFamily is null || DefinitionPicker.SelectedItem is not DefinitionView view || !decorationForms.TryGetValue(decorationFamily, out var form)) return;
        try
        {
            if ((sender as Button)?.Tag as string != "Revert")
            {
                CaptureDecorations(); form = decorationForms[decorationFamily];
                var prepared = document.UpdateGlobalDecorations(form.Baseline, form.Inputs.Select(input => new GlobalDecorationInput(input.Key, input.Value)));
                if (!CommitFields()) return;
                if (!prepared.SameBaseline(document.CreateGlobalDecorationsDraft(view.Row))) { DefinitionMessage("Failed", DecorationText("Changed")); return; }
                document.ApplyGlobalDecorations(prepared);
            }
            decorationForms.Remove(form.Baseline.Family); InvalidateDefinitionEditing(); RefreshDefinitions(); DefinitionMessage("Applied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        { DefinitionMessage("Failed", exception.Message); }
    }
}
