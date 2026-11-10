# Запросы генератора ворот

Живой частичный результат1ноября: [GATE_EFFECTS](GATE_EFFECTS_20261101.md). Он уточняет историческое «не выполнялось» ниже только для перечисленных сценариев.

17 октября 2026, GateGenerationRequests-20261017. Реализованы request routes
R03/R04 из [сверки запросов](SOURCE_REQUEST_AUDIT.md). Сама генерация уже
существовала; теперь её workspace и дочернее окно свойств используют общие
source вопросы, запоминание ответа и сброс. Прежняя checkbox-подтверждалка
удалена из обоих окон; действующие Core API генерации сохранены.

## Подтверждение

Предпросмотр показывает число новых ворот, областей, условий и действий.
Кнопка генерации/сохранения в черновик открывает Yes/No с этими числами,
default Yes, source question icon и размещением у курсора. No/Close/Escape
не применяют план. Галочка «не показывать до закрытия» запоминает только
Yes; при ней No disabled, но Close/Escape по-прежнему отменяет вопрос.
Условия продолжают поддерживать современное event-only восстановление.
Отсутствие работы остаётся статусом NoWork, отдельное подтверждение не нужно.

REQUEST_GATESADD0x8000 хранится в общем EditorRequestPreferences текущего
workspace, который передаётся в RegionPropertiesWindow и дочерний генератор.
Запомненный Yes в одном маршруте подавляет следующий вопрос другого.
Автоответ проходит те же проверки актуальности. Ответ только даёт согласие:
изменение выполняется caller после закрытия request lease. Сам ответ не меняет
байты, счётчики или Undo. В дочернем окне результат попадает в независимый
родительский черновик; Cancel родителя оставляет карту прежней, а runtime
предпочтение остаётся вне Undo, как исходный Config.RequestsFlags.

Согласие связано с конкретным plan, document revision/fingerprint, caller
context и input revision. Изменение карты, raw bytes, edit+Undo, live pen,
открытая транзакция, закрытие/смена владельца, родителя или child version
не позволяют применить устаревший ответ. При сбое owner callback начальная
lease освобождается. Workspace unload/reset-document закрывает открытый
вопрос; изменение параметров тоже закрывает его и очищает preview.

## Недостаточно областей или ворот

Исходный RectEx generator проверяет existing+needed>=256 отдельно для areas
и non-heart gates до выдачи IDs. Теперь план заранее считает те же AddGate/
AddArea/AddEvents predicates. 254+1 допустимо,255+1 — нет. Проверка выполняется
до Allocate, поэтому отчёт корректно различает нехватку областей, ворот и
обоих наборов одновременно. Сердца dungeon heart в число ворот не входят.

Типизированный GateGenerationCapacityException содержит только counts и
guarded lifetime. В дочернем replay сообщение привязывается к настоящему
родителю/child version, а не к временному клону, который строил preview.
Оно показывает source warning icon, OK и собственное запоминание0x4000.
Закрытие не запоминается; default OK может запоминаться. Ни OK, ни Close,
ни автоматический ответ не создают записи и не разрешают генерацию.
Невозможный plan продолжает отвергаться. Confirmation0x8000 и capacity0x4000
независимы; кнопка «Снова показывать вопросы и сообщения генератора» сбрасывает
эти два бита, сохраняя другие request preferences и историю.

Прежние safeguards для orphan references, malformed/duplicate IDs,
неоднозначных native bindings, bounds и protected perimeter остаются.
Пользовательский ответ не обходит их; такие ошибки не маскируются сообщением
о ёмкости. Generic ApplyHeroGateGeneration/AcceptRegionGateGeneration сохраняют
свои прежние контракты; более строгая revision-привязка относится к новым
интерактивным requests.

## Источник и проверки

Source/Sources/Things/Things_RectEx.c: preflight counts, REQUEST_GATESADDPB,
REQUEST_GATESADD, default1, icons/follow-mouse; Misc_Requests.c: auto reply,
последний button→0 и remember только default. Source/Includes/Requests.h
определяет два независимых бита. Источники не изменялись.

Новая CLI model group в обоих форматах проверяет Yes/No/Close, counts,
default-only remember, lease, exceptions/owner callback, shared flags/reset,
readonly bytes/history, Undo, owner/revision/raw/pen/transaction guards,
254+1/255+1 areas/gates/both, невозможность подтвердить capacity notice,
suppression без обхода лимита, private child/parent Cancel/version и lifetime
capacity replay. Текущие прежние generator/nested-generator группы прошли.
WPF smoke переключён с checkbox на actual request buttons; он собран, не запускался.

Build03 всех41 проектов:0 warnings/errors. Focus02, generator, nested-generator,
fresh official seven-file export и publish terminal0. Полный current-DLL
прогон1813PASS/63FAIL, terminal1: прежние1812 passes в том же порядке, один
новый group, raw Inputs/63 failures буквально совпали. Original94 полностью
сопоставлены:31 исправлено ранее, одно известное XXLevel14/wMainTrigger52
сообщение отличается. Новых отказов/потерянных passes нет; audit63/27 inputs/
resolver SHA проверены. Corpus НЕ зелёный и не фильтровался.

EN/RU2613 keys: десять новых на язык, все прежние2603 literal values
сохранены. Source/runtime ZIP и каждый entry, graph41, Source/Binaries1006,
installed maps4453, fixtures, старый TestMap и свежий official комплект из
семи файлов проверены. Evidence: artifacts/Evidence/GateGenerationRequests-20261017.

Редактор/renderer/игра не запускались. Физически проверить EN/RU dialog,
counts, default/follow-mouse, checkbox/No/Escape/Close, два независимых
remember/reset и общий результат workspace/child, закрытие/смену документа,
child Keep→parent Cancel/Apply/Undo и три capacity сообщения. Новые маршруты
не закрывают прочие Config/manager branches, R01/R02 Undo error policy,
все23 области/семь очередей и physical GUI/audio/system-dialog/hardware/DPI/
game gates. См. [REMAINING_WORK](REMAINING_WORK.md).
