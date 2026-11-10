using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Original keeper-spell page6824 and two private submitted projectile slots.</summary>
public sealed class GlobalSpellWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly GlobalDefinitionRow row;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly StackPanel fields = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(4) };
    private readonly Button apply = new();
    private readonly Dictionary<string, string> inputs = [];
    private readonly List<string> registered = [];
    private GlobalSpellDraft working;
    private GlobalSpellOptionsWindow? optionsWindow;
    private bool rebuilding, closed, stale, applying, choosing;
    private string? statusKey;
    private sealed record Choice(string ID, string Label);
    public Action<GlobalSpellOptionsWindow>? OptionsPresenter { get; set; }
    public long InputRevision { get; private set; }
    public bool HasPendingInputs => working.Changed || inputs.Any(item => item.Value != working.Display(item.Key));
    public GlobalSpellWindow(NativeMapDocument document, GlobalDefinitionRow row, Func<bool> ownerCurrent, Func<bool> commitFields)
    {
        this.document = document; this.row = row; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields;
        working = document.CreateGlobalSpellDraft(row); LoadInputs();
        NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 740; Height = 660; MinWidth = 540; MinHeight = 420; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel; panel.SetResourceReference(Panel.BackgroundProperty, "Brush.Background");
        var hint = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4, 0, 4, 8) };
        hint.SetResourceReference(TextBlock.TextProperty, "SpellProperties.Hint"); DockPanel.SetDock(hint, Dock.Top); panel.Children.Add(hint);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status); RegisterName("SpellPropertiesStatus", status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        apply.SetResourceReference(ContentControl.ContentProperty, "SpellProperties.Apply"); apply.Click += (_, _) => ApplySpell(); RegisterName("SpellPropertiesApply", apply); buttons.Children.Add(apply);
        var cancel = new Button { IsCancel = true }; cancel.SetResourceReference(ContentControl.ContentProperty, "SpellProperties.Cancel"); cancel.Click += (_, _) => Close(); RegisterName("SpellPropertiesCancel", cancel); buttons.Children.Add(cancel); footer.Children.Add(buttons);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = fields }; RegisterName("SpellPropertiesScroll", scroll); panel.Children.Add(scroll);
        document.Changed += DocumentChanged; LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) => { closed = true; InputRevision++; optionsWindow?.Close(); optionsWindow = null; document.Changed -= DocumentChanged; LocalizationService.LanguageChanged -= LanguageChanged; };
        Rebuild(); UpdateStatus();
    }
    internal static string Text(string key) => LocalizationService.Get("SpellProperties." + key);
    private bool Current => !closed && !stale && ownerCurrent();
    private void LoadInputs() { foreach (var field in GlobalSpellControls.MainFields) inputs[field] = working.Display(field); }
    private void Register(Control control, string field)
    { var name = "SpellProperty" + field; control.Name = name; RegisterName(name, control); registered.Add(name); }
    private void Rebuild()
    {
        rebuilding = true;
        try
        {
            Title = Text("Title") + " · " + ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue("Spells")[row.Index], row.ID.ToString(CultureInfo.InvariantCulture)).Name(LocalizationService.CurrentLanguage);
            foreach (var name in registered) UnregisterName(name); registered.Clear(); fields.Children.Clear();
            foreach (var field in GlobalSpellControls.MainFields)
            {
                var enabled = working.Editable;
                var cell = new StackPanel { Margin = new(4, 2, 4, 8) };
                cell.Children.Add(new TextBlock { Text = LocalizationService.Get("Field." + field), TextWrapping = TextWrapping.Wrap, IsEnabled = enabled });
                if (GlobalSpellControls.Named(field))
                {
                    var choices = GlobalSpellControls.Choices(field).Select(choice => new Choice(choice.Value, choice.Name(LocalizationService.CurrentLanguage))).ToList();
                    if (!choices.Any(choice => choice.ID == inputs[field])) choices.Insert(0, new(inputs[field], Text("Imported") + " · " + inputs[field]));
                    var combo = new ComboBox { DisplayMemberPath = "Label", ItemsSource = choices, SelectedItem = choices.First(choice => choice.ID == inputs[field]), IsEnabled = enabled };
                    combo.SelectionChanged += (_, _) => { if (!rebuilding && combo.SelectedItem is Choice choice) Changed(field, choice.ID); }; Register(combo, field); cell.Children.Add(combo);
                    if (field is "ShotType" or "UpgShotType")
                    {
                        var button = new Button { Content = Text(field == "ShotType" ? "PrimaryOptions" : "UpgradeOptions"), HorizontalAlignment = HorizontalAlignment.Left, IsEnabled = enabled };
                        button.Click += (_, _) => OpenOptions(field == "ShotType" ? "Shot" : "Upgrade"); Register(button, field + "Options"); cell.Children.Add(button);
                        cell.Children.Add(new TextBlock { Text = working.Display(field == "ShotType" ? "ShotData1" : "UpgShotData1") + " / " + working.Display(field == "ShotType" ? "ShotData2" : "UpgShotData2"), TextWrapping = TextWrapping.Wrap });
                    }
                }
                else if (GlobalSpellControls.Boolean(field))
                {
                    var input = new CheckBox { IsEnabled = enabled, IsThreeState = inputs[field] is not ("0" or "1"), IsChecked = inputs[field] == "1" ? true : inputs[field] == "0" ? false : null };
                    void CheckChanged(object sender, RoutedEventArgs args) { if (!rebuilding && input.IsChecked is bool value) Changed(field, value ? "1" : "0"); }
                    input.Checked += CheckChanged; input.Unchecked += CheckChanged; Register(input, field); cell.Children.Add(input);
                    if (inputs[field] is not ("0" or "1")) cell.Children.Add(new TextBlock { Text = Text("Imported") + " · " + inputs[field], TextWrapping = TextWrapping.Wrap });
                }
                else
                {
                    var input = new TextBox { Text = inputs[field], IsEnabled = enabled };
                    input.TextChanged += (_, _) => NumericChanged(field,input); Register(input, field); cell.Children.Add(input);
                    AddNumberSteps(field,input,cell);
                }
                fields.Children.Add(cell);
            }
            fields.IsEnabled = Current && !choosing;
        }
        finally { rebuilding = false; }
    }

    private bool NumberCurrent(string field,TextBox input)=>Current&&working.Editable&&!applying&&!choosing&&GlobalElementInputs.Handles("Spells",field)&&true&&input.IsEnabled&&ReferenceEquals(FindName("SpellProperty"+field),input);
    private void NumericChanged(string field,TextBox input)
    {
        if(rebuilding)return;
        var text=NumberCurrent(field,input)?GlobalElementInputs.Normalize("Spells",field,input.Text):input.Text;
        if(text!=input.Text)
        {
            rebuilding=true;
            try{input.Text=text;input.CaretIndex=text.Length;}
            finally{rebuilding=false;}
        }
        Changed(field,text);
    }
    private void AddNumberSteps(string field,TextBox input,StackPanel cell)
    {
        if(!GlobalElementInputs.Handles("Spells",field))return;
        var steps=new WrapPanel();
        foreach(var direction in new[]{-1,1})
        {
            var button=new System.Windows.Controls.Primitives.RepeatButton
            {Content=direction<0?"−":"+",MinWidth=32,Delay=400,Interval=75,IsEnabled=input.IsEnabled,
                ToolTip=LocalizationService.Get("CreaturePages."+(direction<0?"Decrease":"Increase"))};
            button.Click+=(_,_)=>StepNumber(field,direction);Register(button,field+(direction<0?"Decrease":"Increase"));steps.Children.Add(button);
        }
        input.PreviewKeyDown+=(_,e)=>
        {
            if(System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.None
                &&e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down
                &&StepNumber(field,e.Key==System.Windows.Input.Key.Up?1:-1))e.Handled=true;
        };
        cell.Children.Add(steps);
    }
    public bool StepNumber(string field,int direction)
    {
        if(direction is not (-1 or 1)||!GlobalElementInputs.Handles("Spells",field)||FindName("SpellProperty"+field) is not TextBox input||!NumberCurrent(field,input))return false;
        input.Text=GlobalElementInputs.Step("Spells",field,input.Text,direction);input.CaretIndex=input.Text.Length;return true;
    }

    private void Changed(string field, string value)
    { inputs[field] = value; InputRevision++; if (!stale) statusKey = null; UpdateStatus(); }

    public bool OpenOptions(string slot)
    {
        if (!Current || !working.Editable || choosing || applying) return false;
        try
        {
            var submitted = document.UpdateGlobalSpellDraft(working, inputs.Select(item => new GlobalSpellInput(item.Key, item.Value)));
            if (GlobalProjectiles.Find(submitted.Raw(GlobalSpellControls.SlotFields(slot).Type)) is not { Form: not 255 })
            { statusKey = "NoOptions"; UpdateStatus(); return false; }
            working = submitted; InputRevision++; var revision = InputRevision;
            choosing = true;
            var child = new GlobalSpellOptionsWindow(document, working, slot, () => Current && choosing && revision == InputRevision);
            optionsWindow = child;
            child.Accepted += (_, _) => { if (Current && revision == InputRevision && child.AcceptedDraft is { } result) { working = result; InputRevision++; } };
            child.Closed += (_, _) => { if (ReferenceEquals(optionsWindow, child)) { choosing = false; optionsWindow = null; if (!closed) { Rebuild(); UpdateStatus(); } } };
            Rebuild(); UpdateStatus();
            if (OptionsPresenter is { } present) present(child);
            else { child.Owner = this; child.ShowDialog(); }
            return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { statusKey = "Invalid"; UpdateStatus(); return false; }
    }
    private void UpdateStatus()
    { status.Text = Text(statusKey ?? (!working.Editable ? "ReadOnly" : choosing ? "Choosing" : HasPendingInputs ? "Draft" : "Ready")); apply.IsEnabled = Current && working.Editable && !choosing; }
    private void DocumentChanged(object? sender, EventArgs args)
    { if (!closed && !applying) { stale = true; statusKey = "Stale"; fields.IsEnabled = false; UpdateStatus(); } }
    private void LanguageChanged(object? sender, EventArgs args)
    { if (!closed) { Rebuild(); UpdateStatus(); } }
    public bool ApplySpell()
    {
        if (!Current || !working.Editable || applying || choosing) return false;
        applying = true; var revision = InputRevision;
        try
        {
            var prepared = document.UpdateGlobalSpellDraft(working, inputs.Select(item => new GlobalSpellInput(item.Key, item.Value)));
            if (!commitFields() || !Current || revision != InputRevision) return false;
            if (!prepared.SameBaseline(document.CreateGlobalSpellDraft(row))) { stale = true; statusKey = "Stale"; UpdateStatus(); return false; }
            document.ApplyGlobalSpellDraft(prepared); working = document.CreateGlobalSpellDraft(row); LoadInputs();
            InputRevision++; Rebuild(); statusKey = "Applied"; UpdateStatus(); return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { statusKey = "Invalid"; UpdateStatus(); return false; }
        finally { applying = false; }
    }
}
