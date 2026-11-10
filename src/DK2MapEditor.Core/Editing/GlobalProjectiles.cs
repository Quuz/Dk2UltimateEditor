using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record GlobalProjectileOption(uint ID, int Form, int Label1, int Label2, bool Signed1, bool Signed2, ValueChoice Name);
public sealed record GlobalProjectileState(string Family, int Index, string Slot, string Type, string Data1, string Data2, bool Editable);
public readonly record struct GlobalProjectileEditorInputs(string Data1, string Data2);

public static class GlobalProjectiles
{
    private static readonly Lazy<IReadOnlyList<GlobalProjectileOption>> options = new(ReadOptions);
    public static IReadOnlyList<GlobalProjectileOption> Options => options.Value;
    public static IReadOnlyList<string> Slots(string family) => family switch { "Traps" or "CrSpells" => ["Shot"], "Spells" => ["Shot", "Upgrade"], _ => [] };
    internal static (string Type, string Data1, string Data2) Fields(string family, string slot)
    {
        if (!Slots(family).Contains(slot)) throw new ArgumentException("Unknown projectile slot.");
        return slot == "Upgrade" ? ("UpgShotType", "UpgShotData1", "UpgShotData2") : (family == "CrSpells" ? "Shot" : "ShotType", "ShotData1", "ShotData2");
    }
    public static GlobalProjectileOption? Find(string type) => uint.TryParse(type, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? Options.SingleOrDefault(option => option.ID == id) : null;
    public static string DisplayParameter(GlobalProjectileOption option, string raw, bool second)
    {
        if ((second ? option.Signed2 : option.Signed1) && int.TryParse(raw, CultureInfo.InvariantCulture, out var value) && value is >= 32768 and <= 65535) return (value - 65536).ToString(CultureInfo.InvariantCulture);
        return raw;
    }
    public static IReadOnlyList<ValueChoice> Targets(GlobalProjectileOption option) => option.Form switch
    {
        2 or 3 => GlobalDefinitionDefaults.Catalogue("Creatures").Select(item => ValueCatalogues.Named(item, item.ID.ToString(CultureInfo.InvariantCulture))).ToArray(),
        4 => ValueCatalogues.Elements("Objects"), _ => []
    };
    /// <summary>Properties_GlobalShotInit selects the first source target when
    /// the imported ID is absent, and level 1 when the stored level is invalid.
    /// This initializes a private UI draft; it does not write the document.</summary>
    public static GlobalProjectileEditorInputs EditorInputs(GlobalProjectileOption option, string first, string second)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (option.Form is 2 or 3 or 4)
        {
            var targets = Targets(option);
            if (!targets.Any(choice => choice.Value == first)) first = targets[0].Value;
            if (option.Form == 3 && (!int.TryParse(second, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) || level is < 1 or > 10)) second = "1";
        }
        return new(first, second);
    }
    private static IReadOnlyList<GlobalProjectileOption> ReadOptions()
    {
        using var stream = typeof(GlobalProjectiles).Assembly.GetManifestResourceStream("Legacy.Datas_GlobalVariables.c")!;
        using var reader = new StreamReader(stream); var text = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        var body = Regex.Match(text, @"ELEMENT\s+GShots\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value;
        var result = Regex.Matches(body, @"\{\s*0x([0-9A-Fa-f]+),\s*0x([0-9A-Fa-f]+),\s*\d+,\s*(\d+),\s*(\d+),\s*([01]),\s*([01]),\s*(sz\w+),\s*NULL\}")
            .Select(match =>
            {
                var id = uint.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return new GlobalProjectileOption(id, int.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture), int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture), int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture), match.Groups[5].Value == "1", match.Groups[6].Value == "1", ValueCatalogues.Named(new(id, 0, 0, match.Groups[7].Value), id.ToString(CultureInfo.InvariantCulture)));
            }).ToArray();
        if (result.Length != 66 || !result.Select(option => option.ID).SequenceEqual(Enumerable.Range(0, 66).Select(id => (uint)id)) || result.Any(option => option.Form is not (0 or 1 or 2 or 3 or 4 or 255) || option.Label1 > 6 || option.Label2 > 2)) throw new InvalidDataException("Unverified original projectile catalogue.");
        return Array.AsReadOnly(result);
    }
}

public sealed partial class NativeMapDocument
{
    public GlobalProjectileState GlobalProjectile(GlobalDefinitionRow row, string slot)
    {
        var fields = GlobalProjectiles.Fields(row.Family, slot); var values = GlobalDefinitionValues(row);
        string Read(string name) => values.Single(value => value.Name == name).Value;
        return new(row.Family, row.Index, slot, Read(fields.Type), Read(fields.Data1), Read(fields.Data2), values.All(value => value.Editable));
    }
    public void SetGlobalProjectile(string family, int index, string slot, uint type, string data1, string data2)
        => SetGlobalProjectileCore(family, index, slot, type, data1, data2, editorAcceptance: false);

    /// <summary>Explicit source options acceptance. Unknown target/level UI
    /// fallbacks become stored values only here; non-gold object options clear
    /// their second argument even when the type and target are unchanged.</summary>
    public void SetEditorGlobalProjectile(string family, int index, string slot, uint type, string data1, string data2)
        => SetGlobalProjectileCore(family, index, slot, type, data1, data2, editorAcceptance: true);

    private void SetGlobalProjectileCore(string family, int index, string slot, uint type, string data1, string data2, bool editorAcceptance)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == family && row.Index == index) ?? throw new ArgumentException("Unknown definition target.");
        if (!row.Editable) throw new InvalidOperationException("A verified definition layout is required.");
        var state = GlobalProjectile(row, slot); var fields = GlobalProjectiles.Fields(family, slot);
        var option = GlobalProjectiles.Options.SingleOrDefault(option => option.ID == type) ?? throw new ArgumentException("Unknown original projectile type.");
        if (editorAcceptance)
        {
            var inputs = GlobalProjectiles.EditorInputs(option, data1, data2);
            data1 = inputs.Data1; data2 = inputs.Data2;
        }
        var sameType = state.Type == type.ToString(CultureInfo.InvariantCulture);
        NativeField Storage(string name) => IsLegacyProject ? LegacyProjectSchemas.Fields(GlobalDefinitionDefaults.Structure(family)).Single(field => field.Name == name) : row.Record!.Fields.Single(field => field.Name == name);
        string Number(string text, string current, string name, int minimum, int maximum, bool signed)
        {
            var value = int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var display = signed ? GlobalProjectiles.DisplayParameter(option, current, name == fields.Data2) : current;
            if (sameType && value.ToString(CultureInfo.InvariantCulture) == display) return current;
            if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(nameof(text), "Value is outside the original projectile control range.");
            var storage = Storage(name); var raw = signed && storage.Kind == NativeValueKind.UInt16 ? unchecked((ushort)(short)value).ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
            storage.Encode(raw); return raw;
        }
        var first = state.Data1; var second = state.Data2;
        if (option.Form is 0 or 1) first = Number(data1, first, fields.Data1, option.Signed1 ? -32768 : 0, 32767, option.Signed1);
        if (option.Form == 0) second = Number(data2, second, fields.Data2, option.Signed2 ? -32768 : 0, 32767, option.Signed2);
        if (option.Form is 2 or 3 or 4)
        {
            // Imported project LONG target values can be negative/unknown.
            // Permit the unchanged target without inventing a replacement;
            // newly selected targets must be actual source catalogue IDs.
            var target = int.Parse(data1, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (!(sameType && target == first) && !GlobalProjectiles.Targets(option).Any(choice => choice.Value == target)) throw new ArgumentException("Choose a source creature/object target.");
            Storage(fields.Data1).Encode(target); first = target;
            if (option.Form == 2) second = Number(data2, second, fields.Data2, 0, 100, false);
            if (option.Form == 3) second = Number(data2, second, fields.Data2, 1, 10, false);
            if (option.Form == 4) second = first == "1" ? Number(data2, second, fields.Data2, 0, 80000, false) : !editorAcceptance && sameType && first == state.Data1 ? state.Data2 : "0";
        }
        SetGlobalDefinitionValues([new(family, index, fields.Type, type.ToString(CultureInfo.InvariantCulture)), new(family, index, fields.Data1, first), new(family, index, fields.Data2, second)]);
    }
}
