using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void FinishCompilerUiTask(MainWindow shell)
    {
        var deadline=DateTime.UtcNow.AddSeconds(30);
        while(shell.IsCompilerTaskActive&&DateTime.UtcNow<deadline)
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        Expect(!shell.IsCompilerTaskActive,"Compiler command did not finish.");
    }
    // Compiled only. Execution needs the user's manual UI workflow.
    private static void CheckCompilerTask(bool native,string language)
    {
        var document=NativeMapDocument.CreateNewMap(new("Task","Task",18,16,NativeFormat:native));
        var workspace=new EditorWorkspace{Document=document};var canvas=(MapCanvas)workspace.FindName("Canvas");
        var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=document.EditRevision;
        var current=true;var launches=0;
        var rows=Enumerable.Range(0,25).Select(i=>new MapDiagnostic(DiagnosticSeverity.Warning,"Repeated","Message "+i,Region:new(2,3,2,2))).ToArray();
        var window=new CompilerTaskWindow(ScenarioCheckPurpose.Run,"Task",()=>current,workspace.NavigateToDiagnostic);
        window.SetPhase("CompilerTask.Test");window.SetDiagnostics(rows);
        Expect(!window.PerformAction()&&!((Button)window.FindName("CompilerAction")).IsEnabled,"Active task accepted a close/run action.");
        window.Close();Expect(window.IsWorking,"Closing an active task pretended to finish it.");
        window.Finish(true,()=>current,()=>{launches++;return true;});
        var list=(DataGrid)window.FindName("CompilerRows");Expect(list.Items.Count==25&&window.Diagnostics.Count==25,"Task truncated/deduplicated diagnostics.");
        list.SelectedIndex=22;Expect(canvas.HighlightedRegion==new TileRect(2,3,2,2),"Completed task did not navigate to selected message.");
        LocalizationService.SetLanguage(language=="en"?"ru":"en");
        Expect(list.SelectedIndex==22&&((Button)window.FindName("CompilerAction")).Content as string==LocalizationService.Get("CompilerTask.Run"),"Task live translation lost selection/run state.");
        current=false;window.RefreshCurrent();window.PerformAction();Expect(launches==0,"Stale task launched a changed map.");
        current=true;var failed=new CompilerTaskWindow(ScenarioCheckPurpose.Run,"Task",()=>current,workspace.NavigateToDiagnostic);
        failed.SetDiagnostics(rows);failed.Finish(true,()=>current,()=>{launches++;throw new InvalidOperationException("Injected launch failure");});
        Expect(!failed.PerformAction()&&!failed.Succeeded&&launches==1&&((TextBox)failed.FindName("CompilerLog")).Text.Contains("Injected launch failure"),"Launch failure was lost or reported successful.");
        failed.PerformAction();Expect(launches==1,"Failed launch could be repeated from retained callback.");
        var success=new CompilerTaskWindow(ScenarioCheckPurpose.Run,"Task",()=>current,workspace.NavigateToDiagnostic);
        success.Finish(true,()=>current,()=>{launches++;return true;});Expect(success.PerformAction()&&launches==2,"Ready task did not launch exactly once.");success.PerformAction();
        Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditRevision==revision,"Task view modified map data/history.");
        LocalizationService.SetLanguage(language);workspace.Document=null;
    }
}
