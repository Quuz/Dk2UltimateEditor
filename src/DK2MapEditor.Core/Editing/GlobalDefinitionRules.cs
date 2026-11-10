using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record GlobalRuleInput(string Field, string Value);

/// <summary>Complete named-rule subset, independent of numeric/projectile fields.</summary>
public sealed class GlobalRulesDraft
{
    internal NativeMapDocument Owner { get; }
    public string Family { get; }
    public int Index { get; }
    public bool Editable { get; }
    internal IReadOnlyList<(string Field, string Raw)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal GlobalRulesDraft(NativeMapDocument owner, string family, int index, bool editable, IReadOnlyList<(string Field, string Raw)> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Family = family; Index = index; Editable = editable; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public bool Changed => Baseline.Any(item => Raw(item.Field) != item.Raw);
    public bool SameBaseline(GlobalRulesDraft other) => ReferenceEquals(Owner, other.Owner) && Family == other.Family && Index == other.Index && Editable == other.Editable && Baseline.SequenceEqual(other.Baseline);
    internal GlobalRulesDraft With(Dictionary<string, string> updated) => new(Owner, Family, Index, Editable, Baseline, new ReadOnlyDictionary<string, string>(updated));
}

public static class GlobalDefinitionRules
{
    public static IReadOnlyList<string> Families { get; } = Array.AsReadOnly(new[] { "Doors", "Traps", "Spells" });
    public static IReadOnlyList<string> Fields(string family) => family switch
    {
        "Doors" => ["Material", "Crate", "Key"], "Traps" => ["Material", "Crate", "TriggerType", "TriggerData"],
        "Spells" => ["HandAnim", "NoGoHandAnim", "CastRule", "TargetRule"], _ => throw new ArgumentException("Unknown named-rule family.")
    };
    private static string? Catalogue(string family, string field) => (family, field) switch
    {
        ("Doors" or "Traps", "Material") => "GDoorsMats", ("Doors", "Crate") => "GDoorsCrates", ("Doors", "Key") => "GDoorsKeys",
        ("Traps", "Crate") => "GTrapsCrates", ("Traps", "TriggerType") => "GTrapsTypes", ("Spells", "HandAnim" or "NoGoHandAnim") => "GSpellsHand",
        ("Spells", "CastRule") => "GSpellsCast", ("Spells", "TargetRule") => "GSpellsTrget", _ => null
    };
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<ValueChoice>>> catalogues = new(() =>
    {
        (string Name, uint[] IDs, string? Zero)[] lists = [
            ("GDoorsMats", [1,2,3,4,5,6,7], "szEffect000"), ("GDoorsCrates", [52,53,54,55,56,57], "szEffect000"),
            ("GDoorsKeys", [17,104,105,106,107,137], "szGlobalNoKey"), ("GTrapsCrates", [5,36,37,38,39,92,40,41,42,43,44,138], "szEffect000"),
            ("GTrapsTypes", [1,2,3], null), ("GSpellsHand", [1], "szSpellHand00"), ("GSpellsCast", [1,2,3,4,5], null), ("GSpellsTrget", [1,2,3,4,5,6], null) ];
        return new ReadOnlyDictionary<string, IReadOnlyList<ValueChoice>>(lists.ToDictionary(list => list.Name, list =>
        {
            var entries = LegacyCompilerRules.Elements(list.Name).Where(entry => entry.ID != uint.MaxValue).ToArray();
            if (!entries.Select(entry => entry.ID).SequenceEqual(list.IDs)) throw new InvalidDataException("Unverified original named rule catalogue: " + list.Name);
            if (list.Zero is not null) entries = [new(0, 0, 0, list.Zero), .. entries]; // Zero is selectable only in FFFFFFFF-terminated lists.
            return (IReadOnlyList<ValueChoice>)Array.AsReadOnly(entries.Select(entry => ValueCatalogues.Named(entry, entry.ID.ToString(CultureInfo.InvariantCulture))).ToArray());
        }));
    });
    public static IReadOnlyList<ValueChoice> Choices(string family, string field) => Catalogue(family, field) is { } catalogue ? catalogues.Value[catalogue] : [];
    public static bool KnownTrigger(string type) => type is "1" or "2" or "3";
    public static bool Enabled(string family, int index, string field)
    {
        if (!Fields(family).Contains(field)) throw new ArgumentException("Unknown named rule field.");
        return family != "Traps" || GlobalTrapControls.Enabled(index, field);
    }
}

public sealed partial class NativeMapDocument
{
    public GlobalRulesDraft CreateGlobalRulesDraft(GlobalDefinitionRow row)
    {
        var names = GlobalDefinitionRules.Fields(row.Family); var values = GlobalDefinitionValues(row).Where(value => names.Contains(value.Name)).ToDictionary(value => value.Name);
        if (values.Count != names.Count) throw new InvalidDataException("Unverified named-rule fields.");
        return new(this, row.Family, row.Index, values.Values.All(value => value.Editable), Array.AsReadOnly(names.Select(field => (field, values[field].Value)).ToArray()), new ReadOnlyDictionary<string, string>(values.ToDictionary(item => item.Key, item => item.Value.Value)));
    }
    private GlobalDefinitionRow VerifyGlobalRulesDraft(GlobalRulesDraft draft)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == draft.Family && row.Index == draft.Index) ?? throw new ArgumentException("Unknown named-rule definition.");
        var current = CreateGlobalRulesDraft(row);
        if (!draft.Editable || !current.Editable) throw new InvalidOperationException("A verified current named-rule layout is required.");
        if (!draft.SameBaseline(current)) throw new InvalidOperationException("Named rules changed after the draft was opened.");
        return row;
    }
    public GlobalRulesDraft UpdateGlobalRules(GlobalRulesDraft draft, IEnumerable<GlobalRuleInput> inputs)
    {
        var row = VerifyGlobalRulesDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(GlobalDefinitionRules.Fields(draft.Family).Order())) throw new ArgumentException("Provide each named-rule field exactly once.");
        var trigger = draft.Family == "Traps" ? uint.Parse(requested.Single(item => item.Field == "TriggerType").Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) : null;
        var values = new Dictionary<string, string>();
        foreach (var input in requested)
        {
            var raw = draft.Raw(input.Field); string encoded;
            if (input.Field == "TriggerData")
            {
                encoded = int.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                var value = int.Parse(encoded, CultureInfo.InvariantCulture);
                if (encoded != raw && (!GlobalDefinitionRules.KnownTrigger(trigger!) || value is < 0 or > 32767)) throw new ArgumentOutOfRangeException(nameof(inputs), "Choose a known trigger type and an integer parameter 0–32767.");
            }
            else
            {
                encoded = uint.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                if (encoded != raw && !GlobalDefinitionRules.Choices(draft.Family, input.Field).Any(choice => choice.Value == encoded)) throw new ArgumentException("Choose a source rule; unknown imported IDs may only be retained.");
            }
            var storage = IsLegacyProject ? LegacyProjectSchemas.Fields(GlobalDefinitionDefaults.Structure(draft.Family)).Single(field => field.Name == input.Field) : row.Record!.Fields.Single(field => field.Name == input.Field);
            if (encoded != raw)
            {
                if (!GlobalDefinitionRules.Enabled(draft.Family, draft.Index, input.Field)) throw new InvalidOperationException("This original trap control is inactive.");
                storage.Encode(encoded);
            }
            values.Add(input.Field, encoded);
        }
        return draft.With(values);
    }
    public void ApplyGlobalRules(GlobalRulesDraft draft)
    {
        VerifyGlobalRulesDraft(draft); if (!draft.Changed) return;
        SetGlobalDefinitionValues(draft.Baseline.Where(item => draft.Raw(item.Field) != item.Raw).Select(item => new GlobalDefinitionChange(draft.Family, draft.Index, item.Field, draft.Raw(item.Field))));
    }
}
