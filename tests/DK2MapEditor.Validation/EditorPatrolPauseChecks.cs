using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class EditorPatrolPauseChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Invalid pause operation accepted.");}
    private sealed class Fixture : IDisposable
    {
        public readonly NativeMapDocument Document;
        public readonly NativeRecord[] Areas;
        public readonly EditorPatrolManagerSession Manager;
        public readonly bool Project;
        public Fixture(bool project,uint initial=17)
        {
            Project=project;Document=NativeMapDocument.CreateNewMap(new("PausePaths","PausePaths",24,24,NativeFormat:!project));
            Areas=Enumerable.Range(0,3).Select(i=>Document.AddMapRegion(MapRegionKind.Area,new(2+i*3,3,1,1))).ToArray();
            Raw(0,project?52:31,2);Raw(1,project?52:31,3);Raw(0,project?56:24,initial);
            foreach(var area in Areas)
            {
                Expect(MemoryMarshal.TryGetArray(area.File.Data,out var bytes),"Fixture memory unavailable.");
                bytes.Array![bytes.Offset+area.Offset+(project?80:27)]=0xE9;
                if(project)bytes.Array[bytes.Offset+area.Offset+105]=0xE8;
            }
            Manager=new(Document);
        }
        public void Raw(int i,int offset,uint number)
        {
            var area=Areas[i];Expect(MemoryMarshal.TryGetArray(area.File.Data,out var bytes),"Fixture bytes unavailable.");
            if(Project)BinaryPrimitives.WriteUInt32LittleEndian(bytes.Array!.AsSpan(bytes.Offset+area.Offset+offset),number);
            else if(offset==31)bytes.Array![bytes.Offset+area.Offset+offset]=checked((byte)number);
            else BinaryPrimitives.WriteUInt16LittleEndian(bytes.Array!.AsSpan(bytes.Offset+area.Offset+offset),checked((ushort)number));
        }
        public EditorPatrolPauseSession Select(int occurrence=0)=>Manager.SelectPause(Manager.GetRoutes()[0],occurrence);
        private Dictionary<NativeFile,byte[]> Bytes()=>Document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        private string Lists()=>Manager.IsClosed?"closed":string.Join(';',Manager.GetRoutes().Select(route=>route.Key+":"+string.Join(',',route.Points.Select(point=>point.ID))));
        private void Exact(Dictionary<NativeFile,byte[]> bytes)=>Expect(Document.Files.Count==bytes.Count&&bytes.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Pause changed unrelated/full opaque bytes.");
        public void ReadOnly(Action action)
        {
            var bytes=Bytes();var revision=Document.EditRevision;var count=Document.EditorChangeCount;var history=Document.GetHistory();
            var lists=Lists();var dirty=Document.HasChanges;var activity=Document.ReadActivityStatistics()!.Encode();
            var notifications=0;EventHandler handler=(_,_)=>notifications++;Document.Changed+=handler;
            try{action();}finally{Document.Changed-=handler;}
            var after=Document.GetHistory();Exact(bytes);
            Expect(Document.EditRevision==revision&&Document.EditorChangeCount==count&&notifications==0
                &&history.Undo.SequenceEqual(after.Undo)&&history.Redo.SequenceEqual(after.Redo)&&history.ChangeCount==after.ChangeCount
                &&dirty==Document.HasChanges&&lists==Lists()&&Document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),"Draft/cancel/refusal changed history, lists, counters or activity.");
        }
        public void Check(Action action,int occurrence,int value)
        {
            var before=Bytes();var expected=before.ToDictionary(row=>row.Key,row=>row.Value.ToArray());var area=Areas[occurrence];
            if(Project)BinaryPrimitives.WriteUInt32LittleEndian(expected[area.File].AsSpan(area.Offset+56),checked((uint)value));
            else BinaryPrimitives.WriteUInt16LittleEndian(expected[area.File].AsSpan(area.Offset+24),checked((ushort)value));
            var revision=Document.EditRevision;var count=Document.EditorChangeCount;var history=Document.GetHistory();
            var activity=Document.ReadActivityStatistics()!.Encode();var notifications=0;EventHandler handler=(_,_)=>notifications++;
            Document.Changed+=handler;try{action();}finally{Document.Changed-=handler;}
            Exact(expected);var after=Document.GetHistory();
            Expect(Document.EditRevision==revision+1&&Document.EditorChangeCount==unchecked(count+1)&&notifications==1
                &&after.Undo.Count==history.Undo.Count+1&&after.ChangeCount==history.ChangeCount+1
                &&Document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(activity),"Pause OK lost equal/changed one manager command/count/Undo.");
            Document.Undo();Exact(before);Expect(Document.EditorChangeCount==count,"Pause Undo lost counter.");
            Document.Redo();Exact(expected);Expect(Document.EditorChangeCount==unchecked(count+1),"Pause Redo lost counter.");
        }
        public void Dispose()=>Manager.Dispose();
    }
    public static void Run()
    {
        var numbers=new (string Text,int Value)[]{("",0),("0",0),("17",17),("250",250),("251",251),("-1",-1),
            ("+12",12),("  12x",12),("\t42 tail",42),("12.9",12),("12 34",12),("0x10",0),("x12",0),
            ("--1",0),("-",0),("+",0),("  ",0),("１２",0),("-0",0),("00017",17),
            ("2147483647",int.MaxValue),("-2147483648",int.MinValue),("2147483648",0),("-2147483649",0),
            ("999999999999999999999999999999999999",0),("65535",65535)};
        foreach(var row in numbers)Expect(EditorPatrolPauseSession.ParseSourceNumber(row.Text)==row.Value,"Signed decimal-prefix expectation differs: "+row.Text);
        Reject(()=>EditorPatrolPauseSession.ParseSourceNumber(null!));
        foreach(var project in new[]{false,true})
        {
            foreach(var row in numbers)
            {
                using var f=new Fixture(project);EditorPatrolPauseSession? session=null;
                f.ReadOnly(()=>{session=f.Select();Expect(session.InitialValue==17&&session.Text=="17"&&session.IsCurrent,"Initial source pause changed or lost authority.");session.SetText(row.Text);Expect(session.Text==row.Text&&session.Value==row.Value,"Literal draft was normalized or parsed differently.");});
                using(session!)
                {
                    if(row.Value is >=0 and <=250)
                    {
                        f.Check(()=>session!.Accept(),0,row.Value);
                        Expect(session!.IsAccepted&&session.IsClosed&&!session.IsCurrent,"Pause OK retained edit authority.");
                        f.ReadOnly(()=>{Reject(()=>session.Accept());Reject(()=>session.SetText("5"));});
                    }
                    else
                    {
                        f.ReadOnly(()=>{Expect(session!.Range==(row.Value<0?EditorPatrolPauseRange.BelowMinimum:EditorPatrolPauseRange.AboveMaximum),"Wrong range notice.");Reject(()=>session.Accept());Expect(session.IsCurrent&&!session.IsAccepted&&session.Text==row.Text,"Range error consumed or changed draft.");session.SetText("23");});
                        f.Check(()=>session!.Accept(),0,23);
                    }
                }
                using var cancelled=f.Select(1);f.ReadOnly(()=>{cancelled.SetText("250");cancelled.Dispose();cancelled.Dispose();Reject(()=>cancelled.Accept());Expect(!cancelled.IsAccepted&&!cancelled.IsCurrent,"Cancel retained authority.");});
            }
            using var imported=new Fixture(project,65535);using var importedSession=imported.Select();
            imported.ReadOnly(()=>{Expect(importedSession.Text=="65535"&&importedSession.Range==EditorPatrolPauseRange.AboveMaximum,"Imported out-of-range pause was clamped.");Reject(()=>importedSession.Accept());importedSession.SetText("250");});
            imported.Check(()=>importedSession.Accept(),0,250);
            if(project)
            {
                using var negative=new Fixture(true,uint.MaxValue);using var negativeSession=negative.Select();
                negative.ReadOnly(()=>{Expect(negativeSession.InitialValue==-1&&negativeSession.Text=="-1"&&negativeSession.Range==EditorPatrolPauseRange.BelowMinimum,"Source initial LONG was displayed unsigned.");Reject(()=>negativeSession.Accept());negativeSession.SetText("0");});
                negative.Check(()=>negativeSession.Accept(),0,0);
            }
            foreach(var change in new[]{"private","raw","revision","structure","reload","closed"})
            {
                using var stale=new Fixture(project);using var session=stale.Select();session.SetText("31");
                switch(change)
                {
                    case "private":stale.Manager.CreateEmptyRoute();break;
                    case "raw":stale.Raw(2,project?56:24,37);break;
                    case "revision":var area=stale.Areas[2];stale.Document.SetField(area,area.Fields.Single(field=>field.Name==(project?"waitdelay":"wAreaWaitDelay")),"41");break;
                    case "structure":stale.Document.AddMapRegion(MapRegionKind.Area,new(3,13,1,1));break;
                    case "reload":stale.Manager.Reload();break;
                    case "closed":stale.Manager.Dispose();break;
                }
                stale.ReadOnly(()=>{Expect(!session.IsCurrent,"Stale pause retained authority.");Reject(()=>session.SetText("250"));Reject(()=>session.Accept());Expect(!session.IsAccepted,"Stale pause was marked accepted.");});
            }
            using var guard=new Fixture(project);
            guard.ReadOnly(()=>{Reject(()=>guard.Select(-1));Reject(()=>guard.Select(3));});
            using var foreign=new EditorPatrolManagerSession(guard.Document);
            guard.ReadOnly(()=>Reject(()=>foreign.SelectPause(guard.Manager.GetRoutes()[0],0)));
            var missing=guard.Manager.GetRoutes()[0];guard.Raw(1,project?52:31,250);guard.Manager.Reload();
            guard.ReadOnly(()=>Reject(()=>guard.Manager.SelectPause(guard.Manager.GetRoutes()[0],2)));
            using var nested=new Fixture(project);using var nestedSession=nested.Select(1);nestedSession.SetText("21");
            EventHandler reenter=(_,_)=>{if(!nestedSession.IsApplying)return;Expect(!nestedSession.IsCurrent,"Applying pause was current.");Reject(()=>nestedSession.Accept());Reject(()=>nestedSession.SetText("250"));Reject(()=>nested.Select());};
            nested.Document.Changed+=reenter;try{nested.Check(()=>nestedSession.Accept(),1,21);}finally{nested.Document.Changed-=reenter;}
            using var observed=new Fixture(project);using var observedSession=observed.Select(1);observedSession.SetText("22");
            var revision=observed.Document.EditRevision;var count=observed.Document.EditorChangeCount;var history=observed.Document.GetHistory();
            EventHandler fail=(_,_)=>throw new InvalidOperationException("Intentional observer failure after pause commit.");observed.Document.Changed+=fail;
            try{Reject(()=>observedSession.Accept());}finally{observed.Document.Changed-=fail;}
            Expect(observedSession.IsAccepted&&observedSession.IsClosed&&observed.Document.EditRevision==revision+1
                &&observed.Document.EditorChangeCount==unchecked(count+1)&&observed.Document.GetHistory().Undo.Count==history.Undo.Count+1
                &&observed.Document.PatrolAreas()[1].WaitDelay==22,"Postcommit pause error lost edits or retained capability.");
            observed.ReadOnly(()=>Reject(()=>observedSession.Accept()));
            using var closed=new Fixture(project);using var closedSession=closed.Select();closedSession.SetText("24");
            EventHandler close=(_,_)=>closed.Manager.Dispose();closed.Document.Changed+=close;
            try{closedSession.Accept();}finally{closed.Document.Changed-=close;}
            Expect(closedSession.IsAccepted&&closedSession.IsClosed&&closed.Manager.IsClosed&&closed.Document.PatrolAreas()[0].WaitDelay==24,"Postcommit close resurrected manager or lost pause.");
            using var saved=new TemporaryFixture();var expected=observed.Document.PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay)).ToArray();
            var path=SaveProjectionChecks.SaveCopy(observed.Document,Path.Combine(saved.DirectoryPath,"working"));var reopened=NativeMapDocument.Load(path);
            Expect(expected.SequenceEqual(reopened.PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay))),"Save/reopen lost accepted pause.");
            if(project){var compiled=reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78",NativeMapDocument.Create(24,24,"PauseBasis"));Expect(expected.SequenceEqual(compiled.PatrolAreas().Select(point=>(point.ID,point.NextID,point.WaitDelay))),"Compilation lost accepted pause.");}
        }
    }
}
