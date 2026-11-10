using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public enum ScenarioCheckPurpose { Test, Compile, Run }
public enum UnusedScriptDecision { Cancel, Keep, Remove }
public sealed record UnusedScriptRequest(ScenarioCheckPurpose Purpose, UnusedScriptPlan Plan);

/// <summary>Immutable read-only candidates. A response is not deletion authority:
/// the calling shell must recheck the document, lifetime and plan.</summary>
public sealed class UnusedScriptConfirmationWindow : Window
{
    private readonly UnusedScriptRequest request;
    private readonly TextBlock summary = new() { TextWrapping = TextWrapping.Wrap, Margin = new(4) };
    private readonly DataGrid candidates = new() { IsReadOnly = true, AutoGenerateColumns = false,
        CanUserAddRows = false, CanUserDeleteRows = false, Height = 210, Margin = new(4), SelectionMode = DataGridSelectionMode.Single };
    private readonly DataGridTextColumn idColumn = new() { Binding = new Binding("ID"), Width = 70 };
    private readonly DataGridTextColumn kindColumn = new() { Binding = new Binding("Kind"), Width = 120 };
    private readonly DataGridTextColumn typeColumn = new() { Binding = new Binding("Type"), MinWidth = 300,
        Width = new DataGridLength(1, DataGridLengthUnitType.Star) };
    private bool closed;
    public UnusedScriptDecision Decision { get; private set; } = UnusedScriptDecision.Cancel;
    public UnusedScriptConfirmationWindow(UnusedScriptRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Plan.CanApply || !request.Plan.HasWork) throw new ArgumentException("A valid nonempty preview is required.", nameof(request));
        this.request = request; NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(TitleProperty, "UnusedScripts.Title");
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 640; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new(16) }; Content = panel;
        panel.SetResourceReference(Panel.BackgroundProperty, "Brush.Background");
        panel.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Brush.Text");
        panel.Children.Add(summary); RegisterName("UnusedScriptsSummary", summary);
        typeColumn.ElementStyle = new Style(typeof(TextBlock)) { Setters =
            { new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap), new Setter(FrameworkElement.ToolTipProperty, new Binding("Type")) } };
        candidates.Columns.Add(idColumn); candidates.Columns.Add(kindColumn); candidates.Columns.Add(typeColumn);
        panel.Children.Add(candidates); RegisterName("UnusedScriptsCandidates", candidates);
        var explanation = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(4) };
        explanation.SetResourceReference(TextBlock.TextProperty, "UnusedScripts.Explanation"); panel.Children.Add(explanation);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        AddButton("UnusedScriptsKeep", "UnusedScripts.Keep", UnusedScriptDecision.Keep, true, false);
        AddButton("UnusedScriptsRemove", "UnusedScripts.Remove", UnusedScriptDecision.Remove, false, false);
        AddButton("UnusedScriptsCancel", "UnusedScripts.Cancel", UnusedScriptDecision.Cancel, false, true);
        panel.Children.Add(buttons);
        LocalizationService.LanguageChanged += LanguageChanged; LanguageChanged(null, EventArgs.Empty);
        Closed += (_, _) => { closed = true; LocalizationService.LanguageChanged -= LanguageChanged; };
        void AddButton(string name, string key, UnusedScriptDecision decision, bool defaultButton, bool cancel)
        {
            var button = new Button { IsDefault = defaultButton, IsCancel = cancel };
            button.SetResourceReference(ContentControl.ContentProperty, key); button.Click += (_, _) => Respond(decision);
            RegisterName(name, button); buttons.Children.Add(button);
        }
    }
    private sealed record Row(ushort ID, string Kind, string Type);
    private void LanguageChanged(object? sender, EventArgs args)
    {
        summary.Text = string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("UnusedScripts.Summary"),
            LocalizationService.Get("UnusedScripts.Purpose." + request.Purpose), request.Plan.Conditions, request.Plan.Actions, request.Plan.ReachableCount);
        idColumn.Header = LocalizationService.Get("UnusedScripts.ID"); kindColumn.Header = LocalizationService.Get("UnusedScripts.Kind"); typeColumn.Header = LocalizationService.Get("UnusedScripts.Type");
        candidates.ItemsSource = request.Plan.Events.Select(item => new Row(item.ID,
            LocalizationService.Get(item.Condition ? "UnusedScripts.Condition" : "UnusedScripts.Action"),
            item.TypeName is null ? LocalizationService.Get("UnusedScripts.Unknown") : LocalizationService.Get("Event." + item.TypeName, item.TypeName))).ToArray();
    }
    public bool Respond(UnusedScriptDecision decision)
    {
        if (closed || !Enum.IsDefined(decision)) return false;
        Decision = decision; Close(); return true;
    }
}
