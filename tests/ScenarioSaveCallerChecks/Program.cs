using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

var root = Path.GetFullPath(args.Single());
using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"tests/DK2MapEditor.Validation/ScenarioCreationDefaults.json")));
foreach (var item in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
    Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,item.Name)))).Equals(item.Value.GetString(),StringComparison.OrdinalIgnoreCase), "Original source oracle hash");
var clear = fixture.RootElement.GetProperty("CreationAndSaveFlow").GetProperty("Trigger").GetProperty("ZeroOnSaveTypes").EnumerateArray().Select(item=>item.GetString()!).ToHashSet();
using var pages = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"tests/DK2MapEditor.Validation/ScenarioParameterPages.json")));
var eligible = pages.RootElement.EnumerateArray().Where(page=>page.GetProperty("Signature").GetUInt32()==213 || page.GetProperty("PageID").GetInt32()!=-1).Select(page=>page.GetProperty("Type").GetString()!).ToArray();
Check(eligible.Length==95 && clear.Count==20,"Independent source scope");
foreach(var native in new[]{false,true})
{
    var format=native?"native":"project";
    var doc=New(native); doc.SetHistoryDepth(65535);
    foreach(var type in eligible)
    {
        var row=Add(doc,native,type,true); var before=Snapshot(doc); var history=doc.GetHistory(); var revision=doc.EditRevision;
        var draft=doc.CreateScenarioParameterDraft(row);
        doc.ApplyScenarioParameterDraft(draft); Exact(before);
        Check(doc.EditRevision==revision,"Generic no-op changed revision");
        SameHistory(history,doc.GetHistory());
        var expected=before.ToDictionary(item=>item.Key,item=>(byte[])item.Value.Clone());
        if(clear.Contains(type)) Array.Clear(expected[row.File],row.Offset+(native?8:16),native?8:32);
        var hc=0;var dc=0; EventHandler onHistory=(_,_)=>hc++; EventHandler onChanged=(_,_)=>dc++;
        doc.HistoryChanged+=onHistory;doc.Changed+=onChanged;
        doc.ApplyScenarioEditorParameterDraft(draft);
        doc.HistoryChanged-=onHistory;doc.Changed-=onChanged;
        Exact(expected); var after=doc.GetHistory();
        Check(after.ChangeCount==history.ChangeCount+1 && after.EditorChangeCount==unchecked(history.EditorChangeCount+1) && doc.EditRevision==revision+1 && hc==1 && dc==1,"Accepted Save source count/revision/events "+type);
        Check(after.Undo.Count==history.Undo.Count+(clear.Contains(type)?1:0),"Changed-only modern Undo "+type);
        if(clear.Contains(type)) {doc.Undo();Exact(before);doc.Redo();Exact(expected);}
        var accepted=doc.CreateScenarioParameterDraft(row); var equalHistory=doc.GetHistory();var equalRevision=doc.EditRevision;
        doc.ApplyScenarioEditorParameterDraft(accepted);Exact(expected);
        var equalAfter=doc.GetHistory();
        Check(equalAfter.Undo.SequenceEqual(equalHistory.Undo) && equalAfter.Redo.SequenceEqual(equalHistory.Redo) && equalAfter.SnapshotBytes==equalHistory.SnapshotBytes,"Equal Save history payload/cost "+type);
        Check(equalAfter.ChangeCount==equalHistory.ChangeCount+1 && equalAfter.EditorChangeCount==unchecked(equalHistory.EditorChangeCount+1) && doc.EditRevision==equalRevision+1,"Repeated equal accepted Save "+type);
        Reject(()=>doc.ApplyScenarioEditorParameterDraft(accepted));Exact(expected);
        doc.DeleteRecord(row);
    }
    Console.WriteLine($"PASS {format} 95 source pages: 20 normalization, 75 retained, generic no-op, accepted and repeated counts, bytes, events, stale guards");

    doc=New(native); var timer=Add(doc,native,"TRIGGER_TIMER",false);
    doc.Paint([new(2,2)],new(5,2,1)); doc.Paint([new(3,2)],new(5,2,1));
    var applied=Snapshot(doc); var appliedHistory=doc.GetHistory(); doc.Undo(); var undone=Snapshot(doc); var undoneHistory=doc.GetHistory();
    doc.ApplyScenarioEditorParameterDraft(doc.CreateScenarioParameterDraft(timer));Exact(undone);
    var acceptedHistory=doc.GetHistory();
    Check(acceptedHistory.Undo.SequenceEqual(undoneHistory.Undo) && acceptedHistory.Redo.SequenceEqual(undoneHistory.Redo) && acceptedHistory.SnapshotBytes==undoneHistory.SnapshotBytes,"Equal Save must retain redo");
    doc.Redo();Exact(applied); Check(doc.GetHistory().ChangeCount==appliedHistory.ChangeCount+1 && doc.EditorChangeCount==unchecked(appliedHistory.EditorChangeCount+1),"Redo carries accepted count");
    doc.Undo();Exact(undone);Check(doc.GetHistory().ChangeCount==undoneHistory.ChangeCount+1 && doc.EditorChangeCount==unchecked(undoneHistory.EditorChangeCount+1),"Undo carries accepted count");
    var repeat=native?"Repeat":"repeat"; var repeatField=timer.Fields.Single(field=>field.Name==repeat);
    var baseline=Snapshot(doc);var beforeChange=doc.GetHistory();
    var prepared=doc.UpdateScenarioParameterDraft(doc.CreateScenarioParameterDraft(timer),[new(repeat,"254")]);
    doc.ApplyScenarioEditorParameterDraft(prepared); var modified=Snapshot(doc);
    var expectedChanged=baseline.ToDictionary(item=>item.Key,item=>(byte[])item.Value.Clone());repeatField.Encode("254").CopyTo(expectedChanged[timer.File],timer.Offset+repeatField.Offset);
    Exact(expectedChanged);Check(doc.GetHistory().Undo.Count==beforeChange.Undo.Count+1 && doc.GetHistory().Redo.Count==0 && doc.GetHistory().ChangeCount==beforeChange.ChangeCount+1,"Changed Save remains one modern Undo");
    doc.Undo();Exact(baseline);doc.Redo();Exact(modified);
    Console.WriteLine($"PASS {format} equal Save keeps redo and rebases both history directions; changed Save remains one exact modern Undo");

    var current=doc.CreateScenarioParameterDraft(timer);var guarded=Snapshot(doc);var guardHistory=doc.GetHistory();var guardRevision=doc.EditRevision;
    Reject(()=>New(native).ApplyScenarioEditorParameterDraft(current));
    Reject(()=>doc.UpdateScenarioParameterDraft(current,[new(repeat,"256")]));
    doc.Edit("Nested guard",()=>Reject(()=>doc.ApplyScenarioEditorParameterDraft(current)));
    using(var pen=doc.BeginLivePenStroke(new(5,2,1))) Reject(()=>doc.ApplyScenarioEditorParameterDraft(current));
    Exact(guarded);SameHistory(guardHistory,doc.GetHistory());Check(doc.EditRevision==guardRevision,"Rejected draft mutated revision");
    var bytes=Mutable(timer.File);var at=timer.Offset+(native?8:16);var previous=bytes[at];bytes[at]^=1;
    Reject(()=>doc.ApplyScenarioEditorParameterDraft(current));bytes[at]=previous;Exact(guarded);SameHistory(guardHistory,doc.GetHistory());
    Console.WriteLine($"PASS {format} foreign, invalid, nested edit, live pen and raw fingerprint rejection are atomic");

    foreach(var overflow in new[]{"changeCount","<EditRevision>k__BackingField"})
    {
        var field=typeof(NativeMapDocument).GetField(overflow,BindingFlags.Instance|BindingFlags.NonPublic)!;var prior=field.GetValue(doc);field.SetValue(doc,long.MaxValue);
        var draftOverflow=doc.CreateScenarioParameterDraft(timer); var overflowHistory=doc.GetHistory();var overflowBytes=Snapshot(doc);
        Reject(()=>doc.ApplyScenarioEditorParameterDraft(draftOverflow));Exact(overflowBytes);SameHistory(overflowHistory,doc.GetHistory());
        Check((long)field.GetValue(doc)! == long.MaxValue,"Overflow changed state");field.SetValue(doc,prior);
    }
    var counter=typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.Instance|BindingFlags.NonPublic)!; counter.SetValue(doc,uint.MaxValue);
    var wrapBefore=Snapshot(doc);doc.ApplyScenarioEditorParameterDraft(doc.CreateScenarioParameterDraft(timer));Exact(wrapBefore);Check(doc.EditorChangeCount==0,"Original unsigned source counter wraps");
    Console.WriteLine($"PASS {format} checked long counters reject atomically and original unsigned source counter wraps");

    var clean=New(native);var cleanRow=Add(clean,native,"TRIGGER_TIMER",false);
    var directory=Path.Combine(Path.GetTempPath(),"DK2ScenarioCaller-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
    try
    {
        var path=clean.SaveCopy(Path.Combine(directory,"before"));var loaded=NativeMapDocument.Load(path);
        var loadedRow=loaded.Records().Single(row=>EntityReferences.EventType(row)?.Name=="TRIGGER_TIMER");var cleanBytes=Snapshot(loaded);
        Check(!loaded.HasChanges && !loaded.CanUndo && loaded.EditorChangeCount==0,"Saved map should reopen clean");
        loaded.ApplyScenarioEditorParameterDraft(loaded.CreateScenarioParameterDraft(loadedRow));Exact(cleanBytes);
        Check(loaded.HasChanges && loaded.EditorChangeCount==1 && !loaded.CanUndo,"Equal explicit Save activates save/dirty without Undo");
        var saved=NativeMapDocument.Load(loaded.SaveCopy(Path.Combine(directory,"after")));
        Check(!saved.HasChanges && saved.EditorChangeCount==0 && !saved.CanUndo && !saved.CanRedo,"Round-trip history/counters clean");
        var savedRow=saved.Records().Single(row=>EntityReferences.EventType(row)?.Name=="TRIGGER_TIMER");
        Check(savedRow.File.Data.Span.Slice(savedRow.Offset,savedRow.Length).SequenceEqual(loadedRow.File.Data.Span.Slice(loadedRow.Offset,loadedRow.Length)),"Save/reopen event payload exact");
    }
    finally {Directory.Delete(directory,true);}
    Console.WriteLine($"PASS {format} clean map equal Save becomes dirty without Undo; save/reopen preserves event bytes and clears counters");

    foreach (var aliasType in new[] {"TRIGGER_VARIABLE", "TRIGGER_TIMER", "ACTION_SETVARIABLE", "ACTION_SETTIMER"})
    {
        var aliasDoc=New(native);var aliasRow=Add(aliasDoc,native,aliasType,false);
        var originalBytes=Snapshot(aliasDoc);var originalNames=Names(aliasDoc);var originalHistory=aliasDoc.GetHistory();var originalRevision=aliasDoc.EditRevision;
        aliasDoc.ApplyScenarioAliasParameterDraft(aliasDoc.CreateScenarioParameterDraft(aliasRow),aliasDoc.CreateScenarioAliasEventDraft(aliasRow));
        Exact(originalBytes);Check(Names(aliasDoc)==originalNames && aliasDoc.EditRevision==originalRevision,"Trusted alias parent no-op");SameHistory(originalHistory,aliasDoc.GetHistory());
        aliasDoc.Paint([new(2,2)],new(5,2,1));var withPaint=Snapshot(aliasDoc);var paintedHistory=aliasDoc.GetHistory();aliasDoc.Undo();
        var equalBytes=Snapshot(aliasDoc);var equalNames=Names(aliasDoc);var equalBefore=aliasDoc.GetHistory();var equalRevision=aliasDoc.EditRevision;
        var parameters=aliasDoc.CreateScenarioParameterDraft(aliasRow);var aliases=aliasDoc.CreateScenarioAliasEventDraft(aliasRow);
        var hc=0;var dc=0;EventHandler onHistory=(_,_)=>hc++;EventHandler onChanged=(_,_)=>dc++;
        aliasDoc.HistoryChanged+=onHistory;aliasDoc.Changed+=onChanged;
        aliasDoc.ApplyScenarioEditorAliasParameterDraft(parameters,aliases);
        aliasDoc.HistoryChanged-=onHistory;aliasDoc.Changed-=onChanged;
        Exact(equalBytes);Check(Names(aliasDoc)==equalNames && hc==1 && dc==1 && aliasDoc.EditRevision==equalRevision+1,"Accepted equal alias parent bytes/events/revision");
        var equalAfter=aliasDoc.GetHistory();
        Check(equalAfter.ChangeCount==equalBefore.ChangeCount+1 && equalAfter.EditorChangeCount==unchecked(equalBefore.EditorChangeCount+1) && equalAfter.Undo.SequenceEqual(equalBefore.Undo) && equalAfter.Redo.SequenceEqual(equalBefore.Redo) && equalAfter.SnapshotBytes==equalBefore.SnapshotBytes,"Accepted equal alias parent counters/history");
        Reject(()=>aliasDoc.ApplyScenarioEditorAliasParameterDraft(parameters,aliases));Exact(equalBytes);Check(Names(aliasDoc)==equalNames,"Stale alias parent names");SameHistory(equalAfter,aliasDoc.GetHistory());
        aliasDoc.Redo();Exact(withPaint);Check(aliasDoc.GetHistory().ChangeCount==paintedHistory.ChangeCount+1 && aliasDoc.EditorChangeCount==unchecked(paintedHistory.EditorChangeCount+1),"Alias parent redo carries source count");
        aliasDoc.Undo();Exact(equalBytes);Check(aliasDoc.GetHistory().ChangeCount==equalBefore.ChangeCount+1,"Alias parent undo carries source count");
        parameters=aliasDoc.CreateScenarioParameterDraft(aliasRow);aliases=aliasDoc.CreateScenarioAliasEventDraft(aliasRow);
        if(aliasType.StartsWith("TRIGGER_",StringComparison.Ordinal)) parameters=aliasDoc.UpdateScenarioParameterDraft(parameters,[new(native?"Repeat":"repeat","254")]);
        var requested=aliases.Values.ToDictionary(item=>item.Key,item=>item.Value);requested[aliases.PrimaryParameter]="1";
        aliases=aliasDoc.UpdateScenarioAliasEvent(aliases,requested.Select(item=>new ScenarioEventParameter(item.Key,item.Value)),[new(1,"Caller")]);
        var beforeChanged=aliasDoc.GetHistory();aliasDoc.ApplyScenarioEditorAliasParameterDraft(parameters,aliases);
        var afterChanged=aliasDoc.GetHistory();var changedBytes=Snapshot(aliasDoc);var changedNames=Names(aliasDoc);
        Check(afterChanged.ChangeCount==beforeChanged.ChangeCount+1 && afterChanged.EditorChangeCount==unchecked(beforeChanged.EditorChangeCount+1) && afterChanged.Undo.Count==beforeChanged.Undo.Count+1 && afterChanged.Redo.Count==0,"Changed alias parent one count/Undo");
        Check(aliasDoc.ReadScenarioNames().Slots.Single(slot=>slot.Kind==aliases.Kind && slot.Index==1).StoredName=="Caller","Changed parent alias accepted");
        aliasDoc.Undo();Exact(equalBytes);Check(Names(aliasDoc)==equalNames,"Changed alias parent Undo exact");aliasDoc.Redo();Exact(changedBytes);Check(Names(aliasDoc)==changedNames,"Changed alias parent Redo exact");
        var currentParameters=aliasDoc.CreateScenarioParameterDraft(aliasRow);var currentAliases=aliasDoc.CreateScenarioAliasEventDraft(aliasRow);var currentHistory=aliasDoc.GetHistory();
        var foreign=New(native);var foreignRow=Add(foreign,native,aliasType,false);
        Reject(()=>aliasDoc.ApplyScenarioEditorAliasParameterDraft(currentParameters,foreign.CreateScenarioAliasEventDraft(foreignRow)));
        aliasDoc.Edit("Nested alias guard",()=>Reject(()=>aliasDoc.ApplyScenarioEditorAliasParameterDraft(currentParameters,currentAliases)));
        using(var pen=aliasDoc.BeginLivePenStroke(new(5,2,1))) Reject(()=>aliasDoc.ApplyScenarioEditorAliasParameterDraft(currentParameters,currentAliases));
        Exact(changedBytes);Check(Names(aliasDoc)==changedNames,"Rejected alias parent names");SameHistory(currentHistory,aliasDoc.GetHistory());
        Console.WriteLine($"PASS {format} {aliasType} parent Save: trusted no-op, accepted equal count, redo rebasing, atomic operand/name/repeat Undo and lifetime guards");
    }
}
WizardSaveCallerChecks.Run(root);
return 0;

static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("Scenario","Scenario",8,8,NativeFormat:native));
static NativeRecord Add(NativeMapDocument doc,bool native,string name,bool imported)
{
    var type=LegacyEventSchemas.All.Single(type=>type.Name==name);var kind=type.RecordSignature==213?"Trigger":"Action";
    var record=native?doc.AddRecord(RecordTemplate.All.Single(item=>item.Structure==kind),new(0,0)):doc.AddLegacyRecord(kind,new(0,0));
    doc.SetEventType(record,type);record=doc.Records().Single(item=>item.File==record.File && item.Offset==record.Offset && item.Length==record.Length);
    if(imported)for(var offset=0;offset<(native?8:32);offset++)Mutable(record.File)[record.Offset+(native?8:16)+offset]=(byte)(0x93^offset);
    Mutable(record.File)[record.Offset+(native?23:55)]=255;return record;
}
static byte[] Mutable(NativeFile file){Check(MemoryMarshal.TryGetArray(file.Data,out var buffer) && buffer.Offset==0,"Independent raw bytes");return buffer.Array!;}
static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument doc)=>doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
static string Names(NativeMapDocument doc)=>string.Join("|",doc.ReadScenarioNames().Slots.Select(slot=>$"{slot.Kind}:{slot.Index}:{slot.StoredName}"));
static void Exact(Dictionary<NativeFile,byte[]> before)=>Check(before.All(item=>item.Key.Data.Span.SequenceEqual(item.Value)),"Unexpected unrelated or opaque byte change");
static void Check(bool accepted,string message){if(!accepted)throw new InvalidOperationException(message);}
static void SameHistory(EditHistorySnapshot before,EditHistorySnapshot after)=>Check(before.ChangeCount==after.ChangeCount && before.EditorChangeCount==after.EditorChangeCount && before.SnapshotBytes==after.SnapshotBytes && before.Depth==after.Depth && before.Undo.SequenceEqual(after.Undo) && before.Redo.SequenceEqual(after.Redo),"History changed");
static void Reject(Action action){try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or FormatException or OverflowException){return;}throw new InvalidOperationException("Invalid operation accepted");}
