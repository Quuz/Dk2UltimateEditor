using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class OriginalTextExportChecks
{
    private static void Expect(bool condition,string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (InvalidDataException) { return; }
        throw new Exception("Invalid projected non-script data or supported graph accepted.");
    }
    private static void Set(NativeMapDocument document,NativeRecord record,string name,string value)
        => document.SetField(record,record.Fields.Single(field => field.Name == name),value);
    private static void Raw(NativeMapDocument document,NativeRecord record,string name,string value)
        => document.Edit("Malformed text-export fixture",() =>
        {
            var field = record.Fields.Single(field => field.Name == name);
            Expect(MemoryMarshal.TryGetArray(record.File.Data,out var buffer),"Fixture bytes unavailable.");
            field.Encode(value).CopyTo(buffer.Array!,buffer.Offset + record.Offset + field.Offset);
        });
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document,Dictionary<NativeFile,byte[]> snapshot)
    {
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]),"Text export changed source bytes.");
    }
    public static void Run()
    {
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root,"Source"))) root = Path.GetDirectoryName(root) ?? throw new Exception("Preserved source missing.");
        var source = File.ReadAllText(Path.Combine(root,"Source/Sources/Files/Files_Text.c"));
        var start = source.IndexOf("int Text_GenerateFile",StringComparison.Ordinal);
        var export = source[start..source.IndexOf("int Text_FillRoomsOptions",start,StringComparison.Ordinal)];
        var triggerSection = export[export.IndexOf("szTextMapTriggers",StringComparison.Ordinal)..];
        Expect(Regex.IsMatch(triggerSection,@"Files_WriteData\(FileHandle,szNumber,0,DATATYPE_TEXTNUMBER\)")
            && !Regex.IsMatch(triggerSection,@"\bMapTriggers\b"),"Original text export no longer proven to omit scenarios.");
        var numberWriter = File.ReadAllText(Path.Combine(root,"Source/Sources/Files/Files_Functions.c"));
        Expect(numberWriter.Contains("wsprintf(Number,(char *)Data,DataSize);",StringComparison.Ordinal),"Original zero argument formatting unverified.");
        var directory = Path.Combine(Path.GetTempPath(),"DK2OriginalText-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false,true }) foreach (var corruption in new[] { "valid","zero","duplicate","dangling-root","dangling-edge","cycle","unknown-type" })
            {
                var document = NativeMapDocument.CreateNewMap(new("OriginalText","OriginalText",32,20,NativeFormat: native));
                NativeRecord Add(string projectKind,string nativeType,TilePoint point) => native
                    ? document.AddRecord(RecordTemplate.All.Single(template => template.Structure == nativeType),point)
                    : document.AddLegacyRecord(projectKind,point);
                var creature = Add("Creature","GAMEKEEPER",new(2,2));
                _ = Add("Door","GAMEDOOR",new(3,3)); _ = Add("Area","GAMEAREA",new(5,5));
                var normal = Add("Object","GAMEOBJECT",new(4,4)); Set(document,normal,native ? "bObjectID" : "id",ValueCatalogues.ForTemplate("Object").First().Value);
                var special = Add("MagicalObject","GAMEOBJECT",new(6,6)); Set(document,special,native ? "bObjectID" : "id",ValueCatalogues.ForTemplate("MagicalObject").First().Value);
                var band = Add("Band","GAMEBAND",new(0,0));
                if (!document.BandMembers(band).Any(member => member.Active)) document.AddBandMember(band,14);
                var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
                var condition = Add("Trigger","Trigger",new(0,0)); document.SetEventType(condition,timer);
                Set(document,condition,native ? "Param1" : "param1","5"); Set(document,condition,native ? "Param5" : "param5","600");
                var second = Add("Trigger","Trigger",new(0,0)); document.SetEventType(second,timer);
                bool KeeperOne(ScriptOwner owner) => owner.Scope == 1 && (native ? owner.Record.Read(owner.Record.Fields.Single(field => field.Name == "bGlobalPlayer")) == "3" : owner.PlayerID == 3);
                var id = ScriptGraph.ID(condition); var owners = document.ScriptOwners().GroupBy(owner => owner.Scope).Select(group => group.Key == 1 ? group.First(KeeperOne) : group.First()).ToArray();
                Expect(owners.Select(owner => owner.Scope).Order().SequenceEqual(new uint[] { 1,2,4,8,16,32,64,128,256 }),"Fixture does not cover all nine scenario scopes.");
                foreach (var owner in owners) { var edge = document.EnsureScriptScope(owner); Set(document,edge.Owner,edge.Field.Name,id.ToString()); }
                condition = document.Records().Single(record => ScriptGraph.IsEvent(record) && ScriptGraph.ID(record) == id);
                second = document.Records().Where(ScriptGraph.IsEvent).Last();
                var playerRoot = document.FindScriptScope(document.ScriptOwners().Single(KeeperOne))!;
                switch (corruption)
                {
                    case "zero": Raw(document,condition,native ? "ID" : "id","0"); break;
                    case "duplicate": Raw(document,second,native ? "ID" : "id",id.ToString()); break;
                    case "dangling-root": Set(document,playerRoot.Owner,playerRoot.Field.Name,"65535"); break;
                    case "dangling-edge": Set(document,condition,native ? "NextOr" : "or","65535"); break;
                    case "cycle": Set(document,condition,native ? "NextOr" : "or",id.ToString()); break;
                    case "unknown-type": Set(document,condition,native ? "Type" : "ctrl","255"); break;
                }
                // Retain live redo across all read-only projections and publishing.
                document.Paint([new(10,10)],new(4,2,1)); document.Undo();
                var before = Snapshot(document); var canUndo = document.CanUndo; var canRedo = document.CanRedo; var dirty = document.HasChanges; var notifications = 0;
                document.Changed += (_,_) => notifications++;
                var ordinary = NativeMapValidator.Validate(document);
                if (corruption is "zero" or "duplicate" or "dangling-root" or "dangling-edge")
                { Expect(ordinary.Any(item => item.Severity == DiagnosticSeverity.Error),"Malformed fixture has no ordinary script error."); Reject(() => TextMapFile.Project(document)); }
                var projection = TextMapFile.Project(document,TextMapScriptMode.OriginalNoScenarios);
                Expect(projection.ScriptMode == TextMapScriptMode.OriginalNoScenarios && projection.Text.Contains("TRIGGERS\r\n0\r\n",StringComparison.Ordinal)
                    && !projection.Text.Contains("TRIGGER_TIME",StringComparison.Ordinal),"Original mode exported scenarios.");
                Expect(projection.Limitations.Any(loss => loss.Code == "OriginalNoScenarios" && loss.Count == 1)
                    && projection.Limitations.Single(loss => loss.Code == "Scripts").Count == 2
                    && projection.Limitations.Single(loss => loss.Code == "ScriptLinks").Count == 9,"Omitted events/owner links not fully reported.");
                var parsed = TextMapFile.Parse(projection.Text); var path = Path.Combine(directory,$"original-{native}-{corruption}");
                var result = SaveProjectionChecks.ExportText(path,document,TextMapScriptMode.OriginalNoScenarios);
                var text = NativeMapDocument.Load(result.TextPath); Expect(!text.Records().Any(ScriptGraph.IsEvent) && ScriptGraph.Build(text).Roots.All(edge => edge.Target == 0),"Original MAP retained script records or dangling owner links.");
                var complete = NativeMapDocument.Load(result.EditableCopyPath);
                if(!native)SaveProjectionChecks.RoundTrip(document,complete,before.ToDictionary(pair=>pair.Key.ExportPath,pair=>pair.Value));
                else foreach(var file in complete.Files)
                    Expect(file.Data.Span.SequenceEqual(document.Files.Single(candidate=>candidate.ExportPath==file.ExportPath).Data.Span),"Native complete copy normalized scenario/owner bytes.");
                if (!native)
                {
                    var again = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory,$"reference-{corruption}"));
                    Expect(SaveProjectionChecks.PreviousEquals(document,File.ReadAllBytes(result.EditableCopyPath)),"Project serialized complete copy differs from source serialization.");
                }
                var report = File.ReadAllText(Path.Combine(path,"Export-notes.txt"));
                Expect(report.Contains("NO scenarios",StringComparison.Ordinal) && report.Contains("БЕЗ сценариев",StringComparison.Ordinal) && report.Contains("ScriptLinks: 9",StringComparison.Ordinal),"Saved bilingual export report omitted explicit policy/losses.");
                Exact(document,before); Expect(canUndo == document.CanUndo && canRedo == document.CanRedo && dirty == document.HasChanges && notifications == 0,"Original export changed history, dirty state or events.");
                var published = Directory.EnumerateFiles(path,"*",SearchOption.AllDirectories).ToDictionary(file => file,File.ReadAllBytes);
                try { SaveProjectionChecks.ExportText(path,document,TextMapScriptMode.OriginalNoScenarios); throw new Exception("Export overwrote existing output."); } catch (IOException) { }
                foreach (var file in published) Expect(File.ReadAllBytes(file.Key).AsSpan().SequenceEqual(file.Value),"Rejected export changed published files.");
                // Graph bypass is limited to an explicit projection, not global validation or non-script errors.
                var coordinate = creature.Fields.Single(field => native ? field.Name.EndsWith("LocationX",StringComparison.Ordinal) : field.Name == "x");
                Set(document,creature,coordinate.Name,"1024"); Reject(() => TextMapFile.Project(document,TextMapScriptMode.OriginalNoScenarios)); document.Undo(); Exact(document,before);
                try { TextMapFile.Project(document,(TextMapScriptMode)99); throw new Exception("Unknown text export mode accepted."); } catch (ArgumentOutOfRangeException) { }
                if (corruption == "valid")
                {
                    var supported = TextMapFile.Project(document); Expect(supported.ScriptMode == TextMapScriptMode.SupportedScenarios && supported.Text.Contains("TRIGGER_TIME 600",StringComparison.Ordinal)
                        && !supported.Limitations.Any(loss => loss.Code == "OriginalNoScenarios"),"Default supported-scenario workflow changed.");
                }
            }
            Expect(!Directory.EnumerateDirectories(directory,".dk2-text-*").Any(),"Failed original export left staging directories.");
        }
        finally { Directory.Delete(directory,true); }
    }
}
