using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Read-only TaskTestThings/TaskTestGates notices. Result2 and
    /// two-square gate prerequisites are informational; no new hard errors.</summary>
    internal IReadOnlyList<MapDiagnostic> CompilerPlacementDiagnostics()
    {
        var records = Records(); var result = new List<MapDiagnostic>();
        var walking = new Dictionary<uint, (bool? Water, bool? Lava)>();
        foreach (var record in records)
        {
            if (IsLegacyProject)
            {
                if (record.File.Signature != LegacyThings || record.File.UInt32(4) is < 1 or > 6
                    || !record.Category.StartsWith("Project.MAPTHING", StringComparison.Ordinal)
                    || !new[] { "node.type", "id", "owner", "x", "y" }.All(name => record.Fields.Any(field => field.Name == name))
                    || PlacementNumber(record, "owner") > 7) continue;
            }
            else if (record.File.Signature != 0xBE || record.File.UInt32(4) != 4
                || RecordTemplate.All.FirstOrDefault(template => template.Structure == record.Category) is not { } template
                || record.Length != template.Size || record.Fields.Count != LegacySchemas.Fields(record.Category).Count) continue;
            if (PlacementExisting(record) is not { } thing) continue;
            var point = thing.Position;
            // Structural diagnostics already report invalid coordinates. Do
            // not index terrain or reinterpret wrapped/unverified locations.
            if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height) continue;
            (bool? Water, bool? Lava) flags = (null, null);
            if (thing.Kind == PlacedThingKind.Creature && !walking.TryGetValue(thing.TypeID, out flags))
                walking[thing.TypeID] = flags = PlacementCreatureFlags(thing.TypeID, records);
            var tile = ThingPlacementRules.EditorTile(GetTile(point.X, point.Y), IsEditorProtectedCell(point.X, point.Y));
            var permission = ThingPlacementRules.Evaluate(tile, thing.Kind, thing.TypeID, thing.Owner, flags.Water, flags.Lava);
            var code = permission switch
            {
                ThingTerrainPermission.BadTerrain => "DK2N041",
                ThingTerrainPermission.Enemy => "DK2N042",
                ThingTerrainPermission.Fatal => "DK2N043",
                _ => null
            };
            if (code is null) continue;
            var severity = permission == ThingTerrainPermission.Enemy ? DiagnosticSeverity.Information : DiagnosticSeverity.Warning;
            result.Add(new(severity, code, $"Source placement rule: {thing.Kind} ID{thing.TypeID}, owner{thing.Owner}, {permission} at ({point.X}, {point.Y}).",
                record.File.SourcePath, new(point.X, point.Y, 1, 1)));
        }
        if (!VerifiedCompilerGateRegions(records)) return result;
        foreach (var site in HeroGateSites())
        {
            var bounds = site.Footprint;
            if (site.Type == 3 && !site.CannotDestroy)
            {
                string? detail = null;
                if (site.Gate is null) detail = "missing gate binding";
                else if (site.Area is null)
                { detail = "missing destruction area"; bounds = new(bounds.X - 1, bounds.Y - 1, bounds.Width + 2, bounds.Height + 2); }
                else if (!site.HasEvents) detail = "missing source destruction events";
                if (detail is not null) result.Add(new(DiagnosticSeverity.Information, "DK2N044",
                    $"Source two-square hero gate: {detail} at ({bounds.X}, {bounds.Y}).", TileFile.SourcePath, new(bounds.X, bounds.Y, bounds.Width, bounds.Height)));
            }
            else if (site.Type is 6 or 7 && site.Gate is null)
                result.Add(new(DiagnosticSeverity.Warning, "DK2N045", $"Source hero gate type{site.Type} has no gate binding at ({bounds.X}, {bounds.Y}).",
                    TileFile.SourcePath, new(bounds.X, bounds.Y, bounds.Width, bounds.Height)));
        }
        return result;
    }
    private bool VerifiedCompilerGateRegions(IReadOnlyList<NativeRecord> records)
    {
        if (IsLegacyProject)
        {
            var sections = projectSections.Where(file => file.Signature == LegacyAreas).ToArray();
            if (sections.Length != 1 || sections[0].UInt32(4) != 1) return false;
            var file = sections[0];
            foreach (var record in records.Where(record => record.File == file))
            {
                if (record.Category != "Project.MAPRECTEX" || PlacementNumber(record, "node.type") > 2) return false;
                var x1 = PlacementNumber(record, "rect.x1"); var x2 = PlacementNumber(record, "rect.x2");
                var y1 = PlacementNumber(record, "rect.y1"); var y2 = PlacementNumber(record, "rect.y2");
                if (x1 > x2 || y1 > y2 || x2 >= Width || y2 >= Height) return false;
            }
            return true;
        }
        if (files.Any(file => file.Signature == 0xBE && file.UInt32(4) != 4)) return false;
        foreach (var record in records.Where(record => record.File.Signature == 0xBE && record.Length >= 8
            && record.File.UInt32(record.Offset) is 0xC5 or 0xCC))
        {
            if (record.Category is not ("GAMEAREA" or "GAMEGATE") || record.Fields.Count == 0
                || record.Length != (record.Category == "GAMEAREA" ? 64 : 28)) return false;
            foreach (var field in record.Fields.Where(field => field.Name.EndsWith('X') || field.Name.EndsWith('Y')))
            {
                var value = PlacementNumber(record, field.Name);
                if (value >= (field.Name.EndsWith('X') ? Width : Height)) return false;
            }
            if (record.Category == "GAMEAREA" && (PlacementNumber(record, "dwAreaLocationStartX") > PlacementNumber(record, "dwAreaLocationEndX")
                || PlacementNumber(record, "dwAreaLocationStartY") > PlacementNumber(record, "dwAreaLocationEndY"))) return false;
        }
        return true;
    }
}
