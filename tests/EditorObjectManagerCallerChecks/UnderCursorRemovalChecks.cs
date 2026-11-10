using System.Reflection;
using DK2MapEditor.Core.Editing;

internal static class UnderCursorRemovalChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action){try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException){return;}throw new InvalidOperationException("Unsafe cursor removal accepted.");}
    private static Dictionary<string,byte[]> State(NativeMapDocument d)=>d.IsLegacyProject?d.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray()):d.PrepareProjectConversion().SourceFiles.ToDictionary(file=>file.ExportPath,file=>file.Bytes.ToArray());
    private static void Exact(NativeMapDocument d,Dictionary<string,byte[]> expected){var actual=State(d);Check(actual.Count==expected.Count && expected.All(pair=>actual.TryGetValue(pair.Key,out var bytes)&&bytes.AsSpan().SequenceEqual(pair.Value)),"Cursor command/Undo changed unexpected game or metadata buffers");}
    private static NativeRecord Add(NativeMapDocument d,PlacedThingKind kind,TilePoint position)
    {
        var name=kind==PlacedThingKind.SpecialItem?"MagicalObject":kind.ToString();
        var row=d.IsLegacyProject?d.AddEditorLegacyRecord(name,position):d.AddRecord(RecordTemplate.All.Single(template=>template.Structure==(kind switch{PlacedThingKind.Creature=>"GAMEKEEPER",PlacedThingKind.Door=>"GAMEDOOR",PlacedThingKind.Trap=>"GAMETRAP",_=>"GAMEOBJECT"})),position);
        if(kind==PlacedThingKind.SpecialItem){var field=row.Fields.Single(field=>field.Name==(d.IsLegacyProject?"id":"bObjectID"));d.SetField(row,field,"117");}
        return row;
    }
    private static NativeMapDocument New(bool native)=>NativeMapDocument.CreateNewMap(new("CursorRemoval","CursorRemoval",16,16,NativeFormat:native));
    private static ScriptOwner Owner(NativeMapDocument d,NativeRecord row)=>d.ScriptOwners().Single(owner=>owner.Record.File==row.File && owner.Record.Offset==row.Offset);
    private static NativeMapDocument Used(bool native,bool empty)
    {
        var d=New(native);var first=Add(d,PlacedThingKind.Creature,new(4,5));var second=Add(d,PlacedThingKind.Creature,new(4,5));_=Add(d,PlacedThingKind.Creature,new(4,5));_=Add(d,PlacedThingKind.Creature,new(9,9));
        var root=d.EnsureScriptScope(Owner(d,first));if(!empty)d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
        var allocated=d.EnsureScriptScope(Owner(d,second));d.RemoveScriptScope(allocated);return d;
    }
    public static void Run(string directory)
    {
        CrystalOrder();
        var cases=0;foreach(var native in new[]{false,true})foreach(var mask in Enumerable.Range(0,8))foreach(var preferred in Enum.GetValues<UnderCursorFamily>()){Priority(native,mask,preferred);cases++;}
        foreach(var native in new[]{false,true})
        {
            foreach(var empty in new[]{false,true})foreach(var reply in new[]{0,1,2})Replies(native,empty,reply);
            BoundsAndGuards(native);DepthAndOverflow(native);Observer(native);Shared(native);Save(native,directory);
        }
        Check(NativeMapDocument.UnderCursorPreference(false,false)==UnderCursorFamily.Creatures && NativeMapDocument.UnderCursorPreference(true,false)==UnderCursorFamily.DoorsAndTraps && NativeMapDocument.UnderCursorPreference(false,true)==UnderCursorFamily.ObjectsAndSpecials && NativeMapDocument.UnderCursorPreference(true,true) is null,"Fixed keyboard modifiers differ from source");
        Console.WriteLine($"UNDER CURSOR REMOVAL DETAIL {cases} native/project x all eight occupancy masks x three preferences; family fallback/absence, every stacked item, door-before-trap/object-before-special order, shared used requests, empty/nonempty YesNoCancel, no-op/No-all no Undo, per-record Undo/count, complete metadata UndoRedo, zero/depth1 history, uint wrap/long overflow, stale/pen/nested guards, postcommit observers, DKM/compiler/seven-file export");
    }
    private static void Priority(bool native,int mask,UnderCursorFamily preferred)
    {
        var d=New(native);foreach(var kind in new[]{PlacedThingKind.SpecialItem,PlacedThingKind.Trap,PlacedThingKind.Creature,PlacedThingKind.Object,PlacedThingKind.Door})
        {
            var bit=kind==PlacedThingKind.Creature?1:kind is PlacedThingKind.Door or PlacedThingKind.Trap?2:4;
            if((mask&bit)!=0)for(var copy=0;copy<2;copy++)Add(d,kind,new(4,5));
        }
        _=Add(d,PlacedThingKind.Creature,new(9,9));var h=d.GetHistory();var before=State(d);var revision=d.EditRevision;
        var bits=new[]{1,2,4}.Where(bit=>(mask&bit)!=0).ToArray();var selectedBit=bits.Length==1?bits[0]:1<<(int)preferred;
        var expected=(mask&selectedBit)==0?0:selectedBit==1?2:4;var plan=d.PlanUnderCursorRemoval(new(4,5),preferred);Exact(d,before);
        Check(plan.Records.Count==expected && plan.IsCurrent,"Under-cursor single-family fallback/preferred absence differs from source");
        var kinds=plan.Records.Select(row=>d.ObjectManagerEntries().Single(entry=>entry.Record.File==row.File && entry.Record.Offset==row.Offset).Thing.Kind).ToArray();
        if(expected==4)Check(kinds.SequenceEqual(selectedBit==2?new[]{PlacedThingKind.Door,PlacedThingKind.Door,PlacedThingKind.Trap,PlacedThingKind.Trap}:new[]{PlacedThingKind.Object,PlacedThingKind.Object,PlacedThingKind.SpecialItem,PlacedThingKind.SpecialItem}),"Source family list order lost to mixed native byte order");
        using var policy=new EditorThingRequestPolicy();using var request=policy.BeginUnderCursor(plan);
        if(expected==0){Check(request is null && d.EditRevision==revision,"Empty cursor command created a request or edit");return;}
        Check(request is not null && !request.RequiresPrompt,"Unused cursor removal acquired manager confirmation");var changes=0;var historyChanges=0;d.Changed+=(_,_)=>changes++;d.HistoryChanged+=(_,_)=>historyChanges++;
        Check(request!.Accept()==expected && policy.Flags==0 && changes==1 && historyChanges==1,"Cursor removal did not publish one complete refresh");
        Check(d.GetHistory().Undo.Count==h.Undo.Count+expected && d.EditorChangeCount==h.EditorChangeCount+expected && d.EditRevision==revision+expected,"Cursor command grouped original per-object Undo/counters");
        Check(d.ObjectManagerEntries().Count(entry=>entry.Thing.Position==new TilePoint(9,9))==1,"Cursor command removed a different tile");
        var after=State(d);for(var index=0;index<expected;index++){d.Undo();Check(d.EditorChangeCount==h.EditorChangeCount+expected-index-1,"Per-item Undo count");}Exact(d,before);for(var index=0;index<expected;index++)d.Redo();Exact(d,after);
    }
    private static void Replies(bool native,bool empty,int reply)
    {
        var d=Used(native,empty);var before=State(d);var h=d.GetHistory();using var policy=new EditorThingRequestPolicy();using var request=policy.BeginUnderCursor(d.PlanUnderCursorRemoval(new(4,5),UnderCursorFamily.Creatures))!;
        Check(request.Prompt==EditorConfirmationPrompt.UsedCursorRemoval && request.RequiresPrompt && request.UsedCount==1,"Missing cursor-only used request");
        if(reply==0){request.Dispose();Exact(d,before);Check(d.GetHistory().Undo.SequenceEqual(h.Undo),"Cursor Cancel allocated Undo");return;}
        var count=reply==1?3:1;if(reply==1)request.Accept();else request.Decline();
        Check(request.IsAccepted && request.RemovedCount==count && d.GetHistory().Undo.Count==h.Undo.Count+count,"Cursor reply did not filter every assigned UID");
        Check(d.ScriptBindings().Any(binding=>binding.Root.Scope==2 && d.HasStoredScriptScope(binding.Root)),"Cursor removal discarded its stored point");for(var index=0;index<count;index++)d.Undo();Exact(d,before);
        if(reply==2)
        {
            // A tile containing only assigned identities has no deletion Undo.
            var free=d.ObjectManagerEntries().Single(entry=>entry.Thing.Position==new TilePoint(4,5) && entry.Identity==0).Record;
            d.ApplyEditorObjectRemoval(d.PlanObjectRemoval([free]),true);var after=State(d);h=d.GetHistory();
            using var no=policy.BeginUnderCursor(d.PlanUnderCursorRemoval(new(4,5),UnderCursorFamily.Creatures))!;no.Decline();Exact(d,after);Check(no.RemovedCount==0 && d.GetHistory().Undo.SequenceEqual(h.Undo) && d.GetHistory().Redo.SequenceEqual(h.Redo),"Cursor all-skipped No copied manager empty-parent behavior");
        }
    }
    private static void BoundsAndGuards(bool native)
    {
        var d=New(native);Add(d,PlacedThingKind.Door,new(0,0));Add(d,PlacedThingKind.Trap,new(0,0));using var policy=new EditorThingRequestPolicy();var plan=d.PlanUnderCursorRemoval(new(0,0),UnderCursorFamily.Creatures);Check(plan.Records.Count==2,"Imported boundary objects were incorrectly protected by terrain/paint rules");
        Reject(()=>d.PlanUnderCursorRemoval(new(-1,0),UnderCursorFamily.Creatures));Reject(()=>d.PlanUnderCursorRemoval(new(16,0),UnderCursorFamily.Creatures));Reject(()=>policy.Begin(d,plan.Records,EditorThingCommand.RemoveAtCursor));
        var before=State(d);d.Paint([new(8,8)],new(8,3,1));d.Undo();Check(!plan.IsCurrent,"Edit+Undo revived cursor preview");Reject(()=>policy.BeginUnderCursor(plan));Exact(d,before);
        using(var pen=d.BeginLivePenStroke(new(8,3,1)))Reject(()=>d.PlanUnderCursorRemoval(new(0,0),UnderCursorFamily.Creatures));d.Edit("nested cursor",()=>Reject(()=>d.PlanUnderCursorRemoval(new(0,0),UnderCursorFamily.Creatures)));Exact(d,before);
        using var current=policy.BeginUnderCursor(d.PlanUnderCursorRemoval(new(0,0),UnderCursorFamily.ObjectsAndSpecials))!;Check(!current.RequiresPrompt,"Doors acquired creature-only safe prompt");current.Accept();d.Undo();d.Undo();Exact(d,before);
    }
    private static void DepthAndOverflow(bool native)
    {
        foreach(var depth in new[]{0,1})
        {
            var d=New(native);Add(d,PlacedThingKind.Object,new(4,5));Add(d,PlacedThingKind.Object,new(4,5));d.SetHistoryDepth(depth);
            typeof(NativeMapDocument).GetField("editorChangeCount",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(d,uint.MaxValue);
            using var policy=new EditorThingRequestPolicy();using var request=policy.BeginUnderCursor(d.PlanUnderCursorRemoval(new(4,5),UnderCursorFamily.Creatures))!;request.Accept();Check(d.EditorChangeCount==1 && d.GetHistory().Undo.Count==depth,"Cursor uint wrap/depth cap differs from source");
        }
        var original=Used(native,false);original.Paint([new(8,8)],new(8,3,1));original.Undo();var before=State(original);var h=original.GetHistory();var counter=typeof(NativeMapDocument).GetField("changeCount",BindingFlags.Instance|BindingFlags.NonPublic)!;var value=counter.GetValue(original);
        using var policy2=new EditorThingRequestPolicy();using var refused=policy2.BeginUnderCursor(original.PlanUnderCursorRemoval(new(4,5),UnderCursorFamily.Creatures))!;counter.SetValue(original,long.MaxValue-1);Reject(()=>refused.Accept(true));counter.SetValue(original,value);Exact(original,before);Check(!refused.IsAccepted && policy2.Flags==0x100 && original.GetHistory().Redo.SequenceEqual(h.Redo),"Preflight failure lost redo, data or remembered source reply");
    }
    private static void Observer(bool native)
    {
        var d=Used(native,false);var before=State(d);using var policy=new EditorThingRequestPolicy();using var request=policy.BeginUnderCursor(d.PlanUnderCursorRemoval(new(4,5),UnderCursorFamily.Creatures))!;
        EventHandler fail=(_,_)=>throw new InvalidOperationException("after complete command");d.Changed+=fail;try{Reject(()=>request.Accept());}finally{d.Changed-=fail;}
        Check(request.IsAccepted && request.RemovedCount==3 && d.ObjectManagerEntries().Count==1,"Observer exposed partial cursor commit or wrong removed count");d.Undo();d.Undo();d.Undo();Exact(d,before);
    }
    private static void Save(bool native,string directory)
    {
        var d=Used(native,false);using var policy=new EditorThingRequestPolicy();using(var request=policy.BeginUnderCursor(d.PlanUnderCursorRemoval(new(4,5),UnderCursorFamily.Creatures))!)request.Accept();
        var path=Path.Combine(directory,$"cursor-{native}.dkm");using(var session=new EditorSaveSession(d))Check(session.Save(session.PrepareSaveAs(path),acceptLosses:true),"Cursor DKM save refused");var reopened=NativeMapDocument.Load(path);
        Check(reopened.ScriptBindings().Any(binding=>binding.Root.Scope==2 && binding.Owner is null),"Cursor orphan lost through real Save/reopen");var basis=NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");var compiled=reopened.CompileLegacyProject("DK2Preview_6a88240eaf814f78",basis);var output=compiled.SaveCopy(Path.Combine(directory,$"cursor-compiled-{native}"));Check(Directory.GetFiles(Path.GetDirectoryName(output)!).Length==7,"Cursor metadata leaked into game export");
    }
    private static void Shared(bool native)
    {
        var d=Used(native,true);var preferences=new EditorRequestPreferences(0x40000002);using var things=new EditorThingRequestPolicy(preferences);using var regions=new EditorRegionRemovalPolicy(preferences);
        using(var request=things.BeginUnderCursor(d.PlanUnderCursorRemoval(new(4,5),UnderCursorFamily.Creatures))!){Check(request.RequiresPrompt,"Manager bit2 incorrectly suppressed cursor use question");request.Accept(true);}d.Undo();d.Undo();d.Undo();Check(preferences.Flags==0x40000102,"Cursor reply damaged unrelated flags or failed to remember0x100");
        using(var request=things.Begin(d,d.ObjectManagerEntries().Select(entry=>entry.Record).ToArray(),EditorThingCommand.Modify)){Check(!request.RequiresPrompt,"Cursor remembered Yes did not cover manager Modify");request.Accept();}
        var area=d.AddMapRegion(MapRegionKind.Area,new(11,11,1,1));d.EnsureScriptScope(d.ScriptOwners().Single(owner=>owner.Scope==4 && owner.Record.File==area.File && owner.Record.Offset==area.Offset));
        using(var request=regions.Begin(d,[area])){request.Accept();Check(!request.RequiresPrompt,"Cursor remembered Yes did not cover regions");}regions.ResetSuppression();
        using var again=things.BeginUnderCursor(d.PlanUnderCursorRemoval(new(4,5),UnderCursorFamily.Creatures))!;Check(again.RequiresPrompt && preferences.Flags==0x40000002,"Region reset failed to restore cursor question independently of manager bit2");
    }
    private static void CrystalOrder()
    {
        var d=New(true);
        foreach(var position in new[]{new TilePoint(4,5),new TilePoint(4,5),new TilePoint(9,9)})
        {var row=Add(d,PlacedThingKind.Object,position);d.SetField(row,row.Fields.Single(field=>field.Name=="bObjectID"),"28");}
        var action=d.AddScenarioEvent(0xD6,new(0,0),LegacyEventSchemas.All.Single(type=>type.Name=="ACTION_SHOWCRYSTAL"));
        // Initial compiler-derived target, not an extra edit in the Undo oracle.
        Check(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(action.File.Data,out var raw),"Action fixture bytes");
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(raw.Array!.AsSpan(raw.Offset+action.Offset+12),4);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(raw.Array.AsSpan(raw.Offset+action.Offset+14),5);
        void Target(int x,int y)=>Check(System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span.Slice(action.Offset+12))==x && System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span.Slice(action.Offset+14))==y,"Cursor per-record Undo lost original first-crystal order/target");
        var before=State(d);using var policy=new EditorThingRequestPolicy();using var request=policy.BeginUnderCursor(d.PlanUnderCursorRemoval(new(4,5),UnderCursorFamily.ObjectsAndSpecials))!;
        request.Accept();Target(9,9);d.Undo();Target(4,5);d.Undo();Exact(d,before);Target(4,5);d.Redo();Target(4,5);d.Redo();Target(9,9);
    }
}
