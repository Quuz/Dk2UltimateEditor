using System.Windows;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App;

public partial class MainWindow
{
    private HistoryWindow? historyWindow;
    private HistorySettingsWindow? historySettingsWindow;
    private bool historyOwnerClosed;
    private EditorHistoryMemoryPolicy? historyMemoryPolicy;
    private HistoryMemoryNoticeWindow? historyMemoryWindow;
    private bool historyMemoryPending;
    public Action<HistoryMemoryNoticeWindow>? HistoryMemoryPresenter {get;set;}
    private EditorHistoryMemoryPolicy HistoryMemoryPolicy=>historyMemoryPolicy??=new(Editor.RequestPreferences);
    public bool PresentHistoryMemoryFailure(EditorHistoryMemoryException failure,Window? owner=null)
    {
        ArgumentNullException.ThrowIfNull(failure);
        if(historyOwnerClosed || historyMemoryPending || !failure.IsSafeToNotify)return false;
        historyMemoryPending=true;
        try
        {
            using var notice=HistoryMemoryPolicy.Begin(failure,()=>!historyOwnerClosed);
            if(!notice.RequiresNotice){notice.Acknowledge();return true;}
            using var window=new HistoryMemoryNoticeWindow(notice);
            var target=owner??this;if(target.IsLoaded)window.Owner=target;historyMemoryWindow=window;
            try{if(HistoryMemoryPresenter is {} presenter)presenter(window);else window.ShowDialog();}
            finally{if(ReferenceEquals(historyMemoryWindow,window))historyMemoryWindow=null;}
            return true;
        }
        finally{historyMemoryPending=false;}
    }
    public void ResetHistoryMemoryNotices()
    {
        if(historyOwnerClosed)throw new InvalidOperationException("The history owner is closed.");
        historyMemoryWindow?.Dispose();HistoryMemoryPolicy.ResetSuppression();
    }
    private int ConfiguredHistoryDepth => settings.HistoryDepth is >= 0 and <= NativeMapDocument.MaximumHistoryDepth
        ? settings.HistoryDepth : NativeMapDocument.DefaultHistoryDepth;
    public HistoryWindow? CreateHistoryWindow() => document is null || historyOwnerClosed ? null : new(document);
    public HistorySettingsWindow? CreateHistorySettingsWindow() => historyOwnerClosed ? null : new(ConfiguredHistoryDepth, depth =>
    {
        if (historyOwnerClosed) throw new InvalidOperationException("The settings owner is closed.");
        document?.SetHistoryDepth(depth); settings.HistoryDepth = depth;
        RefreshHistoryCommands(); SaveSettings();
    },ResetHistoryMemoryNotices);
    private void RefreshHistoryCommands()
    {
        UndoMenuItem.IsEnabled = document?.CanUndo == true;
        RedoMenuItem.IsEnabled = document?.CanRedo == true;
        HistoryMenuItem.IsEnabled = document is not null;
        ClearHistoryMenuItem.IsEnabled = !historyOwnerClosed && document is not null && ReferenceEquals(Editor.Document,document) && document.CanClearHistory;
    }
    public bool RequestClearHistory()
    {
        if(historyOwnerClosed||saveOwnerClosed||document is not {} source||!ReferenceEquals(Editor.Document,source)||!source.CanClearHistory)return false;
        source.ClearHistory();return true;
    }
    private void ClearHistory_Click(object sender,RoutedEventArgs args)=>RequestClearHistory();
    private void Document_HistoryChanged(object? sender, EventArgs args) => RefreshHistoryCommands();
    private void History_Click(object sender, RoutedEventArgs args)
    {
        var window = CreateHistoryWindow(); if (window is null) return;
        historyWindow = window; window.Owner = this;
        window.Closed += (_, _) => { if (ReferenceEquals(historyWindow, window)) historyWindow = null; };
        window.ShowDialog();
    }
    private void HistorySettings_Click(object sender, RoutedEventArgs args)
    {
        var window = CreateHistorySettingsWindow(); if (window is null) return;
        historySettingsWindow = window; window.Owner = this;
        window.Closed += (_, _) => { if (ReferenceEquals(historySettingsWindow, window)) historySettingsWindow = null; };
        window.ShowDialog();
    }
    private void DetachHistoryDocument()
    {
        historyWindow?.Close(); historyWindow = null;
        if (document is not null) document.HistoryChanged -= Document_HistoryChanged;
    }
    private void AttachHistoryDocument()
    {
        if (document is not null) { document.SetHistoryDepth(ConfiguredHistoryDepth); document.HistoryChanged += Document_HistoryChanged; }
        RefreshHistoryCommands();
    }
}
