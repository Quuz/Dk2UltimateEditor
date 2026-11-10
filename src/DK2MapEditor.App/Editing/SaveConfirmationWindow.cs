using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.App.Editing;

/// <summary>Read-only, language-aware consent for these exact Save bytes.</summary>
public sealed class SaveConfirmationWindow : Window
{
    private readonly EditorSavePlan plan;
    private readonly IReadOnlyList<MapDiagnostic> compatibility;
    private readonly Func<bool> ownerCurrent;
    private readonly TextBlock summary=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly TextBlock archive=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4,8,4,8)};
    private readonly StackPanel issues=new();
    private readonly Button accept=new();
    private bool closed;
    public bool Accepted { get; private set; }
    public SaveConfirmationWindow(EditorSavePlan plan,Func<bool> ownerCurrent,IReadOnlyList<MapDiagnostic>? compatibility=null)
    {
        ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(ownerCurrent);this.plan=plan;this.ownerCurrent=ownerCurrent;
        this.compatibility=compatibility?.ToArray()??[];
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"Save.Confirm.Title");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=620;Height=480;MinWidth=520;MinHeight=380;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel {Margin=new(16)};Content=panel;
        var buttons=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(buttons,Dock.Bottom);panel.Children.Add(buttons);
        accept.SetResourceReference(ContentControl.ContentProperty,"Save.Confirm.Accept");accept.Click+=(_,_)=>{if(Accept())Close();};
        RegisterName("SaveAccept",accept);buttons.Children.Add(accept);
        var cancel=new Button {IsCancel=true,IsDefault=true};cancel.SetResourceReference(ContentControl.ContentProperty,"Editor.Cancel");cancel.Click+=(_,_)=>Close();RegisterName("SaveCancel",cancel);buttons.Children.Add(cancel);
        var top=new StackPanel();top.Children.Add(summary);top.Children.Add(archive);RegisterName("SaveSummary",summary);RegisterName("SaveArchiveNotice",archive);DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
        var scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=issues};panel.Children.Add(scroll);RegisterName("SaveIssues",issues);
        LocalizationService.LanguageChanged+=LanguageChanged;Closed+=(_,_)=>{closed=true;LocalizationService.LanguageChanged-=LanguageChanged;};Refresh();
    }
    private void LanguageChanged(object? sender,EventArgs args)=>Refresh();
    private void Refresh()
    {
        summary.Text=LocalizationService.Get(plan.HasBlockingIssues?"Save.Confirm.Blocked":"Save.Confirm.Summary")+"\n"+plan.Path;
        archive.Text=LocalizationService.Get("Save.Confirm.Archive");archive.Visibility=plan.RetainsNativeSource?Visibility.Visible:Visibility.Collapsed;
        issues.Children.Clear();
        foreach(var loss in plan.TextLosses)
        {
            var text=new TextBlock {TextWrapping=TextWrapping.Wrap,Margin=new(4)};
            text.Text=LocalizationService.Get("TextMap.Loss."+loss.Code)+(loss.Count>0?": "+loss.Count.ToString(CultureInfo.CurrentCulture):"");
            text.SetResourceReference(TextBlock.ForegroundProperty,"Brush.Text");issues.Children.Add(text);
        }
        foreach(var issue in plan.ConversionIssues)
        {
            var text=new TextBlock {TextWrapping=TextWrapping.Wrap,Margin=new(4)};
            text.Text=LocalizationService.Get("Save.Issue."+issue.Code,LocalizationService.Get("Save.Issue.Generic"));
            text.SetResourceReference(TextBlock.ForegroundProperty,issue.Severity==ProjectConversionSeverity.Error?"Brush.Error":issue.Severity==ProjectConversionSeverity.Loss?"Brush.Warning":"Brush.Text");issues.Children.Add(text);
        }
        foreach(var issue in compatibility)
        {
            var text=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4),Text=LocalizationService.Get("Diagnostic."+issue.Code,issue.Message),ToolTip=issue.Message};
            text.SetResourceReference(TextBlock.ForegroundProperty,issue.Severity==DiagnosticSeverity.Error?"Brush.Error":issue.Severity==DiagnosticSeverity.Warning?"Brush.Warning":"Brush.Text");issues.Children.Add(text);
        }
        accept.IsEnabled=!closed&&!plan.HasBlockingIssues&&ownerCurrent();
    }
    public bool Accept()
    {
        if(closed||plan.HasBlockingIssues||!ownerCurrent()){accept.IsEnabled=false;return false;}
        Accepted=true;return true;
    }
}

public sealed class AutoSaveSettingsWindow : Window
{
    private readonly Action<uint> apply;
    private readonly TextBox minutes=new(){MinWidth=110,Margin=new(4)};
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private bool closed;
    public AutoSaveSettingsWindow(uint initial,Action<uint> apply)
    {
        this.apply=apply;NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"Save.AutoSettings");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=460;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel {Margin=new(16)};Content=panel;
        var hint=new TextBlock {TextWrapping=TextWrapping.Wrap,Margin=new(4)};hint.SetResourceReference(TextBlock.TextProperty,"Save.AutoSettings.Hint");panel.Children.Add(hint);
        minutes.Text=initial.ToString(CultureInfo.InvariantCulture);RegisterName("AutoSaveMinutes",minutes);panel.Children.Add(minutes);RegisterName("AutoSaveStatus",status);panel.Children.Add(status);
        var buttons=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(buttons);
        var ok=new Button {IsDefault=true};ok.SetResourceReference(ContentControl.ContentProperty,"Save.AutoSettings.Apply");ok.Click+=(_,_)=>{if(Apply())Close();};RegisterName("AutoSaveApply",ok);buttons.Children.Add(ok);
        var cancel=new Button {IsCancel=true};cancel.SetResourceReference(ContentControl.ContentProperty,"Editor.Cancel");cancel.Click+=(_,_)=>Close();buttons.Children.Add(cancel);Closed+=(_,_)=>closed=true;
    }
    public bool Apply()
    {
        if(closed)return false;
        if(!uint.TryParse(minutes.Text,NumberStyles.None,CultureInfo.InvariantCulture,out var value))
        {status.SetResourceReference(TextBlock.TextProperty,"Save.AutoSettings.Invalid");return false;}
        try { apply(value);return true; }
        catch(InvalidOperationException){status.SetResourceReference(TextBlock.TextProperty,"Save.ContextExpired");return false;}
    }
}

public sealed class RecoverySelectionWindow : Window
{
    private readonly ListBox list=new(){MinHeight=130,MaxHeight=240,Margin=new(4)};
    private bool closed;
    public string? SelectedPath { get; private set; }
    private sealed record Choice(EditorRecoveryCandidate Candidate)
    { public override string ToString()=>Candidate.SavedUtc.ToLocalTime().ToString("g",CultureInfo.CurrentCulture)+"\n"+Candidate.Path; }
    public RecoverySelectionWindow(IReadOnlyList<EditorRecoveryCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"Save.Recover");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=650;SizeToContent=SizeToContent.Height;MinWidth=520;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel {Margin=new(16)};Content=panel;
        var hint=new TextBlock {TextWrapping=TextWrapping.Wrap,Margin=new(4)};hint.SetResourceReference(TextBlock.TextProperty,"Save.Recovery.Hint");panel.Children.Add(hint);
        foreach(var candidate in candidates)list.Items.Add(new Choice(candidate));if(list.Items.Count!=0)list.SelectedIndex=0;RegisterName("RecoveryCandidates",list);panel.Children.Add(list);
        var buttons=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(buttons);
        var recover=new Button {IsDefault=true};recover.SetResourceReference(ContentControl.ContentProperty,"Save.Recovery.Accept");recover.Click+=(_,_)=>{if(Accept())Close();};RegisterName("RecoveryAccept",recover);buttons.Children.Add(recover);
        var cancel=new Button {IsCancel=true};cancel.SetResourceReference(ContentControl.ContentProperty,"Editor.Cancel");cancel.Click+=(_,_)=>Close();buttons.Children.Add(cancel);
        LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;LocalizationService.LanguageChanged-=LanguageChanged;};
    }
    private void LanguageChanged(object? sender,EventArgs args)
    {
        var index=list.SelectedIndex;var choices=list.Items.Cast<Choice>().ToArray();list.Items.Clear();
        foreach(var choice in choices)list.Items.Add(choice);list.SelectedIndex=index;
    }
    public bool Accept()
    {
        if(closed||list.SelectedItem is not Choice choice)return false;
        SelectedPath=choice.Candidate.Path;return true;
    }
}
