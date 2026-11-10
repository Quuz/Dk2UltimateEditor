using System.Buffers.Binary;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class WindowMapTitleChecks
{
    public static void Run()
    {
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        foreach(var native in new[]{false,true})
        {
            var d=NativeMapDocument.CreateNewMap(new("DifferentFileKey","Displayed dungeon é",16,14,NativeFormat:native));
            var before=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=d.EditRevision;var history=d.GetHistory();
            Check(EditorWindowCaption.ReadMapName(d)=="Displayed dungeon é","Caption used the filename or wrong metadata section.");
            Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&revision==d.EditRevision
                &&history.Undo.Count==d.GetHistory().Undo.Count&&history.Redo.Count==d.GetHistory().Redo.Count,"Read mutated bytes/revision/history.");
            var draft=d.CreateMapGeneralDraft();
            d.ApplyMapGeneralSettings(d.UpdateMapGeneralSettings(draft,draft.Values.Select(pair=>new MapGeneralInput(pair.Key,pair.Key=="Name"?"Renamed dungeon":pair.Value))));
            Check(EditorWindowCaption.ReadMapName(d)=="Renamed dungeon","Read cached old metadata after Rename.");
            d.Undo();Check(EditorWindowCaption.ReadMapName(d)=="Displayed dungeon é","Read cached old metadata after Undo.");
            Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Rename Undo did not restore fixture bytes.");
        }
        var nativeBytes=NativeMapDocument.Create(16,14,"NativeHeader").MainFile.Data.ToArray();
        var nameField=LegacySchemas.Field("GAMEINFO","wstrMainName");
        nameField.Encode("Крепость").CopyTo(nativeBytes,nameField.Offset);
        Check(EditorWindowCaption.ReadNativeMapName(nativeBytes)=="Крепость","Native Unicode title failed.");
        Check(EditorWindowCaption.ReadNativeMapName(nativeBytes.AsSpan(0,nameField.Offset+nameField.Size-1)) is null,"Truncated native title was accepted.");
        var unknown=nativeBytes.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(unknown.AsSpan(4),5);
        Check(EditorWindowCaption.ReadNativeMapName(unknown) is null,"Unknown native version title was inferred.");
        var noNameExtent=nativeBytes.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(noNameExtent.AsSpan(52),0);
        Check(EditorWindowCaption.ReadNativeMapName(noNameExtent) is null,"Native name outside declared parameter block was read.");
        var unrelated=nativeBytes.ToArray();unrelated.AsSpan(nameField.Offset+nameField.Size).Fill(0xFF);
        Check(EditorWindowCaption.ReadNativeMapName(unrelated)=="Крепость","Title depended on unrelated metadata/records.");
        foreach(var version in Enumerable.Range(1,7))
        {
            var structure=version switch{1=>"MAPPROPERTIESV1",2 or 3=>"MAPPROPERTIESV2",4=>"MAPPROPERTIESV4",5=>"MAPPROPERTIESV5",6=>"MAPPROPERTIESV6",_=>"MAPPROPERTIES"};
            var bytes=new byte[12+LegacyProjectSchemas.Size(structure)];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes,NativeMapDocument.LegacyInfo);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),(uint)version);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),(uint)bytes.Length-12);
            var field=LegacyProjectSchemas.Fields(structure).Single(f=>f.Name=="Name");
            field.Encode("Historical café").CopyTo(bytes,12+field.Offset);
            Check(EditorWindowCaption.ReadProjectMapName(bytes)=="Historical café","Wrong historical Name field: "+version);
            Check(EditorWindowCaption.ReadProjectMapName(bytes.AsSpan(0,bytes.Length-1)) is null,"Truncated historical layout was accepted.");
            var wrongLength=bytes.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(wrongLength.AsSpan(8),0);
            Check(EditorWindowCaption.ReadProjectMapName(wrongLength) is null,"Wrong declared KINF payload was accepted.");
            var wrongVersion=bytes.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(wrongVersion.AsSpan(4),8);
            Check(EditorWindowCaption.ReadProjectMapName(wrongVersion) is null,"Future project layout was inferred.");
            bytes.AsSpan(12+field.Offset,field.Size).Fill((byte)'A');
            Check(EditorWindowCaption.ReadProjectMapName(bytes)==new string('A',64),"Full-width imported name was trimmed.");
            bytes[12+field.Offset]=0;
            Check(EditorWindowCaption.ReadProjectMapName(bytes)=="","Empty imported name was replaced by an invented value.");
        }
        Check(EditorWindowCaption.ReadNativeMapName([]) is null&&EditorWindowCaption.ReadProjectMapName([]) is null,"Short header threw or inferred a title.");
        Console.WriteLine("WINDOW TITLE DETAIL live native/project Rename and Undo, read-only bytes/history, Unicode and 7 historical layouts, full-width/empty names, malformed/future bounds; no UI");
    }
}
