# Числовой ввод Possession и CrSpells

CreatureAuxiliaryInputs-20261019, часть открытого I11. Сопоставлены семь
числовых EN_CHANGE/UDN controls двух исходных страниц. Везде используется
unsigned GetDlgItemInt, присваиваемый LONG, затем целочисленная проверка границ.

| Страница и поля | Source controls | Граница целой части / формат |
| --- | --- | --- |
| Possession: SpecialAb1Data, SpecialAb2Data |105,108|0…32767, integer |
| Possession: WaddleScale, OscillateScale |136,138|0…1, fixed-point |
| Possession: WalkScale |140|0…12, integer |
| CrSpells: RechargeTime, Range |104,106|0…1024, fixed-point |

Спиннеры имеют ID=control+100. Misc_SetUpDownInt меняет unsigned целую часть
на единицу, затем EN_CHANGE ограничивает результат. Ноль при уменьшении
проходит UINT_MAX→LONG−1 и возвращается к0 (`0.0` для fixed-point).
Дробь внутри целой границы сохраняется: WaddleScale=`1.75` допускается.

## Что изменилось

Теперь SpecialAb1Data=`32768` сразу становится `32767`, WalkScale=`13` — `12`,
WaddleScale=`2.5` — `1.0`, CrSpells Range=`1025.75` — `1024.0`. Пустой ввод
остаётся пустым до явного шага; увеличение даёт `1`. Нечисловой изменённый
ввод по-прежнему требует исправления перед Apply. Строгий decoder не заменён
исходным небезопасным разбором произвольного текста.

CreatureAuxiliaryInputs задаёт эти текстовые преобразования для двух страниц;
общий SourceIntegerInput, storage, compiler, проекты и каталоги не менялись.
На Possession существующая панель используется отдельно и в общем окне:
TextChanged, RepeatButton−/+ и Up/Down без модификаторов направлены в helper.
Abilities/swipes/gamma/CameraRolls и зависимости Resistances сохранены.
Data особой способности остаётся активным и при None, как прежде и в исходнике.

На CrSpells реальный CreatureSpellWindow использует тот же helper для ввода и
шага. Стрелки стали RepeatButton; добавлены Up/Down. Шаг изменяет существующий
TextBox, не пересоздаёт форму, поэтому не уничтожает кнопку во время удержания
и не требует смены фокуса после каждого шага. Options child по-прежнему
блокирует редактирование родителя. Документ/owner/baseline/control identity/
read-only guards сохраняют права только у текущей редактируемой формы.

Native WORD имеет отдельную прежнюю границу: CrSpells source LONG допускает
1024, а прямой native WORD хранит максимум65535/4096. Native step достигает
целой части15. Явный ввод1025 ограничится source значением1024.0, после чего
native Apply сообщит о переполнении; DKM сохраняет source LONG. Биты молча
не обрезаются. Это существующая [граница форматов](GLOBAL_DEFINITION_NUMBERS.md).

Чтение/перестроение/смена языка/no-op не ремонтируют импортированные значения.
Частные страницы пишут только в WorkingDocument; общий Apply обеих страниц —
один Undo. Cancel сохраняет живой документ. Новых строк интерфейса нет,
все2781 EN/RU literals сохранены.

## Проверки и остаток I11

CLI `--creature-auxiliary-input-only` проверяет буквальную таблицу7 controls,
границы/дробь/пустой/overflow/unsigned wrap/шаг, native WORD guard. На native/DKM
одновременно меняются Possession и CrSpells: проверяются точные raw значения,
private state, общий один Undo/Redo, Cancel, отказ native overflow и imported
no-op обеих семей. Другие успешные проверки без изменений не повторялись.

WPF suites EN/RU × native/DKM подготовлены и только скомпилированы, **не
исполнены**: standalone/shared Possession, shared/standalone CrSpells,
TextChanged/RepeatButton и identity сохранённых controls, language/stale owner,
общий Apply/Undo, импорт. Старые UI invalid cases Possession используют
`invalid` вместо теперь автоматически ограничиваемых чисел. CrSpells smoke
ожидает source строку `0.0`/`1024.0` после граничного шага и общий тип Control
для нового RepeatButton.

GUI/игра/renderer/Computer Use/audio device не запускались. Последний full1840
PASS/63 прежних отказа относится к CreatureLevelModifierWindow на старом Core;
новый focus не прибавляется к этому числу. Полный корпус без изменения файловых
алгоритмов не повторяется. Source/Binaries/fixtures/карты сохранены.

I11 остаётся открыт. Следующие конкретные обработчики — Jobs: девять source
числовых controls (chance105/125/145, mood/mana107/109/127/129/147/149), в нашей
форме три общих поля выбранной группы. Сопоставить EN_CHANGE и UDN с текущими
Add/Change; не повторять согласованный порядок предупреждения, private arrays
и общий Apply, уже подтверждённые прежними evidence. Затем остаются страницы
global elements и конечная сверка остальных callbacks из inventory14 страниц.

## Ручная приёмка M19/M21/M23

1. В Possession введите SpecialAb1Data=`32768`, WalkScale=`13`, WaddleScale=`2.5`:
   сразу должны появиться `32767`, `12`, `1.0`. Проверьте оба data и оба scale.
2. В CrSpells введите Range=`1025.75`: получится `1024.0`. На native Apply
   должен сообщить о WORD overflow; для проверки общей записи задайте1.75.
3. Очистите числовое поле, нажмите увеличение: получится1. Уменьшите0: integer
   остаётся0, fixed-point показывает0.0. Проверьте удержание и Up/Down; фокус
   и кнопка должны оставаться на месте. Повторите EN/RU и native/DKM.
4. Измените Possession, перейдите к CrSpells, измените их и отмените общее окно:
   карта прежняя. Повторите и примените — один Undo возвращает обе страницы.
5. Проверьте сохранность набранных строк при смене языка, Tab/DPI/resize,
   блокировку родителя при Options и отсутствие ремонта импорта от открытия.

Этим шагам игра не нужна. Последующие игровые прогоны используют только
DK2Preview_6a88240eaf814f78 через DKII-DX.exe -q -level.


Итог: validation-build-01/focus-01/build-01 всех41 проектов/publish-01 — ExitCode0, без ошибок и предупреждений. Предфинальная поставка и оба пакета обновлены из единого текущего проекта.


Продолжение: [Jobs9 input/step callbacks](CREATURE_JOB_INPUT_CALLBACKS.md), CreatureJobInputs-20261019. I11 открыт; далее5 Global Elements numeric pages и остальныеcallbacks.
