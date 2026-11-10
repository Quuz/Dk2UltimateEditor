using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private MapLevelOptionsWindow? levelOptionsWindow;
    private long levelOptionsContext;
    private bool levelOptionsOwnerClosed;
    public Action<MapLevelOptionsWindow>? LevelOptionsPresenter { get; set; }
    private void CloseLevelOptionsWindow() { levelOptionsContext++; levelOptionsWindow?.Close(); levelOptionsWindow = null; }
    public void EndLevelOptionsLifetime() { levelOptionsOwnerClosed = true; CloseLevelOptionsWindow(); LevelOptionsButton.IsEnabled = false; }
    public MapLevelOptionsWindow? CreateLevelOptionsWindow()
    {
        if (levelOptionsOwnerClosed || document is null || !CommitFields()) return null;
        var source = document; var context = levelOptionsContext;
        return new(source, () => !levelOptionsOwnerClosed && context == levelOptionsContext && ReferenceEquals(source, document), CommitFields);
    }
    private void LevelOptions_Click(object sender, RoutedEventArgs args)
    {
        if (levelOptionsWindow is not null) { levelOptionsWindow.Activate(); return; }
        var window = CreateLevelOptionsWindow(); if (window is null) return;
        levelOptionsWindow = window; window.Closed += (_, _) => { if (ReferenceEquals(levelOptionsWindow, window)) levelOptionsWindow = null; };
        if (LevelOptionsPresenter is { } present) present(window); else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
}
