using System.Globalization;
using System.Windows.Data;

namespace DK2MapEditor.App.Editing;

/// <summary>Keep the definition table usable while all expanded controls remain scrollable.</summary>
public sealed class DefinitionPanelHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Math.Max(100, value is double height && double.IsFinite(height) ? height - 230 : 100);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
