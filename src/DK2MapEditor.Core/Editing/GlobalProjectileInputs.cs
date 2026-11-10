using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Numeric callbacks of source TypeProc and shared ShotProc.
/// The user-approved corrections keep the second spinner on its own field
/// and format a signed lower clamp as -32768 instead of an unsigned UINT.</summary>
public static class GlobalProjectileInputs
{
    public static bool IsNumber(GlobalProjectileOption option,bool second)
    {
        ArgumentNullException.ThrowIfNull(option);
        return second?option.Form is 0 or 2 or 4:option.Form is 0 or 1;
    }
    private static (bool Signed,int Minimum,int Maximum) Rule(GlobalProjectileOption option,bool second)
    {
        if(!IsNumber(option,second))throw new ArgumentException("Choose an active numeric projectile control.",nameof(second));
        var signed=option.Form is 0 or 1&&(second?option.Signed2:option.Signed1);
        return (signed,signed?-32768:0,second&&option.Form==2?100:second&&option.Form==4?80000:32767);
    }
    private static string Normalize(string text,bool signed,int minimum,int maximum)
    {
        var whole=SourceIntegerInput.ReadWhole(text,signed);
        return whole<minimum||whole>maximum?Math.Clamp(whole,minimum,maximum).ToString(CultureInfo.InvariantCulture):text;
    }
    private static string Step(string text,int direction,bool signed,int minimum,int maximum)
    {
        if(direction is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(direction));
        var next=unchecked(SourceIntegerInput.ReadWhole(text,signed)+direction);
        return Normalize(signed?next.ToString(CultureInfo.InvariantCulture):unchecked((uint)next).ToString(CultureInfo.InvariantCulture),signed,minimum,maximum);
    }
    public static string NormalizeTrigger(string text)=>Normalize(text,false,0,32767);
    public static string StepTrigger(string text,int direction)=>Step(text,direction,false,0,32767);
    public static string Normalize(GlobalProjectileOption option,bool second,string text)
    {var rule=Rule(option,second);return Normalize(text,rule.Signed,rule.Minimum,rule.Maximum);}
    public static string Step(GlobalProjectileOption option,bool second,string text,int direction)
    {var rule=Rule(option,second);return Step(text,direction,rule.Signed,rule.Minimum,rule.Maximum);}
}
