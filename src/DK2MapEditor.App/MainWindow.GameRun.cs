using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using Microsoft.Win32;

namespace DK2MapEditor.App;

public sealed record GameRunLocation(string GameRoot, string? BasisPath = null);
public sealed record GameRunRequest(string ExecutablePath, string WorkingDirectory, string LevelName, string DescriptorPath);

public partial class MainWindow
{
    public Func<bool, GameRunLocation?>? GameRunLocationPrompt { get; set; }
    public Func<GameRunRequest, bool>? GameRunConfirmationPrompt { get; set; }
    public Func<GameRunRequest, bool>? GameRunLauncher { get; set; }
    public GameRunRequest? LastGameRunRequest { get; private set; }
    public GamePreviewOutput? LastGamePreview { get; private set; }

    private GameRunLocation? ChooseGameRunLocation(bool needsBasis)
    {
        var executable = new OpenFileDialog { Title = LocalizationService.Get("GameRun.ChooseGame"),
            Filter = "Dungeon Keeper 2 (DKII-DX.exe)|DKII-DX.exe", CheckFileExists = true,
            InitialDirectory = Editor.GameResources.Root ?? GetInitialDirectory() };
        if (executable.ShowDialog(this) != true) return null;
        var root = Path.GetDirectoryName(executable.FileName)!;
        if (!needsBasis) return new(root);
        var basis = new EditorPreviewFileDialog(EditorFileDialogKind.Map) { Title = LocalizationService.Get("Editor.CompileBase"),
            Filter = "Dungeon Keeper 2 (*.kwd)|*.kwd", CheckFileExists = true,
            InitialDirectory = Path.Combine(root, "Data", "Editor", "Maps") };
        return basis.ShowDialog(this) == true ? new(root, basis.FileName) : null;
    }

    public static ProcessStartInfo CreateGameRunStartInfo(GameRunRequest request)
    {
        var start = new ProcessStartInfo(request.ExecutablePath) { WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false, WindowStyle = ProcessWindowStyle.Normal };
        start.ArgumentList.Add("-q"); start.ArgumentList.Add("-level"); start.ArgumentList.Add(request.LevelName);
        return start;
    }
    private static bool StartGame(GameRunRequest request)
    { using var process = Process.Start(CreateGameRunStartInfo(request)); return process is not null; }

    private async void RunGame_Click(object sender, RoutedEventArgs args) => await RequestRunGameAsync();
    public bool RequestGameRunShortcut(Key key, ModifierKeys modifiers)
    {
        if (key != Key.E || modifiers != (ModifierKeys.Control | ModifierKeys.Shift)) return false;
        RequestRunGame(); return true;
    }

    /// <summary>Choose game, Test, compile a project, then launch its current
    /// bytes through DKII-DX.exe. The published preview owns all its names.</summary>
    public bool RequestRunGame()=>RequestRunGameCore(false).GetAwaiter().GetResult();
    /// <summary>Generate the preview and leave the task window ready for its
    /// explicit Run button. True means ready, not that a process has started.</summary>
    public Task<bool> RequestRunGameAsync()=>RequestRunGameCore(true);
    private async Task<bool> RequestRunGameCore(bool background)
    {
        if (scenarioOwnerClosed || scenarioCommandActive || document is not { } target
            || (!target.IsLegacyProject && target.MainFile.Signature != 0xDC)) return false;
        scenarioCommandActive = true;compilerUsesWindow=false; LastScenarioCheck = null; LastGameRunRequest = null; LastGamePreview = null;
        var taskStarted = false;var succeeded=false;Func<bool>? deferredRun=null; var title = LocalizationService.Get("Menu.RunGame");
        try
        {
            if (target.HasActivePenStroke || !Editor.CommitFields() || !ScenarioTargetCurrent(target)) return false;
            var locationContext = scenarioContextRevision; var locationRevision = target.EditRevision;
            var location = (GameRunLocationPrompt ?? ChooseGameRunLocation)(target.IsLegacyProject);
            if (location is null || locationContext != scenarioContextRevision || locationRevision != target.EditRevision
                || !ScenarioTargetCurrent(target)) return false;
            var root = Path.GetFullPath(location.GameRoot); var executable = Path.Combine(root, "DKII-DX.exe");
            if (!File.Exists(executable) || !Directory.Exists(Path.Combine(root, "Data", "Editor", "Maps")))
                throw new InvalidDataException(LocalizationService.Get("GameRun.InvalidGame"));
            var result = LastScenarioCheck = await PrepareScenarioCheck(target, ScenarioCheckPurpose.Run,background, () => taskStarted = true);
            if (result is null) return false;
            if (!result.Eligible || result.Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error))
            { CompilerFailureRows(result.Diagnostics,target);return false; }
            var context = scenarioContextRevision; var revision = target.EditRevision;
            bool Current() => context == scenarioContextRevision && revision == target.EditRevision && ScenarioTargetCurrent(target);
            CompilerPhase("Generate");var input=CompilerInput(target,background);
            var missingBasis=LocalizationService.Get("GameRun.MissingBasis");
            var native=await CompilerWork(background,()=>input.IsLegacyProject?input.CompileLegacyProject("PreviewSource",
                NativeMapDocument.Load(location.BasisPath??throw new InvalidDataException(missingBasis))):input);
            if(!Current())return false;
            CompilerPhase("Validate");var diagnostics=await CompilerWork(background,()=>NativeMapValidator.Validate(native,includeGameplay:true));
            if(!Current())return false;
            editableDiagnostics=diagnostics;RenderEditableDiagnostics();CompilerRows(result.Diagnostics.Concat(diagnostics).ToArray());
            if (diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error))
            { if(!compilerUsesWindow)CompilerFailureRows(diagnostics,target,Current);return false; }
            if(!ConfirmCompileDiagnostics(diagnostics,target,Current))return false;
            if (!Current()) return false;
            var preview=await CompilerWork(background,()=>native.PrepareGamePreview());
            if(!Current())return false;
            var request = new GameRunRequest(executable, root, preview.LevelName,
                Path.Combine(root, "Data", "Editor", "Maps", preview.LevelName + ".kwd"));
            var confirmed = GameRunConfirmationPrompt?.Invoke(request) ?? (DK2MapEditor.App.Editing.OriginalMessageBox.Show(this,
                LocalizationService.Get("GameRun.Confirm") + "\n\n" + request.DescriptorPath,
                title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);
            if (!confirmed || !Current()) return false;
            CompilerPhase("Write",request.DescriptorPath);
            LastGamePreview=await CompilerWork(background,()=>preview.Install(root));LastGameRunRequest=request;
            if(background)
            {
                deferredRun=()=>
                {
                    if(!Current())return false;
                    if(!(GameRunLauncher??StartGame)(request))throw new InvalidOperationException(LocalizationService.Get("GameRun.StartFailed"));
                    return true;
                };
                succeeded=true;return true;
            }
            if (!(GameRunLauncher ?? StartGame)(request)) throw new InvalidOperationException(LocalizationService.Get("GameRun.StartFailed"));
            succeeded=true;
            return true;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException
            or InvalidOperationException or OverflowException or FormatException or System.ComponentModel.Win32Exception or AggregateException)
        {
            PresentScenarioNotice(exception.Message + (LastGamePreview is null ? "" : "\n\n" + LastGamePreview.DescriptorPath), title, MessageBoxImage.Error);
            return false;
        }
        finally
        {
            FinishCompilerCommand(target,taskStarted,succeeded,deferredRun);
        }
    }
}
