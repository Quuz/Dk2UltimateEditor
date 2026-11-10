using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Original terrain page6820 with a private, complete 22-field draft.</summary>
public sealed class GlobalTerrainWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly GlobalDefinitionRow row;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly StackPanel fields = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(4) };
    private readonly Button apply = new();
    private readonly Dictionary<string, string> inputs = [];
    private readonly List<string> registered = [];
    private readonly Dictionary<string, Button> colours = [];
    private GlobalTerrainDraft baseline;
    private bool rebuilding, closed, stale, applying, choosingColour;
    private string? statusKey;
    private sealed record Choice(string ID, string Label);
    public long InputRevision { get; private set; }
    public bool HasPendingInputs => inputs.Any(item => item.Value != baseline.Display(item.Key));
    /// <summary>Null uses the actual Windows colour dialog. Injected selection
    /// does not establish native-dialog or hardware acceptance.</summary>
    public Func<uint, uint?>? ColourSelector { get; set; }

    public GlobalTerrainWindow(NativeMapDocument document, GlobalDefinitionRow row, Func<bool> ownerCurrent, Func<bool> commitFields)
    {
        this.document = document; this.row = row; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields;
        baseline = document.CreateGlobalTerrainDraft(row);
        foreach (var field in GlobalTerrainControls.Fields) inputs[field] = baseline.Display(field);
        NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 700; Height = 580; MinWidth = 540; MinHeight = 420; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel;
        panel.SetResourceReference(Panel.BackgroundProperty, "Brush.Background");
        var explanation = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4, 0, 4, 8) };
        explanation.SetResourceReference(TextBlock.TextProperty, "TerrainProperties.Hint"); DockPanel.SetDock(explanation, Dock.Top); panel.Children.Add(explanation);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status); RegisterName("TerrainPropertiesStatus", status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        apply.SetResourceReference(ContentControl.ContentProperty, "TerrainProperties.Apply"); apply.Click += (_, _) => ApplyTerrain(); RegisterName("TerrainPropertiesApply", apply); buttons.Children.Add(apply);
        var cancel = new Button { IsCancel = true }; cancel.SetResourceReference(ContentControl.ContentProperty, "TerrainProperties.Cancel"); cancel.Click += (_, _) => Close(); RegisterName("TerrainPropertiesCancel", cancel); buttons.Children.Add(cancel); footer.Children.Add(buttons);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = fields }; RegisterName("TerrainPropertiesScroll", scroll); panel.Children.Add(scroll);
        document.Changed += DocumentChanged; LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) => { closed = true; InputRevision++; document.Changed -= DocumentChanged; LocalizationService.LanguageChanged -= LanguageChanged; };
        Rebuild(); UpdateStatus();
    }
    private static string Text(string key) => LocalizationService.Get("TerrainProperties." + key);
    private bool Current => !closed && !stale && ownerCurrent();
    private void Register(Control control, string field)
    { var name = "TerrainProperty" + field; control.Name = name; RegisterName(name, control); registered.Add(name); }
    private void Rebuild()
    {
        rebuilding = true;
        try
        {
            Title = Text("Title") + " · " + LocalizationService.Get("Terrain." + row.ID);
            foreach (var name in registered) UnregisterName(name); registered.Clear(); colours.Clear(); fields.Children.Clear();
            foreach (var field in GlobalTerrainControls.Fields)
            {
                var cell = new StackPanel { Margin = new(4, 2, 4, 8) };
                cell.Children.Add(new TextBlock { Text = GlobalElementFieldLabels.Label("Terrains", field), TextWrapping = TextWrapping.Wrap });
                var enabled = baseline.Editable && GlobalTerrainControls.Enabled(row.Index, field);
                if (field is "MaxHealthType" or "DestroyedType" or "MaxHealthEffect" or "DestroyedEffect")
                {
                    var choices = GlobalTerrainControls.Choices(row.Index, field).Select(choice => new Choice(choice.Value, choice.Name(LocalizationService.CurrentLanguage))).ToList();
                    if (!choices.Any(choice => choice.ID == inputs[field])) choices.Insert(0, new(inputs[field], Text("Imported") + " · " + inputs[field]));
                    var combo = new ComboBox { DisplayMemberPath = "Label", ItemsSource = choices, SelectedItem = choices.First(choice => choice.ID == inputs[field]), IsEnabled = enabled, VerticalAlignment = VerticalAlignment.Top };
                    combo.SelectionChanged += (_, _) => { if (!rebuilding && combo.SelectedItem is Choice choice) Changed(field, choice.ID); }; Register(combo, field); cell.Children.Add(combo);
                }
                else if (field is "Visible" or "TerrainLight" or "AmbientLight")
                {
                    var check = new CheckBox { IsThreeState = inputs[field] is not ("0" or "1"), IsChecked = inputs[field] switch { "0" => false, "1" => true, _ => null }, IsEnabled = enabled, Content = Text("Enabled") };
                    check.Checked += (_, _) => { if (!rebuilding) Changed(field, "1"); };
                    check.Unchecked += (_, _) => { if (!rebuilding) Changed(field, "0"); }; Register(check, field); cell.Children.Add(check);
                }
                else
                {
                    var input = new TextBox { Text = inputs[field], IsEnabled = enabled };
                    input.TextChanged += (_, _) => NumericChanged(field,input); Register(input, field); cell.Children.Add(input);
                    AddNumberSteps(field,input,cell);
                }
                fields.Children.Add(cell);
                if (field is "TerrainLightB" or "AmbientLightB")
                {
                    var light = field[..^1]; var button = new Button { Content = Text("ChooseColour"), HorizontalAlignment = HorizontalAlignment.Left, IsEnabled = baseline.Editable };
                    button.Click += (_, _) => ChooseColour(light); Register(button, light + "Colour"); colours.Add(light, button); cell.Children.Add(button);
                }
            }
            UpdateColours();
        }
        finally { rebuilding = false; }
    }

    private bool NumberCurrent(string field,TextBox input)=>Current&&baseline.Editable&&!applying&&!choosingColour&&GlobalElementInputs.Handles("Terrains",field)&&GlobalTerrainControls.Enabled(row.Index,field)&&input.IsEnabled&&ReferenceEquals(FindName("TerrainProperty"+field),input);
    private void NumericChanged(string field,TextBox input)
    {
        if(rebuilding)return;
        var text=NumberCurrent(field,input)?GlobalElementInputs.Normalize("Terrains",field,input.Text):input.Text;
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
        if(!GlobalElementInputs.Handles("Terrains",field))return;
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
        if(direction is not (-1 or 1)||!GlobalElementInputs.Handles("Terrains",field)||FindName("TerrainProperty"+field) is not TextBox input||!NumberCurrent(field,input))return false;
        input.Text=GlobalElementInputs.Step("Terrains",field,input.Text,direction);input.CaretIndex=input.Text.Length;return true;
    }

    private void Changed(string field, string value)
    { inputs[field] = value; InputRevision++; if (!stale) statusKey = null; UpdateColours(); UpdateStatus(); }
    private uint Colour(string light)
    {
        uint Channel(string field) => uint.TryParse(inputs[field], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value & 255 : 0;
        return Channel(light + "R") | Channel(light + "G") << 8 | Channel(light + "B") << 16;
    }
    private void UpdateColours()
    {
        foreach (var (light, button) in colours)
        { var colour = Colour(light); button.BorderBrush = new SolidColorBrush(Color.FromRgb((byte)colour, (byte)(colour >> 8), (byte)(colour >> 16))); button.BorderThickness = new(4); }
    }
    public bool ChooseColour(string light)
    {
        if (light is not ("TerrainLight" or "AmbientLight")) throw new ArgumentException("Unknown terrain light.");
        if (!Current || !baseline.Editable || applying || choosingColour) return false;
        var revision = InputRevision; choosingColour = true;
        try
        {
            var colour = (ColourSelector ?? (value => NativeTerrainColourDialog.Choose(this, value)))(Colour(light));
            if (colour is null || colour > 0xffffff || !Current || revision != InputRevision) return false;
            inputs[light + "R"] = (colour.Value & 255).ToString(CultureInfo.InvariantCulture);
            inputs[light + "G"] = ((colour.Value >> 8) & 255).ToString(CultureInfo.InvariantCulture);
            inputs[light + "B"] = ((colour.Value >> 16) & 255).ToString(CultureInfo.InvariantCulture);
            InputRevision++; Rebuild(); UpdateStatus(); return true;
        }
        finally { choosingColour = false; }
    }
    private void UpdateStatus()
    { status.Text = Text(statusKey ?? (!baseline.Editable ? "ReadOnly" : HasPendingInputs ? "Draft" : "Ready")); apply.IsEnabled = Current && baseline.Editable; }
    private void DocumentChanged(object? sender, EventArgs args)
    { if (!closed && !applying) { stale = true; statusKey = "Stale"; UpdateStatus(); } }
    private void LanguageChanged(object? sender, EventArgs args)
    { if (!closed) { InputRevision++; Rebuild(); UpdateStatus(); } }
    public bool ApplyTerrain()
    {
        if (!Current || !baseline.Editable || applying || choosingColour) return false;
        applying = true; var revision = InputRevision;
        try
        {
            var prepared = document.UpdateGlobalTerrainDraft(baseline, inputs.Select(item => new GlobalTerrainInput(item.Key, item.Value)));
            if (!commitFields() || !Current || revision != InputRevision) return false;
            if (!prepared.SameBaseline(document.CreateGlobalTerrainDraft(row))) { stale = true; statusKey = "Stale"; UpdateStatus(); return false; }
            document.ApplyGlobalTerrainDraft(prepared);
            baseline = document.CreateGlobalTerrainDraft(row);
            foreach (var field in GlobalTerrainControls.Fields) inputs[field] = baseline.Display(field);
            InputRevision++; Rebuild(); statusKey = "Applied"; UpdateStatus(); return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException)
        { statusKey = "Invalid"; UpdateStatus(); return false; }
        finally { applying = false; }
    }
}
