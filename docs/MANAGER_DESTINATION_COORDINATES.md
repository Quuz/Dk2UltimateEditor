# Координаты назначения менеджера объектов

ManagerDestinationCoordinates-20261018. Исправлено установленное отличие I17:
поля X/Y назначения в Things Manager больше не сохраняют выход за границы
до ручного исправления. X ограничивается 0..Width−1, Y — 0..Height−1 при
изменении поля. Кнопки ▲/▼ и Up/Down без модификаторов дают шаг ±1 без выхода
за карту. Подсказки и доступные имена используют существующие переводы EN/RU.

Использована уже проверенная RegionCoordinateRules, без второй реализации
числового разбора. ES_NUMBER typing допускает ASCII цифры. Пустой промежуточный
текст остаётся доступен для набора; unsigned prefix при принятии даёт число
либо 0, а session хранит принятое число. Поэтому пустое X и вставленное Y
«  7tail» дают назначение (0,7), как GetDlgItemInt(FALSE) в исходнике.
Если после дочернего окна принятое число не изменилось, исходный промежуточный
текст родителя сохраняется. На меньшей карте сохранённые координаты
независимо ограничиваются её размерами.

Один и тот же числовой draft используется для показа точки и исходной позиции
Add. Нет fallback1 для нечислового менеджерского X/Y. Add/Modify, options,
preview/confirmed Apply, request preferences и шаблоны остаются отдельными
прежними сессиями. Нормализация не касается координат импортированных records:
они по-прежнему доступны через inspector и не ремонтируются открытием менеджера.

На время выбора точки поля и стрелки блокируются. Клик меняет только private
navigationPoint, Cancel не публикует его. Confirm сначала проверяет текущий
token/owner/revision, затем переносит X/Y и числовой session; callback поля
во время navigating для этого не используется. Source RMB-ignore, явный OK,
возврат modeless/modal окна и stale cleanup сохранены. Closed/foreign owner,
request/options/command busy блокируют ввод стрелками и команды назначения.

## Исходник и исполненные проверки

- Source/Sources/Things/Things_Manager.c:145–151 — UpDown186/187 →106/107;
  215–225 — EN_CHANGE bounds; 444–447 — сохранённые координаты при открытии;
  251, 1029–1030, 1424–1425 — чтение видимых чисел для show/Add/Modify.
- Source/Sources/Misc/Misc_Display.c:554–565 — единичный шаг.
- App/Editing/ObjectManagerWindow.Navigation.cs — fields/spinners/read/picker;
  ObjectManagerSession.cs — числовой session и smaller-map bound;
  ObjectManagerWindow.cs — общий accepted destination для Add.
- ObjectBatchSession вынесен из ObjectBatchWindow.cs в отдельный канонический
  файл без изменения определения класса. Это позволяет исполнять настоящие
  App session types в CLI без загрузки WPF. Вторая копия классов не создаётся.

Новый CLI `--manager-destination-only` исполнен: реальный App session linked
в Validation, отдельные X/Y extents, empty/prefix/UINT overflow, smaller map,
неизменность template/options/bit modes/request preferences, другой workspace.
Native/DKM Add preflight получает правильную позицию без изменения map bytes,
history/revision/events; реальный Add в (8,9) остаётся одним Apply/Undo,
Undo/Redo/Undo восстанавливает bytes. Это не исполнение WPF callback.

Четыре WPF набора EN/RU × native/DKM только скомпилированы: actual fields и
RepeatButton, typing filter, дочерний Add, source (0,7), private picker Cancel,
Confirm→session→Add, language, pending picker и stale owner. Старый navigation
smoke обновлён под исходное принятие unsigned input, но не запускался.
Первая сборка не прошла из-за нового теста: ошибочное имя PlacedThings и
CS0649 для linked session fields. Тест теперь использует ObjectManagerEntries
и задаёт/проверяет сохранение этих реальных настроек. Warnings не подавлялись,
product ради теста не менялся; журнал первой сборки сохранён.

Core sources/DLL не менялись. Full не повторялся: последний фактический full
той же Core — ScenarioExpansion1831 PASS/63 прежних отказа, original94
сопоставлен целиком. Новый focus не прибавляется к историческому full.
GUI/редактор/Computer Use/renderer/audio device/игра не запускались.
Поставка и подтверждённые exit codes — CURRENT_STATUS и evidence этапа.

## Ручная приёмка M20/M23

1. На прямоугольной карте открыть любой из трёх менеджеров объектов. Ввести
   X/Y выше размеров: поля сразу показывают Width−1/Height−1. Проверить обе
   стрелки, удержание, Up/Down, 0, пустое поле, вставку и каретку.
2. Для пустого X и Y «  7tail» показать точку: камера идёт к (0,7). Открыть
   дочерний Add: его X/Y равны 0/7. Отмена дочернего окна не меняет карту.
3. Выбрать точку на карте: до OK старые поля сохраняются, стрелки недоступны;
   Cancel возвращает старые значения. После OK дочерний Add получает новую
   точку. Проверить RMB, Escape, скрытие/возврат и смену документа.
4. Проверить EN/RU, клавиатурный фокус, DPI100/125/150 и положение окна на
   другом мониторе. Применить один Add, выполнить Undo/Redo.

Конечная кодовая сверка I17 — [I17_CODE_CLOSURE](I17_CODE_CLOSURE.md).
Полный остаток — [OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md).
