using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private ScenarioAliasEventWindow? scenarioAliasEventWindow;
    public Action<ScenarioAliasEventWindow>? ScenarioAliasEventPresenter { get; set; }
    private void CloseScenarioAliasEventWindow() { scenarioAliasEventWindow?.Close(); scenarioAliasEventWindow = null; }
    private void RefreshScenarioAliasEvent(NativeRecord? record)
    {
        RefreshScenarioParameter(record);
        var available = record is not null && NativeMapDocument.HasScenarioAliasPage(EntityReferences.EventType(record)?.Name);
        ScenarioAliasEventButton.Visibility = available ? Visibility.Visible : Visibility.Collapsed;
        ScenarioAliasEventButton.IsEnabled = available && document?.CanEditScenarioNames == true && !scenarioNamesOwnerClosed;
    }
    public ScenarioAliasEventWindow? CreateScenarioAliasEventWindow()
    {
        if (scenarioNamesOwnerClosed || document is null || !CommitFields() || RecordList.SelectedItem is not RecordView selected) return null;
        if (!document.CanEditScenarioNames || !NativeMapDocument.HasScenarioAliasPage(EntityReferences.EventType(selected.Record)?.Name)) return null;
        var source = document; var context = scenarioNamesContext;
        return new(source, selected.Record, () => !scenarioNamesOwnerClosed && context == scenarioNamesContext && ReferenceEquals(source, document), CommitFields);
    }
    private void ScenarioAliasEvent_Click(object sender, RoutedEventArgs args)
    {
        if (scenarioAliasEventWindow is not null) { scenarioAliasEventWindow.Activate(); return; }
        var window = CreateScenarioAliasEventWindow(); if (window is null) return;
        scenarioAliasEventWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(scenarioAliasEventWindow, window)) scenarioAliasEventWindow = null; };
        if (ScenarioAliasEventPresenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
}
