using System.Windows;
using System.Windows.Input;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private readonly EditorThingRequestPolicy cursorRemovalPolicy;
    private RegionRemovalWindow? cursorRemovalWindow;
    private bool cursorRemovalPending;
    private long cursorRemovalContext;
    public Action<RegionRemovalWindow>? UnderCursorRemovalPresenter {get;set;}
    private void CloseUnderCursorRemoval(){cursorRemovalContext++;cursorRemovalWindow?.Close();cursorRemovalWindow=null;}
    public bool RequestUnderCursorRemovalShortcut(Key key,ModifierKeys modifiers)
        =>key==Key.Delete && Canvas.IsMouseOver && RequestUnderCursorRemovalAt(key,modifiers,Mouse.GetPosition(Canvas));
    internal bool RequestUnderCursorRemovalAt(Key key,ModifierKeys modifiers,Point pointer)
    {
        if(key!=Key.Delete || (modifiers&~(ModifierKeys.Shift|ModifierKeys.Control))!=0 || document is null || cursorRemovalPending
            || NativeMapDocument.UnderCursorPreference(modifiers.HasFlag(ModifierKeys.Shift),modifiers.HasFlag(ModifierKeys.Control)) is not { } preferred
            || Canvas.RemovalTile(pointer) is not { } position)return false;
        if(!CommitFields())return true;
        var owner=document;var context=cursorRemovalContext;cursorRemovalPending=true;
        try
        {
            var plan=owner.PlanUnderCursorRemoval(position,preferred);using var request=cursorRemovalPolicy.BeginUnderCursor(plan);
            if(request is null)return true;
            while(!request.IsClosed)
            {
                if(!ReferenceEquals(document,owner) || context!=cursorRemovalContext || !Canvas.CanRemoveUnderCursor){request.Dispose();return true;}
                if(!request.RequiresPrompt){request.Accept();continue;}
                // The chosen tile is frozen. Clicking the centered reply moves
                // the real pointer and must never retarget the deletion.
                using var window=new RegionRemovalWindow(request,()=>ReferenceEquals(document,owner) && context==cursorRemovalContext && Canvas.CanRemoveUnderCursor){Owner=Window.GetWindow(this)};
                cursorRemovalWindow=window;
                try{if(UnderCursorRemovalPresenter is { } presenter)presenter(window);else window.ShowDialog();}
                finally{if(ReferenceEquals(cursorRemovalWindow,window))cursorRemovalWindow=null;}
                if(window.Failure is { } error)
                {
                    if(!request.IsAccepted)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                    DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this),LocalizationService.Get("RegionRemoval.CommittedFailure"),LocalizationService.Get("ThingRequest.Title"),MessageBoxButton.OK,MessageBoxImage.Warning);
                }
            }
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or OverflowException or FormatException)
        {objectPaintingStatusKey="Batch.Failed";PlacementStatus.ToolTip=error.Message;RefreshPlacementStatus();}
        finally{cursorRemovalPending=false;}
        return true;
    }
}
