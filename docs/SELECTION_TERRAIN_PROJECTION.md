# Клетки выделения: совместимое представление и исходные байты

12 октября 2026. Исправлено расхождение KMAP в DKZ: прежний writer копировал
непомеченные клетки без преобразования, включая служебный байт и terrain254.
Исходный Compiler_EditorMapToGameMap записывает служебный байт1 и переводит
защищённый материал254 в30. После исходной загрузки через GameMapToEditorMap
также действуют порядок World/Rooms/Gates/Walls, нормализация owner/variant и
fallback неизвестного материала к rock2.

Пример исходных четырёх bytes `fe 02 01 4d`: совместимый KMAP теперь получает
`1e 02 01 01`. Современный snapshot сохраняет исходное `fe 02 01 4d` целиком.
Это преобразование копии для обмена, не исправление исходной карты.

## DKCP5

Совместимый KMAP содержит source game→editor→game projection. Помеченные mask
клетки получают canonical30/2/1/1. Метка защиты — отдельный mask; она не выводится
из материала30/254, координат или служебного байта.

Writer сохраняет DKCP3/4 prefix с records, coordinate provenance, mask и прежними
raw protected cells. После него DKCP5 содержит ровно Width×Height32-bit raw cells
в row-major порядке. Полный raw vector восстанавливает также непомеченные клетки
с нестандартными owner/variant/reserved или неизвестным материалом.

Reader проверяет размеры, полный vector и его согласованность с KMAP. У protected
клетки raw vector должен совпасть с прежним protected prefix. Неизвестный tail,
усечённый vector или изменённый совместимый KMAP отвергаются. Изменение reserved
byte в корректном raw snapshot допустимо: это сохраняемые данные, а не checksum.

DKCP1–4 читаются с их прежними правилами и без предположения о потерянных данных.
Повторное сохранение создаёт DKCP5. KTGS source upgrades из DKCP4 сохранены.
Формат семи игровых KWD/KLD не меняется.

## Проверка

SelectionTerrainProjectionChecks содержит20 native/project literal cases:
locked254, world fixed owner, hero-rest/mana, room owners включая0, оба bridge
variants, неподдержанный variant, unknown terrain и reserved bytes. Ожидаемые
клетки заданы независимо от продуктового recipe. Проверяются полный raw roundtrip,
предыдущие DKCP1/2/3/4, изменённый KMAP, разрешённый raw reserved change, truncation,
сохранение исходных файлов и Undo/Redo. До исправления literal254 case отказал.
Прочие selection-files/protection/historical groups проверяют provenance, masks,
scripts, crop/transforms и record projections. Текущие логи находятся в
`artifacts/Evidence/SelectionTerrain-20261012`.

Порядок объектов дополнительно исправлен и проверяется в
[SELECTION_OBJECT_ORDER](SELECTION_OBJECT_ORDER.md). Эта проверка не закрывает реальный
исторический DKZ corpus, физические буфер/вставку/диалоги, официальный GUI и игру.
Полное соответствие редактора остаётся неподтверждённым.

Итоговая проверка текущей DLL:1798 passes/63 failures, terminal exit1;
все предыдущие63 сообщения совпадают буквально и в полном порядке, прежние1797
passes сохранены. Все41 проекта собраны без предупреждений/ошибок. Focused selection,
archive, conversion и native retention checks exit0. Source/Binaries1006 и
installed4453 files неизменны. Это не физическое GUI/game acceptance.
