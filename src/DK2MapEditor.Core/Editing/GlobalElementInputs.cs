using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>The33 explicit EN_CHANGE/UDN controls on the five source element
/// pages. Clamp text formatting follows each callback, not its storage units.</summary>
public static class GlobalElementInputs
{
    private sealed record Rule(bool Signed,int Minimum,int Maximum,bool DecimalClamp=false);
    private static readonly IReadOnlyDictionary<(string Family,string Field),Rule> rules=CreateRules();
    private static IReadOnlyDictionary<(string,string),Rule> CreateRules()
    {
        var result=new Dictionary<(string,string),Rule>();
        void Add(string family,int maximum,params string[] fields)
        {foreach(var field in fields)result.Add((family,field),new(false,0,maximum));}
        result.Add(("Terrains","Damage"),new(true,-32768,32767));
        Add("Terrains",65535,"GoldValue","StartingHealth","MaxHealth","ManaGain","MaxManaGain");
        Add("Terrains",255,"WibbleH","WibbleV");result.Add(("Terrains","TerrainLightH"),new(false,0,16000,true));
        Add("Rooms",65535,"Cost");Add("Rooms",255,"TorchIntensity");Add("Rooms",8,"TorchLightH");Add("Rooms",3,"TorchLightRad");
        Add("Doors",65535,"Cost","BuildPoints","Health","HealthGain","ManaCost");
        Add("Traps",16000,"IdleEffectDelay","ChargeTime","RechargeTime","ThreatDuration");
        Add("Traps",65535,"GoldCost","BuildPoints","Health","HealthGain","ManaUsage","ManaCostToFire","Threat");
        Add("Spells",999999,"ManaCost");Add("Spells",32767,"ManaDrain","RechargeTime","UpgRechargeTime");
        return result;
    }
    public static bool Handles(string family,string field)=>rules.ContainsKey((family,field));
    public static IReadOnlyList<string> Fields(string family)
    {
        var fields=rules.Keys.Where(k=>k.Family==family).Select(k=>k.Field).ToArray();
        if(fields.Length==0)throw new ArgumentException("Choose a source global element page.",nameof(family));
        return Array.AsReadOnly(fields);
    }
    private static Rule Format(string family,string field)=>rules.TryGetValue((family,field),out var rule)?rule:throw new ArgumentException("Choose an explicit source numeric element control.",nameof(field));
    public static string Normalize(string family,string field,string text)
    {
        var rule=Format(family,field);var whole=SourceIntegerInput.ReadWhole(text,rule.Signed);
        return whole<rule.Minimum||whole>rule.Maximum?Math.Clamp(whole,rule.Minimum,rule.Maximum).ToString(CultureInfo.InvariantCulture)+(rule.DecimalClamp?".0":""):text;
    }
    public static string Step(string family,string field,string text,int delta)
    {
        if(delta is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(delta));
        var rule=Format(family,field);var next=unchecked(SourceIntegerInput.ReadWhole(text,rule.Signed)+delta);
        return Normalize(family,field,rule.Signed?next.ToString(CultureInfo.InvariantCulture):unchecked((uint)next).ToString(CultureInfo.InvariantCulture));
    }
}
