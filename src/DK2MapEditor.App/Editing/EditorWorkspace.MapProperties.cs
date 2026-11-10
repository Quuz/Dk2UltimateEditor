using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private MapPropertiesWindow? mapPropertiesWindow;
    private long mapPropertiesContext;
    private bool mapPropertiesOwnerClosed;
    public Action<MapPropertiesWindow>? MapPropertiesPresenter { get; set; }
    private void CloseMapPropertiesWindow() { mapPropertiesContext++; mapPropertiesWindow?.Close(); mapPropertiesWindow = null; }
    public void EndMapPropertiesLifetime() { mapPropertiesOwnerClosed = true; CloseMapPropertiesWindow(); MapPropertiesButton.IsEnabled = false; }
    public MapPropertiesWindow? CreateMapPropertiesWindow()
    {
        if (mapPropertiesOwnerClosed || document is null || !CommitFields()) return null;
        var source = document; var context = mapPropertiesContext;
        return new(source, () => !mapPropertiesOwnerClosed && context == mapPropertiesContext && ReferenceEquals(source, document), CommitFields, GameResources,RequestPreferences);
    }
    public void ShowMapProperties()
    {
        if (mapPropertiesWindow is not null) { mapPropertiesWindow.Activate(); return; }
        try
        {
            var window = CreateMapPropertiesWindow(); if (window is null) return;
            mapPropertiesWindow = window;
            window.Closed += (_, _) => { if (ReferenceEquals(mapPropertiesWindow, window)) mapPropertiesWindow = null; };
            if (MapPropertiesPresenter is { } present) present(window); else { window.Owner = Window.GetWindow(this); window.Show(); }
        }
        catch (InvalidOperationException) { DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), Localization.LocalizationService.Get("MapProperties.ReadOnly"), Localization.LocalizationService.Get("MapProperties.Title")); }
    }
    private void MapProperties_Click(object sender, RoutedEventArgs args) => ShowMapProperties();

    internal void ConfigureMapPropertiesDraft()
    {
        for (var index = 0; index < WorkspaceTabs.Items.Count; index++)
            ((TabItem)WorkspaceTabs.Items[index]).Visibility = index is 0 or 5 or 6 or 7 ? Visibility.Visible : Visibility.Collapsed;
        var map = (DockPanel)((TabItem)WorkspaceTabs.Items[0]).Content;
        foreach (UIElement element in map.Children) if (!ReferenceEquals(element, MapScroll)) element.Visibility = Visibility.Collapsed;
        PlayerPropertiesButton.Visibility = Visibility.Collapsed;
        Canvas.Tool = DrawingTool.Select; Canvas.GeometryOnly = true; Canvas.CellSize = 16;
        WorkspaceTabs.SelectedIndex = 7;
    }

    // Player controls have explicit child Apply buttons. Outer acceptance must
    // not silently throw away text which has not reached the working document.
    internal bool HasPendingMapPropertiesInputs()
    {
        if (pendingPlayerGeometry is not null||playerGoldPending) return true;
        if (document is null || PlayerPicker.SelectedItem is not PlayerView selected) return false;
        var row = document.PlayerSettingsRows().Single(item => item.ID == selected.Player.ID && item.Record.Offset == selected.Player.Record.Offset);
        static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
        if (row.Editable && (PlayerName.Text != row.Name || PlayerGold.Text != Number(row.Gold)
            || PlayerComputer.IsChecked == true != row.Computer || row.Health is { } health && PlayerHealth.Text != Number(health))) return true;
        if (row.ProfileEditable && PlayerProfile.SelectedItem is NamedChoiceView profile
            && PlayerSettings.AIProfiles.Any(item => item.Value == Number(row.AI)) && profile.Choice.Value != Number(row.AI)) return true;
        if (playerAIState is { } ai && (playerAIDraft.Any(item => item.Value != Number(ai.Values[item.Key])) || !playerAIRoomDraft.SequenceEqual(ai.RoomOrder))) return true;
        var geometry = document.PlayerGeometries().SingleOrDefault(item => item.ID == row.ID && item.Record.Offset == row.Record.Offset);
        if (geometry is null) return false;
        return geometry.CameraEditable && (PlayerCameraX.Text != Number(geometry.EffectiveCamera.X) || PlayerCameraY.Text != Number(geometry.EffectiveCamera.Y))
            || geometry.BuildAreaEditable && (PlayerBuildX1.Text != Number(geometry.EffectiveBuildStart.X) || PlayerBuildY1.Text != Number(geometry.EffectiveBuildStart.Y)
            || PlayerBuildX2.Text != Number(geometry.EffectiveBuildEnd.X) || PlayerBuildY2.Text != Number(geometry.EffectiveBuildEnd.Y));
    }
}
