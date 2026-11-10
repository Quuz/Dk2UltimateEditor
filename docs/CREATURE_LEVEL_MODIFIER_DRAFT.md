# I12: официальные controls и независимый черновик

Актуальное продолжение: [окно EN/RU и копия каталога](CREATURE_LEVEL_MODIFIER_WINDOW.md),
19 октября. Кодовая часть I12 закрыта конечной сверкой; M19/M21/M23 ещё открыты.
Далее — исторические результаты предыдущего этапа.

Актуальное продолжение: [общие значения и новые записи](CREATURE_LEVEL_MODIFIER_DEFAULTS.md),
19 октября. Ядро подключает defaults/reset/missing overrides к общему Apply;
EN/RU UI и экспорт общего каталога ещё открыты. Далее — предыдущий этап.

Актуальное продолжение: [частный контекст и DKM](CREATURE_LEVEL_MODIFIER_SESSION.md),
19 октября. Существующие map tuples уже подключены к Apply/Undo/save/compile;
shared/default/reset и EN/RU UI остаются открыты. Далее — предыдущий этап.

Актуальное продолжение: [запись native коэффициентов](CREATURE_LEVEL_MODIFIER_WRITER.md),
19 октября. Независимый writer уже добавлен; документ/DKM/UI остаются открыты.
Далее — результаты и границы предыдущего этапа.

19 октября 2026. Evidence folder: CreatureLevelModifierEditing-20261018
(начат18 октября). **I12 остаётся открыт.** Установлены реальные обработчики
official dialog238, диапазоны/порядок характеристик; реализован частный
Core draft. Окна продукта и writer native/DKM ещё не подключены.

## Что подтверждено в официальном EXE

Статический анализ установленного обычного DK2 Editor.exe, без его запуска:

| Обработчик / место | Подтверждённое правило |
| --- | --- |
| Message map00514060/78 | CBN_SELCHANGE для1841/1842 вызывает0046c520 |
|0046c520 | Сначала StorePrevious0046c550, затем LoadNext0046c680 |
|0046bf90 DDX | Все10 полей передают signed limits−10000/10000 в004e4b31 |
|004e4b31 | Signed comparisons jl/jle, отличные от unsigned validator рядом |
|0046c550 | Значения записываются WORD в массив9 уровней на характеристику и отдельный FirstPerson |
|0046c680 | Загрузка через movsx SHORT; поля окна — знаковые int |
|0046c320/0046c481 |32-entry catalogue; исключаются8,11,12,14,20,23,25,26,29; отображаются23 ID в исходном порядке |
| Message map005140c0 | OK12431 вызывает0046c800, который сначала сохраняет текущую страницу |
| Cancel0046c7f0 | Переход в обычный MFC Cancel; rollback уже сохранённых global page values этим обработчиком не доказан |

Локальные arrays: normal base005504a8, stride18 bytes на statistic,
элемент2 bytes; уровень2 находится по+4. Отдельный FirstPerson base005506ec.
Native loader0049f2c5–0049f2e7 тоже пишет WORD; исходные DWORD bits при
просмотре не нормализуются. Подтверждено внутреннее SHORT значение, но ещё
не соглашение native writer о верхних16 bits отрицательного DWORD.

Правильная привязка полей:

| Значение | Edit | UpDown |
| --- | --- | --- |
|Уровень2 |1847 |1848 |
|Уровень3 |1851 |1852 |
|Уровень4 |1853 |1854 |
|Уровень5 |1855 |1856 |
|Уровень6 |1857 |1858 |
|Уровень7 |1859 |1860 |
|Уровень8 |1861 |1862 |
|Уровень9 |1863 |1864 |
|Уровень10 |1865 |1866 |
|First Person |1867 |1868 |

Предыдущая расшифровка resource order с1851 как level2 была неверной.
Read-only wire decoder не зависит от control IDs и не менялся. Таблицы
документа CREATURE_LEVEL_MODIFIERS исправлены; старые этапы не объявляются
доказательством неверной привязки.

Capstone5.0.9 установлен только в evidence analysis-deps для чтения x86.
В product/source ZIP он не включается и для сборки не нужен. Сохранены
официальный SHA256, machine code disassembly и проверка прямых адресов:
official-callback-scope.json, official-control-xrefs.json, stats-dialog.asm,
stats-loader.asm, signed-validation.asm. verify-static.py проверяет10 DDV
вызовов, signed branches,10 store/load slots,23 captions/ID order и handlers.
Linear disassembly содержит padding/data; утверждения ограничены перечисленными
известными entrypoints/инструкциями, не всеми линейно полученными строками.

## Core draft

CreatureLevelModifierControls содержит23 исходных ID/English captions,
русские подписи, corrected control mapping, signed SHORT display и пределы.
Это indices статистик, не ID видов существ, не уровни доступности заклинаний.
Источник должен передать полную таблицу эффективных значений: draft не
подставляет выдуманные defaults. Он не владеет документом и не имеет writer.

CreatureLevelModifierDraft копирует строки в private values. Ordered duplicate
occurrences загружаются последовательно, как официальный loader; последний
SHORT определяет view. Полные original DWORD bits сохраняются отдельно.
Неизменный signed input не создаёт delta и не нормализует верхние bits.
Hidden IDs/неподтверждённые keys не редактируются и не попадают в Changes.

Store получает целиком9 levels + FirstPerson, проверяет форму и все10 границ
перед изменением private state. SelectStatistic сначала принимает прежнюю
страницу, затем меняет выбор; неверные значения оставляют и данные, и выбор
прежними. Changes выдаёт точные key/original bits/новый SHORT. Dispose закрывает
draft; дальнейшие read/write/select отказывают. Исходные строки независимы.
Значение за пределами GUI допустимо прочитать без ремонта, но принять такую
страницу нельзя до исправления. Наличие нового draft не разрешает обойти
native archive validation или писать неизвестный layout.

Современный private draft соответствует общему требованию частного редактирования
и отмены. Он **не объявлен** точной копией возможного побочного эффекта старого
Cancel после нескольких selection callbacks. Такая разница не скрывается за
успешным тестом; публикация родителя ещё не реализована.

## Исполненная проверка и поставка

Новый CLI `--creature-level-draft-only` прошёл: все23 IDs/ENRU/order,
10 control slots, signed bit patterns,230 изменённых cells, многократные
переходы,−10000/10000 и обе внешние границы во всех10 slots, неизменность
исходника и копирование входных arrays. Invalid page/shape/ID/missing source/
closed draft не применяются частично. Duplicate low-word/upper-bits no-op
и imported−32768 сохраняются без ремонта. Проверка не запускает GUI.

Все41 проекта собраны без ошибок/предупреждений; focus01/publish01 exit0.
Новый файл Core независим от существующего исполнения: прежние Core/App
sources побайтово совпадают с packaged baseline, текущие writer/Save/clone/
compiler/provenance APIs не изменялись. Full и старые green checks не повторены.
Исторический full1831/63 относится к прежней ScenarioExpansion Core; новая
DLL имеет отдельную фокусную проверку. Счётчики не смешиваются.

## Что ещё требуется для I12

1. Завершить map/shared effective composition/default/reset и native writer
   signed encoding/omission. Исходный global callback не равен готовой записи
   карты; Species Override и level percentages не смешиваются.
2. Подключить EN/RU окно и общий Apply/Cancel/Undo к рабочему документу.
   Для DKM хранить изменяемый delta отдельно от immutable native source;
   расширение должно участвовать в history/fingerprint/clone/Save/reopen и
   семифайловой компиляции без ослабления проверки оригинального bundle.
3. Проверить физические поля, selection/validation, Cancel/Apply и игровое
   действие в M19/M20/M23. Агент не использует Computer Use и не запускает
   официальные или текущие окна редактора/игру.

Не закрыто ни одного дополнительного верхнего scope:16 I scopes/23 M groups.
I14/I17 и согласованные Ctrl+N/New/Ctrl+Y/Redo сохранены. Общий перечень —
[OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md).
