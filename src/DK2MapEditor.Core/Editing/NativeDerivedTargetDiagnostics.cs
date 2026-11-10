using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

internal static class NativeDerivedTargetDiagnostics
{
    internal static IReadOnlyList<MapDiagnostic> Validate(NativeMapDocument document)
    {
        if (document.IsLegacyProject) return [];
        var records = document.Records();
        var crystal = records.FirstOrDefault(NativeMapDocument.NativeSourceCrystal);
        var crystalPoint = crystal is null ? (TilePoint?)null : NativeMapDocument.NativeDerivedPosition(crystal, false);
        var diagnostics = new List<MapDiagnostic>();
        foreach (var action in records.Where(NativeMapDocument.NativeDerivedAction))
        {
            var point = NativeMapDocument.NativeDerivedPayload(action); var isCrystal = action.File.Data.Span[action.Offset + 22] == 2;
            var matches = isCrystal ? crystalPoint is { } expected && point == new TilePoint(unchecked((ushort)expected.X), unchecked((ushort)expected.Y))
                : document.NativeGateTarget(action) is { } gate && NativeMapDocument.NativeDerivedPosition(gate,true) is var gatePoint
                    && point == new TilePoint(unchecked((ushort)(gatePoint.X+1)),unchecked((ushort)(gatePoint.Y+1)));
            if (!matches) diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2N032",
                $"Calculated {(isCrystal ? "portal gem" : "hero gate")} target at 0x{action.Offset:X} is missing or stale: ({point.X}, {point.Y}).", action.File.SourcePath));
        }
        return diagnostics;
    }
}
