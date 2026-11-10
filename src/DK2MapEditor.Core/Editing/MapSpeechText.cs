using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record MapSpeechTextInput(string Field, string Value);
public static class MapSpeechText
{
    public const int DefaultMentorMaximum = 1505;
    public static IReadOnlyList<string> SpeechIds { get; } = Array.AsReadOnly(new[] { "SpeechPreLevelID", "SpeechWinID", "SpeechLooseID", "SpeechNewsID", "SpeechGenericPreLevelID" });
    public static IReadOnlyList<string> TextIds { get; } = Array.AsReadOnly(new[] { "TextTitleID", "TextPlotID", "TextDebriefingID", "TextObjectiveID", "TextSubObjective1ID", "TextSubObjective2ID", "TextSubObjective3ID" });
    public static IReadOnlyList<string> Fields { get; } = Array.AsReadOnly(new[] { "SpeechFile", "TextTableID" }.Concat(SpeechIds).Concat(TextIds)
        .Concat(Enumerable.Range(0, 8).Select(index => "TextIntroCreatureID" + index)).Concat(Enumerable.Range(0, 8).Select(index => "TextIntroTextID" + index)).ToArray());
    public static string StorageField(bool project, string field) => !Fields.Contains(field) ? throw new ArgumentException("Unknown speech/text field.")
        : project ? field : LegacyCompilerRules.Metadata.Single(row => row.Source == field).Target;
    internal static bool Verified(NativeField field, string key, bool project) => field.BitMask == 0 && (key == "SpeechFile" ? field.Kind == NativeValueKind.WesternAnsi && field.Size == 32
        : key.StartsWith("TextIntroCreatureID", StringComparison.Ordinal) || key == "TextTableID" && !project ? field.Kind == NativeValueKind.Byte && field.Size == 1 : field.Kind == NativeValueKind.UInt16 && field.Size == 2);
}
public sealed class MapSpeechTextDraft
{
    internal MapSpeechTextDraft(NativeMapDocument owner, long revision, byte[] fingerprint, NativeRecord? record, IReadOnlyDictionary<string, string> baseline,
        IReadOnlyDictionary<string, string> values, IReadOnlyList<string> banks, int maximum)
    { Owner = owner; Revision = revision; Fingerprint = fingerprint; Record = record; Baseline = baseline; Values = values; Banks = banks; MentorMaximum = maximum; }
    internal NativeMapDocument Owner { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    public NativeRecord? Record { get; }
    internal IReadOnlyDictionary<string, string> Baseline { get; }
    public IReadOnlyDictionary<string, string> Values { get; }
    public IReadOnlyList<string> Banks { get; }
    public int MentorMaximum { get; }
    public bool Editable => Values.Count != 0;
    public bool Partial => Values.Count != MapSpeechText.Fields.Count;
    public bool Changed => Values.Any(row => row.Value != Baseline[row.Key]);
    internal MapSpeechTextDraft With(Dictionary<string, string> values) => new(Owner, Revision, Fingerprint, Record, Baseline, new ReadOnlyDictionary<string, string>(values), Banks, MentorMaximum);
}
public sealed partial class NativeMapDocument
{
    public MapSpeechTextDraft CreateMapSpeechTextDraft(IEnumerable<string>? availableBanks = null, int mentorMaximum = MapSpeechText.DefaultMentorMaximum)
    {
        if (mentorMaximum is < 0 or > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(mentorMaximum));
        var banks = new[] { "" }.Concat(availableBanks ?? []).Distinct(StringComparer.Ordinal).Take(4097).ToArray();
        if (banks.Length > 4096) throw new ArgumentException("Too many speech bank choices.", nameof(availableBanks));
        var candidates = Records().Where(record => IsLegacyProject ? record.File.Signature == LegacyInfo : record.File == MainFile && record.Category == "Map").ToArray();
        var record = candidates.Length == 1 ? candidates[0] : null; var values = new Dictionary<string, string>();
        if (record is not null && (IsLegacyProject ? record.File.UInt32(4) is >= 1 and <= 7 && record.Category.StartsWith("Project.MAPPROPERTIES", StringComparison.Ordinal) : record.File.UInt32(4) == 4))
            foreach (var key in MapSpeechText.Fields)
            {
                var field = record.Fields.SingleOrDefault(field => field.Name == MapSpeechText.StorageField(IsLegacyProject, key));
                if (field is not null && MapSpeechText.Verified(field, key, IsLegacyProject)) values.Add(key, record.Read(field));
            }
        var view = new ReadOnlyDictionary<string, string>(values);
        return new(this, EditRevision, UnusedScriptFingerprint(), record, view, view, Array.AsReadOnly(banks), mentorMaximum);
    }
    private NativeRecord VerifyMapSpeechText(MapSpeechTextDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!ReferenceEquals(draft.Owner, this) || draft.Revision != EditRevision || !UnusedScriptFingerprint().AsSpan().SequenceEqual(draft.Fingerprint))
            throw new InvalidOperationException("Refresh the speech/text draft after document changes.");
        var current = CreateMapSpeechTextDraft(draft.Banks, draft.MentorMaximum);
        if (!draft.Editable || !current.Editable || current.Record is null || draft.Record?.File != current.Record.File
            || draft.Record.Offset != current.Record.Offset || draft.Record.Length != current.Record.Length
            || !draft.Baseline.OrderBy(row => row.Key).SequenceEqual(current.Values.OrderBy(row => row.Key)))
            throw new InvalidOperationException("A current verified speech/text layout is required.");
        return current.Record;
    }
    public MapSpeechTextDraft UpdateMapSpeechText(MapSpeechTextDraft draft, IEnumerable<MapSpeechTextInput> inputs)
    {
        var record = VerifyMapSpeechText(draft); var requested = inputs.ToArray();
        if (!requested.Select(row => row.Field).Order().SequenceEqual(draft.Values.Keys.Order())) throw new ArgumentException("Provide each available speech/text field exactly once.");
        var values = new Dictionary<string, string>();
        foreach (var input in requested)
        {
            var value = input.Field == "SpeechFile" ? input.Value : ushort.Parse(input.Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            // Source Save updates TextTableID only for a nonzero selector. None is
            // a preview/no-new-table choice, not permission to erase imported IDs.
            if (input.Field == "TextTableID" && value == "0") value = draft.Baseline[input.Field];
            if (value != draft.Baseline[input.Field])
            {
                if (input.Field == "SpeechFile")
                {
                    if (!draft.Banks.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("Choose an enumerated bank; unknown imported names may only be retained.");
                    if (value.Length != 0) { if (!value.StartsWith("SPEECH_", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Choose an original speech bank."); MapTextCatalogues.SpeechTextFile(value); }
                }
                else if (input.Field == "TextTableID") { if (!MapTextCatalogues.Tables.Any(row => row.Identifier.ToString(CultureInfo.InvariantCulture) == value)) throw new ArgumentException("Choose an original text table."); }
                else if (input.Field.StartsWith("TextIntroCreatureID", StringComparison.Ordinal)) { if (!MapTextCatalogues.IntroCreatures.Any(row => row.Value == value)) throw new ArgumentException("Choose an original ordinary, elite or hero creature."); }
                else if (int.Parse(value, CultureInfo.InvariantCulture) > (MapSpeechText.SpeechIds.Contains(input.Field) ? draft.MentorMaximum : 9999)) throw new ArgumentOutOfRangeException(nameof(inputs), "Use the original ID range.");
                record.Fields.Single(field => field.Name == MapSpeechText.StorageField(IsLegacyProject, input.Field)).Encode(value);
            }
            values.Add(input.Field, value);
        }
        return draft.With(values);
    }
    public void ApplyMapSpeechText(MapSpeechTextDraft draft)
    {
        var record = VerifyMapSpeechText(draft); if (!draft.Changed) return;
        Edit("Change map speech and text", () => {
            foreach (var row in draft.Values.Where(row => row.Value != draft.Baseline[row.Key]))
                SetField(record, record.Fields.Single(field => field.Name == MapSpeechText.StorageField(IsLegacyProject, row.Key)), row.Value);
        });
    }
}
