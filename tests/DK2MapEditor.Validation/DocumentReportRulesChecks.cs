using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Diagnostics;

internal static class DocumentReportRulesChecks
{
    public static void Run()
    {
        void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);}
        var info=new MapDiagnostic(DiagnosticSeverity.Information,"info","Information");
        var warning=new MapDiagnostic(DiagnosticSeverity.Warning,"warning","Warning");
        var error=new MapDiagnostic(DiagnosticSeverity.Error,"error","Error");
        var version=new MapDiagnostic(DiagnosticSeverity.Information,"DK2V004","Requires 1.70");
        EditorReportEntry[] rows=[new(info),new(warning),new(error),new(version,true)];
        Check((int)EditorSaveReportFlags.Default==14,"Default source flags changed.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Editor,EditorSaveReportFlags.Default).Count==0,"Default editor save must not add a version report.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Editor,EditorSaveReportFlags.Version).SequenceEqual([version]),"Editor report must contain version rows only.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Editor,(EditorSaveReportFlags)15).SequenceEqual([version]),"Editor mode included ordinary severity rows.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Game,EditorSaveReportFlags.Default).SequenceEqual([info,warning,error,version]),"Game report changed source order or dropped version info.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Text,EditorSaveReportFlags.Warnings).SequenceEqual([warning]),"Text warning filter failed.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Text,EditorSaveReportFlags.Alerts).SequenceEqual([error]),"Text alert filter failed.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Text,EditorSaveReportFlags.Information).SequenceEqual([info,version]),"Text info filter failed.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Game,EditorSaveReportFlags.Version).Count==0,"Version bit became a game severity bit.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Temporary,(EditorSaveReportFlags)15).Count==0,"Temporary save opened a report.");
        Check(EditorReportRules.CompatibilityRows(rows,EditorReportOutput.Game,0).Count==0,"Disabled report still selected rows.");
        Check(!EditorReportRules.CanContinue(false,true,[warning]),"Close-only report approved an operation.");
        Check(!EditorReportRules.CanContinue(true,false,[warning]),"Stale report approved an operation.");
        Check(!EditorReportRules.CanContinue(true,true,[warning,error]),"Report approved an error.");
        Check(EditorReportRules.CanContinue(true,true,[info,warning]),"Current warning report cannot continue.");
        Check(EditorReportRules.CanContinue(true,true,[]),"Explicit empty continuation was rejected.");
        Check(EditorReportRules.Region(new(2,3,4,5),16,14)==new DK2MapEditor.Core.Editing.TileRect(2,3,4,5),"Valid diagnostic region was lost.");
        Check(EditorReportRules.Region(null,16,14) is null,"Non-spatial row gained coordinates.");
        Check(EditorReportRules.Region(new(-1,0,1,1),16,14) is null,"Source x=-1 metadata row gained navigation.");
        Check(EditorReportRules.Region(new(0,0,0,1),16,14) is null,"Empty diagnostic area was accepted.");
        Check(EditorReportRules.Region(new(15,13,1,1),16,14) is not null,"Last-cell region was rejected.");
        Check(EditorReportRules.Region(new(15,13,2,1),16,14) is null,"Outside-map diagnostic area was accepted.");
        Check(EditorReportRules.Region(new(int.MaxValue,0,2,1),16,14) is null,"Region addition overflowed.");
        var many=Enumerable.Range(0,25).Select(i=>new EditorReportEntry(new(DiagnosticSeverity.Warning,"same","row "+i))).ToArray();
        Check(EditorReportRules.CompatibilityRows(many,EditorReportOutput.Game,EditorSaveReportFlags.Default).SequenceEqual(many.Select(row=>row.Diagnostic)),"Report truncated beyond12 or deduplicated repeated codes.");
        Console.WriteLine("DOCUMENT REPORT SOURCE DETAIL 24 source filtering/continuation/region outcomes, including25 ordered rows; no WPF or file writes");
    }
}
