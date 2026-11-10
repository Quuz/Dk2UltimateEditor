using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class ScriptIdentitySafetyChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action){try{action();}catch(Exception error) when(error is ArgumentException or InvalidOperationException){return;}throw new InvalidOperationException("Ambiguous/invalid script identity update accepted.");}
    private static NativeField ID(NativeRecord record)=>record.Fields.Single(field=>field.Name is "ID" or "id");
    private static byte[] Array(NativeRecord record){Expect(MemoryMarshal.TryGetArray(record.File.Data,out var bytes)&&bytes.Offset==0,"Literal malformed fixture payload unavailable.");return bytes.Array!;}
    private static void RawID(NativeRecord record,ushort id)=>BinaryPrimitives.WriteUInt16LittleEndian(Array(record).AsSpan(record.Offset+ID(record).Offset),id);
    private static void RawField(NativeRecord record,NativeField field,ushort value)=>field.Encode(value.ToString()).CopyTo(Array(record),record.Offset+field.Offset);
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> state)=>Expect(state.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Script identity rollback changed full payload.");
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("IdentitySafety","IdentitySafety",12,12,NativeFormat:native));
    private static NativeRecord Add(NativeMapDocument document,bool action)=>document.IsLegacyProject?document.AddLegacyRecord(action?"Action":"Trigger",new(0,0))
        :document.AddRecord(RecordTemplate.All.Single(template=>template.Structure==(action?"Action":"Trigger")),new(0,0));
    private static ScriptEdge Root(NativeMapDocument document)=>document.EnsureScriptScope(document.ScriptOwners().First(owner=>owner.Scope==32));
    private static void Expected(Dictionary<NativeFile,byte[]> data,NativeRecord record,NativeField field,ushort value)
        =>field.Encode(value.ToString()).CopyTo(data[record.File],record.Offset+field.Offset);
    private static void Reversible(NativeMapDocument document,NativeRecord record,ushort newID,Dictionary<NativeFile,byte[]> expected)
    {
        var before=Snapshot(document);var counter=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        var notices=0;EventHandler observer=(_,_)=>notices++;document.Changed+=observer;
        document.SetField(record,ID(record),newID.ToString());document.Changed-=observer;
        Exact(expected);Expect(notices==1&&document.EditorChangeCount==unchecked(counter+1)&&document.GetHistory().Undo.Count==history+1,"Identity edit not atomic with one history/counter notification.");
        document.Undo();Exact(before);Expect(document.EditorChangeCount==counter,"Identity Undo lost counter.");document.Redo();Exact(expected);document.Undo();Exact(before);
    }
    public static void Run()
    {
        Expect(LegacyProjectSchemas.Size("NODE")==12,"Original Win32 NODE ABI must be two pointers and a DWORD.");
        foreach(var native in new[]{false,true})
        foreach(var action in new[]{false,true})
        {
            var doc=New(native);var root=Root(doc);var first=Add(doc,action);var second=Add(doc,action);
            RawID(first,0);RawID(second,0);
            var before=Snapshot(doc);var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
            doc.SetField(first,ID(first),"0");Exact(before);Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Retaining malformed imported zero ID created an edit.");
            var fixedID=(ushort)(!native&&action?50000:100);var expected=Snapshot(doc);Expected(expected,first,ID(first),fixedID);
            Reversible(doc,first,fixedID,expected);
            Expect(root.Target==0&&root.Owner.Read(root.Field)=="0"&&ScriptGraph.ID(second)==0,"Zero repair rebound empty roots or another zero event.");
            RawID(first,10);RawID(second,10);
            var incoming=ScriptGraph.Edges(second).First();RawField(incoming.Owner,incoming.Field,10);
            before=Snapshot(doc);count=doc.EditorChangeCount;history=doc.GetHistory().Undo.Count;
            Reject(()=>doc.SetField(first,ID(first),fixedID.ToString()));Exact(before);
            Expect(doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Refused duplicate identity transfer leaked history/count.");
            RawField(incoming.Owner,incoming.Field,0);
            if(!native)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(Array(second).AsSpan(second.Offset+12),10);
                before=Snapshot(doc);Reject(()=>doc.SetField(first,ID(first),fixedID.ToString()));Exact(before);
                BinaryPrimitives.WriteUInt16LittleEndian(Array(second).AsSpan(second.Offset+12),0);
            }
            expected=Snapshot(doc);Expected(expected,first,ID(first),fixedID);Reversible(doc,first,fixedID,expected);
            RawID(second,fixedID);before=Snapshot(doc);Reject(()=>doc.SetField(first,ID(first),fixedID.ToString()));Exact(before);
            if(!native)
            {
                RawID(first,action?(ushort)123:(ushort)50000);RawID(second,action?(ushort)50001:(ushort)101);
                var edge=ScriptGraph.Edges(second).First();RawField(edge.Owner,edge.Field,ScriptGraph.ID(first));RawField(root.Owner,root.Field,ScriptGraph.ID(first));
                before=Snapshot(doc);doc.SetField(first,ID(first),ScriptGraph.ID(first).ToString());Exact(before);
                var newID=action?(ushort)50002:(ushort)102;
                expected=Snapshot(doc);Expected(expected,first,ID(first),newID);Expected(expected,edge.Owner,edge.Field,newID);Expected(expected,root.Owner,root.Field,newID);
                Reversible(doc,first,newID,expected);
                before=Snapshot(doc);Reject(()=>doc.SetField(first,ID(first),(action?"124":"50003")));Exact(before);
            }
        }
        foreach(var native in new[]{false,true})
        {
            var doc=New(native);var root=Root(doc);var target=Add(doc,false);var a=Add(doc,false);var b=Add(doc,false);var reserved=Add(doc,true);
            RawID(target,10);RawID(a,20);RawID(b,20);RawID(reserved,native?(ushort)30:(ushort)50000);
            RawField(root.Owner,root.Field,10);
            var aNext=ScriptGraph.Edges(a).First();var bNext=ScriptGraph.Edges(b).First();RawField(aNext.Owner,aNext.Field,10);RawField(bNext.Owner,bNext.Field,10);
            var expected=Snapshot(doc);Expected(expected,target,ID(target),100);Expected(expected,root.Owner,root.Field,100);Expected(expected,aNext.Owner,aNext.Field,100);Expected(expected,bNext.Owner,bNext.Field,100);
            if(native)
            {
                var opaque=reserved.Fields.Single(field=>field.Name=="ChildAnd");RawField(reserved,opaque,10);expected[reserved.File]=reserved.File.Data.ToArray();
                // Reapply literal expected writes after copying this shared file.
                Expected(expected,target,ID(target),100);Expected(expected,aNext.Owner,aNext.Field,100);Expected(expected,bNext.Owner,bNext.Field,100);
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(Array(a).AsSpan(a.Offset+12),10);BinaryPrimitives.WriteUInt16LittleEndian(Array(b).AsSpan(b.Offset+12),10);
                expected[reserved.File]=reserved.File.Data.ToArray();Expected(expected,target,ID(target),100);Expected(expected,root.Owner,root.Field,100);Expected(expected,aNext.Owner,aNext.Field,100);Expected(expected,bNext.Owner,bNext.Field,100);
                BinaryPrimitives.WriteUInt16LittleEndian(expected[a.File].AsSpan(a.Offset+12),100);BinaryPrimitives.WriteUInt16LittleEndian(expected[b.File].AsSpan(b.Offset+12),100);
            }
            Reversible(doc,target,100,expected);
        }
        Console.WriteLine("SCRIPT IDENTITY DETAIL native/project condition/action zero repair without sentinel changes; referenced duplicate and hidden backlink refusal; unreferenced duplicate repair; wrong-sign import retained/explicit correct-range repair; every physical incoming edge despite other duplicates; native reserved bytes and full atomic UndoRedo");
    }
}
