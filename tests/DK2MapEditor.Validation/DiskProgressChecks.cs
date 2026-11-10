using System.Security.Cryptography;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class DiskProgressChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Fails(Action action){try{action();}catch(IOException){return;}throw new InvalidOperationException("Expected stream failure.");}
    public static void Run(string repo)
    {
        var directory=Path.Combine(Path.GetTempPath(),"DK2DiskProgress-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        const string level="DK2Preview_6a88240eaf814f78";
        var dkm=Path.Combine(repo,"tests/fixtures/NativeRoundtrip",level+".dkm");
        var kwd=Path.Combine(repo,"artifacts/PreFinal/TestMap",level+".kwd");
        var project=NativeMapDocument.CreateNewMap(new("DiskProgress","Disk progress",18,16,PlaceHearts:true));
        using(var session=new EditorSaveSession(project))
        {
            var path=Path.Combine(directory,"progress.map");var plan=session.PrepareSaveAs(path);
            Check(session.Save(plan,true),"Could not prepare MAP load fixture.");
        }
        foreach(var path in new[]{dkm,kwd,Path.Combine(directory,"progress.map")})
        {
            var original=NativeMapDocument.Load(path);var files=original.Files.Where(f=>File.Exists(f.SourcePath)).Select(f=>f.SourcePath).Distinct().ToDictionary(p=>p,p=>SHA256.HashData(File.ReadAllBytes(p)));
            var progress=new List<EditorFileProgress>();var observed=NativeMapDocument.Load(path,progress.Add);
            Check(progress.Count>2&&progress[0].Phase==EditorFilePhase.Read&&progress[^1].Phase==EditorFilePhase.Done
                &&progress.Any(p=>p.Phase==EditorFilePhase.Map)&&progress.Any(p=>p.Phase==EditorFilePhase.Validate),"Load progress lacks real read/map/validation/completion stages.");
            Check(progress.All(p=>p.Completed>=0&&p.Total>=0&&(p.Total==0||p.Completed<=p.Total)),"Load progress fabricated an invalid phase count.");
            Check(original.Files.Count==observed.Files.Count&&original.Files.Zip(observed.Files).All(p=>p.First.SourcePath==p.Second.SourcePath&&p.First.ExportPath==p.Second.ExportPath&&p.First.Data.Span.SequenceEqual(p.Second.Data.Span))
                &&original.MissingReferences.SequenceEqual(observed.MissingReferences),"Progress changed loaded map bytes, topology or missing references.");
            Check(files.All(p=>SHA256.HashData(File.ReadAllBytes(p.Key)).SequenceEqual(p.Value)),"Load progress wrote an input.");
            var interrupted=new List<EditorFileProgress>();
            Fails(()=>NativeMapDocument.Load(path,p=>{interrupted.Add(p);if(p.Phase==EditorFilePhase.Validate)throw new IOException("Injected observer interruption.");}));
            Check(interrupted[^1].Phase==EditorFilePhase.Validate&&!interrupted.Any(p=>p.Phase==EditorFilePhase.Done),"Interrupted load reported completion.");
            using var exclusive=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.None);
            Check(exclusive.Length>0,"Interrupted load retained an input handle.");
        }
        var memory=new MemoryStream();var records=new List<EditorFileProgress>();
        using(var stream=new DiskProgressStream(memory,"Output",records.Add))
        {
            stream.Write(new byte[]{1,2,3},0,3);stream.Write(new ReadOnlySpan<byte>(new byte[]{4,5}));stream.WriteByte(6);stream.Flush();
            Check(stream.Position==6&&stream.Length==6&&memory.ToArray().SequenceEqual(new byte[]{1,2,3,4,5,6}),"Progress stream changed bytes/position.");
            Check(records[^1] is {Phase:EditorFilePhase.Flush,Completed:6}&&records.Where(p=>p.Phase==EditorFilePhase.Write).Select(p=>p.Completed).SequenceEqual(new long[]{0,3,3,5,5,6}),"Progress byte observations do not match actual writes.");
        }
        Check(!memory.CanWrite,"Progress stream did not dispose its owned stream.");
        foreach(var extension in new[]{"dkm","map"})
        {
            var target=Path.Combine(directory,"failure."+extension);var sentinel=new byte[]{9,8,7};File.WriteAllBytes(target,sentinel);
            foreach(var failWrite in new[]{true,false})
            {
                using var save=new EditorSaveSession(NativeMapDocument.CreateNewMap(new("Failure","Failure",18,16,PlaceHearts:true)));
                var plan=save.PrepareSaveAs(target);var history=save.Document.GetHistory().Undo.Count;var progress=new List<EditorFileProgress>();
                Fails(()=>save.Save(plan,true,p=>new DiskProgressStream(new FailingStream(p,failWrite),target,progress.Add)));
                Check(File.ReadAllBytes(target).SequenceEqual(sentinel)&&save.TargetPath is null&&save.LastSave is null
                    &&save.Document.GetHistory().Undo.Count==history&&!Directory.EnumerateFiles(directory,".dk2-*.tmp").Any(),"Observed write/flush failure changed previous target, adopted save state or leaked staging.");
                Check(progress.Any(p=>p.Phase==EditorFilePhase.Write)&&!progress.Any(p=>p.Phase==EditorFilePhase.Done),"Failed write was reported completed.");
            }
        }
        Console.WriteLine("DISK PROGRESS DETAIL DKM/KWD/MAP observer-neutral exact loads; stream array/span/byte writes and ownership; DKM/MAP partial-write/flush failures retain old targets and history, no staging leaks; no WPF/game/installed writes");
    }
    private sealed class FailingStream(string path,bool failWrite):Stream
    {
        private readonly FileStream inner=new(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        public override bool CanRead=>false;public override bool CanSeek=>false;public override bool CanWrite=>true;
        public override long Length=>inner.Length;public override long Position{get=>inner.Position;set=>throw new NotSupportedException();}
        public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long v)=>throw new NotSupportedException();
        public override void Write(byte[] b,int o,int c)=>Write(b.AsSpan(o,c));
        public override void Write(ReadOnlySpan<byte> b){if(failWrite){inner.Write(b[..Math.Min(b.Length,3)]);throw new IOException("Injected partial write.");}inner.Write(b);}
        public override void Flush(){inner.Flush();if(!failWrite)throw new IOException("Injected flush failure.");}
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
}
