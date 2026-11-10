using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DK2MapEditor.App.Editing;

internal static class HeroBandRecordTemplate
{
    internal static DataTemplate Create()
    {
        var panel=new FrameworkElementFactory(typeof(StackPanel));
        var header=new FrameworkElementFactory(typeof(DockPanel));
        var image=new FrameworkElementFactory(typeof(Image));image.SetBinding(Image.SourceProperty,new Binding("Icon"));
        image.SetValue(FrameworkElement.WidthProperty,32d);image.SetValue(FrameworkElement.HeightProperty,32d);image.SetValue(FrameworkElement.MarginProperty,new Thickness(0,0,6,0));image.SetValue(DockPanel.DockProperty,Dock.Left);
        var style=new Style(typeof(Image));var missing=new DataTrigger{Binding=new Binding("Icon"),Value=null};missing.Setters.Add(new Setter(UIElement.VisibilityProperty,Visibility.Collapsed));style.Triggers.Add(missing);image.SetValue(FrameworkElement.StyleProperty,style);header.AppendChild(image);
        var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding("Label"));header.AppendChild(text);panel.AppendChild(header);
        var children=new FrameworkElementFactory(typeof(ItemsControl));children.SetBinding(ItemsControl.ItemsSourceProperty,new Binding("BandPreview"));children.SetValue(ItemsControl.ItemTemplateProperty,HeroBandValuePanel.CreatureTemplate());
        children.SetValue(FrameworkElement.MarginProperty,new Thickness(20,0,0,0));children.SetValue(UIElement.IsHitTestVisibleProperty,false);children.SetValue(UIElement.FocusableProperty,false);panel.AppendChild(children);
        return new DataTemplate{VisualTree=panel};
    }
}
