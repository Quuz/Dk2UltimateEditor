using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioParameterDraftChecks
{
    private sealed record Operand(string Name, int Offset, int Size, string Kind);
    private sealed record Page(string Type, byte ID, uint Signature, int PageID, Operand[] Native);
    private sealed record ComparisonPage(string Type, int PageID, int[] Values, int[] Controls);
    private sealed record EnumPage(string Type, int PageID, int Parameter, int[] Values);
    private static readonly Page[] pages = ReadPages();
    private static readonly ComparisonPage[] comparisonPages = ReadComparisons();
    private static readonly EnumPage[] enumPages = ReadEnums();
    private static EnumPage[] ReadEnums()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioEnumPages.json")!;
        return JsonSerializer.Deserialize<EnumPage[]>(stream)!;
    }
    private static ComparisonPage[] ReadComparisons()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioComparisonPages.json")!;
        return JsonSerializer.Deserialize<ComparisonPage[]>(stream)!;
    }
    private static Page[] ReadPages()
    {
        using var stream = typeof(ScenarioParameterDraftChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.ScenarioParameterPages.json")!;
        return JsonSerializer.Deserialize<Page[]>(stream)!;
    }
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException or OverflowException) { return; }
        throw new InvalidOperationException("Invalid or stale parameter draft was accepted.");
    }
    private static byte[] Mutable(NativeFile file)
    {
        Expect(MemoryMarshal.TryGetArray(file.Data, out var data) && data.Offset == 0, "Independent buffer unavailable.");
        return data.Array!;
    }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(bytes.All(item => item.Key.Data.Span.SequenceEqual(item.Value)), "Unexpected event, graph, opaque or unrelated file write.");
    private static NativeRecord Add(NativeMapDocument document, bool native, Page page)
    {
        var type = LegacyEventSchemas.All.Single(item => item.Name == page.Type);
        Expect(type.Type == page.ID && type.RecordSignature == page.Signature, "Event identity differs from original literal fixture.");
        var kind = page.Signature == 213 ? "Trigger" : "Action";
        var record = native ? document.AddRecord(RecordTemplate.All.Single(item => item.Structure == kind), new(0, 0)) : document.AddLegacyRecord(kind, new(0, 0));
        document.SetEventType(record, type);
        record = document.Records().Single(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length);
        var start = native ? 8 : 16;
        var count = native ? 8 : 32;
        for (var offset = 0; offset < count; offset++) Mutable(record.File)[record.Offset + start + offset] = (byte)(0x93 ^ offset);
        Mutable(record.File)[record.Offset + (native ? 23 : 55)] = 255;
        return record;
    }
    private static Operand[] Fields(bool native, Page page) =>
        [.. page.PageID == -1 ? [] : native ? page.Native : Enumerable.Range(1, 8).Select(number => new Operand("param" + number, 16 + (number - 1) * 4, 4, "LONG")),
         .. page.Signature == 213 ? new[] { new Operand(native ? "Repeat" : "repeat", native ? 23 : 55, 1, "BYTE") } : []];
    private static string Read(byte[] bytes, int offset, Operand operand) => (operand.Kind switch
    {
        "BYTE" => bytes[offset], "WORD" => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset)),
        "LONG" => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset)), _ => throw new InvalidOperationException()
    }).ToString(CultureInfo.InvariantCulture);
    private static void Write(byte[] bytes, int offset, Operand operand, long value)
    {
        if (operand.Size == 1) bytes[offset] = checked((byte)value);
        else if (operand.Size == 2) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), checked((ushort)value));
        else BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), checked((int)value));
    }
    public static void Run()
    {
        Expect(pages.Length == 103 && pages.Count(page => page.PageID != -1) == 69, "Original parameter-page coverage incomplete.");
        Expect(comparisonPages.Length == 30 && comparisonPages.All(page => page.Values.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 })
            && page.Controls.SequenceEqual(new[] { 200, 201, 202, 203, 204, 205 })), "Original comparison radio fixture incomplete.");
        Expect(enumPages.Length == 20, "Original radio fixture incomplete.");
        Expect(LegacyEventSchemas.ParameterPageID("unknown") == -1, "Unknown page was invented.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2ScenarioParameters", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var native in new[] { false, true })
            {
                var document = NativeMapDocument.CreateNewMap(new("Parameters", "Parameters", 20, 20, NativeFormat: native));
                document.SetHistoryDepth(65535);
                document.SetScenarioName(ScenarioNameKind.Variable, 7, "Keep alias");
                foreach (var page in pages)
                {
                    Expect(LegacyEventSchemas.ParameterPageID(page.Type) == page.PageID, "Original page identity differs: " + page.Type);
                    var record = Add(document, native, page);
                    CheckComparisons(document, native, record, page);
                    CheckEnums(document, native, record, page);
                    var before = Snapshot(document); var revision = document.EditRevision; var history = document.GetHistory().Undo.Count;
                    if (page.PageID == -1 && page.Signature != 213)
                    {
                        Reject(() => document.CreateScenarioParameterDraft(record)); Exact(before);
                        continue;
                    }
                    var draft = document.CreateScenarioParameterDraft(record);
                    var expectedFields = Fields(native, page);
                    Expect(draft.Type == page.Type && draft.PageID == page.PageID && !draft.Changed, "Draft initialization differs from original page.");
                    Expect(draft.Fields.Count == expectedFields.Length, "Native/project parameter count differs: " + page.Type);
                    foreach (var operand in expectedFields)
                    {
                        var field = draft.Fields.Single(field => field.Name == operand.Name);
                        Expect(field.Offset == operand.Offset && field.Size == operand.Size && field.BitMask == 0, "Literal compiler parameter location differs: " + page.Type);
                        Expect(draft.Values[operand.Name] == Read(before[record.File], record.Offset + operand.Offset, operand), "Draft changed imported bits.");
                    }
                    document.ApplyScenarioParameterDraft(draft);
                    Expect(document.EditRevision == revision && document.GetHistory().Undo.Count == history, "Unchanged page created history."); Exact(before);
                    var values = expectedFields.Select((operand, index) => (Operand: operand, Value: operand.Kind switch
                    {
                        "BYTE" => (long)(index % 2 == 0 ? 0 : 255), "WORD" => index % 2 == 0 ? 65535L : 0L,
                        "LONG" => index % 2 == 0 ? int.MinValue : int.MaxValue, _ => throw new InvalidOperationException()
                    })).ToArray();
                    var changed = document.UpdateScenarioParameterDraft(draft, values.Select(item => new ScenarioParameterValue(item.Operand.Name, item.Value.ToString(CultureInfo.InvariantCulture))));
                    Expect(changed.Changed && !draft.Changed, "Draft update leaked into the original draft."); Exact(before);
                    var after = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                    foreach (var item in values) Write(after[record.File], record.Offset + item.Operand.Offset, item.Operand, item.Value);
                    document.ApplyScenarioParameterDraft(changed); Exact(after);
                    Expect(document.EditRevision == revision + 1 && document.GetHistory().Undo.Count == history + 1, "Whole page did not commit once.");
                    Reject(() => document.ApplyScenarioParameterDraft(draft));
                    document.Undo(); Exact(before); document.Redo(); Exact(after);
                    var noOp = document.CreateScenarioParameterDraft(record);
                    var hex = document.UpdateScenarioParameterDraft(noOp, values.Select(item => new ScenarioParameterValue(item.Operand.Name,
                        "0x" + (item.Operand.Size == 4 ? unchecked((uint)item.Value).ToString("X8") : item.Value.ToString("X")))));
                    Expect(!hex.Changed, "Equivalent numeric spellings changed bytes.");
                    history = document.GetHistory().Undo.Count; revision = document.EditRevision;
                    document.ApplyScenarioParameterDraft(hex); Exact(after);
                    Expect(document.GetHistory().Undo.Count == history && document.EditRevision == revision, "Equivalent values created history.");
                    Reject(() => document.UpdateScenarioParameterDraft(noOp, [new(expectedFields[0].Name, "0"), new(expectedFields[0].Name, "1")]));
                    Reject(() => document.UpdateScenarioParameterDraft(noOp, [new(native ? "ID" : "id", "9")]));
                    Reject(() => document.UpdateScenarioParameterDraft(noOp, [new(native ? "Type" : "ctrl", "9")]));
                    Reject(() => document.UpdateScenarioParameterDraft(noOp, [new(expectedFields[0].Name, "4294967296")])); Exact(after);
                    Expect(document.ReadScenarioNames().Slots.Single(slot => slot.Kind == ScenarioNameKind.Variable && slot.Index == 7).StoredName == "Keep alias", "Operand edit changed editor aliases.");
                }
                Guards(document, native);
                var output = document.SaveCopy(Path.Combine(directory, native ? "native" : "project"));
                var reopened = NativeMapDocument.Load(output);
                foreach (var page in pages.Where(page => page.PageID != -1 || page.Signature == 213))
                {
                    var oldRecord = document.Records().Single(record => EntityReferences.EventType(record)?.Name == page.Type);
                    var newRecord = reopened.Records().Single(record => EntityReferences.EventType(record)?.Name == page.Type);
                    var oldDraft = document.CreateScenarioParameterDraft(oldRecord); var newDraft = reopened.CreateScenarioParameterDraft(newRecord);
                    Expect(oldDraft.Values.Count == newDraft.Values.Count && oldDraft.Values.All(item => newDraft.Values[item.Key] == item.Value), "Page operands did not round-trip: " + page.Type);
                }
            }
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void Guards(NativeMapDocument document, bool native)
    {
        var record = document.Records().Single(row => EntityReferences.EventType(row)?.Name == "TRIGGER_TIMER");
        var draft = document.CreateScenarioParameterDraft(record); var before = Snapshot(document);
        if (native)
        {
            Expect(BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[4..]) == 4, "Native fixture does not use the original version-4 header.");
            BinaryPrimitives.WriteUInt32LittleEndian(Mutable(record.File).AsSpan(4), 99);
            Reject(() => document.CreateScenarioParameterDraft(record));
            Reject(() => document.ApplyScenarioParameterDraft(draft));
            Expect(ScenarioParameterChoices.ForField(record, draft.Fields[0]).Count == 0, "Future header received verified comparison choices.");
            BinaryPrimitives.WriteUInt32LittleEndian(Mutable(record.File).AsSpan(4), 4); Exact(before);
        }
        var foreign = NativeMapDocument.CreateNewMap(new("Foreign", "Foreign", 20, 20, NativeFormat: native));
        Reject(() => foreign.ApplyScenarioParameterDraft(draft));
        Reject(() => foreign.CreateScenarioParameterDraft(record)); Exact(before);
        var field = draft.Fields[0];
        document.SetField(record, field, "2"); document.Undo(); Exact(before);
        Reject(() => document.UpdateScenarioParameterDraft(draft, []));
        draft = document.CreateScenarioParameterDraft(record);
        var buffer = Mutable(record.File); var offset = record.Offset + field.Offset; var saved = buffer[offset]; buffer[offset] ^= 1;
        Reject(() => document.ApplyScenarioParameterDraft(draft)); buffer[offset] = saved; Exact(before);
        var change = document.UpdateScenarioParameterDraft(draft, [new(field.Name, "2")]);
        var attempted = false;
        EventHandler callback = (_, _) => { attempted = true; Reject(() => document.ApplyScenarioParameterDraft(change)); };
        document.Changed += callback;
        try { document.ApplyScenarioParameterDraft(change); Expect(attempted, "No actual edit notification."); }
        finally { document.Changed -= callback; }
        document.Undo(); Exact(before);
    }
    private static void CheckComparisons(NativeMapDocument document, bool native, NativeRecord record, Page page)
    {
        var comparison = comparisonPages.SingleOrDefault(item => item.Type == page.Type);
        var before = Snapshot(document);
        foreach (var operand in record.Fields)
        {
            var choices = ScenarioParameterChoices.ForField(record, operand);
            var expected = comparison is not null && operand.Name == (native ? "Param1" : "param1");
            var enumExpected = enumPages.SingleOrDefault(item => item.Type == page.Type && operand.Name == (native ? "Param" : "param") + item.Parameter);
            Expect(choices.Count == (expected ? 6 : enumExpected?.Values.Length ?? 0), "Choice routing differs from original source page: " + page.Type);
            if (!expected) continue;
            Expect(comparison!.PageID == page.PageID && choices.Select(choice => int.Parse(choice.Value, CultureInfo.InvariantCulture)).SequenceEqual(comparison.Values), "Source radio values became enum ordinals.");
            Expect(choices.All(choice => choice.English.Length > 0 && choice.Russian.Length > 0)
                && ValueCatalogues.ForField(record, operand).SequenceEqual(choices), "Ordinary property catalogue does not expose bilingual comparisons.");
            var literal = Fields(native, page).Single(item => item.Name == operand.Name);
            foreach (var value in comparison.Values)
            {
                var draft = document.CreateScenarioParameterDraft(record);
                var updated = document.UpdateScenarioParameterDraft(draft, [new(operand.Name, value.ToString(CultureInfo.InvariantCulture))]);
                var after = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                Write(after[record.File], record.Offset + literal.Offset, literal, value);
                document.ApplyScenarioParameterDraft(updated); Exact(after);
                document.Undo(); Exact(before);
            }
        }
        Exact(before);
    }
    private static void CheckEnums(NativeMapDocument document, bool native, NativeRecord record, Page page)
    {
        foreach (var source in enumPages.Where(item => item.Type == page.Type))
        {
            Expect(source.PageID == page.PageID, "Source enum page identity differs.");
            var name = (native ? "Param" : "param") + source.Parameter;
            var operand = Fields(native, page).Single(item => item.Name == name);
            var field = record.Fields.Single(item => item.Name == name);
            var choices = ScenarioParameterChoices.ForField(record, field);
            Expect(choices.Select(item => int.Parse(item.Value)).Order().SequenceEqual(source.Values)
                && choices.All(item => item.English.Length > 0 && item.Russian.Length > 0)
                && ValueCatalogues.ForField(record, field).SequenceEqual(choices), "Original radio encoding/catalogue differs: " + source.Type);
            var before = Snapshot(document);
            foreach (var value in source.Values)
            {
                var draft = document.CreateScenarioParameterDraft(record);
                var updated = document.UpdateScenarioParameterDraft(draft, [new(name, value.ToString(CultureInfo.InvariantCulture))]);
                var after = before.ToDictionary(item => item.Key, item => (byte[])item.Value.Clone());
                Write(after[record.File], record.Offset + operand.Offset, operand, value);
                document.ApplyScenarioParameterDraft(updated); Exact(after);
                document.Undo(); Exact(before);
            }
        }
    }
}
