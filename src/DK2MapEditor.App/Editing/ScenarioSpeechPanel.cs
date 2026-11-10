using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.App.Editing;

/// <summary>Original SAY list/caption/checkbox with private inputs.
/// Preview is read-only and never submits a map edit.</summary>
internal sealed class ScenarioSpeechPanel : StackPanel, IDisposable
{
    private readonly NativeMapDocument document;
    private readonly GameResourceSettings settings;
    private readonly Func<bool> current;
    private readonly Action<int> changed;
    private readonly TextBox identifier = new() { Margin = new(4), MaxLength = 4, MinWidth = 100 };
    private readonly CheckBox suppress = new() { Margin = new(4) };
    private readonly ListBox list = new() { Margin = new(4), Height = 145, DisplayMemberPath = "Label" };
    private readonly TextBox caption = new() { Margin = new(4), Height = 85, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly TextBlock resource = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock imported = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock importedID = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly Button play = new() { Margin = new(4) };
    private GameAudioPreview audio = new();
    private GameSpeechBank? bank;
    private GameTextResource? text;
    private bool updating, disposed;
    private string bankName = "", storedFlag = "0", resourceKey = "ScenarioSpeech.Missing";
    private sealed record Entry(int ID, string Label);
    public Action<AudioPreviewLaunch>? PlaybackLauncher { get; set; }

    internal ScenarioSpeechPanel(NativeMapDocument document, GameResourceSettings settings, Func<bool> current,
        Action<int> changed, Action<string, object> register)
    {
        this.document = document; this.current = current; this.changed = changed;
        this.settings = settings with { Root = settings.Root ?? MapSpeechTextWindow.DetectRoot(document.MainFile.SourcePath) };
        var label = new TextBlock { Margin = new(4) }; label.SetResourceReference(TextBlock.TextProperty, "ScenarioSpeech.Identifier"); Children.Add(label);
        Children.Add(identifier); register("ScenarioParam1", identifier);
        var range = new TextBlock { Margin = new(4) }; range.SetResourceReference(TextBlock.TextProperty, "ScenarioSpeech.Range"); Children.Add(range);
        importedID.SetResourceReference(TextBlock.TextProperty, "ScenarioPage.ImportedRange"); Children.Add(importedID); register("ScenarioSpeechImportedIdentifier", importedID);
        suppress.SetResourceReference(ContentControl.ContentProperty, "ScenarioSpeech.SuppressText"); Children.Add(suppress); register("ScenarioSpeechSuppressText", suppress);
        Children.Add(imported); Children.Add(resource); register("ScenarioSpeechResource", resource);
        Children.Add(list); register("ScenarioSpeechList", list);
        Children.Add(caption); register("ScenarioSpeechCaption", caption);
        play.SetResourceReference(ContentControl.ContentProperty, "SpeechText.Play"); Children.Add(play); register("ScenarioSpeechPlay", play);
        identifier.TextChanged += (_, _) => { if (!updating && !disposed) { importedID.Visibility = Visibility.Collapsed; changed(1); RefreshSelection(); } };
        void Toggle() { if (!updating && !disposed) { changed(5); imported.Visibility = Visibility.Collapsed; } }
        suppress.Checked += (_, _) => Toggle(); suppress.Unchecked += (_, _) => Toggle();
        suppress.Indeterminate += (_, _) => Toggle();
        list.SelectionChanged += (_, _) =>
        {
            if (updating || disposed || !current() || list.SelectedItem is not Entry entry) return;
            updating = true; try { identifier.Text = entry.ID.ToString(CultureInfo.InvariantCulture); } finally { updating = false; }
            importedID.Visibility = Visibility.Collapsed; changed(1); RefreshSelection();
        };
        list.MouseDoubleClick += (_, _) => PlaySelected();
        play.Click += (_, _) => PlaySelected();
    }
    private static bool ResourceError(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException
        or OverflowException or System.ComponentModel.Win32Exception or InvalidOperationException;
    public string Input(int parameter) => parameter switch { 1 => identifier.Text, 5 => suppress.IsChecked == true ? "1" : "0", _ => throw new ArgumentException("Unknown speech operand.") };
    public void Reset(ScenarioParameterDraft draft)
    {
        if (disposed) return;
        updating = true;
        try
        {
            string Value(int number) => draft.Values[draft.Fields.Single(field => field.Name == "Param" + number || field.Name == "param" + number).Name];
            var storedID = unchecked((uint)int.Parse(Value(1), CultureInfo.InvariantCulture));
            identifier.Text = storedID.ToString(CultureInfo.InvariantCulture);
            importedID.Visibility = storedID > 9999 ? Visibility.Visible : Visibility.Collapsed;
            storedFlag = unchecked((uint)int.Parse(Value(5), CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture); suppress.IsChecked = storedFlag != "0";
            imported.Visibility = storedFlag is "0" or "1" ? Visibility.Collapsed : Visibility.Visible;
            IsEnabled = current();
            bank = null; text = null; bankName = document.CreateMapSpeechTextDraft().Values.GetValueOrDefault("SpeechFile", "");
            resourceKey = "ScenarioSpeech.Missing";
            if (!string.IsNullOrWhiteSpace(settings.Root) && bankName.Length != 0)
            {
                try
                {
                    var found = MapTextCatalogues.SpeechBanks(settings.Root).FirstOrDefault(item => item.Name.Equals(bankName, StringComparison.OrdinalIgnoreCase));
                    if (found is not null)
                    {
                        bank = GameSpeechBank.Load(found.FilePath); resourceKey = "ScenarioSpeech.Bank";
                        try { text = GameTextResource.Load(MapTextCatalogues.TextResourcePath(settings.Root, found.TextFile)); }
                        catch (Exception error) when (ResourceError(error)) { /* Valid sound stays usable when captions are absent. */ }
                    }
                }
                catch (Exception error) when (ResourceError(error)) { bank = null; resourceKey = "ScenarioSpeech.InvalidBank"; }
            }
            list.ItemsSource = bank?.Entries.Select(entry => new Entry(entry.Identifier, $"{entry.Identifier} · {entry.Name} · {entry.Length}")).ToArray() ?? [];
        }
        finally { updating = false; }
        RefreshLanguage();
    }
    public void RefreshLanguage()
    {
        if (disposed) return;
        resource.Text = string.Format(CultureInfo.CurrentCulture, LocalizationService.Get(resourceKey), bankName, bank?.Entries.Count ?? 0);
        imported.Text = string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioSpeech.ImportedFlag"), storedFlag);
        RefreshSelection();
    }
    private void RefreshSelection()
    {
        if (disposed) return;
        var id = int.TryParse(identifier.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : -1;
        updating = true;
        try { list.SelectedItem = list.Items.Cast<Entry>().FirstOrDefault(entry => entry.ID == id); }
        finally { updating = false; }
        caption.Text = id == 0 ? LocalizationService.Get("ScenarioSpeech.NoSound")
            : text is not null && id >= 1 && id <= text.Entries.Count ? text.Entry(id).Preview : LocalizationService.Get("ScenarioSpeech.NoCaption");
        play.IsEnabled = IsEnabled && current() && bank is not null && id >= 1 && id <= bank.Entries.Count && bank.Entry(id).PlayableHeader;
    }
    public bool PlaySelected()
    {
        if (disposed || !IsEnabled || !current() || bank is null || list.SelectedItem is not Entry entry || !bank.Entry(entry.ID).PlayableHeader) return false;
        try
        {
            var path = audio.Prepare(bank.AudioPayload(entry.ID));
            if (!current()) return false;
            (PlaybackLauncher ?? GameAudioPreview.Launch)(new(path, settings.Player));
            if (!disposed) resource.Text = LocalizationService.Get("SpeechText.Played");
            return true;
        }
        catch (Exception error) when (ResourceError(error))
        { if (!disposed) resource.Text = LocalizationService.Get("SpeechText.PlaybackFailed"); return false; }
    }
    public void Invalidate()
    {
        if (disposed) return;
        IsEnabled = false; play.IsEnabled = false; audio.Dispose(); audio = new();
    }
    public void Dispose() { if (disposed) return; disposed = true; IsEnabled = false; play.IsEnabled = false; audio.Dispose(); }
}
