using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record CreatureSpellInput(string Field, string Value);

/// <summary>Complete source page and its privately submitted projectile slot.</summary>
public sealed class CreatureSpellDraft
{
    internal NativeMapDocument Owner { get; }
    public int Index { get; }
    public bool Editable { get; }
    internal IReadOnlyList<(string Field, string Raw)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal CreatureSpellDraft(NativeMapDocument owner, int index, bool editable, IReadOnlyList<(string Field, string Raw)> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Index = index; Editable = editable; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public string Display(string field)
    {
        var raw = Raw(field); if (raw.Length == 0) return "";
        if (field is "ShotData1" or "ShotData2")
        {
            var option = GlobalProjectiles.Find(Raw("Shot"));
            return option is null ? raw : GlobalProjectiles.DisplayParameter(option, raw, field.EndsWith("2", StringComparison.Ordinal));
        }
        return GlobalDefinitionNumbers.Display("CrSpells", field, raw);
    }
    public bool Changed => Baseline.Any(item => Raw(item.Field) != item.Raw);
    public bool SameBaseline(CreatureSpellDraft other) => ReferenceEquals(Owner, other.Owner) && Index == other.Index && Editable == other.Editable && Baseline.SequenceEqual(other.Baseline);
    internal CreatureSpellDraft With(Dictionary<string, string> updated) => new(Owner, Index, Editable, Baseline, new ReadOnlyDictionary<string, string>(updated));
    internal Dictionary<string, string> CopyValues() => new(values);
}

public static class CreatureSpellControls
{
    public static IReadOnlyList<string> MainFields { get; } = Array.AsReadOnly(new[] { "Shot", "RechargeTime", "Range", "Attacking", "Defensive", "Creature", "CastSelf", "CastOthers", "CastCreatures" });
    public static IReadOnlyList<string> StoredFields { get; } = Array.AsReadOnly(MainFields.Concat(new[] { "ShotData1", "ShotData2" }).ToArray());
    public static bool Named(string field) => field == "Shot";
    public static bool Boolean(string field) => field is "Attacking" or "Defensive" or "Creature" or "CastSelf" or "CastOthers" or "CastCreatures";
    public static IReadOnlyList<ValueChoice> Choices(string field) => field == "Shot" ? GlobalProjectiles.Options.Select(option => option.Name).ToArray()
        : throw new ArgumentException("This creature-spell field has no source catalogue.");
    public static (string Type, string Data1, string Data2) SlotFields(string slot) => slot == "Shot" ? ("Shot", "ShotData1", "ShotData2")
        : throw new ArgumentException("Unknown creature-spell projectile slot.");
}

public sealed partial class NativeMapDocument
{
    public CreatureSpellDraft CreateCreatureSpellDraft(GlobalDefinitionRow row)
    {
        if (row.Family != "CrSpells") throw new ArgumentException("Choose a creature spell definition.");
        var values = GlobalDefinitionValues(row).ToDictionary(value => value.Name);
        if (!CreatureSpellControls.StoredFields.All(values.ContainsKey)) throw new InvalidDataException("Unverified complete spell fields.");
        return new(this, row.Index, CreatureSpellControls.StoredFields.All(field => values[field].Editable),
            Array.AsReadOnly(CreatureSpellControls.StoredFields.Select(field => (field, values[field].Value)).ToArray()),
            new ReadOnlyDictionary<string, string>(CreatureSpellControls.StoredFields.ToDictionary(field => field, field => values[field].Value)));
    }
    private GlobalDefinitionRow VerifyCreatureSpellDraft(CreatureSpellDraft draft)
    {
        if (!ReferenceEquals(draft.Owner, this)) throw new InvalidOperationException("Foreign spell draft.");
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == "CrSpells" && row.Index == draft.Index) ?? throw new ArgumentException("Unknown creature spell definition.");
        var current = CreateCreatureSpellDraft(row);
        if (!draft.Editable || !current.Editable || !draft.SameBaseline(current)) throw new InvalidOperationException("A verified unchanged spell definition is required.");
        return row;
    }
    private NativeField CreatureSpellStorage(GlobalDefinitionRow row, string field) => IsLegacyProject
        ? LegacyProjectSchemas.Fields("GLOBALCRSPELLS").Single(item => item.Name == field) : row.Record!.Fields.Single(item => item.Name == field);
    public CreatureSpellDraft UpdateCreatureSpellDraft(CreatureSpellDraft draft, IEnumerable<CreatureSpellInput> inputs)
    {
        var row = VerifyCreatureSpellDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(CreatureSpellControls.MainFields.Order())) throw new ArgumentException("Provide each main spell control exactly once.");
        var values = draft.CopyValues();
        foreach (var input in requested)
        {
            var current = draft.Raw(input.Field); var storage = CreatureSpellStorage(row, input.Field); string encoded;
            if (CreatureSpellControls.Named(input.Field))
            {
                var number = uint.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                encoded = number == uint.Parse(current, NumberStyles.Integer, CultureInfo.InvariantCulture) ? current : number.ToString(CultureInfo.InvariantCulture);
                if (encoded != current && !CreatureSpellControls.Choices(input.Field).Any(choice => choice.Value == encoded)) throw new ArgumentException("Choose an original spell value; unknown imports may only be retained.");
            }
            else if (CreatureSpellControls.Boolean(input.Field))
            {
                var number = int.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                encoded = number == int.Parse(current, NumberStyles.Integer, CultureInfo.InvariantCulture) ? current : number.ToString(CultureInfo.InvariantCulture);
                if (encoded != current && number is not (0 or 1)) throw new ArgumentOutOfRangeException(nameof(inputs), "Source checkbox requires zero or one.");
            }
            else encoded = GlobalDefinitionNumbers.Encode("CrSpells", input.Field, input.Value, current, storage);
            if (encoded != current) storage.Encode(encoded);
            values[input.Field] = encoded;
        }
        return draft.With(values);
    }
    public CreatureSpellDraft UpdateCreatureSpellShotDraft(CreatureSpellDraft draft, string slot, string data1, string data2)
    {
        var row = VerifyCreatureSpellDraft(draft); var fields = CreatureSpellControls.SlotFields(slot);
        var option = GlobalProjectiles.Find(draft.Raw(fields.Type)) ?? throw new InvalidOperationException("Choose an original projectile before submitting its options.");
        if (option.Form == 255) throw new InvalidOperationException("This projectile has no options.");
        var values = draft.CopyValues();
        string Number(string field, string input, int minimum, int maximum, bool signed)
        {
            var number = int.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (number.ToString(CultureInfo.InvariantCulture) == draft.Display(field)) return draft.Raw(field);
            if (number < minimum || number > maximum) throw new ArgumentOutOfRangeException(nameof(input), "Value is outside the original projectile option range.");
            var storage = CreatureSpellStorage(row, field);
            var encoded = signed && storage.Kind == NativeValueKind.UInt16 ? unchecked((ushort)(short)number).ToString(CultureInfo.InvariantCulture) : number.ToString(CultureInfo.InvariantCulture);
            storage.Encode(encoded); return encoded;
        }
        if (option.Form is 0 or 1) values[fields.Data1] = Number(fields.Data1, data1, option.Signed1 ? -32768 : 0, 32767, option.Signed1);
        if (option.Form == 0) values[fields.Data2] = Number(fields.Data2, data2, option.Signed2 ? -32768 : 0, 32767, option.Signed2);
        if (option.Form is 2 or 3 or 4)
        {
            var target = int.Parse(data1, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (target != draft.Raw(fields.Data1))
            {
                if (!GlobalProjectiles.Targets(option).Any(choice => choice.Value == target)) throw new ArgumentException("Choose an original creature/object target.");
                CreatureSpellStorage(row, fields.Data1).Encode(target);
            }
            values[fields.Data1] = target;
            values[fields.Data2] = option.Form switch
            {
                2 => Number(fields.Data2, data2, 0, 100, false), 3 => Number(fields.Data2, data2, 1, 10, false),
                4 => target == "1" ? Number(fields.Data2, data2, 0, 80000, false) : "0",
                _ => throw new InvalidDataException("Unknown original projectile form.")
            };
        }
        return draft.With(values);
    }
    public void ApplyCreatureSpellDraft(CreatureSpellDraft draft)
    {
        VerifyCreatureSpellDraft(draft); if (!draft.Changed) return;
        SetGlobalDefinitionValues(draft.Baseline.Where(item => draft.Raw(item.Field) != item.Raw).Select(item => new GlobalDefinitionChange("CrSpells", draft.Index, item.Field, draft.Raw(item.Field))));
    }
}
