using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source catalogue order/grouping and images; only an explicit choice changes the caller's draft.</summary>
public sealed class ObjectImagePickerWindow : Window
{
    private readonly PlacedThingKind kind;
    private readonly int version;
    private readonly Func<bool> canPick;
    private readonly Action<uint> chosen;
    private readonly bool bandMember, includeSpecialItems;
    private readonly TextBox search = new();
    private readonly TreeView tree = new();
    private readonly Button pick = new();
    private uint selected;
    private bool closed, updating;
    public sealed record Item(uint ID, string Group, string Label, BitmapSource Icon);
    public IReadOnlyList<Item> Items { get; private set; } = [];
    public uint? SelectedID => tree.SelectedItem is TreeViewItem { Tag: Item item } ? item.ID : null;
    public ObjectImagePickerWindow(PlacedThingKind kind, int version, uint current, Func<bool> canPick, Action<uint> chosen,
        bool bandMember = false, bool includeSpecialItems = false)
    {
        this.kind = kind; this.version = version; selected = current; this.canPick = canPick; this.chosen = chosen;
        if (bandMember && kind != PlacedThingKind.Creature || includeSpecialItems && kind != PlacedThingKind.Object) throw new ArgumentException("Invalid source image catalogue context.");
        this.bandMember = bandMember; this.includeSpecialItems = includeSpecialItems;
        NameScope.SetNameScope(this, new NameScope());
        Width = 580; Height = 690; MinWidth = 400; MinHeight = 380; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(TitleProperty, "ObjectImages.Choose"); SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        var panel = new DockPanel { Margin = new(12) }; panel.SetResourceReference(Panel.BackgroundProperty, "Brush.Background");
        var bottom = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        DockPanel.SetDock(bottom, Dock.Bottom); panel.Children.Add(bottom);
        Named(pick, "ObjectImagePick"); pick.SetResourceReference(ContentControl.ContentProperty, "ObjectImages.Select"); pick.Click += (_, _) => ApplyChoice(); bottom.Children.Add(pick);
        var cancel = new Button(); cancel.SetResourceReference(ContentControl.ContentProperty, "Editor.Cancel"); cancel.Click += (_, _) => Cancel(); bottom.Children.Add(cancel);
        Named(search, "ObjectImageSearch"); search.SetResourceReference(ToolTipProperty, "Editor.Search"); DockPanel.SetDock(search, Dock.Top); panel.Children.Add(search);
        var searchLabel = new TextBlock { Margin = new(0, 0, 0, 4) }; searchLabel.SetResourceReference(TextBlock.TextProperty, "Editor.Search");
        DockPanel.SetDock(searchLabel, Dock.Top); panel.Children.Insert(1, searchLabel);
        Named(tree, "ObjectImageTree"); panel.Children.Add(tree); Content = panel;
        search.TextChanged += (_, _) => Refresh();
        tree.SelectedItemChanged += (_, _) => { if (updating) return; if (SelectedID is { } id) selected = id; pick.IsEnabled = SelectedID is not null && !closed; };
        tree.MouseDoubleClick += (_, e) => { if (SelectedID is not null) { ApplyChoice(); e.Handled = true; } };
        LocalizationService.LanguageChanged += LanguageChanged; Closed += (_, _) => Release(); Refresh();
    }
    private void Named(FrameworkElement element, string name) { element.Name = name; RegisterName(name, element); }
    private void LanguageChanged(object? sender, EventArgs e) => Refresh();
    private void Refresh()
    {
        if (closed) return; updating = true;
        try
        {
            var groups = bandMember ? new[] { "BadCreatures", "Heros" } : OriginalObjectImages.Groups(kind, version);
            if (includeSpecialItems) groups = groups.Concat(OriginalObjectImages.Groups(PlacedThingKind.SpecialItem, version)).ToArray();
            Items = groups.SelectMany(group => LegacyCompilerRules.Elements(group).Select(element =>
                new Item(element.ID, group, element.ID.ToString(CultureInfo.InvariantCulture) + " · " + ValueCatalogues.Named(element, element.ID.ToString()).Name(LocalizationService.CurrentLanguage),
                    OriginalObjectImages.For(group.StartsWith("Magics", StringComparison.Ordinal) ? PlacedThingKind.SpecialItem : kind, element.ID, version, chooser: true)))).ToArray();
            tree.Items.Clear();
            foreach (var group in Items.GroupBy(item => item.Group))
            {
                var filtered = group.Where(item => item.Label.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase)).ToArray();
                if (filtered.Length == 0) continue;
                var root = new TreeViewItem { Header = LocalizationService.Get("ObjectImages." + group.Key), IsExpanded = true };
                tree.Items.Add(root);
                foreach (var item in filtered)
                {
                    var header = new StackPanel { Orientation = Orientation.Horizontal };
                    var size = item.Icon.PixelWidth;
                    header.Children.Add(new Image { Source = item.Icon, Width = size, Height = size, Margin = new(0, 2, 8, 2) });
                    header.Children.Add(new TextBlock { Text = item.Label, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 380 });
                    var leaf = new TreeViewItem { Header = header, Tag = item }; root.Items.Add(leaf);
                    leaf.IsSelected = item.ID == selected;
                    if (leaf.IsSelected) leaf.Loaded += (_, _) => { if (!closed && leaf.IsSelected) leaf.BringIntoView(); };
                }
            }
            pick.IsEnabled = SelectedID is not null;
        }
        finally { updating = false; }
    }
    public bool ApplyChoice()
    {
        if (closed || SelectedID is not { } id || !canPick() || closed) return false;
        chosen(id); Cancel(); return true;
    }
    public void Cancel() { Release(); Close(); }
    private void Release()
    {
        if (closed) return; closed = true; LocalizationService.LanguageChanged -= LanguageChanged; tree.Items.Clear(); Items = []; pick.IsEnabled = false;
    }
}
