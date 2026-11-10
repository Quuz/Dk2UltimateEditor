# Конечная таблица команд четырёх менеджеров

ManagerCommandScope-20261018, часть I17. Для Heros_SelectProc, RectEx_Proc,
RectEx_PathsProc и Triggers_SelectProc перечислены все46 различных numeric
control IDs и11 именованных notifications. Повтор ID в разных ветвях draw/
change/click сохранён назначением строки, не выдаётся за отдельную ошибку.
Полные parent bodies, текущие paths/symbols и hashes проверены inventory.py.
Это список действий parent windows, не доказательство всех полей вложенных
parameter pages: те остаются в существующих I10/I11/I12/I13 scopes.
Есть обработчик не означает физической приёмки mouse/key/focus/DPI.

| Менеджер | Source ID | Текущий результат |
| --- | --- | --- |
| Bands | 101 | Изменить отряд |
| Bands | 102 | Удалить с прежним confirmation |
| Bands | 103 | Добавить отряд |
| Regions | 101 | X1: MC05 закрыт, clamp0..Width−1 в private field |
| Regions | 102 | Y1: MC05 закрыт, clamp0..Height−1 |
| Regions | 103 | X2: MC05 закрыт, clamp либо Gate=X1/disabled |
| Regions | 104 | Y2: MC05 закрыт, clamp либо Gate=Y1/disabled |
| Regions | 105 | Swatch boundary; actual raster M21 |
| Regions | 106 | Boundary colour chooser |
| Regions | 107 | Text swatch; raster M21 |
| Regions | 108 | Text colour chooser |
| Regions | 109 | Private geometry pick |
| Regions | 112 | Area/effect options; Gate ShowGeneration |
| Regions | 113 | MC01 закрыт: полный private pre-add draft, repeated Add и один Undo |
| Regions | 114 | MC05 закрыт: X1 spinner ±1 и clamp |
| Regions | 115 | MC05 закрыт: Y1 spinner ±1 и clamp |
| Regions | 116 | MC05 закрыт: X2 spinner ±1, Gate disabled |
| Regions | 117 | MC05 закрыт: Y2 spinner ±1, Gate disabled |
| Regions | 121 | Single picker + inspector multi deletion; policies mapped |
| Regions | 122 | Изменить через общий session |
| Regions | 130 | Use selection в общем private Modify/Add draft; MC01 закрыт |
| Patrols | 101 | UI-only empty route |
| Patrols | 102 | Append |
| Patrols | 103 | Insert |
| Patrols | 104 | Point/route removal |
| Patrols | 105 | Pause; modern Undo approved |
| Scenarios | 103 | MC06 закрыт: explicit New Point → private type page, отдельный Open existing |
| Scenarios | 104 | Insert point; DKM list order, native N/A boundary |
| Scenarios | 105 | Modify/reassign owner |
| Scenarios | 106 | Point removal policy |
| Scenarios | 110 | Move point up; DKM order |
| Scenarios | 111 | Move point down; DKM order |
| Scenarios | 122 | MC03 закрыт: explicit root-tail append → общий wizard |
| Scenarios | 123 | MC03 закрыт: explicit child/sibling-tail condition append |
| Scenarios | 124 | Insert condition before selected incoming edge через клавишу |
| Scenarios | 125 | Modify condition through shared wizard |
| Scenarios | 126 | Condition/branch removal policy |
| Scenarios | 127 | MC03 закрыт: explicit child/sibling-tail action append |
| Scenarios | 128 | MC03 закрыт: explicit incoming action insertion/source root guard |
| Scenarios | 129 | Modify action shared wizard |
| Scenarios | 130 | Action/branch removal policy |
| Scenarios | 140 | Up |
| Scenarios | 141 | Down |
| Scenarios | 142 | Outdent (source Left) |
| Scenarios | 143 | Indent (source Right) |
| Scenarios | 153 | Aliases/variable/timer names |

## Notifications

| Менеджер | Source notification | Текущий результат |
| --- | --- | --- |
| Bands | TVN_SELCHANGED | Band selection and private member panel |
| Regions | LVN_GETDISPINFO | Read-only labels/list projection |
| Regions | LVN_ITEMCHANGED | Selection/highlight/action state |
| Regions | NM_DBLCLK | MC02 закрыт: actual row → общий private Modify, guards и точная цель |
| Regions | UDN_DELTAPOS | MC05 закрыт: source one-unit steps → common clamp |
| Patrols | TVN_SELCHANGED | Route/occurrence selection, availability, warning grouping |
| Scenarios | LVN_ITEMCHANGED | MC06 закрыт: owner combo/Open existing и отдельная New Point button |
| Scenarios | LVN_KEYDOWN | Root-point Insert/Delete through existing guarded callers |
| Scenarios | TVN_ITEMEXPANDED | MC04 закрыт: confirmed DKM expanded short14 + TwoWay state; native memory→DKM |
| Scenarios | TVN_KEYDOWN | Event Insert/Delete; condition insertion uses incoming edge and retained type |
| Scenarios | TVN_SELCHANGED | Selection/links/scope/type/availability projections |

## Исправленный маршрут дерева сценария

PreviewKeyDown ScriptTree подключён к существующим операциям. Delete на
event node вызывает общий branch removal/request policy, на point root —
общую point removal policy. Insert на event node создаёт условие перед
выбранной incoming connection, а не в её default ChildAnd. Insert на root
DKM использует существующее ordered point insertion. Native entity roots
не имеют stored Action Point list order: прежний явный отказ сохранён,
функция произвольного native упорядочения не объявлена реализованной.

Как source LVN/TVN_KEYDOWN, Control/Shift не меняют эти локальные действия;
Alt/Windows не захватываются. Поддерево не удаляется Delete другого контекста:
проверены вкладка2, выбранный node, current owner после CommitFields, lifetime
и отсутствие live pen. Ошибки malformed graph попадают в прежний локализованный
RecordOperation handler. Начатые текстовые поля не перехватываются.

Для Insert берётся сохранённый source category choice, а не произвольный
первый тип списка. Добавлен только readonly InitialType(signature) у существующего
ScenarioCreationContext; он не открывает draft и не пишет owner. Initial
TRIGGER_VARIABLE/ACTION_SETVARIABLE и тип после закрытия мастера совпадают
с прежним Context.Remembered. При несовместимом owner выбирается первый
разрешённый condition type; дальнейший мастер и scope guard остаются прежними.

## Конкретный остаток parent-команд I17

Все шесть групп MC01–MC06 ниже закрыты по коду внутри I17,
не новые верхние задачи и не новый процент. Подключённый вход не скрывает семантическое
различие; одинаковый конечный результат через extra edits не считается
доказательством одного source caller.

| ID | Остаток | Условие закрытия |
| --- | --- | --- |
| MC01 | **Закрыт по коду, RegionCreationDraft-20261018.** Общий pre-add draft, три входа, repeated Add и retained names/info/colours/options. | [Реализация и доказательства](REGION_CREATION_DRAFT.md); private nested generation, единая транзакция/Undo, Cancel unadded draft. Physical M12/M20/M23 открыты. |
| MC02 | **Закрыт по коду, RegionListModify-20261018.** Actual ListBox row double-click и кнопка имеют общий caller. | [Реализация и ручная инструкция](REGION_LIST_MODIFY.md): current row/payload/context/owner guards, private session, другой черновик сохраняется. Actual mouse/focus/DPI остаются M12/M20/M21/M23. |
| MC03 | **Закрыт по коду, ScenarioInsertionCommands-20261018.** Пять явных команд через прежний wizard. | [Доказательства и инструкция](SCENARIO_INSERTION_COMMANDS.md): root/child/sibling tails, incoming occurrence, native/DKM exact links/Undo. Physical M18/M20/M23 открыты. |
| MC04 | **Закрыт по коду, ScenarioExpansion-20261018.** DKM expanded short14 и общее TwoWay дерево, native presentation→DKM. | [Доказательства и ручная инструкция](SCENARIO_EXPANSION.md): no command/Undo/semantic dirty, exact7 game bytes, save/reopen. Physical M18/M20/M23 открыты. |
| MC05 | **Закрыт по коду, RegionCoordinateClamp-20261018.** Create/modify field clamp и единичные стрелки, gate mirror. | [Доказательства и инструкция](REGION_COORDINATE_CLAMP.md): independent X/Y extents, private fields/Cancel/Apply, Core/opaque import bytes неизменны. Physical M12/M20/M23 открыты. |
| MC06 | **Закрыт по коду, ScenarioNewPointCommand-20261018.** Explicit New Point, не Open existing; retained type/private wizard и дополнительные DKM player points. | [Доказательства и инструкция](SCENARIO_NEW_POINT_COMMAND.md): empty new target, previous points/events exact, source occupied-owner policy. Native no-list-order остаётся I01/I03; physical M18/M20/M23 открыт. |

Thing Manager controls/single membership уже сопоставлены в
[MANAGER_LIST_COMMANDS](MANAGER_LIST_COMMANDS.md).42 Config fields и15 flags —
[CONFIG_SOURCE_MAPPING](CONFIG_SOURCE_MAPPING.md).70 primary dispatch entries —
[MAIN_MENU_SOURCE_MAPPING](MAIN_MENU_SOURCE_MAPPING.md). Keyboard30/special
routes — [SOURCE_KEYBOARD](SOURCE_KEYBOARD.md), [SPECIAL_KEYBOARD](SPECIAL_KEYBOARD.md).
Ctrl+N/Ctrl+Y согласованы пользователем18 октября: новая карта/Redo. Не открывать
закрытые scopes заново из-за исторической фразы «остальные manager callers».

## Проверка и ручная приёмка

Build03 all41/focus01/publish02 exit0, zero warnings/errors. Build01 имел
недоступное internal имя только в новом CLI test; replaced by independent
literal source initial names, failed log сохранён. Новый focus проверяет
две категории/native/project, default names, active/closed draft retention,
wrong signature refusal и exact owner bytes/counters/history/revision.
Из Core менялся только новый readonly accessor; старый Create/Store/writer
код не переписан, что сравнивается с packaged baseline. Corpus и старые
checks не повторялись. Последний full1822/63 — исторический результат
DiskProgress на прежней Core, не full этой сборки.

Четыре native/project × EN/RU WPF cases только compiled: actual tree selection,
condition Insert wizard/cancel, branch/point Delete requests, ordered DKM
point Insert, no mutation before acceptance, active pen/Alt/context refusal,
complete removal/Undo and document close. UI/Computer Use/renderer/game не
запускались. Вручную M18/M20/M23 проверить Insert/Delete на событии и point
root, Cancel/accept requests, выбор shared incoming occurrence, невозможность
удаления скрытой tree selection с карты, фокус текстовых controls и DPI.

## RegionCreationDraft-20261018

MC01 закрыт по коду; MC02–MC06 открыты. Исторические результаты выше относятся
к прежнему этапу. Текущие полный прогон и поставка — [CURRENT_STATUS](CURRENT_STATUS.md).

## RegionListModify-20261018

MC01/MC02 закрыты по коду; MC03–MC06 открыты. Исторические результаты и прежние
счётчики выше не меняют текущий остаток. [Текущая поставка](CURRENT_STATUS.md).

## ScenarioInsertionCommands-20261018

MC01–MC03 закрыты по коду; MC04–MC06 открыты. Ctrl+N/Y закрыты явным ответом
пользователя «оставить как у нас»: новая карта/Redo. Исторические pending строки
не создают нового вопроса. [Текущая поставка](CURRENT_STATUS.md).

## ScenarioExpansion-20261018

MC01–MC04 закрыты по коду; открыты MC05/MC06. Ctrl+N/Y согласованы.
Исторические результаты выше не создают новых задач. [Текущее состояние](CURRENT_STATUS.md).

## RegionCoordinateClamp-20261018

MC01–MC05 закрыты по коду, parent остаток MC06. Ctrl+N/Y согласованы.
Исторические строки выше не создают задач. [Текущее состояние](CURRENT_STATUS.md).

## ScenarioNewPointCommand-20261018

MC01–MC06 parent table закрыты по коду. I17 сохраняет конкретное установленное
отличие Things Manager destination106/107/186/187: ранний source clamp против
raw modern fields. Оно не входит в четыре parent procedures этой таблицы,
относится к прежнему I17 и таблице MANAGER_LIST_COMMANDS. Следующий шаг —
исправление этих callbacks; неопределённой новой очереди нет.
[Текущее состояние](CURRENT_STATUS.md).


## ManagerDestinationCoordinates-20261018 — кодовое закрытие I17

Конечный объём этой таблицы включён в [завершённую сверку I17](I17_CODE_CLOSURE.md).
MC01–MC06 и destination106/107/186/187 закрыты по коду; Ctrl+N/Y согласованы.
Исторические pending строки выше не добавляют новых задач. Физическая
приёмка в M остаётся открытой.
