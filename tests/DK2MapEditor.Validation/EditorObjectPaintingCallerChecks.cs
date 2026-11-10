using System.Buffers.Binary;
using System.Reflection;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class EditorObjectPaintingCallerChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error) when(error is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new InvalidOperationException("Refused editor object painting accepted.");}
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument doc)=>doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> state)=>Expect(state.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Editor object painting full buffer snapshot differs.");
    private static NativeMapDocument New(bool native)
    {var doc=NativeMapDocument.CreateNewMap(new("ObjectCaller","ObjectCaller",12,12,NativeFormat:native));doc.Paint([new(2,2),new(3,3)],new(3,2,1));doc.SetHistoryDepth(65535);return doc;}
    private static PlacedThingKind Opposite(PlacedThingKind kind)=>kind switch
    {PlacedThingKind.Door=>PlacedThingKind.Trap,PlacedThingKind.Trap=>PlacedThingKind.Door,PlacedThingKind.Object=>PlacedThingKind.SpecialItem,_=>PlacedThingKind.Object};
    private static ObjectBatchTemplate Template(PlacedThingKind kind)=>ObjectBatchTemplate.Default(kind) with{Owner=2};
    private static void Seed(NativeMapDocument doc,PlacedThingKind kind,TilePoint position)
    {
        if(doc.ObjectManagerEntries().FirstOrDefault(entry=>entry.Thing.Kind==kind&&entry.Thing.Position==position) is { } existing)
            doc.DuplicateRecord(existing.Record);
        else doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(Template(kind),new(Position:position,Flags:(ObjectBatchFlags)127)),[0]);
    }
    private static void Counter(NativeMapDocument doc,uint value)=>typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(doc,value);

    public static void Run()
    {
        using var source=JsonDocument.Parse(typeof(EditorObjectPaintingCallerChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.EditorTransformCallersSource.json")!);
        Expect(source.RootElement.GetProperty("ObjectPaintAcceptedIncrements").GetInt32()==1
            &&source.RootElement.GetProperty("ObjectPaintEachRemovalIncrements").GetInt32()==1
            &&source.RootElement.GetProperty("ObjectPaintUsageIncrements").GetInt32()==1
            &&source.RootElement.GetProperty("ObjectPaintEqualUpdateAccepted").GetBoolean(),"Source composed Things_Paint contract differs.");
        var cases=0;
        foreach(var native in new[]{false,true})
        foreach(var kind in Enum.GetValues<PlacedThingKind>())
        {
            foreach(var removed in kind==PlacedThingKind.Creature?new[]{0}:new[]{0,1,3,5})
            {
                var doc=New(native);
                for(var i=0;i<removed;i++)Seed(doc,Opposite(kind),new(2,2));
                Seed(doc,PlacedThingKind.Creature,new(3,3));
                var plan=doc.PlanObjectPainting(Template(kind),new(2,2));
                Expect(!plan.Locked&&plan.Creates&&plan.Removals.Count==removed,$"Source creation/removal fixture invalid: native={native}, kind={kind}, expected={removed}, actual={plan.Removals.Count}, locked={plan.Locked}.");
                Accepted(doc,plan,1u+(uint)removed,false);
                Expect(doc.ObjectManagerEntries().Count==2,"Editor creation did not remove every opposite or changed an unrelated object.");
                cases++;
            }
            if(kind==PlacedThingKind.Creature)continue;
            foreach(var equal in new[]{false,true})
            {
                var doc=New(native);Seed(doc,kind,new(2,2));
                var template=doc.ObjectTemplateForModification(doc.ObjectManagerEntries().Single().Record);
                // Door repaint toggles the lock even with equal template values.
                if(!equal)template=template with{TypeID=template.TypeID+1};
                var plan=doc.PlanObjectPainting(template,new(2,2));
                Expect(!plan.Locked&&plan.Updating is not null&&plan.Removals.Count==0,"Source update fixture invalid.");
                Accepted(doc,plan,1,equal&&kind!=PlacedThingKind.Door);cases++;
            }
            {
                var doc=New(native);
                for(var i=0;i<3;i++)Seed(doc,Opposite(kind),new(2,2));
                var structure=kind switch{PlacedThingKind.Door=>"GAMEDOOR",PlacedThingKind.Trap=>"GAMETRAP",_=>"GAMEOBJECT"};
                var added=native?doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure==structure),new(2,2),2)
                    :doc.AddLegacyRecord(kind==PlacedThingKind.SpecialItem?"MagicalObject":kind.ToString(),new(2,2),2);
                doc.SetField(added,ValueCatalogues.PrimaryField(added)!,Template(kind).TypeID.ToString());
                var existing=doc.ObjectManagerEntries().Single(entry=>entry.Thing.Kind==kind);
                var plan=doc.PlanObjectPainting(doc.ObjectTemplateForModification(existing.Record),new(2,2));
                Expect(plan.Updating is not null&&plan.Removals.Count==0,"Same-family source update incorrectly selected opposite removals.");
                Accepted(doc,plan,1,kind!=PlacedThingKind.Door);
                Expect(doc.ObjectManagerEntries().Count==4,"Same-family source update removed coexistence objects.");cases++;
            }
        }
        Expect(cases==58,"Source object painting combination coverage differs.");
        foreach(var native in new[]{false,true})Guards(native);
        Console.WriteLine("OBJECT PAINT CALLER DETAIL 58 native/project creation/update/equal/coexistence cases; five families, removal0/1/3/5, literal accepted1+N, one paint usage/Undo, full bytes/count UndoRedo, raw no-op unchanged, stale/foreign/locked/nested/wrap/history0 guards");
    }

    private static void Accepted(NativeMapDocument doc,ObjectPaintingPlan plan,uint increments,bool equal)
    {
        var before=Snapshot(doc);var count=doc.EditorChangeCount;var undo=doc.GetHistory().Undo.Count;
        var activity=doc.ReadActivityStatistics()!.Encode();var expectedActivity=activity.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(expectedActivity.AsSpan(28),unchecked(BinaryPrimitives.ReadUInt32LittleEndian(activity.AsSpan(28))+1));
        var notifications=0;EventHandler observe=(_,_)=>notifications++;doc.Changed+=observe;
        Expect(doc.ApplyEditorObjectPainting(plan)==1,"Accepted source paint must report one operation even when equal.");doc.Changed-=observe;
        var expected=unchecked(count+increments);
        Expect(doc.EditorChangeCount==expected&&doc.GetHistory().Undo.Count==undo+1&&notifications==1&&!doc.CanRedo,"Source object caller count/history/notification differs.");
        Expect(doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(expectedActivity),"Source painting increments must include one usage only.");
        if(equal)Exact(before);
        var after=Snapshot(doc);doc.Undo();Exact(before);Expect(doc.EditorChangeCount==count,"Object paint Undo lost count.");
        Expect(doc.ReadActivityStatistics()!.UsageOfPaintThings==BinaryPrimitives.ReadUInt32LittleEndian(expectedActivity.AsSpan(28)),"Undo rolled back cumulative source paint usage.");
        doc.Redo();Exact(after);Expect(doc.EditorChangeCount==expected,"Object paint Redo lost source count.");
    }

    private static void Guards(bool native)
    {
        var doc=New(native);Seed(doc,PlacedThingKind.Trap,new(2,2));Seed(doc,PlacedThingKind.Creature,new(3,3));doc.Undo();
        var plan=doc.PlanObjectPainting(Template(PlacedThingKind.Trap),new(2,2));
        var before=Snapshot(doc);var count=doc.EditorChangeCount;var undo=doc.GetHistory().Undo.Count;var usage=doc.ReadActivityStatistics()!.Encode();
        Expect(doc.ApplyObjectPainting(plan)==0&&doc.CanRedo&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==undo,"Raw equal paint acquired editor acceptance.");Exact(before);
        var foreign=New(native).PlanObjectPainting(Template(PlacedThingKind.Trap),new(2,2));
        Reject(()=>doc.ApplyEditorObjectPainting(foreign));
        var locked=doc.PlanObjectPainting(Template(PlacedThingKind.Trap),new(0,2));Reject(()=>doc.ApplyEditorObjectPainting(locked));
        doc.Edit("Nested caller",()=>Reject(()=>doc.ApplyEditorObjectPainting(plan)));
        Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==undo&&doc.CanRedo&&doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(usage),"Refused caller leaked count/history/activity.");Exact(before);
        Accepted(doc,plan,1,true);
        var stale=doc.PlanObjectPainting(Template(PlacedThingKind.Trap),new(2,2));doc.Paint([new(4,4)],new(3,2,1));
        before=Snapshot(doc);count=doc.EditorChangeCount;undo=doc.GetHistory().Undo.Count;usage=doc.ReadActivityStatistics()!.Encode();
        Reject(()=>doc.ApplyEditorObjectPainting(stale));Exact(before);
        Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==undo&&doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(usage),"Stale paint leaked state.");
        var wrap=New(native);for(var i=0;i<5;i++)Seed(wrap,PlacedThingKind.Door,new(2,2));Counter(wrap,uint.MaxValue-3);
        Accepted(wrap,wrap.PlanObjectPainting(Template(PlacedThingKind.Trap),new(2,2)),6,false);
        Expect(wrap.EditorChangeCount==2,"Composed source paint count must wrap at 32 bits.");
        wrap.SetHistoryDepth(0);var current=wrap.EditorChangeCount;var paintUsage=wrap.ReadActivityStatistics()!.UsageOfPaintThings;
        wrap.ApplyEditorObjectPainting(wrap.PlanObjectPainting(Template(PlacedThingKind.Trap),new(2,2)));
        Expect(wrap.EditorChangeCount==unchecked(current+1)&&!wrap.CanUndo&&wrap.GetHistory().SnapshotBytes==0
            &&wrap.ReadActivityStatistics()!.UsageOfPaintThings==unchecked(paintUsage+1),"Zero-history equal paint lost accepted counter/usage or retained snapshots.");
    }
}
