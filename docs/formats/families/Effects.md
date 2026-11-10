# Effects: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **246 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `ArtResource` |
| 0x74 | 24 | `Light` | `Light` |
| 0x8c | 4 | `i32/4096` | `Mass` |
| 0x90 | 4 | `i32/65536` | `AirFriction` |
| 0x94 | 4 | `i32/65536` | `Elasticity` |
| 0x98 | 4 | `i32/4096` | `Radius` |
| 0x9c | 4 | `i32/4096` | `MinSpeedXy` |
| 0xa0 | 4 | `i32/4096` | `MaxSpeedXy` |
| 0xa4 | 4 | `i32/4096` | `MinSpeedYz` |
| 0xa8 | 4 | `i32/4096` | `MaxSpeedYz` |
| 0xac | 4 | `i32/4096` | `MinScale` |
| 0xb0 | 4 | `i32/4096` | `MaxScale` |
| 0xb4 | 4 | `flags32` | `Flags` |
| 0xb8 | 2 | `u16` | `EffectId` |
| 0xba | 2 | `u16` | `MinHp` |
| 0xbc | 2 | `u16` | `MaxHp` |
| 0xbe | 2 | `u16` | `FadeDuration` |
| 0xc0 | 2 | `u16` | `NextEffectId` |
| 0xc2 | 2 | `u16` | `DeathEffectId` |
| 0xc4 | 2 | `u16` | `HitSolidEffectId` |
| 0xc6 | 2 | `u16` | `HitLavaEffectId` |
| 0xc8 | 2 | `u16` | `HitWaterEffectId` |
| 0xca | 2 | `u16` | `GenerateIds[0].id` |
| 0xcc | 2 | `u16` | `GenerateIds[1].id` |
| 0xce | 2 | `u16` | `GenerateIds[2].id` |
| 0xd0 | 2 | `u16` | `GenerateIds[3].id` |
| 0xd2 | 2 | `u16` | `GenerateIds[4].id` |
| 0xd4 | 2 | `u16` | `GenerateIds[5].id` |
| 0xd6 | 2 | `u16` | `GenerateIds[6].id` |
| 0xd8 | 2 | `u16` | `GenerateIds[7].id` |
| 0xda | 2 | `u16` | `OuterOriginRange` |
| 0xdc | 2 | `u16` | `LowerHeightLimit` |
| 0xde | 2 | `u16` | `UpperHeightLimit` |
| 0xe0 | 2 | `u16` | `OrientationRange` |
| 0xe2 | 2 | `u16` | `SpriteSpinRateRange` |
| 0xe4 | 2 | `u16` | `WhirlpoolRate` |
| 0xe6 | 2 | `u16` | `DirectionalSpread` |
| 0xe8 | 2 | `u16` | `CircularPathRate` |
| 0xea | 2 | `u16` | `InnerOriginRange` |
| 0xec | 2 | `u16` | `GenerateRandomness` |
| 0xee | 2 | `u16` | `Misc2` |
| 0xf0 | 2 | `u16` | `Misc3` |
| 0xf2 | 1 | `enum8` | `GenerationType` |
| 0xf3 | 1 | `u8` | `ElementsPerTurn` |
| 0xf4 | 2 | `u16` | `Unknown3` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
