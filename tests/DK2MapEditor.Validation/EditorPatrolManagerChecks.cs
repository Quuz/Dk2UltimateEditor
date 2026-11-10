using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class EditorPatrolManagerChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Invalid manager operation accepted.");}
    private static string Sequence(EditorPatrolManagerRoute route)=>string.Join(',',route.Points.Select(point=>point.ID));
    private sealed class Fixture
    {
        public readonly bool Project;
        public readonly NativeMapDocument Document;
        public readonly NativeRecord[] Areas;
        public readonly EditorPatrolManagerSession Manager;
        public Fixture(bool project,params uint[] links)
        {
            Project=project;Document=NativeMapDocument.CreateNewMap(new("LocalPaths","LocalPaths",24,24,NativeFormat:!project));
            Areas=links.Select((link,i)=>Document.AddMapRegion(MapRegionKind.Area,new(2+i*2,3,1,1))).ToArray();
            for(var i=0;i<Areas.Length;i++)
            {
                Raw(i,project?"nextid":"bAreaNextID",links[i]);
                Expect(MemoryMarshal.TryGetArray(Areas[i].File.Data,out var bytes),"Fixture memory unavailable.");
                bytes.Array![bytes.Offset+Areas[i].Offset+(project?80:27)]=0xE9;
                if(project)bytes.Array[bytes.Offset+Areas[i].Offset+105]=0xE8;
                Expect(Areas[i].Fields.Single(field=>field.Name==(project?"nextid":"bAreaNextID")).Offset==(project?52:31),"Source next-ID layout mismatch.");
                Expect(Areas[i].Fields.Single(field=>field.Name==(project?"waitdelay":"wAreaWaitDelay")).Offset==(project?56:24),"Source pause layout mismatch.");
            }
            Manager=new(Document);
        }
        public void Raw(int index,string name,uint value)
        {
            var area=Areas[index];var field=area.Fields.Single(field=>field.Name==name);
            Expect(MemoryMarshal.TryGetArray(area.File.Data,out var bytes),"Fixture memory unavailable.");
            field.Encode(value.ToString()).CopyTo(bytes.Array!,bytes.Offset+area.Offset+field.Offset);
        }
        public EditorPatrolManagerRoute Route(long key)=>Manager.GetRoutes().Single(route=>route.Key==key);
        public Dictionary<NativeFile,byte[]> Bytes()=>Document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        public void Exact(Dictionary<NativeFile,byte[]> expected)=>Expect(Document.Files.Count==expected.Count&&expected.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Private manager changed unrelated fields/opaque bytes.");
        public void Check(Action command,params (int Area,uint? Next,int? Wait)[] writes)
        {
            var before=Bytes();var expected=before.ToDictionary(row=>row.Key,row=>row.Value.ToArray());
            foreach(var write in writes)
            {
                var area=Areas[write.Area];var bytes=expected[area.File];
                if(write.Next is{ } next)
                {if(Project)BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(area.Offset+52),next);else bytes[area.Offset+31]=checked((byte)next);}
                if(write.Wait is{ } wait)
                {if(Project)BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(area.Offset+56),wait);else BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(area.Offset+24),checked((ushort)wait));}
            }
            var count=Document.EditorChangeCount;var revision=Document.EditRevision;var history=Document.GetHistory();
            var activity=Document.ReadActivityStatistics()!.Encode();var notifications=0;EventHandler handler=(_,_)=>notifications++;
            Document.Changed+=handler;try{command();}finally{Document.Changed-=handler;}
            Exact(expected);var after=Document.GetHistory();
            Expect(Document.EditorChangeCount==unchecked(count+1)&&Document.EditRevision==revision+1&&notifications==1
                &&after.Undo.Count==history.Undo.Count+1&&after.ChangeCount==history.ChangeCount+1,"Accepted manager command lost its source counter/one atomic Undo.");
            Expect(Document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),"Patrol command changed activity statistics.");
            Document.Undo();Exact(before);Expect(Document.EditorChangeCount==count,"Manager Undo lost source counter.");
            Document.Redo();Exact(expected);Expect(Document.EditorChangeCount==unchecked(count+1),"Manager Redo lost source counter.");
        }
        public void Navigation(Action action)
        {
            var before=Bytes();var count=Document.EditorChangeCount;var rev=Document.EditRevision;var history=Document.GetHistory();
            action();Exact(before);var after=Document.GetHistory();
            Expect(count==Document.EditorChangeCount&&rev==Document.EditRevision&&history.ChangeCount==after.ChangeCount&&history.Undo.SequenceEqual(after.Undo)&&history.Redo.SequenceEqual(after.Redo),"Local list navigation altered history/data.");
        }
        public void Rejected(Action action)
        {
            var keys=Manager.GetRoutes().Select(route=>(route.Key,Sequence(route))).ToArray();
            Navigation(()=>Reject(action));
            Expect(keys.SequenceEqual(Manager.GetRoutes().Select(route=>(route.Key,Sequence(route)))),"Refused operation changed private lists.");
        }
    }
    public static void Run()
    {
        foreach(var project in new[]{false,true})
        {
            var f=new Fixture(project,2,0,4,5,0,0,0,0);using var manager=f.Manager;
            var groups=manager.GetRoutes();var a=groups[0].Key;var b=groups[1].Key;
            Expect(Sequence(groups[0])=="1,2"&&Sequence(groups[1])=="3,4,5","Initial source groups changed.");
            var old=groups[0];
            f.Check(()=>manager.Append(f.Route(a),f.Areas[2],7),(1,3,null),(2,null,7));
            Expect(Sequence(f.Route(a))=="1,2,3"&&Sequence(f.Route(b))=="3,4,5"&&f.Route(a).IsChanged,"Linked append auto-expanded/rebuilt source local lists or lost PathsCheck warning.");
            f.Rejected(()=>manager.Append(old,f.Areas[5],0));
            // The next append overwrites area3, not the expanded chain's area5.
            f.Check(()=>manager.Append(f.Route(a),f.Areas[5],9),(2,6,null),(5,null,9));
            Expect(Sequence(f.Route(a))=="1,2,3,6"&&Sequence(f.Route(b))=="3,4,5"&&f.Route(b).IsChanged&&!f.Route(a).IsChanged,"Indirect branch warning/local list lifetime is wrong.");
            f.Check(()=>manager.Insert(f.Route(b),1,f.Areas[6],11),(2,7,null),(6,4,11));
            Expect(Sequence(f.Route(b))=="3,7,4,5"&&f.Route(a).IsChanged,"Insert did not preserve physical selected occurrence/other local list.");
            f.Check(()=>manager.Remove(f.Route(b),1),(2,4,null),(6,0,null));
            Expect(Sequence(f.Route(b))=="3,4,5"&&manager.LastRemovalChangedRoutes,"Removal missed source all-path warning.");
            f.Check(()=>manager.Clear(f.Route(b)),(2,0,null),(3,0,null));
            Expect(manager.GetRoutes().Count==1&&Sequence(f.Route(a))=="1,2,3,6"&&manager.LastRemovalChangedRoutes,"Whole removal regenerated/removed another local list.");
            f.Navigation(manager.Reload);Expect(manager.GetRoutes().Count==1&&Sequence(manager.GetRoutes()[0])=="1,2,3","Explicit source reopen did not assemble current links.");
            using(var savedFixture=new TemporaryFixture())
            {
                var beforeSave=f.Bytes();var expectedFields=f.Document.PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay)).ToArray();
                var saved=SaveProjectionChecks.SaveCopy(f.Document,Path.Combine(savedFixture.DirectoryPath,"working"));
                var reopened=NativeMapDocument.Load(saved);f.Exact(beforeSave);
                Expect(reopened.PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay)).SequenceEqual(expectedFields),"Save serialized private local lists instead of actual area fields.");
                using var reopenedManager=new EditorPatrolManagerSession(reopened);
                Expect(string.Join(';',reopenedManager.GetRoutes().Select(Sequence))=="1,2,3","Reopen retained vanished/private occurrences.");
                var native=project?f.Document.CompileLegacyProject("DK2Preview_6a88240eaf814f78",NativeMapDocument.Create(24,24,"Basis")):f.Document;
                var compiled=SaveProjectionChecks.SaveCopy(native,Path.Combine(savedFixture.DirectoryPath,"compiled"));
                Expect(NativeMapDocument.Load(compiled).PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay)).SequenceEqual(expectedFields),"Game compilation lost actual patrol links/pauses.");
            }

            var clear=new Fixture(project,2,0,4,5,0,0);using var clearManager=clear.Manager;var ca=clearManager.GetRoutes()[0].Key;
            clear.Check(()=>clearManager.Append(clear.Route(ca),clear.Areas[2],0),(1,3,null));
            clear.Check(()=>clearManager.Clear(clear.Route(ca)),(0,0,null),(1,0,null));
            Expect(Sequence(clearManager.GetRoutes().Single())=="3,4,5","Clear erased the appended linked endpoint's existing outgoing chain.");

            var draft=new Fixture(project,0,0,0);using var dm=draft.Manager;EditorPatrolManagerRoute? empty=null;
            draft.Navigation(()=>empty=dm.CreateEmptyRoute());Expect(empty!.Points.Count==0,"Empty source draft not represented.");
            var emptyKey=empty.Key;draft.Check(()=>dm.Append(draft.Route(emptyKey),draft.Areas[0],0));
            draft.Check(()=>dm.SetPause(draft.Route(emptyKey),0,0));
            draft.Check(()=>dm.Remove(draft.Route(emptyKey),0));Expect(draft.Route(emptyKey).Points.Count==0,"Point removal erased the source empty root.");
            draft.Check(()=>dm.Clear(draft.Route(emptyKey)));Expect(dm.GetRoutes().Count==0,"Root deletion retained an empty draft.");
            var beforeCursor=draft.Document.EditorChangeCount;var cursor=dm.CreateCursorRoute(draft.Areas[1]);
            Expect(draft.Document.EditorChangeCount==beforeCursor&&Sequence(cursor)=="2","Quick cursor draft changed source count.");
            draft.Navigation(dm.Reload);Expect(dm.GetRoutes().Count==0,"Reopen persisted cursor-only/empty drafts.");
            EditorPatrolManagerRoute? start=null;draft.Check(()=>start=dm.Start(draft.Areas[0],13),(0,null,13));
            var startKey=start!.Key;
            foreach(var invalid in new[]{-1,251})draft.Rejected(()=>dm.SetPause(draft.Route(startKey),0,invalid));
            draft.Rejected(()=>dm.Insert(draft.Route(startKey),-1,draft.Areas[1],0));
            draft.Rejected(()=>dm.Remove(draft.Route(startKey),99));
            using(var foreign=new EditorPatrolManagerSession(draft.Document))
            {var wrong=foreign.CreateEmptyRoute();draft.Rejected(()=>dm.Append(wrong,draft.Areas[1],0));}
            var alien=NativeMapDocument.Create(24,24,"Foreign").AddMapRegion(MapRegionKind.Area,new(2,2,1,1));
            draft.Rejected(()=>dm.Append(draft.Route(startKey),alien,0));
            draft.Rejected(()=>draft.Document.Edit("Nested source manager refused",()=>dm.SetPause(draft.Route(startKey),0,0)));
            var stale=draft.Route(startKey);draft.Raw(0,project?"waitdelay":"wAreaWaitDelay",14);
            draft.Rejected(()=>dm.SetPause(stale,0,0));
            var beforeStructural=draft.Route(startKey);draft.Document.AddMapRegion(MapRegionKind.Area,new(20,20,1,1));
            Expect(dm.RequiresReload,"Structural additions did not invalidate manager identity scope.");
            draft.Rejected(()=>dm.Append(beforeStructural,draft.Areas[1],0));
            draft.Navigation(dm.Reload);Expect(!dm.RequiresReload,"Explicit reopen failed to clear structural invalidation.");
            var byteBeforeClose=draft.Bytes();var countBeforeClose=draft.Document.EditorChangeCount;dm.Dispose();dm.Dispose();
            Reject(()=>dm.CreateEmptyRoute());draft.Exact(byteBeforeClose);Expect(countBeforeClose==draft.Document.EditorChangeCount,"Closing rolled accepted map edits back.");

            var loop=new Fixture(project,2,1,0,0);using var lm=loop.Manager;var lk=lm.GetRoutes()[0].Key;
            loop.Rejected(()=>lm.Append(loop.Route(lk),loop.Areas[2],0));
            loop.Check(()=>lm.Remove(loop.Route(lk),2),(1,0,null));Expect(Sequence(loop.Route(lk))=="1,2","Closing occurrence removal destroyed the entry node/link.");
            loop.Check(()=>lm.Insert(loop.Route(lk),0,loop.Areas[2],5),(2,1,5));
            Expect(Sequence(loop.Route(lk))=="3,1,2","Insertion before first occurrence was regrouped.");

            var missing=new Fixture(project,250,0,0);using var mm=missing.Manager;var mk=mm.GetRoutes()[0].Key;
            missing.Check(()=>mm.Append(missing.Route(mk),missing.Areas[1],17),(1,null,17));
            Expect(Sequence(missing.Route(mk))=="1,250,2"&&!missing.Route(mk).IsChanged,"Source missing predecessor skip/PathsCheck semantics changed.");
            missing.Check(()=>mm.Clear(missing.Route(mk)),(0,0,null));

            // Close in a post-commit notification: live accepted edits stay,
            // returned snapshot is closed, no resurrected private capability.
            var closed=new Fixture(project,0,0);var cm=closed.Manager;var cr=cm.CreateCursorRoute(closed.Areas[0]);
            EventHandler close=(_,_)=>cm.Dispose();closed.Document.Changed+=close;
            closed.Check(()=>cm.Append(cr,closed.Areas[1],19),(0,2,null),(1,null,19));
            closed.Document.Changed-=close;Expect(cm.IsClosed,"Post-commit close resurrected manager.");Reject(()=>cm.GetRoutes());

            // Observer failure occurs after the document commit, not within
            // transaction rollback; the private local list must synchronize.
            var observer=new Fixture(project,0,0);using var om=observer.Manager;var ok=om.CreateCursorRoute(observer.Areas[0]).Key;
            var observerCount=observer.Document.EditorChangeCount;
            EventHandler throwing=(_,_)=>throw new InvalidOperationException("observer");observer.Document.Changed+=throwing;
            try{Reject(()=>om.Append(observer.Route(ok),observer.Areas[1],23));}finally{observer.Document.Changed-=throwing;}
            Expect(observer.Document.EditorChangeCount==unchecked(observerCount+1)&&Sequence(observer.Route(ok))=="1,2","Post-commit exception desynchronized accepted local list.");
            om.Reload();
            observer.Document.SetHistoryDepth(0);var zeroKey=om.GetRoutes()[0].Key;var zeroCount=observer.Document.EditorChangeCount;
            om.SetPause(observer.Route(zeroKey),0,0);Expect(!observer.Document.CanUndo&&observer.Document.EditorChangeCount==unchecked(zeroCount+1),"History-disabled equal pause lost acceptance count.");

            // Source arrays have514 slots including the zero terminator.
            // Repeated deliberate insertions into an indirectly changed local
            // list must fail before exhausting storage, without losing state.
            var capacity=new Fixture(project,0,0,0);using var cap=capacity.Manager;var capKey=cap.CreateCursorRoute(capacity.Areas[0]).Key;
            for(var i=1;i<513;i++)
            {capacity.Raw(1,project?"nextid":"bAreaNextID",0);cap.Insert(capacity.Route(capKey),0,capacity.Areas[1],0);}
            capacity.Raw(1,project?"nextid":"bAreaNextID",0);
            Expect(capacity.Route(capKey).Points.Count==513,"Source local-list capacity coverage failed.");
            capacity.Rejected(()=>cap.Insert(capacity.Route(capKey),0,capacity.Areas[1],0));
            capacity.Rejected(()=>cap.Append(capacity.Route(capKey),capacity.Areas[2],0));
        }
    }
}
