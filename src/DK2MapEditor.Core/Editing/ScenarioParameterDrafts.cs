using System.Collections.ObjectModel;

namespace DK2MapEditor.Core.Editing;

public sealed record ScenarioParameterValue(string Field, string Value);

/// <summary>Immutable byte-preserving transaction shared by the original
/// parameter pages. A page supplies its own source choices and active controls;
/// this layer never invents missing native operands or resets inactive bytes.</summary>
public sealed class ScenarioParameterDraft
{
    internal ScenarioParameterDraft(NativeMapDocument owner, NativeRecord record, long revision,
        byte[] fingerprint, string type, IReadOnlyList<NativeField> fields,
        IReadOnlyDictionary<string, string> baseline, IReadOnlyDictionary<string, string> values,
        string? nativeGateKey = null, string? nativeGateBaselineKey = null)
    {
        Owner = owner; Record = record; Revision = revision; Fingerprint = fingerprint;
        Type = type; Fields = fields; Baseline = baseline; Values = values;
        NativeGateKey = nativeGateKey; NativeGateBaselineKey = nativeGateBaselineKey;
    }
    internal NativeMapDocument Owner { get; }
    internal NativeRecord Record { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal IReadOnlyDictionary<string, string> Baseline { get; }
    internal string? NativeGateKey { get; }
    internal string? NativeGateBaselineKey { get; }
    public string Type { get; }
    public int PageID => LegacyEventSchemas.ParameterPageID(Type);
    public IReadOnlyList<NativeField> Fields { get; }
    public IReadOnlyDictionary<string, string> Values { get; }
    public bool Changed => NativeGateKey != NativeGateBaselineKey || Fields.Any(operand => !operand.Encode(Values[operand.Name]).AsSpan().SequenceEqual(operand.Encode(Baseline[operand.Name])));
    internal static IReadOnlyDictionary<string, string> Freeze(IEnumerable<KeyValuePair<string, string>> values)
        => new ReadOnlyDictionary<string, string>(values.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));
    internal ScenarioParameterDraft With(IReadOnlyDictionary<string, string> values, bool resetNativeGate = false)
        => new(Owner, Record, Revision, Fingerprint, Type, Fields, Baseline, values, resetNativeGate ? null : NativeGateKey, NativeGateBaselineKey);
    internal ScenarioParameterDraft WithGate(string key)
        => new(Owner, Record, Revision, Fingerprint, Type, Fields, Baseline, Values, key, NativeGateBaselineKey);
}

public sealed partial class NativeMapDocument
{
    public ScenarioParameterDraft CreateScenarioParameterDraft(NativeRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (transactionOpen) throw new InvalidOperationException("Finish the current edit before opening event parameters.");
        var current = Records().SingleOrDefault(item => item.File == record.File && item.Offset == record.Offset
            && item.Length == record.Length && item.Category == record.Category);
        var type = current is null ? null : EntityReferences.EventType(current);
        if (type is null || LegacyEventSchemas.ParameterPageID(type.Name) == -1 && type.RecordSignature != 0xD5)
            throw new InvalidOperationException("This event has no verified original parameter or repeat page.");
        var hasParameters = LegacyEventSchemas.ParameterPageID(type.Name) != -1;
        var project = current!.Category is "Project.TRIGGER" or "Project.ACTION";
        // Both original Compiler_GenerateTriggers and Files_GameLoadTriggers
        // specify version 4. A future header is not verified by a familiar row.
        if (!project && (current.File.Signature != 0xD2 || current.File.UInt32(4) != 4))
            throw new InvalidOperationException("The native trigger collection version is unverified.");
        if (current.Length != (project ? 56 : 24))
            throw new InvalidOperationException("The event parameter layout is unverified.");
        var fields = new List<NativeField>();
        if (hasParameters && project)
        {
            for (var number = 1; number <= 8; number++)
            {
                var field = current.Fields.Single(item => item.Name == "param" + number);
                if (field.Offset != 16 + (number - 1) * 4 || field.Size != 4 || field.Kind != NativeValueKind.Int32 || field.BitMask != 0)
                    throw new InvalidOperationException("The project parameter layout is unverified.");
                fields.Add(field);
            }
        }
        else if (hasParameters)
        {
            foreach (var argument in type.Arguments)
            {
                var field = current.Fields.Single(item => item == argument);
                if (field.Offset < 8 || field.Size <= 0 || field.Offset + field.Size > 16 || field.BitMask != 0
                    || field.Kind is not (NativeValueKind.Byte or NativeValueKind.UInt16 or NativeValueKind.Int32))
                    throw new InvalidOperationException("The native parameter layout is unverified.");
                fields.Add(field);
            }
        }
        if (type.RecordSignature == 0xD5)
        {
            var repeat = current.Fields.Single(item => item.Name == (project ? "repeat" : "Repeat"));
            if (repeat.Offset != (project ? 55 : 23) || repeat.Size != 1 || repeat.Kind != NativeValueKind.Byte || repeat.BitMask != 0)
                throw new InvalidOperationException("The condition repeat layout is unverified.");
            fields.Add(repeat);
        }
        if (fields.GroupBy(field => field.Name, StringComparer.Ordinal).Any(group => group.Count() != 1)
            || fields.SelectMany(field => Enumerable.Range(field.Offset, field.Size)).Distinct().Count() != fields.Sum(field => field.Size))
            throw new InvalidOperationException("Event parameters overlap.");
        var values = ScenarioParameterDraft.Freeze(fields.Select(field => new KeyValuePair<string, string>(field.Name, current.Read(field))));
        var fingerprint = UnusedScriptFingerprint(); var gateKey = NativeGateTargetKey(current);
        return new(this, current, EditRevision, fingerprint, type.Name, fields.AsReadOnly(), values, values, gateKey, gateKey);
    }

    private void VerifyScenarioParameterDraft(ScenarioParameterDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (transactionOpen || !ReferenceEquals(draft.Owner, this) || draft.Revision != EditRevision
            || !UnusedScriptFingerprint().AsSpan().SequenceEqual(draft.Fingerprint))
            throw new InvalidOperationException("Refresh the parameter draft after document changes.");
        var current = CreateScenarioParameterDraft(draft.Record);
        if (current.Type != draft.Type || !current.Fields.SequenceEqual(draft.Fields)
            || current.Values.Count != draft.Baseline.Count
            || current.Values.Any(item => !draft.Baseline.TryGetValue(item.Key, out var value) || item.Value != value))
            throw new InvalidOperationException("The event parameters changed after opening this draft.");
    }

    public ScenarioParameterDraft UpdateScenarioParameterDraft(ScenarioParameterDraft draft, IEnumerable<ScenarioParameterValue> parameters)
    {
        VerifyScenarioParameterDraft(draft);
        ArgumentNullException.ThrowIfNull(parameters);
        var requested = parameters.ToArray();
        if (requested.Any(item => item is null)) throw new ArgumentException("An event parameter input is missing.");
        if (requested.Select(item => item.Field).Distinct(StringComparer.Ordinal).Count() != requested.Length)
            throw new ArgumentException("Duplicate event parameter inputs.");
        var values = draft.Values.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        foreach (var item in requested)
        {
            if (!values.ContainsKey(item.Field)) throw new ArgumentException("This field is not an editable event operand.");
            ArgumentNullException.ThrowIfNull(item.Value);
            values[item.Field] = item.Value;
        }
        // Preflight every operand before a new draft or any mutation is exposed.
        foreach (var field in draft.Fields) _ = field.Encode(values[field.Name]);
        var resetGate = !IsLegacyProject && draft.Type == "ACTION_DESTROYGATE" && requested.Any(item => item.Field is "NativeArgument5" or "NativeArgument7"
            && !draft.Fields.Single(field => field.Name == item.Field).Encode(values[item.Field]).AsSpan().SequenceEqual(draft.Fields.Single(field => field.Name == item.Field).Encode(draft.Values[item.Field])));
        return draft.With(ScenarioParameterDraft.Freeze(values), resetGate);
    }

    public void ApplyScenarioParameterDraft(ScenarioParameterDraft draft)
    {
        VerifyScenarioParameterDraft(draft);
        var writes = draft.Fields.Select(field => (Field: field, Bytes: field.Encode(draft.Values[field.Name])))
            .Where(item => !item.Bytes.AsSpan().SequenceEqual(draft.Record.File.Data.Span.Slice(draft.Record.Offset + item.Field.Offset, item.Field.Size)))
            .ToArray();
        Edit("Edit scenario parameters", () =>
        {
            foreach (var write in writes) write.Bytes.CopyTo(draft.Record.File.Bytes, draft.Record.Offset + write.Field.Offset);
            ApplyNativeGateParameterBinding(draft);
        });
    }
}
