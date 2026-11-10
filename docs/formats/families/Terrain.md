# Terrain: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **552 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `CompleteResource` |
| 0x74 | 84 | `ArtResource` | `SideResource` |
| 0xc8 | 84 | `ArtResource` | `TopResource` |
| 0x11c | 84 | `ArtResource` | `TaggedTopResource` |
| 0x170 | 24 | `StringId` | `StringIds` |
| 0x188 | 4 | `i32/4096` | `Depth` |
| 0x18c | 4 | `i32/4096` | `LightHeight` |
| 0x190 | 4 | `flags32` | `Flags` |
| 0x194 | 2 | `u16` | `Damage` |
| 0x196 | 2 | `u16` | `EditorTextureId` |
| 0x198 | 2 | `u16` | `Unk198` |
| 0x19a | 2 | `u16` | `GoldValue` |
| 0x19c | 2 | `u16` | `ManaGain` |
| 0x19e | 2 | `u16` | `MaxManaGain` |
| 0x1a0 | 2 | `u16` | `TooltipStringId` |
| 0x1a2 | 2 | `u16` | `NameStringId` |
| 0x1a4 | 2 | `u16` | `MaxHealthEffectId` |
| 0x1a6 | 2 | `u16` | `DestroyedEffectId` |
| 0x1a8 | 2 | `u16` | `GeneralDescriptionStringId` |
| 0x1aa | 2 | `u16` | `StrengthStringId` |
| 0x1ac | 2 | `u16` | `WeaknessStringId` |
| 0x1ae | 2 | `u16` | `unk1ae[0].unk1ae` |
| 0x1b0 | 2 | `u16` | `unk1ae[1].unk1ae` |
| 0x1b2 | 2 | `u16` | `unk1ae[2].unk1ae` |
| 0x1b4 | 2 | `u16` | `unk1ae[3].unk1ae` |
| 0x1b6 | 2 | `u16` | `unk1ae[4].unk1ae` |
| 0x1b8 | 2 | `u16` | `unk1ae[5].unk1ae` |
| 0x1ba | 2 | `u16` | `unk1ae[6].unk1ae` |
| 0x1bc | 2 | `u16` | `unk1ae[7].unk1ae` |
| 0x1be | 2 | `u16` | `unk1ae[8].unk1ae` |
| 0x1c0 | 2 | `u16` | `unk1ae[9].unk1ae` |
| 0x1c2 | 2 | `u16` | `unk1ae[10].unk1ae` |
| 0x1c4 | 2 | `u16` | `unk1ae[11].unk1ae` |
| 0x1c6 | 2 | `u16` | `unk1ae[12].unk1ae` |
| 0x1c8 | 2 | `u16` | `unk1ae[13].unk1ae` |
| 0x1ca | 2 | `u16` | `unk1ae[14].unk1ae` |
| 0x1cc | 2 | `u16` | `unk1ae[15].unk1ae` |
| 0x1ce | 1 | `u8` | `WibbleH` |
| 0x1cf | 1 | `u8` | `leanH[0].leanH` |
| 0x1d0 | 1 | `u8` | `leanH[1].leanH` |
| 0x1d1 | 1 | `u8` | `leanH[2].leanH` |
| 0x1d2 | 1 | `u8` | `WibbleV` |
| 0x1d3 | 1 | `u8` | `leanV[0].leanV` |
| 0x1d4 | 1 | `u8` | `leanV[1].leanV` |
| 0x1d5 | 1 | `u8` | `leanV[2].leanV` |
| 0x1d6 | 1 | `u8` | `TerrainId` |
| 0x1d7 | 2 | `u16` | `StartingHealth` |
| 0x1d9 | 1 | `u8` | `MaxHealthTypeTerrainId` |
| 0x1da | 1 | `u8` | `DestroyedTypeTerrainId` |
| 0x1db | 3 | `RGB8` | `TerrainLight` |
| 0x1de | 1 | `u8` | `TextureFrames` |
| 0x1df | 32 | `string` | `SoundCategory` |
| 0x1ff | 2 | `u16` | `MaxHealth` |
| 0x201 | 3 | `RGB8` | `AmbientLight` |
| 0x204 | 32 | `string` | `SoundCategoryFirstPerson` |
| 0x224 | 4 | `u32` | `Unk224` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
