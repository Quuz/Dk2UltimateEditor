using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Actual-control acceptance prepared for the user's authorized UI run.
    private static void CheckObjectManagerNavigation(bool native,string language)
    {
        var d=NativeMapDocument.CreateNewMap(new("ManagerNavigation","ManagerNavigation",16,14,NativeFormat:native));
        d.ApplyObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Creature) with {Owner=3},new(Position:new(4,5),Flags:(ObjectBatchFlags)127)),[0]);
        var before=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var history=d.GetHistory();var revision=d.EditRevision;
        var workspace=new EditorWorkspace {Document=d};var host=new Window{Content=workspace};
        workspace.SetSelection(new(2,2,3,3));var canvas=Control<MapCanvas>(workspace,"Canvas");var originalSelection=canvas.Selection;
        var manager=workspace.CreateObjectManagerWindow()!;ObjectManagerMapWindow? palette=null;
        manager.NavigationPresenter=(window,_)=>palette=window;
        ShowManagerCallerWindow(manager);
        T Field<T>(string name)=>(T)manager.FindName(name);
        void Pump()=>manager.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
        void Exact()
        {
            Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&d.EditRevision==revision&&d.GetHistory().Undo.SequenceEqual(history.Undo)
                &&d.EditorChangeCount==history.EditorChangeCount&&canvas.Selection==originalSelection,"Manager navigation edited bytes/history/counters/selection.");
        }
        Expect(!Field<Button>("ManagerShowSelected").IsEnabled&&!manager.ShowSelectedOnMap(),"Empty selection allowed CenterView.");
        Field<DataGrid>("ManagerObjects").SelectedIndex=0;
        Expect(Field<Button>("ManagerShowSelected").IsEnabled&&manager.ShowSelectedOnMap()&&canvas.CursorTile==new TilePoint(4,5),"Selected object navigation failed.");
        Expect(manager.IsNavigating&&!manager.IsVisible&&canvas.HighlightedRegion==new TileRect(4,5,1,1),"View did not hide manager/highlight target.");
        palette!.Close();Pump();Expect(!manager.IsNavigating&&manager.IsVisible&&canvas.HighlightedRegion is null,"Return did not restore manager/clear highlight.");Exact();
        Field<TextBox>("ManagerDestinationX").Text="6";Field<TextBox>("ManagerDestinationY").Text="7";
        Expect(manager.ShowDestinationOnMap()&&canvas.CursorTile==new TilePoint(6,7),"Destination navigation failed.");palette!.Close();Pump();Exact();
        Field<TextBox>("ManagerDestinationX").Text="-1";
        Expect(Field<Button>("ManagerShowDestination").IsEnabled&&manager.ShowDestinationOnMap()&&canvas.CursorTile==new TilePoint(0,7),"Source unsigned input did not navigate to accepted zero.");palette!.Close();Pump();Exact();
        Field<TextBox>("ManagerDestinationX").Text="6";
        Expect(manager.BeginPointPick()&&canvas.IsPickingRegion,"Point picker did not start.");
        Expect(!manager.PreviewPointPick(new(1,1),-1),"Stale picker token accepted a point.");
        canvas.CompleteRegionPick(new(8,9),new(10,11));
        Expect(manager.HasPendingPointPick&&canvas.IsPickingRegion&&Field<TextBox>("ManagerDestinationX").Text=="6"&&Field<TextBox>("ManagerDestinationY").Text=="7","Click committed before source OK.");Exact();
        // Original MAP_CHOICELMBPOINT ignores RMB. Exercise the real pointer route.
        typeof(MapCanvas).GetMethod("BeginRightMapPointer",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(canvas,[new Point(1,1)]);
        Expect(canvas.IsPickingRegion&&manager.HasPendingPointPick,"Right click canceled source point choice.");
        canvas.CompleteRegionPick(new(9,8),new(9,8));
        LocalizationService.SetLanguage(language=="en"?"ru":"en");
        Expect(((Button)palette!.FindName("ManagerUsePoint")).Content as string==LocalizationService.Get("Manager.UsePoint"),"Pending point choice lost live translation.");
        ((Button)palette!.FindName("ManagerUsePoint")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();
        Expect(!manager.IsNavigating&&!canvas.IsPickingRegion&&Field<TextBox>("ManagerDestinationX").Text=="9"&&Field<TextBox>("ManagerDestinationY").Text=="8","Confirmed point did not update shared destination.");Exact();
        Expect(manager.BeginPointPick(),"Second point choice failed.");canvas.CompleteRegionPick(new(5,4),new(5,4));palette!.Close();Pump();
        Expect(Field<TextBox>("ManagerDestinationX").Text=="9"&&Field<TextBox>("ManagerDestinationY").Text=="8"&&!canvas.IsPickingRegion,"Cancel committed private point or leaked canvas mode.");Exact();
        var child=manager.CreateAdditionWindow()!;
        Expect(((TextBox)child.FindName("BatchX")).Text=="9"&&((TextBox)child.FindName("BatchY")).Text=="8","Chosen destination was not shared with addition.");child.Cancel();
        Expect(manager.BeginPointPick(),"Stale point fixture failed.");
        d.Paint([new(6,6)],new(5,2,1));Pump();Expect(!manager.IsNavigating&&!canvas.IsPickingRegion,"Document revision left stale point active.");d.Undo();
        Expect(Field<TextBox>("ManagerDestinationX").Text=="9","Stale callback changed destination.");
        Expect(manager.BeginPointPick(),"Close fixture failed.");manager.Cancel();Pump();Expect(!canvas.IsPickingRegion&&!manager.IsVisible,"Closed manager retained picker/restored itself.");
        var reopened=workspace.CreateObjectManagerWindow()!;
        Expect(((TextBox)reopened.FindName("ManagerDestinationX")).Text=="9","Session destination was lost on reopen.");reopened.Cancel();
        var failing=new ObjectManagerWindow(d,()=>canvas.Selection,()=>true,viewRequester:(_,_)=>throw new OutOfMemoryException("Controlled navigation callback"));
        ShowManagerCallerWindow(failing);((DataGrid)failing.FindName("ManagerObjects")).SelectedIndex=0;
        var refused=false;try{failing.ShowSelectedOnMap();}catch(OutOfMemoryException){refused=true;}
        failing.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
        Expect(refused&&!failing.IsNavigating&&failing.IsVisible,"Unexpected view failure stranded a hidden manager.");failing.Cancel();
        host.Close();LocalizationService.SetLanguage(language);
    }
}
