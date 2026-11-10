using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record GlobalDoorInput(string Field, string Value);
public sealed class GlobalDoorDraft
{
    internal NativeMapDocument Owner { get; }
    public int Index { get; }
    public bool Editable { get; }
    internal IReadOnlyList<(string Field, string Raw)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal GlobalDoorDraft(NativeMapDocument owner, int index, bool editable, IReadOnlyList<(string Field, string Raw)> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Index = index; Editable = editable; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public string Display(string field) => Raw(field).Length == 0 ? "" : GlobalDefinitionNumbers.Display("Doors", field, Raw(field));
    public bool Changed => Baseline.Any(item => Raw(item.Field) != item.Raw);
    public bool SameBaseline(GlobalDoorDraft other) => ReferenceEquals(Owner, other.Owner) && Index == other.Index && Editable == other.Editable && Baseline.SequenceEqual(other.Baseline);
    internal GlobalDoorDraft With(Dictionary<string, string> updated) => new(Owner, Index, Editable, Baseline, new ReadOnlyDictionary<string, string>(updated));
}

/// <summary>Complete original door page6822. ManaCost has disabled text entry;
/// its independently enabled native spinner ignores input with a disabled buddy.
/// All nine stored values remain available to save/reset.</summary>
public static class GlobalDoorControls
{
    public static IReadOnlyList<string> Fields { get; } = Array.AsReadOnly(new[] {
        "Cost", "BuildPoints", "Health", "HealthGain", "ManaCost", "Material", "DeathEffect", "Crate", "Key" });
    public static bool Named(string field) => field is "Material" or "DeathEffect" or "Crate" or "Key";
    public static bool TextEntryEnabled(string field)
    {
        if (!Fields.Contains(field) || Named(field)) throw new ArgumentException("Unknown numeric door control.");
        return field != "ManaCost";
    }
    public static bool SpinnerInputEnabled(string field) => TextEntryEnabled(field);
    public static IReadOnlyList<ValueChoice> Choices(string field)
    {
        if (!Fields.Contains(field) || !Named(field)) throw new ArgumentException("This door field has no original catalogue.");
        return field == "DeathEffect" ? GlobalDefinitionDecorations.Choices("Doors", field) : GlobalDefinitionRules.Choices("Doors", field);
    }
    public static string Step(string field, string input, int direction)
    {
        if (!Fields.Contains(field) || Named(field) || direction is not (-1 or 1)) throw new ArgumentException("Unknown numeric door step.");
        if (!SpinnerInputEnabled(field)) throw new InvalidOperationException("The original door spinner has a disabled buddy.");
        return GlobalElementInputs.Step("Doors",field,input,direction);
    }
    internal static string Encode(string field, string input, string current, NativeField storage)
    {
        if (!Named(field)) return GlobalDefinitionNumbers.Encode("Doors", field, input, current, storage);
        var value = uint.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
        if (value == uint.Parse(current, NumberStyles.Integer, CultureInfo.InvariantCulture)) return current;
        var encoded = value.ToString(CultureInfo.InvariantCulture); storage.Encode(encoded); return encoded;
    }
}

public sealed partial class NativeMapDocument
{
    public GlobalDoorDraft CreateGlobalDoorDraft(GlobalDefinitionRow row)
    {
        if (row.Family != "Doors") throw new ArgumentException("Choose a door definition.");
        var values = GlobalDefinitionValues(row).ToDictionary(value => value.Name);
        if (!GlobalDoorControls.Fields.All(values.ContainsKey)) throw new InvalidDataException("Unverified door definition fields.");
        return new(this, row.Index, values.Values.All(value => value.Editable),
            Array.AsReadOnly(GlobalDoorControls.Fields.Select(field => (field, values[field].Value)).ToArray()),
            new ReadOnlyDictionary<string, string>(GlobalDoorControls.Fields.ToDictionary(field => field, field => values[field].Value)));
    }
    private GlobalDefinitionRow VerifyGlobalDoorDraft(GlobalDoorDraft draft)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == "Doors" && row.Index == draft.Index) ?? throw new ArgumentException("Unknown door definition.");
        var current = CreateGlobalDoorDraft(row);
        if (!draft.Editable || !current.Editable || !draft.SameBaseline(current)) throw new InvalidOperationException("A verified unchanged door definition is required.");
        return row;
    }
    public GlobalDoorDraft UpdateGlobalDoorDraft(GlobalDoorDraft draft, IEnumerable<GlobalDoorInput> inputs)
    {
        var row = VerifyGlobalDoorDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(GlobalDoorControls.Fields.Order())) throw new ArgumentException("Provide each door control exactly once.");
        var values = new Dictionary<string, string>();
        foreach (var input in requested)
        {
            var storage = IsLegacyProject ? LegacyProjectSchemas.Fields("GLOBALDOOR").Single(field => field.Name == input.Field) : row.Record!.Fields.Single(field => field.Name == input.Field);
            var encoded = GlobalDoorControls.Encode(input.Field, input.Value, draft.Raw(input.Field), storage);
            if (encoded != draft.Raw(input.Field))
            {
                if (GlobalDoorControls.Named(input.Field) && !GlobalDoorControls.Choices(input.Field).Any(choice => choice.Value == encoded))
                    throw new ArgumentException("Choose an original door value; unknown imports may only be retained.");
            }
            values.Add(input.Field, encoded);
        }
        return draft.With(values);
    }
    public void ApplyGlobalDoorDraft(GlobalDoorDraft draft)
    {
        VerifyGlobalDoorDraft(draft);
        if (!draft.Changed) return;
        SetGlobalDefinitionValues(draft.Baseline.Where(item => draft.Raw(item.Field) != item.Raw).Select(item => new GlobalDefinitionChange("Doors", draft.Index, item.Field, draft.Raw(item.Field))));
    }
}
