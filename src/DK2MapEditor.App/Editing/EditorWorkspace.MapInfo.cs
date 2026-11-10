using System.Globalization;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private MapInfoGoldSnapshot? mapInfoGoldSnapshot;
    private bool mapInfoGoldFailed;
    private void InvalidateMapInfoGold(){mapInfoGoldSnapshot=null;mapInfoGoldFailed=false;}
    private void RefreshMapInfoGold()
    {
        if(GoldInfoPanel is null)return;
        GoldInfoPanel.ToolTip=null;
        if(document is not null&&!mapInfoGoldFailed&&mapInfoGoldSnapshot is null)
        {
            try{mapInfoGoldSnapshot=document.CaptureMapInfoGold();}
            catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or FormatException or OverflowException)
            {mapInfoGoldFailed=true;GoldInfoPanel.ToolTip=error.Message;}
        }
        string Number(long? value)=>value?.ToString(CultureInfo.CurrentCulture)??LocalizationService.Get("Editor.Statistics.Unknown");
        var unavailable=LocalizationService.Get("Editor.InfoGold.Unavailable");
        GoldInfoLevel.Text=string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("Editor.InfoGold.Level"),document is null?unavailable:Number(mapInfoGoldSnapshot?.Total));
        GoldInfoSelection.Text=string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("Editor.InfoGold.Selection"),document is null||Canvas.Selection is not { } rectangle?unavailable:Number(mapInfoGoldSnapshot?.Within(rectangle)));
    }
}
