using System.Globalization;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class MapRuleChecks
{
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    private static void Set(NativeMapDocument document, NativeRecord record, string name, string value) => document.SetField(record, record.Fields.Single(field => field.Name == name), value);
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument document) => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(NativeMapDocument document, Dictionary<NativeFile, byte[]> bytes)
    { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Map rules changed unexpected bytes: " + file.ExportPath); }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException) { return; }
        throw new Exception("Unsafe map-rule edit was accepted.");
    }
    private static MapRuleKey Key(MapRuleKind kind, int index = 0, uint player = 3, uint parameter = 0) => new(kind, kind == MapRuleKind.CreatureModifier ? 0 : player, MapRules.Elements(kind)[index].ID, parameter);
    private static MapRuleCell Cell(NativeMapDocument document, MapRuleKey key) => document.MapRuleCells().Single(cell => cell.Key == key);
    private static long Value(MapRuleKey key) => key.Kind == MapRuleKind.Alliance ? 1 : MapRules.IsAvailability(key.Kind) ? 1 + (key.Element + key.Player) % 4 : key.Kind == MapRuleKind.CreaturePool ? (key.Element + key.Player) % 1000 : 65535 - key.Element - key.Parameter;
    private static long? SourceDefault(MapRuleKey key)
    {
        if (key.Kind == MapRuleKind.Alliance) return key.Player != key.Element && (key.Player == 2 || key.Element == 2) ? 1 : 0;
        if (key.Kind == MapRuleKind.CreatureModifier) return null;
        if (key.Player < 3) return null;
        if (key.Kind == MapRuleKind.CreaturePool)
            return key.Element is 23 or 45 ? 20 : key.Element is 7 or 5 or 12 or 10 or 6 or 4 or 11 or 24 or 3 or 22 ? 32 : null;
        if (key.Kind == MapRuleKind.Traps && key.Element is 14 or 15 or 16 || key.Kind == MapRuleKind.Doors && key.Element is 7 or 8) return null;
        if (key.Kind == MapRuleKind.Spells && key.Element is not (4 or 2 or 5)) return 2;
        return 3;
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DK2-RuleChecks-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var keys = MapRules.Keys().ToArray();
            Expect(keys.Length == 866 && keys.Distinct().Count() == 866, "Original map-rule matrix dimensions/catalogues are incomplete.");
            Expect(MapRules.Elements(MapRuleKind.Rooms).First().ID == 2 && MapRules.Elements(MapRuleKind.Rooms).First().Name.Value == 0x0B000101u.ToString() && MapRules.Elements(MapRuleKind.Rooms).First().Name.English == "Lair", "Room variable ID confused with packed terrain ID.");
            Expect(MapRules.Availability.Select(choice => choice.Value).SequenceEqual(new[] { "255", "1", "2", "3", "4" }) && MapRules.Availability.Last().English == "Locked" && MapRules.Availability.Single(choice => choice.Value == "1").English == "Unavailable", "Original five radio states were mislabelled.");
            foreach (var legacy in new[] { false, true })
            {
                var document = NativeMapDocument.CreateNewMap(new("RuleTest", "RuleTest", 38, 20, Players: 5, PlaceHearts: true, NativeFormat: !legacy));
                var before = Snapshot(document); var notifications = 0; document.Changed += (_, _) => notifications++;
                var cells = document.MapRuleCells(); Exact(document, before);
                Expect(cells.Count == 866 && cells.Count(cell => cell.Editable) == 807, "Matrix lost cells or elite/diagonal guards.");
                var editable = cells.Where(cell => cell.Editable).Select(cell => cell.Key).ToArray();
                var expected = before.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
                if (legacy)
                {
                    var info = document.Records().Single(record => record.File.Signature == NativeMapDocument.LegacyInfo);
                    var players = document.Records().Where(record => record.File.Signature == NativeMapDocument.LegacyPlayers).ToArray();
                    void Encode(NativeRecord record, string name, long value)
                    { var field = record.Fields.Single(field => field.Name == name); field.Encode(value.ToString(CultureInfo.InvariantCulture)).CopyTo(expected[record.File], record.Offset + field.Offset); }
                    foreach (var key in editable)
                    {
                        if (key.Kind == MapRuleKind.Alliance) { Encode(info, $"Allies[{key.Player - 1}][{key.Element - 1}]", 1); Encode(info, $"Allies[{key.Element - 1}][{key.Player - 1}]", 1); }
                        else
                        {
                            var index = MapRules.Elements(key.Kind).Single(item => item.ID == key.Element).Index;
                            if (key.Kind == MapRuleKind.CreatureModifier) Encode(info, $"CreaturesParams[{index}][{key.Parameter - 1}]", Value(key));
                            else Encode(players[key.Player - 1], key.Kind == MapRuleKind.CreaturePool ? $"CreaturesCount[{index}]" : $"{key.Kind}[{index}]", Value(key));
                        }
                    }
                }
                var count = notifications; document.SetMapRules(editable.Select(key => new MapRuleChange(key, Value(key))));
                Expect(notifications == count + 1, "Bulk rules edit did not use one transaction.");
                if (legacy) Exact(document, expected);
                else foreach (var file in document.Files.Where(file => file.Signature != 0xE6)) Expect(file.Data.Span.SequenceEqual(before[file]), "Native rules edit modified terrain/things/players/scripts.");
                foreach (var cell in document.MapRuleCells().Where(cell => cell.Editable)) Expect(cell.Value == Value(cell.Key), "Wrong map-rule value/catalogue binding: " + cell.Key);
                var after = Snapshot(document); document.Undo(); Exact(document, before); document.Redo(); Exact(document, after);
                count = notifications; document.SetMapRules(editable.Select(key => new MapRuleChange(key, Value(key)))); Exact(document, after);
                Expect(notifications == count, "Repeated values created no-op history.");
                var invalid = new MapRuleKey(MapRuleKind.Rooms, 8, 2);
                Reject(() => document.SetMapRules([new(editable[0], 2), new(invalid, 2)])); Exact(document, after);
                Reject(() => document.ResetMapRules([editable[0], invalid])); Exact(document, after);
                Reject(() => document.SetMapRules([new(editable[0], 2), new(editable[0], 3)])); Exact(document, after);
                Reject(() => document.SetMapRules([new(Key(MapRuleKind.CreatureModifier, 16, parameter: 1), 10)]));
                Reject(() => document.SetMapRules([new(new(MapRuleKind.Alliance, 3, 3), 1)]));
                foreach (var kind in new[] { MapRuleKind.Rooms, MapRuleKind.CreaturePool, MapRuleKind.CreatureModifier })
                {
                    var key = Key(kind, parameter: kind == MapRuleKind.CreatureModifier ? 1u : 0);
                    Reject(() => document.SetMapRules([new(key, -1)])); Reject(() => document.SetMapRules([new(key, kind == MapRuleKind.Rooms ? 5 : kind == MapRuleKind.CreaturePool ? 1000 : 65536)]));
                }
                Exact(document, after);
                var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory, "rules-" + legacy)); var reopened = NativeMapDocument.Load(saved);
                Expect(reopened.MapRuleCells().Select(cell => (cell.Key, cell.Value)).SequenceEqual(document.MapRuleCells().Select(cell => (cell.Key, cell.Value))), "Matrix values changed after saving/reopening.");
                var repeated = SaveProjectionChecks.SaveCopy(reopened, Path.Combine(directory, "rules-repeat-" + legacy));
                if (legacy) Expect(SaveProjectionChecks.BeforeEquals(reopened,File.ReadAllBytes(saved)), "Project matrix save/re-export not byte exact.");
                else foreach (var file in reopened.Files) Expect(file.Data.Span.SequenceEqual(document.Files.Single(original => original.ExportPath == file.ExportPath).Data.Span), "Native matrix save/reopen not byte exact.");
                Exact(document, after);
                if (legacy)
                {
                    var compiled = document.CompileLegacyProject("RulesCompiled", NativeMapDocument.Create(38, 20, "Basis"));
                    foreach (var cell in compiled.MapRuleCells().Where(cell => cell.Editable && (cell.Key.Kind is MapRuleKind.CreatureModifier or MapRuleKind.Alliance || cell.Key.Player >= 3)))
                        Expect(cell.Value == Value(cell.Key), "Compiled rules changed variable ID, player or parameter: " + cell.Key);
                    Exact(document, after);
                }
                // Clear all exact overrides. Project sentinel zero and native
                // absence remain distinct where the original formats differ.
                count = notifications; document.SetMapRules(editable.Select(key => new MapRuleChange(key, null)));
                Expect(notifications == count + 1, "Bulk clear was not atomic.");
                foreach (var cell in document.MapRuleCells().Where(cell => cell.Editable)) Expect(cell.Value == (cell.Key.Kind == MapRuleKind.Alliance ? 0 : null), "Clear left a matching override.");
                var cleared = Snapshot(document); document.Undo(); Exact(document, after); document.Redo(); Exact(document, cleared);
                count = notifications; document.SetMapRules(editable.Select(key => new MapRuleChange(key, null))); Exact(document, cleared); Expect(notifications == count, "Repeated clear consumed history.");
                // Table reset retains NPC availability. The original complete
                // player reset first zeroes the entire MAPOPTIONS row instead.
                var npc = Key(MapRuleKind.Rooms, player: 1); document.SetMapRules([new(npc, 4)]);
                var resetBefore = Snapshot(document); document.ResetMapRules(keys);
                foreach (var cell in document.MapRuleCells().Where(cell => cell.Editable))
                {
                    var expectedValue = MapRules.IsAvailability(cell.Key.Kind) && cell.Key.Player <= 2 ? cell.Key == npc ? 4 : null : SourceDefault(cell.Key);
                    Expect(cell.Value == expectedValue, "Original defaults changed protected NPC availability or wrong source row: " + cell.Key);
                }
                var resetAfter = Snapshot(document); document.Undo(); Exact(document, resetBefore); document.Redo(); Exact(document, resetAfter);
                count = notifications; document.ResetMapRules(keys); Exact(document, resetAfter); Expect(notifications == count, "Repeated default reset changed history.");
                document.Undo(); _ = document.MapRuleCells(); Expect(document.CanRedo, "Read-only matrix consumed redo."); document.Redo();
                if (legacy)
                {
                    var info = document.Records().Single(record => record.File.Signature == NativeMapDocument.LegacyInfo);
                    Set(document, info, "Allies[2][3]", "0"); Set(document, info, "Allies[3][2]", "1");
                    var alliance = new MapRuleKey(MapRuleKind.Alliance, 3, 4); before = Snapshot(document);
                    Expect(Cell(document, alliance).Value == 1, "Asymmetric project alliance was not displayed like original OR rule."); Exact(document, before);
                    document.SetMapRules([new(alliance, 0)]); Expect(Read(info, "Allies[2][3]") == "0" && Read(info, "Allies[3][2]") == "0", "Alliance edit failed to update both directions.");
                    document.Undo(); Exact(document, before);
                    CheckHistorical(document, directory);
                }
                else CheckNativeDuplicates(document);
            }
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void CheckNativeDuplicates(NativeMapDocument document)
    {
        var room = Key(MapRuleKind.Rooms); document.SetMapRules([new(room, 3)]);
        var original = document.Records().Last(record => record.Category == "GAMEVARTHINGSTATUS" && Read(record, "wThingType") == "1" && Read(record, "wGlobalPlayer") == "3" && Read(record, "dwThingID") == room.Element.ToString());
        var duplicate = document.DuplicateRecord(original); Set(document, duplicate, "dwThingState", "4");
        var unknown = document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEVARTHINGSTATUS"), new(0, 0));
        Set(document, unknown, "wThingType", "99"); Set(document, unknown, "dwThingID", "0"); Set(document, unknown, "dwThingState", "123456789");
        var before = Snapshot(document); var expected = before.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
        var field = duplicate.Fields.Single(field => field.Name == "dwThingState"); field.Encode("2").CopyTo(expected[duplicate.File], duplicate.Offset + field.Offset);
        Expect(Cell(document, room) is { Value: 4, RecordCount: 2 }, "Native duplicate last-wins projection failed."); Exact(document, before);
        document.SetMapRules([new(room, 2)]); Exact(document, expected); document.Undo(); Exact(document, before); document.Redo();
        var unknownBytes = unknown.File.Data.Span.Slice(unknown.Offset, unknown.Length).ToArray();
        before = Snapshot(document); document.SetMapRules([new(room, null)]);
        Expect(Cell(document, room) is { Value: null, RecordCount: 0 }, "Clear removed only one native duplicate.");
        var preserved = document.Records().Single(record => record.Category == "GAMEVARTHINGSTATUS" && Read(record, "wThingType") == "99");
        Expect(preserved.File.Data.Span.Slice(preserved.Offset, preserved.Length).SequenceEqual(unknownBytes), "Unknown variable payload was normalized/removed.");
        document.Undo(); Exact(document, before);
        var alliance = new MapRuleKey(MapRuleKind.Alliance, 3, 4); document.SetMapRules([new(alliance, 1)]);
        original = document.Records().Last(record => record.Category == "GAMEVARALLIANCE" && Read(record, "dwGlobalPlayer") == "3" && Read(record, "dwAllieWith") == "4");
        duplicate = document.DuplicateRecord(original); Set(document, duplicate, "dwGlobalPlayer", "4"); Set(document, duplicate, "dwAllieWith", "3");
        before = Snapshot(document); Expect(Cell(document, alliance).RecordCount == 2, "Reversed alliance duplicate was missed.");
        document.SetMapRules([new(alliance, 1)]); Exact(document, before);
        document.SetMapRules([new(alliance, 0)]); Expect(Cell(document, alliance) is { Value: 0, RecordCount: 0 }, "Alliance clear left reverse records."); document.Undo(); Exact(document, before);
        var elite = Key(MapRuleKind.CreatureModifier, 16, parameter: 1);
        var record = document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEVARCREATURE"), new(0, 0));
        Set(document, record, "dwGlobalCreatureID", elite.Element.ToString()); Set(document, record, "dwCreatureParamType", "1"); Set(document, record, "dwCreatureNewValue", "567");
        before = Snapshot(document); document.ResetMapRules([elite]); Exact(document, before); Expect(Cell(document, elite).Value == 567, "Reset altered forbidden elite modifier.");
    }
    private static void CheckHistorical(NativeMapDocument document, string directory)
    {
        var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(directory, "historical-basis")); var sections = LegacySectionFile.Read(saved);
        var old = sections.Sections.Select(section => section.Signature == NativeMapDocument.LegacyInfo
            ? new LegacySection(section.Signature, 1, section.Payload[..LegacyProjectSchemas.Size("MAPPROPERTIESV1")]) : section).ToArray();
        var path = Path.Combine(directory, "historical.dkm"); File.WriteAllBytes(path, LegacySectionFile.Create(old).ToArray());
        var historical = NativeMapDocument.Load(path); var before = Snapshot(historical); var modifier = historical.MapRuleCells().First(cell => cell.Key.Kind == MapRuleKind.CreatureModifier);
        Expect(modifier.Editable, "Verified historical modifier fields were unnecessarily disabled.");
        historical.SetMapRules([new(modifier.Key, 17)]); Expect(Cell(historical, modifier.Key).Value == 17, "Historical modifier uses incorrect field layout."); historical.Undo(); Exact(historical, before);
        var future = sections.Sections.Select(section => section.Signature == NativeMapDocument.LegacyInfo ? section with { Version = 99 } : section).ToArray();
        path = Path.Combine(directory, "future.dkm"); File.WriteAllBytes(path, LegacySectionFile.Create(future).ToArray());
        historical = NativeMapDocument.Load(path); before = Snapshot(historical); modifier = historical.MapRuleCells().First(cell => cell.Key.Kind == MapRuleKind.CreatureModifier);
        Expect(!modifier.Editable, "Unknown project modifier layout was guessed.");
        Reject(() => historical.SetMapRules([new(modifier.Key, 1)])); Exact(historical, before);
    }
}
