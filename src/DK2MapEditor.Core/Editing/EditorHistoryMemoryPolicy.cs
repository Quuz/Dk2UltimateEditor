namespace DK2MapEditor.Core.Editing;

/// <summary>Only a failed allocation in preparing history, not a general edit
/// or observer exception. Does not expose a private properties working document.</summary>
public sealed class EditorHistoryMemoryException : OutOfMemoryException
{
    private readonly NativeMapDocument document;
    internal EditorHistoryMemoryException(NativeMapDocument document,OutOfMemoryException error)
        :base("Not enough memory to prepare Undo data.",error){this.document=document;}
    public bool IsSafeToNotify=>!document.IsSaveLocked;
    internal long Revision=>document.EditRevision;
}

/// <summary>Informational source REQUEST_UNDOERROR: replying cannot retry,
/// commit, undo or otherwise change the failed operation.</summary>
public sealed class EditorHistoryMemoryNotice : IDisposable
{
    private readonly Func<bool> ownerCurrent;
    private readonly long revision;
    internal EditorHistoryMemoryNotice(EditorHistoryMemoryPolicy policy,EditorHistoryMemoryException failure,Func<bool> ownerCurrent)
    {Policy=policy;Failure=failure;this.ownerCurrent=ownerCurrent;revision=failure.Revision;}
    internal EditorHistoryMemoryPolicy Policy {get;}
    internal bool TargetCurrent=>ownerCurrent()&&Failure.IsSafeToNotify&&Failure.Revision==revision;
    public EditorHistoryMemoryException Failure {get;}
    public bool RequiresNotice=>(Policy.Flags&EditorHistoryMemoryPolicy.NoticeBit)==0;
    public bool IsCurrent=>Policy.IsCurrent(this);
    public bool IsClosed {get;internal set;}
    public bool IsAcknowledged {get;private set;}
    public bool Acknowledge(bool remember=false)
    {
        if(!IsCurrent){Dispose();return false;}
        Policy.Remember(this,remember);IsAcknowledged=true;Policy.Release(this);return true;
    }
    public void Dispose()=>Policy.Release(this);
}

public sealed class EditorHistoryMemoryPolicy : IDisposable
{
    public const uint NoticeBit=0x00000008;
    private readonly EditorRequestPreferences preferences;
    private EditorHistoryMemoryNotice? active;
    private bool disposed;
    public EditorHistoryMemoryPolicy(uint initialFlags=0):this(new EditorRequestPreferences(initialFlags)) { }
    public EditorHistoryMemoryPolicy(EditorRequestPreferences preferences)=>this.preferences=preferences??throw new ArgumentNullException(nameof(preferences));
    public uint Flags=>preferences.Flags;
    public EditorHistoryMemoryNotice Begin(EditorHistoryMemoryException failure,Func<bool> ownerCurrent)
    {
        ArgumentNullException.ThrowIfNull(failure);ArgumentNullException.ThrowIfNull(ownerCurrent);ObjectDisposedException.ThrowIf(disposed,this);
        if(active is not null)throw new InvalidOperationException("Finish the current Undo memory notice.");
        var notice=new EditorHistoryMemoryNotice(this,failure,ownerCurrent);
        preferences.AcquireNotification(notice);active=notice;
        try{if(!IsCurrent(notice))throw new InvalidOperationException("The failed operation has not finished or its owner changed.");return notice;}
        catch{Release(notice);throw;}
    }
    private bool Owns(EditorHistoryMemoryNotice notice)=>!disposed&&!notice.IsClosed&&ReferenceEquals(active,notice)&&ReferenceEquals(notice.Policy,this);
    internal bool IsCurrent(EditorHistoryMemoryNotice notice)
    {
        if(!Owns(notice))return false;
        try{return notice.TargetCurrent&&Owns(notice);}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or FormatException or OverflowException){return false;}
    }
    internal void Remember(EditorHistoryMemoryNotice notice,bool remember)
    {if(notice.RequiresNotice)preferences.Remember(notice,NoticeBit,remember);}
    internal void Release(EditorHistoryMemoryNotice notice)
    {if(ReferenceEquals(active,notice)){active=null;preferences.Release(notice);}notice.IsClosed=true;}
    public void ResetSuppression()
    {ObjectDisposedException.ThrowIf(disposed,this);active?.Dispose();preferences.Reset(NoticeBit);}
    public void Dispose(){if(disposed)return;active?.Dispose();disposed=true;}
}
