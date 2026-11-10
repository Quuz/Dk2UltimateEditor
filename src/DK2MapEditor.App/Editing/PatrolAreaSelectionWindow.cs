using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class PatrolAreaSelectionWindow : Window
{
    private sealed record CandidateView(EditorPatrolAreaCandidate Candidate,string Label)
    { public System.Windows.Media.Imaging.BitmapSource? Icon => OriginalEditorListImages.For(EditorListImageResources.PatrolArea); }
    private readonly EditorPatrolAreaSelectionSession session;
    private readonly NativeMapDocument document;
    private readonly Func<bool> ownerCurrent;
    private readonly ListBox candidates=new(){ItemTemplate=OriginalObjectImages.NamedTemplate()};
    private readonly TextBox pause=new(){MinWidth=70,Margin=new(8,0,0,0)};
    private readonly Slider slider=new(){Minimum=0,Maximum=250,SmallChange=1,LargeChange=10,TickFrequency=10,TickPlacement=TickPlacement.BottomRight,Margin=new(0,8,0,8)};
    private readonly Button accept=new(){IsDefault=true};
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(0,8,0,8)};
    private bool syncing,closed,closing,applying,stale;
    public EditorPatrolManagerRoute? Result {get;private set;}
    public PatrolAreaSelectionWindow(NativeMapDocument document,EditorPatrolAreaSelectionSession session,Func<bool> ownerCurrent)
    {
        this.document=document;this.session=session;this.ownerCurrent=ownerCurrent;
        NameScope.SetNameScope(this,new NameScope());
        SetResourceReference(TitleProperty,session.Operation==EditorPatrolSelectionOperation.Append?"Editor.PatrolSelectAppend":"Editor.PatrolSelectInsert");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=660;Height=530;MinWidth=480;MinHeight=380;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(14)};Content=panel;
        var hint=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,10)};Key(hint,TextBlock.TextProperty,"Editor.PatrolSelectHint");DockPanel.SetDock(hint,Dock.Top);panel.Children.Add(hint);
        var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);panel.Children.Add(bottom);
        var row=new DockPanel();bottom.Children.Add(row);
        var label=new TextBlock{VerticalAlignment=VerticalAlignment.Center};Key(label,TextBlock.TextProperty,"Editor.PatrolPause");row.Children.Add(label);
        DockPanel.SetDock(pause,Dock.Right);row.Children.Add(pause);bottom.Children.Add(slider);bottom.Children.Add(status);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};bottom.Children.Add(buttons);
        Key(accept,ContentControl.ContentProperty,"Editor.PatrolSelectApply");accept.Click+=(_,_)=>TryAccept();buttons.Children.Add(accept);
        var cancel=new Button{IsCancel=true};Key(cancel,ContentControl.ContentProperty,"Editor.Cancel");cancel.Click+=(_,_)=>Close();buttons.Children.Add(cancel);
        panel.Children.Add(candidates);
        RegisterName("PatrolSelectionCandidates",candidates);RegisterName("PatrolSelectionPause",pause);
        RegisterName("PatrolSelectionSlider",slider);RegisterName("PatrolSelectionAccept",accept);
        candidates.SelectionChanged+=(_,_)=>
        {
            if(syncing||!Current)return;
            session.Select((candidates.SelectedItem as CandidateView)?.Candidate);SyncPause();RefreshAcceptance();
        };
        pause.TextChanged+=(_,_)=>{if(syncing||!Current)return;session.SetPauseText(pause.Text);SyncPause();RefreshAcceptance();};
        slider.ValueChanged+=(_,_)=>{if(syncing||!Current)return;session.SetPause((int)Math.Round(slider.Value,MidpointRounding.AwayFromZero));SyncPause();RefreshAcceptance();};
        document.Changed+=DocumentChanged;LocalizationService.LanguageChanged+=Translated;
        Closing+=(_,_)=>closing=true;
        Closed+=(_,_)=>{closed=true;session.Dispose();document.Changed-=DocumentChanged;LocalizationService.LanguageChanged-=Translated;};
        RebuildCandidates();SyncPause();RefreshAcceptance();
    }
    private static void Key(FrameworkElement element,DependencyProperty property,string key)=>element.SetResourceReference(property,key);
    private bool Current=>!closed&&!closing&&!applying&&!stale&&ownerCurrent()&&session.IsCurrent;
    private void RebuildCandidates()
    {
        syncing=true;
        try
        {
            var views=session.Candidates.Select(candidate=>new CandidateView(candidate,$"#{candidate.ID} · {candidate.WaitDelay} · {candidate.Name}"
                +(candidate.Information.Length==0?"":" · "+candidate.Information)
                +(candidate.NextID==0?"":" · "+LocalizationService.Get("Editor.PatrolLinked")+" → #"+candidate.NextID)
                +(candidate.Selectable?"":" · "+LocalizationService.Get("Editor.PatrolAmbiguous")))).ToArray();
            candidates.ItemsSource=views;
            candidates.SelectedItem=views.FirstOrDefault(view=>ReferenceEquals(view.Candidate,session.Selected));
        }
        finally{syncing=false;}
    }
    private void SyncPause()
    {
        syncing=true;try{pause.Text=session.Pause.ToString(CultureInfo.InvariantCulture);slider.Value=session.Pause;}finally{syncing=false;}
    }
    private void RefreshAcceptance()
    {
        var current=Current;accept.IsEnabled=current&&session.CanAccept;
        candidates.IsEnabled=pause.IsEnabled=slider.IsEnabled=current;
        if(!current&&!closed&&!applying)Key(status,TextBlock.TextProperty,"Editor.PatrolSelectionStale");
    }
    private void Translated(object? sender,EventArgs args){if(closed)return;RebuildCandidates();RefreshAcceptance();}
    private void DocumentChanged(object? sender,EventArgs args){if(closed||applying)return;stale=true;RefreshAcceptance();}
    public bool TryAccept()
    {
        if(!Current||!session.CanAccept){RefreshAcceptance();return false;}
        try
        {
            applying=true;Result=session.Accept();Close();return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException)
        {Key(status,TextBlock.TextProperty,session.IsCurrent?"Editor.PatrolFailed":"Editor.PatrolSelectionStale");status.ToolTip=error.Message;return false;}
        finally{applying=false;if(!closed)RefreshAcceptance();}
    }
}
