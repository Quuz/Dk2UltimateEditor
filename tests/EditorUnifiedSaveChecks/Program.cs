using System.IO.Compression;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;
var directory=Path.Combine(Path.GetTempPath(),"EditorUnifiedSaveChecks",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
string P(string name)=>Path.Combine(directory,name);
void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
void Refuses<T>(Action action) where T:Exception {try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
NativeMapDocument Project()=>NativeMapDocument.CreateNewMap(new(Width:18,Height:18,DisplayName:"Save test",FileName:"SaveTest"));
NativeMapDocument Native(string name)=>NativeMapDocument.Create(18,18,name);
void Paint(NativeMapDocument doc,int x=4)=>doc.Paint([new(x,4)],new(4,2,1));
Dictionary<NativeFile,byte[]> Files(NativeMapDocument doc)=>doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
void SameFiles(Dictionary<NativeFile,byte[]> before)=>Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Working input bytes changed.");
try
{
 var project=Project();Paint(project);using var save=new EditorSaveSession(project);var events=0;
 save.Saved+=(_,result)=>{events++;Expect(ReferenceEquals(result.Document,save.Document),"Event did not bind current document.");};
 var first=save.PrepareSaveAs(P("ordinary.dkm"));Expect(first.Format==EditorSaveFormat.Dkm&&!first.RetainsNativeSource,"Ordinary DKM incorrectly converts.");
 Expect(save.Save(first,true)&&ReferenceEquals(project,save.Document)&&save.TargetPath==first.Path&&!project.HasChanges&&!project.CanUndo&&events==1,"Ordinary Save did not adopt target/baseline.");
 Paint(project,5);Expect(save.CanSave,"Saved session cannot save edit.");save.Save(save.PrepareSave(),true);var reopened=NativeMapDocument.Load(first.Path);
 Expect(reopened.GetTile(5,4)==project.GetTile(5,4)&&reopened.ReadStoredActivityStatistics()?.SavedTimes==project.ReadActivityStatistics()?.SavedTimes,"Repeat Save lost edits/statistics.");
 var map=save.PrepareSaveAs(P("ordinary.map"));var stats=project.ReadActivityStatistics()!.Encode();save.Save(map,true);
 Expect(save.TargetFormat==EditorSaveFormat.Map&&ReferenceEquals(project,save.Document)&&stats.AsSpan().SequenceEqual(project.ReadActivityStatistics()!.Encode()),"MAP changed DKM statistics/source.");
 Paint(project,6);save.Save(save.PrepareSave(),true);Expect(NativeMapDocument.Load(map.Path).GetTile(6,4)==project.GetTile(6,4),"MAP lost selected target/edit.");
 Console.WriteLine("PASS unified ordinary DKM/MAP target, actual repeated Save/reopen, baseline/history reset, event ownership and statistics separation");

 var native=Native("Retained");var metadata=native.Records().Single(record=>record.Category=="Map");native.SetField(metadata,metadata.Fields.Single(field=>field.Name=="wstrMainName"),"Retained Ω");Paint(native);native.SetScenarioName(ScenarioNameKind.Variable,0,"Retained alias");var bytes=Files(native);var count=native.EditorChangeCount;var revision=native.EditRevision;
 var prepared=native.PrepareProjectConversion();var preparedStatistics=native.ReadActivityStatistics()!.Encode();using var nativeSave=new EditorSaveSession(native);
 var plan=nativeSave.PrepareSaveAs(P("native.dkm"));Expect(plan.RetainsNativeSource&&plan.NeedsLossConsent,"Native preparation lacks retention/loss report.");
 Expect(!nativeSave.Save(plan,false)&&!File.Exists(plan.Path)&&nativeSave.TargetPath is null&&nativeSave.LastSave is null,"Decline published/adopted output.");
 nativeSave.Save(plan,true);Expect(!ReferenceEquals(nativeSave.Document,native)&&nativeSave.Document.IsLegacyProject&&!nativeSave.Document.HasChanges&&!nativeSave.Document.CanUndo,"Converted document not adopted clean.");
 SameFiles(bytes);Expect(native.EditorChangeCount==count&&native.EditRevision==revision&&native.CanUndo,"Replaced source lost history.");
 var archivePath=nativeSave.LastSave!.SourceArchives.Single();var archive=EditorSourceArchive.Read(archivePath);
 Expect(archive.SourceFiles.Length==prepared.SourceFiles.Count&&archive.Statistics.AsSpan().SequenceEqual(preparedStatistics),"Archive lost files/statistics.");
 foreach(var original in prepared.SourceFiles){var retained=archive.SourceFiles.Single(file=>file.SourcePath==original.SourcePath&&file.ExportPath==original.ExportPath);Expect(retained.Bytes.AsSpan().SequenceEqual(original.Bytes.AsSpan()),"Persistent raw file/alias/target/fill differs.");}
 using(var zip=ZipFile.OpenRead(archivePath))Expect(zip.Entries.Count==archive.SourceFiles.Length+1&&zip.Entries.All(entry=>entry.FullName=="manifest.json"||entry.FullName.StartsWith("files/",StringComparison.Ordinal)),"Archive used source paths as entries.");
 Paint(nativeSave.Document,7);nativeSave.Save(nativeSave.PrepareSave(),true);var copy=nativeSave.PrepareSaveAs(P("native-copy.dkm"));nativeSave.Save(copy,true);
 Expect(nativeSave.LastSave!.SourceArchives.Count==1&&EditorSourceArchive.Read(nativeSave.LastSave.SourceArchives[0]).ContentHash==archive.ContentHash&&File.Exists(archivePath),"Repeat/Save As dropped source.");
 Expect(NativeMapDocument.Load(copy.Path).GetTile(7,4)==nativeSave.Document.GetTile(7,4),"Converted repeat Save lost edit.");
 Console.WriteLine("PASS native DKM conversion, explicit loss refusal, immutable companion all files/aliases, source preservation and repeated Save As retention");

 var reload=NativeMapDocument.Load(copy.Path);using var loaded=new EditorSaveSession(reload,copy.Path);Paint(reload,8);loaded.Save(loaded.PrepareSaveAs(P("reopened-copy.dkm")),true);
 Expect(loaded.LastSave!.SourceArchives.Count==1&&EditorSourceArchive.Read(loaded.LastSave.SourceArchives[0]).ContentHash==archive.ContentHash,"Reopened Save As dropped archive.");
 var corrupt=P("corrupt.dkm");File.Copy(copy.Path,corrupt);File.WriteAllBytes(corrupt+".native-source-"+archive.ContentHash+".zip",[1,2,3]);
 Refuses<InvalidDataException>(()=>new EditorSaveSession(NativeMapDocument.Load(corrupt),corrupt));
 Console.WriteLine("PASS loaded DKM companion discovery, Save As copy, hash validation and explicit corrupt archive refusal");

 var failedNative=Native("FailNative");Paint(failedNative);using var failed=new EditorSaveSession(failedNative);var failure=failed.PrepareSaveAs(P("failure.dkm"));File.WriteAllBytes(failure.Path,[8,7,6]);var prior=File.ReadAllBytes(failure.Path);var failedBytes=Files(failedNative);var failedCount=failedNative.EditorChangeCount;
 Refuses<IOException>(()=>failed.Save(failure,true,createArchiveOutput:_=>throw new IOException("archive creation fails")));
 Expect(File.ReadAllBytes(failure.Path).AsSpan().SequenceEqual(prior)&&failed.TargetPath is null&&ReferenceEquals(failed.Document,failedNative)&&failed.LastSave is null,"Archive failure changed target.");
 Refuses<IOException>(()=>failed.Save(failure,true,createOutput:_=>throw new IOException("project creation fails")));
 Expect(File.ReadAllBytes(failure.Path).AsSpan().SequenceEqual(prior)&&failed.TargetPath is null&&failedNative.EditorChangeCount==failedCount,"Early DKM failure changed state.");
 var oldSaved=failedNative.ReadActivityStatistics()!.SavedTimes;Refuses<IOException>(()=>failed.Save(failure,true,createOutput:path=>new FaultStream(path)));
 Expect(File.ReadAllBytes(failure.Path).AsSpan().SequenceEqual(prior)&&failed.TargetPath is null&&failedNative.ReadActivityStatistics()!.SavedTimes==oldSaved+1,"Late failure lost target/source writer effects.");
 SameFiles(failedBytes);Expect(failedNative.CanUndo&&!Directory.EnumerateFiles(directory,".dk2-*.tmp").Any(),"Failure leaked temporary file or cleared Undo.");
 Console.WriteLine("PASS source archive/early/late DKM failures preserve old target, ownership/history and source writer effects without staging leaks");

 var stale=Native("Stale");Paint(stale);using var staleSave=new EditorSaveSession(stale);var stalePlan=staleSave.PrepareSaveAs(P("stale.dkm"));stale.RecordActivity(ActivityCounterEvent.LeftMapClick);
 Refuses<InvalidOperationException>(()=>staleSave.Save(stalePlan,true));Expect(!File.Exists(stalePlan.Path),"Stale statistics published.");
 var late=staleSave.PrepareSaveAs(P("late.dkm"));Refuses<InvalidOperationException>(()=>staleSave.Save(late,true,createOutput:path=>new CallbackStream(path,()=>stale.RecordActivity(ActivityCounterEvent.LeftMapClick))));Expect(!File.Exists(late.Path)&&staleSave.TargetPath is null,"Late mutation escaped guard.");
 var closed=Native("Closed");Paint(closed);var closedSave=new EditorSaveSession(closed);var closedPlan=closedSave.PrepareSaveAs(P("closed.dkm"));
 Refuses<ObjectDisposedException>(()=>closedSave.Save(closedPlan,true,createOutput:path=>new CallbackStream(path,closedSave.Dispose)));Expect(!File.Exists(closedPlan.Path),"Closed owner published DKM.");
 var text=Project();Paint(text);var textSave=new EditorSaveSession(text);var textPlan=textSave.PrepareSaveAs(P("closed.map"));
 Refuses<ObjectDisposedException>(()=>textSave.Save(textPlan,true,createOutput:path=>new CallbackStream(path,textSave.Dispose)));Expect(!File.Exists(textPlan.Path)&&text.CanUndo,"Closed owner published MAP/reset Undo.");
 Console.WriteLine("PASS unified stale statistics, late source mutation, disposed DKM/MAP owners and no-publication guards");

 var temporary=Native("Recovery");Paint(temporary);using var recovery=new EditorSaveSession(temporary);var beforeTemporary=Files(temporary);var saved=temporary.ReadActivityStatistics()!.SavedTimes;var changes=temporary.EditorChangeCount;var tempRevision=temporary.EditRevision;var recoveryPath=P("recovery.dkm");
 recovery.SaveTemporary(recoveryPath);var recoveryFile=NativeMapDocument.Load(recoveryPath);
 Expect(ReferenceEquals(recovery.Document,temporary)&&recovery.TargetPath is null&&temporary.EditorChangeCount==changes&&temporary.CanUndo&&temporary.EditRevision==tempRevision,"Temporary conversion adopted state.");
 Expect(temporary.ReadActivityStatistics()!.SavedTimes==saved&&recoveryFile.ReadStoredActivityStatistics()!.SavedTimes==saved+1&&temporary.ReadActivityStatistics()!.LastSaved.Year!=0,"Temporary native writer lost count/date behavior.");
 SameFiles(beforeTemporary);Expect(recovery.LastSave!.Temporary&&File.Exists(recovery.LastSave.SourceArchives.Single()),"Recovery lacks full source.");
 recovery.AutoSave.IntervalMinutes=1;for(var i=0;i<60;i++)Expect(!recovery.TickAutoSave(P("auto.dkm")),"Autosave early.");
 Expect(recovery.TickAutoSave(P("auto.dkm"))&&recovery.AutoSave.Ticks==0&&recovery.TargetPath is null&&temporary.CanUndo,"Autosave failed original boundary/target policy.");
 Console.WriteLine("PASS temporary native DKM and immutable recovery companion, original count/date/history preservation and unified autosave boundary");

 var blocking=Native("Blocking");var trigger=blocking.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="Trigger"),new(4,4),3);
 blocking.SetField(trigger,trigger.Fields.Single(field=>field.Name=="NextOr"),"32000");using var blocked=new EditorSaveSession(blocking);var blockedPlan=blocked.PrepareSaveAs(P("blocked.dkm"));
 Expect(blockedPlan.HasBlockingIssues,"Malformed graph not reported.");Refuses<InvalidDataException>(()=>blocked.Save(blockedPlan,true));Expect(!File.Exists(blockedPlan.Path),"Blocking conversion published normal DKM.");
 blocked.SaveTemporary(P("blocked-recovery.dkm"));Expect(EditorSourceArchive.Read(blocked.LastSave!.SourceArchives.Single()).Issues.Any(issue=>issue.Severity==ProjectConversionSeverity.Error)&&ReferenceEquals(blocked.Document,blocking),"Recovery lost malformed bytes/report.");
 Refuses<ArgumentException>(()=>new EditorSaveSession(blocking,P("wrong-target.dkm")));Refuses<ArgumentException>(()=>blocked.PrepareSaveAs(P("wrong.kwd")));
 Console.WriteLine("PASS blocking conversion normal refusal, temporary malformed raw-data retention and native target/path validation");
 Console.WriteLine("EDITOR UNIFIED SAVE CORE CHECKS PASSED: real window/recovery integration/full regression/delivery remain required");
}
finally
{
 var full=Path.GetFullPath(directory);var parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"EditorUnifiedSaveChecks"))+Path.DirectorySeparatorChar;
 if(!full.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Private cleanup escaped fixture root.");Directory.Delete(full,recursive:true);
}
class CallbackStream(string path,Action callback) : FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)
{public override void Flush(){base.Flush();callback();}}
class FaultStream(string path) : FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)
{public override void Write(ReadOnlySpan<byte> buffer){base.Write(buffer[..Math.Min(buffer.Length,32)]);throw new IOException("late write failure");}}
