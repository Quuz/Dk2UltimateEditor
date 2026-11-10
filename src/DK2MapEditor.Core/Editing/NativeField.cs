using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace DK2MapEditor.Core.Editing;

public enum NativeValueKind { Byte, UInt16, Int16, UInt32, Int32, Ascii, Unicode, WesternAnsi }

/// <summary>A verified byte range in a native DK2 record, not a CLR-marshalled struct.</summary>
public sealed record NativeField(string Name, int Offset, int Size, NativeValueKind Kind, byte BitMask = 0)
{
    private static readonly Encoding WesternAnsi = CreateWesternEncoding();
    private static Encoding CreateWesternEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }
    public string Read(ReadOnlySpan<byte> data)
    {
        var span = data.Slice(Offset, Size);
        if (BitMask != 0) return (span[0] & BitMask) != 0 ? "1" : "0";
        return Kind switch
        {
            NativeValueKind.Byte => span[0].ToString(CultureInfo.InvariantCulture),
            NativeValueKind.UInt16 => BinaryPrimitives.ReadUInt16LittleEndian(span).ToString(CultureInfo.InvariantCulture),
            NativeValueKind.Int16 => BinaryPrimitives.ReadInt16LittleEndian(span).ToString(CultureInfo.InvariantCulture),
            NativeValueKind.UInt32 => BinaryPrimitives.ReadUInt32LittleEndian(span).ToString(CultureInfo.InvariantCulture),
            NativeValueKind.Int32 => BinaryPrimitives.ReadInt32LittleEndian(span).ToString(CultureInfo.InvariantCulture),
            NativeValueKind.Unicode => Encoding.Unicode.GetString(span).Split('\0')[0],
            NativeValueKind.WesternAnsi => WesternAnsi.GetString(span).Split('\0')[0],
            _ => Encoding.ASCII.GetString(span).Split('\0')[0]
        };
    }

    public byte[] Encode(string value)
    {
        var result = new byte[Size];
        var numberStyle = NumberStyles.Integer;
        var numeric = value.Trim();
        if (numeric.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            numberStyle = NumberStyles.AllowHexSpecifier;
            numeric = numeric[2..];
        }
        switch (Kind)
        {
            case NativeValueKind.Byte: result[0] = byte.Parse(numeric, numberStyle, CultureInfo.InvariantCulture); break;
            case NativeValueKind.UInt16: BinaryPrimitives.WriteUInt16LittleEndian(result, ushort.Parse(numeric, numberStyle, CultureInfo.InvariantCulture)); break;
            case NativeValueKind.Int16: BinaryPrimitives.WriteInt16LittleEndian(result, short.Parse(numeric, numberStyle, CultureInfo.InvariantCulture)); break;
            case NativeValueKind.UInt32: BinaryPrimitives.WriteUInt32LittleEndian(result, uint.Parse(numeric, numberStyle, CultureInfo.InvariantCulture)); break;
            case NativeValueKind.Int32: BinaryPrimitives.WriteInt32LittleEndian(result, int.Parse(numeric, numberStyle, CultureInfo.InvariantCulture)); break;
            default:
                if (value.Contains('\0')) throw new ArgumentException("Embedded NUL is not allowed.");
                if (Kind == NativeValueKind.Ascii && value.Any(character => character > 127))
                    throw new ArgumentException("This native field only supports ASCII characters.");
                var encoding = Kind == NativeValueKind.Unicode ? new UnicodeEncoding(false, false, true) : Kind == NativeValueKind.WesternAnsi ? WesternAnsi : Encoding.ASCII;
                var bytes = encoding.GetBytes(value);
                var terminatorSize = Kind == NativeValueKind.Unicode ? 2 : 1;
                if (bytes.Length > Size - terminatorSize) throw new ArgumentException($"The text is too long for this {Size}-byte field.");
                bytes.CopyTo(result, 0);
                break;
        }
        if (BitMask != 0 && result[0] > 1) throw new ArgumentException("A flag must be 0 or 1.");
        return result;
    }
}
