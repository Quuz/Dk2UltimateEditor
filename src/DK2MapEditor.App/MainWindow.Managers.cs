using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App;

public partial class MainWindow
{
    private void SourceObjectBrush_Click(object sender,RoutedEventArgs args)
    {
        if(saveOwnerClosed||scenarioOwnerClosed||document is null||!ReferenceEquals(Editor.Document,document)||sender is not MenuItem{Tag:string tag})return;
        if(Enum.TryParse<PlacedThingKind>(tag,out var kind)&&Enum.IsDefined(kind))Editor.RequestObjectBrush(kind,document);
    }
    private void SourceWorkspaceManager_Click(object sender,RoutedEventArgs args)
    {
        if(saveOwnerClosed||scenarioOwnerClosed||document is null||!ReferenceEquals(Editor.Document,document)||sender is not MenuItem {Tag:string tag})return;
        if(Enum.TryParse<WorkspaceManagerRoute>(tag,out var route)&&Enum.IsDefined(route))Editor.RequestSourceManager(route,document);
    }
    private void RefreshSourceManagerCommands()
    {
        var enabled=!saveOwnerClosed&&document is not null;
        SectionVersionsMenuItem.IsEnabled=enabled;
        foreach(var item in new[]{CreatureManagerMenuItem,DoorTrapManagerMenuItem,ItemManagerMenuItem,BandManagerMenuItem,
            AreaManagerMenuItem,GateManagerMenuItem,EffectManagerMenuItem,PatrolManagerMenuItem,ScenarioManagerMenuItem,CreatureBrushMenuItem,
            DoorBrushMenuItem,TrapBrushMenuItem,ObjectBrushMenuItem,SpecialBrushMenuItem})item.IsEnabled=enabled;
    }
    private void SourceObjectManager_Click(object sender,RoutedEventArgs args)
    {
        if(saveOwnerClosed||scenarioOwnerClosed||document is null||!ReferenceEquals(Editor.Document,document)||sender is not MenuItem {Tag:string tag})return;
        var family=tag switch{"0"=>0,"1"=>1,"2"=>2,_=>-1};
        if(family>=0)Editor.RequestObjectManager(family);
    }
}
