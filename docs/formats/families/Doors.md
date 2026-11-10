# Doors: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **616 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `Mesh` |
| 0x74 | 84 | `ArtResource` | `GuiIcon` |
| 0xc8 | 84 | `ArtResource` | `EditorIcon` |
| 0x11c | 84 | `ArtResource` | `FlowerIcon` |
| 0x170 | 84 | `ArtResource` | `OpenResource` |
| 0x1c4 | 84 | `ArtResource` | `CloseResource` |
| 0x218 | 4 | `i32/4096` | `Height` |
| 0x21c | 2 | `u16` | `HealthGain` |
| 0x21e | 2 | `u16` | `Unknown1` |
| 0x220 | 4 | `u32` | `Unknown2` |
| 0x224 | 2 | `u16` | `ResearchTime` |
| 0x226 | 1 | `enum8` | `Material` |
| 0x227 | 1 | `u8` | `TrapTypeId` |
| 0x228 | 4 | `flags32` | `Flags` |
| 0x22c | 2 | `u16` | `Health` |
| 0x22e | 2 | `u16` | `GoldCost` |
| 0x230 | 1 | `u8` | `unknown3[0].unknown3` |
| 0x231 | 1 | `u8` | `unknown3[1].unknown3` |
| 0x232 | 2 | `u16` | `DeathEffectId` |
| 0x234 | 2 | `u16` | `ManufToBuild` |
| 0x236 | 2 | `u16` | `Unknown236` |
| 0x238 | 2 | `u16` | `ManaCost` |
| 0x23a | 2 | `u16` | `TooltipStringId` |
| 0x23c | 2 | `u16` | `NameStringId` |
| 0x23e | 2 | `u16` | `GeneralDescriptionStringId` |
| 0x240 | 2 | `u16` | `StrengthStringId` |
| 0x242 | 2 | `u16` | `WeaknessStringId` |
| 0x244 | 1 | `u8` | `DoorId` |
| 0x245 | 1 | `u8` | `OrderInEditor` |
| 0x246 | 1 | `u8` | `ManufCrateObjectId` |
| 0x247 | 1 | `u8` | `KeyObjectId` |
| 0x248 | 32 | `string` | `SoundCategory` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
