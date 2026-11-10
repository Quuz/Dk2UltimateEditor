using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private GlobalRoomWindow? roomWindow;
    private long roomContext;
    public Action<GlobalRoomWindow>? RoomPropertiesPresenter { get; set; }
    private void CloseRoomProperties()
    { roomContext++; roomWindow?.Close(); roomWindow = null; }
    public GlobalRoomWindow? CreateRoomPropertiesWindow()
    {
        if (scenarioNamesOwnerClosed || document is null || DefinitionPicker.SelectedItem is not DefinitionView { Row.Family: "Rooms" } view || !CommitFields()) return null;
        var source = document; var context = roomContext;
        return new(source, view.Row, () => !scenarioNamesOwnerClosed && context == roomContext && ReferenceEquals(source, document), CommitFields);
    }
    private void DefinitionRoomProperties_Click(object sender, RoutedEventArgs args)
    {
        if (roomWindow is not null) { roomWindow.Activate(); return; }
        var window = CreateRoomPropertiesWindow(); if (window is null) return;
        roomWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(roomWindow, window)) roomWindow = null; };
        if (RoomPropertiesPresenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
}
