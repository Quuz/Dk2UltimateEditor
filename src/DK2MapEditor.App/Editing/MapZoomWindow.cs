using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace DK2MapEditor.App.Editing;

public sealed class MapZoomWindow : Window
{
    private readonly Func<int, int, bool> apply;
    private readonly TextBox width = new() { Margin = new(4) }, height = new() { Margin = new(4) };
    private readonly TextBlock error = new() { TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed, Margin = new(4) };
    private bool closed;
    public MapZoomWindow(int userWidth, int userHeight, Func<int, int, bool> apply)
    {
        this.apply = apply; NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(TitleProperty, "MapZoom.Custom"); Width = 420; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new(16) }; Content = panel;
        foreach (var (key, input, name, value) in new[] { ("MapZoom.Width", width, "ZoomUserWidth", userWidth), ("MapZoom.Height", height, "ZoomUserHeight", userHeight) })
        {
            var label = new TextBlock(); label.SetResourceReference(TextBlock.TextProperty, key); panel.Children.Add(label);
            input.Text = value.ToString(CultureInfo.InvariantCulture); RegisterName(name, input); panel.Children.Add(input);
        }
        var hint = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4) }; hint.SetResourceReference(TextBlock.TextProperty, "MapZoom.Bounds"); panel.Children.Add(hint);
        error.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Warning"); error.SetResourceReference(TextBlock.TextProperty, "MapZoom.Invalid"); panel.Children.Add(error);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        var ok = new Button { IsDefault = true }; ok.SetResourceReference(ContentControl.ContentProperty, "Editor.ApplyNamedValue"); ok.Click += (_, _) => ApplyDimensions(); RegisterName("ZoomApply", ok);
        var cancel = new Button { IsCancel = true }; cancel.SetResourceReference(ContentControl.ContentProperty, "Editor.Cancel"); cancel.Click += (_, _) => Close(); buttons.Children.Add(ok); buttons.Children.Add(cancel); panel.Children.Add(buttons);
        Closed += (_, _) => closed = true;
    }
    public bool ApplyDimensions()
    {
        if (closed) return false;
        if (!int.TryParse(width.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var w) || !int.TryParse(height.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h)
            || !Core.Editing.MapZoom.Valid(w) || !Core.Editing.MapZoom.Valid(h) || !apply(w, h)) { error.Visibility = Visibility.Visible; return false; }
        Close(); return true;
    }
}
