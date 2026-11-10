using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace : UserControl
{
    public EditorFileDialogSession FileDialogs {get;}=new();
    private NativeMapDocument? document;
    private IReadOnlyList<NativeRecord> records = [];
    private SelectionClipboard? clipboard;
    private bool updating;
    private bool refreshingToolLabels;
    private bool updatingEventType;
    private bool updatingRecords;
    private FieldView? namedField;
    private IReadOnlyList<NamedChoiceView> namedChoices = [];
    private bool updatingNamed;
    private int catalogueVersion = 17;
    private string? transformStatusKey;
    public EditorWorkspace():this(null) { }
    internal EditorWorkspace(EditorRequestPreferences? requestPreferences)
    {
        objectManagerSession=new(requestPreferences);
        regionRemovalPolicy = new(objectManagerSession.RequestPreferences);
        cursorRemovalPolicy = new(objectManagerSession.RequestPreferences);
        inspectorRemovalPolicy = new(objectManagerSession.RequestPreferences);
        scenarioDeletionPolicy = new(objectManagerSession.RequestPreferences);
        playerGoldPolicy=new(objectManagerSession.RequestPreferences);
        InitializeComponent();
        SelectionInfoIcon.Source=OriginalMapInfoImages.For(30);ClipboardInfoIcon.Source=OriginalMapInfoImages.For(31);GoldInfoIcon.Source=OriginalMapInfoImages.For(32);
        OriginalControlIcons.Decorate(PlayerAIRoomUp,ControlImageResources.ForVerticalMove(false));OriginalControlIcons.Decorate(PlayerAIRoomDown,ControlImageResources.ForVerticalMove(true));
        OriginalControlIcons.Decorate(ScriptPointUpButton,ControlImageResources.ForVerticalMove(false));OriginalControlIcons.Decorate(ScriptPointDownButton,ControlImageResources.ForVerticalMove(true));
        OriginalControlIcons.Decorate(ScriptMoveUpButton,ControlImageResources.ForBranchMove(ScriptBranchMove.Up)!.Value);OriginalControlIcons.Decorate(ScriptMoveDownButton,ControlImageResources.ForBranchMove(ScriptBranchMove.Down)!.Value);
        OriginalControlIcons.Decorate(ScriptIndentButton,ControlImageResources.ForBranchMove(ScriptBranchMove.Indent)!.Value);OriginalControlIcons.Decorate(ScriptOutdentButton,ControlImageResources.ForBranchMove(ScriptBranchMove.Outdent)!.Value);
        InitializeEditorSounds();
        WorkspaceTabs.SelectionChanged += (_,args) => { if(ReferenceEquals(args.OriginalSource,WorkspaceTabs) && WorkspaceTabs.SelectedIndex!=2)CloseScenarioDeletionWindow(); };
        Unloaded += (_,_)=>CloseUnderCursorRemoval();
        Unloaded += (_,_)=>CloseInspectorObjectRemoval();
        Unloaded += (_,_)=>CloseScenarioDeletionWindow();
        Unloaded += (_,_)=>InvalidatePlayerGoldNotice();
        Unloaded += (_, _) => CloseScenarioNamesWindow();
        Unloaded += (_, _) => CloseScenarioPointWindow();
        Unloaded += (_, _) => CloseHeroBandWindow();
        Unloaded += (_, _) => CloseBrushQuick();
        Unloaded += (_, _) => CloseLevelOptionsWindow();
        Unloaded += (_, _) => CloseSpeechTextWindow();
        Unloaded += (_, _) => CloseMapPropertiesWindow();
        Unloaded += (_, _) => CloseRegionPropertiesWindow();
        Unloaded += (_, _) => CloseRegionRemovalWindow();
        Unloaded += (_, _) => { generationContext++; CloseGateGenerationRequest(); };
        ConfigureObjectDisplay(null);
        ConfigureOverlays(null);
        Canvas.SelectionChanged += (_, _) => copyContextRevision++;
        Unloaded += (_, _) => CloseCopyBufferConfirmation();
        Canvas.ZoomChanged += (_, _) => { RefreshZoomControls(); RefreshOverviewViewport(); };
        ElementPicker.ItemTemplate = BandCreaturePicker.ItemTemplate = BandSlotPicker.ItemTemplate = NamedValuePicker.ItemTemplate = OriginalObjectImages.NamedTemplate();
        PlayerAIRoomOrder.ItemTemplate = OriginalObjectImages.NamedTemplate();
        ToolPicker.ItemTemplate = TemplatePicker.ItemTemplate = OriginalObjectImages.NamedTemplate(20);
        TerrainPicker.ItemTemplate = OriginalObjectImages.NamedTemplate();
        ScriptOwnerPicker.ItemTemplate = ScriptMovePicker.ItemTemplate = ScriptTargetPicker.ItemTemplate = OriginalEditorListImages.DescriptionTemplate();
        RecordList.ItemTemplate = HeroBandRecordTemplate.Create();
        GeneratorSites.ItemTemplate=OriginalControlIcons.Template("Label");
        Overview.NavigationRequested += Overview_NavigationRequested;
        Overview.FullscreenRequested += (_,_) => ShowFullscreenOverview();
        Unloaded += (_,_) => { CloseZoomWindow(); CloseFullscreenOverview(); CloseThumbnailWindow(); CloseCameraPathWindow(); CloseObjectBatchWindow(); CloseObjectManagerWindow(); CloseWorkspaceImagePicker(); };
        Canvas.SizeChanged += (_,_) => RefreshOverviewViewport();
        InitializeRegions();
        InitializePatrol();
        InitializePlayerGeometry();
        ResetGateGeneration();
        ResetRuleEditing();
        ResetVariableEditing();
        ResetPlayerEditing();
        ResetDefinitionEditing();
        Canvas.CursorChanged += (_, _) => RefreshMapStatus();
        Canvas.SelectionChanged += (_, _) => RefreshMapStatus();
        Canvas.PlacementChanged += (_, _) => RefreshPlacementStatus();
        Canvas.ObjectPaintingRequested += (_, args) => PaintSingleObject(args.Position);
        Canvas.TileSampled += (_, _) =>
        {
            // Each picker raises Paint_Changed. Preserve the original sampled
            // value while updating all three; reading Canvas between changes
            // loses the sampled owner/variant to the old palette state.
            var sampled = Canvas.PaintTile;
            var wasSuppressed=suppressEditorSounds;suppressEditorSounds=true;
            try
            {
                TerrainPicker.SelectedValue = (int)sampled.Terrain;
                OwnerPicker.SelectedValue = (int)sampled.Owner;
                VariantPicker.SelectedIndex = sampled.Variant == 2 ? 1 : 0;
                Canvas.PaintTile = sampled;
            }
            finally{suppressEditorSounds=wasSuppressed;}
        };
        RefreshLanguage();
    }
    public event EventHandler? DocumentContextChanged;
    public NativeMapDocument? Document
    {
        get => document;
        set
        {
            CloseBrushQuick();
            CloseScenarioNamesWindow();
            CloseScenarioPointWindow();
            CloseHeroBandWindow();
            CloseLevelOptionsWindow();
            CloseSpeechTextWindow();
            CloseMapPropertiesWindow();
            CloseRegionPropertiesWindow();
            CloseRegionRemovalWindow();
            CloseUnderCursorRemoval();
            CloseInspectorObjectRemoval();
            CloseScenarioDeletionWindow();
            InvalidatePlayerGoldNotice();
            playerGoldCapacitySource=null;playerGoldHostCurrent=null;
            DocumentContextChanged?.Invoke(this, EventArgs.Empty);
            CloseCopyBufferConfirmation();
            CloseZoomWindow();
            CloseFullscreenOverview();
            CloseCameraPathWindow();
            CloseObjectBatchWindow();
            CloseObjectManagerWindow();
            CloseWorkspaceImagePicker();
            objectBrushUpgrade = false; objectPaintingStatusKey = null;
            CancelRegionEditing();
            ResetPatrolEditing();
            regionStatusKey = null; RegionStatus.ClearValue(TextBlock.TextProperty); RegionStatus.ToolTip = null; RegionStatus.Visibility = Visibility.Collapsed;
            regionManagerKind=null;
            if (document is not null) document.Changed -= DocumentChanged;
            document = value; statisticsSnapshot = null; InvalidateMapInfoGold(); Canvas.Document = value; Overview.Document = value;
            if(value is null)CloseThumbnailWindow();else thumbnailWindow?.BindDocument(value);
            ObjectBatchButton.IsEnabled = value is not null;
            ObjectManagerButton.IsEnabled = value is not null;
            RefreshOverviewViewport();
            generatorForm = null;
            ResetGateGeneration();
            ResetRuleEditing();
            ResetVariableEditing();
            ResetPlayerEditing();
            ResetDefinitionEditing();
            catalogueVersion = ValueCatalogues.GameVersion(document);
            if (document is not null) document.Changed += DocumentChanged;
            RefreshTemplates();
            RefreshRecords();
            ScenarioNamesButton.IsEnabled = value is not null && !scenarioNamesOwnerClosed;
            LevelOptionsButton.IsEnabled = value is not null && !levelOptionsOwnerClosed;
            SpeechTextButton.IsEnabled = value is not null && !speechTextOwnerClosed;
            MapPropertiesButton.IsEnabled = value is not null && !mapPropertiesOwnerClosed;
            RefreshClipboardStatus();
            ClearTransformStatus();
        }
    }

    public void RefreshLanguage()
    {
        var wasSuppressed=suppressEditorSounds;suppressEditorSounds=true;
        try
        {
        // DataGridColumn is outside the visual tree: its DynamicResource Header
        // can retain the dictionary used when the column was first displayed.
        PropertyGrid.Columns[0].Header = LocalizationService.Get("Editor.Field");
        PropertyGrid.Columns[1].Header = LocalizationService.Get("Editor.Value");
        PropertyGrid.Columns[2].Header = LocalizationService.Get("Editor.NamedValue");
        PropertyGrid.Columns[3].Header = LocalizationService.Get("Editor.Offset");
        Canvas.InvalidateVisual();
        RefreshZoomControls();
        var namedSource = document; var namedTarget = namedField;
        var namedSelection = (NamedValuePicker.SelectedItem as NamedChoiceView)?.Choice.Value; var namedQuery = NamedSearch.Text;
        var patrolWait = PatrolWait.Text;
        var placementResult = Canvas.LastPlacementResult;
        var tool = ToolPicker.SelectedIndex;
        refreshingToolLabels = true;
        try
        {
            ToolPicker.ItemsSource = Enum.GetValues<DrawingTool>().Select(value => new PaletteChoice((int)value, LocalizationService.Get("Tool." + value),()=>OriginalEditorListImages.For(MapPaletteImageResources.ForDrawingTool(value.ToString()),20))).ToArray();
            OwnerPicker.DisplayMemberPath = "Label";
            ToolPicker.SelectedValuePath = OwnerPicker.SelectedValuePath = TerrainPicker.SelectedValuePath = "ID";
            ToolPicker.SelectedIndex = Math.Max(0, tool);
        }
        finally { refreshingToolLabels = false; }
        var terrain = TerrainPicker.SelectedValue as int? ?? 2;
        TerrainPicker.ItemsSource = Enumerable.Range(1, 41).Select(id => new PaletteChoice(id, $"{id:X2} · {LocalizationService.Get($"Terrain.{id}", $"ID {id}")}",()=>PaletteTerrainImage(id))).ToArray();
        TerrainPicker.SelectedValue = terrain;
        var owner = OwnerPicker.SelectedValue as int? ?? 2;
        OwnerPicker.ItemsSource = Enumerable.Range(1, 7).Select(id => new Choice(id, LocalizationService.Get($"Owner.{id}"))).ToArray();
        OwnerPicker.SelectedValue = owner;
        RefreshTemplates();
        RefreshRecords();
        RefreshClipboardStatus();
        RefreshMapStatus();
        RefreshPlacementStatus();
        Canvas.RestorePlacementResult(placementResult);
        if (transformStatusKey is { } statusKey) ShowTransformStatus(statusKey);
        RefreshRegions();
        PatrolWait.Text = patrolWait;
        // Rebinding the property grid transiently clears its selected row. Restore only
        // a same-document/same-field draft whose underlying stored value did not change.
        if (namedTarget is not null && ReferenceEquals(namedSource, document) && SameImageField(namedTarget)
            && namedField?.Value == namedTarget.Value && namedSelection is not null)
        {
            updatingNamed = true; NamedSearch.Text = namedQuery; updatingNamed = false; FilterNamedValues(namedSelection);
        }
        }
        finally{suppressEditorSounds=wasSuppressed;}
    }
    private void RefreshTemplates()
    {
        var selected = (TemplatePicker.SelectedItem as TemplateView)?.LegacyKind ?? (TemplatePicker.SelectedItem as TemplateView)?.Template?.Structure;
        TemplatePicker.ItemsSource = document?.IsLegacyProject == true
            ? document.LegacyAddChoices().Select(kind => new TemplateView(null, LocalizationService.Get("ProjectKind." + kind, kind), kind)).ToArray()
            : RecordTemplate.All.Select(template => new TemplateView(template, LocalizationService.Get("Category." + template.Structure, template.Structure))).ToArray();
        TemplatePicker.SelectedItem = ((TemplateView[])TemplatePicker.ItemsSource).FirstOrDefault(item => (item.LegacyKind ?? item.Template?.Structure) == selected);
        if (TemplatePicker.SelectedIndex < 0) TemplatePicker.SelectedIndex = 0;
        RefreshElementPicker();
    }
    private static string ChoiceLabel(ValueChoice choice) => $"{choice.Value} · {choice.Name(LocalizationService.CurrentLanguage)}";
    private void Template_Changed(object sender, SelectionChangedEventArgs e) { if (ElementPicker is not null) RefreshElementPicker(); }
    private void RefreshElementPicker()
    {
        var selected = (ElementPicker.SelectedItem as NamedChoiceView)?.Choice.Value;
        var template = TemplatePicker.SelectedItem as TemplateView;
        if(AddRecordButton is not null)AddRecordButton.IsEnabled=document is not null&&template is not null;
        var choices = ValueCatalogues.ForTemplate(template?.LegacyKind ?? template?.Template?.Structure ?? "", catalogueVersion);
        ElementPicker.Visibility = choices.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        var views = choices.Select(choice => new NamedChoiceView(choice, ChoiceLabel(choice), TemplateChoiceIcon(template, choice))).ToArray();
        ElementPicker.ItemsSource = views;
        if (ElementImagePickerButton is not null) ElementImagePickerButton.Visibility = views.Any(view => view.Icon is not null) ? Visibility.Visible : Visibility.Collapsed;
        ElementPicker.SelectedItem = views.FirstOrDefault(view => view.Choice.Value == selected);
        if (ElementPicker.SelectedIndex < 0) ElementPicker.SelectedIndex = 0;
    }
    public void Undo() => document?.Undo();
    public void Redo() => document?.Redo();
    public void SelectAll() => Canvas.SelectAll();
    public void ClearSelection() => Canvas.ClearSelection();
    public void SetSelection(TileRect rectangle) => Canvas.SetSelection(rectangle);
    public SelectionBoundsDialog? CreateSelectionBoundsDialog() => document is null ? null
        : new(Window.GetWindow(this), document.Width, document.Height, Canvas.Selection);
    public void ChangeSelection()
    {
        var dialog = CreateSelectionBoundsDialog();
        if (dialog?.ShowDialog() == true) SetSelection(dialog.Rectangle);
    }
    private void RefreshMapStatus()
    {
        RefreshMapInfoGold();
        PositionText.Text = document is null ? "" : $"X: {Canvas.CursorTile.X}  Y: {Canvas.CursorTile.Y}  {document.Width} × {document.Height}";
        if (document?.IsEditorProtectedCell(Canvas.CursorTile.X, Canvas.CursorTile.Y) == true) PositionText.Text += " · " + LocalizationService.Get("Editor.ProtectedBorder");
        SelectionStatus.Text = Canvas.Selection is { } rectangle
            ? string.Format(System.Globalization.CultureInfo.CurrentCulture, LocalizationService.Get("Editor.SelectionInfo"), rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height)
            : LocalizationService.Get("Editor.NoSelection");
    }
    private void RefreshPlacementStatus()
    {
        if (FootprintStatus is null || PlacementStatus is null) return;
        var footprint = Canvas.PaintFootprint;
        if (RandomPaintCheck is not null) RandomPaintCheck.IsEnabled = document is not null && !Canvas.IsPickingRegion && !footprint.RequiresRectangle
            && Canvas.Tool is DrawingTool.Line or DrawingTool.Rectangle or DrawingTool.FilledRectangle or DrawingTool.Ellipse or DrawingTool.FilledEllipse;
        FootprintStatus.Visibility = document is not null && !Canvas.IsPickingRegion && footprint.RequiresRectangle && Canvas.Tool is not (DrawingTool.Select or DrawingTool.Sample or DrawingTool.PlaceObject) ? Visibility.Visible : Visibility.Collapsed;
        string Axis(int min, int max) => max == min ? min.ToString() : $"{min}–{(max == 0 ? "∞" : max.ToString())}";
        FootprintStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, LocalizationService.Get("Editor.FootprintInfo"), Axis(footprint.MinWidth, footprint.MaxWidth), Axis(footprint.MinHeight, footprint.MaxHeight));
        PlacementStatus.Visibility = document is not null && Canvas.LastPlacementResult is not (null or EditorPlacementResult.Painted) ? Visibility.Visible : Visibility.Collapsed;
        PlacementStatus.Text = Canvas.LastPlacementResult is not (null or EditorPlacementResult.Painted) ? LocalizationService.Get("Editor.Placement." + Canvas.LastPlacementResult) : "";
        if (Canvas.Tool == DrawingTool.PlaceObject && objectPaintingStatusKey is not null)
        { PlacementStatus.Visibility = Visibility.Visible; PlacementStatus.Text = LocalizationService.Get(objectPaintingStatusKey); }
    }
    private void Resize_Click(object sender, RoutedEventArgs e) => ShowMapProperties();
    public SelectionClipboard? BufferedSelection => clipboard;
    private void CaptureSelection(bool cut)
    {
        if (document is null || Canvas.Selection is not { } rect) return;
        var candidate = document.CopySelection(rect, protectBoundary: true);
        if (cut) document.CutEditorSelection(rect, Canvas.PaintTile);
        clipboard = candidate; copyContextRevision++; RefreshClipboardStatus();
    }
    public void Copy() { if (CommitFields()) RecordOperation(() => { CaptureSelection(false); return null; }); }
    public void Cut()
    {
        if (!CommitFields()) return;
        RecordOperation(() => { CaptureSelection(true); return null; });
    }
    public void Paste() { if (document is not null && clipboard is not null && CommitFields()) RecordOperation(() => { PasteLoadedSelection(clipboard); return null; }); }
    public void LoadSelectionIntoBuffer(SelectionClipboard selection) { clipboard = selection; copyContextRevision++; RefreshClipboardStatus(); }
    public void PasteLoadedSelection(SelectionClipboard selection)
    {
        if (document is null) throw new InvalidOperationException("Open a map before pasting a selection.");
        var position = Canvas.Selection is { } rect ? new TilePoint(rect.X, rect.Y) : Canvas.CursorTile;
        document.PasteEditorSelection(position, SelectionFile.ConvertToFormat(selection, document.IsLegacyProject));
    }
    private void RefreshClipboardStatus()
    {
        if (ClipboardStatus is null) return;
        ClipboardStatus.Text = clipboard is null ? LocalizationService.Get("Editor.CopyBufferEmpty")
            : string.Format(System.Globalization.CultureInfo.CurrentCulture, LocalizationService.Get("Editor.CopyBufferInfo"),
                clipboard.IsLegacyProject ? "DKM" : "KWD", clipboard.Tiles.Width, clipboard.Tiles.Height, clipboard.ObjectCount, clipboard.ScriptCount);
    }
    public void ExportSelection()
    {
        if (document is null || !CommitFields()) return;
        if (Canvas.Selection is not { } rectangle)
        { DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("Editor.SelectionRequired")); return; }
        var location=FileDialogs.Location(EditorFileDialogKind.Area);
        var dialog = new EditorPreviewFileDialog(EditorFileDialogKind.Area,save:true) { Title = LocalizationService.Get("Menu.ExportSelection"), Filter = "Dungeon Keeper 2 (*.dkz)|*.dkz", DefaultExt = ".dkz", AddExtension = true, OverwritePrompt = true,FileName=location.FileName,InitialDirectory=location.Directory };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        RecordOperation(() =>
        {
            FileDialogs.AcceptAreaPath(dialog.FileName);
            SelectionFile.SaveDirectSelectionConfirmed(dialog.FileName, document.CopySelection(rectangle));
            DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("Editor.SelectionExported") + "\n" + dialog.FileName);
            return null;
        });
    }
    public void ExportCopyBuffer()
    {
        if (clipboard is null || !CommitFields()) return;
        var location=FileDialogs.Location(EditorFileDialogKind.Area);
        var dialog = new EditorPreviewFileDialog(EditorFileDialogKind.Area,save:true) { Title = LocalizationService.Get("Menu.ExportCopyBuffer"), Filter = "Dungeon Keeper 2 (*.dkz)|*.dkz", DefaultExt = ".dkz", AddExtension = true, OverwritePrompt = true,FileName=location.FileName,InitialDirectory=location.Directory };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        RecordOperation(() => { FileDialogs.AcceptAreaPath(dialog.FileName);SelectionFile.SaveConfirmed(dialog.FileName, clipboard); return null; });
    }
    public void ImportSelection(bool pasteIntoSelection = false)
    {
        if (!CommitFields() || pasteIntoSelection && (document is null || Canvas.Selection is null)) return;
        var location=FileDialogs.Location(EditorFileDialogKind.Area);
        var dialog = new EditorPreviewFileDialog(EditorFileDialogKind.Area) { Title = LocalizationService.Get(pasteIntoSelection ? "Menu.ImportIntoSelection" : "Menu.ImportSelection"), Filter = "Dungeon Keeper 2 (*.dkz)|*.dkz", CheckFileExists = true,FileName=location.FileName,InitialDirectory=location.Directory };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        var loaded = false;
        RecordOperation(() =>
        {
            FileDialogs.AcceptAreaPath(dialog.FileName);loaded = RequestSelectionFile(dialog.FileName, pasteIntoSelection); return null;
        });
        if (loaded) DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get(pasteIntoSelection ? "Editor.SelectionInserted" : "Editor.SelectionLoaded"));
    }
    public void Rotate() => TransformSelection(true);
    public void RotateCounterclockwise() => TransformSelection(true, clockwise: false);
    public void MirrorHorizontal() => TransformSelection(false, true);
    public void MirrorVertical() => TransformSelection(false, false);
    public void CopyRotatedSelection()
    {
        if (document is null || Canvas.Selection is not { } rect) return;
        if (!CommitFields()) return;
        RecordOperation(() => { LoadSelectionIntoBuffer(document.CopySelection(rect, protectBoundary: true).RotateClockwise()); ClearTransformStatus(); return null; });
    }
    private void TransformSelection(bool rotate, bool horizontal = false, bool clockwise = true)
    {
        if (document is null) return;
        if (Canvas.Selection is not { } rect) { ShowTransformStatus("Editor.SelectionRequired"); return; }
        if (rotate && rect.Width != rect.Height) { ShowTransformStatus("Editor.RotationSquareRequired"); return; }
        if (!rotate && (horizontal ? rect.Width : rect.Height) < 2) { ShowTransformStatus("Editor.ReflectionSizeRequired"); return; }
        if (!CommitFields()) return;
        RecordOperation(() => { document.TransformEditorSelection(rect, rotate, horizontal, clockwise); ClearTransformStatus(); return null; });
    }
    private void ShowTransformStatus(string key)
    {
        transformStatusKey = key; TransformStatus.Text = LocalizationService.Get(key); TransformStatus.Visibility = Visibility.Visible;
    }
    private void ClearTransformStatus()
    {
        transformStatusKey = null;
        if (TransformStatus is not null)
        {
            TransformStatus.ClearValue(TextBlock.TextProperty); TransformStatus.Visibility = Visibility.Collapsed;
        }
    }
    private void Tool_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!refreshingToolLabels && Canvas is not null && ToolPicker.SelectedValue is int value)
        {
            var changed=Canvas.Tool!=(DrawingTool)value;Canvas.Tool=(DrawingTool)value;
            if(changed&&!updating)PlayEditorSound(new(EditorSoundAction.ToolChoice));
        }
    }
    private void RandomPaint_Click(object sender, RoutedEventArgs e) => Canvas.RandomPainting = RandomPaintCheck.IsChecked == true;
    private void Paint_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Canvas is null || TerrainPicker?.SelectedValue is not int terrain || OwnerPicker?.SelectedValue is not int owner) return;
        Canvas.PaintTile = new((byte)terrain, (byte)owner, (byte)(VariantPicker.SelectedIndex == 1 ? 2 : 1));
        RefreshPaletteImages();
        if(!updating&&ReferenceEquals(sender,TerrainPicker)&&e.AddedItems.Count!=0)PlayEditorSound(new(EditorSoundAction.TerrainChoice));
    }
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => ScaleZoom(1 / 1.25);
    private void ZoomIn_Click(object sender, RoutedEventArgs e) => ScaleZoom(1.25);
    private void Entities_Click(object sender, RoutedEventArgs e) { Canvas.ShowEntities = EntityToggle.IsChecked == true; Canvas.InvalidateVisual(); }
    private void DocumentChanged(object? sender, EventArgs e) { CloseCopyBufferConfirmation(); CloseWorkspaceImagePicker(); InvalidateGateGeneration(); InvalidateVariableEditing(); InvalidatePlayerEditing(); InvalidateDefinitionEditing(); RuleConfirmed.IsChecked = false; ruleStatusKey = null; RuleStatus.Text = ""; RuleStatus.ToolTip = null; RefreshRecords(); RefreshMapStatus(); }
    private void RefreshRecords()
    {
        statisticsSnapshot = null;
        InvalidateMapInfoGold();
        var version = ValueCatalogues.GameVersion(document);
        if (version != catalogueVersion) { catalogueVersion = version; RefreshElementPicker(); }
        updating = true;
        var category = (CategoryPicker.SelectedItem as CategoryView)?.ID;
        var oldRecord = (RecordList.SelectedItem as RecordView)?.Record;
        records = document?.Records() ?? [];
        var categories = records.Select(record => record.Category).Distinct().Select(value => new CategoryView(value, LocalizationService.Get("Category." + value, value)))
            .Concat(document is null?[]:new[]{new CategoryView(BandManagerCategory,LocalizationService.Get("Manager.Bands"))}).ToArray();
        if (!categories.Any(value => value.ID == category))
            category = records.FirstOrDefault(record => record.File == oldRecord?.File)?.Category ?? category;
        CategoryPicker.DisplayMemberPath = "Label";
        CategoryPicker.ItemsSource = categories;
        CategoryPicker.SelectedItem = categories.FirstOrDefault(value => value.ID == category);
        if (CategoryPicker.SelectedIndex < 0) CategoryPicker.SelectedIndex = 0;
        updating = false;
        FilterRecords();
        RefreshScriptTree();
        RefreshRegions();
    }
    private void Category_Changed(object sender, SelectionChangedEventArgs e) { if (!updating) FilterRecords(); }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (!updating && RecordList is not null) FilterRecords(); }
    private NativeMapDocument? recordSelectionDocument;
    private long recordSelectionRevision = -1;
    private void FilterRecords()
    {
        var oldView = RecordList.SelectedItem as RecordView;
        var old = oldView?.Record;
        var selectedRecords = ReferenceEquals(document, recordSelectionDocument) && document?.EditRevision == recordSelectionRevision
            ? RecordList.SelectedItems.Cast<RecordView>().Select(view => view.Record).ToArray() : [];
        var category = (CategoryPicker.SelectedItem as CategoryView)?.ID;
        var query = RecordSearch.Text.Trim();
        var views = records.Where(record => category==BandManagerCategory?NativeMapDocument.IsBand(record):record.Category == category).Select((record, index) => new RecordView(record, RecordLabel(record), index,
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[4..]),category==BandManagerCategory?HeroBandPreview(record):null))
            .Where(view => query.Length == 0 || view.Label.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        updatingRecords = true;
        try
        {
            RecordList.SelectionMode = category is "GAMEAREA" or "GAMEGATE" or "GAMEEFFECT" or "Project.MAPRECTEX"
                or "GAMEKEEPER" or "GAMEHERO" or "GAMENEUTRAL" or "GAMEDOOR" or "GAMETRAP" or "GAMEOBJECT"
                or "Project.MAPTHING" ? SelectionMode.Extended : SelectionMode.Single;
            RecordList.ItemsSource = views;
            RecordList.SelectedItem = views.FirstOrDefault(view => view.Record.File == old?.File &&
                (view.Version != oldView!.Version ? view.Index == oldView.Index : view.Record.Offset == old.Offset))
                ?? views.FirstOrDefault(view => selectedRecords.Any(record => SameRemovalRecord(record, view.Record)));
            if (RecordList.SelectedIndex < 0) RecordList.SelectedIndex = 0;
            if (RecordList.SelectionMode == SelectionMode.Extended)
                foreach (var selected in views.Where(view => selectedRecords.Any(record => SameRemovalRecord(record, view.Record))))
                    if (!RecordList.SelectedItems.Contains(selected)) RecordList.SelectedItems.Add(selected);
            recordSelectionDocument = document; recordSelectionRevision = document?.EditRevision ?? -1;
        }
        finally { updatingRecords = false; }
        // Equal record views can suppress WPF's selection event even when the
        // document bytes or referenced entities changed. Refresh explicitly.
        RefreshSelectedRecord();
        if (views.Length == 0) { PropertyGrid.ItemsSource = null; RecordInfo.Text = ""; EventTypePicker.Visibility = Visibility.Collapsed; BandPanel.Visibility = Visibility.Collapsed; ClearNamedValue(); RefreshEffectGenerator(null); RefreshCameraPathControl(null); RefreshScenarioAliasEvent(null); }
    }
    private string RecordLabel(NativeRecord record)
    {
        if (record.Category == "Working.AreaScope") return LocalizationService.Get("RegionRemoval.DeletedArea") + " " + record.Read(record.Fields.Single(field => field.Name == "bAreaID"));
        if (record.Category == "Working.OwnerScope") return LocalizationService.Get("ScenarioPoint.DeletedOwner") + " " + record.Read(record.Fields.Single(field => field.Name == "uniqueid"));
        if (record.Category.StartsWith("Definition.", StringComparison.Ordinal))
        {
            var name = System.Text.Encoding.ASCII.GetString(record.File.Data.Span.Slice(record.Offset, Math.Min(32, record.Length))).Split('\0')[0];
            return name + $" · 0x{record.Offset:X}";
        }
        var id = record.Fields.FirstOrDefault(field => field.Name.EndsWith("ID", StringComparison.Ordinal) || field.Name is "ID" or "bGlobalPlayer");
        var primary = ValueCatalogues.PrimaryField(record);
        var named = primary is null ? null : ValueCatalogues.ForField(record, primary, catalogueVersion).FirstOrDefault(choice => choice.Value == record.Read(primary));
        var nameField = record.Fields.FirstOrDefault(field => field.Name is "name" or "Name" or "PlayerName" or "szPlayerName");
        var label = named?.Name(LocalizationService.CurrentLanguage) ?? (nameField is null ? "" : record.Read(nameField));
        return (label.Length == 0 ? "" : label + " · ") + $"{System.IO.Path.GetFileName(record.File.SourcePath)} · 0x{record.Offset:X}" + (id is null ? "" : $" · ID {record.Read(id)}");
    }
    private void Record_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!updatingRecords) { CloseRegionRemovalWindow(); CloseInspectorObjectRemoval(); RefreshSelectedRecord(); }
    }
    private void RefreshSelectedRecord()
    {
        if (RecordList.SelectedItem is not RecordView view) return;
        updatingEventType = true;
        var projectEvent = view.Record.Category is "Project.TRIGGER" or "Project.ACTION";
        EventTypePicker.Visibility = view.Record.File.Signature == 0xD2 || projectEvent ? Visibility.Visible : Visibility.Collapsed;
        if (view.Record.File.Signature == 0xD2 || projectEvent)
        {
            var signature = projectEvent ? view.Record.Category == "Project.TRIGGER" ? 0xD5u : 0xD6u : System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(view.Record.File.Data.Span[view.Record.Offset..]);
            var type = view.Record.File.Data.Span[view.Record.Offset + (projectEvent ? 54 : 22)];
            var choices = LegacyEventSchemas.All.Where(item => item.RecordSignature == signature).OrderBy(item => item.Type)
                .Select(item => new EventTypeView(item, $"{item.Type:X2} · {LocalizationService.Get("Event." + item.Name, item.Name)}")).ToArray();
            EventTypePicker.ItemsSource = choices;
            EventTypePicker.SelectedItem = choices.FirstOrDefault(item => item.Type.Type == type);
        }
        updatingEventType = false;
        RecordInfo.Text = $"{view.Record.Category} · {view.Record.Length} B · {System.IO.Path.GetFileName(view.Record.File.SourcePath)}";
        RefreshBandPanel(view.Record);
        PopulateRecordFields(view.Record);
    }

    private void PopulateRecordFields(NativeRecord record)
    {
        var selectedSlot = BandPanel.Visibility == Visibility.Visible ? (BandSlotPicker.SelectedItem as BandSlotView)?.Index ?? -1 : -1;
        IEnumerable<NativeField> selectedFields = record.Fields;
        if (NativeMapDocument.IsBand(record))
        {
            if (record.Category == "GAMEBAND" && selectedSlot >= 0)
            {
                record = records.Single(item => item.File == record.File && item.Offset == record.Offset + 51 + selectedSlot * 32);
                selectedFields = record.Fields;
            }
            else if (record.File.Signature == NativeMapDocument.LegacyBands)
                selectedFields = selectedFields.Where(field => selectedSlot < 0 ? !field.Name.StartsWith("heros[", StringComparison.Ordinal) : field.Name.StartsWith($"heros[{selectedSlot}].", StringComparison.Ordinal));
        }
        var projectEvent = record.Category is "Project.TRIGGER" or "Project.ACTION";
        var oldField = (PropertyGrid.SelectedItem as FieldView)?.Field.Name;
        IReadOnlyList<ValueChoice>? Choices(NativeField field)
        {
            if (ScenarioNamedCatalogues.HasFieldContext(record, field)) return ScenarioNamedCatalogues.ForField(record, field, document);
            var names = document?.ScenarioNameChoices(record, field);
            return names is { Count: > 0 } ? names : EntityReferences.Choices(records, record, field);
        }
        var fields = selectedFields.Where(field => (record.File.Signature != 0xD2 || field.Name != "Type") && (!projectEvent || field.Name is not ("ctrl" or "node.type")))
            .Select(field => new FieldView(record, field, EventFieldLabel(record, field), record.Read(field), catalogueVersion, Choices(field))).ToArray();
        PropertyGrid.ItemsSource = fields;
        PropertyGrid.SelectedItem = fields.FirstOrDefault(field => field.Field.Name == oldField);
        if (PropertyGrid.SelectedIndex < 0) PropertyGrid.SelectedIndex = 0;
        RefreshNamedValue();
        RefreshEffectGenerator(record);
        RefreshCameraPathControl(record);
        RefreshScenarioAliasEvent(record);
    }

    private static string EventFieldLabel(NativeRecord record, NativeField field)
    {
        if (PlayerAIFieldCaptions.ForField(record, field) is { } playerCaption)
            return playerCaption.Name(LocalizationService.CurrentLanguage);
        if(DefinitionCaptionRules.ForRecord(record,field) is {} definitionCaption)
            return definitionCaption.Render(key=>LocalizationService.Get(key));
        if(PlacedThingCaptionRules.ForRecord(record,field) is {} placedCaption)
            return LocalizationService.Get(placedCaption);
        if(SpatialCaptionRules.ForRecord(record,field) is {} spatialCaption)
            return LocalizationService.Get(spatialCaption);
        if(MapMetadataCaptions.ForRecord(record,field) is {} mapCaption)
            return mapCaption.Render(key=>LocalizationService.Get(key));
        var project = record.Category is "Project.TRIGGER" or "Project.ACTION";
        if (record.File.Signature != 0xD2 && !project) return FieldLabel(field.Name);
        if (EntityReferences.From(record).FirstOrDefault(reference => reference.Field == field) is { } reference)
            return LocalizationService.Get("Editor.Reference." + reference.Kind);
        var signature = project ? record.Category == "Project.TRIGGER" ? 0xD5u : 0xD6u : System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[record.Offset..]);
        var type = LegacyEventSchemas.Find(signature, record.File.Data.Span[record.Offset + (project ? 54 : 22)]);
        var name = field.Name.StartsWith("param", StringComparison.Ordinal) ? "Param" + field.Name[5..] : field.Name;
        return type is null ? FieldLabel(name) : LocalizationService.Get("EventParam." + type.Name + "." + name, FieldLabel(name));
    }

    private void EventType_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingEventType || document is null || RecordList.SelectedItem is not RecordView view || EventTypePicker.SelectedItem is not EventTypeView type) return;
        RecordOperation(() =>
        {
            if (!ChangeSelectedScenarioEventType(type.Type, ScenarioEventTypeConfirmer ?? (() => DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("Editor.ChangeEventType"), LocalizationService.Get("Editor.Properties"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)))
                Record_Changed(sender, e);
            return null;
        });
    }

    private bool changingScenarioType;
    public Func<bool>? ScenarioEventTypeConfirmer { get; set; }
    public bool ChangeSelectedScenarioEventType(LegacyEventSchemas.EventType type, Func<bool> confirm)
    {
        ArgumentNullException.ThrowIfNull(confirm);
        if (changingScenarioType || scenarioNamesOwnerClosed || document is null || !CommitFields() || RecordList.SelectedItem is not RecordView selected) return false;
        if (EntityReferences.EventType(selected.Record)?.Name == type.Name) return true;
        var source = document; var context = scenarioNamesContext;
        var request = source.PrepareScenarioEventTypeChange(selected.Record, type);
        changingScenarioType = true;
        try
        {
            if (!confirm() || scenarioNamesOwnerClosed || context != scenarioNamesContext || !ReferenceEquals(source, document)
                || RecordList.SelectedItem is not RecordView current || current.Record.File != selected.Record.File
                || current.Record.Offset != selected.Record.Offset || !CommitFields()) return false;
            source.ApplyScenarioEventTypeChange(request);
            return true;
        }
        finally { changingScenarioType = false; }
    }
    private static string FieldLabel(string name)
    {
        var original = name;
        var plain = FieldNaming.CanonicalName(name);
        var indexes = Regex.Match(plain, @"(?:\[\d+\])+$").Value;
        if (indexes.Length > 0) plain = plain[..^indexes.Length];
        var readable = Regex.Replace(plain, @"(?<=[a-z0-9])(?=[A-Z])", " ");
        var label = LocalizationService.Get("Field." + plain, readable) + indexes;
        if (ValueCatalogues.ArrayElement(original) is { } item) label += " · " + item.Name(LocalizationService.CurrentLanguage);
        var hero = Regex.Match(original, @"^heros\[(\d+)\]\.");
        return hero.Success ? $"{LocalizationService.Get("ProjectKind.Creature")} {int.Parse(hero.Groups[1].Value) + 1} · {label}" : label;
    }
    private void Field_EditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit || e.Row.Item is not FieldView field || e.EditingElement is not TextBox input || input.Text == field.Value || document is null) return;
        try
        {
            // Refresh after WPF completes its row commit, not inside CellEditEnding.
            document.Changed -= DocumentChanged;
            CloseCopyBufferConfirmation();
            document.SetField(field.Record, field.Field, input.Text);
            InvalidateGateGeneration(); RuleConfirmed.IsChecked = false; ruleStatusKey = null; RuleStatus.Text = ""; RuleStatus.ToolTip = null;
            InvalidateVariableEditing();
            InvalidatePlayerEditing();
            InvalidateDefinitionEditing();
            field.Value = input.Text;
            Dispatcher.BeginInvoke(RefreshRecords);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
        { e.Cancel = true; DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("Editor.InvalidValue") + "\n" + exception.Message, LocalizationService.Get("Editor.Properties"), MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { document.Changed += DocumentChanged; }
    }
    public bool CommitFields() => PropertyGrid.CommitEdit(DataGridEditingUnit.Cell, true) && PropertyGrid.CommitEdit(DataGridEditingUnit.Row, true);
    private void ClearNamedValue()
    {
        namedField = null; namedChoices = []; NamedValuePanel.Visibility = Visibility.Collapsed;
        if (NamedValuePicker is not null) NamedValuePicker.ItemsSource = null;
        if (NamedFieldLabel is not null) NamedFieldLabel.Text = "";
        if (NamedImagePickerButton is not null) NamedImagePickerButton.Visibility = Visibility.Collapsed;
    }
    private void PropertySelection_Changed(object sender, SelectionChangedEventArgs e) => RefreshNamedValue();
    private void RefreshNamedValue()
    {
        if (NamedValuePanel is null || PropertyGrid.SelectedItem is not FieldView field) { if (NamedValuePanel is not null) ClearNamedValue(); return; }
        var choices = field.Choices;
        if (choices.Count == 0) { ClearNamedValue(); return; }
        var retainDraft = SameImageField(field) && namedField?.Value == field.Value;
        var selectedChoice = retainDraft ? (NamedValuePicker.SelectedItem as NamedChoiceView)?.Choice.Value ?? field.Value : field.Value;
        var retainedSearch = retainDraft ? NamedSearch.Text : "";
        namedField = field;
        namedChoices = choices.Select(choice => new NamedChoiceView(choice, ChoiceLabel(choice), FieldChoiceIcon(field, choice))).ToArray();
        if (!choices.Any(choice => choice.Value == field.Value))
        {
            var custom = new ValueChoice(field.Value, "Custom / unknown value", "Пользовательское / неизвестное значение");
            namedChoices = [new(custom, ChoiceLabel(custom), FieldChoiceIcon(field, custom)), .. namedChoices];
        }
        NamedFieldLabel.Text = field.Label;
        NamedValuePanel.Visibility = Visibility.Visible;
        NamedImagePickerButton.Visibility = OriginalObjectImages.FieldKind(field.Record, field.Field, 0) is null ? Visibility.Collapsed : Visibility.Visible;
        updatingNamed = true; NamedSearch.Text = retainedSearch; updatingNamed = false;
        FilterNamedValues(selectedChoice);
    }
    private void NamedSearch_Changed(object sender, TextChangedEventArgs e) { if (!updatingNamed && NamedValuePicker is not null) FilterNamedValues(); }
    private void FilterNamedValues(string? selected = null)
    {
        selected ??= (NamedValuePicker.SelectedItem as NamedChoiceView)?.Choice.Value;
        var query = NamedSearch.Text.Trim();
        var views = namedChoices.Where(choice => query.Length == 0 || choice.Label.Contains(query, StringComparison.OrdinalIgnoreCase) || choice.Choice.English.Contains(query, StringComparison.OrdinalIgnoreCase) || choice.Choice.Russian.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        NamedValuePicker.ItemsSource = views;
        NamedValuePicker.SelectedItem = views.FirstOrDefault(choice => choice.Choice.Value == selected);
        if (NamedValuePicker.SelectedIndex < 0) NamedValuePicker.SelectedIndex = 0;
    }
    private void ApplyNamedValue_Click(object sender, RoutedEventArgs e)
    {
        var field = namedField; var choice = (NamedValuePicker.SelectedItem as NamedChoiceView)?.Choice;
        if (document is null || field is null || choice is null || !CommitFields()) return;
        RecordOperation(() => { document.SetField(field.Record, field.Field, choice.Value); return null; });
    }
    private void AddRecord_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || TemplatePicker.SelectedItem is not TemplateView view || !CommitFields()) return;
        if (view.LegacyKind is "Trigger" or "Action" || view.Template?.FileSignature == 0xD2)
        {
            OpenScenarioCreation(view.LegacyKind == "Trigger" || view.Template?.RecordSignature == 0xD5 ? 0xD5u : 0xD6u);
            return;
        }
        if (view.LegacyKind == "Band" || view.Template?.Structure == "GAMEBAND")
        { OpenHeroBand(); return; }
        if (PaintingKind(view) is not null)
        {
            var window = CreateObjectPaintingWindow(); if (window is null) return;
            CancelRegionEditing(); window.Owner = Window.GetWindow(this); window.ShowDialog(); return;
        }
        var element = (ElementPicker.SelectedItem as NamedChoiceView)?.Choice;
        RecordOperation(() =>
        {
            NativeRecord? added = null;
            document.Edit("Add named record", () =>
            {
                added = view.LegacyKind is { } kind ? document.AddEditorLegacyRecord(kind, Canvas.CursorTile, Canvas.PaintTile.Owner) : document.AddRecord(view.Template!, Canvas.CursorTile, Canvas.PaintTile.Owner);
                if (element is not null && ValueCatalogues.PrimaryField(added) is { } field) document.SetField(added, field, element.Value);
            });
            return added;
        });
    }
    private void DuplicateRecord_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || RecordList.SelectedItem is not RecordView view || !CommitFields()) return;
        RecordOperation(() => document.DuplicateRecord(view.Record));
    }
    private void DeleteRecord_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || RecordList.SelectedItem is not RecordView) return;
        var owner = document; var revision = owner.EditRevision;
        if (!CommitFields()) return;
        // A committed field can resize/reclassify records and queue a list refresh.
        // Do not apply Delete to the old row addresses or a narrowed selection.
        if (!ReferenceEquals(document, owner) || owner.EditRevision != revision) { ShowRecordRemovalRefusal("Editor.RemovalChanged"); return; }
        if (RecordList.SelectedItem is not RecordView view) return;
        RecordOperation(() =>
        {
            if (TryRemoveSelectedObjects() || TryRemoveSelectedRegions()) return null;
            if (RecordList.SelectedItems.Count != 1) { ShowRecordRemovalRefusal(); return null; }
            if (NativeMapDocument.IsBand(view.Record)) { DeleteHeroBand(view.Record); return null; }
            if (DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("Editor.DeleteConfirm"), LocalizationService.Get("Editor.Delete"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                document.DeleteRecord(view.Record);
            return null;
        });
    }
    public Action<Exception>? OperationErrorPresenter { get; set; }
    private void RecordOperation(Func<NativeRecord?> operation)
    {
        try
        {
            var category=(CategoryPicker.SelectedItem as CategoryView)?.ID;
            var added = operation();
            if (added is null) return;
            var target=category==BandManagerCategory&&NativeMapDocument.IsBand(added)?BandManagerCategory:added.Category;
            CategoryPicker.SelectedItem = (CategoryPicker.ItemsSource as CategoryView[])?.FirstOrDefault(view => view.ID == target);
            RecordList.SelectedItem = (RecordList.ItemsSource as RecordView[])?.FirstOrDefault(view => view.Record.File == added.File && view.Record.Offset == added.Offset);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or OverflowException or FormatException)
        {
            if(OperationErrorPresenter is {} present)present(exception);
            else DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("Editor.OperationFailed") + "\n" + exception.Message, LocalizationService.Get("Editor.Properties"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private sealed record Choice(int ID, string Label);
    private sealed record CategoryView(string ID, string Label);
    private sealed record TemplateView(RecordTemplate? Template, string Label, string? LegacyKind = null)
    { public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalEditorListImages.For(MapPaletteImageResources.ForTemplate(Template?.Structure??LegacyKind),20); }
    private sealed record EventTypeView(LegacyEventSchemas.EventType Type, string Label);
    private sealed record RecordView(NativeRecord Record, string Label, int Index, uint Version, IReadOnlyList<HeroBandDisplayImageRow>? BandPreview=null)
    { public System.Windows.Media.Imaging.BitmapSource? Icon => BandPreview is null?null:OriginalObjectImages.Get(1212); }
    private sealed record NamedChoiceView(ValueChoice Choice, string Label, System.Windows.Media.Imaging.BitmapSource? Icon = null);
    private sealed class FieldView(NativeRecord record, NativeField field, string label, string value, int gameVersion, IReadOnlyList<ValueChoice>? referenceChoices)
    {
        public NativeRecord Record { get; } = record;
        public NativeField Field { get; } = field;
        public string Label { get; } = label;
        public string Value { get; set; } = value;
        public IReadOnlyList<ValueChoice> Choices => referenceChoices ?? ValueCatalogues.ForField(Record, Field, gameVersion);
        public string KnownValue => Choices.FirstOrDefault(choice => choice.Value == Value)?.Name(LocalizationService.CurrentLanguage) ?? "";
        public string Offset => $"0x{Field.Offset:X}";
    }
}
