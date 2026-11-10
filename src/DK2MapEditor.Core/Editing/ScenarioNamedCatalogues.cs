using System.Globalization;

namespace DK2MapEditor.Core.Editing;

/// <summary>Source page contexts: selectable values only, never list headings.
/// A packed interface choice is split only when the parent draft is applied.</summary>
public static partial class ScenarioNamedCatalogues
{
    private sealed record Context(string Kind, bool Any = false, bool Extra = false, int Mode = 0);
    private static readonly IReadOnlyDictionary<(string Type, int Parameter), Context> contexts = Build();
    private static IReadOnlyDictionary<(string, int), Context> Build()
    {
        var result = new Dictionary<(string, int), Context>();
        void Add(string type, int parameter, string kind, bool any = false, bool extra = false, int mode = 0)
            => result.Add((type, parameter), new(kind, any, extra, mode));
        foreach (var type in new[] { "TRIGGER_CREATURES", "TRIGGER_HAPPYCREATURES", "TRIGGER_ANGRYCREATURES" }) Add(type, 2, "Creatures", true);
        Add("TRIGGER_CREAENTERAREA", 3, "Creatures", true);
        Add("ACTION_ADDCREATURE", 1, "Creatures", extra: true); Add("ACTION_GENERATE", 1, "Creatures", extra: true);
        foreach (var type in new[] { "TRIGGER_ROOMS", "TRIGGER_ROOMSSIZE", "TRIGGER_ROOMSCONTENT" }) Add(type, 2, "Rooms", true);
        Add("TRIGGER_ROOMSTYPES", 2, "Rooms", true, true); Add("ACTION_SHOWNEXTROOM", 1, "Rooms", true, mode: -1);
        Add("TRIGGER_DOORS", 2, "Doors", true); Add("TRIGGER_TRAPS", 2, "Traps", true);
        Add("TRIGGER_DESTROYED", 2, "Destroyed", true); Add("TRIGGER_SPELLS", 2, "Spells");
        foreach (var type in new[] { "TRIGGER_LOSTCREATURES", "TRIGGER_KILLEDCREATURES" }) Add(type, 4, "Players", true);
        Add("TRIGGER_HEARTDESTROYED", 1, "Players", true);
        foreach (var type in new[] { "TRIGGER_ENTERAREAINPOSS", "TRIGGER_CREAENTERAREA", "TRIGGER_CLAIMAREA", "TRIGGER_CLAIMENTIREAREA", "TRIGGER_SELECTAREA", "TRIGGER_SELECTALLAREA" }) Add(type, 2, "Players", true);
        Add("TRIGGER_AREACONTENT", 2, "Players");
        Add("ACTION_ALLIES", 1, "Players"); Add("ACTION_ALLIES", 2, "Players");
        Add("ACTION_CHANGEOWNER", 2, "Players"); Add("ACTION_CHANGEPARAMS", 1, "Players", true);
        Add("ACTION_ADDCREATURE", 2, "Players"); Add("ACTION_ADDELEMENT", 1, "Players"); Add("ACTION_CHANGESQUARE", 2, "Players");
        Add("TRIGGER_CREATUREEXP", 5, "Levels"); Add("TRIGGER_CREATURESLEVEL", 4, "Levels");
        Add("ACTION_ADDCREATURE", 3, "Levels"); Add("ACTION_GENERATE", 2, "Levels");
        Add("TRIGGER_AREACONTENT", 3, "Terrain", extra: true);
        Add("ACTION_CHANGESQUARE", 1, "Terrain");
        Add("TRIGGER_CLICKELEMENT", 1, "Elements", mode: 1); Add("ACTION_SHOWELEMENT", 1, "Elements"); Add("ACTION_ADDELEMENT", 2, "Elements", mode: -1);
        foreach (var type in new[] { "TRIGGER_BANDCREATED", "TRIGGER_STUNNEDINBAND", "TRIGGER_JAILEDINBAND", "TRIGGER_DEATHSINBAND" }) Add(type, 2, "Bands");
        Add("ACTION_RELEASEBAND", 1, "Bands");
        foreach (var type in new[] { "ACTION_SHOWAREA", "ACTION_ENABLEFOG", "ACTION_MOVECAMERA", "ACTION_ROTATECAMERA" }) Add(type, 1, "Areas");
        Add("ACTION_RELEASEBAND", 5, "Areas"); Add("ACTION_CAMERAPATH", 2, "Areas"); Add("ACTION_CHANGEPARAMS", 5, "Areas");
        Add("ACTION_TOGGLEEFFECT", 1, "Effects"); Add("ACTION_DESTROYGATE", 5, "Gates");
        return result;
    }
    private static ValueChoice Option(long value, string english, string russian) => new(value.ToString(CultureInfo.InvariantCulture), english, russian);
    private static IReadOnlyList<ValueChoice> Elements(string group, bool variable = true) => ValueCatalogues.Elements(group, variable);
    private static readonly Lazy<IReadOnlyDictionary<uint, ValueChoice>> terrainNames = new(() =>
    {
        using var stream = typeof(ScenarioNamedCatalogues).Assembly.GetManifestResourceStream("Editor.ScenarioDescriptionTerrains")!;
        using var reader = new StreamReader(stream); var result = new Dictionary<uint, ValueChoice>();
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('|'); var value = uint.Parse(parts[0], CultureInfo.InvariantCulture);
            result.Add(value, new(parts[0], parts[1], parts[2]));
        }
        return result;
    });
    public static IReadOnlyList<ValueChoice> Choices(string type, int parameter, NativeMapDocument? document = null)
    {
        if (!contexts.TryGetValue((type, parameter), out var context)) return [];
        var result = new List<ValueChoice>();
        if (context.Any) result.Add(context.Kind switch
        {
            "Rooms" when context.Mode == -1 => Option(0, "No room", "Нет комнаты"),
            "Rooms" => Option(0, "Any room", "Любая комната"),
            "Creatures" => Option(0, "Any creature", "Любое существо"),
            "Players" => Option(0, "Any player", "Любой игрок"),
            "Doors" => Option(0, "Any door", "Любая дверь"), "Traps" => Option(0, "Any trap", "Любая ловушка"),
            _ => Option(0, "Any door or trap", "Любая дверь или ловушка")
        });
        switch (context.Kind)
        {
            case "Creatures":
                result.AddRange(Elements("BadCreatures", false));
                if (context.Extra) result.AddRange(Elements("BadElites", false));
                result.AddRange(Elements("Heros", false)); break;
            case "Rooms":
                if (context.Extra) { result.Add(Option(5, "Dungeon heart", "Сердце подземелья")); result.Add(Option(3, "Portal", "Портал")); }
                result.AddRange(Elements("VarRooms")); break;
            case "Doors": case "Traps": case "Spells": result.AddRange(Elements(context.Kind)); break;
            case "Destroyed":
                // Door and trap IDs overlap. Keep every original row/category label.
                result.AddRange(Elements("Doors").Select(value => value with { English = "Door · " + value.English, Russian = "Дверь · " + value.Russian }));
                result.AddRange(Elements("Traps").Select(value => value with { English = "Trap · " + value.English, Russian = "Ловушка · " + value.Russian })); break;
            case "Players":
                var names = document?.PlayerSettingsRows().Where(row => row.ID is >= 1 and <= 7).GroupBy(row => row.ID).ToDictionary(group => group.Key, group => group.Last().Name);
                for (uint id = 1; id <= 7; id++)
                {
                    var value = ValueCatalogues.Owners[(int)id - 1]; var name = names?.GetValueOrDefault(id);
                    result.Add(string.IsNullOrEmpty(name) ? value : value with { English = name, Russian = name });
                }
                break;
            case "Levels": result.AddRange(Enumerable.Range(1, 10).Select(value => Option(value, "Level " + value, "Уровень " + value))); break;
            case "Terrain":
                result.AddRange(LegacyCompilerRules.Elements("Terrain").Where(row => context.Extra || (byte)row.Variable is not (3 or 5)).Select(row => terrainNames.Value[row.ID >> 24])); break;
            case "Elements": result.AddRange(InterfaceElements(context.Mode)); break;
            case "Bands": case "Areas": case "Effects": case "Gates":
                if (document is not null)
                {
                    var kind = context.Kind switch { "Bands" => EntityKind.Band, "Areas" => EntityKind.Area, "Gates" => EntityKind.Gate, _ => EntityKind.Effect };
                    var identities = document.Records().Select(EntityReferences.Identity).OfType<EntityIdentity>().Where(row => row.Kind == kind).ToArray();
                    var counts=identities.GroupBy(row=>row.Record.Read(row.Field)).ToDictionary(group=>group.Key,group=>group.Count());
                    foreach (var row in identities)
                    {
                        var value = row.Record.Read(row.Field);
                        result.Add(new(value,EntityReferences.DisplayIdentity(row,"en",counts[value]),EntityReferences.DisplayIdentity(row,"ru",counts[value])));
                    }
                }
                break;
        }
        return result.AsReadOnly();
    }
    public static IReadOnlyList<ValueChoice> InterfaceElements(int mode)
    {
        if (mode is not (-1 or 0 or 1)) throw new ArgumentOutOfRangeException(nameof(mode));
        var result = new List<ValueChoice>();
        void Panel(int id, string en, string ru) => result.Add(Option(id, en, ru));
        void Group(string group, int category, string en, string ru)
        {
            foreach (var value in Elements(group))
            {
                var id = int.Parse(value.Value, CultureInfo.InvariantCulture);
                if (group == "Spells" && mode == 0 && id == 5) continue;
                result.Add(Option((category << 16) | id, en + " · " + value.English, ru + " · " + value.Russian));
            }
        }
        if (mode == 0) { Panel(242, "Mana indicator", "Индикатор маны"); Panel(243, "Gold indicator", "Индикатор золота"); }
        if (mode == 1) { Panel(241, "Continuous hero invasion", "Постоянное вторжение героев"); Panel(244, "Release heroes", "Выпустить героев"); Panel(245, "Hero box close-up", "Крупный план окна героев"); Panel(247, "Panel toggle", "Переключатель панели"); }
        if (mode != -1) { Panel(248, "Creatures panel", "Панель существ"); Panel(251, "Rooms panel", "Панель комнат"); }
        Group("VarRooms", 1, "Room", "Комната");
        if (mode != -1) Panel(250, "Keeper spells panel", "Панель заклинаний хранителя");
        Group("Spells", 5, "Spell", "Заклинание");
        if (mode != -1) Panel(249, "Workshop objects panel", "Панель изделий мастерской");
        Group("Doors", 3, "Door", "Дверь"); Group("Traps", 4, "Trap", "Ловушка");
        return result.AsReadOnly();
    }
    public static bool HasFieldContext(NativeRecord record, NativeField field)
    {
        if (field.BitMask != 0 || !record.Fields.Contains(field) || record.File.Signature == 0xD2 && record.File.UInt32(4) != 4) return false;
        var type = EntityReferences.EventType(record); if (type is null) return false;
        var prefix = record.Category is "Project.TRIGGER" or "Project.ACTION" ? "param" : "Param";
        if (!field.Name.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(field.Name[prefix.Length..], out var parameter)) return false;
        return contexts.ContainsKey((type.Name, parameter)) || type.Name is "TRIGGER_CLICKELEMENT" or "ACTION_SHOWELEMENT" && parameter == 2 || type.Name == "ACTION_ADDELEMENT" && parameter == 3;
    }
    public static IReadOnlyList<ValueChoice> ForField(NativeRecord record, NativeField field, NativeMapDocument? document = null)
    {
        if (field.BitMask != 0 || !record.Fields.Contains(field) || record.File.Signature == 0xD2 && record.File.UInt32(4) != 4) return [];
        var type = EntityReferences.EventType(record); if (type is null) return [];
        var prefix = record.Category is "Project.TRIGGER" or "Project.ACTION" ? "param" : "Param";
        if (!field.Name.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(field.Name[prefix.Length..], out var parameter)) return [];
        if (type.Name is "TRIGGER_CLICKELEMENT" or "ACTION_SHOWELEMENT" or "ACTION_ADDELEMENT")
        {
            var primary = type.Name == "ACTION_ADDELEMENT" ? 2 : 1;
            var values = Choices(type.Name, primary, document);
            if (parameter == primary) return values.Select(value => int.Parse(value.Value, CultureInfo.InvariantCulture) >> 16).Distinct().Select(category => Option(category, category switch { 0 => "Interface", 1 => "Rooms", 3 => "Doors", 4 => "Traps", _ => "Spells" }, category switch { 0 => "Интерфейс", 1 => "Комнаты", 3 => "Двери", 4 => "Ловушки", _ => "Заклинания" })).ToArray();
            if (parameter == primary + 1)
            {
                var categoryField = record.Fields.FirstOrDefault(operand => operand.Name == prefix + primary);
                if (categoryField is null || !int.TryParse(record.Read(categoryField), out var category)) return [];
                return values.Where(value => int.Parse(value.Value, CultureInfo.InvariantCulture) >> 16 == category)
                    .Select(value => value with { Value = (int.Parse(value.Value, CultureInfo.InvariantCulture) & 65535).ToString(CultureInfo.InvariantCulture) }).ToArray();
            }
            return [];
        }
        return Choices(type.Name, parameter, document);
    }
}
