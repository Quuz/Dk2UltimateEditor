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
    private static void CheckGlobalDoor(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9;
        void LayoutWorkspace() { workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout(); }
        LayoutWorkspace();
        var family = Control<ComboBox>(workspace, "DefinitionFamily");
        family.SelectedItem = family.Items.Cast<object>().Single(item => Get<string>(item, "ID") == "Doors"); LayoutWorkspace();
        var picker = Control<ComboBox>(workspace, "DefinitionPicker");
        GlobalDoorWindow? active = null; workspace.DoorPropertiesPresenter = window => active = window;
        GlobalDefinitionRow Row(int index = 0) => document.GlobalDefinitionRows().Single(row => row.Family == "Doors" && row.Index == index);
        GlobalDoorDraft Read() => document.CreateGlobalDoorDraft(Row());
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes)
        { Expect(document.Files.Count == bytes.Count && document.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Door UI changed private/cancel/no-op/undo bytes."); }
        void Layout(GlobalDoorWindow window, int width = 700, int height = 580)
        { var content = (FrameworkElement)window.Content; window.Width = width; window.Height = height; content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout(); }
        GlobalDoorWindow Open(int index = 0)
        {
            active?.Close(); active = null;
            picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<GlobalDefinitionRow>(item, "Row").Index == index);
            LayoutWorkspace(); var button = Control<Button>(workspace, "DefinitionDoorProperties");
            Expect(button.Visibility == Visibility.Visible && button.IsEnabled, "Actual door properties command unavailable.");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(active is not null, "Actual door command did not open."); Layout(active!); return active!;
        }
        T WindowControl<T>(GlobalDoorWindow window, string name) where T : Control => (T)window.FindName(name);
        T Field<T>(GlobalDoorWindow window, string field) where T : Control => WindowControl<T>(window, "DoorProperty" + field);
        void Set(GlobalDoorWindow window, string field, string value) => Field<TextBox>(window, field).Text = value;
        void Choose(GlobalDoorWindow window, string field, string value)
        { var combo = Field<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        void Render(GlobalDoorWindow window, string state)
        {
            if (Environment.GetEnvironmentVariable("DK2_DOOR_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return;
            Directory.CreateDirectory(artifacts);
            foreach (var width in new[] { 540, 700 }) foreach (var bottom in new[] { false, true })
            {
                var height = width == 540 ? 420 : 580; Layout(window, width, height);
                var scroll = WindowControl<ScrollViewer>(window, "DoorPropertiesScroll");
                if (bottom) scroll.ScrollToEnd(); else scroll.ScrollToHome(); ((FrameworkElement)window.Content).UpdateLayout();
                Expect(scroll.ScrollableHeight > 0 && scroll.ExtentWidth <= scroll.ViewportWidth + 1, "Door page overflows horizontally/cannot scroll.");
                if (bottom)
                {
                    var last = Field<ComboBox>(window, "Key"); var bounds = last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(), last.RenderSize));
                    Expect(bounds.Top >= 0 && bounds.Bottom <= scroll.ActualHeight + 1, "Last door field cannot be reached.");
                }
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render((FrameworkElement)window.Content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"door-{state}-{language}-{(document.IsLegacyProject ? "project" : "native")}-{width}-{bottom}.png")); encoder.Save(output);
            }
        }
        var initial = Snapshot();
        foreach (var index in Enumerable.Range(0, 8))
        {
            var window = Open(index); var draft = document.CreateGlobalDoorDraft(Row(index));
            var expectedName = ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue("Doors")[index], Row(index).ID.ToString(CultureInfo.InvariantCulture)).Name(language);
            Expect(window.Title.EndsWith(expectedName, StringComparison.Ordinal), "Door title is not its source named type.");
            foreach (var field in GlobalDoorControls.Fields)
            {
                var control = Field<Control>(window, field);
                if (control is ComboBox combo)
                    Expect(combo.IsEnabled && Get<string>(combo.SelectedItem, "ID") == draft.Raw(field), "Door opening guessed/disabled a stored named choice.");
                else
                {
                    Expect(control.IsEnabled == GlobalDoorControls.TextEntryEnabled(field), "Door text entry source availability differs.");
                    Expect(Field<Control>(window, field + "Decrease").IsEnabled == GlobalDoorControls.SpinnerInputEnabled(field) && Field<Control>(window, field + "Increase").IsEnabled == GlobalDoorControls.SpinnerInputEnabled(field), "Measured native buddy spinner availability differs.");
                    if (!GlobalDoorControls.SpinnerInputEnabled(field))
                    {
                        var beforeRevision = window.InputRevision;
                        foreach (var buddyDirection in new[] { -1, 1 })
                        {
                            Field<Control>(window, field + (buddyDirection < 0 ? "Decrease" : "Increase")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            Expect(!window.StepNumber(field, buddyDirection), "Disabled buddy accepted either arrow through the public route.");
                        }
                        Expect(window.InputRevision == beforeRevision && Field<TextBox>(window, field).Text == draft.Display(field) && !window.HasPendingInputs, "Disabled buddy click altered its draft/revision.");
                    }
                }
            }
            if (index == 0) Render(window, "barricade"); if (index == 4) Render(window, "magic");
            Expect(window.ApplyDoor(), "Door default no-op failed."); window.Close(); Exact(initial);
        }
        var window2 = Open(); Set(window2, "Cost", "65535"); Set(window2, "BuildPoints", "1234"); Set(window2, "Health", "5678"); Set(window2, "HealthGain", "999");
        Choose(window2, "Material", "7"); Choose(window2, "DeathEffect", "370"); Choose(window2, "Crate", "57"); Choose(window2, "Key", "137");
        var mana = long.Parse(Read().Raw("ManaCost"), CultureInfo.InvariantCulture); var direction = mana == 65535 ? -1 : 1;
        var revision = window2.InputRevision;
        Field<Control>(window2, "ManaCost" + (direction < 0 ? "Decrease" : "Increase")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var expectedMana = mana.ToString(CultureInfo.InvariantCulture);
        Expect(!window2.StepNumber("ManaCost", direction) && window2.InputRevision == revision, "Disabled mana buddy accepted programmatic step or revised draft.");
        Expect(Field<TextBox>(window2, "ManaCost").Text == expectedMana && !Field<TextBox>(window2, "ManaCost").IsEnabled && !Field<Control>(window2, "ManaCostDecrease").IsEnabled && !Field<Control>(window2, "ManaCostIncrease").IsEnabled, "Disabled mana buddy changed after a raised click.");
        Expect(window2.StepNumber("HealthGain", 1) && Field<TextBox>(window2, "HealthGain").Text == "1000", "Enabled numeric buddy step stopped working.");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(Field<TextBox>(window2, "Cost").Text == "65535" && Field<TextBox>(window2, "ManaCost").Text == expectedMana, "Translation lost the numeric draft."); LocalizationService.SetLanguage(language);
        Render(window2, "selected"); Exact(initial);
        var changed = 0; document.Changed += (_, _) => changed++;
        WindowControl<Button>(window2, "DoorPropertiesApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(changed == 1 && Read().Raw("Cost") == "65535" && Read().Raw("ManaCost") == expectedMana && Read().Raw("DeathEffect") == "370" && Read().Raw("Key") == "137", "Door Apply is not one complete edit.");
        var applied = Snapshot(); Expect(window2.ApplyDoor(), "Door no-op failed."); Exact(applied); Expect(changed == 1, "Door no-op created history.");
        document.Undo(); Exact(initial); Expect(!window2.ApplyDoor() && !window2.StepNumber("Cost", 1), "Stale door accepted Apply/step."); document.Redo(); Exact(applied); document.Undo(); Exact(initial); window2.Close();
        var invalid = Open(); Set(invalid, "Cost", "222"); Set(invalid, "Health", "invalid"); Expect(!invalid.ApplyDoor(), "Invalid door partly applied valid fields."); Exact(initial); invalid.Close();
        var cancelled = Open(); Set(cancelled, "Cost", "222"); cancelled.StepNumber("Cost", 1);
        WindowControl<Button>(cancelled, "DoorPropertiesCancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial); Expect(!cancelled.ApplyDoor() && !cancelled.StepNumber("Cost", 1), "Closed door accepted Apply/step.");
        document.SetGlobalDefinitionValues([new("Doors", 0, "Material", "254"), new("Doors", 0, "DeathEffect", "63000"), new("Doors", 0, "Key", "253"), new("Doors", 0, "ManaCost", document.IsLegacyProject ? "100000" : "65535")]);
        var imported = Snapshot(); var retained = Open(); Render(retained, "imported");
        Expect(Get<string>(Field<ComboBox>(retained, "Material").SelectedItem, "ID") == "254" && Get<string>(Field<ComboBox>(retained, "DeathEffect").SelectedItem, "ID") == "63000", "Door import guessed.");
        Expect(retained.ApplyDoor(), "Unknown/wide door no-op failed."); Exact(imported);
        Choose(retained, "Material", "0"); Expect(retained.ApplyDoor() && Read().Raw("Key") == "253" && Read().Raw("DeathEffect") == "63000", "Door repair normalized other imports.");
        retained.Close(); document.Undo(); Exact(imported); document.Undo(); Exact(initial);
        GlobalDoorWindow? pending = null; var reentrantRejected = false;
        pending = new(document, Row(), () => true, () => { reentrantRejected = !pending!.ApplyDoor() && !pending.StepNumber("Cost", 1); Set(pending, "Cost", "444"); return true; });
        Set(pending, "Cost", "222"); Expect(!pending.ApplyDoor() && reentrantRejected, "Door Apply ignored pending/input/reentrant guard."); Exact(initial); pending.Close();
        var staleTarget = new GlobalDoorWindow(document, Row(), () => true, () => { document.SetGlobalDefinitionValues([new("Doors", 0, "Cost", "555")]); return true; });
        Set(staleTarget, "Cost", "222"); Expect(!staleTarget.ApplyDoor() && Read().Raw("Cost") == "555", "Door pending commit overwrote newer data."); staleTarget.Close(); document.Undo(); Exact(initial);
        document.SetGlobalDefinitionValues([new("Doors", 0, "Cost", "111")]); var materialized = Snapshot();
        var genericPending = Open(); Set(genericPending, "Cost", "222");
        Control<Button>(workspace, "DefinitionProperties").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); LayoutWorkspace();
        var grid = Control<DataGrid>(workspace, "PropertyGrid"); var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == "Cost");
        grid.SelectedItem = property; grid.ScrollIntoView(property, grid.Columns[1]); LayoutWorkspace(); grid.CurrentCell = new(property, grid.Columns[1]);
        Expect(grid.BeginEdit(), "Actual generic door edit did not start."); LayoutWorkspace(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "555";
        Expect(!genericPending.ApplyDoor() && Read().Raw("Cost") == "555", "Actual generic pending door commit overwritten.");
        workspace.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        document.Undo(); Exact(materialized); document.Undo(); Exact(initial); genericPending.Close();
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9; LayoutWorkspace();
        var late = Open(); workspace.Document = null;
        Expect(!late.ApplyDoor() && !late.StepNumber("Cost", 1), "Closed workspace accepted door Apply/step."); Exact(initial);
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} complete door page:9 fields/8 rows, source catalogues/ranges, disabled mana buddy/no input, private Cancel, atomic Apply/Undo, imports/translation, pending/input/owner/reentrant guards");
    }
}
