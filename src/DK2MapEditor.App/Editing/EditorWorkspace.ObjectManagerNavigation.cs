using System.Windows;
using System.Windows.Input;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private void CenterObjectManagerPoint(TilePoint point)
    {
        WorkspaceTabs.SelectedIndex=0;
        MapScroll.ScrollToHorizontalOffset(Math.Max(0,(point.X+0.5)*Canvas.CellWidth-MapScroll.ViewportWidth/2));
        MapScroll.ScrollToVerticalOffset(Math.Max(0,(point.Y+0.5)*Canvas.CellHeight-MapScroll.ViewportHeight/2));
    }
    private Action? ViewObjectManagerPoint(ObjectManagerWindow parent,NativeMapDocument source,TilePoint point)
    {
        var token=parent.NavigationToken;
        CancelRegionEditing();
        if(!ReferenceEquals(source,document)||!parent.NavigationCurrent(token)||!Canvas.NavigateCursorTo(point)||!parent.NavigationCurrent(token))
        {parent.EndNavigation();return null;}
        var previous=Canvas.HighlightedRegion;var highlight=new TileRect(point.X,point.Y,1,1);
        Canvas.HighlightRegion(highlight);CenterObjectManagerPoint(point);
        return ()=>{if(ReferenceEquals(source,document)&&Canvas.HighlightedRegion==highlight)Canvas.HighlightRegion(previous);};
    }
    private void BeginObjectManagerPoint(ObjectManagerWindow parent,NativeMapDocument source)
    {
        var token=parent.NavigationToken;
        CancelRegionEditing();
        if(!ReferenceEquals(source,document)||!parent.NavigationCurrent(token)||!Canvas.NavigateCursorTo(parent.NavigationPoint)||!parent.NavigationCurrent(token))
        {parent.EndNavigation();return;}
        Canvas.BeginRegionPick(true,rightClickCancels:false);
        if(!ReferenceEquals(source,document)||!parent.NavigationCurrent(token)||!parent.HasPendingPointPick)
        {Canvas.CancelRegionPick();parent.EndNavigation();return;}
        var previous=Canvas.HighlightedRegion;var highlight=new TileRect(parent.NavigationPoint.X,parent.NavigationPoint.Y,1,1);
        Canvas.HighlightRegion(highlight);
        var ended=false;Action? cancel=null;
        bool Cleanup()
        {
            if(ended)return false;ended=true;Canvas.RegionPicked-=Picked;Canvas.RegionPickCanceled-=Canceled;Canvas.PreviewKeyDown-=KeyDown;
            if(ReferenceEquals(parent.CancelNavigation,cancel))parent.CancelNavigation=null;
            if(ReferenceEquals(source,document)&&Canvas.HighlightedRegion==highlight)Canvas.HighlightRegion(previous);
            return true;
        }
        void Picked(object? sender,MapRegionPickedEventArgs args)
        {
            if(ended)return;
            if(ReferenceEquals(source,document)&&parent.NavigationCurrent(token)&&parent.PreviewPointPick(new(args.Bounds.X,args.Bounds.Y),token))
            {
                // Source Map_ChoosePointEx keeps a private point until OK. A
                // click ends one canvas gesture, not the enclosing choice.
                highlight=new(args.Bounds.X,args.Bounds.Y,1,1);Canvas.HighlightRegion(highlight);
                Canvas.BeginRegionPick(true,rightClickCancels:false);Canvas.Focus();return;
            }
            Cleanup();if(parent.NavigationToken==token)parent.EndNavigation();
        }
        void Canceled(object? sender,EventArgs args)
        {if(Cleanup()&&parent.NavigationToken==token)parent.EndNavigation();}
        void KeyDown(object sender,KeyEventArgs args)
        {
            if(parent.NavigationCurrent(token)&&args.Key==Key.Enter&&Keyboard.Modifiers==ModifierKeys.None)
            {args.Handled=true;parent.ConfirmPointPick(token);}
        }
        cancel=()=>{if(Cleanup()&&ReferenceEquals(source,document))Canvas.CancelRegionPick();};
        Canvas.RegionPicked+=Picked;Canvas.RegionPickCanceled+=Canceled;Canvas.PreviewKeyDown+=KeyDown;parent.CancelNavigation=cancel;
        CenterObjectManagerPoint(parent.NavigationPoint);Canvas.Focus();Window.GetWindow(this)?.Activate();
    }
}
