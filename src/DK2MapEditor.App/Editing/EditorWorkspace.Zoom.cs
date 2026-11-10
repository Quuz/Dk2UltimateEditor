using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private int userZoomWidth = 64, userZoomHeight = 64;
    private bool refreshingZoom;
    private MapZoomWindow? zoomWindow;
    public MapZoomOptions ZoomOptions => new(Canvas.CellWidth, Canvas.CellHeight, userZoomWidth, userZoomHeight);
    public bool ShowTerrain => Canvas.ShowTerrain;
    public void ConfigureZoom(MapZoomOptions? options)
    {
        var safe = MapZoom.Safe(options); userZoomWidth = safe.UserWidth; userZoomHeight = safe.UserHeight;
        Canvas.SetCellDimensions(safe.Width, safe.Height); RefreshZoomControls();
    }
    public void ConfigureTerrainVisibility(bool visible) { TerrainViewToggle.IsChecked = visible; Canvas.ShowTerrain = visible; }
    private void RefreshZoomControls()
    {
        if (ZoomPicker is null) return;
        refreshingZoom = true;
        try
        {
            var choices = MapZoom.Presets.Select((value, index) => new ZoomChoice(index, $"{value} × {value}")).ToList();
            choices.Add(new(11, $"{LocalizationService.Get("MapZoom.User")} · {userZoomWidth} × {userZoomHeight}"));
            var selected = Canvas.CellWidth == Canvas.CellHeight ? MapZoom.Presets.ToList().IndexOf((int)Canvas.CellWidth) : -1;
            if (selected >= 0 && Canvas.CellWidth != MapZoom.Presets[selected]) selected = -1;
            if (selected < 0) selected = Canvas.CellWidth == userZoomWidth && Canvas.CellHeight == userZoomHeight ? 11 : 12;
            if (selected == 12) choices.Add(new(12, $"{Canvas.CellWidth.ToString("G", CultureInfo.InvariantCulture)} × {Canvas.CellHeight.ToString("G", CultureInfo.InvariantCulture)}"));
            ZoomPicker.ItemsSource = choices; ZoomPicker.SelectedValue = selected;
        }
        finally { refreshingZoom = false; }
    }
    public void ApplyZoomPreset(int index)
    {
        if (index is < 0 or > 11) throw new ArgumentOutOfRangeException(nameof(index));
        ApplyZoom(index == 11 ? userZoomWidth : MapZoom.Presets[index], index == 11 ? userZoomHeight : MapZoom.Presets[index]);
    }
    public void ApplyUserZoom(int width, int height)
    {
        if (!MapZoom.Valid(width) || !MapZoom.Valid(height)) throw new ArgumentOutOfRangeException(nameof(width));
        userZoomWidth = width; userZoomHeight = height; ApplyZoom(width, height); RefreshZoomControls();
    }
    private void ApplyZoom(double width, double height)
    {
        var x = MapScroll.HorizontalOffset / Canvas.CellWidth; var y = MapScroll.VerticalOffset / Canvas.CellHeight;
        Canvas.SetCellDimensions(width, height); Canvas.UpdateLayout();
        MapScroll.ScrollToHorizontalOffset(x * width); MapScroll.ScrollToVerticalOffset(y * height); RefreshOverviewViewport();
    }
    private void ScaleZoom(double factor) => ApplyZoom(Math.Clamp(Canvas.CellWidth * factor, MapZoom.Minimum, MapZoom.Maximum), Math.Clamp(Canvas.CellHeight * factor, MapZoom.Minimum, MapZoom.Maximum));
    private void ZoomPreset_Changed(object sender, SelectionChangedEventArgs e) { if (!refreshingZoom && ZoomPicker.SelectedValue is int index && index <= 11) ApplyZoomPreset(index); }
    private void TerrainView_Click(object sender, RoutedEventArgs e) => Canvas.ShowTerrain = TerrainViewToggle.IsChecked == true;
    public MapZoomWindow CreateZoomWindow()
    {
        var source = document;
        return new(userZoomWidth, userZoomHeight, (width, height) =>
        { if (!ReferenceEquals(source, document)) return false; ApplyUserZoom(width, height); return true; });
    }
    private void CustomZoom_Click(object sender, RoutedEventArgs e)
    {
        if (zoomWindow is not null) { zoomWindow.Activate(); return; }
        var window = CreateZoomWindow(); zoomWindow = window; window.Owner = Window.GetWindow(this);
        window.Closed += (_, _) => { if (ReferenceEquals(zoomWindow, window)) zoomWindow = null; }; window.ShowDialog();
    }
    private void CloseZoomWindow() { zoomWindow?.Close(); zoomWindow = null; }
    private sealed record ZoomChoice(int ID, string Label);
}
