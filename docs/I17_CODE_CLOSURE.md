# I17: завершённая кодовая сверка команд и Config

18 октября 2026, ManagerDestinationCoordinates-20261018. I17 закрыт **по коду**
после исправления последнего установленного numeric callback менеджера
объектов. Это не физическая приёмка всех окон и не завершение редактора.
Остаются 16 I-пунктов и все 23 группы ручной приёмки M.

## Требование и доказательство

| Часть исходного I17 | Законченный конечный объём | Доказательство |
| --- | --- | --- |
| Команды/поля Config | Все42 именованных поля; active/default/compiled-out/no-command явно различены. Нет очереди «прочие настройки» | [CONFIG_SOURCE_MAPPING](CONFIG_SOURCE_MAPPING.md), source-config-inventory.json; ordered field names совпали с текущей таблицей |
| Главное меню и availability/callers | Все70 primary IDs,8 ranges и wall pair имеют route либо исходный stub. Все15 MapViewFlags сопоставлены | [MAIN_MENU_SOURCE_MAPPING](MAIN_MENU_SOURCE_MAPPING.md), HelpSectionVersions/source-command-inventory.json: UnclosedPrimaryMenuIDs=[] |
| Клавиатура |30 menu chords,3 Insert callers,8 scroll families, фокус и guarded callers. Ctrl+N/New, Ctrl+Y/Redo явно выбраны человеком; Find остаётся source stub | [SOURCE_KEYBOARD](SOURCE_KEYBOARD.md), [SPECIAL_KEYBOARD](SPECIAL_KEYBOARD.md), keyboard-user-decision.json |
| Things Manager commands и lists | Source controls/events перечислены; selected/all, сортировка вида, single/multiple, Add/Modify/options/preview/removal, локальный Undo, shared templates и одна физическая membership | [MANAGER_LIST_COMMANDS](MANAGER_LIST_COMMANDS.md), [OBJECT_MANAGER_EXECUTION_FLOW](OBJECT_MANAGER_EXECUTION_FLOW.md), source-scope.json |
| Destination numeric callbacks |106/107 и186/187: immediate bounds, ±1, accepted numeric session, общий Add/show origin; private picker с явным OK/Cancel | [MANAGER_DESTINATION_COORDINATES](MANAGER_DESTINATION_COORDINATES.md), focus01 текущего этапа |
| Четыре остальных parent managers |46 numeric rows и11 notifications; MC01–MC06 реализованы по коду. Общие свойства/creation/Undo и существующие requests сохранены | [MANAGER_COMMAND_SCOPE](MANAGER_COMMAND_SCOPE.md), последовательные source/delivery доказательства шести групп |
| Requests и согласованные отличия |R01–R05 имеют код/модель либо явное решение пользователя. Нехватка Undo memory — полный rollback; обычный Undo восстанавливает прежний порядок | [SOURCE_REQUEST_AUDIT](SOURCE_REQUEST_AUDIT.md), [REGION_UNDO_POLICY](REGION_UNDO_POLICY.md), [HISTORY_MEMORY_NOTICE](HISTORY_MEMORY_NOTICE.md) |

MC01 — общий private region creation/repeated Add; MC02 — точная строка
double-click Modify; MC03 — пять source insertion destinations; MC04 —
сохранённое раскрытие условий; MC05 — координаты регионов; MC06 — New Point
отдельно от Open existing. Для каждой группы сохранён её собственный объём
исполненной проверки. Compiled WPF код не переименовывается в executed GUI.

`artifacts/Evidence/ManagerDestinationCoordinates-20261018/i17-closure-audit.json`
связывает требования с конечными inventories, исходными hashes, текущими
таблицами, решениями человека и успешными build/focus/publish текущего этапа.
Оригинальные исходники не менялись. Эта сверка не запускала повторно прежние
успешные product checks. Определение ObjectBatchSession перенесено в отдельный
канонический файл без изменения его тела; двух версий модели нет.

Сборка всех41 проектов и новый focus01 успешны, publish завершён. Core sources
и DLL совпадают с ScenarioExpansion: исторический full1831 PASS/63 прежних
отказа сохраняется на той же Core. Новый focus не добавлен к full-счётчику.
Все94 исходных отказа уже сопоставлены полностью в том full этапе; никаких
записей/ремонта установленных карт новая работа не выполняла.

## Сохранённые открытые требования

Физические mouse/key/focus/repeat, доставка диалогов и звука, layout,
multimonitor/DPI и фактические игровые карты остаются в M01–M23 по своим
маршрутам. Особенно менеджерские M12/M18/M20 и rendering M21/M23. Агент не
запускал WPF/редактор/Computer Use/renderer/audio device/game на этом этапе.

Native no-list-order и отдельные пустые points/working metadata остаются
I01/I03. Исторические layouts и ambiguous IDs — I01/I04/I18; полнота
compiler messages — I02; вложенные scenario/global/catalogue pages —
I10/I11/I12/I13; acceptance counters вне сопоставленных маршрутов — I15,
activity lifecycle — I16. Это существующие требования, не новые пункты,
добавленные вместо закрытого I17. Успех текущей сверки их не доказывает.

Если конкретный новый дефект относится к I17, он должен иметь source path,
наблюдаемое расхождение и отдельный результат. Исторические неопределённые
фразы «остальные manager callers» не открывают уже закрытую очередь повторно.
Все23 FEATURE_PARITY возможности сохранены. Полный перечень —
[OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md).
