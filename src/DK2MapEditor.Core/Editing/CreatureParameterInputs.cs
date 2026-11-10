using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Active Parameters amount control129 EN_CHANGE and its unsigned spin.
/// The caller retains the existing attraction type and enabled-state guards.</summary>
public static class CreatureParameterInputs
{
    public static string NormalizeAmount(string text)
    {
        var whole=SourceIntegerInput.ReadWhole(text,false);
        return whole is >=0 and <=16000?text:Math.Clamp(whole,0,16000).ToString(CultureInfo.InvariantCulture);
    }
    public static string StepAmount(string text,int delta)
    {
        if(delta is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(delta));
        var next=unchecked((uint)(SourceIntegerInput.ReadWhole(text,false)+delta));
        return NormalizeAmount(next.ToString(CultureInfo.InvariantCulture));
    }
}
