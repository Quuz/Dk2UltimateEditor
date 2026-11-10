# Rooms: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **1055 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `GuiIcon` |
| 0x74 | 84 | `ArtResource` | `EditorIcon` |
| 0xc8 | 84 | `ArtResource` | `CompleteResource` |
| 0x11c | 84 | `ArtResource` | `StraightResource` |
| 0x170 | 84 | `ArtResource` | `InsideCornerResource` |
| 0x1c4 | 84 | `ArtResource` | `UnknownResource` |
| 0x218 | 84 | `ArtResource` | `OutsideCornerResource` |
| 0x26c | 84 | `ArtResource` | `WallResource` |
| 0x2c0 | 84 | `ArtResource` | `CapResource` |
| 0x314 | 84 | `ArtResource` | `CeilingResource` |
| 0x368 | 4 | `i32/4096` | `CeilingHeight` |
| 0x36c | 2 | `u16` | `ResearchTime` |
| 0x36e | 1 | `u8` | `TorchIntensity` |
| 0x36f | 1 | `opaque8` | `TorchIntensityOpaqueByte` |
| 0x370 | 4 | `flags32` | `Flags` |
| 0x374 | 2 | `u16` | `TooltipStringId` |
| 0x376 | 2 | `u16` | `NameStringId` |
| 0x378 | 2 | `u16` | `Cost` |
| 0x37a | 2 | `u16` | `FightEffectId` |
| 0x37c | 2 | `u16` | `GeneralDescriptionStringId` |
| 0x37e | 2 | `u16` | `StrengthStringId` |
| 0x380 | 2 | `i16/4096` | `TorchHeight` |
| 0x382 | 2 | `u16` | `Effects[0].effectId` |
| 0x384 | 2 | `u16` | `Effects[1].effectId` |
| 0x386 | 2 | `u16` | `Effects[2].effectId` |
| 0x388 | 2 | `u16` | `Effects[3].effectId` |
| 0x38a | 2 | `u16` | `Effects[4].effectId` |
| 0x38c | 2 | `u16` | `Effects[5].effectId` |
| 0x38e | 2 | `u16` | `Effects[6].effectId` |
| 0x390 | 2 | `u16` | `Effects[7].effectId` |
| 0x392 | 1 | `u8` | `RoomId` |
| 0x393 | 1 | `u8` | `ReturnPercentage` |
| 0x394 | 1 | `u8` | `TerrainId` |
| 0x395 | 1 | `enum8` | `TileConstruction` |
| 0x396 | 1 | `u8` | `CreatedCreatureId` |
| 0x397 | 3 | `RGB8` | `TorchColor` |
| 0x39a | 1 | `u8` | `Objects[0].objectId` |
| 0x39b | 1 | `u8` | `Objects[1].objectId` |
| 0x39c | 1 | `u8` | `Objects[2].objectId` |
| 0x39d | 1 | `u8` | `Objects[3].objectId` |
| 0x39e | 1 | `u8` | `Objects[4].objectId` |
| 0x39f | 1 | `u8` | `Objects[5].objectId` |
| 0x3a0 | 1 | `u8` | `Objects[6].objectId` |
| 0x3a1 | 1 | `u8` | `Objects[7].objectId` |
| 0x3a2 | 32 | `string` | `SoundCategory` |
| 0x3c2 | 1 | `u8` | `OrderInEditor` |
| 0x3c3 | 4 | `i32/4096` | `TorchRadius` |
| 0x3c7 | 84 | `ArtResource` | `Torch` |
| 0x41b | 1 | `u8` | `RecommendedSizeX` |
| 0x41c | 1 | `u8` | `RecommendedSizeY` |
| 0x41d | 2 | `i16` | `HealthGain` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
