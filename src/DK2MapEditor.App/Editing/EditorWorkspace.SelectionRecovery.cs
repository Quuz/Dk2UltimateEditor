using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private bool requestingSelectionRecovery;
    private SelectionRecoveryConfirmationWindow? selectionRecoveryWindow;
    /// <summary>Actual import-command confirmation. Null uses the modal EN/RU
    /// window; tests can provide explicit accepted/declined recovery replies.</summary>
    public Func<SelectionRecoveryRequest,bool>? SelectionRecoveryPrompt { get; set; }
    public SelectionRecoveryConfirmationWindow CreateSelectionRecoveryConfirmation(SelectionRecoveryRequest request)=>new(request);
    private bool ShowSelectionRecoveryConfirmation(SelectionRecoveryRequest request)
    {
        var window=CreateSelectionRecoveryConfirmation(request);selectionRecoveryWindow=window;window.Owner=Window.GetWindow(this);
        try{window.ShowDialog();return window.Accepted;}
        finally{if(ReferenceEquals(selectionRecoveryWindow,window))selectionRecoveryWindow=null;}
    }
    private void CloseSelectionRecoveryConfirmation()
    {selectionRecoveryWindow?.Close();selectionRecoveryWindow=null;}
    private bool RequestSelectionRecovery(SelectionRecoveryRequest request)
    {
        if(copyOwnerClosed||requestingCopyReplacement||requestingSelectionRecovery)return false;
        requestingSelectionRecovery=true;
        try{return (SelectionRecoveryPrompt??ShowSelectionRecoveryConfirmation)(request);}
        finally{requestingSelectionRecovery=false;}
    }
}
