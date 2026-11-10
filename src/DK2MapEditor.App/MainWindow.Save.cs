using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace DK2MapEditor.App;

public sealed record EditorSaveLocationRequest(string? CurrentPath,string SuggestedName,EditorSaveFormat? CurrentFormat);
public sealed record EditorSaveNotice(string Message,string Title,MessageBoxImage Image);

public partial class MainWindow
{
    private EditorSaveSession? saveSession;
    private EditorRecoveryLifetime? recoveryLifetime;
    private readonly DispatcherTimer saveTimer=new(DispatcherPriority.Background) { Interval=TimeSpan.FromSeconds(1) };
    private SaveConfirmationWindow? saveConfirmation;
    private AutoSaveSettingsWindow? autoSaveSettings;
    private bool saveOwnerClosed,saveCommandActive;
    private long saveContextRevision;
    private string? recoveryRoot;
    private RecoverySelectionWindow? recoverySelection;
    private readonly List<(string Root,EditorRecoveryCandidate Candidate)> pendingRecoveryDismissals=[];
    public Func<bool>? DiscardChangesPrompt { get; set; }
    public Func<EditorSaveLocationRequest,string?>? SaveLocationPrompt { get; set; }
    public Func<EditorSavePlan,bool>? SaveConfirmationPrompt { get; set; }
    public Func<IReadOnlyList<EditorRecoveryCandidate>,string?>? RecoveryPrompt { get; set; }
    public Action<EditorSaveNotice>? SaveNoticePresenter { get; set; }
    public Func<string,Stream>? SaveOutputFactory { get; set; }
    public Func<string,Stream>? SourceArchiveOutputFactory { get; set; }
    public EditorSaveSession? CurrentSaveSession=>saveSession;
    public string? NormalSavePath=>saveSession?.TargetPath;
    public string? RecoveryPath=>recoveryLifetime?.RecoveryPath;
    public Exception? LastSaveError { get; private set; }
    public bool IsRecoveredDocument { get; private set; }

    private void InitializeSaving()
    {
        RefreshSaveCommands(); RefreshRecentFiles();
        saveTimer.Tick+=(_,_)=>RequestAutoSaveTick();
        Editor.DocumentContextChanged+=InvalidateSaveContext;
        Loaded+=(_,_)=>
        {
            if(!persistSettings || saveOwnerClosed || recoveryLifetime is not null)return;
            try
            {
                ConfigureRecoveryDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DK2MapEditor","Recovery"));
                Dispatcher.BeginInvoke(()=>{if(!saveOwnerClosed && document is null)RequestRecovery(automatic:true);},DispatcherPriority.ContextIdle);
            }
            catch(Exception exception) when(exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
            { LastSaveError=exception;PresentSaveNotice("Save.RecoveryUnavailable",MessageBoxImage.Warning); }
        };
    }
    public void ConfigureRecoveryDirectory(string directory,bool startTimer=true)
    {
        if(saveOwnerClosed || saveCommandActive)throw new InvalidOperationException("The save owner is unavailable.");
        var next=new EditorRecoveryLifetime(directory);
        saveTimer.Stop();recoveryLifetime?.Dispose();recoveryLifetime=next;recoveryRoot=next.RootDirectory;
        if(startTimer)saveTimer.Start();RefreshSaveCommands();
    }
    private void InvalidateSaveContext(object? sender=null,EventArgs? args=null)
    { saveContextRevision++;saveConfirmation?.Close();saveConfirmation=null;recoverySelection?.Close();recoverySelection=null; }
    private void DetachSaveSession()
    { InvalidateSaveContext();saveSession?.Dispose();saveSession=null; }
    private void AttachSaveSession(string? loadedPath=null,bool recovered=false)
    {
        DetachSaveSession();IsRecoveredDocument=recovered;
        if(document is not null)
        {
            saveSession=PrepareSaveSession(document,loadedPath,recovered);
        }
        RefreshSaveCommands();
    }
    private EditorSaveSession PrepareSaveSession(NativeMapDocument target,string? loadedPath=null,bool recovered=false)
    {
        var ordinary=loadedPath is not null && (Path.GetExtension(loadedPath).Equals(".map",StringComparison.OrdinalIgnoreCase)
            || target.IsLegacyProject && target.MainFile.Signature==LegacySectionFile.MapFileSignature);
        var session=new EditorSaveSession(target,ordinary?loadedPath:null,recovered);
        session.AutoSave.IntervalMinutes=settings.AutoSaveMinutes;return session;
    }
    public bool RequestSaveShortcut(System.Windows.Input.Key key,System.Windows.Input.ModifierKeys modifiers)
    {
        if(key!=System.Windows.Input.Key.S || modifiers is not (System.Windows.Input.ModifierKeys.Control
            or (System.Windows.Input.ModifierKeys.Control|System.Windows.Input.ModifierKeys.Shift)))return false;
        RequestSave(saveAs:modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift));return true;
    }
    private bool SaveTargetCurrent(EditorSaveSession session,NativeMapDocument target,long context)
        => !saveOwnerClosed && ReferenceEquals(saveSession,session) && ReferenceEquals(document,target)
            && ReferenceEquals(Editor.Document,target) && context==saveContextRevision;
    private void RefreshSaveCommands()
    {
        var available=!saveOwnerClosed && !saveCommandActive && document is not null;
        SaveMenuItem.IsEnabled=SaveButton.IsEnabled=available && document!.EditorChangeCount!=0 && !document.HasActivePenStroke;
        SaveAsMenuItem.IsEnabled=available && !document!.HasActivePenStroke;
        RecoverMenuItem.IsEnabled=!saveOwnerClosed && !saveCommandActive;
    }
    private void Save_Click(object sender,RoutedEventArgs args)=>RequestSave();
    private void SaveAs_Click(object sender,RoutedEventArgs args)=>RequestSave(saveAs:true);
    private string? ChooseSaveLocation(EditorSaveLocationRequest request)
    {
        var location=Editor.FileDialogs.Location(EditorFileDialogKind.Map,request.SuggestedName,GetInitialDirectory());
        var dialog=new EditorPreviewFileDialog(EditorFileDialogKind.Map,save:true) { Title=LocalizationService.Get("Save.Dialog.Title"),Filter=LocalizationService.Get("Save.Dialog.Filter"),
            DefaultExt=request.CurrentFormat==EditorSaveFormat.Map?".map":".dkm",AddExtension=true,OverwritePrompt=true,
            FileName=location.FileName,InitialDirectory=location.Directory,
            FilterIndex=request.CurrentFormat==EditorSaveFormat.Map?2:1 };
        return dialog.ShowDialog(this)==true?dialog.FileName:null;
    }
    public SaveConfirmationWindow CreateSaveConfirmationWindow(EditorSavePlan plan)
        =>CreateSaveConfirmationWindow(plan,SaveCompatibilityRows(plan,document??throw new InvalidOperationException("Open an editable map first.")));
    private SaveConfirmationWindow CreateSaveConfirmationWindow(EditorSavePlan plan,IReadOnlyList<Core.Diagnostics.MapDiagnostic> compatibility)
    {
        if(saveSession is null || document is null)throw new InvalidOperationException("Open an editable map first.");
        var session=saveSession;var target=document;var context=saveContextRevision;
        return new(plan,()=>SaveTargetCurrent(session,target,context)&&session.IsCurrent(plan),compatibility);
    }
    private bool ShowSaveConfirmation(EditorSavePlan plan)
    {
        var window=CreateSaveConfirmationWindow(plan,LastSaveCompatibilityReport);saveConfirmation=window;window.Owner=this;
        try { window.ShowDialog();return window.Accepted; }
        finally { if(ReferenceEquals(saveConfirmation,window))saveConfirmation=null; }
    }
    public bool RequestSave(bool saveAs=false)
    {
        if(saveOwnerClosed || saveCommandActive || document is null || document.HasActivePenStroke || !Editor.CommitFields())return false;
        var target=document;
        if(!ReferenceEquals(Editor.Document,target))return false;
        if(saveSession is null || !ReferenceEquals(saveSession.Document,target))AttachSaveSession();
        var session=saveSession!;var context=saveContextRevision;saveCommandActive=true;LastSaveError=null;RefreshSaveCommands();
        try
        {
            var path=session.TargetPath;
            if(saveAs || path is null)
            {
                path=(SaveLocationPrompt??ChooseSaveLocation)(new(session.TargetPath,Path.GetFileNameWithoutExtension(target.MainFile.SourcePath)+".dkm",session.TargetFormat));
                if(string.IsNullOrWhiteSpace(path)||!SaveTargetCurrent(session,target,context))return false;
            }
            var plan=session.PrepareSaveAs(path);LastSaveCompatibilityReport=SaveCompatibilityRows(plan,target);
            if(plan.HasBlockingIssues || plan.NeedsLossConsent || plan.RetainsNativeSource || LastSaveCompatibilityReport.Count!=0)
            {
                var accepted=(SaveConfirmationPrompt??ShowSaveConfirmation)(plan);
                if(!accepted || plan.HasBlockingIssues || !SaveTargetCurrent(session,target,context)||!session.IsCurrent(plan))return false;
            }
            if(!SaveTargetCurrent(session,target,context)||!session.IsCurrent(plan))return false;
            using var progress=BeginFileProgress(saving:true,plan.Path);
            if(!session.Save(plan,acceptLosses:true,progress?.Output(SaveOutputFactory)??SaveOutputFactory,
                progress?.Output(SourceArchiveOutputFactory)??SourceArchiveOutputFactory))return false;
            var saved=session.LastSave!;
            progress?.Report(new(EditorFilePhase.Validate,saved.Path));
            if(!ReferenceEquals(saved.Document,target))
            {
                target.Changed-=Document_Changed;DetachHistoryDocument();document=saved.Document;document.Changed+=Document_Changed;
                Editor.Document=document;AttachHistoryDocument();
            }
            IsRecoveredDocument=false;inspection=inspector.Inspect(saved.Path);editableDiagnostics=null;
            settings.LastOpenedFile=saved.Path;RememberRecent(saved.Path);SaveSettings();RenderInspection();Document_Changed(this,EventArgs.Empty);
            progress?.Complete();
            return true;
        }
        catch(Exception exception) when(exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OverflowException or FormatException)
        { LastSaveError=exception;PresentSaveNotice(exception is InvalidOperationException?"Save.ContextExpired":"Save.Failed",MessageBoxImage.Error);return false; }
        finally { saveCommandActive=false;RefreshSaveCommands();Editor.RefreshActivityStatistics(); }
    }
    public bool RequestAutoSaveTick(bool locked=false)
    {
        if(saveOwnerClosed || document is null || recoveryLifetime is null || saveSession is null || !ReferenceEquals(saveSession.Document,document))return false;
        try
        {
            var saved=saveSession.TickAutoSave(recoveryLifetime.RecoveryPath,
                locked||saveCommandActive||scenarioCommandActive||fileProgress is not null||startingFileProgress||!IsEnabled,SaveOutputFactory,SourceArchiveOutputFactory);
            if(saved)Editor.RefreshActivityStatistics();return saved;
        }
        catch(Exception exception) when(exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OverflowException)
        { LastSaveError=exception;StatusText.SetResourceReference(TextBlock.TextProperty,"Save.AutoSaveFailed");return false; }
    }
    public AutoSaveSettingsWindow CreateAutoSaveSettingsWindow()
        =>new(settings.AutoSaveMinutes,minutes=>
        {
            if(saveOwnerClosed || saveCommandActive)throw new InvalidOperationException("The settings owner is unavailable.");
            settings.AutoSaveMinutes=minutes;if(saveSession is not null)saveSession.AutoSave.IntervalMinutes=minutes;SaveSettings();
        });
    private void AutoSaveSettings_Click(object sender,RoutedEventArgs args)
    {
        if(saveOwnerClosed || saveCommandActive)return;var window=CreateAutoSaveSettingsWindow();autoSaveSettings=window;window.Owner=this;
        try { window.ShowDialog(); } finally { if(ReferenceEquals(autoSaveSettings,window))autoSaveSettings=null; }
    }
    public RecoverySelectionWindow CreateRecoverySelectionWindow(IReadOnlyList<EditorRecoveryCandidate> candidates)=>new(candidates);
    private string? ChooseRecovery(IReadOnlyList<EditorRecoveryCandidate> candidates)
    {
        var window=CreateRecoverySelectionWindow(candidates);recoverySelection=window;window.Owner=this;
        try { window.ShowDialog();return window.SelectedPath; }
        finally { if(ReferenceEquals(recoverySelection,window))recoverySelection=null; }
    }
    private void Recover_Click(object sender,RoutedEventArgs args)=>RequestRecovery();
    public bool RequestRecovery(bool automatic=false)
    {
        if(saveOwnerClosed || saveCommandActive || recoveryRoot is null)return false;
        var root=recoveryRoot;var context=saveContextRevision;
        try
        {
            var candidates=EditorRecoveryLifetime.Find(root,includeCompleted:!automatic);
            if(candidates.Count==0){if(!automatic)PresentSaveNotice("Save.NoRecovery",MessageBoxImage.Information);return false;}
            var path=(RecoveryPrompt??ChooseRecovery)(candidates);
            if(saveOwnerClosed || context!=saveContextRevision || recoveryRoot!=root)return false;
            if(path is null)
            {
                if(automatic)QueueRecoveryDismissals(root,candidates);
                return false;
            }
            if(!candidates.Any(item=>item.Path.Equals(path,StringComparison.OrdinalIgnoreCase)))return false;
            if(!OpenRecoveryFromPath(path))return false;
            QueueRecoveryDismissals(root,automatic?candidates:candidates.Where(item=>item.Path.Equals(path,StringComparison.OrdinalIgnoreCase)));
            return true;
        }
        catch(Exception exception) when(exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OverflowException)
        { LastSaveError=exception;PresentSaveNotice("Save.RecoveryFailed",MessageBoxImage.Error);return false; }
    }
    private void QueueRecoveryDismissals(string root,IEnumerable<EditorRecoveryCandidate> candidates)
    {
        foreach(var candidate in candidates)
            if(!pendingRecoveryDismissals.Any(item=>item.Root==root&&item.Candidate.Path==candidate.Path))
                pendingRecoveryDismissals.Add((root,candidate));
    }
    public bool OpenRecoveryFromPath(string path)
    {
        if(saveOwnerClosed || saveCommandActive)return false;
        var context=saveContextRevision;
        if(!ConfirmDiscard() || saveOwnerClosed || context!=saveContextRevision)return false;
        try
        {
            using var progress=BeginFileProgress(saving:false,path);
            var next=NativeMapDocument.Load(path,progress is null?null:progress.ReportLoad);var session=new EditorSaveSession(next,path,recovered:true);
            if(!next.IsLegacyProject || next.MainFile.Signature!=LegacySectionFile.MapFileSignature){session.Dispose();throw new InvalidDataException("Recovery must be DKM.");}
            if(saveOwnerClosed||context!=saveContextRevision){session.Dispose();return false;}
            if(document is not null)document.Changed-=Document_Changed;DetachHistoryDocument();DetachSaveSession();
            document=next;saveSession=session;saveSession.AutoSave.IntervalMinutes=settings.AutoSaveMinutes;IsRecoveredDocument=true;
            document.Changed+=Document_Changed;Editor.Document=document;AttachHistoryDocument();inspection=null;editableDiagnostics=null;
            Editor.Visibility=Visibility.Visible;WelcomePanel.Visibility=Visibility.Collapsed;RenderInspection();Document_Changed(this,EventArgs.Empty);
            progress?.Complete();
            // Recovery intentionally keeps normal target/recents unset. A later
            // explicit Save As publishes retained source companions with the map.
            return true;
        }
        catch(Exception exception) when(exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OverflowException)
        { LastSaveError=exception;PresentSaveNotice("Save.RecoveryFailed",MessageBoxImage.Error);return false; }
    }
    private void RememberRecent(string path)
    {
        var full=Path.GetFullPath(path);settings.RecentFiles??=[];
        settings.RecentFiles.RemoveAll(item=>string.IsNullOrWhiteSpace(item)||item.Equals(full,StringComparison.OrdinalIgnoreCase));settings.RecentFiles.Insert(0,full);
        if(settings.RecentFiles.Count>10)settings.RecentFiles.RemoveRange(10,settings.RecentFiles.Count-10);RefreshRecentFiles();
    }
    private void RefreshRecentFiles()
    {
        RecentFilesMenuItem.Items.Clear();
        foreach(var path in (settings.RecentFiles??[]).Where(item=>!string.IsNullOrWhiteSpace(item)).Take(10).ToArray())
        {
            // Config.RecentPaths starts false in the original editor. Keep the
            // full path as command data/tooltip, not as the default menu label.
            var name=Path.GetFileName(path);
            var command=new MenuItem { Header=new TextBlock { Text=string.IsNullOrEmpty(name)?path:name },ToolTip=path };
            command.Click+=(_,_)=>OpenMap(path);RecentFilesMenuItem.Items.Add(command);
        }
        if(RecentFilesMenuItem.Items.Count==0)
        {
            var empty=new MenuItem {IsEnabled=false};empty.SetResourceReference(HeaderedItemsControl.HeaderProperty,"Save.Recent.Empty");
            RecentFilesMenuItem.Items.Add(empty);
        }
        RecentFilesMenuItem.Items.Add(new Separator());
        var obsolete=new MenuItem();obsolete.SetResourceReference(HeaderedItemsControl.HeaderProperty,"Save.Recent.RemoveObsolete");
        obsolete.Click+=(_,_)=>RemoveObsoleteRecentFiles();RecentFilesMenuItem.Items.Add(obsolete);
        var clear=new MenuItem();clear.SetResourceReference(HeaderedItemsControl.HeaderProperty,"Save.Recent.Clear");
        clear.Click+=(_,_)=>ClearRecentFiles();RecentFilesMenuItem.Items.Add(clear);
        RecentFilesMenuItem.IsEnabled=true;
    }
    public void ClearRecentFiles()
    {
        if(saveOwnerClosed || saveCommandActive)return;
        settings.RecentFiles=[];RefreshRecentFiles();SaveSettings();
        PresentSaveNotice("Save.Recent.Cleared",MessageBoxImage.Information);
    }
    public int RemoveObsoleteRecentFiles()
    {
        if(saveOwnerClosed || saveCommandActive)return 0;
        var removed=settings.RecentFiles?.RemoveAll(path=>!RecentFileAvailable(path))??0;
        RefreshRecentFiles();SaveSettings();
        PresentSaveNotice(removed==0?"Save.Recent.NoObsolete":"Save.Recent.Removed",MessageBoxImage.Information);
        return removed;
    }
    private static bool RecentFileAvailable(string? path)
    {
        if(string.IsNullOrWhiteSpace(path))return false;
        // Match Config_RemoveObsolete: probe metadata with desired access 0,
        // share READ|WRITE, OPEN_EXISTING and FILE_FLAG_SEQUENTIAL_SCAN. A read
        // stream would wrongly reject files that deny content access. Never
        // delete the file; removal affects only the recent-history entry.
        using var handle=OpenRecentFile(path,0,3,IntPtr.Zero,3,0x08000000,IntPtr.Zero);
        return !handle.IsInvalid;
    }
    [DllImport("kernel32.dll",EntryPoint="CreateFileW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern SafeFileHandle OpenRecentFile(string fileName,uint desiredAccess,uint shareMode,
        IntPtr securityAttributes,uint creationDisposition,uint flagsAndAttributes,IntPtr templateFile);
    private void PresentSaveNotice(string key,MessageBoxImage image)
    {
        var notice=new EditorSaveNotice(LocalizationService.Get(key),LocalizationService.Get("Save.Dialog.Title"),image);
        if(SaveNoticePresenter is { } presenter)presenter(notice);else DK2MapEditor.App.Editing.OriginalMessageBox.Show(this,notice.Message,notice.Title,MessageBoxButton.OK,notice.Image);
    }
    private void CloseSaving()
    {
        saveOwnerClosed=true;saveTimer.Stop();Editor.DocumentContextChanged-=InvalidateSaveContext;
        autoSaveSettings?.Close();autoSaveSettings=null;DetachSaveSession();
        try { recoveryLifetime?.Dispose(); }
        catch(Exception exception) when(exception is IOException or InvalidDataException or UnauthorizedAccessException){LastSaveError=exception;}
        recoveryLifetime=null;
        // A normal close acknowledges offers from abandoned sessions without
        // deleting their maps or companions. Another crash keeps those offers.
        foreach(var item in pendingRecoveryDismissals)
        {
            try { EditorRecoveryLifetime.Dismiss(item.Root,item.Candidate); }
            catch(Exception exception) when(exception is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException)
            { LastSaveError=exception; }
        }
        pendingRecoveryDismissals.Clear();
    }
}
