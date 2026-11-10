using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class NativeMissingGateConversionChecks
{
    private const string GameName="DK2Preview_6a88240eaf814f78";
    private static readonly Lazy<NativeMapDocument> basis=new(()=>NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd"));
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Set(NativeMapDocument document,NativeRecord record,string name,string value)
        =>document.SetField(record,record.Fields.Single(field=>field.Name==name),value);
    private static NativeRecord Action(NativeMapDocument document)=>document.Records().Single(record=>EntityReferences.EventType(record)?.Name=="ACTION_DESTROYGATE");
    private static NativeRecord Gate(NativeMapDocument document,int x,int y)
    {var gate=document.AddRecord(RecordTemplate.All.Single(row=>row.Structure=="GAMEGATE"),new(x,y),1);Set(document,gate,"bGateType","33");return gate;}
    private static NativeMapDocument Native(ushort x=9,ushort y=12,bool knownGate=false)
    {
        var document=NativeMapDocument.Create(32,30,GameName,basis.Value);
        if(knownGate)Gate(document,8,11);
        var action=document.AddRecord(RecordTemplate.All.Single(row=>row.Structure=="Action"),new(0,0));
        document.SetEventType(action,LegacyEventSchemas.All.Single(type=>type.Name=="ACTION_DESTROYGATE"));action=Action(document);
        Require(MemoryMarshal.TryGetArray(action.File.Data,out var bytes),"Fixture bytes unavailable.");
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.Array!.AsSpan(bytes.Offset+action.Offset+12),x);BinaryPrimitives.WriteUInt16LittleEndian(bytes.Array!.AsSpan(bytes.Offset+action.Offset+14),y);
        return document;
    }
    private static PreparedProjectConversion Prepare(NativeMapDocument document)
    {
        var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var revision=document.EditRevision;var dirty=document.HasChanges;var before=document.GetHistory();
        var changes=0;EventHandler changed=(_,_)=>changes++;document.Changed+=changed;document.HistoryChanged+=changed;
        PreparedProjectConversion conversion;
        try{conversion=document.PrepareProjectConversion();}finally{document.Changed-=changed;document.HistoryChanged-=changed;}
        var after=document.GetHistory();Require(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&revision==document.EditRevision&&dirty==document.HasChanges&&changes==0
            &&before.ChangeCount==after.ChangeCount&&before.EditorChangeCount==after.EditorChangeCount&&before.SnapshotBytes==after.SnapshotBytes&&before.Undo.SequenceEqual(after.Undo)&&before.Redo.SequenceEqual(after.Redo),"Preparation changed native bytes/history.");
        return conversion;
    }
    private static void Reported(PreparedProjectConversion conversion)
    {
        Require(!conversion.HasBlockingIssues,"Missing gate remained a blocking conversion error: "+string.Join(";",conversion.Issues));
        Require(conversion.Issues.Count(issue=>issue.Code=="MissingDestroyGate"&&issue.Severity==ProjectConversionSeverity.Loss)==1,"Missing gate conversion did not report its coordinate-loss policy.");
    }
    private static uint Target(NativeMapDocument project)=>uint.Parse(Action(project).Read(Action(project).Fields.Single(field=>field.Name=="param5")));
    private static TilePoint Wire(NativeMapDocument document)
    {var record=Action(document);return new(BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[(record.Offset+12)..]),BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[(record.Offset+14)..]));}
    private static string Temp(string suffix)
    {var path=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2MissingGate-"+suffix+"-"+Guid.NewGuid().ToString("N")));Require(path.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture root.");Directory.CreateDirectory(path);return path;}
    private static (NativeMapDocument Project,string Archive) Save(NativeMapDocument native,string directory)
    {
        using var save=new EditorSaveSession(native);var path=Path.Combine(directory,GameName+".dkm");var plan=save.PrepareSaveAs(path);
        Require(!plan.HasBlockingIssues&&plan.NeedsLossConsent&&plan.RetainsNativeSource,"Save did not use the explicit loss/retention boundary.");
        Require(!save.Save(plan,false)&&!System.IO.File.Exists(path)&&save.LastSave is null,"Declining loss consent wrote or adopted conversion.");
        Require(save.Save(plan,true),"Accepted conversion was not saved.");return(NativeMapDocument.Load(path),save.LastSave!.SourceArchives.Single());
    }
    public static void Run(Action<string,Action> run)
    {
        run("native missing gate conversion: absent raw coordinates use source ID0 with Loss, not a hard refusal",()=>
        {
            foreach(var point in new[]{new TilePoint(9,12),new TilePoint(1,1),new TilePoint(0,65535)})
            {
                var native=Native((ushort)point.X,(ushort)point.Y);var conversion=Prepare(native);Reported(conversion);
                Require(Target(conversion.Project)==0,"Absent source coordinates acquired a fabricated gate ID.");
                var output=conversion.Project.CompileLegacyProject(GameName,basis.Value);Require(Wire(output)==new TilePoint(1,1),"Bare DKM did not use source missing-gate fallback.");
            }
        });
        run("native missing gate conversion: deleted working ID survives beside another gate at fallback coordinates",()=>
        {
            var native=Native(9,12,knownGate:true);var selected=native.Records().Single(record=>record.Category=="GAMEGATE");Gate(native,0,0);
            Require(native.SelectedScenarioGate(native.CreateScenarioParameterDraft(Action(native))) is not null,"Fixture did not bind the original gate.");
            native.DeleteRecord(selected);Require(native.SelectedScenarioGate(native.CreateScenarioParameterDraft(Action(native))) is null,"Deleted gate acquired another owner.");
            var conversion=Prepare(native);Reported(conversion);Require(Target(conversion.Project)==1,"Deleted source logical ID was reset to zero.");
            var output=conversion.Project.CompileLegacyProject(GameName,basis.Value);
            Require(Wire(output)==new TilePoint(1,1)&&output.SelectedScenarioGate(output.CreateScenarioParameterDraft(Action(output))) is null,"Fallback acquired the unrelated gate at origin.");
        });
        run("native missing gate conversion: explicit loss consent saves original files and report in the companion",()=>
        {
            var native=Native();var originals=native.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray());var directory=Temp("save");
            try
            {
                var(project,path)=Save(native,directory);Require(Target(project)==0,"Real Save/reopen changed the missing target ID.");var archive=EditorSourceArchive.Read(path);
                Require(archive.Issues.Any(issue=>issue.Code=="MissingDestroyGate"&&issue.Severity==ProjectConversionSeverity.Loss),"Companion dropped the loss report.");
                foreach(var pair in originals)Require(archive.SourceFiles.Single(file=>file.ExportPath==pair.Key).Bytes.AsSpan().SequenceEqual(pair.Value),"Companion changed an original native file.");
                var compiled=project.CompileLegacyProject(GameName,basis.Value);Require(Wire(compiled)==new TilePoint(9,12),"Unedited missing raw payload was not retained from its original archive.");
            }
            finally{Directory.Delete(directory,recursive:true);}
        });
        run("native missing gate conversion: archive cannot restore stale coordinates onto a newly inserted unrelated gate",()=>
        {
            var directory=Temp("collision");try
            {
                var(project,_)=Save(Native(),directory);project.AddLegacyRecord("Gate",new(8,11));
                var output=project.CompileLegacyProject(GameName,basis.Value);
                Require(Target(project)==0&&Wire(output)==new TilePoint(1,1),"Archive reactivated a formerly missing coordinate target against a new gate.");
                Require(output.SelectedScenarioGate(output.CreateScenarioParameterDraft(Action(output))) is null,"Working identity acquired the new unrelated gate.");
                project.Undo();Require(Wire(project.CompileLegacyProject(GameName,basis.Value))==new TilePoint(9,12),"Undo did not restore exact original raw retention.");
                project.Redo();Require(Wire(project.CompileLegacyProject(GameName,basis.Value))==new TilePoint(1,1),"Redo restored obsolete archived target bytes.");
            }
            finally{Directory.Delete(directory,recursive:true);}
        });
        run("native missing gate conversion: known duplicate coordinates still use the first physical source gate",()=>
        {
            var native=Native(knownGate:true);Gate(native,8,11);var conversion=Prepare(native);
            Require(!conversion.HasBlockingIssues&&!conversion.Issues.Any(issue=>issue.Code=="MissingDestroyGate")&&Target(conversion.Project)==1,"Known coordinate lookup stopped using first physical source match.");
        });
        run("native missing gate conversion: malformed graph remains blocking and unchanged",()=>
        {
            var native=Native();var record=native.AddRecord(RecordTemplate.All.Single(row=>row.Structure=="Trigger"),new(0,0));
            var id=record.Read(record.Fields.Single(field=>field.Name=="ID"));Set(native,record,"NextOr",id);
            var conversion=Prepare(native);Require(conversion.HasBlockingIssues&&conversion.Issues.Any(issue=>issue.Code=="CyclicEventGraph"&&issue.Severity==ProjectConversionSeverity.Error),"Missing-gate policy weakened the graph conversion guard.");
        });
    }
}
