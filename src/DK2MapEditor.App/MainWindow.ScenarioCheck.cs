using System.IO;
using System.Windows;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using Microsoft.Win32;

namespace DK2MapEditor.App;

public sealed record ScenarioCheckResult(bool Eligible, bool Removed, IReadOnlyList<MapDiagnostic> Diagnostics);
public sealed record CompileLocation(string BasisPath, string OutputParent);
public sealed record ScenarioNotice(string Message, string Title, MessageBoxImage Image);

public partial class MainWindow
{
    private bool scenarioCommandActive;
    private bool scenarioOwnerClosed;
    private long scenarioContextRevision;
    private UnusedScriptConfirmationWindow? unusedScriptWindow;
    /// <summary>Null uses the real modal window. An explicit decision seam is
    /// provided for in-process tests of the same human-command route.</summary>
    public Func<UnusedScriptRequest, UnusedScriptDecision>? UnusedScriptPrompt { get; set; }
    public Func<CompileLocation?>? CompileLocationPrompt { get; set; }
    public Func<IReadOnlyList<MapDiagnostic>, bool>? CompileWarningsPrompt { get; set; }
    public Action<ScenarioNotice>? ScenarioNoticePresenter { get; set; }
    public ScenarioCheckResult? LastScenarioCheck { get; private set; }
    public string? LastCompiledPath { get; private set; }
    public UnusedScriptConfirmationWindow CreateUnusedScriptConfirmation(UnusedScriptRequest request) => new(request);
    private void InvalidateScenarioContext(object? sender = null, EventArgs? args = null)
    {
        scenarioContextRevision++; unusedScriptWindow?.Close(); unusedScriptWindow = null;compilerTaskWindow?.RefreshCurrent();
    }
    private UnusedScriptDecision ShowUnusedScriptConfirmation(UnusedScriptRequest request)
    {
        var window = CreateUnusedScriptConfirmation(request); unusedScriptWindow = window; window.Owner = this;
        try { window.ShowDialog(); return window.Decision; }
        finally { if (ReferenceEquals(unusedScriptWindow, window)) unusedScriptWindow = null; }
    }
    private bool ScenarioTargetCurrent(NativeMapDocument target) => !scenarioOwnerClosed
        && ReferenceEquals(document, target) && ReferenceEquals(Editor.Document, target);
    private void PresentScenarioNotice(string message, string title, MessageBoxImage image = MessageBoxImage.Information)
    {
        if (ScenarioNoticePresenter is { } presenter) presenter(new(message, title, image));
        else if(compilerUsesWindow)compilerTaskWindow?.AppendMessage(message);
        else DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, message, title, MessageBoxButton.OK, image);
    }
    private static IReadOnlyList<MapDiagnostic> ScenarioDiagnostics(NativeMapDocument target, UnusedScriptPlan plan)
    {
        // Keep full diagnostics visible, including unused malformed components.
        // Only the pre-consent eligibility gate excludes those script errors.
        var diagnostics = NativeMapValidator.Validate(target, includeGameplay: true).Concat(plan.Issues.Select(issue =>
            new MapDiagnostic(DiagnosticSeverity.Error, "UnusedScripts." + issue.Problem,
                $"Event #{issue.EventID}: {issue.Detail}", target.MainFile.SourcePath))).ToArray();
        return diagnostics;
    }
    private async Task<ScenarioCheckResult?> PrepareScenarioCheck(NativeMapDocument target, ScenarioCheckPurpose purpose,bool background, Action? taskStarted = null)
    {
        if (!ScenarioTargetCurrent(target) || target.HasActivePenStroke || !Editor.CommitFields() || !ScenarioTargetCurrent(target)) return null;
        taskStarted?.Invoke();
        BeginCompilerTask(purpose,target,background);CompilerPhase("Test");
        var plan = target.PlanUnusedScriptRemoval();
        var context=scenarioContextRevision;var revision=target.EditRevision;
        bool Current()=>context==scenarioContextRevision&&revision==target.EditRevision&&ScenarioTargetCurrent(target);
        var input=CompilerInput(target,background);
        var check=await CompilerWork(background,()=>
        {
            var copyPlan=background?input.PlanUnusedScriptRemoval():plan;
            return (Diagnostics:ScenarioDiagnostics(input,copyPlan),Eligible:copyPlan.CanApply&&!NativeMapValidator.Validate(input,includeGameplay:true,includeScripts:false).Any(item=>item.Severity==DiagnosticSeverity.Error));
        });
        if(!Current())return null;
        var diagnostics=check.Diagnostics;editableDiagnostics=diagnostics;RenderEditableDiagnostics();CompilerRows(diagnostics);
        // Original Test checks terrain/players/things before traversing roots.
        // Do not let a dangling UNUSED edge prevent offering its explicit removal.
        var eligible = check.Eligible;
        if (!eligible || !plan.HasWork) return new(eligible, false, diagnostics);
        var decision = (UnusedScriptPrompt ?? ShowUnusedScriptConfirmation)(new(purpose, plan));
        if (!Enum.IsDefined(decision) || decision == UnusedScriptDecision.Cancel || context != scenarioContextRevision
            || !ScenarioTargetCurrent(target)) return null;
        if (!Editor.CommitFields() || context != scenarioContextRevision || !ScenarioTargetCurrent(target)
            || !target.IsUnusedScriptPlanCurrent(plan)) return null;
        var removed = decision == UnusedScriptDecision.Remove;
        if (removed) target.ApplyUnusedScriptRemoval(plan);
        if (!ScenarioTargetCurrent(target)) return null;
        // Keep never filters export or normalizes a component. Remove changes
        // the live document; fresh full validation follows that real edit.
        plan = target.PlanUnusedScriptRemoval();context=scenarioContextRevision;revision=target.EditRevision;input=CompilerInput(target,background);
        diagnostics=await CompilerWork(background,()=>ScenarioDiagnostics(input,background?input.PlanUnusedScriptRemoval():plan));
        if(!Current())return null;
        editableDiagnostics=diagnostics;RenderEditableDiagnostics();CompilerRows(diagnostics);
        return new(plan.CanApply && !diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error), removed, diagnostics);
    }
    /// <summary>Explicit user Test, unlike automatic read-only ValidateDocument.
    /// Actual toolbar commands and integration tests share this guarded route.</summary>
    public ScenarioCheckResult? RequestScenarioValidation()=>RequestScenarioValidationCore(false).GetAwaiter().GetResult();
    public Task<ScenarioCheckResult?> RequestScenarioValidationAsync()=>RequestScenarioValidationCore(true);
    private async Task<ScenarioCheckResult?> RequestScenarioValidationCore(bool background)
    {
        if (scenarioOwnerClosed || scenarioCommandActive || document is not { } target) return null;
        scenarioCommandActive = true;compilerUsesWindow=false; LastScenarioCheck = null;
        var taskStarted = false;var succeeded=false;
        try
        {
            LastScenarioCheck=await PrepareScenarioCheck(target,ScenarioCheckPurpose.Test,background,()=>taskStarted=true);
            succeeded=LastScenarioCheck is {Eligible:true} result&&!result.Diagnostics.Any(row=>row.Severity==DiagnosticSeverity.Error);
            return LastScenarioCheck;
        }
        catch(Exception error)when(error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OverflowException or FormatException)
        {PresentScenarioNotice(error.Message,LocalizationService.Get("UnusedScripts.Purpose.Test"),MessageBoxImage.Error);return null;}
        finally
        {
            FinishCompilerCommand(target,taskStarted,succeeded);
        }
    }
    private CompileLocation? ChooseCompileLocation()
    {
        var basis = new EditorPreviewFileDialog(EditorFileDialogKind.Map) { Title = LocalizationService.Get("Editor.CompileBase"),
            Filter = "Dungeon Keeper 2 (*.kwd)|*.kwd", CheckFileExists = true, InitialDirectory = GetInitialDirectory() };
        if (basis.ShowDialog(this) != true) return null;
        var folder = new OpenFolderDialog { Title = LocalizationService.Get("Editor.ExportFolder"), Multiselect = false };
        return folder.ShowDialog(this) == true ? new(basis.FileName, folder.FolderName) : null;
    }
    /// <summary>Actual Compile menu route: live cleanup consent, unchanged Keep
    /// semantics, fresh validation, then NEW output only. Never edits the basis.</summary>
    public string? RequestCompileProject()=>RequestCompileProjectCore(false).GetAwaiter().GetResult();
    public Task<string?> RequestCompileProjectAsync()=>RequestCompileProjectCore(true);
    private async Task<string?> RequestCompileProjectCore(bool background)
    {
        if (scenarioOwnerClosed || scenarioCommandActive || document is not { IsLegacyProject: true } target) return null;
        scenarioCommandActive = true;compilerUsesWindow=false; LastScenarioCheck = null; LastCompiledPath = null;
        var taskStarted = false;var succeeded=false;
        var title = LocalizationService.Get("Menu.Compile");
        try
        {
            // Original Compile obtains the game/output path before starting
            // Compiler_Task (which then tests and generates). Canceling that
            // choice must neither prune scripts nor record task completion.
            if (target.HasActivePenStroke || !Editor.CommitFields() || !ScenarioTargetCurrent(target)) return null;
            var locationContext = scenarioContextRevision; var locationRevision = target.EditRevision;
            var location = (CompileLocationPrompt ?? ChooseCompileLocation)();
            if (location is null || locationContext != scenarioContextRevision || locationRevision != target.EditRevision
                || !ScenarioTargetCurrent(target)) return null;
            var result = LastScenarioCheck = await PrepareScenarioCheck(target, ScenarioCheckPurpose.Compile,background, () => taskStarted = true);
            if (result is null) return null;
            if (!result.Eligible || result.Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error))
            { CompilerFailureRows(result.Diagnostics,target);return null; }
            var current = target.PlanUnusedScriptRemoval(); var context = scenarioContextRevision;
            bool Current() => context == scenarioContextRevision && ScenarioTargetCurrent(target) && target.IsUnusedScriptPlanCurrent(current);
            if (!Current()) return null;
            var name = CompilerMapNames.OutputStem(target.MainFile.SourcePath);
            if (CompilerMapNames.IsReserved(name))
            { PresentScenarioNotice(LocalizationService.Get("Diagnostic.DK2N046") + "\n" + name, title, MessageBoxImage.Error); return null; }
            CompilerPhase("Generate");var input=CompilerInput(target,background);
            var compiled=await CompilerWork(background,()=>input.CompileLegacyProject(name,NativeMapDocument.Load(location.BasisPath)));
            if(!Current())return null;
            CompilerPhase("Validate");
            var diagnostics=await CompilerWork(background,()=>NativeMapValidator.Validate(compiled,includeGameplay:true));
            if(!Current())return null;
            editableDiagnostics=diagnostics;RenderEditableDiagnostics();CompilerRows(result.Diagnostics.Concat(diagnostics).ToArray());
            if (diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error))
            { if(!compilerUsesWindow)CompilerFailureRows(diagnostics,target,Current);return null; }
            if(!ConfirmCompileDiagnostics(diagnostics,target,Current))return null;
            if (!Current()) return null;
            var output = Path.Combine(location.OutputParent, "DK2-compiled-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..6]);
            CompilerPhase("Write",output);var exported=await CompilerWork(background,()=>compiled.SaveCopy(output));
            succeeded=true;LastCompiledPath=exported;
            PresentScenarioNotice(LocalizationService.Get("Editor.CompileSuccess") + "\n" + exported, title);
            return exported;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OverflowException or FormatException)
        { PresentScenarioNotice(exception.Message, title, MessageBoxImage.Error); return null; }
        finally
        {
            FinishCompilerCommand(target,taskStarted,succeeded);
        }
    }
}
