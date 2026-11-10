using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source CompilerWindowX/Y/Width/Height: last closed normal window,
/// retained for this owner session, with native monitor work-area bounds.</summary>
internal sealed class CompilerWindowPlacement
{
    private NativeRect? saved;
    public void Attach(Window window)
    {
        window.Loaded+=(_,_)=>
        {
            var handle=new WindowInteropHelper(window).Handle;
            if(handle==IntPtr.Zero||!GetWindowRect(handle,out var initial))return;
            var requested=saved??initial;var monitor=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
            if(!GetMonitorInfo(MonitorFromRect(ref requested,2),ref monitor))return;
            var workWidth=monitor.Work.Right-monitor.Work.Left;var workHeight=monitor.Work.Bottom-monitor.Work.Top;
            if(workWidth<=0||workHeight<=0)return;
            var scaleX=window.ActualWidth>0?(initial.Right-initial.Left)/window.ActualWidth:1;
            var scaleY=window.ActualHeight>0?(initial.Bottom-initial.Top)/window.ActualHeight:1;
            var width=Math.Max((int)Math.Ceiling(window.MinWidth*scaleX),Math.Min(requested.Right-requested.Left,workWidth));
            var height=Math.Max((int)Math.Ceiling(window.MinHeight*scaleY),Math.Min(requested.Bottom-requested.Top,workHeight));
            var point=EditorPatrolWarningPolicy.PlaceAtCursor(requested.Left,requested.Top,width,height,monitor.Work.Left,monitor.Work.Top,workWidth,workHeight);
            SetWindowPos(handle,IntPtr.Zero,point.X,point.Y,width,height,0x0004|0x0010);
        };
        window.Closing+=(_,args)=>
        {
            if(args.Cancel||window.WindowState!=WindowState.Normal)return;
            var handle=new WindowInteropHelper(window).Handle;
            if(handle!=IntPtr.Zero&&GetWindowRect(handle,out var bounds))saved=bounds;
        };
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo{public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window,out NativeRect bounds);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromRect(ref NativeRect bounds,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
