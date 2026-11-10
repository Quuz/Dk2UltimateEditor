using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class HistoryWindow : Window
{
    private NativeMapDocument? document;
    public NativeMapDocument? Document => document;
    public HistoryWindow(NativeMapDocument source)
    {
        ArgumentNullException.ThrowIfNull(source); document = source;
        InitializeComponent(); Refresh();
        source.HistoryChanged += RefreshEvent;
        LocalizationService.LanguageChanged += RefreshEvent;
        Closed += (_, _) =>
        {
            if (document is not null) document.HistoryChanged -= RefreshEvent;
            LocalizationService.LanguageChanged -= RefreshEvent;
            document = null; UndoTree.Items.Clear(); RedoTree.Items.Clear(); SummaryText.Text = "";
        };
    }
    private void RefreshEvent(object? sender, EventArgs args) => Refresh();
    private void Refresh()
    {
        if (document is null) return;
        var history = document.GetHistory();
        SummaryText.Text = string.Format(LocalizationService.Get("History.Summary"), history.Undo.Count, history.Redo.Count,
            history.EditorChangeCount, history.SnapshotBytes, history.Depth);
        static TreeViewItem Node(HistoryOperation operation)
        {
            var item = new TreeViewItem { Header = HistoryLabels.Description(operation.Description), Tag = operation.Description, IsExpanded = true };
            item.SetResourceReference(Control.ForegroundProperty, "Brush.Text");
            foreach (var child in operation.Children) item.Items.Add(Node(child));
            return item;
        }
        UndoTree.Items.Clear(); RedoTree.Items.Clear();
        foreach (var operation in history.Undo) UndoTree.Items.Add(Node(operation));
        foreach (var operation in history.Redo) RedoTree.Items.Add(Node(operation));
    }
    private void Close_Click(object sender, RoutedEventArgs args) => Close();
}
