using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DK2MapEditor.App.Localization;
namespace DK2MapEditor.App.Editing;
public sealed class EditorGuideWindow:Window
{
    private readonly string root;
    private string path;
    private readonly FlowDocumentScrollViewer viewer=new(){IsToolBarVisible=false,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    public string CurrentPath=>path;
    public EditorGuideWindow(string path)
    {
        this.path=path;root=Path.GetDirectoryName(path)!;Width=950;Height=720;MinWidth=600;MinHeight=400;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(TitleProperty,"Help.Guide");SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        var panel=new DockPanel{Margin=new(12)};Content=panel;
        var close=new Button{IsCancel=true,HorizontalAlignment=HorizontalAlignment.Right};close.SetResourceReference(ContentControl.ContentProperty,"Report.Close");close.Click+=(_,_)=>Close();DockPanel.SetDock(close,Dock.Bottom);panel.Children.Add(close);panel.Children.Add(viewer);
        LoadPage(path);LocalizationService.LanguageChanged+=LanguageChanged;Closed+=(_,_)=>LocalizationService.LanguageChanged-=LanguageChanged;
    }
    private void LanguageChanged(object? sender,EventArgs args)
    {
        var guide=Path.Combine(root,LocalizationService.CurrentLanguage=="ru"?"USER_GUIDE_RU.md":"USER_GUIDE_EN.md");if(File.Exists(guide))LoadPage(guide);
    }
    private void LoadPage(string file)
    {
        try
        {
            var text=File.ReadAllText(file);var document=new FlowDocument{PagePadding=new(18),FontFamily=SystemFonts.MessageFontFamily,FontSize=14};
            document.SetResourceReference(FlowDocument.BackgroundProperty,"Brush.Panel");document.SetResourceReference(FlowDocument.ForegroundProperty,"Brush.Text");
            var code=false;
            foreach(var line in text.Replace("\r\n","\n").Split('\n'))
            {
                if(line.StartsWith("```",StringComparison.Ordinal)){code=!code;continue;}
                var heading=Regex.Match(line,@"^(#{1,6})\s+(.*)$");var content=heading.Success?heading.Groups[2].Value:line;
                var paragraph=new Paragraph{Margin=new(0,3,0,5)};
                if(heading.Success){paragraph.FontSize=heading.Groups[1].Length==1?26:heading.Groups[1].Length==2?21:17;paragraph.FontWeight=FontWeights.SemiBold;paragraph.Margin=new(0,16,0,8);}
                if(code||line.TrimStart().StartsWith('|')){paragraph.FontFamily=new("Consolas");paragraph.FontSize=12;paragraph.Inlines.Add(new Run(content));}
                else AddInline(paragraph,content,file);
                document.Blocks.Add(paragraph);
            }
            path=file;viewer.Document=document;
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or ArgumentException)
        {DK2MapEditor.App.Editing.OriginalMessageBox.Show(this,LocalizationService.Get("Help.Failed")+"\n"+error.Message,LocalizationService.Get("Help.Guide"),MessageBoxButton.OK,MessageBoxImage.Information);}
    }
    private void AddInline(Paragraph paragraph,string text,string current)
    {
        var position=0;
        foreach(Match match in Regex.Matches(text,@"\[([^\]]+)\]\(([^)]+)\)|\*\*([^*]+)\*\*|`([^`]+)`"))
        {
            paragraph.Inlines.Add(new Run(text[position..match.Index]));
            if(match.Groups[1].Success)
            {
                var link=EditorHelpRules.LocalLink(root,current,match.Groups[2].Value);
                if(link is null)paragraph.Inlines.Add(new Run(match.Groups[1].Value));
                else{var inline=new Hyperlink(new Run(match.Groups[1].Value));inline.Click+=(_,_)=>LoadPage(link);paragraph.Inlines.Add(inline);}
            }
            else if(match.Groups[3].Success)paragraph.Inlines.Add(new Bold(new Run(match.Groups[3].Value)));
            else paragraph.Inlines.Add(new Run(match.Groups[4].Value){FontFamily=new("Consolas")});
            position=match.Index+match.Length;
        }
        paragraph.Inlines.Add(new Run(text[position..]));
    }
}
