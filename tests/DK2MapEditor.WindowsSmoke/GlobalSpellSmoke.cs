using System.Globalization;
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
    private static void CheckGlobalSpell(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document }; Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9;
        void LayoutWorkspace() { workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout(); }
        LayoutWorkspace(); var family = Control<ComboBox>(workspace, "DefinitionFamily"); family.SelectedItem = family.Items.Cast<object>().Single(item => Get<string>(item, "ID") == "Spells"); LayoutWorkspace();
        var picker = Control<ComboBox>(workspace, "DefinitionPicker"); GlobalSpellWindow? active = null; GlobalSpellOptionsWindow? child = null;
        workspace.SpellPropertiesPresenter = window => active = window;
        GlobalDefinitionRow Row(int index = 0) => document.GlobalDefinitionRows().Single(row => row.Family == "Spells" && row.Index == index);
        GlobalSpellDraft Read() => document.CreateGlobalSpellDraft(Row());
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(document.Files.Count == bytes.Count && document.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Spell UI changed private/cancel/no-op/undo bytes.");
        void Layout(Window window, int width = 740, int height = 660)
        { var content = (FrameworkElement)window.Content; window.Width = width; window.Height = height; content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout(); }
        T Field<T>(GlobalSpellWindow window, string field) where T : Control => (T)window.FindName("SpellProperty" + field);
        T Option<T>(GlobalSpellOptionsWindow window, string field) where T : Control => (T)window.FindName("SpellOptions" + field);
        void Set(GlobalSpellWindow window, string field, string value) => Field<TextBox>(window, field).Text = value;
        void Choose(GlobalSpellWindow window, string field, string value)
        { var combo = Field<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        void ChildChoose(GlobalSpellOptionsWindow window, string field, string value)
        { var combo = Option<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        GlobalSpellWindow Open(int index = 0)
        {
            active?.Close(); active = null; picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<GlobalDefinitionRow>(item, "Row").Index == index);
            LayoutWorkspace(); var button = Control<Button>(workspace, "DefinitionSpellProperties"); Expect(button.Visibility == Visibility.Visible && button.IsEnabled, "Actual spell command unavailable.");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(active is not null, "Actual spell command did not open.");
            active!.OptionsPresenter = window => { child = window; Layout(window, 600, 370); }; Layout(active); return active;
        }
        GlobalSpellOptionsWindow OpenChild(GlobalSpellWindow window, string slot)
        {
            child = null; Field<Button>(window, (slot == "Shot" ? "ShotType" : "UpgShotType") + "Options").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(child is not null && !window.ApplySpell() && !window.StepNumber("ManaCost", 1) && !window.OpenOptions(slot == "Shot" ? "Upgrade" : "Shot"), "Actual modal spell child missing/parent still editable."); return child!;
        }
        void Render(Window window, string state, bool parent)
        {
            if (Environment.GetEnvironmentVariable("DK2_SPELL_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return; Directory.CreateDirectory(artifacts);
            foreach (var width in parent ? new[] { 540, 740 } : new[] { 420, 600 }) foreach (var bottom in new[] { false, true })
            {
                var height = parent ? width == 540 ? 420 : 660 : width == 420 ? 300 : 370; Layout(window, width, height);
                if (parent)
                {
                    var scroll = (ScrollViewer)window.FindName("SpellPropertiesScroll"); if (bottom) scroll.ScrollToEnd(); else scroll.ScrollToHome(); ((FrameworkElement)window.Content).UpdateLayout();
                    Expect(scroll.ScrollableHeight > 0 && scroll.ExtentWidth <= scroll.ViewportWidth + 1, "Spell page overflows horizontally/cannot scroll.");
                    if (bottom)
                    {
                        var last = Field<TextBox>((GlobalSpellWindow)window, "UpgRechargeTime"); var bounds = last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(), last.RenderSize));
                        Expect(bounds.Top >= 0 && bounds.Bottom <= scroll.ActualHeight + 1, "Last spell field unreachable.");
                    }
                }
                else
                {
                    var scroll=((DockPanel)window.Content).Children.OfType<ScrollViewer>().Single();
                    if(bottom)scroll.ScrollToEnd();else scroll.ScrollToHome();((FrameworkElement)window.Content).UpdateLayout();
                    Expect(scroll.ExtentWidth<=scroll.ViewportWidth+1,"Spell child horizontal overflow.");
                    if(bottom)
                    {
                        var suffix=window.FindName("SpellOptionsData2Increase") is Control?"Data2Increase":window.FindName("SpellOptionsData2") is Control?"Data2":window.FindName("SpellOptionsData1Increase") is Control?"Data1Increase":"Data1";
                        var last=(Control)window.FindName("SpellOptions"+suffix);var bounds=last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(),last.RenderSize));
                        Expect(bounds.Top>=0&&bounds.Bottom<=scroll.ActualHeight+1,"Last spell child operand/step unreachable.");
                    }
                    foreach(var suffix in new[]{"OK","Cancel"})
                    {
                        var button=(Button)window.FindName("SpellOptions"+suffix);var bounds=button.TransformToAncestor((FrameworkElement)window.Content).TransformBounds(new Rect(new Point(),button.RenderSize));
                        Expect(button.Visibility==Visibility.Visible&&button.RenderSize.Height>0&&bounds.Top>=0&&bounds.Bottom<=height+1,"Spell child footer unreachable.");
                    }
                }
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render((FrameworkElement)window.Content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"spell-{state}-{language}-{(document.IsLegacyProject ? "project" : "native")}-{width}-{bottom}.png")); encoder.Save(output);
            }
        }
        var initial = Snapshot(); var initialHistory = document.GetHistory().Undo.Count;
        foreach (var index in Enumerable.Range(0, 13))
        {
            var window = Open(index); var draft = document.CreateGlobalSpellDraft(Row(index));
            Expect(window.Title.EndsWith(ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue("Spells")[index], Row(index).ID.ToString(CultureInfo.InvariantCulture)).Name(language), StringComparison.Ordinal), "Spell title lacks original named type.");
            foreach (var field in GlobalSpellControls.MainFields)
            {
                var control = Field<Control>(window, field); Expect(control.IsEnabled, "Original spell field unexpectedly disabled.");
                if (control is ComboBox combo) Expect(Get<string>(combo.SelectedItem, "ID") == draft.Raw(field), "Spell opening guessed a named value.");
                else if (control is TextBox text) Expect(text.Text == draft.Display(field) && Field<Control>(window, field + "Decrease").IsEnabled && Field<Control>(window, field + "Increase").IsEnabled, "Spell source display/spinner differs.");
            }
            if (index is 0 or 12) Render(window, index == 0 ? "imp" : "unique-horny", true);
            Expect(window.ApplySpell(), "Spell default no-op failed."); window.Close(); Exact(initial);
        }
        Expect(document.GetHistory().Undo.Count == initialHistory, "Opening spell definitions materialized history.");
        var edited = Open(); Set(edited, "ManaCost", "999999"); Set(edited, "ManaDrain", "1234"); Set(edited, "RechargeTime", "1.5"); Set(edited, "UpgRechargeTime", "5678");
        Choose(edited, "HandAnim", "1"); Choose(edited, "NoGoHandAnim", "0"); Choose(edited, "CastRule", "5"); Choose(edited, "TargetRule", "6");
        foreach (var field in new[] { "Attacking", "Defensive" }) Field<CheckBox>(edited, field).IsChecked = Read().Raw(field) != "1";
        var two = GlobalProjectiles.Options.First(option => option.Form == 0); Choose(edited, "ShotType", two.ID.ToString(CultureInfo.InvariantCulture)); Choose(edited, "UpgShotType", two.ID.ToString(CultureInfo.InvariantCulture));
        foreach (var slot in new[] { "Shot", "Upgrade" })
        {
            var options = OpenChild(edited, slot); Option<TextBox>(options, "Data1").Text = two.Signed1 ? slot == "Shot" ? "-9" : "-8" : slot == "Shot" ? "111" : "333"; Option<TextBox>(options, "Data2").Text = two.Signed2 ? "-7" : "222";
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(Option<TextBox>(options, "Data1").Text == (two.Signed1 ? slot == "Shot" ? "-9" : "-8" : slot == "Shot" ? "111" : "333"), "Spell child translation lost input."); LocalizationService.SetLanguage(language);
            Render(options, slot.ToLowerInvariant() + "-selected", false); Option<Button>(options, "OK").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(options.AcceptedDraft is not null, "Spell child OK failed."); Exact(initial);
            var cancelledChild = OpenChild(edited, slot); Option<TextBox>(cancelledChild, "Data1").Text = "999"; cancelledChild.Close(); Exact(initial);
        }
        Render(edited, "selected", true); var changes = 0; document.Changed += (_, _) => changes++;
        ((Button)edited.FindName("SpellPropertiesApply")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(changes == 1 && Read().Raw("ManaCost") == "999999" && Read().Raw("ManaDrain") == "1234" && Read().Raw("RechargeTime") == "6144" && Read().Raw("UpgRechargeTime") == "5678" && Read().Raw("CastRule") == "5" && Read().Raw("TargetRule") == "6" && Read().Display("ShotData1") == (two.Signed1 ? "-9" : "111") && Read().Display("UpgShotData1") == (two.Signed1 ? "-8" : "333"), "Parent/two children did not apply in one complete edit.");
        var applied = Snapshot(); Expect(edited.ApplySpell(), "Spell no-op failed."); Exact(applied); Expect(changes == 1, "Spell no-op consumed history.");
        document.Undo(); Exact(initial); Expect(!edited.ApplySpell() && !edited.OpenOptions("Shot"), "Stale parent accepted Apply/child."); document.Redo(); Exact(applied); document.Undo(); Exact(initial); edited.Close();
        foreach (var slot in new[] { "Shot", "Upgrade" }) foreach (var option in GlobalProjectiles.Options)
        {
            var window = Open(); Choose(window, slot == "Shot" ? "ShotType" : "UpgShotType", option.ID.ToString(CultureInfo.InvariantCulture));
            if (option.Form == 255) { Expect(!window.OpenOptions(slot), "No-options projectile opened a child."); window.Close(); continue; }
            var form = OpenChild(window, slot); Expect(form.FindName("SpellOptionsData1") is (ComboBox or TextBox), "First spell operand missing.");
            Expect((form.FindName("SpellOptionsData2") is not null) == (option.Form != 1), "Source form1 second operand exposed/lost.");
            if (option.Form == 3) Expect(form.FindName("SpellOptionsData2") is ComboBox, "Source level catalogue missing.");
            if (option.Form == 4)
            {
                ChildChoose(form, "Data1", "1"); Expect(Option<TextBox>(form, "Data2").IsEnabled, "Gold object did not enable amount."); ChildChoose(form, "Data1", GlobalProjectiles.Targets(option).First(choice => choice.Value != "1").Value);
                Expect(!Option<TextBox>(form, "Data2").IsEnabled && !Option<Control>(form, "Data2Increase").IsEnabled, "Non-gold object enabled amount/spinner.");
            }
            if (option.ID == GlobalProjectiles.Options.First(item => item.Form == option.Form).ID) Render(form, slot.ToLowerInvariant() + "-form-" + option.Form, false);
            form.Close(); Expect(!form.AcceptOptions(), "Closed child accepted late OK."); window.Close(); Exact(initial);
        }
        foreach (var slot in new[] { "Shot", "Upgrade" })
        {
            var type = slot == "Shot" ? "ShotType" : "UpgShotType"; var first = slot == "Shot" ? "ShotData1" : "UpgShotData1"; var second = slot == "Shot" ? "ShotData2" : "UpgShotData2";
            var objectType = GlobalProjectiles.Options.First(option => option.Form == 4); var objectID = GlobalProjectiles.Targets(objectType).First(choice => choice.Value != "1").Value;
            document.SetGlobalDefinitionValues([new("Spells", 0, type, objectType.ID.ToString(CultureInfo.InvariantCulture)), new("Spells", 0, first, objectID), new("Spells", 0, second, "1234")]);
            var importedObject = Snapshot(); var objects = Open(); Expect(objects.ApplySpell(), "Object parent no-op failed."); Exact(importedObject);
            var objectChild = OpenChild(objects, slot); Expect(objectChild.AcceptOptions(), "Explicit non-gold child OK failed."); Exact(importedObject);
            Expect(objects.ApplySpell() && Read().Raw(second) == "0", "Explicit child Save did not clear non-gold argument."); objects.Close(); document.Undo(); Exact(importedObject); document.Undo(); Exact(initial);
        }
        var invalid = Open(); Set(invalid, "ManaCost", "222"); Set(invalid, "UpgRechargeTime", "1.5"); Expect(!invalid.ApplySpell() && !invalid.OpenOptions("Shot"), "Invalid integer upgrade recharge partly applied/opened child."); Exact(initial); invalid.Close();
        var cancelled = Open(); Set(cancelled, "ManaCost", "222"); Choose(cancelled, "ShotType", two.ID.ToString(CultureInfo.InvariantCulture)); var savedChild = OpenChild(cancelled, "Shot"); Option<TextBox>(savedChild, "Data1").Text = "888"; Expect(savedChild.AcceptOptions(), "Private cancelled-parent child failed.");
        ((Button)cancelled.FindName("SpellPropertiesCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial); Expect(!cancelled.ApplySpell(), "Closed spell accepted Apply.");
        var staleParent = Open(); Choose(staleParent, "ShotType", two.ID.ToString(CultureInfo.InvariantCulture)); var staleChild = OpenChild(staleParent, "Shot"); document.SetGlobalDefinitionValues([new("Spells", 0, "UpgShotData2", Read().Raw("UpgShotData2") == "0" ? "1" : "0")]);
        Expect(!staleChild.AcceptOptions() && !staleParent.ApplySpell(), "Changed other-slot baseline accepted stale child/parent."); staleParent.Close(); document.Undo(); Exact(initial);
        document.SetGlobalDefinitionValues([new("Spells", 0, "HandAnim", "254"), new("Spells", 0, "CastRule", "253"), new("Spells", 0, "ShotType", "252"), new("Spells", 0, "UpgShotType", "251"), new("Spells", 0, "ManaCost", "2000000"), new("Spells", 0, "ManaDrain", "65535")]);
        var imported = Snapshot(); var retained = Open(); Render(retained, "imported", true); Expect(retained.ApplySpell() && !retained.OpenOptions("Shot") && !retained.OpenOptions("Upgrade"), "Imported unknown parent changed/invented options."); Exact(imported);
        Choose(retained, "HandAnim", "0"); Expect(retained.ApplySpell() && Read().Raw("CastRule") == "253" && Read().Raw("ManaCost") == "2000000", "Spell import repair normalized another field."); retained.Close(); document.Undo(); Exact(imported); document.Undo(); Exact(initial);
        GlobalSpellWindow? pending = null; var reentrantRejected = false;
        pending = new(document, Row(), () => true, () => { reentrantRejected = !pending!.ApplySpell() && !pending.OpenOptions("Shot") && !pending.StepNumber("ManaCost", 1); Set(pending, "ManaCost", "444"); return true; });
        Set(pending, "ManaCost", "222"); Expect(!pending.ApplySpell() && reentrantRejected, "Spell input/reentrant guard failed."); Exact(initial); pending.Close();
        document.SetGlobalDefinitionValues([new("Spells", 0, "ManaCost", "111")]); var materialized = Snapshot(); var genericPending = Open(); Set(genericPending, "ManaCost", "222");
        Control<Button>(workspace, "DefinitionProperties").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); LayoutWorkspace(); var grid = Control<DataGrid>(workspace, "PropertyGrid"); var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == "ManaCost");
        grid.SelectedItem = property; grid.ScrollIntoView(property, grid.Columns[1]); LayoutWorkspace(); grid.CurrentCell = new(property, grid.Columns[1]); Expect(grid.BeginEdit(), "Generic pending spell edit did not start."); LayoutWorkspace(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "555";
        Expect(!genericPending.ApplySpell() && Read().Raw("ManaCost") == "555", "Generic pending commit was overwritten by spell parent."); workspace.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        document.Undo(); Exact(materialized); document.Undo(); Exact(initial); genericPending.Close(); Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9; LayoutWorkspace();
        var late = Open(); Choose(late, "ShotType", two.ID.ToString(CultureInfo.InvariantCulture)); var lateChild = OpenChild(late, "Shot"); workspace.Document = null;
        Expect(!late.ApplySpell() && !lateChild.AcceptOptions() && !late.StepNumber("ManaCost", 1), "Closed workspace accepted spell/child edit."); Exact(initial);
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} complete keeper spell page:12 main/4 nested,13 rows/2 slots/66 shot types, integer upgrade recharge, private children/Cancel/atomic Apply, source object Save, translation/imports/pending/owner/stale/reentrant guards");
    }
}
