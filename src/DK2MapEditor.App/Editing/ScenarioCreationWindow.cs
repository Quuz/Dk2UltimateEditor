using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public enum ScenarioCreationStep { Type, Parameters, Repeat }

/// <summary>All pages and nested editors share one private document. Finish is
/// the only operation that submits the composed result to the live owner.</summary>
public sealed class ScenarioCreationWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly GameResourceSettings gameResources;
    private readonly ScenarioCreationSession session;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly Action<NativeRecord> completed;
    private ScriptOwner? preferredAreaOwner;
    private bool retainEmptyAreaContext;
    private readonly ContentControl page = new();
    private readonly TextBlock status = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly ComboBox types = new() { Margin = new(4), DisplayMemberPath = "Label", VerticalAlignment = VerticalAlignment.Top };
    private readonly Button back = new(), next = new(), finish = new();
    private readonly ComboBox repeat = new() { Margin = new(4), DisplayMemberPath = "Label" };
    private readonly TextBox count = new() { Margin = new(4), MinWidth = 100, MaxLength = 4 };
    private readonly CheckBox rectangle = new() { Margin = new(4) };
    private readonly Button pickRectangle = new() { Margin = new(4) };
    private bool refreshing, closed, finishing;
    private long inputRevision;
    private string? statusKey;
    private sealed record TypeChoice(LegacyEventSchemas.EventType Type, string Label);
    private sealed record RepeatChoice(ScenarioRepeatMode Mode, string Label);
    public ScenarioCreationStep Step { get; private set; }
    public ScenarioParameterWindow? ParameterEditor { get; private set; }
    public Action<ScenarioParameterWindow>? SpawnPointRequester { get; set; }
    public Action<ScenarioParameterWindow>? SpatialPointRequester { get; set; }
    public Action<ScenarioParameterWindow>? RectangleRequester { get; set; }
    public ushort CreatedEventID { get; private set; }
    public bool IsEditingExisting => session.IsEditingExisting;

    public ScenarioCreationWindow(NativeMapDocument document, ScenarioCreationContext context, uint signature,
        Func<bool> ownerCurrent, Func<bool> commitFields, Action<NativeRecord> completed,
        LegacyEventSchemas.EventType? selectedType = null, ScriptEdge? insertion = null, GameResourceSettings? gameResources = null, NativeRecord? existingRecord = null,
        ScriptOwner? preferredAreaOwner = null)
    {
        this.document = document; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields; this.completed = completed;
        this.preferredAreaOwner = preferredAreaOwner;
        this.gameResources = gameResources ?? new();
        if (existingRecord is not null && (selectedType is not null || insertion is not null || EntityReferences.EventType(existingRecord)?.RecordSignature != signature))
            throw new ArgumentException("Existing event editing must start from the selected record in its category.");
        session = existingRecord is null ? document.CreateScenarioCreationSession(context, signature, selectedType, insertion)
            : document.CreateScenarioEditingSession(context, existingRecord);
        NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(TitleProperty, IsEditingExisting ? "ScenarioWizard.EditTitle" : "ScenarioWizard.Title");
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 700; Height = 580; MinWidth = 540; MinHeight = 420; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(12) }; Content = panel;
        var explanation = new TextBlock { Margin = new(4), TextWrapping = TextWrapping.Wrap };
        explanation.SetResourceReference(TextBlock.TextProperty, IsEditingExisting ? "ScenarioWizard.EditExplanation" : "ScenarioWizard.Explanation"); DockPanel.SetDock(explanation, Dock.Top); panel.Children.Add(explanation);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right }; footer.Children.Add(buttons);
        void Button(Button button, string key, string name, Action action)
        { button.Margin = new(4); button.SetResourceReference(ContentControl.ContentProperty, key); button.Click += (_, _) => action(); buttons.Children.Add(button); RegisterName(name, button); }
        Button(back, "ScenarioWizard.Back", "ScenarioWizardBack", () => Back());
        Button(next, "ScenarioWizard.Next", "ScenarioWizardNext", () => Next());
        Button(finish, "ScenarioWizard.Finish", "ScenarioWizardFinish", () => Finish());
        Button(new Button { IsCancel = true }, "ScenarioPage.Cancel", "ScenarioWizardCancel", Close);
        RegisterName("ScenarioWizardTypes", types); RegisterName("ScenarioWizardRepeat", repeat); RegisterName("ScenarioWizardCount", count); RegisterName("ScenarioWizardStatus", status);
        RegisterName("ScenarioWizardRectangle", rectangle); RegisterName("ScenarioWizardPickRectangle", pickRectangle);
        var rectangleCaption = new TextBlock { TextWrapping = TextWrapping.Wrap }; rectangleCaption.SetResourceReference(TextBlock.TextProperty, "ScenarioSpatial.Rectangle"); rectangle.Content = rectangleCaption;
        pickRectangle.SetResourceReference(ContentControl.ContentProperty, "ScenarioSpatial.PickRectangle");
        OriginalControlIcons.Decorate(pickRectangle,25);
        rectangle.Checked += (_, _) => RectangleChanged(); rectangle.Unchecked += (_, _) => RectangleChanged();
        pickRectangle.Click += (_, _) => { if (Current && !IsEditingExisting && !finishing && ParameterEditor is { } editor) RectangleRequester?.Invoke(editor); };
        panel.Children.Add(page);
        types.SelectionChanged += (_, _) =>
        {
            if (refreshing || closed || finishing || types.SelectedItem is not TypeChoice choice) return;
            inputRevision++;
            try { document.SelectScenarioCreationType(session, choice.Type); statusKey = null; RefreshButtons(); }
            catch (Exception error) when (Expected(error)) { Fail(error); }
        };
        repeat.SelectionChanged += (_, _) => { if (!refreshing) { inputRevision++; count.IsEnabled = repeat.SelectedItem is RepeatChoice { Mode: ScenarioRepeatMode.Count }; statusKey = null; RefreshButtons(); } };
        count.TextChanged += (_, _) =>
        {
            if (refreshing) return;
            inputRevision++;
            if (uint.TryParse(count.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 254)
            { refreshing = true; count.Text = "254"; refreshing = false; }
            statusKey = null; RefreshButtons();
        };
        document.Changed += OwnerChanged; LocalizationService.LanguageChanged += LanguageChanged;
        Closed += (_, _) =>
        {
            closed = true; ParameterEditor?.Close(); ParameterEditor = null; session.Dispose();
            document.Changed -= OwnerChanged; LocalizationService.LanguageChanged -= LanguageChanged; RefreshButtons();
        };
        RefreshLanguage(); ShowType();
    }
    private static bool Expected(Exception error) => error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or FormatException or OverflowException;
    private bool Current => !closed && ownerCurrent() && document.IsScenarioCreationSessionCurrent(session);
    private bool HasParameters => LegacyEventSchemas.ParameterPageID(session.Type.Name) != -1;
    private bool Supported => !HasParameters || ScenarioControlPages.Supports(session.Type.Name);
    private void Fail(Exception error) { statusKey = "AliasEvent.Failed"; status.ToolTip = error.Message; RefreshButtons(); }
    private void RefreshButtons()
    {
        var current = Current && !finishing;
        back.IsEnabled = current && Step != ScenarioCreationStep.Type;
        next.Visibility = Step == ScenarioCreationStep.Repeat || Step == ScenarioCreationStep.Parameters && session.Type.RecordSignature == 0xD6
            || Step == ScenarioCreationStep.Type && session.Type.RecordSignature == 0xD6 && !HasParameters ? Visibility.Collapsed : Visibility.Visible;
        next.IsEnabled = current && Supported;
        finish.Visibility = next.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        finish.IsEnabled = current && Supported;
        types.IsEnabled = repeat.IsEnabled = count.IsEnabled = current;
        rectangle.IsEnabled = current && !IsEditingExisting;
        pickRectangle.IsEnabled = current && session.Rectangle is not null;
        if (current && Step == ScenarioCreationStep.Repeat) count.IsEnabled = repeat.SelectedItem is RepeatChoice { Mode: ScenarioRepeatMode.Count };
        status.Text = LocalizationService.Get(!Current ? "AliasEvent.Stale" : !Supported ? "ScenarioWizard.Unsupported" : statusKey ?? "", "");
    }
    private void ShowType()
    {
        RememberAreaContext();
        ParameterEditor?.Close(); ParameterEditor = null;
        Step = ScenarioCreationStep.Type; page.Content = types; RefreshButtons();
    }
    private void ShowParameters()
    {
        RememberAreaContext();
        ParameterEditor?.Close();
        ParameterEditor = new(session.WorkingDocument, session.Record, () => Current, () => Current, privateCreation: true, gameResources: gameResources, privateEditing: IsEditingExisting,
            areaContexts: () => document.ScenarioCreationAreaContexts(session), preferredAreaOwner: preferredAreaOwner, retainEmptyAreaContext: retainEmptyAreaContext);
        ParameterEditor.SpawnPointRequester = child => SpawnPointRequester?.Invoke(child);
        ParameterEditor.SpatialPointRequester = child => SpatialPointRequester?.Invoke(child);
        var content = ParameterEditor.TakeCreationContent();
        if (session.Type.Name == "ACTION_CHANGESQUARE")
        {
            if (rectangle.Parent is Panel previous) { previous.Children.Remove(rectangle); previous.Children.Remove(pickRectangle); }
            var panel = new DockPanel(); var options = new StackPanel(); options.Children.Add(rectangle); options.Children.Add(pickRectangle); DockPanel.SetDock(options, Dock.Top); panel.Children.Add(options); panel.Children.Add(content); content = panel;
            refreshing = true; try { rectangle.IsChecked = session.Rectangle is not null; } finally { refreshing = false; }
            ParameterEditor.EnableSpatialPointInputs(session.Rectangle is null); pickRectangle.IsEnabled = session.Rectangle is not null;
        }
        Step = ScenarioCreationStep.Parameters; page.Content = content; RefreshButtons();
    }
    private void RememberAreaContext()
    {
        if (ParameterEditor?.HasAreaContextSelection != true) return;
        preferredAreaOwner = ParameterEditor.SelectedAreaContextOwner;
        retainEmptyAreaContext = preferredAreaOwner is null;
    }
    private void ShowRepeat()
    {
        ParameterEditor?.Close(); ParameterEditor = null;
        var value = session.Record.File.Data.Span[session.Record.Offset + (document.IsLegacyProject ? 55 : 23)];
        refreshing = true;
        try { repeat.SelectedItem = repeat.Items.Cast<RepeatChoice>().Single(item => item.Mode == (value == 1 ? ScenarioRepeatMode.Once : value == 255 ? ScenarioRepeatMode.Infinite : ScenarioRepeatMode.Count)); count.Text = value is 1 or 255 ? "2" : value.ToString(CultureInfo.InvariantCulture); }
        finally { refreshing = false; }
        if (repeat.Parent is Panel previous) { previous.Children.Remove(repeat); previous.Children.Remove(count); }
        var panel = new StackPanel(); var label = new TextBlock { Margin = new(4) }; label.SetResourceReference(TextBlock.TextProperty, "ScenarioPage.Repeat");
        panel.Children.Add(label); panel.Children.Add(repeat); panel.Children.Add(count);
        Step = ScenarioCreationStep.Repeat; page.Content = panel; RefreshButtons();
    }
    private bool StoreRepeat()
    {
        try { document.StoreScenarioCreationRepeat(session, ((RepeatChoice)repeat.SelectedItem).Mode, count.Text); return true; }
        catch (Exception error) when (Expected(error)) { Fail(error); return false; }
    }
    public bool Next()
    {
        if (!Current || finishing || !Supported) return false;
        if (Step == ScenarioCreationStep.Type)
        { if (HasParameters) ShowParameters(); else if (session.Type.RecordSignature == 0xD5) ShowRepeat(); else return false; return true; }
        if (Step == ScenarioCreationStep.Parameters && session.Type.RecordSignature == 0xD5 && ParameterEditor!.ApplyDraft())
        { ShowRepeat(); return true; }
        return false;
    }
    public bool Back()
    {
        if (!Current || finishing) return false;
        if (Step == ScenarioCreationStep.Parameters)
        { _ = ParameterEditor!.ApplyDraft(); ShowType(); return true; }
        if (Step == ScenarioCreationStep.Repeat && StoreRepeat())
        { if (HasParameters) ShowParameters(); else ShowType(); return true; }
        return false;
    }
    public bool Finish()
    {
        if (!Current || finishing || !Supported || finish.Visibility != Visibility.Visible) return false;
        finishing = true;
        try
        {
            if (Step == ScenarioCreationStep.Parameters && !ParameterEditor!.ApplyDraft() || Step == ScenarioCreationStep.Repeat && !StoreRepeat()) return false;
            var plan = document.PrepareScenarioCreation(session);
            var revision = inputRevision; var parameters = ParameterEditor;
            if (!commitFields() || !Current || !document.IsScenarioCreationPlanCurrent(plan)
                || inputRevision != revision || !ReferenceEquals(parameters, ParameterEditor) || ParameterEditor?.HasPendingInputs == true) return false;
            var record = document.ApplyScenarioEditorCreation(plan); CreatedEventID = ScriptGraph.ID(record); completed(record); Close(); return true;
        }
        catch (Exception error) when (Expected(error)) { Fail(error); return false; }
        finally { finishing = false; if (!closed) RefreshButtons(); }
    }
    private void RefreshLanguage()
    {
        refreshing = true;
        try
        {
            var type = session.Type;
            types.ItemsSource = session.AllowedTypes
                .OrderBy(item => item.Type).Select(item => new TypeChoice(item, LocalizationService.Get("Event." + item.Name, item.Name))).ToArray();
            types.SelectedItem = types.Items.Cast<TypeChoice>().Single(item => item.Type == type);
            var mode = repeat.SelectedItem is RepeatChoice selected ? selected.Mode : ScenarioRepeatMode.Once;
            repeat.ItemsSource = Enum.GetValues<ScenarioRepeatMode>().Select(item => new RepeatChoice(item, LocalizationService.Get("ScenarioPage.Repeat" + item))).ToArray();
            repeat.SelectedItem = repeat.Items.Cast<RepeatChoice>().Single(item => item.Mode == mode);
        }
        finally { refreshing = false; }
        RefreshButtons();
    }
    private void OwnerChanged(object? sender, EventArgs args) => RefreshButtons();
    private void LanguageChanged(object? sender, EventArgs args) { inputRevision++; RefreshLanguage(); }
    private void RectangleChanged()
    {
        if (refreshing || closed) return;
        inputRevision++;
        if (finishing)
        {
            refreshing = true;
            try { rectangle.IsChecked = session.Rectangle is not null; }
            finally { refreshing = false; }
            return;
        }
        if (!Current || ParameterEditor is null) return;
        if (IsEditingExisting)
        {
            refreshing = true; try { rectangle.IsChecked = false; } finally { refreshing = false; }
            return;
        }
        try
        {
            TileRect? bounds = null;
            if (rectangle.IsChecked == true)
            { var point = ParameterEditor.SpatialPoint; bounds = new(point.X, point.Y, 1, 1); }
            document.UpdateScenarioCreationRectangle(session, bounds);
            ParameterEditor.EnableSpatialPointInputs(rectangle.IsChecked != true); pickRectangle.IsEnabled = rectangle.IsChecked == true; inputRevision++;
        }
        catch (Exception error) when (Expected(error))
        {
            refreshing = true;
            try { rectangle.IsChecked = session.Rectangle is not null; }
            finally { refreshing = false; }
            ParameterEditor.EnableSpatialPointInputs(session.Rectangle is null);
            Fail(error);
        }
    }
    public bool AcceptRectangle(TileRect bounds)
    {
        if (!Current || IsEditingExisting || finishing || Step != ScenarioCreationStep.Parameters || rectangle.IsChecked != true || ParameterEditor is null) return false;
        try
        {
            document.UpdateScenarioCreationRectangle(session, bounds);
            if (!ParameterEditor.ApplySpatialPoint(new(bounds.X, bounds.Y))) return false;
            inputRevision++; return true;
        }
        catch (Exception error) when (Expected(error)) { Fail(error); return false; }
    }
}
