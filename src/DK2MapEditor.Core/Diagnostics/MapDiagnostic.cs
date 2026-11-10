namespace DK2MapEditor.Core.Diagnostics;

public sealed record MapDiagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    string? FilePath = null,
    MapDiagnosticRegion? Region = null);

/// <summary>Zero-based tile coordinates of a reported map region.</summary>
public sealed record MapDiagnosticRegion(int X, int Y, int Width, int Height);
