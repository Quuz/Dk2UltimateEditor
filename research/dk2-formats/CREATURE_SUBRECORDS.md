# Вложенные записи Creatures: номера анимаций и заклинания

Этап86 — `CREATURE_HATE_JOBS_RU.md`: подтверждено пересечение HateJobs3/4 с альтернативной работой0 в игровых/редакторных intermediates ив writerPro. raw14E1/14E5 читаются,затем перезаписываются;альтернативная работа влияет на native job-membership. Схема: `creature_subrecords.phase86_job_overlap`.

Этап85 — `CREATURE_DEFINITION_RU.md`: полный конструктор/загрузчик2199 байт,8XYZ-троек,семь дополнительных presence-флагов и пересечение искусственногоmid308 сWORD6CD. Схема: `catalogue_loading.phase85_complete_definition`.

Game slot1=WALK; slot0 is copied to intermediate but excluded from resource load. Do not use zero-based OpenKeeper animation enum ordinals as FootstepFrames indices.

## Анимации и кадры шагов

Номера ниже подтверждаются переносом ресурсов в 0x568E30 и загрузчиком 0x492320. Первый общий цикл копирует raw+0x20 как slot0, но runtime-цикл начинается с1. Данные FootstepFrames по raw+0x1323 индексируются тем же игровым номером. Ходьба использует пару по raw+0x1327, а не первую пару. Например, Imp имеет пару0=(0,0), пару1=(2,8).

| Игровой слот | Поле базовой схемы | Raw ресурс | Raw пара шагов | Runtime ресурс |
|---:|---|---|---|---|
| 0 | Unknown1Resource | 0x20 | 0x1323 | 0xb |
| 1 | Animation.WALK | 0x74 | 0x1327 | 0x26 |
| 2 | Animation.RUN | 0xc8 | 0x132b | 0x41 |
| 3 | Animation.DRAGGED | 0x11c | 0x132f | 0x5c |
| 4 | Animation.RECOIL_FORWARDS | 0x170 | 0x1333 | 0x77 |
| 5 | Animation.MELEE_ATTACK | 0x1c4 | 0x1337 | 0x92 |
| 6 | Animation.CAST_SPELL | 0x218 | 0x133b | 0xad |
| 7 | Animation.DIE | 0x26c | 0x133f | 0xc8 |
| 8 | Animation.HAPPY | 0x2c0 | 0x1343 | 0xe3 |
| 9 | Animation.ANGRY | 0x314 | 0x1347 | 0xfe |
| 10 | Animation.STUNNED | 0x368 | 0x134b | 0x119 |
| 11 | Animation.IN_HAND | 0x3bc | 0x134f | 0x134 |
| 12 | Animation.SLEEPING | 0x410 | 0x1353 | 0x14f |
| 13 | Animation.EATING | 0x464 | 0x1357 | 0x16a |
| 14 | Animation.RESEARCHING | 0x4b8 | 0x135b | 0x185 |
| 15 | Animation.NULL_2 | 0x50c | 0x135f | 0x1a0 |
| 16 | Animation.NULL_1 | 0x560 | 0x1363 | 0x1bb |
| 17 | Animation.TORTURED_WHEEL | 0x5b4 | 0x1367 | 0x1d6 |
| 18 | Animation.NULL_3 | 0x608 | 0x136b | 0x1f1 |
| 19 | Animation.DRINKING | 0x65c | 0x136f | 0x20c |
| 20 | Animation.IDLE_1 | 0x6b0 | 0x1373 | 0x227 |
| 21 | Animation.RECOIL_BACKWARDS | 0x704 | 0x1377 | 0x242 |
| 22 | Animation.MANUFACTURING | 0x758 | 0x137b | 0x25d |
| 23 | Animation.PRAYING | 0x7ac | 0x137f | 0x278 |
| 24 | Animation.FALLBACK | 0x800 | 0x1383 | 0x293 |
| 25 | Animation.TORTURED_CHAIR | 0x854 | 0x1387 | 0x2ae |
| 26 | Animation.TORTURED_CHAIR_SKELETON | 0x8a8 | 0x138b | 0x2c9 |
| 27 | Animation.GET_UP | 0x8fc | 0x138f | 0x2e4 |
| 28 | Animation.DANCE | 0x950 | 0x1393 | 0x2ff |
| 29 | Animation.DRUNK | 0x9a4 | 0x1397 | 0x31a |
| 30 | Animation.ENTRANCE | 0x9f8 | 0x139b | 0x335 |
| 31 | Animation.IDLE_2 | 0xa4c | 0x139f | 0x350 |
| 32 | Animation.SPECIAL_1 | 0xaa0 | 0x13a3 | 0x36b |
| 33 | Animation.SPECIAL_2 | 0xaf4 | 0x13a7 | 0x386 |
| 34 | Animation.DRUNKED_WALK | 0xb48 | 0x13ab | 0x3a1 |
| 35 | Animation.ROAR | 0xb9c | 0x13af | 0x3bc |
| 36 | Animation.NULL_4 | 0xbf0 | 0x13b3 | 0x3d7 |
| 37 | Animation.DRUNKED_IDLE | 0xfd1 | 0x13b7 | 0x3f2 |
| 38 | Animation.BACK_OFF | 0x12cf | 0x13bb | 0x40d |
| 39 | Animation.STAND_STILL | 0x13e3 | 0x13bf | 0x428 |
| 40 | Animation.STEALTH_WALK | 0x1437 | 0x13c3 | 0x443 |
| 41 | Animation.DEATH_POSE | 0x148b | 0x13c7 | 0x45e |
| 42 | Animation.SWIPE | 0x1083 | 0x13cb | 0x479 |
| 43 | Animation.IDLE_3 | 0x10d7 | 0x13cf | 0x494 |
| 44 | Animation.IDLE_4 | 0x112b | 0x13d3 | 0x4af |
| 45 | Animation.IDLE_3_1 | 0x117f | 0x13d7 | 0x4ca |
| 46 | Animation.IDLE_4_1 | 0x11d3 | 0x13db | 0x4e5 |
| 47 | Animation.DIG | 0x1227 | 0x13df | 0x500 |

Слот0 не загружается как ресурс в просмотренном runtime loader. Это не основание удалять его84 байта: converter копирует их в intermediate. У первой записи отдельно сохранён raw; нельзя пытаться прочитать из неё имя и затем восстановить весь буфер только по этому имени.

## Три определения заклинаний внутри записи существа

Начало raw+0xDC4, ровно3 слота по24 байта. Это ссылки/настройки применения CreatureSpells, а не сами266-байтовые записи каталога CreatureSpells. Размер внешнего Creatures и число существ не меняют размер этого массива.

| Offset внутри24 байт | Байт | Поле |
|---|---:|---|
| 0x0 | 4 | ShotOffsetX |
| 0x4 | 4 | ShotOffsetY |
| 0x8 | 4 | ShotOffsetZ |
| 0xc | 1 | Unknown0C |
| 0xd | 1 | PlayAnimation |
| 0xe | 1 | Unknown0E |
| 0xf | 1 | Unknown0F |
| 0x10 | 4 | ShotDelayRaw |
| 0x14 | 1 | Unknown14 |
| 0x15 | 1 | Unknown15 |
| 0x16 | 1 | CreatureSpellId |
| 0x17 | 1 | LevelAvailable |

Converter переносит XYZ, Unknown0C, PlayAnimation, ShotDelay, ID и уровень в промежуточные слоты по24 байта. Четыре байта по+0E/+0F/+14/+15 он не читает. Getter-цепочка0x569CA0..0x569D50 и loader используют только XYZ, PlayAnimation, ShotDelay, ID и уровень: Unknown0C не попадает в runtime-элемент.

Runtime-элемент имеет19 байт: ID u8; level u8; delayTicks u32; XYZ12 байт; PlayAnimation u8. Задержка преобразуется как uint32(raw×TPS)>>12. Не сохранять19-байтовый runtime-элемент вместо24-байтовой записи файла.

Никакие неизвестные байты не обнуляются. Имена/значения PlayAnimation сохраняются как u8: нормализация произвольного ненулевого байта в1 была бы отдельным изменением данных.
