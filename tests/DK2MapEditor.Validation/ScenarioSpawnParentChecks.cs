using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    public static void RunSpawnParent()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioAddCreaturePage.json")!;
        using var fixture = JsonDocument.Parse(stream);
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Original source missing.");
        foreach (var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original spawn source changed: " + input.Name);
        var sourcePage = fixture.RootElement.GetProperty("SourcePage");
        Expect(sourcePage.GetProperty("DirectStoreParameters").EnumerateArray().Select(item => item.GetInt32()).SequenceEqual(new[] { 1, 2, 3, 5, 7 })
            && sourcePage.GetProperty("CallbackOnlyParameters").EnumerateArray().Single().GetInt32() == 4
            && sourcePage.GetProperty("NumericInputs").EnumerateArray().Select(item => (item.GetProperty("Parameter").GetInt32(), item.GetProperty("Signed").GetBoolean(), item.GetProperty("StoredOffset").GetInt32(), item.GetProperty("SourceDisplayMaximum").GetInt32()))
                .SequenceEqual(new[] { (5, false, 0, 999), (7, false, 0, 999) }), "Original spawn stores/coordinate conventions differ.");
        var flags = fixture.RootElement.GetProperty("CreatureOptions");
        var expectedFlags = flags.GetProperty("Controls").EnumerateArray().Select(item => (Bit: item.GetProperty("Bit").GetUInt32(), Editable: item.GetProperty("Enabled").GetBoolean())).ToArray();
        Expect(expectedFlags.Length == 7 && expectedFlags.Select(item => item.Bit).SequenceEqual(new uint[] { 1, 8, 16, 32, 64, 128, 4 })
            && expectedFlags.Where(item => item.Editable).Aggregate(0u, (mask, item) => mask | item.Bit) == 0xBD, "Independent ADD options oracle incomplete.");
        Expect(ScenarioSpawnOptions.Flags.Select(item => (item.Bit, item.Editable)).SequenceEqual(expectedFlags) && ScenarioSpawnOptions.EditableMask == 0xBD, "Spawn flags differ from original 6111 controls.");
        var page = pages.Single(item => item.Type == "ACTION_ADDCREATURE"); Expect(page.PageID == 7203, "Spawn parent source page identity differs.");
        var controls = ScenarioControlPages.Controls(page.Type);
        Expect(controls.Select(item => (item.Parameter, item.Kind, item.Maximum, item.Unsigned)).SequenceEqual(new[] {
            (1, ScenarioControlKind.NamedChoice, 0L, false), (2, ScenarioControlKind.NamedChoice, 0L, false), (3, ScenarioControlKind.NamedChoice, 0L, false),
            (4, ScenarioControlKind.Number, (long)uint.MaxValue, true), (5, ScenarioControlKind.Number, 999L, true), (7, ScenarioControlKind.Number, 999L, true) }), "Spawn parent controls differ from source.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2SpawnParent-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true })
            {
                var doc = NativeMapDocument.CreateNewMap(new("SpawnParent", "SpawnParent", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
                var area = doc.AddMapRegion(MapRegionKind.Area, new(3, 5, 8, 6)); doc.SetField(area, EntityReferences.Identity(area)!.Field, "13");
                var record = Add(doc, native, page); var operands = Fields(native, page); string Name(int parameter) => (native ? "Param" : "param") + parameter;
                void Encode(Dictionary<NativeFile, byte[]> expected, int number, uint value)
                {
                    var operand = operands.Single(item => item.Name == Name(number));
                    var offset = native ? number <= 4 ? 7 + number : number == 5 ? 12 : 14 : 12 + number * 4;
                    Expect(operand.Offset == offset && operand.Size == (native ? number <= 4 ? 1 : 2 : 4), "Spawn compiler locations differ from literal offsets.");
                    if (operand.Size == 4) BinaryPrimitives.WriteUInt32LittleEndian(expected[record.File].AsSpan(record.Offset + offset), value);
                    else Write(expected[record.File], record.Offset + offset, operand, value);
                }
                var initial = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
                Expect(ScenarioControlPages.MissingRequirements(page.Type, doc).Count == 0, "Spawn invented a mandatory area prerequisite.");
                doc.ApplyScenarioParameterDraft(doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [])); Exact(initial);
                foreach (var context in ReadNamed().Contexts.Where(item => item.Type == page.Type))
                {
                    Expect(ScenarioNamedCatalogues.Choices(context.Type, context.Parameter).Select(item => int.Parse(item.Value, CultureInfo.InvariantCulture)).SequenceEqual(context.Values), "Spawn named choices differ from independent original fixture.");
                    foreach (var value in context.Values)
                    {
                        var expected = initial.ToDictionary(item => item.Key, item => item.Value.ToArray()); Encode(expected, context.Parameter, checked((uint)value));
                        var draft = doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(context.Parameter), value.ToString(CultureInfo.InvariantCulture))]); Exact(initial);
                        doc.ApplyScenarioParameterDraft(draft); Exact(expected); Expect(doc.GetHistory().Undo.Count == history + 1, "Spawn catalogue edit was not one Undo."); doc.Undo(); Exact(initial);
                    }
                }
                var editable = expectedFlags.Where(item => item.Editable).Select(item => item.Bit).ToArray();
                foreach (var bits in native ? new uint[] { 0, 2, 64, 66, 255 } : new uint[] { 0, 2, 64, 66, 255, 0x80010042 })
                {
                    doc.SetField(record, record.Fields.Single(item => item.Name == Name(4)), unchecked((int)bits).ToString(CultureInfo.InvariantCulture));
                    var baseline = Snapshot(doc); var count = doc.GetHistory().Undo.Count;
                    for (var combination = 0; combination < 64; combination++)
                    {
                        var inputs = editable.Select((bit, index) => new KeyValuePair<uint, bool>(bit, (combination & (1 << index)) != 0)).ToArray();
                        var wanted = bits & ~0xBDu;
                        foreach (var input in inputs) if (input.Value) wanted |= input.Key;
                        var expected = baseline.ToDictionary(item => item.Key, item => item.Value.ToArray()); Encode(expected, 4, wanted);
                        var prepared = doc.UpdateScenarioSpawnFlags(doc.CreateScenarioParameterDraft(record), inputs); Exact(baseline);
                        doc.ApplyScenarioParameterDraft(prepared); Exact(expected);
                        var changed = wanted != bits; Expect(doc.GetHistory().Undo.Count == count + (changed ? 1 : 0), "Spawn flags made wrong history/no-op.");
                        if (changed) { doc.Undo(); Exact(baseline); doc.Redo(); Exact(expected); doc.Undo(); }
                    }
                    foreach (var invalid in new[] { 0u, 2u, 64u, 256u }) Reject(() => doc.UpdateScenarioSpawnFlags(doc.CreateScenarioParameterDraft(record), [new(invalid, true)]));
                    Reject(() => doc.UpdateScenarioSpawnFlags(doc.CreateScenarioParameterDraft(record), [new(1, true), new(1, false)])); Exact(baseline);
                    doc.Undo(); Exact(initial);
                }
                var pointDraft = doc.UpdateScenarioSpawnArea(doc.CreateScenarioParameterDraft(record), 13);
                Expect(pointDraft.Values[Name(5)] == "3" && pointDraft.Values[Name(7)] == "5", "Area chooser stored ID/centre/one-based coordinates instead of normalized top-left."); Exact(initial);
                pointDraft = doc.UpdateScenarioSpawnFlags(pointDraft, [new(1, true), new(128, false)]);
                pointDraft = doc.UpdateScenarioControlPage(pointDraft, [new(Name(1), "4"), new(Name(2), "3"), new(Name(3), "10")]);
                var expectedBatch = initial.ToDictionary(item => item.Key, item => item.Value.ToArray());
                var oldFlags = native ? initial[record.File][record.Offset + 11] : BinaryPrimitives.ReadUInt32LittleEndian(initial[record.File].AsSpan(record.Offset + 28));
                foreach (var (number, value) in new[] { (1, 4u), (2, 3u), (3, 10u), (4, (oldFlags | 1u) & ~128u), (5, 3u), (7, 5u) }) Encode(expectedBatch, number, value);
                doc.ApplyScenarioParameterDraft(pointDraft); Exact(expectedBatch); Expect(doc.GetHistory().Undo.Count == history + 1, "Whole spawn parent was not one edit.");
                var saved = doc.SaveCopy(Path.Combine(directory, native ? "native" : "project")); var reopened = NativeMapDocument.Load(saved);
                Expect(reopened.CreateScenarioParameterDraft(reopened.Records().Single(item => EntityReferences.EventType(item)?.Name == page.Type)).Values.SequenceEqual(doc.CreateScenarioParameterDraft(record).Values), "Spawn parent save/reopen lost operands.");
                if (!native)
                {
                    var compiled = doc.CompileLegacyProject("SpawnCompiled", NativeMapDocument.Create(3, 3, "Basis")); var action = compiled.Records().Single(item => EntityReferences.EventType(item)?.Name == page.Type);
                    Expect(action.File.Data.Span[action.Offset + 8] == 4 && action.File.Data.Span[action.Offset + 9] == 3 && action.File.Data.Span[action.Offset + 10] == 10
                        && action.File.Data.Span[action.Offset + 11] == (byte)((oldFlags | 1u) & ~128u)
                        && BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span[(action.Offset + 12)..]) == 3
                        && BinaryPrimitives.ReadUInt16LittleEndian(action.File.Data.Span[(action.Offset + 14)..]) == 5, "Spawn parent compiled literal operands incorrectly.");
                }
                doc.Undo(); Exact(initial); doc.Redo(); Exact(expectedBatch); doc.Undo(); Exact(initial);
                foreach (var point in new[] { new TilePoint(0, 0), new(19, 19) })
                { var prepared = doc.UpdateScenarioSpawnPoint(doc.CreateScenarioParameterDraft(record), point); Exact(initial); Expect(prepared.Values[Name(5)] == point.X.ToString() && prepared.Values[Name(7)] == point.Y.ToString(), "Map point was offset."); }
                foreach (var number in new[] { 5, 7 })
                { var prepared = doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(number), "999")]); Expect(prepared.Values[Name(number)] == "999", "Typed source coordinates were incorrectly restricted to map dimensions."); Reject(() => doc.UpdateScenarioControlPage(doc.CreateScenarioParameterDraft(record), [new(Name(number), "1000")])); }
                foreach (var point in new[] { new TilePoint(-1, 0), new(20, 0), new(0, 20) }) Reject(() => doc.UpdateScenarioSpawnPoint(doc.CreateScenarioParameterDraft(record), point));
                Reject(() => doc.UpdateScenarioSpawnArea(doc.CreateScenarioParameterDraft(record), 14));
                var stale = doc.CreateScenarioParameterDraft(record); doc.SetField(record, record.Fields.Single(item => item.Name == Name(5)), "1"); doc.Undo();
                Reject(() => doc.UpdateScenarioSpawnFlags(stale, [new(1, true)])); Reject(() => doc.UpdateScenarioSpawnPoint(stale, new(1, 1))); Exact(initial);
                var foreign = NativeMapDocument.CreateNewMap(new("ForeignSpawn", "ForeignSpawn", 20, 20, NativeFormat: native)); Reject(() => foreign.UpdateScenarioSpawnFlags(doc.CreateScenarioParameterDraft(record), [new(1, true)]));
                var rawDraft = doc.CreateScenarioParameterDraft(record); Mutable(record.File)[record.Offset + 15] ^= 1; Reject(() => doc.UpdateScenarioSpawnFlags(rawDraft, [new(1, true)])); Mutable(record.File)[record.Offset + 15] ^= 1; Exact(initial);
                Expect(doc.GetHistory().Undo.Count == history, "Rejected spawn draft changed history.");
            }
        }
        finally { Directory.Delete(directory, true); }
    }
}
