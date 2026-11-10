# Сообщение о нехватке памяти для Undo

17 октября 2026, HistoryMemoryNotice-20261017. Реализован R01 из
[сверки запросов](SOURCE_REQUEST_AUDIT.md): informational REQUEST_UNDOERROR0x8,
OK, session-only remember и сброс в настройках истории. Текущий полный
rollback сохраняется. Для R02 пользователь 17 октября явно выбрал полный
откат операции и понятную ошибку. Исходные варианты0x20000 продолжения без
Undo и частичного применения намеренно исключены по этому решению.

## Что классифицируется как ошибка истории

OOM внутри подготовки snapshots, child metadata, delta arrays, history
publication capacity и Undo/Redo replay получает подтип
EditorHistoryMemoryException : OutOfMemoryException с исходным InnerException.
Обычный catch(OutOfMemoryException) продолжает его перехватывать. Само действие,
native crystal completion, drawing completion callbacks и postcommit observers
в эту границу не включены: их общая нехватка памяти не называется отказом Undo.

Новый подтип не меняет алгоритм rollback, прежние snapshot ownership/counters
или форматы. Изначальный root snapshot и child-stack allocation выполняются
до transactionOpen. При отказе после action прежние bytes возвращаются до
выхода finally. В pen неуспешный frame закрывается и возвращает bytes/change
count, сохраняя уже наблюдавшиеся revisions/activity. Replay подготавливает
копии и capacity до движения обеих history stacks. IsSafeToNotify становится
true только когда у соответствующего документа нет открытой transaction/pen.
Приватный working document не выдается через public exception properties.

108 существующих безопасно injected allocation boundaries теперь проверяют
именно этот подтип и завершённую очистку; обе history stacks/bytes/counters/
revision/dirty/Undo usage и recovery/save сохраняются. Полный настоящий heap
не исчерпывался. Ошибка обычного action или observer остаётся plain OOM.
Отдельно проверен secondary observer: основной edit уже committed, затем
второе действие observer не смогло подготовить историю. Оно даёт typed failure,
а первое изменение остаётся на карте и в Undo. Сообщение не утверждает, что
все предшествующие операции были отменены.

## Согласованная политика пакетных операций

Дополнительно проверены12 отказов подготовки metadata после изменения
рабочих bytes пакета: вставка, изменение и удаление по три объекта, native
и project, внутренний child и публикация parent. Все bytes, обе истории,
счётчики, revision и прежняя ветка Redo возвращены; Changed не публикуется.
Предыдущий шаг не отменяется. Повтор тем же plan успешен и отменяется целиком.
Это controlled fault injection, без настоящего исчерпания памяти. Общая
сверка остальных manager callers остаётся в ledger; небезопасные source
Continue/Stop ветки не являются требованием после выбора пользователя.

## Доставка сообщения

App.DispatcherUnhandledException обрабатывает только безопасный помеченный
подтип после разматывания event handler. Общие OOM/прочие исключения этим
handler не поглощаются. При уже идущем уведомлении новый notice не запускается
рекурсивно. MainWindow выбирает активного владельца и использует единый
EditorRequestPreferences с дочерними окнами. Already-handled committed warnings
сохраняют своё прежнее значение, не превращаясь в сообщение об откате.

В окне EN/RU — original Error32.ico, default OK, follow-mouse, текст ошибки
подготовки данных отмены и checkbox запоминания до закрытия редактора.
OK может запомнить0x8. Close/Escape только закрывают окно. Автоответ по
сохранённому биту ничего не меняет в карте и не очищает этот бит. Изменение
revision/владельца до ответа не запоминается. «Показывать ошибки Undo» в
настройках истории сбрасывает только0x8; глубину/содержимое истории не меняет.

Уведомление не retry/commit/Undo и не разрешение продолжить без Undo.
Для него предусмотрена отдельная неразрешающая notification lease: она может
показаться над уже ожидающим подтверждением, не отнимает его normal lease,
не принимает за него решение и блокирует выдачу новых command leases до
закрытия. Reset при чужом active request по-прежнему отвергается. Tests
проверяют сохранение original pending consent и отсутствие чужих изменений
в preference bits. Runtime flag остаётся вне карты, JSON и Undo.

При отказе pen UI очищает pointer state/capture перед доставкой ошибки и
не маскирует её cleanup exceptions. Region-removal dialog передаёт typed
failure через прежний Failure route. Patrol removal/pause закрывают failed
one-shot окно перед центральным notice; если операция уже принята, прежняя
CommitFailure ветка сохраняется. Дополнительная правка этих двух окон была
App-only после full; Core SHA не менялся, full не повторялся.

## Проверки и поставка

Source: Source/Sources/Map/Map_Undo.c Undo_PrintError (void, no veto),
Misc_Requests.c default-only remember, Requests.h0x8; original Error32.ico
встраивается в App. Source/Binaries не изменялись.

Новая native/project CLI group проверяет typed failure/inner identity,
post-unwind safety, ordinary/remembered OK, Close, shared preferences/reset,
readonly bytes/history/counters, stale/owner failures, nested notification
lease и сохранение runtime preference после Undo/Redo. DrawingHistoryChecks
проверяет прежние4 regressions/108 boundaries и новые plain/secondary observer
cases. WPF History smoke дополнен кнопками notice/remember/reset; собран,
не запускался. Настоящая нехватка памяти, renderer, редактор и игра не запускались.

Build04 all41:0 warnings/errors. Focus01, history03, official и publish02
terminal0 на текущей Core. Current full1814PASS/63FAIL, terminal1: прежние1813
passes raw/order сохранены, добавлена одна model group; Inputs и63 failures
буквально совпали. Original94 полностью сопоставлены:31 исправлено ранее,
одно известное XXLevel14/wMainTrigger52 сообщение отличается. Новых failures/
потерянных passes нет. Audit63/27 проверен по input/resolver SHA. Corpus НЕ зелёный.

EN/RU2619 keys: шесть новых/язык, прежние2613 literal values сохранены.
Source/runtime ZIP и каждый entry, graph41, Source/Binaries1006, installed
maps4453, fixtures и прежний/fresh official seven-file TestMap проверены.
Evidence: artifacts/Evidence/HistoryMemoryNotice-20261017.

Физическая EN/RU/follow-mouse/OK/close/checkbox/reset/delivery приёмка остаётся.
Для неё нужны управляемые имитации в GUI harness; CLI доказательства не
заменяют экран/курсор/DPI. Остальные manager/Config/native/compiler/
historical/compound/callback/counter маршруты, все23 области/семь очередей и
GUI/audio/system-dialog/hardware/DPI/game gates остаются в
[REMAINING_WORK](REMAINING_WORK.md).
