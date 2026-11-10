using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Hosts the complete typed attack/options/jobs panels against a private context.</summary>
public sealed class CreatureWorkspacePageWindow : Window
{
    private readonly EditorWorkspace workspace;
    private readonly Func<bool> ownerCurrent;
    private readonly string section;
    private bool closed, applying;
    public long InputRevision => workspace.CreatureHostedInputRevision;
    public object? FindControl(string name) => workspace.FindName(name);
    public IReadOnlyList<CreatureGlobalsJobWarning> JobWarnings => workspace.HostedJobWarnings;
    public CreatureWorkspacePageWindow(NativeMapDocument document,string section,int index,Func<bool> ownerCurrent)
    {
        this.section=section;this.ownerCurrent=ownerCurrent;
        workspace=new(){CreatureHostedOwnerCurrent=()=>!closed&&!applying&&ownerCurrent(),Document=document};
        var panel=workspace.DetachCreaturePage(section,index);
        Content=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=panel};
        Width=740;Height=660;MinWidth=540;MinHeight=420;
        LocalizationService.LanguageChanged+=LanguageChanged;
        Closed+=(_,_)=>{closed=true;LocalizationService.LanguageChanged-=LanguageChanged;workspace.Document=null;};
    }
    private void LanguageChanged(object? sender,EventArgs args)
    {if(!closed)workspace.RefreshHostedCreaturePage(section);}
    public bool ApplyPage()
    {
        if(closed||applying||!ownerCurrent())return false;
        applying=true;
        try{return workspace.ApplyHostedCreaturePage(section);}
        finally{applying=false;}
    }
}
