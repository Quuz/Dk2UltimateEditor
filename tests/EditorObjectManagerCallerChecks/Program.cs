using System.Reflection;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class Program
{
    static int passed;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Reject(Action action)
    {
        try { action(); } catch (Exception e) when (e is ArgumentException or InvalidOperationException or InvalidDataException) { return; }
        throw new Exception("Unsafe manager caller was accepted.");
    }
    static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument d) => d.Files.ToDictionary(f => f, f => f.Data.ToArray());
    static void Exact(Dictionary<NativeFile, byte[]> bytes) => Check(bytes.All(p => p.Key.Data.Span.SequenceEqual(p.Value)), "Unexpected serialized bytes.");
    static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("ManagerCaller", "ManagerCaller", 16, 14, NativeFormat: native));
    static ObjectBatchTemplate Creature => ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 3, Health = 72, DataAll = 4, Objective = 0 };
    static void Add(NativeMapDocument d, ObjectBatchTemplate t, TilePoint p)
        => d.ApplyObjectBatchInsertion(d.PlanObjectBatchInsertion(t, new(Position: p, Flags: (ObjectBatchFlags)127)), [0]);
    static ObjectBatchModificationPlan Plan(NativeMapDocument d, IEnumerable<NativeRecord> records, ObjectBatchTemplate t,
        ObjectBatchModificationOptions options, params ObjectModificationDecision[] decisions)
    {
        var p = d.PlanObjectBatchModification(records, t, options);
        return d.ReviewObjectBatchModification(p, decisions.Length == 0 ? p.Candidates.Select(_ => ObjectModificationDecision.Modify) : decisions);
    }
    static ObjectBatchModificationOptions Health => new(ObjectModificationMask.Health, Flags: (ObjectBatchFlags)127);
    static void RoundTrip(NativeMapDocument d, Dictionary<NativeFile, byte[]> before, Dictionary<NativeFile, byte[]> after, uint prior, uint next)
    {
        d.Undo(); Exact(before); Check(d.EditorChangeCount == prior, "Undo did not restore source count.");
        d.Redo(); Exact(after); Check(d.EditorChangeCount == next, "Redo did not restore source count.");
    }
    static void Run(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    static void Removal(bool native)
    {
        var d = New(native);
        foreach (var kind in Enum.GetValues<PlacedThingKind>()) Add(d, ObjectBatchTemplate.Default(kind) with { Owner = 3 }, new(2+(int)kind, 2));
        Add(d, Creature with { Owner = 1 }, new(2,4)); Add(d, Creature with { Owner = 2 }, new(3,4));
        var rows = d.ObjectManagerEntries(); Check(rows.Count == 7, "Seven physical removal fixtures missing.");
        var before = Snapshot(d); var h = d.GetHistory(); int events=0; d.Changed += (_,_) => events++;
        Check(d.ApplyEditorObjectRemoval(d.PlanObjectRemoval(rows.Select(e => e.Record).Concat([rows[0].Record]))) == 7, "Duplicate selected row counted twice.");
        Check(d.EditorChangeCount == h.EditorChangeCount+7 && d.GetHistory().ChangeCount == h.ChangeCount+1 && d.GetHistory().Undo.Count == h.Undo.Count+1 && events==1, "Removal did not count each object in one transaction.");
        RoundTrip(d,before,Snapshot(d),h.EditorChangeCount,h.EditorChangeCount+7);
    }
    static void EqualSingle(bool native)
    {
        var d=New(native); Add(d,Creature,new(3,4)); d.Paint([new(11,11)],new(8,3,1)); d.Undo();
        var before=Snapshot(d); var h=d.GetHistory(); var p=Plan(d,d.ObjectManagerEntries().Select(e=>e.Record),Creature,Health);
        Check(d.ApplyObjectBatchModification(p)==0 && d.GetHistory().Redo.Count==h.Redo.Count,"Generic equal modification lost redo."); Exact(before);
        int events=0; d.Changed+=(_,_)=>events++;
        Check(d.ApplyEditorObjectBatchModification(p)==1,"Accepted equal manager modification reported zero."); Exact(before);
        Check(d.EditorChangeCount==h.EditorChangeCount+1 && d.GetHistory().Undo.Count==h.Undo.Count+1 && d.GetHistory().Redo.Count==0 && events==1,"Equal manager accept did not retain Undo/count and clear redo.");
        RoundTrip(d,before,before,h.EditorChangeCount,h.EditorChangeCount+1);
    }
    static void ModifyMany(bool native)
    {
        var d=New(native); for(int i=0;i<3;i++) Add(d,Creature,new(2+i,3)); var before=Snapshot(d); var h=d.GetHistory();
        var p=Plan(d,d.ObjectManagerEntries().Select(e=>e.Record),Creature with {Health=93},Health);
        Check(d.ApplyEditorObjectBatchModification(p)==3 && d.EditorChangeCount==h.EditorChangeCount+3 && d.GetHistory().Undo.Count==h.Undo.Count+1,"Three modified objects counted as one.");
        Check(d.ObjectManagerEntries().All(e=>e.Record.Read(e.Record.Fields.Single(f=>f.Name==(native?"dwGlobalHealth":"health")))=="93"),"Health payload not changed.");
        foreach(var pair in before.Where(p=>p.Key.Signature!=(native?0xBE:NativeMapDocument.LegacyThings))) Exact(new(){[pair.Key]=pair.Value});
        RoundTrip(d,before,Snapshot(d),h.EditorChangeCount,h.EditorChangeCount+3);
    }
    static void Mixed(bool native)
    {
        var d=New(native); for(int i=0;i<3;i++) Add(d,Creature,new(2+i,3)); var before=Snapshot(d); var h=d.GetHistory();
        var p=Plan(d,d.ObjectManagerEntries().Select(e=>e.Record),Creature with {Health=93},Health,ObjectModificationDecision.Modify,ObjectModificationDecision.Remove,ObjectModificationDecision.Keep);
        Check(d.ApplyEditorObjectBatchModification(p)==2 && d.EditorChangeCount==h.EditorChangeCount+2 && d.ObjectManagerEntries().Count==2,"Keep/remove/modify counts incorrect.");
        RoundTrip(d,before,Snapshot(d),h.EditorChangeCount,h.EditorChangeCount+2);
    }
    static void Replacement(bool native)
    {
        var d=New(native); var t=ObjectBatchTemplate.Default(PlacedThingKind.Object); Add(d,t,new(2,2)); Add(d,t,new(3,2));
        var rows=d.ObjectManagerEntries(); var before=Snapshot(d); var h=d.GetHistory();
        var p=d.PlanObjectBatchModification(rows.Select(e=>e.Record),t,new(ObjectModificationMask.Coordinates,new(3,2),Flags:(ObjectBatchFlags)127));
        p=d.ReviewObjectBatchModification(p,[ObjectModificationDecision.Modify,ObjectModificationDecision.Keep]);
        Check(p.Candidates[0].Decision==ObjectModificationDecision.Keep && p.Candidates[0].Replacement is not null,"New collision did not require renewed consent.");
        p=d.ReviewObjectBatchModification(p,[ObjectModificationDecision.Modify,ObjectModificationDecision.Keep]);
        Check(d.ApplyEditorObjectBatchModification(p)==1 && d.EditorChangeCount==h.EditorChangeCount+2 && d.ObjectManagerEntries().Count==1,"Replacement removal not counted separately.");
        RoundTrip(d,before,Snapshot(d),h.EditorChangeCount,h.EditorChangeCount+2);
    }
    static void EqualReorder(bool native)
    {
        var d=New(native); for(int i=0;i<3;i++) Add(d,Creature,new(2+i,3));
        var rows=d.ObjectManagerEntries(); var before=Snapshot(d); var h=d.GetHistory();
        var p=Plan(d,new[]{rows[1].Record,rows[0].Record},Creature,Health);
        Check(d.ApplyObjectBatchModification(p)==0,"Generic equal multi-edit became source-sensitive."); Exact(before);
        Check(d.ApplyEditorObjectBatchModification(p)==2 && d.EditorChangeCount==h.EditorChangeCount+2,"Equal multi-edit not accepted/counting.");
        Check(d.ObjectManagerEntries().Select(e=>e.Thing.Position).SequenceEqual(new[]{rows[2].Thing.Position,rows[1].Thing.Position,rows[0].Thing.Position}),"Accepted source multi-edit did not append sources in chosen order.");
        RoundTrip(d,before,Snapshot(d),h.EditorChangeCount,h.EditorChangeCount+2);
    }
    static void ScriptPolicy(bool native)
    {
        var d=New(native); Add(d,Creature,new(2,3)); Add(d,Creature,new(4,3));
        var owner=d.ScriptOwners().First(o=>o.Scope==2); var root=d.EnsureScriptScope(owner);
        d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMER"));
        var rows=d.ObjectManagerEntries(); Check(rows.Count(e=>e.Scripted)==1,"Script owner fixture absent.");
        var before=Snapshot(d); var h=d.GetHistory(); var p=d.PlanObjectRemoval(rows.Select(e=>e.Record));
        Check(d.ApplyEditorObjectRemoval(p)==1 && d.EditorChangeCount==h.EditorChangeCount+1 && d.ObjectManagerEntries().Single().Scripted,"Unapproved scripted object removed or counted.");
        d.Undo(); Exact(before); var events=ScriptGraph.Build(d).Events.Count;
        Check(d.ApplyEditorObjectRemoval(p,true)==2 && d.EditorChangeCount==h.EditorChangeCount+2 && ScriptGraph.Build(d).Events.Count==events,"Explicit scripted removal changed retained event graph.");
        RoundTrip(d,before,Snapshot(d),h.EditorChangeCount,h.EditorChangeCount+2);
    }
    static void Guards(bool native)
    {
        var d=New(native); Add(d,Creature,new(2,3)); var p=d.PlanObjectRemoval(d.ObjectManagerEntries().Select(e=>e.Record));
        var m=Plan(d,d.ObjectManagerEntries().Select(e=>e.Record),Creature,Health); var before=Snapshot(d); var h=d.GetHistory();
        Check(d.ApplyEditorObjectRemoval(d.PlanObjectRemoval([]))==0,"Empty selection accepted.");
        var keep=d.ReviewObjectBatchModification(m,[ObjectModificationDecision.Keep]);
        Check(d.ApplyEditorObjectBatchModification(keep)==0 && d.GetHistory().ChangeCount==h.ChangeCount,"Kept selection consumed history."); Exact(before);
        Reject(()=>New(native).ApplyEditorObjectRemoval(p)); Reject(()=>New(native).ApplyEditorObjectBatchModification(m));
        Reject(()=>d.Edit("nested",()=>d.ApplyEditorObjectRemoval(p))); Reject(()=>d.Edit("nested",()=>d.ApplyEditorObjectBatchModification(m))); Exact(before);
        using(var pen=d.BeginLivePenStroke(new(8,3,1))) { Reject(()=>d.ApplyEditorObjectRemoval(p)); Reject(()=>d.ApplyEditorObjectBatchModification(m)); }
        Exact(before); d.Paint([new(11,11)],new(8,3,1)); var edited=Snapshot(d); var count=d.EditorChangeCount;
        Reject(()=>d.ApplyEditorObjectRemoval(p)); Reject(()=>d.ApplyEditorObjectBatchModification(m)); Exact(edited); Check(d.EditorChangeCount==count,"Refusal changed count.");
    }
    static void ZeroHistoryWrapAndSave(bool native,string directory)
    {
        var d=New(native); for(int i=0;i<3;i++) Add(d,Creature,new(2+i,3)); d.SetHistoryDepth(0);
        typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(d,uint.MaxValue-1);
        var p=Plan(d,d.ObjectManagerEntries().Select(e=>e.Record),Creature with {Health=93},Health);
        Check(d.ApplyEditorObjectBatchModification(p)==3 && d.EditorChangeCount==1 && d.GetHistory().Undo.Count==0,"Source DWORD wrap/zero-depth broken.");
        var saved=d.SaveCopy(Path.Combine(directory,"save-"+native)); var reopened=NativeMapDocument.Load(saved);
        Check(!reopened.HasChanges && reopened.EditorChangeCount==0,"Saved/reopened map not clean.");
        var row=reopened.ObjectManagerEntries()[0]; var t=reopened.ObjectTemplateForModification(row.Record); var bytes=Snapshot(reopened);
        Check(reopened.ApplyEditorObjectBatchModification(Plan(reopened,[row.Record],t,Health))==1 && reopened.HasChanges && reopened.EditorChangeCount==1,"Equal accept after reopen did not mark dirty."); Exact(bytes);
    }
    static void Main(string[] args)
    {
        Check(args.Length==1&&!Directory.Exists(args[0]),"Use a new private fixture directory."); Directory.CreateDirectory(args[0]);
        foreach(var native in new[]{false,true})
        {
            string prefix=native?"native":"project";
            Run(prefix+" removal per-object count and exact single Undo",()=>Removal(native));
            Run(prefix+" equal accept retains Undo while generic retains redo",()=>EqualSingle(native));
            Run(prefix+" three modified objects exact payload and history",()=>ModifyMany(native));
            Run(prefix+" mixed Modify Remove Keep count and history",()=>Mixed(native));
            Run(prefix+" explicitly reapproved replacement adds removal count",()=>Replacement(native));
            Run(prefix+" equal multi-edit source append order",()=>EqualReorder(native));
            Run(prefix+" scripted exclusion consent and retained event graph",()=>ScriptPolicy(native));
            Run(prefix+" empty foreign stale nested and active-pen guards",()=>Guards(native));
            Run(prefix+" zero history DWORD wrap save reopen and equal dirty",()=>ZeroHistoryWrapAndSave(native,args[0]));
        }
        Run("existing generic object manager regression",ObjectManagerChecks.Run);
        Run("existing generic modification transformation history and historical regression",ObjectModificationChecks.Run);
        Run("source shared confirmation and used-creature requests",SharedThingRequestChecks.Run);
        Run("source under-cursor keyboard removal",()=>UnderCursorRemovalChecks.Run(args[0]));
        Run("source manager category/point retention",()=>ManagerCategoryScopeChecks.Run(args[0]));
        Check(passed==23,"Incomplete cases."); Console.WriteLine("EDITOR OBJECT MANAGER CALLER CHECKS PASSED: 23");
    }
}
