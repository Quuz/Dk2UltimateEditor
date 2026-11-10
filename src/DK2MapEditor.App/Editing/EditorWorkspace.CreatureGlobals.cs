using System.Windows;
using DK2MapEditor.Core.Editing;
namespace DK2MapEditor.App.Editing;
public partial class EditorWorkspace
{
    private CreatureGlobalsWindow? creatureGlobalsWindow;
    private readonly CreatureJobsWarningPolicy creatureJobsWarningPolicy=new();
    private long creatureGlobalsContext;
    public Action<CreatureGlobalsWindow>? CreatureGlobalsPresenter{get;set;}
    private void CloseCreatureGlobals(){creatureGlobalsContext++;creatureGlobalsWindow?.Close();creatureGlobalsWindow=null;}
    public CreatureGlobalsWindow? CreateCreatureGlobalsWindow()
    {
        if(scenarioNamesOwnerClosed||document is null||!CommitFields()||!document.GlobalDefinitionRows().Any(row=>CreatureGlobalsSession.Families.Contains(row.Family)&&row.Editable))return null;
        var source=document;var context=creatureGlobalsContext;
        return new(source,()=>!scenarioNamesOwnerClosed&&context==creatureGlobalsContext&&ReferenceEquals(source,document),CommitFields,creatureJobsWarningPolicy);
    }
    private void DefinitionCreatureGlobals_Click(object sender,RoutedEventArgs args)
    {
        if(creatureGlobalsWindow is not null){creatureGlobalsWindow.Activate();return;}
        var window=CreateCreatureGlobalsWindow();if(window is null)return;creatureGlobalsWindow=window;
        window.Closed+=(_,_)=>{if(ReferenceEquals(creatureGlobalsWindow,window))creatureGlobalsWindow=null;};
        if(CreatureGlobalsPresenter is { } present)present(window);else{window.Owner=Window.GetWindow(this);window.Show();}
    }
}
