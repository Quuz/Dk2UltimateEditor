using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class ObjectManagerWindow
{
    private readonly Func<ObjectManagerWindow,TilePoint,Action?>? viewRequester;
    private readonly Action<ObjectManagerWindow>? pointRequester;
    private readonly TextBox destinationX=new(){Width=70},destinationY=new(){Width=70};
    private readonly Button showSelected=new(),showDestination=new(),choosePoint=new();
    private readonly List<RepeatButton> destinationSteps=[];
    private bool normalizingDestination;
    private bool navigating,picking,restoreNavigationVisibility,resumeNavigationModal;
    private long navigationRevision,navigationToken;
    private TilePoint navigationPoint;
    private ObjectManagerMapWindow? mapWindow;
    internal Action? CancelNavigation {get;set;}
    internal long NavigationToken=>navigationToken;
    internal TilePoint NavigationPoint=>navigationPoint;
    internal bool NavigationCurrent(long token)=>!closed && navigating && navigationToken==token && ownerCurrent() && document.EditRevision==navigationRevision;
    public bool HasPendingPointPick=>navigating&&picking;
    public bool IsNavigating=>navigating;
    public Action<ObjectManagerMapWindow,bool>? NavigationPresenter {get;set;}
    private void InitializeNavigation(Panel top,Panel commands)
    {
        var coordinates=new WrapPanel();top.Children.Add(coordinates);
        session.BoundPosition(document.Width,document.Height);destinationX.Text=session.PositionX;destinationY.Text=session.PositionY;
        foreach(var (input,horizontal,key,name) in new[]{(destinationX,true,"Editor.SelectionX","ManagerDestinationX"),(destinationY,false,"Editor.SelectionY","ManagerDestinationY")})
        {
            Label(coordinates,key);Named(input,name);
            var field=new DockPanel();var arrows=new StackPanel();DockPanel.SetDock(arrows,Dock.Right);field.Children.Add(arrows);field.Children.Add(input);coordinates.Children.Add(field);
            foreach(var (delta,glyph,label) in new[]{(1,"▲","Increase"),(-1,"▼","Decrease")})
            {
                var step=Named(new RepeatButton{Content=glyph,Width=22,Height=15,Padding=new(0),Focusable=false},name+label);
                step.SetResourceReference(ToolTipProperty,"RegionProperties."+label);
                step.SetResourceReference(System.Windows.Automation.AutomationProperties.NameProperty,"RegionProperties."+label);
                step.Click+=(_,_)=>StepDestination(input,horizontal,delta);arrows.Children.Add(step);destinationSteps.Add(step);
            }
            input.TextChanged+=(_,_)=>DestinationChanged(input,horizontal);
            input.PreviewTextInput+=(_,args)=>{if(CanEditDestination&&args.Text.Any(c=>!char.IsAsciiDigit(c)))args.Handled=true;};
            input.PreviewKeyDown+=(_,args)=>
            {if(Keyboard.Modifiers==ModifierKeys.None&&args.Key is Key.Up or Key.Down)
                args.Handled=StepDestination(input,horizontal,args.Key==Key.Up?1:-1);};
        }
        Named(showSelected,"ManagerShowSelected");showSelected.SetResourceReference(ContentControl.ContentProperty,"Manager.ShowSelected");showSelected.Click+=(_,_)=>ShowSelectedOnMap();commands.Children.Add(showSelected);
        Named(showDestination,"ManagerShowDestination");showDestination.SetResourceReference(ContentControl.ContentProperty,"Manager.ShowDestination");showDestination.Click+=(_,_)=>ShowDestinationOnMap();coordinates.Children.Add(showDestination);
        Named(choosePoint,"ManagerChoosePoint");choosePoint.SetResourceReference(ContentControl.ContentProperty,"Manager.ChoosePoint");choosePoint.Click+=(_,_)=>BeginPointPick();coordinates.Children.Add(choosePoint);
    }
    private bool ReadDestination(out TilePoint point)
    {
        point=new(RegionCoordinateRules.Value(destinationX.Text,document.Width),RegionCoordinateRules.Value(destinationY.Text,document.Height));return true;
    }
    private bool CanEditDestination=>!closed&&!navigating&&!managerFlowBusy&&!requestPending&&optionsWindow is null&&ownerCurrent();
    private void DestinationChanged(TextBox input,bool horizontal)
    {
        if(normalizingDestination||!CanEditDestination)return;
        normalizingDestination=true;
        try
        {
            var extent=horizontal?document.Width:document.Height;var text=RegionCoordinateRules.Normalize(input.Text,extent);
            if(input.Text!=text){input.Text=text;input.CaretIndex=text.Length;}
            session.SetDestination(horizontal,input.Text,extent);
        }
        finally{normalizingDestination=false;}
        UpdateNavigationCommands();
    }
    private bool StepDestination(TextBox input,bool horizontal,int delta)
    {
        if(!CanEditDestination)return false;
        input.Text=RegionCoordinateRules.Step(input.Text,horizontal?document.Width:document.Height,delta);input.CaretIndex=input.Text.Length;return true;
    }
    private void RefreshDestinationCoordinates()
    {
        if(closed||navigating)return;
        session.BoundPosition(document.Width,document.Height);normalizingDestination=true;
        try
        {
            // Preserve intermediate raw text when its accepted number already
            // equals the shared session; a child change still updates the field.
            if(RegionCoordinateRules.Value(destinationX.Text,document.Width).ToString(CultureInfo.InvariantCulture)!=session.PositionX)destinationX.Text=session.PositionX;
            if(RegionCoordinateRules.Value(destinationY.Text,document.Height).ToString(CultureInfo.InvariantCulture)!=session.PositionY)destinationY.Text=session.PositionY;
        }
        finally{normalizingDestination=false;}
        UpdateNavigationCommands();
    }
    private void UpdateNavigationCommands()
    {
        var available=!closed&&!navigating&&!managerFlowBusy&&!requestPending&&optionsWindow is null&&ownerCurrent();
        showSelected.IsEnabled=available&&viewRequester is not null&&table.SelectedItems.Count==1;
        showDestination.IsEnabled=available&&viewRequester is not null&&ReadDestination(out _);
        choosePoint.IsEnabled=available&&pointRequester is not null;
        destinationX.IsEnabled=destinationY.IsEnabled=available;
        foreach(var step in destinationSteps)step.IsEnabled=available;
    }
    public bool ShowSelectedOnMap()
    {
        if(table.SelectedItems.Count!=1 || table.SelectedItem is not ManagerRow row)return false;
        var token=selectionRevision;var revision=document.EditRevision;
        if(!PrepareNavigation() || token!=selectionRevision || revision!=document.EditRevision)return false;
        return StartNavigation(row.Entry.Thing.Position,false);
    }
    public bool ShowDestinationOnMap()
    {
        if(!PrepareNavigation() || !ReadDestination(out var point))return false;
        return StartNavigation(point,false);
    }
    public bool BeginPointPick()
    {
        if(!PrepareNavigation())return false;
        ReadDestination(out var point);return StartNavigation(point,true);
    }
    private bool PrepareNavigation()
    {
        if(closed||navigating||managerFlowBusy||requestPending||optionsWindow is not null||!ownerCurrent()||!canApply()||closed||!ownerCurrent())return false;
        additionWindow?.Cancel();additionWindow=null;modificationWindow?.Cancel();modificationWindow=null;
        RefreshDestinationCoordinates();return !closed&&ownerCurrent();
    }
    private bool StartNavigation(TilePoint point,bool pick)
    {
        if(pick?pointRequester is null:viewRequester is null)return false;
        if(point.X<0||point.Y<0||point.X>=document.Width||point.Y>=document.Height)
        {statusKey="Manager.NavigationInvalid";RefreshStatus();return false;}
        navigating=true;picking=pick;navigationPoint=point;navigationRevision=document.EditRevision;var token=++navigationToken;
        restoreNavigationVisibility=IsVisible;resumeNavigationModal=restoreNavigationVisibility&&ComponentDispatcher.IsThreadModal;
        UpdateApply();
        try
        {
            if(IsVisible)Hide();
            if(pick)pointRequester!(this);
            else
            {
                var cleanup=viewRequester!(this,point);
                if(!NavigationCurrent(token)){cleanup?.Invoke();EndNavigation();return false;}
                CancelNavigation=cleanup;
            }
            if(!NavigationCurrent(token)){EndNavigation();return false;}
            var window=new ObjectManagerMapWindow(pick,point,()=>ConfirmPointPick(token));mapWindow=window;
            if(Owner is {IsLoaded:true} root)window.Owner=root;
            window.Closed+=(_,_)=>{if(ReferenceEquals(mapWindow,window)){mapWindow=null;EndNavigation();}};
            if(NavigationPresenter is {} presenter)presenter(window,pick);
            else if(pick)window.Show();else window.ShowDialog();
            return true;
        }
        catch(Exception error)
        {
            // In particular, do not strand a hidden modal manager when finishing
            // an old pen stroke raises a typed Undo allocation failure.
            try{EndNavigation();}catch{/* Preserve the original failure. */}
            if(!IsThingRequestFailure(error))throw;
            statusKey="Manager.Failed";status.ToolTip=error.Message;RefreshStatus();return false;
        }
    }
    public bool PreviewPointPick(TilePoint point,long token)
    {
        if(!HasPendingPointPick || !NavigationCurrent(token))return false;
        if(point.X<0||point.Y<0||point.X>=document.Width||point.Y>=document.Height)return false;
        navigationPoint=point;mapWindow?.SetPoint(point);return true;
    }
    public bool ConfirmPointPick(long token)
    {
        if(!HasPendingPointPick||!NavigationCurrent(token))return false;
        destinationX.Text=navigationPoint.X.ToString(CultureInfo.InvariantCulture);destinationY.Text=navigationPoint.Y.ToString(CultureInfo.InvariantCulture);
        // Editing is locked while the picker is active. Publish its private
        // point explicitly only after the current token has been accepted.
        session.SetDestination(true,destinationX.Text,document.Width);session.SetDestination(false,destinationY.Text,document.Height);
        EndNavigation();return true;
    }
    public void EndNavigation(bool restore=true)
    {
        if(!navigating)return;
        navigating=false;picking=false;var token=++navigationToken;
        var cancel=CancelNavigation;CancelNavigation=null;
        try{cancel?.Invoke();}
        finally
        {
            var window=mapWindow;mapWindow=null;window?.Close();
            var visible=restoreNavigationVisibility;var modal=resumeNavigationModal;restoreNavigationVisibility=false;resumeNavigationModal=false;
            UpdateApply();
            // Hide terminates the old ShowDialog frame. Re-enter only after its
            // event handler has returned; modeless callers remain modeless.
            if(restore&&visible&&!closed)Dispatcher.BeginInvoke(()=>
            {
                if(closed||navigating||token!=navigationToken||!ownerCurrent()||IsVisible)return;
                if(modal)ShowDialog();else{Show();Activate();}
            },DispatcherPriority.ContextIdle);
        }
    }
}
