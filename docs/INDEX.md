# Индекс документации

10 ноября 2026: продукт переименован в **DK2 Ultimate Editor**;
запуск — `artifacts/PreFinal/App/UltimateEditor.exe`.
[Репозиторий](https://github.com/Quuz/Dk2UltimateEditor) — публичный, по решению пользователя.
[Описание переименования и публикации](ULTIMATE_EDITOR_REPOSITORY_20261110.md).
Полное решение собрано без ошибок/предупреждений; фактический запуск EN/RU
подтвердил новый заголовок. Форматы и история не менялись, новых manual PASS нет.
Остаток прежний:105сценариев,6приняты/98открыты/1нет входа; цель активна.
Ниже сохранены последние функциональные результаты и прежняя очередь M02/M15.

Актуально 4 ноября 2026: [native-настройки игрока](NATIVE_PLAYER_SETTINGS_20261104.md)
и [происхождение определений](DEFINITION_SOURCES_20261104.md).
M15-02 принят в DKM/native: частный draft/общая отмена, один полный UndoRedo,
source-профиль2/gold5432/health61/камера15,17, полные Players/Things/export7/reopen.
M01-01/02 приняты: общие16 и локальные8 буферов точны; у того же Goblin
ExpPts200 из Creatures.kwd и201 из Globals.kld, пути предупреждений/inspector согласованы.
Игра осталась на Flame unpackResources до наблюдаемой загрузки карты;
ресурсы не приняты, все746 Maps/config/logs восстановлены. Физические клавиши
камеры открыты: CU не вернул отдельное окно; MCP Cancel сохранил весь документ.
Редактор — чистый RU AcceptedNative без модальных окон.
[Перечень](MANUAL_REMAINING_20261104.md):105сценариев,6приняты/98открыты/1нет входа.
Оконная матрица EN/RU/DPI отдельно; группы целиком не объявлены закрытыми.
Цель активна, KWD-миграция не начата. Далее оставшиеся поля игрока и M02.
Ниже исторические записи, их очереди и counts не текущие.


Актуально 4 ноября 2026, следующий проход: [официальная камера и настройки игрока](OFFICIAL_CAMERA_AND_PLAYER_SETTINGS_20261104.md).
Официальный DK2Editor увидел Keeper1→Time>=45→Path50/AP1/ShowCeiling; файлы прежние.
В DKM/RU125% общие Cancel/Apply настроек игрока прошли:84source присваивания ИИ,
gold4321/health83, все7строк, один полный UndoRedo, Save/reopen/compile7 точны.
Native Players/Things целиком сверены с исходной C-разметкой; игрокам/сердцам
посторонних правок нет. Native-GUI ветка M15 остаётся открытой, игра не запускалась.
Редактор — чистый прежний RU AcceptedProject. Для дочерних окон использовать
возвращённый Window из sky.list_apps(), а не disabled owner из list_windows().
[Перечень](MANUAL_REMAINING_20261104.md):105сценариев,3приняты/101открыт/1нет входа;
частичный M15 учтён отдельно. Все23группы частичные; цель активна, KWD-миграция не начата.
Ниже — более ранние датированные отчёты; их counts/очереди исторические.

Актуально 4 ноября 2026: [два новых принятых маршрута](SAVE_CONSENT_AND_GAME_CAMERA_20261104.md).
В игре сработал player3→elapsed45→camera050, cinematic включился и вернулся HUD.
В настоящем RU125% SaveAs отмена предупреждения преобразования сохранила все
буферы/dirty/revision/Undo/target; DKM/ZIP не появились. Все746 игровых map-файлов
и прежние настройки восстановлены точно. Редактор — чистый RU AcceptedProject.
[Конкретный остаток](MANUAL_REMAINING_20261104.md):105сценариев (2приняты,
102открыты,1нет подлинного входа) и отдельная матрица66окон EN/RU/DPI со сверкой
прежних доказательств. Это сценарии с вариантами, не число кликов или процент.
Все23группы частичные; цель активна. KWD без DKM/ZIP ещё не начат.
Ниже — прежние датированные результаты; старые очереди/counts не текущие.

Актуально 3 ноября 2026: [M22 — поиск камеры, DKM и MCP25](CAMERA_SEARCH_LIVE_20261103.md).
Исправлен поиск видимого050: новая регрессия FAILдо/PASSпосле,4набора/8модальных
окон EN/RU×native/DKM. Добавлен ограниченный MCP для уже открытой библиотеки.
Опубликованный EN125% DKM: unknown9999, Search050/portal, выбор050, Cancel,
один UndoRedo; Save/reopen всех7действий, compile7/чтение KWD50/1/0 точны.
43проекта и MCP transport25 PASS. РедакторRU, чистый AcceptedProject, без модальных окон.
Все23группы частично/0полностью. Далее движение камеры/сценарий в игре и остатки
M23 EN/DPI/hardware; игровые эффекты M03 и прочие остатки M01–M23 сохранены.
Миграция KWD без DKM/ZIP ещё не начата. Ниже — предыдущие результаты/старые очереди.

Актуально 3 ноября 2026: [M03 — диалоги сохранения и реальная ошибка записи](SAVE_DIALOGS_LIVE_20261103.md).
Настоящий GUI RU125%: SaveAs Cancel, overwrite Нет/Да, реальная блокировка замены
файла/ошибка/повторный Ctrl+S; буферы/Undo/прежние DKM+ZIP при отказе точны.
GUI ExportCopy создал DKM+точный ZIP; копия открыта, изменена, Ctrl+S/reopen точны.
Все1672 клетки проверены по фактическому KMAP. Исправлено неверное обещание Data
в сообщении экспорта EN/RU. Вся цель открыта:23группы частично,0полностью.
Следом M22 поиск/исполнение и M23 EN/DPI; игровые эффекты M03 и остальные
остатки M01–M23 сохранены. Миграция KWD без DKM/ZIP ещё не начата.
Ниже — предыдущие результаты; старые очереди не текущие.

Актуально 3 ноября 2026: [M22 — камера и исправление вылета мастера](CAMERA_PATHS_LIVE_20261103.md).
Native RU125%:52номера, схемы/потолок050/150, Cancel; путь150/потолок1
применён одним UndoRedo, ровно2байта Triggers.kld, область1 и другие файлы прежние;
export7/reopen точны. Исправлен Owner дочерних камеры/флагов появления в мастере.
Новая регрессия: FAILдо/PASSпосле,4набора/24модальных открытия; build43 без ошибок.
Опубликованный RU125% мастер больше не вылетает; child050/общая Отмена
сохранили прежние150/1/1 и полный fingerprint/revision0/history0.
Все23группы частично,0полностью закрытых. Это не процент готовности.
Далее остатки M03(сохранение), M22(поиск/исполнение), M23(EN/DPI); остальные
остатки M01–M23 сохранены. РедакторRU, чистая CameraPathsLive/AcceptedAreaNative,
без модальных окон. Игровой слот не менялся, игра не запускалась.
Основной Save ещё DKM/ZIP; переход на KWD — после ручной приёмки.
Ниже — история; прежние очереди и counts не текущие.

Актуальный результат1ноября: [сценарная привязка участника и перенос](BAND_SCENARIO_20261101.md). Реальные мастера, moves/UndoRedo/Save/KWD подтвердили uniqueid1→root7; исходные записи сохранены.18групп частично/5без основного прохода/0полностью. Следом nativeGUI и остаткиM13, затемM16/M17. Ниже исторические очереди.

[Последний проход: связи патрулей и кнопки EN/RU](PATROL_LINKS_20261101.md) — добавление/вставка/цикл/UndoRedo/сохранение, текущая очередь.

[Последний проход: области, камеры, сценарная цель и патруль](LINKED_RESIZE_REGIONS_20261101.md) — актуальный результат M09/M12/M18 и очередной остаток.

[Новый проход: девять привязок, объектный буфер, общие Apply/Cancel EN/RU](LIVE_RESIZE_CLIPBOARD_20261101.md) —
актуальный остаток M07/M08/M09/M14/M23; источник текущей очереди.

[Результаты живого MCP и ручных проверок1ноября](MANUAL_MCP_ACCEPTANCE_20261101.md) —
конечный остаток M01–M23, исправленные UI-дефекты, ограничения игрового запуска.
[Локальный MCP, 24 команды, запуск и ограничения](EDITOR_MCP.md) — opt-in мост
к настоящему WPF-документу; headless и живые команды проверены.

Получен результат пользовательской приёмки26.10:
[сравнение наших, Pro и old сохранений](OFFICIAL_RESAVE_COMPARISON_20261026.md).
Крест сохранён и виден в игре; базовая частьM03 и обычные Undo/Redo приняты.
Следующая очередь —M02/M04 по [ручному плану](MANUAL_ACCEPTANCE_RU.md);
M03/M10 целиком не закрыты. Переход на KWD остаётся следующим этапом.

Актуальная [пошаговая ручная приёмка23 групп](MANUAL_ACCEPTANCE_RU.md):
начать с продолжения M01/M03 на уже сохранённом DKM. После её завершения —
[переход на KWD без рабочего DKM и исходного ZIP](KWD_NATIVE_TRANSITION_PLAN.md).
Миграция пока запланирована; порядок утверждён пользователем26.10.

Для M10 подготовлен [human-only notice harness](M10_MANUAL_NOTICE.md): реальное окно через штатный dispatcher route, controlled failure, без реального OOM/сохранения настроек. Исправлен запуск стенда; RU-notice частично проверен, сброс/EN/DPI ещё не приняты. Остаток0I+23M.
Кодовая сверка I01–I18 завершена. [Последний I18](I18_CODE_CLOSURE.md), [25 identity policies](I18_IDENTITY_POLICY.md), [последовательная ручная приёмка23 групп](MANUAL_ACCEPTANCE_RU.md). Начать M01/M03; весь goal пока не завершён.
[I10: конечная кодовая сверка](I10_CODE_CLOSURE.md), [матрица103 типов/15 контекстов](I10_CALLBACK_MATRIX.md).69 parameter/26 RepeatOnly/8 NoPage; новые runtime checks не запускались. Остаток1I+23M=24.
[I09: ворота при повороте и отражении](I09_CODE_CLOSURE.md):12 маршрутов,4 новых PASS; variant и gate-region anchor сохраняют source semantics. Остаток2I+23M=25.
[I08: отряды и специальные правила selection/paste](I08_CODE_CLOSURE.md).15 маршрутов,5 новых логических PASS; product/runtime unchanged. Остаток3I+23M=26.
[I07: конечная сверка14 маршрутов](I07_CODE_CLOSURE.md), [исправление заливки](EDITOR_FLOOD_COMPONENTS.md). Текущий остаток4I+23M=27.
[I04 закрыт по коду](I04_CODE_CLOSURE.md): общий KWD guard, safe parent variable deletion и конечная [таблица20форматов](I04_FORMAT_MATRIX.md). Текущий остаток5I+23M=28.

[Исторические секции и неизвестные native-файлы](HISTORICAL_SECTION_INSPECTION.md):42 выбранные группы PASS. [Конечная таблица I04](I04_FORMAT_MATRIX.md) содержит20 строк; остался общий guard KWD в F01.

[Инспектор packed definitions и хвосты коллекций](NATIVE_PACKED_INSPECTION.md): общий parser, readonly prefix, единая диагностика, source retention/loss consent и51 выбранная группа PASS.

[Native D5/D6 блоки и перенос action bindings](NATIVE_SCRIPT_PARTITION.md): original-reader compatible insertion, split-count guards, private target identity, exact Undo/Redo и67 выбранных групп PASS.

[Физические layouts игровых коллекций и порядок рабочих владельцев](NATIVE_COLLECTION_GUARDS.md): общий inspector/load guard, атомарный отказ, exact retention, 57 выбранных групп PASS.

[Границы игровых коллекций](NATIVE_COLLECTION_BOUNDARIES.md): вставка по фактическим записям, безопасное удаление, opaque suffix, точный Undo/Redo.

[Packed definitions иopaque suffix](NATIVE_DEFINITION_TAILS.md): shared walker/copy,loss consent/source retention,seven-file real DKM checks.

[Текущие native definition layouts](NATIVE_DEFINITION_LAYOUTS.md): общий header guard7семейств,verified prefixes иopaque policy;I04 открыт.

[I03 closed by code](I03_CODE_CLOSURE.md):14routes;[selection owner mapping](SELECTION_OWNER_IDENTITIES.md),5+5PASS.6I/23M remain.

[I03:конечная таблица14маршрутов](I03_ROUTE_MATRIX.md);конкретный открытый риск R14:owner UID при paste.

[Полный диапазон действий в KTRG1 upgrade](HISTORICAL_SCENARIO_COUNTS.md):32768 actions в одном условии,сохранение0x8000,4+1PASS.

[Цель ворот в текстовом MAP](TEXT_MAP_GATE_IDENTITIES.md): рабочая ссылка вместо повторного coordinate lookup, явная граница native copy,6+1+7PASS.

[Отсутствующая цель ворот native→DKM](NATIVE_MISSING_GATE_CONVERSION.md): явный Loss, сохранение ID и защита от переназначения на новые ворота.

[I02 code closure](I02_CODE_CLOSURE.md):28writers,17validation routes/65REPORT IDs;7I/23M остаются.

[Reserved output names](COMPILER_RESERVED_NAMES.md):63 source names,Test info и Generate guard,6PASS.

[Source bounds в Check/Compile](COMPILER_ACTION_BOUNDS.md):отрицательные targets,6 новых и3 связанных PASS.

[Сообщения things/gates](COMPILER_PLACEMENT_DIAGNOSTICS.md):DK2N041–045,first area binding,8 новых и2 связанных PASS.

[Все28 таблиц writers](I02_WRITER_FIELD_AUDIT.md):карта,103 events,262 global fields; далее validator/version audit I02.

[Поля things/variables](I02_THING_VARIABLE_AUDIT.md):155 source stores,15 функций;17 из28 таблиц готовы,11 остаются.

[Сверка metadata/player/AI](I02_METADATA_PLAYER_AUDIT.md):49/91 правил,9 профилей,28 writers с четырьмя pointer helpers.

[Порядок записей Things.kld](COMPILER_THING_ORDER.md):9 исходных этапов,4 новых группы и связанная retention.

[Базовые определения после возврата к defaults](COMPILER_DEFAULT_GLOBALS.md): выбор copy/generate,9 новых групп и связанная regression.

Текущая сборка24 октября: [CompilerGameDates](COMPILER_GAME_DATES.md). Исправлены даты создаваемых headers после [DESTROYGATE fallback](COMPILER_GATE_FALLBACK.md); I02 остаётся открытым. Остаток8I/23M.

Для работы с текущим редактором начните с [русского руководства](USER_GUIDE_RU.md),
[English guide](USER_GUIDE_EN.md) и [текущего состояния](CURRENT_STATUS.md).
Команда сборки и структура проекта — в [README](../README.md) и
[архитектуре](ARCHITECTURE.md).

Ниже технические документы по подсистемам. Их датированные испытания описывают
конкретный этап и не являются альтернативными актуальными версиями продукта.
Старые ссылки Test/... относятся к архиву `Archive/obsolete-development-20261011.zip`
с теми же путями внутри. Для сборки извлекать архив не требуется.
Старые прогнозы, блокеры и paths из исторических записей не заменяют CURRENT_STATUS.

## Карты, интерфейс и история

- [CREATURE_LEVEL_MODIFIER_WINDOW](CREATURE_LEVEL_MODIFIER_WINDOW.md) — EN/RU окно, каталог/копия, закрытая кодовая сверка I12 и ручная инструкция

- [CREATURE_LEVEL_MODIFIER_DEFAULTS](CREATURE_LEVEL_MODIFIER_DEFAULTS.md) — shared defaults, missing overrides, D2LO и общий parent Apply

- [CREATURE_LEVEL_MODIFIER_SESSION](CREATURE_LEVEL_MODIFIER_SESSION.md) — private Apply/Undo, DKM delta/source retention, Save/reopen и7-file compile

- [CREATURE_LEVEL_MODIFIER_WRITER](CREATURE_LEVEL_MODIFIER_WRITER.md) — signed DWORD, ноль→100, сохранение неизвестных bytes и границы публикации

- [CREATURE_LEVEL_MODIFIER_DRAFT](CREATURE_LEVEL_MODIFIER_DRAFT.md) — signed ranges,23 official IDs, corrected controls и private draft

- [CREATURE_LEVEL_MODIFIERS](CREATURE_LEVEL_MODIFIERS.md) — official0x41 percentages, readonly decoder и незакрытый I12

- [I17_CODE_CLOSURE](I17_CODE_CLOSURE.md) — конечная кодовая сверка команд/Config
- [MANAGER_DESTINATION_COORDINATES](MANAGER_DESTINATION_COORDINATES.md) — source X/Y, spinners, private picker

- [SCENARIO_NEW_POINT_COMMAND](SCENARIO_NEW_POINT_COMMAND.md) — New Point отдельно от открытия привязки

- [REGION_COORDINATE_CLAMP](REGION_COORDINATE_CLAMP.md) — ранний clamp, стрелки и gate mirror

- [SCENARIO_EXPANSION](SCENARIO_EXPANSION.md) — source expanded field и состояние дерева

- [SCENARIO_INSERTION_COMMANDS](SCENARIO_INSERTION_COMMANDS.md) — явные root/append/insert callers

- [REGION_LIST_MODIFY](REGION_LIST_MODIFY.md) — точная цель double-click и общий private Modify

- [REGION_CREATION_DRAFT](REGION_CREATION_DRAFT.md) — полный черновик и повторное добавление регионов

- [MANAGER_COMMAND_SCOPE](MANAGER_COMMAND_SCOPE.md) — конечная таблица внутренних команд и tree Insert/Delete

- [SPECIAL_KEYBOARD](SPECIAL_KEYBOARD.md) — Insert окна и cell-aligned прокрутка карты

- [SOURCE_KEYBOARD](SOURCE_KEYBOARD.md) — конечная таблица клавиатуры и текущие сочетания

- [HELP_SECTION_VERSIONS](HELP_SECTION_VERSIONS.md) — встроенное руководство и девять версий секций

- [OBJECT_BRUSH_COMMANDS](OBJECT_BRUSH_COMMANDS.md) — пять команд настройки кисти и private Cancel

- [HISTORY_CLEAR](HISTORY_CLEAR.md) — очистка истории с сохранением глубины и будущего Undo

- [MAIN_MENU_SOURCE_MAPPING](MAIN_MENU_SOURCE_MAPPING.md) — конечная таблица главных команд и15 view flags

- [WORKSPACE_MANAGER_ROUTES](WORKSPACE_MANAGER_ROUTES.md) — команды отрядов, регионов, патрулей и сценариев

- [MANAGER_LIST_COMMANDS](MANAGER_LIST_COMMANDS.md) — source списки и прямые команды менеджеров

- [FILE_IO_PROGRESS](FILE_IO_PROGRESS.md) — загрузка, сохранение и source HideDisk/HideSplash

- [COMPILER_TASK_WINDOW](COMPILER_TASK_WINDOW.md) — фоновая задача проверки, компиляции и запуска

- [THUMBNAIL_WINDOW_LIFETIME](THUMBNAIL_WINDOW_LIFETIME.md) — документ и положение окна миниатюры

- [FILE_DIALOG_PATHS](FILE_DIALOG_PATHS.md) —независимые пути chooser и подтверждённая атомарная замена BMP

- [DOCUMENT_REPORTS](DOCUMENT_REPORTS.md) —полные отчёты, source фильтры, навигация и guarded continuation

- [WINDOW_MAP_TITLE](WINDOW_MAP_TITLE.md) —внутреннее имя карты в заголовке, live Rename/Undo и безопасное чтение metadata

- [EDITOR_SOUNDS](EDITOR_SOUNDS.md) —15 штатных WAV, выбор и воспроизведение звуков инструментов/рисования

- [CONFIG_SOURCE_MAPPING](CONFIG_SOURCE_MAPPING.md) —42 поля Config и конкретный остаток I17
- [REGION_UNDO_POLICY](REGION_UNDO_POLICY.md) —согласованное полное Undo регионов; I14 закрыт

- [VIEW_VISIBILITY_COMMANDS](VIEW_VISIBILITY_COMMANDS.md) — скрытие инструментов и команда миникарты

- [NATIVE_CRYSTAL_MUTATION](NATIVE_CRYSTAL_MUTATION.md)
- [NATIVE_GATE_IDENTITIES](NATIVE_GATE_IDENTITIES.md)
- [PROJECT_GATE_REMOVAL](PROJECT_GATE_REMOVAL.md)
- [REGION_REMOVAL_CONFIRMATION](REGION_REMOVAL_CONFIRMATION.md)
- [AREA_REMOVAL_PATROL](AREA_REMOVAL_PATROL.md)
- [USED_AREA_REMOVAL](USED_AREA_REMOVAL.md)
- [REGION_CROP_SCOPES](REGION_CROP_SCOPES.md)
- [NATIVE_OWNER_SCOPES](NATIVE_OWNER_SCOPES.md)
- [SHARED_SCENARIO_REQUESTS](SHARED_SCENARIO_REQUESTS.md)
- [UNDER_CURSOR_REMOVAL](UNDER_CURSOR_REMOVAL.md)
- [MANAGER_CATEGORY_SCOPES](MANAGER_CATEGORY_SCOPES.md)
- [INSPECTOR_OBJECT_REMOVAL](INSPECTOR_OBJECT_REMOVAL.md)
- [SCENARIO_DELETION_REQUESTS](SCENARIO_DELETION_REQUESTS.md)
- [SCENARIO_CHOICE_CAPTIONS](SCENARIO_CHOICE_CAPTIONS.md)
- [SCENARIO_ENTITY_CAPTIONS](SCENARIO_ENTITY_CAPTIONS.md)
- [SOURCE_REQUEST_AUDIT](SOURCE_REQUEST_AUDIT.md)
- [COPY_CONFIRMATION_LIFETIME](COPY_CONFIRMATION_LIFETIME.md)
- [GATE_GENERATION_REQUESTS](GATE_GENERATION_REQUESTS.md)
- [HISTORY_MEMORY_NOTICE](HISTORY_MEMORY_NOTICE.md)
- [BIG_MAP_REQUESTS](BIG_MAP_REQUESTS.md)
- [PLAYER_GOLD_NOTICE](PLAYER_GOLD_NOTICE.md)
- [MAP_NAME_OVERFLOW](MAP_NAME_OVERFLOW.md)

- [NATIVE_GATE_BINDINGS](NATIVE_GATE_BINDINGS.md)

- [RECENT_FILE_COMMANDS](RECENT_FILE_COMMANDS.md) — очистка и проверка списка недавних файлов
- [OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md) — конечные17 исправлений/сверок и23 группы ручной приёмки; I14 согласован
- [REMAINING_WORK](REMAINING_WORK.md)

- [NATIVE_GATE_MUTATION](NATIVE_GATE_MUTATION.md)

- [MAP_GENERAL_SETTINGS_UI](MAP_GENERAL_SETTINGS_UI.md)
- [EDITOR_RESIZE_SCRIPT_TARGETS](EDITOR_RESIZE_SCRIPT_TARGETS.md) — перенос сценарных целей при изменении размера и проверка компиляции
- [EDITOR_RESIZE_PLAYERS](EDITOR_RESIZE_PLAYERS.md) — исходный перенос камер/областей ИИ, признаки по умолчанию и предупреждения
- [NATIVE_RESIZE_DERIVED_TARGETS](NATIVE_RESIZE_DERIVED_TARGETS.md) — вычисляемые цели кристалла/ворот после resize и crop
- [MAP_LEVEL_OPTIONS](MAP_LEVEL_OPTIONS.md)
- [MAP_SPEECH_TEXT](MAP_SPEECH_TEXT.md)
- [NEW_MAP_CREATION](NEW_MAP_CREATION.md)
- [EDITOR_TERRAIN_PLACEMENT](EDITOR_TERRAIN_PLACEMENT.md)
- [RANDOM_PAINTING](RANDOM_PAINTING.md)
- [SELECTION_BUFFER](SELECTION_BUFFER.md)
- [SELECTION_TRANSFORMS](SELECTION_TRANSFORMS.md)
- [TERRAIN_ZOOM](TERRAIN_ZOOM.md)
- [MAP_OVERLAYS](MAP_OVERLAYS.md)
- [HISTORY_FAILURE_ATOMICITY](HISTORY_FAILURE_ATOMICITY.md)
- [EDIT_HISTORY](EDIT_HISTORY.md) — актуальная история, счётчики и выбранный Undo паузы/сценария
- [MAP_STATISTICS](MAP_STATISTICS.md)

## Объекты, области, игроки

- [OBJECT_MANAGER_NAVIGATION](OBJECT_MANAGER_NAVIGATION.md) — просмотр объекта/точки и приватный выбор координат
- [OBJECT_MANAGER_EXECUTION_FLOW](OBJECT_MANAGER_EXECUTION_FLOW.md) — запуск Add/Modify, шаблон, Ask Options и Preview
- [OBJECT_MANAGER_OPTIONS](OBJECT_MANAGER_OPTIONS.md) — отдельные приватные настройки добавления/изменения
- [OBJECT_MANAGER_COMMANDS](OBJECT_MANAGER_COMMANDS.md) — добавление и локальный Undo
- [OBJECT_MANAGER_SESSION](OBJECT_MANAGER_SESSION.md)
- [OBJECT_BATCH_INSERTION](OBJECT_BATCH_INSERTION.md)
- [OBJECT_BATCH_MODIFICATION](OBJECT_BATCH_MODIFICATION.md)
- [MAP_REGION_EDITING](MAP_REGION_EDITING.md)
- [REGION_PROPERTIES_SOURCE_SCOPE](REGION_PROPERTIES_SOURCE_SCOPE.md)
- [REGION_PROPERTIES_SESSION](REGION_PROPERTIES_SESSION.md)
- [REGION_GEOMETRY_PICKING](REGION_GEOMETRY_PICKING.md)
- [REGION_GATE_GENERATION](REGION_GATE_GENERATION.md)
- [HERO_GATE_GENERATION](HERO_GATE_GENERATION.md)
- [HERO_GATE_GENERATION_COUNTERS](HERO_GATE_GENERATION_COUNTERS.md)
- [EFFECT_GENERATORS](EFFECT_GENERATORS.md)
- [HERO_BAND_ACCEPTANCE](HERO_BAND_ACCEPTANCE.md)
- [PLAYER_SETTINGS](PLAYER_SETTINGS.md)
- [PLAYER_CUSTOM_AI](PLAYER_CUSTOM_AI.md)
- [PATROL_ROUTES](PATROL_ROUTES.md)
- [PATROL_EDITOR_CALLERS](PATROL_EDITOR_CALLERS.md)
- [PATROL_SOURCE_GROUPING](PATROL_SOURCE_GROUPING.md)
- [PATROL_MANAGER_SESSION](PATROL_MANAGER_SESSION.md)
- [PATROL_SELECTOR_SOURCE_SCOPE](PATROL_SELECTOR_SOURCE_SCOPE.md)
- [PATROL_AREA_SELECTION](PATROL_AREA_SELECTION.md)
- [ENTITY_REFERENCE_DIAGNOSTICS](ENTITY_REFERENCE_DIAGNOSTICS.md)

## Сценарии

- [SCENARIO_CREATION_WIZARD](SCENARIO_CREATION_WIZARD.md)
- [SCENARIO_NAMES](SCENARIO_NAMES.md)
- [SCENARIO_ALIAS_EVENTS](SCENARIO_ALIAS_EVENTS.md)
- [SCENARIO_DEPENDENT_INPUTS](SCENARIO_DEPENDENT_INPUTS.md)
- [SCENARIO_PAGE_HINTS](SCENARIO_PAGE_HINTS.md)
- [SCENARIO_AREA_HINTS](SCENARIO_AREA_HINTS.md)
- [SCENARIO_PREREQUISITES](SCENARIO_PREREQUISITES.md)
- [EDITOR_DRAWING_HISTORY](EDITOR_DRAWING_HISTORY.md) — current source pen/cursor route, counters/Undo and manual checks
- [DIRECT_SELECTION_EXPORT](DIRECT_SELECTION_EXPORT.md)
- [EDITOR_SELECTION_CALLERS](EDITOR_SELECTION_CALLERS.md)
- [EDITOR_TRANSFORM_CALLERS](EDITOR_TRANSFORM_CALLERS.md)
- [EDITOR_OBJECT_PAINTING_CALLERS](EDITOR_OBJECT_PAINTING_CALLERS.md)
- [PROJECTILE_EDITOR_ACCEPTANCE](PROJECTILE_EDITOR_ACCEPTANCE.md)
- [MAP_CREATURE_MODIFIERS](MAP_CREATURE_MODIFIERS.md)
- [SCRIPT_IDENTITY_SAFETY](SCRIPT_IDENTITY_SAFETY.md)
- [CURRENT_CORPUS_REFUSALS](CURRENT_CORPUS_REFUSALS.md)
- [SCENARIO_PARAMETER_DRAFTS](SCENARIO_PARAMETER_DRAFTS.md)
- [SCRIPT_BRANCH_EDITING](SCRIPT_BRANCH_EDITING.md)
- [SCRIPT_TREE_PROJECTION](SCRIPT_TREE_PROJECTION.md)

## Каталоги и определения

- [GLOBAL_DEFINITION_RULES](GLOBAL_DEFINITION_RULES.md)
- [GLOBAL_DEFINITION_DEFAULTS](GLOBAL_DEFINITION_DEFAULTS.md)
- [GLOBAL_DEFINITION_IMAGES](GLOBAL_DEFINITION_IMAGES.md)
- [CREATURE_ATTACKS](CREATURE_ATTACKS.md)
- [CREATURE_JOBS](CREATURE_JOBS.md)
- [CREATURE_JOBS_WARNING_REQUEST](CREATURE_JOBS_WARNING_REQUEST.md)
- [CREATURE_JOBS_CALLBACK_SCOPE](CREATURE_JOBS_CALLBACK_SCOPE.md)
- [GLOBAL_PROJECTILES](GLOBAL_PROJECTILES.md)
- [MAP_GLOBAL_VARIABLES](MAP_GLOBAL_VARIABLES.md)

## Форматы и совместимость

- [OFFICIAL_MAP_BUNDLE](OFFICIAL_MAP_BUNDLE.md)
- [NATIVE_COMPATIBILITY](NATIVE_COMPATIBILITY.md)
- [NATIVE_REFERENCE_PRECEDENCE](NATIVE_REFERENCE_PRECEDENCE.md)
- [NATIVE_PROJECT_CONVERSION_SCOPE](NATIVE_PROJECT_CONVERSION_SCOPE.md)
- [NATIVE_RECORD_RETENTION](NATIVE_RECORD_RETENTION.md)
- [NATIVE_GATE_RETENTION](NATIVE_GATE_RETENTION.md)
- [INACTIVE_BAND_COMPILER](INACTIVE_BAND_COMPILER.md)
- [ORIGINAL_TEXT_EXPORT](ORIGINAL_TEXT_EXPORT.md)
- [AREA_WORKFLOW_SOURCE_SCOPE](AREA_WORKFLOW_SOURCE_SCOPE.md)
- [HISTORICAL_SELECTION_PROJECTION](HISTORICAL_SELECTION_PROJECTION.md)
- [SELECTION_TERRAIN_PROJECTION](SELECTION_TERRAIN_PROJECTION.md)
- [SELECTION_OBJECT_ORDER](SELECTION_OBJECT_ORDER.md)
- [OFFICIAL_SEVEN_FILES_USER_CHECK](OFFICIAL_SEVEN_FILES_USER_CHECK.md)

## Проверки и требования

- [PATROL_WARNING_REQUESTS](PATROL_WARNING_REQUESTS.md)
- [PATROL_REMOVAL_REQUESTS](PATROL_REMOVAL_REQUESTS.md)
- [PATROL_PAUSE_REQUEST](PATROL_PAUSE_REQUEST.md)
- [PATROL_UI_HARNESS_SCOPE](PATROL_UI_HARNESS_SCOPE.md)

- [FEATURE_PARITY](FEATURE_PARITY.md)
- [AUTOMATIC_CLOSURE_INVENTORY](AUTOMATIC_CLOSURE_INVENTORY.md)
- [CREATURE_ATTRACTION_CALLBACKS](CREATURE_ATTRACTION_CALLBACKS.md)
- [EXPANDED_CORPUS](EXPANDED_CORPUS.md)
- [ORIGINAL_GUI_PHYSICAL_ACCEPTANCE](ORIGINAL_GUI_PHYSICAL_ACCEPTANCE.md)
- [MANUAL_ACCEPTANCE_RU](MANUAL_ACCEPTANCE_RU.md)
- [LEGACY_AUDIT](LEGACY_AUDIT.md)
- [ACTIVITY_STATISTICS_SOURCE_SCOPE](ACTIVITY_STATISTICS_SOURCE_SCOPE.md)
- [ACTIVITY_STATISTICS_EVENT_SCOPE](ACTIVITY_STATISTICS_EVENT_SCOPE.md)



CreatureLightCallbacks-20261019: [четыре обработчика ввода света, граница I11 и ручные шаги M19/M21/M23](CREATURE_LIGHT_INPUT_CALLBACKS.md). Кодовая сверка I11 и фактическая приёмка остаются открыты.


CreatureParameterInputs-20261019: [числовые обработчики количества привлечения, следующий Attributes callback и ручные шаги M19/M21/M23](CREATURE_PARAMETER_INPUT_CALLBACKS.md). I11 и физическая приёмка остаются открыты.


CreatureAttributeInputs-20261019: [38 атрибутов, исходные callbacks и ручные шаги M19/M21/M23](CREATURE_ATTRIBUTE_INPUT_CALLBACKS.md). I11 и физическая приёмка остаются открыты.


CreatureAttackInputs-20261019: [16 числовых полей Attacks, исходные шаги, обе панели и ручная приёмка](CREATURE_ATTACK_INPUT_CALLBACKS.md). I11/M19/M21/M23 остаются открыты.


CreatureAuxiliaryInputs-20261019: [семь числовых Possession/CrSpells controls, сохранение controls при шаге и ручная приёмка](CREATURE_AUXILIARY_INPUT_CALLBACKS.md). I11/M19/M21/M23 открыты.


CreatureJobInputs-20261019: [Jobs9 numeric controls/3 shared fields, явный Add/Change и ручная приёмка](CREATURE_JOB_INPUT_CALLBACKS.md). I11/M19/M21/M23 открыты.


GlobalElementInputs-20261019: [33 числовых обработчика Global Elements и сохранение controls при шаге](GLOBAL_ELEMENT_INPUT_CALLBACKS.md). I11/M19/M21/M23 открыты.


ProjectileChildInputs-20261019: [Trigger/shot numeric callbacks, два согласованных исправления и ручные шаги](PROJECTILE_CHILD_INPUT_CALLBACKS.md). I11/M19/M21/M23 открыты.


CreatureResistanceSliders-20261019: [четыре Slider/readout, source None masks, imported raw и ручные шаги](CREATURE_RESISTANCE_SLIDERS.md). I11/M19/M21/M23 открыты.


GlobalCallbackClosure-20261019: [конечная кодовая сверкаI11](I11_CODE_CLOSURE.md). M19/M21/M23 остаются: настоящие14 pages/children/input/DPI/game; новые4 WPF suites только скомпилированы.


CreatureSpellUsage-20261019: [source list123: какие существа используют заклинание](CREATURE_SPELL_USAGE.md). [Конечная дальнейшая граница I13](I13_CATALOGUE_SCOPE.md); I13/M19/M21/M23 открыты.


ScenarioChoiceImages-20261019: [изображения контекстных списков сценария](SCENARIO_CATALOGUE_IMAGES.md). I13 открыт; вложенный состав отрядов ещё не сопоставлен. Новые WPF suites толькоcompiled.


ScenarioBandPreviews-20261019: [состав отрядов в списках сценария](SCENARIO_BAND_PREVIEWS.md). Пять source contexts реализованы;4 WPF suites толькоcompiled. I13 остаётся открытым по остальным разделам.


FieldLabelAudit-20261019: [конечный перечень ресурсов подписей полей](FIELD_LABEL_RESOURCE_AUDIT.md);14 historical captions добавлены. Семантическая сверка I13 остаётся открытой.


ScenarioRadioAudit-20261020: [сопоставление всех50 scenario radio/enum contexts](SCENARIO_RADIO_CAPTION_AUDIT.md);10 пар EN/RU уточнены, коды/порядок сохранены. Прочие разделы I13 и реальная приёмка остаются.


GlobalCatalogueAudit-20261020: [20 глобальных enum tables и26 контекстов](GLOBAL_CATALOGUE_CAPTION_AUDIT.md),3caption keys исправлены; значения/порядок прежние. Остальные разделы I13 и physical QA открыты.


EffectVariableCaptions-20261020: [названия371effects/193variables/20groups](EFFECT_VARIABLE_CAPTION_AUDIT.md) сопоставлены;3effect captions исправлены, IDs и variable definitions прежние. Остальные разделы I13 и physical QA открыты.


BrushPlayerGateCaptions-20261020: [подписи ИИ/быстрых кистей/генератора](BRUSH_PLAYER_GATE_CAPTION_AUDIT.md);13 labels+1choice уточнены, значения/маски/порядок прежние. Остальные разделы I13 и physical QA открыты.


PlayerFieldCaptions-20261020: [подписи ИИ в общей таблице](PLAYER_AI_FIELD_CAPTIONS.md). Значения и форматы прежние; остальные разделы I13 и ручная приёмка открыты.


GlobalElementCaptions-20261021: [подписи глобальных элементов](GLOBAL_ELEMENT_FIELD_CAPTIONS.md).95stored fields/source-control inventory,10уточнённых подписей и контекст активации; остальные части I13 и ручная приёмка открыты.


CreatureFieldCaptions-20261021: [подписи существ](CREATURE_FIELD_CAPTIONS.md).38source attribute names/units и уточнения flags/spells подключены; остальной I13 и manual acceptance открыты.


AIRoomPriorityImages-20261021: [картинки приоритетов ИИ](AI_ROOM_PRIORITY_IMAGES.md).15source images подключены; остальные I13 callers и manual acceptance открыты.


EditorListImages-20261021: [значки списков](EDITOR_LIST_IMAGES.md).PathAreas/PvIcons/TrImages подключены; остальные I13 callers и manual acceptance открыты.


MapRuleFillImages-20261021: [изображения правил и заполнения карты](MAP_RULE_FILL_IMAGES.md).Pools/availability/NewMap image callers подключены; остальной I13 и manual acceptance открыты.


ScenarioPointOwnerImages-20261021: [изображения владельцев точки](SCENARIO_POINT_OWNER_IMAGES.md).109cases/49images и81-call matrix; band detail/header rows и остальные конечные consumers/manual acceptance открыты.


ScenarioPointBandDetails-20261021: [состав отрядов мастера](SCENARIO_POINT_BAND_DETAILS.md).Обе band pages подключены; остальные конкретные I13 consumers/manual acceptance открыты.


MapPaletteImages-20261021: [палитра](MAP_PALETTE_IMAGES.md).53terrain rows/12tools подключены; конкретный остаток: hero preview, scenario target list и reward icon size; object/custom labels и manual acceptance открыты.


RewardDestinationImages-20261021: [награды/цели](REWARD_DESTINATION_IMAGES.md). Размер наград и значки сценарных списков исправлены; Hero preview и object/custom labels I13 ещё открыты.14I/23M сохраняются.


HeroBandDisplay-20261021: [отряды](HERO_BAND_DISPLAY.md). Просмотр45creatures/imported elites/flags и manager unknown fallbacks исправлены; I13 object/custom captions и manual acceptance открыты.14I/23M сохраняются.


DefinitionCaptionRoutes-20261021: [контекстные подписи определений](DEFINITION_CAPTION_ROUTES.md). Generic/raw/decorations routes исправлены; placed-object/data/custom captions и manual acceptance открыты.14I/23M сохраняются.


23 октября2026, PlacedThingCaptions-20261021: [подписи размещённых объектов](PLACED_THING_CAPTIONS.md). Data union/historical versions/hero enemy контексты исправлены; custom captions и manual acceptance открыты.14I/23M сохраняются.


SpatialCustomCaptions-20261023: [области/эффекты/патрули](SPATIAL_CUSTOM_CAPTIONS.md). Подписи6dialogs/scale direction/pause seconds сверены. Остаток I13 captions перечислен тремя конечными группами;14I/23M сохраняются.


MapUICaptions-20261023: [карта/речь/игроки](MAP_UI_CAPTIONS.md). Подписи8source pages сверены,appearance replacements уточнены. Остаток I13 captions:settings/requests/reports иcoverage;14I/23M сохраняются.


ServiceCaptionAudit-20261023: [настройки/запросы/отчёты](SERVICE_CAPTION_AUDIT.md). Service captions сверены;source report icons87–89 подключены. Остаток I13 — source-caller coverage,включая direct image loads;14I/23M сохраняются.


DirectListImages-20261023: [прямые списки](DIRECT_LIST_IMAGES.md). Variable/timer,compiler иgate icons подключены;51direct calls распределены. Конечный остаток I13 дан в документе;14I/23M сохраняются.


DirectionalControlImages-20261023: [стрелки/привязки/выбор](DIRECTIONAL_CONTROL_IMAGES.md).36source resize cases/15ICO проверены;следующий I13:status/legend,camera/request routes иdocument-name/reference coverage.14I/23M сохраняются.


MapInfoImages-20261023: [информация/золото/легенда](MAP_INFO_IMAGES.md).3InfoICO подключены,source gold whole-map/selection реализован,30existinglegend BMP иroute сверены. Direct51/24matrix обновлена:status/legend4calls больше не открыты. Следующий I13:camera2/request3 route verification,document-name/reference/source-caller coverage.14I/23M сохраняются.


RequestCameraImages-20261023: [сообщения/камеры](REQUEST_CAMERA_IMAGES.md).35callers используют source32px icons/ENRU buttons;camera2routes/5ICO/52BMP сверены без повторной реализации. Direct51/24matrix:request2 закрыты по коду;file chooser preview1 (Map/AREA/BMP) остаётся конкретной доработкой. Далее document-name/reference/source-caller coverage;14I/23M сохраняются.


FileChooserPreview-20261023: [предпросмотр chooser](FILE_CHOOSER_PREVIEW.md).8routes подключены к native preview pane;9detached format reads/SDK64 ABI/epochs/read-only guard прошли. Direct51/24matrix последний file-preview call подключён по коду. Actual native layout/focus/overwrite/DPI остаётся manual;I13 ещё document-name/reference/source-caller coverage.14I/23M сохраняются.


DocumentReferencePresentation-20261023: [имена/ссылки](DOCUMENT_REFERENCE_PRESENTATION.md).Live card исправлена,typed reference captions/ambiguity сопоставлены;27fields/7metadata layouts проверены. Для final I13 frozen315UI population sites/70functions требуют route-to-evidence match;это не новый список дефектов или повторных тестов.14I/23M остаются.
