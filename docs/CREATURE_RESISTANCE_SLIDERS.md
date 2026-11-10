# Ползунки сопротивлений существ

CreatureResistanceSliders-20261019, часть открытого I11. Общая options-панель
EditorWorkspace, отдельная CreatureWorkspacePageWindow и страница Resistances
в общем CreatureGlobalsWindow используют одни четыре Slider с числовыми подписями.
Редактирование процентов через отдельный TextBox заменено исходным способом ввода.

| Поле | Source type combo | Source trackbar | Source numeric label |
| --- | --- | --- | --- |
| Res1 /Res1Rate |103 |104 |107 |
| Res2 /Res2Rate |113 |114 |117 |
| Res3 /Res3Rate |123 |124 |127 |
| Res4 /Res4Rate |133 |134 |137 |

Properties_GlobalInitCr задаёт всем четырём trackbar диапазон0…100 и частоту
отметок5. Properties_GlobalResistancesProc обрабатывает WM_HSCROLL:
TB_BOTTOM/LINE/PAGE/THUMBPOSITION/THUMBTRACK/TOP копируют TBM_GETPOS в подпись
соответствующего слота. Properties_GlobalChangeRes отключает slider и подписи
при None; значение не очищается. SaveChangesCr читает позиции всех четырёх
trackbar, включая отключённые. GetParamsCr отдельно задаёт thumb и raw label.

## Текущее поведение

Все четыре ползунка имеют диапазон0…100, целую позицию и отметки через5.
Перетаскивание обновляет только свою подпись и частный черновик. Форма,
ползунок и остальные значения при этом не пересоздаются. Left/Up уменьшают
на1, Right/Down увеличивают на1; PageUp/PageDown меняют на20, Home/End задают
0/100. MouseUp фиксирует явный выбор позиции даже на уже достигнутой границе.
Привязка направлений клавиш к исходным уведомлениям соответствует
[документации Win32 trackbar](https://learn.microsoft.com/en-us/windows/win32/controls/trackbar-controls).
Шаг1 — [стандартный line size Win32](https://learn.microsoft.com/en-us/windows/win32/controls/tbm-setlinesize).
Совпадение физического PageUp/PageDown и клика по дорожке с установленной
версией original common-controls ещё требует ручного сравнения; нынешний
page step20 записан явно, а не доказан запуском оригинала.

Список типов и прежние EN/RU названия сохранены. None отключает ползунок и
числовую подпись, удерживая pending rate. Повторное включение возвращает ту же
позицию. Ввод проверяет текущего editable владельца, выбранную строку,
неизменённый baseline группы, registered control identity и source None mask.
Событие от старого ползунка после смены языка не меняет новый черновик.

## Импортированные значения и граница source-поведения

Для raw255 thumb находится на100, но подпись остаётся255. Открытие, перевод,
смена типа и неизменённый Apply не исправляют импортированные байты: это
сохранённая политика текущего редактора. Source GetParams тоже задаёт raw label
отдельно от ограниченного thumb; однако source SaveChanges читает TBM_GETPOS
и может заменить импортированный255 на100 даже без движения. Мы сохраняем
неизменённый raw до явного действия пользователя.

Явная стрелка «уменьшить» при raw255 шагает от thumb100 к99; явный End либо
выбор100 фиксирует100. Изменение одного слота сохраняет остальные raw255,
включая None. Неизвестные imported type IDs остаются прежними и могут быть
исправлены существующим списком без нормализации остальных полей.

CaptureCreatureOptions удерживает сохранённую строку pending rate отдельно
от Slider.Value. Только действительное событие текущего ползунка либо явный
Scroll/Set API изменяет её. Это предотвращает неявную запись100 при обычном
снятии формы/переходе/Apply. Прежние UpdateCreatureOptions, native BYTE/DKM
storage, общий private parent, compiler и Undo не менялись.

## Проверки и границы

CLI `--creature-resistance-slider-only` проверяет четыре поля, проекцию thumb,
линейные/страничные шаги и границы, raw255/4294967295/−1, native/DKM private
контекст каждого слота, отдельные native offsets0xE0D/0xE0F/0xE11/0xE13,
сохранение остальных imported/None rates, один общий Undo/Redo и Cancel.
Старые успешные проверки storage/compiler без изменений не повторялись.

Подготовлены4 WPF suites EN/RU×native/DKM, только скомпилированные:
настоящие Slider/labels, диапазоны/отметки, целая drag position, None,
read-only label, сохранение identity, raw255 versus thumb100, старый контрол
после смены языка, standalone/shared формы, source keyboard directions,
Home/End на границе, stale owner, общий Apply/Undo. Прежние options/shared
parent suites адаптированы с TextBox на Slider; invalid rate101 больше
не является текстовым UI-вводом. Базовый строгий numeric API не менялся.

GUI, renderer, Computer Use, игра и audio device не запускались. Последний
полный корпус1840PASS/63 прежних отказа относится к Core этапа
CreatureLevelModifierWindow. Новый focused результат не прибавляется к нему.
Source/Binaries, fixtures, семифайловый TestMap и установленные карты сохранены.

## Ручная приёмка M19/M21/M23

1. Откройте Resistances: у четырёх слотов должны быть списки типов, ползунки
   0–100, отметки через5 и текущие числа. Проверьте EN/RU, resize/DPI и Tab.
2. Для активного типа перемещайте бегунок, используйте Left/Up, Right/Down,
   Home/End, PageUp/PageDown и клик по дорожке. Подпись должна обновлять свой
   слот; соседние значения и сам контрол должны сохраняться.
3. Выберите None: ползунок и подпись отключаются, значение остаётся прежним.
   Включите обратно. Повторите в отдельном и общем окне существ.
4. При imported rate255 thumb должен быть100, подпись255. Открытие/перевод/
   Cancel/no-op Apply не меняют байты. Явный End фиксирует100, уменьшение —99.
5. Измените несколько сопротивлений и другую страницу общего окна: Cancel
   оставляет карту прежней; общий Apply и один Undo отменяют все изменения.
6. Сравните физические drag/page/timing с исходным редактором. Подготовленные
   WPF suites не доказывают эти действия и не заменяют ручную проверку.

## Оставшаяся граница I11

Известный code gap «rate TextBox вместо Slider/label» исправлен. I11 целиком
не закрыт: следующая работа — конечная сверка selection/reset/child callers
четырнадцати страниц с существующими source comparisons. Числовые обработчики,
этот Slider и ранее проверенные codec/catalogue/Undo не считать заново
отсутствующим кодом. Перечень helpers уже ограничен inventory14 страниц;
новая неопределённая верхняя очередь не создаётся. Общие15I/23M и все23 области
FEATURE_PARITY сохраняются до завершения своих критериев.


Итог: validation-build-01/focus-01/build-02 всех41 проектов/publish-01 — ExitCode0. Финальная сборка без ошибок и предупреждений. Предфинальная поставка и оба пакета обновлены из единого проекта.4 WPF suites не исполнялись.


Текущее продолжение19 октября: [I11_CODE_CLOSURE](I11_CODE_CLOSURE.md). Конечная14-page callback сверка завершена; I11 закрыт по коду. Исторические слова «осталось»/staging/UI ещё не означают текущий отсутствующий код. Физическая M19/M21/M23 приёмка открыта.
