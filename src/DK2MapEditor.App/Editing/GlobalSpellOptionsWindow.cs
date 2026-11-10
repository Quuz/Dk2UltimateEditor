using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Original projectile child6840..6844 for either slot. OK returns a private parent draft.</summary>
public sealed class GlobalSpellOptionsWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly GlobalSpellDraft baseline;
    private readonly Func<bool> ownerCurrent;
    private readonly string slot;
    private readonly (string Type, string Data1, string Data2) slotFields;
    private readonly GlobalProjectileOption option;
    private readonly StackPanel fields = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(4) };
    private readonly Button accept = new();
    private readonly List<string> registered = [];
    private TextBlock? goldLabel;
    private string first, second;
    private bool rebuilding, closed, accepting;
    private bool invalid;
    private long revision;
    private sealed record Choice(string ID,string Label,System.Windows.Media.Imaging.BitmapSource? Icon=null);
    public GlobalSpellDraft? AcceptedDraft { get; private set; }
    public event EventHandler? Accepted;
    public GlobalSpellOptionsWindow(NativeMapDocument document, GlobalSpellDraft baseline, string slot, Func<bool> ownerCurrent)
    {
        this.document = document; this.baseline = baseline; this.slot = slot; slotFields = GlobalSpellControls.SlotFields(slot); this.ownerCurrent = ownerCurrent;
        option = GlobalProjectiles.Find(baseline.Raw(slotFields.Type)) ?? throw new ArgumentException("Unknown original projectile.");
        if (option.Form == 255) throw new ArgumentException("This source type has no options.");
        first = baseline.Display(slotFields.Data1); second = baseline.Display(slotFields.Data2);
        var inputs = GlobalProjectiles.EditorInputs(option, first, second);
        first = inputs.Data1; second = inputs.Data2;
        NameScope.SetNameScope(this, new NameScope()); SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 600; Height = 370; MinWidth = 420; MinHeight = 300; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel; panel.SetResourceReference(Panel.BackgroundProperty, "Brush.Background");
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status); RegisterName("SpellOptionsStatus", status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        accept.SetResourceReference(ContentControl.ContentProperty, "SpellProperties.OptionsOK"); accept.Click += (_, _) => AcceptOptions(); RegisterName("SpellOptionsOK", accept); buttons.Children.Add(accept);
        var cancel = new Button { IsCancel = true }; cancel.SetResourceReference(ContentControl.ContentProperty, "SpellProperties.Cancel"); cancel.Click += (_, _) => Close(); RegisterName("SpellOptionsCancel", cancel); buttons.Children.Add(cancel); footer.Children.Add(buttons);
        panel.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = fields });
        LocalizationService.LanguageChanged += LanguageChanged; document.Changed += DocumentChanged;
        Closed += (_, _) => { closed = true; revision++; LocalizationService.LanguageChanged -= LanguageChanged; document.Changed -= DocumentChanged; };
        Rebuild(); UpdateStatus();
    }
    private static string ProjectileText(string key) => LocalizationService.Get("Editor.Definitions.Projectile." + key);
    private void Register(Control control, string suffix) { var name = "SpellOptions" + suffix; control.Name = name; RegisterName(name, control); registered.Add(name); }
    private void Rebuild()
    {
        rebuilding = true;
        try
        {
            Title = GlobalSpellWindow.Text(slot == "Shot" ? "PrimaryOptions" : "UpgradeOptions");
            foreach (var name in registered) UnregisterName(name); registered.Clear(); fields.Children.Clear(); goldLabel = null;
            {
                fields.Children.Add(new TextBlock { Text = option!.Name.Name(LocalizationService.CurrentLanguage), TextWrapping = TextWrapping.Wrap, Margin = new(4) });
                if (option.Form is 2 or 3 or 4)
                {
                    fields.Children.Add(new TextBlock { Text = ProjectileText(option.Form == 4 ? "Object" : "Creature"), TextWrapping = TextWrapping.Wrap, Margin = new(4) });
                    Choices("Data1", GlobalProjectiles.Targets(option), first, false);
                }
                else Number("Data1", first, ProjectileText("Label1." + option.Label1), option.Signed1 ? "−32768 … 32767" : "0 … 32767", false);
                if (option.Form == 3)
                {
                    fields.Children.Add(new TextBlock { Text = ProjectileText("Level"), TextWrapping = TextWrapping.Wrap, Margin = new(4) });
                    Choices("Data2", Enumerable.Range(1, 10).Select(level => new ValueChoice(level.ToString(CultureInfo.InvariantCulture), level.ToString(CultureInfo.InvariantCulture), level.ToString(CultureInfo.InvariantCulture))).ToArray(), second, true);
                }
                else if (option.Form != 1)
                    Number("Data2", second, ProjectileText(option.Form == 2 ? "Count" : option.Form == 4 ? "Gold" : "Label2." + option.Label2), option.Form == 2 ? "0 … 100" : option.Form == 4 ? docGoldRange : option.Signed2 ? "−32768 … 32767" : "0 … 32767", true);
            }
        }
        finally { rebuilding = false; }
    }
    private string docGoldRange => document.IsLegacyProject ? "0 … 80000" : "0 … 65535";
    private void Choices(string suffix, IReadOnlyList<ValueChoice> catalogue, string value, bool isSecond)
    {
        var choices = catalogue.Select(choice => new Choice(choice.Value, choice.Name(LocalizationService.CurrentLanguage),
            isSecond?null:OriginalDefinitionImages.ProjectileTarget(option,choice.Value))).ToList();
        if (!choices.Any(choice => choice.ID == value)) choices.Insert(0, new(value, GlobalSpellWindow.Text("Imported") + " · " + value));
        var combo = new ComboBox { ItemsSource = choices, SelectedItem = choices.First(choice => choice.ID == value), Margin = new(4) };
        if(isSecond)combo.DisplayMemberPath="Label";else combo.ItemTemplate=OriginalObjectImages.NamedTemplate();
        combo.SelectionChanged += (_, _) => { if (!rebuilding && combo.SelectedItem is Choice choice) { if (isSecond) second = choice.ID; else first = choice.ID; revision++; invalid = false; UpdateGold(); UpdateStatus(); } };
        Register(combo, suffix); fields.Children.Add(combo);
    }
    private void Number(string suffix, string value, string label, string range, bool isSecond)
    {
        var enabled = !isSecond || option.Form != 4 || first == "1";
        var labelControl = new TextBlock { Text = label + " · " + range, TextWrapping = TextWrapping.Wrap, Margin = new(4), IsEnabled = enabled }; fields.Children.Add(labelControl);
        if (isSecond && option.Form == 4) goldLabel = labelControl;
        var input = new TextBox { Text = value, Margin = new(4), IsEnabled = enabled };
        input.TextChanged += (_, _) => NumberChanged(input,isSecond);
        input.PreviewKeyDown+=(_,e)=>
        {
            if(System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.None
                &&e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down
                &&StepNumber(isSecond,e.Key==System.Windows.Input.Key.Up?1:-1))e.Handled=true;
        };
        Register(input, suffix); fields.Children.Add(input);
        var buttons = new WrapPanel { Margin = new(4) };
        foreach (var direction in new[] { -1, 1 })
        {
            var button = new System.Windows.Controls.Primitives.RepeatButton { Delay=400, Interval=75, Content = direction < 0 ? "−" : "+", IsEnabled = enabled, MinWidth = 32, ToolTip = GlobalSpellWindow.Text(direction < 0 ? "Decrease" : "Increase") };
            button.Click += (_, _) => StepNumber(isSecond, direction); Register(button, suffix + (direction < 0 ? "Decrease" : "Increase")); buttons.Children.Add(button);
        }
        fields.Children.Add(buttons);
    }
    private void UpdateGold()
    {
        if (option.Form != 4) return;
        if (goldLabel is not null) goldLabel.IsEnabled = first == "1";
        foreach (var name in new[] { "SpellOptionsData2", "SpellOptionsData2Decrease", "SpellOptionsData2Increase" }) if (FindName(name) is Control control) control.IsEnabled = first == "1";
    }
    private bool NumberCurrent(TextBox input,bool isSecond)
        =>!rebuilding&&!closed&&!accepting&&baseline.Editable&&ownerCurrent()&&input.IsEnabled
            &&(!isSecond||option.Form!=4||first=="1")
            &&(GlobalProjectileInputs.IsNumber(option,isSecond))&&ReferenceEquals(FindName("SpellOptions"+(isSecond?"Data2":"Data1")),input);
    private void NumberChanged(TextBox input,bool isSecond)
    {
        if(!NumberCurrent(input,isSecond))return;
        var text=GlobalProjectileInputs.Normalize(option,isSecond,input.Text);
        if(text!=input.Text)
        {
            rebuilding=true;
            try{input.Text=text;input.CaretIndex=text.Length;}
            finally{rebuilding=false;}
        }
        if(isSecond)second=input.Text;else first=input.Text;
        revision++;invalid=false;UpdateStatus();
    }
    public bool StepNumber(bool isSecond,int direction)
    {
        if(direction is not (-1 or 1)||FindName("SpellOptions"+(isSecond?"Data2":"Data1")) is not TextBox input||!NumberCurrent(input,isSecond))return false;
        input.Text=GlobalProjectileInputs.Step(option,isSecond,input.Text,direction);input.CaretIndex=input.Text.Length;return true;
    }
    private void UpdateStatus() { status.Text = GlobalSpellWindow.Text(!ownerCurrent() ? "Stale" : invalid ? "OptionsInvalid" : "OptionsHint"); accept.IsEnabled = !closed && ownerCurrent() && baseline.Editable; fields.IsEnabled = accept.IsEnabled; }
    private void DocumentChanged(object? sender, EventArgs args) { if (!closed) { revision++; UpdateStatus(); } }
    private void LanguageChanged(object? sender, EventArgs args) { if (!closed) { Rebuild(); UpdateStatus(); } }
    public bool AcceptOptions()
    {
        if (closed || accepting || !ownerCurrent() || !baseline.Editable) return false;
        accepting = true; var currentRevision = revision;
        try
        {
            var result = document.UpdateGlobalSpellShotDraft(baseline, slot, first, second);
            if (closed || !ownerCurrent() || currentRevision != revision) return false;
            AcceptedDraft = result; Accepted?.Invoke(this, EventArgs.Empty); Close(); return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException) { invalid = true; UpdateStatus(); return false; }
        finally { accepting = false; }
    }
}
