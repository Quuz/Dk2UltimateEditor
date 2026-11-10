using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public enum StatisticsThingKind { Creature, Trap, Door, Object, SpecialItem }
public sealed record StatisticsTypeCount(StatisticsThingKind Kind, uint ID, long Count, long LevelSum);
public sealed record StatisticsThings(IReadOnlyList<StatisticsTypeCount> Types, long Creatures, long Imps, long Traps, long Doors, long Objects, long SpecialItems,
    long AverageCreatureLevel, uint? PreferredCreature, uint? PreferredTrap, uint? PreferredDoor, uint? PreferredObject, uint? PreferredSpecialItem);
public sealed record StatisticsTerrain(byte Terrain, long Count, decimal Percentage);
public sealed record StatisticsPlayer(uint ID, StatisticsThings Things, long OwnedTiles, long Walls, long Claimed, long LairCapacity,
    TilePoint? Heart, long? TreasuryCapacity, long? ManaIncrease);
public sealed record StatisticsBand(NativeRecord Record, uint ID, string Name, long Imps, long Dwarfs, long Creatures,
    long AverageLevel, uint? PreferredCreature, long? ReleaseActions);
public sealed record StatisticsBandTotals(long Imps, long Dwarfs, long Creatures, long AverageLevel, uint? PreferredCreature, long HeroesDisplayedCount, long HeroesAverageLevel);
public sealed record MapStatistics(int Width, int Height, long TotalTiles, IReadOnlyList<StatisticsTerrain> Terrains,
    StatisticsThings Things, long? TerrainGold, long ObjectGold, long? SpecialGold, bool UnlimitedGold,
    IReadOnlyList<StatisticsPlayer> Players, IReadOnlyList<StatisticsBand> Bands, StatisticsBandTotals BandTotals,
    IReadOnlyList<string> Issues);

public sealed partial class NativeMapDocument
{
    private sealed class StatisticsThingAccumulator
    {
        private readonly Dictionary<(StatisticsThingKind Kind, uint ID), (long Count, long Levels)> values = [];
        public void Add(StatisticsThingKind kind, uint id, long level = 0)
        { var old = values.GetValueOrDefault((kind, id)); values[(kind, id)] = (old.Count + 1, old.Levels + level); }
        public StatisticsThings Result(bool player)
        {
            long Count(StatisticsThingKind kind) => values.Where(pair => pair.Key.Kind == kind).Sum(pair => pair.Value.Count);
            long creatures = Count(StatisticsThingKind.Creature), imps = values.GetValueOrDefault((StatisticsThingKind.Creature, 1u)).Count;
            var creatureCount = player ? creatures - imps : creatures;
            var levels = values.Where(pair => pair.Key.Kind == StatisticsThingKind.Creature && (!player || pair.Key.ID != 1)).Sum(pair => pair.Value.Levels);
            uint? Preferred(StatisticsThingKind kind) => values.Where(pair => pair.Key.Kind == kind && pair.Key.ID != 0 && (!player || kind != StatisticsThingKind.Creature || pair.Key.ID != 1))
                .OrderByDescending(pair => pair.Value.Count).ThenBy(pair => pair.Key.ID).Select(pair => (uint?)pair.Key.ID).FirstOrDefault();
            return new(values.OrderBy(pair => pair.Key.Kind).ThenBy(pair => pair.Key.ID).Select(pair => new StatisticsTypeCount(pair.Key.Kind, pair.Key.ID, pair.Value.Count, pair.Value.Levels)).ToArray(),
                creatureCount, imps, Count(StatisticsThingKind.Trap), Count(StatisticsThingKind.Door), Count(StatisticsThingKind.Object), Count(StatisticsThingKind.SpecialItem),
                creatureCount == 0 ? 0 : levels / creatureCount, Preferred(StatisticsThingKind.Creature), Preferred(StatisticsThingKind.Trap), Preferred(StatisticsThingKind.Door), Preferred(StatisticsThingKind.Object), Preferred(StatisticsThingKind.SpecialItem));
        }
    }
    private sealed record StatisticsTileRule(int Group, byte Owner);
    private static readonly Lazy<IReadOnlyDictionary<(byte Terrain, byte Variant, byte Owner), StatisticsTileRule>> statisticsTileRules = new(() =>
    {
        var result = new Dictionary<(byte, byte, byte), StatisticsTileRule>();
        foreach (var (group, category) in new[] { ("World", 0), ("Rooms", 1), ("Gates", 2), ("Walls", 3) })
            foreach (var item in LegacyCompilerRules.Elements(group))
                for (byte owner = 0; owner <= 7; owner++)
                {
                    if (category == 3 && owner != (byte)(item.ID >> 16)) continue;
                    var effectiveOwner = category == 1 ? owner : (byte)(item.ID >> 16);
                    result.TryAdd(((byte)(item.ID >> 24), (byte)(item.ID >> 8), owner), new(category, effectiveOwner));
                }
        return new ReadOnlyDictionary<(byte, byte, byte), StatisticsTileRule>(result);
    });
    private static readonly Lazy<IReadOnlyDictionary<uint, (long Gold, long Mana)>> statisticsTerrainDefaults = new(() =>
    {
        var definition = LegacyGlobalSchemas.Find(0x6E)!; var order = LegacyCompilerRules.DefinitionOrder("Terrains");
        var catalogue = LegacyCompilerRules.Elements("GTerrains");
        using var stream = typeof(NativeMapDocument).Assembly.GetManifestResourceStream("Legacy.DefaultTerrain.dkg")!;
        using var buffer = new MemoryStream(); stream.CopyTo(buffer); var bytes = buffer.ToArray();
        if (bytes.Length != order.Count * definition.RecordSize || catalogue.Count != 38) throw new InvalidDataException("Unverified preserved terrain defaults.");
        var gold = definition.Fields.Single(field => field.Name == "GoldValue"); var mana = definition.Fields.Single(field => field.Name == "ManaGain");
        var result = new Dictionary<uint, (long, long)>();
        for (var index = 0; index < order.Count; index++)
        {
            if (order[index] < 0) continue;
            var data = bytes.AsSpan(index * definition.RecordSize, definition.RecordSize);
            result.Add(catalogue[order[index]].ID, (long.Parse(gold.Read(data), CultureInfo.InvariantCulture), long.Parse(mana.Read(data), CultureInfo.InvariantCulture)));
        }
        return new ReadOnlyDictionary<uint, (long, long)>(result);
    });

    private Dictionary<uint,(long? Gold,long? Mana)> StatisticsTerrainValues(IReadOnlyList<NativeRecord> records,HashSet<string> issues)
    {
        var terrainValues = statisticsTerrainDefaults.Value.ToDictionary(pair => pair.Key, pair => ((long? Gold, long? Mana))(pair.Value.Gold, pair.Value.Mana));
        var catalogue = LegacyCompilerRules.Elements("GTerrains");
        long Number(NativeRecord record, string name, long fallback = 0) => record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? long.Parse(record.Read(field), CultureInfo.InvariantCulture) : fallback;
        if (IsLegacyProject)
        {
            var overrides = records.Where(record => record.Category == "Project.GLOBALTERRAIN").ToArray();
            if (records.Any(record => record.File.Signature == LegacyGlobals && record.Category == "Project.Opaque") || overrides.Length != 0 && overrides.Length != catalogue.Count)
            { issues.Add("UnsupportedTerrainDefinitions"); foreach (var id in terrainValues.Keys.ToArray()) terrainValues[id] = (null, null); }
            else for (var index = 0; index < overrides.Length; index++) terrainValues[catalogue[index].ID] = (Number(overrides[index], "GoldValue"), Number(overrides[index], "ManaGain"));
        }
        else
        {
            var order = LegacyCompilerRules.DefinitionOrder("Terrains");
            var groups = records.Where(record => record.Category == "Definition.Terrains").GroupBy(record => record.File).ToArray();
            if (groups.Length > 0)
            {
                var selected = groups.Last().ToArray();
                var header = selected[0].Offset - 56;
                if (selected.Length != order.Count || header < 0 || selected[0].File.UInt32(header) != 0x6E || selected[0].File.UInt32(header + 4) != 4
                    || selected.Any(record => record.Length != 552 || record.Fields.Count == 0))
                { issues.Add("UnsupportedTerrainDefinitions"); foreach (var id in terrainValues.Keys.ToArray()) terrainValues[id] = (null, null); }
                else for (var index = 0; index < order.Count; index++) if (order[index] >= 0) terrainValues[catalogue[order[index]].ID] = (Number(selected[index], "GoldValue"), Number(selected[index], "ManaGain"));
            }
        }
        return terrainValues;
    }

    /// <summary>Original editor map/player/band calculations. Read-only, 64-bit
    /// totals, no implicit upgrade, no input normalization and no game-engine claim.</summary>
    public MapStatistics CalculateStatistics()
    {
        var records = Records(); var issues = new HashSet<string>(StringComparer.Ordinal);
        var variables = MapVariableCells().ToDictionary(cell => cell.Definition.ID);
        long? Variable(uint id)
        {
            var cell = variables[id];
            if (!IsLegacyProject && MapVariableFile is { } file && file.UInt32(4) != 4
                || IsLegacyProject && records.Any(record => record.File.Signature == LegacyInfo && record.Category == "Project.Opaque"))
            { issues.Add("UnsupportedVariables"); return null; }
            // Known old project readers initialize missing arrays with source defaults.
            return unchecked((int)(cell.Bits ?? cell.Definition.DefaultBits));
        }
        var treasuryGold = Variable(0x18); var heartGold = Variable(0xB2); var specialGold = Variable(0x1B); var roomManaCap = Variable(0xEB); var totalManaCap = Variable(0xA6);
        var terrainValues = StatisticsTerrainValues(records,issues);
        long Number(NativeRecord record, string name, long fallback = 0) => record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? long.Parse(record.Read(field), CultureInfo.InvariantCulture) : fallback;
        var terrainCounts = new Dictionary<byte, long>(); var owned = new long[7]; var walls = new long[7]; var claimed = new long[7]; var lairs = new long[7];
        var treasuryTiles = new long[7]; var manaRooms = new long?[7]; var manaVaults = new long?[7];
        Array.Fill(manaRooms, 0L); Array.Fill(manaVaults, 0L);
        long? goldInTerrain = 0; var unlimited = false;
        for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++)
        {
            var tile = GetTile(x, y);
            if (!statisticsTileRules.Value.TryGetValue((tile.Terrain, tile.Variant, tile.Owner), out var rule)) { issues.Add("UnknownTiles"); continue; }
            if (tile.Owner != rule.Owner && rule.Group != 1) issues.Add("NoncanonicalWorldOwners");
            if (rule.Group == 0)
            {
                var terrain = tile.Terrain is 30 or 254 ? (byte)1 : tile.Terrain;
                if (terrain is >= 1 and <= 5) terrainCounts[terrain] = terrainCounts.GetValueOrDefault(terrain) + 1;
                if (terrain == 6) goldInTerrain += terrainValues[6].Gold;
                if (terrain == 7) unlimited = true;
                if (terrain is not (31 or 35)) continue; // Closed mana / heroes' rest count as owned tiles, not room mana.
            }
            if (rule.Owner is < 1 or > 7) { issues.Add("InvalidOwners"); continue; }
            var player = rule.Owner - 1; owned[player]++;
            if (tile.Terrain == 9) walls[player]++; if (tile.Terrain == 8) claimed[player]++;
            if (rule.Group == 1 && tile.Terrain == 11) lairs[player]++;
            if (rule.Group == 1 && tile.Terrain == 10) treasuryTiles[player]++;
            if (rule.Group == 0) continue;
            var gain = terrainValues.TryGetValue(tile.Terrain, out var values) ? values.Mana : 0;
            if (tile.Terrain == 32) manaVaults[player] += gain;
            else if (manaRooms[player] is null || roomManaCap is null || manaRooms[player] < roomManaCap) manaRooms[player] += gain;
        }
        var allThings = new StatisticsThingAccumulator(); var players = Enumerable.Range(0, 7).Select(_ => new StatisticsThingAccumulator()).ToArray();
        long objectGold = 0, influxItems = 0;
        foreach (var record in records)
        {
            StatisticsThingKind? kind = null; uint id = 0, owner = 0; long level = 0;
            if (record.File.Signature == LegacyThings && record.Category != "Project.Opaque")
            {
                kind = Number(record, "node.type", -1) switch { 0 => StatisticsThingKind.Creature, 1 => StatisticsThingKind.Door, 2 => StatisticsThingKind.Trap, 3 => StatisticsThingKind.Object, 4 => StatisticsThingKind.SpecialItem, _ => null };
                id = unchecked((uint)Number(record, "id")); owner = unchecked((uint)Number(record, "owner"));
                if (kind == StatisticsThingKind.Creature) level = Number(record, "dataAll");
                if (kind == StatisticsThingKind.Object && id is >= 1 and <= 3) objectGold += Number(record, "dataAll");
            }
            else if (record.File.Signature == 0xBE && record.Category is "GAMEOBJECT" or "GAMETRAP" or "GAMEDOOR" or "GAMEHERO" or "GAMENEUTRAL" or "GAMEKEEPER")
            {
                if (record.File.UInt32(4) != 4) { issues.Add("UnsupportedThings"); continue; }
                var idField = ValueCatalogues.PrimaryField(record); if (idField is null) { issues.Add("UnsupportedThings"); continue; }
                id = uint.Parse(record.Read(idField), CultureInfo.InvariantCulture);
                kind = record.Category switch { "GAMEOBJECT" => id is >= 0x75 and <= 0x81 ? StatisticsThingKind.SpecialItem : StatisticsThingKind.Object,
                    "GAMETRAP" => StatisticsThingKind.Trap, "GAMEDOOR" => StatisticsThingKind.Door, _ => StatisticsThingKind.Creature };
                owner = record.Category switch { "GAMEHERO" => 1, "GAMENEUTRAL" => 2, _ => unchecked((uint)Number(record, "bGlobalPlayer")) };
                if (kind == StatisticsThingKind.Creature) level = Number(record, "bGlobalLevel");
                if (kind == StatisticsThingKind.Object && id is >= 1 and <= 3) objectGold += Number(record, "dwObjectGoldValue");
            }
            if (kind is null) continue;
            allThings.Add(kind.Value, id, level);
            if (owner is >= 1 and <= 7) players[owner - 1].Add(kind.Value, id, level); else issues.Add("InvalidOwners");
            if (kind == StatisticsThingKind.SpecialItem && id == 0x79) influxItems++;
        }
        if (records.Any(record => record.File.Signature == LegacyThings && record.Category == "Project.Opaque")) issues.Add("UnsupportedThings");
        var playerRows = Enumerable.Range(1, 7).Select(id =>
        {
            var index = id - 1; var point = HeartPosition((byte)id); TilePoint? heart = point == new TilePoint(0, 0) ? null : point;
            var capacity = treasuryTiles[index] == 0 ? 0 : treasuryTiles[index] * treasuryGold;
            if (heart is not null) capacity += 16 * heartGold;
            var mana = manaRooms[index]; if (heart is not null && mana < roomManaCap) mana += 30;
            if (mana > roomManaCap) mana = roomManaCap;
            mana += manaVaults[index]; if (mana > totalManaCap) mana = totalManaCap;
            if (roomManaCap is null || totalManaCap is null) mana = null;
            return new StatisticsPlayer((uint)id, players[index].Result(true), owned[index], walls[index], claimed[index], lairs[index], heart, capacity, mana);
        }).ToArray();
        var unsupportedScenarios = records.Any(record => record.File.Signature == LegacyEvents && record.Category == "Project.Opaque"
            || record.File.Signature == 0xD2 && record.File.UInt32(4) != 4);
        if (unsupportedScenarios) issues.Add("UnsupportedScenarios");
        var releaseCounts = records.Where(action => EntityReferences.EventType(action)?.Name == "ACTION_RELEASEBAND")
            .GroupBy(action => Number(action, IsLegacyProject ? "param1" : "Param1")).ToDictionary(group => group.Key, group => group.LongCount());
        var bandRows = new List<StatisticsBand>(); var bandCounts = new Dictionary<uint, (long Count, long Levels)>(); long bandImps = 0, bandDwarfs = 0, bandCreatures = 0, bandLevels = 0;
        foreach (var record in records.Where(IsBand))
        {
            if (!IsLegacyProject && (record.File.UInt32(4) != 4 || record.Length != 563)) { issues.Add("UnsupportedBands"); continue; }
            var identity = EntityReferences.Identity(record)!; var id = uint.Parse(record.Read(identity.Field), CultureInfo.InvariantCulture);
            var members = BandMembers(record); var active = members.TakeWhile(member => member.Active).ToArray();
            if (members.Skip(active.Length).Any(member => member.Active)) issues.Add("SparseBands");
            var count = active.GroupBy(member => member.CreatureID).ToDictionary(group => group.Key, group => (Count: (long)group.Count(), Levels: group.Sum(member => (long)member.Level)));
            var imps = count.GetValueOrDefault(1u).Count; var dwarfs = count.GetValueOrDefault(14u).Count; var creatures = active.Length - imps - dwarfs; var levels = active.Sum(member => (long)member.Level);
            uint? Preferred(Dictionary<uint, (long Count, long Levels)> values) => values.Where(pair => pair.Key is not (0 or 1 or 14)).OrderByDescending(pair => pair.Value.Count).ThenBy(pair => pair.Key).Select(pair => (uint?)pair.Key).FirstOrDefault();
            long? uses = unsupportedScenarios ? null : releaseCounts.GetValueOrDefault(id);
            bandRows.Add(new(record, id, record.Fields.FirstOrDefault(field => field.Name == "name") is { } name ? record.Read(name) : "", imps, dwarfs, creatures, active.Length == 0 ? 0 : levels / active.Length, Preferred(count), uses));
            bandImps += imps; bandDwarfs += dwarfs; bandCreatures += creatures; bandLevels += levels;
            foreach (var (type, value) in count) { var old = bandCounts.GetValueOrDefault(type); bandCounts[type] = (old.Count + value.Count, old.Levels + value.Levels); }
        }
        if (bandRows.GroupBy(row => row.ID).Any(group => group.Count() > 1)) issues.Add("DuplicateBandIDs");
        if (records.Any(record => record.File.Signature == LegacyBands && record.Category == "Project.Opaque")) issues.Add("UnsupportedBands");
        var bandMembers = bandImps + bandDwarfs + bandCreatures; var heroTypes = playerRows[0].Things.Types.Where(type => type.Kind == StatisticsThingKind.Creature).ToArray();
        var heroCount = heroTypes.Sum(type => type.Count); var heroLevels = heroTypes.Sum(type => type.LevelSum);
        var preferredBandCreature = bandCounts.Where(pair => pair.Key is not (0 or 1 or 14)).OrderByDescending(pair => pair.Value.Count).ThenBy(pair => pair.Key).Select(pair => (uint?)pair.Key).FirstOrDefault();
        return new(Width, Height, (long)Width * Height, Enumerable.Range(1, 5).Select(id => new StatisticsTerrain((byte)id, terrainCounts.GetValueOrDefault((byte)id), terrainCounts.GetValueOrDefault((byte)id) * 100m / ((long)Width * Height))).ToArray(),
            allThings.Result(false), goldInTerrain, objectGold, influxItems == 0 ? 0 : influxItems * specialGold, unlimited, playerRows, bandRows,
            new(bandImps, bandDwarfs, bandCreatures, bandMembers == 0 ? 0 : bandLevels / bandMembers, preferredBandCreature, bandCreatures + heroCount,
                bandMembers + heroCount == 0 ? 0 : (bandLevels + heroLevels) / (bandMembers + heroCount)), issues.Order(StringComparer.Ordinal).ToArray());
    }
}
