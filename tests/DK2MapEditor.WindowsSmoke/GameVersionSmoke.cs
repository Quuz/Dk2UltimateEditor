using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckGameVersion(bool native, string language, string directory)
    {
        var source = NativeMapDocument.CreateNewMap(new("VersionUI", "VersionUI", 24, 24, PlaceHearts: true, NativeFormat: native));
        var creature = native ? source.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), new(8, 9), 3) : source.AddLegacyRecord("Creature", new(8, 9), 3);
        source.SetField(creature, creature.Fields.Single(item => item.Name == (native ? "bGlobalCreatureID" : "id")), "45");
        var type = LegacyEventSchemas.All.Single(item => item.Name == "ACTION_ADDCREATURE");
        var action = native ? source.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0)) : source.AddLegacyRecord("Action", new(0, 0));
        source.SetEventType(action, type); action = source.Records().Single(item => item.File == action.File && item.Offset == action.Offset);
        source.SetField(action, action.Fields.Single(item => item.Name == (native ? "Param1" : "param1")), "32");
        var input = source.SaveCopy(Path.Combine(directory, $"version-input-{language}-{native}"));
        var inputBytes = Directory.GetFiles(Path.GetDirectoryName(input)!, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        var shell = new MainWindow(new AppSettings(), persistSettings: false);
        try
        {
            shell.OpenMapFromPath(input); var workspace = (EditorWorkspace)shell.FindName("Editor"); var doc = workspace.Document!;
            var draft = doc.CreateMapGeneralDraft(); doc.ApplyMapGeneralSettings(doc.UpdateMapGeneralSettings(draft, draft.Values.Select(row => new MapGeneralInput(row.Key, row.Key == "Version" ? "13" : row.Value))));
            var bytes = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count; var revision = doc.EditRevision;
            void Exact() => Expect(bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)) && doc.GetHistory().Undo.Count == history && doc.EditRevision == revision && doc.TargetGameVersion == 13, "Version Test/Compile changed live map/history/target.");
            var diagnostics = shell.ValidateDocument(doc); Expect(diagnostics.Count(row => row.Code.StartsWith("DK2V", StringComparison.Ordinal)) == 2 && diagnostics.Where(row => row.Code.StartsWith("DK2V", StringComparison.Ordinal)).All(row => row.Severity == DiagnosticSeverity.Information), "Actual shell omitted non-failing version notices.");
            var list = (ItemsControl)shell.FindName("DiagnosticsList");
            object Notice(string code) => list.Items.Cast<object>().Single(row => Get<string>(row, "Code") == code);
            foreach (var code in new[] { "DK2V003", "DK2V004" })
            {
                Expect(Get<string>(Notice(code), "Message").StartsWith(LocalizationService.Get("Diagnostic." + code), StringComparison.Ordinal) && LocalizationService.Get("Diagnostic." + code) != "Diagnostic." + code, "Version notice not translated.");
                Expect(Get<Brush>(Notice(code), "Color") != (Brush)shell.FindResource("Brush.Error") && Get<Brush>(Notice(code), "Color") != (Brush)shell.FindResource("Brush.Warning"), "Version notice displayed as a hard error/warning.");
            }
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(Get<string>(Notice("DK2V003"), "Message").StartsWith(LocalizationService.Get("Diagnostic.DK2V003"), StringComparison.Ordinal), "Version notice language did not refresh."); LocalizationService.SetLanguage(language); Exact();
            var notices = new List<ScenarioNotice>(); shell.ScenarioNoticePresenter = notices.Add; shell.UnusedScriptPrompt = _ => UnusedScriptDecision.Keep;
            ((Button)shell.FindName("ValidateButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FinishCompilerUiTask(shell);
            Expect(shell.LastScenarioCheck is { Eligible: true } check && check.Diagnostics.Any(row => row.Code == "DK2V004") && notices.All(row => row.Image != MessageBoxImage.Error), "Actual Test treated source notices as errors."); Exact();
            if (!native)
            {
                var basis = NativeMapDocument.Create(24, 24, "VersionBasis"); var basisPath = basis.SaveCopy(Path.Combine(directory, "version-basis-" + language));
                shell.CompileLocationPrompt = () => new(basisPath, directory);
                shell.CompileWarningsPrompt = warnings => { Expect(warnings.All(row => row.Severity == DiagnosticSeverity.Warning && !row.Code.StartsWith("DK2V", StringComparison.Ordinal)), "Source notices incorrectly required warning approval."); return true; };
                ((MenuItem)shell.FindName("CompileMenuItem")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                FinishCompilerUiTask(shell);
                Expect(shell.LastCompiledPath is not null && File.Exists(shell.LastCompiledPath) && notices.All(row => row.Image != MessageBoxImage.Error), "Actual Compile failed on source informational requirements.");
                Expect(NativeMapDocument.Load(shell.LastCompiledPath!).TargetGameVersion == 17, "Compiled native reload did not restore source 1.70 default."); Exact();
                Expect(list.Items.Cast<object>().Any(row => Get<string>(row, "Code") == "DK2V004"), "Actual Compile report lost chosen working-target notices.");
            }
            doc.Undo(); Expect(doc.TargetGameVersion == 17 && !doc.HasChanges, "Target Undo failed to restore loaded baseline."); doc.Redo(); Expect(doc.TargetGameVersion == 13, "Target Redo failed."); doc.Undo();
            Expect(inputBytes.All(row => File.ReadAllBytes(row.Key).AsSpan().SequenceEqual(row.Value)), "Version workflow changed source files.");
        }
        finally { ((EditorWorkspace)shell.FindName("Editor")).Document = null; shell.Close(); }
        Console.WriteLine($"PASS WPF source game-version notices, bilingual Test/Compile, session undo and reload {language} {(native ? "native" : "project")}");
    }
}
