using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

internal sealed record DefinitionCaption(string Key,string? Argument=null,string? PrefixKey=null)
{
    internal string Render(Func<string,string> text)
    {
        var caption=Argument is null?text(Key):string.Format(CultureInfo.CurrentCulture,text(Key),Argument);
        return PrefixKey is null?caption:text(PrefixKey)+" · "+caption;
    }
}

/// <summary>One contextual caption route for the definition table and raw inspector.
/// Only confirmed fields get an override; unknown data keeps its generic label.</summary>
internal static class DefinitionCaptionRules
{
    private static readonly HashSet<string> attributes=CreatureGlobalsSession.Sections.Single(s=>s.Name=="Attributes").Fields.ToHashSet(StringComparer.Ordinal);
    internal static string? StaticKey(string family,string field)=>(family,field) switch
    {
        ("Terrains","Damage")=>"TerrainProperties.CreatureHealthChange",
        ("Terrains","MaxManaGain")=>"TerrainProperties.MaximumMana",
        ("Rooms","Cost")=>"RoomProperties.CostPerTile",
        ("Doors" or "Traps","DeathEffect")=>"GlobalElements.DestructionEffect",
        ("Creatures",_) when attributes.Contains(field)=>"CreatureAttributes."+field,
        ("CrSpells","Creature")=>"CreatureSpells.CreatureSpell",
        _=>null
    };
    internal static DefinitionCaption? For(string family,string field,Func<string,string?>? read=null)
    {
        if(StaticKey(family,field) is {} key)return new(key);
        if(family=="Creatures")
        {
            var match=Regex.Match(field,@"^Res([1-4])(Rate)?$");
            if(match.Success)return new("Editor.Definitions.Options."+(match.Groups[2].Success?"ResistanceRate":"ResistanceType"),match.Groups[1].Value);
            match=Regex.Match(field,@"^(Melee|Special)([12])$");
            if(match.Success)return new("Editor.Definitions.Options."+(match.Groups[1].Value=="Melee"?"MeleeSwipe":"SpecialSwipe"),match.Groups[2].Value);
            if(field=="Spell")return new("Editor.Definitions.Options.SpellSwipe");
        }
        if(family=="Traps"&&field=="TriggerData")return read?.Invoke("TriggerType") switch
        {"1"=>new("TrapProperties.TriggerAreaOfEffect"),"2"=>new("TrapProperties.TriggerRequiredPressure"),_=>null};
        if(family is "Traps" or "Spells" or "CrSpells")
        {
            var match=Regex.Match(field,@"^(Upg)?ShotData([12])$");
            if(!match.Success||match.Groups[1].Success&&family!="Spells")return null;
            var upgrade=match.Groups[1].Success;var type=upgrade?"UpgShotType":family=="CrSpells"?"Shot":"ShotType";
            if(read?.Invoke(type) is not {} value||GlobalProjectiles.Find(value) is not {Form:>=0 and <=4} option)return null;
            var second=match.Groups[2].Value=="2";
            if(second&&option.Form==1)return null;
            var suffix=second?option.Form switch{2=>"Count",3=>"Level",4=>"Gold",_=>"Label2."+option.Label2}
                :option.Form switch{2 or 3=>"Creature",4=>"Object",_=>"Label1."+option.Label1};
            return new("Editor.Definitions.Projectile."+suffix,PrefixKey:"Editor.Definitions.Projectile."+(upgrade?"Upgrade":"Shot"));
        }
        return null;
    }
    internal static string? Family(NativeRecord record)
    {
        if(record.File.Length<8||record.Offset<0||record.Length<0||record.Offset>record.File.Length-record.Length)return null;
        var native=LegacyGlobalSchemas.Find(record.File.Signature);
        if(native is not null&&record.Category=="Definition."+native.Group&&native.SupportsRecordSize(record.Length))return native.Group;
        if(record.File.Signature!=NativeMapDocument.LegacyGlobals||BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[4..])!=1)return null;
        foreach(var (family,structure) in new[]{("Terrains","GLOBALTERRAIN"),("Rooms","GLOBALROOM"),("Doors","GLOBALDOOR"),("Traps","GLOBALTRAP"),("Spells","GLOBALSPELL"),("Creatures","GLOBALCREATURE"),("CrSpells","GLOBALCRSPELLS")})
            if(record.Category=="Project."+structure&&record.Length==LegacyProjectSchemas.Size(structure))return family;
        return null;
    }
    internal static DefinitionCaption? ForRecord(NativeRecord record,NativeField field)
    {
        if(!record.Fields.Contains(field)||Family(record) is not {} family)return null;
        string? Read(string name){var item=record.Fields.SingleOrDefault(f=>f.Name==name);return item is null?null:record.Read(item);}
        return For(family,field.Name,Read);
    }
}
