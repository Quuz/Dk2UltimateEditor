using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class PatrolRemovalWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly EditorPatrolRemovalRequest request;
    private readonly Func<bool> ownerCurrent;
    private readonly CheckBox remember=new(){Margin=new(4,14,4,10)};
    private readonly Button yes=new(){IsDefault=true},no=new();
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private bool closed,closing;
    public EditorPatrolManagerRoute? Result {get;private set;}
    public Exception? CommitFailure {get;private set;}
    public PatrolRemovalWindow(NativeMapDocument document,EditorPatrolRemovalRequest request,Func<bool> ownerCurrent)
    {
        this.document=document;this.request=request;this.ownerCurrent=ownerCurrent;
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"Editor.PatrolTitle");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=570;MinWidth=440;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel{Margin=new(16)};Content=panel;
        var message=new DockPanel();panel.Children.Add(message);
        using(var stream=typeof(PatrolRemovalWindow).Assembly.GetManifestResourceStream("Legacy.Request.Question")
            ??throw new InvalidDataException("Original request question icon is missing."))
        {
            var icon=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);icon.Freeze();
            message.Children.Add(new Image{Source=icon,Width=32,Height=32,Margin=new(4,4,14,4),VerticalAlignment=VerticalAlignment.Top});
        }
        var text=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4)};
        text.SetResourceReference(TextBlock.TextProperty,request.Kind==EditorPatrolRemovalKind.Point?"Editor.PatrolRemovalPoint":"Editor.PatrolRemovalRoute");message.Children.Add(text);
        var rememberText=new TextBlock{TextWrapping=TextWrapping.Wrap};rememberText.SetResourceReference(TextBlock.TextProperty,"Editor.PatrolRemovalRemember");remember.Content=rememberText;panel.Children.Add(remember);
        remember.Checked+=(_,_)=>{Refresh();yes.Focus();};remember.Unchecked+=(_,_)=>Refresh();
        panel.Children.Add(status);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(buttons);
        yes.SetResourceReference(ContentControl.ContentProperty,"Editor.PatrolRemovalYes");yes.Click+=(_,_)=>Accept();buttons.Children.Add(yes);
        no.SetResourceReference(ContentControl.ContentProperty,"Editor.PatrolRemovalNo");no.Click+=(_,_)=>{if(Current&&request.Decline(remember.IsChecked==true))Close();};buttons.Children.Add(no);
        RegisterName("PatrolRemovalRemember",remember);RegisterName("PatrolRemovalYes",yes);RegisterName("PatrolRemovalNo",no);
        PreviewKeyDown+=(_,args)=>{if(args.Key==Key.Escape){args.Handled=true;Close();}};
        document.Changed+=DocumentChanged;
        Loaded+=(_,_)=>{PlaceAtMouse();Refresh();yes.Focus();};
        Closing+=(_,_)=>closing=true;
        Closed+=(_,_)=>{closed=true;request.Dispose();document.Changed-=DocumentChanged;};
        Refresh();
    }
    private bool Current=>!closed&&!closing&&ownerCurrent()&&request.IsCurrent;
    private void Refresh()
    {
        var current=Current;yes.IsEnabled=remember.IsEnabled=current;no.IsEnabled=current&&request.CanDecline(remember.IsChecked==true);
        if(!current&&!closed)status.SetResourceReference(TextBlock.TextProperty,"Editor.PatrolRemovalStale");
    }
    private void DocumentChanged(object? sender,EventArgs args){if(!closed)Refresh();}
    public bool Accept()
    {
        if(!Current){Refresh();return false;}
        try{Result=request.Accept(remember.IsChecked==true);if(!closed)Close();return true;}
        catch(EditorHistoryMemoryException error)
        {
            if(request.IsAccepted){CommitFailure=error;if(!closed)Close();return true;}
            if(!closed)Close();throw;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException)
        {
            if(request.IsAccepted){CommitFailure=error;if(!closed)Close();return true;}
            Refresh();status.SetResourceReference(TextBlock.TextProperty,"Editor.PatrolRemovalFailed");status.ToolTip=error.Message;return false;
        }
    }
    private void PlaceAtMouse()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle==IntPtr.Zero||!GetCursorPos(out var point)||!GetWindowRect(handle,out var bounds))return;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
        if(!GetMonitorInfo(MonitorFromPoint(point,2),ref info))return;
        var position=EditorPatrolWarningPolicy.PlaceAtCursor(point.X,point.Y,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,
            info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top);
        SetWindowPos(handle,IntPtr.Zero,position.X,position.Y,0,0,0x0001|0x0004|0x0010);
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint {public int X,Y;}
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect {public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo {public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window,out NativeRect bounds);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(NativePoint point,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
