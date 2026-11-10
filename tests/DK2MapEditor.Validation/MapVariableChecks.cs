using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class MapVariableChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes)
    { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Global variables changed unexpected bytes: " + file.ExportPath); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException) { return; }
        throw new Exception("Unsafe global variable edit was accepted.");
    }
    private static void Set(NativeMapDocument document, NativeRecord record, string field, uint value) => document.SetField(record, record.Fields.Single(item => item.Name == field), value.ToString(CultureInfo.InvariantCulture));
    private static uint Read(NativeRecord record, string field) => field == "dwGlobalHeader" ? BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[record.Offset..]) : uint.Parse(record.Read(record.Fields.Single(item => item.Name == field)), CultureInfo.InvariantCulture);
    private static MapVariableCell Cell(NativeMapDocument document, uint id) => document.MapVariableCells().Single(cell => cell.Definition.ID == id);
    public static void Run()
    {
        var definitions = MapVariables.Definitions;
        Expect(definitions.Count == 193 && definitions.Select(item => item.ID).Distinct().Count() == 193 && definitions.Select(item => item.Group).Distinct().Count() == 20, "Original variable catalogue/group coverage changed.");
        Expect(definitions.Count(item => !item.FixedPoint) == 189 && definitions.Where(item => item.FixedPoint).Select(item => item.ID).ToHashSet().SetEquals(new uint[] { 0x9B, 0x9E, 0xDD, 0xDC }), "Original four display-only variables changed.");
        Expect(definitions.All(item => item.ID is not (0 or 1 or 2 or 0x42 or 0x4D) && item.Minimum <= item.Maximum && item.Name.English.Length > 0 && item.Name.Russian.Length > 0), "Reserved variable types or untranslated names entered catalogue.");
        // Independent source anchors, including the intentional source-default
        // anomaly: torch red is 300 although manual input is limited to 255.
        foreach (var (id, value, minimum, maximum) in new (uint, int, int, int)[] { (4, 825, 1, 500000), (5, -140, -500000, 500000), (0x54, -1, -1, 500000), (0xDE, 200000, 0, 999999), (0x7E, 4, 1, 10), (0x1B, 10000, 0, 1000000), (0x34, 300, 0, 255), (0xD9, 1500, 0, 1500) })
        {
            var item = definitions.Single(item => item.ID == id);
            Expect(item.DefaultValue == value && item.Minimum == minimum && item.Maximum == maximum, "Source defaults/ranges changed for " + id);
        }
        var fixedPoint = definitions.Single(item => item.ID == 0x9E);
        Expect(fixedPoint.Display(0x1333) == "1.19995117" && fixedPoint.Display(4096) == "1.0" && fixedPoint.Display(uint.MaxValue) == "1048575.99975586", "20.12 unsigned display confused with float, sign or scaling.");
        Expect(definitions.Single(item => item.ID == 5).Display(unchecked((uint)-140)) == "-140", "Signed variable display changed.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2-VariableChecks-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var legacy in new[] { false, true })
            {
                var document = NativeMapDocument.CreateNewMap(new("Variables", "Variables", 20, 20, PlaceHearts: true, NativeFormat: !legacy));
                var before = Snapshot(document); var notifications = 0; document.Changed += (_, _) => notifications++;
                var cells = document.MapVariableCells(); Exact(document, before);
                Expect(cells.Count(cell => cell.Editable) == 189 && cells.Count(cell => cell.Resettable) == (legacy ? 193 : 189), "Variable edit/reset support changed.");
                Expect(cells.All(cell => legacy ? cell.Bits == cell.Definition.DefaultBits && cell.RecordCount == 1 : cell.Bits is null && cell.RecordCount == 0), "Missing native overrides were inferred or project defaults lost.");
                var changes = cells.Where(cell => cell.Editable).Select(cell => new MapVariableChange(cell.Definition.ID, cell.Definition.Minimum)).ToArray();
                var expected = before.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
                if (legacy)
                {
                    var info = document.Records().Single(record => record.File.Signature == NativeMapDocument.LegacyInfo);
                    foreach (var change in changes)
                    {
                        var field = info.Fields.Single(field => field.Name == $"Variables[{change.ID}]");
                        BinaryPrimitives.WriteInt32LittleEndian(expected[info.File].AsSpan(info.Offset + field.Offset), change.Value);
                    }
                }
                var count = notifications; document.SetMapVariables(changes); Expect(notifications == count + 1, "Variable batch was not one atomic operation.");
                foreach (var change in changes) Expect(Cell(document, change.ID).Bits == unchecked((uint)change.Value), "Wrong variable key/sign: " + change.ID);
                if (legacy) Exact(document, expected);
                else
                {
                    foreach (var file in document.Files.Where(file => file.Signature != 0xE6)) Expect(file.Data.Span.SequenceEqual(before[file]), "Variable edit changed another category.");
                    foreach (var change in changes)
                    {
                        var record = document.Records().Single(record => record.Category == "GAMEGLOBALVARIABLE" && Read(record, "dwGlobalHeader") == change.ID);
                        var bytes = new byte[16]; BinaryPrimitives.WriteUInt32LittleEndian(bytes, change.ID); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), change.Value);
                        Expect(record.File.Data.Span.Slice(record.Offset, record.Length).SequenceEqual(bytes), "Native variable encoding/reserved bytes wrong.");
                    }
                }
                var after = Snapshot(document); document.Undo(); Exact(document, before); document.Redo(); Exact(document, after);
                count = notifications; document.SetMapVariables(changes); Exact(document, after); Expect(notifications == count, "No-op batch consumed history.");
                foreach (var cell in cells.Where(cell => cell.Editable))
                {
                    var item = cell.Definition;
                    Reject(() => document.SetMapVariables([new(4, 123), new(item.ID == 4 ? 5u : item.ID, item.ID == 4 ? -500001 : item.Minimum - 1)]));
                    Reject(() => document.SetMapVariables([new(item.ID, item.Maximum + 1)]));
                }
                foreach (var id in new uint[] { 0, 1, 2, 0x42, 0x4D, 256, uint.MaxValue, 0x9B, 0x9E, 0xDC, 0xDD }) Reject(() => document.SetMapVariables([new(4, 900), new(id, 2)]));
                Reject(() => document.SetMapVariables([new(4, 900), new(4, 1000)]));
                Reject(() => document.ResetMapVariables([4, 1])); Exact(document, after);
                // Maximum endpoints are accepted, even if they are far outside
                // typical gameplay values. The original range is authoritative.
                changes = cells.Where(cell => cell.Editable).Select(cell => new MapVariableChange(cell.Definition.ID, cell.Definition.Maximum)).ToArray();
                document.SetMapVariables(changes);
                foreach (var change in changes) Expect(Cell(document, change.ID).Bits == unchecked((uint)change.Value), "Maximum endpoint rejected/wrapped.");
                after = Snapshot(document);
                var path = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory, "save-" + legacy)); var reopened = NativeMapDocument.Load(path);
                Expect(document.MapVariableCells().Select(cell => (cell.Definition.ID, cell.Bits, cell.RecordCount)).SequenceEqual(reopened.MapVariableCells().Select(cell => (cell.Definition.ID, cell.Bits, cell.RecordCount))), "Global variables changed after reopening.");
                var repeated = SaveProjectionChecks.SaveCopy(reopened, Path.Combine(directory, "repeat-" + legacy));
                if (legacy) Expect(SaveProjectionChecks.BeforeEquals(reopened,File.ReadAllBytes(path)), "Project variables not byte-exact on re-export.");
                else foreach (var file in reopened.Files) Expect(file.Data.Span.SequenceEqual(document.Files.Single(original => original.ExportPath == file.ExportPath).Data.Span), "Native variables not byte-exact on reopen.");
                Exact(document, after);
                if (legacy)
                {
                    var compiled = document.CompileLegacyProject("CompiledVariables", NativeMapDocument.Create(20, 20, "Basis"));
                    foreach (var cell in document.MapVariableCells())
                        Expect(Cell(compiled, cell.Definition.ID).Bits == (cell.Definition.FixedPoint || cell.Bits == cell.Definition.DefaultBits ? null : cell.Bits), "Source compiler variable omission/mapping changed: " + cell.Definition.ID);
                    Exact(document, after);
                    CheckProject(document, directory);
                }
                else { CheckSharedVariables(document, directory); CheckNative(document); }
                before = Snapshot(document); count = notifications;
                expected = before.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
                if (legacy)
                {
                    var info = document.Records().Single(record => record.File.Signature == NativeMapDocument.LegacyInfo);
                    foreach (var item in definitions)
                    {
                        var field = info.Fields.Single(field => field.Name == $"Variables[{item.ID}]");
                        BinaryPrimitives.WriteUInt32LittleEndian(expected[info.File].AsSpan(info.Offset + field.Offset), item.DefaultBits);
                    }
                }
                document.ResetMapVariables(document.MapVariableCells().Where(cell => cell.Resettable).Select(cell => cell.Definition.ID));
                if (legacy) Exact(document, expected);
                Expect(notifications == count + 1, "Full variable reset was not atomic.");
                foreach (var cell in document.MapVariableCells().Where(cell => cell.Resettable)) Expect(legacy ? cell.Bits == cell.Definition.DefaultBits : cell.Bits is null && cell.RecordCount == 0, "Reset lost original defaults/left duplicate overrides.");
                if (legacy) Expect(Cell(document, 0x34).Bits == 300, "Source default outside manual range was clamped.");
                after = Snapshot(document); document.Undo(); Exact(document, before); _ = document.MapVariableCells(); Expect(document.CanRedo, "Projection consumed redo."); document.Redo(); Exact(document, after);
                count = notifications; document.ResetMapVariables(document.MapVariableCells().Where(cell => cell.Resettable).Select(cell => cell.Definition.ID)); Exact(document, after); Expect(notifications == count, "No-op reset created history.");
            }
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void CheckSharedVariables(NativeMapDocument document, string directory)
    {
        var source = Snapshot(document);
        var path = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory, "shared-basis"));
        var mapVariables = document.Files.Single(file => file.Signature == 0xE6);
        var sharedBytes = new byte[72]; mapVariables.Data.Span[..56].CopyTo(sharedBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(sharedBytes.AsSpan(8), 72); BinaryPrimitives.WriteUInt32LittleEndian(sharedBytes.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(sharedBytes.AsSpan(52), 16); BinaryPrimitives.WriteUInt32LittleEndian(sharedBytes.AsSpan(56), 5);
        BinaryPrimitives.WriteInt32LittleEndian(sharedBytes.AsSpan(60), -55); BinaryPrimitives.WriteUInt32LittleEndian(sharedBytes.AsSpan(64), 0x87654321);
        var sharedPath = Path.Combine(directory, "shared-basis", "Data", "Editor", "GlobalVariables.kwd"); File.WriteAllBytes(sharedPath, sharedBytes);
        var loaded = NativeMapDocument.Load(path); var shared = loaded.Files.Single(file => Path.GetFileName(file.ExportPath) == "GlobalVariables.kwd");
        var before = Snapshot(loaded);
        Expect(Cell(loaded, 5).Bits == Cell(document, 5).Bits && Cell(loaded, 5).RecordCount == 1, "Shared installation variable was projected as a map override."); Exact(loaded, before);
        loaded.SetMapVariables([new(5, -2)]); Expect(shared.Data.Span.SequenceEqual(sharedBytes), "Map variable edit changed shared installation data.");
        loaded.ResetMapVariables([5]); Expect(Cell(loaded, 5) is { Bits: null, RecordCount: 0 } && shared.Data.Span.SequenceEqual(sharedBytes), "Map reset changed/inferred shared variables.");
        loaded.Undo(); loaded.Undo(); Exact(loaded, before);
        var repeated = SaveProjectionChecks.SaveCopy(loaded, Path.Combine(directory, "shared-repeat")); var reopened = NativeMapDocument.Load(repeated);
        Expect(reopened.Files.Single(file => Path.GetFileName(file.ExportPath) == "GlobalVariables.kwd").Data.Span.SequenceEqual(sharedBytes), "Untouched shared variable file was rewritten/scoped on export.");
        Exact(document, source);
    }
    private static void CheckNative(NativeMapDocument document)
    {
        var original = document.Records().Single(record => record.Category == "GAMEGLOBALVARIABLE" && Read(record, "dwGlobalHeader") == 5);
        var duplicate = document.DuplicateRecord(original); Set(document, duplicate, "dwGlobalData", unchecked((uint)-9));
        // Deliberately malformed/opaque fixture bytes are not an editor API.
        document.Edit("Opaque variable fixture", () =>
        {
            Expect(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(duplicate.File.Data, out var array), "Fixture buffer unavailable.");
            BinaryPrimitives.WriteUInt32LittleEndian(array.Array!.AsSpan(array.Offset + duplicate.Offset + 8), 0x12345678);
            BinaryPrimitives.WriteUInt32LittleEndian(array.Array.AsSpan(array.Offset + duplicate.Offset + 12), uint.MaxValue);
        });
        var unknown = document.AddRecord(new("GAMEGLOBALVARIABLE", 0xE6, uint.MaxValue, 16), new(0, 0));
        Set(document, unknown, "dwGlobalData", 0x87654321);
        var fractional = document.AddRecord(new("GAMEGLOBALVARIABLE", 0xE6, 0x9E, 16), new(0, 0));
        Set(document, fractional, "dwGlobalData", 0x1333);
        var before = Snapshot(document); var expected = before.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
        BinaryPrimitives.WriteInt32LittleEndian(expected[duplicate.File].AsSpan(duplicate.Offset + 4), -141);
        Expect(Cell(document, 5) is { Bits: unchecked((uint)-9), RecordCount: 2 }, "Duplicate native global variables are not last-wins."); Exact(document, before);
        document.SetMapVariables([new(5, -141)]); Exact(document, expected); document.Undo(); Exact(document, before); document.Redo();
        var unknownBytes = unknown.File.Data.Span.Slice(unknown.Offset, unknown.Length).ToArray(); var fractionalBytes = fractional.File.Data.Span.Slice(fractional.Offset, fractional.Length).ToArray();
        before = Snapshot(document); document.ClearMapVariableOverrides([5]); Expect(Cell(document, 5) is { Bits: null, RecordCount: 0 }, "Clear retained duplicate global variable.");
        document.Undo(); Exact(document, before); document.Redo();
        before = Snapshot(document); Reject(() => document.ClearMapVariableOverrides([4, 0x9E])); Reject(() => document.ResetMapVariables([4, 0x9E])); Exact(document, before);
        document.ResetMapVariables(document.MapVariableCells().Where(cell => cell.Resettable).Select(cell => cell.Definition.ID));
        var remaining = document.Records().Where(record => record.Category == "GAMEGLOBALVARIABLE").ToArray();
        Expect(remaining.Length == 2 && remaining.Single(record => Read(record, "dwGlobalHeader") == uint.MaxValue).File.Data.Span.Slice(remaining.Single(record => Read(record, "dwGlobalHeader") == uint.MaxValue).Offset, 16).SequenceEqual(unknownBytes), "Reset normalized/deleted an unknown record.");
        var fixedRecord = remaining.Single(record => Read(record, "dwGlobalHeader") == 0x9E);
        Expect(fixedRecord.File.Data.Span.Slice(fixedRecord.Offset, 16).SequenceEqual(fractionalBytes) && Cell(document, 0x9E) is { Editable: false, Resettable: false, Bits: 0x1333 }, "Native reset damaged rejected-by-source fixed-point record.");
        // Leave one integer override so the caller's full reset is meaningful.
        document.SetMapVariables([new(4, 999)]);
    }
    private static void CheckProject(NativeMapDocument document, string directory)
    {
        var info = document.Records().Single(record => record.File.Signature == NativeMapDocument.LegacyInfo);
        foreach (var id in new uint[] { 0x9B, 0x9E, 0xDC, 0xDD }) document.SetField(info, info.Fields.Single(field => field.Name == $"Variables[{id}]"), "12345");
        var unknown = info.Fields.Single(field => field.Name == "Variables[0]"); document.SetField(info, unknown, "-123456789");
        var before = Snapshot(document); Reject(() => document.ClearMapVariableOverrides([4])); Exact(document, before);
        var compiled = document.CompileLegacyProject("FractionalVariables", NativeMapDocument.Create(20, 20, "Basis"));
        Expect(compiled.MapVariableCells().Where(cell => cell.Definition.FixedPoint).All(cell => cell.Bits is null), "Compiler emitted original display-only fixed-point values."); Exact(document, before);
        document.ResetMapVariables(new uint[] { 0x9B, 0x9E, 0xDC, 0xDD });
        Expect(document.MapVariableCells().Where(cell => cell.Definition.FixedPoint).All(cell => cell.Bits == cell.Definition.DefaultBits), "Project reset did not restore four fixed-point defaults.");
        Expect(info.Read(unknown) == "-123456789", "Reset changed an unknown project array slot."); document.Undo(); Exact(document, before);
        var path = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory, "historical-basis")); var sections = LegacySectionFile.Read(path);
        foreach (var (version, structure) in new[] { (1, "MAPPROPERTIESV1"), (2, "MAPPROPERTIESV2"), (3, "MAPPROPERTIESV2"), (4, "MAPPROPERTIESV4"), (5, "MAPPROPERTIESV5"), (6, "MAPPROPERTIESV6") })
        {
            var old = sections.Sections.Select(section => section.Signature == NativeMapDocument.LegacyInfo ? new LegacySection(section.Signature, version, section.Payload[..LegacyProjectSchemas.Size(structure)]) : section).ToArray();
            var oldPath = Path.Combine(directory, "v" + version + ".dkm"); File.WriteAllBytes(oldPath, LegacySectionFile.Create(old).ToArray());
            var historical = NativeMapDocument.Load(oldPath); before = Snapshot(historical);
            var supported = historical.MapVariableCells().Where(cell => cell.Editable).ToArray();
            if (version <= 3)
            {
                Expect(supported.Length == 0 && historical.MapVariableCells().All(cell => !cell.Resettable && cell.Bits is null), "Old metadata without a variable array was guessed.");
                Reject(() => historical.SetMapVariables([new(5, -500000)])); Reject(() => historical.ResetMapVariables([5])); Exact(historical, before);
                historical.UpgradeLegacyProject();
                Expect(historical.MapVariableCells().Count(cell => cell.Editable) == 189 && historical.MapVariableCells().All(cell => cell.Bits == cell.Definition.DefaultBits), "Explicit old-project upgrade did not introduce original variable defaults.");
                historical.Undo(); Exact(historical, before); continue;
            }
            Expect(supported.Length == 189, "Verified historical variable array unnecessarily disabled: " + version);
            historical.SetMapVariables([new(5, -500000)]); Expect(Cell(historical, 5).Bits == unchecked((uint)-500000), "Historical signed array layout wrong."); historical.Undo(); Exact(historical, before);
        }
        var future = sections.Sections.Select(section => section.Signature == NativeMapDocument.LegacyInfo ? section with { Version = 99 } : section).ToArray();
        path = Path.Combine(directory, "future.dkm"); File.WriteAllBytes(path, LegacySectionFile.Create(future).ToArray());
        var unsupported = NativeMapDocument.Load(path); before = Snapshot(unsupported);
        Expect(unsupported.MapVariableCells().All(cell => !cell.Editable && !cell.Resettable && cell.Bits is null), "Unknown metadata version was guessed.");
        Reject(() => unsupported.SetMapVariables([new(4, 1)])); Reject(() => unsupported.ResetMapVariables([4])); Exact(unsupported, before);
    }
}
