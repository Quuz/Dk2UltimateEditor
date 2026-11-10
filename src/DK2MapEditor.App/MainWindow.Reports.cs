using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using System.Windows;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App;

public partial class MainWindow
{
    private DocumentReportWindow? documentReport;
    public Action<DocumentReportWindow>? DocumentReportPresenter {get;set;}
    public EditorSaveReportFlags SaveReportFlags {get;set;}=EditorSaveReportFlags.Default;
    public IReadOnlyList<MapDiagnostic> LastDocumentReport {get;private set;}=[];
    public IReadOnlyList<MapDiagnostic> LastSaveCompatibilityReport {get;private set;}=[];
    private bool ShowDocumentReport(DocumentReportPurpose purpose,IReadOnlyList<MapDiagnostic> rows,bool offerContinue,NativeMapDocument? target=null,Func<bool>? additionalCurrent=null)
    {
        if(documentReport is not null||saveOwnerClosed)return false;
        LastDocumentReport=Array.AsReadOnly(rows.ToArray());
        // Existing non-modal notice presenters remain valid for close-only
        // reports. They never approve a continuation, and receive all rows.
        if(DocumentReportPresenter is null&&!offerContinue)
        {
            var text=string.Join("\n",rows.Select(row=>row.Code+": "+LocalizationService.Get("Diagnostic."+row.Code,row.Message)));
            var title=LocalizationService.Get("Report."+purpose);
            var icon=rows.Any(row=>row.Severity==DiagnosticSeverity.Error)?MessageBoxImage.Error:MessageBoxImage.Information;
            if(purpose==DocumentReportPurpose.Load&&SaveNoticePresenter is {} loadNotice)
            {loadNotice(new(text,title,icon));return false;}
            if(purpose!=DocumentReportPurpose.Load&&ScenarioNoticePresenter is {} notice)
            {notice(new(text,title,icon));return false;}
        }
        var context=saveContextRevision;var revision=target?.EditRevision;
        bool Current()=>!saveOwnerClosed&&context==saveContextRevision&&(target is null||ReferenceEquals(document,target)&&target.EditRevision==revision)
            &&(additionalCurrent?.Invoke()??true);
        var restore=target is not null&&ReferenceEquals(Editor.Document,target)?Editor.PreserveDiagnosticHighlight():null;
        var window=new DocumentReportWindow(purpose,rows,offerContinue,Current,target is null?null:region=>Current()&&Editor.NavigateToDiagnostic(region));
        documentReport=window;
        try
        {
            if(DocumentReportPresenter is {} present)present(window);else{if(IsVisible)window.Owner=this;window.ShowDialog();}
            return window.Accepted&&Current();
        }
        finally{window.Cancel();if(ReferenceEquals(documentReport,window))documentReport=null;restore?.Invoke();}
    }
    private bool ConfirmCompileDiagnostics(IReadOnlyList<MapDiagnostic> rows,NativeMapDocument target,Func<bool> current)
    {
        var warnings=rows.Where(row=>row.Severity==DiagnosticSeverity.Warning).ToArray();
        if(warnings.Length==0)return true;
        return CompileWarningsPrompt?.Invoke(warnings)??ShowDocumentReport(DocumentReportPurpose.Compile,rows,true,target,current);
    }
    private IReadOnlyList<MapDiagnostic> SaveCompatibilityRows(EditorSavePlan plan,NativeMapDocument target)
    {
        if(SaveReportFlags==0||plan.HasBlockingIssues||plan.Format==EditorSaveFormat.Dkm&&(SaveReportFlags&EditorSaveReportFlags.Version)==0)return [];
        return EditorReportRules.CompatibilityRows(GameVersionCompatibility.Validate(target).Select(row=>new EditorReportEntry(row,true)),
            plan.Format==EditorSaveFormat.Dkm?EditorReportOutput.Editor:EditorReportOutput.Text,SaveReportFlags);
    }
}
