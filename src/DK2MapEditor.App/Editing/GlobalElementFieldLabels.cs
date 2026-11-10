using DK2MapEditor.App.Localization;

namespace DK2MapEditor.App.Editing;

/// <summary>Source page context which would be lost by a shared raw field name.</summary>
internal static class GlobalElementFieldLabels
{
    internal static string? OverrideKey(string family, string field) => DefinitionCaptionRules.StaticKey(family,field);

    internal static string Label(string family, string field) =>
        LocalizationService.Get(OverrideKey(family, field) ?? "Field." + field);

    internal static string TriggerParameterLabel(string type) => LocalizationService.Get(type switch
    {
        "1" => "TrapProperties.TriggerAreaOfEffect",
        "2" => "TrapProperties.TriggerRequiredPressure",
        _ => "Field.TriggerData"
    });
}
