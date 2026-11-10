using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class ObjectBatchWindow
{
    private IReadOnlyList<NativeRecord>? modificationSelection;
    private int modificationFamily;
    private bool modificationInvalidated;
    private readonly Dictionary<ObjectModificationMask, CheckBox> modificationMasks = [];
    private readonly ComboBox modificationCoordinates = new(), creatureBitMode = new(), heroBitMode = new();
    private readonly TextBox creatureMask = new() { Text = "4294967295", Width = 115 }, heroMask = new() { Text = "4294967295", Width = 115 };
    private readonly CheckBox modificationScripts = new(), detachScripts = new();
    private ObjectBatchModificationPlan? modificationPlan;
    private readonly List<ModificationRow> modificationRows = [];
    private bool IsModification => modificationSelection is not null;
    public ObjectBatchModificationPlan? ModificationPreview => modificationPlan;

    private void InitializeModificationSelection(IReadOnlyList<NativeRecord>? selected)
    {
        if (selected is null) return;
        var entries = document.PlanObjectRemoval(selected).Entries;
        if (entries.Count == 0 || entries.Select(entry => ObjectModificationRules.Family(entry.Thing.Kind)).Distinct().Count() != 1)
            throw new ArgumentException("Select objects from one manager family.");
        modificationSelection = entries.Select(entry => entry.Record).ToArray();
        modificationFamily = ObjectModificationRules.Family(entries[0].Thing.Kind);
        if (entries.Count == 1 && sourceModificationRequest is not {Count:>1})
        {
            var template = document.ObjectTemplateForModification(entries[0].Record);
            session.Templates[template.Kind] = new[] { template.TypeID.ToString(), template.Owner.ToString(), template.Health.ToString(), template.GoldHeld.ToString(),
                template.DataAll.ToString(), template.HeroOptions.ToString(), template.CreatureOptions.ToString(), template.Objective.ToString(), template.Area.ToString(), template.Enemy.ToString() };
        }
    }

    private void InitializeModificationControls(Panel controls)
    {
        SetResourceReference(TitleProperty, "Modify.Title");
        confirmed.SetResourceReference(ContentControl.ContentProperty, "Modify.Confirm");
        apply.SetResourceReference(ContentControl.ContentProperty, "Modify.Apply");
        // Keep the shared full template/EN-RU catalogues; hide addition-only controls.
        ((FrameworkElement)mode.Parent).Visibility = Visibility.Collapsed; wholeMap.Visibility = Visibility.Collapsed;
        ((FrameworkElement)width.Parent).Visibility = Visibility.Collapsed; ((FrameworkElement)height.Parent).Visibility = Visibility.Collapsed;
        ((FrameworkElement)((FrameworkElement)frequency.Parent).Parent).Visibility = Visibility.Collapsed;
        ((Button)FindName("BatchUseSelection")).Visibility = Visibility.Collapsed;
        var panel = new StackPanel(); controls.Children.Insert(0, panel);
        var hint = AddLabel(panel, "Modify.Hint"); hint.TextWrapping = TextWrapping.Wrap;
        var masks = new WrapPanel(); panel.Children.Add(masks);
        foreach (var mask in Enum.GetValues<ObjectModificationMask>().Where(mask => mask is not (ObjectModificationMask.None or ObjectModificationMask.All)))
        {
            if (modificationFamily == 0 ? mask == ObjectModificationMask.Data : mask is ObjectModificationMask.Level or ObjectModificationMask.GoldHeld or ObjectModificationMask.Health
                or ObjectModificationMask.CreatureOptions or ObjectModificationMask.HeroOptions or ObjectModificationMask.Objective) continue;
            var box = Named(new CheckBox { Margin = new(4) }, "ModifyMask" + mask);
            box.SetResourceReference(ContentControl.ContentProperty, "Modify.Mask." + mask); masks.Children.Add(box); modificationMasks.Add(mask, box);
            box.Checked += InputChanged; box.Unchecked += InputChanged;
        }
        var positions = new WrapPanel(); panel.Children.Add(positions);
        AddLabeled(positions, "Modify.Coordinates", Named(modificationCoordinates, "ModifyCoordinates")); modificationCoordinates.Width = 260;
        modificationCoordinates.IsEnabled = modificationFamily == 0;
        modificationCoordinates.SelectionChanged += InputChanged;
        if (modificationFamily == 0)
        {
            var bits = new WrapPanel(); panel.Children.Add(bits);
            AddLabeled(bits, "Modify.CreatureBits", Named(creatureBitMode, "ModifyCreatureBits")); creatureBitMode.Width = 190;
            AddLabeled(bits, "Modify.BitMask", Named(creatureMask, "ModifyCreatureMask"));
            AddLabeled(bits, "Modify.HeroBits", Named(heroBitMode, "ModifyHeroBits")); heroBitMode.Width = 190;
            AddLabeled(bits, "Modify.BitMask", Named(heroMask, "ModifyHeroMask"));
            creatureBitMode.SelectionChanged += InputChanged; heroBitMode.SelectionChanged += InputChanged;
            creatureMask.TextChanged += InputChanged; heroMask.TextChanged += InputChanged;
        }
        Named(modificationScripts, "ModifyIncludeScripted"); modificationScripts.SetResourceReference(ContentControl.ContentProperty, "Modify.IncludeScripted"); panel.Children.Add(modificationScripts);
        if(sourceUsageApproved){modificationScripts.IsChecked=true;modificationScripts.Visibility=Visibility.Collapsed;}
        modificationScripts.Checked += InputChanged; modificationScripts.Unchecked += InputChanged;
        Named(detachScripts, "ModifyDetachScripts"); var detachText = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxWidth = 600 };
        detachText.SetResourceReference(TextBlock.TextProperty, "Modify.DetachScripts"); detachScripts.Content = detachText; panel.Children.Add(detachScripts);
        if(sourceUsageApproved)
        {
            detachScripts.Visibility=Visibility.Collapsed;
            var retention=AddLabel(panel,"Modify.ScopeRetentionHint");retention.TextWrapping=TextWrapping.Wrap;
        }
        detachScripts.Checked += (_, _) => { confirmed.IsChecked = false; UpdateApply(); }; detachScripts.Unchecked += (_, _) => { confirmed.IsChecked = false; UpdateApply(); };
        // Warning flags are restored from session values; they never supply detachment consent.
        // Keep a real candidate table visible even with wrapped Russian options.
        var right = (Grid)controls.Parent; right.Children.Remove((UIElement)controls);
        var optionsScroll = new ScrollViewer { Content = controls, MaxHeight = 360, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        right.Children.Insert(0, optionsScroll); MinHeight = 650;
        preview.Columns.Clear();
        var selector = new FrameworkElementFactory(typeof(ComboBox)); selector.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(ModificationRow.Choices)));
        selector.SetValue(ComboBox.DisplayMemberPathProperty, "Label"); selector.SetValue(ComboBox.SelectedValuePathProperty, "Value");
        selector.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedValueProperty,
            new Binding(nameof(ModificationRow.Decision)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        preview.Columns.Add(new DataGridTemplateColumn { CellTemplate = new DataTemplate { VisualTree = selector }, Width = 140, MinWidth = 140 });
        foreach (var (property, width) in new[] { (nameof(ModificationRow.Source), 110d), (nameof(ModificationRow.Position), 90d),
            (nameof(ModificationRow.Value), 155d), (nameof(ModificationRow.Existing), 125d), (nameof(ModificationRow.Warnings), 230d) })
        {
            if (property == nameof(ModificationRow.Warnings))
            { preview.Columns.Add(new DataGridTemplateColumn { CellTemplate = OriginalEditorListImages.WarningTemplate(), IsReadOnly = true, MinWidth = width, Width = width }); continue; }
            var style = new Style(typeof(TextBlock)); style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            style.Setters.Add(new Setter(ToolTipProperty, new Binding(property)));
            preview.Columns.Add(new DataGridTextColumn { Binding = new Binding(property), ElementStyle = style, IsReadOnly = true, MinWidth = width, Width = width });
        }
        preview.CanUserSortColumns = false; preview.CanUserReorderColumns = false;
        ((Button)FindName("BatchSelectAll")).SetResourceReference(ContentControl.ContentProperty, "Modify.ModifyAll");
        ((Button)FindName("BatchKeepAll")).SetResourceReference(ContentControl.ContentProperty, "Modify.KeepAll");
    }

    private void RestoreModificationPreferences()
    {
        foreach (var pair in modificationMasks) pair.Value.IsChecked = modificationSession.Mask.HasFlag(pair.Key);
        modificationCoordinates.SelectedValue = (modificationFamily == 0 ? modificationSession.Coordinates : 1).ToString();
        creatureBitMode.SelectedValue = modificationSession.CreatureBits.ToString(); heroBitMode.SelectedValue = modificationSession.HeroBits.ToString();
        creatureMask.Text = modificationSession.CreatureMask; heroMask.Text = modificationSession.HeroMask;
    }
    private void SaveModificationPreferences()
    {
        foreach (var pair in modificationMasks)
            modificationSession.Mask = pair.Value.IsChecked == true ? modificationSession.Mask | pair.Key : modificationSession.Mask & ~pair.Key;
        // Noncreature forced-relative controls must not reset the hidden creature preference.
        if (modificationFamily == 0)
        {
            modificationSession.Coordinates = int.Parse(modificationCoordinates.SelectedValue as string ?? "1");
            modificationSession.CreatureBits = int.Parse(creatureBitMode.SelectedValue as string ?? "0");
            modificationSession.HeroBits = int.Parse(heroBitMode.SelectedValue as string ?? "0");
            modificationSession.CreatureMask = creatureMask.Text; modificationSession.HeroMask = heroMask.Text;
        }
    }

    private void RefreshModificationLanguage()
    {
        var coordinate = modificationCoordinates.SelectedValue as string ?? "1";
        Fill(modificationCoordinates, Enum.GetValues<ObjectModificationCoordinates>().Select(value => new Choice(((int)value).ToString(), LocalizationService.Get("Modify.Coordinates." + value))), coordinate);
        foreach (var picker in new[] { creatureBitMode, heroBitMode })
            Fill(picker, Enum.GetValues<ObjectModificationBits>().Select(value => new Choice(((int)value).ToString(), LocalizationService.Get("Modify.Bits." + value))), picker.SelectedValue as string ?? "0");
        var headers = new[] { "Modify.Decision", "Modify.Source", "Batch.Position", "Modify.Value", "Batch.Existing", "Batch.Warnings" };
        for (var index = 0; index < headers.Length; index++) preview.Columns[index].Header = new TextBlock { Text = LocalizationService.Get(headers[index]), TextWrapping = TextWrapping.Wrap, MaxWidth = preview.Columns[index].MinWidth > 0 ? preview.Columns[index].MinWidth - 10 : 125 };
        foreach (var row in modificationRows) row.RefreshLanguage();
    }

    private bool BuildModificationPreview(ObjectBatchTemplate template)
    {
        if (modificationInvalidated) throw new InvalidOperationException("The selected physical objects changed; reopen modification from the manager.");
        var mask = modificationMasks.Where(pair => pair.Value.IsChecked == true).Aggregate(ObjectModificationMask.None, (value, pair) => value | pair.Key);
        uint Bits(TextBox input) => uint.Parse(input.Text, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var options = new ObjectBatchModificationOptions(mask, new(int.Parse(x.Text, CultureInfo.InvariantCulture), int.Parse(y.Text, CultureInfo.InvariantCulture)),
            (ObjectModificationCoordinates)int.Parse(modificationCoordinates.SelectedValue as string ?? "1"),
            (ObjectModificationBits)int.Parse(creatureBitMode.SelectedValue as string ?? "0"), Bits(creatureMask),
            (ObjectModificationBits)int.Parse(heroBitMode.SelectedValue as string ?? "0"), Bits(heroMask), (ObjectReplacementPolicy)session.Policy,
            session.Flags, modificationScripts.IsChecked == true, upgrade.IsChecked == true);
        modificationPlan = sourceModificationRequest is null ? document.PlanObjectBatchModification(modificationSelection!, template, options)
            : document.PlanEditorObjectBatchModification(sourceModificationRequest,template,options);
        foreach (var candidate in modificationPlan.Candidates) modificationRows.Add(new(candidate, ModificationChoicesChanged, () => !updating && !selecting));
        preview.ItemsSource = modificationRows; RefreshSummary(); UpdateApply(); return true;
    }

    private void ModificationChoicesChanged()
    {
        if (updating || selecting || modificationPlan is null || closed) return;
        try
        {
            modificationPlan = document.ReviewObjectBatchModification(modificationPlan, modificationRows.Select(row => (ObjectModificationDecision)row.Decision));
            selecting = true;
            try { for (var index = 0; index < modificationRows.Count; index++) modificationRows[index].SetCandidate(modificationPlan.Candidates[index]); }
            finally { selecting = false; }
            confirmed.IsChecked = false; RefreshSummary(); UpdateApply();
        }
        catch (Exception exception) when (Expected(exception)) { InvalidatePreview(); Failure(exception); }
    }
    private void SelectModifications(bool modify)
    {
        selecting = true;
        try { foreach (var row in modificationRows) row.SetCandidate(row.Candidate with { Decision = modify && !row.Candidate.Locked ? ObjectModificationDecision.Modify : ObjectModificationDecision.Keep }); }
        finally { selecting = false; }
        ModificationChoicesChanged();
    }
    private void RefreshModificationSummary() => summary.Text = modificationPlan is null ? LocalizationService.Get("Batch.Draft")
        : string.Format(LocalizationService.Get("Modify.Summary"), modificationRows.Count, modificationRows.Count(row => row.Decision == 2),
            modificationRows.Count(row => row.Decision == 1), modificationRows.Count(row => row.Candidate.Locked), modificationRows.Count(row => row.Candidate.ChangesScriptScope && row.Candidate.Source.Scripted));
    private bool ApplyModificationPreview()
    {
        if (closed || modificationInvalidated || modificationPlan is null || !ManagerConfirmation) return false;
        try
        {
            var current = modificationPlan;
            if (!canApply() || closed || !ReferenceEquals(current, modificationPlan)) throw new InvalidOperationException("The document or pending properties changed; reopen the manager preview.");
            appliedCount = ApplyManagerCommand(()=>document.ApplyEditorObjectBatchModification(current, detachScripts.IsChecked == true));
            InvalidatePreview(); statusKey = "Modify.Applied"; RefreshStatus();if(managerFlow)Cancel(); return true;
        }
        catch (Exception exception) when (Expected(exception)) { confirmed.IsChecked = false; Failure(exception); UpdateApply(); return false; }
    }

    public sealed record ModificationChoice(int Value, string Label);
    public sealed class ModificationRow : INotifyPropertyChanged
    {
        private readonly Action changed;
        private readonly Func<bool> canChoose;
        private int decision;
        internal ModificationRow(ObjectBatchModificationCandidate candidate, Action changed, Func<bool> canChoose)
        { Candidate = candidate; decision = (int)candidate.Decision; this.changed = changed; this.canChoose = canChoose; }
        public ObjectBatchModificationCandidate Candidate { get; private set; }
        public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalEditorListImages.For(EditorListImageResources.ManagerProblem(Candidate.Locked, Candidate.Warnings, decision == (int)ObjectModificationDecision.Modify));
        public int Decision
        {
            get => decision;
            set
            {
                if (!canChoose() || value is < 0 or > 2 || value == 2 && Candidate.Locked || value == decision) return;
                decision = value; PropertyChanged?.Invoke(this, new(nameof(Decision))); PropertyChanged?.Invoke(this, new(nameof(Icon))); changed();
            }
        }
        public IReadOnlyList<ModificationChoice> Choices => Enum.GetValues<ObjectModificationDecision>()
            .Where(value => value != ObjectModificationDecision.Modify || !Candidate.Locked)
            .Select(value => new ModificationChoice((int)value, LocalizationService.Get("Modify.Decision." + value))).ToArray();
        public string Source => Candidate.Source.Name(LocalizationService.CurrentLanguage) + " · " + Candidate.Source.Thing.Position.X + ", " + Candidate.Source.Thing.Position.Y;
        public string Position => Candidate.Position.X + ", " + Candidate.Position.Y;
        public string Value => LocalizationService.Get("ProjectKind." + (Candidate.Template.Kind == PlacedThingKind.SpecialItem ? "MagicalObject" : Candidate.Template.Kind))
            + " #" + Candidate.Template.TypeID + " · " + LocalizationService.Get("Owner." + Candidate.Template.Owner);
        public string Existing => Candidate.Replacement?.Name(LocalizationService.CurrentLanguage) ?? "—";
        public string Warnings => Candidate.Warnings == 0 ? "—" : string.Join("; ", Enum.GetValues<ObjectBatchWarning>()
            .Where(warning => warning != 0 && Candidate.Warnings.HasFlag(warning)).Select(warning => LocalizationService.Get("Batch.Warning." + warning)));
        internal void SetCandidate(ObjectBatchModificationCandidate candidate) { Candidate = candidate; decision = (int)candidate.Decision; RefreshLanguage(); PropertyChanged?.Invoke(this, new(nameof(Decision))); }
        internal void RefreshLanguage() { foreach (var property in new[] { nameof(Choices), nameof(Source), nameof(Value), nameof(Existing), nameof(Warnings), nameof(Icon) }) PropertyChanged?.Invoke(this, new(property)); }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
