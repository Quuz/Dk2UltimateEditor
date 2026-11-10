using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static class Program
{
    static string artifacts = "", basisPath = "";
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static void Flush()
    {
        var frame = new DispatcherFrame(); Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false)); Dispatcher.PushFrame(frame);
    }
    static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    static void FinishTask(MainWindow shell)
    {
        var deadline=DateTime.UtcNow.AddSeconds(30);
        while(shell.IsCompilerTaskActive&&DateTime.UtcNow<deadline)Flush();
        Check(!shell.IsCompilerTaskActive&&shell.CurrentCompilerTaskWindow is {IsWorking:false},"Compilation task did not reach a terminal result.");
    }
    static void Render(FrameworkElement window, string path)
    {
        window.UpdateLayout(); Flush();
        var image = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        image.Render(window); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(path); encoder.Save(stream);
    }
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length != 2 || Directory.Exists(args[0])) throw new ArgumentException("Require fresh UI artifacts and complete packed basis.");
        artifacts = Path.GetFullPath(args[0]); basisPath = args[1]; Directory.CreateDirectory(artifacts);
        var app = new DK2MapEditor.App.App(); app.InitializeComponent(); app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            foreach (var language in new[] { "en", "ru" }) foreach (var native in new[] { false, true }) CheckRun(language, native);
            Console.WriteLine("GAME RUN UI CHECKS PASSED (24)"); return 0;
        }
        catch (Exception failure) { Console.WriteLine(failure); return 1; }
        finally { app.Shutdown(); }
    }
    static void CheckRun(string language, bool native)
    {
        LocalizationService.SetLanguage(language);
        var basis = NativeMapDocument.Load(basisPath);
        var source = NativeMapDocument.CreateNewMap(new("GameRunUi", "Game run UI", 24, 24, PlaceHearts: true, NativeFormat: native), basis);
        var root = Path.Combine(artifacts, language + (native ? "-native" : "-project")); Directory.CreateDirectory(root);
        var input = source.SaveCopy(Path.Combine(root, "source"));
        var inputHashes = Directory.GetFiles(Path.Combine(root, "source"), "*", SearchOption.AllDirectories).ToDictionary(path => path, Hash);
        var game = Path.Combine(root, "fake-game"); var maps = Path.Combine(game, "Data", "Editor", "Maps"); Directory.CreateDirectory(maps);
        var shared = basis.Files.Single(file => Path.GetFileName(file.ExportPath) == "GlobalVariables.kwd");
        var sharedPath = Path.Combine(game, shared.ExportPath); Directory.CreateDirectory(Path.GetDirectoryName(sharedPath)!);
        File.WriteAllBytes(sharedPath, shared.Data.ToArray()); var sharedHash = Hash(sharedPath);
        var executable = Path.Combine(game, "DKII-DX.exe"); File.WriteAllText(executable, "Test marker. The real launcher is intercepted."); var executableHash = Hash(executable);
        var ordinaryExecutable = Path.Combine(game, "DKII.exe"); File.WriteAllText(ordinaryExecutable, "Must not launch this executable."); var ordinaryExecutableHash = Hash(ordinaryExecutable);
        var sentinel = Path.Combine(maps, "InstalledMap.kwd"); File.WriteAllText(sentinel, "Installed map sentinel."); var sentinelHash = Hash(sentinel);
        var shell = new MainWindow(new AppSettings(), persistSettings: false) { DiscardChangesPrompt = () => true,
            ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000 };
        try
        {
            shell.OpenMapFromPath(input); shell.Show(); Flush(); Check(shell.IsLoaded, "Actual shell not loaded.");
            var editor = (EditorWorkspace)shell.FindName("Editor"); var document = editor.Document!;
            var menu = (MenuItem)shell.FindName("RunGameMenuItem"); Check(menu.IsEnabled, "Actual game command disabled for supported map.");
            var expected = language == "ru" ? "Запустить игру" : "Run game";
            Check(menu.Header?.ToString() == expected && menu.InputGestureText == "Ctrl+Shift+E", "Actual game command has wrong caption/gesture.");
            menu.IsSubmenuOpen = false;
            var notices = new List<ScenarioNotice>(); shell.ScenarioNoticePresenter = notices.Add;
            shell.CompileWarningsPrompt = _ => true; shell.UnusedScriptPrompt = _ => UnusedScriptDecision.Keep;
            shell.GameRunLocationPrompt = needsBasis => { Check(needsBasis == !native, "Unexpected basis requirement."); return new(game, native ? null : basisPath); };
            var bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var revision = document.EditRevision; var undo = document.GetHistory().Undo.Count;
            void Exact() => Check(bytes.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && document.EditRevision == revision
                && document.GetHistory().Undo.Count == undo, "Game run changed live bytes/history.");
            void Inputs() => Check(Hash(executable) == executableHash && Hash(ordinaryExecutable) == ordinaryExecutableHash && Hash(sentinel) == sentinelHash
                && Hash(sharedPath) == sharedHash && inputHashes.All(pair => Hash(pair.Key) == pair.Value), "Run changed an existing input.");
            int launches = 0, confirmations = 0;
            shell.GameRunConfirmationPrompt = request => { confirmations++; Check(!File.Exists(request.DescriptorPath), "Preview published before confirmation."); return true; };
            shell.GameRunLauncher = request =>
            {
                launches++; var start = MainWindow.CreateGameRunStartInfo(request);
                Check(start.FileName == executable && start.WorkingDirectory == game && !start.UseShellExecute
                    && start.ArgumentList.SequenceEqual(new[] { "-q", "-level", request.LevelName }), "Requested DX launch command/working directory differ.");
                var loaded = NativeMapDocument.Load(request.DescriptorPath);
                Check(loaded.MissingReferences.Count == 0 && loaded.Width == 24 && loaded.Height == 24, "Launcher received incomplete or stale preview.");
                return true;
            };
            menu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            FinishTask(shell);
            Check(shell.CurrentCompilerTaskWindow is {IsWorking:false,Succeeded:true}&&launches==0&&shell.IsEnabled,"Actual menu did not finish a deferred task or restore its owner.");
            Check(shell.CurrentCompilerTaskWindow!.PerformAction(),"Ready task did not launch on its explicit Run button.");
            Check(launches == 1 && confirmations == 1 && shell.LastGamePreview is not null && shell.LastGameRunRequest is not null
                && notices.All(notice => notice.Image != MessageBoxImage.Error), "Actual menu did not prepare and launch current map."); Exact(); Inputs();
            var first = shell.LastGamePreview!.CreatedFiles.ToDictionary(path => path, Hash);
            Check(shell.RequestGameRunShortcut(Key.E, ModifierKeys.Control | ModifierKeys.Shift) && launches == 2
                && shell.LastGamePreview!.CreatedFiles.All(path => !first.ContainsKey(path)) && first.All(pair => Hash(pair.Key) == pair.Value), "Shortcut overwrote first preview.");
            Check(!shell.RequestGameRunShortcut(Key.E, ModifierKeys.Control), "Unmodified source shortcut accepted."); Exact(); Inputs();
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Check(menu.Header?.ToString() != expected, "Menu failed live translation."); LocalizationService.SetLanguage(language);
            Render(shell, Path.Combine(root, "game-run-shell.png"));
            var fileMenu = (MenuItem)ItemsControl.ItemsControlFromItemContainer(menu);
            fileMenu.IsSubmenuOpen = true; Flush();
            var popup = PresentationSource.FromVisual(menu)?.RootVisual as FrameworkElement
                ?? throw new InvalidOperationException("Actual file menu popup was not loaded.");
            Check(popup.ActualWidth > 0 && popup.ActualHeight > 0, "Actual file menu popup has zero size.");
            Render(popup, Path.Combine(root, "game-run-file-menu.png")); fileMenu.IsSubmenuOpen = false; Flush();
            Console.WriteLine($"PASS {language} {(native ? "native" : "project")} actual game menu/shortcut, source argv/working directory, complete current preview, repeated output ownership and translation");

            var fileCount = Directory.GetFiles(maps).Length; var beforeLaunches = launches;
            shell.GameRunLocationPrompt = _ => null; Check(!shell.RequestRunGame(), "Canceled game chooser ran.");
            shell.GameRunLocationPrompt = _ => new(game, basisPath); shell.GameRunConfirmationPrompt = _ => false;
            Check(!shell.RequestRunGame() && launches == beforeLaunches && Directory.GetFiles(maps).Length == fileCount, "Cancel published files or started process."); Exact(); Inputs();
            shell.GameRunConfirmationPrompt = _ =>
            {
                var priorRevision = document.EditRevision;
                var draft = document.CreateMapGeneralDraft(); document.ApplyMapGeneralSettings(document.UpdateMapGeneralSettings(draft,
                    draft.Values.Select(row => new MapGeneralInput(row.Key, row.Key == "Name" ? "Changed during confirmation" : row.Value))));
                Check(document.EditRevision > priorRevision, "Stale-confirmation fixture did not actually change the document."); return true;
            };
            Check(!shell.RequestRunGame() && launches == beforeLaunches && Directory.GetFiles(maps).Length == fileCount, "Stale confirmation launched changed document.");
            document.Undo(); bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); revision = document.EditRevision; undo = document.GetHistory().Undo.Count; Inputs();
            Console.WriteLine($"PASS {language} {(native ? "native" : "project")} chooser/launch Cancel and stale-document confirmation produce no files or launch");

            shell.GameRunLocationPrompt = _ => new(Path.Combine(root, "invalid-game"), basisPath); shell.GameRunConfirmationPrompt = _ => true;
            Check(!shell.RequestRunGame() && shell.LastGamePreview is null && notices.Last().Image == MessageBoxImage.Error, "Missing executable/Maps did not fail clearly."); Exact();
            var ordinaryOnly = Path.Combine(root, "ordinary-only-game"); var ordinaryMaps = Path.Combine(ordinaryOnly, "Data", "Editor", "Maps");
            Directory.CreateDirectory(ordinaryMaps); var ordinaryOnlyExe = Path.Combine(ordinaryOnly, "DKII.exe");
            File.WriteAllText(ordinaryOnlyExe, "Only the non-DX executable exists."); var ordinaryOnlyHash = Hash(ordinaryOnlyExe);
            shell.GameRunLocationPrompt = _ => new(ordinaryOnly, basisPath);
            Check(!shell.RequestRunGame() && shell.LastGamePreview is null && launches == beforeLaunches
                && notices.Last().Image == MessageBoxImage.Error && notices.Last().Message.Contains("DKII-DX.exe", StringComparison.Ordinal)
                && Directory.GetFiles(ordinaryMaps).Length == 0 && Hash(ordinaryOnlyExe) == ordinaryOnlyHash,
                "Missing DX executable fell back to DKII.exe or published files."); Exact();
            if (!native)
            {
                shell.GameRunLocationPrompt = _ => new(game); Check(!shell.RequestRunGame() && shell.LastGamePreview is null, "Project ran without native definitions."); Exact();
                var badBasis = Path.Combine(root, "invalid-basis.kwd"); var corrupt = new byte[56];
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(corrupt, 0xDC); File.WriteAllBytes(badBasis, corrupt);
                var badHash = Hash(badBasis); shell.GameRunLocationPrompt = _ => new(game, badBasis);
                Check(!shell.RequestRunGame() && shell.LastGamePreview is null && notices.Last().Image == MessageBoxImage.Error,
                    "Malformed native basis escaped Run error presentation."); Exact();
                shell.CompileLocationPrompt = () => new(badBasis, root);
                ((MenuItem)shell.FindName("CompileMenuItem")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                FinishTask(shell);
                Check(shell.LastCompiledPath is null && notices.Last().Image == MessageBoxImage.Error && Hash(badBasis) == badHash,
                    "Malformed native basis escaped actual Compile error presentation or was modified."); Exact();
            }
            Check(launches == beforeLaunches && Directory.GetFiles(maps).Length == fileCount, "Invalid location wrote files or launched."); Inputs();
            Console.WriteLine($"PASS {language} {(native ? "native" : "project")} invalid game/basis failures preserve source and installed files");

            shell.GameRunLocationPrompt = _ => new(game, basisPath);
            shell.GameRunLauncher = request => { launches++; throw new System.ComponentModel.Win32Exception("Simulated process start failure."); };
            Check(!shell.RequestRunGame() && shell.LastGamePreview is not null && File.Exists(shell.LastGamePreview.DescriptorPath)
                && notices.Last().Message.Contains(shell.LastGamePreview.DescriptorPath, StringComparison.Ordinal), "Failed launch lost private output/report."); Exact(); Inputs();
            shell.GameRunLauncher = _ => { launches++; return true; }; shell.GameRunConfirmationPrompt = _ => { Check(!shell.RequestRunGame(), "Reentrant Run accepted."); return true; };
            Check(shell.RequestRunGame(), "Launcher failure left command locked."); Exact(); Inputs();
            Console.WriteLine($"PASS {language} {(native ? "native" : "project")} launcher error retains private output, reports its path, permits retry and rejects reentrancy");

            var type = LegacyEventSchemas.All.Single(item => item.Name == "ACTION_ALLIES");
            var unused = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0))
                : document.AddLegacyRecord("Action", new(0, 0)); document.SetEventType(unused, type);
            fileCount = Directory.GetFiles(maps).Length; beforeLaunches = launches; var beforeUnused = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            shell.UnusedScriptPrompt = request =>
            {
                Check(request.Purpose == ScenarioCheckPurpose.Run && request.Plan.Actions == 1, "Run omitted source unused-script purpose/count.");
                var window = shell.CreateUnusedScriptConfirmation(request); window.ShowInTaskbar = false; window.Left = -10000; window.Top = -10000;
                window.WindowStartupLocation = WindowStartupLocation.Manual; window.Show(); Flush();
                Check(((TextBlock)window.FindName("UnusedScriptsSummary")).Text.Contains(language == "ru" ? "Запуск игры" : "Game launch", StringComparison.Ordinal), "Run consent purpose not localized.");
                Render(window, Path.Combine(root, "game-run-unused-consent.png")); window.Close(); return UnusedScriptDecision.Cancel;
            };
            Check(!shell.RequestRunGame() && launches == beforeLaunches && Directory.GetFiles(maps).Length == fileCount
                && beforeUnused.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Unused-script Cancel changed map or launched."); Inputs();
            Console.WriteLine($"PASS {language} {(native ? "native" : "project")} actual bilingual Run cleanup consent and Cancel preserve scripts/files");

            editor.Document = null; shell.Close(); Check(!shell.RequestRunGame() && launches == beforeLaunches, "Closed owner launched."); Inputs();
            Console.WriteLine($"PASS {language} {(native ? "native" : "project")} closed owner cannot publish or launch a stale map");
        }
        finally { ((EditorWorkspace)shell.FindName("Editor")).Document = null; shell.Close(); }
    }
}
