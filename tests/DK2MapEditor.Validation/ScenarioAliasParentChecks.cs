using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using DK2MapEditor.Core.Editing;

internal static partial class ScenarioAliasEventChecks
{
    private static void CheckParentComposition(bool native, string type, string directory)
    {
        var (doc, record) = Fixture(native, type);
        var original = Snapshot(doc); var names = Names(doc); var history = doc.GetHistory().Undo.Count;
        var parameterDraft = doc.CreateScenarioParameterDraft(record);
        var aliasDraft = doc.CreateScenarioAliasEventDraft(record);
        doc.ApplyScenarioAliasParameterDraft(parameterDraft, aliasDraft); Exact(original);
        Expect(Names(doc) == names && doc.GetHistory().Undo.Count == history, "Composed no-op changed aliases/history.");
        var trigger = type.StartsWith("TRIGGER_", StringComparison.Ordinal);
        foreach (var repeat in trigger ? new[] { 0, 1, 2, 254, 255 } : new[] { -1 })
        {
            parameterDraft = doc.CreateScenarioParameterDraft(record);
            aliasDraft = doc.CreateScenarioAliasEventDraft(record);
            if (trigger) parameterDraft = doc.UpdateScenarioParameterDraft(parameterDraft,
                [new(native ? "Repeat" : "repeat", repeat.ToString(CultureInfo.InvariantCulture))]);
            var requested = aliasDraft.Values.ToDictionary(value => value.Key, value => value.Value);
            requested[aliasDraft.PrimaryParameter] = "1";
            if (type != "ACTION_SETTIMER") requested[5] = "4294967295";
            aliasDraft = doc.UpdateScenarioAliasEvent(aliasDraft,
                requested.Select(value => new ScenarioEventParameter(value.Key, value.Value)), [new(1, "Combined")]);
            Exact(original); Expect(Names(doc) == names, "Prepared child/parent drafts wrote early.");
            var expected = original.ToDictionary(row => row.Key, row => row.Value.ToArray());
            Operand(expected[record.File], record, native, aliasDraft.PrimaryParameter, 1);
            if (type != "ACTION_SETTIMER") Operand(expected[record.File], record, native, 5, uint.MaxValue);
            if (trigger) expected[record.File][record.Offset + (native ? 23 : 55)] = checked((byte)repeat);
            if (!native)
            {
                var file = doc.Files.Single(file => file.Signature == NativeMapDocument.LegacyEvents);
                var slot = aliasDraft.Kind == ScenarioNameKind.Variable ? 1 : 130;
                var offset = file.Length - 146 * 64 + slot * 64;
                expected[file].AsSpan(offset, 64).Clear(); Encoding.ASCII.GetBytes("Combined").CopyTo(expected[file], offset);
            }
            doc.ApplyScenarioAliasParameterDraft(parameterDraft, aliasDraft); Exact(expected);
            Expect(doc.GetHistory().Undo.Count == history + 1, "Parent operands/names/repeat were not one Undo.");
            Expect(doc.ReadScenarioNames().Slots.Single(slot => slot.Kind == aliasDraft.Kind && slot.Index == 1).StoredName == "Combined", "Composed alias lost source slot identity.");
            var applied = Snapshot(doc); var appliedNames = Names(doc);
            doc.Undo(); Exact(original); Expect(Names(doc) == names, "Parent Undo lost aliases.");
            doc.Redo(); Exact(applied); Expect(Names(doc) == appliedNames, "Parent Redo lost aliases.");
            var saved = doc.SaveCopy(Path.Combine(directory, $"parent-{native}-{type}-{repeat}"));
            var reopened = NativeMapDocument.Load(saved); var loaded = reopened.Records().Single(ScriptGraph.IsEvent);
            Expect(Names(reopened) == appliedNames, "Composed aliases did not round-trip.");
            Expect(reopened.CreateScenarioParameterDraft(loaded).Values.SequenceEqual(doc.CreateScenarioParameterDraft(record).Values), "Composed operands/repeat did not round-trip.");
            doc.Undo(); Exact(original); Expect(Names(doc) == names, "Saved parent Undo lost aliases.");
        }
        parameterDraft = doc.CreateScenarioParameterDraft(record); aliasDraft = doc.CreateScenarioAliasEventDraft(record);
        var operand = parameterDraft.Fields.Single(field => field.Name == (native ? "Param" : "param") + aliasDraft.PrimaryParameter);
        var conflicting = doc.UpdateScenarioParameterDraft(parameterDraft, [new(operand.Name, "2")]);
        Reject(() => doc.ApplyScenarioAliasParameterDraft(conflicting, aliasDraft)); Exact(original);
        var foreign = Fixture(native, type);
        Reject(() => doc.ApplyScenarioAliasParameterDraft(foreign.Document.CreateScenarioParameterDraft(foreign.Record), aliasDraft));
        Reject(() => doc.ApplyScenarioAliasParameterDraft(parameterDraft, foreign.Document.CreateScenarioAliasEventDraft(foreign.Record))); Exact(original);
        doc.SetScenarioName(aliasDraft.Kind, 6, "External"); doc.Undo();
        Reject(() => doc.ApplyScenarioAliasParameterDraft(parameterDraft, aliasDraft)); Exact(original);
        parameterDraft = doc.CreateScenarioParameterDraft(record); aliasDraft = doc.CreateScenarioAliasEventDraft(record);
        var raw = Mutable(doc.MainFile); var last = raw.Length - 1; raw[last] ^= 1;
        Reject(() => doc.ApplyScenarioAliasParameterDraft(parameterDraft, aliasDraft)); raw[last] ^= 1; Exact(original);
        Expect(Names(doc) == names && doc.GetHistory().Undo.Count == history, "Rejected composed draft changed state/history.");
    }
}
