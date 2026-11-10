using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source attribute captions and units without changing shared raw field labels.</summary>
internal static class CreatureDefinitionFieldLabels
{
    internal static string? OverrideKey(string family, string field) => DefinitionCaptionRules.StaticKey(family,field);

    internal static string Label(string family, string field) =>
        LocalizationService.Get(OverrideKey(family, field) ?? "Field." + field);
}
