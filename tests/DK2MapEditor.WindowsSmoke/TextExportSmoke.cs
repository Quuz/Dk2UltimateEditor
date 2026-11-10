using System.IO;
using System.Reflection;
using System.Windows.Controls;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckTextExportModes(NativeMapDocument document,string language,string directory)
    {
        var window = new MainWindow(new AppSettings(),persistSettings: false);
        var documentField = typeof(MainWindow).GetField("document",BindingFlags.Instance | BindingFlags.NonPublic)!;
        var workspace = (EditorWorkspace)window.FindName("Editor");
        var parent = (MenuItem)window.FindName("TextExportMenuItem");
        var supported = (MenuItem)window.FindName("TextExportSupportedMenuItem"); var original = (MenuItem)window.FindName("TextExportOriginalMenuItem");
        var prepare = typeof(MainWindow).GetMethod("PrepareTextExport",BindingFlags.Instance | BindingFlags.NonPublic)!;
        var warning = typeof(MainWindow).GetMethod("TextExportWarning",BindingFlags.Static | BindingFlags.NonPublic)!;
        void Refresh() => typeof(MainWindow).GetMethod("Document_Changed",BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window,[window,EventArgs.Empty]);
        TextMapProjection Preview(MenuItem command) => (TextMapProjection)prepare.Invoke(window,[command])!;
        string Warning(TextMapProjection projection) => (string)warning.Invoke(null,[projection])!;
        void Set(NativeRecord record,string name,string value) => document.SetField(record,record.Fields.Single(field => field.Name == name),value);
        void Refuses(MenuItem command,Type exception)
        {
            try { Preview(command); } catch (TargetInvocationException failed) when (exception.IsInstanceOfType(failed.InnerException)) { return; }
            throw new InvalidOperationException("Actual text export menu preparation accepted an invalid operation.");
        }
        try
        {
            Expect(!parent.IsEnabled && parent.Items.Count == 2 && parent.Items.Contains(supported) && parent.Items.Contains(original),"Text export choices/closed-map guard absent.");
            documentField.SetValue(window,document); workspace.Document = document; Refresh();
            Expect(parent.IsEnabled && supported.IsEnabled && original.IsEnabled,"Text export submenu is not enabled for an editable document.");
            var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
            var owner = document.ScriptOwners().Single(owner => owner.Scope == 1 && (document.IsLegacyProject ? owner.PlayerID == 3
                : owner.Record.Read(owner.Record.Fields.Single(field => field.Name == "bGlobalPlayer")) == "3"));
            var edge = document.EnsureScriptScope(owner); var trigger = document.InsertScriptEvent(edge.Owner,edge.Field,timer);
            Set(trigger,document.IsLegacyProject ? "param1" : "Param1","5"); Set(trigger,document.IsLegacyProject ? "param5" : "Param5","600");
            document.Paint([new(10,10)],new(4,2,1)); document.Undo();
            var before = document.Files.ToDictionary(file => file,file => file.Data.ToArray()); var undo = document.CanUndo; var redo = document.CanRedo; var dirty = document.HasChanges;
            var normal = Preview(supported); var classic = Preview(original);
            Expect(normal.ScriptMode == TextMapScriptMode.SupportedScenarios && normal.Text.Contains("TRIGGER_TIME 600",StringComparison.Ordinal)
                && classic.ScriptMode == TextMapScriptMode.OriginalNoScenarios && classic.Text.Contains("TRIGGERS\r\n0\r\n",StringComparison.Ordinal),"Actual menu choices did not select distinct scenario policies.");
            Expect(!Warning(normal).Contains(LocalizationService.Get("TextMap.Loss.OriginalNoScenarios"),StringComparison.Ordinal)
                && Warning(classic).Contains(LocalizationService.Get("TextMap.Loss.OriginalNoScenarios"),StringComparison.Ordinal),"Original-policy warning absent or incorrectly applied to supported mode.");
            Expect(Warning(classic).Contains(LocalizationService.Get("TextMap.Confirm"),StringComparison.Ordinal) && Warning(classic).Contains("EditableCopy",StringComparison.Ordinal),"Confirmation failed to identify mandatory complete snapshot.");
            foreach (var activeLanguage in new[] { language == "en" ? "ru" : "en",language })
            {
                LocalizationService.SetLanguage(activeLanguage); workspace.RefreshLanguage();
                Expect(original.Header as string == LocalizationService.Get("TextMap.Mode.Original") && supported.Header as string == LocalizationService.Get("TextMap.Mode.Supported")
                    && parent.Header as string == LocalizationService.Get("Menu.TextExport"),"Live resource translation left stale text export menu labels.");
                Expect(Warning(classic).Contains(LocalizationService.Get("TextMap.Loss.OriginalNoScenarios"),StringComparison.Ordinal),"Confirmation losses not translated.");
            }
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]),"Text export preview/translation changed source bytes.");
            Expect(undo == document.CanUndo && redo == document.CanRedo && dirty == document.HasChanges,"Text export preview changed history/dirty state.");
            Refuses(new MenuItem(),typeof(ArgumentException));
            edge = document.FindScriptScope(owner)!; Set(edge.Owner,edge.Field.Name,"65535");
            Refuses(supported,typeof(InvalidDataException)); classic = Preview(original);
            var snapshot = document.Files.ToDictionary(file => file.ExportPath,file => file.Data.ToArray());
            var exported = SaveProjectionChecks.ExportText(Path.Combine(directory,$"original-menu-{language}-{document.IsLegacyProject}"),document,classic.ScriptMode);
            var reopened = NativeMapDocument.Load(exported.EditableCopyPath);
            // Project Save updates the complete FIRST KSTS through the original
            // writer. Verify every output byte, section/header/order and caller
            // snapshot rather than excluding statistics or requiring stale bytes.
            if (document.IsLegacyProject) SaveProjectionChecks.RoundTrip(document,reopened,snapshot);
            else
            {
                Expect(reopened.Files.Count==snapshot.Count,"Native complete copy added or dropped a file.");
                foreach (var file in reopened.Files)
                    Expect(file.Data.Span.SequenceEqual(snapshot[file.ExportPath]),"Original menu policy normalized stored dangling graph in complete copy.");
            }
            Expect(document.Files.Count==snapshot.Count&&document.Files.All(file=>file.Data.Span.SequenceEqual(snapshot[file.ExportPath])),"Text export changed editable dangling graph/source buffers.");
            Console.WriteLine($"SOURCE TEXT SNAPSHOT {language} {(document.IsLegacyProject?"project":"native")} complete-envelope ordered-sections caller-buffers dangling-graph save-statistics");
            Expect(!NativeMapDocument.Load(exported.TextPath).Records().Any(ScriptGraph.IsEvent),"Original menu policy emitted scripts into MAP.");
            var place = document.IsLegacyProject ? document.AddLegacyRecord("Creature",new(2,2))
                : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEKEEPER"),new(2,2));
            Set(place,document.IsLegacyProject ? "x" : place.Fields.Single(field => field.Name.EndsWith("LocationX",StringComparison.Ordinal)).Name,"1024");
            Refuses(original,typeof(InvalidDataException)); document.Undo();
            documentField.SetValue(window,null); workspace.Document = null; Refresh();
            Expect(!parent.IsEnabled && !supported.IsEnabled && !original.IsEnabled,"Closed map retained text export authority."); Refuses(original,typeof(InvalidOperationException));
            Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} actual text-export submenu, explicit original policy, bilingual warning, malformed graph and exact complete copy");
        }
        finally { documentField.SetValue(window,null); workspace.Document = null; window.Close(); }
    }
}
