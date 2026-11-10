using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class ScenarioNameChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action action, string message)
    {
        var rejected = false;
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { rejected = true; }
        Expect(rejected, message);
    }
    private static ushort Add(NativeMapDocument doc, string name, params (int Parameter, int Value)[] values)
    {
        var type = LegacyEventSchemas.All.Single(type => type.Name == name);
        var record = doc.IsLegacyProject ? doc.AddLegacyRecord(type.RecordSignature == 0xD5 ? "Trigger" : "Action", new(0, 0))
            : doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0));
        doc.SetEventType(record, type);
        record = doc.Records().Single(current => current.File == record.File && current.Offset == record.Offset && current.Length == record.Length);
        foreach (var (parameter, value) in values)
        {
            var field = record.Fields.Single(field => field.Name == (doc.IsLegacyProject ? "param" : "Param") + parameter);
            doc.SetField(record, field, value.ToString());
        }
        return ScriptGraph.ID(record);
    }
    private static ScenarioNameSlot Row(NativeMapDocument doc, ScenarioNameKind kind, int index) => doc.ReadScenarioNames().Slots.Single(row => row.Kind == kind && row.Index == index);
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2ScenarioNames", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        foreach (var native in new[] { false, true }) Check(native, directory);
        Historical(directory); OpaqueMetadata(directory); CompiledAliases(directory);
    }
    public static string ReadOnlyProjection(NativeMapDocument document)
    {
        var snapshot = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var revision = document.EditRevision; var dirty = document.HasChanges; var history = document.GetHistory();
        var table = document.ReadScenarioNames();
        var choices = document.Records().Where(ScriptGraph.IsEvent).SelectMany(record => record.Fields
            .Select(field => new { ID = ScriptGraph.ID(record), Field = field.Name, Choices = document.ScenarioNameChoices(record, field) })
            .Where(row => row.Choices.Count != 0)).ToArray();
        Expect(revision == document.EditRevision && dirty == document.HasChanges
            && history.Undo.Count == document.GetHistory().Undo.Count && history.Redo.Count == document.GetHistory().Redo.Count
            && snapshot.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Scenario-name projection changed source bytes/history.");
        return System.Text.Json.JsonSerializer.Serialize(new { Table = table, Choices = choices });
    }
    private static void Check(bool native, string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("Names", "Names", 24, 24, PlaceHearts: true, NativeFormat: native));
        var variable = Add(doc, "TRIGGER_VARIABLE", (2, 7), (3, 2), (4, 7));
        Add(doc, "TRIGGER_VARIABLE", (2, 128), (3, 1), (4, 7));
        Add(doc, "ACTION_SETVARIABLE", (1, 7), (2, 0x0C), (5, 7));
        var arithmetic = Add(doc, "ACTION_SETVARIABLE", (1, 7), (2, 0x14), (5, 7));
        var conditionTimer = Add(doc, "TRIGGER_TIMER", (2, 16));
        var setTimer = Add(doc, "ACTION_SETTIMER", (1, 3)); Add(doc, "ACTION_DISPLAYTIMER", (1, 3)); Add(doc, "TRIGGER_TIMER", (2, 3));
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var revision = doc.EditRevision; var history = doc.GetHistory();
        var table = doc.ReadScenarioNames();
        Expect(table.Supported && table.ReferencesVerified && table.Slots.Count == 146 && table.Slots.All(row => row.StoredName == ""), "Alias table shape/defaults incorrect.");
        Expect(Row(doc, ScenarioNameKind.Variable, 7).References == 5 && Row(doc, ScenarioNameKind.Variable, 128).References == 1
            && Row(doc, ScenarioNameKind.Timer, 3).References == 2 && Row(doc, ScenarioNameKind.Timer, 16).References == 1, "Original all-event operand counts differ.");
        Expect(revision == doc.EditRevision && history.Undo.Count == doc.GetHistory().Undo.Count && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Reading aliases changed bytes/history.");
        doc.Edit("Fixture alias group", () =>
        {
            foreach (var row in table.Slots) doc.SetScenarioName(row.Kind, row.Index, $"{row.Kind}-{row.Index}");
        });
        Expect(doc.GetHistory().Undo.Count == history.Undo.Count + 1 && doc.GetHistory().Undo[^1].Children.Count == 146, "Aliases not one nested reversible operation.");
        var named = doc.ReadScenarioNames(); Expect(named.Slots.All(row => row.StoredName == $"{row.Kind}-{row.Index}"), "An alias slot/order was lost.");
        if (native) Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Editor aliases modified native game payloads.");
        else
        {
            var file = doc.Files.Single(file => file.Signature == NativeMapDocument.LegacyEvents);
            var expected = (byte[])before[file].Clone(); var baseOffset = file.Length - 146 * 64;
            foreach (var row in table.Slots)
            {
                var slot = row.Kind == ScenarioNameKind.Variable ? row.Index : 129 + row.Index;
                var text = System.Text.Encoding.ASCII.GetBytes($"{row.Kind}-{row.Index}"); text.CopyTo(expected, baseOffset + slot * 64);
            }
            Expect(file.Data.Span.SequenceEqual(expected) && before.Where(pair => pair.Key != file).All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Project alias writes changed unrelated bytes.");
        }
        var current = doc.EditRevision; doc.SetScenarioName(ScenarioNameKind.Variable, 7, "Variable-7");
        Expect(doc.EditRevision == current && doc.GetHistory().Undo.Count == history.Undo.Count + 1, "Same name consumed history.");
        var draft = doc.CreateScenarioNameDraft(ScenarioNameKind.Variable, 7);
        var foreign = NativeMapDocument.Create(24, 24, "ForeignNames");
        Reject(() => foreign.ApplyScenarioNameDraft(draft, "Wrong"), "Foreign rename authority accepted.");
        doc.Edit("No-op fixture", () => { }); doc.ApplyScenarioNameDraft(draft, "Variable-7");
        doc.Paint([new(20, 20)], new(4, 2, 1)); doc.Undo();
        Reject(() => doc.ApplyScenarioNameDraft(draft, "Stale"), "Edit+Undo reused rename authority.");
        doc.Undo(); Expect(doc.ReadScenarioNames().Slots.All(row => row.StoredName == "") && before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Alias Undo not exact.");
        doc.Redo(); Expect(doc.ReadScenarioNames().Slots.SequenceEqual(named.Slots), "Alias Redo not exact.");
        var graph = ScriptGraph.Build(doc);
        IReadOnlyList<ValueChoice> Choices(ushort id, int parameter)
        { var record = graph.Events[id].Record; return doc.ScenarioNameChoices(record, record.Fields.Single(field => field.Name == (native ? "Param" : "param") + parameter)); }
        Expect(Choices(variable, 2).Count == 129 && Choices(variable, 4)[7].English == "Variable-7" && Choices(variable, 2)[128].English == "Score", "Variable/special named choices incorrect.");
        Expect(Choices(conditionTimer, 2).Count == 17 && Choices(conditionTimer, 2)[16].Russian == "Лимит времени" && Choices(setTimer, 1).Count == 16
            && Choices(arithmetic, 5).Count == 129, "Timer/special/arithmetic operand lists differ from source.");
        Reject(() => doc.SetScenarioName(ScenarioNameKind.Variable, 129, "bad"), "Invalid variable index accepted.");
        Reject(() => doc.SetScenarioName((ScenarioNameKind)99, 0, "bad"), "Unknown kind accepted.");
        foreach (var bad in new[] { new string('x', 64), "Имя", "A\0B" }) Reject(() => doc.SetScenarioName(ScenarioNameKind.Timer, 0, bad), "Unrepresentable alias accepted.");
        var longName = new string('é', 63); doc.SetScenarioName(ScenarioNameKind.Timer, 0, longName);
        Expect(Row(doc, ScenarioNameKind.Timer, 0).StoredName == longName, "63-byte CP1252 boundary rejected."); doc.Undo();
        var preview = doc.PlanUnusedScriptRemoval(); doc.SetScenarioName(ScenarioNameKind.Timer, 0, "Changed"); doc.Undo();
        Expect(!doc.IsUnusedScriptPlanCurrent(preview), "Alias edit+Undo reused scenario cleanup consent.");
        var saved = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, "names-" + native)); var reopened = NativeMapDocument.Load(saved);
        Expect(reopened.ReadScenarioNames().Slots.SequenceEqual(named.Slots) && !reopened.HasChanges, "Saved/reopened aliases or baseline changed.");
        if (native)
        {
            var bytes = File.ReadAllBytes(saved + NativeMapDocument.ScenarioNamesExtension);
            Expect(bytes.Length == 9356 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x4E533244 && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)) == 1, "Native companion header invalid.");
            Expect(reopened.Files.Count == doc.Files.Count && reopened.Files.All(file => file.Signature != 0x4E533244), "Editor metadata pretends to be a game file.");
            reopened.SetScenarioName(ScenarioNameKind.Variable, 7, "Changed"); Expect(reopened.HasChanges, "Metadata-only edit not marked dirty."); reopened.Undo(); Expect(!reopened.HasChanges, "Metadata Undo not restored clean baseline.");
            var clean = NativeMapDocument.CreateNewMap(new("BlankNames", "BlankNames", 24, 24, NativeFormat: true));
            var emptySaved = SaveProjectionChecks.SaveCopy(clean, Path.Combine(directory, "empty-names")); Expect(!File.Exists(emptySaved + NativeMapDocument.ScenarioNamesExtension), "Unchanged empty names wrote an unnecessary companion.");
        }
        // Nested failure must roll back both alias and ordinary game mutations,
        // retaining prior redo; zero history disables tracking, not metadata edits.
        doc.Undo(); var originalTable = doc.ReadScenarioNames(); var oldTile = doc.GetTile(20, 20); var oldHistory = doc.GetHistory();
        Reject(() => doc.Edit("Failing fixture", () => { doc.SetScenarioName(ScenarioNameKind.Timer, 2, "Rollback"); doc.Paint([new(20, 20)], new(4, 2, 1)); throw new InvalidOperationException("rollback"); }), "Failure did not propagate.");
        Expect(doc.ReadScenarioNames().Slots.SequenceEqual(originalTable.Slots) && doc.GetTile(20, 20) == oldTile && doc.GetHistory().Redo.Count == oldHistory.Redo.Count, "Rollback lost metadata/game/redo.");
        doc.SetHistoryDepth(0); doc.SetScenarioName(ScenarioNameKind.Timer, 2, "NoHistory"); Expect(!doc.CanUndo && Row(doc, ScenarioNameKind.Timer, 2).StoredName == "NoHistory", "Zero-history rename lost edit.");
    }
    private static void Historical(string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("HistoricalNames", "HistoricalNames", 24, 24)); doc.SetScenarioName(ScenarioNameKind.Timer, 16, "Reserved timer alias");
        var path = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, "historical-source")); var project = LegacySectionFile.Read(path);
        foreach (var version in new[] { 1, 2, 8 })
        {
            var section = project.Sections.Single(section => section.Signature == NativeMapDocument.LegacyEvents);
            var replacement = new LegacySection(section.Signature, version, version == 1 ? new byte[4] : section.Payload);
            var bytes = LegacySectionFile.Create(project.Sections.Select(item => item == section ? replacement : item)).ToArray();
            var fixture = Path.Combine(directory, "alias-version-" + version + ".dkm"); File.WriteAllBytes(fixture, bytes); var loaded = NativeMapDocument.Load(fixture);
            Expect(loaded.CanEditScenarioNames == (version == 2), "Unknown/historical scenario-name version guessed.");
            if (version == 2) Expect(Row(loaded, ScenarioNameKind.Timer, 16).StoredName == "Reserved timer alias", "V2 raw special alias lost.");
            else Reject(() => loaded.SetScenarioName(ScenarioNameKind.Timer, 0, "bad"), "Opaque aliases editable.");
            if (version == 1)
            {
                loaded.UpgradeLegacyProject(); Expect(loaded.ReadScenarioNames().Slots.Count == 146, "V1 upgrade did not introduce default aliases.");
                loaded.Undo(); Expect(!loaded.CanEditScenarioNames && SaveProjectionChecks.SaveCopy(loaded, Path.Combine(directory, "v1-restored")) is { } restored && SaveProjectionChecks.BeforeEquals(loaded,bytes), "V1 upgrade Undo changed original bytes.");
            }
        }
    }
    private static void OpaqueMetadata(string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("OpaqueNames", "OpaqueNames", 24, 24, NativeFormat: true)); var path = SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory, "opaque-source"));
        var bytes = new byte[31]; BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0x4E533244); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 9); bytes[^1] = 0xCD;
        File.WriteAllBytes(path + NativeMapDocument.ScenarioNamesExtension, bytes); var loaded = NativeMapDocument.Load(path);
        Expect(!loaded.CanEditScenarioNames && !loaded.HasChanges, "Future metadata guessed or marked changed."); Reject(() => loaded.SetScenarioName(ScenarioNameKind.Timer, 0, "bad"), "Future metadata modified.");
        var exported = SaveProjectionChecks.SaveCopy(loaded, Path.Combine(directory, "opaque-export")); Expect(File.ReadAllBytes(exported + NativeMapDocument.ScenarioNamesExtension).AsSpan().SequenceEqual(bytes), "Opaque metadata not byte-exact exported.");
        File.WriteAllBytes(path + NativeMapDocument.ScenarioNamesExtension, [1, 2, 3]);
        var rejected = false; try { NativeMapDocument.Load(path); } catch (InvalidDataException) { rejected = true; } Expect(rejected, "Truncated metadata header accepted.");
    }
    private static void CompiledAliases(string directory)
    {
        var project = NativeMapDocument.CreateNewMap(new("CompileNames", "CompileNames", 24, 24)); project.SetScenarioName(ScenarioNameKind.Variable, 3, "Kept name");
        var record = project.Records().Where(record => record.Category == "Project.VariableNames").ElementAt(3);
        Expect(MemoryMarshal.TryGetArray(record.File.Data, out var backing), "No synthetic alias backing array."); backing.Array![backing.Offset + record.Offset + 63] = 0xAB;
        var original = record.File.Data.Slice(record.Offset, 64).ToArray(); var compiled = project.CompileLegacyProject("CompiledNames", NativeMapDocument.Create(24, 24, "Base"));
        var path = SaveProjectionChecks.SaveCopy(compiled, Path.Combine(directory, "compiled-names")); var companion = File.ReadAllBytes(path + NativeMapDocument.ScenarioNamesExtension);
        Expect(companion.AsSpan(12 + 3 * 64, 64).SequenceEqual(original) && NativeMapDocument.Load(path).ReadScenarioNames().Slots.Single(row => row.Kind == ScenarioNameKind.Variable && row.Index == 3).StoredName == "Kept name", "Compilation normalized/discarded raw aliases.");
        var revision = project.EditRevision; project.SetScenarioName(ScenarioNameKind.Variable, 3, "Kept name"); Expect(project.EditRevision == revision && record.File.Data.Slice(record.Offset, 64).Span.SequenceEqual(original), "Same imported name normalized hidden bytes.");
    }
}
