using System.Buffers.Binary;
using System.Security.Cryptography;
using DK2MapEditor.Core.Editing;

internal static class NativeCreatureVariantChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes)
    { Expect(bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Creature variant changed unexpected bytes."); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.IO.InvalidDataException) { return; }
        throw new InvalidOperationException("Unverified creature definition accepted.");
    }
    private static void Store(byte[] bytes, int offset, NativeField field, byte[] value)
    {
        if (field.BitMask != 0) bytes[offset] = (byte)((bytes[offset] & ~field.BitMask) | (value[0] == 0 ? 0 : field.BitMask));
        else value.CopyTo(bytes, offset);
    }
    public static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "DK2NativeCreatureVariants", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        (string Path, int Offset, int Count, int Size)[] inputs = [
            (@"D:\Games\DK2\Tools\ORIGIN17Creatures.kwd", 0, 46, 5449),
            (@"D:\Games\DK2\DKII 13\Dungeon Keeper 2\Data\editor\Creatures.kwd", 0, 44, 5449),
            (@"D:\Games\DK2\Dungeon Keeper 2\Data\editor\maps\ANL - 3 FunGlobals.kld", 50174, 46, 5537),
            (@"D:\Games\DK2\DKII 13\Dungeon Keeper 2\Data\editor\AutoSaveGlobals.kld", 45353, 44, 5537)
        ];
        var sourceHashes = inputs.Select(input => input.Path).Distinct().ToDictionary(path => path, path => SHA256.HashData(File.ReadAllBytes(path)));
        const string defaultsPath = @"D:\Games\DK2\DK2-Map-Editor\Binaries\Globals\DefaultCreatures.dkg";
        var defaults = File.ReadAllBytes(defaultsPath); sourceHashes.Add(defaultsPath, SHA256.HashData(defaults));
        // Independent literal GCOrder from the preserved original compiler.
        int[] order = [0,44,10,7,2,5,1,11,6,4,8,3,30,29,34,33,35,31,36,37,41,12,13,9,32,42,15,39,40,38,43,18,16,17,19,20,21,22,23,24,25,26,27,28,14,-2];
        foreach (var input in inputs)
        {
            var bytes = File.ReadAllBytes(input.Path).AsSpan(input.Offset, 56 + input.Count * input.Size).ToArray();
            Expect(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8)) == bytes.Length && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(20)) == input.Count, "Real fixture differs from audited header.");
            var seed = NativeMapDocument.Create(20, 20, "Variant");
            var inputDirectory = Path.Combine(root, $"input-{input.Count}-{input.Size}");
            var path = seed.SaveCopy(inputDirectory);
            var creaturePath = Path.Combine(inputDirectory, "Data", "Editor", "Creatures.kwd");
            File.WriteAllBytes(creaturePath, bytes);
            var document = NativeMapDocument.Load(path); var original = Snapshot(document); var rows = document.GlobalDefinitionRows().Where(row => row.Family == "Creatures").ToArray();
            var records = document.Records().Where(row => row.Category == "Definition.Creatures").ToArray();
            Expect(records.Length == input.Count && records.All(row => row.Length == input.Size && row.Fields.Count > 0), "Real creature prefix was not recognized.");
            Expect(rows.Count(row => row.Editable) == (input.Count == 44 ? 44 : 45) && rows.Where(row => !row.Editable).All(row => row.Record is null), "Historical catalogue invented/renumbered species.");
            if (input.Count == 44)
            {
                var missing = rows.Single(row => !row.Editable); Reject(() => document.SetGlobalDefinitionValues([new("Creatures", missing.Index, "Health", "1")])); Reject(() => document.ResetGlobalDefinitions("Attributes", [missing.Index])); Exact(document, original);
            }
            var selected = rows.First(row => row.Editable); var record = selected.Record!; var fields = document.GlobalDefinitionValues(selected);
            var changes = new List<GlobalDefinitionChange>(); var expected = original.ToDictionary(row => row.Key, row => row.Value.ToArray());
            foreach (var value in fields)
            {
                var field = record.Fields.Single(row => row.Name == value.Name); var changed = field.BitMask != 0 ? value.Value == "0" ? "1" : "0" : value.Value == "1" ? "2" : "1";
                changes.Add(new("Creatures", selected.Index, field.Name, changed));
                var encoded = new byte[field.Size]; var number = int.Parse(changed, System.Globalization.CultureInfo.InvariantCulture);
                if (field.Size == 1) encoded[0] = (byte)number;
                else if (field.Size == 2) BinaryPrimitives.WriteUInt16LittleEndian(encoded, (ushort)number);
                else BinaryPrimitives.WriteInt32LittleEndian(encoded, number);
                Store(expected[record.File], record.Offset + field.Offset, field, encoded);
            }
            document.SetGlobalDefinitionValues(changes); Exact(document, expected); Expect(document.GetHistory().Undo.Count == 1, "Every-field variant batch was not one operation.");
            document.SetGlobalDefinitionValues(changes); Expect(document.GetHistory().Undo.Count == 1, "Variant no-op consumed history."); document.Undo(); Exact(document, original); document.Redo(); Exact(document, expected);
            var exported = document.SaveCopy(Path.Combine(root, $"saved-{input.Count}-{input.Size}")); var reopened = NativeMapDocument.Load(exported);
            var reopenedRows = reopened.GlobalDefinitionRows().Where(row => row.Family == "Creatures").ToArray();
            foreach (var row in rows.Where(row => row.Editable))
                Expect(document.GlobalDefinitionValues(row).Select(value => value.Value).SequenceEqual(reopened.GlobalDefinitionValues(reopenedRows.Single(item => item.Index == row.Index)).Select(value => value.Value)), "Variant map-scoped save/reopen lost fields.");
            foreach (var section in GlobalDefinitionDefaults.Sections.Where(row => row.Family == "Creatures"))
            {
                var before = Snapshot(document); var resetExpected = before.ToDictionary(row => row.Key, row => row.Value.ToArray());
                foreach (var row in rows.Where(row => row.Editable)) foreach (var value in document.GlobalDefinitionValues(row).Where(value => value.Section == section.Name))
                {
                    var field = row.Record!.Fields.Single(item => item.Name == value.Name); var reference = defaults.AsSpan(Array.IndexOf(order, row.Index) * 5449, 5449);
                    var encoded = field.BitMask == 0 ? reference.Slice(field.Offset, field.Size).ToArray() : new byte[] { field.Read(reference) == "0" ? (byte)0 : (byte)1 };
                    Store(resetExpected[row.Record.File], row.Record.Offset + field.Offset, field, encoded);
                }
                document.ResetGlobalDefinitions(section.Name); Exact(document, resetExpected);
                var changed = before.Any(row => !row.Key.Data.Span.SequenceEqual(row.Value)); if (changed) { document.Undo(); Exact(document, before); }
            }
            document.Undo(); Exact(document, original);
            if (input.Offset > 0)
            {
                // Exercise the actual complete packed file, including other
                // sections before/after the extended creature catalogue.
                var packedDirectory = Path.Combine(root, $"packed-{input.Count}-{input.Size}");
                var packedPath = seed.SaveCopy(packedDirectory);
                var packedFilePath = Path.Combine(packedDirectory, "Data", "Editor", "Creatures.kwd");
                var packedBytes = File.ReadAllBytes(input.Path); File.WriteAllBytes(packedFilePath, packedBytes);
                var packed = NativeMapDocument.Load(packedPath); var packedOriginal = Snapshot(packed);
                var packedRows = packed.GlobalDefinitionRows().Where(row => row.Family == "Creatures").ToArray();
                Expect(packedRows.Count(row => row.Editable) == (input.Count == 44 ? 44 : 45)
                    && packed.Records().Any(row => row.Category.StartsWith("Definition.") && row.Category != "Definition.Creatures"), "Real packed container lost adjacent definition sections.");
                var packedRecord = packedRows.Single(row => row.Index == 0).Record!;
                var health = packedRecord.Fields.Single(field => field.Name == "Health");
                var packedExpected = packedOriginal.ToDictionary(row => row.Key, row => row.Value.ToArray());
                BinaryPrimitives.WriteUInt16LittleEndian(packedExpected[packedRecord.File].AsSpan(packedRecord.Offset + health.Offset), 999);
                packed.SetGlobalDefinitionValues([new("Creatures", 0, "Health", "999")]); Exact(packed, packedExpected);
                var packedSaved = packed.SaveCopy(Path.Combine(root, $"packed-saved-{input.Count}-{input.Size}"));
                var packedReopened = NativeMapDocument.Load(packedSaved);
                Expect(packedReopened.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.Index == 0).Record!.File.Data.Span.SequenceEqual(packedExpected[packedRecord.File]), "Packed save/reopen changed adjacent sections or opaque tails.");
                packed.Undo(); Exact(packed, packedOriginal);
                packedBytes[input.Offset + 56 + 0xF4B] = 99; File.WriteAllBytes(packedFilePath, packedBytes);
                var mixed = NativeMapDocument.Load(packedPath); var mixedOriginal = Snapshot(mixed);
                Expect(mixed.GlobalDefinitionRows().Where(row => row.Family == "Creatures").All(row => !row.Editable), "Packed invalid creature ID was guessed.");
                var known = mixed.GlobalDefinitionRows().First(row => row.Family != "Creatures" && row.Editable);
                var knownField = mixed.GlobalDefinitionValues(known).First(value => known.Record!.Fields.Single(field => field.Name == value.Name).BitMask == 0);
                Reject(() => mixed.SetGlobalDefinitionValues([new(known.Family, known.Index, knownField.Name, "1"), new("Creatures", 0, "Health", "1")])); Exact(mixed, mixedOriginal);
                mixed.SetGlobalDefinitionValues([new(known.Family, known.Index, knownField.Name, knownField.Value == "1" ? "2" : "1")]);
                Expect(!mixed.Files.All(file => file.Data.Span.SequenceEqual(mixedOriginal[file])), "Unknown packed creature section blocked a verified unrelated family.");
                mixed.Undo(); Exact(mixed, mixedOriginal);
            }
            // Wrong IDs, information versions, counts and adjacent sizes stay opaque.
            foreach (var defect in new[] { "id", "info", "count", "size" })
            {
                byte[] invalid;
                if (defect is "count" or "size")
                {
                    var count = defect == "count" ? 43 : input.Count; var size = defect == "size" ? 5538 : input.Size;
                    invalid = new byte[56 + count * size]; bytes.AsSpan(0, 56).CopyTo(invalid);
                    BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(8), (uint)invalid.Length); BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(20), (uint)count); BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(52), (uint)invalid.Length - 56);
                    for (var index = 0; index < count; index++) bytes.AsSpan(56 + index * input.Size, Math.Min(input.Size, size)).CopyTo(invalid.AsSpan(56 + index * size));
                }
                else { invalid = bytes.ToArray(); if (defect == "id") invalid[56 + 0xF4B] = 99; else BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(16), 29); }
                File.WriteAllBytes(creaturePath, invalid); var malformed = NativeMapDocument.Load(path); var snapshot = Snapshot(malformed);
                Expect(malformed.GlobalDefinitionRows().Where(row => row.Family == "Creatures").All(row => !row.Editable) && malformed.Records().Where(row => row.Category == "Definition.Creatures").All(row => row.Fields.Count == 0), "Malformed creature catalogue exposed inferred fields.");
                Reject(() => malformed.ResetGlobalDefinitions("Attributes")); Reject(() => malformed.SetGlobalDefinitionValues([new("Creatures", 0, "Health", "1")])); Exact(malformed, snapshot);
            }
        }
        Expect(sourceHashes.All(row => SHA256.HashData(File.ReadAllBytes(row.Key)).AsSpan().SequenceEqual(row.Value)), "Real creature fixture was changed.");
    }
}
