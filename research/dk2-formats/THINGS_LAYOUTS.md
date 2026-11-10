# Things: размещённые объекты карты

Общий заголовок секции — 56 байт. Далее идут записи: `u32 type`, `u32 payloadBytes`, затем полезная часть. Следующая запись начинается через `8+payloadBytes` байт. Таблицы ниже НЕ включают первые 8 байт.
Схемы — интерпретации OpenKeeper. Типы и длины подтверждены оригинальным диспетчером/писателем и наблюдаемыми файлами; смысл не всех полей установлен.

Уточнение26 октября, этап29: `PLACED_STRUCTURES_RU.md` / `placed-structure-semantics.json` устанавливает native override: THING_TRAP payload+0xC используется как0/неноль activation-selector, а THING_DOOR payload+0x10 — сочетание масок0x01/0x02, не взаимоисключающий enum. Baseline имена/типы таблиц ниже оставлены для сопоставления; сохранять все исходные байты.

## 0xC2 THING_OBJECT — 24 байт полезной части

Native override этап30: `PLACED_OBJECTS_RU.md` / `placed-object-semantics.json`. DWORD+0C перегружен: byte содержимого книги и полный селектор старого ObjectId7; игра и Editor Pro могут выбирать разные ID. Money+10 прибавляется с32-битным wrap и обнулением отрицательной суммы. DWORD+08 сохранять целиком, несмотря на Z=0 в game placement и native writer. TriggerId скопирован, но не передаётся создателю в проверенном game callback. Baseline таблица ниже оставлена для сопоставления.

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `PosX` |
| `0x4` | 4 | `i32` | `PosY` |
| `0x8` | 1 | `u8` | `unknown1[0].unknown1` |
| `0x9` | 1 | `u8` | `unknown1[1].unknown1` |
| `0xA` | 1 | `u8` | `unknown1[2].unknown1` |
| `0xB` | 1 | `u8` | `unknown1[3].unknown1` |
| `0xC` | 4 | `i32` | `KeeperSpellId` |
| `0x10` | 4 | `i32` | `MoneyAmount` |
| `0x14` | 2 | `u16` | `TriggerId` |
| `0x16` | 1 | `u8` | `ObjectId` |
| `0x17` | 1 | `u8` | `PlayerId` |

## 0xC3 THING_TRAP — 16 байт полезной части

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `PosX` |
| `0x4` | 4 | `i32` | `PosY` |
| `0x8` | 4 | `i32` | `Unknown1` |
| `0xC` | 1 | `u8` | `NumberOfShots` |
| `0xD` | 1 | `u8` | `TrapId` |
| `0xE` | 1 | `u8` | `PlayerId` |
| `0xF` | 1 | `u8` | `Unknown2` |

## 0xC4 THING_DOOR — 20 байт полезной части

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `PosX` |
| `0x4` | 4 | `i32` | `PosY` |
| `0x8` | 4 | `i32` | `Unknown1` |
| `0xC` | 2 | `u16` | `TriggerId` |
| `0xE` | 1 | `u8` | `DoorId` |
| `0xF` | 1 | `u8` | `PlayerId` |
| `0x10` | 1 | `enum8` | `Flag` |
| `0x11` | 1 | `u8` | `unknown2[0].unknown2` |
| `0x12` | 1 | `u8` | `unknown2[1].unknown2` |
| `0x13` | 1 | `u8` | `unknown2[2].unknown2` |

## 0xC5 THING_ACTION_POINT — 56 байт полезной части

Уточнение этап34: `ACTION_POINT_EDITOR_RU.md` / `action-point-editor-semantics.json` подтверждает native-привязки Hero Lair01, Reveal10, Tool Box20, Ignore Solid40. Панель меняет только71 и сохраняет unknown04/08. Wait GUI0..32767, auto-ID command1..80 не ограничивают физические WORD/byte. Панель исключает текущий ID из Next Waypoint:170 штатных самоссылок корпуса при её применении проецируются в0. Lossless-редактор должен сохранять исходные связи; это не правило ремонта файла.

Native override этап33: `ACTION_POINTS_RU.md` / `action-point-semantics.json`. При raw EndX DWORD08=0 координаты трактуются как center/radius, иначе corners. Game runtime хранит low16 углов, wait=low16(rawWait*TPS), flags=raw&3D. Editor хранит flagsBYTE, writer OR04 всемAP при любой waypoint-связи и не назначает name32 в проверенном цикле. Next deadline истекает при unsigned now>deadline. Baseline имена ниже оставлены для сопоставления; raw данные сохранять полностью.

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `StartX` |
| `0x4` | 4 | `i32` | `StartY` |
| `0x8` | 4 | `i32` | `EndX` |
| `0xC` | 4 | `i32` | `EndY` |
| `0x10` | 2 | `u16` | `WaitDelay` |
| `0x12` | 2 | `flags16` | `Flags` |
| `0x14` | 2 | `u16` | `TriggerId` |
| `0x16` | 1 | `u8` | `Id` |
| `0x17` | 1 | `u8` | `NextWaypointId` |
| `0x18` | 32 | `string` | `Name` |

## 0xC6 THING_NEUTRAL_CREATURE — 24 байт полезной части

Native override этап31: `PLACED_CREATURES_RU.md` / `placed-creature-semantics.json`. GoldHeld WORD и InitialHealth DWORD — проценты от level-adjusted характеристик, не абсолютные количества. Neutral Unknown1+17 имеет native-подпись WanderRadius, Good+1D тоже WanderRadius, Good+1E — PartyId с игровым потребителем группировки. Для WanderRadius алгоритм/единицы пока неизвестны. Baseline таблицы C6/C7/C8 ниже сохранены; raw поля не сужать по byte-модели оригинального редактора. Нормализация одиночного C7 не распространяется автоматически на участников C9.

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `PosX` |
| `0x4` | 4 | `i32` | `PosY` |
| `0x8` | 4 | `i32` | `PosZ` |
| `0xC` | 2 | `u16` | `GoldHeld` |
| `0xE` | 1 | `u8` | `Level` |
| `0xF` | 1 | `flags8` | `Flags` |
| `0x10` | 4 | `i32` | `InitialHealth` |
| `0x14` | 2 | `u16` | `TriggerId` |
| `0x16` | 1 | `u8` | `CreatureId` |
| `0x17` | 1 | `u8` | `Unknown1` |

## 0xC7 THING_GOOD_CREATURE — 32 байт полезной части

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `PosX` |
| `0x4` | 4 | `i32` | `PosY` |
| `0x8` | 4 | `i32` | `PosZ` |
| `0xC` | 2 | `u16` | `GoldHeld` |
| `0xE` | 1 | `u8` | `Level` |
| `0xF` | 1 | `flags8` | `Flags` |
| `0x10` | 4 | `i32` | `ObjectiveTargetActionPointId` |
| `0x14` | 4 | `i32` | `InitialHealth` |
| `0x18` | 2 | `u16` | `TriggerId` |
| `0x1A` | 1 | `u8` | `ObjectiveTargetPlayerId` |
| `0x1B` | 1 | `enum8` | `Objective` |
| `0x1C` | 1 | `u8` | `CreatureId` |
| `0x1D` | 1 | `u8` | `unknown1[0].unknown1` |
| `0x1E` | 1 | `u8` | `unknown1[1].unknown1` |
| `0x1F` | 1 | `flags8` | `Flags2` |

## 0xC8 THING_KEEPER_CREATURE — 28 байт полезной части

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `PosX` |
| `0x4` | 4 | `i32` | `PosY` |
| `0x8` | 4 | `i32` | `PosZ` |
| `0xC` | 2 | `u16` | `GoldHeld` |
| `0xE` | 1 | `u8` | `Level` |
| `0xF` | 1 | `flags8` | `Flags` |
| `0x10` | 4 | `i32` | `InitialHealth` |
| `0x14` | 4 | `i32` | `ObjectiveTargetActionPointId` |
| `0x18` | 2 | `u16` | `TriggerId` |
| `0x1A` | 1 | `u8` | `CreatureId` |
| `0x1B` | 1 | `u8` | `PlayerId` |

## 0xC9 THING_HERO_PARTY — 555 байт полезной части

Native override этап32: `HERO_PARTIES_RU.md` / `hero-party-semantics.json`. Подтверждены старые payload264/268/300 и современный555. Современные X23/X27 не являются установленным счётчиком: parser их не копирует, writer оставляет вместе с именем в scratch-буфере без присваивания. Сохранять raw. 16 слотов с2B определяются по CreatureId0, без отдельных оболочек. Party loader сохраняет/автоматически назначает Leader/Follower и переопределяет member PartyId заголовком; не применять normalizer одиночного C7. Таблица ниже описывает только555-байтовый вариант.

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 32 | `string` | `Name` |
| `0x20` | 2 | `u16` | `TriggerId` |
| `0x22` | 1 | `u8` | `Id` |
| `0x23` | 4 | `i32` | `X23` |
| `0x27` | 4 | `i32` | `X27` |
| `0x2B` | 4 | `i32` | `loop[0].PosX` |
| `0x2F` | 4 | `i32` | `loop[0].PosY` |
| `0x33` | 4 | `i32` | `loop[0].PosZ` |
| `0x37` | 2 | `u16` | `loop[0].GoldHeld` |
| `0x39` | 1 | `u8` | `loop[0].Level` |
| `0x3A` | 1 | `flags8` | `loop[0].Flags` |
| `0x3B` | 4 | `i32` | `loop[0].ObjectiveTargetActionPointId` |
| `0x3F` | 4 | `i32` | `loop[0].InitialHealth` |
| `0x43` | 2 | `u16` | `loop[0].TriggerId` |
| `0x45` | 1 | `u8` | `loop[0].ObjectiveTargetPlayerId` |
| `0x46` | 1 | `enum8` | `loop[0].Objective` |
| `0x47` | 1 | `u8` | `loop[0].CreatureId` |
| `0x48` | 1 | `u8` | `loop[0].unknown1[0].unknown1` |
| `0x49` | 1 | `u8` | `loop[0].unknown1[1].unknown1` |
| `0x4A` | 1 | `flags8` | `loop[0].Flags2` |
| `0x4B` | 4 | `i32` | `loop[1].PosX` |
| `0x4F` | 4 | `i32` | `loop[1].PosY` |
| `0x53` | 4 | `i32` | `loop[1].PosZ` |
| `0x57` | 2 | `u16` | `loop[1].GoldHeld` |
| `0x59` | 1 | `u8` | `loop[1].Level` |
| `0x5A` | 1 | `flags8` | `loop[1].Flags` |
| `0x5B` | 4 | `i32` | `loop[1].ObjectiveTargetActionPointId` |
| `0x5F` | 4 | `i32` | `loop[1].InitialHealth` |
| `0x63` | 2 | `u16` | `loop[1].TriggerId` |
| `0x65` | 1 | `u8` | `loop[1].ObjectiveTargetPlayerId` |
| `0x66` | 1 | `enum8` | `loop[1].Objective` |
| `0x67` | 1 | `u8` | `loop[1].CreatureId` |
| `0x68` | 1 | `u8` | `loop[1].unknown1[0].unknown1` |
| `0x69` | 1 | `u8` | `loop[1].unknown1[1].unknown1` |
| `0x6A` | 1 | `flags8` | `loop[1].Flags2` |
| `0x6B` | 4 | `i32` | `loop[2].PosX` |
| `0x6F` | 4 | `i32` | `loop[2].PosY` |
| `0x73` | 4 | `i32` | `loop[2].PosZ` |
| `0x77` | 2 | `u16` | `loop[2].GoldHeld` |
| `0x79` | 1 | `u8` | `loop[2].Level` |
| `0x7A` | 1 | `flags8` | `loop[2].Flags` |
| `0x7B` | 4 | `i32` | `loop[2].ObjectiveTargetActionPointId` |
| `0x7F` | 4 | `i32` | `loop[2].InitialHealth` |
| `0x83` | 2 | `u16` | `loop[2].TriggerId` |
| `0x85` | 1 | `u8` | `loop[2].ObjectiveTargetPlayerId` |
| `0x86` | 1 | `enum8` | `loop[2].Objective` |
| `0x87` | 1 | `u8` | `loop[2].CreatureId` |
| `0x88` | 1 | `u8` | `loop[2].unknown1[0].unknown1` |
| `0x89` | 1 | `u8` | `loop[2].unknown1[1].unknown1` |
| `0x8A` | 1 | `flags8` | `loop[2].Flags2` |
| `0x8B` | 4 | `i32` | `loop[3].PosX` |
| `0x8F` | 4 | `i32` | `loop[3].PosY` |
| `0x93` | 4 | `i32` | `loop[3].PosZ` |
| `0x97` | 2 | `u16` | `loop[3].GoldHeld` |
| `0x99` | 1 | `u8` | `loop[3].Level` |
| `0x9A` | 1 | `flags8` | `loop[3].Flags` |
| `0x9B` | 4 | `i32` | `loop[3].ObjectiveTargetActionPointId` |
| `0x9F` | 4 | `i32` | `loop[3].InitialHealth` |
| `0xA3` | 2 | `u16` | `loop[3].TriggerId` |
| `0xA5` | 1 | `u8` | `loop[3].ObjectiveTargetPlayerId` |
| `0xA6` | 1 | `enum8` | `loop[3].Objective` |
| `0xA7` | 1 | `u8` | `loop[3].CreatureId` |
| `0xA8` | 1 | `u8` | `loop[3].unknown1[0].unknown1` |
| `0xA9` | 1 | `u8` | `loop[3].unknown1[1].unknown1` |
| `0xAA` | 1 | `flags8` | `loop[3].Flags2` |
| `0xAB` | 4 | `i32` | `loop[4].PosX` |
| `0xAF` | 4 | `i32` | `loop[4].PosY` |
| `0xB3` | 4 | `i32` | `loop[4].PosZ` |
| `0xB7` | 2 | `u16` | `loop[4].GoldHeld` |
| `0xB9` | 1 | `u8` | `loop[4].Level` |
| `0xBA` | 1 | `flags8` | `loop[4].Flags` |
| `0xBB` | 4 | `i32` | `loop[4].ObjectiveTargetActionPointId` |
| `0xBF` | 4 | `i32` | `loop[4].InitialHealth` |
| `0xC3` | 2 | `u16` | `loop[4].TriggerId` |
| `0xC5` | 1 | `u8` | `loop[4].ObjectiveTargetPlayerId` |
| `0xC6` | 1 | `enum8` | `loop[4].Objective` |
| `0xC7` | 1 | `u8` | `loop[4].CreatureId` |
| `0xC8` | 1 | `u8` | `loop[4].unknown1[0].unknown1` |
| `0xC9` | 1 | `u8` | `loop[4].unknown1[1].unknown1` |
| `0xCA` | 1 | `flags8` | `loop[4].Flags2` |
| `0xCB` | 4 | `i32` | `loop[5].PosX` |
| `0xCF` | 4 | `i32` | `loop[5].PosY` |
| `0xD3` | 4 | `i32` | `loop[5].PosZ` |
| `0xD7` | 2 | `u16` | `loop[5].GoldHeld` |
| `0xD9` | 1 | `u8` | `loop[5].Level` |
| `0xDA` | 1 | `flags8` | `loop[5].Flags` |
| `0xDB` | 4 | `i32` | `loop[5].ObjectiveTargetActionPointId` |
| `0xDF` | 4 | `i32` | `loop[5].InitialHealth` |
| `0xE3` | 2 | `u16` | `loop[5].TriggerId` |
| `0xE5` | 1 | `u8` | `loop[5].ObjectiveTargetPlayerId` |
| `0xE6` | 1 | `enum8` | `loop[5].Objective` |
| `0xE7` | 1 | `u8` | `loop[5].CreatureId` |
| `0xE8` | 1 | `u8` | `loop[5].unknown1[0].unknown1` |
| `0xE9` | 1 | `u8` | `loop[5].unknown1[1].unknown1` |
| `0xEA` | 1 | `flags8` | `loop[5].Flags2` |
| `0xEB` | 4 | `i32` | `loop[6].PosX` |
| `0xEF` | 4 | `i32` | `loop[6].PosY` |
| `0xF3` | 4 | `i32` | `loop[6].PosZ` |
| `0xF7` | 2 | `u16` | `loop[6].GoldHeld` |
| `0xF9` | 1 | `u8` | `loop[6].Level` |
| `0xFA` | 1 | `flags8` | `loop[6].Flags` |
| `0xFB` | 4 | `i32` | `loop[6].ObjectiveTargetActionPointId` |
| `0xFF` | 4 | `i32` | `loop[6].InitialHealth` |
| `0x103` | 2 | `u16` | `loop[6].TriggerId` |
| `0x105` | 1 | `u8` | `loop[6].ObjectiveTargetPlayerId` |
| `0x106` | 1 | `enum8` | `loop[6].Objective` |
| `0x107` | 1 | `u8` | `loop[6].CreatureId` |
| `0x108` | 1 | `u8` | `loop[6].unknown1[0].unknown1` |
| `0x109` | 1 | `u8` | `loop[6].unknown1[1].unknown1` |
| `0x10A` | 1 | `flags8` | `loop[6].Flags2` |
| `0x10B` | 4 | `i32` | `loop[7].PosX` |
| `0x10F` | 4 | `i32` | `loop[7].PosY` |
| `0x113` | 4 | `i32` | `loop[7].PosZ` |
| `0x117` | 2 | `u16` | `loop[7].GoldHeld` |
| `0x119` | 1 | `u8` | `loop[7].Level` |
| `0x11A` | 1 | `flags8` | `loop[7].Flags` |
| `0x11B` | 4 | `i32` | `loop[7].ObjectiveTargetActionPointId` |
| `0x11F` | 4 | `i32` | `loop[7].InitialHealth` |
| `0x123` | 2 | `u16` | `loop[7].TriggerId` |
| `0x125` | 1 | `u8` | `loop[7].ObjectiveTargetPlayerId` |
| `0x126` | 1 | `enum8` | `loop[7].Objective` |
| `0x127` | 1 | `u8` | `loop[7].CreatureId` |
| `0x128` | 1 | `u8` | `loop[7].unknown1[0].unknown1` |
| `0x129` | 1 | `u8` | `loop[7].unknown1[1].unknown1` |
| `0x12A` | 1 | `flags8` | `loop[7].Flags2` |
| `0x12B` | 4 | `i32` | `loop[8].PosX` |
| `0x12F` | 4 | `i32` | `loop[8].PosY` |
| `0x133` | 4 | `i32` | `loop[8].PosZ` |
| `0x137` | 2 | `u16` | `loop[8].GoldHeld` |
| `0x139` | 1 | `u8` | `loop[8].Level` |
| `0x13A` | 1 | `flags8` | `loop[8].Flags` |
| `0x13B` | 4 | `i32` | `loop[8].ObjectiveTargetActionPointId` |
| `0x13F` | 4 | `i32` | `loop[8].InitialHealth` |
| `0x143` | 2 | `u16` | `loop[8].TriggerId` |
| `0x145` | 1 | `u8` | `loop[8].ObjectiveTargetPlayerId` |
| `0x146` | 1 | `enum8` | `loop[8].Objective` |
| `0x147` | 1 | `u8` | `loop[8].CreatureId` |
| `0x148` | 1 | `u8` | `loop[8].unknown1[0].unknown1` |
| `0x149` | 1 | `u8` | `loop[8].unknown1[1].unknown1` |
| `0x14A` | 1 | `flags8` | `loop[8].Flags2` |
| `0x14B` | 4 | `i32` | `loop[9].PosX` |
| `0x14F` | 4 | `i32` | `loop[9].PosY` |
| `0x153` | 4 | `i32` | `loop[9].PosZ` |
| `0x157` | 2 | `u16` | `loop[9].GoldHeld` |
| `0x159` | 1 | `u8` | `loop[9].Level` |
| `0x15A` | 1 | `flags8` | `loop[9].Flags` |
| `0x15B` | 4 | `i32` | `loop[9].ObjectiveTargetActionPointId` |
| `0x15F` | 4 | `i32` | `loop[9].InitialHealth` |
| `0x163` | 2 | `u16` | `loop[9].TriggerId` |
| `0x165` | 1 | `u8` | `loop[9].ObjectiveTargetPlayerId` |
| `0x166` | 1 | `enum8` | `loop[9].Objective` |
| `0x167` | 1 | `u8` | `loop[9].CreatureId` |
| `0x168` | 1 | `u8` | `loop[9].unknown1[0].unknown1` |
| `0x169` | 1 | `u8` | `loop[9].unknown1[1].unknown1` |
| `0x16A` | 1 | `flags8` | `loop[9].Flags2` |
| `0x16B` | 4 | `i32` | `loop[10].PosX` |
| `0x16F` | 4 | `i32` | `loop[10].PosY` |
| `0x173` | 4 | `i32` | `loop[10].PosZ` |
| `0x177` | 2 | `u16` | `loop[10].GoldHeld` |
| `0x179` | 1 | `u8` | `loop[10].Level` |
| `0x17A` | 1 | `flags8` | `loop[10].Flags` |
| `0x17B` | 4 | `i32` | `loop[10].ObjectiveTargetActionPointId` |
| `0x17F` | 4 | `i32` | `loop[10].InitialHealth` |
| `0x183` | 2 | `u16` | `loop[10].TriggerId` |
| `0x185` | 1 | `u8` | `loop[10].ObjectiveTargetPlayerId` |
| `0x186` | 1 | `enum8` | `loop[10].Objective` |
| `0x187` | 1 | `u8` | `loop[10].CreatureId` |
| `0x188` | 1 | `u8` | `loop[10].unknown1[0].unknown1` |
| `0x189` | 1 | `u8` | `loop[10].unknown1[1].unknown1` |
| `0x18A` | 1 | `flags8` | `loop[10].Flags2` |
| `0x18B` | 4 | `i32` | `loop[11].PosX` |
| `0x18F` | 4 | `i32` | `loop[11].PosY` |
| `0x193` | 4 | `i32` | `loop[11].PosZ` |
| `0x197` | 2 | `u16` | `loop[11].GoldHeld` |
| `0x199` | 1 | `u8` | `loop[11].Level` |
| `0x19A` | 1 | `flags8` | `loop[11].Flags` |
| `0x19B` | 4 | `i32` | `loop[11].ObjectiveTargetActionPointId` |
| `0x19F` | 4 | `i32` | `loop[11].InitialHealth` |
| `0x1A3` | 2 | `u16` | `loop[11].TriggerId` |
| `0x1A5` | 1 | `u8` | `loop[11].ObjectiveTargetPlayerId` |
| `0x1A6` | 1 | `enum8` | `loop[11].Objective` |
| `0x1A7` | 1 | `u8` | `loop[11].CreatureId` |
| `0x1A8` | 1 | `u8` | `loop[11].unknown1[0].unknown1` |
| `0x1A9` | 1 | `u8` | `loop[11].unknown1[1].unknown1` |
| `0x1AA` | 1 | `flags8` | `loop[11].Flags2` |
| `0x1AB` | 4 | `i32` | `loop[12].PosX` |
| `0x1AF` | 4 | `i32` | `loop[12].PosY` |
| `0x1B3` | 4 | `i32` | `loop[12].PosZ` |
| `0x1B7` | 2 | `u16` | `loop[12].GoldHeld` |
| `0x1B9` | 1 | `u8` | `loop[12].Level` |
| `0x1BA` | 1 | `flags8` | `loop[12].Flags` |
| `0x1BB` | 4 | `i32` | `loop[12].ObjectiveTargetActionPointId` |
| `0x1BF` | 4 | `i32` | `loop[12].InitialHealth` |
| `0x1C3` | 2 | `u16` | `loop[12].TriggerId` |
| `0x1C5` | 1 | `u8` | `loop[12].ObjectiveTargetPlayerId` |
| `0x1C6` | 1 | `enum8` | `loop[12].Objective` |
| `0x1C7` | 1 | `u8` | `loop[12].CreatureId` |
| `0x1C8` | 1 | `u8` | `loop[12].unknown1[0].unknown1` |
| `0x1C9` | 1 | `u8` | `loop[12].unknown1[1].unknown1` |
| `0x1CA` | 1 | `flags8` | `loop[12].Flags2` |
| `0x1CB` | 4 | `i32` | `loop[13].PosX` |
| `0x1CF` | 4 | `i32` | `loop[13].PosY` |
| `0x1D3` | 4 | `i32` | `loop[13].PosZ` |
| `0x1D7` | 2 | `u16` | `loop[13].GoldHeld` |
| `0x1D9` | 1 | `u8` | `loop[13].Level` |
| `0x1DA` | 1 | `flags8` | `loop[13].Flags` |
| `0x1DB` | 4 | `i32` | `loop[13].ObjectiveTargetActionPointId` |
| `0x1DF` | 4 | `i32` | `loop[13].InitialHealth` |
| `0x1E3` | 2 | `u16` | `loop[13].TriggerId` |
| `0x1E5` | 1 | `u8` | `loop[13].ObjectiveTargetPlayerId` |
| `0x1E6` | 1 | `enum8` | `loop[13].Objective` |
| `0x1E7` | 1 | `u8` | `loop[13].CreatureId` |
| `0x1E8` | 1 | `u8` | `loop[13].unknown1[0].unknown1` |
| `0x1E9` | 1 | `u8` | `loop[13].unknown1[1].unknown1` |
| `0x1EA` | 1 | `flags8` | `loop[13].Flags2` |
| `0x1EB` | 4 | `i32` | `loop[14].PosX` |
| `0x1EF` | 4 | `i32` | `loop[14].PosY` |
| `0x1F3` | 4 | `i32` | `loop[14].PosZ` |
| `0x1F7` | 2 | `u16` | `loop[14].GoldHeld` |
| `0x1F9` | 1 | `u8` | `loop[14].Level` |
| `0x1FA` | 1 | `flags8` | `loop[14].Flags` |
| `0x1FB` | 4 | `i32` | `loop[14].ObjectiveTargetActionPointId` |
| `0x1FF` | 4 | `i32` | `loop[14].InitialHealth` |
| `0x203` | 2 | `u16` | `loop[14].TriggerId` |
| `0x205` | 1 | `u8` | `loop[14].ObjectiveTargetPlayerId` |
| `0x206` | 1 | `enum8` | `loop[14].Objective` |
| `0x207` | 1 | `u8` | `loop[14].CreatureId` |
| `0x208` | 1 | `u8` | `loop[14].unknown1[0].unknown1` |
| `0x209` | 1 | `u8` | `loop[14].unknown1[1].unknown1` |
| `0x20A` | 1 | `flags8` | `loop[14].Flags2` |
| `0x20B` | 4 | `i32` | `loop[15].PosX` |
| `0x20F` | 4 | `i32` | `loop[15].PosY` |
| `0x213` | 4 | `i32` | `loop[15].PosZ` |
| `0x217` | 2 | `u16` | `loop[15].GoldHeld` |
| `0x219` | 1 | `u8` | `loop[15].Level` |
| `0x21A` | 1 | `flags8` | `loop[15].Flags` |
| `0x21B` | 4 | `i32` | `loop[15].ObjectiveTargetActionPointId` |
| `0x21F` | 4 | `i32` | `loop[15].InitialHealth` |
| `0x223` | 2 | `u16` | `loop[15].TriggerId` |
| `0x225` | 1 | `u8` | `loop[15].ObjectiveTargetPlayerId` |
| `0x226` | 1 | `enum8` | `loop[15].Objective` |
| `0x227` | 1 | `u8` | `loop[15].CreatureId` |
| `0x228` | 1 | `u8` | `loop[15].unknown1[0].unknown1` |
| `0x229` | 1 | `u8` | `loop[15].unknown1[1].unknown1` |
| `0x22A` | 1 | `flags8` | `loop[15].Flags2` |

## 0xCA THING_DEAD_BODY — 16 байт полезной части

Native override этап38: `DEAD_BODIES_RU.md` / `dead-body-semantics.json`. WORD0C — EditorHealthValue, а BYTE0F — **Level**, не GoldHeld/PlayerId. Игра переносит WORD0C в checksum input, но не использует его в проверенном placement; BYTE0F идёт в runtime12D и расчёт масштаба тела. Editor major!=1 берёт low8(WORD0C) и clamp100; writer выводит Z0 и zero-extended healthBYTE. Исходные поля сохранять raw. Baseline таблица ниже оставлена для сопоставления, новые имена имеют приоритет.

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `PosX` |
| `0x4` | 4 | `i32` | `PosY` |
| `0x8` | 4 | `i32` | `PosZ` |
| `0xC` | 2 | `u16` | `GoldHeld` |
| `0xE` | 1 | `u8` | `CreatureId` |
| `0xF` | 1 | `u8` | `PlayerId` |

## 0xCB THING_EFFECT_GENERATOR — 36 байт полезной части

Native override этап37: `EFFECT_GENERATORS_RU.md` / `effect-generator-semantics.json`. WORD10/12 — Width/Height области появления в клетках; четыре слота WORD14..1B выбираются вместе с пустыми. Frequency задаёт deadline после вызова создания; пустой слот/отклонённая позиция таймер не меняют. DWORD08/0C и хвост1E..23 сохранять raw. Editor Pro пишет Z/flags0, ограничивает extents и не назначает хвост. Baseline имена ниже оставлены для сопоставления; `pad` не означает гарантированные нули.

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `PosX` |
| `0x4` | 4 | `i32` | `PosY` |
| `0x8` | 4 | `i32` | `X08` |
| `0xC` | 4 | `i32` | `X0c` |
| `0x10` | 2 | `u16` | `X10` |
| `0x12` | 2 | `u16` | `X12` |
| `0x14` | 2 | `u16` | `loop[0].effectId` |
| `0x16` | 2 | `u16` | `loop[1].effectId` |
| `0x18` | 2 | `u16` | `loop[2].effectId` |
| `0x1A` | 2 | `u16` | `loop[3].effectId` |
| `0x1C` | 1 | `u8` | `Frequency` |
| `0x1D` | 1 | `u8` | `Id` |
| `0x1E` | 1 | `u8` | `pad[0].pad` |
| `0x1F` | 1 | `u8` | `pad[1].pad` |
| `0x20` | 1 | `u8` | `pad[2].pad` |
| `0x21` | 1 | `u8` | `pad[3].pad` |
| `0x22` | 1 | `u8` | `pad[4].pad` |
| `0x23` | 1 | `u8` | `pad[5].pad` |

## 0xCC THING_ROOM — 20 байт полезной части

Native override этап39: `PLACED_ROOMS_RU.md` / `placed-room-semantics.json`. RoomType12 — TerrainId selector, не Rooms catalogue ID. CC дополняет уже существующую комнату. Game callback применяет12/14/37, PlayerId0 пропускает запись; editor поддерживает12/14/33/37/40 и сначала сравнивает terrain клетки. Правила здоровья/writer отличаются по типу: Portal health0→100 при сохранении, Gate health0 остаётся0. RawBYTE0F у586 записей ненулевой; сохранять opaque, не обнулять. Таблица ниже остаётся baseline для сопоставления.

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32` | `PosX` |
| `0x4` | 4 | `i32` | `PosY` |
| `0x8` | 4 | `i32` | `X08` |
| `0xC` | 2 | `u16` | `X0c` |
| `0xE` | 1 | `enum8` | `Direction` |
| `0xF` | 1 | `u8` | `X0f` |
| `0x10` | 2 | `u16` | `InitialHealth` |
| `0x12` | 1 | `enum8` | `RoomType` |
| `0x13` | 1 | `u8` | `PlayerId` |

## 0xCD THING_CAMERA — 84 байт полезной части

Native override этап40: `CAMERAS_RU.md` / `camera-semantics.json`. Id52 — **BYTE**,53 — отдельный opaque byte, который converters не копируют. Game preset loader принимает1..18, после checksum; storage не означает немедленную активацию. Flags10/20 блокируют относительный ViewDistance/Lens; прямые setters могут их обходить. В обычном registration Editor Pro cameraA8=0, reader пропускает CD, writer не имеет CD-ветви. Сохранять raw независимо; baseline WORD Id в таблице ниже оставлен для аудита и не имеет приоритета над этим уточнением.

| Смещение | Байт | Тип | Поле |
|---|---:|---|---|
| `0x0` | 4 | `i32/4096` | `Position.0` |
| `0x4` | 4 | `i32/4096` | `Position.1` |
| `0x8` | 4 | `i32/4096` | `Position.2` |
| `0xC` | 4 | `i32/4096` | `PositionMinClipExtent.0` |
| `0x10` | 4 | `i32/4096` | `PositionMinClipExtent.1` |
| `0x14` | 4 | `i32/4096` | `PositionMinClipExtent.2` |
| `0x18` | 4 | `i32/4096` | `PositionMaxClipExtent.0` |
| `0x1C` | 4 | `i32/4096` | `PositionMaxClipExtent.1` |
| `0x20` | 4 | `i32/4096` | `PositionMaxClipExtent.2` |
| `0x24` | 4 | `i32/4096` | `ViewDistanceValue` |
| `0x28` | 4 | `i32/4096` | `ViewDistanceMin` |
| `0x2C` | 4 | `i32/4096` | `ViewDistanceMax` |
| `0x30` | 4 | `i32/4096` | `ZoomValue` |
| `0x34` | 4 | `i32/4096` | `ZoomValueMin` |
| `0x38` | 4 | `i32/4096` | `ZoomValueMax` |
| `0x3C` | 4 | `i32/4096` | `LensValue` |
| `0x40` | 4 | `i32/4096` | `LensValueMin` |
| `0x44` | 4 | `i32/4096` | `LensValueMax` |
| `0x48` | 4 | `flags32` | `Flags` |
| `0x4C` | 2 | `u16` | `AngleYaw` |
| `0x4E` | 2 | `u16` | `AngleRoll` |
| `0x50` | 2 | `u16` | `AnglePitch` |
| `0x52` | 2 | `u16` | `Id` |
