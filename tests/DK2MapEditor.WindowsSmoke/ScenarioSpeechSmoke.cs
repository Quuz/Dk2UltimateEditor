using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioSpeech(bool native, string language, string directory)
    {
        var resources = Path.Combine(directory, $"say-resources-{language}-{native}");
        var soundFolder = Path.Combine(resources, "Data", "Sound", "sfx", "SPEECH_LEVEL1"); Directory.CreateDirectory(soundFolder);
        var textFolder = Path.Combine(resources, "Data", "Text", "Default"); Directory.CreateDirectory(textFolder);
        var bankPath = Path.Combine(soundFolder, "SPEECH_LEVEL1HD.SDT"); var textPath = Path.Combine(textFolder, "level1.STR");
        // Literal fixture layout follows original DWORD index/40-byte sound
        // headers, not a writer from the implementation under test.
        var bankBytes = new byte[16 + 3 * 44]; BinaryPrimitives.WriteUInt32LittleEndian(bankBytes, 3);
        var payloads = new[] { new byte[] { 0xFF, 0x01, 0xFD, 0x31 }, new byte[] { 0xFF, 0x02, 0xFD, 0x32 }, new byte[] { 0xFF, 0x03, 0xFD, 0x33 } };
        for (var index = 0; index < 3; index++)
        {
            var offset = 16 + index * 44;
            BinaryPrimitives.WriteUInt32LittleEndian(bankBytes.AsSpan(4 + index * 4), (uint)offset);
            BinaryPrimitives.WriteUInt32LittleEndian(bankBytes.AsSpan(offset), index == 2 ? 41u : 40u);
            BinaryPrimitives.WriteUInt32LittleEndian(bankBytes.AsSpan(offset + 4), 4);
            Encoding.ASCII.GetBytes(new[] { "Zulu", "Alpha", "InvalidHeader" }[index]).CopyTo(bankBytes, offset + 8);
            payloads[index].CopyTo(bankBytes, offset + 40);
        }
        File.WriteAllBytes(bankPath, bankBytes);
        var textBytes = new byte[12 + 3 * 9]; "BFST"u8.CopyTo(textBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(textBytes.AsSpan(4), 0x420); BinaryPrimitives.WriteUInt32LittleEndian(textBytes.AsSpan(8), 3);
        for (var index = 0; index < 3; index++)
        { var offset = 24 + index * 5; BinaryPrimitives.WriteUInt32LittleEndian(textBytes.AsSpan(12 + index * 4), (uint)(offset - 12)); textBytes[offset] = 1; textBytes[offset + 1] = 1; textBytes[offset + 4] = (byte)(0xD4 - index); }
        File.WriteAllBytes(textPath, textBytes);
        var doc = NativeMapDocument.CreateNewMap(new("SayUI", "SayUI", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
        var metadata = doc.CreateMapSpeechTextDraft();
        doc.SetField(metadata.Record!, metadata.Record!.Fields.Single(field => field.Name == MapSpeechText.StorageField(doc.IsLegacyProject, "SpeechFile")), "SPEECH_LEVEL1");
        var type = LegacyEventSchemas.All.Single(item => item.Name == "ACTION_SAY"); var record = doc.AddScenarioEvent(0xD6, new(0, 0), type);
        string Name(int n) => (native ? "Param" : "param") + n;
        var flag = native ? 3u : 0x80010043u;
        doc.SetField(record, record.Fields.Single(field => field.Name == Name(1)), "65000");
        doc.SetField(record, record.Fields.Single(field => field.Name == Name(5)), unchecked((int)flag).ToString());
        Expect(MemoryMarshal.TryGetArray(record.File.Data, out var raw) && raw.Offset == 0, "Speech raw fixture unavailable.");
        raw.Array![record.Offset + (native ? 10 : 20)] ^= 0xA5;
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        void Exact() => Expect(before.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Speech UI changed live/unrelated bytes before Apply/after Undo.");
        var workspace = new EditorWorkspace { Document = doc }; workspace.ConfigureGameResources(new(resources, "custom-player.exe"));
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var categories = Control<ComboBox>(workspace, "CategoryPicker"); categories.SelectedItem = categories.Items.Cast<object>().Single(item => Get<string>(item, "ID") == record.Category);
        var records = Control<ListBox>(workspace, "RecordList"); records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == record.Offset);
        workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout();
        ScenarioParameterWindow? parent = null; workspace.ScenarioParameterPresenter = window => parent = window;
        Control<Button>(workspace, "ScenarioParameterButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(parent is not null, "Actual SAY toolbar did not open.");
        T Field<T>(string name) => (T)parent!.FindName(name);
        var sounds = Field<ListBox>("ScenarioSpeechList"); var id = Field<TextBox>("ScenarioParam1"); var suppress = Field<CheckBox>("ScenarioSpeechSuppressText");
        Expect(sounds.Items.Count == 3 && id.Text == "65000" && suppress.IsChecked == true && !parent!.HasPendingInputs, "Speech bank/imported unknown controls were lost/repaired.");
        Expect(Field<TextBlock>("ScenarioSpeechImportedIdentifier").Visibility == Visibility.Visible, "Imported out-of-range ID was not explained.");
        Expect(parent!.ApplyDraft() && doc.GetHistory().Undo.Count == history, "Speech opening/no-op normalized imported bytes."); Exact();
        RenderScenarioSpeech(parent, "imported", native, language, directory);
        var previews = new List<string>(); var launches = 0;
        parent.SpeechPlaybackLauncher = request =>
        {
            previews.Add(request.Mp2Path); launches++;
            Expect(request.PlayerPath == "custom-player.exe" && File.ReadAllBytes(request.Mp2Path).SequenceEqual(payloads[int.Parse(id.Text) - 1]), "Preview launched wrong payload/player.");
            Exact();
        };
        sounds.SelectedIndex = 1; Expect(id.Text == "2" && Field<TextBox>("ScenarioSpeechCaption").Text == "B", "Sorted sound name changed one-based ID or corresponding text.");
        Expect(Field<TextBlock>("ScenarioSpeechImportedIdentifier").Visibility == Visibility.Collapsed, "List selection retained the obsolete imported ID warning.");
        Expect(parent.PlaySpeech() && launches == 1, "SAY preview failed.");
        sounds.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = System.Windows.Controls.Control.MouseDoubleClickEvent });
        Expect(launches == 2 && doc.GetHistory().Undo.Count == history, "Original double-click preview missing or edited map.");
        sounds.SelectedIndex = 2; Expect(!Field<Button>("ScenarioSpeechPlay").IsEnabled && !parent.PlaySpeech() && launches == 2, "Unverified sound header was played.");
        id.Text = "0"; Expect(Field<TextBox>("ScenarioSpeechCaption").Text == LocalizationService.Get("ScenarioSpeech.NoSound") && !parent.PlaySpeech(), "ID0 invented a sound.");
        Expect(Field<TextBlock>("ScenarioSpeechImportedIdentifier").Visibility == Visibility.Collapsed, "Typed ID retained the obsolete imported ID warning.");
        id.Text = "9999"; Expect(!parent.PlaySpeech(), "Unknown editable speech ID invented a sound."); Exact();
        id.Text = "10000"; Expect(!parent.ApplyDraft(), "Speech exceeded original four-digit range."); parent.ResetDraft(); Exact();
        Expect(Field<TextBlock>("ScenarioSpeechImportedIdentifier").Visibility == Visibility.Visible, "Reset lost the original imported ID warning.");
        sounds.SelectedIndex = 1; suppress.IsChecked = false;
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(id.Text == "2" && suppress.IsChecked == false && Field<TextBox>("ScenarioSpeechCaption").Text == "B" && parent.HasPendingInputs, "Translation lost pending speech controls/caption.");
        Expect(Field<TextBlock>("ScenarioSpeechImportedIdentifier").Visibility == Visibility.Collapsed, "Translation revived the obsolete imported ID warning.");
        LocalizationService.SetLanguage(language);
        RenderScenarioSpeech(parent, "selected", native, language, directory);
        var expected = before.ToDictionary(item => item.Key, item => item.Value.ToArray());
        if (native) { BinaryPrimitives.WriteUInt16LittleEndian(expected[record.File].AsSpan(record.Offset + 8), 2); expected[record.File][record.Offset + 12] = 0; }
        else { BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(record.Offset + 16), 2); BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(record.Offset + 32), 0); }
        Expect(parent.ApplyDraft() && expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)) && doc.GetHistory().Undo.Count == history + 1, "Speech ID/checkbox did not apply atomically or changed padding.");
        workspace.Undo(); Exact(); workspace.Redo(); Expect(expected.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Speech Redo changed bytes."); workspace.Undo(); parent.ResetDraft(); Exact();
        // Missing captions keep valid audio; malformed bank removes preview,
        // while typed IDs and map bytes remain available/unchanged.
        File.Delete(textPath); parent.ResetDraft(); sounds.SelectedIndex = 0;
        Expect(Field<TextBox>("ScenarioSpeechCaption").Text == LocalizationService.Get("ScenarioSpeech.NoCaption") && parent.PlaySpeech(), "Missing STR disabled valid audio.");
        File.WriteAllBytes(textPath, textBytes);
        File.WriteAllBytes(bankPath, [1, 2, 3]); parent.ResetDraft();
        Expect(sounds.Items.Count == 0 && !parent.PlaySpeech() && id.Text == "65000", "Malformed bank invented entries or repaired IDs.");
        RenderScenarioSpeech(parent, "missing", native, language, directory);
        File.WriteAllBytes(bankPath, bankBytes); parent.ResetDraft(); sounds.SelectedIndex = 0;
        Expect(MemoryMarshal.TryGetArray(record.File.Data, out var currentRaw) && currentRaw.Offset == 0, "Current speech raw fixture unavailable.");
        currentRaw.Array![record.Offset + 15] ^= 1;
        Expect(!parent.PlaySpeech() && !parent.ApplyDraft(), "Unnotified raw map edit retained speech authority."); currentRaw.Array[record.Offset + 15] ^= 1; Exact(); parent.ResetDraft();
        sounds.SelectedIndex = 0; parent.Close(); Expect(!parent.PlaySpeech() && !parent.ApplyDraft() && previews.All(path => !File.Exists(path)), "Closed speech page retained playback/Apply or owned MP2 files."); Exact();
        CheckSpeechCreation();
        ScenarioParameterWindow? late = null;
        late = new(doc, record, () => true, () => { ((TextBox)late!.FindName("ScenarioParam1")).Text = "1"; return true; }, gameResources: new(resources));
        ((TextBox)late.FindName("ScenarioParam1")).Text = "2"; Expect(!late.ApplyDraft(), "Speech input changed during generic commit was accepted."); late.Close(); Exact();
        var stale = new ScenarioParameterWindow(doc, record, () => true, () => true, gameResources: new(resources));
        ((ListBox)stale.FindName("ScenarioSpeechList")).SelectedIndex = 0;
        doc.Paint([new(1, 1)], new(1, 2, 3, 4)); doc.Undo(); Expect(!stale.PlaySpeech() && !stale.ApplyDraft(), "Owner Edit/Undo retained speech authority."); stale.Close(); Exact();
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        records.SelectedItem = records.Items.Cast<object>().Single(item => Get<NativeRecord>(item, "Record").Offset == record.Offset);
        var configured = workspace.CreateScenarioParameterWindow()!; ((ListBox)configured.FindName("ScenarioSpeechList")).SelectedIndex = 0;
        workspace.ConfigureGameResources(new(resources)); Expect(!configured.PlaySpeech() && !configured.ApplyDraft(), "Resource reconfiguration retained prior speech authority."); configured.Close(); Exact();
        workspace.EndScenarioNamesLifetime();
        Expect(File.ReadAllBytes(bankPath).SequenceEqual(bankBytes) && File.ReadAllBytes(textPath).SequenceEqual(textBytes), "SAY modified resource fixtures.");
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} speech source page, actual toolbar/list/caption/double-click, exact MP2 bytes, private creation, imports, translation, atomic Undo and lifetime");

        void CheckSpeechCreation()
        {
            Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
            var templates = Control<ComboBox>(workspace, "TemplatePicker"); templates.SelectedItem = templates.Items.Cast<object>().Single(item => native ? Get<RecordTemplate>(item, "Template").Structure == "Action" : Get<string>(item, "LegacyKind") == "Action");
            var add = ((WrapPanel)templates.Parent).Children.OfType<Button>().Single(button => button.Content?.ToString() == LocalizationService.Get("Editor.AddRecord"));
            ScenarioCreationWindow? wizard = null; workspace.ScenarioCreationPresenter = window => wizard = window;
            void Open()
            {
                add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); var picker = (ComboBox)wizard!.FindName("ScenarioWizardTypes");
                picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type") == type);
                Expect(wizard.Next(), "Speech wizard could not open complete parameters.");
            }
            Open(); var child = wizard!.ParameterEditor!; var list = (ListBox)child.FindName("ScenarioSpeechList");
            Expect(list.Items.Count == 3, "Private creation lost current-map bank/resources.");
            list.SelectedIndex = 1; ((CheckBox)child.FindName("ScenarioSpeechSuppressText")).IsChecked = true;
            string? privatePreview = null; child.SpeechPlaybackLauncher = launch => { privatePreview = launch.Mp2Path; Expect(File.ReadAllBytes(privatePreview).SequenceEqual(payloads[1]), "Creation preview payload differs."); Exact(); };
            Expect(child.PlaySpeech(), "Creation preview missing."); RenderScenarioSpeech(wizard, "creation", native, language, directory);
            Expect(wizard.Back() && privatePreview is not null && !File.Exists(privatePreview) && wizard.Next(), "Speech Back lost private Store or preview cleanup.");
            wizard.Close(); Exact(); Open(); child = wizard!.ParameterEditor!;
            Expect(((TextBox)child.FindName("ScenarioParam1")).Text == "2" && ((CheckBox)child.FindName("ScenarioSpeechSuppressText")).IsChecked == true, "Speech Cancel lost submitted temporary controls.");
            Expect(wizard.Finish(), "Speech Finish failed."); var created = ScriptGraph.Build(doc).Events[wizard.CreatedEventID].Record;
            Expect(created.Read(created.Fields.Single(field => field.Name == Name(1))) == "2" && created.Read(created.Fields.Single(field => field.Name == Name(5))) == "1"
                && doc.GetHistory().Undo.Count == history + 1, "Speech creation lost fields or split live history."); workspace.Undo(); Exact();
        }
    }

    private static void RenderScenarioSpeech(Window window, string state, bool native, string language, string directory)
    {
        var output = Environment.GetEnvironmentVariable("DK2_SCENARIO_SPEECH_ARTIFACTS") ?? directory; Directory.CreateDirectory(output);
        IEnumerable<DependencyObject> Descendants(DependencyObject parent)
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index); yield return child;
                foreach (var descendant in Descendants(child)) yield return descendant;
            }
        }
        foreach (var width in new[] { 540, 700 })
        foreach (var bottom in new[] { false, true })
        {
            var height = width == 540 ? 420 : 580; var content = (FrameworkElement)window.Content;
            content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout();
            var scroll = Descendants(content).OfType<ScrollViewer>().First(view => view.Content is StackPanel);
            if (bottom) scroll.ScrollToEnd(); else scroll.ScrollToTop(); content.UpdateLayout();
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = new FileStream(Path.Combine(output, $"scenario-speech-{state}-{language}-{native}-{width}-{bottom}.png"), FileMode.CreateNew); encoder.Save(file);
        }
    }
}
