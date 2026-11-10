using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Compile only. Physical menu/DataGrid behavior needs manual acceptance.
    private static void CheckHistoryClear(bool native,string language,string directory)
    {
        var fixture=NativeMapDocument.CreateNewMap(new("HistoryClearUI","History clear UI",18,16,NativeFormat:native));
        var path=fixture.SaveCopy(Path.Combine(directory,$"clear-history-{language}-{native}"));
        var settings=new AppSettings{HistoryDepth=37};
        var shell=new MainWindow(settings,persistSettings:false){DiscardChangesPrompt=()=>true,SaveNoticePresenter=_=>{}};
        var workspace=(EditorWorkspace)shell.FindName("Editor");var command=(MenuItem)shell.FindName("ClearHistoryMenuItem");
        try
        {
            Expect(!command.IsEnabled&&!shell.RequestClearHistory(),"Clear history enabled without a document.");
            shell.OpenMapFromPath(path);var doc=workspace.Document!;Expect(!command.IsEnabled,"Clean empty history allowed clear.");
            doc.Paint([new(3,3)],new(4,1,0));doc.Paint([new(4,3)],new(5,2,0));doc.Undo();
            Expect(command.IsEnabled,"Mixed history did not enable clear command.");
            workspace.SetSelection(new(2,2,3,3));var canvas=Control<MapCanvas>(workspace,"Canvas");var selection=canvas.Selection;var buffer=workspace.BufferedSelection;
            var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var before=doc.GetHistory();var revision=doc.EditRevision;var dirty=doc.HasChanges;
            command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Expect(!doc.CanUndo&&!doc.CanRedo&&!command.IsEnabled&&doc.HistoryDepth==37&&settings.HistoryDepth==37
                &&doc.GetHistory().ChangeCount==before.ChangeCount&&doc.EditorChangeCount==before.EditorChangeCount&&doc.EditRevision==revision&&doc.HasChanges==dirty
                &&bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&canvas.Selection==selection&&ReferenceEquals(workspace.BufferedSelection,buffer),"Actual clear menu changed document/preferences/selection/clipboard or retained history.");
            doc.Paint([new(5,4)],new(6,1,0));Expect(command.IsEnabled&&doc.CanUndo,"Clear disabled future Undo.");
            using(var pen=doc.BeginLivePenStroke(new(4,2,0)))Expect(!shell.RequestClearHistory(),"Main-window clear bypassed active-pen guard.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(command.Header as string==LocalizationService.Get("History.Clear"),"Clear history menu retained old language.");
            var foreign=NativeMapDocument.CreateNewMap(new("ForeignClear","Foreign clear",18,16,NativeFormat:native));workspace.Document=foreign;
            Expect(!shell.RequestClearHistory()&&doc.CanUndo,"Foreign workspace owner destroyed original history.");workspace.Document=doc;
            shell.Close();Expect(!shell.RequestClearHistory()&&doc.CanUndo,"Closed main-window owner destroyed history.");
        }
        finally{workspace.Document=null;shell.Close();LocalizationService.SetLanguage(language);}
    }
}
