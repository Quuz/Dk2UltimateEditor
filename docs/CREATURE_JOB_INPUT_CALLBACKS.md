# Числовой ввод параметров работ существ

CreatureJobInputs-20261019, часть открытого I11. Сопоставлены девять исходных
EN_CHANGE/UDN controls Properties_GlobalJobsProc. В современной панели это
три общих поля выбранной группы, без изменения capacities и типов работ.

| Значение | Source Happy/Unhappy/Angry controls | Getter / диапазон |
| --- | --- | --- |
| Chance |105/125/145| unsigned0…100 |
| Mood |107/127/147| signed−32767…32767 |
| Mana |109/129/149| signed−32767…32767 |

Спиннеры используют control+100 и Misc_SetUpDownInt для chance,
Misc_SetUpDownIntSigned для mood/mana. Сначала читается целая часть, затем
шаг на единицу и тот же EN_CHANGE. Значения целочисленные, без fixed-point.

## Исправления

Теперь Chance=`101` сразу становится `100`, Mood=`-32768` — `-32767`,
Mana=`32768` — `32767`. Пустая строка остаётся пустой; увеличение из неё
даёт1. Шаг из2.75 даёт3, как исходный integer getter; неподтверждённая
дробная строка сама по себе не становится допустимым integer для Add/Change.
Невалидный изменённый ввод по-прежнему требует исправления перед Submit.

CreatureJobInputs отвечает только за текст. Реальная XAML-панель получила
RepeatButton−/+ и Up/Down без модификаторов. TextChanged нормализует только
текущее редактируемое поле валидной группы с параметрами, current document/
row/baseline/owner. Шаг меняет существующий TextBox без перестроения панели.
Группа Hate не имеет numeric parameters; эти controls скрыты и отключены,
программный Step отказывается. Одна панель используется отдельно и внутри
CreatureGlobalsWindow через CreatureWorkspacePageWindow.

Ввод и шаг **не выполняют Add/Change автоматически**, не переписывают private
job arrays и не меняют документ. Ранее проверенные Add/Change/Remove,
предупреждение о сумме вероятностей, retained Jobs page и порядок
PSN_KILLACTIVE/PSN_APPLY не менялись. Изменённый текст снимает прежнее
подтверждение, как и до исправления. Подробности исходного жизненного цикла:
[CREATURE_JOBS_CALLBACK_SCOPE](CREATURE_JOBS_CALLBACK_SCOPE.md).

При загрузке/import/language/no-op не выполняется нормализация: исходные
chance255 или signed WORD alias32768 (показ−32768) сохраняются. Явный ввод
другого значения использует source bounds. Native signed mood/mana пишутся
WORD bits, DKM сохраняет LONG; существующий writer не менялся.

## Проверки и граница результата

CLI `--creature-job-input-only` проверяет три вида, покрывающие все9 source
controls: верх/низ/пустой/нечисловой/overflow, signed/unsigned wrap, whole-part
шаг, all3 parameter groups native/DKM Add/Change, точные signed tuple bits,
private Apply, общий один Undo/Redo, Cancel и импортированный no-op Change.

Подготовлены WPF suites EN/RU × native/DKM, **только скомпилированы**:
standalone/shared панель, TextChanged/RepeatButton, отсутствие implicit Submit,
all3 groups, Hate refusal, language, stale owner, common Apply/Undo и imported
aliases. Старый invalid-input smoke использует `invalid` вместо−32768,
поскольку последнее теперь ограничивается автоматически.

Ни GUI, ни renderer, ни игра, ни Computer Use, ни audio device не запускались.
Последний full1840 PASS/63 прежних отказа относится к старому Core этапа
CreatureLevelModifierWindow; этот focus к нему не прибавляется. Storage,
compiler, history и существующий jobs warning код не менялись — прежние
успешные группы повторно не запускались. Source/Binaries/fixtures/карты сохранены.

I11 остаётся открыт. Следующий конечный маршрут — числовые EN_CHANGE/UDN
пяти страниц Global Elements: Terrain, Room, Door, Trap, Spells. Конечный
inventory14 page procedures сохраняется; далее требуется завершить сверку
прочих callbacks/helpers, а не объявлять I11 закрытым по одним numeric bounds.
Сохранение кнопки/фокуса при удержании Attributes/Light/Parameters также должно
быть сопоставлено с исходными UDN: прежний переход через Rebuild не доказывает
физическую непрерывность удержания. Это существующие callbacks I11, не новый
верхний scope. M19/M21/M23 сохраняют ручную приёмку.

## Ручная приёмка M19/M21/M23

1. Откройте Jobs, выберите Happy, затем повторите Unhappy и Angry. Введите
   Chance101, Mood−32768, Mana32768: должны сразу получиться100/−32767/32767.
2. Очистите поле и нажмите увеличение: получится1. Проверьте удержание,
   Up/Down, Tab/каретку и EN/RU. Пока не нажаты Add/Change, строка списка
   работ и карта не должны меняться.
3. Нажмите Add либо Change, проверьте значения строки и сумму. Переход к
   Hate скрывает числовые параметры; вернитесь к группе с параметрами.
4. Отмените общее окно — карта прежняя. Повторите и примените; один Undo
   возвращает все изменения. Суммы, отличные от0/100, должны использовать
   прежнее предупреждение, без дополнительного подтверждения от ввода.
5. Повторите native/DKM и standalone/shared формы, DPI/resize. Открытие
   импортированных значений за границей не должно менять их байты.

Игра для этих шагов не требуется. Последующие игровые прогоны — только
DK2Preview_6a88240eaf814f78 через DKII-DX.exe -q -level.


Итог: validation-build-01/focus-01/build-01 всех41 проектов/publish-01 — ExitCode0, без ошибок и предупреждений. Предфинальная поставка и оба пакета обновлены из единого проекта.


Продолжение: [GlobalElementInputs-20261019](GLOBAL_ELEMENT_INPUT_CALLBACKS.md):33 numeric controls исправлены; Step/Rebuild/repeat Attributes/Light/Parameters code gap закрыт. I11 открыт: далее TypeProc/ShotProc/Resistances и конечная сверка оставшихся callers.
