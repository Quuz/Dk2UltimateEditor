using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Compiled only. Actual WPF input/notifications/modal behavior is manual.
    private static void CheckManagerDestination(bool native,string language)
    {
        var d=NativeMapDocument.CreateNewMap(new("DestinationUI","Destination UI",20,14,NativeFormat:native));
        var snapshot=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var history=d.GetHistory();var revision=d.EditRevision;
        var current=true;TilePoint? viewed=null;ObjectManagerMapWindow? palette=null;
        var session=new ObjectManagerSession();
        var window=new ObjectManagerWindow(d,()=>null,()=>true,session,()=>current,(_,point)=>{viewed=point;return ()=>{};},_=>{});
        window.NavigationPresenter=(w,_)=>palette=w;
        T Field<T>(string name)=>(T)window.FindName(name);
        void Step(string axis,string direction)=>Field<RepeatButton>("ManagerDestination"+axis+direction).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        void Pump()=>window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
        long Token()=>(long)typeof(ObjectManagerWindow).GetField("navigationToken",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
        void Exact()=>Expect(snapshot.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&d.EditRevision==revision&&d.EditorChangeCount==history.EditorChangeCount
            &&d.GetHistory().Undo.SequenceEqual(history.Undo),"Manager destination input/navigation changed the map.");
        try
        {
            ShowManagerCallerWindow(window);var x=Field<TextBox>("ManagerDestinationX");var y=Field<TextBox>("ManagerDestinationY");
            x.Text="999";y.Text="999";Expect(x.Text=="19"&&y.Text=="13","Manager immediate clamp was not connected to separate extents.");
            Step("X","Increase");Step("Y","Increase");Expect(x.Text=="19"&&y.Text=="13","Spinners crossed the map edge.");
            Step("X","Decrease");Step("Y","Decrease");Expect(x.Text=="18"&&y.Text=="12","Spinners did not step one unit.");
            x.Text="0";Step("X","Decrease");Expect(x.Text=="0","Spinner crossed zero.");
            var textInput=new TextCompositionEventArgs(Keyboard.PrimaryDevice,new TextComposition(InputManager.Current,x,"a")){RoutedEvent=TextCompositionManager.PreviewTextInputEvent};
            x.RaiseEvent(textInput);Expect(textInput.Handled,"Source ES_NUMBER typing restriction was not connected.");
            x.Text="";y.Text="  7tail";Exact();
            var child=window.CreateAdditionWindow()!;
            Expect(((TextBox)child.FindName("BatchX")).Text=="0"&&((TextBox)child.FindName("BatchY")).Text=="7","Child Add did not receive numeric source destination.");child.Cancel();
            Expect(x.Text==""&&y.Text=="  7tail","Returning from an unchanged child destroyed intermediate raw text.");
            Expect(window.ShowDestinationOnMap()&&viewed==new TilePoint(0,7),"Show destination did not read source zero/prefix.");palette!.Close();Pump();Exact();
            x.Text="6";y.Text="7";Expect(window.BeginPointPick(),"Picker did not open.");var token=Token();
            Step("X","Increase");Expect(x.Text=="6"&&!Field<RepeatButton>("ManagerDestinationXIncrease").IsEnabled,"Active picker allowed an old spinner to update the parent.");
            Expect(window.PreviewPointPick(new(9,8),token),"Private picker preview failed.");palette!.Close();Pump();
            Expect(x.Text=="6"&&y.Text=="7","Cancel wrote the private picker coordinates.");Exact();
            Expect(window.BeginPointPick(),"Second picker did not open.");token=Token();Expect(window.PreviewPointPick(new(9,8),token)&&window.ConfirmPointPick(token),"Confirmed picker did not publish a point.");Pump();
            Expect(x.Text=="9"&&y.Text=="8","Confirmed picker did not update parent fields.");
            child=window.CreateAdditionWindow()!;Expect(((TextBox)child.FindName("BatchX")).Text=="9"&&((TextBox)child.FindName("BatchY")).Text=="8","Confirmed point failed to update the shared numeric session.");child.Cancel();Exact();
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Field<RepeatButton>("ManagerDestinationXIncrease").ToolTip.ToString()==LocalizationService.Get("RegionProperties.Increase"),"Spinner tooltip did not translate.");LocalizationService.SetLanguage(language);
            current=false;Step("X","Increase");Expect(x.Text=="9"&&!window.ShowDestinationOnMap(),"Stale owner retained destination command authority.");Exact();
        }
        finally{window.Cancel();LocalizationService.SetLanguage(language);}
    }
}
