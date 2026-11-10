using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Original GLOBALCTX2: nine pages, shared creature selection, independent spells and one private transaction.</summary>
public sealed class CreatureGlobalsWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly Func<bool> ownerCurrent,commitFields;
    private readonly CreatureJobsWarningPolicy jobsWarningPolicy;
    private readonly CreatureJobsPageContext jobsPageContext=new();
    private CreatureJobsWarningWindow? jobsWarningWindow;
    private CreatureGlobalsSession session;
    private readonly TabControl pages=new();
    private readonly ComboBox rows=new(){ItemTemplate=OriginalObjectImages.NamedTemplate(),Margin=new(4,8,4,8)};
    private readonly ContentControl editorHost=new();
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly Button apply=new(),resetCurrent=new(),resetAll=new(),resetJobWarnings=new();
    private Window? rowEditor,child;
    private string section="Attributes",statusKey="Ready";
    private int creatureIndex,spellIndex;
    private bool closed,stale,updating,transitioning,applying,colourOpen,warningOpen;
    private long contextRevision;
    private sealed record RowChoice(int Index,string Family,string Label)
    {public System.Windows.Media.Imaging.BitmapSource? Icon=>OriginalDefinitionImages.For(Family,Index);}
    public Window? ActiveRowEditor=>rowEditor;
    public string SelectedSection=>section;
    public int SelectedIndex=>section=="CrSpells"?spellIndex:creatureIndex;
    public Action<Window>? OptionsPresenter{get;set;}
    public Func<uint,uint?>? ColourSelector{get;set;}
    public Func<IReadOnlyList<CreatureGlobalsJobWarning>,bool>? JobWarningsConfirm{get;set;}
    public bool JobWarningsSuppressed=>jobsWarningPolicy.Suppressed;
    public CreatureGlobalsWindow(NativeMapDocument document,Func<bool> ownerCurrent,Func<bool> commitFields,CreatureJobsWarningPolicy? jobsWarningPolicy=null)
    {
        this.jobsWarningPolicy=jobsWarningPolicy??new();
        this.document=document;this.ownerCurrent=ownerCurrent;this.commitFields=commitFields;session=document.CreateCreatureGlobalsSession();
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"CreatureGlobals.Title");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=800;Height=800;MinWidth=580;MinHeight=540;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(12)};Content=panel;
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4,0,4,8)};hint.SetResourceReference(TextBlock.TextProperty,"CreatureGlobals.Hint");DockPanel.SetDock(hint,Dock.Top);panel.Children.Add(hint);
        var navigation=new StackPanel();DockPanel.SetDock(navigation,Dock.Top);panel.Children.Add(navigation);
        foreach(var page in CreatureGlobalsSession.Sections)
        {var tab=new TabItem{Tag=page.Name};tab.SetResourceReference(HeaderedContentControl.HeaderProperty,"Editor.Definitions."+page.Name);pages.Items.Add(tab);}
        RegisterName("CreatureGlobalsPages",pages);RegisterName("CreatureGlobalsRows",rows);navigation.Children.Add(pages);navigation.Children.Add(rows);
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);footer.Children.Add(status);RegisterName("CreatureGlobalsStatus",status);
        var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};
        void Button(Button button,string key,string name,RoutedEventHandler click)
        {button.SetResourceReference(ContentControl.ContentProperty,"GlobalVariables."+key);button.Click+=click;RegisterName(name,button);buttons.Children.Add(button);}
        Button(resetCurrent,"ResetCurrent","CreatureGlobalsResetCurrent",(_,_)=>Reset(false));Button(resetAll,"ResetAll","CreatureGlobalsResetAll",(_,_)=>Reset(true));
        Button(apply,"Apply","CreatureGlobalsApply",(_,_)=>ApplyCreatures());var cancel=new Button{IsCancel=true};Button(cancel,"Cancel","CreatureGlobalsCancel",(_,_)=>Close());footer.Children.Add(buttons);
        resetJobWarnings.SetResourceReference(ContentControl.ContentProperty,"CreatureGlobals.JobsResetWarnings");
        resetJobWarnings.Click+=(_,_)=>ResetJobWarnings();RegisterName("CreatureGlobalsResetJobWarnings",resetJobWarnings);buttons.Children.Add(resetJobWarnings);
        panel.Children.Add(editorHost);RegisterName("CreatureGlobalsEditor",editorHost);
        pages.SelectionChanged+=(_,_)=>{if(!updating&&pages.SelectedItem is TabItem tab&&!Select((string)tab.Tag,(string)tab.Tag=="CrSpells"?spellIndex:creatureIndex))RefreshSelectors();};
        rows.SelectionChanged+=(_,_)=>{if(!updating&&rows.SelectedItem is RowChoice choice&&!Select(section,choice.Index))RefreshSelectors();};
        document.Changed+=DocumentChanged;LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;contextRevision++;jobsWarningWindow?.Close();jobsWarningWindow=null;child?.Close();child=null;CloseRow();jobsPageContext.Dispose();session.Dispose();document.Changed-=DocumentChanged;LocalizationService.LanguageChanged-=LanguageChanged;};
        RefreshSelectors();OpenRow();UpdateStatus();
    }
    private bool Current=>!closed&&!stale&&ownerCurrent()&&document.IsCreatureGlobalsSessionCurrent(session);
    private bool Busy=>transitioning||applying||child is not null||colourOpen||warningOpen;
    private string Family=>CreatureGlobalsSession.Sections.Single(page=>page.Name==section).Family;
    private void RefreshSelectors()
    {
        updating=true;
        try
        {
            pages.SelectedItem=pages.Items.Cast<TabItem>().Single(tab=>(string)tab.Tag==section);
            rows.ItemsSource=session.WorkingDocument.GlobalDefinitionRows().Where(row=>row.Family==Family).Select(row=>new RowChoice(row.Index,Family,
                ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue(Family)[row.Index],row.ID.ToString(CultureInfo.InvariantCulture)).Name(LocalizationService.CurrentLanguage))).ToArray();
            rows.SelectedItem=rows.Items.Cast<RowChoice>().Single(row=>row.Index==SelectedIndex);
        }
        finally{updating=false;}
    }
    private void CloseRow(){editorHost.Content=null;rowEditor?.Close();rowEditor=null;contextRevision++;}
    private void OpenRow()
    {
        CloseRow();var working=session.WorkingDocument;var row=working.GlobalDefinitionRows().Single(row=>row.Family==Family&&row.Index==SelectedIndex);
        rowEditor=section switch
        {
            "CrSpells"=>new CreatureSpellWindow(working,row,()=>Current,()=>true){OptionsPresenter=window=>PresentChild(window)},
            "Attributes" or "Parameters" or "Flags" or "Light"=>new CreaturePageWindow(working,row,section,()=>Current,()=>true){ColourSelector=ChooseColour},
            _=>new CreatureWorkspacePageWindow(working,section,SelectedIndex,()=>Current)
        };
        if(section=="Jobs"&&rowEditor is CreatureWorkspacePageWindow jobs)jobsPageContext.Capture(SelectedIndex,jobs.JobWarnings);
        var prefix=section=="CrSpells"?"CreatureSpellProperties":"CreaturePages";
        foreach(var suffix in new[]{"Apply","Cancel"})if(rowEditor.FindName(prefix+suffix) is Button button)button.Visibility=Visibility.Collapsed;
        if(rowEditor.FindName(prefix+"Status") is FrameworkElement rowStatus)rowStatus.Visibility=Visibility.Collapsed;
        var content=rowEditor.Content;rowEditor.Content=null;editorHost.Content=content;
    }
    private long RowRevision=>rowEditor switch{CreaturePageWindow page=>page.InputRevision,CreatureSpellWindow spell=>spell.InputRevision,CreatureWorkspacePageWindow workspace=>workspace.InputRevision,_=>-1};
    private uint? ChooseColour(uint initial)
    {
        if(!Current||Busy)return null;colourOpen=true;var revision=contextRevision;UpdateStatus();
        try{var result=(ColourSelector??(value=>NativeTerrainColourDialog.Choose(this,value)))(initial);return Current&&contextRevision==revision?result:null;}
        finally{colourOpen=false;UpdateStatus();}
    }
    private void PresentChild(Window window)
    {
        if(!Current||Busy){window.Close();return;}child=window;
        window.Closed+=(_,_)=>{if(ReferenceEquals(child,window)){child=null;UpdateStatus();}};UpdateStatus();
        if(OptionsPresenter is { } present)present(window);else{window.Owner=this;window.ShowDialog();}
    }
    private bool ConfirmJobs(IReadOnlyList<CreatureGlobalsJobWarning> warnings)
    {
        if(warnings.Count==0)return true;
        var plan=document.PrepareCreatureGlobals(session);var revision=RowRevision;var context=contextRevision;var editor=rowEditor;var jobsRevision=jobsPageContext.Revision;
        warningOpen=true;UpdateStatus();
        try
        {
            bool OwnerCurrent()=>Current&&context==contextRevision&&ReferenceEquals(editor,rowEditor)&&revision==RowRevision&&jobsRevision==jobsPageContext.Revision&&document.IsCreatureGlobalsPlanCurrent(plan);
            using var request=jobsWarningPolicy.Begin(warnings,OwnerCurrent);
            if(request is null)return OwnerCurrent();
            if(!request.RequiresPrompt)request.Continue();
            else if(JobWarningsConfirm is { } confirm)
            {if(confirm(warnings)&&request.IsCurrent)request.Continue();}
            else
            {
                var window=new CreatureJobsWarningWindow(document,request){Owner=this};jobsWarningWindow=window;
                try{window.ShowDialog();}finally{if(ReferenceEquals(jobsWarningWindow,window))jobsWarningWindow=null;}
            }
            return request.IsAccepted&&OwnerCurrent();
        }
        finally{warningOpen=false;UpdateStatus();}
    }
    private bool SaveCurrent()
    {
        if(!session.WorkingDocument.GlobalDefinitionRows().Single(row=>row.Family==Family&&row.Index==SelectedIndex).Editable)return true;
        if(rowEditor is CreatureWorkspacePageWindow jobs&&section=="Jobs")
        {jobsPageContext.Capture(SelectedIndex,jobs.JobWarnings);if(!ConfirmJobs(jobsPageContext.Warnings))return false;}
        return rowEditor switch{CreaturePageWindow page=>page.ApplyPage(),CreatureSpellWindow spell=>spell.ApplySpell(),CreatureWorkspacePageWindow workspace=>workspace.ApplyPage(),_=>false};
    }
    public bool Select(string selectedSection,int index)
    {
        var page=CreatureGlobalsSession.Sections.SingleOrDefault(page=>page.Name==selectedSection);
        if(!Current||Busy||page is null||!session.WorkingDocument.GlobalDefinitionRows().Any(row=>row.Family==page.Family&&row.Index==index))return false;
        if(section==selectedSection&&SelectedIndex==index)return true;transitioning=true;UpdateStatus();
        try
        {
            if(!SaveCurrent()||!Current){statusKey="Invalid";return false;}
            section=selectedSection;if(section=="CrSpells")spellIndex=index;else creatureIndex=index;
            RefreshSelectors();OpenRow();statusKey="Draft";return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException){statusKey="Invalid";return false;}
        finally{transitioning=false;UpdateStatus();}
    }
    public bool Reset(bool all)
    {
        if(!Current||Busy)return false;transitioning=true;UpdateStatus();
        try{document.ResetCreatureGlobals(session,section,all?null:SelectedIndex);OpenRow();statusKey="Draft";return true;}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException){statusKey="Invalid";return false;}
        finally{transitioning=false;UpdateStatus();}
    }
    public bool ApplyCreatures()
    {
        if(!Current||Busy)return false;applying=true;UpdateStatus();
        try
        {
            if(!SaveCurrent()||!Current){statusKey="Invalid";return false;}
            var plan=document.PrepareCreatureGlobals(session);
            // The source Jobs PSN_APPLY checks its retained page buffers even
            // when another page is active. A changed-row plan is not that buffer.
            if(jobsPageContext.Initialized&&!ConfirmJobs(jobsPageContext.Warnings)){statusKey="Invalid";return false;}
            var revision=RowRevision;var context=contextRevision;var editor=rowEditor;
            if(!commitFields()||!Current||context!=contextRevision||!ReferenceEquals(editor,rowEditor)||revision!=RowRevision||!document.IsCreatureGlobalsPlanCurrent(plan))
            {statusKey="Stale";return false;}
            document.ApplyCreatureGlobals(plan,true);CloseRow();session=document.CreateCreatureGlobalsSession();RefreshSelectors();OpenRow();statusKey="Applied";return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException){statusKey="Invalid";return false;}
        finally{applying=false;UpdateStatus();}
    }
    private void UpdateStatus()
    {
        status.Text=LocalizationService.Get("GlobalVariables."+(!Current?"Stale":child is not null||colourOpen||warningOpen?"Choosing":statusKey));
        apply.IsEnabled=resetCurrent.IsEnabled=resetAll.IsEnabled=Current&&!Busy;pages.IsEnabled=rows.IsEnabled=editorHost.IsEnabled=Current&&!Busy;
        resetJobWarnings.IsEnabled=Current&&!Busy&&jobsWarningPolicy.Suppressed;
    }
    public bool ResetJobWarnings()
    {if(!Current||Busy)return false;jobsWarningPolicy.ResetSuppression();UpdateStatus();return true;}
    private void DocumentChanged(object? sender,EventArgs args){if(!closed&&!applying){stale=true;jobsWarningWindow?.Close();UpdateStatus();}}
    private void LanguageChanged(object? sender,EventArgs args){if(!closed){contextRevision++;RefreshSelectors();UpdateStatus();}}
}
