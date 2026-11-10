using System.Globalization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    public void RefreshActivityStatistics() => RefreshStatistics();

    private static string ActivityDate(ActivityLocalTime? date)
    {
        if (date is null) return StatText("Unknown");
        if (date.Value == default) return "—";
        return date.Value.CalendarValue()?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? StatText("Unknown");
    }

    private IEnumerable<StatisticView> ActivityStatisticsRows()
    {
        var stats = document?.ReadActivityStatistics();
        var elapsed = document?.ActivityElapsedMilliseconds;
        yield return Stat("ActivityDays", StatNumber(elapsed / 86400000u));
        yield return Stat("ActivityHours", StatNumber(elapsed / 3600000u % 24u));
        yield return Stat("ActivityMinutes", StatNumber(elapsed / 60000u % 60u));
        yield return Stat("ActivitySeconds", StatNumber(elapsed / 1000u % 60u));
        yield return Stat("LeftMapClicks", StatNumber(stats?.LMBClicks));
        yield return Stat("RightMapClicks", StatNumber(stats?.RMBClicks));
        yield return Stat("UndoUses", StatNumber(stats?.UsageOfUndo));
        yield return Stat("ObjectPaintUses", StatNumber(stats?.UsageOfPaintThings));
        yield return Stat("FillUses", StatNumber(stats?.UsageOfPaintFill));
        yield return Stat("PaintUses", StatNumber(stats?.UsageOfPaint));
        yield return Stat("RandomPaintUses", StatNumber(stats?.UsageOfPaintRnd));
        yield return Stat("ProjectSaveCount", StatNumber(stats?.SavedTimes));
        yield return Stat("LastProjectSave", ActivityDate(stats?.LastSaved));
        yield return Stat("MapCreationTime", ActivityDate(stats?.CreatedTime));
        yield return Stat("LastCompilationTime", ActivityDate(stats?.CompileTime));
        if (document is { ActivityStatisticsProblem: not ActivityStatisticsProblem.None })
            yield return Stat("ActivityStatus", StatText(document.ActivityStatisticsProblem == ActivityStatisticsProblem.UnsupportedVersion
                ? "ActivityUnsupportedVersion" : "ActivityTruncatedSection"));
    }
}
