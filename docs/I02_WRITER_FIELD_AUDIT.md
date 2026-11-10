# I02: завершение таблиц функций записи

Сверка начата24 и завершена25 октября2026, CompilerRemainingWriters-20261024.
К прежним17 таблицам добавлены11: MapFile, TriggersFile/Triggers/Actions и
семь global writers. **Все28 функций записи сопоставлены в описанном ниже
контракте. I02 целиком ещё открыт: остаётся сверка validator/version routes.**

Продуктовый код, тесты, DLL и EN/RU literals не менялись. Сборки и успешные
тесты не повторялись. Это завершение конечной таблицы записи, а не заявление
полной готовности редактора или побайтового воспроизведения дефектов C-кода.

## Карта

Original Files_Editor сохраняет KMAP уже в игровом формате: до записи вызывает
Compiler_EditorMapToGameMap. Поэтому текущий ReadTiles читает готовые native
DWORD, а compile копирует их в Map.kld с header36, width/height и payload4WH.
Повторное преобразование editor controls было бы лишним. MapTile.Packed имеет
порядок terrain/owner/variant/reserved по одному байту, как в исходном writer.

Source преобразует editor boundary FE в native1E до KMAP Save. Это не повод
ещё раз менять готовый KMAP при компиляции. Подтверждённая retention исходных
native cells сохраняется. Геометрические операции и placement callers I07,
неподдержанные tile layouts и malformed inputs I04/I18 этим не закрываются.

## События

Все103 типа из исходного каталога:63 conditions D5 и40 actions D6. Для каждого
перечислены offsets, размеры и типы записей, параметры и literal defaults.
Каждый payload занимает8 байт; полный record —24: header8, payload8, suffix8.
Default-ветка исходника и fallback схемы совпадают: два LONG, param1/param5.
Более узких RHS casts, потерянных при выборе native типа, не найдено.

Suffix conditions: ID/or/and WORD, ctrl/repeat BYTE. Suffix actions:
ID/next WORD, нулевой WORD, ctrl/one BYTE. Signed project IDs сохраняются
тем же16-битным значением. Conditions записываются перед actions; формат
текущего KEVE хранит эти группы в таком же порядке. Unknown controls не
объявлены поддержанными semantic events: их политика остаётся I04/I18.

Четыре вычисляемых WORD относятся только к двум действиям:

- SHOWCRYSTAL: первая запись обычного object ID28, её X/Y. Проверка source
  запрещает отсутствие кристалла; нынешний отказ соответствует ей.
- DESTROYGATE: origin ворот по param5 плюс1 по каждой оси. Отсутствующий ID
  даёт исходный fallback1,1; исправление уже сделано и проверено отдельно.

Остальных вычисляемых выражений, которые generic Param writer мог бы
пропустить, не найдено. Все constants и Param widths сопоставлены с реальным
parser LegacyEventSchemas и текущим encoder.

Source TriggersFile содержит ошибочные служебные размеры: FileSize задаётся
как60+8(N+M), DefinesSize как16N, хотя реально пишет24 байта на каждый event.
Текущий свежий output сохраняет корректные фактические размеры60+24(N+M) и
24(N+M), отдельные counts N/M. Старый raw native SaveCopy имеет собственную
политику сохранения импортированных заголовков. Ошибочные размеры старого
генератора в новые файлы не переносятся.

## Глобальные определения

| Семейство | Native records | Байт/record исходника | Project fields |
| --- | ---: | ---: | ---: |
| Terrains |41|552|22|
| Rooms |26|1055|20|
| Doors |8|616|9|
| Traps |16|579|28|
| Spells |13|406|16|
| Creatures |46|5449|156|
| Creature spells |41|266|11|

Все262 project-поля сопоставлены с actual runtime schema patterns. Нет
поля с несколькими различными native destinations, которое потерялось бы
из-за deduplication по имени. Casts, offsets и границы records проверены.
Разобраны13 clear operations и шесть дополнительных нулевых WORD stores.
Последние соответствуют очистке ShotData2 для форм1/FF у trap и двух slots
keeper spells; у creature spells такой обработки нет.

Порядок всех семи DTOrder, terminal−1, пропуски−2, counts, canonical sizes
и размеры сохранённых Binaries defaults согласованы. Current compiler
обрабатывает только nonnegative project indices, требует подходящий count
и подтверждённые native fields. При default-valued arrays база копируется
целиком; реальные изменения записывают поля и обновляют дату. Эти две ветки
уже исправлены/проверены предыдущими этапами и не менялись сейчас.

Базовые graphics/opaque bytes берутся из выбранной game basis: DKM их не
хранит. Это действующий контракт. Расширенный creature record5537 использует
проверенный prefix и сохраняемый tail; полнота импорта всех variants остаётся
I04. Неизвестные версии/layouts не получают права на semantic edit по одной
лишь таблице offsets.

## Явное отличие маски Disarmable

У source trap writer байт1EC очищается маской23, но набор его записываемых
флагов имеет маску32: Disarmable использует10. Поэтому на базе с этим битом
включённым значение false может его не выключить; заодно source очищает
не представленный project-полем бит01.

Current compiler уже корректно заменяет каждый mapped bit отдельно:
Disarmable10 можно выключить, непредставленный01 сохраняется. Оставлено это
существующее поведение. Например, при base11 и всех трёх flags=false source
даёт10, current —01. Это документированная коррекция ошибки source mask,
а не новая правка продукта. Во всех остальных байтах source clear masks
равны объединению mapped masks. Неразобранных масок не осталось.

## Доказательства и следующий шаг

`artifacts/Evidence/CompilerRemainingWriters-20261024/remaining-writer-audit.json`:
103 recipes, четыре computed fields,262 global fields, все masks/constants,
семь orders/counts и hashes source/providers. Общий инвентарь объединяет
metadata/player, things/variables и нынешние11 functions:28/28 field tables.
SetDate — отдельное прежнее доказательство, не дополнительный writer.

Product/tests/App/Core остались прежними. Данные последних targeted PASS
использованы по их настоящим terminal codes, повторных запусков нет.
Исторический полный1840/63 не объявляется зелёным full текущей DLL.
GUI/WPF/audio/game/Computer Use не запускались; ручные группы остаются.

Далее — конечное сопоставление Compiler_TaskTestVersion/Map/Players/Things/
Areas/Gates/Triggers и graph tests с current diagnostics/guards. Только
после этого можно решать о закрытии I02. Прежние8 кодовых и23 ручные группы
и вся исходная цель сохраняются.
