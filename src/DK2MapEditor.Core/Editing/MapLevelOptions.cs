using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record MapLevelFlag(string Key, string Field, byte Mask, bool Inverted = false);
public sealed record MapLevelScalar(string Field, int Minimum, int Maximum);
public sealed record MapLevelReward(string Value, string English, string Russian, int? Icon)
{
    public string Name(string language) => language == "ru" ? Russian : English;
}
public sealed record MapLevelInput(string Field, string Value);

public static class MapLevelOptions
{
    public static IReadOnlyList<MapLevelScalar> Scalars { get; } = Array.AsReadOnly(new[] {
        new MapLevelScalar("HornyTalismanPieces", 0, 4), new("GameTicks", 1, 50), new("Soundtrack", 1, 3), new("SpecialLevelNumber", 0, 16) });
    public static IReadOnlyList<string> RewardFields { get; } = Array.AsReadOnly(new[] { "RewardPrev1", "RewardPrev2", "RewardPrev3", "RewardNext1", "RewardNext2", "RewardNext3" });
    public static IReadOnlyList<MapLevelFlag> Flags { get; } = Array.AsReadOnly(new[] {
        new MapLevelFlag("Secret", "Options", 0x20), new("Special", "Options", 0x40), new("Multiplayer", "Options2", 0x04),
        new("Skirmish", "Options2", 0x08), new("PetDungeon", "Options2", 0x20), new("Imprison", "Options", 0x08),
        new("Gem", "Options2", 0x02), new("HornyOnce", "Options", 0x10), new("Objectives", "Options2", 0x01),
        new("LockOptions", "Options2", 0x10), new("LockPossession", "Options", 0x04, true), new("KilledHeroes", "Options", 0x80) });
    public static IReadOnlyList<string> Fields { get; } = Array.AsReadOnly(new[] { "Options", "Options2" }.Concat(Scalars.Select(row => row.Field)).Concat(RewardFields).ToArray());
    public static IReadOnlyList<MapLevelReward> Rewards { get; } = BuildRewards();
    private static IReadOnlyList<MapLevelReward> BuildRewards()
    {
        var entries = LegacyCompilerRules.Elements("Rewards");
        if (entries.Count != 48 || entries.Select(row => row.ID).Distinct().Count() != 48) throw new InvalidDataException("Unverified original reward catalogue.");
        return Array.AsReadOnly(new[] { new MapLevelReward("0", "None", "Нет", null) }.Concat(entries.Select(row => {
            var name = ValueCatalogues.Named(row, row.ID.ToString(CultureInfo.InvariantCulture));
            return new MapLevelReward(name.Value, name.English, name.Russian, row.Icon);
        })).ToArray());
    }
    public static byte KnownMask(string field) => field switch { "Options" => 0xFC, "Options2" => 0x3F, _ => throw new ArgumentException("Select a level flag byte.") };
    public static string StorageField(bool project, string field) => !Fields.Contains(field) ? throw new ArgumentException("Unknown level option.")
        : project ? field : LegacyCompilerRules.Metadata.Single(row => row.Source == field).Target;
    public static bool Checked(MapLevelFlag flag, int raw) => ((raw & flag.Mask) != 0) != flag.Inverted;
    public static int SetChecked(MapLevelFlag flag, int raw, bool value) => value != flag.Inverted ? raw | flag.Mask : raw & ~flag.Mask;
}

public sealed class MapLevelOptionsDraft
{
    internal MapLevelOptionsDraft(NativeMapDocument owner, long revision, byte[] fingerprint, NativeRecord? record, IReadOnlyDictionary<string, string> baseline, IReadOnlyDictionary<string, string> values)
    { Owner = owner; Revision = revision; Fingerprint = fingerprint; Record = record; Baseline = baseline; Values = values; }
    internal NativeMapDocument Owner { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    public NativeRecord? Record { get; }
    internal IReadOnlyDictionary<string, string> Baseline { get; }
    public IReadOnlyDictionary<string, string> Values { get; }
    public bool Editable => Values.Count != 0;
    public bool Partial => Values.Count != MapLevelOptions.Fields.Count;
    public bool Changed => Values.Any(row => row.Value != Baseline[row.Key]);
    internal MapLevelOptionsDraft With(Dictionary<string, string> values) => new(Owner, Revision, Fingerprint, Record, Baseline, new ReadOnlyDictionary<string, string>(values));
}

public sealed partial class NativeMapDocument
{
    public MapLevelOptionsDraft CreateMapLevelOptionsDraft()
    {
        var candidates = Records().Where(record => IsLegacyProject ? record.File.Signature == LegacyInfo : record.File == MainFile && record.Category == "Map").ToArray();
        var record = candidates.Length == 1 ? candidates[0] : null;
        var values = new Dictionary<string, string>();
        var verified = record is not null && (IsLegacyProject ? record.File.UInt32(4) is >= 1 and <= 7 && record.Category.StartsWith("Project.MAPPROPERTIES", StringComparison.Ordinal) : record.File.UInt32(4) == 4);
        if (verified)
            foreach (var key in MapLevelOptions.Fields)
            {
                var name = MapLevelOptions.StorageField(IsLegacyProject, key); var field = record!.Fields.SingleOrDefault(field => field.Name == name);
                // Older project V4 has only Options; absent fields are not pad aliases.
                if (field is not null && field.Size == 1 && field.Kind == NativeValueKind.Byte && field.BitMask == 0) values.Add(key, record.Read(field));
            }
        var readOnly = new ReadOnlyDictionary<string, string>(values);
        return new(this, EditRevision, UnusedScriptFingerprint(), record, readOnly, readOnly);
    }
    private NativeRecord VerifyMapLevelOptionsDraft(MapLevelOptionsDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!ReferenceEquals(draft.Owner, this) || draft.Revision != EditRevision || !UnusedScriptFingerprint().AsSpan().SequenceEqual(draft.Fingerprint))
            throw new InvalidOperationException("Refresh the level-options draft after document changes.");
        var current = CreateMapLevelOptionsDraft();
        if (!draft.Editable || !current.Editable || current.Record is null || draft.Record?.File != current.Record.File
            || draft.Record.Offset != current.Record.Offset || draft.Record.Length != current.Record.Length
            || !draft.Baseline.OrderBy(row => row.Key).SequenceEqual(current.Values.OrderBy(row => row.Key)))
            throw new InvalidOperationException("A current verified level-options layout is required.");
        return current.Record;
    }
    public MapLevelOptionsDraft UpdateMapLevelOptions(MapLevelOptionsDraft draft, IEnumerable<MapLevelInput> inputs)
    {
        var record = VerifyMapLevelOptionsDraft(draft); var requested = inputs.ToArray();
        if (!requested.Select(row => row.Field).Order().SequenceEqual(draft.Values.Keys.Order())) throw new ArgumentException("Provide each available level option exactly once.");
        var values = new Dictionary<string, string>();
        foreach (var input in requested)
        {
            var number = int.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture); var text = number.ToString(CultureInfo.InvariantCulture);
            if (text != draft.Baseline[input.Field])
            {
                if (input.Field is "Options" or "Options2")
                {
                    var original = int.Parse(draft.Baseline[input.Field], CultureInfo.InvariantCulture);
                    if (number is < 0 or > 255 || ((number ^ original) & ~MapLevelOptions.KnownMask(input.Field)) != 0)
                        throw new ArgumentException("Named level flags must retain unknown imported bits.");
                }
                else if (MapLevelOptions.Scalars.FirstOrDefault(row => row.Field == input.Field) is { } scalar)
                { if (number < scalar.Minimum || number > scalar.Maximum) throw new ArgumentOutOfRangeException(nameof(inputs), "Use the original level-option range."); }
                else if (!MapLevelOptions.Rewards.Any(row => row.Value == text)) throw new ArgumentException("Choose a source reward; unknown imported values may only be retained.");
                record.Fields.Single(field => field.Name == MapLevelOptions.StorageField(IsLegacyProject, input.Field)).Encode(text);
            }
            values.Add(input.Field, text);
        }
        return draft.With(values);
    }
    public void ApplyMapLevelOptions(MapLevelOptionsDraft draft)
    {
        var record = VerifyMapLevelOptionsDraft(draft); if (!draft.Changed) return;
        Edit("Change map level options", () => {
            foreach (var row in draft.Values.Where(row => row.Value != draft.Baseline[row.Key]))
                SetField(record, record.Fields.Single(field => field.Name == MapLevelOptions.StorageField(IsLegacyProject, row.Key)), row.Value);
        });
    }
}
