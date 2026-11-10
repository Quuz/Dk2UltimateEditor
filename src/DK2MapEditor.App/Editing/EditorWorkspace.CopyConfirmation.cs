using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;
using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private long copyContextRevision;
    public string AutomationInteractionToken => $"{copyContextRevision}|{Canvas.Tool}|{Canvas.PaintTile}";
    private bool requestingCopyReplacement;
    private bool copyOwnerClosed;
    private CopyBufferConfirmationWindow? copyConfirmationWindow;
    private readonly CopyBufferConfirmationState copyConfirmation = new();
    public bool ConfiguredCopyBufferReplacementConfirmation => copyConfirmation.Configured;
    public bool ConfirmCopyBufferReplacement
    {
        get => copyConfirmation.RequiresConfirmation;
        set
        {
            if (copyConfirmation.Configured == value && !copyConfirmation.Suppressed) return;
            copyConfirmation.Configure(value); CopyConfirmationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? CopyConfirmationChanged;
    /// <summary>Actual-command prompt seam. Null uses the real modal EN/RU
    /// window; tests can supply explicit accepted/declined replies.</summary>
    public Func<CopyBufferRequest, CopyBufferDecision>? CopyBufferPrompt { get; set; }
    public CopyBufferConfirmationWindow CreateCopyBufferConfirmation(CopyBufferRequest request) => new(request);
    private CopyBufferDecision ShowCopyBufferConfirmation(CopyBufferRequest request)
    {
        var window = CreateCopyBufferConfirmation(request); copyConfirmationWindow = window;
        window.Owner = Window.GetWindow(this);
        try { window.ShowDialog(); return window.Decision; }
        finally { if (ReferenceEquals(copyConfirmationWindow, window)) copyConfirmationWindow = null; }
    }
    private void CloseCopyBufferConfirmation()
    { copyContextRevision++; copyConfirmationWindow?.Close(); copyConfirmationWindow = null; CloseSelectionRecoveryConfirmation(); }
    public void EndCopyCommandLifetime() { copyOwnerClosed = true; CloseCopyBufferConfirmation(); }
    private bool ApproveBufferReplacement(CopyBufferOperation operation, int width, int height, Func<bool> perform, SelectionClipboard? incoming = null)
    {
        if (copyOwnerClosed || requestingCopyReplacement || requestingSelectionRecovery) return false;
        var previous = clipboard;
        if (previous is null || !ConfirmCopyBufferReplacement) return perform();
        var revision = copyContextRevision; var source = document; var selection = Canvas.Selection;
        var oldBytes = SelectionFile.Serialize(previous); var newBytes = incoming is null ? null : SelectionFile.Serialize(incoming);
        var request = new CopyBufferRequest(operation, previous.Tiles.Width, previous.Tiles.Height, previous.ObjectCount, previous.ScriptCount, width, height);
        requestingCopyReplacement = true;
        try
        {
            var decision = (CopyBufferPrompt ?? ShowCopyBufferConfirmation)(request);
            if (!decision.Replace || revision != copyContextRevision || !ReferenceEquals(source, document) || selection != Canvas.Selection
                || !ReferenceEquals(previous, clipboard) || !oldBytes.AsSpan().SequenceEqual(SelectionFile.Serialize(previous))
                || incoming is not null && !newBytes!.AsSpan().SequenceEqual(SelectionFile.Serialize(incoming))) return false;
            if (!perform()) return false;
            // Source Misc_MessageBoxExtReset only remembers the checkbox for
            // its default affirmative reply, never for Keep/Cancel or stale data.
            copyConfirmation.AcceptReplacement(decision.SuppressFutureQuestions);
            if (decision.SuppressFutureQuestions) CopyConfirmationChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        finally { requestingCopyReplacement = false; }
    }
    /// <summary>Human-facing Copy/Cut/rotated-copy route. Trusted programmatic
    /// Copy/Cut APIs stay nonmodal; every actual menu/shortcut uses this route.</summary>
    public bool RequestCopy(bool cut = false, bool rotated = false)
    {
        if (copyOwnerClosed || requestingCopyReplacement || requestingSelectionRecovery || document is null || Canvas.Selection is not { } rect || !CommitFields()) return false;
        var success = false;
        RecordOperation(() =>
        {
            success = ApproveBufferReplacement(cut ? CopyBufferOperation.Cut : rotated ? CopyBufferOperation.RotatedCopy : CopyBufferOperation.Copy,
                rotated ? rect.Height : rect.Width, rotated ? rect.Width : rect.Height, () =>
                {
                    if (rotated) { LoadSelectionIntoBuffer(document.CopySelection(rect, protectBoundary: true).RotateClockwise()); ClearTransformStatus(); }
                    else CaptureSelection(cut);
                    return true;
                });
            return null;
        });
        return success;
    }
    /// <summary>Parsed DKZ candidate, before replacing any buffer. Direct map
    /// insertion uses PasteLoadedSelection and never asks to replace the buffer.</summary>
    public bool RequestLoadSelectionIntoBuffer(SelectionClipboard selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return ApproveBufferReplacement(CopyBufferOperation.Import, selection.Tiles.Width, selection.Tiles.Height,
            () => { LoadSelectionIntoBuffer(selection); return true; }, selection);
    }
    public bool RequestSelectionFile(string path, bool pasteIntoSelection = false)
    {
        if (copyOwnerClosed || requestingCopyReplacement || requestingSelectionRecovery) return false;
        if (pasteIntoSelection && (document is null || Canvas.Selection is null)) return false;
        var revision=copyContextRevision;var source=document;var sourceRevision=source?.EditRevision;var target=Canvas.Selection;var old=clipboard;
        var oldBytes=old is null?null:SelectionFile.Serialize(old);
        var mapBytes=source?.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray());
        var candidate=SelectionFile.ReadForImport(path);var selection=candidate.Selection;
        var incoming=SelectionFile.Serialize(selection);
        bool Current()
            =>!copyOwnerClosed&&revision==copyContextRevision&&ReferenceEquals(source,document)&&sourceRevision==source?.EditRevision
                &&target==Canvas.Selection&&ReferenceEquals(old,clipboard)
                &&(old is null||oldBytes!.AsSpan().SequenceEqual(SelectionFile.Serialize(old)))
                &&(source is null||source.Files.Count==mapBytes!.Count&&source.Files.All(file=>file.Data.Span.SequenceEqual(mapBytes[file.ExportPath])))
                &&incoming.AsSpan().SequenceEqual(SelectionFile.Serialize(selection))&&candidate.SourceIsUnchanged();
        if(candidate.RequiresRecoveryConsent)
        {
            var request=new SelectionRecoveryRequest(System.IO.Path.GetFileName(path),selection.Tiles.Width,selection.Tiles.Height,
                selection.ObjectCount,pasteIntoSelection,candidate.Recovery);
            if(!RequestSelectionRecovery(request)||!Current())return false;
        }
        if(!Current())return false;
        if(pasteIntoSelection){PasteLoadedSelection(selection);return true;}
        return ApproveBufferReplacement(CopyBufferOperation.Import,selection.Tiles.Width,selection.Tiles.Height,
            ()=>{if(!Current())return false;LoadSelectionIntoBuffer(selection);return true;},selection);
    }
}
