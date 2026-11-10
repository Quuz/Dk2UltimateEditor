using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

/// <summary>Original Effects list, including real zero but excluding its end marker.</summary>
public static class EffectCatalogue
{
    private static readonly Lazy<IReadOnlyList<ValueChoice>> choices = new(Read);
    public static IReadOnlyList<ValueChoice> Choices => choices.Value;
    private static IReadOnlyList<ValueChoice> Read()
    {
        var entries = LegacyCompilerRules.Elements("Effects").Where(entry => entry.ID != uint.MaxValue).ToArray();
        if (!entries.Select(entry => entry.ID).SequenceEqual(Enumerable.Range(1, 370).Select(id => (uint)id))) throw new InvalidDataException("Unverified original effect catalogue.");
        entries = [new(0, 0, 0, "szEffect000"), .. entries];
        using var stream = typeof(EffectCatalogue).Assembly.GetManifestResourceStream("Editor.EffectNames")!;
        using var reader = new StreamReader(stream);
        var names = new Dictionary<uint, (string English, string Russian)>();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var parts = line.Split('|');
            if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace) || !names.TryAdd(uint.Parse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture), (parts[1], parts[2]))) throw new InvalidDataException("Invalid bilingual effect name.");
        }
        if (names.Count != entries.Length) throw new InvalidDataException("Incomplete bilingual effects.");
        return Array.AsReadOnly(entries.Select(entry =>
        {
            if (entry.Name != $"szEffect{entry.ID:X3}" || !names.TryGetValue(entry.ID, out var name)) throw new InvalidDataException("Effect symbol/name mismatch.");
            return new ValueChoice(entry.ID.ToString(CultureInfo.InvariantCulture), name.English, name.Russian);
        }).ToArray());
    }
}

public sealed record GlobalDecorationInput(string Field, string Value);
public sealed class GlobalDecorationsDraft
{
    internal NativeMapDocument Owner { get; }
    public string Family { get; }
    public int Index { get; }
    public bool Editable { get; }
    internal IReadOnlyList<(string Field, string Raw)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal GlobalDecorationsDraft(NativeMapDocument owner, string family, int index, bool editable, IReadOnlyList<(string Field, string Raw)> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Family = family; Index = index; Editable = editable; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public bool Changed => Baseline.Any(item => Raw(item.Field) != item.Raw);
    public bool SameBaseline(GlobalDecorationsDraft other) => ReferenceEquals(Owner, other.Owner) && Family == other.Family && Index == other.Index && Editable == other.Editable && Baseline.SequenceEqual(other.Baseline);
    internal GlobalDecorationsDraft With(Dictionary<string, string> updated) => new(Owner, Family, Index, Editable, Baseline, new ReadOnlyDictionary<string, string>(updated));
}

/// <summary>Named effects, six room objects and creature armour material.</summary>
public static class GlobalDefinitionDecorations
{
    public static IReadOnlyList<string> Families { get; } = Array.AsReadOnly(new[] { "Terrains", "Rooms", "Doors", "Traps", "Creatures" });
    public static IReadOnlyList<string> Fields(string family) => family switch
    {
        "Terrains" => ["MaxHealthEffect", "DestroyedEffect"],
        "Rooms" => ["BattleEffect", .. Enumerable.Range(0, 6).Select(index => $"Effects[{index}]"), .. Enumerable.Range(0, 6).Select(index => $"Objects[{index}]")],
        "Doors" => ["DeathEffect"], "Traps" => ["DeathEffect", "PowerlessEffect", "IdleEffect"],
        "Creatures" => ["ArmourType", "SlapEffect", "HealthEffect", "EntranceEffect", "DeathEffect"], _ => throw new ArgumentException("Unknown decoration family.")
    };
    private static readonly Lazy<IReadOnlyList<ValueChoice>> objects = new(() => Array.AsReadOnly(new[] { new ValueChoice("0", "None", "Нет") }.Concat(LegacyCompilerRules.Elements("Objects").Select(entry => ValueCatalogues.Named(entry, entry.ID.ToString(CultureInfo.InvariantCulture)))).ToArray()));
    public static IReadOnlyList<ValueChoice> Choices(string family, string field)
    {
        if (!Fields(family).Contains(field)) throw new ArgumentException("Unknown decoration field.");
        return field == "ArmourType" ? GlobalDefinitionRules.Choices("Doors", "Material") : field.StartsWith("Objects[", StringComparison.Ordinal) ? objects.Value : EffectCatalogue.Choices;
    }
    private static readonly Lazy<IReadOnlyDictionary<uint, uint>> roomMasks = new(ReadRoomMasks);
    private static IReadOnlyDictionary<uint, uint> ReadRoomMasks()
    {
        using var stream = typeof(GlobalDefinitionDecorations).Assembly.GetManifestResourceStream("Legacy.Datas_GlobalVariables.c")!;
        using var reader = new StreamReader(stream); var text = reader.ReadToEnd();
        var body = Regex.Match(text, @"ELEMENT\s+GRooms\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value;
        var result = new Dictionary<uint, uint>();
        foreach (Match row in Regex.Matches(body, @"\{\s*0x([0-9A-Fa-f]+)[^{}]*?\(char\s*\*\)\(([^)]*)\)\s*\}"))
        {
            uint mask = 0;
            foreach (Match flag in Regex.Matches(row.Groups[2].Value, @"MGEF_(EFFECT|OBJECT)([0-5])")) mask |= 1u << (int.Parse(flag.Groups[2].Value, CultureInfo.InvariantCulture) + (flag.Groups[1].Value == "OBJECT" ? 8 : 0));
            result.Add(uint.Parse(row.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture), mask);
        }
        if (result.Count != 24 || !GlobalDefinitionDefaults.Catalogue("Rooms").All(row => result.ContainsKey(row.ID))) throw new InvalidDataException("Unverified original room slot masks.");
        return new ReadOnlyDictionary<uint, uint>(result);
    }
    public static bool Enabled(string family, int index, string field)
    {
        if (!Fields(family).Contains(field)) throw new ArgumentException("Unknown decoration field.");
        if (family == "Terrains") return GlobalTerrainControls.Enabled(index, field);
        if (family == "Traps") return GlobalTrapControls.Enabled(index, field);
        if (family != "Rooms") return true;
        if (field == "BattleEffect") return false;
        var match = Regex.Match(field, @"^(Effects|Objects)\[([0-5])\]$");
        var bit = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) + (match.Groups[1].Value == "Objects" ? 8 : 0);
        var room = GlobalDefinitionDefaults.Catalogue("Rooms")[index];
        return (roomMasks.Value[room.ID] & (1u << bit)) != 0;
    }
}

public sealed partial class NativeMapDocument
{
    public GlobalDecorationsDraft CreateGlobalDecorationsDraft(GlobalDefinitionRow row)
    {
        var names = GlobalDefinitionDecorations.Fields(row.Family); var values = GlobalDefinitionValues(row).Where(value => names.Contains(value.Name)).ToDictionary(value => value.Name);
        if (values.Count != names.Count) throw new InvalidDataException("Unverified decoration fields.");
        return new(this, row.Family, row.Index, values.Values.All(value => value.Editable), Array.AsReadOnly(names.Select(field => (field, values[field].Value)).ToArray()), new ReadOnlyDictionary<string, string>(values.ToDictionary(item => item.Key, item => item.Value.Value)));
    }
    private GlobalDefinitionRow VerifyGlobalDecorationsDraft(GlobalDecorationsDraft draft)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == draft.Family && row.Index == draft.Index) ?? throw new ArgumentException("Unknown decoration definition.");
        var current = CreateGlobalDecorationsDraft(row);
        if (!draft.Editable || !current.Editable) throw new InvalidOperationException("A verified current decoration layout is required.");
        if (!draft.SameBaseline(current)) throw new InvalidOperationException("Decorations changed after the draft was opened.");
        return row;
    }
    public GlobalDecorationsDraft UpdateGlobalDecorations(GlobalDecorationsDraft draft, IEnumerable<GlobalDecorationInput> inputs)
    {
        var row = VerifyGlobalDecorationsDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(GlobalDefinitionDecorations.Fields(draft.Family).Order())) throw new ArgumentException("Provide each decoration field exactly once.");
        var values = new Dictionary<string, string>();
        foreach (var input in requested)
        {
            var encoded = uint.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (encoded != draft.Raw(input.Field))
            {
                if (!GlobalDefinitionDecorations.Enabled(draft.Family, draft.Index, input.Field)) throw new InvalidOperationException("This slot is inactive for the original room type.");
                if (!GlobalDefinitionDecorations.Choices(draft.Family, input.Field).Any(choice => choice.Value == encoded)) throw new ArgumentException("Choose a source decoration; unknown imported IDs may only be retained.");
                var storage = IsLegacyProject ? LegacyProjectSchemas.Fields(GlobalDefinitionDefaults.Structure(draft.Family)).Single(field => field.Name == input.Field) : row.Record!.Fields.Single(field => field.Name == input.Field);
                storage.Encode(encoded);
            }
            values.Add(input.Field, encoded);
        }
        return draft.With(values);
    }
    public void ApplyGlobalDecorations(GlobalDecorationsDraft draft)
    {
        VerifyGlobalDecorationsDraft(draft); if (!draft.Changed) return;
        SetGlobalDefinitionValues(draft.Baseline.Where(item => draft.Raw(item.Field) != item.Raw).Select(item => new GlobalDefinitionChange(draft.Family, draft.Index, item.Field, draft.Raw(item.Field))));
    }
}
