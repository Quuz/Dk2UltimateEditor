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
    private static void CheckCreatureOptions(NativeMapDocument document, string language)
    {
        GlobalDefinitionRow Row() => document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == 0);
        CreatureOptionsDraft Read(string group) => document.CreateCreatureOptionsDraft(Row(), group);
        document.SetGlobalDefinitionValues(CreatureOptions.Groups.SelectMany(group => CreatureOptions.Fields(group)).Select(field => new GlobalDefinitionChange("Creatures", 0, field, "0")));
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace,"WorkspaceTabs"); tabs.SelectedIndex = 9;
        void Layout() { workspace.Measure(new Size(1500,1000)); workspace.Arrange(new Rect(0,0,1500,1000)); workspace.UpdateLayout(); }
        Layout();
        var family = Control<ComboBox>(workspace,"DefinitionFamily"); var section = Control<ComboBox>(workspace,"DefinitionSection"); var picker = Control<ComboBox>(workspace,"DefinitionPicker");
        var panel = Control<Expander>(workspace,"DefinitionOptionPanel"); var apply = Control<Button>(workspace,"DefinitionOptionApply"); var revert = Control<Button>(workspace,"DefinitionOptionRevert"); var status = Control<TextBlock>(workspace,"DefinitionStatus");
        ComboBox Choice(string field) => Control<ComboBox>(workspace,"DefinitionOption" + field);
        TextBox Box(string field) => Control<TextBox>(workspace,"DefinitionOption" + field);
        Slider Rate(string field)=>Control<Slider>(workspace,"DefinitionOption"+field);
        string RateText(string field)=>Control<TextBlock>(workspace,"DefinitionOption"+field+"Value").Text;
        void Choose(ComboBox combo, string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item,"ID") == id);
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dictionary<NativeFile,byte[]> Snapshot() => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Option UI changed stored bytes before apply/unrelated bytes."); }
        var initial = Snapshot(); Expect(panel.Visibility == Visibility.Collapsed,"Terrain has invented creature option controls.");
        Choose(family,"Creatures"); picker.SelectedIndex = 0; Choose(section,"Resistances"); panel.IsExpanded = true; Layout();
        Expect(panel.Visibility == Visibility.Visible && Control<WrapPanel>(workspace,"DefinitionOptionFields").Children.Count == 8 && Choice("Res1").Items.Count == 33 && !Rate("Res1Rate").IsEnabled,"Four resistance slots/None behavior missing.");
        Expect(panel.Foreground is SolidColorBrush { Color.R: > 200 },"Dark option captions unreadable.");
        var changes = 0; document.Changed += (_,_) => changes++;
        foreach (var id in Enumerable.Range(0,33)) { Choose(Choice("Res1"),id.ToString()); Expect(Rate("Res1Rate").IsEnabled == (id != 0),"Resistance active state wrong."); }
        Expect(Choice("Res1").Items.Cast<object>().All(item => !Get<string>(item,"Label").Contains("sz")),"Source symbols leaked into resistance choices."); Exact(initial); Expect(changes == 0,"Browsing catalogue modified map."); Click(revert);
        Choose(Choice("Res1"),"1"); Rate("Res1Rate").Value = 23; Choose(Choice("Res2"),"15"); Rate("Res2Rate").Value = 77; Choose(Choice("Res2"),"0");
        Choose(Choice("Res3"),"32"); Rate("Res3Rate").Value = 100; Choose(Choice("Res4"),"2"); Rate("Res4Rate").Value = 50;
        Expect(!Rate("Res2Rate").IsEnabled && RateText("Res2Rate") == "77","None erased pending rate.");
        Choose(section,"Possession"); Layout();
        Expect(Control<WrapPanel>(workspace,"DefinitionOptionFields").Children.Count == 14 && Choice("SpecialAb1").Items.Count == 11 && Choice("Melee1").Items.Count == 23 && Choice("GammaEffect").Items.Count == 17 && Choice("CameraRolls").Items.Count == 2,"Possession fields/source catalogues missing.");
        foreach (var field in new[] { "SpecialAb1","SpecialAb2","Melee1","Melee2","Special1","Special2","Spell","GammaEffect" }) foreach (var choice in CreatureOptions.Choices(field)) Choose(Choice(field),choice.Value);
        Click(revert); Choose(Choice("SpecialAb1"),"10"); Box("SpecialAb1Data").Text = "32767"; Choose(Choice("SpecialAb2"),"0"); Box("SpecialAb2Data").Text = "123";
        Choose(Choice("Melee1"),"22"); Choose(Choice("Melee2"),"15"); Choose(Choice("Special1"),"1"); Choose(Choice("Special2"),"2"); Choose(Choice("Spell"),"18"); Choose(Choice("GammaEffect"),"16");
        Box("WaddleScale").Text = "1.000244140625"; Box("OscillateScale").Text = "0,000244140625"; Box("WalkScale").Text = "12"; Choose(Choice("CameraRolls"),"1");
        var label = Get<string>(Choice("SpecialAb1").SelectedItem,"Label"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Get<string>(Choice("SpecialAb1").SelectedItem,"Label") != label && Box("OscillateScale").Text == "0,000244140625" && Box("WalkScale").Text == "12","Language refresh discarded option inputs.");
        Choose(section,"Resistances"); Expect(RateText("Res1Rate") == "23" && RateText("Res2Rate") == "77" && !Rate("Res2Rate").IsEnabled,"Group/language changes lost hidden resistance draft.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Exact(initial); Expect(changes == 0,"Option drafts modified map."); Click(apply); var resistance = Snapshot();
        Expect(changes == 1 && Read("Resistances").Raw("Res1Rate") == "23" && Read("Resistances").Raw("Res2") == "0" && Read("Resistances").Raw("Res2Rate") == "77","Whole resistance group/None not saved atomically.");
        Click(apply); Exact(resistance); Expect(changes == 1 && !revert.IsEnabled,"No-op resistance apply created history.");
        Choose(section,"Possession"); Expect(Box("SpecialAb1Data").Text == "32767" && Box("WaddleScale").Text == "1.000244140625","Applying another group discarded possession draft."); Click(apply); var full = Snapshot();
        Expect(changes == 2 && Read("Possession").Raw("WaddleScale") == "4097" && Read("Possession").Raw("OscillateScale") == "1" && Read("Possession").Raw("WalkScale") == "12" && Read("Possession").Raw("CameraRolls") == "1","Possession controls did not save whole group in source units.");
        Click(apply); Exact(full); Expect(changes == 2 && !revert.IsEnabled,"No-op possession apply created history.");
        Box("SpecialAb1Data").Text = "invalid"; Box("WalkScale").Text = "1"; Click(apply); Exact(full); Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Invalid parameter partly saved other inputs."); Click(revert);
        Box("WalkScale").Text = "invalid"; Click(apply); Exact(full); Click(revert); Box("OscillateScale").Text = "invalid"; Click(apply); Exact(full); Click(revert);
        Choose(section,"Resistances"); Rate("Res4Rate").Value = 101; Rate("Res1Rate").Value = 1; Expect(Rate("Res4Rate").Value==100&&RateText("Res4Rate")=="100","Resistance slider exceeded source range."); Exact(full); Click(revert);
        Choose(section,"Possession"); var unknown = document.IsLegacyProject ? "4294967295" : "255";
        document.SetGlobalDefinitionValues([new("Creatures",0,"SpecialAb1",unknown),new("Creatures",0,"SpecialAb1Data","65535"),new("Creatures",0,"WaddleScale","65535"),new("Creatures",0,"WalkScale","255")]); Layout(); var imported = Snapshot();
        Expect(Get<string>(Choice("SpecialAb1").SelectedItem,"ID") == unknown && Choice("SpecialAb1").Items.Count == 12 && Box("SpecialAb1Data").Text == "65535" && Box("WaddleScale").Text == "15.999755859375","Unknown source widths/parameters replaced.");
        var before = changes; Click(apply); Exact(imported); Expect(changes == before,"Imported no-op normalized values."); Choose(Choice("SpecialAb1"),"1"); Click(apply); Expect(Read("Possession").Raw("SpecialAb1Data") == "65535" && Read("Possession").Raw("WalkScale") == "255","Type repair normalized unrelated out-of-range inputs."); workspace.Undo(); Exact(imported); workspace.Undo(); Exact(full);
        // An actual pending generic edit in a different slot invalidates the entire group.
        Choose(section,"Resistances"); Click(Control<Button>(workspace,"DefinitionProperties")); var grid = Control<DataGrid>(workspace,"PropertyGrid"); Layout();
        var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item,"Field").Name == "Res4Rate"); grid.SelectedItem = property; grid.ScrollIntoView(property,grid.Columns[1]); Layout(); grid.CurrentCell = new(property,grid.Columns[1]);
        Expect(grid.BeginEdit(),"Cannot begin generic resistance edit."); Layout(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "80"; tabs.SelectedIndex = 9; Rate("Res1Rate").Value = 99; Click(apply);
        workspace.Dispatcher.Invoke(() => {},System.Windows.Threading.DispatcherPriority.Background);
        Expect(Read("Resistances").Raw("Res4Rate") == "80" && Read("Resistances").Raw("Res1Rate") == "23" && status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Pending generic edit applied stale resistance group."); workspace.Undo(); Exact(full);
        if (Environment.GetEnvironmentVariable("DK2_OPTIONS_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts);
            foreach (var group in CreatureOptions.Groups)
            {
                Choose(section,group); Layout(); var bitmap = new RenderTargetBitmap(1500,1000,96,96,PixelFormats.Pbgra32); bitmap.Render(workspace); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts,$"options-{group}-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
            }
        }
        foreach (var group in CreatureOptions.Groups)
        {
            Choose(section,group); document.ResetGlobalDefinitions(group,[0]); var defaults = Snapshot(); before = changes;
            if (group == "Resistances") { Choose(Choice("Res1"),"1"); Rate("Res1Rate").Value = 45; } else { Choose(Choice("SpecialAb1"),"10"); Box("SpecialAb1Data").Text = "123"; }
            Control<CheckBox>(workspace,"DefinitionConfirmed").IsChecked = true; Click(Control<Button>(workspace,"DefinitionReset")); Exact(defaults);
            Expect(changes == before && !revert.IsEnabled,"No-op group reset retained an option draft/history."); workspace.Undo(); Exact(full);
        }
        workspace.Undo(); Exact(resistance); workspace.Undo(); Exact(initial);
        workspace.Document = NativeMapDocument.Create(20,20,"Missing"); Choose(family,"Creatures"); Choose(section,"Possession"); Layout();
        Expect(!apply.IsEnabled && !Choice("SpecialAb1").IsEnabled && Choice("SpecialAb1").SelectedItem is null && Box("SpecialAb1Data").Text.Length == 0,"Missing native options inferred from defaults.");
        workspace.Document = null; Layout(); Expect(panel.Visibility == Visibility.Collapsed && Control<WrapPanel>(workspace,"DefinitionOptionFields").Children.Count == 0,"Close retained dynamic option controls.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} creature resistance/possession options: source lists, complete groups, exact units, None, atomic drafts, translation, unknown values, pending commit and no-op reset");
    }
}
