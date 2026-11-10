using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public enum MapRuleKind { Rooms, Traps, Doors, Spells, CreaturePool, CreatureModifier, Alliance }
public sealed record MapRuleKey(MapRuleKind Kind, uint Player, uint Element, uint Parameter = 0);
public sealed record MapRuleCell(MapRuleKey Key, ValueChoice Name, long? Value, int RecordCount, bool Editable);
public sealed record MapRuleChange(MapRuleKey Key, long? Value);
public sealed record MapRuleElement(uint ID, int Index, ValueChoice Name);

/// <summary>Original catalogue IDs, array positions and numeric states remain distinct.</summary>
public static class MapRules
{
    public static IReadOnlyList<ValueChoice> Availability { get; } =
    [new("255", "Not specified", "Не задано"), new("1", "Unavailable", "Недоступно"), new("2", "Researchable", "Можно исследовать"), new("3", "Available", "Доступно"), new("4", "Locked", "Заблокировано")];
    private static readonly IReadOnlyDictionary<MapRuleKind, IReadOnlyList<MapRuleElement>> elements = Enum.GetValues<MapRuleKind>().Where(kind => kind != MapRuleKind.Alliance).ToDictionary(kind => kind, kind =>
    {
        var catalogue = kind is MapRuleKind.CreaturePool or MapRuleKind.CreatureModifier
            ? LegacyCompilerRules.Elements("BadCreatures").Concat(LegacyCompilerRules.Elements("BadElites")).Concat(LegacyCompilerRules.Elements("Heros"))
            : LegacyCompilerRules.Elements(kind == MapRuleKind.Rooms ? "VarRooms" : kind.ToString());
        return (IReadOnlyList<MapRuleElement>)catalogue.Select((item, index) => new MapRuleElement(kind == MapRuleKind.CreatureModifier ? item.ID : item.Variable, index, ValueCatalogues.Named(item, item.ID.ToString(CultureInfo.InvariantCulture)))).ToArray();
    });
    public static IReadOnlyList<MapRuleElement> Elements(MapRuleKind kind) => elements.GetValueOrDefault(kind) ?? [];
    public static bool IsAvailability(MapRuleKind kind) => kind is MapRuleKind.Rooms or MapRuleKind.Traps or MapRuleKind.Doors or MapRuleKind.Spells;
    public static bool IsElite(MapRuleKey key) => key.Kind == MapRuleKind.CreatureModifier && Elements(key.Kind).Single(item => item.ID == key.Element).Index is >= 16 and <= 28;
    public static IEnumerable<MapRuleKey> Keys()
    {
        foreach (var kind in Enum.GetValues<MapRuleKind>())
        {
            if (kind == MapRuleKind.Alliance)
            { for (uint left = 1; left <= 7; left++) for (uint right = left; right <= 7; right++) yield return new(kind, left, right); }
            else if (kind == MapRuleKind.CreatureModifier)
            { foreach (var item in Elements(kind)) for (uint parameter = 1; parameter <= 4; parameter++) yield return new(kind, 0, item.ID, parameter); }
            else for (uint player = 1; player <= 7; player++) foreach (var item in Elements(kind)) yield return new(kind, player, item.ID);
        }
    }
    public static long? OriginalDefault(MapRuleKey key)
    {
        if (key.Kind == MapRuleKind.Alliance) return key.Player != key.Element && (key.Player == 2 || key.Element == 2) ? 1 : 0;
        if (key.Kind == MapRuleKind.CreatureModifier) return null;
        var index = Elements(key.Kind).Single(item => item.ID == key.Element).Index;
        if (key.Kind == MapRuleKind.CreaturePool)
            return key.Player <= 2 ? null : index is 13 or 14 ? 20 : index is >= 1 and <= 12 && index is not (6 or 11) ? 32 : null;
        if (key.Player <= 2) return null; // Table reset retains NPC availability; full player reset is separate.
        return key.Kind switch { MapRuleKind.Traps when index >= 11 => null, MapRuleKind.Doors when index >= 6 => null, MapRuleKind.Spells when index is >= 2 and <= 10 => 2, _ => 3 };
    }
}

public sealed partial class NativeMapDocument
{
    private static uint RuleNumber(NativeRecord record, string name) => uint.Parse(record.Read(record.Fields.Single(field => field.Name == name)), CultureInfo.InvariantCulture);
    private static string RuleCategory(MapRuleKind kind) => kind switch
    { MapRuleKind.CreaturePool => "GAMEVARCREATURECOUNT", MapRuleKind.CreatureModifier => "GAMEVARCREATURE", MapRuleKind.Alliance => "GAMEVARALLIANCE", _ => "GAMEVARTHINGSTATUS" };
    private static uint RuleType(MapRuleKind kind) => kind switch { MapRuleKind.Rooms => 1, MapRuleKind.Traps => 3, MapRuleKind.Doors => 4, MapRuleKind.Spells => 5, _ => 0 };
    private static string RuleValueField(MapRuleKind kind) => kind switch { MapRuleKind.CreaturePool => "dwCreatureCount", MapRuleKind.CreatureModifier => "dwCreatureNewValue", _ => "dwThingState" };
    private static MapRuleKey? NativeRuleKey(NativeRecord record)
    {
        if (record.Category == "GAMEVARCREATURECOUNT") return new(MapRuleKind.CreaturePool, RuleNumber(record, "dwGlobalPlayer"), RuleNumber(record, "dwGlobalCreatureID"));
        if (record.Category == "GAMEVARCREATURE") return new(MapRuleKind.CreatureModifier, 0, RuleNumber(record, "dwGlobalCreatureID"), RuleNumber(record, "dwCreatureParamType"));
        if (record.Category == "GAMEVARALLIANCE")
        { var left = RuleNumber(record, "dwGlobalPlayer"); var right = RuleNumber(record, "dwAllieWith"); return new(MapRuleKind.Alliance, Math.Min(left, right), Math.Max(left, right)); }
        if (record.Category != "GAMEVARTHINGSTATUS") return null;
        var kind = RuleNumber(record, "wThingType") switch { 1 => MapRuleKind.Rooms, 3 => MapRuleKind.Traps, 4 => MapRuleKind.Doors, 5 => MapRuleKind.Spells, _ => (MapRuleKind?)null };
        return kind is { } known ? new(known, RuleNumber(record, "wGlobalPlayer"), RuleNumber(record, "dwThingID")) : null;
    }
    private IEnumerable<NativeRecord> NativeRuleRecords(MapRuleKey key, IReadOnlyList<NativeRecord> records) => records.Where(record => record.File == MapVariableFile && record.Category == RuleCategory(key.Kind) && (key.Kind switch
    {
        MapRuleKind.CreaturePool => RuleNumber(record, "dwGlobalPlayer") == key.Player && RuleNumber(record, "dwGlobalCreatureID") == key.Element,
        MapRuleKind.CreatureModifier => RuleNumber(record, "dwGlobalCreatureID") == key.Element && RuleNumber(record, "dwCreatureParamType") == key.Parameter,
        MapRuleKind.Alliance => RuleNumber(record, "dwGlobalPlayer") == key.Player && RuleNumber(record, "dwAllieWith") == key.Element || RuleNumber(record, "dwGlobalPlayer") == key.Element && RuleNumber(record, "dwAllieWith") == key.Player,
        _ => RuleNumber(record, "wGlobalPlayer") == key.Player && RuleNumber(record, "wThingType") == RuleType(key.Kind) && RuleNumber(record, "dwThingID") == key.Element
    }));
    private static IReadOnlyDictionary<MapRuleKey, (NativeRecord? Record, NativeField? Field)> ProjectRuleSlots(IReadOnlyList<NativeRecord> records)
    {
        var info = records.SingleOrDefault(record => record.File.Signature == LegacyInfo);
        var players = records.Where(record => record.File.Signature == LegacyPlayers).ToArray();
        var fields = players.Concat(info is null ? [] : new[] { info }).ToDictionary(record => record, record => record.Fields.ToDictionary(field => field.Name));
        var keys = MapRules.Keys().Concat(MapRules.Keys().Where(key => key.Kind == MapRuleKind.Alliance && key.Player != key.Element).Select(key => key with { Player = key.Element, Element = key.Player }));
        return keys.ToDictionary(key => key, key =>
        {
            var record = key.Kind is MapRuleKind.CreatureModifier or MapRuleKind.Alliance ? info : players.ElementAtOrDefault((int)key.Player - 1);
            var index = key.Kind == MapRuleKind.Alliance ? 0 : MapRules.Elements(key.Kind).Single(item => item.ID == key.Element).Index;
            var name = key.Kind switch { MapRuleKind.Alliance => $"Allies[{key.Player - 1}][{key.Element - 1}]", MapRuleKind.CreatureModifier => $"CreaturesParams[{index}][{key.Parameter - 1}]", MapRuleKind.CreaturePool => $"CreaturesCount[{index}]", _ => $"{key.Kind}[{index}]" };
            return (record, record is null ? null : fields[record].GetValueOrDefault(name));
        });
    }
    public IReadOnlyList<MapRuleCell> MapRuleCells()
    {
        var records = Records(); var cells = new List<MapRuleCell>();
        var projectSlots = IsLegacyProject ? ProjectRuleSlots(records) : null;
        var nativeSlots = IsLegacyProject ? null : records.Where(record => record.File == MapVariableFile).Select(record => (Key: NativeRuleKey(record), Record: record)).Where(item => item.Key is not null)
            .GroupBy(item => item.Key!).ToDictionary(group => group.Key, group => group.Select(item => item.Record).ToArray());
        foreach (var key in MapRules.Keys())
        {
            long? value = null; int count; var editable = !MapRules.IsElite(key) && !(key.Kind == MapRuleKind.Alliance && key.Player == key.Element);
            if (IsLegacyProject)
            {
                var (record, field) = projectSlots![key]; count = field is null ? 0 : 1; editable &= field is not null;
                if (field is not null)
                {
                    value = long.Parse(record!.Read(field), CultureInfo.InvariantCulture);
                    if (key.Kind == MapRuleKind.Alliance)
                    {
                        var reverse = projectSlots[key with { Player = key.Element, Element = key.Player }];
                        value = value != 0 || reverse.Field is not null && reverse.Record!.Read(reverse.Field) != "0" ? 1 : 0;
                    }
                    else if (value == (key.Kind == MapRuleKind.CreaturePool ? -1 : key.Kind == MapRuleKind.CreatureModifier ? 0 : 255)) value = null;
                }
            }
            else
            {
                editable &= MapVariableFile is not null;
                var matches = nativeSlots!.GetValueOrDefault(key) ?? []; count = matches.Length;
                value = key.Kind == MapRuleKind.Alliance ? count > 0 ? 1 : 0 : matches.LastOrDefault() is { } record ? RuleNumber(record, RuleValueField(key.Kind)) : null;
                if (key.Kind != MapRuleKind.Alliance && value == (key.Kind == MapRuleKind.CreaturePool ? uint.MaxValue : key.Kind == MapRuleKind.CreatureModifier ? 0 : 255)) value = null;
            }
            var name = key.Kind == MapRuleKind.Alliance ? ValueCatalogues.Owners[(int)key.Element - 1] : MapRules.Elements(key.Kind).Single(item => item.ID == key.Element).Name;
            cells.Add(new(key, name, value, count, editable));
        }
        return cells;
    }
    /// <summary>Clear removes all exact native overrides; scalar updates retain
    /// the original loader's last-wins order and earlier duplicate payloads.</summary>
    public void SetMapRules(IEnumerable<MapRuleChange> changes) => ApplyMapRules(changes.ToArray(), false);
    public void ResetMapRules(IEnumerable<MapRuleKey> keys)
    {
        var selected = keys.Distinct().ToArray(); var verified = MapRules.Keys().ToHashSet();
        if (selected.Any(key => !verified.Contains(key))) throw new ArgumentException("Select verified map-rule keys before resetting.");
        ApplyMapRules(selected.Where(key => !(MapRules.IsAvailability(key.Kind) && key.Player <= 2) && !MapRules.IsElite(key))
            .Select(key => new MapRuleChange(key, MapRules.OriginalDefault(key))).ToArray(), true);
    }
    private void ApplyMapRules(IReadOnlyList<MapRuleChange> changes, bool reset)
    {
        var cells = MapRuleCells().ToDictionary(cell => cell.Key);
        if (changes.Select(change => change.Key).Distinct().Count() != changes.Count) throw new ArgumentException("Select each map-rule cell once.");
        foreach (var change in changes)
        {
            var resetDiagonal = reset && change.Key.Kind == MapRuleKind.Alliance && change.Key.Player == change.Key.Element;
            if (!cells.TryGetValue(change.Key, out var cell) || !cell.Editable && !(resetDiagonal && (IsLegacyProject ? cell.RecordCount == 1 : MapVariableFile is not null)))
                throw new ArgumentException("Select a verified editable map-rule cell (upgrade missing project fields first).");
            if (change.Value is { } value && (MapRules.IsAvailability(change.Key.Kind) ? value is < 1 or > 4 : change.Key.Kind == MapRuleKind.Alliance ? value is < 0 or > 1 : change.Key.Kind == MapRuleKind.CreaturePool ? value is < 0 or > 999 : value is < 0 or > 65535))
                throw new ArgumentOutOfRangeException(nameof(changes));
        }
        Edit("Change availability, creature pools/modifiers or alliances", () =>
        {
            var projectSlots = IsLegacyProject ? ProjectRuleSlots(Records()) : null;
            foreach (var change in changes)
            {
                var key = change.Key;
                if (IsLegacyProject)
                {
                    var (record, field) = projectSlots![key];
                    var value = change.Value ?? (key.Kind == MapRuleKind.CreaturePool ? -1 : key.Kind == MapRuleKind.CreatureModifier || key.Kind == MapRuleKind.Alliance ? 0 : 255);
                    SetField(record!, field!, value.ToString(CultureInfo.InvariantCulture));
                    if (key.Kind == MapRuleKind.Alliance && key.Player != key.Element)
                    {
                        var (other, opposite) = projectSlots[key with { Player = key.Element, Element = key.Player }];
                        if (opposite is null) throw new InvalidOperationException("Upgrade missing alliance fields first.");
                        SetField(other!, opposite, value.ToString(CultureInfo.InvariantCulture));
                    }
                }
                else
                    WriteNativeMapRule(change);
            }
        });
    }
    // Call only inside a transaction after preflight. Complete source player
    // defaults also use NPC availability zero, unlike the matrix's manual range.
    private void WriteNativeMapRule(MapRuleChange change)
    {
        var key = change.Key; var matches = NativeRuleRecords(key, Records()).ToArray();
        if (change.Value is null || key.Kind == MapRuleKind.Alliance && change.Value == 0 || key.Kind == MapRuleKind.CreatureModifier && change.Value == 0)
        { foreach (var match in matches.OrderByDescending(record => record.Offset)) DeleteRecord(match); return; }
        if (key.Kind == MapRuleKind.Alliance && matches.Length != 0) return;
        var record = matches.LastOrDefault();
        if (record is null)
        {
            record = AddMapVariableRecord(RecordTemplate.All.Single(template => template.Structure == RuleCategory(key.Kind)));
            void Set(string name, uint value) => SetField(record, record.Fields.Single(field => field.Name == name), value.ToString(CultureInfo.InvariantCulture));
            if (MapRules.IsAvailability(key.Kind)) { Set("wThingType", RuleType(key.Kind)); Set("wGlobalPlayer", key.Player); Set("dwThingID", key.Element); }
            else if (key.Kind == MapRuleKind.Alliance) { Set("dwGlobalPlayer", key.Player); Set("dwAllieWith", key.Element); }
            else { Set("dwGlobalCreatureID", key.Element); Set(key.Kind == MapRuleKind.CreaturePool ? "dwGlobalPlayer" : "dwCreatureParamType", key.Kind == MapRuleKind.CreaturePool ? key.Player : key.Parameter); }
        }
        if (key.Kind != MapRuleKind.Alliance) SetField(record, record.Fields.Single(field => field.Name == RuleValueField(key.Kind)), change.Value!.Value.ToString(CultureInfo.InvariantCulture));
    }
}
