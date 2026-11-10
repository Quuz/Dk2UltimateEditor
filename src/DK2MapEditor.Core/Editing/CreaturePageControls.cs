using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record CreaturePageInput(string Field, string Value);

/// <summary>Complete private source page, retaining its row/section/storage baseline.</summary>
public sealed class CreaturePageDraft
{
    internal NativeMapDocument Owner { get; }
    public int Index { get; }
    public string Section { get; }
    public bool Editable { get; }
    public bool Native { get; }
    internal IReadOnlyList<(string Field, string Raw)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values, defaults;
    internal CreaturePageDraft(NativeMapDocument owner, int index, string section, bool editable, bool native,
        IReadOnlyList<(string Field, string Raw)> baseline, IReadOnlyDictionary<string, string> values, IReadOnlyDictionary<string, string> defaults)
    { Owner = owner; Index = index; Section = section; Editable = editable; Native = native; Baseline = baseline; this.values = values; this.defaults = defaults; }
    public string Raw(string field) => values[field];
    public string Display(string field) => CreaturePageControls.Display(Section, field, Raw(field));
    public string DefaultDisplay(string field) => CreaturePageControls.Display(Section, field, defaults[field]);
    public bool Changed => Baseline.Any(item => Raw(item.Field) != item.Raw);
    public bool SameBaseline(CreaturePageDraft other) => ReferenceEquals(Owner, other.Owner) && Index == other.Index
        && Section == other.Section && Editable == other.Editable && Native == other.Native && Baseline.SequenceEqual(other.Baseline);
    internal CreaturePageDraft With(Dictionary<string, string> updated) => new(Owner, Index, Section, Editable, Native, Baseline,
        new ReadOnlyDictionary<string, string>(updated), defaults);
}

/// <summary>The complete Attributes, Parameters, Flags and Light source controls.</summary>
public static partial class CreaturePageControls
{
    public static IReadOnlyList<string> Sections { get; } = Array.AsReadOnly(new[] { "Attributes", "Parameters", "Flags", "Light" });
    public static IReadOnlyList<string> Fields(string section)
    {
        if (!Sections.Contains(section)) throw new ArgumentException("Choose a verified creature page.");
        return CreatureGlobalsSession.Sections.Single(page => page.Name == section).Fields;
    }
    public static bool Boolean(string section, string field) => Fields(section).Contains(field)
        && (section == "Flags" || field is "LightFlicker" or "LightPulse" or "LightPlayer");
    public static bool Named(string field) => field is "ArmourType" or "FightStyle" or "JobClass" or "LairObject"
        or "SlapEffect" or "HealthEffect" or "EntranceEffect" or "DeathEffect" or "Attract1Type" or "Attract1ID" or "Attract2Type" or "Attract2ID";
    public static string Display(string section, string field, string raw)
    {
        if (!Fields(section).Contains(field)) throw new ArgumentException("Unknown creature page field.");
        if (Boolean(section, field)) return ((raw != "0") ^ (field == "DoNotTrain")) ? "1" : "0";
        return GlobalDefinitionNumbers.Display("Creatures", field, raw);
    }
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<ValueChoice>>> catalogues = new(() =>
    {
        using var stream = typeof(CreaturePageControls).Assembly.GetManifestResourceStream("Legacy.Datas_GlobalVariables.c")!;
        using var reader = new StreamReader(stream); var text = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
        (string Name, int Count)[] lists = [("GFightStyle",5),("GJobClass",4),("GBeds",13),("GARooms",15)];
        return new ReadOnlyDictionary<string, IReadOnlyList<ValueChoice>>(lists.ToDictionary(list => list.Name, list =>
        {
            var body = Regex.Match(text, @"ELEMENT\s+" + list.Name + @"\[\]\s*=\s*\{([\s\S]*?)\};").Groups[1].Value;
            var entries = Regex.Matches(body, @"\{\s*(0x[0-9A-Fa-f]+)\s*,[^{}]*?\b(sz\w+)\s*,")
                .Select(match => new LegacyCompilerRules.Element(uint.Parse(match.Groups[1].Value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture),0,0,match.Groups[2].Value)).ToArray();
            if (entries.Length != list.Count || entries.Select(entry => entry.ID).Distinct().Count() != list.Count)
                throw new InvalidDataException("Unverified creature parameter catalogue: " + list.Name);
            return (IReadOnlyList<ValueChoice>)Array.AsReadOnly(entries.Select(entry => ValueCatalogues.Named(entry, entry.ID.ToString(CultureInfo.InvariantCulture))).ToArray());
        }));
    });
    public static IReadOnlyList<ValueChoice> Choices(string field, string attractionType = "0") => field switch
    {
        "ArmourType" => GlobalDefinitionDecorations.Choices("Creatures", field),
        "FightStyle" => catalogues.Value["GFightStyle"], "JobClass" => catalogues.Value["GJobClass"], "LairObject" => catalogues.Value["GBeds"],
        "SlapEffect" or "HealthEffect" or "EntranceEffect" or "DeathEffect" => EffectCatalogue.Choices,
        "Attract1Type" or "Attract2Type" => Array.AsReadOnly(Enumerable.Range(0, field == "Attract1Type" ? 2 : 5)
            .Select(index => ValueCatalogues.Named(new((uint)index,0,0,"szAttractionType"+index),index.ToString(CultureInfo.InvariantCulture))).ToArray()),
        "Attract1ID" => attractionType == "1" ? catalogues.Value["GARooms"] : [],
        "Attract2ID" => attractionType switch { "1" => catalogues.Value["GARooms"], "4" => GlobalDefinitionDefaults.Catalogue("Creatures").Select(entry => ValueCatalogues.Named(entry,entry.ID.ToString(CultureInfo.InvariantCulture))).ToArray(), _ => [] },
        _ => throw new ArgumentException("This creature field has no named catalogue.")
    };
    public static bool Enabled(string field, string attractionType = "0") => field switch
    { "Attract1ID" => attractionType == "1", "Attract2ID" => attractionType is "1" or "4", "Attract2Data" => attractionType is "1" or "2" or "3" or "4", _ => true };
    public static GlobalDefinitionNumberFormat Format(string field, bool native) => field switch
    {
        "Attract2Data" => new(false,false,false,0,16000),
        "LightR" or "LightG" or "LightB" => new(false,false,false,0,255),
        _ => GlobalDefinitionNumbers.Format("Creatures", field, native)
    };
    public static string Step(string section, string field, string input, int direction, bool native)
    {
        if (!Fields(section).Contains(field) || Named(field) || Boolean(section,field) || direction is not (-1 or 1))
            throw new ArgumentException("Choose an original numeric creature control.");
        if(section=="Light"&&CreatureLightInputs.Handles(field))return CreatureLightInputs.Step(field,input,direction);
        if(section=="Parameters"&&field=="Attract2Data")return CreatureParameterInputs.StepAmount(input,direction);
        if(section=="Attributes"&&CreatureAttributeInputs.Handles(field))return CreatureAttributeInputs.Step(field,input,direction,native);
        var format = Format(field,native);
        var value = decimal.Parse(input.Trim().Replace(',','.'),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture);
        var min = decimal.Ceiling(format.Minimum ?? throw new InvalidDataException("Missing source minimum."));
        var max = decimal.Floor(format.Maximum ?? throw new InvalidDataException("Missing source maximum."));
        // Misc_SetUpDownInt reads the signed whole part, then replaces the text.
        return Math.Clamp(decimal.Truncate(value)+direction,min,max).ToString(CultureInfo.InvariantCulture);
    }
    public static uint Colour(CreaturePageDraft draft)
    {
        if (draft.Section != "Light") throw new ArgumentException("Choose the creature light page.");
        // Original RGB casts each component to BYTE, without repairing its LONG.
        uint Component(string field) => unchecked((byte)int.Parse(draft.Raw(field),CultureInfo.InvariantCulture));
        return Component("LightR") | Component("LightG")<<8 | Component("LightB")<<16;
    }
}

public sealed partial class NativeMapDocument
{
    public CreaturePageDraft CreateCreaturePageDraft(GlobalDefinitionRow row, string section)
    {
        if (row.Family != "Creatures") throw new ArgumentException("Choose a creature definition.");
        var names = CreaturePageControls.Fields(section);
        var values = GlobalDefinitionValues(row).Where(value => value.Section == section).ToDictionary(value => value.Name);
        if (!names.Order().SequenceEqual(values.Keys.Order())) throw new InvalidDataException("Incomplete creature page layout.");
        return new(this,row.Index,section,values.Values.All(value => value.Editable),!IsLegacyProject,
            Array.AsReadOnly(names.Select(field => (field,values[field].Value)).ToArray()),
            new ReadOnlyDictionary<string,string>(values.ToDictionary(item => item.Key,item => item.Value.Value)),
            new ReadOnlyDictionary<string,string>(values.ToDictionary(item => item.Key,item => item.Value.DefaultValue)));
    }
    private GlobalDefinitionRow VerifyCreaturePageDraft(CreaturePageDraft draft)
    {
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == "Creatures" && row.Index == draft.Index) ?? throw new ArgumentException("Unknown creature definition.");
        var current = CreateCreaturePageDraft(row,draft.Section);
        if (!draft.Editable || !current.Editable || !draft.SameBaseline(current)) throw new InvalidOperationException("A verified current creature page is required.");
        return row;
    }
    public CreaturePageDraft UpdateCreaturePageDraft(CreaturePageDraft draft, IEnumerable<CreaturePageInput> inputs)
    {
        var row = VerifyCreaturePageDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(CreaturePageControls.Fields(draft.Section).Order()))
            throw new ArgumentException("Provide every creature page field exactly once.");
        var text = requested.ToDictionary(input => input.Field,input => input.Value);
        if (draft.Section == "Parameters")
        {
            foreach (var typeField in new[] { "Attract1Type", "Attract2Type" })
                if (text[typeField].Trim() != draft.Display(typeField))
                    text[typeField] = uint.Parse(text[typeField],NumberStyles.Integer,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            // Explicit source Save cleanup, before reading disabled data controls.
            if (text["Attract1Type"] == "0") text["Attract1ID"] = "0";
            if (text["Attract2Type"] == "0") { text["Attract2ID"] = "0"; text["Attract2Data"] = "0"; }
            if (text["Attract2Type"] is "2" or "3") text["Attract2ID"] = "0";
        }
        var values = new Dictionary<string,string>();
        foreach (var field in CreaturePageControls.Fields(draft.Section))
        {
            var raw = draft.Raw(field); var value = text[field].Trim();
            var storage = IsLegacyProject ? LegacyProjectSchemas.Fields("GLOBALCREATURE").Single(item => item.Name == field) : row.Record!.Fields.Single(item => item.Name == field);
            string encoded;
            if (CreaturePageControls.Boolean(draft.Section,field))
            {
                if (value is not ("0" or "1")) throw new ArgumentException("Choose a checkbox state.");
                encoded = value == draft.Display(field) ? raw : ((value == "1") ^ (field == "DoNotTrain") ? "1" : "0");
            }
            else if (CreaturePageControls.Named(field))
            {
                encoded = value == draft.Display(field) ? raw : uint.Parse(value,NumberStyles.Integer,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                var type = field == "Attract1ID" ? text["Attract1Type"] : field == "Attract2ID" ? text["Attract2Type"] : "0";
                if (encoded != raw && !CreaturePageControls.Choices(field,type).Any(choice => choice.Value == encoded)
                    && !(field is "Attract1ID" or "Attract2ID" && !CreaturePageControls.Enabled(field,type) && encoded == "0"))
                    throw new ArgumentException("Choose a source parameter; unknown imports may only be retained.");
            }
            else if (field is "Attract2Data" or "LightR" or "LightG" or "LightB")
            {
                encoded = int.Parse(value,NumberStyles.Integer,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                var number = int.Parse(encoded,CultureInfo.InvariantCulture); var format = CreaturePageControls.Format(field,!IsLegacyProject);
                if (encoded != raw && (number < format.Minimum || number > format.Maximum)) throw new ArgumentOutOfRangeException(nameof(inputs),"Value exceeds the original control range.");
            }
            else encoded = GlobalDefinitionNumbers.Encode("Creatures",field,value,raw,storage);
            if (encoded != raw) storage.Encode(encoded);
            values[field] = encoded;
        }
        return draft.With(values);
    }
    public CreaturePageDraft UpdateCreaturePageColour(CreaturePageDraft draft,uint colour)
    {
        if (draft.Section != "Light" || colour > 0xFFFFFF) throw new ArgumentException("Choose a 24-bit creature light colour.");
        return UpdateCreaturePageDraft(draft,CreaturePageControls.Fields("Light").Select(field => new CreaturePageInput(field,
            field switch { "LightR" => (colour&255).ToString(CultureInfo.InvariantCulture),"LightG" => ((colour>>8)&255).ToString(CultureInfo.InvariantCulture),"LightB" => ((colour>>16)&255).ToString(CultureInfo.InvariantCulture),_ => draft.Display(field) })));
    }
    public void ApplyCreaturePageDraft(CreaturePageDraft draft)
    {
        VerifyCreaturePageDraft(draft); if (!draft.Changed) return;
        SetGlobalDefinitionValues(draft.Baseline.Where(item => draft.Raw(item.Field) != item.Raw).Select(item => new GlobalDefinitionChange("Creatures",draft.Index,item.Field,draft.Raw(item.Field))));
    }
}
