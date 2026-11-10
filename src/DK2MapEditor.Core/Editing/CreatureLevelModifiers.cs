using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public enum CreatureLevelModifierStorage { SharedDefaults, MapVariables, OtherNativeCollection, RetainedNativeSource }

/// <summary>A literal occurrence, not an effective value or authority to edit.
/// Duplicate/unknown keys remain visible and ordered. PercentBits is never
/// clamped or converted to signed/fixed-point units.</summary>
public sealed record CreatureLevelModifierRecord(uint RecordType,uint Statistic,uint PercentBits,uint Level,int Offset)
{
    public bool IsDocumentedLevelKey=>RecordType==0x41&&Statistic<32&&Level is >=2 and <=10;
    public bool IsObservedPossessionKey=>RecordType==0xE0&&Statistic<32&&Level==10;
}
public sealed record CreatureLevelModifierSource(CreatureLevelModifierStorage Storage,string SourcePath,
    IReadOnlyList<CreatureLevelModifierRecord> Records);

/// <summary>Bounded read-only decoder for official editor dialog238's level
/// percentages. The C editor does not expose these native variable records.</summary>
public static class CreatureLevelModifiers
{
    public static IReadOnlyList<CreatureLevelModifierRecord> ReadCollection(ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length<56||N(bytes,0)!=0xE6||N(bytes,4)!=4||N(bytes,12)!=0xE7||N(bytes,16)!=28||N(bytes,48)!=0xE8)
            throw new InvalidDataException("A verified version4 native variable collection is required.");
        var count=N(bytes,20);var length=56L+count*16L;
        if(count>1000000||length>bytes.Length||N(bytes,8)!=length||N(bytes,52)!=count*16L)
            throw new InvalidDataException("Truncated or inconsistent native variable collection.");
        // Bytes after the declared section remain opaque; do not scan them as
        // additional records. GlobalVariables.kwd can contain such an extension.
        return ReadRecords(bytes.Slice(56,checked((int)count*16)),56);
    }
    private static uint N(ReadOnlySpan<byte> bytes,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset,4));
    public static IReadOnlyList<CreatureLevelModifierRecord> ReadRetained(ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length>160000||bytes.Length%16!=0)throw new InvalidDataException("Invalid retained native variable size.");
        return ReadRecords(bytes,0);
    }
    private static IReadOnlyList<CreatureLevelModifierRecord> ReadRecords(ReadOnlySpan<byte> bytes,int origin)
    {
        var result=new List<CreatureLevelModifierRecord>();
        for(var offset=0;offset<bytes.Length;offset+=16)
        {
            var row=bytes.Slice(offset,16);var type=BinaryPrimitives.ReadUInt32LittleEndian(row);
            if(type is not (0x41 or 0xE0))continue;
            result.Add(new(type,BinaryPrimitives.ReadUInt32LittleEndian(row[4..]),BinaryPrimitives.ReadUInt32LittleEndian(row[8..]),
                BinaryPrimitives.ReadUInt32LittleEndian(row[12..]),checked(origin+offset)));
        }
        return result.AsReadOnly();
    }
}

public sealed partial class NativeMapDocument
{
    /// <summary>Read-only source inventory. No last-wins merge, guessed
    /// defaults, allocation, dirty state or change to generic record schemas.</summary>
    public IReadOnlyList<CreatureLevelModifierSource> ReadCreatureLevelModifiers()
    {
        var result=new List<CreatureLevelModifierSource>();
        foreach(var file in files.Where(f=>f.Signature==0xE6))
        {
            var rows=CreatureLevelModifiers.ReadCollection(file.Data.Span);
            var storage=Path.GetFileName(file.ExportPath).Equals("GlobalVariables.kwd",StringComparison.OrdinalIgnoreCase)
                ?CreatureLevelModifierStorage.SharedDefaults:ReferenceEquals(file,MapVariableFile)?CreatureLevelModifierStorage.MapVariables:CreatureLevelModifierStorage.OtherNativeCollection;
            result.Add(new(storage,file.SourcePath,rows));
        }
        if(IsLegacyProject&&nativeRetainedVariables.Length!=0)
            result.Add(new(CreatureLevelModifierStorage.RetainedNativeSource,"native-source retained variables",CreatureLevelModifiers.ReadRetained(nativeRetainedVariables)));
        return result.AsReadOnly();
    }
}
