namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Original Heros_SelectKill removes the band list entry only.
    /// Scenario roots and action operands survive; Undo restores the exact band.
    /// Generic DeleteRecord keeps its separate reference-protection contract.</summary>
    public void DeleteEditorHeroBand(NativeRecord band)
    {
        if(transactionOpen || activePenStroke is not null)throw new InvalidOperationException("Finish the active edit before deleting a hero band.");
        _=RequireEditorHeroBandInputLayout(band);
        _=checked(changeCount+1);_=checked(EditRevision+1);
        var file=band.File;
        var bytes=new byte[checked(file.Length-band.Length)];
        file.Data.Span[..band.Offset].CopyTo(bytes);
        file.Data.Span[(band.Offset+band.Length)..].CopyTo(bytes.AsSpan(band.Offset));
        if(IsLegacyProject)
        {
            // The project band section is a literal flat list with no count.
            WriteUInt32(bytes,8,checked((uint)bytes.Length-12));
            Edit("Delete hero band",()=>file.Bytes=bytes);
        }
        else Edit("Delete hero band",()=>
        {
            RebaseNativeOwnerRemovals([band], preservePoints: true);
            file.Bytes=bytes;UpdateCollectionHeader(file,-band.Length,-1,false);
        });
    }
}
