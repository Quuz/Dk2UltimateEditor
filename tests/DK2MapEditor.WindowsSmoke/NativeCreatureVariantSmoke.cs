using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static readonly (string Path, int Offset, int Count, int Size)[] CreatureVariantInputs = [
        (@"D:\Games\DK2\Tools\ORIGIN17Creatures.kwd", 0, 46, 5449),
        (@"D:\Games\DK2\DKII 13\Dungeon Keeper 2\Data\editor\Creatures.kwd", 0, 44, 5449),
        (@"D:\Games\DK2\Dungeon Keeper 2\Data\editor\maps\ANL - 3 FunGlobals.kld", 50174, 46, 5537),
        (@"D:\Games\DK2\DKII 13\Dungeon Keeper 2\Data\editor\AutoSaveGlobals.kld", 45353, 44, 5537)
    ];
    private static NativeMapDocument CreatureVariantFixture(string directory, string language, (string Path, int Offset, int Count, int Size) input, bool malformed = false)
    {
        DefinitionFixture(directory, language, false);
        var location = Path.Combine(directory, "definitions-" + language);
        var bytes = File.ReadAllBytes(input.Path).AsSpan(input.Offset, 56 + input.Count * input.Size).ToArray();
        if (malformed) bytes[56 + 0xF4B] = 99;
        File.WriteAllBytes(Path.Combine(location, "Data", "Editor", "Creatures.kwd"), bytes);
        return NativeMapDocument.Load(Path.Combine(location, "Data", "Editor", "Maps", "DefinitionsUI.kwd"));
    }
    private static void CheckCreatureVariantControls(NativeMapDocument document, string language, int count, int size, NativeMapDocument malformed)
    {
        var workspace = new EditorWorkspace { Document = document };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 9;
        void Layout() { workspace.Measure(new Size(1500, 1000)); workspace.Arrange(new Rect(0, 0, 1500, 1000)); workspace.UpdateLayout(); }
        var family = Control<ComboBox>(workspace, "DefinitionFamily"); var section = Control<ComboBox>(workspace, "DefinitionSection");
        var picker = Control<ComboBox>(workspace, "DefinitionPicker"); var table = Control<DataGrid>(workspace, "DefinitionTable");
        var number = Control<TextBox>(workspace, "DefinitionNumber"); var confirm = Control<CheckBox>(workspace, "DefinitionConfirmed");
        var apply = Control<Button>(workspace, "DefinitionApply"); var reset = Control<Button>(workspace, "DefinitionReset"); var all = Control<Button>(workspace, "DefinitionResetAll");
        void Choose(ComboBox combo, string id) => combo.SelectedItem = combo.Items.Cast<object>().Single(item => Get<string>(item, "ID") == id);
        void Pick(int index) => picker.SelectedItem = picker.Items.Cast<object>().Single(item => Get<GlobalDefinitionRow>(item, "Row").Index == index);
        void Field(string name) => table.SelectedItem = table.Items.Cast<object>().Single(item => Get<GlobalDefinitionValue>(item, "Field").Name == name);
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected) => Expect(expected.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Historical UI changed unrelated bytes/tails or failed exact Undo.");
        Choose(family, "Creatures"); Choose(section, "Attributes"); Pick(0); Field("Health"); Layout();
        Expect(picker.Items.Count == 45 && apply.IsEnabled && reset.IsEnabled && all.IsEnabled, "Known historical creature controls unavailable.");
        var initial = Snapshot(); var beforeHistory = document.GetHistory().Undo.Count;
        number.Text = "999"; Click(apply);
        Expect(document.GlobalDefinitionValues(Get<GlobalDefinitionRow>(picker.SelectedItem, "Row")).Single(value => value.Name == "Health").Value == "999", "Historical UI did not edit stored health.");
        var edited = Snapshot(); Expect(document.GetHistory().Undo.Count == beforeHistory + 1, "Historical UI apply was not one Undo.");
        Click(all); Exact(edited); confirm.IsChecked = true; Click(all);
        foreach (var row in document.GlobalDefinitionRows().Where(row => row.Family == "Creatures" && row.Editable))
            Expect(document.GlobalDefinitionValues(row).Single(value => value.Name == "Health") is var health && health.Value == health.DefaultValue, "All reset missed an available historical species.");
        workspace.Undo(); Exact(edited); workspace.Undo(); Exact(initial);
        void Render(string state)
        {
            if (Environment.GetEnvironmentVariable("DK2_NATIVE_CREATURE_UI_ARTIFACTS") is not { Length: > 0 } artifacts) return;
            Directory.CreateDirectory(artifacts); Layout(); var bitmap = new RenderTargetBitmap(1500, 1000, 96, 96, PixelFormats.Pbgra32); bitmap.Render(workspace);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(artifacts, $"creatures-{language}-{count}-{size}-{state}.png")); encoder.Save(output);
        }
        Render("available");
        if (count == 44)
        {
            Pick(14); Field("Health"); Layout();
            Expect(!apply.IsEnabled && !reset.IsEnabled && !Control<Button>(workspace, "DefinitionProperties").IsEnabled && all.IsEnabled && number.Text.Length == 0
                && table.Items.Cast<object>().All(item => Get<string>(item, "Value") == "—"), "Missing historical species was invented or blocked available-family reset.");
            var oldLabel = Get<string>(picker.SelectedItem, "Label"); LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); workspace.RefreshLanguage(); Layout();
            Expect(Get<GlobalDefinitionRow>(picker.SelectedItem, "Row").Index == 14 && Get<string>(picker.SelectedItem, "Label") != oldLabel && !apply.IsEnabled, "Translation enabled/renumbered missing historical species.");
            LocalizationService.SetLanguage(language); workspace.RefreshLanguage(); Render("missing");
            Pick(0); Field("Health"); number.Text = "999"; Click(apply); var availableEdit = Snapshot();
            Pick(14); confirm.IsChecked = true; Click(all); Expect(document.Records().Count(row => row.Category == "Definition.Creatures") == 44, "All reset added missing historical records.");
            workspace.Undo(); Exact(availableEdit); workspace.Undo(); Exact(initial);
        }
        workspace.Document = malformed; Choose(family, "Creatures"); Pick(0); Choose(section, "Attacks"); Layout();
        Expect(!apply.IsEnabled && !reset.IsEnabled && !all.IsEnabled && !Control<Button>(workspace, "DefinitionAttackApply").IsEnabled, "Unknown catalogue exposed ordinary or compound edits.");
        workspace.Document = null;
        Console.WriteLine($"PASS {language} real creature {count}/{size} UI apply, full-family reset, missing species, opaque tails, Undo and unknown catalogue guards");
    }
    private static void CheckCreatureVariants(string directory, string language)
    {
        var hashes = CreatureVariantInputs.ToDictionary(input => input.Path, input => SHA256.HashData(File.ReadAllBytes(input.Path)));
        foreach (var input in CreatureVariantInputs)
        {
            var root = Path.Combine(directory, $"variants-{input.Count}-{input.Size}");
            NativeMapDocument Fixture(string name, bool malformed = false) => CreatureVariantFixture(Path.Combine(root, name), language, input, malformed);
            CheckCreatureVariantControls(Fixture("controls"), language, input.Count, input.Size, Fixture("unknown", true));
            CheckDefinitionNumbers(Fixture("numbers"), language);
            CheckCreatureJobs(Fixture("jobs"), language);
            CheckCreatureAttacks(Fixture("attacks"), language);
            CheckCreatureOptions(Fixture("options"), language);
        }
        Expect(hashes.All(row => SHA256.HashData(File.ReadAllBytes(row.Key)).AsSpan().SequenceEqual(row.Value)), "Historical UI tests changed original game files.");
    }
}
