using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private GlobalTerrainWindow? terrainWindow;
    private long terrainContext;
    public Action<GlobalTerrainWindow>? TerrainPropertiesPresenter { get; set; }
    private void CloseTerrainProperties()
    { terrainContext++; terrainWindow?.Close(); terrainWindow = null; }
    public GlobalTerrainWindow? CreateTerrainPropertiesWindow()
    {
        if (scenarioNamesOwnerClosed || document is null || DefinitionPicker.SelectedItem is not DefinitionView { Row.Family: "Terrains" } view || !CommitFields()) return null;
        var source = document; var context = terrainContext;
        return new(source, view.Row, () => !scenarioNamesOwnerClosed && context == terrainContext && ReferenceEquals(source, document), CommitFields);
    }
    private void DefinitionTerrainProperties_Click(object sender, RoutedEventArgs args)
    {
        if (terrainWindow is not null) { terrainWindow.Activate(); return; }
        var window = CreateTerrainPropertiesWindow(); if (window is null) return;
        terrainWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(terrainWindow, window)) terrainWindow = null; };
        if (TerrainPropertiesPresenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
}
