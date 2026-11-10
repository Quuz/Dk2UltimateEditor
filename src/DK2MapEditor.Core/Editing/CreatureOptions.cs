using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record CreatureOptionInput(string Field, string Value);

/// <summary>Immutable complete source-group draft. Unknown imported IDs are not repairs.</summary>
public sealed class CreatureOptionsDraft
{
    internal NativeMapDocument Owner { get; }
    public int Index { get; }
    public string Group { get; }
    public bool Editable { get; }
    public bool Native { get; }
    internal IReadOnlyList<(string Field, string Raw)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal CreatureOptionsDraft(NativeMapDocument owner, int index, string group, bool editable, bool native, IReadOnlyList<(string Field, string Raw)> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Index = index; Group = group; Editable = editable; Native = native; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public string Display(string field) => GlobalDefinitionNumbers.Display("Creatures", field, Raw(field));
    public bool Changed => Baseline.Any(item => Raw(item.Field) != item.Raw);
    public bool SameBaseline(CreatureOptionsDraft other) => ReferenceEquals(Owner, other.Owner) && Index == other.Index && Group == other.Group && Native == other.Native && Editable == other.Editable && Baseline.SequenceEqual(other.Baseline);
    internal CreatureOptionsDraft With(Dictionary<string, string> updated) => new(Owner, Index, Group, Editable, Native, Baseline, new ReadOnlyDictionary<string, string>(updated));
}

public static class CreatureOptions
{
    public static IReadOnlyList<string> Groups { get; } = Array.AsReadOnly(new[] { "Resistances", "Possession" });
    public static IReadOnlyList<string> Fields(string group) => group switch
    {
        "Resistances" => ["Res1", "Res1Rate", "Res2", "Res2Rate", "Res3", "Res3Rate", "Res4", "Res4Rate"],
        "Possession" => ["SpecialAb1", "SpecialAb1Data", "SpecialAb2", "SpecialAb2Data", "Melee1", "Melee2", "Special1", "Special2", "Spell", "GammaEffect", "WaddleScale", "OscillateScale", "WalkScale", "CameraRolls"],
        _ => throw new ArgumentException("Unknown creature options group.")
    };
    private static string? Catalogue(string field) => field switch
    {
        "Res1" or "Res2" or "Res3" or "Res4" => "GCrRes",
        "SpecialAb1" or "SpecialAb2" => "GCrAbilities",
        "Melee1" or "Melee2" or "Special1" or "Special2" or "Spell" => "GCrSwipes",
        "GammaEffect" => "GCrGammas", _ => null
    };
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<ValueChoice>>> catalogues = new(() =>
        new ReadOnlyDictionary<string, IReadOnlyList<ValueChoice>>(new[] { ("GCrRes", 32), ("GCrAbilities", 10), ("GCrSwipes", 22), ("GCrGammas", 16) }.ToDictionary(item => item.Item1, item =>
        {
            var entries = LegacyCompilerRules.Elements(item.Item1).Where(entry => entry.ID != uint.MaxValue).ToArray();
            if (!entries.Select(entry => entry.ID).Order().SequenceEqual(Enumerable.Range(1, item.Item2).Select(id => (uint)id))) throw new InvalidDataException("Unverified source creature option catalogue: " + item.Item1);
            // These four lists terminate with FFFFFFFF, not their first zero row.
            entries = [new(0, 0, 0, item.Item1 == "GCrGammas" ? "szGamma00" : "szResistance00"), .. entries];
            return (IReadOnlyList<ValueChoice>)Array.AsReadOnly(entries.Select(entry => ValueCatalogues.Named(entry, entry.ID.ToString(CultureInfo.InvariantCulture))).ToArray());
        })));
    public static IReadOnlyList<ValueChoice> Choices(string field) => Catalogue(field) is { } name ? catalogues.Value[name] : [];
    public static GlobalDefinitionNumberFormat Format(string field, bool native) => field switch
    {
        "SpecialAb1Data" or "SpecialAb2Data" => new(false, false, false, 0, 32767),
        "WalkScale" => new(false, false, false, 0, 12), "CameraRolls" => new(false, false, false, 0, 1),
        _ => GlobalDefinitionNumbers.Format("Creatures", field, native)
    };
}

public sealed partial class NativeMapDocument
{
    public CreatureOptionsDraft CreateCreatureOptionsDraft(GlobalDefinitionRow row, string group)
    {
        if (row.Family != "Creatures") throw new ArgumentException("Creature definition required.");
        var fields = CreatureOptions.Fields(group); var values = GlobalDefinitionValues(row).Where(value => value.Section == group).ToDictionary(value => value.Name);
        if (!fields.Order().SequenceEqual(values.Keys.Order())) throw new InvalidDataException("Unverified creature option group layout.");
        return new(this, row.Index, group, values.Values.All(value => value.Editable), !IsLegacyProject, Array.AsReadOnly(fields.Select(field => (field, values[field].Value)).ToArray()), new ReadOnlyDictionary<string, string>(values.ToDictionary(item => item.Key, item => item.Value.Value)));
    }
    private GlobalDefinitionRow VerifyCreatureOptionsDraft(CreatureOptionsDraft draft)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == "Creatures" && row.Index == draft.Index) ?? throw new ArgumentException("Unknown creature definition.");
        var current = CreateCreatureOptionsDraft(row, draft.Group);
        if (!draft.Editable || !current.Editable) throw new InvalidOperationException("A verified current creature layout is required.");
        if (!draft.SameBaseline(current)) throw new InvalidOperationException("Creature options changed after the draft was opened.");
        return row;
    }
    public CreatureOptionsDraft UpdateCreatureOptions(CreatureOptionsDraft draft, IEnumerable<CreatureOptionInput> inputs)
    {
        var row = VerifyCreatureOptionsDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(CreatureOptions.Fields(draft.Group).Order())) throw new ArgumentException("Provide each group field exactly once.");
        var values = new Dictionary<string, string>();
        foreach (var input in requested)
        {
            var storage = IsLegacyProject ? LegacyProjectSchemas.Fields("GLOBALCREATURE").Single(field => field.Name == input.Field) : row.Record!.Fields.Single(field => field.Name == input.Field);
            var raw = draft.Raw(input.Field); var choices = CreatureOptions.Choices(input.Field);
            string encoded;
            if (choices.Count > 0)
            {
                encoded = uint.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                if (encoded != raw && !choices.Any(choice => choice.Value == encoded)) throw new ArgumentException("Choose a source option; unknown imported IDs may only be retained.");
                if (encoded != raw) storage.Encode(encoded);
            }
            else if (input.Field is "SpecialAb1Data" or "SpecialAb2Data" or "WalkScale" or "CameraRolls")
            {
                encoded = int.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                var number = int.Parse(encoded, CultureInfo.InvariantCulture); var format = CreatureOptions.Format(input.Field, !IsLegacyProject);
                if (encoded != raw && (number < format.Minimum || number > format.Maximum)) throw new ArgumentOutOfRangeException(nameof(inputs), "Value is outside the original control's range.");
                if (encoded != raw) storage.Encode(encoded);
            }
            else encoded = GlobalDefinitionNumbers.Encode("Creatures", input.Field, input.Value, raw, storage);
            values.Add(input.Field, encoded);
        }
        // Original SaveChangesCr stores all rates/data, even disabled None rates.
        return draft.With(values);
    }
    public void ApplyCreatureOptions(CreatureOptionsDraft draft)
    {
        VerifyCreatureOptionsDraft(draft); if (!draft.Changed) return;
        SetGlobalDefinitionValues(draft.Baseline.Where(item => draft.Raw(item.Field) != item.Raw).Select(item => new GlobalDefinitionChange("Creatures", draft.Index, item.Field, draft.Raw(item.Field))));
    }
}
