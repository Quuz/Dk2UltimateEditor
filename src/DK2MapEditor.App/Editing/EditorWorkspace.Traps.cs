using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private GlobalTrapWindow? trapWindow;
    private long trapContext;
    public Action<GlobalTrapWindow>? TrapPropertiesPresenter { get; set; }
    private void CloseTrapProperties() { trapContext++; trapWindow?.Close(); trapWindow = null; }
    public GlobalTrapWindow? CreateTrapPropertiesWindow()
    {
        if (scenarioNamesOwnerClosed || document is null || DefinitionPicker.SelectedItem is not DefinitionView { Row.Family: "Traps" } view || !CommitFields()) return null;
        var source = document; var context = trapContext;
        return new(source, view.Row, () => !scenarioNamesOwnerClosed && context == trapContext && ReferenceEquals(source, document), CommitFields);
    }
    private void DefinitionTrapProperties_Click(object sender, RoutedEventArgs args)
    {
        if (trapWindow is not null) { trapWindow.Activate(); return; }
        var window = CreateTrapPropertiesWindow(); if (window is null) return;
        trapWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(trapWindow, window)) trapWindow = null; };
        if (TrapPropertiesPresenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
}
