using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioAliasEvents(bool native, string language, string directory)
    {
        foreach (var typeName in new[] { "TRIGGER_VARIABLE", "TRIGGER_TIMER", "ACTION_SETVARIABLE", "ACTION_SETTIMER" })
        {
            var document = NativeMapDocument.CreateNewMap(new("AliasEventUI", "AliasEventUI", 20, 20, NativeFormat: native));
            var type = LegacyEventSchemas.All.Single(type => type.Name == typeName);
            var added = native ? document.AddRecord(RecordTemplate.All.Single(template => template.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0))
                : document.AddLegacyRecord(type.RecordSignature == 0xD5 ? "Trigger" : "Action", new(0, 0));
            document.SetEventType(added, type); var record = ScriptGraph.Build(document).Events[ScriptGraph.ID(added)].Record;
            void Set(int parameter, string value) => document.SetField(record, record.Fields.Single(field => field.Name == (native ? "Param" : "param") + parameter), value);
            var trigger = typeName.StartsWith("TRIGGER_"); var primaryParameter = trigger ? 2 : 1;
            Set(primaryParameter, "7"); if (trigger) Set(1, "3");
            if (typeName == "TRIGGER_VARIABLE") { Set(3, "1"); Set(4, "0"); Set(5, "42"); }
            if (typeName == "TRIGGER_TIMER") Set(5, "3661");
            if (typeName == "ACTION_SETVARIABLE") { Set(2, "10"); Set(5, "42"); }
            var kind = typeName is "TRIGGER_VARIABLE" or "ACTION_SETVARIABLE" ? ScenarioNameKind.Variable : ScenarioNameKind.Timer;
            document.SetScenarioName(kind, 7, "Keep");
            var workspace = new EditorWorkspace { Document = document }; Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
            void WorkspaceLayout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); }
            var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
            var records = Control<ListBox>(workspace, "RecordList"); records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == record.Offset); WorkspaceLayout();
            ScenarioAliasEventWindow? window = null; workspace.ScenarioAliasEventPresenter = shown => window = shown;
            var command = Control<Button>(workspace, "ScenarioAliasEventButton"); Expect(command.IsEnabled && command.Visibility == Visibility.Visible, "Alias event command missing on supported source event."); command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(window is not null, "Actual alias command did not create a window.");
            T Named<T>(string name) => (T)window!.FindName("AliasEvent" + name);
            void Layout() { var content = (FrameworkElement)window!.Content; content.Measure(new Size(780, 640)); content.Arrange(new Rect(0, 0, 780, 640)); content.UpdateLayout(); }
            void Choose(ComboBox combo, string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == id);
            Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Alias window changed source before Apply or failed exact Undo.");
            string Alias(int index) => document.ReadScenarioNames().Slots.Single(slot => slot.Kind == kind && slot.Index == index).StoredName;
            string Value(int parameter) => document.CreateScenarioAliasEventDraft(record).Values[parameter];
            Layout(); var initial = Snapshot(); var history = document.GetHistory().Undo.Count;
            var primary = Named<ComboBox>("Primary"); var secondary = Named<ComboBox>("Secondary"); var operation = Named<ComboBox>("Operation"); var mode = Named<ComboBox>("Mode"); var number = Named<TextBox>("Number");
            Expect(primary.IsEditable && primary.Items.Count == (kind == ScenarioNameKind.Variable ? 129 : typeName == "ACTION_SETTIMER" ? 16 : 17) && primary.Text == "Keep", "Original editable primary alias catalogue differs.");
            Expect(window!.ApplyDraft() && document.GetHistory().Undo.Count == history, "Unchanged window added aliases or normalized operands."); Exact(initial);
            primary.Text = "Pending café"; Layout(); Exact(initial); Expect(window.HasPendingInputs && Alias(7) == "Keep", "Typing changed the source/failed to capture alias input.");
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Layout(); Expect(primary.Text == "Pending café" && window.Title == LocalizationService.Get("AliasEvent.Title"), "Translation lost alias input or left title untranslated.");
            LocalizationService.SetLanguage(language); Layout(); Expect(window.ApplyDraft() && Value(primaryParameter) == "7" && Alias(7) == "Pending café" && document.GetHistory().Undo.Count == history + 1, "Typing an alias changed its ID or split history.");
            workspace.Undo(); Exact(initial); Expect(Alias(7) == "Keep" && !Named<Button>("Apply").IsEnabled, "Undo did not restore alias/invalidate draft."); window.ResetDraft();
            primary.Text = "Unaccepted"; Named<Button>("Discard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(primary.Text == "Keep" && !window.HasPendingInputs, "Reload retained an unapplied alias."); Exact(initial);
            if (typeName == "TRIGGER_VARIABLE")
            {
                Choose(mode, "2"); Choose(secondary, "8"); Choose(operation, "6"); primary.Text = "Variable operand";
                Expect(!secondary.IsEditable && secondary.IsEnabled && !number.IsEnabled && window.ApplyDraft(), "Variable condition mode/control state failed.");
                Expect(Value(1) == "6" && Value(3) == "2" && Value(4) == "8" && Value(5) == "0" && Alias(8) == "", "Condition source mode did not clear inactive number or renamed secondary."); workspace.Undo(); window.ResetDraft(); Exact(initial);
            }
            if (typeName == "ACTION_SETVARIABLE")
            {
                Expect(Get<string>(secondary.SelectedItem,"ID")=="0"&&number.Text=="42","Literal source value became a hidden variable ID.");
                Choose(operation, "20");Expect(Get<string>(secondary.SelectedItem,"ID")=="0"&&number.Text=="42","Entering variable mode lost the source slot0 or cached number.");
                Choose(secondary, "8"); Layout(); primary.Text = "Destination"; secondary.Text = "Source";
                Expect(secondary.IsEditable && secondary.IsEnabled && window.ApplyDraft() && Value(2) == "20" && Value(5) == "8" && Alias(7) == "Destination" && Alias(8) == "Source", "Source-variable arithmetic or separate alias inputs failed."); workspace.Undo(); window.ResetDraft(); Exact(initial);
                Choose(operation, "12"); Choose(secondary, "7"); Layout(); primary.Text = "Shared slot"; Expect(secondary.Text == "Shared slot" && window.ApplyDraft() && Alias(7) == "Shared slot", "Same-slot operands created conflicting aliases."); workspace.Undo(); window.ResetDraft(); Exact(initial);
            }
            if (typeName == "TRIGGER_TIMER")
            {
                Named<TextBox>("Hours").Text = "2"; Named<TextBox>("Minutes").Text = "3"; Named<TextBox>("Seconds").Text = "4";
                Expect(window.ApplyDraft() && Value(5) == "7384", "Named time units encoded incorrectly."); workspace.Undo(); window.ResetDraft(); Exact(initial);
                Named<TextBox>("Minutes").Text = "60"; Expect(window.ApplyDraft() && Value(5) == "7201", "Original two-digit minute arithmetic was restricted to 59."); workspace.Undo(); Exact(initial); window.ResetDraft();
                Named<TextBox>("Minutes").Text = "100"; Expect(!window.ApplyDraft(), "Three-digit minutes exceeded the original control range."); Exact(initial); window.ResetDraft();
            }
            if (typeName == "ACTION_SETTIMER") Expect(primary.Items.Cast<object>().All(item => Get<string>(item, "ID") != "16"), "SetTimer invented time-limit slot.");
            primary.Text = "Кириллица"; Expect(!window.ApplyDraft() && Alias(7) == "Keep", "Unsupported alias characters changed source."); Exact(initial); window.ResetDraft();
            if (Environment.GetEnvironmentVariable("DK2_ALIAS_EVENT_UI_ARTIFACTS") is { Length: > 0 } artifacts)
            {
                Directory.CreateDirectory(artifacts);
                foreach (var bounds in new[] { new Size(780, 640), new Size(660, 480) })
                {
                    var content = (FrameworkElement)window.Content; content.Measure(bounds); content.Arrange(new Rect(new Point(), bounds)); content.UpdateLayout();
                    var bitmap = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = File.Create(Path.Combine(artifacts, $"alias-{language}-{native}-{typeName}-{bounds.Width}.png")); encoder.Save(output);
                }
                Layout();
            }
            // Actual pending generic grid commit must happen before Apply,
            // invalidate its authority, and retain only the generic edit.
            primary.Text = "Must not apply"; var grid = Control<DataGrid>(workspace, "PropertyGrid");
            var item = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == (native ? "Param" : "param") + primaryParameter);
            grid.SelectedItem = item; grid.ScrollIntoView(item); WorkspaceLayout(); grid.CurrentCell = new(item, grid.Columns[1]); Expect(grid.BeginEdit(), "Actual pending event grid editor did not start."); WorkspaceLayout();
            var editor = grid.Columns[1].GetCellContent(item) as TextBox; Expect(editor is not null, "Actual event grid TextBox absent."); editor!.Text = "8";
            Expect(!window.ApplyDraft() && Value(primaryParameter) == "8" && Alias(7) == "Keep" && Alias(8) == "", "Pending generic commit accepted a stale alias edit."); workspace.Undo(); Exact(initial); window.ResetDraft();
            primary.Text = "Cancelled"; window.Close(); Expect(!window.ApplyDraft() && Alias(7) == "Keep", "Closed window applied an alias draft."); Exact(initial);
            var invalidOwner = new ScenarioAliasEventWindow(document, record, () => false, () => true); Expect(!invalidOwner.ApplyDraft(), "Closed owner accepted alias edits."); invalidOwner.Close();
            Set(primaryParameter, typeName == "ACTION_SETTIMER" ? "16" : "255"); var imported = Snapshot();
            var unknownWindow = new ScenarioAliasEventWindow(document, record, () => true, () => true); var unknownContent = (FrameworkElement)unknownWindow.Content;
            unknownContent.Measure(new Size(780, 640)); unknownContent.Arrange(new Rect(0, 0, 780, 640)); unknownContent.UpdateLayout();
            Expect(((ComboBox)unknownWindow.FindName("AliasEventPrimary")).IsReadOnly && unknownWindow.ApplyDraft(), "Unknown/excluded imported ID was editable or rejected on no-op."); Exact(imported);
            var unknownPrimary = (ComboBox)unknownWindow.FindName("AliasEventPrimary"); Choose(unknownPrimary, "8"); unknownPrimary.Text = "Explicit repair";
            Expect(!unknownPrimary.IsReadOnly && unknownWindow.ApplyDraft() && Value(primaryParameter) == "8" && Alias(8) == "Explicit repair", "Choosing a verified source slot did not allow explicit alias repair.");
            workspace.Undo(); Exact(imported);
            unknownWindow.Close(); workspace.Undo(); Exact(initial);
            ScenarioAliasEventWindow? reentrant = null; var nested = true; reentrant = new(document, record, () => true, () => { nested = reentrant!.ApplyDraft(); return true; });
            Expect(reentrant.ApplyDraft() && !nested, "Reentrant alias apply bypassed its latch."); reentrant.Close();
            ScenarioAliasEventWindow? changedInputs = null; changedInputs = new(document, record, () => true, () => { ((TextBox)changedInputs!.FindName("AliasEventNumber")).Text = "999"; return true; });
            Expect(!changedInputs.ApplyDraft(), "Inputs changed during commit were applied with old authority."); Exact(initial); changedInputs.Close();
            workspace.Document = null; Expect(command.Visibility == Visibility.Collapsed && !command.IsEnabled, "Closed document retained an event alias command.");
            Console.WriteLine($"PASS {language} {(native ? "native" : "project")} {typeName} inline names, stable ID, source operands, atomic Undo, translation, pending commit and Cancel");
        }
    }
}
