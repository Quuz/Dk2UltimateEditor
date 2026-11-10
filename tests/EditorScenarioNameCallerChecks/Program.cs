using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

var passed=0;
void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
void Reject(Action action){try{action();}catch(Exception ex)when(ex is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new InvalidOperationException("Invalid caller authority accepted.");}
string Name(NativeMapDocument d,ScenarioNameKind kind,int slot)=>d.ReadScenarioNames().Slots.Single(r=>r.Kind==kind&&r.Index==slot).StoredName;
foreach(var native in new[]{false,true})
{
    var format=native?"native":"project";
    NativeMapDocument New()=>NativeMapDocument.CreateNewMap(new("AliasCaller","AliasCaller",16,16,NativeFormat:native));
    void Pass(string text){Console.WriteLine($"PASS {format} {text}");passed++;}
    var d=New();var files=d.Files.ToDictionary(f=>f.ExportPath,f=>f.Data.ToArray());var history=d.GetHistory();var revision=d.EditRevision;var count=d.EditorChangeCount;
    var changed=0;var historyChanged=0;d.Changed+=(_,_)=>changed++;d.HistoryChanged+=(_,_)=>historyChanged++;
    d.ApplyEditorScenarioNameDraft(d.CreateScenarioNameDraft(ScenarioNameKind.Variable,0),"");
    Expect(d.EditRevision==revision+1&&d.EditorChangeCount==count+1&&d.HasChanges&&d.GetHistory().Undo.Count==history.Undo.Count&&d.GetHistory().ChangeCount==history.ChangeCount+1&&changed==1&&historyChanged==1&&d.Files.All(f=>f.Data.Span.SequenceEqual(files[f.ExportPath])),"Equal GUI acceptance did not count exactly once without bytes/Undo.");
    Pass("accepted equal alias counts once without bytes or Undo");
    d=New();d.SetScenarioName(ScenarioNameKind.Timer,2,"Generic");var h=d.GetHistory();revision=d.EditRevision;count=d.EditorChangeCount;
    d.SetScenarioName(ScenarioNameKind.Timer,2,"Generic");Expect(d.EditRevision==revision&&d.EditorChangeCount==count&&d.GetHistory().Undo.Count==h.Undo.Count,"Generic equal API consumed changes.");d.Undo();Expect(Name(d,ScenarioNameKind.Timer,2)=="","Generic Undo lost its own semantics.");d.Redo();Expect(Name(d,ScenarioNameKind.Timer,2)=="Generic","Generic Redo lost its own semantics.");
    Pass("trusted generic equal and changed APIs retain ordinary Undo and Redo");
    d=New();var eventRecord=native?d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="Trigger"),new(0,0)):d.AddLegacyRecord("Trigger",new(0,0));
    var after=d.Files.ToDictionary(f=>f.ExportPath,f=>f.Data.ToArray());h=d.GetHistory();count=d.EditorChangeCount;
    d.ApplyEditorScenarioNameDraft(d.CreateScenarioNameDraft(ScenarioNameKind.Variable,7),"Persistent");
    Expect(d.GetHistory().Undo.Count==h.Undo.Count&&d.GetHistory().SnapshotBytes==h.SnapshotBytes&&d.EditorChangeCount==count+1,"Changed GUI alias allocated/changed history memory.");
    d.Undo();Expect(Name(d,ScenarioNameKind.Variable,7)=="Persistent"&&d.Records().Count(r=>ScriptGraph.IsEvent(r))==0&&d.EditorChangeCount==count,"Undo event removed permanent alias/counter.");
    d.Redo();Expect(Name(d,ScenarioNameKind.Variable,7)=="Persistent"&&d.Records().Count(r=>ScriptGraph.IsEvent(r))==1&&d.EditorChangeCount==count+1,"Redo event lost alias/counter.");
    foreach(var f in d.Files){if(!native&&f.Signature==NativeMapDocument.LegacyEvents){var expected=after[f.ExportPath].ToArray();System.Text.Encoding.ASCII.GetBytes("Persistent").CopyTo(expected,expected.Length-146*64+7*64);Expect(f.Data.Span.SequenceEqual(expected),"Rebased project history changed unrelated event/header bytes.");}else Expect(f.Data.Span.SequenceEqual(after[f.ExportPath]),"Rebased native/history changed unrelated bytes.");}
    Pass("event length-changing Undo and Redo retain accepted alias and exact other bytes");
    d=New();d.SetScenarioName(ScenarioNameKind.Variable,7,"Previous");d.SetScenarioName(ScenarioNameKind.Timer,8,"Other");d.Undo();h=d.GetHistory();count=d.EditorChangeCount;
    d.ApplyEditorScenarioNameDraft(d.CreateScenarioNameDraft(ScenarioNameKind.Variable,7),"Manager");
    Expect(d.GetHistory().Redo.Count==h.Redo.Count&&d.GetHistory().Undo.Count==h.Undo.Count,"Manager name destroyed unrelated redo.");
    d.Redo();Expect(Name(d,ScenarioNameKind.Variable,7)=="Manager"&&Name(d,ScenarioNameKind.Timer,8)=="Other","Manager rebase changed unrelated slot.");d.Undo();d.Undo();Expect(Name(d,ScenarioNameKind.Variable,7)=="Manager"&&Name(d,ScenarioNameKind.Timer,8)==""&&d.EditorChangeCount==1,"Earlier generic name Undo restored manager-overridden alias.");
    Pass("retained redo and older generic alias history preserve manager slot only");
    d=New();var draft=d.CreateScenarioNameDraft(ScenarioNameKind.Variable,0);files=d.Files.ToDictionary(f=>f.ExportPath,f=>f.Data.ToArray());revision=d.EditRevision;count=d.EditorChangeCount;h=d.GetHistory();
    foreach(var bad in new[]{new string('x',64),"Имя","A\0B"})Reject(()=>d.ApplyEditorScenarioNameDraft(draft,bad));
    Reject(()=>New().ApplyEditorScenarioNameDraft(draft,"Foreign"));Reject(()=>d.Edit("Nested",()=>d.ApplyEditorScenarioNameDraft(draft,"Nested")));
    Expect(d.EditRevision==revision&&d.EditorChangeCount==count&&d.GetHistory().Undo.Count==h.Undo.Count&&d.Files.All(f=>f.Data.Span.SequenceEqual(files[f.ExportPath])),"Refused manager edit changed document.");
    d.ApplyEditorScenarioNameDraft(draft,"OK");Reject(()=>d.ApplyEditorScenarioNameDraft(draft,"Late"));Expect(Name(d,ScenarioNameKind.Variable,0)=="OK","Stale reuse changed alias.");
    Pass("invalid encoding foreign nested and reused draft refusal are atomic");
    d=New();d.SetHistoryDepth(0);count=d.EditorChangeCount;d.ApplyEditorScenarioNameDraft(d.CreateScenarioNameDraft(ScenarioNameKind.Timer,16),new string('é',63));
    Expect(Name(d,ScenarioNameKind.Timer,16)==new string('é',63)&&d.EditorChangeCount==count+1&&!d.CanUndo&&!d.CanRedo,"Zero depth or special timer/CP1252 boundary differs.");
    d.ApplyEditorScenarioNameDraft(d.CreateScenarioNameDraft(ScenarioNameKind.Variable,128),"ScoreAlias");Expect(Name(d,ScenarioNameKind.Variable,128)=="ScoreAlias","Score slot lost.");
    Pass("zero history and both special slots retain 63 byte CP1252 names");
}
Expect(passed==12,"Incomplete checks.");Console.WriteLine("EDITOR SCENARIO NAME CALLER CHECKS PASSED: 12");
