using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class RegionPropertiesSessionChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or OverflowException){return;}throw new InvalidOperationException("Invalid region-property operation accepted.");}
    private static void Raw(NativeRecord record,int offset,byte value)
    {Expect(MemoryMarshal.TryGetArray(record.File.Data,out var array),"Fixture memory unavailable.");array.Array![array.Offset+record.Offset+offset]=value;}
    public static void Run()
    {
        NativeGateMutationChecks.Run();
        NativeGateBindingsChecks.Run();
        NativeGateIdentityChecks.Run();
        ProjectGateRemovalChecks.Run();
        RegionRemovalConfirmationChecks.Run();
        RegionCropScopeChecks.Run();
        NativeCrystalMutationChecks.Run();
        var directory=Path.Combine(Path.GetTempPath(),"RegionProperties-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            foreach(var project in new[]{false,true})foreach(var kind in Enum.GetValues<MapRegionKind>())
            {
                var document=project?NativeMapDocument.CreateNewMap(new("Region","Region",16,16)):NativeMapDocument.Create(16,16,"Region");
                var record=document.AddMapRegion(kind,new(2,3,kind==MapRegionKind.Gate?1:3,kind==MapRegionKind.Gate?1:4));
                if(kind==MapRegionKind.Area)document.SetField(record,record.Fields.Single(field=>field.Name==(project?"flags":"bAreaFlags")),"129");
                var padding=project?80:kind switch{MapRegionKind.Area=>27,MapRegionKind.Gate=>16,_=>38};Raw(record,padding,0xE9);
                if(project)Raw(record,105,0xE9); // Nonzero bytes after the imported name's NUL.
                var original=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
                void Exact(Dictionary<NativeFile,byte[]> snapshot)=>Expect(snapshot.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Region properties changed bytes outside literal approved fields.");
                var originalCount=document.EditorChangeCount;var history=document.GetHistory();var revision=document.EditRevision;var dirty=document.HasChanges;
                var changes=0;EventHandler handler=(_,_)=>changes++;document.Changed+=handler;
                try
                {
                    using(var cancel=document.CreateRegionPropertiesSession(record))
                    {
                        var value=cancel.Values with{Bounds=new(4,5,kind==MapRegionKind.Gate?1:2,kind==MapRegionKind.Gate?1:3)};
                        if(kind==MapRegionKind.Area)value=value with{AreaFlags=161};
                        if(kind==MapRegionKind.Effect)value=value with{Frequency=9};
                        document.UpdateRegionProperties(cancel,value);Expect(document.PrepareRegionProperties(cancel).Changed,"Private draft has no changes.");Exact(original);
                    }
                    Expect(changes==0&&document.EditRevision==revision&&document.HasChanges==dirty&&document.EditorChangeCount==originalCount
                        &&document.GetHistory().Undo.Count==history.Undo.Count,"Cancel changed live map/history/counter.");
                    using(var equal=document.CreateRegionPropertiesSession(record))
                    {var plan=document.PrepareRegionProperties(equal);Expect(!plan.Changed,"Equal draft changed padding.");document.ApplyEditorRegionProperties(plan);Reject(()=>document.ApplyEditorRegionProperties(plan));}
                    Exact(original);Expect(changes==1&&document.EditorChangeCount==unchecked(originalCount+1)&&document.GetHistory().Undo.Count==history.Undo.Count+1,"Equal accepted region did not retain one source Undo/count.");
                    document.Undo();Exact(original);Expect(document.EditorChangeCount==originalCount,"Equal Undo did not restore count.");
                    document.Redo();Exact(original);document.Undo();

                    using var session=document.CreateRegionPropertiesSession(record);
                    var updated=session.Values with{Bounds=new(4,5,kind==MapRegionKind.Gate?1:2,kind==MapRegionKind.Gate?1:3)};
                    if(project)updated=updated with{Name="Region Cafe",Information="Description",Colour=0x123456,TextColour=0xABCDEF};
                    if(kind==MapRegionKind.Area)updated=updated with{AreaFlags=161};
                    if(kind==MapRegionKind.Effect)updated=updated with{Effect1=int.Parse(EffectCatalogue.Choices.First(choice=>choice.Value!="0").Value),Frequency=9};
                    document.UpdateRegionProperties(session,updated);var firstPlan=document.PrepareRegionProperties(session);
                    document.UpdateRegionProperties(session,updated);Reject(()=>document.ApplyEditorRegionProperties(firstPlan));Exact(original);
                    var expected=original.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToArray());var bytes=expected[record.File];
                    void U(int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(record.Offset+offset),value);
                    void W(int offset,ushort value)=>BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(record.Offset+offset),value);
                    if(project)
                    {
                        U(12,4);U(16,5);U(20,kind==MapRegionKind.Gate?4u:5u);U(24,kind==MapRegionKind.Gate?5u:7u);U(32,0x123456);U(36,0xABCDEF);
                        new NativeField("ExpectedName",0,64,NativeValueKind.WesternAnsi).Encode(updated.Name).CopyTo(bytes,record.Offset+100);
                        new NativeField("ExpectedInfo",0,128,NativeValueKind.WesternAnsi).Encode(updated.Information).CopyTo(bytes,record.Offset+164);
                        if(kind==MapRegionKind.Area)U(48,161);if(kind==MapRegionKind.Effect){U(60,(uint)updated.Effect1);U(76,9);}
                    }
                    else
                    {
                        U(8,4);U(12,5);
                        if(kind==MapRegionKind.Area){U(16,5);U(20,7);bytes[record.Offset+26]=161;}
                        if(kind==MapRegionKind.Effect){W(24,1);W(26,2);W(28,(ushort)updated.Effect1);bytes[record.Offset+36]=9;}
                    }
                    var notify=changes;document.ApplyEditorRegionProperties(document.PrepareRegionProperties(session));Exact(expected);
                    Expect(changes==notify+1&&document.EditorChangeCount==unchecked(originalCount+1),"Atomic apply had wrong notifications/count.");
                    document.Undo();Exact(original);document.Redo();Exact(expected);document.Undo();Exact(original);

                    using(var invalid=document.CreateRegionPropertiesSession(record))
                    {
                        Reject(()=>document.UpdateRegionProperties(invalid,invalid.Values with{Bounds=new(-1,0,1,1)}));
                        if(kind==MapRegionKind.Gate)Reject(()=>document.UpdateRegionProperties(invalid,invalid.Values with{Bounds=new(1,1,2,2)}));
                        if(kind==MapRegionKind.Area)Reject(()=>document.UpdateRegionProperties(invalid,invalid.Values with{AreaFlags=invalid.Values.AreaFlags^0x200}));
                        if(kind==MapRegionKind.Effect){Reject(()=>document.UpdateRegionProperties(invalid,invalid.Values with{Frequency=256}));Reject(()=>document.UpdateRegionProperties(invalid,invalid.Values with{Effect4=int.MaxValue}));}
                        if(project){Reject(()=>document.UpdateRegionProperties(invalid,invalid.Values with{Name="Кириллица"}));Reject(()=>document.UpdateRegionProperties(invalid,invalid.Values with{Name=new string('X',64)}));}
                        else Reject(()=>document.UpdateRegionProperties(invalid,invalid.Values with{Name="unsupported"}));
                        Exact(original);document.Paint([new(8,8)],new(4,2,1));Reject(()=>document.PrepareRegionProperties(invalid));document.Undo();Reject(()=>document.PrepareRegionProperties(invalid));
                    }
                    using(var mutated=document.CreateRegionPropertiesSession(record))
                    {Raw(record,padding,0xC3);Reject(()=>document.PrepareRegionProperties(mutated));Raw(record,padding,0xE9);}
                    using(var foreign=document.CreateRegionPropertiesSession(record))
                    {var other=NativeMapDocument.Create(16,16,"Other");Reject(()=>other.PrepareRegionProperties(foreign));}
                    Exact(original);
                    using(var save=document.CreateRegionPropertiesSession(record))
                    {document.UpdateRegionProperties(save,updated);document.ApplyEditorRegionProperties(document.PrepareRegionProperties(save));}
                    var path=document.SaveCopy(Path.Combine(directory,project+"-"+kind));var reopened=NativeMapDocument.Load(path);
                    var loaded=reopened.MapRegions().Single(region=>region.Kind==kind);
                    using var opened=reopened.CreateRegionPropertiesSession(loaded.Record);
                    Expect(opened.Values.Bounds==updated.Bounds&&opened.Values.Name==updated.Name&&opened.Values.Frequency==updated.Frequency,"Region property save/reopen lost composed fields.");
                }
                finally{document.Changed-=handler;}
            }
        }
        finally{Directory.Delete(directory,true);}
    }
}
