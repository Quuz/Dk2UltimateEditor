# I06: диагностика ссылок на игровые определения

Закрыто по поддержанным ссылочным полям 24 октября 2026.
Поставка: `artifacts/PreFinal/App`. Доказательства:
`artifacts/Evidence/DefinitionReferenceDiagnostics-20261024`.

`DefinitionReferenceDiagnostics` подключён к общему `NativeMapValidator`.
Новые сообщения **информационные**: они объясняют, что ID отсутствует в
проверенном каталоге исходного редактора. Они не объявляют пользовательское
определение игры отсутствующим или повреждённым. Игровые модификации могут
содержать другие допустимые ID. Исходные значения сохраняются; новых запретов
на сохранение, экспорт и редактирование эти сообщения не вводят.

| Код | Результат |
| --- | --- |
| DK2N039 | Каталог, категория, поле, значение и физическое смещение нестандартной ссылки |
| DK2N040 | Нестандартный ID местности, число клеток и первая клетка; одно сообщение на ID |

Существующие DK2N026–028 проверяют другие связи: identities областей,
отрядов, проходов и генераторов. Например, ID генератора эффектов и ID эффекта
в его первом слоте принадлежат разным пространствам имён.

## Конечный объём

В `i06-route-audit.json` сохранены 15 групп маршрутов, 27 исходных каталогов,
классификация 235 примитивных объявлений семи global-структур и 20 сценарных
контекстов. Массив здесь считается одним объявлением; это не число отдельных
игровых записей. Все UINT-поля отнесены к ссылке, селектору либо enum.
Непривязанных поддержанных групп и неклассифицированных UINT-полей нет.

| Маршрут | Проверяемые данные и каталог | Ноль, контекст и исключения |
| --- | --- | --- |
| DR01 | Три native-категории существ и DKM thing type0: BadCreatures + BadElites + Heros | Физическое существо с ID0 не обозначает «любой вид» |
| DR02 | Дверь, ловушка, обычный/магический предмет: Doors, Traps, Objects, Magics/MagicsV17 | Категория определяет пространство ID; native GAMEOBJECT объединяет предметы и specials |
| DR03 | Payload книги, object ID4: Spells | dataAll/dwObjectSpell у других предметов не считается ссылкой на заклинание |
| DR04 | GAMEVARCREATURECOUNT/GAMEVARCREATURE: stored species ID | DKM-массивы используют неявный порядок каталога; их значения — counts/параметры |
| DR05 | GAMEVARTHINGSTATUS: категории1/3/4/5 → VarRooms/Traps/Doors/Spells, variable IDs | Неизвестная категория не получает выдуманный каталог |
| DR06 | Native/DKM участники отрядов: полные species IDs | Только активный префикс; native ID/behavior0 либо DKM isvalid0 заканчивает его. Импортированные элитные виды известны, даже когда меню их не предлагает |
| DR07 | Четыре значения генератора: Effects | 0 — отсутствие эффекта; FFFFFFFF — конец исходной таблицы, не selectable ID |
| DR08 | Terrain byte клетки: полный GTerrains, 38 ID | Более узкий сценарный Terrain не используется: он исключает настоящую пограничную клетку30 и специальные клетки |
| DR09 | Global terrain: MaxHealthType/DestroyedType и два эффекта | Только включённые исходные поля; переход использует свой список местности |
| DR10 | Global room: Objects[0..5], Effects[0..5] | Исходные masks определяют активные слоты; постоянно выключенный BattleEffect и неактивные slots пропускаются |
| DR11 | Doors/Traps: Crate/Key и death/idle/powerless effects | Используются отдельные GDoorsCrates/GDoorsKeys/GTrapsCrates и Effects; выключенные trap-поля пропускаются |
| DR12 | Creature: LairObject, два условных attraction ID, Shot1/2/3ID и MeleeID | GBeds; GARooms или GCreatures по attraction type; GCrSpells/GCrMelee. При неактивном attraction ID не читается |
| DR13 | Trap Shot, Spell Shot/Upgrade, CreatureSpell Shot | GShots. Form2/3: Data1 — species; Form4: Data1 — object. Остальные числовые параметры, Data2 и параметры неизвестного типа не считаются ссылками |
| DR14 | 17 обычных и три split-interface сценарных контекста | Собственные Any/None правила; категория интерфейса отделена от ID. Interface panel numbers — не определения. includeScripts=false пропускает все event operands |
| DR15 | Неизвестные layouts и непрозрачные внутренние ссылки | Сначала verified-layout guards; неизвестные bytes не интерпретируются по сходному имени поля |

Для global-полей нулевые сохранённые ссылки консервативно пропускаются:
исходные буферы содержат нули для отсутствующих/неиспользуемых значений,
даже если combo не предлагает отдельную нулевую строку. Это правило диагностики
не доказывает, что любая комбинация нулей будет исполняться игрой. У активного
projectile target Data1 каталог проверяется отдельно: ноль не превращается
автоматически в допустимый species/object ID.

В сценариях проверяются CREATURES/HAPPYCREATURES/ANGRYCREATURES,
CREAENTERAREA, ADDCREATURE/GENERATE, ROOMS/ROOMSSIZE/ROOMSCONTENT/ROOMSTYPES,
SHOWNEXTROOM, DOORS/TRAPS/DESTROYED/SPELLS, AREACONTENT/CHANGESQUARE и
CLICKELEMENT/SHOWELEMENT/ADDELEMENT. Полный список типов и номеров параметров
содержится в JSON. Зарезервированные native bytes, которые не представлены
аргументом схемы, не превращаются в species-ссылку. Привязки к AREA/BAND/GATE/
EFFECT identities остаются у существующей диагностики.

## Что сознательно не считается ссылкой

Material, FightStyle, JobClass, trigger/cast/target rules, resistances,
abilities, jobs и селекторы attraction — перечисления или управляющие значения.
Creature `Spell` — swipe-анимация, `GammaEffect` — режим gamma, а не ссылка
на CreatureSpells/Effects. Уровни, шанс, урон, количество золота, параметры
света, таймеры и индексы элементов массивов также не проверяются как ID.

Для Objects, Shots, Effects и EffectElements нет поддержанной схемы редактирования
их произвольных внутренних бинарных ссылок. Они сохраняются непрозрачно;
диагностика не угадывает смещения и не обещает полного декодирования этих файлов.
Поддержанные ссылки **на** Objects/Shots/Effects проверяются по каталогам выше.
Поддержанного отдельного поля-ссылки **на EffectElements** сейчас нет.
Это явная граница существующих схем, а не новая задача расшифровывать каждый
байт. Согласованность неизвестных/исторических layouts остаётся в I04,
компиляция — в I02, сценарные callback-переходы — в I10.

Native-записи проверяются только при версии4 и точном известном размере;
DKM things1–6, bands1–5, areas1 и events2/3 используют существующие разобранные
поля. Global rows требуют общего verified-catalogue guard, включая варианты
creatures44/46. Отсутствующие optional DKM arrays не создаются ради диагностики:
они используют проверенные defaults.

## Проверки и ограничения

`--definition-reference-diagnostics-only`: **9 PASS, 0 FAIL**, terminal exit0.
Проверены native/DKM категории, реальные неординальные ID, элитные виды,
книга против обычного payload, native availability categories, активный префикс
отряда и неиспользуемый хвост, effects0/1/370/371, полный terrain catalogue,
повторяющиеся неизвестные клетки, Undo/Redo, фактический SaveCopy/reopen,
global links/optional0/attraction context, четыре projectile slots и forms2/3/4,
20 сценарных контекстов, Any/None, split-interface categories, no-script export,
неизвестные thing/global/event headers. Исходные defaults не получают новых
замечаний. При каждой диагностике сравниваются bytes, file set, revision,
dirty, counters, Undo/Redo, snapshot cost и уведомления.

Существующая `--entity-diagnostics-only` также прошла на новой DLL: проверена
интеграция в общий validator, а не повтор всей неизменённой регрессии.
Все41 проекта собраны без предупреждений и ошибок; App опубликован.

До реализации шесть первоначальных групп завершались с FAIL. Промежуточные
сбои и исправления fixtures сохранены в evidence. Первый вариант ошибочно
использовал узкий scenario Terrain; это исправлено выбором полного GTerrains.
Два последующих сбоя были из-за native fixture без определений; тесты теперь
используют существующую fixture с исходными Default*.dkg, guards не ослаблены.

Полный corpus не запускался: исторические1840 PASS/63 FAIL не являются
результатом этой DLL и не становятся зелёными. GUI, звук, DPI, официальный
редактор и игра не запускались. M18/M19/M23 и прочие ручные группы остаются.
I06 закрыт по описанным поддержанным ссылочным полям, общий проект не завершён.
