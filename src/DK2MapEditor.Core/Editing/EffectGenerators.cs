using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record EffectGeneratorInput(string Field, string Value);

public sealed class EffectGeneratorDraft
{
    internal NativeMapDocument Owner { get; }
    public NativeRecord Record { get; }
    public bool Editable { get; }
    internal byte[] Payload { get; }
    internal IReadOnlyDictionary<string, string> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal EffectGeneratorDraft(NativeMapDocument owner, NativeRecord record, bool editable, byte[] payload, IReadOnlyDictionary<string, string> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Record = record; Editable = editable; Payload = payload; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public bool Changed => Baseline.Any(item => Raw(item.Key) != item.Value);
    public bool SameBaseline(EffectGeneratorDraft other) => ReferenceEquals(Owner, other.Owner) && Editable == other.Editable && Record.File == other.Record.File && Record.Offset == other.Record.Offset && Record.Length == other.Record.Length && Record.Category == other.Record.Category && Payload.AsSpan().SequenceEqual(other.Payload);
    internal EffectGeneratorDraft With(Dictionary<string, string> updated) => new(Owner, Record, Editable, Payload, Baseline, new ReadOnlyDictionary<string, string>(updated));
}

public static class EffectGenerators
{
    public static IReadOnlyList<string> Fields { get; } = Array.AsReadOnly(new[] { "Effect1", "Effect2", "Effect3", "Effect4", "Frequency" });
    public static bool IsEffect(NativeRecord record) => record.Category == "GAMEEFFECT" || record.Category == "Project.MAPRECTEX" && record.Fields.FirstOrDefault(field => field.Name == "node.type") is { } type && record.Read(type) == "2";
    public static string StorageField(NativeRecord record, string field) => !Fields.Contains(field) ? throw new ArgumentException("Unknown generator option.")
        : record.Category == "GAMEEFFECT" ? field == "Frequency" ? "bEffectFrequency" : "w" + field : field == "Frequency" ? "freq" : field.ToLowerInvariant();
    public static bool Supported(NativeRecord record)
    {
        if (!IsEffect(record) || record.Length != (record.Category == "GAMEEFFECT" ? 44 : 292)) return false;
        for (var index = 0; index < Fields.Count; index++)
        {
            var field = record.Fields.FirstOrDefault(field => field.Name == StorageField(record, Fields[index]));
            var native = record.Category == "GAMEEFFECT";
            if (field is null || field.Offset != (native ? index == 4 ? 36 : 28 + index * 2 : 60 + index * 4)
                || field.Size != (native ? index == 4 ? 1 : 2 : 4) || field.Kind != (native ? index == 4 ? NativeValueKind.Byte : NativeValueKind.UInt16 : NativeValueKind.Int32)) return false;
        }
        return true;
    }
}

public sealed partial class NativeMapDocument
{
    private NativeRecord CurrentEffectGenerator(NativeRecord record)
    {
        var current = Records().SingleOrDefault(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length && item.Category == record.Category)
            ?? throw new ArgumentException("Select a current effect generator.");
        if (!EffectGenerators.IsEffect(current)) throw new ArgumentException("Select an effect generator, not an area/gate or definition.");
        return current;
    }
    public EffectGeneratorDraft CreateEffectGeneratorDraft(NativeRecord record)
    {
        record = CurrentEffectGenerator(record); var supported = EffectGenerators.Supported(record);
        var values = new ReadOnlyDictionary<string, string>(EffectGenerators.Fields.ToDictionary(field => field, field => supported ? record.Read(record.Fields.Single(item => item.Name == EffectGenerators.StorageField(record,field))) : ""));
        return new(this, record, supported, record.File.Data.Slice(record.Offset, record.Length).ToArray(), values, values);
    }
    private NativeRecord VerifyEffectGeneratorDraft(EffectGeneratorDraft draft)
    {
        if (!ReferenceEquals(this, draft.Owner)) throw new ArgumentException("Generator draft belongs to a different document.");
        var record = CurrentEffectGenerator(draft.Record); var current = CreateEffectGeneratorDraft(record);
        if (!draft.Editable || !current.Editable) throw new InvalidOperationException("A verified generator record layout is required.");
        if (!draft.SameBaseline(current)) throw new InvalidOperationException("The generator changed after the draft was opened.");
        return record;
    }
    public EffectGeneratorDraft UpdateEffectGenerator(EffectGeneratorDraft draft, IEnumerable<EffectGeneratorInput> inputs)
    {
        var record = VerifyEffectGeneratorDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(EffectGenerators.Fields.Order())) throw new ArgumentException("Provide every generator option exactly once.");
        var values = new Dictionary<string, string>();
        foreach (var input in requested)
        {
            var number = int.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture); var encoded = number.ToString(CultureInfo.InvariantCulture);
            if (encoded != draft.Raw(input.Field))
            {
                if (input.Field == "Frequency") { if (number is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(inputs), "Generator frequency is an integer 1–255."); }
                else if (!EffectCatalogue.Choices.Any(choice => choice.Value == encoded)) throw new ArgumentException("Choose a source effect; unknown imported IDs may only be retained.");
                record.Fields.Single(field => field.Name == EffectGenerators.StorageField(record,input.Field)).Encode(encoded);
            }
            values.Add(input.Field, encoded);
        }
        return draft.With(values);
    }
    public void ApplyEffectGenerator(EffectGeneratorDraft draft)
    {
        var record = VerifyEffectGeneratorDraft(draft); if (!draft.Changed) return;
        Edit("Change effect generator options", () =>
        {
            foreach (var field in EffectGenerators.Fields.Where(field => draft.Raw(field) != draft.Baseline[field]))
                SetField(record, record.Fields.Single(item => item.Name == EffectGenerators.StorageField(record,field)), draft.Raw(field));
        });
    }
}
