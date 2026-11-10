using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record GlobalTerrainInput(string Field, string Value);
public sealed class GlobalTerrainDraft
{
    internal NativeMapDocument Owner { get; }
    public int Index { get; }
    public bool Editable { get; }
    internal IReadOnlyList<(string Field, string Raw)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal GlobalTerrainDraft(NativeMapDocument owner, int index, bool editable, IReadOnlyList<(string Field, string Raw)> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Index = index; Editable = editable; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public string Display(string field) => Raw(field).Length == 0 ? "" : GlobalDefinitionNumbers.Display("Terrains", field, Raw(field));
    public bool Changed => Baseline.Any(item => Raw(item.Field) != item.Raw);
    public bool SameBaseline(GlobalTerrainDraft other) => ReferenceEquals(Owner, other.Owner) && Index == other.Index && Editable == other.Editable && Baseline.SequenceEqual(other.Baseline);
    internal GlobalTerrainDraft With(Dictionary<string, string> updated) => new(Owner, Index, Editable, Baseline, new ReadOnlyDictionary<string, string>(updated));
}

/// <summary>All controls from original terrain page6820, including source
/// availability, signed/unsigned integer limits, RGB and explicit Save cleanup.</summary>
public static class GlobalTerrainControls
{
    public static IReadOnlyList<string> Fields { get; } = Array.AsReadOnly(new[] {
        "MaxHealthType", "DestroyedType", "MaxHealthEffect", "DestroyedEffect",
        "Damage", "GoldValue", "StartingHealth", "MaxHealth", "ManaGain", "MaxManaGain", "WibbleH", "WibbleV",
        "Visible", "TerrainLight", "AmbientLight", "TerrainLightR", "TerrainLightG", "TerrainLightB",
        "AmbientLightR", "AmbientLightG", "AmbientLightB", "TerrainLightH" });
    private static readonly Lazy<IReadOnlyList<LegacyCompilerRules.Element>> terrains = new(() =>
    {
        var rows = LegacyCompilerRules.Elements("GTerrains");
        if (rows.Count != 38 || !rows.Select(row => row.ID).SequenceEqual(GlobalDefinitionDefaults.Catalogue("Terrains").Select(row => row.ID)))
            throw new InvalidDataException("Unverified original terrain control order.");
        return rows;
    });
    private static LegacyCompilerRules.Element Terrain(int index) => terrains.Value[index];
    private static readonly Lazy<IReadOnlyDictionary<uint, ValueChoice>> names = new(() =>
    {
        using var stream = typeof(GlobalTerrainControls).Assembly.GetManifestResourceStream("Editor.GlobalTerrainNames")!;
        using var reader = new StreamReader(stream); var values = new Dictionary<uint, ValueChoice>();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var parts = line.Split('|');
            if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace) || !values.TryAdd(uint.Parse(parts[0], CultureInfo.InvariantCulture), new(parts[0], parts[1], parts[2])))
                throw new InvalidDataException("Invalid bilingual terrain name.");
        }
        if (values.Count != 38 || !terrains.Value.All(row => values.ContainsKey(row.ID))) throw new InvalidDataException("Incomplete terrain vocabulary.");
        return new ReadOnlyDictionary<uint, ValueChoice>(values);
    });
    public static bool Enabled(int index, string field)
    {
        if (!Fields.Contains(field)) throw new ArgumentException("Unknown terrain control.");
        var terrain = Terrain(index);
        return field switch { "MaxHealthType" or "DestroyedType" or "MaxHealthEffect" or "DestroyedEffect" => terrain.MinWidth != 0, "Visible" => terrain.MaxWidth == 0, _ => true };
    }
    public static IReadOnlyList<ValueChoice> Choices(int index, string field)
    {
        if (field is "MaxHealthEffect" or "DestroyedEffect") return EffectCatalogue.Choices;
        if (field is not ("MaxHealthType" or "DestroyedType")) throw new ArgumentException("This terrain field has no source catalogue.");
        var rows = terrains.Value;
        return Array.AsReadOnly((Terrain(index).MinWidth != 0 ? rows.TakeWhile(terrain => terrain.MinHeight == 0) : rows)
            .Select(terrain => names.Value[terrain.ID]).ToArray());
    }
    internal static string Encode(string field, string input, string current, NativeField storage)
    {
        // Exact re-entry retains wider imported/default values until changed.
        if (input.Trim().Replace(',', '.') == GlobalDefinitionNumbers.Display("Terrains", field, current)) return current;
        if (field == "TerrainLightH") return GlobalDefinitionNumbers.Encode("Terrains", field, input, current, storage);
        var value = long.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var (minimum, maximum) = field switch {
            "Damage" => (-32768L, 32767L),
            "GoldValue" or "StartingHealth" or "MaxHealth" or "ManaGain" or "MaxManaGain" => (0L, 65535L),
            "WibbleH" or "WibbleV" or "TerrainLightR" or "TerrainLightG" or "TerrainLightB" or "AmbientLightR" or "AmbientLightG" or "AmbientLightB" => (0L, 255L),
            "Visible" or "TerrainLight" or "AmbientLight" => (0L, 1L), _ => (0L, uint.MaxValue) };
        if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(nameof(input), "Value is outside the original terrain control range.");
        var encoded = value.ToString(CultureInfo.InvariantCulture); storage.Encode(encoded); return encoded;
    }
    internal static GlobalTerrainDraft ExplicitSave(GlobalTerrainDraft draft)
    {
        var values = Fields.ToDictionary(field => field, draft.Raw);
        foreach (var light in new[] { "TerrainLight", "AmbientLight" })
            if (values[light] == "0") foreach (var colour in new[] { "R", "G", "B" }) values[light + colour] = "0";
        return draft.With(values);
    }
}

public sealed partial class NativeMapDocument
{
    public GlobalTerrainDraft CreateGlobalTerrainDraft(GlobalDefinitionRow row)
    {
        if (row.Family != "Terrains") throw new ArgumentException("Choose a terrain definition.");
        var values = GlobalDefinitionValues(row).ToDictionary(value => value.Name);
        if (!GlobalTerrainControls.Fields.All(values.ContainsKey)) throw new InvalidDataException("Unverified terrain definition fields.");
        return new(this, row.Index, values.Values.All(value => value.Editable),
            Array.AsReadOnly(GlobalTerrainControls.Fields.Select(field => (field, values[field].Value)).ToArray()),
            new ReadOnlyDictionary<string, string>(GlobalTerrainControls.Fields.ToDictionary(field => field, field => values[field].Value)));
    }
    private GlobalDefinitionRow VerifyGlobalTerrainDraft(GlobalTerrainDraft draft)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == "Terrains" && row.Index == draft.Index) ?? throw new ArgumentException("Unknown terrain definition.");
        var current = CreateGlobalTerrainDraft(row);
        if (!draft.Editable || !current.Editable || !draft.SameBaseline(current)) throw new InvalidOperationException("A verified unchanged terrain definition is required.");
        return row;
    }
    public GlobalTerrainDraft UpdateGlobalTerrainDraft(GlobalTerrainDraft draft, IEnumerable<GlobalTerrainInput> inputs)
    {
        var row = VerifyGlobalTerrainDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(GlobalTerrainControls.Fields.Order())) throw new ArgumentException("Provide each terrain control exactly once.");
        var values = new Dictionary<string, string>();
        foreach (var input in requested)
        {
            var storage = IsLegacyProject ? LegacyProjectSchemas.Fields("GLOBALTERRAIN").Single(field => field.Name == input.Field) : row.Record!.Fields.Single(field => field.Name == input.Field);
            var encoded = GlobalTerrainControls.Encode(input.Field, input.Value, draft.Raw(input.Field), storage);
            if (encoded != draft.Raw(input.Field))
            {
                if (!GlobalTerrainControls.Enabled(draft.Index, input.Field)) throw new InvalidOperationException("This original terrain control is inactive.");
                if (input.Field is "MaxHealthType" or "DestroyedType" or "MaxHealthEffect" or "DestroyedEffect")
                    if (!GlobalTerrainControls.Choices(draft.Index, input.Field).Any(choice => choice.Value == encoded)) throw new ArgumentException("Choose an original terrain/effect; unknown imports may only be retained.");
            }
            values.Add(input.Field, encoded);
        }
        return GlobalTerrainControls.ExplicitSave(draft.With(values));
    }
    public void ApplyGlobalTerrainDraft(GlobalTerrainDraft draft)
    {
        VerifyGlobalTerrainDraft(draft); var saved = GlobalTerrainControls.ExplicitSave(draft);
        if (!saved.Changed) return;
        SetGlobalDefinitionValues(saved.Baseline.Where(item => saved.Raw(item.Field) != item.Raw).Select(item => new GlobalDefinitionChange("Terrains", saved.Index, item.Field, saved.Raw(item.Field))));
    }
}
