# Пять команд настройки кисти объектов

ObjectBrushCommands-20261018, часть I17. Источник — Window_Command:
IDM_MODIFYCREATURE/DOOR/TRAP/OBJECT/MAGICALOBJECT вызывает Things_Select
с соответствующим MapImm*. Things_Selection.c копирует исходный MAPTHING
в приватный Context; Cancel возвращает0, принятие копирует результат обратно.
После принятия MainWindowProc выбирает соответствующий режим размещения.
Настройка шаблона сама не вызывает Things_Paint и не требует подходящей клетки.

В «Правка → Настройка кисти объектов» появились пять EN/RU команд: существо,
дверь, ловушка, предмет и особый предмет. Каждая передаёт явный PlacedThingKind
через MainWindow.SourceObjectBrush_Click → EditorWorkspace.RequestObjectBrush.
Текущие TemplatePicker, выбор Inspector или предыдущий вид кисти не подменяют
этот аргумент. Без карты пункты отключены; живой карандаш, чужой expectedOwner,
закрытый MainWindow и workspace lifetime запрещают вход/принятие.

## Приватный шаблон и принятие

Используется существующий ObjectBatchWindow в отдельном режиме template-only.
Это прежние поля и named/image selectors; отдельного дублирующего редактора
параметров нет. Вид заблокирован, размещение/preview/координаты/upgrade скрыты.
Кнопка «Использовать как кисть» и Enter принимают только текущий вид:
целые payload поля читаются invariant, owner0–7 и named type проверяются по
каталогу текущей версии. Неизвестный вид/ID и неверный ввод не включают кисть.
При смене EN/RU выбранный вид и черновик сохраняются. Полнота внутренних
source controls/диапазонов рассматривается в прежних I13/I17, не объявляется
закрытой лишь по наличию входа.

Принятие сохраняет полный шаблон только выбранного вида, показывает карту
и включает PlaceObject. Файлы карты, счётчики, Undo/Redo, выделение и внутренний
буфер при этом не редактируются. Сам объект появится после явного щелчка по
карте через прежний PaintSingleObject/ApplyEditorObjectPainting. Отказ в
размещении, замены, scripts, terrain, owner и upgrades остаются в прежних
планах; настройка кисти не обходит их. Внутри template-only режима BuildPreview
не строит placement, ApplyPreview принимает шаблон и не записывает объект.

Каждое окно получает deep-copy словаря пяти шаблонов. Cancel/Escape/Close,
замена карты и закрытие окна не публикуют его draft. При принятии копируется
только принятый вид; остальные шаблоны остаются прежними. Обычный workspace
single-placement editor теперь также работает на копии: явный успешный Apply
или UseBrush публикует принятый шаблон, Cancel его не публикует. Это устраняет
скрытую перезапись source defaults отменённым single-placement draft.
Multi-manager session preferences этим изменением не переписаны.

## Доказательства и граница

Source SHA256 и таблица пяти caller → kind → template сохранены в
artifacts/Evidence/ObjectBrushCommands-20261018/source-scope.json.
Core и его reader/writer/painting semantics не менялись. Полный корпус,
прежние успешные CLI checks, renderer, WPF, Computer Use и игра не запускались.
Здесь менялись App routing/private UI drafts; проверки, требующие настоящих
окон, оставлены для пользователя.

Подготовлены четыре native/project × EN/RU WPF cases, каждый для пяти видов:
actual MenuItem, exact kind, no placement preview, принятие на solid/protected
cursor без записи карты, independent defaults, отменённый draft, неверный ID,
неизменный drawing tool после Cancel, pen/stale/closed owner, live language,
generic single-placement Cancel, exact bytes/history/selection/buffer.
Они **только скомпилированы**, не доказательство выполнения UI.
Build01 нашёл nullable dereference только нового smoke; исправлено до финальной
сборки. Неуспешный лог сохранён. Финальные build/publish — в exit records.

Пять MODIFY* снимаются из остатка главных команд I17. Там остаются справка и
сводка версий секций, а также конечная сверка собственных callback/shortcut
routes. Верхние17 I/23 M группы не превращаются в новые произвольные задачи.

## Ручная приёмка M11/M20/M23

1. Выбрать terrain-инструмент. Через «Правка → Настройка кисти объектов»
   последовательно открыть пять видов. Даже при другом выборе Inspector окно
   показывает именно выбранный вид и не позволяет его переключить.
2. Изменить owner/параметры, нажать Cancel; повторно открыть: прежний шаблон.
   Принять параметры: включается кисть объектов, сама карта ещё не изменена.
3. На подходящей клетке разместить объект, Ctrl+Z/Y. Настройка кисти не
   добавляет отдельного шага, размещение использует прежнее атомарное Undo.
4. Проверить независимость пяти шаблонов, invalid input, Escape/Close,
   смену EN/RU, выбор изображений, фокус Enter и layout на DPI100/125/150.
5. В обычном single-placement окне изменить параметры и отменить: принятая
   кисть сохраняется. Повторить принятие Apply и UseBrush; каждое сохраняет
   принятый шаблон, а карта меняется только от явного размещения.

Игровой тест по-прежнему использует ровно семь файлов
DK2Preview_6a88240eaf814f78 и DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78.
