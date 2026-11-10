namespace DK2MapEditor.Core.Editing;

/// <summary>One tick per original main-window one-second timer callback.
/// The postincrement, locked deferral and clean due reset follow Files_AutoSave.</summary>
public sealed class EditorAutoSaveTimer
{
    public uint IntervalMinutes { get; set; } = 5;
    public uint Ticks { get; private set; }
    private bool writing;

    public bool Tick(bool locked, uint editorChangeCount, Action saveTemporary)
    {
        ArgumentNullException.ThrowIfNull(saveTemporary);
        if (writing) throw new InvalidOperationException("An autosave callback is already active.");
        if (IntervalMinutes == 0) return false;
        var previous = Ticks; Ticks = unchecked(Ticks + 1);
        if (previous < unchecked(IntervalMinutes * 60) || locked) return false;
        Ticks = 0;
        if (editorChangeCount == 0) return false;
        writing = true;
        try { saveTemporary(); return true; }
        finally { writing = false; }
    }

    public void Reset()
    {
        if (writing) throw new InvalidOperationException("Finish autosave before resetting its timer.");
        Ticks = 0;
    }
}
