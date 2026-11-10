using System.Windows;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private readonly EditorThingRequestPolicy inspectorRemovalPolicy;
    private RegionRemovalWindow? inspectorRemovalWindow;
    private bool inspectorRemovalPending;
    private long inspectorRemovalContext;
    public Action<RegionRemovalWindow>? InspectorObjectRemovalPresenter { get; set; }
    public Action<string>? RecordRemovalRefusalPresenter { get; set; }

    private void CloseInspectorObjectRemoval()
    {
        inspectorRemovalContext++;
        var window = inspectorRemovalWindow;
        inspectorRemovalWindow = null;
        window?.Close();
    }

    private void ShowRecordRemovalRefusal(string key = "Editor.RemovalMixed")
    {
        var message = LocalizationService.Get(key);
        if (RecordRemovalRefusalPresenter is { } presenter) presenter(message);
        else DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), message, LocalizationService.Get("Editor.Delete"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // Capture in displayed order, as the source manager walks its list rows.
    // Do not silently delete just the primary item of an unsupported selection.
    private NativeRecord[] SelectedRemovalRecords() => RecordList.Items.Cast<RecordView>()
        .Where(view => RecordList.SelectedItems.Contains(view)).Select(view => view.Record).ToArray();

    private bool TryRemoveSelectedObjects()
    {
        if (document is null) return false;
        var selected = SelectedRemovalRecords();
        var physical = document.ObjectManagerEntries();
        bool Physical(NativeRecord record) => physical.Any(entry => SameRemovalRecord(entry.Record, record));
        if (!selected.Any(Physical)) return false;
        if (selected.Any(record => !Physical(record))) { ShowRecordRemovalRefusal(); return true; }
        if (inspectorRemovalPending) throw new InvalidOperationException("Finish the current object-removal request.");

        var owner = document;
        var context = inspectorRemovalContext;
        bool Current()
        {
            if (context != inspectorRemovalContext || !ReferenceEquals(document, owner)) return false;
            var current = SelectedRemovalRecords();
            return current.Length == selected.Length && selected.All(record => current.Any(item => SameRemovalRecord(item, record)));
        }
        using var request = inspectorRemovalPolicy.Begin(owner, selected, EditorThingCommand.Remove);
        inspectorRemovalPending = true;
        try
        {
            while (!request.IsClosed)
            {
                if (!Current()) { request.Dispose(); return true; }
                if (!request.RequiresPrompt) { request.Accept(); continue; }
                using var window = new RegionRemovalWindow(request, Current) { Owner = Window.GetWindow(this) };
                inspectorRemovalWindow = window;
                try { if (InspectorObjectRemovalPresenter is { } presenter) presenter(window); else window.ShowDialog(); }
                finally { if (ReferenceEquals(inspectorRemovalWindow, window)) inspectorRemovalWindow = null; }
                if (window.Failure is { } error)
                {
                    if (!request.IsAccepted) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                    DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("RegionRemoval.CommittedFailure"), LocalizationService.Get("ThingRequest.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        finally { inspectorRemovalPending = false; }
        return true;
    }
}
