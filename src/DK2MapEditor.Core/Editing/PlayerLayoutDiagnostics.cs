using System.Globalization;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

/// <summary>Read-only native player identity/count diagnostics. Project player
/// rows are seven positional slots, not the declared number of keepers.</summary>
public static class PlayerLayoutDiagnostics
{
    public static IReadOnlyList<MapDiagnostic> Validate(NativeMapDocument document, bool includeGameplay = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.IsLegacyProject) return [];
        var files = document.Files.Where(file => file.Signature == 0xB4).ToArray();
        if (files.Length == 0 || files.Any(file => file.UInt32(4) != 4)) return [];
        var records = document.Records();
        // Unknown layouts keep their existing opaque diagnostics. An unreadable
        // record is not evidence that a particular player is absent.
        if (records.Any(record => files.Contains(record.File) && (record.Category != "GAMEPLAYER" || record.Length != 205))) return [];
        var players = document.PlayerSettingsRows();
        var diagnostics = new List<MapDiagnostic>();
        void Add(string code, string message, string path, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
            => diagnostics.Add(new(severity, code, message, path));
        foreach (var row in players.Where(row => row.ID is < 1 or > 7))
            Add("DK2N033", $"Stored player ID {row.ID} is outside 1–7 at 0x{row.Record.Offset:X}; the record is retained.", row.Record.File.SourcePath);
        foreach (var group in players.Where(row => row.ID is >= 1 and <= 7).GroupBy(row => row.ID).Where(group => group.Count() > 1))
            Add("DK2N034", $"Player {group.Key} has {group.Count()} stored records; specialized editing requires a unique ID. Physical order is retained.", group.First().Record.File.SourcePath);

        var metadata = document.MainFile.Signature == 0xDC && document.MainFile.UInt32(4) == 4
            ? records.FirstOrDefault(record => record.Category == "Map" && record.File == document.MainFile) : null;
        var countField = metadata?.Fields.FirstOrDefault(field => field.Name == "bMainPlayers");
        if (metadata is not null && countField is not null)
        {
            var keepers = int.Parse(metadata.Read(countField), CultureInfo.InvariantCulture);
            if (keepers is < 1 or > 5)
                Add("DK2N035", $"Declared keeper count {keepers} is outside 1–5; {players.Count} physical player records are retained.", metadata.File.SourcePath);
            else
            {
                var expected = Enumerable.Range(1, keepers + 2).Select(id => (uint)id).ToArray();
                if (players.Count != expected.Length)
                    Add("DK2N035", $"Declared {keepers} keeper(s) require {expected.Length} player records including heroes/neutral; found {players.Count}.", metadata.File.SourcePath);
                var actual = players.Select(row => row.ID).ToHashSet();
                var missing = expected.Where(id => !actual.Contains(id)).ToArray();
                var extra = actual.Where(id => !expected.Contains(id)).Order().ToArray();
                if (missing.Length != 0 || extra.Length != 0)
                    Add("DK2N036", $"Declared player slots differ from stored IDs. Missing: [{string.Join(", ", missing)}]; outside declaration: [{string.Join(", ", extra)}]. No records were created or reordered.", metadata.File.SourcePath);
            }
        }
        if (!includeGameplay) return diagnostics;
        uint Number(NativeRecord record, string name) => uint.Parse(record.Read(record.Fields.Single(field => field.Name == name)), CultureInfo.InvariantCulture);
        var hearts = records.Where(record => record.Category == "GAMEGATE" && record.File.UInt32(4) == 4 && Number(record, "bGateType") == 14);
        foreach (var group in hearts.GroupBy(record => Number(record, "bGlobalPlayer")))
        {
            var matches = players.Count(row => row.ID == group.Key);
            if (matches != 1)
                Add("DK2N037", $"Dungeon-heart owner {group.Key} matches {matches} stored player records; no player identity is inferred from file order.", group.First().File.SourcePath);
            if (group.Key is >= 3 and <= 7 && group.Count() > 1)
                Add("DK2N038", $"Keeper {group.Key - 2} has {group.Count()} native heart records. Source-compatible health reads/writes use the last matching heart; earlier records are retained.", group.Last().File.SourcePath, DiagnosticSeverity.Information);
        }
        return diagnostics;
    }
}
