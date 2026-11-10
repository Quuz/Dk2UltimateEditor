using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record RuleForm(GlobalRulesDraft Baseline, IReadOnlyDictionary<string, string> Inputs);
    private readonly Dictionary<string, RuleForm> globalRuleForms = [];
    private readonly Dictionary<string, Control> globalRuleControls = [];
    private string? globalRuleFamily;
    private static string RuleText(string key) => DefinitionText("Rules." + key);
    private void ClearGlobalRuleControls()
    {
        foreach (var control in globalRuleControls.Values) UnregisterName(control.Name);
        globalRuleControls.Clear(); DefinitionRuleFields.Children.Clear();
    }
    private void RefreshGlobalRules()
    {
        if (DefinitionRulePanel is null) return;
        var row = (DefinitionPicker.SelectedItem as DefinitionView)?.Row;
        var visible = row is not null && GlobalDefinitionRules.Families.Contains(row.Family);
        DefinitionRulePanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ClearGlobalRuleControls(); globalRuleFamily = visible ? row!.Family : null;
        if (!visible)
        {
            if (document is null) globalRuleForms.Clear();
            DefinitionRuleApply.IsEnabled = DefinitionRuleRevert.IsEnabled = false; DefinitionRuleHint.Text = ""; return;
        }
        var current = document!.CreateGlobalRulesDraft(row!);
        if (!globalRuleForms.TryGetValue(row!.Family, out var form) || !form.Baseline.SameBaseline(current))
            globalRuleForms[row.Family] = form = new(current, GlobalDefinitionRules.Fields(row.Family).ToDictionary(field => field, current.Raw));
        foreach (var field in GlobalDefinitionRules.Fields(row.Family))
        {
            var cell = new StackPanel { Width = 330, Margin = new(0, 0, 12, 8) };
            var caption = new TextBlock { Name = "DefinitionRuleLabel" + field, Text = FieldLabel(field), TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 4) };
            cell.Children.Add(caption);
            var choices = GlobalDefinitionRules.Choices(row.Family, field); Control control;
            if (choices.Count > 0)
            {
                var options = choices.Select(choice => new DefinitionChoice(choice.Value, $"#{uint.Parse(choice.Value, CultureInfo.InvariantCulture):X2} · " + choice.Name(Localization.LocalizationService.CurrentLanguage))).ToList();
                if (form.Inputs[field].Length > 0 && options.All(choice => choice.ID != form.Inputs[field])) options.Add(new(form.Inputs[field], form.Inputs[field] + " · " + RuleText("Unknown")));
                var combo = new ComboBox { DisplayMemberPath = "Label", ItemsSource = options, SelectedItem = options.FirstOrDefault(choice => choice.ID == form.Inputs[field]) };
                combo.SelectionChanged += DefinitionRuleChoice_Changed; control = combo;
            }
            else
            {
                var text = new TextBox { Text = form.Inputs[field] }; text.TextChanged += DefinitionRuleInput_Changed; control = text;
                var hint = new TextBlock { Text = "0 … 32767", TextWrapping = TextWrapping.Wrap, Margin = new(0, 4, 0, 0) }; hint.SetResourceReference(StyleProperty, "MutedText"); cell.Children.Add(hint);
            }
            control.Name = "DefinitionRule" + field; RegisterName(control.Name, control); globalRuleControls.Add(field, control); cell.Children.Insert(1, control); DefinitionRuleFields.Children.Add(cell);
        }
        RefreshGlobalRuleEnabled();
    }
    private void CaptureGlobalRules()
    {
        if (globalRuleFamily is null || !globalRuleForms.TryGetValue(globalRuleFamily, out var form)) return;
        globalRuleForms[globalRuleFamily] = form with { Inputs = globalRuleControls.ToDictionary(item => item.Key, item => item.Value is TextBox text ? text.Text : (item.Value as ComboBox)?.SelectedItem is DefinitionChoice choice ? choice.ID : form.Inputs[item.Key]) };
    }
    private void RefreshGlobalRuleEnabled()
    {
        if (globalRuleFamily is null || !globalRuleForms.TryGetValue(globalRuleFamily, out var form)) return;
        foreach (var (field, control) in globalRuleControls)
        {
            control.IsEnabled = form.Baseline.Editable && GlobalDefinitionRules.Enabled(form.Baseline.Family, form.Baseline.Index, field) && (field != "TriggerData" || GlobalDefinitionRules.KnownTrigger(form.Inputs["TriggerType"]));
            if (field == "TriggerData") ((TextBlock)((StackPanel)control.Parent).Children[0]).Text = RuleText("Trigger." + (GlobalDefinitionRules.KnownTrigger(form.Inputs["TriggerType"]) ? form.Inputs["TriggerType"] : "Unknown"));
        }
        var changed = form.Inputs.Any(input => input.Value != form.Baseline.Raw(input.Key));
        DefinitionRuleApply.IsEnabled = form.Baseline.Editable; DefinitionRuleRevert.IsEnabled = form.Baseline.Editable && changed;
        DefinitionRuleHint.Text = RuleText(!form.Baseline.Editable ? "ReadOnly" : "Hint") + (changed ? " " + RuleText("Draft") : "");
    }
    private void DefinitionRuleChoice_Changed(object sender, SelectionChangedEventArgs e)
    { if (!updatingDefinitions) { InvalidateDefinitionEditing(); CaptureGlobalRules(); RefreshGlobalRuleEnabled(); } }
    private void DefinitionRuleInput_Changed(object sender, TextChangedEventArgs e)
    { if (!updatingDefinitions) { InvalidateDefinitionEditing(); CaptureGlobalRules(); RefreshGlobalRuleEnabled(); } }
    private void DefinitionRuleEdit_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || globalRuleFamily is null || DefinitionPicker.SelectedItem is not DefinitionView view || !globalRuleForms.TryGetValue(globalRuleFamily, out var form)) return;
        try
        {
            if ((sender as Button)?.Tag as string != "Revert")
            {
                CaptureGlobalRules(); form = globalRuleForms[globalRuleFamily];
                var prepared = document.UpdateGlobalRules(form.Baseline, form.Inputs.Select(input => new GlobalRuleInput(input.Key, input.Value)));
                if (!CommitFields()) return;
                if (!prepared.SameBaseline(document.CreateGlobalRulesDraft(view.Row))) { DefinitionMessage("Failed", RuleText("Changed")); return; }
                document.ApplyGlobalRules(prepared);
            }
            globalRuleForms.Remove(form.Baseline.Family); InvalidateDefinitionEditing(); RefreshDefinitions(); DefinitionMessage("Applied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        { DefinitionMessage("Failed", exception.Message); }
    }
}
