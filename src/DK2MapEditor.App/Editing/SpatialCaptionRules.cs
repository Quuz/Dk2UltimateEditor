using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

internal static class SpatialCaptionRules
{
    internal static string? ForRecord(NativeRecord record,NativeField field)
    {
        if(!record.Fields.Contains(field)||record.File.Length<8||record.Offset<0||record.Length<0||record.Offset>record.File.Length-record.Length)return null;
        if(EffectGenerators.Supported(record))
        {
            if(record.Category=="GAMEEFFECT"&&record.File.Signature!=0xBE)return null;
            if(record.Category=="Project.MAPRECTEX"&&(record.File.Signature!=NativeMapDocument.LegacyAreas||BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[4..])!=1))return null;
            foreach(var name in EffectGenerators.Fields)if(field.Name==EffectGenerators.StorageField(record,name))return "Editor.EffectGenerator."+name;
        }
        if(record.Category=="GAMEAREA"&&record.File.Signature==0xBE&&record.Length==LegacySchemas.Size("GAMEAREA"))
            return field.Name switch{"wAreaWaitDelay"=>"RegionProperties.PauseSeconds","bAreaNextID"=>"Field.NextID",_=>null};
        if(record.Category=="Project.MAPRECTEX"&&record.File.Signature==NativeMapDocument.LegacyAreas&&record.Length==LegacyProjectSchemas.Size("MAPRECTEX")&&BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[4..])==1
            &&record.Fields.FirstOrDefault(f=>f.Name=="node.type") is {} kind&&record.Read(kind)=="0")
            return field.Name switch{"waitdelay"=>"RegionProperties.PauseSeconds","nextid"=>"Field.NextID",_=>null};
        return null;
    }
}
