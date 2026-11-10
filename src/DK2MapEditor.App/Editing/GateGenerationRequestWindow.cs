using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class GateGenerationRequestWindow : Window,IDisposable
{
    private readonly EditorGateGenerationRequest request;
    private readonly CheckBox remember=new(){Margin=new(4,12,4,8)};
    private readonly TextBlock message=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)},status=new(){TextWrapping=TextWrapping.Wrap};
    private readonly Button yes=new(){IsDefault=true},no=new();
    private bool closed;
    public Exception? Failure {get;private set;}
    public EditorGateGenerationRequest Request=>request;
    public GateGenerationRequestWindow(EditorGateGenerationRequest request)
    {
        this.request=request??throw new ArgumentNullException(nameof(request));NameScope.SetNameScope(this,new NameScope());
        SetResourceReference(TitleProperty,"GateGenerationRequest.Title");SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=560;MinWidth=440;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel{Margin=new(16)};Content=panel;var row=new DockPanel();panel.Children.Add(row);
        using(var stream=typeof(GateGenerationRequestWindow).Assembly.GetManifestResourceStream(request.Capacity is null?"Legacy.Request.Question":"Legacy.Request.Warning")??throw new InvalidDataException("Original request icon is missing."))
        {
            var icon=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);icon.Freeze();
            row.Children.Add(new Image{Source=icon,Width=32,Height=32,Margin=new(4,4,14,4),VerticalAlignment=VerticalAlignment.Top});
        }
        row.Children.Add(message);RegisterName("GateGenerationMessage",message);
        var label=new TextBlock{TextWrapping=TextWrapping.Wrap};label.SetResourceReference(TextBlock.TextProperty,"GateGenerationRequest.Remember");remember.Content=label;
        panel.Children.Add(remember);RegisterName("GateGenerationRemember",remember);remember.Click+=(_,_)=>Refresh();panel.Children.Add(status);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(buttons);
        yes.SetResourceReference(ContentControl.ContentProperty,request.Capacity is null?"GateGenerationRequest.Yes":"Editor.PatrolWarningOK");yes.Click+=(_,_)=>Accept();buttons.Children.Add(yes);RegisterName("GateGenerationYes",yes);
        no.SetResourceReference(ContentControl.ContentProperty,"GateGenerationRequest.No");no.Click+=(_,_)=>Close();no.Visibility=request.Capacity is null?Visibility.Visible:Visibility.Collapsed;buttons.Children.Add(no);RegisterName("GateGenerationNo",no);
        PreviewKeyDown+=(_,args)=>{if(args.Key==Key.Escape){args.Handled=true;Close();}};
        Loaded+=(_,_)=>{PlaceAtMouse();Refresh();yes.Focus();};LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;request.Dispose();LocalizationService.LanguageChanged-=LanguageChanged;};Refresh();
    }
    private void LanguageChanged(object? sender,EventArgs args)=>Refresh();
    private void Refresh()
    {
        if(closed)return;
        message.Text=request.Capacity is {} capacity?LocalizationService.Get("GateGenerationRequest."+(capacity.AreasExhausted&&capacity.GatesExhausted?"CapacityBoth":capacity.AreasExhausted?"CapacityAreas":"CapacityGates"))
            :string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("GateGenerationRequest.Confirm"),request.Gates,request.Areas,request.Conditions,request.Actions);
        var current=request.IsCurrent;yes.IsEnabled=remember.IsEnabled=current;no.IsEnabled=current&&remember.IsChecked!=true;
        if(!current)status.SetResourceReference(TextBlock.TextProperty,"GateGenerationRequest.Stale");
    }
    public void Accept()
    {
        if(closed)return;
        try{if(request.Capacity is null)request.Confirm(remember.IsChecked==true);else request.Acknowledge(remember.IsChecked==true);}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or IOException or FormatException or OverflowException){Failure=error;}
        finally{Close();}
    }
    public void Dispose(){request.Dispose();if(!closed)Close();}
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
