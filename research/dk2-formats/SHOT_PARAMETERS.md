# Shots: Data1/Data2 и реальные обработчики

Продолжение этап77 — `SIGHT_OF_EVIL_RU.md`:полная native-цепочка Process25→Bridge0D уточнила Data1 как полуразмер области в клеточных координатах,low16 с ограничением7. Полный DWORD управляет отправкой события;0 и65536 неэквивалентны. KeeperC8 остаётся неизвестным.

В файле Data1 и Data2 — DWORD по +0x98/+0x9C. Конвертер 0x56E690 переносит их в промежуточные +0x120/+0x124; загрузчик 0x4ACDF0 — в определение +0x37/+0x3B; создание 0x4AAC90 — в экземпляр CShot+0x13D/+0x141. Заклинания/ловушки могут переопределять параметры экземпляра.
Две независимые системы: ProcessType по +0xC7 и DamageType по +0xC5. ProcessFlags по +0xA0 выбирают события для специального процесса. Нулевой ProcessType **не отключает обычный урон**. Не строить UI только по одному enum.
Имя снаряда и его Shot ID не являются ProcessType. 65 отдельных Shot ID в исходном каталоге используют 0..39 процессов; соседний AutoSaveGlobals содержит 103 Shot ID. Историческое меню из 66 позиций не является полным списком обработчиков или лимитом файла.
Таблица всех адресов процессов извлечена из 0x4AD1B0, все 39 callback декомпилированы. В строках «нет чтения» речь только об этом callback; Data1/Data2 могут использоваться системой обычного урона.

| ProcessType | Имя | Data1 | Data2 | Callback |
|---:|---|---|---|---|
| 0 | NONE | No process callback; inspect DamageType | No process callback; inspect DamageType | — |
| 1 | CREATE_CREATURE | Creature ID (low u8) | Number to create (signed i32, positive loop bound) | 0x004ad7a0 |
| 2 | CREATE_OBJECT | Object ID (low u8) | Gold amount when created object ID is 1 | 0x004ade10 |
| 3 | MODIFY_HEALTH | Signed health delta (positive heals, capped to missing health) | No read in this process callback | 0x004adb30 |
| 4 | MODIFY_SPEED | Speed multiplier in percent (100 = same speed) | Duration seconds, low 16 bits then multiplied by TPS | 0x004ae000 |
| 5 | MODIFY_ARMOUR | Armour timer raw ticks (low 16 bits) | No read in this process callback | 0x004ae0d0 |
| 6 | MODIFY_MANA | Mana amount transferred between target owner and caster | Health delta for target creature | 0x004ae3e0 |
| 7 | MODIFY_EXPERIENCE_LEVEL | Unsigned selector: >1 gains one level-worth of XP, 0/1 loses it | No read in this process callback | 0x004ae630 |
| 8 | MODIFY_HUNGER | No read in this process callback | No read in this process callback | 0x004ae7c0 |
| 9 | LIGHTNING | Damage magnitude, abs(Data1), doubled on checked terrain flag | No read in this process callback | 0x004af1e0 |
| 10 | INVUNERABLE | Duration seconds; low 16 bits multiplied by TPS into invulnerability timer | No read in this process callback | 0x004ae180 |
| 11 | FREEZE | Duration seconds multiplied by TPS; helper writes freeze timer | No read in this process callback | 0x004ae1d0 |
| 12 | ALARM | No read in this process callback | No read in this process callback | 0x004ae120 |
| 13 | TURNCOAT | Duration seconds; low 16 bits multiplied by TPS into turncoat timer | No read in this process callback | 0x004ae550 |
| 14 | CONCEAL | Duration seconds; low 16 bits multiplied by TPS into conceal timer | No read in this process callback | 0x004ae250 |
| 15 | CHICKEN | Duration seconds; low 16 bits multiplied by TPS into chicken timer | No read in this process callback | 0x004ae2a0 |
| 16 | SCAPEGOAT | Duration seconds; low 16 bits multiplied by TPS into scapegoat timer | No read in this process callback | 0x004ae330 |
| 17 | WIND | Integer radius in map cells | Percentage used in distance-dependent damage | 0x004ae990 |
| 18 | RAISE_DEAD | Creature ID (low u8) to create at dead-body positions | No read in this process callback | 0x004ae880 |
| 19 | OBEY | Duration seconds multiplied by TPS into obedient timer | No read in this process callback | 0x004ae5e0 |
| 20 | INVISIBLE | Duration seconds; low 16 bits multiplied by TPS into invisible timer | No read in this process callback | 0x004ae380 |
| 21 | FEAR | No read in callback | No read in callback | 0x00484940 |
| 22 | BOULDER | No read in this process callback | No read in this process callback | 0x004aebd0 |
| 23 | ALERT | Speed multiplier in percent | Duration seconds; low 16 bits multiplied by TPS | 0x004aedf0 |
| 24 | MESMERISE | No read in this process callback | No read in this process callback | 0x004aee50 |
| 25 | SIGHT_OF_EVIL | Nonzero DWORD enables initial event; low u16 is cell-coordinate radius capped at7 by Bridge0D | No read in this process callback | 0x004af0e0 |
| 26 | POSSESS_CREATURE | No read in this process callback | No read in this process callback | 0x004aeea0 |
| 27 | TREMOR | Low 16 bits multiplied by TPS for tremor duration | Low 16 bits shifted left 12, then cast to u16; helper caps at 0x5000 | 0x004af330 |
| 28 | GUIDED | No read in this process callback | No read in this process callback | 0x004af3a0 |
| 29 | DECKEM | No read in this process callback | No read in this process callback | 0x004af610 |
| 30 | INFERNO | Damage, negated for health modification | Integer radius in map cells | 0x004af660 |
| 31 | GAS | No read in this process callback | Integer area extent in cells | 0x004af8e0 |
| 32 | TRIGGER | No read in this process callback | No read in this process callback | 0x004afad0 |
| 33 | SUMMON_REAPER | Optional reaper lifetime seconds; nonzero changes level selection path | No read in this process callback | 0x004afaf0 |
| 34 | INSTANTANEOUS_HIT | No read in this process callback | No read in this process callback | 0x004afcb0 |
| 35 | DRAIN | Percentage of caster maximum health to drain, capped to target current health | No read in this process callback | 0x004afd20 |
| 36 | CREATE_SINGLE_CREATURE | Creature ID (low u8) | Starting level: creates level 1 then repeats level-up while level < Data2 | 0x004afe50 |
| 37 | WEB_AREA_EFFECT | Duration seconds, low 16 bits passed to speed modifier | Integer radius in map cells | 0x004b0050 |
| 38 | POISON_SPIT | Poison duration, low u16 update count; no multiplication by TPS | Poison damage magnitude per eligible update, low u16 | 0x004b0270 |
| 39 | JACK_IN_THE_BOX | Damage, negated in checked branch | Entity tag ID (low u16), used to index live entities | 0x004b02e0 |

## Обычный урон

| DamageType | Имя | Data1 | Data2 | Вызов |
|---:|---|---|---|---|
| 0 | NONE | No normal damage dispatcher | No normal damage dispatcher | — |
| 1 | CONSTANT | Damage per call | Integer area extent in cells | Every tick |
| 2 | RANGE_FROM_ORIGIN | Damage modified by distance from origin | Integer radius in cells | Every tick |
| 3 | TIME | Damage * currentHealth / initialHealth | Definition Data2 >> 12, not instance Data2 | Every tick |
| 4 | RING_CONSTANT | Damage per call | (initialHealth-currentHealth) * (instanceData2 >> 12) / initialHealth | Every tick |
| 5 | AT_START | Damage per call | Integer area extent in cells | Once on first tick (0x4AB180) |
| 6 | AT_END | Damage per call | Integer area extent in cells | End routine 0x4AAF70 under checked world condition |
| 7 | HIT_THING | Positive damage; nonpositive ignored; sniper doubles it | Not read in hit routine | Collision 0x4ABD60 |
| 8 | CONSTANT_FOR_TREMOR | Damage applied to map terrain | Instance Data2 >> 12, area extent | Every tick |

## Практические отличия и ограничения

- Для DamageType 1/2/5/6 область берётся из целого Data2. Для 3/4/8 код использует сдвиг >>12. У type 3 берётся значение определения, а не переопределённое значение экземпляра.
- ModifyArmour записывает Data1 в таймер напрямую. Многие другие статусы умножают Data1 на TPS; применённые таймеры 16-битные, большие значения обрезаются.
- **Исправление этапа115:** PoisonSpit записывает Data1 в poisonedTimer и Data2 в poisonedSeverity, оба low16, без умножения на TPS. Прежнее описание меняло их местами; стек, настоящий caster/collision и20полных обновлений подтвердили исправление. См. `POISON_RU.md`. В пакете используйте `effective_shot_parameters`; историческая строка38 в `shot-semantics.json` сохранена только для аудита и заменена новой схемой.
- Threat по +0xE9 и BurnDuration по +0xEB читаются конвертером в промежуточную структуру, но **не переносятся** в MyShotDataObj загрузчиком 0x4ACDF0 и не используются 0x521810 кроме общего учёта/контрольной суммы. Эффект этих полей в игровом пути данного образа не подтверждён; не обещать, что их изменение меняет угрозу/горение.
- TimedEffectDelay — byte по +0xC0; в 0x4AB180 используется как интервал **тиков**, через age % delay. При ненулевом TimedEffectId и нулевом delay путь содержит деление на ноль. Значения по умолчанию с TimedEffectId=0 этому условию не соответствуют.
- ProcessType 21 FEAR в данном образе указывает на функцию, которая сразу возвращает 0. Это не доказательство отсутствия любой системы страха в игре.
- Для процессов 23 и 39 дополнительно проверены инструкции. У Alert Data1 — процент скорости, Data2 — длительность, умножаемая на TPS. У JackInTheBox Data2 — ID живой сущности, а не ID определения или радиус. Декомпилятор ошибался из-за неизвестных сигнатур косвенных вызовов; сохранены соответствующие assembly-файлы.

Полный машинный результат и ссылки на сохранённые декомпиляции — shot-semantics.json. Смысл каждого callback рассмотрен вместе с DamageType; одинаковые DWORD не означают одинаковые единицы.
