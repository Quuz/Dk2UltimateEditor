using System.Buffers.Binary;
using System.Globalization;
using System.IO;
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
    private static void CheckScenarioSpawnParent(bool native, string language, string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("SpawnParentUI", "SpawnParentUI", 20, 20, NativeFormat: native));
        var area = doc.AddMapRegion(MapRegionKind.Area, new(3, 5, 8, 6)); doc.SetField(area, EntityReferences.Identity(area)!.Field, "13");
        var added = native ? doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0)) : doc.AddLegacyRecord("Action", new(0, 0));
        doc.SetEventType(added, LegacyEventSchemas.All.Single(item => item.Name == "ACTION_ADDCREATURE"));
        var record = doc.Records().Single(item => item.File == added.File && item.Offset == added.Offset);
        string Name(int number) => (native ? "Param" : "param") + number;
        void Set(int number, string value) => doc.SetField(record, record.Fields.Single(item => item.Name == Name(number)), value);
        var bits = native ? 66u : 0x80010042u;
        Set(4, unchecked((int)bits).ToString(CultureInfo.InvariantCulture)); Set(5, "900"); Set(7, "901");
        var workspace = new EditorWorkspace { Document = doc }; Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
        var records = Control<ListBox>(workspace, "RecordList"); records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == record.Offset);
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var originalSelection = canvas.Selection;
        void WorkspaceLayout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); }
        WorkspaceLayout(); ScenarioParameterWindow? parent = null; workspace.ScenarioParameterPresenter = window => parent = window;
        var command = Control<Button>(workspace, "ScenarioParameterButton"); Expect(command.IsEnabled, "Common spawn command disabled."); command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(parent is not null, "Spawn parent did not open from common toolbar.");
        T Field<T>(string name) => (T)parent!.FindName(name);
        void Layout(Window window, Size size) { var content = (FrameworkElement)window.Content; content.Measure(size); content.Arrange(new Rect(new Point(), size)); content.UpdateLayout(); }
        Dictionary<NativeFile, byte[]> Snapshot() => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected) => Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Spawn child/parent changed unrelated or premature bytes.");
        void Write(Dictionary<NativeFile, byte[]> expected, int parameter, uint value)
        {
            var bytes = expected[record.File];
            if (!native) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(record.Offset + 12 + parameter * 4), value);
            else if (parameter <= 4) bytes[record.Offset + 7 + parameter] = checked((byte)value);
            else BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(record.Offset + (parameter == 5 ? 12 : 14)), checked((ushort)value));
        }
        void Render(Window window, string state, Size size)
        {
            if (Environment.GetEnvironmentVariable("DK2_SPAWN_PARENT_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return;
            Directory.CreateDirectory(artifacts); Layout(window, size); var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render((Visual)window.Content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new FileStream(Path.Combine(artifacts, $"spawn-{state}-{language}-{native}-{size.Width}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        Layout(parent!, new(700, 580)); var initial = Snapshot(); var history = doc.GetHistory().Undo.Count;
        Expect(Field<TextBox>("ScenarioParam4").IsReadOnly && Field<TextBox>("ScenarioParam4").Text == bits.ToString(CultureInfo.InvariantCulture), "Spawn flags were manually editable/signed/repaired on opening.");
        Expect(parent!.ApplyDraft() && doc.GetHistory().Undo.Count == history, "Spawn no-op made history."); Exact(initial);
        using var namedStream = typeof(Program).Assembly.GetManifestResourceStream("DK2MapEditor.WindowsSmoke.ScenarioNamedPages.json")!;
        using var named = JsonDocument.Parse(namedStream); var applied = 0;
        foreach (var context in named.RootElement.GetProperty("Contexts").EnumerateArray().Where(item => item.GetProperty("Type").GetString() == "ACTION_ADDCREATURE"))
        {
            var parameter = context.GetProperty("Parameter").GetInt32(); var picker = Field<ComboBox>("ScenarioParam" + parameter);
            foreach (var value in context.GetProperty("Values").EnumerateArray().Select(item => item.GetInt32()))
            {
                picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<string>(item, "Value") == value.ToString(CultureInfo.InvariantCulture));
                var expected = Snapshot(); Write(expected, parameter, checked((uint)value)); Exact(initial);
                Expect(parent.ApplyDraft(), "Spawn source catalogue choice failed to apply."); Exact(expected);
                if (doc.GetHistory().Undo.Count != history) { Expect(doc.GetHistory().Undo.Count == history + 1, "Spawn choice made multiple Undo entries."); workspace.Undo(); Exact(initial); }
                parent.ResetDraft(); applied++;
            }
        }
        foreach (var width in new[] { 700, 540 }) Render(parent, "imported", new(width, width == 700 ? 580 : 420));
        ScenarioSpawnFlagsWindow? child = null; parent.SpawnFlagsPresenter = window => child = window;
        Field<Button>("ScenarioSpawnFlags").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(child is not null && ReferenceEquals(child, parent.CreateSpawnFlagsWindow()), "Spawn option button did not open/reuse private child.");
        Expect(!((CheckBox)child!.FindName("SpawnFlag64")).IsEnabled && ((CheckBox)child.FindName("SpawnFlag64")).IsChecked == true, "Source-disabled placement bit was editable or cleared.");
        ((CheckBox)child.FindName("SpawnFlag64")).IsChecked = false; // Even a scripted disabled-control change must not clear retained bits.
        var editable = new uint[] { 1, 8, 16, 32, 128, 4 };
        for (var combination = 0; combination < 64; combination++)
        {
            var expectedBits = bits & ~0xBDu;
            for (var index = 0; index < editable.Length; index++)
            { var selected = (combination & (1 << index)) != 0; ((CheckBox)child.FindName("SpawnFlag" + editable[index])).IsChecked = selected; if (selected) expectedBits |= editable[index]; }
            Expect(child.ApplyDraft() && Field<TextBox>("ScenarioParam4").Text == expectedBits.ToString(CultureInfo.InvariantCulture), "Private flags lost unknown/disabled bits or source assignment."); Exact(initial);
            Expect(doc.GetHistory().Undo.Count == history, "Child flags Apply wrote document.");
        }
        ((CheckBox)child.FindName("SpawnFlag64")).IsChecked = true; // Restore the real disabled presentation after the scripted guard probe.
        Render(child, "flags", new(580, 440));
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Layout(parent, new(700, 580));
        Expect(Field<TextBox>("ScenarioParam4").Text == (bits | 0xBDu).ToString(CultureInfo.InvariantCulture)
            && Get<string>(Field<ComboBox>("ScenarioSpawnArea").SelectedItem, "Label") == LocalizationService.Get("ScenarioSpawn.NoArea"), "Spawn language change lost private flags or area caption."); LocalizationService.SetLanguage(language); child.Close();
        var areaPicker = Field<ComboBox>("ScenarioSpawnArea"); areaPicker.SelectedItem = areaPicker.Items.Cast<object>().Single(item => Get<uint>(item, "ID") == 13);
        Expect(Field<TextBox>("ScenarioParam5").Text == "3" && Field<TextBox>("ScenarioParam7").Text == "5", "Area chooser used centre/ID/one-based values."); Exact(initial);
        Field<Button>("ScenarioSpawnPoint").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(canvas.IsPickingRegion, "Spawn point command did not use actual map canvas.");
        canvas.CompleteRegionPick(new(7, 8), new(7, 8)); Expect(Field<TextBox>("ScenarioParam5").Text == "7" && Field<TextBox>("ScenarioParam7").Text == "8" && canvas.Selection == originalSelection, "Canvas pick changed ordinary selection or stored offset coordinates."); Exact(initial);
        foreach (var width in new[] { 700, 540 }) Render(parent, "selected", new(width, width == 700 ? 580 : 420));
        var batch = Snapshot(); Write(batch, 4, bits | 0xBDu); Write(batch, 5, 7); Write(batch, 7, 8);
        Expect(parent.ApplyDraft(), "Spawn parent did not accept child flags and canvas point together."); Exact(batch); Expect(doc.GetHistory().Undo.Count == history + 1, "Spawn whole parent was split into multiple edits.");
        workspace.Undo(); Exact(initial); workspace.Redo(); Exact(batch); workspace.Undo(); parent.ResetDraft();
        child = parent.CreateSpawnFlagsWindow()!; ((CheckBox)child.FindName("SpawnFlag1")).IsChecked = true; child.Close(); Expect(Field<TextBox>("ScenarioParam4").Text == bits.ToString(CultureInfo.InvariantCulture), "Child Cancel accepted unconfirmed flags.");
        child = parent.CreateSpawnFlagsWindow()!; parent.ResetDraft(); Expect(!child.ApplyDraft(), "Reload retained child Apply authority."); Exact(initial);
        Field<Button>("ScenarioSpawnPoint").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(canvas.IsPickingRegion, "Second canvas pick did not start."); parent.ResetDraft(); Expect(!canvas.IsPickingRegion, "Reload retained pending canvas pick.");
        Field<TextBox>("ScenarioParam5").Text = "1000"; Expect(!parent.ApplyDraft(), "Spawn X exceeded original 3-digit limit."); parent.ResetDraft(); Exact(initial);
        child = parent.CreateSpawnFlagsWindow()!; Set(5, "1"); Expect(!child.ApplyDraft() && parent.CreateSpawnFlagsWindow() is null && !parent.ApplySpawnPoint(new(1, 1)), "External edit retained child/point authority."); workspace.Undo(); parent.ResetDraft(); Exact(initial);
        Field<Button>("ScenarioSpawnPoint").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); child = parent.CreateSpawnFlagsWindow()!; parent.Close(); Expect(!canvas.IsPickingRegion && !child.ApplyDraft() && !parent.ApplyDraft(), "Parent Cancel retained child or canvas authority."); Exact(initial);
        var closing = workspace.CreateScenarioParameterWindow()!; child = closing.CreateSpawnFlagsWindow()!; workspace.Document = null;
        Expect(!closing.ApplyDraft() && !child.ApplyDraft(), "Closed/replaced document accepted private spawn flags."); closing.Close(); Exact(initial);
        workspace.Document = doc; categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
        records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == record.Offset); Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1; WorkspaceLayout();
        parent = workspace.CreateScenarioParameterWindow()!; Layout(parent, new(700, 580)); child = parent.CreateSpawnFlagsWindow()!;
        ((CheckBox)child.FindName("SpawnFlag1")).IsChecked = true; Expect(child.ApplyDraft(), "Spawn generic-commit setup failed."); child.Close();
        var grid = Control<DataGrid>(workspace, "PropertyGrid") ?? throw new InvalidOperationException("Spawn generic grid absent.");
        var item = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field")?.Name == Name(5));
        grid.SelectedItem = item; grid.ScrollIntoView(item); WorkspaceLayout(); grid.CurrentCell = new(item, grid.Columns[1]); Expect(grid.BeginEdit(), "Spawn pending generic field did not start."); WorkspaceLayout();
        var fieldInput = grid.Columns[1].GetCellContent(item) as TextBox; Expect(fieldInput is not null, "Spawn generic text input absent."); fieldInput!.Text = "7";
        Expect(!parent.ApplyDraft() && record.Read(record.Fields.Single(field => field.Name == Name(5))) == "7"
            && record.Read(record.Fields.Single(field => field.Name == Name(4))) == unchecked((int)bits).ToString(CultureInfo.InvariantCulture), "Spawn parent overwrote pending generic commit or applied private flags after it.");
        workspace.Undo(); Exact(initial); parent.Close();
        ScenarioParameterWindow? reentrant = null; var nested = true;
        reentrant = new(doc, record, () => true, () => { nested = reentrant!.ApplyDraft(); return true; }); Expect(reentrant.ApplyDraft() && !nested, "Spawn parent reentrance guard failed."); reentrant.Close();
        ScenarioParameterWindow? changing = null;
        changing = new(doc, record, () => true, () => { ((TextBox)changing!.FindName("ScenarioParam5")).Text = "2"; return true; }); Expect(!changing.ApplyDraft(), "Spawn input changed during commit was accepted."); changing.Close(); Exact(initial);
        var noArea = NativeMapDocument.CreateNewMap(new("NoAreaSpawn", "NoAreaSpawn", 20, 20, NativeFormat: native));
        var noAreaAdded = native ? noArea.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0)) : noArea.AddLegacyRecord("Action", new(0, 0));
        noArea.SetEventType(noAreaAdded, LegacyEventSchemas.All.Single(item => item.Name == "ACTION_ADDCREATURE")); var noAreaRecord = noArea.Records().Single(item => EntityReferences.EventType(item)?.Name == "ACTION_ADDCREATURE");
        var empty = new ScenarioParameterWindow(noArea, noAreaRecord, () => true, () => true); Expect(((Button)empty.FindName("ScenarioPageApply")).IsEnabled && empty.ApplyDraft(), "Spawn incorrectly required an existing area."); empty.Close();
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} spawn parent: all source named choices ({applied}), 64 private flag combinations, disabled/unknown bits, area/canvas coordinates, one Undo, translation, Cancel and lifetime guards");
    }
}
