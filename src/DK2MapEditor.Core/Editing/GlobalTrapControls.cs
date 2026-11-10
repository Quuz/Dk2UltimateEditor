using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record GlobalTrapInput(string Field, string Value);

/// <summary>One private parent and its submitted child options. ShotDelay is a
/// retained source field with no original editing control.</summary>
public sealed class GlobalTrapDraft
{
    internal NativeMapDocument Owner { get; }
    public int Index { get; }
    public bool Editable { get; }
    internal IReadOnlyList<(string Field, string Raw)> Baseline { get; }
    private readonly IReadOnlyDictionary<string, string> values;
    internal GlobalTrapDraft(NativeMapDocument owner, int index, bool editable, IReadOnlyList<(string Field, string Raw)> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Index = index; Editable = editable; Baseline = baseline; this.values = values; }
    public string Raw(string field) => values[field];
    public string Display(string field) => Raw(field).Length == 0 ? "" : field is "ShotData1" or "ShotData2" && GlobalProjectiles.Find(Raw("ShotType")) is { } option
        ? GlobalProjectiles.DisplayParameter(option, Raw(field), field == "ShotData2") : GlobalDefinitionNumbers.Display("Traps", field, Raw(field));
    public bool Changed => Baseline.Any(item => Raw(item.Field) != item.Raw);
    public bool SameBaseline(GlobalTrapDraft other) => ReferenceEquals(Owner, other.Owner) && Index == other.Index && Editable == other.Editable && Baseline.SequenceEqual(other.Baseline);
    internal GlobalTrapDraft With(Dictionary<string, string> updated) => new(Owner, Index, Editable, Baseline, new ReadOnlyDictionary<string, string>(updated));
    internal Dictionary<string, string> CopyValues() => new(values);
}

public static class GlobalTrapControls
{
    public static IReadOnlyList<string> MainFields { get; } = Array.AsReadOnly(new[] {
        "Material", "Crate", "DeathEffect", "PowerlessEffect", "IdleEffect", "TriggerType", "ShotType",
        "GoldCost", "BuildPoints", "IdleEffectDelay", "Health", "HealthGain", "ChargeTime", "RechargeTime",
        "ManaUsage", "ManaCostToFire", "Threat", "ThreatDuration", "Invisible", "Disarmable", "RevealedWhenFired",
        "RequiresMana", "SolidObstacle", "FPObstacle" });
    public static IReadOnlyList<string> StoredFields { get; } = Array.AsReadOnly(MainFields.Concat(new[] { "TriggerData", "ShotData1", "ShotData2", "ShotDelay" }).ToArray());
    private static readonly HashSet<string> magicDoorInactive = ["Material", "Crate", "DeathEffect", "Invisible", "Disarmable", "RevealedWhenFired", "RequiresMana", "SolidObstacle", "FPObstacle", "GoldCost", "BuildPoints", "Health", "HealthGain", "ManaUsage"];
    public static bool Named(string field) => field is "Material" or "Crate" or "DeathEffect" or "PowerlessEffect" or "IdleEffect" or "TriggerType" or "ShotType";
    public static bool Boolean(string field) => field is "Invisible" or "Disarmable" or "RevealedWhenFired" or "RequiresMana" or "SolidObstacle" or "FPObstacle";
    public static bool Enabled(int index, string field)
    {
        if (!StoredFields.Contains(field)) throw new ArgumentException("Unknown trap control.");
        var catalogue = GlobalDefinitionDefaults.Catalogue("Traps");
        if (catalogue.Count != 15) throw new InvalidDataException("Unverified original trap order.");
        return field != "ShotDelay" && (catalogue[index].ID != 12 || !magicDoorInactive.Contains(field));
    }
    public static IReadOnlyList<ValueChoice> Choices(string field) => field switch
    {
        "Material" or "Crate" or "TriggerType" => GlobalDefinitionRules.Choices("Traps", field),
        "DeathEffect" or "PowerlessEffect" or "IdleEffect" => GlobalDefinitionDecorations.Choices("Traps", field),
        "ShotType" => GlobalProjectiles.Options.Select(option => option.Name).ToArray(),
        _ => throw new ArgumentException("This trap field has no original catalogue.")
    };
}

public sealed partial class NativeMapDocument
{
    public GlobalTrapDraft CreateGlobalTrapDraft(GlobalDefinitionRow row)
    {
        if (row.Family != "Traps") throw new ArgumentException("Choose a trap definition.");
        var values = GlobalDefinitionValues(row).ToDictionary(value => value.Name);
        if (!GlobalTrapControls.StoredFields.All(values.ContainsKey)) throw new InvalidDataException("Unverified complete trap fields.");
        return new(this, row.Index, GlobalTrapControls.StoredFields.All(field => values[field].Editable),
            Array.AsReadOnly(GlobalTrapControls.StoredFields.Select(field => (field, values[field].Value)).ToArray()),
            new ReadOnlyDictionary<string, string>(GlobalTrapControls.StoredFields.ToDictionary(field => field, field => values[field].Value)));
    }
    private GlobalDefinitionRow VerifyGlobalTrapDraft(GlobalTrapDraft draft)
    {
        if (!ReferenceEquals(draft.Owner, this)) throw new InvalidOperationException("Foreign trap draft.");
        var row = GlobalDefinitionRows().SingleOrDefault(row => row.Family == "Traps" && row.Index == draft.Index) ?? throw new ArgumentException("Unknown trap definition.");
        var current = CreateGlobalTrapDraft(row);
        if (!draft.Editable || !current.Editable || !draft.SameBaseline(current)) throw new InvalidOperationException("A verified unchanged trap definition is required.");
        return row;
    }
    private NativeField TrapStorage(GlobalDefinitionRow row, string field) => IsLegacyProject
        ? LegacyProjectSchemas.Fields("GLOBALTRAP").Single(item => item.Name == field) : row.Record!.Fields.Single(item => item.Name == field);
    public GlobalTrapDraft UpdateGlobalTrapDraft(GlobalTrapDraft draft, IEnumerable<GlobalTrapInput> inputs)
    {
        var row = VerifyGlobalTrapDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(input => input.Field).Order().SequenceEqual(GlobalTrapControls.MainFields.Order())) throw new ArgumentException("Provide each main trap control exactly once.");
        var values = draft.CopyValues();
        foreach (var input in requested)
        {
            var current = draft.Raw(input.Field); var storage = TrapStorage(row, input.Field); string encoded;
            if (GlobalTrapControls.Named(input.Field))
            {
                var number = uint.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                encoded = number == uint.Parse(current, NumberStyles.Integer, CultureInfo.InvariantCulture) ? current : number.ToString(CultureInfo.InvariantCulture);
                if (encoded != current && !GlobalTrapControls.Choices(input.Field).Any(choice => choice.Value == encoded)) throw new ArgumentException("Choose an original trap value; unknown imports may only be retained.");
            }
            else if (GlobalTrapControls.Boolean(input.Field))
            {
                var number = int.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                encoded = number == int.Parse(current, NumberStyles.Integer, CultureInfo.InvariantCulture) ? current : number.ToString(CultureInfo.InvariantCulture);
                if (encoded != current && number is not (0 or 1)) throw new ArgumentOutOfRangeException(nameof(inputs), "Source checkbox requires zero or one.");
            }
            else encoded = GlobalDefinitionNumbers.Encode("Traps", input.Field, input.Value, current, storage);
            if (encoded != current)
            {
                if (!GlobalTrapControls.Enabled(draft.Index, input.Field)) throw new InvalidOperationException("This original Magic Door control is inactive.");
                storage.Encode(encoded);
            }
            values[input.Field] = encoded;
        }
        return draft.With(values);
    }
    public GlobalTrapDraft UpdateGlobalTrapTriggerDraft(GlobalTrapDraft draft, string input)
    {
        var row = VerifyGlobalTrapDraft(draft);
        if (!GlobalDefinitionRules.KnownTrigger(draft.Raw("TriggerType"))) throw new InvalidOperationException("Choose an original trigger before submitting its options.");
        var number = int.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var current = draft.Raw("TriggerData"); var encoded = number.ToString(CultureInfo.InvariantCulture);
        if (encoded != current)
        {
            if (number is < 0 or > 32767) throw new ArgumentOutOfRangeException(nameof(input), "Trigger parameter requires 0–32767.");
            TrapStorage(row, "TriggerData").Encode(encoded);
        }
        var values = draft.CopyValues(); values["TriggerData"] = encoded; return draft.With(values);
    }
    public GlobalTrapDraft UpdateGlobalTrapShotDraft(GlobalTrapDraft draft, string data1, string data2)
    {
        var row = VerifyGlobalTrapDraft(draft);
        var option = GlobalProjectiles.Find(draft.Raw("ShotType")) ?? throw new InvalidOperationException("Choose an original projectile before submitting its options.");
        if (option.Form == 255) throw new InvalidOperationException("This projectile has no options.");
        var values = draft.CopyValues();
        string Number(string field, string input, int minimum, int maximum, bool signed)
        {
            var number = int.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (number.ToString(CultureInfo.InvariantCulture) == draft.Display(field)) return draft.Raw(field);
            if (number < minimum || number > maximum) throw new ArgumentOutOfRangeException(nameof(input), "Value is outside the original projectile option range.");
            var storage = TrapStorage(row, field);
            var encoded = signed && storage.Kind == NativeValueKind.UInt16 ? unchecked((ushort)(short)number).ToString(CultureInfo.InvariantCulture) : number.ToString(CultureInfo.InvariantCulture);
            storage.Encode(encoded); return encoded;
        }
        if (option.Form is 0 or 1) values["ShotData1"] = Number("ShotData1", data1, option.Signed1 ? -32768 : 0, 32767, option.Signed1);
        if (option.Form == 0) values["ShotData2"] = Number("ShotData2", data2, option.Signed2 ? -32768 : 0, 32767, option.Signed2);
        if (option.Form is 2 or 3 or 4)
        {
            var target = int.Parse(data1, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (target != draft.Raw("ShotData1"))
            {
                if (!GlobalProjectiles.Targets(option).Any(choice => choice.Value == target)) throw new ArgumentException("Choose an original creature/object target.");
                TrapStorage(row, "ShotData1").Encode(target);
            }
            values["ShotData1"] = target;
            values["ShotData2"] = option.Form switch
            {
                2 => Number("ShotData2", data2, 0, 100, false),
                3 => Number("ShotData2", data2, 1, 10, false),
                4 => target == "1" ? Number("ShotData2", data2, 0, 80000, false) : "0",
                _ => throw new InvalidDataException("Unknown original projectile form.")
            };
        }
        return draft.With(values);
    }
    public void ApplyGlobalTrapDraft(GlobalTrapDraft draft)
    {
        VerifyGlobalTrapDraft(draft); if (!draft.Changed) return;
        SetGlobalDefinitionValues(draft.Baseline.Where(item => draft.Raw(item.Field) != item.Raw).Select(item => new GlobalDefinitionChange("Traps", draft.Index, item.Field, draft.Raw(item.Field))));
    }
}
