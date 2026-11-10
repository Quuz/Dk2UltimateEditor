# Параметры узлов Triggers

Контейнер подтверждён загрузчиком DKII-DX.exe 0x571290. Имена восьми байт параметров ниже получены из закреплённого OpenKeeper KwdFile.java; индивидуальные игровые эффекты остальных действий здесь не проверены; семь AP-условий уточнены по DX в этапе56, SET_CREATURE_MOODS — в этапе57. Padding сохраняется даже при ненулевых байтах. Неизвестные типы остаются raw.

Заголовок 60 байт: +20 число условий D5, +24 число действий D6; узел — tag u32, payloadBytes u32, payload 16 байт. В payload: parameters[8], Id u16, NextId u16, ChildId u16, Type u8, RepeatTimes u8. D по +56 часто равно только 16 × число условий. Физический размер секции включает все узлы и их восьмибайтовые оболочки.

## TriggerGeneric

| Type | Имя | Поля первых 8 байт payload |
|---:|---|---|
| 1 | FLAG | +00 comparison enum8; +01 targetId u8; +02 flag u8; +03 flagId u8; +04 value u32 |
| 2 | TIMER | +00 comparison enum8; +01 targetId u8; +02 flag u8; +03 timerId u8; +04 value u32 |
| 3 | CREATURE_CREATED | +00 reserved_0 bytes; +04 value u32 |
| 4 | CREATURE_KILLED | +00 reserved_0 bytes |
| 5 | CREATURE_SLAPPED | +00 reserved_0 bytes |
| 6 | CREATURE_ATTACKED | +00 reserved_0 bytes |
| 7 | CREATURE_IMPRISONED | +00 reserved_0 bytes |
| 8 | CREATURE_TORTURED | +00 reserved_0 bytes |
| 9 | CREATURE_CONVERTED | +00 reserved_0 bytes |
| 10 | CREATURE_CLAIMED | +00 reserved_0 bytes |
| 11 | CREATURE_ANGRY | +00 reserved_0 bytes |
| 12 | CREATURE_AFRAID | +00 reserved_0 bytes |
| 13 | CREATURE_STEALS | +00 reserved_0 bytes |
| 14 | CREATURE_LEAVES | +00 reserved_0 bytes |
| 15 | CREATURE_STUNNED | +00 reserved_0 bytes |
| 16 | CREATURE_DYING | +00 reserved_0 bytes |
| 19 | PLAYER_CREATURES | +00 comparison enum8; +01 creatureId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 20 | PLAYER_HAPPY_CREATURES | +00 comparison enum8; +01 creatureId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 21 | PLAYER_ANGRY_CREATURES | +00 comparison enum8; +01 creatureId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 22 | PLAYER_CREATURES_KILLED | +00 comparison enum8; +01 targetId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 23 | PLAYER_KILLS_CREATURES | +00 comparison enum8; +01 targetId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 24 | PLAYER_ROOM_SLABS | +00 comparison enum8; +01 roomId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 25 | PLAYER_ROOMS | +00 comparison enum8; +01 roomId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 26 | PLAYER_ROOM_SIZE | +00 comparison enum8; +01 roomId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 27 | PLAYER_DOORS | +00 comparison enum8; +01 targetId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 28 | PLAYER_TRAPS | +00 comparison enum8; +01 targetId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 29 | PLAYER_KEEPER_SPELL | +00 comparison enum8; +01 targetId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 30 | PLAYER_GOLD | +00 comparison enum8; +01 reserved_1 bytes; +02 flag u8; +03 playerId u8; +04 value u32 |
| 31 | PLAYER_GOLD_MINED | +00 comparison enum8; +01 reserved_1 bytes; +02 flag u8; +03 playerId u8; +04 value u32 |
| 32 | PLAYER_MANA | +00 comparison enum8; +01 reserved_1 bytes; +02 flag u8; +03 playerId u8; +04 value u32 |
| 33 | PLAYER_DESTROYS | +00 comparison enum8; +01 targetId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 34 | LEVEL_TIME | +00 comparison enum8; +01 reserved_1 bytes; +04 value u32 |
| 35 | LEVEL_CREATURES | +00 comparison enum8; +01 reserved_1 bytes; +04 value u32 |
| 36 | CREATURE_HEALTH | +00 comparison enum8; +01 reserved_1 bytes; +04 value u32 |
| 37 | CREATURE_GOLD_HELD | +00 comparison enum8; +01 reserved_1 bytes; +04 value u32 |
| 39 | AP_CONGREGATE_IN | +00 comparison enum8; +01 playerId u8; +02 targetId u8; +03 targetType u8; +04 value u32 |
| 40 | AP_CLAIM_PART_OF | +00 comparison enum8; +01 playerId u8; +02 terrainId u8; +03 reserved_3 bytes; +04 value u32 |
| 41 | AP_CLAIM_ALL_OF | +00 comparison enum8; +01 playerId u8; +02 terrainId u8; +03 reserved_3 bytes; +04 value u32 |
| 42 | AP_SLAB_TYPES | +00 comparison enum8; +01 playerId u8; +02 terrainId u8; +03 reserved_3 bytes; +04 value u32 |
| 43 | PARTY_CREATED | +00 reserved_0 bytes |
| 44 | PARTY_MEMBERS_KILLED | +00 comparison enum8; +01 unknown u8; +02 reserved_2 bytes; +04 value u32 |
| 45 | PARTY_MEMBERS_CAPTURED | +00 comparison enum8; +01 reserved_1 bytes; +04 value u32 |
| 46 | LEVEL_PAY_DAY | +00 reserved_0 bytes |
| 47 | PLAYER_KILLED | +00 playerId u8; +01 reserved_1 bytes; +04 value u32 |
| 49 | CREATURE_EXPERIENCE_LEVEL | +00 comparison enum8; +01 reserved_1 bytes; +04 value u32 |
| 52 | PLAYER_CREATURES_AT_LEVEL | +00 comparison enum8; +01 targetId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 53 | GUI_BUTTON_PRESSED | +00 targetType u8; +01 targetId u8; +02 reserved_2 bytes; +04 value u32 |
| 54 | CREATURE_HUNGER_SATED | +00 reserved_0 bytes |
| 55 | CREATURE_PICKS_UP_PORTAL_GEM | +00 reserved_0 bytes |
| 56 | PLAYER_DUNGEON_BREACHED | +00 playerId u8; +01 reserved_1 bytes |
| 57 | PLAYER_ENEMY_BREACHED | +00 playerId u8; +01 reserved_1 bytes |
| 58 | PLAYER_CREATURE_PICKED_UP | +00 creatureId u8; +01 reserved_1 bytes |
| 59 | PLAYER_CREATURE_DROPPED | +00 creatureId u8; +01 roomId u8; +02 reserved_2 bytes |
| 60 | PLAYER_CREATURE_SLAPPED | +00 creatureId u8; +01 reserved_1 bytes |
| 61 | PLAYER_CREATURE_SACKED | +00 creatureId u8; +01 reserved_1 bytes |
| 62 | AP_TAG_PART_OF | +00 comparison enum8; +01 playerId u8; +02 reserved_2 bytes; +04 value u32 |
| 63 | CREATURE_SACKED | +00 reserved_0 bytes |
| 64 | PARTY_MEMBERS_INCAPACITATED | +00 comparison enum8; +01 unknown u8; +02 reserved_2 bytes; +04 value u32 |
| 65 | CREATURE_PICKED_UP | +00 reserved_0 bytes |
| 67 | LEVEL_PLAYED | +00 comparison enum8; +01 reserved_1 bytes; +04 value u32 |
| 68 | PLAYER_ROOM_FURNITURE | +00 comparison enum8; +01 roomId u8; +02 flag u8; +03 playerId u8; +04 value u32 |
| 69 | AP_TAG_ALL_OF | +00 comparison enum8; +01 playerId u8; +02 reserved_2 bytes; +04 value u32 |
| 70 | AP_POSESSED_CREATURE_ENTERS | +00 comparison enum8; +01 playerId u8; +02 targetId u8; +03 targetType u8; +04 value u32 |
| 71 | PLAYER_SLAPS | +00 comparison enum8; +01 reserved_1 bytes; +02 flag u8; +03 playerId u8; +04 value u32 |
| 72 | GUI_TRANSITION_ENDS | +00 reserved_0 bytes |
| 73 | PLAYER_CREATURES_GROUPED | +00 comparison enum8; +01 reserved_1 bytes; +02 flag u8; +03 playerId u8; +04 value u32 |
| 74 | PLAYER_CREATURES_DYING | +00 comparison enum8; +01 reserved_1 bytes; +02 flag u8; +03 playerId u8; +04 value u32 |

## TriggerAction

| Type | Имя | Поля первых 8 байт payload |
|---:|---|---|
| 1 | CREATE_CREATURE | +00 creatureId u8; +01 playerId u8; +02 level u8; +03 flag u8; +04 posX u16; +06 posY u16 |
| 2 | CREATE_PORTAL_GEM | +00 objectId u8; +01 playerId u8; +02 reserved_2 bytes; +04 posX u16; +06 posY u16 |
| 3 | SEND_TO_AP | +00 reserved_0 bytes; +04 actionPointId u8; +05 reserved_5 bytes |
| 4 | DISPLAY_OBJECTIVE | +00 objectiveId u32; +04 actionPointId u8; +05 reserved_5 bytes |
| 5 | INFORMATION | +00 informationId u32; +04 actionPointId u8; +05 reserved_5 bytes |
| 6 | MAKE | +00 playerId u8; +01 type u8; +02 targetId u8; +03 available u8; +04 reserved_4 bytes |
| 7 | FLAG | +00 flagId u8; +01 flag u8; +02 reserved_2 bytes; +04 value u32 |
| 8 | INITIALIZE_TIMER | +00 timerId u8; +01 reserved_1 bytes; +04 value u32 |
| 9 | FLASH_BUTTON | +00 type u8; +01 targetId u8; +02 available u8; +03 reserved_3 bytes; +04 value u32 |
| 10 | WIN_GAME | +00 reserved_0 bytes |
| 11 | LOSE_GAME | +00 reserved_0 bytes |
| 12 | WIN_SUBOBJECTIVE | +00 reserved_0 bytes |
| 13 | LOSE_SUBOBJECTIVE | +00 reserved_0 bytes |
| 14 | CREATE_HERO_PARTY | +00 partyId u8; +01 type u8; +02 reserved_2 bytes; +04 actionPointId u8; +05 reserved_5 bytes |
| 15 | SET_OBJECTIVE | +00 playerId u8; +01 type u8; +02 reserved_2 bytes; +04 actionPointId u32 |
| 16 | FLASH_ACTION_POINT | +00 actionPointId u8; +01 available u8; +02 reserved_2 bytes; +04 value u32 |
| 17 | REVEAL_ACTION_POINT | +00 actionPointId u8; +01 available u8; +02 reserved_2 bytes |
| 18 | SET_ALLIANCE | +00 playerOneId u8; +01 playerTwoId u8; +02 available u8; +03 reserved_3 bytes |
| 20 | ATTACH_PORTAL_GEM | +00 reserved_0 bytes |
| 21 | FORCE_FIRST_PERSON | +00 reserved_0 bytes |
| 22 | ALTER_TERRAIN_TYPE | +00 terrainId u8; +01 playerId u8; +02 reserved_2 bytes; +04 posX u16; +06 posY u16 |
| 23 | SET_TIME_LIMIT | +00 timerId u8; +01 reserved_1 bytes; +04 value u32 |
| 24 | PLAY_SPEECH | +00 speechId u32; +04 text u8; +05 introduction u8; +06 pathId u16 |
| 25 | DISPLAY_TEXT_STRING | +00 textId u32; +04 reserved_4 bytes |
| 26 | ZOOM_TO_ACTION_POINT | +00 targetId u8; +01 reserved_1 bytes |
| 27 | ROTATE_AROUND_ACTION_POINT | +00 actionPointId u8; +01 available u8; +02 angle u16; +04 time u32 |
| 28 | GENERATE_CREATURE | +00 creatureId u8; +01 level u8; +02 reserved_2 bytes |
| 29 | MAKE_HUNGRY | +00 reserved_0 bytes |
| 30 | SHOW_HEALTH_FLOWER | +00 value u32; +04 reserved_4 bytes |
| 31 | FOLLOW_CAMERA_PATH | +00 pathId u8; +01 actionPointId u8; +02 available u8; +03 reserved_3 bytes |
| 32 | COLLAPSE_HERO_GATE | +00 reserved_0 bytes; +04 posX u16; +06 posY u16 |
| 33 | ALTER_SPEED | +00 available u8; +01 reserved_1 bytes |
| 34 | SET_MUSIC_LEVEL | +00 value u32; +04 reserved_4 bytes |
| 35 | REMOVE_FROM_MAP | +00 reserved_0 bytes |
| 36 | SET_FIGHT_FLAG | +00 available u8; +01 reserved_1 bytes |
| 37 | SET_PORTAL_STATUS | +00 available u8; +01 reserved_1 bytes |
| 38 | SET_WIDESCREEN_MODE | +00 available u8; +01 reserved_1 bytes |
| 42 | MAKE_OBJECTIVE | +00 targetId u8; +01 reserved_1 bytes |
| 43 | ZOOM_TO | +00 reserved_0 bytes |
| 44 | SET_CREATURE_MOODS | +00 available u8; +01 reserved_1 bytes |
| 45 | SET_SYSTEM_MESSAGES | +00 available u8; +01 reserved_1 bytes |
| 46 | DISPLAY_SLAB_OWNER | +00 available u8; +01 reserved_1 bytes |
| 47 | DISPLAY_NEXT_ROOM_TYPE | +00 targetId u8; +01 reserved_1 bytes |
| 48 | TOGGLE_EFFECT_GENERATOR | +00 generatorId u8; +01 available u8; +02 reserved_2 bytes |
| 49 | CHANGE_ROOM_OWNER | +00 reserved_0 bytes; +01 playerId u8; +02 reserved_2 bytes; +04 posX u16; +06 posY u16 |
| 50 | SET_SLAPS_LIMIT | +00 reserved_0 bytes; +04 value u32 |
| 51 | SET_TIMER_SPEECH | +00 available u8; +01 reserved_1 bytes |

## Native-уточнение этап56

DX4B0D70 и61 native проверка уточняют семь AP условий. У AP_CLAIM_PART_OF/ALL_OF байт2 — живой TerrainId, а не reserved. AP_CLAIM_ALL_OF принудительно сравнивает count16 с полной32-битной площадью на равенство; AP_TAG_ALL_OF сравнивает >= с числом Terrain17B&08, игнорируя comparison/value. Прежние поля сохранены в baseline_fields; исходные байты не нормализуются. Подробности — AP_TRIGGER_CONDITIONS_RU.md и action-point-condition-semantics.json.

## Native-уточнение этап57

D6 type44 SET_CREATURE_MOODS: только availableBYTE==1 включает bit01 world72A1. Двенадцать полных native action→anger-add цепочек подтвердили gate и сохранение остальных битов. Подробности — CREATURE_ANGER_RU.md.
