using System.Windows;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private readonly EditorScenarioDeletionPolicy scenarioDeletionPolicy;
    private RegionRemovalWindow? scenarioDeletionWindow;
    private long scenarioDeletionContext;
    private bool scenarioDeletionPending;
    public Action<RegionRemovalWindow>? ScenarioDeletionPresenter { get; set; }

    private void CloseScenarioDeletionWindow()
    {
        scenarioDeletionContext++;
        var window=scenarioDeletionWindow;scenarioDeletionWindow=null;window?.Close();
    }
    private static bool SameDeletionEdge(ScriptEdge? first,ScriptEdge? second)
        => first is null?second is null:second is not null && first.Scope==second.Scope && first.Target==second.Target
            && SameRemovalRecord(first.Owner,second.Owner) && first.Field==second.Field;
    private bool ScenarioDeletionSelectionCurrent(ScriptNode original)
        => ScriptTree.SelectedItem is ScriptNode current && current.EventID==original.EventID
            && current.ScopeIdentity==original.ScopeIdentity && current.ProjectScopeIdentity==original.ProjectScopeIdentity
            && SameDeletionEdge(current.Incoming,original.Incoming) && SameDeletionEdge(current.ScopeRoot,original.ScopeRoot);
    private void PresentScenarioDeletion(EditorScenarioDeletionRequest request,ScriptNode node)
    {
        if(scenarioDeletionPending)throw new InvalidOperationException("Finish the current scenario deletion request.");
        var owner=document;var context=scenarioDeletionContext;scenarioDeletionPending=true;
        bool Current()=>ReferenceEquals(document,owner) && context==scenarioDeletionContext && WorkspaceTabs.SelectedIndex==2 && ScenarioDeletionSelectionCurrent(node);
        try
        {
            while(!request.IsClosed)
            {
                if(!Current()){request.Dispose();return;}
                if(!request.RequiresPrompt){request.Accept();continue;}
                using var window=new RegionRemovalWindow(request,Current){Owner=Window.GetWindow(this)};
                scenarioDeletionWindow=window;
                try{if(ScenarioDeletionPresenter is { } presenter)presenter(window);else window.ShowDialog();}
                finally{if(ReferenceEquals(scenarioDeletionWindow,window))scenarioDeletionWindow=null;}
                if(window.Failure is { } error)
                {
                    if(!request.IsAccepted)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                    DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this),LocalizationService.Get("RegionRemoval.CommittedFailure"),LocalizationService.Get("ScenarioDelete.Title"),MessageBoxButton.OK,MessageBoxImage.Warning);
                }
            }
        }
        finally{scenarioDeletionPending=false;}
    }
    private void ScenarioDeletionReset_Click(object sender,RoutedEventArgs args)
        => RecordOperation(()=>{CloseScenarioDeletionWindow();scenarioDeletionPolicy.ResetSuppression();return null;});
}
