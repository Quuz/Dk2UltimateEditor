using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using DK2MapEditor.Core.Editing;

internal static class RegionCreationChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception e)when(e is InvalidOperationException or ArgumentException){return;}throw new Exception("Invalid creation accepted.");}
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("Creation","Creation",24,24,NativeFormat:native));
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Creation changed unexpected live bytes.");
    public static void Run()
    {
        var directory=Path.Combine(Path.GetTempPath(),"RegionCreation-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            foreach(var native in new[]{false,true})
            {
                foreach(var kind in Enum.GetValues<MapRegionKind>())Matrix(native,kind,directory);
                Lifetimes(native);Generation(native);Allocation(native);
            }
        }
        finally{Directory.Delete(directory,true);}
    }
    private static void Matrix(bool native,MapRegionKind kind,string directory)
    {
        var d=New(native);
        if(kind==MapRegionKind.Gate){d.Paint([new(4,5)],new(37,1,2));d.Paint([new(5,5)],new(1,1,1));}
        var original=Bytes(d);var history=d.GetHistory();var revision=d.EditRevision;var count=d.EditorChangeCount;
        var stats=d.ReadActivityStatistics()!.Encode();var notices=0;d.Changed+=(_,_)=>notices++;
        using(var cancel=d.CreateRegionCreationSession(kind,new(1,1,1,1)))
        {
            Check(cancel.Properties.Values.AreaFlags==0&&cancel.Properties.Values.Frequency==(kind==MapRegionKind.Effect?1:0),"Source zero flags/frequency defaults differ.");
            d.PrepareRegionCreation(cancel);Exact(original);
        }
        Check(d.EditRevision==revision&&d.EditorChangeCount==count&&notices==0&&d.GetHistory().Undo.Count==history.Undo.Count
            &&d.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(stats),"Cancelled creation changed counters/history/activity.");
        using var s=d.CreateRegionCreationSession(kind,new(1,1,1,1));var working=s.WorkingDocument;var p=s.Properties;
        var prefix=kind switch{MapRegionKind.Area=>"Z",MapRegionKind.Gate=>"P",_=>"E"};
        if(!native)Check(p.Values.Name==prefix+"1","Original default region prefix/ID differs.");
        var values=p.Values with{Bounds=new(4,5,kind==MapRegionKind.Gate?1:2,kind==MapRegionKind.Gate?1:3)};
        if(!native)values=values with{Name="Custom",Information="Full draft",Colour=0x123456,TextColour=0xABCDEF};
        if(kind==MapRegionKind.Area)values=values with{AreaFlags=81};
        if(kind==MapRegionKind.Effect)values=values with{Effect1=1,Effect2=2,Effect3=3,Effect4=4,Frequency=9};
        working.UpdateRegionProperties(p,values);var old=d.PrepareRegionCreation(s);
        working.UpdateRegionProperties(p,values);Reject(()=>d.ApplyEditorRegionCreation(old));Exact(original);
        var plan=d.PrepareRegionCreation(s);Exact(original);
        var defaults=s.ContinuationDefaults;var added=d.ApplyEditorRegionCreation(plan);Reject(()=>d.ApplyEditorRegionCreation(plan));
        Check(notices==1&&d.EditorChangeCount==unchecked(count+1)&&d.GetHistory().Undo.Count==history.Undo.Count+1&&d.EditRevision==revision+1,"Add is not one live operation.");
        Check(added.File.Data.Span.Slice(added.Offset,added.Length).SequenceEqual(Expected(native,kind,values)),"Full created record differs from literal source layout/defaults.");
        Check(original.Where(row=>row.Key!=added.File).All(row=>row.Key.Data.Span.SequenceEqual(row.Value)),"Creation changed another map file.");
        var after=Bytes(d);d.Undo();Exact(original);Check(d.EditorChangeCount==count,"Add Undo lost source count.");d.Redo();Exact(after);
        using(var second=d.CreateRegionCreationSession(kind,values.Bounds,defaults))
        {
            Check(second.Properties.Values==values,"Repeated Add lost custom draft values.");
            var next=d.ApplyEditorRegionCreation(d.PrepareRegionCreation(second));
            Check(d.MapRegions().Count(r=>r.Kind==kind)==2,"Repeated Add did not create a second independent record.");
            d.Undo();Exact(after);
        }
        if(!native)
        {
            using var automatic=d.CreateRegionCreationSession(kind,new(1,2,1,1));
            automatic.WorkingDocument.UpdateRegionProperties(automatic.Properties,automatic.Properties.Values with{Name=prefix+"2suffix"});
            Check(automatic.ContinuationDefaults.AutomaticName,"Source atoi-style automatic-name recognition differs.");
            var seed=automatic.ContinuationDefaults;d.ApplyEditorRegionCreation(d.PrepareRegionCreation(automatic));
            using var next=d.CreateRegionCreationSession(kind,new(1,2,1,1),seed);
            Check(next.Properties.Values.Name==prefix+"3","Automatic name did not advance to the next free ID.");d.Undo();Exact(after);
        }
        var path=d.SaveCopy(Path.Combine(directory,native+"-"+kind));var loaded=NativeMapDocument.Load(path);
        var loadedRecord=loaded.MapRegions().Single(r=>r.Kind==kind).Record;
        using var read=loaded.CreateRegionPropertiesSession(loadedRecord);
        Check(read.Values.Bounds==values.Bounds&&read.Values.AreaFlags==values.AreaFlags&&read.Values.Frequency==values.Frequency,"Save/reopen lost creation values.");
        if(!native)Check(read.Values.Name==values.Name&&read.Values.Information==values.Information&&read.Values.Colour==values.Colour,"Save/reopen lost editor values.");
    }
    private static byte[] Expected(bool native,MapRegionKind kind,RegionPropertiesValues v)
    {
        var b=new byte[native?kind switch{MapRegionKind.Area=>64,MapRegionKind.Gate=>28,_=>44}:292];
        void U(int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(offset),value);
        void W(int offset,ushort value)=>BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(offset),value);
        if(!native)
        {
            U(8,(uint)kind);U(12,4);U(16,5);U(20,kind==MapRegionKind.Gate?4u:5u);U(24,kind==MapRegionKind.Gate?5u:7u);
            U(32,v.Colour);U(36,v.TextColour);U(44,1);U(48,v.AreaFlags);
            if(kind==MapRegionKind.Effect){U(60,1);U(64,2);U(68,3);U(72,4);U(76,9);}
            Encoding.ASCII.GetBytes(v.Name).CopyTo(b,100);Encoding.ASCII.GetBytes(v.Information).CopyTo(b,164);
        }
        else
        {
            U(0,kind switch{MapRegionKind.Area=>0xC5u,MapRegionKind.Gate=>0xCCu,_=>0xCBu});U(4,(uint)b.Length-8);U(8,4);U(12,5);
            if(kind==MapRegionKind.Area){U(16,5);U(20,7);b[26]=81;b[30]=1;}
            if(kind==MapRegionKind.Gate){b[22]=6;b[23]=1;W(24,100);b[26]=37;b[27]=1;}
            if(kind==MapRegionKind.Effect){W(24,1);W(26,2);W(28,1);W(30,2);W(32,3);W(34,4);b[36]=9;b[37]=1;}
        }
        return b;
    }
    private static void Lifetimes(bool native)
    {
        var d=New(native);using var s=d.CreateRegionCreationSession(MapRegionKind.Area,new(1,1,1,1));var plan=d.PrepareRegionCreation(s);var bytes=Bytes(d);
        Reject(()=>New(native).ApplyEditorRegionCreation(plan));
        var pick=s.WorkingDocument.BeginRegionPropertiesPick(s.Properties);Reject(()=>d.ApplyEditorRegionCreation(plan));Reject(()=>d.PrepareRegionCreation(s));
        s.WorkingDocument.CancelRegionPropertiesPick(pick);
        Reject(()=>d.Edit("Nested",()=>d.ApplyEditorRegionCreation(plan)));Exact(bytes);
        using(var pen=d.BeginLivePenStroke(new(4,1,1))){Reject(()=>d.ApplyEditorRegionCreation(plan));}Exact(bytes);
        d.Paint([new(7,7)],new(4,1,1));d.Undo();Reject(()=>d.ApplyEditorRegionCreation(plan));Exact(bytes);
        using var raw=d.CreateRegionCreationSession(MapRegionKind.Area,new(1,1,1,1));var rawPlan=d.PrepareRegionCreation(raw);
        var file=raw.WorkingDocument.Files.First();Check(MemoryMarshal.TryGetArray(file.Data,out var buffer),"No raw fixture.");buffer.Array![buffer.Offset+file.Length-1]^=1;
        Reject(()=>d.ApplyEditorRegionCreation(rawPlan));Exact(bytes);
        using var tampered=d.CreateRegionCreationSession(MapRegionKind.Area,new(1,1,1,1));tampered.WorkingDocument.Paint([new(7,7)],new(4,1,1));Reject(()=>d.PrepareRegionCreation(tampered));Exact(bytes);
        var closed=d.CreateRegionCreationSession(MapRegionKind.Area,new(1,1,1,1));var closedPlan=d.PrepareRegionCreation(closed);closed.Dispose();Reject(()=>d.ApplyEditorRegionCreation(closedPlan));
        using var observed=d.CreateRegionCreationSession(MapRegionKind.Area,new(1,1,1,1));var observedPlan=d.PrepareRegionCreation(observed);
        EventHandler bad=(_,_)=>throw new InvalidOperationException("After commit");d.Changed+=bad;
        try{Reject(()=>d.ApplyEditorRegionCreation(observedPlan));}finally{d.Changed-=bad;}
        Check(d.MapRegions().Count==1&&!d.IsRegionCreationSessionCurrent(observed),"Observer failure left a repeatable accepted Add.");d.Undo();Exact(bytes);
    }
    private static void Generation(bool native)
    {
        var d=New(native);d.Paint(MapDrawing.Rectangle(new(8,8,2,2),true),new(33,1,1));var bytes=Bytes(d);var count=d.EditorChangeCount;var history=d.GetHistory();
        using var s=d.CreateRegionCreationSession(MapRegionKind.Gate,new(8,8,1,1));var w=s.WorkingDocument;
        using(var canceled=w.BeginRegionGateGeneration(s.Properties)){w.PreviewRegionGateGeneration(canceled,canceled.Defaults);}
        Exact(bytes);
        using var child=w.BeginRegionGateGeneration(s.Properties);var options=child.Defaults with{Flags=HeroGateGenerationFlags.GateDestroy|HeroGateGenerationFlags.AreaDestroy|HeroGateGenerationFlags.Events,Keepers=1};
        var generation=w.PreviewRegionGateGeneration(child,options);Check(generation.Areas==1&&generation.Conditions==1&&generation.Actions==1,"Creation child did not find the prospective gate.");
        w.AcceptRegionGateGeneration(generation,true);Exact(bytes);d.ApplyEditorRegionCreation(d.PrepareRegionCreation(s));
        Check(d.MapRegions().Count==2&&ScriptGraph.Build(d).Events.Count==2&&d.GetHistory().Undo.Count==history.Undo.Count+1,"Composed Add split history or lost generated records.");
        Check(d.EditorChangeCount==unchecked(count+5),"Composed creation source counters differ.");var after=Bytes(d);d.Undo();Exact(bytes);d.Redo();Exact(after);
    }
    private static void Allocation(bool native)
    {
        var d=New(native);using var s=d.CreateRegionCreationSession(MapRegionKind.Area,new(1,1,1,1));var plan=d.PrepareRegionCreation(s);var bytes=Bytes(d);var revision=d.EditRevision;var count=d.EditorChangeCount;var history=d.GetHistory();
        var field=typeof(NativeMapDocument).GetField("historyBufferCopy",BindingFlags.Instance|BindingFlags.NonPublic)!;var prior=field.GetValue(d);var calls=0;
        // Fail after the live action ran, during delta preparation, to verify
        // full rollback rather than merely refusing a pre-action allocation.
        var fileCount=bytes.Count+(native?6:0);
        field.SetValue(d,new Func<byte[],byte[]>(b=>++calls>fileCount?throw new OutOfMemoryException("Creation history fixture"):b.ToArray()));
        try{try{d.ApplyEditorRegionCreation(plan);throw new Exception("Allocation accepted.");}catch(EditorHistoryMemoryException){}}
        finally{field.SetValue(d,prior);}
        Exact(bytes);Check(d.EditRevision==revision&&d.EditorChangeCount==count&&d.GetHistory().Undo.Count==history.Undo.Count&&d.IsRegionCreationSessionCurrent(s),"Allocation failure lost history or retryable draft.");
        d.ApplyEditorRegionCreation(plan);Check(d.MapRegions().Count==1,"Retry after full rollback failed.");
    }
}
