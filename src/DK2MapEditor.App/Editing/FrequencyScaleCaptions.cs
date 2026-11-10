using System.Windows;
using System.Windows.Controls;

namespace DK2MapEditor.App.Editing;

internal static class FrequencyScaleCaptions
{
    internal static Grid Create()
    {
        var grid=new Grid{Margin=new(0,2,0,4)};
        grid.ColumnDefinitions.Add(new());grid.ColumnDefinitions.Add(new());
        var often=new TextBlock{HorizontalAlignment=HorizontalAlignment.Left,TextWrapping=TextWrapping.Wrap};
        often.SetResourceReference(TextBlock.TextProperty,"Editor.EffectGenerator.Often");grid.Children.Add(often);
        var rarely=new TextBlock{HorizontalAlignment=HorizontalAlignment.Right,TextAlignment=TextAlignment.Right,TextWrapping=TextWrapping.Wrap};
        rarely.SetResourceReference(TextBlock.TextProperty,"Editor.EffectGenerator.Rarely");Grid.SetColumn(rarely,1);grid.Children.Add(rarely);
        return grid;
    }
}
