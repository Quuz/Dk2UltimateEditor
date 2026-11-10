using System.Globalization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

internal static class ScenarioBandPreviewRows
{
    internal static string Label(ScenarioBandMemberPreview member, string language, Func<string, string> text)
    {
        var name = member.Creature?.Name(language)
            ?? text("AliasEvent.Unknown") + " " + member.CreatureID.ToString(CultureInfo.InvariantCulture);
        if (!member.IsLeader)
            return string.Format(CultureInfo.CurrentCulture, text("ScenarioPage.BandMember"), name, member.Level);
        var objective = member.Objective is 11 or 12 or 13 or 14 or 17 or 18 or 19 or 22 or 23 or 27
            ? text("Batch.Objective." + member.Objective.ToString(CultureInfo.InvariantCulture))
            : text("AliasEvent.Unknown") + " " + member.Objective.ToString(CultureInfo.InvariantCulture);
        if (member.Objective == 23) objective += " " + member.Area.ToString(CultureInfo.InvariantCulture);
        return string.Format(CultureInfo.CurrentCulture, text("ScenarioPage.BandLeader"), name, member.Level, objective);
    }
}
