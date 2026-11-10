using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class NewMapDialog : Window
{
    private readonly TextBox fileName = new(), displayName = new(), width = new(), height = new();
    private readonly ComboBox players = new(), version = new(), fill = new(), format = new();
    private readonly CheckBox square = new(), hearts = new();
    private readonly EditorLargeMapPolicy largeMapPolicy;
    private readonly Func<bool> ownerCurrent;
    private LargeMapWarningWindow? largeMapWindow;
    private readonly Button largeReset=new();
    private bool closed,accepting;
    private long inputRevision;
    private string? requestErrorKey;
    private readonly TextBlock error = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button create = new() { IsDefault = true };
    private bool updating;
    public NewMapOptions Options { get; private set; } = new();
    public string MapName => Options.FileName;
    public int MapWidth => Options.Width;
    public int MapHeight => Options.Height;
    public Action<LargeMapWarningWindow>? LargeMapPresenter {get;set;}

    public NewMapDialog(Window? owner, NewMapOptions? initial = null,EditorRequestPreferences? requestPreferences=null,Func<bool>? ownerCurrent=null)
    {
        largeMapPolicy=new(requestPreferences??new());this.ownerCurrent=ownerCurrent??(()=>true);
        var settings = initial ?? new();
        if (owner is not null) Owner = owner;
        NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(TitleProperty, "Menu.New"); Width = 560; SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize; WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new(18) }; var grid = new Grid();
        grid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); grid.ColumnDefinitions.Add(new() { Width = new(270) });
        var controls = new (string Name, string Key, Control Input)[] { ("NewFileName", "Editor.NewFileName", fileName), ("NewDisplayName", "Field.Name", displayName),
            ("NewFormat", "Editor.NewFormat", format), ("NewWidth", "Field.Width", width), ("NewHeight", "Field.Height", height),
            ("NewPlayers", "Editor.NewPlayers", players), ("NewVersion", "Editor.NewVersion", version), ("NewFill", "Editor.NewFill", fill) };
        for (var index = 0; index < controls.Length; index++)
        {
            var (name, key, input) = controls[index]; grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
            var label = new TextBlock { Margin = new(4), VerticalAlignment = VerticalAlignment.Center }; label.SetResourceReference(TextBlock.TextProperty, key);
            Grid.SetRow(label, index); grid.Children.Add(label); input.Name = name; RegisterName(name, input); input.Margin = new(4);
            Grid.SetRow(input, index); Grid.SetColumn(input, 1); grid.Children.Add(input);
        }
        panel.Children.Add(grid);
        void Check(CheckBox box, string name, string key)
        {
            box.Name = name; RegisterName(name, box); box.Margin = new(4);
            var label = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxWidth = 460 };
            label.SetResourceReference(TextBlock.TextProperty, key); box.Content = label; panel.Children.Add(box);
        }
        Check(square, "NewSquare", "Editor.NewSquare"); Check(hearts, "NewHearts", "Editor.NewHearts");
        var hint = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4, 10, 4, 4) };
        hint.SetResourceReference(TextBlock.TextProperty, "Editor.NewHint"); panel.Children.Add(hint);
        error.Name = "NewError"; RegisterName(error.Name, error); error.Margin = new(4); error.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Error"); panel.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        create.Name = "CreateMap"; RegisterName(create.Name, create); create.SetResourceReference(ContentControl.ContentProperty, "Menu.New");
        create.Click += (_, _) => { if (TryAcceptOptions()) DialogResult = true; };
        RegisterName("NewLargeReset",largeReset);largeReset.SetResourceReference(ContentControl.ContentProperty,"LargeMap.Reset");
        largeReset.Click+=(_,_)=>{try{CloseLargeMapWindow();largeMapPolicy.ResetSuppression();UpdateValidity();}catch(InvalidOperationException){requestErrorKey="LargeMap.Stale";UpdateValidity();}};buttons.Children.Add(largeReset);
        var cancel = new Button { Name = "CancelNewMap", IsCancel = true }; RegisterName(cancel.Name, cancel); cancel.SetResourceReference(ContentControl.ContentProperty, "Editor.Cancel");
        buttons.Children.Add(create); buttons.Children.Add(cancel); panel.Children.Add(buttons); Content = panel;
        format.Items.Add(FormatItem("Editor.NewProject", false)); format.Items.Add(FormatItem("Editor.NewNative", true));
        format.SelectedIndex = settings.NativeFormat ? 1 : 0;
        players.ItemsSource = Enumerable.Range(1, 5).ToArray(); players.SelectedItem = settings.Players;
        version.DisplayMemberPath = "Label"; version.SelectedValuePath = "ID";
        version.ItemsSource = new[] { new VersionChoice(13, "1.3"), new VersionChoice(16, "1.6"), new VersionChoice(17, "1.7") }; version.SelectedValue = settings.Version;
        fileName.Text = settings.FileName; displayName.Text = settings.DisplayName;
        width.Text = settings.Width.ToString(CultureInfo.InvariantCulture); height.Text = settings.Height.ToString(CultureInfo.InvariantCulture);
        square.IsChecked = settings.Width == settings.Height; hearts.IsChecked = settings.PlaceHearts;
        RefreshFill(settings.FillID);
        foreach (var input in new[] { fileName, displayName, width, height }) input.TextChanged += (_, _) => InputChanged();
        foreach (var input in new[] { players, version, fill, format }) input.SelectionChanged += (_, _) => InputChanged();
        foreach (var input in new[] { square, hearts }) { input.Checked += (_, _) => InputChanged(); input.Unchecked += (_, _) => InputChanged(); }
        LocalizationService.LanguageChanged += LanguageChanged; Closed += (_, _) => {closed=true;CloseLargeMapWindow();largeMapPolicy.Dispose();LocalizationService.LanguageChanged -= LanguageChanged;};
        UpdateValidity();
    }

    private static ComboBoxItem FormatItem(string key, bool native)
    { var item = new ComboBoxItem { Tag = native }; item.SetResourceReference(ContentControl.ContentProperty, key); return item; }
    private sealed record FillChoice(uint ID, string Label, System.Windows.Media.Imaging.BitmapSource? Icon);
    private sealed record VersionChoice(byte ID, string Label);
    private void RefreshFill(uint selected)
    {
        fill.ItemTemplate = OriginalObjectImages.NamedTemplate(); fill.SelectedValuePath = "ID";
        fill.ItemsSource = LegacyCompilerRules.Elements("NewMap").Select(entry => new FillChoice(entry.ID,
            LocalizationService.Get("Terrain." + (entry.ID >> 24)) + ((byte)(entry.ID >> 24) is 8 or 9 ? " · " + LocalizationService.Get("Owner." + (byte)(entry.ID >> 16)) : ""), OriginalScenarioImages.For(MapRuleImageResources.ForFill(entry.ID)))).ToArray();
        fill.SelectedValue = selected;
    }
    private void LanguageChanged(object? sender, EventArgs e)
    { updating = true; try { RefreshFill(fill.SelectedValue is uint value ? value : 0x02020101); } finally { updating = false; } UpdateValidity(); }

    public bool TryGetOptions(out NewMapOptions options)
    {
        options = new(fileName.Text.Trim(), displayName.Text.Trim(),
            int.TryParse(width.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var w) ? w : 0,
            int.TryParse(height.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) ? h : 0,
            players.SelectedItem is int count ? count : 0, version.SelectedValue is byte patch ? patch : (byte)0,
            fill.SelectedValue is uint preset ? preset : 0, hearts.IsChecked == true, format.SelectedItem is ComboBoxItem { Tag: true });
        return options.Validate() == NewMapProblem.None;
    }
    private void CloseLargeMapWindow(){var window=largeMapWindow;largeMapWindow=null;window?.Close();}
    private void InputChanged(){if(updating)return;inputRevision++;requestErrorKey=null;CloseLargeMapWindow();UpdateValidity();}
    public bool TryAcceptOptions()
    {
        if(accepting||closed||!ownerCurrent()||!TryGetOptions(out var options))return false;
        var revision=inputRevision;accepting=true;
        bool Current()=>!closed&&revision==inputRevision&&ownerCurrent()&&TryGetOptions(out var now)&&now==options;
        try
        {
            using var request=largeMapPolicy.BeginNew(options,Current);
            if(request is not null)
            {
                if(!request.RequiresPrompt)request.Accept();
                else
                {
                    using var window=new LargeMapWarningWindow(request){Owner=this};largeMapWindow=window;
                    try{if(LargeMapPresenter is { } presenter)presenter(window);else window.ShowDialog();}
                    finally{if(ReferenceEquals(largeMapWindow,window))largeMapWindow=null;}
                    if(window.Failure is { } error)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                }
                if(!request.IsAccepted)return false;
            }
            if(!Current())return false;Options=options;return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or FormatException or OverflowException)
        {requestErrorKey="LargeMap.Stale";this.error.ToolTip=error.Message;return false;}
        finally{accepting=false;if(!closed)UpdateValidity();}
    }
    private void UpdateValidity()
    {
        if (updating) return;
        updating = true;
        try
        {
            if (square.IsChecked == true) height.Text = width.Text;
            height.IsEnabled = square.IsChecked != true;
            var valid = TryGetOptions(out var options)&&ownerCurrent()&&!closed; var problem = options.Validate();
            version.IsEnabled = !options.NativeFormat;
            largeReset.IsEnabled=largeMapPolicy.Suppressed&&!accepting;
            error.Visibility = valid&&requestErrorKey is null ? Visibility.Collapsed : Visibility.Visible;
            error.SetResourceReference(TextBlock.TextProperty, problem == NewMapProblem.None ? requestErrorKey??"LargeMap.Stale" : "Editor.NewProblem." + problem);
            create.IsEnabled = valid&&!accepting;
        }
        finally { updating = false; }
    }
}
