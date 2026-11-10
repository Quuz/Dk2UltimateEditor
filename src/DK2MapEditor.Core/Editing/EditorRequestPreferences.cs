namespace DK2MapEditor.Core.Editing;

public enum EditorConfirmationPrompt { RegionArea, RegionGate, RegionEffect, UsedAreas, ObjectRemoval, UsedThingRemoval, UsedThingModification, UsedCursorRemoval, ScenarioPointRemoval, ScenarioConditionRemoval, ScenarioActionRemoval }

/// <summary>A guarded request; replying is distinct from merely closing its window.</summary>
public interface IEditorConfirmationRequest : IDisposable
{
    NativeMapDocument Document { get; }
    EditorConfirmationPrompt Prompt { get; }
    int Count { get; }
    bool RequiresPrompt { get; }
    bool IsCurrent { get; }
    bool IsClosed { get; }
    bool IsAccepted { get; }
    bool CanDecline(bool remember);
    int Accept(bool remember = false);
    bool Decline(bool remember = false);
}

/// <summary>Original process request bits, scoped to a workspace and outside map history.
/// The same REQUEST_TRIGGERS bit is shared by areas and creature callers.</summary>
public sealed class EditorRequestPreferences(uint initialFlags = 0)
{
    private object? active;
    private object? notification;
    public uint Flags { get; private set; } = initialFlags;
    internal void Acquire(object request)
    { if (active is not null || notification is not null) throw new InvalidOperationException("Finish the current confirmation request."); active = request; }
    // An informational history failure may interrupt an existing confirmation.
    // It cannot approve that command or replace its lease; only one notice is
    // allowed, and no new command may acquire a lease until it closes.
    internal void AcquireNotification(object request)
    {if(notification is not null)throw new InvalidOperationException("Finish the current error notice.");notification=request;}
    internal void Release(object request)
    { if (ReferenceEquals(active, request)) active = null; if(ReferenceEquals(notification,request))notification=null; }
    internal void Remember(object request, uint bit, bool enabled)
    {
        if (!ReferenceEquals(active, request) && !ReferenceEquals(notification,request)) throw new InvalidOperationException("The confirmation request is no longer active.");
        Flags = (Flags & ~bit) | (enabled ? bit : 0);
    }
    internal void Reset(uint bits)
    { if (active is not null || notification is not null) throw new InvalidOperationException("Finish the current confirmation request before resetting preferences."); Flags &= ~bits; }
}
