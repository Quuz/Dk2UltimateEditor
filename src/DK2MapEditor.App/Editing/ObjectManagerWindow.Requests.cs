using System.Windows;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class ObjectManagerWindow
{
    private readonly EditorThingRequestPolicy requestPolicy;
    private RegionRemovalWindow? confirmationWindow;
    private bool requestPending;
    public Action<RegionRemovalWindow>? ConfirmationPresenter { get; set; }
    private NativeRecord[] CurrentManagerSelection()=>rows.Where(row=>table.SelectedItems.Contains(row)).Select(row=>row.Entry.Record).ToArray();
    private bool ManagerSelectionCurrent(IReadOnlyList<NativeRecord> selected)
    {
        var current=CurrentManagerSelection();
        var keys=selected.Select(row=>(row.File,row.Offset,row.Length,row.Category)).ToHashSet();
        return current.Length==selected.Count && current.All(row=>keys.Contains((row.File,row.Offset,row.Length,row.Category)));
    }
    private bool PresentThingRequest(EditorThingRequest request,IReadOnlyList<NativeRecord> selected)
    {
        if(requestPending)throw new InvalidOperationException("Finish the current object confirmation first.");
        var context=selectionRevision;requestPending=true;UpdateApply();
        try
        {
            while(!request.IsClosed)
            {
                if(closed || context!=selectionRevision || !canApply() || !ManagerSelectionCurrent(selected)){request.Dispose();return false;}
                if(!request.RequiresPrompt){request.Accept();continue;}
                using var window=new RegionRemovalWindow(request,()=>!closed && context==selectionRevision && canApply() && ManagerSelectionCurrent(selected)){Owner=this};
                confirmationWindow=window;
                try{if(ConfirmationPresenter is { } presenter)presenter(window);else window.ShowDialog();}
                finally{if(ReferenceEquals(confirmationWindow,window))confirmationWindow=null;}
                if(window.Failure is { } error)
                {
                    if(request.Command==EditorThingCommand.Modify || !request.IsAccepted)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                    DK2MapEditor.App.Editing.OriginalMessageBox.Show(this,LocalizationService.Get("RegionRemoval.CommittedFailure"),LocalizationService.Get("ThingRequest.Title"),MessageBoxButton.OK,MessageBoxImage.Warning);
                }
            }
            return request.IsAccepted && (request.Command==EditorThingCommand.Remove || !closed && context==selectionRevision && canApply() && ManagerSelectionCurrent(selected));
        }
        finally{requestPending=false;UpdateApply();}
    }
    private static bool IsThingRequestFailure(Exception error)=>error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException
        or System.IO.IOException or UnauthorizedAccessException or FormatException or OverflowException;
}
