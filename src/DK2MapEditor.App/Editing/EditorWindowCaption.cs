using System.Buffers.Binary;
using System.Text;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source UseFilename=false: read only verified metadata, without Records(), drafts or graph fingerprints.</summary>
public static class EditorWindowCaption
{
    public static string? ReadMapName(NativeMapDocument document)
    {
        if(!document.IsLegacyProject)return ReadNativeMapName(document.MainFile.Data.Span);
        var info=ProjectInfo(document);return info is null?null:ReadProjectMapName(info.Data.Span);
    }
    public static string? ReadAuthor(NativeMapDocument document)
    {
        if(!document.IsLegacyProject)return ReadNativeMapAuthor(document.MainFile.Data.Span);
        var info=ProjectInfo(document);return info is null?null:ReadProjectMapAuthor(info.Data.Span);
    }
    private static NativeFile? ProjectInfo(NativeMapDocument document)
    {
        NativeFile? info=null;
        foreach(var file in document.Files)
            if(file.Length>=4&&file.Signature==NativeMapDocument.LegacyInfo)
            {if(info is not null)return null;info=file;}
        return info;
    }
    public static string? ReadNativeMapName(ReadOnlySpan<byte> bytes)=>ReadNativeText(bytes,"wstrMainName");
    public static string? ReadNativeMapAuthor(ReadOnlySpan<byte> bytes)=>ReadNativeText(bytes,"wstrMainAuthor");
    private static string? ReadNativeText(ReadOnlySpan<byte> bytes,string name)
    {
        if(bytes.Length<56||UInt32(bytes,0)!=0xDC||UInt32(bytes,4)!=4)return null;
        var field=LegacySchemas.Field("GAMEINFO",name);
        if(field.Kind!=NativeValueKind.Unicode||field.Size!=128||field.BitMask!=0
            ||(long)field.Offset+field.Size>56L+UInt32(bytes,52)
            ||(long)field.Offset+field.Size>bytes.Length)return null;
        return field.Read(bytes);
    }
    public static string? ReadProjectMapName(ReadOnlySpan<byte> bytes)=>ReadProjectText(bytes,"Name");
    public static string? ReadProjectMapAuthor(ReadOnlySpan<byte> bytes)=>ReadProjectText(bytes,"Author");
    private static string? ReadProjectText(ReadOnlySpan<byte> bytes,string name)
    {
        if(bytes.Length<12||UInt32(bytes,0)!=NativeMapDocument.LegacyInfo)return null;
        var structure=UInt32(bytes,4) switch
        {
            1=>"MAPPROPERTIESV1",2 or 3=>"MAPPROPERTIESV2",4=>"MAPPROPERTIESV4",
            5=>"MAPPROPERTIESV5",6=>"MAPPROPERTIESV6",7=>"MAPPROPERTIES",_=>null
        };
        if(structure is null||bytes.Length!=12L+LegacyProjectSchemas.Size(structure)
            ||UInt32(bytes,8)!=bytes.Length-12)return null;
        var field=LegacyProjectSchemas.Fields(structure).SingleOrDefault(item=>item.Name==name);
        if(field is null||field.Kind!=NativeValueKind.WesternAnsi||field.Size!=64||field.BitMask!=0
            ||(long)field.Offset+field.Size>bytes.Length-12)return null;
        try{return field.Read(bytes[12..]);}
        catch(DecoderFallbackException){return null;}
    }
    private static uint UInt32(ReadOnlySpan<byte> bytes,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
}
