# Stat ID → поле существа в движке

Связаны загрузчик 0x568E30, преобразование 0x492320 и setter 0x494A70, вызываемый Variables type 77. Названия OpenKeeper оставлены для сопоставления. Для 8, 11, 14 setter не делает присваивания. Это не означает, что таблицы типов 65/224 не имеют этих слотов.
Параметры type 77: creatureID, statID (setter принимает u8), runtimeValue. Значение пишется **после** загрузки и преобразований; единицы могут отличаться от сырых полей Creatures. В частности 21/22/23 делятся на TPS, 24 обрезается до u8, 29 записывается как u16 масштаба. Полный switch сохранён в ghidra/game-creature-stat-set.txt.

| Stat ID | Имя OpenKeeper | Offset Creatures | Поле базовой схемы | Offset runtime |
|---:|---|---|---|---|
| 0 | HEIGHT_TILES | 0xea4 | Height | 0x648 |
| 1 | HEALTH | 0xf0c | Hp | 0x692 |
| 2 | FEAR | 0xf10 | Fear | 0x696 |
| 3 | THREAT | 0xf12 | Threat | 0x698 |
| 4 | MELEE_DAMAGE | 0xf14 | MeleeDamage | 0x5c4 |
| 5 | PAY | 0xf1c | Pay | 0x69a |
| 6 | MAX_GOLD_HELD | 0xf1e | MaxGoldHeld | 0x69c |
| 7 | INITIAL_GOLD_HELD | 0xf34 | InitialGoldHeld | 0x69e |
| 8 | UNKNOWN_8 | — | нет ветки setter | — |
| 9 | HUNGER_FILL_CHICKENS | 0xf4e | HungerFill | 0x66d |
| 10 | MANA_GENERATED_BY_PRAYER_PER_SECOND | 0xf18 | ManaGenPrayer | 0x6a2 |
| 11 | UNKNOWN_11 | — | нет ветки setter | — |
| 12 | UNKNOWN_12 | 0xf4c | unk3ea[0].unk3ea | 0x787 |
| 13 | EXPERIENCE_POINTS_FOR_NEXT_LEVEL | 0xf00 | ExpForNextLevel | 0x6b5 |
| 14 | UNKNOWN_14 | — | нет ветки setter | — |
| 15 | EXPERIENCE_POINTS_PER_SECOND | 0xf04 | ExpPerSecond | 0x6b7 |
| 16 | EXPERIENCE_POINTS_FROM_TRAINING_PER_SECOND | 0xf06 | ExpPerSecondTraining | 0x6b9 |
| 17 | RESEARCH_POINTS_PER_SECOND | 0xf08 | ResearchPerSecond | 0x6bb |
| 18 | MANUFACTURE_POINTS_PER_SECOND | 0xf0a | ManufacturePerSecond | 0x6bd |
| 19 | DECOMPOSE_VALUE | 0xf22 | DecomposeValue | 0x6bf |
| 20 | UNKNOWN_20 | 0xf51 | Unk3eb2 | 0x5e0 |
| 21 | SPEED_TILES_PER_SECOND | 0xeb4 | Speed | 0x5b4 |
| 22 | RUN_SPEED_TILES_PER_SECOND | 0xeb8 | RunSpeed | 0x5b8 |
| 23 | UNKNOWN_23 | 0xef0 | Unkef0 | 0x68e |
| 24 | TORTURE_TIME_TO_CONVERT_SECONDS | 0xef4 | TortureTimeToConvert | 0x76c |
| 25 | UNKNOWN_25 | 0xee0 | Unkee0 | 0x75c |
| 26 | UNKNOWN_26 | 0xee4 | Unkee4 | 0x760 |
| 27 | POSSESSION_MANA_COST_PER_SECOND | 0xee8 | PossessionManaCost | 0x764 |
| 28 | OWN_LAND_HEALTH_INCREASE_PER_SECOND | 0xeea | OwnLandHealthIncrease | 0x6a6 |
| 29 | UNKNOWN_29 | 0xfcd | Unk4 | 0x626 |
| 30 | DISTANCE_CAN_HEAR_TILES | 0xecc | DistanceCanHear | 0x77f |
| 31 | MELEE_RECHARGE_TIME_SECONDS | 0xef8 | MeleeRecharge | 0x5c6 |

Сопоставление уточняет Unknown: stat12 попадает в ScoreRating (0xF4C), stat20 — DisarmFailurePercentage (0xF51), stat23 — поле 0xEF0, stat25 — поле 0xEE0, stat26 — AbilityCreepPercentage (0xEE4), stat29 — Scale (0xFCD). Имена дополнительных шаблонов не доказывают единицы или все последствия каждого поля.

## Уточнение25 октября, этап24

Для Stat26 теперь независимо подтверждены native-подпись Ability Creep Percentage и игровые потребители в11 участках8 функций. Raw+0xEE4/runtime+0x760 взвешивает квадрат расстояния при состоянии Creeping; level!=1 использует signed коэффициент Variables65/Stat26. Variables77/Stat26 пишет весь DWORD без TPS или fixed-point преобразования. Native Editor Pro сужает этот DWORD до model-byte уже при callback загрузки; writer сохраняет zero-extended byte. Подробности и116 эмуляций — `CREATURE_CREEP_RU.md`, `creature-creep-semantics.json`. Имена UNKNOWN_26 в baseline перечислении сохранены для сопоставления.
