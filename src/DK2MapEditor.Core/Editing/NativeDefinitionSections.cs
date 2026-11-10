using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private sealed record NativeDefinitionSection(int Offset,int Length,uint Signature);
    private sealed record NativeDefinitionScan(IReadOnlyList<NativeDefinitionSection> Sections,int TailOffset);
    internal static bool NativeDefinitionSignature(uint signature) => LegacyGlobalSchemas.Find(signature) is not null
        || signature is 0xF0 or 0x104 or 0x10E or 0xFA;

    // Walk only adjacent framed sections. Once opaque bytes start, never search
    // inside them for a later signature that happens to resemble a header.
    private static NativeDefinitionScan ScanNativeDefinitionSections(NativeFile file)
    {
        var sections=new List<NativeDefinitionSection>();var offset=0;
        while(offset<=file.Length-56)
        {
            var signature=file.UInt32(offset);var version=file.UInt32(offset+4);var length=file.UInt32(offset+8);
            var known=NativeDefinitionSignature(signature);
            var info=file.UInt32(offset+12)==unchecked(signature+1);var define=file.UInt32(offset+48)==unchecked(signature+2);
            if(offset!=0 && (known ? version==0 || !info&&!define : version!=4 || !info || !define || file.UInt32(offset+16)!=28))break;
            if(length<56 || length>file.Length-offset)
            {
                if(offset==0 || known)throw new InvalidDataException("Invalid packed native definition section: "+file.SourcePath);
                break;
            }
            sections.Add(new(offset,(int)length,signature));offset+=(int)length;
        }
        if(sections.Count==0)throw new InvalidDataException("Truncated native definition section: "+file.SourcePath);
        return new(sections.AsReadOnly(),offset);
    }
    private static int OpaqueNativeDefinitionBytes(NativeFile file,NativeDefinitionScan scan)
        =>file.Length-scan.TailOffset+scan.Sections.Where(section=>!NativeDefinitionSignature(section.Signature)||file.UInt32(section.Offset+4)!=4).Sum(section=>section.Length);

    // File preview uses the same static parser as editable loading, without
    // creating a working document, activity timer or resolving other files.
    internal static IReadOnlyList<MapDiagnostic> InspectNativeDefinitions(NativeFile file)
    {
        var records = new List<NativeRecord>(); AppendDefinitionRecords(file, records);
        var result = records.Where(record => record.Fields.Count == 0).Select(record =>
            new MapDiagnostic(DiagnosticSeverity.Warning, "DK2N008", "This definition variant is preserved but has no verified editing schema.", file.SourcePath)).ToList();
        var opaque = OpaqueNativeDefinitionBytes(file, ScanNativeDefinitionSections(file));
        if (opaque != 0) result.Add(new(DiagnosticSeverity.Warning, "DK2N048",
            $"Native definition sections or trailing bytes without a verified interpretation are retained ({opaque} bytes); review conversion losses before recompilation.", file.SourcePath));
        return result;
    }
}
