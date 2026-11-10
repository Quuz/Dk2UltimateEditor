using System.Buffers.Binary;

namespace DK2MapEditor.App.Editing;

internal static class FilePreviewDialogPlan
{
    internal static string NativeFilter(string filter)
    {var parts=filter.Split('|');if(parts.Length<2||parts.Length%2!=0||parts.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Invalid file filter.",nameof(filter));return string.Join('\0',parts)+"\0\0";}
    internal static string? Extension(string filter,int index,string? fallback)
    {
        var parts=filter.Split('|');var pattern=index>0&&index<=parts.Length/2?parts[index*2-1].Split(';')[0]:"";
        return pattern.StartsWith("*.",StringComparison.Ordinal)&&!pattern[2..].Contains('*')&&!pattern[2..].Contains('?')?pattern[2..]:fallback?.TrimStart('.');
    }
    internal static uint Flags(bool save,bool exists,bool overwrite)=>0x00080000u|0x00000020u|0x00000080u|0x00800000u|0x00000008u|0x00000004u|0x00000800u|(exists?0x00001000u:0)|(save&&overwrite?0x00000002u:0);
    internal static byte[] Template()
    {
        var data=new byte[24];BinaryPrimitives.WriteUInt32LittleEndian(data,0x44000404); // WS_CHILD/CLIPSIBLINGS, DS_CONTROL/3DLOOK; no controls until WM_INITDIALOG.
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(14),340);BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(16),110);return data;
    }
}
internal sealed class FilePreviewEpoch:IDisposable
{
    private long revision;private int closed;
    internal long Begin(){ObjectDisposedException.ThrowIf(Volatile.Read(ref closed)!=0,this);return Interlocked.Increment(ref revision);}
    internal bool IsCurrent(long value)=>Volatile.Read(ref closed)==0&&Volatile.Read(ref revision)==value;
    public void Dispose(){Interlocked.Exchange(ref closed,1);Interlocked.Increment(ref revision);}
}
