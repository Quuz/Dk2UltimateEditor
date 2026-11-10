using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckEditorDrawingHistory(string directory, string language, bool native)
    {
        var created = NativeMapDocument.CreateNewMap(new("MouseHistory", "MouseHistory", 24, 24, NativeFormat: native));
        var path = created.SaveCopy(Path.Combine(directory, $"mouse-history-{language}-{native}"), temporary: true);
        var shell = new MainWindow(new AppSettings(), persistSettings: false)
        {
            WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, Width = 980, Height = 600,
            ShowActivated = false, ShowInTaskbar = false, Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual
        };
        HistoryWindow? historyWindow = null;
        NativeMapDocument? document = null;
        try
        {
            shell.OpenMapFromPath(path); shell.Show();
            shell.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
            var workspace = (EditorWorkspace)shell.FindName("Editor"); document = workspace.Document!;
            var canvas = Control<MapCanvas>(workspace, "Canvas");
            // This owned offscreen shell invokes the real pointer entry methods
            // with explicit coordinates. CaptureMouse synchronizes the physical
            // desktop pointer; its unrelated routed move must not draw a second
            // segment. Physical input acceptance remains a separate requirement.
            Expect(shell.Left == -10000 && shell.Top == -10000, "Synthetic pointer fixture must remain offscreen.");
            canvas.PreviewMouseMove += (_, args) => args.Handled = true;
            var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            bool Same() => before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value));
            bool Star() => shell.Title.EndsWith(" *", StringComparison.Ordinal);
            bool Invoke(string method, params object[] values) => (bool)typeof(MapCanvas).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(canvas, values)!;
            Point Pointer(int x, int y) => canvas.CellBounds(new(x, y, 1, 1)).TopLeft + new Vector(1, 1);
            void Move(int x, int y) => typeof(MapCanvas).GetMethod("MoveMapPointer", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(canvas, [Pointer(x, y)]);
            void Menu(string name) => ((MenuItem)shell.FindName(name)).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            void Summary(uint changes)
            {
                var snapshot = document.GetHistory();
                var text = ((TextBlock)historyWindow!.FindName("SummaryText")).Text;
                Expect(text == string.Format(LocalizationService.Get("History.Summary"), snapshot.Undo.Count, snapshot.Redo.Count,
                    changes, snapshot.SnapshotBytes, snapshot.Depth), "Actual history summary confused original change increments and Undo steps.");
            }
            Expect(shell.IsLoaded && canvas.IsLoaded && Same() && !document.HasChanges && !Star() && document.EditorChangeCount == 0,
                "Actual loaded shell must begin clean before no-op mouse cases.");
            canvas.Tool = DrawingTool.Line; canvas.PaintTile = document.GetTile(3, 3);
            Expect(Invoke("BeginLeftMapPointer", Pointer(3, 3)) && !Star() && document.EditorChangeCount == 0 && document.GetHistory().Undo.Count == 0,
                "Geometry preview allocated release history or incremented changes early.");
            Expect(Invoke("CompleteLeftMapPointer", Pointer(4, 3), false) && Same() && Star() && document.HasChanges
                && document.EditorChangeCount == 1 && document.GetHistory().Undo.Count == 1
                && ((MenuItem)shell.FindName("UndoMenuItem")).IsEnabled, "Actual equal LINE release lost original Undo/dirty/count state.");
            Menu("UndoMenuItem"); Expect(Same() && !Star() && !document.HasChanges && document.EditorChangeCount == 0, "Actual equal-paint Undo did not restore clean title/state.");
            Menu("RedoMenuItem"); Expect(Same() && Star() && document.EditorChangeCount == 1, "Actual equal-paint Redo did not restore title/state.");
            canvas.DrawGesture(new(3, 3), new(7, 4), shift: true, randomSample: _ => 1);
            Expect(Same() && document.GetHistory().Undo.Count == 2 && document.EditorChangeCount == 2
                && document.ReadActivityStatistics()!.UsageOfPaintRnd == 1, "Deterministic random no-hit lost preallocated Undo/change/use.");
            Menu("UndoMenuItem"); Menu("UndoMenuItem"); Expect(!Star() && Same() && document.EditorChangeCount == 0, "Empty operation replay lost baseline.");
            canvas.Tool = DrawingTool.Fill;
            var fills = document.ReadActivityStatistics()!.UsageOfPaintFill;
            Expect(Invoke("BeginLeftMapPointer", Pointer(0, 3)) && Same() && !Star() && document.EditorChangeCount == 0
                && document.GetHistory().Undo.Count == 1 && document.ReadActivityStatistics()!.UsageOfPaintFill == fills,
                "Actual protected fill must retain Undo without dirty/change/fill-use increment.");
            Menu("UndoMenuItem");
            Expect(Invoke("BeginLeftMapPointer", Pointer(3, 3)) && Same() && Star() && document.EditorChangeCount == 1
                && document.GetHistory().Undo.Count == 1 && document.ReadActivityStatistics()!.UsageOfPaintFill == fills + 1,
                "Actual equal successful fill must increment original state despite unchanged bytes.");
            Menu("UndoMenuItem");
            canvas.PaintTile = new(14, 3, 1);
            var left = document.ReadActivityStatistics()!.LMBClicks;
            Expect(!Invoke("BeginLeftMapPointer", Pointer(0, 0)) && Same() && !Star() && document.EditorChangeCount == 0
                && document.ReadActivityStatistics()!.LMBClicks == left && document.GetHistory().Undo.Count == 0,
                "Rejected fixed footprint fabricated click/Undo/change state.");
            canvas.PaintTile = new(4, 2, 1); canvas.Tool = DrawingTool.Brush;
            var originAccepted=Invoke("BeginLeftMapPointer", Pointer(0, 0));
            Console.WriteLine($"OBSERVE origin accepted={originAccepted} active={document.HasActivePenStroke} star={Star()} count={document.EditorChangeCount} undo={document.GetHistory().Undo.Count} tool={canvas.Tool} tile={canvas.PaintTile} footprint={canvas.PaintFootprint} capture={canvas.IsMouseCaptured}");
            Expect(originAccepted && document.HasActivePenStroke && !Star() && document.EditorChangeCount == 0,
                "Actual pen origin sentinel did not preserve zero dirty/change state.");
            Invoke("CompleteLeftMapPointer", Pointer(0, 0), false);
            Expect(Same() && document.CanUndo && document.GetHistory().Undo.Count == 1 && document.EditorChangeCount == 0 && !Star(),
                "Origin-only mouse gesture must retain Undo with no LINE change.");
            Menu("UndoMenuItem");
            Expect(Invoke("BeginLeftMapPointer", Pointer(0, 3)) && Same() && Star() && document.EditorChangeCount == 1 && !document.CanUndo,
                "Protected nonorigin LINE did not publish source dirty/count state immediately.");
            Move(0, 3); Expect(document.EditorChangeCount == 1, "Stationary live mouse incremented MapChanges.");
            Move(0, 4); Move(0, 3);
            Expect(Same() && document.EditorChangeCount == 3 && document.ReadActivityStatistics()!.UsageOfPaint == 4,
                "Actual three protected LINE calls did not count independently of bytes and earlier geometry.");
            Invoke("CompleteLeftMapPointer", Pointer(0, 3), false);
            Expect(Same() && document.GetHistory().Undo.Count == 1 && document.GetHistory().ChangeCount == 1 && document.EditorChangeCount == 3,
                "Three original mouse LINE changes became three modern Undo steps.");
            historyWindow = shell.CreateHistoryWindow()!; Summary(3);
            var oldLanguage = language; LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Summary(3);
            Expect(Same() && document.EditorChangeCount == 3 && Star(), "Live history translation changed source state.");
            LocalizationService.SetLanguage(oldLanguage);
            if (Environment.GetEnvironmentVariable("DK2_EDITOR_HISTORY_UI_ARTIFACTS") is { Length: > 0 } artifacts)
            {
                Directory.CreateDirectory(artifacts);
                historyWindow.WindowStyle = WindowStyle.None; historyWindow.ResizeMode = ResizeMode.NoResize;
                historyWindow.Width = 740; historyWindow.Height = 580; historyWindow.Left = -10000; historyWindow.Top = -10000;
                historyWindow.ShowActivated = false; historyWindow.ShowInTaskbar = false;
                historyWindow.WindowStartupLocation = WindowStartupLocation.Manual; historyWindow.Show();
                historyWindow.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                var content = (FrameworkElement)historyWindow.Content;
                var contentWidth = 740 - content.Margin.Left - content.Margin.Right;
                var contentHeight = 580 - content.Margin.Top - content.Margin.Bottom;
                Expect(historyWindow.IsLoaded && Math.Abs(content.ActualWidth - contentWidth) < 1 && Math.Abs(content.ActualHeight - contentHeight) < 1,
                    "History observation must use actual loaded content size.");
                var bitmap = new RenderTargetBitmap(740, 580, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = new FileStream(Path.Combine(artifacts, $"source-history-{language}-{(native ? "native" : "project")}.png"), FileMode.CreateNew);
                encoder.Save(output);
            }
            historyWindow.Close(); historyWindow = null;
            Menu("UndoMenuItem");
            Expect(Same() && document.EditorChangeCount == 0 && !document.HasChanges && !Star(), "Final actual Undo did not restore clean source state.");
            Console.WriteLine($"PASS {language} {(native ? "native" : "project")} actual loaded mouse equal/random/fill/protected/fixed/origin history, immediate source dirty title, independent LINE/Undo counts, menus, translation and exact all-file replay");
        }
        finally
        {
            historyWindow?.Close();
            // Roll back through real Undo before own-shell cleanup; never dismiss a native discard dialog.
            ((EditorWorkspace)shell.FindName("Editor")).Document = null;
            if (document is not null) while (document.CanUndo) document.Undo();
            shell.Close();
        }
    }
}
