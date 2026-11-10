namespace DK2MapEditor.Core.Editing;

/// <summary>Per-open-manager Undo ownership. The original undocnt counts manager
/// commands; operation identity additionally prevents Undo of an unrelated edit.</summary>
public sealed class EditorObjectManagerHistory(NativeMapDocument document) : IDisposable
{
    private readonly List<HistoryOperation> owned = [];
    private bool running, closed;
    public event EventHandler? Changed;
    public bool CanUndo => !closed && !running && !document.IsSaveLocked && owned.Count!=0
        && ReferenceEquals(owned[^1],document.ManagerUndoHead);

    /// <summary>Wrap only a manager command. The optional acceptance predicate
    /// distinguishes a declined/stale confirmation from edits made by its host.</summary>
    public T Execute<T>(Func<T> command,Func<bool>? accepted=null)
    {
        ArgumentNullException.ThrowIfNull(command);
        if(closed || running || document.IsSaveLocked)throw new InvalidOperationException("The object manager cannot execute a command now.");
        if(owned.Count>document.HistoryDepth)owned.RemoveRange(0,owned.Count-document.HistoryDepth);
        try{owned.EnsureCapacity(checked(owned.Count+1));}
        catch(OutOfMemoryException error){throw new EditorHistoryMemoryException(document,error);}
        var head=document.ManagerUndoHead;var revision=document.EditRevision;
        running=true;
        try{return command();}
        finally
        {
            running=false;
            // An observer may throw after commit. Track that one committed root,
            // but never claim multiple/reentrant edits or an unchanged old head.
            if(!closed && document.EditRevision>revision && document.EditRevision-revision==1
                && document.ManagerUndoHead is {} next && !ReferenceEquals(head,next) && (accepted?.Invoke()??true))
            {
                owned.Add(next);
                if(owned.Count>document.HistoryDepth)owned.RemoveRange(0,owned.Count-document.HistoryDepth);
            }
            Changed?.Invoke(this,EventArgs.Empty);
        }
    }
    public bool Undo()
    {
        if(!CanUndo)return false;
        var head=owned[^1];running=true;
        try{document.Undo();return true;}
        finally
        {
            running=false;
            // Failed replay leaves the original head retryable. A postcommit
            // observer failure must consume the local command, not its neighbour.
            if(!closed && !ReferenceEquals(document.ManagerUndoHead,head))owned.RemoveAt(owned.Count-1);
            Changed?.Invoke(this,EventArgs.Empty);
        }
    }
    public void Dispose(){closed=true;owned.Clear();Changed=null;}
}

public sealed partial class NativeMapDocument
{
    internal HistoryOperation? ManagerUndoHead=>undo.Count==0?null:undo[^1].Operation;
}
