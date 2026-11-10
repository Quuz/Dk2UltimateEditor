using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Formats;

public sealed record Dk2FileInspection(
    string FilePath,
    Dk2FileKind Kind,
    long FileSize,
    uint Header,
    int Version,
    string? MapName,
    string? Author,
    int? Width,
    int? Height,
    IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.All(item => item.Severity != DiagnosticSeverity.Error);
}
