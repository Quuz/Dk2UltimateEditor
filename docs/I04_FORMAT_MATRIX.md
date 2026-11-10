# I04: конечная таблица поддерживаемых форматов

25 октября 2026. I04 [закрыт по коду](I04_CODE_CLOSURE.md). Все20 строк
сопоставлены с текущими providers и датированными доказательствами; F01 закрыта
[общим header guard](DESCRIPTOR_HEADER_GUARDS.md). Неизменённые успешные маршруты
не перезапускались для формирования таблицы. Ручная приёмка остаётся отдельной.

«Opaque» означает сохранение bytes с отказом от неподтверждённого смыслового
редактирования. «Raw fields» и named catalogue tools имеют разные требования:
известной длины строки недостаточно для подтверждения ID/order полного каталога.
Полная расшифровка произвольного неизвестного формата не является условием I04.

| ID | Данные | Load / inspector | Разрешённое редактирование | Save / conversion / опора |
|---|---|---|---|---|
| F01 | Native KWD descriptor | Общий DC4/DD28/DF guard, metadata prefix2304 до2360 внутри parameters; reference count/bounds/paths/type match. Stale generic size warning. | Только присутствующие bounded GAMEINFO fields; named settings показывают partial state при отсутствующих поздних fields. | Raw exact/Undo/Redo; minimal/full/extended parameters. Metadata-only preview без tile допускается, полное editing Open требует tile. [Header guards](DESCRIPTOR_HEADER_GUARDS.md). |
| F02 | Native Map.kld64 | v4, 65/8/66, dimensions1…1024, actual cells. Устаревший define size — warning, suffix retained. | Canvas/resize по фактическим cells. | NativeSizes/NativeTiles; cells1800 exact в Official/PayloadRetention. Неизвестные owners/terrain — отдельный I18. |
| F03 | PlayersB4 | 56-byte B5/28/B6 header, fixed205. Unknown header opaque. | Typed rows; named edits дополнительно требуют owner identity. | Raw exact; unknown header blocks DKM; suffix Loss+archive. [Collection guards](NATIVE_COLLECTION_GUARDS.md). |
| F04 | ThingsBE | 56-byte BF/28/C0; C2/32,C3/24,C4/28,C5/64,C6/32,C7/40,C8/36,C9/563,CB/44,CC/28. | Только точные known lengths; nested band slots16×32. Unknown signature/length opaque. | Counted boundary, stale metadata safe, suffix exact, atomic rollback; [границы](NATIVE_COLLECTION_BOUNDARIES.md), [guards](NATIVE_COLLECTION_GUARDS.md). |
| F05 | TriggersD2 | D3/32/D4,60-byte header, сначала conditionCount D5/24, потом actionCount D6/24. | Known physical schema; unknown event Type сохраняется и блокирует semantic DKM. | D5 insertion перед D6, action binding rebase; bad partition opaque/raw exact; [partition](NATIVE_SCRIPT_PARTITION.md). |
| F06 | VariablesE6 | E7/28/E8,56-byte header,16-byte rows. | Known typed variants; unknown keys сохраняются raw/retained. Named level tools требуют key/range. | Unknown variable occurrence retention отдельна от suffix Loss; [packed inspection](NATIVE_PACKED_INSPECTION.md), NativeVariableRetention. |
| F07 | Семь editable definitions | Shared adjacent-frame walker/header/count/stride/payload guards. Raw strides552/1055/616/579/406/5449or5537/266. | Known raw prefix fields. Named tools требуют полную canonical count/ID/order. | Unknown header/stride opaque; extraction future version отказ; [definition layouts](NATIVE_DEFINITION_LAYOUTS.md). |
| F08 | Creature44/46×5449/5537 | Dense IDs1…count и verified header; prefix5449. | Одинаковые source fields,88-byte extension сохраняется. | Raw exact/reset; real variant checks. [Creature scope](NATIVE_CREATURE_VARIANT_SCOPE.md), layouts. |
| F09 | Packed readonly/unknown definitions | RootsF0/104/10E/FA и adjacent known sections; no resync inside suffix. Unknown frame/header opaque. | Readonly root не скрывает editable following frames; unknown schema без fields. | OpaqueDefinitionData Loss+archive; unsafe official relocation refusal. [Tails](NATIVE_DEFINITION_TAILS.md), [inspection](NATIVE_PACKED_INSPECTION.md). |
| F10 | Целиком unknown native companion | Outer v4/bounds/type reference match; one Opaque.NativeFile; DK2N049. | Нет guessed fields/span edit; whole-operation rollback. | Raw exact; OpaqueNativeFile Loss+complete archive. [Historical/unknown inspection](HISTORICAL_SECTION_INSPECTION.md). |
| F11 | MAPF/AREA outer и KMAP | v1, exact outer payload, ≤64MiB/1024sections, KMAP1/actual exact cells; missing/duplicate KMAP отказ. | Canvas по KMAP; pointers никогда не разыменовываются. | Full stream retention/order; общая factory inspection без activity. [Historical inspection](HISTORICAL_SECTION_INSPECTION.md). |
| F12 | KINF1…7/KPLY1…3 |22 fixed-layout contract с KTGS/KREC/KMTH; KINF одна запись, KPLY7. | Исторические fields; explicit Upgrade для current named tools/compiler. | Raw section exact; malformed known size отказ. `LegacyProjectEditing`, `LegacyProjectUpgrade`, historical tests. |
| F13 | KTGS1…6/KREC1/KMTH1…5 | Known source structure sizes и кратность; opaque future versions. | Подтверждённые historical fields, explicit undoable upgrade. | Exact sections/padding/pointers; undeclared arbitrary version не guessed. Historical inspection/selection projection. |
| F14 | KTRG1 | Nested historical payload сохраняется opaque до явного upgrade. | Рецепты4 conditions/10actions, checked counts/ranges; overflow entire rollback. | Upgraded3 graphs/aliases,32768 actions supported. [Historical counts](HISTORICAL_SCENARIO_COUNTS.md), I03R06. |
| F15 | KTRG2/3 | Link/condition/action counts, source sizes, aliases146×64 exact. | Typed fields/graph and explicit2→3 upgrade. | Bad count/table отказ; labels/reserved bytes retained. Historical inspection, I03route matrix. |
| F16 | KGBL1 | Flags семь optional arrays, exact known payload. Unknown flags opaque. | Known source fields/nested globals tools; future schema readonly. | Unparsed data не угадываются; compiler rejects required opaque section. Historical inspection. |
| F17 | KSTS и D2LM/D2LO | KSTS historical opaque/custom runtime contract; known/future/truncated statistics retain current fallback. D2LM/LO bounded private parser. | Named activity/level sessions сохраняют согласованные границы. | Preview не стартует clock и не читает source archives; Load проверяет source companion отдельно. ActivityDocument/level checks. |
| F18 | Unknown historical section | Future/unknown sections Project.Opaque/DK2N008; known truncation отказ. | Без fields; required opaque core blocks compiler/resize. | Raw exact section/order; Save может добавить/обновить KSTS. AREA→DKM сообщает AreaOpaqueSection. Historical inspection. |
| F19 | Enhanced DKZ, MAP text | DKCP1…6 bounded selection parser; обычное project opening enhanced DKZ отказ. MAP bounded parser. | Только explicit clipboard/convert/import routes. | Attached mixed-format scripts требуют full compilation; no-scenario policy explicit. [I03 routes](I03_ROUTE_MATRIX.md), selection tests. |
| F20 | Source archives | Paths inert, hashes/mappings/retained occurrence validation при Load. | Не являются кодом/зависимостью сборки; source snapshot immutable. | I01/I03/PayloadRetention. Inspector показывает структуру основного файла, archive verification deferred to full Open. |

I18 сохраняет свой конечный scope malformed/ambiguous identities и owners;
он не требуется для объявления неизвестного arbitrary binary layout известным.
Ручные M01/M04/M11/M15/M16 остаются независимо от структурных проверок.
Исторический полный1840/63 не пересчитывается по этой таблице. I03 закрыт по
коду, остальные открытые кодовые пункты: I07,I08,I09,I10,I18.
