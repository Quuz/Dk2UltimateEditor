using System.Globalization;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

internal static class PlayerGeometryDiagnostics
{
    /// <summary>Compiler_TaskTestPlayers reports warnings, not blocking errors.
    /// Camera is checked for every keeper; CPU area only for computer keepers.
    /// Preserve the source's independent -1 camera checks and upper bounds.</summary>
    internal static IReadOnlyList<MapDiagnostic> Validate(NativeMapDocument document)
    {
        var diagnostics = new List<MapDiagnostic>(); var rows = document.PlayerSettingsRows();
        foreach (var row in rows.Where(row => row.ID is >= 3 and <= 7 && NativeMapDocument.VerifiedResizePlayer(row.Record) && rows.Count(other => other.ID == row.ID) == 1))
        {
            var record = row.Record;
            long Value(string coordinate)
            {
                var name = document.IsLegacyProject ? (record.Category == "Project.MAPOPTIONSV1" ? coordinate : "Player" + coordinate)
                    : "bPlayer" + (coordinate.StartsWith("Camera", StringComparison.Ordinal) ? coordinate : "BuildArea" + coordinate[7..]);
                return long.Parse(record.Read(record.Fields.Single(field => field.Name == name)), CultureInfo.InvariantCulture);
            }
            void Warn(string code, string text) => diagnostics.Add(new(DiagnosticSeverity.Warning, code,
                $"Keeper {row.ID - 2}: {text} at 0x{record.Offset:X}.", record.File.SourcePath));
            if (NativeMapDocument.ResizePlayerCameraSupported(record))
            {
                var x = Value("CameraX"); var y = Value("CameraY");
                if (x != -1 && x >= document.Width || y != -1 && y >= document.Height)
                    Warn("DK2N030", $"starting camera exceeds map dimensions ({x}, {y}) / ({document.Width}, {document.Height})");
            }
            if (row.Computer && (Value("CPUAreaX1") >= document.Width || Value("CPUAreaX2") >= document.Width
                || Value("CPUAreaY1") >= document.Height || Value("CPUAreaY2") >= document.Height))
                Warn("DK2N031", "computer build area exceeds map dimensions");
        }
        return diagnostics;
    }
}
