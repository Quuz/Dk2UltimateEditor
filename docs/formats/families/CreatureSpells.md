# CreatureSpells: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **266 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `EditorIcon` |
| 0x74 | 84 | `ArtResource` | `GuiIcon` |
| 0xc8 | 4 | `u32` | `ShotData1` |
| 0xcc | 4 | `u32` | `ShotData2` |
| 0xd0 | 4 | `i32/4096` | `Range` |
| 0xd4 | 4 | `flags32` | `Flags` |
| 0xd8 | 2 | `u16` | `CombatPoints` |
| 0xda | 2 | `u16` | `SoundEvent` |
| 0xdc | 2 | `u16` | `NameStringId` |
| 0xde | 2 | `u16` | `TooltipStringId` |
| 0xe0 | 2 | `u16` | `GeneralDescriptionStringId` |
| 0xe2 | 2 | `u16` | `StrengthStringId` |
| 0xe4 | 2 | `u16` | `WeaknessStringId` |
| 0xe6 | 1 | `u8` | `CreatureSpellId` |
| 0xe7 | 1 | `u8` | `ShotTypeId` |
| 0xe8 | 1 | `u8` | `AlternativeShotId` |
| 0xe9 | 1 | `u8` | `AlternativeRoomId` |
| 0xea | 4 | `i32/4096` | `RechargeTime` |
| 0xee | 1 | `enum8` | `AlternativeShot` |
| 0xef | 1 | `u8` | `data3[0].data3` |
| 0xf0 | 1 | `u8` | `data3[1].data3` |
| 0xf1 | 1 | `u8` | `data3[2].data3` |
| 0xf2 | 1 | `u8` | `data3[3].data3` |
| 0xf3 | 1 | `u8` | `data3[4].data3` |
| 0xf4 | 1 | `u8` | `data3[5].data3` |
| 0xf5 | 1 | `u8` | `data3[6].data3` |
| 0xf6 | 1 | `u8` | `data3[7].data3` |
| 0xf7 | 1 | `u8` | `data3[8].data3` |
| 0xf8 | 1 | `u8` | `data3[9].data3` |
| 0xf9 | 1 | `u8` | `data3[10].data3` |
| 0xfa | 1 | `u8` | `data3[11].data3` |
| 0xfb | 1 | `u8` | `data3[12].data3` |
| 0xfc | 1 | `u8` | `data3[13].data3` |
| 0xfd | 1 | `u8` | `data3[14].data3` |
| 0xfe | 1 | `u8` | `data3[15].data3` |
| 0xff | 1 | `u8` | `data3[16].data3` |
| 0x100 | 1 | `u8` | `data3[17].data3` |
| 0x101 | 1 | `u8` | `data3[18].data3` |
| 0x102 | 1 | `u8` | `data3[19].data3` |
| 0x103 | 1 | `u8` | `data3[20].data3` |
| 0x104 | 1 | `u8` | `data3[21].data3` |
| 0x105 | 1 | `u8` | `data3[22].data3` |
| 0x106 | 1 | `u8` | `data3[23].data3` |
| 0x107 | 1 | `u8` | `data3[24].data3` |
| 0x108 | 1 | `u8` | `data3[25].data3` |
| 0x109 | 1 | `u8` | `data3[26].data3` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
