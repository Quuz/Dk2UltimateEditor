namespace DK2MapEditor.Core.Editing;

/// <summary>Original radio values, exposed as named property suggestions.
/// Imported values outside these choices remain editable and are never rewritten.</summary>
public static class ScenarioParameterChoices
{
    private static readonly IReadOnlyList<ValueChoice> comparisons = Array.AsReadOnly<ValueChoice>([
        new("1", "Less than", "Меньше"), new("2", "Less than or equal to", "Меньше либо равно"),
        new("3", "Equal to", "Равно"), new("4", "Greater than", "Больше"),
        new("5", "Greater than or equal to", "Больше либо равно"), new("6", "Not equal to", "Не равно")]);
    private static readonly HashSet<string> comparisonTypes = new(StringComparer.Ordinal)
    {
        "TRIGGER_VARIABLE", "TRIGGER_TIMER", "TRIGGER_TIMEELAPSED", "TRIGGER_HURTED", "TRIGGER_CREATUREEXP",
        "TRIGGER_CREATURES", "TRIGGER_HAPPYCREATURES", "TRIGGER_ANGRYCREATURES", "TRIGGER_LOSTCREATURES",
        "TRIGGER_KILLEDCREATURES", "TRIGGER_CREATURESLEVEL", "TRIGGER_ROOMS", "TRIGGER_ROOMSTYPES",
        "TRIGGER_ROOMSSIZE", "TRIGGER_ROOMSCONTENT", "TRIGGER_DOORS", "TRIGGER_TRAPS", "TRIGGER_DESTROYED",
        "TRIGGER_GOLDMINED", "TRIGGER_GOLDAMOUNT", "TRIGGER_MANAAMOUNT", "TRIGGER_SLAPCOUNT",
        "TRIGGER_GROUPINPOSSESS", "TRIGGER_CREAENTERAREA", "TRIGGER_CLAIMAREA", "TRIGGER_AREACONTENT",
        "TRIGGER_SELECTAREA", "TRIGGER_STUNNEDINBAND", "TRIGGER_JAILEDINBAND", "TRIGGER_DEATHSINBAND"
    };
    private static readonly IReadOnlyDictionary<(string Type, int Parameter), IReadOnlyList<ValueChoice>> options = ReadOptions();
    private static IReadOnlyDictionary<(string Type, int Parameter), IReadOnlyList<ValueChoice>> ReadOptions()
    {
        var result = new Dictionary<(string Type, int Parameter), IReadOnlyList<ValueChoice>>();
        void Add(string type, int parameter, params ValueChoice[] values) => result.Add((type, parameter), Array.AsReadOnly(values));
        ValueChoice Off(string english, string russian) => new("0", english, russian);
        ValueChoice On(string english, string russian) => new("1", english, russian);
        foreach (var type in new[] { "TRIGGER_DOORS", "TRIGGER_TRAPS" })
            Add(type, 3, new("9", "Blueprints", "Чертежи"), new("17", "Built", "Построенные"), new("1", "All", "Все"));
        Add("ACTION_ADDELEMENT", 4, Off("Remove", "Удалить"), On("Add", "Добавить"));
        Add("ACTION_SHOWAREA", 2, Off("Disable highlighting", "Выключить подсветку"), On("Enable highlighting", "Включить подсветку"));
        Add("ACTION_ENABLEFOG", 2, Off("Disable fog", "Выключить туман"), On("Enable fog", "Включить туман"));
        Add("ACTION_ALLIES", 3, Off("Establish alliance", "Заключить союз"), On("Break alliance", "Расторгнуть союз"));
        Add("ACTION_RELEASEBAND", 2, new("0", "Normal release", "Обычный выпуск"), new("1", "Invasion", "Вторжение"), new("2", "Invasion with random creature types", "Вторжение со случайными типами существ"));
        Add("ACTION_TOGGLEEFFECT", 2, Off("Disable effect", "Выключить эффект"), On("Enable effect", "Включить эффект"));
        Add("ACTION_SYSTEMMESSAGES", 1, Off("Disable game messages", "Выключить игровые сообщения"), On("Enable game messages", "Включить игровые сообщения"));
        Add("ACTION_MOODS", 1, Off("Disable creature moods", "Выключить настроения существ"), On("Enable creature moods", "Включить настроения существ"));
        Add("ACTION_TIMERSPEECH", 1, Off("Disable spoken countdown", "Выключить голосовой отсчёт"), On("Enable spoken countdown", "Включить голосовой отсчёт"));
        Add("ACTION_PORTALS", 1, Off("Close portals", "Закрыть порталы"), On("Open portals", "Открыть порталы"));
        Add("ACTION_CHANGEVIEW", 1, Off("Disable cinematic view", "Выключить кинематографический обзор"), On("Enable cinematic view", "Включить кинематографический обзор"));
        Add("ACTION_SHOWELEMENT", 3, Off("Disable stars/blinking", "Выключить звёздочки/мигание"), On("Enable stars/blinking", "Включить звёздочки/мигание"));
        Add("ACTION_SAY", 5, Off("Display speech text", "Показывать текст реплики"), On("Hide speech text", "Не показывать текст реплики"));
        Add("ACTION_CAMERAPATH", 3, Off("Show ceiling", "Показывать потолок"), On("Hide ceiling", "Скрывать потолок"));
        Add("ACTION_PRIMARYTARGET", 1, Off("Clear primary target", "Убрать главную цель"), On("Kill", "Убить"), new("2", "Imprison", "Пленить"), new("3", "Convert", "Обратить"));
        Add("ACTION_RUN", 1, Off("Stop making the creature run", "Не заставлять существо бежать"), On("Make the creature run", "Заставить существо бежать"));
        Add("ACTION_ATTACK", 1, Off("Creature does not attack", "Существо не атакует"), On("Creature attacks", "Существо атакует"));
        Add("ACTION_CHANGEPARAMS", 2,
            new("11", "Destroy enemy rooms", "Разрушать вражеские комнаты"), new("12", "Destroy enemy walls", "Разрушать вражеские стены"),
            new("13", "Steal enemy gold", "Красть золото врага"), new("14", "Steal enemy spells", "Красть заклинания врага"),
            new("17", "Steal enemy crates", "Красть ящики врага"), new("18", "Kill enemy creatures", "Убивать вражеских существ"),
            new("19", "Attack player", "Атаковать игрока"), new("22", "Stay in place", "Оставаться на месте"),
            new("23", "Attempt to reach area", "Попытаться добраться до области"), new("27", "Attempt escape", "Попытаться сбежать"));
        return result;
    }
    public static IReadOnlyList<ValueChoice> ForField(NativeRecord record, NativeField operand)
    {
        if (operand.BitMask != 0 || !record.Fields.Contains(operand)) return [];
        if (record.File.Signature == 0xD2 && record.File.UInt32(4) != 4) return [];
        var project = record.Category is "Project.TRIGGER" or "Project.ACTION";
        var prefix = project ? "param" : "Param";
        if (!operand.Name.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(operand.Name[prefix.Length..], out var parameter)) return [];
        var type = EntityReferences.EventType(record);
        if (type is null) return [];
        if (parameter == 1 && comparisonTypes.Contains(type.Name)) return comparisons;
        return options.TryGetValue((type.Name, parameter), out var choices) ? choices : [];
    }
}
