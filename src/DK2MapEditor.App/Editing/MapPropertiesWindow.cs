using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class MapPropertiesWindow : Window
{
    private readonly NativeMapDocument source;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly GameResourceSettings resources;
    private readonly EditorRequestPreferences requestPreferences;
    private readonly EditorLargeMapPolicy largeMapPolicy;
    private LargeMapWarningWindow? largeMapWindow;
    private readonly EditorMapNamePolicy mapNamePolicy;
    private MapNameWarningWindow? mapNameWindow;
    private MapPropertiesSession session;
    private readonly TextBox name = new(), width = new(), height = new();
    private readonly ComboBox players = new(), version = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(4) }, fill = new();
    private readonly Button apply = new();
    private readonly RadioButton[] anchors = new RadioButton[9];
    private readonly ResizeAnchorImages anchorImages=new();
    private readonly ContentControl childHost = new();
    private EditorWorkspace child = null!;
    private MapLevelOptionsWindow? levelWindow;
    private MapSpeechTextWindow? speechWindow;
    private MapCreatureModifiersWindow? modifiersWindow;
    private CreatureLevelModifierWindow? creatureLevelsWindow;
    private CreatureLevelModifierCatalogue? creatureLevelCatalogue;
    private bool closed, stale, applying, updating;
    private long inputRevision;
    private string? statusKey;
    private sealed record Choice(string Value, string Label);
    public NativeMapDocument WorkingDocument => session.WorkingDocument;
    public EditorWorkspace ChildWorkspace => child;
    public Func<MapPropertiesPlan, bool>? LargeMapPrompt { get; set; }
    public Action<LargeMapWarningWindow>? LargeMapPresenter {get;set;}
    public Action<MapNameWarningWindow>? MapNamePresenter {get;set;}
    public Func<MapPropertiesPlan, bool>? CropPrompt { get; set; }
    public Action<MapLevelOptionsWindow>? LevelOptionsPresenter { get; set; }
    public Action<MapSpeechTextWindow>? SpeechTextPresenter { get; set; }
    public Action<MapCreatureModifiersWindow>? CreatureModifiersPresenter { get; set; }
    public Action<CreatureLevelModifierWindow>? CreatureLevelsPresenter {get;set;}

    public MapPropertiesWindow(NativeMapDocument source, Func<bool> ownerCurrent, Func<bool> commitFields, GameResourceSettings resources,EditorRequestPreferences? requestPreferences=null)
    {
        this.source = source; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields; this.resources = resources;
        this.requestPreferences=requestPreferences??new();largeMapPolicy=new(this.requestPreferences);mapNamePolicy=new(this.requestPreferences);
        session = source.CreateMapPropertiesSession();
        NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(TitleProperty, "MapProperties.Title"); SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 1080; Height = 800; MinWidth = 850; MinHeight = 650; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel;
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top); panel.Children.Add(top);
        var hint = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4) }; hint.SetResourceReference(TextBlock.TextProperty, "MapProperties.Hint"); top.Children.Add(hint);
        var general = new WrapPanel(); top.Children.Add(general);
        AddField(general, "Name", name, 330); AddField(general, "Players", players, 140); AddField(general, "Version", version, 200);
        var sizing = new WrapPanel { Margin = new(0, 8, 0, 0) }; top.Children.Add(sizing);
        AddField(sizing, "Width", width, 120); AddField(sizing, "Height", height, 120);
        var anchorPanel = new StackPanel { Margin = new(10, 0, 10, 0) };
        var anchorLabel = new TextBlock(); anchorLabel.SetResourceReference(TextBlock.TextProperty, "MapProperties.Anchor"); anchorPanel.Children.Add(anchorLabel);
        var anchorHost=new Grid{Width=144,Height=144,Margin=new(0,4,0,0)};anchorPanel.Children.Add(anchorHost);
        var anchorGrid = new UniformGrid { Columns = 3, Rows = 3 }; anchorHost.Children.Add(anchorGrid); sizing.Children.Add(anchorPanel);
        var anchorStyle=ResizeAnchorImages.ButtonStyle();
        for (var index = 0; index < 9; index++)
        {
            var anchor = new RadioButton { Style=anchorStyle, GroupName = "MapPropertiesAnchor", IsChecked = index == 0 };
            anchor.SetResourceReference(System.Windows.Automation.AutomationProperties.NameProperty,"MapProperties.Anchor"+index);
            anchor.SetResourceReference(ToolTipProperty, "MapProperties.Anchor" + index); RegisterName("MapPropertiesAnchor" + index, anchor); anchors[index] = anchor; anchorGrid.Children.Add(anchor);
            anchor.Checked += (_, _) => InputChanged();
        }
        anchorHost.Children.Add(anchorImages);
        var sizeActions = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        sizeActions.Children.Add(Button("MapPropertiesResetSize", "ResetSize", ResetSize));
        fill.Margin = new(4); sizeActions.Children.Add(fill); sizing.Children.Add(sizeActions);
        var children = new WrapPanel { Margin = new(0, 8, 0, 4) }; top.Children.Add(children);
        children.Children.Add(Button("MapPropertiesLevelOptions", "LevelOptions", OpenLevelOptions)); children.Children.Add(Button("MapPropertiesSpeechText", "SpeechText", OpenSpeechText));
        var modifiers=new Button();modifiers.SetResourceReference(ContentControl.ContentProperty,"CreatureModifiers.Open");RegisterName("MapPropertiesCreatureModifiers",modifiers);modifiers.Click+=(_,_)=>OpenCreatureModifiers();children.Children.Add(modifiers);
        var levels=new Button();levels.SetResourceReference(ContentControl.ContentProperty,"CreatureLevel.Open");RegisterName("MapPropertiesCreatureLevels",levels);levels.Click+=(_,_)=>OpenCreatureLevels();children.Children.Add(levels);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status); RegisterName("MapPropertiesStatus", status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right }; footer.Children.Add(buttons);
        apply.SetResourceReference(ContentControl.ContentProperty, "MapProperties.Apply"); RegisterName("MapPropertiesApply", apply); apply.Click += (_, _) => ApplyProperties(); buttons.Children.Add(apply);
        buttons.Children.Add(Button("MapPropertiesReset", "Reset", ResetDraft)); var cancel = Button("MapPropertiesCancel", "Cancel", Close); cancel.IsCancel = true; buttons.Children.Add(cancel);
        var resetLarge=new Button();resetLarge.SetResourceReference(ContentControl.ContentProperty,"LargeMap.Reset");RegisterName("MapPropertiesLargeReset",resetLarge);
        resetLarge.Click+=(_,_)=>{try{CloseLargeMapWindow();largeMapPolicy.ResetSuppression();}catch(InvalidOperationException){statusKey="Changed";UpdateStatus();}};buttons.Children.Add(resetLarge);
        var resetName=new Button();resetName.SetResourceReference(ContentControl.ContentProperty,"MapName.Reset");RegisterName("MapPropertiesNameReset",resetName);
        resetName.Click+=(_,_)=>{try{CloseMapNameWindow();mapNamePolicy.ResetSuppression();}catch(InvalidOperationException){statusKey="Changed";UpdateStatus();}};buttons.Children.Add(resetName);
        panel.Children.Add(childHost);
        name.TextChanged += (_, _) => InputChanged(); width.TextChanged += (_, _) => InputChanged(); height.TextChanged += (_, _) => InputChanged();
        players.SelectionChanged += (_, _) => InputChanged(); version.SelectionChanged += (_, _) => InputChanged();
        source.Changed += SourceChanged; LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) => { closed = true; CloseMapNameWindow();mapNamePolicy.Dispose();CloseLargeMapWindow();largeMapPolicy.Dispose();CloseChildren(); child.Document = null; session.Dispose(); source.Changed -= SourceChanged; LocalizationService.LanguageChanged -= LanguageChanged; };
        LoadDraft();
    }
    private static string Text(string key) => LocalizationService.Get("MapProperties." + key);
    private Button Button(string controlName, string key, Action action)
    { var button = new Button(); button.SetResourceReference(ContentControl.ContentProperty, "MapProperties." + key); RegisterName(controlName, button); button.Click += (_, _) => action(); return button; }
    private void AddField(Panel panel, string key, Control control, double fieldWidth)
    {
        var box = new StackPanel { Width = fieldWidth, Margin = new(4, 2, 10, 2) }; var label = new TextBlock(); label.SetResourceReference(TextBlock.TextProperty, "MapProperties." + key);
        box.Children.Add(label); control.Margin = new(0, 4, 0, 0); RegisterName("MapProperties" + key, control); box.Children.Add(control); panel.Children.Add(box);
    }
    private void InputChanged() { if (!updating) { inputRevision++;CloseMapNameWindow();CloseLargeMapWindow(); if (!stale) statusKey = null; UpdateStatus(); } }
    private void LoadChoices(ComboBox combo, IReadOnlyList<ValueChoice> choices, string? raw)
    {
        var rows = choices.Select(row => new Choice(row.Value, row.Value + " · " + row.Name(LocalizationService.CurrentLanguage))).ToList();
        if (raw is not null && rows.All(row => row.Value != raw)) rows.Add(new(raw, raw + " · " + Text("Unknown")));
        combo.ItemsSource = rows; combo.DisplayMemberPath = "Label"; combo.SelectedItem = rows.FirstOrDefault(row => row.Value == raw); combo.IsEnabled = raw is not null;
    }
    private void LoadDraft()
    {
        updating = true;
        try
        {
            var general = WorkingDocument.CreateMapGeneralDraft(); name.Text = general.Values.GetValueOrDefault("Name", ""); name.IsEnabled = general.Values.ContainsKey("Name");
            LoadChoices(players, MapGeneralSettings.Keepers, general.Values.GetValueOrDefault("Players"));
            LoadChoices(version, MapGeneralSettings.Versions, general.Values.GetValueOrDefault("Version") ?? WorkingDocument.TargetGameVersion?.ToString(CultureInfo.InvariantCulture));
            version.IsEnabled = general.Values.ContainsKey("Version"); ResetSize(); anchors[0].IsChecked = true;
            child = new EditorWorkspace(requestPreferences) { Document = WorkingDocument };child.ConfigurePlayerGoldContext(source,Current); child.ConfigureGameResources(resources); child.ConfigureMapPropertiesDraft(); childHost.Content = child;
            child.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => InputChanged()));
            child.AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler((_, _) => InputChanged()));
            child.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((_, _) => InputChanged()));
            child.AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler((_, _) => InputChanged()));
            child.AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler((_, _) => InputChanged()));
            statusKey = null; stale = false; UpdateStatus();
        }
        finally { updating = false; }
    }
    private void ResetSize() { width.Text = source.Width.ToString(CultureInfo.InvariantCulture); height.Text = source.Height.ToString(CultureInfo.InvariantCulture); }
    private void CloseChildren() { levelWindow?.Close(); levelWindow = null; speechWindow?.Close(); speechWindow = null; modifiersWindow?.Close();modifiersWindow=null;creatureLevelsWindow?.Close();creatureLevelsWindow=null; }
    private void ResetDraft()
    {
        if (closed || applying || !ownerCurrent() || !commitFields() || closed || !ownerCurrent()) return;
        try
        {
            var fresh = source.CreateMapPropertiesSession(); CloseChildren(); child.Document = null; session.Dispose(); session = fresh; inputRevision++; LoadDraft();
        }
        catch (InvalidOperationException exception) { stale = true; statusKey = "ReadOnly"; status.ToolTip = exception.Message; UpdateStatus(); }
    }
    private bool Current() => !closed && !stale && ownerCurrent() && source.IsMapPropertiesSessionCurrent(session);
    private bool Pending() => child.HasPendingMapPropertiesInputs() || levelWindow?.HasPendingInputs == true || speechWindow?.HasPendingInputs == true || modifiersWindow?.HasPendingInputs==true || creatureLevelsWindow?.HasPendingInputs==true;
    private bool ChildCurrent() => Current() && !applying;
    private void OpenLevelOptions()
    {
        if (!ChildCurrent()) return; if (levelWindow is not null) { levelWindow.Activate(); return; }
        levelWindow = new(WorkingDocument, ChildCurrent, child.CommitFields); var window = levelWindow;
        window.Closed += (_, _) => { if (ReferenceEquals(levelWindow, window)) levelWindow = null; };
        if (LevelOptionsPresenter is { } present) present(window); else { window.Owner = this; window.Show(); }
    }
    private void OpenSpeechText()
    {
        if (!ChildCurrent()) return; if (speechWindow is not null) { speechWindow.Activate(); return; }
        speechWindow = new(WorkingDocument, ChildCurrent, child.CommitFields, resources, _ => { }); var window = speechWindow;
        window.Closed += (_, _) => { if (ReferenceEquals(speechWindow, window)) speechWindow = null; };
        if (SpeechTextPresenter is { } present) present(window); else { window.Owner = this; window.Show(); }
    }
    private void OpenCreatureModifiers()
    {
        if(!ChildCurrent()||!child.CommitFields()||!ChildCurrent())return;
        if(modifiersWindow is not null){modifiersWindow.Activate();return;}
        modifiersWindow=new(WorkingDocument,ChildCurrent,child.CommitFields);var window=modifiersWindow;
        window.Closed+=(_,_)=>{if(ReferenceEquals(modifiersWindow,window))modifiersWindow=null;InputChanged();};
        if(CreatureModifiersPresenter is { } present)present(window);else{window.Owner=this;window.Show();}
    }
    private void OpenCreatureLevels()
    {
        if(!ChildCurrent()||!child.CommitFields()||!ChildCurrent())return;
        if(creatureLevelsWindow is not null){creatureLevelsWindow.Activate();return;}
        var window=new CreatureLevelModifierWindow(WorkingDocument,ChildCurrent,child.CommitFields,resources,creatureLevelCatalogue);creatureLevelsWindow=window;
        window.Closed+=(_,_)=>{if(ReferenceEquals(creatureLevelsWindow,window)){creatureLevelCatalogue=window.SelectedCatalogue??creatureLevelCatalogue;creatureLevelsWindow=null;}InputChanged();};
        if(CreatureLevelsPresenter is { } present)present(window);else{window.Owner=this;window.Show();}
    }
    private void SourceChanged(object? sender, EventArgs args) { if(!closed){CloseMapNameWindow();CloseLargeMapWindow();}if (!closed && !applying) { stale = true; statusKey = "Stale";creatureLevelsWindow?.InvalidateOwner();UpdateStatus(); } }
    private void LanguageChanged(object? sender, EventArgs args)
    {
        if (closed) return; inputRevision++;CloseMapNameWindow();CloseLargeMapWindow(); updating = true;
        try { LoadChoices(players, MapGeneralSettings.Keepers, (players.SelectedItem as Choice)?.Value); var editable = version.IsEnabled; LoadChoices(version, MapGeneralSettings.Versions, (version.SelectedItem as Choice)?.Value); version.IsEnabled = editable; child.RefreshLanguage(); UpdateStatus(); }
        finally { updating = false; }
    }
    private void UpdateStatus()
    {
        var previewWidth=int.TryParse(width.Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var w)&&w is >=0 and <=1024?w:source.Width;
        var previewHeight=int.TryParse(height.Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var h)&&h is >=0 and <=1024?h:source.Height;
        anchorImages.Refresh(Math.Max(0,Array.FindIndex(anchors,anchor=>anchor.IsChecked==true)),previewWidth<source.Width,previewHeight<source.Height);
        status.Text = Text(statusKey ?? (stale ? "Stale" : "Hint"));
        apply.IsEnabled = !applying && Current();
        var preset = WorkingDocument.ResizeFillPreset;
        var known = preset is { } id && LegacyCompilerRules.Elements("NewMap").Any(row => row.ID == id);
        fill.Text = Text("Fill") + ": " + (known ? LocalizationService.Get("Terrain." + (preset!.Value >> 24))
            + ((byte)(preset.Value >> 24) is 8 or 9 ? " · " + LocalizationService.Get("Owner." + (byte)(preset.Value >> 16)) : "") : Text("Unknown"));
    }
    private void CloseLargeMapWindow(){var window=largeMapWindow;largeMapWindow=null;window?.Close();}
    private void CloseMapNameWindow(){var window=mapNameWindow;mapNameWindow=null;window?.Close();}
    private string? SelectMapName(EditorMapNamePlan plan,Func<bool> valid)
    {
        using var request=mapNamePolicy.Begin(plan,valid);
        if(request is null)return plan.CopyName();
        if(!request.RequiresPrompt){request.Copy();return request.SelectedName;}
        using var window=new MapNameWarningWindow(request){Owner=this};mapNameWindow=window;
        try{if(MapNamePresenter is { } presenter)presenter(window);else window.ShowDialog();}
        finally{if(ReferenceEquals(mapNameWindow,window))mapNameWindow=null;}
        if(window.Failure is { } error)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        return request.SelectedName;
    }
    private bool ConfirmLargeMap(MapPropertiesPlan plan,Func<bool> valid)
    {
        using var request=largeMapPolicy.BeginResize(plan,valid);
        if(request is null)return true;
        if(!request.RequiresPrompt)return request.Accept();
        if(LargeMapPrompt is { } legacy)
        {if(!legacy(plan)){request.Decline();return false;}return request.Accept();}
        using var window=new LargeMapWarningWindow(request){Owner=this};largeMapWindow=window;
        try{if(LargeMapPresenter is { } presenter)presenter(window);else window.ShowDialog();}
        finally{if(ReferenceEquals(largeMapWindow,window))largeMapWindow=null;}
        if(window.Failure is { } error)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        return request.IsAccepted;
    }
    private bool Confirm(MapPropertiesPlan plan, bool cropping,Func<bool>? valid=null)
    {
        if(!cropping)return ConfirmLargeMap(plan,valid??Current);
        var prompt = CropPrompt;
        if (prompt is not null) return prompt(plan);
        var details = cropping ? string.Join(", ", plan.Crops.Select(row => LocalizationService.Get("Category." + row.Category, row.Category) + ": " + row.Count)) : plan.Resize.Width + " × " + plan.Resize.Height;
        return DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, Text(cropping ? "CropConsent" : "LargeConsent") + "\n" + details, Text("Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }
    public bool ApplyProperties()
    {
        if (applying || !Current()) return false;
        applying = true;
        try
        {
            if (!commitFields() || !child.CommitFields() || !Current()) return false;
            if (Pending()) { statusKey = "Pending"; return false; }
            int Dimension(TextBox input, int original) { var value = int.Parse(input.Text, NumberStyles.Integer, CultureInfo.InvariantCulture); if (value is < 0 or > 1024) throw new ArgumentOutOfRangeException(nameof(input)); return value == original ? value : Math.Max(7, value); }
            var resize = new MapPropertiesResize(Dimension(width, source.Width), Dimension(height, source.Height), Array.FindIndex(anchors, anchor => anchor.IsChecked == true), RemoveOutside: true);
            var general = WorkingDocument.CreateMapGeneralDraft();
            // Validate other fields before asking about the name. No private
            // write occurs until a current Copy/Keep answer has been obtained.
            var inputs=general.Values.Select(row=>new MapGeneralInput(row.Key,row.Key switch {"Players"=>((Choice)players.SelectedItem).Value,"Version"=>((Choice)version.SelectedItem).Value,_=>row.Value})).ToArray();
            _=WorkingDocument.UpdateMapGeneralSettings(general,inputs);
            string? selectedName=null;
            if(general.Values.ContainsKey("Name"))
            {
                var namePlan=WorkingDocument.PrepareEditorMapName(general,name.Text,session);var nameRevision=inputRevision;
                var nameLevel=levelWindow;var nameSpeech=speechWindow;var nameModifiers=modifiersWindow;var nameCreatureLevels=creatureLevelsWindow;var nameCreatureLevelsRevision=nameCreatureLevels?.InputRevision;
                var nameLevelRevision=nameLevel?.InputRevision;var nameSpeechRevision=nameSpeech?.InputRevision;var nameModifiersRevision=nameModifiers?.InputRevision;
                bool NameValid()=>Current()&&nameRevision==inputRevision&&!Pending()&&namePlan.IsCurrent
                    &&ReferenceEquals(nameLevel,levelWindow)&&ReferenceEquals(nameSpeech,speechWindow)&&ReferenceEquals(nameModifiers,modifiersWindow)
                    &&nameLevelRevision==levelWindow?.InputRevision&&nameSpeechRevision==speechWindow?.InputRevision&&nameModifiersRevision==modifiersWindow?.InputRevision
                    &&ReferenceEquals(nameCreatureLevels,creatureLevelsWindow)&&nameCreatureLevelsRevision==creatureLevelsWindow?.InputRevision;
                selectedName=SelectMapName(namePlan,NameValid);
                if(!NameValid()){statusKey="Changed";return false;}
                if(selectedName is null)return false;
            }
            var staged=WorkingDocument.UpdateMapGeneralSettings(general,inputs.Select(row=>row.Field=="Name"?row with{Value=selectedName!}:row));
            WorkingDocument.ApplyMapGeneralSettings(staged);
            var plan = source.PrepareEditorMapProperties(session, resize); var revision = inputRevision;
            var level = levelWindow; var speech = speechWindow; var levelRevision = level?.InputRevision; var speechRevision = speech?.InputRevision;
            var modifiers=modifiersWindow;var modifiersRevision=modifiers?.InputRevision;
            var creatureLevels=creatureLevelsWindow;var creatureLevelsRevision=creatureLevels?.InputRevision;
            bool Valid() => Current() && revision == inputRevision && ReferenceEquals(level, levelWindow) && ReferenceEquals(speech, speechWindow)
                && levelRevision == levelWindow?.InputRevision && speechRevision == speechWindow?.InputRevision && ReferenceEquals(modifiers,modifiersWindow) && modifiersRevision==modifiersWindow?.InputRevision
                &&ReferenceEquals(creatureLevels,creatureLevelsWindow)&&creatureLevelsRevision==creatureLevelsWindow?.InputRevision && !Pending() && source.IsMapPropertiesPlanCurrent(plan);
            bool large = !plan.NeedsLargeMapConsent, crop = !plan.NeedsCropConsent;
            if (!large) { large = Confirm(plan, false,Valid); if (!Valid()) { statusKey = "Changed"; return false; } if (!large) return false; }
            if (!crop) { crop = Confirm(plan, true); if (!Valid()) { statusKey = "Changed"; return false; } if (!crop) return false; }
            if (!commitFields() || !child.CommitFields() || !Valid()) { statusKey = "Changed"; return false; }
            source.ApplyEditorMapProperties(plan, large, crop); Close(); return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException or InvalidDataException or IOException)
        { statusKey = Current() ? "Invalid" : "Stale"; status.ToolTip = exception.Message; return false; }
        finally { applying = false; if (!closed) UpdateStatus(); }
    }
}
