using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class NativeScriptPartitionChecks
{
    private const string GameName = "DK2Preview_6a88240eaf814f78";
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static uint Word(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static void Word(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static ushort ID(NativeRecord record) => ScriptGraph.ID(record);
    private static NativeRecord Event(NativeMapDocument document, ushort id) => ScriptGraph.Build(document).Events[id].Record;
    private static void Set(NativeMapDocument document, NativeRecord record, string name, string value) => document.SetField(record, record.Fields.Single(field => field.Name == name), value);
    private static LegacyEventSchemas.EventType Type(string name) => LegacyEventSchemas.All.Single(type => type.Name == name);
    private static NativeRecord Condition(NativeMapDocument document) => document.AddScenarioEvent(0xD5, new(0, 0), Type("TRIGGER_TIMEELAPSED"));
    private static NativeRecord Action(NativeMapDocument document) => document.AddScenarioEvent(0xD6, new(0, 0), Type("ACTION_ALLIES"));
    private static NativeMapDocument New() => NativeMapDocument.Create(12, 12, GameName);
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> snapshot) { foreach (var (file, bytes) in snapshot) Require(file.Data.Span.SequenceEqual(bytes), "Undo/refusal changed complete native bytes."); }
    private static void History(NativeMapDocument document, Dictionary<NativeFile, byte[]> before)
    { var after = Snapshot(document); document.Undo(); Exact(before); document.Redo(); Exact(after); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or ArgumentException) { return; } throw new Exception("Malformed script partition permitted semantic editing/export."); }

    // Independent reader oracle: Files_Game.c consumes dwTriggersCount literal
    // D5/0x10 frames, then dwTriggersActionsCount literal D6/0x10 frames.
    private static int OriginalReader(NativeMapDocument document)
    {
        var bytes = document.Files.Single(file => file.Signature == 0xD2).Data.Span; var offset = 60;
        foreach (var (count, signature) in new[] { (Word(bytes, 20), 0xD5u), (Word(bytes, 24), 0xD6u) })
            for (var index = 0u; index < count; index++, offset += 24)
                Require(offset <= bytes.Length - 24 && Word(bytes, offset) == signature && Word(bytes, offset + 4) == 16,
                    "Original reader rejects the condition/action block boundary at " + offset);
        return offset;
    }
    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DK2ScriptPartition-" + Guid.NewGuid().ToString("N")));
        public string Descriptor { get; private set; } = "";
        private readonly List<(string Path, byte[] Bytes)> inputs = [];
        public Fixture() => Directory.CreateDirectory(Root);
        public string Fresh() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
        public NativeMapDocument Load(Func<byte[], byte[]> patch)
        {
            var document = New(); Condition(document); Action(document); Descriptor = document.SaveCopy(Fresh(), temporary: true);
            var path = Path.Combine(Path.GetDirectoryName(Descriptor)!, Path.GetFileName(document.Files.Single(file => file.Signature == 0xD2).ExportPath));
            var bytes = patch(File.ReadAllBytes(path)); Word(bytes, 8, (uint)bytes.Length); File.WriteAllBytes(path, bytes); inputs.Add((path, bytes));
            return NativeMapDocument.Load(Descriptor);
        }
        public NativeMapDocument Reopen(NativeMapDocument document) => NativeMapDocument.Load(document.SaveCopy(Fresh(), temporary: true));
        public void Dispose()
        {
            foreach (var (path, bytes) in inputs) Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "Original script file was changed.");
            var resolved = Path.GetFullPath(Root); Require(resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Unexpected fixture cleanup target.");
            Directory.Delete(resolved, recursive: true);
        }
    }
    private static NativeRecord Gate(NativeMapDocument document)
    { var gate = document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEGATE"), new(4, 5), 1); Set(document, gate, "bGateType", "33"); return gate; }
    private static NativeRecord Destroy(NativeMapDocument document)
    {
        var action = document.AddScenarioEvent(0xD6, new(0, 0), Type("ACTION_DESTROYGATE"));
        Set(document, action, "NativeArgument5", "5"); Set(document, action, "NativeArgument7", "6"); return Event(document, ID(action));
    }
    private static string? Selected(NativeMapDocument document, ushort id) => document.SelectedScenarioGate(document.CreateScenarioParameterDraft(Event(document, id)));
    private static void SelectSecond(NativeMapDocument document, ushort id)
    { var draft = document.CreateScenarioParameterDraft(Event(document, id)); document.ApplyScenarioParameterDraft(document.UpdateScenarioGate(draft, document.ScenarioGateChoices()[1].Key)); }
    private static uint ConvertedTarget(NativeMapDocument document)
    {
        var conversion = document.PrepareProjectConversion(); Require(!conversion.HasBlockingIssues, "Verified gate binding became unprojectable.");
        var action = conversion.Project.Records().Single(record => record.Category == "Project.ACTION"); return uint.Parse(action.Read(action.Fields.Single(field => field.Name == "param5")));
    }
    public static void Run(Action<string, Action> run)
    {
        run("native script partition: alternating action/condition additions satisfy original reader and stable per-category order", () =>
        {
            using var fixture = new Fixture(); var document = New(); var action1 = ID(Action(document)); var before = Snapshot(document);
            var condition1 = ID(Condition(document)); OriginalReader(document); History(document, before);
            var action2 = ID(Action(document)); before = Snapshot(document); var condition2 = ID(Condition(document)); OriginalReader(document); History(document, before);
            Require(document.Records().Where(record => record.File.Signature == 0xD2).Select(ID).SequenceEqual(new[] { condition1, condition2, action1, action2 }), "Existing category order or event IDs changed.");
            var reopened = fixture.Reopen(document); OriginalReader(reopened);
            Require(reopened.Files.Single(file => file.Signature == 0xD2).Data.Span.SequenceEqual(document.Files.Single(file => file.Signature == 0xD2).Data.Span), "Raw Save/reopen changed valid script bytes.");
        });
        run("native script partition: inserting a condition after an action refreshes its moved owner before writing the edge", () =>
        {
            var document = New(); var action = Action(document); var actionID = ID(action);
            Set(document, document.Records().Single(record => record.Category == "Map"), "wMainTrigger", actionID.ToString());
            var field = action.Fields.Single(field => field.Name == "NextOr"); var before = Snapshot(document);
            var condition = document.InsertScriptEvent(action, field, Type("TRIGGER_TIMEELAPSED")); OriginalReader(document);
            Require(Event(document, actionID).Read(field) == ID(condition).ToString(), "Insertion wrote the new condition instead of the moved action owner.");
            History(document, before); OriginalReader(document);
        });
        run("native script partition: condition insertion retains selected duplicate, deleted target and missing-zero gate identities", () =>
        {
            foreach (var mode in new[] { "selected", "deleted", "zero" })
            {
                var document = New(); NativeRecord action;
                if (mode == "zero") { action = Destroy(document); Gate(document); }
                else { Gate(document); var second = Gate(document); action = Destroy(document); SelectSecond(document, ID(action)); if (mode == "deleted") document.DeleteRecord(second); }
                var id = ID(action); var selected = Selected(document, id); var target = ConvertedTarget(document); var payload = Event(document, id).File.Data.Slice(action.Offset, 24).ToArray(); var before = Snapshot(document);
                Condition(document); OriginalReader(document); var moved = Event(document, id);
                Require(moved.File.Data.Span.Slice(moved.Offset, 24).SequenceEqual(payload), "Moving an action changed its raw payload.");
                Require(Selected(document, id) == selected && ConvertedTarget(document) == target && target == (mode == "zero" ? 0u : 2u), "Offset rebasing guessed another gate with the same coordinates.");
                History(document, before); Require(Selected(document, id) == selected && ConvertedTarget(document) == target, "Redo lost the exact gate identity.");
            }
        });
        run("native script partition: cloned mixed graphs keep edge IDs and gate bindings while existing actions move", () =>
        {
            var document = New(); Gate(document); Gate(document); var action = Destroy(document); SelectSecond(document, ID(action)); var actionID = ID(action);
            var condition = Condition(document); var conditionID = ID(condition); Set(document, condition, "ChildAnd", actionID.ToString()); var before = Snapshot(document);
            var copy = document.DuplicateRecord(Event(document, conditionID)); OriginalReader(document);
            var newAction = ushort.Parse(copy.Read(copy.Fields.Single(field => field.Name == "ChildAnd")));
            Require(newAction != actionID && Selected(document, newAction) == Selected(document, actionID) && Selected(document, actionID) == document.ScenarioGateChoices()[1].Key, "Cloning changed a gate identity or shared an event ID.");
            History(document, before); OriginalReader(document);
        });
        run("native script partition: counted boundaries ignore stale payload size and preserve opaque suffix", () =>
        {
            using var fixture = new Fixture(); var tail = Enumerable.Range(0, 96).Select(index => (byte)(0x80 + index % 113)).ToArray();
            var document = fixture.Load(bytes => { var result = bytes.Concat(tail).ToArray(); Word(result, 56, 16); return result; }); var before = Snapshot(document);
            Condition(document); var end = OriginalReader(document); var file = document.Files.Single(file => file.Signature == 0xD2);
            Require(file.Length - end == tail.Length && file.Data.Span[end..].SequenceEqual(tail) && Word(file.Data.Span, 56) == 40, "Insertion used stale payload metadata or changed opaque bytes.");
            History(document, before); OriginalReader(fixture.Reopen(document));
        });
        run("native script partition: bad slot counts/order remain wholly opaque with shared inspector diagnostics and exact raw copy", () =>
        {
            foreach (var mode in new[] { "swap", "conditions", "actions" })
            {
                using var fixture = new Fixture(); var document = fixture.Load(bytes =>
                {
                    if (mode == "swap") { var first = bytes.AsSpan(60, 24).ToArray(); bytes.AsSpan(84, 24).CopyTo(bytes.AsSpan(60)); first.CopyTo(bytes, 84); }
                    else { Word(bytes, 20, mode == "conditions" ? 2u : 0u); Word(bytes, 24, mode == "conditions" ? 0u : 2u); } return bytes;
                });
                var record = document.Records().Single(record => record.File.Signature == 0xD2);
                Require(record.Category == "Opaque.NativeCollection" && record.Fields.Count == 0 && record.Offset == 0 && record.Length == record.File.Length, "Bad script partition received editable fields.");
                var editor = NativeMapValidator.Validate(document).Where(item => item.Code == "DK2N047").ToArray(); var preview = new Dk2FileInspector().Inspect(fixture.Descriptor).Diagnostics.Where(item => item.Code == "DK2N047").ToArray();
                Require(editor.Length == 1 && editor.SequenceEqual(preview), "Editor and inspector disagree about malformed script partition.");
                var before = Snapshot(document); Reject(() => Condition(document)); Exact(before); Require(!document.CanUndo && !document.CanRedo, "Refused insertion left history.");
                Require(document.PrepareProjectConversion().HasBlockingIssues, "DKM silently repaired an unknown script partition.");
                var reopened = fixture.Reopen(document); Require(reopened.Files.Single(file => file.Signature == 0xD2).Data.Span.SequenceEqual(record.File.Data.Span), "Raw copy normalised malformed script bytes.");
            }
        });
        run("native script partition: explicit original text mode may omit malformed script blocks", () =>
        {
            using var fixture = new Fixture(); var document = fixture.Load(bytes => { Word(bytes, 20, 0); Word(bytes, 24, 2); return bytes; });
            Reject(() => TextMapFile.Project(document)); Require(TextMapFile.Project(document, TextMapScriptMode.OriginalNoScenarios).Text.Length != 0, "Explicit no-scenario projection was blocked.");
        });
        run("native script partition: clipboard paste inserts conditions before destination actions without losing its gate choice", () =>
        {
            var source = New(); var thing = source.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEOBJECT"), new(2, 2));
            var owner = source.ScriptOwners().Single(owner => owner.Record.File == thing.File && owner.Record.Offset == thing.Offset); var root = source.EnsureScriptScope(owner);
            var condition = source.InsertScriptEvent(root.Owner, root.Field, Type("TRIGGER_TIMEELAPSED")); var child = condition.Fields.Single(field => field.Name == "ChildAnd"); source.InsertScriptEvent(condition, child, Type("ACTION_ALLIES"));
            var clipboard = source.CopySelection(new(1, 1, 4, 4)); var destination = New(); Gate(destination); Gate(destination); var action = Destroy(destination); var id = ID(action); SelectSecond(destination, id);
            var before = Snapshot(destination); destination.PasteSelection(new(1, 6), clipboard); OriginalReader(destination);
            Require(Selected(destination, id) == destination.ScenarioGateChoices()[1].Key, "Pasting moved an existing action onto the first duplicate gate.");
            Require(ScriptGraph.Build(destination).Events.Count == 3, "Pasting lost a script event."); History(destination, before); OriginalReader(destination);
        });
    }
}
