using System.Reflection;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static readonly uint[] Scopes=[32,1,2,64,128,256,4,8,16];
    private static readonly uint[] UniqueScopes=[2,64,128,256,8,16];
    private static int groups;
    private static string directory="";
    private static int Main(string[] args)
    {
        if(args.Length!=1 || Directory.Exists(args[0]))throw new ArgumentException("Require a fresh private artifacts directory.");
        directory=args[0];Directory.CreateDirectory(directory);
        try
        {
            foreach(var native in new[]{false,true})NewAll(native);
            foreach(var native in new[]{false,true})MoveAll(native);
            foreach(var native in new[]{false,true})EqualAll(native);
            Reallocate();AreaZero();BandOrdinal();
            foreach(var native in new[]{false,true})Preview(native);
            OccupiedProject();OccupiedNative();Orphan();Collisions();Insertion();
            foreach(var native in new[]{false,true})Guards(native);
            foreach(var native in new[]{false,true})Overflow(native);
            foreach(var native in new[]{false,true})Save(native);
            foreach(var native in new[]{false,true})DeleteAll(native);
            foreach(var native in new[]{false,true})DeleteEmpty(native);
            foreach(var native in new[]{false,true})DeleteRefusals(native);
            MovePoints();
            EditorRecordCreation();EditorMemberCreation();EditorBatchCreation();
            NativeOwnerScopes();
            foreach(var native in new[]{false,true})ScenarioDeletionRequests(native);
            foreach(var native in new[]{false,true})ScenarioDeletionRequestGuards(native);
            Check(groups==37,"Expected37 complete groups");Console.WriteLine("EDITOR SCENARIO POINT MODEL CHECKS PASSED: 37");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
    }
    private static NativeMapDocument Fixture(bool native)
    {
        var d=NativeMapDocument.CreateNewMap(new("Point","Point",16,16,NativeFormat:native));
        if(native)
        {
            var present=d.ScriptOwners().Where(o=>o.Scope==1).Select(o=>o.Record.Read(o.Record.Fields.Single(f=>f.Name=="bGlobalPlayer"))).ToHashSet();
            foreach(byte player in new byte[]{1,2,3,4,5,6,7})if(!present.Contains(player.ToString()))d.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEPLAYER"),new(0,0),player);
        }
        for(var copy=0;copy<2;copy++)foreach(var (project,kind) in new[]{("Creature","GAMEKEEPER"),("Door","GAMEDOOR"),("Object","GAMEOBJECT"),("MagicalObject","GAMEOBJECT"),("Area","GAMEAREA"),("Band","GAMEBAND")})
        {
            var row=native?d.AddRecord(RecordTemplate.All.Single(t=>t.Structure==kind),new(2+copy,4)):d.AddLegacyRecord(project,new(2+copy,4));
            if(project=="MagicalObject")Set(d,row,native?"bObjectID":"id",ValueCatalogues.ForTemplate("MagicalObject",ValueCatalogues.GameVersion(d)).First().Value);
            if(project=="Area" && !native)RawFixtureField(row,"id",copy.ToString());
            if(project=="Band"){d.AddBandMember(row,6);d.AddBandMember(row,17);}
        }
        // Original lists offer zero-identity things and active band members.
        // Trusted fixture Add APIs retain their generic allocation behavior.
        if(!native)foreach(var row in d.Records().ToArray())foreach(var field in row.Fields.Where(f=>f.Name.EndsWith("uniqueid",StringComparison.Ordinal)))d.SetField(row,field,"0");
        Check(Scopes.All(scope=>d.ScriptOwners().Any(o=>o.Scope==scope)),"All nine original physical owners required");return d;
    }
    private static ScriptOwner Owner(NativeMapDocument d,uint scope,bool last=false)=>last?d.ScriptOwners().Last(o=>o.Scope==scope):d.ScriptOwners().First(o=>o.Scope==scope);
    private static ScriptEdge Create(NativeMapDocument d,ScriptOwner owner,ScriptEdge? before=null)
    {using var s=d.CreateEditorScenarioPointSession(insertBefore:before,initialScope:owner.Scope);s.SelectOwner(owner);return d.ApplyEditorScenarioPoint(d.PrepareEditorScenarioPoint(s));}
    private static ScriptEdge Branch(NativeMapDocument d,ScriptOwner owner)
    {
        var root=Create(d,owner);var timer=LegacyEventSchemas.All.Single(t=>t.Name=="TRIGGER_TIMEELAPSED");
        var row=d.InsertScriptEvent(root.Owner,root.Field,timer);var id=ScriptGraph.ID(row);var edge=ScriptGraph.Build(d).Events[id].Edges.Single(e=>e.Field.Name is "and" or "ChildAnd");
        var action=LegacyEventSchemas.All.First(t=>t.RecordSignature==0xD6 && (LegacyEventSchemas.AllowedScopes(t)&owner.Scope)!=0);
        d.InsertScriptEvent(edge.Owner,edge.Field,action);return d.FindScriptScope(owner)!;
    }
    private static string Identity(ScriptOwner o)=>o.Scope==16?$"heros[{o.MemberIndex}].uniqueid":"uniqueid";
    private static void Set(NativeMapDocument d,NativeRecord r,string name,string value)=>d.SetField(r,r.Fields.Single(f=>f.Name==name),value);
    // These represent bytes of pre-existing original files, including identities
    // that the trusted modern mutation API deliberately refuses to manufacture.
    private static void RawFixtureField(NativeRecord r,string name,string value)
    {var field=r.Fields.Single(f=>f.Name==name);Check(MemoryMarshal.TryGetArray(r.File.Data,out var raw),"Fixture raw buffer");field.Encode(value).CopyTo(raw.Array!,raw.Offset+r.Offset+field.Offset);}
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> expected)=>Check(expected.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Unexpected whole-file/opaque byte mutation");
    private static Dictionary<ushort,byte[]> Events(NativeMapDocument d)=>ScriptGraph.Build(d).Events.ToDictionary(p=>p.Key,p=>p.Value.Record.File.Data.Span.Slice(p.Value.Record.Offset,p.Value.Record.Length).ToArray());
    private static void ExactEvents(NativeMapDocument d,Dictionary<ushort,byte[]> expected)
    {var actual=Events(d);Check(actual.Count==expected.Count && expected.All(p=>actual.TryGetValue(p.Key,out var bytes)&&bytes.AsSpan().SequenceEqual(p.Value)),"Point changed event identity/links/unknown bytes");}
    private static void Accepted(NativeMapDocument d,EditHistorySnapshot before,long revision,int undo)
    {var after=d.GetHistory();Check(after.ChangeCount==before.ChangeCount+1 && after.EditorChangeCount==unchecked(before.EditorChangeCount+1) && d.EditRevision==revision+1 && after.Undo.Count==before.Undo.Count+undo,"One accepted source Save/one modern operation expected");}
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {Exception? error=null;try{action();}catch(Exception e)when(e is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException){error=e;}Check(error is not null,"Expected atomic rejection");}
    private static void Pass(string text){groups++;Console.WriteLine("PASS "+text);}
    private static string Format(bool native)=>native?"native":"project";

    private static void NewAll(bool native)
    {
        var d=Fixture(native);uint next=1;
        foreach(var scope in Scopes)
        {
            var owner=Owner(d,scope);var bytes=Snapshot(d);var h=d.GetHistory();var revision=d.EditRevision;
            using var s=d.CreateEditorScenarioPointSession(initialScope:scope);Check(s.Choices.Any(c=>c.CanSelect && SameOwner(c.Owner,owner)),$"Source free owner missing for {Format(native)} scope {scope}");s.SelectOwner(owner);var plan=d.PrepareEditorScenarioPoint(s);Exact(bytes);
            var root=d.ApplyEditorScenarioPoint(plan);Check(s.Closed && root.Scope==scope && root.Target==0 && d.OwnerForScriptScope(root)?.Record.File==owner.Record.File,"New point/physical owner identity");Accepted(d,h,revision,native && scope is 1 or 32?0:1);
            if(native)Exact(bytes);else if(UniqueScopes.Contains(scope))Check(owner.Record.Read(owner.Record.Fields.Single(f=>f.Name==Identity(owner)))==(next++).ToString(),"Original free identity sequence");
            Reject(()=>d.ApplyEditorScenarioPoint(plan));
        }
        if(!native)Check(ScriptGraph.Build(d).Roots.Count==9,"Nine stored source point records");
        Pass(Format(native)+" New all nine scopes: private preview, one accepted count, exact identities/owners and single-use Finish");
    }
    private static void MoveAll(bool native)
    {
        foreach(var scope in Scopes.Where(s=>s!=32))
        {
            var d=Fixture(native);var first=Owner(d,scope);var last=Owner(d,scope,true);var root=Branch(d,first);var target=root.Target;var events=Events(d);var bytes=Snapshot(d);var h=d.GetHistory();var revision=d.EditRevision;
            using var s=d.CreateEditorScenarioPointSession(root);Reject(()=>s.SelectScope(32));s.SelectOwner(last);var plan=d.PrepareEditorScenarioPoint(s);Exact(bytes);var moved=d.ApplyEditorScenarioPoint(plan);Accepted(d,h,revision,1);
            Check(moved.Target==target && SameOwner(d.OwnerForScriptScope(moved)!,last),"Moved exact branch/physical owner");ExactEvents(d,events);
            if(!native && UniqueScopes.Contains(scope))Check(first.Record.Read(first.Record.Fields.Single(f=>f.Name==Identity(first)))=="0" && last.Record.Read(last.Record.Fields.Single(f=>f.Name==Identity(last)))=="1","Source ID release/handoff");
            Check(native?d.FindScriptScope(first)!.Target==0:d.FindScriptScope(first) is null,"Previous owner remains bound");d.Undo();Exact(bytes);d.Redo();ExactEvents(d,events);
        }
        Pass(Format(native)+" Modify all eight selectable scopes: original scope lock, ID handoff, branch bytes and exact Undo/Redo");
    }
    private static bool SameOwner(ScriptOwner a,ScriptOwner b)=>a.Scope==b.Scope && a.Record.File==b.Record.File && a.Record.Offset==b.Record.Offset && a.MemberIndex==b.MemberIndex;
    private static void EqualAll(bool native)
    {
        foreach(var scope in Scopes.Where(s=>s!=32))
        {
            var d=Fixture(native);var owner=Owner(d,scope);_=Branch(d,owner);d.Paint([new(8,8)],new(5,2,1));var painted=Snapshot(d);var paintedH=d.GetHistory();d.Undo();var bytes=Snapshot(d);var h=d.GetHistory();var revision=d.EditRevision;
            var root=d.FindScriptScope(owner)!;_=d.ReassignScriptScope(root,owner);Check(d.EditRevision==revision,"Trusted no-op changed");
            using var s=d.CreateEditorScenarioPointSession(root);s.SelectOwner(owner);_=d.ApplyEditorScenarioPoint(d.PrepareEditorScenarioPoint(s));Exact(bytes);Accepted(d,h,revision,0);
            Check(d.GetHistory().Undo.SequenceEqual(h.Undo)&&d.GetHistory().Redo.SequenceEqual(h.Redo)&&d.GetHistory().SnapshotBytes==h.SnapshotBytes,"Equal point replaced history");
            d.Redo();Exact(painted);Check(d.GetHistory().ChangeCount==paintedH.ChangeCount+1,"Accepted count lost across old redo");d.Undo();Exact(bytes);
        }
        Pass(Format(native)+" equal Modify all eight scopes: source count without own Undo, trusted no-op unchanged and old redo rebased");
    }
    private static void Reallocate()
    {
        foreach(var scope in UniqueScopes)
        {
            var d=Fixture(false);var owner=Owner(d,scope);var root=Branch(d,owner);Set(d,owner.Record,Identity(owner),"37");Set(d,root.Owner,scope is 2 or 16?"creature":scope==8?"band":scope==64?"door":"object","37");
            root=d.FindScriptScope(owner)!;var bytes=Snapshot(d);var events=Events(d);var h=d.GetHistory();var rev=d.EditRevision;
            using var s=d.CreateEditorScenarioPointSession(root);s.SelectOwner(owner);var plan=d.PrepareEditorScenarioPoint(s);Exact(bytes);var saved=d.ApplyEditorScenarioPoint(plan);Accepted(d,h,rev,1);
            Check(owner.Record.Read(owner.Record.Fields.Single(f=>f.Name==Identity(owner)))=="1" && saved.Target==root.Target,"Same owner did not receive original lowest free ID");ExactEvents(d,events);d.Undo();Exact(bytes);
        }
        Pass("project same-owner six ID families reallocate to first free ID while retaining branch and opaque bytes");
    }
    private static void AreaZero()
    {
        var d=Fixture(false);var area=Owner(d,4);Check(area.Record.Read(area.Record.Fields.Single(f=>f.Name=="id"))=="0","Area0 fixture");var root=Create(d,area);
        Check(d.FindScriptScope(area)?.Owner==root.Owner && SameOwner(d.OwnerForScriptScope(root)!,area),"Area0 confused with zero unique identity");
        Pass("project area ID zero is a valid source owner and resolves separately from zero unique identities");
    }
    private static void BandOrdinal()
    {
        var d=Fixture(false);var member=Owner(d,16,true);Set(d,member.Record,"id","42");var root=Create(d,member);
        Check(root.Owner.Read(root.Owner.Fields.Single(f=>f.Name=="band"))=="1" && SameOwner(d.OwnerForScriptScope(root)!,member),"Source physical band ordinal replaced by visible band ID");
        Pass("project nested member point stores physical parent band ordinal independently of visible band ID");
    }
    private static void Preview(bool native)
    {
        var d=Fixture(native);var bytes=Snapshot(d);var h=d.GetHistory();var rev=d.EditRevision;using var s=d.CreateEditorScenarioPointSession();
        foreach(var scope in Scopes){s.SelectScope(scope);s.SelectOwner(Owner(d,scope));_=d.PrepareEditorScenarioPoint(s);Exact(bytes);}
        Check(d.EditRevision==rev && d.GetHistory().ChangeCount==h.ChangeCount && d.GetHistory().Undo.SequenceEqual(h.Undo),"Selection preview allocated/write/count");
        s.SelectScope(2);s.SelectOwner(Owner(d,2));var stale=d.PrepareEditorScenarioPoint(s);s.SelectOwner(Owner(d,2,true));Reject(()=>d.ApplyEditorScenarioPoint(stale));Exact(bytes);s.Dispose();Reject(()=>d.PrepareEditorScenarioPoint(s));
        Pass(Format(native)+" all nine previews/Cancel are read-only; changing private owner invalidates prior plan");
    }
    private static void OccupiedProject()
    {
        var d=Fixture(false);var owner=Owner(d,2);Set(d,owner.Record,Identity(owner),"77");using var newPoint=d.CreateEditorScenarioPointSession(initialScope:2);
        Check(newPoint.Choices.Single(c=>SameOwner(c.Owner,owner)).Status==ScenarioPointChoiceStatus.ReservedIdentity,"Unbound nonzero identity was offered by source list");Reject(()=>newPoint.SelectOwner(owner));
        var second=Owner(d,2,true);RawFixtureField(second.Record,Identity(second),"77");using var duplicates=d.CreateEditorScenarioPointSession(initialScope:2);Check(duplicates.Choices.All(c=>c.Status==ScenarioPointChoiceStatus.AmbiguousIdentity),"Duplicate IDs silently chosen");
        var level=Create(d,Owner(d,32));using var edit=d.CreateEditorScenarioPointSession(level);Check(edit.Choices.All(c=>!c.CanSelect),"Source Level Modify Finish must be disabled");
        Pass("project occupied/reserved/duplicate owner IDs and existing Level Finish are explicitly unavailable");
    }
    private static void OccupiedNative()
    {
        var d=Fixture(true);var owner=Owner(d,2);var root=Branch(d,owner);using var s=d.CreateEditorScenarioPointSession(initialScope:2);
        Check(!s.Choices.Single(c=>SameOwner(c.Owner,owner)).CanSelect,"Occupied native root was offered");Reject(()=>s.SelectOwner(owner));var bytes=Snapshot(d);ExactEvents(d,Events(d));Exact(bytes);
        Pass("native occupied owner cannot overwrite another live branch");
    }
    private static void Orphan()
    {
        var d=Fixture(false);var owner=Owner(d,2);var root=Branch(d,owner);RawFixtureField(owner.Record,Identity(owner),"0");Check(d.OwnerForScriptScope(root) is null,"Orphan fixture");var target=root.Target;var events=Events(d);
        using var s=d.CreateEditorScenarioPointSession(root);s.SelectOwner(Owner(d,2,true));var moved=d.ApplyEditorScenarioPoint(d.PrepareEditorScenarioPoint(s));Check(moved.Target==target,"Explicit orphan rebind lost branch");ExactEvents(d,events);
        Pass("project explicit orphan owner rebind retains the selected root and all event bytes");
    }
    private static void Collisions()
    {
        var d=Fixture(false);Set(d,Owner(d,128).Record,"uniqueid","1");Set(d,Owner(d,256).Record,"uniqueid","2");
        var orphan=d.AddLegacyRecord("TriggerLink",new(0,0));Set(d,orphan,"node.type","64");Set(d,orphan,"door","3");
        var owner=Owner(d,2);_=Create(d,owner);Check(owner.Record.Read(owner.Record.Fields.Single(f=>f.Name=="uniqueid"))=="4","Object/magic/orphan IDs were captured");
        Pass("project safe source allocation reserves object/magic identities and unrelated orphan roots");
    }
    private static void Insertion()
    {
        var d=Fixture(false);var map=Create(d,Owner(d,32));_=Create(d,Owner(d,1));var before=Snapshot(d);var owner=Owner(d,2);_=Create(d,owner,map);
        Check(ScriptGraph.Build(d).Roots.Select(r=>r.Scope).SequenceEqual(new uint[]{2,32,1}),"Original InsertBefore point order");d.Undo();Exact(before);d.Redo();Check(ScriptGraph.Build(d).Roots.First().Scope==2,"Point insertion redo order");
        Pass("project InsertBefore keeps the original physical point order and exact Undo/Redo");
    }
    private static void Guards(bool native)
    {
        var d=Fixture(native);var owner=Owner(d,2);using var s=d.CreateEditorScenarioPointSession(initialScope:2);s.SelectOwner(owner);var plan=d.PrepareEditorScenarioPoint(s);var bytes=Snapshot(d);
        var other=Fixture(native);Reject(()=>other.ApplyEditorScenarioPoint(plan));Reject(()=>s.SelectOwner(Owner(other,2)));Exact(bytes);
        using(var pen=d.BeginLivePenStroke(new(5,2,1))){Reject(()=>d.CreateEditorScenarioPointSession());Reject(()=>d.ApplyEditorScenarioPoint(plan));}Exact(bytes);
        d.Edit("nested guard",()=>Reject(()=>d.ApplyEditorScenarioPoint(plan)));Exact(bytes);Check(!s.Closed,"Refusal consumed selection");
        Check(MemoryMarshal.TryGetArray(d.MainFile.Data,out var raw),"Fixture raw buffer");raw.Array![raw.Offset+raw.Count-1]^=1;var modified=Snapshot(d);Reject(()=>d.ApplyEditorScenarioPoint(plan));Exact(modified);raw.Array[raw.Offset+raw.Count-1]^=1;
        d.Paint([new(8,8)],new(5,2,1));d.Undo();Reject(()=>d.ApplyEditorScenarioPoint(plan));Exact(bytes);
        Pass(Format(native)+" foreign/nested/live-pen/raw/stale guards reject atomically without consuming a private selection");
    }
    private static void Overflow(bool native)
    {
        var d=Fixture(native);var owner=Owner(d,2);using var s=d.CreateEditorScenarioPointSession(initialScope:2);s.SelectOwner(owner);var plan=d.PrepareEditorScenarioPoint(s);var bytes=Snapshot(d);
        var count=typeof(NativeMapDocument).GetField("changeCount",BindingFlags.Instance|BindingFlags.NonPublic)!;var prior=(long)count.GetValue(d)!;count.SetValue(d,long.MaxValue);Reject(()=>d.ApplyEditorScenarioPoint(plan));Exact(bytes);Check(!s.Closed,"Overflow consumed selection");count.SetValue(d,prior);
        typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(d,uint.MaxValue);
        _=d.ApplyEditorScenarioPoint(plan);Check(d.EditorChangeCount==0,"Source unsigned counter must wrap");
        Pass(Format(native)+" checked modern overflow refuses before session consumption; retry and source uint wrap remain valid");
    }
    private static void Save(bool native)
    {
        var d=Fixture(native);foreach(var scope in Scopes)_=Branch(d,Owner(d,scope));var events=Events(d);var expected=ScriptGraph.Build(d).Roots.Where(r=>r.Target!=0).Select(r=>(r.Scope,r.Target)).OrderBy(p=>p.Scope).ToArray();
        var path=d.SaveCopy(Path.Combine(directory,"saved-"+Format(native)));var reopened=NativeMapDocument.Load(path);ExactEvents(reopened,events);
        Check(ScriptGraph.Build(reopened).Roots.Where(r=>r.Target!=0).Select(r=>(r.Scope,r.Target)).OrderBy(p=>p.Scope).SequenceEqual(expected),"Saved owner graph lost scope/target");
        Check(!reopened.HasChanges,"Reopened map dirty");Pass(Format(native)+" all nine owner branches survive actual Save/reopen with exact event bytes and clean state");
    }
}
