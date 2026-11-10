using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static Dictionary<string, byte[]> OwnerState(NativeMapDocument d) => d.IsLegacyProject
        ? d.Files.ToDictionary(file => file.ExportPath, file => file.Data.ToArray())
        : d.PrepareProjectConversion().SourceFiles.ToDictionary(file => file.ExportPath, file => file.Bytes.ToArray());
    private static void ExactOwnerState(NativeMapDocument d, Dictionary<string, byte[]> expected)
    { var actual=OwnerState(d);var differences=expected.Where(pair=>!actual.TryGetValue(pair.Key,out var bytes)||!bytes.AsSpan().SequenceEqual(pair.Value)).Select(pair=>pair.Key+":"+(actual.TryGetValue(pair.Key,out var bytes)?string.Join(',',Enumerable.Range(0,Math.Min(bytes.Length,pair.Value.Length)).Where(index=>bytes[index]!=pair.Value[index]).Take(12)) : "missing"));Check(actual.Count==expected.Count && !differences.Any(),"Owner mutation/Undo changed game or private metadata buffers: "+string.Join("; ",differences)); }
    private static ScriptEdge OwnerPoint(NativeMapDocument d,ScriptOwner owner,bool empty)
    {
        var root=Create(d,owner);
        if(!empty)d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
        return d.FindScriptScope(owner)!;
    }
    private static ScriptEdge OrphanRoot(NativeMapDocument d,uint scope)=>d.ScriptBindings().Single(binding=>binding.Root.Scope==scope && binding.Owner is null && d.HasStoredScriptScope(binding.Root)).Root;
    private static void NativeOwnerScopes()
    {
        var cases=0;
        foreach(var scope in UniqueScopes)foreach(var empty in new[]{true,false})
        {
            var d=Fixture(true);var owner=Owner(d,scope);var root=OwnerPoint(d,owner,empty);var target=root.Target;var events=Events(d);
            var before=OwnerState(d);var h=d.GetHistory().Undo.Count;
            if(scope is 8 or 16)d.DeleteEditorHeroBand(scope==8?owner.Record:owner.BandRecord!);
            else Check(d.ApplyEditorObjectRemoval(d.PlanObjectRemoval([owner.Record]),includeScripted:true)==1,"Editor owner removal count");
            var orphan=OrphanRoot(d,scope);Check(orphan.Target==target && d.HasStoredScriptScope(orphan),"Native empty/nonempty orphan point lost");ExactEvents(d,events);
            Check(d.GetHistory().Undo.Count==h+1,"Owner removal not atomic");var after=OwnerState(d);d.Undo();ExactOwnerState(d,before);d.Redo();ExactOwnerState(d,after);
            // A free surviving owner receives a fresh ID; the old orphan must
            // remain independent even when the visible kind/position is reused.
            var replacement=Owner(d,scope,true);_=Create(d,replacement);
            var project=d.PrepareProjectConversion().Project;
            var retained=project.ScriptBindings().Single(binding=>binding.Root.Scope==scope && binding.Owner is null).Root;
            var live=project.ScriptBindings().Single(binding=>binding.Root.Scope==scope && binding.Owner is not null).Root;
            var field=scope is 2 or 16?"creature":scope==8?"band":scope==64?"door":"object";
            Check(retained.Owner.Read(retained.Owner.Fields.Single(f=>f.Name==field))!=live.Owner.Read(live.Owner.Fields.Single(f=>f.Name==field)),"New owner silently acquired an orphan identity");
            var path=Path.Combine(directory,$"owner-{scope}-{empty}.dkm");using(var save=new EditorSaveSession(d))Check(save.Save(save.PrepareSaveAs(path),acceptLosses:true),"Owner DKM save refused");
            var reopened=NativeMapDocument.Load(path);Check(OrphanRoot(reopened,scope).Target==target,"DKM reopen lost orphan");
            var basis=NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
            var compiled=reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78",basis);Check(OrphanRoot(compiled,scope).Target==target,"Compiler lost orphan scope");
            var game=compiled.SaveCopy(Path.Combine(directory,$"compiled-owner-{scope}-{empty}"));Check(Directory.GetFiles(Path.GetDirectoryName(game)!).Length==7,"Owner state leaked into game bundle");
            before=OwnerState(compiled);compiled.DeleteEditorScenarioPoint(OrphanRoot(compiled,scope));Check(!compiled.ScriptBindings().Any(binding=>binding.Root.Scope==scope && binding.Owner is null && compiled.HasStoredScriptScope(binding.Root)),"Orphan Delete retained point");compiled.Undo();ExactOwnerState(compiled,before);
            cases++;
        }
        OwnerReordering();ThingCrop();GenericOwnerRemoval();OwnerDuplicate();PrivateMemberRemoval();NativeReallocate();OwnerReplacement();MemberBandOperands();
        Console.WriteLine($"NATIVE OWNER SCOPE DETAIL {cases} six-family empty/nonempty delete, reserved orphan identity, full-buffer UndoRedo, DKM archive/reopen/compiler, orphan Delete and exactly seven game files; ordering, member permutations, crop, duplicate and generic contracts");
        Pass("native unique-owner working state survives every covered structural mutation");
    }
    private static void OwnerReordering()
    {
        var d=Fixture(true);var first=Owner(d,2);var last=Owner(d,2,true);var a=OwnerPoint(d,first,false).Target;var b=OwnerPoint(d,last,false).Target;
        var before=OwnerState(d);var selected=d.ObjectManagerEntries().Where(entry=>entry.Thing.Kind==PlacedThingKind.Creature).Select(entry=>entry.Record).ToArray();
        var plan=d.PlanObjectBatchModification(selected,d.ObjectTemplateForModification(selected[0]),new(ObjectModificationMask.None,Flags:(ObjectBatchFlags)127,IncludeScripted:true));
        Check(d.ApplyEditorObjectBatchModification(plan)==2,"Batch owner move was filtered or refused");Check(d.ScriptBindings().Where(binding=>binding.Root.Scope==2 && binding.Root.Target!=0).Select(binding=>binding.Root.Target).Order().SequenceEqual(new[]{a,b}.Order()),"Batch move lost branch identity");d.Undo();ExactOwnerState(d,before);
        var band=d.Records().First(NativeMapDocument.IsBand);var members=d.ScriptOwners().Where(owner=>owner.Scope==16 && owner.BandRecord?.Offset==band.Offset).OrderBy(owner=>owner.MemberIndex).ToArray();
        var r0=OwnerPoint(d,members[0],false).Target;var r1=OwnerPoint(d,members[1],false).Target;before=OwnerState(d);
        d.MoveBandMember(band,0,1);Check(d.FindScriptScope(d.ScriptOwners().Single(owner=>owner.Scope==16 && owner.BandRecord?.Offset==band.Offset && owner.MemberIndex==0))!.Target==r1,"Member move exchanged point identity");
        d.RemoveBandMember(band,0);Check(d.ScriptBindings().Single(binding=>binding.Root.Scope==16 && binding.Owner?.BandRecord?.Offset==band.Offset && binding.Owner.MemberIndex==0).Root.Target==r0,"Member removal lost shifted point");
        d.Undo();d.Undo();ExactOwnerState(d,before);
    }
    private static void ThingCrop()
    {
        foreach(var native in new[]{false,true})foreach(var scope in new uint[]{2,64,128,256})foreach(var empty in new[]{false,true})foreach(var x in new[]{0,15})
        {
            var d=Fixture(native);var owner=Owner(d,scope);var root=OwnerPoint(d,owner,empty);var events=Events(d);
            var coordinate=owner.Record.Fields.Single(field=>native?field.Name.EndsWith("LocationX",StringComparison.Ordinal):field.Name=="x");d.SetField(owner.Record,coordinate,x.ToString());
            var before=OwnerState(d);using var session=d.CreateMapPropertiesSession();var plan=d.PrepareEditorMapProperties(session,new(14,14,RemoveOutside:true));ExactOwnerState(d,before);
            d.ApplyEditorMapProperties(plan,croppingAccepted:true);Check(OrphanRoot(d,scope).Target==root.Target,"Thing crop lost its scenario point");ExactEvents(d,events);d.Undo();ExactOwnerState(d,before);
        }
    }
    private static void GenericOwnerRemoval()
    {
        foreach(var native in new[]{false,true})
        {
            var d=Fixture(native);var owner=Owner(d,2);_=OwnerPoint(d,owner,false);var events=Events(d);var before=OwnerState(d);
            d.ApplyObjectRemoval(d.PlanObjectRemoval([owner.Record]),includeScripted:true);Check(!d.ScriptBindings().Any(binding=>binding.Root.Scope==2 && binding.Owner is null),"Generic removal acquired editor retention policy");ExactEvents(d,events);d.Undo();ExactOwnerState(d,before);
        }
    }
    private static void OwnerDuplicate()
    {
        foreach(var scope in UniqueScopes)foreach(var empty in new[]{false,true})
        {
            var d=Fixture(true);var owner=Owner(d,scope);var root=OwnerPoint(d,owner,empty);var before=OwnerState(d);
            var clone=d.DuplicateRecord(scope==16?owner.BandRecord!:owner.Record);var copied=d.ScriptOwners().Single(candidate=>candidate.Scope==scope && (scope==16?candidate.BandRecord?.Offset==clone.Offset && candidate.MemberIndex==owner.MemberIndex:candidate.Record.Offset==clone.Offset));
            var cloned=d.FindScriptScope(copied)!;
            Check(d.HasStoredScriptScope(cloned) && (empty?cloned.Target==0:cloned.Target!=0 && cloned.Target!=root.Target),"Duplicate lost explicit empty point or independent cloned graph");
            var bindings=d.PrepareProjectConversion().Project.ScriptBindings().Where(binding=>binding.Root.Scope==scope && binding.Owner is not null).ToArray();
            Check(bindings.Length==2,"Duplicate did not retain independent physical owners");d.Undo();ExactOwnerState(d,before);
        }
    }
    private static void PrivateMemberRemoval()
    {
        foreach(var empty in new[]{false,true})
        {
            var d=Fixture(true);var band=d.Records().First(NativeMapDocument.IsBand);
            var members=d.ScriptOwners().Where(owner=>owner.Scope==16 && owner.BandRecord?.Offset==band.Offset).OrderBy(owner=>owner.MemberIndex).ToArray();
            Set(d,members[1].Record,"bGlobalBehavior","47");var root=OwnerPoint(d,members[0],empty);var survivor=OwnerPoint(d,members[1],false).Target;var before=OwnerState(d);
            using(var cancel=d.CreateEditorHeroBandSession(band)){Check(cancel.Members[0].HasScenario,"Private band hid empty/member scope");cancel.Remove([0]);_=d.PrepareEditorHeroBand(cancel,"Band");ExactOwnerState(d,before);}
            using var session=d.CreateEditorHeroBandSession(band);session.Remove([0]);var plan=d.PrepareEditorHeroBand(session,"Band");ExactOwnerState(d,before);d.ApplyEditorHeroBand(plan);
            Check(OrphanRoot(d,16).Target==root.Target && d.FindScriptScope(d.ScriptOwners().Single(owner=>owner.Scope==16 && owner.BandRecord?.Offset==band.Offset && owner.MemberIndex==0))!.Target==survivor,"Private parent lost removed or shifted member point");d.Undo();ExactOwnerState(d,before);
        }
    }
    private static void NativeReallocate()
    {
        foreach(var scope in UniqueScopes)
        {
            var project=Fixture(false);var owner=Owner(project,scope);var root=OwnerPoint(project,owner,false);
            Set(project,owner.Record,Identity(owner),"37");
            var basis=NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");var d=project.CompileLegacyProject("Reallocate",basis);
            var native=d.ScriptBindings().Single(binding=>binding.Root.Scope==scope && binding.Root.Target==root.Target);Check(native.Owner is not null,"Compiled source point owner");var before=OwnerState(d);
            using var session=d.CreateEditorScenarioPointSession(native.Root);session.SelectOwner(native.Owner!);d.ApplyEditorScenarioPoint(d.PrepareEditorScenarioPoint(session));
            var converted=d.PrepareProjectConversion().Project;var moved=converted.ScriptBindings().Single(binding=>binding.Root.Scope==scope && binding.Root.Target!=0);
            Check(moved.Owner!.Record.Read(moved.Owner.Record.Fields.Single(field=>field.Name==Identity(moved.Owner)))=="1","Native same-owner Modify did not free/reallocate lowest safe ID");d.Undo();ExactOwnerState(d,before);
        }
    }
    private static void OwnerReplacement()
    {
        foreach(var native in new[]{false,true})foreach(var editor in new[]{false,true})
        {
            var d=Fixture(native);var owner=Owner(d,128);var root=OwnerPoint(d,owner,false);var events=Events(d);var before=OwnerState(d);
            var plan=d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Object),new(Position:new(2,4),Flags:(ObjectBatchFlags)127,Replacement:ObjectReplacementPolicy.All));
            Check(plan.Placement.Candidates.Single().Replacement?.Record.Offset==owner.Record.Offset,"Replacement fixture did not select the scripted owner");
            var applied=editor?d.ApplyEditorObjectBatchInsertion(plan,[0]):d.ApplyObjectBatchInsertion(plan,[0]);Check(applied==1,"Owner replacement was filtered");
            if(editor)Check(OrphanRoot(d,128).Target==root.Target,"Source replacement lost the old point");
            else Check(!d.ScriptBindings().Any(binding=>binding.Root.Scope==128 && binding.Owner is null && d.HasStoredScriptScope(binding.Root)),"Generic replacement acquired source orphan retention");
            ExactEvents(d,events);d.Undo();ExactOwnerState(d,before);
        }
    }
}
