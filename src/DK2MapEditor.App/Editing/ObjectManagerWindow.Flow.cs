using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class ObjectManagerWindow
{
    private bool managerFlowBusy,updatingFlow;
    private readonly List<(CheckBox Box,ObjectBatchSession Target,ObjectBatchFlags Flag)> flowPreferences=[];
    private Button? templateButton;
    public Action<ObjectManagerOptionsWindow>? OptionsPresenter {get;set;}
    public Action<ObjectBatchWindow>? CommandWindowPresenter {get;set;}
    private void InitializeFlowPreferences(Panel top,Panel commands)
    {
        var groups=new WrapPanel();top.Children.Add(groups);
        foreach(var (target,key,prefix) in new[]{(session.Addition,"ManagerOptions.Add","ManagerAdd"),(session.Modification.Template,"ManagerOptions.Modify","ManagerModify")})
        {
            var group=new GroupBox{Margin=new(2)};group.SetResourceReference(HeaderedContentControl.HeaderProperty,key);groups.Children.Add(group);
            var panel=new WrapPanel();group.Content=panel;
            foreach(var flag in new[]{ObjectBatchFlags.AskOptions,ObjectBatchFlags.Preview})
            {
                var box=Named(new CheckBox{Margin=new(5,2,5,2)},prefix+flag);box.SetResourceReference(ContentControl.ContentProperty,"Batch.Flag."+flag);panel.Children.Add(box);
                flowPreferences.Add((box,target,flag));
                void Changed(object sender,RoutedEventArgs args)
                {
                    if(updatingFlow)return;
                    if(closed||managerFlowBusy||navigating||optionsWindow is not null||!ownerCurrent()){RefreshFlowPreferences();return;}
                    target.Flags=box.IsChecked==true?target.Flags|flag:target.Flags&~flag;
                }
                box.Checked+=Changed;box.Unchecked+=Changed;
            }
        }
        templateButton=Button("ManagerTemplate","Manager.Template",()=>
        {
            var window=CreateTemplateWindow();if(window is null)return;
            PresentCommandWindow(window);
        });commands.Children.Add(templateButton);RefreshFlowPreferences();
    }
    public ObjectBatchWindow? CreateTemplateWindow()
    {
        var window=CreateAdditionWindow();if(window is null)return null;
        window.ConfigureManagerTemplateEditor();return window;
    }
    private void RefreshFlowPreferences()
    {
        updatingFlow=true;try{foreach(var (box,target,flag) in flowPreferences)box.IsChecked=target.Flags.HasFlag(flag);}finally{updatingFlow=false;}
    }
    private void UpdateFlowPreferences()
    {
        var available=!closed&&!managerFlowBusy&&!navigating&&!requestPending&&optionsWindow is null&&ownerCurrent();
        foreach(var (box,_,_) in flowPreferences)box.IsEnabled=available;
        if(templateButton is {} button)button.IsEnabled=available;
    }
    private bool PresentCommandOptions(PlacedThingKind kind,bool modifying,Func<bool>? extraCurrent=null)
    {
        var window=NewOptionsWindow(kind,modifying,extraCurrent??(()=>true));
        try
        {
            if(OptionsPresenter is {} present)present(window);else{window.Owner=this;window.ShowDialog();}
            return window.WasApplied&&!closed&&ownerCurrent();
        }
        finally{window.Cancel();}
    }
    private void PresentCommandWindow(ObjectBatchWindow window)
    {if(CommandWindowPresenter is {} present)present(window);else{window.Owner=this;window.ShowDialog();}}
    public bool RequestAddition()=>ExecuteManagerFlow(false);
    public bool RequestModification()=>ExecuteManagerFlow(true);
    private bool ExecuteManagerFlow(bool modifying)
    {
        if(closed||managerFlowBusy||navigating||requestPending||optionsWindow is not null||!ownerCurrent()||!canApply()||closed)return false;
        managerFlowBusy=true;UpdateApply();ObjectBatchWindow? window=null;
        try
        {
            if(!modifying)
            {
                TryOptionsKind(false,out var kind);
                if(ObjectManagerFlowRules.AskOptions(false,0,session.Addition.Flags)&&!PresentCommandOptions(kind,false))return false;
                window=CreateAdditionWindow(true);if(window is null)return false;
                window.UseManagerPlacement(!session.SelectionOnly,selection());
            }
            else
            {
                window=CreateModificationWindow(true);if(window is null)return false;
                if(!window.ManagerMultiple)
                {
                    window.ConfigureManagerSingleEditor(session.Modification.Template.Flags,session.Modification.Template.Policy);
                    PresentCommandWindow(window);return ReportManagerFlow(window);
                }
            }
            if(!window.BuildPreview())return ReportManagerFlow(window);
            if(window.RequiresManagerPreview){window.ConfigureManagerPreview();PresentCommandWindow(window);}
            else window.ApplyManagerDefaults();
            return ReportManagerFlow(window);
        }
        catch(Exception error)when(IsThingRequestFailure(error))
        {statusKey="Manager.CommandFailed";status.ToolTip=error.Message;RefreshStatus();return false;}
        finally
        {
            window?.Cancel();managerFlowBusy=false;RefreshFlowPreferences();UpdateApply();
        }
    }
    private bool ReportManagerFlow(ObjectBatchWindow window)
    {
        if(window.ManagerCommandApplied)
        {appliedCount=window.ManagerAppliedCount;statusKey=window.ManagerCommandFailed?"Batch.CommittedFailure":"Manager.CommandApplied";status.ToolTip=window.ManagerFailure;}
        else if(window.ManagerCommandFailed){statusKey="Manager.CommandFailed";status.ToolTip=window.ManagerFailure;}
        else {RefreshStatus();return false;}
        RefreshStatus();return window.ManagerCommandApplied;
    }
}
