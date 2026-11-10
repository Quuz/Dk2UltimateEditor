using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class EditorPatrolRemovalChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Invalid removal request accepted.");}
    private sealed class Fixture : IDisposable
    {
        public readonly NativeMapDocument Document;
        public readonly NativeRecord[] Areas;
        public readonly EditorPatrolManagerSession Manager;
        public readonly bool Project;
        public Fixture(bool project,params uint[] links)
        {
            Project=project;Document=NativeMapDocument.CreateNewMap(new("RemovalPaths","RemovalPaths",24,24,NativeFormat:!project));
            Areas=links.Select((_,i)=>Document.AddMapRegion(MapRegionKind.Area,new(2+i*2,3,1,1))).ToArray();
            for(var i=0;i<Areas.Length;i++)
            {
                Raw(i,"nextid","bAreaNextID",links[i]);
                Expect(MemoryMarshal.TryGetArray(Areas[i].File.Data,out var bytes),"Fixture memory unavailable.");
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
        public Dictionary<NativeFile,byte[]> Bytes()=>Document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        private string Lists()=>Manager.IsClosed?"closed":string.Join(';',Manager.GetRoutes().Select(route=>route.Key+":"+string.Join(',',route.Points.Select(point=>point.ID))));
        private void Exact(Dictionary<NativeFile,byte[]> bytes)=>Expect(Document.Files.Count==bytes.Count&&bytes.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Removal changed unrelated/opaque fields.");
        public void ReadOnly(Action action)
        {
            var before=Bytes();var revision=Document.EditRevision;var count=Document.EditorChangeCount;var history=Document.GetHistory();
            var dirty=Document.HasChanges;var lists=Lists();var activity=Document.ReadActivityStatistics()!.Encode();
            var notifications=0;EventHandler handler=(_,_)=>notifications++;Document.Changed+=handler;
            try{action();}finally{Document.Changed-=handler;}
            var after=Document.GetHistory();Exact(before);
            Expect(Document.EditRevision==revision&&Document.EditorChangeCount==count&&notifications==0
                &&after.Undo.SequenceEqual(history.Undo)&&after.Redo.SequenceEqual(history.Redo)&&after.ChangeCount==history.ChangeCount
                &&Document.HasChanges==dirty&&Lists()==lists&&Document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),
                "Unaccepted request changed map/private lists/history/counters/activity.");
        }
        public void Check(Action action,params (int Area,uint Next)[] writes)
        {
            var before=Bytes();var expected=before.ToDictionary(row=>row.Key,row=>row.Value.ToArray());
            foreach(var write in writes)
            {
                var area=Areas[write.Area];
                if(Project)BinaryPrimitives.WriteUInt32LittleEndian(expected[area.File].AsSpan(area.Offset+52),write.Next);
                else expected[area.File][area.Offset+31]=checked((byte)write.Next);
            }
            var revision=Document.EditRevision;var count=Document.EditorChangeCount;var history=Document.GetHistory();
            var activity=Document.ReadActivityStatistics()!.Encode();var notifications=0;EventHandler handler=(_,_)=>notifications++;
            Document.Changed+=handler;try{action();}finally{Document.Changed-=handler;}
            Exact(expected);var after=Document.GetHistory();
            Expect(Document.EditRevision==revision+1&&Document.EditorChangeCount==unchecked(count+1)&&notifications==1
                &&after.Undo.Count==history.Undo.Count+1&&after.ChangeCount==history.ChangeCount+1
                &&Document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),"Accepted request did not own exactly one manager command/Undo/counter.");
            Document.Undo();Exact(before);Expect(Document.EditorChangeCount==count,"Removal Undo count differs.");
            Document.Redo();Exact(expected);Expect(Document.EditorChangeCount==unchecked(count+1),"Removal Redo count differs.");
        }
        public void Dispose()=>Manager.Dispose();
    }
    private static EditorPatrolRemovalRequest Begin(EditorPatrolRemovalPolicy policy,Fixture f,bool clear)
    {
        var route=f.Manager.GetRoutes()[0];return clear?policy.BeginRoute(f.Manager,route):policy.BeginPoint(f.Manager,route,1);
    }
    public static void Run()
    {
        Expect(EditorPatrolRemovalPolicy.RemovalBit==0x1000&&EditorPatrolWarningPolicy.ChangedRouteBit==0x2000,"Source request bits differ.");
        foreach(var project in new[]{false,true})
        {
            foreach(var other in new uint[]{0,1,0x2000,0xA5A50000,0xFFFFEFFF})
                foreach(var clear in new[]{false,true})foreach(var remember in new[]{false,true})
                {
                    foreach(var answer in new[]{"Yes","No","Close"})
                    {
                        using var f=new Fixture(project,2,3,0,0);using var policy=new EditorPatrolRemovalPolicy(other);
                        EditorPatrolRemovalRequest? request=null;
                        f.ReadOnly(()=>{request=Begin(policy,f,clear);Expect(request.IsCurrent&&request.RequiresPrompt&&!request.IsAccepted&&request.Kind==(clear?EditorPatrolRemovalKind.Route:EditorPatrolRemovalKind.Point),"Wrong owned source request.");Reject(()=>Begin(policy,f,clear));});
                        using(request!)
                        {
                            if(answer=="Yes")
                            {
                                f.Check(()=>{request!.Accept(remember);Expect(request.IsAccepted&&request.IsClosed&&!request.IsCurrent,"Yes retained command authority.");},clear?[(0,0u),(1,0u)]:[(0,3u),(1,0u)]);
                                Expect(policy.Flags==(other|(remember?0x1000u:0)),"Yes changed another source request bit.");
                                f.ReadOnly(()=>Reject(()=>request!.Accept(true)));
                            }
                            else f.ReadOnly(()=>
                            {
                                if(answer=="No")
                                {Expect(request!.CanDecline(remember)==!remember,"Remember checkbox did not disable No.");Expect(request.Decline(remember)==!remember,"No reply accepted while disabled or failed unchecked.");}
                                request!.Dispose();Expect(!request.IsAccepted&&request.IsClosed&&!request.IsCurrent&&policy.Flags==other,"No/close remembered a checkbox or consumed map command.");
                            });
                        }
                        f.ReadOnly(()=>policy.ResetSuppression());Expect(policy.Flags==other,"Reset changed unrelated flags.");
                    }
                    using var auto=new Fixture(project,2,3,0,0);using var automatic=new EditorPatrolRemovalPolicy(other|0x1000);
                    using var automaticRequest=Begin(automatic,auto,clear);
                    Expect(!automaticRequest.RequiresPrompt&&automaticRequest.IsCurrent,"Suppressed request did not use default Yes without a prompt.");
                    auto.Check(()=>automaticRequest.Accept(remember),clear?[(0,0u),(1,0u)]:[(0,3u),(1,0u)]);
                    Expect(automatic.Flags==(other|0x1000)&&automaticRequest.IsAccepted,"Automatic default reply changed existing flags.");
                }
            using var main=new Fixture(project,2,0,4,5,0,0);using var removal=new EditorPatrolRemovalPolicy();using var warnings=new EditorPatrolWarningPolicy();
            var a=main.Manager.GetRoutes()[0].Key;
            main.Manager.Append(main.Route(a),main.Areas[2],7);
            using(var request=removal.BeginPoint(main.Manager,main.Route(a),1))main.Check(()=>request.Accept(true),(0,3),(1,0));
            main.ReadOnly(()=>{using var warning=warnings.BeginAfterRemoval(main.Manager);Expect(warning is not null,"Accepted removal skipped changed selected root in all warning.");warning!.Respond(true,true);});
            Expect(removal.Suppressed&&warnings.Suppressed,"Independent source flags lost accepted preferences.");
            main.ReadOnly(removal.ResetSuppression);Expect(!removal.Suppressed&&warnings.Suppressed,"Removal reset cleared warning flag.");
            main.ReadOnly(warnings.ResetSuppression);Expect(!warnings.Suppressed&&!removal.Suppressed,"Warning reset enabled removal auto reply.");
            using(var request=removal.BeginRoute(main.Manager,main.Route(a)))main.Check(()=>request.Accept(true),(0,0));
            Expect(main.Manager.GetRoutes().Count==1&&main.Document.PatrolAreas()[2].NextID==4,"Clear removed areas or final outgoing chain.");
            using var next=new Fixture(project,2,3,0);
            using(var request=Begin(removal,next,false)){Expect(!request.RequiresPrompt,"Map replacement reset runtime confirmation.");next.Check(()=>request.Accept(),(0,3),(1,0));}
            using var restart=new EditorPatrolRemovalPolicy();using(var request=restart.BeginRoute(next.Manager,next.Manager.GetRoutes()[0]))Expect(request.RequiresPrompt,"Fresh editor session inherited suppression.");

            using var single=new Fixture(project,0,0);using var singlePolicy=new EditorPatrolRemovalPolicy();
            var root=single.Manager.CreateCursorRoute(single.Areas[0]).Key;
            using(var request=singlePolicy.BeginPoint(single.Manager,single.Route(root),0))single.Check(()=>request.Accept());
            Expect(single.Route(root).Points.Count==0,"One-point removal did not retain empty source root.");
            using(var request=singlePolicy.BeginRoute(single.Manager,single.Route(root)))single.Check(()=>request.Accept());
            Expect(single.Manager.GetRoutes().Count==0,"Empty root Clear did not remove private list.");

            foreach(var change in new[]{"private","raw","revision","structure","reload","closed","reset"})
            {
                using var stale=new Fixture(project,2,3,0,0);using var policy=new EditorPatrolRemovalPolicy(0x2000);
                using var request=Begin(policy,stale,false);
                switch(change)
                {
                    case "private":stale.Manager.CreateEmptyRoute();break;
                    case "raw":stale.Raw(3,"waitdelay","wAreaWaitDelay",37);break;
                    case "revision":var area=stale.Areas[3];stale.Document.SetField(area,area.Fields.Single(field=>field.Name==(project?"waitdelay":"wAreaWaitDelay")),"41");break;
                    case "structure":stale.Document.AddMapRegion(MapRegionKind.Area,new(3,13,1,1));break;
                    case "reload":stale.Manager.Reload();break;
                    case "closed":stale.Manager.Dispose();break;
                    case "reset":policy.ResetSuppression();break;
                }
                stale.ReadOnly(()=>{Expect(!request.IsCurrent&&!request.Decline(),"Stale request retained reply authority.");Reject(()=>request.Accept(true));Expect(policy.Flags==0x2000,"Stale Yes changed suppression.");});
            }
            using var guard=new Fixture(project,2,3,0);using var guardPolicy=new EditorPatrolRemovalPolicy();
            guard.ReadOnly(()=>{Reject(()=>guardPolicy.BeginPoint(guard.Manager,guard.Manager.GetRoutes()[0],-1));Reject(()=>guardPolicy.BeginPoint(guard.Manager,guard.Manager.GetRoutes()[0],3));});
            using var foreignManager=new EditorPatrolManagerSession(guard.Document);
            guard.ReadOnly(()=>Reject(()=>guardPolicy.BeginRoute(foreignManager,guard.Manager.GetRoutes()[0])));
            var pending=Begin(guardPolicy,guard,false);
            guard.ReadOnly(()=>{guardPolicy.Dispose();guardPolicy.Dispose();Expect(pending.IsClosed&&!pending.IsCurrent&&!pending.IsAccepted,"Disposed policy retained authority.");Reject(()=>pending.Accept());Reject(()=>Begin(guardPolicy,guard,false));Reject(guardPolicy.ResetSuppression);});

            using var nested=new Fixture(project,2,3,0);using var nestedPolicy=new EditorPatrolRemovalPolicy();using var nestedRequest=Begin(nestedPolicy,nested,false);
            EventHandler reenter=(_,_)=>{if(!nestedRequest.IsApplying)return;Expect(!nestedRequest.IsCurrent,"Applying request was current.");Reject(()=>nestedRequest.Accept());Reject(()=>Begin(nestedPolicy,nested,false));Reject(nestedPolicy.ResetSuppression);};
            nested.Document.Changed+=reenter;try{nested.Check(()=>nestedRequest.Accept(true),(0,3),(1,0));}finally{nested.Document.Changed-=reenter;}
            Expect(nestedRequest.IsAccepted&&nestedRequest.IsClosed,"Nested reply consumed a second operation.");
            using var observed=new Fixture(project,2,3,0);using var observedPolicy=new EditorPatrolRemovalPolicy();using var observedRequest=Begin(observedPolicy,observed,false);
            var beforeRevision=observed.Document.EditRevision;var beforeCount=observed.Document.EditorChangeCount;var beforeHistory=observed.Document.GetHistory();
            EventHandler fail=(_,_)=>throw new InvalidOperationException("Intentional postcommit observer failure.");observed.Document.Changed+=fail;
            try{Reject(()=>observedRequest.Accept(true));}finally{observed.Document.Changed-=fail;}
            Expect(observedRequest.IsAccepted&&observedRequest.IsClosed&&!observedRequest.IsCurrent&&observedPolicy.Suppressed
                &&observed.Document.EditRevision==beforeRevision+1&&observed.Document.EditorChangeCount==unchecked(beforeCount+1)
                &&observed.Document.GetHistory().Undo.Count==beforeHistory.Undo.Count+1
                &&observed.Manager.GetRoutes()[0].Points.Select(point=>point.ID).SequenceEqual(new uint[]{1,3}),"Postcommit failure rolled back accepted bytes/private list or retained authority.");
            observed.ReadOnly(()=>Reject(()=>observedRequest.Accept()));
            using var closed=new Fixture(project,2,3,0);using var closedPolicy=new EditorPatrolRemovalPolicy();using var closedRequest=Begin(closedPolicy,closed,false);
            EventHandler close=(_,_)=>closed.Manager.Dispose();closed.Document.Changed+=close;
            try{closedRequest.Accept(true);}finally{closed.Document.Changed-=close;}
            Expect(closedRequest.IsAccepted&&closedRequest.IsClosed&&closed.Manager.IsClosed&&closedPolicy.Suppressed,"Owner close after commit resurrected manager or lost accepted response.");
            using var saved=new TemporaryFixture();
            var stored=observed.Document.PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay)).ToArray();
            var path=SaveProjectionChecks.SaveCopy(observed.Document,Path.Combine(saved.DirectoryPath,"working"));var reopened=NativeMapDocument.Load(path);
            Expect(stored.SequenceEqual(reopened.PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay))),"Accepted removal did not survive Save/reopen.");
            if(project)
            {
                var compiled=reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78",NativeMapDocument.Create(24,24,"RemovalBasis"));
                Expect(stored.SequenceEqual(compiled.PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay))),"Game compilation lost accepted removal.");
            }
        }
    }
}
