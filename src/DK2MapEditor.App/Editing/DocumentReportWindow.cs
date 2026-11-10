using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.App.Editing;

public enum DocumentReportPurpose { Load,Validation,Compile }
public sealed class DocumentReportWindow : Window
{
    private readonly MapDiagnostic[] diagnostics;
    private readonly DocumentReportPurpose purpose;
    private readonly bool offered;
    private readonly Func<bool> current;
    private readonly Func<MapDiagnosticRegion,bool>? navigate;
    private readonly DataGrid list=new(){AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,IsReadOnly=true,SelectionMode=DataGridSelectionMode.Single,
        SelectionUnit=DataGridSelectionUnit.FullRow,EnableRowVirtualization=true,CanUserSortColumns=false};
    private readonly TextBlock count=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)},details=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)},state=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly Button accept=new();
    private bool closed,refreshing;
    private sealed record Row(MapDiagnostic Diagnostic,string Severity,string Message,string Position)
    { public System.Windows.Media.Imaging.BitmapSource? Icon=>OriginalReportImages.For(Diagnostic.Severity); }
    public IReadOnlyList<MapDiagnostic> Diagnostics=>Array.AsReadOnly(diagnostics);
    public bool Accepted {get;private set;}
    public DocumentReportWindow(DocumentReportPurpose purpose,IReadOnlyList<MapDiagnostic> diagnostics,bool offerContinue,Func<bool> current,Func<MapDiagnosticRegion,bool>? navigate=null)
    {
        this.purpose=purpose;this.diagnostics=diagnostics.ToArray();offered=offerContinue;this.current=current;this.navigate=navigate;
        NameScope.SetNameScope(this,new NameScope());Width=900;Height=620;MinWidth=620;MinHeight=420;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        var panel=new DockPanel{Margin=new(14)};Content=panel;
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};footer.Children.Add(state);footer.Children.Add(buttons);
        accept.SetResourceReference(ContentControl.ContentProperty,"Report.Continue");accept.Visibility=offered?Visibility.Visible:Visibility.Collapsed;
        accept.Click+=(_,_)=>{if(Accept())Close();};buttons.Children.Add(accept);RegisterName("ReportContinue",accept);
        var close=new Button{IsCancel=true,IsDefault=true};close.SetResourceReference(ContentControl.ContentProperty,offered?"Editor.Cancel":"Report.Close");close.Click+=(_,_)=>Close();buttons.Children.Add(close);RegisterName("ReportClose",close);
        DockPanel.SetDock(count,Dock.Top);panel.Children.Add(count);RegisterName("ReportCount",count);RegisterName("ReportState",state);
        var detailScroll=new ScrollViewer{Content=details,MaxHeight=150,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};DockPanel.SetDock(detailScroll,Dock.Bottom);panel.Children.Add(detailScroll);
        RegisterName("ReportDetails",details);RegisterName("ReportRows",list);
        foreach(var (key,property,width) in new[]{("Report.Severity",nameof(Row.Severity),120d),("Report.Message",nameof(Row.Message),520d),("Report.Position",nameof(Row.Position),150d)})
        {
            var header=new TextBlock();header.SetResourceReference(TextBlock.TextProperty,key);
            if(property==nameof(Row.Severity))
            {list.Columns.Add(new DataGridTemplateColumn{Header=header,CellTemplate=OriginalReportImages.Template(),Width=new(width),MinWidth=80});continue;}
            var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextWrappingProperty,TextWrapping.Wrap));
            list.Columns.Add(new DataGridTextColumn{Header=header,Binding=new Binding(property),ElementStyle=style,Width=new(width),MinWidth=80});
        }
        list.SetResourceReference(Control.BackgroundProperty,"Brush.Panel");list.SetResourceReference(Control.ForegroundProperty,"Brush.Text");panel.Children.Add(list);
        list.SelectionChanged+=(_,_)=>
        {
            if(refreshing)return;RefreshDetails();
            if(!closed&&current()&&list.SelectedItem is Row{Diagnostic.Region:{} region})navigate?.Invoke(region);
        };
        LocalizationService.LanguageChanged+=LanguageChanged;Closed+=(_,_)=>{closed=true;LocalizationService.LanguageChanged-=LanguageChanged;};
        Refresh();
    }
    private void LanguageChanged(object? sender,EventArgs args)=>Refresh();
    private void Refresh()
    {
        SetResourceReference(TitleProperty,"Report."+purpose);
        var selected=(list.SelectedItem as Row)?.Diagnostic;refreshing=true;
        try
        {
            list.ItemsSource=diagnostics.Select(d=>new Row(d,LocalizationService.Get("Report."+d.Severity),
                LocalizationService.Get("Diagnostic."+d.Code,d.Message),d.Region is {} r?$"{r.X}, {r.Y} · {r.Width}×{r.Height}":"")).ToArray();
            list.SelectedItem=((Row[])list.ItemsSource).FirstOrDefault(row=>ReferenceEquals(row.Diagnostic,selected));
            count.Text=string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("Report.Count"),diagnostics.Length);
        }
        finally{refreshing=false;}
        RefreshDetails();RefreshCurrent();
    }
    private void RefreshDetails()
    {
        if(list.SelectedItem is not Row row){details.Text="";return;}
        details.Text=row.Diagnostic.Code+"\n"+row.Message+(row.Message==row.Diagnostic.Message?"":"\n"+row.Diagnostic.Message)
            +(row.Diagnostic.FilePath is {} path?"\n"+path:"");
    }
    public void RefreshCurrent()
    {
        var valid=!closed&&current();accept.IsEnabled=EditorReportRules.CanContinue(offered,valid,diagnostics);
        state.SetResourceReference(TextBlock.TextProperty,valid?"Report.Hint":"Report.Stale");
    }
    public bool Accept()
    {
        RefreshCurrent();if(!accept.IsEnabled)return false;Accepted=true;return true;
    }
    public void Cancel()=>Close();
}
