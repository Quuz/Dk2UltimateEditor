namespace DK2MapEditor.Core.Editing;

/// <summary>Immutable operation metadata; never exposes undo payload arrays.</summary>
public sealed record HistoryOperation(string Description, IReadOnlyList<HistoryOperation> Children);
public sealed record EditHistorySnapshot(IReadOnlyList<HistoryOperation> Undo, IReadOnlyList<HistoryOperation> Redo,
    long ChangeCount, long SnapshotBytes, int Depth)
{ public uint EditorChangeCount { get; init; } }

public sealed partial class NativeMapDocument
{
    public const int DefaultHistoryDepth = 200;
    public const int MaximumHistoryDepth = 65535;
    private int historyDepth = DefaultHistoryDepth;
    private long changeCount;
    private readonly Stack<List<HistoryOperation>> operationChildren = new();
    // Private allocation boundaries keep ownership consistent across commit and
    // replay. Fault checks replace these delegates per document, never exhaust
    // the process heap or change the public editing API.
    private Func<byte[],byte[]> historyBufferCopy = static bytes => (byte[])bytes.Clone();
    private Func<string,List<HistoryOperation>,HistoryOperation> historyOperationFactory =
        static (description,children) => new(description,Array.AsReadOnly(children.ToArray()));
    public int HistoryDepth => historyDepth;
    public bool CanClearHistory => !transactionOpen && activePenStroke is null && (undo.Count != 0 || redo.Count != 0);
    public event EventHandler? HistoryChanged;

    /// <summary>Undo is oldest first; redo is next-to-apply first. Metadata only.</summary>
    public EditHistorySnapshot GetHistory() => new(
        Array.AsReadOnly(undo.Select(change => change.Operation).ToArray()),
        Array.AsReadOnly(redo.AsEnumerable().Reverse().Select(change => change.Operation).ToArray()),
        changeCount, historyBytes, historyDepth) { EditorChangeCount = editorChangeCount };

    /// <summary>Discard retained Undo/Redo without changing the live document,
    /// its counters, save baseline or configured history depth.</summary>
    public void ClearHistory()
    {
        if (transactionOpen || activePenStroke is not null) throw new InvalidOperationException("History cannot be cleared during an edit.");
        if (undo.Count == 0 && redo.Count == 0) return;
        undo.Clear(); redo.Clear(); historyBytes = 0;
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Zero clears both stacks and disables tracking, not editing.</summary>
    public void SetHistoryDepth(int depth)
    {
        if (depth is < 0 or > MaximumHistoryDepth) throw new ArgumentOutOfRangeException(nameof(depth));
        if (transactionOpen || activePenStroke is not null) throw new InvalidOperationException("History settings cannot change during an edit.");
        if (depth == historyDepth) return;
        historyDepth = depth;
        if (depth == 0) { undo.Clear(); redo.Clear(); historyBytes = 0; }
        else TrimHistory();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TrimHistory()
    {
        // Prefer keeping the next redo operation if the user lowers the limit
        // after Undo. Always retain one oversized operation for atomic Undo.
        while (undo.Count + redo.Count > historyDepth || historyBytes > MaximumHistoryBytes && undo.Count + redo.Count > 1)
        {
            if (undo.Count != 0) { historyBytes -= undo[0].ByteCost; undo.RemoveAt(0); }
            else { historyBytes -= redo[0].ByteCost; redo.RemoveAt(0); }
        }
    }

    private void ResetHistory()
    {
        undo.Clear(); redo.Clear(); historyBytes = 0; changeCount = 0; editorChangeCount = 0;
    }

    private HistoryOperation Operation(string description, List<HistoryOperation> children) =>
        historyOperationFactory(description,children);

    // Only allocations owned by preparing history are classified here. Edit
    // actions and postcommit observers run outside this boundary. Existing
    // rollback/finally paths finish before the UI may present the notice.
    private T PrepareHistory<T>(Func<T> prepare)
    {
        try{return prepare();}
        catch(EditorHistoryMemoryException){throw;}
        catch(OutOfMemoryException error){throw new EditorHistoryMemoryException(this,error);}
    }

    private List<HistoryOperation> PushHistoryChildren() => PrepareHistory(()=>
    {var children=new List<HistoryOperation>();operationChildren.Push(children);return children;});

    /// <summary>Prepare every allocation, cost and trim decision before changing
    /// either stack. Publication itself only moves already-owned references.</summary>
    private void PublishHistoryChange(Change? entry)
    {
        var (bytes,remove)=PrepareHistory(()=>
        {
            var remainingBytes=checked(historyBytes-redo.Sum(change=>change.ByteCost)+(entry?.ByteCost??0));
            var count=checked(undo.Count+(entry is null?0:1));var trimCount=0;
            while(count-trimCount>historyDepth || remainingBytes>MaximumHistoryBytes&&count-trimCount>1)
            {
                var discarded=trimCount<undo.Count?undo[trimCount]:entry!;
                remainingBytes=checked(remainingBytes-discarded.ByteCost);trimCount++;
            }
            if(entry is not null)undo.EnsureCapacity(checked(undo.Count+1));
            return(remainingBytes,trimCount);
        });
        redo.Clear();
        if(entry is not null)undo.Add(entry);
        if(remove!=0)undo.RemoveRange(0,remove);
        historyBytes=bytes;
    }

    private void ReplayHistory(bool backward)
    {
        var origin=backward?undo:redo;var destination=backward?redo:undo;var change=origin[^1];
        var revision=checked(EditRevision+1);
        var copies=PrepareHistory(()=>
        {
            var preparedCopies=change.Deltas.Select(delta=>(delta.File,Bytes:historyBufferCopy(backward?delta.Before:delta.After))).ToArray();
            destination.EnsureCapacity(checked(destination.Count+1));return preparedCopies;
        });
        origin.RemoveAt(origin.Count-1);destination.Add(change);
        foreach(var copy in copies)copy.File.Bytes=copy.Bytes;
        changeCount=backward?change.BeforeCount:change.AfterCount;
        editorChangeCount=backward?change.BeforeEditorChangeCount:change.AfterEditorChangeCount;
        EditRevision=revision;
        if(backward)RecordActivity(ActivityCounterEvent.Undo);
        HistoryChanged?.Invoke(this,EventArgs.Empty);Changed?.Invoke(this,EventArgs.Empty);
    }
}
