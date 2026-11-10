# Traps: физическая разметка

Навигационная копия effective_catalogue_layouts этапа193; наличие имени не означает независимо доказанный игровой смысл. [Общие правила](../README.md).

Размер записи: **579 байт**. Варианты/расширения существ см. creature-version-layout-semantics.json.

| Смещение в записи | Размер | Тип | Имя в актуальной схеме |
|---|---:|---|---|
| 0x0 | 32 | `string` | `Name` |
| 0x20 | 84 | `ArtResource` | `MeshResource` |
| 0x74 | 84 | `ArtResource` | `GuiIcon` |
| 0xc8 | 84 | `ArtResource` | `EditorIcon` |
| 0x11c | 84 | `ArtResource` | `FlowerIcon` |
| 0x170 | 84 | `ArtResource` | `FireResource` |
| 0x1c4 | 4 | `i32/4096` | `Height` |
| 0x1c8 | 4 | `i32/4096` | `RechargeTime` |
| 0x1cc | 4 | `i32/4096` | `ChargeTime` |
| 0x1d0 | 4 | `i32/4096` | `ThreatDuration` |
| 0x1d4 | 4 | `u32` | `ManaCostToFire` |
| 0x1d8 | 4 | `i32/4096` | `IdleEffectDelay` |
| 0x1dc | 4 | `u32` | `TriggerData` |
| 0x1e0 | 4 | `u32` | `ShotData1` |
| 0x1e4 | 4 | `u32` | `ShotData2` |
| 0x1e8 | 2 | `u16` | `ResearchTime` |
| 0x1ea | 2 | `u16` | `Threat` |
| 0x1ec | 4 | `flags32` | `Flags` |
| 0x1f0 | 2 | `u16` | `Health` |
| 0x1f2 | 2 | `u16` | `GoldCost` |
| 0x1f4 | 2 | `u16` | `PowerlessEffectId` |
| 0x1f6 | 2 | `u16` | `IdleEffectId` |
| 0x1f8 | 2 | `u16` | `DeathEffectId` |
| 0x1fa | 2 | `u16` | `ManufToBuild` |
| 0x1fc | 2 | `u16` | `GeneralDescriptionStringId` |
| 0x1fe | 2 | `u16` | `StrengthStringId` |
| 0x200 | 2 | `u16` | `WeaknessStringId` |
| 0x202 | 2 | `u16` | `ManaUsage` |
| 0x204 | 2 | `u16` | `Unknown204` |
| 0x206 | 2 | `u16` | `TooltipStringId` |
| 0x208 | 2 | `u16` | `NameStringId` |
| 0x20a | 1 | `u8` | `ShotsWhenArmed` |
| 0x20b | 1 | `enum8` | `TriggerType` |
| 0x20c | 1 | `u8` | `TrapId` |
| 0x20d | 1 | `u8` | `ShotTypeId` |
| 0x20e | 1 | `u8` | `ManufCrateObjectId` |
| 0x20f | 32 | `string` | `SoundCategory` |
| 0x22f | 1 | `enum8` | `Material` |
| 0x230 | 1 | `u8` | `OrderInEditor` |
| 0x231 | 4 | `i32/4096` | `ShotOffset.0` |
| 0x235 | 4 | `i32/4096` | `ShotOffset.1` |
| 0x239 | 4 | `i32/4096` | `ShotOffset.2` |
| 0x23d | 4 | `i32/4096` | `ShotDelay` |
| 0x241 | 2 | `u16` | `HealthGain` |

Ограничения, уверенность и исправления — в [машинном индексе](../../../research/dk2-formats/format-package.index.json) и его компонентах. Unknown/legacy-имя остаётся непроверенным до соответствующего доказательства.
