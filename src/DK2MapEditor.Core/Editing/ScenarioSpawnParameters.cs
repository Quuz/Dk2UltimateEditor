using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record ScenarioSpawnFlag(uint Bit, string Key, bool Editable);

public static class ScenarioSpawnOptions
{
    public static IReadOnlyList<ScenarioSpawnFlag> Flags { get; } = Array.AsReadOnly(new[] {
        new ScenarioSpawnFlag(1, "Attack", true), new(8, "Attacked", true), new(16, "HeroLair", true), new(32, "FreePrisoners", true),
        new(64, "Placed", false), new(128, "Stunned", true), new(4, "InstantDeath", true) });
    public const uint EditableMask = 0xBD;
}

public sealed partial class NativeMapDocument
{
    private void VerifyScenarioSpawn(ScenarioParameterDraft draft)
    {
        VerifyScenarioParameterDraft(draft);
        if (draft.Type != "ACTION_ADDCREATURE") throw new ArgumentException("Select a creature-spawn parent draft.");
    }
    private static string SpawnField(ScenarioParameterDraft draft, int parameter)
        => draft.Fields.Single(item => item.Name == "param" + parameter || item.Name == "Param" + parameter).Name;
    public ScenarioParameterDraft UpdateScenarioSpawnFlags(ScenarioParameterDraft draft, IEnumerable<KeyValuePair<uint, bool>> inputs)
    {
        VerifyScenarioSpawn(draft); ArgumentNullException.ThrowIfNull(inputs);
        var values = inputs.ToArray();
        if (values.Any(item => !ScenarioSpawnOptions.Flags.Any(flag => flag.Bit == item.Key && flag.Editable)) || values.Select(item => item.Key).Distinct().Count() != values.Length)
            throw new ArgumentException("Choose each editable original flag at most once.");
        var field = SpawnField(draft, 4); var bits = unchecked((uint)int.Parse(draft.Values[field], CultureInfo.InvariantCulture));
        foreach (var item in values) bits = item.Value ? bits | item.Key : bits & ~item.Key;
        return UpdateScenarioControlPage(draft, [new(field, bits.ToString(CultureInfo.InvariantCulture))]);
    }
    public ScenarioParameterDraft UpdateScenarioSpawnPoint(ScenarioParameterDraft draft, TilePoint point)
    {
        VerifyScenarioSpawn(draft);
        if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height || point.X > 999 || point.Y > 999)
            throw new ArgumentOutOfRangeException(nameof(point), "Choose an original-range point on this map.");
        return UpdateScenarioControlPage(draft, [new(SpawnField(draft, 5), point.X.ToString(CultureInfo.InvariantCulture)),
            new(SpawnField(draft, 7), point.Y.ToString(CultureInfo.InvariantCulture))]);
    }
    public ScenarioParameterDraft UpdateScenarioSpawnArea(ScenarioParameterDraft draft, uint areaID)
    {
        VerifyScenarioSpawn(draft);
        var matches = MapRegions().Where(region => region.Kind == MapRegionKind.Area && EntityReferences.Identity(region.Record) is { } identity
            && uint.TryParse(identity.Record.Read(identity.Field), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id == areaID).ToArray();
        if (matches.Length != 1) throw new ArgumentException("Choose one unambiguous existing area.");
        return UpdateScenarioSpawnPoint(draft, new(matches[0].Bounds.X, matches[0].Bounds.Y));
    }
}
