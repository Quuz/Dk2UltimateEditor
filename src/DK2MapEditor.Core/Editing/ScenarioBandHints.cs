using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public static class ScenarioBandHints
{
    public static bool Supports(string type) => type is
        "TRIGGER_DEATHSINBAND" or "TRIGGER_JAILEDINBAND" or "TRIGGER_STUNNEDINBAND";
}

public sealed partial class NativeMapDocument
{
    /// <summary>Read-only counterpart of the three source LVN_ITEMCHANGED
    /// population labels. Stored band IDs are used directly; the source list's
    /// transient ID+1 is removed before storage. Count stops at the first
    /// inactive slot, without granting authority to repair imported tails.</summary>
    public int? ReadScenarioBandPopulation(ScenarioParameterDraft draft, string selectedBand)
    {
        VerifyScenarioParameterDraft(draft);
        if (!ScenarioBandHints.Supports(draft.Type))
            throw new ArgumentException("This scenario page has no band population label.", nameof(draft));
        if (!uint.TryParse(selectedBand, NumberStyles.None, CultureInfo.InvariantCulture, out var id)) return null;
        var matches = Records().Where(IsBand).Where(record =>
            EntityReferences.Identity(record) is { Kind: EntityKind.Band } identity
            && uint.TryParse(record.Read(identity.Field), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            && value == id).ToArray();
        if (matches.Length != 1) return null;
        try
        {
            var layout = GetBandLayout(matches[0]);
            var count = ReadBandMembers(layout).TakeWhile(member => member.Active).Count();
            if (layout.Project && count == 16 && layout.Read(16, "isvalid") != "0") return null;
            return count;
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException or InvalidOperationException)
        { return null; }
    }
}
