using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record GlobalRoomInput(string Field, string Value);
public sealed class GlobalRoomDraft
{
    internal NativeMapDocument Owner { get; }
    public int Index { get; }
    public bool Editable { get; }
    internal IReadOnlyList<(string Field, string Raw)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal GlobalRoomDraft(NativeMapDocument owner, int index, bool editable, IReadOnlyList<(string Field, string Raw)> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Index = index; Editable = editable; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public string Display(string field) => Raw(field).Length == 0 ? "" : GlobalDefinitionNumbers.Display("Rooms", field, Raw(field));
    public bool Changed => Baseline.Any(item => Raw(item.Field) != item.Raw);
    public bool SameBaseline(GlobalRoomDraft other) => ReferenceEquals(Owner, other.Owner) && Index == other.Index && Editable == other.Editable && Baseline.SequenceEqual(other.Baseline);
    internal GlobalRoomDraft With(Dictionary<string, string> updated) => new(Owner, Index, Editable, Baseline, new ReadOnlyDictionary<string, string>(updated));
}

/// <summary>Complete original room page6821. Inactive slots and BattleEffect
/// retain their selected/imported values; the source introduces no Save zeroing.</summary>
public static class GlobalRoomControls
{
    public static IReadOnlyList<string> Fields { get; } = Array.AsReadOnly(new[] {
        "Cost", "BattleEffect", "Effects[0]", "Effects[1]", "Effects[2]", "Effects[3]", "Effects[4]", "Effects[5]",
        "Objects[0]", "Objects[1]", "Objects[2]", "Objects[3]", "Objects[4]", "Objects[5]",
        "TorchIntensity", "TorchLightR", "TorchLightG", "TorchLightB", "TorchLightH", "TorchLightRad" });
    private static readonly Lazy<IReadOnlyDictionary<uint, uint>> masks = new(ReadMasks);
    private static IReadOnlyDictionary<uint, uint> ReadMasks()
    {
        using var stream = typeof(GlobalRoomControls).Assembly.GetManifestResourceStream("Legacy.Datas_GlobalVariables.c")!;
        using var reader = new StreamReader(stream);
        var text = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        var body = Regex.Match(text, @"ELEMENT\s+GRooms\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value;
        var result = new Dictionary<uint, uint>();
        foreach (Match row in Regex.Matches(body, @"\{\s*0x([0-9A-Fa-f]+)[^{}]*?\(char\s*\*\)\(([^)]*)\)\s*\}"))
        {
            uint mask = row.Groups[2].Value.Contains("MGEF_CANTBUILD", StringComparison.Ordinal) ? 0x80000000u : 0;
            foreach (Match flag in Regex.Matches(row.Groups[2].Value, @"MGEF_(EFFECT|OBJECT)([0-5])"))
                mask |= 1u << (int.Parse(flag.Groups[2].Value, CultureInfo.InvariantCulture) + (flag.Groups[1].Value == "OBJECT" ? 8 : 0));
            result.Add(uint.Parse(row.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture), mask);
        }
        var rooms = GlobalDefinitionDefaults.Catalogue("Rooms");
        if (rooms.Count != 24 || result.Count != 24 || !rooms.All(room => result.ContainsKey(room.ID)))
            throw new InvalidDataException("Unverified original room masks.");
        return new ReadOnlyDictionary<uint, uint>(result);
    }
    public static bool Enabled(int index, string field)
    {
        if (!Fields.Contains(field)) throw new ArgumentException("Unknown room control.");
        var mask = masks.Value[GlobalDefinitionDefaults.Catalogue("Rooms")[index].ID];
        if (field == "BattleEffect") return false;
        if (field == "Cost") return (mask & 0x80000000u) == 0;
        var slot = Regex.Match(field, @"^(Effects|Objects)\[([0-5])\]$");
        return !slot.Success || (mask & (1u << (int.Parse(slot.Groups[2].Value, CultureInfo.InvariantCulture) + (slot.Groups[1].Value == "Objects" ? 8 : 0)))) != 0;
    }
    public static bool Named(string field) => field == "BattleEffect" || field.StartsWith("Effects[", StringComparison.Ordinal) || field.StartsWith("Objects[", StringComparison.Ordinal);
    public static IReadOnlyList<ValueChoice> Choices(string field)
    {
        if (!Fields.Contains(field) || !Named(field)) throw new ArgumentException("This room field has no original catalogue.");
        return GlobalDefinitionDecorations.Choices("Rooms", field);
    }
    internal static string Encode(string field, string input, string current, NativeField storage)
    {
        if (field is "TorchLightH" or "TorchLightRad") return GlobalDefinitionNumbers.Encode("Rooms", field, input, current, storage);
        var value = long.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
        if (value == long.Parse(current, NumberStyles.Integer, CultureInfo.InvariantCulture)) return current;
        var maximum = field switch { "Cost" => 65535L, "TorchIntensity" or "TorchLightR" or "TorchLightG" or "TorchLightB" => 255L, _ => uint.MaxValue };
        if (value < 0 || value > maximum) throw new ArgumentOutOfRangeException(nameof(input), "Value is outside the original room control range.");
        var encoded = value.ToString(CultureInfo.InvariantCulture); storage.Encode(encoded); return encoded;
    }
}

public sealed partial class NativeMapDocument
{
    public GlobalRoomDraft CreateGlobalRoomDraft(GlobalDefinitionRow row)
    {
        if (row.Family != "Rooms") throw new ArgumentException("Choose a room definition.");
        var values = GlobalDefinitionValues(row).ToDictionary(value => value.Name);
        if (!GlobalRoomControls.Fields.All(values.ContainsKey)) throw new InvalidDataException("Unverified room definition fields.");
        return new(this, row.Index, values.Values.All(value => value.Editable),
            Array.AsReadOnly(GlobalRoomControls.Fields.Select(field => (field, values[field].Value)).ToArray()),
            new ReadOnlyDictionary<string, string>(GlobalRoomControls.Fields.ToDictionary(field => field, field => values[field].Value)));
    }
    private GlobalDefinitionRow VerifyGlobalRoomDraft(GlobalRoomDraft draft)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == "Rooms" && row.Index == draft.Index) ?? throw new ArgumentException("Unknown room definition.");
        var current = CreateGlobalRoomDraft(row);
        if (!draft.Editable || !current.Editable || !draft.SameBaseline(current)) throw new InvalidOperationException("A verified unchanged room definition is required.");
        return row;
    }
    public GlobalRoomDraft UpdateGlobalRoomDraft(GlobalRoomDraft draft, IEnumerable<GlobalRoomInput> inputs)
    {
        var row = VerifyGlobalRoomDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(GlobalRoomControls.Fields.Order())) throw new ArgumentException("Provide each room control exactly once.");
        var values = new Dictionary<string, string>();
        foreach (var input in requested)
        {
            var storage = IsLegacyProject ? LegacyProjectSchemas.Fields("GLOBALROOM").Single(field => field.Name == input.Field) : row.Record!.Fields.Single(field => field.Name == input.Field);
            var encoded = GlobalRoomControls.Encode(input.Field, input.Value, draft.Raw(input.Field), storage);
            if (encoded != draft.Raw(input.Field))
            {
                if (!GlobalRoomControls.Enabled(draft.Index, input.Field)) throw new InvalidOperationException("This original room control is inactive.");
                if (GlobalRoomControls.Named(input.Field) && !GlobalRoomControls.Choices(input.Field).Any(choice => choice.Value == encoded))
                    throw new ArgumentException("Choose an original effect/object; unknown imports may only be retained.");
            }
            values.Add(input.Field, encoded);
        }
        return draft.With(values);
    }
    public void ApplyGlobalRoomDraft(GlobalRoomDraft draft)
    {
        VerifyGlobalRoomDraft(draft);
        if (!draft.Changed) return;
        SetGlobalDefinitionValues(draft.Baseline.Where(item => draft.Raw(item.Field) != item.Raw).Select(item => new GlobalDefinitionChange("Rooms", draft.Index, item.Field, draft.Raw(item.Field))));
    }
}
