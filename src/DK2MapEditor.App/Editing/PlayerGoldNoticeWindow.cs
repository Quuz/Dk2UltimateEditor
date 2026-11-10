using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class PlayerGoldNoticeWindow : Window,IDisposable
{
    private readonly EditorPlayerGoldNotice notice;
    private readonly CheckBox remember=new(){Margin=new(4,12,4,8)};
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private bool closed;
    public Exception? Failure {get;private set;}
    public EditorPlayerGoldNotice Notice=>notice;
    public PlayerGoldNoticeWindow(EditorPlayerGoldNotice notice)
    {
        this.notice=notice??throw new ArgumentNullException(nameof(notice));NameScope.SetNameScope(this,new NameScope());
        SetResourceReference(TitleProperty,"PlayerGold.Title");SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=560;MinWidth=430;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel{Margin=new(16)};Content=panel;var message=new DockPanel();panel.Children.Add(message);
        using(var stream=typeof(PlayerGoldNoticeWindow).Assembly.GetManifestResourceStream("Legacy.Request.Information")??throw new InvalidDataException("Original information icon is missing."))
        {
            var icon=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);icon.Freeze();
            message.Children.Add(new Image{Source=icon,Width=32,Height=32,Margin=new(4,4,14,4),VerticalAlignment=VerticalAlignment.Top});
        }
        var text=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4)};text.SetResourceReference(TextBlock.TextProperty,"PlayerGold.Message");message.Children.Add(text);RegisterName("PlayerGoldNoticeMessage",text);
        var label=new TextBlock{TextWrapping=TextWrapping.Wrap};label.SetResourceReference(TextBlock.TextProperty,"PlayerGold.Remember");remember.Content=label;panel.Children.Add(remember);RegisterName("PlayerGoldNoticeRemember",remember);
        panel.Children.Add(status);var ok=new Button{IsDefault=true,HorizontalAlignment=HorizontalAlignment.Right};ok.SetResourceReference(ContentControl.ContentProperty,"Editor.PatrolWarningOK");ok.Click+=(_,_)=>Acknowledge();panel.Children.Add(ok);RegisterName("PlayerGoldNoticeOK",ok);
        PreviewKeyDown+=(_,args)=>{if(args.Key==Key.Escape){args.Handled=true;Close();}};
        Loaded+=(_,_)=>{PlaceAtMouse();Refresh();ok.Focus();};Closed+=(_,_)=>{closed=true;notice.Dispose();};Refresh();
    }
    private void Refresh()
    {
        remember.IsEnabled=!closed&&notice.IsCurrent;
        if(!closed&&!notice.IsCurrent)status.SetResourceReference(TextBlock.TextProperty,"PlayerGold.Stale");
    }
    public void Acknowledge()
    {
        if(closed)return;
        // An informational result is never a veto. The caller independently
        // checks its plan after close; stale information cannot remember a bit.
        try{if(notice.IsCurrent)notice.Acknowledge(remember.IsChecked==true);}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or IOException or InvalidDataException or FormatException or OverflowException){Failure=error;}
        finally{Close();}
    }
    public void Dispose(){notice.Dispose();if(!closed)Close();}
    private void PlaceAtMouse()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle==IntPtr.Zero||!GetCursorPos(out var point)||!GetWindowRect(handle,out var bounds))return;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};if(!GetMonitorInfo(MonitorFromPoint(point,2),ref info))return;
        var position=EditorPatrolWarningPolicy.PlaceAtCursor(point.X,point.Y,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,
            info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top);
        SetWindowPos(handle,IntPtr.Zero,position.X,position.Y,0,0,0x0001|0x0004|0x0010);
    }
    [StructLayout(LayoutKind.Sequential)]private struct NativePoint{public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct MonitorInfo{public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetWindowRect(IntPtr window,out NativeRect bounds);
    [DllImport("user32.dll")]private static extern IntPtr MonitorFromPoint(NativePoint point,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
