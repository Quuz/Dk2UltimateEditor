# Конечное сопоставление главных команд исходного редактора

SourceMenuInventory-20261018, часть I17. Прочитан Window_Command:70 явных
IDM dispatch cases,8 диапазонных dispatch и отдельная пара wall/claimed.
Это конечный перечень входов данного обработчика, а не число ошибок или
доказательство всех внутренних callbacks. Исходные строки, SHA256 и отдельная
запись каждого ID сохранены в evidence source-command-inventory.json.
Пункты-контейнеры MainMenu и неактивные объявления не превращаются в новые
функции: алгоритм диспетчеризации берётся из Window_Command, а не из одного
перечня labels. Меню, палитры и вкладки могут размещаться иначе; наличие
маршрута не подтверждает физическую приёмку или содержимое I02/I07/I10/I11.

## Явные команды

| Source ID | Текущий маршрут | Итог / граница |
| --- | --- | --- |
| IDM_NEW, IDM_OPEN | Файл → Создать / Открыть; New_Click / OpenMap_Click | **есть маршрут** |
| IDM_SAFEFILE | Файл → Восстановить; RequestRecovery | **есть маршрут** |
| IDM_RECENTREMOVEOBSOLETE, IDM_RECENTREMOVEALL | Файл → Недавние; убрать недоступные / очистить список | **есть маршрут** |
| IDM_SAVE, IDM_SAVEAS | RequestSave / SaveAs; прежний общий save lifecycle | **есть маршрут** |
| IDM_COMPILE, IDM_EXECUTE | Файл → Компиляция / Запустить; окно compiler task | **есть маршрут** |
| IDM_TEST | Кнопка Проверить; Validate_Click → compiler task | **есть маршрут** — Проверка доступна через существующую кнопку; полный состав сообщений остаётся I02. |
| IDM_UNDOLAST | Правка → Undo; Editor.Undo | **есть маршрут** |
| IDM_UNDOHISTORY, IDM_UNDOCOUNT | Правка → История; Настройки → глубина истории | **есть маршрут** |
| IDM_UNDOFREE | Правка → Очистить историю; RequestClearHistory → ClearHistory | **есть маршрут** — [HistoryClear](HISTORY_CLEAR.md), сохраняются depth, counters и live bytes; UI — M10/M20/M23 |
| IDM_COPY, IDM_CUT, IDM_PASTE, IDM_LOADBRUSHINSEL, IDM_LOADBRUSHINCOPY, IDM_SELECTION, IDM_COPYZONE | Правка → copy/cut/paste, import into selection/buffer, export selection/buffer | **есть маршрут** |
| IDM_ROTATEP90, IDM_ROTATEM90, IDM_SYMHORZ, IDM_SYMVERT, IDM_SELECTCHANGE, IDM_SELECTALL, IDM_SELECTNONE | Правка → повороты/отражения/границы/all/none | **есть маршрут** |
| IDM_FIND | Source Find_Select: сообщение OK, алгоритма поиска нет | **source stub** — SEARCH_SOURCE_SCOPE; новый поисковый workflow не требуется. |
| IDM_CREATURES, IDM_DOORSTRAPS, IDM_OBJECTS | Три прямых менеджера физических объектов; общий factory и family | **есть маршрут** |
| IDM_HEROS, IDM_AREAS, IDM_GATES, IDM_EFFECTS, IDM_PATHS, IDM_EVENTS | Шесть direct workspace managers; собственные панели и entry guards | **есть маршрут** |
| IDM_MAPSTATS | Вкладка статистики; неизменяемая проекция, переходы к объектам | **есть маршрут** |
| IDM_CREATURESPROPERTIES | Определения → CreatureGlobals; общий приватный draft | **есть маршрут** — Содержание страниц отдельно I11/I12, не объявляется закрытым маршрутом открытия. |
| IDM_GAMEPROPERTIES | Определения → GlobalVariables; общий приватный draft | **есть маршрут** — Полнота callbacks отдельно I11. |
| IDM_MAPPROPERTIES | Правка → Свойства карты; общий MapPropertiesSession | **есть маршрут** |
| IDM_THUMBNAIL, IDM_FULLSCREEN, IDM_KEEPPROPS | Панель карты → thumbnail/full map/keep proportions | **есть маршрут** |
| IDM_TOOLBAR, IDM_MINIMAP | Вид → панели инструментов / миникарта | **есть маршрут** |
| IDM_MODIFYCREATURE, IDM_MODIFYDOOR, IDM_MODIFYTRAP, IDM_MODIFYOBJECT, IDM_MODIFYMAGICALOBJECT | Правка → Настройка кисти объектов; явные пять kind → RequestObjectBrush | **есть маршрут** — [Приватные шаблоны, Cancel и выбор инструмента после принятия](OBJECT_BRUSH_COMMANDS.md); WPF только compiled, actual UI — M11/M20/M23. |
| IDM_FLAGTERRAIN, IDM_FLAGCREATURES, IDM_FLAGDOORS, IDM_FLAGTRAPS, IDM_FLAGMAGICALS, IDM_FLAGOBJECTS, IDM_FLAGAREAS, IDM_FLAGPATHS, IDM_FLAGGATES, IDM_FLAGEFFECTS, IDM_FLAGCAMERAS, IDM_MOREDETAILS, IDM_EXPANDGATES, IDM_OBJECTSCOLOR, IDM_MAGICSCOLOR | Вид → terrain / physical objects / overlays; существующие общие настройки и render | **есть маршрут** |
| IDM_QUIT | Файл → Выход; Close + существующий save/discard lifecycle | **есть маршрут** |
| IDM_HELPCONTENT | Справка → Руководство пользователя; RequestGuide | **есть маршрут** — [Встроенное RU/EN руководство](HELP_SECTION_VERSIONS.md), без внешнего viewer; actual UI M20/M21/M23. |
| IDM_HELPVERSIONS | Справка → Версии секций проекта; RequestSectionVersions | **есть маршрут** — [Девять входных/рабочих headers](HELP_SECTION_VERSIONS.md), native N/A; actual UI M20/M21/M23. |

## Диапазоны и специальная пара

| Source диапазон | Source caller | Текущий вход / существующий scope |
| --- | --- | --- |
| IDM_MAPWORLDSTRT..IDM_MAPWORLDSTOP | Toolbar_ChangeWorldElem | TerrainPicker / Paint_Changed; [EDITOR_TERRAIN_PLACEMENT.md](EDITOR_TERRAIN_PLACEMENT.md) |
| IDM_MAPWORLDHEROPORTALSSTRT..IDM_MAPWORLDHEROPORTALSSTOP | Toolbar_ChangeGate | TerrainPicker + VariantPicker; gate catalogue; [EDITOR_TERRAIN_PLACEMENT.md](EDITOR_TERRAIN_PLACEMENT.md) |
| IDM_PLAYERSSTRT..IDM_PLAYERSSTOP | Toolbar_ChangePlayer | OwnerPicker / Paint_Changed; [EDITOR_TERRAIN_PLACEMENT.md](EDITOR_TERRAIN_PLACEMENT.md) |
| IDM_ROOMSSTRT..IDM_ROOMSSTOP | Toolbar_ChangeRoom | TerrainPicker + VariantPicker; room catalogue; [EDITOR_TERRAIN_PLACEMENT.md](EDITOR_TERRAIN_PLACEMENT.md) |
| IDM_PAINTMODESTRT..IDM_PAINTMODESTOP | Toolbar_ChangePaintMode | ToolPicker / Tool_Changed; [EDITOR_DRAWING_HISTORY.md](EDITOR_DRAWING_HISTORY.md) |
| IDM_PUTSTRT..IDM_PUTSTOP | Toolbar_ChangeMiscMode | TemplatePicker / single object painting; [EDITOR_OBJECT_PAINTING_CALLERS.md](EDITOR_OBJECT_PAINTING_CALLERS.md) |
| IDM_ZOOMSTRT..IDM_ZOOMSTOP | Zoom_Change | ZoomPicker / ZoomPreset_Changed; [TERRAIN_ZOOM.md](TERRAIN_ZOOM.md) |
| IDM_RECENTITEMSFIRST..IDM_RECENTITEMSMAX | Files_LoadRecentMap | RecentFilesMenuItem / OpenMapFromPath; [RECENT_FILE_COMMANDS.md](RECENT_FILE_COMMANDS.md) |
| IDM_MAPWORLDWALL / IDM_MAPWORLDCLAIMED | Toolbar_ChangeWall | TerrainPicker/OwnerPicker → Paint_Changed; внутренние placement branches — I07 |

## Закрытый кодовый подпункт MapViewFlags

Все15 битов Config.MapViewFlags имеют текущий маршрут:1 terrain,8 физических
display options и6 overlays. Для каждого source ID сверены literal value,
EN/RU resource, настройка, переключатель и потребитель Canvas. Restore/capture
идут через AppSettings.MapObjects/MapOverlays/ShowTerrain; изменение одного
бита сохраняет остальные, включая неизвестные. Paths синхронизирован с
PatrolToggle. Категорийные defaults включены, Detailed включён; ExpandedGates
и оба owner colour по умолчанию выключены. Ранее исполненные проверки описаны
в [MAP_OVERLAYS](MAP_OVERLAYS.md); повторного прогона не было.

Исправлен конкретный пропуск доступа: прежде все15 переключателей находились
в скрываемой MapToolbar. Теперь «Вид» содержит terrain и два подменю physical
objects/overlays. Отметки читаются из тех же настроек при открытии меню;
щелчок вызывает существующие Configure* и обновляет toolbar controls. Нового
хранилища флагов, новых bytes карты, истории или семантики renderer нет.
Уже закрытое восстановление миникарты при скрытой панели сохранено.

MapViewFlags как состав/битовые маршруты снимается из остатка I13/I17.
Изображения и подписи других каталогов остаются I13; растеризация, мышь и
DPI остаются M21/M23. Это не основание закрыть весь I17.

## Конкретный остаток главных команд I17

Все70 явных source IDs из таблицы сопоставлены с текущими маршрутами либо
исходным Find stub. Справка/сводка версий подключены в HelpSectionVersions.
Неподключённых primary entry points не осталось. I17 остаётся открытым по
содержимому собственных command/callback и keyboard callers; наличие маршрута
не подтверждает правильность каждого внутреннего действия или физический GUI.
Собственные callbacks менеджеров и соответствие shortcut callers остаются
отдельной конечной сверкой I17; таблица входов не объявляет их закрытыми.
Полнота параметров сценария и глобальных определений сохраняется I10/I11/I12.

## Ручная приёмка изменения

1. Скрыть «Вид → Панели инструментов», затем в том же меню отключить и включить
   terrain, любой физический слой и любую область/патруль/камеру.
2. Вернуть панель: отметки совпадают. Изменить отметку на панели, снова открыть
   «Вид»: состояние совпадает. Проверить Paths и PatrolToggle.
3. Повторить EN/RU и DPI100/125/150; карта/выделение/буфер/Undo не меняются.
   Закрыть и открыть приложение самостоятельно: сохраняются прежние настройки.

Сборка/публикация подтверждаются exit records этапа. Тесты, WPF, Computer Use
и игра на этом этапе не запускались. Core и файловый формат не менялись.
Полный корпус и прежние успешные проверки повторять для этого не требовалось.

## Обновление HistoryClear-20261018

UNDOFREE закрыт отдельным маршрутом. Исторические указания «Core не менялась»
и «тесты не запускались» выше относятся к SourceMenuInventory. Текущий additive
ClearHistory проверен отдельным focus02; [подробности](HISTORY_CLEAR.md).
Текущий inventory с7 незакрытыми primary IDs сохранён в evidence HistoryClear.

## Обновление ObjectBrushCommands-20261018

Пять MODIFY* подключены. [Подробности и ручная приёмка](OBJECT_BRUSH_COMMANDS.md).
Текущий inventory с2 незакрытыми primary IDs сохранён в evidence этого этапа.

## Обновление HelpSectionVersions-20261018

[Два последних primary entries подключены](HELP_SECTION_VERSIONS.md). Текущий
inventory с пустым UnclosedPrimaryMenuIDs сохранён в evidence этапа; это
не I17 complete и не ручная приёмка70 команд.

## SourceKeyboard-20261018

[Конечная таблица клавиатуры](SOURCE_KEYBOARD.md):19 новых бесконфликтных
комбинаций подключены;2 конфликта ожидают ответа пользователя.
[3 Insert callers и8 scroll families подключены](SPECIAL_KEYBOARD.md);
полнота собственных manager/scenario callbacks остаётся в I17.

## ManagerCommandScope-20261018

[Конечная таблица внутренних parent controls](MANAGER_COMMAND_SCOPE.md):46 IDs,
11 notifications. Tree Insert/Delete подключены; осталось6 конкретных групп
MC01–MC06 внутри I17 и2 pending shortcut conflicts. Общая историческая фраза
«другие manager callers» не добавляет новую очередь вне этой таблицы.


## ManagerDestinationCoordinates-20261018 — кодовое закрытие I17

Конечный объём этой таблицы включён в [завершённую сверку I17](I17_CODE_CLOSURE.md).
MC01–MC06 и destination106/107/186/187 закрыты по коду; Ctrl+N/Y согласованы.
Исторические pending строки выше не добавляют новых задач. Физическая
приёмка в M остаётся открытой.
