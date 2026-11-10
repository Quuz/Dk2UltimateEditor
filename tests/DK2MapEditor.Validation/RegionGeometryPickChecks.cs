using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class RegionGeometryPickChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Invalid region point-choice action accepted.");}
    public static void Run()
    {
        foreach(var project in new[]{false,true})foreach(var kind in Enum.GetValues<MapRegionKind>())
        {
            var document=project?NativeMapDocument.CreateNewMap(new("Pick","Pick",16,16)):NativeMapDocument.Create(16,16,"Pick");
            var region=document.AddMapRegion(kind,new(2,3,kind==MapRegionKind.Gate?1:3,kind==MapRegionKind.Gate?1:4));
            using var session=document.CreateRegionPropertiesSession(region);var original=session.Values;
            var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var revision=document.EditRevision;
            var count=document.EditorChangeCount;var history=document.GetHistory();var dirty=document.HasChanges;
            var stats=document.ReadActivityStatistics()!.Encode();var notifications=0;
            EventHandler handler=(_,_)=>notifications++;document.Changed+=handler;
            try
            {
                void Untouched()=>Expect(notifications==0&&document.EditRevision==revision&&document.EditorChangeCount==count
                    &&document.HasChanges==dirty&&document.GetHistory().Undo.Count==history.Undo.Count&&document.GetHistory().Redo.Count==history.Redo.Count
                    &&document.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(stats)
                    &&bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Geometry choice changed live bytes/history/activity.");
                var beforePlan=document.PrepareRegionProperties(session);var pick=document.BeginRegionPropertiesPick(session);
                Expect(pick.IsPoint==(kind==MapRegionKind.Gate)&&document.IsRegionPropertiesPickCurrent(pick),"Source point/rectangle mode differs.");
                Reject(()=>document.BeginRegionPropertiesPick(session));Reject(()=>document.PrepareRegionProperties(session));Reject(()=>document.ApplyEditorRegionProperties(beforePlan));
                Untouched();document.CancelRegionPropertiesPick(pick);document.CancelRegionPropertiesPick(pick);
                Expect(!document.IsRegionPropertiesPickCurrent(pick)&&session.Values==original,"Cancel lost input or left a live choice.");
                Reject(()=>document.CompleteRegionPropertiesPick(pick,new(4,5,1,1)));Untouched();

                pick=document.BeginRegionPropertiesPick(session);
                Reject(()=>document.CompleteRegionPropertiesPick(pick,new(-1,0,1,1)));
                if(kind==MapRegionKind.Gate)Reject(()=>document.CompleteRegionPropertiesPick(pick,new(4,5,2,3)));
                Expect(document.IsRegionPropertiesPickCurrent(pick),"Rejected bounds consumed the current choice.");
                var chosen=new TileRect(4,5,kind==MapRegionKind.Gate?1:3,kind==MapRegionKind.Gate?1:4);
                document.CompleteRegionPropertiesPick(pick,chosen);Untouched();
                Expect(session.Values==original with{Bounds=chosen}&&!document.IsRegionPropertiesPickCurrent(pick),"Chosen geometry changed other private values or left a live ticket.");
                Reject(()=>document.CompleteRegionPropertiesPick(pick,chosen));Reject(()=>document.ApplyEditorRegionProperties(beforePlan));
                document.ApplyEditorRegionProperties(document.PrepareRegionProperties(session));
                Expect(document.GetMapRegion(region)!.Bounds==chosen&&notifications==1&&document.EditorChangeCount==unchecked(count+1),"Outer Apply failed to publish one chosen region operation.");
                document.Undo();Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&document.EditorChangeCount==count,"Undo lost original geometry/counter.");
            }
            finally{document.Changed-=handler;}

            using(var changing=document.CreateRegionPropertiesSession(region))
            {
                var ticket=document.BeginRegionPropertiesPick(changing);document.UpdateRegionProperties(changing,changing.Values);
                Reject(()=>document.CompleteRegionPropertiesPick(ticket,new(4,5,1,1)));document.CancelRegionPropertiesPick(ticket);
            }
            using(var stale=document.CreateRegionPropertiesSession(region))
            {
                var ticket=document.BeginRegionPropertiesPick(stale);document.Paint([new(8,8)],new(4,2,1));
                Reject(()=>document.CompleteRegionPropertiesPick(ticket,new(4,5,1,1)));document.Undo();
                Reject(()=>document.CompleteRegionPropertiesPick(ticket,new(4,5,1,1)));document.CancelRegionPropertiesPick(ticket);
            }
            using(var raw=document.CreateRegionPropertiesSession(region))
            {
                var ticket=document.BeginRegionPropertiesPick(raw);Expect(MemoryMarshal.TryGetArray(region.File.Data,out var array),"Fixture data missing.");
                var at=array.Offset+region.Offset+(project?80:kind==MapRegionKind.Area?27:kind==MapRegionKind.Gate?16:38);
                var old=array.Array![at];array.Array[at]^=1;
                Reject(()=>document.CompleteRegionPropertiesPick(ticket,new(4,5,1,1)));array.Array[at]=old;document.CancelRegionPropertiesPick(ticket);
            }
            using(var foreign=document.CreateRegionPropertiesSession(region))
            {
                var ticket=document.BeginRegionPropertiesPick(foreign);var other=NativeMapDocument.Create(16,16,"Other");
                Reject(()=>other.CompleteRegionPropertiesPick(ticket,new(4,5,1,1)));Reject(()=>other.CancelRegionPropertiesPick(ticket));
                foreign.Dispose();Reject(()=>document.CompleteRegionPropertiesPick(ticket,new(4,5,1,1)));document.CancelRegionPropertiesPick(ticket);
            }
        }
    }
}
