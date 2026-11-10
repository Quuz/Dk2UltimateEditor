using System.Buffers.Binary;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private static (uint X, uint Y, byte Type, byte Owner) NativeGateKey(NativeRecord record)
    {
        if (record.Length != 28) throw new InvalidDataException("Unverified native gate layout.");
        var bytes = record.File.Data.Span.Slice(record.Offset, 28);
        return (BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]), BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]), bytes[26], bytes[27]);
    }

    private void RestoreUneditedNativeGates(NativeMapDocument output, NativeMapDocument original,
        NativeMapDocument baseline, NativeMapDocument generatedBaseline)
    {
        if (Width != baseline.Width || Height != baseline.Height) return;
        var raw = original.Records().Where(record => record.Category == "GAMEGATE").GroupBy(NativeGateKey).ToDictionary(group => group.Key, group => group.ToArray());
        var prior = generatedBaseline.Records().Where(record => record.Category == "GAMEGATE").GroupBy(NativeGateKey).ToDictionary(group => group.Key, group => group.ToArray());
        var current = output.Records().Where(record => record.Category == "GAMEGATE").GroupBy(NativeGateKey).ToDictionary(group => group.Key, group => group.ToArray());
        static NativeRecord[] ProjectGates(NativeMapDocument map) => map.Records().Where(record => record.Category == "Project.MAPRECTEX"
            && record.Read(record.Fields.Single(field => field.Name == "node.type")) == "1").ToArray();
        var priorRegions = ProjectGates(baseline);
        var currentRegions = ProjectGates(this);
        static string Read(NativeRecord record, string field) => record.Read(record.Fields.Single(candidate => candidate.Name == field));
        foreach (var (key, targets) in current)
        {
            // Coordinates/type/owner are not globally unique IDs. Refuse to
            // guess whenever any of the three bounded documents is ambiguous.
            if (targets.Length != 1 || !raw.TryGetValue(key, out var sources) || sources.Length != 1
                || !prior.TryGetValue(key, out var previous) || previous.Length != 1) continue;
            if (key.Type != 14)
            {
                var regions = priorRegions.Where(region => Read(region, "rect.x1") == key.X.ToString() && Read(region, "rect.y1") == key.Y.ToString()).ToArray();
                if (regions.Length != 1) continue;
                var matches = currentRegions.Where(region => Read(region, "id") == Read(regions[0], "id")).ToArray();
                if (matches.Length != 1 || new[] { "rect.x1", "rect.y1", "rect.x2", "rect.y2" }.Any(field => Read(matches[0], field) != Read(regions[0], field))) continue;
            }
            var x = checked((int)key.X); var y = checked((int)key.Y);
            // Five-by-five covers a heart, both gate footprints and the
            // neighbors used by derived orientation. Compare DKM tiles to its
            // projected baseline, not normalized tiles to raw native bytes.
            var unchanged = true;
            for (var py = Math.Max(0, y - 2); py <= Math.Min(Height - 1, y + 2) && unchanged; py++)
                for (var px = Math.Max(0, x - 2); px <= Math.Min(Width - 1, x + 2); px++)
                    if (GetTile(px, py) != baseline.GetTile(px, py)) { unchanged = false; break; }
            if (!unchanged) continue;
            var target = targets[0].File.Bytes.AsSpan(targets[0].Offset, 28);
            var generated = previous[0].File.Data.Span.Slice(previous[0].Offset, 28);
            // Health is a supported current value. Keep it freshly generated;
            // it does not make the unchanged gate's source-only ID obsolete.
            if (!target[..24].SequenceEqual(generated[..24]) || !target[26..].SequenceEqual(generated[26..])) continue;
            var source = sources[0].File.Data.Span.Slice(sources[0].Offset, 28);
            // Preserve literal ID/orientation bytes without assigning a new
            // meaning to official values such as heart ID117 or gate ID2.
            source.Slice(22, 2).CopyTo(target[22..]);
        }
    }
}
