using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    public MapOverlayOptions OverlayOptions => Canvas.OverlayOptions;
    public void ConfigureOverlays(MapOverlayOptions? options)
    {
        Canvas.OverlayOptions = options ?? new();
        if (OverlayViewMenu.Items.Count == 0) foreach (var flag in Enum.GetValues<MapOverlayFlags>().Where(flag => flag != 0))
        {
            var item = new MenuItem { Tag = flag, IsCheckable = true };
            item.SetResourceReference(HeaderedItemsControl.HeaderProperty, "MapOverlays." + flag);
            item.Click += OverlayView_Click; OverlayViewMenu.Items.Add(item);
        }
        foreach (MenuItem item in OverlayViewMenu.Items) item.IsChecked = (Canvas.OverlayOptions.Flags & (MapOverlayFlags)item.Tag) != 0;
        PatrolToggle.IsChecked = Canvas.ShowPatrolPaths;
        var colour = Canvas.OverlayOptions.PathColour;
        PathColourInput.Text = ((colour & 255) << 16 | colour & 0xFF00 | colour >> 16).ToString("X6", CultureInfo.InvariantCulture);
    }
    private void OverlayView_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender; var flag = (MapOverlayFlags)item.Tag;
        ConfigureOverlays(OverlayOptions with { Flags = item.IsChecked ? OverlayOptions.Flags | flag : OverlayOptions.Flags & ~flag });
    }
    public bool ApplyPathColour()
    {
        var text = PathColourInput.Text.Trim().TrimStart('#');
        if (text.Length is < 1 or > 6 || !uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb)) return false;
        // User-facing RRGGBB converts to the source Windows COLORREF word.
        var colour = (rgb & 255) << 16 | rgb & 0xFF00 | rgb >> 16;
        ConfigureOverlays(OverlayOptions with { PathColour = colour }); return true;
    }
    private void PathColour_Click(object sender, RoutedEventArgs e)
    {
        PathColourInput.ToolTip = ApplyPathColour() ? null : Localization.LocalizationService.Get("MapOverlays.InvalidColour");
    }
}
