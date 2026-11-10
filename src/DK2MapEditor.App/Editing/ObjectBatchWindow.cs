using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class ObjectBatchWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly Func<bool> canApply;
    private readonly Func<TileRect?> selection;
    private readonly ObjectBatchSession session;
    private readonly ObjectManagerSession? managerSession;
    private readonly ObjectModificationSession modificationSession;
    private readonly EditorThingRequest? sourceModificationRequest;
    private bool sourceUsageApproved => sourceModificationRequest is not null;
    private readonly ComboBox family = new(), named = new(), mode = new(), policy = new();
    private readonly TextBox[] fields = Enumerable.Range(0, 10).Select(_ => new TextBox()).ToArray();
    private readonly TextBlock[] fieldLabels = Enumerable.Range(0, 10).Select(_ => new TextBlock()).ToArray();
    private readonly TextBox x = new(), y = new(), width = new(), height = new(), frequency = new();
    private readonly Slider frequencySlider = new() { Minimum = 2, Maximum = 32, IsSnapToTickEnabled = true, TickFrequency = 1 };
    private readonly CheckBox wholeMap = new(), upgrade = new(), confirmed = new();
    private readonly Dictionary<ObjectBatchFlags, CheckBox> flags = [];
    private readonly Dictionary<int, ComboBox> suggestions = [];
    private readonly Dictionary<(int Field, uint Mask), CheckBox> templateBits = [];
    private readonly Dictionary<int, Expander> bitGroups = [];
    private readonly int[] objectiveIDs = [11, 12, 13, 14, 17, 18, 19, 22, 23, 27];
    private IReadOnlyList<Choice> areaChoices = [];
    private readonly DataGrid preview = new() { AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false,
        EnableRowVirtualization = true, EnableColumnVirtualization = true, MinHeight = 180 };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock summary = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button apply = new();
    private readonly TextBlock flowStatus=new(){TextWrapping=TextWrapping.Wrap,Visibility=Visibility.Collapsed};
    private readonly List<BatchRow> rows = [];
    private PlacedThingKind currentKind;
    private bool updating = true, closed, selecting;
    private ObjectBatchInsertionPlan? plan;
    private string? statusKey;
    private int appliedCount;
    public ObjectBatchInsertionPlan? PreviewPlan => plan;
    internal EditorObjectManagerHistory? ManagerHistory {get;set;}
    private int ApplyManagerCommand(Func<int> command)
    {
        var revision=document.EditRevision;
        try{var count=ManagerHistory is {} history?history.Execute(command):command();if(managerFlow)ManagerCommandApplied=true;return count;}
        catch{if(managerFlow&&document.EditRevision!=revision)ManagerCommandApplied=true;throw;}
    }

    public ObjectBatchWindow(NativeMapDocument document, TilePoint position, Func<TileRect?> selection,
        Func<bool> canApply, ObjectBatchSession? session = null, PlacedThingKind initialKind = PlacedThingKind.Creature,
        IReadOnlyList<NativeRecord>? modificationSelection = null, bool singlePainting = false,
        ObjectManagerSession? managerSession = null, ObjectModificationSession? modificationSession = null, EditorThingRequest? sourceModificationRequest = null)
    {
        this.document = document; this.selection = selection; this.canApply = canApply;
        this.sourceModificationRequest = sourceModificationRequest;
        // Single-record property drafts must not overwrite the shared multi-edit/addition template.
        var multiple = sourceModificationRequest is not null ? sourceModificationRequest.Count>1 : modificationSelection is { Count: > 1 };
        this.modificationSession = multiple ? modificationSession ?? new() : new();
        this.session = modificationSelection is null ? session ?? new() : this.modificationSession.Template;
        this.managerSession = singlePainting || modificationSelection is not null && !multiple ? null : managerSession;
        isSinglePainting = singlePainting;
        if (singlePainting && modificationSelection is not null) throw new ArgumentException("Choose painting or modification, not both.");
        if (singlePainting) this.session.Mode = 0;
        InitializeModificationSelection(modificationSelection);
        RefreshAreas();
        currentKind = initialKind; this.session.LastKind = initialKind; this.managerSession?.ChooseKind(initialKind);
        NameScope.SetNameScope(this, new NameScope());
        Width = 1080; Height = 760; MinWidth = 850; MinHeight = 520; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(TitleProperty, "Batch.Title");
        SetResourceReference(ForegroundProperty, "Brush.Text"); SetResourceReference(BackgroundProperty, "Brush.Background");
        var outer = new DockPanel { Margin = new(12) };
        outer.SetResourceReference(Panel.BackgroundProperty, "Brush.Background");
        outer.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Brush.Text");
        var bottom = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        DockPanel.SetDock(bottom, Dock.Bottom); outer.Children.Add(bottom);
        Named(confirmed, "BatchConfirmed"); confirmed.SetResourceReference(ContentControl.ContentProperty, "Batch.Confirm");
        confirmed.Checked += (_, _) => UpdateApply(); confirmed.Unchecked += (_, _) => UpdateApply(); bottom.Children.Add(confirmed);
        Named(apply, "BatchApply"); apply.SetResourceReference(ContentControl.ContentProperty, "Batch.Apply"); apply.Click += (_, _) => ApplyPreview(); bottom.Children.Add(apply);
        var cancel = Button("BatchCancel", "Editor.Cancel", () => Cancel()); bottom.Children.Add(cancel);
        Named(flowStatus,"BatchFlowStatus");DockPanel.SetDock(flowStatus,Dock.Bottom);outer.Children.Add(flowStatus);
        var grid = Named(new Grid(),"BatchBody"); grid.ColumnDefinitions.Add(new() { Width = new(315) }); grid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); outer.Children.Add(grid);
        var left = new StackPanel { Margin = new(0, 0, 12, 0) };
        var scroll = new ScrollViewer { Content = left, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Named(scroll,"BatchTemplatePanel");grid.Children.Add(scroll);
        AddLabel(left, "Batch.Family"); Named(family, "BatchFamily"); left.Children.Add(family);
        Named(named, "BatchNamedType"); named.ItemTemplate = OriginalObjectImages.NamedTemplate(); left.Children.Add(named);
        left.Children.Add(Button("BatchImagePicker", "ObjectImages.Choose", () =>
        {
            var picker = CreateImagePicker(); if (picker is null) return; picker.Owner = this; picker.ShowDialog();
        }));
        var fieldNames = new[] { "TypeID", "Owner", "Health", "GoldHeld", "DataAll", "HeroOptions", "CreatureOptions", "Objective", "Area", "Enemy" };
        for (var index = 0; index < 10; index++)
        {
            fieldLabels[index].Margin = new(0, 5, 0, 1); fieldLabels[index].TextWrapping = TextWrapping.Wrap; left.Children.Add(fieldLabels[index]); Named(fields[index], "Batch" + fieldNames[index]); left.Children.Add(fields[index]);
            fields[index].TextChanged += (_, _) => { if (updating) return; SaveTemplate(); RefreshNamed(); RefreshFieldLabels(); InvalidatePreview(); };
            if (index is 1 or 4 or 7 or 8 or 9)
            {
                var fieldIndex = index; var picker = Named(new ComboBox(), "BatchSuggestion" + fieldNames[index]); suggestions.Add(index, picker); left.Children.Add(picker);
                picker.SelectionChanged += (_, _) => { if (!updating && picker.SelectedItem is Choice choice) fields[fieldIndex].Text = choice.Value; };
            }
            if (index is 5 or 6)
            {
                var fieldIndex = index; var boxes = new StackPanel(); var expander = new Expander { Content = boxes, Margin = new(0, 4, 0, 2) };
                expander.SetResourceReference(ForegroundProperty, "Brush.Text");
                var caption = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxWidth = 250 }; caption.SetResourceReference(TextBlock.TextProperty, "Batch.NamedFlags");
                caption.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text"); expander.Header = caption; left.Children.Add(expander); bitGroups.Add(index, expander);
                foreach (var mask in index == 5 ? new uint[] { 1, 2, 4, 8 } : [1u, 4u, 8u, 16u, 32u, 64u, 128u])
                {
                    var bit = mask; var box = Named(new CheckBox { Margin = new(3) }, "BatchBit" + index + "_" + mask);
                    box.SetResourceReference(ContentControl.ContentProperty, "Batch." + (index == 5 ? "HeroBit." : "CreatureBit.") + mask); boxes.Children.Add(box); templateBits.Add((index, mask), box);
                    void Toggle(object? sender, RoutedEventArgs e)
                    {
                        if (updating || !uint.TryParse(fields[fieldIndex].Text, out var value)) return;
                        fields[fieldIndex].Text = (box.IsChecked == true ? value | bit : value & ~bit).ToString(CultureInfo.InvariantCulture);
                    }
                    box.Checked += Toggle; box.Unchecked += Toggle;
                }
            }
        }
        var hint = AddLabel(left, "Batch.TemplateHint"); hint.TextWrapping = TextWrapping.Wrap; hint.Margin = new(0, 10, 0, 4);
        var right = Named(new Grid(),"BatchPreviewSide"); Grid.SetColumn(right, 1); grid.Children.Add(right);
        right.RowDefinitions.Add(new() { Height = GridLength.Auto }); right.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star) }); right.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var controls = Named(new StackPanel(),"BatchControls"); right.Children.Add(controls);
        var top = new WrapPanel(); controls.Children.Add(top);
        AddLabeled(top, "Batch.Mode", Named(mode, "BatchMode")); mode.Width = 205;
        AddLabeled(top, "Batch.Replacement", Named(policy, "BatchReplacement")); policy.Width = 230;
        Named(wholeMap, "BatchWholeMap"); wholeMap.SetResourceReference(ContentControl.ContentProperty, "Batch.WholeMap"); controls.Children.Add(wholeMap);
        var geometry = new WrapPanel(); controls.Children.Add(geometry);
        AddLabeled(geometry, "Editor.SelectionX", Named(x, "BatchX")); AddLabeled(geometry, "Editor.SelectionY", Named(y, "BatchY"));
        AddLabeled(geometry, "Field.Width", Named(width, "BatchWidth")); AddLabeled(geometry, "Field.Height", Named(height, "BatchHeight"));
        foreach (var input in new[] { x, y, width, height }) input.Width = 85;
        geometry.Children.Add(Button("BatchUseSelection", "Batch.UseSelection", UseSelection));
        var random = new WrapPanel(); controls.Children.Add(random); AddLabeled(random, "Batch.Frequency", Named(frequency, "BatchFrequency")); frequency.Width = 65;
        Named(frequencySlider, "BatchFrequencySlider"); frequencySlider.Width = 230; frequencySlider.VerticalAlignment = VerticalAlignment.Center; random.Children.Add(frequencySlider);
        var warningFlags = new WrapPanel(); controls.Children.Add(warningFlags);
        foreach (var flag in new[] { ObjectBatchFlags.AskOptions, ObjectBatchFlags.Preview, ObjectBatchFlags.IgnoreTerrain, ObjectBatchFlags.IgnoreFatals,
            ObjectBatchFlags.IgnoreEnemies, ObjectBatchFlags.IgnoreIncompatible, ObjectBatchFlags.IgnoreAsk })
        {
            var box = Named(new CheckBox { Margin = new(4) }, "BatchFlag" + flag); box.SetResourceReference(ContentControl.ContentProperty, "Batch.Flag." + flag);
            flags.Add(flag, box); warningFlags.Children.Add(box); box.Checked += InputChanged; box.Unchecked += InputChanged;
        }
        Named(upgrade, "BatchUpgrade"); upgrade.SetResourceReference(ContentControl.ContentProperty, "Batch.Upgrade");
        upgrade.Visibility = document.IsLegacyProject && document.Files.Any(file => file.Signature == NativeMapDocument.LegacyThings && file.Data.Span[4] != 6) ? Visibility.Visible : Visibility.Collapsed;
        controls.Children.Add(upgrade); upgrade.Checked += InputChanged; upgrade.Unchecked += InputChanged;
        var buttons = Named(new WrapPanel(),"BatchSelectionCommands"); controls.Children.Add(buttons);
        buttons.Children.Add(Button("BatchPreview", "Batch.Preview", () => BuildPreview()));
        buttons.Children.Add(Button("BatchSelectAll", "Batch.SelectAll", () => SelectCandidates(true)));
        buttons.Children.Add(Button("BatchKeepAll", "Batch.KeepAll", () => SelectCandidates(false)));
        Named(summary, "BatchSummary"); controls.Children.Add(summary);
        Named(preview, "BatchCandidates"); Grid.SetRow(preview, 1); right.Children.Add(preview);
        var check = new FrameworkElementFactory(typeof(CheckBox)); check.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
            new Binding(nameof(BatchRow.Add)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        check.SetBinding(IsEnabledProperty, new Binding(nameof(BatchRow.CanAdd)));
        preview.Columns.Add(new DataGridTemplateColumn { CellTemplate = new DataTemplate { VisualTree = check }, Width = 55 });
        foreach (var property in new[] { nameof(BatchRow.Position), nameof(BatchRow.Owner), nameof(BatchRow.Existing), nameof(BatchRow.Warnings) })
        {
            if (property == nameof(BatchRow.Warnings))
            { preview.Columns.Add(new DataGridTemplateColumn { CellTemplate = OriginalEditorListImages.WarningTemplate(), IsReadOnly = true, MinWidth = 180, Width = new(1, DataGridLengthUnitType.Star) }); continue; }
            var style = new Style(typeof(TextBlock)); style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            style.Setters.Add(new Setter(ToolTipProperty, new Binding(property)));
            preview.Columns.Add(new DataGridTextColumn { Binding = new Binding(property), ElementStyle = style, IsReadOnly = true,
                MinWidth = property == nameof(BatchRow.Warnings) ? 180 : property == nameof(BatchRow.Position) ? 70 : 105,
                Width = property == nameof(BatchRow.Warnings) ? new(1, DataGridLengthUnitType.Star) : DataGridLength.Auto });
        }
        Named(status, "BatchStatus"); status.Margin = new(0, 8, 0, 4); Grid.SetRow(status, 2); right.Children.Add(status);
        if (IsModification) InitializeModificationControls(controls);
        if (isSinglePainting) InitializePaintingControls(controls, bottom);
        Content = outer;
        foreach (var input in new[] { x, y, width, height }) input.TextChanged += InputChanged;
        mode.SelectionChanged += InputChanged; policy.SelectionChanged += InputChanged; wholeMap.Checked += InputChanged; wholeMap.Unchecked += InputChanged;
        frequency.TextChanged += (_, _) =>
        {
            if (updating) return;
            if (int.TryParse(frequency.Text, out var number) && number is >= 2 and <= 32)
            { updating = true; frequencySlider.Value = number; updating = false; }
            InputChanged(null, null!);
        };
        frequencySlider.ValueChanged += (_, _) => { if (!updating) frequency.Text = ((int)frequencySlider.Value).ToString(CultureInfo.InvariantCulture); };
        family.SelectionChanged += (_, _) => { if (updating || !int.TryParse(family.SelectedValue as string, out var selected)) return; SaveTemplate(); currentKind = (PlacedThingKind)selected; this.session.LastKind = currentKind; this.managerSession?.ChooseKind(currentKind); LoadTemplate(); RefreshNamed(); InvalidatePreview(); };
        named.SelectionChanged += (_, _) => { if (!updating && named.SelectedItem is Choice choice) fields[0].Text = choice.Value; };
        var rectangle = selection() ?? new TileRect(position.X, position.Y, 1, 1);
        x.Text = rectangle.X.ToString(); y.Text = rectangle.Y.ToString(); width.Text = rectangle.Width.ToString(); height.Text = rectangle.Height.ToString();
        if (this.session.Mode == 0) { x.Text = position.X.ToString(); y.Text = position.Y.ToString(); }
        if (this.managerSession is { } preferences)
        { preferences.BoundPosition(document.Width, document.Height); x.Text = preferences.PositionX; y.Text = preferences.PositionY; }
        frequency.Text = this.session.Frequency.ToString(); frequencySlider.Value = this.session.Frequency;
        wholeMap.IsChecked = this.session.WholeMap;
        foreach (var pair in flags) pair.Value.IsChecked = this.session.Flags.HasFlag(pair.Key);
        RefreshLanguage(); LoadTemplate(); RefreshNamed();
        if (IsModification) RestoreModificationPreferences();
        updating = false; RefreshGeometry(); UpdateApply();
        document.Changed += DocumentChanged; LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) => Release();
    }

    private T Named<T>(T control, string name) where T : FrameworkElement { control.Name = name; RegisterName(name, control); return control; }
    private static TextBlock AddLabel(Panel panel, string key)
    {
        var label = new TextBlock { Margin = new(4), VerticalAlignment = VerticalAlignment.Center }; label.SetResourceReference(TextBlock.TextProperty, key); panel.Children.Add(label); return label;
    }
    private static void AddLabeled(Panel panel, string key, FrameworkElement control)
    {
        var group = new StackPanel { Margin = new(4) }; AddLabel(group, key); group.Children.Add(control); panel.Children.Add(group);
    }
    private Button Button(string name, string key, Action action)
    {
        var button = Named(new Button(), name); button.SetResourceReference(ContentControl.ContentProperty, key); button.Click += (_, _) => action(); return button;
    }
    private sealed record Choice(string Value, string Label, System.Windows.Media.Imaging.BitmapSource? Icon = null);
    private void Fill(ComboBox picker, IEnumerable<Choice> choices, string value)
    {
        if (picker.ItemTemplate is null) picker.DisplayMemberPath = "Label";
        picker.SelectedValuePath = "Value"; picker.ItemsSource = choices.ToArray(); picker.SelectedValue = value;
    }
    private void LanguageChanged(object? sender, EventArgs e) => RefreshLanguage();
    public void RefreshLanguage()
    {
        var wasUpdating = updating; updating = true;
        try
        {
            Fill(family, Enum.GetValues<PlacedThingKind>().Where(kind => (brushTemplateKind is null||kind==brushTemplateKind)&&(!IsModification || ObjectModificationRules.Family(kind) == modificationFamily))
                .Select(kind => new Choice(((int)kind).ToString(), LocalizationService.Get("ProjectKind." + (kind == PlacedThingKind.SpecialItem ? "MagicalObject" : kind)))), ((int)currentKind).ToString());
            // Selection values are strings in these three named controls.
            Fill(mode, Enum.GetValues<ObjectBatchMode>().Select(value => new Choice(((int)value).ToString(), LocalizationService.Get("Batch.Mode." + value))), session.Mode.ToString());
            Fill(policy, Enum.GetValues<ObjectReplacementPolicy>().Select(value => new Choice(((int)value).ToString(), LocalizationService.Get("Batch.Policy." + value))), session.Policy.ToString());
            RefreshNamed(); RefreshFieldLabels();
            var headers = new[] { "Batch.Add", "Batch.Position", "Field.Owner", "Batch.Existing", "Batch.Warnings" };
            if (!IsModification) for (var index = 0; index < headers.Length; index++) preview.Columns[index].Header = LocalizationService.Get(headers[index]);
            else RefreshModificationLanguage();
            foreach (var row in rows) row.RefreshLanguage(); RefreshSummary(); RefreshStatus();
        }
        finally { updating = wasUpdating; }
    }
    private void RefreshAreas() => areaChoices = document.PatrolAreas().Select(area => new Choice(area.ID.ToString(),
        area.ID + (area.Region is { } region ? " · " + region.Bounds.X + ", " + region.Bounds.Y : ""))).DistinctBy(choice => choice.Value).ToArray();
    private void RefreshNamed()
    {
        var previous = updating; updating = true;
        try
        {
            Fill(named, ValueCatalogues.ForTemplate(currentKind == PlacedThingKind.SpecialItem ? "MagicalObject" : currentKind.ToString(), ValueCatalogues.GameVersion(document))
                .Select(choice => new Choice(choice.Value, choice.Value + " · " + choice.Name(LocalizationService.CurrentLanguage),
                    OriginalObjectImages.For(currentKind, uint.Parse(choice.Value), ValueCatalogues.GameVersion(document)))), fields[0].Text);
            var owners = ValueCatalogues.Owners.Select(choice => new Choice(choice.Value, choice.Value + " · " + choice.Name(LocalizationService.CurrentLanguage))).ToArray();
            foreach (var (index, picker) in suggestions)
            {
                IEnumerable<Choice> choices = index switch
                {
                    1 => new[] { new Choice("0", LocalizationService.Get("Batch.AutoOwner")) }.Concat(owners),
                    9 => new[] { new Choice("0", LocalizationService.Get("Batch.Unspecified")) }.Concat(owners),
                    7 => objectiveIDs.Select(id => new Choice(id.ToString(), LocalizationService.Get("Batch.Objective." + id))),
                    8 => areaChoices,
                    4 when currentKind == PlacedThingKind.Creature => Enumerable.Range(1, 10).Select(level => new Choice(level.ToString(), LocalizationService.Get("Field.Level") + " " + level)),
                    4 when currentKind == PlacedThingKind.Door => new[] { new Choice("0", LocalizationService.Get("Batch.Unlocked")), new Choice("1", LocalizationService.Get("Field.Locked")), new Choice("2", LocalizationService.Get("Batch.Blueprint")) },
                    4 when currentKind == PlacedThingKind.Trap => new[] { new Choice("0", LocalizationService.Get("Batch.Blueprint")), new Choice("4", LocalizationService.Get("Batch.Built")) },
                    4 when currentKind == PlacedThingKind.Object && fields[0].Text == "4" => ValueCatalogues.Elements("Spells").Select(choice => new Choice(choice.Value, choice.Value + " · " + choice.Name(LocalizationService.CurrentLanguage))),
                    _ => []
                };
                Fill(picker, choices, fields[index].Text);
                var relevant = index == 1 || index == 4 && currentKind != PlacedThingKind.SpecialItem || currentKind == PlacedThingKind.Creature;
                picker.Visibility = picker.Items.Count == 0 || !relevant ? Visibility.Collapsed : Visibility.Visible; picker.IsEnabled = relevant;
            }
            foreach (var ((index, mask), box) in templateBits)
            {
                box.IsEnabled = (currentKind == PlacedThingKind.Creature || isSinglePainting && index == 5) && uint.TryParse(fields[index].Text, out _);
                box.IsChecked = uint.TryParse(fields[index].Text, out var value) && (value & mask) != 0;
            }
        }
        finally { updating = previous; }
    }
    private void RefreshFieldLabels()
    {
        var data = PlacedThingCaptionRules.DataKey(currentKind,uint.TryParse(fields[0].Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var typeID)?typeID:null);
        var keys = new[] { "Batch.TypeID", "Batch.Owner", "Field.Health", "Field.GoldHeld", data, "Batch.HeroOptions", "Field.Behavior", "Field.Objective", "Field.Area", "Field.Enemy" };
        for (var index = 0; index < 10; index++)
        {
            fieldLabels[index].Text = LocalizationService.Get(keys[index]);
            fields[index].IsEnabled = index < 2 || index == 4 && currentKind != PlacedThingKind.SpecialItem || currentKind == PlacedThingKind.Creature || isSinglePainting && index == 5;
            var visible = fields[index].IsEnabled ? Visibility.Visible : Visibility.Collapsed;
            fieldLabels[index].Visibility = fields[index].Visibility = visible;
            if (suggestions.TryGetValue(index, out var picker) && visible == Visibility.Collapsed) picker.Visibility = visible;
            if (bitGroups.TryGetValue(index, out var group)) group.Visibility = visible;
        }
    }
    private void SaveTemplate() => session.Templates[currentKind] = fields.Take(10).Select(field => field.Text).ToArray();
    private void LoadTemplate()
    {
        var previous = updating; updating = true;
        try
        {
            if (!session.Templates.TryGetValue(currentKind, out var values))
            {
                var value = ObjectBatchTemplate.Default(currentKind);
                values = new[] { value.TypeID.ToString(), value.Owner.ToString(), value.Health.ToString(), value.GoldHeld.ToString(), value.DataAll.ToString(),
                    value.HeroOptions.ToString(), value.CreatureOptions.ToString(), value.Objective.ToString(), value.Area.ToString(), value.Enemy.ToString() };
            }
            for (var index = 0; index < 10; index++) fields[index].Text = values[index]; RefreshFieldLabels(); RefreshNamed();
        }
        finally { updating = previous; }
    }
    private void InputChanged(object? sender, RoutedEventArgs e)
    {
        if (updating || closed) return;
        session.Mode = int.Parse(mode.SelectedValue as string ?? "0"); session.Policy = int.Parse(policy.SelectedValue as string ?? "4");
        session.WholeMap = wholeMap.IsChecked == true;
        if (int.TryParse(frequency.Text, out var number) && number is >= 2 and <= 32) session.Frequency = number;
        session.Flags = ObjectBatchFlags.None;
        foreach (var pair in flags) if (pair.Value.IsChecked == true) session.Flags |= pair.Key;
        if (managerSession is { } preferences && (!managerPlacementBounds||session.Mode==0)) { preferences.PositionX = x.Text; preferences.PositionY = y.Text; }
        if (IsModification) SaveModificationPreferences();
        RefreshGeometry(); InvalidatePreview();
    }
    private void RefreshGeometry()
    {
        var rectangle = !IsModification && session.Mode != 0;
        wholeMap.IsEnabled = rectangle; width.IsEnabled = height.IsEnabled = rectangle && !session.WholeMap;
        x.IsEnabled = y.IsEnabled = !rectangle || !session.WholeMap;
        frequency.IsEnabled = frequencySlider.IsEnabled = session.Mode == 2;
    }
    private void UseSelection()
    {
        if (selection() is not { } rectangle) { statusKey = "Batch.NoSelection"; RefreshStatus(); return; }
        x.Text = rectangle.X.ToString(); y.Text = rectangle.Y.ToString(); width.Text = rectangle.Width.ToString(); height.Text = rectangle.Height.ToString(); wholeMap.IsChecked = false;
    }
    private void DocumentChanged(object? sender, EventArgs e) { imagePicker?.Cancel(); if (IsModification) modificationInvalidated = true; InvalidatePreview(); RefreshAreas(); RefreshNamed(); }
    private void InvalidatePreview()
    {
        if(!closed)ManagerCommandFailed=false;
        plan = null; paintingPlan = null; modificationPlan = null; modificationRows.Clear(); rows.Clear(); preview.ItemsSource = null; confirmed.IsChecked = false; statusKey = null; status.ToolTip = null; RefreshStatus(); RefreshSummary(); UpdateApply();
    }
    public bool BuildPreview(Func<int, int>? randomSample = null)
    {
        if (closed||managerTemplateOnly||brushTemplateKind is not null) return false;
        InvalidatePreview();
        try
        {
            uint U(int index) => uint.Parse(fields[index].Text, NumberStyles.Integer, CultureInfo.InvariantCulture);
            int I(TextBox field) => int.Parse(field.Text, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var template = new ObjectBatchTemplate(currentKind, U(0), checked((byte)U(1)), U(2), U(3), I(fields[4]), U(5), U(6), U(7), U(8), U(9));
            var options = new ObjectBatchPlacementOptions((ObjectBatchMode)session.Mode,
                session.Mode == 0 ? new(I(x), I(y)) : default, session.WholeMap,
                session.Mode != 0 && !session.WholeMap ? new(I(x), I(y), I(width), I(height)) : null,
                session.Mode == 2 ? I(frequency) : session.Frequency, (ObjectReplacementPolicy)session.Policy, session.Flags);
            if (IsModification) return BuildModificationPreview(template);
            if (isSinglePainting) return BuildPaintingPreview(template, new(I(x), I(y)));
            plan = document.PlanObjectBatchInsertion(template, options, upgrade.IsChecked == true, randomSample);
            foreach (var candidate in plan.Placement.Candidates) rows.Add(new(candidate, () => { if (selecting) return; confirmed.IsChecked = false; RefreshSummary(); UpdateApply(); }));
            preview.ItemsSource = rows; RefreshSummary(); UpdateApply(); return true;
        }
        catch (Exception exception) when (Expected(exception)) { Failure(exception); return false; }
    }
    public bool ApplyPreview()
    {
        if(brushTemplateKind is not null)return AcceptBrushTemplate();
        if(managerTemplateOnly)return false;
        if(managerSingleEditor)
        {
            if(!BuildPreview())return false;
            if(RequiresManagerPreview){ConfigureManagerPreview();return false;}
            managerDirectApply=true;try{return ApplyModificationPreview();}finally{managerDirectApply=false;}
        }
        if (IsModification) return ApplyModificationPreview();
        if (isSinglePainting) return ApplyPaintingPreview();
        if (closed || plan is null || !ManagerConfirmation) return false;
        try
        {
            var current = plan; var selected = rows.Where(row => row.Add).Select(row => row.Candidate.Index).ToArray();
            if (!canApply() || plan != current) throw new InvalidOperationException("The document or pending properties changed; rebuild the preview.");
            appliedCount = ApplyManagerCommand(()=>document.ApplyEditorObjectBatchInsertion(current, selected)); InvalidatePreview(); statusKey = "Batch.Applied"; RefreshStatus();if(managerFlow)Cancel(); return true;
        }
        catch (Exception exception) when (Expected(exception)) { InvalidatePreview(); Failure(exception); return false; }
    }
    private static bool Expected(Exception exception) => exception is ArgumentException or InvalidOperationException or System.IO.IOException or OverflowException or FormatException;
    private void Failure(Exception exception) { ManagerCommandFailed=true;statusKey = managerFlow&&ManagerCommandApplied?"Batch.CommittedFailure":"Batch.Failed"; status.ToolTip = exception.Message; RefreshStatus();if(managerFlow&&ManagerCommandApplied)Cancel(); }
    private void RefreshStatus()
    {
        status.Text=flowStatus.Text=statusKey is null ? "" : statusKey is "Batch.Applied" or "Modify.Applied" or "Single.Applied"
            ? string.Format(LocalizationService.Get(statusKey), appliedCount) : LocalizationService.Get(statusKey);
        flowStatus.ToolTip=status.ToolTip;
    }
    private void SelectCandidates(bool add)
    {
        if (IsModification) { SelectModifications(add); return; }
        selecting = true; try { foreach (var row in rows) row.Add = add; } finally { selecting = false; }
        confirmed.IsChecked = false; RefreshSummary(); UpdateApply();
    }
    private void RefreshSummary()
    {
        if (IsModification) { RefreshModificationSummary(); return; }
        if (isSinglePainting) { RefreshPaintingSummary(); return; }
        summary.Text = plan is null ? LocalizationService.Get("Batch.Draft") : string.Format(LocalizationService.Get("Batch.Summary"),
            plan.Placement.SampledCells, rows.Count, rows.Count(row => row.Add), rows.Count(row => !row.CanAdd));
    }
    private void UpdateApply() => apply.IsEnabled = !closed && !managerTemplateOnly && (brushTemplateKind is not null || managerSingleEditor&&!modificationInvalidated || managerPreview&&(IsModification?modificationPlan is not null&&!modificationInvalidated:plan is not null) || ManagerConfirmation && (IsModification
        ? !modificationInvalidated && modificationPlan is not null && modificationRows.Any(row => row.Decision != (int)ObjectModificationDecision.Keep)
        : isSinglePainting ? paintingPlan is { Locked: false } && rows.Any(row => row.Add) : plan is not null && rows.Any(row => row.Add)));
    public void Cancel() { Release(); Close(); }
    private void Release()
    {
        if (closed) return; SaveTemplate(); closed = true; imagePicker?.Cancel(); document.Changed -= DocumentChanged; LocalizationService.LanguageChanged -= LanguageChanged; InvalidatePreview();
    }
    public sealed class BatchRow : INotifyPropertyChanged
    {
        private bool add;
        private readonly Action changed;
        public BatchRow(ObjectBatchPlacementCandidate candidate, Action changed) { Candidate = candidate; add = candidate.DefaultAdd; this.changed = changed; }
        public ObjectBatchPlacementCandidate Candidate { get; }
        public bool CanAdd => !Candidate.Locked;
        public bool Add { get => add; set { var next = value && CanAdd; if (next == add) return; add = next; PropertyChanged?.Invoke(this, new(nameof(Add))); PropertyChanged?.Invoke(this, new(nameof(Icon))); changed(); } }
        public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalEditorListImages.For(EditorListImageResources.ManagerProblem(Candidate.Locked, Candidate.Warnings, Add));
        public string Position => $"{Candidate.Position.X}, {Candidate.Position.Y}";
        public string Owner => LocalizationService.Get("Owner." + Candidate.Owner);
        public string Existing => Candidate.Replacement is not { } old ? "—" : LocalizationService.Get("ProjectKind." + (old.Kind == PlacedThingKind.SpecialItem ? "MagicalObject" : old.Kind)) + " · " + old.TypeID;
        public string Warnings => Candidate.Warnings == 0 ? "—" : string.Join("; ", Enum.GetValues<ObjectBatchWarning>().Where(warning => warning != 0 && Candidate.Warnings.HasFlag(warning)).Select(warning => LocalizationService.Get("Batch.Warning." + warning)));
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void RefreshLanguage() { foreach (var name in new[] { nameof(Owner), nameof(Existing), nameof(Warnings) }) PropertyChanged?.Invoke(this, new(name)); }
    }
}
