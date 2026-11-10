using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static partial class Program
{
    private static void CheckPlayers(NativeMapDocument document, string language, string directory)
    {
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace, "WorkspaceTabs"); tabs.SelectedIndex = 7;
        workspace.PlayerGoldPresenter=window=>window.Acknowledge();
        void Layout() { workspace.Measure(new Size(1600, 1050)); workspace.Arrange(new Rect(0, 0, 1600, 1050)); workspace.UpdateLayout(); }
        Layout(); var picker = Control<ComboBox>(workspace, "PlayerPicker"); var profile = Control<ComboBox>(workspace, "PlayerProfile");
        var panel = Control<StackPanel>(workspace, "PlayerPanel"); var aiControls = Control<StackPanel>(workspace, "PlayerAIControls");
        var general = Control<StackPanel>(workspace, "PlayerGeneral");
        var name = Control<TextBox>(workspace, "PlayerName"); var gold = Control<TextBox>(workspace, "PlayerGold"); var health = Control<TextBox>(workspace, "PlayerHealth");
        var computer = Control<CheckBox>(workspace, "PlayerComputer"); var confirmed = Control<CheckBox>(workspace, "PlayerConfirmed");
        var status = Control<TextBlock>(workspace, "PlayerStatus"); var stored = Control<TextBlock>(workspace, "PlayerStored");
        void Select(uint id) { picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<PlayerSettingsRow>(item, "Player").ID == id); Layout(); }
        PlayerSettingsRow Row(uint id) => document.PlayerSettingsRows().Single(row => row.ID == id);
        void Profile(int value) => profile.SelectedItem = profile.Items.Cast<object>().Single(item => Get<ValueChoice>(item, "Choice").Value == value.ToString());
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Player UI modified unexpected data."); }
        Expect(tabs.Items.Count == 10 && picker.Items.Count == 7 && profile.Items.Count == 9 && panel.IsEnabled && aiControls.IsEnabled, "Player/profile controls not bound.");
        Expect(picker.Foreground is SolidColorBrush { Color: var pickerColor } && pickerColor == Color.FromRgb(0x20, 0x1B, 0x16)
            && profile.Foreground is SolidColorBrush { Color: var profileColor } && profileColor == pickerColor, "Light player/profile pickers need a dark readable foreground.");
        Expect(stored.Foreground is SolidColorBrush { Color: var storedColor } && storedColor == Color.FromRgb(0xEE, 0xE7, 0xDA)
            && computer.Foreground is SolidColorBrush { Color: var checkColor } && checkColor == storedColor, "Dark tab labels and checkbox text need a light readable foreground.");
        Select(3); var before = Snapshot(); Expect(name.Text == "Keeper 1" && gold.Text == "3000" && health.Text == "100" && stored.Text.Contains(LocalizationService.Get("Editor.PlayerStored")), "General player draft/label not bound."); Exact(before);
        name.Text = "José"; gold.Text = "999999"; health.Text = "0"; computer.IsChecked = true; Click(general, "Editor.PlayerApply");
        Expect(Row(3) is { Name: "José", Gold: 999999, Health: 0, Computer: true } && status.Text == LocalizationService.Get("Editor.PlayerApplied"), "Actual general-settings button wrote wrong player/heart.");
        var after = Snapshot(); workspace.Undo(); Exact(before); workspace.Redo(); Exact(after);
        foreach (var (input, value) in new[] { (gold, "1000000"), (health, "101"), (name, "Имя"), (gold, "invalid") })
        {
            var old = input.Text; input.Text = value; Click(general, "Editor.PlayerApply"); Exact(after);
            Expect(status.Text == LocalizationService.Get("Editor.PlayerFailed"), "Invalid player input did not show inline validation."); input.Text = old;
        }
        var row = Row(3); var speed = row.Record.Fields.Single(field => field.Name == (document.IsLegacyProject ? "PlayerSpeed" : "bPlayerMiscSpeed"));
        document.SetField(row.Record, speed, "99"); before = Snapshot(); Profile(7); Click(aiControls, "Editor.PlayerTypeOnly");
        Expect(Row(3).AI == 7 && Row(3).Record.Read(speed) == "99", "Changing AI type replaced custom parameters."); after = Snapshot();
        Click(aiControls, "Editor.PlayerProfileApply"); Exact(after); confirmed.IsChecked = true; Click(aiControls, "Editor.PlayerProfileApply");
        Expect(Row(3).AI == 7 && Row(3).Record.Read(speed) == "1" && Row(3) is { Name: "José", Gold: 999999, Health: 0, Computer: true } && confirmed.IsChecked != true, "Confirmed profile reset changed general settings or missed AI values.");
        for (var index = 0; index < 9; index++)
        { Profile(index); confirmed.IsChecked = true; Click(aiControls, "Editor.PlayerProfileApply"); Expect(Row(3).AI == index, "A profile choice/button failed."); }
        after = Snapshot(); name.Text = "Draft"; gold.Text = "123"; Profile(8);
        var oldLabel = Get<string>(profile.SelectedItem, "Label"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Get<PlayerSettingsRow>(picker.SelectedItem, "Player").ID == 3 && name.Text == "Draft" && gold.Text == "123" && Get<ValueChoice>(profile.SelectedItem, "Choice").Value == "8" && Get<string>(profile.SelectedItem, "Label") != oldLabel, "Live language switching lost player/profile drafts."); Exact(after);
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout(); Click(panel, "Editor.PlayerReset"); Exact(after);
        confirmed.IsChecked = true; name.Text = "Changed draft"; Expect(confirmed.IsChecked != true, "Changed player input retained full-reset authority.");
        confirmed.IsChecked = true; Profile(7); Expect(confirmed.IsChecked != true, "Changed profile retained reset authority.");
        confirmed.IsChecked = true; document.Paint([new(2, 8)], new(4, 2, 1)); Expect(confirmed.IsChecked != true, "External edit retained player reset authority."); document.Undo(); Exact(after);
        confirmed.IsChecked = true; Click(panel, "Editor.PlayerReset");
        Expect(Row(3) is { Name: "Keeper 1", Gold: 3000, Health: 100, AI: 0, Computer: false } && name.Text == "Keeper 1" && Get<ValueChoice>(profile.SelectedItem, "Choice").Value == "0" && confirmed.IsChecked != true, "Full player reset/button did not restore source defaults and profile choice.");
        if (Environment.GetEnvironmentVariable("DK2_PLAYER_UI_ARTIFACTS") is { Length: > 0 } artifactDirectory)
        {
            Layout(); Directory.CreateDirectory(artifactDirectory);
            var bitmap = new RenderTargetBitmap(1600, 1050, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(artifactDirectory, $"players-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
        }
        var reset = Snapshot(); workspace.Undo(); Exact(after); workspace.Redo(); Exact(reset);
        Click(panel, "Editor.PlayerProperties"); Expect(tabs.SelectedIndex == 1 && Control<ListBox>(workspace, "RecordList").SelectedItem is not null, "Player properties navigation failed."); tabs.SelectedIndex = 7;
        Select(1); Expect(name.Text == "Good Player" && (document.IsLegacyProject || !health.IsEnabled && health.Text.Length == 0), "NPC name/missing native heart health was fabricated.");
        confirmed.IsChecked = true; Select(2); Expect(confirmed.IsChecked != true && name.Text == "Neutral Player", "Player selection retained reset authority/old values.");
        if (document.IsLegacyProject)
        {
            var saved = document.SaveCopy(Path.Combine(directory, "player-ui-history-" + language)); var sections = LegacySectionFile.Read(saved);
            foreach (var version in new[] { 1, 2 })
            {
                var original = sections.Sections.Single(section => section.Signature == NativeMapDocument.LegacyPlayers);
                var payload = version == 1 ? new byte[7 * LegacyProjectSchemas.Size("MAPOPTIONSV1")] : original.Payload.ToArray();
                var path = Path.Combine(directory, $"player-ui-v{version}-{language}.dkm");
                File.WriteAllBytes(path, LegacySectionFile.Create(sections.Sections.Select(section => section.Signature == original.Signature ? new LegacySection(original.Signature, version, payload) : section)).ToArray());
                workspace.Document = NativeMapDocument.Load(path); Layout();
                Expect(!Control<Button>(workspace, "PlayerReset").IsEnabled && aiControls.IsEnabled == (version == 2), "Historical AI/reset guards not reflected in UI.");
            }
        }
        workspace.Document = null; Layout();
        Expect(!panel.IsEnabled && picker.Items.Count == 0 && name.Text.Length == 0 && gold.Text == "0" && health.Text.Length == 0 && status.Text.Length == 0 && confirmed.IsChecked != true, "Closed document retained player drafts/authority.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} player settings, all nine AI profiles, confirmed source reset and translation");
        CheckPlayerGoldNotice(!document.IsLegacyProject,language);
    }
}
