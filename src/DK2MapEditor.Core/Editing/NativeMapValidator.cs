using System.Buffers.Binary;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

public static class NativeMapValidator
{
    /// <summary>Structural validation remains available for unfinished maps and
    /// lossless conversions. The interactive validator also checks gameplay terrain.</summary>
    public static IReadOnlyList<MapDiagnostic> Validate(NativeMapDocument document, bool includeGameplay = false, bool includeScripts = true)
    {
        var diagnostics = new List<MapDiagnostic>();
        foreach (var file in document.Files)
        {
            var declared = BinaryPrimitives.ReadUInt32LittleEndian(file.Data.Span[8..]);
            var expected = document.IsLegacyProject && file != document.TileFile ? file.Length - 12 : file.Length;
            if (declared != expected)
                diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2N001", $"Declared file size differs from physical size; opaque trailing data is preserved ({declared}/{file.Length}).", file.SourcePath));
        }
        foreach (var missing in document.MissingReferences)
            diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2N002", "Referenced definition is not available locally: " + missing, document.MainFile.SourcePath));
        var records = document.Records();
        foreach (var record in records.Where(record => record.Category == Formats.NativeOpaqueFile.Category))
            diagnostics.Add(Formats.NativeOpaqueFile.Diagnostic(record.File.SourcePath));
        foreach (var record in records.Where(record => record.Category == Formats.NativeCollectionLayout.OpaqueCategory
                     && (includeScripts || record.File.Signature != 0xD2)))
            diagnostics.Add(Formats.NativeCollectionLayout.Unverified(record.File.SourcePath, record.Offset, record.Length));
        foreach (var file in document.Files.Where(file => NativeMapDocument.NativeDefinitionSignature(file.Signature)))
            diagnostics.AddRange(NativeMapDocument.InspectNativeDefinitions(file));
        foreach (var band in records.Where(NativeMapDocument.IsBand))
        {
            if (band.Category == "GAMEBAND" && band.Length != 563)
            {
                diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2N008", "This hero-band variant is preserved but has no verified member-editing layout.", band.File.SourcePath));
                continue;
            }
            var members = document.BandMembers(band);
            var count = members.TakeWhile(member => member.Active).Count();
            if (members.Skip(count).Any(member => member.Active))
                diagnostics.Add(new(document.IsLegacyProject ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning, "DK2N009", "Active band members follow an inactive end slot; the historical compiler/reader stops at the first inactive slot.", band.File.SourcePath));
            if (document.IsLegacyProject)
            {
                var sentinel = LegacyProjectSchemas.Fields(band.Category[8..]).Single(field => field.Name == "heros[16].isvalid");
                if (band.Read(sentinel) != "0") diagnostics.Add(new(DiagnosticSeverity.Error, "DK2N010", "The seventeenth hero-band slot must remain an inactive end marker.", band.File.SourcePath));
            }
        }
        // Original-style text export deliberately contains no scenario graph.
        // Its projection still validates all non-script data; normal validation
        // and all existing callers continue checking scripts by default.
        if (includeScripts) diagnostics.AddRange(ScriptGraphDiagnostics.Validate(document));
        if (includeScripts) diagnostics.AddRange(NativeDerivedTargetDiagnostics.Validate(document));
        diagnostics.AddRange(EntityReferenceDiagnostics.Validate(document, includeScripts));
        diagnostics.AddRange(DefinitionReferenceDiagnostics.Validate(document, includeScripts));
        diagnostics.AddRange(GameVersionCompatibility.Validate(document, includeScripts));
        diagnostics.AddRange(PlayerGeometryDiagnostics.Validate(document));
        diagnostics.AddRange(PlayerLayoutDiagnostics.Validate(document, includeGameplay));
        foreach (var record in records)
        {
            if (record.Category == "Project.Opaque" && record.Fields.Count == 0)
                diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2N008", "This definition variant is preserved but has no verified editing schema.", record.File.SourcePath));
            if (record.File.Signature is not (0xBE or NativeMapDocument.LegacyThings or NativeMapDocument.LegacyAreas)) continue;
            foreach (var field in record.Fields.Where(field => field.Name.EndsWith("LocationX", StringComparison.Ordinal) || field.Name.EndsWith("LocationY", StringComparison.Ordinal) || field.Name.EndsWith("StartX", StringComparison.Ordinal) || field.Name.EndsWith("StartY", StringComparison.Ordinal) || field.Name.EndsWith("EndX", StringComparison.Ordinal) || field.Name.EndsWith("EndY", StringComparison.Ordinal) || field.Name is "x" or "y" or "rect.x1" or "rect.x2" or "rect.y1" or "rect.y2"))
            {
                if (record.Category.StartsWith("BandHero", StringComparison.Ordinal)) continue;
                var limit = field.Name.EndsWith('X') || field.Name is "x" or "rect.x1" or "rect.x2" ? document.Width : document.Height;
                if (long.TryParse(record.Read(field), out var value) && (value < 0 || value >= limit))
                    diagnostics.Add(new(DiagnosticSeverity.Error, "DK2N005", $"Object coordinate is outside the map: {field.Name}={value} at 0x{record.Offset:X}.", record.File.SourcePath));
            }
        }
        var invalidOwner = 0;
        var invalidVariant = 0;
        for (var y = 0; y < document.Height; y++)
            for (var x = 0; x < document.Width; x++)
            {
                var tile = document.GetTile(x, y);
                if (tile.Owner is < 1 or > 7) invalidOwner++;
                if (tile.Variant is not (1 or 2)) invalidVariant++;
            }
        if (invalidOwner != 0) diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2N006", $"Tiles with owner IDs outside 1–7: {invalidOwner}.", document.TileFile.SourcePath));
        if (invalidVariant != 0) diagnostics.Add(new(DiagnosticSeverity.Warning, "DK2N007", $"Tiles with unsupported terrain variants: {invalidVariant}.", document.TileFile.SourcePath));
        if (includeGameplay)
        {
            if (document.IsLegacyProject && CompilerMapNames.OutputStem(document.MainFile.SourcePath) is var stem && CompilerMapNames.IsReserved(stem))
                diagnostics.Add(new(DiagnosticSeverity.Information, "DK2N046", "The game output name is reserved by the original compiler: " + stem + ". Use Save As with another filename before compiling.", document.MainFile.SourcePath));
            diagnostics.AddRange(TerrainLayoutDiagnostics.Validate(document));
            diagnostics.AddRange(document.CompilerPlacementDiagnostics());
        }
        return diagnostics;
    }
}
