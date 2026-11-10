# Objects: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **894 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `MeshResource` |
| 0x74 | 84 | `ArtResource` | `GuiIconResource` |
| 0xc8 | 84 | `ArtResource` | `InHandIconResource` |
| 0x11c | 84 | `ArtResource` | `InHandMeshResource` |
| 0x170 | 84 | `ArtResource` | `kUnknownResource` |
| 0x1c4 | 84 | `ArtResource` | `AdditionalResources[0].resource` |
| 0x218 | 84 | `ArtResource` | `AdditionalResources[1].resource` |
| 0x26c | 84 | `ArtResource` | `AdditionalResources[2].resource` |
| 0x2c0 | 84 | `ArtResource` | `AdditionalResources[3].resource` |
| 0x314 | 24 | `Light` | `Light` |
| 0x32c | 4 | `i32/4096` | `Width` |
| 0x330 | 4 | `i32/4096` | `Height` |
| 0x334 | 4 | `i32/4096` | `Mass` |
| 0x338 | 4 | `i32/4096` | `Speed` |
| 0x33c | 4 | `i32/65536` | `AirFriction` |
| 0x340 | 1 | `enum8` | `Material` |
| 0x341 | 1 | `u8` | `unknown3[0].unknown3` |
| 0x342 | 1 | `u8` | `unknown3[1].unknown3` |
| 0x343 | 1 | `u8` | `unknown3[2].unknown3` |
| 0x344 | 4 | `flags32` | `Flags` |
| 0x348 | 2 | `u16` | `Hp` |
| 0x34a | 2 | `u16` | `MaxAngle` |
| 0x34c | 2 | `u16` | `X34c` |
| 0x34e | 2 | `u16` | `ManaValue` |
| 0x350 | 2 | `u16` | `TooltipStringId` |
| 0x352 | 2 | `u16` | `NameStringId` |
| 0x354 | 2 | `u16` | `SlapEffectId` |
| 0x356 | 2 | `u16` | `DeathEffectId` |
| 0x358 | 2 | `u16` | `MiscEffectId` |
| 0x35a | 1 | `u8` | `ObjectId` |
| 0x35b | 1 | `enum8` | `StartState` |
| 0x35c | 1 | `u8` | `RoomCapacity` |
| 0x35d | 1 | `u8` | `PickUpPriority` |
| 0x35e | 32 | `string` | `SoundCategory` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
