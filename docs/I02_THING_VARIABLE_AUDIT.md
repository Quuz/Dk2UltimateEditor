# I02: запись сущностей и переменных

24 октября 2026, CompilerThingVariableAudit-20261024. Сверены15 функций:
контейнер Things, девять его writers, контейнер Variables и четыре helpers.
Вместе с metadata/player разобраны таблицы17 из28 writers. Это счётчик
таблиц исходных функций, а не процент готовности редактора. I02 ещё открыт.

Продуктовый код, тесты, DLL и EN/RU literals не менялись. Новых запусков
сборки или тестов нет. Сохранены уже подтверждённые доказательства порядка,
дат, UID/scopes, сохранности native records и payloads.

## Сущности

| Writer | Поля и особые правила |
| --- | --- |
| Objects | Координаты, ID, owner, object root. Для ID1/2/3 записывается золото, для ID4 — spell; второе union-поле нулевое. Для остальных ID оба нулевые. |
| MagicalObjects | Та же native GAMEOBJECT, отдельный magic scope. Gold/spell остаются нулевыми независимо от project dataAll. |
| Traps | Координаты, ID/owner/shots с BYTE cast; dwTrapUnknown0 нулевой. У source stack struct последний byte bTrapUnknown2 не инициализирован; наш свежий output определённо нулевой. Случайные stack bytes не воспроизводятся. |
| Doors | Координаты, ID/owner/locked, door root. Остальные bytes нулевые. |
| Creatures | Три layouts HERO/NEUTRAL/KEEPER. Gold/level/behavior/ID с BYTE cast, health DWORD, creature root. Hero использует enemy вместо физического owner, objective/area/options и BandID255; neutral owner задан самим layout. |
| Areas | Четыре координаты, wait delay WORD, flags/ID/next BYTE, area root. Flags не превращаются в boolean. |
| Gates | Hero gate: origin, terrain-derived type/orientation, owner1, ID1, health100. Heart: проверенный centre, owner3–7, type14, ID0, orientation0, health из player options с WORD cast. |
| Effects | Origin, WORD разности endpoints, четыре WORD effect IDs, BYTE frequency и ID. |
| Heros | Header51 +16 slots×32 =563 bytes, version9/payload555. Active prefix заканчивается на первом isvalid0 и ограничен16 слотами. Остальные слоты нулевые. Member health/enemy/level/options/area/objective/ID/gold и отдельный member root; BandID берётся из отряда. |
| Things container | Header56, count/размеры по полным top-level records. Вложенные band members не увеличивают count. Порядок девяти writers и положение hearts уже исправлены и доказаны отдельно. |

Независимо перечислены155 исходных присваиваний, включая constants и zero
fields. Сверены фактический switch, tuple mappings, sizes/templates, scope
constants и специальные ветки текущего compiler. Неклассифицированных
исходных записей нет. Все числовые cast сохраняют размер native поля;
source signed union values записываются теми же32 битами.

Это сверка поддержанных структур и допустимой геометрии. Повреждённые owners,
обратные endpoints, unsupported layouts и опасный выход source band loop за
16 slots не объявлены поддержанным поведением. Их load/edit/export policy
остаётся в I04/I18. Raw native SaveCopy и последующая native→DKM конверсия —
разные маршруты; сохранность всех неизвестных entity bytes при конверсии
этой таблицей не объявляется. I03 и существующие conversion reports остаются.

## Variables: точные16-байтовые рецепты

| Данные | Четыре DWORD в игровом файле | Условие |
| --- | --- | --- |
| Creature pool | 1, catalogue Variable, count bits, player | Есть heart; count!=-1. Ноль тоже записывается. |
| Rooms/traps/doors/spells | 2, type OR player<<16, catalogue Variable, state | Есть heart; state!=255. Type1/3/4/5; две WORD в source эквивалентны одному little-endian DWORD. |
| Creature modifier | 4D, catalogue ID, parameter1…4, DWORD data | Data!=0; elite rows пропускаются. |
| Alliance | 42, left+1, right+1, 0 | Ненулевой верхний треугольник, включая диагональ. Нижняя половина не создаёт вторую запись. |
| Global variable | ID, value bits, 0, 0 | Не separator FFFFFFFF, Icon0, значение отличается от default. |

Порядок: player1…7, внутри pool/rooms/traps/doors/spells; затем modifiers,
alliances, globals. Независимо разобраны все source pointer stores в блоки
по16 байт и сопоставлены с текущими Add recipes. Header56 и count16/payload
обновляются общим InsertRecord; dates разобраны предыдущим этапом.

Сверены8 исходных каталогов. Creatures имеют16 обычных +13 elite +16 heroes,
всего45 индексов: elite modifier rows16…28 пропускаются, heroes начинаются29.
Все реальные MapVars IDs помещаются в256; поэтому current ID<256 эквивалентен
source исключению separator. Порядок каталогов и их terminal0 учтены.
CreaturesAvailable не записывается исходным helper; это не пропущенный нами
native field. Дополнительный includeDeclaredKeepers принадлежит только
фабрике новой карты. Обычная компиляция применяет исходный heart filter.

Retained native variable occurrences, level-modifier deltas и extra metadata
не менялись. Это описанные дополнительные маршруты I01; они не подменяют
пять рецептов свежего DKM writer и не дают права ремонтировать исходные карты.

## Доказательства и остаток

Адресный перечень155 записей, все source16-byte stores, восемь каталогов,
матрица15 функций и hashes:
`artifacts/Evidence/CompilerThingVariableAudit-20261024/thing-variable-audit.json`.
В инвентаре сохранены прежние две готовые таблицы и добавлены15 текущих.
Новых продуктовых расхождений в этом участке не найдено.

Остаются11 таблиц: GenerateMapFile; GenerateTriggersFile/Triggers/Actions;
GenerateTerrainsFile/RoomsFile/DoorsFile/TrapsFile/SpellsFile/CreaturesFile/
CreatureSpellsFile. Их прежние частичные доказательства не обнуляются.
Общие validator/version routes также ещё входят в I02.

Связанные успешные проверки последней DLL — четыре группы Things order,
97 whole records,1800 cells/711 payloads и шесть gate groups — не повторялись;
их terminal records и Core hash сохранены. Исторический полный1840/63 не
объявляется новым зелёным full. GUI/WPF/audio/game/Computer Use не запускались.
Прежние8 кодовых и23 ручные группы остаются, исходная цель не завершена.
