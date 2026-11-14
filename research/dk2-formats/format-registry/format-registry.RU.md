# Реестр форматов DK2 (структурный, исторический baseline)

Детерминированный структурный реестр по четырём закреплённым JSON. Не является текущей семантической полнотой и не содержит процента покрытия.

## Источники (пиннинг по SHA-256)

| Файл | SHA-256 (факт) | Совпадает с ожидаемым | Байт |
| --- | --- | --- | --- |
| format-package.index.json | b87128dc941d1a5b1073e25a6638669d13f176094e8d09ef22bdfcf11d52aefe | да | 667280 |
| layouts.json | d36009c8064b04073d46c186ee128506c84bfb4900f343db626ba9f2ccb7d767 | да | 288007 |
| coverage.json | 77f04bf33b1a61762cb8030b949e2f875ae9433a0e9675687ca4256c2cb9aaf9 | да | 53594 |
| snapshot-manifest.json | 0f40d12ec2b4494301b540e5cac60d0824a1b9ff435e93dc80be7ad2983f9e8b | да | 737879 |

## Снимок

- Фаза: 205; компонентов машины: 182; документации: 237; python-источников: 1446; дата: 2026-11-11
- Оригинальный пакет: 31325234 байт, sha256 2851de145c6591b0a91b27b7687762cb8de95223afeecd4d9aa5c1925ef38eb7

## Макеты (12 ключей)

| Ключ | ID каталога | record_size | field_rows | сверка строк | неизв. строк | неизв. байт (факт/объявл.) | min/max поле | макс. extent = record_size | непрерывно с 0 | вложенные имена |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Terrain | TerrainId | 552 | 57 | да | 18 | 38/38 | 1/84 | да | да | 22 |
| Rooms | RoomId | 1055 | 51 | да | 1 | 84/84 | 1/84 | да | да | 16 |
| Doors | DoorId | 616 | 32 | да | 4 | 8/8 | 1/84 | да | да | 2 |
| Traps | TrapId | 579 | 45 | да | 2 | 2/2 | 1/84 | да | да | 5 |
| KeeperSpells | KeeperSpellId | 406 | 31 | да | 1 | 2/2 | 1/84 | да | да | 0 |
| CreatureSpells | CreatureSpellId | 266 | 47 | да | 0 | 0/0 | 1/84 | да | да | 27 |
| Creatures | CreatureId | 5449 | 361 | да | 140 | 608/608 | 1/84 | да | да | 235 |
| Creatures_5537 | — | 5537 | 444 | да | 222 | 692/692 | 1/84 | да | да | 318 |
| Objects | ObjectId | 894 | 35 | да | 5 | 89/89 | 1/84 | да | да | 7 |
| EffectElements | EffectElementId | 182 | 25 | да | 0 | 0/0 | 1/84 | да | да | 0 |
| Effects | EffectId | 246 | 46 | да | 1 | 2/2 | 1/84 | да | да | 8 |
| Shots | ShotId | 239 | 34 | да | 0 | 0/0 | 1/84 | да | да | 0 |

Terrain: неизвестных строк = 18 из 18 ожидаемых (Unk198=1, unk1ae_group=16, Unk224=1).

## Варианты Level только в coverage (2)

| Ключ | record_size | field_rows | неизв. строк | неизв. байт | макет в layouts.json |
| --- | --- | --- | --- | --- | --- |
| Level_25603 | 25603 | 68 | 2 | 522 | нет |
| Level_25683 | 25683 | 79 | 2 | 522 | нет |

## Связка версий Creatures / Creatures_5537

- record_size: Creatures=5449, Creatures_5537=5537; field_rows: 361 / 444
- catalogue_ids: Creatures=CreatureId, Creatures_5537=—
- Общий effective view: effective_catalogue_layouts; компонент правок: combat_category_semantics
- semantic_corrections[6]: /Creatures -> combat_category_semantics / Creatures
- semantic_corrections[7]: /Creatures_5537 -> combat_category_semantics / Creatures_5537
- These files link both keys to one effective view and one correction component; they state no rule that selects record_size 5449 vs 5537 for a given input file.
- По-русски: оба ключа привязаны к одному effective view и одному компоненту правок; правила выбора record_size 5449 или 5537 для конкретного файла в этих четырёх файлах нет.

## Контейнеры (6 классов)

| Класс | Ключ JSON | Свойства |
| --- | --- | --- |
| catalogue | format-package.index.json#/package_metadata/containers/catalogue | header_bytes, count_offset, declared_data_bytes_offset, whole_size_offset, signature_offset, record_stride, warning |
| things | format-package.index.json#/package_metadata/containers/things | header_bytes, count_offset, record_envelope, native_overrides, warning |
| triggers | format-package.index.json#/package_metadata/containers/triggers | header_bytes, condition_count_offset, action_count_offset, declared_data_bytes_offset, record_envelope, warning |
| variables | format-package.index.json#/package_metadata/containers/variables | header_bytes, count_offset, record_bytes, tag_offset, warning |
| map | format-package.index.json#/package_metadata/containers/map | header_bytes, width_offset, height_offset, cell_bytes, cell_order, cell_fields, legacy_field_aliases, native_rules |
| level | format-package.index.json#/package_metadata/containers/level | header_bytes, reference_count_u16_offset, creature_list_count_u16_offset, property_data_tag, references_tag, reference_bytes, creature_list_item_bytes, warning |

## Компоненты и precedence

- Компонентов: 182 (все с file+sha256: да); ключ: format-package.index.json#/package_metadata/components
- Precedence: 85 строк, sha256 f8a1038fdc495489c6f5baa40a73c51b6e0bc60d8a0a1a5216827a854abd104a; строк со ссылками на компоненты: 80
- Кодировки: byte_order=little, packed_records=True; кодировки отдельных полей здесь не проверялись.

## Политика непрозрачных полей

- Preserve source field labels verbatim; unresolved rows stay opaque and are never renamed.
- Новых имён полей не введено: 0.
- Числовой процент покрытия: отсутствует (только строки/байты unresolved).

## Пробелы реестра (отсутствующие литеральные ключи)

- This registry does not audit a separate per-field endian/text-encoding table. Consult each schema kind and its effective component; package-wide byte_order is recorded.
- No rule selecting between Creatures (record_size 5449) and Creatures_5537 (record_size 5537) occurs in these four files. Both keys point to one effective_catalogue_layouts view and one replacement component combat_category_semantics (semantic_corrections[6]/[7]); catalogue_ids contains CreatureId only and no key for Creatures_5537.
- Labels marked unresolved refer to the historical baseline only. Source labels are preserved here; their current semantic status must be read in effective components.
- No '15 families' mapping key occurs in these four files; the only literal 'family' key is effective_object_resource_bindings.family = 'Objects'.
- No primary runtime/native/roundtrip artifacts are carried by these four files; precedence statements only reference such work performed elsewhere (e.g. phase162 native counts, phase192 '15roundtrips').
- Effective value-range annotations and checksum rules need their referenced components. This structural registry does not independently validate those rules; catalogue_checksums references catalogue-checksums.json.
- No numeric coverage percentage occurs in these four files; coverage.json counts unresolved baseline rows and bytes only, and purpose explicitly disclaims verified gameplay for every named field.

## Границы утверждения

- Structural inventory of historical baseline layouts, not current semantic completeness.
- Absence of a metadata key here is a registry gap in these four files, not proof the fact is unknown elsewhere.
- Little-endian is cited from explicit package-wide package_metadata.byte_order, not inferred from a container key.
- Phase 205 / 182 machine components is a frozen snapshot; later supplements are not part of it.
- These four files carry no primary execution traces; precedence only references native/runtime/roundtrip work whose evidence lives outside this snapshot.
- No index file and no source schema was modified by this registry.
- No numeric coverage percentage is asserted here, and none is present in the inputs.

## Проверка

- Макеты: 12/12
- ID каталога: 11/11; связанных: 11
- Контейнеры: 6/6
- Level-варианты: 2/2
- Компоненты: 182; precedence: 85; правки с разрешёнными компонентами: 19/19
- Ошибок: 0; статус: ok

- digest: aa57f7ae19b79f8a773bcbc6be08c295dc7a2cfae6521aaa91c8dbfa21d24e16
