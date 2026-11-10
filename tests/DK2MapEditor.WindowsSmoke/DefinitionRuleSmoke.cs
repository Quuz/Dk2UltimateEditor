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
    private static void CheckDefinitionRules(NativeMapDocument document, string language)
    {
        GlobalDefinitionRow Row(string family) => document.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == 0);
        GlobalRulesDraft Read(string family) => document.CreateGlobalRulesDraft(Row(family));
        document.SetGlobalDefinitionValues(GlobalDefinitionRules.Families.SelectMany(family => GlobalDefinitionRules.Fields(family).Select(field => new GlobalDefinitionChange(family,0,field,GlobalDefinitionRules.Choices(family,field).FirstOrDefault()?.Value ?? "0"))));
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace,"WorkspaceTabs"); tabs.SelectedIndex = 9;
        void Layout() { workspace.Measure(new Size(1500,1000)); workspace.Arrange(new Rect(0,0,1500,1000)); workspace.UpdateLayout(); }
        Layout();
        var family = Control<ComboBox>(workspace,"DefinitionFamily"); var picker = Control<ComboBox>(workspace,"DefinitionPicker");
        var panel = Control<Expander>(workspace,"DefinitionRulePanel"); var apply = Control<Button>(workspace,"DefinitionRuleApply"); var revert = Control<Button>(workspace,"DefinitionRuleRevert"); var status = Control<TextBlock>(workspace,"DefinitionStatus");
        ComboBox Choice(string field) => Control<ComboBox>(workspace,"DefinitionRule" + field);
        TextBox Data() => Control<TextBox>(workspace,"DefinitionRuleTriggerData");
        void Choose(ComboBox combo,string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item,"ID") == id);
        void Select(string name) { Choose(family,name); picker.SelectedIndex = 0; Layout(); }
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dictionary<NativeFile,byte[]> Snapshot() => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Named-rule UI changed unrelated/pending bytes."); }
        var initial = Snapshot(); Expect(panel.Visibility == Visibility.Collapsed,"Terrain has invented material/cast rules."); var changes = 0; document.Changed += (_,_) => changes++;
        foreach (var name in GlobalDefinitionRules.Families)
        {
            Select(name); panel.IsExpanded = true;
            Expect(panel.Visibility == Visibility.Visible && Control<WrapPanel>(workspace,"DefinitionRuleFields").Children.Count == GlobalDefinitionRules.Fields(name).Count && panel.Foreground is SolidColorBrush { Color.R: > 200 },"Named-rule panel missing/unreadable.");
            foreach (var field in GlobalDefinitionRules.Fields(name).Where(field => field != "TriggerData"))
            {
                var choice = Choice(field); Expect(choice.Items.Count == GlobalDefinitionRules.Choices(name,field).Count,"Source rule count wrong.");
                foreach (var option in GlobalDefinitionRules.Choices(name,field)) Choose(choice,option.Value);
                Expect(choice.Items.Cast<object>().All(item => !Get<string>(item,"Label").Contains("sz")),"Rule source symbols leaked into choices.");
            }
            Exact(initial); Click(revert);
        }
        Expect(changes == 0,"Browsing named rules wrote map data.");
        Select("Doors"); Choose(Choice("Material"),"7"); Choose(Choice("Crate"),"57"); Choose(Choice("Key"),"137");
        Select("Traps"); Choose(Choice("Material"),"3"); Choose(Choice("Crate"),"138"); Data().Text = "25";
        foreach (var id in new[] { "1","2","3" })
        {
            Choose(Choice("TriggerType"),id);
            Expect(Data().Text == "25" && Data().IsEnabled && ((TextBlock)((StackPanel)Data().Parent).Children[0]).Text == LocalizationService.Get("Editor.Definitions.Rules.Trigger." + id),"Trigger selection lost data/context caption.");
        }
        Data().Text = "32767";
        Select("Spells"); Choose(Choice("HandAnim"),"1"); Choose(Choice("NoGoHandAnim"),"1"); Choose(Choice("CastRule"),"5"); Choose(Choice("TargetRule"),"6");
        var label = Get<string>(Choice("TargetRule").SelectedItem,"Label"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Get<string>(Choice("TargetRule").SelectedItem,"Label") != label && Get<string>(Choice("CastRule").SelectedItem,"ID") == "5","Language refresh lost named-rule input.");
        Select("Doors"); Expect(Get<string>(Choice("Material").SelectedItem,"ID") == "7" && Get<string>(Choice("Key").SelectedItem,"ID") == "137","Family/language changes lost hidden door draft.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Exact(initial); Click(apply); var door = Snapshot(); Expect(changes == 1 && Read("Doors").Raw("Key") == "137" && Read("Doors").Raw("Crate") == "57","Door named rules not applied together."); Click(apply); Exact(door); Expect(changes == 1 && !revert.IsEnabled,"No-op rule apply created history.");
        Select("Traps"); Expect(Data().Text == "32767" && Get<string>(Choice("TriggerType").SelectedItem,"ID") == "3","Applying door rules lost trap draft."); Click(apply); var trap = Snapshot(); Expect(changes == 2 && Read("Traps").Raw("TriggerData") == "32767" && Read("Traps").Raw("Crate") == "138","Trigger parameter not applied with rule group.");
        Select("Spells"); Expect(Get<string>(Choice("HandAnim").SelectedItem,"ID") == "1" && Get<string>(Choice("TargetRule").SelectedItem,"ID") == "6","Other-family apply lost spell draft."); Click(apply); var full = Snapshot(); Expect(changes == 3 && Read("Spells").Raw("HandAnim") == "1" && Read("Spells").Raw("NoGoHandAnim") == "1" && Read("Spells").Raw("CastRule") == "5" && Read("Spells").Raw("TargetRule") == "6","Spell rules not applied together."); Click(apply); Exact(full); Expect(changes == 3,"Spell no-op added history.");
        Select("Traps"); Data().Text = "32768"; Choose(Choice("Material"),"7"); Click(apply); Exact(full); Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Invalid trigger partly saved another rule."); Click(revert); Data().Text = "1.5"; Click(apply); Exact(full); Click(revert);
        var unknown = document.IsLegacyProject ? "4294967295" : "255";
        document.SetGlobalDefinitionValues([new("Traps",0,"TriggerType",unknown),new("Traps",0,"TriggerData","65535")]); Layout(); var imported = Snapshot();
        Expect(Get<string>(Choice("TriggerType").SelectedItem,"ID") == unknown && Choice("TriggerType").Items.Count == 4 && !Data().IsEnabled && Data().Text == "65535","Unknown trigger guessed editable/cleared data."); var before = changes; Click(apply); Exact(imported); Expect(changes == before,"Unknown trigger no-op normalized data.");
        Choose(Choice("TriggerType"),"1"); Expect(Data().IsEnabled && Data().Text == "65535","Trigger repair cleared parameter."); Click(apply); Expect(Read("Traps").Raw("TriggerData") == "65535","Explicit type repair normalized imported parameter."); workspace.Undo(); Exact(imported); workspace.Undo(); Exact(full);
        Select("Spells"); document.SetGlobalDefinitionValues([new("Spells",0,"CastRule","0"),new("Spells",0,"TargetRule",unknown)]); Layout(); imported = Snapshot();
        Expect(Choice("CastRule").Items.Count == 6 && Get<string>(Choice("CastRule").SelectedItem,"ID") == "0" && Get<string>(Choice("CastRule").SelectedItem,"Label").Contains(LocalizationService.Get("Editor.Definitions.Rules.Unknown")) && Choice("TargetRule").Items.Count == 7,"Imported zero replaced by an invented cast None/source sentinel."); Click(apply); Exact(imported); workspace.Undo(); Exact(full);
        Select("Traps"); Click(Control<Button>(workspace,"DefinitionProperties")); var grid = Control<DataGrid>(workspace,"PropertyGrid"); Layout();
        var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item,"Field").Name == "Crate"); grid.SelectedItem = property; grid.ScrollIntoView(property,grid.Columns[1]); Layout(); grid.CurrentCell = new(property,grid.Columns[1]);
        Expect(grid.BeginEdit(),"Cannot begin generic named-rule edit."); Layout(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "5"; tabs.SelectedIndex = 9; Data().Text = "23"; Click(apply); workspace.Dispatcher.Invoke(() => {},System.Windows.Threading.DispatcherPriority.Background);
        Expect(Read("Traps").Raw("Crate") == "5" && Read("Traps").Raw("TriggerData") == "32767" && status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Pending generic edit applied stale named-rule draft."); workspace.Undo(); Exact(full);
        if (Environment.GetEnvironmentVariable("DK2_RULES_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts);
            foreach (var name in GlobalDefinitionRules.Families)
            {
                Select(name); Layout(); var bitmap = new RenderTargetBitmap(1500,1000,96,96,PixelFormats.Pbgra32); bitmap.Render(workspace); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts,$"rules-{name}-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
            }
        }
        foreach (var name in GlobalDefinitionRules.Families)
        {
            Select(name); document.ResetGlobalDefinitions(name,[0]); var defaults = Snapshot(); before = changes;
            Choose(Choice(GlobalDefinitionRules.Fields(name)[0]),"1"); Control<TextBox>(workspace,"DefinitionNumber").Text = "unapplied input";
            Control<CheckBox>(workspace,"DefinitionConfirmed").IsChecked = true; Click(Control<Button>(workspace,"DefinitionReset")); Exact(defaults);
            Expect(changes == before && !revert.IsEnabled && Get<string>(Choice(GlobalDefinitionRules.Fields(name)[0]).SelectedItem,"ID") == Read(name).Raw(GlobalDefinitionRules.Fields(name)[0]),"No-op named-rule reset retained draft/history."); workspace.Undo(); Exact(full);
        }
        workspace.Undo(); Exact(trap); workspace.Undo(); Exact(door); workspace.Undo(); Exact(initial);
        workspace.Document = NativeMapDocument.Create(20,20,"Missing"); Select("Traps"); Layout(); Expect(!apply.IsEnabled && !Choice("TriggerType").IsEnabled && Choice("TriggerType").SelectedItem is null && Data().Text.Length == 0,"Missing native rules inferred from defaults.");
        workspace.Document = null; Layout(); Expect(panel.Visibility == Visibility.Collapsed && Control<WrapPanel>(workspace,"DefinitionRuleFields").Children.Count == 0,"Close retained registered rule controls.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} named door/trap/keeper-spell rules: eight source lists, trigger captions/data, family drafts, atomic apply, unknown imports, translation, pending commit and no-op reset");
    }
}
