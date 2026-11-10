using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DK2MapEditor.App;

using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Formats;
using Microsoft.Win32;
using IO = System.IO;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.App.Editing;

public partial class MainWindow : Window
{
    private readonly AppSettings settings;
    private readonly bool persistSettings;
    private readonly Dk2FileInspector inspector = new();
    private Dk2FileInspection? inspection;
    private NativeMapDocument? document;
    private IReadOnlyList<MapDiagnostic>? editableDiagnostics;

    public MainWindow(AppSettings settings, bool persistSettings = true)
    {
        this.settings = settings;
        this.persistSettings = persistSettings;
        InitializeComponent();
        Editor.ConfigureGameResources(settings.GameResources);
        settings.HistoryDepth = ConfiguredHistoryDepth;
        RefreshHistoryCommands();
        Editor.ConfigureThumbnailOptions(settings.Thumbnail);
        Editor.ConfigureObjectDisplay(settings.MapObjects);
        Editor.ConfigureOverlays(settings.MapOverlays);
        Editor.ConfirmCopyBufferReplacement = settings.ConfirmCopyBufferReplacement;
        Editor.CopyConfirmationChanged += CopyConfirmationChanged; CopyConfirmationChanged(null, EventArgs.Empty);
        Editor.ConfigureZoom(settings.MapZoom); Editor.ConfigureTerrainVisibility(settings.ShowTerrain);
        LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;
        Closed += MainWindow_Closed;
        Editor.DocumentContextChanged += InvalidateScenarioContext;
        Editor.DocumentContextChanged += CloseVersionsOnContextChange;
        Editor.Unloaded += InvalidateScenarioContext;
        Closing += (_, e) => e.Cancel = !ConfirmDiscard();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        UpdateLanguageChecks();InitializeSaving();
    }

    private void OpenMap_Click(object sender, RoutedEventArgs e)
    {
        var location=Editor.FileDialogs.Location(EditorFileDialogKind.Map,fallbackDirectory:GetInitialDirectory());
        var dialog = new EditorPreviewFileDialog(EditorFileDialogKind.Map)
        {
            Title = LocalizationService.Get("Dialog.Open.Title"),
            Filter = LocalizationService.Get("Dialog.Open.Filter"),
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = location.Directory,
            FileName = location.FileName
        };

        if (dialog.ShowDialog(this) == true)
        {
            OpenMap(dialog.FileName);
        }
    }

    private void OpenMap(string filePath)
    {
        if(saveOwnerClosed)return;
        var context=saveContextRevision;
        if(!ConfirmDiscard() || saveOwnerClosed || context!=saveContextRevision)return;
        NativeMapDocument? next=null;EditorSaveSession? nextSession=null;
        Dk2FileInspection? nextInspection=null;
        FileProgressScope? progress=null;
        try
        {
            progress=BeginFileProgress(saving:false,filePath);
            nextInspection=inspector.Inspect(filePath);
            if(nextInspection.Kind is Dk2FileKind.GameMapDescriptor or Dk2FileKind.GameMapTiles or Dk2FileKind.EditorMap or Dk2FileKind.EditorArea or Dk2FileKind.TextMap)
            {
                next=NativeMapDocument.Load(filePath,progress is null?null:progress.ReportLoad);
                nextSession=PrepareSaveSession(next,filePath);
            }
        }
        catch(Exception exception) when(exception is IO.IOException or IO.InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OverflowException)
        {
            progress?.Dispose();nextSession?.Dispose();LastSaveError=exception;
            var rows=(nextInspection?.Diagnostics??[]).Concat([new MapDiagnostic(DiagnosticSeverity.Error,"Editor.OpenFailed",exception.Message,filePath)]).ToArray();
            ShowDocumentReport(DocumentReportPurpose.Load,rows,false);return;
        }
        if(saveOwnerClosed||context!=saveContextRevision){progress?.Dispose();nextSession?.Dispose();return;}
        try
        {
        DetachHistoryDocument();DetachSaveSession();
        if(document is not null)document.Changed-=Document_Changed;
        document=next;saveSession=nextSession;IsRecoveredDocument=false;inspection=nextInspection;
        if(document is not null)document.Changed+=Document_Changed;
        Editor.Document = document;
        AttachHistoryDocument();
        Editor.Visibility = document is null ? Visibility.Collapsed : Visibility.Visible;
        WelcomePanel.Visibility = document is null ? Visibility.Visible : Visibility.Collapsed;
        settings.LastOpenedFile = filePath;
        if(document is not null)RememberRecent(filePath);
        SaveSettings();
        RenderInspection();
        Document_Changed(this, EventArgs.Empty);
        progress?.Complete();
        }
        finally{progress?.Dispose();}
        if(nextInspection.Diagnostics.Count!=0)ShowDocumentReport(DocumentReportPurpose.Load,nextInspection.Diagnostics,false,next);
    }

    private bool ConfirmDiscard() => document is not { HasChanges: true } || (DiscardChangesPrompt?.Invoke() ?? (DK2MapEditor.App.Editing.OriginalMessageBox.ShowWithIcon(75,this,
        LocalizationService.Get("Editor.Discard"), LocalizationService.Get("App.Title"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes));

    private void Document_Changed(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, document)) { InvalidateScenarioContext();InvalidateSaveContext(); }
        documentReport?.RefreshCurrent();
        if (editableDiagnostics is not null) { editableDiagnostics = null; RenderInspection(); }
        else if (inspection is null && document is not null) RenderInspection();
        RefreshDocumentMetadata();
        UndoMenuItem.IsEnabled = document?.CanUndo == true;
        RedoMenuItem.IsEnabled = document?.CanRedo == true;
        HistoryMenuItem.IsEnabled = document is not null;
        CompileMenuItem.IsEnabled = document?.IsLegacyProject == true;
        RunGameMenuItem.IsEnabled = document is not null && (document.IsLegacyProject || document.MainFile.Signature == 0xDC);
        RefreshSourceManagerCommands();
        MapPropertiesMenuItem.IsEnabled = document is not null;
        UpgradeMenuItem.IsEnabled = document?.HasLegacyUpgrade == true;
        TextExportMenuItem.IsEnabled = document is not null;
        TextExportSupportedMenuItem.IsEnabled = TextExportOriginalMenuItem.IsEnabled = document is not null;
        RefreshSaveCommands();
        Editor.FileDialogs.SetMapTarget(saveSession?.TargetPath);
        var caption=document is null?null:EditorWindowCaption.ReadMapName(document)
            ??IO.Path.GetFileName(saveSession?.TargetPath??document.MainFile.SourcePath);
        if(IsRecoveredDocument&&caption is not null)caption+=" · "+LocalizationService.Get("Save.Recovered");
        Title = LocalizationService.Get("App.Title") + (caption is null?"":" — "+caption) + (document?.HasChanges == true ? " *" : "");
    }
    private void UpgradeProject_Click(object sender, RoutedEventArgs e)
    {
        if (document is not { HasLegacyUpgrade: true } || !Editor.CommitFields()) return;
        if (DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, LocalizationService.Get("Editor.UpgradeConfirm"), LocalizationService.Get("Menu.UpgradeProject"),
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { document.UpgradeLegacyProject(); }
        catch (Exception exception) when (exception is IO.InvalidDataException or InvalidOperationException or ArgumentException or OverflowException)
        { DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, exception.Message, LocalizationService.Get("Menu.UpgradeProject"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private async void Compile_Click(object sender, RoutedEventArgs e)
    {
        await RequestCompileProjectAsync();
    }

    private void TextExport_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || !Editor.CommitFields()) return;
        try
        {
            var projection = PrepareTextExport((MenuItem)sender);
            if (DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, TextExportWarning(projection), LocalizationService.Get("Menu.TextExport"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            var dialog = new OpenFolderDialog { Title = LocalizationService.Get("Editor.ExportFolder"), Multiselect = false };
            if (dialog.ShowDialog(this) != true) return;
            var destination = IO.Path.Combine(dialog.FolderName, "DK2-text-export-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..6]);
            using var progress=BeginFileProgress(saving:true,destination);
            progress?.Report(new(EditorFilePhase.Write,destination));
            var exported = TextMapFile.SaveCopy(destination, document,projection.ScriptMode);
            progress?.Complete();progress?.Dispose();
            DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, LocalizationService.Get("TextMap.Success") + "\n\n" + exported.TextPath + "\n" + exported.EditableCopyPath, LocalizationService.Get("Menu.TextExport"));
        }
        catch (Exception exception) when (exception is IO.IOException or IO.InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OverflowException or FormatException)
        { DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, exception.Message, LocalizationService.Get("Menu.TextExport"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    // Shared non-modal preparation path, also exercised by actual-menu tests.
    private TextMapProjection PrepareTextExport(MenuItem command)
    {
        if (document is null) throw new InvalidOperationException("Open a map before text export.");
        var mode = command == TextExportOriginalMenuItem ? TextMapScriptMode.OriginalNoScenarios
            : command == TextExportSupportedMenuItem ? TextMapScriptMode.SupportedScenarios
            : throw new ArgumentException("Choose an explicit text export command.",nameof(command));
        return TextMapFile.Project(document,mode);
    }
    private static string TextExportWarning(TextMapProjection projection)
        => LocalizationService.Get("TextMap.Confirm") + "\n\n" + string.Join("\n",projection.Limitations.Select(loss =>
            LocalizationService.Get("TextMap.Loss." + loss.Code) + (loss.Code is "AdditionalSettings" or "OriginalNoScenarios" ? "" : ": " + loss.Count)));

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (document is null) return;
        if (!Editor.CommitFields()) return;
        var dialog = new OpenFolderDialog { Title = LocalizationService.Get("Editor.ExportFolder"), Multiselect = false };
        if (dialog.ShowDialog(this) != true) return;
        var output = IO.Path.Combine(dialog.FolderName, "DK2-export-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..6]);
        try
        {
            var exported = document.SaveCopy(output);
            DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, LocalizationService.Get("Editor.ExportSuccess") + "\n" + exported, LocalizationService.Get("Menu.Export"));
        }
        catch (Exception exception) when (exception is IO.IOException or IO.InvalidDataException or UnauthorizedAccessException or ArgumentException)
        { DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, exception.Message, LocalizationService.Get("Menu.Export"), MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { Editor.RefreshActivityStatistics(); }
    }
    private void New_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscard()) return;
        var sourceContext=document;
        var dialog = new NewMapDialog(this, NewMapOptions.ForDocument(document),Editor.RequestPreferences,()=>ReferenceEquals(document,sourceContext));
        if (dialog.ShowDialog() != true) return;
        try
        {
            var next = NativeMapDocument.CreateNewMap(dialog.Options, document);
            AdoptNewMap(next);
        }
        catch (Exception exception) when (exception is ArgumentException or IO.InvalidDataException)
        { DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, exception.Message, LocalizationService.Get("Menu.New"), MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void AdoptNewMap(NativeMapDocument next)
    {
        if (document is not null) document.Changed -= Document_Changed;
        DetachHistoryDocument(); document = next; document.Changed += Document_Changed; AttachHistoryDocument();
        AttachSaveSession();inspection = null; Editor.Document = document; Editor.Visibility = Visibility.Visible; WelcomePanel.Visibility = Visibility.Collapsed;
        RenderInspection(); Document_Changed(this, EventArgs.Empty);
        ValidateDocument(document);
    }
    private void Undo_Click(object sender, RoutedEventArgs e) => Editor.Undo();
    private void Redo_Click(object sender, RoutedEventArgs e) => Editor.Redo();
    private void Cut_Click(object sender, RoutedEventArgs e) => Editor.RequestCopy(cut: true);
    private void Copy_Click(object sender, RoutedEventArgs e) => Editor.RequestCopy();
    private void CopyConfirmation_Click(object sender, RoutedEventArgs e) => Editor.ConfirmCopyBufferReplacement = CopyConfirmationMenuItem.IsChecked;
    private void CopyConfirmationChanged(object? sender, EventArgs args) => CopyConfirmationMenuItem.IsChecked = Editor.ConfirmCopyBufferReplacement;
    private void Paste_Click(object sender, RoutedEventArgs e) => Editor.Paste();
    private void ImportSelection_Click(object sender, RoutedEventArgs e) => Editor.ImportSelection();
    private void ImportIntoSelection_Click(object sender, RoutedEventArgs e) => Editor.ImportSelection(pasteIntoSelection: true);
    private void ExportCopyBuffer_Click(object sender, RoutedEventArgs e) => Editor.ExportCopyBuffer();
    private void ExportSelection_Click(object sender, RoutedEventArgs e) => Editor.ExportSelection();
    private void SelectAll_Click(object sender, RoutedEventArgs e) => Editor.SelectAll();
    private void SelectNone_Click(object sender, RoutedEventArgs e) => Editor.ClearSelection();
    private void ChangeSelection_Click(object sender, RoutedEventArgs e) => Editor.ChangeSelection();
    private void Rotate_Click(object sender, RoutedEventArgs e) => Editor.Rotate();
    private void RotateCounterclockwise_Click(object sender, RoutedEventArgs e) => Editor.RotateCounterclockwise();
    private void CopyRotatedSelection_Click(object sender, RoutedEventArgs e) => Editor.RequestCopy(rotated: true);
    private void MirrorHorizontal_Click(object sender, RoutedEventArgs e) => Editor.MirrorHorizontal();
    private void MirrorVertical_Click(object sender, RoutedEventArgs e) => Editor.MirrorVertical();

    public void OpenMapFromPath(string filePath) => OpenMap(filePath);

    private string? GetInitialDirectory()
    {
        if (string.IsNullOrWhiteSpace(settings.LastOpenedFile))
        {
            return null;
        }

        var directory = IO.Path.GetDirectoryName(settings.LastOpenedFile);
        return IO.Directory.Exists(directory) ? directory : null;
    }

    private void RenderInspection()
    {
        if (inspection is null)
        {
            if (document is not null)
            {
                NoMapText.Visibility = Visibility.Collapsed; MetadataGrid.Visibility = Visibility.Visible;
                FileValue.Text = IO.Path.GetFileName(document.MainFile.SourcePath);
                TypeValue.Text = LocalizationService.Get("Kind." + (document.IsLegacyProject ? Dk2FileKind.EditorMap : Dk2FileKind.GameMapDescriptor));
                VersionValue.Text = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(document.MainFile.Data.Span[4..]).ToString(System.Globalization.CultureInfo.CurrentCulture);
                SizeValue.Text = LocalizationService.Get("Editor.Unsaved"); DimensionsValue.Text = $"{document.Width} × {document.Height}";
                RefreshDocumentMetadata();
                DiagnosticsList.ItemsSource = null; StatusText.SetResourceReference(TextBlock.TextProperty, "Status.Ready");
                return;
            }
            NoMapText.Visibility = Visibility.Visible;
            MetadataGrid.Visibility = Visibility.Collapsed;
            DiagnosticsList.ItemsSource = null;
            StatusText.SetResourceReference(TextBlock.TextProperty, "Status.Ready");
            WorkspaceTitle.SetResourceReference(TextBlock.TextProperty, "Workspace.Welcome");
            return;
        }

        var empty = LocalizationService.Get("Inspector.None");
        NoMapText.Visibility = Visibility.Collapsed;
        MetadataGrid.Visibility = Visibility.Visible;
        FileValue.Text = IO.Path.GetFileName(inspection.FilePath);
        TypeValue.Text = LocalizationService.Get($"Kind.{inspection.Kind}", inspection.Kind.ToString());
        VersionValue.Text = inspection.Version.ToString(System.Globalization.CultureInfo.CurrentCulture);
        SizeValue.Text = $"{inspection.FileSize:N0} B";
        DimensionsValue.Text = inspection.Width.HasValue && inspection.Height.HasValue
            ? $"{inspection.Width} × {inspection.Height}"
            : empty;
        AuthorValue.Text = inspection.Author ?? empty;
        WorkspaceTitle.Text = inspection.MapName ?? IO.Path.GetFileNameWithoutExtension(inspection.FilePath);

        DiagnosticsList.ItemsSource = inspection.Diagnostics.Select(diagnostic => new DiagnosticView(
            diagnostic.Code,
            LocalizationService.Get($"Diagnostic.{diagnostic.Code}", diagnostic.Message),
            diagnostic.FilePath is null ? string.Empty : IO.Path.GetFileName(diagnostic.FilePath),
            GetDiagnosticBrush(diagnostic.Severity))).ToArray();

        StatusText.SetResourceReference(
            TextBlock.TextProperty,
            inspection.IsValid ? "Status.Valid" : "Status.Invalid");
        RefreshDocumentMetadata();
    }
    private void RefreshDocumentMetadata()
    {
        if(document is null)return;
        var current=EditorDocumentPresentation.Read(document,ReferenceEquals(saveSession?.Document,document)?saveSession?.TargetPath:null);
        FileValue.Text=current.FileName;FileValue.ToolTip=current.FilePath;WorkspaceTitle.Text=current.DisplayName;
        AuthorValue.Text=current.Author is {Length:>0}?current.Author:LocalizationService.Get("Inspector.None");
        DimensionsValue.Text=$"{current.Width} × {current.Height}";
    }

    private System.Windows.Media.Brush GetDiagnosticBrush(DiagnosticSeverity severity)
    {
        var key = severity switch
        {
            DiagnosticSeverity.Error => "Brush.Error",
            DiagnosticSeverity.Warning => "Brush.Warning",
            _ => "Brush.Success"
        };
        return (System.Windows.Media.Brush)FindResource(key);
    }

    private async void Validate_Click(object sender, RoutedEventArgs e)
    {
        if (scenarioOwnerClosed || scenarioCommandActive) return;
        if (document is not null)
        {
            await RequestScenarioValidationAsync();
            return;
        }
        if (inspection is not null) { inspection = inspector.Inspect(inspection.FilePath); RenderInspection(); }
        var messageKey = inspection switch
        {
            null => "Dialog.Validate.NoMap",
            { IsValid: true } => "Dialog.Validate.Valid",
            _ => "Dialog.Validate.Invalid"
        };
        var icon = inspection is { IsValid: false } ? MessageBoxImage.Warning : MessageBoxImage.Information;
        DK2MapEditor.App.Editing.OriginalMessageBox.Show(this, LocalizationService.Get(messageKey), LocalizationService.Get("Dialog.Validate.Title"), MessageBoxButton.OK, icon);
    }

    private void English_Click(object sender, RoutedEventArgs e) => ChangeLanguage(LocalizationService.English);

    private void Russian_Click(object sender, RoutedEventArgs e) => ChangeLanguage(LocalizationService.Russian);

    private void ChangeLanguage(string language)
    {
        LocalizationService.SetLanguage(language);
        settings.Language = language;
        SaveSettings();
    }

    private void LocalizationService_LanguageChanged(object? sender, EventArgs e)
    {
        var previousDiagnostics = editableDiagnostics;
        UpdateLanguageChecks();
        RenderInspection();
        Editor.RefreshLanguage();
        Document_Changed(this, EventArgs.Empty);
        if (previousDiagnostics is not null) { editableDiagnostics = previousDiagnostics; RenderEditableDiagnostics(); }
    }

    /// <summary>Run full read-only validation without explicit cleanup consent,
    /// displaying results without opening a message box or changing the document.</summary>
    public IReadOnlyList<MapDiagnostic> ValidateDocument(NativeMapDocument target)
    {
        editableDiagnostics = NativeMapValidator.Validate(target, includeGameplay: true);
        RenderEditableDiagnostics();
        return editableDiagnostics;
    }

    private void RenderEditableDiagnostics()
    {
        if (editableDiagnostics is null) return;
        DiagnosticsList.ItemsSource = editableDiagnostics.Select(diagnostic => new DiagnosticView(diagnostic.Code,
            LocalizationService.Get("Diagnostic." + diagnostic.Code, diagnostic.Message) + "\n" + diagnostic.Message,
            diagnostic.FilePath is null ? "" : IO.Path.GetFileName(diagnostic.FilePath), GetDiagnosticBrush(diagnostic.Severity))).ToArray();
        StatusText.SetResourceReference(TextBlock.TextProperty, editableDiagnostics.Any(item => item.Severity == DiagnosticSeverity.Error) ? "Status.Invalid" : "Status.Valid");
    }

    private void UpdateLanguageChecks()
    {
        EnglishMenuItem.IsChecked = LocalizationService.CurrentLanguage == LocalizationService.English;
        RussianMenuItem.IsChecked = LocalizationService.CurrentLanguage == LocalizationService.Russian;
    }

    private void ToolboxVisibility_Click(object sender, RoutedEventArgs e)
    {
        ToolboxPanel.Visibility = ToolboxMenuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        ToolboxColumn.Width = ToolboxMenuItem.IsChecked ? new GridLength(220) : new GridLength(0);
    }

    private void InspectorVisibility_Click(object sender, RoutedEventArgs e)
    {
        InspectorPanel.Visibility = InspectorMenuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        InspectorColumn.Width = InspectorMenuItem.IsChecked ? new GridLength(320) : new GridLength(0);
    }

    private void About_Click(object sender, RoutedEventArgs e) =>
        DK2MapEditor.App.Editing.OriginalMessageBox.Show(
            this,
            LocalizationService.Get("Dialog.About.Text"),
            LocalizationService.Get("Dialog.About.Title"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if(RequestSaveShortcut(e.Key,Keyboard.Modifiers)){e.Handled=true;return;}
        if(e.Key==Key.E&&Keyboard.Modifiers==(ModifierKeys.Control|ModifierKeys.Shift))
        {RunGame_Click(sender,e);e.Handled=true;return;}
        if (e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase or PasswordBox || Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase or PasswordBox) return;
        if(IsActive&&IsEnabled&&RequestSourceKeyboardShortcut(e.Key,Keyboard.Modifiers)){e.Handled=true;return;}
        if(e.Key==Key.Delete && IsActive && IsEnabled && Editor.RequestUnderCursorRemovalShortcut(e.Key,Keyboard.Modifiers)){e.Handled=true;return;}
        if (e.Key == Key.Escape) { if (!Editor.CancelRegionEditing()) Editor.ClearSelection(); e.Handled = true; return; }
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.Z: Editor.Undo(); break;
                case Key.Y: Editor.Redo(); break;
                case Key.C: Editor.RequestCopy(); break;
                case Key.X: Editor.RequestCopy(cut: true); break;
                case Key.V: Editor.Paste(); break;
                case Key.A: Editor.SelectAll(); break;
                case Key.O: OpenMap_Click(sender, e); break;
                case Key.N: New_Click(sender, e); break;
                default: return;
            }
            e.Handled = true;
            return;
        }
        if (e.Key == Key.O && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            OpenMap_Click(sender, e);
            e.Handled = true;
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        CloseHelpWindows();Editor.DocumentContextChanged-=CloseVersionsOnContextChange;
        fileProgress?.Dispose();CloseSaving();documentReport?.Cancel();scenarioOwnerClosed = true; InvalidateScenarioContext();CloseCompilerTask();
        Editor.DocumentContextChanged -= InvalidateScenarioContext; Editor.Unloaded -= InvalidateScenarioContext;
        historyOwnerClosed = true; DetachHistoryDocument(); historySettingsWindow?.Close(); historySettingsWindow = null;
        historyMemoryWindow?.Dispose();historyMemoryPolicy?.Dispose();
        LocalizationService.LanguageChanged -= LocalizationService_LanguageChanged;
        Editor.CopyConfirmationChanged -= CopyConfirmationChanged;
        Editor.EndCopyCommandLifetime();
        Editor.EndScenarioNamesLifetime();
        Editor.EndLevelOptionsLifetime();
        Editor.EndSpeechTextLifetime();
        Editor.EndMapPropertiesLifetime();
        SaveSettings();
    }

    private void SaveSettings()
    {
        settings.Thumbnail = Editor.ThumbnailOptions;
        settings.GameResources = Editor.GameResources;
        settings.MapObjects = Editor.ObjectDisplayOptions;
        settings.MapOverlays = Editor.OverlayOptions;
        settings.ConfirmCopyBufferReplacement = Editor.ConfiguredCopyBufferReplacementConfirmation;
        settings.MapZoom = Editor.ZoomOptions; settings.ShowTerrain = Editor.ShowTerrain;
        if (persistSettings) SettingsStore.Save(settings);
    }

    private sealed record DiagnosticView(
        string Code,
        string Message,
        string FileName,
        System.Windows.Media.Brush Color);
}
