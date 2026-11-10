using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class HistorySettingsWindow : Window
{
    private Action<int>? apply;
    private Action? resetMemoryNotices;
    private string? statusKey;
    public bool Applied { get; private set; }
    public HistorySettingsWindow(int depth, Action<int> callback,Action? resetMemoryNotices=null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        this.resetMemoryNotices=resetMemoryNotices;
        if (depth is < 0 or > NativeMapDocument.MaximumHistoryDepth) throw new ArgumentOutOfRangeException(nameof(depth));
        apply = callback; InitializeComponent(); DepthInput.Text = depth.ToString(CultureInfo.InvariantCulture);
        ResetMemoryButton.IsEnabled=resetMemoryNotices is not null;
        LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) =>
        {
            apply = null; this.resetMemoryNotices=null; LocalizationService.LanguageChanged -= LanguageChanged;
            ApplyButton.IsEnabled = DefaultButton.IsEnabled = DepthInput.IsEnabled = ConfirmClear.IsEnabled = ResetMemoryButton.IsEnabled = false;
        };
    }
    private void LanguageChanged(object? sender, EventArgs args) => StatusText.Text = statusKey is null ? "" : LocalizationService.Get(statusKey);
    private void SetStatus(string? key) { statusKey = key; LanguageChanged(null, EventArgs.Empty); }
    private void Input_Changed(object sender, TextChangedEventArgs args)
    {
        if (ConfirmClear is null) return;
        ConfirmClear.IsChecked = false; SetStatus(null);
    }
    private void Confirm_Changed(object sender, RoutedEventArgs args) { if (StatusText is not null) SetStatus(null); }
    private void Default_Click(object sender, RoutedEventArgs args) => DepthInput.Text = NativeMapDocument.DefaultHistoryDepth.ToString(CultureInfo.InvariantCulture);
    private void ResetMemory_Click(object sender,RoutedEventArgs args)
    {if(resetMemoryNotices is null)return;try{resetMemoryNotices();SetStatus("HistoryMemory.Restored");}catch(InvalidOperationException){SetStatus("History.SettingsFailed");}}
    public bool ApplyDepth()
    {
        if (apply is null) return false;
        var text = DepthInput.Text.Trim();
        if (text.Length == 0 || text.Any(character => character is < '0' or > '9') ||
            !int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var depth) || depth > NativeMapDocument.MaximumHistoryDepth)
        { SetStatus("History.InvalidDepth"); return false; }
        if (depth == 0 && ConfirmClear.IsChecked != true) { SetStatus("History.NeedClearConfirmation"); return false; }
        try { apply(depth); Applied = true; SetStatus(null); return true; }
        catch (InvalidOperationException) { SetStatus("History.SettingsFailed"); return false; }
    }
    private void Apply_Click(object sender, RoutedEventArgs args) { if (ApplyDepth()) Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs args) => Close();
}
