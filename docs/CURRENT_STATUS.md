# Текущее состояние / Current status

10 ноября 2026: продукт переименован в **DK2 Ultimate Editor**;
запуск — `artifacts/PreFinal/App/UltimateEditor.exe`.
[Репозиторий](https://github.com/Quuz/UltimateEditor) — публичный, по решению пользователя.
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

BandScenario-20261101: настоящий GUI RU125%/MCP24. Мастер точкиscope16
выбрал участника2/level3; мастер условияResizeFlag=7 создал uniqueid1/root7.
GUI Ниже→слот3 и Выше→слот2:180полей/всеTRIGGERLINK/TRIGGER/ACTION/прочие
буферы точны, каждый move одним полнымUndoRedo. Дерево реально показывает
участника3/level3 и его корень7. Save/reopen всех связанных записей,
compile7; фактическаяKWD wHeroTrigger7 у героя2/0уостальных, все13полей16
героев и поляусловия7 точны. DKM восстановлена по полномуhash.
[Результат и точный остаток](BAND_SCENARIO_20261101.md).
Код/сборка не менялись; успешные сборки не повторялись.18групп частично/
5безосновногопрохода/0полностью. Следом nativeGUI/удаление/прочие остаткиM13,
затемM16/M17. ОстальныеM сохранены. РедакторRU clean AcceptedLinked44×38,
модальныхокон нет. Игра/установленныйслот не менялись, KWDmigration не начата.
Ниже — история.

BandCapacity-20261101: настоящий GUI RU125%/MCP24. Отряд заполнен до16:
14новых независимых/options2=41; Add общего окна отключён. ОбщийApply одним
точным UndoRedo всех буферов. Основная панель17-й отказала без изменений;
реальные Ниже/Выше меняют только два полных payload,180полей/прочиефайлы/
одиночныеUndoRedo точны. Save/reopen+compile7, KWD все13полей16активных точны.
Исправлены команды основной панели: Adddisabled16, Up/Down на границах и
неактивных слотах. Build43/publish0, ошибок/предупреждений0; настоящие выборы
first/last/неполный отряд подтвердили кнопки. После возврата clean16карты
полныйhash прежний, история0. Core/формат/исходные карты не менялись.
[Результат и точный остаток](BAND_CAPACITY_20261101.md).
18групп частично/5без основного прохода/0полностью. Следом сценарные связи
M13, удаление/компактизация и прочие остатки, затемM16/M17. ОстальныеM открыты.
РедакторRU clean AcceptedCapacity44×38. Игра и установленный слот не менялись,
миграцияKWD не начата. Ниже — история.

HeroBandsLive-20261101: настоящие окна M13/MCP24. Дочерние73/37,
отказgold321, reopen/outerCancel: все буферы/history/revision точны.
GUI Live band:2участника (лидер1/помощник3), здоровье73/золото37,
mercenary8/GoToArea23/area3 через Replace. ОбщийApply однимUndoRedo,
Save/reopen180полей+регионы/сценарий, compile7; фактическая KWD:
GAMEBAND и все поля16слотов точны,14неактивны. Русское имя в WesternAnsi
DKM отклоняется; исправлен неточный общий текст на отдельный EN/RU
NameEncoding. Build43/publish0, реальный повтор сообщения EN/RU125%,
послеCancel карта/history неизменны;2895ресурсов/язык.
[Результат и конечный остаток](HERO_BANDS_LIVE_20261101.md).
18групп частично/5без основного прохода/0полностью. Следом предел16,
перестановки и прочие остаткиM13, затемM16/M17. Остатки иныхM сохранены.
РедакторRU clean AcceptedBand44×38, без модальных окон. Игра и установленный
слот не менялись; native migration не начата. Ниже — история.

GateEffects-20261101: MCP24, живой GUI RU/DPI125%. Выбор точки и прямоугольника
с карты/Hide/Show/Escape, полный общийCancel. Реальная вложенная генерация:
preview0gates/1area/5conditions/5actions;No, rememberYes, подавление следующего
вопроса; общийCancel принятого child сохраняет все буферы/history/revision.
ОбщееApply точки+area Nested4/color0x112233+5conditions/5actions однимUndoRedo.
Эффекты: frequency256 отказ, физическийslider78, дочернийreopen, outerCancel.
ОбщееApply4слотов1/2/3/4 freq78 однимUndoRedo. Save/reopen всех областей/
ворот/сценария точны, compile7файлов. ВыходнаяKWD открыта для чтения:
GAMEEFFECT все10полей точны;6gates(5hearts+1point),4areas. DKM восстановлена
по полномуhash. ИсходныйAcceptedLinks и установленныйслот не менялись.
Продуктовыйкод не менялся; предыдущий build остаётся актуальным.
[Результаты и точный остаток](GATE_EFFECTS_20261101.md).
РедакторRU, clean, AcceptedEffects44×38. Следом отряды M13; остаткиM12 и
прочих групп сохранены.17групп частично,6 без основного прохода,0 полностью.
M15/M18 уже частичные, прежние «не проверено» исправлены по Evidence.
Полная цель открыта; native migration не начата. Ниже — история.

PatrolLinks-20261101: MCP24, живой GUI/MCP. Исправлены пустые Apply/Cancel
в окнах выбора области и паузы (отсутствовали ключи);2894ключа EN/RU.
Реальные EN/RU окна/кнопки показаны при125%. Выбор области: начальная
блокировка, пауза251→250/−1→0, смена кандидата, Cancel без изменений.
GUI Append1→2/пауза15, Insert1→3→2, цикл1→3→2→1: все поля точны,
по1Undo,3Undo/3Redo всех буферов. Запрос удаления: No сохраняет всё.
Save/reopen цикл/паузы, compile7файлов. Исходный AcceptedPatrol не изменён.
[Подробности и остаток M12](PATROL_LINKS_20261101.md).
РедакторRU, clean, AcceptedLinks44×38. Игра/установленный слот не менялись.
Собрано43проекта без ошибок/предупреждений; та же PreFinal/App обновлена.
Следом проходы/эффекты/вложенная генерация M12 и отряды M13. Остатки других
групп сохраняются; полные23M не закрыты, KWD migration не начата.
Ниже — история, а не новая очередь.

LinkedResizeRegions-20261101: MCP24, новый живой проход. Девять привязок
36×30→40×34: область/камера и областьИИ/колдун, все233 поля7игроков,
полные UndoRedo. Геометрия человека не сдвигается согласно исходнику.
Настоящие мастера Map/условие+ResizeFlag/действие22; перенос цели+4,+4,
обрезка с DK2N029 блокирует compile без изменения документа. 44×38 сохранена/
открыта/7файлов. Реальный маршрут и пауза250: Escape/Apply/полныеUndoRedo/
Save-reopen. Исправлена тесная панель патруля и перенос длинных кнопок,
собрано/опубликовано в ту же PreFinal/App. Подробности и остаток:
[связанные записи, сценарий и патруль](LINKED_RESIZE_REGIONS_20261101.md).
РедакторRU, чистая копия AcceptedPatrol44×38; игра и установленный слот в
проходе не менялись. Полные23M открыты, KWD migration не начата.
Следом связи патруля/проходы/эффекты M12 и отряды M13.
Ниже — история; прежняя очередь не является текущей.

LiveResizeClipboard-20261101: MCP24 (read-only fingerprint всех transaction
buffers и служебных метаданных). В живом WPF пройдены9привязок32×24→36×30:
все1080клеток/поля колдуна/одинUndo/точныйRedo. Crop16×14: отказ, Cancel,
явное согласие, удаление1объекта, полныеUndoRedo. Буфер с объектом:
Copy/Paste/CopyRotated/Cut/край; оба поворота и отражения с идентичностью.
В настоящем общем окне: имя+resize+внутреннийGameTicks Apply, затем общий
Cancel — точный hash; общееApply всё однимUndo/Redo — подтверждено.
Исправлены обрезанные подписи Width/Height, провереныEN/RU при125%;
контраст hover подтверждён. Рабочая копия36×30 сохранена/открыта/скомпилирована
в7файлов; установленный слот в этом проходе не менялся. Официальный редактор
оставлен на незавершённом запросе GlobalData, запись не подтверждалась.
[Подробные результаты и точный остаток](LIVE_RESIZE_CLIPBOARD_20261101.md).
[Ручной план](MANUAL_ACCEPTANCE_RU.md). Следом связанные регионы/камеры/сценарии
M09 и M12/M13, затем сложные окна. Полные23M не закрыты, native migration
не начата. Редактор оставленRU на отдельной сохранённой36×30 копии.
КопияExport отличается от исходного32×24 толькоKSTS — штатная статистика;
остальные секции побайтно точны, исходный файл не перезаписывался.
Ниже история предыдущих этапов; их числа инструментов/очередь не текущие.

ManualMcpAcceptance-20261101: по новому разрешению пользователя агент
выполнил часть ручной приёмки через Computer Use и живой MCP. Теперь23
инструмента, новый headless transport PASS, настоящий WPF-документ доступен.
Исправлены вытеснение карты панелями, контраст отчёта и тема производных окон.
M02: GUI32×24/5сердец, Save/7файлов, официальный редактор открыл; MCP16×14
без сердец. Мышью проверены фигуры/случайный Redo/отпускание за картой.
Буфер/оба отражения/оба поворота/Undo проверены по raw-клеткам. Колдун:
уровень3→4, Undo/Redo, Duplicate/Undo, Save/reopen, compile — подтверждены.
M10: исправлен запуск App.xaml; реальный RU-notice частично проверен:
Escape/X/remember/OK/подавление, счётчики сохранены. Сброс/EN/другиеDPI открыты.
DPI125%, основной RU/EN/отчёт проверены; другие окна/DPI остаются.
Игра запущена DKII-DX с фиксированным stem, но остановилась на этапе Flame
unpack; эффекты карты не наблюдались. Установлены только7 тестовых файлов,
прежние сохранены в Evidence/ManualMcpAcceptance-20261101/InstalledPreviewBeforeObjectTest.
[Факты и конечный остаток по группам](MANUAL_MCP_ACCEPTANCE_20261101.md).
[Обновлённый ручной план](MANUAL_ACCEPTANCE_RU.md), [MCP23](EDITOR_MCP.md).
Полные23M не закрыты, миграция KWD не начата. Исходный пользовательский DKM,
Source/Binaries/fixtures не изменялись. Следующая очередь — остатки M06–M11,
затем M09/M12/M13 и окна M14–M22. Успешное без изменений не повторять.
Ниже — история; старые ограничения на GUI и прежние числа инструментов
не являются текущими указаниями.

EditorMcpBridge-20261101: по новому указанию пользователя добавлен локальный
opt-in MCP: 16 инструментов текущего документа, общий Undo/Redo, черновик
свойств Prepare/Apply/Cancel, штатные Save/Compile. Чтение/правки через
Core/Automation, адаптер WPF MainWindow.Mcp.cs, отдельный stdio проект Mcp.
[Запуск, подключение и ограничения](EDITOR_MCP.md). Выходы только внутри
artifacts/McpWorkspace, никакой установки/запуска игры или shell-команд.
WPF/GUI/renderer/game агентом не запускались. Новый headless transport test
проверяет реальную цепочку MCP process → stdio → pipe → Core, не живое окно.
Codex-сервер dk2-editor зарегистрирован, tool_timeout_sec=150, опубликованный
консольный сервер отдельно прошёл initialize/tools/list и ответ закрытого
редактора editor_unavailable. Единое решение43 проектов собрано без ошибок/
предупреждений, новый headless transport test и обе публикации terminal0.
EN/RU по2892 literal keys. Логи/выходы — artifacts/Evidence/EditorMcpBridge-20261101.
Связь с настоящим окном ещё должна быть подтверждена после ручного запуска.
Ручные M-группы не закрываются этим PASS. Переход KWD без DKM/ZIP не начат;
его порядок после ручной приёмки сохраняется. Старые проверки не повторять.
Ниже сохранена история предыдущих этапов; актуальная поставка описана здесь.

ManualHistoricalInputs-20261026: для следующего пользовательского M04
подготовлены6 точных копий реальных GUI_GENERATED_MAP из установленных карт
в artifacts/ManualAcceptance-f3cea5a8/M04-Inputs. Начать Imbalance43×43/2players,
затем при необходимости Ancients120×120/2players; Citadel52×52/3players и
Jibbits128×128 также доступны. Это чтение заголовков/подготовка входов,
не новый parser test или GUI PASS. Исходные hashes сохранены, product прежний.
Реальных старых DKM/DKZ/AREA в просмотренных каталогах нет; fixture текущего
NativeRoundtrip не подменяет historical acceptance. M04 остаётся непроверенным.
Далее нужны пользовательские результаты M02/M04; миграция KWD после приёмки.
Крест/прежние успешные проверки не повторять, CU/GUI агентом не использовать.

OfficialResaveComparison-20261026: пользователь выполнил шаги1–7,
сохранение/компиляция/UndoRedo и официальное открытие подтверждены. Затем
подтвердил видимый крест при запуске ManualAcceptance-f3cea5a8 и дал old-save.
[Отчёт сравнения всех семи файлов](OFFICIAL_RESAVE_COMPARISON_20261026.md):
наш export меняет только5 tiles; Pro/old сохраняют крест, только reserved1→0.
Фиксированный слот содержит прежнюю карту, другой установленный — изменённую;
агент слоты не менял.711 raw events после60-байтного header точны везде.
182 добавленные официальными редакторами переменные точно равны source defaults;
существующие значения и проверенные поля191definitions/337things/3players совпадают.
Остаются raw opaque отличия. Old меняет creature stride5537→5449: удаляет
46×88=4048 opaque bytes. Pro/old также меняют unknown AA/10E/FA; full-byte/
полная семантическая эквивалентность этих данных не заявляется.
Все12 DKM backlink notices относятся к16unused nodes;5 scope notices есть
в исходной карте и всех сохранениях. Данные не ремонтировались.
M03 terrain-cycle принят, вся группа частична; M10 обычный UndoRedo частично
принят, M01 EN/RU/свойства не подтверждены.23M остаются незавершёнными группами.
Далее M02/M04; object param совместить сM11, rules/scenario сM16–19.
Крест/успешное открытие не повторять. Native KWD migration после manual acceptance.
Новых product edits/build/full/GUI/CU/game нет. Разовый read-only probe на
опубликованном Core4bf7c889… собран/прочитал изолированные копии, terminal0;
все исходные hashes повторно сохранены. Старая локация ManualAcceptance.dkm
отсутствует, существующий DKM в maps/artifacts/ManualAcceptance-f3cea5a8/
DK2Preview_6a88240eaf814f78.dkm; отсутствующий файл не восстанавливать.

Порядок пользователя26.10: сначала завершаем [ручную приёмку текущей
сборки](MANUAL_ACCEPTANCE_RU.md), затем реализуем [native KWD без рабочего
DKM и исходного ZIP](KWD_NATIVE_TRANSITION_PLAN.md). Миграция пока только
запланирована. Новая инструкция содержит23 пошаговые группы, очередь прохода,
различия Save/Export/Compile и формат отчёта. Следующий пользовательский шаг —
M01/M03 для уже сохранённого DKM; повторять прежние успешные действия не нужно.
M01 частичный/M03 не завершён,23 группы открыты; текущий Core4bf7c889…,
исправление пропуска ZIP26.10 уже в PreFinal/App. Изменена только документация;
новых сборок/тестов/GUI/game и принятия M-пунктов в этом обновлении нет.

ManualHistoryPresentation-20261025: подготовлен [ручной режим M10](M10_MANUAL_NOTICE.md).
WindowsSmoke принимает --manual-history-memory-notice ru|en, отдельный synthetic
DKM sandbox, autosave0, settings persistence false. Изолированный App override
не запускает обычную загрузку настроек; реальный product dispatcher handler
получает безопасно injected history failure и показывает настоящий notice.
GUI/renderer/игра не запускались; manual acceptance не заявлена. Build-04 Exit0,
0warnings/errors. Product src/Core/App/2890 ENRU literals и4485 game files прежние.
Это подготовка ручного инструмента, не новый дефект редактора и не новый PASS.
Остаток0I+23M неизменен. Ожидается пользовательский результат M01/M03;
после него доступен отдельный M10 проход по инструкции. Не повторять проверки
без изменений и не запускать Computer Use. Полная цель активна.

OwnerScopeNamespaces-20261025: [I18 закрыт по коду](I18_CODE_CLOSURE.md).
Конечная [политика25 классов](I18_IDENTITY_POLICY.md) охватывает event/owner/entity
IDs, scope namespaces, orphan/empty points, inactive band tail, players и unknown layouts.
Исправлены2 дефекта: owner lookup считает дубликаты только среди активных
владельцев того же scope; Copy проверяет уникальность только reachable IDs.
Cross-scope UID не перенумеровывается, inactive tail не получает binding authority.
Посторонние bad IDs не мешают безопасному Copy; реально выбранный duplicate/
dangling target по-прежнему явный отказ. Никакая установленная карта не исправлялась.
Новые8 логических групп и6 related: текущий Core10PASS/0FAIL; ещё4 индивидуальных
PASS на предшествующей namespace-only DLL. Это разные DLL и журналы, не один
новый14-group/full run. Старый focus-01 Exit1 сохранён с2 harness failures:
magic fixture требовалID0x75, existing-root fixture читал stale offset после
вставки link. Исправлены только fixtures; failed cases отдельно PASS. Три других
namespace success не повторялись после изменения только Copy.
Все41build-02/test-build-02/publish-01 Exit0,0warnings/errors. App/test/published Core
совпадают: `f3cea5a8bd33a1cd5776eac1ada720c8ae9a7f82c5d5a38d77251931b8758c57`; App `7f4b23dd5e05372330b2dd915de9f5b275875bc01b0b6622f96c60bb8cc43b0a`.2890 EN/RU literals неизменны.
Source/Binaries/fixtures,4485 игровых файлов,15WAV и corpus inputs сохранены.
Full/GUI/WPF/renderer/CU/audio/game не запускались.1840/63 — исторический full run;
27 карт/63 отказа остаются классифицированными, не63 заданиями на ремонт карт.
Открытых кодовых пунктов I01–I18 нет. Остались23 ранее заданные ручные группы;
они не считаются пройденными по сборке или модели. [Ручной план](MANUAL_ACCEPTANCE_RU.md)
переписан в последовательный текущий checklist без дублирующих исторических сводок.
Далее нужен пользовательский проход M01/M03, затем остальных M-групп. Computer Use
не использовать. Полная цель активна до фактической ручной приёмки. Ниже история.

ScenarioCallbackMatrix-20261025: [I10 закрыт по кодовой сверке](I10_CODE_CLOSURE.md).
Конечная [матрица103 типов](I10_CALLBACK_MATRIX.md):69 parameter pages,
26 RepeatOnly conditions,8 NoPage actions. Из19 source functions извлечены
37 trigger Init/Check/Store cases,33 action Init/Check и32 Store. SHOWCRYSTAL
имеет неактивные Init/Check, но PageID=-1 и нет Store: это не пропущенная страница.
Все69 active triplets и15 stateful callback/context families сопоставлены с
текущими providers, frozen primary-source fixtures и датированными доказательствами.
Alias modes/private composition, terrain owner, area/band context, prerequisite
guards, duration/repeat, paired operands, spawn flags, spatial selection/rectangle,
camera ceiling, speech ID/text/audio lifetime и wizard Back/Next/Finish перечислены.
Нового расхождения в product code не найдено. Source src/tests и runtime bytes
не менялись. Новые runtime tests=0, build/publish не требовались; успешные103
schemas/127 descriptions,100/138 numeric/context projections не перезапускались.
Это статическая конечная сверка исходных веток с существующей реализацией,
а не новый full run и не физическая проверка окна/аудио/игры. Все23 M-группы
сохранены; I18 остаётся для malformed owners/identities.
Core `4227ca830f35d52803c3f1baaef2de179a4d604267ea2e3b39a0ea01c3a5f180`, App `1265f61c9bf4d86a29164f7fae63afb119a826a1beda6bcaefb33b117dd967b1`.2890 EN/RU literals, Source/Binaries/fixtures,
4485 игровых files,15WAV и corpus inputs сохранены.1840/63 — исторический full run.
Остался1 кодовый пункт I18 и23 ручные группы,всего24.
Следующий шаг I18 — конечная политика zero/duplicate/dangling IDs, orphan scopes,
unknown owners и band/member/player variants. Не исправлять карты и не считать
63 известных отказа63 новыми заданиями. Полная цель активна. Ниже история.

GateTransformSemantics-20261025: [I09 закрыт по коду](I09_CODE_CLOSURE.md).
Конечные12 source/caller маршрутов сопоставлены. Source permutes terrain bytes
буквально: gate variant1/2 не меняется на противоположный при повороте.
Размещённые physical things меняют XY; отдельные gate/area/band/effect records
не перемещаются. Project compiler использует неподвижный MapGates anchor и
свежую геометрию: gate37/variant1 — south solid0/open4, variant2 — east solid6/open2;
неориентированный anchor использует исходный fallback33/0.
Новые4 группы/16 fixtures PASS: все6 gate catalogue variants, четыре transforms,
точные native buffers с literal gate orientation/ID, stationary project anchor,
fresh compilation, native→DKM archive/save/reopen, original-source retention
до edits/после Undo и fresh regeneration после changed geometry/Redo.
Product code не менялся. Validation/Core test-build-01 Exit0,0warnings/errors;
focus-01 Exit0. Прежний all41 build EditorFloodComponents остаётся актуальным.
Core `4227ca830f35d52803c3f1baaef2de179a4d604267ea2e3b39a0ea01c3a5f180`, App `1265f61c9bf4d86a29164f7fae63afb119a826a1beda6bcaefb33b117dd967b1`. Runtime bytes прежние, повторная publish не нужна.
Уже успешные square/axis/1+K/provenance/mask tests не повторялись. Full corpus,
GUI/WPF/renderer/CU/audio/game не запускались.1840/63 — только исторический full run.
Все2890 EN/RU literals, Source/Binaries/fixtures,4485 game files и15WAV сохранены.
Осталось2 кодовых пункта I10/I18 и23 ручные группы,всего25.
Следующий участок I10: конечная таблица непокрытых context/callbacks сценариев;
подтверждённые103 schemas/127 descriptions иnumeric/hints не повторять без изменения.
Полная цель активна. Ниже история с прежними counts, не новый scope.

SelectionBandPlacement-20261025: [I08 закрыт по коду](I08_CODE_CLOSURE.md).
Все15 source/caller маршрутов сопоставлены: band-member chooser отличается
от spatial selection, global band/member/region records не переносятся как
размещённые герои. Пять source physical categories соответствуют шести native
типам и Project.MAPTHING0..4. New choices исключают elites, импортированные
elite/unknown значения сохраняются отдельной политикой отображения.
Новые5 логических групп PASS на неизменённом текущем Core: native/project
Copy/DKZ/Cut/Paste с independent global band/member roots, exact physical
payloads на воде без single-object CanPaint,14 partial special terrain cases.
Проверены полные Undo/Redo buffers,1+removed counter, сохранение global records
и root bytes. Три группы подтверждены focus-01, project payload focus-02,
native payload focus-03; это5 логических групп, а не сумма повторов.
Усиление native harness сначала ошиблось в едином имени LocationX; исправлен
только lookup реальных schema fields, byte-equality expectation не ослаблено.
Перезапущен только этот случай; product code не менялся.
Собран Validation/Core, без новых warnings/errors; прежний all41 build из
EditorFloodComponents-20261025 остаётся актуальным при неизменённом product.
Core `4227ca830f35d52803c3f1baaef2de179a4d604267ea2e3b39a0ea01c3a5f180`, App `1265f61c9bf4d86a29164f7fae63afb119a826a1beda6bcaefb33b117dd967b1`. Повторная publish не нужна; оба runtime bytes прежние.
Сохранены2890 EN/RU literals, Source/Binaries/fixtures,4485 игровых files и15WAV.
Full/GUI/WPF/renderer/audio/game/CU не запускались.1840/63 — исторический corpus,
не новый результат. Исправлена неверная ссылка I07 на I05: object placement
evidence — OBJECT_PAINTING и EDITOR_OBJECT_PAINTING_CALLERS; I05 — игроки.
Осталось3 кодовых пункта I09/I10/I18 и23 ручные группы,всего26.
Следующий участок I09: конечная сверка gate orientation/geometry при transform;
успешные square/axis/1+K/provenance проверки не повторять без изменения.
Полная цель активна. Ниже история; прежние counts не задают новый scope.

EditorFloodComponents-20261025: [I07 закрыт по коду](I07_CODE_CLOSURE.md).
Конечная таблица14 маршрутов сопоставляет source/callers для terrain/room/path/
wall/bridge/gate, owner/variant, footprint, границы, fill, random и объектов.
Исправлена [идентичность области заливки](EDITOR_FLOOD_COMPONENTS.md): native
reserved не является внутренним source catalogue byte; fixed owners и import
fallback учитываются только при сравнении. Load не нормализует исходные bytes;
raw FloodFill сохраняет прежнее четырёхбайтовое сравнение. Terrain painting
не создаёт и не перемещает region records; gate transforms остаются в I09.
До исправления6FAIL/1PASS, после7PASS/0FAIL на текущем Core. В уже проходившем
variant fixture исправлен bridge ID10 на17; шесть failing cases сохранены.
Смежные успешные geometry/footprint/history/object/gate проверки использованы
как датированные доказательства, без повторного запуска и без выдачи за новый
full run. Все41 проекта build-01 и publish-01 ExitCode0;0warnings/errors.
Core `4227ca830f35d52803c3f1baaef2de179a4d604267ea2e3b39a0ea01c3a5f180`, App `1265f61c9bf4d86a29164f7fae63afb119a826a1beda6bcaefb33b117dd967b1`; build/test/published Core совпадают.
Все2890 EN/RU literals, Source/Binaries/Fixtures,4485 игровых файлов и15WAV
сохранены. Full/GUI/WPF/renderer/audio/game/CU не запускались.1840/63 относятся
только к историческому full run CreatureLevelModifierWindow-20261019.
Осталось4 кодовых пункта I08/I09/I10/I18 и23 ручные группы,всего27.
Следующий кодовый участок — I08: конечная сверка band selection и специальных
copy/cut/paste placement rules. Полная цель активна. Ниже история с прежними counts.

DescriptorHeaderGuards-20261025: [I04 закрыт по коду](I04_CODE_CLOSURE.md).
Все20 строк [форматов](I04_FORMAT_MATRIX.md) и4 требования закрытия сопоставлены
с текущими providers и датированными доказательствами. F01 использует общий
HeaderProblem: DC4/DD28/DF, bounded prefix2304 до2360, references отдельно.
Поддержаны minimal/full/extended parameters, stale generic size0/12/56,
exact raw Save/Undo/Redo. Ошибочный descriptor сохраняет bounded text preview
и прежний DK2E113; editing отказ. Metadata-only preview не требует tile,
полное Open требует реальный tile companion. Это явная разница маршрутов.
580 descriptor headers read-only:0 нарушений. Исправленный fixture на прежней
DLL:3PASS/3FAIL; новый6PASS. Смежный тест выявил product regression preview,
исправлено без изменения исходного InspectorReferenceChecks.
Дополнительно устранён реальный parent-properties underflow: verification
использует общий safe metadata fallback. Новый позитивный тест воспроизвёл
переполнение; полный MapProperties и12 properties-caller групп PASS.
Итого текущий Core:21PASS; ещё32 смежные группы этого же этапа PASS на build-03.
После них менялась только одна parent formula; неизменённые успехи не повторены.
Это разные датированные DLL, не выданы за один full/current53-group run.
Реальные DKM/level1/seven-file/1800cells/711payloads проверены до этой формулы.
Все41build-04/test-build/publish ExitCode0,0warnings/errors.
Core `af6f064fb6069931cfc62aeb927728d877b4584f3d1d976ba1895f04f1c2273c`, App `b68592738c6518e369934f3fb22de46baeaf84ffbe4815e559c6d593a4ff4f0e`; build/test/App/published Core совпадают.
Все2890EN/RU literals, Source/Binaries/Fixtures,4485 игровых файлов и15WAV
сохранены. Full/GUI/WPF/audio/game/CU не запускались;1840/63 исторические.
Осталось5 кодовых пунктов I07/I08/I09/I10/I18 и23 ручные группы,всего28.
Следующий кодовый участок I07: конечная source/caller сверка drawing/fill/
single-object routes. Полная цель активна. Ниже история; её counts устарели.

HistoricalSectionInspection-20261025: [общая инспекция DKM/AREA и неизвестные native-файлы](HISTORICAL_SECTION_INSPECTION.md).
Предпросмотр использует тот же FromLegacyProject, что открытие, с initializeActivity:false:
без timer, source archive discovery или UI. Проверены22 fixed layouts в обоих
контейнерах:44 позитивных и44 усечённых случая; KTRG2/3 counts/aliases, KGBL arrays,
opaque historical versions, inner framing, KMAP и отдельный enhanced DKZ route.
Unknown native companion виден как Opaque.NativeFile/DK2N049; raw copy exact,
DKM Loss OpaqueNativeFile требует согласия и сохраняет complete source archive.
Прямые guessed fields/span writes отклоняются с полным откатом; typed KWD reference
signature проверяется также при загрузке. Future outer version остаётся отказом.
До8FAIL; промежуточно7PASS/1FAIL: неверное full-container ожидание поправлено
для штатной добавленной KSTS, все исходные sections/order по-прежнему exact.
Два дополнительных boundary-before FAIL; финальные10PASS и32 related PASS,
всего42 выбранные группы PASS на текущей DLL. Оригинальные tests не ослаблены
для проверки opaque bytes. Preview/activity/upgrade/DKZ/packed/native bounds/
conversion/real DKM/level1/1800cells/seven-file Save/reopen проверены.
Все41build-02/publish ExitCode0,0warnings/errors.
Core `75009956e942b185205ca206a0e28bb1011e00d5091fd99bfe88cfb96330c169`, App `8d4e480080438ed842423c20aa446c69e260c8cbb27a2bb0fecf8368c2e7dde7`; tested/App/published DLL совпадают.
2890 EN/RU keys, прежние2887 literals, Source/Binaries/Fixtures,
4485 игровых файлов и15WAV сохранены. Full/GUI/WPF/audio/game/CU не запускались.
Исторические1840/63 не пересчитаны по частичному прогону.
I04 сведен в [20 строк форматов](I04_FORMAT_MATRIX.md). Остаётся F01:
information header DD/size28 и minimum metadata prefix KWD не одинаково
проверяются loader/inspector. Следующее действие — общий guard дескриптора,
затем конечный аудит таблицы. 6I + 23M = 29 пунктов; полная цель активна.
Ниже история; её counts/next steps могут быть устаревшими.

NativePackedInspection-20261025: [общая инспекция definitions и collection suffix loss](NATIVE_PACKED_INSPECTION.md).
KWD inspector вызывает тот же статический definition parser, что и Records,
без working document/activity timer. Внутренние count/stride/payload/truncation
проверяются одинаково; unverified schema DK2N008 и opaque data DK2N048 совпадают.
Readonly root F0/104/10E/FA больше не скрывает последующие named editable frames.
Unknown stride сохраняется одним opaque frame, без speculative row allocation.
DKM получает OpaqueCollectionData Loss для хвостов B4/BE/D2/E6. Отказ согласия
не пишет проект; после согласия source archive exact. На всех4 типах реально
подтверждено исключение96-byte suffix при пересборке. Unknown whole header
остаётся UnverifiedCollection Error. Raw-copy bytes не меняются.
До5FAIL/2PASS, после7PASS; отдельный unknown-stride-before1FAIL, финальный8PASS.
Related43PASS, всего51 выбранная группа PASS на текущей DLL. Реальные DKM/level1,
семь файлов,1800cells и711event payloads, aliases и повторное сохранение PASS.
Все41build-02/publish ExitCode0,0warnings/errors; промежуточные логи сохранены.
Core `52d298ad64fd33b1662a428275d9f82f8813dbebd608a3d7250f5accea2f8cca`, App `49507e724855259c72a051f22564fe418f053be970c11916b283957b0d1c3528`; tested/App/published DLL совпадают.
2887 EN/RU keys:2 новых на язык, прежние2885 literals/Source/Binaries/Fixtures/
4485 игровых файлов/15WAV сохранены. Full/GUI/WPF/audio/game/CU не запускались;
1840/63 — исторический full, не текущий результат.
I04 открыт: historical DKM/AREA inspection, политика wholly unknown native
companions и общая inspector/load/raw-edit/named-edit/export matrix.
6I + 23M = 29 пунктов, полная цель активна. Ниже история.

NativeScriptPartition-20261025: [исправлен порядок native условий и действий](NATIVE_SCRIPT_PARTITION.md).
Новое D5 вставляется перед блоком D6 по conditionCount, D6 — перед opaque suffix.
ActionOffset рабочих привязок переносится до вставки; duplicate target2,
deleted target2 и missing0 не переназначаются по совпавшим координатам.
Shared partition verifier проверяет split counts/signatures во всех counted frames.
Неправильный D2 целиком opaque: одинаковый DK2N047 в inspector/editor,
отказ semantic edit/DKM, exact raw copy без ремонта. OriginalNoScenarios
может явно исключить scripts. Усечение по-прежнему ошибка, suffix не сканируется.
Read-only audit109 установленных Triggers:0 нарушений, SHA до/после совпадают.
До8FAIL, после8PASS: action/condition interleave, moved owner, private targets,
clone, clipboard, stale payload16, suffix96, raw Save/reopen и exact Undo/Redo.
Related59PASS, всего67 выбранных групп PASS на текущей DLL. Реальный DKM/
level1, 1800cells, 7files, source-only gate retention и116 allocation boundaries
проверены. Expansion test исправлен для независимых generated GAMEDATE:
принудительная граница секунды, только14 clock bytes, padding/operands exact,
включая packed Globals; отдельные5 date groups PASS. Product dates не менялись.
Промежуточные2 expansion FAIL сохранены; это не новый полный corpus.
Все41build-01/test-builds/publish ExitCode0, 0 warnings/errors.
Core `29055a63fa87648e0c247987c21f1532304a308f6adcdebb2e9e9c9f5823c5b1`, App `16928897870e331b4422439998b3f9339bbebf75301f54f37853ee8465c122b0`; tested/App/published DLL совпадают.
Все2885 EN/RU literals, Source/Binaries/Fixtures, 4485 игровых файлов и15WAV
сохранены. Full/GUI/WPF/audio/game/CU не запускались;1840/63 исторические.
I03 закрыт по коду, I04 открыт: packed-definition inspector, historical sections,
opaque collection suffix conversion policy и итоговая inspector/load/edit/export
matrix. 6I + 23M = 29 пунктов; полная цель активна. Ниже история.

NativeCollectionGuards-20261025: [единая политика физических layouts и исправление владельцев](NATIVE_COLLECTION_GUARDS.md).
Players/Things/Triggers/Variables используют общий header/record verifier в
инспекторе и редакторе. Unknown header целиком opaque до чтения count;
короткие/расширенные/неизвестные записи без typed fields. Raw Save/reopen
сохраняет bytes, DKM/Text дают явный отказ, OriginalNoScenarios может исключить
только scripts. Общий commit guard защищает прямые span writers и suffix,
resize/selection не угадывают геометрию. Допустимая покраска клеток сохранена.
Исправлена реальная ошибка native owner restoration: source list теперь
сопоставляется в том же category order, что и compiler writer. Лишние виртуальные
orphan roots исчезли; игровые payloads и порядок writer не менялись.
Старое сравнение шаблонов исправлено по source category order; valid-graph
fixture дополнен hero/neutral slots1/2 без ослабления semantic assertion.
Новый результат: guards9 + owner-order1 + related47 = 57 PASS / 0 FAIL,
включая все9 owner scopes, 116 точек отказа allocation, полный Undo/Redo,
реальный DKM/level1, 1800 клеток и повторную запись семи официальных файлов.
Промежуточные FAIL и исправления fixtures сохранены, не выданы за полный corpus.
Все41 проекта build-04 и publish ExitCode0, 0 warnings/errors.
Core `2f5ca3dd0907c9ea8070e1f2b196b903a261902cb2cea4c0e7ab5675250f30e6`, App `67b54f581ff5f562b8bbe74b24d9bc9b9d9f428f8af07f881df4e37843278f19`; tested/App/published Core совпадают.
2885 EN/RU keys, прежние2882 literals, Source/Binaries/Fixtures,
4485 игровых файлов и15 WAV сохранены. GUI/WPF/audio/game/CU не запускались.
Полного corpus не было; исторические1840/63 не обновлены.
I03 остаётся закрыт по коду с дополнительным owner-order исправлением.
I04 открыт. Следующий конкретный дефект: новое native D5 condition должно
вставляться перед D6 actions, как требует Game_ExtractTriggers; сейчас общая
вставка идёт в конец. Нужен перенос private action bindings и split-count audit.
Далее packed-definition inspector, historical sections и общая route matrix.
6I + 23M = 29 открытых пунктов. Полная цель активна. Ниже история.

NativeCollectionLayouts-20261025: [исправлены границы native collections](NATIVE_COLLECTION_BOUNDARIES.md).
Чтение и вставка используют один counted-record walker. Размер payload больше
не служит адресом вставки; opaque suffix и порядок прежних записей сохранены.
Удаление безопасно при заниженном размере. Исправлены обычные и batch routes,
включая адреса переноса владельцев. Decoder полей и framing не менялись.
Read-only inventory: 437 файлов, 111 расхождений размера, 3 усечённых inputs.
До исправления 7 FAIL, после 7 PASS: Add/Duplicate/Delete, batch add/remove/
modify, band deletion; exact Undo/Redo и raw Save/reopen. Related 11 PASS/2 FAIL.
Оба отказа отдельно подтверждены на прежней опубликованной DLL с теми же
сообщениями: Direct native template comparison и valid-graph diagnostics.
Они остаются для классификации в I04/I18; новые успешные результаты их не закрывают.
Реальный DKM/level1, 1800 клеток, aliases и повторная семифайловая запись PASS.
Все 41 проекта и publish: ExitCode 0, 0 warnings/errors.
Core `f4783ab6e04d4d1fdeb4ee5d36fcffbc3cfbdfb3e695558e632530ff619511dc`, App `6409a3625c3c0b7dba92087cfaf5c4cd01b149a10704e0b8c66611c2cc7108ee`; DLL сборки, тестов и поставки совпадают.
2882 EN/RU literals, Source/Binaries/Fixtures, 4485 game files и 15 WAV сохранены.
Full/GUI/WPF/audio/game/CU не запускались. Исторические 1840/63 не обновлялись.
I04 открыт: semantic sizes/header guards прочих collections, исторические
sections и inspector/editable/conversion matrix. 6I + 23M = 29 пунктов.
Полная цель активна. Ниже история; её старые counts/next steps не актуальны.

NativeDefinitionTails-20261025: [единый walker packed definitions иopaque tails](NATIVE_DEFINITION_TAILS.md).
Reader/copy идут поadjacent frames,неищутsignature внутри opaque suffix.
Unknown frames/tails сохранены contiguous; coherent truncated known frame
по-прежнему отказ. Future header opaque; standalone extraction version!=4
отказ. DKM OpaqueDefinitionData Loss требуетсогласия,source ZIP exact.
До8FAIL/1PASS. После8PASS/1FAIL:последний test fixture имелнеполныйbasis,
поэтому official mode небылвключён. Он отдельноисправлен до9families,
official-tail01 PASS;product послеэтого неменялся,остальные8неповторялись.
Related layout6/creature3/conversion5/official6PASS; настоящий DKM/level1,
семь файлов,9+2families/1800cells/aliases/reopen/edit/resave подтверждены.
Все41build01/testbuild01/related/publish01 ExitCode0,0warnings/errors;
focus01 ExitCode1 сохранёнкакпромежуточный,невыданзазелёныйfull.
Core `3ad1f7b153e8465ea8c75babaafa7c0391d006c9e676886392573b1b4fc38886`,App `e35619e2f48d26c570c6217b304113f14958816c404d686a24f90db16d411c91`; проверенные и опубликованные DLL совпадают.
2882ENRU keys,старые2881literals/Source/Binaries/Fixtures/4485game inputs/
15WAV сохранены. Full/GUI/WPF/audio/game/CU небыло. I03 закрыт,I04 открыт:
остальные native collections/исторические sections иобщая route matrix.
6I+23M=29;полнаяцельактивна. Нижеистория,еёcounts/nextstepsустарели.

DefinitionLayoutGuards-20261025: [общий guard семи native definition families](NATIVE_DEFINITION_LAYOUTS.md).
Raw fields/catalogue/creature verifier используют полный header guard каждой
inner секции:version4/info marker+size28/count/define marker/payload/bounds.
Unknown inner headers opaque; SetField отказ,DKM UnverifiedDefinitions,raw
Save/reopen bytes exact. Zero stride при positive count отвергается до allocation.
До3FAIL/2PASS; отдельный zero-stride1FAIL. После6PASS,related3PASS иотдельная
extended future raw-copy1PASS. Все44/46×5449/5537 prefixes и88opaque bytes
сохранены. Inventory93files/567sections/22shapes — read-only,не full run.
Все41build01/testbuild01/focus/related/copy/publish01 ExitCode0,0warnings/errors.
Core `f5c07cd151fe74105e520547ed34315f961f19d57584fcb838366e9f0b9a9c61`,App `918540305d24f6402be0bec89685488a0f7b31447580a6a173dd04a8dd80bda3`; проверенные и опубликованные DLL совпадают.
2881ENRU/Source/Binaries/Fixtures/4485game inputs/15WAV сохранены.
I04 открыт:opaque tails>=56 иpacked/copy boundaries;прочие collection layouts
иобщая inspector/editable/conversion matrix. I03 остаётся закрыт по коду.
6I+23M=29;полная цель активна. Full/GUI/WPF/audio/game/CU не запускались.
Ниже история;старые counts/next steps не создают новые задачи.

SelectionOwnerIdentity-20261025: [I03 закрыт по коду](I03_CODE_CLOSURE.md).
Последний R14 исправлен:paste переносит owner по category+UID и меняет только
активное поле TRIGGERLINK. Inactive bytes сохранены; same-category ambiguity
по-прежнему отказ. До4FAIL/1PASS,после5PASS; direct/buffer DKZ,резервированные
UID/event IDs,Undo/Redo,настоящий DKM Save/reopen и destination aliases проверены.
Related selection-files5PASS. Все41build01/focus01/related01/publish01 ExitCode0,
0warnings/errors. Core `946ead5feca38ca755e881d338bfadceb363de6367ea7720a97d25f04f7856e1`,App `b6d54d8ed099ec6622ba44a33fdf5badfb1b194a7b0971ac2858761e5aed9324`; tests/App/published Core совпадают.
Все14 маршрутов сопоставлены,неучтённых нет. Прежние scoped providers/proofs
проверены поhash; датированные runs не выданы за новый full. M04 остаётся
ручной. 2881ENRU/Source/Binaries/Fixtures/4485game inputs/15WAV сохранены.
Game writers не менялись; full/GUI/WPF/audio/game/CU не запускались.
Осталось6I(I04/I07/I08/I09/I10/I18)+23M=29пунктов;полная цель активна.
Далее I04:конечная матрица поддержанных layouts inspector/editable/conversion,
включая44/46-row creatures иopaque tails;без повторения успешных проверок.
Ниже история,её counts/next steps устарели.

InterformatRouteInventory-20261025: [сведена конечная таблица14маршрутов I03](I03_ROUTE_MATRIX.md).
Зафиксированы load/raw copy, native↔DKM, AREA, KTRG1/2, text import,
две политики text export/ordinary Save, DKZ read/write/convert и paste.
Следующий конкретный участок — R14: IsOwnedLink различает category+UID,
но paste uniqueMapping использует только UID и меняет неактивные target fields.
Runtime reproduction ещё нет; нужен Core-тест cross-category duplicate UID,
затем перенос действительного owner field по category+UID. Same-category
ambiguity guard сохранить; входы не исправлять. Полная I03 ещё не закрыта.
Product/tests/DLL/2881ENRU unchanged; build/test/publish не повторялись.
Core `ffe4d78d5ad5e9376b78781b6b1ba80af8f6ee3f0ed2c99f08a888eafd139db9`,App `002d2bc8a0607fc3d4da29d237bbea7dceeb7514736cdf830cddcff6cf44979c` прежние. 7I+23M=30,полная цель активна.
Таблица — конечные направления,не14новых задач. Следом R14,без широкого
повторного корпуса/GUI/game/CU. Ниже история.

HistoricalScenarioCounts-20261025: [исправлен предел действий в KTRG1 upgrade](HISTORICAL_SCENARIO_COUNTS.md).
Одно условие теперь может содержать все32768 отрицательных action IDs.
Прежний общий Count32767 ошибочно запрещал это при допустимом32767+1 split.
Условия/суммарные пределы/проверки record markers/types/tails/rollback прежние.
До3PASS/1FAIL,после4PASS; настоящий DKM Save/reopen,Undo/Redo и native compile
всех32768 actions сохраняют конечный0x8000 и уникальные IDs. Related1PASS:
все4 condition/10 action recipes,параметры,пустые scopes,backlinks,malformed.
Все41build01/focus01/related01/publish01 ExitCode0,0warnings/errors.
Core `ffe4d78d5ad5e9376b78781b6b1ba80af8f6ee3f0ed2c99f08a888eafd139db9`,App `002d2bc8a0607fc3d4da29d237bbea7dceeb7514736cdf830cddcff6cf44979c`; проверенные и опубликованные DLL совпадают.
2881ENRU literals/Source/Binaries/Fixtures/4485game inputs/15WAV сохранены.
Game writers и другие upgrade recipes не менялись. Full/GUI/WPF/game/CU
не запускались;1840/63 исторические. I03 открыт для остальных направлений.
7I+23M=30пунктов;полная цель активна. Ниже история.

TextGateIdentity-20261025: [исправлена цель ворот в текстовом MAP](TEXT_MAP_GATE_IDENTITIES.md).
Supported native→MAP использует рабочую physical target и projected ID,
а не повторный coordinate lookup. Missing ID0/deleted non-reused ID не
захватывают чужие ворота; выбранный duplicate и Undo/Redo сохраняют цель.
Намеренное source freed-ID reuse прежнее. NativeEditorState сообщает I01/R19:
raw native copy сохраняет игровые bytes,private IDs требуют DKM+source ZIP.
До1PASS/5FAIL; после6PASS. Промежуточное ожидание private IDs от plain KWD
исправлено по уже принятому контракту,сам MAP по-прежнему проверен на ID2.
Related original-policy1PASS/save7PASS. Все41build02/focus02/related/publish01
ExitCode0,0warnings/errors. Core `01542ebf98ebd7a1989156dad1cb68bc4edc6891732c2def7f5cdf59f337c3a5`,App `002d2bc8a0607fc3d4da29d237bbea7dceeb7514736cdf830cddcff6cf44979c`; проверенные и опубликованные DLL совпадают.
2881ENRU keys,старые2880 literals/Source/Binaries/Fixtures/4485game inputs/
15WAV сохранены. Seven-file writer не менялся. Full/GUI/WPF/game/CU не было.
I03 открыт:остальные owner/alias/graph/upgrade/DKZ/text-import routes.
7I+23M=30пунктов;полная цель активна.1840/63 исторические. Ниже история.

NativeMissingGateConversion-20261025: [исправлен перенос отсутствующей цели ворот](NATIVE_MISSING_GATE_CONVERSION.md).
Native→DKM: MissingDestroyGate теперь Loss с явным согласием, missing ID
сохранён; существующая неподдержанная цель и повреждённый граф остаются Error.
Архив сохраняет исходные bytes. Восстановление raw payload запрещено, если
на прежней отсутствующей цели появились новые ворота; Undo/Redo проверены.
До преобразования4FAIL/2PASS; после него5PASS/1FAIL воспроизвели stale-target;
после защиты6PASS. Related conversion5PASS/payload6PASS(1800cells/711events).
Все41build01/focus01/related/publish01 ExitCode0,0warnings/errors.
Core `d5e9f688353b6c8aee7f2b5193f02fb9de75ee206de8433a7dd855dafa2e68eb`,App `d8002b117be7b9f820d89a36e60498ab0eb134cef4870fb87411e9757032e755`; проверенные и опубликованные DLL совпадают.
2880ENRU keys:одно старое сообщение изменено,одно новое добавлено на язык.
Source/Binaries/Fixtures/4485game inputs/15WAV сохранены; seven-file writer
не менялся. Full/GUI/WPF/audio/game/CU не запускались;1840/63 исторические.
I03 открыт:нужна конечная graph/alias/coordinate/loss matrix всех направлений.
Осталось7I+23M=30пунктов;полная цель активна. Ниже история.

CompilerValidatorAudit-20261025: [I02 закрыт по коду](I02_CODE_CLOSURE.md).
Все28writer tables и17validator/generation routes с65source REPORT IDs
сопоставлены,включая13writer/I-O failures.10 action cases/versions13/16/17,
shape/player/thing/gate/root/unused/reserved checks имеют явный provider/policy.
Shared DAG/unsafe source repair/retained malformed unused guards иплатформенные
ограничения указаны явно;нет заявления об их побайтовой эмуляции или OOM test.
Product/tests/Core/App/2879ENRU unchanged; build/publish/tests не повторялись.
Core `5fe1bb8fdc7f0173acbe66376732ad51bd3c506a3432d93b580abb136ba40d36`, App `dd86ac917005f045a504429607e8eb1629cd5f9405b81d3672bdaac27f82e35a` прежние. Датированные terminal proofs не сложены
в новый full;1840/63 остаётся историческим не зелёным корпусом.
Source/Binaries/Fixtures/4485game inputs/15WAV сохранены; docs/архивы обновлены.
Осталось7I(I03/I04/I07/I08/I09/I10/I18)+23M=30пунктов;исходная цель активна.
Далее I03:graph/aliases/coordinates/loss policy между native/DKM/DKZ/textMAP.
GUI/WPF/audio/game/CU не запускались. Ниже история,её counts/next steps устарели.

CompilerReservedNames-20261025: [добавлена исходная проверка63имён](COMPILER_RESERVED_NAMES.md).
Test:DK2N046 Information. Generate:отказ перед первым writer для reserved
output stem,без изменения display title. App/validator/Core используют общий
helper; Save As обновляет stem. Native draft factory/raw SaveCopy прежние.
Source exact case-insensitive matching,никаких prefix/trim guesses.
До3FAIL/1PASS,после6PASS. Все41build01/focus02/publish01 ExitCode0,
0warnings/errors. Core `5fe1bb8fdc7f0173acbe66376732ad51bd3c506a3432d93b580abb136ba40d36`,App `dd86ac917005f045a504429607e8eb1629cd5f9405b81d3672bdaac27f82e35a`, tests/App Core совпадают.
2879ENRU keys,прежние2878 literals/Source/Binaries/Fixtures/4485game inputs/
15WAV сохранены. Writer bodies unchanged; guard добавлен перед generation.
I02 открыт до общей validator/version matrix;28writer tables готовы.
Full/GUI/audio/game/CU не запускались;1840/63 исторические.8I/23M прежние.
Исходная цель не завершена; нижние даты/next steps — история.

CompilerActionBounds-20261025: [убран лишний отказ для отрицательных action targets](COMPILER_ACTION_BOUNDS.md).
PlanUnusedScriptRemoval теперь применяет source upper-bounds-only test для
трёх reachable actions; X<0/Y<0 сохраняются в project LONG. Upper equality
по-прежнему блокируется; unbound cleanup/other guards/raw Resize прежние.
Новые6PASS; before negative/reopen false refusals доказаны,неверный raw-resize
fixture и absent-heart fixture исправлены отдельно,не выданы за product bugs.
Related unused1PASS/map-properties2PASS,включая78script/54player matrices.
Build01 всех41/focus02/related/publish01 ExitCode0,0warnings/errors.
Core `1f09f13bab8f468904fb06cf9d59dcca7f854d4adf8d06ae912a16b4725e244c`,App `f2dbcdec4ecb3cfc248a01f72a65908eccc42176199c1348a28a7527f89b5961`; App/tests/published Core совпадают.
2878 ENRU literals,Source/Binaries/Fixtures/4485game inputs/15WAV сохранены.
Все28writer tables готовы;I02 открыт для оставшегося validator/version audit.
Full/GUI/audio/game/CU не запускались;1840/63 исторические.8I/23M прежние,
полная исходная цель не завершена. Ниже история.

CompilerPlacementDiagnostics-20261025: [подключены сообщения things/gates](COMPILER_PLACEMENT_DIAGNOSTICS.md).
DK2N041–045 в full gameplay check:bad terrain/fatal liquids/wide-tall gates
Warning;enemy и2×2 gate prerequisites Information по source prefixes ?/#.
Нет новых hard errors и auto repair; structural mode прежний. Shared rules
и HeroGateSites используются повторно. First area binding исправлен в общем
helper:поздний duplicate root не скрывает сценарий,отсутствующий в первой.
Новые8PASS,до6FAIL/1PASS; отдельный first-root до7PASS/1FAIL. Related обычный
и nested gate generation PASS. Build03 всех41/focus03/publish01 ExitCode0,
0warnings/errors. Core `73a4ffa7720917117d6bb567bfd3e31b5d72afc0eb965fad47b3dcc8bb885234`,App `f2dbcdec4ecb3cfc248a01f72a65908eccc42176199c1348a28a7527f89b5961`; binaries tests/App совпадают.
2878 ENRU keys,старые2873 literals сохранены.Source/Binaries/Fixtures/4485game
inputs/15WAV прежние. Full/GUI/audio/game/CU не запускались;1840/63 исторические.
28writer tables остаются готовы; I02 открыт для остальных validator/version
routes.8I/23M прежние,полная цель не завершена. Ниже история.

CompilerRemainingWriters-20261024, завершено25 октября: [сведены все28 writers](I02_WRITER_FIELD_AUDIT.md).
Добавлены11 таблиц:MapFile,3event functions,7global families.103 event recipes,
4computed WORD,262 global fields,13clear ops,6constant stores и7DTOrder
сопоставлены. Новых ошибок текущей записи не найдено. Корректные D2 sizes
и trap Disarmable-mask сохранены; source defects явно описаны.
Все28/28 field tables готовы в описанном контракте; это не процент готовности.
I02 остаётся открытым для конечного validator/version audit. Прежние proofs
не обнуляются и не ставятся на повторный запуск.
Product/tests/DLL/ENRU unchanged; build/publish/tests не повторялись.
Core `4b1fa6fed0d06af43a725ba38f0074f20e26e1731061fba83beafb1be95b8c5b`, App `9a1ec4cad8a0821e80021d5a3a2102464329c0c7919aa29179c157c8a6b8855f` прежние,Source/Binaries/Fixtures/game inputs сохранены.
Последние related PASS привязаны к terminal/Core;1840/63 не current green full.
I02 и исходная цель остаются открыты;8I/23M прежние. GUI/audio/game/CU не было.
Ниже история; её прежние следующий шаг и counts не заменяют эту запись.

CompilerThingVariableAudit-20261024: [сверены поля things и variables](I02_THING_VARIABLE_AUDIT.md).
15 functions:Things container+9writers,Variables container+4helpers.
155 source stores,8catalogues и все16-byte pointer recipes сопоставлены;
новых расхождений не найдено. Trap undefined stack padding определённо0,
band active prefix/нулевые slots, scopes, union и casts явно описаны.
Pools/availability heart filter, modifiers/elite skip, upper allies triangle,
nondefault globals и source order совпадают в поддержанном контракте.
Вместе с metadata/player готовы17/28 таблиц, это не процент готовности.
Остаются11:MapFile,3event writers,7global writers; validator/version routes.
Прежние ordering/date/retention proofs сохранены,не ставятся на повторение.
Product/tests/DLL/ENRU unchanged; build/publish/tests не повторялись.
Core `4b1fa6fed0d06af43a725ba38f0074f20e26e1731061fba83beafb1be95b8c5b`, App `9a1ec4cad8a0821e80021d5a3a2102464329c0c7919aa29179c157c8a6b8855f` прежние,Source/Binaries/Fixtures/game inputs сохранены.
Последние related PASS привязаны к terminal/Core;1840/63 не current green full.
I02 и исходная цель остаются открыты;8I/23M прежние. GUI/audio/game/CU не было.
Ниже история; её прежние следующий шаг и counts не заменяют эту запись.

CompilerMetadataAudit-20261024: [сверены поля metadata/player/AI](I02_METADATA_PLAYER_AUDIT.md).
Два writers:49 metadata rules/78 referenced fields,91 player rules/101 stores,
9AI profiles×84 assignments. Numeric constants/address overlaps и15 reference
types/paths сопоставлены; неклассифицированных полей нет. Текстовый CP1252→UTF16
контракт явно отделён от source OEM API. Declared count, heart-derived presence,
camera/CPU sentinels, health и project-only UseCount field разобраны.
Исправлен inventory:28 writers+SetDate; прежние24int не включали4BYTE* helpers.
Это не новые defects или новые I groups. Далее26 остальных field mappings,
начиная things/variables helpers; прежние partial proofs не обнуляются.
Product/tests/DLL/ENRU unchanged; build/publish/tests не повторялись.
Core `4b1fa6fed0d06af43a725ba38f0074f20e26e1731061fba83beafb1be95b8c5b`, App `9a1ec4cad8a0821e80021d5a3a2102464329c0c7919aa29179c157c8a6b8855f` прежние,Source/Binaries/Fixtures/game inputs сохранены.
Исторические отдельные PASS привязаны к log/terminal;1840/63 не current green full.
I02 и исходная цель остаются открыты;8I/23M прежние. GUI/audio/game/CU не было.
Ниже история; её прежние следующий шаг и counts не заменяют эту запись.

CompilerThingOrder-20261024: [исправлен порядок Things.kld](COMPILER_THING_ORDER.md).
Экспорт следует9 source writers: objects/magic/traps/doors/creatures/areas/
gates+hearts/effects/bands. DKM storage order иной; исходный документ прежний.
Внутри категории порядок сохранён; тела записи полей и unknown-region guard
сохранены буквально.4 новых группы: до4FAIL,после4PASS,targets13/16/17,
противоположные порядки вставки, сердца и реальный DKM/7-file Save/reopen.
Related retention:97whole records,1800cells/711payloads и6gate groups PASS.
Все41 build01/focus/related/publish01 ExitCode0,0warnings/errors.
Core `4b1fa6fed0d06af43a725ba38f0074f20e26e1731061fba83beafb1be95b8c5b`, App `9a1ec4cad8a0821e80021d5a3a2102464329c0c7919aa29179c157c8a6b8855f`; App/tests/published Core совпадают.
Source/Binaries/Fixtures/4485 game inputs/15WAV/2873 ENRU literals сохранены.
GUI/audio/game/full не запускались;1840/63 — исторический full,не current DLL.
I02 открыт: далее metadata/player/AI и оставшиеся field/validator/version
routes.8I/23M прежние; исходная цель не завершена. Ниже история,
её прежние hashes/next steps не заменяют эту запись.

CompilerDefaultGlobals-20261024: [исправлено сохранение базовых definitions](COMPILER_DEFAULT_GLOBALS.md).
После изменения и возврата параметров к исходным DKM array может остаться,
но source Properties_GlobalChanged=False теперь сохраняет все bytes выбранной
базы. Раньше поля базы переписывались defaults при прежней дате. Changed
семейства по-прежнему генерируются; их writer bodies и helper262 fields прежние.
Новые9 групп:7 families × targets13/16/17, selective override и реальный
7-file Save/reopen. До9FAIL, после9PASS; связанная date regression5PASS.
Первый after focus8PASS/1FAIL был ошибкой сравнения packed file/standalone
section в тесте; исправлено выделение секций, продукт дополнительно не менялся.
Все41 build01/test-build02/publish01 ExitCode0,0warnings/errors.
Core `683b1c6406609d22bdb5b93b71990d433b40d7f70ea0f88abefd79e611870ada`, App `0e8bdc358b16fb9a8ed41259b19dbda27036d62c2313b2ed7f66f6699505de4d`; tests/App/published Core совпадают.
Source/Binaries/Fixtures/4485 game inputs/15WAV/2873ENRU literals сохранены.
GUI/audio/game/full не запускались.1840/63 — исторический full,не current DLL.
I02 остаётся открытым; далее metadata/player/AI и оставшиеся writer/validator
routes.8I/23M прежние; полная исходная цель не завершена. Ниже история,
её прежние hashes/next steps не заменяют эту запись.

CompilerGameDates-20261024: [исправлены даты создаваемых native headers](COMPILER_GAME_DATES.md).
Пять новых headers получают local creation/modified date по исходнику;
семь global families — только при source Properties_GlobalChanged.
Raw native SaveCopy и скопированные default/basis dates не переписываются.
Разобраны12 source calls, GAMEDATE20 bytes/12 assigned fields и262 default
comparison fields (259 exact,3 equivalent BYTE/LONG RGB comparisons).
Focus02:5PASS/0FAIL, ранее5FAIL. Record retention97 и payload1800cells/711events
также прошли на той же DLL. Все41 build02/publish01 ExitCode0,0warnings/errors.
Core `82bc0d17101781082214716c50fd570dcd518cf9a17cf8c320bcd71cc4310335`, App `0e8bdc358b16fb9a8ed41259b19dbda27036d62c2313b2ed7f66f6699505de4d`. Другие compiler bodies сохранены буквально.
Source/Binaries/Fixtures/4485 game inputs/15WAV/2873 ENRU literals сохранены.
GUI/audio/game/full не запускались.1840/63 — исторический полный результат,
не текущий результат новой DLL; corpus не зелёный. I02 остаётся открытым:
после DESTROYGATE/GAMEDATE продолжается full field/version сверка24 writers
и validator routes. Далее metadata/player/AI literals и derived fields.
Остаток8I/23M, вся исходная цель сохранена. Нижние этапы — история,
их прежние следующий шаг/даты/hash не заменяют это текущее состояние.

CompilerGateFallback-20261024: [исправлена отсутствующая цель DESTROYGATE](COMPILER_GATE_FALLBACK.md).
Writer теперь пишет исходный fallback1,1 вместо лишнего отказа. param5 и
предупреждение остаются; known target, другие guards и игровые файлы прежние.
Native working target не захватывает чужие ворота в0,0; source companion
не возвращает старые координаты. Новые5 групп и изменённый deletion/reuse
check прошли:6PASS/0FAIL. Все24 bytes независимого recipe, версии13/16/17,
Undo/Redo, real DKM+companion/reopen и ровно7 preview files проверены.
Terrain gameplay validator fixture прошёл; это не запуск игры.
Build03 всех41 проектов, focus02 и publish01 ExitCode0, warnings/errors0.
Core `2d1b1a497dd19c8a60d10b035505475adf135f10296def3fd9267ba14be8eb04`, App `4a7bc5f078118e07bbc0e4d3c0b4717004f89b8c7a2ab9adb5704ba57ab5d62b`. App/tests используют тот же Core hash.
Source/Binaries/Fixtures/4485 игровых файлов/15 WAV/2873 ENRU literals
сохранены. GUI/audio/game/full не запускались.1840/63 остаётся историческим
полным результатом, не новым результатом этой DLL; corpus не зелёный.
I02 остаётся открытым:24 source writer functions и date helper внесены
в inventory, но это не proof всех полей. Далее GAMEDATE12 header calls:
сопоставить source stamp/current create-copy policy, затем остальные
stored/derived fields и validator/version routes. Остаток8I/23M неизменен.
Ниже датированные предыдущие этапы; их counts/следующие шаги исторические.

NativeMetadataAudit-20261024: [I01 закрыт по рабочим метаданным](I01_CODE_CLOSURE.md).
Учтены14 видов состояния и19 маршрутов. Все6 native/2 DKM virtual files
сопоставлены с объявлениями, TransactionFiles, копиями, fingerprints, Save,
companion и compiler. Physical address, unique UID и исходный first-gate
coordinate rule явно разделены; неоднозначные UID не дают owner authority.
Это аудит текущего кода с9 конкретными историческими terminal0 результатами,
не9 новых запусков. Product/tests/2873 EN/RU keys и вся папка App прежние.
Core `5b3989a3f599ddbc649cf3d92f293ffbbd908ff6c442d0901addf40007daf9af`, App `4a7bc5f078118e07bbc0e4d3c0b4717004f89b8c7a2ab9adb5704ba57ab5d62b` — опубликованный код предыдущего этапа I06.
Build/tests/publish/GUI/audio/game/full повторно не запускались.
Source/Binaries/Fixtures/4485 игровых файлов/15 WAV/corpus inputs сохранены.
1840 PASS/63 FAIL — исторический полный результат, corpus не зелёный.
Остаток **8 участков кода и23 ручные группы (31 пункт работы)**.
Далее I02: конечная field/version сверка compiler writer/validator13/16/17;
не повторять неизменённый успешный экспорт ради счётчика. Вся исходная цель
и все23 возможности сохранены. Нижние датированные этапы — история,
их прежние counts/следующие шаги не заменяют это текущее состояние.

DefinitionReferenceDiagnostics-20261024: [I06 закрыт по поддержанным ссылочным полям](I06_CODE_CLOSURE.md).
DK2N039/040 — Information о значениях вне проверенных каталогов исходного
редактора, не утверждение об отсутствии пользовательского определения игры.
Данные сохраняются, новых блокировок/repair нет. Учтены15 групп маршрутов,
27 каталогов,235 примитивных global-объявлений и20 сценарных контекстов.
Новые9 групп и существующая entity-диагностика прошли; build07 всех41 проектов,
validation build08 и publish01 завершились ExitCode0, warnings/errors0.
Core `5b3989a3f599ddbc649cf3d92f293ffbbd908ff6c442d0901addf40007daf9af`, App `4a7bc5f078118e07bbc0e4d3c0b4717004f89b8c7a2ab9adb5704ba57ab5d62b`.
2873 EN/RU keys, все2871 прежних literals сохранены. Source/Binaries/Fixtures,
4485 игровых файлов,15 WAV и corpus inputs сохранены. GUI/audio/game/full
не запускались;1840/63 — исторический полный результат, corpus не зелёный.
Остаток **9 участков кода и23 ручные группы (32 пункта работы)**.
Следующий открытый участок — I01: конечная сверка рабочих native-метаданных
и владельцев. Закрытые каталоги/identity captions повторно не открываются.
Ниже приведены датированные предыдущие этапы; их прежние counts/hashes
не заменяют этот текущий результат.

Актуальный конечный перечень: [OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md) —
8 пунктов «исправить»/завершить сверку и23 группы «проверить вручную».
I14 закрыт согласованным полным Undo регионов; I17 — завершённой кодовой сверкой
команд/Config; I12 — кодом окна/каталога; I11 — сверкой14 страниц; I13 — конечной сверкой каталогов/подписей/изображений; I16 — статистикой/lifecycle; I15 — счётчиками принятия команд; I05 — players/hearts/import policy; I06 — typed definition reference diagnostics; I01 — native working metadata audit. Приёмка M19/M21/M23 открыта. Это 31 открытый пункт работы, не 31 доказанный дефект. Все23 возможности сохранены; успешные проверки
без изменений не повторяются.

24 октября 2026. Предфинальная поставка: `artifacts/PreFinal/App`.
Текущая поставка — [CompilerGameDates-20261024](COMPILER_GAME_DATES.md).
PlayerImportPolicy-20261024: [I05 закрыт по коду](I05_CODE_CLOSURE.md).
11конечных cases связывают declared count/native IDs/7DKM slots/hearts/camera/
resources/resize/historical guards/Save/compile. PlayerSlotDefaults Information
теперь объясняет пустыеnative slots передDKM adoption;old assignment/writer
bodies не менялись. Original native files/order retained companion,не repair.
Новыеfocus6PASS;native-conversion5PASS;build01 всех41/publish01 ExitCode0,
warnings/errors0. Доfix5FAIL/1PASS после исправления fixture;логи сохранены.
Core `e10463c5d21ffc802135d95efb73f7fff549810423dfb35c9c9523e7e1c564f2`, App `2153be6e4f010ee9cc760900f73640c4f1610b081ca292410b4f6c0f61448a09`.
2871ENRU keys,все2870old literals прежние;Source/Binaries/Fixtures/4485game
inputs/15WAV/corpus inputs сохранены. GUI/audio/game/full не запускались;
full1840/63исторический. Остаток10I/23M (33пункта),все23возможности сохранены.
Следующий открытый участок I06:typed references к игровым definition catalogues;
не перепроверять уже закрытые identity links илиподписи ради счётчика.
Конечный остаток и граница между кодом и поставкой уточнены в
[OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md). Сверка списка не запускала проверки повторно.
Один основной код в `src/`, все 41 проекта собраны одним решением без предупреждений
и ошибок. Перенос из старых версий для сборки не требуется. Поставка включает
native record retention и исправление приоритета
явных ссылок переименованной KWD.
Также включена read-only диагностика отсутствующих/неоднозначных ссылок на
поддержанные объекты и повторных IDs, с общими описаниями EN/RU.

## Подтверждено

- Решение собирается из корня через `./build.ps1`; приложение опубликовано.
- Product references ведут к текущему Core и относительным ресурсам Source/Binaries.
- Рабочий DKM сохраняется с проверенным native-source ZIP при импорте native данных.
- Игровой экспорт содержит ровно семь файлов по официальной схеме.
- Пользователь открыл семифайловую карту в игре и официальном редакторе.
- Фокусные checks текущей DLL прошли:97 полных AREA/BAND/PLAYER records исходного
  level1 после нормализации root IDs, opaque fields/text tails, guards изменённых
  значений и identity, Undo/resize; четыре полных GAMEGATE,1800 cells/711 event
  payloads и восемь literal563-byte band records.
- Полные EN/RU руководства описывают документы, рабочий цикл, объекты, сценарии,
  настройки, сохранение, компиляцию, запуск и ограничения.

Исторический полный regression runner24329 завершился с кодом 1: **1811 успешных проверок и 63 отказа**.
Все 63 сообщения буквально совпадают с предыдущим CreatureJobRequest в прежнем
порядке; все 1810 прежних passes сохранены в прежнем порядке. Исходные 94 полностью
сопоставлены: 31 исправлен раньше, одно известное изменение текста XXLevel14,
новых failures/lost passes нет. Corpus НЕ зелёный; отчёт не фильтровался.

Append/Insert менеджера патрулей теперь открывают независимое EN/RU окно выбора.
Append сохраняет физический порядок всех областей, включая связанные; Insert
показывает только области без исходящей связи. Кандидат первоначально не выбран,
пауза0, Apply недоступен. Выбор загружает паузу выбранной области с clamp0–250;
slider/text синхронизированы, смена языка сохраняет выбранный ID и паузу. Cancel/
Escape/close не меняют карту, локальные списки, счётчик, историю или activity.
Apply вызывает один существующий manager Append/Insert и один Undo. Duplicate/
zero/out-of-range кандидаты показаны без предоставления edit authority.

Core group проверяет 16 независимых literal source masks ×2 операции ×2 формата
(64 случая), navigation/cancel/no-choice/equal Apply, linked Append затем ещё
один Append, Insert, полные ожидаемые buffers по packed offsets31/24 и52/56,
один counter/notification/Undo, Save/reopen/game compilation, foreign/private-
stale/raw/structural/nested/closed/capacity guards и observer reentrancy/error/
close после commit. Focus exit 0. Core GetAvailability разделяет root и point,
loop/missing/unlinked/capacity; toolbar использует эти predicates с confirmation.
Выбор маршрута теперь сбрасывает выбранную точку. Дополнительный modern Start
и обе quick drawing операции сохранены. Raw graph APIs и тела прежних manager
commands буквально прежние; изменена только декларация класса на partial.
Подробности: [выбор области](PATROL_AREA_SELECTION.md),
[источник selector](PATROL_SELECTOR_SOURCE_SCOPE.md).

Локальные списки EditorPatrolManagerSession сохраняются до Reload/close. Append
добавляет только выбранную область, её цепочка не добавляется автоматически;
следующий Append меняет последнюю показанную точку. Clear сохраняет исходящую
связь финальной точки; one-point Remove оставляет пустой root. Source grouping
15404 cases/формат, 9 hand cases, 255 IDs и packed Level10 сохранены в full.
PathsCheck теперь показывает отдельное owned EN/RU окно при выборе изменённого
маршрута и после удаления, с OK, исходной иконкой вопроса и флажком подавления.
All проверяет первый изменённый оставшийся список, включая выбранный; обычная
пауза не добавляет explicit check. Только OK сохраняет состояние флажка0x2000;
close/Escape ничего не запоминают. Подавление действует в текущем сеансе, включая
замену карты, до перезапуска или отдельного сброса на панели. Оно не сохраняется
в карте/JSON и не меняет Undo/activity/counters. Source startup обнуляет Config;
отдельная source persistence не доказана. Source remove confirmation0x1000 и
modern copy-buffer setting отдельные; removal теперь имеет своё owned окно.

Core проверен80 native/project flag/reply/kind cases, shared selection/all,
loop/missing predicates, reset/restart/map replacement и private/raw/revision/
structural/foreign/closed guards. Read-only reply сравнивает все bytes/private
lists/history/dirty/activity/counters/notifications. Шесть literal work-area
cases проверяют геометрию; App использует nearest-monitor device pixels вместо
source primary-screen/taskbar, physical DPI не доказан. Исходная questionICO
встроена побайтно; старые manager/selector/grouping/raw APIs не менялись.
На предыдущем warning этапе первый focusexit1: fixture выбирала связанную cursor area. Исправлены данные
fixture, product guard сохранён; final focusexit0, оба лога сохранены.
Подробности: [предупреждения патрулей](PATROL_WARNING_REQUESTS.md).

Remove/Clear теперь открывают owned EN/RU Yes/No request вместо флажка панели.
Source lastbutton No→0, Yes→1; checked checkbox1200 отключает No, close0 не
запоминает флажок. Только Yes меняет runtimebit0x1000 перед одной manager
операцией. Suppressed defaultYes вызывает тот же guarded command без окна и
не переписываетflags. Reset/перезапуск сбрасывают лишь removal; другая карта/
Reload сохраняют выбор в сеансе. Opening/No/close/reset не меняют карту/history/
activity/counters/privateLists; Yes owns oneUndo/count даже при no-byte root.
Старая availability root/point действует без checkboxgating. Source finalchain
и emptyroot effects прежние. После удаления проверяется existing allwarning.

Новая группа проверила160 native/project flag/reply/kind/remember/autodefault
cases, whole bytes/opaque fields по independent offsets31/52, exact Undo/Redo,
session/reset/shared warnings, private/raw/revision/structure/reload/closed/
foreign/nested guards и Save/reopen/game compilation. Observer exception/close
после commit сохраняетaccepted edits/consumed authority; App сообщает об ошибке
уведомления только для прежнегоmanager/document. Focus/finalfocus0; финальная
сборка включает усиленный ownerguard, added после initialbuild. Старые Core
manager/selector/warning/raw bodies не менялись. Physical новое окно не запускалось.
Legacy CheckPatrolRoutes WPF обновлён для текущих owned manager/window paths;
его сборка подтверждена, исполнение ещё не проводилось. Старые inline/raw
ожидания удалены; подготовленный код не доказывает текущие modal workflows. Подробности: [удаление патруля](PATROL_REMOVAL_REQUESTS.md).

Все 41 проекта собраны без warnings/errors, приложение опубликовано. Девять
archive/native/official/retention/activity model checks завершились exit 0 на
той же Core DLL. Seven-file test bundle bytes прежние. В EN/RU по 2533 ключей;
все прежние 2533 значения буквально сохранены; этот этап не добавлял переводов.
App wiring/language/lifecycle проверены статикой и сборкой. Новые selector/warning/removal/pause окна не
запускались и не рендерились агентом; physical EN/RU/DPI/modal UI ещё не доказан.

Изменение паузы теперь открывает owned EN/RU OK/Cancel числовой запрос с private
literal draft. Начальное signedLONG значение не clamp; typed ASCII digits,
paste разрешён, decimal prefix/failed conversion0, range0–250 проверяется на OK.
Min/max — readonly fields, без slider/spinner; range child warning оставляет
родительское окно открытым для исправления и возвращает focus/select-all.
Cancel/close/range error не пишут карту; OK вызывает один прежний manager SetPause
даже для equal value. Original question/warning ICO встроены побайтно; outer
NOBEEP и range exclamation sound подключены, physical звук не подтверждён.
Readonly PatrolAreas getter исправлен: отрицательный project Int32 wait больше
не вызывает UInt32 OverflowException до показа маршрутов. Raw mutation bodies
сохранены. Core проверяет26 numeric cases×2formats, packed offsets24/56,
oneUndo/count, lifetime/observer guards и Save/reopen/compile. Initial build1
CS0819 и focus1 OverflowException сохранены, final build/focus0; full62118 был
прерван без итогового отчёта, partial log сохранён, новый92175 terminal1.
Подробности: [числовая пауза](PATROL_PAUSE_REQUEST.md).

Modern pause/no-byte Undo остаётся документированным отличием source history.
Private region generation, parent Cancel/one Apply/topology replay и point pick
сохранены. Original GUI, hardware/DPI/audio/system dialogs, полные historical
formats/catalogues/scenarios/aliases и actual game workflow не закрыты этим
checkpoint; все FEATURE_PARITY capabilities остаются в scope, цель ACTIVE.
Агент редактор и игру не запускал. Ручная проверка — MANUAL_ACCEPTANCE_RU.

Предыдущий этап resolver дал1795 passes/63 failures:
Исправление общего resolver устранило загрузку соседнего резервного компонента,
когда KWD уже содержит найденную явную ссылку того же типа. Например,
XXLevel10 больше не объединяет Level10Triggers с XXLevel10Triggers.
Проверки не ослаблялись, установленные карты не ремонтировались.

Весь прежний упорядоченный список 94 отказов сопоставлен с новым:
31 проверка исправлена (28 новых именованных passes и 3 исчезнувшие структурные
диагностики; успешная структурная инспекция не добавляется runner в Passed).
Все 1767 прежних passes сохранены. Новых неуспешных проверок нет, но у одного
прежнего отказа XXLevel14 изменился текст: вместо смешанных duplicate IDs теперь
диагностируется отсутствующий wMainTrigger=52 в явно указанном Level14Triggers.
Это отдельно подтверждено чтением обоих исходных файлов, без их изменения.
Оставшиеся отказавшие проверки сохраняют прежний порядок; остальные 62 сообщения
совпадают буквально. Полный corpus **НЕ зелёный**.

Оставшиеся 63 отказа: 27 из-за отсутствующего файла клеток, 24 из-за усечённых
Variables.kld, 8 из-за нулевых/повторных IDs, 4 из-за отсутствующих событий.
Числа включают повторные операции и копии карт и не обозначают 63 отдельных бага.
Прежние 4 ошибки устаревшего preview _Global.kwd исчезли ещё 11 октября после
пользовательской замены комплекта; это отдельное изменение входов, а не эта правка.

Current Core SHA256: `7f36b40d9ecb3184bb85ea75927797e7ff6b6beb42f83868f4c72607a1f97f11`.
DLL полного corpus, свежих focused checks и опубликованной поставки совпадает.
Вновь прошли OfficialMapBundleChecks, NativeRecordRetentionChecks,
NativeGateRetentionChecks и NativePayloadRetentionChecks, каждый с exit 0.
1006 Source/Binaries и все 4453 установленных map files совпадают с manifest
11 октября. Агент не устанавливал и не изменял постоянный preview-слот.
Доказательства текущей правки: `artifacts/Evidence/CreatureJobCallbacks-20261012`;
предыдущий job request: `artifacts/Evidence/CreatureJobRequest-20261012`;
предыдущая пауза: `artifacts/Evidence/PatrolPause-20261012`;
предыдущее удаление: `artifacts/Evidence/PatrolRemoval-20261012`;
предыдущий warning: `artifacts/Evidence/PatrolWarning-20261012`;
предыдущий selector: `artifacts/Evidence/PatrolSelection-20261012`;
предыдущие local lists: `artifacts/Evidence/PatrolManager-20261012`;
предыдущая первоначальная группировка: `artifacts/Evidence/PatrolGrouping-20261012`;
предыдущий patrol caller: `artifacts/Evidence/PatrolCallers-20261012`;
предыдущий private region generator: `artifacts/Evidence/RegionGeneration-20261012`;
предыдущий gate counter: `artifacts/Evidence/GateGenerationCounter-20261012`;
предыдущий region geometry pick: `artifacts/Evidence/RegionGeometryPick-20261012`;
предыдущие region properties: `artifacts/Evidence/RegionProperties-20261012`;
предыдущий object order: `artifacts/Evidence/SelectionOrder-20261012`;
предыдущие terrain bytes: `artifacts/Evidence/SelectionTerrain-20261012`;
предыдущие historical records: `artifacts/Evidence/HistoricalSelection-20261012`;
предыдущие entity warnings: `artifacts/Evidence/EntityDiagnostics-20261012`;
предыдущий resolver: `artifacts/Evidence/ReferenceResolution-20261012`;
предыдущий предфинальный этап: `artifacts/Evidence/PreFinal-20261011`.

Дополнительный source audit: AREA/DKZ — исходный selection/буфер/вставка workflow,
без отдельного полного map-load route и без собственных new-map/player defaults.
Современная AREA→DKM conversion — дополнительная операция. Текущие31 selection
archive checks (17 import/14 write), NativeProjectConversionChecks и activity
core/document checks завершились с exit0 на той же Core DLL; editor/game не
запускались. Доказательства: `artifacts/Evidence/HistoricalWorkflowAudit-20261012`;
границы — [AREA scope](AREA_WORKFLOW_SOURCE_SCOPE.md) и текущие activity documents.
Полное AREA direct-export/historical/physical соответствие ещё не доказано.

## Ещё не доказано

Полное соответствие всем функциям оригинального GUI; все игровые сценарии после
редактирования; аппаратные конфигурации/DPI; аудио и системные диалоги; все варианты
native/исторических данных и оставшиеся требования FEATURE_PARITY.
Открытие одной семифайловой карты не доказывает эти пункты.
Последняя сборка приложения отдельно не проверена пользователем через GUI/игру.
Для проверки общей отмены, одного Undo, сохранения/повторного открытия и EN/RU
подготовлен [ручной список](MANUAL_ACCEPTANCE_RU.md). Ответ пользователя пока
не получен; агент редактор и игру не запускал.
Повторное физическое открытие текущей поставки — отдельное подтверждение.
Числовой процент полной готовности из имеющихся тестов достоверно не выводится.

## Где работать

README — запуск и сборка; USER_GUIDE_RU/EN — руководство; ARCHITECTURE — устройство;
FEATURE_PARITY — полный ledger; CONTINUE_AFTER_CODEX_RESTART — текущие правила
продолжения. INDEX группирует более подробные технические документы.
Исторические эксперименты архивированы отдельно, в сборке не участвуют.
Полная цель остаётся ACTIVE.

## Очистка и воспроизводимость

Удалён прежний Test с328 каталогами экспериментов и промежуточными поставками;
168874 файла сохранены в архив с проверкой каждого содержимого поSHA256.
Объём прежних данных13.35GB, проверенного архива4.72GB. Удалены82 bin/obj,
затем всё решение заново собрано из текущего кода без ошибок и предупреждений.
DLL чистой сборки точно совпала с DLL полного corpus и focused tests.
Сборка не обращается к архиву и не требует восстановления прежних исходников.
После этого проверены1006 Source/Binaries и текущие4453 installed-map files:
они не менялись при очистке; исходная DKM/ZIP-пара сохранена в tests/Fixtures.

Готовая поставка: artifacts/PreFinal. Единый исходный проект:
artifacts/DK2-Map-Editor-Source.zip. Архив истории не включён в исходный проект.
Обычная сборка: ./build.ps1. Полные доказательства и отчёты:
artifacts/Evidence/PreFinal-20261011, artifacts/Evidence/ReferenceResolution-20261012
artifacts/Evidence/EntityDiagnostics-20261012 и artifacts/Evidence/HistoricalSelection-20261012.

Инвентаризация nonpainting callers:89 исходных MapChanges increment sites,
83 вне двух painting файлов, с функциями/строками и hashes. Это статический
список для дальнейшего аудита active branches, equal/Cancel и compound history,
не доказательство полного counter parity.

Region property caller/window теперь реализован с private area/effect children;
интерактивный source ChoosePoint реализован, но его physical
mouse/focus/Hide/Show проверка открыта, как и physical nested gate-generation и
window/colour/DPI/game acceptance. Source audit и implementation scope
не заменяют эти acceptance gates.

## Предыдущий этап проверочного кода: PatrolHarness-20261012

Устаревший CheckPatrolRoutes заменён на проверки текущих local occurrence lists,
реальных команд и owned selector/pause/warning/removal окон. Добавлен отдельный
format/language entry point и dispatcher обработка вложенной range warning.
Подготовлены Cancel/equal/one-command, shared routes, suppression independence,
map-owner-close/stale rejection, quick routes и buffer preservation assertions.
Все41 проекта снова собраны0warnings/errors; App/Core и все1306product/source/
binary inputs прежние, installed4453 unchanged. Старый body сохранён только как
evidence, не build dependency. Физические окна/рендеры/звук НЕ запускались;
новый harness ещё не имеет execution proof. Fullcorpus не повторялся: последний
реальный результат выше относится к PatrolPause на той же DLL. Архив единого
проекта включает новый проверочный код и обновлённую документацию. Подробности:
[границы подготовленного UI-прогона](PATROL_UI_HARNESS_SCOPE.md).
Доказательства: artifacts/Evidence/PatrolHarness-20261012. Цель ACTIVE.

## Предыдущий этап: CreatureJobRequest-20261012

Подтверждено наличие текущих общих окон: пять element pages и девять creature
pages уже реализованы в единомsrc. Source job probability request теперь owned
Continue/Cancel EN/RU вместо обычного Yes/No MessageBox. DefaultContinue1,
checkbox отключаетCancel, close/Escape0 не запоминают; толькоContinue меняет
runtimebit0x10000. Auto reply сохраняетflags и не открывает окно. Workspace
сохраняетpolicy при reopen/map replacement; reset/restart возвращают вопросы.
Source signedLONG totals/first invalid group modeled; consent readonly и
owner/context/input/plan guards. Other raw job/global APIs буквально прежние.
Исходная Warning32 ICO встроена; source exclamation beep подключён, физический
звук не доказан. Broad changed-row review был сохранён на этом прежнем этапе;
его замена source retained Jobs buffer описана в текущем этапе ниже.

Core96 flag/reply/remember/formats cases и11 signed total projections passed.
Первыйfocus1: fixture пыталась удалить empty slots; corrected occupied selection,
productguard прежний. Finalfocus0; все41build/finalbuild/harnessbuild0warnings/
errors. Ninefreshmodel checks0 и publish0 на текущейCore. Новый UI test есть в
focused и full WindowsSmoke, только compiled, не executed. Ни приложение, ни
WPF/originalC/игра/звук/рендер не запускались. Source1006/installed4453/fixturepair/
семифайловыйTestMap прежние. CoreSHA и полный1810/63 comparison выше; initial94
полностью сохранены и сопоставлены. Подробности:
[запрос занятий](CREATURE_JOBS_WARNING_REQUEST.md). Full goal ACTIVE.

## Изменение продукта: CreatureJobCallbacks-20261012

Исправлен source mismatch общегоApply: вместо первого changed-row JobWarnings
используется отдельный буфер последней открытой страницыJobs. Независимый пример
row0=37%, row1=73%, переходнаAttributes row2 теперь запрашивает73%, какcached
HappyJobs источник, вместо37%первойизменённойстроки. Capture наJobsOpen и перед
JobsSave; буфер сохраняется приotherpage/row иcommonApply, заменяется приновом
JobsOpen/reset, очищаетсяприclose. Егоrevision дополнительноguards consent.
ActiveJobs KillActive проверяетсяпередSave, JobsApply проверяетcached buffer;
двеchecksбезremember, autoContinueпослеremember, inactive/visited/noop сохраняют
sourcecheck. NevervisitedJobs неинвентируетrequest. GenericJob/session/warning
APIs прежние, Coreplanchanged-row warnings остаютсядляотдельногоdirectAPI.

Source+primaryMicrosoft notification contracts дают этуmodel trace; original
nativeGUI никогда неисполнялся, actualnative timing/focus недоказаны. Современный
parent остаётсяatomic oneUndo/count; возможныйpartial sourcecommit предыдущего
Applypage передJobsCancel намереннонеповторяется и невыдаётся заhistoryparity.
FocusedCore0 native/project, полный1811/63 exactpriororder выше, original94full.
Все41 build/finalbuild0warnings/errors, ninefreshmodels0/publish0. Newcallback
UI harness в full/focused compiledONLY; no app/WPF/render/audio/originalC/game.
Source1006/installed4453/fixturepair/TestMap7bytes unchanged. ENRU2533literal.
Unified delivery updated; подробности [callback scope](CREATURE_JOBS_CALLBACK_SCOPE.md).
PhysicalGUI/audio/dialog/DPI/editedmap/game/fullFEATURE_PARITY остаются ACTIVE.

## Сверка автоматического остатка, 13 октября 2026

Выполнена независимая сверка метаданных: 14 страниц/262 поля и native layouts,
73 проектных числовых формата, 22 упорядоченные привязки каталогов, страницы и
native параметры всех 103 типов сценариев, 127 семантических контролов.
Различий в этом объёме нет; все исходные страницы сценариев поддержаны.
Это уточняет покрытие, но не закрывает все зависимые списки, составные обработчики,
ввод, переводы, исторические версии или GUI/game acceptance.

Продукт не изменён; Validation дополнен командой read-only выгрузки, документация
обновлена. Сборка Validation/экспорт/окончательная сверка: exit0. Последний полный
корпус остаётся 1811/63, запуск24329 терминальный и не повторялся ради метаданных.
Окна, звук и игра не запускались. Предварительный срок 1–2 рабочих дня до ручной
части зависит от завершения инвентаризации. Подробности и следующие блоки:
[AUTOMATIC_CLOSURE_INVENTORY](AUTOMATIC_CLOSURE_INVENTORY.md).

## Текущая сборка: CreatureAttractions-20261013

Исправлена реальная зависимость полей привлечения существ: смена второго типа
восстанавливает количество из сохранённой приватной строки вместо переноса ещё
не применённого ввода предыдущей формы. Отключённые исходные списки/число пусты;
отсутствующий ID показывает первую строку нового каталога, сохраняя исходный ID
до явного выбора/исходной очистки. Неизвестные импортированные типы сохраняют
прежнее отображение. Отмена и общий атомарный Apply не изменены.

Все41 проекта Release: exit0, без предупреждений/ошибок. Focused native/project
callbacks: exit0, включены в прежнюю группу полного корпуса. Девять отдельных
проверок записи/преобразования/статистики/семифайловой карты: exit0; publish0.
Полный запуск15642 терминальный exit1: **1811 PASS / 63 отказа**. Новых отказов
или потерянных PASS нет. Сравнён весь порядок относительно предыдущего запуска
и исходных94. Входной путь был передан со слешами, поэтому1743 строки отчёта
отличаются только написанием этого проверенного общего префикса. Verifier
нормализует только этот префикс; все raw отчёты сохранены. Это не raw string
equality и не зелёный корпус:63 отказа остаются открытыми.

Core SHA256: `6b6e34c35ff07e1a1cda3cce05e597b0c13274207f4a0286ebf23539ecefd93c`.
Новое реальное UI поведение проверено моделью/исходником; дополненный
CreaturePageSmoke собран, но не запускался. Оригинальный GUI, WPF, рендер,
звук и игра не исполнялись. Сохранены Source/Binaries, установленные карты и
комплект DK2Preview_6a88240eaf814f78. Подробности и ручные шаги:
[CREATURE_ATTRACTION_CALLBACKS](CREATURE_ATTRACTION_CALLBACKS.md).
Доказательства: `artifacts/Evidence/CreatureAttractions-20261013`.
Остальная контекстная/составная сверка и все23 строки FEATURE_PARITY открыты
в прежнем объёме; цель ACTIVE, переход к одной ручной части ещё не объявлен.

## Текущая сборка: GlobalDefinitionImages-20261013

Восстановлены оригинальные изображения 184 строк семи глобальных каталогов:
общая вкладка, общий пятистраничный parent элементов и девятистраничный parent
существ. В трёх окнах параметров выстрелов формы 2/3 показывают 45 существ,
форма4 — 116 объектов; уровни остаются числами, неизвестные ID — текстом.
Core сохраняет исходный третий элемент ELEMENT (номер ресурса), не смешивая его
с ID игры или индексом. Добавлены 43 BMP-ресурса; четыре исходных номера-алиаса
переиспользуют проверенные по RC общие файлы. Никакие BMP не редактировались.

Сверка 184+116 строк проверила порядок/ID/символ/номер ресурса, исходные BMP
SHA256/размеры и все соответствующие байты в managed PE собранной App DLL.
Сборка всех41 проектов, focused definition checks, source audit, официальный
семифайловый export/reopen/resave и publish завершились exit0. Сборка без
предупреждений/ошибок. Начальный build exit1 (NETSDK1022, повторный Include
одного BMP для разных RC-номеров) сохранён; исправлен общим ресурсом, не удалением
исходных номеров. Девять прежних standalone checks в этом блоке не повторялись:
правка относится к отображению/метаданным изображений; полный корпус и свежая
проверка официального экспорта выполнены на текущей Core DLL.

Полный запуск55746 терминальный exit1: **1811 PASS / 63 отказа**. Проверены
полные упорядоченные списки: относительно предыдущего Attraction запуска
нормализовано только его написание общего входного пути; относительно более
раннего canonical JobCallbacks запуска оба списка совпали буквально. Исходные
94 отказа сопоставлены полностью:31 исправлен ранее, одно прежнее изменение
описания XXLevel14; новых отказов/потерянных PASS нет. Корпус не зелёный.

Core SHA256: `1add4eb898cccd81c842ae8d04e85d96ca40f374b6e9f7c7f431ce40bf77d715`.
Доказательства: `artifacts/Evidence/GlobalDefinitionImages-20261013`.
Сохраняются Source/Binaries, все установленные карты, исходные fixtures и
семифайловый TestMap. Свежий экспорт побайтно совпадает с прежним TestMap.
В EN/RU прежние2533 ключа и значения; опубликованный App соответствует текущим
Core/App DLL, а source/runtime архивы содержат текущие код и документацию.

Окна/App/WPF/рендер/звук/игра не запускались. Качество масштабирования и выделения
списков, DPI и фактическая навигация EN/RU остаются ручными. Инструкция:
[GLOBAL_DEFINITION_IMAGES](GLOBAL_DEFINITION_IMAGES.md). Остальные контекстные
обработчики, сценарии/псевдонимы, исторические и физические ворота FEATURE_PARITY
сохраняются; вся цель остаётся ACTIVE.

## Текущая сборка: ScenarioTerrainOwners-20261013

Закрыты два конкретных расхождения зависимых полей сценариев. На страницах
«Содержимое области» и «Изменить клетку карты» выбор территории задаёт и
блокирует исходного владельца: типы1–7/31 — Нейтральный2, тип35 — Герои1.
Остальные известные типы разрешают выбор владельца. При открытии импортированные
значения сохраняются; callback применяется после явного выбора территории.
Core готовит обе величины в приватном черновике, Apply даёт один Undo.

У ACTION_SETVARIABLE скрытый вторичный список после открытия числового режима
начинается с0, а не с числового операнда. Число42 сохраняется при переключениях;
режим переменной сначала выбирает0, явно выбранный ID затем сохраняется внутри
окна. Импортированные variable IDs/неизвестные режимы и точный no-op сохранены.
Исходные callback/check обработчики и границы начальной инициализации описаны
в [SCENARIO_DEPENDENT_INPUTS](SCENARIO_DEPENDENT_INPUTS.md).

Независимый source capture проверяет пять исходных файлов,29/27 территорий,
индексы владельцев и все шесть режимов вторичного списка. Адресные Core checks:
896 terrain-owner случаев и84 secondary-default случая, полные буферы обоих
форматов, private/Cancel/no-op/unknown/lifetime guards, один Undo/Redo и счётчик.
Scenario named/alias/spatial checks, source audit, fresh official seven-file
export/reopen/resave и publish завершились exit0. Все41 проекта собраны с0
предупреждений/ошибок (финальный build94353). Обновлённые WPF checks только
собраны: никакие окна, App, renderer или игра не исполнялись.

Один полный прогон после всего блока,7211, terminal exit1: **1811 PASS / 63
отказа**. Входной путь и оба полных упорядоченных списка буквально совпали с
предыдущим GlobalDefinitionImages запуском; новые случаи исполняются внутри
существующих групп. Исходные94 также сопоставлены полностью:31 устранён ранее,
одно прежнее изменение сообщения XXLevel14. Новых отказов/потерянных PASS нет;
корпус остаётся не зелёным, установленные карты не ремонтировались.

Core SHA256: `1bba6a20dd1d49a0535037374be441e1c3401547496efbedd9f6597808bd1cc8`.
Доказательства: `artifacts/Evidence/ScenarioTerrainOwners-20261013`.
Поставка и source/runtime архивы обновляются из единого текущего src/41-project
решения; Source/Binaries, fixtures, установленные4453 файла,2533 EN/RU ресурса
и прежний семифайловый TestMap проверены при упаковке. Свежий официальный
compiler output побайтно совпадает с ним. Архивные версии для сборки не нужны.

Физическое поведение уведомлений при первом открытии оригинального окна,
навигация/мышь/EN/RU и изменённые сценарии в игре остаются ручными. Остальная
контекстная/составная/caller/историческая сверка и все23 строки FEATURE_PARITY
сохраняются; цель ACTIVE. Этот блок не объявляет готовность «только ручные».

## Текущая сборка: ScenarioPageHints-20261013

На трёх условиях гибели/заключения/потери сознания добавлена исходная подпись
численности выбранного отряда. Она обновляется при выборе, Reset и смене EN/RU;
при инвалидировании окна очищается. Подсчёт до первого неактивного слота,
отряд0 и native species/behavior sentinels соответствуют источнику. Неизвестные,
неоднозначные или непроверенные данные не получают выдуманного числа. Чтение
не меняет параметры условия, bytes, историю, activity или editor counter.

Восстановлены исходные ограничения обычного ввода псевдонимных страниц:
числовой операнд переменной —4 символа, часы/минуты/секунды сравнения таймера —2.
Программная загрузка старых импортированных значений их не обрезает. Сохранение
карт, компилятор и прежние alias/history APIs этим блоком не менялись.

Все41 проекта собраны с0 ошибок/предупреждений, финальный build28080 terminal0;
первая сборка54930 также terminal0. Source audit семи файлов, named/terrain/
band hints и alias focus, publish и последняя Validation сборка exit0.
Проверены42 маски префикса/страницы/формата, обе native stop-причины, проектный
end marker, unknown/duplicate/stale/foreign/nested guards и read-only snapshots.
Прежние896 territory-owner и84 alias-default случая также прошли на текущей
Core DLL. WPF проверка показа и перевода подписи подготовлена и собрана,
но не исполнялась; обычный набор/вставка текста остаются физической проверкой.

Первый focus exit1 выявил ошибку доступа к17-му end marker как к обычному
редактируемому полю; исправлен прямым чтением проверенного layout. Следующий
focus exit1 был дефектом теста: duplicate ID пытались создать через API,
правильно запрещающее дубликаты. Fixture теперь задаёт malformed import
непосредственно в своих private bytes. Исходные журналы сохранены, guard
normal editing не ослаблен. Окончательный focus exit0.

Core SHA256: `216d052ae06470a077af5ed50834bd470f4fae612cb8008f4fc8e38f022981d6`.
Доказательства: `artifacts/Evidence/ScenarioPageHints-20261013`.
Новый полный corpus и свежий официальный compiler round-trip в этом блоке
не запускались: изменён только read-only getter, подпись и ограничения UI;
писатели карт/компилятор прежние. Последний полный7211 относится к предыдущей
Core `1bba6a20dd1d49a0535037374be441e1c3401547496efbedd9f6597808bd1cc8`:
1811PASS/63 отказа с полным ordered сравнением и исходными94. Это прежний
результат, а не новый полный результат текущей DLL. Новый общий прогон нужен
после следующего существенного блока записи/компиляции или перед общей приёмкой.

Единые source/runtime архивы и поставка обновляются из текущего проекта.
При упаковке проверяются Source/Binaries1006, installed maps4453, fixtures
и прежний TestMap ровно7 файлов. Прежние2533 EN/RU значения сохраняются;
добавлены только две строки численности на язык, всего2535 одинаковых ключей.
Редактор, renderer, WPF harness, звук и игра не запускались.

Следующий установленный пробел — AREA-площадь/периметр на трёх условиях;
нужно передать физический selected owner через страницу/мастер и shared
graph, без произвольного выбора первой зоны. Подробности и ручные шаги:
[SCENARIO_PAGE_HINTS](SCENARIO_PAGE_HINTS.md). Caller/историческая сверка и все
физические ворота FEATURE_PARITY остаются; общая цель ACTIVE.

## Текущая сборка: ScenarioAreaHints-20261013

Восстановлена площадь зоны на TRIGGER_CLAIMAREA/AREACONTENT/SELECTAREA и
исходный периметр на CLAIMAREA. Обратные inclusive endpoints нормализуются,
площадь не ограничивается размером карты; ширина1 даёт исходный периметр1.
Информация относится к физическому AREA-владельцу сценария, не его operand.

Core разрешает реальные достижимые AREA root-связи и переводит владельцев
в приватный документ мастера через session.Files. Insertion получает контекст
до записи root, editing сохраняет всех владельцев общей ветки. Выбранный в
дереве экземпляр несёт root через вложенные AND/sibling связи; refresh/Finish
учитывают корневой identity. При нескольких зонах без выбранного экземпляра
информационный список предлагает явный выбор. Этот выбор не перепривязывает
событие и не меняет Undo/bytes/counters. Back/Next, EN/RU и явное отсутствие
выбора сохраняются. Stale информация очищается, перевод не вызывает старый
provider; неподтверждённые geometry/owner не получают выдуманных величин.

Source audit двух файлов exit0, named focus30 AREA+42band+896terrain cases0,
creation/existing wizard models всех103 типов ×2 формата0. Сборки Validation/
WindowsSmoke и всех41 проектов0/0W/E, final build43044; первая99803 также0.
Первоначальный focus1 обнаружил stale offset fixture после добавления project
scope records: событие теперь заново разрешается по сохранённому ID; guard
текущего record не ослаблен. Окончательный focus0. Publish0, текущие Core/App
совпадают с проверенными build binaries. WPF shared/nested owner/Back/Next/
ENRU/Cancel/stale smoke подготовлен и собран, но не исполнялся.

Core SHA256: `be0174f8814dcb888db31ffc35f4bb953b3262e670cbc73e39b3763dcf49c4be`.
Доказательства: `artifacts/Evidence/ScenarioAreaHints-20261013`.
Новый полный corpus/official round-trip для read-only информации и App
selection не запускался. Последний full7211 относится к предыдущей Core
`1bba6a20dd1d49a0535037374be441e1c3401547496efbedd9f6597808bd1cc8`:
1811PASS/63 отказа с полным ordered сравнением и исходными94. Это не новый
полный результат текущей DLL. Запись/компиляция карт прежние; существующие
wizard models проверили их взаимодействие с текущим private контекстом.

Source/runtime поставка обновляется из единого проекта, без restoration chains.
Проверяются Source/Binaries1006, installed4453, fixtures и прежний TestMap7.
Прежние2535 EN/RU значения сохраняются; добавлены5 AREA строк на язык,
всего2540 одинаковых ключей. Редактор, игра и WPF harness не запускались.
Ручные шаги: [SCENARIO_AREA_HINTS](SCENARIO_AREA_HINTS.md).

Следующий блок — завершение source callback inventory страниц сценариев и
оставшиеся caller/рисование/selection/исторические маршруты, а не повторение
уже выполненных прогонов. Все23 строки FEATURE_PARITY и физические ворота
original GUI/audio/system dialogs/hardware/DPI/game остаются; цель ACTIVE.

## Текущая сборка: ScenarioPrerequisites-20261013

В common ScenarioControlPages добавлено пропущенное требование Band для
TRIGGER_BANDCREATED/DEATHSINBAND/JAILEDINBAND/STUNNEDINBAND. Без отрядов
Apply блокируется и показывает существующее пояснение EN/RU; private создание/
редактирование проходят через тот же guard. Отряд0 считается настоящим.
Generic raw/сохранение импортов/ID guards остаются прежними. Исходное
условие коллекции сверено для обеих ParamsCheck функций, включая порядок.

Capture охватывает37 condition и33 action top-level parameter-check cases;
12 страниц имеют explicit collection requirement (4conditions+8actions).
Первоначальное предположение13 исправлено до fixture, failed source log
сохранён. Модель проверяет103 типа ×16 наборов объектов ×2 формата:3296
сравнений, read-only snapshots/history/counters, пустой/явный Apply четырёх
условий и private creation/editing guards. Named/area30/band42/terrain896
и прежние creation/existing103type suites exit0. WPF prerequisite smoke
подготовлен ENRU/native/project, только compiled. Все41 проекта собраны
с0 предупреждений/ошибок, build23226 terminal0; publish/official checks0.

Один full2974 после блока завершён terminal exit1: **1811PASS /63 отказа**.
Входной путь и оба полных упорядоченных списка буквально совпали с последним
ScenarioTerrainOwners full; исправления band/AREA hints и prerequisite models
реально исполняются внутри прежних групп. Исходные94 также полностью
сопоставлены:31 устранён раньше, одно прежнее XXLevel14 сообщение; новых
отказов/потерянных PASS нет. Корпус остаётся не зелёным. Полный отчёт не
фильтровался, installed карты не ремонтировались.

Core SHA256: `6a04e8a627b11e1d3b4f99320f2911b4d5222778098e1f401ca8d104edb6bf7c`.
Доказательства: `artifacts/Evidence/ScenarioPrerequisites-20261013`.
Fresh official DKM-to-native/export/reopen/resave даёт ровно7 файлов;
они побайтно совпадают с прежним TestMap. Checked Validation/Official и
publishedCore/App DLLs совпадают с текущими сборками. Единые source/runtime
архивы обновляются из текущего canonical41graph/src/docs без recovery chains.
Проверяются Source/Binaries1006/maps4453/fixtures/TestMap7,все2540ENRU
ключа и их прежние значения. Редактор, WPF harness, звук и игра не запускались.

Прочитаны конкретные notification switch parameter страниц: alias operand
switches/selection, duration callbacks, band/terrain labels, spawn flags/points/
areas, rectangle/camera library, release flags и speech. Маршруты/границы
сопоставления приведены в [SCENARIO_PREREQUISITES](SCENARIO_PREREQUISITES.md).
Это не физический callback прогон и не закрытие всякого GUI behavior.
Далее — оставшиеся brush/selection/nonpainting callers и исторические workflows;
все23 строки FEATURE_PARITY и physical originalGUI/audio/systemdialog/hardware/
DPI/game gates остаются. Цель ACTIVE, «только ручные» ещё не объявлено.

## Сверка карандаша: LivePenSource-20261013

Сверены действующие Mouse_LeftButtonDown/Mouse_Moved/Mouse_LeftButtonUp,
Mouse_UpdateCursor, Map_PaintElement/temporary mask/CanPaint и Undo route
по пяти original source files. Обычный cursor1×1, catalog-sized special
footprints, initial0,0sentinel, immediate firstLINE, stationary/noextra-up,
accepted segment counters и source Undo change restoration сопоставлены.
Source catalogue содержит39 selectable rows и8special footprints. Независимый
fixture и Core проверяют39×2формата=78размеров/free-geometric routes с exact
unchangedbytes/history/counters при reservation/refusal. Existing live-pen CLI
проверяет actual frames, counters, oneUndo/Redo, limits/protection/origin,
history0 и failed/reentrant callbacks. Source audit/targetbuild/focus exit0.

Код продукта и publishedApp не менялись; только CLI source-backed test и docs.
Core SHA256 прежний: `6a04e8a627b11e1d3b4f99320f2911b4d5222778098e1f401ca8d104edb6bf7c`.
Последний полный ScenarioPrerequisites2974 уже terminal1:1811PASS/63sameordered
failures и полное94сопоставление; эта DLL совпадает с текущей. Полный корпус/
official/compiler/publish/all41build не повторялись. Последний all41build23226
сохраняется; изменённый standalone CLI project и Core reference собраны отдельно
exit0/0W/E. Это source/model доказательство, не actual App/WPF/input исполнение.

EDITOR_DRAWING_HISTORY полностью заменён актуальным описанием canonical src,
cursor/liveStroke, counters/history и ручными шагами. Его прежние33590bytes
сохранены дословно с hash в Archive/historical-documents-20261014/
EDITOR_DRAWING_HISTORY-before-pen-source-audit.md. Исторические Test кандидаты
и Running hints не являются текущими командами или build dependencies.
Details/расположение/хеши: artifacts/Evidence/LivePenSource-20261013.

Unified packages обновляются из текущего единого source tree; SourceBinaries1006/
maps4453/fixtures/TestMap7 и2540ENRU literals проверяются без изменений.
Source pen-size/live-stroke audit снят с remaining как конкретный source блок;
physical original/modern capture/timing/mouse/audio/DPI/game ещё нужны. Остальные
selection/nonpaintingcaller/history/workflows и все23rows/fullgates остаются;
цельACTIVE, заявления «только ручные» нет.

## Текущая сборка: DirectSelectionExport-20261014

Разделены direct selection и buffer export. Source Areas_WriteThings(FALSE)
пишет всех creatures и первого noncreature каждой категории клетки; Copy/
FromCopyMemory сохраняет всех. App ExportSelection теперь вызывает отдельный
writer. DKCP6 direct mode удерживает полный raw/graph snapshot, тогда как
совместимый KTGS применяет original row/category/first-member rule. Buffer
writer остаётсяDKCP5, DKCP1–5 сохраняют прежнюю read compatibility. В fixture
12raw/8KTGS объектов; наш reopen восстанавливает12 и exact buffer/graph bytes.
Pointers обнулены, XY относительные. Source duplicate header-size defect
исправлен actual byte count; пустой KTGS остаётся перед extension для oldreader.

Source capture3files0, direct/selection/historical/protection/terrain/order
focus0, archive14checks0, official7file model0, publish0. All41build0/0W/E,
final Validation build0. Initial all41build86510 exit1 содержал ошибки новых
test helpers (API названия); исправлены по actual APIs, product build был
успешным. Первый дополнительный lock fixture ловил толькоIOException, но
Windows далUnauthorizedAccess; fixture принимает обе штатные причины отказа
и проверяет unchanged destination/no leftovers. Product file guard не ослаблен.
Ошибочный unsupported --selection-policy-only запустилdefault route22115;
его явно остановили terminal1 без acceptance, затем правильный selection-files
focus завершился0. Эти initial/stopped журналы сохранены.

Один полный20503 после готового блока terminal exit1: **1811PASS /63 отказа**.
Входной путь и оба полных упорядоченных списка совпали буквально с последним
ScenarioPrerequisites full; исходные94 сопоставлены полностью,31priorfixed/
oneknownXXLevel14 diagnostic. Новых failures/lostPASS нет; corpus не зелёный,
отчёт не фильтровался. Новая direct группа встроена в прежний selection-order
PASS; counts top-level не увеличены для новых cases.

Core SHA256: `edee0dd4690ed78c874dff181240398d7ce7d6f784dfab3443af619a72eeb515`.
Evidence: artifacts/Evidence/DirectSelectionExport-20261014. Current checked
Validation/Official/ArchiveWrite/publishedCore/App соответствуют build binaries.
Fresh official compiler/export/reopen7bytes совпадают с прежним TestMap.
Source/runtime archives обновляются из canonical41graph/src/docs; проверяются
SourceBinaries1006/maps4453/fixtures/TestMap7 и все2540ENRUliteral values.
Окна/App/WPF/officialGUI/game не запускались; source route не заменяет runtime.

Подробности/ручные шаги: [DIRECT_SELECTION_EXPORT](DIRECT_SELECTION_EXPORT.md).
Глобальные отряды/зоны/paths не добавлялись вplaced-object archive. Real
historical DKZ, actualoriginalGUI/paste/dialog иgame checks остаются; remaining
selection/nonpaintingcaller/history/workflows иall23FEATURE_PARITYrows/fullgates
сохраняются. ЦельACTIVE, не «только ручные».

## Текущая сборка: SelectionCallers-20261014

Workspace Cut/Paste используют новые explicit editor callers. Исходный
Select_Copy(Cut)/Select_Paste создаёт один parentUndo и MapChanges+1 даже
при равных bytes; removal helper добавляет1 за каждый снятый placed object.
Inserted Paste objects дополнительных increments не дают. Copy read-only,
paint/click activity usage не меняются. Новые wrappers задают exact1+removed
completion в одной atomic transaction, используя прежние generic byte/graph/
crop/protection operations. Generic CutSelection/PasteSelection не изменены.

Source capture3files0;32 native/project/CutPaste/removal0,1,3,5/protected
cases0, все5sourceкатегорий, oneUndoRedo/counter/notification, activity0,
generic equalno-op, uintwrap, invalid/format/nestedrefusal иclipped insertion0
extra. WPF actual-route equalCut/Paste/buffer/declined request/ENRU smoke
подготовлен иcompiled only. All41build81187terminal0/0W/E; latestValidation/
WindowsSmoke builds0, selectionfocus0, freshofficial7checks0, publish0.

Один full88361 после блока terminal exit1: **1811PASS /63 отказа**.
Inputs и полные ordered PASS/fail lists буквально совпали с предыдущим
DirectSelectionExport full. Original94 fullycompared:31priorfixed/oneknown
XXLevel14 diagnostic; новых failures/lostPASS нет, corpus не зелёный.
Новые cases исполняются внутри прежней selection-order group; top-level
число не увеличивается для вложенных комбинаций. Installed карты не исправлялись.

Core SHA256: `e01e145ae10634d1384318e896e1795e1e8c2bbd1e260b7690704974d3247df4`.
Evidence: artifacts/Evidence/SelectionCallers-20261014. Checked/published
Core/App совпадают; fresh official7bytes совпадают с прежнимTestMap.
Source/runtime packages updated изcanonical41graph/src/docs без recoverychain;
SourceBinaries1006/maps4453/fixtures/TestMap7 и2540ENRUliterals защищены.
UI/App/WPF/renderer/game не запускались. Current detailed documentation/manual:
[EDITOR_SELECTION_CALLERS](EDITOR_SELECTION_CALLERS.md).

Следующее доказанное расхождение — equal Rotate/Reflect acceptance: source
Select_Rotate/Select_Symmetry даёт parentUndo/MapChanges+1, current App ещё
вызывает generic changed-onlyTransformSelection. Требуется explicit editor
transform caller при сохранении raw semantics/sizeguards/identities. Это
обнаружено чтением source/caller, не исправлено текущим блоком. Остальные
nonpainting/history/workflows/realcorpus иall23rows/fullphysicalGUI/audio/
systemdialog/hardware/DPI/game gates остаются; goalACTIVE/notmanualonly.


## Предыдущая сборка: TransformCallers-20261014 (счётчик уточнён ниже)

14 октября 2026. Повороты и отражения в рабочем окне используют
`TransformEditorSelection`. Как в исходном Select_Rotate/Select_Symmetry,
принятая команда создаёт один шаг Undo и увеличивает счётчик изменений на 1,
даже если квадрат однообразный и байты не меняются. Перемещённые объекты
дополнительного увеличения не дают; статистика рисования не меняется.
Базовый TransformSelection сохранил прежнее поведение при равном результате.
Ограничения размеров, защищённые клетки, координаты, идентификаторы и связи
сценариев используют прежний алгоритм. Подробности и ручные шаги:
[EDITOR_TRANSFORM_CALLERS](EDITOR_TRANSFORM_CALLERS.md).

Все 41 проекта собраны: код 0, без ошибок и предупреждений. Проверка исходника,
проверки выделения, свежий семифайловый комплект и публикация завершены с кодом 0.
Новые модели проверяют 32 сочетания: два формата, четыре преобразования,
равный/неравный результат и край/внутренняя часть карты; отдельно проверены
отказы, квадрат 1×1, переполнение счётчика и нулевая глубина истории.
Дополненные проверки команд WPF скомпилированы; окна не запускались.

Полный запуск 15564 завершён с кодом 1: **1811 PASS / 63 отказа**.
Весь упорядоченный список результатов и входных файлов совпал с предыдущим
SelectionCallers. Новых отказов и потерянных PASS нет. Исходные 94 отказа
сопоставлены полностью: 31 устранён ранее, одна известная диагностика XXLevel14
изменилась ранее. 63 отказа остаются видимыми; установленный корпус не исправлялся.
Повторный полный прогон для этого блока не нужен.

Core SHA256: `fa03403be71f072d2bcb78135aa348046fae1b56cedc3b64a5cd713100faf810`.
Доказательства: `artifacts/Evidence/TransformCallers-20261014`.
Проверенные и опубликованные DLL совпадают. Пакеты исходников и приложения
обновляются из единого проекта; Source/Binaries, карты, fixtures и семь файлов
TestMap сохраняются. EN/RU: 2540 совпадающих ключей, прежние значения сохранены.

Ранее отмеченное расхождение равных Rotate/Reflect исправлено этим блоком.
До состояния «только ручные проверки» остаются контекстные/составные обработчики,
остальные маршруты счётчиков и истории, семантика компиляции и исторические
форматы. Все 23 строки FEATURE_PARITY и физические проверки сохраняются.
Общая цель ACTIVE. Окна редактора, игра, звук и системные диалоги не запускались.


## Текущая сборка: TransformRemoval-20261014

14 октября 2026. Исправлены два составных маршрута, использующих
Things_RemoveAtMapPosEx: поворот/отражение и кисть объектов. Предыдущая сверка
TransformCallers учитывала только верхнюю функцию и ошибочно утверждала,
что перемещаемые объекты добавляют 0 увеличений. Правило — **1 + N**, включая
объекты у защищённого края при преобразовании. Повторное размещение добавляет 0.
Одна принятая команда сохраняет один общий шаг Undo.

Кисть карты и окно одиночного размещения вызывают ApplyEditorObjectPainting:
1 + число удалений при создании, +1 при обновлении, включая равные байты,
и один UsageOfPaintThings. Базовые API сохранили прежние правила. Счётчик
откатывается с Undo; статистика остаётся накопительной.

Все 41 проекта собраны без ошибок и предупреждений. Окончательные сборки
Validation и WindowsSmoke, проверки выделения/кисти, захват трёх исходных
файлов, свежий семифайловый комплект и публикация завершены с кодом 0.
Дополнительно проверены 80 сочетаний количества объектов при преобразовании
и 58 случаев кисти; прежние 32 геометрических случая также сохранены.
Исходная ошибка новых тестовых данных (повторное пакетное размещение заменяло
объекты вместо создания скопления) исправлена явным дублированием. Журнал
painting-focus-fixture-failure сохранён; проверки продукта не ослаблялись.

Окончательный полный запуск 24412: terminal exit 1, **1811 PASS / 63 отказа**.
Полные упорядоченные списки PASS, отказов и входов совпали с TransformCallers.
Исходные 94 отказа сопоставлены полностью; новых отказов и потерянных PASS нет.
Промежуточный запуск 41711 осознанно остановлен после расширения блока на
кисть объектов. Его журналы сохранены с intermediate-префиксом и не считаются
доказательством окончательной приёмки. Повторять зелёные проверки этого блока
без новых правок или обнаруженных ошибок не требуется.

Core SHA256: `524b002147c056800e9d99954ac61625187151211cd3132c8da0c764ba2129ed`.
Доказательства: `artifacts/Evidence/TransformRemoval-20261014`.
Проверенные и опубликованные DLL совпадают; пакеты обновлены из единого
основного проекта. Source/Binaries, установленный корпус, fixtures, семь файлов
TestMap и все 2540 прежних значений EN/RU сохранены.

Подробности и ручные шаги: [преобразования](EDITOR_TRANSFORM_CALLERS.md),
[кисть объектов](EDITOR_OBJECT_PAINTING_CALLERS.md).
Редактор, WPF, игра, звук и системные диалоги не запускались.
Следующая работа — оставшиеся составные обработчики/история и исторические
форматы по полному FEATURE_PARITY. Все 23 строки и физические ворота сохраняются;
цель ACTIVE, состояние «остались только ручные» ещё не подтверждено.


## Текущая сборка: ProjectileAcceptance-20261015

15 октября 2026. Специализированные параметры снарядов теперь повторяют
исходные выборы: неизвестная цель → первый элемент каталога, неверный уровень
→ 1, принятие предмета ID≠1 → второй параметр0. Открытие и отмена не записывают
подстановки. Дочерние окна возвращают частный черновик до общего Apply;
встроенная панель использует явный SetEditorGlobalProjectile. Базовый API
сохраняет прежние правила удержания импортированных значений.

Охвачены ловушки, оба слота хранителя и заклинания существ. 1320 сочетаний
инициализации всех66 типов и96 native/project/four-slot принятия проходят;
полные байты, история, счётчики и Undo/Redo сверены. Захват исходных функций
и каталогов, all41 build без предупреждений/ошибок, окончательный WindowsSmoke
build, focused projectiles, свежий семифайловый комплект и publish: terminal0.
Дополненные проверки дочерних окон и встроенной панели скомпилированы;
окна, WPF/renderer, игра и официальный редактор не запускались.

Один полный запуск50749 после блока: terminal exit1, **1811PASS /63 отказа**.
Входы и полные ordered PASS/failure arrays совпали с TransformRemoval.
Исходные94 отказа сопоставлены полностью:31 устранён ранее, одна известная
диагностика XXLevel14 изменена ранее. Новых отказов/потерянных PASS нет.
Установленный корпус не исправлялся. Зелёные проверки этого блока не повторять
без новых изменений или обнаруженной ошибки.

Core SHA256: `a752d115ac092b74d9914c1423e9718ef1b089987ce94242358dd5cf43d0e307`.
Доказательства: `artifacts/Evidence/ProjectileAcceptance-20261015`.
Проверенные/published DLL совпадают, пакеты обновлены изcanonical41project.
SourceBinaries1006/maps4453/fixtures/TestMap7 и2540ENRUliteral values сохранены.
Подробная документация/ручные шаги: [PROJECTILE_EDITOR_ACCEPTANCE](PROJECTILE_EDITOR_ACCEPTANCE.md).

Остальные составные callbacks, caller/history differences, compiler/historical
семантика и физическая EN/RU GUI/audio/systemdialog/hardware/DPI/game приёмка
остаётся. Все23 capability rows/fullgates сохраняются; цельACTIVE, переход
к состоянию «только ручные проверки» ещё не подтверждён.

Следующий конкретный пробел подтверждён чтением: Properties_MapCreaturesChanged
показывает четыре средних только по ненулевым значениям выбранных неэлитных
видов; Properties_MapCreaturesSetParam пишет выбранный параметр выбранным видам.
Текущая RuleTable не имеет обработчика SelectedCellsChanged и отдельного
четырёхпараметрического выбора. Сериализация/диапазоны/elite guards уже есть;
требуется частный контекст выбора/ввода и compose с общим MapProperties Apply/
Cancel. Исходные методы, current Rules/XAML и хеши сохранены в
next-creature-modifier-scope.json этого evidence. Этот пробел пока не исправлен.


## Текущая сборка: CreatureModifiers-20261015

15 октября 2026. Выявленный пробел групповых модификаторов закрыт новым
частным контекстом и дочерним окном общих свойств карты: выбор нескольких
видов, четыре независимых средних только по ненулевым значениям, исключение
элитных строк17–29, per-parameter запись выбранным видам. Дочерний Cancel и
общий Cancel сохраняют живую карту; дочерний OK переносит в WorkingDocument,
общий Apply объединяет изменения в один исходный счётчик/Undo без resize.
Pending/stale/closed/foreign/nested guards и ревизия ребёнка при consent сохранены.

Все41 проекта собраны0 без предупреждений/ошибок. Окончательный WindowsSmoke
build пересобрал App после правки выбора и защиты ввода при consent, завершён0.
Source capture четырёх функций, focused parent/map-properties и свежий
семифайловый комплект:0. Новый Core проверяет32 буквальных сочетания выбора,
все строки/13 элитных, четыре поля, частную/общую отмену и полный Undo/Redo.
Новые WPF EN/RU проверки реальных классов подготовлены иcompiled only.
Добавлено8 ключей EN/RU; прежние2540 literal values сохранены, всего2548 ключей.

Один окончательный full42074: terminal exit1, **1811PASS /63 отказа**.
Входы и весь ordered PASS/failure arrays совпали с ProjectileAcceptance;
исходные94 сопоставлены полностью. Новых отказов и потерянных PASS нет.
Установленные карты не исправлялись. Повторять проверки без новых правок
или обнаруженной ошибки не требуется.

Core SHA256: `b08a1f54fccd56f65b34d202a20df1d64e797abb601c849b9a2961f613bb96c8`.
Evidence: `artifacts/Evidence/CreatureModifiers-20261015`.
Проверенные/published DLL совпадают; source/runtime пакеты обновлены из
единого41project без восстановления прежних версий. SourceBinaries1006,
maps4453, fixtures и TestMap ровно7файлов/bytes сохранены.
Подробное описание/ручные шаги: [MAP_CREATURE_MODIFIERS](MAP_CREATURE_MODIFIERS.md).

Окна, WPF/renderer, игра, официальный редактор, звук и системные диалоги
не запускались. Остальные составные callbacks/история, compiler/historical
семантика и физическая приёмка EN/RU GUI/audio/systemdialog/hardware/DPI/game
остаются; все23 строки FEATURE_PARITY/fullgates сохраняются. GoalACTIVE.


## Текущая сборка: ScriptIdentities-20261015

15 октября 2026. Ручное изменение сценарного ID использует общий remapper:
нельзя перенести ссылки от дублирующегося старого ID к произвольной записи;
однозначное событие обновляет все физические связи, в том числе из записей,
не вошедших в основной dictionary index из-за посторонних дубликатов.
Нулевой ID исправляется без переноса пустых связей. Project условия требуют
1–32767, действия32768–65535 для новых ID; неизменённые импортированные IDs
и native reserved ACTION bytes сохраняются. Никакие карты не ремонтируются
при загрузке, диагностике или этом этапе.

Все41 build0 без ошибок/предупреждений; окончательный Validation build0,
focused identities/diagnostics/event-layout groups0, official seven0, publish0.
Новый malformed fixture исправлен по исходному NODE ABI12 вместо8; исходный
failed journal сохранён. WPF identity projection использует ID своей категории;
собран, но не запускался. Общий full64684: terminal exit1, **1811PASS/63 отказа**.
Весь ordered PASS/failure arrays и Inputs совпали с CreatureModifiers;
исходные94 сопоставлены полностью, новых failures/lostPASS нет.

Read-only аудит63 отказов дал27карт:9 missing tile/27checks,6 truncatedVariables/
24checks,8 noncanonicalIDs/8checks,4 danglinglinks/4checks. Variables действительно
не имеют одной16-byte записи по count/size; исходный reader тоже EOF.
Для сценарных текстовых отказов игровой вылет не доказан. Это конкретные
причины/пути, а не63 независимых дефекта редактора. Все ошибки остаются видимыми.

Core SHA256: `0056e6fbe50a383482ab2c2c91f956aed801abd1b36955288ec40e81e28585ea`.
Evidence: `artifacts/Evidence/ScriptIdentities-20261015`.
Проверенные/published DLL совпадают; единые source/runtime пакеты обновлены.
SourceBinaries1006/maps4453/fixtures/TestMap7 и2548ENRUliteral values сохранены.
Документация: [ID safety](SCRIPT_IDENTITY_SAFETY.md),
[разбор27карт](CURRENT_CORPUS_REFUSALS.md). Окна/WPF/игра не запускались.

Другие составные обработчики/caller-history differences, real historical
corpus/compiler acceptance и физическая EN/RU GUI/audio/systemdialog/hardware/
DPI/game приёмка ещё остаются. Все23строки/fullgates сохраняются; goalACTIVE.

## 15 октября 2026 — согласованная история Undo

Пользователь выбрал сохранение нынешнего Undo для изменения паузы патруля
и параметров сценария. Не удалять эту возможность ради отсутствующего Undo
в соответствующих старых обработчиках и не запрашивать решение повторно.
Изменённые параметры создают один Undo; равный принятый сценарный Save
сохраняет нынешнее увеличение счётчика без нового шага. Принятая пауза,
включая равную, сохраняет нынешний шаг при ненулевой глубине.
Точная семантика и остальные отличия описаны в [EDIT_HISTORY](EDIT_HISTORY.md).

Текущая правка касается документации. Код, тесты, DLL и семифайловая карта
не менялись; сборка и полный прогон не повторялись. Унаследованы результаты
`ScriptIdentities-20261015`: все 41 проекта собраны, 1811 PASS / 63 отказа,
terminal exit 1, без новых отказов или потерянных PASS. Обновлены документы
обоих архивов поставки; доказательство — `artifacts/Evidence/HistoryDocumentation-20261015`.
Ни аппаратная приёмка, ни остальные требования полной цели этим решением
не закрываются; цель остаётся ACTIVE.


## Текущая сборка: ResizeScriptTargets-20261015

15 октября 2026. Исправлен исходный маршрут переноса координат трёх действий
при изменении размера через общее окно свойств: создание существа, изменение
клетки и владельца. Цель может остаться за границей карты; изменение размера
сохраняется одним общим Undo. Отдельные общие Resize/PrepareMapProperties
сохраняют строгий отказ. Проект сохраняет LONG, native — WORD-проекцию;
перенос целей не добавляет source counter increments к общему Apply/resize.

Добавлена EN/RU диагностика DK2N029: ошибка для привязанной выходящей за
верхние границы цели проекта, предупреждение для native/непривязанного действия.
Проверка не исправляет карту. Исходный compiler проверяет это после resize.
Все прежние2548 literal resource values сохранены, теперь2549 ключей EN/RU.

All41 build0 без предупреждений/ошибок; окончательная Validation build0,
focus-02 map-properties0, script identities/diagnostics/all103layouts0,
source properties caller и cross-caller interaction0, official seven0/publish0.
Новая фикстура исправлена: ID захватывается до вставки project binding,
которая перемещает ACTION record. Первый failed focus journal сохранён.
Проверены78 literal source cases, все100 других типов событий неизменны,
preview/Cancel/Undo/Redo/save/reopen, строгий API, валидная компиляция,
отказ привязанной неверной цели и нулевая история. WPF EN/RU/native/project
тест фактического класса окна собран, но не запускался.

Один окончательный полный прогон: terminal exit1, **1811 PASS /63 отказа**.
Входы и весь ordered PASS/failure arrays совпали с ScriptIdentities;
исходные94 полностью сопоставлены, новых failures/lostPASS нет.
Разбор27карт унаследован после проверки неизменности его входов;
новый аудит или ремонт установленных карт не заявляется.

Core SHA256: `fae04916102c077f22b1a073c40ba0f5021d58ddf335632e73e5e067f0ab4268`.
Доказательства: `artifacts/Evidence/ResizeScriptTargets-20261015`.
Проверенные/published DLL совпадают. Пакеты source/runtime обновляются из
единого41project с документацией и прежним семифайловым TestMap.
SourceBinaries1006/maps4453/fixtures сохраняются и проверяются упаковщиком.
Подробности и ручные шаги: [EDITOR_RESIZE_SCRIPT_TARGETS](EDITOR_RESIZE_SCRIPT_TARGETS.md).

Окна/WPF/renderer, официальный редактор, игра, звук и системные диалоги
не запускались. Полная цель ACTIVE; все23строки/fullgates сохраняются.
Остальные составные callbacks/caller-history, compiler/historical и физическая
EN/RU GUI/audio/systemdialog/hardware/DPI/game приёмка остаются.

Следующий подтверждённый остаток resize: исходный Properties_UpdateProperties
сдвигает camera/CPUArea только у компьютерных keeper3–7, сохраняет парные
sentinel и не ограничивает координаты границей. Общий Resize сейчас сдвигает
и ограничивает поля всех игроков. Требуется отдельный исходный оконный маршрут
с сохранением generic API, проверкой исходных пар и native проекции.
Снимок этого отличия: `next-player-resize-scope.json`. Оно не исправлено этим блоком.


## Текущая сборка: ResizePlayers-20261015

15 октября 2026. Общее окно свойств переносит камеру и область ИИ только
компьютерных хранителей3–7. Проектный X=-1 сохраняет всю пару/область;
прочие вторичные -1 сдвигаются вместе с координатами. Геометрия людей,
героя и нейтрального игрока не меняется. Учитывается частное переключение
управления перед общим Apply. Координаты не ограничиваются размером карты;
source LONG/игровая WORD-проекция сохраняются. Native65535 reader загружает
как положительный LONG, не как проектный -1. Общий generic Resize прежний.

V1/V2 player layouts сохраняют игнорируемые камеры V2 и сердца V1, обновляют
только поддержанную область ИИ без автоматического upgrade. Неоднозначный
native компьютерный keeper не даёт разрешения угадывать владельца.
DK2N030/031 — исходные неблокирующие предупреждения EN/RU о камере любого
хранителя и области компьютерного хранителя. Они не исправляют данные;
native диагностика оценивает WORD-проекцию, а не отсутствующий исходный LONG.

All41build0 без предупреждений/ошибок; focus map-properties0, identities/
diagnostics/all103eventlayouts0, source properties caller/cross-caller0,
official seven0/publish0. Новый блок прошёл с первой попытки. Проверены54
матрицы обоих форматов/9anchors/7players, независимые literal переполнения,
парные признаки/FFFF, V1/V2 opaque bytes, полный UndoRedo/save/reopen,
частная композиция/Cancel, предупреждения/компиляция и нулевая история.
Добавлены2 EN/RU ключа, прежние2549 literal values сохранены, всего2551.
Подготовлен и собран WPF-тест фактического окна EN/RU, но не запускался.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Весь ordered
PASS/failure arrays и Inputs совпали с ResizeScriptTargets; исходные94
полностью сопоставлены. Новых отказов или потерянных PASS нет. Существующий
разбор27карт унаследован после проверки неизменности его входов; установленные
карты не ремонтировались. Исходные raw reports сохранены полностью.

Core SHA256: `86ef97f7f4e1474d960faae1c37347f52839b35e24e6c79314e5f675921b29e6`.
Evidence: `artifacts/Evidence/ResizePlayers-20261015`. Current/published DLL
совпадают; единые source/runtime пакеты включают актуальную документацию.
SourceBinaries1006/maps4453/fixtures и ровно7 TestMap файлов проверяются
при обновлении пакетов; результат указан в delivery-verification.json.
Описание и ручные шаги: [EDITOR_RESIZE_PLAYERS](EDITOR_RESIZE_PLAYERS.md).

Окна/WPF/renderer, официальный редактор, игра, звук и системные диалоги
не запускались. Цель ACTIVE; все23 capability rows/fullgates сохраняются.
Остаются другие callbacks/caller-history, compiler/historical/native editor
metadata и физическая EN/RU GUI/audio/systemdialog/hardware/DPI/game приёмка.

Следующий конкретный остаток resize: native ACTION_SHOWCRYSTAL и
ACTION_DESTROYGATE содержат вычисляемые координаты. Исходный compiler
вычисляет их по предмету0x1C и прямоугольнику выбранных ворот; native resize
сейчас перемещает сущности, но меняет payload только трёх прямых координатных
действий. Требуется сверить native reader и порядок/однозначность разрешения
ссылок, не угадывая импортированные связи. Снимок: next-native-computed-target-scope.json.
Этот остаток не исправлен данным блоком и не заменяет остальные требования.


## Текущая сборка: NativeDerivedResize-20261015

15 октября 2026. Исправлен пересчёт вычисляемых native координат действий
«показать кристалл» и «уничтожить ворота» при изменении размера через общее
окно свойств. Первый кристалл выбирается в физическом порядке после удаления;
ворота разрешаются по точной прежней позиции, без выдуманного уникального ID.
Учитывается окончательная граница. Исходные результаты при отсутствии целей:
WORD65535/65535 и1/1 соответственно; они не объявляются исправленной целью.
Меняются только четыре байта координат, без дополнительных счётчиков;
частная подготовка и общий Undo сохраняют все остальные байты/связи/IDs.
Generic Resize, равное Apply, загрузка и простое сохранение не нормализуются.
DK2N032 EN/RU предупреждает об отсутствующей/устаревшей native цели без записи.

Все41 проекта собраны без ошибок и предупреждений. Первая сборка обнаружила
nullable-ошибку в новой тестовой фикстуре; она исправлена, первый журнал сохранён.
Фокусный блок, identities/diagnostics/all103 layouts, source properties caller,
cross-caller, source audit, официальный семифайловый compiler и publish: exit0.
Новые проверки:27 матриц привязок и9 буквальных случаев удаления/границы/
отсутствия/сердца/одинаковых позиций; полные action bytes, счётчики, Undo/Redo,
save/reopen, preview/Cancel, отказ от удаления, повторные физические IDs,
нулевая история, общий Resize и проектная компиляция. Прямые цели сохраняют
78 случаев, игроки54; остальные98 типов проверяются на неизменность отдельно.
Всего2552 EN/RU ключа, прежние2551 значения сохранены буквально.

Один полный прогон завершён: exit1, **1811 PASS /63 отказа**. Полные ordered
PASS/failure arrays и Inputs совпали с ResizePlayers. Все исходные94 отказа
сопоставлены:31 ранее исправлен, одно ранее известное изменение сообщения.
Новых отказов и потерянных PASS нет. Разбор63 отказов на27 картах унаследован
только после сверки хешей его входов; карты не ремонтировались.
Core SHA256: `47b9f84c93f32a4ff388405e5da16b987b96ac17fe6393ed90babce7407eca42`.
Evidence: `artifacts/Evidence/NativeDerivedResize-20261015`.
Текущая DLL и опубликованная совпадают. Упаковка проверяет Source/Binaries1006,
установленные maps4453, fixtures, все записи ZIP и неизменные7 файлов TestMap;
фактический результат записывается в delivery-verification.json.
[Правила, ограничения и ручные шаги](NATIVE_RESIZE_DERIVED_TARGETS.md).

Этот блок закрывает только общий resize. Другие перемещения/удаления/добавления
сущностей и native рабочие связи требуют отдельной сверки исходных маршрутов.
В частности, SetMapRegionBounds сам меняет геометрию ворот, но не пересчитывает
координаты зависимого действия. Снимок следующего участка:
next-other-computed-caller-scope.json. Общая политика компилятора для отсутствующих
проектных целей этим изменением не исправлялась.

Цель ACTIVE; все23 строки capability ledger сохранены. Остаток автоматической
работы: прочие связи объектов/сценариев и команды, семантика compiler/historical/
native metadata, оставшиеся зависимые callbacks и source counters/history.
Это группы аудита, а не конечное число подтверждённых ошибок. Физические EN/RU
окна, мышь, звук, системные диалоги, hardware/DPI, официальный редактор и игра
остаются отдельной приёмкой. WPF-тест нового окна собран, но не запускался.
Повторять зелёные прогоны из-за обновления документации не требуется.


## Текущая сборка: NativeGateMutation-20261015

15 октября 2026. Исправлено обновление native ACTION_DESTROYGATE при явной
геометрии ворот, редактировании поля, общем окне свойств области и его
вложенной генерации, смене ворот на сердце и удалении выбранной записи.
Разрешение до команды соответствует reader: первая физическая точная позиция,
сердца по type14 исключены независимо от bGateID. Список выбора и выбор
сохранённой цели используют те же правила; bGateID больше не считается ID.
При переносе меняются только четыре байта координат, при удалении выбранных
ворот writer даёт1/1. Это не исправленная отсутствующая цель. Preview/Cancel,
повторные IDs, резервные байты, исходные счётчики, общий Undo и raw generic crop
сохраняются. Вложенный replay обновляет и импортированные, и созданные действия.
[Подробности и ограничения](NATIVE_GATE_MUTATION.md).

Все41 проекта собраны с первой попытки,0 ошибок/предупреждений. Focus region
properties0; spatial, region generation, map properties, identities/diagnostics/
103 layouts, properties caller и cross-caller0; official seven/publish0.
Проверены11 сочетаний геометрии/полей/частного родителя,5 случаев удаления/типа,
физический порядок дубликатов, hearts и kind0/77/99, промежуточное совпадение,
полные action bytes, один счётчик/Undo/notification, save/reopen, zero history,
повторные IDs, raw generic crop и imported/generated replay.
Ресурсы EN/RU не менялись:2552 ключа и все2552 прежних literal values.
WPF-тест фактического RegionPropertiesWindow EN/RU подготовлен и собран;
окна, WPF, renderer, официальный редактор, звук и игра не запускались.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Все ordered PASS,
failures и Inputs буквально совпали с NativeDerivedResize; исходные94 полностью
сопоставлены,31 ранее исправлен и одно ранее известное сообщение изменено.
Нет новых отказов или потерянных PASS. Разбор63 отказов/27 карт унаследован
после сверки неизменных входов. Карты не ремонтировались.
Core SHA256: `a54db833d7bd02ac1c24944d2a5ee347368642004ec91dfa45bfe6ec3ce7d150`.
Evidence: `artifacts/Evidence/NativeGateMutation-20261015`.
DLL current/published совпадают; единые source/runtime архивы и документация
обновляются с проверкой всех ZIP entries, Source/Binaries1006, maps4453,
fixtures и неизменного комплекта ровно7 TestMap. Результат упаковки:
delivery-verification.json. Повторного прогона из-за документации нет.

Конкретный следующий остаток: постоянные рабочие ссылки native ворот между
разными командами при совмещении/разделении, с Undo/private copies/save policy;
эта правка сохраняет разрешение внутри одной команды, не вводит DKM IDs.
Также остаются другие команды изменения первого кристалла и полная политика
компилятора для отсутствующих проектных целей. Снимок next-working-bindings.json.
Цель ACTIVE; все23 строки capability ledger и физические gates сохранены.
Остаток теперь собран в [едином перечне](REMAINING_WORK.md): семь очередей
сверки/исправлений плюс ручная приёмка. Это не семь ошибок и не семь прогонов.


## Текущая сборка: NativeGateBindings-20261015

15 октября 2026. Рабочая native связь ACTION_DESTROYGATE сохраняет конкретную
физическую запись ворот между командами совмещения/разделения, отдельными X/Y,
Undo/Redo и нулевой историей. Можно выбрать вторые ворота с теми же игровыми
координатами: меняется рабочая ссылка, один source count и один современный
Undo, игровые байты остаются прежними. Равный повтор Save сохраняет прежнюю
counter-only семантику. bGateID и ID события не служат рабочей идентичностью.
Native список выбора показывает порядковый номер и координаты EN/RU.

Внутренний NativeFile D2GB/version1 участвует в TransactionFiles, fingerprint,
частных копиях и архиве исходных данных DKM; он исключён из Files/Records и
физического игрового экспорта. Удаления ворот/событий переадресуют записи,
удаление обычных вещей перед воротами их связь не меняет. Копирование графа
переносит связь явно. Родительский resize, окно области, генерация и мастер
сценария сохраняют её. Scope мастера разрешает только связь текущего события.
DKM-конвертация использует выбранную запись для проектного param5, даже при
совпадающей позиции. Обычный native KWD после открытия повторяет исходный
reader first-position; для редакторского ID сохраняется рабочий DKM.
[Подробное устройство и границы](NATIVE_GATE_BINDINGS.md).

Все41 проекта собраны0 warnings/errors. Первые две focus-попытки сохранили
ошибку новой экспортной фикстуры: неполная synthetic draft без игровых
определений была неверно принята за официальный комплект. Проверка усилена
семью точными именами и запретом выхода GateBindings в export, использует
полный игровой basis; focus-03 exit0. Два Validation rebuild0, Core не менялся.
Первый standalone scenario-save вызван без обязательного пути корня и завершился
ошибкой запуска; исходный журнал сохранён, исправленный scenario-save-02 exit0.
Неудачные попытки не скрываются и не считаются продуктовыми PASS.

Native conversion, unified save/source archive, source parameter/Finish callers,
native gate/record/payload retention, spatial, generation, map properties,
identities/diagnostics/all103 layouts, properties и cross-caller: exit0.
Official seven/publish0. EN/RU2552 ключа и все2552 прежних literal values сохранены.
Новый блок проверил физические коллизии, metadata-only выбор, отдельные X/Y,
полные Undo/Redo и count, zero history, удаления разных семейств и bulk unused,
clone, wizard Cancel/Apply, parent resize, настоящий DKM Save/reopen с ID2,
официальные7 имён и исходную native reload policy. WPF-тест второго совпадающего
прохода через фактическое окно EN/RU собран; окна/WPF/renderer, официальный
редактор, звук и игра не запускались.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Все ordered PASS,
failures и Inputs совпали с NativeGateMutation. Исходные94 полностью сопоставлены:
31 ранее исправлен, одно ранее известное изменение сообщения. Нет новых отказов
и потерянных PASS. Разбор63 отказов/27 карт унаследован только после сверки
хешей входов. Установленные карты не ремонтировались.
Core SHA256: `7cecdb893483ebcdd97f548cc5bbf73ee613baf4c10ee1e13242419a3ecda8bb`.
Evidence: `artifacts/Evidence/NativeGateBindings-20261015`.
Current/published DLL совпадают. Упаковка обновляет единые source/runtime архивы,
проверяет все ZIP entries, Source/Binaries1006, maps4453, fixtures и неизменные
ровно7 TestMap; результат в delivery-verification.json. Документация не запускает
полный тест повторно.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Следующие конкретные участки A: source выделение/освобождение/повторное применение
ID после удаления (таблица физических ссылок не является полным распределителем),
исходные вопросы использования ворот и прочие команды пересчёта первого кристалла.
Compiler tasks/writer/historical, прочие callbacks/counters/catalogues и физическая
GUI/audio/system-dialog/hardware/DPI/game приёмка остаются. Материалы следующего
участка: next-derived-work.json. Этот блок не закрывает всю очередь A или цель.


## Текущая сборка: NativeCrystalMutation-20261015

15 октября 2026. Native ACTION_SHOWCRYSTAL пересчитывается при изменении
упорядоченного списка кристаллов ID28 или их координат. Общая внешняя транзакция
охватывает прямые поля, добавление/удаление/дублирование, менеджеры, рисование
объектов, Cut/Paste и Rotate/Reflect, включая замену целого буфера коллекции.
Первая оставшаяся физическая копия даёт координаты без+1, последняя удалена —
WORD65535/65535. Только четыре байта действия, все IDs/связи/резервные поля
сохраняются. Производная запись не добавляет count/Undo/notification.
Undo/Redo и rollback содержат полные буферы; нулевая история поддерживается.
[Правила и границы](NATIVE_CRYSTAL_MUTATION.md).

Равное поле/preview/Cancel, посторонняя покраска и параметр золота не меняют
цель при неизменном списке кристаллов. Загрузка/save/диагностика не нормализуют
payload. Generic Resize и generic map-properties сохраняют raw-контракт,
включая crop и подавление до конца внешней транзакции, также после исключения.
Исходный оконный resize имеет прежний полный маршрут. DKM параметры сохраняются,
координату вычисляет compiler; task-политика отсутствующего кристалла не менялась.

Все41 проекта с первой попытки:0 предупреждений/ошибок. Focus прошёл с первой
попытки:13 буквальных командных случаев, полные action bytes и нецелевое действие,
точный count/один Undo/notification, whole-buffer UndoRedo/save/reopen, physical
first-order при массовом перемещении в конец, no-op/preview/raw Resize/generic
properties, raw crop/rollback, missing-last/add-new, duplicate event IDs, depth0
и DKM semantic/compiler policy. Первый вспомогательный stage generator остановлен
до запуска команд: неизвестного CLI-флага selection не существует. Проверен
реальный --selection-order-only, source-audit-02 exit0; оба журнала сохранены.
Неподдержанный флаг не запускался и не создал дополнительный полный прогон.

Native conversion, object painting, selection order/cut/paste/transform callers,
source parameter/Finish, unified Save/archive, gate/record/payload retention,
spatial, region generation, parent properties/resize, identities/diagnostics/
all103 layouts, properties/cross-caller: exit0. Official seven/publish0.
2552 EN/RU ключа и все2552 прежних literal values неизменны. Этот блок Core
не менял виджеты и не запускал WPF/редактор/renderer/официальный редактор/звук/игру.

Первый полный прогон: terminal exit1, **1810 PASS /64 отказа**. Новый отказ
оказался устаревшим ожиданием менеджера: тест требовал неизменного D2 даже при
удалении первого кристалла. Ожидание исправлено независимым разбором wire bytes:
рассчитываются только два координатных WORD, остальные буферы сравниваются
целиком; полный Undo проверяется отдельно. Validation rebuild0 и адресная
проверка этой карты0. Код Core не менялся. Оба сырых отчёта сохранены.
Повторный полный прогон: terminal exit1, **1811 PASS /63 отказа**. Весь ordered PASS,
failures и Inputs совпали с NativeGateBindings. Исходные94 сопоставлены целиком:
31 ранее исправлен и одно ранее известное сообщение изменено. Нет новых отказов
и потерянных PASS. Разбор63 отказов/27 карт унаследован после сверки входных
хешей. Установленные карты не ремонтировались.
Core SHA256: `206ba94293befa1f5145a889a4bdda941ab856f5b89e10b6d640120306b776bc`.
Evidence: `artifacts/Evidence/NativeCrystalMutation-20261015`.
Current/published DLL совпадают. Единые source/runtime архивы и документация
обновляются с проверкой всех entries, Source/Binaries1006, maps4453, fixtures
и прежних ровно7 TestMap; итог указан в delivery-verification.json.
Повторного полного прогона из-за документации нет.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
В очереди A закрыт конкретный пересчёт кристалла для перечисленных команд;
source gate-ID allocation/free/reuse и вопросы используемых ворот остаются.
Подтверждён source следующий участок: RectEx_GetFreeID ищет первый свободный ID
от1, Undo удалённых ворот добавляет запись обратно в MapGates. Рабочая таблица
пока хранит физические ссылки и отсутствующий target, не всю жизнь source IDs.
Точные функции и хеши: next-gate-id-lifecycle.json.
Отдельно подтверждён source Undo_SUBOBJECT: Undo_RestoreNode добавляет предмет
в конец MapObjects, тогда как текущий точный снимок восстанавливает прежний
порядок и прежнюю цель. 15 октября пользователь явно выбрал нынешний полный
Undo удаления предметов: это согласованное отличие, не остаток очереди E.
Выбор не распространяется автоматически на gate-ID lifecycle и другие отличия.
Политика compiler tasks/writer, остальные callbacks/counters/historical/catalogues
и физические GUI/audio/
systemdialog/hardware/DPI/game gates остаются отдельными требованиями.


## Текущая сборка: NativeGateIdentity-20261015

15 октября 2026. Реализованы source выделение/освобождение/reuse ID ворот:
первая свободная1–255, отсутствие0 не приобретает цель, удалённая ненулевая
цель сохраняет ID и снова разрешается при его повторном использовании.
Рабочие ID не перенумеровываются после удаления. Сохранены sparse DKM IDs,
ID в рабочем результате compiler и generator site/plan; EN/RU picker
показывает действительный ID. Игровые байты меняются только в двух WORD
зависимого действия, в общей операции. Клон отсутствующей цели сохраняет ID.
[Правила, версия2, совместимость версии1 и границы](NATIVE_GATE_IDENTITIES.md).

Все41 проекта: exit0,0 ошибок/предупреждений. Первые три focus exit1 — ошибки
новых тестов: добавление ворот через ограниченное окно свойств карты, подсчёт
всех элементов плана вместо добавляемых и заранее созданная export directory.
Ожидания/fixture исправлены; Core после общей сборки не менялся. Три Validation
rebuild exit0; focus-04 exit0. Все журналы сохранены. Проверены literal IDs,
полные bytes/metadata UndoRedo/rollback, history0, Cancel/resize Apply,
свободные1/2/7/9, generator, версия1,255/отказ256 и семифайловый reload.

Native conversion, painting, selection/cut/paste/transform, parameter/Finish,
unified Save/archive, gate/record/payload retention, spatial, nested generation,
parent properties/resize, identities/all103 layouts, properties/cross-caller:
exit0. Official seven и publish exit0.2552 EN/RU ключа и literal values сохранены.
App изменён и собран, но реальные окна/WPF/renderer/звук/официальный редактор/
игра не запускались. Ручная приёмка этих поверхностей остаётся.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Полные ordered
Passed/Failures/Inputs точно совпали с NativeCrystalMutation-20261015.
Исходные94 сопоставлены целиком:31 ранее исправленный, одно ранее известное
сообщение изменено, новых отказов/потерянных PASS нет. Разбор63 отказов на27
картах унаследован только после сверки входных хешей. Карты не ремонтировались.
Core SHA256: `ee4cd768da725a8e5c6e090eadfde6852078f5e7c520316922a8f3d0e3c3ea45`.
Evidence: `artifacts/Evidence/NativeGateIdentity-20261015`.
Source/runtime архивы обновляются из единого проекта: все ZIP entries,
Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap и ресурсные
значения проверяются в delivery-verification.json. Документация не вызывает
повторный полный тест.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Следующий конкретный участок A — DKM gate removal guard: исходный SCOPE_GATE
не устанавливает Used при ссылке только из ACTION_DESTROYGATE, а текущий DKM
guard запрещает удаление. Общие подтверждения/remember/suppression тоже остаются;
E — порядок ворот после Undo
удаления. Согласованный полный Undo предметов сохраняется и не переобсуждается.
Остальные compiler/historical/catalogues/callbacks/counters и физическая
GUI/audio/systemdialog/hardware/DPI/game приёмка остаются требованиями.


## Текущая сборка: ProjectGateRemoval-20261015

15 октября 2026. Исправлен лишний guard удаления DKM ворот с входящим
ACTION_DESTROYGATE. Исходные Triggers_GetFirstTrigger/Triggers_IsUsed при
SCOPE_GATE возвращают-1 и не показывают отдельный used-dialog; удаление
сохраняет ACTION.param5. Текущая команда теперь сохраняет ID и все байты
действия; неизвестные layouts и другие entity/script guards не смягчены.
Предупреждение о ненулевой отсутствующей цели сохраняется; DKM draft можно
сохранить/открыть, compiler по-прежнему отказывает до устранения цели.
Повторное использование первого свободного ID восстанавливает цель.
[Полное описание](PROJECT_GATE_REMOVAL.md).

Все41 проекта: первая сборка exit0,0 ошибок/предупреждений. Focus с первой
попытки exit0: первые/средние/последние ворота с двумя действиями, независимое
ожидание полного KREC/размера +8, целые действия/другие разделы, один count/
Undo/notification, batch/rollback/history0, прочие entity/script/foreign guards,
read-only warning, реальный DKM Save/reopen, отказ compiler, клон отсутствующей
цели, reuse1, буквальные native WORD16/18 и семифайловый экспорт.
Прежнее ожидание общего typed-identity теста исправлено до прогона: ворота
удаляются с точным сравнением/Undo, остальные ссылочные сущности отказывают.

Native conversion, painting, selection/cut/paste/transform, parameter/Finish,
unified Save/archive, gate/record/payload retention, spatial, nested generation,
parent properties/resize, identities/all103 layouts, properties/cross-caller:
exit0. Official seven/publish exit0.2552 EN/RU ключа и literal values неизменны.
App widgets не менялись; окна/WPF/renderer/звук/системные диалоги/официальный
редактор/игра не запускались. Физическая приёмка остаётся.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Полные ordered
Passed/Failures/Inputs точно совпали с NativeGateIdentity-20261015.
Исходные94 сопоставлены целиком:31 ранее исправленный, одно ранее известное
сообщение изменено, новых отказов и потерянных PASS нет. Разбор63 отказов/27
карт унаследован после сверки входных хешей. Карты не ремонтировались.
Core SHA256: `8a77189c68333958312f5927321aa0136568b0737106bd7d7ec8ab4304947a1a`.
Evidence: `artifacts/Evidence/ProjectGateRemoval-20261015`.
Source/runtime архивы обновляются из единого проекта с проверкой всех ZIP
entries, Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap и ENRU
literal values; итог в delivery-verification.json. Полный тест из-за docs не
повторяется.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Следующий конкретный A/G участок — общее подтверждение удаления регионов:
source REQUEST_RECTEXKILL=0x10, Yes/No/default/remember/suppression и полнота
менеджера. В текущем inspector есть обычный Yes/No, source remember нет.
Источник/текущий callback сохранены в next-region-removal-work.json.
E — source порядок ворот после Undo; согласованный полный Undo предметов
сохраняется. Остальные compiler/historical/catalogues/callbacks/counters и
физическая GUI/audio/systemdialog/hardware/DPI/game приёмка остаются.


## Текущая сборка: RegionRemovalConfirmation-20261015

15 октября 2026. Добавлен первый общий вопрос удаления областей/ворот/эффектов:
REQUEST_RECTEXKILL0x10, defaultYes, No/close, remember/default-only suppression
и reset. Preference вне карты/Undo, общий между видами и документами рабочего
окна. Immutable request фиксирует актуальные записи/поля/revision/fingerprint;
не доверяет переданному caller полевому описанию. Один пакет — один count/
Undo/notification, history0 и исключение/послеcommit failure различаются.
[Поведение, исходники и границы](REGION_REMOVAL_CONFIRMATION.md).

Подключены inspector (Ctrl/Shift для регионов одного вида) и региональная
панель; разные виды/сердца не принимаются как общий logical-region пакет.
Прочие категории сохраняют single-selection и старые команды. Список регионов
показывает рабочие native gate IDs, не bGateID, и исключает keeper hearts.
EN/RU dialog/remember/reset/stale/mixed/postcommit message/history:10 новых ключей,
всего2562. Все2552 прежних literal values сохранены.

Три сборки всех41 проектов: exit0,0 предупреждений/ошибок. Вторая завершила
UI labels/heart filtering; после первой зелёной полной проверки review выявил
необходимость canonical field-schema snapshot. Доработан Begin и добавлен
отдельный случай пустого caller Fields: действующий area-reference guard
не обходится. Третья общая сборка и focus-02 exit0. Первый focus тоже exit0.
Шесть native/project × area/gate/effect матриц, целые buffers/header oracle,
No/checked-No/close/default/remember/reset, cross-kind/document, immutable
selection/canonical fields, one operation/history0, stale/raw/foreign/mixed/
duplicate/nonregion/heart/nested/pen/closed guards, failure и committed observer.

Обе серии native conversion/painting/selection/parameter-Finish/unified Save/
gate-record-payload retention/spatial/generation/map-properties/identities103/
properties/cross-caller прошли exit0; вторая серия на конечной Core с отдельными
каталогами. Обе official seven/publish exit0. Четыре WPF EN/RU × format случая
подготовлены и собраны, но не запускались. Реальные окна, renderer, звук,
системные диалоги, исходный редактор, игра и hardware/DPI не проверены этим блоком.

Первый полный прогон: terminal exit1,1811 PASS/63 отказа на прежней Core этого
блока; после canonical-schema доработки второй полный прогон: terminal exit1,
**1811 PASS /63 отказа**. Оба ordered Passed/Failures/Inputs точно совпали с
ProjectGateRemoval-20261015, сырые отчёты сохранены. Исходные94 сопоставлены
целиком:31 ранее исправленный, одно ранее известное сообщение изменено, новых
отказов/потерянных PASS нет. Разбор63 отказов/27 карт унаследован после сверки
input hashes. Установленные карты не ремонтировались.
Core SHA256: `685419d754af27523d5794bb8057ba6499cd4312a08848280d18d5d2b6bd5a6c`.
Evidence: `artifacts/Evidence/RegionRemovalConfirmation-20261015`.
Единые source/runtime архивы обновляются с проверкой всех ZIP entries,
Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap и EN/RU.
Итог — delivery-verification.json; полный тест из-за docs не повторяется.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Этот блок закрывает первый общий вопрос, не весь RectEx_RemoveRects.
Следующий A/G участок: specialized used-area Yes/No/Cancel, удержание orphan
scopes/ссылки эффектов и source переадресация первого nextid. Источники и
текущая команда сохранены в next-specialized-region-removal.json.
E — исходный append-порядок регионов после Undo; выбранный пользователем
полный Undo предметов сохраняется. Compiler/historical/catalogues/callbacks/
counters и физическая GUI/audio/systemdialog/hardware/DPI/game приёмка остаются.


## Текущая сборка: AreaRemovalPatrol-20261015

15 октября 2026. Подтверждённое удаление областей теперь обрабатывает строки
в порядке отображения и переадресует первого оставшегося предшественника
на следующую область удалённой записи. Работает для DKM и native; прочие
входящие патрульные связи не нормализуются. Низкоуровневый DeleteRecord
сохраняет прежнюю защиту. Остальные проверки ссылок и неизвестных layouts
остаются; второй used-area вопрос/orphan scopes/эффекты ещё не завершены.
[Поведение и исходный алгоритм](AREA_REMOVAL_PATROL.md).

Собраны все41 проекта: terminal exit0,0 предупреждений/ошибок. Focus exit0:
60 независимых packed-buffer сочетаний графа/порядка удаления, первая связь,
циклы/самоссылки/несколько предшественников/отсутствующие цели, все ожидаемые
байты и заголовки, полные Undo/Redo, один count/notification. Дополнительно
два late-failure случая подтверждают откат уже удалённой записи и изменённой
связи. Canonical field-schema и отдельный generic reference guard проверены.
Прежние шесть матриц запросов также прошли.

Native conversion, painting, selection, scenario Finish, unified Save,
gate/record/payload retention, spatial, generation, map properties, script
identities, properties callers и cross-caller: terminal exit0. Official
seven-file и publish: exit0. WPF harness собран; окна, renderer и игра
не запускались. Physical GUI/audio/dialog/hardware/DPI/game приёмка остаётся.

Полный прогон конечной Core: terminal exit1, **1811 PASS /63 отказа**.
Сырые упорядоченные Passed/Failures/Inputs точно совпали с предыдущим
RegionRemovalConfirmation-20261015/full-regression-02.json. Новых отказов
и потерянных PASS нет. Исходные94 сопоставлены целиком:31 ранее исправленный
и одно прежнее изменение сообщения XXLevel14. Аудит63 отказов на27 картах
унаследован после проверки входных SHA; установленные карты не исправлялись.
Core SHA256: `741f2c498db214771c508842cd172c22149f12780a598d696979f8d192fd1d30`.
Evidence: `artifacts/Evidence/AreaRemovalPatrol-20261015`.
Source/runtime ZIP обновляются из текущего единого проекта. Delivery проверяет
все entries, Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap,
новый official export и все2562 прежних EN/RU literal values. Итог —
delivery-verification.json. Полный тест из-за документации не повторяется.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md)
сохранены. Следующий участок A: второй Yes/No/Cancel используемых областей,
сохранение native orphan scopes в истории/приватных копиях/DKM и работа
ссылок эффектов. Это требует согласованной модели сценарных точек; нельзя
ограничиться снятием reference guard. Источники сохранены в предыдущем
next-specialized-region-removal.json. Исходный порядок регионов после Undo
остаётся отдельной E; полный Undo предметов согласован пользователем.


## Текущая сборка: UsedAreaRemoval-20261016

16 октября 2026. Подтверждённое удаление регионов включает второй used-area
Yes/No/Cancel. Пустая сохранённая точка тоже used; ID0 и action-only ссылки
не открывают второй вопрос, как в исходнике. No сохраняет used и удаляет
остальные, Yes удаляет все, Cancel ничего не меняет. Второй бит0x100 отдельно
от общего0x10; оба runtime preferences вне Undo и сбрасываются вместе.
Даже all-skipped No принимает один исходный счётчик и пустой Undo.
Полные [поведение и границы](USED_AREA_REMOVAL.md).

Сценарные точки и event/entity operands сохраняются при source-команде удаления
областей/эффектов. Generic DeleteRecord и неизвестные layouts сохраняют защиту.
Native #AreaScopes D2AS/v1 хранит явные пустые и orphan точки, входит в history,
private copies/fingerprints и DKM/native-source архив. Точки остаются корнями
unused planner, event-ID remapping, branch/wizard операций, явного удаления
точки и handoff владельца. ID reuse восстанавливает связь. Обычный KWD reload
не восстанавливает editor-only точки; игровой комплект по-прежнему7 файлов.
Map-properties child scope guard запрещает менять сценарии из своих настроек.
При позднем переполнении счётчика общий EditCore больше не теряет Redo.

Шесть сборок всех41 проектов и validation-build-06 завершились exit0 без
предупреждений/ошибок. Конечный focus-07 и hero-gate-focus-07 exit0.
Focus-04 и focus-05 также прошли на прежней
Core. Три ранних focus отказали на тестовых setup: неверно
повреждённый count исключал запись из списка, попытка изменить сценарий через
свойства карты корректно отклонялась, а native Save требовал единого
EditorSaveSession вместо project-only session. Фикстуры исправлены; сырые
логи и terminal codes сохранены. Два ранних source-audit отказа были вызваны
неполным regex return type и именем Request; source-audit-03 exit0.

Конечная проверка охватывает оба формата × пустые/непустые точки × ответы,
сохранённые operands/roots, обе настройки и stale second phase, полные native
game/working buffers и Undo/Redo, all-skipped/history0, неверный scope/ID0,
unused/identity/guard/ID reuse, private properties и wizard, DKM/archive/
compiler/seven-file. Прежние60 packed graph/order случаев сохранены; late
overflow проверяет также оба стека истории. EditorScenarioPointChecks32groups,
native conversion, painting/selection, scenario Finish103types, unified Save,
gate/record/payload retention, spatial/generation/map-properties/identities,
properties/cross-callers, official и publish: terminal exit0.
WPF EN/RU×format harness дополнен переходом двух окон/No/Cancel/remember,
только собран. Окна, renderer, игра, звук, исходный GUI и hardware/DPI не запускались.

Первый полный прогон:1810 PASS/64 отказа. Единственное добавление — прежнее
ожидание генератора, считавшее явно созданную пустую native точку отсутствующей.
Фикстура уточнена: отсутствующая точка добавляет отдельный счётчик, явно
сохранённая пустая — нет, в обоих форматах. Также после source review сохранён
прежний физический порядок живых roots при conversion среди других вещей;
добавлен независимый случай creature→area→band. После этой продуктовой правки
повторены integration-02/official-02/publish-02; все exit0. Второй полный
прогон1811/63 точно совпал с прежним ordered report. Последующий review выявил
конфликт DuplicateRecord с retained orphan ID и потерю пустого marker у копии.
Воспроизводящий focus-06 сохранил отказ Duplicate working area-scope address.
Исправлено: копия сохраняет свою пустую/клонированную точку, не присваивает
старую orphan и не теряет её; обычное создание с ID reuse восстанавливает
прежнюю точку. Четыре empty/nonempty × orphan/free-ID случая проверяют полные
буферы и одну операцию Undo/Redo. После этого integration-03/official-03/
publish-03 повторены на конечной Core, все exit0.
Полный-03 прогон конечной Core: terminal exit1, **1811 PASS /63 отказа**.
Упорядоченные сырые Passed/Failures/Inputs точно совпали с AreaRemovalPatrol.
Новых отказов или потерянных PASS нет. Исходные94 сопоставлены целиком:31
ранее исправленный, одно прежнее изменение XXLevel14. Аудит63 отказов/27 карт
унаследован после проверки входных SHA; установленные карты не ремонтировались.
Core SHA256: `a9d13669d5e6757a8854ed663b8cd48e9b057e899f3911a0b886e607213ce3ee`.
Evidence: `artifacts/Evidence/UsedAreaRemoval-20261016`.
Source/runtime архивы обновляются из единого проекта с проверкой каждого entry,
Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap и нового official
export. EN/RU2565keys:3 новых, все2562 прежних literal values сохранены.
Итог — delivery-verification.json. Полный прогон из-за docs не повторяется.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Далее A: caller обрезки Properties_RemoveRectEx. Исходник сохраняет точки и
operands, выставляет Nasty для used area, но не задаёт второй Yes/No/Cancel
и не переадресует nextid. Текущие resize callers ещё используют generic
DeleteRecord / DeleteLegacyRecord; их нужно перенести отдельно с общим parent
count/Undo. Nasty в полном исходном обработчике не читается, отдельного
предупреждения он не показывает. Нельзя вызывать direct-delete алгоритм с nextid
для crop. Исходные функции и современные callers сохранены в next-resize-crop.json.
Остальные compiler/historical/catalogue/callback/counter/native metadata и
физическая GUI/audio/systemdialog/hardware/DPI/game приёмка остаются открытыми.
Согласованный полный Undo предметов сохраняется; порядок регионов исходного
Undo остаётся отдельной E.


## Текущая сборка: RegionCropScopes-20261016

16 октября 2026. Source crop регионов сохраняет пустые/непустые сценарные
точки областей и operands, включая references областей/эффектов/ворот.
Подключены оба этапа фактического editor caller: outside ResizeCore и fixed
boundary после него. Предшественники nextid не переадресуются; второй вопрос
удаления не вызывается. Nasty исходником не читается и не требует окна.
Preview остаётся приватным, родительский Apply имеет один Undo/notification
и исходные счётчики. Generic native/project resize сохранили прежние отдельные
контракты. [Поведение и ручная проверка](REGION_CROP_SCOPES.md).

Две сборки всех41 проектов: terminal exit0,0 предупреждений/ошибок. Focus exit0:
72 native/project × kind/empty point × anchors × outside/frame случая,
полные event buffers и native working state при Undo/Redo, оба сохраняемых
предшественника, private Cancel/declined Apply, counts3/6, одна операция;
пакет трёх областей, DKM/native-source archive Save/reopen, first-free ID reuse,
compile/seven-file и прежние generic политики. Окно уже использует
PrepareEditorMapProperties. Старый быстрый Resize тоже направлен в то же
ShowMapProperties; отдельный ResizeMapDialog удалён после проверки отсутствия
других callers. Common Apply сохраняет точки из обеих UI-команд. Ресурсы не
менялись, все2565 прежних
EN/RU literal values сохранены. WPF проекты собраны, окна/игра не запускались.

Native conversion,32 point-operation groups, painting/selection, scenario
Finish103types, unified Save, gate/record/payload retention, spatial/generation/
map-properties/identities, properties/cross-callers, official и publish:
terminal exit0. Финальная App после перенаправления Resize повторно собрана/
published-02, Core hash не изменился; второй полный прогон ради UI delegation
не нужен. Окна не запускались. Полный прогон конечной Core: terminal exit1,
**1811 PASS /63 отказа**. Сырые ordered Passed/Failures/Inputs точно совпали
с UsedAreaRemoval/full-03. Новых отказов и потерянных PASS нет. Исходные94
сопоставлены целиком:31 ранее исправленный и одно прежнее изменение XXLevel14.
Аудит63 отказов/27 карт унаследован после проверки входных SHA; карты не правились.
Core SHA256: `06e669a3dea0acffb71add95bd8913e4640d854d60cab593291f5e5c347593f8`.
Evidence: `artifacts/Evidence/RegionCropScopes-20261016`.
Source/runtime ZIP обновляются с проверкой каждого entry, Source/Binaries1006,
maps4453, fixtures, ровно7 прежних TestMap, нового official export и всех2565
literal values. Итог — delivery-verification.json. Полный прогон ради docs
не повторяется. Физические GUI/audio/dialog/hardware/DPI/game проверки остаются.

Цель ACTIVE, все23 области и очереди A–G сохранены. Source crop регионов
закрыт; следующие связанные маршруты — Properties_RemoveThings / удаление
существ, дверей, обычных/специальных предметов и их native working scopes/
identity. Источник Things_RemoveAtMapPosEx сохраняет точки; существующие
generic paths ещё имеют другие scope contracts. Общий REQUEST_TRIGGERS0x100
между regional и creature callers остаётся в G. Не обобщать результат региона
на все физические владельцы. Источники и текущие callers — next-thing-scopes.json.
Остальные compiler/historical/catalogue/callback/counter и ручная приёмка
сохранены в [полном остатке](REMAINING_WORK.md).

## NativeOwnerScopes-20261016

16 октября: [рабочие точки владельцев](NATIVE_OWNER_SCOPES.md) охватывают
creature2/band8/member16/door64/object128/magical256. Явные пустые точки и
UID сохраняются независимо от raw event pointer. Source manager removal,
replacement, band deletion/private member compaction и Properties_RemoveThings
crop сохраняют orphan точки/операнды. Generic API сохраняют отдельные прежние
contracts. Полный Undo предметов с прежним порядком/целью подтверждён
пользователем; append-порядок исходного Undo предметов не воспроизводится.

Пересчитываются адреса владельцев после удаления любой BE-записи, переноса
пакета в конец файла, изменения размера и перестановки участников. Private
родитель отряда проверяет отображение слотов; Cancel не меняет карту. D2OS/v1
входит в dirty/history/rollback/private clones/fingerprints/DKM source archive
и compiler. Source Modify освобождает и перераспределяет ID, включая тот же
owner. Orphan ID зарезервирован: allocator сознательно безопаснее исходного,
пропускавшего objects и orphan roots; буквальный source allocator parity не заявлен.

Все41 проекта: build-final exit0, без предупреждений/ошибок. Последняя правка
тестовой фикстуры пересобрана point-build-08, Core неизменна. 33 point-operation
группы, включая12 six-family empty/nonempty матриц и32 thing-crop комбинации,
private band, replacement/generic distinction, full-buffer UndoRedo, UID
reallocation, DKM/archive/reopen/compiler и ровно7 игровых файлов: exit0.
Hero band24, historical20/compile5, inactive0/1/15/16, manager20, insertion8,
region focus, native conversion, painting/selection, scenario/unified save,
gate/record/payload retention, spatial/generation/map-properties/identities,
properties/cross-callers, official и publish: terminal exit0.

Полный корпус конечной Core: exit1, **1811 PASS /63 отказа**. Сырые ordered
Passed/Failures/Inputs совпадают с RegionCropScopes полностью. Исходные94
сопоставлены целиком; новых failures/lost passes нет. Аудит63/27 унаследован
после проверки исходных SHA; установленные карты не исправлялись. Перехода
к полностью ручной приёмке пока нет. Source/runtime ZIP проверены по каждому
entry, защищённые Source/Binaries1006, maps4453, fixtures и7 TestMap сохранены.
EN/RU2566: один новый ключ на язык, все2565 прежних literal values неизменны.
Core SHA256: `e128613d5109945b34108477927f66e9f4502aaefae04656f584dabcdf1e6e9b`.
Evidence: `artifacts/Evidence/NativeOwnerScopes-20261016`.

Цель ACTIVE; все23 области и очереди A–G сохранены. Текущий source thing crop
и шесть рабочих owner families реализованы. Далее остаются общий
REQUEST_TRIGGERS0x100 между creature/region callers, auxiliary member band
operand (native parent ID versus source physical ordinal), другие native metadata,
неоднозначные imported identities/layouts, compiler/historical/catalogue/
callback/counter coverage и отдельная ручная GUI/audio/dialog/hardware/DPI/game
приёмка. Полный перечень — [REMAINING_WORK](REMAINING_WORK.md). Окна и игра
не запускались. Полный корпус ради документации повторно не запускать.

## SharedScenarioRequests-20261016

16 октября: [общие запросы об использовании в сценариях](SHARED_SCENARIO_REQUESTS.md).
Менеджер удаления/изменения и регионы используют один REQUEST_TRIGGERS0x100
через workspace store. Общие запросы удаления0x2/0x10 независимы. Default-only
remember, reset, cross-policy lease и stale/schema/mutating-input guards входят
в модель. No фильтрует все assigned IDs; all-skipped delete сохраняет parent
Undo при source MapChanges+0. ModifyMulti фиксируется до фильтрации; дочернее
окно получает разрешённый текущий запрос. Старые manager consent checkbox
удалены из маршрута. Выбор фиксируется в display order; перевод сортированных
строк сохраняет selection и снимок, реальная смена выбора отзывает authority.

Auxiliary TRIGGERLINK.band участника теперь сохраняется буквально в D2OS,
DKM/archive, compiler и повторной конверсии. Native parent ID и physical source
ordinal различаются намеренно: простой игровой импорт использует ID, явный
Save точки обновляет ordinal. Пустая/непустая точки, metadata-only Modify,
ID77/ordinal1→0, удаление другого отряда, полный UndoRedo, реальные Save/reopen,
два compiler прохода и обычный семифайловый импорт проверены.

Core manager21 с24 request матрицами, point33 с новыми band-operand проверками,
hero-band24, historical20/compile5, inactive0/1/15/16, insertion8, region focus,
conversion/painting/selection/save/retention/spatial/generation/properties/
identities/cross-callers/official: exit0. Все41 проекта build-final/02/03:
exit0, без предупреждений/ошибок. Последние App-only исправления заново собраны
и опубликованы-03; Core не менялась. Bound WindowsSmoke App/Core совпадают
по SHA. WPF smoke код обновлён и собран; окна, renderer и игра не запускались.

Один полный прогон конечной Core: terminal exit1, **1811 PASS /63 отказа**.
Ordered raw Passed/Failures/Inputs совпали с NativeOwnerScopes полностью;
исходные94 сопоставлены целиком, новых failures/lost passes нет. Аудит63/27
унаследован после проверки входных SHA; установленные карты не правились.
Core SHA256: `7e62361c0ccdbc3552f244b6e7d8fb28c0853ad02f21f0f427b174171984ab4a`.
App SHA256: `2308248335c388b7f1359a551a785488d12ddf0cddcf4c1b448d1fb93d9b0d70`.
Evidence: `artifacts/Evidence/SharedScenarioRequests-20261016`.
Source/runtime ZIP сверены по каждому entry; Source/Binaries1006, maps4453,
fixtures, прежние7 TestMap и fresh official bytes сохранены. EN/RU2573:
семь новых ключей на язык, два прежних текста исправлены явно; остальные2564
literal values неизменны. Полный корпус ради docs/App-only правок не повторялся.

Цель ACTIVE, все23 области и семь очередей сохранены. Общий manager/region
request и auxiliary member operand реализованы; это не закрывает все G/E.
Далее — Delete/Shift+Delete/Ctrl+Delete → WM_KILLTHING →
Mouse_RemoveThingUnderCursor и фактический Canvas/keyboard caller,
прочие команды/Inspector routing, преобразования категорий/детачменты,
Config preference lifetime/persistence; затем прочий native/compiler/
historical/catalogue/callback/counter остаток и отдельная ручная физическая
GUI/audio/system-dialog/hardware/DPI/game приёмка. Системный звук и показ
новых запросов на этом этапе не подтверждены. См. [полный остаток](REMAINING_WORK.md).

## UnderCursorRemoval-20261016

16 октября: [удаление под курсором](UNDER_CURSOR_REMOVAL.md) подключено к
Delete/Shift+Delete/Ctrl+Delete в главном активном окне. Правый клик продолжает
выделять карту. Единственная занятая группа имеет приоритет над preference;
при нескольких группах выбирается предпочтительная либо ничего. Удаляется
вся стопка: creatures physical order, doors перед traps, objects перед specials.
Geometry-only, region pick, активный штрих/выделение, текстовый ввод,
outside Canvas, Alt/Windows и Shift+Ctrl не дают destructive authority.

Source safe question только creatures, общий0x100 с регионами/менеджером;
общего manager0x2 здесь нет. Frozen tile не зависит от движения указателя к
ответу или языка. No сохраняет все assigned UID. Points/events сохраняются.
Каждая удалённая запись даёт свой Undo и MapChanges+1; empty/no-all не создаёт
историю. Полный прежний порядок/цель предметов, выбранный пользователем,
сохранён. Независимая preflight копия и rollback обеих stacks/counters/buffers
не дают частичного отказа; UI refresh публикуется после готовности всей серии.
Observer failure после commit классифицируется как принятие всей серии.

Все41 проекта build-final: exit0, без предупреждений/ошибок. Manager22 groups
с48 occupancy/preference матрицами и crystal per-item Undo, shared requests,
replies/empty points/depth0/1/wrap/overflow/observer/DKM/compiler/seven-file:
exit0. Последняя test-only crystal проверка пересобрана manager-build-02,
manager-focus-02 exit0; Core не менялась. Focus, hero-band24, historical20/
compile5, inactive0/1/15/16, insertion8, point33, conversion/painting/selection/
save/retention/spatial/generation/properties/identities/cross-callers/official/
publish: terminal exit0. WPF keyboard/window harness подготовлен и собран,
его исполнение не заявлено; окна, renderer и игра не запускались.

Один полный прогон конечной Core: exit1, **1811 PASS /63 прежних отказа**.
Raw ordered Passed/Failures/Inputs совпали с SharedScenarioRequests полностью;
исходные94 сопоставлены целиком, новых failures/lost passes нет. Аудит63/27
унаследован после SHA входов; установленные карты не исправлялись.
Core SHA256: `b6670dc51c8c8c4e78fa03afa77f2cb2b7379cf2f0e19c2ec4275ff074a66808`.
App SHA256: `b4c0cea07dda6c14e3268b38b3f90b73e6b993531d30ffcb52551282731e9295`.
Evidence: `artifacts/Evidence/UnderCursorRemoval-20261016`.
Source/runtime ZIP проверены по каждому entry, Source/Binaries1006, maps4453,
fixtures, прежние7 TestMap и fresh official bytes сохранены. EN/RU2576:
три новых ключа на язык, все2573 прежних literal values неизменны.

Цель ACTIVE, все23 области и семь очередей остаются. Этот keyboard/cursor
caller реализован в модели/App; actual hardware/focus/viewport/DPI/audio и
игровая приёмка ещё ручные. Далее — Inspector deletion routing против source
object commands, manager category/signature/point detachment semantics и
полный Config preference lifecycle/persistence. Остальные native metadata,
historical/compiler, catalogue/compound definition/callback/counter и
GUI/system-dialog/game требования не закрыты. См. [остаток](REMAINING_WORK.md).
Не повторять полный корпус без изменения Core или новой причины.

## ManagerCategoryScopes-20261016

16 октября: [смена категории и literal points](MANAGER_CATEGORY_SCOPES.md).
Source manager сохраняет UID и все TRIGGERLINK/event bytes при door→trap,
ordinary→special и special→ordinary. Прежняя точка остаётся в своём scope
без владельца; обратная явная смена категории того же UID восстанавливает
связь. Creature keeper/hero/neutral остаётся scope2. Generic API отдельно
сохраняет прежнюю миграцию/explicit detachment, его guards не ослаблены.
Source child скрывает generic detachment и объясняет сохранение EN/RU.

Native D2OSv2 добавляет только живую identity-only строку trap: Scope0,
Member-1, Band/Root/Present0. Старыеv1 читаются; writer используетv2 только
при наличии таких строк. UID ловушки переносится в DKM/через compiler;
old point остаётся orphan со своим scope/root/ID. Reconnection ограничен
явным переходом door/object категорий; unrelated BE rewrite ничего не чинит.
Неверные версии, live address другого типа, Present/root/band/reserved и
orphan identity-only rows отвергаются. Семифайловый игровой формат не расширен.

Manager23 groups включает24 native/project × empty/nonempty × single/multiple
× category transitions/returns, real DKM/archive/reopen/compiler, retained
literal Events/UID, same-scope signatures, equal trap, allocator safety,
strict metadata и unrelated-rewrite guard. Core point33, under-cursor48,
shared requests24, hero-band24, historical20/compile5, inactive0/1/15/16,
insertion8, focus/integration/official/publish: final05 terminal exit0.
Все41 проекта build-05 exit0, без предупреждений/ошибок. Bound App/Core в
WindowsSmoke совпадают с поставкой по SHA. WPF тесты только собраны;
окна/renderer/игра не запускались.

Первая focus попытка нашла ошибку старого root snapshot в новой фикстуре;
исправлена. Первая полная сборка не прошла из-за internal UInt32 в новом
тесте и пересечения повторной сборки; обе закончились, затем отдельно
подтверждена build-03. Прогон старой DLL отменён до финального отчёта после
сужения reconnection authority; full-superseded.json и все старые логи
сохранены. Итог относится только к build-05/exit codes с тем же Core SHA.

Конечный полный прогон full-05: terminal exit1, **1811 PASS /63 прежних отказа**.
Raw ordered Passed/Failures/Inputs совпали с UnderCursorRemoval целиком;
исходные94 сопоставлены полностью, новых failures/lost passes нет. Аудит63/27
унаследован после SHA входов; установленные карты не правились.
Core SHA256: `c05f15d69ffc722edff0fde839ddf3d0cb604c6d2034b4fae609c80faba99db5`.
App SHA256: `eaa0f65ff0fb8a74b2edb5126b39dbee85fa8e0dae0f54c203b92520dbe0da34`.
Evidence: `artifacts/Evidence/ManagerCategoryScopes-20261016`.
Source/runtime ZIP проверены по каждому entry; Source/Binaries1006, maps4453,
fixtures, прежние7 TestMap и fresh official05 bytes неизменны. EN/RU2577:
один новый ключ на язык, все2576 прежних literal values сохранены.

Цель ACTIVE, все23 области/семь очередей сохранены. Реализована указанная
source category/point семантика; не заявлены весь manager inventory,
single-list membership pathological cases и все imported layouts. Далее —
Inspector deletion routing против source physical commands и остальные Config
request callers. Для реализованных bits2/16/256 подтверждён session-only
lifetime: source Config_SetToDefault обнуляет RequestsFlags, registry хранит
только Recent/SafeFlag/GamePath. Новому запуску не положено помнить эти bits;
записывать их в AppSettings не требуется. Это не закрывает остальные Config
настройки/requests. Далее остальные native/compiler/historical/catalogue/
compound/callback/counter требования и отдельная физическая GUI/audio/system
dialogs/hardware/DPI/official-editor/game приёмка. Полный corpus не повторять
ради docs или без новых изменений Core. См. [REMAINING_WORK](REMAINING_WORK.md).

## InspectorObjectRemoval-20261016

16 октября: [удаление из панели свойств](INSPECTOR_OBJECT_REMOVAL.md).
Физические категории native/DKM поддерживают extended selection. Delete
фиксирует видимый порядок и использует существующий EditorThingRequestPolicy
Remove и общие session bits2/256 менеджера. Первый запрос, used Yes/No/Cancel,
remember, source assigned-ID filtering и parent Undo/per-object counts
не обходятся generic DeleteRecord. Сохранён согласованный полный Undo
порядка предметов/цели кристалла. Mixed physical/raw не удаляет первую строку;
region/single band/other raw guards отдельно сохранены. Pending field commit
останавливает удаление по прежним адресам; selection/document/unload отменяют
старый запрос. EN/RU refresh сохраняет выбранную группу.

Build-02 всех41 проектов exit0, предупреждений/ошибок0. Manager23 и point33
CLI groups и publish exit0. Подготовлен/собран InspectorObjectRemovalSmoke;
WPF/renderer/окна/игра не запускались. Сверка source и DLL всего Core exact:
изменений нет. Полный корпус не повторялся; предыдущие1811 PASS/63 отказа
и исходные94 сопоставлены целиком с ordered raw Inputs/Passed/Failures,
SHA входов/старого аудита63/27 проверены. Это inherited Core evidence,
не физическая проверка нового App route. Тесты не подменяют ручную приёмку.

Core SHA256: `c05f15d69ffc722edff0fde839ddf3d0cb604c6d2034b4fae609c80faba99db5`.
App SHA256: `5e46b1a52dd8a4a07b4d892204135aebde62ad13660e5377ba74c8b797adc447`.
Evidence: `artifacts/Evidence/InspectorObjectRemoval-20261016`.
Поставка/source ZIP сверены по всем entries,41 project graph. Source/Binaries,
fixtures, карты и прежние7 TestMap не менялись. EN/RU2579: два новых ключа,
все2577 прежних literal values сохранены. Экспорт по-прежнему ровно7 файлов.

Цель ACTIVE,23 области/семь очередей сохранены. Дальше сверять остальные
manager single-list membership/команды и Config request callers, затем
остальные native/imported, historical/compiler, catalogue/compound/callback/
counter требования и ручную GUI/audio/system-dialog/hardware/DPI/game приёмку.
Не повторять полный корпус без изменения Core или новой причины.
См. [остаток](REMAINING_WORK.md).

## ScenarioDeletionRequests-20261016

17 октября, продолжение начатого16 октября этапа: [удаление сценариев](SCENARIO_DELETION_REQUESTS.md).
Point/condition/action используют отдельные source request bits0x800/0x200/0x400,
общий workspace EditorRequestPreferences и single-active lease. Default Yes,
No/checked-close/remember, centered questions и reset имеют EN/RU. Обязательный
checkbox перед branch delete заменён вопросом. Предпочтения вне Undo, карты
и AppSettings; действуют между документами, а не между запусками редактора.

Source extent уже был в Core: point удаляет весь AND/OR/next graph и point,
condition — self+AND descendants без своей continuation, action — только
self со splice continuation. Валидаторы выделены для общего Begin/Apply;
generic операции/guards и современный полный Undo сохранены. Source root-action
Unlink ограничен поиском condition/action predecessor; современная корректная
обработка root splice не заменена dangling link. Source direct commands
не создают Undo; современная история сохраняется, как и ранее.

Request snapshot включает revision/fingerprint/owner edge или eventID;
изменение графа, stale/raw/foreign/shared/pen/nested/overflow отвергается.
Commit followed by Changed observer failure остаётся принятым и освобождает
lease. App фиксирует выбранные Incoming/ScopeRoot/identity, а не только ID.
Любая пользовательская смена узла, в том числе shared-parent view, вкладки,
документа/unload отменяет запрос. Восстановление selection при EN/RU
перестроении дерева отдельно помечено и не считается новой целью.

Point model37 groups exit0 (прежние33 плюс четыре новые группы): восемь
native/DKM × empty point/nonempty point/condition/action shapes, exact
extent/continuation, No/checked close/Yes, independent shared bits, reset,
Save/reopen, full game/metadata UndoRedo; отдельные lease/stale/observer guards.
Point-02 исполнялся с тем же конечным Core SHA. После него менялся только App
selection-restore guard. Все41 проекта build-03 exit0, предупреждений/ошибок0;
owners/integrations/focus/official/publish final03 exit0. WindowsSmoke и
EditorScenarioPointUiChecks ретаргетированы и собраны; UI/renderer/игра
не запускались. Физическая работа окон/мыши/keyboard/audio/DPI ещё ручная.

Full-03: terminal exit1,1811 PASS/63 прежних отказа. Полный ordered raw
Inputs/Passed/Failures совпал с UnderCursorRemoval/ManagerCategoryScopes;
исходные94 сопоставлены полностью, новых failures/lost passes нет. Аудит63/27
и его SHA входов проверены. Установленные карты не правились.
Core SHA256: `396ef992c0a961db72b853d436924b11bffe7f200befb67e840ec53bbf185123`.
App SHA256: `ab718bffaf1f0af98bd1899ae47ba10b425b38d4c1eb3bf246167c5b6995a7d2`.
Evidence: `artifacts/Evidence/ScenarioDeletionRequests-20261016`.
Шесть global-definition scope документов уточняют текущую canonical parent
привязку и отделяют её от старых staging/Test текстов; физическая приёмка
и полный catalogue/unit/range inventory не объявлены закрытыми.
Все три build attempts завершились exit0; ранние App restore варианты
не объявлены физически прошедшими UI. Полный корпус запускался только для
конечного Core, логи/exit codes сохранены.

Source/runtime ZIP сверены по каждому entry и canonical41 project graph.
Source/Binaries1006, maps4453, fixtures, прежние7 TestMap и fresh official03
неизменны. EN/RU2586: семь новых ключей; все2579 прежних literal values
сохранены. Игра получает ровно7 файлов, новые request bits не сериализуются.

Цель ACTIVE, все23 области/семь очередей сохранены. Остались прочие Config
request callers, manager branches/single-list membership, native/imported
metadata и historical/compiler/catalogue/compound/callback/counter требования,
а также фактическая GUI/audio/system-dialog/hardware/DPI/official-editor/game
приёмка. Не повторять полный корпус ради документации или без новой причины.
См. [остаток](REMAINING_WORK.md).

## BigMapRequests-20261017

17 октября: [общий вопрос о большой карте](BIG_MAP_REQUESTS.md).
New Map и Map Properties используют общий REQUEST_BIGMAP0x20 и workspace
EditorRequestPreferences, single-active lease, default Yes, No/checked close/
remember/reset. Original informational icon и follow-mouse placement с
рабочей областью монитора. New form больше не требует обязательный checkbox:
валидный OK открывает вопрос; No оставляет форму. Crop consent отдельно.
Source threshold — площадь>14400, для resize только изменённые размеры.
Современные New Map1024/дополнительный>300 notice сохранены как расширение.

Approval не создаёт/не меняет карту или её Undo. Запомненный ответ общий
между new/resize/документами, вне Undo/AppSettings и только до нового запуска.
App сохраняет revision/settings/caller, Core resize request — live/working
plan/fingerprint и закрытие parent. Stale/raw/working/pen и callbacks,
закрывшие/сбросившие запрос, не дают старое разрешение и не крадут lease.
Current working/source map и семифайловый compiler формат не менялись.

Build-01 всех41 проектов exit0, предупреждений/ошибок0. Focus map-properties
exit0:26 source/new-extension matrices, shared reply в обе стороны, No/checked
close/reset, approval-only history, Apply/UndoRedo и stale/raw/working/lease
guards. PEReader проверил original Information32.ico внутри App DLL побайтно,
без Windows/WPF/renderer execution. Point37, manager23, owners/integrations/
focus/official/publish exit0 с тем же конечным Core SHA. WindowsSmoke новой
карты/общего вопроса обновлён и собран; окна/звук/игра не запускались.

Full-01 terminal exit1:1811 PASS/63 прежних отказа. Ordered raw Inputs/Passed/
Failures совпали с прежним отчётом целиком; исходные94 сопоставлены полностью,
новых failures/lost passes нет. Аудит63/27 и SHA его входов проверены.
Core SHA256: `1e4bc25f55338bc724a2da97ed60b32299704ca926e7e7ccb8c6ffcca8cae17f`.
App SHA256: `0a729a2a9711ae5360e0de990dd8052586f05fe4df6668a2155c512a2f007863`.
Evidence: `artifacts/Evidence/BigMapRequests-20261017`.
Source/runtime ZIP проверены по всем entries и canonical41 project graph;
Source/Binaries1006, maps4453, fixtures, прежние7 TestMap и fresh official01
неизменны. EN/RU2591: пять новых ключей, все2586 прежних literal values
сохранены. Никаких новых игровых файлов.

Описание новой карты обновлено: source question вместо старого checkbox;
устаревшая фраза об отсутствующей работе с project global overrides заменена
текущим lazy-family/reset поведением с сохранённой физической приёмкой.
Цель ACTIVE, все23 области/семь очередей сохранены. Остальные Config request
callers/manager branches, native/imported/historical/compiler, catalogue/
compound/callback/counter и GUI/audio/system-dialog/hardware/DPI/official/game
требования остаются в [остатке](REMAINING_WORK.md). Не повторять полный корпус
ради документации или без новой причины.

## PlayerGoldNotice-20261017

17 октября: информационное OK-only предупреждение о стартовом золоте
Хранителей реализовано по source REQUEST_MAPGOLD0x40. Закрытие сохраняет
актуальные параметры без ограничения золота; только OK запоминает флажок.
Память общая с вложенным workspace, вне Undo и файлов карты. Вложенная запись
остаётся приватной, вместимость считается по исходной живой карте; оба
контекста защищены revision/fingerprint. Прежние inline statistics и широкий
расчёт статистики сохранены; предупреждение использует source signed32.

Final build03: все41 проекта, ноль предупреждений/ошибок; MapFocus03 exit0,
28 native/project player/capacity вариантов и два hosted-context случая.
Point37, manager23, owners/historical/conversion, полный integration tail,
region focus, official seven-file export и publish: terminal0 одной текущей DLL.
Full03: terminal1,1811PASS/63FAIL; Inputs/Passed/Failures буквально совпадают
с предыдущим полным отчётом по порядку. Исходные94 сопоставлены полностью:
31 ранее исправлен, одно известное изменение XXLevel14, новых отказов и
потерянных passes нет. Audit63/27 проверен по текущим input/resolver hashes.
Corpus остаётся НЕ зелёным. Build01 завершился1 из-за ошибочного имени API
в новой проверке; тест исправлен без ослабления product validation. Build02
и MapFocus02 успешны, затем добавлен live/private контекст; final03 проверяет
итоговый код. Все логи сохранены.

13 product-файлов и шесть test-файлов изменены относительно прошлой поставки.
EN/RU2598: семь новых ключей, все2591 прежних literal values сохранены.
Сборка, source/runtime ZIP, текущая документация, graph41, каждый файл архивов,
Source/Binaries1006, installed maps4453, fixtures и неизменные семь файлов
TestMap сверены. Доказательства: artifacts/Evidence/PlayerGoldNotice-20261017.
WindowsSmoke только собран; GUI/renderer/audio/game не запускались.

Подробное поведение и ручной сценарий: [PLAYER_GOLD_NOTICE](PLAYER_GOLD_NOTICE.md).
Пользователь повторно подтвердил современный полный Undo удаления предметов:
порядок и цель кристалла восстанавливаются. Все23 области/семь очередей и цель
ACTIVE сохранены в [остатке](REMAINING_WORK.md). Физическая приёмка,
native/imported/historical/compiler, catalogue/compound/callback/counter и
прочие manager/request callers остаются в работе. Полный корпус не повторять
из-за документации или без новой причины.

## MapNameOverflow-20261017

17 октября: source REQUEST_BUFOVERFLOW0x4 реализован в общих свойствах.
При имени длиннее64 — Copy/Keep/Cancel; Copy берёт до63, Keep сохраняет имя
черновика и остальные настройки, Cancel не записывает общие поля даже в
приватный документ. Исходный случай ровно64 →63 без вопроса сохранён.
Generic UpdateMapGeneralSettings не ослаблен: новое64 отклоняется, прежнее
импортированное полное поле и padding сохраняются при другом изменении.
Native Unicode/RU сохранён, surrogate pair на границе не разрывается.

Имя проверяется до записи вместе с размерами/прочими общими параметрами.
Frozen plan связан с live/working session, ревизиями и fingerprints обоих
документов; pen/foreign/closed/stale и дочерние/языковые изменения не дают
старому ответу применить данные. Request0x4 использует общий session store/lease;
только Copy запоминается, reset очищает только0x4, Undo и outerCancel память
не сбрасывают. Запрос размера и crop остаются отдельными.

Build03:41 проектов, ноль предупреждений/ошибок. MapFocus02 exit0 проверяет
тот же final Core:18 name matrices, исторические v1–v7 и lifetime/Unicode/
generic guards, прежние gold28/two hosted и big-map26. После него изменена
только UI обработка IOException доставки; build03 связывает текущие App и
prepared WindowsSmoke, повторный Core target не запускался. Build01 завершился1
из-за ошибочного доступа App к внутреннему IsSaveLocked; исправлен Core session
API, лог сохранён. Build02 был успешен. Point37, manager23, owners/historical/
conversion, integration tail, region focus, official и publish03 terminal0.

Full03: terminal1,1811PASS/63FAIL. Полные ordered raw Inputs/Passed/Failures
совпадают с предыдущими UnderCursor/ManagerCategory/BigMap/PlayerGold отчётами.
Исходные94 сопоставлены полностью:31 прежний отказ исправлен, одно известное
изменение XXLevel14, новых отказов/потерянных passes нет. Audit63/27 проверен
по input/resolver hashes. Corpus НЕ зелёный; отчёт не фильтровался.

Пять product-файлов и четыре test-файла изменены относительно прошлой поставки.
EN/RU2603: пять новых ключей, все2598 прежних literal values сохранены.
Source/runtime ZIP, graph41 и каждый файл архивов проверены; Source/Binaries1006,
installed maps4453, fixtures и прежние семь TestMap/fresh official03 неизменны.
Evidence: artifacts/Evidence/MapNameOverflow-20261017.
Редактор, WPF renderer, игра и звук не запускались; WindowsSmoke только собран.

Подробности и ручной сценарий: [MAP_NAME_OVERFLOW](MAP_NAME_OVERFLOW.md).
Выбранные современные Undo сохранены. Цель ACTIVE; все23 области и семь
очередей, native/imported/historical/compiler, catalogue/compound/callback/
counter/manager и физическая GUI/audio/system-dialog/hardware/DPI/game приёмка
остаются в [REMAINING_WORK](REMAINING_WORK.md).

## HistoryFailureAtomicity-20261017

17 октября: исправлена атомарность отказов истории в текущем Core.
Исходный reproduce01 показал четыре сбоя native/DKM: lock после ошибки
подготовки снимков и изменённые клетки после отказа завершения штриха.
Теперь подготовка root snapshots предшествует lock; метаданные, стоимость,
trim и capacity готовятся до изменения стеков. Undo/Redo готовят все файлы
до их замены. Отказ завершения штриха возвращает клетки/EditorChangeCount,
сохраняет старую Redo и снимает захват; опубликованные paint activity/revision
не откатываются и не оживляют stale drafts. Postcommit observer error остаётся
postcommit, повторный Dispose не создаёт ещё один шаг.

Drawing focus02 terminal0: четыре исходных регрессии и108 безопасно
имитированных отказов snapshot/metadata в root/nested/replay, плюс pen
reservation/close, revision overflow, observer, recovery/retry и Save/reopen.
Никакого реального OOM не создавалось. Прежняя drawing-history модель прошла.
Build03:41 проекта, ноль предупреждений/ошибок. Map-properties03 (name18,
gold28/two hosted, big-map26), Point37, manager23, owners/historical/conversion,
integration tail, region focus, official и publish03: terminal0 этой DLL.

Full03: terminal1,1811PASS/63FAIL; полный raw ordered Inputs/Passed/Failures
буквально совпал с предыдущими отчётами, включая MapNameOverflow. Исходные94
сопоставлены полностью,31 исправлен раньше, одно известное изменение XXLevel14;
новых отказов/потерянных passes нет. Audit63/27 проверен по input/resolver hashes.
Corpus НЕ зелёный, отчёт не фильтровался.

Три product Core-файла и два test-файла изменены. Публичный API редактирования,
форматы/компилятор и EN/RU2603 не изменялись; все2603 literal values прежние.
Source/runtime ZIP, каждый entry, graph41, Source/Binaries1006, installed maps4453,
fixtures и прежние7 TestMap/fresh official03 проверены. Evidence:
artifacts/Evidence/HistoryFailureAtomicity-20261017. Сборка и документация обновлены.

Шесть сценарных документов больше не называют текущий продукт frozen/staging
или старые проверки живыми workers: описанные alias/camera/spawn/spatial/speech
маршруты находятся в общем src; исторические артефакты отделены от текущего
статуса. Физическая приёмка этих окон от этого не становится выполненной.

GUI/renderer/audio/game не запускались. Source error request UI0x8/0x20000,
варианты продолжения без child Undo и частичного source commit не объявлены
закрытыми; они остаются в E/G. Выбранный полный современный Undo сохранён.
Все23 области и семь очередей, compiler/native/historical/catalogue/compound/
callback/counter и физическая приёмка остаются в [REMAINING_WORK](REMAINING_WORK.md).
Подробности: [HISTORY_FAILURE_ATOMICITY](HISTORY_FAILURE_ATOMICITY.md).

## ScenarioChoiceCaptions-20261017

17 октября: уточнены EN/RU подписи семи radio-контекстов сценариев и Built
для дверей/ловушек. Порталы Open/Close, game messages, spoken countdown,
cinematic view и бег теперь названы конкретно. Attack choice следует radio
«атакует/не атакует»; исходные aggressive/passive описания сохранены отдельно.
Коды и порядок100 native/DKM choice fields до/после буквально совпали;
текст изменён в18 полях. В EN/RU2603 по шесть existing values изменены,
прочие2597 и набор ключей сохранены. Остальной src/tests не менялся.

Build01:41 проекта, ноль предупреждений/ошибок. Current parameters/descriptions,
fresh official seven-file export и publish terminal0. Full corpus не повторялся:
последний full1811/63 с исходным94 сравнением относится к предыдущему
HistoryFailureAtomicity Core, как указано в отчёте этого этапа. Inherited
результат не выдан за новый full текущей DLL. Writer/compiler/bounds и весь
прочий код/тесты проверены по прежним SHA, numeric projection — по runtime
снимкам до/после. Evidence: artifacts/Evidence/ScenarioChoiceCaptions-20261017.
Новых тестов для текстовой правки не добавлялось. Диагностическая утилита
в Evidence только читает in-memory choices и не входит в поставку/graph41.

Source/runtime ZIP и каждый entry проверены, Source/Binaries1006,
installed maps4453, fixtures и семь TestMap/fresh official неизменны.
GUI/renderer/audio/game не запускались. Все23 области и семь очередей,
остальные catalogue/compound/callback, source error UI и физическая приёмка
остаются в [REMAINING_WORK](REMAINING_WORK.md).
[Подробности](SCENARIO_CHOICE_CAPTIONS.md).

## ScenarioEntityCaptions-20261017

Имена/комментарии областей, эффектов и проектных ворот показаны в контекстных
списках/inspector и выборе области появления/смены клетки. RectEx CR/LF/TAB
обрабатываются только для отображения. Отряды сохраняют буквальное имя.
Специальные ворота уже показывали info; их тексты теперь однострочные.
Смена языка использует прежний snapshot областей и сохраняет ID под refreshing.

Build02:41 проекта,0 warnings/errors; parameters/spatial/spawn/official/publish
terminal0. Numeric/order138 fields exact; подписи изменились в18
полях только проектного формата. Карта при чтении не менялась. EN/RU2603
ключа и все literal values, прочие src/tests/writers/compiler/bounds неизменны.
Core SHA: `8d88be9a2387f3921ed4f4bf73caf5f4db356424c98d182a36f5b6a1bda3f8fd`. App SHA: `b4e256f3dc3c7591cc9f519d737dc1b4fb66f43ab6f26fd42a77dc34f4ec4714`.

Full corpus не повторялся:1811/63 и original94 comparison принадлежат
HistoryFailureAtomicity Core f527…; проверены inherited inputs/resolver SHA.
ZIP/каждый entry/graph41, Source/Binaries1006, maps4453, fixtures, старые7
TestMap и свежий official проверены. GUI/renderer/audio/game не запускались.
Все23/семь очередей и ручная приёмка остаются открытыми по своему ledger.
[Подробности](SCENARIO_ENTITY_CAPTIONS.md). Evidence: ScenarioEntityCaptions-20261017.

## RequestSourceAudit-20261017

Сверены все18 source REQUEST bits:17 имеют активные calls, AREASIZE0x80
не используется.12 маршрутов уже присутствуют (здесь не пересертифицированы);
четыре конкретных request routes отсутствуют: Undo error0x8, manager recovery
0x20000, generator capacity0x4000 и generator confirmation0x8000. Генерация
сама реализована, её текущий checkbox не заменяет remember/reset questions.
Отдельно COPY remembered answer сейчас попадает в постоянную настройку.
Конечные задачи этой группы R01–R05 и точная цепочка source Undo child failure
→ destroy parent → возможный restore предыдущего entry записаны в
[SOURCE_REQUEST_AUDIT](SOURCE_REQUEST_AUDIT.md). Это source inference, не запуск EXE.

Код, Core/App и EN/RU не изменялись; build/corpus/GUI не повторялись.
Текущий продукт — ScenarioEntityCaptions-20261017, поставка дополнена
документацией. Общая цель, все23/семь очередей и остальные completion gates
сохраняются. Выбор recovery policy задан пользователю, текущий полный
rollback/Undo пока сохранён. Evidence: RequestSourceAudit-20261017.

## CopyConfirmationLifetime-20261017

R05 исправлен: explicit menu preference сохраняется в JSON, remembered Yes
только в текущем workspace. Меню показывает effective state; его включение
сбрасывает suppression. Copy/Cut/rotated/DKZ guards и nonmodal APIs прежние.
Три product/три test files, EN/RU2603/all literals и AppSettings schema прежние.
Build02:41 проектов,0 warnings/errors; state focus/official/publish terminal0.
WPF smoke расширен и собран, но не запускался.

Current full terminal1:1812PASS/63FAIL. Все1811 предыдущих passes raw/order
сохранены; добавлена одна модель срока действия. Inputs/63 failures exact,
original94 comparison31 fixed/one known XXLevel14 message; audit63/27 verified.
Core `69f47ca64205c9906a77a7f74a32751b2e5847960474c7fe31dae594e9bf4c89`; App `a3136efbb931d53ed32c37be977d7e6cc4f42d038908a772fd7bc4f56aeb0ab6`.
ZIP/every entry/graph41, Source/Binaries1006/maps4453/fixtures/7TestMap/fresh
official проверены. GUI/renderer/audio/game не запускались. R01–R04, все23/
семь очередей и physical gates остаются. [Подробности](COPY_CONFIRMATION_LIFETIME.md).

## GateGenerationRequests-20261017

R03/R04: source Yes/No0x8000 и capacity OK0x4000 теперь используются workspace
и nested generator с общими session flags/reset. Checkbox заменён вопросом;
capacity preflight различает areas/gates/both до Allocate и никогда не даёт
разрешения. Answers read-only/outside Undo; private parent composition и
counter semantics прежние. Revision/raw/owner/pen/transaction и replay-child
guards проверены, unload/reset-document закрывают запрос.13 product/3 test
files; EN/RU2613, десять новых keys/язык, прежние2603 literals сохранены.

Build03 all41 zero; focus02/generator/nested-generator/official/publish exit0.
Current full exit1:1813PASS/63FAIL;1812 old passes raw/order retained, один
новый group, Inputs/63 failures exact. Original94:31 fixed и one known
XXLevel14 message, input/resolver audit63/27 verified. Core `e9d4e0462ec9f200814d3dea627cb85cae6bc207e9b09aafb2f8387d2ef09bad`;
App `cd2fd0f65b34e23da2da6016dd10afe2a334749ce4d12ff550e1ffefdd4774cc`. Source/Binaries/maps/fixtures/7TestMap/fresh official,
ZIP/each entry/graph41/docs проверены. WPF только compiled; GUI/game/audio
не запускались. R01/R02 и все23/семь очередей/physical gates остаются.
[Подробности](GATE_GENERATION_REQUESTS.md).

## HistoryMemoryNotice-20261017

R01: помечены только OOM подготовки истории; post-unwind Dispatcher route
показывает source informational0x8/OK/default-only remember/reset EN/RU.
Общий action/observer OOM не переклассифицируется. Secondary observer history
failure оставляет уже committed edit и Undo. Notification lease не отнимает
normal confirmation и не одобряет её. Pen capture и failed patrol dialogs
очищаются; accepted CommitFailure semantics сохранены. Для R02 пользователь
выбрал полный откат и понятную ошибку; частичное применение/продолжение без
Undo исключены.12 дополнительных отказов staged manager batches прошли.

Build04 all41 zero; focus01/history03/official/publish02 exit0.4 прежних
failure regressions/108 safe injected boundaries и новые classification cases
прошли. WPF только compiled. Current Core full exit1:1814PASS/63FAIL,
1813 previous passes/order, один новый group, Inputs/63 failures exact;
original94/audit63 input SHA verified. Core `5079c00b06b555c9eb71ad5d075131bab93b22de1432caab4b2bc130cf397a2c`;
App `3aa8e284a010d0aee9f70fc4cc599ceae4aa14cd5b621ffbb17587d3d579c5a2`. Поздняя App-only patrol cleanup не меняла Core/full.
EN/RU2619/6new/2613old literals. ZIP/every entry/graph41, Source/Binaries1006,
maps4453/fixtures/7TestMap/fresh official проверены. GUI/real OOM/game не запускались.
Все23/семь очередей/physical gates остаются. [Подробности](HISTORY_MEMORY_NOTICE.md).

## OpenFunctions-20261017

Актуальный конечный перечень: [OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md) —
18 пунктов «исправить» (одно установленное отличие,17 незакрытых сверок полноты)
и23 группы «проверить вручную». Это41 пункт работы, не41 дефект. Сохраняет
все23 возможности; успешные проверки без изменений не повторяются.
Перечень заменяет широкие очереди как управляющий список, но не сокращает
FEATURE_PARITY. Только документация: продуктовые исходники, DLL, фикстуры
и карты не менялись, build/corpus/WPF/game не запускались. Последний полный
результат остаётся1814/63 от HistoryMemoryNotice. Не объявлять17 сверок
найденными ошибками и не выдавать I-пункты за уже закрытые ручные проверки.

## RecentFileCommands-20261017

Внутри I17 реализованы исходные команды очистки недавних файлов и удаления
недоступных записей; default menu label теперь basename, полный путь остаётся
tooltip/аргументом открытия. Source Config_RemoveObsolete probe access0/share3/
OPEN_EXISTING/SEQUENTIAL_SCAN повторён через закрываемый SafeFileHandle.
Только recent history/JSON; карты, Undo и LastOpenedFile не изменяются.
EN/RU2625 keys,6 новых на язык;2619 прежних literal values сохранены.
Все41 build/publish exit0, zero warnings/errors. Core `5079c00b06b555c9eb71ad5d075131bab93b22de1432caab4b2bc130cf397a2c` неизменна;
App `26800d62128e876879d1c1a8007f5d07d27c3f3818088ccdb6f272e10ea7d3e0`. Полный corpus не повторялся: последний1814/63 на этой Core
от HistoryMemoryNotice; это не новый full результат. GUI/game не запускались.
[Поведение и ручные шаги M20](RECENT_FILE_COMMANDS.md). I17 закрыт только в
этих трёх маршрутах; остальная сверка и все требуемые manual gates сохраняются.

## ManagerCommandHistory-20261017

Внутри I17 добавлены Add и локальный Undo самого менеджера; double-click
строки использует общий Modify/request route. Точный root ownership исключает
отмену чужой правки/шага до открытия; scope живёт один сеанс окна. Allocation
failure сохраняет retry, postcommit observers не теряют выполненный шаг.
Native/project CLI прошли; EN/RU WPF4 случая только compiled.
Build04 all41/publish02 exit0, zero warnings/errors. Full current Core:
1815PASS/63FAIL exit1; прежние1814 passes/order,63 failures и Inputs exact,
original94/audit63 hashes проверены. Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`;
App `9f28e7c41639b667665bfddf91dc8238ec84962033d3b5d1f4f42805ed716e77`. EN/RU2626/1new/2625old literals. Late App-only cleanup
не меняла Core; corpus повторно не запускался. GUI/game не запускались.
[Функции и ручные шаги M20](OBJECT_MANAGER_COMMANDS.md). I17 остаётся открыт:
CenterView/Show destination/ChoosePoint и прочие options/Config branches.
Все остальные пункты OPEN_FUNCTIONS_RU и physical gates сохраняются.

## ManagerNavigation-20261017

В I17 реализованы CenterView одного объекта, Show destination X/Y и
ChoosePoint: private repeated click preview, source OK/Cancel/RMB, shared
session coordinates; canvas/viewport/cursor, окно возврата и queued modal
restore. Revision/token/owner/close guards и очистка подписок предусмотрены.
[Поведение и ручные шаги M20](OBJECT_MANAGER_NAVIGATION.md).
Build03 all41/publish01 exit0, zero warnings/errors; WPF4 случая только compiled.
App `0ee675093dd7f604fcc01d730ba340408b23aff29fd47bbdecbc4e877aa5d725`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa` неизменна. Последний full1815/63 — предыдущий
ManagerCommandHistory на этой же Core, не новый запуск. Успешные CLI/corpus/
official проверки без изменения Core не повторялись. EN/RU2635/9new/2626old.
GUI/editor/game не запускались. Эти три code routes переведены в M20;
I17 остаётся открыт по options/preview/прочим Config, все другие пункты и
physical gates сохраняются. Пакеты/документация обновлены, карты не записывались.

## ManagerOptionsDialogs-20261017

В I17 подключены отдельные AddOptions/ModifyOptions: private draft, Apply-only
запись runtime preferences, source семейные поля/hidden bits, EN/RU и guarded
Cancel/close/document lifetime. [Окна и M20](OBJECT_MANAGER_OPTIONS.md).
Build01 all41/publish01 exit0, zero warnings/errors. Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa` неизменна;
App `60f482784d1fe650b6a87c31382e68079529b7eb620c52326e600fe4d96ec750`. EN/RU2641/6new/2635old literals. Tests/ не менялись; новые
повторяющие реализацию tests и повтор corpus/CLI/official не выполнялись.
Последний full1815/63 остаётся от ManagerCommandHistory на той же Core.
GUI/editor/game не запускались; окна требуют ручной приёмки.
I17 остаётся открыт: AskOptions/Preview execution sequencing, direct template
path и on-map/selection Add coupling, прочие Config branches. Эти параметры
пока не выданы за полную source flow parity. Остальные I/M/gates сохранены.

## ManagerExecutionFlow-20261017

Внутри I17 подключён фактический запуск Add/Modify: независимые AskOptions и
Preview, исходное число выбранных строк до фильтрации, single properties с X/Y,
общий шаблон и rectangle/selection scope. Неразрешённые предупреждения и
replacement Ask требуют решений; прямое применение использует общие Core-планы,
guards и локальный Undo. [Поведение и ручная M20](OBJECT_MANAGER_EXECUTION_FLOW.md).
Build02 all41/publish01 exit0, zero warnings/errors. Focus01:24 исхода source
options/preview policy, без WPF. EN/RU2647 keys,6 новых,2641 прежних literals
сохранены. Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa` неизменна;
App `47452db7f7b15fab90b8e71a43675e6c23281790ba92ea8e58abf30b1b289a48`. Full/corpus/official не повторялись:
последний full1815/63 от ManagerCommandHistory на этой же Core. Это не новый
1816 full result. WPF4 EN/RU/native/project последовательности только compiled.
Source/Binaries/fixtures и прежние7TestMap bytes сохранены; GUI/game не запускались.
Пакеты обновлены из единого текущего проекта. I17 прочие команды/Config и
все остальные I/M пункты остаются открытыми; вся цель ещё не завершена.

## ViewVisibilityCommands-20261017

Внутри I17 исправлен пропуск IDM_TOOLBAR: «Вид → Панели инструментов» скрывает
полосу команд и инструменты карты. IDM_MINIMAP доступен из того же меню;
его отметка синхронизируется с прежним флажком workspace. Предпочтения действуют
в текущем окне, по исходным Toolbar_Hide/Minimap_Hide, без записи карты/Undo.
[Поведение и ручная M21/M23](VIEW_VISIBILITY_COMMANDS.md).
Build01 all41/publish01 exit0, zero warnings/errors. App `bb7f4a4a3305782dab3424a9f6082b9e7212351c0e3c61177bbc2d8c2dc9669a`;
Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa` неизменна. EN/RU2648 keys:
один новый ключ,2647 прежних literals сохранены. Tests/ не менялись; новые
повторяющие реализацию tests и повтор CLI/corpus/official не выполнялись.
Последний полный1815/63 — ManagerCommandHistory на этой же Core.
Source/Binaries/fixtures/7TestMap bytes сохранены. GUI/game не запускались.
Дополнительно сохранён lexical inventory42 CONFIG fields/337 refs/37 files;
он не доказывает semantic closure остальных полей. Следующая работа I17 —
их конечное сопоставление, без повторной реализации закрытых команд.
I14 ожидает уже заданный пользователю выбор порядка Undo регионов;
повторный вопрос не нужен. Остальные I/M и вся цель остаются открытыми.

## RegionUndoConfigScope-20261017

I14 закрыт явным ответом пользователя: полное восстановление прежнего места
и состояния областей, проходов и генераторов. Код уже делает это через
Before/After replay; имитация source List_AddEntry в конец не требуется.
[Решение](REGION_UNDO_POLICY.md). Остаток:17 кодовых пунктов и23 manual группы.
[Сопоставление42 CONFIG fields](CONFIG_SOURCE_MAPPING.md) заменяет неопределённые
«прочие настройки» конечной таблицей. Конкретно отсутствуют Sound_Play callers
для15 штатных WAV и source default title; дополнительные геометрия/пути/report
маршруты явно оставлены на сверку. Следующий кодовый объём — звуки интерфейса
и рисования, не повтор уже закрытых команд. I17 целиком не закрыт.
Только решение/чтение кода/документация/упаковка: src/tests/Core/App неизменны,
новых build/publish/CLI/corpus/WPF/game нет. Поставка приложения остаётся
ViewVisibilityCommands, последний full1815/63 на прежней Core сохраняется.
Source/Binaries/fixtures и прежние7TestMap bytes сохранены. Вся цель активна.

## EditorSounds-20261017

Внутри I17 подключены Sound_Play callers: выбор инструмента/terrain, pen start,
shape definition/completion и object brush.15 исходных WAV публикуются рядом
с EXE, bytes/RIFF PCM сверены. Categories interface/terrain и source−500
centibel volume сохранены; один player на sample перезапускается независимо
от остальных. Unloaded закрывает players; load/language/sample rebinding тихие.
[Правила и ручная M14/M21/M23](EDITOR_SOUNDS.md).
Build02 all41/publish01 exit0, zero warnings/errors. Focus01:42 исхода чистой
политики,15 имён WAV,3 громкости — PASS; без WPF/медиасистемы/устройства.
App `0831c7252f750f6d3855224722f780a4fc63f9bd7f6003911391956df800d061`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2648 keys/literals unchanged. Corpus/старые CLI/official не
повторялись; последний full1815/63 от ManagerCommandHistory на этой же Core.
Новые фокусные группы не считаются новым полным запуском. Source/Binaries/
fixtures/прежние7TestMap bytes сохранены. GUI/game/audio не запускались.
Кодовый пропуск SoundsVolume/SoundsFlags снят в указанном scope; слышимость,
latency/mixing/device/close остаются ручной приёмкой. I17 default title,
геометрия/пути/report и прочие manager callers открыты. I14 остаётся закрытым
выбранным полным Undo;17 кодовых пунктов/23 manual группы и вся цель сохранены.
Следующий конкретный кодовый маршрут — source default title через внутреннее
имя карты с обновлением после Rename/Undo, без полного graph scan на refresh.

## WindowMapTitle-20261017

Внутри I17 исправлен source default title: основное окно использует внутреннее
имя карты, не basename Save As. EditorWindowCaption читает проверенный native
GAMEINFO либо единственную текущую KINF layouts1–7 через существующие schemas;
не вызывает Records/CreateMapGeneralDraft/fingerprint. Rename/Undo видят живые
bytes; неизвестный/неполный header даёт fallback к filename. Dirty/recovery и
EN/RU app caption сохранены. [Поведение и ручная M03/M14/M23](WINDOW_MAP_TITLE.md).
Build01 all41/publish01 exit0, zero warnings/errors. Focus01 — PASS: native/
project live Rename/Undo, read-neutral bytes/history, Unicode,7 old layouts,
full-width/empty names и malformed/future bounds. Без WPF запуска.
App `916050b83ffafd03c975a3e65761169904939cc681aa9253d77c36ac7d80b230`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2648 keys/literals unchanged. Corpus/старые CLI/official не
повторялись; последний full1815/63 от ManagerCommandHistory на этой же Core.
Новые фокусные группы не являются новым full. SaveLifecycle WPF assertion
обновлён под internal title, только compiled.15 WAV bytes и Source/Binaries/
fixtures/прежние7TestMap сохранены. GUI/game/audio не запускались.
UseFilename/ShowFullName default route закрыт; I17 геометрия/пути/report и
прочие manager callers открыты. Следующий объём — исходные report callers:
Load/Save/Compile, ограничения severity/version и принятие/отмена отчёта.
I14 закрыт согласованным полным Undo;17 кодовых пунктов/23 manual группы и
вся цель сохраняются, стадия не выдаётся за полное соответствие.

## DocumentReports-20261017

Внутри I17 подключены полные Load/Test/Compile/Run reports вместо усечённого
MessageBox; список не ограничен12 строками и не объединяет одинаковые codes.
Выбор области перемещает viewport/cursor и временную подсветку без правки
карты/выделения; close восстанавливает прежний highlight того же документа.
Load/Test close-only; errors не разрешают Continue; stale/owner/revision/plan
guards сохранены. Default Close/Cancel, включая Save consent.
Source SaveReportFlags Version1/Warnings2/Alerts4/Infos8/default14 подключены
для дополнительных version notices в существующий SaveConfirmationWindow;
loss/blocking/native-source/archive consent не обходятся, autosave не менялся.
[Объём, исходник и ручные шаги](DOCUMENT_REPORTS.md).
Build03 all41/publish02 exit0, zero warnings/errors. Focus01:24 filtering/
continuation/region исхода,25 ordered rows — PASS, без WPF. WPF4 EN/RU×native/
project cases только compiled; реальные Root dialogs/отмена до writer не приняты.
App `6ad06973bb5f01b733aa989b178da5ce17f78ae39f9dbb883e3f6b5d4b79be3e`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2662 keys,14 новых на язык,2648 прежних literals сохранены.
Corpus/старые CLI/official не повторялись; последний full1815/63 на этой Core
от ManagerCommandHistory. Новые фокусные группы не являются новым full.
Source/Binaries/fixtures/15WAV/прежние7TestMap bytes сохранены; GUI/game/audio
не запускались. I02 полнота всех compatibility generators остаётся открыта;
I17 прочие manager/геометрия/пути остаются.17 code/23 manual и вся цель сохранены.
Следующая ограниченная работа — независимые MapPath/AreaPath/ThumbPath callers
из CONFIG_SOURCE_MAPPING, без повторной реализации закрытых report routes.

## FileDialogPaths-20261017

Внутри I17 подключены независимые MapPath/AreaPath/ThumbPath scopes:
working map следует принятому EditorSaveSession target, AREA/BMP запоминаются
после OK chooser перед IO, включая последующий отказ. Cancel не сохраняет
новое имя. AREA/BMP общие для своих дочерних окон и переживают смену карты;
не добавлены в JSON/Undo. Старый LastOpenedFile fallback каталога сохранён.
Исправлен BMP UI overwrite: после native consent подготовленные bytes пишутся
в staging и заменяют target; generic SaveTo/SaveNewBmp остаётся new-only.
Source/revision/options guards и EN/RU stale message добавлены.
[Поведение и ручные шаги](FILE_DIALOG_PATHS.md).
Build02 all41/publish02 exit0, zero warnings/errors. Focus02 — PASS: scope
independence/failed AREA path/new-native reset, new-only protection, bad BMP,
partial write/flush failures и exact new/replacement publication, private temp
only. Focus01 failed InvalidDataException assumption сохранён; отдельный catch
исправлен и в UI, и в check. WPF/native dialogs не запускались.
App `7899b03a8e2ee905e3a60c16d9a4f03e5d0bf11eff5ecc579743f5f61ab796fe`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2663 keys,1 новый,2662 прежних literals сохранены.
Corpus/старые CLI/official не повторялись; последний full1815/63 на этой Core
от ManagerCommandHistory. Новый focus не является новым full.
Source/Binaries/fixtures/15WAV/прежние7TestMap bytes сохранены; GUI/game/audio
не запускались. I17 scopes этих трёх путей закрыты по коду; native dialogs
M03/M04/M07/M21/M23 остаются. I17 геометрия и прочие manager callers открыты;
I02 compatibility completeness и все17 code/23 manual группы сохраняются.
Следующая ограниченная работа — служебные окна compiler/thumbnail:
геометрия, повторное открытие и source lifetime из CONFIG_SOURCE_MAPPING.

## ThumbnailWindowLifetime-20261017

Внутри I17 исправлены lifetime и положение миниатюры: при смене карты окно
остаётся открытым, переключает источник и сохраняет preferences; старый
документ отписан. Callback пропорций работает после переключения. Закрытие
документа закрывает окно и запрещает повторную привязку/экспорт. Ошибка
построения очищает старое изображение и отключает экспорт с сообщением EN/RU.
ThumbnailWindowX/Y сохраняются в памяти workspace; первое положение у правого
края owner, восстановление ограничено рабочей областью ближайшего монитора.
Размер не сохраняется, как в source. JSON/карта/Undo не меняются.
[Поведение и ручные шаги](THUMBNAIL_WINDOW_LIFETIME.md).
Build02 all41 и publish01 exit0, zero warnings/errors. После build01 уточнён
probe отписки: изменение старой карты обязательно реально меняет клетку.
WindowsSmoke расширен
и только скомпилирован; GUI/native HWND/DPI не запускались. CLI/corpus повторно
не запускались. App `a1e8ca32745fb9064918c23325115c922cb33fc12eddf6273804dee12edcf94c`, Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2664 keys:1 новый,2663 прежних literals сохранены.
Source/Binaries/fixtures/15WAV/прежние7TestMap bytes сохранены.
Самостоятельное compiler task window (фазы/текст/errors/итог/Close-Run и
CompilerWindowX/Y/Width/Height) ещё отсутствует. Это следующий конкретный
маршрут I17; запоминание геометрии DocumentReport не считается его заменой.
I02 и остальные I/M scopes, включая M21/M23, остаются открыты; цель не завершена.

## CompilerTaskWindow-20261017

18 октября2026: отдельное окно Test/Compile/Run подключено к реальным меню,
toolbar и Ctrl+Shift+E. Фазы, полный журнал и список сообщений без truncation,
severity/counts/details, навигация к области после завершения и итог EN/RU.
Background validation/compilation/writing используют независимую копию по
существующему ClonePropertiesDocument; live cleanup/consent остаются на UI.
Существующие revision/context/plan guards и предупреждения перед writer
сохранены. На время работы открытые editor windows отключаются, autosave
заблокирован, закрытие task window не прерывает writer. После работы states
восстановлены. Run запускает игру отдельной кнопкой; stale документ лишает её
права запуска. Normal X/Y/W/H сохраняются между задачами, monitor-clamped.
[Поведение, доказательства и ручные шаги](COMPILER_TASK_WINDOW.md).
Build04 all41/focus01/publish02 exit0, zero warnings/errors. После build03
уточнён source default: центр desktop/screen вместо owner. Новый Core
`f594915efcd4ce4a36c2131d71196e26a4a609229c4a4ff69896451560239a12`, App `81dba951daec269f0be73eed56b79f43623d128eb5d5f2faa2520f20667e98dd`.
Новый snapshot focus подтвердил независимость bytes/history/statistics,
aliases/target/missing references, active-edit refusal и одинаковые7 official
файлов из пользовательского DKM/native-source и их snapshots.
Полный прогон1821 PASS/63 прежних отказа, exit1. Все1815 прежних PASS сохранены
в том же порядке; добавлены5 уже реализованных App policy checks и1 snapshot.
Failures/Inputs совпадают с предыдущим full побайтно и по порядку. Полный
исходный список94 сопоставлен:31 ранее исправлены, остальные63 прежние;
одно известное уточнение XXLevel14 wMainTrigger=52 сохранено. Новых отказов нет.
EN/RU2676 keys,12 новых; все2664 прежних literals сохранены.
WindowsSmoke/GameRunUiChecks обновлены под async, только скомпилированы.
WPF/native dialogs/audio/game не запускались. Source/Binaries/fixtures,
15WAV и прежние7TestMap bytes сохранены. GUI/DPI и игра остаются M-приёмкой.
CompilerWindowX/Y/Width/Height закрыты по коду; I02 message completeness и
остаток I17 (HideSplash/HideDisk, manager single-list/pending-field/command
routes) не закрыты этим окном. Все17 I и23 M группы остаются конечным перечнем.

## DiskProgress-20261018

Внутри I17 закрыт активный HideDisk caller:5 source методов загрузки DKM/KWD/MAP,
обычного сохранения DKM и MAP сопоставлены,70 create/update/destroy references
записаны в source-scope.json. В App подключено краткоживущее EN/RU окно с путём,
текущим разделом/файлом, фактическими counts/bytes; без оценки общего времени.
Save начинается после согласия и использует прежний atomic writer с наблюдаемым
stream; durable Flush и владение сохранены. Load observer не меняет данные;
принятие документа защищено context. Корневые окна временно отключены, redraw
через Render priority, cleanup в Dispose. Загрузка/Save остаются синхронными,
как source. Recovery подключён; autosave тихий и заблокирован во время явного I/O.
HideSplash caller исключён source #define SPLASH_DISABLED1: новый UI не нужен.
[Поведение, source mapping и ручные шаги](FILE_IO_PROGRESS.md).
Build04 all41/focus01/publish01 exit0, zero warnings/errors. Build02 typo
TickAutoSave в новом UI case сохранён как failed evidence, исправлен в build03.
Core `73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`, App `72c00770ab44beefb8b945d1722d0145e83353dfe3f27fa1f4936698d2ca37e8`.
CLI: DKM/KWD/MAP exact observer/no-observer loads; observer interruption closes
input and never reports Done; array/span/byte stream writes, position/ownership;
DKM/MAP partial-write/flush failures retain old target/history and remove staging.
Полный1822 PASS/63 прежних отказа, exit1. Все1821 прежних PASS сохранились по
порядку, Failures/Inputs совпали точно. Исходный94-list сопоставлен:31 ранее
исправлены,63 прежние; уточнение XXLevel14 wMainTrigger52 сохранено, новых нет.
EN/RU2696 keys:20 новых,2676 прежних literals сохранены. WindowsSmoke добавлен,
только скомпилирован. Физические GUI/dialogs/audio/DPI/game не запускались.
Source/Binaries/fixtures/15WAV/прежние7TestMap bytes сохранены. Все42 Config fields
имеют конкретную маршрутизацию либо неактивность; остаточная полнота MapViewFlags
остаётся I13/I17. Остаток I17 — конечная сверка manager commands, single-list
membership, pending field/menu/shortcut routes; весь I17 не закрыт.17 I/23 M
группы остаются конечным перечнем, цель не завершена.

## ManagerListCommands-20261018

Внутри I17 исправлены3 list-init/whole-map routes: открытие выбирает текущее
выделение; без него scope switch отключён и показана карта. WholeMap теперь
отсекает координаты вне0..W−1/0..H−1, не удаляя raw records из документа.
Selection/family predicates вынесены в канонический pure App helper.
Три source команды «Правка» IDM_CREATURES/DOORSTRAPS/OBJECTS подключены к
общему workspace factory с явной family; toolbar использует тот же route.
Нет выдуманных accelerators. Entry commit/live-pen/closed-owner guards сохранены.
Конечная таблица Things_Manager controls и single-list membership разобраны:
normal source multi move сначала unlink через Undo_StoreMoveThing, затем add;
modern physical projection не дедуплицирует UID и не склеивает разные records.
R02/I14, сценарные scope policies, Core writer/Undo не менялись.
[Сопоставление и ручная приёмка](MANAGER_LIST_COMMANDS.md).
Build02 all41/focus01/publish01 exit0, zero warnings/errors. Новый pure focus
прошёл: WholeMap/selection boundaries, invalid imported coordinates,
empty/missing/overflow-safe selection и unique family пяти kinds.
WindowsSmoke добавлен для actual menu/factory, defaults, reopening, exclusion
без потери raw data, EN/RU, live pen и закрытого owner; прежние initial-scope
ожидания ObjectManagerSmoke обновлены. WPF cases только скомпилированы.
App `1262e270626a02113bafcc04d20f845daf46bacdedf11da28fb4bc75f8c1d908`. Core `73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`
неизменна; full corpus/старые checks не повторялись. Последний существующий
full1822/63 — DiskProgress, на той же Core. Новый focus не является full1823.
EN/RU2697 keys:1 новый, все2696 прежних literals сохранены.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены. GUI/audio/game/DPI
не запускались. Thing list/entry routes закрыты по коду; I17 целиком открыт.
Следующие конкретные menu callers: HEROS, AREAS/GATES/EFFECTS, PATHS, EVENTS
и их own command/pending-field routes. Все17 I/23 M scopes остаются в перечне.

## WorkspaceManagerRoutes-20261018

Подключены шесть source Edit-команд HEROS/AREAS/GATES/EFFECTS/PATHS/EVENTS
к существующим полным редакторам. Стабильная пустая категория отрядов,
правильный Band template, списки регионов по виду и «Все виды областей»,
отключённые действия пустой строки; патруль без областей только сообщает
причину, сценарный вход выбирает глобальный контекст32 без новой привязки.
Общие CommitFields/pen/same-owner/closed-window guards. Навигация не является
операцией карты. [Поведение, source mapping и ручные шаги](WORKSPACE_MANAGER_ROUTES.md).
Build03 all41 и publish01 exit0, zero warnings/errors. Build02 имел2 ошибки
только нового smoke (Selection у workspace вместо Canvas); исправлены,
неуспешный лог сохранён. Четыре новых WPF cases только скомпилированы,
UI/renderer/game не запускались. I14 остаётся закрытым решением пользователя.
App `893a238fbb3b25eee35b6eb6fdf65a6c594ec6ef9e91e4b5ec72b59ed1b6acb7`; Core `73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`
не менялась. Full corpus и прежние CLI-focus не повторялись: последний
фактический full1822/63 — DiskProgress на той же Core. EN/RU2705 keys,
8 новых на язык, все2697 прежних literals сохранены. Source/Binaries/fixtures,
15WAV и7TestMap bytes сохранены. I17 целиком открыт: остаточная полнота
собственных команд/callbacks и MapViewFlags входит в прежний конечный список,
подключённые девять menu entries повторно не ставятся на разработку.
Все17 I и23 M пунктов остаются; цель в целом не завершена.

## SourceMenuInventory-20261018

Закончен конечный inventory входов Window_Command:70 явных IDM cases,
8 диапазонов и wall/claimed pair. [Полная таблица и конкретный остаток](MAIN_MENU_SOURCE_MAPPING.md).
Это сопоставление входов, не подмена полноты внутренних callbacks I02/I07/I10/I11.
Из остатка I13/I17 снят состав/битовые маршруты всех15 MapViewFlags:
значения/defaults, EN/RU, restore/capture и render consumers сопоставлены.
Исправлен доступ при скрытом toolbar: все15 переключателей теперь также
в постоянном меню «Вид», используют прежние Configure* и прежние settings.
Нет второго состояния, записи карты или нового Undo. Actual rendering/DPI — M21/M23.
Остались8 конкретных source IDs главных команд: UNDOFREE (очистить без
отключения tracking), пять MODIFY* входов настройки кисти по виду, HELPCONTENT
и HELPVERSIONS (сводка девяти section versions). Они остаются внутри I17;
новые верхнеуровневые задачи не создаются. Собственные manager callbacks и
shortcut caller completeness не закрыты одним перечнем menu entry points.
Build02 all41/publish02 exit0, zero warnings/errors. Build01/publish01 тоже
успешны; build02 убрал лишний Separator в новой части меню. Core `73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`
не менялась; App `8aba7df588f20b350deea690d9aa1eb7812a60d57dbdf1fb7b702c77c0d645ef`. Full corpus,
прежние CLI checks и WPF не запускались. Последний full1822/63 — DiskProgress
на той же Core. Tests/ не менялись; для простого обратимого routing новые
зеркальные tests не писались. Все2705 EN/RU literals, Source/Binaries/fixtures,
15WAV и7TestMap bytes сохранены. Цель активна,17 I и23 M scopes остаются.
Следующий конкретный дефект по коду — source UNDOFREE с сохранением depth,
счётчиков, live bytes и save state; не использовать SetHistoryDepth(0) как подмену.

## HistoryClear-20261018

IDM_UNDOFREE подключён как «Правка → Очистить историю». Отдельный ClearHistory
сохраняет bytes карты, dirty/save baseline, ChangeCount/EditorChangeCount,
EditRevision и HistoryDepth; очищает оба стека и snapshot cost. Следующее
изменение получает Undo по прежней глубине. Empty clear — no-op; active pen
и transaction отклоняются; current-owner/closed-window guards в MainWindow.
Один HistoryChanged, без Changed и без записи settings. Старые методы Core
не менялись — проверено сравнением с packaged source после удаления только
нового property/method. [Поведение, source mapping и ручные шаги](HISTORY_CLEAR.md).
Build02 all41/focus02/publish01 exit0, zero warnings/errors. Первый focus
ошибочно считал новую карту чистой; fixture изменён на save/reload. Исходный
failure сохранён, продукт из-за него не менялся. Focus проверяет native/project,
mixed/redo-only, exact bytes/counters/depth/dirty/revision, future Undo,
transaction rollback, active pen, immutable snapshots, observer exception и
manager-local Undo authority после очистки. Новые4 WPF cases только compiled.
Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935`, App `7ea314d2d34d0f76f0e8e7979c1617eddf07058e8f561e17b4cd4844a6b2fc75`.
Это additive API, ни один существующий Core caller/read/write/history replay
не переписан; новый focus покрывает затронутые границы. Полный корпус не
повторялся. Последний фактический full1822/63 — DiskProgress на прежней Core
`73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`; он не выдаётся за новый full.
Следующая обоснованная общая интеграция должна отдельно включить новые checks
manager-list и history-clear; полный PASS count сейчас не увеличивается.
Все2705 прежних literals EN/RU сохранены, новый History.Clear, итого2706/язык.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены. I17 целиком открыт:
в таблице главных команд осталось7 IDs — пять MODIFY* brush-kind callers,
HELPCONTENT и HELPVERSIONS; внутренние callbacks/shortcut completeness остаются.
17 I/23 M верхних scope не переименованы. UI/renderer/game не запускались.
Предыдущий goal turn — progress; цель в целом активна.

## ObjectBrushCommands-20261018

Подключены пять MODIFY* команд: «Правка → Настройка кисти объектов».
Явный kind не зависит от Inspector. Прежний ObjectBatchWindow получил
template-only mode: locked family/private copy, без placement preview/coords,
accept выбирает PlaceObject, Cancel не публикует. Шаблоны пяти видов независимы.
Обычный single-placement workspace editor также получает copy; успешный Apply
и UseBrush публикуют принятый template, Cancel больше не меняет source defaults.
Manager multi-session не переписана. Map bytes/counters/history/selection/buffer
настройкой кисти не записываются; будущий paint использует прежние Core plans.
Документ, live-pen, expectedOwner, pending fields и closed-window guards.
[Source mapping, граница и ручные шаги](OBJECT_BRUSH_COMMANDS.md).
Build03 all41/publish01 exit0, zero warnings/errors. Build01/02 имели один
nullable error только нового smoke; исправлено, failures сохранены. Четыре
EN/RU × native/project WPF cases для всех пяти видов только скомпилированы;
GUI/renderer/game не запускались. Зеркальные CLI tests для UI routing не добавлялись.
App `fec04c195aca4a3d35847c5ea7c3ca1ad6790d1ec9c2066f8f1eb11608afa482`; Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935`
не менялась, прежний history-clear focus не повторялся. Последний фактический
full1822/63 — DiskProgress на более ранней Core
`73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`, не новый результат этой сборки.
Core writer/reader/history/painting не изменены, full corpus не повторялся.
EN/RU2708 keys:2 новых, все2706 прежних literals сохранены.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены. В главном dispatch
I17 остались2 IDs: HELPCONTENT и HELPVERSIONS. Собственные callbacks менеджеров
и shortcut completeness остаются в прежнем I17;17 I/23 M верхних scope не закрыты.
Предыдущий goal turn — progress; вся цель активна.

## HelpSectionVersions-20261018

Подключены IDM_HELPCONTENT и IDM_HELPVERSIONS. Справка открывает актуальный
RU/EN USER_GUIDE внутри приложения из shipped Documentation или canonical docs;
local links/heading/inline formatting, missing/failure notices, live language,
modeless owner lifecycle; shell/browser не требуется. Сводка9 source sections
сверяет literal signatures/current versions, раздельно показывает входной
контейнер и working headers, generated-only/old/new/duplicate/malformed,
native KWD/KLD N/A без угадывания. Это read-only без record parsing/upgrade.
[Поведение, source mapping и ручные шаги](HELP_SECTION_VERSIONS.md).
Build03 all41/focus02/publish01 exit0, zero warnings/errors. Focus01 тоже прошёл;
после него добавлен guard короткого рабочего header и независимый case,
поэтому выполнен focus02. Четыре WPF cases только скомпилированы.
App `8524b2c01ef522f864a567d12e70e8f22b2648ab3573ab67062cd73499a5fe59`; Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935`
не менялась. Старые CLI checks/full corpus/WPF/renderer/game не запускались.
Последний full1822/63 — исторический DiskProgress на прежней Core
`73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`, не новый full этой сборки.
EN/RU2736 keys:28 новых, все2708 прежних literals сохранены.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены. В главном Window_Command
не осталось неподключённых входов:70 IDs имеют route либо source Find stub;
8 range dispatch и wall/claimed mapping сохранены. I17 целиком открыт:
конечная сверка own commands/callbacks managers и keyboard callers ещё нужна;
она не заменяется перечнем primary entries.17 I/23 M scopes остаются.
Предыдущий goal turn — progress; вся цель активна.

## SourceKeyboard-20261018

Составлен конечный source inventory30 menu chords и special keys. Подключены
19 бесконфликтных комбинаций: source New/Compile/Test/Help, повороты/vertical
reflection,7 drawing tools,5 explicit placement kinds. Первое placement chord
выбирает принятый/default template, повторный тот же вид открывает настройку,
как Toolbar_ChangeMiscMode. Real PreviewKeyDown сохраняет text-focus gate;
live-pen/current-owner/closed guards. [Таблица и ручные шаги](SOURCE_KEYBOARD.md).
Два конфликта Ctrl+N/New vs source SelectNone и Ctrl+Y/Redo vs source horizontal
reflection заданы пользователю, ответа пока нет; прежние команды не заменены.
3 Insert callers и8 scroll families остаются конкретным keyboard остатком I17;
самостоятельный WPF scroll не объявлен source parity. Внутренние manager callbacks
сохраняются отдельно; primary dispatch70 уже сопоставлен предыдущими этапами.
Build02 all41/focus01/publish01 exit0, zero warnings/errors. Build01 неверно
называл DrawingTool.Pen вместо Brush; исправлено, failed evidence сохранён.
Pure linked-App focus19 routes прошёл;4 WPF cases только compiled.
Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935` не менялась, App `0b64ed2fb0d6a0e90ff69b8dcbc60a4d485208dcaa57cdcb73ba67e3038e573e`.
Corpus и старые CLI/WPF/renderer/game не запускались. Последний full1822/63
остаётся историческим DiskProgress на прежней Core.2736 EN/RU literals без изменений.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены.17 I/23 M scopes открыты;
вся цель активна, предыдущий goal turn — progress.

## SpecialKeyboard-20261018

Подключены3 Insert callers и8 scroll families исходного Keyboard_ProcessSpecialKeys.
Insert окна имеют source close-as-accept: в RC нет Cancel, IDOK/IDCANCEL собирают
маски0xF9/0x0F либо objective/area/enemy. Только Creature template; активный tool
не меняется, карта/Undo не пишется. Stale/doc replacement/unload → Abort без записи.
Полный brush dialog сохраняет настоящий Cancel, не смешан с quick windows.
Scroll использует source integer origin/page=floor(viewport/cell)+1/end clamp;
пустой margin допускает последний source origin при неполной клетке, Canvas tile
Width/Height не меняются. Pointer refresh вызывает прежний MoveMapPointer;
при живом pen прокрутка может продолжить прежний штрих одним Undo, без второго pen.
Map focus не даёт перехватывать ввод полей/дерева; Ctrl+Shift special/Alt отказ.
[Source mapping и ручные шаги](SPECIAL_KEYBOARD.md).
Build03 all41/focus01/publish01 exit0, zero warnings/errors. Build02 nullable
error только нового smoke исправлен, failure сохранён. Pure focus покрывает
literal geometry/margins, routes, modifiers, borders,256 option words/high bits,
objective Area clearing и CB_ERR.4 WPF cases только compiled, UI/renderer/game
не запускались. После focus01 менялись только actual App adapters/новый WPF case;
pure helper и linked focus не менялись, зелёный focus повторно не запускался.
Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935` не менялась, App `1a22e5d0c6ccc6edecdc05c907ae0221f388669ad9d8e52568ca549b998343fd`.
Corpus/старые CLI не повторялись. Full1822/63 — исторический DiskProgress на
прежней Core, не новый full этой сборки. EN/RU2740,4 новых/язык, все2736 прежних
literals сохранены. Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены.
В keyboard остатке I17 остаются2 конфликта Ctrl+N/Ctrl+Y с ожидаемым ответом
человека; нынешние New/Redo не заменены и не объявлены согласованными.
Собственные manager/scenario callbacks остаются I17;17 I/23 M scopes открыты.
Предыдущий goal turn — progress; вся цель активна.

## ManagerCommandScope-20261018

Закончен конечный parent command inventory четырёх менеджеров:46 numeric IDs,
11 named notifications. У каждой строки есть current path/symbol и результат,
совпадения не запущены на новый прогон. [Таблица и шесть конкретных различий](MANAGER_COMMAND_SCOPE.md).
Реализован source LVN/TVN Insert/Delete через ScriptTree PreviewKeyDown:
point/event removal uses прежние requests, condition Insert привязан к выбранной
incoming edge, ordered DKM point Insert прежний; native no-list-order отказ не скрыт.
Type мастера берётся из прежнего retained category context. Core получила только
readonly InitialType(signature); доказано сравнением packaged source после
удаления accessor, что все старые Create/Store/reader/writer методы без изменений.
App malformed-graph errors идут через RecordOperation, tabs/current owner,
CommitFields/selection/closed/live-pen guards сохранены. Script parameters I10
и остальные child pages не объявляются закрытыми parent таблицей.
Build03 all41/focus01/publish02 exit0, zero warnings/errors. Build01 test wrongly
referenced internal ScenarioTypeDefaults; заменён независимыми literal source
initial names, failed log сохранён.4 WPF native/project EN/RU cases только compiled.
Core `b381b548d3c1c188442594d1f6302e1021c8c505017fa1ce6d54e20515487138`, App `3b6687b84908adb40d18b2adcc26e97b2fa91aec590cab3c8c78494377226398`.
Full corpus/старые CLI/WPF/renderer/game не запускались. Новый focus проверяет
readonly accessor, initial names, active/closed category retention, invalid
signature и no live bytes/counters/history/revision changes. Исторический
full1822/63 относится к DiskProgress на более ранней Core, не этой сборке.
2740 EN/RU literals без изменений. Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены.
Внутри I17 осталось6 конкретных parent groups MC01–MC06: полный pre-add region
draft; region list double-click; четыре explicit root/append/action insertion
callers; stored expanded tree state; source coordinate clamp; distinct New Point.
Это подпункты прежнего I17, не новая верхняя очередь.2 Ctrl+N/Y конфликта всё
ещё ожидают ответа человека, нынешние New/Redo сохранены.17 I/23 M scopes открыты.
Следующее конкретное исправление — MC01: общий независимый region creation
draft и один принятый Add с прежними fields/options/Undo/counters.
Предыдущий goal turn — progress; вся цель активна.

## RegionCreationDraft-20261018

В I17 закрыт MC01: создание области, прохода или генератора из полного
независимого черновика. [Поведение и ручная инструкция](REGION_CREATION_DRAFT.md).
Общее окно использует прежний RegionPropertiesSession и его geometry/options/
generation children. До Add живые bytes/counters/history/activity не меняются;
принятые параметры и генерация публикуются одной транзакцией/Undo. Окно остаётся
открытым: геометрия, пользовательские name/info, цвета, flags/effects сохраняются,
ID и исходное автоматическое имя обновляются. Генерация сама не повторяется.
Три Add entry points используют этот route. Закрытие отменяет только unadded
draft; уже принятые записи отменяются обычным Undo. Два цвета сохраняются по
категории до конца приложения, без новых JSON fields. Native labels по-прежнему
не хранятся; новый gate type/orientation берётся из окончательной клетки.
Старые Core методы не переписаны; добавлен отдельный RegionCreationSession.
Build01 all41/focus01/publish01 exit0, zero warnings/errors. Проверены literal
полные records, defaults, repeated Add/names, save/reopen, generation, stale/
foreign/raw/closed/active-edit guards, observer failure и injected post-action
history allocation failure с полным rollback и последующим retry.
Полный интеграционный прогон выполнен из-за нового Core writer route:
**1829 PASS /63 прежних отказа**, exit1. Прежние1822 PASS в прежнем
порядке; все63 Failures и Inputs совпали точно.7 накопленных новых групп с
последнего full, включая MC01, добавились отдельно; это не7 новых возможностей.
Исходные94 сопоставлены полностью:31 ранее исправлен, одно прежнее изменение
диагностики XXLevel14/root52. Новых failures/lost passes нет; корпус не зелёный.
Core `4b1d804ce7a46d961b9fa60739268449c2a356e16d9cd1457b637f7306c35cd1`, App `d1363225543c72a355d8b19fcad1e9df9a8366dc40e8eb67ed8bfa0aca8ac532`.
2746 EN/RU keys,6 новых на язык; все прежние literals неизменны. Source/Binaries/
fixtures,15 WAV и TestMap ровно7 файлов сохранены.4 новых WPF cases только
скомпилированы; GUI/Computer Use/renderer/audio device/game не запускались.
MC02–MC06 и2 конфликта Ctrl+N/Y остаются внутри I17; I14 остаётся закрытым.
17 I scopes/23 M groups открыты. Следующий конкретный маршрут — MC02: double-click
именно выбранной строки региона в существующее private Modify окно.
Предыдущий goal turn — no progress (повторная выдача списка); текущий — progress.
Полная цель активна, физическая приёмка M01–M23 не подменяется сборкой.

## RegionListModify-20261018

MC02 закрыт по коду внутри I17: [двойной щелчок строки региона](REGION_LIST_MODIFY.md).
RegionPicker заменён ComboBox→ListBox с двумя полосами прокрутки и явными
читаемыми цветами. MouseDoubleClick проверяет левую кнопку, настоящий row
container/принадлежность/ссылку на текущую проекцию и передаёт именно его запись
в общий с кнопкой OpenRegionProperties. Blank/scroll/right/foreign/stale
отказывают; full record payload, owner/context/revision, live pen и CommitFields
проверяются до/после commit и после смены выбора. Другой открытый черновик не
подменяется: показано новое EN/RU сообщение, значения сохраняются.
Готовы4 новых WPF cases EN/RU×native/DKM; только compiled, не executed.
Также изменены только типы RegionPicker в4 прежних WPF callers.
Build01 failed: helper Control<T> затенил имя WPF Control в новом тесте;
исправлено полное имя класса. Build02 all41/publish01 exit0, zero warnings/errors.
Core `4b1d804ce7a46d961b9fa60739268449c2a356e16d9cd1457b637f7306c35cd1` без изменений. App `20041aa845510ccd32f2ff3c8fe4297fac8d8ea600dc632fb3e8e8e8927da2f3`.
2747 EN/RU keys,1 новый на язык; прежние literals, Source/Binaries/fixtures,
TestMap7 и15WAV bytes сохранены. Новых CLI/full/WPF/renderer/game/audio прогонов
нет. Последний full1829/63 RegionCreationDraft на той же Core сохраняется
в фактическом объёме; GUI этой поставки им не проверен.
MC01 иMC02 закрыты по коду; MC03–MC06 и2 Ctrl+N/Y conflicts остаются внутри I17.
17 I scopes/23 M groups сохраняются. Physical M12/M20/M21/M23 открыты.
Следующий маршрут — MC03: четыре explicit NewRoot/AppendCondition/AppendAction/
InsertAction callers и их source tail/incoming destinations к общему wizard.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## ScenarioInsertionCommands-20261018

MC03 закрыт по коду внутри I17: [пять явных сценарных команд](SCENARIO_INSERTION_COMMANDS.md).
NewRoot append к хвосту выбранного point root; AppendCondition/AppendAction под
condition идут к хвосту его children, под action — к хвосту его siblings;
InsertCondition/InsertAction используют incoming выбранного occurrence.
Источник IDs122/123/124/127/128, Trigger/ActionSave TVI_LAST и SelectLink.
Сохраняется source запрет действия перед корневым условием. Новый корень не
подменяет New Point (MC06). Shared graph сохраняет actual visual occurrence,
не ищет произвольный unique parent. Read-only resolver проверяет current root,
path/preceding edges и хвосты, cycle/dangling/duplicate/zero/foreign/stale.
App source/context/revision/resources/selection/CommitFields/pen guards;
уже открытый wizard сохраняется с EN/RU notice. Существующий общий мастер,
writer, aliases, repeat, rectangle batching и orphan generic route сохранены.
Core добавлена только новая функция чтения и enum; все прежние Core files
побайтово совпали с предыдущим source package. Новая focus04 проверяет26
native/DKM destination/insertion cases, полные bytes прежних events с packed
offsets, Cancel, source count/Undo/Redo и malformed/shared/empty boundaries.
Build04 all41/focus04/publish01 exit0, zero warnings/errors. Build01 имел ошибку
только нового WPF test: ushort CreatedEventID принят за nullable. Focus01 test
сравнивал UsageOfUndo с началом серии после реального Undo; исправлен snapshot
перед resolver. Focus02/03 выявили неверный test oracle: после вставки DKM
condition старый ACTION offset уже указывает другую запись. Идентичности и
incoming source IDs теперь фиксируются до insertion; byte assertions сохранены.
Все failed logs сохранены.4 WPF EN/RU×native/DKM cases только compiled.
Core `7f530dc5997406195e4f6fb5be557bd6eabc8a50f48d7e203b831232dd18f2ed`, App `cdfe901eeb06cda580fdffd95125fb2626eca1a35b1ca9cdfec53bc5d80278f5`.
2754 EN/RU keys,7 новых на язык; прежние literals, Source/Binaries/fixtures,
15WAV/TestMap7 bytes неизменны. Full corpus/старые checks/GUI/Computer Use/
renderer/audio device/game не запускались. Последний full1829/63 относится
к прежней Core RegionCreationDraft; новый readonly API проверен focus04,
не выдаётся за full новой DLL. Нового reader/writer behavior нет.
18 октября человек ответил «оставить как у нас»: Ctrl+N = новая карта,
Ctrl+Y = Redo. Оба конфликта закрыты, code этих сочетаний не менялся;
source clear-selection/horizontal reflection остаются в меню.
MC01/MC02/MC03 закрыты по коду; MC04–MC06 остаются внутри I17.
17 I scopes/23 M groups открыты, полная цель активна. Следующее — MC04:
сохранённое раскрытие condition tree, с проверенным DKM/native layout.
Предыдущий goal turn — progress; текущий — progress.

## ScenarioExpansion-20261018

MC04 закрыт по коду: [сохранённое раскрытие условий](SCENARIO_EXPANSION.md).
TRIGGER56/expanded short14 подтверждены исходником; KTRG2/3 read/write, любое
ненулевое imported значение читается без нормализации. Source TVN_ITEMEXPANDED
не создаёт MapChanges/Undo: наш setter также не создаёт command count, revision,
history/Changed/activity. HasChanges игнорирует только подтверждённые2 байта
условия в bounded KTRG2/3, прочие параметры/links/opaque bytes остаются dirty.
Native24 не имеет expanded: хранится только состояние документа по condition
ID, клон независим; native→DKM переносит true в short14 до remap IDs. Native
game/alias bytes не меняются, новый companion не создаётся. Save fingerprints
учитывают presentation и отклоняют stale preflight. KWD fresh load имеетfalse,
рабочий DKM сохраняет флаг. Дерево использует TwoWay IsExpanded, generation/
owner/revision/closed guards и не раскрывает свёрнутый путь ради старого выбора.
build-03 all41/focus-03/publish01 exit0, zero warnings/errors. Новый focus
проверяет exact2-byte delta/noop, no command/history/events/activity, clone,
Save/Conversion staleness, DKM/native→DKM save/reopen, nonzero/malformed/zero/
active-edit guards, dirty-state exclusion и весь официальный семифайловый
экспорт до/после переключения флагов: bytes совпали.4 WPF cases только compiled.
Первый focus имел ошибочную границу нового теста: Begin/Dispose pen испускает
собственные availability callbacks. Следующий focus ожидал KTRG restoration
от Undo terrain, хотя его delta содержит только tiles. Тест теперь проверяет
отказ setter внутри pen lifetime и Undo правки самого condition record;
byte/count/activity assertions сохранены, failed logs не удалены.
Полный прогон обоснован изменениями Core Save guard/conversion/dirty logic:
**1831 PASS/63 прежних отказа**, exit1. Прежние1829 PASS в прежнем порядке,
все63 Failures/Inputs совпали точно.2 добавленные группы: MC03 и MC04.
Исходные94 сопоставлены полностью:31 ранее исправлен, одно прежнее уточнение
XXLevel14/root52. Новых failures/утраченных PASS нет; корпус не зелёный.
Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347`, App `18dfc9ee014005ae842b6b4b200560a3b05ced6098eb5c6bb41e88b5316cd88b`.
2754 EN/RU keys без изменений; Source/Binaries/fixtures/TestMap7/15WAV сохранены.
GUI/Computer Use/renderer/audio device/game не запускались. MC01–MC04 закрыты
по коду, MC05/MC06 остаются в I17. Ctrl+N = новая карта, Ctrl+Y = Redo сохраняются
по явному выбору пользователя.17 I scopes/23 M groups открыты; вся цель активна.
Следующий маршрут — MC05: source coordinate clamp в private region draft.
Предыдущий goal turn — progress; текущий — progress.

## RegionCoordinateClamp-20261018

MC05 закрыт по коду в I17: [координаты и стрелки](REGION_COORDINATE_CLAMP.md).
Общие creation/modify поля101–104 немедленно ограничивают X/Y отдельными
Width−1/Height−1. RepeatButton и Up/Down дают шаг1; gate endpoints и их
стрелки disabled, X2/Y2 следуют X1/Y1. ES_NUMBER typing filter, unsigned
decimal prefix/UINT→LONG/overflow и промежуточное пустое поле сопоставлены.
Меняются только поля окна, не импортированные bytes. Owner/current/pick/child
guards, независимые sessions и существующий атомарный Apply/Undo сохранены.
Build01/02 all41 exit0 без warnings/errors. Focus01 exit0: границы/шаг/prefix,
отдельные размеры X/Y, native/DKM×3 kinds private Cancel/Apply/Undo/Redo.
После focus добавлен только ES_NUMBER WPF typing filter и compiled assertion;
чистая policy/Core/CLI checks не менялись, успешный focus не повторялся.
Publish01/02 exit0; окончательная поставка соответствует build02/publish02.
4 WPF EN/RU×native/DKM suites (в каждой create/modify×3 kinds) только compiled.
Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347` и все Core sources без изменений; App `9ece9079bae761e44bd872fe8ecd35f5e80e306dda00360ecd68dd5062c44f54`.
Последний фактический full этой Core — ScenarioExpansion1831 PASS/63 прежних
отказа, original94 сопоставлен целиком. Full не повторялся, новый focus не
прибавлен к историческому full. GUI этой App им не проверен.
2756 EN/RU keys,2 новых на язык; прежние literals/Source/Binaries/fixtures,
TestMap7/15WAV bytes сохранены. Редактор/GUI/Computer Use/renderer/audio/game
не запускались. MC01–MC05 закрыты по коду, parent остаток MC06. I17 остаётся
открытым;17 I scopes/23 M groups и все23 FEATURE_PARITY сохранены. I14 и
Ctrl+N=новая карта/Ctrl+Y=Redo не переоткрываются. Полная цель активна.
Следующий маршрут — MC06: отдельный безусловный New Point, не OpenScope.
Предыдущий goal turn — progress; текущий — progress.

## ScenarioNewPointCommand-20261018

MC06 закрыт по коду: [отдельная команда New Point](SCENARIO_NEW_POINT_COMMAND.md).
Source103 Modify=FALSE/Insert=FALSE всегда открывает private type page, не
выбирает уже существующий root. Open existing/Insert/Modify/New Root сохранены.
Default Player и retained type после Cancel принадлежат workspace context;
смена/выгрузка документа сбрасывает тип. New без Existing/InsertBefore и
preferred old owner, новый trigger0. Existing wizard сохраняется с EN/RU notice.
Loaded/tab/owner/context/revision/pen/CommitFields guards; shared Finish
проверяет lifetime, закрытый context не пишет новую карту.
Build01/02 all41/focus01/publish01 exit0, warnings/errors0. Focus проверяет
private Cancel/preflight, две дополнительные DKM player points с существующим
Level, append/exact previous point+event bytes, empty target, count/Undo/Redo,
occupied level/player refusal. Это модельный контракт общего мастера;
actual New button и четыре WPF EN/RU×native/DKM suites только compiled.
После focus добавлен только loaded-workspace guard App, чистые Core/CLI
не менялись; успешный focus не повторялся. Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347` и все его sources
неизменны; App `87d5afa9a7e8941fa5453f7257cd0dd6fb7a3be0b7106d73bf9f21c34f205e9d`. Full не повторялся: последний фактический full той же
Core — ScenarioExpansion1831 PASS/63 прежних отказа, original94 сопоставлен.
Новый focus не добавлен к историческому full. Source/Binaries/fixtures,
TestMap7/15WAV bytes сохранены;2758 EN/RU keys,2 новых на язык, старые literals
сохранены. Редактор/GUI/Computer Use/renderer/audio device/game не запускались.
MC01–MC06 конечной parent table закрыты по коду. Это не I17 complete: найден
конкретный прежний Things Manager destination callback106/107/186/187 — source
ранний clamp против сохранённого raw input. Source/App тела сопоставлены,
remaining-i17-site.json хранит точные paths/hashes/границы. Следующее — этот
callback в прежнем I17; новый верхний scope/неопределённая очередь не добавлены.
Native no-list-order/empty-root boundary остаётся I01/I03; новая кнопка не
создаёт придуманный companion. Duplicate Level/player не разрешён исходником;
additional global DKM points означают других players, а не дубли одного owner.
17 I scopes/23 M groups и все23 FEATURE_PARITY сохранены. I14 и согласованные
Ctrl+N=новая карта/Ctrl+Y=Redo не переоткрываются. Полная цель активна.
Предыдущий goal turn — progress; текущий — progress.

## ManagerDestinationCoordinates-20261018

Исправлен последний установленный Thing Manager numeric callback I17:
[destination106/107 и spinners186/187](MANAGER_DESTINATION_COORDINATES.md).
Общая прежняя RegionCoordinateRules даёт immediate X/Y bounds и шаг1;
typing ES_NUMBER, empty/prefix/UINT принимаются по source numeric правилам.
Промежуточный raw text остаётся в поле, session хранит число. Add/show используют
общую принятую точку, smaller-map BoundPosition ограничивает оба измерения.
Private picker Cancel не пишет session; Confirm после token/owner/revision
явно публикует его. Old callbacks блокируются navigating/busy/request/owner.
ObjectBatchSession вынесен без изменения тела в один канонический файл;
настоящие App session models linked в CLI без WPF. Второй модели нет.
Build01 failed только в новом тесте: PlacedThings не существует, linked unused
fields CS0649. Тест использует ObjectManagerEntries и задаёт/проверяет эти
реальные settings, warnings не подавлены, журнал сохранён. Build02 all41,
focus01 и publish01 exit0, warnings/errors0. Focus: canonical session axes,
empty/prefix/UINT/smaller-map, unchanged options/templates/requests, workspace
isolation и native/DKM Add planning/no bytes/event/revision, atomic Add/Undo.
4 WPF EN/RU×native/DKM suites и старый обновлённый navigation case только
compiled. Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347` и все его sources неизменны; App `7ed36da9124001c295d25dc7a696dce3480f8ef0b9d98795062a3236590ba246`.
Full не повторялся, старые green checks не повторялись. Исторический full
той же Core — ScenarioExpansion1831 PASS/63 прежних отказа, original94
сопоставлен полностью; новый focus к нему не добавлен. Source/Binaries/fixtures,
TestMap7/15WAV bytes сохранены;2758 EN/RU keys/literals без изменений.
Редактор/GUI/Computer Use/renderer/audio device/game не запускались.
Конечная кодовая сверка [I17 закрыта](I17_CODE_CLOSURE.md):42 Config fields,
70 primary IDs+8ranges/wall pair,15 MapViewFlags,30 chords+3Insert+8scroll,
Thing Manager controls/membership,46 parent rows+11 notifications,MC01–MC06,
R01–R05 и явные human Undo/keyboard решения. i17-closure-audit.json привязывает
все inventories/current tables/source hashes/последние build/focus/publish.
Физическая приёмка остаётся в M, границы native I01/I03, nested I10–I13,
исторических layouts/IDs I04/I18, compiler I02, counters I15/activity I16
сохраняются. Теперь16 I scopes/23 M groups=39 открытых пунктов, не39 bugs.
Все23 FEATURE_PARITY сохранены. I14 и Ctrl+N/New/Ctrl+Y/Redo не переоткрыты.
Следующее — I12: level modifiers определения существа, отдельные от модификаторов
видов в свойствах карты; закончить конечное source/control/storage сопоставление.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## CreatureLevelModifiers-20261018

I12 конкретизирован по настоящему официальному editor binary:
[проценты характеристик по уровням](CREATURE_LEVEL_MODIFIERS.md).
Доступный C editor не имеет этой таблицы в GLOBALCREATURE/9 pages;
она не равна4 map-species modifiers и не Shot1/2/3Level. Статически прочитаны
обычный/Pro EXE resources: dialogs213/238/318, fields1851/53…1867 и spin
1852/54…1868 для уровней2–10. Оба EXE только читались, не запускались.
GlobalVariables.kwd: normal type0x41,32 stats×9 levels=288 literal records;
0xE0 —32 отдельных possession tuples на10 уровне. Layout16 bytes:
type/statistic/unsigned-percent-bits/level. Declared section9992, actual13636;
opaque trailer3644 не сканируется как records. Первый inspection неверно
ожидал равенства file length и первого section; граница исправлена без записи
исходного файла. Raw resource/shared snapshots и hashes сохранены.
Добавлен только новый read-only Core API: bounded version4/header/count,
ordered occurrences/duplicates/unknowns/unsigned bits, отдельные SharedDefaults/
MapVariables/OtherCollection/DKM RetainedNativeSource. Никакого last-wins,
guessed default/перенумерации/правки bytes/count/revision/history/events.
Все прежние Core/App files byte-exact относительно baseline, generic schema,
Save/compiler/clone и provenance guard неизменны. Корректный writer должен
обойтись без ослабления immutable-source validation; его ещё нет.
Build01/02 all41/validationbuild01/publish01 exit0, warnings/errors0. Focus01
failed только из-за нового literal fixture: generic schema намеренно не имеет
Unused полей. Fixture использует свои явные bytes, product не менялся. Focus02
exit0: literal/truncation/headers/opaque trailer/unknown/duplicate/possession,
real32×9+32, map и DKM retained, без source mutation/events. Failed log сохранён.
Core `636efcf900e58cad541077cf33d809bb0849bf7fce791f6acd8add719b669c32`, App `c1d828b6e01860f186005cedefaffe993555b0802f9e3c366e63d300fd6a3558`. Нового WPF case/GUI/renderer/game/audio device/
Computer Use не было.2758 EN/RU keys/literals, Source/Binaries/fixtures,
TestMap7/15WAV сохранены. Full/старые green checks не повторялись: только новый
readonly API; старый full1831/63 относится к прежней Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347`,
не к новой DLL. Original94 сопоставлен в том full, результаты не смешиваются.
I12 остаётся открытым: official callback/range/default/type mapping, частный
EN/RU draft/writer с общим Apply/Undo и DKM source-provenance/save/reopen/
семифайловая компиляция. Это конечные части прежнего I12, не новая очередь.
I14/I17 закрыты,16 I scopes/23 M groups и все23 FEATURE_PARITY сохранены.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## CreatureLevelModifierEditing-20261018 — 19 октября

I12 продвинут до [официальных controls и частного draft](CREATURE_LEVEL_MODIFIER_DRAFT.md).
Обычный DK2 Editor.exe исследован статически Capstone5.0.9 в evidence-only
analysis-deps; EXE/GUI не запускался. MFC maps1841/1842→0046c520: storePrevious
0046c550, loadNext0046c680; DDX0046bf90 вызывает signed DDV004e4b31 десять раз
с−10000/10000. Store WORD/load movsx SHORT,32 catalogue entries,9 excluded IDs,
23 visible captions/order подтверждены direct binary references и Core names.
Corrected resource mapping: level2 Edit1847, levels3–10 Edit1851…1865 odd,
FirstPerson1867; прошлый порядок resource entries был ошибочно принят за
порядок levels. Read decoder wire layout от этого не зависел, не менялся.
Имеется отдельное отличие old Cancel: ранее посещённые selections могли уже
писать global arrays. Новый independent draft не выдаётся за доказательство
старых side effects; общий частный Apply ещё не подключён.
Новый CreatureLevelModifierDraft хранит230 private cells, исходные DWORD bits
и signed SHORT view. Duplicate occurrences читаются последовательно как loader;
hidden IDs не редактируются. Все10 values/shape/range проверяются до публикации,
invalid transition не меняет selection/values. No-op не нормализует upper bits,
imported outside GUI range читается без ремонта, incomplete source и closed
draft отвергаются. Нет owner/writer/mutable archive ссылки.
Build01 all41/focus01/publish01 exit0, warnings/errors0. Focus проверяет23 IDs,
ENRU/corrected controls/signed bit patterns/230 values/selection, все10 slots
с−10000/10000 и внешними границами, immutable caller buffers/noop/duplicates/
missing/closed. Нового WPF case/GUI/renderer/Computer Use/game/audio device нет.
Core `e461ec4adb6fd49487e8a7d51864c125a4c00f717d5ff5e1636b22ca0cfc53b9`, App `c4e15526f24282b325651fb2c27741073002c767d653cc320c5e29eec9813918`. Только новый независимый Core file и2 test files;
все прежние Core/App sources/2758 resource literals/Source/Binaries/fixtures/
TestMap7/15WAV bytes сохранены. Full/прежние green checks не повторялись.
Исторический full1831/63 — ScenarioExpansionCore `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347`,
не текущая DLL; original94 сопоставлен там, новые focus counts не прибавлены.
I12 открыт: effective shared/map composition/default/reset, native signed
DWORD writer/omission, mutable DKM delta отдельно от immutable archive с
history/fingerprint/clone/Save/reopen, EN/RU UI/common Apply и7-file compile.
Provenance validation не ослаблена. Это прежний конечный scope I12.
I14/I17 закрыты;16 I scopes/23 M groups и все23 FEATURE_PARITY сохранены.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## CreatureLevelModifierStorage-20261019

I12: [native collection writer](CREATURE_LEVEL_MODIFIER_WRITER.md) добавлен как
независимое преобразование буфера, без публикации в рабочий документ/архив.
Official004aa6e0/004ab0be..004ab152: SHORT→signed DWORD, zero→100,
288normal+32FirstPerson записываются без omission. Caller0047d0c0 затем
пишетThings; ранее opaque3644 bytes после E6 действительно section0xBE.
Этот путь отличается от packed map Globals0047d510. Все EXE/shared только
читались. PatchCollection сохраняет headers/order/ранние duplicates/hidden/
unknown/extension; пишет только последний edited DWORD, проверяет весь batch
и original bits перед output, не нормализует semantic no-op. Missing keys
без подтверждённого effective-source insertion plan отвергаются.
Build01 all41/focus01/publish01 exit0, warnings/errors0. Новый focus:
literal negative DWORD/zero100/bounds и real23-page draft→write→read,
last duplicate, byte-exact scope, stale/duplicate/missing/invalid batch,
source/output independence и installation SHA. Static check01 неверно обрезал
CALL004ab0fd на004ab0ff; диапазон исправлен до004ab100, failed script сохранён.
Core `bcaaa132f882ae2a87b38e8469c31251a87b2184ee9f671efea7c05783764e59`, App `0030930c793a50259d3c4840acb99dd7a73a48609fa4b0ea075e4ccd3e36a633`. Новый один Core file +2 test files; прежние
Core/App sources/2758 resource literals/Source/Binaries/fixtures/TestMap7/15WAV
сохранены. Full/старые green checks не повторялись: новая независимая API
ещё не вызывается существующими документами. Full1831/63 остаётся историческим
для Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347`, original94 сопоставлен там.
I12 открыт: effective shared/map/default/reset, mutable DKM state отдельно от
immutable archive, history/fingerprint/clone/Save/reopen, EN/RU UI/common Apply/
Cancel/Undo/7-file compile и physical acceptance. I14/I17 закрыты,16 I scopes/
23 M groups и все23 FEATURE_PARITY сохранены. Нового WPF case/GUI/game/audio
или Computer Use нет. Предыдущий goal turn — no progress (подтверждён уже
записанный выбор клавиш); текущий — progress. Полная цель активна.

## CreatureLevelModifierSession-20261019

I12: [существующие map tuples подключены к private context и DKM](CREATURE_LEVEL_MODIFIER_SESSION.md).
Native writer меняет только map Variables.kld. DKM хранит отдельныйD2LM v1:
52-byte header/hash исходного retained stream +24-byte records, максимум230.
Проверяютсяlast occurrence/key/original bits/hash/offset/signed value; immutable
ZIP/retained variables и прежний provenance validator не изменены. Внутренний
буфер участвует вTransactionFiles/Undo/Redo/dirty/fingerprint/clone и DKM Save;
в обычных Files/Records/игровых ссылках его нет. Empty delta не сериализуется.
Cancel/no-op не меняют owner; один Apply — один Undo. Save/Save As/recovery/
SaveCopy/project writer/MAP EditableCopy сохраняют необходимый исходныйZIP;
повторное открытие даёт тот же effective результат. Полный rollback после
поздней history allocation failure и outer transaction exception подтверждён.
Сквозное сравнение двух компиляций с фиксированнымDK2Preview_6a88240eaf814f78:
ровно7 файлов, изменились только2 выбранных DWORD вVariables.kld.
Build01 failed только на nullable dictionary key нового test; validationbuild01/02
exit0. Focus01 имел неверный Undo.Count oracle после320 fixture вставок до
history cap; setup очищает только подготовительную историю. Focus02/build02
all41/publish01 exit0, warnings/errors0. Failed logs сохранены.
Full terminal exit1: **1838PASS/63 прежних отказа** на Core
`85a5c8af8cc45821c3575b004bb978da32e4166642e3e40529786b2fcf7e7ec1`; все1831 прежних orderedPASS и63 rawFAIL/Inputs совпали, добавлены7
известных групп с более поздних этапов. Original94 полностью сопоставлен:
31 ранее исправлены,1 прежнее сообщениеXXLevel14 уточнено,63 сохранены.
Protected corpus/source/resolver hashes совпали. Это текущий full, не сумма
focus counters. Нового WPF case/GUI/game/audio device/Computer Use нет.
App `e157861e31c6971d9b1e4040bef4c6d8539539394280bd44160c57763d6ece48`.2758 EN/RU literals, Source/Binaries/fixtures/TestMap7/15WAV
сохранены. I12 **открыт**: shared/map/default/reset и публикация общих каталогов,
missing override insertion, EN/RU parent/child integration и physical acceptance.
Методы этого этапа редактируют существующие tuples, не угадывают отсутствующие
shared values. I14/I17 закрыты;16 I scopes/23 M groups и все23 FEATURE_PARITY
сохранены. Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## CreatureLevelModifierDefaults-20261019

I12: [shared defaults и недостающие записи подключены в ядре](CREATURE_LEVEL_MODIFIER_DEFAULTS.md).
Каталог берётся из загруженных данных/immutable archive либо явного снимка;
эффективные map values накладываются последними. No-op/Cancel не создают записи.
Private reset копирует9+1 значений captured страницы; это современное явное
действие, в original dialog238 direct Reset отсутствует. Static PE/IAT proof:
1846/1849/1841/1843/1844/1845 скрываются черезShowWindow(0), не EnableWindow;
EXE не запускался. Дополнительная primary-reference OpenKeeper читает refs
по порядку и обновляет level/stat keys; это не actual game execution.
Native добавляет typed16-byte tuples в Variables.kld. DKM хранит новые user
records отдельно вD2LO v1 (16-byte header+16-byte records,max230); они не
выдаются за исходные данные. D2LM/immutable retained bytes/старый provenance
validator сохранены. D2LO проверяетcount/key/range/canonical bits/duplicates/
collision with original source. Fresh DKM не требует выдуманного nativeZIP.
Mixed existing update/new insertion, history/fingerprint/clone/Save/reopen/
recovery/copies и seven-file compile проверены. ParentMapProperties scope
разрешает толькоtyped level DWORD/new rows/D2LM/D2LO; unknown/early duplicates/
shared/source archive защищены. Общий parent Apply остаётся одним Undo.
Focus01/validationbuild01/build01 all41/publish01 exit0, warnings/errors0.
Full terminal exit1: **1839PASS/63 прежних отказа**, Core
`46aaca15d19d50a4c2d9618129eda9886506382d7cf9dfd46f976faa5c7f695e`. Все1838 orderedPASS/63 rawFAIL/Inputs совпали с предыдущим full;
добавлена1 новая группа. Original94 полностью сопоставлен:31 ранее исправлены,
1 прежнее сообщениеXXLevel14 уточнено,63 сохранены. Protected hashes совпали.
Новый late-memory case отказывает после owned insertion и полностью откатывает
owner; все230 visible values сохраняются/компилируются. Старый injection helper
считаетTransactionFiles динамически; прежний отказ MapProperties level edits
заменён intentional private Prepare/общим typed parent Apply.
App `471740122b12613cb838fcc2807709473ec58bc73aba57de4495d554f4b666d6`,2758 EN/RU literals, Source/Binaries/fixtures/TestMap7/15WAV
сохранены. Нового WPF case/GUI/game/audio device/Computer Use нет.
I12 открыт: EN/RU child window вMapProperties.WorkingDocument, выбор source
черезGameResources, явный экспорт копии общего GlobalVariables.kwd, корректные
conversion captions для поддержанных tuples, затем физическая приёмка.
Это конечные части прежнегоI12, не новая очередь. I14/I17 закрыты;16 I scopes/
23 M groups и все23 FEATURE_PARITY сохранены. Предыдущий goal turn — progress;
текущий — progress. Полная цель активна.

## CreatureLevelModifierWindow-20261019

[Окно EN/RU и экспорт копии каталога](CREATURE_LEVEL_MODIFIER_WINDOW.md)
подключены в MapProperties.WorkingDocument.23 statistics/10 signed fields,
spinners ±1/clamp, store-before-selection, private reset, ENRU labels without
rewriting pending strings, source priority/picker, retained catalogue on reopen.
Child Apply пишет только в private parent; pending child блокирует commonApply;
parent Cancel/oneUndo/source lifecycle/две consent revision guards сохранены.
Каталог ограничен64MiB, snapshot independent. Private all230 view экспортируется
через verified writer в выбранную копию: exact hidden/unknown/header/following
section bytes; atomic staging/flush/late owner+input guard; partial I/O сохраняет
прежний target. UI запрещает исходный путь каталога. Экспорт не применяет карту.
Добавлены23 matching ENRU keys, теперь2781; все прежние literals сохранены.
Save.Issue.CreatureLevelModifiers различает2visible valid tuples и4unknown/
hidden/invalid в новой проверке. Неизвестные сохраняют прежний read-only caption.
В inspection исправлен catch: InvalidDataException наследуетSystemException,
неIOException; новый UI path перехватывает его/ошибки доступа, сохраняя контекст.
Build01/02/03/04 all41 exit0; итоговыйbuild04 warnings/errors0; focus02/publish01
exit0. Focus01 до последнего conversion-case тожеpassed, повтор только из-за
изменённого кода/новогоcase. WPF4suite ENRU×native/DKM добавлены/скомпилированы,
**не исполнены**. Ни редактор, ни игра, ни native dialogs, ни Computer Use не
запускались. Статическая route/resource сверка и full привязаны к hashes.
Full terminal exit1: **1840PASS/63 прежних отказа**, Core
`d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`. Все1839 orderedPASS/63rawFAIL/Inputs совпали с прошлымfull,
добавлена1 export-group. Original94 полностью сопоставлен:31 ранее исправлены,
1 прежнееXXLevel14 сообщение уточнено,63 сохранены. Protected hashes совпали.
App `9f9105a967e2b59b6c135dc1923b6e8830900f25a2f6a238cc607d840f6ae5fc`.Source/Binaries/fixtures/TestMap7/15WAV не изменены.
Конечная source/control/storage/UI/export сверкаI12 закрыта **по коду**:
[таблица требований](CREATURE_LEVEL_MODIFIER_WINDOW.md) иsource-scope.json.
Физическая приёмка не объявлена успешной: M19/M21/M23 сохраняют23stat fields,
фокус/Tab/Enter/Escape/ENRU/DPI/native dialogs и actual game effect. Это реальные
ручные gates уже существовавшей очереди, не перенос незавершённого кода.
Открыто15I scopes и23M groups=38 работ; все23 FEATURE_PARITY сохранены.
I14/I17 и утверждённые Ctrl+N/New,Ctrl+Y/Redo/полныйUndo сохранены.
ДалееI11: конечное сопоставление оставшихсяglobal callers. I12 tests не повторять
без кода/новогоdefect; ручная инструкция дана пользователю вfocused doc.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## CreatureLightCallbacks-20261019

[Исправлен ввод положения и радиуса света существа](CREATURE_LIGHT_INPUT_CALLBACKS.md).
I11: четыре source controls104/106/108/114 ограничивают целую часть сразу при
EN_CHANGE. Дробь внутри границ сохраняется; spin следует исходному whole-part
signed/unsigned маршруту. Загрузка/смена языка/no-op не нормализуют импорт.
Native/DKM private page/common Apply/один Undo и exact fixed-point DWORD
проверены focus01; validationbuild01/build01 all41/publish01 terminal exit0,
warnings/errors0. Четыре ENRU×native/DKM WPF suites толькоcompiled, не исполнены.
Полная регрессия не повторялась: последние1840PASS/63 прежних отказа относятся
к предыдущему Core `d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`, не к новой DLL.
Текущий Core `ddc00389a2c51baad23e91515324ed4a6d7267a9f3e623ec52cfa5e75ad6f75d`, App `b194020dc30e47388005f2fd8d6c6dbaec094fb6774bd9321ef99f8d3a6a15cd`.2781 ENRU literals сохранены.
Source/Binaries/fixtures/TestMap7/15WAV bytes сохранены; игровые карты не менялись.
callback-inventory.json фиксирует14 исходных page procedures, notifications,
явные control IDs и source hashes. Inventory не доказываетsemantic closure.
I11 остаётся открыт: далееконкретные Params EN_CHANGE/UDN, затем остальные
уведомления inventory со сверкой существующего кода и evidence; уже успешные
неизменённые поля/attraction/jobs не повторять. Общие15I/23M scopes сохранены,
I12/I14/I17 закрыты по коду. Физическая приёмка M19/M21/M23 ещё открыта.
GUI/игра/audio device/Computer Use не запускались. Предыдущий goal turn —
progress (решение Ctrl+N/Y отражено в документации); текущий — progress.
Полная цель активна.

## CreatureParameterInputs-20261019

[Parameters: исправлен ввод количества второго привлечения](CREATURE_PARAMETER_INPUT_CALLBACKS.md).
Source control129 EN_CHANGE сразу ограничивает0..16000 для активных типов;
UDN следует unsigned Get/SetDlgItemInt и повторному EN_CHANGE. Пустой ввод
не заменяется до явного spin; none/unknown/stale не получают clamp/step authority.
Открытие/смена языка/no-op не ремонтируют импортированное количество.
Общий parser буквально выделен из Light; обе focused группы прошли на новойDLL.
Все4 active types native/DKM private Apply/common oneUndo/Redo/Cancel проверены.
Validationbuild01/focus01/build02 all41/publish01 terminal exit0; warnings/errors0.
Build01 обнаружил синтаксическую ошибку в новомWPFtest; исправлена, лог сохранён.
Четыре ENRU×native/DKM WPF suites толькоcompiled, не исполнены.2781 ENRU literals
сохранены. Source/Binaries/fixtures/TestMap7/15WAV bytes сохранены; карты не менялись.
Текущий Core `1c18dc138b895c4667245695e9f812231b01e71ba528d786b776b19c99d43ded`, App `d2c3cf67960903c61e80d2f5df83221d19e91527f086e63afa3451f561829872`.
Полный корпус не повторялся: последний full1840PASS/63 прежних отказа относится
к CreatureLevelModifierWindow на Core `d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`.
callback-inventory.json сохраняет14 procedures с полным перечнем named case labels,
включаяWM/LVN (их не было в первом notification-only snapshot). Это inventory,
не semantic closure. I11 остаётся открыт; закрыты только Light4 и Parameters129
numeric callbacks. ДалееAttributes EN_CHANGE→SetNewValue, UDN→ModifyValue,
LVN_ITEMCHANGED→ChangeValueType; сверить existing metadata controls с source.
Не повторять неизменённые262fields/73formats/22catalogues/attraction/jobs checks.
I12/I14/I17 закрыты по коду;15I/23M groups и все23FEATURE_PARITY сохранены.
Физическая приёмка M19/M21/M23 открыта. GUI/game/audio device/Computer Use
не запускались. Предыдущий и текущий goal turns — progress. Полная цель активна.

## CreatureAttributeInputs-20261019

[Исправлен ввод38 атрибутов существ](CREATURE_ATTRIBUTE_INPUT_CALLBACKS.md).
EN_CHANGE→SetNewValue:11 fixed,13 signed,14 unsigned; source whole-part bounds
сразу отражаются в TextBox. UDN→ModifyValue использует signed Get/Set во всех
трёх ветвях, включая empty/invalid zero и LONG wrap. Native WORD guards и
строгий Apply сохранены. LVN→ChangeValueType сопоставлен с отдельными38 fields
и default hints; нет выбора группового заголовка как редактируемого поля.
Read/rebuild/language/no-op не ремонтируют imported bytes/signed aliases.
Native/DKM private Apply/common oneUndo/Redo/Cancel и width refusal прошли
focus01. Validationbuild01/build01 all41/publish01 terminal exit0, warnings/errors0.
Четыре ENRU×native/DKM WPF suites подготовлены иcompiled, не исполнены.
Текущий Core `89aa3123d57166fb089d7d10289bd479c7424fb5f95496c34eca66efcf91e536`, App `00fdca5e21b9a4e034f7138b62ceb09b4bd48768fc318e2ba8f6489561a083ef`.
2781 ENRU literals, Source/Binaries/fixtures/TestMap7/15WAV bytes сохранены.
Игровые карты не менялись. Полная регрессия здесь не повторялась; последние
1840PASS/63 прежних отказа относятся к CreatureLevelModifierWindow на Core
`d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`. Focus к этому числу не прибавляется.
source-scope.json сохраняет38 source entries/flags/offsets и3 helper hashes/lines;
связь с finite14-page inventory сохранена. I11 остаётся открыт. Следующий
конкретный маршрут —16 числовых EN_CHANGE/UDN controls страницы Attacks;
Shot list dependencies и доказанные поля/units/reset/общий lifecycle не
реализовывать повторно. I12/I14/I17 закрыты по коду;15I/23M groups и все23
FEATURE_PARITY сохранены. M19/M21/M23 физическая приёмка открыта; GUI/game/
audio device/Computer Use не запускались. Предыдущий и текущий goal turns —
progress. Полная цель активна.

## CreatureAttackInputs-20261019

[Attacks: исправлены16 числовых controls и добавлены исходные шаги](CREATURE_ATTACK_INPUT_CALLBACKS.md).
EN_CHANGE normalizes whole part:9 signed fixed,6 unsigned fixed,1 unsigned
integer. UDN uses corresponding signed/unsigned Get/Set and subsequent clamp.
Одна существующая панель обслуживает standalone и private shared parent;
TextChanged/RepeatButton/UpDown wired, owner/baseline/control/enabled guards.
None отключает параметры и шаги, не очищая pending значения; common Apply
сохраняет их вместе с ID0. Read/language/no-op не ремонтируют импорт.
Core focus проверил все16 полей и4slots native/DKM private/common oneUndo/
Redo/Cancel, literal raw values и imported unknown/out-of-range no-op.
Validationbuild01/focus01/build01 all41/publish01 terminal exit0, warnings/errors0.
Четыре ENRU×native/DKM WPF suites толькоcompiled: обе панели/кнопки/cache/None/
language/stale owner/common Apply/import. Actual UI/renderer/game/audio device/
Computer Use не запускались.2781 ENRU literals сохранены.
Текущий Core `daec065f4b53b47c473f253a13033fe89f9f18c728993ba4dde483791767c530`, App `b4d05cd2b8681a48039c6fc1237fecab5906a02a6e0bb7e937eaebc44f472d75`.
Source/Binaries/fixtures/TestMap7/15WAV bytes сохранены, игровые карты не менялись.
Полный корпус не повторялся: последние1840PASS/63 прежних отказа относятся
к CreatureLevelModifierWindow на Core `d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`.
Source-scope содержит16 source control/spinner/field maps и hashes. I11 открыт;
следующие7 numeric controls — Possession105/108/136/138/140 и CrSpells104/106.
Их existing Shot/options/list relationships не реализовывать повторно.
I12/I14/I17 закрыты по коду;15I/23M groups и все23FEATURE_PARITY сохранены.
Ручная M19/M21/M23 приёмка остаётся. Предыдущий и текущий goal turns — progress.
Полная цель активна.

## CreatureAuxiliaryInputs-20261019

[Исправлены7 числовых controls Possession/CrSpells](CREATURE_AUXILIARY_INPUT_CALLBACKS.md).
Unsigned EN_CHANGE/UDN, whole-part bounds, empty/overflow/UINT roundtrip;
native CrSpells WORD guard сохранён. Possession standalone/shared используют
одну панель, CrSpells private window получает тот жеhelper. RepeatButton/UpDown
подключены. CrSpells шаг меняет существующий TextBox без Rebuild: controls не
пересоздаются во время удержания. Existing Shot/options/Resistances не менялись.
Read/language/no-op не ремонтируют импорт. Core focus проверил обе семьи
native/DKM private Apply/common oneUndo/Redo/Cancel/width refusal иliteral bits.
Validationbuild01/focus01/build01 all41/publish01 terminal exit0, warnings/errors0.
Четыре ENRU×native/DKM WPF suites толькоcompiled, не исполнены; обе страницы/
обе формы/repeat/control identity/language/stale/common Apply/import подготовлены.
Текущий Core `c1edcf88f5a1b166e1cb6fdf303a47abeecfb5d02cfb7ff65a6c15dcdb6b8165`, App `ec7019af15c13f85a96a8c2c1231bd1045a5f35fc7b96480f700f8a68a3a2ed9`.
2781 ENRU literals, Source/Binaries/fixtures/TestMap7/15WAV bytes сохранены.
Игровые карты не менялись. Full здесь не повторялся: последние1840PASS/63
прежних отказа относятся к CreatureLevelModifierWindow на Core
`d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`. Новыйfocus не прибавляется.
source-scope.json связывает7 source fields/controls/spinners/ranges и2proc hashes.
I11 открыт. Следующие9 source numeric controls Jobs соответствуют3 общим полям
выбранной группы: EN_CHANGE/UDN перед Add/Change; не повторять доказанные private
arrays/warning order/common Apply. Затем remaining global-element callbacks.
I12/I14/I17 закрыты по коду;15I/23M groups и все23FEATURE_PARITY сохранены.
Физическая M19/M21/M23 приёмка открыта; GUI/game/renderer/audio device/Computer
Use не запускались. Предыдущий и текущий goal turns — progress. Полная цель активна.

## CreatureJobInputs-20261019

[Jobs: исправлены9 source numeric controls /3 shared fields](CREATURE_JOB_INPUT_CALLBACKS.md).
EN_CHANGE/UDN: chance unsigned0..100, mood/mana signed±32767; whole-part,
empty/invalid getter zero, signed/unsigned wrap. В одной standalone/shared
панели подключены TextChanged,6RepeatButtons и UpDown. Шаг не перестраивает
controls; Hate не получает numeric authority. Ввод не выполняет Add/Change.
Проверены3 parameter groups native/DKM exact Add/Change tuples, private Apply,
общий одинUndo/Redo, Cancel и imported no-op Change. Writer/private arrays/
warning order/retained page context не менялись; их green checks не повторялись.
Validationbuild01/focus01/build01 all41/publish01 terminal exit0, warnings/errors0.
Четыре ENRU×native/DKM WPF suites толькоcompiled, не исполнены. GUI/renderer/
game/audio device/Computer Use не запускались.2781 ENRU literals сохранены.
Текущий Core `568efaff61fceb71afdfffbad4ccf6284cb6b456325a3a801caf0006e3ae814d`, App `c02964fd18b1afdc6a7d113251264aff3f2e4c9660e601a57bf31215bde96e68`.
Source/Binaries/fixtures/TestMap7/15WAV bytes сохранены; игровые карты не менялись.
Full здесь не повторялся; последние1840PASS/63 прежних отказа относятся к Core
`d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1` этапаCreatureLevelModifierWindow.
Source-scope сохраняет9 controls/spinners/ranges иsource hash/line. I11 открыт:
далее numeric callbacks5 Global Elements pages; затем remaining callbacks/
helpers. Известный code остаток Attributes/Light/Parameters: Step/Rebuild не
доказывает сохранение RepeatButton/focus при удержании; не переносить его вM
до исправления/сверки. Это часть прежнегоI11, не новая верхняя очередь.
I12/I14/I17 закрыты по коду;15I/23M groups и все23FEATURE_PARITY сохранены.
Ручная M19/M21/M23 приёмка открыта. Предыдущий и текущий goal turns — progress.
Полная цель активна.

## GlobalElementInputs-20261019

[Исправлены33 числовых обработчика пяти страниц Global Elements](GLOBAL_ELEMENT_INPUT_CALLBACKS.md).
Ввод и шаги Terrain9/Room4/Door5/Trap11/Spells4 сопоставлены с source:
signed/unsigned whole-part, integer clamp и отдельный float setter TerrainLightH.
Пять окон используют RepeatButton и Up/Down без пересоздания controls;
source masks и disabled ManaCost buddy сохранены. В CreaturePageWindow
Attributes/Light/Parameters также устранён известный Step/Rebuild code gap.
Удержание/фокус/DPI остаются ручной приёмкой; скомпилированные cases её не заменяют.
Native/DKM focus проверил32 активных поля, exact wire values, private/common
Apply, один Undo/Redo, Cancel и imported no-op. Storage/compiler не менялись.
Validationbuild01/focus01/build01/build02 all41/publish01 terminal exit0.
Финальный build02: ошибок и предупреждений0. Четыре ENRU×native/DKM WPF suites
только скомпилированы; GUI/renderer/game/audio device/Computer Use не запускались.
Текущий Core `55091f68dde9ba16a64e719a36bb5ebb9374542db80556b36c9e2e7405a78d3a`, App `6201abd7c2a9c499142961946f9ac5d74ab92ab0d9402b9d0986bcb4c17fa81c`. Все2781 ENRU literals сохранены.
Source/Binaries/fixtures/TestMap7/15WAV и игровые карты сохранены побайтно.
Full не повторялся: последние1840PASS/63 прежних отказа относятся к Core
`d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1` этапаCreatureLevelModifierWindow.
В evidence source-scope:33 source bindings, ranges, setters и5 source procedures;
remaining-callers: конечный inventory14 страниц, без заявления semantic closure.
I11 открыт: далее child TypeProc, shared ShotProc, Resistances WM_HSCROLL/ChangeRes,
затем конечная сверка остальных selection/reset/child callers. Подробности по ссылке.
Прежний Step/Rebuild code gap Attributes/Light/Parameters исправлен этим этапом;
исторические записи о нём ниже/выше не являются текущим остатком.
I12/I14/I17 закрыты по коду;15I/23M groups и все23FEATURE_PARITY сохранены.
Полная цель активна; текущий этап — прогресс, не завершение.

## ProjectileChildInputs-20261019

[Исправлен числовой ввод вложенных trigger/shot окон и общей панели](PROJECTILE_CHILD_INPUT_CALLBACKS.md).
Source TypeProc101/spin102, ShotProc101/103/203/205 и спиннеры104/105/204/206:
whole-part signed/unsigned input/step, immediate integer clamps, empty step→1.
Три child classes и inline panel получили RepeatButton и Up/Down; шаг не
пересоздаёт controls. Named/level/inactive/gold masks и owner/baseline guards сохранены.
Пользователь подтвердил два отличия от дефектов source: второй spinner меняет
своё второе поле; signed lower clamp выводит−32768. Decision сохранён в evidence.
Новый focus02 проверил native/DKM и все четыре места хранения, exact signed bits,
private context, соответствующие GlobalVariables/CreatureGlobals parent sessions,
один Undo/Redo, Cancel и native WORD отказ выше65535 при source UI max80000.
Validationbuild04/focus02/build01 всех41 проектов/publish01 terminal exit0;
сборка без ошибок и предупреждений.4 ENRU×native/DKM WPF suites только compiled.
Core `3459b614f4bedb4d7aee73ca7a5912f91138bfb55f100b2afbe96496558e96a8`, App `f61b6e347448707aeab2cb67762dd5715c5dd6890eb0bc32ca61b41618749960`. Все2781 ENRU literals сохранены.
Source/Binaries/fixtures/TestMap7/15WAV и игровые карты сохранены побайтно.
GUI/renderer/game/audio device/Computer Use не запускались. Full не повторялся:
1840PASS/63 прежних отказа относятся к Core `d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`.
Первые restore attempts столкнулись с недоступным user NuGet.Config; stage runner
использует локальный APPDATA только для дочерней CLI. Focus01 выявил ошибку
нового теста: CrSpells направлен в контекст пяти global families; исправлен
выбор CreatureGlobalsSession. Неудачные logs/exit records сохранены.
Следующий I11: Resistances WM_HSCROLL/ChangeRes — четыре Slider/label/type пары,
None/import preservation. Rate пока TextBox; это остаток кода. Затем конечная
сверка прочих selection/reset/child callers из inventory14 страниц.
I11 открыт; I12/I14/I17 code scopes закрыты,15I/23M и все23FEATURE_PARITY сохранены.
Предыдущий и текущий goal turns — progress. Полная цель активна.

## CreatureResistanceSliders-20261019

[Resistances: добавлены четыре Slider и числовые подписи](CREATURE_RESISTANCE_SLIDERS.md).
Один options panel используется inline, standalone и shared CreatureGlobals.
Source WM_HSCROLL104/114/124/134→labels107/117/127/137, range0…100/ticks5,
ChangeRes masks1/2/4/8 и None сопоставлены. Slider сохраняет целую позицию,
свою подпись и control identity; старые/replaced/stale controls не получают authority.
Imported raw255 остаётся в pending/label при thumb100; открытие/язык/тип/no-op
не переписывают его. Явный scroll заменяет только выбранный слот. Это прежняя
политика preservation; source unconditional Save TBM_GETPOS мог бы нормализовать импорт.
Native/DKM focus01 проверил четыре слота, независимые packed offsets,
private parent, общий один Undo/Redo/Cancel и None retention. Storage/compiler/
catalogues не менялись; прежние green checks не повторялись.
Validationbuild01/focus01/build02 всех41 проектов/publish01 terminal exit0.
Build02 без ошибок и предупреждений. Build01 ошибка имени нового WPF test
сохранена; исправлена до финальной сборки.4 ENRU×native/DKM WPF suites только
compiled; реальные GUI/renderer/game/audio device/Computer Use не запускались.
Core `af9be704f576cef2a4585591f646ecb94b769c5ac4fb6473b5a4186edba08866`, App `4e4eb01bee5af118d7773f3f3a2df49fd394daf1180ef915488549b20f94e598`;2781 ENRU literals сохранены.
Source/Binaries/fixtures/TestMap7/15WAV/игровые карты сохранены побайтно.
Full не повторялся:1840PASS/63 прежних отказа принадлежат Core
`d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1` этапаCreatureLevelModifierWindow.
Source-scope сохраняет пять процедур/четыре пары IDs/flags/offsets и границу
raw label/limited thumb. Физические drag/page/key timing/DPI — M19/M21/M23.
Известный code gap TextBox вместо Slider закрыт. I11 остаётся открыт:
далее конечная сверка14-page selection/reset/child callers с существующими
доказательствами; начать LBN101 save-before-row-switch, CBN dependencies,
current/all reset998/999. Повторно не считать проверенные codecs/units/Undo
отсутствующим кодом. I12/I14/I17 закрыты;15I/23M и все23FEATURE_PARITY сохранены.
Предыдущий и текущий goal turns — progress. Полная цель активна.

## GlobalCallbackClosure-20261019 — I11 закрыт по коду

[Конечная сверка14 глобальных страниц завершена](I11_CODE_CLOSURE.md).
14 source procedures,262 reset assignments/native offsets и33 helper bindings
сопоставлены с текущими typed pages/private parents. Непривязанных page calls
в inventory нет. Это code comparison, не33 новых теста и не GUI acceptance.
Исправлен обнаруженный guard GlobalVariablesWindow: после private SaveCurrent
повторно проверяется Current перед Select/Apply; Busy отражается до перехода.
Подготовлены4 ENRU×native/DKM WPF suites с инъекцией owner/source/close в событии
private WorkingDocument.Changed. Suites только скомпилированы, не исполнены.
Build01 всех41 проектов/publish01 exit0, warnings/errors0; audit01 exit0.
Core неизменён: `af9be704f576cef2a4585591f646ecb94b769c5ac4fb6473b5a4186edba08866`; App `8e7add14a4f1909b5d5593851c16b331aa4af3261266d69571f71df701acda37`.
Предыдущие numeric focus proofs связаны с текущими input hashes; контексты,
reset/Undo/storage проверены по сохранённым source hashes. Green checks без
изменений не повторялись. Последний полный1840PASS/63 прежних отказа остаётся
на Core `d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`.
Source/Binaries/fixtures/TestMap7/15WAV/установленные карты сохранены побайтно;
2781 ENRU literals неизменны. GUI/renderer/game/audio device/Computer Use не
запускались. M19/M21/M23 сохраняют настоящие окна/children/input/DPI/game.
I11/I12/I14/I17 code scopes закрыты; остаются14I/23M,37 пунктов работы,
все23FEATURE_PARITY сохранены. I13 catalogue/bitmap vocabulary, I02compiler,
I01/I04/I18metadata/layouts, I15counts иI16activity не объявляются завершёнными.
Далее — конечная оставшаяся сверкаI13 по текущим source controls и контекстам;
не переписывать подтверждённые lists/images/codec ради нового PASS.
Предыдущий и текущий goal turns — progress. Полная цель активна.

## CreatureSpellUsage-20261019 — I13 progress

[Добавлен source used-by list123 страницы заклинания существа](CREATURE_SPELL_USAGE.md).
Native/default-DKM читают OR трёх attack IDs, source45-row order, один user на
существо. Отдельное и shared окно показывают прежние source creature images32×32
и EN/RU имена, сохраняют выбранную строку при переводе; состав — private snapshot.
Непроверенные layouts не получают default users. Read-only список не меняет карту.
Focus01 проверил41 spells×45 rows, независимые offsets0xDDA/0xDF2/0xE0A,
empty/duplicate/private/snapshot/missing случаи и неизменность bytes/revision/Undo.
Validationbuild01/focus01/build01 всех41 проектов/publish01 terminal exit0;
сборка без ошибок и предупреждений.4 WPF suites толькоcompiled, не исполнялись.
Core `5931f3c6bf084c2b86f4feb9df170b15d3117f0e29d2578c938229144e4ca594`, App `08d767cd6ce5bea59f2ba5c9c194139360b595484b6d00fbfdd713e0fab66f62`.2784 ENRU keys:3new,2781old literals unchanged.
Source/Binaries/fixtures/TestMap7/15WAV/установленные карты сохранены побайтно.
Writer/compiler/typed parameters/Undo и source image resources не менялись.
Full не повторялся:1840PASS/63 прежних отказа принадлежат Core
`d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1` этапаCreatureLevelModifierWindow.
GUI/renderer/game/audio device/Computer Use не запускались. M19/M21/M23 открыты.
[I13 finite scope](I13_CATALOGUE_SCOPE.md) фиксирует шесть частей прежнего пункта,
без новой верхней очереди. Следующий конкретный code gap: текстовые universal
Choice строки ScenarioParameterWindow вместо source image lists. В evidence
captured callers девяти Triggers_DisplayCreate*List families с context/line/args.
Не добавлять иконки в source plain combos/gate lists без основания. Остальные
ID/order/captions/images сравнивать по сохранённым доказательствам, не повторять
неизменённые green checks. I11/I12/I14/I17 закрыты; I13 открыт,14I/23M и все23
FEATURE_PARITY сохранены. Предыдущий и текущий goal turns — progress. Цель активна.

## ScenarioChoiceImages-20261019 — I13 progress

[Контекстные картинки сценариев](SCENARIO_CATALOGUE_IMAGES.md): 38 source
контекстов девяти helper families, 114 изображений;13 недостающих BMP resources
встроены по относительным ссылкам без копирования Source. Door/trap IDs и row
identity разделены; plain combos и unknown imports без выдуманных картинок.
Focus01 native/DKM:690 fixture rows при двух тестовых band IDs0/7. Снимки138
context/reference полей до/после буквально совпали. Все114 встроенных BMP
сверены по PE metadata без загрузки App/WPF.2784 ENRU literals прежние.
Validation-build01/focus01/build01 всех41 проектов/publish01 exit0;0warnings/errors.
4 WPF suite толькоcompiled; GUI/game/renderer/Computer Use не запускались.
Core `83902af39b5db4a602a094de0434f0f8cfcd2bfd168e4ab4aa290447470e57ca`, App `fc9395c49a35302aa6385eb05d68f6374ff141d3ddf31331aed6b34fdb45e44b`. Source/Binaries/fixtures/installed maps/TestMap7/15WAV
сохранены. Writer/compiler/Apply/Undo не менялись; full не повторялся.
Последний full1840/63 относится к Core `d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`.
I11/I12/I14/I17 закрыты по коду; I13 открыт.14I/23M, вся цель ACTIVE.
Далее I13: source HerosList вложенные member/leader/objective previews, затем
остальные конечные разделы I13_CATALOGUE_SCOPE. Изображения выбираемой строки
отряда добавлены; состав отряда ещё нельзя считать сопоставленным.

## ScenarioBandPreviews-20261019 — I13 progress

[Состав отрядов в сценариях](SCENARIO_BAND_PREVIEWS.md) добавлен во все5
source HerosList contexts. Участники показываются по исходному порядку до
первого пустого слота, максимум16: имя/уровень/image; только лидер получает
objective, для23 — area. Child rows справочные и не выбираются; параметр
остаётся ID отряда, включая0. Повторные IDs не объединяют разные payloads.
Band choices и members — snapshot открытия/Reset, язык не читает чужие изменения.
Focus01 native/DKM:45 source creatures, packed32/44-byte offsets, ten objectives,
ENRU настоящего App formatter, unknown/raw/gap/empty/duplicate/snapshot/guards.
Чтение не меняет bytes/revision/Undo. Validation-build01/focus01/build01 всех41
проектов/publish01 exit0,0warnings/errors;4 WPF suites толькоcompiled.
Core `38148fc9e25be70975dbe5fbc378df66d07645453b091768021c83a2cd5741a9`, App `d2bda8a770c99bed7754bfbb1c8bb181b94ad106d8533dc6d6f079e345a01d9e`.2787 ENRU keys:3new,2784old literals unchanged.
Все369 original embedded resources прежние по прямому PE чтению; Source/Binaries/
old fixtures/installed maps/TestMap7/15WAV сохранены. Writer/compiler не менялись.
Full не повторялся: прежние1840/63 принадлежат Core `d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`.
HerosList member code comparison завершён; I13 в целом открыт.14I/23M остаются,
вся цель ACTIVE. Далее конечная сверка enum/radio captions/field labels/reference
previews из I13_CATALOGUE_SCOPE. Настоящий popup, scroll/focus/keyboard/DPI — ручная
приёмка; GUI/game/renderer/Computer Use не запускались.

## FieldLabelAudit-20261019 — завершён20 октября, I13 progress

[Конечный перечень ресурсов подписей](FIELD_LABEL_RESOURCE_AUDIT.md):27551
field occurrences с повторениями, current native/DKM/definitions и все project
schema layouts. Пропусков в current records не было;14 исторических keys
получили EN/RU captions. Поля/канонические имена/array annotations/order прежние.
2801 ENRU keys,2787old literals сохранены. Изменены только два словаря.
Core `38148fc9e25be70975dbe5fbc378df66d07645453b091768021c83a2cd5741a9` прежний; App `0ef7b456f4ed3bbc61db9b36b0bb19af3778afc1c017c0d7cee6336c6916ded2` новый. Все41 проекта build01/publish01 exit0,
0warnings/errors. Core/CS/tests/Source/Binaries/Fixtures/installed corpus/TestMap7/
15WAV не менялись. Успешные прежние функциональные проверки не повторялись.
Наличие всех ключей не закрывает семантическую сверку переводов/enum/radio/custom
captions/reference previews. I13 открыт,14I/23M остаются, цель ACTIVE.
Далее source-bound enum/radio caption inventory и оставшиеся контекстные previews.
GUI/game/renderer/Computer Use не запускались. Полный1840/63 остаётся историческим.

## ScenarioRadioAudit-20261020 — I13 progress

[Все50 контекстов ScenarioParameterChoices сопоставлены](SCENARIO_RADIO_CAPTION_AUDIT.md):
30comparison+20enum/radio/checkbox; source symbols/captions/ParamsStore values
сохранены в evidence.100 native/DKM choice fields сохранили значения/порядок.
Уточнены10 ENRU choice pairs в RELEASEBAND/SHOWELEMENT/CHANGEPARAMS: invasion
с random types, stars/blinking, enemy targets и попытка достижения области23.
Изменены2resource literals каждого языка;2801key и остальные2799literals прежние.
Все41 проекта build01/inventory-after/publish01 exit0,0warnings/errors.
Core `6fdeda078cb3ca058b3234aa5e31f8a9045d5844dd6970101206623f80af17cc`, App `56df869b125acbf2b6daedaf6e137ede4b7b12736255990dd6c5a5fe7a23b7d7`. В provider изменились только строки; прочие CS,
tests/Fixtures/Source/Binaries/installed maps/TestMap7/15WAV сохранены.
Writer/compiler/Apply/Undo/bounds не менялись; старые green tests не повторялись.
Scenario radio caption code scope сопоставлен. I13 открыт: ещё global/brush/rule
enum captions, смысл field labels и reference previews из I13_CATALOGUE_SCOPE.
14I/23M остаются; цель ACTIVE. Full1840/63 исторический, не повторялся.
GUI/game/renderer/Computer Use не запускались; actual windows/DPI остаются ручными.

## GlobalCatalogueAudit-20261020 — I13 progress

[Глобальные каталоги и подписи](GLOBAL_CATALOGUE_CAPTION_AUDIT.md):20 source
tables/26контекстов,343 source rows/400вхождений/304symbols. У каждого есть ID,
source caption и ENRU; найдены реальные CB/LB_ADDSTRING callers всех20 tables.
До/после значения/order/bindings прежние. Исправлены3CatalogNames keys: Vampiric
drain, RU Disruption и Translucent arrow;4row occurrences изменили только текст.
17 неизвестных source resistances остаются неизвестными с номером.
Все2801 resource literals и прочие catalogue rows прежние. ВсеCS/tests/Source/
Binaries/Fixtures/installed maps/TestMap7/15WAV сохранены. Writer/compiler/Undo
не менялись; прежние green checks не повторялись. Build01 всех41 проектов,
inventory-after/publish01 exit0,0warnings/errors. Core `6c3697da0851f6929340354bc28648eb73ab5e44dc306f4b314f4094ac9f2299`, App `56df869b125acbf2b6daedaf6e137ede4b7b12736255990dd6c5a5fe7a23b7d7`.
Кодовая сверка перечисленных глобальных enum tables завершена; прочие I13
разделы (EffectNames/MapVariableNames/brush/player/gate captions, field semantics,
reference previews) открыты.14I/23M, цель ACTIVE. Полный1840/63 исторический,
не повторялся. GUI/game/renderer/Computer Use не запускались.

## EffectVariableCaptions-20261020 — I13 progress

[Названия эффектов и переменных](EFFECT_VARIABLE_CAPTION_AUDIT.md) сопоставлены:
371effects/193variables/20groups, source IDs/symbols/captions и ENRU. У variables
сохранены все names/groups/order/default/range/fixed flags;4source20.12 поля прежние.
Исправлены3effect names: Guard Post вместо Sentry, Keeper вместо Guardian,
canonical «Злобный взор». Остальные368 effect captions и все2801 XAML literals
прежние. Ненулевые source <rien> не объединяются сNone0; Fill In Health0B не
получил придуманной игровой семантики. ВсеCS/tests/Source/Binaries/Fixtures/
installed maps/TestMap7/15WAV сохранены. Writer/compiler/Undo не менялись.
Build01 всех41 проектов/inventory-after/publish01 exit0,0warnings/errors.
Core `56a06543681379a9e461b86495fcd24a5d7a5b9c10234f82aabf11851c528e03`, App `56df869b125acbf2b6daedaf6e137ede4b7b12736255990dd6c5a5fe7a23b7d7`. Кодовая сверка названий EffectNames/MapVariableNames
завершена. I13 открыт: brush/player/gate enums, field semantics/reference previews.
14I/23M, цель ACTIVE. Full1840/63 исторический, не повторялся; прежние green
операции не повторялись. GUI/game/renderer/Computer Use не запускались.

## BrushPlayerGateCaptions-20261020 — I13 progress

[Подписи быстрых кистей/ИИ/генератора](BRUSH_PLAYER_GATE_CAPTION_AUDIT.md):67AI
parameters/34choice fields/95rows,10source arrays/31rows,9profiles/15room priorities,
7owners/5availability;10quick flags/10objectives/6gate bits сопоставлены.
Исправлены13parameter labels и1CallToArms choice: source conditions возвращены,
коридоры к проломам вместо поворотов, молния при враге на воде, порог
превосходства угрозы; canonical spell/library names. Names/Group/range/mask/
codes/order прежние. Изменены только строки одного CS; остальныеCS/tests/Source/
Binaries/Fixtures/TXT/2801XAML/installed maps/TestMap7/15WAV сохранены.
Build01 всех41 проектов/inventory-after/publish01 exit0,0warnings/errors.
Core `e573fcf6b1ef2e99b72c736f62c12f433b7ca8d367b0c6aff2338366ea55631e`, App `56df869b125acbf2b6daedaf6e137ede4b7b12736255990dd6c5a5fe7a23b7d7`. Длинные подписи используют прежний Wrap; actual
window/scroll/focus/DPI не доказаны. Прежние green operations не повторялись.
I13 открыт: semantic field labels и remaining reference previews/callers.
14I/23M, цель ACTIVE. Полный1840/63 исторический, не повторялся.
GUI/game/renderer/Computer Use не запускались.
