using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class ScenarioExpansionChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception e)when(e is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new Exception("Invalid condition expansion accepted.");}
    private static void Raw(NativeFile f,int offset,ushort value)
    {Check(MemoryMarshal.TryGetArray(f.Data,out var b),"No fixture memory.");BinaryPrimitives.WriteUInt16LittleEndian(b.Array!.AsSpan(b.Offset+offset),value);}
    public static void Run(string repo)
    {
        var directory=Path.Combine(Path.GetTempPath(),"ScenarioExpansion-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            foreach(var native in new[]{false,true})Matrix(native,directory);
            SevenFiles(repo,directory);
        }
        finally{Directory.Delete(directory,true);}
    }
    private static void Matrix(bool native,string directory)
    {
        var d=NativeMapDocument.CreateNewMap(new("Expansion","Expansion",20,20,NativeFormat:native));
        var root=d.EnsureScriptScope(d.ScriptOwners().Single(o=>o.Scope==32));
        var condition=d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED"));var id=ScriptGraph.ID(condition);
        var action=d.AddScenarioEvent(0xD6,new(0,0));var actionID=ScriptGraph.ID(action);
        var source=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=d.EditorChangeCount;var history=d.GetHistory();var revision=d.EditRevision;var stats=d.ReadActivityStatistics()!.Encode();
        var changed=0;var changedHistory=0;var expectedChanged=0;var expectedHistory=0;d.Changed+=(_,_)=>changed++;d.HistoryChanged+=(_,_)=>changedHistory++;
        void State()=>Check(d.EditorChangeCount==count&&d.EditRevision==revision&&d.GetHistory().Undo.Count==history.Undo.Count&&d.GetHistory().Redo.Count==history.Redo.Count
            &&d.GetHistory().ChangeCount==history.ChangeCount&&d.GetHistory().SnapshotBytes==history.SnapshotBytes&&changed==expectedChanged&&changedHistory==expectedHistory&&d.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(stats),"Presentation change created a command/history/revision/activity event.");
        var expected=source.ToDictionary(p=>p.Key,p=>p.Value.ToArray());
        Check(d.ReadScenarioExpansion().Count==1&&!d.ReadScenarioExpansion()[id],"New condition expansion default differs.");State();
        Check(d.SetScenarioExpanded(id,true)&&d.ReadScenarioExpansion()[id]&&!d.SetScenarioExpanded(id,true),"Expansion did not retain one true/no-op value.");
        if(!native)BinaryPrimitives.WriteInt16LittleEndian(expected[condition.File].AsSpan(condition.Offset+14),1);
        Check(expected.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Expansion changed anything except the confirmed DKM short.");State();
        var snapshot=d.CreateCompilationSnapshot();Check(snapshot.ReadScenarioExpansion()[id],"Independent snapshot lost expansion.");snapshot.SetScenarioExpanded(id,false);Check(d.ReadScenarioExpansion()[id],"Snapshot expansion changed live document.");State();
        Reject(()=>d.SetScenarioExpanded(actionID,true));Reject(()=>d.SetScenarioExpanded(0,true));Reject(()=>d.SetScenarioExpanded(32000,true));State();
        Reject(()=>d.Edit("Forbidden presentation edit",()=>d.SetScenarioExpanded(id,false)));State();
        using(var pen=d.BeginLivePenStroke(new(4,1,1)))
        {
            expectedChanged=changed;expectedHistory=changedHistory;
            Reject(()=>d.SetScenarioExpanded(id,false));State();
        }
        // Pen lifetime emits its own availability callbacks even without a
        // stroke; the refused presentation setter must emit none of its own.
        expectedChanged=changed;expectedHistory=changedHistory;State();
        // Save plans must include native presentation despite zero game-byte
        // changes, so a late collapse cannot publish an older converted DKM.
        using(var save=new EditorSaveSession(d))
        {
            var plan=save.PrepareSaveAs(Path.Combine(directory,native+"-save.dkm"));Check(save.IsCurrent(plan),"Current presentation save is stale.");
            d.SetScenarioExpanded(id,false);Check(!save.IsCurrent(plan),"Presentation change retained stale Save authority.");d.SetScenarioExpanded(id,true);
        }
        State();
        if(native)
        {
            var conversion=d.PrepareProjectConversion(Path.Combine(directory,"native-expanded.dkm"));var mapped=conversion.NativeEventIDs[id];
            Check(conversion.Project.ReadScenarioExpansion()[mapped],"Native conversion did not project its condition flag into DKM offset14.");
            using var writer=new EditorProjectSaveSession(conversion.Project);writer.SaveAs(Path.Combine(directory,"native-expanded.dkm"));
            var reopened=NativeMapDocument.Load(Path.Combine(directory,"native-expanded.dkm"));Check(reopened.ReadScenarioExpansion()[mapped],"Native->DKM save/reopen lost expansion.");
            d.SetScenarioExpanded(id,false);Check(!conversion.IsCurrent,"Native collapse retained old conversion plan.");
            Check(source.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Native presentation touched game/alias bytes.");
        }
        else
        {
            var path=d.SaveCopy(Path.Combine(directory,"expanded-project"));var reopened=NativeMapDocument.Load(path);
            Check(reopened.ReadScenarioExpansion()[id],"DKM save/reopen lost expanded short.");
            var old=d.GetHistory().Undo.Count;d.SetField(condition,condition.Fields.Single(f=>f.Name=="param1"),"42");d.SetScenarioExpanded(id,false);d.Undo();
            Check(d.ReadScenarioExpansion()[id]&&d.GetHistory().Undo.Count==old,"Full Undo did not retain the captured expansion byte.");
            // Noncanonical imported nonzero shorts are only read, never
            // normalized on load or by a read-only refresh.
            Raw(condition.File,condition.Offset+14,0xBEEF);var opaque=condition.File.Data.ToArray();
            Check(d.ReadScenarioExpansion()[id]&&condition.File.Data.Span.SequenceEqual(opaque),"Reading expansion rewrote an imported nonzero short.");
            d.SetScenarioExpanded(id,false);Check(BinaryPrimitives.ReadUInt16LittleEndian(condition.File.Data.Span[(condition.Offset+14)..])==0,"Explicit collapse did not write the source short.");
            var version=BinaryPrimitives.ReadUInt32LittleEndian(condition.File.Data.Span[4..]);Raw(condition.File,4,99);
            Check(d.ReadScenarioExpansion().Count==0,"Unknown KTRG version became editable.");Reject(()=>d.SetScenarioExpanded(id,true));Raw(condition.File,4,(ushort)version);
        }
        var bad=NativeMapDocument.CreateNewMap(new("BadExpansion","Bad expansion",20,20,NativeFormat:native));var badCondition=bad.AddScenarioEvent(0xD5,new(0,0));
        Raw(badCondition.File,badCondition.Offset+(native?16:48),0);Check(bad.ReadScenarioExpansion().Count==0,"Zero-ID graph offered a stored expansion.");Reject(()=>bad.SetScenarioExpanded(0,true));
        var clean=NativeMapDocument.Load(d.SaveCopy(Path.Combine(directory,"clean-"+native)));var cleanID=clean.ReadScenarioExpansion().Keys.First();
        Check(!clean.HasChanges,"Saved fixture did not reopen clean.");clean.SetScenarioExpanded(cleanID,!clean.ReadScenarioExpansion()[cleanID]);
        Check(!clean.HasChanges&&!clean.CanUndo&&clean.EditorChangeCount==0,"Presentation alone marked the clean document dirty or created history.");
        var cleanRecord=ScriptGraph.Build(clean).Events[cleanID].Record;
        var parameterOffset=cleanRecord.Offset+(native?8:16);var originalParameter=BinaryPrimitives.ReadUInt16LittleEndian(cleanRecord.File.Data.Span[parameterOffset..]);
        Raw(cleanRecord.File,parameterOffset,123);
        Check(clean.HasChanges,"Ignoring expansion concealed a genuine parameter change.");
        if(!native)
        {
            Raw(cleanRecord.File,parameterOffset,originalParameter);Check(!clean.HasChanges,"Restoring parameters did not leave only presentation changes.");Raw(cleanRecord.File,cleanRecord.Offset+12,123);
            Check(clean.HasChanges,"Ignoring expansion concealed an opaque predecessor change.");
        }
    }
    private static void SevenFiles(string repo,string directory)
    {
        const string name="DK2Preview_6a88240eaf814f78";
        var project=NativeMapDocument.Load(Path.Combine(repo,"tests/Fixtures/NativeRoundtrip",name+".dkm"));
        var basis=NativeMapDocument.Load(Path.Combine(repo,"artifacts/PreFinal/TestMap",name+".kwd"));
        var untouched=project.CreateCompilationSnapshot();var flags=project.ReadScenarioExpansion();Check(flags.Count>0,"Official export fixture has no condition.");
        foreach(var row in flags)project.SetScenarioExpanded(row.Key,!row.Value);
        Dictionary<string,byte[]> Export(NativeMapDocument source,string target)
        {
            var compiled=source.CompileLegacyProject(name,basis);var path=compiled.SaveCopy(Path.Combine(directory,target));
            var files=Directory.GetFiles(Path.GetDirectoryName(path)!);Check(files.Length==7,"Expansion changed the official seven-file count.");
            return files.ToDictionary(p=>Path.GetFileName(p),File.ReadAllBytes);
        }
        var before=Export(untouched,"game-before");
        // Compiler_SetDate/GetLocalTime intentionally stamps each compilation.
        // Cross a second boundary instead of depending on a fast machine, then
        // exclude only the fourteen clock bytes; padding and all operands stay
        // in the complete file comparison. CompilerGameDateChecks tests dates.
        Thread.Sleep(1100);
        var after=Export(project,"game-after");
        byte[] Comparable(byte[] bytes)
        {
            var copy=bytes.ToArray();var signature=BinaryPrimitives.ReadUInt32LittleEndian(copy);
            void Clock(int offset)
            {foreach(var field in new[]{0,1,2,3,6,7,8,10,11,12,13,16,17,18})copy[offset+field]=0;}
            if(signature is 0xDC or 0xB4 or 0xBE or 0xD2 or 0xE6)
                Clock(signature==0xD2?32:28);
            else if(signature is 0x6E or 0x78 or 0x8C or 0x82 or 0x96 or 0xAA or 0xA0)
                for(var frame=0;frame<copy.Length;)
                {
                    Check(frame<=copy.Length-56,"Truncated packed definition header in official export.");
                    var span=copy.AsSpan(frame);var kind=BinaryPrimitives.ReadUInt32LittleEndian(span);var length=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(span[8..]));
                    Check(BinaryPrimitives.ReadUInt32LittleEndian(span[4..])==4&&length>=56&&length<=span.Length
                        &&BinaryPrimitives.ReadUInt32LittleEndian(span[12..])==kind+1&&BinaryPrimitives.ReadUInt32LittleEndian(span[48..])==kind+2,"Unverified packed definition frame in official export.");
                    if(kind is 0x6E or 0x78 or 0x8C or 0x82 or 0x96 or 0xAA or 0xA0)Clock(frame+28);
                    frame=checked(frame+length);
                }
            return copy;
        }
        var differences=before.SelectMany(pair=>Enumerable.Range(0,pair.Value.Length).Where(index=>after[pair.Key][index]!=pair.Value[index]).Select(index=>pair.Key+":0x"+index.ToString("X"))).ToArray();
        Check(differences.Length!=0,"The clock-boundary fixture did not exercise regenerated timestamps.");
        Console.WriteLine("EXPANSION GAME CLOCK DETAIL "+string.Join(",",differences));
        Check(before.Count==after.Count&&before.All(pair=>Comparable(after[pair.Key]).AsSpan().SequenceEqual(Comparable(pair.Value))),"Editor expansion leaked into game operands, padding or non-clock file bytes.");
    }
}
