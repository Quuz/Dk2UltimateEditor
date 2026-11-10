using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckPlayerAI(NativeMapDocument document,string language)
    {
        var workspace = new EditorWorkspace { Document = document }; var tabs = Control<TabControl>(workspace,"WorkspaceTabs"); tabs.SelectedIndex = 7;
        var custom = Control<Expander>(workspace,"PlayerCustomAI"); custom.IsExpanded = true; var groups = Control<StackPanel>(workspace,"PlayerAIParameters");
        var picker = Control<ComboBox>(workspace,"PlayerPicker"); var rooms = Control<ListBox>(workspace,"PlayerAIRoomOrder");
        var apply = Control<Button>(workspace,"PlayerAIApply"); var revert = Control<Button>(workspace,"PlayerAIRevert"); var hint = Control<TextBlock>(workspace,"PlayerAIHint");
        void Layout() { workspace.Measure(new Size(1600,1050)); workspace.Arrange(new Rect(0,0,1600,1050)); workspace.UpdateLayout(); }
        void Press(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        void Select(uint id) { picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<PlayerSettingsRow>(item,"Player").ID == id); Layout(); }
        Dictionary<string,FrameworkElement> Inputs() => (Dictionary<string,FrameworkElement>)typeof(EditorWorkspace).GetField("playerAIInputs",BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(workspace)!;
        void Set(string name,string value)
        {
            var input = Inputs()[name]; if (input is TextBox text) text.Text = value;
            else { var combo = (ComboBox)input; combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<ValueChoice>(item,"Choice").Value == value); }
        }
        string Value(string name) => Inputs()[name] is TextBox box ? box.Text : Get<ValueChoice>(((ComboBox)Inputs()[name]).SelectedItem,"Choice").Value;
        Dictionary<NativeFile,byte[]> Snapshot() => document.Files.ToDictionary(file => file,file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> bytes) { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]),"Custom AI UI touched unrelated bytes."); }
        Select(3); Layout(); var before = Snapshot(); var initial = document.GetPlayerAIConfiguration(3);
        void RoomImages(IReadOnlyList<long> order)
        {
            Expect(rooms.ItemTemplate is not null && string.IsNullOrEmpty(rooms.DisplayMemberPath), "Room priority list remains text-only.");
            Expect(rooms.Items.Count == order.Count, "Room image template changed priority row count.");
            for (var i = 0; i < order.Count; i++)
            {
                var item = rooms.Items[i]; var image = (BitmapSource?)item.GetType().GetProperty("Icon")!.GetValue(item);
                Expect(Get<int>(item,"Value") == i, "Room image row changed physical priority position.");
                Expect(PlayerAIOptions.RoomImageResource(order[i]) is { } resource
                    ? ReferenceEquals(image,OriginalObjectImages.Get(resource)) : image is null, "Room image follows position or invents an unknown fallback.");
            }
        }
        RoomImages(initial.RoomOrder);
        Expect(Inputs().Count == 67 && groups.Children.Count == 7 && rooms.Items.Count == 15 && custom.IsEnabled && apply.IsEnabled,"Complete manual AI controls/order missing.");
        Expect(custom.Foreground is SolidColorBrush { Color: var foreground } && foreground == Color.FromRgb(0xEE,0xE7,0xDA)
            && groups.Children.OfType<Expander>().All(group => group.Foreground is SolidColorBrush { Color: var colour } && colour == foreground),"Custom AI group/header labels must be readable on dark backgrounds.");
        foreach (var parameter in PlayerAIOptions.Parameters)
        {
            var input = Inputs()[parameter.Name]; var grid = (Grid)input.Parent;
            Expect(grid.Children.OfType<TextBlock>().Single().Text == parameter.Label(language),"AI control label not translated.");
            Expect(grid.Children.OfType<TextBlock>().Single().Foreground is SolidColorBrush { Color: var labelColour } && labelColour == foreground,"AI parameter label has unreadable foreground.");
            if (input is ComboBox combo) Expect(combo.Items.Count == parameter.Choices.Count,"Source AI named choices incomplete.");
        }
        Set("PlayerSpeed","4"); Set("PlayerMineGoldAmount","65535"); Set("PlayerExpandMode","2"); Set("PlayerCreaturesSlap","0");
        rooms.SelectedIndex = 1; Press(Control<Button>(workspace,"PlayerAIRoomUp")); Expect(rooms.SelectedIndex == 0,"Actual room-up button failed.");
        var roomDraft = initial.RoomOrder.ToArray(); (roomDraft[0],roomDraft[1]) = (roomDraft[1],roomDraft[0]); Exact(before);
        RoomImages(roomDraft);
        Set("PlayerMana","invalid"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
        Expect(Value("PlayerMana") == "invalid" && Value("PlayerMineGoldAmount") == "65535" && Value("PlayerExpandMode") == "2" && rooms.SelectedIndex == 0,"Live AI translation discarded invalid/numeric/choice/order drafts."); Exact(before);
        RoomImages(roomDraft);
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Layout(); Press(apply); Exact(before);
        Expect(Control<TextBlock>(workspace,"PlayerStatus").Text == LocalizationService.Get("Editor.PlayerFailed"),"Invalid whole AI draft not rejected inline.");
        Set("PlayerMana",initial.Values["PlayerMana"].ToString(CultureInfo.InvariantCulture)); Press(apply);
        var applied = document.GetPlayerAIConfiguration(3); Expect(applied.Values["PlayerSpeed"] == 4 && applied.Values["PlayerMineGoldAmount"] == 65535 && applied.Values["PlayerExpandMode"] == 2 && applied.Values["PlayerCreaturesSlap"] == 0 && applied.RoomOrder.SequenceEqual(roomDraft),"Actual whole-AI apply did not update selected player/order.");
        var after = Snapshot(); Press(apply); workspace.Undo(); Exact(before); Expect(!document.CanUndo,"UI unchanged AI apply created undo history."); workspace.Redo(); Exact(after); Press(revert);
        Set("PlayerSpeed","1"); var gold = applied.Record.Fields.Single(field => field.Name == (document.IsLegacyProject ? "PlayerGold" : "bPlayerGold")); document.SetField(applied.Record,gold,"1234"); var external = Snapshot();
        Expect(!apply.IsEnabled && hint.Text == LocalizationService.Get("Editor.PlayerAI.Stale") && Value("PlayerSpeed") == "1","External player edit discarded AI draft or retained stale authority."); Press(apply); Exact(external); Press(revert); Expect(apply.IsEnabled,"Discard did not reload current AI authority.");
        document.Undo(); Press(revert); Exact(after);
        Set("PlayerSpeed","2"); Select(7); Select(3); Expect(Value("PlayerSpeed") == "4","Player change leaked custom AI drafts.");
        var state = document.GetPlayerAIConfiguration(3);
        NativeField Field(string name) => state.Record.Fields.Single(field => field.Name == (document.IsLegacyProject ? name : LegacyCompilerRules.Players.Single(mapping => mapping.Source == name).Target));
        document.SetField(state.Record,Field("PlayerExpandMode"),"255"); document.SetField(state.Record,Field("PlayerCreaturesSlap"),"161"); document.SetField(state.Record,Field("PlayerRoomToBuildPri[0]"),"255"); Press(revert);
        var unknown = Snapshot(); Expect(Value("PlayerExpandMode") == "255" && Value("PlayerCreaturesSlap") == "1" && !Control<Button>(workspace,"PlayerAIRoomUp").IsEnabled && !Control<Button>(workspace,"PlayerAIRoomDown").IsEnabled,"Unknown AI values/order hidden or normalized.");
        RoomImages(document.GetPlayerAIConfiguration(3).RoomOrder);
        Press(apply); Exact(unknown); Set("PlayerCreaturesSlap","0"); Press(apply); Expect(state.Record.Read(Field("PlayerCreaturesSlap")) == "129","UI no-slap toggle destroyed unknown flag bits."); workspace.Undo(); Exact(unknown); Press(revert);
        if (Environment.GetEnvironmentVariable("DK2_PLAYER_AI_UI_ARTIFACTS") is { Length: > 0 } artifactDirectory)
        {
            Directory.CreateDirectory(artifactDirectory);
            foreach (var group in groups.Children.OfType<Expander>()) group.IsExpanded = Equals(group.Tag,"Rooms");
            custom.BringIntoView(); Layout();
            var bitmap = new RenderTargetBitmap(1600,1050,96,96,PixelFormats.Pbgra32); bitmap.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(artifactDirectory,$"custom-ai-{language}-{(document.IsLegacyProject ? "project" : "native")}.png")); encoder.Save(output);
        }
        workspace.Document = null; Layout(); Expect(Inputs().Count == 0 && groups.Children.Count == 0 && rooms.Items.Count == 0 && !apply.IsEnabled && !custom.IsEnabled,"Closed document retained custom AI controls/state.");
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject ? "project" : "native")} 67 custom AI controls, seven groups, 15-room priority, drafts, stale/unknown guards, translation and undo");
    }
}
