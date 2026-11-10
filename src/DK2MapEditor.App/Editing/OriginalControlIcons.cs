using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

internal static class OriginalControlIcons
{
    private static readonly Dictionary<int,BitmapSource> images=[];
    internal static BitmapSource? For(int? id)
    {
        if(id is 87 or 88 or 89)return OriginalReportImages.For(id==87?DiagnosticSeverity.Warning:id==88?DiagnosticSeverity.Error:DiagnosticSeverity.Information);
        if(id is not (19 or 25 or 26 or >=40 and <=47 or >=50 and <=55))return null;
        if(images.TryGetValue(id.Value,out var image))return image;
        using var stream=typeof(OriginalControlIcons).Assembly.GetManifestResourceStream("Legacy.ControlIcon."+id)
            ??throw new InvalidOperationException("Missing original control icon: "+id);
        image=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames.First(frame=>frame.PixelWidth==16&&frame.PixelHeight==16);
        image.Freeze();images.Add(id.Value,image);return image;
    }
    internal static void Decorate(Button button,int resource)
    {
        var panel=new FrameworkElementFactory(typeof(DockPanel));var image=new FrameworkElementFactory(typeof(Image));
        image.SetValue(Image.SourceProperty,For(resource));image.SetValue(FrameworkElement.WidthProperty,16d);image.SetValue(FrameworkElement.HeightProperty,16d);image.SetValue(FrameworkElement.MarginProperty,new Thickness(0,0,4,0));image.SetValue(DockPanel.DockProperty,Dock.Left);panel.AppendChild(image);
        var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding("."));text.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);panel.AppendChild(text);
        button.ContentTemplate=new DataTemplate{VisualTree=panel};
    }
    internal static DataTemplate Template(string label,string? tooltip=null)
    {
        var panel=new FrameworkElementFactory(typeof(DockPanel));var image=new FrameworkElementFactory(typeof(Image));
        image.SetBinding(Image.SourceProperty,new Binding("Icon"));image.SetValue(FrameworkElement.WidthProperty,16d);image.SetValue(FrameworkElement.HeightProperty,16d);image.SetValue(FrameworkElement.MarginProperty,new Thickness(0,1,4,1));image.SetValue(DockPanel.DockProperty,Dock.Left);
        var style=new Style(typeof(Image));var missing=new DataTrigger{Binding=new Binding("Icon"),Value=null};missing.Setters.Add(new Setter(UIElement.VisibilityProperty,Visibility.Collapsed));style.Triggers.Add(missing);image.SetValue(FrameworkElement.StyleProperty,style);panel.AppendChild(image);
        var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding(label));text.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);
        if(tooltip is not null)text.SetBinding(FrameworkElement.ToolTipProperty,new Binding(tooltip));panel.AppendChild(text);
        return new DataTemplate{VisualTree=panel};
    }
}
