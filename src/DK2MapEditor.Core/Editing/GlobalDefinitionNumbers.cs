using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record GlobalDefinitionNumberFormat(bool Fractional, bool SignedAttribute, bool Percent, decimal? Minimum, decimal? Maximum);

/// <summary>Source control units, distinct from the lossless raw integer API.
/// Fixed-point is an integer / 4096, not an IEEE float. Exact decimal output
/// avoids the original eight-decimal display losing low bits on re-entry.</summary>
public static class GlobalDefinitionNumbers
{
    private static readonly Lazy<IReadOnlyDictionary<(string, string), GlobalDefinitionNumberFormat>> formats = new(ReadFormats);
    public static GlobalDefinitionNumberFormat Format(string family, string field) => formats.Value.TryGetValue((family, field), out var result) ? result : new(false, false, false, null, null);
    public static GlobalDefinitionNumberFormat Format(string family, string field, bool native)
    {
        var format = Format(family, field);
        if (!native) return format;
        var storage = LegacyGlobalSchemas.All.Single(item => item.Group == family).Fields.Single(item => item.Name == field);
        if (format.SignedAttribute) return format with { Minimum = short.MinValue, Maximum = short.MaxValue };
        if (format.Fractional && storage.Kind == NativeValueKind.UInt16)
            return format with { Maximum = Math.Min(format.Maximum ?? decimal.MaxValue, ushort.MaxValue / 4096m) };
        return format;
    }
    public static string Display(string family, string field, string raw)
    {
        if (raw.Length == 0) return "";
        var format = Format(family, field);
        var value = decimal.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture);
        if (format.SignedAttribute && value is >= 32768 and <= 65535) value -= 65536;
        if (format.Fractional)
        {
            if (format.Minimum >= 0 && value < 0) value += 4294967296m;
            return (value / 4096m).ToString("0.0###########", CultureInfo.InvariantCulture);
        }
        return value.ToString(CultureInfo.InvariantCulture);
    }
    internal static string Encode(string family, string name, string input, string current, NativeField storage)
    {
        var format = Format(family, name);
        var text = input.Trim().Replace(',', '.');
        if (!Regex.IsMatch(text, @"^[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)$", RegexOptions.CultureInvariant)) throw new FormatException("Enter a decimal number without grouping or an exponent.");
        var number = decimal.Parse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        if (!format.Fractional && decimal.Truncate(number) != number) throw new ArgumentException("This field requires an integer.");
        // Preserve signed WORD aliases in project defaults and imported values
        // outside manual control limits. Merely re-applying is never a repair.
        if (number == decimal.Parse(Display(family, name, current), CultureInfo.InvariantCulture)) return current;
        var encoded = format.Fractional ? decimal.Truncate(number * 4096m) : number;
        var limited = format.Fractional ? encoded / 4096m : encoded;
        if (format.Minimum is { } minimum && limited < minimum || format.Maximum is { } maximum && limited > maximum || format.Fractional && format.Minimum >= 0 && number < 0)
            throw new ArgumentOutOfRangeException(nameof(input), "Value is outside the original control's range.");
        if (format.SignedAttribute && storage.Kind == NativeValueKind.UInt16)
        {
            if (encoded < short.MinValue || encoded > short.MaxValue) throw new OverflowException("The native signed attribute is 16-bit; use a project to retain wider source values.");
            encoded = unchecked((ushort)(short)encoded);
        }
        var result = encoded.ToString(CultureInfo.InvariantCulture);
        storage.Encode(result); // Preflight native width, never silently truncate.
        return result;
    }
    private static string Source(string name)
    {
        using var stream = typeof(GlobalDefinitionNumbers).Assembly.GetManifestResourceStream("Legacy." + name)!;
        using var reader = new StreamReader(stream);
        return Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
    }
    private static IReadOnlyDictionary<(string, string), GlobalDefinitionNumberFormat> ReadFormats()
    {
        var result = new Dictionary<(string, string), GlobalDefinitionNumberFormat>();
        var attributes = Regex.Match(Source("Datas_GlobalVariables.c"), @"GAttribs\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value;
        var matches = Regex.Matches(attributes, @"Creatures\[0\]\.(\w+)-[^,]+,\s*0x([0-9A-Fa-f]+),\s*(TRUE|FALSE),\s*(TRUE|FALSE),");
        if (matches.Count != 38) throw new InvalidDataException("Incomplete original attribute units.");
        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value; var fractional = match.Groups[3].Value == "TRUE"; var signed = match.Groups[4].Value == "TRUE";
            var native = LegacyGlobalSchemas.All.Single(family => family.Group == "Creatures").Fields.Single(field => field.Name == name);
            if (native.Offset != int.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture) || native.Size != (name == "HungerRate" ? 4 : 2)) throw new InvalidDataException("Attribute/compiler units disagree: " + name);
            // The original signed integer input has no explicit lower clamp.
            result.Add(("Creatures", name), fractional ? Fixed(false, 32767) : new(false, signed, false, signed ? int.MinValue : 0, 32767));
        }
        foreach (var source in new[] { "Properties_Global_Elements.c", "Properties_Global_Creatures.c" })
            foreach (Match match in Regex.Matches(Source(source), @"Context->Variables\.(\w+)\[[^\]]+\]\.(\w+)\s*=\s*Misc_GetDlgItemFloat(Signed)?\(hDlg,\d+\)"))
            {
                var family = match.Groups[1].Value; var name = match.Groups[2].Value; var signed = match.Groups[3].Success;
                var native = LegacyGlobalSchemas.All.Single(item => item.Group == family).Fields.Single(field => field.Name == name);
                if (native.Size is not (2 or 4) || native.BitMask != 0) throw new InvalidDataException("Unverified fixed-point storage: " + family + "." + name);
                var maximum = family switch
                {
                    "Terrains" or "Traps" => 16000,
                    "Rooms" => name == "TorchLightH" ? 8 : 3,
                    "Spells" => 32767,
                    "CrSpells" => 1024,
                    "Creatures" => name switch { "WaddleScale" or "OscillateScale" => 1, "LightRadius" => 3, "RechargeTime" => 255, "Range" or "Delay" or "Shot1Delay" or "Shot2Delay" or "Shot3Delay" => 1000, _ when signed => 32767, _ => throw new InvalidDataException("Unknown original fixed-point control.") },
                    _ => throw new InvalidDataException("Unknown fixed-point family.")
                };
                result.Add((family, name), Fixed(signed, maximum));
            }
        foreach (var name in new[] { "Cost", "BuildPoints", "Health", "HealthGain", "ManaCost" }) result.Add(("Doors", name), new(false, false, false, 0, 65535));
        foreach (var name in new[] { "GoldCost", "BuildPoints", "Health", "HealthGain", "ManaUsage", "ManaCostToFire", "Threat" }) result.Add(("Traps", name), new(false, false, false, 0, 65535));
        result.Add(("Traps", "TriggerData"), new(false, false, false, 0, 32767));
        result.Add(("Spells", "ManaCost"), new(false, false, false, 0, 999999));
        foreach (var name in new[] { "ManaDrain", "UpgRechargeTime" }) result.Add(("Spells", name), new(false, false, false, 0, 32767));
        result.Add(("Rooms", "Cost"), new(false, false, false, 0, 65535));
        result.Add(("Rooms", "TorchIntensity"), new(false, false, false, 0, 255));
        foreach (var name in new[] { "Res1Rate", "Res2Rate", "Res3Rate", "Res4Rate" }) result.Add(("Creatures", name), new(false, false, true, 0, 100));
        // Properties_GlobalTerrainProc EN_CHANGE: ordinary controls retain
        // these same source limits in the named page and numeric table.
        result.Add(("Terrains", "Damage"), new(false, false, false, -32768, 32767));
        foreach (var name in new[] { "GoldValue", "StartingHealth", "MaxHealth", "ManaGain", "MaxManaGain" }) result.Add(("Terrains", name), new(false, false, false, 0, 65535));
        foreach (var name in new[] { "WibbleH", "WibbleV" }) result.Add(("Terrains", name), new(false, false, false, 0, 255));
        if (result.Values.Count(format => format.Fractional) != 42 || result.Values.Count(format => format.SignedAttribute) != 13) throw new InvalidDataException("Incomplete original numeric controls.");
        return new ReadOnlyDictionary<(string, string), GlobalDefinitionNumberFormat>(result);
    }
    // EN_CHANGE checks the whole part, so the fractional endpoint is accepted
    // too. Restrict new values to the last representable step in that interval.
    private static GlobalDefinitionNumberFormat Fixed(bool signed, int maximum) => new(true, false, false, signed ? -maximum - 4095m / 4096m : 0, maximum + 4095m / 4096m);
}

public sealed partial class NativeMapDocument
{
    public void SetGlobalDefinitionDisplayValues(IEnumerable<GlobalDefinitionChange> changes)
    {
        var rows = GlobalDefinitionRows().ToDictionary(row => (row.Family, row.Index));
        var values = new Dictionary<(string Family, int Index), IReadOnlyList<GlobalDefinitionValue>>();
        var converted = changes.Select(change =>
        {
            if (!rows.TryGetValue((change.Family, change.Index), out var row)) throw new ArgumentException("Unknown definition target.");
            if (!row.Editable) throw new InvalidOperationException("A verified definition layout is required.");
            if (!values.TryGetValue((row.Family, row.Index), out var fields)) values.Add((row.Family, row.Index), fields = GlobalDefinitionValues(row));
            var value = fields.SingleOrDefault(value => value.Name == change.Field) ?? throw new ArgumentException("Unknown definition field.");
            var storage = IsLegacyProject ? LegacyProjectSchemas.Fields(GlobalDefinitionDefaults.Structure(change.Family)).Single(field => field.Name == change.Field) : row.Record!.Fields.Single(field => field.Name == change.Field);
            return change with { Value = GlobalDefinitionNumbers.Encode(change.Family, change.Field, change.Value, value.Value, storage) };
        }).ToArray();
        SetGlobalDefinitionValues(converted);
    }
}
