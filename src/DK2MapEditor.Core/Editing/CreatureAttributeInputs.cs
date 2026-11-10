using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Properties_GlobalSetNewValue / ModifyValue for the 38 GAttribs
/// entries. Loading and displaying an imported row do not invoke these edits.</summary>
public static class CreatureAttributeInputs
{
    public static bool Handles(string field)=>CreaturePageControls.Fields("Attributes").Contains(field)
        &&!CreaturePageControls.Named(field)&&!CreaturePageControls.Boolean("Attributes",field);
    private static GlobalDefinitionNumberFormat Format(string field)
    {
        if(!Handles(field))throw new ArgumentException("Choose an original creature attribute.",nameof(field));
        return GlobalDefinitionNumbers.Format("Creatures",field);
    }
    public static string Normalize(string field,string text)
    {
        var format=Format(field);var whole=SourceIntegerInput.ReadWhole(text,true);
        if(whole>32767)return format.Fractional?"32767.0":"32767";
        if(!format.SignedAttribute&&whole<0)return format.Fractional?"0.0":"0";
        // Original signed integer attributes have no lower EN_CHANGE clamp.
        return text;
    }
    public static string Step(string field,string text,int delta,bool native)
    {
        if(delta is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(delta));
        Format(field);
        var result=Normalize(field,unchecked(SourceIntegerInput.ReadWhole(text,true)+delta).ToString(CultureInfo.InvariantCulture));
        if(!native)return result;
        // Retain the editor's existing native packed-width guard. Source LONG
        // project input has a wider range than direct native WORD editing.
        var format=GlobalDefinitionNumbers.Format("Creatures",field,true);
        var whole=SourceIntegerInput.ReadWhole(result,true);
        var min=(int)decimal.Ceiling(format.Minimum!.Value);var max=(int)decimal.Floor(format.Maximum!.Value);
        return whole<min||whole>max?Math.Clamp(whole,min,max).ToString(CultureInfo.InvariantCulture):result;
    }
}
