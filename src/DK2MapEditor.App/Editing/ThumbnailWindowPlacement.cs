using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source ThumbnailWindowX/Y process-session position; no map/JSON state or stored thumbnail size.</summary>
internal sealed class ThumbnailWindowPlacement
{
    private (int X,int Y)? saved;
    public void Attach(Window window)
    {
        var placed=false;
        void Capture()
        {
            if(!placed||window.WindowState!=WindowState.Normal)return;
            var handle=new WindowInteropHelper(window).Handle;
            if(handle!=IntPtr.Zero&&GetWindowRect(handle,out var bounds))saved=(bounds.Left,bounds.Top);
        }
        window.Loaded+=(_,_)=>
        {
            var handle=new WindowInteropHelper(window).Handle;
            if(handle==IntPtr.Zero||!GetWindowRect(handle,out var bounds))return;
            var width=bounds.Right-bounds.Left;var height=bounds.Bottom-bounds.Top;
            if(width<=0||height<=0)return;
            var x=saved?.X??bounds.Left;var y=saved?.Y??bounds.Top;
            if(saved is null&&window.Owner is {} owner)
            {
                var parent=new WindowInteropHelper(owner).Handle;
                if(parent!=IntPtr.Zero&&GetWindowRect(parent,out var ownerBounds))
                {x=ownerBounds.Right-width-12;y=ownerBounds.Top+GetSystemMetrics(4);}
            }
            var requested=new NativeRect{Left=x,Top=y,Right=x+width,Bottom=y+height};
            var monitor=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
            if(!GetMonitorInfo(MonitorFromRect(ref requested,2),ref monitor))return;
            var workWidth=monitor.Work.Right-monitor.Work.Left;var workHeight=monitor.Work.Bottom-monitor.Work.Top;
            if(workWidth<=0||workHeight<=0)return;
            // Native coordinates remain consistent across monitors. Fit the
            // modern resizable controls where possible without lowering MinSize.
            var scaleX=window.ActualWidth>0?width/window.ActualWidth:1;
            var scaleY=window.ActualHeight>0?height/window.ActualHeight:1;
            width=Math.Max((int)Math.Ceiling(window.MinWidth*scaleX),Math.Min(width,workWidth));
            height=Math.Max((int)Math.Ceiling(window.MinHeight*scaleY),Math.Min(height,workHeight));
            var location=EditorPatrolWarningPolicy.PlaceAtCursor(x,y,width,height,monitor.Work.Left,monitor.Work.Top,workWidth,workHeight);
            SetWindowPos(handle,IntPtr.Zero,location.X,location.Y,width,height,0x0004|0x0010);
            placed=true;Capture();
        };
        window.LocationChanged+=(_,_)=>Capture();window.Closed+=(_,_)=>Capture();
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo{public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window,out NativeRect bounds);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromRect(ref NativeRect bounds,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
