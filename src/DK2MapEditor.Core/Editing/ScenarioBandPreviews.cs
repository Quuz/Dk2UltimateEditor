using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record ScenarioBandMemberPreview(int Index, uint CreatureID, int Level,
    bool IsLeader, uint Objective, uint Area, ValueChoice? Creature, int? ImageResource);
public sealed record ScenarioBandPreview(string ID, IReadOnlyList<ScenarioBandMemberPreview> Members, bool Complete);

public static class ScenarioBandCatalogue
{
    public static int Parameter(string type) => type switch
    {
        "TRIGGER_BANDCREATED" or "TRIGGER_STUNNEDINBAND" or "TRIGGER_JAILEDINBAND" or "TRIGGER_DEATHSINBAND" => 2,
        "ACTION_RELEASEBAND" => 1, _ => 0
    };
}

public sealed partial class NativeMapDocument
{
    /// <summary>Source HerosList rows, in physical band/member order, stopping at
    /// the first inactive slot. Each occurrence remains distinct even with duplicate
    /// band IDs. Returned values are a read-only snapshot; no repair/edit authority.</summary>
    public IReadOnlyList<ScenarioBandPreview> ReadScenarioBandPreviews(ScenarioParameterDraft draft)
    {
        VerifyScenarioParameterDraft(draft);
        if (ScenarioBandCatalogue.Parameter(draft.Type) == 0) throw new ArgumentException("This source page has no hero-band list.", nameof(draft));
        var catalogue = new[] { "BadCreatures", "BadElites", "Heros" }.SelectMany(LegacyCompilerRules.Elements).ToArray();
        var result = new List<ScenarioBandPreview>();
        foreach (var record in Records().Where(IsBand))
        {
            var identity = EntityReferences.Identity(record);
            if (identity is null) continue;
            result.Add(ReadScenarioBandPreview(record, record.Read(identity.Field), catalogue));
        }
        return result.AsReadOnly();
    }

    private ScenarioBandPreview ReadScenarioBandPreview(NativeRecord record, string identity, IReadOnlyList<LegacyCompilerRules.Element> catalogue)
    {
        var members = new List<ScenarioBandMemberPreview>(); var complete = true;
        try
        {
            var layout = GetBandLayout(record);
            var required = layout.Project ? new[] { "isvalid", "id", "level", "options2", "objective", "area" }
                : ["bGlobalCreatureID", "bGlobalLevel", "bGlobalBehavior", "bHeroObjective", "bHeroTargetArea"];
            if (required.Any(name => layout.Field(name) is null)) throw new InvalidDataException("Unverified member fields.");
            uint U(int index, string project, string native) => uint.Parse(layout.Read(index, layout.Project ? project : native), CultureInfo.InvariantCulture);
            for (var index = 0; index < 16; index++)
            {
                var id = U(index, "id", "bGlobalCreatureID"); var behavior = U(index, "options2", "bGlobalBehavior");
                if (layout.Project ? layout.Read(index, "isvalid") == "0" : id == 0 || behavior == 0) break;
                var entry = catalogue.FirstOrDefault(item => item.ID == id);
                members.Add(new(index, id, int.Parse(layout.Read(index, layout.Project ? "level" : "bGlobalLevel"), CultureInfo.InvariantCulture),
                    (behavior & 2) != 0, U(index, "objective", "bHeroObjective"), U(index, "area", "bHeroTargetArea"),
                    entry is null ? null : ValueCatalogues.Named(entry, id.ToString(CultureInfo.InvariantCulture)), entry?.Icon));
            }
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException)
        { complete = false; }
        return new(identity, members.AsReadOnly(), complete);
    }
}
