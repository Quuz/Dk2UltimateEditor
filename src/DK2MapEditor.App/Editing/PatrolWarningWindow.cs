using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class PatrolWarningWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly EditorPatrolWarningRequest request;
    private readonly Func<bool> ownerCurrent;
    private readonly CheckBox suppress=new(){Margin=new(4,14,4,10)};
    private readonly Button acknowledge=new(){IsDefault=true};
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private bool closed,closing;
    public PatrolWarningWindow(NativeMapDocument document,EditorPatrolWarningRequest request,Func<bool> ownerCurrent)
    {
        this.document=document;this.request=request;this.ownerCurrent=ownerCurrent;
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"Editor.PatrolTitle");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=570;MinWidth=440;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel{Margin=new(16)};Content=panel;
        var message=new DockPanel();panel.Children.Add(message);
        using(var stream=typeof(PatrolWarningWindow).Assembly.GetManifestResourceStream("Legacy.Request.Question")
            ??throw new InvalidDataException("Original request question icon is missing."))
        {
            var icon=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);icon.Freeze();
            message.Children.Add(new Image{Source=icon,Width=32,Height=32,Margin=new(4,4,14,4),VerticalAlignment=VerticalAlignment.Top});
        }
        var text=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4)};
        text.SetResourceReference(TextBlock.TextProperty,request.Kind==EditorPatrolWarningKind.Selection?"Editor.PatrolWarningSelection":"Editor.PatrolWarningOther");message.Children.Add(text);
        var suppressText=new TextBlock{TextWrapping=TextWrapping.Wrap};suppressText.SetResourceReference(TextBlock.TextProperty,"Editor.PatrolWarningSuppress");suppress.Content=suppressText;panel.Children.Add(suppress);
        panel.Children.Add(status);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(buttons);
        acknowledge.SetResourceReference(ContentControl.ContentProperty,"Editor.PatrolWarningOK");acknowledge.Click+=(_,_)=>Respond();buttons.Children.Add(acknowledge);
        RegisterName("PatrolWarningSuppress",suppress);RegisterName("PatrolWarningAcknowledge",acknowledge);
        PreviewKeyDown+=(_,args)=>{if(args.Key==Key.Escape){args.Handled=true;Close();}};
        document.Changed+=DocumentChanged;
        Loaded+=(_,_)=>{PlaceAtMouse();Refresh();acknowledge.Focus();};
        Closing+=(_,_)=>closing=true;
        Closed+=(_,_)=>{closed=true;request.Dispose();document.Changed-=DocumentChanged;};
        Refresh();
    }
    private bool Current=>!closed&&!closing&&ownerCurrent()&&request.IsCurrent;
    private void Refresh()
    {
        var current=Current;acknowledge.IsEnabled=suppress.IsEnabled=current;
        if(!current&&!closed)status.SetResourceReference(TextBlock.TextProperty,"Editor.PatrolWarningStale");
    }
    private void DocumentChanged(object? sender,EventArgs args){if(!closed)Refresh();}
    public bool Respond()
    {
        if(!Current){Refresh();return false;}
        if(!request.Respond(true,suppress.IsChecked==true)){Refresh();return false;}
        Close();return true;
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
