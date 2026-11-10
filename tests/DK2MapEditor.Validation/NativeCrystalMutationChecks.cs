using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class NativeCrystalMutationChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static NativeMapDocument New()
    {
        var doc=NativeMapDocument.CreateNewMap(new("CrystalCommands","CrystalCommands",32,30,NativeFormat:true));
        doc.Paint(MapDrawing.Rectangle(new(1,1,30,28),true),new(3,2,1));return doc;
    }
    private static NativeRecord Object(NativeMapDocument doc,int x,int y,int id=28)
    {
        var row=doc.AddRecord(RecordTemplate.All.Single(item=>item.Structure=="GAMEOBJECT"),new(x,y));
        doc.SetField(row,row.Fields.Single(field=>field.Name=="bObjectID"),id.ToString());return row;
    }
    private static NativeRecord Action(NativeMapDocument doc,int type=2)
    {
        var action=doc.AddScenarioEvent(0xD6,new(0,0),LegacyEventSchemas.All.Single(item=>item.RecordSignature==0xD6&&item.Type==type));
        Expect(MemoryMarshal.TryGetArray(action.File.Data,out var buffer),"Crystal fixture buffer unavailable.");var bytes=buffer.Array??throw new InvalidOperationException("Missing fixture memory.");
        for(var i=8;i<12;i++)bytes[action.Offset+i]=(byte)(0xD0+i);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(action.Offset+12),25);BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(action.Offset+14),26);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(action.Offset+20),0xEFA5);bytes[action.Offset+23]=9;return action;
    }
    private static byte[] Bytes(NativeRecord row)=>row.File.Data.Slice(row.Offset,row.Length).ToArray();
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument doc)=>doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Crystal operation lost whole-file rollback/Undo.");
    private static void Target(NativeRecord action,byte[] before,int x,int y)
    {
        var expected=before.ToArray();BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(12),unchecked((ushort)x));BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(14),unchecked((ushort)y));
        Expect(Bytes(action).AsSpan().SequenceEqual(expected),"Crystal target used the wrong physical first gem or changed another action byte.");
    }
    public static void Run()
    {
        for(var route=0;route<13;route++)Commands(route);
        NoMutationAndRaw();AtomicityAndDuplicates();ProjectPolicy();
        Console.WriteLine("NATIVE CRYSTAL MUTATION DETAIL 13 field/add/delete/duplicate/manager/painting/cut/paste/transform routes; physical first-object order, sourceFFFF, four-byte-only writes, unchanged/no-op/raw Resize retention, preview/rollback/UndoRedo/save, zero history, duplicate event IDs and project reference policy");
    }
    private static void Commands(int route)
    {
        var doc=New();var first=Object(doc,4,5);var second=Object(doc,12,13);var extra=Object(doc,8,9,1);
        var action=Action(doc);var other=Action(doc,32);var a=Bytes(action);var b=Bytes(other);var before=Snapshot(doc);var counter=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;var changes=0;doc.Changed+=(_,_)=>changes++;
        var expected=new TilePoint(4,5);uint increments=1;
        switch(route)
        {
            case 0:doc.SetField(first,first.Fields.Single(field=>field.Name=="dwObjectLocationX"),"7");expected=new(7,5);break;
            case 1:doc.SetField(first,first.Fields.Single(field=>field.Name=="bObjectID"),"1");expected=new(12,13);break;
            case 2:doc.DeleteRecord(first);expected=new(12,13);break;
            case 3:doc.DuplicateRecord(second);break;
            case 4:
                var removal=doc.PlanObjectRemoval([first]);Exact(before);doc.ApplyEditorObjectRemoval(removal);expected=new(12,13);break;
            case 5:
                var modify=doc.PlanObjectBatchModification([first],doc.ObjectTemplateForModification(first),new(ObjectModificationMask.Coordinates,Position:new(6,7),Flags:(ObjectBatchFlags)127));Exact(before);
                doc.ApplyEditorObjectBatchModification(modify);expected=new(6,7);break;
            case 6:
                var multi=doc.PlanObjectBatchModification([first,extra],doc.ObjectTemplateForModification(first),new(ObjectModificationMask.None,Flags:(ObjectBatchFlags)127));Exact(before);
                doc.ApplyEditorObjectBatchModification(multi);expected=new(12,13);increments=2;break;
            case 7:
                var insertion=doc.PlanObjectBatchInsertion(new(PlacedThingKind.Object,28,2),new(Position:new(16,17),Flags:(ObjectBatchFlags)127));Exact(before);doc.ApplyEditorObjectBatchInsertion(insertion,[0]);break;
            case 8:
                var paint=doc.PlanObjectPainting(new(PlacedThingKind.Object,1,2),new(4,5));Expect(!paint.Locked,"Crystal painting fixture has incompatible terrain.");Exact(before);doc.ApplyEditorObjectPainting(paint);expected=new(12,13);break;
            case 9:doc.CutEditorSelection(new(4,5,1,1),new(3,2,1));expected=new(12,13);increments=2;break;
            case 10:
                var clipboard=doc.CopySelection(new(4,5,1,1));doc.PasteEditorSelection(new(4,5),clipboard);expected=new(12,13);increments=2;break;
            case 11:doc.TransformEditorSelection(new(3,4,4,4),rotate:true);expected=new(5,5);increments=2;break;
            case 12:doc.TransformEditorSelection(new(3,4,4,4),rotate:false,horizontal:true);expected=new(5,5);increments=2;break;
        }
        Target(action,a,expected.X,expected.Y);Target(other,b,25,26);
        Expect(doc.EditorChangeCount==unchecked(counter+increments)&&doc.GetHistory().Undo.Count==history+1&&changes==1,"Crystal projection added counts/history/notifications to its caller.");
        var after=Snapshot(doc);doc.Undo();Exact(before);Expect(doc.EditorChangeCount==counter,"Crystal Undo lost source counter.");doc.Redo();Exact(after);
        var saved=SaveProjectionChecks.SaveCopy(doc,Path.Combine(Path.GetTempPath(),"DK2CrystalCommands",Guid.NewGuid().ToString("N")));
        var loaded=NativeMapDocument.Load(saved);var row=loaded.Records().Single(item=>EntityReferences.EventType(item)?.Name=="ACTION_SHOWCRYSTAL");Target(row,a,expected.X,expected.Y);
    }
    private static void NoMutationAndRaw()
    {
        var doc=New();var first=Object(doc,4,5);Object(doc,12,13);var action=Action(doc);var initial=Bytes(action);
        doc.SetField(first,first.Fields.Single(field=>field.Name=="dwObjectLocationX"),"4");Target(action,initial,25,26);
        doc.Paint([new(20,20)],new(4,2,1));Target(action,initial,25,26);doc.SetField(first,first.Fields.Single(field=>field.Name=="dwObjectGoldValue"),"7");Target(action,initial,25,26);
        var preview=doc.PlanObjectBatchModification([first],doc.ObjectTemplateForModification(first),new(ObjectModificationMask.Coordinates,Position:new(6,7),Flags:(ObjectBatchFlags)127));_=preview;Target(action,initial,25,26);
        doc.Resize(36,34,2,2,new(3,2,1));Target(action,initial,25,26);doc.Undo();
        using(var session=doc.CreateMapPropertiesSession())doc.ApplyMapProperties(doc.PrepareMapProperties(session,new(36,34,8)));Target(action,initial,25,26);doc.Undo();
        doc.Resize(8,8,0,0,new(3,2,1),removeOutside:true);Target(action,initial,25,26);doc.Undo();
        // A subsequent real object command must not inherit raw suppression.
        first=doc.Records().First(item=>item.Category=="GAMEOBJECT");doc.SetField(first,first.Fields.Single(field=>field.Name=="dwObjectLocationY"),"8");Target(action,initial,4,8);
    }
    private static void AtomicityAndDuplicates()
    {
        var doc=New();var gem=Object(doc,4,5);var one=Action(doc);var two=Action(doc);var oneBefore=Bytes(one);var twoBefore=Bytes(two);
        Expect(MemoryMarshal.TryGetArray(two.File.Data,out var array),"Duplicate fixture buffer unavailable.");var buffer=array.Array??throw new InvalidOperationException("Missing duplicate buffer.");
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(two.Offset+16),ushort.Parse(one.Read(one.Fields.Single(field=>field.Name=="ID"))));twoBefore=Bytes(two);
        var before=Snapshot(doc);try{doc.Edit("failed object transaction",()=>{doc.DeleteRecord(gem);throw new InvalidOperationException("literal rollback");});}catch(InvalidOperationException error)when(error.Message=="literal rollback"){}Exact(before);
        doc.SetHistoryDepth(0);doc.DeleteRecord(gem);Target(one,oneBefore,65535,65535);Target(two,twoBefore,65535,65535);Expect(!doc.CanUndo,"Crystal projection created history at depth zero.");
        Object(doc,9,10);Target(one,oneBefore,9,10);Target(two,twoBefore,9,10);
        var newer=doc.Records().Single(item=>item.Category=="GAMEOBJECT");var original=Bytes(one);before=Snapshot(doc);
        try{doc.Edit("raw failing resize",()=>{doc.Resize(40,40,1,1,new(3,2,1));throw new InvalidOperationException("raw rollback");});}catch(InvalidOperationException error)when(error.Message=="raw rollback"){}Exact(before);
        doc.SetField(newer,newer.Fields.Single(field=>field.Name=="dwObjectLocationX"),"11");Target(one,original,11,10);
    }
    private static void ProjectPolicy()
    {
        var project=NativeMapDocument.CreateNewMap(new("CrystalProject","CrystalProject",32,30));var gem=project.AddLegacyRecord("Object",new(4,5));project.SetField(gem,gem.Fields.Single(field=>field.Name=="id"),"28");
        var action=project.AddScenarioEvent(0xD6,new(0,0),LegacyEventSchemas.All.Single(item=>item.Name=="ACTION_SHOWCRYSTAL"));var before=Bytes(action);
        project.SetField(gem,gem.Fields.Single(field=>field.Name=="x"),"9");Expect(Bytes(action).AsSpan().SequenceEqual(before),"Native projection rewrote DKM semantic operands.");
        var output=project.CompileLegacyProject("CrystalCompiled",NativeMapDocument.Create(32,30,"Basis"));var compiled=output.Records().Single(item=>EntityReferences.EventType(item)?.Name=="ACTION_SHOWCRYSTAL");
        Expect(BinaryPrimitives.ReadUInt16LittleEndian(compiled.File.Data.Span.Slice(compiled.Offset+12))==9&&BinaryPrimitives.ReadUInt16LittleEndian(compiled.File.Data.Span.Slice(compiled.Offset+14))==5,"Project compiler did not retain the original derived coordinate policy.");
    }
}
