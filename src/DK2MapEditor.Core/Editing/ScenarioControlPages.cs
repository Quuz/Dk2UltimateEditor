using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public enum ScenarioControlKind { Choice, Number, Duration, Percentage, NamedChoice, Element }
public sealed record ScenarioControl(int Parameter, ScenarioControlKind Kind, long Maximum, string Caption, int SecondaryParameter = 0, bool Unsigned = false, int DisplayOffset = 0);
public enum ScenarioRepeatMode { Once, Count, Infinite }

/// <summary>Source-specific page schemas. Unimplemented pages are deliberately
/// excluded; retained project payloads are not inferred to be visible controls.</summary>
public static class ScenarioControlPages
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<ScenarioControl>> pages = Build();
    private static IReadOnlyDictionary<string, IReadOnlyList<ScenarioControl>> Build()
    {
        var result = new Dictionary<string, IReadOnlyList<ScenarioControl>>(StringComparer.Ordinal);
        void Add(string type, params ScenarioControl[] controls) => result.Add(type, Array.AsReadOnly(controls));
        ScenarioControl Comparison() => new(1, ScenarioControlKind.Choice, 6, "Comparison");
        Add("TRIGGER_TIMEELAPSED", Comparison(), new(5, ScenarioControlKind.Duration, 86399, "Elapsed"));
        Add("TRIGGER_HURTED", Comparison(), new(5, ScenarioControlKind.Number, 999, "Health", Unsigned: true));
        Add("TRIGGER_GOLDMINED", Comparison(), new(5, ScenarioControlKind.Number, 999999999, "GoldMined", Unsigned: true));
        Add("TRIGGER_GOLDAMOUNT", Comparison(), new(5, ScenarioControlKind.Number, 9999999, "Gold", Unsigned: true));
        Add("TRIGGER_MANAAMOUNT", Comparison(), new(5, ScenarioControlKind.Number, 999999, "Mana", Unsigned: true));
        Add("TRIGGER_SLAPCOUNT", Comparison(), new(5, ScenarioControlKind.Number, 9999, "Slaps", Unsigned: true));
        Add("TRIGGER_GROUPINPOSSESS", Comparison(), new(5, ScenarioControlKind.Number, 9999, "GroupCount", Unsigned: true));
        foreach (var type in new[] { "ACTION_SYSTEMMESSAGES", "ACTION_MOODS", "ACTION_TIMERSPEECH", "ACTION_PORTALS", "ACTION_CHANGEVIEW", "ACTION_RUN", "ACTION_ATTACK", "ACTION_PRIMARYTARGET" })
            Add(type, new ScenarioControl(1, ScenarioControlKind.Choice, type == "ACTION_PRIMARYTARGET" ? 3 : 1, ""));
        Add("ACTION_SHOWHEALTH", new ScenarioControl(1, ScenarioControlKind.Number, 9999, "HealthDuration", Unsigned: true));
        Add("ACTION_SLAPSREMAINING", new ScenarioControl(5, ScenarioControlKind.Number, 9999, "SlapsRemaining", Unsigned: true));
        Add("ACTION_MUSICLEVEL", new ScenarioControl(1, ScenarioControlKind.Percentage, 100, "Music"));
        Add("ACTION_DISPLAYTIMER", new ScenarioControl(5, ScenarioControlKind.Duration, 86399, "TimeLimit"));
        ScenarioControl Named(int parameter) => new(parameter, ScenarioControlKind.NamedChoice, 0, "");
        ScenarioControl Count(long maximum = 9999) => new(5, ScenarioControlKind.Number, maximum, "", Unsigned: true);
        Add("TRIGGER_CREATUREEXP", Comparison(), Named(5));
        foreach (var type in new[] { "TRIGGER_CREATURES", "TRIGGER_HAPPYCREATURES", "TRIGGER_ANGRYCREATURES", "TRIGGER_ROOMS", "TRIGGER_ROOMSTYPES", "TRIGGER_ROOMSSIZE", "TRIGGER_ROOMSCONTENT", "TRIGGER_CLAIMAREA", "TRIGGER_SELECTAREA", "TRIGGER_STUNNEDINBAND", "TRIGGER_JAILEDINBAND", "TRIGGER_DEATHSINBAND" })
            Add(type, Comparison(), Named(2), Count());
        foreach (var type in new[] { "TRIGGER_LOSTCREATURES", "TRIGGER_KILLEDCREATURES", "TRIGGER_CREATURESLEVEL" }) Add(type, Comparison(), Named(4), Count());
        foreach (var type in new[] { "TRIGGER_DOORS", "TRIGGER_TRAPS" }) Add(type, Comparison(), Named(2), new(3, ScenarioControlKind.Choice, 17, ""), Count());
        // GetDlgItemInt(..., FALSE), without an EM_LIMITTEXT: the operand's
        // actual storage width remains an additional bound on explicit input.
        Add("TRIGGER_DESTROYED", Comparison(), Named(2), Count(uint.MaxValue));
        Add("TRIGGER_SPELLS", Named(2));
        Add("TRIGGER_CLICKELEMENT", new ScenarioControl(1, ScenarioControlKind.Element, 0, "Element", 2));
        Add("TRIGGER_HEARTDESTROYED", Named(1));
        foreach (var type in new[] { "TRIGGER_ENTERAREAINPOSS", "TRIGGER_CLAIMENTIREAREA", "TRIGGER_SELECTALLAREA", "TRIGGER_BANDCREATED" }) Add(type, Named(2));
        foreach (var type in new[] { "TRIGGER_CREAENTERAREA", "TRIGGER_AREACONTENT" }) Add(type, Comparison(), Named(2), Named(3), Count());
        Add("ACTION_ADDELEMENT", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(2, ScenarioControlKind.Element, 0, "Element", 3), new ScenarioControl(4, ScenarioControlKind.Choice, 1, ""));
        Add("ACTION_SHOWAREA", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(2, ScenarioControlKind.Choice, 1, ""));
        Add("ACTION_ENABLEFOG", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(2, ScenarioControlKind.Choice, 1, ""));
        Add("ACTION_ALLIES", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(2, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(3, ScenarioControlKind.Choice, 1, ""));
        Add("ACTION_RELEASEBAND", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(2, ScenarioControlKind.Choice, 2, ""), new ScenarioControl(5, ScenarioControlKind.NamedChoice, 0, ""));
        Add("ACTION_TOGGLEEFFECT", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(2, ScenarioControlKind.Choice, 1, ""));
        Add("ACTION_SHOWELEMENT", new ScenarioControl(1, ScenarioControlKind.Element, 0, "Element", 2), new ScenarioControl(3, ScenarioControlKind.Choice, 1, ""), new ScenarioControl(5, ScenarioControlKind.Number, 999999, "", Unsigned: true));
        Add("ACTION_MOVECAMERA", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""));
        Add("ACTION_ROTATECAMERA", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(3, ScenarioControlKind.Number, 999, "", Unsigned: true), new ScenarioControl(5, ScenarioControlKind.Number, 9999, "", Unsigned: true));
        Add("ACTION_GENERATE", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(2, ScenarioControlKind.NamedChoice, 0, ""));
        Add("ACTION_SHOWNEXTROOM", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""));
        Add("ACTION_CHANGEPARAMS", new ScenarioControl(1, ScenarioControlKind.NamedChoice, 0, ""), new ScenarioControl(2, ScenarioControlKind.Choice, 27, ""), new ScenarioControl(5, ScenarioControlKind.NamedChoice, 0, ""));
        Add("ACTION_CAMERAPATH", new ScenarioControl(1, ScenarioControlKind.Number, 9999, "", Unsigned: true), Named(2), new ScenarioControl(3, ScenarioControlKind.Choice, 1, ""));
        Add("ACTION_ADDCREATURE", Named(1), Named(2), Named(3), new ScenarioControl(4, ScenarioControlKind.Number, uint.MaxValue, "", Unsigned: true),
            new ScenarioControl(5, ScenarioControlKind.Number, 999, "", Unsigned: true), new ScenarioControl(7, ScenarioControlKind.Number, 999, "", Unsigned: true));
        Add("ACTION_CHANGESQUARE", Named(1), Named(2), new(5, ScenarioControlKind.Number, 1000, "X", Unsigned: true, DisplayOffset: 1),
            new(7, ScenarioControlKind.Number, 1000, "Y", Unsigned: true, DisplayOffset: 1));
        Add("ACTION_CHANGEOWNER", Named(2), new(5, ScenarioControlKind.Number, 1000, "X", Unsigned: true, DisplayOffset: 1),
            new(7, ScenarioControlKind.Number, 1000, "Y", Unsigned: true, DisplayOffset: 1));
        Add("ACTION_DESTROYGATE");
        Add("ACTION_SAY", new(1, ScenarioControlKind.Number, 9999, "", Unsigned: true), new(5, ScenarioControlKind.Number, 1, "", Unsigned: true));
        return result;
    }
    /// <summary>Original ParamsCheck prerequisites, using actual current map identities.
    /// Unknown imported operands remain untouched; absent required collections block Apply.</summary>
    public static IReadOnlyList<EntityKind> MissingRequirements(string type, NativeMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        EntityKind[] required = type switch
        {
            "TRIGGER_BANDCREATED" or "TRIGGER_DEATHSINBAND" or "TRIGGER_JAILEDINBAND" or "TRIGGER_STUNNEDINBAND" => [EntityKind.Band],
            "ACTION_SHOWAREA" or "ACTION_ENABLEFOG" or "ACTION_MOVECAMERA" or "ACTION_ROTATECAMERA" or "ACTION_CAMERAPATH" => [EntityKind.Area],
            "ACTION_RELEASEBAND" => [EntityKind.Band, EntityKind.Area],
            "ACTION_TOGGLEEFFECT" => [EntityKind.Effect],
            _ => []
        };
        if (type == "ACTION_DESTROYGATE")
            return document.ScenarioGateChoices().Count == 0 ? Array.AsReadOnly(new[] { EntityKind.Gate }) : [];
        if (required.Length == 0) return [];
        var present = document.Records().Select(EntityReferences.Identity).OfType<EntityIdentity>().Select(item => item.Kind).ToHashSet();
        return Array.AsReadOnly(required.Where(kind => !present.Contains(kind)).ToArray());
    }
    public static bool Supports(string type) => NativeMapDocument.HasScenarioAliasPage(type) || pages.ContainsKey(type)
        || type.StartsWith("TRIGGER_", StringComparison.Ordinal) && LegacyEventSchemas.All.Any(item => item.Name == type) && LegacyEventSchemas.ParameterPageID(type) == -1;
    public static IReadOnlyList<ScenarioControl> Controls(string type)
    {
        if (!Supports(type)) throw new InvalidOperationException("This original parameter page is not implemented yet.");
        return pages.TryGetValue(type, out var controls) ? controls : [];
    }
    public static IReadOnlyList<ValueChoice> Choices(ScenarioParameterDraft draft, ScenarioControl control, NativeMapDocument document)
        => control.Kind is ScenarioControlKind.NamedChoice or ScenarioControlKind.Element
            ? ScenarioNamedCatalogues.Choices(draft.Type, control.Parameter, document)
            : ScenarioParameterChoices.ForField(draft.Record, draft.Fields.Single(field => field.Name == "param" + control.Parameter || field.Name == "Param" + control.Parameter));
    public static uint Duration(string hours, string minutes, string seconds)
    {
        var h = uint.Parse(hours, NumberStyles.None, CultureInfo.InvariantCulture);
        var m = uint.Parse(minutes, NumberStyles.None, CultureInfo.InvariantCulture);
        var s = uint.Parse(seconds, NumberStyles.None, CultureInfo.InvariantCulture);
        if (h > 23 || m > 59 || s > 59) throw new ArgumentOutOfRangeException(nameof(hours), "The original time controls accept 23:59:59 at most.");
        return h * 3600 + m * 60 + s;
    }
    public static byte Repeat(ScenarioRepeatMode mode, string count) => mode switch
    {
        ScenarioRepeatMode.Once => 1, ScenarioRepeatMode.Infinite => 255,
        ScenarioRepeatMode.Count => byte.TryParse(count, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value <= 254
            ? value : throw new ArgumentOutOfRangeException(nameof(count), "Repeat count must be between 0 and 254."),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}

public sealed partial class NativeMapDocument
{
    /// <summary>Validate only explicit inputs. Untouched imported/inactive values
    /// survive a page Apply, even when outside the original editing range.</summary>
    public ScenarioParameterDraft UpdateScenarioControlPage(ScenarioParameterDraft draft, IEnumerable<ScenarioParameterValue> inputs)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var controls = ScenarioControlPages.Controls(draft.Type);
        ArgumentNullException.ThrowIfNull(inputs);
        var requested = inputs.ToArray();
        // Original GetDlgItemInt(..., FALSE) returns UINT; the no-limit
        // destroyed-count input may use every DWORD bit, even in a LONG field.
        var encoded = requested.Select(input =>
        {
            if (input is null) return input!;
            var operand = draft.Fields.SingleOrDefault(item => item.Name == input.Field);
            var control = controls.SingleOrDefault(item => input.Field == "param" + item.Parameter || input.Field == "Param" + item.Parameter);
            return operand?.Kind == NativeValueKind.Int32 && control is { Kind: ScenarioControlKind.Number } && (control.Unsigned || control.Maximum == uint.MaxValue)
                && uint.TryParse(input.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                ? input with { Value = unchecked((int)value).ToString(CultureInfo.InvariantCulture) } : input;
        }).ToArray();
        var prepared = UpdateScenarioParameterDraft(draft, encoded);
        if (ScenarioControlPages.MissingRequirements(draft.Type, this).Count != 0)
            throw new InvalidOperationException("The original scenario page requires existing map objects.");
        foreach (var input in requested)
        {
            var operand = draft.Fields.Single(item => item.Name == input.Field);
            var repeat = input.Field is "Repeat" or "repeat";
            var control = controls.SingleOrDefault(item => input.Field == "param" + item.Parameter || input.Field == "Param" + item.Parameter
                || item.SecondaryParameter != 0 && (input.Field == "param" + item.SecondaryParameter || input.Field == "Param" + item.SecondaryParameter));
            if (!repeat && control is null) throw new ArgumentException("This operand has no control on this source page.");
            if (operand.Encode(prepared.Values[input.Field]).AsSpan().SequenceEqual(operand.Encode(draft.Values[input.Field]))) continue;
            if (repeat) continue; // All 256 byte encodings are valid; 255 is Infinite.
            if (control!.Kind is ScenarioControlKind.Choice or ScenarioControlKind.NamedChoice or ScenarioControlKind.Element)
            {
                var value = input.Value;
                if (control.Kind == ScenarioControlKind.Element)
                {
                    long Read(int parameter) => long.Parse(prepared.Values[draft.Fields.Single(field => field.Name == "param" + parameter || field.Name == "Param" + parameter).Name], CultureInfo.InvariantCulture);
                    var category = Read(control.Parameter); var id = Read(control.SecondaryParameter);
                    if (category is < 0 or > 65535 || id is < 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(inputs), "The interface pair is invalid.");
                    value = ((category << 16) | id).ToString(CultureInfo.InvariantCulture);
                }
                else if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) value = number.ToString(CultureInfo.InvariantCulture);
                if (!ScenarioControlPages.Choices(draft, control, this).Any(choice => choice.Value == value))
                    throw new ArgumentOutOfRangeException(nameof(inputs), "Choose an original source option.");
            }
            else if (!long.TryParse(input.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                || number < control.DisplayOffset || number > control.Maximum)
                throw new ArgumentOutOfRangeException(nameof(inputs), "The number exceeds the original page range.");
        }
        return prepared;
    }
}
