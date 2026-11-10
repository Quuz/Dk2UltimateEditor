# Перечисления и флаги

Названия и значения из закреплённой версии OpenKeeper. Неизвестные значения и биты сохранять. Название enum само по себе не доказывает игровой эффект.

## AI.Distance

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `CLOSE` |
| 1 | `0x1` | `RANDOM` |

## AI.CorridorStyle

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `ANY_ANGLE` |
| 1 | `0x1` | `DEGREES_45_AND_90` |
| 2 | `0x2` | `DEGREES_90` |

## AI.RoomExpandPolicy

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `EXPAND_EXISTING` |
| 1 | `0x1` | `CREATE_NEW` |
| 2 | `0x2` | `IGNORE` |

## AI.DoorUsagePolicy

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `BEST_DOOR` |
| 1 | `0x1` | `WORST_DOOR` |
| 2 | `0x2` | `RANDOM` |

## AI.BreachRoomPolicy

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `ENEMY_DUNGEON_HEART` |
| 1 | `0x1` | `RANDOM` |
| 2 | `0x2` | `WEAK_ROOM` |
| 3 | `0x3` | `STRONG_ROOM` |

## AI.DigToPolicy

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `FURTHEST_FROM_HEART` |
| 1 | `0x1` | `RANDOM` |
| 2 | `0x2` | `HEART` |
| 3 | `0x3` | `CLOSEST_TO_ENEMY_HEART` |
| 255 | `0xFF` | `UNKNOWN_255` |

## AI.CreatureDisposalPolicy

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `SACK` |
| 1 | `0x1` | `SACRIFICE` |

## AI.SightOfEvilUsagePolicy

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `RANDOM` |
| 2 | `0x2` | `ENEMY_LAND` |

## AI.CallToArmsUsagePolicy

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `TOWARDS_HEART` |
| 1 | `0x1` | `AT_HEART_WITH_CREATURES` |
| 2 | `0x2` | `AT_HEART` |

## AI.MoveToResearchPolicy

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `VERY_LIKELY` |
| 1 | `0x1` | `PROBABLE` |
| 2 | `0x2` | `UNLIKELY` |

## AI.ImprisonedCreatureFatePolicy

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `TORTURE` |
| 1 | `0x1` | `LEAVE_TO_DIE` |
| 2 | `0x2` | `PLACE_IN_PIT` |
| 3 | `0x3` | `SACRIFICE` |

## ArtResource.ArtResourceFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `UNKNOWN` |
| 2 | `0x2` | `PLAYER_COLOURED` |
| 4 | `0x4` | `ANIMATING_TEXTURE` |
| 8 | `0x8` | `HAS_START_ANIMATION` |
| 16 | `0x10` | `HAS_END_ANIMATION` |
| 32 | `0x20` | `RANDOM_START_FRAME` |
| 64 | `0x40` | `ORIGIN_AT_BOTTOM` |
| 128 | `0x80` | `DOESNT_LOOP` |
| 256 | `0x100` | `FLAT` |
| 512 | `0x200` | `DOESNT_USE_PROGRESSIVE_MESH` |
| 1024 | `0x400` | `UNKNOWN_1` |
| 32768 | `0x8000` | `UNKNOWN_2` |
| 65536 | `0x10000` | `USE_ANIMATING_TEXTURE_FOR_SELECTION` |
| 131072 | `0x20000` | `PRELOAD` |
| 262144 | `0x40000` | `BLOOD` |

## ArtResource.ArtResourceType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `SPRITE` |
| 2 | `0x2` | `ALPHA` |
| 3 | `0x3` | `ADDITIVE_ALPHA` |
| 4 | `0x4` | `TERRAIN_MESH` |
| 5 | `0x5` | `MESH` |
| 6 | `0x6` | `ANIMATING_MESH` |
| 7 | `0x7` | `PROCEDURAL_MESH` |
| 8 | `0x8` | `MESH_COLLECTION` |
| 12 | `0xC` | `UNKNOWN` |

## Creature.CreatureFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `IS_WORKER` |
| 2 | `0x2` | `CAN_BE_PICKED_UP` |
| 4 | `0x4` | `CAN_BE_SLAPPED` |
| 8 | `0x8` | `ALWAYS_FLEE` |
| 16 | `0x10` | `CAN_WALK_ON_WATER` |
| 32 | `0x20` | `CAN_WALK_ON_LAVA` |
| 64 | `0x40` | `CAN_DISARM_TRAPS` |
| 128 | `0x80` | `IS_EVIL` |
| 256 | `0x100` | `IS_GOOD` |
| 512 | `0x200` | `IS_IMMUNE_TO_TURNCOAT` |
| 1024 | `0x400` | `AVAILABLE_VIA_PORTAL` |
| 2048 | `0x800` | `UNKNOWN_3` |
| 4096 | `0x1000` | `UNKNOWN_4` |
| 8192 | `0x2000` | `CAN_FLY` |
| 16384 | `0x4000` | `IS_HORNY` |
| 32768 | `0x8000` | `GENERATE_DEAD_BODY` |
| 65536 | `0x10000` | `CAN_BE_HYPNOTIZED` |
| 131072 | `0x20000` | `IS_IMMUNE_TO_CHICKEN` |
| 262144 | `0x40000` | `IS_FEARLESS` |
| 524288 | `0x80000` | `CAN_BE_ELECTROCUTED` |
| 1048576 | `0x100000` | `NEED_BODY_FOR_FIGHT_IDLE` |
| 2097152 | `0x200000` | `NOT_TRAIN_WHEN_IDLE` |
| 4194304 | `0x400000` | `ONLY_ATTACKABLE_BY_HORNY` |
| 8388608 | `0x800000` | `CAN_BE_RESURECTED` |
| 16777216 | `0x1000000` | `DOESNT_GET_ANGRY_WITH_ENEMIES` |
| 33554432 | `0x2000000` | `FREES_FRIENDS_ON_JAILBREAK` |
| 67108864 | `0x4000000` | `REVEALS_ADJACENT_TRAPS` |
| 134217728 | `0x8000000` | `IS_UNIQUE` |
| 268435456 | `0x10000000` | `CAMERA_ROLLS_WHEN_TURNING` |
| 536870912 | `0x20000000` | `UNKNOWN_5` |
| 1073741824 | `0x40000000` | `MPD_RANDOM_INVADER` |
| 2147483648 | `0x80000000` | `IS_MALE` |

## Creature.CreatureFlag2

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `IS_IMMUNE_TO_LIGHTNING` |
| 2 | `0x2` | `IS_STONE_KNIGHT` |
| 4 | `0x4` | `IS_EMOTIONLESS` |
| 8 | `0x8` | `AVAILABLE_VIA_HERO_PORTAL` |

## Creature.CreatureFlag3

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `UNK_1` |
| 2 | `0x2` | `UNK_2` |
| 4 | `0x4` | `UNK_4` |
| 8 | `0x8` | `UNK_8` |

## Creature.AttackType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `MELEE_BLADE` |
| 2 | `0x2` | `MELEE_BLUNT` |
| 3 | `0x3` | `MELEE_BODY` |
| 4 | `0x4` | `MISTRESS_LIGHTNING` |
| 5 | `0x5` | `GAS_CLOUD` |
| 6 | `0x6` | `GAS_MISSILE` |
| 7 | `0x7` | `DARK_ELF_GUIDED_BOLT` |
| 8 | `0x8` | `SLOW` |
| 9 | `0x9` | `GRENADE` |
| 10 | `0xA` | `DRAIN` |
| 11 | `0xB` | `MISTRESS_HAIL_STORM` |
| 12 | `0xC` | `DARK_ELF_ARROW` |
| 13 | `0xD` | `HASTE_CREATURE` |
| 14 | `0xE` | `WARLOCK_HEAL_CREATURE` |
| 15 | `0xF` | `MELEE_SCYTHE` |
| 16 | `0x10` | `INVULNERABLE` |
| 17 | `0x11` | `THIEF_INVISIBLE` |
| 18 | `0x12` | `KNIVES` |
| 19 | `0x13` | `DISRUPTION` |
| 20 | `0x14` | `RAISE_DEAD` |
| 21 | `0x15` | `WIND` |
| 22 | `0x16` | `SKELETON_ARMY` |
| 23 | `0x17` | `CHICKEN_ARROW` |
| 24 | `0x18` | `ELF_ARCHER_ARROW` |
| 25 | `0x19` | `WIZARD_FIREBALL` |
| 26 | `0x1A` | `SALAMANDER_FIREBALL` |
| 27 | `0x1B` | `WIZARD_FIREBOMB_1` |
| 28 | `0x1C` | `WIZARD_FIREBOMB_2` |
| 29 | `0x1D` | `FAIRY_FREEZE` |
| 30 | `0x1E` | `ELVEN_ARCHER_GUIDED_BOLT` |
| 31 | `0x1F` | `DARK_ANGLE_HAIL_STORM` |
| 32 | `0x20` | `MONK_HEAL_CREATURE` |
| 33 | `0x21` | `ROGUE_INVISIBLE` |
| 34 | `0x22` | `FAIRY_LIGHTNING` |
| 35 | `0x23` | `DARK_ANGLE_FIREBOMB` |
| 36 | `0x24` | `IMP_TELEPORT` |
| 37 | `0x25` | `IMP_HASTE` |
| 38 | `0x26` | `REAPER_FIREBALL` |
| 39 | `0x27` | `SALAMANDER_SPIT` |
| 40 | `0x28` | `MAIDEN_WEB` |
| 41 | `0x29` | `MAIDEN_POISON_SPIT` |

## Creature.JobType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `SLEEP` |
| 2 | `0x2` | `EAT` |
| 3 | `0x3` | `RESEARCH` |
| 4 | `0x4` | `TRAIN` |
| 5 | `0x5` | `MANUFACTURE` |
| 6 | `0x6` | `GUARD` |
| 7 | `0x7` | `TORTURE` |
| 8 | `0x8` | `PRAY` |
| 9 | `0x9` | `DRINK` |
| 10 | `0xA` | `LEAVE` |
| 11 | `0xB` | `DESTROY_ENEMY_ROOMS` |
| 12 | `0xC` | `DESTROY_WALLS` |
| 13 | `0xD` | `STEAL_GOLD` |
| 14 | `0xE` | `STEAL_SPELLS` |
| 15 | `0xF` | `SULK` |
| 16 | `0x10` | `REBEL` |
| 17 | `0x11` | `STEAL_MANUFACTURE_CRATES` |
| 18 | `0x12` | `KILL_CREATURES` |
| 19 | `0x13` | `KILL_PLAYER` |
| 20 | `0x14` | `TUNNELLING` |
| 22 | `0x16` | `WAIT` |
| 23 | `0x17` | `SEND_TO_ACTION_POINT` |
| 24 | `0x18` | `EXPLORE` |
| 25 | `0x19` | `STEAL_ENEMY_GOLD` |
| 26 | `0x1A` | `COMBAT_PIT_SPECTATE` |
| 27 | `0x1B` | `JAIL_BREAK` |
| 28 | `0x1C` | `TOLLING` |

## Creature.JobClass

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `ARTISAN` |
| 1 | `0x1` | `THINKER` |
| 2 | `0x2` | `FIGHTER` |
| 3 | `0x3` | `SCOUT` |

## Creature.FightStyle

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NON_FIGHTER` |
| 1 | `0x1` | `BLITZER` |
| 2 | `0x2` | `BLOCKER` |
| 3 | `0x3` | `FLANKER` |
| 4 | `0x4` | `SUPPORT` |

## Creature.DeathFallDirection

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NORTH` |
| 1 | `0x1` | `NORTH_EAST` |
| 2 | `0x2` | `EAST` |
| 3 | `0x3` | `SOUTH_EAST` |
| 4 | `0x4` | `SOUTH` |
| 5 | `0x5` | `SOUTH_WEST` |
| 6 | `0x6` | `WEST` |
| 7 | `0x7` | `NORTH_WEST` |

## Creature.Swipe

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `BITE` |
| 2 | `0x2` | `CLAW_SLASH` |
| 3 | `0x3` | `CLUB_SMASH` |
| 4 | `0x4` | `DARK_FGBS_SLASH` |
| 5 | `0x5` | `KING_FGBS_SLASH` |
| 6 | `0x6` | `LEFT_PUNCH` |
| 7 | `0x7` | `RIGHT_PUNCH` |
| 8 | `0x8` | `LEFT_SCYTHE_SLASH` |
| 9 | `0x9` | `RIGHT_SCYTHE_SLASH` |
| 10 | `0xA` | `MISTRESS_SLASH` |
| 11 | `0xB` | `SMALL_SLASH` |
| 12 | `0xC` | `STAFF_STRIKE` |
| 13 | `0xD` | `SWORD_SLASH` |
| 14 | `0xE` | `LIGHT_ARROW` |
| 15 | `0xF` | `DARK_ARROW` |
| 16 | `0x10` | `LEFT_BILE_SMASH` |
| 17 | `0x11` | `RIGHT_BILE_SMASH` |
| 18 | `0x12` | `HYPNOTISE` |
| 19 | `0x13` | `LEFT_SMALL_PUNCH` |
| 20 | `0x14` | `RIGHT_SMALL_PUNCH` |
| 21 | `0x15` | `LEFT_KING_FGBS_SLASH` |
| 22 | `0x16` | `RIGHT_KING_FGBS_SLASH` |
| 36 | `0x24` | `UNK_36` |

## Creature.SpecialAbility

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `PICK_UP_WORKERS` |
| 2 | `0x2` | `SNIPER_MODE` |
| 3 | `0x3` | `TURN_TO_BAT` |
| 4 | `0x4` | `FLY` |
| 5 | `0x5` | `HYPNOTISE` |
| 6 | `0x6` | `DISGUISE_N_STEAL` |
| 7 | `0x7` | `PRAY` |
| 8 | `0x8` | `PICK_LOCKS` |
| 9 | `0x9` | `FEAR_ATTACK` |
| 10 | `0xA` | `GHOST_POSESSION` |

## Creature.GammaEffect

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NORMAL` |
| 1 | `0x1` | `VAMPIRE_RED` |
| 2 | `0x2` | `DARK_ELF_PURPLE` |
| 3 | `0x3` | `SKELETON_BLACK_N_WHITE` |
| 4 | `0x4` | `SALAMANDER_INFRARED` |
| 5 | `0x5` | `DARK_ANGER_BRIGHT_BLUE` |
| 6 | `0x6` | `WHITEOUT` |
| 7 | `0x7` | `BLACKOUT` |
| 8 | `0x8` | `REDOUT` |
| 9 | `0x9` | `DEATH_VIEW_HALF_RED` |
| 10 | `0xA` | `MARTINS_TEST_1` |
| 11 | `0xB` | `MARTINS_TEST_2` |
| 12 | `0xC` | `MARTINS_TEST_3` |
| 13 | `0xD` | `MARTINS_TEST_4` |
| 14 | `0xE` | `FREEZE_VIEW_ONLY_BLUE` |
| 15 | `0xF` | `MAIDEN` |
| 16 | `0x10` | `GHOST` |

## CreatureSpell.CreatureSpellFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `IS_ATTACKING` |
| 2 | `0x2` | `IS_DEFENSIVE` |
| 4 | `0x4` | `CREATURE` |
| 8 | `0x8` | `CAST_ONLY_ON_CREATURES` |
| 32 | `0x20` | `IMP_TELEPORT` |
| 64 | `0x40` | `IMP_HASTE` |
| 128 | `0x80` | `CAST_ON_SELF` |
| 256 | `0x100` | `CAST_ON_OTHERS` |

## CreatureSpell.AlternativeShot

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `HASTE` |
| 2 | `0x2` | `HEAL` |
| 3 | `0x3` | `ARMOUR` |
| 4 | `0x4` | `INVULNERABLE` |
| 5 | `0x5` | `INVISIBLE` |

## Door.DoorFlag

| Число | Hex | Имя |
|---:|---|---|
| 32 | `0x20` | `IS_SECRET` |
| 64 | `0x40` | `IS_BARRICADE` |
| 128 | `0x80` | `IS_GOOD` |

## Effect.EffectFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `DIE_WHEN_HIT_SOLID` |
| 2 | `0x2` | `SHRINK` |
| 4 | `0x4` | `EXPAND` |
| 16 | `0x10` | `DIRECTIONAL_FRICTION` |
| 32 | `0x20` | `EXPAND_THEN_SHRINK` |
| 64 | `0x40` | `GENERATE_EFFECTS` |
| 128 | `0x80` | `GENERATE_EFFECT_ELEMENTS` |
| 256 | `0x100` | `PASS_ON_VELOCITY` |
| 512 | `0x200` | `UNIFORM_DISTRIBUTION` |
| 1024 | `0x400` | `RANDOM_DISTRIBUTION` |
| 2048 | `0x800` | `FADE_IN` |
| 4096 | `0x1000` | `FADE_OUT` |
| 8192 | `0x2000` | `FADE_IN_OUT` |
| 16384 | `0x4000` | `USE_GENERATION_SCALE` |
| 32768 | `0x8000` | `ALWAYS_TRIGGER` |

## Effect.GenerationType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `DEFAULT` |
| 2 | `0x2` | `ADJACENT_EXPLOSION` |
| 3 | `0x3` | `STORM` |
| 4 | `0x4` | `CUBE_GEN` |
| 5 | `0x5` | `EXPLOSION_2` |
| 6 | `0x6` | `SPRAY` |
| 7 | `0x7` | `EXPLOSION` |
| 8 | `0x8` | `SPIRAL` |
| 9 | `0x9` | `EVERYTHING` |
| 10 | `0xA` | `CHICKEN_SLAP` |
| 11 | `0xB` | `DIG` |
| 12 | `0xC` | `SMOKE_PUFF` |
| 13 | `0xD` | `FIRE` |
| 14 | `0xE` | `GOLD_COST` |
| 15 | `0xF` | `TEST_0` |
| 16 | `0x10` | `TEST_1` |
| 17 | `0x11` | `TEST_2` |
| 18 | `0x12` | `TEST_3` |

## EffectElement.EffectElementFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `DIE_WHEN_HIT_SOLID` |
| 2 | `0x2` | `SHRINK` |
| 4 | `0x4` | `EXPAND` |
| 8 | `0x8` | `FADE` |
| 16 | `0x10` | `DIRECTIONAL_FRICTION` |
| 32 | `0x20` | `CAN_BE_DISTURBED` |
| 64 | `0x40` | `ROTATE_TO_MOVEMENT_DIRECTION` |
| 512 | `0x200` | `ALWAYS_TRIGGER` |

## GameLevel.LevFlag

| Число | Hex | Имя |
|---:|---|---|
| 4 | `0x4` | `UNKNOWN` |
| 8 | `0x8` | `ALWAYS_IMPRISON_ENEMIES` |
| 16 | `0x10` | `ONE_SHOT_HORNY` |
| 32 | `0x20` | `IS_SECRET_LEVEL` |
| 64 | `0x40` | `IS_SPECIAL_LEVEL` |
| 128 | `0x80` | `SHOW_HERO_KILLS` |
| 256 | `0x100` | `AUTO_OBJECTIVE_BOX` |
| 512 | `0x200` | `HEART_MAKES_GEM` |
| 1024 | `0x400` | `IS_MULTIPLAYER_LEVEL` |
| 2048 | `0x800` | `IS_SKIRMISH_LEVEL` |
| 4096 | `0x1000` | `FREEZE_OPTIONS` |
| 8192 | `0x2000` | `IS_MY_PET_DUNGEON_LEVEL` |

## GameLevel.LevelReward

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `ALARM` |
| 2 | `0x2` | `BARRICADE` |
| 3 | `0x3` | `BOULDER` |
| 4 | `0x4` | `BRACED` |
| 5 | `0x5` | `BRIDGE_STONE` |
| 6 | `0x6` | `BRIDGE_WOODEN` |
| 7 | `0x7` | `CALL_TO_ARMS` |
| 8 | `0x8` | `CASINO` |
| 12 | `0xC` | `DARK_LIBRARY` |
| 14 | `0xE` | `FEAR` |
| 15 | `0xF` | `FIREBURST` |
| 16 | `0x10` | `FREEZE` |
| 17 | `0x11` | `GAS` |
| 18 | `0x12` | `GRAVEYARD` |
| 19 | `0x13` | `GUARD_ROOM` |
| 20 | `0x14` | `GUARD_POST` |
| 21 | `0x15` | `HATCHERY` |
| 24 | `0x18` | `LAIR` |
| 25 | `0x19` | `LIGHTNING` |
| 26 | `0x1A` | `MAGIC_DOOR` |
| 27 | `0x1B` | `PIT` |
| 29 | `0x1D` | `PRISON` |
| 30 | `0x1E` | `REAPER_TALISMAN_4` |
| 32 | `0x20` | `SECRET_DOOR` |
| 33 | `0x21` | `SENTRY` |
| 35 | `0x23` | `SPIKE` |
| 36 | `0x24` | `STEEL_DOOR` |
| 39 | `0x27` | `TEMPLE` |
| 41 | `0x29` | `TORTURE` |
| 42 | `0x2A` | `TRAINING` |
| 43 | `0x2B` | `TREASURY` |
| 44 | `0x2C` | `TRIGGER` |
| 47 | `0x2F` | `WOOD_DOOR` |
| 48 | `0x30` | `WORK_SHOP` |
| 49 | `0x31` | `REAPER_TALISMAN_1` |
| 50 | `0x32` | `REAPER_TALISMAN_2` |
| 51 | `0x33` | `REAPER_TALISMAN_3` |

## GameObject.ObjectFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `DIE_OVER_TIME` |
| 2 | `0x2` | `DIE_OVER_TIME_IF_NOT_IN_ROOM` |
| 4 | `0x4` | `OBJECT_TYPE_SPECIAL` |
| 8 | `0x8` | `OBJECT_TYPE_SPELL_BOOK` |
| 16 | `0x10` | `OBJECT_TYPE_CRATE` |
| 32 | `0x20` | `OBJECT_TYPE_LAIR` |
| 64 | `0x40` | `OBJECT_TYPE_GOLD` |
| 128 | `0x80` | `OBJECT_TYPE_FOOD` |
| 256 | `0x100` | `CAN_BE_PICKED_UP` |
| 512 | `0x200` | `CAN_BE_SLAPPED` |
| 1024 | `0x400` | `DIE_WHEN_SLAPPED` |
| 4096 | `0x1000` | `OBJECT_TYPE_LEVEL_GEM` |
| 8192 | `0x2000` | `CAN_BE_DROPPED_ON_ANY_LAND` |
| 16384 | `0x4000` | `OBSTACLE` |
| 32768 | `0x8000` | `BOUNCE` |
| 65536 | `0x10000` | `BOULDER_CAN_ROLL_THROUGH` |
| 131072 | `0x20000` | `BOULDER_DESTROYS` |
| 262144 | `0x40000` | `PILLAR` |
| 1048576 | `0x100000` | `DOOR_KEY` |
| 2097152 | `0x200000` | `DAMAGEABLE` |
| 4194304 | `0x400000` | `HIGHLIGHTABLE` |
| 8388608 | `0x800000` | `PLACEABLE` |
| 16777216 | `0x1000000` | `FIRST_PERSON_OBSTACLE` |
| 33554432 | `0x2000000` | `SOLID_OBSTACLE` |
| 67108864 | `0x4000000` | `CAST_SHADOWS` |

## GameObject.State

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `BEING_DROPPED` |
| 2 | `0x2` | `BEING_SLAPPED` |
| 3 | `0x3` | `PRISON_ARRIVE` |
| 4 | `0x4` | `TEMPLE_ARRIVE` |
| 5 | `0x5` | `GOLD_ARRIVE` |
| 6 | `0x6` | `HATCHERY_ARRIVE` |
| 7 | `0x7` | `LAIR_ARRIVE` |
| 8 | `0x8` | `PECKING` |
| 9 | `0x9` | `MOVE_TO` |
| 10 | `0xA` | `WANDER` |
| 11 | `0xB` | `WANDER_WITHIN_PLAYER` |
| 12 | `0xC` | `CREATE_CALL_TO_ARMS` |
| 13 | `0xD` | `CALL_TO_ARMS` |
| 14 | `0xE` | `RESEND_CALL_TO_ARMS` |
| 15 | `0xF` | `DESTROY_CALL_TO_ARMS` |
| 16 | `0x10` | `DO_NOTHING` |
| 17 | `0x11` | `BOULDER` |
| 18 | `0x12` | `SPINNING` |
| 19 | `0x13` | `DESTROY_CALL_TO_ARMS_COPY` |
| 20 | `0x14` | `DESTROY_CALL_TO_ARMS_COPY_INIT` |
| 21 | `0x15` | `ROTATE` |
| 22 | `0x16` | `BEING_PICKED_UP` |
| 23 | `0x17` | `DODGE` |
| 24 | `0x18` | `FREE_CHICKEN` |
| 25 | `0x19` | `ESCAPE_FROM_FIGHT` |
| 26 | `0x1A` | `ACTIVATE_ANIM` |
| 27 | `0x1B` | `WORKSHOP_ARRIVE` |
| 28 | `0x1C` | `KICKED` |
| 29 | `0x1D` | `SHAKE` |
| 30 | `0x1E` | `TORTURE_WHEEL_SPIN` |
| 31 | `0x1F` | `GOLD_PROCESS` |
| 32 | `0x20` | `HATCHING` |
| 33 | `0x21` | `FLARE` |
| 34 | `0x22` | `TOMBSTONE_SCALE_UP` |
| 35 | `0x23` | `TOMBSTONE_SCALE_DOWN` |
| 36 | `0x24` | `SPECIAL` |
| 37 | `0x25` | `HEART_CONSTRUCTION` |
| 38 | `0x26` | `DESTRUCTOR` |
| 39 | `0x27` | `MANA_VAULT` |
| 40 | `0x28` | `TORTURE_COALS` |
| 41 | `0x29` | `THREAT_BURN` |
| 42 | `0x2A` | `IDLE_EFFECT` |
| 43 | `0x2B` | `FRONTEND_LEVEL_GEM` |
| 44 | `0x2C` | `PRISON_DOOR_OPEN` |
| 45 | `0x2D` | `PRISON_DOOR_CLOSE` |
| 46 | `0x2E` | `OPEN_PRISON_DOOR_BAR` |
| 47 | `0x2F` | `CLOSE_PRISON_DOOR_BAR` |
| 48 | `0x30` | `PLAY_WIN_ANIM_INDICATOR_TO_SMILES` |
| 49 | `0x31` | `PLAY_WIN_ANIM_INDICATOR_TO_MONEY` |
| 50 | `0x32` | `PORTAL_GEM` |
| 51 | `0x33` | `MY_PET_DUNGEON_LEVEL_INDICATOR` |
| 52 | `0x34` | `ENTRANCE_ARRIVE` |
| 53 | `0x35` | `SPELL_BOOK_IDLE` |
| 54 | `0x36` | `MPD_LEVEL_UP` |
| 55 | `0x37` | `MPD_LEVEL_DOWN` |
| 56 | `0x38` | `JACK_IN_THE_BOX` |
| 57 | `0x39` | `CRYPT_DANCER` |
| 58 | `0x3A` | `CRYPT_SPECIAL_DANCER` |
| 59 | `0x3B` | `GEM_CHALLENGE_JEWEL` |
| 60 | `0x3C` | `PLINTH` |
| 61 | `0x3D` | `SPECIAL_PLINTH` |
| 62 | `0x3E` | `DROP_OFF_POINT` |
| 63 | `0x3F` | `GEM_EXIT_POINT` |

## KeeperSpell.KeeperSpellFlag

| Число | Hex | Имя |
|---:|---|---|
| 2 | `0x2` | `CREATE_WORKER` |
| 4 | `0x4` | `ATTACKING` |
| 8 | `0x8` | `DEFENSIVE` |
| 16 | `0x10` | `DONT_CAST_ON_CREATURES` |

## KeeperSpell.HandAnimId

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NULL` |
| 1 | `0x1` | `POINT` |

## KeeperSpell.CastRule

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `OWN_LAND` |
| 2 | `0x2` | `OWN_AND_NEUTRAL_LAND` |
| 3 | `0x3` | `ENEMY_LAND` |
| 4 | `0x4` | `ANY_LAND` |
| 5 | `0x5` | `ANYWHERE` |

## KeeperSpell.TargetRule

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `OWN_CREATURES` |
| 2 | `0x2` | `ENEMY_CREATURES` |
| 3 | `0x3` | `ALL_CREATURES` |
| 4 | `0x4` | `LAND` |
| 5 | `0x5` | `ALL` |
| 6 | `0x6` | `POSESSION` |

## Light.LightFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `UNKNOWN` |
| 2 | `0x2` | `FLICKER` |
| 4 | `0x4` | `PULSE` |
| 128 | `0x80` | `PLAYER_COLOURED` |
| 2048 | `0x800` | `COLOR_RED` |
| 4096 | `0x1000` | `COLOR_GREEN` |
| 8192 | `0x2000` | `COLOR_BLUE` |

## MapDataTypeEnum

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `GLOBALS` |
| 100 | `0x64` | `MAP` |
| 110 | `0x6E` | `TERRAIN` |
| 120 | `0x78` | `ROOMS` |
| 130 | `0x82` | `TRAPS` |
| 140 | `0x8C` | `DOORS` |
| 150 | `0x96` | `KEEPER_SPELLS` |
| 160 | `0xA0` | `CREATURE_SPELLS` |
| 170 | `0xAA` | `CREATURES` |
| 180 | `0xB4` | `PLAYERS` |
| 190 | `0xBE` | `THINGS` |
| 210 | `0xD2` | `TRIGGERS` |
| 220 | `0xDC` | `LEVEL` |
| 230 | `0xE6` | `VARIABLES` |
| 240 | `0xF0` | `OBJECTS` |
| 250 | `0xFA` | `EFFECT_ELEMENTS` |
| 260 | `0x104` | `SHOTS` |
| 270 | `0x10E` | `EFFECTS` |

## Material

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `FLESH` |
| 2 | `0x2` | `ROCK` |
| 3 | `0x3` | `WOOD` |
| 4 | `0x4` | `METAL1` |
| 5 | `0x5` | `METAL2` |
| 6 | `0x6` | `MAGIC` |
| 7 | `0x7` | `GLASS` |

## Room.RoomFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `PLACEABLE_ON_WATER` |
| 2 | `0x2` | `PLACEABLE_ON_LAVA` |
| 4 | `0x4` | `PLACEABLE_ON_LAND` |
| 8 | `0x8` | `HAS_WALLS` |
| 16 | `0x10` | `CENTRE` |
| 32 | `0x20` | `SPECIAL_TILES` |
| 64 | `0x40` | `NORMAL_TILES` |
| 128 | `0x80` | `BUILDABLE` |
| 256 | `0x100` | `SPECIAL_WALLS` |
| 512 | `0x200` | `ATTACKABLE` |
| 1024 | `0x400` | `UNKNOWN` |
| 2048 | `0x800` | `UNKNOWN_2` |
| 4096 | `0x1000` | `HAS_FLAME` |
| 8192 | `0x2000` | `IS_GOOD` |

## Room.TileConstruction

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `COMPLETE` |
| 2 | `0x2` | `QUAD` |
| 5 | `0x5` | `NORMAL` |
| 6 | `0x6` | `CENTER_POOL` |
| 7 | `0x7` | `DOUBLE_QUAD` |
| 9 | `0x9` | `HERO_GATE` |
| 10 | `0xA` | `HERO_GATE_TILE` |
| 11 | `0xB` | `HERO_GATE_2_BY_2` |
| 12 | `0xC` | `HERO_GATE_FRONT_END` |
| 13 | `0xD` | `HERO_GATE_3_BY_1` |

## Shot.ShotFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `DIE_WHEN_HIT_SOLID` |
| 2 | `0x2` | `DIE_WHEN_HIT_THING` |
| 4 | `0x4` | `DIE_OVER_TIME` |
| 8 | `0x8` | `BOUNCE` |
| 16 | `0x10` | `THROWN_PROJECTILE` |
| 32 | `0x20` | `MOVE_TOWARDS_TARGET` |
| 64 | `0x40` | `KILL_EFFECTS_WHEN_DEAD` |
| 128 | `0x80` | `PASS_ON_SHOT_VELOCITY_TO_CREATION_EFFECT` |
| 256 | `0x100` | `RECOIL` |

## Shot.ShotProcessFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `NO_PROCESSING` |
| 2 | `0x2` | `CONSTANT` |
| 4 | `0x4` | `HIT_COLLIDE_THING` |
| 8 | `0x8` | `HIT_SOLID` |
| 16 | `0x10` | `HIT_WATER` |
| 32 | `0x20` | `HIT_LAVA` |
| 64 | `0x40` | `AT_START` |
| 128 | `0x80` | `AT_END` |
| 256 | `0x100` | `HIT_THING` |

## Shot.DamageType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `CONSTANT` |
| 2 | `0x2` | `RANGE_FROM_ORIGIN` |
| 3 | `0x3` | `TIME` |
| 4 | `0x4` | `RING_CONSTANT` |
| 5 | `0x5` | `AT_START` |
| 6 | `0x6` | `AT_END` |
| 7 | `0x7` | `HIT_THING` |
| 8 | `0x8` | `CONSTANT_FOR_TREMOR` |

## Shot.CollideType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `OWN_CREATURES` |
| 2 | `0x2` | `ENEMY_CREATURES` |
| 3 | `0x3` | `ALL_CREATURES` |
| 4 | `0x4` | `DEAD_BODIES` |

## Shot.ProcessType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `CREATE_CREATURE` |
| 2 | `0x2` | `CREATE_OBJECT` |
| 3 | `0x3` | `MODIFY_HEALTH` |
| 4 | `0x4` | `MODIFY_SPEED` |
| 5 | `0x5` | `MODIFY_ARMOUR` |
| 6 | `0x6` | `MODIFY_MANA` |
| 7 | `0x7` | `MODIFY_EXPERIENCE_LEVEL` |
| 8 | `0x8` | `MODIFY_HUNGER` |
| 9 | `0x9` | `LIGHTNING` |
| 10 | `0xA` | `INVUNERABLE` |
| 11 | `0xB` | `FREEZE` |
| 12 | `0xC` | `ALARM` |
| 13 | `0xD` | `TURNCOAT` |
| 14 | `0xE` | `CONCEAL` |
| 15 | `0xF` | `CHICKEN` |
| 16 | `0x10` | `SCAPEGOAT` |
| 17 | `0x11` | `WIND` |
| 18 | `0x12` | `RAISE_DEAD` |
| 19 | `0x13` | `OBEY` |
| 20 | `0x14` | `INVISIBLE` |
| 21 | `0x15` | `FEAR` |
| 22 | `0x16` | `BOULDER` |
| 23 | `0x17` | `ALERT` |
| 24 | `0x18` | `MESMERISE` |
| 25 | `0x19` | `SIGHT_OF_EVIL` |
| 26 | `0x1A` | `POSSESS_CREATURE` |
| 27 | `0x1B` | `TREMOR` |
| 28 | `0x1C` | `GUIDED` |
| 29 | `0x1D` | `DECKEM` |
| 30 | `0x1E` | `INFERNO` |
| 31 | `0x1F` | `GAS` |
| 32 | `0x20` | `TRIGGER` |
| 33 | `0x21` | `SUMMON_REAPER` |
| 34 | `0x22` | `INSTANTANEOUS_HIT` |
| 35 | `0x23` | `DRAIN` |
| 36 | `0x24` | `CREATE_SINGLE_CREATURE` |
| 37 | `0x25` | `WEB_AREA_EFFECT` |
| 38 | `0x26` | `POISON_SPIT` |
| 39 | `0x27` | `JACK_IN_THE_BOX` |

## Shot.AttackCategory

| Число | Hex | Имя |
|---:|---|---|
| 4 | `0x4` | `FIRE` |
| 5 | `0x5` | `MOVEMENT` |
| 6 | `0x6` | `LIGHTNING` |
| 7 | `0x7` | `GAS` |
| 8 | `0x8` | `PROJECTILE` |
| 9 | `0x9` | `ENCHANCE_OTHER` |
| 10 | `0xA` | `ENCHANCE_SELF` |
| 11 | `0xB` | `CLOSE_COMBAT` |
| 12 | `0xC` | `GENERATION` |
| 13 | `0xD` | `PHYSICAL_TRAP` |
| 14 | `0xE` | `NON_LETHAL_TRAP` |
| 15 | `0xF` | `MELEE_SCYTHE` |

## Terrain.TerrainFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `SOLID` |
| 2 | `0x2` | `IMPENETRABLE` |
| 4 | `0x4` | `OWNABLE` |
| 8 | `0x8` | `TAGGABLE` |
| 16 | `0x10` | `ROOM` |
| 32 | `0x20` | `ATTACKABLE` |
| 64 | `0x40` | `TORCH` |
| 128 | `0x80` | `WATER` |
| 256 | `0x100` | `LAVA` |
| 512 | `0x200` | `ALWAYS_EXPLORED` |
| 1024 | `0x400` | `PLAYER_COLOURED_PATH` |
| 2048 | `0x800` | `PLAYER_COLOURED_WALL` |
| 4096 | `0x1000` | `CONSTRUCTION_TYPE_WATER` |
| 8192 | `0x2000` | `CONSTRUCTION_TYPE_QUAD` |
| 16384 | `0x4000` | `UNEXPLORE_IF_DUG_BY_ANOTHER_PLAYER` |
| 32768 | `0x8000` | `FILL_INABLE` |
| 65536 | `0x10000` | `ALLOW_ROOM_WALLS` |
| 131072 | `0x20000` | `DECAY` |
| 262144 | `0x40000` | `RANDOM_TEXTURE` |
| 524288 | `0x80000` | `TERRAIN_COLOR_RED` |
| 1048576 | `0x100000` | `TERRAIN_COLOR_GREEN` |
| 2097152 | `0x200000` | `TERRAIN_COLOR_BLUE` |
| 8388608 | `0x800000` | `DWARF_CAN_DIG_THROUGH` |
| 16777216 | `0x1000000` | `REVEAL_THROUGH_FOG_OF_WAR` |
| 33554432 | `0x2000000` | `AMBIENT_COLOR_RED` |
| 67108864 | `0x4000000` | `AMBIENT_COLOR_GREEN` |
| 134217728 | `0x8000000` | `AMBIENT_COLOR_BLUE` |
| 268435456 | `0x10000000` | `TERRAIN_LIGHT` |
| 536870912 | `0x20000000` | `AMBIENT_LIGHT` |

## Thing.ActionPoint.ActionPointFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `HERO_LAIR` |
| 4 | `0x4` | `UNKNOWN_4` |
| 8 | `0x8` | `UNKNOWN_8` |
| 16 | `0x10` | `REVEAL_THROUGH_FOG_OF_WAR` |
| 32 | `0x20` | `TOOL_BOX` |
| 64 | `0x40` | `IGNORE_SOLID` |

## Thing.Creature.CreatureFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `WILL_FIGHT` |
| 2 | `0x2` | `LEADER` |
| 4 | `0x4` | `FOLLOWER` |
| 8 | `0x8` | `WILL_BE_ATTACKED` |
| 16 | `0x10` | `RETURN_TO_HERO_LAIR` |
| 32 | `0x20` | `FREE_FRIENDS_ON_JAIL_BREAK` |
| 64 | `0x40` | `ACT_AS_DROPPED` |
| 128 | `0x80` | `START_AS_DYING` |

## Thing.Creature.CreatureFlag2

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `DESTROY_ROOMS` |
| 2 | `0x2` | `I_AM_A_TOOL` |
| 4 | `0x4` | `DIES_INSTANTLY` |
| 8 | `0x8` | `I_AM_A_MERCENARY` |

## Thing.Room.RoomType

| Число | Hex | Имя |
|---:|---|---|
| 12 | `0xC` | `PORTAL` |
| 14 | `0xE` | `DUNGEON_HEART` |
| 33 | `0x21` | `HERO_GATE_2X2` |
| 34 | `0x22` | `HERO_GATE_FRONT_END` |
| 37 | `0x25` | `HERO_GATE_3X1` |
| 40 | `0x28` | `HERO_PORTAL` |

## Thing.Room.Direction

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NORTH` |
| 2 | `0x2` | `EAST` |
| 4 | `0x4` | `SOUTH` |
| 6 | `0x6` | `WEST` |

## Thing.Camera.CameraFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `DISABLE_YAW` |
| 2 | `0x2` | `DISABLE_ROLL` |
| 4 | `0x4` | `DISABLE_PITCH` |
| 8 | `0x8` | `DISABLE_ZOOM` |
| 16 | `0x10` | `UNKNOWN_10` |
| 32 | `0x20` | `UNKNOWN_20` |
| 64 | `0x40` | `DISABLE_MOVE` |
| 128 | `0x80` | `DISABLE_CHANGE` |

## Thing.HeroParty.Objective

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 11 | `0xB` | `DESTROY_ROOMS` |
| 12 | `0xC` | `DESTROY_WALLS` |
| 13 | `0xD` | `STEAL_GOLD` |
| 14 | `0xE` | `STEAL_SPELLS` |
| 17 | `0x11` | `STEAL_MAFUFACTURE_CRATES` |
| 18 | `0x12` | `KILL_CREATURES` |
| 19 | `0x13` | `KILL_PLAYER` |
| 22 | `0x16` | `WAIT` |
| 23 | `0x17` | `SEND_TO_ACTION_POINT` |
| 27 | `0x1B` | `JAIL_BREAK` |

## Thing.Door.DoorFlag

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `LOCKED` |
| 2 | `0x2` | `BLUEPRINT` |

## Tile.BridgeTerrainType

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `WATER` |
| 2 | `0x2` | `LAVA` |

## Trap.TrapFlag

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `DIE_WHEN_TRIGGERED` |
| 2 | `0x2` | `REVEAL_WHEN_FIRED` |
| 8 | `0x8` | `UNKNOWN1` |
| 16 | `0x10` | `DISARMABLE` |
| 32 | `0x20` | `INVISIBLE` |
| 128 | `0x80` | `TRACK_NEAREST_TARGET` |
| 256 | `0x100` | `REQUIRE_MANA` |
| 512 | `0x200` | `LOOP_FIRE_ANIMATION` |
| 1024 | `0x400` | `UNKNOWN2` |
| 2048 | `0x800` | `GUARD_POST` |
| 4096 | `0x1000` | `OBSTACLE` |
| 8192 | `0x2000` | `DOOR_TRAP` |
| 32768 | `0x8000` | `IS_GOOD` |
| 65536 | `0x10000` | `FIRST_PERSON_OBSTACLE` |
| 131072 | `0x20000` | `SOLID_OBSTACLE` |

## Trap.TriggerType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `LINE_OF_SIGHT` |
| 2 | `0x2` | `PRESSURE` |
| 3 | `0x3` | `TRIGGER` |

## TriggerAction.ActionType

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `CREATE_CREATURE` |
| 2 | `0x2` | `CREATE_PORTAL_GEM` |
| 3 | `0x3` | `SEND_TO_AP` |
| 4 | `0x4` | `DISPLAY_OBJECTIVE` |
| 5 | `0x5` | `INFORMATION` |
| 6 | `0x6` | `MAKE` |
| 7 | `0x7` | `FLAG` |
| 8 | `0x8` | `INITIALIZE_TIMER` |
| 9 | `0x9` | `FLASH_BUTTON` |
| 10 | `0xA` | `WIN_GAME` |
| 11 | `0xB` | `LOSE_GAME` |
| 12 | `0xC` | `WIN_SUBOBJECTIVE` |
| 13 | `0xD` | `LOSE_SUBOBJECTIVE` |
| 14 | `0xE` | `CREATE_HERO_PARTY` |
| 15 | `0xF` | `SET_OBJECTIVE` |
| 16 | `0x10` | `FLASH_ACTION_POINT` |
| 17 | `0x11` | `REVEAL_ACTION_POINT` |
| 18 | `0x12` | `SET_ALLIANCE` |
| 20 | `0x14` | `ATTACH_PORTAL_GEM` |
| 21 | `0x15` | `FORCE_FIRST_PERSON` |
| 22 | `0x16` | `ALTER_TERRAIN_TYPE` |
| 23 | `0x17` | `SET_TIME_LIMIT` |
| 24 | `0x18` | `PLAY_SPEECH` |
| 25 | `0x19` | `DISPLAY_TEXT_STRING` |
| 26 | `0x1A` | `ZOOM_TO_ACTION_POINT` |
| 27 | `0x1B` | `ROTATE_AROUND_ACTION_POINT` |
| 28 | `0x1C` | `GENERATE_CREATURE` |
| 29 | `0x1D` | `MAKE_HUNGRY` |
| 30 | `0x1E` | `SHOW_HEALTH_FLOWER` |
| 31 | `0x1F` | `FOLLOW_CAMERA_PATH` |
| 32 | `0x20` | `COLLAPSE_HERO_GATE` |
| 33 | `0x21` | `ALTER_SPEED` |
| 34 | `0x22` | `SET_MUSIC_LEVEL` |
| 35 | `0x23` | `REMOVE_FROM_MAP` |
| 36 | `0x24` | `SET_FIGHT_FLAG` |
| 37 | `0x25` | `SET_PORTAL_STATUS` |
| 38 | `0x26` | `SET_WIDESCREEN_MODE` |
| 42 | `0x2A` | `MAKE_OBJECTIVE` |
| 43 | `0x2B` | `ZOOM_TO` |
| 44 | `0x2C` | `SET_CREATURE_MOODS` |
| 45 | `0x2D` | `SET_SYSTEM_MESSAGES` |
| 46 | `0x2E` | `DISPLAY_SLAB_OWNER` |
| 47 | `0x2F` | `DISPLAY_NEXT_ROOM_TYPE` |
| 48 | `0x30` | `TOGGLE_EFFECT_GENERATOR` |
| 49 | `0x31` | `CHANGE_ROOM_OWNER` |
| 50 | `0x32` | `SET_SLAPS_LIMIT` |
| 51 | `0x33` | `SET_TIMER_SPEECH` |

## TriggerAction.MakeType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `MISC_BUTTON` |
| 1 | `0x1` | `ROOM` |
| 2 | `0x2` | `CREATURE` |
| 3 | `0x3` | `DOOR` |
| 4 | `0x4` | `TRAP` |
| 5 | `0x5` | `KEEPER_SPELL` |

## TriggerAction.ButtonType

| Число | Hex | Имя |
|---:|---|---|
| 255 | `0xFF` | `SELL_ROOM` |
| 253 | `0xFD` | `ZOOM_IN_OUT` |
| 248 | `0xF8` | `CREATURE_TAB` |
| 247 | `0xF7` | `GUI_MIN_MAX` |
| 246 | `0xF6` | `HORNY_TALISMAN` |
| 245 | `0xF5` | `MPD_TOOLBOX` |
| 244 | `0xF4` | `MPD_SINGLE_INVASION` |
| 243 | `0xF3` | `GOLD_VALUE` |
| 242 | `0xF2` | `MANA_VALUE` |
| 241 | `0xF1` | `MPD_CONTINUAL_INVASION` |

## TriggerGeneric.TargetType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `FLAG` |
| 2 | `0x2` | `TIMER` |
| 3 | `0x3` | `CREATURE_CREATED` |
| 4 | `0x4` | `CREATURE_KILLED` |
| 5 | `0x5` | `CREATURE_SLAPPED` |
| 6 | `0x6` | `CREATURE_ATTACKED` |
| 7 | `0x7` | `CREATURE_IMPRISONED` |
| 8 | `0x8` | `CREATURE_TORTURED` |
| 9 | `0x9` | `CREATURE_CONVERTED` |
| 10 | `0xA` | `CREATURE_CLAIMED` |
| 11 | `0xB` | `CREATURE_ANGRY` |
| 12 | `0xC` | `CREATURE_AFRAID` |
| 13 | `0xD` | `CREATURE_STEALS` |
| 14 | `0xE` | `CREATURE_LEAVES` |
| 15 | `0xF` | `CREATURE_STUNNED` |
| 16 | `0x10` | `CREATURE_DYING` |
| 17 | `0x11` | `DOOR_DESTROYED` |
| 18 | `0x12` | `OBJECT_CLAIMED` |
| 19 | `0x13` | `PLAYER_CREATURES` |
| 20 | `0x14` | `PLAYER_HAPPY_CREATURES` |
| 21 | `0x15` | `PLAYER_ANGRY_CREATURES` |
| 22 | `0x16` | `PLAYER_CREATURES_KILLED` |
| 23 | `0x17` | `PLAYER_KILLS_CREATURES` |
| 24 | `0x18` | `PLAYER_ROOM_SLABS` |
| 25 | `0x19` | `PLAYER_ROOMS` |
| 26 | `0x1A` | `PLAYER_ROOM_SIZE` |
| 27 | `0x1B` | `PLAYER_DOORS` |
| 28 | `0x1C` | `PLAYER_TRAPS` |
| 29 | `0x1D` | `PLAYER_KEEPER_SPELL` |
| 30 | `0x1E` | `PLAYER_GOLD` |
| 31 | `0x1F` | `PLAYER_GOLD_MINED` |
| 32 | `0x20` | `PLAYER_MANA` |
| 33 | `0x21` | `PLAYER_DESTROYS` |
| 34 | `0x22` | `LEVEL_TIME` |
| 35 | `0x23` | `LEVEL_CREATURES` |
| 36 | `0x24` | `CREATURE_HEALTH` |
| 37 | `0x25` | `CREATURE_GOLD_HELD` |
| 39 | `0x27` | `AP_CONGREGATE_IN` |
| 40 | `0x28` | `AP_CLAIM_PART_OF` |
| 41 | `0x29` | `AP_CLAIM_ALL_OF` |
| 42 | `0x2A` | `AP_SLAB_TYPES` |
| 43 | `0x2B` | `PARTY_CREATED` |
| 44 | `0x2C` | `PARTY_MEMBERS_KILLED` |
| 45 | `0x2D` | `PARTY_MEMBERS_CAPTURED` |
| 46 | `0x2E` | `LEVEL_PAY_DAY` |
| 47 | `0x2F` | `PLAYER_KILLED` |
| 49 | `0x31` | `CREATURE_EXPERIENCE_LEVEL` |
| 52 | `0x34` | `PLAYER_CREATURES_AT_LEVEL` |
| 53 | `0x35` | `GUI_BUTTON_PRESSED` |
| 54 | `0x36` | `CREATURE_HUNGER_SATED` |
| 55 | `0x37` | `CREATURE_PICKS_UP_PORTAL_GEM` |
| 56 | `0x38` | `PLAYER_DUNGEON_BREACHED` |
| 57 | `0x39` | `PLAYER_ENEMY_BREACHED` |
| 58 | `0x3A` | `PLAYER_CREATURE_PICKED_UP` |
| 59 | `0x3B` | `PLAYER_CREATURE_DROPPED` |
| 60 | `0x3C` | `PLAYER_CREATURE_SLAPPED` |
| 61 | `0x3D` | `PLAYER_CREATURE_SACKED` |
| 62 | `0x3E` | `AP_TAG_PART_OF` |
| 63 | `0x3F` | `CREATURE_SACKED` |
| 64 | `0x40` | `PARTY_MEMBERS_INCAPACITATED` |
| 65 | `0x41` | `CREATURE_PICKED_UP` |
| 67 | `0x43` | `LEVEL_PLAYED` |
| 68 | `0x44` | `PLAYER_ROOM_FURNITURE` |
| 69 | `0x45` | `AP_TAG_ALL_OF` |
| 70 | `0x46` | `AP_POSESSED_CREATURE_ENTERS` |
| 71 | `0x47` | `PLAYER_SLAPS` |
| 72 | `0x48` | `GUI_TRANSITION_ENDS` |
| 73 | `0x49` | `PLAYER_CREATURES_GROUPED` |
| 74 | `0x4A` | `PLAYER_CREATURES_DYING` |

## Variable.SacrificeType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `CREATURE` |
| 2 | `0x2` | `OBJECT` |
| 3 | `0x3` | `KEEPER_SPELL` |

## Variable.SacrificeRewardType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `NONE` |
| 1 | `0x1` | `CREATURE` |
| 2 | `0x2` | `OBJECT` |
| 3 | `0x3` | `GOLD` |
| 4 | `0x4` | `MANA` |
| 5 | `0x5` | `KEEPER_SPELL` |
| 6 | `0x6` | `CREATURES_HAPPY` |
| 7 | `0x7` | `CREATURES_UNHAPPY` |
| 8 | `0x8` | `CREATURES_ANGRY` |
| 9 | `0x9` | `EASTER_EGG` |

## Variable.Availability.AvailabilityType

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `ROOM` |
| 4 | `0x4` | `DOOR` |
| 3 | `0x3` | `TRAP` |
| 5 | `0x5` | `SPELL` |
| 2 | `0x2` | `CREATURE` |

## Variable.Availability.AvailabilityValue

| Число | Hex | Имя |
|---:|---|---|
| 1 | `0x1` | `UNAVAILABLE` |
| 2 | `0x2` | `RESEARCHABLE` |
| 3 | `0x3` | `AVAILABLE` |
| 4 | `0x4` | `UNAVAILABLE_ROOM` |

## Variable.CreatureStats.StatType

| Число | Hex | Имя |
|---:|---|---|
| 0 | `0x0` | `HEIGHT_TILES` |
| 1 | `0x1` | `HEALTH` |
| 2 | `0x2` | `FEAR` |
| 3 | `0x3` | `THREAT` |
| 4 | `0x4` | `MELEE_DAMAGE` |
| 5 | `0x5` | `PAY` |
| 6 | `0x6` | `MAX_GOLD_HELD` |
| 7 | `0x7` | `INITIAL_GOLD_HELD` |
| 8 | `0x8` | `UNKNOWN_8` |
| 9 | `0x9` | `HUNGER_FILL_CHICKENS` |
| 10 | `0xA` | `MANA_GENERATED_BY_PRAYER_PER_SECOND` |
| 11 | `0xB` | `UNKNOWN_11` |
| 12 | `0xC` | `UNKNOWN_12` |
| 13 | `0xD` | `EXPERIENCE_POINTS_FOR_NEXT_LEVEL` |
| 14 | `0xE` | `UNKNOWN_14` |
| 15 | `0xF` | `EXPERIENCE_POINTS_PER_SECOND` |
| 16 | `0x10` | `EXPERIENCE_POINTS_FROM_TRAINING_PER_SECOND` |
| 17 | `0x11` | `RESEARCH_POINTS_PER_SECOND` |
| 18 | `0x12` | `MANUFACTURE_POINTS_PER_SECOND` |
| 19 | `0x13` | `DECOMPOSE_VALUE` |
| 20 | `0x14` | `UNKNOWN_20` |
| 21 | `0x15` | `SPEED_TILES_PER_SECOND` |
| 22 | `0x16` | `RUN_SPEED_TILES_PER_SECOND` |
| 23 | `0x17` | `UNKNOWN_23` |
| 24 | `0x18` | `TORTURE_TIME_TO_CONVERT_SECONDS` |
| 25 | `0x19` | `UNKNOWN_25` |
| 26 | `0x1A` | `UNKNOWN_26` |
| 27 | `0x1B` | `POSSESSION_MANA_COST_PER_SECOND` |
| 28 | `0x1C` | `OWN_LAND_HEALTH_INCREASE_PER_SECOND` |
| 29 | `0x1D` | `UNKNOWN_29` |
| 30 | `0x1E` | `DISTANCE_CAN_HEAR_TILES` |
| 31 | `0x1F` | `MELEE_RECHARGE_TIME_SECONDS` |

## Variable.MiscVariable.MiscType

| Число | Hex | Имя |
|---:|---|---|
| 3 | `0x3` | `ENTRANCE_GENERATION_SPEED_SECONDS` |
| 4 | `0x4` | `CLAIM_TILE_HEALTH` |
| 5 | `0x5` | `ATTACK_TILE_HEALTH` |
| 6 | `0x6` | `REPAIR_TILE_HEALTH` |
| 7 | `0x7` | `MINE_GOLD_HEALTH` |
| 8 | `0x8` | `DIG_ROCK_HEALTH` |
| 9 | `0x9` | `DIG_OWN_WALL_HEALTH` |
| 10 | `0xA` | `DIG_ENEMY_WALL_HEALTH` |
| 11 | `0xB` | `FILL_IN_HEALTH` |
| 12 | `0xC` | `REINFORCE_WALL_HEALTH` |
| 13 | `0xD` | `REPAIR_WALL_HEALTH` |
| 14 | `0xE` | `CONVERT_ROOM_HEALTH` |
| 15 | `0xF` | `ATTACK_ROOM_HEALTH` |
| 16 | `0x10` | `REPAIR_ROOM_HEALTH` |
| 18 | `0x12` | `CHICKEN_GENERATION_TIME_PER_HATCHERY` |
| 20 | `0x14` | `GOLD_MINED_FROM_GEMS` |
| 21 | `0x15` | `TIME_BETWEEN_REATTEMPTING_HUNGER_SECONDS` |
| 22 | `0x16` | `TIME_BETWEEN_REATTEMPTING_SLEEP_SECONDS` |
| 23 | `0x17` | `DEAD_BODY_DIES_AFTER_SECONDS` |
| 24 | `0x18` | `MAX_GOLD_PER_TREASURY_TILE` |
| 25 | `0x19` | `DECOMPOSE_VALUE_NEEDED_FOR_VAMPIRE` |
| 26 | `0x1A` | `CREATURE_DYING_STATE_DURATION_SECONDS` |
| 27 | `0x1B` | `SPECIAL_INCREASE_GOLD_AMOUNT` |
| 28 | `0x1C` | `SPECIAL_INCREASE_MANA_AMOUNT` |
| 30 | `0x1E` | `IMP_IDLE_DELAY_BEFORE_REEVALUATION_SECONDS` |
| 31 | `0x1F` | `DELAY_BEFORE_PAY_REEVALUATION_SECONDS` |
| 32 | `0x20` | `MAX_GOLD_PILE_OUTSIDE_TREASURY` |
| 33 | `0x21` | `PAY_DAY_FREQUENCY_SECONDS` |
| 34 | `0x22` | `MOTIONLESS_WHILE_RESEARCHING_SECONDS` |
| 35 | `0x23` | `MOTIONLESS_WHILE_MANUFACTURING_SECONDS` |
| 36 | `0x24` | `STATE_COUNTER_DELAY_BEFORE_REATTEMPT_EATING` |
| 37 | `0x25` | `STATE_COUNTER_WHILE_GUARDING` |
| 38 | `0x26` | `MOTIONLESS_WHILE_PRAYING_SECONDS` |
| 39 | `0x27` | `MOTIONLESS_IN_PRISON_SECONDS` |
| 40 | `0x28` | `MOTIONLESS_WHILE_TRAINING_SECONDS` |
| 41 | `0x29` | `MOTIONLESS_IN_CASINO_SECONDS` |
| 42 | `0x2A` | `EXCESS_MANA_DECREASE_RATE_PER_SECOND` |
| 45 | `0x2D` | `DEFAULT_HORIZONTAL_TERRAIN_WIBBLE` |
| 46 | `0x2E` | `DEFAULT_VERTICAL_TERRAIN_WIBBLE` |
| 47 | `0x2F` | `REBEL_MAX_FOLLOWERS` |
| 48 | `0x30` | `REBEL_LEAVING_PERCENTAGE` |
| 49 | `0x31` | `REBEL_BECOME_GOOD_PERCENTAGE` |
| 50 | `0x32` | `REBEL_DEFECT_PERCENTAGE` |
| 51 | `0x33` | `TIME_BEFORE_FIRST_PAY_DAY_SECONDS` |
| 52 | `0x34` | `DEFAULT_TORCH_LIGHT_RED` |
| 53 | `0x35` | `DEFAULT_TORCH_LIGHT_GREEN` |
| 54 | `0x36` | `DEFAULT_TORCH_LIGHT_BLUE` |
| 55 | `0x37` | `PRISON_MODIFY_CREATURE_HEALTH_PER_SECOND` |
| 67 | `0x43` | `DEFAULT_CELLING_HEIGHT_TILES` |
| 68 | `0x44` | `PAY_DAY_CUT_OFF_TIME_SECONDS` |
| 70 | `0x46` | `CASINO_MODIFY_CREATURE_GOLD_PER_SECOND` |
| 71 | `0x47` | `CASINO_BIG_WIN_KEEPER_LOSS_PERCENTAGE` |
| 73 | `0x49` | `CREATURES_SUPPORTED_BY_FIRST_PORTAL` |
| 75 | `0x4B` | `MODIFY_HEALTH_OF_CREATURE_IN_LAIR_PER_SECOND` |
| 76 | `0x4C` | `MODIFY_ANGER_OF_CREATURE_IN_LAIR_PER_SECOND` |
| 77 | `0x4D` | `MODIFY_CREATURE_STATS` |
| 78 | `0x4E` | `MODIFY_ANGER_IN_COMPANY_OF_HATED_CREATURES_PER_SECOND` |
| 79 | `0x4F` | `FORCE_APPLIED_TO_SLAPPED_CREATURE` |
| 82 | `0x52` | `CANNOT_SLEEP_MODIFY_CREATURE_HEALTH_PER_SECOND` |
| 83 | `0x53` | `CANNOT_LEAVE_MODIFY_CREATURE_HEALTH_PER_SECOND` |
| 84 | `0x54` | `PLAYER_RESCAN_INTERVAL_SECONDS` |
| 85 | `0x55` | `PLAYER_RESCAN_RANGE` |
| 86 | `0x56` | `MAX_FREE_RANGE_CHICKENS_PER_PLAYER` |
| 87 | `0x57` | `WOOD_BRIDGE_LIFE_ON_LAVA_SECONDS` |
| 88 | `0x58` | `CREATURE_SLEEPS_WHEN_BELOW_PERCENT_HEALTH` |
| 93 | `0x5D` | `DOOR_PICKED_TIME_SECONDS` |
| 94 | `0x5E` | `MINIMUM_IMP_THRESHOLD` |
| 95 | `0x5F` | `TIME_BEFORE_FREE_IMP_GENERATED_SECONDS` |
| 96 | `0x60` | `CREATURE_STUNNED_TIME_SECONDS` |
| 97 | `0x61` | `CREATURE_STUNNED_EFFECT_DELAY_SECONDS` |
| 98 | `0x62` | `FIRST_PERSON_STAT_INCREASE_PERCENTAGE` |
| 100 | `0x64` | `MANA_COST_IN_FIRST_PERSON_PER_SECOND` |
| 101 | `0x65` | `HEALTH_DECREASE_WHEN_ON_FIRE_PER_SECOND` |
| 102 | `0x66` | `MODIFY_CREATURE_ANGER_WHILE_PRAYING_PER_SECOND` |
| 104 | `0x68` | `ROOM_SELL_VALUE_PERCENTAGE_OF_COST` |
| 105 | `0x69` | `DOOR_SELL_VALUE_PERCENTAGE_OF_COST` |
| 106 | `0x6A` | `TRAP_SELL_VALUE_PERCENTAGE_OF_COST` |
| 110 | `0x6E` | `INSUFFICIENT_MANA_IMP_EVALUATION_PERIOD_SECONDS` |
| 111 | `0x6F` | `DEATH_WHEN_POSSESSING_MANA_REDUCTION` |
| 112 | `0x70` | `PERCENTAGE_OF_MANA_GAINED_BY_SACRIFICING` |
| 113 | `0x71` | `POSSESSION_FRIENDLY_STAT_INCREASE_PERCENTAGE` |
| 114 | `0x72` | `POSSESSION_FRIENDLY_HEALTH_INCREASE_PERCENTAGE` |
| 115 | `0x73` | `MODIFY_CREATURE_HEALTH_WHILE_PRAYING_PER_SECOND` |
| 116 | `0x74` | `HYPNOTISM_MANA_DRAIN_PER_SECOND` |
| 125 | `0x7D` | `LOW_MANA_WARNING_THRESHOLD` |
| 126 | `0x7E` | `TRAINING_ROOM_MAX_EXPERIENCE_LEVEL` |
| 127 | `0x7F` | `STUNNED_DAMAGE_INCREASE_PERCENTAGE` |
| 128 | `0x80` | `MAX_NUMBER_OF_THINGS_IN_HAND` |
| 129 | `0x81` | `RUBBER_BAND_AREA_LIMIT` |
| 130 | `0x82` | `GRAVITY_CONSTANT` |
| 131 | `0x83` | `DUNGEON_HEART_HEALTH_REGENERATION_PER_SECOND` |
| 132 | `0x84` | `E_P_LOSS_SECOND_WHILE_NOT_TRAINING` |
| 133 | `0x85` | `MODIFY_ANGER_WHILE_SOLITARY_IN_PIT_PER_SECOND` |
| 134 | `0x86` | `MODIFY_ANGER_OF_COMBAT_PIT_VICTOR` |
| 135 | `0x87` | `MODIFY_ANGER_OF_PIT_SPECTATOR_PER_SECOND` |
| 136 | `0x88` | `CONVERT_NEUTRAL_ROOM_HEALTH` |
| 138 | `0x8A` | `MANUFACTURE_POINT_LOSS_WHILE_NO_WORKERS_PER_SECOND` |
| 139 | `0x8B` | `CASINO_BIG_WINNER_INCREASED_HAPPINESS_PERCENTAGE` |
| 140 | `0x8C` | `DECREASE_ENTRANCE_PERCENTAGE_OF_LAST_SACKED_CREATURE_TYPE` |
| 141 | `0x8D` | `INCREASE_ENTRANCE_PERCENTAGE_OF_NEW_CREATURE_TYPES` |
| 142 | `0x8E` | `INCREASED_WORK_RATE_PERCENTAGE_OF_PLAYERS_TORTURED_CREATURE_TYPES` |
| 143 | `0x8F` | `INCREASED_WORK_RATE_PERCENTAGE_WHEN_CASINO_BIG_WIN_OCCURS` |
| 144 | `0x90` | `IMP_POP_COUNTDOWN_TIME_SECONDS` |
| 145 | `0x91` | `NUMBER_OF_RANDOM_WALL_DETERIORATIONS_PER_SECOND` |
| 146 | `0x92` | `WALL_DETERIORATION_DAMAGE` |
| 147 | `0x93` | `WALL_DAMAGE_FROM_SHOT` |
| 148 | `0x94` | `PERCENTAGE_CHANCE_OF_KILLING_CREATURE_IN_PIT` |
| 149 | `0x95` | `MODIFY_PLAYER_GOLD_WHILE_TRAINING_PER_SECOND` |
| 150 | `0x96` | `SKELETON_ARMY_DURATION_SECONDS` |
| 151 | `0x97` | `OVERCROWDING_JAIL_BREAK_PERCENTAGE_PER_EXTRA_CREATURE` |
| 152 | `0x98` | `MAX_TIME_IN_GUARD_ROOM_BEFORE_WANDERING_TO_GUARD_POST_SECONDS` |
| 154 | `0x9A` | `AMOUNT_OF_GOLD_STOLEN_IN_FIRST_PERSON` |
| 155 | `0x9B` | `BOULDER_DETERIORATION_DAMAGE_PERCENTAGE_PER_SECOND` |
| 156 | `0x9C` | `DRUNKEN_DURATION_SECONDS` |
| 157 | `0x9D` | `BOULDER_INITIAL_HEALTH` |
| 158 | `0x9E` | `BOULDER_SPEED_TILES_PER_SECOND` |
| 159 | `0x9F` | `BOULDER_TO_CREATURE_DAMAGE_PERCENTAGE` |
| 160 | `0xA0` | `BOULDER_SPEED_TO_HEALTH_RATIO` |
| 161 | `0xA1` | `BOULDER_FEAR_AMOUNT` |
| 162 | `0xA2` | `BOULDER_FEAR_RANGE` |
| 163 | `0xA3` | `DRUNK_CHANCE_PERCENTAGE_PER_DRINK` |
| 164 | `0xA4` | `INCREASED_WORK_RATE_PECENTAGE_FROM_SLAPPING` |
| 165 | `0xA5` | `INCREASED_WORK_RATE_DURATION_FROM_SLAPPING_SECONDS` |
| 166 | `0xA6` | `DUNGEON_HEART_MAX_MANA_INCREASE` |
| 167 | `0xA7` | `DUNGEON_HEART_MANA_GENERATION_INCREASE_PER_SECOND` |
| 168 | `0xA8` | `TREMOR_DAMAGE_PER_TERRAIN_TILE_PER_SECOND` |
| 169 | `0xA9` | `EXTERNAL_GUARDING_DURATION_SECONDS` |
| 170 | `0xAA` | `MINIMUM_JOB_DURATION_SECONDS` |
| 173 | `0xAD` | `CREATURE_CRITICAL_HEALTH_PERCENTAGE_OF_MAX` |
| 174 | `0xAE` | `DUNGEON_HEART_OBJECT_HEALTH` |
| 175 | `0xAF` | `CLAIM_NEUTRAL_MANA_VAULT_HEALTH` |
| 176 | `0xB0` | `CLAIM_ENEMY_MANA_VAULT_HEALTH` |
| 177 | `0xB1` | `CREATURES_SUPPORTED_PER_ADDITIONAL_PORTAL` |
| 178 | `0xB2` | `MAX_GOLD_PER_DUNGEON_HEART_TILE` |
| 179 | `0xB3` | `IMP_EXPERIENCE_GAIN_PER_SECOND` |
| 180 | `0xB4` | `DUNGEON_HEART_CLAIM_SCAN_RADIUS_TILES` |
| 181 | `0xB5` | `STUN_TIME_PERCENTAGE_INCREASE_WHEN_HIT` |
| 182 | `0xB6` | `CALL_TO_ARMS_FIGHT_DISTANCE_TILES` |
| 183 | `0xB7` | `DUNGEON_HEART_REPORTING_DISTANCE_TILES` |
| 184 | `0xB8` | `DWARF_DIGGING_MULTIPLIER` |
| 185 | `0xB9` | `TRIGGER_TRAP_TRIGGER_SPEED_TILES_PER_SECOND` |
| 187 | `0xBB` | `FIRST_PERSON_ACCURACY_ANGLE_DEGREES` |
| 188 | `0xBC` | `SPECIAL_RECEIVE_IMPS_AMOUNT` |
| 189 | `0xBD` | `DUNGEON_HEART_CREATURE_GATHERING_DISTANCE_TILES` |
| 190 | `0xBE` | `GUARD_ROOM_REPORTING_DISTANCE_TILES` |
| 191 | `0xBF` | `GUARD_POST_REPORTING_DISTANCE_TILES` |
| 192 | `0xC0` | `COMBAT_PIT_MAX_EXPERIENCE_LEVEL` |
| 193 | `0xC1` | `TIME_IN_HAND_BEFORE_CREATURES_BECOME_ANGRY_SECONDS` |
| 194 | `0xC2` | `SLAP_WORK_FASTER_DURATION_SECONDS` |
| 195 | `0xC3` | `SLAP_SPEED_UP_DURATION_SECONDS` |
| 196 | `0xC4` | `TORTURE_SPEED_UP_DURATION_SECONDS` |
| 197 | `0xC5` | `CASINO_WIN_SPEED_UP_DURATION_SECONDS` |
| 198 | `0xC6` | `CRITICAL_HEALTH_PERCENTAGE_FOR_HERO_LAIR` |
| 199 | `0xC7` | `GAMBLING_MODIFY_CREATURE_MONEY_SMILES_PER_SECOND` |
| 200 | `0xC8` | `GAMBLING_MODIFY_CREATURE_MONEY_MONEY_PER_SECOND` |
| 201 | `0xC9` | `GAMBLING_MODIFY_CREATURE_ANGER_SMILES_PER_SECOND` |
| 202 | `0xCA` | `GAMBLING_MODIFY_CREATURE_ANGER_MONEY_PER_SECOND` |
| 203 | `0xCB` | `GAMBLING_DELAY_BEFORE_BECOMING_FEARLESS_SECONDS` |
| 204 | `0xCC` | `GAMBLING_FEARLESS_DURATION_SECONDS` |
| 205 | `0xCD` | `GAMBLING_CREATURE_JACKPOT_CHANCE_SMILES_PER_SECOND` |
| 206 | `0xCE` | `GAMBLING_CREATURE_JACKPOT_CHANCE_MONEY_PER_SECOND` |
| 207 | `0xCF` | `GAMBLING_JACKPOT_PAY_TIME_LIMIT` |
| 208 | `0xD0` | `GAMBLING_JACKPOT_PAY_TIME_LIMIT_MISSED_ANGER_PERCENTAGE_MODIFIER` |
| 209 | `0xD1` | `GAMBLING_JACKPOT_BASIC_AMOUNT` |
| 210 | `0xD2` | `FIRST_PERSON_RECHARGE_MODIFY_PERCENTAGE` |
| 211 | `0xD3` | `BACK_OFF_MINIMUM_DURATION_SECONDS` |
| 212 | `0xD4` | `BACK_OFF_MAXIMUM_DURATION_SECONDS` |
| 213 | `0xD5` | `GOOD_CREATURES_CHASE_ENEMY_DURATION_SECONDS` |
| 214 | `0xD6` | `EVIL_CREATURES_CHASE_ENEMY_DURATION_SECONDS` |
| 215 | `0xD7` | `MUSIC_LEVEL_TWO_THREAT_THRESHOLD` |
| 216 | `0xD8` | `MUSIC_LEVEL_THREE_THREAT_THRESHOLD` |
| 217 | `0xD9` | `MUSIC_LEVEL_FOUR_THREAT_THRESHOLD` |
| 218 | `0xDA` | `TIME_BEFORE_DUNGEON_HEART_CONSTRUCTION_BEGINS` |
| 219 | `0xDB` | `DEFAULT_TORCH_LIGHT_INTENSITY` |
| 220 | `0xDC` | `DEFAULT_TORCH_LIGHT_RADIUS_TILES` |
| 221 | `0xDD` | `DEFAULT_TORCH_LIGHT_HEIGHT_TILES` |
| 222 | `0xDE` | `MAXIMUM_MANA_THRESHOLD` |
| 223 | `0xDF` | `MAXIMUM_GROUP_NUMBER_OF_FOLLOWERS` |
| 225 | `0xE1` | `COMBAT_PIT_MELEE_DAMAGE_MODIFIER_PERCENTAGE` |
| 226 | `0xE2` | `UPGRADED_POSSESSION_COST_PERCENTAGE` |
| 227 | `0xE3` | `UPGRADED_CALL_TO_ARMS_COST_PERCENTAGE` |
| 228 | `0xE4` | `MPD_SCORE_HERO_KILLED` |
| 229 | `0xE5` | `MPD_SCORE_LAND_OWNED` |
| 230 | `0xE6` | `MPD_SCORE_GOLD_SLABS_MINED` |
| 231 | `0xE7` | `MPD_SCORE_ITEM_MANUFACTURED` |
| 232 | `0xE8` | `MPD_SCORE_CREATURE_ENTERED` |
| 234 | `0xEA` | `TORTURE_CHANCE_OF_DYING_WHEN_CONVERTED` |
| 235 | `0xEB` | `MAXIMUM_MANA_GAIN_PER_SECOND` |
| 236 | `0xEC` | `CLAIM_SCAN_LIGHT_RED` |
| 237 | `0xED` | `CLAIM_SCAN_LIGHT_GREEN` |
| 238 | `0xEE` | `CLAIM_SCAN_LIGHT_BLUE` |
| 239 | `0xEF` | `PIT_PERCENTAGE_DAMAGE_TAKEN_OF_NORMAL_COMBAT` |
| 240 | `0xF0` | `BOULDER_SLAP_DAMAGE` |
| 74 | `0x4A` | `GAME_TICKS` |
| 241 | `0xF1` | `LEVEL_RATING` |
| 242 | `0xF2` | `AVERAGE_TIME` |
| 233 | `0xE9` | `UNKNOWN_233` |
| 61 | `0x3D` | `UNKNOWN_61` |
| 62 | `0x3E` | `UNKNOWN_62` |
| 63 | `0x3F` | `UNKNOWN_63` |
