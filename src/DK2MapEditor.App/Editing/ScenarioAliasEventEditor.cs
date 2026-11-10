using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

internal sealed class ScenarioAliasEventEditor : UserControl
{
    private readonly NativeMapDocument document;
    private readonly NativeRecord record;
    private readonly Func<bool> ownerCurrent, commitFields;
    private readonly ComboBox primary = Picker(true), secondary = Picker(false), operation = Picker(false), mode = Picker(false);
    private readonly TextBox number = Input(), hours = Input(), minutes = Input(), seconds = Input();
    private readonly TextBlock heading = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap }, status = new() { Margin = new(4), TextWrapping = TextWrapping.Wrap };
    private readonly Button apply = new(), discard = new();
    private readonly StackPanel footer = new();
    private bool embedded;
    internal event Action? CloseRequested;
    internal long InputRevision => inputRevision;
    private readonly StackPanel primaryRow, secondaryRow, operationRow, modeRow, numberRow, timeRow;
    private readonly Dictionary<int, string> aliasInputs = [];
    private ScenarioAliasEventDraft draft = null!;
    private string primaryID = "", secondaryID = "", operationID = "", modeID = "";
    private string? statusKey;
    private bool refreshing, applying, writing, closed, stale, dirty;
    private long inputRevision;
    private sealed record Choice(string ID, string Label);
    private static ComboBox Picker(bool editable) => new() { Margin = new(4), MinWidth = 260, DisplayMemberPath = "Label", IsEditable = editable, IsTextSearchEnabled = false };
    private static TextBox Input() => new() { Margin = new(4), MinWidth = 90 };
    public bool HasPendingInputs => !closed && dirty;
    public ScenarioAliasEventEditor(NativeMapDocument document, NativeRecord record, Func<bool> ownerCurrent, Func<bool> commitFields)
    {
        this.document = document; this.record = record; this.ownerCurrent = ownerCurrent; this.commitFields = commitFields;
        draft = document.CreateScenarioAliasEventDraft(record);
        if (draft.Type is "TRIGGER_VARIABLE" or "ACTION_SETVARIABLE") number.MaxLength = 4;
        foreach (var box in new[] { hours, minutes, seconds }) box.MaxLength = 2;
        NameScope.SetNameScope(this, new NameScope());
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        var panel = new DockPanel { Margin = new(12) }; panel.SetResourceReference(Panel.BackgroundProperty, "Brush.Background"); Content = panel;
        DockPanel.SetDock(heading, Dock.Top); panel.Children.Add(heading);
        var explanation = new TextBlock { Margin = new(4), TextWrapping = TextWrapping.Wrap }; explanation.SetResourceReference(TextBlock.TextProperty, "AliasEvent.Explanation"); DockPanel.SetDock(explanation, Dock.Top); panel.Children.Add(explanation);
        var nameHint = new TextBlock { Margin = new(4), TextWrapping = TextWrapping.Wrap }; nameHint.SetResourceReference(TextBlock.TextProperty, "AliasEvent.NameHint"); nameHint.SetResourceReference(TextBlock.ForegroundProperty, "Brush.TextMuted"); DockPanel.SetDock(nameHint, Dock.Top); panel.Children.Add(nameHint);
        DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer); footer.Children.Add(status);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right }; footer.Children.Add(buttons);
        apply.SetResourceReference(ContentControl.ContentProperty, "AliasEvent.Apply"); apply.Click += (_, _) => ApplyDraft(); buttons.Children.Add(apply); RegisterName("AliasEventApply", apply);
        discard.SetResourceReference(ContentControl.ContentProperty, "AliasEvent.Discard"); discard.Click += (_, _) => ResetDraft(); buttons.Children.Add(discard); RegisterName("AliasEventDiscard", discard);
        var close = new Button { IsCancel = true }; close.SetResourceReference(ContentControl.ContentProperty, "AliasEvent.Close"); close.Click += (_, _) => CloseRequested?.Invoke(); buttons.Children.Add(close);
        var body = new StackPanel(); panel.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        StackPanel Row(string key, UIElement control) { var row = new StackPanel { Margin = new(0, 3, 0, 3) }; var label = new TextBlock { Margin = new(4) }; label.SetResourceReference(TextBlock.TextProperty, "AliasEvent." + key); row.Children.Add(label); row.Children.Add(control); body.Children.Add(row); return row; }
        primaryRow = Row("Primary", primary); operationRow = Row("Operation", operation); modeRow = Row("Operand", mode); secondaryRow = Row("Secondary", secondary); numberRow = Row("Number", number);
        var time = new WrapPanel(); time.Children.Add(hours); time.Children.Add(minutes); time.Children.Add(seconds); timeRow = Row("Time", time);
        RegisterName("AliasEventPrimary", primary); RegisterName("AliasEventSecondary", secondary); RegisterName("AliasEventOperation", operation); RegisterName("AliasEventMode", mode);
        RegisterName("AliasEventNumber", number); RegisterName("AliasEventHours", hours); RegisterName("AliasEventMinutes", minutes); RegisterName("AliasEventSeconds", seconds); RegisterName("AliasEventStatus", status);
        primary.SelectionChanged += (_, _) => ChooseAlias(primary, true); secondary.SelectionChanged += (_, _) => ChooseAlias(secondary, false);
        primary.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((_, args) => TypeAlias(primary, true, (args.OriginalSource as TextBox)?.Text ?? primary.Text)), true);
        secondary.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((_, args) => TypeAlias(secondary, false, (args.OriginalSource as TextBox)?.Text ?? secondary.Text)), true);
        operation.SelectionChanged += (_, _) => { if (refreshing || operation.SelectedItem is not Choice choice) return; operationID = choice.ID; InputsChanged(); RefreshActive(); };
        mode.SelectionChanged += (_, _) => { if (refreshing || mode.SelectedItem is not Choice choice) return; modeID = choice.ID; InputsChanged(); RefreshActive(); };
        foreach (var box in new[] { number, hours, minutes, seconds }) box.TextChanged += (_, _) => { if (!refreshing) InputsChanged(); };
        document.Changed += SourceChanged; LocalizationService.LanguageChanged += LanguageChanged;
        ResetDraft();
    }
    private void InputsChanged() { inputRevision++; dirty = true; statusKey = null; status.Text = ""; }
    private string Caption(string key) => LocalizationService.Get("AliasEvent." + key);
    private string AliasText(string id)
    {
        if (!int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) || index < 0 || index > (draft.Kind == ScenarioNameKind.Variable ? 128 : 16)) return Caption("Unknown") + " " + id;
        if (aliasInputs.TryGetValue(index, out var text)) return text;
        if (draft.Kind == ScenarioNameKind.Variable && index == 128) return Caption("Score");
        if (draft.Kind == ScenarioNameKind.Timer && index == 16) return Caption("TimeLimit");
        var name = draft.Name(index); return name.Length == 0 ? id : name;
    }
    private void List(ComboBox combo, IEnumerable<Choice> choices, string id, bool alias)
    {
        var items = choices.ToList(); if (items.All(choice => choice.ID != id)) items.Add(new(id, Caption("Unknown") + " " + id));
        combo.ItemsSource = items; combo.SelectedItem = items.Single(choice => choice.ID == id);
        if (alias) { combo.IsReadOnly = !int.TryParse(id, out var index) || index < 0 || index > (combo == primary ? draft.MaximumPrimaryID : 128); combo.Text = combo.IsReadOnly ? Caption("Unknown") + " " + id : AliasText(id); }
    }
    private void RefreshLists()
    {
        refreshing = true;
        try
        {
            heading.Text = LocalizationService.Get("Event." + draft.Type, draft.Type);
            List(primary, Enumerable.Range(0, draft.MaximumPrimaryID + 1).Select(id => new Choice(id.ToString(CultureInfo.InvariantCulture), AliasText(id.ToString(CultureInfo.InvariantCulture)))), primaryID, true);
            List(secondary, Enumerable.Range(0, 129).Select(id => new Choice(id.ToString(CultureInfo.InvariantCulture), AliasText(id.ToString(CultureInfo.InvariantCulture)))), secondaryID, draft.Type == "ACTION_SETVARIABLE");
            var trigger = draft.Type.StartsWith("TRIGGER_", StringComparison.Ordinal);
            List(operation, (trigger ? Enumerable.Range(1, 6) : new[] { 10, 18, 34, 12, 20, 36 }).Select(id => new Choice(id.ToString(), Caption((trigger ? "Compare" : "Action") + id))), operationID, false);
            List(mode, new[] { new Choice("1", Caption("Literal")), new Choice("2", Caption("Variable")) }, modeID, false);
            secondary.IsEditable = draft.Type == "ACTION_SETVARIABLE";
            RefreshActive(); if (statusKey is not null) status.Text = Caption(statusKey);
        }
        finally { refreshing = false; }
    }
    private void RefreshActive()
    {
        bool Visible(StackPanel row, bool visible) { row.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; return visible; }
        var variable = draft.Type == "TRIGGER_VARIABLE"; var action = draft.Type == "ACTION_SETVARIABLE";
        var sourceVariable = variable ? modeID == "2" : action && operationID is "12" or "20" or "36";
        var sourceNumber = variable ? modeID == "1" : action && operationID is "10" or "18" or "34";
        Visible(primaryRow, true); Visible(operationRow, draft.Type != "ACTION_SETTIMER"); Visible(modeRow, variable);
        secondary.IsEnabled = Visible(secondaryRow, sourceVariable); number.IsEnabled = Visible(numberRow, sourceNumber); Visible(timeRow, draft.Type == "TRIGGER_TIMER");
        apply.IsEnabled = !closed && !stale && ownerCurrent(); discard.IsEnabled = !closed && ownerCurrent();
    }
    private void ChooseAlias(ComboBox combo, bool first)
    {
        if (refreshing || combo.SelectedItem is not Choice choice) return;
        if (first) primaryID = choice.ID; else secondaryID = choice.ID;
        InputsChanged(); refreshing = true;
        try
        {
            if (combo.IsEditable)
            {
                combo.IsReadOnly = !int.TryParse(choice.ID, out var index) || index < 0 || index > (first ? draft.MaximumPrimaryID : 128);
                combo.Text = combo.IsReadOnly ? Caption("Unknown") + " " + choice.ID : AliasText(choice.ID);
            }
        }
        finally { refreshing = false; }
    }
    private void TypeAlias(ComboBox combo, bool first, string text)
    {
        if (refreshing || !combo.IsEditable || combo.IsReadOnly) return;
        var id = first ? primaryID : secondaryID;
        if (combo.SelectedItem is Choice choice && choice.ID != id || text == AliasText(id) || !int.TryParse(id, out var index)) return;
        aliasInputs[index] = text; InputsChanged();
        if (draft.Type == "ACTION_SETVARIABLE" && primaryID == secondaryID)
        { refreshing = true; try { (first ? secondary : primary).Text = text; } finally { refreshing = false; } }
    }
    private void SourceChanged(object? sender, EventArgs args)
    {
        if (writing || closed) return; stale = true; statusKey = "Stale"; status.Text = Caption(statusKey); apply.IsEnabled = false;
    }
    private void LanguageChanged(object? sender, EventArgs args) { if (closed) return; inputRevision++; RefreshLists(); }
    public void ResetDraft()
    {
        if (closed || !ownerCurrent()) { apply.IsEnabled = discard.IsEnabled = false; return; }
        try
        {
            draft = document.CreateScenarioAliasEventDraft(record); inputRevision++; aliasInputs.Clear(); stale = dirty = false; statusKey = null; status.Text = ""; status.ToolTip = null;
            primaryID = draft.Values[draft.PrimaryParameter]; operationID = draft.Values.TryGetValue(draft.Type.StartsWith("TRIGGER_") ? 1 : 2, out var op) ? op : "10";
            modeID = draft.Values.TryGetValue(3, out var value) ? value : "1"; secondaryID=draft.InitialSecondaryID;
            refreshing = true;
            try
            {
                number.Text = draft.Values.TryGetValue(5, out value) ? value : "0";
                var total = uint.Parse(number.Text, CultureInfo.InvariantCulture); hours.Text = (total / 3600).ToString(); minutes.Text = (total / 60 % 60).ToString(); seconds.Text = (total % 60).ToString();
            }
            finally { refreshing = false; }
            RefreshLists();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { stale = true; statusKey = "Failed"; status.Text = Caption(statusKey); status.ToolTip = exception.Message; apply.IsEnabled = false; }
    }
    internal void RegisterControls(Action<string, object> register)
    {
        foreach (var name in new[] { "Primary", "Secondary", "Operation", "Mode", "Number", "Hours", "Minutes", "Seconds", "Status", "Apply", "Discard" })
            register("AliasEvent" + name, FindName("AliasEvent" + name));
    }
    internal void Embed(Action<string, object> register)
    {
        embedded = true; footer.Visibility = Visibility.Collapsed; heading.Visibility = Visibility.Collapsed;
        RegisterControls(register);
    }
    internal void CloseEditor()
    {
        if (closed) return;
        closed = true; apply.IsEnabled = discard.IsEnabled = false;
        document.Changed -= SourceChanged; LocalizationService.LanguageChanged -= LanguageChanged;
    }
    internal ScenarioAliasEventDraft PrepareDraft()
    {
        if (closed || stale || !ownerCurrent()) throw new InvalidOperationException("Reopen the alias page after its owner or document changes.");
            var values = draft.Values.ToDictionary(value => value.Key, value => value.Value);
            values[draft.PrimaryParameter] = primaryID;
            if (draft.Type.StartsWith("TRIGGER_")) values[1] = operationID;
            if (draft.Type == "TRIGGER_VARIABLE") { values[3] = modeID; values[4] = secondaryID; if (modeID == "1") values[5] = number.Text; }
            if (draft.Type == "ACTION_SETVARIABLE") { values[2] = operationID; values[5] = operationID is "12" or "20" or "36" ? secondaryID : number.Text; }
            if (draft.Type == "TRIGGER_TIMER")
            {
                var h = uint.Parse(hours.Text, CultureInfo.InvariantCulture); var m = uint.Parse(minutes.Text, CultureInfo.InvariantCulture); var s = uint.Parse(seconds.Text, CultureInfo.InvariantCulture);
                // Original controls accept two decimal digits, including 60–99,
                // and Store adds them directly. Do not invent a 0–59 limit.
                if (m > 99 || s > 99) throw new ArgumentOutOfRangeException(nameof(minutes), "Minutes and seconds must fit the source two-digit controls.");
                values[5] = checked(h * 3600u + m * 60u + s).ToString(CultureInfo.InvariantCulture);
            }
            var aliases = new Dictionary<int, string>();
            void Alias(string id) { if (int.TryParse(id, out var index) && aliasInputs.TryGetValue(index, out var text)) aliases[index] = text; }
            Alias(primaryID); if (draft.Type == "ACTION_SETVARIABLE" && operationID is "12" or "20" or "36") Alias(secondaryID);
            return document.UpdateScenarioAliasEvent(draft, values.Select(value => new ScenarioEventParameter(value.Key, value.Value)), aliases.Select(value => new ScenarioEventAlias(value.Key, value.Value)));
    }
    internal void ApplyParentDraft(ScenarioParameterDraft parameters, ScenarioAliasEventDraft prepared, bool editorSave = false)
    {
        if (!embedded || closed || stale || applying || !ownerCurrent()) throw new InvalidOperationException("This alias page cannot accept a parent Apply.");
        applying = writing = true;
        try
        {
            if (editorSave) document.ApplyScenarioEditorAliasParameterDraft(parameters, prepared);
            else document.ApplyScenarioAliasParameterDraft(parameters, prepared);
        }
        finally { applying = writing = false; }
        ResetDraft();
    }
    public bool ApplyDraft()
    {
        if (embedded || closed || stale || applying || !ownerCurrent()) return false;
        applying = true;
        try
        {
            var revision = inputRevision; var prepared = PrepareDraft();
            if (!commitFields() || closed || stale || !ownerCurrent() || revision != inputRevision) return false;
            writing = true; try { document.ApplyScenarioAliasEvent(prepared); } finally { writing = false; }
            ResetDraft(); statusKey = "Applied"; status.Text = Caption(statusKey); return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { statusKey = "Failed"; status.Text = Caption(statusKey); status.ToolTip = exception.Message; return false; }
        finally { applying = false; }
    }
}
