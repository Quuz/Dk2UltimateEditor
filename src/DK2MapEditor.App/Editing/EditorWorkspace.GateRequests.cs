using System.Runtime.ExceptionServices;
using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private EditorGateGenerationPolicy? generationRequestPolicy;
    private GateGenerationRequestWindow? generationRequestWindow;
    private bool generationRequestPending;
    private long generationContext;
    private EditorGateGenerationPolicy GenerationRequestPolicy=>generationRequestPolicy??=new(RequestPreferences);
    public Action<GateGenerationRequestWindow>? GateGenerationRequestPresenter {get;set;}
    private void CloseGateGenerationRequest()=>generationRequestWindow?.Dispose();
    private bool PresentGateGenerationRequest(EditorGateGenerationRequest request)
    {
        if(generationRequestPending)throw new InvalidOperationException("Finish the current generation request.");
        generationRequestPending=true;RefreshGateGeneration();
        try
        {
            if(!request.RequiresPrompt){if(request.Capacity is null)request.Confirm();else request.Acknowledge();return request.IsAccepted;}
            using var window=new GateGenerationRequestWindow(request){Owner=Window.GetWindow(this)};generationRequestWindow=window;
            try{if(GateGenerationRequestPresenter is {} presenter)presenter(window);else window.ShowDialog();}
            finally{if(ReferenceEquals(generationRequestWindow,window))generationRequestWindow=null;}
            if(window.Failure is {} error)ExceptionDispatchInfo.Capture(error).Throw();
            return request.IsAccepted;
        }
        finally{generationRequestPending=false;RefreshGateGeneration();}
    }
    private void ShowGenerationCapacity(GateGenerationCapacityException error,NativeMapDocument source,long context)
    {
        using var request=GenerationRequestPolicy.BeginCapacity(error,()=>ReferenceEquals(source,document)&&context==generationContext&&!scenarioNamesOwnerClosed);
        PresentGateGenerationRequest(request);
    }
    private void GeneratorResetRequests_Click(object sender,RoutedEventArgs args)
    {
        try{CloseGateGenerationRequest();GenerationRequestPolicy.ResetSuppression();RefreshGateGeneration();}
        catch(InvalidOperationException error){GeneratorStatusMessage("Editor.GeneratorFailed");GeneratorStatus.ToolTip=error.Message;}
    }
}
