using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class EditorPatrolSelectionChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Invalid selector operation accepted.");}
    private sealed class Fixture : IDisposable
    {
        public readonly NativeMapDocument Document;
        public readonly NativeRecord[] Areas;
        public EditorPatrolManagerSession Manager=null!;
        public readonly bool Project;
        public Fixture(bool project,params uint[] links)
        {
            Project=project;Document=NativeMapDocument.CreateNewMap(new("SelectedPaths","SelectedPaths",24,24,NativeFormat:!project));
            Areas=links.Select((link,i)=>Document.AddMapRegion(MapRegionKind.Area,new(2+i*2,3,1,1))).ToArray();
            for(var i=0;i<Areas.Length;i++)
            {
                Raw(i,"nextid","bAreaNextID",links[i]);
                Expect(MemoryMarshal.TryGetArray(Areas[i].File.Data,out var bytes),"Fixture memory unavailable.");
                bytes.Array![bytes.Offset+Areas[i].Offset+(project?80:27)]=0xE9;
                if(project)bytes.Array[bytes.Offset+Areas[i].Offset+105]=0xE8;
                Expect(Areas[i].Fields.Single(field=>field.Name==(project?"nextid":"bAreaNextID")).Offset==(project?52:31),"Independent next offset mismatch.");
                Expect(Areas[i].Fields.Single(field=>field.Name==(project?"waitdelay":"wAreaWaitDelay")).Offset==(project?56:24),"Independent pause offset mismatch.");
            }
        }
        public void Open()=>Manager=new(Document);
        public void Raw(int i,string project,string native,uint value)
        {
            var area=Areas[i];var field=area.Fields.Single(field=>field.Name==(Project?project:native));
            Expect(MemoryMarshal.TryGetArray(area.File.Data,out var bytes),"Fixture bytes unavailable.");
            field.Encode(value.ToString()).CopyTo(bytes.Array!,bytes.Offset+area.Offset+field.Offset);
        }
        public EditorPatrolManagerRoute Route(long key)=>Manager.GetRoutes().Single(route=>route.Key==key);
        public Dictionary<NativeFile,byte[]> Bytes()=>Document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        public void Exact(Dictionary<NativeFile,byte[]> expected)=>Expect(Document.Files.Count==expected.Count&&expected.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Selector changed unrelated map bytes.");
        private string Lists()=>string.Join(';',Manager.GetRoutes().Select(route=>route.Key+":"+string.Join(',',route.Points.Select(point=>point.ID))));
        public void Navigation(Action action)
        {
            var before=Bytes();var count=Document.EditorChangeCount;var revision=Document.EditRevision;var history=Document.GetHistory();
            var lists=Lists();var notifications=0;EventHandler handler=(_,_)=>notifications++;Document.Changed+=handler;
            try{action();}finally{Document.Changed-=handler;}
            Exact(before);var after=Document.GetHistory();
            Expect(Document.EditorChangeCount==count&&Document.EditRevision==revision&&notifications==0
                &&after.Undo.Count==history.Undo.Count&&after.Redo.Count==history.Redo.Count&&after.ChangeCount==history.ChangeCount
                &&Lists()==lists,"Selector navigation/Cancel changed map, counter, local lists or history.");
        }
        public Dictionary<NativeFile,byte[]> Expected(params (int Area,uint? Next,int? Wait)[] writes)
        {
            var expected=Bytes();
            foreach(var write in writes)
            {
                var area=Areas[write.Area];var bytes=expected[area.File];
                if(write.Next is{} next)
                {if(Project)BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(area.Offset+52),next);else bytes[area.Offset+31]=checked((byte)next);}
                if(write.Wait is{} wait)
                {if(Project)BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(area.Offset+56),wait);else BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(area.Offset+24),checked((ushort)wait));}
            }
            return expected;
        }
        public EditorPatrolManagerRoute Accept(EditorPatrolAreaSelectionSession selector,params (int Area,uint? Next,int? Wait)[] writes)
        {
            var before=Bytes();var expected=Expected(writes);var count=Document.EditorChangeCount;var revision=Document.EditRevision;
            var history=Document.GetHistory();var activity=Document.ReadActivityStatistics()!.Encode();var notifications=0;
            EventHandler handler=(_,_)=>notifications++;Document.Changed+=handler;EditorPatrolManagerRoute result;
            try{result=selector.Accept();}finally{Document.Changed-=handler;}
            Exact(expected);var after=Document.GetHistory();
            Expect(selector.IsClosed&&!selector.CanAccept&&Document.EditorChangeCount==unchecked(count+1)
                &&Document.EditRevision==revision+1&&notifications==1&&after.Undo.Count==history.Undo.Count+1
                &&after.ChangeCount==history.ChangeCount+1,"Selector acceptance lost atomic source count/history/close.");
            Expect(Document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),"Selector changed activity data.");
            Document.Undo();Exact(before);Expect(Document.EditorChangeCount==count,"Selector Undo counter differs.");
            Document.Redo();Exact(expected);Expect(Document.EditorChangeCount==unchecked(count+1),"Selector Redo counter differs.");
            return result;
        }
        public void Dispose(){Manager?.Dispose();}
    }
    private static EditorPatrolAreaCandidate Candidate(EditorPatrolAreaSelectionSession selection,uint id)=>selection.Candidates.Single(candidate=>candidate.ID==id);
    public static void Run(string root)
    {
        var rows=File.ReadAllLines(Path.Combine(root,"tests/Fixtures/PatrolSelection/source-candidates.tsv"))
            .Where(line=>!line.StartsWith('#')).Select(line=>line.Split('\t')).ToArray();
        Expect(rows.Length==16,"Source candidate fixture incomplete.");
        foreach(var project in new[]{false,true})
        {
            foreach(var row in rows)
            {
                var mask=int.Parse(row[0]);var order=new uint[]{3,1,4,2,5};
                using var f=new Fixture(project,0,0,0,0,0);
                for(var i=0;i<order.Length;i++)
                {f.Raw(i,"id","bAreaID",order[i]);f.Raw(i,"nextid","bAreaNextID",order[i]<=4&&(mask&(1<<((int)order[i]-1)))!=0?order[i]:0);}
                f.Open();var route=f.Manager.CreateCursorRoute(f.Areas[4]);
                f.Navigation(()=>
                {
                    using var append=f.Manager.SelectAppend(route);using var insert=f.Manager.SelectInsert(route,0);
                    Expect(string.Join(',',append.Candidates.Select(candidate=>candidate.ID))=="3,1,4,2,5","Append lost physical order/linked candidates.");
                    Expect(string.Join(',',insert.Candidates.Select(candidate=>candidate.ID))==row[1],"Insert candidate filter differs from literal source fixture.");
                    Expect(append.Selected is null&&insert.Selected is null&&append.Pause==0&&insert.Pause==0&&!append.CanAccept&&!insert.CanAccept,"Selector silently chose an initial candidate.");
                    Expect(append.Candidates.All(candidate=>candidate.Selectable),"Valid source candidate disabled.");
                });
            }
            using var main=new Fixture(project,2,0,4,5,0,0,0);main.Raw(2,"waitdelay","wAreaWaitDelay",65535);main.Raw(5,"waitdelay","wAreaWaitDelay",19);main.Open();
            var key=main.Manager.GetRoutes()[0].Key;
            using(var selection=main.Manager.SelectAppend(main.Route(key)))
            {
                main.Navigation(()=>
                {
                    Reject(()=>selection.Accept());selection.Select(Candidate(selection,3));Expect(selection.Pause==250,"Selected imported pause did not clamp without mutation.");
                    selection.SetPauseText(" 17 ");Expect(selection.Pause==17,"Integer pause parsing changed.");
                    selection.Select(Candidate(selection,6));Expect(selection.Pause==19,"Changing candidate did not initialize its stored pause.");
                    selection.Select(null);Expect(selection.Pause==19&&!selection.CanAccept,"Deselect changed pause or retained authority.");
                    selection.SetPause(-8);Expect(selection.Pause==0,"Negative pause did not clamp.");selection.SetPause(999);Expect(selection.Pause==250,"Large pause did not clamp.");
                    selection.SetPauseText("bad");Expect(selection.Pause==0,"Invalid numeric input differs from zero conversion.");
                    selection.SetPauseText("");Expect(selection.Pause==0,"Empty numeric input differs from zero conversion.");
                    selection.Select(Candidate(selection,3));selection.SetPause(7);
                    using var foreign=main.Manager.SelectAppend(main.Route(key));Reject(()=>selection.Select(Candidate(foreign,3)));
                    Expect(selection.Selected?.ID==3&&selection.Pause==7,"Foreign candidate refusal changed draft.");
                });
                main.Accept(selection,(1,3,null),(2,null,7));
                Reject(()=>selection.Accept());Reject(()=>selection.Select(null));
            }
            Expect(string.Join(',',main.Route(key).Points.Select(point=>point.ID))=="1,2,3","Accepted linked choice traced its chain into local list.");
            using(var selection=main.Manager.SelectAppend(main.Route(key)))
            {selection.Select(Candidate(selection,6));selection.SetPause(13);main.Accept(selection,(2,6,null),(5,null,13));}
            using(var selection=main.Manager.SelectInsert(main.Route(key),1))
            {Expect(selection.Candidates.All(candidate=>candidate.NextID==0),"Insert exposes linked candidates.");selection.Select(Candidate(selection,7));selection.SetPause(23);main.Accept(selection,(0,7,null),(6,2,23));}
            using(var canceled=main.Manager.SelectInsert(main.Route(key),0))
                main.Navigation(()=>{canceled.Select(canceled.Candidates[0]);canceled.SetPause(250);canceled.Dispose();Expect(!canceled.IsCurrent&&!canceled.CanAccept,"Canceled selection retained authority.");Reject(()=>canceled.Accept());});
            using(var copy=new TemporaryFixture())
            {
                var before=main.Bytes();var fields=main.Document.PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay)).ToArray();
                var saved=SaveProjectionChecks.SaveCopy(main.Document,Path.Combine(copy.DirectoryPath,"saved"));
                Expect(NativeMapDocument.Load(saved).PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay)).SequenceEqual(fields),"Saved selector choice differs.");
                var native=project?main.Document.CompileLegacyProject("DK2Preview_6a88240eaf814f78",NativeMapDocument.Create(24,24,"Basis")):main.Document;
                var compiled=SaveProjectionChecks.SaveCopy(native,Path.Combine(copy.DirectoryPath,"compiled"));
                Expect(NativeMapDocument.Load(compiled).PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay)).SequenceEqual(fields),"Compiled selector choice differs.");main.Exact(before);
            }
            using var equal=new Fixture(project,0,0);equal.Open();var empty=equal.Manager.CreateEmptyRoute();
            using(var selection=equal.Manager.SelectAppend(empty)){selection.Select(Candidate(selection,1));equal.Accept(selection);}
            using(var selection=equal.Manager.SelectAppend(equal.Route(empty.Key)))
            {
                selection.Select(Candidate(selection,2));
                var count=equal.Document.EditorChangeCount;
                equal.Document.Edit("nested",()=>Reject(()=>selection.Accept()));Expect(equal.Document.EditorChangeCount==count&&selection.IsCurrent,"Rejected nested acceptance modified state.");
                equal.Navigation(()=>{selection.Select(Candidate(selection,2));selection.Dispose();});
            }
            using var stale=new Fixture(project,0,0,0);stale.Open();var sk=stale.Manager.CreateCursorRoute(stale.Areas[0]).Key;
            using(var selection=stale.Manager.SelectAppend(stale.Route(sk)))
            {selection.Select(Candidate(selection,2));stale.Manager.CreateEmptyRoute();stale.Navigation(()=>{Expect(!selection.IsCurrent&&!selection.CanAccept,"Private navigation did not stale selector.");Reject(()=>selection.Accept());});}
            using(var selection=stale.Manager.SelectAppend(stale.Route(sk)))
            {selection.Select(Candidate(selection,2));stale.Raw(2,"waitdelay","wAreaWaitDelay",9);stale.Navigation(()=>{Expect(!selection.IsCurrent,"Raw unrevisioned bytes retained selector authority.");Reject(()=>selection.Accept());});}
            using(var selection=stale.Manager.SelectAppend(stale.Route(sk)))
            {selection.Select(Candidate(selection,2));var wait=stale.Areas[2].Fields.Single(field=>field.Name==(project?"waitdelay":"wAreaWaitDelay"));stale.Document.SetField(stale.Areas[2],wait,"11");stale.Navigation(()=>{Expect(!selection.IsCurrent,"Revision changed without stale selector.");Reject(()=>selection.Accept());});}
            using(var selection=stale.Manager.SelectAppend(stale.Route(sk)))
            {stale.Document.AddMapRegion(MapRegionKind.Area,new(2,12,1,1));stale.Navigation(()=>{Expect(stale.Manager.RequiresReload&&!selection.IsCurrent,"Structural change retained selector.");Reject(()=>selection.Accept());});}
            stale.Manager.Reload();var current=stale.Manager.CreateCursorRoute(stale.Areas[0]);using var closing=stale.Manager.SelectAppend(current);
            var closeBytes=stale.Bytes();var closeCount=stale.Document.EditorChangeCount;stale.Manager.Dispose();closing.Dispose();
            Expect(!closing.IsCurrent&&!closing.CanAccept&&stale.Document.EditorChangeCount==closeCount,"Closing selector/manager modified map.");stale.Exact(closeBytes);Reject(()=>closing.Accept());

            using var invalid=new Fixture(project,0,0,0,0,0);invalid.Raw(1,"id","bAreaID",1);invalid.Raw(2,"id","bAreaID",0);invalid.Open();
            var valid=invalid.Manager.CreateCursorRoute(invalid.Areas[4]);
            invalid.Navigation(()=>
            {
                using var selection=invalid.Manager.SelectAppend(valid);
                Expect(selection.Candidates.Count==5&&selection.Candidates.Count(candidate=>!candidate.Selectable)==3,"Malformed candidates were hidden or granted authority.");
                foreach(var candidate in selection.Candidates.Where(candidate=>!candidate.Selectable))
                {selection.Select(candidate);Expect(!selection.CanAccept,"Ambiguous candidate accepted.");Reject(()=>selection.Accept());}
            });
            using var loop=new Fixture(project,2,1,0);loop.Open();var lr=loop.Manager.GetRoutes()[0];
            var rootAvailability=loop.Manager.GetAvailability(lr,-1);var pointAvailability=loop.Manager.GetAvailability(lr,0);
            Expect(rootAvailability.New&&!rootAvailability.Append&&!rootAvailability.Insert&&!rootAvailability.Pause&&!rootAvailability.RemovePoint&&rootAvailability.Clear,"Source root button availability differs.");
            Expect(pointAvailability.New&&!pointAvailability.Append&&pointAvailability.Insert&&pointAvailability.Pause&&pointAvailability.RemovePoint&&pointAvailability.Clear,"Source point button availability differs.");
            loop.Navigation(()=>{Reject(()=>loop.Manager.SelectAppend(lr));Reject(()=>loop.Manager.SelectInsert(lr,-1));using var select=loop.Manager.SelectInsert(lr,0);Expect(select.Candidates.Count==1&&select.Candidates[0].ID==3,"Closed route insertion lost unlinked candidate.");});
            using var unavailable=new Fixture(project,2,1);unavailable.Open();var ur=unavailable.Manager.GetRoutes()[0];var ua=unavailable.Manager.GetAvailability(ur,0);
            Expect(!ua.New&&!ua.Append&&!ua.Insert&&ua.Pause&&ua.Clear&&ua.RemovePoint,"No-unlinked availability differs.");unavailable.Navigation(()=>{Reject(()=>unavailable.Manager.SelectAppend(ur));Reject(()=>unavailable.Manager.SelectInsert(ur,0));});
            using var missing=new Fixture(project,250,0);missing.Open();var mr=missing.Manager.GetRoutes()[0];var ma=missing.Manager.GetAvailability(mr,1);
            Expect(ma.Append&&!ma.Insert&&!ma.Pause&&ma.RemovePoint&&ma.Clear,"Missing occurrence availability differs.");missing.Navigation(()=>Reject(()=>missing.Manager.SelectInsert(mr,1)));
            using var observer=new Fixture(project,0,0);observer.Open();var oroute=observer.Manager.CreateCursorRoute(observer.Areas[0]);using var pending=observer.Manager.SelectAppend(oroute);
            pending.Select(Candidate(pending,2));pending.SetPause(31);var expected=observer.Expected((0,2,null),(1,null,31));var oldCount=observer.Document.EditorChangeCount;
            EventHandler handler=(_,_)=>{Reject(()=>pending.Accept());throw new InvalidOperationException("observer");};observer.Document.Changed+=handler;
            try{Reject(()=>pending.Accept());}finally{observer.Document.Changed-=handler;}
            observer.Exact(expected);Expect(pending.IsClosed&&!pending.CanAccept&&observer.Document.EditorChangeCount==unchecked(oldCount+1)
                &&string.Join(',',observer.Route(oroute.Key).Points.Select(point=>point.ID))=="1,2","Post-commit failure retained selector authority or lost private list.");Reject(()=>pending.Accept());
            using var notifiedClose=new Fixture(project,0,0);notifiedClose.Open();var nr=notifiedClose.Manager.CreateCursorRoute(notifiedClose.Areas[0]);
            using var notifiedSelection=notifiedClose.Manager.SelectAppend(nr);notifiedSelection.Select(Candidate(notifiedSelection,2));notifiedSelection.SetPause(41);
            EventHandler dispose=(_,_)=>{notifiedSelection.Dispose();notifiedClose.Manager.Dispose();};notifiedClose.Document.Changed+=dispose;
            try{notifiedClose.Accept(notifiedSelection,(0,2,null),(1,null,41));}finally{notifiedClose.Document.Changed-=dispose;}
            Expect(notifiedSelection.IsClosed&&notifiedClose.Manager.IsClosed,"Post-commit close resurrected selection or manager.");Reject(()=>notifiedSelection.Accept());
            using var cap=new Fixture(project,0,0,0);cap.Open();var ck=cap.Manager.CreateCursorRoute(cap.Areas[0]).Key;
            for(var i=1;i<513;i++){cap.Raw(1,"nextid","bAreaNextID",0);cap.Manager.Insert(cap.Route(ck),0,cap.Areas[1],0);}cap.Raw(1,"nextid","bAreaNextID",0);
            var cr=cap.Route(ck);Expect(!cap.Manager.GetAvailability(cr,0).Append&&!cap.Manager.GetAvailability(cr,0).Insert,"Capacity did not disable selection commands.");
            cap.Navigation(()=>{Reject(()=>cap.Manager.SelectAppend(cr));Reject(()=>cap.Manager.SelectInsert(cr,0));});
        }
    }
}
