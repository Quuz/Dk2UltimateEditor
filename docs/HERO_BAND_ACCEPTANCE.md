# Актуальная проверка отрядов героев

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

Позднейший результат: [непустой сценарий участника/перенос/UndoRedo/Save/KWD](BAND_SCENARIO_20261101.md). Остаток ниже исторический; приоритет у нового отчёта.

Позднейший проход: [предел16/перестановки/сохранение полного отряда](BAND_CAPACITY_20261101.md). Эти части теперь проверены; остаток ниже относится к состоянию до этого прохода.

[Живой проход1ноября2026](HERO_BANDS_LIVE_20261101.md) имеет приоритет над историей ниже. M13 частичный: два участника/вложенная отмена/общий UndoRedo/Save/7KWD проверены; точный остаток в отчёте. Старый отказ сохранения11октября ниже не описывает текущую сборку.

Уточнение 11 октября 2026 — реальная проверка KWD→Save As DKM для level1.

Проверка выполнена на текущей основной Release сборке и реальном окне редактора:
открыт исходный level1.kwd, Ctrl+Shift+S, выбран отдельный physical-roundtrip-01.dkm.
В окне «Проверка сохранения карты» кнопка «Сохранить эту версию» отключена.
Доказательство: Test/existing-map-dkm-roundtrip-20261011-01/actual-roundtrip-refusal-accepted-01.json.
И независимый model preflight, и реальный GUI показывают17 issues:5 blocking errors,
12 loss warnings. Не считать этот путь успешно пройденным, DKM/compile/game этапы
не завершены. Ошибки: неизвестный ID каталога доступности, неизвестная категория
доступности (native category2/player3/ID7/state3), map variables E9/4A/F1.
Предупреждения: TileProjection и11 EventPayloadProjection.
Во всех1800 исходных tiles Reserved byte=0, ConversionTile создаёт Reserved=1;
не описывать это само по себе как исчезновение1800 клеток/изменение всей планировки.
E9 raw words233/40/13/1,4A=74/340/0/0,F1=241/6501/0/0; назначение не проверено,
не пропускать/переименовывать эти поля ради прохождения теста и не ослаблять guards.
Source original importer Files_Game.c также сообщает об неизвестных variables/category;
сейчас современный Save As блокирует их вместо молчаливого удаления.

Запрошенные model/physical DKM не созданы. Исходные8 files level1 и установленный
постоянный8-file тест поSHA неизменны. В постоянном слоте остаётся рабочая native
копия level1 из предыдущего теста, а не результат компиляции DKM. Основной код/DLL
не менялись. GUI подтверждение отменено кнопкой «Отмена», исходная карта открыта.
Правило пользователя сохраняется: для игровых тестов всегда
DK2Preview_6a88240eaf814f78; перед заменой проверить отсутствие игры и сделать backup.
Общая цель не завершена. Следующая работа по этому пути — перенос неподдерживаемых
native settings/event bytes с проверяемым сохранением смысла; сначала определить
семантику данных/возможности DKM, затем проверять Save As/reopen/compile/game.

Ниже исторические записи до обнаруженного отказа Save As.

Уточнение 11 октября 2026 — постоянное имя тестовой карты и копия level1.



Прямое требование пользователя: для тестов ВСЕГДА использовать

DK2Preview_6a88240eaf814f78. Это постоянный рабочий слот теста, а не новое имя

для каждого прогона. Перед заменой проверить отсутствие DKII/DKII-DX процесса;

сохранить точные предыдущие файлы, заменять только собственный набор по manifest/SHA.

Оригинальные игровые карты и общие определения не изменять. Случайные names

в штатном PrepareGamePreview продукта пока не менялись; данное правило относится

к нашим дальнейшим тестам. Не использовать случайные новые названия тестовых уровней.



Сейчас под этим именем находится копия существующего level1 (не пустая карта с сердцем).

Source: Data/editor/maps/level1.kwd; реальный SaveCopy экспорт и повторное Load

сохранили все исходные байты и11 packed sections, затем изменены только имена

файлов и ссылки descriptor. Комплект8 файлов: классические7 плюс частный GlobalVariables.

Все оригиналы поSHA неизменны. Прежний16-file тест с сердцем сохранён в

Test/existing-map-roundtrip-20261011-01/previous-fixed-test-copy-01.

Подготовленный новый комплект: renamed-game-copy-02; current installed manifest:

actual-fixed-test-replaced-01.json в той же директории.



Реальный DX запуск PID12916 с -q -level DK2Preview_6a88240eaf814f78;

после15с процесс работает, окно Dungeon Keeper II. Пользователь: «запустилось вроде».

Собственный screenshot capture не состоялся: foreground window did not report a process id.

Доказательство: Test/existing-map-roundtrip-20261011-01/actual-fixed-level1-user-launch-01.json.

Это ещё не проверка GUI Save As/KWD→DKM→compile. Обычный Save As выбирает DKM/MAP,

а неизменённую игровую копию делает Export/SaveCopy. Полную цель завершённой не считать.

Игру оставить пользователю, не подменять текущий тестовый комплект пока игра открыта.

Попытка01 подготовки имела ошибку слишком длинного имени reference, сохранена как

история; успешен только actual-roundtrip-copy-02.json. Once-only scripts не перезапускать.



Ниже исторические записи до замены постоянного тестового слота копией level1.



Уточнение 11 октября 2026: тестовая карта реально загрузилась в DKII-DX.





Пользователь подтвердил «карта запустилась» и прислал скриншот с сердцем подземелья,


HUD/миникартой (мана120, золото3000). Живой процесс18364 имеет команду


DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78.


Доказательство: Test/preview-crash-diagnosis-20261011-01/actual-user-confirmed-game-map-loaded-01.json.


При диагностике в установленном комплекте было6 из16 файлов: десять ссылок на


частные определения не разрешались. Восстановлены только отсутствовавшие десять,


все16 теперь совпадают поSHA с сохранёнными копиями; затем пользователь запустил


уровень успешно. Собственная повторная попытка запуска отказалась запускать вторую


игру, поскольку пользовательский игровой процесс уже был открыт.


Комплект оставлен в игре для пользователя, ничего не удалять во время его игры.


Классический level1 состоит из7 файлов,11 sections definitions упакованы вGlobals.kld


и общийGlobalVariables берётся изData/Editor. Наши16 файлов используют раздельные


определения и частную копиюGlobalVariables. Само число16 запуск не блокирует.


Предположение о двух не включённых sections effects как причине вылета НЕ доказано.


Принята именно загрузка тестового уровня. Полный игровой сценарий, все эффекты,


audio/DPI и полная приёмка редактора этим не проверены; общая цель ещё не завершена.





Ниже записи до подтверждённого успешного запуска; их утверждения «карта не наблюдалась»


относятся к предыдущим попыткам и не описывают нынешний результат.





Актуальная запись — 11 октября 2026, исправление запуска DKII-DX.







Общая цель PAUSED по состоянию get_goal. Выполнено отдельное уточнение пользователя:



основной редактор запускает DKII-DX.exe -q -level <имя приватной карты>.



Доказательство: Test/editor-game-dx-fix-20261011-01/actual-dx-fix-accepted-01.json.



Две чистые сборки и все четыре этапа завершены; по24 группы загруженного UI



в Debug/Release, включая отказ при отсутствии DX, даже если обычный DKII.exe есть.



Core DLL не изменены. Эти проверки не являются полной приёмкой всей цели.







Предыдущий перенос17 файлов основной версии также завершён и проверен:



Test/editor-game-and-role-root-integration-20261011-01/actual-root-integration-accepted-01.json.



Все40 этапов /19 сборок, Core115, по53 model и90 loaded UI в каждой конфигурации.



Эта приёмка предшествует DX-исправлению; старые App DLL hashes уже исторические.



Исторический root436 full также принят, но предшествует обоим изменениям.



Все перечисленные прогоны завершены; не считать старые Running/pending действующими.







Физически создан и сохранён частный проект24x24; через реальный Ctrl+Shift+E



запущен DX процесс6980 от нового editor4436 с точной командой:



DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78.



Последнее наблюдение: окно Flame unpack and parse progress; затем процесс отсутствует.



Карта внутри игры НЕ наблюдалась, причина исчезновения процесса не установлена.



Доказательства, UI снимки, процесс/argv и копия Flame log находятся в



Test/editor-game-dx-fix-20261011-01/physical-01.



Только16 собственных игровых файлов удалены после проверки SHA и отсутствия игры;



точные копии сохранены, все4446 защищённых файлов установленных Maps неизменны.



В4446 входит чужой Thumbnails/MapColours.png — не удалять для старого baseline4445.



Original Source/Binaries1006 и сохранённый DKM неизменны.







Неисправленные физические наблюдения: светлые подписи checkbox New Map на белом



фоне; Dimensions становится «—» после Save DKM; предупреждение Run использует



общий текст Compile про экспорт. Физическое отображение карты в игре ещё требуется.



Диалог базовых определений ожидает игровой descriptor Data/Editor/Maps/level1.kwd:



по ссылкам берутся общие определения, планировка/сценарий берутся из нашего DKM.



Не выбирать отдельные Creatures.kwd/Rooms.kwd вместо descriptor уровня.







Новая полная пятёрка после изменений НЕ запускалась. Старые once-only scripts



prepare_game_and_role_root_full_01.py и checkpoint_game_and_role_root_01.py не запускать



без адаптации: старые App/source hashes и exact Maps4445 уже не соответствуют состоянию.



Продолжение полной цели требует её возобновления пользователем. Нестабильный чат



«Редактор2.0» не читать; не делать reset/commit/push и массовую очистку кеша.







Ниже исторические записи; при расхождении актуальна запись выше.







# Hero bands in the main editor









New and Modify now use a private whole-band draft. Nested member edits remain




private until the parent Save; Cancel discards them. The source guards require




exactly one leader, at most16 members and at most24 bands for New. Existing




Modify remains available at24 bands. Accepted Save owns a single Undo even




when equal; source counters and modern overflow guards are retained.









Source selector Delete removes only the band. It retains literal script roots




and operands, including references that can become orphaned. The actual UI




confirmation explains that behavior. Generic DeleteRecord protections remain.




Names retain raw bytes after their new terminator; native maps have no stored




band names. Creature icons, source catalogs and objective/flag controls work




in EN/RU. History labels also translate and refresh on language changes.









The historical project-band formats1–5 are now accepted in the main parent.




Older records are converted only inside its private band section, using the




original reader's defaults. Selected-band ordinals map to the current record




size; names and available identities survive. Other sections and literal




scripts remain intact. Parent Save commits that conversion and the selected




change in one Undo. Cancel and Undo retain/restore the exact old bytes.




Delete preserves the historical section version. Unknown versions and an




active seventeenth slot are refused atomically. Existing v5 New behavior




for an unrelated malformed band is retained.









Initial delivery: `Test/editor-hero-band-delivery-applied-20261010-01.json`.




Historical delivery: `Test/editor-historical-hero-band-delivery-applied-20261011-01.json`;




10 files (3 Core product files and7 test files),3 before backups/7 additions.




Current source has436 C# files. Candidate acceptance:




`Test/editor-hero-band-historical-staging-20261010-01/actual-candidate-accepted-04.json`.




Current main acceptance:




`Test/editor-historical-hero-band-root-integration-20261011-01/actual-root-integration-accepted-01.json`.




Debug and Release each passed83 model groups: historical20, current band24,




generic band/scope2, unchanged point32, and5 native compilation checks. Each




also passed62 loaded UI groups: historical EN/RU30, current band24, history4




and scope4. All36 actual gates completed0 with16 clean builds.152 fresh PNGs




were rendered;8 historical images across both languages/builds were inspected.




Native compilation checks compare11 original compiler fields for each member,




save and reopen private native copies, and check exact historical project Undo.




1006 original Source/Binaries files and4445 installed map files are unchanged.









The fresh combined five-gate run is pending:




`Test/editor-historical-hero-band-root-full-20261011-01/full-status-01.json`.




The prior root432 Debug UI attempt ended with420/458 identical ordered passes,




exit0 and no final success marker; it is retained and not accepted. Its cause




is unproven. Physical input, DPI, original GUI and actual game execution remain




separate required acceptance work; these focused results do not complete the




whole editor goal.









Known role-caption defect discovered in the original physical v1 Options dialog:




band behavior mask4 means assist the leader, but the main radio currently says




instant death. Mask0 needs the explicit independent-member caption. The separate




hero option4 and ordinary-creature behavior4 keep their instant-death meaning.




A three-App-file fix is prepared in `Test/editor-hero-band-role-fix-staging-20261011-01`.




Its new semantic loaded UI test compiled Debug with zero warnings/errors; runtime




and delivery are pending. Original physical evidence is limited to v1 manager,




parent, member and Options; all drafts were canceled and the original app closed.









Additional actual original hero-band acceptance, 2026-10-11:




The original GUI loaded the private v5 project, changed the second member of




the second band from assist-leader to independent, and used native Save As to




a new private DKM. A literal source-layout check proves exactly one band-section




bit changed (Options2 45→41); every other band/member byte and all five inputs




remain unchanged. The original app was then closed.




Ready main Core Debug/Release passed two clean probe builds/four actual gates,




three model/native groups each: actual-original clean load and typed fields,




private Cancel/equal Apply/one Undo, and eleven native compiler fields per member




with private save/reopen. Main DLLs were not rebuilt. Accepted proof:




`Test/original-hero-band-physical-20261011-01/actual-original-v5-model-native-accepted-01.json`.




This does not accept the modern physical GUI/audio/hardware/DPI/game, the




pending role-caption candidate, or the still-running combined full UI gates.




