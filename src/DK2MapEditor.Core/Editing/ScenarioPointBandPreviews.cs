namespace DK2MapEditor.Core.Editing;

public sealed record ScenarioPointBandPreview(NativeRecord Record, string Name, ScenarioBandPreview Preview);
public sealed record ScenarioPointBandRow(ScenarioPointBandPreview Band, ScenarioBandMemberPreview? Member, ScenarioPointOwnerChoice? Choice)
{
    public bool IsHeader => Member is null;
    public bool CanSelect => Choice?.CanSelect == true;
}

/// <summary>Physical source group/member presentation. Choices remain the session's authority.</summary>
public static class ScenarioPointBandRows
{
    public static IReadOnlyList<ScenarioPointBandRow> Build(uint scope, IReadOnlyList<ScenarioPointBandPreview> bands, IReadOnlyList<ScenarioPointOwnerChoice> choices)
    {
        if (scope is not (8 or 16)) throw new ArgumentException("Only hero-band owner pages have band groups.", nameof(scope));
        static (uint Scope,NativeFile File,int Offset,int Length,int Member) Key(ScriptOwner owner)
        {
            var record = owner.BandRecord ?? owner.Record;
            return (owner.Scope,record.File,record.Offset,record.Length,owner.MemberIndex);
        }
        var byPhysicalOwner = choices.ToLookup(choice => Key(choice.Owner));
        ScenarioPointOwnerChoice? Choice(uint kind,NativeRecord band,int index) =>
            byPhysicalOwner[(kind,band.File,band.Offset,band.Length,index)].SingleOrDefault();
        var result = new List<ScenarioPointBandRow>();
        foreach (var band in bands)
        {
            var header = scope == 8 ? Choice(8,band.Record,-1) : null;
            result.Add(new(band, null, header));
            foreach (var member in band.Preview.Members)
            {
                var choice = scope == 16 ? Choice(16,band.Record,member.Index) : null;
                result.Add(new(band, member, choice));
            }
        }
        return result.AsReadOnly();
    }
}

public sealed partial class NativeMapDocument
{
    /// <summary>Immutable physical-band snapshot for one private Action Point wizard.</summary>
    public IReadOnlyList<ScenarioPointBandPreview> ReadScenarioPointBandPreviews(EditorScenarioPointSession session)
    {
        RequirePointSession(session);
        var catalogue = new[] { "BadCreatures", "BadElites", "Heros" }.SelectMany(LegacyCompilerRules.Elements).ToArray();
        var result = new List<ScenarioPointBandPreview>();
        foreach (var band in Records().Where(IsBand))
        {
            var identity = EntityReferences.Identity(band);
            if (identity is null) continue;
            var name = band.Fields.FirstOrDefault(field => field.Name == "name");
            result.Add(new(band, name is null ? "" : band.Read(name), ReadScenarioBandPreview(band, band.Read(identity.Field), catalogue)));
        }
        return result.AsReadOnly();
    }
}
