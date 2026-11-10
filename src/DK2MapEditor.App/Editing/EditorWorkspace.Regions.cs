using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record RegionView(MapRegion Region,string Label,byte[] Payload);
    private sealed record PendingRegion(MapRegionKind Kind, NativeRecord? Record, byte[]? Payload, bool SelectOnly = false);
    private PendingRegion? pendingRegion;
    private bool updatingRegions;
    private MapRegionKind? regionManagerKind;
    private string? regionStatusKey;
    public Action<RegionPropertiesWindow>? RegionPropertiesPresenter {get;set;}
    private RegionPropertiesWindow? regionPropertiesWindow;
    private NativeRecord? regionPropertiesTarget;
    private long regionPropertiesContext;
    private readonly Dictionary<MapRegionKind,(uint Colour,uint TextColour)> regionCreationColours=[];
    private void CloseRegionPropertiesWindow()
    {regionPropertiesContext++;var window=regionPropertiesWindow;regionPropertiesWindow=null;regionPropertiesTarget=null;window?.Close();}

    private void InitializeRegions()
    {
        RegionKindPicker.SelectionChanged+=(_,_)=>
        {
            if(!updatingRegions&&regionManagerKind is not null&&RegionKindPicker.SelectedValue is int kind&&Enum.IsDefined((MapRegionKind)kind))
            {regionManagerKind=(MapRegionKind)kind;RefreshRegions();}
        };
        Canvas.RegionPicked += (_, args) => ApplyPickedRegion(args.Bounds);
        Canvas.RegionPickCanceled += (_, _) => { pendingRegion = null; ShowRegionStatus("Editor.RegionCanceled"); };
    }
    private void RefreshRegions()
    {
        if (RegionPicker is null) return;
        updatingRegions = true;
        try
        {
            var selected = (RegionPicker.SelectedItem as RegionView)?.Region.Record;
            var kind = RegionKindPicker.SelectedValue is int value ? value : 0;
            RegionKindPicker.ItemsSource = Enum.GetValues<MapRegionKind>().Select(value => new Choice((int)value, LocalizationService.Get("Editor.RegionKind." + value))).ToArray();
            RegionKindPicker.SelectedValue = kind;
            var gateIDs = document is { IsLegacyProject: false } ? document.ScenarioGateChoices().ToDictionary(choice => choice.Key, choice => choice.WorkingID) : [];
            string Label(MapRegion region)
            {
                var record = region.Record;
                var idField = record.Fields.FirstOrDefault(field => field.Name is "id" or "bAreaID" or "bGateID" or "bEffectID");
                var nameField = record.Fields.FirstOrDefault(field => field.Name == "name");
                var name = nameField is null ? "" : record.Read(nameField);
                var id = record.Category == "GAMEGATE" && gateIDs.TryGetValue(record.File.ExportPath + ":" + record.Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), out var gateID)
                    ? gateID.ToString(System.Globalization.CultureInfo.InvariantCulture) : idField is null ? "" : record.Read(idField);
                return LocalizationService.Get("Editor.RegionKind." + region.Kind) + (id.Length == 0 ? "" : " #" + id)
                    + (string.IsNullOrWhiteSpace(name) ? "" : " · " + name)
                    + $" · ({region.Bounds.X}, {region.Bounds.Y}) {region.Bounds.Width} × {region.Bounds.Height} · 0x{record.Offset:X}";
            }
            var views = (document?.MapRegions() ?? [])
                .Where(region=>regionManagerKind is null||region.Kind==regionManagerKind)
                .Where(region => region.Record.Category != "GAMEGATE" || region.Record.Read(region.Record.Fields.Single(field => field.Name == "bGateType")) != "14")
                .Select(region => new RegionView(region,Label(region),region.Record.File.Data.Slice(region.Record.Offset,region.Record.Length).ToArray())).ToArray();
            RegionPicker.ItemsSource = views;
            RegionPicker.SelectedItem = views.FirstOrDefault(view => view.Region.Record.File == selected?.File && view.Region.Record.Offset == selected.Offset && view.Region.Record.Category == selected.Category);
            if (RegionPicker.SelectedIndex < 0 && views.Length > 0) RegionPicker.SelectedIndex = 0;
            RegionPanel.IsEnabled = document is not null;
            RefreshRegionManagerActions();
            Canvas.HighlightRegion((RegionPicker.SelectedItem as RegionView)?.Region.Bounds);
            if (regionStatusKey is { } key) ShowRegionStatus(key);
        }
        finally { updatingRegions = false; }
        RefreshPatrol();
        RefreshGateGeneration();
        RefreshRuleMatrix();
        RefreshVariables();
        RefreshPlayers();
        RefreshStatistics();
        RefreshDefinitions();
    }
    private void Region_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingRegions) return;
        RefreshRegionManagerActions();
        CloseRegionRemovalWindow();
        CancelRegionEditing(); Canvas.HighlightRegion((RegionPicker.SelectedItem as RegionView)?.Region.Bounds);
    }
    private void RefreshRegionManagerActions()
    {
        var selected=RegionPicker.SelectedItem is RegionView;
        RegionShowButton.IsEnabled=RegionPropertiesButton.IsEnabled=RegionDeleteButton.IsEnabled=selected;
    }
    private void ShowRegionStatus(string key)
    {
        regionStatusKey = key;
        if (key != "Editor.RegionFailed") RegionStatus.ToolTip = null;
        RegionStatus.SetResourceReference(TextBlock.TextProperty, key); RegionStatus.Visibility = Visibility.Visible;
    }
    public bool CancelRegionEditing()
    {
        var pending = pendingRegion is not null; pendingRegion = null;
        return Canvas.CancelRegionPick() || pending;
    }
    private void RegionCancel_Click(object sender, RoutedEventArgs e) => CancelRegionEditing();
    private void RegionShow_Click(object sender, RoutedEventArgs e)
    {
        if (RegionPicker.SelectedItem is not RegionView view) return;
        try { Canvas.SetSelection(view.Region.Bounds); Canvas.BringIntoView(Canvas.CellBounds(view.Region.Bounds)); }
        catch (ArgumentException) { ShowRegionStatus("Editor.RegionInvalidBounds"); }
    }
    private void RegionProperties_Click(object sender, RoutedEventArgs e)
    {
        if(RegionPicker.SelectedItem is RegionView view)OpenRegionProperties(view);
    }
    private void RegionList_DoubleClick(object sender,MouseButtonEventArgs args)
    {
        if(args.ChangedButton!=MouseButton.Left||args.OriginalSource is not DependencyObject origin
            ||ItemsControl.ContainerFromElement(RegionPicker,origin) is not ListBoxItem row
            ||!ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(row),RegionPicker)
            ||row.Content is not RegionView view)return;
        // Use the actual row, not a possibly different SelectedItem. Blank
        // space, scrollbar chrome and containers from an older refresh refuse.
        if(OpenRegionProperties(view))args.Handled=true;
    }
    private static bool SameRegionRecord(NativeRecord? first,NativeRecord? second)=>first is not null&&second is not null
        &&ReferenceEquals(first.File,second.File)&&first.Offset==second.Offset&&first.Length==second.Length&&first.Category==second.Category;
    private bool OpenRegionProperties(RegionView view)
    {
        if(document is not {} source||scenarioNamesOwnerClosed||source.HasActivePenStroke||WorkspaceTabs.SelectedIndex!=0
            ||!RegionPicker.Items.Cast<RegionView>().Any(item=>ReferenceEquals(item,view)))return false;
        var revision=source.EditRevision;var context=regionPropertiesContext;var record=view.Region.Record;
        bool PayloadCurrent()=>record.Offset>=0&&record.Length>=0&&record.Offset<=record.File.Length-record.Length
            &&record.File.Data.Span.Slice(record.Offset,record.Length).SequenceEqual(view.Payload);
        if(!PayloadCurrent()||!CommitFields()||!ReferenceEquals(source,document)||scenarioNamesOwnerClosed
            ||context!=regionPropertiesContext||revision!=source.EditRevision||source.HasActivePenStroke||!PayloadCurrent())return false;
        if(regionPropertiesWindow is {} previous)
        {
            if(previous.IsDraftCurrent)
            {
                if(!SameRegionRecord(regionPropertiesTarget,record)){ShowRegionStatus("RegionProperties.FinishCurrent");return false;}
                previous.EndPointPick();previous.Activate();return true;
            }
            CloseRegionPropertiesWindow();context=regionPropertiesContext;
        }
        var opened=false;
        RecordOperation(()=>
        {
            RegionPicker.SelectedItem=view;
            if(!ReferenceEquals(source,document)||scenarioNamesOwnerClosed||context!=regionPropertiesContext
                ||revision!=source.EditRevision||source.HasActivePenStroke||!PayloadCurrent())return null;
            var window=new RegionPropertiesWindow(source,record,()=>ReferenceEquals(source,document)&&context==regionPropertiesContext&&!scenarioNamesOwnerClosed,CommitFields,()=>Canvas.Selection,RequestPreferences)
                {Owner=Window.GetWindow(this)};
            regionPropertiesWindow=window;regionPropertiesTarget=record;window.PointRequester=parent=>BeginRegionPropertiesPoint(parent,source,context);
            window.Closed+=(_,_)=>{if(ReferenceEquals(regionPropertiesWindow,window)){regionPropertiesWindow=null;regionPropertiesTarget=null;}};
            if(RegionPropertiesPresenter is {} present)present(window);else window.Show();
            opened=true;RefreshRegions();return record;
        });
        return opened;
    }
    private void RegionAdd_Click(object sender,RoutedEventArgs e)
    {
        if(document is null||!CommitFields())return;
        var kind=(MapRegionKind)(RegionKindPicker.SelectedValue is int value?value:0);
        var bounds=Canvas.Selection??new TileRect(0,0,1,1);
        if(kind==MapRegionKind.Gate)bounds=new(bounds.X,bounds.Y,1,1);
        OpenRegionCreation(kind,bounds);
    }
    private void OpenRegionCreation(MapRegionKind kind,TileRect bounds)
    {
        if(document is null)return;
        if(regionPropertiesWindow is {} previous)
        {if(previous.IsDraftCurrent){previous.EndPointPick();previous.Activate();return;}CloseRegionPropertiesWindow();}
        var source=document;var context=regionPropertiesContext;
        RecordOperation(()=>
        {
            var creation=source.CreateRegionCreationSession(kind,bounds);
            try
            {
                if(creation.Properties.HasEditorLabels&&regionCreationColours.TryGetValue(kind,out var colours))
                    creation.WorkingDocument.UpdateRegionProperties(creation.Properties,creation.Properties.Values with{Colour=colours.Colour,TextColour=colours.TextColour});
                var window=new RegionPropertiesWindow(source,creation,()=>ReferenceEquals(source,document)&&context==regionPropertiesContext&&!scenarioNamesOwnerClosed,
                    CommitFields,()=>Canvas.Selection,RequestPreferences){Owner=Window.GetWindow(this)};
                regionPropertiesWindow=window;regionPropertiesTarget=null;window.PointRequester=parent=>BeginRegionPropertiesPoint(parent,source,context);
                window.RegionCreated+=record=>
                {
                    if(!ReferenceEquals(source,document)||context!=regionPropertiesContext)return;
                    RefreshRegions();RegionPicker.SelectedItem=RegionPicker.Items.Cast<RegionView>().FirstOrDefault(view=>view.Region.Record.File==record.File&&view.Region.Record.Offset==record.Offset);
                    ShowRegionStatus("Editor.RegionApplied");
                };
                window.Closed+=(_,_)=>
                {
                    regionCreationColours[kind]=window.DraftColours;
                    if(ReferenceEquals(regionPropertiesWindow,window)){regionPropertiesWindow=null;regionPropertiesTarget=null;}
                };
                if(RegionPropertiesPresenter is {} present)present(window);else window.Show();
            }
            catch{creation.Dispose();throw;}
            return null;
        });
    }
    private void BeginRegionPropertiesPoint(RegionPropertiesWindow parent,NativeMapDocument source,long context)
    {
        if(!ReferenceEquals(source,document)||context!=regionPropertiesContext||!parent.IsDraftCurrent){parent.EndPointPick();return;}
        CancelRegionEditing();Canvas.BeginRegionPick(parent.IsPointPick);
        // Finishing an older live stroke may have changed the document while
        // BeginRegionPick switched modes. Do not subscribe an orphaned picker.
        if(!ReferenceEquals(source,document)||context!=regionPropertiesContext||!parent.IsDraftCurrent||!parent.HasPendingPointPick)
        {Canvas.CancelRegionPick();parent.EndPointPick();return;}
        var ended=false;
        void Cleanup(){if(ended)return;ended=true;Canvas.RegionPicked-=Picked;Canvas.RegionPickCanceled-=Canceled;parent.CancelPointPick=null;}
        void Picked(object? sender,MapRegionPickedEventArgs args)
        {
            Cleanup();if(ReferenceEquals(source,document)&&context==regionPropertiesContext&&parent.IsDraftCurrent)parent.AcceptPointPick(args.Bounds);
            else parent.EndPointPick();
        }
        void Canceled(object? sender,EventArgs args){Cleanup();parent.EndPointPick();}
        Canvas.RegionPicked+=Picked;Canvas.RegionPickCanceled+=Canceled;
        parent.CancelPointPick=()=>{Canvas.CancelRegionPick();Cleanup();};
        WorkspaceTabs.SelectedIndex=0;ShowRegionStatus(parent.IsPointPick?"RegionProperties.PickPointHint":"RegionProperties.PickRectangleHint");
        Canvas.Focus();Window.GetWindow(this)?.Activate();
    }
    private void RegionUseSelection_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields()) return;
        if (Canvas.Selection is not { } bounds) { ShowRegionStatus("Editor.RegionNeedSelection"); return; }
        var add = (sender as Button)?.Tag as string == "Add";
        var selected = RegionPicker.SelectedItem as RegionView;
        if (!add && selected is null) { ShowRegionStatus("Editor.RegionNeedRecord"); return; }
        var kind = add ? (MapRegionKind)(RegionKindPicker.SelectedValue is int value ? value : 0) : selected!.Region.Kind;
        if (kind == MapRegionKind.Gate) bounds = new(bounds.X, bounds.Y, 1, 1);
        ApplyRegion(new(kind, add ? null : selected!.Region.Record, null), bounds);
    }
    private void RegionChoose_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !CommitFields()) return;
        var action = (sender as Button)?.Tag as string;
        var selected = RegionPicker.SelectedItem as RegionView;
        if (action == "Modify" && selected is null) { ShowRegionStatus("Editor.RegionNeedRecord"); return; }
        var kind = action == "Modify" ? selected!.Region.Kind : (MapRegionKind)(RegionKindPicker.SelectedValue is int value ? value : 0);
        var record = action == "Modify" ? selected!.Region.Record : null;
        var pending = new PendingRegion(kind, record, record?.File.Data.Slice(record.Offset, record.Length).ToArray(), action == "Select");
        CancelRegionEditing(); Canvas.BeginRegionPick(pending.SelectOnly || kind == MapRegionKind.Gate); pendingRegion = pending;
        ShowRegionStatus(pending.SelectOnly ? "Editor.RegionPickRecord" : kind == MapRegionKind.Gate ? "Editor.RegionPickPoint" : "Editor.RegionPickRectangle");
    }
    private void ApplyPickedRegion(TileRect bounds)
    {
        var pending = pendingRegion; pendingRegion = null;
        if (pending is null || document is null) return;
        if (!pending.SelectOnly) { ApplyRegion(pending, bounds); return; }
        var candidates = RegionPicker.Items.Cast<RegionView>().Where(view => view.Region.Bounds.Contains(bounds.X, bounds.Y)).ToArray();
        if (candidates.Length == 0) { ShowRegionStatus("Editor.RegionNoHit"); return; }
        var index = Array.IndexOf(candidates, RegionPicker.SelectedItem as RegionView);
        RegionPicker.SelectedItem = candidates[(index + 1) % candidates.Length]; ShowRegionStatus("Editor.RegionSelected");
    }
    private void ApplyRegion(PendingRegion pending, TileRect bounds)
    {
        try
        {
            NativeRecord record;
            if (pending.Record is { } existing)
            {
                ReadOnlyMemory<byte>? snapshot = null;
                if (pending.Payload is { } payload) snapshot = new ReadOnlyMemory<byte>(payload);
                document!.SetMapRegionBounds(existing, bounds, snapshot); record = existing;
            }
            else {OpenRegionCreation(pending.Kind,bounds);return;}
            RefreshRegions();
            RegionPicker.SelectedItem = RegionPicker.Items.Cast<RegionView>().FirstOrDefault(view => view.Region.Record.File == record.File && view.Region.Record.Offset == record.Offset);
            ShowRegionStatus("Editor.RegionApplied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or OverflowException)
        { ShowRegionStatus("Editor.RegionFailed"); RegionStatus.ToolTip = exception.Message; }
    }
}
