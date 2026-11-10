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
    private static void CheckDefinitionNumbers(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document }; Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9;
        void Layout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); }
        Layout();
        var family = Control<ComboBox>(workspace, "DefinitionFamily"); var section = Control<ComboBox>(workspace, "DefinitionSection"); var picker = Control<ComboBox>(workspace, "DefinitionPicker"); var table = Control<DataGrid>(workspace, "DefinitionTable");
        var number = Control<TextBox>(workspace, "DefinitionNumber"); var raw = Control<CheckBox>(workspace, "DefinitionRaw"); var confirm = Control<CheckBox>(workspace, "DefinitionConfirmed"); var apply = Control<Button>(workspace, "DefinitionApply"); var status = Control<TextBlock>(workspace, "DefinitionStatus"); var hint = Control<TextBlock>(workspace, "DefinitionFormat");
        void Choose(ComboBox combo, string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == id);
        void Field(string name) => table.SelectedItem = table.Items.Cast<object>().Single(item => Get<GlobalDefinitionValue>(item, "Field").Name == name);
        string Current(string name) => document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == 0)).Single(field => field.Name == name).Value;
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Numeric UI changed unrelated bytes/undo."); }
        Choose(family, "Creatures"); Choose(section, "Attributes"); picker.SelectedIndex = 0; Field("Speed"); Layout();
        var initial = Snapshot(); var events = 0; document.Changed += (_, _) => events++;
        Expect(raw.IsChecked != true && table.Columns.Count == 4 && number.Text == GlobalDefinitionNumbers.Display("Creatures", "Speed", Current("Speed")), "Ordinary units were not default.");
        Expect(table.Columns[3].GetCellContent(table.SelectedItem) is TextBlock text && text.Text == Current("Speed") && hint.Text.Contains("4096"), "Raw column/unit hint absent from actual bound control.");
        apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial); Expect(events == 0, "Unchanged UI value materialized defaults/history.");
        number.Text = "1,5"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Layout();
        Expect(Current("Speed") == "6144" && status.Text == LocalizationService.Get("Editor.Definitions.Applied") && table.Columns[1].GetCellContent(table.SelectedItem) is TextBlock { Text: "1.5" }, "Comma input failed to edit/display source units.");
        var edited = Snapshot(); confirm.IsChecked = true; raw.IsChecked = true; Layout();
        Expect(!confirm.IsChecked!.Value && number.Text == "6144" && hint.Text == LocalizationService.Get("Editor.Definitions.RawHint"), "Raw toggle retained confirmation or reinterpreted draft."); Exact(edited);
        number.Text = "6145"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(Current("Speed") == "6145", "Raw mode no longer edits exact integers.");
        raw.IsChecked = false; Layout(); Expect(number.Text == "1.500244140625", "Mode switch rounded one native bit.");
        var exactFraction = Snapshot(); var before = events; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(exactFraction); Expect(events == before, "Exact fractional UI reapply changed bits/history.");
        number.Text = "2,25"; var oldLabel = Get<string>(picker.SelectedItem, "Label");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(number.Text == "2,25" && raw.IsChecked != true && Get<string>(picker.SelectedItem, "Label") != oldLabel && hint.Text.StartsWith(LocalizationService.Get("Editor.Definitions.FixedHint")), "Translation lost mode/draft/units.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout(); Exact(exactFraction);
        number.Text = document.IsLegacyProject ? "32768" : "16"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(exactFraction);
        Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"), "Source/native width failure not visible.");
        workspace.Undo(); Exact(edited); workspace.Undo(); Exact(initial);
        Field("TortureHealthCng"); Layout(); var old = Current("TortureHealthCng");
        Expect(number.Text == GlobalDefinitionNumbers.Display("Creatures", "TortureHealthCng", old), "Signed native/default display differs.");
        before = events; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial); Expect(before == events, "Signed default alias was normalized.");
        number.Text = "-9"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(Current("TortureHealthCng") is "-9" or "65527", "Signed input failed.");
        Field("Health"); number.Text = "1.5"; var signed = Snapshot(); apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(signed);
        Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"), "Integer field accepted a fraction.");
        Choose(section, "Resistances"); Field("Res1Rate"); Expect(hint.Text.StartsWith(LocalizationService.Get("Editor.Definitions.PercentHint")), "Resistance percentage treated as fixed-point.");
        number.Text = "101"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(signed);
        Choose(section, "Attributes"); Field("Speed"); number.Text = "0.000244140625"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(Current("Speed") == "1", "Smallest fractional step lost through real apply button."); Layout();
        if (Environment.GetEnvironmentVariable("DK2_DEFINITION_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts);
            foreach (var mode in new[] { false, true })
            {
                raw.IsChecked = mode; Layout(); var bitmap = new RenderTargetBitmap(1500, 1000, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"units-{language}-{(document.IsLegacyProject ? "project" : "native")}-{(mode ? "raw" : "ordinary")}.png")); encoder.Save(output);
            }
        }
        raw.IsChecked = false; workspace.Document = NativeMapDocument.Create(20, 20, "Missing"); Choose(family, "Creatures"); Choose(section, "Flags"); Layout();
        Expect(!apply.IsEnabled && !Control<ComboBox>(workspace, "DefinitionFlag").IsEnabled && number.Text.Length == 0, "Missing layout enabled numeric/flag input.");
        workspace.Document = null; Layout(); Expect(hint.Text.Length == 0 && table.Items.Count == 0, "Close retained numeric format/current fields.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} definition ordinary/raw units, exact fractions, signed aliases, source/native limits and live translation");
    }
}
