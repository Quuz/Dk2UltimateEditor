using System.IO;
using System.Windows;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private readonly EditorRegionRemovalPolicy regionRemovalPolicy;
    private RegionRemovalWindow? regionRemovalWindow;
    private bool regionRemovalPending;
    private long regionRemovalContext;
    public Action<RegionRemovalWindow>? RegionRemovalPresenter { get; set; }
    private void CloseRegionRemovalWindow()
    { regionRemovalContext++; var window = regionRemovalWindow; regionRemovalWindow = null; window?.Close(); }

    private void RemoveRegionsWithConfirmation(IReadOnlyList<NativeRecord> selected, Func<bool> selectionCurrent)
    {
        if (document is null) return;
        if (regionRemovalPending) throw new InvalidOperationException("Finish the current region-removal request.");
        using var request = regionRemovalPolicy.Begin(document, selected);
        var owner = document; var context = regionRemovalContext; regionRemovalPending = true;
        try
        {
            while (!request.IsClosed)
            {
                if(context!=regionRemovalContext || !ReferenceEquals(document,owner) || !selectionCurrent()){request.Dispose();return;}
                if (!request.RequiresPrompt) { request.Accept(); continue; }
                using var window = new RegionRemovalWindow(request, () => context == regionRemovalContext && ReferenceEquals(document, owner) && selectionCurrent()) { Owner = Window.GetWindow(this) };
                regionRemovalWindow = window;
                try { if (RegionRemovalPresenter is { } presenter) presenter(window); else window.ShowDialog(); }
                finally { if (ReferenceEquals(regionRemovalWindow, window)) regionRemovalWindow = null; }
                if (window.Failure is { } error)
                {
                    if (!request.IsAccepted) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                    DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("RegionRemoval.CommittedFailure"), LocalizationService.Get("RegionRemoval.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        finally { regionRemovalPending = false; }
    }
    private void RegionDelete_Click(object sender, RoutedEventArgs args)
    {
        if (document is null || RegionPicker.SelectedItem is not RegionView view || !CommitFields()) return;
        RecordOperation(() => { RemoveRegionsWithConfirmation([view.Region.Record], () => RegionPicker.SelectedItem is RegionView current && SameRemovalRecord(current.Region.Record, view.Region.Record)); return null; });
    }
    private void RegionRemovalReset_Click(object sender, RoutedEventArgs args)
    { CloseRegionRemovalWindow(); regionRemovalPolicy.ResetSuppression(); }
    private bool TryRemoveSelectedRegions()
    {
        if (document is null) return false;
        var selected = RecordList.Items.Cast<RecordView>().Where(view => RecordList.SelectedItems.Contains(view)).Select(view => view.Record).ToArray();
        var currentRegions = document.EditorRegionRemovalRecords();
        bool Region(NativeRecord record) => currentRegions.Any(current => SameRemovalRecord(current, record));
        if (selected.Length == 0 || !selected.Any(Region)) return false;
        if (selected.Any(record => !Region(record)) || selected.Select(RemovalKind).Distinct().Count() != 1)
        {
            DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("RegionRemoval.Mixed"), LocalizationService.Get("RegionRemoval.Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }
        RecordOperation(() =>
        {
            RemoveRegionsWithConfirmation(selected, () =>
            {
                var current = RecordList.SelectedItems.Cast<RecordView>().Select(view => view.Record).ToArray();
                return current.Length == selected.Length && selected.All(record => current.Any(item => SameRemovalRecord(item, record)));
            });
            return null;
        }); return true;
    }
    private static bool SameRemovalRecord(NativeRecord left, NativeRecord right)
        => left.File == right.File && left.Offset == right.Offset && left.Length == right.Length && left.Category == right.Category;
    private static MapRegionKind RemovalKind(NativeRecord record) => record.Category switch
    {
        "GAMEAREA" => MapRegionKind.Area, "GAMEGATE" => MapRegionKind.Gate, "GAMEEFFECT" => MapRegionKind.Effect,
        _ => (MapRegionKind)int.Parse(record.Read(record.Fields.Single(field => field.Name == "node.type")), System.Globalization.CultureInfo.InvariantCulture)
    };
}
