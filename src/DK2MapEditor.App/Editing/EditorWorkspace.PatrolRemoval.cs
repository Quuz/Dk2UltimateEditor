using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private readonly EditorPatrolRemovalPolicy patrolRemovalPolicy=new();
    private PatrolRemovalWindow? patrolRemovalWindow;
    private bool patrolRemovalPending;
    private long patrolRemovalSelectionRevision;
    private void ClosePatrolRemoval()
    {
        patrolRemovalSelectionRevision++;
        var window=patrolRemovalWindow;patrolRemovalWindow=null;window?.Close();
    }
    private bool RemovePatrolWithConfirmation(EditorPatrolManagerSession manager,EditorPatrolManagerRoute route,
        bool clear,int occurrence,out EditorPatrolManagerRoute? result)
    {
        result=null;
        if(patrolRemovalPending||patrolPausePending||requestingPatrolWarning)throw new InvalidOperationException("Finish the current patrol request.");
        using var request=clear?patrolRemovalPolicy.BeginRoute(manager,route):patrolRemovalPolicy.BeginPoint(manager,route,occurrence);
        var context=patrolRemovalSelectionRevision;patrolRemovalPending=true;
        try
        {
            if(!request.RequiresPrompt){result=request.Accept();return true;}
            var window=new PatrolRemovalWindow(manager.Document,request,
                ()=>!patrolOwnerClosed&&context==patrolRemovalSelectionRevision&&ReferenceEquals(manager,patrolManager)&&ReferenceEquals(document,manager.Document))
                {Owner=Window.GetWindow(this)};
            patrolRemovalWindow=window;
            try{window.ShowDialog();}
            finally{if(ReferenceEquals(patrolRemovalWindow,window))patrolRemovalWindow=null;}
            if(window.CommitFailure is{ } error)throw new PatrolRemovalCommittedException(manager,error);
            result=window.Result;return request.IsAccepted;
        }
        catch(Exception error)when(request.IsAccepted&&error is not PatrolRemovalCommittedException)
        {throw new PatrolRemovalCommittedException(manager,error);}
        finally{patrolRemovalPending=false;RefreshPatrolRemoval();}
    }
    private void RefreshPatrolRemoval()=>PatrolRemovalReset.IsEnabled=patrolRemovalPolicy.Suppressed;
    private void PatrolRemovalReset_Click(object sender,RoutedEventArgs args)
    {ClosePatrolRemoval();patrolRemovalPolicy.ResetSuppression();RefreshPatrolRemoval();}
    private sealed class PatrolRemovalCommittedException(EditorPatrolManagerSession manager,Exception error)
        :InvalidOperationException("The patrol removal was committed, but its notification failed.",error)
    {public EditorPatrolManagerSession Manager {get;}=manager;}
}
