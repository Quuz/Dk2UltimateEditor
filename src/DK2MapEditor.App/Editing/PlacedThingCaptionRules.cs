using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source MAPTHING union meanings, scoped to its actual kind/version.
/// This only labels stored numbers; it never normalizes or converts them.</summary>
internal static class PlacedThingCaptionRules
{
    internal static string DataKey(PlacedThingKind kind,uint? id)=>kind switch
    {
        PlacedThingKind.Creature=>"Field.Level",
        PlacedThingKind.Door=>"PlacedThing.DoorState",
        PlacedThingKind.Trap=>"Field.Shots",
        PlacedThingKind.Object when id is 1 or 2 or 3=>"Field.GoldValue",
        PlacedThingKind.Object when id==4=>"Field.Spell",
        _=>"Field.DataAll"
    };
    internal static string? ForRecord(NativeRecord record,NativeField field)
    {
        if(!record.Fields.Contains(field)||record.File.Length<8||record.Offset<0||record.Length<0||record.Offset>record.File.Length-record.Length)return null;
        uint? Read(string name){var f=record.Fields.SingleOrDefault(f=>f.Name==name);return f is not null&&uint.TryParse(record.Read(f),NumberStyles.Integer,CultureInfo.InvariantCulture,out var value)?value:null;}
        if(record.File.Signature==NativeMapDocument.LegacyThings)
        {
            var version=BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[4..]);
            var structure=version switch{1=>"MAPTHINGV1",2 or 3=>"MAPTHINGV2",4=>"MAPTHINGV4",5=>"MAPTHINGV5",6=>"MAPTHING",_=>null};
            if(structure is null||record.Category!="Project."+structure||record.Length!=LegacyProjectSchemas.Size(structure)||Read("node.type") is not {} type||type>4)return null;
            if(field.Name=="dataAll")
            {
                if(type==1&&version<=4)return "Field.Locked";
                if(type==2&&version<=4)return "Field.DataAll"; // Old union has no shots member.
                return DataKey((PlacedThingKind)type,Read("id"));
            }
            return field.Name switch
            {
                "herosoptions"=>"Batch.HeroOptions","creaturesoptions"=>"Field.Behavior",
                "options" when version<=3=>"Batch.HeroOptions",_=>null
            };
        }
        if(record.File.Signature!=0xBE)return null;
        if(record.Category.StartsWith("BandHero ",StringComparison.Ordinal)&&record.Length==32&&record.Fields.SequenceEqual(LegacySchemas.Fields("GAMEBANDHERO")))
            return field.Name switch{"bGlobalPlayer"=>"Field.Enemy","bHeroOptions"=>"Batch.HeroOptions",_=>null};
        var header=record.Category switch{"GAMEOBJECT"=>0xC2u,"GAMETRAP"=>0xC3u,"GAMEDOOR"=>0xC4u,"GAMENEUTRAL"=>0xC6u,"GAMEHERO"=>0xC7u,"GAMEKEEPER"=>0xC8u,_=>0u};
        // The members named *Version in Game.h store payload byte counts here.
        if(header==0||record.Length!=LegacySchemas.Size(record.Category)||record.Length<8||BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[record.Offset..])!=header||BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[(record.Offset+4)..])!=record.Length-8)return null;
        return field.Name switch
        {
            "bDoorLocked" when record.Category=="GAMEDOOR"=>"PlacedThing.DoorState",
            "bGlobalPlayer" when record.Category=="GAMEHERO"=>"Field.Enemy",
            "bGlobalPlayer"=>"Field.Owner",
            "bHeroOptions"=>"Batch.HeroOptions",_=>null
        };
    }
}
