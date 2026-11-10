using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioEditorSave(bool native, string language, string directory)
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioCreationDefaults.json")!;
        using var fixture = JsonDocument.Parse(stream);
        var clear = fixture.RootElement.GetProperty("CreationAndSaveFlow").GetProperty("Trigger").GetProperty("ZeroOnSaveTypes").EnumerateArray().Select(item => item.GetString()!).ToHashSet();
        using var scalarStream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioScalarPages.json")!;
        using var scalar = JsonDocument.Parse(scalarStream);
        var repeatTypes = scalar.RootElement.GetProperty("RepeatOnlyTypes").EnumerateArray().Select(item => item.GetString()!).ToArray();
        Expect(clear.Count == 20 && repeatTypes.Length == 26 && clear.IsSubsetOf(repeatTypes), "Independent repeat/Save scope differs.");
        var doc = NativeMapDocument.CreateNewMap(new("SaveUI", "SaveUI", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
        var ids = new Dictionary<string, ushort>();
        foreach (var name in repeatTypes)
        {
            var record = doc.AddScenarioEvent(0xD5, new(0, 0), LegacyEventSchemas.All.Single(item => item.Name == name)); ids[name] = ScriptGraph.ID(record);
            Expect(MemoryMarshal.TryGetArray(record.File.Data, out var bytes), "Imported fixture buffer missing.");
            for (var index = 0; index < (native ? 8 : 32); index++) bytes.Array![record.Offset + (native ? 8 : 16) + index] = (byte)(0x91 ^ index);
            bytes.Array![record.Offset + (native ? 23 : 55)] = 255;
        }
        var workspace = new EditorWorkspace { Document = doc };
        var clipboard = doc.CopySelection(new(0, 0, 1, 1)); workspace.LoadSelectionIntoBuffer(clipboard);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); var records = Control<ListBox>(workspace, "RecordList");
        NativeRecord Select(string name)
        {
            var record = ScriptGraph.Build(doc).Events[ids[name]].Record;
            categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            records.SelectedItem = records.Items.Cast<object>().Single(item => ScriptGraph.ID(Get<NativeRecord>(item, "Record")) == ids[name]);
            return record;
        }
        Dictionary<NativeFile, byte[]> Snapshot() => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected) => Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)) && ReferenceEquals(workspace.BufferedSelection, clipboard), "Source Save changed unrelated bytes or copy buffer.");
        var normalized = 0; var retained = 0;
        foreach (var name in repeatTypes)
        {
            var record = Select(name); var baseline = Snapshot(); var history = doc.GetHistory().Undo.Count;
            ScenarioParameterWindow? presented = null; workspace.ScenarioParameterPresenter = window => presented = window;
            var button = Control<Button>(workspace, "ScenarioParameterButton");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(presented is not null, "Actual parameter command did not present a repeat window.");
            var window = presented!;
            void Layout() { var content = (FrameworkElement)window.Content; content.Measure(new(540, 420)); content.Arrange(new(0, 0, 540, 420)); content.UpdateLayout(); }
            Layout(); Exact(baseline); Expect(!window.HasPendingInputs && doc.GetHistory().Undo.Count == history, "Opening normalized hidden parameters or created history.");
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Layout(); Exact(baseline); LocalizationService.SetLanguage(language);
            window.ResetDraft(); Exact(baseline); window.Close(); Exact(baseline); Expect(!window.ApplyDraft(), "Closed repeat window performed source Save.");
            window = workspace.CreateScenarioParameterWindow()!; Layout(); Exact(baseline);
            var expected = baseline.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
            if (clear.Contains(name)) Array.Clear(expected[record.File], record.Offset + (native ? 8 : 16), native ? 8 : 32);
            if (name == "TRIGGER_APPEAR") Render(window, "imported");
            var apply = (Button)window.FindName("ScenarioPageApply"); apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(expected);
            if (clear.Contains(name))
            {
                normalized++; Expect(doc.GetHistory().Undo.Count == history + 1, "Explicit source cleanup was not one live edit.");
                var revision = doc.EditRevision; var acceptedHistory = doc.GetHistory(); Expect(window.ApplyDraft(), "Already-saved acceptance was rejected."); Exact(expected); var afterAccepted = doc.GetHistory(); Expect(doc.EditRevision == revision + 1 && afterAccepted.ChangeCount == acceptedHistory.ChangeCount + 1 && afterAccepted.EditorChangeCount == unchecked(acceptedHistory.EditorChangeCount + 1) && afterAccepted.Undo.SequenceEqual(acceptedHistory.Undo) && afterAccepted.Redo.SequenceEqual(acceptedHistory.Redo), "Already-zero source Save did not count once while retaining history.");
                if (name == "TRIGGER_APPEAR") Render(window, "saved");
                doc.Undo(); Exact(baseline); doc.Redo(); Exact(expected); doc.Undo(); Exact(baseline); window.ResetDraft();
            }
            else { retained++; Expect(doc.GetHistory().Undo.Count == history, "Unlisted repeat condition lost opaque parameters."); }
            var mode = (ComboBox)window.FindName("ScenarioRepeatMode"); var count = (TextBox)window.FindName("ScenarioRepeatCount");
            mode.SelectedItem = mode.Items.Cast<object>().Single(item => Get<object>(item, "Mode").ToString() == "Count"); count.Text = "254";
            expected[record.File][record.Offset + (native ? 23 : 55)] = 254;
            apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(expected);
            Expect(doc.GetHistory().Undo.Count == history + 1, "Repeat and source cleanup were not one edit."); doc.Undo(); Exact(baseline); window.ResetDraft();
            Expect(MemoryMarshal.TryGetArray(record.File.Data, out var raw), "Raw guard buffer missing."); raw.Array![record.Offset + (native ? 8 : 16)] ^= 1;
            var mutated = Snapshot(); Expect(!window.ApplyDraft(), "Raw mutation accepted stale normalization."); Exact(mutated);
            raw.Array[record.Offset + (native ? 8 : 16)] ^= 1; Exact(baseline); window.Close();
        }
        Expect(normalized == 20 && retained == 6, "All repeat Save branches were not exercised.");
        var selected = Select("TRIGGER_APPEAR"); var beforeGuards = Snapshot();
        ScenarioParameterWindow? reentrant = null; var nested = true;
        reentrant = new(doc, selected, () => true, () => { nested = reentrant!.ApplyDraft(); return true; });
        Expect(reentrant.ApplyDraft() && !nested, "Reentrant source Save was accepted."); doc.Undo(); Exact(beforeGuards); reentrant.Close();
        ScenarioParameterWindow? changing = null;
        changing = new(doc, selected, () => true, () => { ((TextBox)changing!.FindName("ScenarioRepeatCount")).Text = "3"; return true; });
        Expect(!changing.ApplyDraft(), "Changing input during commit performed cleanup."); changing.Close(); Exact(beforeGuards);
        var closing = workspace.CreateScenarioParameterWindow()!;
        workspace.EndScenarioNamesLifetime(); Expect(!closing.ApplyDraft(), "Closed owner performed cleanup."); closing.Close(); Exact(beforeGuards);
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} source explicit Save: 20 cleared/6 retained repeat windows, read-only opening/Cancel, literal bytes, composed repeat/Undo and stale/lifetime guards");

        void Render(ScenarioParameterWindow window, string state)
        {
            var target = Environment.GetEnvironmentVariable("DK2_SCENARIO_SAVE_RENDER_DIR") ?? directory; Directory.CreateDirectory(target);
            foreach (var width in new[] { 540, 700 })
            {
                var height = width == 540 ? 420 : 580; var content = (FrameworkElement)window.Content;
                content.Measure(new(width, height)); content.Arrange(new(0, 0, width, height)); content.UpdateLayout();
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = new FileStream(Path.Combine(target, $"save-{state}-{language}-{native}-{width}.png"), FileMode.CreateNew); encoder.Save(output);
            }
        }
    }
}
