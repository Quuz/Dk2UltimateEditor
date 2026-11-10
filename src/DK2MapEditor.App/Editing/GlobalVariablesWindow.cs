using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>The original five pages share one private context, one Apply and Cancel.</summary>
public sealed class GlobalVariablesWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly Func<bool> ownerCurrent, commitFields;
    private GlobalVariablesSession session;
    private readonly TabControl pages = new();
    private readonly ComboBox rows = new() { ItemTemplate=OriginalObjectImages.NamedTemplate(), Margin = new(4, 8, 4, 8) };
    private readonly ContentControl editorHost = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(4) };
    private readonly Button apply = new(), resetCurrent = new(), resetAll = new();
    private readonly Dictionary<string, int> selections = GlobalVariablesSession.Families.ToDictionary(family => family, _ => 0);
    private Window? rowEditor, child;
    private bool closed, stale, updating, transitioning, applying, colourOpen;
    private string family = "Terrains", statusKey = "Ready";
    private sealed record RowChoice(int Index,string Family,string Label)
    { public System.Windows.Media.Imaging.BitmapSource? Icon=>OriginalDefinitionImages.For(Family,Index); }
    public Window? ActiveRowEditor => rowEditor;
    public string SelectedFamily => family;
    public int SelectedIndex => selections[family];
    public Action<Window>? OptionsPresenter { get; set; }
    public Func<uint, uint?>? ColourSelector { get; set; }

    public GlobalVariablesWindow(NativeMapDocument document, Func<bool> ownerCurrent, Func<bool> commitFields)
    {
        this.document = document; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields;
        session = document.CreateGlobalVariablesSession(); NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(TitleProperty, "GlobalVariables.Title"); SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 780; Height = 760; MinWidth = 560; MinHeight = 480; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel;
        var hint = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4, 0, 4, 8) };
        hint.SetResourceReference(TextBlock.TextProperty, "GlobalVariables.Hint"); DockPanel.SetDock(hint, Dock.Top); panel.Children.Add(hint);
        var navigation = new StackPanel(); DockPanel.SetDock(navigation, Dock.Top); panel.Children.Add(navigation);
        foreach (var name in GlobalVariablesSession.Families)
        {
            var tab = new TabItem { Tag = name }; tab.SetResourceReference(HeaderedContentControl.HeaderProperty, "Editor.Definitions." + name); pages.Items.Add(tab);
        }
        RegisterName("GlobalVariablesPages", pages); RegisterName("GlobalVariablesRows", rows); navigation.Children.Add(pages); navigation.Children.Add(rows);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status); RegisterName("GlobalVariablesStatus", status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        void Button(Button button, string key, string name, RoutedEventHandler click)
        { button.SetResourceReference(ContentControl.ContentProperty, "GlobalVariables." + key); button.Click += click; RegisterName(name, button); buttons.Children.Add(button); }
        Button(resetCurrent, "ResetCurrent", "GlobalVariablesResetCurrent", (_, _) => Reset(false));
        Button(resetAll, "ResetAll", "GlobalVariablesResetAll", (_, _) => Reset(true));
        Button(apply, "Apply", "GlobalVariablesApply", (_, _) => ApplyVariables());
        var cancel = new Button { IsCancel = true }; Button(cancel, "Cancel", "GlobalVariablesCancel", (_, _) => Close()); footer.Children.Add(buttons);
        panel.Children.Add(editorHost); RegisterName("GlobalVariablesEditor", editorHost);
        pages.SelectionChanged += (_, _) =>
        {
            if (updating || pages.SelectedItem is not TabItem tab) return;
            if (!Select((string)tab.Tag, selections[(string)tab.Tag])) RefreshSelectors();
        };
        rows.SelectionChanged += (_, _) =>
        { if (!updating && rows.SelectedItem is RowChoice choice && !Select(family, choice.Index)) RefreshSelectors(); };
        document.Changed += DocumentChanged; LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) =>
        {
            closed = true; child?.Close(); child = null; CloseRow(); session.Dispose();
            document.Changed -= DocumentChanged; LocalizationService.LanguageChanged -= LanguageChanged;
        };
        RefreshSelectors(); OpenRow(); UpdateStatus();
    }

    private bool Current => !closed && !stale && ownerCurrent() && document.IsGlobalVariablesSessionCurrent(session);
    private bool Busy => transitioning || applying || child is not null || colourOpen;
    private void RefreshSelectors()
    {
        updating = true;
        try
        {
            pages.SelectedItem = pages.Items.Cast<TabItem>().Single(tab => (string)tab.Tag == family);
            rows.ItemsSource = session.WorkingDocument.GlobalDefinitionRows().Where(row => row.Family == family)
                .Select(row => new RowChoice(row.Index,family, family is "Terrains" or "Rooms"
                    ? LocalizationService.Get("Terrain." + row.ID)
                    : ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue(family)[row.Index], row.ID.ToString(CultureInfo.InvariantCulture)).Name(LocalizationService.CurrentLanguage))).ToArray();
            rows.SelectedItem = rows.Items.Cast<RowChoice>().Single(row => row.Index == SelectedIndex);
        }
        finally { updating = false; }
    }

    private void CloseRow() { editorHost.Content = null; rowEditor?.Close(); rowEditor = null; }
    private uint? ChooseColour(uint initial)
    {
        if (!Current || Busy) return null; colourOpen = true; UpdateStatus();
        try { return (ColourSelector ?? (value => NativeTerrainColourDialog.Choose(this, value)))(initial); }
        finally { colourOpen = false; UpdateStatus(); }
    }
    private void PresentChild(Window window)
    {
        if (!Current || Busy) { window.Close(); return; }
        child = window;
        window.Closed += (_, _) => { if (ReferenceEquals(child, window)) { child = null; UpdateStatus(); } };
        UpdateStatus();
        if (OptionsPresenter is { } present) present(window);
        else { window.Owner = this; window.ShowDialog(); }
    }
    private void OpenRow()
    {
        CloseRow(); var working = session.WorkingDocument;
        var row = working.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == SelectedIndex);
        // Reuse each complete source page and its typed/nested validation,
        // hosted inside the common window against the private document.
        rowEditor = family switch
        {
            "Terrains" => new GlobalTerrainWindow(working, row, () => Current, () => true) { ColourSelector = ChooseColour },
            "Rooms" => new GlobalRoomWindow(working, row, () => Current, () => true) { ColourSelector = ChooseColour },
            "Doors" => new GlobalDoorWindow(working, row, () => Current, () => true),
            "Traps" => new GlobalTrapWindow(working, row, () => Current, () => true) { OptionsPresenter = PresentChild },
            "Spells" => new GlobalSpellWindow(working, row, () => Current, () => true) { OptionsPresenter = PresentChild },
            _ => throw new InvalidOperationException("Unknown source page.")
        };
        var prefix = family switch { "Terrains" => "Terrain", "Rooms" => "Room", "Doors" => "Door", "Traps" => "Trap", _ => "Spell" };
        foreach (var suffix in new[] { "Apply", "Cancel" })
            if (rowEditor.FindName(prefix + "Properties" + suffix) is Button button) button.Visibility = Visibility.Collapsed;
        if (rowEditor.FindName(prefix + "PropertiesStatus") is FrameworkElement rowStatus) rowStatus.Visibility = Visibility.Collapsed;
        var content = rowEditor.Content; rowEditor.Content = null; editorHost.Content = content;
    }
    private bool SaveCurrent()
    {
        if (!session.WorkingDocument.GlobalDefinitionRows().Single(row => row.Family == family && row.Index == SelectedIndex).Editable) return true;
        return rowEditor switch
        {
            GlobalTerrainWindow terrain => terrain.ApplyTerrain(), GlobalRoomWindow room => room.ApplyRoom(),
            GlobalDoorWindow door => door.ApplyDoor(), GlobalTrapWindow trap => trap.ApplyTrap(), GlobalSpellWindow spell => spell.ApplySpell(), _ => false
        };
    }
    private long RowRevision => rowEditor switch
    { GlobalTerrainWindow terrain => terrain.InputRevision, GlobalRoomWindow room => room.InputRevision, GlobalDoorWindow door => door.InputRevision, GlobalTrapWindow trap => trap.InputRevision, GlobalSpellWindow spell => spell.InputRevision, _ => -1 };

    public bool Select(string selectedFamily, int index)
    {
        if (!Current || Busy || !GlobalVariablesSession.Families.Contains(selectedFamily)
            || !session.WorkingDocument.GlobalDefinitionRows().Any(row => row.Family == selectedFamily && row.Index == index)) return false;
        if (selectedFamily == family && index == SelectedIndex) return true;
        transitioning = true; UpdateStatus();
        try
        {
            if (!SaveCurrent() || !Current) { statusKey = "Invalid"; return false; }
            family = selectedFamily; selections[family] = index; RefreshSelectors(); OpenRow(); statusKey = "Draft"; return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException)
        { statusKey = "Invalid"; return false; }
        finally { transitioning = false; UpdateStatus(); }
    }
    public bool Reset(bool all)
    {
        if (!Current || Busy) return false; transitioning = true; UpdateStatus();
        try
        {
            // Source Reset replaces the pending page, even if its controls
            // currently contain invalid input. It never saves that bad input.
            document.ResetGlobalVariables(session, family, all ? null : SelectedIndex);
            OpenRow(); statusKey = "Draft"; return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException)
        { statusKey = "Invalid"; return false; }
        finally { transitioning = false; UpdateStatus(); }
    }
    public bool ApplyVariables()
    {
        if (!Current || Busy) return false; applying = true; UpdateStatus();
        try
        {
            if (!SaveCurrent() || !Current) { statusKey = "Invalid"; return false; }
            var plan = document.PrepareGlobalVariables(session); var revision = RowRevision; var editor = rowEditor;
            if (!commitFields() || !Current || !ReferenceEquals(editor, rowEditor) || revision != RowRevision || !document.IsGlobalVariablesPlanCurrent(plan))
            { statusKey = "Stale"; return false; }
            document.ApplyGlobalVariables(plan); CloseRow(); session = document.CreateGlobalVariablesSession();
            RefreshSelectors(); OpenRow(); statusKey = "Applied"; return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or FormatException or OverflowException)
        { statusKey = "Invalid"; return false; }
        finally { applying = false; UpdateStatus(); }
    }
    private void UpdateStatus()
    {
        status.Text = LocalizationService.Get("GlobalVariables." + (!Current ? "Stale" : child is not null || colourOpen ? "Choosing" : statusKey));
        apply.IsEnabled = resetCurrent.IsEnabled = resetAll.IsEnabled = Current && !Busy;
        pages.IsEnabled = rows.IsEnabled = Current && !Busy; editorHost.IsEnabled = Current && !Busy;
    }
    private void DocumentChanged(object? sender, EventArgs args)
    { if (!closed && !applying) { stale = true; UpdateStatus(); } }
    private void LanguageChanged(object? sender, EventArgs args)
    { if (!closed) { RefreshSelectors(); UpdateStatus(); } }
}
