using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record ScenarioEventParameter(int Number, string Value);
public sealed record ScenarioEventAlias(int Index, string Name);

/// <summary>Private immutable draft for the four source inline alias pages.
/// Editor aliases and game operands are committed together; IDs/graph/repeat
/// are never inferred from typed alias text.</summary>
public sealed class ScenarioAliasEventDraft
{
    internal ScenarioAliasEventDraft(NativeMapDocument owner, NativeRecord record, long revision, byte[] fingerprint,
        string type, IReadOnlyDictionary<int, string> baseline, IReadOnlyDictionary<int, string> values,
        IReadOnlyDictionary<int, string> names, IReadOnlyDictionary<int, string> aliases)
    {
        Owner = owner; Record = record; Revision = revision; Fingerprint = fingerprint;
        Type = type; Baseline = baseline; Values = values; Names = names; Aliases = aliases;
    }
    internal NativeMapDocument Owner { get; }
    internal NativeRecord Record { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal IReadOnlyDictionary<int, string> Baseline { get; }
    internal IReadOnlyDictionary<int, string> Names { get; }
    internal IReadOnlyDictionary<int, string> Aliases { get; }
    public string Type { get; }
    public IReadOnlyDictionary<int, string> Values { get; }
    public ScenarioNameKind Kind => Type is "TRIGGER_VARIABLE" or "ACTION_SETVARIABLE" ? ScenarioNameKind.Variable : ScenarioNameKind.Timer;
    public int PrimaryParameter => Type.StartsWith("TRIGGER_", StringComparison.Ordinal) ? 2 : 1;
    public int MaximumPrimaryID => Type == "ACTION_SETTIMER" ? 15 : Kind == ScenarioNameKind.Variable ? 128 : 16;
    public bool SecondaryAliasActive => Type == "ACTION_SETVARIABLE" && Values[2] is "12" or "20" or "36";
    // The source initializes secondary combo214 at slot0 before selecting the
    // stored slot only for variable modes. Literal param5 is a number, not an
    // initial hidden variable ID. Unknown modes retain their literal bits.
    public string InitialSecondaryID=>Type=="ACTION_SETVARIABLE"&&!SecondaryAliasActive?"0"
        :Values.TryGetValue(Type=="TRIGGER_VARIABLE"?4:5,out var value)?value:"0";
    public bool Changed => Values.Any(value => value.Value != Baseline[value.Key]) || Aliases.Any(alias => alias.Value != Names[alias.Key]);
    public string Name(int index) => Aliases.TryGetValue(index, out var name) ? name : Names.TryGetValue(index, out name) ? name : "";
    internal static IReadOnlyDictionary<int, string> Freeze(IEnumerable<KeyValuePair<int, string>> items) => new ReadOnlyDictionary<int, string>(items.ToDictionary(item => item.Key, item => item.Value));
    internal ScenarioAliasEventDraft With(IReadOnlyDictionary<int, string> values, IReadOnlyDictionary<int, string> aliases)
        => new(Owner, Record, Revision, Fingerprint, Type, Baseline, values, Names, aliases);
}

public sealed partial class NativeMapDocument
{
    public static bool HasScenarioAliasPage(string? type) => type is "TRIGGER_VARIABLE" or "TRIGGER_TIMER" or "ACTION_SETVARIABLE" or "ACTION_SETTIMER";
    private static int[] ScenarioAliasParameters(string type) => type switch
    {
        "TRIGGER_VARIABLE" => [1, 2, 3, 4, 5],
        "TRIGGER_TIMER" => [1, 2, 5],
        "ACTION_SETVARIABLE" => [1, 2, 5],
        "ACTION_SETTIMER" => [1],
        _ => throw new ArgumentException("This event has no original inline alias page.")
    };
    private static NativeField ScenarioEventField(NativeRecord record, int number)
    {
        var name = record.Category is "Project.TRIGGER" or "Project.ACTION" ? "param" + number : "Param" + number;
        return record.Fields.Single(field => field.Name == name);
    }
    private static bool ScenarioEventNumber(string type, int number) => number == 5 && type is "TRIGGER_VARIABLE" or "TRIGGER_TIMER" or "ACTION_SETVARIABLE";
    private static string ScenarioEventRead(NativeRecord record, string type, int number)
    {
        var raw = record.Read(ScenarioEventField(record, number));
        // Source GetDlgItemInt is unsigned; the compiler stores LONG bits.
        // Treat -1/0xFFFFFFFF as one value without normalizing imported bytes.
        return ScenarioEventNumber(type, number) ? unchecked((uint)long.Parse(raw, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture) : raw;
    }
    private static byte[] ScenarioEventEncode(NativeField field, string type, int number, string value)
    {
        if (ScenarioEventNumber(type, number))
        {
            var bits = uint.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            value = field.Kind == NativeValueKind.Int32 ? unchecked((int)bits).ToString(CultureInfo.InvariantCulture) : bits.ToString(CultureInfo.InvariantCulture);
        }
        return field.Encode(value);
    }
    public ScenarioAliasEventDraft CreateScenarioAliasEventDraft(NativeRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (transactionOpen) throw new InvalidOperationException("Finish the current edit before opening event aliases.");
        if (!CanEditScenarioNames) throw new InvalidOperationException("The editor alias table is unverified.");
        var current = Records().SingleOrDefault(item => item.File == record.File && item.Offset == record.Offset
            && item.Length == record.Length && item.Category == record.Category);
        var type = current is null ? null : EntityReferences.EventType(current)?.Name;
        if (!HasScenarioAliasPage(type) || !CanEditScenarioNames) throw new InvalidOperationException("A verified event and alias table are required.");
        var numbers = ScenarioAliasParameters(type!);
        var values = ScenarioAliasEventDraft.Freeze(numbers.Select(number => new KeyValuePair<int, string>(number, ScenarioEventRead(current!, type!, number))));
        var kind = type is "TRIGGER_VARIABLE" or "ACTION_SETVARIABLE" ? ScenarioNameKind.Variable : ScenarioNameKind.Timer;
        var names = ScenarioAliasEventDraft.Freeze(ReadScenarioNames().Slots.Where(slot => slot.Kind == kind).Select(slot => new KeyValuePair<int, string>(slot.Index, slot.StoredName)));
        return new(this, current!, EditRevision, UnusedScriptFingerprint(), type!, values, values, names, ScenarioAliasEventDraft.Freeze([]));
    }
    private void VerifyScenarioAliasEvent(ScenarioAliasEventDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (transactionOpen || !ReferenceEquals(draft.Owner, this) || draft.Revision != EditRevision
            || !UnusedScriptFingerprint().AsSpan().SequenceEqual(draft.Fingerprint) || !CanEditScenarioNames)
            throw new InvalidOperationException("Refresh the event and alias draft after document changes.");
        var current = CreateScenarioAliasEventDraft(draft.Record);
        if (current.Type != draft.Type || current.Baseline.Any(value => draft.Baseline[value.Key] != value.Value))
            throw new InvalidOperationException("The event layout or operands changed after opening this draft.");
    }
    public ScenarioAliasEventDraft UpdateScenarioAliasEvent(ScenarioAliasEventDraft draft,
        IEnumerable<ScenarioEventParameter> parameters, IEnumerable<ScenarioEventAlias> aliases)
    {
        VerifyScenarioAliasEvent(draft);
        ArgumentNullException.ThrowIfNull(parameters); ArgumentNullException.ThrowIfNull(aliases);
        var requested = parameters.ToArray(); var names = aliases.ToArray();
        if (!requested.Select(value => value.Number).Order().SequenceEqual(draft.Baseline.Keys.Order()))
            throw new ArgumentException("Provide each source operand exactly once.");
        if (names.Select(value => value.Index).Distinct().Count() != names.Length)
            throw new ArgumentException("Conflicting aliases for the same slot must be resolved before applying.");
        var values = requested.ToDictionary(value => value.Number, value => long.Parse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
        void SourceChoice(int parameter, IEnumerable<int> accepted)
        {
            if (values[parameter] != draft.Baseline[parameter] && !accepted.Any(value => values[parameter] == value.ToString(CultureInfo.InvariantCulture)))
                throw new ArgumentOutOfRangeException(nameof(parameters), "Choose a source operand; unknown imports may only be retained.");
        }
        SourceChoice(draft.PrimaryParameter, Enumerable.Range(0, draft.MaximumPrimaryID + 1));
        if (draft.Type.StartsWith("TRIGGER_", StringComparison.Ordinal)) SourceChoice(1, Enumerable.Range(1, 6));
        if (draft.Type == "ACTION_SETVARIABLE") SourceChoice(2, [0x0A, 0x12, 0x22, 0x0C, 0x14, 0x24]);
        if (draft.Type == "TRIGGER_VARIABLE")
        {
            SourceChoice(3, [1, 2]);
            if (values[3] == "2") SourceChoice(4, Enumerable.Range(0, 129));
            // Explicit operand edits follow source mode switching. Pure alias
            // edits/no-op retain imported inactive arguments byte for byte.
            if (values.Any(value => value.Value != draft.Baseline[value.Key]))
            {
                if (values[3] == "1") values[4] = "0";
                else if (values[3] == "2") values[5] = "0";
            }
        }
        if (draft.Type == "ACTION_SETVARIABLE" && values[2] is "12" or "20" or "36") SourceChoice(5, Enumerable.Range(0, 129));
        foreach (var value in values)
            _ = ScenarioEventEncode(ScenarioEventField(draft.Record, value.Key), draft.Type, value.Key, value.Value);
        var primary = long.Parse(values[draft.PrimaryParameter], CultureInfo.InvariantCulture);
        var secondary = draft.Type == "ACTION_SETVARIABLE" && values[2] is "12" or "20" or "36" ? long.Parse(values[5], CultureInfo.InvariantCulture) : long.MinValue;
        var edits = new Dictionary<int, string>();
        foreach (var alias in names)
        {
            ArgumentNullException.ThrowIfNull(alias.Name);
            if (alias.Index != primary && alias.Index != secondary || !draft.Names.ContainsKey(alias.Index))
                throw new ArgumentException("Inline naming is limited to the current editable operand slots.");
            // Original inline pages do not clear an existing alias on empty
            // text. Explicit clearing remains in the named alias manager.
            if (alias.Name.Length == 0 || alias.Name == draft.Names[alias.Index]) continue;
            if (alias.Index == primary && alias.Index > draft.MaximumPrimaryID && alias.Index != secondary)
                throw new ArgumentException("An excluded imported operand slot may be retained, not renamed by this page.");
            _ = ScenarioAlias.Encode(alias.Name); edits.Add(alias.Index, alias.Name);
        }
        return draft.With(ScenarioAliasEventDraft.Freeze(values), ScenarioAliasEventDraft.Freeze(edits));
    }
    public void ApplyScenarioAliasEvent(ScenarioAliasEventDraft draft)
    {
        VerifyScenarioAliasEvent(draft);
        var operands = draft.Values.Where(value => value.Value != draft.Baseline[value.Key]).Select(value =>
        {
            var field = ScenarioEventField(draft.Record, value.Key);
            return (Field: field, Bytes: ScenarioEventEncode(field, draft.Type, value.Key, value.Value));
        }).ToArray();
        var aliases = draft.Aliases.Select(value => (Location: ScenarioAliasLocation(draft.Kind, value.Key), Bytes: ScenarioAlias.Encode(value.Value))).ToArray();
        Edit("Edit scenario operands and aliases", () =>
        {
            foreach (var operand in operands) operand.Bytes.CopyTo(draft.Record.File.Bytes, draft.Record.Offset + operand.Field.Offset);
            foreach (var alias in aliases) alias.Bytes.CopyTo(alias.Location.File.Bytes, alias.Location.Offset);
        });
    }
}
