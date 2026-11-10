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
    private static void CheckGlobalTrap(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9;
        void LayoutWorkspace() { workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout(); }
        LayoutWorkspace(); var family = Control<ComboBox>(workspace, "DefinitionFamily");
        family.SelectedItem = family.Items.Cast<object>().Single(item => Get<string>(item, "ID") == "Traps"); LayoutWorkspace();
        var picker = Control<ComboBox>(workspace, "DefinitionPicker");
        GlobalTrapWindow? active = null; GlobalTrapOptionsWindow? child = null;
        workspace.TrapPropertiesPresenter = window => active = window;
        GlobalDefinitionRow Row(int index = 0) => document.GlobalDefinitionRows().Single(row => row.Family == "Traps" && row.Index == index);
        GlobalTrapDraft Read() => document.CreateGlobalTrapDraft(Row());
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(document.Files.Count == bytes.Count && document.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Trap UI changed private/cancel/no-op/undo bytes.");
        void Layout(Window window, int width = 740, int height = 660)
        { var content = (FrameworkElement)window.Content; window.Width = width; window.Height = height; content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout(); }
        T Field<T>(GlobalTrapWindow window, string field) where T : Control => (T)window.FindName("TrapProperty" + field);
        T Option<T>(GlobalTrapOptionsWindow window, string field) where T : Control => (T)window.FindName("TrapOptions" + field);
        void Set(GlobalTrapWindow window, string field, string value) => Field<TextBox>(window, field).Text = value;
        void Choose(GlobalTrapWindow window, string field, string value)
        { var combo = Field<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        void ChildChoose(GlobalTrapOptionsWindow window, string field, string value)
        { var combo = Option<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        GlobalTrapWindow Open(int index = 0)
        {
            active?.Close(); active = null;
            picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<GlobalDefinitionRow>(item, "Row").Index == index);
            LayoutWorkspace(); var button = Control<Button>(workspace, "DefinitionTrapProperties");
            Expect(button.Visibility == Visibility.Visible && button.IsEnabled, "Actual trap command unavailable.");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(active is not null, "Actual trap command did not open.");
            active!.OptionsPresenter = window => { child = window; Layout(window, 600, 370); }; Layout(active); return active;
        }
        GlobalTrapOptionsWindow OpenChild(GlobalTrapWindow window, bool trigger)
        {
            child = null; Field<Button>(window, (trigger ? "TriggerType" : "ShotType") + "Options").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(child is not null, "Actual trap options command did not open child.");
            Expect(!window.ApplyTrap() && !window.StepNumber("Health", 1), "Parent editable while modal child pending."); return child!;
        }
        void Render(Window window, string state, bool parent)
        {
            if (Environment.GetEnvironmentVariable("DK2_TRAP_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return;
            Directory.CreateDirectory(artifacts);
            foreach (var width in parent ? new[] { 540, 740 } : new[] { 420, 600 }) foreach (var bottom in new[] { false, true })
            {
                var height = parent ? width == 540 ? 420 : 660 : width == 420 ? 300 : 370; Layout(window, width, height);
                if (parent)
                {
                    var scroll = (ScrollViewer)window.FindName("TrapPropertiesScroll"); if (bottom) scroll.ScrollToEnd(); else scroll.ScrollToHome(); ((FrameworkElement)window.Content).UpdateLayout();
                    Expect(scroll.ScrollableHeight > 0 && scroll.ExtentWidth <= scroll.ViewportWidth + 1, "Trap page overflows horizontally/cannot scroll.");
                    if (bottom)
                    {
                        var last = Field<CheckBox>((GlobalTrapWindow)window, "FPObstacle"); var bounds = last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(), last.RenderSize));
                        Expect(bounds.Top >= 0 && bounds.Bottom <= scroll.ActualHeight + 1, "Last trap field is unreachable.");
                    }
                }
                else
                {
                    var scroll=((DockPanel)window.Content).Children.OfType<ScrollViewer>().Single();
                    if(bottom)scroll.ScrollToEnd();else scroll.ScrollToHome();((FrameworkElement)window.Content).UpdateLayout();
                    Expect(scroll.ExtentWidth<=scroll.ViewportWidth+1,"Trap child horizontal overflow.");
                    if(bottom)
                    {
                        var suffix=window.FindName("TrapOptionsData2Increase") is Control?"Data2Increase":window.FindName("TrapOptionsData2") is Control?"Data2":window.FindName("TrapOptionsData1Increase") is Control?"Data1Increase":"Data1";
                        var last=(Control)window.FindName("TrapOptions"+suffix);var bounds=last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(),last.RenderSize));
                        Expect(bounds.Top>=0&&bounds.Bottom<=scroll.ActualHeight+1,"Last trap child operand/step unreachable.");
                    }
                    foreach(var suffix in new[]{"OK","Cancel"})
                    {
                        var button=(Button)window.FindName("TrapOptions"+suffix);var bounds=button.TransformToAncestor((FrameworkElement)window.Content).TransformBounds(new Rect(new Point(),button.RenderSize));
                        Expect(button.Visibility==Visibility.Visible&&button.RenderSize.Height>0&&bounds.Top>=0&&bounds.Bottom<=height+1,"Trap child footer unreachable.");
                    }
                }
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render((FrameworkElement)window.Content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"trap-{state}-{language}-{(document.IsLegacyProject ? "project" : "native")}-{width}-{bottom}.png")); encoder.Save(output);
            }
        }
        var initial = Snapshot(); var initialHistory = document.GetHistory().Undo.Count;
        foreach (var index in Enumerable.Range(0, 15))
        {
            var window = Open(index); var draft = document.CreateGlobalTrapDraft(Row(index));
            Expect(window.Title.EndsWith(ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue("Traps")[index], Row(index).ID.ToString(CultureInfo.InvariantCulture)).Name(language), StringComparison.Ordinal), "Trap title lacks original type name.");
            foreach (var field in GlobalTrapControls.MainFields)
            {
                var control = Field<Control>(window, field); var enabled = GlobalTrapControls.Enabled(index, field);
                Expect(control.IsEnabled == enabled, "Source trap field availability differs: " + field);
                if (control is ComboBox combo) Expect(Get<string>(combo.SelectedItem, "ID") == draft.Raw(field), "Trap opening guessed a named value.");
                else if (control is TextBox text)
                {
                    Expect(text.Text == draft.Display(field) && Field<Control>(window, field + "Decrease").IsEnabled == enabled && Field<Control>(window, field + "Increase").IsEnabled == enabled, "Disabled Magic Door spinner enabled/source numeric display lost.");
                    if (!enabled) Expect(!window.StepNumber(field, 1), "Inactive numeric source field accepted a step.");
                }
            }
            Expect(window.FindName("TrapPropertyShotDelay") is null, "Hidden ShotDelay exposed.");
            if (index is 0 or 14) Render(window, index == 0 ? "alarm" : "magic-door", true);
            Expect(window.ApplyTrap(), "Trap default no-op failed."); window.Close(); Exact(initial);
        }
        Expect(document.GetHistory().Undo.Count == initialHistory, "Opening trap definitions materialized history.");
        var edited = Open(); Set(edited, "GoldCost", "65535"); Set(edited, "Health", "1234"); Set(edited, "ChargeTime", "1.5"); Choose(edited, "Material", "7");
        Choose(edited, "TriggerType", "2"); var triggerChild = OpenChild(edited, true); Option<TextBox>(triggerChild, "Data1").Text = "321";
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); Expect(Option<TextBox>(triggerChild, "Data1").Text == "321", "Child translation lost pending input."); LocalizationService.SetLanguage(language);
        Render(triggerChild, "trigger", false); Option<Button>(triggerChild, "OK").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(triggerChild.AcceptedDraft?.Raw("TriggerData") == "321", "Trigger child OK failed."); Exact(initial);
        var cancelledChild = OpenChild(edited, true); Option<TextBox>(cancelledChild, "Data1").Text = "999"; cancelledChild.Close(); Exact(initial);
        var two = GlobalProjectiles.Options.First(option => option.Form == 0); Choose(edited, "ShotType", two.ID.ToString(CultureInfo.InvariantCulture));
        var shotChild = OpenChild(edited, false); Option<TextBox>(shotChild, "Data1").Text = two.Signed1 ? "-9" : "111"; Option<TextBox>(shotChild, "Data2").Text = two.Signed2 ? "-7" : "222";
        Render(shotChild, "shot-two", false); Expect(shotChild.AcceptOptions(), "Numeric child OK failed."); Exact(initial); Render(edited, "selected", true);
        var changes = 0; document.Changed += (_, _) => changes++;
        ((Button)edited.FindName("TrapPropertiesApply")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(changes == 1 && Read().Raw("GoldCost") == "65535" && Read().Raw("Health") == "1234" && Read().Raw("ChargeTime") == "6144" && Read().Raw("TriggerData") == "321" && Read().Display("ShotData1") == (two.Signed1 ? "-9" : "111"), "Parent/children did not apply in one complete edit.");
        var applied = Snapshot(); Expect(edited.ApplyTrap(), "Trap no-op failed."); Exact(applied); Expect(changes == 1, "Trap no-op consumed history.");
        document.Undo(); Exact(initial); Expect(!edited.ApplyTrap() && !edited.OpenOptions(true), "Stale parent accepted Apply/child."); document.Redo(); Exact(applied); document.Undo(); Exact(initial); edited.Close();
        foreach (var option in GlobalProjectiles.Options)
        {
            var window = Open(); Choose(window, "ShotType", option.ID.ToString(CultureInfo.InvariantCulture));
            if (option.Form == 255) { Expect(!window.OpenOptions(false), "No-options projectile opened a child."); window.Close(); continue; }
            var form = OpenChild(window, false);
            Expect(form.FindName("TrapOptionsData1") is (ComboBox or TextBox), "Source first operand missing.");
            Expect((form.FindName("TrapOptionsData2") is not null) == (option.Form != 1), "Source form1 second operand exposed/lost.");
            if (option.Form == 3) Expect(form.FindName("TrapOptionsData2") is ComboBox, "Source level catalogue missing.");
            if (option.Form == 4)
            {
                ChildChoose(form, "Data1", "1"); Expect(Option<TextBox>(form, "Data2").IsEnabled, "Gold object did not enable its amount.");
                ChildChoose(form, "Data1", GlobalProjectiles.Targets(option).First(choice => choice.Value != "1").Value);
                Expect(!Option<TextBox>(form, "Data2").IsEnabled && !Option<Control>(form, "Data2Increase").IsEnabled, "Non-gold object enabled amount/spinner.");
            }
            if (option.ID == GlobalProjectiles.Options.First(item => item.Form == option.Form).ID) Render(form, "shot-form-" + option.Form, false);
            form.Close(); Expect(!form.AcceptOptions(), "Closed child accepted a late OK."); window.Close(); Exact(initial);
        }
        var objectType = GlobalProjectiles.Options.First(option => option.Form == 4); var objectID = GlobalProjectiles.Targets(objectType).First(choice => choice.Value != "1").Value;
        document.SetGlobalDefinitionValues([new("Traps", 0, "ShotType", objectType.ID.ToString(CultureInfo.InvariantCulture)), new("Traps", 0, "ShotData1", objectID), new("Traps", 0, "ShotData2", "1234")]);
        var importedObject = Snapshot(); var objects = Open(); Expect(objects.ApplyTrap(), "Object parent no-op failed."); Exact(importedObject);
        var objectChild = OpenChild(objects, false); Expect(objectChild.AcceptOptions(), "Explicit non-gold child OK failed."); Exact(importedObject);
        Expect(objects.ApplyTrap() && Read().Raw("ShotData2") == "0", "Explicit child Save did not clear non-gold argument."); objects.Close(); document.Undo(); Exact(importedObject); document.Undo(); Exact(initial);
        var invalid = Open(); Set(invalid, "Health", "invalid"); Set(invalid, "GoldCost", "222"); Expect(!invalid.ApplyTrap() && !invalid.OpenOptions(true), "Invalid main controls partly applied/opened child."); Exact(initial); invalid.Close();
        var cancelled = Open(); Set(cancelled, "Health", "222"); Choose(cancelled, "TriggerType", "1"); var savedChild = OpenChild(cancelled, true); Option<TextBox>(savedChild, "Data1").Text = "888"; Expect(savedChild.AcceptOptions(), "Private cancelled-parent child failed.");
        ((Button)cancelled.FindName("TrapPropertiesCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial); Expect(!cancelled.ApplyTrap(), "Closed parent accepted Apply.");
        var staleParent = Open(); Choose(staleParent, "TriggerType", "1"); var staleChild = OpenChild(staleParent, true); document.SetGlobalDefinitionValues([new("Traps", 0, "ShotDelay", Read().Raw("ShotDelay") == "0" ? "4096" : "0")]);
        Expect(!staleChild.AcceptOptions() && !staleParent.ApplyTrap(), "Changed hidden baseline accepted stale child/parent."); staleParent.Close(); document.Undo(); Exact(initial);
        document.SetGlobalDefinitionValues([new("Traps", 0, "Material", "254"), new("Traps", 0, "DeathEffect", "63000"), new("Traps", 0, "TriggerType", "253"), new("Traps", 0, "ShotType", "252"), new("Traps", 0, "Health", document.IsLegacyProject ? "100000" : "65535")]);
        var imported = Snapshot(); var retained = Open(); Render(retained, "imported", true); Expect(retained.ApplyTrap() && !retained.OpenOptions(true) && !retained.OpenOptions(false), "Imported unknown parent changed/invented options."); Exact(imported);
        Choose(retained, "Material", "0"); Expect(retained.ApplyTrap() && Read().Raw("DeathEffect") == "63000" && Read().Raw("TriggerType") == "253", "Trap import repair normalized another field."); retained.Close(); document.Undo(); Exact(imported); document.Undo(); Exact(initial);
        GlobalTrapWindow? pending = null; var reentrantRejected = false;
        pending = new(document, Row(), () => true, () => { reentrantRejected = !pending!.ApplyTrap() && !pending.OpenOptions(true) && !pending.StepNumber("Health", 1); Set(pending, "Health", "444"); return true; });
        Set(pending, "Health", "222"); Expect(!pending.ApplyTrap() && reentrantRejected, "Trap pending/input/reentrant guard failed."); Exact(initial); pending.Close();
        document.SetGlobalDefinitionValues([new("Traps", 0, "Health", "111")]); var materialized = Snapshot();
        var genericPending = Open(); Set(genericPending, "Health", "222");
        Control<Button>(workspace, "DefinitionProperties").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); LayoutWorkspace();
        var grid = Control<DataGrid>(workspace, "PropertyGrid"); var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == "Health");
        grid.SelectedItem = property; grid.ScrollIntoView(property, grid.Columns[1]); LayoutWorkspace(); grid.CurrentCell = new(property, grid.Columns[1]);
        Expect(grid.BeginEdit(), "Generic pending trap edit did not start."); LayoutWorkspace(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "555";
        Expect(!genericPending.ApplyTrap() && Read().Raw("Health") == "555", "Actual generic pending commit was overwritten by parent.");
        workspace.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        document.Undo(); Exact(materialized); document.Undo(); Exact(initial); genericPending.Close();
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9; LayoutWorkspace();
        var late = Open(); Choose(late, "TriggerType", "1"); var lateChild = OpenChild(late, true); workspace.Document = null;
        Expect(!late.ApplyTrap() && !lateChild.AcceptOptions() && !late.StepNumber("Health", 1), "Closed workspace accepted trap/child edit."); Exact(initial);
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} complete trap page:24 main/3 nested/1 hidden,15 rows/66 shot forms, MagicDoor controls/spinners, private children/Cancel/atomic Apply, source object Save, translation/imports/pending/owner/stale/reentrant guards");
    }
}
