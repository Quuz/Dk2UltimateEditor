using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source Things_ManagerShowProc return palette. It owns no map edit.</summary>
public sealed class ObjectManagerMapWindow : Window
{
    private readonly TextBlock coordinates=new(){Margin=new(0,8,0,4)};
    public ObjectManagerMapWindow(bool picking,TilePoint point,Func<bool>? accept=null)
    {
        Width=430;MinWidth=340;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;
        WindowStyle=WindowStyle.ToolWindow;ShowInTaskbar=false;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(TitleProperty,"Manager.ViewTitle");SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        var panel=new StackPanel{Margin=new(14)};Content=panel;
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap};hint.SetResourceReference(TextBlock.TextProperty,picking?"Manager.PickHint":"Manager.ViewHint");panel.Children.Add(hint);
        SetPoint(point);panel.Children.Add(coordinates);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right,Margin=new(0,10,0,0)};panel.Children.Add(buttons);
        if(picking)
        {
            var ok=new Button{IsDefault=true};ok.SetResourceReference(ContentControl.ContentProperty,"Manager.UsePoint");ok.Click+=(_,_)=>accept?.Invoke();buttons.Children.Add(ok);
            NameScope.SetNameScope(this,new NameScope());ok.Name="ManagerUsePoint";RegisterName(ok.Name,ok);
        }
        var button=new Button{IsDefault=!picking};
        button.SetResourceReference(ContentControl.ContentProperty,picking?"Editor.Cancel":"Manager.Return");button.Click+=(_,_)=>Close();buttons.Children.Add(button);
        PreviewKeyDown+=(_,args)=>{if(args.Key==Key.Escape){args.Handled=true;Close();}};
        Loaded+=(_,_)=>PlaceNearOwner();
    }
    public void SetPoint(TilePoint point)=>coordinates.Text=$"{point.X}, {point.Y}";
    private void PlaceNearOwner()
    {
        if(Owner is null)return;
        var owner=new WindowInteropHelper(Owner).Handle;var handle=new WindowInteropHelper(this).Handle;
        if(owner==IntPtr.Zero||handle==IntPtr.Zero||!GetWindowRect(owner,out var parent)||!GetWindowRect(handle,out var bounds))return;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
        if(!GetMonitorInfo(MonitorFromWindow(owner,2),ref info))return;
        var position=EditorPatrolWarningPolicy.PlaceAtCursor(parent.Right-(bounds.Right-bounds.Left),parent.Top+GetSystemMetrics(4)/2,
            bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top);
        SetWindowPos(handle,IntPtr.Zero,position.X,position.Y,0,0,0x0001|0x0004|0x0010);
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo{public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window,out NativeRect bounds);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
