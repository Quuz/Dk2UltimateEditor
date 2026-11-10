using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DK2MapEditor.App.Editing;

internal static class OriginalEditorListImages
{
    internal static DataTemplate DescriptionTemplate()
    {
        var panel=new FrameworkElementFactory(typeof(DockPanel));
        var image=new FrameworkElementFactory(typeof(Image));image.SetBinding(Image.SourceProperty,new Binding("Icon"));
        image.SetValue(FrameworkElement.WidthProperty,16d);image.SetValue(FrameworkElement.HeightProperty,16d);image.SetValue(FrameworkElement.MarginProperty,new Thickness(0,1,4,1));image.SetValue(DockPanel.DockProperty,Dock.Left);
        var style=new Style(typeof(Image));var missing=new DataTrigger{Binding=new Binding("Icon"),Value=null};missing.Setters.Add(new Setter(UIElement.VisibilityProperty,Visibility.Collapsed));style.Triggers.Add(missing);image.SetValue(FrameworkElement.StyleProperty,style);panel.AppendChild(image);
        var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding("Label"));text.SetBinding(FrameworkElement.ToolTipProperty,new Binding("Label"));text.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);text.SetValue(FrameworkElement.MaxWidthProperty,300d);panel.AppendChild(text);
        return new DataTemplate{VisualTree=panel};
    }
    private static readonly Dictionary<(int, int), BitmapSource> images = [];
    internal static BitmapSource? For(int? resource, int size = 32)
    {
        if (resource is not { } id) return null;
        if (size is not (16 or 20 or 32)) throw new ArgumentOutOfRangeException(nameof(size));
        lock (images)
        {
            if (images.TryGetValue((id, size), out var cached)) return cached;
            var assembly = typeof(OriginalEditorListImages).Assembly;
            using var stream = assembly.GetManifestResourceStream("Legacy.ListImage." + id)
                ?? assembly.GetManifestResourceStream("Legacy.PaletteImage." + id)
                ?? assembly.GetManifestResourceStream("Legacy.ObjectImage." + id)
                ?? throw new InvalidOperationException("Missing original list image: " + id);
            var original = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapSource result = original.PixelWidth == size && original.PixelHeight == size ? original
                : new TransformedBitmap(original, new ScaleTransform((double)size / original.PixelWidth, (double)size / original.PixelHeight));
            result.Freeze(); images.Add((id, size), result); return result;
        }
    }
    internal static DataTemplate WarningTemplate()
    {
        var panel = new FrameworkElementFactory(typeof(DockPanel));
        var image = new FrameworkElementFactory(typeof(Image)); image.SetBinding(Image.SourceProperty, new Binding("Icon"));
        image.SetValue(FrameworkElement.WidthProperty, 32d); image.SetValue(FrameworkElement.HeightProperty, 32d);
        image.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 1, 6, 1)); image.SetValue(DockPanel.DockProperty, Dock.Left);
        var style = new Style(typeof(Image)); var missing = new DataTrigger { Binding = new Binding("Icon"), Value = null };
        missing.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed)); style.Triggers.Add(missing);
        image.SetValue(FrameworkElement.StyleProperty, style); panel.AppendChild(image);
        var text = new FrameworkElementFactory(typeof(TextBlock)); text.SetBinding(TextBlock.TextProperty, new Binding("Warnings"));
        text.SetBinding(FrameworkElement.ToolTipProperty, new Binding("Warnings"));
        text.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap); panel.AppendChild(text);
        return new DataTemplate { VisualTree = panel };
    }
}
