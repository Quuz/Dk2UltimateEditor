using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;

internal static class DefinitionReferenceDiagnosticChecks
{
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static NativeMapDocument Document(bool project) => project
        ? NativeMapDocument.CreateNewMap(new("Definitions", "Definitions", 80, 80, PlaceHearts: false)) : NativeMapDocument.Create(12, 12, "Definitions");
    private static NativeMapDocument Globals(bool project) => project ? Document(true)
        : GlobalDefinitionNumericChecks.Fixture(Path.Combine(Path.GetTempPath(), "DK2DefinitionGlobals-" + Guid.NewGuid().ToString("N")));
    private static NativeRecord Add(NativeMapDocument document, string project, string native) => document.IsLegacyProject
        ? document.AddLegacyRecord(project, new(3, 3))
        : document.AddRecord(RecordTemplate.All.Single(row => row.Structure == native), new(3, 3));
    private static void Raw(NativeRecord record, string name, string value)
    {
        var field = record.Fields.Single(row => row.Name == name);
        Require(MemoryMarshal.TryGetArray(record.File.Data, out var buffer), "Fixture bytes unavailable.");
        field.Encode(value).CopyTo(buffer.Array!, buffer.Offset + record.Offset + field.Offset);
    }
    private static void Header(NativeFile file, uint version)
    {
        Require(MemoryMarshal.TryGetArray(file.Data, out var buffer), "Fixture bytes unavailable.");
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.Array!.AsSpan(buffer.Offset + 4), version);
    }
    private static IReadOnlyList<MapDiagnostic> Check(NativeMapDocument document, bool scripts = false)
    {
        _ = document.Records();
        var files = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var history = document.GetHistory(); var revision = document.EditRevision; var dirty = document.HasChanges;
        var changes = 0; var histories = 0;
        EventHandler changed = (_, _) => changes++; EventHandler hc = (_, _) => histories++;
        document.Changed += changed; document.HistoryChanged += hc;
        MapDiagnostic[] Read() => NativeMapValidator.Validate(document, includeScripts: scripts)
            .Where(row => row.Code is "DK2N039" or "DK2N040").ToArray();
        MapDiagnostic[] result;
        try { result = Read(); Require(Read().SequenceEqual(result), "Definition diagnostics are unstable."); }
        finally { document.Changed -= changed; document.HistoryChanged -= hc; }
        var after = document.GetHistory();
        Require(files.Count == document.Files.Count && files.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value))
            && history.EditorChangeCount == after.EditorChangeCount && history.ChangeCount == after.ChangeCount
            && history.SnapshotBytes == after.SnapshotBytes && history.Undo.SequenceEqual(after.Undo) && history.Redo.SequenceEqual(after.Redo)
            && revision == document.EditRevision && dirty == document.HasChanges && changes == 0 && histories == 0,
            "Definition diagnosis changed bytes, history, counters, revision or notifications.");
        Require(result.All(row => row.Severity == DiagnosticSeverity.Information), "A catalogue notice became a warning/error about game validity.");
        return result;
    }
    private static void One(NativeMapDocument document, string field, string value)
    {
        var rows = Check(document);
        Require(rows.Count == 1 && rows[0].Code == "DK2N039" && rows[0].Message.Contains(field + "=" + value, StringComparison.Ordinal),
            "Expected exactly one field/value notice for " + field + ": " + string.Join(";", rows.Select(row => row.Message)));
    }
    public static void Run(Action<string, Action> run)
    {
        run("definition references: physical categories and spellbook payload are contextual in native/DKM", () =>
        {
            foreach (var project in new[] { false, true })
            foreach (var pair in new[] { ("Creature", "GAMEKEEPER", 1), ("Creature", "GAMENEUTRAL", 17), ("Creature", "GAMEHERO", 32),
                ("Door", "GAMEDOOR", 6), ("Trap", "GAMETRAP", 4), ("Object", "GAMEOBJECT", 28), ("MagicalObject", "GAMEOBJECT", 127) })
            {
                var document = Document(project); var record = Add(document, pair.Item1, pair.Item2);
                var field = project ? "id" : pair.Item2 switch { "GAMEDOOR" => "bDoorID", "GAMETRAP" => "bTrapID", "GAMEOBJECT" => "bObjectID", _ => "bGlobalCreatureID" };
                Raw(record, field, pair.Item3.ToString());
                Require(Check(document).Count == 0, "A real nonordinal source ID or imported elite was diagnosed.");
                Raw(record, field, "255"); One(document, field, "255");
                Raw(record, field, "0"); One(document, field, "0");
            }
            foreach (var project in new[] { false, true })
            {
                var document = Document(project); var record = Add(document, "Object", "GAMEOBJECT");
                Raw(record, project ? "id" : "bObjectID", "4"); Raw(record, project ? "dataAll" : "dwObjectSpell", "4");
                Require(Check(document).Count == 0, "A real Create Imp book was diagnosed.");
                Raw(record, project ? "dataAll" : "dwObjectSpell", "255"); One(document, project ? "dataAll" : "dwObjectSpell", "255");
                Raw(record, project ? "id" : "bObjectID", "28");
                Require(Check(document).Count == 0, "Ordinary object data was misread as a spell ID.");
            }
        });
        run("definition references: native pool and availability IDs use their category rather than row position", () =>
        {
            foreach (var type in new[] { "GAMEVARCREATURECOUNT", "GAMEVARCREATURE" })
            {
                var document = Document(false); var record = Add(document, "", type);
                Raw(record, "dwGlobalCreatureID", "32"); Require(Check(document).Count == 0, "Elite source ID was confused with an array position.");
                Raw(record, "dwGlobalCreatureID", "255"); One(document, "dwGlobalCreatureID", "255");
            }
            foreach (var (kind, id) in new[] { (1, 1), (3, 4), (4, 6), (5, 4) })
            {
                var document = Document(false); var record = Add(document, "", "GAMEVARTHINGSTATUS");
                Raw(record, "wThingType", kind.ToString()); Raw(record, "dwThingID", id.ToString());
                Require(Check(document).Count == 0, "Real variable ID was diagnosed for category " + kind);
                Raw(record, "dwThingID", "255"); One(document, "dwThingID", "255");
                Raw(record, "wThingType", "99"); Require(Check(document).Count == 0, "Unknown availability category was guessed.");
            }
        });
        run("definition references: only the active band prefix is scanned and imported elites remain known", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = Document(project); var band = Add(document, "Band", "GAMEBAND"); document.AddBandMember(band, 17);
                band = document.Records().Single(row => NativeMapDocument.IsBand(row));
                var member = project ? band : document.Records().Single(row => row.File == band.File && row.Offset == band.Offset + 51);
                var id = project ? "heros[0].id" : "bGlobalCreatureID";
                Raw(member, id, "32"); Require(Check(document).Count == 0, "Elite was restricted by the narrower band suggestion list.");
                Raw(member, id, "255"); One(document, id, "255");
                Raw(member, id, "17");
                if (project) { Raw(band, "heros[1].id", "255"); Raw(band, "heros[2].id", "255"); Raw(band, "heros[2].isvalid", "1"); }
                else
                {
                    var inactive = document.Records().Single(row => row.File == band.File && row.Offset == band.Offset + 83);
                    Raw(inactive, "bGlobalCreatureID", "255"); Raw(inactive, "bGlobalBehavior", "0");
                    var later = document.Records().Single(row => row.File == band.File && row.Offset == band.Offset + 115);
                    Raw(later, "bGlobalCreatureID", "255"); Raw(later, "bGlobalBehavior", "1");
                }
                Require(Check(document).Count == 0, "Unused or post-sentinel band bytes became live references.");
            }
        });
        run("definition references: generator slots have real zero, accept effect 370 and retain extensions", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = Document(project); var effect = Add(document, "Effect", "GAMEEFFECT");
                for (var index = 1; index <= 4; index++) Raw(effect, project ? "effect" + index : "wEffect" + index, index switch { 1 => "0", 2 => "1", 3 => "370", _ => "0" });
                Require(Check(document).Count == 0, "Effect zero, repeated values or source endpoint was diagnosed.");
                for (var index = 1; index <= 4; index++)
                {
                    var field = project ? "effect" + index : "wEffect" + index;
                    Raw(effect, field, "371"); One(document, field, "371"); Raw(effect, field, "0");
                }
            }
        });
        run("definition references: terrain uses tile IDs and aggregates repeated unknown IDs without mutation", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = Document(project); Require(Check(document).Count == 0, "Original default terrain was diagnosed.");
                document.Paint(new[] { new TilePoint(3, 3), new TilePoint(4, 3) }, new MapTile(250, 2, 1));
                var rows = Check(document);
                Require(rows.Count == 1 && rows[0].Code == "DK2N040" && rows[0].Message.Contains("250") && rows[0].Message.Contains("2 cells"),
                    "Unknown repeated terrain did not produce one bounded notice.");
                document.Undo(); Require(Check(document).Count == 0, "Undo left stale catalogue diagnosis.");
                document.Redo(); Require(Check(document).SequenceEqual(rows), "Redo changed the notice order/content.");
            }
        });
        run("definition references: future headers are not interpreted and unknown IDs survive actual save/reopen", () =>
        {
            var directory = Path.Combine(Path.GetTempPath(), "DK2DefinitionReferences-" + Guid.NewGuid().ToString("N"));
            foreach (var project in new[] { false, true })
            {
                var document = Document(project); var thing = Add(document, "Creature", "GAMEKEEPER");
                Raw(thing, project ? "id" : "bGlobalCreatureID", "255");
                One(document, project ? "id" : "bGlobalCreatureID", "255");
                var saved = document.SaveCopy(Path.Combine(directory, project ? "project" : "native"));
                var reopened = NativeMapDocument.Load(saved);
                Require(Check(reopened).Select(row => row.Message).SequenceEqual(Check(document).Select(row => row.Message)), "Save/reopen lost or repaired an unknown ID.");
                Header(thing.File, 99); Require(Check(document).Count == 0, "A future thing header inherited current semantics.");
            }
        });
        run("definition references: global links use verified fields, optional zero and active attraction context", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = Globals(project);
                var defaults = Check(document);
                Require(defaults.Count == 0, "Original default references were diagnosed: " + string.Join("; ", defaults.Take(4).Select(row => row.Message)));
                foreach (var (family, id, field, invalid) in new[] { ("Terrains", 2u, "MaxHealthEffect", "371"), ("Terrains", 2u, "MaxHealthType", "255"),
                    ("Rooms", 15u, "Objects[0]", "255"), ("Rooms", 15u, "Effects[0]", "371"), ("Doors", 1u, "Crate", "255"), ("Doors", 1u, "Key", "255"),
                    ("Traps", 4u, "Crate", "255"), ("Creatures", 1u, "LairObject", "255"), ("Creatures", 1u, "Shot1ID", "255"), ("Creatures", 1u, "MeleeID", "255") })
                {
                    var row = document.GlobalDefinitionRows().Single(row => row.Family == family && row.ID == id);
                    Require(row.Editable, "Global fixture has no verified " + family + " layout.");
                    document.SetGlobalDefinitionValues([new(family, row.Index, field, invalid)]);
                    row = document.GlobalDefinitionRows().Single(row => row.Family == family && row.ID == id);
                    var record = row.Record!; var stored = record.Fields.Single(item => item.Name == field);
                    var target = record.Category + "." + field + "=" + invalid + ", 0x" + (record.Offset + stored.Offset).ToString("X");
                    Require(Check(document).Count(item => item.Message.Contains(target, StringComparison.Ordinal)) == 1, "Global link was not diagnosed: " + target);
                    Raw(record, field, "0");
                    Require(!Check(document).Any(item => item.Message.Contains(record.Category + "." + field + "=0, 0x" + (record.Offset + stored.Offset).ToString("X"), StringComparison.Ordinal)), "Optional zero definition link was diagnosed.");
                }
                var creature = document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.ID == 1);
                document.SetGlobalDefinitionValues([new("Creatures", creature.Index, "Attract2Type", "4"), new("Creatures", creature.Index, "Attract2ID", "255")]);
                creature = document.GlobalDefinitionRows().Single(row => row.Family == "Creatures" && row.ID == 1);
                Require(Check(document).Any(row => row.Message.Contains(".Attract2ID=255")), "Species attraction target was omitted.");
                Raw(creature.Record!, "Attract2ID", "32"); Require(!Check(document).Any(row => row.Message.Contains(".Attract2ID=32")), "Elite attraction species was diagnosed.");
                Raw(creature.Record!, "Attract2ID", "255"); Raw(creature.Record!, "Attract2Type", "2");
                Require(!Check(document).Any(row => row.Message.Contains(".Attract2ID=255")), "Unused attraction bytes were interpreted as species.");
                Raw(creature.Record!, "Spell", "255"); Raw(creature.Record!, "GammaEffect", "255");
                Require(!Check(document).Any(row => row.Message.Contains(".Spell=255") || row.Message.Contains(".GammaEffect=255")), "Possession swipe/gamma enums were mistaken for spell/effect definitions.");
                Raw(creature.Record!, "LairObject", "255"); Header(creature.Record!.File, 99);
                Require(!Check(document).Any(row => row.Message.Contains(creature.Record.Category + ".LairObject=255")), "A future global layout inherited verified reference fields.");
            }
        });
        run("definition references: all four projectile slots discriminate numbers, species, object targets and unknown types", () =>
        {
            foreach (var project in new[] { false, true })
            {
                var document = Globals(project);
                foreach (var (family, id, type, data1, data2) in new[] { ("Traps", 4u, "ShotType", "ShotData1", "ShotData2"),
                    ("Spells", 4u, "ShotType", "ShotData1", "ShotData2"), ("Spells", 4u, "UpgShotType", "UpgShotData1", "UpgShotData2"),
                    ("CrSpells", 1u, "Shot", "ShotData1", "ShotData2") })
                {
                    var row = document.GlobalDefinitionRows().Single(row => row.Family == family && row.ID == id);
                    document.SetGlobalDefinitionValues([new(family, row.Index, type, "1"), new(family, row.Index, data1, "255"), new(family, row.Index, data2, "255")]);
                    row = document.GlobalDefinitionRows().Single(row => row.Family == family && row.ID == id);
                    var record = row.Record!;
                    Require(!Check(document).Any(item => item.Message.Contains(record.Category + "." + data1 + "=255")), "A numeric projectile argument became a target.");
                    foreach (var targetType in new[] { "2", "60", "5" }) // Literal GShots forms 2, 3, 4.
                    {
                        Raw(record, type, targetType); Raw(record, data1, "255");
                        var location = record.Offset + record.Fields.Single(field => field.Name == data1).Offset;
                        var target = record.Category + "." + data1 + "=255, 0x" + location.ToString("X");
                        Require(Check(document).Count(item => item.Message.Contains(target, StringComparison.Ordinal)) == 1, "Typed projectile target was omitted: " + family + " / " + type);
                        Raw(record, data1, "1"); Require(!Check(document).Any(item => item.Message.Contains(record.Category + "." + data1 + "=1, 0x" + location.ToString("X"))), "Real projectile target was diagnosed.");
                    }
                    Raw(record, type, "255"); Raw(record, data1, "255");
                    Require(Check(document).Any(item => item.Message.Contains(record.Category + "." + type + "=255"))
                        && !Check(document).Any(item => item.Message.Contains(record.Category + "." + data1 + "=255")), "Unknown projectile type invented a parameter catalogue.");
                    Raw(record, type, "0"); // Restore an inactive slot before the next row shares its category.
                }
            }
        });
        run("definition references: scenario contexts, Any sentinels, split interface categories and no-script export", () =>
        {
            foreach (var project in new[] { false, true })
            {
                foreach (var (name, parameter, valid, any) in new[] { ("TRIGGER_CREATURES", 2, 32, true), ("TRIGGER_HAPPYCREATURES", 2, 1, true),
                    ("TRIGGER_ANGRYCREATURES", 2, 1, true), ("TRIGGER_CREAENTERAREA", 3, 1, true), ("ACTION_ADDCREATURE", 1, 32, false), ("ACTION_GENERATE", 1, 32, false),
                    ("TRIGGER_ROOMS", 2, 1, true), ("TRIGGER_ROOMSSIZE", 2, 1, true), ("TRIGGER_ROOMSCONTENT", 2, 1, true), ("TRIGGER_ROOMSTYPES", 2, 5, true),
                    ("ACTION_SHOWNEXTROOM", 1, 1, true), ("TRIGGER_DOORS", 2, 6, true), ("TRIGGER_TRAPS", 2, 4, true), ("TRIGGER_DESTROYED", 2, 6, true),
                    ("TRIGGER_SPELLS", 2, 4, false), ("TRIGGER_AREACONTENT", 3, 2, false), ("ACTION_CHANGESQUARE", 1, 2, false) })
                {
                    var document = Document(project); var type = LegacyEventSchemas.All.Single(type => type.Name == name);
                    var record = Add(document, type.RecordSignature == 0xD5 ? "Trigger" : "Action", type.RecordSignature == 0xD5 ? "Trigger" : "Action");
                    document.SetEventType(record, type); record = document.Records().Single(ScriptGraph.IsEvent);
                    var field = (project ? "param" : "Param") + parameter;
                    Raw(record, field, valid.ToString()); Require(Check(document, true).Count == 0, "Real scenario ID was diagnosed: " + name);
                    Raw(record, field, "255");
                    Require(Check(document, true).Count == 1 && Check(document, false).Count == 0, "Scenario definition notice was lost or leaked into no-script export: " + name);
                    if (any) { Raw(record, field, "0"); Require(Check(document, true).Count == 0, "Contextual Any/None zero was diagnosed: " + name); }
                }
                foreach (var (name, primary) in new[] { ("TRIGGER_CLICKELEMENT", 1), ("ACTION_SHOWELEMENT", 1), ("ACTION_ADDELEMENT", 2) })
                {
                    var document = Document(project); var type = LegacyEventSchemas.All.Single(type => type.Name == name);
                    var record = Add(document, type.RecordSignature == 0xD5 ? "Trigger" : "Action", type.RecordSignature == 0xD5 ? "Trigger" : "Action");
                    document.SetEventType(record, type); record = document.Records().Single(ScriptGraph.IsEvent);
                    var prefix = project ? "param" : "Param";
                    Raw(record, prefix + primary, "3"); Raw(record, prefix + (primary + 1), "6"); Require(Check(document, true).Count == 0, "Scenario door category was confused with native variable trap category.");
                    Raw(record, prefix + (primary + 1), "255"); Require(Check(document, true).Count == 1, "Split interface definition ID was omitted.");
                    Raw(record, prefix + primary, "0"); Require(Check(document, true).Count == 0, "Interface panel number became a definition ID.");
                    Raw(record, prefix + primary, "99"); Require(Check(document, true).Count == 0, "Unknown interface category invented a target catalogue.");
                    Raw(record, prefix + primary, "3"); Header(record.File, 99);
                    Require(Check(document, true).Count == 0, "A future event header inherited current reference contexts.");
                }
            }
        });
    }
}
