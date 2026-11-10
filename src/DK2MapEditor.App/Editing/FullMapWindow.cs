using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class FullMapWindow : Window
{
    private readonly DispatcherTimer timer;
    private readonly NativeMapDocument source;
    public FullMapView View { get; }
    public bool AnimationRunning => timer.IsEnabled;
    public FullMapWindow(NativeMapDocument document, Action<double,double> navigate)
    {
        ArgumentNullException.ThrowIfNull(document); ArgumentNullException.ThrowIfNull(navigate);
        source = document;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false; Background = Brushes.Black;
        WindowStartupLocation = WindowStartupLocation.Manual;
        View = new FullMapView { Document = document }; Content = View;
        SetResourceReference(TitleProperty,"Editor.Overview.Fullscreen");
        View.SetResourceReference(ToolTipProperty,"Editor.Overview.FullscreenHint");
        timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (_,_) => View.AdvanceAnimation();
        View.PointChosen += (_,point) => { navigate(point.X,point.Y); Close(); };
        View.DismissRequested += (_,_) => Close();
        source.Changed += SourceChanged;
        Loaded += (_,_) => { timer.Start(); View.Focus(); };
        Activated += (_,_) => Topmost = true;
        Deactivated += (_,_) => Topmost = false;
        SourceInitialized += (_,_) => PlaceOnOwnerMonitor();
        Closed += (_,_) => { timer.Stop(); source.Changed -= SourceChanged; View.Document = null; Content = null; };
    }
    // A map edit/replacement should never leave a stale topmost navigation view.
    private void SourceChanged(object? sender,EventArgs e) => Close();
    private void PlaceOnOwnerMonitor()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var ownerHandle = Owner is null ? handle : new WindowInteropHelper(Owner).Handle;
        var monitor = MonitorFromWindow(ownerHandle,2); var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (GetMonitorInfo(monitor,ref info))
            SetWindowPos(handle,IntPtr.Zero,info.Monitor.Left,info.Monitor.Top,info.Monitor.Right - info.Monitor.Left,info.Monitor.Bottom - info.Monitor.Top,0x0004 | 0x0010);
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor,Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
