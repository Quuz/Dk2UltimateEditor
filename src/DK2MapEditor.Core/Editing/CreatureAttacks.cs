using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record CreatureAttackOption(uint ID, ValueChoice Name);
public sealed record CreatureAttackInput(string Field, string Value);

/// <summary>Immutable 23-field draft; source display units remain separate from stored bits.</summary>
public sealed class CreatureAttacksDraft
{
    public int Index { get; }
    public bool Editable { get; }
    public bool Native { get; }
    internal NativeMapDocument Owner { get; }
    internal IReadOnlyList<(string Name, string Value)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal CreatureAttacksDraft(NativeMapDocument owner, int index, bool editable, bool native, IReadOnlyList<(string Name, string Value)> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Index = index; Editable = editable; Native = native; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public string Display(string field) => GlobalDefinitionNumbers.Display("Creatures", field, Raw(field));
    public bool Changed => Baseline.Any(item => Raw(item.Name) != item.Value);
    public bool SameBaseline(CreatureAttacksDraft other) => ReferenceEquals(Owner, other.Owner) && Index == other.Index && Editable == other.Editable && Native == other.Native && Baseline.SequenceEqual(other.Baseline);
    internal CreatureAttacksDraft With(IReadOnlyDictionary<string, string> changes) => new(Owner, Index, Editable, Native, Baseline,
        new ReadOnlyDictionary<string, string>(values.ToDictionary(item => item.Key, item => changes.GetValueOrDefault(item.Key, item.Value))));
}

public static class CreatureAttacks
{
    public static IReadOnlyList<string> Slots { get; } = Array.AsReadOnly(new[] { "Shot1", "Shot2", "Shot3", "Melee" });
    public static string TypeField(string slot) => Slots.Contains(slot) ? slot + "ID" : throw new ArgumentException("Unknown creature attack slot.");
    public static IReadOnlyList<string> Parameters(string slot)
    {
        TypeField(slot);
        return slot == "Melee" ? ["RechargeTime", "Range", "Dammage", "Delay"] : [slot + "Level", slot + "Delay", slot + "X", slot + "Y", slot + "Z"];
    }
    private static readonly Lazy<IReadOnlyList<CreatureAttackOption>> ranged = new(() => Read(false));
    private static readonly Lazy<IReadOnlyList<CreatureAttackOption>> melee = new(() => Read(true));
    public static IReadOnlyList<CreatureAttackOption> Options(string slot) { TypeField(slot); return slot == "Melee" ? melee.Value : ranged.Value; }
    private static IReadOnlyList<CreatureAttackOption> Read(bool closeCombat)
    {
        var items = LegacyCompilerRules.Elements(closeCombat ? "GCrMelee" : "GCrSpells").Where(item => item.ID != uint.MaxValue).ToArray();
        if (items.Length != 41 || !items.Select(item => item.ID).Order().SequenceEqual(Enumerable.Range(1, 41).Select(id => (uint)id))) throw new InvalidDataException("Unverified original creature attack catalogue.");
        if (!closeCombat) items = [new(0, 0, 0, "szEffect000"), .. items]; // Explicit first GCrSpells row; zero is not its FFFFFFFF sentinel.
        return Array.AsReadOnly(items.Select(item => new CreatureAttackOption(item.ID, ValueCatalogues.Named(item, item.ID.ToString(CultureInfo.InvariantCulture)))).ToArray());
    }
    public static bool IsLevel(string field) => field is "Shot1Level" or "Shot2Level" or "Shot3Level";
    public static GlobalDefinitionNumberFormat Format(string field, bool native) => IsLevel(field) ? new(false, false, false, 0, 10) : field == "Dammage" ? new(false, false, false, 0, 32767) : GlobalDefinitionNumbers.Format("Creatures", field, native);
}

public sealed partial class NativeMapDocument
{
    public CreatureAttacksDraft CreateCreatureAttacksDraft(GlobalDefinitionRow row)
    {
        if (row.Family != "Creatures") throw new ArgumentException("Creature definition required.");
        var values = GlobalDefinitionValues(row).Where(value => value.Section == "Attacks").ToArray();
        if (values.Length != 23 || !CreatureAttacks.Slots.SelectMany(slot => new[] { CreatureAttacks.TypeField(slot) }.Concat(CreatureAttacks.Parameters(slot))).Order().SequenceEqual(values.Select(value => value.Name).Order())) throw new InvalidDataException("Unverified creature attack layout.");
        return new(this, row.Index, values.All(value => value.Editable), !IsLegacyProject, Array.AsReadOnly(values.Select(value => (value.Name, value.Value)).ToArray()), new ReadOnlyDictionary<string, string>(values.ToDictionary(value => value.Name, value => value.Value)));
    }
    private GlobalDefinitionRow VerifyCreatureAttacksDraft(CreatureAttacksDraft draft)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == "Creatures" && row.Index == draft.Index) ?? throw new ArgumentException("Unknown creature definition.");
        var current = CreateCreatureAttacksDraft(row);
        if (!draft.Editable || !current.Editable) throw new InvalidOperationException("A verified current creature attack layout is required.");
        if (!draft.SameBaseline(current)) throw new InvalidOperationException("Creature attacks changed after this draft was opened. Reload the current values.");
        return row;
    }
    public CreatureAttacksDraft UpdateCreatureAttack(CreatureAttacksDraft draft, string slot, string type, IEnumerable<CreatureAttackInput> inputs)
    {
        var row = VerifyCreatureAttacksDraft(draft); var typeField = CreatureAttacks.TypeField(slot); var parameters = CreatureAttacks.Parameters(slot); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(parameters.Order())) throw new ArgumentException("Provide each parameter of this attack slot exactly once.");
        type = uint.Parse(type, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        if (type != draft.Raw(typeField) && !CreatureAttacks.Options(slot).Any(option => option.Name.Value == type)) throw new ArgumentException("Choose a source attack type; unknown imported types may only be retained.");
        var changes = new Dictionary<string, string> { [typeField] = type };
        // None only disables the controls. The original SaveChangesCr still
        // copies their current contents, including edits made before choosing
        // None. Do not zero or discard those pending parameter values.
        foreach (var input in requested)
        {
            var storage = IsLegacyProject ? LegacyProjectSchemas.Fields("GLOBALCREATURE").Single(field => field.Name == input.Field) : row.Record!.Fields.Single(field => field.Name == input.Field);
            var raw = draft.Raw(input.Field);
            if (CreatureAttacks.IsLevel(input.Field) || input.Field == "Dammage")
            {
                var number = int.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture); var canonical = number.ToString(CultureInfo.InvariantCulture);
                if (canonical != raw && (number < 0 || number > (input.Field == "Dammage" ? 32767 : 10))) throw new ArgumentOutOfRangeException(nameof(inputs), "Attack level must be 0–10 and melee damage 0–32767.");
                storage.Encode(canonical); changes.Add(input.Field, canonical);
            }
            else changes.Add(input.Field, GlobalDefinitionNumbers.Encode("Creatures", input.Field, input.Value, raw, storage));
        }
        return draft.With(changes);
    }
    public void ApplyCreatureAttacks(CreatureAttacksDraft draft)
    {
        VerifyCreatureAttacksDraft(draft); if (!draft.Changed) return;
        SetGlobalDefinitionValues(draft.Baseline.Select(item => new GlobalDefinitionChange("Creatures", draft.Index, item.Name, draft.Raw(item.Name))));
    }
}
