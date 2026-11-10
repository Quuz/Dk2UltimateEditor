using System.Collections.ObjectModel;
using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record PlayerAIParameter(string Name,string Group,string English,string Russian,int Minimum,int Maximum,IReadOnlyList<ValueChoice> Choices,byte Mask = 0)
{
    public string Label(string language) => language == "ru" ? Russian : English;
}

/// <summary>Manual controls in original Properties_MapComputerProc2–8; room order is separate.</summary>
public static class PlayerAIOptions
{
    public static IReadOnlyList<ValueChoice> Groups { get; } = Array.AsReadOnly(new ValueChoice[]
    {
        new("Rooms","Room construction","Строительство комнат"),new("Traps","Traps and doors","Ловушки и двери"),
        new("Spells","Keeper spells","Заклинания хранителя"),new("Creatures","Creatures","Существа"),
        new("FirstAttack","First attack","Первая атака"),new("Battle","Battles","Сражения"),new("Misc","Other settings","Прочие настройки")
    });
    private static IReadOnlyList<ValueChoice> Choices(params (string English,string Russian)[] labels) => Array.AsReadOnly(labels.Select((label,index) => new ValueChoice(index.ToString(CultureInfo.InvariantCulture),label.English,label.Russian)).ToArray());
    private static readonly IReadOnlyList<ValueChoice> offOn = Choices(("No","Нет"),("Yes","Да"));
    private static IReadOnlyList<ValueChoice> Scale(string lowEnglish,string lowRussian,string highEnglish,string highRussian) => Choices(("0 · " + lowEnglish,"0 · " + lowRussian),("1","1"),("2","2"),("3","3"),("4 · " + highEnglish,"4 · " + highRussian));
    public static IReadOnlyList<PlayerAIParameter> Parameters { get; } = CreateParameters();
    public static IReadOnlyList<ValueChoice> Rooms { get; } = Array.AsReadOnly(ValueCatalogues.Elements("CPURooms").ToArray());
    /// <summary>CPURooms uses its own IDs, not terrain IDs or the current priority position.</summary>
    public static int? RoomImageResource(long id)
    {
        var resource = LegacyCompilerRules.Elements("CPURooms").FirstOrDefault(room => room.ID == id)?.Icon;
        return resource is > 0 ? resource : null;
    }
    public static bool ValidRoomOrder(IEnumerable<long> order) => order.Order().SequenceEqual(Rooms.Select(room => long.Parse(room.Value,CultureInfo.InvariantCulture)).Order());
    private static IReadOnlyList<PlayerAIParameter> CreateParameters()
    {
        var result = new List<PlayerAIParameter>();
        void Number(string group,string name,string en,string ru,int maximum = 255,int minimum = 0) => result.Add(new(name,group,en,ru,minimum,maximum,Array.Empty<ValueChoice>()));
        void Choice(string group,string name,string en,string ru,IReadOnlyList<ValueChoice> choices,byte mask = 0) => result.Add(new(name,group,en,ru,0,choices.Count - 1,choices,mask));
        void Flag(string group,string name,string en,string ru,byte mask = 0) => Choice(group,name,en,ru,offOn,mask);
        Number("Rooms","PlayerResearchedRoom","Wait after researching a room (s)","Ожидание после исследования комнаты (с)");
        Number("Rooms","PlayerRebuildRoom","Wait before rebuilding a lost room (s)","Ожидание перед восстановлением потерянной комнаты (с)");
        Choice("Rooms","PlayerExpandMode","When a room needs more space","Если комнате нужно больше места",Choices(("Expand the room","Расширить комнату"),("Build another room","Построить новую комнату"),("Do nothing","Ничего не делать")));
        Number("Rooms","PlayerDefaultTiles","Default room size (tiles)","Обычный размер комнаты (клетки)");
        Number("Rooms","PlayerTilesBetweenRooms","Space between rooms (tiles)","Расстояние между комнатами (клетки)");
        Choice("Rooms","PlayerDistance","Room placement distance","Расположение комнат",Choices(("Nearby","Рядом"),("Random","Случайное")));
        Choice("Rooms","PlayerCorridorStyle","Corridor angles","Углы коридоров",Choices(("Any angle","Любые углы"),("45° and 90°","45° и 90°"),("90°","90°")));
        Number("Rooms","PlayerTilesFromHeart","Dig to neutral rooms within heart distance (tiles)","Копать к нейтральным комнатам вблизи сердца (клетки)");
        Number("Rooms","PlayerTilesFromClaimed","Dig to neutral rooms within claimed-area distance (tiles)","Копать к нейтральным комнатам вблизи своей территории (клетки)");
        Flag("Rooms","PlayerCreateEmptyAreas","Create empty areas when idle","Создавать пустые области при бездействии");
        Flag("Rooms","PlayerBuildBiggerLair","Build a larger lair after claiming a portal","Строить увеличенное логово после захвата портала");
        Flag("Rooms","PlayerSellCapturedRoom","Sell captured rooms when short of gold","Продавать захваченные комнаты при нехватке золота");
        Number("Rooms","PlayerFlexibility","Construction flexibility (%)","Гибкость строительства (%)",100);
        Choice("Traps","PlayerTrapUseStyle","Trap placement style","Стиль размещения ловушек",Scale("Aggressive","Агрессивный","Defensive","Оборонительный"));
        Choice("Traps","PlayerPreference","Trap/door preference","Предпочтение ловушек или дверей",Scale("Traps","Ловушки","Doors","Двери"));
        Choice("Traps","PlayerDoorUsage","Door choice","Выбор двери",Choices(("Best door","Лучшая дверь"),("Worst door","Худшая дверь"),("Random door","Случайная дверь")));
        Flag("Traps","PlayerBoulderInCorridors","Place boulder traps in long corridors","Ставить ловушки-валуны в длинных коридорах");
        Flag("Traps","PlayerBoulderInAngles","Place boulder traps in corridors leading to breaches","Ставить ловушки-валуны в коридорах к проломам");
        Number("Traps","PlayerUseTrapsnDoors","Chance of using traps/doors (%)","Вероятность использования ловушек и дверей (%)",100);
        Choice("Spells","PlayerSpellsInBattle","Spell frequency in battles","Частота заклинаний в бою",Scale("Often","Часто","Rarely","Редко"));
        Choice("Spells","PlayerSpellsPower","Preferred spell power","Предпочтительная сила заклинаний",Scale("Powerful","Мощные","Weak","Слабые"));
        Choice("Spells","PlayerSpellsSightOfEvil","Sight of Evil targets","Цели «Злобного взора»",Choices(("Nowhere","Нигде"),("Anywhere","Где угодно"),("Enemy territory","Территория врага")));
        Choice("Spells","PlayerSpellsCallToArms","Call to Arms targets","Цели «Призыва к оружию»",Choices(("Towards the dungeon heart","В направлении сердца"),("On the dungeon heart","На сердце подземелья"),("On the heart of an inhabited dungeon","На сердце обитаемого подземелья")));
        Flag("Spells","PlayerSpellsLightning","Use Lightning when the enemy is on water","Использовать молнию, когда враг находится на воде");
        Flag("Creatures","PlayerCreaturesHappyUnhappy","Try to make unhappy creatures happy","Стараться развеселить недовольных существ");
        Flag("Creatures","PlayerCreaturesHappyUngry","Try to make angry creatures happy","Стараться успокоить разъярённых существ");
        Flag("Creatures","PlayerCreaturesDisposeUngry","Dismiss angry creatures","Изгонять разъярённых существ");
        Flag("Creatures","PlayerCreaturesDisposeRubish","Dismiss useless creatures when better ones arrive","Изгонять ненужных существ при появлении лучших");
        Choice("Creatures","PlayerCreaturesSackMethod","Dismissal method","Способ избавления от существ",Choices(("Drop into a portal","Бросить в портал"),("Sacrifice","Принести в жертву")));
        Number("Creatures","PlayerCreaturesImps","Maximum imps","Максимум бесов");
        Flag("Creatures","PlayerCreaturesSlap","Do not slap creatures","Не бить существ",0x20);
        Flag("FirstAttack","PlayerUseLevelInFirstAttack","Require creature level","Учитывать уровень существ");
        Number("FirstAttack","PlayerFirstAttackLevel","Required creature level","Требуемый уровень существ",10);
        Number("FirstAttack","PlayerFirstAttackCount","Minimum own creature count","Минимальное число своих существ");
        Flag("FirstAttack","PlayerUseThreatInFirstAttack","Require threat advantage over the enemy","Требовать превосходства угрозы над врагом");
        Number("FirstAttack","PlayerFirstAttackThreat","Required threat advantage over enemy (%)","Требуемое превосходство угрозы над врагом (%)");
        Flag("FirstAttack","PlayerFirstAttackAllRooms","Require all rooms built","Требовать строительства всех комнат");
        Flag("FirstAttack","PlayerFirstAttackAllSpells","Require all spells discovered","Требовать открытия всех заклинаний");
        Choice("FirstAttack","PlayerFirstAttackBreach","Breach destination","Цель прорыва",Choices(("Enemy dungeon heart","Сердце врага"),("Any room","Любая комната"),("Weak room","Слабая комната"),("Strong room","Сильная комната")));
        Choice("FirstAttack","PlayerFirstAttackDig","Dig destination","Направление подкопа",Choices(("Far from the enemy heart","Далеко от сердца врага"),("Anywhere","Где угодно"),("Into the dungeon heart","В сердце подземелья"),("As close as possible to the heart","Как можно ближе к сердцу")));
        Number("FirstAttack","PlayerFirstAttackPoints","Attack points","Точки атаки");
        Number("FirstAttack","PlayerFirstAttackCreatures","Forces used (%)","Используемые силы (%)",100);
        Flag("FirstAttack","PlayerFirstAttackOnlyAttackers","Counterattack only","Только контратаковать");
        Flag("FirstAttack","PlayerFirstAttackDontAttack","Do not attack","Не атаковать");
        Number("Battle","PlayerBattleDropped","Dropped-creature threat relative to enemy (%)","Угроза высаживаемых существ относительно врага (%)");
        Choice("Battle","PlayerBattleSpellType","Battle spell style","Стиль боевых заклинаний",Scale("Aggressive","Агрессивный","Defensive","Оборонительный"));
        Number("Battle","PlayerBattlePrison","Enemy creatures to imprison (%)","Вражеские существа для захвата в плен (%)",100);
        Flag("Battle","PlayerBattleRemove","Withdraw when outnumbered without reinforcements","Отступать при численном превосходстве врага без подкреплений");
        Number("Battle","PlayerBattleHealth","Move injured creatures off own land to lair/temple at health (%)","Уводить раненых вне своей земли в логово или храм при здоровье (%)",100);
        Number("Battle","PlayerBattleCallToHarmsPlace","Place Call to Arms above threat","Ставить «Призыв к оружию» при угрозе выше",32767);
        Number("Battle","PlayerBattleCallToHarmsRemove","Remove Call to Arms after (s)","Убирать «Призыв к оружию» через (с)",32767);
        Number("Battle","PlayerBattleCallToHarmsRemoveNoCreature","Remove when available creatures below","Убирать, если доступных существ меньше");
        Number("Battle","PlayerBattleCallToHarmsRemoveNoDanger1","Remove when nearby enemies below","Убирать, если ближайших врагов меньше");
        Number("Battle","PlayerBattleCallToHarmsRemoveNoDanger2","Nearby-enemy radius (tiles)","Радиус поиска ближайших врагов (клетки)");
        Number("Battle","PlayerBattleWait","Delay between attacks (s)","Ожидание между атаками (с)",32767);
        Choice("Misc","PlayerLibraryUsage","Library usage probability","Вероятность использования библиотеки",Choices(("Very likely","Очень вероятно"),("Likely","Вероятно"),("Unlikely","Маловероятно")));
        Choice("Misc","PlayerJailedFate","Prisoner fate","Судьба пленников",Choices(("Torture","Пытки"),("Death","Смерть"),("Combat pit","Боевая арена"),("Sacrifice","Жертвоприношение")));
        Number("Misc","PlayerMineGoldAmount","Mine gold until amount exceeds","Добывать золото до суммы выше",65535);
        Number("Misc","PlayerFindSpecialsTiles","Tiles explored for secrets","Клетки для поиска секретов",32767);
        Number("Misc","PlayerFindSpecialsExplore","Chance of searching for secrets (%)","Вероятность поиска секретов (%)",100);
        Number("Misc","PlayerFindSpecialsChances","Chance of finding secrets (%)","Вероятность обнаружения секретов (%)",100);
        Number("Misc","PlayerImpsToTilesRatio","(Tiles / imps) − 1","(Клетки / бесы) − 1");
        Number("Misc","PlayerMana","Starting mana","Начальная мана",64000);
        Number("Misc","PlayerSpeed","Speed","Скорость",4,1);
        Number("Misc","PlayerManaValue","Mana value (%)","Ценность маны (%)",100);
        Number("Misc","PlayerGoldValue","Gold value (%)","Ценность золота (%)",100);
        Number("Misc","PlayerOpenness","Exposure (%)","Открытость (%)",100);
        if (result.Count != 67 || result.Select(parameter => parameter.Name).Distinct().Count() != 67) throw new InvalidDataException("Unverified manual AI parameter catalogue.");
        return result.AsReadOnly();
    }
}

public sealed class PlayerAIConfiguration
{
    internal NativeMapDocument Document { get; }
    internal byte[] Payload { get; }
    internal IReadOnlyDictionary<string,NativeField> Fields { get; }
    public uint ID { get; }
    public NativeRecord Record { get; }
    public IReadOnlyDictionary<string,long> Values { get; }
    public IReadOnlyList<long> RoomOrder { get; }
    public bool IsCurrent
    {
        get
        {
            try
            {
                var current = Document.GetPlayerAIConfiguration(ID);
                return current.Record.File == Record.File && current.Record.Offset == Record.Offset && current.Payload.AsSpan().SequenceEqual(Payload);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or OverflowException) { return false; }
        }
    }
    internal PlayerAIConfiguration(NativeMapDocument document,PlayerSettingsRow row,IReadOnlyDictionary<string,NativeField> fields)
    {
        Document = document; ID = row.ID; Record = row.Record; Fields = fields; Payload = row.Record.File.Data.Span.Slice(row.Record.Offset,row.Record.Length).ToArray();
        long Raw(string name) => long.Parse(fields[name].Read(Payload),CultureInfo.InvariantCulture);
        Values = new ReadOnlyDictionary<string,long>(PlayerAIOptions.Parameters.ToDictionary(parameter => parameter.Name,parameter => parameter.Mask == 0 ? Raw(parameter.Name) : (Raw(parameter.Name) & parameter.Mask) != 0 ? 1L : 0L));
        RoomOrder = Array.AsReadOnly(Enumerable.Range(0,15).Select(index => Raw($"PlayerRoomToBuildPri[{index}]")).ToArray());
    }
}

public sealed partial class NativeMapDocument
{
    public PlayerAIConfiguration GetPlayerAIConfiguration(uint id)
    {
        var row = SelectedPlayers([id],player => player.ProfileEditable).Single(); var fields = row.Record.Fields.ToDictionary(field => field.Name);
        var names = PlayerAIOptions.Parameters.Select(parameter => parameter.Name).Concat(Enumerable.Range(0,15).Select(index => $"PlayerRoomToBuildPri[{index}]"));
        var mapped = names.ToDictionary(name => name,name => fields[IsLegacyProject ? name : LegacyCompilerRules.Players.Single(mapping => mapping.Source == name).Target]);
        return new(this,row,new ReadOnlyDictionary<string,NativeField>(mapped));
    }
    public void SetPlayerAIConfiguration(PlayerAIConfiguration expected,IReadOnlyDictionary<string,long> values,IReadOnlyList<long>? roomOrder = null)
    {
        ArgumentNullException.ThrowIfNull(expected); ArgumentNullException.ThrowIfNull(values);
        if (expected.Document != this) throw new ArgumentException("AI draft belongs to another document.");
        var current = GetPlayerAIConfiguration(expected.ID);
        if (current.Record.File != expected.Record.File || current.Record.Offset != expected.Record.Offset || !current.Payload.AsSpan().SequenceEqual(expected.Payload))
            throw new InvalidOperationException("Player changed after this AI draft was opened; reload the draft first.");
        var writes = new List<(NativeField Field,byte[] Bytes)>();
        void Add(string name,long value) => writes.Add((current.Fields[name],current.Fields[name].Encode(value.ToString(CultureInfo.InvariantCulture))));
        foreach (var (name,value) in values)
        {
            var parameter = PlayerAIOptions.Parameters.SingleOrDefault(parameter => parameter.Name == name) ?? throw new ArgumentException("Unknown or non-manual AI parameter.");
            if (value == current.Values[name]) continue; // Unknown imported values remain byte-exact until explicitly changed.
            if (value < parameter.Minimum || value > parameter.Maximum) throw new ArgumentOutOfRangeException(nameof(values),"AI value outside original manual control limits: " + name);
            var stored = parameter.Mask == 0 ? value : (long.Parse(current.Fields[name].Read(current.Payload),CultureInfo.InvariantCulture) & ~(long)parameter.Mask) | (value == 0 ? 0L : (long)parameter.Mask);
            Add(name,stored);
        }
        if (roomOrder is not null && !roomOrder.SequenceEqual(current.RoomOrder))
        {
            if (!PlayerAIOptions.ValidRoomOrder(roomOrder)) throw new ArgumentException("Room build order must contain each of the fifteen original room IDs once.");
            for (var index = 0; index < 15; index++) if (roomOrder[index] != current.RoomOrder[index]) Add($"PlayerRoomToBuildPri[{index}]",roomOrder[index]);
        }
        Edit("Change custom player AI and room build order",() => { foreach (var (field,bytes) in writes) bytes.CopyTo(current.Record.File.Bytes,current.Record.Offset + field.Offset); });
    }
}
