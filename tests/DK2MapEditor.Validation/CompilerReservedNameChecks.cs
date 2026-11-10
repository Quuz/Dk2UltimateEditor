using System.Text.RegularExpressions;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class CompilerReservedNameChecks
{
    private const string GameName="DK2Preview_6a88240eaf814f78";
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static string Root()
    {var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!System.IO.File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;return root!.FullName;}
    private static string[] SourceNames()
    {
        var text=System.IO.File.ReadAllText(Path.Combine(Root(),"Source/Sources/Compiler/Compiler_Compile.c"));
        var body=Regex.Match(text,@"ReservedNames\[\]\s*=\s*\{([^}]+)\}").Groups[1].Value;
        var names=Regex.Matches(body,"\"([^\"]+)\"").Select(match=>match.Groups[1].Value).ToArray();
        Require(names.Length==63&&names.Distinct(StringComparer.OrdinalIgnoreCase).Count()==63,"Source reserved-name list differs.");return names;
    }
    private static NativeMapDocument New(string name="level1")=>NativeMapDocument.CreateNewMap(new(name,"Editable title",32,30));
    private static void Unchanged(NativeMapDocument document,Action action)
    {
        var bytes=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var revision=document.EditRevision;var dirty=document.HasChanges;var before=document.GetHistory();
        var notices=0;EventHandler changed=(_,_)=>notices++;document.Changed+=changed;document.HistoryChanged+=changed;
        try{action();}finally{document.Changed-=changed;document.HistoryChanged-=changed;}
        var after=document.GetHistory();Require(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&revision==document.EditRevision&&dirty==document.HasChanges&&notices==0
            &&before.ChangeCount==after.ChangeCount&&before.EditorChangeCount==after.EditorChangeCount&&before.SnapshotBytes==after.SnapshotBytes&&before.Undo.SequenceEqual(after.Undo)&&before.Redo.SequenceEqual(after.Redo),"Name check changed source/history.");
    }
    private static void Reject(NativeMapDocument document,string name,NativeMapDocument basis)
    {
        var rejected=false;
        try{_ = document.CompileLegacyProject(name,basis);}catch(InvalidDataException error){Require(error.Message.Contains(name,StringComparison.Ordinal),"Refusal did not identify output name.");rejected=true;}
        Require(rejected,"Reserved output stem was compiled: "+name);
    }
    private static MapDiagnostic[] Notices(NativeMapDocument document,bool gameplay=true)=>NativeMapValidator.Validate(document,includeGameplay:gameplay).Where(item=>item.Code=="DK2N046").ToArray();
    public static void Run(Action<string,Action> run)
    {
        run("compiler reserved names: exact case-insensitive lookup and actual filename fallback share one rule",()=>
        {
            foreach(var name in SourceNames())
                Require(CompilerMapNames.IsReserved(name)&&CompilerMapNames.IsReserved(name.ToUpperInvariant())
                    &&!CompilerMapNames.IsReserved(name+"_copy")&&!CompilerMapNames.IsReserved(" "+name)&&!CompilerMapNames.IsReserved(name+" "),"Name matching became a prefix/trim heuristic.");
            foreach(var pair in new[]{("level1.dkm","level1"),("a level1.dkm","a level1"),("level1.copy.dkm","level1.copy"),("level1.dkm.extra","level1.dkm"),("Карта.dkm","CompiledMap"),(new string('x',33)+".dkm","CompiledMap"),(".dkm","CompiledMap"),("","CompiledMap")})
                Require(CompilerMapNames.OutputStem(pair.Item1)==pair.Item2,"Output filename fallback changed: "+pair.Item1);
        });
        run("compiler reserved names: all63 literal source names and case variants are refused before output generation",()=>
        {
            var document=New("Allowed");var basis=NativeMapDocument.Create(32,30,"Basis");
            Unchanged(document,()=>{foreach(var name in SourceNames()){Reject(document,name,basis);Reject(document,name.ToUpperInvariant(),basis);}});
        });
        run("compiler reserved names: draft Test is Information and structural-only validation remains available",()=>
        {
            var document=New();Unchanged(document,()=>
            {
                var messages=Notices(document);Require(messages.Length==1&&messages[0].Severity==DiagnosticSeverity.Information&&messages[0].Message.Contains("level1",StringComparison.Ordinal),"Missing source Test notice.");
                Require(Notices(document,false).Length==0,"Structural draft check blocked editing stock-name source.");
            });
        });
        run("compiler reserved names: output filename is independent of the editable internal map title",()=>
        {
            var document=New();var draft=document.CreateMapGeneralDraft();
            document.ApplyMapGeneralSettings(document.UpdateMapGeneralSettings(draft,draft.Values.Select(item=>new MapGeneralInput(item.Key,item.Key=="Name"?GameName:item.Value))));
            Require(Notices(document).Length==1,"Changing only display title hid the reserved output filename.");
            var allowed=New(GameName);var allowedDraft=allowed.CreateMapGeneralDraft();allowed.ApplyMapGeneralSettings(allowed.UpdateMapGeneralSettings(allowedDraft,allowedDraft.Values.Select(item=>new MapGeneralInput(item.Key,item.Key=="Name"?"level1":item.Value))));
            Require(Notices(allowed).Length==0,"Reserved display title blocked a different actual output stem.");
        });
        run("compiler reserved names: real DKM Save As changes the reported stem; allowed seven-file export keeps display title",()=>
        {
            var directory=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2ReservedNames-"+Guid.NewGuid().ToString("N")));
            Require(directory.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture root.");Directory.CreateDirectory(directory);
            try
            {
                var document=New();var path=Path.Combine(directory,GameName+".dkm");using(var save=new EditorProjectSaveSession(document))save.SaveAs(path);
                var loaded=NativeMapDocument.Load(path);Require(Notices(loaded).Length==0,"Save As retained stale reserved filename diagnostic.");
                var basis=NativeMapDocument.Load("D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/level1.kwd");
                var compiled=loaded.CompileLegacyProject(GameName,basis);var saved=compiled.SaveCopy(Path.Combine(directory,"game"));
                Require(Directory.GetFiles(Path.GetDirectoryName(saved)!).Length==7,"Game export is not seven files.");
                var map=compiled.Records().Single(item=>item.Category=="Map");Require(map.Read(map.Fields.Single(field=>field.Name=="wstrMainName"))=="Editable title","Output stem changed internal title.");
            }
            finally{Directory.Delete(directory,recursive:true);}
        });
        run("compiler reserved names: native factory and raw SaveCopy remain editing routes for protected input names",()=>
        {
            var directory=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DK2ReservedDraft-"+Guid.NewGuid().ToString("N")));
            Require(directory.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Unexpected fixture root.");Directory.CreateDirectory(directory);
            try
            {
                var native=NativeMapDocument.CreateNewMap(new("level1","Draft",32,30,NativeFormat:true));
                Require(!native.IsLegacyProject&&Notices(native).Length==0,"Native editing factory was mistaken for publishing a compiled DKM.");
                var bytes=native.MainFile.Data.ToArray();var path=native.SaveCopy(Path.Combine(directory,"copy"));
                Require(NativeMapDocument.Load(path).MainFile.Data.Span.SequenceEqual(bytes),"Raw native SaveCopy changed protected-name input.");
            }
            finally{Directory.Delete(directory,recursive:true);}
        });
    }
}
