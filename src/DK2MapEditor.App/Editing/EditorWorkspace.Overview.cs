using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private FullMapWindow? fullMapWindow;
    private ThumbnailWindow? thumbnailWindow;
    private readonly ThumbnailWindowPlacement thumbnailPlacement=new();
    private Core.Editing.MapThumbnailOptions thumbnailOptions = new();
    public Core.Editing.MapThumbnailOptions ThumbnailOptions => thumbnailOptions;
    public void ConfigureThumbnailOptions(Core.Editing.MapThumbnailOptions? options)
    {
        var requested = options ?? new();
        // Old/malformed settings must not prevent startup. Retain valid flag
        // words and booleans while bounding display-only colour/size values.
        thumbnailOptions = requested with { HiddenColour = requested.HiddenColour & 0xFFFFFF,PreviewSize = requested.PreviewSize is 96 or 144 ? requested.PreviewSize : 144 };
        Overview.KeepProportions = thumbnailOptions.KeepProportions; OverviewProportions.IsChecked = thumbnailOptions.KeepProportions;
    }
    private void RefreshOverviewViewport()
    {
        if (Overview is null || MapScroll is null || Canvas is null) return;
        RefreshSourceScrollPadding();
        Overview.IsEnabled = document is not null;
        if (FullscreenOverviewButton is not null) FullscreenOverviewButton.IsEnabled = document is not null;
        if (ThumbnailButton is not null) ThumbnailButton.IsEnabled = document is not null;
        Overview.Viewport = document is null ? Rect.Empty : new Rect(MapScroll.HorizontalOffset / Canvas.CellWidth,MapScroll.VerticalOffset / Canvas.CellHeight,
            Math.Min(document.Width,MapScroll.ViewportWidth / Canvas.CellWidth),Math.Min(document.Height,MapScroll.ViewportHeight / Canvas.CellHeight));
    }
    private void Overview_NavigationRequested(object? sender,OverviewNavigationEventArgs e)
    {
        if (document is null) return;
        MapScroll.ScrollToHorizontalOffset(e.X * Canvas.CellWidth);
        MapScroll.ScrollToVerticalOffset(e.Y * Canvas.CellHeight);
    }
    private void MapScroll_Changed(object sender,ScrollChangedEventArgs e) => RefreshOverviewViewport();
    private void MapScroll_SizeChanged(object sender,SizeChangedEventArgs e) => RefreshOverviewViewport();
    private void FullscreenOverview_Click(object sender,RoutedEventArgs e) => ShowFullscreenOverview();
    private void ShowFullscreenOverview()
    {
        if (document is null) return;
        if (fullMapWindow is not null) { fullMapWindow.Activate(); return; }
        var window = CreateFullscreenOverview(); fullMapWindow = window;
        window.Owner = Window.GetWindow(this);
        window.Closed += (_,_) => { if (ReferenceEquals(fullMapWindow,window)) fullMapWindow = null; };
        window.Show();
    }
    /// <summary>Same window and navigation callback used by the real command;
    /// constructing it does not show a window or start its animation timer.</summary>
    public FullMapWindow CreateFullscreenOverview()
    {
        if (document is null) throw new InvalidOperationException("Open a map before viewing it.");
        var source = document;
        return new(source,(x,y) =>
        {
            if (!ReferenceEquals(source,document)) return;
            MapScroll.ScrollToHorizontalOffset(Math.Max(0,(x + 0.5) * Canvas.CellWidth - MapScroll.ViewportWidth / 2));
            MapScroll.ScrollToVerticalOffset(Math.Max(0,(y + 0.5) * Canvas.CellHeight - MapScroll.ViewportHeight / 2));
        });
    }
    private void CloseFullscreenOverview() { fullMapWindow?.Close(); fullMapWindow = null; }
    public bool AreToolbarsVisible => MapToolbar.Visibility == Visibility.Visible;
    public bool IsOverviewVisible => OverviewToggle.IsChecked == true;
    public void SetToolbarsVisibility(bool visible)
        => MapToolbar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    public void SetOverviewVisibility(bool visible)
    {
        OverviewToggle.IsChecked = visible;
        OverviewPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        RefreshOverviewViewport();
    }
    private void OverviewToggle_Click(object sender,RoutedEventArgs e)
        => SetOverviewVisibility(OverviewToggle.IsChecked == true);
    private void OverviewProportions_Click(object sender,RoutedEventArgs e)
    {
        Overview.KeepProportions = OverviewProportions.IsChecked == true;
        thumbnailOptions = thumbnailOptions with { KeepProportions = Overview.KeepProportions };
        thumbnailWindow?.SetKeepProportions(Overview.KeepProportions);
    }
    private void Thumbnail_Click(object sender,RoutedEventArgs e)
    {
        if (document is null) return;
        if (thumbnailWindow is not null) { thumbnailWindow.Activate(); return; }
        var window = CreateThumbnailWindow(); thumbnailWindow = window; window.Owner = Window.GetWindow(this);
        window.Closed += (_,_) => { if (ReferenceEquals(thumbnailWindow,window)) thumbnailWindow = null; }; window.Show();
    }
    public ThumbnailWindow CreateThumbnailWindow()
    {
        if (document is null) throw new InvalidOperationException("Open a map before creating a thumbnail.");
        var source = document;
        ThumbnailWindow? window=null;
        window=new(source,thumbnailOptions with { KeepProportions = Overview.KeepProportions },options =>
        {
            if (!ReferenceEquals(window?.Document,document)||document is null) return;
            thumbnailOptions = options; Overview.KeepProportions = options.KeepProportions; OverviewProportions.IsChecked = options.KeepProportions;
        },FileDialogs);
        thumbnailPlacement.Attach(window);return window;
    }
    private void CloseThumbnailWindow() { thumbnailWindow?.Close(); thumbnailWindow = null; }
    private void OverviewSize_Changed(object sender,SelectionChangedEventArgs e)
    {
        if (Overview is not null && OverviewSize.SelectedItem is ComboBoxItem { Tag: string value })
            Overview.Width = Overview.Height = int.Parse(value,CultureInfo.InvariantCulture);
    }
}
