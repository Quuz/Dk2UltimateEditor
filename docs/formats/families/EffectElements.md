# EffectElements: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **182 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `ArtResource` |
| 0x74 | 4 | `i32/4096` | `Mass` |
| 0x78 | 4 | `i32/65536` | `AirFriction` |
| 0x7c | 4 | `i32/65536` | `Elasticity` |
| 0x80 | 4 | `i32/4096` | `MinSpeedXy` |
| 0x84 | 4 | `i32/4096` | `MaxSpeedXy` |
| 0x88 | 4 | `i32/4096` | `MinSpeedYz` |
| 0x8c | 4 | `i32/4096` | `MaxSpeedYz` |
| 0x90 | 4 | `i32/4096` | `MinScale` |
| 0x94 | 4 | `i32/4096` | `MaxScale` |
| 0x98 | 4 | `i32/4096` | `ScaleRatio` |
| 0x9c | 4 | `flags32` | `Flags` |
| 0xa0 | 2 | `u16` | `EffectElementId` |
| 0xa2 | 2 | `u16` | `MinHp` |
| 0xa4 | 2 | `u16` | `MaxHp` |
| 0xa6 | 2 | `u16` | `DeathElementId` |
| 0xa8 | 2 | `u16` | `HitSolidElementId` |
| 0xaa | 2 | `u16` | `HitLavaElementId` |
| 0xac | 2 | `u16` | `HitWaterElementId` |
| 0xae | 3 | `RGB8` | `Color` |
| 0xb1 | 1 | `u8` | `RandomColorIndex` |
| 0xb2 | 1 | `u8` | `TableColorIndex` |
| 0xb3 | 1 | `u8` | `FadePercentage` |
| 0xb4 | 2 | `u16` | `NextEffectId` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
