using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private PatrolPauseWindow? patrolPauseWindow;
    private bool patrolPausePending;
    private long patrolPauseSelectionRevision;
    private void ClosePatrolPause()
    {patrolPauseSelectionRevision++;var window=patrolPauseWindow;patrolPauseWindow=null;window?.Close();}
    private EditorPatrolManagerRoute? SelectPatrolPause(EditorPatrolManagerSession manager,EditorPatrolManagerRoute route,int occurrence)
    {
        if(patrolPausePending||patrolRemovalPending||requestingPatrolWarning)throw new InvalidOperationException("Finish the current patrol request.");
        using var session=manager.SelectPause(route,occurrence);
        var context=patrolPauseSelectionRevision;patrolPausePending=true;
        try
        {
            var window=new PatrolPauseWindow(manager.Document,session,
                ()=>!patrolOwnerClosed&&context==patrolPauseSelectionRevision&&ReferenceEquals(manager,patrolManager)&&ReferenceEquals(document,manager.Document))
                {Owner=Window.GetWindow(this)};
            patrolPauseWindow=window;
            try{window.ShowDialog();}finally{if(ReferenceEquals(patrolPauseWindow,window))patrolPauseWindow=null;}
            if(window.CommitFailure is{ } error)throw new PatrolPauseCommittedException(manager,error);
            return session.IsAccepted?window.Result:null;
        }
        finally{patrolPausePending=false;}
    }
    private sealed class PatrolPauseCommittedException(EditorPatrolManagerSession manager,Exception error)
        :InvalidOperationException("The patrol pause was committed, but its notification failed.",error)
    {public EditorPatrolManagerSession Manager {get;}=manager;}
}
