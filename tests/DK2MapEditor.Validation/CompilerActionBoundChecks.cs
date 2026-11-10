using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class CompilerActionBoundChecks
{
    private const string GameName="DK2Preview_6a88240eaf814f78";
    private static readonly string[] names=["ACTION_ADDCREATURE","ACTION_CHANGESQUARE","ACTION_CHANGEOWNER"];
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Set(NativeMapDocument document,NativeRecord record,string name,string value)
        =>document.SetField(record,record.Fields.Single(field=>field.Name==name),value);
    private static NativeRecord Event(NativeMapDocument document,ushort id)=>ScriptGraph.Build(document).Events[id].Record;
    private static (NativeMapDocument Document,ushort Action) Fixture(string name,bool bound=true,int version=17)
    {
        var document=NativeMapDocument.CreateNewMap(new("ActionBounds","ActionBounds",32,30));
        document.Paint(from y in Enumerable.Range(12,5) from x in Enumerable.Range(14,5) select new TilePoint(x,y),new(14,3,1));
        var info=document.Records().Single(record=>record.Category=="Project.MAPPROPERTIES");Set(document,info,"Version",version.ToString());
        var condition=document.AddLegacyRecord("Trigger",new(0,0));document.SetEventType(condition,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));var conditionID=ScriptGraph.ID(condition);
        var action=document.AddLegacyRecord("Action",new(0,0));document.SetEventType(action,LegacyEventSchemas.All.Single(type=>type.Name==name));var actionID=ScriptGraph.ID(action);
        condition=Event(document,conditionID);Set(document,condition,"and",actionID.ToString());
        if(bound){var root=document.EnsureScriptScope(document.ScriptOwners().Single(owner=>owner.Scope==32));Set(document,root.Owner,root.Field.Name,conditionID.ToString());}
        return(document,actionID);
    }
    private static UnusedScriptPlan Plan(NativeMapDocument document)
    {
        var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var revision=document.EditRevision;var dirty=document.HasChanges;var before=document.GetHistory();
        var notices=0;EventHandler changed=(_,_)=>notices++;document.Changed+=changed;document.HistoryChanged+=changed;
        UnusedScriptPlan plan;
        try{plan=document.PlanUnusedScriptRemoval();}
        finally{document.Changed-=changed;document.HistoryChanged-=changed;}
        var after=document.GetHistory();Require(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&revision==document.EditRevision&&dirty==document.HasChanges&&notices==0
            &&before.EditorChangeCount==after.EditorChangeCount&&before.ChangeCount==after.ChangeCount&&before.SnapshotBytes==after.SnapshotBytes
            &&before.Undo.SequenceEqual(after.Undo)&&before.Redo.SequenceEqual(after.Redo),"Scenario eligibility changed source/history.");return plan;
    }
    public static void Run(Action<string,Action> run)
    {
        foreach(var name in names)run("compiler action bounds: "+name+" keeps source negative coordinates in targets13/16/17",()=>
        {
            foreach(var version in new[]{13,16,17})foreach(var point in new[]{new TilePoint(-1,3),new TilePoint(3,-1),new TilePoint(int.MinValue,int.MinValue),new TilePoint(0,0),new TilePoint(31,29)})
            {
                var(document,id)=Fixture(name,version:version);Set(document,Event(document,id),"param5",point.X.ToString());Set(document,Event(document,id),"param7",point.Y.ToString());
                Require(Plan(document).CanApply,"Source-allowed action target was rejected: "+name+" "+point);
                Require(!NativeMapValidator.Validate(document,includeGameplay:true).Any(item=>item.Severity==DiagnosticSeverity.Error),"Public full check disagrees with source bounds.");
            }
        });
        run("compiler action bounds: upper equality blocks bound actions; unbound actions remain removable",()=>
        {
            foreach(var name in names)foreach(var point in new[]{new TilePoint(32,3),new TilePoint(3,30),new TilePoint(32,30)})
            {
                foreach(var bound in new[]{true,false})
                {
                    var(document,id)=Fixture(name,bound);Set(document,Event(document,id),"param5",point.X.ToString());Set(document,Event(document,id),"param7",point.Y.ToString());
                    var plan=Plan(document);Require(plan.Issues.Any(issue=>issue.Problem==UnusedScriptProblem.ActionCoordinates)==bound,"Reachability or upper-exclusive limit changed.");
                    if(!bound)Require(plan.CanApply&&plan.Events.Any(item=>item.ID==id),"Unused action was blocked instead of offered for explicit cleanup.");
                }
            }
        });
        run("compiler action bounds: resize-shifted negative targets survive eligibility and exact Undo/Redo",()=>
        {
            foreach(var name in names)
            {
                var(document,id)=Fixture(name);Set(document,Event(document,id),"param5","2");Set(document,Event(document,id),"param7","3");
                var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
                using(var session=document.CreateMapPropertiesSession())document.ApplyEditorMapProperties(document.PrepareEditorMapProperties(session,new(24,24,8,Fill:new(2,2,1))));
                var shifted=Event(document,id);Require(shifted.Read(shifted.Fields.Single(field=>field.Name=="param5"))=="-6"&&shifted.Read(shifted.Fields.Single(field=>field.Name=="param7"))=="-3","Fixture did not retain source-shifted targets.");
                Require(Plan(document).CanApply,"Resize produced a newly blocked source target.");
                document.Undo();Require(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Resize Undo changed original bytes.");
                document.Redo();Require(Plan(document).CanApply,"Redo reintroduced the false negative-coordinate refusal.");
            }
        });
        run("compiler action bounds: DKM Save/reopen keeps signed LONG and seven-file compilation uses source WORD casts",()=>
        {
            var directory=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2ActionBounds-"+Guid.NewGuid().ToString("N")));
            Require(directory.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture root.");Directory.CreateDirectory(directory);
            try
            {
                var basis=NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
                foreach(var name in names)
                {
                    var(document,id)=Fixture(name);Set(document,Event(document,id),"param5","-1");Set(document,Event(document,id),"param7",int.MinValue.ToString());
                    var path=Path.Combine(directory,name+".dkm");using(var save=new EditorProjectSaveSession(document))save.SaveAs(path);
                    var loaded=NativeMapDocument.Load(path);Require(Plan(loaded).CanApply,"Signed target became ineligible after DKM reopen.");
                    var action=Event(loaded,id);Require(action.Read(action.Fields.Single(field=>field.Name=="param5"))=="-1"&&action.Read(action.Fields.Single(field=>field.Name=="param7"))==int.MinValue.ToString(),"DKM changed signed source values.");
                    var output=loaded.CompileLegacyProject(GameName,basis);var native=Event(output,id);
                    Require(BinaryPrimitives.ReadUInt16LittleEndian(native.File.Data.Span[(native.Offset+12)..])==65535&&BinaryPrimitives.ReadUInt16LittleEndian(native.File.Data.Span[(native.Offset+14)..])==0,"Compiler changed source unchecked WORD casts.");
                    var saved=output.SaveCopy(Path.Combine(directory,name));Require(Directory.GetFiles(Path.GetDirectoryName(saved)!).Length==7,"Export is not seven files.");
                    var reopened=Event(NativeMapDocument.Load(saved),id);Require(reopened.File.Data.Slice(reopened.Offset,24).Span.SequenceEqual(native.File.Data.Slice(native.Offset,24).Span),"Native save/reopen changed compiled action bytes.");
                }
            }
            finally{Directory.Delete(directory,recursive:true);}
        });
    }
}
