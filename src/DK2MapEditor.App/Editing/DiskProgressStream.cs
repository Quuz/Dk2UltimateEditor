using System.IO;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Presentation around the existing writer's stream. It never publishes
/// a target, retries I/O or changes the original stream's ownership/position.</summary>
public sealed class DiskProgressStream(Stream inner,string target,Action<EditorFileProgress> progress):Stream
{
    private long written;
    public override bool CanRead=>inner.CanRead;
    public override bool CanSeek=>inner.CanSeek;
    public override bool CanWrite=>inner.CanWrite;
    public override long Length=>inner.Length;
    public override long Position{get=>inner.Position;set=>inner.Position=value;}
    public override int Read(byte[] buffer,int offset,int count)=>inner.Read(buffer,offset,count);
    public override long Seek(long offset,SeekOrigin origin)=>inner.Seek(offset,origin);
    public override void SetLength(long value)=>inner.SetLength(value);
    public override void Write(byte[] buffer,int offset,int count)
    {
        progress(new(EditorFilePhase.Write,target,written));inner.Write(buffer,offset,count);written=checked(written+count);
        progress(new(EditorFilePhase.Write,target,written));
    }
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        progress(new(EditorFilePhase.Write,target,written));inner.Write(buffer);written=checked(written+buffer.Length);
        progress(new(EditorFilePhase.Write,target,written));
    }
    public override void WriteByte(byte value)
    {
        progress(new(EditorFilePhase.Write,target,written));inner.WriteByte(value);written=checked(written+1);
        progress(new(EditorFilePhase.Write,target,written));
    }
    public override void Flush()
    {
        progress(new(EditorFilePhase.Flush,target,written));inner.Flush();
        // Keep the durable flush that callers perform for an unwrapped file.
        if(inner is FileStream file)file.Flush(flushToDisk:true);
    }
    protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
}
