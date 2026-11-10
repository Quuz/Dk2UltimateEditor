using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed record PlayerView(PlayerSettingsRow Player, string Label);
    private bool updatingPlayers;
    private uint? playerDraftID;
    private string? playerStatusKey;
    private void InvalidatePlayerEditing()
    { if (PlayerConfirmed is null) return; InvalidatePlayerGoldNotice();PlayerConfirmed.IsChecked = false; playerStatusKey = null; PlayerStatus.Text = ""; PlayerStatus.ToolTip = null; }
    private void ResetPlayerEditing()
    { CancelPlayerGeometryPick(); playerDraftID = null; playerGeometryDraftID = null; playerAIState = null; InvalidatePlayerEditing(); RefreshPlayers(); }
    private void RefreshPlayers()
    {
        if (PlayerPicker is null) return;
        var selectedID = (PlayerPicker.SelectedItem as PlayerView)?.Player.ID;
        var profile = (PlayerProfile.SelectedItem as NamedChoiceView)?.Choice.Value;
        updatingPlayers = true;
        try
        {
            var choices = (document?.PlayerSettingsRows() ?? []).Select(row => new PlayerView(row,
                (row.ID is >= 1 and <= 7 ? ValueCatalogues.Owners[(int)row.ID - 1].Name(LocalizationService.CurrentLanguage) : "#" + row.ID)
                + " · " + row.Name + (row.Editable ? "" : " · " + LocalizationService.Get("Editor.RuleReadOnly")))).ToArray();
            PlayerPicker.ItemsSource = choices; PlayerPicker.SelectedItem = choices.FirstOrDefault(choice => choice.Player.ID == selectedID);
            if (PlayerPicker.SelectedIndex < 0 && choices.Length > 0) PlayerPicker.SelectedIndex = 0;
            var row = (PlayerPicker.SelectedItem as PlayerView)?.Player;
            var reloadDraft = row is null || playerDraftID != row.ID;
            var profileValue = reloadDraft ? row?.AI.ToString(CultureInfo.InvariantCulture) : profile;
            var profiles = PlayerSettings.AIProfiles.Select(choice => new NamedChoiceView(choice, ChoiceLabel(choice))).ToArray();
            PlayerProfile.ItemsSource = profiles;
            PlayerProfile.SelectedItem = profiles.FirstOrDefault(choice => choice.Choice.Value == profileValue) ?? profiles[0];
            PlayerPanel.IsEnabled = row is not null;
            PlayerGeneral.IsEnabled = row?.Editable == true; PlayerAIControls.IsEnabled = row?.ProfileEditable == true;
            PlayerReset.IsEnabled = row?.Resettable == true;
            PlayerHealth.IsEnabled = row?.Health is not null;
            if (reloadDraft) LoadPlayerDraft(row);
            RefreshCustomPlayerAI(row);
            RefreshPlayerGeometry(row);
            RefreshPlayerResources();
            PlayerStored.Text = row is null ? "" : $"{LocalizationService.Get("Editor.PlayerStored")}: {row.Name} · {LocalizationService.Get("Editor.PlayerGold")}: {row.Gold} · AI #{row.AI} · {LocalizationService.Get("Editor.PlayerHealth")}: {row.Health?.ToString(CultureInfo.InvariantCulture) ?? "—"}";
            if (playerStatusKey is { } key) PlayerStatus.SetResourceReference(TextBlock.TextProperty, key);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException)
        { PlayerPicker.ItemsSource = null; PlayerPanel.IsEnabled = false; PlayerMessage("Editor.PlayerFailed", exception.Message); }
        finally { updatingPlayers = false; }
    }
    private void LoadPlayerDraft(PlayerSettingsRow? row)
    {
        playerDraftID = row?.ID;
        PlayerName.Text = row?.Name ?? ""; PlayerGold.Text = row?.Gold.ToString(CultureInfo.InvariantCulture) ?? "0";
        PlayerComputer.IsChecked = row?.Computer == true; PlayerHealth.Text = row?.Health?.ToString(CultureInfo.InvariantCulture) ?? "";
    }
    private void PlayerSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (updatingPlayers || PlayerStatus is null) return;
        CancelPlayerGeometryPick(); InvalidatePlayerEditing(); playerDraftID = null; playerGeometryDraftID = null; playerAIState = null; RefreshPlayers();
    }
    private void PlayerDraft_Changed(object sender, RoutedEventArgs e)
    { if (!updatingPlayers&&PlayerStatus is not null) { CancelPlayerGeometryPick(); InvalidatePlayerEditing(); RefreshPlayerResources(); } }
    private void PlayerMessage(string key, string? detail = null) { playerStatusKey = key; PlayerStatus.ToolTip = detail; PlayerStatus.SetResourceReference(TextBlock.TextProperty, key); }
    private void PlayerEdit_Click(object sender, RoutedEventArgs e)
    {
        if(playerGoldPending)return;
        if (document is null || PlayerPicker.SelectedItem is not PlayerView selected) return;
        var operation = (sender as Button)?.Tag as string;
        if (operation is "Profile" or "Reset" && PlayerConfirmed.IsChecked != true) return;
        var id = selected.Player.ID;
        CancelPlayerGeometryPick();
        if (!CommitFields()) return;
        if (operation is "Profile" or "Reset" && PlayerConfirmed.IsChecked != true) return;
        try
        {
            string? committedWarning=null;
            if (operation == "General") {if(!ApplyPlayerGeneralWithNotice(id,out committedWarning))return;}
            else if (operation == "Reset") document.ResetPlayerSettings([id]);
            else
            {
                var profile = int.Parse(((NamedChoiceView)PlayerProfile.SelectedItem).Choice.Value, CultureInfo.InvariantCulture);
                if (operation == "Profile") document.ApplyPlayerAIProfile([id], profile);
                else document.SetPlayerAIType([id], profile);
            }
            PlayerConfirmed.IsChecked = false; playerDraftID = null; playerGeometryDraftID = null; RefreshPlayers(); PlayerMessage(committedWarning is null?"Editor.PlayerApplied":"PlayerGold.CommittedFailure",committedWarning);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException or FormatException)
        { PlayerMessage("Editor.PlayerFailed", exception.Message); }
    }
    private void PlayerProperties_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas.GeometryOnly) return;
        if (PlayerPicker.SelectedItem is not PlayerView selected) return;
        RecordSearch.Text = ""; RecordOperation(() => selected.Player.Record); WorkspaceTabs.SelectedIndex = 1;
    }
}
