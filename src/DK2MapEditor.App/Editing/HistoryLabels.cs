using System.Text.RegularExpressions;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public static class HistoryLabels
{
    private static readonly Dictionary<string, string> operations = new(StringComparer.Ordinal)
    {
        ["Add scenario event"] = "History.AddScenarioEvent",
        ["Edit scenario event"] = "History.EditScenarioEvent",
        ["Paint"] = "History.Op.0",
        ["Resize map"] = "History.Op.1",
        ["Flood fill"] = "History.Op.2",
        ["Paste"] = "History.Op.3",
        ["Choose camera path"] = "History.Op.4",
        ["Add band member"] = "History.Op.5",
        ["Remove band member"] = "History.Op.6",
        ["Move band member"] = "History.Op.7",
        ["Change map region bounds"] = "History.Op.8",
        ["Add map region"] = "History.Op.9",
        ["Upgrade historical project"] = "History.Op.10",
        ["Change global definitions"] = "History.Op.11",
        ["Change availability, creature pools/modifiers or alliances"] = "History.Op.12",
        ["Generate hero gates, areas and destruction events"] = "History.Op.13",
        ["Change effect generator options"] = "History.Op.14",
        ["Add map variable override"] = "History.Op.15",
        ["Change global map variables"] = "History.Op.16",
        ["Restore original global map variable defaults"] = "History.Op.17",
        ["Remove native global map variable overrides"] = "History.Op.18",
        ["Duplicate script graph"] = "History.Op.19",
        ["Change custom player AI and room build order"] = "History.Op.20",
        ["Change patrol pause"] = "History.Op.21",
        ["Start patrol with first area"] = "History.PatrolStart",
        ["Change region properties"] = "History.RegionProperties",
        ["Delete selected map regions"] = "History.RegionRemoval",
        ["Delete object under cursor"] = "History.CursorRemoval",
        ["Draw patrol link"] = "History.Op.22",
        ["Append patrol point"] = "History.Op.23",
        ["Insert patrol point"] = "History.Op.24",
        ["Remove patrol point link"] = "History.Op.25",
        ["Remove patrol route links"] = "History.Op.26",
        ["Cut tiles and things"] = "History.Op.27",
        ["Paste tiles, things and scripts"] = "History.Op.28",
        ["Transform tiles and placed things"] = "History.Op.29",
        ["Move scenario branch"] = "History.Op.30",
        ["Delete scenario branch"] = "History.Op.31",
        ["Change player starting camera"] = "History.Op.32",
        ["Change player AI build area"] = "History.Op.33",
        ["Change scenario link"] = "History.Op.34",
        ["Insert scenario event"] = "History.Op.35",
        ["Change player name, resources and control"] = "History.Op.36",
        ["Change player AI type only"] = "History.Op.37",
        ["Apply original AI profile defaults"] = "History.Op.38",
        ["Restore original player defaults"] = "History.Op.39",
        ["Create scenario binding"] = "History.Op.40",
        ["Bind owner scenario"] = "History.Op.41",
        ["Change scenario owner"] = "History.Op.42",
        ["Remove scenario binding"] = "History.Op.43",
        ["Add named record"] = "History.Op.44",
        ["Duplicate project script graph"] = "History.Op.45",
        ["Duplicate project record"] = "History.Op.46",
        ["Delete project record"] = "History.Op.47",
        ["Batch add objects"] = "History.Op.48",
        ["Batch remove objects"] = "History.Op.49",
        ["Batch modify objects"] = "History.Op.50",
        ["Paint object"] = "History.Op.51",
        ["Remove unused scenario conditions and actions"] = "History.Op.52",
        ["Rename scenario variable or timer"] = "History.Op.53",
        ["Edit scenario operands and aliases"] = "History.ScenarioAliasEvent",
        ["Edit scenario parameters"] = "History.ScenarioParameters",
        ["Edit scenario parameters and aliases"] = "History.ScenarioParametersAndAliases",
        ["Change map level options"] = "History.Op.54",
        ["Change map speech and text"] = "History.Op.55",
        ["Change general map settings"] = "History.Op.56",
        ["Change target game version"] = "History.Op.57",
        ["Move scenario point"] = "History.ScenarioPoint.Move",
        ["Delete scenario point"] = "History.ScenarioPoint.Delete",
        ["Save scenario point"] = "History.ScenarioPoint.Save",
        ["Add hero band"] = "History.HeroBand.Add",
        ["Save hero band"] = "History.HeroBand.Save",
        ["Delete hero band"] = "History.HeroBand.Delete",
    };
    public static string Description(string description)
    {
        if (description.StartsWith("Random ", StringComparison.Ordinal)) return LocalizationService.Get("Paint.Random." + description[7..],description);
        if (operations.TryGetValue(description, out var key)) return LocalizationService.Get(key, description);
        if (Enum.TryParse<DrawingTool>(description, out var tool)) return LocalizationService.Get("Tool." + tool, description);
        foreach (var prefix in new[] { "Add ", "Delete ", "Duplicate " })
            if (description.StartsWith(prefix, StringComparison.Ordinal))
            {
                var category = description[prefix.Length..];
                if (category.StartsWith("project ", StringComparison.Ordinal)) category = category[8..];
                var categoryLabel = LocalizationService.Get("Category." + category,
                    LocalizationService.Get("ProjectKind." + category.Replace("Project.", ""), category));
                return LocalizationService.Get("History." + prefix.Trim()) + ": " + categoryLabel;
            }
        if (description.StartsWith("ACTION_", StringComparison.Ordinal) || description.StartsWith("TRIGGER_", StringComparison.Ordinal))
            return LocalizationService.Get("Event." + description, description);
        // Field edits retain the schema name internally; translate only display.
        var plain = FieldNaming.CanonicalName(description);
        var indexes = Regex.Match(plain, @"(?:\[\d+\])+$").Value;
        if (indexes.Length > 0) plain = plain[..^indexes.Length];
        var label = LocalizationService.Get("Field." + plain, description);
        return label == description ? description : label + indexes;
    }
}
