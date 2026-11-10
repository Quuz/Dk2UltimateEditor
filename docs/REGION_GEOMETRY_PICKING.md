# Выбор геометрии из частных свойств

Живой частичный результат1ноября: [GATE_EFFECTS](GATE_EFFECTS_20261101.md). Он уточняет историческое «не выполнялось» ниже только для перечисленных сценариев.

12 октября 2026. RegionPropertiesWindow получил кнопку «Выбрать координаты на
карте». Окно открывается modeless с единственным владельцем workspace; повторная
команда активирует существующий черновик, а при ожидающем выборе отменяет его.
Окно временно скрывается перед передачей фокуса карте. Проход выбирается одной
точкой, зона/эффект — прямоугольником, как source RectEx_ModifyRect.

## Частное состояние и отмена

RegionPropertiesPick — однократный ticket конкретного RegionPropertiesSession,
его версии и живого документа. Complete изменяет только Bounds в private values.
Ни Terrain, ни обычное выделение/clipboard, ни live fields, Undo/counter/activity
не меняются. Все другие pending form inputs сохраняются; controls заморожены на
время выбора. Общее Apply запрещено до завершения/отмены выбора.

Escape или замена режима выбора возвращает окно с прежним черновиком. Новый
результат попадает в поля координат и остаётся частным до Apply. Закрытие окна,
смена/выгрузка документа, SourceChanged, другой document или изменённая версия
draft блокируют поздний результат. Cleanup отписывает handlers и не возрождает
закрытое окно. Невидимый тестовый экземпляр не показывается автоматически.

Workspace проверяет lifetime повторно после Canvas.BeginRegionPick: завершение
прежнего live stroke может изменить документ во время смены режима. Такой draft
не получает нового orphaned picker. Сам Canvas используется через прежний
Begin/Complete/CancelRegionPick route; choice mode не рисует и не считает paint
операцию. Generic geometry editing API не меняется.

## Проверка

RegionGeometryPickChecks: native/project×area/gate/effect, source point versus
rectangle, повторный Begin, Prepare/Apply при active ticket, cancel/idempotence,
недопустимые bounds, deferred Apply/Undo, stale draft version, Edit+Undo, raw
mutation, foreign owner и Dispose. Проверяются полные live bytes, revision,
dirty, Undo/Redo, EditorChangeCount, activity payload и отсутствие Changed.
Core focus завершился с exit0. Доказательства:
`artifacts/Evidence/RegionGeometryPick-20261012`.

Физический mouse/focus/Hide/Show/Escape маршрут, выбор после смены zoom/DPI и
системные окна ещё не выполнялись пользователем. Статический command/lifetime
audit и compilation не заменяют это. Private gate-generation child также остаётся
отдельным незакрытым требованием общего window workflow.

Итоговая полная CLIregression:1801 passes/63 failures, terminalexit1;
все63 сообщения совпадают буквально и в полном порядке, all1800 previouspasses
retained. Все41 проекта собраны0warnings/errors; focused archive/retention/
conversion/activity checks exit0. Source/Binaries1006 и installed4453 unchanged.
StaticEN/RU34-key/actualroute audit passed; physicalUI/game acceptance not claimed.
