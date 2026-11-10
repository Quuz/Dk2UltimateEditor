using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckStatistics(NativeMapDocument document, string language)
    {
        NativeRecord Band() => document.IsLegacyProject ? document.AddLegacyRecord("Band", new(1, 1))
            : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(1, 1));
        var first = Band(); document.AddBandMember(first, 17, 3); var second = Band(); document.AddBandMember(second, 14, 7);
        document.Paint([new(20, 18)], new(6, 2, 1));
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); tabs.SelectedIndex = 8;
        var host = new Window { Content=workspace, Width=1500, Height=1000, WindowStyle=WindowStyle.None,
            ResizeMode=ResizeMode.NoResize, ShowInTaskbar=false, Left=-10000, Top=-10000 };
        host.Show();
        try
        {
        // DataGrid star columns settle after Loaded and queued binding/layout.
        // Measure/Arrange on an unhosted control leaves ActualWidth at MinWidth.
        void Layout()
        {
            workspace.Measure(new Size(1500,1000));workspace.Arrange(new Rect(0,0,1500,1000));workspace.UpdateLayout();
            host.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);workspace.UpdateLayout();
        }
        Layout();Expect(host.IsLoaded&&workspace.IsLoaded,"Statistics fixture never completed the real Loaded lifecycle.");var sections = Control<TabControl>(workspace, "StatisticsTabs");
        var map = Control<DataGrid>(workspace, "StatisticsMapTable"); var players = Control<DataGrid>(workspace, "StatisticsPlayerTable"); var bands = Control<DataGrid>(workspace, "StatisticsBandTable");
        var playerPicker = Control<ComboBox>(workspace, "StatisticsPlayerPicker"); var bandPicker = Control<ComboBox>(workspace, "StatisticsBandPicker");
        string Value(DataGrid table, string key) => Get<string>(table.Items.Cast<object>().Single(item => Get<string>(item, "Key") == key), "Value");
        string Label(DataGrid table, string key) => Get<string>(table.Items.Cast<object>().Single(item => Get<string>(item, "Key") == key), "Label");
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Statistics UI modified map buffers."); }
        var bytes = Snapshot(); var undo = document.CanUndo; var redo = document.CanRedo;
        Expect(tabs.Items.Count == 10 && sections.Items.Count == 3 && sections.IsEnabled && playerPicker.Items.Count == 7 && bandPicker.Items.Count == 2, "Statistics tabs/pickers not bound.");
        Expect(map.IsReadOnly && players.IsReadOnly && bands.IsReadOnly && map.Columns.Count == 2 && Value(map, "Tiles") == "760" && Value(map, "TerrainGold") == "3000", "Statistics map values/read-only grid wrong.");
        var rendered = map.Columns[1].GetCellContent(map.Items[0]) as TextBlock;
        Expect(rendered is { Text.Length: > 0 }, "Actual statistics DataGrid binding is blank.");
        Console.WriteLine($"STATISTICS LAYOUT {language} {(document.IsLegacyProject?"project":"native")} workspace={workspace.ActualWidth} grid={map.ActualWidth} label={map.Columns[0].ActualWidth} value={map.Columns[1].ActualWidth} rows={map.Items.Count}");
        if (Environment.GetEnvironmentVariable("DK2_STATISTICS_DIAGNOSTICS") is { Length: > 0 } diagnostics)
        {
            Directory.CreateDirectory(diagnostics);var bitmap=new RenderTargetBitmap(1500,1000,96,96,PixelFormats.Pbgra32);bitmap.Render(workspace);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output=File.Create(Path.Combine(diagnostics,$"statistics-initial-{language}-{(document.IsLegacyProject?"project":"native")}.png"));encoder.Save(output);
        }
        Expect(map.Columns[0].ActualWidth >= 620 && map.Columns[1].ActualWidth >= 300, "Statistics columns collapsed and clipped their labels/values.");
        Expect(Control<TextBlock>(workspace, "StatisticsIssues").Text.Length == 0, "Supported statistics fixture reported unknown layouts.");
        sections.SelectedIndex = 1;
        playerPicker.SelectedItem = playerPicker.Items.Cast<object>().Single(item => Get<StatisticsPlayer>(item, "Player").ID == 3); Layout();
        var player = document.CalculateStatistics().Players[2];
        Expect(Value(players, "TreasuryCapacity") == player.TreasuryCapacity!.Value.ToString() && Value(players, "Heart") == $"({player.Heart!.Value.X}, {player.Heart.Value.Y})" && Control<Button>(workspace, "StatisticsShowHeart").IsEnabled, "Player resources/heart grid wrong.");
        sections.SelectedIndex = 2; bandPicker.SelectedIndex = 1; Layout();
        Expect(Value(bands, "BandCount") == "2" && Value(bands, "Dwarfs") == "1" && Value(bands, "AverageLevel") == "7" && Value(bands, "ReleaseActions") == "0", "Band selection/count/average wrong.");
        Expect(Value(bands, "OtherBandCreatures") == "0" && Control<Button>(workspace, "StatisticsBandProperties").Content as string == LocalizationService.Get("Editor.Statistics.BandProperties"), "Band-specific labels confused band properties or creature categories.");
        var label = Label(map, "Tiles"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Label(map, "Tiles") != label && Get<StatisticsPlayer>(playerPicker.SelectedItem, "Player").ID == 3 && Get<StatisticsBand>(bandPicker.SelectedItem, "Band").Record.Offset == second.Offset && sections.SelectedIndex == 2, "Translation lost statistics selection or labels.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout(); Exact(bytes);
        Expect(document.CanUndo == undo && document.CanRedo == redo, "Statistics browsing changed history.");
        Control<Button>(workspace, "StatisticsBandProperties").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(tabs.SelectedIndex == 1 && Get<NativeRecord>(Control<ListBox>(workspace, "RecordList").SelectedItem, "Record").Offset == second.Offset, "Band properties navigation chose another row.");
        tabs.SelectedIndex = 8; sections.SelectedIndex = 1; Control<Button>(workspace, "StatisticsShowHeart").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var heart = player.Heart!.Value;
        Expect(tabs.SelectedIndex == 0 && Control<MapCanvas>(workspace, "Canvas").Selection == new TileRect(heart.X, heart.Y, 1, 1), "Statistics heart navigation wrong."); Exact(bytes);
        tabs.SelectedIndex = 7;
        var picker = Control<ComboBox>(workspace, "PlayerPicker"); picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<PlayerSettingsRow>(item, "Player").ID == 3);
        workspace.PlayerGoldPresenter=window=>window.Acknowledge();
        var gold = Control<TextBox>(workspace, "PlayerGold"); var warning = Control<TextBlock>(workspace, "PlayerGoldWarning"); var resources = Control<TextBlock>(workspace, "PlayerResources");
        gold.Text = "999999"; Layout(); Expect(warning.Visibility == Visibility.Visible && warning.Text == LocalizationService.Get("Editor.Statistics.GoldWarning") && resources.Text.Contains(player.TreasuryCapacity.Value.ToString()), "Draft gold warning/resource summary missing."); Exact(bytes);
        Click(Control<StackPanel>(workspace, "PlayerGeneral"), "Editor.PlayerApply");
        Expect(document.PlayerSettingsRows().Single(row => row.ID == 3).Gold == 999999 && warning.Visibility == Visibility.Visible, "Treasury warning wrongly blocked saving player fields."); workspace.Undo(); Exact(bytes);
        gold.Text = "0"; Expect(warning.Visibility == Visibility.Collapsed, "Gold warning retained below capacity.");
        gold.Text = "invalid"; Expect(warning.Visibility == Visibility.Collapsed, "Invalid gold draft created a misleading warning.");
        tabs.SelectedIndex = 8; sections.SelectedIndex = 0; document.Paint([new(21, 18)], new(6, 2, 1));
        Expect(Value(map, "TerrainGold") == "6000", "Statistics snapshot was not invalidated on map edit."); workspace.Undo(); Expect(Value(map, "TerrainGold") == "3000", "Statistics undo refresh stale."); Exact(bytes);
        if (Environment.GetEnvironmentVariable("DK2_STATISTICS_UI_ARTIFACTS") is { Length: > 0 } artifactDirectory)
        {
            Directory.CreateDirectory(artifactDirectory);
            void Render(string page)
            {
                Layout(); var bitmap = new RenderTargetBitmap(1500, 1000, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(artifactDirectory, $"statistics-{page}-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
            }
            foreach (var (index, page) in new[] { (0, "map"), (1, "players"), (2, "bands") }) { sections.SelectedIndex = index; Render(page); }
            tabs.SelectedIndex = 7; gold.Text = "999999"; Render("warning"); Exact(bytes);
        }
        workspace.Document = null; Layout();
        Expect(!sections.IsEnabled && map.Items.Count == 0 && players.Items.Count == 0 && bands.Items.Count == 0 && playerPicker.Items.Count == 0 && bandPicker.Items.Count == 0 && resources.Text.Length == 0 && warning.Visibility == Visibility.Collapsed, "Closed document retained statistics/resources.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} map/player/band statistics, read-only navigation, resources and nonblocking gold warning");
        }
        finally { workspace.Document=null;host.Close(); }
    }
}
