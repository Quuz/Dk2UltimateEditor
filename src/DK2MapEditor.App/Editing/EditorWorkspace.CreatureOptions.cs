using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record OptionForm(CreatureOptionsDraft Baseline, IReadOnlyDictionary<string, string> Inputs);
    private readonly Dictionary<string, OptionForm> creatureOptionForms = [];
    private readonly Dictionary<string, Control> creatureOptionControls = [];
    private readonly List<Control> creatureOptionStepControls = [];
    private readonly Dictionary<string,TextBlock> creatureResistanceLabels=[];
    private string? creatureOptionGroup;
    private static string OptionText(string key) => DefinitionText("Options." + key);
    private static string CreatureOptionLabel(string field) => field switch
    {
        "Res1" or "Res2" or "Res3" or "Res4" => string.Format(CultureInfo.InvariantCulture, OptionText("ResistanceType"), field[^1..]),
        "Res1Rate" or "Res2Rate" or "Res3Rate" or "Res4Rate" => string.Format(CultureInfo.InvariantCulture, OptionText("ResistanceRate"), field[3]),
        "Melee1" or "Melee2" => string.Format(CultureInfo.InvariantCulture, OptionText("MeleeSwipe"), field[^1..]),
        "Special1" or "Special2" => string.Format(CultureInfo.InvariantCulture, OptionText("SpecialSwipe"), field[^1..]),
        "Spell" => OptionText("SpellSwipe"), _ => FieldLabel(field)
    };
    private void ClearCreatureOptionControls()
    {
        foreach (var control in creatureOptionControls.Values) UnregisterName(control.Name);
        foreach(var control in creatureOptionStepControls)UnregisterName(control.Name);
        creatureOptionStepControls.Clear();
        foreach(var label in creatureResistanceLabels.Values)UnregisterName(label.Name);
        creatureResistanceLabels.Clear();
        creatureOptionControls.Clear(); DefinitionOptionFields.Children.Clear();
    }
    private void RefreshCreatureOptions()
    {
        if (DefinitionOptionPanel is null) return;
        var row = (DefinitionPicker.SelectedItem as DefinitionView)?.Row;
        var group = (DefinitionSection.SelectedItem as DefinitionChoice)?.ID;
        var visible = row?.Family == "Creatures" && group is not null && CreatureOptions.Groups.Contains(group);
        DefinitionOptionPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ClearCreatureOptionControls(); creatureOptionGroup = visible ? group : null;
        if (!visible)
        {
            if (document is null) creatureOptionForms.Clear();
            DefinitionOptionApply.IsEnabled = DefinitionOptionRevert.IsEnabled = false; DefinitionOptionHint.Text = ""; return;
        }
        var current = document!.CreateCreatureOptionsDraft(row!, group!);
        if (!creatureOptionForms.TryGetValue(group!, out var form) || !form.Baseline.SameBaseline(current))
            creatureOptionForms[group!] = form = new(current, CreatureOptions.Fields(group!).ToDictionary(field => field, current.Display));
        foreach (var field in CreatureOptions.Fields(group!))
        {
            var cell = new StackPanel { Width = 330, Margin = new(0, 0, 12, 8) };
            cell.Children.Add(new TextBlock { Text = CreatureOptionLabel(field), TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 4) });
            var choices = CreatureOptions.Choices(field);
            if (field == "CameraRolls") choices = [new("0", DefinitionText("No"), DefinitionText("No")), new("1", DefinitionText("Yes"), DefinitionText("Yes"))];
            Control control;
            if (choices.Count > 0)
            {
                var options = choices.Select(choice => new DefinitionChoice(choice.Value, (field == "CameraRolls" ? "" : $"#{uint.Parse(choice.Value, CultureInfo.InvariantCulture):X2} · ") + choice.Name(Localization.LocalizationService.CurrentLanguage))).ToList();
                if (form.Inputs[field].Length > 0 && options.All(choice => choice.ID != form.Inputs[field])) options.Add(new(form.Inputs[field], form.Inputs[field] + " · " + OptionText("Unknown")));
                var combo = new ComboBox { DisplayMemberPath = "Label", ItemsSource = options, SelectedItem = options.FirstOrDefault(choice => choice.ID == form.Inputs[field]) };
                combo.SelectionChanged += DefinitionOptionChoice_Changed; control = combo;
            }
            else if(group=="Resistances"&&CreatureResistanceInputs.IsRate(field))
            {
                var slider=new Slider{Minimum=0,Maximum=100,SmallChange=1,LargeChange=20,TickFrequency=5,
                    TickPlacement=System.Windows.Controls.Primitives.TickPlacement.BottomRight,
                    Value=CreatureResistanceInputs.Position(form.Inputs[field]),MinWidth=280};
                bool Registered()=>creatureOptionControls.TryGetValue(field,out var current)&&ReferenceEquals(current,slider);
                slider.ValueChanged+=(_,_)=>{if(Registered())SetCreatureResistanceRate(field,slider.Value);};
                slider.PreviewKeyDown+=(_,e)=>
                {
                    if(!Registered()||System.Windows.Input.Keyboard.Modifiers!=System.Windows.Input.ModifierKeys.None)return;
                    var delta=e.Key switch
                    {System.Windows.Input.Key.Left or System.Windows.Input.Key.Up=>-1,
                     System.Windows.Input.Key.Right or System.Windows.Input.Key.Down=>1,
                     System.Windows.Input.Key.PageUp=>-20,System.Windows.Input.Key.PageDown=>20,_=>0};
                    if(delta!=0&&ScrollCreatureResistance(field,delta))e.Handled=true;
                    else if(e.Key is System.Windows.Input.Key.Home or System.Windows.Input.Key.End
                        &&SetCreatureResistanceRate(field,e.Key==System.Windows.Input.Key.Home?0:100))e.Handled=true;
                };
                slider.AddHandler(System.Windows.Input.Mouse.MouseUpEvent,new System.Windows.Input.MouseButtonEventHandler((_,e)=>
                {if(Registered()&&e.ChangedButton==System.Windows.Input.MouseButton.Left)SetCreatureResistanceRate(field,slider.Value);}),true);
                control=slider;
            }
            else
            {
                var text = new TextBox { Text = form.Inputs[field] }; text.TextChanged += DefinitionOptionInput_Changed; control = text;
            }
            control.Name = "DefinitionOption" + field; RegisterName(control.Name, control); creatureOptionControls.Add(field, control); cell.Children.Add(control);
            if(control is Slider)
            {
                var label=new TextBlock{Name="DefinitionOption"+field+"Value",Text=form.Inputs[field],Margin=new(0,4,0,0)};
                RegisterName(label.Name,label);creatureResistanceLabels.Add(field,label);cell.Children.Add(label);
            }
            if(control is TextBox number&&CreatureAuxiliaryInputs.Handles(group!,field))
            {
                var steps=new WrapPanel();
                foreach(var direction in new[]{-1,1})
                {
                    var button=new System.Windows.Controls.Primitives.RepeatButton
                    {Content=direction<0?"−":"+",MinWidth=32,Delay=400,Interval=75,
                        Name="DefinitionOption"+field+(direction<0?"Decrease":"Increase"),
                        ToolTip=Localization.LocalizationService.Get("CreaturePages."+(direction<0?"Decrease":"Increase"))};
                    button.Click+=(_,_)=>StepCreaturePossession(field,direction);RegisterName(button.Name,button);creatureOptionStepControls.Add(button);steps.Children.Add(button);
                }
                number.PreviewKeyDown+=(_,e)=>
                {
                    if(System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.None
                        &&e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down
                        &&StepCreaturePossession(field,e.Key==System.Windows.Input.Key.Up?1:-1))e.Handled=true;
                };
                cell.Children.Add(steps);
            }
            var format = CreatureOptions.Format(field, current.Native);
            if (format.Minimum is { } min && format.Maximum is { } max)
            {
                var hint = new TextBlock { Text = min.ToString(CultureInfo.InvariantCulture) + " … " + max.ToString(CultureInfo.InvariantCulture) + (format.Percent ? " %" : ""), TextWrapping = TextWrapping.Wrap, Margin = new(0, 4, 0, 0) };
                hint.SetResourceReference(StyleProperty, "MutedText"); cell.Children.Add(hint);
            }
            DefinitionOptionFields.Children.Add(cell);
        }
        RefreshCreatureOptionEnabled();
    }
    private void CaptureCreatureOptions()
    {
        if (creatureOptionGroup is null || !creatureOptionForms.TryGetValue(creatureOptionGroup, out var form)) return;
        creatureOptionForms[creatureOptionGroup] = form with { Inputs = creatureOptionControls.ToDictionary(item => item.Key, item => item.Value is TextBox text ? text.Text : (item.Value as ComboBox)?.SelectedItem is DefinitionChoice choice ? choice.ID : form.Inputs[item.Key]) };
    }
    private void RefreshCreatureOptionEnabled()
    {
        if (creatureOptionGroup is null || !creatureOptionForms.TryGetValue(creatureOptionGroup, out var form)) return;
        foreach (var (field, control) in creatureOptionControls)
            control.IsEnabled = form.Baseline.Editable && (!field.StartsWith("Res", StringComparison.Ordinal) || !field.EndsWith("Rate", StringComparison.Ordinal) || form.Inputs[field[..^4]] != "0");
        foreach(var control in creatureOptionStepControls)control.IsEnabled=form.Baseline.Editable;
        foreach(var (field,label) in creatureResistanceLabels)label.IsEnabled=creatureOptionControls[field].IsEnabled;
        DefinitionOptionApply.IsEnabled = form.Baseline.Editable;
        var changed = form.Inputs.Any(item => item.Value != form.Baseline.Display(item.Key));
        DefinitionOptionRevert.IsEnabled = form.Baseline.Editable && changed;
        DefinitionOptionHint.Text = OptionText(!form.Baseline.Editable ? "ReadOnly" : creatureOptionGroup == "Resistances" ? "ResHint" : "PossHint") + (changed ? " " + OptionText("Draft") : "");
    }
    private void DefinitionOptionChoice_Changed(object sender, SelectionChangedEventArgs e)
    {
        if(updatingDefinitions||CreatureHostedOwnerCurrent?.Invoke()==false)return;
        if(sender is ComboBox combo&&creatureOptionGroup=="Resistances")
        {
            var field=creatureOptionControls.FirstOrDefault(pair=>ReferenceEquals(pair.Value,combo)).Key;
            if(field is null||!ResistanceFormCurrent(field,combo))return;
        }
        InvalidateDefinitionEditing();CaptureCreatureOptions();RefreshCreatureOptionEnabled();
    }
    private bool ResistanceFormCurrent(string field,Control control)
    {
        if(updatingDefinitions||document is null||CreatureHostedOwnerCurrent?.Invoke()==false||creatureOptionGroup!="Resistances"
            ||!creatureOptionForms.TryGetValue("Resistances",out var form)||!form.Baseline.Editable
            ||!creatureOptionControls.TryGetValue(field,out var registered)||!ReferenceEquals(registered,control)||!control.IsEnabled
            ||DefinitionPicker.SelectedItem is not DefinitionView view||view.Row.Family!="Creatures"||view.Row.Index!=form.Baseline.Index)return false;
        try{return form.Baseline.SameBaseline(document.CreateCreatureOptionsDraft(view.Row,"Resistances"));}
        catch(Exception error)when(error is ArgumentException or InvalidDataException or InvalidOperationException){return false;}
    }
    public bool SetCreatureResistanceRate(string field,double position)
    {
        if(!CreatureResistanceInputs.IsRate(field)||!double.IsFinite(position)||!creatureOptionControls.TryGetValue(field,out var control)
            ||control is not Slider slider||!ResistanceFormCurrent(field,slider))return false;
        var form=creatureOptionForms["Resistances"];
        if(form.Inputs[CreatureResistanceInputs.TypeField(field)]=="0")return false;
        var value=(int)Math.Clamp(Math.Round(position,MidpointRounding.AwayFromZero),0d,100d);
        var text=value.ToString(CultureInfo.InvariantCulture);
        updatingDefinitions=true;
        try{slider.Value=value;}
        finally{updatingDefinitions=false;}
        if(form.Inputs[field]==text)return true;
        var inputs=form.Inputs.ToDictionary(pair=>pair.Key,pair=>pair.Key==field?text:pair.Value);
        creatureOptionForms["Resistances"]=form with{Inputs=inputs};creatureResistanceLabels[field].Text=text;
        InvalidateDefinitionEditing();RefreshCreatureOptionEnabled();return true;
    }
    public bool ScrollCreatureResistance(string field,int delta)
    {
        if(!CreatureResistanceInputs.IsRate(field)||delta is not (-20 or -1 or 1 or 20)
            ||!creatureOptionForms.TryGetValue("Resistances",out var form))return false;
        return SetCreatureResistanceRate(field,CreatureResistanceInputs.Position(CreatureResistanceInputs.Scroll(form.Inputs[field],delta)));
    }
    private bool PossessionInputCurrent(string field,TextBox input)
    {
        if(updatingDefinitions||document is null||CreatureHostedOwnerCurrent?.Invoke()==false||creatureOptionGroup!="Possession"
            ||!CreatureAuxiliaryInputs.Handles("Possession",field)||!creatureOptionForms.TryGetValue("Possession",out var form)||!form.Baseline.Editable
            ||!creatureOptionControls.TryGetValue(field,out var control)||!ReferenceEquals(control,input)||!input.IsEnabled
            ||DefinitionPicker.SelectedItem is not DefinitionView view||view.Row.Family!="Creatures"||view.Row.Index!=form.Baseline.Index)return false;
        try{return form.Baseline.SameBaseline(document.CreateCreatureOptionsDraft(view.Row,"Possession"));}
        catch(Exception error)when(error is ArgumentException or InvalidDataException or InvalidOperationException){return false;}
    }
    private void DefinitionOptionInput_Changed(object sender,TextChangedEventArgs e)
    {
        if(updatingDefinitions)return;
        if(sender is TextBox input)
        {
            var field=creatureOptionControls.FirstOrDefault(pair=>ReferenceEquals(pair.Value,input)).Key;
            if(field is not null&&PossessionInputCurrent(field,input))
            {
                var normalized=CreatureAuxiliaryInputs.Normalize("Possession",field,input.Text);
                if(normalized!=input.Text)
                {
                    updatingDefinitions=true;
                    try{input.Text=normalized;input.CaretIndex=normalized.Length;}
                    finally{updatingDefinitions=false;}
                }
            }
        }
        InvalidateDefinitionEditing();CaptureCreatureOptions();RefreshCreatureOptionEnabled();
    }
    public bool StepCreaturePossession(string field,int direction)
    {
        if(direction is not (-1 or 1)||!creatureOptionControls.TryGetValue(field,out var control)||control is not TextBox input||!PossessionInputCurrent(field,input))return false;
        input.Text=CreatureAuxiliaryInputs.Step("Possession",field,input.Text,direction,!document!.IsLegacyProject);input.CaretIndex=input.Text.Length;return true;
    }
    private void DefinitionOptionEdit_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || creatureOptionGroup is null || DefinitionPicker.SelectedItem is not DefinitionView view || !creatureOptionForms.TryGetValue(creatureOptionGroup, out var form)) return;
        try
        {
            if ((sender as Button)?.Tag as string != "Revert")
            {
                CaptureCreatureOptions(); form = creatureOptionForms[creatureOptionGroup];
                var prepared = document.UpdateCreatureOptions(form.Baseline, form.Inputs.Select(item => new CreatureOptionInput(item.Key, item.Value)));
                if (!CommitFields()) return;
                if (!prepared.SameBaseline(document.CreateCreatureOptionsDraft(view.Row, creatureOptionGroup))) { DefinitionMessage("Failed", OptionText("Changed")); return; }
                document.ApplyCreatureOptions(prepared);
            }
            creatureOptionForms.Remove(form.Baseline.Group); InvalidateDefinitionEditing(); RefreshDefinitions(); DefinitionMessage("Applied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        { DefinitionMessage("Failed", exception.Message); }
    }
}
