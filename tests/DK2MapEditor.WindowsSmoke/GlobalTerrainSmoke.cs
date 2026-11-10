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
    private static void CheckGlobalTerrain(NativeMapDocument document, string language)
    {
        var workspace = new EditorWorkspace { Document = document };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9;
        void LayoutWorkspace() { workspace.Measure(new Size(1200, 850)); workspace.Arrange(new Rect(0, 0, 1200, 850)); workspace.UpdateLayout(); }
        LayoutWorkspace();
        var picker = Control<ComboBox>(workspace, "DefinitionPicker");
        GlobalTerrainWindow? active = null;
        workspace.TerrainPropertiesPresenter = window => active = window;
        GlobalDefinitionRow Row(int index) => document.GlobalDefinitionRows().Single(row => row.Family == "Terrains" && row.Index == index);
        GlobalTerrainDraft Read(int index = 2) => document.CreateGlobalTerrainDraft(Row(index));
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes)
        { Expect(document.Files.Count == bytes.Count, "Terrain UI added a file before applying."); foreach (var file in document.Files) Expect(bytes[file].AsSpan().SequenceEqual(file.Data.Span), "Terrain UI changed read-only/cancel/undo bytes."); }
        void Layout(GlobalTerrainWindow window, int width = 700, int height = 580)
        { window.Width = width; window.Height = height; var content = (FrameworkElement)window.Content; content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout(); }
        GlobalTerrainWindow Open(int index = 2)
        {
            active?.Close(); active = null;
            picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<GlobalDefinitionRow>(item, "Row").Index == index);
            LayoutWorkspace(); var button = Control<Button>(workspace, "DefinitionTerrainProperties");
            Expect(button.Visibility == Visibility.Visible && button.IsEnabled, "Actual terrain command is unavailable.");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(active is not null, "Actual terrain command did not open its window."); Layout(active!); return active!;
        }
        T WindowControl<T>(GlobalTerrainWindow window, string name) where T : Control => (T)window.FindName(name);
        T Field<T>(GlobalTerrainWindow window, string field) where T : Control => WindowControl<T>(window, "TerrainProperty" + field);
        void Set(GlobalTerrainWindow window, string field, string value) => Field<TextBox>(window, field).Text = value;
        void Choose(GlobalTerrainWindow window, string field, string value)
        { var combo = Field<ComboBox>(window, field); combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == value); }
        void Render(GlobalTerrainWindow window, string state)
        {
            if (Environment.GetEnvironmentVariable("DK2_TERRAIN_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return;
            Directory.CreateDirectory(artifacts);
            foreach (var width in new[] { 540, 700 }) foreach (var bottom in new[] { false, true })
            {
                var height = width == 540 ? 420 : 580; Layout(window, width, height);
                var scroll = WindowControl<ScrollViewer>(window, "TerrainPropertiesScroll");
                if (bottom) scroll.ScrollToEnd(); else scroll.ScrollToHome(); ((FrameworkElement)window.Content).UpdateLayout();
                Expect(scroll.ScrollableHeight > 0 && scroll.ExtentWidth <= scroll.ViewportWidth + 1, "Complete terrain page overflows horizontally or cannot scroll.");
                if (bottom)
                {
                    var last = Field<TextBox>(window, "TerrainLightH"); var bounds = last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(), last.RenderSize));
                    Expect(bounds.Top >= 0 && bounds.Bottom <= scroll.ActualHeight + 1, "Last terrain control is unreachable.");
                }
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render((FrameworkElement)window.Content);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifacts, $"terrain-{state}-{language}-{(document.IsLegacyProject ? "project" : "native")}-{width}-{bottom}.png")); encoder.Save(output);
            }
        }
        var initial = Snapshot();
        foreach (var index in Enumerable.Range(0, 38))
        {
            var window = Open(index); var draft = Read(index);
            if (index == 13) Expect(window.Title.EndsWith(language == "en" ? "Perception field" : "Поле восприятия", StringComparison.Ordinal), "Known original perception-field title remains unknown.");
            foreach (var field in GlobalTerrainControls.Fields)
            {
                var control = WindowControl<Control>(window, "TerrainProperty" + field);
                Expect(control.IsEnabled == (draft.Editable && GlobalTerrainControls.Enabled(index, field)), "Terrain control availability differs from source metadata.");
                if (control is ComboBox combo)
                {
                    var choices = GlobalTerrainControls.Choices(index, field);
                    Expect(combo.Items.Count == choices.Count + (choices.Any(choice => choice.Value == draft.Raw(field)) ? 0 : 1), "Terrain list lost original order/unknown import.");
                    Expect(Get<string>(combo.SelectedItem, "ID") == draft.Raw(field), "Terrain opening guessed an imported ID.");
                }
            }
            if (index == 0) Render(window, "border");
            if (index == 13) Render(window, "room");
            window.Close(); Exact(initial);
        }
        var imported = Open(); Render(imported, "imported");
        var typeLabel = Get<string>(Field<ComboBox>(imported, "MaxHealthType").Items[2], "Label");
        Set(imported, "GoldValue", "4321");
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(Field<TextBox>(imported, "GoldValue").Text == "4321" && Get<string>(Field<ComboBox>(imported, "MaxHealthType").Items[2], "Label") != typeLabel, "Live translation lost terrain draft or names.");
        LocalizationService.SetLanguage(language); imported.Close(); Exact(initial);

        var window2 = Open();
        Choose(window2, "MaxHealthType", "8"); Choose(window2, "DestroyedType", "3"); Choose(window2, "MaxHealthEffect", "370"); Choose(window2, "DestroyedEffect", "369");
        foreach (var (field, value) in new[] { ("Damage", "-32768"), ("GoldValue", "65535"), ("StartingHealth", "12345"), ("MaxHealth", "54321"), ("ManaGain", "321"), ("MaxManaGain", "654"), ("WibbleH", "255"), ("WibbleV", "254"), ("TerrainLightH", "16000.999755859375") }) Set(window2, field, value);
        foreach (var field in new[] { "Visible", "TerrainLight", "AmbientLight" }) Field<CheckBox>(window2, field).IsChecked = true;
        window2.ColourSelector = colour => 0x332211;
        Field<Button>(window2, "TerrainLightColour").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window2.ColourSelector = colour => 0xccbbaa;
        Expect(window2.ChooseColour("AmbientLight"), "Private ambient colour did not accept COLORREF.");
        Expect(Field<TextBox>(window2, "TerrainLightR").Text == "17" && Field<TextBox>(window2, "TerrainLightG").Text == "34" && Field<TextBox>(window2, "TerrainLightB").Text == "51", "Original COLORREF channel order differs.");
        Render(window2, "selected"); Exact(initial);
        var changed = 0; document.Changed += (_, _) => changed++;
        WindowControl<Button>(window2, "TerrainPropertiesApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(changed == 1 && Read().Raw("GoldValue") == "65535" && Read().Raw("TerrainLightH") == "65540095" && Read().Raw("AmbientLightB") == "204", "Actual terrain Apply was not one complete edit.");
        var applied = Snapshot(); Expect(window2.ApplyTerrain(), "Unchanged terrain Apply failed."); Exact(applied); Expect(changed == 1, "No-op terrain Apply created history.");
        document.Undo(); Exact(initial); Expect(!window2.ApplyTerrain(), "Owner undo allowed a stale terrain Apply."); document.Redo(); Exact(applied); document.Undo(); Exact(initial); window2.Close();

        var invalid = Open(); Set(invalid, "GoldValue", "222"); Set(invalid, "Damage", "invalid"); Expect(!invalid.ApplyTerrain(), "Invalid terrain range partly applied valid fields."); Exact(initial); invalid.Close();
        var cancelledColour = Open(); cancelledColour.ColourSelector = colour => null; Expect(!cancelledColour.ChooseColour("TerrainLight"), "Cancelled native selection changed draft."); Expect(!cancelledColour.HasPendingInputs, "Colour cancellation retained an invented draft.");
        cancelledColour.ColourSelector = colour => { Set(cancelledColour, "GoldValue", "333"); return 0x123456; };
        Expect(!cancelledColour.ChooseColour("TerrainLight") && Field<TextBox>(cancelledColour, "GoldValue").Text == "333", "Late colour response overwrote newer input.");
        cancelledColour.ColourSelector = colour => { Expect(!cancelledColour.ApplyTerrain() && !cancelledColour.ChooseColour("AmbientLight"), "Reentrant colour route accepted another operation."); return 0x123456; };
        Expect(cancelledColour.ChooseColour("TerrainLight"), "Guarded colour response failed."); cancelledColour.Close(); Exact(initial); Expect(!cancelledColour.ApplyTerrain(), "Closed terrain window applied.");

        var unknown = document.IsLegacyProject ? "100000" : "255";
        document.SetGlobalDefinitionValues([new("Terrains", 2, "MaxHealthType", unknown), new("Terrains", 2, "MaxHealthEffect", "65000"), new("Terrains", 2, "TerrainLight", "1"), new("Terrains", 2, "AmbientLight", "1")]);
        var unknownBytes = Snapshot(); var retained = Open();
        Expect(Get<string>(Field<ComboBox>(retained, "MaxHealthType").SelectedItem, "ID") == unknown && Get<string>(Field<ComboBox>(retained, "MaxHealthEffect").SelectedItem, "ID") == "65000", "Unknown terrain import was guessed.");
        Expect(retained.ApplyTerrain(), "Untouched imported terrain could not apply."); Exact(unknownBytes); retained.Close(); document.Undo(); Exact(initial);
        document.SetGlobalDefinitionValues([new("Terrains", 2, "TerrainLight", "0"), new("Terrains", 2, "AmbientLight", "0"), .. new[] { "TerrainLight", "AmbientLight" }.SelectMany(light => new[] { "R", "G", "B" }.Select(channel => new GlobalDefinitionChange("Terrains", 2, light + channel, "42")))]);
        var dirtyLights = Snapshot(); var cleanup = Open(); cleanup.Close(); Exact(dirtyLights); cleanup = Open(); Expect(cleanup.ApplyTerrain(), "Explicit terrain Save cleanup failed.");
        foreach (var light in new[] { "TerrainLight", "AmbientLight" }) foreach (var channel in new[] { "R", "G", "B" }) Expect(Read().Raw(light + channel) == "0", "Disabled terrain light RGB was not cleared on explicit Apply.");
        document.Undo(); Exact(dirtyLights); document.Undo(); Exact(initial); cleanup.Close();

        GlobalTerrainWindow? pending = null; var reentrantRejected = false;
        pending = new(document, Row(2), () => true, () => { reentrantRejected = !pending!.ApplyTerrain(); Set(pending, "GoldValue", "444"); return true; });
        Set(pending, "GoldValue", "222"); Expect(!pending.ApplyTerrain() && reentrantRejected, "Apply ignored input revision/reentrant guard during pending commit."); Exact(initial); pending.Close();
        using (var staleTarget = new TerrainTestWindowLifetime(new GlobalTerrainWindow(document, Row(2), () => true, () => { document.SetGlobalDefinitionValues([new("Terrains", 2, "GoldValue", "555")]); return true; })))
        { Set(staleTarget.Window, "GoldValue", "222"); Expect(!staleTarget.Window.ApplyTerrain() && Read().Raw("GoldValue") == "555", "Pending commit overwrote newer terrain data."); }
        document.Undo(); Exact(initial);
        document.SetGlobalDefinitionValues([new("Terrains", 2, "GoldValue", "111")]);
        var materialized = Snapshot(); var genericPending = Open();
        Set(genericPending, "GoldValue", "222");
        Control<Button>(workspace, "DefinitionProperties").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); LayoutWorkspace();
        var grid = Control<DataGrid>(workspace, "PropertyGrid");
        var property = grid.Items.Cast<object>().Single(item => Get<NativeField>(item, "Field").Name == "GoldValue");
        grid.SelectedItem = property; grid.ScrollIntoView(property, grid.Columns[1]); LayoutWorkspace(); grid.CurrentCell = new(property, grid.Columns[1]);
        Expect(grid.BeginEdit(), "Actual generic terrain field cannot begin editing."); LayoutWorkspace();
        ((TextBox)grid.Columns[1].GetCellContent(property)).Text = "555";
        Expect(!genericPending.ApplyTerrain() && Read().Raw("GoldValue") == "555", "Actual pending generic commit overwrote newer terrain data.");
        workspace.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        document.Undo(); Exact(materialized); document.Undo(); Exact(initial); genericPending.Close();
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9; LayoutWorkspace();
        var late = Open(); late.ColourSelector = colour => { workspace.Document = null; return 0x123456; };
        Expect(!late.ChooseColour("TerrainLight") && !late.ApplyTerrain(), "Closed owner accepted late colour/Apply."); Exact(initial);
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} complete terrain page:22 fields/38 rows, source choices/masks, COLORREF private/cancel/late, atomic Save/Undo, imports, translation, input/owner/reentrant guards");
    }
    private sealed class TerrainTestWindowLifetime(GlobalTerrainWindow window) : IDisposable
    { public GlobalTerrainWindow Window { get; } = window; public void Dispose() => Window.Close(); }
}
