using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private GlobalDoorWindow? doorWindow;
    private long doorContext;
    public Action<GlobalDoorWindow>? DoorPropertiesPresenter { get; set; }
    private void CloseDoorProperties()
    { doorContext++; doorWindow?.Close(); doorWindow = null; }
    public GlobalDoorWindow? CreateDoorPropertiesWindow()
    {
        if (scenarioNamesOwnerClosed || document is null || DefinitionPicker.SelectedItem is not DefinitionView { Row.Family: "Doors" } view || !CommitFields()) return null;
        var source = document; var context = doorContext;
        return new(source, view.Row, () => !scenarioNamesOwnerClosed && context == doorContext && ReferenceEquals(source, document), CommitFields);
    }
    private void DefinitionDoorProperties_Click(object sender, RoutedEventArgs args)
    {
        if (doorWindow is not null) { doorWindow.Activate(); return; }
        var window = CreateDoorPropertiesWindow(); if (window is null) return;
        doorWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(doorWindow, window)) doorWindow = null; };
        if (DoorPropertiesPresenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
}
