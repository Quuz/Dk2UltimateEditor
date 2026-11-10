using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class NativeGateMutationChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static byte[] Buffer(NativeRecord row)
    {Expect(MemoryMarshal.TryGetArray(row.File.Data,out var bytes),"Gate fixture has no buffer.");return bytes.Array??throw new InvalidOperationException("Gate fixture buffer absent.");}
    private static NativeMapDocument New()=>NativeMapDocument.CreateNewMap(new("GateMutation","GateMutation",32,30,NativeFormat:true));
    private static NativeRecord Gate(NativeMapDocument doc,int x,int y,int kind=1,bool heart=false)
    {
        var gate=doc.AddRecord(RecordTemplate.All.Single(item=>item.Structure=="GAMEGATE"),new(x,y));
        doc.SetField(gate,gate.Fields.Single(field=>field.Name=="bGateID"),kind.ToString());
        doc.SetField(gate,gate.Fields.Single(field=>field.Name=="bGateType"),heart?"14":"33");return gate;
    }
    private static NativeRecord Action(NativeMapDocument doc,int type,int x,int y)
    {
        var row=doc.AddScenarioEvent(0xD6,new(0,0),LegacyEventSchemas.All.Single(item=>item.RecordSignature==0xD6&&item.Type==type));
        var bytes=Buffer(row);for(var i=8;i<12;i++)bytes[row.Offset+i]=(byte)(0xB1+i);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(row.Offset+12),(ushort)x);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(row.Offset+14),(ushort)y);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(row.Offset+20),0xCDEF);bytes[row.Offset+23]=7;return row;
    }
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument doc)=>doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Expect(bytes.All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Gate command lost complete buffers.");
    private static byte[] Bytes(NativeRecord row)=>row.File.Data.Slice(row.Offset,row.Length).ToArray();
    private static void Coordinates(NativeRecord row,byte[] before,int x,int y)
    {
        var expected=before.ToArray();BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(12),unchecked((ushort)x));BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(14),unchecked((ushort)y));
        Expect(Bytes(row).AsSpan().SequenceEqual(expected),"Gate target changed an unrelated byte or used the wrong first physical gate.");
    }
    public static void Run()
    {
        using var captured=JsonDocument.Parse(typeof(NativeGateMutationChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.NativeGateMutationSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var input in captured.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Gate mutation source changed.");
        foreach(var route in Enumerable.Range(0,3))foreach(var mode in Enumerable.Range(0,4))Move(route,mode);
        RemovalAndType();GenericCrop();Generation();
        Console.WriteLine("NATIVE GATE MUTATION DETAIL 11 geometry/field/private-parent cases; first physical duplicates and kind/type catalogue, intermediate collision, deletion fallback, heart exclusion, exact action bytes, atomic count/Undo/Redo/save, imported/generated child targets and generic crop retention");
    }
    private static void Move(int route,int mode)
    {
        var doc=New();var first=Gate(doc,3,5,77,heart:mode==3);var gate=first;
        if(mode is 1 or 2){var second=Gate(doc,3,5,0);if(mode==2)gate=second;}
        Gate(doc,9,5,99); // x-first intermediate position must not capture this gate's actions.
        var action=Action(doc,32,4,6);var untouched=Action(doc,32,10,6);var crystal=Action(doc,2,4,6);
        var a=Bytes(action);var b=Bytes(untouched);var c=Bytes(crystal);var before=Snapshot(doc);var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;var notifications=0;
        doc.Changed+=(_,_)=>notifications++;
        var x=route==2?11:9;const int y=11;
        if(route==0)doc.SetMapRegionBounds(gate,new(x,y,1,1));
        if(route==1)
        {
            if(mode==3)return; // Heart geometry is outside the region-property workflow.
            using(var cancel=doc.CreateRegionPropertiesSession(gate)){doc.UpdateRegionProperties(cancel,cancel.Values with{Bounds=new(x,y,1,1)});_=doc.PrepareRegionProperties(cancel);Exact(before);}
            using var session=doc.CreateRegionPropertiesSession(gate);doc.UpdateRegionProperties(session,session.Values with{Bounds=new(x,y,1,1)});
            var plan=doc.PrepareRegionProperties(session);Exact(before);Expect(plan.Changed,"Gate move was not prepared privately.");doc.ApplyEditorRegionProperties(plan);
        }
        if(route==2)doc.Edit("Move gate fields",()=>
        {
            doc.SetField(gate,gate.Fields.Single(field=>field.Name=="dwGateLocationX"),x.ToString());
            doc.SetField(gate,gate.Fields.Single(field=>field.Name=="dwGateLocationY"),y.ToString());
        });
        var follows=mode is 0 or 1;
        Coordinates(action,a,follows?x+1:4,follows?y+1:6);Coordinates(untouched,b,10,6);Coordinates(crystal,c,4,6);
        Expect(doc.EditorChangeCount==unchecked(count+1)&&doc.GetHistory().Undo.Count==history+1&&notifications==1,"Derived gate writes added a counter/history/notification.");
        var after=Snapshot(doc);doc.Undo();Exact(before);Expect(doc.EditorChangeCount==count,"Gate Undo lost count.");doc.Redo();Exact(after);
        var path=SaveProjectionChecks.SaveCopy(doc,Path.Combine(Path.GetTempPath(),"DK2GateMutation",Guid.NewGuid().ToString("N")));
        var opened=NativeMapDocument.Load(path);var saved=opened.Records().Single(row=>row.File.Signature==0xD2&&row.Read(row.Fields.Single(field=>field.Name=="ID"))==action.Read(action.Fields.Single(field=>field.Name=="ID")));
        Coordinates(saved,a,follows?x+1:4,follows?y+1:6);
        doc.Undo();Exact(before);
    }
    private static void RemovalAndType()
    {
        foreach(var mode in Enumerable.Range(0,5))
        {
            var doc=New();var gate=Gate(doc,3,5,77,heart:mode==3);if(mode is 0 or 1){var second=Gate(doc,3,5,0);if(mode==1)gate=second;}
            var target=Action(doc,32,4,6);var other=Action(doc,32,17,18);var a=Bytes(target);var b=Bytes(other);var before=Snapshot(doc);var count=doc.EditorChangeCount;
            if(mode==4)doc.SetField(gate,gate.Fields.Single(field=>field.Name=="bGateType"),"14");else doc.DeleteRecord(gate);
            Coordinates(target,a,mode is 0 or 2 or 4?1:4,mode is 0 or 2 or 4?1:6);Coordinates(other,b,17,18);
            Expect(doc.EditorChangeCount==unchecked(count+1),"Gate removal added derived counter increments.");var after=Snapshot(doc);doc.Undo();Exact(before);doc.Redo();Exact(after);
        }
        var same=New();var g=Gate(same,3,5,0);var action=Action(same,32,4,6);var original=Snapshot(same);
        using(var equal=same.CreateRegionPropertiesSession(g))same.ApplyEditorRegionProperties(same.PrepareRegionProperties(equal));Exact(original);same.Undo();Exact(original);
        same.SetHistoryDepth(0);same.SetMapRegionBounds(g,new(7,8,1,1));Coordinates(action,BytesFrom(original,action),8,9);Expect(!same.CanUndo,"Zero history acquired a gate Undo.");
        same=New();g=Gate(same,3,5);var one=Action(same,32,4,6);var two=Action(same,32,4,6);
        BinaryPrimitives.WriteUInt16LittleEndian(Buffer(two).AsSpan(two.Offset+16),ushort.Parse(one.Read(one.Fields.Single(field=>field.Name=="ID"))));
        var first=Bytes(one);var secondBytes=Bytes(two);same.SetMapRegionBounds(g,new(7,8,1,1));Coordinates(one,first,8,9);Coordinates(two,secondBytes,8,9);
    }
    private static byte[] BytesFrom(Dictionary<NativeFile,byte[]> snapshot,NativeRecord row)=>snapshot[row.File].AsSpan(row.Offset,row.Length).ToArray();
    private static void GenericCrop()
    {
        var doc=New();Gate(doc,20,20);var action=Action(doc,32,21,21);var bytes=Bytes(action);doc.Resize(12,12,0,0,new(2,2,1),removeOutside:true);Coordinates(action,bytes,21,21);
    }
    private static void Generation()
    {
        var doc=New();doc.Paint(MapDrawing.Rectangle(new(8,8,2,2),true),new(33,1,1));var gate=Gate(doc,8,8,77);
        var imported=Action(doc,32,9,9);var actionBytes=Bytes(imported);var before=Snapshot(doc);var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
        using(var cancel=doc.CreateRegionPropertiesSession(gate))
        {
            using var child=doc.BeginRegionGateGeneration(cancel);doc.AcceptRegionGateGeneration(doc.PreviewRegionGateGeneration(child,child.Defaults),true);
            doc.UpdateRegionProperties(cancel,cancel.Values with{Bounds=new(10,10,1,1)});_=doc.PrepareRegionProperties(cancel);Exact(before);
        }
        using var parent=doc.CreateRegionPropertiesSession(gate);using var accepted=doc.BeginRegionGateGeneration(parent);
        var plan=doc.PreviewRegionGateGeneration(accepted,accepted.Defaults);Expect(plan.Conditions==1&&plan.Actions==1,"Source gate site did not produce one event pair.");
        doc.AcceptRegionGateGeneration(plan,true);doc.UpdateRegionProperties(parent,parent.Values with{Bounds=new(10,10,1,1)});
        var prepared=doc.PrepareRegionProperties(parent);Exact(before);doc.ApplyEditorRegionProperties(prepared);
        Coordinates(imported,actionBytes,11,11);
        var generated=doc.Records().Where(row=>EntityReferences.EventType(row)?.Name=="ACTION_DESTROYGATE").ToArray();
        Expect(generated.Length==2&&generated.All(row=>row.Read(row.Fields.Single(field=>field.Name=="NativeArgument5"))=="11"&&row.Read(row.Fields.Single(field=>field.Name=="NativeArgument7"))=="11"),"Replay moved only generated actions and left the imported gate action behind.");
        Expect(doc.GetHistory().Undo.Count==history+1,"Parent replay split derived updates into extra Undo steps.");
        var after=Snapshot(doc);doc.Undo();Exact(before);Expect(doc.EditorChangeCount==count,"Replay Undo lost original count.");doc.Redo();Exact(after);
    }
}
