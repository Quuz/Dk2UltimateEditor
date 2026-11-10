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
    private static void CheckProjectiles(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); tabs.SelectedIndex = 9;
        void Layout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); }
        Layout();
        var family = Control<ComboBox>(workspace, "DefinitionFamily"); var picker = Control<ComboBox>(workspace, "DefinitionPicker");
        var panel = Control<Expander>(workspace, "DefinitionProjectilePanel"); var slot = Control<ComboBox>(workspace, "DefinitionProjectileSlot"); var type = Control<ComboBox>(workspace, "DefinitionProjectileType");
        var first = Control<TextBox>(workspace, "DefinitionProjectileData1"); var second = Control<TextBox>(workspace, "DefinitionProjectileData2"); var target = Control<ComboBox>(workspace, "DefinitionProjectileTarget"); var level = Control<ComboBox>(workspace, "DefinitionProjectileLevel");
        var apply = Control<Button>(workspace, "DefinitionProjectileApply"); var confirm = Control<CheckBox>(workspace, "DefinitionConfirmed"); var status = Control<TextBlock>(workspace, "DefinitionStatus"); var hint = Control<TextBlock>(workspace, "DefinitionProjectileHint");
        void Choose(ComboBox combo, string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == id);
        GlobalProjectileState State() => document.GlobalProjectile(Get<GlobalDefinitionRow>(picker.SelectedItem, "Row"), Get<string>(slot.SelectedItem, "ID"));
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Projectile UI changed unrelated bytes/undo."); }
        var initial = Snapshot(); Expect(panel.Visibility == Visibility.Collapsed, "Terrain has invented projectile controls.");
        Choose(family, "Traps"); picker.SelectedIndex = 0; panel.IsExpanded = true; Layout();
        Expect(panel.Visibility == Visibility.Visible && slot.Items.Count == 1 && type.Items.Count == 66 && apply.IsEnabled && type.Items.Cast<object>().All(item => !Get<string>(item, "Label").Contains("sz")), "Source projectile picker not bound.");
        Expect(panel.Foreground is SolidColorBrush { Color.R: > 200 } && Control<TextBlock>(workspace, "DefinitionProjectileLabel1").Foreground is SolidColorBrush { Color.R: > 200 }, "Dark projectile header/labels are unreadable on the dark panel.");
        Exact(initial); var notifications = 0; document.Changed += (_, _) => notifications++;
        foreach (var option in GlobalProjectiles.Options)
        {
            Choose(type, option.ID.ToString());
            Expect(Control<StackPanel>(workspace, "DefinitionProjectileFirst").Visibility == (option.Form == 255 ? Visibility.Collapsed : Visibility.Visible)
                && Control<StackPanel>(workspace, "DefinitionProjectileSecond").Visibility == (option.Form is 1 or 255 ? Visibility.Collapsed : Visibility.Visible)
                && target.Visibility == (option.Form is 2 or 3 or 4 ? Visibility.Visible : Visibility.Collapsed)
                && level.Visibility == (option.Form == 3 ? Visibility.Visible : Visibility.Collapsed)
                && apply.IsEnabled, "Incorrect rendered form for original projectile #" + option.ID);
        }
        Exact(initial); Expect(notifications == 0, "Browsing projectile forms modified the map.");
        confirm.IsChecked = true; Choose(type, "3"); first.Text = "123"; second.Text = "45";
        Expect(confirm.IsChecked == false && target.Visibility == Visibility.Collapsed && first.IsEnabled && second.IsEnabled, "Two-number form or confirmation invalidation failed.");
        apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(State().Type == "3" && State().Data1 == "123" && State().Data2 == "45" && notifications == 1, "Two-number apply not atomic/correct.");
        Choose(type, "4"); first.Text = "-9"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Layout();
        Expect(State().Data1 == (document.IsLegacyProject ? "-9" : "65527") && State().Data2 == "45" && first.Text == "-9" && Control<StackPanel>(workspace, "DefinitionProjectileSecond").Visibility == Visibility.Collapsed, "Signed one-number form changed inactive data2.");
        var signed = Snapshot(); var before = notifications; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(signed); Expect(before == notifications, "Signed reapply normalized bytes/history.");
        var beforeFallback=Snapshot();
        Choose(type, "2"); Expect(Get<string>(target.SelectedItem, "ID") == "1", "Creature form did not select the original first-target fallback.");Exact(beforeFallback);
        Choose(target, "7"); second.Text = "25"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(State().Type == "2" && State().Data1 == "7" && State().Data2 == "25" && target.Items.Count == 45 && Control<TextBlock>(workspace, "DefinitionProjectileLabel2").Text == LocalizationService.Get("Editor.Definitions.Projectile.Count"), "Creature/count form not bound.");
        var counted = Snapshot(); second.Text = "invalid"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(counted); Expect(status.Text == LocalizationService.Get("Editor.Definitions.Failed"), "Count input error invisible.");
        Choose(type, "60"); Choose(level, "10"); apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(State().Data1 == "7" && State().Data2 == "10" && level.Visibility == Visibility.Visible && second.Visibility == Visibility.Collapsed, "Creature/level form failed.");
        Choose(type, "5"); Choose(target, "1"); second.Text = "500"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(State().Data1 == "1" && State().Data2 == "500" && second.IsEnabled && hint.Text.Contains(document.IsLegacyProject ? "80000" : "65535"), "Object/gold form/native limits failed.");
        Choose(target, "4"); Expect(!second.IsEnabled && second.Text == "500", "Selecting non-gold changed bytes/draft before apply.");
        apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(State().Data1 == "4" && State().Data2 == "0", "Explicit non-gold apply did not clear gold.");
        Choose(type, "0"); apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(State().Type == "0" && State().Data1 == "4" && State().Data2 == "0" && Control<StackPanel>(workspace, "DefinitionProjectileFirst").Visibility == Visibility.Collapsed, "No-options type overwrote inactive arguments.");
        var saved = Snapshot(); Choose(type, "3"); first.Text = "321"; second.Text = "654"; var oldLabel = Get<string>(type.SelectedItem, "Label");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Get<string>(type.SelectedItem, "ID") == "3" && Get<string>(type.SelectedItem, "Label") != oldLabel && first.Text == "321" && second.Text == "654", "Translation discarded compound draft/type.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout(); Exact(saved);
        Choose(family, "Spells"); picker.SelectedIndex = 0; var primary = State(); Expect(slot.Items.Count == 2, "Upgraded projectile slot missing.");
        Choose(slot, "Upgrade"); Choose(type, "4"); first.Text = "-11"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(State().Data1 == (document.IsLegacyProject ? "-11" : "65525"), "Upgraded projectile signed input failed.");
        Choose(slot, "Shot"); Expect(State() == primary, "Upgrade apply changed primary projectile.");
        Choose(family, "CrSpells"); picker.SelectedIndex = 0; Choose(type, "3"); first.Text = "678"; second.Text = "90"; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(State().Type == "3" && State().Data1 == "678" && State().Data2 == "90", "Creature spell's Shot field incorrectly mapped to ShotType.");
        // A generic edit committing during apply must not reuse the old type
        // against refreshed parameter drafts.
        Choose(family, "Traps"); var record = Get<GlobalDefinitionRow>(picker.SelectedItem, "Row").Record!;
        Control<Button>(workspace, "DefinitionProperties").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); var grid = Control<DataGrid>(workspace, "PropertyGrid");
        var item = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == "ShotData1"); grid.SelectedItem = item; grid.ScrollIntoView(item); Layout(); grid.CurrentCell = new(item, grid.Columns[1]);
        Expect(grid.BeginEdit(), "Cannot begin projectile generic-property edit."); Layout(); ((TextBox)grid.Columns[1].GetCellContent(item)).Text = "999";
        tabs.SelectedIndex = 9; Choose(type, "3"); first.Text = "123"; second.Text = "456";
        apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); workspace.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        Expect(State().Type == "0" && State().Data1 == "999" && status.Text == LocalizationService.Get("Editor.Definitions.Failed"), "Pending generic commit applied stale compound draft."); workspace.Undo();
        if (Environment.GetEnvironmentVariable("DK2_PROJECTILE_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            Directory.CreateDirectory(artifacts);
            foreach (var (id, formName) in new[] { ("3", "two-values"), ("4", "signed-value"), ("2", "creature-count"), ("60", "creature-level"), ("5", "object-gold"), ("0", "no-options") })
            {
                Choose(type, id);
                if (id is "2" or "60") Choose(target, "7");
                else if (id == "5") Choose(target, "1");
                else first.Text = id == "4" ? "-9" : "123";
                if (id == "60") Choose(level, "10"); else second.Text = "45";
                Layout(); var bitmap = new RenderTargetBitmap(1500, 1000, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"projectile-{formName}-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
            }
        }
        document.ResetGlobalDefinitions("Traps", [0]); var resetDefaults = Snapshot(); var beforeReset = notifications;
        Choose(type, "3"); first.Text = "123"; second.Text = "45"; confirm.IsChecked = true;
        Control<Button>(workspace, "DefinitionReset").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(resetDefaults);
        Expect(notifications == beforeReset && Get<string>(type.SelectedItem, "ID") == State().Type && first.Text == GlobalProjectiles.DisplayParameter(GlobalProjectiles.Find(State().Type)!, State().Data1, false), "No-op reset retained a projectile draft/history.");
        document.SetGlobalDefinitionValues([new("Traps", 0, "ShotType", "255")]); Layout();
        Expect(Get<string>(type.SelectedItem, "ID") == "255" && type.Items.Count == 67 && !apply.IsEnabled && hint.Text == LocalizationService.Get("Editor.Definitions.Projectile.UnknownHint"), "Unknown type was replaced or guessed editable.");
        var unknown = Snapshot(); apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(unknown);
        workspace.Document = NativeMapDocument.Create(20, 20, "Missing"); Choose(family, "Traps"); Layout();
        Expect(!apply.IsEnabled && !type.IsEnabled && !first.IsEnabled && hint.Text == LocalizationService.Get("Editor.Definitions.Projectile.ReadOnly"), "Missing layout invented projectile values.");
        workspace.Document = null; Layout(); Expect(panel.Visibility == Visibility.Collapsed && type.Items.Count == 0 && first.Text.Length == 0, "Close retained projectile state.");
        CheckProjectileSourceOptionFallbacks(document,language);
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} all five projectile forms, four slots, named targets, signed values, inactive fields, drafts, pending commit and unknown guards");
    }
}
