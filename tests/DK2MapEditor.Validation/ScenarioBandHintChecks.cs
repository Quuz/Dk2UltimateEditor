using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    public static void RunBandHints()
    {
        using var fixture = JsonDocument.Parse(typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioPageHintsSource.json")!);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        foreach (var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Band hint source changed.");
        var types = fixture.RootElement.GetProperty("BandTypes").EnumerateArray().Select(value => value.GetString()!).ToArray();
        Expect(types.Length == 3 && types.All(ScenarioBandHints.Supports) && !ScenarioBandHints.Supports("TRIGGER_BANDCREATED"), "Source population page scope differs.");
        foreach (var native in new[] { false, true })
        {
            var doc = NativeMapDocument.CreateNewMap(new("BandHints", "BandHints", 20, 20, NativeFormat: native));
            var band = native ? doc.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0)) : doc.AddLegacyRecord("Band", new(0, 0));
            doc.SetField(band, EntityReferences.Identity(band)!.Field, "0");
            void Active(int index, bool active)
            {
                if (native)
                {
                    var bytes = Mutable(band.File);
                    bytes[band.Offset + 51 + index * 32 + 15] = active ? (byte)1 : (byte)0;
                    bytes[band.Offset + 51 + index * 32 + 28] = active ? (byte)17 : (byte)0;
                }
                else
                {
                    var field = band.Fields.Single(value => value.Name == $"heros[{index}].isvalid");
                    BinaryPrimitives.WriteInt32LittleEndian(Mutable(band.File).AsSpan(band.Offset + field.Offset), active ? 1 : 0);
                }
            }
            foreach (var type in types)
            {
                var record = Add(doc, native, pages.Single(page => page.Type == type));
                foreach (var (mask, count) in new[] { (0, 0), (1, 1), (3, 2), (15, 4), (65535, 16), (0xA5A5, 1), (4, 0) })
                {
                    for (var index = 0; index < 16; index++) Active(index, (mask & (1 << index)) != 0);
                    var draft = doc.CreateScenarioParameterDraft(record); var before = Snapshot(doc);
                    var revision = doc.EditRevision; var history = doc.GetHistory().Undo.Count; var counter = doc.EditorChangeCount;
                    Expect(doc.ReadScenarioBandPopulation(draft, "0") == count, "Source first-inactive prefix or band-zero hint differs.");
                    foreach (var unknown in new[] { "", "-1", "1", "255", "4294967296", "x" })
                        Expect(doc.ReadScenarioBandPopulation(draft, unknown) is null, "Unknown selection acquired a population.");
                    Exact(before); Expect(doc.EditRevision == revision && doc.GetHistory().Undo.Count == history && doc.EditorChangeCount == counter, "Read-only population changed history/counters.");
                }
                if (native)
                {
                    Active(0, true); var raw = Mutable(band.File); raw[band.Offset + 51 + 15] = 0;
                    Expect(doc.ReadScenarioBandPopulation(doc.CreateScenarioParameterDraft(record), "0") == 0, "Native behavior sentinel ignored.");
                    Active(0, true); raw[band.Offset + 51 + 28] = 0;
                    Expect(doc.ReadScenarioBandPopulation(doc.CreateScenarioParameterDraft(record), "0") == 0, "Native species sentinel ignored.");
                }
                else
                {
                    for (var index = 0; index < 16; index++) Active(index, true);
                    // The seventeenth sentinel is retained bytes, not a visible
                    // field. Source MAPTHINGHERO has eleven 32-bit primitives.
                    var endOffset = band.Fields.Single(field => field.Name == "heros[0].isvalid").Offset + 16 * 44;
                    BinaryPrimitives.WriteInt32LittleEndian(Mutable(band.File).AsSpan(band.Offset + endOffset), 1);
                    Expect(doc.ReadScenarioBandPopulation(doc.CreateScenarioParameterDraft(record), "0") is null, "Unterminated imported band acquired a bounded count.");
                    BinaryPrimitives.WriteInt32LittleEndian(Mutable(band.File).AsSpan(band.Offset + endOffset), 0);
                }
                var stale = doc.CreateScenarioParameterDraft(record);
                doc.SetScenarioName(ScenarioNameKind.Variable, 0, type);
                Reject(() => doc.ReadScenarioBandPopulation(stale, "0"));
                var current = doc.CreateScenarioParameterDraft(record);
                var foreign = NativeMapDocument.CreateNewMap(new("ForeignHint", "ForeignHint", 20, 20, NativeFormat: native));
                Reject(() => foreign.ReadScenarioBandPopulation(current, "0"));
                var stable = Snapshot(doc);
                doc.Edit("Read-only nested hint", () => Reject(() => doc.ReadScenarioBandPopulation(current, "0")));
                Exact(stable);
            }
            var last = doc.Records().First(record => EntityReferences.EventType(record)?.Name == types[0]);
            var duplicate = native ? doc.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0)) : doc.AddLegacyRecord("Band", new(0, 0));
            // Normal editing refuses duplicate IDs. Seed a malformed import
            // directly so this read-only path sees the actual ambiguity.
            var duplicateID = EntityReferences.Identity(duplicate)!.Field;
            Mutable(duplicate.File).AsSpan(duplicate.Offset + duplicateID.Offset, duplicateID.Size).Clear();
            var baseline = Snapshot(doc);
            Expect(doc.ReadScenarioBandPopulation(doc.CreateScenarioParameterDraft(last), "0") is null, "Duplicate band IDs guessed a population."); Exact(baseline);
            Console.WriteLine($"BAND HINT DETAIL native={native} three source pages x seven prefix masks; band0, unknown/duplicate/end/sentinel and foreign/stale/nested guards; read-only bytes/history/counters");
        }
    }
}
