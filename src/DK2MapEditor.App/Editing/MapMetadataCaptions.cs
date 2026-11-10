using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

internal sealed record MapMetadataCaption(string Key,int? Ordinal=null)
{
    internal string Render(Func<string,string> text)=>Ordinal is {} ordinal?string.Format(CultureInfo.CurrentCulture,text(Key),ordinal):text(Key);
}

/// <summary>Map-level labels shared by its typed dialogs and raw properties.
/// Target game version, speech/text IDs and creature appearance replacements
/// are separate contexts; option bytes remain raw flags.</summary>
internal static class MapMetadataCaptions
{
    internal static MapMetadataCaption? ForSemantic(string name)
    {
        if(MapGeneralSettings.Fields.Contains(name))return new("MapProperties."+name);
        if(MapLevelOptions.Scalars.Any(row=>row.Field==name)||MapLevelOptions.RewardFields.Contains(name))return new("LevelOptions.Field."+name);
        if(!MapSpeechText.Fields.Contains(name))return null;
        foreach(var prefix in new[]{"TextIntroCreatureID","TextIntroTextID"})
            if(name.StartsWith(prefix,StringComparison.Ordinal)&&int.TryParse(name[prefix.Length..],NumberStyles.None,CultureInfo.InvariantCulture,out var index)&&index is >=0 and <8)
                return new(prefix=="TextIntroCreatureID"?"SpeechText.IntroCreature":"SpeechText.IntroText",index+1);
        return new("SpeechText.Field."+name);
    }
    internal static MapMetadataCaption? ForRecord(NativeRecord record,NativeField field)
    {
        if(field.BitMask!=0||!record.Fields.Contains(field)||record.File.Length<8||record.Offset<0||record.Length<0||record.Offset>record.File.Length-record.Length)return null;
        var version=BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[4..]);
        if(record.Category=="Map"&&record.File.Signature==0xDC&&version==4&&record.Offset==0&&LegacySchemas.Fields("GAMEINFO").Contains(field))
        {
            var source=LegacyCompilerRules.Metadata.FirstOrDefault(mapping=>mapping.Target==field.Name)?.Source;
            // Native target version lives in working session state, not GAMEINFO.
            return source is null or "Version"?null:ForSemantic(source);
        }
        if(record.File.Signature!=NativeMapDocument.LegacyInfo)return null;
        var structure=version switch{1=>"MAPPROPERTIESV1",2 or 3=>"MAPPROPERTIESV2",4=>"MAPPROPERTIESV4",5=>"MAPPROPERTIESV5",6=>"MAPPROPERTIESV6",7=>"MAPPROPERTIES",_=>null};
        return structure is not null&&record.Category=="Project."+structure&&record.Length==LegacyProjectSchemas.Size(structure)&&LegacyProjectSchemas.Fields(structure).Contains(field)?ForSemantic(field.Name):null;
    }
}
