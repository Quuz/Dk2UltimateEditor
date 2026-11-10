using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class EditorPatrolWarningChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Invalid warning operation accepted.");}
    private sealed class Fixture : IDisposable
    {
        public readonly NativeMapDocument Document;
        public readonly NativeRecord[] Areas;
        public readonly EditorPatrolManagerSession Manager;
        public readonly bool Project;
        public Fixture(bool project,params uint[] links)
        {
            Project=project;Document=NativeMapDocument.CreateNewMap(new("WarningPaths","WarningPaths",24,24,NativeFormat:!project));
            Areas=links.Select((link,i)=>Document.AddMapRegion(MapRegionKind.Area,new(2+i*2,3,1,1))).ToArray();
            for(var i=0;i<Areas.Length;i++)
            {
                Raw(i,"nextid","bAreaNextID",links[i]);
                Expect(MemoryMarshal.TryGetArray(Areas[i].File.Data,out var bytes),"Warning fixture memory unavailable.");
                bytes.Array![bytes.Offset+Areas[i].Offset+(project?80:27)]=0xE9;
                if(project)bytes.Array[bytes.Offset+Areas[i].Offset+105]=0xE8;
            }
            Manager=new(Document);
        }
        public void Raw(int i,string project,string native,uint value)
        {
            var area=Areas[i];var field=area.Fields.Single(field=>field.Name==(Project?project:native));
            Expect(MemoryMarshal.TryGetArray(area.File.Data,out var bytes),"Fixture bytes unavailable.");
            field.Encode(value.ToString()).CopyTo(bytes.Array!,bytes.Offset+area.Offset+field.Offset);
        }
        public EditorPatrolManagerRoute Route(long key)=>Manager.GetRoutes().Single(route=>route.Key==key);
        private string Lists()=>Manager.IsClosed?"closed":string.Join(';',Manager.GetRoutes().Select(route=>route.Key+":"+string.Join(',',route.Points.Select(point=>point.ID))));
        public long ChangedSingleRoute()
        {
            var key=Manager.CreateCursorRoute(Areas[0]).Key;
            Document.LinkEditorPatrolAreas(Areas[0],Areas[1]);
            Expect(Route(key).IsChanged,"Source single-list changed fixture is not changed.");return key;
        }
        public void ReadOnly(Action action)
        {
            var before=Document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var count=Document.EditorChangeCount;var revision=Document.EditRevision;var history=Document.GetHistory();
            var dirty=Document.HasChanges;var lists=Lists();var activity=Document.ReadActivityStatistics()!.Encode();
            var notifications=0;EventHandler handler=(_,_)=>notifications++;Document.Changed+=handler;
            try{action();}finally{Document.Changed-=handler;}
            var after=Document.GetHistory();
            Expect(Document.Files.Count==before.Count&&before.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Warning changed full map bytes/opaque fields.");
            Expect(Document.EditorChangeCount==count&&Document.EditRevision==revision&&notifications==0
                &&after.Undo.Count==history.Undo.Count&&after.Redo.Count==history.Redo.Count&&after.ChangeCount==history.ChangeCount
                &&Document.HasChanges==dirty&&Lists()==lists&&Document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),
                "Warning reply changed map, counter, dirty/history, lists or activity.");
        }
        public void Dispose()=>Manager.Dispose();
    }
    public static void Run()
    {
        Expect(EditorPatrolWarningPolicy.ChangedRouteBit==0x2000,"Source warning bit differs.");
        foreach(var project in new[]{false,true})
        {
            foreach(var other in new uint[]{0,1,0x1000,0xA5A50000,0xFFFFFFFF&~0x2000u})
                foreach(var acknowledge in new[]{false,true})foreach(var suppress in new[]{false,true})foreach(var all in new[]{false,true})
                {
                    using var f=new Fixture(project,0,0,0);var key=f.ChangedSingleRoute();using var policy=new EditorPatrolWarningPolicy(other);
                    f.ReadOnly(()=>
                    {
                        using var request=(all?policy.BeginAfterRemoval(f.Manager):policy.BeginSelection(f.Manager,f.Route(key)))!;
                        Expect(request is not null&&request.IsCurrent&&request.RouteKey==key
                            &&request.Kind==(all?EditorPatrolWarningKind.AfterRemoval:EditorPatrolWarningKind.Selection),"Wrong source selection/all warning.");
                        Reject(()=>policy.BeginAfterRemoval(f.Manager));Reject(()=>policy.BeginSelection(f.Manager,f.Route(key)));
                        Expect(request!.Respond(acknowledge,suppress)&&request.IsClosed&&!request.IsCurrent,"Valid warning reply did not close its owned request.");
                        Expect(!request.Respond(true,true),"Duplicate reply retained warning authority.");
                        var expected=other|(acknowledge&&suppress?0x2000u:0);
                        Expect(policy.Flags==expected&&policy.Suppressed==(acknowledge&&suppress),"Close/default reply corrupted source request bits.");
                        using var again=policy.BeginSelection(f.Manager,f.Route(key));
                        Expect((again is null)==policy.Suppressed,"Suppressed source request failed to return without a dialog.");
                        again?.Dispose();policy.ResetSuppression();Expect(policy.Flags==other&&!policy.Suppressed,"Reset changed other request bits.");
                    });
                }
            using var main=new Fixture(project,2,0,4,5,0,0,0);using var warnings=new EditorPatrolWarningPolicy();
            var a=main.Manager.GetRoutes()[0].Key;var b=main.Manager.GetRoutes()[1].Key;
            main.ReadOnly(()=>{Expect(warnings.BeginSelection(main.Manager,null) is null,"No selection produced a warning.");Expect(warnings.BeginSelection(main.Manager,main.Route(a)) is null&&warnings.BeginAfterRemoval(main.Manager) is null,"Unchanged routes warned.");});
            main.Manager.Append(main.Route(a),main.Areas[2],9);
            main.ReadOnly(()=>
            {
                using var request=warnings.BeginSelection(main.Manager,main.Route(a));Expect(request is{Kind:EditorPatrolWarningKind.Selection},"Linked endpoint did not trigger source selection warning.");
                request!.Dispose();Expect(!warnings.Suppressed,"Dialog close remembered a checkbox.");
                Expect(warnings.BeginSelection(main.Manager,main.Route(b)) is null,"Unchanged neighbour warned.");
            });
            main.Manager.Append(main.Route(a),main.Areas[5],13);
            main.ReadOnly(()=>
            {
                Expect(warnings.BeginSelection(main.Manager,main.Route(a)) is null,"Fresh actual local edges warned.");
                using var request=warnings.BeginSelection(main.Manager,main.Route(b));Expect(request is{Kind:EditorPatrolWarningKind.Selection},"Indirect shared-route selection did not warn.");request!.Respond(true,false);
            });
            main.Manager.Remove(main.Route(a),1);
            main.ReadOnly(()=>
            {using var request=warnings.BeginAfterRemoval(main.Manager);Expect(request is{Kind:EditorPatrolWarningKind.AfterRemoval}&&request.RouteKey==b,"All-after-removal warning did not pick first changed root.");request!.Respond(false,true);Expect(!warnings.Suppressed,"Closing all warning remembered preference.");});
            main.Manager.Clear(main.Route(b));
            main.ReadOnly(()=>
            {using var request=warnings.BeginAfterRemoval(main.Manager);Expect(request?.RouteKey==a,"All warning incorrectly excluded chosen/remaining changed route.");request!.Respond(true,true);Expect(warnings.Suppressed,"OK did not remember suppression.");});
            using var nextMap=new Fixture(project,0,0);var next=nextMap.ChangedSingleRoute();
            nextMap.ReadOnly(()=>
            {Expect(warnings.BeginSelection(nextMap.Manager,nextMap.Route(next)) is null,"Session suppression was lost on map replacement.");using var restart=new EditorPatrolWarningPolicy();using var first=restart.BeginSelection(nextMap.Manager,nextMap.Route(next));Expect(first is not null,"New editor-session zero flags inherited suppression.");});
            using var stale=new Fixture(project,0,0,0);var sk=stale.ChangedSingleRoute();using var stalePolicy=new EditorPatrolWarningPolicy(0x1000);
            using(var request=stalePolicy.BeginSelection(stale.Manager,stale.Route(sk))!)
            {
                stale.Manager.CreateEmptyRoute();stale.ReadOnly(()=>{Expect(!request.IsCurrent&&!request.Respond(true,true)&&stalePolicy.Flags==0x1000,"Private navigation retained reply authority.");});
            }
            using(var request=stalePolicy.BeginSelection(stale.Manager,stale.Route(sk))!)
            {stale.Raw(2,"waitdelay","wAreaWaitDelay",37);stale.ReadOnly(()=>{Expect(!request.IsCurrent&&!request.Respond(true,true),"Raw area mutation retained reply authority.");});}
            using(var request=stalePolicy.BeginSelection(stale.Manager,stale.Route(sk))!)
            {
                var field=stale.Areas[2].Fields.Single(field=>field.Name==(project?"waitdelay":"wAreaWaitDelay"));stale.Document.SetField(stale.Areas[2],field,"41");
                stale.ReadOnly(()=>{Expect(!request.IsCurrent&&!request.Respond(true,true)&&stalePolicy.Flags==0x1000,"Document revision retained reply authority or changed flags.");});
            }
            using(var request=stalePolicy.BeginSelection(stale.Manager,stale.Route(sk))!)
            {stale.Document.AddMapRegion(MapRegionKind.Area,new(3,13,1,1));stale.ReadOnly(()=>{Expect(stale.Manager.RequiresReload&&!request.IsCurrent&&!request.Respond(true,true),"Structural change retained warning authority.");});}
            stale.Manager.Reload();var current=stale.Manager.CreateCursorRoute(stale.Areas[2]);
            stale.Document.LinkEditorPatrolAreas(stale.Areas[2],stale.Areas[0]);
            current=stale.Route(current.Key);
            using(var request=stalePolicy.BeginSelection(stale.Manager,current)!)
            {stalePolicy.ResetSuppression();stale.ReadOnly(()=>{Expect(request.IsClosed&&!request.IsCurrent&&!request.Respond(true,true)&&stalePolicy.Flags==0x1000,"Preference reset did not invalidate old request.");});}
            using(var request=stalePolicy.BeginSelection(stale.Manager,stale.Route(current.Key))!)
            {stale.Manager.Dispose();stale.ReadOnly(()=>{Expect(!request.IsCurrent&&!request.Respond(true,true),"Closed manager retained request.");request.Dispose();});}
            using var owner=new Fixture(project,0,0);var ok=owner.ChangedSingleRoute();var ownerPolicy=new EditorPatrolWarningPolicy();var closed=ownerPolicy.BeginSelection(owner.Manager,owner.Route(ok))!;
            owner.ReadOnly(()=>{ownerPolicy.Dispose();ownerPolicy.Dispose();Expect(closed.IsClosed&&!closed.IsCurrent&&!closed.Respond(true,true),"Closed policy retained authority.");Reject(()=>ownerPolicy.BeginAfterRemoval(owner.Manager));Reject(ownerPolicy.ResetSuppression);});
            using var foreignPolicy=new EditorPatrolWarningPolicy();using var foreign=foreignPolicy.BeginSelection(owner.Manager,owner.Route(ok));
            owner.ReadOnly(()=>{foreign!.Respond(true,true);Expect(foreignPolicy.Suppressed&&ownerPolicy.Flags==0,"Reply updated a foreign policy.");});
            using var guard=new EditorPatrolWarningPolicy();using var otherManager=new EditorPatrolManagerSession(owner.Document);
            owner.ReadOnly(()=>Reject(()=>guard.BeginSelection(otherManager,owner.Route(ok))));
            using var missing=new Fixture(project,250,0);using var missingPolicy=new EditorPatrolWarningPolicy();var mk=missing.Manager.GetRoutes()[0].Key;
            missing.ReadOnly(()=>Expect(missingPolicy.BeginSelection(missing.Manager,missing.Route(mk)) is null,"Missing marker was treated as a modified area."));
            missing.Raw(0,"nextid","bAreaNextID",0);
            missing.ReadOnly(()=>{using var request=missingPolicy.BeginSelection(missing.Manager,missing.Route(mk));Expect(request is not null,"Existing area mismatch before missing marker did not warn.");});
            using var finalLoop=new Fixture(project,2,3,2,0);using var loopPolicy=new EditorPatrolWarningPolicy();var lk=finalLoop.Manager.GetRoutes()[0].Key;
            finalLoop.Raw(2,"nextid","bAreaNextID",1); // The final repeated2 still targets3 within the local list; earlier3→2 mismatch is real.
            finalLoop.ReadOnly(()=>{using var request=loopPolicy.BeginSelection(finalLoop.Manager,finalLoop.Route(lk));Expect(request is not null,"Earlier loop-edge mismatch was ignored.");});
            using var finalOnly=new Fixture(project,2,0,0);using var finalPolicy=new EditorPatrolWarningPolicy();var fk=finalOnly.Manager.GetRoutes()[0].Key;
            finalOnly.Raw(1,"nextid","bAreaNextID",1);
            finalOnly.ReadOnly(()=>Expect(finalPolicy.BeginSelection(finalOnly.Manager,finalOnly.Route(fk)) is null,"Final target inside the same local list incorrectly warned."));
            finalOnly.Raw(1,"nextid","bAreaNextID",3);
            finalOnly.ReadOnly(()=>{using var request=finalPolicy.BeginSelection(finalOnly.Manager,finalOnly.Route(fk));Expect(request is not null,"Final outside target did not warn.");});
        }
        foreach(var row in new[]{
            (10,20,400,200,0,0,1920,1040,10,20),
            (1900,1030,400,200,0,0,1920,1040,1520,840),
            (-900,-1100,400,200,-1280,-1024,1280,984,-900,-1024),
            (-10,-10,400,200,-1280,0,1280,1024,-400,0),
            (100,100,2000,2000,-1280,-1024,1280,984,-1280,-1024),
            (100,100,600,300,0,40,1920,1000,100,100)})
        {
            var position=EditorPatrolWarningPolicy.PlaceAtCursor(row.Item1,row.Item2,row.Item3,row.Item4,row.Item5,row.Item6,row.Item7,row.Item8);
            Expect(position==(row.Item9,row.Item10),"Follow-mouse work-area geometry differs from independent literal expectations.");
        }
        Reject(()=>EditorPatrolWarningPolicy.PlaceAtCursor(0,0,0,1,0,0,100,100));
        Reject(()=>EditorPatrolWarningPolicy.PlaceAtCursor(0,0,1,1,0,0,-1,100));
    }
}
