using System.Text;
using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.Core.Editing;

/// <summary>Frozen original no-scenario MAP output and explicit loss consent.
/// Preparation does not write files, alter statistics or clear edit history.</summary>
public sealed class EditorTextMapSavePlan
{
    internal EditorTextMapSavePlan(EditorTextMapSaveSession owner, long revision, byte[] fingerprint,
        string path, byte[] bytes, IReadOnlyList<TextMapLoss> losses)
    { Owner = owner; Revision = revision; Fingerprint = fingerprint; Path = path; Bytes = bytes; Losses = Array.AsReadOnly(losses.ToArray()); }
    internal EditorTextMapSaveSession Owner { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal byte[] Bytes { get; }
    public string Path { get; }
    public IReadOnlyList<TextMapLoss> Losses { get; }
    public bool NeedsLossConsent => Losses.Count != 0;
}

/// <summary>Ordinary historical MAP save, distinct from MAP+editable-copy export.
/// Native/project inputs retain their full working data after accepting a lossy
/// MAP projection, as in the original editor. No DKM statistics write occurs.</summary>
public sealed class EditorTextMapSaveSession : IDisposable
{
    private readonly NativeMapDocument document;
    private bool closed, writing;
    public string? TargetPath { get; private set; }
    public EditorAutoSaveTimer AutoSave { get; } = new();
    public bool CanSave => !closed && !writing && TargetPath is not null && document.EditorChangeCount != 0;

    public EditorTextMapSaveSession(NativeMapDocument document, string? loadedTextPath = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        this.document = document;
        if (loadedTextPath is not null) TargetPath = TextPath(loadedTextPath);
    }

    public EditorTextMapSavePlan PrepareSave()
    {
        Current();
        if (TargetPath is null) throw new InvalidOperationException("Choose a MAP target with Save As first.");
        return PrepareSaveAs(TargetPath);
    }
    public EditorTextMapSavePlan PrepareSaveAs(string path)
    {
        Current();
        if (writing || document.IsSaveLocked) throw new InvalidOperationException("Finish the current edit or save first.");
        var target = TextPath(path);
        var projection = TextMapFile.Project(document, TextMapScriptMode.OriginalNoScenarios);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(projection.Text);
        return new(this, document.EditRevision, document.SaveWorkingFingerprint(), target, bytes, projection.Limitations);
    }

    public bool Save(EditorTextMapSavePlan plan, bool acceptLosses, Func<string, Stream>? createOutput = null)
    {
        ArgumentNullException.ThrowIfNull(plan); Current();
        if (!ReferenceEquals(plan.Owner, this)) throw new InvalidOperationException("Use this save session's preparation.");
        if (plan.NeedsLossConsent && !acceptLosses) return false;
        if (writing || document.IsSaveLocked || plan.Revision != document.EditRevision
            || !document.SaveWorkingFingerprint().AsSpan().SequenceEqual(plan.Fingerprint))
            throw new InvalidOperationException("Prepare MAP save again after document changes.");
        if (Directory.Exists(plan.Path)) throw new IOException("The MAP target is a directory.");
        var parent = System.IO.Path.GetDirectoryName(plan.Path)!;
        if (!Directory.Exists(parent)) throw new DirectoryNotFoundException(parent);
        var staging = System.IO.Path.Combine(parent, ".dk2-map-save-" + Guid.NewGuid().ToString("N") + ".tmp");
        writing = true;
        try
        {
            document.BeginProjectSave();
            try
            {
                using (var stream = createOutput is null ? new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None) : createOutput(staging))
                {
                    stream.Write(plan.Bytes);
                    stream.Flush();
                    if (stream is FileStream file) file.Flush(flushToDisk: true);
                }
                Current();
                // Guard any direct metadata change that did not go through an
                // edit callback. Consent is for these exact captured bytes.
                if (plan.Revision != document.EditRevision || !document.SaveWorkingFingerprint().AsSpan().SequenceEqual(plan.Fingerprint))
                    throw new InvalidOperationException("MAP save document changed during writing.");
                File.Move(staging, plan.Path, overwrite: true);
            }
            finally { document.EndProjectSave(); }
            TargetPath = plan.Path;
            AutoSave.Reset();
            document.AcceptProjectSave();
            return true;
        }
        finally
        {
            writing = false;
            if (File.Exists(staging)) File.Delete(staging);
        }
    }

    private static string TextPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = System.IO.Path.GetFullPath(path);
        if (!System.IO.Path.GetExtension(full).Equals(".map", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Choose a MAP path.", nameof(path));
        return full;
    }
    private void Current() => ObjectDisposedException.ThrowIf(closed, this);
    public void Dispose() { closed = true; }
}

public sealed partial class NativeMapDocument
{
    internal byte[] SaveWorkingFingerprint() => ScenarioPresentationSaveFingerprint();
    private void MarkLoadedTextMap()
    {
        foreach (var file in files) originals[file] = file.Data.ToArray();
        ResetHistory();
    }
}
