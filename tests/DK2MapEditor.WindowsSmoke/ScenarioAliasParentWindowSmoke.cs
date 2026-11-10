using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioAliasParentWindows(bool native, string language, string directory)
    {
        foreach (var typeName in new[] { "TRIGGER_VARIABLE", "TRIGGER_TIMER", "ACTION_SETVARIABLE", "ACTION_SETTIMER" })
        {
            var document = NativeMapDocument.CreateNewMap(new("AliasParent", "AliasParent", 20, 20, NativeFormat: native));
            var type = LegacyEventSchemas.All.Single(item => item.Name == typeName);
            var added = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0))
                : document.AddLegacyRecord(type.RecordSignature == 0xD5 ? "Trigger" : "Action", new(0, 0));
            document.SetEventType(added, type); var record = ScriptGraph.Build(document).Events[ScriptGraph.ID(added)].Record;
            var trigger = typeName.StartsWith("TRIGGER_", StringComparison.Ordinal);
            var primaryParameter = trigger ? 2 : 1;
            var kind = typeName is "TRIGGER_VARIABLE" or "ACTION_SETVARIABLE" ? ScenarioNameKind.Variable : ScenarioNameKind.Timer;
            void Set(int number, string value) => document.SetField(record, record.Fields.Single(field => field.Name == (native ? "Param" : "param") + number), value);
            Set(primaryParameter, "7");
            if (trigger) { Set(1, "3"); document.SetField(record, record.Fields.Single(field => field.Name == (native ? "Repeat" : "repeat")), "1"); }
            if (typeName == "TRIGGER_VARIABLE") { Set(3, "1"); Set(4, "0"); Set(5, "42"); }
            if (typeName == "TRIGGER_TIMER") Set(5, "3661");
            if (typeName == "ACTION_SETVARIABLE") { Set(2, "10"); Set(5, "42"); }
            document.SetScenarioName(kind, 7, "Keep");
            var workspace = new EditorWorkspace { Document = document };
            Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
            var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            var records = Control<ListBox>(workspace, "RecordList"); records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == record.Offset);
            void WorkspaceLayout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); }
            WorkspaceLayout(); ScenarioParameterWindow? window = null;
            workspace.ScenarioParameterPresenter = shown => window = shown;
            var command = Control<Button>(workspace, "ScenarioParameterButton");
            Expect(command.IsEnabled && command.Visibility == Visibility.Visible, "Shared alias parent command is unavailable.");
            command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(window is not null, "Shared command did not open an alias parent.");
            T Named<T>(string name) => (T)window!.FindName(name);
            void Layout(Size? size = null) { var bounds = size ?? new Size(700, 580); var content = (FrameworkElement)window!.Content; content.Measure(bounds); content.Arrange(new Rect(new Point(), bounds)); content.UpdateLayout(); }
            Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            void Exact(Dictionary<NativeFile, byte[]> expected) => Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Alias parent changed unrelated bytes or wrote before Apply.");
            string Alias(int index) => document.ReadScenarioNames().Slots.Single(slot => slot.Kind == kind && slot.Index == index).StoredName;
            void Choose(ComboBox combo, string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == id);
            Layout(); var initial = Snapshot(); var history = document.GetHistory().Undo.Count;
            Expect(window!.ApplyDraft() && document.GetHistory().Undo.Count == history, "Shared alias parent no-op created history."); Exact(initial);
            var primary = Named<ComboBox>("AliasEventPrimary"); primary.Text = "Parent cafe";
            if (typeName is "TRIGGER_VARIABLE" or "ACTION_SETVARIABLE") Named<TextBox>("AliasEventNumber").Text = "43";
            if (typeName == "TRIGGER_TIMER") { Named<TextBox>("AliasEventHours").Text = "2"; Named<TextBox>("AliasEventMinutes").Text = "3"; Named<TextBox>("AliasEventSeconds").Text = "4"; }
            if (trigger)
            {
                var repeat = Named<ComboBox>("ScenarioRepeatMode"); repeat.SelectedItem = repeat.Items.Cast<object>().Single(item => Get<ScenarioRepeatMode>(item, "Mode") == ScenarioRepeatMode.Count);
                Named<TextBox>("ScenarioRepeatCount").Text = "254";
            }
            Layout(); Expect(window.HasPendingInputs && Alias(7) == "Keep", "Child typing escaped its private parent draft."); Exact(initial);
            Named<Button>("AliasEventApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial);
            Expect(Alias(7) == "Keep" && document.GetHistory().Undo.Count == history, "Embedded alias child applied independently.");
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Layout();
            Expect(primary.Text == "Parent cafe" && window.Title == LocalizationService.Get("ScenarioPage.Title"), "Parent translation lost alias input/title.");
            if (trigger) Expect(Named<TextBox>("ScenarioRepeatCount").Text == "254", "Parent translation lost repeat input.");
            LocalizationService.SetLanguage(language); Layout();
            var expected = initial.ToDictionary(item => item.Key, item => item.Value.ToArray());
            if (typeName != "ACTION_SETTIMER") BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(record.Offset + (native ? 12 : 32)), typeName == "TRIGGER_TIMER" ? 7384u : 43u);
            if (trigger) expected[record.File][record.Offset + (native ? 23 : 55)] = 254;
            if (!native)
            {
                var file = document.Files.Single(file => file.Signature == NativeMapDocument.LegacyEvents);
                var offset = file.Length - 146 * 64 + (kind == ScenarioNameKind.Variable ? 7 : 136) * 64;
                expected[file].AsSpan(offset, 64).Clear(); Encoding.ASCII.GetBytes("Parent cafe").CopyTo(expected[file], offset);
            }
            Expect(window.ApplyDraft(), "Parent rejected valid aliases/operands/repeat."); Exact(expected);
            Expect(Alias(7) == "Parent cafe" && document.GetHistory().Undo.Count == history + 1, "Parent aliases/operands/repeat were split across Undo.");
            var edited = Snapshot(); workspace.Undo(); Exact(initial); Expect(Alias(7) == "Keep", "Parent Undo failed to restore alias metadata.");
            workspace.Redo(); Exact(edited); Expect(Alias(7) == "Parent cafe", "Parent Redo lost alias metadata."); workspace.Undo(); window.ResetDraft(); Layout();
            primary.Text = "Unaccepted"; Named<Button>("ScenarioPageDiscard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Layout();
            Expect(primary.Text == "Keep" && !window.HasPendingInputs, "Parent Discard retained child inputs."); Exact(initial);
            primary.Text = "Кириллица"; Expect(!window.ApplyDraft() && Alias(7) == "Keep", "Invalid child alias was accepted by parent."); Exact(initial); window.ResetDraft(); Layout();
            if (Environment.GetEnvironmentVariable("DK2_ALIAS_PARENT_UI_ARTIFACTS") is { Length: > 0 } artifacts)
            {
                Directory.CreateDirectory(artifacts);
                foreach (var size in new[] { new Size(700, 580), new Size(540, 420) })
                {
                    Layout(size); var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render((FrameworkElement)window.Content);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = new FileStream(Path.Combine(artifacts, $"alias-parent-{typeName}-{language}-{native}-{size.Width}.png"), FileMode.CreateNew); encoder.Save(stream);
                }
            }
            primary.Text = "Pending";
            var grid = Control<DataGrid>(workspace, "PropertyGrid") ?? throw new InvalidOperationException("Parent ordinary property grid is unavailable.");
            var item = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field")?.Name == (native ? "Param" : "param") + primaryParameter);
            grid.SelectedItem = item; grid.ScrollIntoView(item); WorkspaceLayout(); grid.CurrentCell = new(item, grid.Columns[1]); Expect(grid.BeginEdit(), "Parent pending generic editor did not start."); WorkspaceLayout();
            var fieldInput = grid.Columns[1].GetCellContent(item) as TextBox; Expect(fieldInput is not null, "Parent generic TextBox absent."); fieldInput!.Text = "8";
            Expect(!window.ApplyDraft() && document.CreateScenarioAliasEventDraft(record).Values[primaryParameter] == "8" && Alias(7) == "Keep", "Generic commit accepted stale parent child inputs.");
            workspace.Undo(); Exact(initial); window.ResetDraft(); Layout();
            primary.Text = "Cancel"; if (trigger) Named<TextBox>("ScenarioRepeatCount").Text = "0";
            window.Close(); Expect(!window.ApplyDraft() && Alias(7) == "Keep", "Parent Cancel applied child state."); Exact(initial);
            ScenarioParameterWindow? reentrant = null; var nested = true;
            reentrant = new(document, record, () => true, () => { nested = reentrant!.ApplyDraft(); return true; });
            Expect(reentrant.ApplyDraft() && !nested, "Alias parent bypassed reentrance guard."); reentrant.Close();
            ScenarioParameterWindow? changed = null;
            changed = new(document, record, () => true, () => { ((TextBox)changed!.FindName("AliasEventNumber")).Text = "999"; return true; });
            Expect(!changed.ApplyDraft(), "Parent accepted alias inputs changed during generic commit."); Exact(initial); changed.Close();
            var invalidOwner = new ScenarioParameterWindow(document, record, () => false, () => true); Expect(!invalidOwner.ApplyDraft(), "Closed parent owner accepted alias changes."); invalidOwner.Close();
            Set(primaryParameter, typeName == "ACTION_SETTIMER" ? "16" : "255"); var imported = Snapshot();
            var unknown = new ScenarioParameterWindow(document, record, () => true, () => true);
            var unknownContent = (FrameworkElement)unknown.Content; unknownContent.Measure(new Size(700, 580)); unknownContent.Arrange(new Rect(0, 0, 700, 580)); unknownContent.UpdateLayout();
            var unknownPrimary = (ComboBox)unknown.FindName("AliasEventPrimary");
            Expect(unknownPrimary.IsReadOnly && unknown.ApplyDraft(), "Parent coerced or exposed unknown alias slot."); Exact(imported);
            Choose(unknownPrimary, "8"); unknownPrimary.Text = "Repair"; Expect(unknown.ApplyDraft() && Alias(8) == "Repair", "Parent source-slot repair failed."); workspace.Undo(); Exact(imported); unknown.Close(); workspace.Undo(); Exact(initial);
            var closing = workspace.CreateScenarioParameterWindow(); Expect(closing is not null, "Parent lifetime test failed to create a window."); workspace.Document = null;
            Expect(!closing!.ApplyDraft(), "Document replacement retained parent authority."); closing.Close();
        }
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} four alias parent windows, private drafts, repeat/operand/name atomic Undo, translation, generic commit, Cancel and lifetime guards");
    }
}
