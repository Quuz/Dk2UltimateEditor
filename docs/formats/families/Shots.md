# Shots: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **239 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `MeshResource` |
| 0x74 | 24 | `Light` | `Light` |
| 0x8c | 4 | `i32/65536` | `AirFriction` |
| 0x90 | 4 | `i32/4096` | `Mass` |
| 0x94 | 4 | `i32/4096` | `Speed` |
| 0x98 | 4 | `u32` | `Data1` |
| 0x9c | 4 | `u32` | `Data2` |
| 0xa0 | 4 | `flags32` | `ShotProcessFlags` |
| 0xa4 | 4 | `i32/4096` | `Radius` |
| 0xa8 | 4 | `flags32` | `Flags` |
| 0xac | 2 | `u16` | `GeneralEffectId` |
| 0xae | 2 | `u16` | `CreationEffectId` |
| 0xb0 | 2 | `u16` | `DeathEffectId` |
| 0xb2 | 2 | `u16` | `TimedEffectId` |
| 0xb4 | 2 | `u16` | `HitSolidEffectId` |
| 0xb6 | 2 | `u16` | `HitLavaEffectId` |
| 0xb8 | 2 | `u16` | `HitWaterEffect` |
| 0xba | 2 | `u16` | `HitThingEffectId` |
| 0xbc | 2 | `u16` | `Health` |
| 0xbe | 1 | `u8` | `ShotId` |
| 0xbf | 1 | `u8` | `DeathShotId` |
| 0xc0 | 1 | `u8` | `TimedDelay` |
| 0xc1 | 1 | `u8` | `HitSolidShotId` |
| 0xc2 | 1 | `u8` | `HitLavaShotId` |
| 0xc3 | 1 | `u8` | `HitWaterShotId` |
| 0xc4 | 1 | `u8` | `HitThingShotId` |
| 0xc5 | 1 | `enum8` | `DamageType` |
| 0xc6 | 1 | `enum8` | `CollideType` |
| 0xc7 | 1 | `enum8` | `ProcessType` |
| 0xc8 | 1 | `enum8` | `AttackCategory` |
| 0xc9 | 32 | `string` | `SoundCategory` |
| 0xe9 | 2 | `u16` | `Threat` |
| 0xeb | 4 | `i32/4096` | `BurnDuration` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
