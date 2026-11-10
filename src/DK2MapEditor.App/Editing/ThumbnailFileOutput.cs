using System.Buffers.Binary;
using System.IO;

namespace DK2MapEditor.App.Editing;

/// <summary>Publish the exact prepared BMP only after native Save/overwrite consent.</summary>
public static class ThumbnailFileOutput
{
    public static void SaveConfirmed(string path,ReadOnlyMemory<byte> bmp,Func<string,Stream>? createStaging=null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if(bmp.Length<54||bmp.Span[0]!='B'||bmp.Span[1]!='M'||BinaryPrimitives.ReadUInt32LittleEndian(bmp.Span[2..])!=bmp.Length)
            throw new InvalidDataException("Invalid prepared thumbnail bitmap.");
        var target=Path.GetFullPath(path);var parent=Path.GetDirectoryName(target)!;
        if(!Directory.Exists(parent))throw new DirectoryNotFoundException(parent);
        if(Directory.Exists(target))throw new IOException("Thumbnail destination is a directory.");
        var staging=Path.Combine(parent,".dk2-thumbnail-"+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            using(var stream=(createStaging??CreateStaging)(staging))
            {
                stream.Write(bmp.Span);
                if(stream is FileStream file)file.Flush(flushToDisk:true);else stream.Flush();
            }
            File.Move(staging,target,overwrite:true);
        }
        finally{if(File.Exists(staging))File.Delete(staging);}
    }
    private static Stream CreateStaging(string path)=>new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
}
