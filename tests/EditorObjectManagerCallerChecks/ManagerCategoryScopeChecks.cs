using DK2MapEditor.Core.Editing;

internal static class ManagerCategoryScopeChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static Dictionary<string,byte[]> State(NativeMapDocument d)=>d.IsLegacyProject?d.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray()):d.PrepareProjectConversion().SourceFiles.ToDictionary(file=>file.ExportPath,file=>file.Bytes.ToArray());
    private static void Exact(NativeMapDocument d,Dictionary<string,byte[]> expected){var actual=State(d);Check(actual.Count==expected.Count && expected.All(pair=>actual.TryGetValue(pair.Key,out var bytes)&&bytes.AsSpan().SequenceEqual(pair.Value)),"Category mutation/Undo changed full game or working metadata buffers");}
    private static Dictionary<string,byte[]> Events(NativeMapDocument d)=>d.Files.Where(file=>d.IsLegacyProject?file.Signature==NativeMapDocument.LegacyEvents:file.Signature==0xD2).ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray());
    private static void ExactEvents(NativeMapDocument d,Dictionary<string,byte[]> expected){var actual=Events(d);Check(actual.Count==expected.Count && expected.All(pair=>actual[pair.Key].AsSpan().SequenceEqual(pair.Value)),"Source category change rewrote a point, event, operand, order or unknown byte");}
    private static uint Scope(PlacedThingKind kind)=>kind switch{PlacedThingKind.Door=>64,PlacedThingKind.Object=>128,PlacedThingKind.SpecialItem=>256,_=>0};
    private static ObjectManagerEntry At(NativeMapDocument d,int x)=>d.ObjectManagerEntries().Single(entry=>entry.Thing.Position==new TilePoint(x,5));
    private static ScriptBinding Stored(NativeMapDocument d,uint scope)=>d.ScriptBindings().Single(binding=>binding.Root.Scope==scope && d.HasStoredScriptScope(binding.Root));
    private static int Change(NativeMapDocument d,NativeRecord[] selected,PlacedThingKind kind)
    {
        using var policy=new EditorThingRequestPolicy();using var request=policy.Begin(d,selected,EditorThingCommand.Modify);request.Accept();
        var plan=d.PlanEditorObjectBatchModification(request,ObjectBatchTemplate.Default(kind),new(ObjectModificationMask.Type,Flags:(ObjectBatchFlags)127));
        Check(plan.Candidates.All(candidate=>!candidate.DetachesScript && !candidate.Locked),"Source plan retained generic scenario detachment consent");
        return d.ApplyEditorObjectBatchModification(plan);
    }
    public static void Run(string directory)
    {
        var count=0;
        foreach(var native in new[]{false,true})foreach(var empty in new[]{false,true})foreach(var multiple in new[]{false,true})
            foreach(var (from,to) in new[]{(PlacedThingKind.Door,PlacedThingKind.Trap),(PlacedThingKind.Object,PlacedThingKind.SpecialItem),(PlacedThingKind.SpecialItem,PlacedThingKind.Object)})
            {Matrix(native,empty,multiple,from,to,directory);count++;}
        CreatureOwners();IdentityOnlyTrap();TrapMetadataGuards();UnrelatedPointNotReconnected();
        Console.WriteLine($"MANAGER CATEGORY SCOPE DETAIL {count} native/project x empty/nonempty x single/multiple x three category transitions and returns; literal source point/event bytes and UID, orphan/reconnection, trap identity D2OSv2, per-owner allocation, private plans, exact full-buffer UndoRedo/counters, DKM/archive/reopen/compiler/seven-file export; generic guards retained separately");
    }
    private static void Matrix(bool native,bool empty,bool multiple,PlacedThingKind from,PlacedThingKind to,string directory)
    {
        var d=NativeMapDocument.CreateNewMap(new("CategoryScope","CategoryScope",20,20,NativeFormat:native));
        foreach(var x in new[]{4,8})d.ApplyEditorObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(from),new(Position:new(x,5),Flags:(ObjectBatchFlags)127)),[0]);
        var entry=At(d,4);var owner=d.ScriptOwners().Single(owner=>owner.Record.File==entry.Record.File && owner.Record.Offset==entry.Record.Offset);var root=d.EnsureScriptScope(owner);
        if(!empty)d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
        root=d.FindScriptScope(owner)!;
        var id=At(d,4).Identity;var scope=Scope(from);var before=State(d);var events=Events(d);var h=d.GetHistory();var revision=d.EditRevision;
        var selected=(multiple?new[]{At(d,8).Record,At(d,4).Record}:new[]{At(d,4).Record});
        Check(Change(d,selected,to)==selected.Length,"Category apply count");ExactEvents(d,events);
        var converted=At(d,4);Check(converted.Thing.Kind==to && converted.Identity==id && !converted.Scripted,"Category migration lost UID or attached old-scope point to new owner");
        Check(Stored(d,scope).Owner is null && Stored(d,scope).Root.Target==root.Target,"Original scope did not survive as an orphan");
        Check(d.GetHistory().Undo.Count==h.Undo.Count+1 && d.EditorChangeCount==h.EditorChangeCount+selected.Length && d.EditRevision==revision+1,"Source manager parent/counters");
        var after=State(d);d.Undo();Exact(d,before);d.Redo();Exact(d,after);
        var path=Path.Combine(directory,$"category-{native}-{empty}-{multiple}-{from}.dkm");using(var save=new EditorSaveSession(d))Check(save.Save(save.PrepareSaveAs(path),acceptLosses:true),"Source category DKM Save refused");
        var reopened=NativeMapDocument.Load(path);Check(At(reopened,4).Identity==id && Stored(reopened,scope).Owner is null,"DKM/archive reopen changed UID or old-scope point");
        var basis=NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");var compiled=reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78",basis);
        Check(At(compiled,4).Identity==id && Stored(compiled,scope).Owner is null,"Compiler retargeted old point or discarded trap UID");
        var output=compiled.SaveCopy(Path.Combine(directory,$"category-native-{native}-{empty}-{multiple}-{from}"));Check(Directory.GetFiles(Path.GetDirectoryName(output)!).Length==7,"Category metadata changed seven-file game layout");
        // Unrelated native edits/synchronization while a trap retains only UID.
        compiled.Paint([new(12,12)],new(8,3,1));Check(At(compiled,4).Identity==id,"Synchronization discarded identity-only owner");
        var compiledBefore=State(compiled);var compiledEvents=Events(compiled);var returning=multiple?new[]{At(compiled,8).Record,At(compiled,4).Record}:new[]{At(compiled,4).Record};
        Check(Change(compiled,returning,from)==returning.Length,"Return source transition count");ExactEvents(compiled,compiledEvents);
        Check(At(compiled,4).Identity==id && At(compiled,4).Scripted && Stored(compiled,scope).Owner is not null,"Returning category failed to reconnect the literal old point by retained UID");
        compiled.Undo();Exact(compiled,compiledBefore);compiled.Redo();
        // The project route must likewise reconnect without rewriting Events.
        var reloadedEvents=Events(reopened);Check(Change(reopened,multiple?new[]{At(reopened,8).Record,At(reopened,4).Record}:new[]{At(reopened,4).Record},from)==returning.Length,"Project return count");ExactEvents(reopened,reloadedEvents);Check(At(reopened,4).Scripted,"DKM source return failed to recover old scope");
    }
    private static void CreatureOwners()
    {
        foreach(var native in new[]{false,true})foreach(var owner in new byte[]{1,2,7})
        {
            var d=NativeMapDocument.CreateNewMap(new("SignatureScope","SignatureScope",16,16,NativeFormat:native));d.ApplyEditorObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Creature),new(Position:new(4,5),Flags:(ObjectBatchFlags)127)),[0]);
            var thing=At(d,4);var point=d.EnsureScriptScope(d.ScriptOwners().Single(o=>o.Scope==2));d.InsertScriptEvent(point.Owner,point.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));var before=State(d);var events=Events(d);
            using var policy=new EditorThingRequestPolicy();using var request=policy.Begin(d,[thing.Record],EditorThingCommand.Modify);request.Accept();
            var plan=d.PlanEditorObjectBatchModification(request,ObjectBatchTemplate.Default(PlacedThingKind.Creature) with{Owner=owner},new(ObjectModificationMask.Owner,Flags:(ObjectBatchFlags)127));d.ApplyEditorObjectBatchModification(plan);ExactEvents(d,events);
            Check(At(d,4).Thing.Owner==owner && At(d,4).Scripted && Stored(d,2).Owner is not null,"Source creature signature migration orphaned same-scope point");d.Undo();Exact(d,before);
        }
    }
    private static void IdentityOnlyTrap()
    {
        var d=NativeMapDocument.CreateNewMap(new("TrapUID","TrapUID",16,16,NativeFormat:true));d.ApplyEditorObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Door),new(Position:new(4,5),Flags:(ObjectBatchFlags)127)),[0]);
        d.EnsureScriptScope(d.ScriptOwners().Single(owner=>owner.Scope==64));var id=At(d,4).Identity;Change(d,[At(d,4).Record],PlacedThingKind.Trap);
        var before=State(d);Change(d,[At(d,4).Record],PlacedThingKind.Trap);Check(At(d,4).Identity==id && Stored(d,64).Owner is null,"Equal trap manager rewrite lost UID/old point");d.Undo();Exact(d,before);
        // A new point for another owner must reserve both live and orphan IDs.
        d.ApplyEditorObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Door),new(Position:new(8,5),Flags:(ObjectBatchFlags)127)),[0]);d.EnsureScriptScope(d.ScriptOwners().Single(owner=>owner.Scope==64));Check(At(d,8).Identity!=id,"New owner acquired retained former-door identity");
    }
    private static void TrapMetadataGuards()
    {
        var d=NativeMapDocument.CreateNewMap(new("MetadataScope","MetadataScope",16,16,NativeFormat:true));d.ApplyEditorObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Door),new(Position:new(4,5),Flags:(ObjectBatchFlags)127)),[0]);d.EnsureScriptScope(d.ScriptOwners().Single(owner=>owner.Scope==64));Change(d,[At(d,4).Record],PlacedThingKind.Trap);
        d.ApplyEditorObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Object),new(Position:new(8,5),Flags:(ObjectBatchFlags)127)),[0]);
        var table=(NativeFile)typeof(NativeMapDocument).GetField("nativeOwnerScopes",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(d)!;
        Check(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(table.Data,out var memory),"Working metadata fixture buffer");var bytes=memory.Array!;var start=memory.Offset;var original=table.Data.ToArray();
        uint Read(int offset)=>System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(table.Data.Span.Slice(offset,4));
        Check(Read(4)==2,"Identity-only trap must use D2OSv2");var row=Enumerable.Range(0,(int)Read(12)).Select(index=>16+index*36).Single(offset=>Read(offset+12)==0);
        foreach(var (offset,value) in new[]{(4,1u),(4,3u),(row+28,1u),(row+24,1u),(row,0xFFFFFFFFu),(row+20,7u),(row+32,1u),(row+4,1u)})
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(start+offset),value);var refused=false;
            try{_=d.PrepareProjectConversion();}catch(InvalidDataException){refused=true;}
            finally{original.CopyTo(bytes,start);}
            Check(refused,"Invalid identity-only owner row/version was silently accepted");
        }
        Check(At(d,4).Identity!=0 && Stored(d,64).Owner is null,"Metadata refusal corrupted retained identity/point");
    }
    private static void UnrelatedPointNotReconnected()
    {
        var d=NativeMapDocument.CreateNewMap(new("UnrelatedScope","UnrelatedScope",16,16,NativeFormat:true));
        foreach(var x in new[]{4,8})d.ApplyEditorObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Object),new(Position:new(x,5),Flags:(ObjectBatchFlags)127)),[0]);
        var first=At(d,4);d.EnsureScriptScope(d.ScriptOwners().Single(owner=>owner.Record.Offset==first.Record.Offset && owner.Record.File==first.Record.File));Change(d,[first.Record],PlacedThingKind.SpecialItem);
        // Imported/raw category change does not have source command authority.
        // Rewriting another owner in the same BE file must not infer a repair.
        first=At(d,4);var field=first.Record.Fields.Single(field=>field.Name=="bObjectID");Check(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(first.Record.File.Data,out var raw),"Raw imported category fixture");field.Encode("1").CopyTo(raw.Array!,raw.Offset+first.Record.Offset+field.Offset);
        var before=State(d);Check(Stored(d,128).Owner is null,"Orphan fixture unexpectedly repaired");Change(d,[At(d,8).Record],PlacedThingKind.Object);
        Check(Stored(d,128).Owner is null && !At(d,4).Scripted,"Unrelated BE rewrite reconnected an imported orphan without a category transition");d.Undo();Exact(d,before);
    }
}
