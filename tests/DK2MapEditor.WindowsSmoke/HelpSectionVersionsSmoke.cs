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
    // Prepared WPF callers only. Do not execute the GUI during this stage.
    private static void CheckHelpSectionVersions(bool native,string language,string directory)
    {
        var source=NativeMapDocument.CreateNewMap(new("HelpVersionsUI","Help versions UI",18,16,NativeFormat:native));
        var path=source.SaveCopy(Path.Combine(directory,$"help-versions-{language}-{native}"));
        var shell=new MainWindow(new AppSettings(),persistSettings:false){DiscardChangesPrompt=()=>true,SaveNoticePresenter=_=>{}};
        var workspace=(EditorWorkspace)shell.FindName("Editor");var menu=(MenuItem)shell.FindName("SectionVersionsMenuItem");
        EditorGuideWindow? guide=null;SectionVersionsWindow? versions=null;var notices=new List<string>();
        shell.GuidePresenter=w=>guide=w;shell.SectionVersionsPresenter=w=>versions=w;shell.HelpNoticePresenter=notices.Add;
        try
        {
            Expect(!menu.IsEnabled&&!shell.RequestSectionVersions(),"Versions menu enabled without an editable document.");
            Expect(shell.RequestGuide()&&guide is not null&&guide.CurrentPath.EndsWith(language=="ru"?"USER_GUIDE_RU.md":"USER_GUIDE_EN.md",StringComparison.Ordinal),"Guide command did not load the selected canonical language without a map.");
            shell.OpenMapFromPath(path);var doc=workspace.Document!;
            var before=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=doc.EditRevision;var history=doc.GetHistory();var clipboard=workspace.BufferedSelection;
            menu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Expect(versions is not null&&versions.Title==LocalizationService.Get("Versions.Title"),"Actual versions menu did not reach the localized window.");
            var table=((DockPanel)versions!.Content).Children.OfType<DataGrid>().Single();Expect(table.IsReadOnly&&table.Items.Count==9,"Version report did not show nine read-only source sections.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(guide!.Title==LocalizationService.Get("Help.Guide")&&guide.CurrentPath.EndsWith(language=="en"?"USER_GUIDE_RU.md":"USER_GUIDE_EN.md",StringComparison.Ordinal)
                &&versions.Title==LocalizationService.Get("Versions.Title"),"Live language did not update the guide and version report.");
            Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditRevision==revision&&doc.GetHistory().ChangeCount==history.ChangeCount
                &&doc.GetHistory().Undo.Count==history.Undo.Count&&ReferenceEquals(workspace.BufferedSelection,clipboard),"Help or version viewing changed the map/history/buffer.");
            using(var pen=doc.BeginLivePenStroke(new(4,2,0)))Expect(!shell.RequestSectionVersions(),"Version report reopened during a live pen.");
            workspace.Document=null;Expect(table.Items.Count==0,"Map replacement retained the old versions window subscription.");
            shell.Close();Expect(!shell.RequestGuide()&&!shell.RequestSectionVersions(),"Closed owner reopened a help window.");
        }
        finally{guide?.Close();versions?.Close();workspace.Document=null;shell.Close();LocalizationService.SetLanguage(language);}
    }
}
