using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    public MapObjectDisplayOptions ObjectDisplayOptions => Canvas.ObjectDisplayOptions;
    public void ConfigureObjectDisplay(MapObjectDisplayOptions? options)
    {
        Canvas.ObjectDisplayOptions = options ?? new();
        if (ObjectViewMenu.Items.Count == 0)
        {
            foreach (var flag in Enum.GetValues<MapObjectViewFlags>().Where(flag => flag != MapObjectViewFlags.None))
            {
                var item = new MenuItem { Tag = flag, IsCheckable = true };
                item.SetResourceReference(HeaderedItemsControl.HeaderProperty, "MapObjects." + flag);
                item.Click += ObjectView_Click; ObjectViewMenu.Items.Add(item);
            }
        }
        foreach (MenuItem item in ObjectViewMenu.Items) item.IsChecked = (Canvas.ObjectDisplayOptions.Flags & (MapObjectViewFlags)item.Tag) != 0;
    }
    private void ObjectView_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender; var flag = (MapObjectViewFlags)item.Tag;
        Canvas.ObjectDisplayOptions = Canvas.ObjectDisplayOptions with { Flags = item.IsChecked ? Canvas.ObjectDisplayOptions.Flags | flag : Canvas.ObjectDisplayOptions.Flags & ~flag };
    }
}
