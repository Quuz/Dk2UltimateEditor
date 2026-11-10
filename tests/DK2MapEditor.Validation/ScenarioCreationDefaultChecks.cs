using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    internal static void RunCreationDefaults()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioCreationDefaults.json")!;
        using var fixture = JsonDocument.Parse(stream);
        var oracle = fixture.RootElement;
        var root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "Source"))) root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Original source missing.");
        foreach (var input in oracle.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, input.Name)))).Equals(input.Value.GetString(), StringComparison.OrdinalIgnoreCase), "Original defaults/compiler input changed.");
        var defaults = oracle.GetProperty("Pages").EnumerateArray().ToDictionary(item => item.GetProperty("Type").GetString()!,
            item => Enumerable.Range(1, 8).Select(number => unchecked((uint)item.GetProperty("Values").GetProperty(number.ToString()).GetInt64())).ToArray());
        var flow = oracle.GetProperty("CreationAndSaveFlow");
        var zero = flow.EnumerateObject().SelectMany(item => item.Value.GetProperty("ZeroOnSaveTypes").EnumerateArray().Select(value => value.GetString()!)).ToHashSet();
        Expect(pages.Length == 103 && defaults.Count == 70 && zero.Count == 26, "Independent defaults/save catalogue is incomplete.");
        Expect(defaults["ACTION_ADDCREATURE"].SequenceEqual(new uint[] { 4, 3, 1, 41, 1, 0, 1, 0 }), "Literal spawn defaults differ.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2ScenarioDefaults-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true })
            {
                var doc = NativeMapDocument.CreateNewMap(new("Defaults", "Defaults", 20, 20, NativeFormat: native)); doc.SetHistoryDepth(65535);
                var gate = doc.AddMapRegion(MapRegionKind.Gate, new(3, 4, 1, 1));
                if (!native) doc.SetField(gate, EntityReferences.Identity(gate)!.Field, "1");
                var crystal = native ? doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEOBJECT"), new(5, 6)) : doc.AddLegacyRecord("Object", new(5, 6));
                var objectID = crystal.Fields.Single(item => item.Name == (native ? "bObjectID" : "id")); doc.SetField(crystal, objectID, "28");
                var creations = 0; var changes = 0;
                foreach (var source in pages)
                {
                    var type = LegacyEventSchemas.All.Single(item => item.Name == source.Type);
                    var kind = source.Signature == 213 ? "Trigger" : "Action";
                    var first = flow.GetProperty(kind); var initialName = first.GetProperty("InitialType").GetString()!;
                    var initial = first.GetProperty("InitialValues").EnumerateArray().Select(item => item.GetUInt32()).ToArray();
                    var wanted = source.Type != initialName && defaults.TryGetValue(source.Type, out var defined) ? defined : initial;
                    if (zero.Contains(source.Type)) wanted = new uint[8];
                    var before = Snapshot(doc); var history = doc.GetHistory().Undo.Count;
                    var record = doc.AddScenarioEvent(source.Signature, new(0, 0), type);
                    var expected = Snapshot(doc); WriteExpected(expected, record, source, wanted, native, true);
                    Exact(expected); Expect(doc.GetHistory().Undo.Count == history + 1, "New scenario event was not one Undo.");
                    doc.Undo(); Exact(before); doc.Redo(); Exact(expected); creations++;
                    record = Current(doc, record);
                    // Independent imported parameter bytes, including high DWORD values,
                    // inactive parameters and unusual repeat. Changing a type must not touch links/aliases.
                    if (native)
                    {
                        for (var index = 0; index < 8; index++) Mutable(record.File)[record.Offset + 8 + index] = (byte)(0xA5 ^ index * 17);
                    }
                    else for (var number = 0; number < 8; number++)
                        BinaryPrimitives.WriteUInt32LittleEndian(Mutable(record.File).AsSpan(record.Offset + 16 + 4 * number), 0x82436517u + (uint)number);
                    Mutable(record.File)[record.Offset + (native ? 23 : 55)] = 255;
                    var baseline = Snapshot(doc); var revision = doc.EditRevision; history = doc.GetHistory().Undo.Count;
                    var same = doc.PrepareScenarioEventTypeChange(record, type); doc.ApplyScenarioEventTypeChange(same);
                    Exact(baseline); Expect(doc.EditRevision == revision && doc.GetHistory().Undo.Count == history, "Same-type selection reset values or created history.");
                    var old = ReadLogical(record, source, native);
                    foreach (var target in pages.Where(page => page.Signature == source.Signature && page.Type != source.Type))
                    {
                        var targetType = LegacyEventSchemas.All.Single(item => item.Name == target.Type);
                        wanted = defaults.TryGetValue(target.Type, out defined) ? defined : old;
                        if (zero.Contains(target.Type)) wanted = new uint[8];
                        expected = baseline.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone()); WriteExpected(expected, record, target, wanted, native, false);
                        var request = doc.PrepareScenarioEventTypeChange(record, targetType); Exact(baseline);
                        doc.ApplyScenarioEventTypeChange(request); Exact(expected);
                        Expect(doc.GetHistory().Undo.Count == history + 1, "Changed type was not one Undo.");
                        doc.Undo(); Exact(baseline); doc.Redo(); Exact(expected); doc.Undo(); Exact(baseline); changes++;
                    }
                    record = Current(doc, record);
                    // Receipt invalidation is revision- and byte-sensitive, even after Undo.
                    var requestOld = doc.PrepareScenarioEventTypeChange(record, type);
                    var repeat = record.Fields.Single(item => item.Name == (native ? "Repeat" : source.Signature == 213 ? "repeat" : "one"));
                    doc.SetField(record, repeat, "254"); doc.Undo(); Exact(baseline);
                    Reject(() => doc.ApplyScenarioEventTypeChange(requestOld)); Exact(baseline);
                    requestOld = doc.PrepareScenarioEventTypeChange(record, type);
                    Mutable(record.File)[record.Offset + (native ? 23 : 55)] = 253;
                    Reject(() => doc.ApplyScenarioEventTypeChange(requestOld)); Mutable(record.File)[record.Offset + (native ? 23 : 55)] = 255; Exact(baseline);
                    var other = NativeMapDocument.CreateNewMap(new("Other", "Other", 8, 8, NativeFormat: native));
                    Reject(() => other.ApplyScenarioEventTypeChange(doc.PrepareScenarioEventTypeChange(record, type)));
                    Reject(() => other.ChangeScenarioEventType(record, type));
                    Reject(() => doc.ChangeScenarioEventType(record, LegacyEventSchemas.All.First(item => item.RecordSignature != source.Signature))); Exact(baseline);
                    // Remove the standalone test event; every later creation starts from the same live map.
                    doc.DeleteRecord(record);
                }
                Expect(creations == 103 && changes == pages.Sum(source => pages.Count(target => target.Signature == source.Signature) - 1), "All type transitions were not exercised.");
                var addType = LegacyEventSchemas.All.Single(item => item.Name == "ACTION_ADDCREATURE");
                var add = doc.AddScenarioEvent(0xD6, new(0, 0), addType);
                var addBytes = Snapshot(doc); var saved = doc.SaveCopy(Path.Combine(directory, native ? "native" : "project")); var reopened = NativeMapDocument.Load(saved);
                var loaded = reopened.Records().Single(item => EntityReferences.EventType(item)?.Name == "ACTION_ADDCREATURE");
                Expect(loaded.File.Data.Span.Slice(loaded.Offset, loaded.Length).SequenceEqual(add.File.Data.Span.Slice(add.Offset, add.Length)), "Creation defaults changed during save/reopen."); Exact(addBytes);
                if (!native)
                {
                    var compiled = doc.CompileLegacyProject("DefaultsCompiled", NativeMapDocument.Create(20, 20, "Basis"));
                    var packed = compiled.Records().Single(item => EntityReferences.EventType(item)?.Name == "ACTION_ADDCREATURE");
                    Expect(packed.File.Data.Span.Slice(packed.Offset + 8, 8).SequenceEqual(new byte[] { 4, 3, 1, 41, 1, 0, 1, 0 }), "Source creation defaults compiled to different literal native bytes.");
                }
                Console.WriteLine($"DEFAULTS DETAIL native={native} creations={creations} transitions={changes} source-defaults=70 save-zero=26");
            }
        }
        finally { Directory.Delete(directory, true); }

        static NativeRecord Current(NativeMapDocument doc, NativeRecord record) => doc.Records().Single(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length);
        static uint[] ReadLogical(NativeRecord record, Page page, bool native)
        {
            var result = new uint[8];
            if (!native) for (var number = 0; number < 8; number++) result[number] = BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[(record.Offset + 16 + number * 4)..]);
            else foreach (var field in page.Native.Where(field => field.Name.StartsWith("Param", StringComparison.Ordinal)))
            {
                var bytes = record.File.Data.Span[(record.Offset + field.Offset)..];
                result[int.Parse(field.Name[5..], CultureInfo.InvariantCulture) - 1] = field.Size == 1 ? bytes[0] : field.Size == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            }
            return result;
        }
        void WriteExpected(Dictionary<NativeFile, byte[]> expected, NativeRecord record, Page page, uint[] values, bool native, bool initial)
        {
            var bytes = expected[record.File];
            if (!native) for (var number = 0; number < 8; number++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(record.Offset + 16 + number * 4), values[number]);
            else
            {
                var payload = oracle.GetProperty("CompilerPadding").GetProperty(page.Type).EnumerateArray().Select(item => item.GetByte()).ToArray();
                foreach (var field in page.Native.Where(field => field.Name.StartsWith("Param", StringComparison.Ordinal)))
                {
                    var value = values[int.Parse(field.Name[5..], CultureInfo.InvariantCulture) - 1]; var offset = field.Offset - 8;
                    if (field.Size == 1) payload[offset] = unchecked((byte)value);
                    else if (field.Size == 2) BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(offset), unchecked((ushort)value));
                    else BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(offset), value);
                }
                if (page.Type == "ACTION_SHOWCRYSTAL") { BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), 5); BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), 6); }
                // Native GAMEGATE.bGateID is a 0/1 kind, not the project gate identity.
                // A project-only default ID must not be matched to that unrelated byte.
                if (page.Type == "ACTION_DESTROYGATE") { BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), 0); BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), 0); }
                payload.CopyTo(bytes, record.Offset + 8);
            }
            bytes[record.Offset + (native ? 22 : 54)] = page.ID;
            if (initial) bytes[record.Offset + (native ? 23 : 55)] = 1;
        }
    }
}
