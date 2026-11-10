using System.Security.Cryptography;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class CompilationSnapshotChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Refuse(Action action){try{action();}catch(InvalidOperationException){return;}throw new InvalidOperationException("Active edit was captured for compilation.");}
    private static string Alias(NativeMapDocument doc)=>doc.ReadScenarioNames().Slots.Single(s=>s.Kind==ScenarioNameKind.Variable&&s.Index==4).StoredName;
    public static void Run(string repo)
    {
        foreach(var native in new[]{false,true})
        {
            var source=NativeMapDocument.CreateNewMap(new("Snapshot","Snapshot",26,24,PlaceHearts:true,NativeFormat:native));
            source.SetScenarioName(ScenarioNameKind.Variable,4,"Worker alias");
            var draft=source.CreateMapGeneralDraft();
            source.ApplyMapGeneralSettings(source.UpdateMapGeneralSettings(draft,MapGeneralSettingsChecks.Inputs(draft,"Version","13")));
            var before=source.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var history=source.GetHistory();var revision=source.EditRevision;
            var statistics=source.ReadActivityStatistics();var dirty=source.HasChanges;var changed=0;source.Changed+=(_,_)=>changed++;
            var copy=source.CreateCompilationSnapshot();
            Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&source.EditRevision==revision&&source.GetHistory().Undo.Count==history.Undo.Count
                &&source.GetHistory().Redo.Count==history.Redo.Count&&source.HasChanges==dirty&&changed==0,"Compilation capture edited the live map/history.");
            var afterStatistics=source.ReadActivityStatistics();
            Check(statistics is null?afterStatistics is null:afterStatistics is not null&&statistics.Encode().SequenceEqual((afterStatistics with{TotalTicks=statistics.TotalTicks,RealTicks=statistics.RealTicks}).Encode()),"Compilation capture altered live save/activity statistics.");
            Check(!copy.CanUndo&&!copy.CanRedo&&copy.TargetGameVersion==source.TargetGameVersion
                &&Alias(copy)==Alias(source)
                &&copy.MissingReferences.SequenceEqual(source.MissingReferences),"Snapshot lost working metadata or inherited history.");
            foreach(var (a,b) in source.Files.Zip(copy.Files))Check(!ReferenceEquals(a,b)&&a.SourcePath==b.SourcePath&&a.ExportPath==b.ExportPath&&a.Data.Span.SequenceEqual(b.Data.Span),"Snapshot file topology/bytes differ.");
            var diagnostics=NativeMapValidator.Validate(copy,includeGameplay:true).ToArray();
            var tile=copy.GetTile(2,2);var worker=Task.Run(()=>NativeMapValidator.Validate(copy,includeGameplay:true).ToArray());
            source.Paint([new(2,2)],tile with{Owner=(byte)(tile.Owner==2?3:2)});
            Check(worker.GetAwaiter().GetResult().SequenceEqual(diagnostics)&&copy.GetTile(2,2)==tile,"Live edit affected background snapshot validation.");source.Undo();
            var sourceChanges=changed;copy.Paint([new(3,3)],new(4,3,1));copy.SetScenarioName(ScenarioNameKind.Variable,4,"Independent");
            Check(changed==sourceChanges&&before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))
                &&Alias(source)=="Worker alias","Worker edits or events escaped into live source.");
            source.Edit("Snapshot guard",()=>Refuse(()=>source.CreateCompilationSnapshot()));
            using(var stroke=source.BeginLivePenStroke(new(4,2,1)))Refuse(()=>source.CreateCompilationSnapshot());
        }
        const string level="DK2Preview_6a88240eaf814f78";
        var project=NativeMapDocument.Load(Path.Combine(repo,"tests/fixtures/NativeRoundtrip",level+".dkm"));
        var basis=NativeMapDocument.Load(Path.Combine(repo,"artifacts/PreFinal/TestMap",level+".kwd"));
        var inputHashes=project.Files.Concat(basis.Files).Where(f=>File.Exists(f.SourcePath)).Select(f=>f.SourcePath).Distinct().ToDictionary(p=>p,p=>SHA256.HashData(File.ReadAllBytes(p)));
        var directory=Path.Combine(Path.GetTempPath(),"DK2CompilationSnapshot-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var copyProject=project.CreateCompilationSnapshot();
        var compiled=project.CompileLegacyProject(level,basis);var copied=copyProject.CompileLegacyProject(level,basis.CreateCompilationSnapshot());
        Dictionary<string,byte[]> Bundle(NativeMapDocument doc,string child)
        {
            var path=doc.SaveCopy(Path.Combine(directory,child));var maps=Path.GetDirectoryName(path)!;
            var files=Directory.GetFiles(maps);Check(files.Length==7,"Snapshot compilation lost the official seven-file format.");
            return files.ToDictionary(p=>Path.GetFileName(p)!,p=>File.ReadAllBytes(p));
        }
        var expected=Bundle(compiled,"original");
        foreach(var actual in new[]{Bundle(copied,"project-snapshot"),Bundle(compiled.CreateCompilationSnapshot(),"native-snapshot")})
            Check(expected.Count==actual.Count&&expected.All(p=>actual.TryGetValue(p.Key,out var data)&&data.SequenceEqual(p.Value)),"Background snapshot changed official map bytes or omitted retained definition payloads.");
        Check(inputHashes.All(p=>SHA256.HashData(File.ReadAllBytes(p.Key)).SequenceEqual(p.Value)),"Compilation snapshot changed an input file.");
        Console.WriteLine("COMPILATION SNAPSHOT DETAIL native/project live byte-history-statistics isolation, independent aliases/target/missing references, background validation during live edits, active-edit refusal; user's archived DKM and official native snapshot produce exactly the same seven files; no WPF/game/installed writes");
        // Keep the private output as failure evidence; no installed or fixture paths are written.
    }
}
