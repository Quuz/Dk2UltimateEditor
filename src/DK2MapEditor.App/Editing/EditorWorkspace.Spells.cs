using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private GlobalSpellWindow? spellWindow;
    private long spellContext;
    public Action<GlobalSpellWindow>? SpellPropertiesPresenter { get; set; }
    private void CloseSpellProperties() { spellContext++; spellWindow?.Close(); spellWindow = null; }
    public GlobalSpellWindow? CreateSpellPropertiesWindow()
    {
        if (scenarioNamesOwnerClosed || document is null || DefinitionPicker.SelectedItem is not DefinitionView { Row.Family: "Spells" } view || !CommitFields()) return null;
        var source = document; var context = spellContext;
        return new(source, view.Row, () => !scenarioNamesOwnerClosed && context == spellContext && ReferenceEquals(source, document), CommitFields);
    }
    private void DefinitionSpellProperties_Click(object sender, RoutedEventArgs args)
    {
        if (spellWindow is not null) { spellWindow.Activate(); return; }
        var window = CreateSpellPropertiesWindow(); if (window is null) return;
        spellWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(spellWindow, window)) spellWindow = null; };
        if (SpellPropertiesPresenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
}
