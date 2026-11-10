using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using Microsoft.Win32;

namespace DK2MapEditor.App.Editing;

/// <summary>Private child of map properties. Typed values remain in the child
/// until its Apply, then in the parent's working document until common Apply.</summary>
public sealed class CreatureLevelModifierWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly Func<bool> ownerCurrent,commitFields;
    private readonly ComboBox statistics=new(){DisplayMemberPath="Label",Margin=new(4)};
    private readonly TextBox[] values=Enumerable.Range(0,10).Select(_=>new TextBox{MinWidth=80,Margin=new(4)}).ToArray();
    private readonly TextBlock source=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)},status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly Button apply=new(),reset=new(),choose=new(),export=new();
    private readonly List<RepeatButton> spinners=[];
    private CreatureLevelModifierSession? session;
    private CreatureLevelModifierCatalogue? catalogue;
    private readonly long openedRevision;
    private bool closed,stale,updating,applying,busy,dirty;
    private string statusKey="Ready";
    private sealed record Stat(uint ID,string Label);
    public long InputRevision {get;private set;}
    public CreatureLevelModifierCatalogue? SelectedCatalogue=>catalogue;
    public bool HasPendingInputs=>!closed&&(busy||dirty||session is {IsClosed:false}&&session.Draft.Changes.Count!=0);
    public Func<string?>? CataloguePicker {get;set;}
    public Func<string?>? ExportTargetPicker {get;set;}
    public Action<string,byte[],Func<bool>>? CatalogueExporter {get;set;}

    public CreatureLevelModifierWindow(NativeMapDocument document,Func<bool> ownerCurrent,Func<bool> commitFields,
        GameResourceSettings resources,CreatureLevelModifierCatalogue? initialCatalogue=null)
    {
        this.document=document;this.ownerCurrent=ownerCurrent;this.commitFields=commitFields;openedRevision=document.EditRevision;
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"CreatureLevel.Title");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=620;Height=570;MinWidth=540;MinHeight=460;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(12)};Content=panel;
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);footer.Children.Add(status);RegisterName("CreatureLevelStatus",status);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};footer.Children.Add(buttons);
        void Add(Button button,string key,string name,Action action)
        {button.SetResourceReference(ContentControl.ContentProperty,key);button.Click+=(_,_)=>action();RegisterName(name,button);buttons.Children.Add(button);}
        Add(reset,"CreatureLevel.Reset","CreatureLevelReset",ResetValues);Add(export,"CreatureLevel.Export","CreatureLevelExport",()=>ExportCatalogue());
        apply.IsDefault=true;
        Add(apply,"GlobalVariables.Apply","CreatureLevelApply",()=>TryApply());var cancel=new Button{IsCancel=true};Add(cancel,"MapProperties.Cancel","CreatureLevelCancel",Close);
        var body=new StackPanel();panel.Children.Add(new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4)};hint.SetResourceReference(TextBlock.TextProperty,"CreatureLevel.Hint");body.Children.Add(hint);
        body.Children.Add(source);RegisterName("CreatureLevelSource",source);
        choose.SetResourceReference(ContentControl.ContentProperty,"CreatureLevel.Choose");choose.Click+=(_,_)=>ChooseCatalogue();RegisterName("CreatureLevelChoose",choose);body.Children.Add(choose);
        var label=new TextBlock{Margin=new(4,10,4,2)};label.SetResourceReference(TextBlock.TextProperty,"CreatureLevel.Statistic");body.Children.Add(label);
        RegisterName("CreatureLevelStatistic",statistics);body.Children.Add(statistics);
        var grid=new UniformGrid{Columns=3,Rows=3,Margin=new(0,8,0,8)};body.Children.Add(grid);
        void Numeric(Panel container,int index,string name)
        {
            var row=new DockPanel();var steps=new StackPanel();DockPanel.SetDock(steps,Dock.Right);row.Children.Add(steps);
            foreach(var direction in new[]{1,-1})
            {
                var step=direction;var button=new RepeatButton{Content=step>0?"▲":"▼",Focusable=false,MinWidth=22,Padding=new(2,0,2,0)};
                button.SetResourceReference(ToolTipProperty,"CreatureLevel."+(step>0?"Increase":"Decrease"));button.Click+=(_,_)=>Step(index,step);
                RegisterName("CreatureLevel"+name+(step>0?"Increase":"Decrease"),button);steps.Children.Add(button);spinners.Add(button);
            }
            row.Children.Add(values[index]);container.Children.Add(row);
            values[index].PreviewKeyDown+=(_,e)=>{if(Keyboard.Modifiers==ModifierKeys.None&&e.Key is Key.Up or Key.Down){Step(index,e.Key==Key.Up?1:-1);e.Handled=true;}};
        }
        for(var i=0;i<9;i++)
        {var cell=new StackPanel();cell.Children.Add(new TextBlock{Text=(i+2).ToString(CultureInfo.InvariantCulture)+" (%)",Margin=new(4)});Numeric(cell,i,"Value"+(i+2));RegisterName("CreatureLevelValue"+(i+2),values[i]);grid.Children.Add(cell);}
        var first=new StackPanel();var firstLabel=new TextBlock{Margin=new(4)};firstLabel.SetResourceReference(TextBlock.TextProperty,"CreatureLevel.FirstPerson");first.Children.Add(firstLabel);
        Numeric(first,9,"FirstPerson");RegisterName("CreatureLevelFirstPerson",values[9]);body.Children.Add(first);
        foreach(var input in values)input.TextChanged+=(_,_)=>{if(!updating){dirty=true;InputRevision++;statusKey="Ready";UpdateStatus();}};
        statistics.SelectionChanged+=(_,_)=>SelectStatistic();
        document.Changed+=DocumentChanged;LocalizationService.LanguageChanged+=LanguageChanged;
        Loaded+=(_,_)=>{if(session is null)choose.Focus();else statistics.Focus();};
        Closed+=(_,_)=>{closed=true;InputRevision++;session?.Dispose();document.Changed-=DocumentChanged;LocalizationService.LanguageChanged-=LanguageChanged;};
        try
        {
            var configured=string.IsNullOrWhiteSpace(resources.Root)?null:Path.Combine(resources.Root,"Data","Editor","GlobalVariables.kwd");
            var initial=initialCatalogue??(configured is not null&&File.Exists(configured)?CreatureLevelModifierCatalogue.ReadFile(configured):document.CaptureCreatureLevelModifierCatalogue());
            if(initial is not null)LoadCatalogue(initial);
            else
            {
                try{session=document.CreateCreatureLevelModifierSession();RefreshStatistics();RefreshValues();}
                catch(InvalidDataException){statusKey="NeedCatalogue";}
            }
        }
        catch(Exception error)when(Known(error)){Fail(error,"CatalogueError");}
        UpdateStatus();
    }
    private static bool Known(Exception e)=>e is ArgumentException or InvalidOperationException or IOException or InvalidDataException or UnauthorizedAccessException or FormatException or OverflowException or OutOfMemoryException;
    private bool Current()=>!closed&&!stale&&ownerCurrent()&&(session is null?document.EditRevision==openedRevision:document.IsCreatureLevelModifierSessionCurrent(session));
    private bool Ready()=>Current()&&!applying&&!busy&&session is {IsClosed:false};
    public void InvalidateOwner(){stale=true;UpdateStatus();}
    private void Fail(Exception error,string key="Invalid")
    {statusKey=error is OutOfMemoryException?"Memory":Current()?key:"Stale";status.ToolTip=error.Message;UpdateStatus();}
    private void RefreshStatistics()
    {
        updating=true;
        try
        {
            statistics.ItemsSource=CreatureLevelModifierControls.Statistics.Select(s=>new Stat(uint.Parse(s.Value,CultureInfo.InvariantCulture),s.Name(LocalizationService.CurrentLanguage))).ToArray();
            statistics.SelectedItem=statistics.Items.Cast<Stat>().FirstOrDefault(s=>s.ID==(session?.Draft.SelectedStatistic??0));
        }
        finally{updating=false;}
    }
    private void RefreshValues()
    {
        if(session is null)return;updating=true;
        try{var page=session.Draft.Current;for(var i=0;i<9;i++)values[i].Text=page.Levels[i].ToString(CultureInfo.InvariantCulture);values[9].Text=page.FirstPerson.ToString(CultureInfo.InvariantCulture);dirty=false;}
        finally{updating=false;}
    }
    private CreatureLevelModifierValues ReadPage()
    {
        var parsed=new int[10];for(var i=0;i<10;i++)
            if(!int.TryParse(values[i].Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out parsed[i])||parsed[i]<CreatureLevelModifierControls.Minimum||parsed[i]>CreatureLevelModifierControls.Maximum)
            {values[i].Focus();throw new ArgumentOutOfRangeException(nameof(values));}
        return new(Array.AsReadOnly(parsed[..9]),parsed[9]);
    }
    private bool CommitPage()
    {
        if(!Ready())return false;
        try{session!.Draft.Store(ReadPage());dirty=false;return true;}
        catch(Exception error)when(Known(error)){Fail(error);return false;}
    }
    private void Step(int index,int direction)
    {
        if(!Ready())return;
        if(!int.TryParse(values[index].Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var value))
        {if(values[index].Text.Length!=0){statusKey="Invalid";UpdateStatus();return;}value=0;}
        values[index].Text=Math.Clamp((long)value+direction,CreatureLevelModifierControls.Minimum,CreatureLevelModifierControls.Maximum).ToString(CultureInfo.InvariantCulture);
    }
    private void SelectStatistic()
    {
        if(updating||closed||statistics.SelectedItem is not Stat selected)return;InputRevision++;
        if(!Ready()){RefreshStatistics();UpdateStatus();return;}
        try{session!.Draft.SelectStatistic(selected.ID,ReadPage());RefreshValues();statusKey="Ready";}
        catch(Exception error)when(Known(error)){RefreshStatistics();Fail(error);}
        UpdateStatus();
    }
    public bool LoadCatalogue(CreatureLevelModifierCatalogue candidate)
    {
        if(!Current()||busy||applying||HasPendingInputs)return false;
        try
        {
            var next=document.CreateCreatureLevelModifierSession(candidate.Records);session?.Dispose();session=next;catalogue=candidate;
            InputRevision++;statusKey="Ready";RefreshStatistics();RefreshValues();UpdateStatus();return true;
        }
        catch(Exception error)when(Known(error)){Fail(error,"CatalogueError");return false;}
    }
    private void ChooseCatalogue()
    {
        if(!Current()||busy||applying||HasPendingInputs)return;var revision=++InputRevision;busy=true;UpdateStatus();
        try
        {
            var path=CataloguePicker is { } picker?picker():PickCatalogue();
            if(path is null||!Current()||revision!=InputRevision)return;
            var candidate=CreatureLevelModifierCatalogue.ReadFile(path);
            if(!Current()||revision!=InputRevision)return;
            busy=false;LoadCatalogue(candidate);
        }
        catch(Exception error)when(Known(error)){Fail(error,"CatalogueError");}
        finally{busy=false;InputRevision++;UpdateStatus();}
    }
    private string? PickCatalogue()
    {var dialog=new OpenFileDialog{Title=LocalizationService.Get("CreatureLevel.Choose"),Filter=LocalizationService.Get("CreatureLevel.Filter"),FileName="GlobalVariables.kwd",CheckFileExists=true};return dialog.ShowDialog(this)==true?dialog.FileName:null;}
    private void ResetValues()
    {
        if(!Ready())return;
        try{session!.ResetSelectedToDefaults();InputRevision++;RefreshValues();statusKey="Ready";UpdateStatus();}
        catch(Exception error)when(Known(error)){Fail(error);}
    }
    public bool TryApply()
    {
        if(!Ready()||!commitFields()||!Ready()||!CommitPage()){UpdateStatus();return false;}
        try{applying=true;document.ApplyCreatureLevelModifiers(session!);Close();return true;}
        catch(Exception error)when(Known(error)){Fail(error);return false;}
        finally{applying=false;if(!closed)UpdateStatus();}
    }
    public bool ExportCatalogue()
    {
        if(!Ready()||catalogue?.HasCompleteVisibleTable!=true||!commitFields()||!Ready()||!CommitPage())return false;
        try
        {
            var bytes=session!.CreateSharedCatalogueCopy(catalogue);var revision=++InputRevision;busy=true;UpdateStatus();
            var path=ExportTargetPicker is { } picker?picker():PickExport();
            bool Valid()=>Current()&&!closed&&revision==InputRevision;
            if(path is null||!Valid())return false;
            if(Path.IsPathFullyQualified(catalogue.SourceName)&&Path.GetFullPath(path).Equals(Path.GetFullPath(catalogue.SourceName),StringComparison.OrdinalIgnoreCase))
            {statusKey="CopyTarget";return false;}
            (CatalogueExporter??((target,payload,valid)=>CreatureCatalogueFileOutput.SaveConfirmed(target,payload,valid)))(path,bytes,Valid);
            if(!Valid())return false;statusKey="Saved";return true;
        }
        catch(Exception error)when(Known(error)){Fail(error,"ExportError");return false;}
        finally{busy=false;InputRevision++;UpdateStatus();}
    }
    private string? PickExport()
    {var dialog=new SaveFileDialog{Title=LocalizationService.Get("CreatureLevel.Export"),Filter=LocalizationService.Get("CreatureLevel.Filter"),FileName="GlobalVariables-copy.kwd",DefaultExt=".kwd",AddExtension=true,OverwritePrompt=true};return dialog.ShowDialog(this)==true?dialog.FileName:null;}
    private void UpdateStatus()
    {
        var current=Current();status.SetResourceReference(TextBlock.TextProperty,"CreatureLevel."+(!current?"Stale":statusKey));
        source.Text=catalogue is null?LocalizationService.Get("CreatureLevel.NoCatalogue"):string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("CreatureLevel.Source"),catalogue.SourceName);
        var ready=current&&!busy&&!applying&&session is {IsClosed:false};foreach(var input in values)input.IsEnabled=ready;foreach(var spinner in spinners)spinner.IsEnabled=ready;statistics.IsEnabled=ready;
        apply.IsEnabled=ready;reset.IsEnabled=ready&&session!.DefaultValues is not null;export.IsEnabled=ready&&catalogue?.HasCompleteVisibleTable==true;
        choose.IsEnabled=current&&!busy&&!applying&&!HasPendingInputs;
    }
    private void DocumentChanged(object? sender,EventArgs args){if(!closed&&!applying){stale=true;UpdateStatus();}}
    private void LanguageChanged(object? sender,EventArgs args)
    {if(!closed){InputRevision++;RefreshStatistics();UpdateStatus();}}
}
