using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record PatrolRouteView(EditorPatrolManagerRoute Route, string Label);
    private sealed record PatrolPointView(uint ID, PatrolPoint? Point, string Label)
    { public System.Windows.Media.Imaging.BitmapSource? Icon => Point is null ? null : OriginalEditorListImages.For(EditorListImageResources.PatrolArea); }
    private bool updatingPatrol, patrolPicking, patrolNewPick;
    private uint? patrolFocusID, patrolLastID;
    private long? patrolRouteKey;
    private EditorPatrolManagerSession? patrolManager;
    private PatrolAreaSelectionWindow? patrolSelector;
    private bool patrolOwnerClosed;
    private string? patrolStatusKey;

    private void InitializePatrol()
    {
        PatrolPointList.ItemTemplate = PatrolAreaPicker.ItemTemplate = OriginalObjectImages.NamedTemplate();
        Unloaded+=(_,_)=>{patrolOwnerClosed=true;ResetPatrolEditing();};
        Loaded+=(_,_)=>{patrolOwnerClosed=false;RefreshPatrol();};
        Canvas.RightMapSelectionCompleted += (_, args) =>
        {
            if (patrolOwnerClosed||document is null || (!args.ContinuePath && !args.NewPath) || !CommitFields()) return;
            try
            {
                var target = document.FindPatrolArea(args.Bounds, args.NewPath) ?? throw new ArgumentException("No eligible area contains the chosen rectangle.");
                var point = document.PatrolAreas().Single(item => item.Record.File == target.File && item.Record.Offset == target.Offset);
                if (patrolLastID is null || args.NewPath)
                {
                    patrolLastID = point.ID; patrolFocusID = point.ID;
                    var manager=PatrolManager();manager.Reload();
                    if(point.NextID==0)patrolRouteKey=manager.CreateCursorRoute(target).Key;
                }
                else
                {
                    var previous = document.PatrolAreas().SingleOrDefault(item => item.ID == patrolLastID)
                        ?? throw new InvalidOperationException("Start a new path before continuing it.");
                    document.LinkEditorPatrolAreas(previous.Record, target); patrolLastID = point.ID;
                    PatrolManager().Reload();patrolRouteKey=null;
                }
                RefreshPatrol(); ShowPatrolStatus("Editor.PatrolApplied");
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or OverflowException)
            { PatrolError(exception); }
        };
        WorkspaceTabs.SelectionChanged += (_, args) =>
        {
            if (args.Source != WorkspaceTabs) return;
            if (WorkspaceTabs.SelectedIndex == 3) Canvas.HighlightRegion((PatrolPointList.SelectedItem as PatrolPointView)?.Point?.Region?.Bounds);
            else if (WorkspaceTabs.SelectedIndex == 0) Canvas.HighlightRegion((RegionPicker.SelectedItem as RegionView)?.Region.Bounds);
        };
        Canvas.RegionPicked += (_, args) =>
        {
            if (patrolOwnerClosed||!patrolPicking) return;
            patrolPicking = false;
            try
            {
                var target = document!.FindPatrolArea(args.Bounds, patrolNewPick) ?? throw new ArgumentException("No eligible area contains the chosen rectangle.");
                var point = document.PatrolAreas().Single(item => item.Record.File == target.File && item.Record.Offset == target.Offset);
                if (patrolNewPick && document.PatrolRouteFrom(target).Ambiguous) throw new InvalidOperationException("Choose an area with an unambiguous ID in 1–255.");
                if (!patrolNewPick)
                {
                    var previous = document.PatrolAreas().SingleOrDefault(item => item.ID == patrolLastID)
                        ?? throw new InvalidOperationException("Start a new path before continuing it.");
                    document.LinkEditorPatrolAreas(previous.Record, target);
                    PatrolManager().Reload();patrolRouteKey=null;
                }
                patrolLastID = point.ID;
                if (patrolNewPick)
                {
                    patrolFocusID = point.ID;var manager=PatrolManager();manager.Reload();
                    patrolRouteKey=manager.CreateCursorRoute(target).Key;
                }
                else patrolFocusID ??= point.ID;
                RefreshPatrol(); ShowPatrolStatus("Editor.PatrolApplied"); ShowRegionStatus("Editor.PatrolApplied");
                WorkspaceTabs.SelectedIndex = 3;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or OverflowException)
            { PatrolError(exception); ShowRegionStatus("Editor.PatrolFailed"); RegionStatus.ToolTip = exception.Message; }
        };
        Canvas.RegionPickCanceled += (_, _) => { if (patrolPicking) { patrolPicking = false; ShowPatrolStatus("Editor.RegionCanceled"); } };
    }
    private void ResetPatrolEditing()
    {
        ClosePatrolWarning();ClosePatrolRemoval();ClosePatrolPause();
        var selector=patrolSelector;patrolSelector=null;selector?.Close();
        patrolManager?.Dispose();patrolManager=null;patrolRouteKey=null;
        patrolPicking = false; patrolFocusID = patrolLastID = null; patrolStatusKey = null;
        PatrolStatus.ClearValue(TextBlock.TextProperty); PatrolStatus.ToolTip = null;
        PatrolWait.Text = "0";
    }
    private EditorPatrolManagerSession PatrolManager()
    {
        if(patrolOwnerClosed||document is null)throw new InvalidOperationException("Open a map before editing patrols.");
        if(patrolManager is null||patrolManager.IsClosed||!ReferenceEquals(patrolManager.Document,document))
        {patrolManager?.Dispose();patrolManager=new(document);patrolRouteKey=null;}
        return patrolManager;
    }
    private static string PatrolName(PatrolPoint point)
    {
        var name = point.Record.Fields.FirstOrDefault(field => field.Name == "name");
        var info = point.Record.Fields.FirstOrDefault(field => field.Name == "info");
        return (name is null ? "" : point.Record.Read(name)) + (info is null || point.Record.Read(info).Length == 0 ? "" : " · " + point.Record.Read(info));
    }
    private string PatrolPointLabel(PatrolPoint point) => $"#{point.ID} · {point.WaitDelay} · " + PatrolName(point)
        + (point.Region is { } region ? $" ({region.Bounds.X}, {region.Bounds.Y})" : "")
        + (point.NextID == 0 ? "" : " → #" + point.NextID) + $" · 0x{point.Record.Offset:X}";
    private void RefreshPatrol()
    {
        if (patrolOwnerClosed||PatrolRouteList is null||patrolManager?.IsEditing==true) return;
        updatingPatrol = true;
        try
        {
            var selected = (PatrolRouteList.SelectedItem as PatrolRouteView)?.Route.Key;
            var candidate = (PatrolAreaPicker.SelectedItem as PatrolPointView)?.ID;
            var areas = document?.PatrolAreas() ?? [];
            IReadOnlyList<EditorPatrolManagerRoute> routes = document is null?[]:PatrolManager().GetRoutes();
            var views = routes.Select(route => new PatrolRouteView(route, (route.Points.Count==0?LocalizationService.Get("Editor.PatrolEmptyRoute"):string.Join(" → ", route.Points.Select(point => "#" + point.ID)))
                + (route.Ambiguous ? " · " + LocalizationService.Get("Editor.PatrolAmbiguous") : route.Closed ? " · " + LocalizationService.Get("Editor.PatrolLoop")
                    : route.Points.Any(point=>point.Point is null) ? " · " + LocalizationService.Get("Editor.PatrolMissing") : ""))).ToArray();
            PatrolRouteList.ItemsSource = views;
            PatrolRouteList.SelectedItem = views.FirstOrDefault(view=>view.Route.Key==(patrolRouteKey??selected))
                ?? views.FirstOrDefault(view=>view.Route.Points.Any(point=>point.ID==patrolFocusID));
            if (PatrolRouteList.SelectedIndex < 0 && views.Length > 0) PatrolRouteList.SelectedIndex = 0;
            PatrolAreaPicker.ItemsSource = areas.Select(point => new PatrolPointView(point.ID, point, PatrolPointLabel(point))).ToArray();
            PatrolAreaPicker.SelectedItem = PatrolAreaPicker.Items.Cast<PatrolPointView>().FirstOrDefault(view => view.ID == candidate);
            if (PatrolAreaPicker.SelectedIndex < 0 && areas.Count > 0) PatrolAreaPicker.SelectedIndex = 0;
            PatrolPanel.IsEnabled = document is not null;
            if (patrolStatusKey is { } key) ShowPatrolStatus(key);
        }
        finally { updatingPatrol = false; }
        RefreshPatrolPoints();
    }
    private void RefreshPatrolPoints(bool selectRoot=false)
    {
        var wasUpdating=updatingPatrolPoints;updatingPatrolPoints=true;
        try
        {
        var index = selectRoot?-1:PatrolPointList.SelectedIndex;
        var route = (PatrolRouteList.SelectedItem as PatrolRouteView)?.Route;
        var views = route?.Points.Select(item => new PatrolPointView(item.ID, item.Point,item.Point is{ } point?PatrolPointLabel(point):"#"+item.ID+" · "+LocalizationService.Get("Editor.PatrolMissing"))).ToList() ?? [];
        PatrolPointList.ItemsSource = views;
        PatrolPointList.SelectedIndex = views.Count == 0 || index<0 ? -1 : Math.Clamp(index, 0, views.Count - 1);
        RefreshPatrolWarning();
        RefreshPatrolButtons();
        }
        finally{updatingPatrolPoints=wasUpdating;}
    }
    private void RefreshPatrolButtons()
    {
        if(PatrolNewButton is null)return;
        var route=(PatrolRouteList.SelectedItem as PatrolRouteView)?.Route;
        var availability=patrolOwnerClosed||document is null||patrolManager is null||patrolManager.IsClosed
            ?new EditorPatrolCommandAvailability(false,false,false,false,false,false)
            :patrolManager.GetAvailability(route,PatrolPointList.SelectedIndex);
        PatrolNewButton.IsEnabled=availability.New;
        PatrolAppendButton.IsEnabled=availability.Append;
        PatrolInsertButton.IsEnabled=availability.Insert;
        PatrolPauseButton.IsEnabled=availability.Pause;
        PatrolRemoveButton.IsEnabled=availability.RemovePoint;
        PatrolClearButton.IsEnabled=availability.Clear;
        RefreshPatrolRemoval();
        var target=(PatrolAreaPicker.SelectedItem as PatrolPointView)?.Point;
        PatrolStartButton.IsEnabled=availability.New&&target is{NextID:0,ID:>0 and <=255}
            &&document!.PatrolAreas().Count(point=>point.ID==target.ID)==1;
    }
    private void RefreshPatrolWarning()
    {
        var key=patrolManager is{IsClosed:false}&&patrolManager.RequiresReload?"Editor.PatrolReloadRequired":null;
        PatrolWarning.Visibility=key is null?Visibility.Collapsed:Visibility.Visible;
        if(key is not null)PatrolWarning.SetResourceReference(TextBlock.TextProperty,key);
        PatrolWarningReset.IsEnabled=patrolWarningPolicy.Suppressed;
    }
    private void PatrolReload_Click(object sender,RoutedEventArgs e)
    {
        if(patrolOwnerClosed||document is null||!CommitFields())return;
        PatrolManager().Reload();patrolRouteKey=null;RefreshPatrol();ShowPatrolStatus("Editor.PatrolReloaded");
    }
    private void PatrolRoute_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingPatrol) return;
        if(!updatingPatrolPoints){ClosePatrolRemoval();ClosePatrolPause();}
        CancelRegionEditing();patrolRouteKey=(PatrolRouteList.SelectedItem as PatrolRouteView)?.Route.Key;
        patrolFocusID = (PatrolRouteList.SelectedItem as PatrolRouteView)?.Route.Points.FirstOrDefault()?.ID;
        RefreshPatrolPoints(selectRoot:true);
        PatrolSelectionWarning();
    }
    private void PatrolPoint_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingPatrol) return;
        if(!updatingPatrolPoints){ClosePatrolRemoval();ClosePatrolPause();}
        var point = (PatrolPointList.SelectedItem as PatrolPointView)?.Point;
        if (WorkspaceTabs.SelectedIndex == 3) Canvas.HighlightRegion(point?.Region?.Bounds);
        if (point is not null) PatrolWait.Text = point.WaitDelay.ToString(CultureInfo.InvariantCulture);
        RefreshPatrolButtons();
        PatrolSelectionWarning();
    }
    private void PatrolArea_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!updatingPatrol && (PatrolAreaPicker.SelectedItem as PatrolPointView)?.Point is { } point) PatrolWait.Text = point.WaitDelay.ToString(CultureInfo.InvariantCulture);
        if(!updatingPatrol)RefreshPatrolButtons();
    }
    private EditorPatrolManagerRoute? SelectPatrolArea(EditorPatrolManagerSession manager,EditorPatrolManagerRoute route,bool insert,int occurrence)
    {
        if(patrolSelector is not null)throw new InvalidOperationException("Finish the current patrol selector.");
        using var selection=insert?manager.SelectInsert(route,occurrence):manager.SelectAppend(route);
        var selector=new PatrolAreaSelectionWindow(manager.Document,selection,
            ()=>!patrolOwnerClosed&&ReferenceEquals(manager,patrolManager)&&ReferenceEquals(document,manager.Document))
            {Owner=Window.GetWindow(this)};
        patrolSelector=selector;
        try{selector.ShowDialog();return selector.Result;}
        finally{if(ReferenceEquals(patrolSelector,selector))patrolSelector=null;}
    }
    private void ShowPatrolStatus(string key)
    { patrolStatusKey = key; if (key != "Editor.PatrolFailed") PatrolStatus.ToolTip = null; PatrolStatus.SetResourceReference(TextBlock.TextProperty, key); }
    private void PatrolError(Exception exception) { ShowPatrolStatus("Editor.PatrolFailed"); PatrolStatus.ToolTip = exception.Message; }
    private void PatrolEdit_Click(object sender, RoutedEventArgs e)
    {
        if (patrolOwnerClosed||document is null || !CommitFields()) return;
        CancelRegionEditing();
        try
        {
            var action = (sender as Button)?.Tag as string;
            var selected = (PatrolRouteList.SelectedItem as PatrolRouteView)?.Route;
            var target = (PatrolAreaPicker.SelectedItem as PatrolPointView)?.Point;
            var manager=PatrolManager();EditorPatrolManagerRoute? result=null;
            var nextOccurrence=PatrolPointList.SelectedIndex;
            int Wait()
            {
                if (!int.TryParse(PatrolWait.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var wait) || wait is < 0 or > 250)
                    throw new ArgumentException("Original patrol pauses must be 0–250.");
                return wait;
            }
            EditorPatrolManagerRoute Route() => selected ?? throw new ArgumentException("Select a patrol route.");
            PatrolPoint Target() => target ?? throw new ArgumentException("Select an area to add.");
            switch (action)
            {
                case "New":result=manager.CreateEmptyRoute();nextOccurrence=-1;break;
                case "Start":
                    if (Target().NextID != 0) throw new InvalidOperationException("Start a new path from an unlinked area.");
                    result=manager.Start(Target().Record,Wait());patrolLastID=Target().ID;nextOccurrence=0;break;
                case "Append":
                    result=SelectPatrolArea(manager,Route(),false,-1);if(result is null)return;
                    nextOccurrence=result.Points.Count-1;break;
                case "Insert":
                    result=SelectPatrolArea(manager,Route(),true,PatrolPointList.SelectedIndex);if(result is null)return;
                    break;
                case "Pause":
                    result=SelectPatrolPause(manager,Route(),PatrolPointList.SelectedIndex);if(result is null)return;
                    break;
                case "Remove":
                    var occurrence = PatrolPointList.SelectedIndex;
                    if(!RemovePatrolWithConfirmation(manager,Route(),false,occurrence,out result))return;
                    break;
                case "Clear":
                    if(!RemovePatrolWithConfirmation(manager,Route(),true,-1,out result))return;
                    break;
                default: return;
            }
            if(manager.IsClosed||!ReferenceEquals(manager,patrolManager)||!ReferenceEquals(document,manager.Document))return;
            patrolRouteKey=result?.Key;patrolFocusID=result?.Points.FirstOrDefault()?.ID;
            RefreshPatrol();
            SelectPatrolOccurrence(result is{Points.Count:>0}?Math.Clamp(nextOccurrence,0,result.Points.Count-1):-1);
            ShowPatrolStatus("Editor.PatrolApplied");
            if(action is "Remove" or "Clear")RequestPatrolWarning(manager,true);
            else if(action is "New" or "Start" or "Append" or "Insert")PatrolSelectionWarning();
        }
        catch(PatrolPauseCommittedException exception)
        {
            if(!patrolOwnerClosed&&patrolManager is{IsClosed:false}&&ReferenceEquals(exception.Manager,patrolManager)&&ReferenceEquals(document,exception.Manager.Document))
            {RefreshPatrol();ShowPatrolStatus("Editor.PatrolPauseCommitted");PatrolStatus.ToolTip=exception.InnerException?.Message;}
        }
        catch(PatrolRemovalCommittedException exception)
        {
            if(!patrolOwnerClosed&&patrolManager is{IsClosed:false}&&ReferenceEquals(exception.Manager,patrolManager)&&ReferenceEquals(document,exception.Manager.Document))
            {RefreshPatrol();RequestPatrolWarning(patrolManager,true);ShowPatrolStatus("Editor.PatrolRemovalCommitted");PatrolStatus.ToolTip=exception.InnerException?.Message;}
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or OverflowException) { PatrolError(exception); }
    }
    private void PatrolShow_Click(object sender, RoutedEventArgs e)
    {
        if (patrolOwnerClosed||(PatrolPointList.SelectedItem as PatrolPointView)?.Point?.Region is not { } region) return;
        try { WorkspaceTabs.SelectedIndex = 0; Canvas.SetSelection(region.Bounds); Canvas.HighlightRegion(region.Bounds); }
        catch (ArgumentException exception) { PatrolError(exception); }
    }
    private void PatrolProperties_Click(object sender, RoutedEventArgs e)
    {
        if (patrolOwnerClosed||(PatrolPointList.SelectedItem as PatrolPointView)?.Point is not { } point || !CommitFields()) return;
        RecordSearch.Text = ""; RecordOperation(() => point.Record); WorkspaceTabs.SelectedIndex = 1;
    }
    private void PatrolDraw_Click(object sender, RoutedEventArgs e)
    {
        if (patrolOwnerClosed||document is null || !CommitFields()) return;
        CancelRegionEditing(); Canvas.BeginRegionPick(false);
        patrolPicking = true; patrolNewPick = (sender as Button)?.Tag as string == "Start";
        ShowPatrolStatus("Editor.PatrolDrawHint"); ShowRegionStatus("Editor.PatrolDrawHint"); WorkspaceTabs.SelectedIndex = 0;
    }
    private void PatrolToggle_Click(object sender, RoutedEventArgs e)
        => ConfigureOverlays(OverlayOptions with { Flags = PatrolToggle.IsChecked == true ? OverlayOptions.Flags | MapOverlayFlags.Paths : OverlayOptions.Flags & ~MapOverlayFlags.Paths });
}
