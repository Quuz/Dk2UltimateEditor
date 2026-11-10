using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class WizardSaveCallerChecks
{
    internal static void Run(string root)
    {
        using var pages=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"tests/DK2MapEditor.Validation/ScenarioParameterPages.json")));
        var types=pages.RootElement.EnumerateArray().Select(page=>page.GetProperty("Type").GetString()!).ToArray();
        using var oracle=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"tests/DK2MapEditor.Validation/ScenarioCreationDefaults.json")));
        var zero=oracle.RootElement.GetProperty("CreationAndSaveFlow").EnumerateObject().SelectMany(kind=>kind.Value.GetProperty("ZeroOnSaveTypes").EnumerateArray().Select(value=>value.GetString()!)).ToHashSet();
        using var wizard=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"tests/DK2MapEditor.Validation/ScenarioExistingWizardSource.json")));
        Check(types.Length==103 && zero.Count==26 && wizard.RootElement.GetProperty("Functions").EnumerateObject().Count()==19,"Independent original wizard scope");
        foreach(var input in wizard.RootElement.GetProperty("InputHashes").EnumerateObject())
            Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Original wizard source changed");
        foreach(var native in new[]{false,true})
        {
            var format=native?"native":"project";
            foreach(var name in types)
            {
                var document=New(native);var record=Add(document,native,name,imported:true);var id=ScriptGraph.ID(record);
                var context=document.CreateScenarioCreationContext();var baseline=Snapshot(document);var baselineNames=Names(document);var baselineHistory=document.GetHistory();var baselineRevision=document.EditRevision;
                var expected=baseline.ToDictionary(item=>item.Key,item=>(byte[])item.Value.Clone());
                if(zero.Contains(name))Array.Clear(expected[record.File],record.Offset+(native?8:16),native?8:32);
                using(var session=document.CreateScenarioEditingSession(context,record))
                {
                    var plan=document.PrepareScenarioCreation(session);Exact(baseline);
                    var hc=0;var dc=0;EventHandler historyChanged=(_,_)=>hc++;EventHandler changed=(_,_)=>dc++;
                    document.HistoryChanged+=historyChanged;document.Changed+=changed;
                    var saved=document.ApplyScenarioEditorCreation(plan);
                    document.HistoryChanged-=historyChanged;document.Changed-=changed;
                    Exact(expected);Check(Names(document)==baselineNames && ScriptGraph.ID(saved)==id && ScriptGraph.Build(document).Events.Count==1,"Wizard changed unrelated names/identity/collection");
                    var after=document.GetHistory();
                    Check(after.ChangeCount==baselineHistory.ChangeCount+1 && after.EditorChangeCount==unchecked(baselineHistory.EditorChangeCount+1) && document.EditRevision==baselineRevision+1 && hc==1 && dc==1,"Accepted Finish count/events "+name);
                    Check(after.Undo.Count==baselineHistory.Undo.Count+(zero.Contains(name)?1:0) && !document.IsScenarioCreationPlanCurrent(plan),"Changed-only modern Undo and consumed plan "+name);
                }
                var normalized=Snapshot(document);var normalizedNames=Names(document);var genericHistory=document.GetHistory();var genericRevision=document.EditRevision;
                using(var generic=document.CreateScenarioEditingSession(context,record))
                {
                    var plan=document.PrepareScenarioCreation(generic);document.ApplyScenarioCreation(plan);Exact(normalized);
                    SameHistory(genericHistory,document.GetHistory());Check(document.EditRevision==genericRevision && Names(document)==normalizedNames,"Trusted normalized Finish no-op "+name);
                }
                document.Paint([new(2,2)],new(5,2,1));var painted=Snapshot(document);var paintedHistory=document.GetHistory();document.Undo();
                var equal=Snapshot(document);var equalHistory=document.GetHistory();
                using(var equalSession=document.CreateScenarioEditingSession(context,record))
                {
                    var plan=document.PrepareScenarioCreation(equalSession);document.ApplyScenarioEditorCreation(plan);Exact(equal);
                    var accepted=document.GetHistory();Check(accepted.ChangeCount==equalHistory.ChangeCount+1 && accepted.EditorChangeCount==unchecked(equalHistory.EditorChangeCount+1) && accepted.Undo.SequenceEqual(equalHistory.Undo) && accepted.Redo.SequenceEqual(equalHistory.Redo) && accepted.SnapshotBytes==equalHistory.SnapshotBytes,"Equal Finish retains redo/cost "+name);
                }
                document.Redo();Exact(painted);Check(document.GetHistory().ChangeCount==paintedHistory.ChangeCount+1 && document.EditorChangeCount==unchecked(paintedHistory.EditorChangeCount+1),"Wizard Redo carries accepted count "+name);
                document.Undo();Exact(equal);Check(document.GetHistory().ChangeCount==equalHistory.ChangeCount+1 && document.EditorChangeCount==unchecked(equalHistory.EditorChangeCount+1) && Names(document)==normalizedNames,"Wizard Undo carries accepted count "+name);
            }
            Console.WriteLine($"PASS {format} Finish all 103 original event types: 26 zeroing/77 retained, accepted equal count, generic no-op, exact bytes/identity/names and redo rebasing");

            foreach(var depth in new[]{0,200})
            {
                var document=New(native);document.SetHistoryDepth(depth);var context=document.CreateScenarioCreationContext();
                var baseline=Snapshot(document);var history=document.GetHistory();
                using var session=document.CreateScenarioCreationSession(context,214,LegacyEventSchemas.All.Single(type=>type.Name=="ACTION_CHANGESQUARE"));
                document.UpdateScenarioCreationRectangle(session,new(2,3,3,2));var plan=document.PrepareScenarioCreation(session);
                Check(plan.EventIDs.Count==6,"Independent rectangle cell count");Exact(baseline);
                document.ApplyScenarioEditorCreation(plan);var after=document.GetHistory();var graph=ScriptGraph.Build(document);
                Check(after.EditorChangeCount==unchecked(history.EditorChangeCount+6) && after.ChangeCount==history.ChangeCount+1 && graph.Events.Count==6 && after.Undo.Count==history.Undo.Count+(depth==0?0:1),"Rectangle six physical saves, one modern operation/Undo");
                for(var index=0;index<6;index++)
                {
                    var row=graph.Events[plan.EventIDs[index]].Record;
                    var x=int.Parse(row.Read(row.Fields.Single(field=>field.Name==(native?"Param":"param")+5)));
                    var y=int.Parse(row.Read(row.Fields.Single(field=>field.Name==(native?"Param":"param")+7)));
                    Check(x==3+index%3 && y==4+index/3,"Source x-first rectangle coordinates/order");
                }
                if(depth!=0)
                {
                    var result=Snapshot(document);document.Undo();Exact(baseline);Check(document.EditorChangeCount==history.EditorChangeCount,"Rectangle Undo counter");document.Redo();Exact(result);Check(document.EditorChangeCount==unchecked(history.EditorChangeCount+6),"Rectangle Redo counter");
                }
            }
            Console.WriteLine($"PASS {format} rectangle Finish counts all six original saves, x-first coordinates, one Undo, disabled history and exact Undo/Redo");

            foreach(var overflow in new[]{"changeCount","<EditRevision>k__BackingField"})
            {
                var document=New(native);var record=Add(document,native,"ACTION_SHOWCRYSTAL",imported:false);var context=document.CreateScenarioCreationContext();
                var field=typeof(NativeMapDocument).GetField(overflow,BindingFlags.NonPublic|BindingFlags.Instance)!;var prior=field.GetValue(document);field.SetValue(document,long.MaxValue);
                using(var session=document.CreateScenarioEditingSession(context,record))
                {
                    var plan=document.PrepareScenarioCreation(session);var bytes=Snapshot(document);var names=Names(document);var history=document.GetHistory();var revision=document.EditRevision;
                    Reject(()=>document.ApplyScenarioEditorCreation(plan));Exact(bytes);SameHistory(history,document.GetHistory());
                    Check(Names(document)==names && document.EditRevision==revision && document.IsScenarioCreationPlanCurrent(plan),"Overflow consumed private session or changed revision/metadata");
                    if(overflow=="changeCount")
                    {
                        field.SetValue(document,prior);Check(document.IsScenarioCreationPlanCurrent(plan),"Counter rejection lost retry authority");
                        document.ApplyScenarioEditorCreation(plan);Exact(bytes);Check(!document.IsScenarioCreationPlanCurrent(plan),"Retried Finish did not consume session");
                    }
                }
                field.SetValue(document,prior);
            }
            Console.WriteLine($"PASS {format} Finish overflow rejects before consuming private session, bytes, counters and both history stacks; counter retry remains usable");

            var guarded=New(native);var guardedRow=Add(guarded,native,"ACTION_SHOWCRYSTAL",imported:false);var guardedContext=guarded.CreateScenarioCreationContext();
            using(var session=guarded.CreateScenarioEditingSession(guardedContext,guardedRow))
            {
                var plan=guarded.PrepareScenarioCreation(session);var before=Snapshot(guarded);var history=guarded.GetHistory();var names=Names(guarded);
                Reject(()=>New(native).ApplyScenarioEditorCreation(plan));
                guarded.Edit("Nested Finish",()=>Reject(()=>guarded.ApplyScenarioEditorCreation(plan)));
                using(var pen=guarded.BeginLivePenStroke(new(5,2,1)))Reject(()=>guarded.ApplyScenarioEditorCreation(plan));
                Exact(before);SameHistory(history,guarded.GetHistory());Check(Names(guarded)==names && guarded.IsScenarioCreationPlanCurrent(plan),"Rejected Finish consumed plan");
                var counter=typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.NonPublic|BindingFlags.Instance)!;counter.SetValue(guarded,uint.MaxValue);
                guarded.ApplyScenarioEditorCreation(plan);Exact(before);Check(guarded.EditorChangeCount==0,"Original unsigned Finish counter wraps");
            }
            Console.WriteLine($"PASS {format} foreign/nested/active-pen Finish rejection keeps plan; source unsigned counter wraps on accepted equal Finish");
        }
    }
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("Finish","Finish",8,8,NativeFormat:native));
    private static NativeRecord Add(NativeMapDocument document,bool native,string name,bool imported)
    {
        var type=LegacyEventSchemas.All.Single(type=>type.Name==name);var kind=type.RecordSignature==213?"Trigger":"Action";
        var row=native?document.AddRecord(RecordTemplate.All.Single(item=>item.Structure==kind),new(0,0)):document.AddLegacyRecord(kind,new(0,0));
        document.SetEventType(row,type);row=document.Records().Single(item=>item.File==row.File && item.Offset==row.Offset && item.Length==row.Length);
        Check(MemoryMarshal.TryGetArray(row.File.Data,out var buffer),"Independent fixture buffer");
        for(var offset=0;offset<(native?8:32);offset++)buffer.Array![buffer.Offset+row.Offset+(native?8:16)+offset]=imported?(byte)(0x93^offset):(byte)0;
        buffer.Array![buffer.Offset+row.Offset+(native?23:55)]=255;return row;
    }
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static string Names(NativeMapDocument document)=>string.Join("|",document.ReadScenarioNames().Slots.Select(slot=>$"{slot.Kind}:{slot.Index}:{slot.StoredName}"));
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(item=>item.Key.Data.Span.SequenceEqual(item.Value)),"Unexpected graph/opaque/unrelated bytes");
    private static void SameHistory(EditHistorySnapshot a,EditHistorySnapshot b)=>Check(a.ChangeCount==b.ChangeCount && a.EditorChangeCount==b.EditorChangeCount && a.SnapshotBytes==b.SnapshotBytes && a.Undo.SequenceEqual(b.Undo) && a.Redo.SequenceEqual(b.Redo),"History changed on refusal/no-op");
    private static void Check(bool accepted,string message){if(!accepted)throw new InvalidOperationException(message);}
    private static void Reject(Action action){try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or FormatException or OverflowException){return;}throw new InvalidOperationException("Invalid Finish accepted");}
}
