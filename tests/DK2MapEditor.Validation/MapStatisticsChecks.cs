using System.Buffers.Binary;
using System.Globalization;
using System.Text.Json;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class MapStatisticsChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Set(NativeMapDocument document, NativeRecord record, string name, long value) => document.SetField(record, record.Fields.Single(field => field.Name == name), value.ToString(CultureInfo.InvariantCulture));
    // Record locations are not statistics. Compare all logical values, including
    // nested type counts, rather than IReadOnlyList reference identities.
    internal static string Projection(NativeMapDocument document)
    {
        var stats = document.CalculateStatistics();
        return JsonSerializer.Serialize(new { stats.Width, stats.Height, stats.TotalTiles, stats.Terrains, stats.Things, stats.TerrainGold, stats.ObjectGold, stats.SpecialGold, stats.UnlimitedGold,
            stats.Players, Bands = stats.Bands.Select(band => new { band.ID, band.Name, band.Imps, band.Dwarfs, band.Creatures, band.AverageLevel, band.PreferredCreature, band.ReleaseActions }), stats.BandTotals, stats.Issues });
    }
    public static void Run()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "DK2MapEditor.slnx"))) root = root.Parent;
        var defaults = File.ReadAllBytes(Path.Combine(root!.FullName, "Binaries", "Globals", "DefaultTerrain.dkg"));
        Expect(defaults.Length == 41 * 552, "Preserved terrain default layout changed.");
        // Independent source order anchors and literal Compiler_Generate offsets.
        long Gold(int index) => BinaryPrimitives.ReadUInt16LittleEndian(defaults.AsSpan(index * 552 + 0x19A));
        long Mana(int index) => BinaryPrimitives.ReadUInt16LittleEndian(defaults.AsSpan(index * 552 + 0x19C));
        Expect(Gold(5) == 3000 && Mana(7) == 1 && Mana(8) == 1 && Mana(31) == 100, "Original resource defaults changed.");
        var directory = Path.Combine(Path.GetTempPath(), "DK2-Statistics-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var project in new[] { false, true })
            {
                var document = NativeMapDocument.CreateNewMap(new("Statistics", "Statistics", 24, 20, NativeFormat: !project));
                document.Paint(MapDrawing.Rectangle(new(0, 0, 24, 20), true), new(2, 2, 1));
                document.Paint(MapDrawing.Rectangle(new(2, 2, 5, 5), true), new(14, 3, 1));
                foreach (var (terrain, count, y) in new[] { (10, 2, 8), (11, 3, 9), (8, 1, 10), (9, 1, 11), (32, 2, 12) })
                    document.Paint(Enumerable.Range(2, count).Select(x => new TilePoint(x, y)), new((byte)terrain, 3, 1, 0xA7));
                foreach (var (terrain, owner, x) in new[] { (4, 2, 2), (5, 2, 3), (1, 2, 4), (6, 2, 5), (6, 2, 6), (7, 2, 7), (31, 2, 8), (35, 1, 9) }) document.Paint([new(x, 14)], new((byte)terrain, (byte)owner, 1));
                document.SetMapVariables([new(0x18, 101), new(0xB2, 17), new(0x1B, 1234), new(0xEB, 40), new(0xA6, 55)]);
                NativeRecord Add(string kind, string native, uint id, byte owner = 3, long data = 0)
                {
                    var record = project ? document.AddLegacyRecord(kind, new(15, 15), owner) : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == native), new(15, 15), owner);
                    Set(document, record, project ? "id" : ValueCatalogues.PrimaryField(record)!.Name, id);
                    if (kind == "Creature") Set(document, record, project ? "dataAll" : "bGlobalLevel", data);
                    if (kind == "Object") Set(document, record, project ? "dataAll" : "dwObjectGoldValue", data);
                    return record;
                }
                Add("Creature", "GAMEKEEPER", 1, data: 9); Add("Creature", "GAMEKEEPER", 6, data: 4); Add("Creature", "GAMEKEEPER", 17, data: 8);
                var hero = Add("Creature", "GAMEHERO", 14, 1, 6); if (!project) Set(document, hero, "bGlobalPlayer", 7); // This is the enemy, not hero ownership.
                Add("Creature", "GAMENEUTRAL", 6, 2, 2);
                Add("Trap", "GAMETRAP", 2); Add("Trap", "GAMETRAP", 1); Add("Door", "GAMEDOOR", 1);
                Add("Object", "GAMEOBJECT", 1, data: 123); Add("Object", "GAMEOBJECT", 3, data: 456); Add("Object", "GAMEOBJECT", 4, data: 999);
                Add("MagicalObject", "GAMEOBJECT", 0x79); Add("MagicalObject", "GAMEOBJECT", 0x79);
                var band = project ? document.AddLegacyRecord("Band", new(0, 0)) : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0));
                Set(document, band, project ? "id" : "bBandID", 0);
                foreach (var (id, level) in new[] { (1u, 1), (14u, 3), (17u, 5), (6u, 7) }) document.AddBandMember(band, id, level, 3);
                var action = project ? document.AddLegacyRecord("Action", new(0, 0)) : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "Action"), new(0, 0));
                document.SetEventType(action, LegacyEventSchemas.All.Single(type => type.Name == "ACTION_RELEASEBAND"));
                action = document.Records().Single(record => record.File == action.File && record.Offset == action.Offset);
                Set(document, action, project ? "param1" : "Param1", 0);
                var bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var notifications = 0; var undo = document.CanUndo; var redo = document.CanRedo;
                document.Changed += (_, _) => notifications++;
                var stats = document.CalculateStatistics();
                foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Statistics modified map buffers.");
                Expect(notifications == 0 && document.CanUndo == undo && document.CanRedo == redo, "Statistics modified history or raised Changed.");
                Expect(stats.Width == 24 && stats.Height == 20 && stats.TotalTiles == 480 && stats.Issues.Count == 0, "Dimensions or supported-fixture diagnostics wrong: " + string.Join(",", stats.Issues));
                Expect(stats.TerrainGold == 2 * Gold(5) && stats.ObjectGold == 579 && stats.SpecialGold == 2468 && stats.UnlimitedGold, "Gold categories mixed or gems counted as finite gold.");
                foreach (var id in Enumerable.Range(1, 5))
                {
                    long expected = 0; for (var y = 0; y < 20; y++) for (var x = 0; x < 24; x++) if (document.GetTile(x, y).Terrain == id) expected++;
                    var row = stats.Terrains.Single(row => row.Terrain == id);
                    Expect(row.Count == expected && row.Percentage == expected * 100m / 480, "Independent terrain percentage mismatch.");
                }
                var player = stats.Players.Single(row => row.ID == 3);
                Expect(player.OwnedTiles == 34 && player.Walls == 1 && player.Claimed == 1 && player.LairCapacity == 3 && player.Heart == new TilePoint(4, 4), "Owned terrain/heart source rules wrong.");
                Expect(player.TreasuryCapacity == 2 * 101 + 16 * 17 && player.ManaIncrease == 55 && stats.Players.Single(row => row.ID == 4).TreasuryCapacity == 0, "Heart/treasury or two-tier mana cap wrong.");
                Expect(stats.Players[0].OwnedTiles == 1 && stats.Players[1].OwnedTiles == 1, "Closed mana vault / heroes' rest ownership lost.");
                Expect(stats.Things.Creatures == 5 && stats.Things.AverageCreatureLevel == 5 && stats.Things.PreferredCreature == 6 && player.Things.Creatures == 2 && player.Things.Imps == 1 && player.Things.AverageCreatureLevel == 6 && player.Things.PreferredCreature == 6, "Imp inclusion, creature levels or low-ID tie break wrong.");
                Expect(stats.Players[0].Things.Creatures == 1 && stats.Players[6].Things.Creatures == 0 && stats.Things.Traps == 2 && stats.Things.PreferredTrap == 1 && stats.Things.Doors == 1 && stats.Things.Objects == 3 && stats.Things.SpecialItems == 2, "Hero enemy mistaken for owner, or placed categories wrong.");
                Expect(stats.Bands.Single() is { ID: 0, Imps: 1, Dwarfs: 1, Creatures: 2, AverageLevel: 4, PreferredCreature: 6, ReleaseActions: 1 }
                    && stats.BandTotals is { Imps: 1, Dwarfs: 1, Creatures: 2, AverageLevel: 4, PreferredCreature: 6, HeroesDisplayedCount: 3, HeroesAverageLevel: 4 }, "Original band-zero/release/count/average rules wrong.");
                var projection = Projection(document); Expect(projection == Projection(document), "Repeated calculations differ.");
                var saved = document.SaveCopy(Path.Combine(directory, project ? "project" : "native")); Expect(projection == Projection(NativeMapDocument.Load(saved)), "Statistics changed after save/reopen.");
                document.SetMapVariables([new(0xEB, 1), new(0xA6, 999)]); Expect(document.CalculateStatistics().Players[2].ManaIncrease == 201, "Mana vaults did not bypass first cap.");
                document.Undo(); Expect(Projection(document) == projection, "Undo did not recover statistics."); document.Redo(); Expect(document.CalculateStatistics().Players[2].ManaIncrease == 201, "Redo statistics stale."); document.Undo();
                document.SetMapVariables([new(0xEB, 0), new(0xA6, 999)]); Expect(document.CalculateStatistics().Players[2].ManaIncrease == 200, "Heart mana ignored zero room cap."); document.Undo();
                Add("Object", "GAMEOBJECT", 2, data: 2_000_000_000); Add("Object", "GAMEOBJECT", 2, data: 2_000_000_000);
                Expect(document.CalculateStatistics().ObjectGold == 4_000_000_579L, "Gold sum overflowed a 32-bit integer.");
                if (project) CheckProjectDefinitions(document, directory);
                // Raw malformed imports remain editable; unknown tile bytes are never normalized.
                document.Paint([new(20, 18)], new(250, 255, 254, 0xCC)); bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
                Expect(document.CalculateStatistics().Issues.Contains("UnknownTiles"), "Unknown tile was silently categorized.");
                foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Unknown tile normalization changed buffers.");
            }
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void CheckProjectDefinitions(NativeMapDocument document, string directory)
    {
        var path = document.SaveCopy(Path.Combine(directory, "overrides-base")); var sections = LegacySectionFile.Read(path);
        var stride = LegacyProjectSchemas.Size("GLOBALTERRAIN"); var payload = new byte[4 + 38 * stride]; BinaryPrimitives.WriteUInt32LittleEndian(payload, 1);
        var fields = LegacyProjectSchemas.Fields("GLOBALTERRAIN");
        for (var index = 0; index < 38; index++) foreach (var field in fields.Where(field => field.Name is "GoldValue" or "ManaGain"))
            field.Encode(field.Name == "GoldValue" ? "222" : "7").CopyTo(payload, 4 + index * stride + field.Offset);
        var fixture = Path.Combine(directory, "overrides.dkm");
        File.WriteAllBytes(fixture, LegacySectionFile.Create(sections.Sections.Where(section => section.Signature != NativeMapDocument.LegacyGlobals).Append(new(NativeMapDocument.LegacyGlobals, 1, payload))).ToArray());
        var project = NativeMapDocument.Load(fixture); project.SetMapVariables([new(0xEB, 40), new(0xA6, 999)]);
        Expect(project.CalculateStatistics().TerrainGold == 444 && project.CalculateStatistics().Players[2].ManaIncrease == 54, "Project definition-array order or loaded mana overrides wrong.");
        var basisPath = NativeMapDocument.Create(24, 20, "Basis").SaveCopy(Path.Combine(directory, "definitions-basis"));
        var terrainBytes = new byte[56 + 41 * 552];
        void Header(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(terrainBytes.AsSpan(offset), value);
        Header(0, 0x6E); Header(4, 4); Header(8, (uint)terrainBytes.Length); Header(12, 0x6F); Header(16, 28); Header(20, 41); Header(48, 0x70); Header(52, (uint)terrainBytes.Length - 56);
        File.WriteAllBytes(Path.Combine(directory, "definitions-basis", "Data", "Editor", "Terrain.kwd"), terrainBytes);
        var native = project.CompileLegacyProject("StatisticsCompiled", NativeMapDocument.Load(basisPath));
        Expect(native.CalculateStatistics().TerrainGold == 444 && native.CalculateStatistics().Players[2].ManaIncrease == 54, "Native terrain definition order or loaded overrides wrong.");
        var saved = native.SaveCopy(Path.Combine(directory, "native-overrides")); Expect(Projection(native) == Projection(NativeMapDocument.Load(saved)), "Native definition statistics changed after export.");
        var future = Path.Combine(directory, "future.dkm");
        File.WriteAllBytes(future, LegacySectionFile.Create(sections.Sections.Where(section => section.Signature != NativeMapDocument.LegacyGlobals).Append(new(NativeMapDocument.LegacyGlobals, 99, payload))).ToArray());
        var unknown = NativeMapDocument.Load(future); var stats = unknown.CalculateStatistics();
        Expect(stats.TerrainGold is null && stats.Players[2].ManaIncrease is null && stats.Issues.Contains("UnsupportedTerrainDefinitions"), "Future definitions guessed valid values.");
        var futureEvents = Path.Combine(directory, "future-events.dkm");
        File.WriteAllBytes(futureEvents, LegacySectionFile.Create(sections.Sections.Select(section => section.Signature == NativeMapDocument.LegacyEvents ? new LegacySection(section.Signature, 99, section.Payload) : section)).ToArray());
        stats = NativeMapDocument.Load(futureEvents).CalculateStatistics();
        Expect(stats.Issues.Contains("UnsupportedScenarios") && stats.Bands.All(band => band.ReleaseActions is null), "Unknown scenario layout reported a guessed zero release count.");
        var bandSection = sections.Sections.Single(section => section.Signature == NativeMapDocument.LegacyBands);
        var sparsePayload = bandSection.Payload.ToArray(); var active = LegacyProjectSchemas.Fields("MTHBAND").Single(field => field.Name == "heros[1].isvalid");
        active.Encode("0").CopyTo(sparsePayload, active.Offset);
        var sparse = Path.Combine(directory, "sparse.dkm");
        File.WriteAllBytes(sparse, LegacySectionFile.Create(sections.Sections.Select(section => section.Signature == NativeMapDocument.LegacyBands ? new LegacySection(section.Signature, section.Version, sparsePayload) : section)).ToArray());
        stats = NativeMapDocument.Load(sparse).CalculateStatistics();
        Expect(stats.Issues.Contains("SparseBands") && stats.Bands.Single() is { Imps: 1, Dwarfs: 0, Creatures: 0, AverageLevel: 1 }, "Sparse band did not follow source initial-active-prefix rule.");
        var duplicatePayload = bandSection.Payload.ToArray().Concat(bandSection.Payload.ToArray()).ToArray(); var duplicate = Path.Combine(directory, "duplicate.dkm");
        File.WriteAllBytes(duplicate, LegacySectionFile.Create(sections.Sections.Select(section => section.Signature == NativeMapDocument.LegacyBands ? new LegacySection(section.Signature, section.Version, duplicatePayload) : section)).ToArray());
        stats = NativeMapDocument.Load(duplicate).CalculateStatistics();
        Expect(stats.Issues.Contains("DuplicateBandIDs") && stats.Bands.Count == 2 && stats.Bands.All(band => band is { Imps: 1, Dwarfs: 1, Creatures: 2, AverageLevel: 4, ReleaseActions: 1 }) && stats.BandTotals.Creatures == 4, "Duplicate IDs merged physical bands or multiplied release counts.");
        var malformed = NativeMapDocument.Load(path); var creature = malformed.AddLegacyRecord("Creature", new(1, 1));
        Set(malformed, creature, "id", 1000); Set(malformed, creature, "owner", 8); Set(malformed, creature, "dataAll", 10);
        var malformedBytes = malformed.Files.ToDictionary(file => file, file => file.Data.ToArray());
        stats = malformed.CalculateStatistics(); Expect(stats.Issues.Contains("InvalidOwners") && stats.Things.Types.Any(type => type.ID == 1000 && type.Count == 1) && stats.Things.Creatures == 6, "Unknown species overflowed source arrays or invalid owner became a player.");
        foreach (var file in malformed.Files) Expect(file.Data.Span.SequenceEqual(malformedBytes[file]), "Malformed statistics repaired input bytes.");
    }
}
