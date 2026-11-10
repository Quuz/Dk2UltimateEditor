using System.Reflection;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

if(args.Length!=1||Directory.Exists(args[0]))throw new ArgumentException("A new private fixture directory is required.");
var directory=Path.GetFullPath(args[0]);Directory.CreateDirectory(directory);var passed=0;
void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
void Reject(Action action){try{action();}catch(Exception ex)when(ex is ArgumentException or InvalidOperationException or OverflowException){return;}throw new InvalidOperationException("Invalid name caller accepted.");}
string Name(NativeMapDocument d)=>d.ReadScenarioNames().Slots.Single(r=>r.Kind==ScenarioNameKind.Timer&&r.Index==0).StoredName;
foreach(var native in new[]{false,true})
{
    var format=native?"native":"project";NativeMapDocument New()=>NativeMapDocument.CreateNewMap(new("AliasHistory","AliasHistory",16,16,NativeFormat:native));
    void Pass(string text){Console.WriteLine($"PASS {format} {text}");passed++;}
    var d=New();d.SetScenarioName(ScenarioNameKind.Timer,0,"Before");d.Paint([new(4,4)],new(4,2,1));d.Undo();
    typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(d,uint.MaxValue);
    var draft=d.CreateScenarioNameDraft(ScenarioNameKind.Timer,0);var h=d.GetHistory();d.ApplyEditorScenarioNameDraft(draft,"Wrapped");
    Expect(d.EditorChangeCount==0&&d.GetHistory().Undo.Count==h.Undo.Count&&d.GetHistory().Redo.Count==h.Redo.Count,"Source 32-bit wrap or history shape differs.");
    d.Redo();Expect(Name(d)=="Wrapped","Wrapped redo lost permanent name.");d.Undo();d.Undo();Expect(Name(d)=="Wrapped"&&d.EditorChangeCount==1,"Older undo lost accepted wrapped counter/name.");
    Pass("source counter wraps and prior Undo Redo counters retain accepted naming operation");
    d=New();draft=d.CreateScenarioNameDraft(ScenarioNameKind.Timer,0);var revision=d.EditRevision;var count=d.EditorChangeCount;
    var tile=d.Files.Single(f=>f==d.TileFile);Expect(MemoryMarshal.TryGetArray(tile.Data,out var backing),"Raw backing unavailable.");var offset=backing.Offset+36;var old=backing.Array![offset];backing.Array[offset]^=1;
    Reject(()=>d.ApplyEditorScenarioNameDraft(draft,"Raw"));backing.Array[offset]=old;
    using(var stroke=d.BeginLivePenStroke(new(4,2,1)))Reject(()=>d.ApplyEditorScenarioNameDraft(draft,"Gesture"));
    Expect(d.EditRevision==revision&&d.EditorChangeCount==count&&Name(d)=="","Raw/active gesture refusal changed alias/count.");
    Pass("raw byte mutations and active gesture cannot reuse name draft authority");
    d=New();draft=d.CreateScenarioNameDraft(ScenarioNameKind.Timer,0);revision=d.EditRevision;count=d.EditorChangeCount;
    var counterField=typeof(NativeMapDocument).GetField("changeCount",BindingFlags.Instance|BindingFlags.NonPublic)!;counterField.SetValue(d,long.MaxValue);
    Reject(()=>d.ApplyEditorScenarioNameDraft(draft,"Overflow"));Expect(Name(d)==""&&d.EditorChangeCount==count&&d.EditRevision==revision&&d.GetHistory().ChangeCount==long.MaxValue,"Counter overflow partially committed alias/history.");
    Pass("modern counter overflow refuses before alias history or source counters change");
    d=New();d.ApplyEditorScenarioNameDraft(d.CreateScenarioNameDraft(ScenarioNameKind.Timer,0),"Saved");
    var saved=d.SaveCopy(Path.Combine(directory,$"saved-{format}"));var reopened=NativeMapDocument.Load(saved);Expect(Name(reopened)=="Saved"&&!reopened.HasChanges,"Save/reopen lost nonhistorical alias or clean baseline.");
    var saveBytes=reopened.Files.ToDictionary(f=>f.ExportPath,f=>f.Data.ToArray());reopened.ApplyEditorScenarioNameDraft(reopened.CreateScenarioNameDraft(ScenarioNameKind.Timer,0),"Saved");
    Expect(reopened.HasChanges&&!reopened.CanUndo&&reopened.EditorChangeCount==1&&reopened.Files.All(f=>f.Data.Span.SequenceEqual(saveBytes[f.ExportPath])),"Equal name on clean reopened map lost source dirty/count semantics.");
    Pass("saved name reopens clean and equal GUI acceptance becomes dirty without Undo");
}
var seed=NativeMapDocument.CreateNewMap(new("HistoricalNames","HistoricalNames",16,16));var seedPath=seed.SaveCopy(Path.Combine(directory,"historical-seed"));var project=LegacySectionFile.Read(seedPath);var events=project.Sections.Single(s=>s.Signature==NativeMapDocument.LegacyEvents);
foreach(var version in new[]{1,2})
{
    var replacement=new LegacySection(events.Signature,version,version==1?new byte[4]:events.Payload);
    var bytes=LegacySectionFile.Create(project.Sections.Select(s=>s==events?replacement:s)).ToArray();var path=Path.Combine(directory,$"v{version}.dkm");File.WriteAllBytes(path,bytes);var d=NativeMapDocument.Load(path);
    d.UpgradeLegacyProject();var h=d.GetHistory();d.ApplyEditorScenarioNameDraft(d.CreateScenarioNameDraft(ScenarioNameKind.Timer,0),"Historical");Expect(d.GetHistory().Undo.Count==h.Undo.Count,"Historical acceptance added Undo.");
    d.Undo();if(version==1)Expect(!d.CanEditScenarioNames,"V1 undo invented editable alias table.");else Expect(Name(d)=="Historical","V2 undo lost manager alias.");
    d.Redo();Expect(Name(d)=="Historical"&&d.CanEditScenarioNames,"Historical upgrade redo lost accepted alias.");
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes),"Historical input file modified.");
    Console.WriteLine($"PASS project version{version} upgrade Undo Redo preserves original input and accepted name in supported tables");passed++;
}
Expect(passed==10,"Incomplete historical caller checks.");Console.WriteLine("EDITOR SCENARIO NAME HISTORICAL CHECKS PASSED: 10");
