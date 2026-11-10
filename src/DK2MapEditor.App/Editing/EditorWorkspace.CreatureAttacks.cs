using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record AttackForm(string Type, IReadOnlyDictionary<string,string> Inputs);
    private CreatureAttacksDraft? attackState;
    private readonly Dictionary<string,AttackForm> attackForms = [];
    private readonly Dictionary<string,Control> attackParameterControls = [];
    private readonly List<Control> attackStepControls = [];
    private string attackSlot = "Shot1";
    private static string AttackText(string key) => DefinitionText("Attack." + key);
    private static string AttackParameterKey(string field) => CreatureAttacks.IsLevel(field) ? "Level" : field is "RechargeTime" or "Range" or "Dammage" or "Delay" ? field : field.EndsWith("Delay",StringComparison.Ordinal) ? "CastDelay" : field[^1..];
    private void ClearAttackControls()
    {
        foreach (var control in attackParameterControls.Values) UnregisterName(control.Name);
        foreach(var control in attackStepControls)UnregisterName(control.Name);
        attackStepControls.Clear();
        attackParameterControls.Clear(); DefinitionAttackParameters.Children.Clear();
    }
    private void RefreshCreatureAttacks()
    {
        if (DefinitionAttackPanel is null) return;
        var row = (DefinitionPicker.SelectedItem as DefinitionView)?.Row;
        var visible = row?.Family == "Creatures" && (DefinitionSection.SelectedItem as DefinitionChoice)?.ID == "Attacks";
        DefinitionAttackPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible)
        {
            if (document is null) { attackState = null; attackForms.Clear(); }
            ClearAttackControls(); DefinitionAttackSlot.ItemsSource = DefinitionAttackType.ItemsSource = null;
            DefinitionAttackApply.IsEnabled = DefinitionAttackRevert.IsEnabled = false; DefinitionAttackHint.Text = ""; return;
        }
        var current = document!.CreateCreatureAttacksDraft(row!);
        if (attackState?.SameBaseline(current) != true)
        {
            attackState = current; attackForms.Clear();
            foreach (var slot in CreatureAttacks.Slots) attackForms.Add(slot,new(current.Raw(CreatureAttacks.TypeField(slot)),CreatureAttacks.Parameters(slot).ToDictionary(field => field,field => current.Display(field))));
        }
        var selected = (DefinitionAttackSlot.SelectedItem as DefinitionChoice)?.ID ?? attackSlot;
        DefinitionAttackSlot.ItemsSource = CreatureAttacks.Slots.Select(slot => new DefinitionChoice(slot,AttackText(slot))).ToArray();
        DefinitionAttackSlot.SelectedItem = DefinitionAttackSlot.Items.Cast<DefinitionChoice>().Single(item => item.ID == selected); attackSlot = selected;
        LoadAttackForm();
    }
    private void LoadAttackForm()
    {
        if (attackState is null) return; var form = attackForms[attackSlot];
        ClearAttackControls();
        var types = CreatureAttacks.Options(attackSlot).Select(option => new DefinitionChoice(option.Name.Value,$"#{option.ID:X2} · " + option.Name.Name(Localization.LocalizationService.CurrentLanguage))).ToList();
        if (form.Type.Length > 0 && types.All(choice => choice.ID != form.Type)) types.Add(new(form.Type,"#" + form.Type + " · " + AttackText("Unknown")));
        DefinitionAttackType.ItemsSource = types; DefinitionAttackType.SelectedItem = types.FirstOrDefault(choice => choice.ID == form.Type);
        foreach (var field in CreatureAttacks.Parameters(attackSlot))
        {
            var container = new StackPanel { Width = 180, Margin = new(0,0,12,0) };
            container.Children.Add(new TextBlock { Text = AttackText(AttackParameterKey(field)), TextWrapping = TextWrapping.Wrap, Margin = new(0,0,0,4) });
            Control control;
            if (CreatureAttacks.IsLevel(field))
            {
                var choices = Enumerable.Range(0,11).Select(level => new DefinitionChoice(level.ToString(CultureInfo.InvariantCulture),level.ToString(CultureInfo.InvariantCulture))).ToList();
                if (form.Inputs[field].Length > 0 && choices.All(item => item.ID != form.Inputs[field])) choices.Add(new(form.Inputs[field],form.Inputs[field] + " · " + AttackText("Unknown")));
                var combo = new ComboBox { DisplayMemberPath = "Label", ItemsSource = choices, SelectedItem = choices.FirstOrDefault(item => item.ID == form.Inputs[field]) };
                combo.SelectionChanged += DefinitionAttackLevel_Changed; control = combo;
            }
            else
            {
                var text = new TextBox { Text = form.Inputs[field] }; text.TextChanged += DefinitionAttackInput_Changed; control = text;
            }
            control.Name = "DefinitionAttack" + field; RegisterName(control.Name,control); attackParameterControls.Add(field,control); container.Children.Add(control);
            if(control is TextBox number)
            {
                var steps=new WrapPanel();
                foreach(var direction in new[]{-1,1})
                {
                    var button=new System.Windows.Controls.Primitives.RepeatButton
                    {Content=direction<0?"−":"+",MinWidth=32,Delay=400,Interval=75,
                        Name="DefinitionAttack"+field+(direction<0?"Decrease":"Increase"),
                        ToolTip=Localization.LocalizationService.Get("CreaturePages."+(direction<0?"Decrease":"Increase"))};
                    button.Click+=(_,_)=>StepCreatureAttack(field,direction);RegisterName(button.Name,button);attackStepControls.Add(button);steps.Children.Add(button);
                }
                number.PreviewKeyDown+=(_,e)=>
                {
                    if(System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.None
                        &&e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down
                        &&StepCreatureAttack(field,e.Key==System.Windows.Input.Key.Up?1:-1))e.Handled=true;
                };
                container.Children.Add(steps);
            }
            var format = CreatureAttacks.Format(field,attackState.Native);
            var range = format.Minimum?.ToString(CultureInfo.InvariantCulture) + " … " + format.Maximum?.ToString(CultureInfo.InvariantCulture);
            var hint = new TextBlock { Text = range, TextWrapping = TextWrapping.Wrap, Margin = new(0,4,0,0) }; hint.SetResourceReference(StyleProperty,"MutedText"); container.Children.Add(hint);
            DefinitionAttackParameters.Children.Add(container);
        }
        DefinitionAttackSlot.IsEnabled = DefinitionAttackType.IsEnabled = DefinitionAttackApply.IsEnabled = attackState.Editable;
        RefreshAttackControls();
    }
    private void CaptureAttackForm()
    {
        if (attackState is null || !attackForms.TryGetValue(attackSlot,out var current)) return;
        var inputs = current.Inputs.ToDictionary(item => item.Key,item => item.Value);
        foreach (var (field,control) in attackParameterControls)
            inputs[field] = control is TextBox text ? text.Text : (control as ComboBox)?.SelectedItem is DefinitionChoice choice ? choice.ID : current.Inputs[field];
        attackForms[attackSlot] = new((DefinitionAttackType.SelectedItem as DefinitionChoice)?.ID ?? current.Type,inputs);
    }
    private bool AttackDraftChanged() => attackState is not null && attackForms.Any(form => form.Value.Type != attackState.Raw(CreatureAttacks.TypeField(form.Key)) || form.Value.Inputs.Any(input => input.Value != attackState.Display(input.Key)));
    private void RefreshAttackControls()
    {
        var editable = attackState?.Editable == true; var active = editable && (attackSlot == "Melee" || attackForms[attackSlot].Type != "0");
        foreach (var control in attackParameterControls.Values) control.IsEnabled = active;
        foreach(var control in attackStepControls)control.IsEnabled=active;
        DefinitionAttackRevert.IsEnabled = editable && AttackDraftChanged();
        DefinitionAttackHint.Text = AttackText(!editable ? "ReadOnly" : !active ? "NoneHint" : "Hint") + (AttackDraftChanged() ? " " + AttackText("Draft") : "");
    }
    private void DefinitionAttackSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingDefinitions || attackState is null) return; InvalidateDefinitionEditing(); updatingDefinitions = true;
        try
        {
            if (sender == DefinitionAttackSlot && DefinitionAttackSlot.SelectedItem is DefinitionChoice slot) { attackSlot = slot.ID; LoadAttackForm(); }
            else { CaptureAttackForm(); RefreshAttackControls(); }
        }
        finally { updatingDefinitions = false; }
    }
    private bool AttackInputCurrent(string field,TextBox input)
    {
        if(updatingDefinitions||document is null||attackState?.Editable!=true||CreatureHostedOwnerCurrent?.Invoke()==false
            ||!CreatureAttackInputs.Handles(field)||!attackParameterControls.TryGetValue(field,out var control)||!ReferenceEquals(control,input)||!input.IsEnabled
            ||DefinitionPicker.SelectedItem is not DefinitionView view||view.Row.Family!="Creatures"||view.Row.Index!=attackState.Index
            ||(attackSlot!="Melee"&&attackForms[attackSlot].Type=="0"))return false;
        try{return attackState.SameBaseline(document.CreateCreatureAttacksDraft(view.Row));}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException){return false;}
    }
    private void DefinitionAttackInput_Changed(object sender, TextChangedEventArgs e)
    {
        if(updatingDefinitions)return;
        if(sender is TextBox input)
        {
            var field=attackParameterControls.FirstOrDefault(pair=>ReferenceEquals(pair.Value,input)).Key;
            if(field is not null&&AttackInputCurrent(field,input))
            {
                var normalized=CreatureAttackInputs.Normalize(field,input.Text);
                if(normalized!=input.Text)
                {
                    updatingDefinitions=true;
                    try{input.Text=normalized;input.CaretIndex=normalized.Length;}
                    finally{updatingDefinitions=false;}
                }
            }
        }
        InvalidateDefinitionEditing();CaptureAttackForm();RefreshAttackControls();
    }
    public bool StepCreatureAttack(string field,int direction)
    {
        if(direction is not (-1 or 1)||!attackParameterControls.TryGetValue(field,out var control)||control is not TextBox input||!AttackInputCurrent(field,input))return false;
        input.Text=CreatureAttackInputs.Step(field,input.Text,direction);input.CaretIndex=input.Text.Length;return true;
    }
    private void DefinitionAttackLevel_Changed(object sender, SelectionChangedEventArgs e)
    { if (!updatingDefinitions) { InvalidateDefinitionEditing(); CaptureAttackForm(); RefreshAttackControls(); } }
    private void DefinitionAttackEdit_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || attackState is null || DefinitionPicker.SelectedItem is not DefinitionView view) return;
        try
        {
            if ((sender as Button)?.Tag as string != "Revert")
            {
                CaptureAttackForm(); var prepared = attackState;
                foreach (var slot in CreatureAttacks.Slots)
                {
                    var form = attackForms[slot]; prepared = document.UpdateCreatureAttack(prepared,slot,form.Type,form.Inputs.Select(input => new CreatureAttackInput(input.Key,input.Value)));
                }
                if (!CommitFields()) return;
                if (!prepared.SameBaseline(document.CreateCreatureAttacksDraft(view.Row))) { DefinitionMessage("Failed",AttackText("Changed")); return; }
                document.ApplyCreatureAttacks(prepared);
            }
            attackState = null; attackForms.Clear(); InvalidateDefinitionEditing(); RefreshDefinitions(); DefinitionMessage("Applied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        { DefinitionMessage("Failed",exception.Message); }
    }
}
