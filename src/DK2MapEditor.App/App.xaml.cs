using System.Configuration;
using System.Data;
using System.Windows;

using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;

namespace DK2MapEditor.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private bool handlingHistoryMemory;
    private readonly bool startEditorOnStartup;
    public App():this(true) { }
    // Isolated WPF checks load the exact App resources without user settings/startup.
    public App(bool startEditorOnStartup)=>this.startEditorOnStartup=startEditorOnStartup;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Implicit Window styles do not match derived Window types in WPF.
        // Apply the common theme when a derived editor dialog has no own style.
        EventManager.RegisterClassHandler(typeof(Window),FrameworkElement.LoadedEvent,new RoutedEventHandler((sender,_)=>
        {
            if(sender is Window dialog && dialog.Style is null)
                dialog.SetResourceReference(FrameworkElement.StyleProperty,typeof(Window));
        }));
        if(!startEditorOnStartup)return;
        var settings = SettingsStore.Load();
        var (requestedLanguage, startupFile) = ParseArguments(e.Args);
        LocalizationService.SetLanguage(requestedLanguage ?? settings.Language ?? LocalizationService.GetDefaultLanguage());
        DispatcherUnhandledException+=HistoryMemoryFailure;

        var window = new MainWindow(settings);
        MainWindow = window;
        window.Show();
        if (startupFile is not null)
        {
            window.OpenMapFromPath(startupFile);
        }
        var pipeOptions=e.Args.Where(arg=>arg.StartsWith("--mcp-pipe=",StringComparison.Ordinal)).ToArray();
        var rootOptions=e.Args.Where(arg=>arg.StartsWith("--mcp-root=",StringComparison.Ordinal)).ToArray();
        if(pipeOptions.Length!=0||rootOptions.Length!=0)
        {
            try
            {
                if(pipeOptions.Length!=1||rootOptions.Length!=1)
                    throw new ArgumentException("Provide exactly one --mcp-pipe=<name> and --mcp-root=<absolute workspace>.");
                window.StartMcp(pipeOptions[0]["--mcp-pipe=".Length..],rootOptions[0]["--mcp-root=".Length..]);
            }
            catch(Exception error) when(error is System.IO.IOException or UnauthorizedAccessException or ArgumentException
                or DK2MapEditor.Core.Automation.AutomationException)
            {
                DK2MapEditor.App.Editing.OriginalMessageBox.Show(window,LocalizationService.Get("Mcp.StartFailed")+"\n"+error.Message,
                    LocalizationService.Get("App.Title"),MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }
    }

    private void HistoryMemoryFailure(object sender,System.Windows.Threading.DispatcherUnhandledExceptionEventArgs args)
    {
        if(handlingHistoryMemory || args.Exception is not DK2MapEditor.Core.Editing.EditorHistoryMemoryException failure || !failure.IsSafeToNotify)return;
        if(MainWindow is not MainWindow root)return;
        handlingHistoryMemory=true;
        try
        {
            var owner=Windows.OfType<Window>().FirstOrDefault(window=>window.IsActive)??root;
            args.Handled=root.PresentHistoryMemoryFailure(failure,owner);
        }
        finally{handlingHistoryMemory=false;}
    }

    private static (string? Language, string? FilePath) ParseArguments(IReadOnlyList<string> arguments)
    {
        string? language = null;
        string? filePath = null;

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (argument.Equals("--lang", StringComparison.OrdinalIgnoreCase) && index + 1 < arguments.Count)
            {
                language = arguments[++index];
            }
            else if (argument.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
            {
                language = argument["--lang=".Length..];
            }
            else if (!argument.StartsWith("--", StringComparison.Ordinal) && filePath is null)
            {
                filePath = argument;
            }
        }

        return (language, filePath);
    }
}

