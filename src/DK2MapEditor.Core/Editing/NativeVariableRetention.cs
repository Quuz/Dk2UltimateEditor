using System.Buffers.Binary;
using System.Collections.Immutable;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    // DKM remains an original-editor section stream. The immutable source
    // companion carries native records that have no editable DKM counterpart.
    internal byte[] nativeRetainedVariables=[];

    internal static bool IsUnrepresentedNativeVariable(ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length!=16)return false;
        var kind=BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        if(kind==2)
        {
            var player=BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..]);
            if(player is <1 or >7)return false;
            var category=BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]);
            var group=category switch {1=>"VarRooms",3=>"Traps",4=>"Doors",5=>"Spells",_=>""};
            var id=BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]);
            return group.Length==0 || !LegacyCompilerRules.Elements(group).Any(item=>item.Variable==id);
        }
        return kind is not (1 or 0x42 or 0x4D) && !MapVariables.Definitions.Any(item=>item.ID==kind);
    }

    internal static void ValidateRetainedNativeVariables(byte[] retained, ImmutableArray<ProjectConversionSourceFile> files)
    {
        if(retained.Length>160000 || retained.Length%16!=0)throw new InvalidDataException("Invalid retained native variable size.");
        if(retained.Length==0)return;
        // The manifest cannot invent records: each occurrence must exist in
        // a bounded native variable collection in the hashed original bundle.
        var available=new Dictionary<string,int>(StringComparer.Ordinal);
        foreach(var file in files)
        {
            var bytes=file.Bytes.AsSpan();
            if(bytes.Length<56 || BinaryPrimitives.ReadUInt32LittleEndian(bytes)!=0xE6)continue;
            var count=BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]);
            if(count>1000000 || 56L+count*16L>bytes.Length)throw new InvalidDataException("Truncated archived variable collection.");
            for(var i=0;i<count;i++)
            {
                var value=bytes.Slice(56+checked((int)i)*16,16);
                if(!IsUnrepresentedNativeVariable(value))continue;
                var key=Convert.ToHexString(value);
                available[key]=available.GetValueOrDefault(key)+1;
            }
        }
        for(var i=0;i<retained.Length;i+=16)
        {
            var record=retained.AsSpan(i,16);var key=Convert.ToHexString(record);
            if(!IsUnrepresentedNativeVariable(record) || available.GetValueOrDefault(key)==0)
                throw new InvalidDataException("Retained variable is not an unrepresented record in the source bundle.");
            available[key]--;
        }
    }

    internal void BindNativeSourceArchives(IEnumerable<EditorSourceArchive> archives)
    {
        var candidates=archives.Where(archive=>!archive.RetainedVariables.IsEmpty || HasNativePayloadSource(archive)).DistinctBy(archive=>archive.ContentHash).ToArray();
        if(candidates.Length>1)throw new InvalidDataException("Multiple native variable companions are ambiguous; refusing to choose source data.");
        nativePayloadSource=candidates.SingleOrDefault();
        nativeRetainedVariables=nativePayloadSource?.RetainedVariables.ToArray() ?? [];
        if(IsLegacyProject){_=EffectiveRetainedLevelVariables();_=ReadOwnedLevelModifiers();}
    }

    private void CompileRetainedNativeVariables(NativeMapDocument output)
    {
        var effective=EffectiveRetainedLevelVariables();
        for(var offset=0;offset<effective.Length;offset+=16)
        {
            var bytes=effective.AsSpan(offset,16);
            if(!IsUnrepresentedNativeVariable(bytes))throw new InvalidDataException("Retained variable now collides with an editable DKM field.");
            output.InsertRecord(output.CollectionFile(0xE6),bytes.ToArray());
        }
        _=ReadOwnedLevelModifiers();
        for(var offset=16;offset<LevelModifierOwnedFile.Length;offset+=16)
            output.InsertRecord(output.CollectionFile(0xE6),LevelModifierOwnedFile.Data.Slice(offset,16).ToArray());
    }
}
