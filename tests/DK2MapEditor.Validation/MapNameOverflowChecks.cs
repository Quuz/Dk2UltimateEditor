using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class MapNameOverflowChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException){return;}throw new InvalidOperationException("Unsafe map-name operation accepted.");}
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("Original","Original",16,14,NativeFormat:native));
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Name decision changed unexpected bytes.");
    private static void Stage(NativeMapDocument working,MapGeneralDraft draft,string name)
        =>working.ApplyMapGeneralSettings(working.UpdateMapGeneralSettings(draft,draft.Values.Select(row=>new MapGeneralInput(row.Key,row.Key switch{"Name"=>name,"Players"=>"2",_=>row.Value}))));
    public static void Run()
    {
        var matrices=0;var directory=Path.Combine(Path.GetTempPath(),"DK2MapName",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        foreach(var native in new[]{false,true})
        {
            foreach(var length in new[]{0,63,64,65,100})
            foreach(var reply in length>64?new[]{EditorMapNameReply.Cancel,EditorMapNameReply.Copy,EditorMapNameReply.Keep}:new[]{EditorMapNameReply.Copy})
            {
                var document=New(native);using var session=document.CreateMapPropertiesSession();var working=session.WorkingDocument;
                var before=Snapshot(document);var privateBefore=Snapshot(working);var count=document.EditorChangeCount;
                var draft=working.CreateMapGeneralDraft();var text=new string('X',length);var plan=working.PrepareEditorMapName(draft,text,session);
                using var policy=new EditorMapNamePolicy();using var request=policy.Begin(plan,()=>document.IsMapPropertiesSessionCurrent(session));
                Check((request is not null)==(length>64)&&plan.EnteredName==text,"Source strict >64 threshold/frozen text changed.");
                string? name;
                if(request is null)name=plan.CopyName();
                else if(reply==EditorMapNameReply.Copy){Check(request.Copy(),"Copy failed.");name=request.SelectedName;Reject(()=>request.Copy());}
                else if(reply==EditorMapNameReply.Keep){Check(!request.Keep(true)&&request.IsCurrent&&request.Keep(),"Checked No closed request or No failed.");name=request.SelectedName;}
                else{request.Dispose();name=request.SelectedName;Check(request.Reply==EditorMapNameReply.Cancel,"Close invented a reply.");}
                Exact(before);Exact(privateBefore);Check(document.GetHistory().Undo.Count==0&&working.GetHistory().Undo.Count==0,"Decision allocated history.");
                if(reply==EditorMapNameReply.Cancel){Check(name is null&&policy.Flags==0,"Cancel remembered/copy changed name.");matrices++;continue;}
                var expectedName=reply==EditorMapNameReply.Keep?"Original":new string('X',Math.Min(63,length));Check(name==expectedName,"Wrong source copied/retained name.");
                Stage(working,draft,name!);Exact(before);
                var parent=document.PrepareEditorMapProperties(session);document.ApplyEditorMapProperties(parent);
                var expected=before.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToArray());
                var record=document.CreateMapGeneralDraft().Record!;var output=expected[record.File];var offset=record.Offset+(native?56:16);
                if(reply!=EditorMapNameReply.Keep){Array.Clear(output,offset,native?128:64);(native?Encoding.Unicode:Encoding.ASCII).GetBytes(expectedName).CopyTo(output,offset);}
                if(native)output[25660]=2;else BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(record.Offset+8),2);
                Exact(expected);Check(document.GetHistory().Undo.Count==1&&document.EditorChangeCount==unchecked(count+1),"Parent Save did not use one source Undo/count.");
                var saved=SaveProjectionChecks.SaveCopy(document,Path.Combine(directory,$"case-{native}-{length}-{reply}"));
                Check(NativeMapDocument.Load(saved).CreateMapGeneralDraft().Values["Name"]==expectedName,"Saved name did not reopen.");
                document.Undo();Exact(before);document.Redo();Exact(expected);matrices++;
            }
            Shared(native);Guards(native);HostedGuards(native);UnicodeAndImported(native);
        }
        Historical(directory);
        Console.WriteLine($"MAP NAME OVERFLOW DETAIL {matrices} native/project empty63/64/65/100 x Copy/Keep/Cancel; source >64/buffer64/defaultCopy, private name/other-field composition, literal bytes/one parent Undo/count, save/reopen, session0x4/remember/reset/lease, stale/raw/pen/callback/foreign guards; generic validation and native Unicode retained");
    }
    private static void Shared(bool native)
    {
        var shared=new EditorRequestPreferences();using var a=new EditorMapNamePolicy(shared);using var b=new EditorMapNamePolicy(shared);
        var document=New(native);var before=Snapshot(document);
        using(var session=document.CreateMapPropertiesSession())
        {
            var working=session.WorkingDocument;var plan=working.PrepareEditorMapName(working.CreateMapGeneralDraft(),new string('A',80));
            using(var checkedClose=a.Begin(plan,()=>true)!){Check(!checkedClose.Keep(true),"Remember allowed No.");}
            Check(a.Flags==0,"Checked close remembered copy.");
            using(var request=a.Begin(plan,()=>true)!)Check(request.Copy(true)&&a.Flags==4,"Copy did not remember source bit.");
            Exact(before);session.Dispose();
        }
        using(var session=document.CreateMapPropertiesSession())
        {
            var working=session.WorkingDocument;var plan=working.PrepareEditorMapName(working.CreateMapGeneralDraft(),new string('B',90));
            using(var request=b.Begin(plan,()=>true)!)Check(!request.RequiresPrompt&&request.Copy()&&request.SelectedName==new string('B',63),"Suppressed copy did not use new text.");
        }
        using(var fresh=new EditorMapNamePolicy())using(var request=fresh.Begin(document.PrepareEditorMapName(document.CreateMapGeneralDraft(),new string('C',65)),()=>true)!)Check(request.RequiresPrompt,"Fresh process inherited suppression.");
        document.Paint([new(2,2)],new(5,2,1));document.Undo();Exact(before);Check(b.Flags==4,"Map Undo reset preference.");
        b.ResetSuppression();Check(a.Flags==0,"Reset did not reach another caller.");
        using var otherBits=new EditorMapNamePolicy(new EditorRequestPreferences(0xF76));otherBits.ResetSuppression();Check(otherBits.Flags==0xF72,"Name reset changed another source bit.");
    }
    private static void Guards(bool native)
    {
        var document=New(native);var before=Snapshot(document);var plan=document.PrepareEditorMapName(document.CreateMapGeneralDraft(),new string('X',65));
        using var policy=new EditorMapNamePolicy();Reject(()=>policy.Begin(plan,()=>false));Reject(()=>policy.Begin(plan,()=>{policy.ResetSuppression();return true;}));
        using(var request=policy.Begin(plan,()=>true)!)Check(request.IsCurrent,"Callback rejection leaked lease.");
        var revision=0;using(var request=policy.Begin(plan,()=>revision==0)!){revision++;revision++;Check(!request.IsCurrent,"Changed UI revived request.");Reject(()=>request.Copy(true));}
        using(var pen=document.BeginLivePenStroke(new(5,2,1)))Reject(()=>policy.Begin(plan,()=>true));
        using(var request=policy.Begin(plan,()=>true)!)
        {
            Check(MemoryMarshal.TryGetArray(document.MainFile.Data,out var bytes),"Raw fixture unavailable.");bytes.Array![bytes.Offset+bytes.Count-1]^=1;
            Check(!request.IsCurrent,"Raw edit left name request current.");Reject(()=>request.Copy(true));bytes.Array[bytes.Offset+bytes.Count-1]^=1;
        }
        using(var request=policy.Begin(plan,()=>true)!){document.Paint([new(2,2)],new(5,2,1));document.Undo();Check(!request.IsCurrent,"Undo revived name request.");Reject(()=>request.Copy(true));}
        Exact(before);Check(policy.Flags==0,"Rejected reply changed preference.");
        var fresh=document.PrepareEditorMapName(document.CreateMapGeneralDraft(),new string('Y',65));var shared=new EditorRequestPreferences();using var name=new EditorMapNamePolicy(shared);using var large=new EditorLargeMapPolicy(shared);
        using(var request=name.Begin(fresh,()=>true)!){Reject(()=>large.BeginNew(new("Large","Large",121,121),()=>true));Reject(large.ResetSuppression);Check(request.IsCurrent,"Other caller stole/reset name lease.");}
        using(var request=large.BeginNew(new("Large","Large",121,121),()=>true)!){var calls=0;Reject(()=>name.Begin(fresh,()=>{calls++;return true;}));Reject(name.ResetSuppression);Check(calls==0&&request.IsCurrent,"Busy lease invoked name caller or was stolen.");}
        var foreign=New(native);Reject(()=>foreign.PrepareEditorMapName(document.CreateMapGeneralDraft(),"Other"));
        // No ignores an unrepresentable proposal while preserving other fields;
        // Yes must still pass the strict existing storage encoder.
        var invalid=document.PrepareEditorMapName(document.CreateMapGeneralDraft(),"\0"+new string('Z',65));
        using(var request=policy.Begin(invalid,()=>true)!){Reject(()=>request.Copy(true));Check(policy.Flags==0&&request.IsCurrent&&request.Keep(),"Invalid Copy lost No or remembered approval.");}
        var draft=document.CreateMapGeneralDraft();Reject(()=>document.UpdateMapGeneralSettings(draft,MapGeneralSettingsChecks.Inputs(draft,"Name",new string('X',64))));Exact(before);
    }
    private static void UnicodeAndImported(bool native)
    {
        var document=New(native);var draft=document.CreateMapGeneralDraft();
        var text=native?new string('Ж',62)+"😀TAIL":new string('é',70);var plan=document.PrepareEditorMapName(draft,text);
        Check(plan.CopyName()==(native?new string('Ж',62):new string('é',63)),"Native surrogate boundary/Western ANSI copy changed.");
        if(!native){var invalid=document.PrepareEditorMapName(draft,new string('Ж',70));using var policy=new EditorMapNamePolicy();using var request=policy.Begin(invalid,()=>true)!;Reject(()=>request.Copy());Check(request.Keep(),"No could not discard an unencodable new name.");}
        var field=draft.Record!.Fields.Single(item=>item.Name==(native?"wstrMainName":"Name"));
        Check(MemoryMarshal.TryGetArray(draft.Record.File.Data,out var bytes),"Imported fixture unavailable.");
        var encoded=(native?Encoding.Unicode:Encoding.ASCII).GetBytes(new string('I',64));encoded.CopyTo(bytes.Array!,bytes.Offset+draft.Record.Offset+field.Offset);
        var original=Snapshot(document);draft=document.CreateMapGeneralDraft();var copied=document.PrepareEditorMapName(draft,draft.Values["Name"]);
        Check(!copied.NeedsPrompt&&copied.CopyName()==new string('I',63),"Source exactly64 read changed.");
        Stage(document,draft,new string('I',64));Check(document.CreateMapGeneralDraft().Values["Name"].Length==64,"Generic unrelated edit normalized imported name.");document.Undo();Exact(original);
    }
    private static void HostedGuards(bool native)
    {
        var document=New(native);using var session=document.CreateMapPropertiesSession();var working=session.WorkingDocument;var draft=working.CreateMapGeneralDraft();
        var plan=working.PrepareEditorMapName(draft,new string('H',65),session);using var policy=new EditorMapNamePolicy();
        using(var pen=document.BeginLivePenStroke(new(5,2,1)))Reject(()=>policy.Begin(plan,()=>true));
        using(var pen=working.BeginLivePenStroke(new(5,2,1)))Reject(()=>policy.Begin(plan,()=>true));
        using(var foreign=New(native).CreateMapPropertiesSession())Reject(()=>working.PrepareEditorMapName(draft,new string('F',65),foreign));
        using(var request=policy.Begin(plan,()=>true)!){document.Paint([new(2,2)],new(5,2,1));document.Undo();Check(!request.IsCurrent,"Live Undo revived private name reply.");Reject(()=>request.Copy(true));}
        using var renewed=document.CreateMapPropertiesSession();working=renewed.WorkingDocument;draft=working.CreateMapGeneralDraft();plan=working.PrepareEditorMapName(draft,new string('N',65),renewed);
        using(var request=policy.Begin(plan,()=>true)!){working.Paint([new(2,2)],new(5,2,1));working.Undo();Check(!request.IsCurrent,"Private Undo revived name reply.");Reject(()=>request.Copy(true));}
        draft=working.CreateMapGeneralDraft();plan=working.PrepareEditorMapName(draft,new string('C',65),renewed);
        using(var request=policy.Begin(plan,()=>true)!){renewed.Dispose();Check(!request.IsCurrent,"Closed parent kept name reply current.");Reject(()=>request.Copy(true));}
        Check(policy.Flags==0,"Invalid private reply remembered Copy.");
    }
    private static void Historical(string directory)
    {
        var source=New(false);var path=SaveProjectionChecks.SaveCopy(source,Path.Combine(directory,"historical-source"));var file=LegacySectionFile.Read(path);var info=file.Sections.Single(row=>row.Signature==NativeMapDocument.LegacyInfo);
        foreach(var version in Enumerable.Range(1,7))
        {
            var structure=version switch{1=>"MAPPROPERTIESV1",2 or 3=>"MAPPROPERTIESV2",4=>"MAPPROPERTIESV4",5=>"MAPPROPERTIESV5",6=>"MAPPROPERTIESV6",_=>"MAPPROPERTIES"};
            var replacement=new LegacySection(info.Signature,version,info.Payload[..LegacyProjectSchemas.Size(structure)]);var name=Path.Combine(directory,$"name-v{version}.dkm");File.WriteAllBytes(name,LegacySectionFile.Create(file.Sections.Select(row=>row==info?replacement:row)).ToArray());
            var document=NativeMapDocument.Load(name);var before=Snapshot(document);var draft=document.CreateMapGeneralDraft();var plan=document.PrepareEditorMapName(draft,new string('H',65));using var policy=new EditorMapNamePolicy();using var request=policy.Begin(plan,()=>true)!;
            Check(request.Copy(),"Historical name Copy failed.");Stage(document,draft,request.SelectedName!);document.Undo();Exact(before);
        }
    }
}
