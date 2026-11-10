using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private ScenarioNamesWindow? scenarioNamesWindow;
    private long scenarioNamesContext;
    private bool scenarioNamesOwnerClosed;
    /// <summary>Null shows the actual tool window; tests can present its real
    /// controls offscreen without changing the command route.</summary>
    public Action<ScenarioNamesWindow>? ScenarioNamesPresenter { get; set; }
    private void CloseScenarioNamesWindow()
    { scenarioNamesContext++; scenarioNamesWindow?.Close(); scenarioNamesWindow = null; CloseScenarioAliasEventWindow(); CloseScenarioParameterWindow(); CloseScenarioCreationWindow(); CloseTerrainProperties(); CloseRoomProperties(); CloseDoorProperties(); CloseTrapProperties(); CloseSpellProperties(); CloseGlobalVariables(); CloseCreatureGlobals(); }
    public void EndScenarioNamesLifetime() { scenarioNamesOwnerClosed = true; CloseScenarioNamesWindow(); ScenarioNamesButton.IsEnabled = ScenarioAliasEventButton.IsEnabled = ScenarioParameterButton.IsEnabled = ScenarioEditEventButton.IsEnabled = ScriptEditEventButton.IsEnabled = false; }
    public ScenarioNamesWindow? CreateScenarioNamesWindow()
    {
        if (scenarioNamesOwnerClosed || document is null || !CommitFields()) return null;
        var source = document; var context = scenarioNamesContext;
        return new(source, () => !scenarioNamesOwnerClosed && context == scenarioNamesContext && ReferenceEquals(source, document), CommitFields);
    }
    private void ScenarioNames_Click(object sender, RoutedEventArgs args)
    {
        if (scenarioNamesWindow is not null) { scenarioNamesWindow.Activate(); return; }
        var window = CreateScenarioNamesWindow(); if (window is null) return;
        scenarioNamesWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(scenarioNamesWindow, window)) scenarioNamesWindow = null; };
        if (ScenarioNamesPresenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
}
