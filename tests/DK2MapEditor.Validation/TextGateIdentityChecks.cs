using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class TextGateIdentityChecks
{
    private const string GameName="DK2Preview_6a88240eaf814f78";
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Set(NativeMapDocument document,NativeRecord record,string name,string value)
        =>document.SetField(record,record.Fields.Single(field=>field.Name==name),value);
    private static NativeRecord Action(NativeMapDocument document)=>document.Records().Single(record=>EntityReferences.EventType(record)?.Name=="ACTION_DESTROYGATE");
    private static NativeRecord Gate(NativeMapDocument document,int x=4,int y=5)
    {var gate=document.AddRecord(RecordTemplate.All.Single(row=>row.Structure=="GAMEGATE"),new(x,y),1);Set(document,gate,"bGateType","33");return gate;}
    private static NativeMapDocument Native(bool gate=true)
    {
        var document=NativeMapDocument.Create(12,12,GameName);
        if(gate)Gate(document);
        NativeRecord Event(string name)
        {
            var type=LegacyEventSchemas.All.Single(row=>row.Name==name);
            var record=document.AddRecord(RecordTemplate.All.Single(row=>row.Structure==(type.RecordSignature==0xD5?"Trigger":"Action")),new(0,0));
            document.SetEventType(record,type);return document.Records().Single(row=>row.File==record.File&&row.Offset==record.Offset);
        }
        var condition=Event("TRIGGER_TIMEELAPSED");Set(document,condition,"Param1","5");Set(document,condition,"Param5","600");
        var action=Event("ACTION_DESTROYGATE");Set(document,action,"NativeArgument5","5");Set(document,action,"NativeArgument7","6");
        Set(document,condition,"ChildAnd",action.Read(action.Fields.Single(field=>field.Name=="ID")));
        var player=document.Records().Single(row=>row.Category=="GAMEPLAYER");
        Set(document,player,"bPlayerTrigger",condition.Read(condition.Fields.Single(field=>field.Name=="ID")));
        return document;
    }
    private static string? Selected(NativeMapDocument document)=>document.SelectedScenarioGate(document.CreateScenarioParameterDraft(Action(document)));
    private static void Select(NativeMapDocument document,int index)
    {var draft=document.CreateScenarioParameterDraft(Action(document));document.ApplyScenarioParameterDraft(document.UpdateScenarioGate(draft,document.ScenarioGateChoices()[index].Key));}
    private static TextMapProjection Project(NativeMapDocument document)
    {
        var selected=Selected(document);var files=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var revision=document.EditRevision;var dirty=document.HasChanges;var history=document.GetHistory();
        var changes=0;EventHandler changed=(_,_)=>changes++;document.Changed+=changed;document.HistoryChanged+=changed;
        TextMapProjection result;try{result=TextMapFile.Project(document);}finally{document.Changed-=changed;document.HistoryChanged-=changed;}
        var after=document.GetHistory();Require(files.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&revision==document.EditRevision&&dirty==document.HasChanges&&changes==0&&selected==Selected(document)
            &&history.ChangeCount==after.ChangeCount&&history.EditorChangeCount==after.EditorChangeCount&&history.SnapshotBytes==after.SnapshotBytes&&history.Undo.SequenceEqual(after.Undo)&&history.Redo.SequenceEqual(after.Redo),"Projection changed source bytes, gate identity or history.");
        return result;
    }
    private static void Missing(NativeMapDocument document)
    {
        Require(Selected(document) is null,"Fixture acquired a target before export.");var projection=Project(document);
        Require(!projection.Text.Contains("ACTION_DESTROY_HERO_GATE",StringComparison.Ordinal),"Text export silently attached a missing target to another physical gate.");
        Require(projection.Limitations.Any(loss=>loss.Code=="Scripts"),"Omitted missing-gate action was not reported as scenario loss.");
    }
    private static void Target(NativeMapDocument document,int id)
    {Require(Project(document).Text.Contains("ACTION_DESTROY_HERO_GATE "+id+"\r\n",StringComparison.Ordinal),"Text export did not preserve the selected physical gate as projected ID"+id+".");}
    public static void Run(Action<string,Action> run)
    {
        run("text gate identity: known single and imported first physical target retain source projection",()=>
        {
            var native=Native();Target(native,1);Gate(native);Target(native,1);
            var reused=Native();Require(Selected(reused) is not null,"Initial target is absent.");
            reused.DeleteRecord(reused.Records().Single(row=>row.Category=="GAMEGATE"));Gate(reused,6,7);
            Require(Selected(reused) is not null,"Source policy of deliberately reusing a freed logical ID changed.");Target(reused,1);
        });
        run("text gate identity: adding a gate at formerly missing coordinates cannot acquire the action",()=>
        {var native=Native(false);Require(Selected(native) is null,"Fixture was not missing.");Gate(native);Missing(native);});
        run("text gate identity: explicitly selected duplicate survives projection and parameter Undo/Redo",()=>
        {var native=Native();Gate(native);Select(native,1);Target(native,2);native.Undo();Target(native,1);native.Redo();Target(native,2);});
        run("text gate identity: deleted target remains missing beside an existing gate at fallback coordinates",()=>
        {
            var native=Native();var first=native.Records().Single(row=>row.Category=="GAMEGATE");Gate(native,0,0);native.DeleteRecord(first);Missing(native);
        });
        run("text gate identity: removing the selected duplicate does not retarget the remaining duplicate",()=>
        {
            var native=Native();var second=Gate(native);Select(native,1);native.DeleteRecord(second);Missing(native);
            native.Undo();Target(native,2);native.Redo();Missing(native);
        });
        run("text gate identity: actual MAP reopens selected projected ID; native copy keeps wire bytes with editor-state warning",()=>
        {
            var root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2TextGate-"+Guid.NewGuid().ToString("N")));
            Require(root.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture root.");
            var native=Native();Gate(native);Select(native,1);var originals=native.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray(),StringComparer.OrdinalIgnoreCase);
            try
            {
                var output=TextMapFile.SaveCopy(root,native);var text=NativeMapDocument.Load(output.TextPath);
                var action=text.Records().Single(row=>row.Category=="Project.ACTION");Require(action.Read(action.Fields.Single(field=>field.Name=="param5"))=="2","Reopened MAP changed the selected gate ID.");
                var editable=NativeMapDocument.Load(output.EditableCopyPath);
                Require(editable.Files.All(file=>originals.TryGetValue(file.ExportPath,out var bytes)&&file.Data.Span.SequenceEqual(bytes)),"Native source copy changed game-file bytes.");
                // I01/R19: game-file reload resolves the first physical match;
                // private working identities persist through DKM, not raw KWD.
                Require(Selected(editable)==editable.ScenarioGateChoices()[0].Key,"Raw native reload stopped following its source coordinate contract.");
                Target(native,2);Require(Project(native).Limitations.Any(loss=>loss.Code=="NativeEditorState"),"Export did not warn about the native working-state persistence boundary.");
                var report=File.ReadAllText(Path.Combine(root,"Export-notes.txt"));Require(report.Contains("editor-only working IDs",StringComparison.Ordinal)&&report.Contains("рабочие ID",StringComparison.Ordinal),"Export omitted its bilingual native-state warning.");
                Require(!TextMapFile.Project(native,TextMapScriptMode.OriginalNoScenarios).Text.Contains("ACTION_DESTROY_HERO_GATE",StringComparison.Ordinal),"Original no-scenario mode changed.");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,recursive:true);}
        });
    }
}
