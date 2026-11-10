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
    private static void CheckDecorations(NativeMapDocument document, string language)
    {
        document.SetGlobalDefinitionValues(GlobalDefinitionDecorations.Families.SelectMany(name => GlobalDefinitionDecorations.Fields(name).Select(field => new GlobalDefinitionChange(name,name == "Rooms" ? 6 : name == "Terrains" ? 2 : 0,field,"0"))));
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace,"WorkspaceTabs"); tabs.SelectedIndex = 9;
        void Layout(double width = 1500,double height = 1000) { workspace.Measure(new Size(width,height)); workspace.Arrange(new Rect(0,0,width,height)); workspace.UpdateLayout(); }
        Layout(); var family = Control<ComboBox>(workspace,"DefinitionFamily"); var section = Control<ComboBox>(workspace,"DefinitionSection"); var picker = Control<ComboBox>(workspace,"DefinitionPicker");
        var panel = Control<Expander>(workspace,"DefinitionDecorationPanel"); var apply = Control<Button>(workspace,"DefinitionDecorationApply"); var revert = Control<Button>(workspace,"DefinitionDecorationRevert"); var status = Control<TextBlock>(workspace,"DefinitionStatus");
        ComboBox Choice(string field) => Control<ComboBox>(workspace,"DefinitionDecoration" + field.Replace("[", "").Replace("]", ""));
        void Choose(ComboBox combo,string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item,"ID") == id);
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        void Select(string name,int? index = null) { Choose(family,name); if (name == "Creatures") Choose(section,"Parameters"); picker.SelectedIndex = index ?? (name == "Rooms" ? 6 : name == "Terrains" ? 2 : 0); Layout(); panel.IsExpanded = true; }
        GlobalDecorationsDraft Read(string name,int? index = null) => document.CreateGlobalDecorationsDraft(document.GlobalDefinitionRows().Single(row => row.Family == name && row.Index == (index ?? (name == "Rooms" ? 6 : name == "Terrains" ? 2 : 0))));
        Dictionary<NativeFile,byte[]> Snapshot() => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Decoration UI changed unrelated/undo bytes."); }
        var initial = Snapshot(); var changes = 0; document.Changed += (_,_) => changes++;
        foreach (var name in GlobalDefinitionDecorations.Families)
        {
            Select(name); Expect(panel.Visibility == Visibility.Visible && panel.Foreground is SolidColorBrush { Color.R: > 200 } && Control<WrapPanel>(workspace,"DefinitionDecorationFields").Children.Count == GlobalDefinitionDecorations.Fields(name).Count,"Wrong/low-contrast decoration panel.");
            foreach (var field in GlobalDefinitionDecorations.Fields(name))
            {
                var combo = Choice(field); var choices = GlobalDefinitionDecorations.Choices(name,field);
                Expect(combo.Items.Count == choices.Count && combo.IsEnabled == GlobalDefinitionDecorations.Enabled(name,Read(name).Index,field),"Decoration list or inactive slot mismatch.");
                foreach (var choice in choices) if (combo.IsEnabled) Choose(combo,choice.Value);
                Expect(combo.Items.Cast<object>().All(item => !Get<string>(item,"Label").Contains("szEffect")),"Effect symbols leaked into UI.");
            }
            Exact(initial); Click(revert);
        }
        Select("Rooms"); for (var index = 0; index < 24; index++)
        {
            picker.SelectedIndex = index; Layout();
            foreach (var field in GlobalDefinitionDecorations.Fields("Rooms")) Expect(Choice(field).IsEnabled == GlobalDefinitionDecorations.Enabled("Rooms",index,field),"Room selection did not update slot masks.");
        }
        Exact(initial); Expect(changes == 0,"Browsing choices/masks wrote data.");
        foreach (var name in GlobalDefinitionDecorations.Families)
        {
            Select(name); foreach (var field in GlobalDefinitionDecorations.Fields(name).Where(field => Choice(field).IsEnabled)) Choose(Choice(field),GlobalDefinitionDecorations.Choices(name,field)[^1].Value);
        }
        Choose(section,"Attributes"); Layout(); Expect(panel.Visibility == Visibility.Collapsed,"Creature effects visible outside Parameters."); Choose(section,"Parameters"); Layout(); Expect(Get<string>(Choice("ArmourType").SelectedItem,"ID") == "7","Changing creature group lost draft.");
        var label = Get<string>(Choice("EntranceEffect").SelectedItem,"Label"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Get<string>(Choice("EntranceEffect").SelectedItem,"Label") != label && Get<string>(Choice("EntranceEffect").SelectedItem,"ID") == "370","Translation lost draft/names.");
        Select("Rooms"); Expect(Get<string>(Choice("Effects[0]").SelectedItem,"ID") == "370" && !Choice("BattleEffect").IsEnabled,"Translation lost hidden family draft or enabled original inactive BattleEffect."); LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Exact(initial);
        foreach (var name in GlobalDefinitionDecorations.Families)
        {
            Select(name); var before = changes; Click(apply); Expect(changes == before + 1,"Whole decoration group not one edit.");
            foreach (var field in GlobalDefinitionDecorations.Fields(name)) Expect(Read(name).Raw(field) == (Choice(field).IsEnabled ? GlobalDefinitionDecorations.Choices(name,field)[^1].Value : "0"),"Decoration UI applied wrong field or inactive slot.");
            var snapshot = Snapshot(); Click(apply); Exact(snapshot); Expect(changes == before + 1 && !revert.IsEnabled,"No-op decoration apply created history/draft.");
        }
        var full = Snapshot(); Select("Rooms"); Choose(Choice("BattleEffect"),"1"); Choose(Choice("Effects[5]"),"1"); Click(apply); Exact(full); Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Disabled room slot partly applied another choice."); Click(revert);
        Layout(980,500); var scroll = Control<ScrollViewer>(workspace,"DefinitionScroll");
        Expect(scroll.ScrollableHeight > 0 && Control<DataGrid>(workspace,"DefinitionTable").ActualHeight >= 100,"Expanded room controls displaced the definition table in a small window.");
        scroll.ScrollToEnd(); workspace.UpdateLayout(); var lastBounds = Choice("Objects[5]").TransformToAncestor(scroll).TransformBounds(new Rect(new Point(),Choice("Objects[5]").RenderSize));
        Expect(lastBounds.Top >= 0 && lastBounds.Bottom <= scroll.ActualHeight + 1,"Last room object cannot be reached by scrolling."); scroll.ScrollToHome(); Layout(); Exact(full);
        foreach (var name in GlobalDefinitionDecorations.Families)
        {
            Select(name); var unknown = GlobalDefinitionDecorations.Fields(name).Select(field => new GlobalDefinitionChange(name,Read(name).Index,field,document.IsLegacyProject ? "4294967295" : field == "ArmourType" || field.StartsWith("Objects[") ? "255" : "65535")).ToArray();
            document.SetGlobalDefinitionValues(unknown); Layout(); var imported = Snapshot(); var before = changes;
            foreach (var change in unknown) Expect(Get<string>(Choice(change.Field).SelectedItem,"ID") == change.Value && Choice(change.Field).Items.Count == GlobalDefinitionDecorations.Choices(name,change.Field).Count + 1,"Imported unknown guessed/removed.");
            Click(apply); Exact(imported); Expect(changes == before,"Unknown decoration no-op normalized data.");
            var field = GlobalDefinitionDecorations.Fields(name).First(item => Choice(item).IsEnabled); Choose(Choice(field),"0"); Click(apply); Expect(Read(name).Raw(field) == "0" && GlobalDefinitionDecorations.Fields(name).Where(item => item != field).All(item => Read(name).Raw(item) == unknown.Single(change => change.Field == item).Value),"Explicit choice repair normalized other fields.");
            workspace.Undo(); Exact(imported); workspace.Undo(); Exact(full);
        }
        Select("Creatures"); Click(Control<Button>(workspace,"DefinitionProperties")); var grid = Control<DataGrid>(workspace,"PropertyGrid"); Layout();
        var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item,"Field").Name == "SlapEffect"); grid.SelectedItem = property; grid.ScrollIntoView(property,grid.Columns[1]); Layout(); grid.CurrentCell = new(property,grid.Columns[1]);
        Expect(grid.BeginEdit(),"Cannot begin generic decoration edit."); Layout(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "1"; tabs.SelectedIndex = 9; Choose(Choice("EntranceEffect"),"2"); Click(apply); workspace.Dispatcher.Invoke(() => {},System.Windows.Threading.DispatcherPriority.Background);
        Expect(Read("Creatures").Raw("SlapEffect") == "1" && Read("Creatures").Raw("EntranceEffect") == "370" && status.Text == LocalizationService.Get("Editor.Definitions.Failed"),"Pending generic commit saved stale decoration draft."); workspace.Undo(); Exact(full);
        if (Environment.GetEnvironmentVariable("DK2_DECORATIONS_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts);
            foreach (var name in GlobalDefinitionDecorations.Families)
            {
                Select(name); Layout(); Render(name,1500);
                if (name == "Rooms") { Layout(900); Render("Rooms-narrow",900); }
            }
            void Render(string name,int width)
            {
                var bitmap = new RenderTargetBitmap(width,1000,96,96,PixelFormats.Pbgra32); bitmap.Render(workspace); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts,$"decorations-{name}-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
            }
        }
        foreach (var name in GlobalDefinitionDecorations.Families)
        {
            Select(name); var group = name == "Creatures" ? "Parameters" : name; document.ResetGlobalDefinitions(group,[Read(name).Index]); var defaults = Snapshot(); var before = changes;
            var field = GlobalDefinitionDecorations.Fields(name).First(item => Choice(item).IsEnabled); var replacement = Read(name).Raw(field) == "0" ? "1" : "0"; Choose(Choice(field),replacement); Control<TextBox>(workspace,"DefinitionNumber").Text = "unapplied input";
            Control<CheckBox>(workspace,"DefinitionConfirmed").IsChecked = true; Click(Control<Button>(workspace,"DefinitionReset")); Exact(defaults);
            Expect(changes == before && !revert.IsEnabled && Get<string>(Choice(field).SelectedItem,"ID") == Read(name).Raw(field),"No-op reset retained decoration draft/history."); workspace.Undo(); Exact(full);
        }
        for (var index = 0; index < 5; index++) workspace.Undo(); Exact(initial);
        workspace.Document = NativeMapDocument.Create(20,20,"Missing"); Select("Creatures"); Layout(); Expect(!apply.IsEnabled && !Choice("DeathEffect").IsEnabled && Choice("DeathEffect").SelectedItem is null,"Missing native effects inferred from defaults.");
        workspace.Document = null; Layout(); Expect(panel.Visibility == Visibility.Collapsed && Control<WrapPanel>(workspace,"DefinitionDecorationFields").Children.Count == 0 && workspace.FindName("DefinitionDecorationDeathEffect") is null,"Closing retained decoration controls.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} effects/room objects/armour: 371 bilingual effects, 24 fields, all room masks, retained inactive/unknown slots, independent drafts, atomic apply, pending commits and no-op reset");
    }
}
