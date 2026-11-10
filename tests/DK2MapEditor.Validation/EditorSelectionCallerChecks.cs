using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class EditorSelectionCallerChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error) when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Refused editor selection was accepted.");}
    private static void Counter(NativeMapDocument doc,uint value)=>typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(doc,value);
    private static int ObjectCount(NativeMapDocument doc)=>doc.CopySelection(new(0,0,doc.Width,doc.Height)).ObjectCount;
    public static void Run()
    {
        using var capture=JsonDocument.Parse(typeof(EditorSelectionCallerChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.EditorSelectionCallersSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var hash in capture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,hash.Name)))).Equals(hash.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Selection caller source changed.");
        Expect(capture.RootElement.GetProperty("AcceptanceIncrements").GetInt32()==1&&capture.RootElement.GetProperty("EachRemovalIncrements").GetInt32()==1
            &&!capture.RootElement.GetProperty("EachPasteInsertionIncrements").GetBoolean(),"Original cut/paste increment recipe differs.");
        foreach(var native in new[]{false,true})
        foreach(var paste in new[]{false,true})
        foreach(var removed in new[]{0,1,3,5})
        foreach(var blocked in new[]{false,true})
        {
            var doc=NativeMapDocument.CreateNewMap(new("SelectionCounter","SelectionCounter",12,12,NativeFormat:native));doc.SetHistoryDepth(65535);
            var point=new TilePoint(blocked?0:3,3);var rectangle=new TileRect(point.X,point.Y,2,2);
            NativeRecord Add(int index,TilePoint location)
            {
                var kind=new[]{"Creature","Door","Trap","Object","MagicalObject"}[index%5];
                var structure=kind switch{"Creature"=>"GAMEKEEPER","Door"=>"GAMEDOOR","Trap"=>"GAMETRAP",_=>"GAMEOBJECT"};
                var record=native?doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure==structure),location):doc.AddLegacyRecord(kind,location);
                var name=native?kind switch{"Creature"=>"bGlobalCreatureID","Door"=>"bDoorID","Trap"=>"bTrapID",_=>"bObjectID"}:"id";
                doc.SetField(record,record.Fields.Single(field=>field.Name==name),(kind=="MagicalObject"?117:index+1).ToString());return record;
            }
            for(var index=0;index<removed;index++)Add(index,point);
            var selection=doc.CopySelection(new(7,7,2,2),protectBoundary:true); // equal empty source terrain
            var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var usage=doc.ReadActivityStatistics()!.Encode();
            var baseline=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;var notifications=0;
            EventHandler observer=(_,_)=>notifications++;doc.Changed+=observer;
            if(paste)doc.PasteEditorSelection(point,selection);else doc.CutEditorSelection(rectangle,new(2,2,1));
            var expected=unchecked(baseline+1+(uint)(blocked?0:removed));
            var after=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            Expect(doc.EditorChangeCount==expected&&doc.GetHistory().Undo.Count==history+1&&notifications==1,"Accepted selection did not count once+removals with one Undo/notification.");
            Expect(doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(usage),"Cut/paste invented paint/click activity counts.");
            Expect(ObjectCount(doc)==(blocked?removed:0),"Selection removed protected objects or retained editable removals.");
            doc.Changed-=observer;doc.Undo();Expect(doc.EditorChangeCount==baseline&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Selection Undo lost source count or full bytes.");
            doc.Redo();Expect(doc.EditorChangeCount==expected&&after.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Selection Redo resampled or lost final count/bytes.");
        }
        foreach(var native in new[]{false,true})
        {
            var doc=NativeMapDocument.CreateNewMap(new("SelectionNoops","SelectionNoops",12,12,NativeFormat:native));
            var clipboard=doc.CopySelection(new(2,2,2,2));var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
            doc.CutSelection(new(2,2,2,2),new(2,2,1),respectEditorRules:true);doc.PasteSelection(new(3,3),clipboard,clipToMap:true,respectEditorRules:true);
            Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Generic raw equal selection acquired editor acceptance semantics.");
            Reject(()=>doc.CutEditorSelection(new(-1,2,2,2),new(2,2,1)));Reject(()=>doc.PasteEditorSelection(new(-1,2),clipboard));
            Reject(()=>doc.PasteEditorSelection(new(12,2),clipboard));
            var foreign=NativeMapDocument.CreateNewMap(new("WrongFormat","WrongFormat",12,12,NativeFormat:!native)).CopySelection(new(2,2,2,2));
            Reject(()=>doc.PasteEditorSelection(new(3,3),foreign));
            doc.Edit("Nested selection",()=>{Reject(()=>doc.CutEditorSelection(new(2,2,2,2),new(2,2,1)));Reject(()=>doc.PasteEditorSelection(new(3,3),clipboard));});
            Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Invalid/mismatched/nested selection leaked data/count/history.");
            Counter(doc,uint.MaxValue);doc.PasteEditorSelection(new(3,3),clipboard);
            Expect(doc.EditorChangeCount==0,"Source32bit accepted counter did not wrap.");doc.Undo();Expect(doc.EditorChangeCount==uint.MaxValue,"Wrapped selection Undo lost counter.");
            doc.SetHistoryDepth(0);doc.CutEditorSelection(new(2,2,2,2),new(2,2,1));
            Expect(doc.EditorChangeCount==0&&!doc.CanUndo&&doc.GetHistory().SnapshotBytes==0,"No-history accepted equal cut lost source count or retained bytes.");
            var source=NativeMapDocument.CreateNewMap(new("InsertionOnly","InsertionOnly",12,12,NativeFormat:native));
            var creature=native?source.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEKEEPER"),new(2,2)):source.AddLegacyRecord("Creature",new(2,2));
            var selected=source.CopySelection(new(2,2,2,2));var target=NativeMapDocument.CreateNewMap(new("TargetCounter","TargetCounter",12,12,NativeFormat:native));
            var targetBefore=target.EditorChangeCount;target.PasteEditorSelection(new(10,10),selected);
            Expect(target.EditorChangeCount==unchecked(targetBefore+1)&&ObjectCount(target)==1,"Clipped inserted thing added a source removal count.");
            Console.WriteLine($"SELECTION CALLER DETAIL native={native} cut/paste x removal0/1/3/5 x protected/editable; five source categories, equal acceptance, one Undo/count notification, unchanged activity/raw APIs, wrap, invalid/nested/format guards and clipped insertion-only count");
        }
    }
}
