using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>The original Select_Change workflow: zero-based origin plus width/height.</summary>
public sealed class SelectionBoundsDialog : Window
{
    private readonly int mapWidth;
    private readonly int mapHeight;
    private readonly TextBox[] inputs = new TextBox[4];
    private readonly TextBlock error = new() { TextWrapping = TextWrapping.Wrap, Margin = new(0, 8, 0, 8) };
    private readonly Button apply = new() { IsDefault = true };
    public TileRect Rectangle { get; private set; }

    public SelectionBoundsDialog(Window? owner, int mapWidth, int mapHeight, TileRect? selection)
    {
        if (mapWidth < 1 || mapHeight < 1) throw new ArgumentOutOfRangeException(nameof(mapWidth));
        this.mapWidth = mapWidth; this.mapHeight = mapHeight;
        NameScope.SetNameScope(this, new NameScope());
        if (owner is not null) Owner = owner;
        SetResourceReference(TitleProperty, "Menu.ChangeSelection");
        Width = 440; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        var initial = selection ?? new TileRect(Math.Min(1, mapWidth - 1), Math.Min(1, mapHeight - 1), 1, 1);
        var values = new[] { initial.X, initial.Y, initial.Width, initial.Height };
        var panel = new StackPanel { Margin = new(18) };
        var grid = new Grid(); grid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); grid.ColumnDefinitions.Add(new() { Width = new(150) });
        var keys = new[] { "Editor.SelectionX", "Editor.SelectionY", "Field.Width", "Field.Height" };
        var names = new[] { "SelectionX", "SelectionY", "SelectionWidth", "SelectionHeight" };
        for (var index = 0; index < inputs.Length; index++)
        {
            grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new(4) };
            label.SetResourceReference(TextBlock.TextProperty, keys[index]); Grid.SetRow(label, index); grid.Children.Add(label);
            var input = new TextBox { Name = names[index], Text = values[index].ToString(CultureInfo.InvariantCulture), Margin = new(4), MaxLength = 10 };
            RegisterName(names[index], input); inputs[index] = input;
            Grid.SetRow(input, index); Grid.SetColumn(input, 1); grid.Children.Add(input);
        }
        panel.Children.Add(grid);
        var hint = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4, 8, 4, 4) };
        hint.SetResourceReference(TextBlock.TextProperty, "Editor.SelectionBoundsHint"); panel.Children.Add(hint);
        error.Name = "SelectionError"; RegisterName(error.Name, error);
        error.SetResourceReference(TextBlock.TextProperty, "Editor.SelectionBoundsError");
        error.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Error"); panel.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        apply.Name = "ApplySelection"; RegisterName(apply.Name, apply); apply.SetResourceReference(Button.ContentProperty, "Editor.SelectRectangle");
        apply.Click += (_, _) => { if (TryGetRectangle(out var rectangle)) { Rectangle = rectangle; DialogResult = true; } };
        var cancel = new Button { IsCancel = true, Name = "CancelSelection" }; RegisterName(cancel.Name, cancel);
        cancel.SetResourceReference(Button.ContentProperty, "Editor.Cancel");
        buttons.Children.Add(apply); buttons.Children.Add(cancel); panel.Children.Add(buttons); Content = panel;
        foreach (var input in inputs) input.TextChanged += (_, _) => UpdateValidity();
        UpdateValidity();
    }

    public bool TryGetRectangle(out TileRect rectangle)
    {
        rectangle = default;
        var values = new int[4];
        for (var index = 0; index < inputs.Length; index++)
            if (!int.TryParse(inputs[index].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out values[index])) return false;
        var (x, y, width, height) = (values[0], values[1], values[2], values[3]);
        // Use subtraction: malicious numeric inputs must not wrap an int sum.
        if (x < 0 || y < 0 || x >= mapWidth || y >= mapHeight || width < 1 || height < 1
            || width > mapWidth - x || height > mapHeight - y) return false;
        rectangle = new(x, y, width, height); return true;
    }

    private void UpdateValidity()
    {
        var valid = TryGetRectangle(out _); apply.IsEnabled = valid;
        error.Visibility = valid ? Visibility.Collapsed : Visibility.Visible;
    }
}
