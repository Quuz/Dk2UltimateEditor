using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class PlayerSettingsChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes)
    { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Player edit changed unexpected bytes: " + file.ExportPath); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException) { return; }
        throw new Exception("Invalid player operation was accepted.");
    }
    private static long Number(string value) => value switch { "TRUE" => 1, "FALSE" => 0, _ when value.StartsWith("0x") => Convert.ToInt64(value[2..], 16), _ => long.Parse(value, CultureInfo.InvariantCulture) };
    // Read the preserved source independently of the embedded production parser.
    private static Dictionary<string, long>[] SourceProfiles()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        var source = File.ReadAllText(Path.Combine(root!.FullName, "Source", "Sources", "Properties", "Map", "Properties_Map_Global.c"));
        source = source[source.IndexOf("void Properties_MapOptionsKeeperAI(", StringComparison.Ordinal)..];
        return Enumerable.Range(0, 9).Select(profile =>
        {
            var body = Regex.Match(source, @"case\s+" + profile + @"\s*:(.*?)break\s*;", RegexOptions.Singleline).Groups[1].Value;
            var values = new Dictionary<string, long>();
            foreach (var line in body.Split('\n'))
            {
                var match = Regex.Match(line, @"MapOptions->([^\s=]+)\s*=\s*([^;]+);");
                if (match.Success) values.Add(match.Groups[1].Value, Number(match.Groups[2].Value.Trim()));
            }
            Expect(values.Count == 84 && values["PlayerAI"] == profile, "Source fixture did not verify all profile assignments."); return values;
        }).ToArray();
    }
    private static byte[] Encoded(NativeField field, long value)
    {
        var bytes = new byte[field.Size];
        switch (field.Kind)
        {
            case NativeValueKind.Byte: bytes[0] = unchecked((byte)value); break;
            case NativeValueKind.UInt16: case NativeValueKind.Int16: BinaryPrimitives.WriteUInt16LittleEndian(bytes, unchecked((ushort)value)); break;
            case NativeValueKind.UInt32: case NativeValueKind.Int32: BinaryPrimitives.WriteUInt32LittleEndian(bytes, unchecked((uint)value)); break;
            default: throw new Exception("Expected numeric player fixture field.");
        }
        return bytes;
    }
    private static byte[] DefaultBytes(uint player, Dictionary<string, long> profile)
    {
        var fields = LegacyProjectSchemas.Fields("MAPOPTIONS").ToDictionary(field => field.Name); var bytes = new byte[LegacyProjectSchemas.Size("MAPOPTIONS")];
        void Set(string field, long value) => Encoded(fields[field], value).CopyTo(bytes, fields[field].Offset);
        foreach (var (name, value) in profile) Set(name, value);
        Set("PlayerGold", 3000); Set("PlayerHealth", 100); Set("PlayerComputer", player <= 2 ? 1 : 0);
        Encoding.ASCII.GetBytes(player == 1 ? "Good Player" : player == 2 ? "Neutral Player" : "Keeper " + (player - 2)).CopyTo(bytes, fields["PlayerName"].Offset);
        foreach (var field in new[] { "PlayerCPUAreaX1", "PlayerCPUAreaY1", "PlayerCPUAreaX2", "PlayerCPUAreaY2", "PlayerCameraX", "PlayerCameraY" }) Set(field, -1);
        for (var index = 0; index < 45; index++) Set($"CreaturesCount[{index}]", player < 3 ? -1 : index == 13 || index == 14 ? 20 : index >= 1 && index <= 12 && index != 6 && index != 11 ? 32 : -1);
        if (player >= 3)
        {
            for (var index = 0; index < 15; index++) Set($"Rooms[{index}]", 3);
            for (var index = 0; index < 14; index++) Set($"Traps[{index}]", index < 11 ? 3 : 255);
            for (var index = 0; index < 8; index++) Set($"Doors[{index}]", index < 6 ? 3 : 255);
            for (var index = 0; index < 12; index++) Set($"Spells[{index}]", index == 0 || index == 1 || index == 11 ? 3 : 2);
        }
        return bytes;
    }
    private static string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    private static void Set(NativeMapDocument document, NativeRecord record, string name, string value) => document.SetField(record, record.Fields.Single(field => field.Name == name), value);
    private static void Expected(Dictionary<NativeFile, byte[]> bytes, NativeRecord record, string name, long value)
    { var field = record.Fields.Single(field => field.Name == name); Encoded(field, value).CopyTo(bytes[record.File], record.Offset + field.Offset); }
    public static void Run()
    {
        var profiles = SourceProfiles();
        Expect(PlayerSettings.AIProfiles.Count == 9 && PlayerSettings.AIProfiles.Select(choice => choice.Value).SequenceEqual(Enumerable.Range(0, 9).Select(value => value.ToString())), "Nine bilingual profiles are missing/reordered.");
        for (var profile = 0; profile < 9; profile++)
            Expect(LegacyCompilerRules.PlayerAIProfile(profile).All(item => profiles[profile][item.Target] == long.Parse(item.Source, CultureInfo.InvariantCulture)), "Embedded AI profile differs from preserved source.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2-PlayerChecks-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var legacy in new[] { true, false })
            {
                var document = NativeMapDocument.CreateNewMap(new("Players", "Players", 38, 20, Players: 5, PlaceHearts: true, NativeFormat: !legacy));
                var rows = document.PlayerSettingsRows(); var before = Snapshot(document);
                Expect(rows.Count == 7 && rows.All(row => row.Editable && row.ProfileEditable && row.Resettable), "Current player layouts were disabled.");
                Expect(rows.Select(row => row.Name).SequenceEqual(new[] { "Good Player", "Neutral Player", "Keeper 1", "Keeper 2", "Keeper 3", "Keeper 4", "Keeper 5" }), "New-map names differ from source constants.");
                Exact(document, before);
                var owner = document.ScriptOwners().Single(owner => owner.Scope == 1 && owner.Record.File == rows[2].Record.File && owner.Record.Offset == rows[2].Record.Offset);
                var root = document.EnsureScriptScope(owner); document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.First(type => type.RecordSignature == 0xD5 && (LegacyEventSchemas.AllowedScopes(type) & 1) != 0));
                rows = document.PlayerSettingsRows();
                // Imported opaque bytes must survive profile and full-row reset.
                document.Edit("Opaque player fixture", () =>
                {
                    foreach (var row in rows)
                    {
                        var known = new bool[row.Record.Length]; foreach (var field in row.Record.Fields) Array.Fill(known, true, field.Offset, field.Size);
                        Expect(MemoryMarshal.TryGetArray(row.Record.File.Data, out var array), "Fixture player buffer missing.");
                        for (var index = 0; index < known.Length; index++) if (!known[index]) array.Array![array.Offset + row.Record.Offset + index] = 0xAB;
                    }
                });
                document.SetPlayerSettings(rows.Select(row => new PlayerSettingsChange(row.ID, "José " + row.ID, (int)row.ID * 100, !row.Computer, row.Health is null ? null : 80)));
                Expect(document.PlayerSettingsRows().All(row => row.Name == "José " + row.ID && row.Gold == row.ID * 100 && (row.Health is null || row.Health == 80)), "Player CP1252/resources/heart updates failed.");
                before = Snapshot(document); var notifications = 0; document.Changed += (_, _) => notifications++;
                for (var profile = 0; profile < 9; profile++)
                {
                    rows = document.PlayerSettingsRows(); var expected = Snapshot(document);
                    foreach (var row in rows)
                        if (legacy) Expected(expected, row.Record, "PlayerAI", profile);
                        else Expected(expected, row.Record, "bPlayerAI", profile);
                    document.SetPlayerAIType(rows.Select(row => row.ID), profile); Exact(document, expected);
                    var typeBefore = Snapshot(document); expected = Snapshot(document);
                    foreach (var row in rows) foreach (var (name, value) in profiles[profile])
                        if (legacy) Expected(expected, row.Record, name, value);
                        else foreach (var mapping in LegacyCompilerRules.Players.Where(mapping => mapping.Source == name)) Expected(expected, row.Record, mapping.Target, value);
                    var count = notifications; document.ApplyPlayerAIProfile(rows.Select(row => row.ID), profile); Exact(document, expected);
                    Expect(notifications == count + 1 || expected.All(pair => pair.Value.AsSpan().SequenceEqual(typeBefore[pair.Key])) && notifications == count, "AI batch history was not atomic.");
                    var after = Snapshot(document); count = notifications; document.ApplyPlayerAIProfile(rows.Select(row => row.ID), profile); Exact(document, after); Expect(notifications == count, "Repeated AI profile created no-op history.");
                    if (profile > 0) { document.Undo(); Exact(document, typeBefore); document.Redo(); Exact(document, after); }
                }
                before = Snapshot(document); var ruleValues = document.MapRuleCells().ToDictionary(cell => cell.Key, cell => (cell.Value, cell.RecordCount));
                // Reset each player independently, then undo. Other owners,
                // global modifiers, alliances, variables and graphs stay intact.
                for (uint player = 1; player <= 7; player++)
                {
                    var row = document.PlayerSettingsRows().Single(row => row.ID == player); var expected = Snapshot(document); var defaults = DefaultBytes(player, profiles[0]);
                    var source = LegacyProjectSchemas.Fields("MAPOPTIONS").ToDictionary(field => field.Name);
                    if (legacy)
                    {
                        foreach (var field in row.Record.Fields) defaults.AsSpan(source[field.Name].Offset, field.Size).CopyTo(expected[row.Record.File].AsSpan(row.Record.Offset + field.Offset));
                    }
                    else
                    {
                        foreach (var mapping in LegacyCompilerRules.Players) Expected(expected, row.Record, mapping.Target, long.Parse(source[mapping.Source].Read(defaults), CultureInfo.InvariantCulture));
                        var name = row.Record.Fields.Single(field => field.Name == "szPlayerName"); Array.Clear(expected[row.Record.File], row.Record.Offset + name.Offset, name.Size);
                        Encoding.ASCII.GetBytes(source["PlayerName"].Read(defaults)).CopyTo(expected[row.Record.File], row.Record.Offset + name.Offset);
                        Expected(expected, row.Record, "bPlayerCameraX", player < 3 ? 0 : 3 + (player - 3) * 6); Expected(expected, row.Record, "bPlayerCameraY", player < 3 ? 0 : 3);
                        Expected(expected, row.Record, "bPlayerBuildAreaX1", 2); Expected(expected, row.Record, "bPlayerBuildAreaY1", 2); Expected(expected, row.Record, "bPlayerBuildAreaX2", 36); Expected(expected, row.Record, "bPlayerBuildAreaY2", 18);
                        var heart = document.Records().LastOrDefault(record => record.Category == "GAMEGATE" && Read(record, "bGateType") == "14" && Read(record, "bGlobalPlayer") == player.ToString());
                        if (heart is not null) Expected(expected, heart, "wGateHealth", 100);
                    }
                    var count = notifications; document.ResetPlayerSettings([player]); Expect(notifications == count + 1, "Full player reset did not form one transaction.");
                    foreach (var file in document.Files.Where(file => legacy || file.Signature != 0xE6)) Expect(file.Data.Span.SequenceEqual(expected[file]), "Full reset modified padding, identities, scripts or another player.");
                    foreach (var cell in document.MapRuleCells())
                    {
                        if (cell.Key.Player != player || cell.Key.Kind is MapRuleKind.Alliance or MapRuleKind.CreatureModifier) Expect((cell.Value, cell.RecordCount) == ruleValues[cell.Key], "Full reset changed another player/alliance/modifier.");
                        else
                        {
                            var index = MapRules.Elements(cell.Key.Kind).Single(element => element.ID == cell.Key.Element).Index;
                            var field = source[cell.Key.Kind == MapRuleKind.CreaturePool ? $"CreaturesCount[{index}]" : $"{cell.Key.Kind}[{index}]"];
                            var value = long.Parse(field.Read(defaults), CultureInfo.InvariantCulture);
                            Expect(cell.Value == (value == (cell.Key.Kind == MapRuleKind.CreaturePool ? -1 : 255) ? null : value), "Full reset did not restore exact NPC/keeper source defaults.");
                        }
                    }
                    var after = Snapshot(document); count = notifications; document.ResetPlayerSettings([player]); Exact(document, after); Expect(notifications == count, "Repeated full player reset created history.");
                    document.Undo(); Exact(document, before); document.Redo(); Exact(document, after); document.Undo(); Exact(document, before);
                }
                Reject(() => document.ApplyPlayerAIProfile([3, 8], 1)); Reject(() => document.ApplyPlayerAIProfile([3, 3], 1)); Reject(() => document.ApplyPlayerAIProfile([3], 9));
                Reject(() => document.SetPlayerAIType([3], -1)); Reject(() => document.ResetPlayerSettings([3, 8])); Reject(() => document.ResetPlayerSettings([3, 3]));
                foreach (var change in new[] { new PlayerSettingsChange(3, "Valid", -1, true, 100), new(3, "Valid", 1000000, true, 100), new(3, "Valid", 100, true, 101), new(3, new string('a', 32), 100, true, 100), new(3, "Имя", 100, true, 100), new(3, "a\0b", 100, true, 100) })
                    Reject(() => document.SetPlayerSettings([new(7, "No partial", 999999, true, 0), change]));
                if (!legacy) Reject(() => document.SetPlayerSettings([new(1, "NPC", 10, true, 100)])); Exact(document, before);
                document.ResetPlayerSettings(Enumerable.Range(1, 7).Select(id => (uint)id)); var allReset = Snapshot(document); document.Undo(); Exact(document, before); document.Redo(); Exact(document, allReset);
                var saved = document.SaveCopy(Path.Combine(directory, "reset-" + legacy)); var reopened = NativeMapDocument.Load(saved);
                Expect(document.PlayerSettingsRows().Select(row => (row.ID, row.Name, row.Gold, row.Computer, row.Health, row.AI)).SequenceEqual(reopened.PlayerSettingsRows().Select(row => (row.ID, row.Name, row.Gold, row.Computer, row.Health, row.AI))), "Player values changed after saving/reopening.");
                if (legacy)
                {
                    var compiled = document.CompileLegacyProject("ResetCompiled", NativeMapDocument.Create(38, 20, "Basis"));
                    Expect(compiled.PlayerSettingsRows().All(row => row.Gold == 3000 && row.AI == 0 && (row.Health is null || row.Health == 100)), "Reset defaults changed during native compilation."); Exact(document, allReset);
                    CheckHistorical(document, directory);
                }
                else CheckNativeGuards(document, directory);
            }
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void CheckHistorical(NativeMapDocument document, string directory)
    {
        var path = document.SaveCopy(Path.Combine(directory, "historical-player-basis")); var sections = LegacySectionFile.Read(path);
        foreach (var version in new[] { 1, 2, 99 })
        {
            var stride = LegacyProjectSchemas.Size(version == 1 ? "MAPOPTIONSV1" : "MAPOPTIONSV2");
            var original = sections.Sections.Single(section => section.Signature == NativeMapDocument.LegacyPlayers);
            var payload = version == 1 ? new byte[7 * stride] : original.Payload.ToArray();
            var file = Path.Combine(directory, "players-v" + version + ".dkm"); File.WriteAllBytes(file, LegacySectionFile.Create(sections.Sections.Select(section => section.Signature == original.Signature ? new LegacySection(original.Signature, version, payload) : section)).ToArray());
            var old = NativeMapDocument.Load(file); var before = Snapshot(old);
            Expect(old.PlayerSettingsRows().All(row => !row.Resettable), "Historical ignored/unknown fields were treated as current full-row defaults.");
            Reject(() => old.ResetPlayerSettings([3])); Exact(old, before);
            if (version == 2)
            { Expect(old.PlayerSettingsRows().All(row => row.ProfileEditable), "Verified V2 AI fields were disabled."); old.ApplyPlayerAIProfile([3], 8); Expect(old.PlayerSettingsRows()[2].AI == 8, "V2 profile layout incorrect."); old.Undo(); Exact(old, before); }
            else Reject(() => old.ApplyPlayerAIProfile([3], 8));
            if (version != 99) { old.UpgradeLegacyProject(); old.ResetPlayerSettings([3]); Expect(old.PlayerSettingsRows()[2] is { Resettable: true, Gold: 3000, AI: 0 }, "Explicit upgrade did not enable full source reset."); }
        }
    }
    private static void CheckNativeGuards(NativeMapDocument document, string directory)
    {
        var nativeBefore = Snapshot(document);
        foreach (var signature in new uint[] { 0xB4, 0xBE, 0xE6 })
        {
            var file = document.Files.Single(file => file.Signature == signature);
            document.Edit("Future native player fixture", () =>
            {
                Expect(MemoryMarshal.TryGetArray(file.Data, out var array), "Native version fixture buffer missing.");
                BinaryPrimitives.WriteUInt32LittleEndian(array.Array!.AsSpan(array.Offset + 4), 99);
            });
            var beforeVersion = Snapshot(document); var row = document.PlayerSettingsRows().Single(row => row.ID == 3);
            if (signature == 0xB4)
            { Expect(!row.Editable && !row.ProfileEditable && !row.Resettable, "Unverified native player version was editable."); Reject(() => document.ApplyPlayerAIProfile([3], 1)); Reject(() => document.SetPlayerSettings([new(3, "Future", 1, true, null)])); }
            if (signature == 0xBE)
            { Expect(row.Health is null, "Unverified native heart version exposed guessed health."); Reject(() => document.SetPlayerSettings([new(3, "Future", 1, true, 1)])); }
            if (signature == 0xE6) { Expect(!row.Resettable && row.ProfileEditable, "Future rule version enabled full reset or blocked unrelated AI."); Reject(() => document.ResetPlayerSettings([3])); }
            Exact(document, beforeVersion); document.Undo(); Exact(document, nativeBefore);
        }
        var firstHeart = document.Records().Single(record => record.Category == "GAMEGATE" && Read(record, "bGateType") == "14" && Read(record, "bGlobalPlayer") == "3");
        var lastHeart = document.DuplicateRecord(firstHeart); Set(document, firstHeart, "wGateHealth", "5"); Set(document, lastHeart, "wGateHealth", "20");
        var heartBefore = Snapshot(document); Expect(document.PlayerSettingsRows().Single(row => row.ID == 3).Health == 20, "Native health projection is not source last-wins.");
        document.SetPlayerSettings([new(3, "Heart test", 500, true, 40)]);
        Expect(Read(firstHeart, "wGateHealth") == "5" && Read(lastHeart, "wGateHealth") == "40", "General health edit changed earlier duplicate heart."); document.Undo(); Exact(document, heartBefore);
        document.ResetPlayerSettings([3]); Expect(Read(firstHeart, "wGateHealth") == "5" && Read(lastHeart, "wGateHealth") == "100", "Player reset normalized an earlier duplicate heart."); document.Undo(); Exact(document, heartBefore);
        document.Undo(); document.Undo(); document.Undo(); // Remove deliberate duplicate and its two health fixtures.
        var player = document.PlayerSettingsRows().Single(row => row.ID == 7).Record;
        Set(document, player, "bGlobalPlayer", "5"); var before = Snapshot(document);
        Reject(() => document.ApplyPlayerAIProfile([3, 5], 1)); Reject(() => document.ResetPlayerSettings([3, 5])); Reject(() => document.SetPlayerSettings([new(5, "Collision", 1, true, 1)])); Exact(document, before); document.Undo();
        // Noncontiguous identities are matched by ID, never by array position.
        document.DeleteRecord(document.PlayerSettingsRows().Single(row => row.ID == 4).Record);
        before = Snapshot(document); document.ApplyPlayerAIProfile([7], 8); Expect(document.PlayerSettingsRows().Single(row => row.ID == 7).AI == 8 && document.PlayerSettingsRows().Single(row => row.ID == 5).AI == 0, "Noncontiguous keeper identity shifted profile target."); document.Undo(); Exact(document, before);
        var gameRoot = Path.Combine(directory, "missing-variable-player-basis"); var saved = document.SaveCopy(gameRoot);
        var sharedBytes = document.Files.Single(file => file.Signature == 0xE6).Data.ToArray();
        var sharedPath = Path.Combine(gameRoot, "Data", "Editor", "GlobalVariables.kwd"); File.WriteAllBytes(sharedPath, sharedBytes);
        var sharedDocument = NativeMapDocument.Load(saved); sharedDocument.SetPlayerSettings([new(3, "Shared reset", 222, true, 20)]); before = Snapshot(sharedDocument);
        sharedDocument.ResetPlayerSettings([3]);
        Expect(sharedDocument.Files.Single(file => Path.GetFileName(file.ExportPath) == "GlobalVariables.kwd").Data.Span.SequenceEqual(sharedBytes), "Complete player reset modified shared game variables.");
        sharedDocument.Undo(); Exact(sharedDocument, before);
        var variablesPath = Path.Combine(Path.GetDirectoryName(saved)!, "PlayersVariables.kld"); File.Move(variablesPath, variablesPath + ".unreferenced");
        var missing = NativeMapDocument.Load(saved); before = Snapshot(missing);
        Expect(missing.PlayerSettingsRows().All(row => !row.Resettable && row.ProfileEditable), "Missing rules collection disabled unrelated AI or enabled full reset.");
        Reject(() => missing.ResetPlayerSettings([3])); Exact(missing, before); missing.ApplyPlayerAIProfile([3], 1); missing.Undo(); Exact(missing, before);
    }
}
