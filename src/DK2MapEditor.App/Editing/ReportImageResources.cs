using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.App.Editing;

/// <summary>Compiler_Report.c's87/88/89 source resources, by the report's
/// already-normalized severity. This mapping does not classify diagnostics.</summary>
internal static class ReportImageResources
{
    internal static int? For(DiagnosticSeverity severity)=>severity switch
    {DiagnosticSeverity.Warning=>87,DiagnosticSeverity.Error=>88,DiagnosticSeverity.Information=>89,_=>null};
}
