using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private bool updatingBand;
    private int selectedBandSlot = -1;
    private sealed record BandSlotView(int Index, string Label, System.Windows.Media.Imaging.BitmapSource? Icon = null);

    private void RefreshBandPanel(NativeRecord record)
    {
        if (document is null || !NativeMapDocument.IsBand(record) || record.Category == "GAMEBAND" && record.Length != 563)
        { BandPanel.Visibility = Visibility.Collapsed; selectedBandSlot = -1; return; }
        var oldSlot = selectedBandSlot;
        var creature = (BandCreaturePicker.SelectedItem as NamedChoiceView)?.Choice.Value;
        var level = BandLevelPicker.SelectedValue as int? ?? 1;
        var enemy = BandEnemyPicker.SelectedValue as int? ?? 3;
        var names = ValueCatalogues.ForTemplate("Creature");
        string Name(uint id) => names.FirstOrDefault(choice => choice.Value == id.ToString())?.Name(LocalizationService.CurrentLanguage) ?? $"ID {id}";
        updatingBand = true;
        BandPanel.Visibility = Visibility.Visible;
        var members = document.BandMembers(record);
        BandSlotView[] views = [new(-1, LocalizationService.Get("Editor.BandProperties") + $" · {members.Count(member => member.Active)}/16"), .. members.Select(member => new BandSlotView(member.Index,
            $"{member.Index + 1} · " + (member.Active ? Name(member.CreatureID) + $" · {LocalizationService.Get("Field.Level")} {member.Level}" : LocalizationService.Get("Editor.EmptyBandSlot") + (member.CreatureID == 0 ? "" : $" · {Name(member.CreatureID)}")),
                member.CreatureID == 0 ? null : OriginalObjectImages.For(PlacedThingKind.Creature, member.CreatureID)))];
        BandSlotPicker.ItemsSource = views;
        BandSlotPicker.SelectedItem = views.FirstOrDefault(view => view.Index == oldSlot) ?? views[0];
        selectedBandSlot = (BandSlotPicker.SelectedItem as BandSlotView)?.Index ?? -1;
        var choices = ValueCatalogues.BandCreatures.Select(choice => new NamedChoiceView(choice, ChoiceLabel(choice), OriginalObjectImages.For(PlacedThingKind.Creature, uint.Parse(choice.Value)))).ToArray();
        BandCreaturePicker.ItemsSource = choices;
        BandCreaturePicker.SelectedItem = choices.FirstOrDefault(choice => choice.Choice.Value == creature) ?? choices.First(choice => choice.Choice.Value == ValueCatalogues.Elements("Heros").First().Value);
        BandLevelPicker.ItemsSource = Enumerable.Range(1, 10).Select(value => new Choice(value, LocalizationService.Get("Field.Level") + " " + value)).ToArray();
        BandLevelPicker.SelectedValue = level;
        BandEnemyPicker.ItemsSource = ValueCatalogues.Owners.Select(choice => new Choice(int.Parse(choice.Value), choice.Name(LocalizationService.CurrentLanguage))).ToArray();
        BandEnemyPicker.SelectedValue = enemy;
        updatingBand = false;
        UpdateBandMemberCommands(members);
    }

    private void UpdateBandMemberCommands(IReadOnlyList<BandMember> members)
    {
        var count = members.TakeWhile(member => member.Active).Count();
        var consecutive = !members.Skip(count).Any(member => member.Active);
        BandAddMemberButton.IsEnabled = consecutive && count < 16;
        BandMoveUpButton.IsEnabled = consecutive && selectedBandSlot > 0 && selectedBandSlot < count;
        BandMoveDownButton.IsEnabled = consecutive && selectedBandSlot >= 0 && selectedBandSlot + 1 < count;
    }

    private void BandSlot_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingBand || document is null || RecordList.SelectedItem is not RecordView view || !NativeMapDocument.IsBand(view.Record)) return;
        if (!CommitFields())
        {
            updatingBand = true;
            BandSlotPicker.SelectedItem = ((BandSlotView[])BandSlotPicker.ItemsSource).First(item => item.Index == selectedBandSlot);
            updatingBand = false; return;
        }
        selectedBandSlot = (BandSlotPicker.SelectedItem as BandSlotView)?.Index ?? -1;
        PopulateRecordFields(view.Record);
        UpdateBandMemberCommands(document.BandMembers(view.Record));
    }

    private void SelectBandSlot(int index)
    {
        if (BandSlotPicker.ItemsSource is BandSlotView[] views) BandSlotPicker.SelectedItem = views.FirstOrDefault(view => view.Index == index) ?? views[0];
    }

    private void AddBandMember_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || RecordList.SelectedItem is not RecordView view || BandCreaturePicker.SelectedItem is not NamedChoiceView creature || !CommitFields()) return;
        var level = BandLevelPicker.SelectedValue as int? ?? 1; var enemy = BandEnemyPicker.SelectedValue as int? ?? 3;
        var added = -1;
        RecordOperation(() => { added = document.AddEditorBandMember(view.Record, uint.Parse(creature.Choice.Value), level, (uint)enemy); return null; });
        if (added >= 0) SelectBandSlot(added);
    }

    private void RemoveBandMember_Click(object sender, RoutedEventArgs e)
    {
        if (document is null || RecordList.SelectedItem is not RecordView view || selectedBandSlot < 0 || !CommitFields()) return;
        var index = selectedBandSlot;
        if (DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this), LocalizationService.Get("Editor.RemoveBandMemberConfirm"), LocalizationService.Get("Editor.BandMembers"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        RecordOperation(() => { document.RemoveBandMember(view.Record, index); return null; });
    }

    private void BandMemberUp_Click(object sender, RoutedEventArgs e) => MoveSelectedBandMember(-1);
    private void BandMemberDown_Click(object sender, RoutedEventArgs e) => MoveSelectedBandMember(1);
    private void MoveSelectedBandMember(int shift)
    {
        if (document is null || RecordList.SelectedItem is not RecordView view || selectedBandSlot < 0 || !CommitFields()) return;
        var index = selectedBandSlot; var destination = index + shift; var moved = false;
        RecordOperation(() => { document.MoveBandMember(view.Record, index, destination); moved = true; return null; });
        if (moved) SelectBandSlot(destination);
    }
}
