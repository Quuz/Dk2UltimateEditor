using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

/// <summary>Official variable writer 004aa6e0, loops 004ab0be..004ab150:
/// signed SHORT to DWORD; zero is emitted as 100. This is a byte projection,
/// not authority to publish a shared installation file or a DKM source archive.</summary>
public static class CreatureLevelModifierWriter
{
    public static uint EncodeEditedValue(int value)
    {
        if(value<CreatureLevelModifierControls.Minimum||value>CreatureLevelModifierControls.Maximum)
            throw new ArgumentOutOfRangeException(nameof(value),"Official level percentages range from -10000 to 10000.");
        return unchecked((uint)(value==0?100:value));
    }

    public static byte[] CreateRecord(CreatureLevelModifierKey key,int value)
    {
        VerifyKey(key);
        var bytes=new byte[16];
        U(bytes,0,key.RecordType);U(bytes,4,key.Statistic);U(bytes,8,EncodeEditedValue(value));U(bytes,12,key.Level);
        return bytes;
    }

    /// <summary>Patch only the final existing occurrence of each edited visible
    /// key. Validate the whole request before producing output. Earlier
    /// duplicates, hidden/unknown rows, headers and opaque trailers are retained.
    /// No-op input preserves noncanonical original DWORD bits. Missing keys
    /// require a separately verified effective-source/override insertion plan.</summary>
    public static byte[] PatchCollection(ReadOnlySpan<byte> source,IEnumerable<CreatureLevelModifierChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var selected=changes.ToArray();
        var rows=CreatureLevelModifiers.ReadCollection(source);
        if(selected.Any(change=>change is null))throw new ArgumentException("A level change cannot be null.",nameof(changes));
        if(selected.Select(change=>change.Key).Distinct().Count()!=selected.Length)
            throw new ArgumentException("Select each level modifier key once.",nameof(changes));
        var last=rows.GroupBy(row=>new CreatureLevelModifierKey(row.RecordType,row.Statistic,row.Level))
            .ToDictionary(group=>group.Key,group=>group.Last());
        var writes=new List<(int Offset,uint Bits)>();
        foreach(var change in selected)
        {
            VerifyKey(change.Key);
            var encoded=EncodeEditedValue(change.Value);
            if(!last.TryGetValue(change.Key,out var row))throw new InvalidDataException("The level modifier key is absent from this collection.");
            if(row.PercentBits!=change.OriginalBits)throw new InvalidOperationException("Refresh the level draft after source values change.");
            var before=CreatureLevelModifierControls.DisplayValue(row.PercentBits);
            if(change.Value==before||CreatureLevelModifierControls.DisplayValue(encoded)==before)continue;
            writes.Add((row.Offset+8,encoded));
        }
        var output=source.ToArray();
        foreach(var write in writes)U(output,write.Offset,write.Bits);
        return output;
    }

    private static void VerifyKey(CreatureLevelModifierKey key)
    {
        if(!CreatureLevelModifierControls.HasStatistic(key.Statistic)
            ||!(key.RecordType==0x41&&key.Level is >=2 and <=10||key.RecordType==0xE0&&key.Level==10))
            throw new ArgumentException("Choose a visible official level or First Person key.",nameof(key));
    }
    private static void U(byte[] bytes,int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset,4),value);
}
