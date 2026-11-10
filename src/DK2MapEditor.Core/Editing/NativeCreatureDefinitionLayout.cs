namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private static bool VerifiedDefinitionHeader(NativeFile file,int header,uint signature,int count,int recordSize)
    {
        var payload=(long)count*recordSize;
        return header>=0 && count>0 && recordSize>0 && header+56L+payload<=file.Length
            && file.UInt32(header)==signature && file.UInt32(header+4)==4
            && file.UInt32(header+8)==56L+payload && file.UInt32(header+12)==signature+1
            && file.UInt32(header+16)==28 && file.UInt32(header+20)==count
            && file.UInt32(header+48)==signature+2 && file.UInt32(header+52)==payload;
    }
    // Original compiler offsets remain in the common 5449-byte prefix. Real
    // game Globals.kld records append 88 bytes; those bytes stay opaque. The
    // 44-row historical catalogue is the same ID/order prefix of the 46 rows.
    // See docs/NATIVE_CREATURE_VARIANT_SCOPE.md and the 500-section audit.
    private static bool VerifiedCreatureCatalogue(NativeFile file, int header, int count, int recordSize)
    {
        if (count is not (44 or 46) || recordSize is not (5449 or 5537)
            || !VerifiedDefinitionHeader(file,header,0xAA,count,recordSize)) return false;
        for (var index = 0; index < count; index++)
            if (file.Data.Span[header + 56 + index * recordSize + 0xF4B] != index + 1) return false;
        return true;
    }

    private static bool VerifiedDefinitionCatalogue(LegacyGlobalSchemas.Definition family, IReadOnlyList<NativeRecord> records)
    {
        var expectedCount = LegacyCompilerRules.DefinitionOrder(family.Group).Count;
        if (records.Count == 0 || family.Signature != 0xAA && records.Count != expectedCount) return false;
        var first = records[0]; var header = first.Offset - 56; var size = first.Length;
        if (!family.SupportsRecordSize(size)
            || !VerifiedDefinitionHeader(first.File,header,family.Signature,records.Count,size)) return false;
        if (family.Signature == 0xAA && !VerifiedCreatureCatalogue(first.File, header, records.Count, size)) return false;
        return records.Select((record, index) => record.File == first.File && record.Offset == header + 56 + index * size
            && record.Length == size && record.Fields.Count == family.Fields.Count).All(value => value);
    }
}
