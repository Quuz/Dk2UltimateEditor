using System.Buffers.Binary;
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
    private static NativeMapDocument DefinitionFixture(string directory, string language, bool project)
    {
        if (project) return NativeMapDocument.CreateNewMap(new("DefinitionsUI", "DefinitionsUI", 20, 20));
        var location = Path.Combine(directory, "definitions-" + language);
        var path = NativeMapDocument.Create(20, 20, "DefinitionsUI").SaveCopy(location);
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        foreach (var family in LegacyGlobalSchemas.All)
        {
            var fileName = family.Group switch { "Terrains" => "Terrain", "Spells" => "KeeperSpells", "CrSpells" => "CreatureSpells", _ => family.Group };
            var defaults = File.ReadAllBytes(Path.Combine(root!.FullName, "Binaries", "Globals", "Default" + fileName + ".dkg"));
            var bytes = new byte[56 + defaults.Length];
            void Header(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
            Header(0, family.Signature); Header(4, 4); Header(8, (uint)bytes.Length); Header(12, family.Signature + 1); Header(16, 28); Header(20, (uint)(defaults.Length / family.RecordSize)); Header(48, family.Signature + 2); Header(52, (uint)defaults.Length);
            defaults.CopyTo(bytes, 56); File.WriteAllBytes(Path.Combine(location, "Data", "Editor", fileName + ".kwd"), bytes);
        }
        return NativeMapDocument.Load(path);
    }
    private static void CheckDefinitions(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); tabs.SelectedIndex = 9;
        void Layout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); }
        Layout();
        var family = Control<ComboBox>(workspace, "DefinitionFamily"); var section = Control<ComboBox>(workspace, "DefinitionSection"); var picker = Control<ComboBox>(workspace, "DefinitionPicker"); var table = Control<DataGrid>(workspace, "DefinitionTable");
        var number = Control<TextBox>(workspace, "DefinitionNumber"); var search = Control<TextBox>(workspace, "DefinitionSearch"); var status = Control<TextBlock>(workspace, "DefinitionStatus"); var notice = Control<TextBlock>(workspace, "DefinitionNotice");
        var confirm = Control<CheckBox>(workspace, "DefinitionConfirmed"); var apply = Control<Button>(workspace, "DefinitionApply"); var reset = Control<Button>(workspace, "DefinitionReset"); var all = Control<Button>(workspace, "DefinitionResetAll");
        void Choose(ComboBox combo, string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == id);
        void Pick(int index) => picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<GlobalDefinitionRow>(item, "Row").Index == index);
        void Field(string name) => table.SelectedItem = table.Items.Cast<object>().Single(item => Get<GlobalDefinitionValue>(item, "Field").Name == name);
        string Current(string name) => document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(row => row.Family == Get<string>(family.SelectedItem, "ID") && row.Index == Get<GlobalDefinitionRow>(picker.SelectedItem, "Row").Index)).Single(field => field.Name == name).Value;
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Definition UI changed unrelated bytes or failed exact undo."); }
        var initial = Snapshot(); var selection = new TileRect(2, 2, 3, 3); Control<MapCanvas>(workspace, "Canvas").SetSelection(selection);
        Expect(tabs.Items.Count == 10 && family.Items.Count == 7 && Control<StackPanel>(workspace, "DefinitionPanel").IsEnabled && table.IsReadOnly, "Definition tab not bound.");
        foreach (var (id, count) in new[] { ("Terrains", 38), ("Rooms", 24), ("Doors", 8), ("Traps", 15), ("Spells", 13), ("Creatures", 45), ("CrSpells", 41) })
        {
            Choose(family, id); Layout();
            Expect(picker.Items.Count == count && table.Items.Count > 0 && apply.IsEnabled && status.Text.Length == 0, "Missing definition category/name/fields: " + id + " · " + status.ToolTip);
            Expect(section.Items.Count == (id == "Creatures" ? 8 : 1), "Source reset groups differ.");
            Expect(picker.Items.Cast<object>().All(item => !Get<string>(item, "Label").Contains("sz")), "Technical source symbols leaked instead of translated definition names.");
        }
        Choose(family, "Terrains"); Pick(6); Field("GoldValue"); Layout();
        Expect(number.Text == "3000" && table.Columns[0].ActualWidth >= 480 && table.Columns[1].GetCellContent(table.SelectedItem) is TextBlock { Text: "3000" }, "Actual bound default value/layout missing.");
        Expect(notice.Text == LocalizationService.Get("Editor.Definitions." + (document.IsLegacyProject ? "SourceDefaults" : "Stored")), "Default-backed/stored notice wrong.");
        number.Text = "invalid"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial);
        Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"), "Invalid input failure not visible.");
        number.Text = "444"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(Current("GoldValue") == "444", "Apply button edited another definition.");
        var edited = Snapshot();
        reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(edited);
        confirm.IsChecked = true; reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(Current("GoldValue") == "3000" && confirm.IsChecked == false, "Selected reset/confirmation failed.");
        workspace.Undo(); Exact(edited); workspace.Undo(); Exact(initial); workspace.Redo(); Exact(edited);
        Pick(7); Field("GoldValue"); number.Text = "555"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); var two = Snapshot();
        confirm.IsChecked = true; all.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(Current("GoldValue") != "555", "All-definition reset ignored another row."); workspace.Undo(); Exact(two);
        confirm.IsChecked = true; search.Text = "Gold"; Expect(confirm.IsChecked == false && picker.Items.Count == 1, "Search failed or retained destructive confirmation.");
        search.Text = "Золото"; Expect(picker.Items.Count == 1 && Get<GlobalDefinitionRow>(picker.SelectedItem, "Row").Index == 6, "Opposite-language terrain search failed."); search.Text = "";
        Choose(family, "Creatures"); Choose(section, "Flags"); Pick(0); Layout();
        var flagName = Get<GlobalDefinitionValue>(table.Items[0], "Field").Name; Field(flagName);
        var flag = Control<ComboBox>(workspace, "DefinitionFlag"); var oldFlag = Current(flagName);
        Expect(flag.Visibility == Visibility.Visible && flag.Items.Count == 2, "Flag choices not bound.");
        flag.SelectedValue = oldFlag == "0" ? "1" : "0"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(Current(flagName) != oldFlag, "Named flag choice failed to apply.");
        var flagEdited = Snapshot(); confirm.IsChecked = true; Choose(section, "Light"); Expect(confirm.IsChecked == false, "Changing reset group retained confirmation.");
        Choose(section, "Flags"); Field(flagName); number.Text = "1"; var label = Get<string>(picker.SelectedItem, "Label");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Get<string>(picker.SelectedItem, "Label") != label && Get<string>(section.SelectedItem, "ID") == "Flags" && Get<GlobalDefinitionValue>(table.SelectedItem, "Field").Name == flagName && number.Text == "1", "Translation lost definition/group/field/draft.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout(); Exact(flagEdited);
        var selectedRecord = Get<GlobalDefinitionRow>(picker.SelectedItem, "Row").Record!;
        Control<Button>(workspace, "DefinitionProperties").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var propertiesRecord = Get<NativeRecord>(Control<ListBox>(workspace, "RecordList").SelectedItem, "Record");
        Expect(tabs.SelectedIndex == 1 && propertiesRecord.File == selectedRecord.File && propertiesRecord.Offset == selectedRecord.Offset, "Definition properties navigation chose another record.");
        var propertyGrid = Control<DataGrid>(workspace, "PropertyGrid");
        var healthItem = propertyGrid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == "Health");
        propertyGrid.SelectedItem = healthItem; propertyGrid.ScrollIntoView(healthItem); Layout(); propertyGrid.CurrentCell = new(healthItem, propertyGrid.Columns[1]);
        Expect(propertyGrid.BeginEdit(), "Could not begin actual generic property edit."); Layout();
        var editor = propertyGrid.Columns[1].GetCellContent(healthItem) as TextBox;
        Expect(editor is not null, "Actual generic property editor missing."); editor!.Text = "999"; confirm.IsChecked = true;
        Expect(workspace.CommitFields() && confirm.IsChecked == false, "Detached generic property commit retained global-reset confirmation.");
        workspace.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        workspace.Undo(); Exact(flagEdited); tabs.SelectedIndex = 9;
        confirm.IsChecked = true; document.SetMapVariables([new(0x18, 999)]); Expect(confirm.IsChecked == false, "External change retained reset confirmation."); workspace.Undo(); Exact(flagEdited);
        Expect(Control<MapCanvas>(workspace, "Canvas").Selection == selection, "Definition editing modified map selection.");
        foreach (var sourceGroup in GlobalDefinitionDefaults.Sections)
        {
            search.Text = ""; Choose(family, sourceGroup.Family); Pick(0); Choose(section, sourceGroup.Name);
            document.ResetGlobalDefinitions(sourceGroup.Name, [0]); var defaults = Snapshot(); var changes = 0;
            EventHandler changed = (_, _) => changes++; document.Changed += changed;
            try
            {
                number.Text = "unapplied invalid input"; confirm.IsChecked = true; reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(defaults);
                Expect(changes == 0 && number.Text == Get<string>(table.SelectedItem, "Value") && confirm.IsChecked == false, "No-op selected reset retained a numeric draft/history: " + sourceGroup.Name);
                number.Text = "another unapplied input"; confirm.IsChecked = true; all.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Expect(number.Text == Get<string>(table.SelectedItem, "Value") && confirm.IsChecked == false, "Full-family reset retained a numeric draft: " + sourceGroup.Name);
            }
            finally { document.Changed -= changed; }
        }
        if (Environment.GetEnvironmentVariable("DK2_DEFINITION_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts);
            foreach (var (category, group) in new[] { ("Terrains", "Terrains"), ("Creatures", "Attributes"), ("Creatures", "Flags"), ("CrSpells", "CrSpells") })
            {
                Choose(family, category); Choose(section, group); Layout();
                var bitmap = new RenderTargetBitmap(1500, 1000, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"definitions-{group}-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
            }
        }
        workspace.Document = NativeMapDocument.Create(20, 20, "Missing"); Layout();
        Expect(!apply.IsEnabled && !reset.IsEnabled && !all.IsEnabled && notice.Text == LocalizationService.Get("Editor.Definitions.ReadOnly") && table.Items.Cast<object>().All(item => Get<string>(item, "Value") == "—"), "Missing native definitions invented current values/editable controls.");
        workspace.Document = null; Layout();
        Expect(!Control<StackPanel>(workspace, "DefinitionPanel").IsEnabled && picker.Items.Count == 0 && table.Items.Count == 0 && number.Text.Length == 0, "Closed document retained editable definitions.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} definition families, groups, apply/flags/default reset, confirmations, language/drafts and unknown guards");
    }
}
