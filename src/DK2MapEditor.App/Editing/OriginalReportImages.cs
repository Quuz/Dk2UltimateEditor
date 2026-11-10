using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.App.Editing;

internal static class OriginalReportImages
{
    private static readonly Dictionary<int,BitmapSource> images=[];
    internal static BitmapSource? For(DiagnosticSeverity severity)
    {
        if(ReportImageResources.For(severity) is not {} id)return null;
        if(images.TryGetValue(id,out var cached))return cached;
        using var stream=typeof(OriginalReportImages).Assembly.GetManifestResourceStream("Legacy.Report."+id)
            ??throw new InvalidOperationException("Missing original report icon: "+id);
        var image=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames.First(frame=>frame.PixelWidth==16&&frame.PixelHeight==16);
        image.Freeze();images.Add(id,image);return image;
    }
    internal static DataTemplate Template()
    {
        var panel=new FrameworkElementFactory(typeof(DockPanel));
        var image=new FrameworkElementFactory(typeof(Image));image.SetBinding(Image.SourceProperty,new Binding("Icon"));
        image.SetValue(FrameworkElement.WidthProperty,16d);image.SetValue(FrameworkElement.HeightProperty,16d);image.SetValue(FrameworkElement.MarginProperty,new Thickness(0,1,4,1));image.SetValue(DockPanel.DockProperty,Dock.Left);
        var style=new Style(typeof(Image));var missing=new DataTrigger{Binding=new Binding("Icon"),Value=null};missing.Setters.Add(new Setter(UIElement.VisibilityProperty,Visibility.Collapsed));style.Triggers.Add(missing);image.SetValue(FrameworkElement.StyleProperty,style);panel.AppendChild(image);
        var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding("Severity"));text.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);panel.AppendChild(text);
        return new DataTemplate{VisualTree=panel};
    }
}
