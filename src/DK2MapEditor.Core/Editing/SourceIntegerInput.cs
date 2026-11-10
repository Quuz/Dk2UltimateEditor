namespace DK2MapEditor.Core.Editing;

internal static class SourceIntegerInput
{
    // GetDlgItemInt reads the leading whole part. The original callbacks
    // assign even unsigned UINT results to LONG before testing their limits.
    internal static int ReadWhole(string text,bool signed)
    {
        ArgumentNullException.ThrowIfNull(text);
        var token=text.AsSpan().TrimStart();var negative=signed&&token.Length>0&&token[0]=='-';
        if(negative)token=token[1..];
        uint value=0;var maximum=signed?(negative?0x80000000u:0x7fffffffu):uint.MaxValue;
        foreach(var c in token)
        {
            if(!char.IsAsciiDigit(c))break;
            var digit=(uint)(c-'0');if(value>(maximum-digit)/10)return 0;
            value=value*10+digit;
        }
        return negative?unchecked(-(int)value):unchecked((int)value);
    }
}
