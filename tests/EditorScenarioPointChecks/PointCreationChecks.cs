using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void EditorRecordCreation()
    {
        foreach(var (kind,scope) in new[]{("Creature",2u),("Door",64u),("Object",128u),("MagicalObject",256u),("Band",8u)})
        {
            var d=NativeMapDocument.CreateNewMap(new("EditorCreation","EditorCreation",16,16));var before=Snapshot(d);var h=d.GetHistory();var rev=d.EditRevision;
            var row=d.AddEditorLegacyRecord(kind,new(2,2));Accepted(d,h,rev,1);Check(row.Read(row.Fields.Single(field=>field.Name=="uniqueid"))=="0","Source editor inserted a reserved identity");
            if(kind=="MagicalObject")Set(d,row,"id",ValueCatalogues.ForTemplate(kind,ValueCatalogues.GameVersion(d)).First().Value);
            var owner=d.ScriptOwners().Single(candidate=>candidate.Scope==scope && candidate.Record.Offset==row.Offset && candidate.Record.File==row.File);
            using(var session=d.CreateEditorScenarioPointSession(initialScope:scope)){Check(session.Choices.Any(choice=>choice.CanSelect && SameOwner(choice.Owner,owner)),"Fresh editor record unavailable to point wizard");session.SelectOwner(owner);_=d.ApplyEditorScenarioPoint(d.PrepareEditorScenarioPoint(session));}
            Check(row.Read(row.Fields.Single(field=>field.Name=="uniqueid"))=="1","First point did not assign source identity");d.Undo();d.Undo();if(kind=="MagicalObject")d.Undo();Exact(before);
            var trusted=d.AddLegacyRecord(kind,new(3,3));Check(trusted.Read(trusted.Fields.Single(field=>field.Name=="uniqueid"))!="0","Trusted Add API changed its identity policy");
        }
        Pass("editor record insertion uses zero identity until point Finish across five owner families; trusted Add keeps prior policy and exact Undo remains valid");
    }
    private static void EditorMemberCreation()
    {
        var d=NativeMapDocument.CreateNewMap(new("Members","Members",16,16));var band=d.AddEditorLegacyRecord("Band",new(2,2));var before=Snapshot(d);var history=d.GetHistory();var revision=d.EditRevision;
        var index=d.AddEditorBandMember(band,6);Accepted(d,history,revision,1);Check(index==0 && band.Read(band.Fields.Single(field=>field.Name=="heros[0].uniqueid"))=="0","Source new member identity reserved");
        var owner=d.ScriptOwners().Single(candidate=>candidate.Scope==16);_=Create(d,owner);Check(band.Read(band.Fields.Single(field=>field.Name=="heros[0].uniqueid"))=="1","Member point did not assign source identity");d.Undo();d.Undo();Exact(before);
        _=d.AddBandMember(band,6);Check(band.Read(band.Fields.Single(field=>field.Name=="heros[0].uniqueid"))!="0","Trusted member Add identity changed");
        Pass("editor band member insertion remains available until point Finish; trusted Add and exact parent/member Undo stay unchanged");
    }
    private static void EditorBatchCreation()
    {
        foreach(var kind in Enum.GetValues<PlacedThingKind>())
        {
            var d=NativeMapDocument.CreateNewMap(new("BatchIdentity","BatchIdentity",16,16));var template=ObjectBatchTemplate.Default(kind) with{Owner=3};
            var options=new ObjectBatchPlacementOptions(Position:new(2,2),Replacement:ObjectReplacementPolicy.All,Flags:(ObjectBatchFlags)127);
            var plan=d.PlanObjectBatchInsertion(template,options);var before=Snapshot(d);_=d.ApplyEditorObjectBatchInsertion(plan,[0]);
            var row=d.ObjectManagerEntries().Single().Record;Check(row.Read(row.Fields.Single(field=>field.Name=="uniqueid"))=="0","Source manager insertion reserved scenario ID");
            var owner=d.ScriptOwners().FirstOrDefault(candidate=>candidate.Record.File==row.File && candidate.Record.Offset==row.Offset);
            if(owner is not null){using var session=d.CreateEditorScenarioPointSession(initialScope:owner.Scope);Check(session.Choices.Any(choice=>choice.CanSelect && SameOwner(choice.Owner,owner)),"Manager inserted owner missing from point list");}
            d.Undo();Exact(before);_=d.ApplyObjectBatchInsertion(d.PlanObjectBatchInsertion(template,options),[0]);row=d.ObjectManagerEntries().Single().Record;Check(row.Read(row.Fields.Single(field=>field.Name=="uniqueid"))!="0","Trusted batch identity policy changed");
        }
        var painted=NativeMapDocument.CreateNewMap(new("PaintIdentity","PaintIdentity",16,16));painted.Paint([new(2,2)],new(3,2,1));var p=painted.PlanObjectPainting(ObjectBatchTemplate.Default(PlacedThingKind.Creature),new(2,2));Check(!p.Locked,"Paint identity terrain fixture");_=painted.ApplyObjectPainting(p);
        var creature=painted.ScriptOwners().Single(owner=>owner.Scope==2);Check(creature.Record.Read(creature.Record.Fields.Single(field=>field.Name=="uniqueid"))=="0","Source painted creature unavailable to point list");_=Create(painted,creature);
        Pass("editor manager insertion all five families and source creature paint defer identity until point attachment; trusted batch preserves allocation");
    }
}
