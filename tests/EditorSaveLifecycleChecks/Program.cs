using DK2MapEditor.Core.Editing;
var directory=Path.Combine(Path.GetTempPath(),"EditorSaveLifecycleChecks",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
void Refuses<T>(Action action) where T:Exception {try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
try
{
 foreach(var native in new[]{false,true})
 {
  var document=NativeMapDocument.CreateNewMap(new("LiveRecovery","LiveRecovery",18,18,NativeFormat:native));var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
  using var session=new EditorSaveSession(document);session.AutoSave.IntervalMinutes=1;
  var path=Path.Combine(directory,"pen-"+native+".dkm");var saved=document.ReadActivityStatistics()!.SavedTimes;
  var pen=document.BeginLivePenStroke(new(4,2,1));pen.Start(new(3,3));var revision=document.EditRevision;var changes=document.EditorChangeCount;
  Refuses<InvalidOperationException>(()=>session.PrepareSaveAs(Path.Combine(directory,"normal-"+native+".dkm")));
  for(var i=0;i<60;i++)Expect(!session.TickAutoSave(path),"Autosave fired before postincrement boundary.");
  Expect(session.TickAutoSave(path)&&document.HasActivePenStroke&&!pen.IsClosed&&document.EditorChangeCount==changes&&document.EditRevision==revision&&session.TargetPath is null,"Due autosave ended or changed active pen state.");
  var restored=NativeMapDocument.Load(path);
  Expect(restored.GetTile(3,3)==document.GetTile(3,3)&&restored.ReadStoredActivityStatistics()!.SavedTimes==saved+1&&document.ReadActivityStatistics()!.SavedTimes==saved,"Partial-frame recovery lost original saved-count policy.");
  var frame=File.ReadAllBytes(path);pen.Move(new(6,4));Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(frame)&&document.HasActivePenStroke,"Later live edit changed written recovery frame or ended capture.");
  pen.Dispose();Expect(document.GetHistory().Undo.Count==1&&document.CanUndo,"Autosave split the original one-gesture Undo.");document.Undo();
  Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&document.EditorChangeCount==0,"Post-autosave pen Undo did not restore all bytes/count.");document.Redo();Expect(document.GetTile(6,4)==new MapTile(4,2,1),"Post-autosave Redo lost later stroke segment.");
  Console.WriteLine("PASS "+(native?"native":"project")+" source timer saves current live pen frame, keeps capture/history/count, temporary statistics, later Move and exact Undo/Redo");
 }
 var fail=NativeMapDocument.CreateNewMap(new("FailurePen","FailurePen",18,18));using var failures=new EditorSaveSession(fail);var failurePath=Path.Combine(directory,"late-pen.dkm");File.WriteAllBytes(failurePath,[7,8,9]);var old=File.ReadAllBytes(failurePath);var count=fail.ReadActivityStatistics()!.SavedTimes;
 var failingPen=fail.BeginLivePenStroke(new(4,2,1));failingPen.Start(new(4,4));
 Refuses<IOException>(()=>failures.SaveTemporary(failurePath,path=>new FaultStream(path)));
 Expect(fail.HasActivePenStroke&&!failingPen.IsClosed&&File.ReadAllBytes(failurePath).AsSpan().SequenceEqual(old)&&fail.ReadActivityStatistics()!.SavedTimes==count,"Late temporary failure ended pen or replaced old recovery/count.");
 failingPen.Move(new(5,4));failingPen.Dispose();Expect(fail.CanUndo&&fail.GetHistory().Undo.Count==1,"Failed temporary save split pen history.");
 Console.WriteLine("PASS failed temporary live-pen write preserves recovery target/capture/count and one subsequent complete Undo");

 var root=Path.Combine(directory,"recovery-store");string recovery;
 using(var lifetime=new EditorRecoveryLifetime(root))
 {
  recovery=lifetime.RecoveryPath;using var save=new EditorSaveSession(NativeMapDocument.CreateNewMap(new("Lifetime","Lifetime",18,18)));save.Document.Paint([new(3,3)],new(4,2,1));save.SaveTemporary(recovery);
  Expect(EditorRecoveryLifetime.Find(root).Count==0&&EditorRecoveryLifetime.Find(root,includeCompleted:true).Count==0,"Live exclusive marker was mistaken for a crashed editor.");
 }
 Expect(EditorRecoveryLifetime.Find(root).Count==0&&EditorRecoveryLifetime.Find(root,includeCompleted:true).Single().Path==recovery&&File.Exists(recovery),"Clean close did not retain manual recovery while suppressing crash prompt.");
 var marker=Path.Combine(Path.GetDirectoryName(recovery)!,"open.flag");File.Move(marker+".closed",marker);
 var crashed=EditorRecoveryLifetime.Find(root).Single();Expect(crashed.Path==recovery,"Abandoned session marker was not detected.");var recoveryBytes=File.ReadAllBytes(recovery);var markerBytes=File.ReadAllBytes(marker);
 EditorRecoveryLifetime.Dismiss(root,crashed);Expect(EditorRecoveryLifetime.Find(root).Count==0&&File.ReadAllBytes(recovery).AsSpan().SequenceEqual(recoveryBytes)&&File.ReadAllBytes(marker+".dismissed").AsSpan().SequenceEqual(markerBytes),"Dismissal changed recovery or marker bytes.");
 Console.WriteLine("PASS exclusive live/crashed/completed recovery discovery, safe dismissal and retained manual recovery without input deletion");

 var nativeRecovery=NativeMapDocument.Create(18,18,"OriginalRecovered");nativeRecovery.SetScenarioName(ScenarioNameKind.Variable,0,"Persisted alias");var recoveryNativePath=Path.Combine(directory,"native-recovery.dkm");
 using(var save=new EditorSaveSession(nativeRecovery))save.SaveTemporary(recoveryNativePath);
 var recovered=NativeMapDocument.Load(recoveryNativePath);using var reopened=new EditorSaveSession(recovered,recoveryNativePath,recovered:true);
 Expect(reopened.TargetPath is null&&!reopened.CanSave&&!recovered.HasChanges,"Recovery incorrectly adopted ordinary target or fabricated changes.");
 var plan=reopened.PrepareSaveAs(Path.Combine(directory,"recovered-as.dkm"));Expect(reopened.IsCurrent(plan),"Current recovered plan rejected.");recovered.Paint([new(5,5)],new(4,2,1));Expect(!reopened.IsCurrent(plan),"Edited recovery retained stale Save plan.");
 reopened.Save(reopened.PrepareSaveAs(plan.Path),true);Expect(reopened.LastSave!.SourceArchives.Count==1&&EditorSourceArchive.Read(reopened.LastSave.SourceArchives.Single()).SourceFiles.Any(file=>file.ExportPath.Contains("alias",StringComparison.OrdinalIgnoreCase)||file.Bytes.Length==12+146*64),"Recovered Save As dropped raw alias source companion.");
 Console.WriteLine("PASS recovery opens without normal target, keeps clean original load state, rejects stale plan and retains source archive on actual Save As");
 Console.WriteLine("EDITOR SAVE LIFECYCLE CORE CHECKS PASSED: actual EN/RU Save/recovery windows and full integrated regression remain required");
}
finally
{
 var root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"EditorSaveLifecycleChecks"))+Path.DirectorySeparatorChar;var resolved=Path.GetFullPath(directory);
 if(!resolved.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Private fixture cleanup escaped root.");Directory.Delete(resolved,recursive:true);
}
class FaultStream(string path):FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)
{public override void Write(ReadOnlySpan<byte> buffer){base.Write(buffer[..Math.Min(32,buffer.Length)]);throw new IOException("Owned late-write failure.");}}
