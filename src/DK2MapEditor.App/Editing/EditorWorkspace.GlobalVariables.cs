using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private GlobalVariablesWindow? globalVariablesWindow;
    private long globalVariablesContext;
    public Action<GlobalVariablesWindow>? GlobalVariablesPresenter { get; set; }
    private void CloseGlobalVariables()
    { globalVariablesContext++; globalVariablesWindow?.Close(); globalVariablesWindow = null; }
    public GlobalVariablesWindow? CreateGlobalVariablesWindow()
    {
        if (scenarioNamesOwnerClosed || document is null || !CommitFields()
            || !document.GlobalDefinitionRows().Any(row => GlobalVariablesSession.Families.Contains(row.Family) && row.Editable)) return null;
        var source = document; var context = globalVariablesContext;
        return new(source, () => !scenarioNamesOwnerClosed && context == globalVariablesContext && ReferenceEquals(source, document), CommitFields);
    }
    private void DefinitionGlobalVariables_Click(object sender, RoutedEventArgs args)
    {
        if (globalVariablesWindow is not null) { globalVariablesWindow.Activate(); return; }
        var window = CreateGlobalVariablesWindow(); if (window is null) return;
        globalVariablesWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(globalVariablesWindow, window)) globalVariablesWindow = null; };
        if (GlobalVariablesPresenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
}
