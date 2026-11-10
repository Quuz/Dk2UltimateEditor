# Кодовое закрытие I02: компиляция и проверки

25 октября2026. CompilerValidatorAudit-20261025. **I02 закрыт по коду**:
сведены все28 функций записи и17 маршрутов проверки/генерации с65 исходными
REPORT identifiers, включая13 сообщений об отказах записи. Список
неклассифицированных source reports пуст. Неизменённые успешные группы не
повторялись. Это не завершение всей цели и не новая игровая/GUI-приёмка.

## Конечная таблица записи

| Функции | Что сопоставлено | Доказательство |
| --- | --- | --- |
| InfoFile/PlayersFile —2 |49 metadata rules,78 referenced metadata fields,91 player mappings/101 stores,9 AI profiles×84 assignments; constants/references, roots, камеры, CPU area, имена и health | [metadata/player](I02_METADATA_PLAYER_AUDIT.md) |
| ThingsFile и9 writers —10 |155 source stores, constants, owner/UID scopes, object unions, три layouts существ, gates/hearts/effects, band active prefix и нулевые slots | [things/variables](I02_THING_VARIABLE_AUDIT.md) |
| VariablesFile и4 helpers —5 | Все16-byte recipes; pool/availability, heart filter, modifier/elite skip, upper-triangle alliances, nondefault globals;8 source catalogues | [things/variables](I02_THING_VARIABLE_AUDIT.md) |
| MapFile —1 | KMAP уже game DWORD; header36, dimensions/payload4WH, отсутствие повторного editor conversion | [остальные writers](I02_WRITER_FIELD_AUDIT.md) |
| TriggersFile/Triggers/Actions —3 |103 event types,8-byte payloads иsuffix, constants/casts/default branches,4 computed WORD для gem/gate, counts/размеры | [остальные writers](I02_WRITER_FIELD_AUDIT.md) |
| Global writers —7 |262 fields, masks/casts,13 clear ops,6 extra zero stores,7 orders/counts/canonical sizes, opaque rows иbasis policy | [остальные writers](I02_WRITER_FIELD_AUDIT.md) |

SetDate — отдельный helper с прежним доказательством, не29-й writer.
Сначала учитывались только24 int-return functions; четыре BYTE* variables
helpers были добавлены в полный перечень. Ни один capability requirement из
FEATURE_PARITY ради этого не удалён.

## Конечная таблица валидаторов

| Исходный маршрут | Текущий результат |
| --- | --- |
| TaskTest | Source order Version/Map/Players/Things/Areas/Gates/Triggers сопоставлен с общими providers. Human Check/Compile проверяет gameplay; Core draft compile имеет отдельную structural проверку. |
| TaskTestVersion | DK2V001–007 Information: target<16/17, все elite/maiden/trap/event/object условия. Исходная особенность TRAPS condition1.61 против placed trap1.70 сохранена. Native reserved bytes не читаются как отсутствующий param2. |
| TaskTestMap | DK2N022–025: duplicates с heroes/neutral exemption, no heart, no keeper1, последовательность хранителей. Declared count отдельно от фактического присутствия. |
| TaskTestMapRect | DK2N019 Error для nonrectangle; N020 Error для heart size; N021 Warning для portal/gate size. Row/width/height/catalogue identity совпадают в поддержанном layout. |
| TaskTestMapExpand | Навигационный rectangle по source minimum-row-width/height и marking, отдельная ранее доказанная geometry, без изменения карты. |
| TaskTestMapInRect | Source handled-region suppression соответствует handled bitmap. Современный проход собирает все регионы, а не только первый отказ. |
| TaskTestPlayers | N030/N031 Warning: independent camera−1 sentinels иверхние границы; CPU area только computer keepers. Нижняя граница не придумана. |
| TaskTestThings | N041–043 иV007: пять families, source placement rules, walking flags, enemy Information, fatal/unsupported terrain Warning, version objects. Нет коллизии объекта с самим собой. |
| TaskTestAreas | В исходнике нет semantic checks, только Task->Failed gate и progress. Не выдано за отсутствующий алгоритм. Наши дополнительные geometry/identity guards описаны отдельно. |
| TaskTestGates | N044 Information для разрушаемых2×2 gate/area/events по source priority; N045 Warning для wide/tall. First area binding, perimeter/cannot-destroy иnavigation expansion учтены. |
| TaskTestTriggers | Root reachability, unused condition/action counts иявный Keep/Remove/Cancel. Remove меняет живой документ в одном Undo только после актуального согласия. |
| TaskTestResetTrigger | Managed traversal не меняет node.type flags; исходный memory reset не нужен. Reachability не теряет shared branch после повторного reset. |
| TaskTestResetAction | Аналогично для actions; next edge учитывается, native reserved child bytes не превращаются в links. |
| TaskTestTrigger | Missing bound root/edge → MissingLink/DK2N004. Cycles → Cycle/N011. Shared acyclic branch не считается циклом; duplicate физические IDs имеют отдельный guard. |
| TaskTestAction | Все10 cases: три upper-bound координаты, area params1/2/5, crystal28, RELEASEBAND включая band0, pet option20. Проверяются достижимые события; статусы unused компонентов не подменяют этот gate. |
| TaskIsReserved |63 exact case-insensitive имён. Test Information N046; независимый display title не используется вместо actual output stem. |
| TaskGenerate | Protected-name refusal перед первым writer;13 source writer/I-O error labels сопоставлены с managed generation failure иApp. SaveCopy пишет только новый staging, moves on success, удаляет свой staging при отказе. Старые installed outputs не удаляются. |

Все65 identifiers распределены в `validator-route-audit.json`: source body
hashes, provider, code/issue, severity или явная managed policy. Фактические
проверки guards/cases/version sets иsource phase order включены в capture script.

## Исправления, сделанные при завершении I02

| Расхождение | Результат |
| --- | --- |
| DESTROYGATE с отсутствующим ID | Source fallback1,1; project param5 инамеренно отсутствующая рабочая цель сохраняются. [Details](COMPILER_GATE_FALLBACK.md) |
| Даты headers | Local creation/modified stamp для generated sections; скопированная basis date не меняется. [Details](COMPILER_GAME_DATES.md) |
| Default-valued materialized global arrays | База сохраняется целиком; факт существования массива не означает source change. [Details](COMPILER_DEFAULT_GLOBALS.md) |
| Things ordering | Девять source categories, stable порядок внутри; hearts перед effects. [Details](COMPILER_THING_ORDER.md) |
| Не было source placement/gate notices | Добавлены N041–045 с исходными уровнями; общий gate helper использует первую area binding. [Details](COMPILER_PLACEMENT_DIAGNOSTICS.md) |
| Лишний отказ negative action targets | Bound plan проверяет только upper bounds; LONG/WORD source arithmetic сохранена. [Details](COMPILER_ACTION_BOUNDS.md) |
| Не было reserved-name rule | Source63 list, Test notice иpre-generation refusal; actual filename отдельно отdisplay title. [Details](COMPILER_RESERVED_NAMES.md) |

## Явные адаптации и оставшиеся границы

Это не попытка повторить ошибки исходника. Текстовый контракт CP1252→UTF16
вместо повторной OEM интерпретации, фактические размеры свежего D2, корректное
управление Disarmable bit10, deterministic trap padding ипроверенный16-slot
band bound описаны в таблицах. Выбранная game basis даёт graphics/opaque data,
которых DKM не хранит. Семь игровых файлов — отдельное требование пользователя.

Shared DAG не вызывает source emergency prompt иразрушительное удаление всех
non-AREA roots. Современный iterative traversal отличает reuse отцикла.
Очистка unused выполняется только явной операцией; Keep ничего не фильтрует.
Однако оставленный malformed unused component может заблокировать итоговый
structural export, например dangling edge. Это существующая дополнительная
политика безопасного импорта, а не точная эмуляция source inert-component
export. Она указана явно; I18 остаётся открытым для остальных повреждённых
вариантов, а не объявляется решённым через I02.

Нулевые/повторные владельцы и IDs, unknown layouts, неподтверждённые versions
не получают authority из того, что C source иногда выбирал первую запись
или выходил за границу. Более широкая политика остаётся в I04/I18. Межформатное
сохранение graph/aliases/loss — I03; UI callbacks — I10; geometry callers —
I07–I09. Их объём и условия закрытия не уменьшены.

Source REPORT_LASTERROR при нехватке памяти для gate analysis относится к
platform failure. Managed allocation failure ифизический I/O отказ не выданы
за успешно воспроизведённые тесты этого аудита. Окна, navigation, системные
диалоги, hardware/DPI, audio ифактическая игра остаются в прежних23 M-группах.

## Проверки и поставка

На этом финальном этапе product/tests/App/Core/ENRU не менялись. Build,
publish иуспешные tests не повторялись. Реальные terminal records предыдущих
исправлений привязаны ких датам иCore hashes; результаты разных DLL не
объединены в новый полный corpus result. Исторический1840 PASS/63 известных
отказа остаётся историческим, corpus не объявляется зелёным.

Source/Binaries/Fixtures иустановленные карты сохранены. Документация иоба
актуальных архива обновлены с проверкой manifest entries. Evidence:
`artifacts/Evidence/CompilerValidatorAudit-20261025/`.

Теперь остаются **7 кодовых пунктов I03/I04/I07/I08/I09/I10/I18 и23 ручные
группы:30 пунктов работы**. Это не число дефектов или проверок. Вся исходная
цель активна; следующий кодовый участок — межформатные сценарии I03.
