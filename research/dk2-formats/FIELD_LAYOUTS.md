# Смещения записей KWD/KLD

Смещения в таблицах считаются от начала записи, без 56-байтового заголовка каталога. Для Things — от начала полезной части после 8-байтового тега.
Последовательность чтения взята из закреплённой версии OpenKeeper; все длины проверены на файлах пользователя. Названия отражают интерпретацию OpenKeeper. Это не независимое подтверждение игровой семантики каждого поля.
Неизвестные поля, объединения ArtResource и нераспознанные биты нужно сохранять. `Unk`, `Unknown`, `X...` не заменять выдуманными названиями.
`i32/4096` и `i32/65536` — целые числа с фиксированной точкой, НЕ IEEE float. `i16/4096` — знаковый 16-битный fixed-point. Все числа little-endian.
Полная машинная схема: `layouts.json`, `thing-layouts.json`; перечисления и маски: `enums.json`.
Исправления по движку находятся в `verified-overrides.json` и `ENGINE_FINDINGS_RU.md`. В частности Creatures `0xF20` игра читает как **u16, угловые единицы (2048 на полный оборот)**; показанное ниже i16/4096 является исходной трактовкой OpenKeeper и для игрового UI неверно. Дополнительные игровые флаги читаются по `0x1545`; редакторский слот `0x1599` хранить отдельно.

## Terrain — 552 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `CompleteResource` |  |
| `0x74` | 84 | `ArtResource` | `SideResource` |  |
| `0xC8` | 84 | `ArtResource` | `TopResource` |  |
| `0x11C` | 84 | `ArtResource` | `TaggedTopResource` |  |
| `0x170` | 24 | `StringId` | `StringIds` |  |
| `0x188` | 4 | `i32/4096` | `Depth` |  |
| `0x18C` | 4 | `i32/4096` | `LightHeight` |  |
| `0x190` | 4 | `flags32` | `Flags` | Terrain.TerrainFlag |
| `0x194` | 2 | `u16` | `Damage` |  |
| `0x196` | 2 | `u16` | `EditorTextureId` |  |
| `0x198` | 2 | `u16` | `Unk198` |  НЕИЗВЕСТНО |
| `0x19A` | 2 | `u16` | `GoldValue` |  |
| `0x19C` | 2 | `u16` | `ManaGain` |  |
| `0x19E` | 2 | `u16` | `MaxManaGain` |  |
| `0x1A0` | 2 | `u16` | `TooltipStringId` |  |
| `0x1A2` | 2 | `u16` | `NameStringId` |  |
| `0x1A4` | 2 | `u16` | `MaxHealthEffectId` |  |
| `0x1A6` | 2 | `u16` | `DestroyedEffectId` |  |
| `0x1A8` | 2 | `u16` | `GeneralDescriptionStringId` |  |
| `0x1AA` | 2 | `u16` | `StrengthStringId` |  |
| `0x1AC` | 2 | `u16` | `WeaknessStringId` |  |
| `0x1AE` | 2 | `u16` | `unk1ae[0].unk1ae` |  НЕИЗВЕСТНО |
| `0x1B0` | 2 | `u16` | `unk1ae[1].unk1ae` |  НЕИЗВЕСТНО |
| `0x1B2` | 2 | `u16` | `unk1ae[2].unk1ae` |  НЕИЗВЕСТНО |
| `0x1B4` | 2 | `u16` | `unk1ae[3].unk1ae` |  НЕИЗВЕСТНО |
| `0x1B6` | 2 | `u16` | `unk1ae[4].unk1ae` |  НЕИЗВЕСТНО |
| `0x1B8` | 2 | `u16` | `unk1ae[5].unk1ae` |  НЕИЗВЕСТНО |
| `0x1BA` | 2 | `u16` | `unk1ae[6].unk1ae` |  НЕИЗВЕСТНО |
| `0x1BC` | 2 | `u16` | `unk1ae[7].unk1ae` |  НЕИЗВЕСТНО |
| `0x1BE` | 2 | `u16` | `unk1ae[8].unk1ae` |  НЕИЗВЕСТНО |
| `0x1C0` | 2 | `u16` | `unk1ae[9].unk1ae` |  НЕИЗВЕСТНО |
| `0x1C2` | 2 | `u16` | `unk1ae[10].unk1ae` |  НЕИЗВЕСТНО |
| `0x1C4` | 2 | `u16` | `unk1ae[11].unk1ae` |  НЕИЗВЕСТНО |
| `0x1C6` | 2 | `u16` | `unk1ae[12].unk1ae` |  НЕИЗВЕСТНО |
| `0x1C8` | 2 | `u16` | `unk1ae[13].unk1ae` |  НЕИЗВЕСТНО |
| `0x1CA` | 2 | `u16` | `unk1ae[14].unk1ae` |  НЕИЗВЕСТНО |
| `0x1CC` | 2 | `u16` | `unk1ae[15].unk1ae` |  НЕИЗВЕСТНО |
| `0x1CE` | 1 | `u8` | `WibbleH` |  |
| `0x1CF` | 1 | `u8` | `leanH[0].leanH` |  |
| `0x1D0` | 1 | `u8` | `leanH[1].leanH` |  |
| `0x1D1` | 1 | `u8` | `leanH[2].leanH` |  |
| `0x1D2` | 1 | `u8` | `WibbleV` |  |
| `0x1D3` | 1 | `u8` | `leanV[0].leanV` |  |
| `0x1D4` | 1 | `u8` | `leanV[1].leanV` |  |
| `0x1D5` | 1 | `u8` | `leanV[2].leanV` |  |
| `0x1D6` | 1 | `u8` | `TerrainId` |  |
| `0x1D7` | 2 | `u16` | `StartingHealth` |  |
| `0x1D9` | 1 | `u8` | `MaxHealthTypeTerrainId` |  |
| `0x1DA` | 1 | `u8` | `DestroyedTypeTerrainId` |  |
| `0x1DB` | 3 | `RGB8` | `TerrainLight` |  |
| `0x1DE` | 1 | `u8` | `TextureFrames` |  |
| `0x1DF` | 32 | `string` | `SoundCategory` |  |
| `0x1FF` | 2 | `u16` | `MaxHealth` |  |
| `0x201` | 3 | `RGB8` | `AmbientLight` |  |
| `0x204` | 32 | `string` | `SoundCategoryFirstPerson` |  |
| `0x224` | 4 | `u32` | `Unk224` |  НЕИЗВЕСТНО |

## Rooms — 1055 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `GuiIcon` |  |
| `0x74` | 84 | `ArtResource` | `EditorIcon` |  |
| `0xC8` | 84 | `ArtResource` | `CompleteResource` |  |
| `0x11C` | 84 | `ArtResource` | `StraightResource` |  |
| `0x170` | 84 | `ArtResource` | `InsideCornerResource` |  |
| `0x1C4` | 84 | `ArtResource` | `UnknownResource` |  НЕИЗВЕСТНО |
| `0x218` | 84 | `ArtResource` | `OutsideCornerResource` |  |
| `0x26C` | 84 | `ArtResource` | `WallResource` |  |
| `0x2C0` | 84 | `ArtResource` | `CapResource` |  |
| `0x314` | 84 | `ArtResource` | `CeilingResource` |  |
| `0x368` | 4 | `i32/4096` | `CeilingHeight` |  |
| `0x36C` | 2 | `u16` | `ResearchTime` |  |
| `0x36E` | 2 | `u16` | `TorchIntensity` |  |
| `0x370` | 4 | `flags32` | `Flags` | Room.RoomFlag |
| `0x374` | 2 | `u16` | `TooltipStringId` |  |
| `0x376` | 2 | `u16` | `NameStringId` |  |
| `0x378` | 2 | `u16` | `Cost` |  |
| `0x37A` | 2 | `u16` | `FightEffectId` |  |
| `0x37C` | 2 | `u16` | `GeneralDescriptionStringId` |  |
| `0x37E` | 2 | `u16` | `StrengthStringId` |  |
| `0x380` | 2 | `i16/4096` | `TorchHeight` |  |
| `0x382` | 2 | `u16` | `Effects[0].effectId` |  |
| `0x384` | 2 | `u16` | `Effects[1].effectId` |  |
| `0x386` | 2 | `u16` | `Effects[2].effectId` |  |
| `0x388` | 2 | `u16` | `Effects[3].effectId` |  |
| `0x38A` | 2 | `u16` | `Effects[4].effectId` |  |
| `0x38C` | 2 | `u16` | `Effects[5].effectId` |  |
| `0x38E` | 2 | `u16` | `Effects[6].effectId` |  |
| `0x390` | 2 | `u16` | `Effects[7].effectId` |  |
| `0x392` | 1 | `u8` | `RoomId` |  |
| `0x393` | 1 | `u8` | `ReturnPercentage` |  |
| `0x394` | 1 | `u8` | `TerrainId` |  |
| `0x395` | 1 | `enum8` | `TileConstruction` | Room.TileConstruction |
| `0x396` | 1 | `u8` | `CreatedCreatureId` |  |
| `0x397` | 3 | `RGB8` | `TorchColor` |  |
| `0x39A` | 1 | `u8` | `Objects[0].objectId` |  |
| `0x39B` | 1 | `u8` | `Objects[1].objectId` |  |
| `0x39C` | 1 | `u8` | `Objects[2].objectId` |  |
| `0x39D` | 1 | `u8` | `Objects[3].objectId` |  |
| `0x39E` | 1 | `u8` | `Objects[4].objectId` |  |
| `0x39F` | 1 | `u8` | `Objects[5].objectId` |  |
| `0x3A0` | 1 | `u8` | `Objects[6].objectId` |  |
| `0x3A1` | 1 | `u8` | `Objects[7].objectId` |  |
| `0x3A2` | 32 | `string` | `SoundCategory` |  |
| `0x3C2` | 1 | `u8` | `OrderInEditor` |  |
| `0x3C3` | 4 | `i32/4096` | `TorchRadius` |  |
| `0x3C7` | 84 | `ArtResource` | `Torch` |  |
| `0x41B` | 1 | `u8` | `RecommendedSizeX` |  |
| `0x41C` | 1 | `u8` | `RecommendedSizeY` |  |
| `0x41D` | 2 | `i16` | `HealthGain` |  |

## Doors — 616 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `Mesh` |  |
| `0x74` | 84 | `ArtResource` | `GuiIcon` |  |
| `0xC8` | 84 | `ArtResource` | `EditorIcon` |  |
| `0x11C` | 84 | `ArtResource` | `FlowerIcon` |  |
| `0x170` | 84 | `ArtResource` | `OpenResource` |  |
| `0x1C4` | 84 | `ArtResource` | `CloseResource` |  |
| `0x218` | 4 | `i32/4096` | `Height` |  |
| `0x21C` | 2 | `u16` | `HealthGain` |  |
| `0x21E` | 2 | `u16` | `Unknown1` |  НЕИЗВЕСТНО |
| `0x220` | 4 | `u32` | `Unknown2` |  НЕИЗВЕСТНО |
| `0x224` | 2 | `u16` | `ResearchTime` |  |
| `0x226` | 1 | `enum8` | `Material` | Material |
| `0x227` | 1 | `u8` | `TrapTypeId` |  |
| `0x228` | 4 | `flags32` | `Flags` | DoorFlag |
| `0x22C` | 2 | `u16` | `Health` |  |
| `0x22E` | 2 | `u16` | `GoldCost` |  |
| `0x230` | 1 | `u8` | `unknown3[0].unknown3` |  НЕИЗВЕСТНО |
| `0x231` | 1 | `u8` | `unknown3[1].unknown3` |  НЕИЗВЕСТНО |
| `0x232` | 2 | `u16` | `DeathEffectId` |  |
| `0x234` | 4 | `u32` | `ManufToBuild` |  |
| `0x238` | 2 | `u16` | `ManaCost` |  |
| `0x23A` | 2 | `u16` | `TooltipStringId` |  |
| `0x23C` | 2 | `u16` | `NameStringId` |  |
| `0x23E` | 2 | `u16` | `GeneralDescriptionStringId` |  |
| `0x240` | 2 | `u16` | `StrengthStringId` |  |
| `0x242` | 2 | `u16` | `WeaknessStringId` |  |
| `0x244` | 1 | `u8` | `DoorId` |  |
| `0x245` | 1 | `u8` | `OrderInEditor` |  |
| `0x246` | 1 | `u8` | `ManufCrateObjectId` |  |
| `0x247` | 1 | `u8` | `KeyObjectId` |  |
| `0x248` | 32 | `string` | `SoundCategory` |  |

## Traps — 579 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `MeshResource` |  |
| `0x74` | 84 | `ArtResource` | `GuiIcon` |  |
| `0xC8` | 84 | `ArtResource` | `EditorIcon` |  |
| `0x11C` | 84 | `ArtResource` | `FlowerIcon` |  |
| `0x170` | 84 | `ArtResource` | `FireResource` |  |
| `0x1C4` | 4 | `i32/4096` | `Height` |  |
| `0x1C8` | 4 | `i32/4096` | `RechargeTime` |  |
| `0x1CC` | 4 | `i32/4096` | `ChargeTime` |  |
| `0x1D0` | 4 | `i32/4096` | `ThreatDuration` |  |
| `0x1D4` | 4 | `u32` | `ManaCostToFire` |  |
| `0x1D8` | 4 | `i32/4096` | `IdleEffectDelay` |  |
| `0x1DC` | 4 | `u32` | `TriggerData` |  |
| `0x1E0` | 4 | `u32` | `ShotData1` |  |
| `0x1E4` | 4 | `u32` | `ShotData2` |  |
| `0x1E8` | 2 | `u16` | `ResearchTime` |  |
| `0x1EA` | 2 | `u16` | `Threat` |  |
| `0x1EC` | 4 | `flags32` | `Flags` | Trap.TrapFlag |
| `0x1F0` | 2 | `u16` | `Health` |  |
| `0x1F2` | 2 | `u16` | `GoldCost` |  |
| `0x1F4` | 2 | `u16` | `PowerlessEffectId` |  |
| `0x1F6` | 2 | `u16` | `IdleEffectId` |  |
| `0x1F8` | 2 | `u16` | `DeathEffectId` |  |
| `0x1FA` | 2 | `u16` | `ManufToBuild` |  |
| `0x1FC` | 2 | `u16` | `GeneralDescriptionStringId` |  |
| `0x1FE` | 2 | `u16` | `StrengthStringId` |  |
| `0x200` | 2 | `u16` | `WeaknessStringId` |  |
| `0x202` | 2 | `u16` | `ManaUsage` |  |
| `0x204` | 1 | `u8` | `unknown4[0].unknown4` |  НЕИЗВЕСТНО |
| `0x205` | 1 | `u8` | `unknown4[1].unknown4` |  НЕИЗВЕСТНО |
| `0x206` | 2 | `u16` | `TooltipStringId` |  |
| `0x208` | 2 | `u16` | `NameStringId` |  |
| `0x20A` | 1 | `u8` | `ShotsWhenArmed` |  |
| `0x20B` | 1 | `enum8` | `TriggerType` | Trap.TriggerType |
| `0x20C` | 1 | `u8` | `TrapId` |  |
| `0x20D` | 1 | `u8` | `ShotTypeId` |  |
| `0x20E` | 1 | `u8` | `ManufCrateObjectId` |  |
| `0x20F` | 32 | `string` | `SoundCategory` |  |
| `0x22F` | 1 | `enum8` | `Material` | Material |
| `0x230` | 1 | `u8` | `OrderInEditor` |  |
| `0x231` | 4 | `i32/4096` | `ShotOffset.0` |  |
| `0x235` | 4 | `i32/4096` | `ShotOffset.1` |  |
| `0x239` | 4 | `i32/4096` | `ShotOffset.2` |  |
| `0x23D` | 4 | `i32/4096` | `ShotDelay` |  |
| `0x241` | 2 | `u16` | `HealthGain` |  |

## KeeperSpells — 406 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `GuiIcon` |  |
| `0x74` | 84 | `ArtResource` | `EditorIcon` |  |
| `0xC8` | 4 | `i32` | `Xc8` |  |
| `0xCC` | 4 | `i32/4096` | `RechargeTime` |  |
| `0xD0` | 4 | `i32` | `ShotData1` |  |
| `0xD4` | 4 | `i32` | `ShotData2` |  |
| `0xD8` | 2 | `u16` | `ResearchTime` |  |
| `0xDA` | 1 | `enum8` | `TargetRule` | KeeperSpell.TargetRule |
| `0xDB` | 1 | `u8` | `OrderInEditor` |  |
| `0xDC` | 4 | `flags32` | `Flags` | KeeperSpell.KeeperSpellFlag |
| `0xE0` | 2 | `u16` | `Xe0Unreferenced` |  НЕИЗВЕСТНО |
| `0xE2` | 2 | `u16` | `ManaDrain` |  |
| `0xE4` | 2 | `u16` | `TooltipStringId` |  |
| `0xE6` | 2 | `u16` | `NameStringId` |  |
| `0xE8` | 2 | `u16` | `GeneralDescriptionStringId` |  |
| `0xEA` | 2 | `u16` | `StrengthStringId` |  |
| `0xEC` | 2 | `u16` | `WeaknessStringId` |  |
| `0xEE` | 1 | `u8` | `KeeperSpellId` |  |
| `0xEF` | 1 | `enum8` | `CastRule` | KeeperSpell.CastRule |
| `0xF0` | 1 | `u8` | `ShotTypeId` |  |
| `0xF1` | 32 | `string` | `SoundCategory` |  |
| `0x111` | 2 | `u16` | `BonusRTime` |  |
| `0x113` | 1 | `u8` | `BonusShotTypeId` |  |
| `0x114` | 4 | `i32` | `BonusShotData1` |  |
| `0x118` | 4 | `i32` | `BonusShotData2` |  |
| `0x11C` | 4 | `i32` | `ManaCost` |  |
| `0x120` | 84 | `ArtResource` | `BonusIcon` |  |
| `0x174` | 32 | `string` | `SoundCategoryGui` |  |
| `0x194` | 1 | `enum8` | `HandAnimId` | KeeperSpell.HandAnimId |
| `0x195` | 1 | `enum8` | `NoGoHandAnimId` | KeeperSpell.HandAnimId |

## CreatureSpells — 266 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `EditorIcon` |  |
| `0x74` | 84 | `ArtResource` | `GuiIcon` |  |
| `0xC8` | 4 | `u32` | `ShotData1` |  |
| `0xCC` | 4 | `u32` | `ShotData2` |  |
| `0xD0` | 4 | `i32/4096` | `Range` |  |
| `0xD4` | 4 | `flags32` | `Flags` | CreatureSpell.CreatureSpellFlag |
| `0xD8` | 2 | `u16` | `CombatPoints` |  |
| `0xDA` | 2 | `u16` | `SoundEvent` |  |
| `0xDC` | 2 | `u16` | `NameStringId` |  |
| `0xDE` | 2 | `u16` | `TooltipStringId` |  |
| `0xE0` | 2 | `u16` | `GeneralDescriptionStringId` |  |
| `0xE2` | 2 | `u16` | `StrengthStringId` |  |
| `0xE4` | 2 | `u16` | `WeaknessStringId` |  |
| `0xE6` | 1 | `u8` | `CreatureSpellId` |  |
| `0xE7` | 1 | `u8` | `ShotTypeId` |  |
| `0xE8` | 1 | `u8` | `AlternativeShotId` |  |
| `0xE9` | 1 | `u8` | `AlternativeRoomId` |  |
| `0xEA` | 4 | `i32/4096` | `RechargeTime` |  |
| `0xEE` | 1 | `enum8` | `AlternativeShot` | CreatureSpell.AlternativeShot |
| `0xEF` | 1 | `u8` | `data3[0].data3` |  |
| `0xF0` | 1 | `u8` | `data3[1].data3` |  |
| `0xF1` | 1 | `u8` | `data3[2].data3` |  |
| `0xF2` | 1 | `u8` | `data3[3].data3` |  |
| `0xF3` | 1 | `u8` | `data3[4].data3` |  |
| `0xF4` | 1 | `u8` | `data3[5].data3` |  |
| `0xF5` | 1 | `u8` | `data3[6].data3` |  |
| `0xF6` | 1 | `u8` | `data3[7].data3` |  |
| `0xF7` | 1 | `u8` | `data3[8].data3` |  |
| `0xF8` | 1 | `u8` | `data3[9].data3` |  |
| `0xF9` | 1 | `u8` | `data3[10].data3` |  |
| `0xFA` | 1 | `u8` | `data3[11].data3` |  |
| `0xFB` | 1 | `u8` | `data3[12].data3` |  |
| `0xFC` | 1 | `u8` | `data3[13].data3` |  |
| `0xFD` | 1 | `u8` | `data3[14].data3` |  |
| `0xFE` | 1 | `u8` | `data3[15].data3` |  |
| `0xFF` | 1 | `u8` | `data3[16].data3` |  |
| `0x100` | 1 | `u8` | `data3[17].data3` |  |
| `0x101` | 1 | `u8` | `data3[18].data3` |  |
| `0x102` | 1 | `u8` | `data3[19].data3` |  |
| `0x103` | 1 | `u8` | `data3[20].data3` |  |
| `0x104` | 1 | `u8` | `data3[21].data3` |  |
| `0x105` | 1 | `u8` | `data3[22].data3` |  |
| `0x106` | 1 | `u8` | `data3[23].data3` |  |
| `0x107` | 1 | `u8` | `data3[24].data3` |  |
| `0x108` | 1 | `u8` | `data3[25].data3` |  |
| `0x109` | 1 | `u8` | `data3[26].data3` |  |

## Creatures — 5449 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `bytes` | `Unknown1Resource` |  НЕИЗВЕСТНО |
| `0x74` | 84 | `ArtResource` | `Animation.WALK` |  |
| `0xC8` | 84 | `ArtResource` | `Animation.RUN` |  |
| `0x11C` | 84 | `ArtResource` | `Animation.DRAGGED` |  |
| `0x170` | 84 | `ArtResource` | `Animation.RECOIL_FORWARDS` |  |
| `0x1C4` | 84 | `ArtResource` | `Animation.MELEE_ATTACK` |  |
| `0x218` | 84 | `ArtResource` | `Animation.CAST_SPELL` |  |
| `0x26C` | 84 | `ArtResource` | `Animation.DIE` |  |
| `0x2C0` | 84 | `ArtResource` | `Animation.HAPPY` |  |
| `0x314` | 84 | `ArtResource` | `Animation.ANGRY` |  |
| `0x368` | 84 | `ArtResource` | `Animation.STUNNED` |  |
| `0x3BC` | 84 | `ArtResource` | `Animation.IN_HAND` |  |
| `0x410` | 84 | `ArtResource` | `Animation.SLEEPING` |  |
| `0x464` | 84 | `ArtResource` | `Animation.EATING` |  |
| `0x4B8` | 84 | `ArtResource` | `Animation.RESEARCHING` |  |
| `0x50C` | 84 | `ArtResource` | `Animation.NULL_2` |  |
| `0x560` | 84 | `ArtResource` | `Animation.NULL_1` |  |
| `0x5B4` | 84 | `ArtResource` | `Animation.TORTURED_WHEEL` |  |
| `0x608` | 84 | `ArtResource` | `Animation.NULL_3` |  |
| `0x65C` | 84 | `ArtResource` | `Animation.DRINKING` |  |
| `0x6B0` | 84 | `ArtResource` | `Animation.IDLE_1` |  |
| `0x704` | 84 | `ArtResource` | `Animation.RECOIL_BACKWARDS` |  |
| `0x758` | 84 | `ArtResource` | `Animation.MANUFACTURING` |  |
| `0x7AC` | 84 | `ArtResource` | `Animation.PRAYING` |  |
| `0x800` | 84 | `ArtResource` | `Animation.FALLBACK` |  |
| `0x854` | 84 | `ArtResource` | `Animation.TORTURED_CHAIR` |  |
| `0x8A8` | 84 | `ArtResource` | `Animation.TORTURED_CHAIR_SKELETON` |  |
| `0x8FC` | 84 | `ArtResource` | `Animation.GET_UP` |  |
| `0x950` | 84 | `ArtResource` | `Animation.DANCE` |  |
| `0x9A4` | 84 | `ArtResource` | `Animation.DRUNK` |  НЕИЗВЕСТНО |
| `0x9F8` | 84 | `ArtResource` | `Animation.ENTRANCE` |  |
| `0xA4C` | 84 | `ArtResource` | `Animation.IDLE_2` |  |
| `0xAA0` | 84 | `ArtResource` | `Animation.SPECIAL_1` |  |
| `0xAF4` | 84 | `ArtResource` | `Animation.SPECIAL_2` |  |
| `0xB48` | 84 | `ArtResource` | `Animation.DRUNKED_WALK` |  НЕИЗВЕСТНО |
| `0xB9C` | 84 | `ArtResource` | `Animation.ROAR` |  |
| `0xBF0` | 84 | `ArtResource` | `Animation.NULL_4` |  |
| `0xC44` | 84 | `ArtResource` | `Icon1Resource` |  |
| `0xC98` | 84 | `ArtResource` | `Icon2Resource` |  |
| `0xCEC` | 2 | `u16` | `Unkcec` |  НЕИЗВЕСТНО |
| `0xCEE` | 4 | `u32` | `Unkcee` |  НЕИЗВЕСТНО |
| `0xCF2` | 4 | `u32` | `Unkcf2` |  НЕИЗВЕСТНО |
| `0xCF6` | 1 | `u8` | `OrderInEditor` |  |
| `0xCF7` | 2 | `u16` | `AngerStringIdGeneral` |  |
| `0xCF9` | 4 | `i32/4096` | `ShotDelay` |  |
| `0xCFD` | 2 | `u16` | `OlhiEffectId` |  |
| `0xCFF` | 2 | `u16` | `IntroductionStringId` |  |
| `0xD01` | 4 | `i32/4096` | `PerceptionRange` |  |
| `0xD05` | 2 | `u16` | `AngerStringIdLair` |  |
| `0xD07` | 2 | `u16` | `AngerStringIdFood` |  |
| `0xD09` | 2 | `u16` | `AngerStringIdPay` |  |
| `0xD0B` | 2 | `u16` | `AngerStringIdWork` |  |
| `0xD0D` | 2 | `u16` | `AngerStringIdSlap` |  |
| `0xD0F` | 2 | `u16` | `AngerStringIdHeld` |  |
| `0xD11` | 2 | `u16` | `AngerStringIdLonely` |  |
| `0xD13` | 2 | `u16` | `AngerStringIdHatred` |  |
| `0xD15` | 2 | `u16` | `AngerStringIdTorture` |  |
| `0xD17` | 32 | `string` | `TranslationSoundGategory` |  |
| `0xD37` | 4 | `i32/4096` | `ShuffleSpeed` |  |
| `0xD3B` | 1 | `u8` | `CloneCreatureId` |  |
| `0xD3C` | 1 | `enum8` | `FirstPersonGammaEffect` | Creature.GammaEffect |
| `0xD3D` | 1 | `u8` | `FirstPersonWalkCycleScale` |  |
| `0xD3E` | 1 | `u8` | `IntroCameraPathIndex` |  |
| `0xD3F` | 1 | `u8` | `Unk2e2` |  НЕИЗВЕСТНО |
| `0xD40` | 84 | `ArtResource` | `PortraitResource` |  |
| `0xD94` | 24 | `Light` | `Light` |  |
| `0xDAC` | 4 | `u32` | `attractions[0].Present` |  |
| `0xDB0` | 2 | `u16` | `attractions[0].RoomId` |  |
| `0xDB2` | 2 | `u16` | `attractions[0].RoomSize` |  |
| `0xDB4` | 4 | `u32` | `attractions[1].Present` |  |
| `0xDB8` | 2 | `u16` | `attractions[1].RoomId` |  |
| `0xDBA` | 2 | `u16` | `attractions[1].RoomSize` |  |
| `0xDBC` | 4 | `i32/4096` | `FirstPersonWaddleScale` |  |
| `0xDC0` | 4 | `i32/4096` | `FirstPersonOscillateScale` |  |
| `0xDC4` | 4 | `i32/4096` | `Spells[0].ShotOffset.0` |  |
| `0xDC8` | 4 | `i32/4096` | `Spells[0].ShotOffset.1` |  |
| `0xDCC` | 4 | `i32/4096` | `Spells[0].ShotOffset.2` |  |
| `0xDD0` | 1 | `u8` | `Spells[0].X0c` |  НЕИЗВЕСТНО |
| `0xDD1` | 1 | `u8` | `Spells[0].PlayAnimation` |  |
| `0xDD2` | 1 | `u8` | `Spells[0].X0e` |  НЕИЗВЕСТНО |
| `0xDD3` | 1 | `u8` | `Spells[0].X0f` |  НЕИЗВЕСТНО |
| `0xDD4` | 4 | `i32/4096` | `Spells[0].ShotDelay` |  |
| `0xDD8` | 1 | `u8` | `Spells[0].X14` |  НЕИЗВЕСТНО |
| `0xDD9` | 1 | `u8` | `Spells[0].X15` |  НЕИЗВЕСТНО |
| `0xDDA` | 1 | `u8` | `Spells[0].CreatureSpellId` |  |
| `0xDDB` | 1 | `u8` | `Spells[0].LevelAvailable` |  |
| `0xDDC` | 4 | `i32/4096` | `Spells[1].ShotOffset.0` |  |
| `0xDE0` | 4 | `i32/4096` | `Spells[1].ShotOffset.1` |  |
| `0xDE4` | 4 | `i32/4096` | `Spells[1].ShotOffset.2` |  |
| `0xDE8` | 1 | `u8` | `Spells[1].X0c` |  НЕИЗВЕСТНО |
| `0xDE9` | 1 | `u8` | `Spells[1].PlayAnimation` |  |
| `0xDEA` | 1 | `u8` | `Spells[1].X0e` |  НЕИЗВЕСТНО |
| `0xDEB` | 1 | `u8` | `Spells[1].X0f` |  НЕИЗВЕСТНО |
| `0xDEC` | 4 | `i32/4096` | `Spells[1].ShotDelay` |  |
| `0xDF0` | 1 | `u8` | `Spells[1].X14` |  НЕИЗВЕСТНО |
| `0xDF1` | 1 | `u8` | `Spells[1].X15` |  НЕИЗВЕСТНО |
| `0xDF2` | 1 | `u8` | `Spells[1].CreatureSpellId` |  |
| `0xDF3` | 1 | `u8` | `Spells[1].LevelAvailable` |  |
| `0xDF4` | 4 | `i32/4096` | `Spells[2].ShotOffset.0` |  |
| `0xDF8` | 4 | `i32/4096` | `Spells[2].ShotOffset.1` |  |
| `0xDFC` | 4 | `i32/4096` | `Spells[2].ShotOffset.2` |  |
| `0xE00` | 1 | `u8` | `Spells[2].X0c` |  НЕИЗВЕСТНО |
| `0xE01` | 1 | `u8` | `Spells[2].PlayAnimation` |  |
| `0xE02` | 1 | `u8` | `Spells[2].X0e` |  НЕИЗВЕСТНО |
| `0xE03` | 1 | `u8` | `Spells[2].X0f` |  НЕИЗВЕСТНО |
| `0xE04` | 4 | `i32/4096` | `Spells[2].ShotDelay` |  |
| `0xE08` | 1 | `u8` | `Spells[2].X14` |  НЕИЗВЕСТНО |
| `0xE09` | 1 | `u8` | `Spells[2].X15` |  НЕИЗВЕСТНО |
| `0xE0A` | 1 | `u8` | `Spells[2].CreatureSpellId` |  |
| `0xE0B` | 1 | `u8` | `Spells[2].LevelAvailable` |  |
| `0xE0C` | 1 | `enum8` | `resistances[0].AttackType` | Creature.AttackType |
| `0xE0D` | 1 | `u8` | `resistances[0].Value` |  |
| `0xE0E` | 1 | `enum8` | `resistances[1].AttackType` | Creature.AttackType |
| `0xE0F` | 1 | `u8` | `resistances[1].Value` |  |
| `0xE10` | 1 | `enum8` | `resistances[2].AttackType` | Creature.AttackType |
| `0xE11` | 1 | `u8` | `resistances[2].Value` |  |
| `0xE12` | 1 | `enum8` | `resistances[3].AttackType` | Creature.AttackType |
| `0xE13` | 1 | `u8` | `resistances[3].Value` |  |
| `0xE14` | 36 | `JobPreference[3]` | `HappyJobs` |  |
| `0xE38` | 24 | `JobPreference[2]` | `UnhappyJobs` |  |
| `0xE50` | 36 | `JobPreference[3]` | `AngryJobs` |  |
| `0xE74` | 4 | `enum32` | `hateJobs[0].hateJobs` | Creature.JobType |
| `0xE78` | 4 | `enum32` | `hateJobs[1].hateJobs` | Creature.JobType |
| `0xE7C` | 4 | `enum32` | `alternatives[0].JobType` | Creature.JobType |
| `0xE80` | 2 | `u16` | `alternatives[0].MoodChange` |  |
| `0xE82` | 2 | `u16` | `alternatives[0].ManaChange` |  |
| `0xE84` | 4 | `enum32` | `alternatives[1].JobType` | Creature.JobType |
| `0xE88` | 2 | `u16` | `alternatives[1].MoodChange` |  |
| `0xE8A` | 2 | `u16` | `alternatives[1].ManaChange` |  |
| `0xE8C` | 4 | `enum32` | `alternatives[2].JobType` | Creature.JobType |
| `0xE90` | 2 | `u16` | `alternatives[2].MoodChange` |  |
| `0xE92` | 2 | `u16` | `alternatives[2].ManaChange` |  |
| `0xE94` | 4 | `i32/4096` | `AnimationOffsets.PORTAL_ENTRANCE.0` |  |
| `0xE98` | 4 | `i32/4096` | `AnimationOffsets.PORTAL_ENTRANCE.1` |  |
| `0xE9C` | 4 | `i32/4096` | `AnimationOffsets.PORTAL_ENTRANCE.2` |  |
| `0xEA0` | 4 | `i32` | `Unkea0` |  НЕИЗВЕСТНО |
| `0xEA4` | 4 | `i32/4096` | `Height` |  |
| `0xEA8` | 4 | `i32/4096` | `Unkea8` |  НЕИЗВЕСТНО |
| `0xEAC` | 4 | `u32` | `Unk3ab` |  НЕИЗВЕСТНО |
| `0xEB0` | 4 | `i32/4096` | `EyeHeight` |  |
| `0xEB4` | 4 | `i32/4096` | `Speed` |  |
| `0xEB8` | 4 | `i32/4096` | `RunSpeed` |  |
| `0xEBC` | 4 | `i32/4096` | `HungerRate` |  |
| `0xEC0` | 4 | `u32` | `TimeAwake` |  |
| `0xEC4` | 4 | `u32` | `TimeSleep` |  |
| `0xEC8` | 4 | `i32/4096` | `DistanceCanSee` |  |
| `0xECC` | 4 | `i32/4096` | `DistanceCanHear` |  |
| `0xED0` | 4 | `i32/4096` | `StunDuration` |  |
| `0xED4` | 4 | `i32/4096` | `GuardDuration` |  |
| `0xED8` | 4 | `i32/4096` | `IdleDuration` |  |
| `0xEDC` | 4 | `i32/4096` | `SlapFearlessDuration` |  |
| `0xEE0` | 4 | `i32` | `Unkee0` |  НЕИЗВЕСТНО |
| `0xEE4` | 4 | `i32` | `Unkee4` |  НЕИЗВЕСТНО |
| `0xEE8` | 2 | `i16` | `PossessionManaCost` |  |
| `0xEEA` | 2 | `i16` | `OwnLandHealthIncrease` |  |
| `0xEEC` | 4 | `i32/4096` | `MeleeRange` |  |
| `0xEF0` | 4 | `u32` | `Unkef0` |  НЕИЗВЕСТНО |
| `0xEF4` | 4 | `i32/4096` | `TortureTimeToConvert` |  |
| `0xEF8` | 4 | `i32/4096` | `MeleeRecharge` |  |
| `0xEFC` | 4 | `flags32` | `Flags` | Creature.CreatureFlag |
| `0xF00` | 2 | `u16` | `ExpForNextLevel` |  |
| `0xF02` | 1 | `enum8` | `JobClass` | Creature.JobClass |
| `0xF03` | 1 | `enum8` | `FightStyle` | Creature.FightStyle |
| `0xF04` | 2 | `u16` | `ExpPerSecond` |  |
| `0xF06` | 2 | `u16` | `ExpPerSecondTraining` |  |
| `0xF08` | 2 | `u16` | `ResearchPerSecond` |  |
| `0xF0A` | 2 | `u16` | `ManufacturePerSecond` |  |
| `0xF0C` | 2 | `u16` | `Hp` |  |
| `0xF0E` | 2 | `u16` | `HpFromChicken` |  |
| `0xF10` | 2 | `u16` | `Fear` |  |
| `0xF12` | 2 | `u16` | `Threat` |  |
| `0xF14` | 2 | `u16` | `MeleeDamage` |  |
| `0xF16` | 2 | `u16` | `SlapDamage` |  |
| `0xF18` | 2 | `u16` | `ManaGenPrayer` |  |
| `0xF1A` | 2 | `u16` | `Unk3cb` |  НЕИЗВЕСТНО |
| `0xF1C` | 2 | `u16` | `Pay` |  |
| `0xF1E` | 2 | `u16` | `MaxGoldHeld` |  |
| `0xF20` | 2 | `i16/4096` | `Unk3cc` |  НЕИЗВЕСТНО |
| `0xF22` | 2 | `u16` | `DecomposeValue` |  |
| `0xF24` | 2 | `u16` | `NameStringId` |  |
| `0xF26` | 2 | `u16` | `TooltipStringId` |  |
| `0xF28` | 2 | `i16` | `AngerNoLair` |  |
| `0xF2A` | 2 | `i16` | `AngerNoFood` |  |
| `0xF2C` | 2 | `i16` | `AngerNoPay` |  |
| `0xF2E` | 2 | `i16` | `AngerNoWork` |  |
| `0xF30` | 2 | `i16` | `AngerSlap` |  |
| `0xF32` | 2 | `i16` | `AngerInHand` |  |
| `0xF34` | 2 | `i16` | `InitialGoldHeld` |  |
| `0xF36` | 2 | `u16` | `EntranceEffectId` |  |
| `0xF38` | 2 | `u16` | `GeneralDescriptionStringId` |  |
| `0xF3A` | 2 | `u16` | `StrengthStringId` |  |
| `0xF3C` | 2 | `u16` | `WeaknessStringId` |  |
| `0xF3E` | 2 | `u16` | `SlapEffectId` |  |
| `0xF40` | 2 | `u16` | `DeathEffectId` |  |
| `0xF42` | 1 | `enum8` | `Melee1Swipe` | Creature.Swipe |
| `0xF43` | 1 | `enum8` | `Melee2Swipe` | Creature.Swipe |
| `0xF44` | 1 | `enum8` | `Melee3Swipe` | Creature.Swipe |
| `0xF45` | 1 | `enum8` | `SpellSwipe` | Creature.Swipe |
| `0xF46` | 1 | `enum8` | `FirstPersonSpecialAbility1` | Creature.SpecialAbility |
| `0xF47` | 1 | `enum8` | `FirstPersonSpecialAbility2` | Creature.SpecialAbility |
| `0xF48` | 1 | `u8` | `unkf48[0].unkf48` |  НЕИЗВЕСТНО |
| `0xF49` | 1 | `u8` | `unkf48[1].unkf48` |  НЕИЗВЕСТНО |
| `0xF4A` | 1 | `u8` | `unkf48[2].unkf48` |  НЕИЗВЕСТНО |
| `0xF4B` | 1 | `u8` | `CreatureId` |  |
| `0xF4C` | 1 | `u8` | `unk3ea[0].unk3ea` |  НЕИЗВЕСТНО |
| `0xF4D` | 1 | `u8` | `unk3ea[1].unk3ea` |  НЕИЗВЕСТНО |
| `0xF4E` | 1 | `u8` | `HungerFill` |  |
| `0xF4F` | 1 | `u8` | `UnhappyThreshold` |  |
| `0xF50` | 1 | `enum8` | `MeleeAttackType` | Creature.AttackType |
| `0xF51` | 1 | `u8` | `Unk3eb2` |  НЕИЗВЕСТНО |
| `0xF52` | 1 | `u8` | `LairObjectId` |  |
| `0xF53` | 1 | `u8` | `Unk3f1` |  НЕИЗВЕСТНО |
| `0xF54` | 1 | `enum8` | `DeathFallDirection` | Creature.DeathFallDirection |
| `0xF55` | 1 | `u8` | `Unk3f2` |  НЕИЗВЕСТНО |
| `0xF56` | 32 | `string` | `SoundCategory` |  |
| `0xF76` | 1 | `enum8` | `Material` | Material |
| `0xF77` | 84 | `ArtResource` | `FirstPersonFilterResource` |  |
| `0xFCB` | 2 | `u16` | `Unkfcb` |  НЕИЗВЕСТНО |
| `0xFCD` | 4 | `i32/4096` | `Unk4` |  НЕИЗВЕСТНО |
| `0xFD1` | 84 | `ArtResource` | `Animation.DRUNKED_IDLE` |  НЕИЗВЕСТНО |
| `0x1025` | 1 | `enum8` | `Special1Swipe` | Creature.Swipe |
| `0x1026` | 1 | `enum8` | `Special2Swipe` | Creature.Swipe |
| `0x1027` | 84 | `ArtResource` | `FirstPersonMeleeResource` |  |
| `0x107B` | 4 | `u32` | `Unk6` |  НЕИЗВЕСТНО |
| `0x107F` | 2 | `i16` | `TortureHpChange` |  |
| `0x1081` | 2 | `i16` | `TortureMoodChange` |  |
| `0x1083` | 84 | `ArtResource` | `Animation.SWIPE` |  |
| `0x10D7` | 84 | `ArtResource` | `Animation.IDLE_3` |  |
| `0x112B` | 84 | `ArtResource` | `Animation.IDLE_4` |  |
| `0x117F` | 84 | `ArtResource` | `Animation.IDLE_3_1` |  |
| `0x11D3` | 84 | `ArtResource` | `Animation.IDLE_4_1` |  |
| `0x1227` | 84 | `ArtResource` | `Animation.DIG` |  |
| `0x127B` | 4 | `i32/4096` | `AnimationOffsets[0].AnimationOffsets.0` |  |
| `0x127F` | 4 | `i32/4096` | `AnimationOffsets[0].AnimationOffsets.1` |  |
| `0x1283` | 4 | `i32/4096` | `AnimationOffsets[0].AnimationOffsets.2` |  |
| `0x1287` | 4 | `i32/4096` | `AnimationOffsets[1].AnimationOffsets.0` |  |
| `0x128B` | 4 | `i32/4096` | `AnimationOffsets[1].AnimationOffsets.1` |  |
| `0x128F` | 4 | `i32/4096` | `AnimationOffsets[1].AnimationOffsets.2` |  |
| `0x1293` | 4 | `i32/4096` | `AnimationOffsets[2].AnimationOffsets.0` |  |
| `0x1297` | 4 | `i32/4096` | `AnimationOffsets[2].AnimationOffsets.1` |  |
| `0x129B` | 4 | `i32/4096` | `AnimationOffsets[2].AnimationOffsets.2` |  |
| `0x129F` | 4 | `i32/4096` | `AnimationOffsets[3].AnimationOffsets.0` |  |
| `0x12A3` | 4 | `i32/4096` | `AnimationOffsets[3].AnimationOffsets.1` |  |
| `0x12A7` | 4 | `i32/4096` | `AnimationOffsets[3].AnimationOffsets.2` |  |
| `0x12AB` | 4 | `i32/4096` | `AnimationOffsets[4].AnimationOffsets.0` |  |
| `0x12AF` | 4 | `i32/4096` | `AnimationOffsets[4].AnimationOffsets.1` |  |
| `0x12B3` | 4 | `i32/4096` | `AnimationOffsets[4].AnimationOffsets.2` |  |
| `0x12B7` | 4 | `i32/4096` | `AnimationOffsets[5].AnimationOffsets.0` |  |
| `0x12BB` | 4 | `i32/4096` | `AnimationOffsets[5].AnimationOffsets.1` |  |
| `0x12BF` | 4 | `i32/4096` | `AnimationOffsets[5].AnimationOffsets.2` |  |
| `0x12C3` | 4 | `i32/4096` | `AnimationOffsets[6].AnimationOffsets.0` |  |
| `0x12C7` | 4 | `i32/4096` | `AnimationOffsets[6].AnimationOffsets.1` |  |
| `0x12CB` | 4 | `i32/4096` | `AnimationOffsets[6].AnimationOffsets.2` |  |
| `0x12CF` | 84 | `ArtResource` | `Animation.BACK_OFF` |  |
| `0x1323` | 2 | `u16` | `x1323s[0].X00` |  НЕИЗВЕСТНО |
| `0x1325` | 2 | `u16` | `x1323s[0].X02` |  НЕИЗВЕСТНО |
| `0x1327` | 2 | `u16` | `x1323s[1].X00` |  НЕИЗВЕСТНО |
| `0x1329` | 2 | `u16` | `x1323s[1].X02` |  НЕИЗВЕСТНО |
| `0x132B` | 2 | `u16` | `x1323s[2].X00` |  НЕИЗВЕСТНО |
| `0x132D` | 2 | `u16` | `x1323s[2].X02` |  НЕИЗВЕСТНО |
| `0x132F` | 2 | `u16` | `x1323s[3].X00` |  НЕИЗВЕСТНО |
| `0x1331` | 2 | `u16` | `x1323s[3].X02` |  НЕИЗВЕСТНО |
| `0x1333` | 2 | `u16` | `x1323s[4].X00` |  НЕИЗВЕСТНО |
| `0x1335` | 2 | `u16` | `x1323s[4].X02` |  НЕИЗВЕСТНО |
| `0x1337` | 2 | `u16` | `x1323s[5].X00` |  НЕИЗВЕСТНО |
| `0x1339` | 2 | `u16` | `x1323s[5].X02` |  НЕИЗВЕСТНО |
| `0x133B` | 2 | `u16` | `x1323s[6].X00` |  НЕИЗВЕСТНО |
| `0x133D` | 2 | `u16` | `x1323s[6].X02` |  НЕИЗВЕСТНО |
| `0x133F` | 2 | `u16` | `x1323s[7].X00` |  НЕИЗВЕСТНО |
| `0x1341` | 2 | `u16` | `x1323s[7].X02` |  НЕИЗВЕСТНО |
| `0x1343` | 2 | `u16` | `x1323s[8].X00` |  НЕИЗВЕСТНО |
| `0x1345` | 2 | `u16` | `x1323s[8].X02` |  НЕИЗВЕСТНО |
| `0x1347` | 2 | `u16` | `x1323s[9].X00` |  НЕИЗВЕСТНО |
| `0x1349` | 2 | `u16` | `x1323s[9].X02` |  НЕИЗВЕСТНО |
| `0x134B` | 2 | `u16` | `x1323s[10].X00` |  НЕИЗВЕСТНО |
| `0x134D` | 2 | `u16` | `x1323s[10].X02` |  НЕИЗВЕСТНО |
| `0x134F` | 2 | `u16` | `x1323s[11].X00` |  НЕИЗВЕСТНО |
| `0x1351` | 2 | `u16` | `x1323s[11].X02` |  НЕИЗВЕСТНО |
| `0x1353` | 2 | `u16` | `x1323s[12].X00` |  НЕИЗВЕСТНО |
| `0x1355` | 2 | `u16` | `x1323s[12].X02` |  НЕИЗВЕСТНО |
| `0x1357` | 2 | `u16` | `x1323s[13].X00` |  НЕИЗВЕСТНО |
| `0x1359` | 2 | `u16` | `x1323s[13].X02` |  НЕИЗВЕСТНО |
| `0x135B` | 2 | `u16` | `x1323s[14].X00` |  НЕИЗВЕСТНО |
| `0x135D` | 2 | `u16` | `x1323s[14].X02` |  НЕИЗВЕСТНО |
| `0x135F` | 2 | `u16` | `x1323s[15].X00` |  НЕИЗВЕСТНО |
| `0x1361` | 2 | `u16` | `x1323s[15].X02` |  НЕИЗВЕСТНО |
| `0x1363` | 2 | `u16` | `x1323s[16].X00` |  НЕИЗВЕСТНО |
| `0x1365` | 2 | `u16` | `x1323s[16].X02` |  НЕИЗВЕСТНО |
| `0x1367` | 2 | `u16` | `x1323s[17].X00` |  НЕИЗВЕСТНО |
| `0x1369` | 2 | `u16` | `x1323s[17].X02` |  НЕИЗВЕСТНО |
| `0x136B` | 2 | `u16` | `x1323s[18].X00` |  НЕИЗВЕСТНО |
| `0x136D` | 2 | `u16` | `x1323s[18].X02` |  НЕИЗВЕСТНО |
| `0x136F` | 2 | `u16` | `x1323s[19].X00` |  НЕИЗВЕСТНО |
| `0x1371` | 2 | `u16` | `x1323s[19].X02` |  НЕИЗВЕСТНО |
| `0x1373` | 2 | `u16` | `x1323s[20].X00` |  НЕИЗВЕСТНО |
| `0x1375` | 2 | `u16` | `x1323s[20].X02` |  НЕИЗВЕСТНО |
| `0x1377` | 2 | `u16` | `x1323s[21].X00` |  НЕИЗВЕСТНО |
| `0x1379` | 2 | `u16` | `x1323s[21].X02` |  НЕИЗВЕСТНО |
| `0x137B` | 2 | `u16` | `x1323s[22].X00` |  НЕИЗВЕСТНО |
| `0x137D` | 2 | `u16` | `x1323s[22].X02` |  НЕИЗВЕСТНО |
| `0x137F` | 2 | `u16` | `x1323s[23].X00` |  НЕИЗВЕСТНО |
| `0x1381` | 2 | `u16` | `x1323s[23].X02` |  НЕИЗВЕСТНО |
| `0x1383` | 2 | `u16` | `x1323s[24].X00` |  НЕИЗВЕСТНО |
| `0x1385` | 2 | `u16` | `x1323s[24].X02` |  НЕИЗВЕСТНО |
| `0x1387` | 2 | `u16` | `x1323s[25].X00` |  НЕИЗВЕСТНО |
| `0x1389` | 2 | `u16` | `x1323s[25].X02` |  НЕИЗВЕСТНО |
| `0x138B` | 2 | `u16` | `x1323s[26].X00` |  НЕИЗВЕСТНО |
| `0x138D` | 2 | `u16` | `x1323s[26].X02` |  НЕИЗВЕСТНО |
| `0x138F` | 2 | `u16` | `x1323s[27].X00` |  НЕИЗВЕСТНО |
| `0x1391` | 2 | `u16` | `x1323s[27].X02` |  НЕИЗВЕСТНО |
| `0x1393` | 2 | `u16` | `x1323s[28].X00` |  НЕИЗВЕСТНО |
| `0x1395` | 2 | `u16` | `x1323s[28].X02` |  НЕИЗВЕСТНО |
| `0x1397` | 2 | `u16` | `x1323s[29].X00` |  НЕИЗВЕСТНО |
| `0x1399` | 2 | `u16` | `x1323s[29].X02` |  НЕИЗВЕСТНО |
| `0x139B` | 2 | `u16` | `x1323s[30].X00` |  НЕИЗВЕСТНО |
| `0x139D` | 2 | `u16` | `x1323s[30].X02` |  НЕИЗВЕСТНО |
| `0x139F` | 2 | `u16` | `x1323s[31].X00` |  НЕИЗВЕСТНО |
| `0x13A1` | 2 | `u16` | `x1323s[31].X02` |  НЕИЗВЕСТНО |
| `0x13A3` | 2 | `u16` | `x1323s[32].X00` |  НЕИЗВЕСТНО |
| `0x13A5` | 2 | `u16` | `x1323s[32].X02` |  НЕИЗВЕСТНО |
| `0x13A7` | 2 | `u16` | `x1323s[33].X00` |  НЕИЗВЕСТНО |
| `0x13A9` | 2 | `u16` | `x1323s[33].X02` |  НЕИЗВЕСТНО |
| `0x13AB` | 2 | `u16` | `x1323s[34].X00` |  НЕИЗВЕСТНО |
| `0x13AD` | 2 | `u16` | `x1323s[34].X02` |  НЕИЗВЕСТНО |
| `0x13AF` | 2 | `u16` | `x1323s[35].X00` |  НЕИЗВЕСТНО |
| `0x13B1` | 2 | `u16` | `x1323s[35].X02` |  НЕИЗВЕСТНО |
| `0x13B3` | 2 | `u16` | `x1323s[36].X00` |  НЕИЗВЕСТНО |
| `0x13B5` | 2 | `u16` | `x1323s[36].X02` |  НЕИЗВЕСТНО |
| `0x13B7` | 2 | `u16` | `x1323s[37].X00` |  НЕИЗВЕСТНО |
| `0x13B9` | 2 | `u16` | `x1323s[37].X02` |  НЕИЗВЕСТНО |
| `0x13BB` | 2 | `u16` | `x1323s[38].X00` |  НЕИЗВЕСТНО |
| `0x13BD` | 2 | `u16` | `x1323s[38].X02` |  НЕИЗВЕСТНО |
| `0x13BF` | 2 | `u16` | `x1323s[39].X00` |  НЕИЗВЕСТНО |
| `0x13C1` | 2 | `u16` | `x1323s[39].X02` |  НЕИЗВЕСТНО |
| `0x13C3` | 2 | `u16` | `x1323s[40].X00` |  НЕИЗВЕСТНО |
| `0x13C5` | 2 | `u16` | `x1323s[40].X02` |  НЕИЗВЕСТНО |
| `0x13C7` | 2 | `u16` | `x1323s[41].X00` |  НЕИЗВЕСТНО |
| `0x13C9` | 2 | `u16` | `x1323s[41].X02` |  НЕИЗВЕСТНО |
| `0x13CB` | 2 | `u16` | `x1323s[42].X00` |  НЕИЗВЕСТНО |
| `0x13CD` | 2 | `u16` | `x1323s[42].X02` |  НЕИЗВЕСТНО |
| `0x13CF` | 2 | `u16` | `x1323s[43].X00` |  НЕИЗВЕСТНО |
| `0x13D1` | 2 | `u16` | `x1323s[43].X02` |  НЕИЗВЕСТНО |
| `0x13D3` | 2 | `u16` | `x1323s[44].X00` |  НЕИЗВЕСТНО |
| `0x13D5` | 2 | `u16` | `x1323s[44].X02` |  НЕИЗВЕСТНО |
| `0x13D7` | 2 | `u16` | `x1323s[45].X00` |  НЕИЗВЕСТНО |
| `0x13D9` | 2 | `u16` | `x1323s[45].X02` |  НЕИЗВЕСТНО |
| `0x13DB` | 2 | `u16` | `x1323s[46].X00` |  НЕИЗВЕСТНО |
| `0x13DD` | 2 | `u16` | `x1323s[46].X02` |  НЕИЗВЕСТНО |
| `0x13DF` | 2 | `u16` | `x1323s[47].X00` |  НЕИЗВЕСТНО |
| `0x13E1` | 2 | `u16` | `x1323s[47].X02` |  НЕИЗВЕСТНО |
| `0x13E3` | 84 | `ArtResource` | `Animation.STAND_STILL` |  |
| `0x1437` | 84 | `ArtResource` | `Animation.STEALTH_WALK` |  |
| `0x148B` | 84 | `ArtResource` | `Animation.DEATH_POSE` |  |
| `0x14DF` | 2 | `u16` | `UniqueNameTextId` |  |
| `0x14E1` | 4 | `u32` | `x14e1[0].x14e1` |  НЕИЗВЕСТНО |
| `0x14E5` | 4 | `u32` | `x14e1[1].x14e1` |  НЕИЗВЕСТНО |
| `0x14E9` | 4 | `u32` | `FirstPersonSpecialAbility1Count` |  |
| `0x14ED` | 4 | `u32` | `FirstPersonSpecialAbility2Count` |  |
| `0x14F1` | 84 | `ArtResource` | `UniqueResource` |  |
| `0x1545` | 4 | `flags32` | `Flags3` | Creature.CreatureFlag3 |

## Creatures_5537 — 5537 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `bytes` | `Unknown1Resource` |  НЕИЗВЕСТНО |
| `0x74` | 84 | `ArtResource` | `Animation.WALK` |  |
| `0xC8` | 84 | `ArtResource` | `Animation.RUN` |  |
| `0x11C` | 84 | `ArtResource` | `Animation.DRAGGED` |  |
| `0x170` | 84 | `ArtResource` | `Animation.RECOIL_FORWARDS` |  |
| `0x1C4` | 84 | `ArtResource` | `Animation.MELEE_ATTACK` |  |
| `0x218` | 84 | `ArtResource` | `Animation.CAST_SPELL` |  |
| `0x26C` | 84 | `ArtResource` | `Animation.DIE` |  |
| `0x2C0` | 84 | `ArtResource` | `Animation.HAPPY` |  |
| `0x314` | 84 | `ArtResource` | `Animation.ANGRY` |  |
| `0x368` | 84 | `ArtResource` | `Animation.STUNNED` |  |
| `0x3BC` | 84 | `ArtResource` | `Animation.IN_HAND` |  |
| `0x410` | 84 | `ArtResource` | `Animation.SLEEPING` |  |
| `0x464` | 84 | `ArtResource` | `Animation.EATING` |  |
| `0x4B8` | 84 | `ArtResource` | `Animation.RESEARCHING` |  |
| `0x50C` | 84 | `ArtResource` | `Animation.NULL_2` |  |
| `0x560` | 84 | `ArtResource` | `Animation.NULL_1` |  |
| `0x5B4` | 84 | `ArtResource` | `Animation.TORTURED_WHEEL` |  |
| `0x608` | 84 | `ArtResource` | `Animation.NULL_3` |  |
| `0x65C` | 84 | `ArtResource` | `Animation.DRINKING` |  |
| `0x6B0` | 84 | `ArtResource` | `Animation.IDLE_1` |  |
| `0x704` | 84 | `ArtResource` | `Animation.RECOIL_BACKWARDS` |  |
| `0x758` | 84 | `ArtResource` | `Animation.MANUFACTURING` |  |
| `0x7AC` | 84 | `ArtResource` | `Animation.PRAYING` |  |
| `0x800` | 84 | `ArtResource` | `Animation.FALLBACK` |  |
| `0x854` | 84 | `ArtResource` | `Animation.TORTURED_CHAIR` |  |
| `0x8A8` | 84 | `ArtResource` | `Animation.TORTURED_CHAIR_SKELETON` |  |
| `0x8FC` | 84 | `ArtResource` | `Animation.GET_UP` |  |
| `0x950` | 84 | `ArtResource` | `Animation.DANCE` |  |
| `0x9A4` | 84 | `ArtResource` | `Animation.DRUNK` |  НЕИЗВЕСТНО |
| `0x9F8` | 84 | `ArtResource` | `Animation.ENTRANCE` |  |
| `0xA4C` | 84 | `ArtResource` | `Animation.IDLE_2` |  |
| `0xAA0` | 84 | `ArtResource` | `Animation.SPECIAL_1` |  |
| `0xAF4` | 84 | `ArtResource` | `Animation.SPECIAL_2` |  |
| `0xB48` | 84 | `ArtResource` | `Animation.DRUNKED_WALK` |  НЕИЗВЕСТНО |
| `0xB9C` | 84 | `ArtResource` | `Animation.ROAR` |  |
| `0xBF0` | 84 | `ArtResource` | `Animation.NULL_4` |  |
| `0xC44` | 84 | `ArtResource` | `Icon1Resource` |  |
| `0xC98` | 84 | `ArtResource` | `Icon2Resource` |  |
| `0xCEC` | 2 | `u16` | `Unkcec` |  НЕИЗВЕСТНО |
| `0xCEE` | 4 | `u32` | `Unkcee` |  НЕИЗВЕСТНО |
| `0xCF2` | 4 | `u32` | `Unkcf2` |  НЕИЗВЕСТНО |
| `0xCF6` | 1 | `u8` | `OrderInEditor` |  |
| `0xCF7` | 2 | `u16` | `AngerStringIdGeneral` |  |
| `0xCF9` | 4 | `i32/4096` | `ShotDelay` |  |
| `0xCFD` | 2 | `u16` | `OlhiEffectId` |  |
| `0xCFF` | 2 | `u16` | `IntroductionStringId` |  |
| `0xD01` | 4 | `i32/4096` | `PerceptionRange` |  |
| `0xD05` | 2 | `u16` | `AngerStringIdLair` |  |
| `0xD07` | 2 | `u16` | `AngerStringIdFood` |  |
| `0xD09` | 2 | `u16` | `AngerStringIdPay` |  |
| `0xD0B` | 2 | `u16` | `AngerStringIdWork` |  |
| `0xD0D` | 2 | `u16` | `AngerStringIdSlap` |  |
| `0xD0F` | 2 | `u16` | `AngerStringIdHeld` |  |
| `0xD11` | 2 | `u16` | `AngerStringIdLonely` |  |
| `0xD13` | 2 | `u16` | `AngerStringIdHatred` |  |
| `0xD15` | 2 | `u16` | `AngerStringIdTorture` |  |
| `0xD17` | 32 | `string` | `TranslationSoundGategory` |  |
| `0xD37` | 4 | `i32/4096` | `ShuffleSpeed` |  |
| `0xD3B` | 1 | `u8` | `CloneCreatureId` |  |
| `0xD3C` | 1 | `enum8` | `FirstPersonGammaEffect` | Creature.GammaEffect |
| `0xD3D` | 1 | `u8` | `FirstPersonWalkCycleScale` |  |
| `0xD3E` | 1 | `u8` | `IntroCameraPathIndex` |  |
| `0xD3F` | 1 | `u8` | `Unk2e2` |  НЕИЗВЕСТНО |
| `0xD40` | 84 | `ArtResource` | `PortraitResource` |  |
| `0xD94` | 24 | `Light` | `Light` |  |
| `0xDAC` | 4 | `u32` | `attractions[0].Present` |  |
| `0xDB0` | 2 | `u16` | `attractions[0].RoomId` |  |
| `0xDB2` | 2 | `u16` | `attractions[0].RoomSize` |  |
| `0xDB4` | 4 | `u32` | `attractions[1].Present` |  |
| `0xDB8` | 2 | `u16` | `attractions[1].RoomId` |  |
| `0xDBA` | 2 | `u16` | `attractions[1].RoomSize` |  |
| `0xDBC` | 4 | `i32/4096` | `FirstPersonWaddleScale` |  |
| `0xDC0` | 4 | `i32/4096` | `FirstPersonOscillateScale` |  |
| `0xDC4` | 4 | `i32/4096` | `Spells[0].ShotOffset.0` |  |
| `0xDC8` | 4 | `i32/4096` | `Spells[0].ShotOffset.1` |  |
| `0xDCC` | 4 | `i32/4096` | `Spells[0].ShotOffset.2` |  |
| `0xDD0` | 1 | `u8` | `Spells[0].X0c` |  НЕИЗВЕСТНО |
| `0xDD1` | 1 | `u8` | `Spells[0].PlayAnimation` |  |
| `0xDD2` | 1 | `u8` | `Spells[0].X0e` |  НЕИЗВЕСТНО |
| `0xDD3` | 1 | `u8` | `Spells[0].X0f` |  НЕИЗВЕСТНО |
| `0xDD4` | 4 | `i32/4096` | `Spells[0].ShotDelay` |  |
| `0xDD8` | 1 | `u8` | `Spells[0].X14` |  НЕИЗВЕСТНО |
| `0xDD9` | 1 | `u8` | `Spells[0].X15` |  НЕИЗВЕСТНО |
| `0xDDA` | 1 | `u8` | `Spells[0].CreatureSpellId` |  |
| `0xDDB` | 1 | `u8` | `Spells[0].LevelAvailable` |  |
| `0xDDC` | 4 | `i32/4096` | `Spells[1].ShotOffset.0` |  |
| `0xDE0` | 4 | `i32/4096` | `Spells[1].ShotOffset.1` |  |
| `0xDE4` | 4 | `i32/4096` | `Spells[1].ShotOffset.2` |  |
| `0xDE8` | 1 | `u8` | `Spells[1].X0c` |  НЕИЗВЕСТНО |
| `0xDE9` | 1 | `u8` | `Spells[1].PlayAnimation` |  |
| `0xDEA` | 1 | `u8` | `Spells[1].X0e` |  НЕИЗВЕСТНО |
| `0xDEB` | 1 | `u8` | `Spells[1].X0f` |  НЕИЗВЕСТНО |
| `0xDEC` | 4 | `i32/4096` | `Spells[1].ShotDelay` |  |
| `0xDF0` | 1 | `u8` | `Spells[1].X14` |  НЕИЗВЕСТНО |
| `0xDF1` | 1 | `u8` | `Spells[1].X15` |  НЕИЗВЕСТНО |
| `0xDF2` | 1 | `u8` | `Spells[1].CreatureSpellId` |  |
| `0xDF3` | 1 | `u8` | `Spells[1].LevelAvailable` |  |
| `0xDF4` | 4 | `i32/4096` | `Spells[2].ShotOffset.0` |  |
| `0xDF8` | 4 | `i32/4096` | `Spells[2].ShotOffset.1` |  |
| `0xDFC` | 4 | `i32/4096` | `Spells[2].ShotOffset.2` |  |
| `0xE00` | 1 | `u8` | `Spells[2].X0c` |  НЕИЗВЕСТНО |
| `0xE01` | 1 | `u8` | `Spells[2].PlayAnimation` |  |
| `0xE02` | 1 | `u8` | `Spells[2].X0e` |  НЕИЗВЕСТНО |
| `0xE03` | 1 | `u8` | `Spells[2].X0f` |  НЕИЗВЕСТНО |
| `0xE04` | 4 | `i32/4096` | `Spells[2].ShotDelay` |  |
| `0xE08` | 1 | `u8` | `Spells[2].X14` |  НЕИЗВЕСТНО |
| `0xE09` | 1 | `u8` | `Spells[2].X15` |  НЕИЗВЕСТНО |
| `0xE0A` | 1 | `u8` | `Spells[2].CreatureSpellId` |  |
| `0xE0B` | 1 | `u8` | `Spells[2].LevelAvailable` |  |
| `0xE0C` | 1 | `enum8` | `resistances[0].AttackType` | Creature.AttackType |
| `0xE0D` | 1 | `u8` | `resistances[0].Value` |  |
| `0xE0E` | 1 | `enum8` | `resistances[1].AttackType` | Creature.AttackType |
| `0xE0F` | 1 | `u8` | `resistances[1].Value` |  |
| `0xE10` | 1 | `enum8` | `resistances[2].AttackType` | Creature.AttackType |
| `0xE11` | 1 | `u8` | `resistances[2].Value` |  |
| `0xE12` | 1 | `enum8` | `resistances[3].AttackType` | Creature.AttackType |
| `0xE13` | 1 | `u8` | `resistances[3].Value` |  |
| `0xE14` | 36 | `JobPreference[3]` | `HappyJobs` |  |
| `0xE38` | 24 | `JobPreference[2]` | `UnhappyJobs` |  |
| `0xE50` | 36 | `JobPreference[3]` | `AngryJobs` |  |
| `0xE74` | 4 | `enum32` | `hateJobs[0].hateJobs` | Creature.JobType |
| `0xE78` | 4 | `enum32` | `hateJobs[1].hateJobs` | Creature.JobType |
| `0xE7C` | 4 | `enum32` | `alternatives[0].JobType` | Creature.JobType |
| `0xE80` | 2 | `u16` | `alternatives[0].MoodChange` |  |
| `0xE82` | 2 | `u16` | `alternatives[0].ManaChange` |  |
| `0xE84` | 4 | `enum32` | `alternatives[1].JobType` | Creature.JobType |
| `0xE88` | 2 | `u16` | `alternatives[1].MoodChange` |  |
| `0xE8A` | 2 | `u16` | `alternatives[1].ManaChange` |  |
| `0xE8C` | 4 | `enum32` | `alternatives[2].JobType` | Creature.JobType |
| `0xE90` | 2 | `u16` | `alternatives[2].MoodChange` |  |
| `0xE92` | 2 | `u16` | `alternatives[2].ManaChange` |  |
| `0xE94` | 4 | `i32/4096` | `AnimationOffsets.PORTAL_ENTRANCE.0` |  |
| `0xE98` | 4 | `i32/4096` | `AnimationOffsets.PORTAL_ENTRANCE.1` |  |
| `0xE9C` | 4 | `i32/4096` | `AnimationOffsets.PORTAL_ENTRANCE.2` |  |
| `0xEA0` | 4 | `i32` | `Unkea0` |  НЕИЗВЕСТНО |
| `0xEA4` | 4 | `i32/4096` | `Height` |  |
| `0xEA8` | 4 | `i32/4096` | `Unkea8` |  НЕИЗВЕСТНО |
| `0xEAC` | 4 | `u32` | `Unk3ab` |  НЕИЗВЕСТНО |
| `0xEB0` | 4 | `i32/4096` | `EyeHeight` |  |
| `0xEB4` | 4 | `i32/4096` | `Speed` |  |
| `0xEB8` | 4 | `i32/4096` | `RunSpeed` |  |
| `0xEBC` | 4 | `i32/4096` | `HungerRate` |  |
| `0xEC0` | 4 | `u32` | `TimeAwake` |  |
| `0xEC4` | 4 | `u32` | `TimeSleep` |  |
| `0xEC8` | 4 | `i32/4096` | `DistanceCanSee` |  |
| `0xECC` | 4 | `i32/4096` | `DistanceCanHear` |  |
| `0xED0` | 4 | `i32/4096` | `StunDuration` |  |
| `0xED4` | 4 | `i32/4096` | `GuardDuration` |  |
| `0xED8` | 4 | `i32/4096` | `IdleDuration` |  |
| `0xEDC` | 4 | `i32/4096` | `SlapFearlessDuration` |  |
| `0xEE0` | 4 | `i32` | `Unkee0` |  НЕИЗВЕСТНО |
| `0xEE4` | 4 | `i32` | `Unkee4` |  НЕИЗВЕСТНО |
| `0xEE8` | 2 | `i16` | `PossessionManaCost` |  |
| `0xEEA` | 2 | `i16` | `OwnLandHealthIncrease` |  |
| `0xEEC` | 4 | `i32/4096` | `MeleeRange` |  |
| `0xEF0` | 4 | `u32` | `Unkef0` |  НЕИЗВЕСТНО |
| `0xEF4` | 4 | `i32/4096` | `TortureTimeToConvert` |  |
| `0xEF8` | 4 | `i32/4096` | `MeleeRecharge` |  |
| `0xEFC` | 4 | `flags32` | `Flags` | Creature.CreatureFlag |
| `0xF00` | 2 | `u16` | `ExpForNextLevel` |  |
| `0xF02` | 1 | `enum8` | `JobClass` | Creature.JobClass |
| `0xF03` | 1 | `enum8` | `FightStyle` | Creature.FightStyle |
| `0xF04` | 2 | `u16` | `ExpPerSecond` |  |
| `0xF06` | 2 | `u16` | `ExpPerSecondTraining` |  |
| `0xF08` | 2 | `u16` | `ResearchPerSecond` |  |
| `0xF0A` | 2 | `u16` | `ManufacturePerSecond` |  |
| `0xF0C` | 2 | `u16` | `Hp` |  |
| `0xF0E` | 2 | `u16` | `HpFromChicken` |  |
| `0xF10` | 2 | `u16` | `Fear` |  |
| `0xF12` | 2 | `u16` | `Threat` |  |
| `0xF14` | 2 | `u16` | `MeleeDamage` |  |
| `0xF16` | 2 | `u16` | `SlapDamage` |  |
| `0xF18` | 2 | `u16` | `ManaGenPrayer` |  |
| `0xF1A` | 2 | `u16` | `Unk3cb` |  НЕИЗВЕСТНО |
| `0xF1C` | 2 | `u16` | `Pay` |  |
| `0xF1E` | 2 | `u16` | `MaxGoldHeld` |  |
| `0xF20` | 2 | `i16/4096` | `Unk3cc` |  НЕИЗВЕСТНО |
| `0xF22` | 2 | `u16` | `DecomposeValue` |  |
| `0xF24` | 2 | `u16` | `NameStringId` |  |
| `0xF26` | 2 | `u16` | `TooltipStringId` |  |
| `0xF28` | 2 | `i16` | `AngerNoLair` |  |
| `0xF2A` | 2 | `i16` | `AngerNoFood` |  |
| `0xF2C` | 2 | `i16` | `AngerNoPay` |  |
| `0xF2E` | 2 | `i16` | `AngerNoWork` |  |
| `0xF30` | 2 | `i16` | `AngerSlap` |  |
| `0xF32` | 2 | `i16` | `AngerInHand` |  |
| `0xF34` | 2 | `i16` | `InitialGoldHeld` |  |
| `0xF36` | 2 | `u16` | `EntranceEffectId` |  |
| `0xF38` | 2 | `u16` | `GeneralDescriptionStringId` |  |
| `0xF3A` | 2 | `u16` | `StrengthStringId` |  |
| `0xF3C` | 2 | `u16` | `WeaknessStringId` |  |
| `0xF3E` | 2 | `u16` | `SlapEffectId` |  |
| `0xF40` | 2 | `u16` | `DeathEffectId` |  |
| `0xF42` | 1 | `enum8` | `Melee1Swipe` | Creature.Swipe |
| `0xF43` | 1 | `enum8` | `Melee2Swipe` | Creature.Swipe |
| `0xF44` | 1 | `enum8` | `Melee3Swipe` | Creature.Swipe |
| `0xF45` | 1 | `enum8` | `SpellSwipe` | Creature.Swipe |
| `0xF46` | 1 | `enum8` | `FirstPersonSpecialAbility1` | Creature.SpecialAbility |
| `0xF47` | 1 | `enum8` | `FirstPersonSpecialAbility2` | Creature.SpecialAbility |
| `0xF48` | 1 | `u8` | `unkf48[0].unkf48` |  НЕИЗВЕСТНО |
| `0xF49` | 1 | `u8` | `unkf48[1].unkf48` |  НЕИЗВЕСТНО |
| `0xF4A` | 1 | `u8` | `unkf48[2].unkf48` |  НЕИЗВЕСТНО |
| `0xF4B` | 1 | `u8` | `CreatureId` |  |
| `0xF4C` | 1 | `u8` | `unk3ea[0].unk3ea` |  НЕИЗВЕСТНО |
| `0xF4D` | 1 | `u8` | `unk3ea[1].unk3ea` |  НЕИЗВЕСТНО |
| `0xF4E` | 1 | `u8` | `HungerFill` |  |
| `0xF4F` | 1 | `u8` | `UnhappyThreshold` |  |
| `0xF50` | 1 | `enum8` | `MeleeAttackType` | Creature.AttackType |
| `0xF51` | 1 | `u8` | `Unk3eb2` |  НЕИЗВЕСТНО |
| `0xF52` | 1 | `u8` | `LairObjectId` |  |
| `0xF53` | 1 | `u8` | `Unk3f1` |  НЕИЗВЕСТНО |
| `0xF54` | 1 | `enum8` | `DeathFallDirection` | Creature.DeathFallDirection |
| `0xF55` | 1 | `u8` | `Unk3f2` |  НЕИЗВЕСТНО |
| `0xF56` | 32 | `string` | `SoundCategory` |  |
| `0xF76` | 1 | `enum8` | `Material` | Material |
| `0xF77` | 84 | `ArtResource` | `FirstPersonFilterResource` |  |
| `0xFCB` | 2 | `u16` | `Unkfcb` |  НЕИЗВЕСТНО |
| `0xFCD` | 4 | `i32/4096` | `Unk4` |  НЕИЗВЕСТНО |
| `0xFD1` | 84 | `ArtResource` | `Animation.DRUNKED_IDLE` |  НЕИЗВЕСТНО |
| `0x1025` | 1 | `enum8` | `Special1Swipe` | Creature.Swipe |
| `0x1026` | 1 | `enum8` | `Special2Swipe` | Creature.Swipe |
| `0x1027` | 84 | `ArtResource` | `FirstPersonMeleeResource` |  |
| `0x107B` | 4 | `u32` | `Unk6` |  НЕИЗВЕСТНО |
| `0x107F` | 2 | `i16` | `TortureHpChange` |  |
| `0x1081` | 2 | `i16` | `TortureMoodChange` |  |
| `0x1083` | 84 | `ArtResource` | `Animation.SWIPE` |  |
| `0x10D7` | 84 | `ArtResource` | `Animation.IDLE_3` |  |
| `0x112B` | 84 | `ArtResource` | `Animation.IDLE_4` |  |
| `0x117F` | 84 | `ArtResource` | `Animation.IDLE_3_1` |  |
| `0x11D3` | 84 | `ArtResource` | `Animation.IDLE_4_1` |  |
| `0x1227` | 84 | `ArtResource` | `Animation.DIG` |  |
| `0x127B` | 4 | `i32/4096` | `AnimationOffsets[0].AnimationOffsets.0` |  |
| `0x127F` | 4 | `i32/4096` | `AnimationOffsets[0].AnimationOffsets.1` |  |
| `0x1283` | 4 | `i32/4096` | `AnimationOffsets[0].AnimationOffsets.2` |  |
| `0x1287` | 4 | `i32/4096` | `AnimationOffsets[1].AnimationOffsets.0` |  |
| `0x128B` | 4 | `i32/4096` | `AnimationOffsets[1].AnimationOffsets.1` |  |
| `0x128F` | 4 | `i32/4096` | `AnimationOffsets[1].AnimationOffsets.2` |  |
| `0x1293` | 4 | `i32/4096` | `AnimationOffsets[2].AnimationOffsets.0` |  |
| `0x1297` | 4 | `i32/4096` | `AnimationOffsets[2].AnimationOffsets.1` |  |
| `0x129B` | 4 | `i32/4096` | `AnimationOffsets[2].AnimationOffsets.2` |  |
| `0x129F` | 4 | `i32/4096` | `AnimationOffsets[3].AnimationOffsets.0` |  |
| `0x12A3` | 4 | `i32/4096` | `AnimationOffsets[3].AnimationOffsets.1` |  |
| `0x12A7` | 4 | `i32/4096` | `AnimationOffsets[3].AnimationOffsets.2` |  |
| `0x12AB` | 4 | `i32/4096` | `AnimationOffsets[4].AnimationOffsets.0` |  |
| `0x12AF` | 4 | `i32/4096` | `AnimationOffsets[4].AnimationOffsets.1` |  |
| `0x12B3` | 4 | `i32/4096` | `AnimationOffsets[4].AnimationOffsets.2` |  |
| `0x12B7` | 4 | `i32/4096` | `AnimationOffsets[5].AnimationOffsets.0` |  |
| `0x12BB` | 4 | `i32/4096` | `AnimationOffsets[5].AnimationOffsets.1` |  |
| `0x12BF` | 4 | `i32/4096` | `AnimationOffsets[5].AnimationOffsets.2` |  |
| `0x12C3` | 4 | `i32/4096` | `AnimationOffsets[6].AnimationOffsets.0` |  |
| `0x12C7` | 4 | `i32/4096` | `AnimationOffsets[6].AnimationOffsets.1` |  |
| `0x12CB` | 4 | `i32/4096` | `AnimationOffsets[6].AnimationOffsets.2` |  |
| `0x12CF` | 84 | `ArtResource` | `Animation.BACK_OFF` |  |
| `0x1323` | 2 | `u16` | `x1323s[0].X00` |  НЕИЗВЕСТНО |
| `0x1325` | 2 | `u16` | `x1323s[0].X02` |  НЕИЗВЕСТНО |
| `0x1327` | 2 | `u16` | `x1323s[1].X00` |  НЕИЗВЕСТНО |
| `0x1329` | 2 | `u16` | `x1323s[1].X02` |  НЕИЗВЕСТНО |
| `0x132B` | 2 | `u16` | `x1323s[2].X00` |  НЕИЗВЕСТНО |
| `0x132D` | 2 | `u16` | `x1323s[2].X02` |  НЕИЗВЕСТНО |
| `0x132F` | 2 | `u16` | `x1323s[3].X00` |  НЕИЗВЕСТНО |
| `0x1331` | 2 | `u16` | `x1323s[3].X02` |  НЕИЗВЕСТНО |
| `0x1333` | 2 | `u16` | `x1323s[4].X00` |  НЕИЗВЕСТНО |
| `0x1335` | 2 | `u16` | `x1323s[4].X02` |  НЕИЗВЕСТНО |
| `0x1337` | 2 | `u16` | `x1323s[5].X00` |  НЕИЗВЕСТНО |
| `0x1339` | 2 | `u16` | `x1323s[5].X02` |  НЕИЗВЕСТНО |
| `0x133B` | 2 | `u16` | `x1323s[6].X00` |  НЕИЗВЕСТНО |
| `0x133D` | 2 | `u16` | `x1323s[6].X02` |  НЕИЗВЕСТНО |
| `0x133F` | 2 | `u16` | `x1323s[7].X00` |  НЕИЗВЕСТНО |
| `0x1341` | 2 | `u16` | `x1323s[7].X02` |  НЕИЗВЕСТНО |
| `0x1343` | 2 | `u16` | `x1323s[8].X00` |  НЕИЗВЕСТНО |
| `0x1345` | 2 | `u16` | `x1323s[8].X02` |  НЕИЗВЕСТНО |
| `0x1347` | 2 | `u16` | `x1323s[9].X00` |  НЕИЗВЕСТНО |
| `0x1349` | 2 | `u16` | `x1323s[9].X02` |  НЕИЗВЕСТНО |
| `0x134B` | 2 | `u16` | `x1323s[10].X00` |  НЕИЗВЕСТНО |
| `0x134D` | 2 | `u16` | `x1323s[10].X02` |  НЕИЗВЕСТНО |
| `0x134F` | 2 | `u16` | `x1323s[11].X00` |  НЕИЗВЕСТНО |
| `0x1351` | 2 | `u16` | `x1323s[11].X02` |  НЕИЗВЕСТНО |
| `0x1353` | 2 | `u16` | `x1323s[12].X00` |  НЕИЗВЕСТНО |
| `0x1355` | 2 | `u16` | `x1323s[12].X02` |  НЕИЗВЕСТНО |
| `0x1357` | 2 | `u16` | `x1323s[13].X00` |  НЕИЗВЕСТНО |
| `0x1359` | 2 | `u16` | `x1323s[13].X02` |  НЕИЗВЕСТНО |
| `0x135B` | 2 | `u16` | `x1323s[14].X00` |  НЕИЗВЕСТНО |
| `0x135D` | 2 | `u16` | `x1323s[14].X02` |  НЕИЗВЕСТНО |
| `0x135F` | 2 | `u16` | `x1323s[15].X00` |  НЕИЗВЕСТНО |
| `0x1361` | 2 | `u16` | `x1323s[15].X02` |  НЕИЗВЕСТНО |
| `0x1363` | 2 | `u16` | `x1323s[16].X00` |  НЕИЗВЕСТНО |
| `0x1365` | 2 | `u16` | `x1323s[16].X02` |  НЕИЗВЕСТНО |
| `0x1367` | 2 | `u16` | `x1323s[17].X00` |  НЕИЗВЕСТНО |
| `0x1369` | 2 | `u16` | `x1323s[17].X02` |  НЕИЗВЕСТНО |
| `0x136B` | 2 | `u16` | `x1323s[18].X00` |  НЕИЗВЕСТНО |
| `0x136D` | 2 | `u16` | `x1323s[18].X02` |  НЕИЗВЕСТНО |
| `0x136F` | 2 | `u16` | `x1323s[19].X00` |  НЕИЗВЕСТНО |
| `0x1371` | 2 | `u16` | `x1323s[19].X02` |  НЕИЗВЕСТНО |
| `0x1373` | 2 | `u16` | `x1323s[20].X00` |  НЕИЗВЕСТНО |
| `0x1375` | 2 | `u16` | `x1323s[20].X02` |  НЕИЗВЕСТНО |
| `0x1377` | 2 | `u16` | `x1323s[21].X00` |  НЕИЗВЕСТНО |
| `0x1379` | 2 | `u16` | `x1323s[21].X02` |  НЕИЗВЕСТНО |
| `0x137B` | 2 | `u16` | `x1323s[22].X00` |  НЕИЗВЕСТНО |
| `0x137D` | 2 | `u16` | `x1323s[22].X02` |  НЕИЗВЕСТНО |
| `0x137F` | 2 | `u16` | `x1323s[23].X00` |  НЕИЗВЕСТНО |
| `0x1381` | 2 | `u16` | `x1323s[23].X02` |  НЕИЗВЕСТНО |
| `0x1383` | 2 | `u16` | `x1323s[24].X00` |  НЕИЗВЕСТНО |
| `0x1385` | 2 | `u16` | `x1323s[24].X02` |  НЕИЗВЕСТНО |
| `0x1387` | 2 | `u16` | `x1323s[25].X00` |  НЕИЗВЕСТНО |
| `0x1389` | 2 | `u16` | `x1323s[25].X02` |  НЕИЗВЕСТНО |
| `0x138B` | 2 | `u16` | `x1323s[26].X00` |  НЕИЗВЕСТНО |
| `0x138D` | 2 | `u16` | `x1323s[26].X02` |  НЕИЗВЕСТНО |
| `0x138F` | 2 | `u16` | `x1323s[27].X00` |  НЕИЗВЕСТНО |
| `0x1391` | 2 | `u16` | `x1323s[27].X02` |  НЕИЗВЕСТНО |
| `0x1393` | 2 | `u16` | `x1323s[28].X00` |  НЕИЗВЕСТНО |
| `0x1395` | 2 | `u16` | `x1323s[28].X02` |  НЕИЗВЕСТНО |
| `0x1397` | 2 | `u16` | `x1323s[29].X00` |  НЕИЗВЕСТНО |
| `0x1399` | 2 | `u16` | `x1323s[29].X02` |  НЕИЗВЕСТНО |
| `0x139B` | 2 | `u16` | `x1323s[30].X00` |  НЕИЗВЕСТНО |
| `0x139D` | 2 | `u16` | `x1323s[30].X02` |  НЕИЗВЕСТНО |
| `0x139F` | 2 | `u16` | `x1323s[31].X00` |  НЕИЗВЕСТНО |
| `0x13A1` | 2 | `u16` | `x1323s[31].X02` |  НЕИЗВЕСТНО |
| `0x13A3` | 2 | `u16` | `x1323s[32].X00` |  НЕИЗВЕСТНО |
| `0x13A5` | 2 | `u16` | `x1323s[32].X02` |  НЕИЗВЕСТНО |
| `0x13A7` | 2 | `u16` | `x1323s[33].X00` |  НЕИЗВЕСТНО |
| `0x13A9` | 2 | `u16` | `x1323s[33].X02` |  НЕИЗВЕСТНО |
| `0x13AB` | 2 | `u16` | `x1323s[34].X00` |  НЕИЗВЕСТНО |
| `0x13AD` | 2 | `u16` | `x1323s[34].X02` |  НЕИЗВЕСТНО |
| `0x13AF` | 2 | `u16` | `x1323s[35].X00` |  НЕИЗВЕСТНО |
| `0x13B1` | 2 | `u16` | `x1323s[35].X02` |  НЕИЗВЕСТНО |
| `0x13B3` | 2 | `u16` | `x1323s[36].X00` |  НЕИЗВЕСТНО |
| `0x13B5` | 2 | `u16` | `x1323s[36].X02` |  НЕИЗВЕСТНО |
| `0x13B7` | 2 | `u16` | `x1323s[37].X00` |  НЕИЗВЕСТНО |
| `0x13B9` | 2 | `u16` | `x1323s[37].X02` |  НЕИЗВЕСТНО |
| `0x13BB` | 2 | `u16` | `x1323s[38].X00` |  НЕИЗВЕСТНО |
| `0x13BD` | 2 | `u16` | `x1323s[38].X02` |  НЕИЗВЕСТНО |
| `0x13BF` | 2 | `u16` | `x1323s[39].X00` |  НЕИЗВЕСТНО |
| `0x13C1` | 2 | `u16` | `x1323s[39].X02` |  НЕИЗВЕСТНО |
| `0x13C3` | 2 | `u16` | `x1323s[40].X00` |  НЕИЗВЕСТНО |
| `0x13C5` | 2 | `u16` | `x1323s[40].X02` |  НЕИЗВЕСТНО |
| `0x13C7` | 2 | `u16` | `x1323s[41].X00` |  НЕИЗВЕСТНО |
| `0x13C9` | 2 | `u16` | `x1323s[41].X02` |  НЕИЗВЕСТНО |
| `0x13CB` | 2 | `u16` | `x1323s[42].X00` |  НЕИЗВЕСТНО |
| `0x13CD` | 2 | `u16` | `x1323s[42].X02` |  НЕИЗВЕСТНО |
| `0x13CF` | 2 | `u16` | `x1323s[43].X00` |  НЕИЗВЕСТНО |
| `0x13D1` | 2 | `u16` | `x1323s[43].X02` |  НЕИЗВЕСТНО |
| `0x13D3` | 2 | `u16` | `x1323s[44].X00` |  НЕИЗВЕСТНО |
| `0x13D5` | 2 | `u16` | `x1323s[44].X02` |  НЕИЗВЕСТНО |
| `0x13D7` | 2 | `u16` | `x1323s[45].X00` |  НЕИЗВЕСТНО |
| `0x13D9` | 2 | `u16` | `x1323s[45].X02` |  НЕИЗВЕСТНО |
| `0x13DB` | 2 | `u16` | `x1323s[46].X00` |  НЕИЗВЕСТНО |
| `0x13DD` | 2 | `u16` | `x1323s[46].X02` |  НЕИЗВЕСТНО |
| `0x13DF` | 2 | `u16` | `x1323s[47].X00` |  НЕИЗВЕСТНО |
| `0x13E1` | 2 | `u16` | `x1323s[47].X02` |  НЕИЗВЕСТНО |
| `0x13E3` | 84 | `ArtResource` | `Animation.STAND_STILL` |  |
| `0x1437` | 84 | `ArtResource` | `Animation.STEALTH_WALK` |  |
| `0x148B` | 84 | `ArtResource` | `Animation.DEATH_POSE` |  |
| `0x14DF` | 2 | `u16` | `UniqueNameTextId` |  |
| `0x14E1` | 4 | `u32` | `x14e1[0].x14e1` |  НЕИЗВЕСТНО |
| `0x14E5` | 4 | `u32` | `x14e1[1].x14e1` |  НЕИЗВЕСТНО |
| `0x14E9` | 4 | `u32` | `FirstPersonSpecialAbility1Count` |  |
| `0x14ED` | 4 | `u32` | `FirstPersonSpecialAbility2Count` |  |
| `0x14F1` | 84 | `ArtResource` | `UniqueResource` |  |
| `0x1545` | 4 | `flags32` | `Flags3` | Creature.CreatureFlag3 |
| `0x1549` | 1 | `u8` | `Extension.unknownExtraBytes[0].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x154A` | 1 | `u8` | `Extension.unknownExtraBytes[1].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x154B` | 1 | `u8` | `Extension.unknownExtraBytes[2].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x154C` | 1 | `u8` | `Extension.unknownExtraBytes[3].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x154D` | 1 | `u8` | `Extension.unknownExtraBytes[4].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x154E` | 1 | `u8` | `Extension.unknownExtraBytes[5].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x154F` | 1 | `u8` | `Extension.unknownExtraBytes[6].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1550` | 1 | `u8` | `Extension.unknownExtraBytes[7].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1551` | 1 | `u8` | `Extension.unknownExtraBytes[8].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1552` | 1 | `u8` | `Extension.unknownExtraBytes[9].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1553` | 1 | `u8` | `Extension.unknownExtraBytes[10].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1554` | 1 | `u8` | `Extension.unknownExtraBytes[11].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1555` | 1 | `u8` | `Extension.unknownExtraBytes[12].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1556` | 1 | `u8` | `Extension.unknownExtraBytes[13].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1557` | 1 | `u8` | `Extension.unknownExtraBytes[14].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1558` | 1 | `u8` | `Extension.unknownExtraBytes[15].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1559` | 1 | `u8` | `Extension.unknownExtraBytes[16].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x155A` | 1 | `u8` | `Extension.unknownExtraBytes[17].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x155B` | 1 | `u8` | `Extension.unknownExtraBytes[18].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x155C` | 1 | `u8` | `Extension.unknownExtraBytes[19].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x155D` | 1 | `u8` | `Extension.unknownExtraBytes[20].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x155E` | 1 | `u8` | `Extension.unknownExtraBytes[21].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x155F` | 1 | `u8` | `Extension.unknownExtraBytes[22].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1560` | 1 | `u8` | `Extension.unknownExtraBytes[23].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1561` | 1 | `u8` | `Extension.unknownExtraBytes[24].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1562` | 1 | `u8` | `Extension.unknownExtraBytes[25].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1563` | 1 | `u8` | `Extension.unknownExtraBytes[26].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1564` | 1 | `u8` | `Extension.unknownExtraBytes[27].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1565` | 1 | `u8` | `Extension.unknownExtraBytes[28].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1566` | 1 | `u8` | `Extension.unknownExtraBytes[29].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1567` | 1 | `u8` | `Extension.unknownExtraBytes[30].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1568` | 1 | `u8` | `Extension.unknownExtraBytes[31].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1569` | 1 | `u8` | `Extension.unknownExtraBytes[32].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x156A` | 1 | `u8` | `Extension.unknownExtraBytes[33].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x156B` | 1 | `u8` | `Extension.unknownExtraBytes[34].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x156C` | 1 | `u8` | `Extension.unknownExtraBytes[35].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x156D` | 1 | `u8` | `Extension.unknownExtraBytes[36].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x156E` | 1 | `u8` | `Extension.unknownExtraBytes[37].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x156F` | 1 | `u8` | `Extension.unknownExtraBytes[38].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1570` | 1 | `u8` | `Extension.unknownExtraBytes[39].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1571` | 1 | `u8` | `Extension.unknownExtraBytes[40].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1572` | 1 | `u8` | `Extension.unknownExtraBytes[41].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1573` | 1 | `u8` | `Extension.unknownExtraBytes[42].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1574` | 1 | `u8` | `Extension.unknownExtraBytes[43].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1575` | 1 | `u8` | `Extension.unknownExtraBytes[44].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1576` | 1 | `u8` | `Extension.unknownExtraBytes[45].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1577` | 1 | `u8` | `Extension.unknownExtraBytes[46].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1578` | 1 | `u8` | `Extension.unknownExtraBytes[47].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1579` | 1 | `u8` | `Extension.unknownExtraBytes[48].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x157A` | 1 | `u8` | `Extension.unknownExtraBytes[49].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x157B` | 1 | `u8` | `Extension.unknownExtraBytes[50].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x157C` | 1 | `u8` | `Extension.unknownExtraBytes[51].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x157D` | 1 | `u8` | `Extension.unknownExtraBytes[52].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x157E` | 1 | `u8` | `Extension.unknownExtraBytes[53].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x157F` | 1 | `u8` | `Extension.unknownExtraBytes[54].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1580` | 1 | `u8` | `Extension.unknownExtraBytes[55].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1581` | 1 | `u8` | `Extension.unknownExtraBytes[56].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1582` | 1 | `u8` | `Extension.unknownExtraBytes[57].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1583` | 1 | `u8` | `Extension.unknownExtraBytes[58].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1584` | 1 | `u8` | `Extension.unknownExtraBytes[59].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1585` | 1 | `u8` | `Extension.unknownExtraBytes[60].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1586` | 1 | `u8` | `Extension.unknownExtraBytes[61].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1587` | 1 | `u8` | `Extension.unknownExtraBytes[62].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1588` | 1 | `u8` | `Extension.unknownExtraBytes[63].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1589` | 1 | `u8` | `Extension.unknownExtraBytes[64].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x158A` | 1 | `u8` | `Extension.unknownExtraBytes[65].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x158B` | 1 | `u8` | `Extension.unknownExtraBytes[66].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x158C` | 1 | `u8` | `Extension.unknownExtraBytes[67].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x158D` | 1 | `u8` | `Extension.unknownExtraBytes[68].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x158E` | 1 | `u8` | `Extension.unknownExtraBytes[69].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x158F` | 1 | `u8` | `Extension.unknownExtraBytes[70].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1590` | 1 | `u8` | `Extension.unknownExtraBytes[71].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1591` | 1 | `u8` | `Extension.unknownExtraBytes[72].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1592` | 1 | `u8` | `Extension.unknownExtraBytes[73].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1593` | 1 | `u8` | `Extension.unknownExtraBytes[74].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1594` | 1 | `u8` | `Extension.unknownExtraBytes[75].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1595` | 1 | `u8` | `Extension.unknownExtraBytes[76].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1596` | 1 | `u8` | `Extension.unknownExtraBytes[77].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1597` | 1 | `u8` | `Extension.unknownExtraBytes[78].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1598` | 1 | `u8` | `Extension.unknownExtraBytes[79].unknownExtraBytes` |  НЕИЗВЕСТНО |
| `0x1599` | 4 | `flags32` | `Extension.Flags2` | Creature.CreatureFlag2 |
| `0x159D` | 2 | `u16` | `Extension.Unknown` |  НЕИЗВЕСТНО |
| `0x159F` | 2 | `i16/4096` | `Extension.Unknown_1` |  НЕИЗВЕСТНО |

## Objects — 894 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `MeshResource` |  |
| `0x74` | 84 | `ArtResource` | `GuiIconResource` |  |
| `0xC8` | 84 | `ArtResource` | `InHandIconResource` |  |
| `0x11C` | 84 | `ArtResource` | `InHandMeshResource` |  |
| `0x170` | 84 | `ArtResource` | `kUnknownResource` |  НЕИЗВЕСТНО |
| `0x1C4` | 84 | `ArtResource` | `AdditionalResources[0].resource` |  |
| `0x218` | 84 | `ArtResource` | `AdditionalResources[1].resource` |  |
| `0x26C` | 84 | `ArtResource` | `AdditionalResources[2].resource` |  |
| `0x2C0` | 84 | `ArtResource` | `AdditionalResources[3].resource` |  |
| `0x314` | 24 | `Light` | `Light` |  |
| `0x32C` | 4 | `i32/4096` | `Width` |  |
| `0x330` | 4 | `i32/4096` | `Height` |  |
| `0x334` | 4 | `i32/4096` | `Mass` |  |
| `0x338` | 4 | `i32/4096` | `Speed` |  |
| `0x33C` | 4 | `i32/65536` | `AirFriction` |  |
| `0x340` | 1 | `enum8` | `Material` | Material |
| `0x341` | 1 | `u8` | `unknown3[0].unknown3` |  НЕИЗВЕСТНО |
| `0x342` | 1 | `u8` | `unknown3[1].unknown3` |  НЕИЗВЕСТНО |
| `0x343` | 1 | `u8` | `unknown3[2].unknown3` |  НЕИЗВЕСТНО |
| `0x344` | 4 | `flags32` | `Flags` | GameObject.ObjectFlag |
| `0x348` | 2 | `u16` | `Hp` |  |
| `0x34A` | 2 | `u16` | `MaxAngle` |  |
| `0x34C` | 2 | `u16` | `X34c` |  НЕИЗВЕСТНО |
| `0x34E` | 2 | `u16` | `ManaValue` |  |
| `0x350` | 2 | `u16` | `TooltipStringId` |  |
| `0x352` | 2 | `u16` | `NameStringId` |  |
| `0x354` | 2 | `u16` | `SlapEffectId` |  |
| `0x356` | 2 | `u16` | `DeathEffectId` |  |
| `0x358` | 2 | `u16` | `MiscEffectId` |  |
| `0x35A` | 1 | `u8` | `ObjectId` |  |
| `0x35B` | 1 | `enum8` | `StartState` | GameObject.State |
| `0x35C` | 1 | `u8` | `RoomCapacity` |  |
| `0x35D` | 1 | `u8` | `PickUpPriority` |  |
| `0x35E` | 32 | `string` | `SoundCategory` |  |

## EffectElements — 182 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `ArtResource` |  |
| `0x74` | 4 | `i32/4096` | `Mass` |  |
| `0x78` | 4 | `i32/65536` | `AirFriction` |  |
| `0x7C` | 4 | `i32/65536` | `Elasticity` |  |
| `0x80` | 4 | `i32/4096` | `MinSpeedXy` |  |
| `0x84` | 4 | `i32/4096` | `MaxSpeedXy` |  |
| `0x88` | 4 | `i32/4096` | `MinSpeedYz` |  |
| `0x8C` | 4 | `i32/4096` | `MaxSpeedYz` |  |
| `0x90` | 4 | `i32/4096` | `MinScale` |  |
| `0x94` | 4 | `i32/4096` | `MaxScale` |  |
| `0x98` | 4 | `i32/4096` | `ScaleRatio` |  |
| `0x9C` | 4 | `flags32` | `Flags` | EffectElement.EffectElementFlag |
| `0xA0` | 2 | `u16` | `EffectElementId` |  |
| `0xA2` | 2 | `u16` | `MinHp` |  |
| `0xA4` | 2 | `u16` | `MaxHp` |  |
| `0xA6` | 2 | `u16` | `DeathElementId` |  |
| `0xA8` | 2 | `u16` | `HitSolidElementId` |  |
| `0xAA` | 2 | `u16` | `HitLavaElementId` |  |
| `0xAC` | 2 | `u16` | `HitWaterElementId` |  |
| `0xAE` | 3 | `RGB8` | `Color` |  |
| `0xB1` | 1 | `u8` | `RandomColorIndex` |  |
| `0xB2` | 1 | `u8` | `TableColorIndex` |  |
| `0xB3` | 1 | `u8` | `FadePercentage` |  |
| `0xB4` | 2 | `u16` | `NextEffectId` |  |

## Effects — 246 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `ArtResource` |  |
| `0x74` | 24 | `Light` | `Light` |  |
| `0x8C` | 4 | `i32/4096` | `Mass` |  |
| `0x90` | 4 | `i32/65536` | `AirFriction` |  |
| `0x94` | 4 | `i32/65536` | `Elasticity` |  |
| `0x98` | 4 | `i32/4096` | `Radius` |  |
| `0x9C` | 4 | `i32/4096` | `MinSpeedXy` |  |
| `0xA0` | 4 | `i32/4096` | `MaxSpeedXy` |  |
| `0xA4` | 4 | `i32/4096` | `MinSpeedYz` |  |
| `0xA8` | 4 | `i32/4096` | `MaxSpeedYz` |  |
| `0xAC` | 4 | `i32/4096` | `MinScale` |  |
| `0xB0` | 4 | `i32/4096` | `MaxScale` |  |
| `0xB4` | 4 | `flags32` | `Flags` | Effect.EffectFlag |
| `0xB8` | 2 | `u16` | `EffectId` |  |
| `0xBA` | 2 | `u16` | `MinHp` |  |
| `0xBC` | 2 | `u16` | `MaxHp` |  |
| `0xBE` | 2 | `u16` | `FadeDuration` |  |
| `0xC0` | 2 | `u16` | `NextEffectId` |  |
| `0xC2` | 2 | `u16` | `DeathEffectId` |  |
| `0xC4` | 2 | `u16` | `HitSolidEffectId` |  |
| `0xC6` | 2 | `u16` | `HitLavaEffectId` |  |
| `0xC8` | 2 | `u16` | `HitWaterEffectId` |  |
| `0xCA` | 2 | `u16` | `GenerateIds[0].id` |  |
| `0xCC` | 2 | `u16` | `GenerateIds[1].id` |  |
| `0xCE` | 2 | `u16` | `GenerateIds[2].id` |  |
| `0xD0` | 2 | `u16` | `GenerateIds[3].id` |  |
| `0xD2` | 2 | `u16` | `GenerateIds[4].id` |  |
| `0xD4` | 2 | `u16` | `GenerateIds[5].id` |  |
| `0xD6` | 2 | `u16` | `GenerateIds[6].id` |  |
| `0xD8` | 2 | `u16` | `GenerateIds[7].id` |  |
| `0xDA` | 2 | `u16` | `OuterOriginRange` |  |
| `0xDC` | 2 | `u16` | `LowerHeightLimit` |  |
| `0xDE` | 2 | `u16` | `UpperHeightLimit` |  |
| `0xE0` | 2 | `u16` | `OrientationRange` |  |
| `0xE2` | 2 | `u16` | `SpriteSpinRateRange` |  |
| `0xE4` | 2 | `u16` | `WhirlpoolRate` |  |
| `0xE6` | 2 | `u16` | `DirectionalSpread` |  |
| `0xE8` | 2 | `u16` | `CircularPathRate` |  |
| `0xEA` | 2 | `u16` | `InnerOriginRange` |  |
| `0xEC` | 2 | `u16` | `GenerateRandomness` |  |
| `0xEE` | 2 | `u16` | `Misc2` |  |
| `0xF0` | 2 | `u16` | `Misc3` |  |
| `0xF2` | 1 | `enum8` | `GenerationType` | Effect.GenerationType |
| `0xF3` | 1 | `u8` | `ElementsPerTurn` |  |
| `0xF4` | 2 | `u16` | `Unknown3` |  НЕИЗВЕСТНО |

## Shots — 239 байт

| Смещение | Байт | Тип | Поле | Перечисление / статус |
|---|---:|---|---|---|
| `0x0` | 32 | `string` | `Name` |  |
| `0x20` | 84 | `ArtResource` | `MeshResource` |  |
| `0x74` | 24 | `Light` | `Light` |  |
| `0x8C` | 4 | `i32/65536` | `AirFriction` |  |
| `0x90` | 4 | `i32/4096` | `Mass` |  |
| `0x94` | 4 | `i32/4096` | `Speed` |  |
| `0x98` | 4 | `u32` | `Data1` |  |
| `0x9C` | 4 | `u32` | `Data2` |  |
| `0xA0` | 4 | `flags32` | `ShotProcessFlags` | Shot.ShotProcessFlag |
| `0xA4` | 4 | `i32/4096` | `Radius` |  |
| `0xA8` | 4 | `flags32` | `Flags` | Shot.ShotFlag |
| `0xAC` | 2 | `u16` | `GeneralEffectId` |  |
| `0xAE` | 2 | `u16` | `CreationEffectId` |  |
| `0xB0` | 2 | `u16` | `DeathEffectId` |  |
| `0xB2` | 2 | `u16` | `TimedEffectId` |  |
| `0xB4` | 2 | `u16` | `HitSolidEffectId` |  |
| `0xB6` | 2 | `u16` | `HitLavaEffectId` |  |
| `0xB8` | 2 | `u16` | `HitWaterEffect` |  |
| `0xBA` | 2 | `u16` | `HitThingEffectId` |  |
| `0xBC` | 2 | `u16` | `Health` |  |
| `0xBE` | 1 | `u8` | `ShotId` |  |
| `0xBF` | 1 | `u8` | `DeathShotId` |  |
| `0xC0` | 1 | `u8` | `TimedDelay` |  |
| `0xC1` | 1 | `u8` | `HitSolidShotId` |  |
| `0xC2` | 1 | `u8` | `HitLavaShotId` |  |
| `0xC3` | 1 | `u8` | `HitWaterShotId` |  |
| `0xC4` | 1 | `u8` | `HitThingShotId` |  |
| `0xC5` | 1 | `enum8` | `DamageType` | Shot.DamageType |
| `0xC6` | 1 | `enum8` | `CollideType` | Shot.CollideType |
| `0xC7` | 1 | `enum8` | `ProcessType` | Shot.ProcessType |
| `0xC8` | 1 | `enum8` | `AttackCategory` | Shot.AttackCategory |
| `0xC9` | 32 | `string` | `SoundCategory` |  |
| `0xE9` | 2 | `u16` | `Threat` |  |
| `0xEB` | 4 | `i32/4096` | `BurnDuration` |  |
