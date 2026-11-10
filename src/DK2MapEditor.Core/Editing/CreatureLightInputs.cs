using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Properties_GlobalLightProc EN_CHANGE/UDN_DELTAPOS. These are
/// explicit text callbacks, not a decoder or permission to repair imports.</summary>
public static class CreatureLightInputs
{
    public static bool Handles(string field)=>field is "LightX" or "LightY" or "LightZ" or "LightRadius";
    private static bool Signed(string field)
    {
        if(!Handles(field))throw new ArgumentException("Choose a source light position or radius control.",nameof(field));
        return field!="LightRadius";
    }
    public static string Normalize(string field,string text)
    {
        var signed=Signed(field);var whole=SourceIntegerInput.ReadWhole(text,signed);var minimum=signed?-32767:0;var maximum=signed?32767:3;
        if(whole>=minimum&&whole<=maximum)return text;
        // Original Misc_SetDlgItemFloat[Signed] retains one decimal digit for
        // an integral clamped fixed-point value: 3.0, -32767.0, etc.
        return Math.Clamp(whole,minimum,maximum).ToString(CultureInfo.InvariantCulture)+".0";
    }
    public static string Step(string field,string text,int delta)
    {
        if(delta is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(delta));
        var signed=Signed(field);var whole=unchecked(SourceIntegerInput.ReadWhole(text,signed)+delta);
        // Misc_SetUpDownInt uses SetDlgItemInt before EN_CHANGE. The unsigned
        // radius decrement at zero passes UINT_MAX to that callback.
        var value=signed?whole.ToString(CultureInfo.InvariantCulture):unchecked((uint)whole).ToString(CultureInfo.InvariantCulture);
        return Normalize(field,value);
    }
}
