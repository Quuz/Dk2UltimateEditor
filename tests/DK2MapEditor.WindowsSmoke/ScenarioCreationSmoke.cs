using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CompleteDefaultCreation(ScenarioCreationWindow window)
    {
        var next = (Button)window.FindName("ScenarioWizardNext");
        for (var index = 0; index < 2 && next.Visibility == Visibility.Visible; index++)
            Expect(window.Next(), "Actual default creation wizard could not advance.");
        Expect(window.Finish(), "Actual default creation wizard could not Finish.");
    }

    private static void CheckScenarioCreation(bool native, string language, string directory)
    {
        var document = NativeMapDocument.CreateNewMap(new("CreationUI", "CreationUI", 20, 20, NativeFormat: native)); document.SetHistoryDepth(65535);
        var workspace = new EditorWorkspace { Document = document };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 1;
        var templates = Control<ComboBox>(workspace, "TemplatePicker");
        var add = ((WrapPanel)templates.Parent).Children.OfType<Button>().Single(button => button.Content?.ToString() == LocalizationService.Get("Editor.AddRecord"));
        ScenarioCreationWindow? active = null; workspace.ScenarioCreationPresenter = window => active = window;
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(bytes.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Creation UI changed live bytes before Finish or changed other data.");
        var before = Snapshot(); var history = document.GetHistory().Undo.Count;
        void Open(string kind)
        {
            templates.SelectedItem = templates.Items.Cast<object>().Single(item => native
                ? Get<RecordTemplate>(item, "Template").Structure == kind : Get<string>(item, "LegacyKind") == kind);
            active = null; add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Expect(active is not null, "Ordinary Add did not open the actual wizard."); Exact(before);
        }
        void Type(string name)
        {
            var picker = (ComboBox)active!.FindName("ScenarioWizardTypes");
            picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<LegacyEventSchemas.EventType>(item, "Type").Name == name);
            Exact(before);
        }
        string TypeName() => Get<LegacyEventSchemas.EventType>(((ComboBox)active!.FindName("ScenarioWizardTypes")).SelectedItem, "Type").Name;
        void Click(string name)
        {
            ((Button)active!.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (active.Content is FrameworkElement content)
            { content.Measure(new Size(700, 580)); content.Arrange(new Rect(0, 0, 700, 580)); content.UpdateLayout(); }
        }
        Open("Trigger"); Type("TRIGGER_TIMEELAPSED"); Click("ScenarioWizardNext");
        Expect(active!.Step == ScenarioCreationStep.Parameters && active.ParameterEditor is not null, "Condition parameter step did not open.");
        ((TextBox)active.ParameterEditor!.FindName("ScenarioSeconds")).Text = "17";
        Render("parameters", 540, 420);
        Click("ScenarioWizardBack"); Expect(active.Step == ScenarioCreationStep.Type, "Back did not store parameters and return to type.");
        Click("ScenarioWizardNext"); Expect(((TextBox)active.ParameterEditor!.FindName("ScenarioSeconds")).Text == "17", "Back/Next lost stored parameter controls.");
        ((TextBox)active.ParameterEditor.FindName("ScenarioSeconds")).Text = "23";
        Click("ScenarioWizardCancel"); Exact(before);
        Open("Trigger"); Expect(TypeName() == "TRIGGER_TIMEELAPSED", "Repeated Add lost canceled temporary type.");
        Click("ScenarioWizardNext"); Expect(((TextBox)active!.ParameterEditor!.FindName("ScenarioSeconds")).Text == "17", "Cancel retained unsubmitted fields instead of last Back-store.");
        Click("ScenarioWizardNext"); Expect(active.Step == ScenarioCreationStep.Repeat, "Condition Next did not reach repeat.");
        var repeat = (ComboBox)active.FindName("ScenarioWizardRepeat");
        repeat.SelectedItem = repeat.Items.Cast<object>().Single(item => Get<ScenarioRepeatMode>(item, "Mode") == ScenarioRepeatMode.Count);
        ((TextBox)active.FindName("ScenarioWizardCount")).Text = "254"; Render("repeat", 540, 420);
        Click("ScenarioWizardBack"); Click("ScenarioWizardNext");
        Expect(((TextBox)active.FindName("ScenarioWizardCount")).Text == "254", "Repeat Back-store was lost.");
        var currentLanguage = LocalizationService.CurrentLanguage; LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(((TextBox)active.FindName("ScenarioWizardCount")).Text == "254", "Translation reset private repeat.");
        LocalizationService.SetLanguage(currentLanguage); Exact(before);
        Click("ScenarioWizardFinish");
        Expect(active.CreatedEventID != 0 && document.GetHistory().Undo.Count == history + 1, "Finish did not create one live Undo.");
        var created = ScriptGraph.Build(document).Events[active.CreatedEventID].Record;
        Expect(created.Read(created.Fields.Single(field => field.Name == (native ? "Param5" : "param5"))) == "17"
            && created.File.Data.Span[created.Offset + (native ? 23 : 55)] == 254, "Final event did not contain stored duration/repeat.");
        var after = Snapshot(); document.Undo(); Exact(before); document.Redo(); Exact(after); document.Undo(); Exact(before);
        // Alias child changes stay private and commit with event creation.
        Open("Trigger"); Type("TRIGGER_TIMER"); Click("ScenarioWizardNext");
        var parameter = active!.ParameterEditor!;
        var alias = (ComboBox)parameter.FindName("AliasEventPrimary");
        alias.Text = "Wizard timer";
        Click("ScenarioWizardNext"); Exact(before);
        Expect(document.ReadScenarioNames().Slots.All(item => item.StoredName == ""), "Next leaked a nested alias.");
        Click("ScenarioWizardFinish");
        Expect(document.ReadScenarioNames().Slots.Any(item => item.StoredName == "Wizard timer") && document.GetHistory().Undo.Count == history + 1, "Finish split or lost alias/event composition.");
        document.Undo(); Exact(before); Expect(document.ReadScenarioNames().Slots.All(item => item.StoredName == ""), "Undo lost alias metadata.");
        // Nested spawn flags/point compose privately with the new action.
        Open("Action"); Type("ACTION_ADDCREATURE"); Click("ScenarioWizardNext"); parameter = active!.ParameterEditor!;
        var flags = parameter.CreateSpawnFlagsWindow(); Expect(flags is not null, "Creation lost nested flag editor.");
        ((CheckBox)flags!.FindName("SpawnFlag4")).IsChecked = true;
        Expect(flags.ApplyDraft(), "Creation flag child could not confirm its private result."); flags.Close();
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var selection = canvas.Selection;
        ((Button)parameter.FindName("ScenarioSpawnPoint")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Expect(canvas.IsPickingRegion, "Creation did not use actual map canvas picking.");
        canvas.CompleteRegionPick(new(6, 7), new(6, 7));
        Expect(canvas.Selection == selection && !canvas.IsPickingRegion, "Creation changed ordinary selection or retained canvas picking.");
        Render("spawn", 540, 420); Exact(before); Click("ScenarioWizardFinish");
        created = ScriptGraph.Build(document).Events[active.CreatedEventID].Record;
        var offset = created.Offset;
        if (native) Expect(created.File.Data.Span.Slice(offset + 8, 8).SequenceEqual(new byte[] { 4, 3, 1, 45, 6, 0, 7, 0 }), "Created spawn differs from literal compiler bytes.");
        else Expect(BinaryPrimitives.ReadUInt32LittleEndian(created.File.Data.Span[(offset + 28)..]) == 45
            && BinaryPrimitives.ReadUInt32LittleEndian(created.File.Data.Span[(offset + 32)..]) == 6
            && BinaryPrimitives.ReadUInt32LittleEndian(created.File.Data.Span[(offset + 40)..]) == 7, "Created spawn differs from source flags/coordinates.");
        document.Undo(); Exact(before);
        // No-parameter action finishes directly and zeroes the source payload.
        Open("Action"); Type("ACTION_VICTORY"); Expect(((Button)active!.FindName("ScenarioWizardNext")).Visibility == Visibility.Collapsed, "No-parameter action wrongly requires a page.");
        Click("ScenarioWizardFinish"); created = ScriptGraph.Build(document).Events[active.CreatedEventID].Record;
        Expect(created.File.Data.Span.Slice(created.Offset + (native ? 8 : 16), native ? 8 : 32).IndexOfAnyExcept((byte)0) < 0, "No-page Finish did not perform original Save zeroing.");
        document.Undo(); Exact(before);
        // Speech opens its complete list/caption/checkbox page even with no bank.
        Open("Action"); Type("ACTION_SAY");
        Expect(active!.Next() && active.ParameterEditor!.FindName("ScenarioSpeechList") is ListBox
            && active.ParameterEditor.FindName("ScenarioSpeechSuppressText") is CheckBox, "Speech creation bypassed its complete source page."); active.Close(); Exact(before);
        // Parameter/repeat changes during the live generic commit invalidate
        // Finish even if they have not reached the private document yet.
        var sourceContext = document.CreateScenarioCreationContext();
        ScenarioCreationWindow? late = null;
        late = new(document, sourceContext, 0xD5, () => true, () =>
        {
            ((TextBox)late!.FindName("ScenarioWizardCount")).Text = "3";
            Expect(!late.Finish(), "Reentrant Finish was accepted."); return true;
        }, _ => throw new InvalidOperationException("Changed-input Finish committed."),
            LegacyEventSchemas.All.Single(item => item.Name == "TRIGGER_APPEAR"));
        Expect(late.Next(), "No-parameter condition did not reach repeat.");
        Expect(!late.Finish(), "Repeat input changed during commit was accepted."); late.Close(); Exact(before);
        late = new(document, sourceContext, 0xD6, () => true, () =>
        {
            ((TextBox)late!.ParameterEditor!.FindName("ScenarioParam5")).Text = "2"; return true;
        }, _ => throw new InvalidOperationException("Changed child Finish committed."),
            LegacyEventSchemas.All.Single(item => item.Name == "ACTION_ADDCREATURE"));
        Expect(late.Next(), "Late-input spawn page did not open.");
        Expect(!late.Finish(), "Parameter input changed during commit was accepted."); late.Close(); Exact(before);
        CheckScriptTree(NativeMapDocument.CreateNewMap(new("WizardGraph", "WizardGraph", 20, 20, NativeFormat: native)), language);
        Open("Action"); Type("ACTION_VICTORY"); document.Paint([new(1, 1)], new(1, 2, 3, 4)); document.Undo(); Exact(before);
        Expect(!active!.Finish(), "Stale owner revision was accepted after Undo."); active.Close();
        Open("Action"); Type("ACTION_VICTORY"); workspace.EndScenarioNamesLifetime(); Expect(!active!.Finish(), "Closed owner accepted delayed Finish."); Exact(before);
        Expect(HistoryLabels.Description("Add scenario event") == LocalizationService.Get("History.AddScenarioEvent"), "Add history label is missing.");
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} private creation wizard, actual Add/Back/Next/Finish/Cancel, repeated temporary retention, aliases/spawn/repeat, translation and atomic Undo/lifetime");

        void Render(string stage, int width, int height)
        {
            var content = (FrameworkElement)active!.Content;
            content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout();
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            var output = Environment.GetEnvironmentVariable("DK2_SCENARIO_CREATION_RENDER_DIR") ?? directory; Directory.CreateDirectory(output);
            using var file = File.Create(Path.Combine(output, $"creation-{stage}-{language}-{native}-{width}.png")); encoder.Save(file);
        }
    }
}
