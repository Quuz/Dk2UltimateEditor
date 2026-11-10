using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>One Test/Compile/Run task, with a retained result rather than a
/// transient notice. Active work cannot be closed/cancelled halfway through
/// publication; Run is a distinct user action after successful generation.</summary>
public sealed class CompilerTaskWindow:Window
{
    private readonly ScenarioCheckPurpose purpose;
    private readonly DateTime started=DateTime.Now;
    private readonly string mapName;
    private Func<bool> current;
    private readonly Func<MapDiagnosticRegion,bool> navigate;
    private readonly List<(string Key,string? Detail)> entries=[];
    private MapDiagnostic[] diagnostics=[];
    private readonly TextBox log=new(){IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    private readonly DataGrid list=new(){AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,IsReadOnly=true,
        SelectionMode=DataGridSelectionMode.Single,SelectionUnit=DataGridSelectionUnit.FullRow,CanUserSortColumns=false,EnableRowVirtualization=true};
    private readonly TextBlock phase=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)},counts=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)},details=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly ProgressBar progress=new(){Height=16,Margin=new(4),IsIndeterminate=true};
    private readonly Button button=new(){MinWidth=110,IsDefault=true,IsEnabled=false};
    private Func<bool>? run;
    private bool closed,refreshing,forceClose;
    private string phaseKey="CompilerTask.Test";
    private sealed record Row(MapDiagnostic Diagnostic,string Severity,string Message,string Position)
    { public System.Windows.Media.Imaging.BitmapSource? Icon=>OriginalReportImages.For(Diagnostic.Severity); }
    public bool IsWorking{get;private set;}=true;
    public bool Succeeded{get;private set;}
    public IReadOnlyList<MapDiagnostic> Diagnostics=>Array.AsReadOnly(diagnostics);
    public CompilerTaskWindow(ScenarioCheckPurpose purpose,string mapName,Func<bool> current,Func<MapDiagnosticRegion,bool> navigate)
    {
        this.purpose=purpose;this.mapName=mapName;this.current=current;this.navigate=navigate;
        NameScope.SetNameScope(this,new NameScope());Width=800;Height=600;MinWidth=620;MinHeight=420;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        var panel=new DockPanel{Margin=new(12)};Content=panel;
        var footer=new DockPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);
        DockPanel.SetDock(button,Dock.Right);footer.Children.Add(button);footer.Children.Add(progress);
        var header=new StackPanel();header.Children.Add(phase);header.Children.Add(counts);DockPanel.SetDock(header,Dock.Top);panel.Children.Add(header);
        var grid=new Grid();grid.RowDefinitions.Add(new(){Height=new(2,GridUnitType.Star)});grid.RowDefinitions.Add(new(){Height=new(6)});
        grid.RowDefinitions.Add(new(){Height=new(3,GridUnitType.Star)});grid.RowDefinitions.Add(new(){Height=GridLength.Auto});panel.Children.Add(grid);
        grid.Children.Add(log);var splitter=new GridSplitter{Height=6,HorizontalAlignment=HorizontalAlignment.Stretch};Grid.SetRow(splitter,1);grid.Children.Add(splitter);
        Grid.SetRow(list,2);grid.Children.Add(list);var detailScroll=new ScrollViewer{Content=details,MaxHeight=120,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetRow(detailScroll,3);grid.Children.Add(detailScroll);
        foreach(var (key,property,width) in new[]{("Report.Severity",nameof(Row.Severity),110d),("Report.Message",nameof(Row.Message),440d),("Report.Position",nameof(Row.Position),140d)})
        {
            var label=new TextBlock();label.SetResourceReference(TextBlock.TextProperty,key);
            if(property==nameof(Row.Severity))
            {list.Columns.Add(new DataGridTemplateColumn{Header=label,CellTemplate=OriginalReportImages.Template(),Width=new(width),MinWidth=80});continue;}
            var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextWrappingProperty,TextWrapping.Wrap));
            list.Columns.Add(new DataGridTextColumn{Header=label,Binding=new Binding(property),ElementStyle=style,Width=new(width),MinWidth=80});
        }
        list.SetResourceReference(Control.BackgroundProperty,"Brush.Panel");list.SetResourceReference(Control.ForegroundProperty,"Brush.Text");
        log.SetResourceReference(Control.BackgroundProperty,"Brush.Panel");log.SetResourceReference(Control.ForegroundProperty,"Brush.Text");
        foreach(var (name,control) in new (string,object)[]{("CompilerLog",log),("CompilerRows",list),("CompilerPhase",phase),("CompilerCounts",counts),("CompilerAction",button)})RegisterName(name,control);
        list.SelectionChanged+=(_,_)=>
        {
            if(refreshing)return;RefreshDetails();
            if(!closed&&!IsWorking&&current()&&list.SelectedItem is Row{Diagnostic.Region:{} region})navigate(region);
        };
        button.Click+=(_,_)=>PerformAction();
        Closing+=(_,args)=>{if(IsWorking&&!forceClose)args.Cancel=true;};
        Closed+=(_,_)=>{closed=true;run=null;LocalizationService.LanguageChanged-=LanguageChanged;};
        LocalizationService.LanguageChanged+=LanguageChanged;Refresh();
    }
    public void SetPhase(string key,string? detail=null){if(closed)return;phaseKey=key;entries.Add((key,detail));Refresh();}
    public void SetDiagnostics(IReadOnlyList<MapDiagnostic> rows){if(closed)return;diagnostics=rows.ToArray();Refresh();}
    public void AppendMessage(string text){if(closed)return;entries.Add(("",text));Refresh();}
    public void Finish(bool succeeded,Func<bool> resultCurrent,Func<bool>? launch=null)
    {
        if(closed)return;IsWorking=false;Succeeded=succeeded;current=resultCurrent;run=succeeded?launch:null;
        progress.IsIndeterminate=false;progress.Minimum=0;progress.Maximum=1;progress.Value=succeeded?1:0;
        SetPhase(succeeded?(run is null?"CompilerTask.Done":"CompilerTask.Ready"):"CompilerTask.Aborted");
    }
    public bool PerformAction()
    {
        if(closed||IsWorking)return false;
        if(run is null){Close();return true;}
        if(!current()){RefreshCurrent();return false;}
        var launch=run;run=null;
        try
        {
            if(!launch()){Succeeded=false;SetPhase("CompilerTask.Aborted");return false;}
            SetPhase("CompilerTask.Launched");return true;
        }
        catch(Exception error)when(error is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or AggregateException)
        {Succeeded=false;AppendMessage(error.Message);SetPhase("CompilerTask.Aborted");return false;}
        finally{RefreshCurrent();}
    }
    public void CloseForOwner(){forceClose=true;Close();}
    public void RefreshCurrent()
    {
        button.SetResourceReference(ContentControl.ContentProperty,run is null?"Report.Close":"CompilerTask.Run");
        button.IsEnabled=!closed&&!IsWorking;
        if(!IsWorking&&run is not null&&!current())
        {phaseKey="CompilerTask.Stale";phase.SetResourceReference(TextBlock.TextProperty,phaseKey);run=null;button.SetResourceReference(ContentControl.ContentProperty,"Report.Close");}
    }
    private void LanguageChanged(object? sender,EventArgs args)=>Refresh();
    private void Refresh()
    {
        SetResourceReference(TitleProperty,"UnusedScripts.Purpose."+purpose);
        var selected=(list.SelectedItem as Row)?.Diagnostic;refreshing=true;
        try
        {
            list.ItemsSource=diagnostics.Select(d=>new Row(d,LocalizationService.Get("Report."+d.Severity),LocalizationService.Get("Diagnostic."+d.Code,d.Message),
                d.Region is {} r?$"{r.X}, {r.Y} · {r.Width}×{r.Height}":"")).ToArray();
            list.SelectedItem=((Row[])list.ItemsSource).FirstOrDefault(row=>ReferenceEquals(row.Diagnostic,selected));
            log.Text=string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("CompilerTask.Started"),mapName,started)+"\n\n"+
                string.Join("\n",entries.Select(entry=>(entry.Key.Length==0?"":LocalizationService.Get(entry.Key))+(entry.Detail is null?"":"\n"+entry.Detail)));
            log.ScrollToEnd();phase.SetResourceReference(TextBlock.TextProperty,phaseKey);
            counts.Text=string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("CompilerTask.Counts"),diagnostics.Count(d=>d.Severity==DiagnosticSeverity.Error),diagnostics.Count(d=>d.Severity==DiagnosticSeverity.Warning),diagnostics.Count(d=>d.Severity==DiagnosticSeverity.Information));
        }
        finally{refreshing=false;}
        RefreshDetails();RefreshCurrent();
    }
    private void RefreshDetails()
    {
        details.Text=list.SelectedItem is Row row?row.Diagnostic.Code+"\n"+row.Message+(row.Message==row.Diagnostic.Message?"":"\n"+row.Diagnostic.Message)
            +(row.Diagnostic.FilePath is {} path?"\n"+path:""):"";
    }
}
