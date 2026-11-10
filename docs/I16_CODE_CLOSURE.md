# Кодовое закрытие I16: статистика времени, активности и сохранений

24 октября 2026. Этап ActivityLifecycleAudit-20261023 (каталог создан 23 октября).
**I16 закрыт по коду.** Настоящие окна, физические таймеры/ввод и звук остаются
в ручной приёмке. Алгоритмы writer/identity/command acceptance в других I-пунктах
не объявляются завершёнными этой сверкой.

Из актуальных C-исходников без Obsolete и комментариев получен конечный перечень:
36 мест прямого изменения MapStats, вызова сброса и установки TickCount.
Исторический снимок выбирал24 места, включая отдельные size/write sites;
он сохранён и проверен, а не выдаётся за полный новый перечень. Новый список
дополнительно фиксирует все поля native даты, reset callers и startup.
Числа36/24 — объём исходного перечня, не число ошибок или новых тестов.

Каждое из36 мест имеет source function/line/hash, текущий provider/anchor/hash,
описанную семантику, доказательства и границу вывода в
`artifacts/Evidence/ActivityLifecycleAudit-20261023/caller-route-audit.json`.
В таблице14 семейств. Поиск API/хеши сами по себе не являются функциональным
тестом: они сохраняют границу и проверенные входы ручной сверки кода.

| Семейство | Исходные функции | Текущее правило |
| --- | --- | --- |
| Reset | Map_ResetStats | Новый payload/StartTime; импортированный KSTS не сбрасывается при чтении. |
| Startup/New | Window_Create, Status_CreateTimer, Map_New | Новый документ получает дату создания и начало отсчёта; запуск UI timer не добавляет второй интервал. |
| DKM load | Editor_ExtractAll, Editor_ExtractStats, Files_LoadMap | Первый KSTS, TotalTicks=RealTicks, новый tick после разбора. Missing получает runtime defaults; future/truncated не редактируются догадками. |
| Native load | Game_ExtractAll, Game_ExtractMain | Runtime counters, literal header dates, weekdayFFFF; отдельный игровой файл статистики не создаётся. |
| Text MAP load | Text_ExtractAll | StartTime без выдуманной CreatedTime; MAP не сохраняет KSTS. |
| Project write | Editor_GenerateFile | После создания output: SavedTimes++, RealTicks/LastSaved. Ошибка позже сохраняет source side effects, но не принимает цель/успех. |
| Temporary | Files_SaveMapQuick | Output содержит +1; live SavedTimes возвращается, дата/ticks остаются. Autosave использует этот маршрут. |
| Completion | Compiler_Task | Один CompileTime на захваченном target после начала задачи, включая failure. Отмена location/preflight не считается начатой задачей. |
| Mouse | Mouse_LeftButtonDown/RightButtonDown | Click после соответствующих eligibility guards; отмена drag/picker не создаёт клик карты. |
| Fill | Map_FillArea | Один принятый fill, включая равный; protected/excluded start не считается. |
| Inactive helper | Map_PaintElementSimple | В активном исходнике только declaration/definition; его changed-only правило не управляет live pen. |
| Paint/random | Map_PaintElement | Один accepted call; source LINE initial sentinel(0,0), stationary skip; random отдельно от Paint. |
| Object paint | Things_Paint | Update/Create дают один UsageOfPaintThings; removal increments относятся к editor counter. |
| Undo | Undo_Restore | Один завершённый backward restore; empty/refused и Redo не добавляют Undo use. Activity не откатывается картой. |

Отдельно сверены отсутствие focus-only паузы, закрытие UI таймера без скрытого
Save, принятие нового DKM после конверсии и read-only показ. Elapsed идёт
по uint32 wall-clock ticks, включая время без фокуса; Time/Count overflow
остаётся исходным unchecked поведением. Остановка UI callbacks не означает
записи нового TotalTicks и не создаёт отдельной статистической операции.
Source statistics page заполняет elapsed при Init и не имеет WM_TIMER.
Текущий panel читает runtime snapshot/elapsed на обычном workspace refresh;
показ не меняет байты, history, revision, dirty или импортированные даты.

Первый native/AREA Save As уже [исправлен](ACTIVITY_SAVE_CONTINUATION.md):
принятие нового DKM сохраняет исходный tick, payload и clock providers;
Write/Flush не пропадают. Настоящая повторная загрузка по-прежнему rebases
с RealTicks. Временный Save не принимает документ, неудачный не объявляется
успешным. Шесть новых случаев и существующая unified-save группа прошли
в предыдущем этапе на тех же DLL, поэтому не запускались здесь повторно.

Поскольку в предыдущем этапе изменились общие ActivityStatistics и
NativeMapDocument.ActivityStatistics, два их прежних harness были проверены
на текущем Core: ActivityStatisticsCoreChecks и ActivityStatisticsDocumentChecks
завершились ExitCode0 (по5 PASS-групп). Проверены192-byte layout, malformed
даты, opaque extensions/tails, uint32 wrap, regular/temporary/failed writes,
reentrant clocks/closure, loaded/missing/duplicate/future/truncated KSTS,
точная проекция файла, Undo/Redo, native header и AREA signature.

Первый запуск document harness был ошибкой команды: пропущен обязательный
путь нового fixture-каталога. Он остановился на проверке аргументов
и не начал fixture operations; лог сохранён. Остаточный процесс после
необработанного исключения был остановлен, затем выполнен корректный запуск
activity-document-02 с уникальным каталогом. У первого запуска нет exit JSON;
его нельзя считать успешным. Продукт и тестовые требования не менялись.

Current task/click/paint callers сверены по коду. Существующие WPF smoke cases
остаются подготовленными/скомпилированными; они здесь не запускались и не
выдаются за physical acceptance. Actual scheduling, focus, capture/lost capture,
modal dialogs, audio, rendering/DPI и поведение в игре остаются M-группами.

Нового product/test/resource изменения и новой сборки в этом этапе нет.
Опубликованные DLL — ActivitySaveContinuation-20261023, все41 проекта ранее
собраны без предупреждений/ошибок, publish exit0. Полный корпус без изменений
не повторялся;1840 PASS/63 FAIL — исторический результат, корпус не зелёный.
Текущий остаток:12 участков кодовой сверки и23 ручные группы, всего35 пунктов.
Все23 возможности FEATURE_PARITY сохранены; полная цель ещё не достигнута.
