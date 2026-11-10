using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class FileDialogOutputChecks
{
    public static void Run()
    {
        var directory=Path.Combine(Path.GetTempPath(),"DK2FileDialogOutput-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Fails(Action action){try{action();}catch(Exception error)when(error is IOException or InvalidDataException){return;}throw new InvalidOperationException("Expected output failure.");}
        var paths=new EditorFileDialogSession();var map=Path.Combine(directory,"map.dkm");var area=Path.Combine(directory,"area.dkz");var target=Path.Combine(directory,"thumb.bmp");
        paths.SetMapTarget(map);paths.AcceptAreaPath(area);paths.AcceptThumbnailPath(target);
        Check(paths.Location(EditorFileDialogKind.Map).FileName=="map.dkm"&&paths.Location(EditorFileDialogKind.Area).FileName=="area.dkz"
            &&paths.Location(EditorFileDialogKind.Thumbnail).FileName=="thumb.bmp","Three file scopes interfered.");
        Check(new EditorFileDialogSession().Location(EditorFileDialogKind.Area).FileName=="","Independent workspace inherited a path.");
        var bad=Path.Combine(directory,"chosen-but-unreadable.dkz");paths.AcceptAreaPath(bad);Fails(()=>File.OpenRead(bad).Dispose());
        Check(paths.AreaPath==bad&&paths.MapTarget==map&&paths.ThumbnailPath==target,"Accepted failed AREA read lost its path or changed another scope.");
        paths.SetMapTarget(null);
        Check(paths.MapTarget is null&&paths.AreaPath==bad&&paths.ThumbnailPath==target,"New/native map reset AREA/BMP paths.");
        var doc=NativeMapDocument.Create(16,14,"ThumbnailFixture");var bmp=MapThumbnail.Bmp(doc,new());
        var old=new byte[]{5,4,3,2,1};File.WriteAllBytes(target,old);
        Fails(()=>MapThumbnail.SaveNewBmp(target,doc,new()));Check(File.ReadAllBytes(target).SequenceEqual(old),"Generic new-only API overwrote a file.");
        Fails(()=>ThumbnailFileOutput.SaveConfirmed(target,new byte[]{1,2,3}));Check(File.ReadAllBytes(target).SequenceEqual(old),"Bad prepared BMP overwrote a file.");
        Fails(()=>ThumbnailFileOutput.SaveConfirmed(target,bmp,path=>new FailingStream(path,failWrite:true)));
        Check(File.ReadAllBytes(target).SequenceEqual(old)&&!Directory.EnumerateFiles(directory,".dk2-thumbnail-*.tmp").Any(),"Partial staging write changed target or leaked staging.");
        Fails(()=>ThumbnailFileOutput.SaveConfirmed(target,bmp,path=>new FailingStream(path,failWrite:false)));
        Check(File.ReadAllBytes(target).SequenceEqual(old)&&!Directory.EnumerateFiles(directory,".dk2-thumbnail-*.tmp").Any(),"Staging flush failure changed target or leaked staging.");
        ThumbnailFileOutput.SaveConfirmed(target,bmp);Check(File.ReadAllBytes(target).SequenceEqual(bmp),"Confirmed replacement did not publish exact prepared bytes.");
        var fresh=Path.Combine(directory,"new.bmp");ThumbnailFileOutput.SaveConfirmed(fresh,bmp);
        Check(File.ReadAllBytes(fresh).SequenceEqual(bmp),"Confirmed new file did not publish exact bytes.");
        Check(!Directory.EnumerateFiles(directory,".dk2-thumbnail-*.tmp").Any(),"Successful output leaked staging.");
        // Remove only the private files created by this check; never recurse or touch installed inputs.
        foreach(var p in new[]{target,fresh})File.Delete(p);Directory.Delete(directory);
        Console.WriteLine("FILE DIALOG OUTPUT DETAIL independent source paths, retained failed AREA selection, new/native reset; BMP new-only protection, bad payload, partial write/flush rollback, exact new/replacement publication; private temp only, no WPF");
    }
    private sealed class FailingStream(string path,bool failWrite):Stream
    {
        private readonly FileStream inner=new(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        public override bool CanRead=>false;public override bool CanSeek=>false;public override bool CanWrite=>true;
        public override long Length=>inner.Length;public override long Position{get=>inner.Position;set=>throw new NotSupportedException();}
        public override void Write(byte[] buffer,int offset,int count)=>Write(buffer.AsSpan(offset,count));
        public override void Write(ReadOnlySpan<byte> buffer)
        {inner.Write(failWrite?buffer[..Math.Min(8,buffer.Length)]:buffer);if(failWrite)throw new IOException("Controlled staging write failure.");}
        public override void Flush()=>throw new IOException("Controlled staging flush failure.");
        public override int Read(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
        public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
        public override void SetLength(long value)=>throw new NotSupportedException();
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
}
