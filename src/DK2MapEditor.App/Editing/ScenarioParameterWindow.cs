using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class ScenarioParameterWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly NativeRecord record;
    private readonly string sourceType;
    private readonly ushort sourceID;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly bool privateCreation;
    private readonly bool privateEditing;
    private readonly StackPanel body = new();
    private readonly TextBlock heading = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap }, status = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly Button apply = new(), discard = new();
    private readonly Dictionary<int, TextBox> numbers = [];
    private readonly Dictionary<int, ComboBox> choices = [];
    private readonly Dictionary<int, (TextBox Hours, TextBox Minutes, TextBox Seconds, TextBox Total)> times = [];
    private readonly Dictionary<int, Slider> sliders = [];
    private readonly Dictionary<int, TextBlock> importedHints = [], rangeLabels = [];
    private readonly TextBlock bandPopulation = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly ComboBox areaContext = new() { Margin = new(4), DisplayMemberPath = "Label" };
    private readonly TextBlock areaMetrics = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly Func<IReadOnlyList<ScenarioAreaOwnerContext>> areaContexts;
    private readonly ScriptOwner? preferredAreaOwner;
    private readonly bool retainEmptyAreaContext;
    public bool HasAreaContextSelection => ScenarioAreaHints.Supports(draft.Type) && areaContext.SelectedItem is AreaContextChoice;
    public ScriptOwner? SelectedAreaContextOwner => (areaContext.SelectedItem as AreaContextChoice)?.Context?.OriginalOwner;
    private sealed record AreaContextChoice(ScenarioAreaOwnerContext? Context, string Label);
    private readonly HashSet<int> changed = [];
    private readonly ComboBox repeatMode = new() { Margin = new(4), MinWidth = 200, DisplayMemberPath = "Label" };
    private readonly TextBox repeatCount = Input();
    private ScenarioParameterDraft draft = null!;
    private readonly ScenarioAliasEventEditor? aliasEditor;
    private readonly ScenarioSpeechPanel? speech;
    public Action<AudioPreviewLaunch>? SpeechPlaybackLauncher
    { get => speech?.PlaybackLauncher; set { if (speech is not null) speech.PlaybackLauncher = value; } }
    public bool PlaySpeech() => speech?.PlaySelected() == true;
    private CameraPathWindow? cameraLibrary;
    public Action<CameraPathWindow>? CameraLibraryPresenter { get; set; }
    private ScenarioSpawnFlagsWindow? spawnFlags;
    public Action<ScenarioSpawnFlagsWindow>? SpawnFlagsPresenter { get; set; }
    public Action<ScenarioParameterWindow>? SpawnPointRequester { get; set; }
    public Action? CancelSpawnPointPick { get; set; }
    private sealed record SpawnArea(uint ID, string Label, string Name = "", string Info = "");
    private static string AreaLabel(uint id, string name, string info) => LocalizationService.Get("ScriptScope.4")
        + " #" + id.ToString(CultureInfo.InvariantCulture) + EntityReferences.DisplayDetails(name, info);
    private static SpawnArea AreaChoice(EntityIdentity identity, uint id)
    {
        string Read(string name) => identity.Record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? identity.Record.Read(field) : "";
        var name = Read("name"); var info = Read("info");
        return new(id, AreaLabel(id, name, info), name, info);
    }
    private bool refreshing, stale, closed, applying, writing, repeatChanged;
    private long inputRevision;
    private string? statusKey;
    private ComboBox? gatePicker;
    private bool gateChanged;
    private sealed record GateItem(string? Key, string Label);
    public Action<ScenarioParameterWindow>? SpatialPointRequester { get; set; }
    public TilePoint SpatialPoint => new(int.Parse(numbers[5].Text, CultureInfo.InvariantCulture), int.Parse(numbers[7].Text, CultureInfo.InvariantCulture));
    internal void EnableSpatialPointInputs(bool enabled)
    {
        if (draft.Type != "ACTION_CHANGESQUARE") return;
        foreach (var number in new[] { 5, 7 }) numbers[number].IsEnabled = enabled;
        foreach (var name in new[] { "ScenarioSpatialArea", "ScenarioSpatialPoint" }) ((Control)FindName(name)).IsEnabled = enabled;
    }
    private IReadOnlyList<ScenarioBandPreview> bandPreviews = [];
    private IReadOnlyList<ValueChoice>? bandChoices;
    private sealed record Choice(string Value, string Label, int Row = -1, int? ImageResource = null, bool Selectable = true)
    {
        public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalScenarioImages.For(ImageResource);
    }
    private sealed record RepeatChoice(ScenarioRepeatMode Mode, string Label);
    private static TextBox Input() => new() { Margin = new(4), MinWidth = 90 };
    private static bool IsChoice(ScenarioControl control) => control.Kind is ScenarioControlKind.Choice or ScenarioControlKind.NamedChoice or ScenarioControlKind.Element;
    public bool HasPendingInputs => !closed && (changed.Count != 0 || repeatChanged || gateChanged || aliasEditor?.HasPendingInputs == true);

    public ScenarioParameterWindow(NativeMapDocument document, NativeRecord record, Func<bool> ownerCurrent, Func<bool> commitFields, bool privateCreation = false, GameResourceSettings? gameResources = null, bool privateEditing = false,
        Func<IReadOnlyList<ScenarioAreaOwnerContext>>? areaContexts = null, ScriptOwner? preferredAreaOwner = null, bool retainEmptyAreaContext = false)
    {
        this.document = document; this.record = record; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields; this.privateCreation = privateCreation;
        if (privateEditing && !privateCreation) throw new ArgumentException("Editing wizard pages require a private document.");
        this.privateEditing = privateEditing;
        draft = document.CreateScenarioParameterDraft(record);
        this.areaContexts = areaContexts ?? (() => document.ScenarioAreaContexts(draft));
        this.preferredAreaOwner = preferredAreaOwner;
        this.retainEmptyAreaContext = retainEmptyAreaContext;
        sourceType = draft.Type; sourceID = ScriptGraph.ID(record);
        _ = ScenarioControlPages.Controls(draft.Type);
        NameScope.SetNameScope(this, new NameScope()); SetResourceReference(TitleProperty, "ScenarioPage.Title");
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 700; Height = 580; MinWidth = 540; MinHeight = 420; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel;
        DockPanel.SetDock(heading, Dock.Top); panel.Children.Add(heading);
        var hint = new TextBlock { Margin = new(4), TextWrapping = TextWrapping.Wrap }; hint.SetResourceReference(TextBlock.TextProperty, privateEditing ? "ScenarioWizard.EditPageExplanation" : privateCreation ? "ScenarioWizard.PageExplanation" : "ScenarioPage.Explanation"); DockPanel.SetDock(hint, Dock.Top); panel.Children.Add(hint);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right }; footer.Children.Add(buttons);
        apply.SetResourceReference(ContentControl.ContentProperty, "ScenarioPage.Apply"); apply.Click += (_, _) => ApplyDraft(); buttons.Children.Add(apply); RegisterName("ScenarioPageApply", apply);
        discard.SetResourceReference(ContentControl.ContentProperty, "AliasEvent.Discard"); discard.Click += (_, _) => ResetDraft(); buttons.Children.Add(discard); RegisterName("ScenarioPageDiscard", discard);
        var close = new Button { IsCancel = true }; close.SetResourceReference(ContentControl.ContentProperty, "ScenarioPage.Cancel"); close.Click += (_, _) => Close(); buttons.Children.Add(close);
        RegisterName("ScenarioPageStatus", status);
        panel.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        if (NativeMapDocument.HasScenarioAliasPage(draft.Type))
        {
            aliasEditor = new(document, record, () => !closed && ownerCurrent(), commitFields);
            aliasEditor.Embed(RegisterName); body.Children.Add(aliasEditor);
        }
        if (draft.Type == "ACTION_SAY")
        { speech = new(document, gameResources ?? new(), SpeechCurrent, Changed, RegisterName); body.Children.Add(speech); }
        foreach (var control in draft.Type == "ACTION_SAY" ? Array.Empty<ScenarioControl>() : ScenarioControlPages.Controls(draft.Type))
        {
            var labelKey = control.Caption.Length == 0 ? "EventParam." + draft.Type + ".Param" + control.Parameter : "ScenarioPage." + control.Caption;
            var row = Row(labelKey);
            if (!IsChoice(control))
            {
                var importedHint = new TextBlock { Margin = new(4), TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
                importedHint.SetResourceReference(TextBlock.TextProperty, "ScenarioPage.ImportedRange"); row.Children.Add(importedHint); importedHints.Add(control.Parameter, importedHint);
                if (control.Kind != ScenarioControlKind.Duration)
                { var range = new TextBlock { Margin = new(4), TextWrapping = TextWrapping.Wrap }; row.Children.Add(range); rangeLabels.Add(control.Parameter, range); }
            }
            if (IsChoice(control))
            {
                var combo = new ComboBox { Margin = new(4), ItemTemplate = OriginalObjectImages.NamedTemplate(), MinWidth = 220 }; choices.Add(control.Parameter, combo); row.Children.Add(combo); RegisterName("ScenarioParam" + control.Parameter, combo);
                var style = new Style(typeof(ComboBoxItem)) { BasedOn = TryFindResource(typeof(ComboBoxItem)) as Style };
                var preview = new System.Windows.DataTrigger { Binding = new System.Windows.Data.Binding("Selectable"), Value = false };
                preview.Setters.Add(new Setter(IsEnabledProperty, false)); preview.Setters.Add(new Setter(FocusableProperty, false));
                preview.Setters.Add(new Setter(MarginProperty, new Thickness(24, 0, 0, 0))); style.Triggers.Add(preview); combo.ItemContainerStyle = style;
                combo.SelectionChanged += (_, args) =>
                {
                    if (refreshing) return;
                    if (combo.SelectedItem is Choice { Selectable: false })
                    {
                        refreshing = true;
                        try { combo.SelectedItem = args.RemovedItems.OfType<Choice>().FirstOrDefault(item => item.Selectable); }
                        finally { refreshing = false; }
                        return;
                    }
                    if (combo.SelectedItem is Choice) Changed(control.Parameter);
                };
            }
            else if (control.Kind == ScenarioControlKind.Duration)
            {
                var total = Input(); total.IsReadOnly = true; row.Children.Add(total); RegisterName("ScenarioParam" + control.Parameter, total);
                var units = new WrapPanel(); row.Children.Add(units); var h = Input(); var m = Input(); var s = Input();
                void Unit(TextBox box, string key) { var container = new StackPanel(); var label = new TextBlock { Margin = new(4) }; label.SetResourceReference(TextBlock.TextProperty, "ScenarioPage." + key); container.Children.Add(label); container.Children.Add(box); units.Children.Add(container); RegisterName("Scenario" + key, box); box.MaxLength = 2; }
                Unit(h, "Hours"); Unit(m, "Minutes"); Unit(s, "Seconds"); times.Add(control.Parameter, (h, m, s, total));
                foreach (var (box, maximum) in new[] { (h, 23u), (m, 59u), (s, 59u) })
                    box.TextChanged += (_, _) => { if (refreshing) return; Clamp(box, maximum); Changed(control.Parameter); UpdateTotal(control.Parameter); };
            }
            else
            {
                var box = Input(); numbers.Add(control.Parameter, box); row.Children.Add(box); RegisterName("ScenarioParam" + control.Parameter, box);
                box.MaxLength = (control.Maximum - control.DisplayOffset).ToString(CultureInfo.InvariantCulture).Length;
                box.TextChanged += (_, _) => { if (!refreshing) Changed(control.Parameter); };
                if (control.Kind == ScenarioControlKind.Percentage)
                {
                    var slider = new Slider { Minimum = 0, Maximum = 100, TickFrequency = 25, IsSnapToTickEnabled = false, SmallChange = 1, LargeChange = 25, Margin = new(4) };
                    sliders.Add(control.Parameter, slider); row.Children.Add(slider); RegisterName("ScenarioMusicSlider", slider);
                    slider.ValueChanged += (_, _) => { if (!refreshing) box.Text = Math.Round(slider.Value).ToString(CultureInfo.InvariantCulture); };
                }
            }
        }
        if (ScenarioAreaHints.Supports(draft.Type))
        {
            var row = Row("ScenarioPage.AreaContext"); row.Children.Add(areaContext); row.Children.Add(areaMetrics);
            RegisterName("ScenarioAreaContext", areaContext); RegisterName("ScenarioAreaMetrics", areaMetrics);
            areaContext.SelectionChanged += (_, _) => { if (!refreshing) RefreshAreaMetrics(); };
        }
        if (ScenarioBandHints.Supports(draft.Type))
        {
            body.Children.Add(bandPopulation);
            RegisterName("ScenarioBandPopulation", bandPopulation);
        }
        if (draft.Type is "ACTION_CHANGESQUARE" or "ACTION_CHANGEOWNER")
        {
            var row = Row("ScenarioSpawn.AreaPoint"); var area = new ComboBox { Margin = new(4), DisplayMemberPath = "Label" };
            var items = new List<SpawnArea> { new(0, LocalizationService.Get("ScenarioSpawn.NoArea")) };
            foreach (var region in document.MapRegions().Where(region => region.Kind == MapRegionKind.Area))
                if (EntityReferences.Identity(region.Record) is { } identity && uint.TryParse(identity.Record.Read(identity.Field), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id != 0)
                    items.Add(AreaChoice(identity, id));
            area.ItemsSource = items; area.SelectedIndex = 0; row.Children.Add(area); RegisterName("ScenarioSpatialArea", area);
            area.SelectionChanged += (_, _) =>
            {
                if (refreshing || area.SelectedItem is not SpawnArea { ID: > 0 } selected) return;
                try { UpdateSpatialCoordinates(document.UpdateScenarioSpatialArea(draft, selected.ID)); }
                catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException) { status.Text = LocalizationService.Get("AliasEvent.Failed"); status.ToolTip = error.Message; }
            };
            var point = new Button(); point.SetResourceReference(ContentControl.ContentProperty, "ScenarioSpawn.PickPoint");
            OriginalControlIcons.Decorate(point,19);
            point.Click += (_, _) => { if (!closed && !stale && ownerCurrent()) SpatialPointRequester?.Invoke(this); };
            body.Children.Add(point); RegisterName("ScenarioSpatialPoint", point);
        }
        if (draft.Type == "ACTION_DESTROYGATE")
        {
            var row = Row("ScenarioSpatial.Gate"); gatePicker = new ComboBox { Margin = new(4), DisplayMemberPath = "Label" };
            gatePicker.SelectionChanged += (_, _) => { if (!refreshing) { gateChanged = true; inputRevision++; ClearStatus(); } };
            row.Children.Add(gatePicker); RegisterName("ScenarioSpatialGate", gatePicker);
        }
        if (draft.Type == "ACTION_CAMERAPATH")
        {
            var library = new Button(); library.SetResourceReference(ContentControl.ContentProperty, "CameraPath.Open");
            OriginalControlIcons.Decorate(library,26);
            library.Click += (_, _) =>
            {
                var child = CreateCameraLibraryWindow(); if (child is null) return;
                if (CameraLibraryPresenter is { } present) present(child);
                else { child.Owner = Window.GetWindow(body) ?? this; child.ShowDialog(); }
            };
            body.Children.Add(library); RegisterName("ScenarioCameraLibrary", library);
        }
        if (draft.Type == "ACTION_ADDCREATURE")
        {
            numbers[4].IsReadOnly = true;
            var options = new Button(); options.SetResourceReference(ContentControl.ContentProperty, "ScenarioSpawn.Flags");
            options.Click += (_, _) => { var child = CreateSpawnFlagsWindow(); if (child is null) return; if (SpawnFlagsPresenter is { } present) present(child); else { child.Owner = Window.GetWindow(body) ?? this; child.ShowDialog(); } };
            body.Children.Add(options); RegisterName("ScenarioSpawnFlags", options);
            var areaRow = Row("ScenarioSpawn.AreaPoint"); var areas = new ComboBox { Margin = new(4), DisplayMemberPath = "Label" };
            var areaItems = new List<SpawnArea> { new(0, LocalizationService.Get("ScenarioSpawn.NoArea")) };
            foreach (var region in document.MapRegions().Where(region => region.Kind == MapRegionKind.Area))
                if (EntityReferences.Identity(region.Record) is { } identity && uint.TryParse(identity.Record.Read(identity.Field), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id != 0)
                    areaItems.Add(AreaChoice(identity, id));
            areas.ItemsSource = areaItems; areas.SelectedIndex = 0; areaRow.Children.Add(areas); RegisterName("ScenarioSpawnArea", areas);
            areas.SelectionChanged += (_, _) =>
            {
                if (refreshing || areas.SelectedItem is not SpawnArea { ID: > 0 } area) return;
                try { var selected = document.UpdateScenarioSpawnArea(draft, area.ID); UpdateSpawnCoordinates(selected); }
                catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException) { status.Text = LocalizationService.Get("AliasEvent.Failed"); status.ToolTip = error.Message; }
            };
            var point = new Button(); point.SetResourceReference(ContentControl.ContentProperty, "ScenarioSpawn.PickPoint"); point.Click += (_, _) => { if (!closed && !stale && ownerCurrent()) SpawnPointRequester?.Invoke(this); };
            body.Children.Add(point); RegisterName("ScenarioSpawnPoint", point);
            OriginalControlIcons.Decorate(point,19);
        }
        if (draft.Fields.Any(field => field.Name is "Repeat" or "repeat"))
        {
            var row = Row("ScenarioPage.Repeat"); row.Visibility = privateCreation ? Visibility.Collapsed : Visibility.Visible; row.Children.Add(repeatMode); row.Children.Add(repeatCount); repeatCount.MaxLength = 4;
            RegisterName("ScenarioRepeatMode", repeatMode); RegisterName("ScenarioRepeatCount", repeatCount);
            repeatMode.SelectionChanged += (_, _) => { if (!refreshing) { RepeatChanged(); RefreshRepeatActive(); } };
            repeatCount.TextChanged += (_, _) => { if (!refreshing) { Clamp(repeatCount, 254); RepeatChanged(); } };
        }
        document.Changed += SourceChanged; LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) => { closed = true; speech?.Dispose(); CancelSpawnPointPick?.Invoke(); spawnFlags?.Close(); spawnFlags = null; cameraLibrary?.Close(); cameraLibrary = null; aliasEditor?.CloseEditor(); apply.IsEnabled = discard.IsEnabled = false; document.Changed -= SourceChanged; LocalizationService.LanguageChanged -= LanguageChanged; };
        ResetDraft();
    }
    private StackPanel Row(string key)
    {
        var row = new StackPanel { Margin = new(0, 5, 0, 5) }; var label = new TextBlock { Margin = new(4), TextWrapping = TextWrapping.Wrap }; label.SetResourceReference(TextBlock.TextProperty, key); row.Children.Add(label); body.Children.Add(row); return row;
    }
    private bool SpeechCurrent()
    {
        if (closed || stale || !ownerCurrent()) return false;
        try { _ = document.UpdateScenarioControlPage(draft, []); return true; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException) { return false; }
    }
    private void Clamp(TextBox box, uint maximum)
    {
        if (uint.TryParse(box.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > maximum)
        { refreshing = true; try { box.Text = maximum.ToString(CultureInfo.InvariantCulture); } finally { refreshing = false; } }
    }
    private void Changed(int parameter)
    {
        changed.Add(parameter);if(importedHints.TryGetValue(parameter,out var hint))hint.Visibility=Visibility.Collapsed;
        inputRevision++;ClearStatus();
        if(ScenarioTerrainOwners.TerrainParameter(draft.Type)==parameter)RefreshTerrainOwner(selectedTerrain:true);
        if (parameter == 2) RefreshBandPopulation();
    }
    private void RefreshBandPopulation()
    {
        if (!ScenarioBandHints.Supports(draft.Type)) return;
        bandPopulation.Text = "";
        if (closed || stale || !ownerCurrent()) return;
        try
        {
            var selected = (choices[2].SelectedItem as Choice)?.Value ?? "";
            var count = document.ReadScenarioBandPopulation(draft, selected);
            bandPopulation.Text = count is { } value
                ? string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioPage.BandPopulation"), value)
                : LocalizationService.Get("ScenarioPage.BandPopulationUnavailable");
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException)
        { bandPopulation.Text = LocalizationService.Get("ScenarioPage.BandPopulationUnavailable"); }
    }
    private static bool SameAreaOwner(ScriptOwner? first, ScriptOwner? second) => first is not null && second is not null
        && first.Scope == second.Scope && first.Record.File == second.Record.File && first.Record.Offset == second.Record.Offset
        && first.Record.Length == second.Record.Length && first.Record.Category == second.Record.Category;
    private void RefreshAreaContexts()
    {
        if (!ScenarioAreaHints.Supports(draft.Type)) return;
        var previous = (areaContext.SelectedItem as AreaContextChoice)?.Context?.OriginalOwner;
        var retainedEmpty = areaContext.SelectedItem is AreaContextChoice { Context: null }
            || areaContext.SelectedItem is null && retainEmptyAreaContext;
        IReadOnlyList<ScenarioAreaOwnerContext> contexts;
        try { contexts = closed || stale || !ownerCurrent() ? [] : areaContexts(); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException)
        { contexts = []; }
        var items = new List<AreaContextChoice> { new(null, LocalizationService.Get("ScenarioPage.SelectAreaContext")) };
        foreach (var context in contexts)
        {
            var identity = EntityReferences.Identity(context.Owner.Record)!;
            var id = context.Owner.Record.Read(identity.Field);
            items.Add(new(context, LocalizationService.Get("ScriptScope.4") + " #" + id + EntityReferences.DisplayDetails(context.Owner.Record)));
        }
        areaContext.ItemsSource = items;
        areaContext.SelectedItem = retainedEmpty ? items[0] : items.FirstOrDefault(item => SameAreaOwner(item.Context?.OriginalOwner, previous))
            ?? items.FirstOrDefault(item => SameAreaOwner(item.Context?.OriginalOwner, preferredAreaOwner))
            ?? (contexts.Count == 1 ? items[1] : items[0]);
        areaContext.IsEnabled = contexts.Count > 1 && !stale && !closed && ownerCurrent();
        RefreshAreaMetrics();
    }
    private void RefreshAreaMetrics()
    {
        if (!ScenarioAreaHints.Supports(draft.Type)) return;
        areaMetrics.Text = LocalizationService.Get("ScenarioPage.AreaMetricsUnavailable");
        if (closed || stale || !ownerCurrent() || areaContext.SelectedItem is not AreaContextChoice { Context: { } context }) return;
        try
        {
            if (document.ReadScenarioAreaMetrics(draft, context.Owner) is not { } metrics) return;
            areaMetrics.Text = string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioPage.AreaTotal"), metrics.TotalCells);
            if (metrics.PerimeterCells is { } perimeter)
                areaMetrics.Text += "\n" + string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioPage.AreaPerimeter"), perimeter);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException)
        { areaMetrics.Text = LocalizationService.Get("ScenarioPage.AreaMetricsUnavailable"); }
    }
    private void RefreshTerrainOwner(bool selectedTerrain=false)
    {
        if(ScenarioTerrainOwners.TerrainParameter(draft.Type) is not { } parameter)return;
        var owner=choices[2];var terrain=(choices[parameter].SelectedItem as Choice)?.Value??"";
        var state=ScenarioTerrainOwners.State(draft.Type,terrain)!;
        if(selectedTerrain&&state.ForcedOwner is { } forced)
        {
            var wasRefreshing=refreshing;refreshing=true;
            try{owner.SelectedItem=owner.Items.Cast<Choice>().Single(item=>item.Value==forced);changed.Add(2);}
            finally{refreshing=wasRefreshing;}
        }
        owner.IsEnabled=state.OwnerEnabled&&!closed&&!stale&&ownerCurrent();
    }
    private void RepeatChanged() { repeatChanged = true; inputRevision++; ClearStatus(); }
    private void ClearStatus()
    {
        statusKey = stale ? "AliasEvent.Stale" : null; status.ToolTip = null;
        var missing = ScenarioControlPages.MissingRequirements(draft.Type, document);
        if (!stale && missing.Count != 0) statusKey = "ScenarioPage.Requires" + missing[0];
        status.Text = statusKey is null ? "" : LocalizationService.Get(statusKey);
    }
    private string Field(int parameter) => draft.Fields.Single(field => field.Name == "param" + parameter || field.Name == "Param" + parameter).Name;
    private string ChoiceValue(ScenarioControl control)
    {
        var value = draft.Values[Field(control.Parameter)];
        if (control.Kind != ScenarioControlKind.Element) return value;
        var category = long.Parse(value, CultureInfo.InvariantCulture);
        var id = long.Parse(draft.Values[Field(control.SecondaryParameter)], CultureInfo.InvariantCulture);
        return category is >= 0 and <= 65535 && id is >= 0 and <= 65535
            ? ((category << 16) | id).ToString(CultureInfo.InvariantCulture) : value + " / " + id.ToString(CultureInfo.InvariantCulture);
    }
    private long Maximum(ScenarioControl control)
    {
        var operand = draft.Fields.Single(item => item.Name == Field(control.Parameter));
        if (control.Kind == ScenarioControlKind.Number && control.Maximum == uint.MaxValue && operand.Kind == NativeValueKind.Int32) return uint.MaxValue;
        return Math.Min(control.Maximum, operand.Kind switch { NativeValueKind.Byte => 255L, NativeValueKind.UInt16 => 65535L, NativeValueKind.Int32 => int.MaxValue, _ => control.Maximum });
    }
    private string NumberValue(ScenarioControl control)
    {
        var value = draft.Values[Field(control.Parameter)];
        var number = control.Kind == ScenarioControlKind.Number && (control.Unsigned || control.Maximum == uint.MaxValue)
            ? (long)unchecked((uint)int.Parse(value, CultureInfo.InvariantCulture)) : long.Parse(value, CultureInfo.InvariantCulture);
        return (number - control.DisplayOffset).ToString(CultureInfo.InvariantCulture);
    }
    private void UpdateTotal(int parameter)
    {
        var time = times[parameter]; refreshing = true;
        try { time.Total.Text = ScenarioControlPages.Duration(time.Hours.Text, time.Minutes.Text, time.Seconds.Text).ToString(CultureInfo.InvariantCulture); }
        catch (Exception error) when (error is FormatException or OverflowException or ArgumentException) { time.Total.Text = ""; }
        finally { refreshing = false; }
    }
    private void RefreshRepeatActive() => repeatCount.IsEnabled = repeatMode.SelectedItem is RepeatChoice { Mode: ScenarioRepeatMode.Count };
    private void RefreshLabels()
    {
        refreshing = true;
        try
        {
            heading.Text = LocalizationService.Get("Event." + draft.Type, draft.Type);
            foreach (var pair in rangeLabels)
            {
                var source = ScenarioControlPages.Controls(draft.Type).Single(item => item.Parameter == pair.Key);
                var maximum = Maximum(source) - source.DisplayOffset;
                pair.Value.Text = string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioPage.Range"), maximum);
            }
            foreach (var pair in choices)
            {
                var control = ScenarioControlPages.Controls(draft.Type).Single(item => item.Parameter == pair.Key);
                var value = pair.Value.SelectedItem is Choice selected ? selected.Value : ChoiceValue(control);
                var selectedRow = (pair.Value.SelectedItem as Choice)?.Row;
                var catalogue = control.Parameter == ScenarioBandCatalogue.Parameter(draft.Type) && bandChoices is not null ? bandChoices : ScenarioControlPages.Choices(draft, control, document);
                var items = new List<Choice>();
                for (var index = 0; index < catalogue.Count; index++)
                {
                    var item = catalogue[index];
                    items.Add(new(item.Value, item.Name(LocalizationService.CurrentLanguage), index,
                        ScenarioNamedCatalogues.ImageResource(draft.Type, control.Parameter, index, item.Value)));
                    if (control.Parameter != ScenarioBandCatalogue.Parameter(draft.Type) || index >= bandPreviews.Count || bandPreviews[index].ID != item.Value) continue;
                    var band = bandPreviews[index];
                    foreach (var member in band.Members) items.Add(new("", ScenarioBandPreviewRows.Label(member, LocalizationService.CurrentLanguage, key => LocalizationService.Get(key)), -2, member.ImageResource, false));
                    if (!band.Complete) items.Add(new("", LocalizationService.Get("ScenarioPage.BandPreviewUnavailable"), -2, null, false));
                }
                if (items.All(item => item.Value != value))
                {
                    var label = control.Kind == ScenarioControlKind.Element
                        ? string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioPage.UnknownElement"), draft.Values[Field(control.Parameter)], draft.Values[Field(control.SecondaryParameter)])
                        : LocalizationService.Get("AliasEvent.Unknown") + " " + value;
                    items.Add(new(value, label));
                }
                pair.Value.ItemsSource = items;
                pair.Value.SelectedItem = items.FirstOrDefault(item => item.Value == value && item.Row == selectedRow) ?? items.First(item => item.Value == value);
            }
            var mode = repeatMode.SelectedItem is RepeatChoice selectedMode ? selectedMode.Mode : ScenarioRepeatMode.Once;
            repeatMode.ItemsSource = Enum.GetValues<ScenarioRepeatMode>().Select(value => new RepeatChoice(value, LocalizationService.Get("ScenarioPage.Repeat" + value))).ToArray();
            repeatMode.SelectedItem = repeatMode.Items.Cast<RepeatChoice>().Single(item => item.Mode == mode);
            RefreshRepeatActive(); if (statusKey is not null) status.Text = LocalizationService.Get(statusKey);
            RefreshGates();
            speech?.RefreshLanguage();
            foreach (var name in new[] { "ScenarioSpawnArea", "ScenarioSpatialArea" })
            if (FindName(name) is ComboBox spawnAreas && spawnAreas.ItemsSource is IEnumerable<SpawnArea> existingAreas)
            {
                var selectedArea = (spawnAreas.SelectedItem as SpawnArea)?.ID ?? 0;
                spawnAreas.ItemsSource = existingAreas.Select(area => area with
                { Label = area.ID == 0 ? LocalizationService.Get("ScenarioSpawn.NoArea") : AreaLabel(area.ID, area.Name, area.Info) }).ToArray();
                spawnAreas.SelectedItem = spawnAreas.Items.Cast<SpawnArea>().FirstOrDefault(area => area.ID == selectedArea);
            }
            RefreshTerrainOwner();
            RefreshBandPopulation();
            RefreshAreaContexts();
        }
        finally { refreshing = false; }
    }
    private void LanguageChanged(object? sender, EventArgs args) { if (!closed) { inputRevision++; RefreshLabels(); } }
    private void SourceChanged(object? sender, EventArgs args)
    {
        if (writing || closed) return; bandPopulation.Text = areaMetrics.Text = ""; areaContext.IsEnabled = false; speech?.Invalidate(); CancelSpawnPointPick?.Invoke(); spawnFlags?.Close(); spawnFlags = null; cameraLibrary?.Close(); cameraLibrary = null; stale = true; statusKey = "AliasEvent.Stale"; status.Text = LocalizationService.Get("AliasEvent.Stale"); apply.IsEnabled = false;
    }
    public void ResetDraft()
    {
        CancelSpawnPointPick?.Invoke(); spawnFlags?.Close(); spawnFlags = null;
        cameraLibrary?.Close(); cameraLibrary = null;
        if (closed || !ownerCurrent()) { apply.IsEnabled = discard.IsEnabled = false; return; }
        try
        {
            var current = document.CreateScenarioParameterDraft(record);
            if (current.Type != sourceType || ScriptGraph.ID(record) != sourceID)
                throw new InvalidOperationException("Reopen event parameters after its identity or type changes.");
            draft = current; aliasEditor?.ResetDraft(); inputRevision++; stale = false; changed.Clear(); repeatChanged = false; gateChanged = false; ClearStatus();
            bandChoices = null; bandPreviews = [];
            if (ScenarioBandCatalogue.Parameter(draft.Type) is > 0 and var bandParameter)
            {
                bandChoices = ScenarioNamedCatalogues.Choices(draft.Type, bandParameter, document);
                bandPreviews = document.ReadScenarioBandPreviews(draft);
            }
            speech?.Reset(draft);
            refreshing = true;
            try
            {
                foreach (var pair in numbers) pair.Value.Text = NumberValue(ScenarioControlPages.Controls(draft.Type).Single(item => item.Parameter == pair.Key));
                foreach (var pair in sliders) pair.Value.Value = Math.Clamp(long.Parse(draft.Values[Field(pair.Key)], CultureInfo.InvariantCulture), 0, 100);
                foreach (var pair in times)
                {
                    var value = draft.Values[Field(pair.Key)]; var total = unchecked((uint)int.Parse(value, CultureInfo.InvariantCulture));
                    pair.Value.Total.Text = total.ToString(CultureInfo.InvariantCulture); pair.Value.Hours.Text = (total / 3600).ToString(CultureInfo.InvariantCulture); pair.Value.Minutes.Text = (total / 60 % 60).ToString(CultureInfo.InvariantCulture); pair.Value.Seconds.Text = (total % 60).ToString(CultureInfo.InvariantCulture);
                }
                foreach (var pair in importedHints)
                {
                    var source = ScenarioControlPages.Controls(draft.Type).Single(item => item.Parameter == pair.Key);
                    var value = long.Parse(source.Kind == ScenarioControlKind.Number ? NumberValue(source) : draft.Values[Field(pair.Key)], CultureInfo.InvariantCulture);
                    pair.Value.Visibility = value < 0 || value > Maximum(source) - source.DisplayOffset ? Visibility.Visible : Visibility.Collapsed;
                }
                foreach (var combo in choices.Values) combo.SelectedItem = null;
                var repeat = draft.Values.FirstOrDefault(item => item.Key is "Repeat" or "repeat").Value;
                var valueRepeat = repeat is null ? 1 : int.Parse(repeat, CultureInfo.InvariantCulture);
                repeatCount.Text = valueRepeat is 1 or 255 ? "2" : valueRepeat.ToString(CultureInfo.InvariantCulture);
                repeatMode.ItemsSource = new[] { new RepeatChoice(valueRepeat == 1 ? ScenarioRepeatMode.Once : valueRepeat == 255 ? ScenarioRepeatMode.Infinite : ScenarioRepeatMode.Count, "") };
                repeatMode.SelectedIndex = 0;
            }
            finally { refreshing = false; }
            RefreshLabels();
            foreach (var name in new[] { "ScenarioSpawnArea", "ScenarioSpatialArea" })
            if (FindName(name) is ComboBox spawnAreas) { refreshing = true; try { spawnAreas.SelectedIndex = 0; } finally { refreshing = false; } }
            discard.IsEnabled = ownerCurrent();
            var missing = ScenarioControlPages.MissingRequirements(draft.Type, document);
            apply.IsEnabled = discard.IsEnabled && missing.Count == 0;
            if (missing.Count != 0)
            {
                statusKey = "ScenarioPage.Requires" + missing[0]; status.Text = LocalizationService.Get(statusKey);
            }
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { stale = true; statusKey = "AliasEvent.Failed"; status.Text = LocalizationService.Get("AliasEvent.Failed"); status.ToolTip = error.Message; apply.IsEnabled = false; }
    }
    private void UpdateSpawnCoordinates(ScenarioParameterDraft selected)
    {
        numbers[5].Text = unchecked((uint)int.Parse(selected.Values[Field(5)], CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);
        numbers[7].Text = unchecked((uint)int.Parse(selected.Values[Field(7)], CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);
    }
    public bool ApplySpawnPoint(TilePoint point)
    {
        if (closed || stale || !ownerCurrent() || draft.Type != "ACTION_ADDCREATURE") return false;
        try { UpdateSpawnCoordinates(document.UpdateScenarioSpawnPoint(draft, point)); return true; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { status.Text = LocalizationService.Get("AliasEvent.Failed"); status.ToolTip = error.Message; return false; }
    }
    public ScenarioSpawnFlagsWindow? CreateSpawnFlagsWindow()
    {
        if (closed || stale || !ownerCurrent() || draft.Type != "ACTION_ADDCREATURE") return null;
        if (spawnFlags is not null) return spawnFlags;
        var sourceDraft = draft;
        ScenarioParameterDraft privateDraft;
        try { privateDraft = document.UpdateScenarioControlPage(draft, [new(Field(4), numbers[4].Text)]); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { status.Text = LocalizationService.Get("AliasEvent.Failed"); status.ToolTip = error.Message; return null; }
        var bits = unchecked((uint)int.Parse(privateDraft.Values[Field(4)], CultureInfo.InvariantCulture));
        var child = new ScenarioSpawnFlagsWindow(bits, inputs =>
        {
            if (closed || stale || !ownerCurrent() || !ReferenceEquals(sourceDraft, draft)) throw new InvalidOperationException("Reopen creature options after the parent changes.");
            var selected = document.UpdateScenarioSpawnFlags(privateDraft, inputs);
            numbers[4].Text = unchecked((uint)int.Parse(selected.Values[Field(4)], CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);
        });
        spawnFlags = child; child.Closed += (_, _) => { if (ReferenceEquals(spawnFlags, child)) spawnFlags = null; }; return child;
    }
    public CameraPathWindow? CreateCameraLibraryWindow()
    {
        if (closed || stale || !ownerCurrent() || draft.Type != "ACTION_CAMERAPATH") return null;
        if (cameraLibrary is not null) return cameraLibrary;
        var sourceDraft = draft;
        var initial = uint.TryParse(numbers[1].Text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? (long)value : unchecked((uint)int.Parse(draft.Values[Field(1)], CultureInfo.InvariantCulture));
        var child = new CameraPathWindow(initial, choice =>
        {
            if (closed || stale || !ownerCurrent() || !ReferenceEquals(sourceDraft, draft))
                throw new InvalidOperationException("Reopen the camera library after its parent changes.");
            _ = document.UpdateScenarioCameraLibrary(sourceDraft, choice.ID);
            numbers[1].Text = choice.ID.ToString(CultureInfo.InvariantCulture);
            choices[3].SelectedItem = choices[3].Items.Cast<Choice>().Single(item => item.Value == choice.CeilingMode.ToString(CultureInfo.InvariantCulture));
        }, privateDraft: true);
        cameraLibrary = child;
        child.Closed += (_, _) => { if (ReferenceEquals(cameraLibrary, child)) cameraLibrary = null; };
        return child;
    }
    public bool ApplyDraft()
    {
        if (closed || stale || applying || !ownerCurrent() || ScenarioControlPages.MissingRequirements(draft.Type, document).Count != 0) return false;
        applying = true;
        try
        {
            var revision = inputRevision; var aliasRevision = aliasEditor?.InputRevision;
            var preparedAlias = aliasEditor?.PrepareDraft(); var inputs = new List<ScenarioParameterValue>();
            foreach (var parameter in changed)
            {
                var control = ScenarioControlPages.Controls(draft.Type).Single(item => item.Parameter == parameter);
                if (control.Kind == ScenarioControlKind.Element)
                {
                    var selected = ((Choice)choices[parameter].SelectedItem).Value;
                    if (selected == ChoiceValue(control)) continue;
                    var packed = uint.Parse(selected, NumberStyles.None, CultureInfo.InvariantCulture);
                    inputs.Add(new(Field(parameter), (packed >> 16).ToString(CultureInfo.InvariantCulture)));
                    inputs.Add(new(Field(control.SecondaryParameter), (packed & 65535).ToString(CultureInfo.InvariantCulture)));
                    continue;
                }
                var value = speech is not null ? speech.Input(parameter) : choices.TryGetValue(parameter, out var combo) ? ((Choice)combo.SelectedItem).Value
                    : times.TryGetValue(parameter, out var time) ? ScenarioControlPages.Duration(time.Hours.Text, time.Minutes.Text, time.Seconds.Text).ToString(CultureInfo.InvariantCulture)
                    : numbers[parameter].Text;
                if (control.DisplayOffset != 0)
                {
                    var display = uint.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
                    if (display > control.Maximum - control.DisplayOffset) throw new ArgumentOutOfRangeException(nameof(value));
                    value = checked(display + (uint)control.DisplayOffset).ToString(CultureInfo.InvariantCulture);
                }
                inputs.Add(new(Field(parameter), value));
            }
            if (repeatChanged) inputs.Add(new(draft.Fields.Single(field => field.Name is "Repeat" or "repeat").Name,
                ScenarioControlPages.Repeat(((RepeatChoice)repeatMode.SelectedItem).Mode, repeatCount.Text).ToString(CultureInfo.InvariantCulture)));
            var prepared = document.UpdateScenarioControlPage(draft, inputs);
            if(ScenarioTerrainOwners.TerrainParameter(draft.Type) is { } terrainParameter&&changed.Contains(terrainParameter))
                prepared=document.UpdateScenarioTerrainSelection(prepared,((Choice)choices[terrainParameter].SelectedItem).Value);
            if (gateChanged && gatePicker?.SelectedItem is GateItem { Key: not null } gate)
                prepared = document.UpdateScenarioGate(prepared, gate.Key);
            if (!commitFields() || closed || stale || !ownerCurrent() || inputRevision != revision || aliasEditor?.InputRevision != aliasRevision) return false;
            writing = true;
            try
            {
                if (preparedAlias is not null) aliasEditor!.ApplyParentDraft(prepared, preparedAlias, editorSave: !privateCreation);
                else if (privateCreation) document.ApplyScenarioParameterDraft(prepared);
                else document.ApplyScenarioEditorParameterDraft(prepared);
            }
            finally { writing = false; }
            ResetDraft(); statusKey = preparedAlias is null ? "ScenarioPage.Applied" : "AliasEvent.Applied"; status.Text = LocalizationService.Get(statusKey); return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { statusKey = "AliasEvent.Failed"; status.Text = LocalizationService.Get("AliasEvent.Failed"); status.ToolTip = error.Message; return false; }
        finally { applying = false; }
    }
    internal FrameworkElement TakeCreationContent()
    {
        if (!privateCreation) throw new InvalidOperationException("Only private creation pages can be embedded.");
        ((FrameworkElement)apply.Parent).Visibility = Visibility.Collapsed;
        var content = (FrameworkElement)Content; Content = null; return content;
    }
    private void RefreshGates()
    {
        if (gatePicker is null || closed || stale) return;
        var previous = gateChanged ? (gatePicker.SelectedItem as GateItem)?.Key : document.SelectedScenarioGate(draft);
        var items = document.ScenarioGateChoices().Select(item => new GateItem(item.Key,
            LocalizationService.Get("ScenarioSpatial.Gate") + " " + (item.ProjectID?.ToString(CultureInfo.InvariantCulture) ?? $"{item.WorkingID} ({item.X}, {item.Y})")
            + EntityReferences.DisplayDetails(item.Name, item.Info))).ToList();
        if (previous is null && (!privateCreation || privateEditing))
        {
            var stored = document.IsLegacyProject ? draft.Values["param5"]
                : $"({draft.Values["NativeArgument5"]}, {draft.Values["NativeArgument7"]})";
            items.Insert(0, new(null, LocalizationService.Get("ScenarioSpatial.UnknownGate") + " " + stored));
        }
        var wasRefreshing = refreshing;
        refreshing = true;
        try { gatePicker.ItemsSource = items; gatePicker.SelectedItem = items.FirstOrDefault(item => item.Key == previous) ?? items.FirstOrDefault(); }
        finally { refreshing = wasRefreshing; }
        if (previous is null && privateCreation && !privateEditing && items.Count > 0) gateChanged = true;
    }
    private void UpdateSpatialCoordinates(ScenarioParameterDraft selected)
    {
        foreach (var number in new[] { 5, 7 })
            numbers[number].Text = (long.Parse(selected.Values[Field(number)], CultureInfo.InvariantCulture) - 1).ToString(CultureInfo.InvariantCulture);
    }
    public bool ApplySpatialPoint(TilePoint point)
    {
        if (closed || stale || !ownerCurrent() || draft.Type is not ("ACTION_CHANGESQUARE" or "ACTION_CHANGEOWNER")) return false;
        try { UpdateSpatialCoordinates(document.UpdateScenarioSpatialPoint(draft, point)); return true; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { status.Text = LocalizationService.Get("AliasEvent.Failed"); status.ToolTip = error.Message; return false; }
    }
}
