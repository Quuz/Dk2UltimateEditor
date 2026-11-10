using System.Globalization;

namespace DK2MapEditor.App.Editing;

/// <summary>RectEx_Proc EN_CHANGE and Misc_SetUpDownInt. Only explicit UI
/// input is normalized; this policy never edits imported records.</summary>
public static class RegionCoordinateRules
{
    // GetDlgItemInt(FALSE) reads a leading unsigned decimal prefix, returns
    // zero on failure/UINT overflow, then source assigns UINT to LONG/int.
    public static int Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var digits=text.AsSpan().TrimStart();uint value=0;
        foreach(var c in digits)
        {
            if(!char.IsAsciiDigit(c))break;
            var digit=(uint)(c-'0');
            if(value>(uint.MaxValue-digit)/10)return 0;
            value=value*10+digit;
        }
        return unchecked((int)value);
    }
    public static int Value(string text,int extent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(extent);
        return Math.Clamp(Read(text),0,extent-1);
    }
    public static string Normalize(string text,int extent)
    {
        var value=Value(text,extent);
        // Source only calls SetDlgItemInt when clamping. Keep an empty field
        // or a partial token available for editing; acceptance reads it as 0.
        return value==Read(text)?text:value.ToString(CultureInfo.InvariantCulture);
    }
    public static string Step(string text,int extent,int delta)
    {
        if(delta is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(delta));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(extent);
        return Math.Clamp(unchecked(Read(text)+delta),0,extent-1).ToString(CultureInfo.InvariantCulture);
    }
}
