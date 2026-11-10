using System.IO;
using System.Windows;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
namespace DK2MapEditor.App;
public partial class MainWindow
{
    private EditorGuideWindow? guideWindow;
    private SectionVersionsWindow? versionsWindow;
    public Action<EditorGuideWindow>? GuidePresenter{get;set;}
    public Action<SectionVersionsWindow>? SectionVersionsPresenter{get;set;}
    public Action<string>? HelpNoticePresenter{get;set;}
    public bool RequestGuide()
    {
        if(saveOwnerClosed)return false;
        if(guideWindow is {} previous){previous.Activate();return true;}
        var path=EditorHelpRules.FindGuide(AppContext.BaseDirectory,LocalizationService.CurrentLanguage);
        if(path is null){HelpNotice(LocalizationService.Get("Help.Missing"));return false;}
        try
        {
            var window=new EditorGuideWindow(path);guideWindow=window;window.Closed+=(_,_)=>{if(ReferenceEquals(guideWindow,window))guideWindow=null;};
            if(GuidePresenter is {} present)present(window);else{window.Owner=this;window.Show();}return true;
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or ArgumentException)
        {HelpNotice(LocalizationService.Get("Help.Failed")+"\n"+error.Message);return false;}
    }
    public bool RequestSectionVersions()
    {
        if(saveOwnerClosed||document is not {} source||!ReferenceEquals(Editor.Document,source)||source.HasActivePenStroke)return false;
        versionsWindow?.Close();var window=new SectionVersionsWindow(source);versionsWindow=window;
        window.Closed+=(_,_)=>{if(ReferenceEquals(versionsWindow,window))versionsWindow=null;};
        if(SectionVersionsPresenter is {} present)present(window);else{window.Owner=this;window.Show();}return true;
    }
    private void HelpNotice(string text)
    {if(HelpNoticePresenter is {} present)present(text);else DK2MapEditor.App.Editing.OriginalMessageBox.Show(this,text,LocalizationService.Get("Help.Guide"),MessageBoxButton.OK,MessageBoxImage.Information);}
    private void Guide_Click(object sender,RoutedEventArgs args)=>RequestGuide();
    private void SectionVersions_Click(object sender,RoutedEventArgs args)=>RequestSectionVersions();
    private void CloseHelpWindows(){guideWindow?.Close();guideWindow=null;CloseSectionVersions();}
    private void CloseSectionVersions(){versionsWindow?.Close();versionsWindow=null;}
    private void CloseVersionsOnContextChange(object? sender,EventArgs args)=>CloseSectionVersions();
}
