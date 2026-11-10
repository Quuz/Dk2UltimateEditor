using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class CreatureLevelModifierSessionChecks
{
    private const string Name="DK2Preview_6a88240eaf814f78";
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action run)
    {try{run();}catch(Exception e)when(e is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new Exception("An invalid level session/write was accepted.");}
    private static uint N(byte[] bytes,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset,4));
    private static void U(byte[] bytes,int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset,4),value);
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> before)=>Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Unrelated document buffers changed.");
    private static CreatureLevelModifierRecord Row(NativeMapDocument d,uint type=65,uint stat=0,uint level=2)
        =>d.ReadExistingCreatureLevelModifiers().Last(r=>r.RecordType==type&&r.Statistic==stat&&r.Level==level);
    private static int Value(NativeMapDocument d,uint type=65,uint stat=0,uint level=2)=>unchecked((short)Row(d,type,stat,level).PercentBits);
    private static CreatureLevelModifierChange Change(NativeMapDocument d,short value,uint type=65,uint stat=0,uint level=2)
        =>new(new(type,stat,level),Row(d,type,stat,level).PercentBits,value);
    private static void Seed(NativeMapDocument d,IEnumerable<CreatureLevelModifierRecord> rows)
    {
        foreach(var row in rows)
        {
            var record=d.AddRecord(new("GAMEGLOBALVARIABLE",0xE6,row.RecordType,16),new(0,0));
            Check(MemoryMarshal.TryGetArray(record.File.Data,out var memory),"No fixture buffer.");
            var bytes=memory.Array!.AsSpan(memory.Offset+record.Offset,16);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[4..],row.Statistic);BinaryPrimitives.WriteUInt32LittleEndian(bytes[8..],row.PercentBits);BinaryPrimitives.WriteUInt32LittleEndian(bytes[12..],row.Level);
        }
    }
    public static void Run(string root)
    {
        var directory=Path.Combine(Path.GetTempPath(),"CreatureLevelSession-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var shared=Path.GetFullPath(Path.Combine(root,"..","Dungeon Keeper 2","Data","editor","GlobalVariables.kwd"));
            var sharedBytes=File.ReadAllBytes(shared);var sharedHash=SHA256.HashData(sharedBytes);
            var basis=NativeMapDocument.Load(Path.Combine(root,"artifacts/PreFinal/TestMap",Name+".kwd"));
            var d=basis.CreateCompilationSnapshot();var defaults=CreatureLevelModifiers.ReadCollection(sharedBytes);
            Seed(d,defaults);Seed(d,[new(65,0,0x0000ffff,2,0),new(0x7ffffffe,123,456,789,0)]);
            d.ClearHistory();
            var source=Snapshot(d);var revision=d.EditRevision;var count=d.EditorChangeCount;var history=d.GetHistory().Undo.Count;var events=0;var historyEvents=0;
            d.Changed+=(_,_)=>events++;d.HistoryChanged+=(_,_)=>historyEvents++;
            using(var cancelled=d.CreateCreatureLevelModifierSession())
            {
                var values=cancelled.Draft.Current;var levels=values.Levels.ToArray();levels[0]=700;
                cancelled.Draft.Store(new(levels,-100));cancelled.Dispose();Reject(()=>d.ApplyCreatureLevelModifiers(cancelled));
            }
            Exact(source);Check(d.EditRevision==revision&&d.EditorChangeCount==count&&events==0&&historyEvents==0,"Cancel changed the live document.");
            using(var equal=d.CreateCreatureLevelModifierSession())
            {equal.Draft.Store(equal.Draft.Current);d.ApplyCreatureLevelModifiers(equal);Check(equal.IsClosed,"Equal Apply did not close the session.");}
            Exact(source);Check(d.EditRevision==revision&&events==0&&historyEvents==0,"Equal signed input created history or normalized source bits.");
            Reject(()=>d.SetExistingCreatureLevelModifiers([Change(d,3),new(new(65,0,1),100,3)]));Exact(source);
            using(var accepted=d.CreateCreatureLevelModifierSession())
            {
                var values=accepted.Draft.Current;var levels=values.Levels.ToArray();levels[0]=-10000;levels[1]=0;
                accepted.Draft.Store(new(levels,-1));d.ApplyCreatureLevelModifiers(accepted);Check(accepted.IsClosed,"Successful Apply retained authority.");
            }
            Check(Value(d)==-10000&&Value(d,65,0,3)==100&&Value(d,224,0,10)==-1,"Native Apply did not use signed/zero100 semantics.");
            Check(d.EditRevision==revision+1&&d.EditorChangeCount==count+1&&d.GetHistory().Undo.Count==history+1&&events==1&&historyEvents==1,"Native Apply was not one command.");
            var changed=Snapshot(d);var map=d.Files.Single(f=>f.Signature==0xE6&&f.ExportPath.Contains("Maps"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));
            var expected=source.ToDictionary(p=>p.Key,p=>p.Value.ToArray());U(expected[map],Row(d).Offset+8,0xffffd8f0);U(expected[map],Row(d,224,0,10).Offset+8,uint.MaxValue);
            Exact(expected);d.Undo();Exact(source);d.Redo();Exact(changed);
            using(var stale=d.CreateCreatureLevelModifierSession())
            {d.Undo();d.Redo();Reject(()=>d.ApplyCreatureLevelModifiers(stale));}
            using(var pen=d.BeginLivePenStroke(new(4,1,1)))Reject(()=>d.CreateCreatureLevelModifierSession());
            var nativeSnapshot=d.CreateCompilationSnapshot();nativeSnapshot.SetExistingCreatureLevelModifiers([Change(nativeSnapshot,25)]);
            Check(Value(d)==-10000&&Value(nativeSnapshot)==25,"Native snapshot aliases source values.");
            using var save=new EditorSaveSession(d);
            var firstPath=Path.Combine(directory,"first.dkm");Check(save.Save(save.PrepareSaveAs(firstPath),true),"Native->DKM save refused.");
            var project=save.Document;Check(project.IsLegacyProject&&Value(project)==-10000,"Conversion lost existing level tuples.");
            var archivePath=save.LastSave!.SourceArchives.Single();var archiveBytes=File.ReadAllBytes(archivePath);var archive=EditorSourceArchive.Read(archivePath);var retained=archive.RetainedVariables.ToArray();
            var beforeProject=project.CreateCompilationSnapshot();var projectFiles=Snapshot(project);var projectCount=project.EditorChangeCount;var projectUndo=project.GetHistory().Undo.Count;
            var oldPlan=save.PrepareSave();
            using(var edit=project.CreateCreatureLevelModifierSession())
            {
                var values=edit.Draft.Current;var levels=values.Levels.ToArray();levels[0]=7;edit.Draft.Store(new(levels,-2));project.ApplyCreatureLevelModifiers(edit);
            }
            Exact(projectFiles);Check(project.HasCreatureLevelModifierOverrides&&Value(project)==7&&Value(project,224,0,10)==-2,"DKM delta did not provide effective values.");
            using(var guarded=project.CreateCreatureLevelModifierSession())
            {
                var plan=save.PrepareSave();var field=typeof(NativeMapDocument).GetField("nativeLevelModifierDelta",BindingFlags.Instance|BindingFlags.NonPublic)!;
                var metadata=(NativeFile)field.GetValue(project)!;Check(MemoryMarshal.TryGetArray(metadata.Data,out var memory),"No metadata fixture buffer.");
                memory.Array![memory.Offset+12]^=1;
                Check(!project.IsCreatureLevelModifierSessionCurrent(guarded)&&!save.IsCurrent(plan),"Raw metadata changes retained session/Save authority without a revision.");
                memory.Array[memory.Offset+12]^=1;
            }
            using(var properties=project.CreateMapPropertiesSession())
            {
                properties.WorkingDocument.SetExistingCreatureLevelModifiers([Change(properties.WorkingDocument,71)]);
                _=project.PrepareMapProperties(properties);Check(Value(project)==7,"Preparing a parent plan published a private level delta.");
            }
            Check(project.EditorChangeCount==projectCount+1&&project.GetHistory().Undo.Count==projectUndo+1&&!save.IsCurrent(oldPlan),"DKM delta did not invalidate Save or create one command.");
            var raw=project.ReadCreatureLevelModifiers().Single(s=>s.Storage==CreatureLevelModifierStorage.RetainedNativeSource);
            Check(raw.Records.Last(r=>r.RecordType==65&&r.Statistic==0&&r.Level==2).PercentBits==0xffffd8f0,"DKM edited the immutable original instead of a delta.");
            project.Undo();Check(!project.HasCreatureLevelModifierOverrides&&Value(project)==-10000,"DKM Undo failed to remove its complete delta.");
            project.Redo();Check(Value(project)==7&&Value(project,224,0,10)==-2,"DKM Redo lost a value.");
            var copied=project.CreateCompilationSnapshot();copied.SetExistingCreatureLevelModifiers([Change(copied,99)]);
            Check(Value(project)==7&&Value(copied)==99,"DKM clone aliases its delta.");
            // Reverting restores literal original bits, including upper-word
            // peculiarities; it does not retain an unnecessary delta.
            copied.SetExistingCreatureLevelModifiers([Change(copied,-10000),Change(copied,-1,224,0,10)]);
            Check(!copied.HasCreatureLevelModifierOverrides&&Value(copied)==-10000,"Restoring source values left private metadata active.");
            Reject(()=>project.Edit("failed outer",()=>{project.SetExistingCreatureLevelModifiers([Change(project,44)]);throw new InvalidOperationException("Injected outer failure");}));
            Check(Value(project)==7,"Outer failure did not restore synthetic delta state.");
            MemoryRollback(project);
            var recovery=Path.Combine(directory,"recovery.dkm");save.SaveTemporary(recovery);
            Check(Value(NativeMapDocument.Load(recovery))==7,"Temporary save/reopen lost the delta or original companion.");
            var secondPath=Path.Combine(directory,"second.dkm");Check(save.Save(save.PrepareSaveAs(secondPath),true),"DKM delta save refused.");
            var reopened=NativeMapDocument.Load(secondPath);Check(Value(reopened)==7&&Value(reopened,224,0,10)==-2&&!reopened.HasChanges,"DKM reopen/baseline lost values or stayed dirty.");
            Check(File.ReadAllBytes(archivePath).AsSpan().SequenceEqual(archiveBytes)&&archive.RetainedVariables.AsSpan().SequenceEqual(retained),"Original archive changed during DKM edits/save.");
            var savedArchive=save.LastSave!.SourceArchives.Single();Check(File.ReadAllBytes(savedArchive).AsSpan().SequenceEqual(archiveBytes),"Save As replaced the original source archive.");
            var copyPath=reopened.SaveCopy(Path.Combine(directory,"editable-copy"));Check(Value(NativeMapDocument.Load(copyPath))==7,"SaveCopy lost the source companion.");
            var directPath=Path.Combine(directory,"direct.dkm");using(var writer=new EditorProjectSaveSession(reopened))writer.SaveAs(directPath);
            Check(Value(NativeMapDocument.Load(directPath))==7,"Project writer Save As lost the source companion.");
            var text=TextMapFile.SaveCopy(Path.Combine(directory,"text"),reopened);
            Check(Value(NativeMapDocument.Load(text.EditableCopyPath))==7,"MAP complete editable copy lost the delta.");
            SevenFiles(beforeProject,reopened,basis,directory);
            Malformed(secondPath,savedArchive,archive.ContentHash,directory);
            Check(SHA256.HashData(File.ReadAllBytes(shared)).AsSpan().SequenceEqual(sharedHash),"Installation shared data changed.");
            Console.WriteLine("CREATURE LEVEL SESSION DETAIL native and DKM private Cancel/no-op/atomic Apply/full Undo/Redo; stale/pen/range guards, independent clones, late allocation and outer failure rollback; D2LM separate from unchanged hashed source, Save/SaveAs/recovery/SaveCopy/MAP editable copy/reopen, malformed/hash/offset/duplicate/source guards and exact seven-file compilation; existing map tuples only, no guessed shared defaults or GUI/game");
        }
        finally{Directory.Delete(directory,true);}
    }
    private static void MemoryRollback(NativeMapDocument project)
    {
        var before=project.ReadExistingCreatureLevelModifiers().ToArray();var history=project.GetHistory();var count=project.EditorChangeCount;var revision=project.EditRevision;
        var field=typeof(NativeMapDocument).GetField("historyBufferCopy",BindingFlags.Instance|BindingFlags.NonPublic)!;var previous=field.GetValue(project);var calls=0;
        var buffers=((IEnumerable<NativeFile>)typeof(NativeMapDocument).GetMethod("TransactionFiles",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(project,null)!).Count();
        field.SetValue(project,new Func<byte[],byte[]>(bytes=>++calls==buffers+1?throw new OutOfMemoryException("After delta mutation"):bytes.ToArray()));
        try
        {
            try{project.SetExistingCreatureLevelModifiers([Change(project,53)]);throw new Exception("Expected history memory failure.");}
            catch(EditorHistoryMemoryException){}
        }
        finally{field.SetValue(project,previous);}
        Check(project.ReadExistingCreatureLevelModifiers().SequenceEqual(before)&&project.GetHistory().Undo.SequenceEqual(history.Undo)
            &&project.EditorChangeCount==count&&project.EditRevision==revision,"Late memory failure partially applied the delta.");
    }
    private static void SevenFiles(NativeMapDocument before,NativeMapDocument after,NativeMapDocument basis,string directory)
    {
        Dictionary<string,byte[]> Export(NativeMapDocument d,string suffix)
        {
            var compiled=d.CompileLegacyProject(Name,basis);var path=compiled.SaveCopy(Path.Combine(directory,suffix));var files=Directory.GetFiles(Path.GetDirectoryName(path)!);
            Check(files.Length==7,"Level deltas changed the official seven-file map set.");return files.ToDictionary(p=>Path.GetFileName(p)!,p=>File.ReadAllBytes(p));
        }
        var left=Export(before,"seven-before");var right=Export(after,"seven-after");
        Check(left.Keys.Order().SequenceEqual(right.Keys.Order()),"Compiled game file names differ.");
        var variable=Name+"Variables.kld";var expected=left[variable].ToArray();var rows=CreatureLevelModifiers.ReadCollection(expected);
        foreach(var (type,value) in new (uint,uint)[]{(65,7),(224,0xfffffffe)})
        {var row=rows.Last(r=>r.RecordType==type&&r.Statistic==0&&r.Level==(type==65?2:10));U(expected,row.Offset+8,value);}
        Check(right[variable].AsSpan().SequenceEqual(expected)&&left.Where(p=>p.Key!=variable).All(p=>right[p.Key].AsSpan().SequenceEqual(p.Value)),"Compiled changes escaped the two intended value DWORDs.");
    }
    private static void Malformed(string projectPath,string archivePath,string archiveHash,string directory)
    {
        const uint signature=0x4d4c3244;var parsed=LegacySectionFile.Read(projectPath);var extension=parsed.Sections.Single(s=>s.Signature==signature);
        void Bad(string name,Action<byte[]> corrupt,bool source=true,bool duplicate=false)
        {
            var bytes=extension.Payload.ToArray();corrupt(bytes);var sections=parsed.Sections.Select(s=>s.Signature==signature?new LegacySection(signature,s.Version,bytes):s).ToList();
            if(duplicate)sections.Add(extension);var path=Path.Combine(directory,"bad-"+name+".dkm");File.WriteAllBytes(path,LegacySectionFile.Create(sections).ToArray());
            if(source)File.Copy(archivePath,path+".native-source-"+archiveHash+".zip");
            Reject(()=>NativeMapDocument.Load(path));
        }
        Bad("hash",b=>b[0]^=1);Bad("offset",b=>U(b,52,0));Bad("original",b=>b[56]^=1);Bad("key",b=>U(b,44,8));
        Bad("bits",b=>U(b,60,0x0000ffff));Bad("range",b=>U(b,60,10001));Bad("count",b=>U(b,32,231));Bad("reserved",b=>U(b,36,1));
        Bad("missing-source",_=>{},false);Bad("duplicate",_=>{},duplicate:true);
        var bytesOnly=extension.Payload.ToArray();Check(N(bytesOnly,32)==2,"Unexpected delta fixture shape.");
    }
}
