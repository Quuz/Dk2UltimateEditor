using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record GeneratorForm(EffectGeneratorDraft Baseline, IReadOnlyDictionary<string, string> Inputs);
    private GeneratorForm? generatorForm;
    private readonly Dictionary<string, Control> generatorControls = [];
    private Slider? generatorSlider;
    private bool updatingGenerator;
    private static string GeneratorText(string key) => Localization.LocalizationService.Get("Editor.EffectGenerator." + key);
    private void ClearGeneratorControls()
    {
        foreach (var control in generatorControls.Values) UnregisterName(control.Name);
        generatorControls.Clear(); generatorSlider = null; EffectGeneratorFields.Children.Clear();
    }
    private void RefreshEffectGenerator(NativeRecord? record)
    {
        if (EffectGeneratorPanel is null) return;
        updatingGenerator = true;
        try
        {
            ClearGeneratorControls(); var visible = record is not null && EffectGenerators.IsEffect(record);
            EffectGeneratorPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (!visible || document is null)
            {
                generatorForm = null; EffectGeneratorApply.IsEnabled = EffectGeneratorRevert.IsEnabled = false; EffectGeneratorHint.Text = ""; EffectGeneratorStatus.Text = ""; EffectGeneratorStatus.ToolTip = null; return;
            }
            var current = document.CreateEffectGeneratorDraft(record!);
            if (generatorForm is null || !generatorForm.Baseline.SameBaseline(current))
            {
                generatorForm = new(current, EffectGenerators.Fields.ToDictionary(field => field, current.Raw));
                EffectGeneratorStatus.Text = ""; EffectGeneratorStatus.ToolTip = null;
            }
            foreach (var field in EffectGenerators.Fields)
            {
                var cell = new StackPanel { Width = 300, Margin = new(0, 0, 12, 8) };
                cell.Children.Add(new TextBlock { Text = GeneratorText(field), TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 4) }); Control control;
                if (field == "Frequency")
                {
                    var number = new TextBox { Text = generatorForm.Inputs[field] }; number.TextChanged += EffectGeneratorInput_Changed; control = number;
                    generatorSlider = new Slider { Minimum = 1, Maximum = 255, SmallChange = 1, LargeChange = 5, TickFrequency = 1, IsSnapToTickEnabled = true, IsEnabled = current.Editable, Margin = new(0, 6, 0, 0) };
                    generatorSlider.ValueChanged += EffectGeneratorSlider_Changed; cell.Children.Add(generatorSlider);
                    cell.Children.Add(FrequencyScaleCaptions.Create());
                }
                else
                {
                    var choices = EffectCatalogue.Choices.Select(choice => new DefinitionChoice(choice.Value, $"#{int.Parse(choice.Value, CultureInfo.InvariantCulture):X3} · " + choice.Name(Localization.LocalizationService.CurrentLanguage))).ToList();
                    if (generatorForm.Inputs[field].Length > 0 && choices.All(choice => choice.ID != generatorForm.Inputs[field])) choices.Add(new(generatorForm.Inputs[field], generatorForm.Inputs[field] + " · " + GeneratorText("Unknown")));
                    var combo = new ComboBox { DisplayMemberPath = "Label", ItemsSource = choices, SelectedItem = choices.FirstOrDefault(choice => choice.ID == generatorForm.Inputs[field]) };
                    combo.ToolTip = (combo.SelectedItem as DefinitionChoice)?.Label; combo.SelectionChanged += EffectGeneratorChoice_Changed; control = combo;
                }
                control.Name = "EffectGenerator" + field; control.IsEnabled = current.Editable; RegisterName(control.Name, control); generatorControls.Add(field, control); cell.Children.Insert(1, control); EffectGeneratorFields.Children.Add(cell);
            }
            RefreshGeneratorSlider(); RefreshGeneratorHint();
        }
        finally { updatingGenerator = false; }
    }
    private void CaptureGenerator()
    {
        if (generatorForm is null) return;
        generatorForm = generatorForm with { Inputs = generatorControls.ToDictionary(item => item.Key, item => item.Value is TextBox text ? text.Text : (item.Value as ComboBox)?.SelectedItem is DefinitionChoice choice ? choice.ID : generatorForm.Inputs[item.Key]) };
        foreach (var combo in generatorControls.Values.OfType<ComboBox>()) combo.ToolTip = (combo.SelectedItem as DefinitionChoice)?.Label;
    }
    private void RefreshGeneratorSlider()
    {
        if (generatorSlider is null || generatorForm is null) return;
        var wasUpdating = updatingGenerator; updatingGenerator = true;
        try { generatorSlider.Value = int.TryParse(generatorForm.Inputs["Frequency"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var frequency) ? Math.Clamp(frequency,1,255) : 1; }
        finally { updatingGenerator = wasUpdating; }
    }
    private void RefreshGeneratorHint()
    {
        if (generatorForm is null) return;
        var changed = generatorForm.Inputs.Any(input => input.Value != generatorForm.Baseline.Raw(input.Key));
        EffectGeneratorApply.IsEnabled = generatorForm.Baseline.Editable; EffectGeneratorRevert.IsEnabled = generatorForm.Baseline.Editable && changed;
        EffectGeneratorHint.Text = GeneratorText(generatorForm.Baseline.Editable ? "Hint" : "ReadOnly") + (changed ? " " + GeneratorText("Draft") : "");
    }
    private void EffectGeneratorChoice_Changed(object sender, SelectionChangedEventArgs e)
    { if (!updatingGenerator) { CaptureGenerator(); RefreshGeneratorHint(); EffectGeneratorStatus.Text = ""; EffectGeneratorStatus.ToolTip = null; } }
    private void EffectGeneratorInput_Changed(object sender, TextChangedEventArgs e)
    { if (!updatingGenerator) { CaptureGenerator(); RefreshGeneratorSlider(); RefreshGeneratorHint(); EffectGeneratorStatus.Text = ""; EffectGeneratorStatus.ToolTip = null; } }
    private void EffectGeneratorSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (!updatingGenerator && generatorControls.GetValueOrDefault("Frequency") is TextBox number) number.Text = ((int)Math.Round(e.NewValue)).ToString(CultureInfo.InvariantCulture); }
    private void EffectGeneratorEdit_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || generatorForm is null) return;
        try
        {
            var reverting = (sender as Button)?.Tag as string == "Revert";
            if (!reverting)
            {
                CaptureGenerator(); var prepared = document.UpdateEffectGenerator(generatorForm.Baseline, generatorForm.Inputs.Select(input => new EffectGeneratorInput(input.Key,input.Value)));
                if (!CommitFields()) return;
                if (!prepared.SameBaseline(document.CreateEffectGeneratorDraft(prepared.Record))) throw new InvalidOperationException(GeneratorText("Changed"));
                document.ApplyEffectGenerator(prepared);
            }
            generatorForm = null; RefreshSelectedRecord(); EffectGeneratorStatus.Text = GeneratorText(reverting ? "Reverted" : "Applied"); EffectGeneratorStatus.ToolTip = null;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        { EffectGeneratorStatus.Text = GeneratorText("Failed"); EffectGeneratorStatus.ToolTip = exception.Message; }
    }
}
