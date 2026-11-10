using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckVariableScope(string language, string directory)
    {
        var root = Path.Combine(directory, "variable-scope-" + language);
        var baseline = NativeMapDocument.CreateNewMap(new("ScopeUI", "ScopeUI", 20, 20, NativeFormat: true));
        var seed = NativeMapDocument.CreateNewMap(new("SeedUI", "SeedUI", 20, 20, NativeFormat: true));
        var room = new MapRuleKey(MapRuleKind.Rooms, 3, MapRules.Elements(MapRuleKind.Rooms)[0].ID);
        seed.SetMapRules([new(room, 4), new(new(MapRuleKind.Alliance, 3, 4), 1)]); seed.SetMapVariables([new(5, -55)]);
        var sharedBytes = seed.Files.Single(file => file.Signature == 0xE6).Data.ToArray();
        var path = baseline.SaveCopy(root);
        var sharedPath = Path.Combine(root, "Data", "Editor", "GlobalVariables.kwd"); File.WriteAllBytes(sharedPath, sharedBytes);
        var loaded = NativeMapDocument.Load(path);
        foreach (var document in new[] { loaded, NativeMapDocument.CreateNewMap(new("ScopeUI", "ScopeUI", 20, 20, NativeFormat: true), loaded) })
        {
            var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace, "WorkspaceTabs");
            void Layout(int tab) { tabs.SelectedIndex = tab; workspace.Measure(new Size(1600, 1050)); workspace.Arrange(new Rect(0, 0, 1600, 1050)); workspace.UpdateLayout(); }
            Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            void Exact(Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Scoped UI changed unexpected bytes."); }
            void SharedExact() => Expect(document.Files.Single(file => Path.GetFileName(file.ExportPath) == "GlobalVariables.kwd").Data.Span.SequenceEqual(sharedBytes), "Scoped UI changed shared game data.");
            var before = Snapshot();
            Layout(5); var rules = Control<DataGrid>(workspace, "RuleTable"); var rulePanel = Control<StackPanel>(workspace, "RulePanel");
            var picker = Control<ComboBox>(workspace, "RuleValuePicker"); var ruleConfirmed = Control<CheckBox>(workspace, "RuleConfirmed");
            Expect(document.MapRuleCells().Single(cell => cell.Key == room) is { Value: 3, RecordCount: 1 }, "Shared room state leaked into map table.");
            rules.SelectedCells.Add(new(rules.Items[0], rules.Columns[3]));
            var cellText = (TextBlock)rules.Columns[3].GetCellContent(rules.Items[0]);
            Expect(cellText.Text == MapRules.Availability.Single(choice => choice.Value == "3").Name(language), "Bound room text includes a shared state/duplicate.");
            picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == "2");
            Click(rulePanel, "Editor.RuleApply"); SharedExact();
            Expect(document.MapRuleCells().Single(cell => cell.Key == room) is { Value: 2, RecordCount: 1 }, "UI rule edit targeted shared/duplicate records.");
            workspace.Undo(); Exact(before);
            ruleConfirmed.IsChecked = true; Click(rulePanel, "Editor.RuleClear"); SharedExact();
            Expect(document.MapRuleCells().Single(cell => cell.Key == room) is { Value: null, RecordCount: 0 }, "UI clear inherited shared room state.");
            workspace.Undo(); Exact(before); ruleConfirmed.IsChecked = true; Click(rulePanel, "Editor.RuleResetTable"); SharedExact();
            Expect(document.MapRuleCells().Where(cell => cell.Key.Kind == MapRuleKind.Rooms && cell.Key.Player >= 3).All(cell => cell.Value == 3), "Table reset did not set source defaults for every keeper.");
            workspace.Undo(); Exact(before);
            Layout(6); var variables = Control<DataGrid>(workspace, "VariableTable"); var variablePanel = Control<StackPanel>(workspace, "VariablePanel");
            var number = Control<TextBox>(workspace, "VariableNumber"); var confirmed = Control<CheckBox>(workspace, "VariableConfirmed");
            void SelectVariable()
            { variables.SelectedItems.Clear(); variables.SelectedItems.Add(variables.Items.Cast<object>().Single(row => Get<MapVariableCell>(row, "Cell").Definition.ID == 5)); }
            SelectVariable(); Expect(Get<MapVariableCell>(variables.SelectedItem, "Cell") is { Bits: null, RecordCount: 0 }, "Shared scalar value leaked into UI.");
            number.Text = "17"; Click(variablePanel, "Editor.VariableApply"); SharedExact();
            Expect(document.MapVariableCells().Single(cell => cell.Definition.ID == 5) is { Bits: 17, RecordCount: 1 }, "UI variable insertion targeted wrong collection.");
            var after = Snapshot(); workspace.Undo(); Exact(before); workspace.Redo(); Exact(after);
            confirmed.IsChecked = true; Click(variablePanel, "Editor.VariableClear"); Exact(before); SharedExact(); workspace.Undo(); Exact(after);
            confirmed.IsChecked = true; Click(variablePanel, "Editor.VariableResetAll"); Exact(before); SharedExact(); workspace.Undo(); Exact(after);
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout(6); SharedExact(); Exact(after);
            LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); workspace.Document = null;
        }
        // Retarget the same real controls to a document with no per-map file.
        var mapPath = Path.Combine(root, "Data", "Editor", "Maps", "ScopeUIVariables.kld"); File.Move(mapPath, mapPath + ".unreferenced");
        var missing = NativeMapDocument.Load(path); var missingBefore = missing.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var emptyWorkspace = new EditorWorkspace { Document = missing }; var emptyTabs = Control<TabControl>(emptyWorkspace, "WorkspaceTabs");
        void MissingLayout(int index) { emptyTabs.SelectedIndex = index; emptyWorkspace.Measure(new Size(1600, 1050)); emptyWorkspace.Arrange(new Rect(0, 0, 1600, 1050)); emptyWorkspace.UpdateLayout(); }
        MissingLayout(5); var table = Control<DataGrid>(emptyWorkspace, "RuleTable"); var panel = Control<StackPanel>(emptyWorkspace, "RulePanel");
        var kinds = Control<ComboBox>(emptyWorkspace, "RuleKindPicker"); var consent = Control<CheckBox>(emptyWorkspace, "RuleConfirmed");
        foreach (var kind in Enum.GetValues<MapRuleKind>())
        {
            kinds.SelectedValue = (int)kind; MissingLayout(5); table.SelectedCells.Add(new(table.Items[0], table.Columns[3]));
            var text = (TextBlock)table.Columns[3].GetCellContent(table.Items[0]);
            Expect(text.Text.Contains(LocalizationService.Get("Editor.RuleReadOnly")), "Missing collection was not labelled read-only.");
            Click(panel, "Editor.RuleApply"); consent.IsChecked = true; Click(panel, "Editor.RuleClear"); consent.IsChecked = true; Click(panel, "Editor.RuleResetTable");
        }
        MissingLayout(6); var variableTable = Control<DataGrid>(emptyWorkspace, "VariableTable");
        variableTable.SelectedItems.Add(variableTable.Items.Cast<object>().Single(row => Get<MapVariableCell>(row, "Cell").Definition.ID == 5));
        var variableControls = Control<StackPanel>(emptyWorkspace, "VariablePanel"); var variableConsent = Control<CheckBox>(emptyWorkspace, "VariableConfirmed");
        Control<TextBox>(emptyWorkspace, "VariableNumber").Text = "17"; Click(variableControls, "Editor.VariableApply");
        foreach (var action in new[] { "Editor.VariableClear", "Editor.VariableReset", "Editor.VariableResetAll" }) { variableConsent.IsChecked = true; Click(variableControls, action); }
        foreach (var file in missing.Files) Expect(file.Data.Span.SequenceEqual(missingBefore[file]), "Missing-collection UI changed shared/other buffers.");
        Expect(!missing.CanUndo && !missing.HasChanges, "Rejected missing-collection commands created history.");
        emptyWorkspace.Document = null; Expect(File.ReadAllBytes(sharedPath).AsSpan().SequenceEqual(sharedBytes), "UI tests changed original shared file.");
        Console.WriteLine($"PASS {language} map/shared rules and variables, both reference orders and missing-file UI guards");
    }
}
