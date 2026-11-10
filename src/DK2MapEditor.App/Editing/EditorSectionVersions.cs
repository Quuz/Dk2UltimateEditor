using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;
namespace DK2MapEditor.App.Editing;
public sealed record EditorSectionVersionSpec(string Key,uint Signature,uint Current);
public sealed record EditorSectionVersion(EditorSectionVersionSpec Section,uint? Input,uint? Working,string State);
public static class EditorSectionVersions
{
    public static IReadOnlyList<EditorSectionVersionSpec> Sections {get;}=Array.AsReadOnly(new[]{
        new EditorSectionVersionSpec("Statistics",0x5354534B,1),new("Map",0x50414D4B,1),new("Things",0x5347544B,6),
        new("MapInfo",0x464E494B,7),new("Players",0x594C504B,3),new("Regions",0x4345524B,1),
        new("Bands",0x48544D4B,5),new("Scenarios",0x4752544B,3),new("Globals",0x4C42474B,1)});
    public static IReadOnlyList<EditorSectionVersion> Capture(NativeMapDocument document)
    {
        if(!document.IsLegacyProject)return Sections.Select(s=>new EditorSectionVersion(s,null,null,"NotApplicable")).ToArray();
        var inputs=InputVersions(document.MainFile.Data.Span);
        return Sections.Select(section=>
        {
            var candidates=document.Files.Where(f=>f.Length>=4&&f.Signature==section.Signature).ToArray();
            var source=inputs is not null&&inputs.TryGetValue(section.Signature,out var versions)?versions:[];
            var duplicate=candidates.Length>1||source.Count>1;
            uint? input=source.Count==1?source[0]:null;uint? working=candidates.Length==1&&candidates[0].Length>=12?UInt(candidates[0].Data.Span,4):null;
            var malformed=candidates.Any(f=>f.Length<12||UInt(f.Data.Span,8)!=(uint)f.Length-12);
            var state=inputs is null||malformed?"Invalid":duplicate?"Ambiguous":working is null?"Absent":working==0?"Invalid":
                working>section.Current?"Newer":working<section.Current?"Older":input is null?"WorkingOnly":"Current";
            return new EditorSectionVersion(section,input,working,state);
        }).ToArray();
    }
    public static IReadOnlyDictionary<uint,List<uint>>? InputVersions(ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length<12||UInt(bytes,0) is not (LegacySectionFile.MapFileSignature or LegacySectionFile.AreaFileSignature)||UInt(bytes,8)!=(uint)bytes.Length-12)return null;
        var result=new Dictionary<uint,List<uint>>();var offset=12;
        while(offset<bytes.Length)
        {
            if(bytes.Length-offset<12)return null;var signature=UInt(bytes,offset);var size=UInt(bytes,offset+8);
            if(size>(uint)(bytes.Length-offset-12))return null;
            if(!result.TryGetValue(signature,out var versions))result.Add(signature,versions=[]);
            versions.Add(UInt(bytes,offset+4));offset+=12+(int)size;
        }
        return result;
    }
    private static uint UInt(ReadOnlySpan<byte> bytes,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
}
