using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckCopyConfirmation(bool native, string language, string directory)
    {
        CheckEditorSelectionCallers(native,language);
        var doc = NativeMapDocument.CreateNewMap(new("CopyConsent", "CopyConsent", 20, 16, NativeFormat: native));
        doc.Paint(MapDrawing.Rectangle(new(1, 1, 18, 14), true), new(8, 3, 1)); doc.Paint([new(6, 4)], new(6, 2, 1));
        if (native) doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), new(2, 2)); else doc.AddLegacyRecord("Creature", new(2, 2));
        var owner = doc.ScriptOwners().Single(item => item.Scope == 2); var root = doc.EnsureScriptScope(owner);
        doc.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        var settings = new AppSettings(); var shell = new MainWindow(settings, persistSettings: false); var workspace = (EditorWorkspace)shell.FindName("Editor"); workspace.Document = doc;
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var copy = (MenuItem)shell.FindName("CopyMenuItem"); var cut = (MenuItem)shell.FindName("CutMenuItem"); var rotated = (MenuItem)shell.FindName("CopyRotatedMenuItem"); var preference = (MenuItem)shell.FindName("CopyConfirmationMenuItem");
        var calls = 0; workspace.CopyBufferPrompt = _ => { calls++; throw new InvalidOperationException("Empty buffer unexpectedly requested replacement."); };
        workspace.SetSelection(new(1, 1, 4, 3)); copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        var initial = workspace.BufferedSelection!; var initialBytes = SelectionFile.Serialize(initial);
        Expect(calls == 0 && initial.ObjectCount == 1 && initial.ScriptCount == (native ? 1 : 2) && preference.IsChecked, "Actual initial Copy/default/counts not source-correct.");
        workspace.SetSelection(new(6, 4, 3, 2)); var selection = canvas.Selection;
        var original = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        void ExactMap() => Expect(original.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history, "Copy consent changed map/history.");
        void ExactBuffer() => Expect(ReferenceEquals(initial, workspace.BufferedSelection) && SelectionFile.Serialize(workspace.BufferedSelection!).AsSpan().SequenceEqual(initialBytes), "Declined/stale request replaced or damaged old buffer.");
        workspace.CopyBufferPrompt = request =>
        { calls++; Expect(request.OldWidth == 4 && request.OldHeight == 3 && request.OldObjects == 1 && request.OldScripts == initial.ScriptCount, "Question lacks actual old-buffer summary."); return new(false, true); };
        foreach (var item in new[] { copy, cut, rotated }) { item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); ExactMap(); ExactBuffer(); }
        Expect(calls == 3 && workspace.ConfirmCopyBufferReplacement && canvas.Selection == selection, "Keep incorrectly persisted suppression or altered selection.");
        var requestModel = new CopyBufferRequest(CopyBufferOperation.Cut, 4, 3, 1, initial.ScriptCount, 3, 2);
        var dialog = workspace.CreateCopyBufferConfirmation(requestModel); ((CheckBox)dialog.FindName("CopyBufferSuppress")).IsChecked = true;
        Expect(((Button)dialog.FindName("CopyBufferReplace")).IsDefault && ((Button)dialog.FindName("CopyBufferKeep")).IsCancel, "Source affirmative default/Escape keep missing.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(((CheckBox)dialog.FindName("CopyBufferSuppress")).IsChecked == true && ((TextBlock)dialog.FindName("CopyBufferSummary")).Text.Contains("4 × 3", StringComparison.Ordinal), "Dialog translation lost suppression draft/summary.");
        LocalizationService.SetLanguage(language);
        var content = (FrameworkElement)dialog.Content;
        content.Measure(new Size(540, double.PositiveInfinity));
        var contentHeight = (int)Math.Ceiling(content.DesiredSize.Height);
        content.Arrange(new Rect(0, 0, 540, contentHeight)); content.UpdateLayout();
        var lastButton = (Button)dialog.FindName("CopyBufferKeep");
        var buttonOrigin = lastButton.TransformToAncestor(content).Transform(new Point());
        Expect(buttonOrigin.Y + lastButton.ActualHeight <= content.ActualHeight && buttonOrigin.X + lastButton.ActualWidth <= content.ActualWidth
            && ((TextBlock)dialog.FindName("CopyBufferSummary")).Foreground == Application.Current.FindResource("Brush.Text"), "Confirmation buttons clipped or summary unreadable.");
        if (Environment.GetEnvironmentVariable("DK2_COPY_CONFIRMATION_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts);
            // VisualBrush maps the content's local bounds explicitly. Rendering
            // an already-parented Window child directly retains its ancestor
            // offset and would crop the rightmost Russian button in this PNG.
            var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen())
            {
                drawing.DrawRectangle((Brush)Application.Current.FindResource("Brush.Background"), null, new Rect(0, 0, 540, contentHeight));
                drawing.DrawRectangle(new VisualBrush(content) { Stretch = Stretch.Fill }, null, new Rect(16, 16, content.ActualWidth, content.ActualHeight));
            }
            var bitmap = new RenderTargetBitmap(540, contentHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new FileStream(Path.Combine(artifacts, $"copy-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        ((Button)dialog.FindName("CopyBufferKeep")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(dialog.Decision == new CopyBufferDecision(false) && !dialog.Respond(true), "Keep/closed dialog changed authority.");
        workspace.CopyBufferPrompt = _ => new(true); copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        var candidate = workspace.BufferedSelection!; Expect(!ReferenceEquals(initial, candidate) && candidate.Tiles.Width == 3 && candidate.Tiles.Height == 2, "Actual approved Copy unavailable."); ExactMap();
        var expectedCut = SelectionFile.Serialize(doc.CopySelection(selection!.Value, protectBoundary: true));
        cut.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); Expect(doc.GetHistory().Undo.Count == history + 1 && SelectionFile.Serialize(workspace.BufferedSelection!).AsSpan().SequenceEqual(expectedCut), "Approved Cut did not preserve pre-cut buffer/one undo.");
        doc.Undo(); ExactMap(); workspace.LoadSelectionIntoBuffer(initial);
        workspace.CopyBufferPrompt = _ => { workspace.SetSelection(new(12, 10, 1, 1)); return new(true, true); };
        Expect(!workspace.RequestCopy(cut: true) && workspace.ConfirmCopyBufferReplacement, "Stale selection/suppression applied."); ExactMap(); ExactBuffer(); workspace.SetSelection(selection.Value);
        workspace.CopyBufferPrompt = _ => { doc.Paint([new(16, 12)], new(4, 2, 1)); doc.Undo(); return new(true, true); };
        Expect(!workspace.RequestCopy(cut: true), "Edit+Undo reused stale consent."); ExactMap(); ExactBuffer();
        var initialTile = initial.Tiles.Tiles[0];
        workspace.CopyBufferPrompt = _ => { ((MapTile[])initial.Tiles.Tiles)[0] = new(4, 2, 1); return new(true, true); };
        Expect(!workspace.RequestCopy(cut: true) && workspace.ConfirmCopyBufferReplacement, "Mutated old buffer reused replacement consent.");
        ((MapTile[])initial.Tiles.Tiles)[0] = initialTile; ExactMap(); ExactBuffer();
        workspace.CopyBufferPrompt = _ => { workspace.LoadSelectionIntoBuffer(candidate); return new(true, true); };
        Expect(!workspace.RequestCopy() && ReferenceEquals(candidate, workspace.BufferedSelection) && workspace.ConfirmCopyBufferReplacement, "Reentrant replacement overwrote newer buffer or persisted stale suppression."); workspace.LoadSelectionIntoBuffer(initial);
        workspace.CopyBufferPrompt = _ => { workspace.Document = null; return new(true, true); };
        Expect(!workspace.RequestCopy(cut: true), "Closed-map request applied."); ExactMap(); ExactBuffer(); workspace.Document = doc; workspace.SetSelection(selection.Value);
        workspace.CopyBufferPrompt = _ => new(false); Expect(!workspace.RequestLoadSelectionIntoBuffer(candidate), "DKZ declined request replaced old buffer."); ExactMap(); ExactBuffer();
        workspace.CopyBufferPrompt = _ => { var tiles = (MapTile[])candidate.Tiles.Tiles; tiles[0] = new(4, 2, 1); return new(true, true); };
        var oldCandidateTile = candidate.Tiles.Tiles[0]; Expect(!workspace.RequestLoadSelectionIntoBuffer(candidate), "Changed incoming bytes reused consent."); ((MapTile[])candidate.Tiles.Tiles)[0] = oldCandidateTile; ExactMap(); ExactBuffer();
        var file = Path.Combine(directory, $"copy-consent-{language}-{native}.dkz"); SelectionFile.SaveNew(file, candidate);
        workspace.CopyBufferPrompt = request => { Expect(request.Operation == CopyBufferOperation.Import && request.NewWidth == 3 && request.NewHeight == 2, "Parsed DKZ replacement question wrong."); return new(false, true); };
        Expect(!workspace.RequestSelectionFile(file) && workspace.ConfirmCopyBufferReplacement, "Declined parsed DKZ replaced buffer or suppressed later questions."); ExactMap(); ExactBuffer();
        workspace.CopyBufferPrompt = _ => new(true);
        Expect(workspace.RequestSelectionFile(file) && SelectionFile.Serialize(workspace.BufferedSelection!).AsSpan().SequenceEqual(SelectionFile.Serialize(candidate)), "Accepted parsed DKZ not loaded exactly."); ExactMap(); workspace.LoadSelectionIntoBuffer(initial);
        workspace.CopyBufferPrompt = _ => throw new InvalidOperationException("Direct insertion/malformed parse requested buffer replacement.");
        workspace.SetSelection(new(8, 8, 3, 2)); Expect(workspace.RequestSelectionFile(file, pasteIntoSelection: true) && ReferenceEquals(initial, workspace.BufferedSelection), "Direct DKZ insertion changed buffer or asked confirmation."); doc.Undo(); ExactMap();
        var badFile = Path.Combine(directory, $"copy-consent-bad-{language}-{native}.dkz"); File.WriteAllBytes(badFile, [1, 2, 3]);
        var rejected = false; try { workspace.RequestSelectionFile(badFile); } catch (InvalidDataException) { rejected = true; }
        Expect(rejected, "Malformed DKZ was accepted."); ExactMap(); ExactBuffer();
        workspace.SetSelection(selection.Value); workspace.CopyBufferPrompt = request =>
        {
            var accepted = workspace.CreateCopyBufferConfirmation(request);
            ((CheckBox)accepted.FindName("CopyBufferSuppress")).IsChecked = true;
            ((Button)accepted.FindName("CopyBufferReplace")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(accepted.Decision == new CopyBufferDecision(true, true) && !accepted.Respond(false), "Actual Yes/remember button decision wrong.");
            return accepted.Decision;
        };
        copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); Expect(!workspace.ConfirmCopyBufferReplacement && !preference.IsChecked, "Affirmative remembered setting did not reach actual menu.");
        Expect(workspace.ConfiguredCopyBufferReplacementConfirmation, "Remembered reply overwrote the persistent menu preference.");
        typeof(MainWindow).GetMethod("SaveSettings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(shell, null);
        Expect(settings.ConfirmCopyBufferReplacement, "Saving settings persisted a session-only reply.");
        workspace.CopyBufferPrompt = _ => throw new InvalidOperationException("Suppressed source preference still asked."); Expect(workspace.RequestCopy(), "Suppressed replacement unavailable."); ExactMap();
        preference.IsChecked = true; preference.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); Expect(workspace.ConfirmCopyBufferReplacement, "Preference cannot be reenabled.");
        workspace.CopyBufferPrompt = _ => new(false); copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        shell.Close(); Expect(settings.ConfirmCopyBufferReplacement, "Actual shell failed preference capture.");
        var restored = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(new AppSettings { ConfirmCopyBufferReplacement = false }))!;
        var closing = new MainWindow(restored, persistSettings: false); var editor = (EditorWorkspace)closing.FindName("Editor");
        Expect(!editor.ConfirmCopyBufferReplacement && !((MenuItem)closing.FindName("CopyConfirmationMenuItem")).IsChecked, "JSON/shell preference restore failed.");
        editor.Document = doc; editor.SetSelection(selection.Value); editor.LoadSelectionIntoBuffer(initial); editor.ConfirmCopyBufferReplacement = true;
        editor.CopyBufferPrompt = _ => { closing.Close(); return new(true, true); };
        Expect(!editor.RequestCopy(cut: true) && !editor.RequestCopy() && !editor.RequestLoadSelectionIntoBuffer(candidate)
            && !editor.RequestSelectionFile(file) && !editor.RequestSelectionFile(file, pasteIntoSelection: true), "Closed shell retained command authority."); ExactMap();
        Console.WriteLine($"PASS WPF source copy-buffer consent, actual Copy/Cut/rotated menus, DKZ, stale/closed guards, session reply and persistent menu preference {language} {(native ? "native" : "project")}");
    }
}
