using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record ScenarioDescription(string Text, bool Verified);

/// <summary>An immutable, read-only projection of one current scenario. A fresh
/// projection is built on edit/Undo/language refresh, never retained as a draft.
/// All 103 source cases are explicit; native operands use actual compiler fields.</summary>
public sealed class ScenarioDescriptions
{
    private readonly IReadOnlyDictionary<(NativeFile File, int Offset, int Length), ScenarioDescription> descriptions;
    private readonly string unavailable;
    private ScenarioDescriptions(Dictionary<(NativeFile, int, int), ScenarioDescription> items, string unavailable)
    { descriptions = new ReadOnlyDictionary<(NativeFile, int, int), ScenarioDescription>(items); this.unavailable = unavailable; }
    public ScenarioDescription Describe(NativeRecord record) => descriptions.GetValueOrDefault((record.File, record.Offset, record.Length)) ?? new(unavailable, false);
    public static ScenarioDescriptions Build(NativeMapDocument document, string language)
    {
        ArgumentNullException.ThrowIfNull(document);
        var context = new Formatter(document, language);
        return new(context.Records.Where(ScriptGraph.IsEvent).ToDictionary(record => (record.File, record.Offset, record.Length), context.Describe),
            context.T("Unverified event", "Непроверенное событие"));
    }

    private sealed class Formatter
    {
        private readonly NativeMapDocument document;
        private readonly bool russian;
        private readonly Dictionary<(ScenarioNameKind Kind, int Index), string> aliases = [];
        private readonly bool namesSupported;
        public IReadOnlyList<NativeRecord> Records { get; }
        private readonly IReadOnlyList<MapRegion> gates;
        private static readonly IReadOnlyDictionary<string, (string English, string Russian)> simple = new Dictionary<string, (string, string)>
        {
            ["TRIGGER_DOORDESTROYED"] = ("If the door is destroyed", "Если дверь разрушена"),
            ["TRIGGER_OBJECTCLAIMED"] = ("If the object is claimed", "Если объект захвачен"),
            ["TRIGGER_APPEAR"] = ("If the creature appears", "Если существо появляется"),
            ["TRIGGER_DEATH"] = ("If the creature dies", "Если существо погибает"),
            ["TRIGGER_SLAPPED"] = ("If the creature is slapped", "Если существо получает оплеуху"),
            ["TRIGGER_SEE"] = ("If the creature is attacked", "Если существо атаковано"),
            ["TRIGGER_JAILED"] = ("If the creature is imprisoned", "Если существо заключено в тюрьму"),
            ["TRIGGER_TORTURED"] = ("If the creature is tortured", "Если существо подвергается пыткам"),
            ["TRIGGER_CONVERTED"] = ("If the creature is converted", "Если существо обращено"),
            ["TRIGGER_TURNCOAT"] = ("If the creature is no longer neutral", "Если существо перестало быть нейтральным"),
            ["TRIGGER_HUNGRY"] = ("If the creature is hungry", "Если существо голодно"),
            ["TRIGGER_AFRAID"] = ("If the creature is afraid", "Если существо напугано"),
            ["TRIGGER_STEAL"] = ("If the creature has stolen something", "Если существо что-либо украло"),
            ["TRIGGER_GOAWAY"] = ("If the creature leaves the dungeon", "Если существо покидает подземелье"),
            ["TRIGGER_STUNNED"] = ("If the creature is unconscious", "Если существо потеряло сознание"),
            ["TRIGGER_TRANSITIONENDS"] = ("If the transition ends", "Если переход завершён"),
            ["TRIGGER_NOTHUNGRY"] = ("If the creature is no longer hungry", "Если существо больше не голодно"),
            ["TRIGGER_FIRED"] = ("If the creature is dismissed", "Если существо уволено"),
            ["TRIGGER_TAKE"] = ("If the creature has picked something up", "Если существо что-либо подобрало"),
            ["TRIGGER_GOLDDAY"] = ("If it is payday", "Если наступил день выплаты жалованья"),
            ["TRIGGER_TAKECREATURE"] = ("If the player picks up a creature", "Если игрок берёт существо в руку"),
            ["TRIGGER_FREECREATURE"] = ("If the player drops a creature", "Если игрок выпускает существо из руки"),
            ["TRIGGER_FIRECREATURE"] = ("If the player dismisses a creature", "Если игрок увольняет существо"),
            ["TRIGGER_HURTCREATURE"] = ("If the player gives a slap", "Если игрок даёт оплеуху"),
            ["TRIGGER_BREACHED"] = ("If the dungeon is breached", "Если в подземелье пробита брешь"),
            ["TRIGGER_ENNEMYBREACHED"] = ("If an enemy dungeon is breached", "Если во вражеском подземелье пробита брешь"),
            ["ACTION_SHOWCRYSTAL"] = ("Make the crystal appear", "Создать кристалл"),
            ["ACTION_VICTORY"] = ("End the level in victory", "Завершить уровень победой"),
            ["ACTION_DEFEAT"] = ("End the level in defeat", "Завершить уровень поражением"),
            ["ACTION_HASCRYSTAL"] = ("This creature holds the crystal", "Это существо несёт кристалл"),
            ["ACTION_POSSESSCREATURE"] = ("Possess the creature", "Вселиться в существо"),
            ["ACTION_MAKEHUNGRY"] = ("Make the creature hungry", "Сделать существо голодным"),
            ["ACTION_REMOVE"] = ("Make the creature disappear", "Убрать существо"),
            ["ACTION_SHOW"] = ("Centre the camera on the creature", "Навести камеру на существо")
        };
        private static readonly IReadOnlyDictionary<long, ValueChoice> terrainNames = ReadTerrainNames();
        private static IReadOnlyDictionary<long, ValueChoice> ReadTerrainNames()
        {
            using var stream = typeof(ScenarioDescriptions).Assembly.GetManifestResourceStream("Editor.ScenarioDescriptionTerrains")!;
            using var reader = new StreamReader(stream);
            var result = new Dictionary<long, ValueChoice>();
            while (reader.ReadLine() is { } line)
            {
                if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('|');
                if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace) || !result.TryAdd(long.Parse(parts[0], CultureInfo.InvariantCulture), new(parts[0], parts[1], parts[2])))
                    throw new InvalidDataException("Invalid description terrain vocabulary.");
            }
            if (!result.Keys.Order().SequenceEqual(LegacyCompilerRules.Elements("Terrain").Select(item => (long)(item.ID >> 24)).Order()))
                throw new InvalidDataException("Description terrain vocabulary differs from the original catalogue.");
            return new ReadOnlyDictionary<long, ValueChoice>(result);
        }
        public Formatter(NativeMapDocument document, string language)
        {
            this.document = document; russian = language == "ru"; Records = document.Records();
            gates = Records.Select(document.GetMapRegion).OfType<MapRegion>().Where(region => region.Kind == MapRegionKind.Gate).ToArray();
            try
            {
                var table = document.ReadScenarioNames(); namesSupported = table.Supported;
                foreach (var slot in table.Slots) aliases.Add((slot.Kind, slot.Index), slot.StoredName);
            }
            catch (DecoderFallbackException) { namesSupported = false; }
        }
        public string T(string english, string russian, params object[] values) => string.Format(CultureInfo.InvariantCulture, this.russian ? russian : english, values);
        private string Unknown(string english, string russian, long value) => T("Unknown {0} ({1})", "Неизвестное значение: {0} ({1})", T(english, russian), value);
        private static long Read(NativeRecord record, NativeField field)
        {
            if (field.Offset < 0 || field.Size <= 0 || field.Offset > record.Length - field.Size || field.BitMask != 0 ||
                field.Kind is not (NativeValueKind.Byte or NativeValueKind.Int16 or NativeValueKind.UInt16 or NativeValueKind.Int32 or NativeValueKind.UInt32))
                throw new InvalidOperationException("Unverified description operand.");
            var value = long.Parse(record.Read(field), CultureInfo.InvariantCulture);
            return field.Size == 4 && field.Kind == NativeValueKind.UInt32 ? unchecked((int)(uint)value) : value;
        }
        private static NativeField Field(NativeRecord record, string name) => record.Fields.Single(field => field.Name == name);
        public ScenarioDescription Describe(NativeRecord record)
        {
            if (record.Offset < 0 || record.Length <= 0 || record.Offset > record.File.Length - record.Length)
                return new(T("Unverified event layout", "Непроверенный формат события"), false);
            try
            {
                var type = EntityReferences.EventType(record);
                var project = record.Category is "Project.TRIGGER" or "Project.ACTION";
                var condition = project ? record.Category == "Project.TRIGGER" : record.File.Data.Span[record.Offset] == 0xD5;
                string Repeat(string text)
                {
                    if (!condition) return text;
                    var repeat = Read(record, Field(record, project ? "repeat" : "Repeat"));
                    return text + (repeat == 255 ? T(" (repeat indefinitely)", " (повторять бесконечно)") : repeat > 1 ? T(" (repeat {0} times)", " (число повторов: {0})", repeat) : "");
                }
                if (type is null)
                {
                    var ctrl = project ? Read(record, Field(record, "ctrl")) : record.File.Data.Span[record.Offset + 22];
                    return new(Repeat(Unknown(condition ? "condition" : "action", condition ? "условие" : "действие", ctrl)), false);
                }
                long P(int number)
                {
                    var field = Field(record, (project ? "param" : "Param") + number);
                    if (project)
                    {
                        if (field.Offset != 16 + (number - 1) * 4 || field.Size != 4 || field.Kind != NativeValueKind.Int32)
                            throw new InvalidOperationException("Unverified original LONG operand.");
                    }
                    else if (!type.Arguments.Contains(field)) throw new InvalidOperationException("Unverified compiler operand.");
                    return Read(record, field);
                }
                var text = simple.TryGetValue(type.Name, out var fixedText) ? T(fixedText.English, fixedText.Russian)
                    : type.Name.StartsWith("TRIGGER_", StringComparison.Ordinal) ? Condition(type.Name, P)
                    : Action(type.Name, P, record, project);
                return new(Repeat(text), true);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or OverflowException)
            { return new(T("Unverified event operands", "Непроверенные параметры события"), false); }
        }
        private string Comparison(long operation, int gender = 0) => operation switch
        {
            1 => T("less than", "меньше"), 2 => T("at most", "не больше"), 3 => T("equal to", gender == 1 ? "равна" : gender == 2 ? "равен" : "равно"),
            4 => T("greater than", "больше"), 5 => T("at least", "не меньше"), 6 => T("not equal to", gender == 1 ? "не равна" : gender == 2 ? "не равен" : "не равно"),
            _ => Unknown("comparison", "сравнение", operation)
        };
        private string CountComparison(long operation) => operation switch
        {
            1 => "fewer than", 2 => "at most", 3 => "exactly", 4 => "more than", 5 => "at least", 6 => "a number other than",
            _ => Unknown("comparison", "сравнение", operation)
        };
        private string Player(long id) => id switch
        {
            0 => T("any player", "Любой игрок"), 1 => T("the heroes", "Герои"), 2 => T("the neutral player", "Нейтральный игрок"),
            >= 3 and <= 7 => T("Keeper {0}", "Хранитель {0}", id - 2), _ => Unknown("player", "игрок", id)
        };
        private string Catalogue(string group, long id, string english, string russian)
        {
            var item = LegacyCompilerRules.Elements(group).FirstOrDefault(item => (group == "VarRooms" ? item.Variable : item.ID) == id);
            return item is null ? Unknown(english, russian, id) : ValueCatalogues.Named(item, id.ToString(CultureInfo.InvariantCulture)).Name(this.russian ? "ru" : "en");
        }
        private string Creature(long id) => id == 0 ? T("any creature", "Любое существо") : Catalogue("TrCreatures", id, "creature", "существо");
        private string CreatureType(long id) => id == 0 ? "" : T(" of type “{0}”", " типа «{0}»", Creature(id));
        private string Room(long id) => id switch { 0 => T("any room", "Любая комната"), 3 => T("Portal", "Портал"), 5 => T("Dungeon heart", "Сердце подземелья"), _ => Catalogue("VarRooms", id, "room", "комната") };
        private string Door(long id) => id == 0 ? T("any door", "Любая дверь") : Catalogue("Doors", id, "door", "дверь");
        private string Trap(long id) => id == 0 ? T("any trap", "Любая ловушка") : Catalogue("Traps", id, "trap", "ловушка");
        private string Spell(long id) => Catalogue("Spells", id, "spell", "заклинание");
        private string Terrain(long id) => id == 0 ? T("any terrain", "Любая земля") : terrainNames.TryGetValue(id, out var choice) ? choice.Name(russian ? "ru" : "en") : Unknown("terrain", "тип земли", id);
        private string Interface(long id) => id switch
        {
            241 => T("continuous hero invasion button", "Кнопка постоянного вторжения героев"),
            242 => T("mana indicator", "Индикатор маны"), 243 => T("gold indicator", "Индикатор золота"),
            244 => T("release heroes button", "Кнопка выпуска героев"), 245 => T("hero box close-up", "Крупный план окна героев"),
            247 => T("panel toggle", "Переключатель панели"), 248 => T("creatures button", "Кнопка существ"),
            249 => T("workshop objects button", "Кнопка изделий мастерской"), 250 => T("keeper spells button", "Кнопка заклинаний хранителя"),
            251 => T("rooms button", "Кнопка комнат"), _ => Unknown("interface element", "элемент интерфейса", id)
        };
        private string Element(long kind, long id) => kind switch
        {
            0 => Interface(id), 1 => T("room “{0}”", "комната «{0}»", Room(id)), 2 => T("creature “{0}”", "существо «{0}»", Creature(id)),
            3 => T("door “{0}”", "дверь «{0}»", Door(id)), 4 => T("trap “{0}”", "ловушка «{0}»", Trap(id)),
            5 => T("spell “{0}”", "заклинание «{0}»", Spell(id)), _ => Unknown("element kind", "категория элемента", kind) + " / " + id.ToString(CultureInfo.InvariantCulture)
        };
        private string Alias(ScenarioNameKind kind, long id)
        {
            if (id < 0 || id > (kind == ScenarioNameKind.Variable ? 128 : 16)) return Unknown(kind == ScenarioNameKind.Variable ? "variable" : "timer", kind == ScenarioNameKind.Variable ? "переменная" : "таймер", id);
            if (!namesSupported) return T("{0} (name metadata unavailable)", "{0} (метаданные имени недоступны)", id);
            var name = aliases.GetValueOrDefault((kind, (int)id), "");
            // Original atol + canonical length comparison, including its one
            // nonnumeric character at slot zero quirk. Bounded names are data,
            // never format strings, and no stored alias is rewritten.
            var match = Regex.Match(name, @"^[\t\n\v\f\r ]*([+-]?\d+)", RegexOptions.CultureInvariant);
            var numeric = match.Success && long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
            return name.Length == 0 || numeric == id && numeric.ToString(CultureInfo.InvariantCulture).Length == name.Length
                ? id.ToString(CultureInfo.InvariantCulture) : T("“{0}”", "«{0}»", name);
        }
        private string Variable(long id) => id == 128 ? T("score", "счёт") : T("variable {0}", "переменная {0}", Alias(ScenarioNameKind.Variable, id));
        private string VariableCase(long id, int grammaticalCase) => !russian ? Variable(id) : id == 128
            ? grammaticalCase == 1 ? "счёту" : grammaticalCase == 2 ? "счёта" : "счёт"
            : T("", grammaticalCase == 0 ? "переменную {0}" : "переменной {0}", Alias(ScenarioNameKind.Variable, id));
        private string Timer(long id, bool reserved = true) => reserved && id == 16 ? T("time limit", "лимит времени") : T("timer {0}", "таймер {0}", Alias(ScenarioNameKind.Timer, id));
        private string Duration(long seconds, int grammaticalCase = 0)
        {
            var values = new[] { seconds / 3600, seconds / 60 % 60, seconds % 60 };
            var english = new[] { "hour", "minute", "second" };
            var russian = new[] { new[] { "час", "часа", "часов" }, new[] { "минута", "минуты", "минут" }, new[] { "секунда", "секунды", "секунд" } };
            var parts = new List<string>();
            for (var index = 0; index < 3; index++)
            {
                var value = values[index];
                if (value == 0 && (index != 2 || parts.Count != 0)) continue;
                var absolute = Math.Abs(value);
                var form = absolute % 100 is >= 11 and <= 14 ? 2 : absolute % 10 == 1 ? 0 : absolute % 10 is >= 2 and <= 4 ? 1 : 2;
                var unit = russian[index][form];
                if (grammaticalCase == 1) unit = new[] { "часу", "минуте", "секунде" }[index];
                if (grammaticalCase == 1 && form != 0) unit = new[] { "часам", "минутам", "секундам" }[index];
                if (grammaticalCase == 2) unit = form == 0 ? new[] { "часа", "минуты", "секунды" }[index] : russian[index][2];
                if (grammaticalCase == 3 && form == 0) unit = new[] { "час", "минуту", "секунду" }[index];
                parts.Add(value.ToString(CultureInfo.InvariantCulture) + " " + (this.russian ? unit : english[index] + (absolute == 1 ? "" : "s")));
            }
            return string.Join(" ", parts);
        }
        private string NumberOf(string english, string russian, long op, long number, string qualifier = "") =>
            T("If the number of {0}{1} is {2} {3}", "Если число {0}{1} {2} {3}", T(english, russian), qualifier, Comparison(op), number);
        private string Destroyed(long id)
        {
            if (id == 0) return T("doors or traps", "дверей или ловушек");
            var trap = LegacyCompilerRules.Elements("Traps").Any(item => item.ID == id);
            var door = LegacyCompilerRules.Elements("Doors").Any(item => item.ID == id);
            // The source checks traps first even when IDs collide. Surface
            // both possibilities rather than inventing an unambiguous target.
            return trap && door ? T("doors/traps with ID {0} (“{1}” / “{2}”)", "дверей/ловушек с ID {0} («{1}» / «{2}»)", id, Door(id), Trap(id))
                : trap ? T("traps of type “{0}”", "ловушек типа «{0}»", Trap(id)) : T("doors of type “{0}”", "дверей типа «{0}»", Door(id));
        }
        private string Condition(string type, Func<int, long> p) => type switch
        {
            "TRIGGER_VARIABLE" => T("If {0} is {1} {2}", "Если {0} {1} {2}", Variable(p(2)), Comparison(p(1), p(2) == 128 ? 2 : 1), p(3) == 2 ? VariableCase(p(4), p(1) is 3 or 6 ? 1 : 2) : p(5)),
            "TRIGGER_TIMER" => T("If {0} is {1} {2}", "Если {0} {1} {2}", Timer(p(2)), Comparison(p(1), 2), Duration(p(5), p(1) is 3 or 6 ? 1 : 2)),
            "TRIGGER_CREATURES" => NumberOf("creatures", "существ", p(1), p(5), CreatureType(p(2)) + T(" owned by the player", " у игрока")),
            "TRIGGER_HAPPYCREATURES" => NumberOf("happy creatures", "довольных существ", p(1), p(5), CreatureType(p(2)) + T(" owned by the player", " у игрока")),
            "TRIGGER_ANGRYCREATURES" => NumberOf("angry creatures", "недовольных существ", p(1), p(5), CreatureType(p(2)) + T(" owned by the player", " у игрока")),
            "TRIGGER_LOSTCREATURES" => NumberOf("creatures killed", "существ, убитых", p(1), p(5), T(" by “{0}”", " игроком «{0}»", Player(p(4)))),
            "TRIGGER_KILLEDCREATURES" => NumberOf("creatures killed by the player", "убитых игроком существ", p(1), p(5), T(" belonging to “{0}”", ", принадлежащих игроку «{0}»,", Player(p(4)))),
            "TRIGGER_ROOMS" => NumberOf("room tiles", "клеток комнат", p(1), p(5), T(" of type “{0}” owned by the player", " типа «{0}» у игрока", Room(p(2)))),
            "TRIGGER_ROOMSSIZE" => T("If the player has a room “{0}” of {1} {2} tiles", "Если у игрока есть комната «{0}», число клеток которой {1} {2}", Room(p(2)), russian ? Comparison(p(1)) : CountComparison(p(1)), p(5)),
            "TRIGGER_ROOMSCONTENT" => NumberOf("objects", "объектов", p(1), p(5), T(" in a player room “{0}”", " в комнате игрока «{0}»", Room(p(2)))),
            "TRIGGER_ROOMSTYPES" => NumberOf("rooms", "комнат", p(1), p(5), T(" of type “{0}” owned by the player", " типа «{0}» у игрока", Room(p(2)))),
            "TRIGGER_DOORS" => NumberOf("door blueprints (built or waiting)", "чертежей дверей (готовых или ожидающих создания)", p(1), p(5), T(" of type “{0}” owned by the player", " типа «{0}» у игрока", Door(p(2)))),
            "TRIGGER_TRAPS" => NumberOf("trap blueprints (built or waiting)", "чертежей ловушек (готовых или ожидающих создания)", p(1), p(5), T(" of type “{0}” owned by the player", " типа «{0}» у игрока", Trap(p(2)))),
            "TRIGGER_DESTROYED" => T("If the player has destroyed {0} {1} {2}", "Если число уничтоженных игроком {2} {0} {1}", russian ? Comparison(p(1)) : CountComparison(p(1)), p(5), Destroyed(p(2))),
            "TRIGGER_SPELLS" => T("If the player has discovered the spell “{0}”", "Если игрок открыл заклинание «{0}»", Spell(p(2))),
            "TRIGGER_GOLDAMOUNT" => T("If the player's gold is {0} {1}", "Если количество золота у игрока {0} {1}", Comparison(p(1)), p(5)),
            "TRIGGER_GOLDMINED" => T("If the gold mined by the player is {0} {1}", "Если количество добытого игроком золота {0} {1}", Comparison(p(1)), p(5)),
            "TRIGGER_MANAAMOUNT" => T("If the player's mana is {0} {1}", "Если количество маны у игрока {0} {1}", Comparison(p(1)), p(5)),
            "TRIGGER_TIMEELAPSED" => T("If the elapsed time since the start is {0} {1}", "Если время с начала уровня {0} {1}", Comparison(p(1)), Duration(p(5), p(1) is 3 or 6 ? 1 : 2)),
            "TRIGGER_HURTED" => T("If the creature's remaining health is {0} {1}%", "Если оставшееся здоровье существа {0} {1}%", Comparison(p(1)), p(5)),
            "TRIGGER_CREAENTERAREA" => NumberOf("creatures entering the area", "существ, входящих в область", p(1), p(5), CreatureType(p(3)) + T(" belonging to “{0}”", ", принадлежащих игроку «{0}»,", Player(p(2)))),
            "TRIGGER_CLAIMAREA" => NumberOf("tiles in the area", "клеток области", p(1), p(5), T(" claimed by “{0}”", ", захваченных игроком «{0}»,", Player(p(2)))),
            "TRIGGER_CLAIMENTIREAREA" => T("If “{0}” claims the entire area", "Если игрок «{0}» захватил всю область", Player(p(2))),
            "TRIGGER_AREACONTENT" => NumberOf("tiles in the area", "клеток области", p(1), p(5), T(" of terrain “{0}” belonging to “{1}”", " типа «{0}», принадлежащих игроку «{1}»,", Terrain(p(3)), Player(p(2)))),
            "TRIGGER_BANDCREATED" => T("If hero band {0} has been created", "Если создан отряд героев {0}", p(2)),
            "TRIGGER_JAILEDINBAND" => NumberOf("imprisoned creatures", "заключённых существ", p(1), p(5), T(" in hero band {0}", " в отряде героев {0}", p(2))),
            "TRIGGER_DEATHSINBAND" => NumberOf("dead creatures", "погибших существ", p(1), p(5), T(" in hero band {0}", " в отряде героев {0}", p(2))),
            "TRIGGER_HEARTDESTROYED" => T("If the dungeon heart of “{0}” is destroyed", "Если сердце подземелья игрока «{0}» разрушено", Player(p(1))),
            "TRIGGER_CREATURESLEVEL" => NumberOf("creatures", "существ", p(1), p(5), T(" which reached level {0}", ", достигших уровня {0},", p(4))),
            "TRIGGER_CREATUREEXP" => T("If the creature's level is {0} {1}", "Если уровень существа {0} {1}", Comparison(p(1), 2), p(5)),
            "TRIGGER_CLICKELEMENT" => T("If the player clicks {0}", "Если игрок нажимает на элемент: {0}", Element(p(1), p(2))),
            "TRIGGER_SELECTAREA" => NumberOf("tiles in the area", "клеток области", p(1), p(5), T(" selected by “{0}”", ", выбранных игроком «{0}»,", Player(p(2)))),
            "TRIGGER_STUNNEDINBAND" => NumberOf("unconscious creatures", "существ без сознания", p(1), p(5), T(" in hero band {0}", " в отряде героев {0}", p(2))),
            "TRIGGER_SELECTALLAREA" => T("If “{0}” selects the entire area", "Если игрок «{0}» выбрал всю область", Player(p(2))),
            "TRIGGER_ENTERAREAINPOSS" => T("If “{0}” enters the area while possessing a creature", "Если игрок «{0}» вошёл в область, вселившись в существо", Player(p(2))),
            "TRIGGER_SLAPCOUNT" => T("If the number of slaps given by the player to their creatures is {0} {1}", "Если число оплеух, данных игроком своим существам, {0} {1}", Comparison(p(1)), p(5)),
            "TRIGGER_GROUPINPOSSESS" => NumberOf("creatures", "существ", p(1), p(5), T(" in the possession group", " в группе вселения")),
            _ => throw new InvalidOperationException("Missing original condition description: " + type)
        };
        private string Enable(long value) => value != 0 ? T("Enable", "Включить") : T("Disable", "Выключить");
        private string SetVariable(Func<int, long> p)
        {
            var target = Variable(p(1)); var mode = p(2);
            var value = mode is 0x0C or 0x14 or 0x24 ? !russian || p(5) == 128 ? Variable(p(5)) : "значение " + VariableCase(p(5), 2) : p(5).ToString(CultureInfo.InvariantCulture);
            return mode switch
            {
                0x0A or 0x0C => T("Set {0} to {1}", "Установить {0} в {1}", russian ? VariableCase(p(1), 0) : target, value),
                0x12 or 0x14 => T("Add {1} to {0}", "Прибавить {1} к {0}", russian ? VariableCase(p(1), 1) : target, value),
                0x22 or 0x24 => T("Subtract {1} from {0}", "Вычесть {1} из {0}", russian ? VariableCase(p(1), 2) : target, value),
                _ => T("Unknown variable operation {0}: {1}, value {2}", "Неизвестная операция с переменной {0}: {1}, значение {2}", mode, target, value)
            };
        }
        private string ChangeCreature(Func<int, long> p) => p(2) switch
        {
            0x0B => T("Make the creature destroy rooms belonging to “{0}”", "Приказать существу разрушать комнаты игрока «{0}»", Player(p(1))),
            0x0C => T("Make the creature destroy walls belonging to “{0}”", "Приказать существу разрушать стены игрока «{0}»", Player(p(1))),
            0x0D => T("Make the creature steal gold belonging to “{0}”", "Приказать существу красть золото игрока «{0}»", Player(p(1))),
            0x0E => T("Make the creature steal spells belonging to “{0}”", "Приказать существу красть заклинания игрока «{0}»", Player(p(1))),
            0x11 => T("Make the creature steal crates belonging to “{0}”", "Приказать существу красть ящики игрока «{0}»", Player(p(1))),
            0x12 => T("Make the creature kill creatures belonging to “{0}”", "Приказать существу убивать существ игрока «{0}»", Player(p(1))),
            0x13 => T("Make the creature attack “{0}”", "Приказать существу атаковать игрока «{0}»", Player(p(1))),
            0x16 => T("Make the creature stay in place", "Приказать существу оставаться на месте"),
            0x17 => T("Make the creature go to area {0}", "Приказать существу идти в область {0}", p(5)),
            0x1B => T("Make the creature attempt an escape", "Приказать существу попытаться сбежать"),
            _ => Unknown("creature order", "приказ существу", p(2))
        };
        private string DestroyGate(NativeRecord record, bool project, Func<int, long> p)
        {
            if (project) return T("Destroy hero gate {0}", "Уничтожить ворота героев {0}", p(5));
            long Coordinate(string name, int offset)
            {
                var field = Field(record, name);
                if (field.Offset != offset || field.Size != 2 || field.Kind != NativeValueKind.UInt16) throw new InvalidOperationException("Unverified native gate coordinate.");
                return Read(record, field) - 1;
            }
            var x = Coordinate("NativeArgument5", 12); var y = Coordinate("NativeArgument7", 14);
            var matches = gates.Count(region => region.Bounds.X == x && region.Bounds.Y == y && region.Record.Fields.FirstOrDefault(field => field.Name == "bGateType") is { } kind && region.Record.Read(kind) != "14");
            return T("Destroy hero gate at ({0},{1})", "Уничтожить ворота героев в ({0},{1})", x, y)
                + (matches == 1 && x >= 0 && y >= 0 && x < document.Width && y < document.Height ? "" : T(" (unresolved gate: {0} matches)", " (ворота не определены: совпадений {0})", matches));
        }
        private string Action(string type, Func<int, long> p, NativeRecord record, bool project) => type switch
        {
            "ACTION_ADDCREATURE" => T("Add creature “{0}” at level {1} at ({2},{3}) for “{4}”", "Добавить существо «{0}» уровня {1} в ({2},{3}) для игрока «{4}»", Creature(p(1)), p(3), p(5), p(7), Player(p(2))),
            "ACTION_ADDELEMENT" => T("{0} {1} for “{2}”", "{0} элемент: {1}, игрок «{2}»", p(4) != 0 ? T("Add", "Добавить") : T("Remove", "Удалить"), Element(p(2), p(3)), Player(p(1))),
            "ACTION_SETVARIABLE" => SetVariable(p),
            "ACTION_SETTIMER" => T("Initialize {0}", "Инициализировать {0}", Timer(p(1), false)),
            "ACTION_SHOWELEMENT" => T("{0} highlighting of {1}", "{0} подсветку элемента: {1}", Enable(p(3)), Element(p(1), p(2))) + (p(3) != 0 && p(5) != 0 ? T(" for {0}", " на {0}", Duration(p(5), 3)) : ""),
            "ACTION_RELEASEBAND" => T("Release hero band {0} in area {1}", "Выпустить отряд героев {0} в область {1}", p(1), p(5)) + (p(2) == 1 ? T(" (invaders)", " (захватчики)") : p(2) == 2 ? T(" (random invaders)", " (случайные захватчики)") : ""),
            "ACTION_CHANGEPARAMS" => ChangeCreature(p),
            "ACTION_SHOWAREA" => T("{0} highlighting of area {1}", "{0} подсветку области {1}", Enable(p(2)), p(1)),
            "ACTION_ENABLEFOG" => T("{0} fog of war in area {1}", "{0} туман войны в области {1}", Enable(p(2)), p(1)),
            "ACTION_ALLIES" => T("{0} alliance between “{1}” and “{2}”", "{0} союз между игроками «{1}» и «{2}»", p(3) != 0 ? T("Break", "Расторгнуть") : T("Establish", "Заключить"), Player(p(1)), Player(p(2))),
            "ACTION_CHANGESQUARE" => T("Change terrain at ({0},{1}) to “{2}” belonging to “{3}”", "Изменить землю в ({0},{1}) на «{2}», владелец — игрок «{3}»", p(5) - 1, p(7) - 1, Terrain(p(1)), Player(p(2))),
            "ACTION_DISPLAYTIMER" => T("{0} the time limit and remaining-time display", "{0} лимит времени и показ оставшегося времени", Enable(p(5))) + (p(5) != 0 ? T(" ({0})", " ({0})", Duration(p(5))) : ""),
            "ACTION_SAY" => T("Say speech #{0}", "Произнести реплику №{0}", p(1)) + (p(5) != 0 ? T(" (do not display text)", " (не показывать текст)") : ""),
            "ACTION_MOVECAMERA" => T("Place the camera above area {0}", "Переместить камеру над областью {0}", p(1)),
            "ACTION_ROTATECAMERA" => T("Rotate the camera by {0}° in area {1} for {2}", "Повернуть камеру на {0}° в области {1} за {2}", p(3), p(1), Duration(p(5), 3)),
            "ACTION_GENERATE" => T("Generate creature “{0}” at level {1} from a portal", "Выпустить из портала существо «{0}» уровня {1}", Creature(p(1)), p(2)),
            "ACTION_SHOWHEALTH" => T("Show health for {0}", "Показать здоровье на {0}", Duration(p(1), 3)),
            "ACTION_CAMERAPATH" => T("Run camera path {0} around area {1}", "Выполнить маршрут камеры {0} вокруг области {1}", p(1), p(2)) + (p(3) != 0 ? T(" (hide the ceiling)", " (скрыть потолок)") : T(" (show the ceiling)", " (показать потолок)")),
            "ACTION_DESTROYGATE" => DestroyGate(record, project, p),
            "ACTION_RUN" => p(1) != 0 ? T("Make the creature run", "Заставить существо бежать") : T("Stop making the creature run", "Прекратить заставлять существо бежать"),
            "ACTION_MUSICLEVEL" => T("Set music level to {0}", "Установить уровень музыки {0}", p(1)),
            "ACTION_ATTACK" => p(1) != 0 ? T("Make the creature aggressive", "Сделать существо агрессивным") : T("Make the creature passive", "Сделать существо пассивным"),
            "ACTION_PORTALS" => p(1) != 0 ? T("Open the portals", "Открыть порталы") : T("Close the portals", "Закрыть порталы"),
            "ACTION_CHANGEVIEW" => T("{0} widescreen view", "{0} широкоэкранный вид", Enable(p(1))),
            "ACTION_PRIMARYTARGET" => T("{0} primary target designation", "{0} обозначение главной цели", p(1) > 0 ? Enable(1) : Enable(0)) + (p(1) == 0 ? "" : p(1) == 2 ? T(" (imprison)", " (пленить)") : p(1) == 3 ? T(" (convert)", " (обратить)") : T(" (kill)", " (убить)")),
            "ACTION_MOODS" => T("{0} creature moods", "{0} настроение существ", Enable(p(1))),
            "ACTION_SYSTEMMESSAGES" => T("{0} system messages", "{0} системные сообщения", Enable(p(1))),
            "ACTION_SHOWNEXTROOM" => p(1) != 0 ? T("Show the next room: “{0}”", "Показать следующую комнату: «{0}»", Room(p(1))) : T("Disable the next-room display", "Выключить показ следующей комнаты"),
            "ACTION_TOGGLEEFFECT" => T("{0} effect generator {1}", "{0} генератор эффектов {1}", Enable(p(2)), p(1)),
            "ACTION_CHANGEOWNER" => T("The room at ({0},{1}) now belongs to “{2}”", "Комната в ({0},{1}) теперь принадлежит игроку «{2}»", p(5) - 1, p(7) - 1, Player(p(2))),
            "ACTION_SLAPSREMAINING" => p(5) != 0 ? T("Show remaining slaps: {0}", "Показать оставшиеся оплеухи: {0}", p(5)) : T("Disable the remaining-slaps display", "Выключить показ оставшихся оплеух"),
            "ACTION_TIMERSPEECH" => T("{0} the spoken remaining-time countdown", "{0} голосовой отсчёт оставшегося времени", Enable(p(1))),
            _ => throw new InvalidOperationException("Missing original action description: " + type)
        };
    }
}
