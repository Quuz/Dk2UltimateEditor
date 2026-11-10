using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Jobs EN_CHANGE/UDN for the three parameter kinds, repeated by
/// source Happy/Unhappy/Angry controls. This never submits an Add/Change tuple.</summary>
public static class CreatureJobInputs
{
    public static bool Handles(string field)=>field is "Chance" or "Mood" or "Mana";
    private static bool Signed(string field)
    {
        if(!Handles(field))throw new ArgumentException("Choose job chance, mood or mana.",nameof(field));
        return field!="Chance";
    }
    public static string Normalize(string field,string text)
    {
        var signed=Signed(field);var whole=SourceIntegerInput.ReadWhole(text,signed);var minimum=signed?-32767:0;var maximum=signed?32767:100;
        return whole<minimum||whole>maximum?Math.Clamp(whole,minimum,maximum).ToString(CultureInfo.InvariantCulture):text;
    }
    public static string Step(string field,string text,int delta)
    {
        if(delta is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(delta));
        var signed=Signed(field);var next=unchecked(SourceIntegerInput.ReadWhole(text,signed)+delta);
        return Normalize(field,signed?next.ToString(CultureInfo.InvariantCulture):unchecked((uint)next).ToString(CultureInfo.InvariantCulture));
    }
}
