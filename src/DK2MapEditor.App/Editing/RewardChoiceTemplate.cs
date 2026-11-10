using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DK2MapEditor.App.Editing;

internal static class RewardChoiceTemplate
{
    internal static DataTemplate Create()
    {
        var panel=new FrameworkElementFactory(typeof(StackPanel));panel.SetValue(StackPanel.OrientationProperty,Orientation.Horizontal);
        var image=new FrameworkElementFactory(typeof(FontSizedRewardImage));image.SetBinding(FontSizedRewardImage.ImageResourceProperty,new Binding("ImageResource"));
        image.SetValue(FrameworkElement.MarginProperty,new Thickness(0,0,1,0));image.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);panel.AppendChild(image);
        var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding("Label"));text.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);panel.AppendChild(text);
        return new DataTemplate{VisualTree=panel};
    }
}
