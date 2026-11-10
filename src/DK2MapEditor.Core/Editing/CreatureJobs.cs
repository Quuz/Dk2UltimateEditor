using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record CreatureJobGroup(string ID, string Prefix, int Capacity, bool HasParameters);
public sealed record CreatureJobOption(uint ID, ValueChoice Name, bool Happy, bool Unhappy, bool Angry, bool Hate);
/// <summary>Stored integers, not display-normalized values. Moving a slot retains its whole tuple.</summary>
public sealed record CreatureJobSlot(string Job, string Chance, string Mood, string Mana);
public sealed record CreatureJobTotal(string Group, long Total) { public bool NeedsAcknowledgement => Total is not (0 or 100); }

/// <summary>Immutable, source-validated draft. Browsing and drafting do not change the document.</summary>
public sealed class CreatureJobsDraft
{
    public int Index { get; }
    public bool Editable { get; }
    public bool Native { get; }
    internal IReadOnlyList<(string Name, string Value)> Baseline { get; }
    internal NativeMapDocument Owner { get; }
    private readonly IReadOnlyDictionary<string, IReadOnlyList<CreatureJobSlot>> groups;
    internal CreatureJobsDraft(NativeMapDocument owner, int index, bool editable, bool native, IReadOnlyList<(string Name, string Value)> baseline, IReadOnlyDictionary<string, IReadOnlyList<CreatureJobSlot>> groups)
    { Owner = owner; Index = index; Editable = editable; Native = native; Baseline = baseline; this.groups = groups; }
    public IReadOnlyList<CreatureJobSlot> Slots(string group) => groups[CreatureJobs.Group(group).ID];
    internal CreatureJobsDraft With(string group, IEnumerable<CreatureJobSlot> slots) => new(Owner, Index, Editable, Native, Baseline,
        CreatureJobs.Groups.ToDictionary(item => item.ID, item => item.ID == group ? (IReadOnlyList<CreatureJobSlot>)Array.AsReadOnly(slots.ToArray()) : groups[item.ID]));
    public bool SameBaseline(CreatureJobsDraft other) => ReferenceEquals(Owner, other.Owner) && Index == other.Index && Native == other.Native && Editable == other.Editable && Baseline.SequenceEqual(other.Baseline);
    public bool Changed => CreatureJobs.Groups.Any(group => Slots(group.ID).Where((slot, index) => CreatureJobs.Fields(group, index).Zip(CreatureJobs.Values(group, slot)).Any(pair => Baseline.Single(item => item.Name == pair.First).Value != pair.Second)).Any());
    public IReadOnlyList<CreatureJobTotal> Totals => CreatureJobs.Groups.Where(group => group.HasParameters).Select(group => new CreatureJobTotal(group.ID,
        Slots(group.ID).Where(slot => slot.Job != "0" && slot.Job.Length > 0).Sum(slot => long.Parse(slot.Chance, CultureInfo.InvariantCulture)))).ToArray();
}

public static class CreatureJobs
{
    public static IReadOnlyList<CreatureJobGroup> Groups { get; } = Array.AsReadOnly(new CreatureJobGroup[] { new("Happy", "H", 3, true), new("Unhappy", "U", 2, true), new("Angry", "A", 3, true), new("Hate", "N", 4, false) });
    private static readonly Lazy<IReadOnlyList<CreatureJobOption>> options = new(() =>
    {
        var source = LegacyCompilerRules.Elements("GCrJobs");
        if (source.Count != 22 || source.Any(item => item.MinWidth is not (0 or 1) || item.MinHeight is not (0 or 1) || item.MaxWidth is not (0 or 1) || item.MaxHeight is not (0 or 1))) throw new InvalidDataException("Unverified original creature jobs.");
        return Array.AsReadOnly(source.Select(item => new CreatureJobOption(item.ID, ValueCatalogues.Named(item, item.ID.ToString(CultureInfo.InvariantCulture)), item.MinWidth != 0, item.MinHeight != 0, item.MaxWidth != 0, item.MaxHeight != 0)).ToArray());
    });
    public static IReadOnlyList<CreatureJobOption> Options => options.Value;
    public static CreatureJobGroup Group(string group) => Groups.SingleOrDefault(item => item.ID == group) ?? throw new ArgumentException("Unknown creature job group.");
    public static IReadOnlyList<CreatureJobOption> Allowed(string group) => Options.Where(item => Group(group).ID switch { "Happy" => item.Happy, "Unhappy" => item.Unhappy, "Angry" => item.Angry, _ => item.Hate }).ToArray();
    public static string DisplaySigned(string raw) => int.TryParse(raw, CultureInfo.InvariantCulture, out var value) && value is >= 32768 and <= 65535 ? (value - 65536).ToString(CultureInfo.InvariantCulture) : raw;
    internal static string[] Fields(CreatureJobGroup group, int slot) => group.HasParameters ? [$"{group.Prefix}Jobs[{slot}]", $"{group.Prefix}JobsChance[{slot}]", $"{group.Prefix}JobsMood[{slot}]", $"{group.Prefix}JobsMana[{slot}]"] : [$"{group.Prefix}Jobs[{slot}]"];
    internal static string[] Values(CreatureJobGroup group, CreatureJobSlot slot) => group.HasParameters ? [slot.Job, slot.Chance, slot.Mood, slot.Mana] : [slot.Job];
    internal static readonly CreatureJobSlot Empty = new("0", "0", "0", "0");
}

public sealed partial class NativeMapDocument
{
    public CreatureJobsDraft CreateCreatureJobsDraft(GlobalDefinitionRow row)
    {
        if (row.Family != "Creatures") throw new ArgumentException("Creature definition required.");
        var values = GlobalDefinitionValues(row).Where(value => value.Section == "Jobs").ToArray();
        if (values.Length != 36) throw new InvalidDataException("Unverified creature jobs layout.");
        string Read(string field) => values.Single(value => value.Name == field).Value;
        var groups = CreatureJobs.Groups.ToDictionary(group => group.ID, group => (IReadOnlyList<CreatureJobSlot>)Array.AsReadOnly(Enumerable.Range(0, group.Capacity).Select(index =>
        {
            var fields = CreatureJobs.Fields(group, index);
            return group.HasParameters ? new CreatureJobSlot(Read(fields[0]), Read(fields[1]), Read(fields[2]), Read(fields[3])) : new CreatureJobSlot(Read(fields[0]), "0", "0", "0");
        }).ToArray()));
        return new(this, row.Index, values.All(value => value.Editable), !IsLegacyProject, Array.AsReadOnly(values.Select(value => (value.Name, value.Value)).ToArray()), groups);
    }
    private void VerifyCreatureJobsDraft(CreatureJobsDraft draft)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == "Creatures" && row.Index == draft.Index) ?? throw new ArgumentException("Unknown creature definition.");
        var current = CreateCreatureJobsDraft(row);
        if (!draft.Editable || !current.Editable) throw new InvalidOperationException("A verified current creature definition layout is required.");
        if (!draft.SameBaseline(current)) throw new InvalidOperationException("Creature jobs changed after this draft was opened. Reload the current values.");
    }
    public CreatureJobsDraft AddCreatureJob(CreatureJobsDraft draft, string group, uint job, string chance, string mood, string mana)
    {
        VerifyCreatureJobsDraft(draft);
        var slots = draft.Slots(group); var free = Enumerable.Range(0, slots.Count).FirstOrDefault(index => slots[index].Job == "0", -1);
        if (free < 0) throw new InvalidOperationException("Creature job group is full.");
        return ChangeCreatureJob(draft, group, free, job.ToString(CultureInfo.InvariantCulture), chance, mood, mana);
    }
    public CreatureJobsDraft ChangeCreatureJob(CreatureJobsDraft draft, string group, int slot, string job, string chance, string mood, string mana)
    {
        VerifyCreatureJobsDraft(draft); var definition = CreatureJobs.Group(group); var slots = draft.Slots(group);
        if (slot < 0 || slot >= slots.Count) throw new ArgumentOutOfRangeException(nameof(slot));
        // Project job IDs are UINT while native compiler fields are LONG.
        // Keep both full unsigned project IDs and negative native imports.
        var old = slots[slot]; job = long.Parse(job, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        if (job == "0") throw new ArgumentException("Use Remove to clear an occupied job slot.");
        if (job != old.Job && (!CreatureJobs.Allowed(group).Any(item => item.Name.Value == job) || slots.Where((_, index) => index != slot).Any(item => item.Job == job))) throw new ArgumentException("Choose an allowed, unused job for this group.");
        string Number(string text, string raw, bool signed)
        {
            var value = int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (value.ToString(CultureInfo.InvariantCulture) == (signed ? CreatureJobs.DisplaySigned(raw) : raw)) return raw;
            if (value < (signed ? -32767 : 0) || value > (signed ? 32767 : 100)) throw new ArgumentOutOfRangeException(nameof(text), "Job chance must be 0–100; mood and mana must be −32767–32767.");
            return signed && draft.Native ? unchecked((ushort)(short)value).ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
        }
        // A new job does not inherit dormant data from its formerly empty slot.
        var basis = old.Job == "0" ? CreatureJobs.Empty : old;
        var replacement = definition.HasParameters ? new CreatureJobSlot(job, Number(chance, basis.Chance, false), Number(mood, basis.Mood, true), Number(mana, basis.Mana, true)) : new CreatureJobSlot(job, "0", "0", "0");
        return draft.With(group, slots.Select((item, index) => index == slot ? replacement : item));
    }
    public CreatureJobsDraft RemoveCreatureJobs(CreatureJobsDraft draft, string group, IEnumerable<int> slots)
    {
        VerifyCreatureJobsDraft(draft); var current = draft.Slots(group); var removed = slots.Distinct().ToArray();
        if (removed.Any(index => index < 0 || index >= current.Count || current[index].Job == "0")) throw new ArgumentException("Select occupied job slots.");
        // Match the original left-shift operation, including any existing holes
        // and their inactive tuple data, then append its zero sentinel values.
        return draft.With(group, current.Where((_, index) => !removed.Contains(index)).Concat(Enumerable.Repeat(CreatureJobs.Empty, removed.Length)));
    }
    public void ApplyCreatureJobs(CreatureJobsDraft draft, bool acknowledgeNonstandardTotals = false)
    {
        VerifyCreatureJobsDraft(draft);
        if (!draft.Changed) return;
        if (!acknowledgeNonstandardTotals && draft.Totals.Any(total => total.NeedsAcknowledgement)) throw new InvalidOperationException("A job probability total differs from 0 or 100%. Acknowledge it explicitly to save, as in the original editor.");
        SetGlobalDefinitionValues(CreatureJobs.Groups.SelectMany(group => draft.Slots(group.ID).SelectMany((slot, index) => CreatureJobs.Fields(group, index).Zip(CreatureJobs.Values(group, slot), (name, value) => new GlobalDefinitionChange("Creatures", draft.Index, name, value)))).ToArray());
    }
}
