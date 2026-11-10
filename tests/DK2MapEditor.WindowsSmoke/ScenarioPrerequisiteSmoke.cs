using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioBandPrerequisites(bool native, string language)
    {
        foreach (var type in new[] { "TRIGGER_BANDCREATED", "TRIGGER_DEATHSINBAND", "TRIGGER_JAILEDINBAND", "TRIGGER_STUNNEDINBAND" })
        foreach (var withBand in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("BandPrerequisiteUI", "BandPrerequisiteUI", 20, 20, NativeFormat: native));
            if (withBand)
            {
                var band = native ? doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEBAND"), new(0, 0)) : doc.AddLegacyRecord("Band", new(0, 0));
                doc.SetField(band, EntityReferences.Identity(band)!.Field, "0");
            }
            var record = native ? doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Trigger"), new(0, 0)) : doc.AddLegacyRecord("Trigger", new(0, 0));
            doc.SetEventType(record, LegacyEventSchemas.All.Single(item => item.Name == type));
            record = ScriptGraph.Build(doc).Events[ScriptGraph.ID(record)].Record;
            var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
            var window = new ScenarioParameterWindow(doc, record, () => true, () => true);
            var apply = (Button)window.FindName("ScenarioPageApply"); var status = (TextBlock)window.FindName("ScenarioPageStatus");
            Expect(apply.IsEnabled == withBand && window.ApplyDraft() == withBand, "Band condition allowed Apply without the original required collection.");
            if (!withBand) Expect(status.Text == LocalizationService.Get("ScenarioPage.RequiresBand"), "Missing-band source requirement was not explained.");
            LocalizationService.SetLanguage(language == "ru" ? "en" : "ru");
            Expect(apply.IsEnabled == withBand, "Translation changed the required-band gate.");
            if (!withBand) Expect(status.Text == LocalizationService.Get("ScenarioPage.RequiresBand"), "Missing-band notice did not translate.");
            LocalizationService.SetLanguage(language);
            Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history, "Requirement/no-op page changed bytes or history.");
            window.Close();
        }
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} four source band-condition prerequisites, absent/present band0, bilingual notice and read-only Apply/Cancel");
    }
}
