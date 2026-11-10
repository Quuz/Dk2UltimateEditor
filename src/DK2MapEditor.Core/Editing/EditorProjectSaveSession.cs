using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.Core.Editing;

/// <summary>Original ordinary/temporary DKM state for an editable project.
/// Native-to-DKM conversion, ordinary MAP writer and window commands are separate
/// integration requirements; this session never silently substitutes exports.</summary>
public sealed class EditorProjectSaveSession : IDisposable
{
    private readonly NativeMapDocument document;
    private bool closed, writing;
    public string? TargetPath { get; private set; }
    public EditorAutoSaveTimer AutoSave { get; } = new();
    public bool CanSave => !closed && !writing && TargetPath is not null && document.EditorChangeCount != 0;

    public EditorProjectSaveSession(NativeMapDocument document, string? loadedProjectPath = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        this.document = document;
        if (loadedProjectPath is not null)
        {
            RequireProject();
            TargetPath = ProjectPath(loadedProjectPath);
        }
    }

    public void Save(Func<string, Stream>? createOutput = null)
    {
        Current();
        if (TargetPath is null) throw new InvalidOperationException("Choose a normal project target with Save As first.");
        SaveAs(TargetPath, createOutput);
    }

    public void SaveAs(string path, Func<string, Stream>? createOutput = null)
        => Write(ProjectPath(path), temporary: false, createOutput);

    public void SaveTemporary(string recoveryPath, Func<string, Stream>? createOutput = null)
        => Write(ProjectPath(recoveryPath), temporary: true, createOutput);

    public bool TickAutoSave(string recoveryPath, bool locked = false, Func<string, Stream>? createOutput = null)
    {
        Current();
        return AutoSave.Tick(locked || document.IsTemporarySaveLocked, document.EditorChangeCount,
            () => SaveTemporary(recoveryPath, createOutput));
    }

    private void Write(string path, bool temporary, Func<string, Stream>? createOutput)
    {
        Current(); RequireProject();
        if (writing) throw new InvalidOperationException("Finish the current save first.");
        if (Directory.Exists(path)) throw new IOException("The selected project path is a directory.");
        var parent = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(parent)) throw new DirectoryNotFoundException(parent);
        // Only a generated sibling file is removed on failure. The previous
        // user-selected target survives incomplete output and publication failure.
        var staging = Path.Combine(parent, ".dk2-save-" + Guid.NewGuid().ToString("N") + ".tmp");
        writing = true;
        try
        {
            if (temporary) document.BeginTemporaryProjectSave(); else document.BeginProjectSave();
            try
            {
                var bytes = document.ProjectSaveBytes();
                document.PublishCreatureLevelModifierSource(path);
                using (var stream = createOutput is null
                    ? new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                    : createOutput(staging))
                {
                    // Match source side-effect boundary: only after file creation.
                    using var statistics = document.BeginProjectStatisticsWrite(temporary);
                    stream.Write(document.ProjectSaveActivity(bytes, statistics?.Output));
                    stream.Flush();
                    if (stream is FileStream file) file.Flush(flushToDisk: true);
                }
                Current();
                File.Move(staging, path, overwrite: true);
            }
            finally { document.EndProjectSave(); }
            if (!temporary)
            {
                TargetPath = path;
                AutoSave.Reset();
                document.AcceptProjectSave();
            }
        }
        finally
        {
            writing = false;
            if (File.Exists(staging)) File.Delete(staging);
        }
    }

    private void RequireProject()
    {
        if (!document.IsLegacyProject || document.MainFile.Signature != LegacySectionFile.MapFileSignature)
            throw new InvalidOperationException("Normal DKM conversion for native/AREA inputs is not integrated yet.");
    }
    private static string ProjectPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        if (!Path.GetExtension(full).Equals(".dkm", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Choose a DKM project path.", nameof(path));
        return full;
    }
    private void Current() => ObjectDisposedException.ThrowIf(closed, this);
    public void Dispose() { closed = true; }
}

public sealed partial class NativeMapDocument
{
    internal bool IsSaveLocked => transactionOpen || activePenStroke is not null;
    internal bool IsTemporarySaveLocked => transactionOpen;
    internal void BeginTemporaryProjectSave()
    {
        if (transactionOpen) throw new InvalidOperationException("Finish the current transaction before temporary Save.");
        // Original WM_TIMER may save the current frame while the free pen is
        // captured. Keep the stroke, its initial snapshot and counters alive.
        transactionOpen = true;
    }
    internal void BeginProjectSave()
    {
        if (IsSaveLocked) throw new InvalidOperationException("Finish the current edit before saving.");
        transactionOpen = true;
    }
    internal void EndProjectSave() => transactionOpen = false;
    internal byte[] ProjectSaveBytes() => SerializeLegacyProject();
    internal ActivityStatisticsSave? BeginProjectStatisticsWrite(bool temporary)
        => activityStatistics?.BeginProjectWrite(temporary);
    internal byte[] ProjectSaveActivity(byte[] bytes, ActivityStatisticsPayload? statistics)
        => statistics is null ? bytes : ProjectSavedActivity(bytes, statistics);
    internal void AcceptProjectSave()
    {
        // A formerly absent optional section that was actually written now
        // belongs to the saved project. Baseline adoption must not hide it on
        // the next save simply because its bytes then equal that baseline.
        optionalProjectSections.RemoveWhere(section => !section.Data.Span.SequenceEqual(originals[section]));
        foreach (var file in files) originals[file] = file.Data.ToArray();
        if(IsLegacyProject){originalLevelModifierDelta=LevelModifierDeltaFile.Data.ToArray();originalLevelModifierOwned=LevelModifierOwnedFile.Data.ToArray();}
        if (!IsLegacyProject)
        {
            originalNativeScenarioNames = NativeScenarioNames.Data.ToArray();
            originalNativeGameTarget = NativeGameTarget.UInt32(0);
            originalNativeResizeFill = NativeResizeFill.UInt32(0);
            originalNativeGateBindings = NativeGateBindings.Data.ToArray();
            originalNativeAreaScopes = NativeAreaScopes.Data.ToArray();
            originalNativeOwnerScopes = NativeOwnerScopes.Data.ToArray();
        }
        ResetHistory();
        EditRevision++;
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
