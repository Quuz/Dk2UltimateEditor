using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App;

public partial class MainWindow
{
    private void ViewMenu_Opened(object sender,RoutedEventArgs e)
    {
        ToolbarsMenuItem.IsChecked=Editor.AreToolbarsVisible;
        MinimapMenuItem.IsChecked=Editor.IsOverviewVisible;
        RefreshMapLayerCommands();
    }
    private void ToolbarsVisibility_Click(object sender,RoutedEventArgs e)
    {
        var visible=ToolbarsMenuItem.IsChecked;
        CommandToolbar.Visibility=visible?Visibility.Visible:Visibility.Collapsed;
        Editor.SetToolbarsVisibility(visible);
    }
    private void MinimapVisibility_Click(object sender,RoutedEventArgs e)
        => Editor.SetOverviewVisibility(MinimapMenuItem.IsChecked);

    private void RefreshMapLayerCommands()
    {
        TerrainLayerMenuItem.IsChecked=Editor.ShowTerrain;
        if(ObjectLayersMenuItem.Items.Count==0)
            foreach(var flag in Enum.GetValues<MapObjectViewFlags>().Where(f=>f!=MapObjectViewFlags.None))
            {
                var item=new MenuItem{Tag=flag,IsCheckable=true};
                item.SetResourceReference(HeaderedItemsControl.HeaderProperty,"MapObjects."+flag);
                item.Click+=MapLayer_Click;ObjectLayersMenuItem.Items.Add(item);
            }
        if(OverlayLayersMenuItem.Items.Count==0)
            foreach(var flag in Enum.GetValues<MapOverlayFlags>().Where(f=>f!=MapOverlayFlags.None))
            {
                var item=new MenuItem{Tag=flag,IsCheckable=true};
                item.SetResourceReference(HeaderedItemsControl.HeaderProperty,"MapOverlays."+flag);
                item.Click+=MapLayer_Click;OverlayLayersMenuItem.Items.Add(item);
            }
        foreach(MenuItem item in ObjectLayersMenuItem.Items)
            item.IsChecked=(Editor.ObjectDisplayOptions.Flags&(MapObjectViewFlags)item.Tag)!=0;
        foreach(MenuItem item in OverlayLayersMenuItem.Items)
            item.IsChecked=(Editor.OverlayOptions.Flags&(MapOverlayFlags)item.Tag)!=0;
    }
    private void MapLayer_Click(object sender,RoutedEventArgs e)
    {
        if(saveOwnerClosed||sender is not MenuItem item)return;
        if(ReferenceEquals(item,TerrainLayerMenuItem))Editor.ConfigureTerrainVisibility(item.IsChecked);
        else if(item.Tag is MapObjectViewFlags objects)
            Editor.ConfigureObjectDisplay(Editor.ObjectDisplayOptions with{Flags=item.IsChecked?Editor.ObjectDisplayOptions.Flags|objects:Editor.ObjectDisplayOptions.Flags&~objects});
        else if(item.Tag is MapOverlayFlags overlays)
            Editor.ConfigureOverlays(Editor.OverlayOptions with{Flags=item.IsChecked?Editor.OverlayOptions.Flags|overlays:Editor.OverlayOptions.Flags&~overlays});
    }
}
