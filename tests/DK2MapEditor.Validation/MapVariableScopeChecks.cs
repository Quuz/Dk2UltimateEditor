using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using DK2MapEditor.Core.Editing;

internal static class MapVariableScopeChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes)
    { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Variable scope changed unexpected bytes: " + file.ExportPath); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return; }
        throw new Exception("Missing map collection was edited as shared game data.");
    }
    private static long RuleValue(MapRuleKey key) => key.Kind == MapRuleKind.Alliance ? 1 : MapRules.IsAvailability(key.Kind) ? 2 : key.Kind == MapRuleKind.CreaturePool ? 17 : 4321;
    private static object[] Rules(NativeMapDocument document) => document.MapRuleCells().Select(cell => (object)(cell.Key, cell.Value, cell.RecordCount, cell.Editable)).ToArray();
    private static object[] Variables(NativeMapDocument document) => document.MapVariableCells().Select(cell => (object)(cell.Definition.ID, cell.Bits, cell.RecordCount, cell.Editable, cell.Resettable)).ToArray();
    private static NativeFile Shared(NativeMapDocument document) => document.Files.Single(file => Path.GetFileName(file.ExportPath).Equals("GlobalVariables.kwd", StringComparison.OrdinalIgnoreCase));
    private static NativeFile MapFile(NativeMapDocument document) => document.Files.Single(file => file.Signature == 0xE6 && file != Shared(document));
    private static int Index(NativeMapDocument document, NativeFile file) => Array.IndexOf(document.Files.ToArray(), file);

    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2-VariableScope-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            // Shared fixtures contain every editable canonical key, all four
            // typed rule layouts and self-alliances. These are not map values.
            var seed = NativeMapDocument.CreateNewMap(new("Seed", "Seed", 38, 20, Players: 5, NativeFormat: true));
            seed.SetMapRules(seed.MapRuleCells().Where(cell => cell.Editable).Select(cell => new MapRuleChange(cell.Key, RuleValue(cell.Key))));
            seed.SetMapVariables(seed.MapVariableCells().Where(cell => cell.Editable).Select(cell => new MapVariableChange(cell.Definition.ID, cell.Definition.Minimum)));
            for (var player = 1; player <= 7; player++)
            {
                var record = seed.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEVARALLIANCE"), new(0, 0));
                foreach (var name in new[] { "dwGlobalPlayer", "dwAllieWith" }) seed.SetField(record, record.Fields.Single(field => field.Name == name), player.ToString(CultureInfo.InvariantCulture));
            }
            var sharedBytes = seed.Files.Single(file => file.Signature == 0xE6).Data.ToArray();
            var baseline = NativeMapDocument.CreateNewMap(new("Scope", "Scope", 38, 20, Players: 5, NativeFormat: true));
            var source = Snapshot(baseline);
            var gameRoot = Path.Combine(directory, "basis"); var path = baseline.SaveCopy(gameRoot);
            var sharedPath = Path.Combine(gameRoot, "Data", "Editor", "GlobalVariables.kwd"); File.WriteAllBytes(sharedPath, sharedBytes);
            var loaded = NativeMapDocument.Load(path);
            Expect(Index(loaded, Shared(loaded)) < Index(loaded, MapFile(loaded)), "Fixture did not put the shared reference first.");
            CheckIsolation(loaded, baseline, sharedBytes, directory, "shared-first");
            var inherited = NativeMapDocument.CreateNewMap(new("Scope", "Scope", 38, 20, Players: 5, NativeFormat: true), loaded);
            Expect(Index(inherited, Shared(inherited)) > Index(inherited, MapFile(inherited)), "Factory fixture did not put the shared reference last.");
            CheckIsolation(inherited, baseline, sharedBytes, directory, "shared-last");

            // A verified map collection can have a custom descriptor path.
            // Its shared counterpart must not become the insertion fallback.
            var descriptor = File.ReadAllBytes(path);
            var referenceStart = 56 + (int)BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(52)) + 8;
            var count = (int)(BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(20)) & 0xFFFF);
            var renamed = false;
            for (var index = 0; index < count; index++)
            {
                var offset = referenceStart + index * 72 + 8;
                var reference = Encoding.ASCII.GetString(descriptor, offset, 64).Split('\0')[0];
                if (!reference.EndsWith("ScopeVariables.kld", StringComparison.OrdinalIgnoreCase)) continue;
                Array.Clear(descriptor, offset, 64); Encoding.ASCII.GetBytes("Data\\Editor\\Overrides\\ScopeVariables.kld").CopyTo(descriptor, offset); renamed = true;
            }
            Expect(renamed, "Custom collection fixture did not find its reference.");
            var originalMapPath = Path.Combine(gameRoot, "Data", "Editor", "Maps", "ScopeVariables.kld");
            var customMapPath = Path.Combine(gameRoot, "Data", "Editor", "Overrides", "ScopeVariables.kld");
            Directory.CreateDirectory(Path.GetDirectoryName(customMapPath)!); File.Move(originalMapPath, customMapPath);
            File.WriteAllBytes(path, descriptor);
            var custom = NativeMapDocument.Load(path);
            CheckIsolation(custom, baseline, sharedBytes, directory, "custom-path");
            // Reverse these two references as well, so the shared collection is
            // last and the generic AddRecord fallback would select it.
            var sharedIndex = -1; var mapIndex = -1;
            for (var index = 0; index < count; index++)
            {
                var reference = Encoding.ASCII.GetString(descriptor, referenceStart + index * 72 + 8, 64).Split('\0')[0];
                if (reference.EndsWith("GlobalVariables.kwd", StringComparison.OrdinalIgnoreCase)) sharedIndex = index;
                if (reference.EndsWith("ScopeVariables.kld", StringComparison.OrdinalIgnoreCase)) mapIndex = index;
            }
            var entry = descriptor.AsSpan(referenceStart + sharedIndex * 72, 72).ToArray();
            descriptor.AsSpan(referenceStart + mapIndex * 72, 72).CopyTo(descriptor.AsSpan(referenceStart + sharedIndex * 72, 72));
            entry.CopyTo(descriptor, referenceStart + mapIndex * 72); File.WriteAllBytes(path, descriptor);
            custom = NativeMapDocument.Load(path);
            Expect(Index(custom, Shared(custom)) > Index(custom, MapFile(custom)), "Custom fixture did not put shared data last.");
            CheckIsolation(custom, baseline, sharedBytes, directory, "custom-shared-last");

            File.Move(customMapPath, customMapPath + ".unreferenced");
            var missing = NativeMapDocument.Load(path); var before = Snapshot(missing); var notifications = 0; missing.Changed += (_, _) => notifications++;
            Expect(missing.Files.Count(file => file.Signature == 0xE6) == 1, "Missing-map fixture still loaded a map variable file.");
            Expect(missing.MapRuleCells().All(cell => !cell.Editable && cell.RecordCount == 0 && cell.Value == (cell.Key.Kind == MapRuleKind.Alliance ? 0 : null)), "Missing collection inferred editable map rules from shared data.");
            Expect(missing.MapVariableCells().All(cell => !cell.Editable && !cell.Resettable && cell.RecordCount == 0 && cell.Bits is null), "Missing collection inferred editable map variables.");
            foreach (var cell in baseline.MapRuleCells().Where(cell => cell.Editable))
            {
                Reject(() => missing.SetMapRules([new(cell.Key, RuleValue(cell.Key))]));
                Reject(() => missing.SetMapRules([new(cell.Key, null)]));
            }
            Reject(() => missing.ResetMapRules(MapRules.Keys()));
            Reject(() => missing.ResetMapRules([new(MapRuleKind.Alliance, 3, 3)]));
            Reject(() => missing.SetMapVariables([new(4, 1)])); Reject(() => missing.ResetMapVariables([4])); Reject(() => missing.ClearMapVariableOverrides([4]));
            Exact(missing, before); Expect(notifications == 0 && !missing.CanUndo, "Rejected missing-collection edits created history/notifications.");
            Expect(File.ReadAllBytes(sharedPath).AsSpan().SequenceEqual(sharedBytes), "Shared source file was overwritten."); Exact(baseline, source);
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void CheckIsolation(NativeMapDocument document, NativeMapDocument baseline, byte[] sharedBytes, string directory, string name)
    {
        var before = Snapshot(document); var shared = Shared(document); var map = MapFile(document);
        Expect(Rules(document).SequenceEqual(Rules(baseline)), "Shared game records changed the map-rule projection: " + name);
        Expect(Variables(document).SequenceEqual(Variables(baseline)), "Shared game records changed the map-variable projection: " + name); Exact(document, before);
        var rules = document.MapRuleCells().Where(cell => cell.Editable).Select(cell => new MapRuleChange(cell.Key, RuleValue(cell.Key))).ToArray();
        var variables = document.MapVariableCells().Where(cell => cell.Editable).Select(cell => new MapVariableChange(cell.Definition.ID, cell.Definition.Maximum)).ToArray();
        var notifications = 0; document.Changed += (_, _) => notifications++;
        document.SetMapRules(rules); Expect(notifications == 1, "All map rules did not form one scoped transaction.");
        var ruleCells = document.MapRuleCells().ToDictionary(cell => cell.Key);
        foreach (var change in rules) Expect(ruleCells[change.Key] is { RecordCount: 1 } cell && cell.Value == change.Value, "Rule update/insertion did not target map collection.");
        document.SetMapVariables(variables); Expect(notifications == 2, "All map variables did not form one scoped transaction.");
        var variableCells = document.MapVariableCells().ToDictionary(cell => cell.Definition.ID);
        foreach (var change in variables) Expect(variableCells[change.ID] is { RecordCount: 1 } cell && cell.Bits == unchecked((uint)change.Value), "Variable insertion did not target map collection.");
        foreach (var file in document.Files.Where(file => file != map)) Expect(file.Data.Span.SequenceEqual(before[file]), "Scoped edit changed a shared/unrelated buffer.");
        var after = Snapshot(document); document.Undo(); document.Undo(); Exact(document, before); document.Redo(); document.Redo(); Exact(document, after);
        document.SetMapRules(rules.Select(change => change with { Value = null }));
        document.ClearMapVariableOverrides(variables.Select(change => change.ID));
        Expect(document.MapRuleCells().Where(cell => cell.Editable).All(cell => cell.RecordCount == 0), "Cleared rules inherited shared entries.");
        Expect(document.MapVariableCells().Where(cell => cell.Editable).All(cell => cell.RecordCount == 0 && cell.Bits is null), "Cleared variables inherited shared entries.");
        Expect(shared.Data.Span.SequenceEqual(sharedBytes), "Clear changed shared rules/variables.");
        var cleared = Snapshot(document); document.ResetMapRules(MapRules.Keys());
        var baselineRules = baseline.MapRuleCells().ToDictionary(cell => cell.Key);
        foreach (var cell in document.MapRuleCells().Where(cell => cell.Editable))
            Expect(MapRules.IsAvailability(cell.Key.Kind) && cell.Key.Player <= 2 ? cell.Value is null && cell.RecordCount == 0 : cell.Value == baselineRules[cell.Key].Value,
                "Reset did not restore source map defaults/retain cleared NPC availability independently of shared data.");
        Expect(shared.Data.Span.SequenceEqual(sharedBytes), "Reset changed shared rules or diagonal alliances."); document.Undo(); Exact(document, cleared);
        document.Undo(); document.Undo(); Exact(document, after);
        var saved = document.SaveCopy(Path.Combine(directory, name)); var reopened = NativeMapDocument.Load(saved);
        Expect(Rules(reopened).SequenceEqual(Rules(document)) && Variables(reopened).SequenceEqual(Variables(document)), "Scoped values changed after export/reopening.");
        Expect(Shared(reopened).Data.Span.SequenceEqual(sharedBytes), "Export rewrote/scoped untouched shared variable file.");
        Exact(document, after);
    }
}
