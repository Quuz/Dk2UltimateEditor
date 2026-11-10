using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class LargeMapWarningChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new InvalidOperationException("Unsafe large-map approval accepted.");}
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("LargeRequest","LargeRequest",16,14,NativeFormat:native));
    private static NewMapOptions Options(bool native,int width=121,int height=121)=>new("LargeRequest","LargeRequest",width,height,NativeFormat:native);
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> snapshot)=>Check(snapshot.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Approval changed map bytes.");
    private static MapPropertiesPlan Plan(NativeMapDocument document,MapPropertiesSession session,int width=121,int height=121)
        =>document.PrepareEditorMapProperties(session,new(width,height,RemoveOutside:true));
    public static void Run()
    {
        var matrices=0;
        foreach(var native in new[]{false,true})
        {
            foreach(var (width,height) in new[]{(7,7),(120,120),(119,121),(121,121),(300,48),(300,49),(301,7)})
            {
                using var policy=new EditorLargeMapPolicy();var options=Options(native,width,height);using var request=policy.BeginNew(options,()=>true);
                var needed=width>300||height>300||(long)width*height>14400;
                Check((request is not null)==needed,"New-map threshold/retained extension warning changed.");
                if(request is not null){Check(request.RequiresPrompt&&request.Width==width&&request.Height==height,"Wrong frozen new-map size.");Check(request.Decline()&&!request.IsAccepted&&policy.Flags==0,"New-map No remembered acceptance.");}
                matrices++;
            }
            foreach(var (width,height) in new[]{(120,120),(119,121),(121,121),(300,48),(300,49),(301,7)})
            {
                var document=New(native);using var session=document.CreateMapPropertiesSession();var plan=Plan(document,session,width,height);
                var bytes=Snapshot(document);var working=Snapshot(session.WorkingDocument);var history=document.GetHistory();
                using var policy=new EditorLargeMapPolicy();using var request=policy.BeginResize(plan,()=>true);
                Check((request is not null)==((long)width*height>14400),"Resize strict source area threshold changed.");
                if(request is not null){Check(!request.Decline(true)&&request.IsCurrent,"Checked No closed the request.");request.Dispose();Check(policy.Flags==0,"Checked close remembered approval.");}
                Exact(bytes);Exact(working);Check(document.GetHistory().Undo.SequenceEqual(history.Undo)&&document.EditorChangeCount==history.EditorChangeCount,"Preview/No allocated map history.");matrices++;
            }
            Shared(native);Guards(native);
        }
        VerifyInformationResource();
        Console.WriteLine($"BIG MAP REQUEST DETAIL {matrices} new/resize native/project size matrices; shared0x20/defaultYes, strict source area14400, retained modern300 extension, No/checked close/remember/reset, approval-only history, UndoRedo, stale/raw/working/lease/callback guards; compiled original information icon exact without WPF execution");
    }
    private static void Shared(bool native)
    {
        var shared=new EditorRequestPreferences();using var creation=new EditorLargeMapPolicy(shared);using var resize=new EditorLargeMapPolicy(shared);
        var document=New(native);var bytes=Snapshot(document);var history=document.GetHistory();
        using(var session=document.CreateMapPropertiesSession())
        {
            var plan=Plan(document,session);
            using(var first=creation.BeginNew(Options(native),()=>true)!)Check(first.Accept(true)&&first.IsAccepted&&creation.Flags==0x20,"New-map remembered Yes failed.");
            Exact(bytes);Check(document.GetHistory().Undo.SequenceEqual(history.Undo),"New-map approval constructed or edited a document.");
            using(var suppressed=resize.BeginResize(plan,()=>true)!)Check(!suppressed.RequiresPrompt&&suppressed.Accept(),"New and resize do not share the same request bit.");
            Exact(bytes);Check(document.Width==16&&document.GetHistory().Undo.SequenceEqual(history.Undo),"Resize approval applied the map before the caller.");
            document.ApplyEditorMapProperties(plan,largeMapAccepted:true,croppingAccepted:true);Check(document.Width==121&&document.GetHistory().Undo.Count==history.Undo.Count+1,"Caller could not apply accepted resize once.");
            document.Undo();Exact(bytes);Check(resize.Flags==0x20,"Undo rolled back a process preference.");document.Redo();Check(document.Width==121,"Resize Redo missing.");document.Undo();Exact(bytes);
            Reject(()=>resize.BeginResize(plan,()=>true));
        }
        var other=New(native);using(var session=other.CreateMapPropertiesSession())using(var approval=resize.BeginResize(Plan(other,session,130,130),()=>true)!)Check(!approval.RequiresPrompt,"Shared reply did not cross documents.");
        using(var fresh=new EditorLargeMapPolicy())using(var asks=fresh.BeginNew(Options(native),()=>true)!)Check(asks.RequiresPrompt,"A fresh session inherited suppression.");
        using(var policy=new EditorLargeMapPolicy(new EditorRequestPreferences(0xF32)))
        {using var active=policy.BeginNew(Options(native),()=>true)!;policy.ResetSuppression();Check(active.IsClosed&&policy.Flags==0xF12,"Reset cleared another source request bit.");}
        resize.ResetSuppression();using(var asks=creation.BeginNew(Options(native),()=>true)!)Check(asks.RequiresPrompt,"Shared reset did not reach New Map.");
        // The reverse caller direction must share the same bit as well.
        using(var session=other.CreateMapPropertiesSession())using(var approval=resize.BeginResize(Plan(other,session),()=>true)!)Check(approval.Accept(true),"Resize Yes failed.");
        using(var suppressed=creation.BeginNew(Options(native),()=>true)!)Check(!suppressed.RequiresPrompt,"Resize reply did not reach New Map.");
    }
    private static void Guards(bool native)
    {
        var shared=new EditorRequestPreferences();using var policy=new EditorLargeMapPolicy(shared);var options=Options(native);
        Reject(()=>policy.BeginNew(options with{Width=0},()=>true));Reject(()=>policy.BeginNew(options,()=>false));
        Reject(()=>policy.BeginNew(options,()=>{policy.ResetSuppression();return true;}));
        using(var usable=policy.BeginNew(options,()=>true)!)Check(usable.IsCurrent,"Rejected callback leaked the active lease.");
        var revision=0;var current=options;using(var old=policy.BeginNew(options,()=>revision==0&&current==options)!)
        {current=options with{Width=130};revision++;current=options;revision++;Check(!old.IsCurrent,"Restored UI values revived an old request.");Reject(()=>old.Accept(true));}
        Check(policy.Flags==0,"Stale approval remembered Yes.");
        var document=New(native);var root=document.EnsureScriptScope(document.ScriptOwners().Single(owner=>owner.Scope==32));
        using var session=document.CreateMapPropertiesSession();var plan=Plan(document,session);var bytes=Snapshot(document);
        using(var request=policy.BeginResize(plan,()=>true)!)
        {
            using var scripts=new EditorScenarioDeletionPolicy(shared);
            Reject(()=>scripts.BeginPoint(document,root));Check(request.IsCurrent,"Another caller stole the large-map lease.");
        }
        using(var scripts=new EditorScenarioDeletionPolicy(shared))using(var active=scripts.BeginPoint(document,root))
        {var callbacks=0;Reject(()=>policy.BeginNew(options,()=>{callbacks++;return true;}));Reject(policy.ResetSuppression);Check(callbacks==0&&active.IsCurrent,"Busy lease ran a callback or was reset by another caller.");}
        using(var request=policy.BeginResize(plan,()=>true)!)
        {
            Check(MemoryMarshal.TryGetArray(document.MainFile.Data,out var raw),"Raw large-map fixture missing.");raw.Array![raw.Offset+raw.Count-1]^=1;
            Check(!request.IsCurrent,"Raw source edit left approval current.");Reject(()=>request.Accept(true));raw.Array[raw.Offset+raw.Count-1]^=1;
        }
        using(var pen=document.BeginLivePenStroke(new(5,2,1)))Reject(()=>policy.BeginResize(plan,()=>true));
        using(var pen=session.WorkingDocument.BeginLivePenStroke(new(5,2,1)))Reject(()=>policy.BeginResize(plan,()=>true));
        using(var request=policy.BeginResize(plan,()=>true)!)
        {session.WorkingDocument.Paint([new(2,2)],new(5,2,1));session.WorkingDocument.Undo();Check(!request.IsCurrent,"Working Undo revived old approval.");Reject(()=>request.Accept(true));}
        using(var renewed=document.CreateMapPropertiesSession())
        {
            var next=Plan(document,renewed);using var request=policy.BeginResize(next,()=>true)!;
            document.Paint([new(2,2)],new(5,2,1));document.Undo();Check(!request.IsCurrent,"Source Undo revived old approval.");Reject(()=>request.Accept(true));
        }
        Exact(bytes);Check(policy.Flags==0,"Refused resize mutated map or preferences.");
        using(var renewed=document.CreateMapPropertiesSession())
        {var next=Plan(document,renewed);using var request=policy.BeginResize(next,()=>true)!;renewed.Dispose();Check(!request.IsCurrent,"Closed parent session kept approval current.");Reject(()=>request.Accept(true));}
    }
    private static void VerifyInformationResource()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
        var expected=File.ReadAllBytes(Path.Combine(root,"Source/Icons/Request/Information32.ico"));
        using var stream=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin/Release/net10.0-windows/DK2MapEditor.App.dll"));
        using var pe=new PEReader(stream);var metadata=pe.GetMetadataReader();
        var resource=metadata.ManifestResources.Select(metadata.GetManifestResource).Single(item=>metadata.GetString(item.Name)=="Legacy.Request.Information");
        Check(resource.Implementation.IsNil,"Information icon is not embedded.");
        var content=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress).GetContent();var offset=checked((int)resource.Offset);
        var length=BinaryPrimitives.ReadInt32LittleEndian(content.AsSpan(offset,4));Check(length==expected.Length&&content.AsSpan(offset+4,length).SequenceEqual(expected),"Compiled information icon differs from the original.");
    }
}
