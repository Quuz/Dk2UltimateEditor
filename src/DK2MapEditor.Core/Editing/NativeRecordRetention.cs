namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    private static bool RetainedRecordCategory(NativeRecord record)
        => record.Category is "GAMEAREA" or "GAMEBAND" or "GAMEPLAYER";

    private static string RetainedRecordIdentity(NativeRecord record)
    {
        var name = record.Category switch { "GAMEAREA" => "bAreaID", "GAMEBAND" => "bBandID", _ => "bGlobalPlayer" };
        return record.Category + ":" + record.Read(record.Fields.Single(field => field.Name == name));
    }

    private void RestoreUneditedNativeRecords(NativeMapDocument output, NativeMapDocument original,
        NativeMapDocument baseline, NativeMapDocument generatedBaseline, IReadOnlyDictionary<ushort, ushort> eventIDs)
    {
        if (Width != baseline.Width || Height != baseline.Height) return;
        static Dictionary<string, NativeRecord[]> Groups(NativeMapDocument map) => map.Records()
            .Where(RetainedRecordCategory).GroupBy(RetainedRecordIdentity)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var raw = Groups(original); var prior = Groups(generatedBaseline); var current = Groups(output);
        var previousBands = baseline.Records().Where(record => record.Category == "Project.MTHBAND").ToArray();
        var currentBands = Records().Where(record => record.Category == "Project.MTHBAND").ToArray();
        static string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
        foreach (var (identity, targets) in current)
        {
            // A native byte ID is not a globally unique identity. Never choose
            // among duplicate source, baseline or output records.
            if (targets.Length != 1 || !raw.TryGetValue(identity, out var sources) || sources.Length != 1
                || !prior.TryGetValue(identity, out var generated) || generated.Length != 1) continue;
            var target = targets[0]; var source = sources[0]; var previous = generated[0];
            var expectedLength = target.Category switch { "GAMEAREA" => 64, "GAMEBAND" => 563, _ => 205 };
            if (target.Length != expectedLength || source.Length != expectedLength || previous.Length != expectedLength) continue;
            var bytes = target.File.Bytes.AsSpan(target.Offset, target.Length);
            var previousBytes = previous.File.Data.Span.Slice(previous.Offset, previous.Length);
            var sourceBytes = source.File.Data.Span.Slice(source.Offset, source.Length);
            // This restores unedited imported records. A supported value or
            // derived geometry edit always keeps its current compiler result.
            if (!bytes.SequenceEqual(previousBytes)) continue;
            if (target.Category != "GAMEPLAYER" && !bytes[..8].SequenceEqual(sourceBytes[..8])) continue;
            if (target.Category == "GAMEBAND")
            {
                var id = Read(target, "bBandID");
                var oldBands = previousBands.Where(band => Read(band, "id") == id).ToArray();
                var newBands = currentBands.Where(band => Read(band, "id") == id).ToArray();
                if (oldBands.Length != 1 || newBands.Length != 1 || Read(oldBands[0], "uniqueid") != Read(newBands[0], "uniqueid")) continue;
                // Unlinked member identity changes may compile to identical
                // roots. They still invalidate source record association.
                if (Enumerable.Range(0, 16).Any(index => Read(oldBands[0], $"heros[{index}].uniqueid") != Read(newBands[0], $"heros[{index}].uniqueid"))) continue;
            }
            var fieldsMatch = true;
            foreach (var field in target.Fields)
            {
                var value = field.Read(sourceBytes);
                if (field.Name.EndsWith("Trigger", StringComparison.Ordinal) && ushort.TryParse(value, out var root) && root != 0)
                {
                    if (!eventIDs.TryGetValue(root, out var mapped)) { fieldsMatch = false; break; }
                    value = mapped.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                if (value != field.Read(previousBytes)) { fieldsMatch = false; break; }
            }
            if (!fieldsMatch) continue;
            // The band header is 51 wire bytes, not the aligned CLR/C sizeof.
            // Do not restore inactive member slots or replace current roots.
            var limit = target.Category == "GAMEBAND" ? 51 : target.Length;
            var editable = new bool[limit];
            foreach (var field in target.Fields)
                for (var offset = field.Offset; offset < Math.Min(limit, field.Offset + field.Size); offset++) editable[offset] = true;
            for (var offset = target.Category == "GAMEPLAYER" ? 0 : 8; offset < limit; offset++)
                if (!editable[offset]) bytes[offset] = sourceBytes[offset];
            // Native strings can contain opaque bytes after their terminator.
            // Equal decoded text permits exact restoration of that field;
            // changed names already failed the generated-record comparison.
            foreach (var field in target.Fields.Where(field => field.Kind is NativeValueKind.Ascii or NativeValueKind.Unicode or NativeValueKind.WesternAnsi))
                sourceBytes.Slice(field.Offset, field.Size).CopyTo(bytes[field.Offset..]);
        }
    }
}
