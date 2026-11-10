using System.Globalization;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

/// <summary>Read-only checks of source-proven entity links, including imported data.</summary>
public static class EntityReferenceDiagnostics
{
    public static IReadOnlyList<MapDiagnostic> Validate(NativeMapDocument document, bool includeScripts = true)
    {
        var records = document.Records();
        var diagnostics = new List<MapDiagnostic>();
        var identities = records.Select(EntityReferences.Identity).OfType<EntityIdentity>()
            .GroupBy(identity => (identity.Kind, ID: identity.Record.Read(identity.Field)))
            .ToDictionary(group => group.Key, group => group.ToArray());
        void Add(string code, NativeRecord record, NativeField field, string message) => diagnostics.Add(new(
            DiagnosticSeverity.Warning, code, message + $" ({record.Category}.{field.Name}, 0x{record.Offset + field.Offset:X}).", record.File.SourcePath));
        foreach (var ((kind, id), owners) in identities)
            if (owners.Length > 1)
                foreach (var owner in owners)
                    Add("DK2N028", owner.Record, owner.Field, $"Duplicate {kind} identity {id}: {owners.Length} records");

        var unknown = new HashSet<EntityKind>();
        foreach (var record in records)
        {
            if (record.File.Signature == 0xBE && record.Category.StartsWith("Record ", StringComparison.Ordinal))
                unknown.UnionWith(Enum.GetValues<EntityKind>());
            if (record.Category != "Project.Opaque") continue;
            if (record.File.Signature == NativeMapDocument.LegacyAreas)
                unknown.UnionWith(new[] { EntityKind.Area, EntityKind.Gate, EntityKind.Effect });
            if (record.File.Signature == NativeMapDocument.LegacyBands) unknown.Add(EntityKind.Band);
        }
        // Both original compiler and reader stop at the first inactive band slot.
        // Bytes in the unused suffix are retained, not interpreted as live references.
        var nativeMembers = records.Where(record => record.Category.StartsWith("BandHero", StringComparison.Ordinal))
            .ToDictionary(record => (record.File, record.Offset));
        var bandCounts = new Dictionary<(NativeFile File, int Offset), int>();
        var activeNativeMembers = new HashSet<(NativeFile File, int Offset)>();
        foreach (var band in records.Where(NativeMapDocument.IsBand).Where(record => record.Category != "GAMEBAND" || record.Length == 563))
        {
            var count = 0;
            for (; count < 16; count++)
            {
                if (band.Category == "GAMEBAND")
                {
                    var key = (band.File, band.Offset + 51 + count * 32);
                    if (!nativeMembers.TryGetValue(key, out var member)
                        || member.Read(member.Fields.Single(field => field.Name == "bGlobalCreatureID")) == "0"
                        || member.Read(member.Fields.Single(field => field.Name == "bGlobalBehavior")) == "0") break;
                    activeNativeMembers.Add(key);
                }
                else if (band.Fields.FirstOrDefault(field => field.Name == $"heros[{count}].isvalid") is not { } valid
                    || band.Read(valid) == "0") break;
            }
            bandCounts.Add((band.File, band.Offset), count);
        }
        foreach (var record in records)
        {
            if (!includeScripts && (ScriptGraph.IsEvent(record) || record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal))) continue;
            if (record.Category.StartsWith("BandHero", StringComparison.Ordinal)
                && !activeNativeMembers.Contains((record.File, record.Offset))) continue;
            foreach (var reference in EntityReferences.From(record))
            {
                if (NativeMapDocument.IsBand(record) && reference.Field.Name.StartsWith("heros[", StringComparison.Ordinal))
                {
                    var member = Regex.Match(reference.Field.Name, @"^heros\[(\d+)\]\.");
                    if (!member.Success || int.Parse(member.Groups[1].Value, CultureInfo.InvariantCulture) >= bandCounts.GetValueOrDefault((record.File, record.Offset))) continue;
                }
                var id = record.Read(reference.Field);
                if (EntityReferences.IsNone(reference, id)) continue;
                var count = identities.GetValueOrDefault((reference.Kind, id))?.Length ?? 0;
                if (count == 0 && !unknown.Contains(reference.Kind))
                    Add("DK2N026", record, reference.Field, $"Missing {reference.Kind} target {id}");
                else if (count > 1)
                    Add("DK2N027", record, reference.Field, $"Ambiguous {reference.Kind} target {id}: {count} records");
            }
        }
        return diagnostics;
    }
}
