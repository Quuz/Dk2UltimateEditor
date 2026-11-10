using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class CreatureLevelModifierDefaultsChecks
{
    private const string Name="DK2Preview_6a88240eaf814f78";
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception e)when(e is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new Exception("Invalid inherited level edit accepted.");}
    private static uint N(byte[] b,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(offset,4));
    private static void U(byte[] b,int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(offset,4),value);
    private static CreatureLevelModifierRecord Row(NativeMapDocument d,uint stat=0,uint type=65,uint level=2)=>d.ReadExistingCreatureLevelModifiers().Last(r=>r.RecordType==type&&r.Statistic==stat&&r.Level==level);
    private static int Value(NativeMapDocument d,uint stat=0,uint type=65,uint level=2)=>unchecked((short)Row(d,stat,type,level).PercentBits);
    private static void Store(CreatureLevelModifierSession session,int value,int firstPerson)
    {var levels=session.Draft.Current.Levels.ToArray();levels[0]=value;session.Draft.Store(new(levels,firstPerson));}
    public static void Run(string root)
    {
        var directory=Path.Combine(Path.GetTempPath(),"CreatureLevelDefaults-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var shared=Path.GetFullPath(Path.Combine(root,"..","Dungeon Keeper 2","Data","editor","GlobalVariables.kwd"));var source=File.ReadAllBytes(shared);var hash=SHA256.HashData(source);
            var defaults=CreatureLevelModifiers.ReadCollection(source).ToArray();
            var installed=Path.GetFullPath(Path.Combine(root,"..","Dungeon Keeper 2","Data","editor","maps",Name+".kwd"));
            var basis=NativeMapDocument.Load(installed);Check(basis.ReadCreatureLevelModifierDefaults().Count==320,"Installed standard map did not resolve its shared catalogue.");
            foreach(var native in new[]{false,true})Matrix(native,basis,defaults,directory);
            FullTable(defaults,basis,directory);
            Check(SHA256.HashData(File.ReadAllBytes(shared)).AsSpan().SequenceEqual(hash),"Default inheritance edited the installation.");
            Console.WriteLine("CREATURE LEVEL DEFAULT DETAIL real shared320 rows, independent captured defaults and map last-wins; fresh native/DKM missing-key insertion, reset copy/no-op/Cancel, mixed updates/additions, parent common Apply/Undo/Redo and source guards; D2LO typed230-key ownership without forged native provenance, Save/reopen/recovery/copies, exact seven-file count, all230 values, malformed/duplicate/collision guards and post-mutation memory rollback; no UI/game");
        }
        finally{Directory.Delete(directory,true);}
    }
    private static void Matrix(bool native,NativeMapDocument basis,CreatureLevelModifierRecord[] defaults,string directory)
    {
        var d=NativeMapDocument.CreateNewMap(new(Name,"Inherited stats",20,20,NativeFormat:native),basis);
        var raw=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=d.EditorChangeCount;var revision=d.EditRevision;var history=d.GetHistory().Undo.Count;
        Check(d.ReadExistingCreatureLevelModifiers().Count==0,"New map unexpectedly materialized level records.");
        var input=defaults.ToArray();using(var cancelled=d.CreateCreatureLevelModifierSession(input))
        {
            var first=cancelled.Draft.Current.Levels[0];input[0]=input[0] with{PercentBits=9999};
            Check(cancelled.Draft.Current.Levels[0]==first,"Captured defaults alias caller storage.");Store(cancelled,901,902);cancelled.ResetSelectedToDefaults();
            Check(cancelled.Draft.Changes.Count==0,"Reset of an inherited page left a delta.");Store(cancelled,3,4);
        }
        using(var equal=d.CreateCreatureLevelModifierSession(defaults)){equal.ResetSelectedToDefaults();d.ApplyCreatureLevelModifiers(equal);}
        Check(d.EditRevision==revision&&d.EditorChangeCount==count&&d.GetHistory().Undo.Count==history&&raw.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Cancel/no-op wrote inherited records.");
        Reject(()=>d.CreateCreatureLevelModifierSession(defaults.Where(r=>r.Statistic!=0)));
        using(var edit=d.CreateCreatureLevelModifierSession(defaults)){Store(edit,-11,222);d.ApplyCreatureLevelModifiers(edit);}
        Check(Value(d)==-11&&Value(d,0,224,10)==222&&d.ReadExistingCreatureLevelModifiers().Count==2,"Missing override insertion failed.");
        Check(d.EditorChangeCount==count+1&&d.GetHistory().Undo.Count==history+1,"Inherited Apply was not one command.");
        d.Undo();Check(d.ReadExistingCreatureLevelModifiers().Count==0,"Undo did not remove new records.");d.Redo();
        var prior=d.EditorChangeCount;
        using(var parent=d.CreateMapPropertiesSession())
        {
            using var child=parent.WorkingDocument.CreateCreatureLevelModifierSession(defaults);child.ResetSelectedToDefaults();parent.WorkingDocument.ApplyCreatureLevelModifiers(child);
            Check(Value(d)==-11,"Parent's private child wrote the live map.");
            d.ApplyEditorMapProperties(d.PrepareEditorMapProperties(parent));
        }
        Check(Value(d)==100&&Value(d,0,224,10)==50&&d.EditorChangeCount==prior+1,"Common parent Apply lost reset values/counter.");
        d.Undo();Check(Value(d)==-11&&Value(d,0,224,10)==222,"Common parent Undo did not restore the complete child.");d.Redo();
        using(var edit=d.CreateCreatureLevelModifierSession(defaults)){Store(edit,77,-10000);d.ApplyCreatureLevelModifiers(edit);}
        using var save=new EditorSaveSession(d);var path=Path.Combine(directory,native+".dkm");Check(save.Save(save.PrepareSaveAs(path),true),"Inherited DKM Save refused.");
        var project=save.Document;var archives=save.LastSave!.SourceArchives.Select(File.ReadAllBytes).ToArray();
        if(native)Check(project.ReadCreatureLevelModifierDefaults().Count==320,"Conversion archive lost the shared source.");
        else Check(archives.Length==0,"Fresh typed DKM data pretended to have a native source archive.");
        using(var mixed=project.CreateCreatureLevelModifierSession(defaults))
        {
            Store(mixed,88,-10000);mixed.Draft.SelectStatistic(1,mixed.Draft.Current);var values=mixed.Draft.Current;
            var levels=values.Levels.ToArray();levels[0]=-33;mixed.Draft.Store(new(levels,values.FirstPerson));project.ApplyCreatureLevelModifiers(mixed);
        }
        Check(Value(project)==88&&Value(project,1)==-33,"Mixed update/insertion failed.");
        using(var guarded=project.CreateCreatureLevelModifierSession(defaults))
        {
            var metadata=(NativeFile)typeof(NativeMapDocument).GetField("nativeLevelModifierOwned",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(project)!;
            Check(MemoryMarshal.TryGetArray(metadata.Data,out var buffer),"No owned fixture buffer.");buffer.Array![buffer.Offset+24]^=1;
            Check(!project.IsCreatureLevelModifierSessionCurrent(guarded),"Raw D2LO changes retained session authority.");buffer.Array[buffer.Offset+24]^=1;
        }
        Check(save.Save(save.PrepareSave(),true),"Mixed DKM save refused.");var reopened=NativeMapDocument.Load(path);
        Check(Value(reopened)==88&&Value(reopened,1)==-33&&!reopened.HasChanges,"Mixed DKM reopen lost owned values/baseline.");
        var copy=reopened.SaveCopy(Path.Combine(directory,native+"-copy"));Check(Value(NativeMapDocument.Load(copy),1)==-33,"Mixed SaveCopy lost the source or owned values.");
        var recovery=Path.Combine(directory,native+"-recovery.dkm");save.SaveTemporary(recovery);Check(Value(NativeMapDocument.Load(recovery),1)==-33,"Owned recovery lost a value.");
        for(var i=0;i<archives.Length;i++)Check(File.ReadAllBytes(save.LastSave!.SourceArchives[i]).AsSpan().SequenceEqual(archives[i]),"Owned additions rewrote the original archive.");
        var game=reopened.CompileLegacyProject(Name,basis);var gamePath=game.SaveCopy(Path.Combine(directory,native+"-game"));
        Check(Directory.GetFiles(Path.GetDirectoryName(gamePath)!).Length==7&&Value(game)==88&&Value(game,1)==-33,"Inherited overrides did not compile to seven files.");
        if(native)MalformedCollision(path,save.LastSave!.SourceArchives.Single(),directory);
        else MalformedOwned(path,directory);
    }
    private static void FullTable(CreatureLevelModifierRecord[] defaults,NativeMapDocument basis,string directory)
    {
        var d=NativeMapDocument.CreateNewMap(new(Name,"All inherited",20,20));
        using(var edit=d.CreateCreatureLevelModifierSession(defaults))
        {
            foreach(var stat in CreatureLevelModifierControls.Statistics)
            {
                var id=uint.Parse(stat.Value);edit.Draft.SelectStatistic(id,edit.Draft.Current);
                edit.Draft.Store(new(Enumerable.Range(0,9).Select(i=>-10000+(int)id+i).ToArray(),10000-(int)id));
            }
            var buffers=((IEnumerable<NativeFile>)typeof(NativeMapDocument).GetMethod("TransactionFiles",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(d,null)!).Count();
            var field=typeof(NativeMapDocument).GetField("historyBufferCopy",BindingFlags.Instance|BindingFlags.NonPublic)!;var previous=field.GetValue(d);var calls=0;var revision=d.EditRevision;
            field.SetValue(d,new Func<byte[],byte[]>(b=>++calls==buffers+1?throw new OutOfMemoryException("After owned insertion"):b.ToArray()));
            try{try{d.ApplyCreatureLevelModifiers(edit);throw new Exception("Expected late owned allocation failure.");}catch(EditorHistoryMemoryException){}}
            finally{field.SetValue(d,previous);}
            Check(d.ReadExistingCreatureLevelModifiers().Count==0&&d.EditRevision==revision&&!edit.IsClosed,"Late allocation failure partially inserted owned records.");
            d.ApplyCreatureLevelModifiers(edit);
        }
        Check(d.ReadExistingCreatureLevelModifiers().Count==230,"Full visible inherited table was truncated.");
        using(var writer=new EditorProjectSaveSession(d))writer.SaveAs(Path.Combine(directory,"all230.dkm"));
        var reopened=NativeMapDocument.Load(Path.Combine(directory,"all230.dkm"));var compiled=reopened.CompileLegacyProject(Name,basis);
        Check(compiled.ReadExistingCreatureLevelModifiers().Count==230&&compiled.ReadExistingCreatureLevelModifiers().All(r=>unchecked((short)r.PercentBits)==(r.RecordType==65?-10000+(int)r.Statistic+(int)r.Level-2:10000-(int)r.Statistic)),"Full owned table did not survive Save/compile.");
    }
    private static void MalformedOwned(string path,string directory)
    {
        const uint signature=0x4f4c3244;var parsed=LegacySectionFile.Read(path);var original=parsed.Sections.Single(s=>s.Signature==signature);
        void Bad(string name,Action<byte[]> corrupt,bool duplicate=false)
        {
            var bytes=original.Payload.ToArray();corrupt(bytes);var sections=parsed.Sections.Select(s=>s.Signature==signature?new LegacySection(signature,1,bytes):s).ToList();if(duplicate)sections.Add(original);
            var target=Path.Combine(directory,"bad-own-"+name+".dkm");File.WriteAllBytes(target,LegacySectionFile.Create(sections).ToArray());Reject(()=>NativeMapDocument.Load(target));
        }
        Bad("count",b=>U(b,0,231));Bad("hidden",b=>U(b,8,8));Bad("range",b=>U(b,12,10001));Bad("bits",b=>U(b,12,0x0000ffff));
        Bad("type",b=>U(b,4,999));Bad("duplicate-key",b=>b.AsSpan(4,16).CopyTo(b.AsSpan(20,16)));Bad("duplicate-section",_=>{},true);
    }
    private static void MalformedCollision(string path,string archivePath,string directory)
    {
        const uint signature=0x4f4c3244;var parsed=LegacySectionFile.Read(path);var owned=parsed.Sections.Single(s=>s.Signature==signature);var bytes=owned.Payload.ToArray();
        U(bytes,4,65);U(bytes,8,0);U(bytes,16,2);var target=Path.Combine(directory,"bad-own-collision.dkm");
        File.WriteAllBytes(target,LegacySectionFile.Create(parsed.Sections.Select(s=>s.Signature==signature?new LegacySection(signature,1,bytes):s)).ToArray());
        var archive=EditorSourceArchive.Read(archivePath);File.Copy(archivePath,target+".native-source-"+archive.ContentHash+".zip");Reject(()=>NativeMapDocument.Load(target));
    }
}
