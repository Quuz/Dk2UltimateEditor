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
    private static void CheckCreatureAttacks(NativeMapDocument document,string language)
    {
        GlobalDefinitionRow Row() => document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == 0);
        CreatureAttacksDraft Read() => document.CreateCreatureAttacksDraft(Row());
        var clear = Read(); foreach (var id in CreatureAttacks.Slots) clear = document.UpdateCreatureAttack(clear,id,id == "Melee" ? "1" : "0",CreatureAttacks.Parameters(id).Select(field => new CreatureAttackInput(field,"0"))); document.ApplyCreatureAttacks(clear);
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace,"WorkspaceTabs"); tabs.SelectedIndex = 9;
        void Layout() { workspace.Measure(new Size(1500,1000)); workspace.Arrange(new Rect(0,0,1500,1000)); workspace.UpdateLayout(); }
        Layout();
        var family = Control<ComboBox>(workspace,"DefinitionFamily"); var section = Control<ComboBox>(workspace,"DefinitionSection"); var picker = Control<ComboBox>(workspace,"DefinitionPicker");
        var panel = Control<Expander>(workspace,"DefinitionAttackPanel"); var slot = Control<ComboBox>(workspace,"DefinitionAttackSlot"); var type = Control<ComboBox>(workspace,"DefinitionAttackType");
        var apply = Control<Button>(workspace,"DefinitionAttackApply"); var revert = Control<Button>(workspace,"DefinitionAttackRevert"); var status = Control<TextBlock>(workspace,"DefinitionStatus"); var hint = Control<TextBlock>(workspace,"DefinitionAttackHint");
        TextBox Box(string field) => Control<TextBox>(workspace,"DefinitionAttack" + field);
        ComboBox Level(string id) => Control<ComboBox>(workspace,"DefinitionAttack" + id + "Level");
        void Choose(ComboBox combo,string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item,"ID") == id);
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dictionary<NativeFile,byte[]> Snapshot() => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Attack UI changed stored bytes before apply/unrelated bytes."); }
        var initial = Snapshot(); Expect(panel.Visibility == Visibility.Collapsed,"Terrain has invented attack controls.");
        Choose(family,"Creatures"); picker.SelectedIndex = 0; Choose(section,"Attacks"); panel.IsExpanded = true; Layout();
        Expect(panel.Visibility == Visibility.Visible && slot.Items.Count == 4 && type.Items.Count == 42 && apply.IsEnabled && Level("Shot1").Items.Count == 11 && !Level("Shot1").IsEnabled && !Box("Shot1Delay").IsEnabled,"Source slots/spells/levels/None disabling missing.");
        Expect(panel.Foreground is SolidColorBrush { Color.R: > 200 } && ((StackPanel)Control<WrapPanel>(workspace,"DefinitionAttackParameters").Children[0]).Children[0] is TextBlock { Foreground: SolidColorBrush { Color.R: > 200 } },"Dark attack captions unreadable.");
        var notifications = 0; document.Changed += (_,_) => notifications++;
        foreach (var option in CreatureAttacks.Options("Shot1"))
        {
            Choose(type,option.Name.Value); Expect(Level("Shot1").IsEnabled == (option.ID != 0) && Box("Shot1Delay").IsEnabled == (option.ID != 0),"Wrong active state for creature spell #" + option.ID);
        }
        Exact(initial); Expect(notifications == 0 && type.Items.Cast<object>().All(item => !Get<string>(item,"Label").Contains("sz")),"Browsing attack catalogue modified map/leaked source symbols."); Click(revert);
        Choose(type,"1"); Choose(Level("Shot1"),"10"); Box("Shot1Delay").Text = "1.000244140625"; Box("Shot1X").Text = "-1,25"; Box("Shot1Y").Text = "0.000244140625"; Box("Shot1Z").Text = "2.5";
        Choose(slot,"Shot2"); Choose(type,"2"); Choose(Level("Shot2"),"0"); Box("Shot2Delay").Text = "3.5"; Box("Shot2X").Text = "1"; Box("Shot2Y").Text = "-2"; Box("Shot2Z").Text = "3";
        Choose(slot,"Shot3"); Choose(type,"3"); Choose(Level("Shot3"),"4"); Box("Shot3Delay").Text = "5.25"; Box("Shot3X").Text = "-20.25"; Box("Shot3Y").Text = "-7.75"; Box("Shot3Z").Text = "1";
        Choose(slot,"Melee"); Layout();
        Expect(type.Items.Count == 41 && Get<string>(type.Items[0],"Label").Contains(language == "ru" ? "Клинок" : "Blade") && Get<string>(type.Items[3],"ID") == "15" && Get<string>(type.Items[3],"Label").Contains(language == "ru" ? "Коса" : "Scythe") && Control<WrapPanel>(workspace,"DefinitionAttackParameters").Children.Count == 4,"Contextual melee choices/parameter count wrong.");
        Choose(type,"15"); Box("RechargeTime").Text = "2.25"; Box("Range").Text = "1.125"; Box("Dammage").Text = "32767"; Box("Delay").Text = "0.5";
        Choose(slot,"Shot1"); Expect(Get<string>(type.SelectedItem,"ID") == "1" && Box("Shot1X").Text == "-1,25" && Get<string>(Level("Shot1").SelectedItem,"ID") == "10","Slot switching discarded typed draft.");
        var oldLabel = Get<string>(type.SelectedItem,"Label"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Get<string>(type.SelectedItem,"ID") == "1" && Get<string>(type.SelectedItem,"Label") != oldLabel && Box("Shot1Delay").Text == "1.000244140625" && Box("Shot1X").Text == "-1,25","Translation discarded typed attack inputs.");
        Choose(slot,"Melee"); Expect(Box("Dammage").Text == "32767" && Box("Range").Text == "1.125" && Get<string>(type.SelectedItem,"ID") == "15","Translation discarded a hidden slot's draft.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Choose(slot,"Shot1"); Layout(); Exact(initial); Expect(notifications == 0 && revert.IsEnabled,"Draft changes modified bytes before Apply.");
        Click(apply); var full = Snapshot();
        Expect(notifications == 1 && Read().Raw("Shot1Delay") == "4097" && Read().Raw("Shot1X") == "-5120" && Read().Raw("Shot1Y") == "1" && Read().Raw("Shot1Level") == "10" && Read().Raw("Shot2ID") == "2" && Read().Raw("Shot2Level") == "0" && Read().Raw("Shot3ID") == "3" && Read().Raw("MeleeID") == "15" && Read().Raw("Dammage") == "32767" && Read().Raw("RechargeTime") == "9216","All four attack slots not applied atomically/correctly.");
        Click(apply); Exact(full); Expect(notifications == 1 && !revert.IsEnabled,"Attack no-op apply created history/draft.");
        Box("Shot1Delay").Text = "7.5"; Choose(type,"0"); Expect(!Box("Shot1Delay").IsEnabled && Box("Shot1Delay").Text == "7.5" && hint.Text == LocalizationService.Get("Editor.Definitions.Attack.NoneHint") + " " + LocalizationService.Get("Editor.Definitions.Attack.Draft"),"Choosing None cleared pending parameters or left them enabled.");
        Click(apply); Expect(Read().Raw("Shot1ID") == "0" && Read().Raw("Shot1Delay") == "30720" && Read().Raw("Shot1Level") == "10" && Read().Raw("Shot1X") == "-5120","None apply discarded pending or stored inactive values."); workspace.Undo(); Exact(full);
        // Source BYTE native types and UINT project types are different widths.
        var unknown = document.IsLegacyProject ? "4294967295" : "255"; var importedLevel = document.IsLegacyProject ? "-1" : "255";
        document.SetGlobalDefinitionValues([new("Creatures",0,"Shot1ID",unknown),new("Creatures",0,"Shot1Level",importedLevel),new("Creatures",0,"Shot1Delay","-1")]); Layout();
        Expect(Get<string>(type.SelectedItem,"ID") == unknown && type.Items.Count == 43 && Get<string>(Level("Shot1").SelectedItem,"ID") == importedLevel && Level("Shot1").Items.Count == 12 && Box("Shot1Delay").Text == "1048575.999755859375" && Box("Shot1Delay").IsEnabled,"Unknown spell/level/unsigned fixed-point alias replaced or disabled by guess.");
        var imported = Snapshot(); var before = notifications; Click(apply); Exact(imported); Expect(notifications == before,"Unknown attack no-op normalized bits.");
        Choose(type,"1"); Click(apply); var repaired = Snapshot(); Expect(Read().Raw("Shot1ID") == "1" && Read().Raw("Shot1Level") == importedLevel && Read().Raw("Shot1Delay") == "-1","Explicit type repair normalized imported parameters.");
        Choose(slot,"Shot2"); Box("Shot2Delay").Text = "invalid"; Choose(slot,"Shot1"); Box("Shot1X").Text = "2.0"; Click(apply); Exact(repaired);
        Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Invalid hidden slot partly applied another attack."); Click(revert); Exact(repaired);
        Choose(slot,"Melee"); Box("Dammage").Text = "invalid"; Click(apply); Exact(repaired); Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Source integer damage validation not enforced."); Click(revert); Choose(slot,"Shot1");
        // Commit in another slot must invalidate the complete 23-field draft.
        Click(Control<Button>(workspace,"DefinitionProperties")); var grid = Control<DataGrid>(workspace,"PropertyGrid"); Layout();
        var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item,"Field").Name == "Shot2Delay"); grid.SelectedItem = property; grid.ScrollIntoView(property,grid.Columns[1]); Layout(); grid.CurrentCell = new(property,grid.Columns[1]);
        Expect(grid.BeginEdit(),"Cannot begin actual generic attack property edit."); Layout(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "999";
        tabs.SelectedIndex = 9; Box("Shot1X").Text = "2.0"; Click(apply); workspace.Dispatcher.Invoke(() => {},System.Windows.Threading.DispatcherPriority.Background);
        Expect(Read().Raw("Shot2Delay") == "999" && Read().Raw("Shot1X") == "-5120" && status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Pending commit applied a stale all-slot attack draft."); workspace.Undo(); Exact(repaired); workspace.Undo(); Exact(imported); workspace.Undo(); Exact(full);
        // Reset means reset the dialog too, even if the stored values already
        // equal the source defaults and no new history entry is appropriate.
        document.ResetGlobalDefinitions("Attacks",[0]); var defaults = Snapshot(); var beforeReset = notifications;
        Choose(type,"1"); Box("Shot1Delay").Text = "9.0"; Control<TextBox>(workspace,"DefinitionNumber").Text = "123";
        Control<CheckBox>(workspace,"DefinitionConfirmed").IsChecked = true; Click(Control<Button>(workspace,"DefinitionReset")); Exact(defaults);
        Expect(notifications == beforeReset && Get<string>(type.SelectedItem,"ID") == Read().Raw("Shot1ID") && Box("Shot1Delay").Text == Read().Display("Shot1Delay") && !revert.IsEnabled
            && Control<TextBox>(workspace,"DefinitionNumber").Text == Read().Raw("Shot1ID"),"No-op source reset retained unapplied specialized/generic draft inputs.");
        workspace.Undo(); Exact(full);
        if (Environment.GetEnvironmentVariable("DK2_ATTACKS_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts);
            foreach (var id in new[] { "Shot1","Shot2","Shot3","Melee","None" })
            {
                Choose(slot,id == "None" ? "Shot1" : id); if (id == "None") Choose(type,"0"); Layout(); var bitmap = new RenderTargetBitmap(1500,1000,96,96,PixelFormats.Pbgra32); bitmap.Render(workspace); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts,$"attacks-{id}-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
            }
        }
        Exact(full); workspace.Document = NativeMapDocument.Create(20,20,"Missing"); Choose(family,"Creatures"); Choose(section,"Attacks"); Layout();
        Expect(!apply.IsEnabled && !type.IsEnabled && type.SelectedItem is null && Box("Shot1Delay").Text.Length == 0 && hint.Text == LocalizationService.Get("Editor.Definitions.Attack.ReadOnly"),"Missing native attacks inferred from defaults.");
        workspace.Document = null; Layout(); Expect(panel.Visibility == Visibility.Collapsed && type.Items.Count == 0 && Control<WrapPanel>(workspace,"DefinitionAttackParameters").Children.Count == 0,"Close retained attack state/registered controls.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} creature attacks: named spell/melee choices, four slots, exact units, all-slot atomic drafts, None, hidden errors, unsigned imports, translation and pending commit");
    }
}
