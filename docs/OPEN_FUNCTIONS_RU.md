# Конечный перечень незакрытых функций

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

M03: базовый цикл сохранение→компиляция→официальный редактор→игра с
изменёнными клетками принят по результату пользователя26.10. Обычные Undo/Redo
также подтверждены. [Сравнение Pro/old](OFFICIAL_RESAVE_COMPARISON_20261026.md)
не выявило потерь проверенных значений; raw opaque отличия перечислены,
old удаляет88байт расширения у каждого существа. M03 и M10 остаются частичными,
весь список23M не закрыт. Далее M02/M04; принятый terrain-cycle не повторять.

Текущая очередь: [пошаговая приёмка23 групп](MANUAL_ACCEPTANCE_RU.md),
сначала M01/M03. По решению26.10 [переход на KWD без DKM/ZIP](KWD_NATIVE_TRANSITION_PLAN.md)
выполняется после завершения этой приёмки. Это следующий этап, он не входит
в прежние закрытые I01–I18 и пока не реализован. Найденные реальные ошибки
текущей сборки исправляем по месту и повторяем только затронутые шаги.

Актуализировано 26 октября 2026. **23 открытых пункта: 0 «исправить»
и 23 «проверить вручную».** Это перечень работ, не число найденных ошибок,
отдельных проверок или процент готовности. Все 23 возможности сохранены.

Получен [ручной результат M01/M03](MANUAL_ACCEPTANCE_RU.md): M01 частичный,
причины двух DK2W141 установлены по границам блоков. M03 не завершён:
инструкция Save As→KWD была неверна; DKM компилируется в игровой комплект.
Реальный пропуск исходного ZIP при Export Copy native-derived DKM исправлен;
новая регрессия на пользовательском файле воспроизвела FAIL до и PASS после.
Ручная приёмка экспорта/компиляции/изменённой карты остаётся открытой.

По коду закрыты I14 (согласованный полный Undo регионов),
[I17](I17_CODE_CLOSURE.md), [I12](CREATURE_LEVEL_MODIFIER_WINDOW.md),
[I11](I11_CODE_CLOSURE.md), [I13](I13_CATALOGUE_SCOPE.md), [I16](I16_CODE_CLOSURE.md), [I15](I15_CODE_CLOSURE.md) и [I05](I05_CODE_CLOSURE.md), [I06](I06_CODE_CLOSURE.md), [I01](I01_CODE_CLOSURE.md) и [I02](I02_CODE_CLOSURE.md), [I03](I03_CODE_CLOSURE.md), [I04](I04_CODE_CLOSURE.md), [I07](I07_CODE_CLOSURE.md), [I08](I08_CODE_CLOSURE.md), [I09](I09_CODE_CLOSURE.md), [I10](I10_CODE_CLOSURE.md), [I18](I18_CODE_CLOSURE.md).
I13 завершён [конечной сверкой](UI_LIST_CALLER_AUDIT.md) 70 функций и 315
мест заполнения списков с текущими поставщиками и прежними доказательствами.
Уже успешные проверки без изменений не повторялись. Ручные M-группы остаются.

Кодовая сверка I01–I18 завершена. Последние25 классов I18 и два исправления
описаны в [I18](I18_CODE_CLOSURE.md). Новые реальные дефекты добавляются только
по наблюдению с причиной; отсутствие нового full run не создаёт повторный кодовый
план. Все23 ручные группы ниже сохранены без переименования кодовых долгов.
Сборка и headless checks не подтверждают окно, звук, DPI или результат в игре.

## Исправить

Открытых кодовых пунктов нет. Полная цель остаётся незавершённой до ручной
приёмки. Начинать с [последовательного плана](MANUAL_ACCEPTANCE_RU.md).

## Закрытая кодовая сверка I18

[25 классов](I18_IDENTITY_POLICY.md), два исправления,8 новых/6 related логических
групп с разделением current/previous DLL evidence. Полные before/final ExitCode
сохранены; корпус не выдан за зелёный. Остаток0I+23M=23.

## Закрытая кодовая сверка I10

[Матрица103 типов/15 контекстов](I10_CALLBACK_MATRIX.md) завершена; [I10 закрыт](I10_CODE_CLOSURE.md).
69 parameter/26 RepeatOnly/8 NoPage; все active Init/Check/Store учтены.
Product/tests/runtime прежние, успешные проверки не повторены. Физическая
приёмка остаётся M18/M23. Остаток1I+23M=24; counts ниже исторические.

## Закрытая кодовая сверка I09

[12 маршрутов](I09_CODE_CLOSURE.md) завершены;4 новых группы/16 fixtures PASS.
Клетки/variant переносятся буквально; region anchors неподвижны. Компиляция
пересчитывает ориентацию, Undo восстанавливает source-retention eligibility.
Product/runtime bytes прежние. Остаток2I+23M=25; counts ниже исторические.

## Закрытая кодовая сверка I08

[15 маршрутов I08](I08_CODE_CLOSURE.md) завершены. Global band/member и их
сценарии остаются вне spatial selection; literal paste не применяет cursor
footprints и single-object CanPaint. Новые5 логических групп PASS.
Product/runtime bytes прежние. Остаток3I+23M=26; старые counts ниже исторические.

## Закрытая кодовая сверка I07

[14 маршрутов](I07_CODE_CLOSURE.md) завершены; [заливка](EDITOR_FLOOD_COMPONENTS.md)
исправлена. Terrain paint/fill сохраняет независимые region/thing/scenario
buffers; I09 остаётся для gate transforms. Осталось4I+23M=27.
Старые counts ниже относятся к прежним этапам.

## Закрытая кодовая сверка I04

[I04 закрыт по коду](I04_CODE_CLOSURE.md): все20 строк [форматов](I04_FORMAT_MATRIX.md)
имеют явные version/length/count, inspector/load/edit/raw/convert policies и
поставщиков доказательств. Последний KWD header guard и parent metadata fallback
исправлены. I18 сохраняет свой scope identities/owners; ручные M-группы не закрыты.
Текущий остаток —5I+23M=28. Датированные counts ниже относятся к прежним этапам.

## Закрытая кодовая сверка I03

[I03_CODE_CLOSURE](I03_CODE_CLOSURE.md): конечные14 маршрутов между native,
DKM,AREA,DKZ иMAP. Owners/aliases/graph IDs/coordinates/loss policy иисторические
upgrades сопоставлены. Последний R14 исправлен:category+UID mapping итолько
active owner field; новые5/related5PASS. M04 остаётся ручной; I04/I08/I09/I10/
I18 сохраняют самостоятельный scope. Осталось6I+23M=29,вся цель не завершена.

## Закрытая кодовая сверка I02

[I02_CODE_CLOSURE](I02_CODE_CLOSURE.md): все28 функций записи и17 маршрутов
валидации/генерации с65 исходными REPORT identifiers. Поля,constants/derived
values,version notices,geometry/players/things/gates,graph/unused иreserved
names имеют текущий provider иописанную policy. Конкретные ошибки исправлены;
safe malformed guards иотличия отsource bugs указаны,не скрыты. Product/tests
не менялись при финальном аудите,успешные группы не повторялись. I04/I18
и23manual groups сохраняют свой объём;вся исходная цель не завершена.
Остаток6I/23M=29пунктов. Датированные цифры ниже относятся кпрошлым этапам.

## Закрытая кодовая сверка I01

[I01_CODE_CLOSURE](I01_CODE_CLOSURE.md): 14 видов состояния и19 групп
маршрутов, в том числе все6 native/2 DKM virtual files. Указаны источник,
хранение, copy/history/fingerprint/Save/companion/compiler и границы
presentation/derived state. Unique UID, physical ordinal и исходный
first-gate coordinate lookup разделены. Текущие guards не выбирают чужой
owner среди неоднозначных UID; opaque retention требует уникальной базы.
Это кодовый аудит с конкретными историческими доказательствами, не новый
запуск старых групп. Product/tests/App/ENRU unchanged, повторных build/tests
нет. Ручные группы остаются. Полный writer I02, межформатный scenario I03,
остальные layouts/повреждённые inputs I04/I18 сохраняют прежний scope.
Остаток8I/23M; исходная цель не завершена.

## Закрытая кодовая сверка I06

[I06_CODE_CLOSURE](I06_CODE_CLOSURE.md): 15 конечных групп,27 исходных
каталогов,235 primitive global declarations и20 сценарных контекстов.
Новые Information DK2N039/040 описывают ID вне проверенного source catalogue;
не объявляют custom game definitions missing и не вводят repair/блокировки.
У поддержанных полей указан catalogue/context/zero rule; неподдержанные
opaque internals и enum/scalar поля явно исключены с причиной. Новые9 групп
и связанная entity-диагностика прошли, all41 build/publish ExitCode0.
Реальные M18/M19/M23, unknown layouts I04, compiler I02 и callbacks I10
остаются в прежнем объёме. Остаток9I/23M, проект в целом не завершён.

## Закрытая кодовая сверка I05

[I05_CODE_CLOSURE](I05_CODE_CLOSURE.md) фиксирует11 cases дляcount/ID/order/
hearts/camera/resources/resize/historical/Save/compile. Пустые slots вDKM
теперь объясняются до принятия. Оригиналы не ремонтируются,дубли не дают
edit authority. Underlying compiler/graph/layout scopes и23M сохраняются.

## Закрытая кодовая сверка I15

[I15_CODE_CLOSURE](I15_CODE_CLOSURE.md) сопоставляет все89 source increments
в55 функциях,5 rollback decrements и5 reset/restore assignments.
Нет требования повторять прежние успешные проверки без изменений.
Алгоритмы underlying commands остаются вI01–I10/I18; все23 M-группы сохранены.

## Закрытая кодовая сверка I16

[I16_CODE_CLOSURE](I16_CODE_CLOSURE.md) сопоставляет36 source mutations/clock
sites,14 семейств и дополнительные lifecycle cases. Shared model/document
checks прошли на текущем Core; первое принятие DKM продолжает прежний таймер.
Physical timers/focus/input иtask/window acceptance остаются M14/M20.

## Закрытая кодовая сверка I13

[I13_CATALOGUE_SCOPE](I13_CATALOGUE_SCOPE.md) объединяет конечные подписи,
source ID/order и image routes; [UI_LIST_CALLER_AUDIT](UI_LIST_CALLER_AUDIT.md)
закрывает оставшиеся 70 source population functions. Это не закрытие всей
логики этих функций. Все 23 ручные группы остаются; source API inventories
и совпадение SHA не выдаются за исполнение окон или новый функциональный тест.

## Закрытая кодовая сверка I17

[I17_CODE_CLOSURE](I17_CODE_CLOSURE.md) привязывает исходное требование к
42 Config fields,70 primary commands/8ranges/wall pair,15 MapViewFlags,
30 chords/3Insert/8scroll, Thing Manager controls/membership,46 parent rows/
11 notifications иMC01–MC06. Последнее установленное отличие destination
106/107/186/187 [исправлено](MANAGER_DESTINATION_COORDINATES.md).
R01–R05, user Ctrl+N/New иCtrl+Y/Redo, полный rollback/Undo сохранены.
Кодовое закрытие не означает исполнения WPF: physical M12/M18/M20/M21/M23
остаются. Native point metadata — I01/I03; unknown layouts/identities —
I01/I04/I18; nested scenario/catalogue pages — I10/I13; I11/I12 закрыты по коду;
compiler — I02, прочие command counts — I15, activity — I16.
Это завершённый прежний scope, не перенос его незаконченного кода в ручной.
Исторические «остальные manager callers» не открывают неопределённую очередь.
## Закрытая кодовая сверка I12

[Creature level window](CREATURE_LEVEL_MODIFIER_WINDOW.md) связывает official
23statistics/10controls/range/SHORT/DWORD с частным draft/default/reset,
map overrides/native/DKM/source provenance/history/save/7-file compile,
EN/RU child наMapProperties.WorkingDocument и guarded export копии каталога.
Build04 all41/focus02/publish01 exit0;4WPFsuite подготовлены и скомпилированы,
не исполнены. Конечная таблица code требований и hashes — в focused doc/evidence.
M19/M21/M23 сохраняют фактическое окно/диалоги/DPI/игровой эффект. Закрытие кода
не означает их прохождения. Неизвестныеlayouts/metadata/global callers остаются
вI01/I04/I13/I18; не открывается новая неопределённая очередь дляI12.

## Закрытая кодовая сверка I11

[I11_CODE_CLOSURE](I11_CODE_CLOSURE.md) завершает конечный14-page inventory:
обычные/составные callbacks, row/leave save, зависимости списков, numeric
input/step, source reset262 fields, children/colour, private context и common
Apply/Undo. Source functions/helpers привязаны к текущим App/Core и сохранённым
proofs. Выявленный owner-after-private-Save guard исправлен; build01 всех41
проектов/audit01/publish01 exit0.4 WPF suites толькоcompiled. M19/M21/M23
остаются. Это закрытие существующего code scope, не перенос недоделанного
кодового сравнения в ручной пункт. Другие I01–I18 и все23FEATURE_PARITY сохранены.

## Проверить вручную

Код соответствующих функций уже существует. Здесь нет заявления, что весь
GUI когда-либо проверен: старые исполнения относятся к своим снимкам, новые
окна после них только собраны. Пункты M группируют реальные действия, а не
обозначают 23 одиночных клика. Для функций с I-пунктом окончательная ручная
приёмка выполняется после его закрытия. Уже сообщённый пользователем успешный
запуск семифайловой карты и её открытие в официальном редакторе сохраняются.

| ID | Отметка | Функция | Что выполнить / условие закрытия | Инструкция и источник |
| --- | --- | --- | --- | --- |
| M01 | **проверить вручную** | Открытие native-карт и inspector | На копиях из доступных установок открыть KWD с собственными/shared definitions; проверить выбор файла, сообщения и просмотр записей. Реальные новые варианты связать с I04/I18. | [MANUAL_ACCEPTANCE_RU](MANUAL_ACCEPTANCE_RU.md) |
| M02 | **проверить вручную** | Создание карты | Пустая/со starter heart, фон, размер,1–5 хранителей и версии; Cancel/предупреждение большой карты; открыть результат в официальном редакторе и игре. | [NEW_MAP_CREATION](NEW_MAP_CREATION.md), [BIG_MAP_REQUESTS](BIG_MAP_REQUESTS.md) |
| M03 | **проверить вручную** | Сохранение, компиляция и запуск изменённой карты | SaveAs Cancel/overwrite No/Yes, реальная ошибка+retry CtrlS и GUI DKM+ZIP export/reopen приняты3ноября. Terrain-крест/семь файлов приняты ранее. Остаются игровые эффекты objects/scenario/rules, отдельная отмена согласия преобразования; EN/DPI в M23. [Evidence](SAVE_DIALOGS_LIVE_20261103.md). | [MANUAL_ACCEPTANCE_RU](MANUAL_ACCEPTANCE_RU.md) |
| M04 | **проверить вручную** | Исторические DKM/DKZ/AREA/MAP и upgrades | На доступных настоящих старых файлах открыть, явно обновить поддержанную секцию, сохранить/импортировать в официальный редактор; отдельно direct selection, buffer export и MAP без/с поддержанными сценариями. При отсутствии реальных файлов результат остаётся неподтверждённым. | [DIRECT_SELECTION_EXPORT](DIRECT_SELECTION_EXPORT.md), [FEATURE_PARITY](FEATURE_PARITY.md) |
| M05 | **проверить вручную** | Палитра terrain/room/path/wall/bridge/gate | Мышью выбрать варианты и владельцев, сверить previews и размещение у края/ворот; проверить результат в игре. | [EDITOR_TERRAIN_PLACEMENT](EDITOR_TERRAIN_PLACEMENT.md) |
| M06 | **проверить вручную** | Карандаш и фигуры | Настоящий drag/capture/release/lost capture, размеры кисти, line/rect/ellipse/fill, случайный рисунок; сравнение исходного Win32/GDI и hardware timing. CLI masks не заменяют это наблюдение. | [EDITOR_DRAWING_HISTORY](EDITOR_DRAWING_HISTORY.md) |
| M07 | **проверить вручную** | Выделение, буфер и copy/cut/paste | All/none, прямоугольник и отряды, непустой буфер Yes/No/remember/reset, Cut Cancel, вставка и clipping; меню/shortcut и сохранение буфера при Undo. | [EDITOR_SELECTION_CALLERS](EDITOR_SELECTION_CALLERS.md), [COPY_CONFIRMATION_LIFETIME](COPY_CONFIRMATION_LIFETIME.md) |
| M08 | **проверить вручную** | Поворот/отражение | Оба поворота квадратного выделения, обе оси отражения, отказ по размеру, защищённый край и воротная геометрия; сверить GUI и игровой результат. | [EDITOR_TRANSFORM_CALLERS](EDITOR_TRANSFORM_CALLERS.md) |
| M09 | **проверить вручную** | Размер карты, anchors и crop | Через свойства карты поменять размер/якорь с objects/areas/gates/player cameras/AI/script targets у края; Cancel/Apply/Undo и native/game результат. Модельные 72 матрицы повторно не запускаются. | [EDITOR_RESIZE_SCRIPT_TARGETS](EDITOR_RESIZE_SCRIPT_TARGETS.md), [EDITOR_RESIZE_PLAYERS](EDITOR_RESIZE_PLAYERS.md), [REGION_CROP_SCOPES](REGION_CROP_SCOPES.md) |
| M10 | **проверить вручную** | История и уведомление об ошибке Undo | Ctrl+Z/Y, дерево/глубина/default/Cancel; согласованные pause/scenario/object Undo. Окно ошибки: EN/RU/OK/Close/Escape/remember/reset, только управляемая имитация через harness, без исчерпания реальной памяти; освобождение pen capture и сохранение прежней истории. | [EDIT_HISTORY](EDIT_HISTORY.md), [HISTORY_MEMORY_NOTICE](HISTORY_MEMORY_NOTICE.md), [ручной режим M10](M10_MANUAL_NOTICE.md) |
| M11 | **проверить вручную** | Объекты, существа, двери, ловушки, specials | Палитра/свойства/кисть/замена/равная правка/удаление, физический стек и запросы; визуальные изображения, Cancel/Apply/Undo и результат в игре. | [EDITOR_OBJECT_PAINTING_CALLERS](EDITOR_OBJECT_PAINTING_CALLERS.md), [INSPECTOR_OBJECT_REMOVAL](INSPECTOR_OBJECT_REMOVAL.md), [UNDER_CURSOR_REMOVAL](UNDER_CURSOR_REMOVAL.md) |
| M12 | **проверить вручную** | Области, патрули, проходы и генераторы эффектов | Point/rectangle picking с Hide/Show/Escape; локальные списки/циклы/выбор точки/пауза 0–250; косвенное предупреждение и удаления; nested gate generation parent Cancel/Apply, capacity/approval, цвет/шаблон. Проверить фокус и фактические TreeView notifications. | [REGION_GEOMETRY_PICKING](REGION_GEOMETRY_PICKING.md), [PATROL_AREA_SELECTION](PATROL_AREA_SELECTION.md), [PATROL_REMOVAL_REQUESTS](PATROL_REMOVAL_REQUESTS.md), [GATE_GENERATION_REQUESTS](GATE_GENERATION_REQUESTS.md) |
| M13 | **проверить вручную** | Отряды героев и 16 слотов | Выбор/редактирование участника, активный префикс, роли/цели/поведение/release actions; родительский Cancel/Apply, сохранение, официальный редактор и игра. Старое наблюдение изменения одного бита сохраняется, новое окно им не принято. | [HERO_BAND_ACCEPTANCE](HERO_BAND_ACCEPTANCE.md) |
| M14 | **проверить вручную** | Свойства карты, музыка, речь и текст | Имя 64/>64 и запрос Copy/Keep/Cancel; options/rewards, текстовые банки и preview; реальный внешний player/codec, заблокированный временный файл/cleanup и звук. Сравнить с официальным редактором. | [MAP_NAME_OVERFLOW](MAP_NAME_OVERFLOW.md), [MAP_LEVEL_OPTIONS](MAP_LEVEL_OPTIONS.md), [MAP_SPEECH_TEXT](MAP_SPEECH_TEXT.md) |
| M15 | **проверить вручную** | Игроки, AI, камеры и начальные ресурсы | Приватный дочерний draft/общий Cancel/один Apply; manual AI, предустановки, camera, gold capacity warning и player-order случаи; фактическая игра. | [PLAYER_SETTINGS](PLAYER_SETTINGS.md), [PLAYER_CUSTOM_AI](PLAYER_CUSTOM_AI.md), [PLAYER_GOLD_NOTICE](PLAYER_GOLD_NOTICE.md) |
| M16 | **проверить вручную** | Доступность, pools, alliances, групповые модификаторы | Групповое выделение, смешанные значения, выбор видов/средние/исключённые элитные строки, частная правка/общая отмена; source GUI и эффект в игре. | [MAP_CREATURE_MODIFIERS](MAP_CREATURE_MODIFIERS.md), [MANUAL_ACCEPTANCE_RU](MANUAL_ACCEPTANCE_RU.md) |
| M17 | **проверить вручную** | Глобальные переменные карты | Выбор известной переменной, значение/единицы/default, parent Cancel/Apply/Undo и игра. Whole-map reset при new/load не превращать в выдуманную команду existing map. | [MAP_GLOBAL_VARIABLES](MAP_GLOBAL_VARIABLES.md) |
| M18 | **проверить вручную** | Дерево условий/действий и параметры сценария | Создание/alias wizard/BackNext, dependent choices и hints, ветки AND/OR/next, состояния раскрытия, point/condition/action delete requests; Cancel/Apply/Undo, сохранение и выполненный сценарий в игре. | [SCENARIO_REMAINING_PARAMETER_PAGES](SCENARIO_REMAINING_PARAMETER_PAGES.md), [SCENARIO_DELETION_REQUESTS](SCENARIO_DELETION_REQUESTS.md), [SCENARIO_ENTITY_CAPTIONS](SCENARIO_ENTITY_CAPTIONS.md) |
| M19 | **проверить вручную** | Глобальные определения и jobs callbacks | Пройти обычные/составные страницы 14 семейств: выбор/reset/частный draft/родительский Apply. Jobs: переход со суммой 37, Continue/Cancel/remember/reset, клавиатура/звук/момент записи; level modifiers и результат в игре. | [AUTOMATIC_CLOSURE_INVENTORY](AUTOMATIC_CLOSURE_INVENTORY.md), [CREATURE_JOBS_WARNING_REQUEST](CREATURE_JOBS_WARNING_REQUEST.md), [GLOBAL_DEFINITION_RULES](GLOBAL_DEFINITION_RULES.md) |
| M20 | **проверить вручную** | Менеджеры, статистика, настройки и навигация | Проверить фактические меню/клавиши, selected/all и search/filter, переход по объекту/сердцу, pending field commit и single-list membership; отображение counters/time/save после команд и смены документа; доступность Config/reset controls. General search в source — stub. | [OBJECT_MANAGER_SESSION](OBJECT_MANAGER_SESSION.md), [MAP_STATISTICS](MAP_STATISTICS.md), [SEARCH_SOURCE_SCOPE](SEARCH_SOURCE_SCOPE.md) |
| M21 | **проверить вручную** | Вид карты и системные окна | Minimap drag/click/visibility/sizes,11 zoom presets/User, fullscreen/hover/right-click, thumbnail37 options/BMP export; реальные Open/Save/colour dialogs, мышь, разные мониторы и DPI100/125/150. | [MANUAL_ACCEPTANCE_RU](MANUAL_ACCEPTANCE_RU.md), [FEATURE_PARITY](FEATURE_PARITY.md) |
| M22 | **проверить вручную** | Библиотека путей камеры | Native RU125% библиотека/52номера и DKM EN125% unknown9999/Search050/portal/Cancel/Apply/UndoRedo/Save/compile7 приняты. Остаются физическая клавиатура, живые вложенные Apply/stale context, другие схемы/DPI, официальный GUI и движение камеры в игре. [Evidence](CAMERA_SEARCH_LIVE_20261103.md). Редактор траекторий в исходнике отсутствует. | [CAMERA_PATH_SELECTION_SCOPE](CAMERA_PATH_SELECTION_SCOPE.md) |
| M23 | **проверить вручную** | EN/RU на настоящих окнах | Все окна M01–M22: смена языка с набранным draft/выбором, читабельные подписи/значения/иконки, Tab/Enter/Escape/default button, отсутствие обрезки, контраст и доступность на DPI. Это общая приёмка, не повтор каждого CLI-теста. | [MANUAL_ACCEPTANCE_RU](MANUAL_ACCEPTANCE_RU.md) |

Агент не использует Computer Use и не запускает редактор/игру. Ручную приёмку
выполняет пользователь по отдельным коротким инструкциям. Только собственные
копии, исходники/эталоны/установленные карты не исправляются. Все игровые
проверки — ровно семь файлов с именем `DK2Preview_6a88240eaf814f78` и командой
`DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78`.

## Что уже закрыто и не возвращается в исправления

- **I14 закрыт17 октября:** пользователь отдельно согласовал полное Undo
  областей, проходов и генераторов с прежним порядком.
  [Решение и действующий маршрут](REGION_UNDO_POLICY.md).

- Согласованные отличия: Undo паузы и параметров сценария; полное восстановление
  предметов с порядком и целью кристалла; полный откат при нехватке памяти Undo
  с понятной ошибкой, без частичного применения и продолжения без Undo.
- R01/R03/R04/R05 реализованы; политика R02 выбрана пользователем. Их окна
  остаются в M07/M10/M12, не в списке отсутствующего кода.
- Шесть owner scopes, gate IDs/derived targets, crystal routes, crop и used-area
  requests, source category retention, inspector deletion, scenario deletion,
  BigMap/gold/name requests, copy lifetime, generator requests и memory notice:
  прежние успешные доказательства принимаются в их фактическом объёме.
- 262 поля 14 страниц,73 числовых формата,22 catalogue bindings,103 схемы событий,
 127 описаний,100/138 numeric projections,184 definition images/116 shot-object
 images,39 cursor footprints — не отсутствующие функции и не очередь повторов.
- General search — исходный stub; AREASIZE0x80 не имеет активного caller;
  авторский мастер metadata, редактирование траекторий камеры, произвольные
  неизвестные variables и whole-map reset существующей карты не добавляются
  под видом переноса функций оригинала.
- 63 известных отказа на 27 картах уже разобраны:9 отсутствующих tile комплектов,
 6 усечённых Variables,8 карт с zero/duplicate script IDs и 4 с dangling links.
 Это не 63 дефекта приложения; никаких ремонтов/перенумерации карт не требуется.
 См. [разбор](CURRENT_CORPUS_REFUSALS.md).

## Правило проверок и перехода к ручной части

1. Сначала закрыть оставшиеся I01–I18 (кроме согласованного I14) по source/evidence, объединяя реальные изменения.
   Найденное совпадение закрывается ссылкой на существующее доказательство
   или результатом чтения кода, без повторного зелёного прогона ради счётчика.
2. После изменения проверять затронутый маршрут; полный корпус запускать,
   когда изменение Core или новая неопределённость требует интеграционной
   проверки. Не повторять успешные проверки без изменения или нового основания.
3. Документация/перечень/переупаковка документации не требуют build/publish,
   полного корпуса, WPF или запуска игры. Этот перечень составлен без них.
4. Когда I-пунктов не осталось, автоматический остаток закрыт в зафиксированном
   объёме; M01–M23 остаются пользовательской приёмкой. Обнаруженная там ошибка
   открывает конкретный дефект с воспроизведением, а не всю прежнюю очередь.
5. Закрытие — дата, фактический результат и ссылка на доказательство/ответ
   пользователя. Без такого результата статус не меняется. Полная цель не
   завершена, пока требуемые M-пункты не приняты.

Последний полный результат —1840 PASS/63 прежних отказа,
CreatureLevelModifierWindow на Core `d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`.
Все1839 прежних orderedPASS/63rawFAIL сохранены, original94 сопоставлен целиком.
Build04 all41/focus02/publish01 exit0.2781 EN/RU literals; новое окно и4WPFsuite
толькоcompiled, физическая приёмка открыта. I12/I14/I17 закрыты по коду;
карты не ремонтировались. [Подробности](CREATURE_LEVEL_MODIFIER_WINDOW.md),
[CURRENT_STATUS](CURRENT_STATUS.md).

## Покрытие всех 23 возможностей

Номер совпадает с REMAINING_WORK. Пустая графа I означает только ручную приёмку,
а не удаление возможности из цели. Графа M всегда остаётся до результата.

| № FEATURE_PARITY | Исправить / завершить сверку | Проверить вручную |
| --- | --- | --- |
| 1 | I01, I02, I04, I06, I18 | M01 |
| 2 | — | M02 |
| 3 | I02 | M03 |
| 4 | I02, I03, I04 | M04 |
| 5 | I07, I09 | M05 |
| 6 | I07 | M06 |
| 7 | I08 | M07 |
| 8 | I09 | M08 |
| 9 | I01 | M09 |
| 10 | — | M10 |
| 11 | I04, I06, I18 | M11 |
| 12 | I01, I09 | M12 |
| 13 | I02, I08, I18 | M13 |
| 14 | I02 | M14 |
| 15 | I02, I04, I18 | M15 |
| 16 | I04, I18 | M16 |
| 17 | — | M17 |
| 18 | I01, I03, I04, I06, I10, I18 | M18 |
| 19 | I06 | M19 |
| 20 | — | M20 |
| 21 | — | M21 |
| 22 | — | M22 |
| 23 | I10 | M23 |


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
