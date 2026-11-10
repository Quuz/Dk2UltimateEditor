using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record ScenarioGateChoice(string Key, uint? ProjectID, int X, int Y, string Name, string Info)
{
    public uint WorkingID { get; init; }
}

public sealed partial class NativeMapDocument
{
    public IReadOnlyList<ScenarioGateChoice> ScenarioGateChoices()
    {
        var result = new List<ScenarioGateChoice>();
        foreach (var region in MapRegions().Where(region => region.Kind == MapRegionKind.Gate))
        {
            var record = region.Record; uint? id = null;
            if (IsLegacyProject)
            {
                if (EntityReferences.Identity(record) is not { } identity
                    || !uint.TryParse(identity.Record.Read(identity.Field), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) continue;
                id = value;
            }
            else
            {
                // GAMEGATE.bGateID is compiler kind 0/1, never a project ID.
                // Keeper hearts (type14), future records and unverified layouts
                // must not masquerade as source MapGates.
                if (!NativeSourceGate(record)) continue;
            }
            var x = region.Bounds.X; var y = region.Bounds.Y;
            if (x is < 0 or >= 65535 || y is < 0 or >= 65535) continue;
            string Text(string name) => record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? record.Read(field) : "";
            result.Add(new(record.File.ExportPath + ":" + record.Offset.ToString(CultureInfo.InvariantCulture), id, x, y, Text("name"), Text("info")) { WorkingID = id ?? (uint)NativeGateID(record) });
        }
        return result.AsReadOnly();
    }

    public string? SelectedScenarioGate(ScenarioParameterDraft draft)
    {
        VerifyScenarioParameterDraft(draft);
        if (draft.Type != "ACTION_DESTROYGATE") throw new ArgumentException("Select the gate action.");
        var choices = ScenarioGateChoices();
        if (!IsLegacyProject)
        {
            if (draft.NativeGateKey is { } chosen) return choices.Any(choice => choice.Key == chosen) ? chosen : null;
            if (draft.Values["NativeArgument5"] == draft.Baseline["NativeArgument5"] && draft.Values["NativeArgument7"] == draft.Baseline["NativeArgument7"] && draft.NativeGateBaselineKey is null) return null;
        }
        var matches = IsLegacyProject
            ? choices.Where(choice => unchecked((uint)int.Parse(draft.Values["param5"], CultureInfo.InvariantCulture)) == choice.ProjectID).ToArray()
            : choices.Where(choice => draft.Values["NativeArgument5"] == (choice.X + 1).ToString(CultureInfo.InvariantCulture)
                && draft.Values["NativeArgument7"] == (choice.Y + 1).ToString(CultureInfo.InvariantCulture)).ToArray();
        // The original native reader selects the first physical exact match.
        // Project IDs still require a unique owner.
        return IsLegacyProject ? matches.Length == 1 ? matches[0].Key : null : matches.FirstOrDefault()?.Key;
    }

    public ScenarioParameterDraft UpdateScenarioGate(ScenarioParameterDraft draft, string key)
    {
        VerifyScenarioParameterDraft(draft);
        if (draft.Type != "ACTION_DESTROYGATE") throw new ArgumentException("Select the gate action.");
        var choice = ScenarioGateChoices().SingleOrDefault(item => item.Key == key)
            ?? throw new ArgumentException("Choose one current source gate.");
        if (IsLegacyProject)
        {
            if (ScenarioGateChoices().Count(item => item.ProjectID == choice.ProjectID) != 1)
                throw new InvalidOperationException("Duplicate project gate identities are ambiguous.");
            return UpdateScenarioParameterDraft(draft, [new("param5", unchecked((int)choice.ProjectID!.Value).ToString(CultureInfo.InvariantCulture))]);
        }
        return UpdateScenarioParameterDraft(draft, [
            new("NativeArgument5", (choice.X + 1).ToString(CultureInfo.InvariantCulture)),
            new("NativeArgument7", (choice.Y + 1).ToString(CultureInfo.InvariantCulture))]).WithGate(choice.Key);
    }

    public ScenarioParameterDraft UpdateScenarioSpatialPoint(ScenarioParameterDraft draft, TilePoint point)
    {
        VerifyScenarioParameterDraft(draft);
        if (draft.Type is not ("ACTION_CHANGESQUARE" or "ACTION_CHANGEOWNER"))
            throw new ArgumentException("Select a cell or owner action.");
        if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height || point.X > 999 || point.Y > 999)
            throw new ArgumentOutOfRangeException(nameof(point), "Choose an original-range point on this map.");
        string Field(int number) => draft.Fields.Single(field => field.Name == "param" + number || field.Name == "Param" + number).Name;
        return UpdateScenarioControlPage(draft, [new(Field(5), (point.X + 1).ToString(CultureInfo.InvariantCulture)),
            new(Field(7), (point.Y + 1).ToString(CultureInfo.InvariantCulture))]);
    }

    public ScenarioParameterDraft UpdateScenarioSpatialArea(ScenarioParameterDraft draft, uint id)
    {
        VerifyScenarioParameterDraft(draft);
        var matches = MapRegions().Where(region => region.Kind == MapRegionKind.Area && EntityReferences.Identity(region.Record) is { } identity
            && uint.TryParse(identity.Record.Read(identity.Field), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value == id).ToArray();
        if (matches.Length != 1) throw new ArgumentException("Choose one current unambiguous area.");
        return UpdateScenarioSpatialPoint(draft, new(matches[0].Bounds.X, matches[0].Bounds.Y));
    }
}
