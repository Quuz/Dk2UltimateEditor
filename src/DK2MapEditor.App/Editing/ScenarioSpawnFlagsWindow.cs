using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class ScenarioSpawnFlagsWindow : Window
{
    private readonly Dictionary<uint, CheckBox> flags = [];
    private Action<IEnumerable<KeyValuePair<uint, bool>>>? accept;
    private readonly TextBlock status = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    public ScenarioSpawnFlagsWindow(uint value, Action<IEnumerable<KeyValuePair<uint, bool>>> accept)
    {
        this.accept = accept; Width = 580; Height = 440; MinWidth = 480; MinHeight = 390;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(TitleProperty, "ScenarioSpawn.Flags"); SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        NameScope.SetNameScope(this, new NameScope()); var panel = new DockPanel { Margin = new(12) }; Content = panel;
        var hint = new TextBlock { Margin = new(4), TextWrapping = TextWrapping.Wrap }; hint.SetResourceReference(TextBlock.TextProperty, "ScenarioSpawn.FlagsHint"); DockPanel.SetDock(hint, Dock.Top); panel.Children.Add(hint);
        DockPanel.SetDock(status, Dock.Top); panel.Children.Add(status); RegisterName("SpawnFlagsStatus", status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right }; DockPanel.SetDock(buttons, Dock.Bottom); panel.Children.Add(buttons);
        var apply = new Button(); apply.SetResourceReference(ContentControl.ContentProperty, "ScenarioSpawn.AcceptFlags"); apply.Click += (_, _) => { if (ApplyDraft()) Close(); }; buttons.Children.Add(apply); RegisterName("SpawnFlagsApply", apply);
        var cancel = new Button { IsCancel = true }; cancel.SetResourceReference(ContentControl.ContentProperty, "CameraPath.Cancel"); cancel.Click += (_, _) => Close(); buttons.Children.Add(cancel);
        var body = new StackPanel(); panel.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        foreach (var flag in ScenarioSpawnOptions.Flags)
        {
            var box = new CheckBox { Margin = new(4, 7, 4, 7), IsEnabled = flag.Editable, IsChecked = (value & flag.Bit) != 0 };
            var caption = new TextBlock { TextWrapping = TextWrapping.Wrap }; caption.SetResourceReference(TextBlock.TextProperty, "ScenarioSpawn.Flag" + flag.Key); box.Content = caption;
            body.Children.Add(box); flags.Add(flag.Bit, box); RegisterName("SpawnFlag" + flag.Bit, box);
        }
        Closed += (_, _) => this.accept = null;
    }
    public bool ApplyDraft()
    {
        if (accept is null) return false;
        try { accept(ScenarioSpawnOptions.Flags.Where(flag => flag.Editable).Select(flag => new KeyValuePair<uint, bool>(flag.Bit, flags[flag.Bit].IsChecked == true)).ToArray()); return true; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { status.SetResourceReference(TextBlock.TextProperty, "AliasEvent.Failed"); status.ToolTip = error.Message; return false; }
    }
}
