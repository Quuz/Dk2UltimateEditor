using System.Security.Cryptography;

namespace DK2MapEditor.Core.Editing;

/// <summary>Independent bounded source for catalogue-copy export. The source
/// path is a label; captured archive paths are never opened implicitly.</summary>
public sealed class CreatureLevelModifierCatalogue
{
    public const int MaximumBytes=64*1024*1024;
    private readonly byte[] bytes;
    private readonly IReadOnlyList<CreatureLevelModifierRecord> rows;
    private CreatureLevelModifierCatalogue(string name,byte[] bytes)
    {
        SourceName=name;this.bytes=bytes;rows=CreatureLevelModifiers.ReadCollection(bytes);
        SHA256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        try{using var draft=new CreatureLevelModifierDraft(rows);HasCompleteVisibleTable=true;}
        catch(InvalidDataException){HasCompleteVisibleTable=false;}
    }
    public string SourceName {get;}
    public string SHA256 {get;}
    public bool HasCompleteVisibleTable {get;}
    public IReadOnlyList<CreatureLevelModifierRecord> Records=>rows;
    public byte[] CopyBytes()=>bytes.ToArray();
    public static CreatureLevelModifierCatalogue Capture(string sourceName,ReadOnlySpan<byte> bytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        if(bytes.Length>MaximumBytes)throw new InvalidDataException("The variable catalogue exceeds its safe size limit.");
        return new(sourceName,bytes.ToArray());
    }
    public static CreatureLevelModifierCatalogue ReadFile(string path)
    {
        var full=Path.GetFullPath(path);
        using var stream=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(stream.Length<56||stream.Length>MaximumBytes)throw new InvalidDataException("Invalid variable catalogue size.");
        var bytes=new byte[(int)stream.Length];stream.ReadExactly(bytes);
        if(stream.ReadByte()!=-1)throw new InvalidDataException("The variable catalogue changed while reading.");
        return Capture(full,bytes);
    }
}

public sealed partial class NativeMapDocument
{
    public CreatureLevelModifierCatalogue? CaptureCreatureLevelModifierCatalogue()
    {
        if(!IsLegacyProject)
        {
            var candidates=files.Where(file=>file.Signature==0xE6&&Path.GetFileName(file.ExportPath).Equals("GlobalVariables.kwd",StringComparison.OrdinalIgnoreCase)).ToArray();
            if(candidates.Length>1)throw new InvalidDataException("Ambiguous shared variable catalogues.");
            return candidates.Length==0?null:CreatureLevelModifierCatalogue.Capture(candidates[0].SourcePath,candidates[0].Data.Span);
        }
        var archived=nativePayloadSource?.SourceFiles.Where(file=>Path.GetFileName(file.ExportPath).Equals("GlobalVariables.kwd",StringComparison.OrdinalIgnoreCase)).ToArray()??[];
        if(archived.Length>1)throw new InvalidDataException("Ambiguous archived variable catalogues.");
        return archived.Length==0?null:CreatureLevelModifierCatalogue.Capture(archived[0].SourcePath,archived[0].Bytes.AsSpan());
    }
}
