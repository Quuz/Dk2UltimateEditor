using System.Reflection;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class Program
{
    static int passed;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Reject(Action action)
    {
        try{action();}catch(Exception e)when(e is ArgumentException or InvalidOperationException or InvalidDataException){return;}
        throw new Exception("Unsafe insertion caller accepted.");
    }
    static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("InsertionCaller","InsertionCaller",12,10,NativeFormat:native));
    static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Unexpected serialized/Undo bytes.");
    static ObjectBatchTemplate Item=>ObjectBatchTemplate.Default(PlacedThingKind.Object) with {Owner=2};
    static ObjectBatchPlacementOptions Options(TilePoint p)=>new(Position:p,Replacement:ObjectReplacementPolicy.All,Flags:(ObjectBatchFlags)127);
    static ObjectBatchPlacementOptions Rect=>Options(new(2,2)) with {Mode=ObjectBatchMode.Rectangle,WholeMap=false,Selection=new(2,2,2,2)};
    static void Add(NativeMapDocument d,ObjectBatchTemplate t,TilePoint p)=>d.ApplyObjectBatchInsertion(d.PlanObjectBatchInsertion(t,Options(p)),[0]);
    static void History(NativeMapDocument d,Dictionary<NativeFile,byte[]> before,uint prior,uint after)
    {
        var bytes=Snapshot(d);d.Undo();Exact(before);Check(d.EditorChangeCount==prior,"Undo source count wrong.");
        d.Redo();Exact(bytes);Check(d.EditorChangeCount==after,"Redo source count wrong.");
    }
    static void Run(string name,Action action){action();passed++;Console.WriteLine("PASS "+name);}
    static void AllKinds(bool native)
    {
        foreach(var kind in Enum.GetValues<PlacedThingKind>())
        {
            var d=New(native);var t=ObjectBatchTemplate.Default(kind) with {Owner=3};var before=Snapshot(d);var h=d.GetHistory();
            var p=d.PlanObjectBatchInsertion(t,Rect);int events=0;d.Changed+=(_,_)=>events++;
            Check(d.ApplyEditorObjectBatchInsertion(p,[2,0,3])==3&&d.ObjectManagerEntries().Count==3,"Three selected objects missing.");
            Check(d.EditorChangeCount==h.EditorChangeCount+3&&d.GetHistory().ChangeCount==h.ChangeCount+1&&d.GetHistory().Undo.Count==h.Undo.Count+1&&events==1,"Batch count/parent Undo not source-correct.");
            Check(d.ObjectManagerEntries().Select(e=>e.Thing.Position).SequenceEqual(p.Placement.Candidates.Where(c=>new[]{0,2,3}.Contains(c.Index)).Select(c=>c.Position)),"Candidate source order lost.");
            History(d,before,h.EditorChangeCount,h.EditorChangeCount+3);
            var generic=New(native);var gh=generic.GetHistory();Check(generic.ApplyObjectBatchInsertion(generic.PlanObjectBatchInsertion(t,Rect),[0,2,3])==3&&generic.EditorChangeCount==gh.EditorChangeCount+1,"Generic API no longer counts atomic transactions.");
        }
    }
    static void Replacements(bool native)
    {
        var d=New(native);Add(d,Item,new(2,2));Add(d,Item,new(3,2));var h=d.GetHistory();var before=Snapshot(d);
        var p=d.PlanObjectBatchInsertion(Item with {TypeID=2},Rect);
        Check(p.Placement.Candidates.Count(c=>c.Replacement is not null)==2,"Two physical replacement fixtures missing.");
        Check(d.ApplyEditorObjectBatchInsertion(p)==4&&d.ObjectManagerEntries().Count==4&&d.EditorChangeCount==h.EditorChangeCount+6,"Replacement removals not counted in addition to four insertions.");
        Check(d.GetHistory().ChangeCount==h.ChangeCount+1&&d.GetHistory().Undo.Count==h.Undo.Count+1,"Replacements not one parent operation.");
        History(d,before,h.EditorChangeCount,h.EditorChangeCount+6);
    }
    static void EqualReplacement(bool native)
    {
        var d=New(native);Add(d,Item,new(2,2));d.Paint([new(8,8)],new(8,3,1));d.Undo();var before=Snapshot(d);var h=d.GetHistory();
        var p=d.PlanObjectBatchInsertion(Item,Options(new(2,2)));var priorId=d.ObjectManagerEntries().Single().Identity;
        Check(p.Placement.Candidates.Single().Replacement is not null,"Equal replacement fixture not recognized.");
        Check(d.ApplyEditorObjectBatchInsertion(p,[0])==1&&d.EditorChangeCount==h.EditorChangeCount+2&&d.GetHistory().Undo.Count==h.Undo.Count+1&&d.GetHistory().Redo.Count==0,"Equal replacement did not retain source removal+add/Undo.");
        if(native)Exact(before);else Check(d.ObjectManagerEntries().Single().Identity!=priorId,"Project replacement reused a reserved identity.");
        History(d,before,h.EditorChangeCount,h.EditorChangeCount+2);
    }
    static void RandomPreview(bool native)
    {
        var d=New(native);int samples=0;
        var p=d.PlanObjectBatchInsertion(Item,Rect with {Mode=ObjectBatchMode.RandomRectangle,RandomFrequency=2},randomSample:max=>{samples++;return 0;});
        var before=Snapshot(d);var h=d.GetHistory();var previewSamples=samples;var rows=p.Placement.Candidates;
        Check(previewSamples>0&&rows.Count>0,"Random preview did not sample.");
        int enumerations=0;IEnumerable<int> Selected(){enumerations++;foreach(var c in rows.Take(2))yield return c.Index;}
        var count=Math.Min(rows.Count,2);Check(d.ApplyEditorObjectBatchInsertion(p,Selected())==count&&samples==previewSamples&&enumerations==1,"Apply resampled or re-enumerated user selection.");
        Check(d.EditorChangeCount==h.EditorChangeCount+(uint)count,"Random source count wrong.");History(d,before,h.EditorChangeCount,h.EditorChangeCount+(uint)count);
    }
    static void Guards(bool native)
    {
        var d=New(native);var p=d.PlanObjectBatchInsertion(Item,Rect);var before=Snapshot(d);var h=d.GetHistory();
        Check(d.ApplyEditorObjectBatchInsertion(p,[])==0&&d.GetHistory().ChangeCount==h.ChangeCount,"Empty selection consumed history.");
        Reject(()=>d.ApplyEditorObjectBatchInsertion(p,[0,0]));Reject(()=>d.ApplyEditorObjectBatchInsertion(p,[-1]));Reject(()=>New(native).ApplyEditorObjectBatchInsertion(p,[0]));
        Reject(()=>d.ApplyEditorObjectBatchInsertion(p with {Template=p.Template with {TypeID=99}},[0]));
        var locked=d.PlanObjectBatchInsertion(Item,Options(new(0,0)));Reject(()=>d.ApplyEditorObjectBatchInsertion(locked,[0]));
        Reject(()=>d.Edit("nested",()=>d.ApplyEditorObjectBatchInsertion(p,[0])));
        using(var pen=d.BeginLivePenStroke(new(8,3,1))){Reject(()=>d.ApplyEditorObjectBatchInsertion(p,[0]));}Exact(before);
        IEnumerable<int> Mutating(){d.Paint([new(8,8)],new(8,3,1));yield return 0;}
        Reject(()=>d.ApplyEditorObjectBatchInsertion(p,Mutating()));Exact(before);Check(d.EditorChangeCount==h.EditorChangeCount&&d.GetHistory().Undo.Count==h.Undo.Count,"Mutating enumerable refusal leaked bytes/history/count.");
        d.Paint([new(8,8)],new(8,3,1));var edited=Snapshot(d);var count=d.EditorChangeCount;
        Reject(()=>d.ApplyEditorObjectBatchInsertion(p,[0]));Exact(edited);Check(d.EditorChangeCount==count,"Stale preview refusal changed source count.");
    }
    static void WrapSave(bool native,string directory)
    {
        var d=New(native);d.SetHistoryDepth(0);typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(d,uint.MaxValue-1);
        Check(d.ApplyEditorObjectBatchInsertion(d.PlanObjectBatchInsertion(Item,Rect),[0,1,2])==3&&d.EditorChangeCount==1&&d.GetHistory().Undo.Count==0,"DWORD wrap/zero-depth failed.");
        var path=d.SaveCopy(Path.Combine(directory,"saved-"+native));var reopened=NativeMapDocument.Load(path);
        Check(!reopened.HasChanges&&reopened.EditorChangeCount==0&&reopened.ObjectManagerEntries().Count==3,"Save/reopen not clean/complete.");
        var position=reopened.ObjectManagerEntries()[0].Thing.Position;var h=reopened.GetHistory();
        Check(reopened.ApplyEditorObjectBatchInsertion(reopened.PlanObjectBatchInsertion(Item,Options(position)),[0])==1&&reopened.EditorChangeCount==2&&reopened.HasChanges&&reopened.GetHistory().Undo.Count==h.Undo.Count+1,"Reopened replacement source count/dirty incorrect.");
    }
    static void ScriptReplacement(bool native)
    {
        var d=New(native);Add(d,Item,new(2,2));var entry=d.ObjectManagerEntries().Single();var owner=d.ScriptOwners().Single(o=>ReferenceEquals(o.Record.File,entry.Record.File)&&o.Record.Offset==entry.Record.Offset);Check(owner.Scope==128,"Ordinary object source scope must be128.");var binding=d.EnsureScriptScope(owner);
        d.InsertScriptEvent(binding.Owner,binding.Field,LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMER"));
        Check(d.ObjectManagerEntries().Single().Scripted,"Scripted replacement fixture missing.");
        var eventBytes=ScriptGraph.Build(d).Events.Values.Select(e=>e.Record.File.Data.Span.Slice(e.Record.Offset,e.Record.Length).ToArray()).ToArray();
        var before=Snapshot(d);var h=d.GetHistory();var p=d.PlanObjectBatchInsertion(Item with {TypeID=2},Options(new(2,2)));
        Check(d.ApplyEditorObjectBatchInsertion(p,[0])==1&&d.EditorChangeCount==h.EditorChangeCount+2,"Scripted physical replacement count wrong.");
        var events=ScriptGraph.Build(d).Events.Values.ToArray();Check(events.Length==eventBytes.Length&&events.Select((e,i)=>e.Record.File.Data.Span.Slice(e.Record.Offset,e.Record.Length).SequenceEqual(eventBytes[i])).All(v=>v),"Replacement changed retained event graph bytes.");
        Check(!d.ObjectManagerEntries().Single().Scripted,"Replacement retained obsolete owned binding.");History(d,before,h.EditorChangeCount,h.EditorChangeCount+2);
    }
    static void Main(string[] args)
    {
        var supplement=args.Length==2&&args[1]=="--remaining-project-and-native";
        Check((args.Length==1||supplement)&&!Directory.Exists(args[0]),"Choose new private fixture directory.");Directory.CreateDirectory(args[0]);
        foreach(var native in new[]{false,true})
        {
            var prefix=native?"native":"project";
            if(supplement&&!native){Run(prefix+" scripted replacement retains event bytes and exact history",()=>ScriptReplacement(native));continue;}
            Run(prefix+" all five categories count selected rows once with exact Undo",()=>AllKinds(native));
            Run(prefix+" four insertions and two replacements count six",()=>Replacements(native));
            Run(prefix+" equal replacement retains source two-count and Undo",()=>EqualReplacement(native));
            Run(prefix+" sampled preview and one enumeration without resampling",()=>RandomPreview(native));
            Run(prefix+" empty duplicate locked foreign stale nested pen and mutation guards",()=>Guards(native));
            Run(prefix+" DWORD wrap zero history save reopen and replacement dirty",()=>WrapSave(native,args[0]));
            Run(prefix+" scripted replacement retains event bytes and exact history",()=>ScriptReplacement(native));
        }
        Check(passed==(supplement?8:14),"Incomplete insertion caller cases.");Console.WriteLine("EDITOR OBJECT INSERTION CALLER CHECKS PASSED: "+passed);
    }
}
