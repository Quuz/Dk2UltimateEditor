using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    public static void RunPrerequisites()
    {
        using var fixture = JsonDocument.Parse(typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioPrerequisitesSource.json")!);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        foreach (var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Scenario prerequisite source changed.");
        var source = fixture.RootElement.GetProperty("Pages").EnumerateArray().ToDictionary(row => row.GetProperty("Type").GetString()!,
            row => row.GetProperty("Required").EnumerateArray().Select(value => Enum.Parse<EntityKind>(value.GetString()!)).ToArray());
        Expect(source.Count == 12 && source.Keys.Count(type => type.StartsWith("TRIGGER_")) == 4, "Incomplete original map-collection prerequisites.");
        foreach (var native in new[] { false, true })
        {
            var kinds = new[] { EntityKind.Band, EntityKind.Area, EntityKind.Effect, EntityKind.Gate };
            for (var mask = 0; mask < 16; mask++)
            {
                var doc = NativeMapDocument.CreateNewMap(new("Prerequisites", "Prerequisites", 20, 20, NativeFormat: native));
                foreach (var (kind, bit) in kinds.Select((kind, bit) => (kind, bit)))
                {
                    if ((mask & (1 << bit)) == 0) continue;
                    if (kind == EntityKind.Band)
                    {
                        var band = native ? doc.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0)) : doc.AddLegacyRecord("Band", new(0, 0));
                        doc.SetField(band, EntityReferences.Identity(band)!.Field, "0");
                    }
                    else doc.AddMapRegion(kind switch { EntityKind.Area => MapRegionKind.Area, EntityKind.Effect => MapRegionKind.Effect, _ => MapRegionKind.Gate }, new(2, 2, 1, 1));
                }
                foreach (var type in LegacyEventSchemas.All)
                {
                    var required = source.GetValueOrDefault(type.Name, []);
                    var expected = required.Where(kind => (mask & (1 << Array.IndexOf(kinds, kind))) == 0).ToArray();
                    var before = Snapshot(doc); var revision = doc.EditRevision; var count = doc.EditorChangeCount; var history = doc.GetHistory().Undo.Count;
                    Expect(ScenarioControlPages.MissingRequirements(type.Name, doc).SequenceEqual(expected), "Original prerequisite scope/order differs: " + type.Name);
                    Exact(before); Expect(doc.EditRevision == revision && doc.EditorChangeCount == count && doc.GetHistory().Undo.Count == history, "Checking prerequisites changed the document.");
                }
                foreach (var type in source.Keys.Where(type => type.StartsWith("TRIGGER_")))
                {
                    var record = Add(doc, native, pages.Single(page => page.Type == type));
                    var draft = doc.CreateScenarioParameterDraft(record); var before = Snapshot(doc);
                    if ((mask & 1) == 0)
                    {
                        Reject(() => doc.UpdateScenarioControlPage(draft, []));
                        Reject(() => doc.UpdateScenarioControlPage(draft, [new(native ? "Param2" : "param2", "0")]));
                        using var session = doc.CreateScenarioCreationSession(doc.CreateScenarioCreationContext(), 0xD5, LegacyEventSchemas.All.Single(value => value.Name == type));
                        Reject(() => session.WorkingDocument.UpdateScenarioControlPage(session.WorkingDocument.CreateScenarioParameterDraft(session.Record), []));
                        using var editing = doc.CreateScenarioEditingSession(doc.CreateScenarioCreationContext(), record);
                        Reject(() => editing.WorkingDocument.UpdateScenarioControlPage(editing.WorkingDocument.CreateScenarioParameterDraft(editing.Record), []));
                    }
                    else _ = doc.UpdateScenarioControlPage(draft, []);
                    Exact(before);
                }
            }
            Console.WriteLine($"PREREQUISITE DETAIL native={native} all103 types x16 collection masks;12 source pages/four band conditions, band0 and private creation/editing Apply guards; read-only bytes/history/counters");
        }
    }
}
