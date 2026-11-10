using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DK2MapEditor.App;
using DK2MapEditor.App.Settings;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    // In-process WPF integration tests; individual modal tests open real windows.
    // Execution requires UI authorization. No user settings are written.
    [STAThread]
    private static int Main(string[] args)
    {
        var manualHistory=args.FirstOrDefault()=="--manual-history-memory-notice";
        if(manualHistory && (args.Length!=2 || args[1] is not ("ru" or "en")))
        {Console.Error.WriteLine("Usage: --manual-history-memory-notice ru|en");return 2;}
        DK2MapEditor.App.App app=manualHistory||args.SequenceEqual(new[]{"--workspace-language-headers-only"})||args.SequenceEqual(new[]{"--creature-jobs-request-only"})||args.SequenceEqual(new[]{"--scenario-embedded-owner-only"})||args.SequenceEqual(new[]{"--camera-library-mcp-only"})?new DK2MapEditor.App.App(startEditorOnStartup:false):new DK2MapEditor.App.App();app.InitializeComponent();
        // Closing an offscreen shell must not enqueue application shutdown.
        // Actual DataGrid commits pump the dispatcher; without this setting
        // they execute that old shutdown and lose Application.Current.
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        if(manualHistory)return RunManualHistoryMemoryNotice(args[1]);
        var directory = Path.Combine(Path.GetTempPath(), "DK2MapEditor.WindowsSmoke", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            if(args.SequenceEqual(new[]{"--camera-library-mcp-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {LocalizationService.SetLanguage(language);foreach(var native in new[]{false,true})CheckCameraLibraryMcp(directory,native,language);}
                Console.WriteLine("CAMERA LIBRARY MCP:4 suites/8 real modal libraries PASS");return 0;
            }
            if(args.SequenceEqual(new[]{"--scenario-embedded-owner-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {LocalizationService.SetLanguage(language);foreach(var native in new[]{false,true})CheckScenarioEmbeddedOwners(native,language);}
                Console.WriteLine("SCENARIO EMBEDDED OWNERS:4 suites/24 real modal children PASS");return 0;
            }
            if(args.SequenceEqual(new[]{"--workspace-language-headers-only"}))
            {
                foreach(var native in new[]{false,true})CheckWorkspaceLanguageHeaders(native);
                Console.WriteLine("WORKSPACE LANGUAGE HEADERS: project/native EN -> RU -> EN; rendered headers and full document/history invariants");return 0;
            }
            if(args.SequenceEqual(new[]{"--hero-band-display-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckHeroBandDisplay(native,language);
                }
                Console.WriteLine("HERO BAND DISPLAY UI:4 prepared suites; physical selector, scroll, icons and DPI remain manual");return 0;
            }
            if(args.SequenceEqual(new[]{"--reward-destination-images-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckRewardDestinationImages(native,language);
                }
                Console.WriteLine("REWARD DESTINATION UI:4 prepared suites; physical font/DPI/popup/keyboard acceptance remains manual");return 0;
            }
            if(args.SequenceEqual(new[]{"--map-palette-images-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckMapPaletteImages(native,language);
                }
                Console.WriteLine("MAP PALETTE UI:4 prepared suites; physical selection, scroll and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--scenario-band-previews-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckScenarioBandPreviews(native,language);
                }
                Console.WriteLine("SCENARIO BAND PREVIEWS:4 prepared suites; actual popup/keyboard/images/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--scenario-catalogue-images-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckScenarioCatalogueImages(native,language);
                }
                Console.WriteLine("SCENARIO CATALOGUE IMAGES:4 prepared suites; actual windows and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-spell-usage-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureSpellUsage(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE SPELL USAGE:4 prepared suites; actual list/bitmap/font/scroll/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--global-callback-transition-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckGlobalCallbackTransitions(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("GLOBAL CALLBACK TRANSITIONS:4 prepared suites; private Save owner/source/close boundaries; actual windows require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-resistance-slider-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureResistanceSliders(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE RESISTANCE SLIDERS:4 prepared suites; actual drag/keyboard/focus/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--projectile-child-input-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckProjectileChildInputs(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("PROJECTILE CHILD INPUT:4 prepared suites; actual hold/focus/keyboard/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--global-element-input-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckGlobalElementInputs(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("GLOBAL ELEMENT INPUT:4 prepared suites across5 standalone/shared pages; actual repeat/focus/keyboard/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-job-input-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureJobInputs(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE JOB INPUT:4 prepared suites; actual repeat/focus/keyboard/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-auxiliary-input-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureAuxiliaryInputs(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE AUXILIARY INPUT:4 prepared suites; actual repeat/focus/keyboard/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-attack-input-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureAttackInputs(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE ATTACK INPUT:4 prepared suites; actual repeat/keyboard/caret/focus/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-attribute-input-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureAttributeInputs(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE ATTRIBUTE INPUT:4 prepared suites; actual focus/caret/spin/Tab/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-parameter-input-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureParameterInputs(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE PARAMETER INPUT:4 prepared suites; actual input/caret/spin/focus/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-light-input-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureLightInputs(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE LIGHT INPUT:4 prepared suites; physical input/caret/repeat/focus/DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-level-window-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckCreatureLevelWindow(native,language,directory);
                }
                Console.WriteLine("CREATURE LEVEL WINDOW:4 prepared suites; native dialogs, focus, DPI and game effect require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--manager-destination-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckManagerDestination(native,language);
                }
                Console.WriteLine("MANAGER DESTINATION UI:4 prepared suites; physical input, repeat, focus and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--scenario-new-point-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckScenarioNewPoint(native,language);
                }
                Console.WriteLine("SCENARIO NEW POINT UI:4 prepared suites; actual focus, notifications and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--region-coordinate-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckRegionCoordinates(native,language);
                }
                Console.WriteLine("REGION COORDINATE UI:4 prepared suites; physical input, focus, repeat and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--scenario-expansion-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckScenarioExpansionWindow(native,language);
                }
                Console.WriteLine("SCENARIO EXPANSION UI:4 prepared cases; actual notifications, focus and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--scenario-insertion-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckScenarioInsertionCommands(native,language);
                }
                Console.WriteLine("SCENARIO INSERTION UI:4 prepared cases; actual focus, notifications and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--region-list-modify-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckRegionListModify(native,language);
                }
                Console.WriteLine("REGION LIST MODIFY UI:4 prepared cases; physical double-click, focus and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--region-creation-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckRegionCreationWindow(native,language);
                }
                Console.WriteLine("REGION CREATION UI:4 cases; actual mouse, child dialogs and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--scenario-tree-keyboard-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckScenarioTreeKeyboard(native,language);
                }
                Console.WriteLine("SCENARIO TREE KEYBOARD UI:4 cases; physical focus, notifications and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--special-keyboard-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckSpecialKeyboard(native,language);
                }
                Console.WriteLine("SPECIAL KEYBOARD UI:4 cases; physical map focus, pointer refresh, capture and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--source-keyboard-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckSourceKeyboard(native,language,directory);
                }
                Console.WriteLine("SOURCE KEYBOARD UI:4 cases; physical focus, routed keys and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--help-section-versions-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckHelpSectionVersions(native,language,directory);
                }
                Console.WriteLine("HELP AND SECTION VERSIONS UI:4 cases; physical guide layout, focus and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--object-brush-commands-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckObjectBrushCommands(native,language,directory);
                }
                Console.WriteLine("OBJECT BRUSH COMMAND UI:4 cases across five source kinds; physical field commits, focus and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--history-clear-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckHistoryClear(native,language,directory);
                }
                Console.WriteLine("HISTORY CLEAR UI:4 cases; physical menu, pending fields, focus and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--workspace-manager-routes-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckWorkspaceManagerRoutes(native,language,directory);
                }
                Console.WriteLine("WORKSPACE MANAGER ROUTES UI:4 cases; physical field commits, focus, menus and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--manager-list-commands-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckManagerListCommands(native,language,directory);
                }
                Console.WriteLine("MANAGER LIST COMMAND UI SMOKE PREPARED:4; physical menus, field commits, focus, original GUI and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--disk-progress-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckDiskProgress(native,language,directory);
                }
                Console.WriteLine("DISK PROGRESS UI SMOKE PREPARED:4; actual rendering, owner locking, native dialogs and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--compiler-task-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckCompilerTask(native,language);
                }
                Console.WriteLine("COMPILER TASK UI SMOKE PREPARED:4; actual owner locking, task progress, native geometry and DPI require manual acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--document-reports-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckDocumentReports(native,language);
                }
                Console.WriteLine("DOCUMENT REPORT UI SMOKE PASSED:4; physical dialogs/mouse/keyboard/DPI remain required");return 0;
            }
            if(args.SequenceEqual(new[]{"--object-manager-flow-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckObjectManagerFlow(native,language);
                }
                Console.WriteLine("OBJECT MANAGER FLOW UI SMOKE PASSED:4; physical source GUI/mouse/keyboard/DPI remain required");return 0;
            }
            if(args.SequenceEqual(new[]{"--object-manager-navigation-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckObjectManagerNavigation(native,language);
                }
                Console.WriteLine("OBJECT MANAGER NAVIGATION UI SMOKE PASSED:4; physical modal restoration/mouse/keyboard/DPI remain required");return 0;
            }
            if(args.SequenceEqual(new[]{"--object-manager-commands-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckObjectManagerCommands(native,language);
                }
                Console.WriteLine("OBJECT MANAGER COMMAND UI SMOKE PASSED:4; physical original GUI/mouse/DPI acceptance remains required");return 0;
            }
            if(args.SequenceEqual(new[]{"--region-removal-confirmation-ui-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true}) CheckRegionRemovalConfirmation(native,language);
                }
                Console.WriteLine("REGION REMOVAL CONFIRMATION UI SMOKE PASSED:4; physical source GUI/keyboard/mouse/audio/DPI remain required");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-jobs-request-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureJobsRequest(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE JOB REQUEST UI SMOKE PASSED:4; physical original GUI/audio/DPI remain required");return 0;
            }
            if(args.SequenceEqual(new[]{"--creature-jobs-callbacks-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var project in new[]{false,true})CheckCreatureJobsCallbacks(DefinitionFixture(directory,language,project),language);
                }
                Console.WriteLine("CREATURE JOB CALLBACK UI SMOKE PASSED:4; original native notification timing, physical GUI/audio/DPI still require acceptance");return 0;
            }
            if(args.SequenceEqual(new[]{"--patrol-owned-requests-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})
                        CheckPatrolRoutes(NativeMapDocument.CreateNewMap(new("PatrolUI","PatrolUI",20,20,NativeFormat:native)),language);
                }
                Console.WriteLine("PATROL OWNED REQUESTS UI SMOKE PASSED: 4 format/language cases; physical original GUI, clipboard, focus, DPI and gameplay acceptance remain required");
                return 0;
            }
            if(args.SequenceEqual(new[]{"--object-manager-callers-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true}){CheckObjectManager(native,language);CheckObjectModification(native,language);}
                }
                Console.WriteLine("OBJECT MANAGER SOURCE CALLERS UI SMOKE PASSED: 8");return 0;
            }
            if(args.SequenceEqual(new[]{"--object-insertion-caller-only"}))
            {
                foreach(var language in new[]{"en","ru"}){LocalizationService.SetLanguage(language);foreach(var native in new[]{false,true})CheckObjectBatch(native,language);}
                Console.WriteLine("OBJECT INSERTION SOURCE CALLER UI SMOKE PASSED: 4");return 0;
            }
            if(args.SequenceEqual(new[]{"--editor-save-lifecycle-only"}))
            {
                foreach(var language in new[]{"en","ru"})
                {
                    LocalizationService.SetLanguage(language);
                    foreach(var native in new[]{false,true})CheckSaveLifecycle(directory,language,native);
                    CheckSaveRecovery(directory,language);
                }
                Console.WriteLine("EDITOR SAVE LIFECYCLE UI SMOKE PASSED: physical shortcuts/system dialogs/full regression/delivery remain required");return 0;
            }
            if (args.SequenceEqual(new[] { "--editor-drawing-history-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckEditorDrawingHistory(directory, language, native);
                }
                Console.WriteLine("EDITOR DRAWING HISTORY UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--activity-events-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true })
                    { CheckActivityEvents(language, native); CheckActivityTasks(directory, language, native); }
                }
                Console.WriteLine("ACTIVITY EVENTS UI SMOKE PASSED: full regression and physical input acceptance remain pending"); return 0;
            }
            if (args.SequenceEqual(new[] { "--map-statistics-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var legacy in new[] { false, true })
                        CheckStatistics(NativeMapDocument.CreateNewMap(new("StatisticsUI", "StatisticsUI", 38, 20, Players: 5, PlaceHearts: true, NativeFormat: !legacy)), language);
                }
                Console.WriteLine("MAP STATISTICS UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--activity-statistics-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckActivityStatistics(directory, language, native);
                }
                Console.WriteLine("ACTIVITY STATISTICS UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--creature-spell-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { true, false }) CheckCreatureSpell(DefinitionFixture(directory, language, project), language);
                }
                Console.WriteLine("CREATURE SPELL UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--creature-parent-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { true, false }) CheckCreatureGlobals(DefinitionFixture(directory, language, project), language);
                }
                Console.WriteLine("CREATURE PARENT UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--creature-pages-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { true, false })
                        CheckCreaturePages(DefinitionFixture(directory, language, project), language);
                }
                Console.WriteLine("CREATURE PAGES UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--global-variables-session-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { true, false }) CheckGlobalVariables(DefinitionFixture(directory, language, project), language);
                }
                Console.WriteLine("GLOBAL VARIABLES SESSION UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--global-spell-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { true, false }) CheckGlobalSpell(DefinitionFixture(directory, language, project), language);
                }
                Console.WriteLine("GLOBAL SPELL UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--global-trap-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { true, false })
                        CheckGlobalTrap(DefinitionFixture(directory, language, project), language);
                }
                Console.WriteLine("GLOBAL TRAP UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--global-door-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { true, false })
                        CheckGlobalDoor(DefinitionFixture(directory, language, project), language);
                }
                Console.WriteLine("GLOBAL DOOR UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--global-room-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { true, false })
                        CheckGlobalRoom(DefinitionFixture(directory, language, project), language);
                }
                Console.WriteLine("GLOBAL ROOM UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--global-terrain-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { true, false })
                        CheckGlobalTerrain(DefinitionFixture(directory, language, project), language);
                }
                Console.WriteLine("GLOBAL TERRAIN UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scope-controls-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true })
                        CheckScopeControls(NativeMapDocument.CreateNewMap(new("ScopeUI", "ScopeUI", 8, 8, NativeFormat: native)), language);
                }
                Console.WriteLine("SCOPE CONTROLS UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-action-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckScenarioActionWindows(native, language, directory); }
                Console.WriteLine("SCENARIO ACTION UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-existing-wizard-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckExistingWizard(native, language, directory); }
                Console.WriteLine("SCENARIO EXISTING WIZARD UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-speech-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckScenarioSpeech(native, language, directory); }
                Console.WriteLine("SCENARIO SPEECH UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-spatial-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckScenarioSpatial(native, language, directory); }
                Console.WriteLine("SCENARIO SPATIAL UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-creation-session-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckScenarioCreation(native, language, directory); }
                Console.WriteLine("SCENARIO CREATION SESSION UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-controls-save-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) { CheckScenarioEditorSave(native, language, directory); CheckScenarioControlWindows(native, language, directory); } }
                Console.WriteLine("SCENARIO CONTROLS SAVE UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-editor-save-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckScenarioEditorSave(native, language, directory); }
                Console.WriteLine("SCENARIO EDITOR SAVE UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-creation-defaults-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckScenarioCreationDefaults(native, language, directory); }
                Console.WriteLine("SCENARIO CREATION DEFAULTS UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-spawn-parent-only" }))
            {
                foreach (var language in new[] { "en", "ru" }) { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckScenarioSpawnParent(native, language, directory); }
                Console.WriteLine("SCENARIO SPAWN PARENT UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-camera-parent-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) { CheckScenarioCameraParent(native, language, directory); CheckCameraPaths(NativeMapDocument.CreateNewMap(new("CameraUI", "CameraUI", 20, 20, NativeFormat: native)), language, directory); } }
                Console.WriteLine("SCENARIO CAMERA PARENT UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-alias-parent-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) { CheckScenarioAliasEvents(native, language, directory); CheckScenarioAliasParentWindows(native, language, directory); } }
                Console.WriteLine("SCENARIO ALIAS PARENT UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-parameters-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) { CheckScenarioParameterChoices(native, language, directory); CheckScenarioControlWindows(native, language, directory); CheckScenarioNamedWindows(native, language, directory); CheckScenarioActionWindows(native, language, directory); CheckScenarioAliasParentWindows(native, language, directory); } }
                Console.WriteLine("SCENARIO PARAMETER UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-alias-events-only" }))
            {
                foreach (var language in new[] { "en", "ru" }) { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckScenarioAliasEvents(native, language, directory); }
                Console.WriteLine("SCENARIO ALIAS EVENT UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--native-creature-variants-only" }))
            {
                foreach (var language in new[] { "en", "ru" }) { LocalizationService.SetLanguage(language); CheckCreatureVariants(directory, language); }
                Console.WriteLine("NATIVE CREATURE VARIANT UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--map-properties-only" }))
            {
                foreach (var language in new[] { "en", "ru" }) { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckMapProperties(native, language, directory); CheckMapPropertiesLayouts(language, directory); }
                Console.WriteLine("MAP PROPERTIES UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--game-version-only" }))
            {
                foreach (var language in new[] { "en", "ru" }) { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckGameVersion(native, language, directory); }
                Console.WriteLine("GAME VERSION UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--speech-text-only" }))
            {
                foreach(var language in new[]{"en","ru"}){LocalizationService.SetLanguage(language);foreach(var native in new[]{false,true})CheckSpeechText(native,language,directory);}
                Console.WriteLine("SPEECH TEXT UI SMOKE PASSED");return 0;
            }
            if (args.SequenceEqual(new[] { "--level-options-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckLevelOptions(native, language, directory); }
                Console.WriteLine("LEVEL OPTIONS UI SMOKE PASSED"); return 0;
            }
            if(args.SequenceEqual(new[]{"--scenario-names-only"}))
            {
                foreach(var language in new[]{"en","ru"}){LocalizationService.SetLanguage(language);foreach(var native in new[]{false,true})CheckScenarioNames(native,language,directory);}
                Console.WriteLine("SCENARIO NAMES SOURCE CALLER UI SMOKE PASSED: 4");return 0;
            }
            if (args.SequenceEqual(new[] { "--scenario-descriptions-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckScenarioDescriptions(native, language, directory); }
                Console.WriteLine("SCENARIO DESCRIPTION UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--unused-scripts-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckUnusedScripts(native, language, directory); }
                Console.WriteLine("UNUSED SCRIPT UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--copy-confirmation-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckCopyConfirmation(native, language, directory); }
                Console.WriteLine("COPY CONFIRMATION UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--map-overlays-only" }))
            {
                CheckCameraPixels();
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckMapOverlays(native, language); }
                Console.WriteLine("MAP OVERLAY UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--terrain-zoom-only" }))
            {
                CheckTerrainPixels();
                foreach (var language in new[] { "en", "ru" })
                { LocalizationService.SetLanguage(language); foreach (var native in new[] { false, true }) CheckTerrainZoom(native, language); }
                Console.WriteLine("TERRAIN ZOOM UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--map-object-display-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckMapObjectDisplay(native, language);
                }
                Console.WriteLine("MAP OBJECT DISPLAY UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--property-images-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckPropertyImages(native, language);
                }
                Console.WriteLine("PROPERTY IMAGES UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--object-images-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckObjectImages(native, language);
                }
                Console.WriteLine("OBJECT IMAGES UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--object-session-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckObjectSession(native, language);
                }
                Console.WriteLine("OBJECT SESSION UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--object-painting-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckObjectPainting(native, language);
                }
                Console.WriteLine("OBJECT PAINTING UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--object-modification-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckObjectModification(native, language);
                }
                Console.WriteLine("OBJECT MODIFICATION UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--object-manager-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckObjectManager(native, language);
                }
                Console.WriteLine("OBJECT MANAGER UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--object-insertion-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckObjectBatch(native, language);
                }
                Console.WriteLine("OBJECT BATCH UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--random-paint-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckRandomPaint(NativeMapDocument.CreateNewMap(new("RandomUI","RandomUI",20,20,NativeFormat:native)),language);
                }
                Console.WriteLine("RANDOM PAINT UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--history-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckHistory(directory,language,native);
                }
                Console.WriteLine("HISTORY UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--camera-paths-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false, true }) CheckCameraPaths(NativeMapDocument.CreateNewMap(new("CameraUI", "CameraUI", 20,20, NativeFormat: native)),language,directory);
                }
                Console.WriteLine("CAMERA PATH UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--inspector-references-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var custom in new[] { false, true }) CheckInspectorReferences(directory, language, custom);
                }
                Console.WriteLine("REFERENCE INSPECTOR UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--thumbnail-only" }))
            {
                foreach (var language in new[] { "en","ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false,true }) CheckThumbnail(NativeMapDocument.CreateNewMap(new("ThumbnailUI","ThumbnailUI",64,32,NativeFormat: native)),language,directory);
                }
                Console.WriteLine("THUMBNAIL UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--fullmap-only" }))
            {
                foreach (var language in new[] { "en","ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var native in new[] { false,true }) CheckFullMap(NativeMapDocument.CreateNewMap(new("FullMapUI","FullMapUI",128,64,NativeFormat: native)),language);
                }
                Console.WriteLine("FULLSCREEN UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--original-text-only" }))
            {
                foreach (var language in new[] { "en","ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var nativeTextExport in new[] { false,true }) CheckTextExportModes(NativeMapDocument.CreateNewMap(new("OriginalTextUI","OriginalTextUI",32,20,NativeFormat: nativeTextExport)),language,directory);
                }
                Console.WriteLine("ORIGINAL TEXT EXPORT UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--overview-only" }))
            {
                foreach (var language in new[] { "en","ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var nativeOverview in new[] { false,true }) CheckOverview(NativeMapDocument.CreateNewMap(new("OverviewUI","OverviewUI",128,64,NativeFormat: nativeOverview)),language);
                }
                Console.WriteLine("MINIMAP UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--player-ai-only" }))
            {
                foreach (var language in new[] { "en","ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var nativeAI in new[] { false,true }) CheckPlayerAI(NativeMapDocument.CreateNewMap(new("CustomAIUI","CustomAIUI",38,20,Players: 5,PlaceHearts: true,NativeFormat: nativeAI)),language);
                }
                Console.WriteLine("CUSTOM PLAYER AI UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--native-tiles-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var storedSize in new uint[] { 0,uint.MaxValue }) foreach (var undersized in new[] { false,true }) CheckNativeTileLayout(directory,language,storedSize,undersized);
                }
                Console.WriteLine("NATIVE TILE UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--effect-generators-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { false, true }) CheckEffectGenerators(NativeMapDocument.CreateNewMap(new("GeneratorUI", "GeneratorUI", 20, 20, NativeFormat: !project)), language);
                }
                Console.WriteLine("EFFECT GENERATOR UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--decorations-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { false, true }) CheckDecorations(DefinitionFixture(Path.Combine(directory, "decorations"), language, project), language);
                }
                Console.WriteLine("DECORATION UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--definition-rules-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { false, true }) CheckDefinitionRules(DefinitionFixture(Path.Combine(directory, "rules"), language, project), language);
                }
                Console.WriteLine("DEFINITION RULE UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--creature-options-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { false, true }) CheckCreatureOptions(DefinitionFixture(Path.Combine(directory, "options"), language, project), language);
                }
                Console.WriteLine("CREATURE OPTIONS UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--attacks-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { false, true }) CheckCreatureAttacks(DefinitionFixture(Path.Combine(directory, "attacks"), language, project), language);
                }
                Console.WriteLine("CREATURE ATTACK UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--jobs-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { false, true }) CheckCreatureJobs(DefinitionFixture(Path.Combine(directory, "jobs"), language, project), language);
                }
                Console.WriteLine("CREATURE JOB UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--projectiles-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { false, true }) CheckProjectiles(DefinitionFixture(Path.Combine(directory, "projectiles"), language, project), language);
                }
                Console.WriteLine("PROJECTILE UI SMOKE PASSED"); return 0;
            }
            if (args.SequenceEqual(new[] { "--definitions-only" }))
            {
                foreach (var language in new[] { "en", "ru" })
                {
                    LocalizationService.SetLanguage(language);
                    foreach (var project in new[] { false, true })
                    {
                        CheckDefinitions(DefinitionFixture(directory, language, project), language);
                        CheckDefinitionNumbers(DefinitionFixture(Path.Combine(directory, "numeric"), language, project), language);
                    }
                }
                Console.WriteLine("DEFINITION UI SMOKE PASSED"); return 0;
            }
            var basis = TextMapFile.Parse("GUI_GENERATED_MAP\nUI\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF");
            CheckTerrainPixels();
            CheckCameraPixels();
            foreach (var language in new[] { "en", "ru" })
            {
                LocalizationService.SetLanguage(language);
                CheckMainWindow(language);
                foreach (var nativeTerrain in new[] { false, true }) CheckTerrainZoom(nativeTerrain, language);
                foreach (var nativeOverlay in new[] { false, true }) CheckMapOverlays(nativeOverlay, language);
                foreach (var nativeCopyConfirmation in new[] { false, true }) CheckCopyConfirmation(nativeCopyConfirmation, language, directory);
                foreach (var nativeUnusedScripts in new[] { false, true }) CheckUnusedScripts(nativeUnusedScripts, language, directory);
                foreach (var nativeActivity in new[] { false, true })
                { CheckActivityStatistics(directory, language, nativeActivity); CheckActivityEvents(language, nativeActivity); CheckActivityTasks(directory, language, nativeActivity); CheckEditorDrawingHistory(directory, language, nativeActivity); }
                foreach (var nativeNames in new[] { false, true }) CheckScenarioNames(nativeNames, language, directory);
                foreach (var nativeAliases in new[] { false, true }) { CheckScenarioAliasEvents(nativeAliases, language, directory); CheckScenarioAliasParentWindows(nativeAliases, language, directory); }
                foreach (var nativeDescriptions in new[] { false, true }) CheckScenarioDescriptions(nativeDescriptions, language, directory);
                foreach (var nativeParameters in new[] { false, true }) CheckScenarioParameterChoices(nativeParameters, language, directory);
                foreach (var nativeParameters in new[] { false, true }) CheckScenarioControlWindows(nativeParameters, language, directory);
                foreach (var nativeParameters in new[] { false, true }) CheckScenarioNamedWindows(nativeParameters, language, directory);
                foreach (var nativeParameters in new[] { false, true }) CheckScenarioActionWindows(nativeParameters, language, directory);
                foreach (var nativeLevel in new[] { false, true }) CheckLevelOptions(nativeLevel, language, directory);
                foreach (var nativeSpeech in new[] { false, true }) CheckSpeechText(nativeSpeech, language, directory);
                foreach (var nativeVersion in new[] { false, true }) CheckGameVersion(nativeVersion, language, directory);
                foreach (var nativeProperties in new[] { false, true }) CheckMapProperties(nativeProperties, language, directory);
                CheckMapPropertiesLayouts(language, directory);
                foreach (var nativeDisplay in new[] { false, true }) CheckMapObjectDisplay(nativeDisplay, language);
                foreach (var nativeProperties in new[] { false, true }) CheckPropertyImages(nativeProperties, language);
                foreach (var nativeImages in new[] { false, true }) CheckObjectImages(nativeImages, language);
                foreach (var nativeSession in new[] { false, true }) CheckObjectSession(nativeSession, language);
                foreach (var nativePainting in new[] { false, true }) CheckObjectPainting(nativePainting, language);
                foreach (var nativeManager in new[] { false, true }) {CheckObjectManager(nativeManager, language);CheckObjectManagerCommands(nativeManager,language);CheckObjectManagerNavigation(nativeManager,language);CheckObjectManagerFlow(nativeManager,language);CheckDocumentReports(nativeManager,language);}
                foreach (var nativeModification in new[] { false, true }) CheckObjectModification(nativeModification, language);
                foreach (var nativeBatch in new[] { false, true }) CheckObjectBatch(nativeBatch, language);
                foreach (var nativeRandom in new[] { false,true }) CheckRandomPaint(NativeMapDocument.CreateNewMap(new("RandomUI","RandomUI",20,20,NativeFormat:nativeRandom)),language);
                foreach (var nativeHistory in new[] { false, true }) CheckHistory(directory,language,nativeHistory);
                foreach(var nativeSave in new[]{false,true})CheckSaveLifecycle(directory,language,nativeSave);
                CheckSaveRecovery(directory,language);
                foreach (var nativeCamera in new[] { false, true }) { CheckExistingWizard(nativeCamera, language, directory); CheckScenarioSpeech(nativeCamera, language, directory); CheckScenarioSpatial(nativeCamera, language, directory); CheckScenarioCreation(nativeCamera, language, directory); CheckScenarioEditorSave(nativeCamera, language, directory); CheckScenarioCreationDefaults(nativeCamera, language, directory); CheckScenarioSpawnParent(nativeCamera, language, directory); CheckScenarioCameraParent(nativeCamera, language, directory); CheckCameraPaths(NativeMapDocument.CreateNewMap(new("CameraUI", "CameraUI", 20,20, NativeFormat: nativeCamera)),language,directory); }
                foreach (var custom in new[] { false, true }) CheckInspectorReferences(directory, language, custom);
                foreach (var nativeThumbnail in new[] { false,true }) CheckThumbnail(NativeMapDocument.CreateNewMap(new("ThumbnailUI","ThumbnailUI",64,32,NativeFormat: nativeThumbnail)),language,directory);
                foreach (var nativeFullmap in new[] { false,true }) CheckFullMap(NativeMapDocument.CreateNewMap(new("FullMapUI","FullMapUI",128,64,NativeFormat: nativeFullmap)),language);
                foreach (var nativeTextExport in new[] { false,true }) CheckTextExportModes(NativeMapDocument.CreateNewMap(new("OriginalTextUI","OriginalTextUI",32,20,NativeFormat: nativeTextExport)),language,directory);
                foreach (var nativeOverview in new[] { false,true }) CheckOverview(NativeMapDocument.CreateNewMap(new("OverviewUI","OverviewUI",128,64,NativeFormat: nativeOverview)),language);
                foreach (var nativeAI in new[] { false,true }) CheckPlayerAI(NativeMapDocument.CreateNewMap(new("CustomAIUI","CustomAIUI",38,20,Players: 5,PlaceHearts: true,NativeFormat: nativeAI)),language);
                CheckTerrainValidation(language, directory);
                CheckNewMapDialog(language);
                foreach (var storedSize in new uint[] { 0,uint.MaxValue }) foreach (var undersized in new[] { false,true }) CheckNativeTileLayout(directory,language,storedSize,undersized);
                foreach (var legacy in new[] { false, true })
                    CheckMapRegions(NativeMapDocument.CreateNewMap(new("RegionUI", "RegionUI", 16, 16, NativeFormat: !legacy)), language);
                foreach (var legacy in new[] { false, true })
                    CheckEffectGenerators(NativeMapDocument.CreateNewMap(new("GeneratorUI", "GeneratorUI", 20, 20, NativeFormat: !legacy)), language);
                foreach (var legacy in new[] { false, true })
                    CheckPatrolRoutes(NativeMapDocument.CreateNewMap(new("PatrolUI", "PatrolUI", 20, 20, NativeFormat: !legacy)), language);
                foreach (var legacy in new[] { false, true })
                    CheckGateGeneration(NativeMapDocument.CreateNewMap(new("GateUI", "GateUI", 32, 32, Players: 3, NativeFormat: !legacy)), language);
                foreach (var legacy in new[] { false, true })
                    CheckRuleMatrix(NativeMapDocument.CreateNewMap(new("RuleUI", "RuleUI", 20, 20, NativeFormat: !legacy)), language);
                foreach (var legacy in new[] { false, true })
                    CheckVariables(NativeMapDocument.CreateNewMap(new("VariableUI", "VariableUI", 20, 20, NativeFormat: !legacy)), language);
                CheckVariableScope(language, directory);
                foreach (var legacy in new[] { false, true })
                    CheckPlayers(NativeMapDocument.CreateNewMap(new("PlayerUI", "PlayerUI", 38, 20, Players: 5, PlaceHearts: true, NativeFormat: !legacy)), language, directory);
                foreach (var legacy in new[] { false, true })
                    CheckPlayerGeometry(NativeMapDocument.CreateNewMap(new("GeometryUI", "GeometryUI", 38, 20, Players: 5, PlaceHearts: true, NativeFormat: !legacy)), language, directory);
                foreach (var legacy in new[] { false, true })
                    CheckStatistics(NativeMapDocument.CreateNewMap(new("StatisticsUI", "StatisticsUI", 38, 20, Players: 5, PlaceHearts: true, NativeFormat: !legacy)), language);
                foreach (var legacy in new[] { false, true })
                    CheckDefinitions(DefinitionFixture(directory, language, legacy), language);
                foreach (var legacy in new[] { false, true })
                    CheckDefinitionNumbers(DefinitionFixture(Path.Combine(directory, "numeric"), language, legacy), language);
                foreach (var legacy in new[] { false, true })
                    CheckProjectiles(DefinitionFixture(Path.Combine(directory, "projectiles"), language, legacy), language);
                foreach (var legacy in new[] { false, true })
                    CheckCreatureJobs(DefinitionFixture(Path.Combine(directory, "jobs"), language, legacy), language);
                foreach (var legacy in new[] { false, true })
                    CheckCreatureAttacks(DefinitionFixture(Path.Combine(directory, "attacks"), language, legacy), language);
                foreach (var legacy in new[] { false, true })
                    CheckCreatureOptions(DefinitionFixture(Path.Combine(directory, "options"), language, legacy), language);
                CheckCreatureVariants(directory, language);
                foreach (var legacy in new[] { false, true })
                    CheckDefinitionRules(DefinitionFixture(Path.Combine(directory, "rules"), language, legacy), language);
                foreach (var legacy in new[] { false, true })
                {
                    CheckDecorations(DefinitionFixture(Path.Combine(directory, "decorations"), language, legacy), language);
                    CheckGlobalTerrain(DefinitionFixture(Path.Combine(directory, "global-terrain"), language, legacy), language);
                    CheckGlobalRoom(DefinitionFixture(Path.Combine(directory, "global-room"), language, legacy), language);
                    CheckGlobalDoor(DefinitionFixture(Path.Combine(directory, "global-door"), language, legacy), language);
                    CheckGlobalTrap(DefinitionFixture(Path.Combine(directory, "global-trap"), language, legacy), language);
                    CheckGlobalSpell(DefinitionFixture(Path.Combine(directory, "global-spell"), language, legacy), language);
                    CheckCreatureSpell(DefinitionFixture(Path.Combine(directory, "creature-spell"), language, legacy), language);
                    CheckCreaturePages(DefinitionFixture(Path.Combine(directory, "creature-pages"), language, legacy), language);
                    CheckCreatureGlobals(DefinitionFixture(Path.Combine(directory, "creature-parent"), language, legacy), language);
                    CheckCreatureJobsRequest(DefinitionFixture(Path.Combine(directory, "creature-job-request"), language, legacy), language);
                    CheckCreatureJobsCallbacks(DefinitionFixture(Path.Combine(directory, "creature-job-callbacks"), language, legacy), language);
                    CheckGlobalVariables(DefinitionFixture(Path.Combine(directory, "global-variables-session"), language, legacy), language);
                }
                var native = NativeMapDocument.Create(8, 8, "BandUI");
                var nativeBand = native.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0));
                CheckWorkspace(native, nativeBand, language);
                CheckReferenceControls(NativeMapDocument.Create(8, 8, "ReferenceUI"), language);
                CheckScriptTree(NativeMapDocument.Create(8, 8, "TreeUI"), language);
                CheckBranchControls(NativeMapDocument.Create(8, 8, "BranchUI"), language);
                CheckTreeProjection(NativeMapDocument.Create(8, 8, "ProjectionUI"), language);
                CheckCopyBuffer(NativeMapDocument.Create(3, 3, "CopyUI"), language, directory);
                CheckSelectionBounds(NativeMapDocument.Create(8, 8, "SelectionUI"), language);
                CheckTransformControls(NativeMapDocument.Create(8, 8, "TransformUI"), language);
                CheckEditorTerrainControls(NativeMapDocument.Create(6, 6, "TerrainUI"), language);
                CheckProtectedSelections(NativeMapDocument.Create(6, 6, "MaskUI"), language, directory);
                CheckTerrainPlacement(NativeMapDocument.Create(16, 16, "FootprintUI"), language);
                CheckScopeControls(NativeMapDocument.Create(8, 8, "ScopeUI"), language);
                var scopePath = Path.Combine(directory, $"scope-{language}.dkm"); File.WriteAllBytes(scopePath, basis.ToArray());
                CheckScopeControls(NativeMapDocument.Load(scopePath), language);
                var branchPath = Path.Combine(directory, $"branches-{language}.dkm"); File.WriteAllBytes(branchPath, basis.ToArray());
                CheckBranchControls(NativeMapDocument.Load(branchPath), language);
                var projectionPath = Path.Combine(directory, $"projection-{language}.dkm"); File.WriteAllBytes(projectionPath, basis.ToArray());
                CheckTreeProjection(NativeMapDocument.Load(projectionPath), language);
                var copyPath = Path.Combine(directory, $"copy-{language}.dkm"); File.WriteAllBytes(copyPath, basis.ToArray());
                CheckCopyBuffer(NativeMapDocument.Load(copyPath), language, directory);
                CheckSelectionBounds(NativeMapDocument.Load(copyPath), language);
                var terrainPath = Path.Combine(directory, $"terrain-{language}.dkm");
                File.WriteAllBytes(terrainPath, TextMapFile.Parse("GUI_GENERATED_MAP\nTerrainUI\nPLAYERS 1\nMAP\n6 X 6\n" + string.Concat(Enumerable.Repeat("OaOaOaOaOaOa\n", 6)) + "EOF").ToArray());
                CheckEditorTerrainControls(NativeMapDocument.Load(terrainPath), language);
                CheckTransformControls(NativeMapDocument.Load(terrainPath), language);
                CheckProtectedSelections(NativeMapDocument.Load(terrainPath), language, directory);
                var placementPath = Path.Combine(directory, $"placement-{language}.dkm");
                File.WriteAllBytes(placementPath, TextMapFile.Parse("GUI_GENERATED_MAP\nPlacementUI\nPLAYERS 1\nMAP\n16 X 16\n" + string.Concat(Enumerable.Repeat(string.Concat(Enumerable.Repeat("Oa", 16)) + "\n", 16)) + "EOF").ToArray());
                CheckTerrainPlacement(NativeMapDocument.Load(placementPath), language);
                for (var version = 1; version <= 5; version++)
                {
                    var path = Path.Combine(directory, $"v{version}-{language}.dkm");
                    File.WriteAllBytes(path, LegacySectionFile.Create(basis.Sections.Where(section => section.Signature != NativeMapDocument.LegacyBands).Append(new(NativeMapDocument.LegacyBands, version, ReadOnlyMemory<byte>.Empty))).ToArray());
                    var project = NativeMapDocument.Load(path);
                    project.AddLegacyRecord("Band", new(1, 1)); // An earlier record tests selection across changing strides.
                    CheckWorkspace(project, project.AddLegacyRecord("Band", new(1, 1)), language);
                    if (version == 5) { CheckReferenceControls(project, language); CheckScriptTree(project, language); }
                }
                foreach (var path in args) CheckLoadedTree(NativeMapDocument.Load(path), language);
            }
            Console.WriteLine("WINDOWS UI SMOKE PASSED"); return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        finally { Directory.Delete(directory, true); app.Shutdown(); }
    }

    private static T Get<T>(object? value, string property) => (T)(value?.GetType().GetProperty(property)?.GetValue(value) ?? throw new InvalidOperationException("Missing property " + property));
    private static void CheckMainWindow(string language)
    {
        // The startup crash was in MainWindow's DrawingBrush, not EditorWorkspace.
        // Construct and lay out the complete shell; never show it or persist settings.
        var window = new MainWindow(new AppSettings(), persistSettings: false);
        try
        {
            var content = (FrameworkElement)window.Content;
            content.Measure(new Size(1400, 900)); content.Arrange(new Rect(0, 0, 1400, 900)); content.UpdateLayout();
            IEnumerable<DependencyObject> Descendants(DependencyObject parent)
            {
                for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
                {
                    var child = VisualTreeHelper.GetChild(parent, index); yield return child;
                    foreach (var descendant in Descendants(child)) yield return descendant;
                }
            }
            var drawing = Descendants(content).OfType<Grid>().Select(grid => grid.Background).OfType<DrawingBrush>().Single();
            var lines = ((DrawingGroup)drawing.Drawing).Children.OfType<GeometryDrawing>().Single(item => item.Pen is not null);
            Expect(lines.Pen.Brush is SolidColorBrush { Color: var color } && color == Color.FromRgb(0x2B, 0x25, 0x1F) && lines.Pen.Thickness == 1,
                "Startup grid must use a real Pen, not a string or Brush.");
            Expect(window.Title == LocalizationService.Get("App.Title"), "Main window title is not localized.");
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
            Expect(window.Title == LocalizationService.Get("App.Title"), "Main window live language switching failed.");
            LocalizationService.SetLanguage(language);
            Console.WriteLine($"PASS {language} main-window startup XAML and live translation");
        }
        finally { window.Close(); }
    }
    private static void CheckRuleMatrix(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document };
        var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); tabs.SelectedIndex = 5;
        void Layout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); }
        Layout();
        var table = Control<DataGrid>(workspace, "RuleTable"); var kinds = Control<ComboBox>(workspace, "RuleKindPicker");
        var picker = Control<ComboBox>(workspace, "RuleValuePicker"); var number = Control<TextBox>(workspace, "RuleNumber");
        var unspecified = Control<CheckBox>(workspace, "RuleUnspecified"); var confirmed = Control<CheckBox>(workspace, "RuleConfirmed");
        var panel = Control<StackPanel>(workspace, "RulePanel"); var status = Control<TextBlock>(workspace, "RuleStatus"); var canvas = Control<MapCanvas>(workspace, "Canvas");
        // Generic dictionaries of private view types are invariant; inspect
        // their non-generic enumerator without fabricating the selected key.
        MapRuleCell CellAt(int row, int column)
        {
            var cells = (System.Collections.IEnumerable)(table.Items[row].GetType().GetProperty("Cells")!.GetValue(table.Items[row])!);
            var pair = cells.Cast<object>().Single(item => Get<int>(item, "Key") == column);
            return Get<MapRuleCell>(Get<object>(pair, "Value"), "Cell");
        }
        void SelectCells(params (int Row, int Column)[] cells)
        { table.SelectedCells.Clear(); foreach (var cell in cells) table.SelectedCells.Add(new(table.Items[cell.Row], table.Columns[cell.Column])); }
        void Kind(MapRuleKind kind) { kinds.SelectedValue = (int)kind; Layout(); }
        void Value(string value) => picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == value);
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Matrix UI changed unexpected buffers/history."); }
        Expect(kinds.Items.Count == 7 && table.Items.Count == 15 && table.Columns.Count == 8 && table.IsReadOnly && table.SelectionUnit == DataGridSelectionUnit.Cell, "Actual table dimensions/multicell controls incorrect.");
        var before = Snapshot();
        var content = table.Columns[3].GetCellContent(table.Items[0]) as TextBlock;
        Expect(content is { Text.Length: > 0 }, "DataGrid dictionary cell binding is blank."); Exact(before);
        canvas.SetSelection(new(1, 1, 2, 2)); workspace.Copy(); var buffer = workspace.BufferedSelection; var copy = SelectionFile.Serialize(buffer!); var selection = canvas.Selection;
        SelectCells((0, 3), (1, 4)); Value("4"); Click(panel, "Editor.RuleApply");
        Expect(CellAt(0, 3).Value == 4 && CellAt(1, 4).Value == 4 && table.SelectedCells.Count == 2 && status.Text == LocalizationService.Get("Editor.RuleApplied"), "Multi-cell apply edited wrong owner/element or lost selection.");
        var after = Snapshot(); workspace.Undo(); Exact(before); workspace.Redo(); Exact(after);
        Click(panel, "Editor.RuleClear"); Exact(after); confirmed.IsChecked = true; Click(panel, "Editor.RuleClear");
        Expect(CellAt(0, 3).Value is null && CellAt(1, 4).Value is null && confirmed.IsChecked != true, "Confirmed clear did not remove selected overrides.");
        workspace.Undo(); Exact(after); confirmed.IsChecked = true; Click(panel, "Editor.RuleReset");
        Expect(CellAt(0, 3).Value == 3 && CellAt(1, 4).Value == 3, "Selected default reset lost original room states.");
        SelectCells((2, 5)); Value("4"); Click(panel, "Editor.RuleApply");
        before = Snapshot(); Click(panel, "Editor.RuleResetTable"); Exact(before); confirmed.IsChecked = true; Click(panel, "Editor.RuleResetTable");
        Expect(document.MapRuleCells().Where(cell => cell.Key.Kind == MapRuleKind.Rooms && cell.Key.Player >= 3).All(cell => cell.Value == 3), "Full-table reset failed to restore all keeper rooms.");
        workspace.Undo(); Exact(before); workspace.Redo();
        Kind(MapRuleKind.Traps); Expect(table.Items.Count == 14 && table.SelectedCells.Count == 0, "Changing categories retained stale cells.");
        SelectCells((0, 5)); Value("1"); Click(panel, "Editor.RuleApply"); Expect(CellAt(0, 5).Value == 1, "Trap availability edited another category.");
        Kind(MapRuleKind.Doors); Expect(table.Items.Count == 8, "Door table catalogue wrong."); SelectCells((0, 6)); Value("2"); Click(panel, "Editor.RuleApply"); Expect(CellAt(0, 6).Value == 2, "Door research state failed.");
        Kind(MapRuleKind.Spells); Expect(table.Items.Count == 12, "Spell table catalogue wrong."); SelectCells((0, 7)); Value("3"); Click(panel, "Editor.RuleApply"); Expect(CellAt(0, 7).Value == 3, "Spell available state failed.");
        Kind(MapRuleKind.CreaturePool); SelectCells((0, 3), (1, 4)); number.Text = "999"; Click(panel, "Editor.RuleApply");
        Expect(table.Items.Count == 45 && CellAt(0, 3).Value == 999 && CellAt(1, 4).Value == 999 && picker.Visibility == Visibility.Collapsed, "Pool table failed to use actual creature/player IDs.");
        before = Snapshot(); number.Text = "1000"; Click(panel, "Editor.RuleApply"); Exact(before);
        Expect(status.Text == LocalizationService.Get("Editor.RuleFailed") && status.ToolTip is string, "Pool overflow was not rejected inline.");
        number.Text = "not a number"; Click(panel, "Editor.RuleApply"); Exact(before);
        unspecified.IsChecked = true; unspecified.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        Expect(!number.IsEnabled, "Not-specified control did not disable numeric input."); Click(panel, "Editor.RuleApply"); Expect(CellAt(0, 3).Value is null && CellAt(1, 4).Value is null, "Pool sentinel/override removal failed.");
        unspecified.IsChecked = false; unspecified.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        Kind(MapRuleKind.CreatureModifier); SelectCells((0, 1), (29, 4)); number.Text = "65535"; Click(panel, "Editor.RuleApply");
        Expect(table.Items.Count == 45 && table.Columns.Count == 5 && CellAt(0, 1).Value == 65535 && CellAt(29, 4).Value == 65535, "Modifier columns/batch ID binding incorrect.");
        before = Snapshot(); SelectCells((16, 1)); number.Text = "10"; Click(panel, "Editor.RuleApply"); Exact(before);
        Expect(!CellAt(16, 1).Editable && status.Text == LocalizationService.Get("Editor.RuleFailed"), "Original forbidden elite modifier was changed.");
        SelectCells((0, 1), (29, 4)); number.Text = "65536"; Click(panel, "Editor.RuleApply"); Exact(before);
        number.Text = "17"; var keys = table.SelectedCells.Select(info => CellAt(table.Items.IndexOf(info.Item), info.Column.DisplayIndex).Key).ToHashSet();
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(number.Text == "17" && table.SelectedCells.Select(info => CellAt(table.Items.IndexOf(info.Item), info.Column.DisplayIndex).Key).ToHashSet().SetEquals(keys)
            && table.Columns[1].Header as string == LocalizationService.Get("Editor.RuleParameter.1") && status.Text == LocalizationService.Get("Editor.RuleFailed"), "Live translation lost selection/input or retained old matrix/status labels."); Exact(before);
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout();
        Click(panel, "Editor.RuleApply"); Expect(CellAt(0, 1).Value == 17 && CellAt(29, 4).Value == 17, "Translated selection applied to different cells.");
        number.Text = "0"; Click(panel, "Editor.RuleApply"); Expect(CellAt(0, 1).Value is null && CellAt(29, 4).Value is null, "Modifier zero is not displayed as original not-specified state.");
        Kind(MapRuleKind.Alliance); SelectCells((2, 4), (3, 3)); Value("1"); Click(panel, "Editor.RuleApply");
        Expect(table.Items.Count == 7 && table.Columns.Count == 8 && CellAt(2, 4).Value == 1 && CellAt(3, 3).Value == 1, "Alliance symmetric selected cells were not deduplicated/updated.");
        before = Snapshot(); SelectCells((2, 3)); Value("0"); Click(panel, "Editor.RuleApply"); Exact(before); Expect(!CellAt(2, 3).Editable, "Self-alliance became editable.");
        SelectCells((2, 4)); Value("0"); Click(panel, "Editor.RuleApply"); Expect(CellAt(2, 4).Value == 0 && CellAt(3, 3).Value == 0, "Alliance clearing left reverse relation.");
        confirmed.IsChecked = true; Click(panel, "Editor.RuleResetTable");
        Expect(CellAt(0, 2).Value == 1 && CellAt(1, 1).Value == 1 && CellAt(2, 4).Value == 0, "Original neutral alliance reset failed.");
        confirmed.IsChecked = true; document.Paint([new(2, 2)], new(4, 2, 1)); Expect(confirmed.IsChecked != true, "External document edit retained clearing/reset confirmation."); document.Undo();
        Expect(ReferenceEquals(buffer, workspace.BufferedSelection) && SelectionFile.Serialize(buffer!).AsSpan().SequenceEqual(copy) && canvas.Selection == selection, "Matrix edits changed selection/copy buffer.");
        workspace.Document = null; Expect(!panel.IsEnabled && table.Items.Count == 0 && status.Text.Length == 0 && confirmed.IsChecked != true, "Closed document retained rule matrix.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} rule matrices, multicell editing, defaults and undo");
    }
    private static void CheckGateGeneration(NativeMapDocument document, string language)
    {
        document.Edit("UI gate fixture", () =>
        {
            document.Paint([new(3, 3)], new(29, 1, 1));
            document.Paint(MapDrawing.Rectangle(new(8, 8, 2, 2), true), new(33, 1, 1));
            document.Paint(MapDrawing.Rectangle(new(15, 15, 3, 1), true), new(37, 1, 1));
            document.Paint(MapDrawing.Rectangle(new(23, 23, 1, 3), true), new(37, 1, 2));
        });
        var workspace = new EditorWorkspace { Document = document };
        workspace.Measure(new Size(1400, 1000)); workspace.Arrange(new Rect(0, 0, 1400, 1000)); workspace.UpdateLayout();
        var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); var panel = Control<StackPanel>(workspace, "GeneratorPanel");
        var sites = Control<ListBox>(workspace, "GeneratorSites"); var summary = Control<TextBlock>(workspace, "GeneratorSummary");
        var status = Control<TextBlock>(workspace, "GeneratorStatus"); var apply = Control<Button>(workspace, "GeneratorApply");
        var keepers = Control<ComboBox>(workspace, "GeneratorKeepers");
        workspace.GateGenerationRequestPresenter = window => window.Close();
        var name = Control<TextBox>(workspace, "GatePatternName"); var color = Control<TextBox>(workspace, "GatePatternColor");
        var canvas = Control<MapCanvas>(workspace, "Canvas");
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Gate UI preview/undo changed buffers."); }
        void Toggle(string control, bool value)
        {
            var checkbox = Control<CheckBox>(workspace, control); checkbox.IsChecked = value;
            checkbox.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }
        Expect(!apply.IsEnabled && (int)keepers.SelectedItem == 3 && Control<StackPanel>(workspace, "GeneratorTemplates").IsEnabled == document.IsLegacyProject, "Generator did not initialize from actual format/keeper count.");
        Expect(Control<TextBlock>(workspace, "GeneratorNativeNote").Visibility == (document.IsLegacyProject ? Visibility.Collapsed : Visibility.Visible), "Native template limitation is not visible.");
        Click(Control<StackPanel>(workspace, "RegionPanel"), "Editor.GeneratorTitle"); Expect(tabs.SelectedIndex == 4, "Map manager button did not open generator.");
        canvas.SetSelection(new(1, 1, 2, 2)); workspace.Copy(); var buffer = workspace.BufferedSelection; var copy = SelectionFile.Serialize(buffer!); var selection = canvas.Selection;
        var before = Snapshot(); Click(panel, "Editor.GeneratorPreview"); Exact(before);
        Expect(sites.Items.Count == 4 && summary.Text == string.Format(LocalizationService.Get("Editor.GeneratorSummary"), 4, 4, 3, 3) && apply.IsEnabled, "Preview count/approval route incorrect.");
        Click(panel, "Editor.GeneratorApply"); Exact(before);
        Toggle("GeneratorGateGroup", false);
        Expect(!Control<CheckBox>(workspace, "GeneratorGateRelease").IsEnabled && Control<CheckBox>(workspace, "GeneratorGateRelease").IsChecked == true && !Control<CheckBox>(workspace, "GeneratorEvents").IsEnabled && sites.Items.Count == 0, "Master option lost remembered children or did not invalidate preview.");
        Click(panel, "Editor.GeneratorPreview"); Exact(before);
        Expect(summary.Text == string.Format(LocalizationService.Get("Editor.GeneratorSummary"), 0, 4, 0, 0), "Disabled gate group still generated events/gates.");
        Toggle("GeneratorGateGroup", true); keepers.SelectedItem = 2;
        if (document.IsLegacyProject)
        {
            name.Text = "G%g of %a"; color.Text = "xyz"; Click(panel, "Editor.GeneratorPreview"); Exact(before);
            Expect(status.Text == LocalizationService.Get("Editor.GeneratorFailed") && status.ToolTip is string && !apply.IsEnabled, "Invalid template color was not rejected inline.");
            color.Text = "00112233"; name.Text = "Ворота"; Click(panel, "Editor.GeneratorPreview"); Exact(before);
            Expect(status.Text == LocalizationService.Get("Editor.GeneratorFailed"), "Unrepresentable project text was silently replaced.");
            name.Text = "G%g of %a";
        }
        Click(panel, "Editor.GeneratorPreview"); Expect(apply.IsEnabled, "Generation request was not enabled.");
        var templateText = name.Text; LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(sites.Items.Count == 4 && apply.IsEnabled && name.Text == templateText && (int)keepers.SelectedItem == 2
            && status.Text == LocalizationService.Get("Editor.GeneratorReady") && summary.Text == string.Format(LocalizationService.Get("Editor.GeneratorSummary"), 4, 4, 2, 2)
            && Get<string>(sites.Items[0], "Label").Contains(LocalizationService.Get("Editor.GeneratorProtected")), "Live translation lost preview/template/options or retained old labels.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        // Map edits invalidate an already confirmed preview.
        document.Paint([new(2, 2)], new(4, 2, 1));
        Expect(sites.Items.Count == 0 && !apply.IsEnabled && status.Text.Length == 0, "Map mutation retained stale preview/confirmation.");
        document.Undo(); Exact(before); Click(panel, "Editor.GeneratorPreview");
        workspace.GateGenerationRequestPresenter = window => ((Button)window.FindName("GateGenerationYes")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Click(panel, "Editor.GeneratorApply");
        Expect(document.MapRegions().Count == 8 && ScriptGraph.Build(document).Events.Count == 4 && !apply.IsEnabled
            && status.Text == LocalizationService.Get("Editor.GeneratorApplied"), "Actual apply controls did not generate/clear confirmation.");
        if (document.IsLegacyProject)
        {
            var gate = document.MapRegions().First(region => region.Kind == MapRegionKind.Gate).Record;
            Expect(gate.Read(gate.Fields.Single(field => field.Name == "name")) == "G1 of 1" && gate.Read(gate.Fields.Single(field => field.Name == "rect.color")) == "1122867", "UI templates did not reach the generated record.");
        }
        var after = Snapshot(); Click(panel, "Editor.GeneratorPreview"); Exact(after);
        Expect(status.Text == LocalizationService.Get("Editor.GeneratorNoWork") && !apply.IsEnabled, "Repeated generation duplicated gates/events.");
        workspace.Undo(); Exact(before); workspace.Redo(); Exact(after); workspace.Undo(); Exact(before);
        Expect(ReferenceEquals(buffer, workspace.BufferedSelection) && SelectionFile.Serialize(buffer!).AsSpan().SequenceEqual(copy) && canvas.Selection == selection, "Gate generation changed selection/copy buffer.");
        Click(panel, "Editor.GeneratorPreview"); keepers.SelectedItem = 1;
        Expect(sites.Items.Count == 0 && !apply.IsEnabled, "Option edits retained confirmed preview.");
        workspace.Document = null; Expect(!panel.IsEnabled && sites.Items.Count == 0 && status.Text.Length == 0 && !apply.IsEnabled, "Closed document retained generator state.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} gate-generation controls, templates, preview and undo");
    }
    private static void CheckMapRegions(NativeMapDocument document, string language)
    {
        if(!document.IsLegacyProject)CheckNativeGateMutationWindow(language);
        var workspace = new EditorWorkspace { Document = document };
        var propertyDraftPresented=false;
        workspace.RegionPropertiesPresenter=window=>
        {
            Expect(((TextBox)window.FindName("RegionName")).IsEnabled==document.IsLegacyProject,"Region editor-label availability differs from native/project format.");
            Expect(((Button)window.FindName("RegionApply")).IsEnabled,"Current region private Apply is disabled.");
            propertyDraftPresented=true;
            if(window.IsCreationMode)Expect(window.TryApply(),"Add draft did not accept the selected geometry.");
            window.Close(); // Cancel the unadded/next draft; accepted Add remains.
        };
        workspace.Measure(new Size(1400, 950)); workspace.Arrange(new Rect(0, 0, 1400, 950)); workspace.UpdateLayout();
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var panel = Control<StackPanel>(workspace, "RegionPanel");
        var kinds = Control<ComboBox>(workspace, "RegionKindPicker"); var picker = Control<ListBox>(workspace, "RegionPicker");
        var status = Control<TextBlock>(workspace, "RegionStatus"); var tools = Control<ComboBox>(workspace, "ToolPicker");
        MapRegion Selected() => Get<MapRegion>(picker.SelectedItem, "Region");
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected)
        { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Map-region control changed unexpected bytes."); }
        var initial = Snapshot(); var changes = 0; document.Changed += (_, _) => changes++;
        Expect(kinds.Items.Count == 3 && picker.Items.Count == 0 && panel.IsEnabled, "Region manager did not initialize with three source categories.");
        canvas.SetSelection(new(2, 3, 4, 5)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        var copyBytes = SelectionFile.Serialize(buffer!); var selection = canvas.Selection;
        tools.SelectedIndex = (int)DrawingTool.Fill; Control<ComboBox>(workspace, "TerrainPicker").SelectedValue = 14;
        foreach (var kind in Enum.GetValues<MapRegionKind>())
        {
            kinds.SelectedValue = (int)kind; var before = Snapshot(); var notifications = changes;
            Click(panel, "Editor.RegionAddSelection");
            Expect(Selected().Kind == kind && Selected().Bounds == (kind == MapRegionKind.Gate ? new TileRect(2, 3, 1, 1) : selection), "Actual add-from-selection control lost rectangle/point semantics.");
            Expect(changes == notifications + 1 && canvas.HighlightedRegion == Selected().Bounds && Get<string>(picker.SelectedItem, "Label").Contains("#1", StringComparison.Ordinal), "Region manager lost independent IDs, highlighting or atomic edit.");
            var after = Snapshot(); workspace.Undo(); Exact(before); workspace.Redo(); Exact(after);
            // Undo removes the newly added row; redo does not invent selection history.
            picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<MapRegion>(item, "Region").Kind == kind);
            Click(panel, "Editor.RegionChangeMap");
            Expect(canvas.IsPickingRegion && canvas.PlacementPreview is null, "Pending geometry still exposes a terrain-placement footprint.");
            var original = Selected().Bounds; notifications = changes;
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
            Expect(canvas.IsPickingRegion && canvas.Tool == DrawingTool.Fill && kinds.SelectedValue is int value && value == (int)kind
                && status.Text == LocalizationService.Get(kind == MapRegionKind.Gate ? "Editor.RegionPickPoint" : "Editor.RegionPickRectangle"), $"Live translation state: picking={canvas.IsPickingRegion}, tool={canvas.Tool}, kind={kinds.SelectedValue}, status={status.Text}.");
            LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
            // Same release operation as canvas mouse-up: reverse rectangle, or a gate point at the origin.
            canvas.CompleteRegionPick(new(9, 10), new(6, 7));
            var expected = kind == MapRegionKind.Gate ? new TileRect(9, 10, 1, 1) : new(6, 7, 4, 4);
            Expect(!canvas.IsPickingRegion && Selected().Bounds == expected && changes == notifications + 1 && status.Text == LocalizationService.Get("Editor.RegionApplied"), "Map-picked region bounds were not applied atomically.");
            Expect(canvas.Selection == selection && ReferenceEquals(buffer, workspace.BufferedSelection) && SelectionFile.Serialize(buffer!).AsSpan().SequenceEqual(copyBytes), "Region picking modified ordinary copy selection or retained buffer.");
            workspace.Undo(); Expect(Selected().Bounds == original, "Map bounds undo did not refresh selected geometry."); workspace.Redo();
            Click(panel, "Editor.RegionProperties");
            Expect(propertyDraftPresented && Get<NativeRecord>(Control<ListBox>(workspace, "RecordList").SelectedItem, "Record").Offset == Selected().Record.Offset,"Region properties did not present the private draft for the selected record.");
            propertyDraftPresented=false;
            Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 0;
        }
        var tileSnapshot = document.TileFile.Data.ToArray();
        Expect(tileSnapshot.AsSpan().SequenceEqual(initial[document.TileFile]), "Region selection/add/modify painted terrain while Fill/Heart was selected.");
        var stable = Snapshot(); var count = changes;
        Click(panel, "Editor.RegionShow"); Expect(canvas.Selection == Selected().Bounds, "Show-bounds did not expose the selected region rectangle."); Exact(stable); Expect(changes == count, "Read-only region navigation consumed history.");
        // All three regions deliberately overlap before applying selection; point-pick cycles them without editing.
        document.SetMapRegionBounds(document.MapRegions().Single(region => region.Kind == MapRegionKind.Gate).Record, new(6, 7, 1, 1));
        stable = Snapshot(); count = changes;
        var hits = new HashSet<MapRegionKind>();
        for (var index = 0; index < 3; index++)
        { Click(panel, "Editor.RegionSelectMap"); canvas.CompleteRegionPick(new(6, 7), new(6, 7)); hits.Add(Selected().Kind); }
        Expect(hits.Count == 3 && status.Text == LocalizationService.Get("Editor.RegionSelected"), "Map hit testing did not cycle overlapping kinds."); Exact(stable); Expect(changes == count, "Hit testing changed data/history.");
        Click(panel, "Editor.RegionSelectMap"); canvas.CompleteRegionPick(new(0, 0), new(0, 0));
        Expect(status.Text == LocalizationService.Get("Editor.RegionNoHit"), "Empty region hit did not report localized read-only result."); Exact(stable);
        foreach (var kind in new[] { MapRegionKind.Area, MapRegionKind.Effect, MapRegionKind.Gate })
        {
            picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<MapRegion>(item, "Region").Kind == kind);
            canvas.SetSelection(new(0, 0, 2, 3)); count = changes;
            Click(panel, "Editor.RegionUseSelection");
            Expect(Selected().Bounds == (kind == MapRegionKind.Gate ? new TileRect(0, 0, 1, 1) : canvas.Selection) && changes == count + 1, $"Apply-selection state: kind={kind}, bounds={Selected().Bounds}, selection={canvas.Selection}, changes={changes-count}, status={status.Text}, detail={status.ToolTip}.");
            workspace.Undo();
        }
        stable = Snapshot();
        kinds.SelectedValue = (int)MapRegionKind.Effect;
        Click(panel, "Editor.RegionAddMap"); canvas.CompleteRegionPick(new(15, 15), new(15, 15));
        Expect(Selected().Kind == MapRegionKind.Effect && Selected().Bounds == new TileRect(15, 15, 1, 1), "Point-sized map effect was rejected.");
        workspace.Undo(); Exact(stable);
        Click(panel, "Editor.RegionChangeMap"); Expect(workspace.CancelRegionEditing() && !canvas.IsPickingRegion && status.Text == LocalizationService.Get("Editor.RegionCanceled"), "Cancel did not release pending region geometry."); Exact(stable);
        Click(panel, "Editor.RegionChangeMap"); tools.SelectedIndex = (int)DrawingTool.Brush;
        Expect(!canvas.IsPickingRegion, "An actual tool change retained a stale pending geometry edit."); Exact(stable);
        Click(panel, "Editor.RegionChangeMap"); workspace.Undo();
        Expect(!canvas.IsPickingRegion, "Undo retained pending record coordinates."); workspace.Redo(); Exact(stable);
        Click(panel, "Editor.RegionChangeMap"); picker.SelectedIndex = picker.SelectedIndex == 0 ? 1 : 0;
        Expect(!canvas.IsPickingRegion, "Region selection changed without canceling pending record edit."); Exact(stable);
        Click(panel, "Editor.RegionChangeMap"); workspace.Document = null;
        Expect(!canvas.IsPickingRegion && canvas.HighlightedRegion is null && !panel.IsEnabled && picker.Items.Count == 0 && status.Visibility == Visibility.Collapsed && ReferenceEquals(buffer, workspace.BufferedSelection), "Document close leaked region state or discarded copy buffer.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} map-region controls, point/rectangle picking and live translation");
    }

    private static void CheckNewMapDialog(string language)
    {
        foreach (var native in new[] { false, true })
        {
            var initial = new NewMapOptions("NewUI", "Display €", 32, 20, 5, 16, 0x09070101, NativeFormat: native);
            var dialog = new NewMapDialog(null, initial);
            try
            {
                var panel = (FrameworkElement)dialog.Content; panel.Measure(new Size(560, 900)); panel.Arrange(new Rect(0, 0, 560, panel.DesiredSize.Height)); panel.UpdateLayout();
                T Input<T>(string name) => (T)dialog.FindName(name);
                var width = Input<TextBox>("NewWidth"); var height = Input<TextBox>("NewHeight"); var square = Input<CheckBox>("NewSquare");
                var players = Input<ComboBox>("NewPlayers"); var versions = Input<ComboBox>("NewVersion"); var fills = Input<ComboBox>("NewFill");
                var format = Input<ComboBox>("NewFormat"); var hearts = Input<CheckBox>("NewHearts"); Expect(dialog.FindName("NewLargeConsent") is null,"Obsolete large-map checkbox retained");
                var create = Input<Button>("CreateMap"); var error = Input<TextBlock>("NewError"); var name = Input<TextBox>("NewDisplayName"); var fileName = Input<TextBox>("NewFileName");
                Expect(dialog.TryGetOptions(out var selected) && selected == initial && create.IsEnabled, "New-map dialog lost supplied original settings.");
                Expect(players.Items.Count == 5 && fills.Items.Count == 23 && versions.Items.Count == 3 && versions.IsEnabled == !native, "New-map options/version format guard are incomplete.");
                Expect(Get<string>(fills.SelectedItem, "Label") == LocalizationService.Get("Terrain.9") + " · " + LocalizationService.Get("Owner.7"), "Owned background label is not bilingual or lost owner identity.");
                square.IsChecked = true; width.Text = "7"; hearts.IsChecked = true;
                Expect(height.Text == "7" && !height.IsEnabled && !dialog.TryGetOptions(out _) && error.Text == LocalizationService.Get("Editor.NewProblem.StarterSpace"), "Small square map accepted overlapping keeper hearts.");
                players.SelectedItem = 1;
                Expect(dialog.TryGetOptions(out var small) && small.Width == 7 && small.Height == 7 && small.PlaceHearts, "Smallest one-keeper starter was rejected.");
                width.Text = "2147483647";
                Expect(!dialog.TryGetOptions(out _) && !create.IsEnabled && error.Text == LocalizationService.Get("Editor.NewProblem.Dimensions"), "Overflowing dimensions were accepted or misclassified.");
                width.Text = "121";
                Expect(dialog.TryGetOptions(out _) && create.IsEnabled,"Large valid settings disabled the source OK-to-question route");
                dialog.LargeMapPresenter=window=>window.Close();Expect(!dialog.TryAcceptOptions(),"Closing large-map question approved creation");
                dialog.LargeMapPresenter=window=>Expect(window.Accept(),"Large-map Yes refused");Expect(dialog.TryAcceptOptions(),"Large-map approval did not enable creation");
                square.IsChecked = false; width.Text = "32"; height.Text = "20"; players.SelectedItem = 5;
                Expect(height.IsEnabled, "Rectangular dimensions did not refresh.");
                name.Text = "Новая карта"; format.SelectedIndex = 0;
                Expect(!dialog.TryGetOptions(out _) && error.Text == LocalizationService.Get("Editor.NewProblem.DisplayName"), "DKM silently accepted unrepresentable Cyrillic display text.");
                format.SelectedIndex = 1;
                Expect(dialog.TryGetOptions(out _) && !versions.IsEnabled, "Native Unicode names or game-version asset guard failed.");
                fileName.Text = "../escape";
                Expect(!dialog.TryGetOptions(out _) && !create.IsEnabled && error.Text == LocalizationService.Get("Editor.NewProblem.FileName"), "New-map file path escaping was accepted.");
                var originalLanguage = LocalizationService.CurrentLanguage;
                LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
                Expect(error.Text == LocalizationService.Get("Editor.NewProblem.FileName") && hearts.Content is TextBlock { TextWrapping: TextWrapping.Wrap } heartLabel && heartLabel.Text == LocalizationService.Get("Editor.NewHearts"), "New-map live translation lost the active error/wrapped checkbox label.");
                Expect(fills.SelectedValue is uint preset && preset == initial.FillID && width.Text == "32" && height.Text == "20" && players.SelectedItem is 5, "Language switching changed new-map choices.");
                LocalizationService.SetLanguage(originalLanguage); fileName.Text = "NewUI";
                if (!native) { name.Text = "Display €"; format.SelectedIndex = 0; versions.SelectedValue = (byte)13; }
                Expect(dialog.TryGetOptions(out var options), "Valid new-map settings could not be collected from real controls.");
                var document = NativeMapDocument.CreateNewMap(options);
                Expect(TerrainLayoutDiagnostics.Validate(document).Count == 0 && document.IsLegacyProject == !native, "Actual dialog settings did not produce the requested valid starter format.");
                var workspace = new EditorWorkspace { Document = document };
                workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
                Expect(Control<MapCanvas>(workspace, "Canvas").Document == document && workspace.Document.Width == 32 && workspace.Document.Height == 20, "New map did not bind to the actual canvas.");
                workspace.Document = null;
                // Synthetic unsaved-shell fixture: no modal New/Discard dialogs.
                // The real dialog controls/factory/canvas were exercised above;
                // inject only the private shell document to test its header path.
                var window = new MainWindow(new AppSettings(), persistSettings: false);
                var documentField = typeof(MainWindow).GetField("document", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                try
                {
                    documentField.SetValue(window, document);
                    ((EditorWorkspace)window.FindName("Editor")).Document = document;
                    typeof(MainWindow).GetMethod("Document_Changed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, new object[] { window, EventArgs.Empty });
                    Expect(((TextBlock)window.FindName("NoMapText")).Visibility == Visibility.Collapsed && ((Grid)window.FindName("MetadataGrid")).Visibility == Visibility.Visible,
                        "Unsaved new map still displays 'no map loaded' in the actual shell.");
                    Expect(((TextBlock)window.FindName("WorkspaceTitle")).Text == options.DisplayName && ((TextBlock)window.FindName("DimensionsValue")).Text == "32 × 20"
                        && ((TextBlock)window.FindName("SizeValue")).Text == LocalizationService.Get("Editor.Unsaved"), "Unsaved header lost name/dimensions or invented a saved file size.");
                    window.ValidateDocument(document);
                    LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
                    Expect(((TextBlock)window.FindName("SizeValue")).Text == LocalizationService.Get("Editor.Unsaved") && ((TextBlock)window.FindName("WorkspaceTitle")).Text == options.DisplayName,
                        "Live translation reset an unsaved map header or changed its display name.");
                    LocalizationService.SetLanguage(language);
                }
                finally { documentField.SetValue(window, null); ((EditorWorkspace)window.FindName("Editor")).Document = null; window.Close(); }
                Console.WriteLine($"PASS {language} {(native ? "native" : "project")} new-map original options, validation and starter canvas");
                CheckLargeMapSharing(native,language);
            }
            finally { dialog.Close(); }
        }
    }
    private static T Control<T>(EditorWorkspace workspace, string name) => (T)workspace.FindName(name);
    private static void CheckTerrainValidation(string language, string directory)
    {
        var path = Path.Combine(directory, "validate-" + language + ".dkm");
        File.WriteAllBytes(path, TextMapFile.Parse("GUI_GENERATED_MAP\nValidateUI\nPLAYERS 1\nMAP\n16 X 16\n" + string.Concat(Enumerable.Repeat(string.Concat(Enumerable.Repeat("Oa", 16)) + "\n", 16)) + "EOF").ToArray());
        foreach (var legacy in new[] { false, true })
        {
            var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(16, 16, "ValidationUI");
            var saved = document.SaveCopy(Path.Combine(directory, "validation-" + language + "-" + legacy));
            var window = new MainWindow(new AppSettings(), persistSettings: false);
            try
            {
                window.OpenMapFromPath(saved);
                document = ((EditorWorkspace)window.FindName("Editor")).Document!;
                document.Paint(MapDrawing.Rectangle(new(1, 1, 5, 5), true), new(14, 3, 1, 217));
                document.Paint(MapDrawing.Rectangle(new(9, 9, 5, 5), true), new(14, 3, 1, 218));
                var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
                var content = (FrameworkElement)window.Content;
                content.Measure(new Size(1400, 900)); content.Arrange(new Rect(0, 0, 1400, 900)); content.UpdateLayout();
                var diagnostics = window.ValidateDocument(document);
                Expect(diagnostics.Count(item => item.Code == "DK2N022") == 1, "The actual main-window validation did not check duplicate hearts.");
                var list = (ItemsControl)window.FindName("DiagnosticsList");
                object Duplicate() => list.Items.Cast<object>().Single(item => Get<string>(item, "Code") == "DK2N022");
                Expect(Get<string>(Duplicate(), "Message").StartsWith(LocalizationService.Get("Diagnostic.DK2N022"), StringComparison.Ordinal), "Validation result is not localized.");
                Expect(Get<Brush>(Duplicate(), "Color") == (Brush)window.FindResource("Brush.Error"), "Fatal terrain diagnostic is not displayed as an error.");
                LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
                Expect(Get<string>(Duplicate(), "Message").StartsWith(LocalizationService.Get("Diagnostic.DK2N022"), StringComparison.Ordinal), "Live language switching discarded terrain results or retained the old label.");
                LocalizationService.SetLanguage(language);
                foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "UI validation mutated map data.");
                document.Undo();
                Expect(!list.Items.Cast<object>().Any(item => Get<string>(item, "Code") == "DK2N022"), "Map changes retained a stale duplicate-heart result.");
                diagnostics = window.ValidateDocument(document);
                Expect(!diagnostics.Any(item => item.Code is "DK2N022" or "DK2N023" or "DK2N024"), "Validation did not refresh after undoing the duplicate heart.");
                document.Undo(); diagnostics = window.ValidateDocument(document);
                Expect(diagnostics.Any(item => item.Code == "DK2N023"), "Validation missed a map with no heart.");
                document.Redo(); diagnostics = window.ValidateDocument(document);
                Expect(!diagnostics.Any(item => item.Code == "DK2N023"), "Validation did not refresh after redo.");
                document.Undo(); // Restore the loaded file; closing must not prompt.
                Expect(!document.HasChanges, "UI diagnostic case did not restore its loaded source.");
                Console.WriteLine($"PASS {language} {(legacy ? "project" : "native")} main-window terrain validation and live translation");
            }
            finally { window.Close(); }
        }
    }
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Click(StackPanel panel, string resource)
    {
        var button = panel.Children.OfType<Button>().Concat(panel.Children.OfType<WrapPanel>().SelectMany(wrap => wrap.Children.OfType<Button>())).Single(button => Equals(button.Content, LocalizationService.Get(resource)));
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private static bool Select(ItemsControl parent, Func<object, bool> predicate)
    {
        parent.UpdateLayout();
        foreach (var item in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem container) continue;
            if (predicate(item)) { container.IsSelected = true; return true; }
            container.IsExpanded = true; if (Select(container, predicate)) return true;
        }
        return false;
    }

    private static void CheckScriptTree(NativeMapDocument document, string language)
    {
        if (document.IsLegacyProject) document.AddLegacyRecord("TriggerLink", new(0, 0));
        var workspace = new EditorWorkspace { Document = document };
        var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); tabs.SelectedIndex = 2;
        workspace.ScenarioCreationPresenter = CompleteDefaultCreation;
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        var tree = Control<TreeView>(workspace, "ScriptTree");
        Expect(Select(tree, item => item.GetType().GetProperty("Incoming")?.GetValue(item) is ScriptEdge { Scope: 32 } && Get<ushort>(item, "EventID") == 0), "Map scenario scope is not selectable in the actual tree.");
        var picker = Control<ComboBox>(workspace, "ScriptTypePicker");
        Expect(!picker.Items.Cast<object>().Any(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == "TRIGGER_DOORDESTROYED"), "Tree type picker ignores original scope restrictions.");
        picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == "TRIGGER_TIMEELAPSED");
        Click(Control<StackPanel>(workspace, "ScriptPanel"), "Editor.ScriptInsert");
        var graph = ScriptGraph.Build(document); var conditionID = graph.Roots.Single(edge => edge.Scope == 32).Target;
        Expect(conditionID != 0 && Get<ushort>(tree.SelectedItem, "EventID") == conditionID, "Tree insertion did not select its new connected condition.");
        picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == "ACTION_ALLIES");
        Click(Control<StackPanel>(workspace, "ScriptPanel"), "Editor.ScriptInsert");
        graph = ScriptGraph.Build(document); var child = graph.Events[conditionID].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and"); var actionID = child.Target;
        Expect(actionID != 0 && Get<ushort>(tree.SelectedItem, "EventID") == actionID, "Tree insertion did not connect/select a child action.");
        if (document.IsLegacyProject) Expect(actionID >= 32768 && conditionID <= 32767, "Project tree lost signed condition/action ID categories.");
        Click(Control<StackPanel>(workspace, "ScriptPanel"), "Editor.ScriptProperties");
        Expect(tabs.SelectedIndex == 1 && Control<DataGrid>(workspace, "PropertyGrid").Items.Cast<object>().All(row => ScriptGraph.ID(Get<NativeRecord>(row, "Record")) == actionID), "Tree property navigation selected another record.");
        tabs.SelectedIndex = 2; workspace.UpdateLayout();
        Expect(Select(tree, item => Get<ushort>(item, "EventID") == conditionID), "Connected condition is missing from tree.");
        Click(Control<StackPanel>(workspace, "ScriptPanel"), "Editor.ScriptDetach");
        graph = ScriptGraph.Build(document);
        Expect(graph.Events.ContainsKey(actionID) && graph.Events[conditionID].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and").Target == 0, "Tree detach deleted a branch or edited another link.");
        document.Undo(); graph = ScriptGraph.Build(document);
        Expect(graph.Events[conditionID].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and").Target == actionID, "Tree detach undo did not restore connection.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(Get<string>(tree.Items[0], "Label").StartsWith(language == "en" ? "Карта" : "Map", StringComparison.Ordinal), "Live tree switching retained the old language.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        var next = ScriptGraph.Build(document).Events[actionID].Edges.Single(); document.SetField(next.Owner, next.Field, conditionID.ToString());
        bool HasLabel(IEnumerable<object> items, string text)
        {
            foreach (var item in items)
                if (Get<string>(item, "Label").Contains(text, StringComparison.Ordinal) || HasLabel(((System.Collections.IEnumerable)item.GetType().GetProperty("Children")!.GetValue(item)!).Cast<object>(), text)) return true;
            return false;
        }
        Expect(HasLabel(tree.Items.Cast<object>(), LocalizationService.Get("Editor.ScriptCycle")), "Existing cycle was hidden or could not be rendered safely.");
        document.SetScriptLink(next.Owner, next.Field, 0);
        workspace.Document = null;
        Expect(tree.Items.Count == 0 && Control<ComboBox>(workspace, "ScriptLinkPicker").Items.Count == 0, "Scenario tree retained editable closed-document links.");
        Console.WriteLine($"PASS WPF script tree {language} {(document.IsLegacyProject ? "project" : "native")}");
    }

    private static void CheckEditorTerrainControls(NativeMapDocument document, string language)
    {
        if (document.IsLegacyProject) { document.AddLegacyRecord("Creature", new(0, 1)); document.AddLegacyRecord("Creature", new(1, 1)); }
        else foreach (var point in new[] { new TilePoint(0, 1), new TilePoint(1, 1) }) document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), point);
        foreach (var owner in document.ScriptOwners().Where(owner => owner.Scope == 2).ToArray())
        { var root = document.EnsureScriptScope(owner); document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED")); }
        if (document.IsLegacyProject)
        {
            var info = document.Records().Single(record => record.Category == "Project.MAPPROPERTIES"); document.SetField(info, info.Fields.Single(field => field.Name == "FillID"), 0x06020101u.ToString());
        }
        document.Paint(MapDrawing.Rectangle(new(0, 0, 6, 6), true), new(2, 2, 1, 211));
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var workspace = new EditorWorkspace { Document = document };
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        var canvas = Control<MapCanvas>(workspace, "Canvas");
        Expect(Control<TextBlock>(workspace, "PositionText").Text.Contains(LocalizationService.Get("Editor.ProtectedBorder"), StringComparison.Ordinal), "Initial edge cursor did not identify the protected border.");
        void Exact() { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "WPF editor paint/cut undo did not restore exact bytes."); }
        void Border()
        { for (var y = 0; y < 6; y++) for (var x = 0; x < 6; x++) if (document.IsEditorProtectedCell(x, y)) Expect(document.GetTile(x, y) == new MapTile(2, 2, 1, 211), "WPF map tools rewrote original border bytes."); }
        Control<ComboBox>(workspace, "TerrainPicker").SelectedValue = 4;
        Control<ComboBox>(workspace, "OwnerPicker").SelectedValue = 7;
        Control<ComboBox>(workspace, "VariantPicker").SelectedIndex = 1;
        canvas.PaintStroke(MapDrawing.Rectangle(new(0, 0, 6, 6), true)); Border();
        Expect(document.GetTile(1, 1) == new MapTile(4, 2, 1), "Actual palette/canvas paint failed to normalize world owner/variant."); workspace.Undo(); Exact();
        workspace.SetSelection(new(1, 1, 2, 2)); canvas.FillAt(new(1, 1)); Border();
        Expect(document.GetTile(1, 1).Terrain == 4 && document.GetTile(3, 3).Terrain == 2, "Canvas flood fill ignored its current selection."); workspace.Undo(); Exact();
        workspace.ClearSelection(); canvas.FillAt(new(0, 1)); Exact();
        workspace.SelectAll(); workspace.Copy();
        Expect(workspace.BufferedSelection is { ObjectCount: 1 } && workspace.BufferedSelection.ScriptCount == (document.IsLegacyProject ? 2 : 1), "Actual copy command included a protected-edge thing or unrelated scenario.");
        Control<ComboBox>(workspace, "TerrainPicker").SelectedValue = 14;
        workspace.Cut(); Border();
        Expect(document.GetTile(1, 1) == new MapTile(document.IsLegacyProject ? (byte)6 : (byte)2, 2, 1)
            && document.Records().Count(record => record.Category == (document.IsLegacyProject ? "Project.MAPTHING" : "GAMEKEEPER")) == 1,
            "Actual cut command created heart tiles, ignored the project fill or removed a border thing.");
        workspace.Undo(); Exact(); workspace.Redo(); Border(); workspace.Undo(); Exact();
        var buffer = workspace.BufferedSelection; workspace.Document = null; canvas.PaintStroke([new(1, 1)]); canvas.FillAt(new(1, 1));
        Expect(ReferenceEquals(buffer, workspace.BufferedSelection), "Closed-document map painting lost the independent copy buffer.");
        Console.WriteLine($"PASS WPF protected-border painting, selected flood and special cut fill {language} {(document.IsLegacyProject ? "project" : "native")}");
    }

    private static void CheckTerrainPlacement(NativeMapDocument document, string language)
    {
        var thing = document.IsLegacyProject ? document.AddLegacyRecord("Creature", new(3, 3))
            : document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), new(3, 3));
        var root = document.EnsureScriptScope(document.ScriptOwners().Single(owner => owner.Scope == 2));
        document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        document.Paint(MapDrawing.Rectangle(new(0, 0, 16, 16), true), new(2, 2, 1, 213));
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var workspace = new EditorWorkspace { Document = document };
        workspace.Measure(new Size(1400, 900)); workspace.Arrange(new Rect(0, 0, 1400, 900)); workspace.UpdateLayout();
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var status = Control<TextBlock>(workspace, "PlacementStatus"); var dimensions = Control<TextBlock>(workspace, "FootprintStatus");
        var tool = Control<ComboBox>(workspace, "ToolPicker"); var terrain = Control<ComboBox>(workspace, "TerrainPicker");
        var owner = Control<ComboBox>(workspace, "OwnerPicker"); var variant = Control<ComboBox>(workspace, "VariantPicker");
        void Exact() { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "WPF terrain-placement rejection/undo changed bytes."); }
        void Palette(int id, int v = 1) { terrain.SelectedValue = id; owner.SelectedValue = 7; variant.SelectedIndex = v - 1; }
        Palette(14); tool.SelectedValue = (int)DrawingTool.Ellipse;
        Expect(dimensions.Visibility == Visibility.Visible && dimensions.Text.Contains("5 × 5", StringComparison.Ordinal) && canvas.PaintFootprint.FixedSize
            && canvas.PlacementPreview == new TileRect(0, 0, 5, 5), "Actual palette/preview did not expose a fixed heart footprint.");
        workspace.SetSelection(new(3, 3, 5, 5)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        foreach (var selectedTool in new[] { DrawingTool.Brush, DrawingTool.Line, DrawingTool.Rectangle, DrawingTool.FilledRectangle, DrawingTool.Ellipse, DrawingTool.FilledEllipse, DrawingTool.Fill })
        {
            tool.SelectedValue = (int)selectedTool;
            if (selectedTool == DrawingTool.Fill) canvas.FillAt(new(3, 3)); else canvas.DrawGesture(new(3, 3), new(10, 10));
            Expect(document.GetTile(3, 3) == new MapTile(14, 7, 1) && document.GetTile(7, 7) == new MapTile(14, 7, 1)
                && document.GetTile(8, 8) == new MapTile(2, 2, 1, 213) && document.GetTile(0, 0) == new MapTile(2, 2, 1, 213), "A heart gesture/fill did not stamp precisely 5×5.");
            Expect(status.Visibility == Visibility.Collapsed && ReferenceEquals(buffer, workspace.BufferedSelection)
                && document.Records().Count(record => record.Category == thing.Category) == 1 && ScriptGraph.Build(document).Events.Count == 1, "Terrain placement changed physical owners/scripts or the copy buffer.");
            workspace.Undo(); Exact(); workspace.Redo(); Expect(document.GetTile(7, 7).Terrain == 14, "Footprint redo did not restore the rectangle."); workspace.Undo(); Exact();
        }
        workspace.ClearSelection(); tool.SelectedValue = (int)DrawingTool.FilledRectangle;
        var undo = document.CanUndo; var redo = document.CanRedo;
        canvas.DrawGesture(new(15, 15), new(10, 10)); Exact();
        Expect(status.Visibility == Visibility.Visible && status.Text == LocalizationService.Get("Editor.Placement.ProtectedFootprint")
            && document.CanUndo == undo && document.CanRedo == redo, "Border footprint rejection mutated history or did not explain the cause.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(status.Visibility == Visibility.Visible && status.Text == LocalizationService.Get("Editor.Placement.ProtectedFootprint")
            && dimensions.Text.StartsWith(LocalizationService.Get("Editor.FootprintInfo").Split('{')[0], StringComparison.Ordinal), "Live translation cleared or failed to translate footprint validation.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        workspace.SetSelection(new(3, 3, 2, 2)); canvas.DrawGesture(new(3, 3), new(7, 7)); Exact();
        Expect(status.Text == LocalizationService.Get("Editor.Placement.SelectionClipsFootprint"), "Selection clipping silently produced a broken heart.");
        workspace.ClearSelection(); Palette(12); tool.SelectedValue = (int)DrawingTool.Ellipse;
        Expect(dimensions.Text.Contains("3–5 × 3–5", StringComparison.Ordinal) && status.Visibility == Visibility.Collapsed, "Portal dimensions or palette warning reset failed.");
        canvas.DrawGesture(new(6, 6), new(2, 2));
        Expect(document.GetTile(2, 2) == new MapTile(12, 7, 1) && document.GetTile(6, 6) == new MapTile(12, 7, 1) && document.GetTile(3, 3).Terrain == 12, "Reversed portal ellipse did not force a filled 5×5 rectangle."); workspace.Undo(); Exact();
        tool.SelectedValue = (int)DrawingTool.Fill; canvas.FillAt(new(3, 3));
        Expect(document.GetTile(5, 5).Terrain == 12 && document.GetTile(6, 6).Terrain == 2, "Portal fill flooded the map instead of stamping minimum 3×3."); workspace.Undo(); Exact();
        Palette(40); tool.SelectedValue = (int)DrawingTool.Rectangle; canvas.DrawGesture(new(6, 6), new(5, 5));
        Expect(document.GetTile(8, 8).Terrain == 40 && document.GetTile(5, 5).Terrain == 2, "Undersized reversed mercenary-portal gesture did not expand positively."); workspace.Undo(); Exact();
        Palette(37); canvas.DrawGesture(new(3, 3), new(10, 10));
        Expect(document.GetTile(5, 3) == new MapTile(37, 1, 1) && document.GetTile(3, 4).Terrain == 2, "Horizontal oriented gate dimensions/fixed hero owner differ from source."); workspace.Undo(); Exact();
        variant.SelectedIndex = 1; canvas.DrawGesture(new(3, 3), new(10, 10));
        Expect(dimensions.Text.Contains("1 × 3", StringComparison.Ordinal) && document.GetTile(3, 5) == new MapTile(37, 1, 2)
            && document.GetTile(4, 3).Terrain == 2, "Vertical oriented gate did not retain its catalogue variant/footprint.");
        var sampled = document.GetTile(3, 5); Palette(4); tool.SelectedValue = (int)DrawingTool.Sample;
        canvas.SampleAt(new(3, 5));
        Expect(canvas.PaintTile == sampled && Equals(terrain.SelectedValue, 37) && Equals(owner.SelectedValue, 1) && variant.SelectedIndex == 1
            && canvas.PaintFootprint == new EditorTerrainFootprint(1, 3, 1, 3) && dimensions.Visibility == Visibility.Collapsed,
            "Sampling from another palette state lost gate owner/variant while picker callbacks ran.");
        tool.SelectedValue = (int)DrawingTool.Rectangle;
        Expect(dimensions.Text.Contains("1 × 3", StringComparison.Ordinal) && dimensions.Visibility == Visibility.Visible, "Sampled orientation did not reach the next painting footprint.");
        workspace.Undo(); Exact();
        Palette(34); tool.SelectedValue = (int)DrawingTool.Line; canvas.DrawGesture(new(3, 3), new(3, 10));
        Expect(dimensions.Text.Contains("3 × 2–∞", StringComparison.Ordinal) && document.GetTile(5, 10) == new MapTile(34, 1, 1)
            && document.GetTile(6, 10).Terrain == 2, "Variable-height hero gate was flattened to minimum or painted as a line."); workspace.Undo(); Exact();
        Palette(4); tool.SelectedValue = (int)DrawingTool.Fill;
        Expect(dimensions.Visibility == Visibility.Collapsed && canvas.PlacementPreview is null, "Normal terrain retained a special footprint preview.");
        workspace.SetSelection(new(3, 3, 2, 2)); canvas.FillAt(new(3, 3));
        Expect(document.GetTile(4, 4) == new MapTile(4, 2, 1) && document.GetTile(5, 5).Terrain == 2, "Ordinary selected flood regressed after footprint routing."); workspace.Undo(); Exact();
        Palette(14); workspace.Document = NativeMapDocument.Create(4, 4, "SmallFootprint");
        canvas.DrawGesture(new(1, 1), new(1, 1));
        Expect(status.Text == LocalizationService.Get("Editor.Placement.MapTooSmall") && canvas.PlacementPreview is null, "Small-map footprint failure was not localized.");
        workspace.Document = null; canvas.DrawGesture(new(1, 1), new(1, 1)); canvas.FillAt(new(1, 1)); canvas.SampleAt(new(1, 1));
        Expect(status.Visibility == Visibility.Collapsed && dimensions.Visibility == Visibility.Collapsed && canvas.PlacementPreview is null
            && ReferenceEquals(buffer, workspace.BufferedSelection), "Closed-document footprint state was retained or buffer was lost.");
        Console.WriteLine($"PASS WPF heart/portal/gate footprint gestures, orientation and live validation {language} {(document.IsLegacyProject ? "project" : "native")}");
    }

    private static void CheckProtectedSelections(NativeMapDocument document, string language, string directory)
    {
        var category = document.IsLegacyProject ? "Project.MAPTHING" : "GAMEKEEPER";
        foreach (var point in new[] { new TilePoint(0, 1), new TilePoint(1, 2) })
            if (document.IsLegacyProject) document.AddLegacyRecord("Creature", point);
            else document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), point);
        foreach (var owner in document.ScriptOwners().Where(owner => owner.Scope == 2).ToArray())
        { var root = document.EnsureScriptScope(owner); document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED")); }
        for (var y = 0; y < 6; y++) for (var x = 0; x < 6; x++) document.Paint([new(x, y)], new((byte)(2 + y * 6 + x), 3, 1, (byte)(180 + y * 6 + x)));
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var workspace = new EditorWorkspace { Document = document };
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        workspace.SetSelection(new(0, 0, 3, 3)); workspace.Copy();
        var buffer = workspace.BufferedSelection!;
        Expect(buffer.ProtectedTileCount == 5 && buffer.ObjectCount == 1 && buffer.ScriptCount == (document.IsLegacyProject ? 2 : 1), "UI copy did not preserve its independent source mask.");
        var bufferBytes = SelectionFile.Serialize(buffer);
        var path = Path.Combine(directory, $"mask-{language}-{document.IsLegacyProject}.dkz"); SelectionFile.SaveNew(path, buffer);
        var reopened = SelectionFile.Read(path); workspace.LoadSelectionIntoBuffer(reopened);
        Expect(reopened.ProtectedTileCount == 5 && SelectionFile.Serialize(reopened).AsSpan().SequenceEqual(bufferBytes), "Saved UI buffer lost protection/raw bytes.");
        void Exact() { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "WPF protected-selection undo changed bytes."); }
        foreach (var (command, map) in new (Action, Func<int, int, TilePoint>)[]
        {
            (workspace.Rotate, (x, y) => new(2 - y, x)), (workspace.RotateCounterclockwise, (x, y) => new(y, 2 - x)),
            (workspace.MirrorHorizontal, (x, y) => new(2 - x, y)), (workspace.MirrorVertical, (x, y) => new(x, 2 - y))
        })
        {
            var expected = before.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++)
            {
                var target = map(x, y); if (document.IsEditorProtectedCell(target.X, target.Y)) continue;
                var tile = x == 0 || y == 0 ? new MapTile(1, 2, 1) : document.GetTile(x, y);
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(expected[document.TileFile].AsSpan(36 + (target.Y * 6 + target.X) * 4), tile.Packed);
            }
            foreach (var record in document.Records().Where(record => record.Category == category))
            {
                var x = record.Fields.Single(field => field.Name == "x" || field.Name.EndsWith("LocationX", StringComparison.Ordinal));
                var y = record.Fields.Single(field => field.Name == "y" || field.Name.EndsWith("LocationY", StringComparison.Ordinal));
                var point = map(int.Parse(record.Read(x)), int.Parse(record.Read(y)));
                x.Encode(point.X.ToString()).CopyTo(expected[record.File], record.Offset + x.Offset); y.Encode(point.Y.ToString()).CopyTo(expected[record.File], record.Offset + y.Offset);
            }
            command(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Actual WPF transform lost original protected-tile/all-object ordering.");
            Expect(ReferenceEquals(workspace.BufferedSelection, reopened), "Protected transform replaced the independent buffer.");
            workspace.Undo(); Exact(); workspace.Redo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Protected transform redo changed its result."); workspace.Undo(); Exact();
        }
        workspace.SetSelection(new(2, 2, 1, 1)); workspace.Paste();
        Expect(document.GetTile(2, 2) == new MapTile(1, 2, 1) && document.GetTile(3, 4).Terrain == 15
            && document.Records().Count(record => record.Category == category) == 3 && ScriptGraph.Build(document).Events.Count == 3,
            "Actual DKZ buffer paste did not fill masks or retain the accepted object's graph.");
        for (var y = 0; y < 6; y++) for (var x = 0; x < 6; x++) if (document.IsEditorProtectedCell(x, y))
            Expect(document.GetTile(x, y).Packed == System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(before[document.TileFile].AsSpan(36 + (y * 6 + x) * 4)), "Actual DKZ paste changed boundary padding.");
        workspace.Undo(); Exact();
        workspace.SetSelection(new(0, 0, 3, 3)); workspace.CopyRotatedSelection();
        Expect(workspace.BufferedSelection is { ProtectedTileCount: 5, ObjectCount: 1 } && workspace.BufferedSelection.IsProtectedTile(0, 0)
            && workspace.BufferedSelection.IsProtectedTile(2, 2) && !workspace.BufferedSelection.IsProtectedTile(0, 1), "Explicit UI rotated-copy misaligned mask and physical payload.");
        Expect(SelectionFile.Serialize(buffer).AsSpan().SequenceEqual(bufferBytes), "WPF mask workflow modified the previous copy snapshot.");
        var retained = workspace.BufferedSelection; workspace.Document = null;
        Expect(ReferenceEquals(workspace.BufferedSelection, retained), "Closing the document dropped a masked buffer.");
        Console.WriteLine($"PASS WPF protected-mask DKZ paste and original all-object transform ordering {language} {(document.IsLegacyProject ? "project" : "native")}");
    }

    private static void CheckTransformControls(NativeMapDocument document, string language)
    {
        var creature = document.IsLegacyProject ? document.AddLegacyRecord("Creature", new(1, 1))
            : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEKEEPER"), new(1, 1));
        var root = document.EnsureScriptScope(document.ScriptOwners().Single(owner => owner.Scope == 2));
        document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        document.Paint([new(1, 1)], new(6, 3, 1));
        var bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var workspace = new EditorWorkspace { Document = document };
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var status = Control<TextBlock>(workspace, "TransformStatus");
        var square = new TileRect(1, 1, 2, 2); workspace.SetSelection(square); workspace.Copy();
        var buffer = workspace.BufferedSelection!; var bufferBytes = SelectionFile.Serialize(buffer);
        void Exact() { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "WPF transform undo/rejection changed bytes."); }
        foreach (var (command, point) in new[] { (new Action(workspace.Rotate), new TilePoint(2, 1)), (new Action(workspace.RotateCounterclockwise), new TilePoint(1, 2)),
            (new Action(workspace.MirrorHorizontal), new TilePoint(2, 1)), (new Action(workspace.MirrorVertical), new TilePoint(1, 2)) })
        {
            command();
            var current = document.Records().Single(item => item.File == creature.File && item.Category == creature.Category && item.Offset == creature.Offset);
            var x = current.Fields.Single(field => field.Name == "x" || field.Name.EndsWith("LocationX", StringComparison.Ordinal));
            var y = current.Fields.Single(field => field.Name == "y" || field.Name.EndsWith("LocationY", StringComparison.Ordinal));
            Expect(document.GetTile(point.X, point.Y).Terrain == 6 && current.Read(x) == point.X.ToString() && current.Read(y) == point.Y.ToString(), "WPF transform command used the wrong direction or misaligned its object.");
            Expect(canvas.Selection == square && status.Visibility == Visibility.Collapsed && ReferenceEquals(workspace.BufferedSelection, buffer), "In-place transform changed bounds/buffer or retained a warning.");
            workspace.Undo(); Exact(); workspace.Redo(); Expect(document.GetTile(point.X, point.Y).Terrain == 6, "WPF transform redo lost its result."); workspace.Undo(); Exact();
        }
        workspace.SetSelection(new(1, 1, 2, 3)); var canUndo = document.CanUndo; var canRedo = document.CanRedo;
        workspace.Rotate(); workspace.RotateCounterclockwise(); Exact();
        Expect(status.Visibility == Visibility.Visible && status.Text == LocalizationService.Get("Editor.RotationSquareRequired")
            && document.CanUndo == canUndo && document.CanRedo == canRedo && ReferenceEquals(workspace.BufferedSelection, buffer), "A rejected nonsquare rotation mutated history/buffer or did not explain the original restriction.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(status.Text == LocalizationService.Get("Editor.RotationSquareRequired"), "Rotation validation retained the old language.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        workspace.CopyRotatedSelection(); Exact();
        Expect(workspace.BufferedSelection is { Tiles.Width: 3, Tiles.Height: 2 } && !ReferenceEquals(workspace.BufferedSelection, buffer)
            && status.Visibility == Visibility.Collapsed, "The explicit rotated-copy command did not produce the separate rectangle buffer.");
        Expect(SelectionFile.Serialize(buffer).AsSpan().SequenceEqual(bufferBytes), "Rotated-copy command mutated its previous snapshot.");
        var rotated = workspace.BufferedSelection;
        workspace.SetSelection(new(0, 0, 1, 3)); workspace.MirrorHorizontal(); Exact();
        Expect(status.Text == LocalizationService.Get("Editor.ReflectionSizeRequired"), "Horizontal reflection accepted a one-column selection.");
        workspace.SetSelection(new(0, 0, 3, 1)); workspace.MirrorVertical(); Exact();
        Expect(status.Text == LocalizationService.Get("Editor.ReflectionSizeRequired") && ReferenceEquals(workspace.BufferedSelection, rotated), "Vertical reflection accepted a one-row selection or replaced the buffer.");
        workspace.ClearSelection(); workspace.RotateCounterclockwise();
        Expect(status.Text == LocalizationService.Get("Editor.SelectionRequired"), "Rotation without a selection did not explain the missing selection.");
        workspace.Document = null; workspace.Rotate(); workspace.RotateCounterclockwise(); workspace.CopyRotatedSelection();
        Expect(status.Visibility == Visibility.Collapsed && canvas.Selection is null && ReferenceEquals(workspace.BufferedSelection, rotated), "Closed-document transforms retained warnings or lost the independent buffer.");
        Console.WriteLine($"PASS WPF clockwise/counterclockwise rotation, reflection guards and explicit rotated buffer {language} {(document.IsLegacyProject ? "project" : "native")}");
    }

    private static void CheckSelectionBounds(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document };
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var status = Control<TextBlock>(workspace, "SelectionStatus");
        var bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var canUndo = document.CanUndo; var canRedo = document.CanRedo;
        var changes = 0; document.Changed += (_, _) => changes++;
        Expect(canvas.Selection is null && status.Text == LocalizationService.Get("Editor.NoSelection"), "An unopened numeric selection retained old bounds.");
        var dialog = workspace.CreateSelectionBoundsDialog()!;
        try
        {
            var content = (FrameworkElement)dialog.Content;
            content.Measure(new Size(440, 600)); content.Arrange(new Rect(0, 0, 440, content.DesiredSize.Height)); content.UpdateLayout();
            var inputs = new[] { "SelectionX", "SelectionY", "SelectionWidth", "SelectionHeight" }.Select(name => (TextBox)dialog.FindName(name)).ToArray();
            var apply = (Button)dialog.FindName("ApplySelection"); var cancel = (Button)dialog.FindName("CancelSelection"); var error = (TextBlock)dialog.FindName("SelectionError");
            void Enter(params string[] values) { for (var index = 0; index < inputs.Length; index++) inputs[index].Text = values[index]; }
            Expect(dialog.TryGetRectangle(out var initial) && initial == new TileRect(1, 1, 1, 1) && canvas.Selection is null,
                "The original empty-selection default is not 1,1 / 1×1, or opening the dialog already edited the selection.");
            Expect(apply.IsDefault && cancel.IsCancel && dialog.Title == LocalizationService.Get("Menu.ChangeSelection"), "Numeric selection lost Enter/Escape or translated title.");
            Enter("0", "0", document.Width.ToString(), document.Height.ToString());
            Expect(dialog.TryGetRectangle(out var rectangle) && rectangle == new TileRect(0, 0, document.Width, document.Height)
                && apply.IsEnabled && error.Visibility == Visibility.Collapsed, "Full-map numeric selection was incorrectly restricted to width/height minus one.");
            workspace.SetSelection(rectangle); workspace.Copy(); var buffer = workspace.BufferedSelection;
            Enter((document.Width - 1).ToString(), (document.Height - 1).ToString(), "1", "1");
            Expect(dialog.TryGetRectangle(out rectangle), "The last tile cannot be selected numerically.");
            workspace.SetSelection(rectangle);
            Expect(canvas.Selection == rectangle && status.Text.Contains("1×1", StringComparison.Ordinal), "Accepted numeric selection did not update actual canvas bounds/status.");
            foreach (var invalid in new[]
            {
                new[] { "-1", "0", "1", "1" }, new[] { "0", "-1", "1", "1" },
                new[] { "0", "0", "0", "1" }, new[] { "0", "0", "1", "-1" },
                new[] { "0", "0", "1.5", "1" }, new[] { "", "0", "1", "1" },
                new[] { "0", "0", "2147483647", "1" }, new[] { "2147483647", "0", "1", "1" },
                new[] { document.Width.ToString(), "0", "1", "1" }, new[] { "0", document.Height.ToString(), "1", "1" },
                new[] { (document.Width - 1).ToString(), "0", "2", "1" }, new[] { "0", (document.Height - 1).ToString(), "1", "2" }
            })
            {
                Enter(invalid);
                Expect(!dialog.TryGetRectangle(out _) && !apply.IsEnabled && error.Visibility == Visibility.Visible,
                    "Invalid/overflowing numeric bounds enabled the selection command: " + string.Join(",", invalid));
                Expect(canvas.Selection == rectangle && ReferenceEquals(workspace.BufferedSelection, buffer), "Invalid numeric input changed the selection or copy buffer.");
            }
            var other = language == "en" ? "ru" : "en"; LocalizationService.SetLanguage(other); workspace.RefreshLanguage();
            Expect(dialog.Title == LocalizationService.Get("Menu.ChangeSelection") && Equals(apply.Content, LocalizationService.Get("Editor.SelectRectangle"))
                && error.Text == LocalizationService.Get("Editor.SelectionBoundsError") && status.Text.StartsWith(LocalizationService.Get("Editor.SelectionInfo").Split('{')[0], StringComparison.Ordinal),
                "Selection dialog, validation or canvas status did not switch language live.");
            LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
            var existing = workspace.CreateSelectionBoundsDialog()!;
            try { Expect(existing.TryGetRectangle(out var current) && current == rectangle, "Reopening numeric bounds ignored the current selection."); }
            finally { existing.Close(); }
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Numeric selection changed map bytes.");
            Expect(changes == 0 && document.CanUndo == canUndo && document.CanRedo == canRedo, "Selection-only editing created a map history entry.");
            Expect(ReferenceEquals(workspace.BufferedSelection, buffer), "Changing/canceling numeric bounds replaced the copy buffer.");
        }
        finally { dialog.Close(); }
        var tiny = new SelectionBoundsDialog(null, 1, 1, null);
        try { Expect(tiny.TryGetRectangle(out var rectangle) && rectangle == new TileRect(0, 0, 1, 1), "One-tile map has an out-of-map default selection."); }
        finally { tiny.Close(); }
        document.Resize(document.Width + 1, document.Height + 1, 0, 0, new(2, 2, 1));
        workspace.SetSelection(new(document.Width - 1, document.Height - 1, 1, 1));
        workspace.Undo(); Expect(canvas.Selection is null && status.Text == LocalizationService.Get("Editor.NoSelection"), "Resize undo retained an out-of-map numeric selection.");
        workspace.Redo(); workspace.SetSelection(new(0, 0, 1, 1)); workspace.Undo();
        Expect(canvas.Selection == new TileRect(0, 0, 1, 1), "Resize undo unnecessarily discarded an in-bounds selection.");
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Selection cleanup broke exact resize undo.");
        workspace.ClearSelection(); workspace.Document = null;
        Expect(workspace.CreateSelectionBoundsDialog() is null && canvas.Selection is null, "Numeric selection can still edit a closed document.");
        Console.WriteLine($"PASS WPF numeric selection bounds, validation, live translation and resize undo {language} {(document.IsLegacyProject ? "project" : "native")}");
    }

    private static void CheckCopyBuffer(NativeMapDocument source, string language, string directory)
    {
        var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
        if (source.IsLegacyProject) source.AddLegacyRecord("Creature", new(1, 1));
        else source.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), new(1, 1));
        var owner = source.ScriptOwners().Single(item => item.Scope == 2); var root = source.EnsureScriptScope(owner); source.InsertScriptEvent(root.Owner, root.Field, timer);
        source.Paint([new(1, 1)], new(6, 3, 1));
        var original = source.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var workspace = new EditorWorkspace { Document = source };
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var status = Control<TextBlock>(workspace, "ClipboardStatus");
        workspace.SelectAll(); workspace.Copy();
        var selection = workspace.BufferedSelection ?? throw new InvalidOperationException("Copy command did not fill the independent buffer.");
        var bytes = SelectionFile.Serialize(selection);
        Expect(selection.ObjectCount == 1 && selection.ScriptCount == (source.IsLegacyProject ? 2 : 1) && status.Text.Contains("3×3", StringComparison.Ordinal), "Copy status/contents do not reflect the actual selection.");
        foreach (var file in source.Files) Expect(file.Data.Span.SequenceEqual(original[file]), "Copy command changed source buffers.");
        workspace.Document = null;
        Expect(ReferenceEquals(workspace.BufferedSelection, selection) && canvas.Selection is null && canvas.CursorTile == new TilePoint(0, 0), "Closing a map destroyed its independent copy buffer or retained a stale cursor.");
        NativeMapDocument Destination()
        {
            if (!source.IsLegacyProject) return NativeMapDocument.Create(5, 5, "CopyDestination");
            var path = Path.Combine(directory, "copy-destination-" + Guid.NewGuid().ToString("N") + ".dkm");
            File.WriteAllBytes(path, TextMapFile.Parse("GUI_GENERATED_MAP\nCopyUI\nPLAYERS 1\nMAP\n5 X 5\nOaOaOaOaOa\nOaOaOaOaOa\nOaOaOaOaOa\nOaOaOaOaOa\nOaOaOaOaOa\nEOF").ToArray());
            return NativeMapDocument.Load(path);
        }
        var destination = Destination();
        workspace.Document = destination; canvas.SetSelection(new(2, 2, 1, 1));
        var before = destination.Files.ToDictionary(file => file, file => file.Data.ToArray()); workspace.Paste();
        Expect(destination.GetTile(3, 3).Terrain == 6 && ScriptGraph.Build(destination).Events.Count == 1,
            "Paste did not use the selection origin and clip at the map edge with the retained object's graph.");
        var creature = destination.Records().Single(record => record.Category == (source.IsLegacyProject ? "Project.MAPTHING" : "GAMEKEEPER"));
        Expect(creature.Read(creature.Fields.Single(field => field.Name == (source.IsLegacyProject ? "x" : "dwKeeperLocationX"))) == "3", "Clipped UI paste placed the creature at another origin.");
        workspace.Undo(); foreach (var file in destination.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "UI clipped paste undo was not byte-exact.");
        workspace.Redo(); workspace.Undo();
        Expect(ReferenceEquals(workspace.BufferedSelection, selection) && SelectionFile.Serialize(selection).AsSpan().SequenceEqual(bytes), "Paste/undo consumed or modified the independent buffer.");
        var direct = source.CopySelection(new(1, 1, 1, 1));
        workspace.PasteLoadedSelection(SelectionFile.Parse(SelectionFile.Serialize(direct)));
        Expect(destination.GetTile(2, 2).Terrain == 6 && ReferenceEquals(workspace.BufferedSelection, selection), "Direct DKZ insertion overwrote the retained buffer."); workspace.Undo();
        var protectedTile = destination.GetTile(4, 4); canvas.SetSelection(new(3, 3, 1, 1)); workspace.Paste();
        Expect(destination.GetTile(3, 3) == new MapTile(1, 2, 1) && destination.GetTile(4, 4) == protectedTile
            && !destination.Records().Any(record => record.Category == (source.IsLegacyProject ? "Project.MAPTHING" : "GAMEKEEPER"))
            && ScriptGraph.Build(destination).Events.Count == 0, "Clipped border paste retained a rejected owner or its graph.");
        workspace.Undo(); foreach (var file in destination.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Border clipping undo changed bytes.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(status.Text.StartsWith(LocalizationService.Get("Editor.CopyBufferInfo").Split('{')[0], StringComparison.Ordinal), "Copy status retained the old language.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        workspace.Document = source; workspace.SelectAll();
        workspace.Cut(); Expect(!source.Records().Any(record => record.Category == (source.IsLegacyProject ? "Project.MAPTHING" : "GAMEKEEPER")), "Cut command did not remove its placed object.");
        workspace.Undo(); foreach (var file in source.Files) Expect(file.Data.Span.SequenceEqual(original[file]), "UI cut undo changed original buffers.");
        var retained = workspace.BufferedSelection;
        root = source.FindScriptScope(owner)!; source.SetField(root.Owner, root.Field, "60000");
        try
        {
            typeof(EditorWorkspace).GetMethod("CaptureSelection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(workspace, [true]);
            throw new InvalidOperationException("A cut with a dangling scenario was accepted.");
        }
        catch (System.Reflection.TargetInvocationException exception) when (exception.InnerException is InvalidDataException) { }
        Expect(ReferenceEquals(workspace.BufferedSelection, retained), "A failed cut discarded the previous independent buffer.");
        source.Undo(); foreach (var file in source.Files) Expect(file.Data.Span.SequenceEqual(original[file]), "Failed cut or repair undo changed source buffers.");
        NativeMapDocument opposite;
        if (source.IsLegacyProject) opposite = NativeMapDocument.Create(5, 5, "Opposite");
        else
        {
            var path = Path.Combine(directory, "opposite-" + Guid.NewGuid().ToString("N") + ".dkm");
            File.WriteAllBytes(path, TextMapFile.Parse("GUI_GENERATED_MAP\nOpposite\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF").ToArray());
            opposite = NativeMapDocument.Load(path);
        }
        workspace.Document = opposite; var oppositeBefore = opposite.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { workspace.PasteLoadedSelection(retained!); throw new InvalidOperationException("Scripted cross-format paste was accepted."); }
        catch (InvalidOperationException exception) when (exception.Message.Contains("same map format", StringComparison.Ordinal)) { }
        foreach (var file in opposite.Files) Expect(file.Data.Span.SequenceEqual(oppositeBefore[file]), "Rejected cross-format paste changed target buffers.");
        var tilesOnly = source.CopySelection(new(0, 0, 1, 1)); workspace.LoadSelectionIntoBuffer(tilesOnly); workspace.SetSelection(new(1, 1, 1, 1)); workspace.Paste();
        Expect(opposite.GetTile(1, 1) == source.GetTile(0, 0) && ReferenceEquals(workspace.BufferedSelection, tilesOnly), "Script-free cross-format paste consumed its source buffer or lost tiles.");
        workspace.Undo();
        var wrong = source.CopySelection(new(0, 0, 1, 1)); workspace.Document = null; workspace.LoadSelectionIntoBuffer(wrong);
        Expect(ReferenceEquals(workspace.BufferedSelection, wrong), "DKZ buffer loading requires an open map.");
        workspace.LoadSelectionIntoBuffer(retained!); workspace.Document = destination;
        Expect(canvas.CursorTile == new TilePoint(0, 0) && canvas.Selection is null && workspace.BufferedSelection is not null, "Opening another map lost the copied area or selection/cursor reset.");
        Console.WriteLine($"PASS WPF persistent copy buffer, clipped/direct DKZ paste and undo {language} {(source.IsLegacyProject ? "project" : "native")}");
    }

    private static void CheckTreeProjection(NativeMapDocument document, string language)
    {
        var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
        var action = LegacyEventSchemas.All.Single(type => type.Name == "ACTION_ALLIES");
        var mapOwner = document.ScriptOwners().Single(owner => owner.Scope == 32); var root = document.EnsureScriptScope(mapOwner);
        var a = ScriptGraph.ID(document.InsertScriptEvent(root.Owner, root.Field, timer));
        ScriptEdge Next(ushort id) => ScriptGraph.Build(document).Events[id].Edges.Single(edge => edge.Field.Name is "NextOr" or "or" or "next");
        ScriptEdge Child(ushort id) => ScriptGraph.Build(document).Events[id].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and");
        var edge = Next(a); var b = ScriptGraph.ID(document.InsertScriptEvent(edge.Owner, edge.Field, timer));
        edge = Child(a); var left = ScriptGraph.ID(document.InsertScriptEvent(edge.Owner, edge.Field, action));
        edge = Child(b); var right = ScriptGraph.ID(document.InsertScriptEvent(edge.Owner, edge.Field, action));
        edge = Next(left); var shared = ScriptGraph.ID(document.InsertScriptEvent(edge.Owner, edge.Field, timer));
        edge = Next(right); document.SetScriptLink(edge.Owner, edge.Field, shared);
        if (!document.IsLegacyProject)
        {
            var record = ScriptGraph.Build(document).Events[right].Record;
            document.SetField(record, record.Fields.Single(field => field.Name == "ChildAnd"), b.ToString());
        }
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var workspace = new EditorWorkspace { Document = document }; Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 2;
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        var tree = Control<TreeView>(workspace, "ScriptTree");
        object[] Children(object node) => ((System.Collections.IEnumerable)node.GetType().GetProperty("Children")!.GetValue(node)!).Cast<object>().ToArray();
        var map = tree.Items.Cast<object>().Single(item => item.GetType().GetProperty("Incoming")?.GetValue(item) is ScriptEdge { Scope: 32 });
        var top = Children(map);
        Expect(top.Select(item => Get<ushort>(item, "EventID")).SequenceEqual(new[] { a, b }), "Actual WPF tree still nests following events under their predecessor.");
        Expect(Children(top[0]).Select(item => Get<ushort>(item, "EventID")).SequenceEqual(new[] { left, shared })
            && Children(top[1]).Select(item => Get<ushort>(item, "EventID")).SequenceEqual(new[] { right, shared }), "Child chains are not siblings under their actual condition.");
        Expect(Children(Children(top[0])[0]).Length == 0 && Children(Children(top[1])[0]).Length == 0,
            "Action leaves acquired next-event children or interpreted reserved bytes as children.");
        Expect(Get<string>(Children(top[1])[1], "Label").Contains(LocalizationService.Get("Editor.ScriptShared"), StringComparison.Ordinal), "Shared continuation is not clearly marked.");
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Rendering the WPF tree changed original buffers.");
        Expect(Select(tree, item => Get<ushort>(item, "EventID") == shared && Get<ushort>(item, "IncomingEventID") == right), "Could not select the second shared continuation context.");
        void SelectedContext() => Expect(Get<ushort>(tree.SelectedItem, "EventID") == shared && Get<ushort>(tree.SelectedItem, "IncomingEventID") == right,
            "Tree refresh jumped from the selected shared connection to another parent.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); SelectedContext();
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); SelectedContext();
        root = document.FindScriptScope(mapOwner)!; document.InsertScriptEvent(root.Owner, root.Field, timer); SelectedContext();
        // In DKM, adding this condition shifts both incoming ACTION offsets. The
        // selected context must use the captured parent ID, not stale record bytes.
        document.Undo(); SelectedContext(); document.Redo(); SelectedContext(); document.Undo(); SelectedContext();
        var renamed = ScriptGraph.Build(document).Events[right].Record;
        document.SetField(renamed, renamed.Fields.Single(field => field.Name is "ID" or "id"), document.IsLegacyProject && renamed.Category=="Project.TRIGGER" ? "5000" : "50000");
        Expect(tree.SelectedItem is null, "An unresolvable shared parent identity silently selected another connection.");
        document.Undo();
        Expect(Select(tree, item => Get<ushort>(item, "EventID") == shared && Get<ushort>(item, "IncomingEventID") == right), "Shared context could not be explicitly selected after identity undo.");
        var player = document.ScriptOwners().First(owner => owner.Scope == 1); var playerRoot = document.EnsureScriptScope(player);
        document.SetScriptLink(playerRoot.Owner, playerRoot.Field, a);
        Expect(Select(tree, item => Get<ushort>(item, "EventID") == a && item.GetType().GetProperty("Incoming")?.GetValue(item) is ScriptEdge { Scope: 1 }), "Could not select the second shared owner context.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(Get<ScriptEdge>(tree.SelectedItem, "Incoming").Scope == 1, "Shared owner selection switched to map scope after translation.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        document.RemoveScriptScope(document.FindScriptScope(mapOwner)!);
        Expect(Get<ushort>(tree.SelectedItem, "EventID") == a && Get<ScriptEdge>(tree.SelectedItem, "Incoming").Scope == 1
            && document.OwnerForScriptScope(Get<ScriptEdge>(tree.SelectedItem, "Incoming"))?.PlayerID == player.PlayerID,
            "Removing an earlier binding lost the selected shared owner's identity after link offsets shifted.");
        document.Undo(); document.Redo();
        Expect(Get<ScriptEdge>(tree.SelectedItem, "Incoming").Scope == 1, "Binding-offset undo/redo switched selected scope.");
        document.Undo();
        Expect(Select(tree, item => Get<ushort>(item, "EventID") == 0 && item.GetType().GetProperty("Incoming")?.GetValue(item) is ScriptEdge { Scope: 1 }), "Could not select the shared owner's scope heading.");
        document.RemoveScriptScope(document.FindScriptScope(mapOwner)!);
        Expect(Get<ushort>(tree.SelectedItem, "EventID") == 0 && Get<ScriptEdge>(tree.SelectedItem, "Incoming").Scope == 1,
            "Scope heading selection did not survive project root record compaction.");
        document.Undo();
        workspace.Document = null; Expect(tree.Items.Count == 0, "Projected tree retained a closed document.");
        Console.WriteLine($"PASS WPF sibling projection and shared selection {language} {(document.IsLegacyProject ? "project" : "native")}");
    }

    private static void CheckBranchControls(NativeMapDocument document, string language)
    {
        var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
        var action = LegacyEventSchemas.All.Single(type => type.Name == "ACTION_ALLIES");
        var owner = document.ScriptOwners().Single(item => item.Scope == 32);
        var root = document.EnsureScriptScope(owner);
        var first = ScriptGraph.ID(document.InsertScriptEvent(root.Owner, root.Field, timer));
        ScriptEdge Next(ushort id) => ScriptGraph.Build(document).Events[id].Edges.Single(edge => edge.Field.Name is "NextOr" or "or" or "next");
        ScriptEdge Child(ushort id) => ScriptGraph.Build(document).Events[id].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and");
        var edge = Next(first); var second = ScriptGraph.ID(document.InsertScriptEvent(edge.Owner, edge.Field, timer));
        edge = Child(first); var child = ScriptGraph.ID(document.InsertScriptEvent(edge.Owner, edge.Field, action));
        var workspace = new EditorWorkspace { Document = document };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 2;
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        var tree = Control<TreeView>(workspace, "ScriptTree"); var panel = Control<StackPanel>(workspace, "ScriptBranchPanel");
        void Pick(ushort id) => Expect(Select(tree, item => Get<ushort>(item, "EventID") == id), "Branch UI could not select its event.");
        void ExactUndo(Dictionary<NativeFile, byte[]> before)
        { document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Branch UI undo was not byte-exact."); }
        Pick(first); var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        Click(panel, "Editor.ScriptMoveDown");
        Expect(document.FindScriptScope(owner)!.Target == second && Next(second).Target == first && Child(first).Target == child, "Move-down button lost children or reordered another branch.");
        Pick(first); Click(panel, "Editor.ScriptMoveUp");
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Move-up button did not restore original bytes/backlinks.");
        Pick(first); Click(panel, "Editor.ScriptIndent");
        Expect(document.FindScriptScope(owner)!.Target == second && Child(second).Target == first && Child(first).Target == child, "Indent button did not move into next condition.");
        Pick(first); Click(panel, "Editor.ScriptOutdent");
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Outdent button did not restore original bytes/backlinks.");
        Pick(first);
        var destinations = Control<ComboBox>(workspace, "ScriptMovePicker");
        destinations.SelectedItem = destinations.Items.Cast<object>().Single(view => Get<ScriptEdge>(view, "Edge") is { } target
            && ScriptGraph.IsEvent(target.Owner) && ScriptGraph.ID(target.Owner) == second && target.Field.Name is "ChildAnd" or "and");
        Click(panel, "Editor.ScriptMove");
        Expect(Child(second).Target == first && Next(first).Target == 0 && Child(first).Target == child, "Destination picker/button did not move selected branch.");
        ExactUndo(before); Pick(first);
        workspace.ScenarioDeletionPresenter=window=>window.Close();
        Click(panel, "Editor.ScriptDeleteBranch");
        Expect(ScriptGraph.Build(document).Events.Count == 3, "An unconfirmed delete button removed events.");
        workspace.ScenarioDeletionPresenter=window=>Expect(window.Accept(),"Scenario deletion confirmation refused Yes.");
        Click(panel, "Editor.ScriptDeleteBranch");
        Expect(ScriptGraph.Build(document).Events.Count == 1 && document.FindScriptScope(owner)!.Target == second, "Confirmed branch-delete button lost continuation or retained deleted children.");
        ExactUndo(before); Pick(first);
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(destinations.Items.Cast<object>().Any(view => Get<string>(view, "Label").Contains(LocalizationService.Get("Editor.ScriptAnd"), StringComparison.Ordinal)), "Destination choices retained old language.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        workspace.Document = null;
        Expect(destinations.Items.Count == 0 && !panel.IsEnabled && workspace.FindName("ScriptDeleteConfirmed") is null, "Closed document retained branch editing or obsolete confirmation.");
        Console.WriteLine($"PASS WPF branch movement and deletion {language} {(document.IsLegacyProject ? "project" : "native")}");
        CheckScenarioDeletionRequests(!document.IsLegacyProject,language);
    }

    private static void CheckScopeControls(NativeMapDocument document, string language)
    {
        foreach (var (project, native) in new[] { ("Creature", "GAMEKEEPER"), ("Door", "GAMEDOOR"), ("Object", "GAMEOBJECT"), ("MagicalObject", "GAMEOBJECT"), ("Area", "GAMEAREA"), ("Band", "GAMEBAND") })
        {
            var record = document.IsLegacyProject ? document.AddEditorLegacyRecord(project, new(1, 1)) : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == native), new(1, 1));
            if (project == "MagicalObject") document.SetField(record, record.Fields.Single(field => field.Name == (document.IsLegacyProject ? "id" : "bObjectID")), ValueCatalogues.ForTemplate("MagicalObject").First().Value);
            if (project == "Band")
            {
                document.SetField(record, record.Fields.Single(field => field.Name == (document.IsLegacyProject ? "id" : "bBandID")), "0");
                document.AddEditorBandMember(record, 6); document.AddEditorBandMember(record, 17);
            }
        }
        if (document.IsLegacyProject) document.AddEditorLegacyRecord("Creature", new(2, 1));
        else document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEKEEPER"), new(2, 1));
        var workspace = new EditorWorkspace { Document = document };
        workspace.ScenarioCreationPresenter = CompleteDefaultCreation;
        workspace.ScenarioPointPresenter = window =>
        {
            window.ShowInTaskbar = false; window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = -10000; window.Top = -10000; window.Show();
            try
            {
                Expect(window.IsLoaded, "Actual scope wizard was not loaded.");
                if (window.SelectedScope != 32) Expect(window.Next(), "Actual scope wizard did not open its owner page.");
                Expect(window.Finish(), "Actual scope wizard did not accept the selected physical owner.");
            }
            finally { window.Close(); }
        };
        workspace.ScenarioDeletionPresenter = window => Expect(window.Accept(),"Point deletion confirmation refused Yes.");
        var host = new Window { Content = workspace, Width = 1200, Height = 850, ShowInTaskbar = false, Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual };
        host.Show();
        try
        {
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 2;
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        var scopes = Control<ComboBox>(workspace, "ScriptScopePicker"); var owners = Control<ComboBox>(workspace, "ScriptOwnerPicker");
        var scopePanel = Control<StackPanel>(workspace, "ScriptScopePanel"); var tree = Control<TreeView>(workspace, "ScriptTree");
        Expect(scopes.Items.Count == 9, "The actual scope picker does not expose all nine scopes.");
        ScriptOwner Pick(uint scope, bool last = false)
        {
            scopes.SelectedValue = (int)scope;
            owners.SelectedIndex = last ? owners.Items.Count - 1 : 0;
            return Get<ScriptOwner>(owners.SelectedItem, "Owner");
        }
        foreach (var scope in new uint[] { 32, 1, 2, 4, 8, 16, 64, 128, 256 })
        {
            var owner = Pick(scope);
            Click(scopePanel, "Editor.ScriptOpenScope");
            var selected = Get<ScriptEdge>(tree.SelectedItem, "Incoming");
            Expect(selected.Scope == scope && document.OwnerForScriptScope(selected) is { } actual && actual.Record.File == owner.Record.File && actual.Record.Offset == owner.Record.Offset
                && actual.MemberIndex == owner.MemberIndex, "Scope creation/opening selected another physical owner.");
            var picker = Control<ComboBox>(workspace, "ScriptTypePicker");
            picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == "TRIGGER_TIMEELAPSED");
            Click(Control<StackPanel>(workspace, "ScriptPanel"), "Editor.ScriptInsert");
            Expect(document.FindScriptScope(owner)!.Target != 0, "Scope UI insertion did not connect to its selected owner.");
        }
        var first = Pick(2); Click(scopePanel, "Editor.ScriptOpenScope");
        var destination = Pick(2, last: true); var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var target = document.FindScriptScope(first)!.Target;
        Click(scopePanel, "Editor.ScriptReassign");
        Expect(document.FindScriptScope(destination)!.Target == target && (document.IsLegacyProject ? document.FindScriptScope(first) is null : document.FindScriptScope(first)!.Target == 0),
            "The reassign button did not move the selected owner binding.");
        document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Owner binding UI undo was not byte-exact."); document.Redo();
        Pick(2, last: true); Click(scopePanel, "Editor.ScriptOpenScope");
        var events = ScriptGraph.Build(document).Events.Count;
        var removed = ScriptGraph.Build(document).Reachable(target).Count;
        Click(scopePanel, "Editor.ScriptRemoveScope");
        Expect(ScriptGraph.Build(document).Events.Count == events - removed && (document.IsLegacyProject ? document.FindScriptScope(destination) is null : document.FindScriptScope(destination)!.Target == 0),
            "Source Delete Point failed to remove the complete branch or retained the binding.");
        document.Undo(); Expect(document.FindScriptScope(destination)!.Target == target, "Removed owner scope was not restored by undo.");
        Pick(16);
        var member = Get<ScriptOwner>(owners.SelectedItem, "Owner");
        var creature = document.IsLegacyProject ? document.BandMembers(member.Record)[member.MemberIndex].CreatureID
            : uint.Parse(member.Record.Read(member.Record.Fields.Single(field => field.Name == "bGlobalCreatureID")));
        var name = ValueCatalogues.ForTemplate("Creature").Single(choice => choice.Value == creature.ToString()).Name(language);
        var search = Control<TextBox>(workspace, "ScriptOwnerSearch"); search.Text = name;
        Expect(owners.Items.Count != 0 && owners.Items.Cast<object>().All(item => Get<string>(item, "Label").Contains(name, StringComparison.Ordinal)), "Named member owner search did not filter actual picker choices.");
        search.Text = "";
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(owners.Items.Cast<object>().All(item => Get<string>(item, "Label").StartsWith(LocalizationService.Get("ScriptScope.16"), StringComparison.Ordinal)), "Owner choices retained the old language.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        workspace.Document = null;
        Expect(owners.Items.Count == 0 && scopes.Items.Count == 0 && tree.Items.Count == 0, "Owner selectors retained a closed document.");
        Console.WriteLine($"PASS WPF owner scopes {language} {(document.IsLegacyProject ? "project" : "native")}");
        }
        finally { host.Close(); }
    }

    private static void CheckLoadedTree(NativeMapDocument document, string language)
    {
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var graph = ScriptGraph.Build(document);
        var workspace = new EditorWorkspace { Document = document };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 2;
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        Expect(Control<TreeView>(workspace, "ScriptTree").Items.Count != 0 || graph.Events.Count == 0 && graph.Roots.Count == 0, "Loaded scenario tree has no components.");
        Expect(Control<TextBlock>(workspace, "ScriptStatus").Text.StartsWith(LocalizationService.Get("Editor.Scripts"), StringComparison.Ordinal), "Loaded tree status has the wrong language.");
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Read-only tree projection mutated loaded map data.");
        workspace.Document = null;
        Console.WriteLine($"PASS WPF loaded script tree {language} {Path.GetFileName(document.MainFile.SourcePath)} events={graph.Events.Count} scopes={graph.Roots.Count}");
    }

    private static void CheckWorkspace(NativeMapDocument document, NativeRecord band, string language)
    {
        document.AddBandMember(band, 6); document.AddBandMember(band, 17);
        var workspace = new EditorWorkspace { Document = document };
        var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); tabs.SelectedIndex = 1;
        workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout();
        var categories = Control<ComboBox>(workspace, "CategoryPicker");
        categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == band.Category);
        var list = Control<ListBox>(workspace, "RecordList");
        list.SelectedItem = list.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == band.Offset && Get<NativeRecord>(item, "Record").File == band.File);
        var panel = Control<StackPanel>(workspace, "BandPanel");
        var slots = Control<ComboBox>(workspace, "BandSlotPicker");
        Expect(panel.Visibility == Visibility.Visible && slots.Items.Count == 17, "Band panel or end-slot bounds are wrong.");
        Expect(Get<string>(slots.Items[0], "Label").StartsWith(language == "ru" ? "Свойства отряда" : "Band properties", StringComparison.Ordinal), "Band labels loaded from the wrong resource language.");
        slots.SelectedIndex = 2; // Member index 1; index 0 shows band properties.
        var grid = Control<DataGrid>(workspace, "PropertyGrid");
        var levelField = document.IsLegacyProject ? "heros[1].level" : "bGlobalLevel";
        var row = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == levelField);
        var rowOwner = Get<NativeRecord>(row, "Record");
        Expect(rowOwner.Offset == (document.IsLegacyProject ? band.Offset : band.Offset + 83), "Property grid edits another hero's byte range.");
        Expect(!grid.Items.Cast<object>().Any(item => Get<NativeField>(item, "Field").Name == "bBandID" || Get<NativeField>(item, "Field").Name == "heros[0].level"), "Member property grid exposes the wrong owner.");
        grid.SelectedItem = row;
        var named = Control<ComboBox>(workspace, "NamedValuePicker");
        named.SelectedItem = named.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == "9");
        Click(Control<StackPanel>(workspace, "NamedValuePanel"), "Editor.ApplyNamedValue");
        Expect(document.BandMembers(band)[0].Level == 1 && document.BandMembers(band)[1].Level == 9, "Named UI edit changed an adjacent member.");
        var types = Control<ComboBox>(workspace, "BandCreaturePicker");
        Expect(types.ItemTemplate is not null && types.Items.Count == 32 && types.Items.Cast<object>().All(item => Get<System.Windows.Media.Imaging.BitmapSource>(item, "Icon").IsFrozen)
            && !types.Items.Cast<object>().Any(item => Get<ValueChoice>(item, "Choice").Value == "34"), "Historical/native band picker does not follow the source's nonelite image catalogue.");
        types.SelectedItem = types.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == "6");
        Control<ComboBox>(workspace, "BandLevelPicker").SelectedValue = 5;
        Control<ComboBox>(workspace, "BandEnemyPicker").SelectedValue = 7;
        Click(panel, "Editor.AddBandMember");
        Expect(document.BandMembers(band)[2] is { Active: true, CreatureID: 6, Level: 5, Enemy: 7 } && slots.SelectedIndex == 3, "UI add-member choices or selection are wrong.");
        Click(panel, "Editor.MemberUp");
        Expect(document.BandMembers(band)[1] is { CreatureID: 6, Level: 5, Enemy: 7 } && slots.SelectedIndex == 2, "UI move-member changed identity/settings or selected another slot.");
        document.Undo(); Expect(document.BandMembers(band)[2].Level == 5, "UI move undo failed.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage();
        Expect(panel.Visibility == Visibility.Visible && slots.SelectedIndex == 2 && grid.Items.Count > 0, "Language switch lost the selected member.");
        Expect(Get<string>(slots.Items[0], "Label").StartsWith(language == "en" ? "Свойства отряда" : "Band properties", StringComparison.Ordinal), "Live switching kept the old-language dictionary.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        if (document.HasLegacyUpgrade)
        {
            var originalCategory = band.Category;
            document.UpgradeLegacyProject();
            Expect(Get<string>(categories.SelectedItem, "ID") == "Project.MTHBAND" && slots.SelectedIndex == 2 && panel.Visibility == Visibility.Visible, "Upgrade lost the selected band/member.");
            row = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == "heros[1].level");
            Expect(Get<NativeRecord>(row, "Record").Category == "Project.MTHBAND", "Upgraded property grid retained an obsolete byte layout.");
            Expect(Get<NativeRecord>(row, "Record").Offset == 12 + LegacyProjectSchemas.Size("MTHBAND"), "Upgrade selected an adjacent band instead of preserving its ordinal.");
            document.Undo();
            Expect(Get<string>(categories.SelectedItem, "ID") == originalCategory && slots.SelectedIndex == 2, "Upgrade undo lost the historical band selection.");
            document.Redo();
            Expect(Get<string>(categories.SelectedItem, "ID") == "Project.MTHBAND" && grid.Items.Count > 0, "Upgrade redo retained an obsolete property grid.");
        }
        workspace.Document = null;
        Expect(panel.Visibility == Visibility.Collapsed && grid.Items.Count == 0, "Old band remains editable after the document closes.");
        Console.WriteLine($"PASS WPF band member controls {language} {band.Category}");
    }

    private static void CheckReferenceControls(NativeMapDocument document, string language)
    {
        var legacy = document.IsLegacyProject;
        NativeRecord Area() => legacy ? document.AddLegacyRecord("Area", new(1, 1)) : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEAREA"), new(1, 1));
        var first = Area(); var target = Area();
        document.SetField(first, EntityReferences.Identity(first)!.Field, "10"); document.SetField(target, EntityReferences.Identity(target)!.Field, "11");
        if (legacy) document.SetField(target, target.Fields.Single(field => field.Name == "name"), "Patrol destination");
        var action = legacy ? document.AddLegacyRecord("Action", new(0, 0)) : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "Action"), new(0, 0));
        document.SetEventType(action, LegacyEventSchemas.All.Single(type => type.Name == "ACTION_MOVECAMERA"));
        action = document.Records().Single(record => record.File == action.File && record.Offset == action.Offset && record.Category == action.Category);
        var parameter = action.Fields.Single(field => field.Name == (legacy ? "param1" : "Param1")); document.SetField(action, parameter, "10");
        var workspace = new EditorWorkspace { Document = document };
        var categories = Control<ComboBox>(workspace, "CategoryPicker");
        categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == action.Category);
        var grid = Control<DataGrid>(workspace, "PropertyGrid");
        var row = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == parameter.Name); grid.SelectedItem = row;
        Expect(Get<string>(row, "Label") == LocalizationService.Get("Editor.Reference.Area"), "Scenario reference label is not semantic/localized.");
        var choices = Control<ComboBox>(workspace, "NamedValuePicker");
        choices.SelectedItem = choices.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == "11");
        Click(Control<StackPanel>(workspace, "NamedValuePanel"), "Editor.ApplyNamedValue");
        Expect(action.Read(parameter) == "11", "Scenario named reference selection set the wrong ID.");
        document.SetField(target, EntityReferences.Identity(target)!.Field, "13");
        Expect(action.Read(parameter) == "13", "Scenario reference did not follow the target's identity edit.");
        Expect(choices.Items.Cast<object>().Any(item => Get<ValueChoice>(item, "Choice").Value == "13") && !choices.Items.Cast<object>().Any(item => Get<ValueChoice>(item, "Choice").Value == "11"), "Reference picker retained stale identities.");
        row = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == parameter.Name);
        Expect(Get<string>(row, "KnownValue").Contains(language == "ru" ? "Зона 13" : "Area 13", StringComparison.Ordinal), "Interpreted reference name uses a stale ID/language.");
        document.Undo();
        Expect(action.Read(parameter) == "11" && choices.Items.Cast<object>().Any(item => Get<ValueChoice>(item, "Choice").Value == "11")
            && !choices.Items.Cast<object>().Any(item => Get<ValueChoice>(item, "Choice").Value == "13"), "Undo left stale reference values/choices in the UI.");
        document.Redo();
        Expect(action.Read(parameter) == "13" && choices.Items.Cast<object>().Any(item => Get<ValueChoice>(item, "Choice").Value == "13"), "Redo left stale reference choices in the UI.");
        var band = document.Records().FirstOrDefault(NativeMapDocument.IsBand) ?? document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0));
        document.SetField(band, EntityReferences.Identity(band)!.Field, "0");
        document.SetEventType(action, LegacyEventSchemas.All.Single(type => type.Name == "ACTION_RELEASEBAND"));
        row = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == parameter.Name); grid.SelectedItem = row;
        var zero = choices.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == "0");
        Expect(Get<ValueChoice>(zero, "Choice").Name(language).StartsWith(language == "ru" ? "Отряд 0" : "Band 0", StringComparison.Ordinal), "Band zero is incorrectly offered as an absent target.");
        choices.SelectedItem = zero;
        Click(Control<StackPanel>(workspace, "NamedValuePanel"), "Editor.ApplyNamedValue");
        Expect(action.Read(parameter) == "0", "Named UI cannot select valid band zero.");
        workspace.Document = null;
        Expect(Control<StackPanel>(workspace, "NamedValuePanel").Visibility == Visibility.Collapsed, "Reference UI remains editable after closing its document.");
        Console.WriteLine($"PASS WPF named scenario references {language} {(legacy ? "project" : "native")}");
    }
}
