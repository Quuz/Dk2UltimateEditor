using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Original variable/timer manager acceptance: one change count even
    /// for an equal name, without adding Undo. Generic name editing remains undoable.</summary>
    public void ApplyEditorScenarioNameDraft(ScenarioNameDraft draft, string name)
    {
        ArgumentNullException.ThrowIfNull(draft);ArgumentNullException.ThrowIfNull(name);
        if (transactionOpen || activePenStroke is not null || !ReferenceEquals(draft.Owner,this)
            || draft.Revision!=EditRevision || !UnusedScriptFingerprint().AsSpan().SequenceEqual(draft.Fingerprint))
            throw new InvalidOperationException("Refresh the scenario-name draft after document changes.");
        _=Records();
        var location=ScenarioAliasLocation(draft.Kind,draft.Index);
        var slot=ScenarioSlot(draft.Kind,draft.Index);
        var encoded=ScenarioAlias.Encode(name);
        var nextCount=checked(changeCount+1);var nextRevision=checked(EditRevision+1);

        // Original aliases are separate globals, outside every Undo payload.
        // Our whole-file history contains KTRG alias tails: rebase this one slot
        // in both directions before writing anything, preserving every other byte.
        Change Rebase(Change entry)
        {
            var deltas=entry.Deltas.Select(delta=>
            {
                if (!ReferenceEquals(delta.File,location.File))return delta;
                return new Delta(delta.File,RebaseAlias(delta.Before),RebaseAlias(delta.After));
            }).ToArray();
            return entry with {Deltas=deltas,BeforeCount=checked(entry.BeforeCount+1),AfterCount=checked(entry.AfterCount+1),
                BeforeEditorChangeCount=unchecked(entry.BeforeEditorChangeCount+1),AfterEditorChangeCount=unchecked(entry.AfterEditorChangeCount+1)};
        }
        byte[] RebaseAlias(byte[] bytes)
        {
            var offset=AliasSnapshotOffset(bytes,slot);
            if (offset is null || bytes.AsSpan(offset.Value,64).SequenceEqual(encoded))return bytes;
            var copy=(byte[])bytes.Clone();encoded.CopyTo(copy,offset.Value);return copy;
        }
        var previousUndo=undo.Select(Rebase).ToArray();var previousRedo=redo.Select(Rebase).ToArray();
        var current=(byte[])location.File.Bytes.Clone();encoded.CopyTo(current,location.Offset);
        undo.Clear();undo.AddRange(previousUndo);redo.Clear();redo.AddRange(previousRedo);
        location.File.Bytes=current;changeCount=nextCount;editorChangeCount=unchecked(editorChangeCount+1);EditRevision=nextRevision;
        HistoryChanged?.Invoke(this,EventArgs.Empty);Changed?.Invoke(this,EventArgs.Empty);
    }

    private int? AliasSnapshotOffset(byte[] bytes,int slot)
    {
        uint Number(int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset,4));
        if (!IsLegacyProject)
        {
            if (bytes.Length!=ScenarioNamesSize || Number(0)!=ScenarioNamesSignature || Number(4)!=1 || Number(8)!=ScenarioNamesSize)
                return null; // Unknown companion snapshots remain opaque.
            return 12+slot*64;
        }
        if (bytes.Length<12 || Number(0)!=LegacyEvents)return null;
        var version=Number(4);if(version is not (2 or 3))return null;
        if(bytes.Length<24 || Number(8)!=bytes.Length-12)throw new InvalidDataException("Invalid historical alias snapshot envelope.");
        long offset=24;
        foreach(var (structure,countOffset) in new[]{(version==2?"TRIGGERLINKV2":"TRIGGERLINK",12),("TRIGGER",16),("ACTION",20)})
            offset=checked(offset+(long)Number(countOffset)*LegacyProjectSchemas.Size(structure));
        if(offset+146*64!=bytes.Length)throw new InvalidDataException("Invalid historical alias snapshot record/table bounds.");
        return checked((int)offset+slot*64);
    }
}
