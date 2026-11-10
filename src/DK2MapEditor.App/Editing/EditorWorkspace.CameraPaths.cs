using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private CameraPathWindow? cameraPathWindow;
    internal CameraPathWindow? AutomationCameraLibrary => cameraPathWindow;
    private void RefreshCameraPathControl(NativeRecord? record)
    {
        CameraPathButton.Visibility = record is not null && CameraPaths.IsCameraAction(record) ? Visibility.Visible : Visibility.Collapsed;
        CameraPathButton.IsEnabled = document is not null && record is not null && CameraPaths.Supported(record);
    }
    public CameraPathWindow? CreateCameraPathWindow()
    {
        if (document is null || RecordList.SelectedItem is not RecordView view || !CameraPaths.Supported(view.Record)) return null;
        var source = document; var draft = source.CreateCameraPathDraft(view.Record);
        return new(draft.PathID, choice =>
        {
            if (!ReferenceEquals(document, source) || RecordList.SelectedItem is not RecordView current
                || current.Record.File != draft.Record.File || current.Record.Offset != draft.Record.Offset || current.Record.Category != draft.Record.Category)
                throw new InvalidOperationException("Select the unchanged source camera action.");
            if (!CommitFields()) throw new InvalidOperationException("Commit valid property values before choosing a camera path.");
            source.ApplyCameraPath(draft, choice.ID);
        });
    }
    private void CameraPath_Click(object sender, RoutedEventArgs e)
    {
        var window = CreateCameraPathWindow(); if (window is null) return;
        cameraPathWindow = window; window.Owner = Window.GetWindow(this);
        window.Closed += (_, _) => { if (ReferenceEquals(cameraPathWindow, window)) cameraPathWindow = null; };
        window.ShowDialog();
    }
    private void CloseCameraPathWindow() { cameraPathWindow?.Close(); cameraPathWindow = null; }
}
