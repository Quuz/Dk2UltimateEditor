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
    private static void CheckGlobalRoom(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9;
        void LayoutWorkspace() { workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout(); }
        LayoutWorkspace();
        var family = Control<ComboBox>(workspace, "DefinitionFamily");
        family.SelectedItem = family.Items.Cast<object>().Single(item => Get<string>(item, "ID") == "Rooms");
        LayoutWorkspace();
        var picker = Control<ComboBox>(workspace, "DefinitionPicker");
        GlobalRoomWindow? active = null;
        workspace.RoomPropertiesPresenter = window => active = window;
        GlobalDefinitionRow Row(int index) => document.GlobalDefinitionRows().Single(row => row.Family == "Rooms" && row.Index == index);
        GlobalRoomDraft Read(int index = 6) => document.CreateGlobalRoomDraft(Row(index));
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes)
        { Expect(document.Files.Count == bytes.Count && document.Files.All(file => bytes[file].AsSpan().SequenceEqual(file.Data.Span)), "Room UI changed private/cancel/no-op/undo bytes."); }
        void Layout(GlobalRoomWindow window, int width = 700, int height = 580)
        { var content = (FrameworkElement)window.Content; window.Width = width; window.Height = height; content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout(); }
        GlobalRoomWindow Open(int index = 6)
        {
            active?.Close(); active = null;
            picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<GlobalDefinitionRow>(item, "Row").Index == index);
            LayoutWorkspace(); var button = Control<Button>(workspace, "DefinitionRoomProperties");
            Expect(button.Visibility == Visibility.Visible && button.IsEnabled, "Actual room properties command unavailable.");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(active is not null, "Actual room command did not open its window."); Layout(active!); return active!;
        }
        T WindowControl<T>(GlobalRoomWindow window, string name) where T : Control => (T)window.FindName(name);
        T Field<T>(GlobalRoomWindow window, string field) where T : Control => WindowControl<T>(window, "RoomProperty" + field.Replace("[", "").Replace("]", ""));
        void Set(GlobalRoomWindow window, string field, string value) => Field<TextBox>(window, field).Text = value;
        void Choose(GlobalRoomWindow window, string field, string value)
        { var combo = Field<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        void Render(GlobalRoomWindow window, string state)
        {
            if (Environment.GetEnvironmentVariable("DK2_ROOM_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return;
            Directory.CreateDirectory(artifacts);
            foreach (var width in new[] { 540, 700 }) foreach (var bottom in new[] { false, true })
            {
                var height = width == 540 ? 420 : 580; Layout(window, width, height);
                var scroll = WindowControl<ScrollViewer>(window, "RoomPropertiesScroll");
                if (bottom) scroll.ScrollToEnd(); else scroll.ScrollToHome(); ((FrameworkElement)window.Content).UpdateLayout();
                Expect(scroll.ScrollableHeight > 0 && scroll.ExtentWidth <= scroll.ViewportWidth + 1, "Complete room page overflows horizontally/cannot scroll.");
                if (bottom)
                {
                    var last = Field<TextBox>(window, "TorchLightRad"); var bounds = last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(), last.RenderSize));
                    Expect(bounds.Top >= 0 && bounds.Bottom <= scroll.ActualHeight + 1, "Last room control cannot be reached.");
                }
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render((FrameworkElement)window.Content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"room-{state}-{language}-{(document.IsLegacyProject ? "project" : "native")}-{width}-{bottom}.png")); encoder.Save(output);
            }
        }
        var initial = Snapshot();
        foreach (var index in Enumerable.Range(0, 24))
        {
            var window = Open(index); var draft = Read(index);
            Expect(window.Title.EndsWith(LocalizationService.Get("Terrain." + Row(index).ID), StringComparison.Ordinal), "Room title is not the original named type.");
            foreach (var field in GlobalRoomControls.Fields)
            {
                var control = Field<Control>(window, field);
                Expect(control.IsEnabled == (draft.Editable && GlobalRoomControls.Enabled(index, field)), "Actual room control availability differs.");
                if (control is ComboBox combo)
                    Expect(Get<string>(combo.SelectedItem, "ID") == draft.Raw(field), "Room opening guessed the stored effect/object.");
            }
            Expect(!Field<ComboBox>(window, "BattleEffect").IsEnabled, "Permanently inactive original BattleEffect enabled.");
            if (index == 0) Render(window, "heart");
            if (index == 6) Render(window, "torture");
            window.Close(); Exact(initial);
        }
        var imported = Open(); Render(imported, "imported"); Set(imported, "Cost", "4321");
        var label = Get<string>(Field<ComboBox>(imported, "Effects[0]").Items[2], "Label");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(Field<TextBox>(imported, "Cost").Text == "4321" && Get<string>(Field<ComboBox>(imported, "Effects[0]").Items[2], "Label") != label, "Room translation lost draft/names.");
        LocalizationService.SetLanguage(language); WindowControl<Button>(imported, "RoomPropertiesCancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Exact(initial);

        var window2 = Open(); Choose(window2, "Effects[0]", "370"); Choose(window2, "Effects[1]", "369");
        for (var slot = 0; slot < 6; slot++) Choose(window2, $"Objects[{slot}]", (slot + 1).ToString());
        foreach (var (field, value) in new[] { ("Cost", "65535"), ("TorchIntensity", "255"), ("TorchLightH", "8.999755859375"), ("TorchLightRad", "3.999755859375") }) Set(window2, field, value);
        window2.ColourSelector = colour => 0x332211;
        Field<Button>(window2, "TorchLightColour").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(Field<TextBox>(window2, "TorchLightR").Text == "17" && Field<TextBox>(window2, "TorchLightG").Text == "34" && Field<TextBox>(window2, "TorchLightB").Text == "51", "Room private COLORREF channel order differs.");
        Render(window2, "selected"); Exact(initial);
        var changed = 0; document.Changed += (_, _) => changed++;
        WindowControl<Button>(window2, "RoomPropertiesApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(changed == 1 && Read().Raw("Cost") == "65535" && Read().Raw("TorchLightH") == "36863" && Read().Raw("TorchLightRad") == "16383" && Read().Raw("TorchLightB") == "51", "Room Apply not one complete edit.");
        var applied = Snapshot(); Expect(window2.ApplyRoom(), "Room no-op failed."); Exact(applied); Expect(changed == 1, "Room no-op created history.");
        document.Undo(); Exact(initial); Expect(!window2.ApplyRoom(), "Stale room owner Undo accepted Apply."); document.Redo(); Exact(applied); document.Undo(); Exact(initial); window2.Close();
        var invalid = Open(); Set(invalid, "Cost", "222"); Set(invalid, "TorchIntensity", "invalid"); Expect(!invalid.ApplyRoom(), "Invalid room partly applied a valid field."); Exact(initial); invalid.Close();
        var cancelled = Open(); cancelled.ColourSelector = colour => null; Expect(!cancelled.ChooseColour() && !cancelled.HasPendingInputs, "Room cancelled colour changed draft.");
        cancelled.ColourSelector = colour => { Set(cancelled, "Cost", "333"); return 0x123456; };
        Expect(!cancelled.ChooseColour() && Field<TextBox>(cancelled, "Cost").Text == "333", "Room late colour replaced newer input.");
        cancelled.ColourSelector = colour => { Expect(!cancelled.ApplyRoom() && !cancelled.ChooseColour(), "Room reentrant colour accepted."); return 0x123456; };
        Expect(cancelled.ChooseColour(), "Guarded room colour failed."); cancelled.Close(); Exact(initial); Expect(!cancelled.ApplyRoom(), "Closed room Apply accepted.");
        document.SetGlobalDefinitionValues([new("Rooms", 6, "BattleEffect", "65000"), new("Rooms", 6, "Effects[5]", "64000"), new("Rooms", 6, "Effects[0]", "63000")]);
        var unknownBytes = Snapshot(); var retained = Open();
        Expect(Get<string>(Field<ComboBox>(retained, "BattleEffect").SelectedItem, "ID") == "65000" && Get<string>(Field<ComboBox>(retained, "Effects[5]").SelectedItem, "ID") == "64000", "Inactive room import guessed.");
        Expect(retained.ApplyRoom(), "Unknown room no-op failed."); Exact(unknownBytes); retained.Close(); document.Undo(); Exact(initial);

        GlobalRoomWindow? pending = null; var reentrantRejected = false;
        pending = new(document, Row(6), () => true, () => { reentrantRejected = !pending!.ApplyRoom(); Set(pending, "Cost", "444"); return true; });
        Set(pending, "Cost", "222"); Expect(!pending.ApplyRoom() && reentrantRejected, "Room Apply ignored pending/input/reentrant guard."); Exact(initial); pending.Close();
        var staleTarget = new GlobalRoomWindow(document, Row(6), () => true, () => { document.SetGlobalDefinitionValues([new("Rooms", 6, "Cost", "555")]); return true; });
        Set(staleTarget, "Cost", "222"); Expect(!staleTarget.ApplyRoom() && Read().Raw("Cost") == "555", "Room pending commit overwrote newer data."); staleTarget.Close(); document.Undo(); Exact(initial);
        document.SetGlobalDefinitionValues([new("Rooms", 6, "Cost", "111")]); var materialized = Snapshot();
        var genericPending = Open(); Set(genericPending, "Cost", "222");
        Control<Button>(workspace, "DefinitionProperties").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); LayoutWorkspace();
        var grid = Control<DataGrid>(workspace, "PropertyGrid");
        var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == "Cost");
        grid.SelectedItem = property; grid.ScrollIntoView(property, grid.Columns[1]); LayoutWorkspace(); grid.CurrentCell = new(property, grid.Columns[1]);
        Expect(grid.BeginEdit(), "Actual generic room edit did not start."); LayoutWorkspace(); ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "555";
        Expect(!genericPending.ApplyRoom() && Read().Raw("Cost") == "555", "Actual generic pending room commit overwrote newer data.");
        workspace.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        document.Undo(); Exact(materialized); document.Undo(); Exact(initial); genericPending.Close();
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9; LayoutWorkspace();
        var late = Open(); late.ColourSelector = colour => { workspace.Document = null; return 0x123456; };
        Expect(!late.ChooseColour() && !late.ApplyRoom(), "Closed room owner accepted late colour/Apply."); Exact(initial);
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} complete room page:20 fields/24 rows, CantBuild/BattleEffect/source masks, private COLORREF/Cancel, atomic Apply/Undo, imports/translation, pending/input/owner/reentrant guards");
    }
}
