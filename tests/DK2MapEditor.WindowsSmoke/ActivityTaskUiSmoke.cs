using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckActivityTasks(string directory, string language, bool native)
    {
        var source = NativeMapDocument.CreateNewMap(new("ActivityTask", "ActivityTask", 24, 24, PlaceHearts: true, NativeFormat: native));
        var schema = LegacyEventSchemas.All.Single(item => item.Name == "TRIGGER_TIMER");
        var record = native ? source.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Trigger"), new(0, 0))
            : source.AddLegacyRecord("Trigger", new(0, 0));
        source.SetEventType(record, schema);
        var input = source.SaveCopy(Path.Combine(directory, $"activity-task-input-{language}-{native}"));
        var shell = new MainWindow(new AppSettings(), persistSettings: false); shell.OpenMapFromPath(input);
        var workspace = (EditorWorkspace)shell.FindName("Editor"); var doc = workspace.Document!;
        var now = new ActivityLocalTime(2026, 10, ushort.MaxValue, 9, 1, 2, 3, 0);
        var initial = now; var clockReads = 0;
        var field = typeof(NativeMapDocument).GetField("activityStatistics", BindingFlags.Instance | BindingFlags.NonPublic)!;
        (field.GetValue(doc) as IDisposable)?.Dispose();
        using var session = new ActivityStatisticsSession(ActivityStatisticsPayload.New(initial, initial, initial), () => 0, () => { clockReads++; return now; });
        field.SetValue(doc, session);
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var history = doc.GetHistory().Undo.Count; var notices = new List<ScenarioNotice>(); shell.ScenarioNoticePresenter = notices.Add;
        void DateIs(ActivityLocalTime date, int reads) => Expect(doc.ReadActivityStatistics()!.CompileTime == date && clockReads == reads,
            "Task completion date/clock count differs from started-task boundary.");
        void Exact() => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history,
            "Task date changed document bytes or Undo history.");
        now = now with { Second = 10 };
        shell.ValidateDocument(doc); DateIs(initial, 0); Exact();
        shell.UnusedScriptPrompt = _ => UnusedScriptDecision.Keep;
        ((Button)shell.FindName("ValidateButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        FinishCompilerUiTask(shell);
        DateIs(now, 1); Exact();
        now = now with { Second = 11 };
        shell.UnusedScriptPrompt = _ => UnusedScriptDecision.Cancel;
        Expect(shell.RequestScenarioValidation() is null, "Canceled Test continued."); DateIs(now, 2); Exact();
        now = now with { Second = 12 };
        using (var pen = doc.BeginLivePenStroke(new(4, 2, 1)))
        {
            Expect(shell.RequestScenarioValidation() is null && shell.RequestCompileProject() is null, "Active pen admitted Test/Compile.");
            DateIs(now with { Second = 11 }, 2);
        }
        Exact();
        doc.Paint(MapDrawing.Rectangle(new(12, 12, 5, 5), true), new(14, 3, 1, 217));
        shell.UnusedScriptPrompt = _ => throw new InvalidOperationException("Invalid map asked cleanup.");
        Expect(shell.RequestScenarioValidation() is { Eligible: false }, "Failed terrain validation was accepted.");
        DateIs(now, 3); doc.Undo(); Exact();
        if (!native)
        {
            var basis = NativeMapDocument.CreateNewMap(new("ActivityBasis", "ActivityBasis", 24, 24, PlaceHearts: true, NativeFormat: true));
            var basisPath = basis.SaveCopy(Path.Combine(directory, $"activity-task-basis-{language}"));
            var basisBytes = Directory.GetFiles(Path.GetDirectoryName(basisPath)!, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
            var outputs = Directory.GetDirectories(directory).ToHashSet();
            var previous = now;
            now = now with { Second = 13 };
            shell.CompileLocationPrompt = () => null;
            Expect(shell.RequestCompileProject() is null, "Canceled location exported."); DateIs(previous, 3); Exact();
            shell.CompileLocationPrompt = () => { doc.Paint([new(20, 20)], new(4, 2, 1)); doc.Undo(); return new(basisPath, directory); };
            Expect(shell.RequestCompileProject() is null, "Stale location exported."); DateIs(previous, 3); Exact();
            var locationCalls = 0; var cleanupCalls = 0;
            shell.CompileLocationPrompt = () => { locationCalls++; return new(basisPath, directory); };
            shell.UnusedScriptPrompt = _ => { Expect(locationCalls == 1 && clockReads == 3, "Cleanup preceded location or completion occurred inside task."); cleanupCalls++; return UnusedScriptDecision.Cancel; };
            Expect(shell.RequestCompileProject() is null && cleanupCalls == 1, "Started cleanup cancellation not reached."); DateIs(now, 4); Exact();
            now = now with { Second = 14 };
            shell.UnusedScriptPrompt = _ => UnusedScriptDecision.Keep;
            shell.CompileLocationPrompt = () => new(Path.Combine(directory, "missing-task-basis.kwd"), directory);
            Expect(shell.RequestCompileProject() is null && notices.Last().Image == MessageBoxImage.Error, "Started basis failure did not report failure.");
            DateIs(now, 5); Exact();
            Expect(outputs.SetEquals(Directory.GetDirectories(directory)), "Canceled/failed task created output.");
            now = now with { Second = 15 };
            doc.Paint([new(20, 20)], new(1, 2, 3));
            var warningCalls = 0;
            shell.CompileLocationPrompt = () => new(basisPath, directory);
            shell.CompileWarningsPrompt = _ => { warningCalls++; return false; };
            Expect(shell.RequestCompileProject() is null && warningCalls == 1, "Warning cancellation did not reach actual task warnings.");
            DateIs(now, 6); doc.Undo(); Exact();
            Expect(outputs.SetEquals(Directory.GetDirectories(directory)), "Warning cancellation created output.");
            now = now with { Second = 16 };
            shell.CompileLocationPrompt = () => new(basisPath, directory); shell.CompileWarningsPrompt = _ => true;
            shell.UnusedScriptPrompt = _ =>
            {
                Expect(shell.RequestScenarioValidation() is null && shell.RequestCompileProject() is null && clockReads == 6,
                    "Reentrant command was admitted or completed the outer task early.");
                return UnusedScriptDecision.Keep;
            };
            Expect(shell.RequestCompileProject() is { } compiled && File.Exists(compiled), "Successful task did not export."); DateIs(now, 7); Exact();
            Expect(basisBytes.All(pair => File.ReadAllBytes(pair.Key).AsSpan().SequenceEqual(pair.Value)), "Task changed source basis files.");
        }
        else
        {
            now = now with { Second = 13 };
            Expect(shell.RequestCompileProject() is null, "Native map admitted project compilation."); DateIs(now with { Second = 12 }, 3); Exact();
        }
        var finalReads = clockReads;
        now = now with { Second = 20 };
        var replacement = NativeMapDocument.CreateNewMap(new("TaskReplacement", "TaskReplacement", 24, 24, NativeFormat: native));
        var replacementDate = replacement.ReadActivityStatistics()!.CompileTime;
        shell.UnusedScriptPrompt = _ => { workspace.Document = replacement; shell.Close(); return UnusedScriptDecision.Keep; };
        Expect(shell.RequestScenarioValidation() is null, "Closed/replaced task reused target authority.");
        DateIs(now, finalReads + 1); Exact();
        Expect(replacement.ReadActivityStatistics()!.CompileTime == replacementDate
            && shell.RequestScenarioValidation() is null && shell.RequestCompileProject() is null, "Old task updated replacement or closed shell accepted work.");
        DateIs(now, finalReads + 1); workspace.Document = null;
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} actual Test/Compile task dates: automatic validation excluded, successful/failed/canceled started tasks, active gesture and location boundaries, captured target after replacement/close, exact bytes/history");
    }
}
