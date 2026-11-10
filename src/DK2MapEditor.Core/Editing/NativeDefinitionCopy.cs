namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Retail Globals.kld contains several complete native sections.
    /// Select the requested physical section, rather than matching only its first header.</summary>
    private static (string Source, byte[] Bytes)? CopyNativeDefinition(NativeMapDocument basis, uint signature)
    {
        foreach (var file in basis.Files.Reverse())
        {
            if (signature == 0xE6)
            {
                if (file.Signature == signature && !file.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return (file.SourcePath, file.Data.ToArray());
                continue;
            }
            if (!NativeDefinitionSignature(file.Signature)) continue;
            var scan=ScanNativeDefinitionSections(file);
            var section=scan.Sections.FirstOrDefault(section=>section.Signature==signature);
            if(section is null)continue;
            if(file.UInt32(section.Offset+4)!=4)
                throw new InvalidDataException("Cannot extract an unsupported native definition version: "+file.SourcePath);
            // Keep unmodelled bytes adjacent to their physical preceding chunk.
            // Official packing separately refuses to relocate such a chunk.
            var end=scan.Sections.Where(next=>next.Offset>section.Offset&&NativeDefinitionSignature(next.Signature)).Select(next=>next.Offset).DefaultIfEmpty(file.Length).First();
            return (file.SourcePath,file.Data.Slice(section.Offset,end-section.Offset).ToArray());
        }
        return null;
    }
}
