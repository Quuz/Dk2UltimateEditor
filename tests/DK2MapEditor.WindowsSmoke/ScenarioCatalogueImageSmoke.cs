using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private sealed record ScenarioImageRow(string Value, int ImageResource);
    private sealed record ScenarioImageContext(string Type, int Parameter, string Helper, ScenarioImageRow[] Rows);
    private sealed record ScenarioImageFixture(ScenarioImageContext[] Contexts);
    private static void CheckScenarioCatalogueImages(bool native, string language)
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("ScenarioCatalogueImagesSource")!;
        var fixture = JsonSerializer.Deserialize<ScenarioImageFixture>(stream)!;
        var document = NativeMapDocument.CreateNewMap(new("ScenarioImages", "ScenarioImages", 20, 20, NativeFormat: native));
        foreach (var id in new[] { "0", "7" })
        {
            var band = native ? document.AddRecord(RecordTemplate.All.Single(t => t.Structure == "GAMEBAND"), new(0, 0)) : document.AddLegacyRecord("Band", new(0, 0));
            document.SetField(band, EntityReferences.Identity(band)!.Field, id);
        }
        foreach (var group in fixture.Contexts.GroupBy(c => c.Type))
        {
            var type = LegacyEventSchemas.All.Single(t => t.Name == group.Key); var kind = type.RecordSignature == 0xD5 ? "Trigger" : "Action";
            var record = native ? document.AddRecord(RecordTemplate.All.Single(t => t.Structure == kind), new(0, 0)) : document.AddLegacyRecord(kind, new(0, 0));
            document.SetEventType(record, type); record = document.Records().Single(r => r.File == record.File && r.Offset == record.Offset);
            var before = document.Files.ToDictionary(f => f, f => f.Data.ToArray()); var revision = document.EditRevision; var history = document.GetHistory().Undo.Count;
            var current = true;
            var window = new ScenarioParameterWindow(document, record, () => current, () => true);
            foreach (var context in group)
            {
                var combo = (ComboBox)window.FindName("ScenarioParam" + context.Parameter);
                Expect(combo.ItemTemplate is not null && combo.DisplayMemberPath == "", "Scenario source images have no row template.");
                var known = combo.Items.Cast<object>().Where(v => Get<int>(v, "Row") >= 0).ToArray();
                Expect(known.Length == context.Rows.Length, "Scenario image list lost selectable rows.");
                for (var row = 0; row < known.Length; row++)
                {
                    Expect(Get<string>(known[row], "Value") == context.Rows[row].Value, "Scenario image changed a choice value/order.");
                    var icon = Get<BitmapSource>(known[row], "Icon");
                    Expect(icon.IsFrozen && icon.PixelWidth == 32 && icon.PixelHeight == 32, "Scenario bitmap size/freeze differs.");
                }
                var duplicate = known.GroupBy(v => Get<string>(v, "Value")).FirstOrDefault(g => g.Count() > 1);
                combo.SelectedItem = duplicate?.Last() ?? known.Last();
                var selectedRow = Get<int>(combo.SelectedItem!, "Row"); var selectedValue = Get<string>(combo.SelectedItem!, "Value");
                LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
                Expect(Get<int>(combo.SelectedItem!, "Row") == selectedRow && Get<string>(combo.SelectedItem!, "Value") == selectedValue, "Language change lost overlapping row identity.");
                LocalizationService.SetLanguage(language);
            }
            current = false; window.Close();
            Expect(document.EditRevision == revision && document.GetHistory().Undo.Count == history && before.All(b => b.Key.Data.Span.SequenceEqual(b.Value)), "Preview/select/translate/cancel changed the map.");
        }
    }
}
