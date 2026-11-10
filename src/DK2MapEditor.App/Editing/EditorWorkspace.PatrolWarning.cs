using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private readonly EditorPatrolWarningPolicy patrolWarningPolicy=new();
    private PatrolWarningWindow? patrolWarningWindow;
    private bool updatingPatrolPoints,requestingPatrolWarning;
    private long patrolWarningSelectionRevision;
    private void ClosePatrolWarning()
    {
        patrolWarningSelectionRevision++;
        var window=patrolWarningWindow;patrolWarningWindow=null;window?.Close();
    }
    private void PatrolSelectionWarning()
    {
        if(updatingPatrol||updatingPatrolPoints||patrolOwnerClosed||document is null||patrolManager is null||patrolManager.IsClosed)return;
        patrolWarningSelectionRevision++;
        if(requestingPatrolWarning){patrolWarningWindow?.Close();return;}
        if(patrolRemovalPending||patrolPausePending)return;
        RequestPatrolWarning(patrolManager,false);
    }
    private void RequestPatrolWarning(EditorPatrolManagerSession manager,bool afterRemoval)
    {
        if(requestingPatrolWarning||patrolRemovalPending||patrolPausePending||updatingPatrol||updatingPatrolPoints||patrolOwnerClosed||manager.IsClosed
            ||!ReferenceEquals(manager,patrolManager)||!ReferenceEquals(document,manager.Document)||manager.RequiresReload)return;
        try
        {
            using var request=afterRemoval?patrolWarningPolicy.BeginAfterRemoval(manager)
                :patrolWarningPolicy.BeginSelection(manager,(PatrolRouteList.SelectedItem as PatrolRouteView)?.Route);
            if(request is null)return;
            var context=patrolWarningSelectionRevision;requestingPatrolWarning=true;
            var window=new PatrolWarningWindow(manager.Document,request,
                ()=>!patrolOwnerClosed&&context==patrolWarningSelectionRevision&&ReferenceEquals(manager,patrolManager)&&ReferenceEquals(document,manager.Document))
                {Owner=Window.GetWindow(this)};
            patrolWarningWindow=window;
            try{window.ShowDialog();}
            finally{if(ReferenceEquals(patrolWarningWindow,window))patrolWarningWindow=null;}
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException)
        {
            if(!patrolOwnerClosed&&ReferenceEquals(manager,patrolManager)&&ReferenceEquals(document,manager.Document))
            {ShowPatrolStatus("Editor.PatrolWarningFailed");PatrolStatus.ToolTip=error.Message;}
        }
        finally{requestingPatrolWarning=false;RefreshPatrolWarning();}
    }
    private void SelectPatrolOccurrence(int index)
    {
        var previous=updatingPatrolPoints;updatingPatrolPoints=true;
        try{PatrolPointList.SelectedIndex=index;}finally{updatingPatrolPoints=previous;}
    }
    private void PatrolWarningReset_Click(object sender,RoutedEventArgs args)
    {ClosePatrolWarning();patrolWarningPolicy.ResetSuppression();RefreshPatrolWarning();}
}
