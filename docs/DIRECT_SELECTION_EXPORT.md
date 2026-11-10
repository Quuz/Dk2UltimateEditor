# Прямой экспорт выделения

25 октября: [I08 закрыт по коду](I08_CODE_CLOSURE.md): band selection и специальные copy/cut/paste rules сопоставлены. Global bands/members не являются spatial things; paste сохраняет буквальный terrain/object payload. I09 gate transforms и ручная приёмка остаются в [управляющем списке](OPEN_FUNCTIONS_RU.md).
14 октября2026. Устранено различие с original writer: прямой экспорт
выделения и сохранение copy buffer раньше использовали одну projection.

## Исходные правила

`Areas_WriteThings(FALSE)` обходит клетки поY/X, пишет всех MapCreatures,
затем через `Things_GetAtMapPos` берёт первый Door/Trap/Object/MagicalObject
категории в этой клетке. Node.next/prev обнуляются, координаты становятся
относительными к origin выделения.

`Select_Copy`/`Things_CloneAtMapPos` клонируют все совпавшие элементы.
`Areas_WriteThings(TRUE)` сохраняет уже созданный список без first-member
отбора или повторной сортировки. Поэтому несколько creatures сохраняются
обоими маршрутами, duplicates других категорий — только buffer маршрутом.

| Команда | Текущий writer | Совместимый KTGS |
| --- | --- | --- |
| Экспорт выделения карты |SaveDirectSelectionConfirmed / SerializeDirectSelection|SourceY/X/category order, все creatures, первый noncreature категории|
| Экспорт буфера |SaveConfirmed / Serialize|Все элементы, сохранённый порядок буфера|

Native keeper/hero/neutral входят в creatures; special items определяются
прежним source каталогом. Глобальные отряды/зоны/маршруты не стали placed
objects и не включаются этой правкой. Attached graphs размещённых объектов
сохраняются расширением по прежним правилам.

## Полный snapshot и DKCP6

Прямой файл хранит source projection в KTGS и все исходные raw records,
graphs, tiles/protection/provenance в расширении. В fixture12raw объектов:
совместимая часть для официального reader содержит8, наш reader восстанавливает12 без потери bytes.
Сохранение открытого файла как буфера снова сохраняет все элементы.

DKCP6 добавляет DWORD1 после прежнего raw tile vector — прямой projection
mode. Record counts относятся к полному raw snapshot. DKCP1–5 читаются
по прежним правилам, buffer writer продолжает создаватьDKCP5. Повторный
`SerializeDirectSelection` даёт прямой файл, обычный `Serialize` — buffer.

Parse проверяет mode, schema/records/provenance/mask/raw tiles и соответствие
KTGS этому projection. Подмена first-member record, buffer KTGS или mode
отвергается. Raw duplicates вне KTGS проверяются как собственные структурные
records: их bytes не выводятся из отсутствующего совместимого представления.

Original Areas_GetThingsSize считал всех duplicates, тогда как direct writer
писал первого: header размер становился больше реально записанного. Сохранено
first-member поведение, но Size теперь соответствует фактическим bytes.
Дополнительные records сохранены расширением.

Source пустой файл заканчивался после KMAP. У нас дальше идётDKCP, поэтому
перед ним остаётся пустой KTGS6: иначе Areas_ExtractThings отверг бы DKCP
как неверный object header. Reader обрабатывает Count0 и игнорирует tail.
Это расширение одного DKZ, обычная игровая карта остаётся комплектом7файлов.

## Запись и проверки

Archive полностью сериализуется и валидируется до записи. Временный файл
создаётся рядом с destination, Flush(true), затем подтверждённая замена.
Отказ удаляет temporary и сохраняет прежние bytes. Экспорт не меняет рабочую
карту, revision/editor counter/Undo. Используется прежний Save/overwrite dialog.

Source capture трёх файлов и журналы находятся в
`artifacts/Evidence/DirectSelectionExport-20261014`. Native/project fixtures
проверяют перемешанный порядок, пять категорий, creatures stack/noncreature
duplicates, literalIDs/types/relativeXY/pointer zeroing,12raw/8KTGS counts,
full raw/graph roundtrip, transformed buffer retention, mode/tamper guards,
пустой KTGS, actual overwrite и locked failure/no temporary leftovers.
Новые случаи входят в существующую selection-order группу Validation;
top-level PASS count этим не увеличивается. Итоговые terminal результаты
текущей DLL — в CURRENT_STATUS. Окна/official GUI не запускались.

## Ручные шаги

1. Выделите участок копии карты с creatures/объектами. Экспортируйте выделение
   и импортируйте наш файл: полный snapshot должен восстановиться, исходная
   карта не получает Undo от экспорта.
2. Откройте DKZ в официальном редакторе. Direct projection сохраняет всех
   creatures и первого noncreature категории в клетке; buffer export — всех.
3. Проверьте выделение без объектов и cancel/overwrite обоих Save диалогов:
   официальный reader должен открыть terrain без ошибки object header.

Игровой тест — только `DK2Preview_6a88240eaf814f78`,7файлов через
`DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78`. Реальный исторический
DKZ corpus/official GUI/paste/game остаются требованиями полной цели.
