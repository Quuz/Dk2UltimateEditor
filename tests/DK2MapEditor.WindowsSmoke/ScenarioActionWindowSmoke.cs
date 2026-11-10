using System.Buffers.Binary;
using System.Globalization;
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
    private sealed record ActionWindowPage(string Type, int PageID, NamedWindowControl[] Controls, string[] RequiredCollections);
    private sealed record ActionWindowFixture(ActionWindowPage[] Pages);
    private static void CheckScenarioActionWindows(bool native, string language, string directory)
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioActionPages.json")!;
        var fixture = JsonSerializer.Deserialize<ActionWindowFixture>(stream)!;
        using var offsets = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioParameterPages.json")!;
        var matrix = JsonSerializer.Deserialize<LiteralOptionPage[]>(offsets)!;
        Expect(fixture.Pages.Length == 12, "Original action window fixture incomplete.");
        var document = NativeMapDocument.CreateNewMap(new("ActionUI", "ActionUI", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
        foreach (var id in new[] { 0, 4 })
        {
            var band = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEBAND"), new(0, 0)) : document.AddLegacyRecord("Band", new(0, 0));
            document.SetField(band, EntityReferences.Identity(band)!.Field, id.ToString(CultureInfo.InvariantCulture));
        }
        foreach (var (kind, ids) in new[] { (MapRegionKind.Area, new[] { 13, 39 }), (MapRegionKind.Effect, new[] { 7, 29 }) })
            for (var index = 0; index < ids.Length; index++)
            {
                var region = document.AddMapRegion(kind, new(2 + index * 4, kind == MapRegionKind.Area ? 2 : 8, 1, 1));
                document.SetField(region, EntityReferences.Identity(region)!.Field, ids[index].ToString(CultureInfo.InvariantCulture));
            }
        int[] Values(ActionWindowPage page, NamedWindowControl control) => control.Values.Length != 0 ? control.Values
            : page.Type == "ACTION_RELEASEBAND" && control.Parameter == 1 ? [0, 4] : page.Type == "ACTION_TOGGLEEFFECT" ? [7, 29] : [13, 39];
        var idsByType = new Dictionary<string, ushort>();
        foreach (var page in fixture.Pages)
        {
            var record = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0)) : document.AddLegacyRecord("Action", new(0, 0));
            document.SetEventType(record, LegacyEventSchemas.All.Single(item => item.Name == page.Type));
            record = document.Records().Single(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length); idsByType.Add(page.Type, ScriptGraph.ID(record));
            Expect(MemoryMarshal.TryGetArray(record.File.Data, out var data) && data.Offset == 0, "Independent action imports unavailable.");
            for (var index = 0; index < (native ? 8 : 32); index++) data.Array![record.Offset + (native ? 8 : 16) + index] = (byte)(0x93 ^ index);
        }
        var workspace = new EditorWorkspace { Document = document }; var clipboard = document.CopySelection(new(0, 0, 1, 1)); workspace.LoadSelectionIntoBuffer(clipboard);
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected) => Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)) && ReferenceEquals(workspace.BufferedSelection, clipboard), "Action window changed unrelated bytes/clipboard.");
        LiteralOptionOperand Operand(ActionWindowPage page, int parameter) => native ? matrix.Single(item => item.Type == page.Type).Native.Single(item => item.Name == "Param" + parameter) : new("param" + parameter, 16 + (parameter - 1) * 4, 4, "LONG");
        void Write(Dictionary<NativeFile, byte[]> expected, NativeRecord record, LiteralOptionOperand operand, long value)
        {
            var offset = record.Offset + operand.Offset;
            if (operand.Size == 1) expected[record.File][offset] = checked((byte)value);
            else if (operand.Size == 2) BinaryPrimitives.WriteUInt16LittleEndian(expected[record.File].AsSpan(offset), checked((ushort)value));
            else BinaryPrimitives.WriteInt32LittleEndian(expected[record.File].AsSpan(offset), checked((int)value));
        }
        void Choose(ComboBox combo, long value) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "Value") == value.ToString(CultureInfo.InvariantCulture));
        var applied = 0;
        foreach (var page in fixture.Pages)
        {
            var record = ScriptGraph.Build(document).Events[idsByType[page.Type]].Record;
            var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            var list = Control<ListBox>(workspace, "RecordList"); list.SelectedItem = list.Items.Cast<object>().Single(item => ScriptGraph.ID(Get<NativeRecord>(item, "Record")) == idsByType[page.Type]);
            workspace.Measure(new Size(1000, 760)); workspace.Arrange(new Rect(0, 0, 1000, 760)); workspace.UpdateLayout();
            Expect(Control<Button>(workspace, "ScenarioParameterButton").Visibility == Visibility.Visible, "Complete action page lacks its actual command.");
            ScenarioParameterWindow? presented = null; workspace.ScenarioParameterPresenter = value => presented = value;
            Control<Button>(workspace, "ScenarioParameterButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var window = presented ?? throw new InvalidOperationException("Action page was not presented.");
            T Named<T>(string name) where T : FrameworkElement => (T)(window.FindName(name) ?? throw new InvalidOperationException(name));
            var before = Snapshot(); var history = document.GetHistory().Undo.Count;
            Expect(window.ApplyDraft(), "Existing source prerequisites prevented no-op Apply."); Exact(before); Expect(document.GetHistory().Undo.Count == history, "Action no-op repaired unknown imports.");
            var batch = Snapshot();
            foreach (var control in page.Controls)
            {
                var operand = Operand(page, control.Parameter); var maximum = Math.Min(control.Maximum, operand.Size == 1 ? 255L : operand.Size == 2 ? 65535L : int.MaxValue);
                if (control.Unsigned && operand.Size == 4)
                {
                    var imported = BinaryPrimitives.ReadUInt32LittleEndian(before[record.File].AsSpan(record.Offset + operand.Offset)).ToString(CultureInfo.InvariantCulture);
                    var input = Named<TextBox>("ScenarioParam" + control.Parameter);
                    Expect(input.Text == imported, "Source unsigned input displayed a signed imported number.");
                    input.Text = "0"; input.Text = imported;
                    Expect(window.ApplyDraft(), "Retyping unsigned imported bits failed a no-op."); Exact(before);
                    Expect(document.GetHistory().Undo.Count == history, "Unsigned imported re-entry created history.");
                }
                var values = control.Kind == "Number" ? new[] { 0L, maximum } : Values(page, control).Select(value => (long)value).ToArray();
                void Prepare(Dictionary<NativeFile, byte[]> expected, long value)
                {
                    if (control.Kind == "Number") Named<TextBox>("ScenarioParam" + control.Parameter).Text = value.ToString(CultureInfo.InvariantCulture);
                    else Choose(Named<ComboBox>("ScenarioParam" + control.Parameter), value);
                    Write(expected, record, operand, control.Kind == "Element" ? value >> 16 : value);
                    if (control.SecondaryParameter != 0) Write(expected, record, Operand(page, control.SecondaryParameter), value & 65535);
                }
                foreach (var value in values)
                {
                    var expected = Snapshot(); Prepare(expected, value); Expect(window.ApplyDraft(), "Source action choice/number failed Apply: " + page.Type); Exact(expected);
                    Expect(document.GetHistory().Undo.Count == history + 1, "Actual action did not commit once."); document.Undo(); Exact(before); document.Redo(); Exact(expected); document.Undo(); Exact(before); window.ResetDraft(); applied++;
                }
                if (control.Kind == "Number")
                {
                    Named<TextBox>("ScenarioParam" + control.Parameter).Text = (maximum + 1).ToString(CultureInfo.InvariantCulture);
                    Expect(!window.ApplyDraft(), "Action ignored source/storage bound."); Exact(before); window.ResetDraft();
                }
            }
            foreach (var control in page.Controls)
            {
                var operand = Operand(page, control.Parameter);
                var value = control.Kind == "Number" ? Math.Min(control.Maximum, operand.Size == 1 ? 255L : operand.Size == 2 ? 65535L : int.MaxValue) : Values(page, control).Last();
                if (control.Kind == "Number") Named<TextBox>("ScenarioParam" + control.Parameter).Text = value.ToString(CultureInfo.InvariantCulture); else Choose(Named<ComboBox>("ScenarioParam" + control.Parameter), value);
                Write(batch, record, operand, control.Kind == "Element" ? value >> 16 : value);
                if (control.SecondaryParameter != 0) Write(batch, record, Operand(page, control.SecondaryParameter), value & 65535);
            }
            LocalizationService.SetLanguage(language == "ru" ? "en" : "ru"); Exact(before); LocalizationService.SetLanguage(language); Exact(before);
            Expect(window.ApplyDraft(), "Translated full action batch failed."); Exact(batch); Expect(document.GetHistory().Undo.Count == history + 1, "Action batch was not atomic."); document.Undo(); Exact(before); window.ResetDraft();
            if (Environment.GetEnvironmentVariable("DK2_SCENARIO_ACTION_RENDER_DIR") is { Length: > 0 } artifacts)
            {
                Directory.CreateDirectory(artifacts); var content = (FrameworkElement)window.Content;
                content.Measure(new Size(540, 420)); content.Arrange(new Rect(0, 0, 540, 420)); content.UpdateLayout();
                var bitmap = new RenderTargetBitmap(540, 420, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var image = File.Create(Path.Combine(artifacts, language + "-" + (native ? "native-" : "project-") + page.Type + ".png")); encoder.Save(image);
            }
            Choose(Named<ComboBox>("ScenarioParam" + page.Controls[0].Parameter), Values(page, page.Controls[0]).First()); window.Close(); Exact(before); Expect(!window.ApplyDraft(), "Closed action remained editable.");
            var empty = NativeMapDocument.CreateNewMap(new("EmptyActionUI", "EmptyActionUI", 20, 20, NativeFormat: native));
            var added = native ? empty.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0)) : empty.AddLegacyRecord("Action", new(0, 0)); empty.SetEventType(added, LegacyEventSchemas.All.Single(item => item.Name == page.Type));
            var emptyRecord = ScriptGraph.Build(empty).Events[ScriptGraph.ID(added)].Record; var emptyBefore = empty.Files.ToDictionary(file => file, file => file.Data.ToArray()); var revision = empty.EditRevision; var emptyHistory = empty.GetHistory();
            var emptyWindow = new ScenarioParameterWindow(empty, emptyRecord, () => true, () => true);
            Expect(((Button)emptyWindow.FindName("ScenarioPageApply")).IsEnabled == (page.RequiredCollections.Length == 0) && emptyWindow.ApplyDraft() == (page.RequiredCollections.Length == 0), "Actual missing-list guard differs from original source.");
            if (page.RequiredCollections.Length != 0) Expect(((TextBlock)emptyWindow.FindName("ScenarioPageStatus")).Text.Length != 0, "Missing objects have no translated explanation.");
            var acceptedEmptySave = page.RequiredCollections.Length == 0;
            var emptyAfter = empty.GetHistory();
            Expect(empty.EditRevision == revision + (acceptedEmptySave ? 1 : 0)
                && emptyAfter.ChangeCount == emptyHistory.ChangeCount + (acceptedEmptySave ? 1 : 0)
                && emptyAfter.EditorChangeCount == unchecked(emptyHistory.EditorChangeCount + (acceptedEmptySave ? 1u : 0u))
                && emptyAfter.Undo.Count == emptyHistory.Undo.Count && emptyAfter.Redo.Count == emptyHistory.Redo.Count,
                "Empty-list parent Save counters/history differ from source; a refused page must remain unchanged.");
            Expect(emptyBefore.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Empty-list page changed literal bytes."); emptyWindow.Close();
            var privateRevision = empty.EditRevision; var privateHistory = empty.GetHistory();
            var privateWindow = new ScenarioParameterWindow(empty, emptyRecord, () => true, () => true, privateCreation: true);
            Expect(privateWindow.ApplyDraft() == acceptedEmptySave, "Private empty-list guard differs from source.");
            Expect(empty.EditRevision == privateRevision && empty.GetHistory().ChangeCount == privateHistory.ChangeCount
                && empty.GetHistory().EditorChangeCount == privateHistory.EditorChangeCount
                && emptyBefore.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Private unchanged page acquired parent Save state.");
            privateWindow.Close();
        }
        workspace.EndScenarioNamesLifetime();
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} 12 named action windows, source choices and bounds ({applied}), map identities, prerequisites, atomic history, translation and Cancel");
    }
}
