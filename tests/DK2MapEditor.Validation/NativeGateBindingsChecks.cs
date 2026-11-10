using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;

internal static class NativeGateBindingsChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static NativeMapDocument New()=>NativeMapDocument.CreateNewMap(new("GateLinks","GateLinks",32,30,NativeFormat:true));
    private static NativeRecord Action(NativeMapDocument doc)=>doc.AddScenarioEvent(0xD6,new(0,0),LegacyEventSchemas.All.Single(type=>type.Name=="ACTION_DESTROYGATE"));
    private static string Key(NativeMapDocument doc,NativeRecord gate)=>doc.ScenarioGateChoices().Single(choice=>choice.Key==gate.File.ExportPath+":"+gate.Offset).Key;
    private static string? Selected(NativeMapDocument doc,NativeRecord action)=>doc.SelectedScenarioGate(doc.CreateScenarioParameterDraft(action));
    private static void Choose(NativeMapDocument doc,NativeRecord action,NativeRecord gate)=>doc.ApplyScenarioEditorParameterDraft(doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(action),Key(doc,gate)));
    private static void Point(NativeRecord action,int x,int y)=>Expect(BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span.Slice(action.Offset+12))==x&&BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span.Slice(action.Offset+14))==y,"Working gate link followed the wrong coincident record.");
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument doc)=>doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Gate-link operation changed unexpected game buffers.");
    private static NativeRecord GateAt(NativeMapDocument doc,int x,int y)=>doc.MapRegions().Single(region=>region.Kind==MapRegionKind.Gate&&region.Bounds.X==x&&region.Bounds.Y==y).Record;
    public static void Run()
    {
        CollisionAndHistory();DeletionAndClone();PrivateAndProject();
        Console.WriteLine("NATIVE GATE BINDINGS DETAIL persistent coincident gate identity, metadata-only second choice, separate X/Y, Undo/Redo/zero history, object/gate/event removal rebasing, cloned actions, unused graph removal, wizard/parent resize private copies, DKM identity and seven-file native export policy");
    }
    private static void CollisionAndHistory()
    {
        var doc=New();var a=doc.AddMapRegion(MapRegionKind.Gate,new(3,5,1,1));var b=doc.AddMapRegion(MapRegionKind.Gate,new(9,11,1,1));
        var one=Action(doc);var two=Action(doc);Choose(doc,one,a);Choose(doc,two,b);var original=Bytes(doc);var count=doc.EditorChangeCount;
        doc.SetMapRegionBounds(b,new(3,5,1,1));Point(one,4,6);Point(two,4,6);
        Expect(Selected(doc,one)==Key(doc,a)&&Selected(doc,two)==Key(doc,b),"Collision discarded a working gate identity.");
        doc.SetMapRegionBounds(a,new(15,17,1,1));Point(one,16,18);Point(two,4,6);
        doc.Undo();Expect(Selected(doc,two)==Key(doc,b),"Undo restored coordinates without the second gate identity.");
        doc.Undo();Exact(original);Expect(doc.EditorChangeCount==count&&Selected(doc,two)==Key(doc,b),"Second Undo lost the pre-collision link/count.");doc.Redo();doc.Redo();
        doc.SetMapRegionBounds(b,new(15,17,1,1));
        doc.SetField(b,b.Fields.Single(field=>field.Name=="dwGateLocationX"),"21");doc.SetField(b,b.Fields.Single(field=>field.Name=="dwGateLocationY"),"23");
        Point(one,16,18);Point(two,22,24);
        doc.SetMapRegionBounds(b,new(15,17,1,1));var before=Bytes(doc);var history=doc.GetHistory().Undo.Count;count=doc.EditorChangeCount;
        var draft=doc.UpdateScenarioGate(doc.CreateScenarioParameterDraft(one),Key(doc,b));Expect(draft.Changed&&Selected(doc,one)==Key(doc,a),"Private second-gate choice wrote live metadata or appeared unchanged.");
        doc.ApplyScenarioEditorParameterDraft(draft);Exact(before);
        Expect(Selected(doc,one)==Key(doc,b)&&doc.GetHistory().Undo.Count==history+1&&doc.EditorChangeCount==unchecked(count+1),"Same-coordinate semantic choice did not create one atomic changed-parameter Undo.");
        doc.Undo();Exact(before);Expect(Selected(doc,one)==Key(doc,a),"Metadata-only Undo lost first choice.");doc.Redo();Expect(Selected(doc,one)==Key(doc,b),"Metadata-only Redo lost second choice.");
        doc.SetHistoryDepth(0);doc.SetMapRegionBounds(a,new(7,8,1,1));Point(one,16,18);Point(two,16,18);doc.SetMapRegionBounds(b,new(25,26,1,1));Point(one,26,27);Point(two,26,27);Expect(!doc.CanUndo,"Zero history retained a hidden link Undo.");
    }
    private static void DeletionAndClone()
    {
        var doc=New();var objectRow=doc.AddRecord(RecordTemplate.All.Single(item=>item.Structure=="GAMEOBJECT"),new(2,2));
        var a=doc.AddMapRegion(MapRegionKind.Gate,new(3,5,1,1));var b=doc.AddMapRegion(MapRegionKind.Gate,new(9,11,1,1));
        var unused=doc.AddScenarioEvent(0xD5,new(0,0),LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
        var one=Action(doc);var two=Action(doc);Choose(doc,one,a);Choose(doc,two,b);doc.SetMapRegionBounds(b,new(3,5,1,1));
        var clone=doc.DuplicateRecord(two);Expect(Selected(doc,clone)==Key(doc,b),"Cloned action rebound to first coincident gate.");
        var ids=new[]{one,two,clone}.Select(ScriptGraph.ID).ToArray();doc.DeleteRecord(objectRow);
        var gates=doc.MapRegions().Where(region=>region.Kind==MapRegionKind.Gate).ToArray();a=gates[0].Record;b=gates[1].Record;
        Expect(Selected(doc,clone)==Key(doc,b),"Non-gate deletion shifted a gate address.");
        doc.DeleteRecord(unused);NativeRecord Event(ushort id)=>doc.Records().Single(row=>ScriptGraph.IsEvent(row)&&ScriptGraph.ID(row)==id);
        one=Event(ids[0]);two=Event(ids[1]);clone=Event(ids[2]);Expect(Selected(doc,two)==Key(doc,b)&&Selected(doc,clone)==Key(doc,b),"Earlier event removal lost later action bindings.");
        var before=Bytes(doc);doc.DeleteRecord(a);b=doc.MapRegions().Single(region=>region.Kind==MapRegionKind.Gate).Record;
        Point(one,1,1);Expect(Selected(doc,one) is null&&Selected(doc,two)==Key(doc,b),"Gate removal rebound missing first gate or lost second ordinal.");
        doc.SetMapRegionBounds(b,new(8,9,1,1));Point(two,9,10);Point(clone,9,10);Point(one,1,1);doc.Undo();doc.Undo();Exact(before);
        gates=doc.MapRegions().Where(region=>region.Kind==MapRegionKind.Gate).ToArray();b=gates[1].Record;
        var area=doc.AddMapRegion(MapRegionKind.Area,new(2,3,2,2));doc.SetField(area,area.Fields.Single(field=>field.Name=="wAreaTrigger"),ids[1].ToString());
        var plan=doc.PlanUnusedScriptRemoval();Expect(plan.CanApply&&plan.Events.Count==2,"Unused-removal fixture did not retain the one bound destroy action.");doc.ApplyUnusedScriptRemoval(plan);
        two=Event(ids[1]);Expect(Selected(doc,two)==Key(doc,b),"Bulk unused-event removal lost the surviving working gate link.");doc.SetMapRegionBounds(b,new(12,13,1,1));Point(two,13,14);
    }
    private static void PrivateAndProject()
    {
        var doc=New();var a=doc.AddMapRegion(MapRegionKind.Gate,new(3,5,1,1));var b=doc.AddMapRegion(MapRegionKind.Gate,new(3,5,1,1));var action=Action(doc);Choose(doc,action,b);
        var before=Bytes(doc);using(var cancel=doc.CreateScenarioEditingSession(doc.CreateScenarioCreationContext(),action))
        {var working=cancel.WorkingDocument;Choose(working,cancel.Record,working.MapRegions().Where(region=>region.Kind==MapRegionKind.Gate).First().Record);_=doc.PrepareScenarioCreation(cancel);Exact(before);}
        Expect(Selected(doc,action)==Key(doc,b),"Wizard Cancel changed the coincident gate link.");
        using(var edit=doc.CreateScenarioEditingSession(doc.CreateScenarioCreationContext(),action))
        {var working=edit.WorkingDocument;Choose(working,edit.Record,working.MapRegions().Where(region=>region.Kind==MapRegionKind.Gate).First().Record);doc.ApplyScenarioEditorCreation(doc.PrepareScenarioCreation(edit));}
        Expect(Selected(doc,action)==Key(doc,a),"Wizard Apply lost its private working gate choice.");doc.Undo();Expect(Selected(doc,action)==Key(doc,b),"Wizard Undo lost original second gate.");
        using(var resize=doc.CreateMapPropertiesSession())doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(resize,new(36,34,8)));
        Point(action,8,10);Expect(Selected(doc,action)==Key(doc,b),"Parent resize clone rebound the second coincident gate.");
        doc.SetMapRegionBounds(a,new(15,17,1,1));Point(action,8,10);doc.Undo();doc.Undo();Exact(before);Expect(Selected(doc,action)==Key(doc,b),"Parent resize Undo lost binding metadata.");
        var conversion=doc.PrepareProjectConversion();Expect(!conversion.HasBlockingIssues,"Coincident gates blocked an otherwise supported DKM projection.");
        var projected=conversion.Project.Records().Single(row=>row.Category=="Project.ACTION");Expect(projected.Read(projected.Fields.Single(field=>field.Name=="param5"))=="2","DKM conversion guessed first coordinates instead of the selected gate ID.");
        Expect(conversion.SourceFiles.Any(file=>file.ExportPath.EndsWith("#GateBindings",StringComparison.Ordinal)),"Native source snapshot omitted working links.");
        var directory=Path.Combine(Path.GetTempPath(),"DK2GateBindings",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        using(var save=new EditorProjectSaveSession(conversion.Project))save.SaveAs(Path.Combine(directory,"selected.dkm"));
        var loaded=NativeMapDocument.Load(Path.Combine(directory,"selected.dkm"));var saved=loaded.Records().Single(row=>row.Category=="Project.ACTION");Expect(saved.Read(saved.Fields.Single(field=>field.Name=="param5"))=="2","Real DKM Save/reopen lost second gate identity.");
        // Publish through the actual official compiler; a raw synthetic draft
        // may reference shared definitions instead of packing Globals.kld.
        var basis=NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
        var compiled=conversion.Project.CompileLegacyProject("GateLinksOfficial",basis);
        var output=compiled.SaveCopy(Path.Combine(directory,"native"));var maps=Path.GetDirectoryName(output)!;
        var names=new[]{"GateLinksOfficial.kwd","GateLinksOfficialGlobals.kld","GateLinksOfficialMap.kld","GateLinksOfficialPlayers.kld","GateLinksOfficialThings.kld","GateLinksOfficialTriggers.kld","GateLinksOfficialVariables.kld"};
        Expect(Directory.GetFiles(maps).Select(Path.GetFileName).Order().SequenceEqual(names.Order()),"Official publication did not contain exactly the seven original map files.");
        Expect(!Directory.GetFiles(Path.Combine(directory,"native"),"*GateBindings*",SearchOption.AllDirectories).Any(),"Working links escaped into the native game export.");
        var native=NativeMapDocument.Load(output);var opened=native.Records().Single(IsDestroy);Expect(native.SelectedScenarioGate(native.CreateScenarioParameterDraft(opened))==native.ScenarioGateChoices()[0].Key,"Plain native reopen changed the original first-coordinate reader policy.");
    }
    private static bool IsDestroy(NativeRecord record)=>EntityReferences.EventType(record)?.Name=="ACTION_DESTROYGATE";
}
