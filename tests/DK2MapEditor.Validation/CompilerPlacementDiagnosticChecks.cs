using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class CompilerPlacementDiagnosticChecks
{
    private static readonly HashSet<string> codes=["DK2N041","DK2N042","DK2N043","DK2N044","DK2N045"];
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static NativeMapDocument Project()=>NativeMapDocument.CreateNewMap(new("PlacementChecks","PlacementChecks",32,30,PlaceHearts:false));
    private static void Set(NativeMapDocument document,NativeRecord record,string field,string value)
        =>document.SetField(record,record.Fields.Single(item=>item.Name==field),value);
    private static IReadOnlyList<MapDiagnostic> Check(NativeMapDocument document,bool gameplay=true)
    {
        var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var revision=document.EditRevision;var dirty=document.HasChanges;
        var before=document.GetHistory();var notifications=0;EventHandler changed=(_,_)=>notifications++;
        document.Changed+=changed;document.HistoryChanged+=changed;IReadOnlyList<MapDiagnostic> result;
        try{result=NativeMapValidator.Validate(document,includeGameplay:gameplay).Where(item=>codes.Contains(item.Code)).ToArray();}
        finally{document.Changed-=changed;document.HistoryChanged-=changed;}
        var after=document.GetHistory();
        Require(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&revision==document.EditRevision&&dirty==document.HasChanges&&notifications==0
            &&before.ChangeCount==after.ChangeCount&&before.EditorChangeCount==after.EditorChangeCount&&before.SnapshotBytes==after.SnapshotBytes
            &&before.Undo.SequenceEqual(after.Undo)&&before.Redo.SequenceEqual(after.Redo),"Compiler diagnostics modified document/history.");
        Require(result.All(item=>item.Severity!=DiagnosticSeverity.Error),"Source placement/gate notices became blockers.");return result;
    }
    private static NativeRecord Thing(NativeMapDocument document,string kind,int x,int y,byte owner=3)
    {
        var record=document.AddLegacyRecord(kind,new(x,y),owner);
        if(kind=="MagicalObject")Set(document,record,"id","117");return record;
    }
    private static void Rectangle(NativeMapDocument document,int x,int y,int width,int height,MapTile tile)
        =>document.Paint(from yy in Enumerable.Range(y,height) from xx in Enumerable.Range(x,width) select new TilePoint(xx,yy),tile);
    private static void RawVersion(NativeFile file,uint version)
    {Require(MemoryMarshal.TryGetArray(file.Data,out var bytes),"Fixture bytes unavailable.");BinaryPrimitives.WriteUInt32LittleEndian(bytes.Array!.AsSpan(bytes.Offset+4),version);}
    public static void Run(Action<string,Action> run)
    {
        run("compiler placement notices: all five thing families on solid terrain warn without counting self as replacement",()=>
        {
            var project=Project();var x=3;
            foreach(var kind in new[]{"Creature","Door","Trap","Object","MagicalObject"})Thing(project,kind,x++,3);
            var result=Check(project);Require(result.Count(item=>item.Code=="DK2N041"&&item.Severity==DiagnosticSeverity.Warning)==5,"Missing solid-terrain source warnings.");
            Require(result.All(item=>item.Region is {Width:1,Height:1}),"Thing navigation regions missing.");
            Rectangle(project,3,3,5,1,new(8,3,1));
            Require(Check(project).Count==0,"Existing records were treated as collisions/replacements on valid terrain.");
        });
        run("compiler placement notices: enemy creature/object/magic are Information; doors/traps remain warnings",()=>
        {
            var project=Project();Rectangle(project,3,4,5,1,new(15,4,1));var x=3;
            foreach(var kind in new[]{"Creature","Door","Trap","Object","MagicalObject"})Thing(project,kind,x++,4);
            var result=Check(project);
            Require(result.Count(item=>item.Code=="DK2N042"&&item.Severity==DiagnosticSeverity.Information)==3,"Source result2 became a warning/error or was omitted.");
            Require(result.Count(item=>item.Code=="DK2N041"&&item.Severity==DiagnosticSeverity.Warning)==2,"Door/trap terrain rejection did not fall through source rules.");
        });
        run("compiler placement notices: verified water/lava flags and original unknown-species fallthrough",()=>
        {
            var project=Project();var row=project.GlobalDefinitionRows().Single(item=>item.Family=="Creatures"&&item.ID==1);
            project.SetGlobalDefinitionValues([new("Creatures",row.Index,"CanWalkWater","0"),new("Creatures",row.Index,"CanWalkLava","1")]);
            Rectangle(project,3,5,1,1,new(4,2,1));Rectangle(project,4,5,1,1,new(5,2,1));
            Thing(project,"Creature",3,5);Thing(project,"Creature",4,5);
            var result=Check(project);Require(result.Count(item=>item.Code=="DK2N043"&&item.Severity==DiagnosticSeverity.Warning)==1&&result.Single().Region?.X==3,"Walking flags were ignored or enemy checks ran before liquid checks.");
            var unknown=Project();Rectangle(unknown,3,5,1,1,new(4,2,1));Rectangle(unknown,4,5,1,1,new(5,2,1));
            Set(unknown,Thing(unknown,"Creature",3,5),"id","9999");Set(unknown,Thing(unknown,"Creature",4,5),"id","9999");
            var unknownResult=Check(unknown);Require(unknownResult.Count==1&&unknownResult[0].Code=="DK2N043"&&unknownResult[0].Region?.X==4,"Unknown source species must fall through safe on water and fatal on lava.");
        });
        run("compiler gate notices: destroyable two-square gate checks gate, expanded area, then destruction events",()=>
        {
            var project=Project();Rectangle(project,8,8,2,2,new(33,1,1));
            var absent=Check(project).Where(item=>item.Code=="DK2N044").ToArray();
            Require(absent.Length==1&&absent[0].Severity==DiagnosticSeverity.Information&&absent[0].Region==new MapDiagnosticRegion(8,8,2,2),"Missing gate binding notice absent.");
            project.AddLegacyRecord("Gate",new(8,8));
            var areaNotice=Check(project).Single(item=>item.Code=="DK2N044");Require(areaNotice.Region==new MapDiagnosticRegion(7,7,4,4),"Missing destruction area must navigate expanded perimeter.");
            var area=project.AddLegacyRecord("Area",new(7,7));Set(project,area,"rect.x2","10");Set(project,area,"rect.y2","10");
            Require(Check(project).Count(item=>item.Code=="DK2N044")==1,"Missing destruction event notice absent.");
            project.ApplyHeroGateGeneration(project.PlanHeroGateGeneration(HeroGateGenerationOptions.Defaults(project)));
            Require(Check(project).All(item=>item.Code!="DK2N044"),"Complete source gate/area/events was still diagnosed.");
        });
        run("compiler gate notices: wide/tall missing gates warn; display and indestructible two-square gates do not",()=>
        {
            var project=Project();Rectangle(project,5,5,3,1,new(37,1,1));Rectangle(project,10,5,1,3,new(37,1,2));
            Rectangle(project,15,5,1,1,new(29,1,1));Rectangle(project,1,1,2,2,new(33,1,1));
            var result=Check(project);Require(result.Count(item=>item.Code=="DK2N045"&&item.Severity==DiagnosticSeverity.Warning)==2&&result.All(item=>item.Code!="DK2N044"),"Gate type/cannot-destroy severity rules differ.");
            project.AddLegacyRecord("Gate",new(5,5));Require(Check(project).Count(item=>item.Code=="DK2N045")==1,"Existing wide gate binding was ignored.");
        });
        run("compiler gate notices: the first area binding controls destruction even if a later duplicate has valid events",()=>
        {
            var project=Project();Rectangle(project,8,8,2,2,new(33,1,1));project.AddLegacyRecord("Gate",new(8,8));
            var area=project.AddLegacyRecord("Area",new(7,7));Set(project,area,"rect.x2","10");Set(project,area,"rect.y2","10");
            project.ApplyHeroGateGeneration(project.PlanHeroGateGeneration(HeroGateGenerationOptions.Defaults(project)));
            var first=project.Records().Single(item=>item.Category.StartsWith("Project.TRIGGERLINK",StringComparison.Ordinal)
                &&item.Read(item.Fields.Single(field=>field.Name=="node.type"))=="4");
            var original=first.File.Data.Slice(first.Offset,first.Length).ToArray();
            var duplicate=project.AddLegacyRecord("TriggerLink",new(0,0));
            Require(duplicate.Length==original.Length,"Fixture root layout differs.");
            Require(MemoryMarshal.TryGetArray(duplicate.File.Data,out var target),"Fixture root bytes unavailable.");
            original.CopyTo(target.Array!,target.Offset+duplicate.Offset);Set(project,first,"trigger","0");
            Require(Check(project).Count(item=>item.Code=="DK2N044")==1&&!project.HeroGateSites().Single().HasEvents,"A later area root hid the source-first missing destruction events.");
        });
        run("compiler placement notices: verified native layouts use physical hero/neutral owners and preserve raw bytes",()=>
        {
            var native=NativeMapDocument.Create(32,30,"DK2Preview_6a88240eaf814f78");Rectangle(native,3,5,5,1,new(15,4,1));
            foreach(var item in new[]{("GAMEHERO",3),("GAMEDOOR",4),("GAMETRAP",5),("GAMEOBJECT",6),("GAMENEUTRAL",7)})
                native.AddRecord(RecordTemplate.All.Single(row=>row.Structure==item.Item1),new(item.Item2,5),3);
            var result=Check(native);Require(result.Count(item=>item.Code=="DK2N042")==3&&result.Count(item=>item.Code=="DK2N041")==2,"Native record projection did not use source placement rules.");
        });
        run("compiler notices: structural validation stays unchanged and opaque/future records gain no inferred authority",()=>
        {
            var project=Project();Thing(project,"Creature",3,3);Rectangle(project,5,5,3,1,new(37,1,1));
            Require(Check(project,gameplay:false).Count==0,"Draft structural compilation gained gameplay warnings.");
            RawVersion(project.Files.Single(file=>file.Signature==NativeMapDocument.LegacyThings),99);
            RawVersion(project.Files.Single(file=>file.Signature==NativeMapDocument.LegacyAreas),99);
            Require(Check(project).Count==0,"Opaque project records were guessed or missing gate bindings were asserted from an unknown area layout.");
            var native=NativeMapDocument.Create(32,30,"FutureNotices");native.AddRecord(RecordTemplate.All.Single(row=>row.Structure=="GAMEOBJECT"),new(3,3),3);
            RawVersion(native.Files.Single(file=>file.Signature==0xBE),99);Require(Check(native).Count==0,"Future native record was interpreted as a verified placed thing.");
        });
    }
}
