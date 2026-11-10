# Клавиатурные команды исходного редактора

SourceKeyboard-20261018, часть I17. Прочитаны Main_Keyboard.c, MainMenu.ciKey
и все30 строк назначений Texts_Menus.c. Новые19 бесконфликтных сочетаний
подключены через pure EditorKeyboardRoutes и реальные MainWindow/workspace
callers. Ещё8 исходных сочетаний уже имели обработчики. Оба конфликта разрешены
пользователем: Ctrl+N — новая карта, Ctrl+Y — Redo. Исходный Find — stub без алгоритма поиска.
JSON с source hashes/каждой строкой и специальными keys — в evidence этапа.

| Source ID | Сочетание исходника | Текущий результат |
| --- | --- | --- |
| IDM_NEW | Shift + Ctrl + N | **подключено** — New |
| IDM_OPEN | Ctrl + O | **подключено** — OpenMap_Click |
| IDM_SAVE | Ctrl + S | **подключено** — RequestSaveShortcut |
| IDM_COMPILE | Shift + Ctrl + C | **подключено** — Compile |
| IDM_TEST | Shift + Ctrl + T | **подключено** — Test |
| IDM_EXECUTE | Shift + Ctrl + E | **подключено** — RunGame_Click |
| IDM_UNDOLAST | Ctrl + Z | **подключено** — Editor.Undo |
| IDM_COPY | Ctrl + C | **подключено** — Editor.RequestCopy |
| IDM_CUT | Ctrl + X | **подключено** — Editor.RequestCopy(cut:true) |
| IDM_PASTE | Ctrl + V | **подключено** — Editor.Paste |
| IDM_ROTATEP90 | Ctrl + R | **подключено** — RotateClockwise |
| IDM_ROTATEM90 | Shift + Ctrl + R | **подключено** — RotateCounterclockwise |
| IDM_SYMHORZ | Ctrl + Y | **согласовано пользователем 18 октября** — Ctrl+Y = Redo; исходное отражение доступно через меню |
| IDM_SYMVERT | Shift + Ctrl + Y | **подключено** — MirrorVertical |
| IDM_SELECTALL | Ctrl + A | **подключено** — Editor.SelectAll |
| IDM_SELECTNONE | Ctrl + N | **согласовано пользователем 18 октября** — Ctrl+N = новая карта; снять выделение можно через меню |
| IDM_FIND | Ctrl + F | **source stub** — Find_Select has no search workflow; SEARCH_SOURCE_SCOPE |
| IDM_RECT | Ctrl + f1 | **подключено** — Rectangle |
| IDM_RECTFILL | Ctrl + f2 | **подключено** — FilledRectangle |
| IDM_CIRCLE | Ctrl + f3 | **подключено** — Ellipse |
| IDM_CIRCLEFILL | Ctrl + f4 | **подключено** — FilledEllipse |
| IDM_LINE | Ctrl + f5 | **подключено** — Line |
| IDM_PEN | Ctrl + f6 | **подключено** — Pen |
| IDM_FILL | Ctrl + f7 | **подключено** — Fill |
| IDM_PUTCREATURE | Shift + Ctrl + f1 | **подключено** — PlaceCreature |
| IDM_PUTDOOR | Shift + Ctrl + f2 | **подключено** — PlaceDoor |
| IDM_PUTTRAP | Shift + Ctrl + f3 | **подключено** — PlaceTrap |
| IDM_PUTOBJECT | Shift + Ctrl + f4 | **подключено** — PlaceObject |
| IDM_PUTMAGICALOBJECT | Shift + Ctrl + f5 | **подключено** — PlaceSpecialItem |
| IDM_HELPCONTENT | f1 | **подключено** — Help |

## Поведение новых команд

Ctrl+Shift+N открывает прежнее создание карты; Ctrl+Shift+C — существующую
компиляцию, Ctrl+Shift+T — проверку, F1 — встроенную справку. Ctrl+R и
Ctrl+Shift+R вызывают существующие повороты; Ctrl+Shift+Y — вертикальное
отражение. У самих transforms прежние guards/Undo/история не переписаны.
Новые menu gesture captions показывают работающие комбинации.

Ctrl+F1..F7 выбирают Rectangle/FilledRectangle/Ellipse/FilledEllipse/Line/
Brush/Fill. Source Pen называется Brush в текущем DrawingTool; семантика
живого карандаша не заменена отдельным новым инструментом. Выбор возвращает
вкладку карты, фиксирует допустимое pending поле и отменяет выбор геометрии;
байты карты/Undo/selection/clipboard не редактируются.

Ctrl+Shift+F1..F5 выбирают Creature/Door/Trap/Object/SpecialItem. Как
Toolbar_ChangeMiscMode, первое нажатие выбирает ранее принятый template
или source default без диалога и без размещения. Повторное нажатие для
того же активного вида вызывает прежний RequestObjectBrush, с приватным
Cancel/accept. Выбор не берёт другой kind из Inspector. Следующий paint
использует прежние Core plans. Source array/defaults в памяти, не JSON.

Новый resolver требует точного состава Control/Shift, Alt/Windows не
перехватывает. MainWindow adapter работает только на активном enabled owner
после существующего исключения TextBox/PasswordBox. Чужой/закрытый документ
и live pen не принимают смену tool/brush. Низкоуровневый public caller нужен
для подготовленной интеграции; реальный PreviewKeyDown применяет focus gate.

## Согласованное разрешение конфликтов

Пользователь выбрал «оставить как у нас»: Ctrl+N создаёт новую карту,
Ctrl+Y выполняет Redo. Оба конфликта закрыты; менять обработчики не требуется.
Исходные ClearSelection и MirrorHorizontal доступны из меню.
Ctrl+Shift+N также создаёт карту согласно исходному назначению.

[Три Insert callers и восемь scroll families подключены](SPECIAL_KEYBOARD.md).
Source quick windows сохраняют при закрытии, без смены инструмента; прокрутка
следует source page/end и обновляет прежний map pointer/pen. Это уже не
кодовaя очередь; actual focus/capture/raster остаются M20/M21/M23.
Delete/plain/Shift/Ctrl сохраняет прежний UNDER_CURSOR_REMOVAL.

## Проверка и ручная приёмка

Build02 all41/focus01/publish01 exit0, zero warnings/errors. Build01 нашёл
неверное имя DrawingTool.Pen в новом adapter; исправлено на существующий
Brush, неуспешный лог сохранён. Linked-App focus проверяет таблицу19,
все7 tool/5 placement chords, точные modifiers, Alt refusal, отсутствие
захвата конфликтных/неприсвоенных клавиш и source Find stub.
Core/файловый формат не менялись; corpus и старые checks не повторялись.
Новые4 native/project × EN/RU WPF cases только compiled: real adapter,
tool/placement selection, repeated-kind dialog, no data/history changes,
focus-independent API guards, live pen, language и closed owner.
Actual UI, renderer, Computer Use и игра не запускались.

Вручную M20/M21/M23: на карте без активного текстового поля проверить
Ctrl+F1..F7, Ctrl+Shift+F1..F5 и повторное нажатие; Cancel должен сохранять
кисть. На тестовом выделении проверить оба Ctrl+R и Ctrl+Shift+Y с Undo.
Проверить F1 и разрешённые Create/Test/Compile через source combinations,
EN/RU, фокус полей/диалогов, key repeat и DPI. Сам выбор инструмента не
должен добавлять шаг истории. Ctrl+N/Y до решения пользователя не заменять.

## SpecialKeyboard-20261018

[Source special keys реализованы](SPECIAL_KEYBOARD.md), поэтому прежний
keyboard inventory с3/8 незакрытыми callers — исторический. Текущий snapshot
с нулевым special остатком и2 pending conflicts сохранён в evidence SpecialKeyboard.

## ManagerCommandScope-20261018

[Конечная таблица внутренних parent controls](MANAGER_COMMAND_SCOPE.md):46 IDs,
11 notifications. Tree Insert/Delete подключены; осталось6 конкретных групп
MC01–MC06 внутри I17 и2 pending shortcut conflicts. Общая историческая фраза
«другие manager callers» не добавляет новую очередь вне этой таблицы.

## Решение пользователя 18 октября 2026

Ответ «оставить как у нас»: Ctrl+N создаёт карту, Ctrl+Y выполняет Redo.
Оба конфликта закрыты явным выбором человека. Исходные действия доступны
через меню. Исторические слова pending/ожидается выше относятся к прежним этапам.
Повторный вопрос или изменение этих клавиш не требуется. Код клавиш не менялся.


## ManagerDestinationCoordinates-20261018 — кодовое закрытие I17

Конечный объём этой таблицы включён в [завершённую сверку I17](I17_CODE_CLOSURE.md).
MC01–MC06 и destination106/107/186/187 закрыты по коду; Ctrl+N/Y согласованы.
Исторические pending строки выше не добавляют новых задач. Физическая
приёмка в M остаётся открытой.
