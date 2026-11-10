using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared actual controls; this stage compiles but does not execute WPF.
    private static void CheckDocumentReports(bool native,string language)
    {
        var d=NativeMapDocument.CreateNewMap(new("ReportFixture","Report fixture",16,14,NativeFormat:native));
        var workspace=new EditorWorkspace{Document=d};workspace.SetSelection(new(6,6,2,2));
        var canvas=(MapCanvas)workspace.FindName("Canvas");canvas.HighlightRegion(new(1,1,1,1));
        var before=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=d.EditRevision;var selection=canvas.Selection;
        var rows=Enumerable.Range(0,25).Select(i=>new MapDiagnostic(DiagnosticSeverity.Warning,"RepeatedCode","Message "+i,Region:new(2,3,2,2))).ToArray();
        var restore=workspace.PreserveDiagnosticHighlight();var current=true;
        var report=new DocumentReportWindow(DocumentReportPurpose.Compile,rows,true,()=>current,workspace.NavigateToDiagnostic);
        try
        {
            var list=(DataGrid)report.FindName("ReportRows");
            Expect(report.Diagnostics.Count==25&&list.Items.Count==25,"Report truncated or deduplicated diagnostics.");
            list.SelectedIndex=20;
            Expect(canvas.CursorTile==new TilePoint(2,3)&&canvas.HighlightedRegion==new TileRect(2,3,2,2)&&canvas.Selection==selection,"Report navigation edited selection or lost region.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(list.SelectedIndex==20&&report.Title==LocalizationService.Get("Report.Compile"),"Live report translation lost selection/title.");
            current=false;report.RefreshCurrent();
            Expect(!report.Accept()&&!((Button)report.FindName("ReportContinue")).IsEnabled,"Stale report approved continuation.");
        }
        finally{report.Cancel();restore();LocalizationService.SetLanguage(language);}
        Expect(canvas.HighlightedRegion==new TileRect(1,1,1,1)&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&d.EditRevision==revision,"Report changed bytes/revision or lost prior highlight.");
        var load=new DocumentReportWindow(DocumentReportPurpose.Load,rows,false,()=>true);
        Expect(!load.Accept(),"Load close-only report approved an operation.");load.Cancel();
        var compile=new DocumentReportWindow(DocumentReportPurpose.Compile,rows,true,()=>true);
        Expect(compile.Accept(),"Current warning report did not accept explicit continuation.");compile.Cancel();
        var errors=new DocumentReportWindow(DocumentReportPurpose.Compile,[new(DiagnosticSeverity.Error,"bad","Bad")],true,()=>true);
        Expect(!errors.Accept(),"Error report approved continuation.");errors.Cancel();
    }
}
