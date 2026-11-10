using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Unsigned EN_CHANGE/UDN controls on source Possession and CrSpells.
/// Native width protection stays separate from the source LONG input bounds.</summary>
public static class CreatureAuxiliaryInputs
{
    public static bool Handles(string page,string field)=>page switch
    {
        "Possession"=>field is "SpecialAb1Data" or "SpecialAb2Data" or "WaddleScale" or "OscillateScale" or "WalkScale",
        "CrSpells"=>field is "RechargeTime" or "Range",_=>false
    };
    private static (int Maximum,bool Fractional) Format(string page,string field)
    {
        if(!Handles(page,field))throw new ArgumentException("Choose a source Possession or CrSpells numeric control.",nameof(field));
        return page=="CrSpells"?(1024,true):field switch
        {"SpecialAb1Data" or "SpecialAb2Data"=>(32767,false),"WalkScale"=>(12,false),_=>(1,true)};
    }
    public static string Normalize(string page,string field,string text)
    {
        var rule=Format(page,field);var whole=SourceIntegerInput.ReadWhole(text,false);
        if(whole>=0&&whole<=rule.Maximum)return text;
        return Math.Clamp(whole,0,rule.Maximum).ToString(CultureInfo.InvariantCulture)+(rule.Fractional?".0":"");
    }
    public static string Step(string page,string field,string text,int delta,bool native)
    {
        if(delta is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(delta));
        Format(page,field);
        var next=unchecked((uint)(SourceIntegerInput.ReadWhole(text,false)+delta));
        var result=Normalize(page,field,next.ToString(CultureInfo.InvariantCulture));
        if(!native||page!="CrSpells")return result;
        var maximum=(int)decimal.Floor(GlobalDefinitionNumbers.Format("CrSpells",field,true).Maximum!.Value);
        return SourceIntegerInput.ReadWhole(result,false)>maximum?maximum.ToString(CultureInfo.InvariantCulture):result;
    }
}
