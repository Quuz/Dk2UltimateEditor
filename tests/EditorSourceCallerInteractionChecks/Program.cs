using System.Text;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

if(args.Length!=1||Directory.Exists(args[0]))throw new ArgumentException("Use a new private fixture directory.");
Directory.CreateDirectory(args[0]);int passed=0;
void Check(bool condition,string message){if(!condition)throw new Exception(message);}
Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Cross-caller Undo/Redo changed unrelated bytes.");
Dictionary<NativeFile,byte[]> ExpectedCarried(Dictionary<NativeFile,byte[]> original,bool native)
{
    var result=original.ToDictionary(p=>p.Key,p=>p.Value.ToArray());
    if(!native)
    {
        var file=result.Single(p=>p.Key.Signature==NativeMapDocument.LegacyEvents);var offset=file.Value.Length-146*64+7*64;
        file.Value.AsSpan(offset,64).Clear();Encoding.ASCII.GetBytes("Carried").CopyTo(file.Value,offset);
    }
    return result;
}
foreach(var native in new[]{false,true})foreach(var operation in new[]{"remove","modify","insert"})
{
    var d=NativeMapDocument.CreateNewMap(new("CombinedCallers","CombinedCallers",12,10,NativeFormat:native));
    d.SetScenarioName(ScenarioNameKind.Variable,7,"Before");d.SetScenarioName(ScenarioNameKind.Variable,8,"Untouched");
    d.SetScenarioName(ScenarioNameKind.Timer,16,"Special");
    var kind=operation=="modify"?PlacedThingKind.Door:PlacedThingKind.Object;
    var template=ObjectBatchTemplate.Default(kind) with {Owner=3};
    foreach(var position in new[]{new TilePoint(2,2),new TilePoint(3,2)})
        d.ApplyObjectBatchInsertion(d.PlanObjectBatchInsertion(template,new(Position:position,Replacement:ObjectReplacementPolicy.All,Flags:(ObjectBatchFlags)127)),[0]);
    var first=d.ObjectManagerEntries()[0];var owner=d.ScriptOwners().Single(o=>ReferenceEquals(o.Record.File,first.Record.File)&&o.Record.Offset==first.Record.Offset);
    var binding=d.EnsureScriptScope(owner);d.InsertScriptEvent(binding.Owner,binding.Field,LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMER"));
    var rows=d.ObjectManagerEntries();var before=Snapshot(d);var h=d.GetHistory();
    if(operation=="remove")Check(d.ApplyEditorObjectRemoval(d.PlanObjectRemoval(rows.Select(r=>r.Record)),true)==2,"Manager removal fixture incomplete.");
    else if(operation=="modify")
    {
        var plan=d.PlanObjectBatchModification(rows.Select(r=>r.Record),ObjectBatchTemplate.Default(PlacedThingKind.Trap) with {Owner=3},
            new(ObjectModificationMask.Type|ObjectModificationMask.Data,Flags:(ObjectBatchFlags)127,IncludeScripted:true));
        plan=d.ReviewObjectBatchModification(plan,plan.Candidates.Select(_=>ObjectModificationDecision.Modify));
        Check(d.ApplyEditorObjectBatchModification(plan,true)==2,"Manager conversion fixture incomplete.");
    }
    else
    {
        var plan=d.PlanObjectBatchInsertion(template with {TypeID=2},new(Position:first.Thing.Position,Replacement:ObjectReplacementPolicy.All,Flags:(ObjectBatchFlags)127));
        Check(d.ApplyEditorObjectBatchInsertion(plan,[0])==1,"Manager insertion replacement fixture incomplete.");
    }
    Check(d.EditorChangeCount==h.EditorChangeCount+2&&d.GetHistory().ChangeCount==h.ChangeCount+1,"Physical operation source count incorrect.");
    var afterMutation=Snapshot(d);var expectedBefore=ExpectedCarried(before,native);var expectedAfter=ExpectedCarried(afterMutation,native);
    d.ApplyEditorScenarioNameDraft(d.CreateScenarioNameDraft(ScenarioNameKind.Variable,7),"Carried");
    Exact(expectedAfter);Check(d.GetHistory().Undo.Count==h.Undo.Count+1&&d.EditorChangeCount==h.EditorChangeCount+3,"Name acceptance added Undo or lost manager physical count.");
    d.Undo();Exact(expectedBefore);Check(d.EditorChangeCount==h.EditorChangeCount+1&&d.GetHistory().ChangeCount==h.ChangeCount+1,"Manager Undo failed to retain non-Undo name count.");
    var redo=d.GetHistory().Redo.Count;Check(redo==1,"Physical operation redo absent.");
    d.ApplyEditorScenarioNameDraft(d.CreateScenarioNameDraft(ScenarioNameKind.Variable,7),"Carried");
    Exact(expectedBefore);Check(d.GetHistory().Redo.Count==redo&&d.EditorChangeCount==h.EditorChangeCount+2,"Equal non-Undo name acceptance consumed redo.");
    d.Redo();Exact(expectedAfter);Check(d.EditorChangeCount==h.EditorChangeCount+4&&d.GetHistory().ChangeCount==h.ChangeCount+3,"Manager Redo lost carried alias count or counted physical operation as one.");
    d.Undo();Exact(expectedBefore);Check(d.EditorChangeCount==h.EditorChangeCount+2,"Second manager Undo count wrong.");
    d.SetScenarioName(ScenarioNameKind.Variable,7,"Future");d.Undo();Exact(expectedBefore);
    var names=d.ReadScenarioNames();Check(names.Slots.Single(r=>r.Kind==ScenarioNameKind.Variable&&r.Index==7).StoredName=="Carried"
        &&names.Slots.Single(r=>r.Kind==ScenarioNameKind.Variable&&r.Index==8).StoredName=="Untouched"
        &&names.Slots.Single(r=>r.Kind==ScenarioNameKind.Timer&&r.Index==16).StoredName=="Special","Cross-caller history lost selected or unrelated aliases.");
    var path=d.SaveCopy(Path.Combine(args[0],$"save-{native}-{operation}"));var reopened=NativeMapDocument.Load(path);
    Check(!reopened.HasChanges&&reopened.EditorChangeCount==0&&reopened.ReadScenarioNames().Slots.Single(r=>r.Kind==ScenarioNameKind.Variable&&r.Index==7).StoredName=="Carried","Cross-caller saved map not clean or alias lost.");
    passed++;Console.WriteLine($"PASS {(native?"native":"project")} {operation} physical count, event-size history, non-Undo alias/equal redo persistence, unrelated bytes and saved roundtrip");
}
Check(passed==6,"Incomplete cross-caller cases.");Console.WriteLine("EDITOR SOURCE CALLER INTERACTION CHECKS PASSED: 6");
