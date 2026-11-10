using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

[Flags]
public enum EditorSaveReportFlags { None=0,Version=1,Warnings=2,Alerts=4,Information=8,Default=14 }
public enum EditorReportOutput { Game,Text,Editor,Temporary }
public sealed record EditorReportEntry(MapDiagnostic Diagnostic,bool Version=false);
/// <summary>Combined Files_Save compatibility gate and Compiler_Report row filtering. Never grants write permission.</summary>
public static class EditorReportRules
{
    public static IReadOnlyList<MapDiagnostic> CompatibilityRows(IEnumerable<EditorReportEntry> entries,EditorReportOutput output,EditorSaveReportFlags flags)
    {
        if(output==EditorReportOutput.Temporary||flags==0)return [];
        return entries.Where(entry=>output==EditorReportOutput.Editor
            ? (flags&EditorSaveReportFlags.Version)!=0&&entry.Version
            : entry.Diagnostic.Severity switch
            {
                DiagnosticSeverity.Information=>(flags&EditorSaveReportFlags.Information)!=0,
                DiagnosticSeverity.Warning=>(flags&EditorSaveReportFlags.Warnings)!=0,
                DiagnosticSeverity.Error=>(flags&EditorSaveReportFlags.Alerts)!=0,_=>false
            }).Select(entry=>entry.Diagnostic).ToArray();
    }
    public static bool CanContinue(bool offered,bool current,IEnumerable<MapDiagnostic> entries)
        =>offered&&current&&!entries.Any(entry=>entry.Severity==DiagnosticSeverity.Error);
    public static TileRect? Region(MapDiagnosticRegion? region,int width,int height)
        =>region is not null&&region.X>=0&&region.Y>=0&&region.Width>0&&region.Height>0
            &&(long)region.X+region.Width<=width&&(long)region.Y+region.Height<=height
            ?new(region.X,region.Y,region.Width,region.Height):null;
}
