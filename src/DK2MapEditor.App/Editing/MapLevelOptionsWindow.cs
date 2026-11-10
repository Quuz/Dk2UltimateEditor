using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class MapLevelOptionsWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly StackPanel fields = new();
    private readonly TextBlock status = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly Button apply = new(), discard = new();
    private readonly Dictionary<string, Control> controls = [];
    private readonly Dictionary<string, Slider> sliders = [];
    private MapLevelOptionsDraft baseline;
    private Dictionary<string, string> inputs;
    private bool updating, stale, closed, applying;
    public bool HasPendingInputs => !closed && inputs.Any(row => baseline.Values.GetValueOrDefault(row.Key) != row.Value);
    public long InputRevision { get; private set; }
    private string? statusKey;
    private sealed record RewardView(string ID, string Label, int? ImageResource);
    public MapLevelOptionsWindow(NativeMapDocument document, Func<bool> ownerCurrent, Func<bool> commitFields)
    {
        this.document = document; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields;
        baseline = document.CreateMapLevelOptionsDraft(); inputs = baseline.Values.ToDictionary(row => row.Key, row => row.Value);
        NameScope.SetNameScope(this, new NameScope()); SetResourceReference(TitleProperty, "LevelOptions.Title");
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 850; Height = 700; MinWidth = 760; MinHeight = 600; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel;
        var explanation = new TextBlock { Margin = new(4), TextWrapping = TextWrapping.Wrap }; explanation.SetResourceReference(TextBlock.TextProperty, "LevelOptions.Explanation");
        DockPanel.SetDock(explanation, Dock.Top); panel.Children.Add(explanation);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status); RegisterName("LevelOptionsStatus", status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        apply.SetResourceReference(ContentControl.ContentProperty, "LevelOptions.Apply"); apply.Click += (_, _) => ApplyOptions(); RegisterName("LevelOptionsApply", apply);
        discard.SetResourceReference(ContentControl.ContentProperty, "LevelOptions.Discard"); discard.Click += (_, _) => ResetDraft(); RegisterName("LevelOptionsDiscard", discard);
        var close = new Button { IsCancel = true }; close.SetResourceReference(ContentControl.ContentProperty, "LevelOptions.Close"); close.Click += (_, _) => Close();
        buttons.Children.Add(apply); buttons.Children.Add(discard); buttons.Children.Add(close); footer.Children.Add(buttons);
        panel.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = fields });
        document.Changed += DocumentChanged; LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) => { closed = true; document.Changed -= DocumentChanged; LocalizationService.LanguageChanged -= LanguageChanged; };
        Rebuild(); UpdateStatus();
    }
    private static string Text(string key) => LocalizationService.Get("LevelOptions." + key);
    private void Register(Control control, string name) { control.Name = name; RegisterName(name, control); controls.Add(name, control); }
    private void Rebuild()
    {
        updating = true;
        try
        {
            foreach (var key in controls.Keys) UnregisterName(key); controls.Clear(); sliders.Clear(); fields.Children.Clear();
            var scalars = new WrapPanel(); fields.Children.Add(scalars);
            foreach (var scalar in MapLevelOptions.Scalars)
            {
                var cell = new StackPanel { Width = 350, Margin = new(4, 4, 12, 8) };
                cell.Children.Add(new TextBlock { Text = Text("Field." + scalar.Field) + $" ({scalar.Minimum}–{scalar.Maximum})", TextWrapping = TextWrapping.Wrap });
                var input = new TextBox { Text = inputs.GetValueOrDefault(scalar.Field, ""), IsEnabled = inputs.ContainsKey(scalar.Field), Margin = new(0, 4, 0, 0) };
                Register(input, "LevelOption" + scalar.Field); cell.Children.Add(input);
                var slider = new Slider { Minimum = scalar.Minimum, Maximum = scalar.Maximum, IsSnapToTickEnabled = true, TickFrequency = 1, SmallChange = 1, LargeChange = 1, IsEnabled = input.IsEnabled };
                sliders.Add(scalar.Field, slider); SyncSlider(scalar.Field); cell.Children.Add(slider);
                input.TextChanged += (_, _) => { if (!updating) { inputs[scalar.Field] = input.Text; SyncSlider(scalar.Field); ChangedInput(); } };
                slider.ValueChanged += (_, args) => { if (!updating) input.Text = ((int)Math.Round(args.NewValue)).ToString(CultureInfo.InvariantCulture); };
                scalars.Children.Add(cell);
            }
            var flags = new WrapPanel(); fields.Children.Add(flags);
            foreach (var flag in MapLevelOptions.Flags)
            {
                var caption = new TextBlock { Text = Text("Flag." + flag.Key), TextWrapping = TextWrapping.Wrap, Width = 325 };
                var check = new CheckBox { Content = caption, Width = 350, Margin = new(4, 5, 12, 5), IsEnabled = inputs.ContainsKey(flag.Field),
                    IsChecked = inputs.TryGetValue(flag.Field, out var raw) && MapLevelOptions.Checked(flag, int.Parse(raw, CultureInfo.InvariantCulture)) };
                Register(check, "LevelFlag" + flag.Key);
                check.Checked += (_, _) => Toggle(); check.Unchecked += (_, _) => Toggle(); flags.Children.Add(check);
                void Toggle() { if (!updating) { inputs[flag.Field] = MapLevelOptions.SetChecked(flag, int.Parse(inputs[flag.Field], CultureInfo.InvariantCulture), check.IsChecked == true).ToString(CultureInfo.InvariantCulture); ChangedInput(); } }
            }
            var rewards = new WrapPanel { Margin = new(0, 10, 0, 0) }; fields.Children.Add(rewards);
            foreach (var key in MapLevelOptions.RewardFields)
            {
                var cell = new StackPanel { Width = 350, Margin = new(4, 4, 12, 8) }; cell.Children.Add(new TextBlock { Text = Text("Field." + key) });
                var choices = MapLevelOptions.Rewards.Select(row => new RewardView(row.Value, row.Value + " · " + row.Name(LocalizationService.CurrentLanguage), row.Icon)).ToList();
                var raw = inputs.GetValueOrDefault(key, ""); if (raw.Length != 0 && choices.All(row => row.ID != raw)) choices.Add(new(raw, raw + " · " + Text("Unknown"), null));
                var combo = new ComboBox { ItemsSource = choices, ItemTemplate = RewardChoiceTemplate.Create(), SelectedItem = choices.FirstOrDefault(row => row.ID == raw), IsEnabled = inputs.ContainsKey(key), Margin = new(0, 4, 0, 0) };
                combo.ToolTip = (combo.SelectedItem as RewardView)?.Label; Register(combo, "LevelOption" + key); cell.Children.Add(combo); rewards.Children.Add(cell);
                combo.SelectionChanged += (_, _) => { if (!updating && combo.SelectedItem is RewardView choice) { inputs[key] = choice.ID; combo.ToolTip = choice.Label; ChangedInput(); } };
            }
        }
        finally { updating = false; }
    }
    private void SyncSlider(string key)
    {
        var previous = updating; updating = true;
        try { if (sliders.TryGetValue(key, out var slider)) slider.Value = int.TryParse(inputs.GetValueOrDefault(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? Math.Clamp(value, (int)slider.Minimum, (int)slider.Maximum) : slider.Minimum; }
        finally { updating = previous; }
    }
    private void ChangedInput() { InputRevision++; if (!stale) statusKey = null; UpdateStatus(); }
    private void UpdateStatus()
    {
        status.Text = Text(statusKey ?? (!baseline.Editable ? "ReadOnly" : baseline.Partial ? "Partial" : "Hint"));
        apply.IsEnabled = !closed && !stale && ownerCurrent() && baseline.Editable;
        discard.IsEnabled = !closed && ownerCurrent();
    }
    private void ResetDraft()
    {
        if (closed || !ownerCurrent()) return;
        InputRevision++;
        baseline = document.CreateMapLevelOptionsDraft(); inputs = baseline.Values.ToDictionary(row => row.Key, row => row.Value); stale = false; statusKey = null; Rebuild(); UpdateStatus();
    }
    private void DocumentChanged(object? sender, EventArgs args) { if (!closed && !applying) { stale = true; statusKey = "Stale"; UpdateStatus(); } }
    private void LanguageChanged(object? sender, EventArgs args) { if (!closed) { InputRevision++; Rebuild(); UpdateStatus(); } }
    public bool ApplyOptions()
    {
        if (closed || stale || !ownerCurrent() || !baseline.Editable) return false;
        try
        {
            var prepared = document.UpdateMapLevelOptions(baseline, inputs.Select(row => new MapLevelInput(row.Key, row.Value)));
            if (!commitFields() || closed || stale || !ownerCurrent()) return false;
            applying = true; document.ApplyMapLevelOptions(prepared); applying = false; ResetDraft(); return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException or System.IO.InvalidDataException)
        { if (exception is InvalidOperationException) stale = true; statusKey = stale ? "Stale" : "Invalid"; UpdateStatus(); return false; }
        finally { applying = false; }
    }
}
