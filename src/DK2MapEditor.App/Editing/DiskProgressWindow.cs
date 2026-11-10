using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Short-lived source disk indicator. It reports observed file/section
/// work; Save remains on its original thread with its original atomic writer.</summary>
public sealed class DiskProgressWindow:Window
{
    private readonly bool saving;
    private readonly TextBlock target=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)},phase=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)},count=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private readonly ProgressBar progress=new(){Height=20,Margin=new(4),IsIndeterminate=true};
    private EditorFileProgress? state;
    private bool closing,closed;
    public DiskProgressWindow(bool saving,string path)
    {
        this.saving=saving;Width=620;MinWidth=360;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;
        ShowInTaskbar=false;WindowStartupLocation=WindowStartupLocation.CenterOwner;NameScope.SetNameScope(this,new NameScope());
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        var panel=new StackPanel{Margin=new(14)};Content=panel;target.Text=path;
        panel.Children.Add(target);panel.Children.Add(phase);panel.Children.Add(progress);panel.Children.Add(count);
        RegisterName("DiskTarget",target);RegisterName("DiskPhase",phase);RegisterName("DiskCount",count);RegisterName("DiskProgress",progress);
        Closing+=(_,args)=>{if(!closing)args.Cancel=true;};Closed+=(_,_)=>{closed=true;LocalizationService.LanguageChanged-=LanguageChanged;};
        LocalizationService.LanguageChanged+=LanguageChanged;Refresh();
    }
    public void Report(EditorFileProgress value){if(closed)return;state=value;Refresh();}
    private void LanguageChanged(object? sender,EventArgs args)=>Refresh();
    private void Refresh()
    {
        SetResourceReference(TitleProperty,saving?"FileProgress.Saving":"FileProgress.Loading");
        if(state is null)return;
        phase.SetResourceReference(TextBlock.TextProperty,"FileProgress."+state.Phase);
        progress.IsIndeterminate=state.Total<=0&&state.Phase!=EditorFilePhase.Done;
        progress.Minimum=0;progress.Maximum=state.Total>0?state.Total:1;
        progress.Value=state.Phase==EditorFilePhase.Done?progress.Maximum:Math.Clamp(state.Completed,0,progress.Maximum);
        count.Text=state.Total>0?string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("FileProgress.Count"),state.Completed,state.Total)
            :state.Completed>0?string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("FileProgress.Bytes"),state.Completed):"";
    }
    public void CloseForOperation(){closing=true;Close();}
}
