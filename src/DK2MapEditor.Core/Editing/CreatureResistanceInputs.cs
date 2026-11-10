using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Source resistance trackbars display 0..100 while their labels and
/// untouched drafts retain imported rates. An explicit scroll submits position.</summary>
public static class CreatureResistanceInputs
{
    public static IReadOnlyList<string> Fields { get; }=Array.AsReadOnly(new[]{"Res1Rate","Res2Rate","Res3Rate","Res4Rate"});
    public static bool IsRate(string field)=>Fields.Contains(field);
    public static string TypeField(string field)=>IsRate(field)?field[..^4]:throw new ArgumentException("Choose a resistance rate.",nameof(field));
    public static int Position(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if(text.Length==0)return 0; // Unverified layouts have no editable value.
        return (int)Math.Clamp(long.Parse(text,NumberStyles.Integer,CultureInfo.InvariantCulture),0L,100L);
    }
    public static string Scroll(string current,int delta)
    {
        if(delta is not (-20 or -1 or 1 or 20))throw new ArgumentOutOfRangeException(nameof(delta));
        return Math.Clamp(Position(current)+delta,0,100).ToString(CultureInfo.InvariantCulture);
    }
}
