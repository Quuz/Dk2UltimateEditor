using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private PlayerAIConfiguration? playerAIState;
    private readonly Dictionary<string,string> playerAIDraft = [];
    private readonly Dictionary<string,FrameworkElement> playerAIInputs = [];
    private List<long> playerAIRoomDraft = [];
    private bool updatingPlayerAI;
    private string? playerAIUiLanguage;
    private sealed record AIRoomChoiceView(int Value, string Label, System.Windows.Media.Imaging.BitmapSource? Icon);
    private static string AIText(string name) => LocalizationService.Get("Editor.PlayerAI." + name);
    private void RefreshCustomPlayerAI(PlayerSettingsRow? row)
    {
        if (PlayerCustomAI is null) return;
        PlayerCustomAI.IsEnabled = row?.ProfileEditable == true;
        if (row?.ProfileEditable != true || document is null)
        {
            playerAIState = null; playerAIDraft.Clear(); playerAIInputs.Clear(); playerAIRoomDraft.Clear(); PlayerAIParameters.Children.Clear(); PlayerAIRoomOrder.ItemsSource = null;
            PlayerAIApply.IsEnabled = PlayerAIRevert.IsEnabled = PlayerAIRoomUp.IsEnabled = PlayerAIRoomDown.IsEnabled = false; PlayerAIHint.Text = AIText("ReadOnly"); return;
        }
        var rebuild = playerAIState is null || playerAIState.ID != row.ID || playerAIState.Record.File != row.Record.File;
        if (rebuild)
        {
            playerAIState = document.GetPlayerAIConfiguration(row.ID); playerAIDraft.Clear();
            foreach (var (name,value) in playerAIState.Values) playerAIDraft.Add(name,value.ToString(CultureInfo.InvariantCulture));
            playerAIRoomDraft = playerAIState.RoomOrder.ToList();
        }
        if (rebuild || playerAIUiLanguage != LocalizationService.CurrentLanguage) BuildCustomAIControls();
        UpdateCustomAIButtons();
    }
    private bool CurrentPlayerAI()
        => document is not null && playerAIState?.IsCurrent == true;
    private void BuildCustomAIControls()
    {
        var expanded = PlayerAIParameters.Children.OfType<Expander>().Where(group => group.IsExpanded).Select(group => group.Tag as string).ToHashSet();
        var roomIndex = PlayerAIRoomOrder.SelectedIndex; updatingPlayerAI = true;
        try
        {
            PlayerAIParameters.Children.Clear(); playerAIInputs.Clear(); playerAIUiLanguage = LocalizationService.CurrentLanguage;
            foreach (var group in PlayerAIOptions.Groups)
            {
                var panel = new StackPanel { Margin = new(8) }; var expander = new Expander { Header = group.Name(LocalizationService.CurrentLanguage),Tag = group.Value,Content = panel,IsExpanded = expanded.Contains(group.Value),Margin = new(0,4,0,0) }; PlayerAIParameters.Children.Add(expander);
                expander.SetResourceReference(Control.ForegroundProperty,"Brush.Text");
                foreach (var parameter in PlayerAIOptions.Parameters.Where(parameter => parameter.Group == group.Value))
                {
                    var grid = new Grid { Margin = new(0,3,0,3) }; grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1,GridUnitType.Star) }); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) }); panel.Children.Add(grid);
                    grid.Children.Add(new TextBlock { Text = parameter.Label(LocalizationService.CurrentLanguage),TextWrapping = TextWrapping.Wrap,VerticalAlignment = VerticalAlignment.Center,Margin = new(0,0,8,0) });
                    var value = playerAIDraft[parameter.Name]; FrameworkElement input;
                    if (parameter.Choices.Count > 0)
                    {
                        var choices = parameter.Choices.Select(choice => new NamedChoiceView(choice,choice.Name(LocalizationService.CurrentLanguage))).ToList();
                        if (!choices.Any(choice => choice.Choice.Value == value)) choices.Add(new(new(value,"#" + value,"#" + value),"#" + value + " · " + AIText("Unknown")));
                        var combo = new ComboBox { ItemsSource = choices,DisplayMemberPath = "Label",SelectedItem = choices.Single(choice => choice.Choice.Value == value),Tag = parameter.Name };
                        combo.SelectionChanged += (_,_) => { if (!updatingPlayerAI && combo.SelectedItem is NamedChoiceView selected) { playerAIDraft[parameter.Name] = selected.Choice.Value; UpdateCustomAIButtons(); } }; input = combo;
                    }
                    else
                    {
                        var box = new TextBox { Text = value,Tag = parameter.Name,ToolTip = $"{parameter.Minimum}–{parameter.Maximum}" };
                        box.TextChanged += (_,_) => { if (!updatingPlayerAI) { playerAIDraft[parameter.Name] = box.Text; UpdateCustomAIButtons(); } }; input = box;
                    }
                    input.ToolTip ??= parameter.Name; playerAIInputs.Add(parameter.Name,input); Grid.SetColumn(input,1); grid.Children.Add(input);
                }
            }
            RefreshAIRoomOrder(roomIndex);
        }
        finally { updatingPlayerAI = false; }
    }
    private void RefreshAIRoomOrder(int index)
    {
        PlayerAIRoomOrder.ItemsSource = playerAIRoomDraft.Select((id,position) => new AIRoomChoiceView(position,$"{position + 1} · " + (PlayerAIOptions.Rooms.FirstOrDefault(room => room.Value == id.ToString(CultureInfo.InvariantCulture))?.Name(LocalizationService.CurrentLanguage) ?? "#" + id),
            PlayerAIOptions.RoomImageResource(id) is { } resource ? OriginalObjectImages.Get(resource) : null)).ToArray();
        PlayerAIRoomOrder.SelectedIndex = Math.Clamp(index,0,Math.Max(0,playerAIRoomDraft.Count - 1));
    }
    private void UpdateCustomAIButtons()
    {
        if (PlayerAIApply is null || updatingPlayerAI) return;
        var current = CurrentPlayerAI(); var validOrder = PlayerAIOptions.ValidRoomOrder(playerAIRoomDraft); var index = PlayerAIRoomOrder.SelectedIndex;
        PlayerAIApply.IsEnabled = current; PlayerAIRevert.IsEnabled = playerAIState is not null;
        PlayerAIRoomUp.IsEnabled = current && validOrder && index > 0; PlayerAIRoomDown.IsEnabled = current && validOrder && index >= 0 && index < playerAIRoomDraft.Count - 1;
        PlayerAIHint.Text = AIText(!current ? "Stale" : !validOrder ? "InvalidOrder" : "Hint");
    }
    private void PlayerAIRoomSelection_Changed(object sender,SelectionChangedEventArgs e) => UpdateCustomAIButtons();
    private void PlayerAIRoomMove_Click(object sender,RoutedEventArgs e)
    {
        if (!CurrentPlayerAI() || !PlayerAIOptions.ValidRoomOrder(playerAIRoomDraft)) return;
        var index = PlayerAIRoomOrder.SelectedIndex; var target = index + int.Parse((string)((Button)sender).Tag,CultureInfo.InvariantCulture);
        if (index < 0 || target < 0 || target >= playerAIRoomDraft.Count) return;
        (playerAIRoomDraft[index],playerAIRoomDraft[target]) = (playerAIRoomDraft[target],playerAIRoomDraft[index]);
        updatingPlayerAI = true; try { RefreshAIRoomOrder(target); } finally { updatingPlayerAI = false; } UpdateCustomAIButtons();
    }
    private void PlayerAIApply_Click(object sender,RoutedEventArgs e)
    {
        if (document is null || playerAIState is null || !CommitFields()) return;
        try
        {
            document.SetPlayerAIConfiguration(playerAIState,playerAIDraft.ToDictionary(pair => pair.Key,pair => long.Parse(pair.Value,CultureInfo.InvariantCulture)),playerAIRoomDraft);
            ReloadCustomPlayerAI(); PlayerMessage("Editor.PlayerApplied");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException or FormatException) { UpdateCustomAIButtons(); PlayerMessage("Editor.PlayerFailed",exception.Message); }
    }
    private void ReloadCustomPlayerAI()
    { playerAIState = null; playerAIUiLanguage = null; RefreshPlayers(); }
    private void PlayerAIRevert_Click(object sender,RoutedEventArgs e) => ReloadCustomPlayerAI();
}
