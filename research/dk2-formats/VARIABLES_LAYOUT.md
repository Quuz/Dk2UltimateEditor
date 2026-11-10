# GlobalVariables: записи и варианты

Variables E6: общий заголовок 56 байт; записи всегда 16 байт, первый DWORD выбирает вариант. Повторения типов, порядок, дополнительные параметры и отдельную секцию камер сохранять.
Скалярные типы 3..242 сопоставлены с именами в enums.json (Variable.MiscVariable.MiscType). Эти имена — источник OpenKeeper, не подтверждение всех единиц. Для неизвестного типа сохранять все 16 байт. Диспетчер исследованного движка находится в variable-dispatch.json; исходные указатели/метаданные не являются бинарными полями KWD.

## 1 CreaturePool

0x51C9C0; player 0 iterates eligible players, not neutral player only

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | i32 | CreatureId |
| 0x8 | 4 | i32 | Count |
| 0xc | 4 | i32 | PlayerId |

## 2 Availability

0x51CA60 -> 0x51CAE0; player 0 iterates player list

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 2 | u16 | AvailabilityType |
| 0x6 | 2 | u16 | PlayerId |
| 0x8 | 4 | i32 | DefinitionId |
| 0xc | 4 | i32 | Value |

## 17 ResearchOrder

0x51DB00 -> 0x506FF0; category names from template

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | u32 | ResearchCategory |
| 0x8 | 4 | u32 | DefinitionId |
| 0xc | 4 | u32 | ResearchPointsRaw |

## 43 Sacrifice

0x51DB30; OpenKeeper

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 1 | u8 | Input[0].Type |
| 0x5 | 1 | u8 | Input[0].Id |
| 0x6 | 1 | u8 | Input[1].Type |
| 0x7 | 1 | u8 | Input[1].Id |
| 0x8 | 1 | u8 | Input[2].Type |
| 0x9 | 1 | u8 | Input[2].Id |
| 0xa | 1 | u8 | RewardType |
| 0xb | 1 | u8 | SpeechId |
| 0xc | 4 | i32 | RewardValue |

## 61 NoOpInOriginal170

0x51DC20 -> 0x401200 (empty function in this image)

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | u32 | Ignored1 |
| 0x8 | 4 | u32 | Ignored2 |
| 0xc | 4 | u32 | Ignored3 |

## 62 NoOpInOriginal170

0x51DC20 -> 0x401200 (empty function in this image)

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | u32 | Ignored1 |
| 0x8 | 4 | u32 | Ignored2 |
| 0xc | 4 | u32 | Ignored3 |

## 63 NoOpInOriginal170

0x51DC20 -> 0x401200 (empty function in this image)

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | u32 | Ignored1 |
| 0x8 | 4 | u32 | Ignored2 |
| 0xc | 4 | u32 | Ignored3 |

## 64 NoOpInOriginal170

0x51DC20 -> 0x401200 (empty function in this image)

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | u32 | Ignored1 |
| 0x8 | 4 | u32 | Ignored2 |
| 0xc | 4 | u32 | Ignored3 |

## 65 CreatureLevelStat

0x51DC40; low 16 bits of ValueRaw stored; observed levels 2..10

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | u32 | StatId |
| 0x8 | 4 | u32 | ValueRaw |
| 0xc | 4 | u32 | CreatureLevel |

## 66 Alliance

0x51DC90; player IDs truncated to u8; symmetric write

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | u32 | Player1 |
| 0x8 | 4 | u32 | Player2 |
| 0xc | 4 | u32 | Unused |

## 77 ModifyCreatureStat

0x51DD10 -> 0x494A70; stat ID u8; runtime units

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | u32 | CreatureId |
| 0x8 | 4 | u32 | StatId |
| 0xc | 4 | u32 | RuntimeValueRaw |

## 224 FirstPersonStat

0x51DC70; third payload DWORD ignored; low 16 bits of value stored

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | u32 | StatId |
| 0x8 | 4 | u32 | ValueRaw |
| 0xc | 4 | u32 | Unused |

## generic MiscOrUnknown

Raw 4-DWORD container; signed view and names from OpenKeeper, semantics vary by Type

| Offset записи | Байт | Тип | Поле |
|---|---:|---|---|
| 0x0 | 4 | u32 | Type |
| 0x4 | 4 | i32 | Value |
| 0x8 | 4 | i32 | Extra1 |
| 0xc | 4 | i32 | Extra2 |

Категории ResearchOrder: 1 Room, 2 Door, 3 Trap, 4 KeeperSpell. Категории Availability: 1 Room, 2 Creature, 3 Trap, 4 Door, 5 KeeperSpell. Не объединять enum.
Статистика type 65 имеет измеренные уровни 2..10; третье слово type 224 в файле равно 10, но движок его не использует. Поэтому общего ключа (type, stat, level) для этих двух таблиц нет. Изменения type 77 проходят другой путь, чем процентные/уровневые таблицы: см. CREATURE_STATS.md.
