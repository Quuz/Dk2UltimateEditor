# KeeperSpells: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **406 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `GuiIcon` |
| 0x74 | 84 | `ArtResource` | `EditorIcon` |
| 0xc8 | 4 | `i32` | `Xc8` |
| 0xcc | 4 | `i32/4096` | `RechargeTime` |
| 0xd0 | 4 | `i32` | `ShotData1` |
| 0xd4 | 4 | `i32` | `ShotData2` |
| 0xd8 | 2 | `u16` | `ResearchTime` |
| 0xda | 1 | `enum8` | `TargetRule` |
| 0xdb | 1 | `u8` | `OrderInEditor` |
| 0xdc | 4 | `flags32` | `Flags` |
| 0xe0 | 2 | `u16` | `Xe0Unreferenced` |
| 0xe2 | 2 | `u16` | `ManaDrain` |
| 0xe4 | 2 | `u16` | `TooltipStringId` |
| 0xe6 | 2 | `u16` | `NameStringId` |
| 0xe8 | 2 | `u16` | `GeneralDescriptionStringId` |
| 0xea | 2 | `u16` | `StrengthStringId` |
| 0xec | 2 | `u16` | `WeaknessStringId` |
| 0xee | 1 | `u8` | `KeeperSpellId` |
| 0xef | 1 | `enum8` | `CastRule` |
| 0xf0 | 1 | `u8` | `ShotTypeId` |
| 0xf1 | 32 | `string` | `SoundCategory` |
| 0x111 | 2 | `u16` | `BonusRTime` |
| 0x113 | 1 | `u8` | `BonusShotTypeId` |
| 0x114 | 4 | `i32` | `BonusShotData1` |
| 0x118 | 4 | `i32` | `BonusShotData2` |
| 0x11c | 4 | `i32` | `ManaCost` |
| 0x120 | 84 | `ArtResource` | `BonusIcon` |
| 0x174 | 32 | `string` | `SoundCategoryGui` |
| 0x194 | 1 | `enum8` | `HandAnimId` |
| 0x195 | 1 | `enum8` | `NoGoHandAnimId` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
