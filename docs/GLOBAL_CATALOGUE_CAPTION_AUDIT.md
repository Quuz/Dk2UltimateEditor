# Глобальные каталоги: ID, порядок и подписи EN/RU

20 октября2026. Этап GlobalCatalogueAudit-20261020, часть I13.

Сопоставлены20 исходных таблиц и шесть дополнительных контекстов, всего26
списков/фильтров текущего Core. Каждая строка имеет source table/ID/symbol,
исходное название с файлом и строкой, текущие EN/RU. В сохранённых source
callers найдены настоящие CB/LB_ADDSTRING маршруты всех20 таблиц.

| Семейство | Исходные таблицы / текущая привязка |
| --- | --- |
| Материалы, ящики, ключи, активация |GDoorsMats, GDoorsCrates, GDoorsKeys, GTrapsCrates, GTrapsTypes → GlobalDefinitionRules; те же материалы используются у существ. |
| Правила заклинаний |GSpellsHand, GSpellsCast, GSpellsTrget → анимация руки/отказа, территория применения, цель. |
| Сопротивления и вселение |GCrRes, GCrAbilities, GCrSwipes, GCrGammas → CreatureOptions, все повторные слоты. |
| Параметры существ |GFightStyle, GJobClass, GBeds, GARooms → CreaturePageControls. |
| Типы атак |GCrSpells, GCrMelee → CreatureAttacks; три ranged slots и melee сохраняют свои каталоги. |
| Снаряды |GShots → GlobalProjectiles; четыре потребителя Traps/Spells primary/upgrade/CrSpells. |
| Занятия |GCrJobs → CreatureJobs; отдельные Happy/Unhappy/Angry/Hate фильтры по четырём source флагам. |
| Привлечение |Attract1Type0–1 и Attract2Type0–4 → source szAttractionType; комнаты GARooms повторно используются в обоих контекстах. |

Двадцать таблиц содержат343 выбираемые строки;26 контекстов —400 вхождений,
304 различных source symbols. Это объём inventory с повторениями, не400
новых функциональных тестов. Снимок проверяет точные значения и порядок,
допустимый ноль отдельно от конечного sentinel, имена и повторные callers.
Ни один проход не создаёт документ карты, UI или игровой процесс.

Семнадцать исходных неизвестных сопротивлений0x10–0x20 остаются явно
неизвестными с исходным номером. Смысл им не придумывается. Списки,
заканчивающиеся FFFFFFFF, сохраняют выбираемую нулевую строку. Особый
порядок melee/resistance1/2/3/15/4… сохраняется; ID не равен индексу строки.
Фильтры занятий здесь сверяются по разрешённым source категориям. Динамическое
исключение уже добавленного занятия и обработчики Jobs остаются в прежней
закрытой сверке I11, заново не реализуются.

Исправлены три подписи CatalogNames.txt:

- CreatureSpell.0A: английское Drain → Vampiric drain по Vampirisation;
  русский «Вампиризм» сохранён. Подпись используется в ranged и melee.
- Projectile.2B: русское «Разрушение» → «Нарушение», согласованно с тем же
  source Perturbation и существующим CreatureSpell.13 / Disruption.
- Projectile.30: русское «Прозрачная стрела» → «Полупрозрачная стрела»
  по source Flèche translucide и английскому Translucent arrow.

До/после все400 значений и их порядок прежние; меняется текст четырёх
вхождений трёх source symbols. Остальные строки каталога, все2801 EN/RU
resource keys/literals, CS, tests/Fixtures, Source и Binaries не менялись.
Следовательно, typed writers/compiler/Apply/Undo/границы и поведение слотов
не меняются. Старые успешные проверки операций повторно не запускаются.

В перечисленной конечной выборке названия сопоставлены с исходными названиями,
структура списков подтверждена. Это не означает расшифровки неизвестной
игровой семантики, всех EffectNames/MapVariableNames, global field captions,
brush/player/gate enums или всех reference previews. Эти разделы остаются
в I13_CATALOGUE_SCOPE. Настоящие popup/scroll/focus, изображения, шрифты и DPI
остаются ручной приёмкой. GUI, renderer, игра и Computer Use не запускались.

Доказательства: artifacts/Evidence/GlobalCatalogueAudit-20261020 — текущие
снимки, source captions/callers/hashes, изменения подписей и проверка поставки.
Общий остаток14I/23M, все23 исходные возможности сохранены; цель ACTIVE.

Итог: build01 всех41 проектов/inventory-after/publish01 — ExitCode0,0warnings/errors. Поставка и оба ZIP обновлены, каждый entry проверен по SHA256. Реальные окна не запускались.
