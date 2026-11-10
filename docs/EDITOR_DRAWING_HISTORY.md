# Рисование и история — текущий единый проект

25 октября: [I07 закрыт по коду](I07_CODE_CLOSURE.md); [компоненты заливки](EDITOR_FLOOD_COMPONENTS.md) теперь сравниваются по source identity. Датированные остатки ниже уточнены: кодовое размещение закрыто; transform/copy/cut/paste scope и ручная приёмка остаются в управляющем [списке](OPEN_FUNCTIONS_RU.md).
Состояние на14 октября2026. Основной код — `src/`, решение
`DK2MapEditor.slnx`, обычная сборка из корня через `./build.ps1`.
Готовое приложение — `artifacts/PreFinal/App`. Этот файл описывает текущую
реализацию; общие результаты и обязательные остатки находятся в
[CURRENT_STATUS](CURRENT_STATUS.md) и [FEATURE_PARITY](FEATURE_PARITY.md).

## Счётчики и история

Редактор различает четыре состояния:

| Величина | Назначение |
| --- | --- |
| `EditorChangeCount` |Обратимый32-битный счётчик принятых editor-действий, аналог исходного MapChanges|
| `GetHistory().ChangeCount` |Число современных завершённых операций, не число клеток или source вызовов|
| `EditRevision` |Инвалидирование ранее открытых планов и черновиков|
| Activity statistics |Накопленные использования paint/fill/random/Undo и другие исходные статистики|

Accepted paint-call может увеличить editor counter без изменения bytes:
все клетки уже равны, защищены или находятся вне выбранной маски. Принятый
жест при этом может сохранять Undo. Raw `Edit` и raw painting сохраняют свою
проверку changed/no-op; правила реальных editor callers задаются отдельно.

Undo восстанавливает полные сохранённые buffers и editor counter до операции;
Redo — результат и counter после неё. Activity statistics не откатываются
вместе с Undo: источник `Undo_Restore` увеличивает UsageOfUndo, а
`Undo_RestoreNode` восстанавливает MapChanges из записи. Отмена уже записанных
клеток выполняется через Undo, а завершение захвата карандаша удерживает их.

`HasChanges` учитывает editor counter, отличающиеся файлы и отдельные editor
metadata. Поэтому принятый равный paint-call может давать признак изменения,
а нулевое число изменённых клеток не доказывает no-op исходного редактора.

## Непрерывный карандаш

`MapCanvas.BeginLeftMapPointer` сначала проверяет начальную область и
записывает принятый LMB click. Для обычного Brush он создаёт
`NativeMapDocument.LivePenStroke`, вызывает Start и захватывает мышь. Move
пишет клетки каждого сегмента сразу; отпускание кнопки, Escape, потеря
захвата, изменение инструмента/документа завершают жест через Dispose.
Проверьте конкретный маршрут смены инструмента/карты в физической приёмке.

Исходные правила `Mouse_LeftButtonDown`/`Mouse_Moved`:

- До первого сегмента создаётся full-map Undo; первоначальный old-point равен0,0.
- В точке0,0 первоначальный paint-call пропускается; в другой точке выполняется
  один LINE-call, даже если bytes равны или защищены.
- Повторное движение в той же клетке ничего не рисует и не увеличивает usage.
- Каждое изменение клетки курсора вызывает один LINE, а не count по числу
  закрашенных клеток. Accepted equal/protected сегмент тоже считается.
- Windows mask использует исходный GDI LineTo и отдельно включает конечную
  клетку; одинарная точка отмечается SetPixelV.
- Source button-up только завершает pen: дополнительного последнего сегмента
  нет. Современный CompleteLeftMapPointer соблюдает это правило.

Core сохраняет before tiles/counter при Begin, блокирует конкурентные edits,
Undo/Redo/save и создание другого жеста, затем создаёт одну запись при Dispose.
Успешно начатый равный/защищённый жест тоже сохраняет запись. Зарезервированный
жест, который не был начат, не создаёт Undo. При нулевой глубине истории
рисование действует, snapshots не удерживаются. Callback завершения может
закрыть захват после принятого кадра; текущая модель проверяет такую ситуацию.

Source рисует только editable клетки; native reserved bytes сохраняются,
а protection mask/защищённая граница проверяются отдельно от хранимых terrain
значений. Native четвертый byte не переинтерпретируется как source discriminator.

## Размер курсора и специальные типы

Размер обычного карандаша — одна клетка. В source Mouse_UpdateCursor размеры
берутся из World/Rooms/Gates, а у объектов maximum фиксирован1×1. Кнопка
«Карандаш» не задаёт произвольный пользовательский radius.

39 доступных terrain/variant строк содержат размеры1×1/unbounded либо
специальную область. Locked world FE — внутренний защитный тип, в эти строки
доступного выбора не входит. Восемь областей требуют геометрический маршрут:
сердце5×5, два портала3..5×3..5 и пять gate-вариантов. Gate29 — обычный1×1.

`EditorTerrainRules.Footprint` хранит source minimum/maximum,0 означает
отсутствие верхнего предела. Fixed footprint и Brush/Fill специального типа
создают немедленный filled rectangle. У line/rectangle/ellipse специальный
minimum также переводит итоговый paint в filled rectangle. Свободный
LivePenStroke для такого типа недоступен. `CheckEditorPlacement` проверяет
начальный footprint и selection; последующие обычные записи пропускают
защищённые клетки. Подробности:
[terrain policy](EDITOR_TERRAIN_RULES.md),
[terrain placements](EDITOR_TERRAIN_PLACEMENT.md).

## Остальные фигуры

`PaintEditorGesture`/`EditEditorDrawing` проводят editor-рисование через
общий transaction, source acceptance counter и атомарный Undo. Прямоугольники,
эллипсы и линии используют source Windows masks, а random-режимы — исходный
порядок probability/prefilter. Redo воспроизводит сохранённый результат и
не выбирает random клетки повторно. Не-Windows software masks не заявляются
побайтовым эквивалентом Windows GDI; последовательность исторического PRNG
и физический input проверяются отдельно.

Fill, выделение, cut/paste и transforms имеют собственные source counters и
правила сохранения. Их наличие не доказывает совпадение каждого caller:
[caller scope](EDITOR_CHANGE_CALLERS_SOURCE_SCOPE.md),
[selection buffer](SELECTION_BUFFER.md),
[selection transforms](SELECTION_TRANSFORMS.md).

## Свежие доказательства и границы

`artifacts/Evidence/LivePenSource-20261013` связывает пять оригинальных файлов
с hash и сохраняет фактические Down/Move/Up/cursor/paint/Undo branches.
Независимый catalogue fixture проверяет39 строк ×2 формата:78 случаев размеров,
доступа free/geometric pen и unchanged bytes/history/counters. Existing CLI
модели проверили реальные кадры Start/Move, stationary/equal/limited/protected/
origin случаи, один Undo/Redo, disabled history, lifecycle и failed/reentrant
callbacks на текущем Core. Все source/build/focus шаги завершились exit0.

Core остаётся тем же, что у последнего полного ScenarioPrerequisites прогона:
`6a04e8a627b11e1d3b4f99320f2911b4d5222778098e1f401ca8d104edb6bf7c`.
Код продукта и published App в этом блоке не менялись. Обновлён только
source-backed CLI fixture/check и документация. Полный корпус не повторялся;
его ранее проверенные1811PASS/63ordered failures привязаны к этой же Core.
Этот standalone pen model не выдаётся за встроенную full Validation группу.

App route прочитан, но WPF/окна/renderer/игра не запускались. Физические
capture/timing/Escape/переключение документа, звук, DPI/мультимонитор, original
GUI и изменённая игровая карта ещё требуют пользовательской приёмки.
Сверка source pen-size/live-stroke маршрута этим блоком сужена; остальные
drawing/selection/caller/history requirements сохраняются полностью.

## Ручная последовательность

1. На копии карты выберите воду и карандаш. Зажмите левую кнопку и проведите
   несколько сегментов: клетки должны появляться во время движения.
   Отпускание заканчивает один жест; один Undo отменяет его целиком, Redo
   возвращает тот же результат.
2. Повторите по уже воде и по защищённой границе. Во время движения между
   разными клетками accepted counter меняется даже при равных bytes;
   неподвижный курсор не создаёт дополнительные сегменты.
3. Начните в0,0 и затем войдите во внутреннюю клетку; проверьте защёлкивание
   origin/границы, один Undo. Повторите Escape и потерю захвата окна: уже
   записанные клетки сохраняются до Undo, незавершённого жеста не остаётся.
4. Выберите сердце/портал/gate со специальным footprint. Brush должен создавать
   прямоугольник нужных размеров, не произвольный свободный след; граница и
   selection должны соблюдаться. Переключите EN/RU и проверьте историю/подписи.

Все игровые тесты используют только `DK2Preview_6a88240eaf814f78`, экспорт
ровно семи файлов и `DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78`.

## Исторический текст

Полный прежний документ сохранён с проверенным hash в
`Archive/historical-documents-20261014/EDITOR_DRAWING_HISTORY-before-pen-source-audit.md`.
Его Test-кандидаты, прежние наблюдения Running и команды восстановлений —
история доказательств. Архив не нужен для сборки единого текущего проекта.


## Текущие команды кисти объектов — 14 октября 2026

Кисть и одиночное размещение используют ApplyEditorObjectPainting: счётчик
1 + число удалённых противоположных объектов, один шаг Undo, принятие равного
обновления и одна статистика использования. Базовый ApplyObjectPainting
сохраняет прежние правила равного результата. Подробности и ручные шаги:
[EDITOR_OBJECT_PAINTING_CALLERS](EDITOR_OBJECT_PAINTING_CALLERS.md).
