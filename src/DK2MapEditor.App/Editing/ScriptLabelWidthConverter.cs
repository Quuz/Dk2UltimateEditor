using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DK2MapEditor.App.Editing;

/// <summary>Tree headers occupy an Auto column even when horizontal scrolling
/// is disabled. Bound text width to the viewport minus this node's indentation.</summary>
public sealed class ScriptLabelWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var width = values.FirstOrDefault() is double number && double.IsFinite(number) ? number : 320;
        var depth = 0;
        if (values.ElementAtOrDefault(1) is DependencyObject item)
            for (var parent = item; parent is not null; parent = VisualTreeHelper.GetParent(parent))
                if (parent is TreeViewItem) depth++;
        return Math.Max(72, width - 28 - Math.Max(1, depth) * 20 - (Equals(parameter,"Image") ? 20 : 0));
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
