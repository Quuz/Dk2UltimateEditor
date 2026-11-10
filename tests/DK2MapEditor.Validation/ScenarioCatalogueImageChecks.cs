using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class ScenarioCatalogueImageChecks
{
    private sealed record Row(string Value, int ImageResource);
    private sealed record Context(string Type, int Parameter, string Helper, Row[] Rows);
    private sealed record Fixture(Context[] Contexts);
    internal static void Run()
    {
        using var stream = typeof(ScenarioCatalogueImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioCatalogueImagesSource.json")!;
        var fixture = JsonSerializer.Deserialize<Fixture>(stream)!;
        if (fixture.Contexts.Length != 38 || fixture.Contexts.Sum(c => c.Rows.Length) != 690) throw new Exception("Source image caller coverage changed.");
        foreach (var native in new[] { false, true })
        {
            var document = NativeMapDocument.CreateNewMap(new("ImageChoices", "ImageChoices", 20, 20, NativeFormat: native));
            foreach (var id in new[] { "0", "7" })
            {
                var band = native ? document.AddRecord(RecordTemplate.All.Single(t => t.Structure == "GAMEBAND"), new(0, 0)) : document.AddLegacyRecord("Band", new(0, 0));
                document.SetField(band, EntityReferences.Identity(band)!.Field, id);
                if (band.Fields.FirstOrDefault(f => f.Name == "name") is { } field) document.SetField(band, field, "Band " + id);
            }
            var bytes = document.Files.Select(f => f.Data.ToArray()).ToArray();
            var revision = document.EditRevision; var history = document.GetHistory().Undo.Count;
            foreach (var context in fixture.Contexts)
            {
                var choices = ScenarioNamedCatalogues.Choices(context.Type, context.Parameter, document);
                if (!choices.Select(r => r.Value).SequenceEqual(context.Rows.Select(r => r.Value))) throw new Exception("Source selectable order: " + context.Type + ":" + context.Parameter);
                for (var row = 0; row < choices.Count; row++)
                {
                    var expected = context.Rows[row];
                    var actual = ScenarioNamedCatalogues.ImageResource(context.Type, context.Parameter, row, choices[row].Value);
                    if (actual != expected.ImageResource) throw new Exception($"Source image {context.Type}/{context.Parameter}/{row}/{expected.Value}: expected {expected.ImageResource}, got {actual}.");
                    if (ScenarioNamedCatalogues.ImageResource(context.Type, context.Parameter, -1, expected.Value) is not null) throw new Exception("Unknown imported row received an image.");
                }
            }
            foreach (var (type, parameter) in new[] { ("TRIGGER_AREACONTENT", 2), ("ACTION_ADDCREATURE", 2), ("ACTION_ADDELEMENT", 1), ("ACTION_CHANGESQUARE", 2), ("ACTION_ADDCREATURE", 3), ("ACTION_DESTROYGATE", 5), ("ACTION_SHOWAREA", 1), ("ACTION_TOGGLEEFFECT", 1) })
                foreach (var (value, row) in ScenarioNamedCatalogues.Choices(type, parameter, document).Select((v, i) => (v, i)))
                    if (ScenarioNamedCatalogues.ImageResource(type, parameter, row, value.Value) is not null) throw new Exception("An ordinary source combo acquired an image.");
            if (ScenarioNamedCatalogues.ImageResource("TRIGGER_DOORS", 2, 1, "999999") is not null
                || ScenarioNamedCatalogues.ImageResource("TRIGGER_CLICKELEMENT", 1, 0, "246") is not null
                || ScenarioNamedCatalogues.ImageResource("unknown", 1, 0, "1") is not null) throw new Exception("Unknown image identity was guessed.");
            var destroyed = fixture.Contexts.Single(c => c.Type == "TRIGGER_DESTROYED");
            var overlap = destroyed.Rows.Select((r, i) => (r, i)).GroupBy(r => r.r.Value).First(g => g.Count() > 1).ToArray();
            if (overlap.Select(r => ScenarioNamedCatalogues.ImageResource(destroyed.Type, destroyed.Parameter, r.i, r.r.Value)).Distinct().Count() != overlap.Length) throw new Exception("Overlapping door/trap IDs lost their category.");
            if (document.EditRevision != revision || document.GetHistory().Undo.Count != history
                || !document.Files.Select((f, i) => f.Data.Span.SequenceEqual(bytes[i])).All(v => v)) throw new Exception("Reading images changed map bytes/revision/history.");
        }
    }
}
