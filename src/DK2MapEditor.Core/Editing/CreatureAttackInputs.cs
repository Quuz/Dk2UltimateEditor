using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Properties_GlobalAttacksProc EN_CHANGE and UDN_DELTAPOS for its
/// sixteen numeric controls. Levels remain original 0..10 combo boxes.</summary>
public static class CreatureAttackInputs
{
    private sealed record Rule(bool Signed,bool Fractional,int Maximum);
    private static readonly IReadOnlyDictionary<string,Rule> rules=CreateRules();
    private static IReadOnlyDictionary<string,Rule> CreateRules()
    {
        var result=new Dictionary<string,Rule>
        { ["RechargeTime"]=new(false,true,255),["Range"]=new(false,true,1000),["Dammage"]=new(false,false,32767),["Delay"]=new(false,true,1000) };
        foreach(var slot in new[]{"Shot1","Shot2","Shot3"})
        {
            result.Add(slot+"Delay",new(false,true,1000));
            foreach(var axis in new[]{"X","Y","Z"})result.Add(slot+axis,new(true,true,32767));
        }
        return result;
    }
    public static IReadOnlyList<string> Fields { get; }=Array.AsReadOnly(rules.Keys.ToArray());
    public static bool Handles(string field)=>rules.ContainsKey(field);
    private static Rule Format(string field)=>rules.TryGetValue(field,out var rule)?rule:throw new ArgumentException("Choose a source numeric attack control.",nameof(field));
    public static string Normalize(string field,string text)
    {
        var rule=Format(field);var whole=SourceIntegerInput.ReadWhole(text,rule.Signed);var minimum=rule.Signed?-32767:0;
        if(whole>=minimum&&whole<=rule.Maximum)return text;
        return Math.Clamp(whole,minimum,rule.Maximum).ToString(CultureInfo.InvariantCulture)+(rule.Fractional?".0":"");
    }
    public static string Step(string field,string text,int delta)
    {
        if(delta is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(delta));
        var rule=Format(field);var whole=unchecked(SourceIntegerInput.ReadWhole(text,rule.Signed)+delta);
        return Normalize(field,rule.Signed?whole.ToString(CultureInfo.InvariantCulture):unchecked((uint)whole).ToString(CultureInfo.InvariantCulture));
    }
}
