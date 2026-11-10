using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class EditorPatrolCallerChecks
{
    private static void Expect(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Invalid patrol caller accepted.");}
    public static void Run()
    {
        foreach(var project in new[]{false,true})
        {
            var document=project?NativeMapDocument.CreateNewMap(new("Patrol","Patrol",16,16)):NativeMapDocument.Create(16,16,"Patrol");
            var areas=Enumerable.Range(0,3).Select(index=>document.AddMapRegion(MapRegionKind.Area,new(2+index*3,3,2,2))).ToArray();
            // Imported padding/labels are opaque to these callers.
            foreach(var area in areas)
            {
                Expect(MemoryMarshal.TryGetArray(area.File.Data,out var array),"Fixture memory unavailable.");
                array.Array![array.Offset+area.Offset+(project?80:27)]=0xE9;
                if(project)array.Array[array.Offset+area.Offset+105]=0xE8;
            }
            var next=project?"nextid":"bAreaNextID";var wait=project?"waitdelay":"wAreaWaitDelay";
            Dictionary<NativeFile,byte[]> Snapshot()=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            void Exact(Dictionary<NativeFile,byte[]> snapshot)=>Expect(snapshot.Count==document.Files.Count&&snapshot.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Patrol caller changed unrelated/opaque bytes.");
            void Field(int index,string name,int value)=>document.SetField(areas[index],areas[index].Fields.Single(field=>field.Name==name),value.ToString());
            void Check(Action action,params (int Area,string Field,int Value)[] changes)
            {
                var before=Snapshot();var expected=before.ToDictionary(row=>row.Key,row=>row.Value.ToArray());
                foreach(var change in changes)
                {var record=areas[change.Area];var field=record.Fields.Single(field=>field.Name==change.Field);field.Encode(change.Value.ToString()).CopyTo(expected[record.File],record.Offset+field.Offset);}
                var count=document.EditorChangeCount;var history=document.GetHistory();var revision=document.EditRevision;var stats=document.ReadActivityStatistics()!.Encode();
                var notifications=0;EventHandler handler=(_,_)=>notifications++;document.Changed+=handler;
                try{action();}finally{document.Changed-=handler;}
                Exact(expected);
                Expect(document.EditorChangeCount==unchecked(count+1)&&document.EditRevision==revision+1&&notifications==1
                    &&document.GetHistory().Undo.Count==history.Undo.Count+1&&document.GetHistory().ChangeCount==history.ChangeCount+1,"Accepted editor patrol did not create exactly one operation/increment/notification.");
                Expect(document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(stats),"Core patrol command altered unrelated activity data.");
                document.Undo();Exact(before);Expect(document.EditorChangeCount==count,"Patrol Undo lost source counter.");
                document.Redo();Exact(expected);Expect(document.EditorChangeCount==unchecked(count+1),"Patrol Redo lost source counter.");
                document.Undo();Exact(before);
            }
            Check(()=>document.SetEditorPatrolPause(areas[0],0));
            Check(()=>document.SetEditorPatrolPause(areas[0],17),(0,wait,17));
            Check(()=>document.StartEditorPatrol(areas[0],0));
            Check(()=>document.StartEditorPatrol(areas[0],23),(0,wait,23));
            Check(()=>document.LinkEditorPatrolAreas(areas[0],areas[1]),(0,next,2));
            Check(()=>document.LinkEditorPatrolAreas(areas[0],areas[0]),(0,next,1));
            Check(()=>document.AppendEditorPatrolPoint(document.PatrolRouteFrom(areas[0]),areas[1],7),(0,next,2),(1,wait,7));
            Check(()=>document.InsertEditorPatrolPoint(document.PatrolRouteFrom(areas[0]),0,areas[2],11),(2,next,1),(2,wait,11));
            Check(()=>document.RemoveEditorPatrolOccurrence(document.PatrolRouteFrom(areas[0]),0));
            Check(()=>document.ClearEditorPatrolRoute(document.PatrolRouteFrom(areas[0])));
            Field(0,next,2);Field(1,next,3);Field(2,next,2);
            Check(()=>document.RemoveEditorPatrolOccurrence(document.PatrolRouteFrom(areas[0]),3),(2,next,0));
            Check(()=>document.RemoveEditorPatrolOccurrence(document.PatrolRouteFrom(areas[0]),1),(0,next,3),(1,next,0));
            Check(()=>document.ClearEditorPatrolRoute(document.PatrolRouteFrom(areas[0])),(0,next,0),(1,next,0),(2,next,0));
            var stable=Snapshot();var savedCount=document.EditorChangeCount;var savedRevision=document.EditRevision;var savedHistory=document.GetHistory();
            void Untouched()=>Expect(document.EditorChangeCount==savedCount&&document.EditRevision==savedRevision
                &&document.GetHistory().Undo.Count==savedHistory.Undo.Count&&document.GetHistory().Redo.Count==savedHistory.Redo.Count,"Rejected caller changed counters/history.");
            foreach(var invalid in new[]{-1,251}){Reject(()=>document.SetEditorPatrolPause(areas[0],invalid));Reject(()=>document.AppendEditorPatrolPoint(document.PatrolRouteFrom(areas[0]),areas[1],invalid));Exact(stable);Untouched();}
            Reject(()=>document.StartEditorPatrol(areas[0],0));Reject(()=>document.LinkEditorPatrolAreas(areas[0],areas[2]));
            Reject(()=>document.InsertEditorPatrolPoint(document.PatrolRouteFrom(areas[0]),-1,areas[2],0));
            Reject(()=>document.RemoveEditorPatrolOccurrence(document.PatrolRouteFrom(areas[0]),99));
            var foreign=NativeMapDocument.Create(16,16,"Foreign").AddMapRegion(MapRegionKind.Area,new(2,2,1,1));
            Reject(()=>document.SetEditorPatrolPause(foreign,0));Reject(()=>document.AppendEditorPatrolPoint(document.PatrolRouteFrom(areas[0]),foreign,0));
            Reject(()=>document.Edit("Nested caller refused",()=>document.SetEditorPatrolPause(areas[0],0)));Exact(stable);Untouched();
            var stale=document.PatrolRouteFrom(areas[0]);Field(0,next,0);var changed=Snapshot();
            Reject(()=>document.ClearEditorPatrolRoute(stale));Exact(changed);document.Undo();Exact(stable);
            // Generic no-op semantics remain: raw equal pause retains redo.
            document.SetPatrolPause(areas[0],12);document.Undo();Expect(document.CanRedo,"Fixture lacks redo.");
            var rawCount=document.EditorChangeCount;var rawHistory=document.GetHistory();document.SetPatrolPause(areas[0],0);
            Expect(document.CanRedo&&document.EditorChangeCount==rawCount&&document.GetHistory().Undo.Count==rawHistory.Undo.Count,"Raw no-op pause semantics changed.");
            document.SetEditorPatrolPause(areas[0],0);Expect(!document.CanRedo&&document.EditorChangeCount==unchecked(rawCount+1),"Accepted equal editor pause did not consume redo/increment.");document.Undo();Exact(stable);
            document.SetHistoryDepth(0);var noHistory=Snapshot();var noHistoryCount=document.EditorChangeCount;
            document.SetEditorPatrolPause(areas[0],0);Exact(noHistory);Expect(!document.CanUndo&&!document.CanRedo&&document.EditorChangeCount==unchecked(noHistoryCount+1),"History-disabled accepted pause lost its source counter.");
        }
    }
}
