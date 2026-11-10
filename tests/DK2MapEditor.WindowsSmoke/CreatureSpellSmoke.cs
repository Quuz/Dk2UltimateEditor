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
    // This exercises the complete typed row page for later hosting inside the
    // nine-page parent. It does not establish the still-missing parent command.
    private static void CheckCreatureSpell(NativeMapDocument document, string language)
    {
        document.SetHistoryDepth(65535);
        GlobalDefinitionRow Row(NativeMapDocument doc, int index = 0) => doc.GlobalDefinitionRows().Single(row => row.Family == "CrSpells" && row.Index == index);
        CreatureSpellDraft Read() => document.CreateCreatureSpellDraft(Row(document));
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(document.Files.Count == bytes.Count && document.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Creature spell UI changed private/cancel/no-op/undo bytes.");
        void Layout(Window window, int width = 740, int height = 660)
        { var content = (FrameworkElement)window.Content; window.Width = width; window.Height = height; content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout(); }
        T Field<T>(CreatureSpellWindow window, string field) where T : Control => (T)window.FindName("CreatureSpellProperty" + field);
        T Option<T>(CreatureSpellOptionsWindow window, string field) where T : Control => (T)window.FindName("CreatureSpellOptions" + field);
        void Set(CreatureSpellWindow window, string field, string value) => Field<TextBox>(window, field).Text = value;
        void Choose(CreatureSpellWindow window, string field, string value)
        { var combo = Field<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        void ChildChoose(CreatureSpellOptionsWindow window, string field, string value)
        { var combo = Option<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        CreatureSpellOptionsWindow? child = null;
        CreatureSpellWindow Open(int index = 0, NativeMapDocument? working = null, Func<bool>? ownerCurrent = null, Func<bool>? commit = null)
        {
            var doc = working ?? document; var window = new CreatureSpellWindow(doc, Row(doc, index), ownerCurrent ?? (() => true), commit ?? (() => true));
            window.OptionsPresenter = options => { child = options; Layout(options, 600, 370); }; Layout(window); return window;
        }
        CreatureSpellOptionsWindow OpenChild(CreatureSpellWindow window)
        {
            child = null; Field<Button>(window, "ShotOptions").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(child is not null && !window.ApplySpell() && !window.StepNumber("Range", 1) && !window.OpenOptions("Shot"), "Creature spell child missing or parent editable during options."); return child!;
        }
        void Render(Window window, string state, bool parent)
        {
            if (Environment.GetEnvironmentVariable("DK2_CREATURE_SPELL_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return;
            Directory.CreateDirectory(artifacts);
            foreach (var width in parent ? new[] { 540, 740 } : new[] { 420, 600 }) foreach (var bottom in new[] { false, true })
            {
                var height = parent ? width == 540 ? 420 : 660 : width == 420 ? 300 : 370; Layout(window, width, height);
                if (parent)
                {
                    var scroll = (ScrollViewer)window.FindName("CreatureSpellPropertiesScroll"); if (bottom) scroll.ScrollToEnd(); else scroll.ScrollToHome(); ((FrameworkElement)window.Content).UpdateLayout();
                    Expect(scroll.ExtentWidth <= scroll.ViewportWidth + 1 && (width != 540 || scroll.ScrollableHeight > 0), "Creature spell horizontal overflow/minimum scroll unreachable.");
                    if (bottom)
                    { var last = Field<CheckBox>((CreatureSpellWindow)window, "CastCreatures"); var bounds = last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(), last.RenderSize)); Expect(bounds.Top >= 0 && bounds.Bottom <= scroll.ActualHeight + 1, "Last creature spell field unreachable."); }
                }
                else
                {
                    var scroll = ((DockPanel)window.Content).Children.OfType<ScrollViewer>().Single();
                    if (bottom) scroll.ScrollToEnd(); else scroll.ScrollToHome(); ((FrameworkElement)window.Content).UpdateLayout();
                    Expect(scroll.ExtentWidth <= scroll.ViewportWidth + 1, "Creature projectile child overflows horizontally.");
                    if (bottom)
                    {
                        var name = window.FindName("CreatureSpellOptionsData2Increase") is Control ? "CreatureSpellOptionsData2Increase" : window.FindName("CreatureSpellOptionsData2") is Control ? "CreatureSpellOptionsData2" : "CreatureSpellOptionsData1Increase";
                        var last = (Control)window.FindName(name); var bounds = last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(), last.RenderSize));
                        Expect(bounds.Top >= 0 && bounds.Bottom <= scroll.ActualHeight + 1, "Last creature projectile child control unreachable after scrolling.");
                    }
                }
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render((FrameworkElement)window.Content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"creature-spell-{state}-{language}-{(document.IsLegacyProject ? "project" : "native")}-{width}-{bottom}.png")); encoder.Save(output);
            }
        }
        var initial = Snapshot(); var initialHistory = document.GetHistory().Undo.Count;
        foreach (var index in Enumerable.Range(0, 41))
        {
            var window = Open(index); var draft = document.CreateCreatureSpellDraft(Row(document, index));
            Expect(window.Title.EndsWith(ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue("CrSpells")[index], Row(document, index).ID.ToString(CultureInfo.InvariantCulture)).Name(language), StringComparison.Ordinal), "Creature spell title lacks original name.");
            foreach (var field in CreatureSpellControls.MainFields)
            {
                var control = Field<Control>(window, field); Expect(control.IsEnabled, "Source creature spell field unexpectedly disabled.");
                if (control is ComboBox combo) Expect(Get<string>(combo.SelectedItem, "ID") == draft.Raw(field), "Creature spell named value guessed.");
                else if (control is TextBox text) Expect(text.Text == draft.Display(field) && Field<Control>(window, field + "Decrease").IsEnabled && Field<Control>(window, field + "Increase").IsEnabled, "Creature spell fixed display/spinner differs.");
                else if (control is CheckBox flag) Expect(flag.IsChecked == (draft.Raw(field) == "1"), "Creature spell checkbox differs.");
            }
            if (index is 0 or 40) Render(window, index == 0 ? "first" : "last", true);
            Expect(window.ApplySpell(), "Creature spell default no-op failed."); window.Close(); Exact(initial);
        }
        Expect(document.GetHistory().Undo.Count == initialHistory, "Opening creature spells materialized history.");
        var two = GlobalProjectiles.Options.First(option => option.Form == 0);
        var edited = Open(); Set(edited, "RechargeTime", "1.5"); Set(edited, "Range", "2.25"); Choose(edited, "Shot", two.ID.ToString(CultureInfo.InvariantCulture));
        foreach (var field in CreatureSpellControls.MainFields.Where(CreatureSpellControls.Boolean)) Field<CheckBox>(edited, field).IsChecked = Read().Raw(field) != "1";
        var options = OpenChild(edited); Option<TextBox>(options, "Data1").Text = two.Signed1 ? "-9" : "111"; Option<TextBox>(options, "Data2").Text = two.Signed2 ? "-7" : "222";
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(Option<TextBox>(options, "Data1").Text == (two.Signed1 ? "-9" : "111"), "Creature spell child translation lost pending input."); LocalizationService.SetLanguage(language);
        Render(options, "selected-child", false); Option<Button>(options, "OK").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(options.AcceptedDraft is not null, "Creature spell child OK failed."); Exact(initial);
        var canceledChild = OpenChild(edited); Option<TextBox>(canceledChild, "Data1").Text = "999"; canceledChild.Close(); Exact(initial);
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(Field<TextBox>(edited, "Range").Text == "2.25", "Creature spell parent translation lost input."); LocalizationService.SetLanguage(language);
        Render(edited, "selected", true); var notices = 0; void Notice(object? sender, EventArgs args) => notices++; document.Changed += Notice;
        ((Button)edited.FindName("CreatureSpellPropertiesApply")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(notices == 1 && document.GetHistory().Undo.Count == initialHistory + 1 && Read().Raw("RechargeTime") == "6144" && Read().Raw("Range") == "9216" && Read().Display("ShotData1") == (two.Signed1 ? "-9" : "111") && Read().Display("ShotData2") == (two.Signed2 ? "-7" : "222"), "Creature spell parent/child did not apply atomically.");
        var applied = Snapshot(); Expect(edited.ApplySpell(), "Creature spell no-op failed."); Exact(applied); Expect(notices == 1, "Creature spell no-op notified/consumed history."); document.Changed -= Notice;
        document.Undo(); Exact(initial); Expect(!edited.ApplySpell() && !edited.OpenOptions("Shot"), "Stale creature spell parent accepted late edit."); document.Redo(); Exact(applied); document.Undo(); Exact(initial); edited.Close();
        foreach (var option in GlobalProjectiles.Options)
        {
            var window = Open(); Choose(window, "Shot", option.ID.ToString(CultureInfo.InvariantCulture));
            if (option.Form == 255) { Expect(!window.OpenOptions("Shot"), "Creature no-options projectile opened child."); window.Close(); continue; }
            var form = OpenChild(window); Expect(form.FindName("CreatureSpellOptionsData1") is (ComboBox or TextBox), "Creature spell first operand missing.");
            Expect((form.FindName("CreatureSpellOptionsData2") is not null) == (option.Form != 1), "Creature spell source form1 second operand mismatch.");
            if (option.Form == 3) Expect(form.FindName("CreatureSpellOptionsData2") is ComboBox, "Creature spell level catalogue missing.");
            if (option.Form == 4)
            {
                ChildChoose(form, "Data1", "1"); Expect(Option<TextBox>(form, "Data2").IsEnabled, "Creature gold object did not enable amount.");
                ChildChoose(form, "Data1", GlobalProjectiles.Targets(option).First(choice => choice.Value != "1").Value); Expect(!Option<TextBox>(form, "Data2").IsEnabled && !Option<Control>(form, "Data2Increase").IsEnabled, "Creature non-gold object enabled amount/spinner.");
            }
            if (option.ID == GlobalProjectiles.Options.First(item => item.Form == option.Form).ID) Render(form, "form-" + option.Form, false);
            form.Close(); Expect(!form.AcceptOptions(), "Closed creature spell child accepted late OK."); window.Close(); Exact(initial);
        }
        var invalid = Open(); Set(invalid, "Range", "1e2"); Set(invalid, "RechargeTime", "1"); Expect(!invalid.ApplySpell() && !invalid.OpenOptions("Shot"), "Invalid creature spell input partly applied/opened child."); Exact(initial); invalid.Close();
        var step = Open(); Set(step, "Range", document.IsLegacyProject ? "1024" : "15"); Expect(step.StepNumber("Range", 1) && Field<TextBox>(step, "Range").Text == (document.IsLegacyProject ? "1024.0" : "15"), "Creature spell stepper exceeds source/native whole limit."); Set(step, "Range", "0"); Expect(step.StepNumber("Range", -1) && Field<TextBox>(step, "Range").Text == "0.0", "Creature spell negative step not clamped."); step.Close(); Exact(initial);
        var canceled = Open(); Choose(canceled, "Shot", two.ID.ToString(CultureInfo.InvariantCulture)); var savedChild = OpenChild(canceled); Option<TextBox>(savedChild, "Data1").Text = "888"; Expect(savedChild.AcceptOptions(), "Creature canceled-parent private child failed.");
        ((Button)canceled.FindName("CreatureSpellPropertiesCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial); Expect(!canceled.ApplySpell(), "Closed creature spell parent applied.");
        var staleParent = Open(); Choose(staleParent, "Shot", two.ID.ToString(CultureInfo.InvariantCulture)); var staleChild = OpenChild(staleParent);
        document.SetGlobalDefinitionValues([new("CrSpells", 0, "ShotData2", Read().Raw("ShotData2") == "0" ? "1" : "0")]); Expect(!staleChild.AcceptOptions() && !staleParent.ApplySpell(), "Changed creature spell baseline accepted child/parent."); staleParent.Close(); document.Undo(); Exact(initial);
        document.SetGlobalDefinitionValues([new("CrSpells", 0, "Shot", "254")]); var imported = Snapshot(); var retained = Open(); Render(retained, "imported", true);
        Expect(retained.ApplySpell() && !retained.OpenOptions("Shot"), "Unknown creature spell projectile changed/invented child."); Exact(imported); Choose(retained, "Shot", "0"); Expect(retained.ApplySpell(), "Creature spell imported repair failed."); retained.Close(); document.Undo(); Exact(imported); document.Undo(); Exact(initial);
        if (document.IsLegacyProject)
        {
            document.SetGlobalDefinitionValues([new("CrSpells", 0, "Range", "10000000")]); var record = Row(document).Record!; document.SetField(record, record.Fields.Single(field => field.Name == "Attacking"), "2");
            var unusual = Snapshot(); var window = Open(); Expect(Field<CheckBox>(window, "Attacking").IsThreeState && Field<CheckBox>(window, "Attacking").IsChecked is null && window.ApplySpell(), "Imported creature BOOL/wide number lost."); Exact(unusual); window.Close(); document.Undo(); document.Undo(); Exact(initial);
        }
        CreatureSpellWindow? pending = null; var reentryRejected = false;
        pending = Open(commit: () => { reentryRejected = !pending!.ApplySpell() && !pending.OpenOptions("Shot") && !pending.StepNumber("Range", 1); Set(pending, "Range", "3"); return true; });
        Set(pending, "Range", "2"); Expect(!pending.ApplySpell() && reentryRejected, "Creature spell reentry/input-revision guard failed."); Exact(initial); pending.Close();
        var pendingExternal = Open(commit: () => { document.SetGlobalDefinitionValues([new("CrSpells", 0, "Range", "12288")]); return true; }); Set(pendingExternal, "Range", "2");
        Expect(!pendingExternal.ApplySpell() && Read().Raw("Range") == "12288", "External pending commit overwritten by creature spell parent."); pendingExternal.Close(); document.Undo(); Exact(initial);
        var current = true; var late = Open(ownerCurrent: () => current); Choose(late, "Shot", two.ID.ToString(CultureInfo.InvariantCulture)); var lateChild = OpenChild(late); current = false;
        Expect(!late.ApplySpell() && !lateChild.AcceptOptions() && !late.StepNumber("Range", 1), "Owner replacement accepted creature spell/child edit."); late.Close(); Exact(initial);
        using (var session = document.CreateCreatureGlobalsSession())
        {
            var window = Open(working: session.WorkingDocument, ownerCurrent: () => document.IsCreatureGlobalsSessionCurrent(session)); Choose(window, "Shot", two.ID.ToString(CultureInfo.InvariantCulture)); Set(window, "Range", "1.25"); var nested = OpenChild(window); Option<TextBox>(nested, "Data1").Text = "123";
            Expect(nested.AcceptOptions() && window.ApplySpell(), "Creature spell private shared-row composition failed."); window.Close(); Exact(initial);
            session.WorkingDocument.SetGlobalDefinitionValues([new("Creatures", 0, "Health", "333")]); var plan = document.PrepareCreatureGlobals(session); var count = document.GetHistory().Undo.Count;
            document.ApplyCreatureGlobals(plan); Expect(document.GetHistory().Undo.Count == count + 1 && Read().Raw("Range") == "5120" && Read().Raw("ShotData1") == "123", "Shared creature/spell UI lost private child or split Undo."); document.Undo(); Exact(initial);
        }
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} complete creature spell row page:9 main/2 nested,41 rows/66 shots, private child/Cancel/atomic Apply, translation/imports/source-native-limits/stale/reentry/shared context; nine-page parent command not covered");
    }
}
