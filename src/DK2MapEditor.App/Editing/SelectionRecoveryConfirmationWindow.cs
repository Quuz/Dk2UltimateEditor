using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.App.Editing;

public sealed record SelectionRecoveryRequest(string FileName,int Width,int Height,int CompatibleObjects,
    bool DirectInsertion,SelectionFile.ImportRecovery Recovery);

public sealed class SelectionRecoveryConfirmationWindow : Window
{
    private readonly SelectionRecoveryRequest request;
    private readonly TextBlock summary=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private bool closed;
    public bool Accepted { get; private set; }
    public SelectionRecoveryConfirmationWindow(SelectionRecoveryRequest request)
    {
        if(request.Recovery==SelectionFile.ImportRecovery.None)throw new ArgumentException("Recovery confirmation requires incomplete archive data.",nameof(request));
        this.request=request;NameScope.SetNameScope(this,new NameScope());
        SetResourceReference(TitleProperty,"Editor.SelectionRecovery.Title");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=560;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel{Margin=new(16)};Content=panel;panel.SetResourceReference(Panel.BackgroundProperty,"Brush.Background");
        panel.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty,"Brush.Text");
        var question=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4)};
        question.SetResourceReference(TextBlock.TextProperty,request.Recovery==SelectionFile.ImportRecovery.TerrainOnly
            ?"Editor.SelectionRecovery.TerrainQuestion":"Editor.SelectionRecovery.HistoricalQuestion");
        RegisterName("SelectionRecoveryQuestion",question);panel.Children.Add(question);RegisterName("SelectionRecoverySummary",summary);panel.Children.Add(summary);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(buttons);
        var yes=new Button{IsDefault=true};yes.SetResourceReference(ContentControl.ContentProperty,"Editor.SelectionRecovery.Load");yes.Click+=(_,_)=>Respond(true);RegisterName("SelectionRecoveryLoad",yes);buttons.Children.Add(yes);
        var no=new Button{IsCancel=true};no.SetResourceReference(ContentControl.ContentProperty,"Editor.SelectionRecovery.Cancel");no.Click+=(_,_)=>Respond(false);RegisterName("SelectionRecoveryCancel",no);buttons.Children.Add(no);
        LocalizationService.LanguageChanged+=LanguageChanged;LanguageChanged(null,EventArgs.Empty);
        Closed+=(_,_)=>{closed=true;LocalizationService.LanguageChanged-=LanguageChanged;};
    }
    private void LanguageChanged(object? sender,EventArgs args)
        =>summary.Text=string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("Editor.SelectionRecovery.Summary"),request.FileName,request.Width,request.Height,request.CompatibleObjects);
    public bool Respond(bool accept)
    {
        if(closed)return false;Accepted=accept;Close();return true;
    }
}
