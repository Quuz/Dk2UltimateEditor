using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioBandPreviews(bool native, string language)
    {
        var document = NativeMapDocument.CreateNewMap(new("BandPreviewUI", "BandPreviewUI", 20, 20, NativeFormat:native));
        byte[] MutablePreview(NativeFile file)
        {
            Expect(MemoryMarshal.TryGetArray(file.Data, out var bytes) && bytes.Offset == 0, "Independent preview fixture unavailable."); return bytes.Array!;
        }
        NativeRecord AddBand()
        {
            var band = native ? document.AddRecord(RecordTemplate.All.Single(t => t.Structure == "GAMEBAND"), new(0, 0)) : document.AddLegacyRecord("Band", new(0, 0));
            var id = EntityReferences.Identity(band)!.Field; id.Encode("0").CopyTo(MutablePreview(band.File).AsSpan(band.Offset + id.Offset)); return band;
        }
        void Seed(NativeRecord band, int slot, uint id, uint level, uint behavior, uint objective, uint area)
        {
            var bytes = MutablePreview(band.File); var start = band.Offset + (native ? 51 + slot * 32 : 12 + slot * 44);
            if (native)
            {
                bytes[start + 14] = (byte)level; bytes[start + 15] = (byte)behavior; bytes[start + 16] = (byte)area; bytes[start + 27] = (byte)objective; bytes[start + 28] = (byte)id;
            }
            else foreach (var (offset, value) in new[] { (0,1u), (4,level), (12,id), (28,objective), (32,behavior), (36,area) }) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(start + offset), value);
        }
        var first = AddBand(); Seed(first, 0, 17, 2, 1, 23, 47); Seed(first, 1, 23, 3, 3, 23, 47); Seed(first, 2, 250, 4, 3, 255, 8);
        var second = AddBand(); Seed(second, 0, 17, 9, 1, 19, 0);
        foreach (var name in new[] { "TRIGGER_BANDCREATED", "TRIGGER_STUNNEDINBAND", "TRIGGER_JAILEDINBAND", "TRIGGER_DEATHSINBAND", "ACTION_RELEASEBAND" })
        {
            var type = LegacyEventSchemas.All.Single(t => t.Name == name); var kind = type.RecordSignature == 0xD5 ? "Trigger" : "Action";
            var record = native ? document.AddRecord(RecordTemplate.All.Single(t => t.Structure == kind), new(0, 0)) : document.AddLegacyRecord(kind, new(0, 0));
            document.SetEventType(record, type); record = document.Records().Single(r => r.File == record.File && r.Offset == record.Offset);
            var before = document.Files.ToDictionary(f => f, f => f.Data.ToArray()); var revision = document.EditRevision; var history = document.GetHistory().Undo.Count;
            var current = true; var window = new ScenarioParameterWindow(document, record, () => current, () => true);
            var combo = (ComboBox)window.FindName("ScenarioParam" + ScenarioBandCatalogue.Parameter(name));
            object[] Rows() => combo.Items.Cast<object>().ToArray();
            var rows = Rows(); Expect(rows.Length == 6, "Band members were omitted or became extra selectable bands.");
            Expect(rows.Select(r => Get<bool>(r, "Selectable")).SequenceEqual(new[] { true, false, false, false, true, false }), "Band hierarchy/order differs.");
            Expect(Get<string>(rows[2], "Label").Contains("47") && Get<string>(rows[3], "Label").Contains("255") && Get<string>(rows[3], "Label").Contains("250"), "Leader/objective/area/unknown details omitted.");
            Expect(Get<BitmapSource>(rows[1], "Icon").PixelWidth == 32 && rows[3].GetType().GetProperty("Icon")!.GetValue(rows[3]) is null, "Member image guessed or missing.");
            combo.SelectedItem = rows[4];
            var pending = window.HasPendingInputs; combo.SelectedItem = rows[1];
            Expect(ReferenceEquals(combo.SelectedItem, rows[4]) && window.HasPendingInputs == pending, "Informational member row changed the band parameter.");
            window.Measure(new Size(700, 580)); window.Arrange(new Rect(0, 0, 700, 580)); window.UpdateLayout();
            combo.IsDropDownOpen = true; combo.UpdateLayout();
            var childContainer = combo.ItemContainerGenerator.ContainerFromItem(rows[1]) as ComboBoxItem;
            Expect(childContainer is not null && !childContainer.IsEnabled && !childContainer.Focusable, "Informational member row still takes keyboard/mouse selection."); combo.IsDropDownOpen = false;
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
            Expect(Get<int>(combo.SelectedItem!, "Row") == 1 && Rows().Length == 6 && Get<string>(Rows()[2], "Label").Contains(language == "en" ? "Лидер" : "Leader"), "Translation lost band occurrence or member caption.");
            LocalizationService.SetLanguage(language);
            Expect(document.EditRevision == revision && document.GetHistory().Undo.Count == history && before.All(b => b.Key.Data.Span.SequenceEqual(b.Value)), "Preview/select/translate changed map bytes/history.");
            var labels = Rows().Select(r => Get<string>(r, "Label")).ToArray();
            document.SetScenarioName(ScenarioNameKind.Variable, 0, "Changed"); Seed(first, 0, 17, 8, 3, 23, 99);
            LocalizationService.SetLanguage(language == "en" ? "ru" : "en"); LocalizationService.SetLanguage(language);
            Expect(Rows().Select(r => Get<string>(r, "Label")).SequenceEqual(labels), "Stale language callback reread live band payload.");
            current = false; window.Close();
        }
    }
}
