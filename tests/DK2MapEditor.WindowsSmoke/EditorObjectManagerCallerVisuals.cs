using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

internal static partial class Program
{
    private static void ShowManagerCallerWindow(Window window)
    {
        window.ShowInTaskbar=false;window.WindowStartupLocation=WindowStartupLocation.Manual;
        window.Left=-10000;window.Top=-10000;window.Show();
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);window.UpdateLayout();
        Expect(window.IsLoaded&&window.ActualWidth>0&&window.ActualHeight>0,"Manager test window not loaded.");
    }
    private static void CaptureManagerCallerWindow(Window window,string directory,string filename,params string[] controls)
    {
        window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);window.UpdateLayout();
        foreach(var name in controls)
        {
            var c=(FrameworkElement)window.FindName(name);var p=c.TranslatePoint(new Point(),window);
            Expect(c.IsVisible&&c.ActualWidth>0&&c.ActualHeight>0&&p.X>=0&&p.Y>=0
                &&p.X+c.ActualWidth<=window.ActualWidth+0.5&&p.Y+c.ActualHeight<=window.ActualHeight+0.5,
                "Actual manager control clipped: "+name);
        }
        Directory.CreateDirectory(directory);
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,PixelFormats.Pbgra32);
        bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output=new FileStream(Path.Combine(directory,filename),FileMode.CreateNew);encoder.Save(output);
    }
}
