# Счётчики и Undo команд выделения

25 октября: [I08 закрыт по коду](I08_CODE_CLOSURE.md): band selection и специальные copy/cut/paste rules сопоставлены. Global bands/members не являются spatial things; paste сохраняет буквальный terrain/object payload. I09 gate transforms и ручная приёмка остаются в [управляющем списке](OPEN_FUNCTIONS_RU.md).
14 октября2026. Команды Cut/Paste теперь используют собственные editor callers
с исходными правилами принятия. Общие `CutSelection`/`PasteSelection` сохраняют
changed-only/no-op поведение для raw/model операций.

## Что делает оригинал

`Select_Copy(Cut)` после выбора и согласия на замену буфера создаёт один
parent Undo и увеличивает MapChanges один раз. Для каждого снятого placed
объекта `Things_RemoveAtMapPosEx` увеличивает MapChanges ещё раз. Существуют
пять списков: creatures, doors, traps, objects, magical objects. Копирование
без Cut не меняет MapChanges и Undo.

`Select_Paste` после проверки selection/buffer также создаёт parent Undo,
увеличивает MapChanges один раз, удаляет объекты назначения через тот же
helper, а вставляет новые без дополнительных increments. Размер/правая нижняя
граница обрезаются, защищённые destinations пропускаются. Если source клетка
masked, объект назначения всё равно снимается до замены terrain на solid.

| Результат | Source increments | Undo |
| --- | --- | --- |
| Copy |0|0|
| Cut без удалённых объектов, даже при равных/защищённых bytes |1|1parent|
| Cut сNудалёнными объектами |1+N|1parent с child changes|
| Paste без target removals, даже при равных bytes |1|1parent|
| Paste сNtarget removals |1+N|1parent|
| Добавленные Paste объекты |0дополнительно|В том же parent|
| Отказ до принятия/Cancel buffer request |0|0|

Ни одна из этих source функций не увеличивает paint/click activity usage.
32-битный счётчик может обернуться; Undo восстанавливает сохранённое значение.

## Текущий маршрут

`CutEditorSelection`/`PasteEditorSelection` заранее считают реальные eligible
target removals и вызывают прежние generic operations внутри одной editor
transaction. Completion задаёт accepted1+removed, сохраняя Undo при равных
данных. Generic byte writers, ID/graph remapping, crop, protection и validation
не менялись. Невалидные bounds, несовместимый format, nested edit или active
pen не получают authority этой команды. Ошибка внутри transaction откатывает
bytes/count/history; уже завершённый commit следует общему notification policy.

Workspace CaptureSelection создаёт copy candidate до Cut и публикует буфер
после успешного применения. PasteLoadedSelection использует новый caller;
обычная вставка и DKZ-вставка в выбранную зону получают один механизм счёта.
Undo не очищает внешний copy buffer. Согласие/отказ на его замену остаются
в существующем owned request, до этих commands.

У modern editor остаётся atomic graph cloning и общий Undo полного изменения,
а не воспроизведение небезопасных source allocation failures. Эти обёртки
не меняют previously documented raw/graph behavior и не доказывают все83
nonpainting sites. Само наличие generic transaction не считается доказательством
исторического caller: для данного пути извлечены отдельные source functions.

## Проверки

Capture трёх original files сохраняет Select_Copy/Paste, removal helper,
Undo_StoreEntry,1+Nrecipe и отсутствие insertion/activity increments.
`EditorSelectionCallerChecks` проверяет32 комбинации: два формата ×Cut/Paste
×removed0/1/3/5 ×protected/editable. Все пять source categories представлены.
Проверяются полные before/after bytes, oneUndo/Redo/counter/notification,
unchanged activity, raw equal no-op, uint wrap, invalid/nested/format refusal,
clipped insertion без source extra count и history-depth0.

Новые случаи встроены в существующую selection-order Validation группу.
All41/build/focus/official/full результаты текущей DLL указаны в CURRENT_STATUS.
Evidence: `artifacts/Evidence/SelectionCallers-20261014`.
WPF smoke equalCut/Paste, buffer retention, declined request иEN/RU подготовлен
и только собран: окно/editor/renderer/game не запускались.

## Ручная проверка

1. На копии карты выделите2×2 клетки выбранного terrain, чтобы Cut не менял
   сам terrain. После Cut должен появиться один Undo и одно accepted изменение.
   Copy buffer заполнен; один Undo восстанавливает counter и сохраняет буфер.
2. Paste того же terrain в пустую равную область тоже даёт один Undo/accepted
   increment. После Undo/Redo bytes и итоговый counter должны совпадать.
3. Повторите с несколькими objects назначения: count равен1+число удалённых
   objects, а не число добавленных. Защищённая граница не снимает objects.
4. Откажитесь заменить непустой copy buffer при Cut: карта/counter/Undo/buffer
   должны сохраниться. ПереключитеEN/RU, проверьте меню и окно истории.

Игровой тест — только `DK2Preview_6a88240eaf814f78`,7файлов и
`DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78`.
Original GUI/confirmation/pointer/history acceptance, реальные historical
corpus и remaining selection/caller requirements остаются в полной цели.
