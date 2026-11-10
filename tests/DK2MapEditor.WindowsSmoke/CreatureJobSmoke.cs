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
    private static void CheckCreatureJobs(NativeMapDocument document, string language)
    {
        GlobalDefinitionRow Row() => document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == 0);
        var clear = document.CreateCreatureJobsDraft(Row());
        foreach (var item in CreatureJobs.Groups) clear = document.RemoveCreatureJobs(clear,item.ID,clear.Slots(item.ID).Select((slot,index) => (slot,index)).Where(pair => pair.slot.Job != "0").Select(pair => pair.index));
        document.ApplyCreatureJobs(clear);
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace,"WorkspaceTabs"); tabs.SelectedIndex = 9;
        void Layout() { workspace.Measure(new Size(1500,1000)); workspace.Arrange(new Rect(0,0,1500,1000)); workspace.UpdateLayout(); }
        Layout();
        var family = Control<ComboBox>(workspace,"DefinitionFamily"); var section = Control<ComboBox>(workspace,"DefinitionSection"); var picker = Control<ComboBox>(workspace,"DefinitionPicker");
        var panel = Control<Expander>(workspace,"DefinitionJobsPanel"); var group = Control<ComboBox>(workspace,"DefinitionJobsGroup"); var type = Control<ComboBox>(workspace,"DefinitionJobsType"); var table = Control<DataGrid>(workspace,"DefinitionJobsTable");
        var chance = Control<TextBox>(workspace,"DefinitionJobsChance"); var mood = Control<TextBox>(workspace,"DefinitionJobsMood"); var mana = Control<TextBox>(workspace,"DefinitionJobsMana");
        var add = Control<Button>(workspace,"DefinitionJobsAdd"); var change = Control<Button>(workspace,"DefinitionJobsChange"); var remove = Control<Button>(workspace,"DefinitionJobsRemove"); var apply = Control<Button>(workspace,"DefinitionJobsApply"); var revert = Control<Button>(workspace,"DefinitionJobsRevert");
        var acknowledgement = Control<CheckBox>(workspace,"DefinitionJobsAcknowledged"); var status = Control<TextBlock>(workspace,"DefinitionStatus"); var totals = Control<TextBlock>(workspace,"DefinitionJobsTotals");
        void Choose(ComboBox combo,string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item,"ID") == id);
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dictionary<NativeFile,byte[]> Snapshot() => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Job UI changed stored bytes before apply/unrelated bytes."); }
        CreatureJobsDraft Read() => document.CreateCreatureJobsDraft(Row());
        var initial = Snapshot(); Expect(panel.Visibility == Visibility.Collapsed,"Terrain has invented job controls.");
        Choose(family,"Creatures"); picker.SelectedIndex = 0; Choose(section,"Jobs"); panel.IsExpanded = true; Layout();
        Expect(panel.Visibility == Visibility.Visible && group.Items.Count == 4 && table.Items.Count == 3 && table.Columns.Count == 5 && apply.IsEnabled && panel.Foreground is SolidColorBrush { Color.R: > 200 },"Job pane/group columns/readability missing.");
        var notifications = 0; document.Changed += (_,_) => notifications++;
        foreach (var item in CreatureJobs.Groups)
        {
            Choose(group,item.ID); Layout();
            Expect(table.Items.Count == item.Capacity && type.Items.Count == CreatureJobs.Allowed(item.ID).Count && type.Items.Cast<object>().All(value => !Get<string>(value,"Label").Contains("sz"))
                && table.Columns.Count == (item.HasParameters ? 5 : 2) && Control<WrapPanel>(workspace,"DefinitionJobsParameters").Visibility == (item.HasParameters ? Visibility.Visible : Visibility.Collapsed),"Group filtering/capacity/hate parameters wrong.");
        }
        Exact(initial); Expect(notifications == 0,"Browsing job groups modified the map.");
        void Add(string id,string probability,string signedMood,string signedMana) { Choose(type,id); chance.Text = probability; mood.Text = signedMood; mana.Text = signedMana; Expect(add.IsEnabled,"Allowed free-slot add disabled."); Click(add); }
        Choose(group,"Happy"); Add("1","30","-9","7"); Add("2","20","-32767","32767"); Add("3","50","15","-8");
        Expect(!add.IsEnabled && Get<string>(table.Items[1],"Mood") == "-32767" && Get<string>(table.Items[0],"Name").Contains(language == "ru" ? "Спать" : "Sleep"),"Draft full group/signed/named table failed.");
        Choose(group,"Unhappy"); Add("4","60","1","2"); Add("5","40","3","4"); Choose(group,"Angry"); Add("10","100","5","6");
        Choose(group,"Hate"); Add("28","ignored","ignored","ignored"); Choose(group,"Happy");
        Exact(initial); Expect(notifications == 0 && acknowledgement.Visibility == Visibility.Collapsed && revert.IsEnabled,"Draft changes leaked into document or totals wrong.");
        Click(apply); Expect(notifications == 1 && Read().Slots("Happy")[1].Mood == (document.IsLegacyProject ? "-32767" : "32769") && Read().Slots("Unhappy")[1].Job == "5" && Read().Slots("Hate")[0].Job == "28","All four groups not applied atomically.");
        var full = Snapshot(); Click(apply); Exact(full); Expect(notifications == 1,"Job no-op apply created history.");
        table.SelectedItems.Clear(); table.SelectedItems.Add(table.Items[0]); table.SelectedItems.Add(table.Items[2]); Click(remove); Exact(full);
        Expect(Get<string>(table.Items[0],"Name").Contains(language == "ru" ? "Есть" : "Eat") && Get<string>(table.Items[0],"Chance") == "20" && acknowledgement.Visibility == Visibility.Visible,"Multiple selected removal lost tuple/total warning.");
        Click(apply); Exact(full); Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Nonstandard total not explicitly acknowledged.");
        acknowledgement.IsChecked = true; Click(apply); Expect(Read().Slots("Happy")[0].Job == "2" && Read().Totals.Single(item => item.Group == "Happy").Total == 20,"Original warning override unsupported.");
        workspace.Undo(); Exact(full); Layout();
        table.SelectedItem = table.Items[0]; Choose(type,"1"); chance.Text = "73"; mood.Text = "-9"; mana.Text = "7"; Click(change); mood.Text = "-321";
        var beforeLabel = Get<string>(type.SelectedItem,"Label"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Get<string>(type.SelectedItem,"ID") == "1" && Get<string>(type.SelectedItem,"Label") != beforeLabel && chance.Text == "73" && mood.Text == "-321" && Get<string>(table.Items[0],"Chance") == "73" && acknowledgement.Visibility == Visibility.Visible,"Language switch discarded draft/input/total warning.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout(); Exact(full); Click(revert); Exact(full);
        table.SelectedItem = table.Items[0]; chance.Text = "100"; mood.Text = "invalid"; Click(change); Exact(full);
        Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed") && Get<string>(table.Items[0],"Chance") == "30","Invalid input partially changed draft.");
        // Pending generic property edits must not apply an older job snapshot.
        Control<Button>(workspace,"DefinitionProperties").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); var grid = Control<DataGrid>(workspace,"PropertyGrid");
        Layout(); var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item,"Field").Name == "HJobsMood[0]"); grid.SelectedItem = property; grid.ScrollIntoView(property,grid.Columns[1]); Layout(); grid.CurrentCell = new(property,grid.Columns[1]);
        Expect(grid.BeginEdit(),"Cannot begin generic job property edit."); Layout(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "999";
        tabs.SelectedIndex = 9; table.SelectedItem = table.Items[0]; chance.Text = "30"; mood.Text = "-12"; mana.Text = "7"; Click(change); Click(apply);
        workspace.Dispatcher.Invoke(() => {},System.Windows.Threading.DispatcherPriority.Background);
        Expect(Read().Slots("Happy")[0].Mood == "999" && status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Pending property commit applied stale job draft."); workspace.Undo(); Exact(full);
        // Preserve imported unknown IDs and signed aliases without a default selection.
        var unknownID = document.IsLegacyProject ? "4294967295" : "-1";
        document.SetGlobalDefinitionValues([new("Creatures",0,"HJobs[0]",unknownID),new("Creatures",0,"HJobsMood[0]","32768"),new("Creatures",0,"HJobsMana[0]","65527")]); Layout(); table.SelectedItem = table.Items[0];
        Expect(Get<string>(type.SelectedItem,"ID") == unknownID && mood.Text == "-32768" && mana.Text == "-9" && Get<string>(table.Items[0],"Name").Contains(LocalizationService.Get("Editor.Definitions.Jobs.Unknown")),"Unknown signed/unsigned job ID or WORD signed alias replaced.");
        var imported = Snapshot(); Click(change); Click(apply); Exact(imported); workspace.Undo(); Exact(full);
        document.ResetGlobalDefinitions("Jobs",[0]); var resetDefaults = Snapshot(); var beforeReset = notifications;
        Choose(group,"Happy"); table.SelectedItem = table.Items[0]; Choose(type,"1"); chance.Text = "40"; Click(change);
        Control<CheckBox>(workspace,"DefinitionConfirmed").IsChecked = true; Click(Control<Button>(workspace,"DefinitionReset")); Exact(resetDefaults);
        Expect(notifications == beforeReset && !revert.IsEnabled && Get<string>(type.SelectedItem,"ID") == Read().Slots("Happy")[0].Job && chance.Text == Read().Slots("Happy")[0].Chance,"No-op reset retained a job draft/history.");
        workspace.Undo(); Exact(full);
        if (Environment.GetEnvironmentVariable("DK2_JOBS_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts);
            foreach (var id in new[] { "Happy","Unhappy","Angry","Hate" })
            {
                Choose(group,id); Layout(); var bitmap = new RenderTargetBitmap(1500,1000,96,96,PixelFormats.Pbgra32); bitmap.Render(workspace); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts,$"jobs-{id}-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
            }
        }
        workspace.Document = NativeMapDocument.Create(20,20,"Missing"); Choose(family,"Creatures"); Choose(section,"Jobs"); Layout();
        Expect(!apply.IsEnabled && !type.IsEnabled && table.Items.Cast<object>().All(item => Get<string>(item,"Name") == "—"),"Missing native creature jobs inferred from defaults.");
        workspace.Document = null; Layout(); Expect(panel.Visibility == Visibility.Collapsed && type.Items.Count == 0 && chance.Text.Length == 0,"Closed document retained job state.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} creature jobs: four groups, filtered names, atomic drafts, multi-remove, signed values, warning override, translation and pending commit");
    }
}
