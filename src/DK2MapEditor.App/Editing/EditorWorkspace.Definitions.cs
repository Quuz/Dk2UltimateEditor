using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record DefinitionChoice(string ID, string Label);
    private sealed record DefinitionView(GlobalDefinitionRow Row, string Label)
    { public System.Windows.Media.Imaging.BitmapSource? Icon=>OriginalDefinitionImages.For(Row.Family,Row.Index); }
    private sealed record DefinitionFieldView(GlobalDefinitionValue Field, string Label, string Value, string Default, string Raw);
    private bool updatingDefinitions;
    private readonly Dictionary<string, int> definitionSelections = [];
    private static string DefinitionText(string key) => LocalizationService.Get("Editor.Definitions." + key);
    private static string DefinitionFieldLabel(string family,string field,Func<string,string?>? read=null) =>
        DefinitionCaptionRules.For(family,field,read)?.Render(key=>LocalizationService.Get(key))??FieldLabel(field);
    private static string DefinitionName(GlobalDefinitionRow row, string language)
    {
        if (row.Family is "Terrains" or "Rooms") return LocalizationService.GetForLanguage("Terrain." + row.ID, language);
        return ValueCatalogues.Named(GlobalDefinitionDefaults.Catalogue(row.Family)[row.Index], row.ID.ToString()).Name(language);
    }
    private void InvalidateDefinitionEditing()
    {
        CreatureHostedInputRevision++;
        if (DefinitionConfirmed is null) return;
        DefinitionConfirmed.IsChecked = false; DefinitionStatus.Text = ""; DefinitionStatus.ToolTip = null;
    }
    private void ResetDefinitionEditing()
    {
        CloseTerrainProperties(); CloseRoomProperties(); CloseDoorProperties(); CloseTrapProperties(); CloseSpellProperties();
        InvalidateDefinitionEditing(); updatingDefinitions = true;
        projectileState = null;
        jobsDraft = null;
        attackState = null; attackForms.Clear(); attackSlot = "Shot1";
        creatureOptionForms.Clear();
        globalRuleForms.Clear();
        decorationForms.Clear();
        definitionSelections.Clear();
        try { DefinitionSearch.Text = ""; DefinitionNumber.Text = ""; DefinitionFamily.ItemsSource = DefinitionSection.ItemsSource = DefinitionPicker.ItemsSource = DefinitionTable.ItemsSource = null; }
        finally { updatingDefinitions = false; }
        RefreshDefinitions();
    }
    private void RefreshDefinitions()
    {
        if (DefinitionTable is null || document is not null && WorkspaceTabs.SelectedIndex != 9) return;
        var family = (DefinitionFamily.SelectedItem as DefinitionChoice)?.ID ?? "Terrains";
        var section = (DefinitionSection.SelectedItem as DefinitionChoice)?.ID;
        var selected = (DefinitionPicker.SelectedItem as DefinitionView)?.Row;
        var field = DefinitionTable.SelectedItem as DefinitionFieldView; var draft = DefinitionNumber.Text;
        updatingDefinitions = true;
        try
        {
            DefinitionPanel.IsEnabled = document is not null;
            DefinitionGlobalVariables.IsEnabled = !scenarioNamesOwnerClosed && document?.GlobalDefinitionRows().Any(row => GlobalVariablesSession.Families.Contains(row.Family) && row.Editable) == true;
            DefinitionCreatureGlobals.IsEnabled = !scenarioNamesOwnerClosed && document?.GlobalDefinitionRows().Any(row => CreatureGlobalsSession.Families.Contains(row.Family) && row.Editable) == true;
            DefinitionFamily.ItemsSource = LegacyGlobalSchemas.All.Select(item => new DefinitionChoice(item.Group, DefinitionText(item.Group))).ToArray();
            DefinitionFamily.SelectedItem = DefinitionFamily.Items.Cast<DefinitionChoice>().First(item => item.ID == family);
            var sections = GlobalDefinitionDefaults.Sections.Where(item => item.Family == family).Select(item => new DefinitionChoice(item.Name, DefinitionText(item.Name))).ToArray();
            DefinitionSection.ItemsSource = sections; DefinitionSection.SelectedItem = sections.FirstOrDefault(item => item.ID == section) ?? sections[0];
            var query = DefinitionSearch.Text.Trim();
            var rows = (document?.GlobalDefinitionRows() ?? []).Where(row => row.Family == family).Select(row => new DefinitionView(row, $"#{row.ID:X2} · " + DefinitionName(row, LocalizationService.CurrentLanguage)))
                .Where(view => query.Length == 0 || new[] { view.Label, view.Row.Symbol, DefinitionName(view.Row, "en"), DefinitionName(view.Row, "ru") }.Any(text => text.Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();
            DefinitionPicker.DisplayMemberPath="";DefinitionPicker.ItemTemplate=OriginalObjectImages.NamedTemplate();
            DefinitionPicker.ItemsSource = rows;
            var remembered = selected?.Family == family ? selected.Index : definitionSelections.GetValueOrDefault(family, -1);
            DefinitionPicker.SelectedItem = rows.FirstOrDefault(view => view.Row.Index == remembered) ?? rows.FirstOrDefault();
            RefreshDefinitionFields(field?.Field.Name);
            if (DefinitionTable.SelectedItem is DefinitionFieldView updated && selected?.Family == (DefinitionPicker.SelectedItem as DefinitionView)?.Row.Family
                && selected?.Index == (DefinitionPicker.SelectedItem as DefinitionView)?.Row.Index && updated.Field.Name == field?.Field.Name && updated.Value == field.Value)
                DefinitionNumber.Text = draft;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException)
        {
            DefinitionPicker.ItemsSource = DefinitionTable.ItemsSource = null; DefinitionApply.IsEnabled = DefinitionReset.IsEnabled = DefinitionResetAll.IsEnabled = DefinitionProperties.IsEnabled = false;
            DefinitionGlobalVariables.IsEnabled = false;
            DefinitionMessage("Failed", exception.Message);
        }
        finally { updatingDefinitions = false; }
    }
    private void RefreshDefinitionFields(string? selectedField = null)
    {
        var row = (DefinitionPicker.SelectedItem as DefinitionView)?.Row;
        var section = (DefinitionSection.SelectedItem as DefinitionChoice)?.ID;
        if (row is not null) definitionSelections[row.Family] = row.Index;
        string Display(string name, string raw) => DefinitionRaw.IsChecked == true || row is null ? raw : GlobalDefinitionNumbers.Display(row.Family, name, raw);
        var allValues=row is null?[]:document!.GlobalDefinitionValues(row).ToArray();
        string? Read(string field){var value=allValues.FirstOrDefault(value=>value.Name==field);return value?.Value.Length==0?value.DefaultValue:value?.Value;}
        var values = row is null ? [] : allValues.Where(value => value.Section == section).Select(value => new DefinitionFieldView(value, DefinitionFieldLabel(row.Family,value.Name,Read), value.Value.Length == 0 ? "—" : Display(value.Name, value.Value), Display(value.Name, value.DefaultValue), value.Value.Length == 0 ? "—" : value.Value)).ToArray();
        var style = new Style(typeof(TextBlock)); style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
        DefinitionTable.Columns.Clear();
        foreach (var (property, key, width) in new[] { ("Label", "Field", 480), ("Value", "Value", 170), ("Default", "Default", 210), ("Raw", "RawValue", 150) })
            DefinitionTable.Columns.Add(new DataGridTextColumn { Header = DefinitionText(key), Binding = new Binding(property), ElementStyle = style, MinWidth = width, Width = new(property == "Label" ? 2 : 1, DataGridLengthUnitType.Star) });
        DefinitionTable.ItemsSource = values; DefinitionTable.SelectedItem = values.FirstOrDefault(value => value.Field.Name == selectedField) ?? values.FirstOrDefault();
        DefinitionNotice.Text = row is null ? "" : DefinitionText(!row.Editable ? "ReadOnly" : row.Record is null ? "SourceDefaults" : "Stored");
        DefinitionProperties.IsEnabled = row?.Record is not null;
        DefinitionTerrainProperties.Visibility = row?.Family == "Terrains" ? Visibility.Visible : Visibility.Collapsed;
        DefinitionTerrainProperties.IsEnabled = row?.Family == "Terrains";
        DefinitionRoomProperties.Visibility = row?.Family == "Rooms" ? Visibility.Visible : Visibility.Collapsed;
        DefinitionRoomProperties.IsEnabled = row?.Family == "Rooms";
        DefinitionDoorProperties.Visibility = row?.Family == "Doors" ? Visibility.Visible : Visibility.Collapsed;
        DefinitionDoorProperties.IsEnabled = row?.Family == "Doors";
        DefinitionTrapProperties.Visibility = row?.Family == "Traps" ? Visibility.Visible : Visibility.Collapsed;
        DefinitionTrapProperties.IsEnabled = row?.Family == "Traps";
        DefinitionSpellProperties.Visibility = row?.Family == "Spells" ? Visibility.Visible : Visibility.Collapsed;
        DefinitionSpellProperties.IsEnabled = row?.Family == "Spells";
        DefinitionReset.IsEnabled = row?.Editable == true;
        var familyRows = document?.GlobalDefinitionRows().Where(item => item.Family == row?.Family).ToArray() ?? [];
        DefinitionResetAll.IsEnabled = row is not null && familyRows.Any(item => item.Editable)
            && familyRows.All(item => item.Editable || document?.IsLegacyProject == false && item.Record is null);
        LoadDefinitionField();
        RefreshProjectiles();
        RefreshCreatureJobs();
        RefreshCreatureAttacks();
        RefreshCreatureOptions();
        RefreshGlobalRules();
        RefreshDecorations();
    }
    private void LoadDefinitionField()
    {
        var selected = DefinitionTable.SelectedItem as DefinitionFieldView;
        DefinitionNumber.Text = selected?.Field.Editable == true ? selected.Value : "";
        DefinitionApply.IsEnabled = DefinitionNumber.IsEnabled = selected?.Field.Editable == true;
        var row = (DefinitionPicker.SelectedItem as DefinitionView)?.Row;
        var flag = row is not null && selected is not null && LegacyGlobalSchemas.All.Single(family => family.Group == row.Family).Fields.Single(field => field.Name == selected.Field.Name).BitMask != 0;
        DefinitionFlag.Visibility = flag ? Visibility.Visible : Visibility.Collapsed;
        DefinitionFlag.IsEnabled = selected?.Field.Editable == true;
        DefinitionFlag.ItemsSource = flag ? new[] { new DefinitionChoice("0", DefinitionText("No")), new DefinitionChoice("1", DefinitionText("Yes")) } : [];
        DefinitionFlag.SelectedValue = selected?.Field.Value;
        var format = row is null || selected is null ? null : GlobalDefinitionNumbers.Format(row.Family, selected.Field.Name, document?.IsLegacyProject == false);
        DefinitionFormat.Text = format is null ? "" : DefinitionText(DefinitionRaw.IsChecked == true ? "RawHint" : format.Fractional ? "FixedHint" : format.SignedAttribute ? "SignedHint" : format.Percent ? "PercentHint" : "IntegerHint");
        if (DefinitionRaw.IsChecked != true && format?.Minimum is { } minimum && format.Maximum is { } maximum)
            DefinitionFormat.Text += $" · {minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)} … {maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
    private void DefinitionMode_Changed(object sender, RoutedEventArgs e)
    {
        if (updatingDefinitions || DefinitionTable is null) return;
        var field = (DefinitionTable.SelectedItem as DefinitionFieldView)?.Field.Name;
        InvalidateDefinitionEditing(); updatingDefinitions = true;
        try { RefreshDefinitionFields(field); }
        finally { updatingDefinitions = false; }
    }
    private void DefinitionFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (updatingDefinitions || DefinitionTable is null) return;
        InvalidateDefinitionEditing(); RefreshDefinitions();
    }
    private void DefinitionSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingDefinitions || DefinitionTable is null) return;
        InvalidateDefinitionEditing(); updatingDefinitions = true;
        try { if (sender == DefinitionTable) LoadDefinitionField(); else RefreshDefinitionFields(); }
        finally { updatingDefinitions = false; }
    }
    private void DefinitionFlag_Changed(object sender, SelectionChangedEventArgs e)
    { if (!updatingDefinitions && DefinitionFlag.SelectedItem is DefinitionChoice choice) DefinitionNumber.Text = choice.ID; }
    private void DefinitionDraft_Changed(object sender, TextChangedEventArgs e)
    { if (!updatingDefinitions) InvalidateDefinitionEditing(); }
    private void DefinitionMessage(string key, string? detail = null)
    { DefinitionStatus.SetResourceReference(TextBlock.TextProperty, "Editor.Definitions." + key); DefinitionStatus.ToolTip = detail; }
    private void DefinitionEdit_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || DefinitionPicker.SelectedItem is not DefinitionView view) return;
        var operation = (sender as Button)?.Tag as string ?? "Apply";
        if (operation != "Apply" && DefinitionConfirmed.IsChecked != true || !CommitFields()) return;
        if (operation != "Apply" && DefinitionConfirmed.IsChecked != true) return;
        try
        {
            if (operation == "Apply")
            {
                if (DefinitionTable.SelectedItem is not DefinitionFieldView field) return;
                GlobalDefinitionChange[] changes = [new(view.Row.Family, view.Row.Index, field.Field.Name, DefinitionNumber.Text)];
                if (DefinitionRaw.IsChecked == true) document.SetGlobalDefinitionValues(changes);
                else document.SetGlobalDefinitionDisplayValues(changes);
            }
            else if (DefinitionSection.SelectedItem is DefinitionChoice section)
            {
                document.ResetGlobalDefinitions(section.ID, operation == "All" ? null : [view.Row.Index]);
                // An explicit reset also resets the corresponding dialog draft,
                // including a stored no-op. Other source groups retain drafts.
                if (GlobalProjectiles.Slots(view.Row.Family).Count > 0) projectileState = null;
                if (section.ID == "Jobs") jobsDraft = null;
                if (section.ID == "Attacks") { attackState = null; attackForms.Clear(); }
                creatureOptionForms.Remove(section.ID);
                globalRuleForms.Remove(view.Row.Family);
                if (view.Row.Family != "Creatures" || section.ID == "Parameters") decorationForms.Remove(view.Row.Family);
            }
            InvalidateDefinitionEditing(); RefreshDefinitions(); DefinitionMessage("Applied");
            if (operation != "Apply")
            {
                updatingDefinitions = true;
                try { LoadDefinitionField(); }
                finally { updatingDefinitions = false; }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        { DefinitionMessage("Failed", exception.Message); }
    }
    private void DefinitionProperties_Click(object sender, RoutedEventArgs e)
    {
        if ((DefinitionPicker.SelectedItem as DefinitionView)?.Row.Record is not { } record) return;
        RecordSearch.Text = ""; RecordOperation(() => record); WorkspaceTabs.SelectedIndex = 1;
    }
}
