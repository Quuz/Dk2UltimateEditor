using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class NativeResizeDerivedChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action){try{action();}catch(Exception error) when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Unaccepted derived-target resize applied.");}
    private static NativeMapDocument New(bool native=true)=>NativeMapDocument.CreateNewMap(new("Derived","Derived",32,30,NativeFormat:native));
    private static NativeRecord Object(NativeMapDocument doc,TilePoint point)
    {
        var row=doc.IsLegacyProject?doc.AddLegacyRecord("Object",point):doc.AddRecord(RecordTemplate.All.Single(item=>item.Structure=="GAMEOBJECT"),point);
        doc.SetField(row,row.Fields.Single(field=>field.Name==(doc.IsLegacyProject?"id":"bObjectID")),"28");return row;
    }
    private static NativeRecord Gate(NativeMapDocument doc,TilePoint point,bool heart=false,int kind=1)
    {
        if(doc.IsLegacyProject)return doc.AddLegacyRecord("Gate",point);
        var row=doc.AddRecord(RecordTemplate.All.Single(item=>item.Structure=="GAMEGATE"),point);
        doc.SetField(row,row.Fields.Single(field=>field.Name=="bGateType"),heart?"14":"33");
        doc.SetField(row,row.Fields.Single(field=>field.Name=="bGateID"),kind.ToString(CultureInfo.InvariantCulture));return row;
    }
    private static NativeRecord Action(NativeMapDocument doc,int type)=>doc.AddScenarioEvent(0xD6,new(0,0),LegacyEventSchemas.All.Single(item=>item.RecordSignature==0xD6&&item.Type==type));
    private static void Raw(NativeRecord row,TilePoint point)
    {
        Expect(MemoryMarshal.TryGetArray(row.File.Data,out var data),"Native fixture backing unavailable.");var offset=data.Offset+row.Offset;
        var buffer=data.Array??throw new InvalidOperationException("Native fixture has no array.");
        for(var i=8;i<12;i++)buffer[offset+i]=(byte)(0xA5+i);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset+12),(ushort)point.X);BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset+14),(ushort)point.Y);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset+20),0xAB34);buffer[offset+23]=5;
    }
    private static byte[] Bytes(NativeRecord row)=>row.File.Data.Span.Slice(row.Offset,row.Length).ToArray();
    private static TilePoint Point(NativeRecord row)=>new(BinaryPrimitives.ReadUInt16LittleEndian(row.File.Data.Span.Slice(row.Offset+12)),BinaryPrimitives.ReadUInt16LittleEndian(row.File.Data.Span.Slice(row.Offset+14)));
    private static void OnlyCoordinates(NativeRecord action,byte[] before,TilePoint expected)
    {
        var bytes=before.ToArray();BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12),unchecked((ushort)expected.X));BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14),unchecked((ushort)expected.Y));
        Expect(Bytes(action).AsSpan().SequenceEqual(bytes),"Derived projection changed an identity/link/flag/reserved byte or used the wrong coordinate.");
    }
    private static int Nodes(NativeMapDocument doc)=>doc.Records().Count(row=>row.Category=="GAMEOBJECT"||row.Category=="GAMEGATE"&&row.Read(row.Fields.Single(field=>field.Name=="bGateType"))!="14");
    public static void Run()
    {
        using var source=JsonDocument.Parse(typeof(NativeResizeDerivedChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.NativeResizeDerivedSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var input in source.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Derived source input changed.");
        Expect(source.RootElement.GetProperty("MissingCrystalWords")[0].GetInt32()==65535&&source.RootElement.GetProperty("MissingGateWords")[0].GetInt32()==1&&source.RootElement.GetProperty("FirstCrystalID").GetInt32()==28,"Unverified source missing-target projection.");
        foreach(var size in new[]{(35,33),(29,27),(35,27)})foreach(var anchor in Enumerable.Range(0,9))
        {
            var doc=New();Object(doc,new(18,17));Object(doc,new(12,13));Gate(doc,new(9,10));
            var crystal=Action(doc,2);var gate=Action(doc,32);Raw(crystal,new(25,26));Raw(gate,new(10,11));
            var first=Bytes(crystal);var second=Bytes(gate);var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var counter=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
            var dx=(size.Item1-32)*(anchor%3)/2;var dy=(size.Item2-30)*(anchor/3)/2;
            using var session=doc.CreateMapPropertiesSession();var draft=session.WorkingDocument.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var plan=doc.PrepareEditorMapProperties(session,new(size.Item1,size.Item2,anchor));
            Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&draft.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Derived preview changed live/private input.");
            doc.ApplyEditorMapProperties(plan);OnlyCoordinates(crystal,first,new(18+dx,17+dy));OnlyCoordinates(gate,second,new(10+dx,11+dy));
            Expect(doc.EditorChangeCount==unchecked(counter+(dx!=0||dy!=0?5u:2u))&&doc.GetHistory().Undo.Count==history+1,"Derived targets added a source counter or live Undo.");
            var after=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());doc.Undo();Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&doc.EditorChangeCount==counter,"Derived resize Undo lost whole buffers/count.");
            doc.Redo();Expect(after.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Derived resize Redo lost whole buffers.");
            var saved=SaveProjectionChecks.SaveCopy(doc,Path.Combine(Path.GetTempPath(),"DK2Derived",Guid.NewGuid().ToString("N")));
            var reopened=NativeMapDocument.Load(saved);var actions=reopened.Records().Where(row=>row.File.Signature==0xD2).ToArray();
            Expect(Point(actions.Single(row=>row.File.Data.Span[row.Offset+22]==2))==new TilePoint(18+dx,17+dy)&&Point(actions.Single(row=>row.File.Data.Span[row.Offset+22]==32))==new TilePoint(10+dx,11+dy),"Derived save/reopen lost source projection.");
            Expect(!NativeMapValidator.Validate(reopened).Any(item=>item.Code=="DK2N032"),"Correctly resolved computed targets produced a warning.");
        }
        CropsAndMissing();GenericAndDuplicates();ProjectReferences();
        Console.WriteLine("NATIVE DERIVED RESIZE DETAIL 27 anchor matrices, nine literal crop/missing/heart/duplicate/kind cases, source-first gem and exact old gate resolution; reserved bytes/physical duplicate events retained, generic/equal unchanged, common counters/Undo/Redo/save/reopen and project compile comparison");
    }
    private static void CropsAndMissing()
    {
        foreach(var mode in Enumerable.Range(0,9))
        {
            var doc=New();var first=mode switch{0 or 3=>new TilePoint(18,17),1=>new(11,5),_=>new(5,6)};
            if(mode!=2){Object(doc,first);Object(doc,mode==3?new(19,18):mode==0||mode==1?new(5,6):new(6,7));}
            var location=mode==3?new TilePoint(18,17):mode==4?new(11,5):mode==5?new(5,5):new(9,10);
            Gate(doc,location,heart:mode==5,kind:mode==8?77:1);if(mode==7)Gate(doc,location,kind:99);
            var crystal=Action(doc,2);var gate=Action(doc,32);Raw(crystal,new(25,26));Raw(gate,mode==6?new(24,25):new(location.X+1,location.Y+1));
            var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var actorA=Bytes(crystal);var actorB=Bytes(gate);var counter=doc.EditorChangeCount;var nodes=Nodes(doc);
            var removed=mode switch{0 or 1 or 4=>1,3=>3,_=>0};
            using var session=doc.CreateMapPropertiesSession();var plan=doc.PrepareEditorMapProperties(session,new(12,12,RemoveOutside:true));
            if(removed!=0){Reject(()=>doc.ApplyEditorMapProperties(plan));Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Declined crop changed derived fields.");}
            doc.ApplyEditorMapProperties(plan,croppingAccepted:removed!=0);
            var expectedCrystal=mode is 2 or 3?new TilePoint(65535,65535):new(5,6);
            var expectedGate=mode is 3 or 4 or 5 or 6?new TilePoint(1,1):new(10,11);
            OnlyCoordinates(crystal,actorA,expectedCrystal);OnlyCoordinates(gate,actorB,expectedGate);
            Expect(Nodes(doc)==nodes-removed&&doc.EditorChangeCount==unchecked(counter+2+(uint)removed),"Computed target cropping invented source increments or lost a node.");
            var bytes=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var warnings=NativeMapValidator.Validate(doc).Where(item=>item.Code=="DK2N032").ToArray();
            var expectedWarnings=(mode is 2 or 3?1:0)+(mode is 3 or 4 or 5 or 6?1:0);
            Expect(warnings.Length==expectedWarnings&&warnings.All(item=>item.Severity==DiagnosticSeverity.Warning)&&bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Missing computed target warning changed data or severity.");
            Expect(!NativeMapValidator.Validate(doc,includeScripts:false).Any(item=>item.Code=="DK2N032"),"No-scenario validation acquired a computed target check.");
            doc.Undo();Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Computed crop Undo lost exact original fields.");
        }
    }
    private static void GenericAndDuplicates()
    {
        var doc=New();Object(doc,new(6,7));Gate(doc,new(9,10));var crystal=Action(doc,2);var gate=Action(doc,32);Raw(crystal,new(6,7));Raw(gate,new(10,11));
        var a=Bytes(crystal);var b=Bytes(gate);doc.Resize(36,34,4,4,new(2,2,1));
        Expect(Bytes(crystal).AsSpan().SequenceEqual(a)&&Bytes(gate).AsSpan().SequenceEqual(b),"Generic raw resize acquired editor compiler projection.");
        using(var equal=doc.CreateMapPropertiesSession())doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(equal));
        Expect(Bytes(crystal).AsSpan().SequenceEqual(a)&&Bytes(gate).AsSpan().SequenceEqual(b),"Equal properties acceptance normalized imported targets.");
        using(var canceled=doc.CreateMapPropertiesSession())_=doc.PrepareEditorMapProperties(canceled,new(40,38,8));
        Expect(Bytes(crystal).AsSpan().SequenceEqual(a)&&Bytes(gate).AsSpan().SequenceEqual(b),"Canceled computed-target plan wrote live data.");
        doc=New();Object(doc,new(6,7));Gate(doc,new(9,10));var one=Action(doc,2);var two=Action(doc,2);var three=Action(doc,32);
        Raw(one,new(6,7));Raw(two,new(6,7));Raw(three,new(10,11));
        Expect(MemoryMarshal.TryGetArray(two.File.Data,out var raw),"Duplicate fixture backing missing.");
        BinaryPrimitives.WriteUInt16LittleEndian(raw.Array!.AsSpan(raw.Offset+two.Offset+16),ScriptGraph.ID(one));
        var originals=new[]{one,two,three}.Select(Bytes).ToArray();var baseline=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        using(var session=doc.CreateMapPropertiesSession())doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(session,new(36,34,8)));
        OnlyCoordinates(one,originals[0],new(10,11));OnlyCoordinates(two,originals[1],new(10,11));OnlyCoordinates(three,originals[2],new(14,15));
        doc.Undo();Expect(baseline.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Physical duplicate-event target projection lost raw IDs/bytes on Undo.");
        doc.SetHistoryDepth(0);using(var session=doc.CreateMapPropertiesSession())doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(session,new(36,34,8)));
        Expect(!doc.CanUndo&&doc.GetHistory().SnapshotBytes==0&&Point(one)==new TilePoint(10,11),"Zero-depth computed projection lost behavior or retained Undo.");
    }
    private static void ProjectReferences()
    {
        var doc=New(false);Object(doc,new(18,17));Object(doc,new(12,13));var region=Gate(doc,new(9,10));var gateID=region.Read(region.Fields.Single(field=>field.Name=="id"));
        var crystal=Action(doc,2);var gate=Action(doc,32);doc.SetField(gate,gate.Fields.Single(field=>field.Name=="param5"),gateID);
        var a=Bytes(crystal);var b=Bytes(gate);
        using(var session=doc.CreateMapPropertiesSession())doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(session,new(36,34,8)));
        Expect(Bytes(crystal).AsSpan().SequenceEqual(a)&&Bytes(gate).AsSpan().SequenceEqual(b),"Project resize replaced semantic references with native coordinates.");
        var compiled=doc.CompileLegacyProject("DerivedProject",NativeMapDocument.Create(36,34,"Definitions"));var actions=compiled.Records().Where(row=>row.File.Signature==0xD2).ToArray();
        Expect(Point(actions.Single(row=>row.File.Data.Span[row.Offset+22]==2))==new TilePoint(22,21)&&Point(actions.Single(row=>row.File.Data.Span[row.Offset+22]==32))==new TilePoint(14,15),"Project compiler/source computed-target reference differs from native resize.");
    }
}
