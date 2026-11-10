using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App;
using DK2MapEditor.App.Settings;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckActivityStatistics(string directory, string language, bool native)
    {
        var doc = NativeMapDocument.CreateNewMap(new("ActivityUI", "ActivityUI", 24, 24, NativeFormat: native));
        Expect(doc.ReadActivityStatistics() is not null, "Activity timer was not initialized by the real factory.");
        uint ticks = 0;
        var date = new ActivityLocalTime(2026, 10, ushort.MaxValue, 9, 6, 7, 8, 0);
        var invalid = new ActivityLocalTime(65535, 500, 65535, 42, 99, 99, 99, 65535);
        var payload = ActivityStatisticsPayload.New(date, date, invalid) with
        {
            TotalTicks = 999, LMBClicks = uint.MaxValue, RMBClicks = 2, UsageOfUndo = 3,
            UsageOfPaintThings = 4, UsageOfPaintFill = 5, UsageOfPaint = 6, UsageOfPaintRnd = 7, SavedTimes = 8
        };
        var field = typeof(NativeMapDocument).GetField("activityStatistics", BindingFlags.Instance | BindingFlags.NonPublic)!;
        (field.GetValue(doc) as IDisposable)?.Dispose();
        using var session = new ActivityStatisticsSession(payload, () => ticks, () => date);
        field.SetValue(doc, session);
        ticks = 2 * 86400000u + 3 * 3600000u + 4 * 60000u + 5000u;
        var workspace = new EditorWorkspace { Document = doc };
        var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); tabs.SelectedIndex = 8;
        var sections = Control<TabControl>(workspace, "StatisticsTabs"); sections.SelectedIndex = 0;
        var table = Control<DataGrid>(workspace, "StatisticsMapTable");
        void Layout() { workspace.Measure(new Size(1400, 900)); workspace.Arrange(new Rect(0, 0, 1400, 900)); workspace.UpdateLayout(); }
        string Value(string key) => Get<string>(table.Items.Cast<object>().Single(row => Get<string>(row, "Key") == key), "Value");
        string Label(string key) => Get<string>(table.Items.Cast<object>().Single(row => Get<string>(row, "Key") == key), "Label");
        var names = new[] { "ActivityDays", "ActivityHours", "ActivityMinutes", "ActivitySeconds", "LeftMapClicks", "RightMapClicks", "UndoUses",
            "ObjectPaintUses", "FillUses", "PaintUses", "RandomPaintUses", "ProjectSaveCount", "LastProjectSave", "MapCreationTime", "LastCompilationTime" };
        var buffers = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var revision = doc.EditRevision; var dirty = doc.HasChanges; var history = doc.GetHistory();
        var runtime = doc.ReadActivityStatistics()!;
        void Unchanged()
        {
            Expect(buffers.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.EditRevision == revision && doc.HasChanges == dirty
                && doc.GetHistory().Undo.Count == history.Undo.Count && doc.GetHistory().Redo.Count == history.Redo.Count
                && doc.ReadActivityStatistics()!.Encode().AsSpan().SequenceEqual(runtime.Encode()), "Statistics viewing/translation changed bytes, history, invalid dates or counters.");
        }
        Layout();
        Expect(table.IsReadOnly && table.Columns.Count == 2 && Value("ActivityDays") == "2" && Value("ActivityHours") == "3"
            && Value("ActivityMinutes") == "4" && Value("ActivitySeconds") == "5", "Source elapsed day/hour/minute/second decomposition differs.");
        var expected = new[] { "4294967295", "2", "3", "4", "5", "6", "7", "8" };
        for (var index = 0; index < expected.Length; index++) Expect(Value(names[4 + index]) == expected[index], "Unsigned counter binding differs: " + names[4 + index]);
        Expect(Value("LastProjectSave") == "—" && Value("MapCreationTime") == "2026-10-09 06:07:08"
            && Value("LastCompilationTime") == LocalizationService.Get("Editor.Statistics.Unknown"), "Missing/valid/invalid source date presentation differs.");
        var labels = names.ToDictionary(key => key, Label);
        Expect(labels.All(pair => pair.Value == LocalizationService.Get("Editor.Statistics." + pair.Key)), "Activity label is missing or untranslated.");
        void Render(string name)
        {
            var root = Environment.GetEnvironmentVariable("DK2_ACTIVITY_UI_ARTIFACTS"); if (string.IsNullOrWhiteSpace(root)) return;
            Directory.CreateDirectory(root);
            var row = table.Items.Cast<object>().Single(item => Get<string>(item, "Key") == "LastCompilationTime");
            table.ScrollIntoView(row); Layout();
            var bitmap = new RenderTargetBitmap(1400, 900, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(root, $"activity-{language}-{(native ? "native" : "project")}-{name}.png")); encoder.Save(output);
        }
        Render("initial"); Unchanged();
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(names.All(key => Label(key) != labels[key] && Value(key) == (key == "LastCompilationTime"
            ? LocalizationService.Get("Editor.Statistics.Unknown") : key == "MapCreationTime" ? "2026-10-09 06:07:08"
            : key == "LastProjectSave" ? "—" : key is "ActivityDays" or "RightMapClicks" ? "2"
            : key is "ActivityHours" or "UndoUses" ? "3" : key is "ActivityMinutes" or "ObjectPaintUses" ? "4"
            : key is "ActivitySeconds" or "FillUses" ? "5" : key == "PaintUses" ? "6" : key == "RandomPaintUses" ? "7"
            : key == "ProjectSaveCount" ? "8" : "4294967295")), "Live translation changed values or left source labels unchanged.");
        Unchanged(); LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout();
        doc.RecordActivity(ActivityCounterEvent.LeftMapClick); workspace.RefreshActivityStatistics(); Layout();
        Expect(Value("LeftMapClicks") == "0", "Unsigned source counter wrap was displayed as negative or did not refresh.");
        var sourcePath = doc.SaveCopy(Path.Combine(directory, $"activity-save-{language}-{native}"));
        workspace.RefreshActivityStatistics(); Layout();
        Expect(Value("ProjectSaveCount") == (native ? "8" : "9") && Value("LastProjectSave") == (native ? "—" : "2026-10-09 06:07:08"),
            "Actual project/game Save counter/date refresh differs.");
        Render("saved");
        CheckActivityStatisticsMinimum(sourcePath, language, native, names);
        var source = native ? NativeMapDocument.CreateNewMap(new("UnknownStats", "UnknownStats", 24, 24)) : doc;
        var basePath = native ? source.SaveCopy(Path.Combine(directory, $"activity-base-{language}")) : sourcePath;
        var baseSections = LegacySectionFile.Read(basePath).Sections.Where(section => section.Signature != ActivityStatisticsPayload.Signature).ToArray();
        foreach (var variant in new[] { "future", "short" })
        {
            var bytes = LegacySectionFile.Create(baseSections.Append(new(ActivityStatisticsPayload.Signature, variant == "future" ? 2 : 1, new byte[191]))).ToArray();
            var path = Path.Combine(directory, $"activity-{language}-{native}-{variant}.dkm"); File.WriteAllBytes(path, bytes);
            var unknown = NativeMapDocument.Load(path); var before = unknown.Files.ToDictionary(file => file, file => file.Data.ToArray());
            workspace.Document = unknown; tabs.SelectedIndex = 8; workspace.RefreshActivityStatistics(); Layout();
            Expect(names.All(key => Value(key) == LocalizationService.Get("Editor.Statistics.Unknown"))
                && Value("ActivityStatus") == LocalizationService.Get("Editor.Statistics." + (variant == "future" ? "ActivityUnsupportedVersion" : "ActivityTruncatedSection")),
                "Unsupported/truncated runtime statistics were displayed as zero or without a localized reason.");
            Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && !unknown.CanUndo && !unknown.CanRedo,
                "Unknown statistics browsing changed imported data/history.");
        }
        workspace.Document = NativeMapDocument.CreateNewMap(new("ReplacementStats", "ReplacementStats", 24, 24, NativeFormat: native));
        tabs.SelectedIndex = 8; workspace.RefreshActivityStatistics(); Layout();
        Expect(Value("ProjectSaveCount") == "0" && Value("LeftMapClicks") == "0", "Document replacement retained previous runtime counters.");
        workspace.Document = null; Layout(); Expect(table.Items.Count == 0, "Closed document retained statistics rows.");
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} activity statistics rows:15 localized fields, source elapsed/unsigned/date presentation, read-only history, actual Save refresh, future/truncated reasons, minimum shell layout/scroll reachability and document lifetime; physical input acceptance pending");
    }

    private static void CheckActivityStatisticsMinimum(string path, string language, bool native, string[] names)
    {
        var shell = new MainWindow(new AppSettings(), persistSettings: false);
        try
        {
            shell.OpenMapFromPath(path);
            var workspace = (EditorWorkspace)shell.FindName("Editor"); var document = workspace.Document!;
            var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var history = document.GetHistory(); var revision = document.EditRevision;
            Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 8;
            Control<TabControl>(workspace, "StatisticsTabs").SelectedIndex = 0;
            var table = Control<DataGrid>(workspace, "StatisticsMapTable");
            var content = (FrameworkElement)shell.Content;
            void Layout()
            {
                content.Measure(new Size(shell.MinWidth, shell.MinHeight));
                content.Arrange(new Rect(0, 0, shell.MinWidth, shell.MinHeight)); content.UpdateLayout();
            }
            // ScrollIntoView requires a loaded presentation source. Keep the
            // content exactly at980x600 without physical chrome/DPI claims.
            shell.WindowStyle = WindowStyle.None; shell.ResizeMode = ResizeMode.NoResize;
            shell.Width = shell.MinWidth; shell.Height = shell.MinHeight;
            shell.ShowActivated = false; shell.ShowInTaskbar = false;
            shell.Left = -10000; shell.Top = -10000; shell.Show();
            shell.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle); Layout();
            Expect(shell.IsLoaded && table.IsLoaded && Math.Abs(content.ActualWidth - shell.MinWidth) < 1
                && Math.Abs(content.ActualHeight - shell.MinHeight) < 1, "Minimum statistics shell is not loaded at the exact content size.");
            Expect(table.ActualWidth > 0 && table.ActualHeight > 0 && table.Columns.Sum(column => column.ActualWidth) <= table.ActualWidth,
                "Statistics columns overflow minimum main-window layout.");
            foreach (var key in names)
            {
                var item = table.Items.Cast<object>().Single(row => Get<string>(row, "Key") == key);
                table.ScrollIntoView(item);
                shell.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle); Layout();
                var row = table.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                Expect(row is not null && row.ActualHeight > 0, "Activity metric was not reachable by actual minimum-layout scrolling: " + key);
                foreach (var column in table.Columns)
                {
                    var text = column.GetCellContent(item) as TextBlock;
                    Expect(text is not null && text.ActualWidth > 0 && text.ActualHeight > 0 && !string.IsNullOrEmpty(text.Text),
                        "Activity cell was blank/unrealized at minimum layout: " + key);
                    var bounds = text!.TransformToAncestor(table).TransformBounds(new Rect(0, 0, text.ActualWidth, text.ActualHeight));
                    Expect(bounds.Left >= 0 && bounds.Right <= table.ActualWidth + 1 && bounds.Top >= 0 && bounds.Bottom <= table.ActualHeight + 1,
                        "Activity cell is clipped after scrolling at minimum layout: " + key);
                }
                if (key is "LeftMapClicks" or "PaintUses" or "LastCompilationTime"
                    && Environment.GetEnvironmentVariable("DK2_ACTIVITY_UI_ARTIFACTS") is { Length: > 0 } artifacts)
                {
                    Directory.CreateDirectory(artifacts);
                    var bitmap = new RenderTargetBitmap((int)shell.MinWidth, (int)shell.MinHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var output = new FileStream(Path.Combine(artifacts, $"activity-{language}-{(native ? "native" : "project")}-minimum-{key}.png"), FileMode.CreateNew);
                    encoder.Save(output);
                }
            }
            Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && document.EditRevision == revision
                && document.GetHistory().Undo.Count == history.Undo.Count && document.GetHistory().Redo.Count == history.Redo.Count,
                "Minimum statistics browsing changed bytes/revision/history.");
            workspace.Document = null;
        }
        finally { shell.Close(); }
    }
}
