using System.Windows;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App;

public partial class MainWindow
{
    private CompilerTaskWindow? compilerTaskWindow;
    public CompilerTaskWindow? CurrentCompilerTaskWindow=>compilerTaskWindow;
    public bool IsCompilerTaskActive=>scenarioCommandActive;
    private readonly CompilerWindowPlacement compilerPlacement=new();
    private Action? restoreCompilerWindows;
    private Action? restoreCompilerHighlight;
    private bool compilerUsesWindow;
    private static Task<T> CompilerWork<T>(bool background,Func<T> work)=>background?Task.Run(work):Task.FromResult(work());
    private static NativeMapDocument CompilerInput(NativeMapDocument source,bool background)=>background?source.CreateCompilationSnapshot():source;
    private void BeginCompilerTask(ScenarioCheckPurpose purpose,NativeMapDocument target,bool background)
    {
        if(!background)return;
        compilerUsesWindow=true;
        compilerTaskWindow?.CloseForOwner();restoreCompilerHighlight?.Invoke();restoreCompilerHighlight=Editor.PreserveDiagnosticHighlight();
        var window=new CompilerTaskWindow(purpose,EditorWindowCaption.ReadMapName(target)??System.IO.Path.GetFileName(target.MainFile.SourcePath),
            ()=>ScenarioTargetCurrent(target),region=>ScenarioTargetCurrent(target)&&Editor.NavigateToDiagnostic(region));
        compilerTaskWindow=window;window.Owner=this;compilerPlacement.Attach(window);
        window.Closed+=(_,_)=>
        {
            if(ReferenceEquals(compilerTaskWindow,window))
            {compilerTaskWindow=null;restoreCompilerHighlight?.Invoke();restoreCompilerHighlight=null;}
        };
        window.Show();
        // Existing modeless editors must not modify the live source while a
        // background snapshot is being accepted or published. New consent
        // dialogs remain modal and enabled; autosave also checks task-active.
        var disabled=Application.Current.Windows.Cast<Window>().Where(w=>w!=window&&w.IsVisible&&w.IsEnabled).ToArray();
        foreach(var other in disabled)other.IsEnabled=false;
        restoreCompilerWindows=()=>{foreach(var other in disabled)if(other.IsVisible)other.IsEnabled=true;};
    }
    private void CompilerRows(IReadOnlyList<MapDiagnostic> rows)
    {if(compilerUsesWindow)compilerTaskWindow?.SetDiagnostics(rows);}
    private void CompilerPhase(string phase,string? detail=null){if(compilerUsesWindow)compilerTaskWindow?.SetPhase("CompilerTask."+phase,detail);}
    private void EndCompilerTask(NativeMapDocument target,bool succeeded,Func<bool>? launch=null)
    {
        try
        {
            var context=scenarioContextRevision;var revision=target.EditRevision;
            compilerTaskWindow?.Finish(succeeded,()=>context==scenarioContextRevision&&revision==target.EditRevision&&ScenarioTargetCurrent(target),launch);
        }
        finally{restoreCompilerWindows?.Invoke();restoreCompilerWindows=null;}
    }
    private void CloseCompilerTask()
    {
        try{compilerTaskWindow?.CloseForOwner();}
        finally{restoreCompilerWindows?.Invoke();restoreCompilerWindows=null;}
    }
    private void CompilerFailureRows(IReadOnlyList<MapDiagnostic> rows,NativeMapDocument target,Func<bool>? current=null)
    {
        CompilerRows(rows);
        if(!compilerUsesWindow)ShowDocumentReport(DocumentReportPurpose.Compile,rows,false,target,current);
    }
    private void FinishCompilerCommand(NativeMapDocument target,bool started,bool succeeded,Func<bool>? launch=null)
    {
        try{if(started)target.CompilationTaskFinished();}
        finally
        {
            try{if(compilerUsesWindow)EndCompilerTask(target,succeeded,launch);}
            finally{compilerUsesWindow=false;scenarioCommandActive=false;Editor.RefreshActivityStatistics();}
        }
    }
}
