using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioAliasEventChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException) { return; }
        throw new InvalidOperationException("Invalid/stale event alias edit accepted.");
    }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Alias event edit changed unrelated/opaque/graph bytes.");
    private static string Names(NativeMapDocument doc) => string.Join("\n", doc.ReadScenarioNames().Slots.Select(slot => $"{slot.Kind}:{slot.Index}:{slot.StoredName}"));
    private static byte[] Mutable(NativeFile file) { Expect(MemoryMarshal.TryGetArray(file.Data, out var segment) && segment.Offset == 0, "Mutable test buffer unavailable."); return segment.Array!; }
    private static void Operand(byte[] bytes, NativeRecord record, bool native, int parameter, uint value)
    {
        // Literal independent original ABI/compiler offsets, not Encode.
        var offset = record.Offset + (native ? parameter == 5 ? 12 : 7 + parameter : 16 + (parameter - 1) * 4);
        if (native && parameter != 5) bytes[offset] = checked((byte)value);
        else BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    }
    internal static (NativeMapDocument Document, NativeRecord Record) Fixture(bool native, string typeName)
    {
        var doc = NativeMapDocument.CreateNewMap(new("AliasEvent", "AliasEvent", 20, 20, NativeFormat: native));
        var type = LegacyEventSchemas.All.Single(type => type.Name == typeName);
        var record = native ? doc.AddRecord(RecordTemplate.All.Single(template => template.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0))
            : doc.AddLegacyRecord(type.RecordSignature == 0xD5 ? "Trigger" : "Action", new(0, 0));
        doc.SetEventType(record, type); record = doc.Records().Single(row => row.File == record.File && row.Offset == record.Offset && row.Length == record.Length);
        var seed = typeName switch
        {
            "TRIGGER_VARIABLE" => new Dictionary<int, int> { [1] = 3, [2] = 7, [3] = 1, [4] = 0, [5] = 42 },
            "TRIGGER_TIMER" => new Dictionary<int, int> { [1] = 3, [2] = 7, [5] = 3661 },
            "ACTION_SETVARIABLE" => new Dictionary<int, int> { [1] = 7, [2] = 0x0A, [5] = 42 },
            _ => new Dictionary<int, int> { [1] = 7 }
        };
        foreach (var value in seed) doc.SetField(record, record.Fields.Single(field => field.Name == (native ? "Param" : "param") + value.Key), value.Value.ToString(CultureInfo.InvariantCulture));
        doc.SetScenarioName(typeName is "TRIGGER_VARIABLE" or "ACTION_SETVARIABLE" ? ScenarioNameKind.Variable : ScenarioNameKind.Timer, 7, "Keep");
        return (doc, record);
    }
    private static ScenarioEventParameter[] Parameters(ScenarioAliasEventDraft draft, params (int Key, string Value)[] changes)
    {
        var values = draft.Values.ToDictionary(value => value.Key, value => value.Value);
        foreach (var change in changes) values[change.Key] = change.Value;
        return values.Select(value => new ScenarioEventParameter(value.Key, value.Value)).ToArray();
    }
    public static void Run()
    {
        CheckSecondaryDefaults();
        var directory = Path.Combine(Path.GetTempPath(), "DK2ScenarioAliasEvents", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        foreach (var native in new[] { false, true }) foreach (var type in new[] { "TRIGGER_VARIABLE", "TRIGGER_TIMER", "ACTION_SETVARIABLE", "ACTION_SETTIMER" }) { Check(native, type, directory); Imported(native, type, directory); CheckParentComposition(native, type, directory); }
    }
    private static void CheckSecondaryDefaults()
    {
        using var fixture=System.Text.Json.JsonDocument.Parse(typeof(ScenarioAliasEventChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioTerrainOwnersSource.json")!);
        var source=fixture.RootElement.GetProperty("AliasSecondaryDefaults");
        var literals=source.GetProperty("LiteralModes").EnumerateArray().Select(value=>value.GetInt32()).ToArray();
        var variables=source.GetProperty("VariableModes").EnumerateArray().Select(value=>value.GetInt32()).ToArray();
        Expect(source.GetProperty("DefaultSlot").GetInt32()==0&&source.GetProperty("NumericTextKeepsParam5").GetBoolean(),"Source secondary initialization capture differs.");
        foreach(var native in new[]{false,true})
        {
            var (doc,record)=Fixture(native,"ACTION_SETVARIABLE");var checkedCases=0;
            foreach(var operation in literals.Concat(variables).Append(255))
            foreach(var value in new uint[]{0,1,42,128,255,uint.MaxValue})
            {
                Operand(Mutable(record.File),record,native,2,(uint)operation);Operand(Mutable(record.File),record,native,5,value);
                var baseline=doc.CreateScenarioAliasEventDraft(record);var before=Snapshot(doc);var names=Names(doc);var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
                var expected=variables.Contains(operation)?value.ToString(CultureInfo.InvariantCulture):"0";
                Expect(baseline.InitialSecondaryID==expected&&baseline.Values[5]==value.ToString(CultureInfo.InvariantCulture),"Literal number became a hidden variable ID or a stored variable ID was lost.");
                doc.ApplyScenarioAliasEvent(doc.UpdateScenarioAliasEvent(baseline,Parameters(baseline),[]));Exact(before);
                Expect(Names(doc)==names&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history,"Opening/alias no-op materialized a hidden default.");
                // Model the actual mode switch using its initialized selector;
                // the inactive literal remains cached outside the game operand.
                foreach(var variableMode in variables)
                {
                    var inputs=Parameters(baseline).Select(input=>input.Number==2?new ScenarioEventParameter(2,variableMode.ToString(CultureInfo.InvariantCulture))
                        :input.Number==5?new ScenarioEventParameter(5,expected):input).ToArray();
                    var selected=doc.UpdateScenarioAliasEvent(baseline,inputs,[]);Exact(before);
                    Expect(selected.Values[5]==expected&&baseline.Values[5]==value.ToString(CultureInfo.InvariantCulture),"Switch mutated cached number/baseline or chose the numeric value as a slot.");
                }
                checkedCases++;
            }
            Expect(checkedCases==42,"Incomplete six-mode/unknown secondary default scope.");
            Console.WriteLine($"ALIAS SECONDARY DETAIL native={native} six source modes plus unknown x six imported values: literal hidden slot0, numeric bits retained, variable IDs preserved and no-op exact");
        }
    }
    private static void Imported(bool native, string type, string directory)
    {
        var (doc, record) = Fixture(native, type); var draft = doc.CreateScenarioAliasEventDraft(record);
        var unknown = type == "ACTION_SETTIMER" ? 16 : 255;
        doc.SetField(record, record.Fields.Single(field => field.Name == (native ? "Param" : "param") + draft.PrimaryParameter), unknown.ToString());
        draft = doc.CreateScenarioAliasEventDraft(record); var before = Snapshot(doc); var names = Names(doc); var history = doc.GetHistory().Undo.Count;
        doc.ApplyScenarioAliasEvent(doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [])); Exact(before);
        Expect(Names(doc) == names && doc.GetHistory().Undo.Count == history, "Unknown imported operand was normalized.");
        Reject(() => doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [new(unknown, "Invented")])); Exact(before);
        doc.Undo(); record = doc.Records().Single(row => row.File == record.File && row.Offset == record.Offset && row.Length == record.Length);
        var kind = type is "TRIGGER_VARIABLE" or "ACTION_SETVARIABLE" ? ScenarioNameKind.Variable : ScenarioNameKind.Timer;
        var input = doc.SaveCopy(Path.Combine(directory, $"imported-{native}-{type}"));
        var slot = kind == ScenarioNameKind.Variable ? 7 : 129 + 7;
        if (native)
        {
            var path = input + NativeMapDocument.ScenarioNamesExtension; var bytes = File.ReadAllBytes(path); bytes.AsSpan(12 + slot * 64, 64).Fill((byte)'A'); File.WriteAllBytes(path, bytes);
            doc = NativeMapDocument.Load(input); record = doc.Records().Single(ScriptGraph.IsEvent);
        }
        else
        {
            var file = doc.Files.Single(file => file.Signature == NativeMapDocument.LegacyEvents); Mutable(file).AsSpan(file.Length - 146 * 64 + slot * 64, 64).Fill((byte)'A');
        }
        draft = doc.CreateScenarioAliasEventDraft(record); before = Snapshot(doc); history = doc.GetHistory().Undo.Count;
        doc.ApplyScenarioAliasEvent(doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [new(7, new string('A', 64))])); Exact(before);
        Expect(doc.GetHistory().Undo.Count == history && draft.Name(7).Length == 64, "Unchanged nonterminated imported alias was rejected or rewritten.");
        var output = doc.SaveCopy(Path.Combine(directory, $"imported-saved-{native}-{type}")); var reopened = NativeMapDocument.Load(output);
        Expect(reopened.CreateScenarioAliasEventDraft(reopened.Records().Single(ScriptGraph.IsEvent)).Name(7) == new string('A', 64), "Opaque imported alias did not round-trip.");
        if (native)
        {
            Expect(File.ReadAllBytes(input + NativeMapDocument.ScenarioNamesExtension).AsSpan().SequenceEqual(File.ReadAllBytes(output + NativeMapDocument.ScenarioNamesExtension)), "No-op changed raw companion bytes.");
            var bytes = File.ReadAllBytes(input + NativeMapDocument.ScenarioNamesExtension); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 99); File.WriteAllBytes(input + NativeMapDocument.ScenarioNamesExtension, bytes);
            var future = NativeMapDocument.Load(input); Reject(() => future.CreateScenarioAliasEventDraft(future.Records().Single(ScriptGraph.IsEvent)));
        }
    }
    private static void Check(bool native, string type, string directory)
    {
        var (doc, record) = Fixture(native, type); var original = Snapshot(doc); var originalNames = Names(doc);
        var draft = doc.CreateScenarioAliasEventDraft(record); var history = doc.GetHistory().Undo.Count;
        doc.ApplyScenarioAliasEvent(doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [])); Exact(original);
        Expect(Names(doc) == originalNames && doc.GetHistory().Undo.Count == history, "No-op alias page consumed history.");
        for (var id = 0; id <= draft.MaximumPrimaryID; id++)
        {
            draft = doc.CreateScenarioAliasEventDraft(record); var label = "Named " + id;
            var prepared = doc.UpdateScenarioAliasEvent(draft, Parameters(draft, (draft.PrimaryParameter, id.ToString())), [new(id, label)]);
            Exact(original); Expect(Names(doc) == originalNames, "Private alias draft mutated source.");
            var expected = original.ToDictionary(row => row.Key, row => row.Value.ToArray()); Operand(expected[record.File], record, native, draft.PrimaryParameter, (uint)id);
            if (!native)
            {
                var file = doc.Files.Single(file => file.Signature == NativeMapDocument.LegacyEvents);
                var offset = file.Length - 146 * 64 + (draft.Kind == ScenarioNameKind.Variable ? id : 129 + id) * 64;
                expected[file].AsSpan(offset, 64).Clear(); Encoding.ASCII.GetBytes(label).CopyTo(expected[file], offset);
            }
            doc.ApplyScenarioAliasEvent(prepared); Exact(expected);
            Expect(doc.GetHistory().Undo.Count == history + 1 && doc.ReadScenarioNames().Slots.Single(slot => slot.Kind == draft.Kind && slot.Index == id).StoredName == label, "Operand+alias was not one source-ID transaction.");
            var edited = Snapshot(doc); var editedNames = Names(doc); doc.Undo(); Exact(original); Expect(Names(doc) == originalNames, "Undo failed to restore editor alias metadata."); doc.Redo(); Exact(edited); Expect(Names(doc) == editedNames, "Redo lost alias metadata."); doc.Undo();
        }
        draft = doc.CreateScenarioAliasEventDraft(record);
        Reject(() => doc.UpdateScenarioAliasEvent(draft, Parameters(draft, (draft.PrimaryParameter, (draft.MaximumPrimaryID + 1).ToString())), []));
        Reject(() => doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [new(7, "One"), new(7, "Two")]));
        Reject(() => doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [new(8, "Unselected")]));
        Reject(() => doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [new(7, "Кириллица")]));
        Reject(() => doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [new(7, new string('A', 64))]));
        doc.ApplyScenarioAliasEvent(doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [new(7, "")])); Exact(original); Expect(Names(doc) == originalNames, "Empty inline text cleared an existing alias.");
        var cp1252 = "Café € " + new string('A', 56); Expect(Encoding.GetEncoding(1252).GetByteCount(cp1252) == 63, "Wrong independent name limit fixture.");
        doc.ApplyScenarioAliasEvent(doc.UpdateScenarioAliasEvent(draft, Parameters(draft), [new(7, cp1252)]));
        var saved = doc.SaveCopy(Path.Combine(directory, $"saved-{native}-{type}")); var reopened = NativeMapDocument.Load(saved);
        Expect(Names(reopened) == Names(doc) && reopened.CreateScenarioAliasEventDraft(reopened.Records().Single(row => row.Category == record.Category && ScriptGraph.IsEvent(row))).Values.SequenceEqual(doc.CreateScenarioAliasEventDraft(record).Values), "Operand+alias save/reopen changed values.");
        doc.Undo(); Exact(original);
        if (type.StartsWith("TRIGGER_", StringComparison.Ordinal)) foreach (var comparison in Enumerable.Range(1, 6))
        {
            draft = doc.CreateScenarioAliasEventDraft(record); doc.ApplyScenarioAliasEvent(doc.UpdateScenarioAliasEvent(draft, Parameters(draft, (1, comparison.ToString()), (5, "4294967295")), []));
            var expected = original.ToDictionary(row => row.Key, row => row.Value.ToArray()); Operand(expected[record.File], record, native, 1, (uint)comparison); Operand(expected[record.File], record, native, 5, uint.MaxValue); Exact(expected); doc.Undo(); Exact(original);
        }
        if (type == "TRIGGER_VARIABLE")
        {
            draft = doc.CreateScenarioAliasEventDraft(record); doc.ApplyScenarioAliasEvent(doc.UpdateScenarioAliasEvent(draft, Parameters(draft, (3, "2"), (4, "128")), []));
            var expected = original.ToDictionary(row => row.Key, row => row.Value.ToArray()); Operand(expected[record.File], record, native, 3, 2); Operand(expected[record.File], record, native, 4, 128); Operand(expected[record.File], record, native, 5, 0); Exact(expected); doc.Undo();
        }
        if (type == "ACTION_SETVARIABLE") foreach (var mode in new[] { 0x0A, 0x12, 0x22, 0x0C, 0x14, 0x24 })
        {
            draft = doc.CreateScenarioAliasEventDraft(record); var secondary = mode is 0x0C or 0x14 or 0x24;
            var plan = doc.UpdateScenarioAliasEvent(draft, Parameters(draft, (2, mode.ToString()), (5, secondary ? "8" : "4294967295")), secondary ? [new(7, "Destination"), new(8, "Source")] : []);
            doc.ApplyScenarioAliasEvent(plan); Expect(plan.SecondaryAliasActive == secondary, "Source variable modes differ.");
            var expected = original.ToDictionary(row => row.Key, row => row.Value.ToArray()); Operand(expected[record.File], record, native, 2, (uint)mode); Operand(expected[record.File], record, native, 5, secondary ? 8u : uint.MaxValue);
            if (!native && secondary) foreach (var alias in new[] { (7, "Destination"), (8, "Source") })
            {
                var offset = record.File.Length - 146 * 64 + alias.Item1 * 64; expected[record.File].AsSpan(offset, 64).Clear(); Encoding.ASCII.GetBytes(alias.Item2).CopyTo(expected[record.File], offset);
            }
            Exact(expected); doc.Undo(); Exact(original); Expect(Names(doc) == originalNames, "Mode Undo lost aliases.");
        }
        draft = doc.CreateScenarioAliasEventDraft(record); doc.SetScenarioName(draft.Kind, 6, "External"); doc.Undo(); Reject(() => doc.ApplyScenarioAliasEvent(draft));
        draft = doc.CreateScenarioAliasEventDraft(record); var raw = Mutable(doc.MainFile); var last = raw.Length - 1; raw[last] ^= 1; Reject(() => doc.ApplyScenarioAliasEvent(draft)); raw[last] ^= 1;
        var foreign = Fixture(native, type).Document; Reject(() => foreign.ApplyScenarioAliasEvent(doc.CreateScenarioAliasEventDraft(record)));
        Exact(original); Expect(Names(doc) == originalNames, "Rejected draft changed aliases.");
    }
}
