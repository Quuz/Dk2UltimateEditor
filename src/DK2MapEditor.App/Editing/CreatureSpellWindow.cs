using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Original creature-spell page6868 and one private submitted projectile slot.</summary>
public sealed class CreatureSpellWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly GlobalDefinitionRow row;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly StackPanel fields = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(4) };
    private readonly Button apply = new();
    private readonly Dictionary<string, string> inputs = [];
    private readonly List<string> registered = [];
    private CreatureSpellDraft working;
    private readonly CreatureSpellUsage usage;
    private int? selectedUserIndex;
    private CreatureSpellOptionsWindow? optionsWindow;
    private bool rebuilding, closed, stale, applying, choosing;
    private string? statusKey;
    private sealed record Choice(string ID, string Label);
    private sealed record UserChoice(int Index,uint ID,string Label)
    {public System.Windows.Media.Imaging.BitmapSource? Icon=>OriginalDefinitionImages.For("Creatures",Index);}
    public Action<CreatureSpellOptionsWindow>? OptionsPresenter { get; set; }
    public long InputRevision { get; private set; }
    public bool HasPendingInputs => working.Changed || inputs.Any(item => item.Value != working.Display(item.Key));
    public CreatureSpellWindow(NativeMapDocument document, GlobalDefinitionRow row, Func<bool> ownerCurrent, Func<bool> commitFields)
    {
        this.document = document; this.row = row; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields;
        working = document.CreateCreatureSpellDraft(row); LoadInputs();
        usage=document.ReadCreatureSpellUsage(row);
        NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 740; Height = 660; MinWidth = 540; MinHeight = 420; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel; panel.SetResourceReference(Panel.BackgroundProperty, "Brush.Background");
        var hint = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4, 0, 4, 8) };
        hint.SetResourceReference(TextBlock.TextProperty, "CreatureSpellProperties.Hint"); DockPanel.SetDock(hint, Dock.Top); panel.Children.Add(hint);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status); RegisterName("CreatureSpellPropertiesStatus", status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        apply.SetResourceReference(ContentControl.ContentProperty, "CreatureSpellProperties.Apply"); apply.Click += (_, _) => ApplySpell(); RegisterName("CreatureSpellPropertiesApply", apply); buttons.Children.Add(apply);
        var cancel = new Button { IsCancel = true }; cancel.SetResourceReference(ContentControl.ContentProperty, "CreatureSpellProperties.Cancel"); cancel.Click += (_, _) => Close(); RegisterName("CreatureSpellPropertiesCancel", cancel); buttons.Children.Add(cancel); footer.Children.Add(buttons);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = fields }; RegisterName("CreatureSpellPropertiesScroll", scroll); panel.Children.Add(scroll);
        document.Changed += DocumentChanged; LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) => { closed = true; InputRevision++; optionsWindow?.Close(); optionsWindow = null; document.Changed -= DocumentChanged; LocalizationService.LanguageChanged -= LanguageChanged; };
        Rebuild(); UpdateStatus();
    }
    internal static string Text(string key) => LocalizationService.Get("CreatureSpellProperties." + key);
    private bool Current => !closed && !stale && ownerCurrent();
    private void LoadInputs() { foreach (var field in CreatureSpellControls.MainFields) inputs[field] = working.Display(field); }
    private void Register(Control control, string field)
    { var name = "CreatureSpellProperty" + field; control.Name = name; RegisterName(name, control); registered.Add(name); }
    private void Rebuild()
    {
        rebuilding = true;
        try
        {
            Title = Text("Title") + " · " + ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue("CrSpells")[row.Index], row.ID.ToString(CultureInfo.InvariantCulture)).Name(LocalizationService.CurrentLanguage);
            foreach (var name in registered) UnregisterName(name); registered.Clear(); fields.Children.Clear();
            foreach (var field in CreatureSpellControls.MainFields)
            {
                var enabled = working.Editable;
                var cell = new StackPanel { Margin = new(4, 2, 4, 8) };
                cell.Children.Add(new TextBlock { Text = CreatureDefinitionFieldLabels.Label("CrSpells", field), TextWrapping = TextWrapping.Wrap, IsEnabled = enabled });
                if (CreatureSpellControls.Named(field))
                {
                    var choices = CreatureSpellControls.Choices(field).Select(choice => new Choice(choice.Value, choice.Name(LocalizationService.CurrentLanguage))).ToList();
                    if (!choices.Any(choice => choice.ID == inputs[field])) choices.Insert(0, new(inputs[field], Text("Imported") + " · " + inputs[field]));
                    var combo = new ComboBox { DisplayMemberPath = "Label", ItemsSource = choices, SelectedItem = choices.First(choice => choice.ID == inputs[field]), IsEnabled = enabled };
                    combo.SelectionChanged += (_, _) => { if (!rebuilding && combo.SelectedItem is Choice choice) Changed(field, choice.ID); }; Register(combo, field); cell.Children.Add(combo);
                    if (field == "Shot")
                    {
                        var button = new Button { Content = Text("PrimaryOptions"), HorizontalAlignment = HorizontalAlignment.Left, IsEnabled = enabled };
                        button.Click += (_, _) => OpenOptions("Shot"); Register(button, field + "Options"); cell.Children.Add(button);
                        cell.Children.Add(new TextBlock { Text = working.Display("ShotData1") + " / " + working.Display("ShotData2"), TextWrapping = TextWrapping.Wrap });
                    }
                }
                else if (CreatureSpellControls.Boolean(field))
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
                    input.PreviewKeyDown+=(_,e)=>
                    {
                        if(System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.None
                            &&e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down
                            &&StepNumber(field,e.Key==System.Windows.Input.Key.Up?1:-1))e.Handled=true;
                    };
                    var steps = new WrapPanel();
                    foreach (var direction in new[] { -1, 1 })
                    {
                        var button = new System.Windows.Controls.Primitives.RepeatButton { Content = direction < 0 ? "−" : "+", IsEnabled = enabled, MinWidth = 32, Delay=400, Interval=75, ToolTip = Text(direction < 0 ? "Decrease" : "Increase") };
                        button.Click += (_, _) => StepNumber(field, direction); Register(button, field + (direction < 0 ? "Decrease" : "Increase")); steps.Children.Add(button);
                    }
                    cell.Children.Add(steps);
                }
                fields.Children.Add(cell);
            }
            var usageLabel=new TextBlock{Text=Text("UsedBy"),TextWrapping=TextWrapping.Wrap,Margin=new(4,12,4,4)};
            fields.Children.Add(usageLabel);
            var users=usage.Users.Select(creature=>new UserChoice(creature.Index,creature.ID,
                ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue("Creatures")[creature.Index],creature.ID.ToString(CultureInfo.InvariantCulture)).Name(LocalizationService.CurrentLanguage))).ToArray();
            var list=new ListBox{ItemsSource=users,ItemTemplate=OriginalObjectImages.NamedTemplate(),Margin=new(4),MinHeight=40,MaxHeight=192};
            list.SelectedItem=users.FirstOrDefault(user=>user.Index==selectedUserIndex);
            list.SelectionChanged+=(_,_)=>
            {if(!rebuilding&&!closed&&ReferenceEquals(FindName("CreatureSpellPropertyUsers"),list))selectedUserIndex=(list.SelectedItem as UserChoice)?.Index;};
            Register(list,"Users");fields.Children.Add(list);
            if(!usage.Complete||users.Length==0)fields.Children.Add(new TextBlock
            {Text=Text(!usage.Complete?"UsageIncomplete":"NoUsers"),TextWrapping=TextWrapping.Wrap,Margin=new(4)});
            fields.IsEnabled = Current && !choosing;
        }
        finally { rebuilding = false; }
    }
    private void Changed(string field, string value)
    { inputs[field] = value; InputRevision++; if (!stale) statusKey = null; UpdateStatus(); }
    private void NumericChanged(string field,TextBox input)
    {
        if(rebuilding)return;
        var text=Current&&working.Editable&&!choosing&&!applying&&CreatureAuxiliaryInputs.Handles("CrSpells",field)
            ?CreatureAuxiliaryInputs.Normalize("CrSpells",field,input.Text):input.Text;
        if(text!=input.Text)
        {
            rebuilding=true;
            try{input.Text=text;input.CaretIndex=text.Length;}
            finally{rebuilding=false;}
        }
        Changed(field,text);
    }
    public bool StepNumber(string field, int direction)
    {
        if (!Current || !working.Editable || choosing || applying || !CreatureSpellControls.MainFields.Contains(field) || CreatureSpellControls.Named(field) || CreatureSpellControls.Boolean(field)) return false;
        try
        {
            if (direction is not (-1 or 1)) throw new ArgumentException("Invalid step.");
            if(FindName("CreatureSpellProperty"+field) is not TextBox input)return false;
            input.Text=CreatureAuxiliaryInputs.Step("CrSpells",field,inputs[field],direction,!document.IsLegacyProject);
            input.CaretIndex=input.Text.Length;return true;
        }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException) { statusKey = "Invalid"; UpdateStatus(); return false; }
    }
    public bool OpenOptions(string slot)
    {
        if (!Current || !working.Editable || choosing || applying) return false;
        try
        {
            var submitted = document.UpdateCreatureSpellDraft(working, inputs.Select(item => new CreatureSpellInput(item.Key, item.Value)));
            if (GlobalProjectiles.Find(submitted.Raw(CreatureSpellControls.SlotFields(slot).Type)) is not { Form: not 255 })
            { statusKey = "NoOptions"; UpdateStatus(); return false; }
            working = submitted; InputRevision++; var revision = InputRevision;
            choosing = true;
            var child = new CreatureSpellOptionsWindow(document, working, slot, () => Current && choosing && revision == InputRevision);
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
            var prepared = document.UpdateCreatureSpellDraft(working, inputs.Select(item => new CreatureSpellInput(item.Key, item.Value)));
            if (!commitFields() || !Current || revision != InputRevision) return false;
            if (!prepared.SameBaseline(document.CreateCreatureSpellDraft(row))) { stale = true; statusKey = "Stale"; UpdateStatus(); return false; }
            document.ApplyCreatureSpellDraft(prepared); working = document.CreateCreatureSpellDraft(row); LoadInputs();
            InputRevision++; Rebuild(); statusKey = "Applied"; UpdateStatus(); return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { statusKey = "Invalid"; UpdateStatus(); return false; }
        finally { applying = false; }
    }
}
