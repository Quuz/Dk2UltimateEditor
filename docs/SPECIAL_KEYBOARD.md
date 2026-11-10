# Специальные клавиши карты: Insert и прокрутка

SpecialKeyboard-20261018, часть I17. Конечные3 Insert callers и8 scroll
families из Main_Keyboard.c подключены; source hashes, маски и правила
закрытия — artifacts/Evidence/SpecialKeyboard-20261018/source-scope.json.
Это не закрывает все собственные manager/scenario callbacks I17 или ручную
приёмку клавиатуры, фокуса, геометрии и захвата мыши.

## Insert: параметры шаблона без смены инструмента

| Клавиши | Исходный caller | Окно / изменяемые поля |
| --- | --- | --- |
| Insert | Things_SelectObjective, ресурс6140 | Objective/Area/Enemy кисти существ |
| Shift+Insert | Things_SelectOptions(THING_OPTIONSCREATURES), ресурс6110 | CreatureOptions кисти существ |
| Ctrl+Insert | Things_SelectOptions(THING_OPTIONSHEROS), ресурс6120 | HeroOptions кисти существ |

Это небольшие окна параметров MapImmCreature, не полное окно настройки кисти.
В исходном RC у них нет кнопки Cancel. IDOK и IDCANCEL в обработчиках сохраняют
показанные значения. Поэтому современные окна имеют честную кнопку «Закрыть»:
Enter/Escape/крестик сохраняют параметры шаблона. Само окно прямо сообщает
это поведение. Оно отличается от полного source Things_Select, у которого
настоящий Cancel оставляет прежний шаблон; тот маршрут не изменён.

Creature page показывает шесть source bits1/8/16/32/64/128, hero page — четыре
bits1/4/2/8. При закрытии выбранное слово собирается заново, как source:
маски0xF9 и0x0F. Source-excluded/unknown биты этих конкретных слов не
сохраняются этим отдельным caller. Прочие template fields сохраняются.
Это не изменение bytes карты, global definitions или manager modify masks.

Objective page имеет десять исходных значений в порядке11,12,13,14,17,18,19,
22,23,27. Неизвестная исходная цель отображается первой11, как исходный radio
fallback. Только23 включает Area; при другой цели Area записывается0. В
списке областей используется порядок текущих PatrolAreas, отсутствующая
исходная Area выбирает первую, пустой список возвращает0. Enemy — исходные
индексы0..7; отсутствие выбранного индекса остаётся UINT_MAX, как CB_ERR.
Смена EN/RU сохраняет выбранные raw IDs, окно/подписи переключаются.

Workspace публикует результат только в шаблон Creature. Если активна кисть
существ, её параметры обновляются; если активен другой вид/terrain-инструмент,
он остаётся активным. Нет placement, смены вкладки, изменений bytes/счётчиков,
истории, выделения/буфера или записи JSON. Неверный/stale/закрытый owner,
замена документа или закрытие workspace не принимают старые параметры;
программный Abort закрывает окно без публикации. Новое окно не открывается
во время живого карандаша.

## Прокрутка в клетках

| Клавиши | Без модификатора | Shift | Ctrl |
| --- | --- | --- | --- |
| Up/Down | Одна клетка по Y | Страница по Y | Верх/низ |
| Left/Right | Одна клетка по X | Страница по X | Левый/правый край |
| PageUp/PageDown | Страница по Y | То же | То же |
| Home/End | Левый/правый край | То же | Верх/низ |

Shift+Ctrl и Alt/Windows special handler не принимает. Delete и его source
семейства сохранены в прежнем UNDER_CURSOR_REMOVAL. MainWindow adapter
передаёт Insert/scroll только при клавиатурном фокусе Canvas; текстовые поля,
dropdowns и дерево сценария получают собственный ввод. Это сохраняет границы
логических source окон, которые в современном редакторе могут быть вкладками.

Source Map_GetMapSquares даёт page=floor(viewportPixels/cellPixels)+1.
Map_Scroll* изменяет целочисленный origin и ограничивает его диапазоном
0..mapLength+1−page. Если карта короче page, операция не прокручивает её.
Например, viewport700/cell64 даёт page11, для карты20 клеток последний origin
равен10 клеткам, то есть640px, а не стандартным WPF580px.

Чтобы ScrollViewer допускал тот же origin, справа/снизу Canvas получает только
пустой layout margin, равный неполной видимой клетке. В примере это60px.
Width/Height Canvas, размеры клеток, map bytes, hit area и renderer не растут.
Margin пересчитывается после viewport/zoom/document change, без настроек карты.
Сам keyboard target кратен размеру клетки; начало после fractional minimap
offset приводится к source целой клетке. Пустая полоса не становится клеткой.

После действительного scroll обновляется положение реального указателя через
существующий MoveMapPointer, как source Map_Scroll* → Mouse_Moved. При живом
штрихе карандаша это может продолжить его на новую клетку под неподвижной мышью;
используется прежний LivePenStroke, его один шаг Undo и counters. Без штриха
прокрутка изменяет только view/cursor, не карту. Указатель вне viewport/карты
не дорисовывает край. Новая система рисования или второй pen не создаются.

## Проверка и ручная приёмка

Linked-App focus01 прошёл: три Insert routes, восемь групп scroll с modifiers,
refusal Ctrl+Shift/Alt, literal700/64/20 geometry, разные zoom/viewport/map
границы, cell-aligned target, точная связь padding/extent, все256 words с
верхними неизвестными bits для масок0xF9/0x0F и objective inactive-area rules.
Core и его pen/Undo/reader/writer не менялись; старые проверки и корпус не
повторялись. Последний full1822/63 — исторический DiskProgress на прежней Core.

Четыре native/project × EN/RU WPF cases только скомпилированы: фактические
windows/checkboxes/radios, close acceptance, template publication без смены
tool и bytes/history, stale Abort, nonsquare zoom, source scroll end/line,
Canvas size и owner lifetime. Build02 имел nullable error нового smoke;
исправлено до build03, лог сохранён. WPF/renderer/Computer Use/игра не запускались.

Ручная M20/M21/M23:

1. Дать фокус карте; проверить все строки таблицы, в том числе неполную клетку
   у края и масштаб17×63. В полях/дереве эти клавиши не должны прокручивать карту.
2. На копии карты начать штрих, прокрутить стрелкой при неподвижной мыши,
   отпустить и Undo: отменяется весь штрих одним шагом. Проверить захват мыши,
   pointer outside viewport, cursor/status/миникарту и изменение размера окна.
3. Insert/Shift+Insert/Ctrl+Insert: выбрать параметры и закрыть Escape/крестиком;
   открыть снова, значения сохранены. Активный terrain/tool не переключается,
   карта не становится изменённой от настройки шаблона.
4. Проверить EN/RU, цель23 и другой objective, пустую/непустую Area, DPI100/125/150.

В keyboard inventory больше нет неподключённых3 Insert/8 scroll callers.
Два конфликта Ctrl+N/Ctrl+Y всё ещё ожидают предпочтения человека; нынешние
New/Redo не заменены и не объявлены согласованными с исходником.
Остальные собственные manager/scenario callbacks остаются внутри I17.

## Решение пользователя18 октября

Ctrl+N = новая карта, Ctrl+Y = Redo, ответ «оставить как у нас».
Оба прежних pending conflicts закрыты; source действия остаются в меню.


## ManagerDestinationCoordinates-20261018 — кодовое закрытие I17

Конечный объём этой таблицы включён в [завершённую сверку I17](I17_CODE_CLOSURE.md).
MC01–MC06 и destination106/107/186/187 закрыты по коду; Ctrl+N/Y согласованы.
Исторические pending строки выше не добавляют новых задач. Физическая
приёмка в M остаётся открытой.
