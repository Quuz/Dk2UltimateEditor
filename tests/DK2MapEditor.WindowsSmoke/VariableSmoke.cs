using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckVariables(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document }; Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 6;
        void Layout() { workspace.Measure(new Size(1600, 1050)); workspace.Arrange(new Rect(0, 0, 1600, 1050)); workspace.UpdateLayout(); }
        Layout();
        var table = Control<DataGrid>(workspace, "VariableTable"); var groups = Control<ComboBox>(workspace, "VariableGroup");
        var search = Control<TextBox>(workspace, "VariableSearch"); var number = Control<TextBox>(workspace, "VariableNumber");
        var confirmed = Control<CheckBox>(workspace, "VariableConfirmed"); var status = Control<TextBlock>(workspace, "VariableStatus");
        var panel = Control<StackPanel>(workspace, "VariablePanel"); var canvas = Control<MapCanvas>(workspace, "Canvas");
        MapVariableCell Cell(object row) => Get<MapVariableCell>(row, "Cell");
        MapVariableCell Value(uint id) => document.MapVariableCells().Single(cell => cell.Definition.ID == id);
        void Select(params uint[] ids)
        {
            table.SelectedItems.Clear(); foreach (var id in ids) table.SelectedItems.Add(table.Items.Cast<object>().Single(row => Cell(row).Definition.ID == id));
        }
        void Group(string value) => groups.SelectedItem = groups.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == value);
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Variable UI changed unexpected buffers."); }
        Expect(table.Items.Count == 193 && table.Columns.Count == 7 && groups.Items.Count == 21 && table.IsReadOnly && table.SelectionMode == DataGridSelectionMode.Extended, "Variable table/group/batch controls incomplete.");
        var before = Snapshot();
        var content = table.Columns[3].GetCellContent(table.Items[0]) as TextBlock;
        Expect(content is { Text.Length: > 0 } && content.Text == (document.IsLegacyProject ? "825" : "—"), "Actual variable value binding is blank or infers game defaults."); Exact(before);
        var name = table.Columns[2].GetCellContent(table.Items[0]) as TextBlock;
        Expect(name?.Text == MapVariables.Definitions[0].Name.Name(language), "Actual variable name is not translated.");
        Expect(Control<Button>(workspace, "VariableClear").IsEnabled == !document.IsLegacyProject, "Project array offers native clear command.");
        canvas.SetSelection(new(1, 1, 2, 2)); workspace.Copy(); var buffer = workspace.BufferedSelection; var copy = SelectionFile.Serialize(buffer!); var selection = canvas.Selection;
        Select(5, 6); number.Text = "-321"; Click(panel, "Editor.VariableApply");
        Expect(Value(5).Bits == unchecked((uint)-321) && Value(6).Bits == unchecked((uint)-321) && table.SelectedItems.Count == 2 && status.Text == LocalizationService.Get("Editor.VariableApplied"), "Batch variable apply used wrong array/key/sign or lost selection.");
        var after = Snapshot(); workspace.Undo(); Exact(before); workspace.Redo(); Exact(after);
        number.Text = "-500001"; Click(panel, "Editor.VariableApply"); Exact(after);
        Expect(status.Text == LocalizationService.Get("Editor.VariableFailed") && status.ToolTip is string, "Out-of-range error not inline.");
        number.Text = "2147483648"; Click(panel, "Editor.VariableApply"); Exact(after);
        number.Text = "not a number"; Click(panel, "Editor.VariableApply"); Exact(after);
        Select(5, 0x7E); number.Text = "11"; Click(panel, "Editor.VariableApply"); Exact(after);
        Expect(Value(5).Bits == unchecked((uint)-321), "Mixed-range batch partially changed first row.");
        Select(5, 6); Click(panel, "Editor.VariableReset"); Exact(after);
        confirmed.IsChecked = true; Click(panel, "Editor.VariableReset");
        Expect(Value(5).Bits == (document.IsLegacyProject ? unchecked((uint)-140) : null) && Value(6).Bits == (document.IsLegacyProject ? 125u : null) && confirmed.IsChecked != true, "Confirmed source reset/override removal failed.");
        workspace.Undo(); Exact(after);
        if (!document.IsLegacyProject)
        {
            confirmed.IsChecked = true; Click(panel, "Editor.VariableClear"); Expect(Value(5).Bits is null && Value(6).Bits is null, "Native variable clear failed."); workspace.Undo(); Exact(after);
        }
        Select(0x9E); number.Text = "1"; Click(panel, "Editor.VariableApply"); Exact(after);
        Expect(!Value(0x9E).Editable && status.Text == LocalizationService.Get("Editor.VariableFailed"), "Source display-only float became editable.");
        Group("GP"); Layout(); Expect(table.Items.Count == 8 && table.Items.Cast<object>().All(row => Cell(row).Definition.Group.Value == "GP"), "Group filter selected wrong source variables.");
        var fractional = table.Items.Cast<object>().Single(row => Cell(row).Definition.ID == 0x9E);
        Expect(Get<string>(fractional, "Default") == "1.19995117" && Get<string>(fractional, "Range") == "20.12", "Fractional display uses wrong format/default.");
        search.Text = "jackpot"; Expect(table.Items.Count == 0, "Search ignored variable names and group constraints.");
        search.Text = "Speed"; Expect(table.Items.Count == 2, "Bilingual search should match boulder speed and speed/health ratio.");
        Group("*"); search.Text = "claimed tile"; Expect(table.Items.Count == 1 && Cell(table.Items[0]).Definition.ID == 4, "English search does not work in both locales.");
        search.Text = "прочность захваченной"; Expect(table.Items.Count == 1 && Cell(table.Items[0]).Definition.ID == 4, "Russian search does not work in both locales.");
        search.Text = ""; Select(5, 6); number.Text = "17"; confirmed.IsChecked = true;
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(table.SelectedItems.Cast<object>().Select(row => Cell(row).Definition.ID).ToHashSet().SetEquals(new uint[] { 5, 6 }) && number.Text == "17" && confirmed.IsChecked == true
            && table.Columns[2].Header as string == LocalizationService.Get("Editor.VariableName") && Get<string>(table.Items[0], "Name") == MapVariables.Definitions[0].Name.Name(LocalizationService.CurrentLanguage), "Translation lost variable selection/input or retained old labels."); Exact(after);
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout(); Click(panel, "Editor.VariableApply");
        Expect(Value(5).Bits == 17 && Value(6).Bits == 17, "Live translation changed selected variable keys.");
        Select(0x34); number.Text = "255"; Click(panel, "Editor.VariableApply"); Expect(Value(0x34).Bits == 255, "Manual colour boundary rejected.");
        confirmed.IsChecked = true; Click(panel, "Editor.VariableReset"); Expect(Value(0x34).Bits == (document.IsLegacyProject ? 300u : null), "Source reset silently clamped 300 to manual 255 maximum.");
        if (document.IsLegacyProject)
        {
            var info = document.Records().Single(record => record.File.Signature == NativeMapDocument.LegacyInfo);
            document.SetField(info, info.Fields.Single(field => field.Name == "Variables[158]"), "16384");
            Select(0x9E); before = Snapshot(); confirmed.IsChecked = true; Click(panel, "Editor.VariableReset"); Expect(Value(0x9E).Bits == 0x1333, "Project float reset unavailable."); workspace.Undo(); Exact(before);
        }
        search.Text = "claimed tile"; Select(4); number.Text = "999"; Click(panel, "Editor.VariableApply"); before = Snapshot();
        Click(panel, "Editor.VariableResetAll"); Exact(before); confirmed.IsChecked = true; Click(panel, "Editor.VariableResetAll");
        Expect(document.MapVariableCells().Where(cell => cell.Resettable).All(cell => document.IsLegacyProject ? cell.Bits == cell.Definition.DefaultBits : cell.Bits is null), "Reset all was limited to visible filtered rows.");
        workspace.Undo(); Exact(before); workspace.Redo();
        confirmed.IsChecked = true; search.Text = ""; Expect(confirmed.IsChecked != true, "Filter retained destructive confirmation.");
        confirmed.IsChecked = true; Select(6); Expect(confirmed.IsChecked != true, "New selected rows retained destructive confirmation.");
        confirmed.IsChecked = true; document.Paint([new(2, 2)], new(4, 2, 1)); Expect(confirmed.IsChecked != true, "External edit retained variable-reset confirmation."); document.Undo();
        Expect(ReferenceEquals(buffer, workspace.BufferedSelection) && SelectionFile.Serialize(buffer!).AsSpan().SequenceEqual(copy) && canvas.Selection == selection, "Variable edits changed map selection/copy buffer.");
        workspace.Document = null;
        Expect(!panel.IsEnabled && table.Items.Count == 0 && status.Text.Length == 0 && confirmed.IsChecked != true && number.Text == "0" && search.Text == "", "Closed document retained variable editor state.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} named variables, ranges, filtering, defaults and undo");
    }
}
