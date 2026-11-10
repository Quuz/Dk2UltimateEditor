using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record StatisticView(string Key, string Label, string Value);
    private sealed record StatisticsPlayerView(StatisticsPlayer Player, string Label);
    private sealed record StatisticsBandView(StatisticsBand Band, string Label);
    private MapStatistics? statisticsSnapshot;
    private bool updatingStatistics;
    private MapStatistics? CurrentStatistics() => document is null ? null : statisticsSnapshot ??= document.CalculateStatistics();
    private static string StatText(string key) => LocalizationService.Get("Editor.Statistics." + key);
    private static string StatNumber(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? StatText("Unknown");
    private static StatisticView Stat(string key, string value) => new(key, StatText(key), value);
    private string StatName(StatisticsThingKind kind, uint? id)
    {
        if (id is null) return "—";
        var template = kind == StatisticsThingKind.SpecialItem ? "MagicalObject" : kind.ToString();
        return "#" + id + " · " + (ValueCatalogues.ForTemplate(template, catalogueVersion).FirstOrDefault(choice => choice.Value == id.Value.ToString(CultureInfo.InvariantCulture))?.Name(LocalizationService.CurrentLanguage) ?? StatText("Unknown"));
    }
    private IEnumerable<StatisticView> ThingStats(StatisticsThings things)
    {
        foreach (var (key, number) in new[] { ("Creatures", things.Creatures), ("Imps", things.Imps), ("Traps", things.Traps), ("Doors", things.Doors), ("Objects", things.Objects), ("SpecialItems", things.SpecialItems), ("AverageLevel", things.AverageCreatureLevel) })
            yield return Stat(key, StatNumber(number));
        foreach (var (kind, preferred) in new[] { (StatisticsThingKind.Creature, things.PreferredCreature), (StatisticsThingKind.Trap, things.PreferredTrap), (StatisticsThingKind.Door, things.PreferredDoor), (StatisticsThingKind.Object, things.PreferredObject), (StatisticsThingKind.SpecialItem, things.PreferredSpecialItem) })
            yield return new("Preferred." + kind, StatText("Preferred") + " · " + StatText(kind.ToString()), StatName(kind, preferred));
        foreach (var type in things.Types)
            yield return new("Type." + type.Kind + "." + type.ID, StatText(type.Kind.ToString()) + " · " + StatName(type.Kind, type.ID), StatNumber(type.Count));
    }
    private static void BindStatistics(DataGrid grid, IEnumerable<StatisticView> rows)
    {
        var textStyle = new Style(typeof(TextBlock));
        textStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
        grid.Columns.Clear();
        grid.Columns.Add(new DataGridTextColumn { Header = StatText("Metric"), Binding = new Binding(nameof(StatisticView.Label)), ElementStyle = textStyle, MinWidth = 120, Width = new(2, DataGridLengthUnitType.Star) });
        grid.Columns.Add(new DataGridTextColumn { Header = StatText("Value"), Binding = new Binding(nameof(StatisticView.Value)), ElementStyle = textStyle, MinWidth = 120, Width = new(1, DataGridLengthUnitType.Star) });
        grid.ItemsSource = rows.ToArray();
    }
    private void RefreshStatistics()
    {
        if (StatisticsMapTable is null || document is not null && WorkspaceTabs.SelectedIndex != 8) return;
        updatingStatistics = true;
        try
        {
            var selectedPlayer = (StatisticsPlayerPicker.SelectedItem as StatisticsPlayerView)?.Player.ID;
            var selectedBand = (StatisticsBandPicker.SelectedItem as StatisticsBandView)?.Band.Record;
            var stats = CurrentStatistics(); StatisticsTabs.IsEnabled = stats is not null;
            StatisticsIssues.ToolTip = null;
            if (stats is null)
            {
                StatisticsPlayerPicker.ItemsSource = StatisticsBandPicker.ItemsSource = null;
                StatisticsShowHeart.IsEnabled = StatisticsBandProperties.IsEnabled = false;
                BindStatistics(StatisticsMapTable, []); BindStatistics(StatisticsPlayerTable, []); BindStatistics(StatisticsBandTable, []); StatisticsIssues.Text = ""; return;
            }
            var rows = new List<StatisticView> { Stat("Dimensions", $"{stats.Width} × {stats.Height}"), Stat("Tiles", StatNumber(stats.TotalTiles)),
                Stat("TerrainGold", StatNumber(stats.TerrainGold)), Stat("ObjectGold", StatNumber(stats.ObjectGold)), Stat("SpecialGold", StatNumber(stats.SpecialGold)),
                Stat("UnlimitedGold", StatText(stats.UnlimitedGold ? "Yes" : "No")) };
            foreach (var terrain in stats.Terrains)
                rows.Add(new("Terrain." + terrain.Terrain, LocalizationService.Get("Terrain." + terrain.Terrain), $"{terrain.Count} · {terrain.Percentage.ToString("0.00", CultureInfo.InvariantCulture)}%"));
            rows.AddRange(ThingStats(stats.Things));
            rows.AddRange(ActivityStatisticsRows()); BindStatistics(StatisticsMapTable, rows);
            var players = stats.Players.Select(player => new StatisticsPlayerView(player, ValueCatalogues.Owners[(int)player.ID - 1].Name(LocalizationService.CurrentLanguage))).ToArray();
            StatisticsPlayerPicker.ItemsSource = players; StatisticsPlayerPicker.SelectedItem = players.FirstOrDefault(player => player.Player.ID == selectedPlayer) ?? players[0];
            var bands = stats.Bands.Select(band => new StatisticsBandView(band, $"#{band.ID}" + (band.Name.Length == 0 ? "" : " · " + band.Name) + $" · 0x{band.Record.Offset:X}")).ToArray();
            StatisticsBandPicker.ItemsSource = bands;
            StatisticsBandPicker.SelectedItem = bands.FirstOrDefault(band => band.Band.Record.File == selectedBand?.File && band.Band.Record.Offset == selectedBand.Offset) ?? bands.FirstOrDefault();
            StatisticsIssues.Text = string.Join("\n", stats.Issues.Select(issue => StatText("Issue." + issue)));
            RefreshStatisticsSelection();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException or FormatException)
        {
            StatisticsPlayerPicker.ItemsSource = StatisticsBandPicker.ItemsSource = null; StatisticsTabs.IsEnabled = false;
            BindStatistics(StatisticsMapTable, []); BindStatistics(StatisticsPlayerTable, []); BindStatistics(StatisticsBandTable, []);
            StatisticsShowHeart.IsEnabled = StatisticsBandProperties.IsEnabled = false;
            StatisticsIssues.Text = StatText("Failed"); StatisticsIssues.ToolTip = exception.Message;
        }
        finally { updatingStatistics = false; }
    }
    private void RefreshStatisticsSelection()
    {
        var player = (StatisticsPlayerPicker.SelectedItem as StatisticsPlayerView)?.Player;
        var rows = new List<StatisticView>();
        if (player is not null)
        {
            foreach (var (key, value) in new[] { ("OwnedTiles", player.OwnedTiles), ("Walls", player.Walls), ("Claimed", player.Claimed), ("LairCapacity", player.LairCapacity) }) rows.Add(Stat(key, StatNumber(value)));
            rows.Add(Stat("Heart", player.Heart is { } heart ? $"({heart.X}, {heart.Y})" : "—"));
            rows.Add(Stat("TreasuryCapacity", StatNumber(player.TreasuryCapacity))); rows.Add(Stat("ManaIncrease", StatNumber(player.ManaIncrease)));
            rows.AddRange(ThingStats(player.Things));
        }
        BindStatistics(StatisticsPlayerTable, rows); StatisticsShowHeart.IsEnabled = player?.Heart is not null;
        rows.Clear(); var stats = CurrentStatistics(); var band = (StatisticsBandPicker.SelectedItem as StatisticsBandView)?.Band;
        if (stats is not null)
        {
            var totals = stats.BandTotals;
            foreach (var (key, value) in new[] { ("BandCount", (long)stats.Bands.Count), ("BandImps", totals.Imps), ("BandDwarfs", totals.Dwarfs), ("BandCreatures", totals.Creatures), ("BandAverage", totals.AverageLevel), ("HeroesCount", totals.HeroesDisplayedCount), ("HeroesAverage", totals.HeroesAverageLevel) }) rows.Add(Stat(key, StatNumber(value)));
            rows.Add(Stat("BandPreferred", StatName(StatisticsThingKind.Creature, totals.PreferredCreature)));
        }
        if (band is not null)
        {
            rows.Add(Stat("SelectedBand", $"#{band.ID}" + (band.Name.Length == 0 ? "" : " · " + band.Name)));
            foreach (var (key, value) in new[] { ("Imps", band.Imps), ("Dwarfs", band.Dwarfs), ("OtherBandCreatures", band.Creatures), ("AverageLevel", band.AverageLevel), ("ReleaseActions", band.ReleaseActions) }) rows.Add(Stat(key, StatNumber(value)));
            rows.Add(new("Preferred.Creature", StatText("Preferred") + " · " + StatText("Creature"), StatName(StatisticsThingKind.Creature, band.PreferredCreature)));
        }
        BindStatistics(StatisticsBandTable, rows); StatisticsBandProperties.IsEnabled = band is not null;
    }
    private void StatisticsSelection_Changed(object sender, SelectionChangedEventArgs e)
    { if (!updatingStatistics && StatisticsPlayerTable is not null) RefreshStatisticsSelection(); }
    private void StatisticsShowHeart_Click(object sender, RoutedEventArgs e)
    {
        if ((StatisticsPlayerPicker.SelectedItem as StatisticsPlayerView)?.Player.Heart is not { } point) return;
        WorkspaceTabs.SelectedIndex = 0; Canvas.SetSelection(new(point.X, point.Y, 1, 1));
        Canvas.BringIntoView(Canvas.CellBounds(new(point.X, point.Y, 1, 1)));
    }
    private void StatisticsBandProperties_Click(object sender, RoutedEventArgs e)
    {
        if ((StatisticsBandPicker.SelectedItem as StatisticsBandView)?.Band is not { } band) return;
        RecordSearch.Text = ""; RecordOperation(() => band.Record); WorkspaceTabs.SelectedIndex = 1;
    }
    private void RefreshPlayerResources()
    {
        if (PlayerResources is null || document is not null && WorkspaceTabs.SelectedIndex != 7) return;
        PlayerResources.Text = ""; PlayerResources.ToolTip = null; PlayerGoldWarning.Visibility = Visibility.Collapsed;
        if ((PlayerPicker.SelectedItem as PlayerView)?.Player is not { } selected) return;
        try
        {
            var stats = CurrentStatistics(); var player = stats?.Players.FirstOrDefault(player => player.ID == selected.ID);
            if (stats is null || player is null) return;
            PlayerResources.Text = $"{StatText("TerrainGold")}: {StatNumber(stats.TerrainGold)}{(stats.UnlimitedGold ? " + ∞" : "")} · {StatText("TreasuryCapacity")}: {StatNumber(player.TreasuryCapacity)} · {StatText("ManaIncrease")}: {StatNumber(player.ManaIncrease)}";
            if (selected.ID >= 3 && player.TreasuryCapacity is { } capacity && long.TryParse(PlayerGold.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var gold) && gold > capacity)
                PlayerGoldWarning.Visibility = Visibility.Visible;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException or FormatException)
        { PlayerResources.Text = StatText("Failed"); PlayerResources.ToolTip = exception.Message; }
    }
}
